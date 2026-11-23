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
            completions: (MTM_Waitlist.GenericRequest.Models.GenericRequestPartOption[])[Part("100-AAA")],
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
            completions: (MTM_Waitlist.GenericRequest.Models.GenericRequestPartOption[])[Part("100-AAA"), Part("200-BBB")],
            sequences: GenericRequestStepResult<GenericRequestSequenceOption>.Populated(
                (MTM_Waitlist.GenericRequest.Models.GenericRequestSequenceOption[])[Sequence("20")],
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
            completions: (MTM_Waitlist.GenericRequest.Models.GenericRequestPartOption[])[Part("100-AAA")],
            sequences: GenericRequestStepResult<GenericRequestSequenceOption>.Populated(
                (MTM_Waitlist.GenericRequest.Models.GenericRequestSequenceOption[])[Sequence("20")],
                DateTimeOffset.UtcNow));

        await viewModel.LoadAsync();
        await viewModel.AcceptPartAsync(Part("100-AAA"));

        Assert.IsFalse(viewModel.ContinueCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task Audit_GenericRequestPartViewModelTests_AFigureOfZeroStillLetsTheRequestGoOn()
    {
        var (viewModel, navigator) = Create(
            completions: (MTM_Waitlist.GenericRequest.Models.GenericRequestPartOption[])[Part("100-AAA")],
            sequences: GenericRequestStepResult<GenericRequestSequenceOption>.Populated((MTM_Waitlist.GenericRequest.Models.GenericRequestSequenceOption[])[Sequence("20")], DateTimeOffset.UtcNow),
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
            completions: (MTM_Waitlist.GenericRequest.Models.GenericRequestPartOption[])[Part("100-AAA")],
            sequences: GenericRequestStepResult<GenericRequestSequenceOption>.Populated(
                (MTM_Waitlist.GenericRequest.Models.GenericRequestSequenceOption[])[Sequence("90", isLastOperation: false, nextResource: "Outside Service")],
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
            completions: (MTM_Waitlist.GenericRequest.Models.GenericRequestPartOption[])[Part("100-AAA")],
            sequences: GenericRequestStepResult<GenericRequestSequenceOption>.Populated((MTM_Waitlist.GenericRequest.Models.GenericRequestSequenceOption[])[Sequence("20")], DateTimeOffset.UtcNow),
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
            completions: (MTM_Waitlist.GenericRequest.Models.GenericRequestPartOption[])[Part("100-AAA")],
            sequences: GenericRequestStepResult<GenericRequestSequenceOption>.Populated((MTM_Waitlist.GenericRequest.Models.GenericRequestSequenceOption[])[Sequence("20")], DateTimeOffset.UtcNow),
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
                    (MTM_Waitlist.GenericRequest.Models.GenericRequestPartOption[])[Part("A0003")],
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
            completions: (MTM_Waitlist.GenericRequest.Models.GenericRequestPartOption[])[Part("100-AAA")],
            sequences: GenericRequestStepResult<GenericRequestSequenceOption>.Populated((MTM_Waitlist.GenericRequest.Models.GenericRequestSequenceOption[])[Sequence("20")], DateTimeOffset.UtcNow),
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

    [TestMethod]
    public async Task Audit_GenericRequestPartViewModelTests_ACoilStatesThePartAloneAndShowsWhatIsOnHand()
    {
        var coil = new GenericRequestPartOption("MMC0000658", "Coil, 10Ga X 6.000", SequenceSource.VisualOwnPart);
        var (viewModel, navigator) = Create(
            completions: (MTM_Waitlist.GenericRequest.Models.GenericRequestPartOption[])[coil],
            sequences: GenericRequestStepResult<GenericRequestSequenceOption>.Populated(
                (MTM_Waitlist.GenericRequest.Models.GenericRequestSequenceOption[])[Sequence("10")],
                DateTimeOffset.UtcNow),
            partWideReading: GenericRequestQuantityReading.OnHand(
                4998.7m,
                SequenceSource.VisualOwnPart,
                GenericRequestPartStage.Unknown,
                DateTimeOffset.UtcNow));

        await viewModel.LoadAsync();
        navigator.Draft.RequiresPart = true;
        navigator.Draft.ItemCode = "pickup-coil";
        await viewModel.AcceptPartAsync(coil);

        Assert.IsTrue(viewModel.IsCoilOrFlatstock, "an MMC part number is a coil");
        Assert.AreEqual(0, viewModel.Sequences.Count, "a coil is not attributed to a work-order sequence");
        Assert.AreEqual("4999", viewModel.DetailQuantityOnHand, "the whole-part figure is rounded up");
        Assert.AreEqual("Coil, 10Ga X 6.000", viewModel.DetailDescription);
        Assert.IsTrue(viewModel.ContinueCommand.CanExecute(null), "the part alone answers a coil");

        await viewModel.ContinueCommand.ExecuteAsync(null);

        Assert.AreEqual(string.Empty, navigator.Draft.Sequence, "a coil request records no sequence");
        Assert.AreEqual(
            GenericRequestStep.Summary,
            navigator.CurrentStep,
            "a coil has nothing for the details page to ask, so it goes straight to the review");
    }

    [TestMethod]
    public async Task Audit_GenericRequestPartViewModelTests_AFailedPartWideReadKeepsACoilOnTheStep()
    {
        var (viewModel, navigator) = Create(
            completions: (MTM_Waitlist.GenericRequest.Models.GenericRequestPartOption[])[Part("MMF0005511")],
            partWideReading: GenericRequestQuantityReading.Unavailable(DateTimeOffset.UtcNow));

        await viewModel.LoadAsync();
        navigator.Draft.ItemCode = "deliver-flatstock";
        await viewModel.AcceptPartAsync(Part("MMF0005511"));

        Assert.IsTrue(viewModel.IsCoilOrFlatstock, "an MMF part number is a flatstock");
        Assert.IsFalse(
            viewModel.ContinueCommand.CanExecute(null),
            "the step does not advance on a quantity nobody answered");
        Assert.AreEqual(0, navigator.Visited.Count);
    }

    [DataTestMethod]
    [DataRow("pickup-coil", DisplayName = "the Pickup coil-and-flatstock item")]
    [DataRow("deliver-coil", DisplayName = "the Deliver coil item")]
    [DataRow("deliver-flatstock", DisplayName = "the Deliver flatstock item")]
    public async Task Audit_GenericRequestPartViewModelTests_ACoilOrFlatstockItemOffersOnlyCoilAndFlatstockParts(string itemCode)
    {
        var (viewModel, navigator) = Create(
            completions: (MTM_Waitlist.GenericRequest.Models.GenericRequestPartOption[])[
                Part("MMC0000658"),
                Part("MMF0005511"),
                Part("100-AAA")]);
        navigator.Draft.ItemCode = itemCode;
        viewModel.SearchText = "MM";

        await viewModel.SearchAsync();

        CollectionAssert.AreEqual(
            new[] { "MMC0000658", "MMF0005511" },
            viewModel.Completions.Select(option => option.PartNumber).ToArray(),
            "a coil or flatstock request is only ever about a coil or a flatstock");
    }

    [DataTestMethod]
    [DataRow("pickup-fg", DisplayName = "a finished-good pickup")]
    [DataRow("pickup-ncm", DisplayName = "a non-conforming pickup")]
    [DataRow("pickup-wip", DisplayName = "a work-in-process pickup")]
    [DataRow("pickup-outside-service", DisplayName = "an outside-service pickup")]
    public async Task Audit_GenericRequestPartViewModelTests_EveryOtherItemOffersNoCoilOrFlatstockPart(string itemCode)
    {
        var (viewModel, navigator) = Create(
            completions: (MTM_Waitlist.GenericRequest.Models.GenericRequestPartOption[])[
                Part("MMC0000658"),
                Part("MMF0005511"),
                Part("100-AAA")]);
        navigator.Draft.ItemCode = itemCode;
        viewModel.SearchText = "MM";

        await viewModel.SearchAsync();

        CollectionAssert.AreEqual(
            new[] { "100-AAA" },
            viewModel.Completions.Select(option => option.PartNumber).ToArray(),
            "only the coil and flatstock items are about a coil or a flatstock");
    }

    [TestMethod]
    public async Task Audit_GenericRequestPartViewModelTests_AItemThatIsNotAboutACoilOrFlatstockCannotAcceptOne()
    {
        var (viewModel, navigator) = Create(
            completions: (MTM_Waitlist.GenericRequest.Models.GenericRequestPartOption[])[Part("100-AAA")]);
        navigator.Draft.ItemCode = "pickup-fg";

        await viewModel.LoadAsync();
        await viewModel.AcceptPartAsync(Part("MMC0000658"));

        Assert.IsNull(viewModel.AcceptedPart, "a part the item cannot be about is never accepted");
        Assert.AreEqual(string.Empty, navigator.Draft.PartNumber);
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
            (MTM_Waitlist.GenericRequest.Models.SequenceSource[])[SequenceSource.VisualOwnPart, SequenceSource.WipFloor],
            "R",
            isLastOperation,
            nextResource,
            HasCancelledOnlyOperations: false);

    private static (GenericRequestPartViewModel ViewModel, StubGenericRequestNavigator Navigator) Create(
        IReadOnlyList<GenericRequestPartOption> completions,
        GenericRequestStepResult<GenericRequestSequenceOption>? sequences = null,
        GenericRequestQuantityReading? reading = null,
        GenericRequestQuantityReading? partWideReading = null)
    {
        var data = new StubPartData
        {
            Completions = completions.Count == 0
                ? GenericRequestStepResult<GenericRequestPartOption>.Empty(DateTimeOffset.UtcNow)
                : GenericRequestStepResult<GenericRequestPartOption>.Populated(completions, DateTimeOffset.UtcNow),
            Sequences = sequences
                ?? GenericRequestStepResult<GenericRequestSequenceOption>.Empty(DateTimeOffset.UtcNow),
            Reading = reading ?? GenericRequestQuantityReading.Unavailable(DateTimeOffset.UtcNow),
            PartWideReading = partWideReading ?? GenericRequestQuantityReading.Unavailable(DateTimeOffset.UtcNow),
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

        /// <summary>
        /// The answer a coil's or a flatstock's part-wide quantity read gets, which is a different question from
        /// <see cref="Reading"/>: it has no sequence to read against.
        /// </summary>
        public GenericRequestQuantityReading PartWideReading { get; init; } =
            GenericRequestQuantityReading.Unavailable(DateTimeOffset.UtcNow);

        /// <summary>The part numbers a part-wide read was made for, so a test can prove the sequence path was not used.</summary>
        public List<string> PartWideReads { get; } = new();

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

        public Task<GenericRequestQuantityReading> ReadPartQuantityAsync(
            string partNumber,
            CancellationToken cancellationToken = default)
        {
            PartWideReads.Add(partNumber);
            return Task.FromResult(PartWideReading);
        }
    }
}
