using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Startup.Services;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// The identifier stage's promises (`contracts/identifier-signin-contract.md` §§1-3; FR-002, FR-003, FR-004,
/// FR-006, FR-012, FR-013): exactly four digits is an employee number and nothing else is, the store is asked
/// first, an inactive account ends resolution without offering set-up, a number the store does not hold is put to
/// the plant system, and an unreachable plant system with an empty mirror says so rather than guessing.
/// </summary>
/// <remarks>
/// The store directory, the credential read and the plant system are doubles, so what is proved is the order the
/// questions are asked in and the outcome each answer produces, rather than any database's behaviour. The store's
/// own ordering — active account first when two people share a number — is proved separately, against the
/// procedure's shipped text, because that ordering is the store's and must not be re-implemented here.
/// </remarks>
[TestClass]
public sealed class IdentifierSignInServiceTests
{
    [TestMethod]
    [DataRow("4330", IdentifierKind.EmployeeNumber, DisplayName = "four digits is an employee number")]
    [DataRow("6229", IdentifierKind.EmployeeNumber, DisplayName = "leading zeros are still four digits")]
    [DataRow("  4330  ", IdentifierKind.EmployeeNumber, DisplayName = "surrounding whitespace is trimmed first")]
    [DataRow("123", IdentifierKind.UserName, DisplayName = "three digits is not")]
    [DataRow("12345", IdentifierKind.UserName, DisplayName = "five digits is not")]
    [DataRow("12 3", IdentifierKind.UserName, DisplayName = "a space among the digits is not")]
    [DataRow("123a", IdentifierKind.UserName, DisplayName = "a letter among the digits is not")]
    [DataRow("433a", IdentifierKind.UserName, DisplayName = "a trailing letter is not")]
    [DataRow("johnk", IdentifierKind.UserName, DisplayName = "a name is a user name")]
    [DataRow("", IdentifierKind.UserName, DisplayName = "blank is a user name, never an employee number")]
    public void Service_IdentifierSignInServiceTests_Classify_AnswersTheWholeShapeAndNothingElse(string identifier, IdentifierKind expected)
        => Assert.AreEqual(expected, IdentifierSignInService.Classify(identifier));

    [TestMethod]
    public async Task Service_IdentifierSignInServiceTests_ResolveAsync_BlankIdentifierIsRefusedBeforeAnythingIsAsked()
    {
        var world = new World();

        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => new IdentifierSignInService(world.Directory, world.CredentialCheck, world.Plant).ResolveAsync("   "));

        Assert.AreEqual(0, world.Store.CallCount, "a blank identifier is refused by the field's own hint (FR-015)");
        Assert.AreEqual(0, world.Plant.CallCount, "and the plant system is not asked either");
    }

    [TestMethod]
    public async Task Service_IdentifierSignInServiceTests_ResolveAsync_UserNameTheStoreHoldsResolvesInStoreForTheCredentialStage()
    {
        var world = new World().WithAccount(42);
        var service = new IdentifierSignInService(world.Directory, world.CredentialCheck, world.Plant);

        var result = await service.ResolveAsync("johnk");

        Assert.AreEqual(IdentifierSignInOutcome.ResolvedInStore, result.Outcome);
        Assert.AreEqual(IdentifierKind.UserName, result.Kind);
        Assert.AreEqual("johnk", result.SignInName);
        Assert.IsNull(result.EmployeeNumber);
        Assert.AreEqual(1, world.Store.CallCount, "one store read answers a user name");
        Assert.AreEqual(0, world.Plant.CallCount, "the plant system is never asked about a user name (FR-003)");
    }

    [TestMethod]
    public async Task Service_IdentifierSignInServiceTests_ResolveAsync_UserNameTheStoreDoesNotHoldIsRefusedInPlainWords()
    {
        var world = new World();
        var service = new IdentifierSignInService(world.Directory, world.CredentialCheck, world.Plant);

        var result = await service.ResolveAsync("nobody");

        Assert.AreEqual(IdentifierSignInOutcome.Unknown, result.Outcome);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Reason), "a refusal says why in plain words (FR-012)");
        Assert.AreEqual(0, world.Plant.CallCount, "a user-name miss is not put to the plant system (FR-004)");
    }

    [TestMethod]
    public async Task Service_IdentifierSignInServiceTests_ResolveAsync_EmployeeNumberTheStoreHoldsResolvesToTheAccountsOwnSignInName()
    {
        var world = new World().WithEmployee("6229", userName: "JKOLL", isActive: true);
        var service = new IdentifierSignInService(world.Directory, world.CredentialCheck, world.Plant);

        var result = await service.ResolveAsync("6229");

        Assert.AreEqual(IdentifierSignInOutcome.ResolvedInStore, result.Outcome);
        Assert.AreEqual(IdentifierKind.EmployeeNumber, result.Kind);
        Assert.AreEqual("6229", result.EmployeeNumber);
        Assert.AreEqual(
            "JKOLL",
            result.SignInName,
            "the credential check is keyed on the account's own sign-in name, not the four digits");
        Assert.AreEqual(0, world.Plant.CallCount, "the store held the number, so the plant system is not asked");
    }

    [TestMethod]
    public async Task Service_IdentifierSignInServiceTests_ResolveAsync_EmployeeNumberTheStoreHoldsInactiveEndsResolutionWithoutOfferingSetUp()
    {
        // The plant system also knows this number, which is the case the clarification names: an inactive store
        // account must not become a second account.
        var world = new World().WithEmployee("6229", userName: "JKOLL", isActive: false)
            .WithPlantEmployee("6229", "John", "Koll");
        var service = new IdentifierSignInService(world.Directory, world.CredentialCheck, world.Plant);

        var result = await service.ResolveAsync("6229");

        Assert.AreEqual(IdentifierSignInOutcome.NotActive, result.Outcome);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Reason), "the person is told they are not active");
        Assert.IsNull(result.FirstName, "no set-up panel is offered for a number the store already holds");
        Assert.AreEqual(0, world.Plant.CallCount, "the plant system is not consulted for an inactive account");
    }

    [TestMethod]
    public async Task Service_IdentifierSignInServiceTests_ResolveAsync_EmployeeNumberOnlyThePlantSystemHoldsOffersSetUpWithTheNameItAnswered()
    {
        var world = new World().WithPlantEmployee("6229", "John", "Koll");
        var service = new IdentifierSignInService(world.Directory, world.CredentialCheck, world.Plant);

        var result = await service.ResolveAsync("6229");

        Assert.AreEqual(IdentifierSignInOutcome.KnownToPlantSystemOnly, result.Outcome);
        Assert.AreEqual("6229", result.EmployeeNumber);
        Assert.AreEqual("John", result.FirstName);
        Assert.AreEqual("Koll", result.LastName);
        Assert.AreEqual(1, world.Plant.CallCount);
    }

    [TestMethod]
    public async Task Service_IdentifierSignInServiceTests_ResolveAsync_EmployeeNumberInNeitherSourceIsRefusedAndThePlantSystemWasReadable()
    {
        var world = new World().WithPlantNotHeld();
        var service = new IdentifierSignInService(world.Directory, world.CredentialCheck, world.Plant);

        var result = await service.ResolveAsync("6229");

        Assert.AreEqual(IdentifierSignInOutcome.Unknown, result.Outcome);
        StringAssert.Contains(result.Reason, "Neither the store nor the plant system");
    }

    [TestMethod]
    public async Task Service_IdentifierSignInServiceTests_ResolveAsync_PlantSystemUnreachableAndMirrorNeverHeldTheNumberSaysItCannotBeDoneNow()
    {
        var world = new World().WithPlantUnreadable();
        var service = new IdentifierSignInService(world.Directory, world.CredentialCheck, world.Plant);

        var result = await service.ResolveAsync("6229");

        Assert.AreEqual(IdentifierSignInOutcome.Unknown, result.Outcome);
        StringAssert.Contains(
            result.Reason,
            "cannot be completed right now",
            "FR-013: an unreachable plant system with an empty mirror is not the same answer as \"no such employee\"");
        Assert.IsNull(result.FirstName, "no name is invented for a person nobody could confirm");
        Assert.IsNull(result.LastName);
    }

    [TestMethod]
    public async Task Service_IdentifierSignInServiceTests_ResolveAsync_TwoPeopleShareTheNumberResolvesTheStoreAnswerItWasGiven()
    {
        // The active-first ordering is the store's, inside the procedure. This proves the service does not
        // re-decide it: whichever account the store answered is the account that is signed in.
        var world = new World().WithEmployee("6229", userName: "JKOLL", isActive: true);
        var service = new IdentifierSignInService(world.Directory, world.CredentialCheck, world.Plant);

        var result = await service.ResolveAsync("6229");

        Assert.AreEqual(IdentifierSignInOutcome.ResolvedInStore, result.Outcome);
        Assert.AreEqual("JKOLL", result.SignInName);
    }

    [TestMethod]
    public void Service_IdentifierSignInServiceTests_TheStoresEmployeeNumberRead_OrdersTheActiveAccountFirst()
    {
        // The ordering the clarification calls for lives in the procedure, not in application code, so this
        // proves the shipped text carries it rather than the service re-sorting what it is given.
        var script = File.ReadAllText(RepoPath(
            "Database", "StoredProcedures", "sp_core_employee_by_identifier_get", "create.sql"));

        StringAssert.Contains(script, "ORDER BY u.is_active DESC");
        StringAssert.Contains(script, "employee_identifier = p_employee_identifier");
        StringAssert.Contains(script, "username_normalized", "the read carries the account's own sign-in name");
    }

    [TestMethod]
    public void Service_IdentifierSignInServiceTests_TheStoresEmployeeNumberRead_ShipsItsRollback()
    {
        // The schema-artifact rule (constitution III): every artifact ships create and rollback in the same change.
        Assert.IsTrue(File.Exists(RepoPath("Database", "StoredProcedures", "sp_core_employee_by_identifier_get", "rollback.sql")));
    }

    private static string RepoPath(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MTM_Waitlist.sln")))
        {
            directory = directory.Parent;
        }

        Assert.IsNotNull(directory, "the repository root could not be found from the test output directory");

        return Path.Combine([directory!.FullName, .. parts]);
    }

    /// <summary>One arranged world: the store, the plant system and the answers each was told to give.</summary>
    private sealed class World
    {
        private readonly List<Dictionary<string, object?>> _accountRows = [];
        private EmployeeIdentity? _employee;
        private PlantEmployeeLookup _plant = PlantEmployeeLookup.NotHeld;

        public StubStore Store { get; } = new();

        public StubDirectory Directory { get; } = new();

        public StubPlant Plant { get; } = new();

        public CredentialCheckService CredentialCheck => new(Store);

        public World WithAccount(long userId)
        {
            Store.UserId = userId;

            return this;
        }

        public World WithEmployee(string employeeNumber, string userName, bool isActive)
        {
            _employee = new EmployeeIdentity
            {
                EmployeeNumber = employeeNumber,
                DisplayName = "John Koll",
                IsActive = isActive,
                SignInName = userName,
            };

            Directory.Answer = _employee;

            return this;
        }

        public World WithPlantEmployee(string employeeNumber, string firstName, string lastName)
        {
            _plant = PlantEmployeeLookup.Found(new PlantEmployee(employeeNumber, firstName, lastName));
            Plant.Answer = _plant;

            return this;
        }

        public World WithPlantNotHeld()
        {
            _plant = PlantEmployeeLookup.NotHeld;
            Plant.Answer = _plant;

            return this;
        }

        public World WithPlantUnreadable()
        {
            _plant = PlantEmployeeLookup.Unreadable;
            Plant.Answer = _plant;

            return this;
        }
    }

    /// <summary>The store seam, recording what it was asked and answering the credential read's one row.</summary>
    private sealed class StubStore : IMySqlHelperServer
    {
        /// <summary>The account identifier the credential read answers with, or zero for no account.</summary>
        public long UserId { get; set; }

        public int CallCount { get; private set; }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            IReadOnlyList<Dictionary<string, object?>> rows = UserId > 0
                ? [new Dictionary<string, object?> { ["id"] = UserId }]
                : [];

            return Task.FromResult(rows);
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

    /// <summary>The store's account records, answering the employee-identifier read.</summary>
    private sealed class StubDirectory : IEmployeeDirectoryService
    {
        public EmployeeIdentity? Answer { get; set; }

        public int CallCount { get; private set; }

        public Task<EmployeeIdentity?> FindByEmployeeIdentifierAsync(
            string employeeIdentifier,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            return Task.FromResult(Answer);
        }
    }

    /// <summary>The plant system, answering the sixth read shape's contract.</summary>
    private sealed class StubPlant : IPlantEmployeeReader
    {
        public PlantEmployeeLookup Answer { get; set; } = PlantEmployeeLookup.NotHeld;

        public int CallCount { get; private set; }

        public Task<PlantEmployeeLookup> FindByNumberAsync(
            string employeeNumber,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            return Task.FromResult(Answer);
        }
    }
}
