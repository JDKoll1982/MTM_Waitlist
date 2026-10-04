using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Startup.Services;
using MTM_Waitlist.Module_Startup.ViewModels;

namespace MTM_Waitlist.Tests.Module_Startup.ViewModels;

/// <summary>
/// The sign-in surface (checklist 8.4a; FR-012, FR-013, FR-014): the hint states what is still missing rather
/// than leaving the person guessing, a refusal stays on the form, and the forced password change opens only
/// after a temporary credential has been accepted.
/// </summary>
/// <remarks>
/// The doubles are hand-written recordings, as the suite's conventions require. The account row is built with the
/// same hasher the application uses, so a passing case proves the comparison rather than a fake's idea of one.
/// The hint's expected values are resolved through the same resource call the view model makes, so the case
/// holds whether the test host can resolve resources or falls back to the key.
/// </remarks>
[TestClass]
public sealed class SignInViewModelTests
{
    private const string CredentialReadProcedure = "sp_auth_user_credential_get";
    private const string SignInName = "JKoll";
    private const string Pin = "8391";

    /// <summary>One random salt for the class, so no case depends on a value written into the repository.</summary>
    private static readonly byte[] s_salt = PasswordSecretHasher.NewSalt();

    [TestMethod]
    public void Hint_BeforeANameIsTyped_SaysTheNameIsWhatIsMissing()
    {
        // Arrange
        var viewModel = Build(new StubAccountStore(TemporaryAccount()));

        // Act
        var hint = viewModel.Hint;

        // Assert: the person is told what to do rather than left to guess (checklist 8.4a).
        Assert.AreEqual("Startup_SignIn.HintNameRequired".GetLocalized(), hint);
    }

    [TestMethod]
    public void Hint_OnceANameIsTyped_SaysThePasswordIsWhatIsMissing()
    {
        // Arrange
        var viewModel = Build(new StubAccountStore(TemporaryAccount()));

        // Act
        viewModel.SignInName = SignInName;

        // Assert
        Assert.AreEqual("Startup_SignIn.HintCredentialRequired".GetLocalized(), viewModel.Hint);
    }

    [TestMethod]
    public async Task SignInCommand_WhenTheCredentialIsRefused_StatesItAndKeepsTheFormOpen()
    {
        // Arrange
        var viewModel = Build(new StubAccountStore(TemporaryAccount()));
        var accepted = false;
        viewModel.SignInAccepted += (_, _) => accepted = true;

        // Act
        await viewModel.SignInCommand.ExecuteAsync(new SignInCredentials(SignInName, "0000"));

        // Assert: the person is asked again rather than sent away, which is what makes the attempt limit mean
        // something (FR-012).
        Assert.IsFalse(accepted, "a refused credential must not carry the launch on");
        Assert.AreEqual("Startup_SignIn.RefusedCredential".GetLocalized(), viewModel.Message);
        Assert.IsTrue(viewModel.HasMessage);
        Assert.IsTrue(viewModel.ShowSignInForm);
        Assert.IsFalse(viewModel.IsPasswordChangeRequired);
    }

    [TestMethod]
    public async Task SignInCommand_WhenTheTemporaryCredentialLimitIsSpent_SaysANewPasswordIsNeeded()
    {
        // Arrange: five failures already recorded against the account.
        var viewModel = Build(new StubAccountStore(TemporaryAccount(failedAttempts: 5)));

        // Act: the correct value is presented and is still refused (FR-012).
        await viewModel.SignInCommand.ExecuteAsync(new SignInCredentials(SignInName, Pin));

        // Assert
        Assert.AreEqual("Startup_SignIn.RefusedAttemptsExhausted".GetLocalized(), viewModel.Message);
        Assert.IsFalse(viewModel.IsPasswordChangeRequired);
    }

    [TestMethod]
    public async Task SignInCommand_WhenTheCredentialIsAccepted_CarriesTheLaunchOnWithTheSignInHeld()
    {
        // Arrange
        var viewModel = Build(new StubAccountStore(TemporaryAccount(requirePasswordChange: false)));
        var accepted = 0;
        viewModel.SignInAccepted += (_, _) => accepted++;

        // Act
        await viewModel.SignInCommand.ExecuteAsync(new SignInCredentials(SignInName, Pin));

        // Assert: the launch holds the sign-in, so the steps that follow can present it (FR-002).
        Assert.AreEqual(1, accepted);
        Assert.AreEqual(SignInName, viewModel.SignInName);
        Assert.IsFalse(viewModel.HasMessage);
    }

    [TestMethod]
    public async Task SignInCommand_WhenATemporaryCredentialIsAccepted_OpensThePasswordChangeAndWaits()
    {
        // Arrange: the account is still on its temporary credential.
        var viewModel = Build(new StubAccountStore(TemporaryAccount()));
        var accepted = false;
        viewModel.SignInAccepted += (_, _) => accepted = true;

        // Act
        await viewModel.SignInCommand.ExecuteAsync(new SignInCredentials(SignInName, Pin));

        // Assert: the new password is asked for before anything else, and the launch does not carry on until it
        // has been chosen (FR-013).
        Assert.IsTrue(viewModel.IsPasswordChangeRequired);
        Assert.IsFalse(viewModel.ShowSignInForm, "the form is replaced by the new-password panel");
        Assert.IsFalse(viewModel.CanSignIn);
        Assert.IsFalse(accepted);
    }

    [TestMethod]
    public async Task UseRememberedSignInCommand_WhenNothingIsRemembered_TellsThePersonToTypeThePassword()
    {
        // Arrange: a key the service can read, and no remembered sign-in stored for this person and machine.
        var viewModel = Build(new StubAccountStore(TemporaryAccount(requirePasswordChange: false)));
        viewModel.SignInName = SignInName;

        // Act
        await viewModel.UseRememberedSignInCommand.ExecuteAsync(null);

        // Assert: the fall-back is stated rather than the button appearing to do nothing (FR-014).
        Assert.AreEqual("Startup_SignIn.RememberedUnavailable".GetLocalized(), viewModel.Message);
    }

    [TestMethod]
    public void CanUseRememberedSignIn_UntilANameIsTyped_IsNotOffered()
    {
        // Arrange
        var viewModel = Build(new StubAccountStore(TemporaryAccount()));

        // Act: the store keys a remembered sign-in by person, so there is nothing to ask about without a name.
        var before = viewModel.CanUseRememberedSignIn;
        viewModel.SignInName = SignInName;

        // Assert
        Assert.IsFalse(before);
        Assert.IsTrue(viewModel.CanUseRememberedSignIn);
    }

    [TestMethod]
    public async Task SignInCommand_WhenRememberMeIsNotChosen_ForgetsWhatThisMachineRemembered()
    {
        // Arrange: the person signs in with the box clear, which is how someone stops being remembered on a
        // shared computer.
        var store = new StubAccountStore(TemporaryAccount(requirePasswordChange: false));
        var viewModel = Build(store);

        // Act
        await viewModel.SignInCommand.ExecuteAsync(new SignInCredentials(SignInName, Pin));

        // Assert: the earlier choice cannot outlive the person changing their mind (FR-014).
        CollectionAssert.Contains(
            store.CalledProcedures,
            "sp_auth_remembered_sign_ins_clear",
            "signing in without the choice must forget what this machine held");
    }

    [TestMethod]
    public async Task SignInCommand_WhenRememberMeIsChosen_StoresTheSignInAgainstThePerson()
    {
        // Arrange
        var store = new StubAccountStore(TemporaryAccount(requirePasswordChange: false));
        var viewModel = Build(store);
        viewModel.RememberMe = true;

        // Act
        await viewModel.SignInCommand.ExecuteAsync(new SignInCredentials(SignInName, Pin));

        // Assert
        CollectionAssert.Contains(store.CalledProcedures, "sp_auth_remembered_sign_ins_upsert");
    }

    private static SignInViewModel Build(StubAccountStore store) => new(
        new CredentialCheckService(store),
        new RememberedSignInService(store, new StubKeySource()),
        new PendingSignIn(),
        new SignInOutcome(),
        new StubMachineFacts());

    /// <summary>One account row in the column names the credential read returns.</summary>
    private static Dictionary<string, object?> TemporaryAccount(
        long failedAttempts = 0,
        bool requirePasswordChange = true) => new()
    {
        ["id"] = 7L,
        ["role_code"] = "role:production",
        ["display_name"] = "J. Koll",
        ["employee_identifier"] = "1042",
        ["password_hash"] = PasswordSecretHasher.Hash(Pin, s_salt),
        ["password_salt"] = s_salt,
        ["require_password_change"] = requirePasswordChange ? 1L : 0L,
        ["temporary_credential_failed_attempts"] = failedAttempts,
    };

    /// <summary>A key source that can always be read, since this class is about what the form does with an answer.</summary>
    private sealed class StubKeySource : ISharedKeySource
    {
        public byte[]? ReadKey() => [.. Enumerable.Repeat((byte)11, SharedKeyFileSource.KeyLengthBytes)];
    }

    /// <summary>This computer as the form sees it: recorded, so a remembered sign-in has a machine to be keyed to.</summary>
    private sealed class StubMachineFacts : IMachineFacts
    {
        public string Hostname => "MTMFG-161";

        public string? MacAddress => "aa-bb-cc-dd-ee-ff";

        public ComputerRecord? RegisteredComputer => new()
        {
            Id = 3L,
            ComputerName = "MTMFG-161",
            DisplayName = "Shop floor station",
            Description = "a machine the test owns",
            MacAddressNormalized = "aa-bb-cc-dd-ee-ff",
            IsRegistered = true,
        };

        public bool IsRegistered => true;

        public bool HardwareIdentityReadable => true;
    }

    /// <summary>
    /// A store that answers the credential read and the attempt write, and nothing else, so the sign-in form can
    /// be exercised without a database.
    /// </summary>
    private sealed class StubAccountStore : IMySqlHelperServer
    {
        private readonly Dictionary<string, object?> _account;

        public StubAccountStore(Dictionary<string, object?> account) => _account = account;

        /// <summary>Every procedure the store was asked for, so a claim about a call can be read back.</summary>
        public List<string> CalledProcedures { get; } = [];

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            CalledProcedures.Add(storedProcedureName);

            return Task.FromResult<IReadOnlyList<Dictionary<string, object?>>>(
                string.Equals(storedProcedureName, CredentialReadProcedure, StringComparison.Ordinal)
                    ? [_account]
                    : []);
        }

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            CalledProcedures.Add(storedProcedureName);

            return Task.FromResult(1);
        }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("No statement text is written in the sign-in path (constitution III).");

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("No statement text is written in the sign-in path (constitution III).");
    }
}
