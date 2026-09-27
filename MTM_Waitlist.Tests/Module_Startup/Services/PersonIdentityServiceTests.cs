using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Startup.Services;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// The person identity contract's own promises (`contracts/identity-contracts.md`; FR-011): a person resolved from
/// the store carries the name, the identifier and the role in force, the held-role set answers case-insensitively
/// whatever form the store or the caller used, the role in force is always held, and ending the identity really
/// reports nobody signed in.
/// </summary>
/// <remarks>
/// <para>
/// The held-role set is what permissions are answered from, so the code has to mean one thing in every spelling:
/// <c>role:developer</c>, <c>ROLE:Developer</c> and <c>developer</c> are the same role, and a person is never
/// answered as not holding the role the store put them in.
/// </para>
/// <para>
/// The identity is never persisted on the machine, so nothing here needs a file or a fixture: every case is the
/// service and a store that answers what the case is about.
/// </para>
/// </remarks>
[TestClass]
public sealed class PersonIdentityServiceTests
{
    private const string IdentityProcedure = "sp_auth_user_row_get";

    [TestMethod]
    public void ANewService_ReportsNobodySignedIn()
    {
        var person = new PersonIdentityService(new StubMySqlHelperServer([]));

        Assert.IsFalse(person.IsSignedIn);
        Assert.AreEqual(0L, person.UserId);
        Assert.AreEqual(string.Empty, person.SignInName);
        Assert.AreEqual(string.Empty, person.DisplayName);
        Assert.IsNull(person.EmployeeNumber);
        Assert.AreEqual(string.Empty, person.CurrentRoleCode);
        Assert.AreEqual(0, person.HeldRoleCodes.Count);
        Assert.IsFalse(person.Holds("developer"));
    }

    [TestMethod]
    [DataRow("role:developer", "developer", DisplayName = "the scope-key prefix is removed")]
    [DataRow("ROLE:Developer", "Developer", DisplayName = "the prefix is matched whatever its case")]
    [DataRow("  role:admin  ", "admin", DisplayName = "the prefix is found after trimming")]
    [DataRow("developer", "developer", DisplayName = "a bare code is left alone")]
    [DataRow("", "", DisplayName = "empty stays empty")]
    [DataRow("   ", "", DisplayName = "blank collapses to empty")]
    [DataRow(null, "", DisplayName = "no role at all")]
    public void NormalizeRoleCode_AnswersOneCodeForEverySpelling(string? input, string expected)
    {
        Assert.AreEqual(expected, PersonIdentityService.NormalizeRoleCode(input));
    }

    [TestMethod]
    public void Holds_AnswersTheSameForEverySpellingOfAHeldRole()
    {
        var person = new PersonIdentityService(new StubMySqlHelperServer([]));

        person.Apply(
            userId: 42L,
            signInName: "JOHN",
            displayName: "John K",
            employeeNumber: "E-1042",
            currentRoleCode: "role:developer",
            heldRoleCodes: ["role:Developer", "Setup Lead"]);

        Assert.IsTrue(person.Holds("developer"));
        Assert.IsTrue(person.Holds("DEVELOPER"));
        Assert.IsTrue(person.Holds("role:developer"));
        Assert.IsTrue(person.Holds("ROLE:Developer"));
        Assert.IsTrue(person.Holds("setup lead"), "the second held role is held too");
        Assert.IsFalse(person.Holds("plant manager"));
        Assert.IsFalse(person.Holds(string.Empty), "nothing is held when nothing is asked for");
    }

    [TestMethod]
    public void Apply_AlwaysHoldsTheRoleInForce_EvenWithNoSet()
    {
        var person = new PersonIdentityService(new StubMySqlHelperServer([]));

        person.Apply(42L, "JOHN", "John K", null, "developer");

        // A person is never answered as not holding the role the store put them in, so the role in force joins
        // the set even when the caller passed none.
        Assert.IsTrue(person.Holds("developer"));
        Assert.AreEqual(1, person.HeldRoleCodes.Count);
    }

    [TestMethod]
    public void Apply_KeepsTheHeldSetToDistinctCodes()
    {
        var person = new PersonIdentityService(new StubMySqlHelperServer([]));

        person.Apply(42L, "JOHN", "John K", null, "developer", ["developer", "role:developer", "DEVELOPER"]);

        Assert.AreEqual(1, person.HeldRoleCodes.Count);
    }

    [TestMethod]
    public void Apply_TrimsWhatItIsGivenAndBlanksWhatTheStoreDoesNotHold()
    {
        var person = new PersonIdentityService(new StubMySqlHelperServer([]));

        person.Apply(42L, "  JOHN  ", "  John K  ", "   ", "  developer  ");

        Assert.AreEqual("JOHN", person.SignInName);
        Assert.AreEqual("John K", person.DisplayName);
        Assert.AreEqual("developer", person.CurrentRoleCode);
        Assert.IsNull(person.EmployeeNumber, "a blank identifier means the store holds none");
    }

    [TestMethod]
    public void Clear_EndsTheIdentity()
    {
        var person = new PersonIdentityService(new StubMySqlHelperServer([]));
        person.Apply(42L, "JOHN", "John K", "E-1042", "developer", ["setup lead"]);

        person.Clear();

        Assert.IsFalse(person.IsSignedIn);
        Assert.AreEqual(0L, person.UserId);
        Assert.AreEqual(string.Empty, person.SignInName);
        Assert.AreEqual(string.Empty, person.DisplayName);
        Assert.IsNull(person.EmployeeNumber);
        Assert.AreEqual(string.Empty, person.CurrentRoleCode);
        Assert.AreEqual(0, person.HeldRoleCodes.Count);
        Assert.IsFalse(person.Holds("developer"));
        Assert.IsFalse(person.Holds("setup lead"));
    }

    [TestMethod]
    public async Task ResolveAsync_BlankSignInName_EndsAnyIdentityWithoutReadingTheStore()
    {
        var stub = new StubMySqlHelperServer([]);
        var person = new PersonIdentityService(stub);
        person.Apply(42L, "JOHN", "John K", "E-1042", "developer", ["setup lead"]);

        Assert.IsFalse(await person.ResolveAsync("   "));

        // There is no person to look up, so the store is not asked: a read with nothing to match on would only
        // be a guess at which row the caller meant.
        Assert.AreEqual(0, stub.QueryCallCount);

        // And the previous person does not survive an answer of "nobody", which is the same answer the store's
        // empty read gives: resolving nothing must never leave somebody signed in.
        Assert.IsFalse(person.IsSignedIn);
        Assert.AreEqual(string.Empty, person.SignInName);
        Assert.AreEqual(0, person.HeldRoleCodes.Count);
        Assert.IsFalse(person.Holds("developer"));
        Assert.IsFalse(person.Holds("setup lead"));
    }

    [TestMethod]
    public async Task ResolveAsync_Row_AppliesThePersonTheStoreReports()
    {
        var stub = new StubMySqlHelperServer([PersonRow("role:developer")]);
        var person = new PersonIdentityService(stub);

        var resolved = await person.ResolveAsync("  john  ");

        Assert.IsTrue(resolved);
        Assert.AreEqual(IdentityProcedure, stub.LastStoredProcedureName);
        Assert.AreEqual(MySqlDatabaseTarget.MtmWaitlist, stub.LastDatabaseTarget);
        Assert.AreEqual("john", stub.LastParameters["p_username"], "the name is matched trimmed");
        Assert.AreEqual(1, stub.QueryCallCount);

        Assert.IsTrue(person.IsSignedIn);
        Assert.AreEqual(42L, person.UserId);
        Assert.AreEqual("john", person.SignInName, "the resolved name is the one that was matched");
        Assert.AreEqual("John K", person.DisplayName);
        Assert.AreEqual("E-1042", person.EmployeeNumber);
        Assert.AreEqual("role:developer", person.CurrentRoleCode);
        Assert.IsTrue(person.Holds("developer"), "the prefix the store returned is not part of the held code");
    }

    [TestMethod]
    public async Task ResolveAsync_RowWithoutAnEmployeeIdentifier_LeavesItUnset()
    {
        var row = PersonRow("developer");
        row.Remove("employee_identifier");

        var person = new PersonIdentityService(new StubMySqlHelperServer([row]));

        Assert.IsTrue(await person.ResolveAsync("john"));
        Assert.IsNull(person.EmployeeNumber);
    }

    [TestMethod]
    public async Task ResolveAsync_NoRow_EndsAnyIdentityAndAnswersFalse()
    {
        var stub = new StubMySqlHelperServer([]);
        var person = new PersonIdentityService(stub);
        person.Apply(42L, "JOHN", "John K", "E-1042", "developer", ["setup lead"]);

        var resolved = await person.ResolveAsync("JOHN");

        // The store answered that it holds nobody by that name, so the previous identity must not survive the
        // answer: a person the store no longer has is not signed in.
        Assert.IsFalse(resolved);
        Assert.AreEqual(1, stub.QueryCallCount);
        Assert.IsFalse(person.IsSignedIn);
        Assert.AreEqual(0, person.HeldRoleCodes.Count);
        Assert.IsFalse(person.Holds("setup lead"));
    }

    /// <summary>One person row as the store returns it, in the column names the mapping reads.</summary>
    private static Dictionary<string, object?> PersonRow(string roleCode) => new()
    {
        ["id"] = 42L,
        ["display_name"] = "John K",
        ["employee_identifier"] = "E-1042",
        ["role_code"] = roleCode,
    };

    /// <summary>
    /// Records what it was asked for and answers with the rows it was built with, so the seam can be read back
    /// without a database.
    /// </summary>
    private sealed class StubMySqlHelperServer : IMySqlHelperServer
    {
        private readonly IReadOnlyList<Dictionary<string, object?>> _rows;

        public StubMySqlHelperServer(IReadOnlyList<Dictionary<string, object?>> rows) => _rows = rows;

        public int QueryCallCount { get; private set; }

        public string? LastStoredProcedureName { get; private set; }

        public IReadOnlyDictionary<string, object?> LastParameters { get; private set; } =
            new Dictionary<string, object?>();

        public MySqlDatabaseTarget? LastDatabaseTarget { get; private set; }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            QueryCallCount++;
            LastStoredProcedureName = storedProcedureName;
            LastParameters = parameters;
            LastDatabaseTarget = databaseTarget;

            return Task.FromResult(_rows);
        }

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult(1);

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_rows);

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult(1);
    }
}
