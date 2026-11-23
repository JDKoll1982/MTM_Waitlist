using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.GenericRequest.ViewModels;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// The work center step, and the answers it discards when the work center changes.
/// </summary>
/// <remarks>
/// A stated scrap type says where one work center's scrap goes, so it says nothing about another. Carrying it across
/// a change would raise a request nobody described, against an arrangement the requester never looked at.
/// </remarks>
[TestClass]
public sealed class GenericRequestWorkCenterViewModelTests
{
    [TestMethod]
    public async Task ViewModel_GenericRequestWorkCenterViewModelTests_ContinueCommand_ChangingTheWorkCenterClearsTheStatedScrapType()
    {
        var data = new StubGenericRequestStepData
        {
            WorkCenters = GenericRequestStepResult<GenericRequestWorkCenterOption>.Populated(
                (MTM_Waitlist.GenericRequest.Models.GenericRequestWorkCenterOption[])[
                    new GenericRequestWorkCenterOption("WC-1", "Expo Drive"),
                    new GenericRequestWorkCenterOption("WC-2", "Vits Drive"),
                ],
                DateTimeOffset.UtcNow),
        };
        var navigator = new StubGenericRequestNavigator();
        navigator.Draft.WorkCenter = "WC-1";
        navigator.Draft.RequiresScrap = true;
        navigator.Draft.ScrapType = "Galvanized Steel";

        var viewModel = new GenericRequestWorkCenterViewModel(data, navigator);
        await viewModel.LoadAsync();
        viewModel.Selected = viewModel.Options.Single(option => option.Name == "WC-2");
        await viewModel.ContinueCommand.ExecuteAsync(null);

        Assert.AreEqual("WC-2", navigator.Draft.WorkCenter);
        Assert.AreEqual(
            string.Empty,
            navigator.Draft.ScrapType,
            "A type stated for one work center is not an answer about another.");
        Assert.IsFalse(navigator.Draft.RequiresScrap, "The item that asked for scrap belonged to the old work center.");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestWorkCenterViewModelTests_ContinueCommand_KeepingTheSameWorkCenterKeepsTheStatedScrapType()
    {
        var data = new StubGenericRequestStepData
        {
            WorkCenters = GenericRequestStepResult<GenericRequestWorkCenterOption>.Populated(
                (MTM_Waitlist.GenericRequest.Models.GenericRequestWorkCenterOption[])[new GenericRequestWorkCenterOption("WC-1", "Expo Drive")],
                DateTimeOffset.UtcNow),
        };
        var navigator = new StubGenericRequestNavigator();
        navigator.Draft.WorkCenter = "WC-1";
        navigator.Draft.RequiresScrap = true;
        navigator.Draft.ScrapType = "Galvanized Steel";

        var viewModel = new GenericRequestWorkCenterViewModel(data, navigator);
        await viewModel.LoadAsync();
        await viewModel.ContinueCommand.ExecuteAsync(null);

        Assert.AreEqual(
            "Galvanized Steel",
            navigator.Draft.ScrapType,
            "Going back and forward through the same work center changes nothing.");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestWorkCenterViewModelTests_LoadAsync_AnUnreadableWorkCenterListOffersTheAttemptTimeAndARetry()
    {
        var attemptedAt = new DateTimeOffset(2026, 11, 16, 14, 5, 0, TimeSpan.Zero);
        var data = new StubGenericRequestStepData
        {
            WorkCenters = GenericRequestStepResult<GenericRequestWorkCenterOption>.Unavailable(attemptedAt),
        };

        var viewModel = new GenericRequestWorkCenterViewModel(data, new StubGenericRequestNavigator());
        await viewModel.LoadAsync();

        Assert.IsTrue(viewModel.IsUnavailable, "an unreadable work center list must not present as an empty one");
        Assert.AreEqual(attemptedAt, viewModel.AttemptedAtUtc, "the unavailable presentation carries the time of the attempt");
        Assert.AreNotEqual(string.Empty, viewModel.AttemptedAtDisplay, "the attempt time must be offered to the requester");
        Assert.IsTrue(viewModel.RetryCommand.CanExecute(null), "the unavailable presentation must offer a retry");
    }
}
