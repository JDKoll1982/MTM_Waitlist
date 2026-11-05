using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.GenericRequest.ViewModels;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// The part step refuses what it cannot prove, clears what a part change invalidates, and will not advance without
/// an answered quantity (017-generic-wc-stated-part FR-002, FR-003, FR-007, FR-008, FR-017).
/// </summary>
[TestClass]
public sealed class GenericRequestPartViewModelTests
{
    [TestMethod]
    public async Task Audit_GenericRequestPartViewModelTests_ATypedNumberNeitherSystemKnowsIsRefused()
    {
        var (viewModel, _) = Create(completions: []);

        viewModel.SearchText = "ZZZ-NOT-A-PART";
        await viewModel.SearchAsync();
        viewModel.RefuseTypedNumber();

        Assert.IsTrue(viewModel.HasNoMatch, "the refusal has to be visible to the requester");
        Assert.AreEqual(0, viewModel.Completions.Count);
        Assert.IsFalse(viewModel.ContinueCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task Audit_GenericRequestPartViewModelTests_APartWithNoSequenceCannotAdvance()
    {
        var (viewModel, _) = Create(
            completions: [Part("100-AAA")],
            sequences: GenericRequestStepResult<GenericRequestSequenceOption>.Empty(DateTimeOffset.UtcNow));

        await viewModel.LoadAsync();
        await viewModel.AcceptPartAsync(Part("100-AAA"));

        Assert.IsTrue(viewModel.HasNoSequence, "the step has to say that no sequence was found");
        Assert.IsFalse(viewModel.ContinueCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task Audit_GenericRequestPartViewModelTests_ChangingThePartClearsTheSequenceAndTheReading()
    {
        var (viewModel, navigator) = Create(
            completions: [Part("100-AAA"), Part("200-BBB")],
            sequences: GenericRequestStepResult<GenericRequestSequenceOption>.Populated(
                [Sequence("20")],
                DateTimeOffset.UtcNow),
            reading: GenericRequestQuantityReading.OnHand(5m, SequenceSource.WipFloor, GenericRequestPartStage.Wip, DateTimeOffset.UtcNow));

        await viewModel.LoadAsync();
        await viewModel.AcceptPartAsync(Part("100-AAA"));
        await viewModel.ChooseSequenceAsync(Sequence("20"));

        Assert.IsNotNull(viewModel.QuantityReading);

        await viewModel.AcceptPartAsync(Part("200-BBB"));

        Assert.IsNull(viewModel.QuantityReading, "a different part must not keep the previous reading");
        Assert.AreEqual(string.Empty, navigator.Draft.Sequence);
        Assert.AreEqual(string.Empty, navigator.Draft.SequenceSource);
    }

    [TestMethod]
    public async Task Audit_GenericRequestPartViewModelTests_TheStepDoesNotAdvanceWithoutAChosenSequence()
    {
        var (viewModel, _) = Create(
            completions: [Part("100-AAA")],
            sequences: GenericRequestStepResult<GenericRequestSequenceOption>.Populated(
                [Sequence("20")],
                DateTimeOffset.UtcNow));

        await viewModel.LoadAsync();
        await viewModel.AcceptPartAsync(Part("100-AAA"));

        Assert.IsFalse(viewModel.ContinueCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task Audit_GenericRequestPartViewModelTests_AFigureOfZeroStillLetsTheRequestGoOn()
    {
        var (viewModel, navigator) = Create(
            completions: [Part("100-AAA")],
            sequences: GenericRequestStepResult<GenericRequestSequenceOption>.Populated([Sequence("20")], DateTimeOffset.UtcNow),
            reading: GenericRequestQuantityReading.OnHand(0m, SequenceSource.WipFloor, GenericRequestPartStage.Wip, DateTimeOffset.UtcNow));

        await viewModel.LoadAsync();
        await viewModel.AcceptPartAsync(Part("100-AAA"));
        await viewModel.ChooseSequenceAsync(Sequence("20"));

        Assert.IsTrue(viewModel.HasNothingOnHand, "nothing on hand warns");
        Assert.IsTrue(viewModel.ContinueCommand.CanExecute(null), "and does not block the request");

        await viewModel.ContinueCommand.ExecuteAsync(null);

        Assert.AreEqual(0m, navigator.Draft.QuantityReading?.Figure);
        Assert.AreEqual(GenericRequestPartStage.Wip, navigator.Draft.PartStage);
    }

    [TestMethod]
    public async Task Audit_GenericRequestPartViewModelTests_AnOutsideServiceOperationIsWorkInProgressAndSaysWhereThePartGoesNext()
    {
        var (viewModel, navigator) = Create(
            completions: [Part("100-AAA")],
            sequences: GenericRequestStepResult<GenericRequestSequenceOption>.Populated(
                [Sequence("90", isLastOperation: false, nextResource: "Outside Service")],
                DateTimeOffset.UtcNow),
            reading: GenericRequestQuantityReading.OnHand(12m, SequenceSource.WipFloor, GenericRequestPartStage.Wip, DateTimeOffset.UtcNow));

        await viewModel.LoadAsync();
        await viewModel.AcceptPartAsync(Part("100-AAA"));
        await viewModel.ChooseSequenceAsync(Sequence("90", isLastOperation: false, nextResource: "Outside Service"));

        Assert.AreEqual("GenericRequest_Part_Stage_Wip.Content", viewModel.StageLabel, "the step says work in progress");
        Assert.IsTrue(viewModel.HasNextResource, "the WIP tag's next-resource line is shown");
        Assert.AreEqual("Outside Service", viewModel.NextResourceDisplay);
        Assert.IsTrue(viewModel.ContinueCommand.CanExecute(null));

        await viewModel.ContinueCommand.ExecuteAsync(null);

        Assert.AreEqual(GenericRequestPartStage.Wip, navigator.Draft.PartStage);
    }

    [TestMethod]
    public async Task Audit_GenericRequestPartViewModelTests_AnUnreadQuantityKeepsTheStep()
    {
        var (viewModel, _) = Create(
            completions: [Part("100-AAA")],
            sequences: GenericRequestStepResult<GenericRequestSequenceOption>.Populated([Sequence("20")], DateTimeOffset.UtcNow),
            reading: GenericRequestQuantityReading.Unavailable(DateTimeOffset.UtcNow));

        await viewModel.LoadAsync();
        await viewModel.AcceptPartAsync(Part("100-AAA"));
        await viewModel.ChooseSequenceAsync(Sequence("20"));

        Assert.IsTrue(viewModel.IsQuantityUnavailable);
        Assert.IsFalse(viewModel.ContinueCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task Audit_GenericRequestPartViewModelTests_TheDraftKeepsThePartSequenceAndSource()
    {
        var (viewModel, navigator) = Create(
            completions: [Part("100-AAA")],
            sequences: GenericRequestStepResult<GenericRequestSequenceOption>.Populated([Sequence("20")], DateTimeOffset.UtcNow),
            reading: GenericRequestQuantityReading.OnHand(4m, SequenceSource.VisualOwnPart, GenericRequestPartStage.FinishedGood, DateTimeOffset.UtcNow));

        await viewModel.LoadAsync();
        await viewModel.AcceptPartAsync(Part("100-AAA"));
        await viewModel.ChooseSequenceAsync(Sequence("20"));

        await viewModel.ContinueCommand.ExecuteAsync(null);

        Assert.AreEqual("100-AAA", navigator.Draft.PartNumber);
        Assert.AreEqual("20", navigator.Draft.Sequence);
        Assert.AreEqual("VisualOwnPart, WipFloor", navigator.Draft.SequenceSource);
        Assert.IsTrue(navigator.Visited.Count > 0, "an answered step advances");
    }

    [TestMethod]
    public async Task Audit_GenericRequestPartViewModelTests_AHalfTypedNumberThatMatchesNothingKeepsTheBoxUsable()
    {
        var (viewModel, _) = Create(completions: []);

        viewModel.SearchText = "A0003";
        await viewModel.SearchAsync();

        Assert.AreEqual(
            GenericRequestPresentation.Populated,
            viewModel.Presentation,
            "a term nobody matches yet must not collapse the step, or the requester cannot finish typing");
        Assert.AreEqual(0, viewModel.Completions.Count);
    }

    [TestMethod]
    public async Task Audit_GenericRequestPartViewModelTests_AnEarlierReadCannotOverwriteTheNewerOne()
    {
        var stalled = new TaskCompletionSource<GenericRequestStepResult<GenericRequestPartOption>>();

        var data = new StubPartData
        {
            CompletionsFactory = term => term == "A0"
                ? stalled.Task
                : Task.FromResult(GenericRequestStepResult<GenericRequestPartOption>.Populated(
                    [Part("A0003")],
                    DateTimeOffset.UtcNow)),
        };

        var viewModel = new GenericRequestPartViewModel(data, new StubGenericRequestNavigator());

        viewModel.SearchText = "A0";
        var older = viewModel.SearchAsync();

        viewModel.SearchText = "A0003";
        await viewModel.SearchAsync();

        stalled.SetResult(GenericRequestStepResult<GenericRequestPartOption>.Empty(DateTimeOffset.UtcNow));
        await older;

        Assert.AreEqual(1, viewModel.Completions.Count, "the newest term's suggestions are the ones on screen");
        Assert.AreEqual("A0003", viewModel.Completions[0].PartNumber);
    }

    [TestMethod]
    public async Task Audit_GenericRequestPartViewModelTests_EditingTheTermWithdrawsTheAcceptedPart()
    {
        var (viewModel, navigator) = Create(
            completions: [Part("100-AAA")],
            sequences: GenericRequestStepResult<GenericRequestSequenceOption>.Populated([Sequence("20")], DateTimeOffset.UtcNow),
            reading: GenericRequestQuantityReading.OnHand(5m, SequenceSource.WipFloor, GenericRequestPartStage.Wip, DateTimeOffset.UtcNow));

        await viewModel.LoadAsync();
        await viewModel.AcceptPartAsync(Part("100-AAA"));
        await viewModel.ChooseSequenceAsync(Sequence("20"));

        viewModel.SearchText = "100-AA";

        Assert.IsNull(viewModel.AcceptedPart, "a term that no longer names the part un-states it");
        Assert.IsNull(viewModel.QuantityReading);
        Assert.AreEqual(string.Empty, navigator.Draft.PartNumber);
        Assert.IsFalse(viewModel.ContinueCommand.CanExecute(null));
    }

    [TestMethod]
    public void Audit_GenericRequestPartViewModelTests_ACompletionShowsItsPartNumberAnywhereItsTextIsFilledIn()
    {
        // A suggestion box fills its text from the item, and the record's own shape is not a part number. The
        // control is told which member to read, and this is the fallback underneath that.
        Assert.AreEqual("100-AAA", Part("100-AAA").ToString());
    }

    private static GenericRequestPartOption Part(string partNumber) =>
        new(partNumber, string.Empty, SequenceSource.VisualOwnPart);

    private static GenericRequestSequenceOption Sequence(
        string sequence,
        bool isLastOperation = false,
        string nextResource = "") =>
        new(
            sequence,
            string.Empty,
            [SequenceSource.VisualOwnPart, SequenceSource.WipFloor],
            "R",
            isLastOperation,
            nextResource,
            HasCancelledOnlyOperations: false);

    private static (GenericRequestPartViewModel ViewModel, StubGenericRequestNavigator Navigator) Create(
        IReadOnlyList<GenericRequestPartOption> completions,
        GenericRequestStepResult<GenericRequestSequenceOption>? sequences = null,
        GenericRequestQuantityReading? reading = null)
    {
        var data = new StubPartData
        {
            Completions = completions.Count == 0
                ? GenericRequestStepResult<GenericRequestPartOption>.Empty(DateTimeOffset.UtcNow)
                : GenericRequestStepResult<GenericRequestPartOption>.Populated(completions, DateTimeOffset.UtcNow),
            Sequences = sequences
                ?? GenericRequestStepResult<GenericRequestSequenceOption>.Empty(DateTimeOffset.UtcNow),
            Reading = reading ?? GenericRequestQuantityReading.Unavailable(DateTimeOffset.UtcNow),
        };

        var navigator = new StubGenericRequestNavigator();
        return (new GenericRequestPartViewModel(data, navigator), navigator);
    }

    /// <summary>The part step's three answers, held in memory so a test can say what the plant would say.</summary>
    private sealed class StubPartData : IGenericRequestPartData
    {
        public GenericRequestStepResult<GenericRequestPartOption> Completions { get; init; } =
            GenericRequestStepResult<GenericRequestPartOption>.Empty(DateTimeOffset.UtcNow);

        /// <summary>
        /// Answers a completion read term by term, so a test can hold one read open while the next one lands.
        /// When null, every read answers <see cref="Completions"/>.
        /// </summary>
        public Func<string, Task<GenericRequestStepResult<GenericRequestPartOption>>>? CompletionsFactory { get; init; }

        public GenericRequestStepResult<GenericRequestSequenceOption> Sequences { get; init; } =
            GenericRequestStepResult<GenericRequestSequenceOption>.Empty(DateTimeOffset.UtcNow);

        public GenericRequestQuantityReading Reading { get; init; } =
            GenericRequestQuantityReading.Unavailable(DateTimeOffset.UtcNow);

        public Task<GenericRequestStepResult<GenericRequestPartOption>> ReadCompletionsAsync(
            string term,
            CancellationToken cancellationToken = default) =>
            CompletionsFactory is { } factory ? factory(term) : Task.FromResult(Completions);

        public Task<GenericRequestStepResult<GenericRequestSequenceOption>> ReadSequencesAsync(
            string partNumber,
            CancellationToken cancellationToken = default) => Task.FromResult(Sequences);

        public Task<GenericRequestQuantityReading> ReadQuantityAsync(
            string partNumber,
            string sequence,
            CancellationToken cancellationToken = default) => Task.FromResult(Reading);
    }
}
