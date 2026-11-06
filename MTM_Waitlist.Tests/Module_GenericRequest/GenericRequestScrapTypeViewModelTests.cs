using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.UI.Xaml;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.GenericRequest.ViewModels;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Permissions;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// The scrap type step: it draws the composed list, maps the read's outcomes onto the frame's presentations, gates
/// the forward button on an actual choice, writes that choice into the draft, and re-selects it on a revisit.
/// </summary>
/// <remarks>
/// The second half covers the add affordance (019 FR-017, FR-018): a person entitled to maintain the lugger arrangement
/// can give a missing type to a lugger, through the same assignment the arrangement editor writes, and everyone else
/// is never shown the control.
/// </remarks>
[TestClass]
public sealed class GenericRequestScrapTypeViewModelTests
{
    [TestMethod]
    public async Task ViewModel_GenericRequestScrapTypeViewModelTests_APopulatedReadMapsToPopulated()
    {
        var attempted = DateTimeOffset.UtcNow;
        var data = new ScrapStepData
        {
            ScrapTypes = GenericRequestStepResult<GenericRequestScrapOption>.Populated(
                [new GenericRequestScrapOption("Steel")],
                attempted),
        };
        var viewModel = Create(data, out _);

        await viewModel.LoadAsync();

        Assert.AreEqual(GenericRequestPresentation.Populated, viewModel.Presentation);
        Assert.AreEqual(1, viewModel.Options.Count);
        Assert.AreEqual(attempted, viewModel.AttemptedAtUtc);
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestScrapTypeViewModelTests_AnUnavailableReadMapsToUnavailableWithTheAttemptTime()
    {
        var attempted = DateTimeOffset.UtcNow.AddMinutes(-3);
        var data = new ScrapStepData
        {
            ScrapTypes = GenericRequestStepResult<GenericRequestScrapOption>.Unavailable(attempted),
        };
        var viewModel = Create(data, out _);

        await viewModel.LoadAsync();

        Assert.AreEqual(GenericRequestPresentation.Unavailable, viewModel.Presentation);
        Assert.AreEqual(attempted, viewModel.AttemptedAtUtc, "The unavailable state carries when the read was attempted.");
        Assert.AreEqual(0, viewModel.Options.Count, "An unavailable read offers no list at all.");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestScrapTypeViewModelTests_TheForwardButtonIsClosedUntilATypeIsChosen()
    {
        var viewModel = Create(Populated(["Steel", "5052 aluminum"]), out _);
        await viewModel.LoadAsync();

        Assert.IsFalse(
            viewModel.ContinueCommand.CanExecute(null),
            "A scrap step with nothing chosen must not be left: a request carrying no type names no destination.");

        viewModel.Selected = viewModel.Options[0];

        Assert.IsTrue(
            viewModel.ContinueCommand.CanExecute(null),
            "Choosing a type is what opens the forward button.");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestScrapTypeViewModelTests_ChoosingATypeWritesItIntoTheDraft()
    {
        var viewModel = Create(Populated(["Steel", "5052 aluminum"]), out var navigator);
        await viewModel.LoadAsync();

        viewModel.Selected = viewModel.Options[0];
        await viewModel.ContinueCommand.ExecuteAsync(null);

        Assert.AreEqual("Steel", navigator.Draft.ScrapType);
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestScrapTypeViewModelTests_RevisitingShowsTheStoredTypeAsSelected()
    {
        var viewModel = Create(Populated(["Steel", "5052 aluminum"]), out var navigator);
        navigator.Draft.ScrapType = " 5052 ALUMINUM ";

        await viewModel.LoadAsync();

        Assert.IsNotNull(viewModel.Selected, "The stored type is matched on the trimmed, case-insensitive name.");
        Assert.AreEqual("5052 aluminum", viewModel.Selected!.ScrapType, "The chosen option's own spelling is shown.");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestScrapTypeViewModelTests_NothingIsChosenOnAFreshVisit()
    {
        var viewModel = Create(Populated(["Steel"]), out var navigator);
        navigator.Draft.ScrapType = string.Empty;

        await viewModel.LoadAsync();

        Assert.IsNull(viewModel.Selected, "Nothing is pre-selected when the draft holds no type.");
    }

    // ── Adding a type the list does not carry (019 FR-017, FR-018) ──────────────────────────────────

    [TestMethod]
    public async Task ViewModel_GenericRequestScrapTypeViewModelTests_TheAddControlIsAbsentWithoutTheArrangementPermission()
    {
        var viewModel = Create(Populated(["Steel"]), out _, holdsArrangementPermission: false);

        await viewModel.LoadAsync();

        Assert.IsFalse(viewModel.IsAddAvailable, "Adding a type changes the plant's dump assignments for every card.");
        Assert.AreEqual(Visibility.Collapsed, viewModel.AddVisibility, "A person without the permission sees no control at all.");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestScrapTypeViewModelTests_TheAddControlIsDrawnForAPersonWhoMaintainsTheArrangement()
    {
        var viewModel = Create(Populated(["Steel"]), out _);

        await viewModel.LoadAsync();

        Assert.IsTrue(viewModel.IsAddAvailable);
        Assert.AreEqual(Visibility.Visible, viewModel.AddVisibility);
        Assert.AreEqual(Visibility.Collapsed, viewModel.AddPanelVisibility, "The panel opens on request, not on load.");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestScrapTypeViewModelTests_AddingATypeWritesTheSameAssignmentTheArrangementEditorWrites()
    {
        var data = Populated(["Steel"]);
        var arrangement = new FakeLuggerArrangementService
        {
            Luggers = [Lugger("lug-1", "Door 9 Lugger"), Lugger("lug-2", "Bay 4")],
        };
        arrangement.OnAssign = () => data.ScrapTypes = Result(["Steel", "Aluminum"]);
        var viewModel = Create(data, out _, arrangement);

        await viewModel.LoadAsync();
        await viewModel.ShowAddAsync();
        viewModel.NewScrapType = "  Aluminum  ";
        viewModel.SelectedLugger = viewModel.Luggers.Single(lugger => lugger.Name == "Bay 4");

        await viewModel.AddScrapTypeAsync();

        Assert.AreEqual(1, arrangement.Assignments.Count, "One type is one assignment.");
        var assignment = arrangement.Assignments[0];
        Assert.AreEqual("Aluminum", assignment.ScrapType, "The typed value is trimmed before it is written.");
        Assert.AreEqual("lug-2", assignment.LuggerPublicId, "The type goes to the lugger the person named.");
        Assert.AreEqual(1, assignment.SortPosition, "A type the arrangement does not already carry starts its own order.");
        Assert.AreEqual("1234", assignment.ActorEmployeeNumber, "The write records the signed-in person.");
        Assert.AreEqual("J. Koll", assignment.ActorEmployeeName);
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestScrapTypeViewModelTests_AnAddedTypeIsOfferedAndSelectedAfterTheWrite()
    {
        var data = Populated(["Steel"]);
        var arrangement = new FakeLuggerArrangementService { Luggers = [Lugger("lug-1", "Door 9 Lugger")] };
        arrangement.OnAssign = () => data.ScrapTypes = Result(["Steel", "Aluminum"]);
        var viewModel = Create(data, out _, arrangement);

        await viewModel.LoadAsync();
        await viewModel.ShowAddAsync();
        viewModel.NewScrapType = "Aluminum";

        await viewModel.AddScrapTypeAsync();

        Assert.AreEqual(2, viewModel.Options.Count, "The list is re-read, so what the arrangement now holds is offered.");
        Assert.AreEqual("Aluminum", viewModel.Selected?.ScrapType, "The type just added is the one chosen.");
        Assert.IsTrue(viewModel.ContinueCommand.CanExecute(null), "The requester can carry on with the type they added.");
        Assert.IsFalse(viewModel.IsAdding, "The panel closes once the write lands.");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestScrapTypeViewModelTests_ATypeTheListAlreadyCarriesIsChosenRatherThanWritten()
    {
        var arrangement = new FakeLuggerArrangementService { Luggers = [Lugger("lug-1", "Door 9 Lugger")] };
        var viewModel = Create(Populated(["Steel", "5052 aluminum"]), out _, arrangement);

        await viewModel.LoadAsync();
        await viewModel.ShowAddAsync();
        viewModel.NewScrapType = " 5052 ALUMINUM ";

        await viewModel.AddScrapTypeAsync();

        Assert.AreEqual(0, arrangement.Assignments.Count, "The arrangement gains no duplicate the matcher would treat as the same entry.");
        Assert.AreEqual("5052 aluminum", viewModel.Selected?.ScrapType, "The type the list already carries is the one chosen.");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestScrapTypeViewModelTests_ABlankTypeIsRefusedWithoutAWrite()
    {
        var arrangement = new FakeLuggerArrangementService { Luggers = [Lugger("lug-1", "Door 9 Lugger")] };
        var viewModel = Create(Populated(["Steel"]), out _, arrangement);

        await viewModel.LoadAsync();
        await viewModel.ShowAddAsync();
        viewModel.NewScrapType = "   ";

        await viewModel.AddScrapTypeAsync();

        Assert.AreEqual(0, arrangement.Assignments.Count, "Nothing is written for a blank value.");
        Assert.IsTrue(viewModel.HasAddMessage, "The panel says why it refused.");
        Assert.AreEqual(Visibility.Visible, viewModel.AddMessageVisibility);
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestScrapTypeViewModelTests_AWriteThatFailsIsReportedAndInventsNoType()
    {
        var arrangement = new FakeLuggerArrangementService
        {
            Luggers = [Lugger("lug-1", "Door 9 Lugger")],
            AssignFailure = new InvalidOperationException("the store is down"),
        };
        var viewModel = Create(Populated(["Steel"]), out _, arrangement);

        await viewModel.LoadAsync();
        await viewModel.ShowAddAsync();
        viewModel.NewScrapType = "Aluminum";

        await viewModel.AddScrapTypeAsync();

        Assert.IsTrue(viewModel.HasAddMessage, "A failed write is reported rather than swallowed.");
        Assert.AreEqual(1, viewModel.Options.Count, "A type that was not written is not offered.");
        Assert.IsNull(viewModel.Selected, "Nothing is chosen on the strength of a write that did not land.");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestScrapTypeViewModelTests_TheAddPanelOffersEveryLuggerInTheArrangementOrder()
    {
        var arrangement = new FakeLuggerArrangementService
        {
            Luggers = [Lugger("lug-1", "Door 9 Lugger"), Lugger("lug-2", "Bay 4")],
        };
        var viewModel = Create(Populated(["Steel"]), out _, arrangement);

        await viewModel.LoadAsync();
        await viewModel.ShowAddAsync();

        CollectionAssert.AreEqual(
            new[] { "Door 9 Lugger", "Bay 4" },
            viewModel.Luggers.Select(lugger => lugger.Name).ToArray(),
            "The person names where the type dumps, from the arrangement's own order.");
    }

    private static GenericRequestScrapTypeViewModel Create(
        ScrapStepData data,
        out StubGenericRequestNavigator navigator,
        FakeLuggerArrangementService? arrangement = null,
        bool holdsArrangementPermission = true)
    {
        navigator = new StubGenericRequestNavigator();

        return new GenericRequestScrapTypeViewModel(
            data,
            navigator,
            arrangement ?? new FakeLuggerArrangementService(),
            new StubPermissionService(holdsArrangementPermission),
            new StubPersonIdentity { EmployeeNumber = "1234", DisplayName = "J. Koll" });
    }

    private static Lugger Lugger(string publicId, string name) =>
        new() { PublicId = publicId, Name = name, Status = LuggerStatus.Available, DisplayPosition = 1 };

    private static ScrapStepData Populated(string[] types) =>
        new() { ScrapTypes = Result(types) };

    private static GenericRequestStepResult<GenericRequestScrapOption> Result(string[] types) =>
        GenericRequestStepResult<GenericRequestScrapOption>.Populated(
            types.Select(type => new GenericRequestScrapOption(type)).ToArray(),
            DateTimeOffset.UtcNow);

    /// <summary>
    /// The permission seam, answering one question about the lugger arrangement and nothing else.
    /// </summary>
    private sealed class StubPermissionService : IPermissionService
    {
        private readonly bool _holds;

        public StubPermissionService(bool holds) => _holds = holds;

        public Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _holds
                && string.Equals(permissionKey, PermissionKeys.SettingsLuggerArrangement, StringComparison.Ordinal));

        public Task<IReadOnlyDictionary<string, bool>> HasPermissionsAsync(
            IEnumerable<string> permissionKeys,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public void Invalidate()
        {
        }
    }

    /// <summary>
    /// The scrap step's own seam, answering only the one read the step makes.
    /// </summary>
    private sealed class ScrapStepData : IGenericRequestStepData
    {
        public GenericRequestStepResult<GenericRequestScrapOption> ScrapTypes { get; set; } =
            GenericRequestStepResult<GenericRequestScrapOption>.Unavailable(DateTimeOffset.UtcNow);

        public Task<GenericRequestStepResult<GenericRequestScrapOption>> ReadScrapTypesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ScrapTypes);

        public Task<GenericRequestStepResult<GenericRequestWorkCenterOption>> ReadWorkCentersAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<GenericRequestStepResult<GenericRequestCategoryOption>> ReadCategoriesAsync(string workCenter, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<GenericRequestStepResult<GenericRequestItemOption>> ReadItemsAsync(string category, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<GenericRequestStepResult<GenericRequestPartOption>> ReadPartsAsync(string itemCode, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<GenericRequestStepResult<GenericRequestSequenceOption>> ReadSequencesAsync(string partNumber, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<GenericRequestStepResult<GenericRequestDieOption>> ReadDiesAsync(string searchText, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<GenericRequestStepAvailability> ReadStepAvailabilityAsync(GenericRequestStep step, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<GenericRequestSubmission> SubmitAsync(GenericRequestDraft draft, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
