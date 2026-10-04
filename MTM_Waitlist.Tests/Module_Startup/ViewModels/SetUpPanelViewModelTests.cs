using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Startup.Services;
using MTM_Waitlist.Module_Startup.ViewModels;

namespace MTM_Waitlist.Tests.Module_Startup.ViewModels;

/// <summary>
/// The set-up panel's keyboard behaviour (`contracts/identifier-signin-contract.md` §5; FR-014, FR-015): Enter
/// advances one field at a time and submits only at the end, a blank field shows its own hint rather than doing
/// nothing, and leaving the panel creates nothing.
/// </summary>
[TestClass]
public sealed class SetUpPanelViewModelTests
{
    private static AccountSetupDraft Draft(
        string firstName = "John",
        string lastName = "Koll",
        string pin = "2468",
        string confirmation = "2468")
        => new("6229", firstName, lastName, pin, confirmation);

    [TestMethod]
    public async Task Enter_OnTheFirstName_AdvancesToTheLastName()
    {
        var panel = Build();

        var outcome = await panel.EnterAsync(SetUpField.FirstName, Draft());

        Assert.AreEqual(SetUpField.LastName, outcome.FocusNext);
        Assert.IsFalse(outcome.WasSubmitted);
        Assert.IsNull(panel.Message);
    }

    [TestMethod]
    public async Task Enter_OnABlankField_ShowsItsHintAndStays()
    {
        var panel = Build();

        var outcome = await panel.EnterAsync(SetUpField.LastName, Draft(lastName: "   "));

        Assert.AreEqual(SetUpField.LastName, outcome.FocusNext, "focus stays on the field that needs filling");
        Assert.IsFalse(outcome.WasSubmitted);
        Assert.IsTrue(panel.HasMessage, "FR-015: a blank field shows its hint rather than doing nothing");
        StringAssert.Contains(panel.Message, "last name");
    }

    [TestMethod]
    public async Task Enter_OnThePin_AdvancesToTheConfirmation()
    {
        var panel = Build();

        var outcome = await panel.EnterAsync(SetUpField.Pin, Draft());

        Assert.AreEqual(SetUpField.Confirmation, outcome.FocusNext);
        Assert.IsFalse(outcome.WasSubmitted);
    }

    [TestMethod]
    public async Task Enter_OnABlankPin_ShowsItsHintAndDoesNotSubmit()
    {
        var store = new StubStore();
        var panel = Build(store);

        var outcome = await panel.EnterAsync(SetUpField.Pin, Draft(pin: string.Empty, confirmation: string.Empty));

        Assert.AreEqual(SetUpField.Pin, outcome.FocusNext);
        Assert.IsFalse(outcome.WasSubmitted);
        Assert.AreEqual(0, store.CallCount, "nothing is written while the PIN is still missing");
    }

    [TestMethod]
    public async Task Enter_OnTheConfirmation_SubmitsExactlyAsTheButtonDoes()
    {
        var store = new StubStore
        {
            Rows = [new Dictionary<string, object?> { ["username_normalized"] = "6229", ["role_code"] = "production" }],
        };
        var panel = Build(store);
        AccountSetupResult? created = null;
        panel.AccountCreated += (_, result) => created = result;

        var outcome = await panel.EnterAsync(SetUpField.Confirmation, Draft());

        Assert.IsTrue(outcome.WasSubmitted);
        Assert.IsNull(outcome.FocusNext, "the last field submits rather than moving focus");
        Assert.AreEqual(1, store.CallCount, "Enter submits through the same creation call the button uses");
        Assert.IsNotNull(created);
        Assert.IsTrue(created!.IsCreated);
    }

    [TestMethod]
    public async Task Enter_OnTheConfirmationWithAMismatchedPin_StatesTheReasonAndWritesNothing()
    {
        var store = new StubStore();
        var panel = Build(store);

        await panel.EnterAsync(SetUpField.Confirmation, Draft(pin: "2468", confirmation: "2469"));

        Assert.AreEqual(0, store.CallCount, "a mismatched PIN never reaches the store");
        Assert.IsTrue(panel.HasMessage);
        StringAssert.Contains(panel.Message, "do not match");
    }

    [TestMethod]
    public void LeavingThePanel_CreatesNothingAndRaisesCancelled()
    {
        var store = new StubStore();
        var panel = Build(store);
        panel.OpenFor("6229", "John", "Koll");
        var cancelled = false;
        panel.Cancelled += (_, _) => cancelled = true;

        panel.LeaveCommand.Execute(null);

        Assert.IsTrue(cancelled, "FR-024a: leaving returns the person to the identifier stage");
        Assert.IsFalse(panel.IsVisible);
        Assert.AreEqual(0, store.CallCount, "FR-024a: leaving creates nothing");
    }

    [TestMethod]
    public void OpeningThePanel_ShowsThePlantSystemsNamePrefilled()
    {
        var panel = Build();

        panel.OpenFor("6229", "  John ", "  Koll ");

        Assert.IsTrue(panel.IsVisible);
        Assert.AreEqual("6229", panel.EmployeeNumber);
        Assert.AreEqual("John", panel.FirstName, "the plant system's name is pre-filled (FR-007)");
        Assert.AreEqual("Koll", panel.LastName);
        StringAssert.Contains(panel.PermissionNoteText, "IT", "FR-011: a higher level is requested from IT");
    }

    private static SetUpPanelViewModel Build(StubStore? store = null)
        => new(new AccountCreationService(store ?? new StubStore()));

    private sealed class StubStore : IMySqlHelperServer
    {
        public IReadOnlyList<Dictionary<string, object?>> Rows { get; set; } = [];

        public int CallCount { get; private set; }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

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
