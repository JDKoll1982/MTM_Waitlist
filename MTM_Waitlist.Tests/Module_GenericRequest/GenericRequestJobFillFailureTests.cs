using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.GenericRequest.ViewModels;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// What the offer reports when the job's values cannot be read after the requester accepted.
/// </summary>
/// <remarks>
/// A failed read is not a refusal and not a silent nothing: the step says the job could not be read, names when it
/// tried, and offers to try again. Nothing is seeded, so a request is never presented as filled when it is not, and
/// the question is never asked twice (FR-003, FR-016).
/// </remarks>
[TestClass]
public sealed class GenericRequestJobFillFailureTests
{
    [TestMethod]
    public async Task ViewModel_GenericRequestJobFillFailureTests_AcceptFillCommand_AnUnreadableJobSeedsNothingAndReportsTheAttempt()
    {
        var (viewModel, fill, navigator) = Create("WC-1");
        fill.RunningJobWorkOrder = "WO-9001";
        fill.ReadFails = true;
        fill.ReadAttemptedAtUtc = new DateTimeOffset(2026, 11, 15, 14, 30, 0, TimeSpan.Zero);

        await viewModel.LoadAsync();
        await viewModel.AcceptFillCommand.ExecuteAsync(null);

        Assert.IsTrue(viewModel.IsFillUnavailable, "the step reports the failed read (FR-016)");
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.FillUnavailableText));
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.FillAttemptedAtDisplay), "the attempt time is shown");
        Assert.IsNull(navigator.Draft.JobFill, "nothing is seeded, so no partly filled request exists (FR-016)");
        Assert.IsFalse(viewModel.IsFilledVisible);
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestJobFillFailureTests_RetryFillCommand_ReadsAgainWithoutAskingTheQuestionASecondTime()
    {
        var (viewModel, fill, navigator) = Create("WC-1");
        fill.RunningJobWorkOrder = "WO-9001";
        fill.ReadFails = true;

        await viewModel.LoadAsync();
        await viewModel.AcceptFillCommand.ExecuteAsync(null);

        fill.ReadFails = false;
        fill.Fill = GenericRequestJobFill.FromJob("WO-9001", "PART-1", "10");

        await viewModel.RetryFillCommand.ExecuteAsync(null);

        Assert.IsTrue(viewModel.IsFilledVisible, "the retry fills the request (FR-016)");
        Assert.IsNotNull(navigator.Draft.JobFill);
        Assert.AreEqual("PART-1", navigator.Draft.JobFill!.PartNumber);
        Assert.AreEqual(1, fill.ProbeCount, "the retry reads the job, and never re-offers it (FR-003)");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestJobFillFailureTests_AcceptFillCommand_AJobThatWentAwayLeavesTheStepPlainRatherThanFailed()
    {
        var (viewModel, fill, navigator) = Create("WC-1");
        fill.RunningJobWorkOrder = "WO-9001";
        fill.Fill = null;

        await viewModel.LoadAsync();
        await viewModel.AcceptFillCommand.ExecuteAsync(null);

        Assert.IsFalse(viewModel.IsFillUnavailable, "a job that vanished is not a failed read (FR-014)");
        Assert.IsFalse(viewModel.IsFilledVisible);
        Assert.IsNull(navigator.Draft.JobFill);
        Assert.IsTrue(navigator.Draft.JobFillAnswered, "the offer was answered, so it is not asked again");
    }

    private static GenericRequestStepResult<GenericRequestCategoryOption> Populated() =>
        GenericRequestStepResult<GenericRequestCategoryOption>.Populated(
            [new GenericRequestCategoryOption("Pickup", "Pickup", "Move it")],
            DateTimeOffset.UtcNow);

    private static (
        GenericRequestCategoryViewModel ViewModel,
        StubGenericRequestJobFillData Fill,
        StubGenericRequestNavigator Navigator) Create(string workCenter)
    {
        var data = new StubGenericRequestStepData { Categories = Populated() };
        var fill = new StubGenericRequestJobFillData();
        var navigator = new StubGenericRequestNavigator();

        navigator.Draft.WorkCenter = workCenter;
        navigator.Draft.Building = "Expo Drive";

        return (new GenericRequestCategoryViewModel(data, fill, navigator), fill, navigator);
    }
}
