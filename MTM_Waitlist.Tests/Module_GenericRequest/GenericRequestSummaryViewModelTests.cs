using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.GenericRequest.ViewModels;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// The review step lists every answer given, and a refusal keeps the requester on the review instead of showing a
/// result the store never produced (016-generic-wc-request-foundation, FR-013, FR-014, FR-017, FR-018, FR-029).
/// </summary>
[TestClass]
public sealed class GenericRequestSummaryViewModelTests
{
    private static GenericRequestDraft Draft(int? allottedMinutes) => new()
    {
        WorkCenter = "Press 4",
        Building = "Expo Drive",
        Category = "Other",
        ItemName = "Other",
        ItemCode = "other",
        Answer = "Bring the spare riser table",
        EstimatedMinutes = allottedMinutes,
        Description = "The one in bay 4 is cracked.",
    };

    /// <summary>A scrap request: no part, so no stage exists for it to review.</summary>
    private static GenericRequestDraft ScrapDraft() => new()
    {
        WorkCenter = "Die Shop",
        Building = "Expo Drive",
        Category = "Pickup",
        ItemName = "Scrap",
        ItemCode = "pickup-scrap",
        RequiresScrap = true,
        ScrapType = "3003 Aluminum",
        EstimatedMinutes = 30,
    };

    /// <summary>A proved part's request, which is the only kind that has a stage to review.</summary>
    private static GenericRequestDraft PartDraft() => new()
    {
        WorkCenter = "Press 4",
        Building = "Expo Drive",
        Category = "Pickup",
        ItemName = "Coil",
        ItemCode = "pickup-coil",
        RequiresPart = true,
        PartNumber = "MMC0001000",
        PartStage = GenericRequestPartStage.Wip,
        Sequence = "10",
    };

    private static (GenericRequestSummaryViewModel ViewModel, StubGenericRequestNavigator Navigator) Create(
        StubGenericRequestStepData data,
        GenericRequestDraft draft)
    {
        var navigator = new StubGenericRequestNavigator();

        navigator.GoTo(GenericRequestStep.Summary);

        // The navigator owns the draft, so the answers are written through it exactly as a step would write them.
        CopyInto(navigator.Draft, draft);

        return (new GenericRequestSummaryViewModel(data, navigator), navigator);
    }

    private static void CopyInto(GenericRequestDraft target, GenericRequestDraft source)
    {
        target.WorkCenter = source.WorkCenter;
        target.Building = source.Building;
        target.Category = source.Category;
        target.ItemName = source.ItemName;
        target.ItemCode = source.ItemCode;
        target.Answer = source.Answer;
        target.EstimatedMinutes = source.EstimatedMinutes;
        target.Description = source.Description;
        target.RequiresPart = source.RequiresPart;
        target.RequiresScrap = source.RequiresScrap;
        target.ScrapType = source.ScrapType;
        target.PartNumber = source.PartNumber;
        target.PartStage = source.PartStage;
        target.Sequence = source.Sequence;
    }

    private static IReadOnlyList<string> Labels(GenericRequestSummaryViewModel viewModel) =>
        viewModel.Rows.Select(row => row.Label).ToList();

    [TestMethod]
    public async Task ViewModel_GenericRequestSummaryViewModelTests_LoadAsync_TheReviewListsTheAnswerAndTheConfiguredTime()
    {
        var (viewModel, _) = Create(new StubGenericRequestStepData(), Draft(allottedMinutes: 15));

        await viewModel.LoadAsync();

        var values = viewModel.Rows.Select(row => row.Value).ToList();

        Assert.IsTrue(values.Contains("Bring the spare riser table"), "the answer should be on the review");
        Assert.IsTrue(values.Contains("15"), "the configured allotment should be on the review");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestSummaryViewModelTests_LoadAsync_AnItemWithNoConfiguredTimeAddsNoEstimate()
    {
        var (viewModel, _) = Create(new StubGenericRequestStepData(), Draft(allottedMinutes: null));

        await viewModel.LoadAsync();

        Assert.IsFalse(
            viewModel.Rows.Any(row => row.Value == "0" || row.Value == "15"),
            "an item that configures no time must state none rather than a default");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestSummaryViewModelTests_LoadAsync_AScrapRequestReviewsNoStage()
    {
        var (viewModel, _) = Create(new StubGenericRequestStepData(), ScrapDraft());

        await viewModel.LoadAsync();

        StringAssert.Contains(
            string.Join("|", viewModel.Rows.Select(row => row.Value)),
            "3003 Aluminum",
            "the review still names the scrap type the requester chose");

        Assert.IsFalse(
            Labels(viewModel).Any(label => label.Contains("Stage", StringComparison.OrdinalIgnoreCase)),
            "a scrap request has no stage, so the review must not report one as 'Not recorded'.");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestSummaryViewModelTests_LoadAsync_AProvedPartRequestStillReviewsItsStage()
    {
        var (viewModel, _) = Create(new StubGenericRequestStepData(), PartDraft());

        await viewModel.LoadAsync();

        Assert.IsTrue(
            Labels(viewModel).Any(label => label.Contains("Stage", StringComparison.OrdinalIgnoreCase)),
            "a proved part's stage is derived and is exactly what the review is for.");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestSummaryViewModelTests_ContinueCommand_AnAcceptedRequestRecordsItsIdentifierAndAdvances()
    {
        var data = new StubGenericRequestStepData();
        var (viewModel, navigator) = Create(data, Draft(allottedMinutes: 15));
        await viewModel.LoadAsync();

        await viewModel.ContinueCommand.ExecuteAsync(null);

        Assert.AreEqual(1, data.SubmitCount, "the review should raise the request exactly once");
        Assert.AreEqual(GenericRequestPresentation.Populated, navigator.Draft.ResultPresentation);
        Assert.AreNotEqual(string.Empty, navigator.Draft.RaisedRequestNumber, "the raised request's identifier should be recorded");
        Assert.AreEqual(GenericRequestStep.Result, navigator.CurrentStep, "an accepted request should move to the result step");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestSummaryViewModelTests_ContinueCommand_ARefusedRequestStaysOnTheReviewAndSaysWhy()
    {
        var data = new StubGenericRequestStepData
        {
            SubmitResult = _ => GenericRequestSubmission.Failed(
                "An active matching request already exists.",
                DateTimeOffset.UtcNow),
        };
        var (viewModel, navigator) = Create(data, Draft(allottedMinutes: 15));
        await viewModel.LoadAsync();

        await viewModel.ContinueCommand.ExecuteAsync(null);

        Assert.AreEqual(
            GenericRequestPresentation.Unavailable,
            viewModel.Presentation,
            "a refused request must leave the review reporting the failure rather than a result");
        Assert.AreEqual(
            "An active matching request already exists.",
            navigator.Draft.ResultMessage,
            "the store's own words should be recorded for the result step");
        Assert.AreEqual(
            "An active matching request already exists.",
            viewModel.UnavailableText,
            "the review itself says why, so the requester is not told the step 'could not be read' by a read that worked.");
        Assert.AreNotEqual(GenericRequestStep.Result, navigator.CurrentStep, "a refused request must not advance");
    }
}
