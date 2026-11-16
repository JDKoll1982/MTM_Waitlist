using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.GenericRequest.ViewModels;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// The job-fill offer on the category step: it is asked when a job is running, never when one is not, and a refusal
/// is the end of it for that request.
/// </summary>
/// <remarks>
/// The offer is a decision, not a page, so every assertion here is about what the category step shows and what the
/// draft records rather than about a step of its own. The choice belongs to one request, which is why declining twice
/// in the same request is impossible and a second request is offered again.
/// </remarks>
[TestClass]
public sealed class GenericRequestJobFillOfferTests
{
    [TestMethod]
    public async Task ViewModel_GenericRequestJobFillOfferTests_LoadAsync_ARunningJobIsOfferedAndItsValuesAreNotReadYet()
    {
        var (viewModel, fill, navigator) = Create("WC-1");
        fill.RunningJobWorkOrder = "WO-9001";

        await viewModel.LoadAsync();

        Assert.IsTrue(viewModel.IsOfferVisible, "a work center with a running job offers to fill the request (FR-001)");
        Assert.AreEqual("WO-9001", viewModel.JobWorkOrder, "the offer names the job it is about");
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.OfferTitle));
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.OfferQuestion));
        Assert.AreEqual(1, fill.ProbeCount);
        Assert.AreEqual(0, fill.ReadCount, "the job's values are not read before the requester chooses (FR-002)");
        Assert.IsNull(navigator.Draft.JobFill);
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestJobFillOfferTests_LoadAsync_AWorkCenterWithNoJobNeverSeesTheOffer()
    {
        var (viewModel, fill, _) = Create("WC-1");
        fill.RunningJobWorkOrder = null;

        await viewModel.LoadAsync();

        Assert.IsFalse(viewModel.IsOfferVisible, "no job means no question (FR-004)");
        Assert.AreEqual(string.Empty, viewModel.JobWorkOrder);
        Assert.IsTrue(viewModel.IsPopulated, "the step is exactly what it is at a work center with no job");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestJobFillOfferTests_LoadAsync_AProbeThatFailsDrawsNoOfferRatherThanAnError()
    {
        var (viewModel, fill, _) = Create("WC-1");
        fill.ProbeFails = true;

        await viewModel.LoadAsync();

        Assert.IsFalse(viewModel.IsOfferVisible, "a question that cannot be named is not asked");
        Assert.IsFalse(viewModel.IsFillUnavailable, "nothing was accepted, so nothing failed to read (FR-016)");
        Assert.IsTrue(viewModel.IsPopulated);
        Assert.AreEqual(0, fill.ReadCount);
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestJobFillOfferTests_DeclineFillCommand_LeavesTheRequestBlankAndIsNeverAskedAgainInTheRequest()
    {
        var (viewModel, fill, navigator) = Create("WC-1");
        fill.RunningJobWorkOrder = "WO-9001";

        await viewModel.LoadAsync();
        viewModel.DeclineFillCommand.Execute(null);

        Assert.IsFalse(viewModel.IsOfferVisible, "the offer closes when it is answered");
        Assert.IsFalse(viewModel.IsFilledVisible);
        Assert.IsNull(navigator.Draft.JobFill, "a refusal seeds nothing (FR-003)");
        Assert.AreEqual(0, fill.ReadCount, "declining never reads the job's values (FR-002)");

        // Returning to the step is the same request, so the choice already made stands.
        var returning = new GenericRequestCategoryViewModel(
            new StubGenericRequestStepData { Categories = Populated() },
            fill,
            navigator);

        await returning.LoadAsync();

        Assert.IsFalse(returning.IsOfferVisible, "a declined offer is not shown again in the same request (FR-003)");
        Assert.AreEqual(1, fill.ProbeCount, "the step does not even ask whether a job is running again");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestJobFillOfferTests_AWholeNewDraftAtTheSameWorkCenter_IsOfferedAgain()
    {
        var (viewModel, fill, navigator) = Create("WC-1");
        fill.RunningJobWorkOrder = "WO-9001";

        await viewModel.LoadAsync();
        viewModel.DeclineFillCommand.Execute(null);

        // A second request is a second draft, and the offer belongs to the request rather than to the work center.
        var nextRequest = new StubGenericRequestNavigator();
        nextRequest.Draft.WorkCenter = "WC-1";
        nextRequest.Draft.Building = navigator.Draft.Building;

        var second = new GenericRequestCategoryViewModel(
            new StubGenericRequestStepData { Categories = Populated() },
            fill,
            nextRequest);

        await second.LoadAsync();

        Assert.IsTrue(second.IsOfferVisible, "the choice belongs to each request, so the next one is asked again (A3)");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestJobFillOfferTests_ContinueCommand_TheFirstCategoryPickedKeepsTheFillAndAChangeDropsIt()
    {
        var (first, fill, navigator) = Create("WC-1");
        fill.RunningJobWorkOrder = "WO-9001";
        fill.Fill = GenericRequestJobFill.FromJob("WO-9001", "PART-1", "20");

        await first.LoadAsync();
        await first.AcceptFillCommand.ExecuteAsync(null);
        first.SelectCategoryCommand.Execute(first.Options[0]);
        await first.ContinueCommand.ExecuteAsync(null);

        Assert.IsNotNull(navigator.Draft.JobFill, "picking the first category must not throw the fill away");

        var returning = new GenericRequestCategoryViewModel(
            new StubGenericRequestStepData
            {
                Categories = GenericRequestStepResult<GenericRequestCategoryOption>.Populated(
                    [new GenericRequestCategoryOption("Pickup", "Pickup", "Move it"), new GenericRequestCategoryOption("Deliver", "Deliver", "Deliver it")],
                    DateTimeOffset.UtcNow),
            },
            fill,
            navigator);

        await returning.LoadAsync();
        returning.SelectCategoryCommand.Execute(returning.Options.Single(option => option.Code == "Deliver"));
        await returning.ContinueCommand.ExecuteAsync(null);

        Assert.AreEqual("Deliver", navigator.Draft.Category);
        Assert.IsNull(navigator.Draft.JobFill, "a changed category drops the fill with the answers below it (FR-009)");
        Assert.IsTrue(navigator.Draft.JobFillAnswered, "and the offer is still not asked twice in one request (FR-003)");
    }

    private static GenericRequestStepResult<GenericRequestCategoryOption> Populated() =>
        GenericRequestStepResult<GenericRequestCategoryOption>.Populated(
            [new GenericRequestCategoryOption("Pickup", "Pickup", "Move it")],
            DateTimeOffset.UtcNow);

    /// <summary>
    /// The words the offer and the qualifiers carry are pinned by the specification, so the shipped resource file is
    /// read here and the exact copy asserted. The screen resolves these keys through <c>GetLocalized</c>, which is
    /// why a test host cannot see the words themselves.
    /// </summary>
    [TestMethod]
    public void Copy_GenericRequestJobFillOfferTests_ThePinnedWordsAreShippedVerbatim()
    {
        var resources = File.ReadAllText(Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "MTM_Waitlist.GenericRequest",
            "Strings",
            "en-us",
            "Resources.resw"));

        AssertContainsValue(resources, "GenericRequest_Category_JobFill_Title.Text", "A job is running at this work center");
        AssertContainsValue(resources, "GenericRequest_Category_JobFill_Question.Text", "Fill this request from job {0} at {1}?");
        AssertContainsValue(resources, "GenericRequest_Category_JobFill_Accept.Content", "Fill the request");
        AssertContainsValue(resources, "GenericRequest_Category_JobFill_Decline.Content", "Start blank");
        AssertContainsValue(resources, "GenericRequest_Category_JobFill_Filled.Text", "Filled from job {0}");
        AssertContainsValue(resources, "GenericRequest_JobFill_FromJob.Text", "From job {0}");
        AssertContainsValue(resources, "GenericRequest_Category_JobFill_Unavailable.Text", "The running job could not be read");
    }

    private static void AssertContainsValue(string resources, string key, string value)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            resources,
            $@"<data name=""{System.Text.RegularExpressions.Regex.Escape(key)}""[^>]*>\s*<value>(?<value>[^<]*)</value>");

        Assert.IsTrue(match.Success, $"'{key}' must be shipped, because the screen resolves it by that name.");
        Assert.AreEqual(value, match.Groups["value"].Value.Trim(), $"'{key}' carries the pinned words exactly.");
    }

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
