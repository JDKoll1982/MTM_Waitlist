using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.GenericRequest.ViewModels;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// The scrap step's own answer: which scrap type the requester stated.
/// </summary>
/// <remarks>
/// A generic work center has no job to read a scrap decision from, so the step asks, and a step that asks a question
/// cannot be left until the question is answered (FR-001, FR-004).
/// </remarks>
[TestClass]
public sealed class GenericRequestScrapTypeViewModelTests
{
    [TestMethod]
    public async Task ViewModel_GenericRequestScrapTypeViewModelTests_ContinueCommand_ContinueStaysDisabledUntilATypeIsChosen()
    {
        var data = new StubGenericRequestStepData { ScrapTypes = Populated("Steel", "Skeleton") };
        var viewModel = new GenericRequestScrapTypeViewModel(data, new StubGenericRequestNavigator());

        await viewModel.LoadAsync();

        Assert.IsFalse(
            viewModel.ContinueCommand.CanExecute(null),
            "Nothing has been chosen, so the flow must not advance (FR-004).");

        viewModel.Selected = viewModel.Options[1];

        Assert.IsTrue(
            viewModel.ContinueCommand.CanExecute(null),
            "A chosen type is an answer, so the flow may advance (FR-004).");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestScrapTypeViewModelTests_LoadAsync_AStoredTypeRestoresOntoTheChoiceIgnoringCaseAndSpaces()
    {
        var data = new StubGenericRequestStepData { ScrapTypes = Populated("5052 aluminum") };
        var navigator = new StubGenericRequestNavigator();
        navigator.Draft.ScrapType = "  5052 ALUMINUM ";

        var viewModel = new GenericRequestScrapTypeViewModel(data, navigator);
        await viewModel.LoadAsync();

        Assert.IsNotNull(
            viewModel.Selected,
            "A type stated once is the same type whatever its surrounding spaces and letter case (FR-002).");
        Assert.IsTrue(
            viewModel.ContinueCommand.CanExecute(null),
            "Coming back to an answered step leaves it answered.");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestScrapTypeViewModelTests_ContinueCommand_TheChosenTypeIsWrittenToTheDraft()
    {
        var data = new StubGenericRequestStepData { ScrapTypes = Populated("Galvanized Steel") };
        var navigator = new StubGenericRequestNavigator();
        var viewModel = new GenericRequestScrapTypeViewModel(data, navigator);

        await viewModel.LoadAsync();
        viewModel.Selected = viewModel.Options[0];
        await viewModel.ContinueCommand.ExecuteAsync(null);

        Assert.AreEqual(
            "Galvanized Steel",
            navigator.Draft.ScrapType,
            "The wording the requester chose is what the request keeps (FR-005).");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestScrapTypeViewModelTests_LoadAsync_AnUnreadableSourceOffersNoChoiceAndNoAdvance()
    {
        var data = new StubGenericRequestStepData
        {
            ScrapTypes = GenericRequestStepResult<GenericRequestScrapOption>.Unavailable(DateTimeOffset.UtcNow),
        };
        var viewModel = new GenericRequestScrapTypeViewModel(data, new StubGenericRequestNavigator());

        await viewModel.LoadAsync();

        Assert.IsTrue(viewModel.IsUnavailable, "A read that failed is reported as unavailable, not as an empty list.");
        Assert.AreEqual(0, viewModel.Options.Count, "No list is offered rather than a shorter one.");
        Assert.IsFalse(viewModel.ContinueCommand.CanExecute(null), "The flow does not advance off a failed read.");
    }

    private static GenericRequestStepResult<GenericRequestScrapOption> Populated(params string[] scrapTypes) =>
        GenericRequestStepResult<GenericRequestScrapOption>.Populated(
            scrapTypes.Select(scrapType => new GenericRequestScrapOption(scrapType)).ToArray(),
            DateTimeOffset.UtcNow);
}
