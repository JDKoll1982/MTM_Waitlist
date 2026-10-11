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
    private const string PreviousPeopleReadProcedure = "sp_auth_user_previous_sign_ins_list";
    private const string SignInName = "JKoll";
    private const string Pin = "8391";

    /// <summary>One random salt for the class, so no case depends on a value written into the repository.</summary>
    private static readonly byte[] s_salt = PasswordSecretHasher.NewSalt();

    [TestMethod]
    public void ViewModel_SignInViewModelTests_Hint_BeforeANameIsTypedSaysTheNameIsWhatIsMissing()
    {
        // Arrange
        var viewModel = Build(new StubAccountStore(TemporaryAccount()));

        // Act
        var hint = viewModel.Hint;

        // Assert: the person is told what to do rather than left to guess (checklist 8.4a).
        Assert.AreEqual("Startup_SignIn.HintNameRequired".GetLocalized(), hint);
    }

    [TestMethod]
    public void ViewModel_SignInViewModelTests_Hint_OnceANameIsTypedSaysThePasswordIsWhatIsMissing()
    {
        // Arrange
        var viewModel = Build(new StubAccountStore(TemporaryAccount()));

        // Act
        viewModel.SignInName = SignInName;

        // Assert
        Assert.AreEqual("Startup_SignIn.HintCredentialRequired".GetLocalized(), viewModel.Hint);
    }

    [TestMethod]
    public async Task ViewModel_SignInViewModelTests_SignInCommand_WhenTheCredentialIsRefusedStatesItAndKeepsTheFormOpen()
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
    public async Task ViewModel_SignInViewModelTests_SignInCommand_WhenTheTemporaryCredentialLimitIsSpentSaysANewPasswordIsNeeded()
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
    public async Task ViewModel_SignInViewModelTests_SignInCommand_WhenTheCredentialIsAcceptedCarriesTheLaunchOnWithTheSignInHeld()
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
    public async Task ViewModel_SignInViewModelTests_SignInCommand_WhenATemporaryCredentialIsAcceptedOpensThePasswordChangeAndWaits()
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
    public async Task ViewModel_SignInViewModelTests_LoadPreviousPeopleCommand_WhenTheStoreHoldsThreePeopleOffersThemInTheOrderTheStoreSent()
    {
        // Arrange: three people who signed in on this computer, most recent first, as the read returns them.
        var store = new StubAccountStore(TemporaryAccount());
        store.PreviousPeople.Add(Person("KSmith", "K. Smith"));
        store.PreviousPeople.Add(Person("ABrown", "A. Brown"));
        store.PreviousPeople.Add(Person(SignInName, "J. Koll"));
        var viewModel = Build(store);

        // Act
        await viewModel.LoadPreviousPeopleAsync();

        // Assert: every one of them is offered, and it is the read's own order that is kept (FR-048).
        Assert.AreEqual(3, viewModel.PreviousPeople.Count);
        Assert.IsTrue(viewModel.HasPreviousPeople);
        CollectionAssert.AreEqual(
            new[] { "KSmith", "ABrown", SignInName },
            viewModel.PreviousPeople.Select(person => person.SignInName).ToArray());
    }

    [TestMethod]
    public async Task ViewModel_SignInViewModelTests_LoadPreviousPeopleCommand_WhenTheStoreCannotAnswerShowsTheFormWithoutThem()
    {
        // Arrange: a store that is unreachable, which must not stop anybody signing in (FR-015).
        var store = new StubAccountStore(TemporaryAccount()) { Fails = true };
        var viewModel = Build(store);

        // Act
        await viewModel.LoadPreviousPeopleAsync();

        // Assert: the list is empty and the form is untouched.
        Assert.AreEqual(0, viewModel.PreviousPeople.Count);
        Assert.IsFalse(viewModel.HasPreviousPeople);
    }

    [TestMethod]
    public async Task ViewModel_SignInViewModelTests_LoadPreviousPeopleCommand_WhenThisComputerHasNoRecordAsksTheStoreNothing()
    {
        // Arrange: a machine the store has never held, so no sign-ins are recorded against it.
        var store = new StubAccountStore(TemporaryAccount());
        var viewModel = Build(store, identifierSignIn: null, recorded: false);

        // Act
        await viewModel.LoadPreviousPeopleAsync();

        // Assert: there is nothing to ask about and no round trip is made (FR-048).
        Assert.AreEqual(0, viewModel.PreviousPeople.Count);
        CollectionAssert.DoesNotContain(store.CalledProcedures, "sp_auth_user_previous_sign_ins_list");
    }

    [TestMethod]
    public async Task ViewModel_SignInViewModelTests_ChoosePreviousPersonCommand_WhenAPersonIsChosenFillsInTheUserNameAndNothingElse()
    {
        // Arrange
        var store = new StubAccountStore(TemporaryAccount(requirePasswordChange: false));
        store.PreviousPeople.Add(Person("KSmith", "K. Smith"));
        store.PreviousPeople.Add(Person(SignInName, "J. Koll"));
        var viewModel = Build(store, Identifier(new StubDirectory(), new StubPlant()));
        var accepted = 0;
        viewModel.SignInAccepted += (_, _) => accepted++;

        await viewModel.LoadPreviousPeopleAsync();

        // Act: the person picks somebody from the list beside the form.
        viewModel.ChoosePreviousPersonCommand.Execute(viewModel.PreviousPeople[1]);

        // Assert: the name is filled in, the PIN is still to be typed, and nobody has been signed in (FR-014).
        Assert.AreEqual(SignInName, viewModel.SignInName);
        Assert.IsTrue(viewModel.IsCredentialStage, "choosing a person moves to the PIN, not past it");
        Assert.AreEqual(0, accepted, "choosing a name signs nobody in");
        CollectionAssert.DoesNotContain(
            store.CalledProcedures,
            "sp_auth_user_credential_get",
            "no credential is checked until one is typed");
    }

    [TestMethod]
    public void ViewModel_SignInViewModelTests_ChoosePreviousPersonCommand_WithNothingBehindItChangesNothing()
    {
        var viewModel = Build(new StubAccountStore(TemporaryAccount()));

        viewModel.ChoosePreviousPersonCommand.Execute(null);

        Assert.AreEqual(string.Empty, viewModel.SignInName);
        Assert.IsTrue(viewModel.IsIdentifierStage);
    }

    [TestMethod]
    public async Task ViewModel_SignInViewModelTests_Enter_OnTheIdentifierStageWithAValueResolvesExactlyAsTheContinueButtonDoes()
    {
        // Arrange: a user name the store holds.
        var store = new StubAccountStore(TemporaryAccount(requirePasswordChange: false));
        var viewModel = Build(store, Identifier(new StubDirectory(), new StubPlant()));

        // Act
        await viewModel.EnterAsync(SignInField.Identifier, SignInName);

        // Assert: the second stage is showing, with the name the store resolved.
        Assert.IsTrue(viewModel.IsCredentialStage, "an identifier the store holds moves to the credential stage");
        Assert.IsFalse(viewModel.IsIdentifierStage);
        Assert.AreEqual(SignInName, viewModel.ResolvedSignInName);
        Assert.IsFalse(viewModel.HasMessage);
    }

    [TestMethod]
    public async Task ViewModel_SignInViewModelTests_Enter_OnTheIdentifierStageWithABlankFieldShowsItsHint()
    {
        var viewModel = Build(
            new StubAccountStore(TemporaryAccount()),
            Identifier(new StubDirectory(), new StubPlant()));

        await viewModel.EnterAsync(SignInField.Identifier, "   ");

        Assert.IsTrue(viewModel.IsIdentifierStage, "nothing resolved, so the first stage is still showing");
        Assert.AreEqual("Startup_SignIn.HintNameRequired".GetLocalized(), viewModel.Message);
    }

    [TestMethod]
    public async Task ViewModel_SignInViewModelTests_Enter_OnTheIdentifierStageForAnUnknownNumberRefusesAndKeepsTheFormOpen()
    {
        var viewModel = Build(
            new StubAccountStore(TemporaryAccount()),
            Identifier(new StubDirectory(), new StubPlant()));

        await viewModel.EnterAsync(SignInField.Identifier, "6229");

        Assert.IsTrue(viewModel.IsIdentifierStage, "a refusal leaves the form open (FR-012)");
        Assert.IsTrue(viewModel.HasMessage);
    }

    [TestMethod]
    public async Task ViewModel_SignInViewModelTests_ContinueWithIdentifier_WhenOnlyThePlantSystemKnowsTheNumberAsksForSetUp()
    {
        var directory = new StubDirectory();
        var plant = new StubPlant
        {
            Answer = PlantEmployeeLookup.Found(new PlantEmployee("6229", "John", "Koll")),
        };
        var viewModel = Build(new StubAccountStore(TemporaryAccount()), Identifier(directory, plant));
        SetUpRequestedEventArgs? requested = null;
        viewModel.SetUpRequested += (_, args) => requested = args;

        await viewModel.ContinueWithIdentifierCommand.ExecuteAsync("6229");

        Assert.IsNotNull(requested, "FR-006: a person the plant system knows is offered set-up");
        Assert.AreEqual("6229", requested!.EmployeeNumber);
        Assert.AreEqual("John", requested.FirstName);
        Assert.AreEqual("Koll", requested.LastName);
    }

    [TestMethod]
    public async Task ViewModel_SignInViewModelTests_Enter_OnTheCredentialStageWithAValueSubmitsExactlyAsTheButtonDoes()
    {
        var store = new StubAccountStore(TemporaryAccount(requirePasswordChange: false));
        var viewModel = Build(store, Identifier(new StubDirectory(), new StubPlant()));
        var accepted = 0;
        viewModel.SignInAccepted += (_, _) => accepted++;

        await viewModel.EnterAsync(SignInField.Identifier, SignInName);
        await viewModel.EnterAsync(SignInField.Credential, Pin);

        Assert.AreEqual(1, accepted, "Enter on the credential stage signs in exactly as the button does (FR-014)");
    }

    [TestMethod]
    public async Task ViewModel_SignInViewModelTests_Enter_OnTheCredentialStageWithABlankFieldShowsItsHint()
    {
        var viewModel = Build(
            new StubAccountStore(TemporaryAccount()),
            Identifier(new StubDirectory(), new StubPlant()));

        await viewModel.EnterAsync(SignInField.Credential, string.Empty);

        Assert.AreEqual("Startup_SignIn.HintCredentialRequired".GetLocalized(), viewModel.Message);
    }

    /// <summary>The identifier stage over the store directory and the plant system the test supplied.</summary>
    private static IdentifierSignInService Identifier(
        IEmployeeDirectoryService directory,
        IPlantEmployeeReader plant)
        => new(directory, new CredentialCheckService(new StubAccountStore(TemporaryAccount())), plant);

    /// <summary>The store's account records, answering the employee-identifier read.</summary>
    private sealed class StubDirectory : IEmployeeDirectoryService
    {
        public EmployeeIdentity? Answer { get; set; }

        public Task<EmployeeIdentity?> FindByEmployeeIdentifierAsync(
            string employeeIdentifier,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Answer);
    }

    /// <summary>The plant system, answering the sixth read shape's contract.</summary>
    private sealed class StubPlant : IPlantEmployeeReader
    {
        public PlantEmployeeLookup Answer { get; set; } = PlantEmployeeLookup.NotHeld;

        public Task<PlantEmployeeLookup> FindByNumberAsync(
            string employeeNumber,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Answer);
    }

    private static SignInViewModel Build(StubAccountStore store) => Build(store, identifierSignIn: null);

    private static SignInViewModel Build(
        StubAccountStore store,
        IdentifierSignInService? identifierSignIn,
        bool recorded = true) => new(
        new CredentialCheckService(store),
        new PreviousSignInService(store),
        new PendingSignIn(),
        new SignInOutcome(),
        new StubMachineFacts { Recorded = recorded },
        identifierSignIn);

    /// <summary>One row of the previous-people read, in the column names that read returns.</summary>
    private static Dictionary<string, object?> Person(string signInName, string displayName) => new()
    {
        ["username_normalized"] = signInName,
        ["display_name"] = displayName,
    };

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

    /// <summary>This computer as the form sees it: recorded, so the offer has a machine to be drawn for.</summary>
    private sealed class StubMachineFacts : IMachineFacts
    {
        /// <summary>Whether the store already holds this computer's record.</summary>
        public bool Recorded { get; init; } = true;

        public string Hostname => "MTMFG-161";

        public string? MacAddress => "aa-bb-cc-dd-ee-ff";

        public ComputerRecord? RegisteredComputer => Recorded
            ? new ComputerRecord
            {
                Id = 3L,
                ComputerName = "MTMFG-161",
                DisplayName = "Shop floor station",
                Location = "a machine the test owns",
                MacAddressNormalized = "aa-bb-cc-dd-ee-ff",
                IsRegistered = true,
            }
            : null;

        public bool IsRegistered => Recorded;

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

        /// <summary>The rows the previous-people read answers with, in the order the store would return them.</summary>
        public List<Dictionary<string, object?>> PreviousPeople { get; } = [];

        /// <summary>Whether every read fails, so a store outage can be exercised.</summary>
        public bool Fails { get; init; }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            CalledProcedures.Add(storedProcedureName);

            if (Fails)
            {
                throw new InvalidOperationException("the store did not answer");
            }

            return Task.FromResult<IReadOnlyList<Dictionary<string, object?>>>(
                storedProcedureName switch
                {
                    CredentialReadProcedure => [_account],
                    PreviousPeopleReadProcedure => [.. PreviousPeople],
                    _ => [],
                });
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
