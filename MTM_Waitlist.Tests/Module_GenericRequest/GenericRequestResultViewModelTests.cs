using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.GenericRequest.ViewModels;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// The result step is the same screen the work-order flow ends on: one icon, one heading, one sentence, and the
/// buttons that fit the outcome. These tests fix which outcome each raise attempt produces and what each button does,
/// so the two endings cannot drift apart (FR-019).
/// </summary>
[TestClass]
public sealed class GenericRequestResultViewModelTests
{
    private const string RequestNumber = "ba1d2c3e-0000-4000-8000-000000000001";

    private static (GenericRequestResultViewModel ViewModel, StubGenericRequestNavigator Navigator, StubGenericRequestStepData Data) Create()
    {
        var data = new StubGenericRequestStepData();
        var navigator = new StubGenericRequestNavigator();
        navigator.GoTo(GenericRequestStep.Result);

        return (new GenericRequestResultViewModel(data, navigator), navigator, data);
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestResultViewModelTests_LoadAsync_ARaisedRequestShowsItsNumberAsSuccess()
    {
        var (viewModel, navigator, _) = Create();
        navigator.Draft.ResultPresentation = GenericRequestPresentation.Populated;
        navigator.Draft.RaisedRequestNumber = RequestNumber;
        navigator.Draft.ResultAttemptedAtUtc = DateTimeOffset.UtcNow;

        await viewModel.LoadAsync();

        Assert.IsTrue(viewModel.IsSuccess, "a raised request is the success outcome");
        Assert.IsFalse(viewModel.IsDuplicate, "a raised request is not a refusal");
        Assert.IsFalse(viewModel.IsFailure, "a raised request is not a failure");
        Assert.AreNotEqual(string.Empty, viewModel.Message, "the success outcome states what was raised");
        Assert.AreNotEqual(string.Empty, viewModel.Heading, "the outcome needs a heading");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestResultViewModelTests_LoadAsync_ARefusedRequestShowsTheQueuesOwnSentence()
    {
        const string refusal = "An active matching request already exists.";

        var (viewModel, navigator, _) = Create();
        navigator.Draft.ResultPresentation = GenericRequestPresentation.Unavailable;
        navigator.Draft.ResultWasDuplicate = true;
        navigator.Draft.ResultMessage = refusal;

        await viewModel.LoadAsync();

        Assert.IsTrue(viewModel.IsDuplicate, "a refused request is the duplicate outcome");
        Assert.IsFalse(viewModel.IsSuccess, "a refused request is not a success");
        Assert.IsFalse(viewModel.IsFailure, "a refusal is an answer, not a fault");
        Assert.AreEqual(refusal, viewModel.Message, "the queue's own words must reach the screen unchanged");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestResultViewModelTests_LoadAsync_AWriteThatFailedOffersARetry()
    {
        var (viewModel, navigator, _) = Create();
        navigator.Draft.ResultPresentation = GenericRequestPresentation.Unavailable;
        navigator.Draft.ResultMessage = "The store could not be reached.";

        await viewModel.LoadAsync();

        Assert.IsTrue(viewModel.IsFailure, "a write that did not land is the failure outcome");
        Assert.IsFalse(viewModel.IsDuplicate, "a failure is not a refusal");
        Assert.AreEqual("The store could not be reached.", viewModel.Message);
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestResultViewModelTests_LoadAsync_EachOutcomeDrawsItsOwnGlyph()
    {
        var (success, successNavigator, _) = Create();
        successNavigator.Draft.ResultPresentation = GenericRequestPresentation.Populated;
        successNavigator.Draft.RaisedRequestNumber = RequestNumber;
        await success.LoadAsync();

        var (duplicate, duplicateNavigator, _) = Create();
        duplicateNavigator.Draft.ResultPresentation = GenericRequestPresentation.Unavailable;
        duplicateNavigator.Draft.ResultWasDuplicate = true;
        await duplicate.LoadAsync();

        var (failure, failureNavigator, _) = Create();
        failureNavigator.Draft.ResultPresentation = GenericRequestPresentation.Unavailable;
        await failure.LoadAsync();

        Assert.AreNotEqual(
            success.IconGlyph,
            failure.IconGlyph,
            "a success and a failure must not look the same");
        Assert.AreNotEqual(
            duplicate.IconGlyph,
            failure.IconGlyph,
            "a refusal and a fault are different answers and must draw differently");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestResultViewModelTests_LoadAsync_AnUnfinishedDraftStatesSomething()
    {
        var (viewModel, _, _) = Create();

        await viewModel.LoadAsync();

        Assert.AreEqual(
            GenericRequestPresentation.Empty,
            viewModel.Presentation,
            "a draft nobody finished has nothing to report");
        Assert.AreNotEqual(string.Empty, viewModel.Message, "an outcome with no sentence reads as a broken screen");
    }

    [TestMethod]
    public void ViewModel_GenericRequestResultViewModelTests_AddAnotherRequest_StartsAFreshRequest()
    {
        var (viewModel, navigator, _) = Create();

        viewModel.AddAnotherRequestCommand.Execute(null);

        Assert.AreEqual(1, navigator.StartAnotherCount, "the button must ask the flow to start over exactly once");
    }

    [TestMethod]
    public void ViewModel_GenericRequestResultViewModelTests_ReturnToWaitlist_LeavesTheFlow()
    {
        var (viewModel, navigator, _) = Create();

        viewModel.ReturnToWaitlistCommand.Execute(null);

        Assert.AreEqual(1, navigator.LeaveToWaitlistCount, "the button must show the waitlist");
    }

    [TestMethod]
    public void ViewModel_GenericRequestResultViewModelTests_CancelToWaitlist_LeavesTheFlow()
    {
        var (viewModel, navigator, _) = Create();

        viewModel.CancelToWaitlistCommand.Execute(null);

        Assert.AreEqual(1, navigator.LeaveToWaitlistCount, "closing a failure leaves the flow the same way");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestResultViewModelTests_Resubmit_AWriteThatLandsTurnsTheFailureIntoASuccess()
    {
        var (viewModel, navigator, data) = Create();
        navigator.Draft.ResultPresentation = GenericRequestPresentation.Unavailable;
        navigator.Draft.ResultMessage = "The store could not be reached.";
        await viewModel.LoadAsync();

        data.SubmitResult = _ => GenericRequestSubmission.Raised(RequestNumber, DateTimeOffset.UtcNow);

        await viewModel.ResubmitCommand.ExecuteAsync(null);

        Assert.AreEqual(1, data.SubmitCount, "retrying must ask the queue again rather than only redraw the screen");
        Assert.IsTrue(viewModel.IsSuccess, "the retry's own answer decides what the screen shows");
        Assert.AreEqual(
            RequestNumber,
            navigator.Draft.RaisedRequestNumber,
            "the number the retry produced must be the one the screen reports");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestResultViewModelTests_Resubmit_AWriteRefusedAgainTurnsIntoTheDuplicateOutcome()
    {
        var (viewModel, navigator, data) = Create();
        navigator.Draft.ResultPresentation = GenericRequestPresentation.Unavailable;
        await viewModel.LoadAsync();

        data.SubmitResult = _ => GenericRequestSubmission.Duplicate(
            "An active matching request already exists.",
            DateTimeOffset.UtcNow);

        await viewModel.ResubmitCommand.ExecuteAsync(null);

        Assert.IsTrue(viewModel.IsDuplicate, "a retry the queue refuses is a refusal, not a fault");
        Assert.IsFalse(viewModel.IsFailure, "a refusal is an answer");
    }
}
