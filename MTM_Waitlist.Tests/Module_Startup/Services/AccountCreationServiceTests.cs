using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Startup.Services;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// The self set-up creation path (`contracts/account-creation-contract.md`; FR-008, FR-009, FR-010, FR-034,
/// FR-045, SC-011): profile, credential and rank are written by one call, the rank is never named by the caller,
/// the PIN is hashed before the store sees anything, and a failure part-way leaves nothing behind.
/// </summary>
/// <remarks>
/// The store seam is a double, so what is proved is what the call sends and what it does when the store refuses:
/// the atomicity itself is the procedure's, and this proves the caller makes exactly one call so there is nothing
/// for a failure to leave half-written.
/// </remarks>
[TestClass]
public sealed class AccountCreationServiceTests
{
    private const string CreateProcedure = "sp_core_account_setup_create";

    private static AccountSetupDraft Draft(
        string employeeNumber = "6229",
        string firstName = "John",
        string lastName = "Koll",
        string pin = "2468",
        string confirmation = "2468")
        => new(employeeNumber, firstName, lastName, pin, confirmation);

    [TestMethod]
    public async Task Service_AccountCreationServiceTests_CreateAsync_CreatedWritesOneCallAndAnswersTheStoreRow()
    {
        var store = new StubStore
        {
            Rows =
            [
                new Dictionary<string, object?>
                {
                    ["user_id"] = 42L,
                    ["username_normalized"] = "6229",
                    ["display_name"] = "John Koll",
                    ["employee_identifier"] = "6229",
                    ["role_code"] = "production",
                },
            ],
        };
        var service = new AccountCreationService(store);

        var result = await service.CreateAsync(Draft());

        Assert.IsTrue(result.IsCreated);
        Assert.AreEqual("6229", result.SignInName, "the account's own sign-in name is what the store answered");
        Assert.AreEqual("John Koll", result.DisplayName);
        Assert.AreEqual("6229", result.EmployeeNumber);
        Assert.AreEqual("production", result.RoleCode, "the rank is whatever the store's own list chose");
        Assert.AreEqual(1, store.CallCount, "profile, credential and rank travel in one call (SC-011, FR-034)");
        Assert.AreEqual(CreateProcedure, store.LastProcedure);
        Assert.AreEqual(MySqlDatabaseTarget.MtmWaitlist, store.LastTarget);
    }

    [TestMethod]
    public async Task Service_AccountCreationServiceTests_CreateAsync_WritesTheAccountUnderTheEmployeeNumberAndANonTemporaryCredential()
    {
        var store = new StubStore
        {
            Rows = [new Dictionary<string, object?> { ["username_normalized"] = "6229", ["role_code"] = "production" }],
        };

        await new AccountCreationService(store).CreateAsync(Draft());

        Assert.AreEqual("6229", store.LastParameters["p_username"], "the person signs in with the number they typed");
        Assert.AreEqual("6229", store.LastParameters["p_employee_identifier"]);
        Assert.AreEqual("John", store.LastParameters["p_first_name"]);
        Assert.AreEqual("Koll", store.LastParameters["p_last_name"]);
        Assert.IsFalse(
            store.LastParameters.ContainsKey("p_is_temporary"),
            "the procedure writes a non-temporary credential itself, so there is no flag to get wrong (FR-009)");
        Assert.IsFalse(
            store.LastParameters.ContainsKey("p_role_code") || store.LastParameters.ContainsKey("p_rank"),
            "no rank may be named by the caller (FR-010, FR-039)");
    }

    [TestMethod]
    public async Task Service_AccountCreationServiceTests_CreateAsync_NeverSendsThePlainPin()
    {
        var store = new StubStore
        {
            Rows = [new Dictionary<string, object?> { ["username_normalized"] = "6229" }],
        };

        await new AccountCreationService(store).CreateAsync(Draft(pin: "2468"));

        foreach (var value in store.LastParameters.Values)
        {
            Assert.AreNotEqual("2468", value as string, "the PIN is hashed before the store sees anything");
        }

        Assert.IsNotNull(store.LastParameters["p_password_hash"]);
        Assert.IsNotNull(store.LastParameters["p_password_salt"]);
    }

    [TestMethod]
    [DataRow("123", DisplayName = "three characters is too short")]
    [DataRow("", DisplayName = "nothing is not a PIN")]
    public async Task Service_AccountCreationServiceTests_CreateAsync_PinShorterThanTheFloorWritesNothing(string pin)
    {
        var store = new StubStore();
        var service = new AccountCreationService(store);

        var result = await service.CreateAsync(Draft(pin: pin, confirmation: pin));

        Assert.IsFalse(result.IsCreated);
        StringAssert.Contains(result.FailureReason, "PIN");
        Assert.AreEqual(0, store.CallCount, "a refused PIN never reaches the store");
    }

    [TestMethod]
    public async Task Service_AccountCreationServiceTests_CreateAsync_PinTypedTwoDifferentWaysWritesNothing()
    {
        var store = new StubStore();

        var result = await new AccountCreationService(store).CreateAsync(Draft(pin: "2468", confirmation: "2469"));

        Assert.IsFalse(result.IsCreated);
        StringAssert.Contains(result.FailureReason, "do not match");
        Assert.AreEqual(0, store.CallCount);
    }

    [TestMethod]
    public async Task Service_AccountCreationServiceTests_CreateAsync_BlankConfirmedNameWritesNothing()
    {
        var store = new StubStore();

        var result = await new AccountCreationService(store).CreateAsync(Draft(firstName: "   "));

        Assert.IsFalse(result.IsCreated);
        Assert.AreEqual(0, store.CallCount);
    }

    [TestMethod]
    public async Task Service_AccountCreationServiceTests_CreateAsync_WhenTheStoreRefusesLeavesNothingBehindAndSaysSo()
    {
        var store = new StubStore { Throws = new InvalidOperationException("duplicate sign-in name") };
        var service = new AccountCreationService(store);

        var result = await service.CreateAsync(Draft());

        Assert.IsFalse(result.IsCreated);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailureReason), "the person is told nothing was written");
        Assert.IsNull(result.SignInName);
        Assert.AreEqual(1, store.CallCount, "one call, so there is one transaction and nothing to leave half-written");
    }

    [TestMethod]
    public async Task Service_AccountCreationServiceTests_CreateAsync_WhenTheStoreAnswersNoRowReportsNothingCreated()
    {
        var store = new StubStore();

        var result = await new AccountCreationService(store).CreateAsync(Draft());

        Assert.IsFalse(result.IsCreated, "a creation that cannot be confirmed is not reported as a creation");
        Assert.AreEqual(1, store.CallCount);
    }

    /// <summary>The store seam, recording the one call and answering what the test stated.</summary>
    private sealed class StubStore : IMySqlHelperServer
    {
        /// <summary>The rows the creation procedure answers with.</summary>
        public IReadOnlyList<Dictionary<string, object?>> Rows { get; set; } = [];

        /// <summary>An exception the store throws instead of answering, or <c>null</c>.</summary>
        public Exception? Throws { get; set; }

        public int CallCount { get; private set; }

        public string? LastProcedure { get; private set; }

        public IReadOnlyDictionary<string, object?> LastParameters { get; private set; } =
            new Dictionary<string, object?>();

        public MySqlDatabaseTarget? LastTarget { get; private set; }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastProcedure = storedProcedureName;
            LastParameters = parameters;
            LastTarget = databaseTarget;

            if (Throws is not null)
            {
                return Task.FromException<IReadOnlyList<Dictionary<string, object?>>>(Throws);
            }

            return Task.FromResult(Rows);
        }

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Dictionary<string, object?>>>([]);

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult(0);
    }
}
