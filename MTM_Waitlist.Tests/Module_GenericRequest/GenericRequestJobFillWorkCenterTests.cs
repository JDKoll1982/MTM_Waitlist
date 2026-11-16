using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.GenericRequest.ViewModels;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// A fill belongs to the work center it was taken at: moving the request to another one drops it and opens the offer
/// again, because the question that was answered was about the other work center's job.
/// </summary>
/// <remarks>
/// The work center step is reachable from the category step's Back, so this is not a hypothetical path: a requester
/// who accepts the fill and then goes back to pick a different press must not be told the request was filled from a
/// job at the press they just left.
/// </remarks>
[TestClass]
public sealed class GenericRequestJobFillWorkCenterTests
{
    [TestMethod]
    public async Task ViewModel_GenericRequestJobFillWorkCenterTests_ContinueCommand_AnotherWorkCenterDropsTheFillAndOpensTheOfferAgain()
    {
        var data = new StubGenericRequestStepData
        {
            WorkCenters = GenericRequestStepResult<GenericRequestWorkCenterOption>.Populated(
                [new GenericRequestWorkCenterOption("WC-1", "Expo Drive"), new GenericRequestWorkCenterOption("WC-2", "Expo Drive")],
                DateTimeOffset.UtcNow),
        };
        var navigator = new StubGenericRequestNavigator();
        navigator.Draft.WorkCenter = "WC-1";
        navigator.Draft.Building = "Expo Drive";
        navigator.Draft.JobFill = GenericRequestJobFill.FromJob("WO-9001", "PART-1", "20");
        navigator.Draft.JobFillAnswered = true;

        var viewModel = new GenericRequestWorkCenterViewModel(data, navigator);
        await viewModel.LoadAsync();
        viewModel.Selected = viewModel.Options.Single(option => option.Name == "WC-2");
        await viewModel.ContinueCommand.ExecuteAsync(null);

        Assert.AreEqual("WC-2", navigator.Draft.WorkCenter);
        Assert.IsNull(navigator.Draft.JobFill, "a fill taken at another work center says nothing about this one");
        Assert.IsFalse(navigator.Draft.JobFillAnswered, "so the new work center's own job is offered (FR-001)");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestJobFillWorkCenterTests_ContinueCommand_KeepingTheSameWorkCenterKeepsTheFill()
    {
        var data = new StubGenericRequestStepData
        {
            WorkCenters = GenericRequestStepResult<GenericRequestWorkCenterOption>.Populated(
                [new GenericRequestWorkCenterOption("WC-1", "Expo Drive")],
                DateTimeOffset.UtcNow),
        };
        var navigator = new StubGenericRequestNavigator();
        navigator.Draft.WorkCenter = "WC-1";
        navigator.Draft.Building = "Expo Drive";
        navigator.Draft.JobFill = GenericRequestJobFill.FromJob("WO-9001", "PART-1", "20");
        navigator.Draft.JobFillAnswered = true;

        var viewModel = new GenericRequestWorkCenterViewModel(data, navigator);
        await viewModel.LoadAsync();
        await viewModel.ContinueCommand.ExecuteAsync(null);

        Assert.IsNotNull(navigator.Draft.JobFill, "going back and forward through the same work center changes nothing");
        Assert.IsTrue(navigator.Draft.JobFillAnswered);
    }
}
