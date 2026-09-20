using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Waitlist.ViewModels;

/// <summary>
/// The two review steps show <b>one row per entry the wizard will raise</b>, not one row per request.
/// </summary>
/// <remarks>
/// <para>
/// Found by walking the wizard in the running app on 2026-09-20: with two dies chosen the preview listed a single
/// die, and the confirmation listed a single die <b>and never showed the count it had already computed</b> — so
/// both steps understated what the operator was about to raise, even though the submission raised one request per
/// die correctly. These checks close the pair: the field content, and the markup that has to render it.
/// </para>
/// <para>
/// <see cref="NewRequestFlowState.ToDrafts"/> is the single answer to "what will be raised". Both steps are tied
/// to it rather than to the request's one value column, so neither can drift from the submission again.
/// </para>
/// </remarks>
[TestClass]
public sealed class NewRequestReviewShowsEveryDieTests
{
    private static readonly XNamespace s_presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    [TestMethod]
    public void PreviewStep_WithTwoDiesChosen_ShowsOneRowPerDie()
    {
        var viewModel = new NewRequestPreviewViewModel(new WaitlistTestNavigationService());

        viewModel.OnNavigatedTo(DieState(("FGT0002000", "DIE SHOP"), ("FGT0002001", "PRESS BAY")));

        CollectionAssert.AreEqual(
            new[] { "FGT0002000", "FGT0002001" },
            viewModel.DetailLines.ToArray(),
            "The preview lists every die the operator chose, in the order they will be raised.");
        Assert.IsTrue(viewModel.HasDetail, "Two dies is something to show, so the row is not hidden.");
    }

    [TestMethod]
    public void ConfirmStep_WithTwoDiesChosen_ShowsOneRowPerDie()
    {
        var viewModel = BuildSummaryViewModel();

        viewModel.OnNavigatedTo(DieState(("FGT0002000", "DIE SHOP"), ("FGT0002001", "PRESS BAY")));

        CollectionAssert.AreEqual(
            new[] { "FGT0002000", "FGT0002001" },
            viewModel.DetailLines.ToArray(),
            "The confirmation lists every die, so it cannot show one request where two will be raised.");
        Assert.IsTrue(viewModel.HasDetail);
    }

    [TestMethod]
    public void BothReviewSteps_ListExactlyWhatWillBeRaised()
    {
        // The invariant, rather than a count: whatever the submission will raise is what the operator was shown.
        var state = DieState(
            ("FGT0001000", "DIE SHOP"),
            ("FGT0001001", "PRESS BAY"),
            ("FGT0001002", "RACK 4"));

        var expected = state.ToDrafts().Select(draft => draft.InputValue!).ToArray();

        var preview = new NewRequestPreviewViewModel(new WaitlistTestNavigationService());
        preview.OnNavigatedTo(state);

        var confirm = BuildSummaryViewModel();
        confirm.OnNavigatedTo(state);

        CollectionAssert.AreEqual(expected, preview.DetailLines.ToArray(), "The preview must not understate the run.");
        CollectionAssert.AreEqual(expected, confirm.DetailLines.ToArray(), "The confirmation must not understate the run.");
    }

    [TestMethod]
    public void PreviewStep_WithOneAnswer_IsThatOneRow()
    {
        // The ordinary single-request wizard is untouched by the list.
        var state = new NewRequestFlowState
        {
            WorkCenter = "100-7",
            Category = RequestCategory.Other,
            Item = RequestItemCatalog.FindById("other"),
            InputValue = "Skid 4471 is on the wrong dock",
        };

        var viewModel = new NewRequestPreviewViewModel(new WaitlistTestNavigationService());

        viewModel.OnNavigatedTo(state);

        CollectionAssert.AreEqual(new[] { "Skid 4471 is on the wrong dock" }, viewModel.DetailLines.ToArray());
    }

    [TestMethod]
    public void PreviewStep_WithNothingToShow_ShowsNoLabelledEmptyRow()
    {
        var state = new NewRequestFlowState
        {
            WorkCenter = "100-7",
            Category = RequestCategory.Pickup,
            Item = RequestItemCatalog.FindById("pickup-die"),
        };

        var viewModel = new NewRequestPreviewViewModel(new WaitlistTestNavigationService());

        viewModel.OnNavigatedTo(state);

        Assert.AreEqual(0, viewModel.DetailLines.Count, "Nothing was chosen, so there is nothing to list.");
        Assert.IsFalse(viewModel.HasDetail, "An empty list must not render a label with no value under it.");
    }

    [TestMethod]
    public void PreviewPage_RendersTheDetailAsARepeatingList()
    {
        var markup = File.ReadAllText(PagePath("NewRequestPreviewPage.xaml"));

        AssertDetailIsAList(markup, "NewRequestPreviewPage");
    }

    [TestMethod]
    public void ConfirmPage_RendersTheDetailAsARepeatingList()
    {
        var markup = File.ReadAllText(PagePath("NewRequestSummaryPage.xaml"));

        AssertDetailIsAList(markup, "NewRequestSummaryPage");
    }

    [TestMethod]
    public void ConfirmPage_ShowsTheRequestCountItComputed()
    {
        // The step computed "this will raise N requests" and bound it to nothing, which is how the operator was
        // left looking at one die while two requests were about to be raised. The binding is the fix.
        var markup = File.ReadAllText(PagePath("NewRequestSummaryPage.xaml"));

        Assert.IsTrue(
            markup.Contains("RequestCountText", StringComparison.Ordinal),
            "The confirmation must render the request count it resolves, or the operator is told nothing.");
        Assert.IsTrue(
            markup.Contains("HasMultipleRequests", StringComparison.Ordinal),
            "Showing several is the exception, so the announcement is gated on it rather than shown always.");
    }

    /// <summary>
    /// The detail field must be a repeating list driven by the list the step resolved, and the one-value binding
    /// it replaced must be gone — otherwise a single die can be shown again while several are raised.
    /// </summary>
    private static void AssertDetailIsAList(string markup, string pageName)
    {
        var document = XDocument.Parse(markup);

        Assert.IsTrue(
            document.Descendants(s_presentation + "ItemsControl")
                .Any(element => element.Attribute("ItemsSource")?.Value.Contains("DetailLines", StringComparison.Ordinal) == true),
            $"{pageName} must render its detail as an ItemsControl bound to DetailLines, so every die is listed.");

        Assert.IsFalse(
            markup.Contains("ViewModel.Detail,", StringComparison.Ordinal),
            $"{pageName} still binds a single Detail value, which shows one die where several will be raised.");
    }

    private static NewRequestFlowState DieState(params (string PartNumber, string Location)[] dies)
    {
        var parts = dies.Select(die => new RequestDiePart { PartNumber = die.PartNumber, Location = die.Location }).ToArray();

        return new NewRequestFlowState
        {
            WorkCenter = "100-7",
            Category = RequestCategory.Pickup,
            Item = RequestItemCatalog.FindById("pickup-die"),
            Availability = (RequestJobPartAvailability.None with { HasActiveJob = true, HasDie = true }).WithDies(parts),
            SelectedDies = parts,
            InputValue = parts[0].Label,
        };
    }

    private static NewRequestSummaryViewModel BuildSummaryViewModel()
        => new(new WaitlistTestNavigationService(), new NoOpRequestService(), new NoCoilAvailabilityService());

    private static string PagePath(string fileName)
    {
        var path = Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), "Module_Waitlist", "Views", fileName);

        Assert.IsTrue(File.Exists(path), $"The wizard markup was not found at '{path}'.");

        return path;
    }

    private sealed class NoCoilAvailabilityService : ICoilAvailabilityService
    {
        public Task<WaitlistCoilInfo> GetCoilForJobAsync(string? workCenter, CancellationToken cancellationToken = default)
            => Task.FromResult(new WaitlistCoilInfo { HasCoil = false });
    }

    private sealed class NoOpRequestService : IWaitlistRequestService
    {
        public event EventHandler? RequestsChanged
        {
            add { }
            remove { }
        }

        public IReadOnlyList<WaitlistRequest> GetActiveRequests(string? building = null) => [];

        public WaitlistRequest? GetRequest(Guid requestId) => null;

        public IReadOnlyList<WaitlistRequest> GetMyRequests(string requesterEmployeeNumber, string? building = null) => [];

        public IReadOnlyList<WaitlistRequestAuditEntry> GetAuditTrail(Guid requestId) => [];

        public Task<IReadOnlyList<WaitlistRequestAuditEntry>> LoadAuditTrailAsync(Guid requestId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<WaitlistRequestAuditEntry>>([]);

        public void Reset()
        {
        }

        public Task<int> RefreshFromDatabaseAsync(string? building = null, CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<WaitlistRequestSubmitResult> SubmitAsync(WaitlistRequestDraft draft, bool allowDuplicate, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> TransitionStatusAsync(Guid requestId, string status, string? cancellationReason = null, string? canceledByEmployeeNumber = null, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<WaitlistRequestCancelResult> CancelOwnRequestAsync(Guid requestId, string requesterEmployeeNumber, string? reason = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<WaitlistRequest?> UpdateNoteAsync(Guid requestId, string? note, string? actorEmployeeNumber = null, string? actorEmployeeName = null, CancellationToken cancellationToken = default) => Task.FromResult<WaitlistRequest?>(null);

        public Task<WaitlistRequest?> AcceptAsync(Guid requestId, string handlerEmployeeNumber, string? handlerEmployeeName = null, CancellationToken cancellationToken = default) => Task.FromResult<WaitlistRequest?>(null);

        public Task<WaitlistRequest?> CompleteAsync(Guid requestId, string handlerEmployeeNumber, string? handlerEmployeeName = null, CancellationToken cancellationToken = default) => Task.FromResult<WaitlistRequest?>(null);

        public Task<WaitlistRequest?> ReleaseAsync(Guid requestId, string handlerEmployeeNumber, string? handlerEmployeeName = null, CancellationToken cancellationToken = default) => Task.FromResult<WaitlistRequest?>(null);
    }
}
