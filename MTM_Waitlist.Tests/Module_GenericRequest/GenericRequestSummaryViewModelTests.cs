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

    private static (GenericRequestSummaryViewModel ViewModel, StubGenericRequestNavigator Navigator) Create(
        StubGenericRequestStepData data,
        int? allottedMinutes)
    {
        var navigator = new StubGenericRequestNavigator();
        var draft = Draft(allottedMinutes);

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
    }

    [TestMethod]
    public async Task Unit_GenericRequestSummaryViewModelTests_TheReviewListsTheAnswerAndTheConfiguredTime()
    {
        var (viewModel, _) = Create(new StubGenericRequestStepData(), allottedMinutes: 15);

        await viewModel.LoadAsync();

        var values = viewModel.Rows.Select(row => row.Value).ToList();

        Assert.IsTrue(values.Contains("Bring the spare riser table"), "the answer should be on the review");
        Assert.IsTrue(values.Contains("15"), "the configured allotment should be on the review");
    }

    [TestMethod]
    public async Task Unit_GenericRequestSummaryViewModelTests_AnItemWithNoConfiguredTimeAddsNoEstimate()
    {
        var (viewModel, _) = Create(new StubGenericRequestStepData(), allottedMinutes: null);

        await viewModel.LoadAsync();

        Assert.IsFalse(
            viewModel.Rows.Any(row => row.Value == "0" || row.Value == "15"),
            "an item that configures no time must state none rather than a default");
    }

    [TestMethod]
    public async Task Unit_GenericRequestSummaryViewModelTests_AnAcceptedRequestRecordsItsIdentifierAndAdvances()
    {
        var data = new StubGenericRequestStepData();
        var (viewModel, navigator) = Create(data, allottedMinutes: 15);
        await viewModel.LoadAsync();

        await viewModel.ContinueCommand.ExecuteAsync(null);

        Assert.AreEqual(1, data.SubmitCount, "the review should raise the request exactly once");
        Assert.AreEqual(GenericRequestPresentation.Populated, navigator.Draft.ResultPresentation);
        Assert.AreNotEqual(string.Empty, navigator.Draft.RaisedRequestNumber, "the raised request's identifier should be recorded");
        Assert.AreEqual(GenericRequestStep.Result, navigator.CurrentStep, "an accepted request should move to the result step");
    }

    [TestMethod]
    public async Task Unit_GenericRequestSummaryViewModelTests_ARefusedRequestStaysOnTheReviewAndSaysWhy()
    {
        var data = new StubGenericRequestStepData
        {
            SubmitResult = _ => GenericRequestSubmission.Failed(
                "An active matching request already exists.",
                DateTimeOffset.UtcNow),
        };
        var (viewModel, navigator) = Create(data, allottedMinutes: 15);
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
        Assert.AreNotEqual(GenericRequestStep.Result, navigator.CurrentStep, "a refused request must not advance");
    }
}
