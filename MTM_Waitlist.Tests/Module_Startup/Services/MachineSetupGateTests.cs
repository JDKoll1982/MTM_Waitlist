using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Permissions;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Startup.Services;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// The machine-setup gate (`contracts/machine-configuration-contract.md` section 2; FR-007): it authorises a person
/// who holds IT Department or Developer authority and nobody else, it states every refusal, and it answers nothing
/// that could open the main screens.
/// </summary>
/// <remarks>
/// <para>
/// What is pinned here is the seam rather than the store: which procedure is called, with which normalised value,
/// against which database, and what each answer means. The store's own rules, the unique display name and the
/// seeded baselines, live in the store and are asserted where they live.
/// </para>
/// <para>
/// The doubles are hand-written recordings, as the suite's conventions require. The account row is built with the
/// same hasher the application uses, so a passing test proves the credential comparison rather than a fake's idea
/// of one.
/// </para>
/// </remarks>
[TestClass]
public sealed class MachineSetupGateTests
{
    private const string CredentialProcedure = "sp_auth_user_credential_get";
    private const string PermissionsProcedure = "sp_config_permissions_user_get";

    private const string SignInName = "JKoll";
    private const string Credential = "8391";
    private const long UserId = 7L;

    /// <summary>One random salt per test class run, so no case depends on a value written into the repository.</summary>
    private static readonly byte[] s_salt = PasswordSecretHasher.NewSalt();

    [TestMethod]
    public async Task AuthorizeAsync_WhenAnITDepartmentSignInHoldsThePermission_AuthorisesConfigurationOnly()
    {
        var stub = new StubMySqlHelperServer()
            .Returns(CredentialProcedure, AccountRow("it_department"))
            .Returns(PermissionsProcedure, PermissionRow(isHeld: true));

        var authorization = await CreateGate(stub).AuthorizeAsync(SignInName, Credential, CancellationToken.None);

        Assert.IsTrue(authorization.IsAuthorized, "IT Department holds the machine-configuration key, so it unlocks setup");
        Assert.IsNull(authorization.RefusalReason, "an authorisation states no refusal");
        Assert.IsNotNull(authorization.Person);
        Assert.AreEqual("J. Koll", authorization.Person!.DisplayName);
        Assert.IsTrue(authorization.Person.Holds("role:it_department"), "the person is the one the authorisation belongs to");
        Assert.IsFalse(authorization.Person.IsSignedIn, "authorising setup never signs anybody in (FR-007)");
    }

    [TestMethod]
    public async Task AuthorizeAsync_WhenADeveloperSignsIn_AuthorisesConfiguration()
    {
        var stub = new StubMySqlHelperServer()
            .Returns(CredentialProcedure, AccountRow("developer"))
            .Returns(PermissionsProcedure, PermissionRow(isHeld: true));

        var authorization = await CreateGate(stub).AuthorizeAsync(SignInName, Credential, CancellationToken.None);

        Assert.IsTrue(authorization.IsAuthorized);
        Assert.IsNotNull(authorization.Person);
        Assert.IsTrue(authorization.Person!.Holds("developer"));
    }

    [TestMethod]
    public void AuthorizeAsync_AnswersNothingThatCouldOpenTheMainScreens()
    {
        // The authorisation is a verdict about configuration and carries no route: no member of the gate or of its
        // answer has a launch outcome in it, so a caller cannot read the shell out of an unlocked setup (FR-007).
        var gateTypes = new[] { typeof(MachineSetupGate), typeof(IMachineSetupGate), typeof(MachineSetupAuthorization) };

        var carried = gateTypes
            .SelectMany(type => type
                .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Select(member => member switch
                {
                    PropertyInfo property => property.PropertyType,
                    FieldInfo field => field.FieldType,
                    MethodInfo method => method.ReturnType,
                    _ => typeof(object),
                }))
            .ToArray();

        Assert.AreEqual(
            0,
            carried.Count(type => type == typeof(LaunchOutcome) || type == typeof(ILaunchPipeline)),
            "the gate answers an authorisation, not a surface: it cannot route anybody to the shell");
    }

    [TestMethod]
    public async Task AuthorizeAsync_WhenThePersonDoesNotHoldThePermission_RefusesAndStatesWhy()
    {
        var stub = new StubMySqlHelperServer()
            .Returns(CredentialProcedure, AccountRow("material_handler"))
            .Returns(PermissionsProcedure, PermissionRow(isHeld: false));

        var authorization = await CreateGate(stub).AuthorizeAsync(SignInName, Credential, CancellationToken.None);

        Assert.IsFalse(authorization.IsAuthorized, "the permission key is what authorises, not the name (FR-007)");
        Assert.AreEqual(MachineSetupRefusals.NotPermitted, authorization.RefusalReason);
        Assert.IsNull(authorization.Person, "a refusal carries no person");
    }

    [TestMethod]
    public async Task AuthorizeAsync_WhenTheStoreHoldsNoRowForTheKey_FallsBackToRefusing()
    {
        // Every declared key falls back to its shipped answer when the store holds no row for it, and this key's
        // fallback is false. A missing baseline therefore refuses rather than unlocking setup.
        var stub = new StubMySqlHelperServer()
            .Returns(CredentialProcedure, AccountRow("developer"));

        var authorization = await CreateGate(stub).AuthorizeAsync(SignInName, Credential, CancellationToken.None);

        Assert.IsFalse(authorization.IsAuthorized);
        Assert.AreEqual(MachineSetupRefusals.NotPermitted, authorization.RefusalReason);
    }

    [TestMethod]
    public async Task AuthorizeAsync_WhenTheCredentialDoesNotMatch_RefusesBeforeReadingAnyPermission()
    {
        var stub = new StubMySqlHelperServer()
            .Returns(CredentialProcedure, AccountRow("developer"));

        var authorization = await CreateGate(stub).AuthorizeAsync(SignInName, "0000", CancellationToken.None);

        Assert.IsFalse(authorization.IsAuthorized);
        Assert.AreEqual(MachineSetupRefusals.CredentialRefused, authorization.RefusalReason);
        Assert.AreEqual(0, stub.CallCount(PermissionsProcedure), "a credential that did not match is never followed by a permission read");
    }

    [TestMethod]
    public async Task AuthorizeAsync_WhenTheStoreHoldsNoSuchSignInName_RefusesAsThoughTheCredentialWereWrong()
    {
        var stub = new StubMySqlHelperServer();

        var authorization = await CreateGate(stub).AuthorizeAsync(SignInName, Credential, CancellationToken.None);

        Assert.IsFalse(authorization.IsAuthorized);
        Assert.AreEqual(MachineSetupRefusals.CredentialRefused, authorization.RefusalReason);
        Assert.AreEqual(0, stub.CallCount(PermissionsProcedure));
    }

    [TestMethod]
    public async Task AuthorizeAsync_WithAMixedCaseSignInName_SendsItUpperCasedToTheStore()
    {
        // Sign-in names are stored and compared upper case (FR-002), so the gate normalises before it asks.
        var stub = new StubMySqlHelperServer();

        await CreateGate(stub).AuthorizeAsync("  jkoll  ", Credential, CancellationToken.None);

        Assert.AreEqual("JKOLL", stub.Last(CredentialProcedure).Parameters["p_username"]);
        Assert.AreEqual(MySqlDatabaseTarget.MtmWaitlist, stub.Last(CredentialProcedure).Target);
    }

    [TestMethod]
    public async Task AuthorizeAsync_WhenTheStoreCannotAnswer_RefusesRatherThanAuthorising()
    {
        var stub = new StubMySqlHelperServer()
            .Fails(CredentialProcedure, new InvalidOperationException("the store did not answer"));

        var authorization = await CreateGate(stub).AuthorizeAsync(SignInName, Credential, CancellationToken.None);

        Assert.IsFalse(authorization.IsAuthorized, "a check that could not be made has confirmed nothing");
        Assert.AreEqual(MachineSetupRefusals.StoreUnreadable, authorization.RefusalReason);
    }

    [TestMethod]
    public async Task AuthorizeAsync_WhenTheStoreAnsweredTheSaltAsText_StillConfirmsTheCredential()
    {
        // The store holds the salt as VARBINARY, and a provider may hand it back as bytes or as the base64 text of
        // them. Both are the same salt, and a salt that could not be read is not a credential that did not match.
        var stub = new StubMySqlHelperServer()
            .Returns(CredentialProcedure, AccountRow("developer", saltAsText: true))
            .Returns(PermissionsProcedure, PermissionRow(isHeld: true));

        var authorization = await CreateGate(stub).AuthorizeAsync(SignInName, Credential, CancellationToken.None);

        Assert.IsTrue(authorization.IsAuthorized);
    }

    [TestMethod]
    public async Task AuthorizeAsync_WhenNothingWasGiven_RefusesWithoutReadingTheStore()
    {
        var stub = new StubMySqlHelperServer();

        var withoutName = await CreateGate(stub).AuthorizeAsync("   ", Credential, CancellationToken.None);
        var withoutCredential = await CreateGate(stub).AuthorizeAsync(SignInName, string.Empty, CancellationToken.None);

        Assert.AreEqual(MachineSetupRefusals.SignInRequired, withoutName.RefusalReason);
        Assert.AreEqual(MachineSetupRefusals.SignInRequired, withoutCredential.RefusalReason);
        Assert.AreEqual(0, stub.TotalCallCount, "an incomplete sign-in is not worth a store read");
    }

    /// <summary>The gate under test, over the store seam the test owns.</summary>
    private static IMachineSetupGate CreateGate(IMySqlHelperServer stub) => new MachineSetupGate(stub);

    /// <summary>One account row, carrying the credential the hasher would have written for the password.</summary>
    private static Dictionary<string, object?> AccountRow(string roleCode, bool saltAsText = false) => new()
    {
        ["id"] = UserId,
        ["role_code"] = roleCode,
        ["display_name"] = "J. Koll",
        ["employee_identifier"] = "1007",
        ["password_hash"] = PasswordSecretHasher.Hash(Credential, s_salt),
        ["password_salt"] = saltAsText ? Convert.ToBase64String(s_salt) : s_salt,
    };

    /// <summary>One stored permission row for the machine-configuration key, at the role's own scope.</summary>
    private static Dictionary<string, object?> PermissionRow(bool isHeld) => new()
    {
        ["scope_type"] = "role",
        ["setting_key"] = PermissionKeys.SettingsMachineConfiguration,
        ["setting_value_bool"] = isHeld ? 1L : 0L,
    };

    /// <summary>
    /// Records what it was asked for and answers with the rows it was built with, so the seam can be read back
    /// without a store. A procedure with no configured answer returns nothing.
    /// </summary>
    private sealed class StubMySqlHelperServer : IMySqlHelperServer
    {
        private readonly List<Call> _calls = [];
        private readonly Dictionary<string, Queue<IReadOnlyList<Dictionary<string, object?>>>> _rows = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Exception> _failures = new(StringComparer.Ordinal);

        public int TotalCallCount => _calls.Count;

        public StubMySqlHelperServer Returns(string procedure, params Dictionary<string, object?>[] rows)
        {
            if (!_rows.TryGetValue(procedure, out var queue))
            {
                queue = new Queue<IReadOnlyList<Dictionary<string, object?>>>();
                _rows[procedure] = queue;
            }

            queue.Enqueue(rows);

            return this;
        }

        public StubMySqlHelperServer Fails(string procedure, Exception exception)
        {
            _failures[procedure] = exception;

            return this;
        }

        public Call Last(string procedure) => _calls.Last(call => call.Procedure == procedure);

        public int CallCount(string procedure) => _calls.Count(call => call.Procedure == procedure);

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            _calls.Add(new Call(storedProcedureName, parameters, databaseTarget));

            if (_failures.TryGetValue(storedProcedureName, out var failure))
            {
                return Task.FromException<IReadOnlyList<Dictionary<string, object?>>>(failure);
            }

            IReadOnlyList<Dictionary<string, object?>> rows =
                _rows.TryGetValue(storedProcedureName, out var queue) && queue.Count > 0
                    ? queue.Dequeue()
                    : [];

            return Task.FromResult(rows);
        }

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("The gate writes nothing, so no non-query is expected.");

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("No statement text is expected from this gate (constitution III).");

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("No statement text is expected from this gate (constitution III).");

        public sealed record Call(
            string Procedure,
            IReadOnlyDictionary<string, object?> Parameters,
            MySqlDatabaseTarget Target);
    }
}
