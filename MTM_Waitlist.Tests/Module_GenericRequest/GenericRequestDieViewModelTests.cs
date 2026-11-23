using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.GenericRequest.ViewModels;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// The die step's search: what it asks, what each answer draws, and when it will not advance
/// (020-generic-wc-die-search FR-001, FR-002, FR-004, FR-005, FR-007, FR-010, FR-013).
/// </summary>
/// <remarks>
/// The step is driven through the seam every screen uses, so a test says what Infor Visual answered and reads back
/// what the screen would draw. Nothing here reads a job, because a generic die request has none.
/// </remarks>
[TestClass]
public sealed class GenericRequestDieViewModelTests
{
    [TestMethod]
    public async Task ViewModel_GenericRequestDieViewModelTests_EnteringTheStepAsksNothingAndKeepsTheBoxUsable()
    {
        var (viewModel, data, _) = Create(
            GenericRequestStepResult<GenericRequestDieOption>.Empty(DateTimeOffset.UtcNow));

        Assert.IsTrue(
            viewModel.IsPopulated,
            "nothing has been asked yet, so nothing may be claimed: an empty presentation takes the search box "
            + "away with the body, which left the step with no way to search at all");
        Assert.AreEqual(0, data.DieSearches.Count, "entering the step asks no question, because the box is empty");
        Assert.IsFalse(viewModel.HasNoMatch, "no search has run, so no search can have missed");
        Assert.AreEqual(string.Empty, viewModel.CountText);
        Assert.AreEqual(0, viewModel.Options.Count);
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestDieViewModelTests_ABlankSearchAsksNothingAndClaimsNothing()
    {
        var (viewModel, data, _) = Create(
            GenericRequestStepResult<GenericRequestDieOption>.Empty(DateTimeOffset.UtcNow));

        viewModel.SearchText = "   ";
        await viewModel.LoadAsync();

        Assert.AreEqual(0, data.DieSearches.Count, "a blank box is not a question");
        Assert.IsTrue(viewModel.IsPopulated);
        Assert.IsFalse(viewModel.HasNoMatch, "an unasked question is not a miss");
        Assert.IsFalse(viewModel.HasHint, "there is no list to choose from, so there is nothing to choose");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestDieViewModelTests_ATermMatchingNothingKeepsTheStepAndSaysSo()
    {
        var (viewModel, _, navigator) = Create(
            GenericRequestStepResult<GenericRequestDieOption>.Empty(DateTimeOffset.UtcNow));

        viewModel.SearchText = "FGT9999-99";
        await viewModel.LoadAsync();

        Assert.IsTrue(
            viewModel.IsPopulated,
            "a miss is said inside the step, beside the box that can search again, because the frame's empty "
            + "presentation takes that box away with the body");
        Assert.IsTrue(viewModel.HasNoMatch, "the miss is still stated, so a quiet list cannot pass for a search that never ran");
        Assert.IsFalse(
            viewModel.ContinueCommand.CanExecute(null),
            "A step that matched nothing never advances (FR-002, FR-013).");
        Assert.AreEqual(0, navigator.Visited.Count);
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestDieViewModelTests_AFailedSearchTakesTheBodyAndOffersTheRetry()
    {
        var (viewModel, _, _) = Create(
            GenericRequestStepResult<GenericRequestDieOption>.Unavailable(DateTimeOffset.UtcNow));

        viewModel.SearchText = "FGT0602-01";
        await viewModel.LoadAsync();

        Assert.IsTrue(viewModel.IsUnavailable, "a read that failed is reported, and the frame offers the retry (FR-005)");
        Assert.IsFalse(viewModel.HasNoMatch, "a failed read proves nothing about the die, so it is never a miss");
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(viewModel.AttemptedAtDisplay),
            "the failed read says when it was attempted, which is what the retry line reads");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestDieViewModelTests_ASearchThatMatchesDrawsTheCountAndTheHint()
    {
        var (viewModel, _, _) = Create(
            GenericRequestStepResult<GenericRequestDieOption>.Populated(
                (MTM_Waitlist.GenericRequest.Models.GenericRequestDieOption[])[Die("FGT0602-01", "DIE", "U-B0-01")],
                DateTimeOffset.UtcNow));

        viewModel.SearchText = "FGT0602";
        await viewModel.LoadAsync();

        Assert.IsTrue(viewModel.IsPopulated);
        Assert.IsTrue(viewModel.HasSearched);
        Assert.IsFalse(viewModel.HasNoMatch);
        Assert.AreEqual(1, viewModel.MatchCount);
        Assert.IsTrue(viewModel.HasCount);
        Assert.IsTrue(viewModel.HasHint, "nothing is marked yet, so the step says why Continue is not available");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestDieViewModelTests_ContinueNeedsADieMarked()
    {
        var (viewModel, _, _) = Create(
            GenericRequestStepResult<GenericRequestDieOption>.Populated(
                (MTM_Waitlist.GenericRequest.Models.GenericRequestDieOption[])[Die("FGT0602-01", "DIE", "U-B0-01"), Die("FGT0602-02", "DIE TWO", string.Empty)],
                DateTimeOffset.UtcNow));

        viewModel.SearchText = "FGT0602";
        await viewModel.LoadAsync();

        Assert.IsFalse(viewModel.ContinueCommand.CanExecute(null), "No die chosen does not advance (FR-013).");
        Assert.IsTrue(viewModel.HasHint, "The step says why it cannot continue.");

        viewModel.Options[0].IsSelected = true;

        Assert.IsTrue(viewModel.ContinueCommand.CanExecute(null), "One chosen die is enough to continue (FR-010).");
        Assert.IsFalse(viewModel.HasHint);
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestDieViewModelTests_AnUnreachableSourceKeepsTheTermAndOffersRetry()
    {
        var (viewModel, data, _) = Create(
            GenericRequestStepResult<GenericRequestDieOption>.Unavailable(DateTimeOffset.UtcNow));

        viewModel.SearchText = "PART-9003";
        await viewModel.LoadAsync();

        Assert.IsTrue(viewModel.IsUnavailable, "An unreachable source is reported, never substituted (FR-005).");
        Assert.IsNotNull(viewModel.AttemptedAtUtc, "The attempt time is what the requester is shown.");
        Assert.IsFalse(viewModel.ContinueCommand.CanExecute(null));
        Assert.AreEqual("PART-9003", viewModel.SearchText, "The typed term is kept for the retry (FR-005).");

        data.Dies = GenericRequestStepResult<GenericRequestDieOption>.Populated(
            (MTM_Waitlist.GenericRequest.Models.GenericRequestDieOption[])[Die("FGT0602-01", "DIE", "U-B0-01")],
            DateTimeOffset.UtcNow);
        await viewModel.RetryCommand.ExecuteAsync(null);

        CollectionAssert.AreEqual(
            new[] { "PART-9003", "PART-9003" },
            data.DieSearches.ToArray(),
            "Retry asks the same question rather than a blank one (FR-005).");
        Assert.IsTrue(viewModel.IsPopulated);
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestDieViewModelTests_ChoosingSeveralDiesRecordsEveryOneInOrder()
    {
        var (viewModel, _, navigator) = Create(
            GenericRequestStepResult<GenericRequestDieOption>.Populated(
                (MTM_Waitlist.GenericRequest.Models.GenericRequestDieOption[])[
                    Die("FGT0602-01", "DIE", "U-B0-01"),
                    Die("FGT0602-02", "DIE TWO", "U-B0-02"),
                    Die("FGT0603-01", "DIE THREE", "U-B1-01"),
                ],
                DateTimeOffset.UtcNow));

        viewModel.SearchText = "FGT0602";
        await viewModel.LoadAsync();
        viewModel.Options[2].IsSelected = true;
        viewModel.Options[0].IsSelected = true;
        await viewModel.ContinueCommand.ExecuteAsync(null);

        var draft = navigator.Draft;
        CollectionAssert.AreEqual(
            new[] { "FGT0603-01", "FGT0602-01" },
            draft.SelectedDies.Select(die => die.DieNumber).ToArray(),
            "Every chosen die is kept, in the order the requester chose them (FR-010).");
        Assert.AreEqual("FGT0603-01", draft.DieNumber, "The first chosen die is the one a single-die screen reads.");
        Assert.AreEqual(GenericRequestStep.Summary, navigator.CurrentStep);
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestDieViewModelTests_AChosenDieStaysMarkedWhenALaterSearchReturnsIt()
    {
        var (viewModel, data, _) = Create(
            GenericRequestStepResult<GenericRequestDieOption>.Populated(
                (MTM_Waitlist.GenericRequest.Models.GenericRequestDieOption[])[Die("FGT0602-01", "DIE", "U-B0-01")],
                DateTimeOffset.UtcNow));

        viewModel.SearchText = "FGT0602";
        await viewModel.LoadAsync();
        viewModel.Options[0].IsSelected = true;

        data.Dies = GenericRequestStepResult<GenericRequestDieOption>.Populated(
            (MTM_Waitlist.GenericRequest.Models.GenericRequestDieOption[])[Die("FGT0602-01", "DIE", "U-B0-01"), Die("FGT0603-01", "ANOTHER", "U-B1-01")],
            DateTimeOffset.UtcNow);
        viewModel.SearchText = "FGT";
        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.IsTrue(
            viewModel.Options.Single(option => option.DieNumber == "FGT0602-01").IsSelected,
            "Narrowing or widening the search never drops a choice (FR-010).");
        Assert.IsTrue(viewModel.ContinueCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestDieViewModelTests_TheCountIsWhatTheReadAnswered()
    {
        var (viewModel, _, _) = Create(
            GenericRequestStepResult<GenericRequestDieOption>.Populated(
                (MTM_Waitlist.GenericRequest.Models.GenericRequestDieOption[])[
                    Die("FGT0602-01", "DIE", "U-B0-01"),
                    Die("FGT0602-02", "DIE TWO", "U-B0-02"),
                ],
                DateTimeOffset.UtcNow));

        viewModel.SearchText = "FGT0602";
        await viewModel.LoadAsync();

        Assert.AreEqual(2, viewModel.MatchCount);
        Assert.IsTrue(viewModel.HasCount);
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(viewModel.CountText),
            "The number of matches is drawn above the list (FR-007).");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestDieViewModelTests_ADieWithNoLocationDrawsTheStepsOwnWording()
    {
        var (viewModel, _, _) = Create(
            GenericRequestStepResult<GenericRequestDieOption>.Populated(
                (MTM_Waitlist.GenericRequest.Models.GenericRequestDieOption[])[Die("FGT0602-01", "DIE", string.Empty)],
                DateTimeOffset.UtcNow));

        viewModel.SearchText = "FGT0602";
        await viewModel.LoadAsync();

        Assert.IsFalse(
            string.IsNullOrWhiteSpace(viewModel.Options[0].LocationText),
            "A die with no recorded location says so rather than drawing blank (FR-006).");
        Assert.AreEqual(string.Empty, viewModel.Options[0].Location);
    }

    private static GenericRequestDieOption Die(string number, string description, string location) =>
        new(number, description, location);

    private static (
        GenericRequestDieViewModel ViewModel,
        StubGenericRequestStepData Data,
        StubGenericRequestNavigator Navigator) Create(GenericRequestStepResult<GenericRequestDieOption> dies)
    {
        var data = new StubGenericRequestStepData { Dies = dies };
        var navigator = new StubGenericRequestNavigator();

        // The step only exists in a flow whose chosen item asks for a die, so the draft says so.
        navigator.Draft.RequiresDie = true;

        return (new GenericRequestDieViewModel(data, navigator), data, navigator);
    }
}
