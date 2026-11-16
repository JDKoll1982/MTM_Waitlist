using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.GenericRequest.ViewModels;
using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Shared.ViewModels;
using MTM_Waitlist.Tests.Module_Settings;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// What accepting the offer puts in front of the requester, and what changing an answer takes away.
/// </summary>
/// <remarks>
/// The fill supplies candidates and never a bypass, so every step here is driven through its own load and its own
/// reads: a seeded part is still completed, listed and measured, a seeded die is still searched for, and a seeded
/// dunnage part is still drawn from the catalogue. Changing one answer stops that one reading as the job's, and
/// changing the item drops every answer that belonged to the previous one.
/// </remarks>
[TestClass]
public sealed class GenericRequestJobFillSeedingTests
{
    [TestMethod]
    public async Task ViewModel_GenericRequestJobFillSeedingTests_AcceptFillCommand_SeedsTheJobsAnswersAndNoneOfTheRequestsIdentity()
    {
        var data = new StubGenericRequestStepData
        {
            Categories = GenericRequestStepResult<GenericRequestCategoryOption>.Populated(
                [new GenericRequestCategoryOption("Pickup", "Pickup", "Move it")],
                DateTimeOffset.UtcNow),
        };
        var fill = new StubGenericRequestJobFillData
        {
            RunningJobWorkOrder = "WO-9001",
            Fill = GenericRequestJobFill.FromJob("WO-9001", "PART-1", "20", "FGT0602-01", "Rack", "DNG0007788"),
        };
        var navigator = new StubGenericRequestNavigator();
        navigator.Draft.WorkCenter = "WC-1";
        navigator.Draft.Building = "Expo Drive";

        var viewModel = new GenericRequestCategoryViewModel(data, fill, navigator);

        await viewModel.LoadAsync();
        await viewModel.AcceptFillCommand.ExecuteAsync(null);

        var draft = navigator.Draft;

        Assert.IsTrue(viewModel.IsFilledVisible, "the step says the request was filled (FR-006)");
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.FilledLine));
        Assert.AreEqual("WO-9001", draft.JobFill?.WorkOrder);

        Assert.AreEqual("PART-1", draft.JobFill!.PartNumber, "the job's part is the candidate for the part step");
        Assert.AreEqual("20", draft.JobFill.Sequence);
        Assert.AreEqual("FGT0602-01", draft.JobFill.DieNumber);
        Assert.AreEqual("DNG0007788", draft.JobFill.DunnagePart);

        Assert.AreEqual(string.Empty, draft.Category, "the fill never chooses the category (FR-011)");
        Assert.AreEqual(string.Empty, draft.ItemCode, "the fill never chooses the item (FR-011)");
        Assert.AreEqual("WC-1", draft.WorkCenter);
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestJobFillSeedingTests_PartStep_OffersTheJobsPartAndSequenceAndStillProvesThem()
    {
        var partData = new StubPartData { Completions = [Part("PART-1")], Sequences = [Sequence("20")] };
        var navigator = Draft(GenericRequestJobFill.FromJob("WO-9001", "PART-1", "20"));
        var viewModel = new GenericRequestPartViewModel(partData, navigator);

        await viewModel.LoadAsync();

        Assert.AreEqual("PART-1", viewModel.AcceptedPart?.PartNumber, "the job's part is the step's candidate (FR-005)");
        Assert.AreEqual("20", viewModel.SelectedSequence?.Sequence, "the job's sequence is chosen with it");
        Assert.IsTrue(viewModel.HasFromJob, "the step says where the part came from (FR-006)");
        Assert.AreEqual(1, partData.CompletionReads, "the seeded part is completed against both systems (FR-010)");
        Assert.AreEqual(1, partData.SequenceReads, "and its sequences are still read (FR-010)");
        Assert.AreEqual(1, partData.QuantityReads, "and its quantity is still measured (FR-010)");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestJobFillSeedingTests_PartStep_StatingAnotherPartStopsItReadingAsTheJobs()
    {
        var partData = new StubPartData { Completions = [Part("PART-1"), Part("PART-2")] };
        var navigator = Draft(GenericRequestJobFill.FromJob("WO-9001", "PART-1", "20"));
        var viewModel = new GenericRequestPartViewModel(partData, navigator);

        await viewModel.LoadAsync();
        await viewModel.AcceptPartAsync(Part("PART-2"));

        Assert.IsFalse(viewModel.HasFromJob, "a different part is the requester's own statement (FR-007)");
        Assert.IsFalse(navigator.Draft.JobFill!.IsPartFromJob);
        Assert.IsFalse(navigator.Draft.JobFill.IsSequenceFromJob, "the sequence goes with the part it belonged to");
        Assert.AreEqual("WO-9001", navigator.Draft.JobFill.WorkOrder, "the job is still where the offer came from");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestJobFillSeedingTests_DieStep_MarksTheJobsDieAndStillSearchesForIt()
    {
        var data = new StubGenericRequestStepData
        {
            Dies = GenericRequestStepResult<GenericRequestDieOption>.Populated(
                [new GenericRequestDieOption("FGT0602-01", "Die one", "ROW-2")],
                DateTimeOffset.UtcNow),
        };
        var navigator = Draft(GenericRequestJobFill.FromJob("WO-9001", dieNumber: "FGT0602-01"));
        var viewModel = new GenericRequestDieViewModel(data, navigator);

        await viewModel.LoadAsync();

        Assert.AreEqual(1, viewModel.Options.Count(option => option.IsSelected), "the job's die is marked (FR-005)");
        Assert.AreEqual("FGT0602-01", viewModel.Options.Single(option => option.IsSelected).DieNumber);
        Assert.IsTrue(viewModel.HasFromJob);
        CollectionAssert.Contains(data.DieSearches, "FGT0602-01");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestJobFillSeedingTests_DieStep_ChoosingADifferentDieStopsItReadingAsTheJobs()
    {
        var data = new StubGenericRequestStepData
        {
            Dies = GenericRequestStepResult<GenericRequestDieOption>.Populated(
                [
                    new GenericRequestDieOption("FGT0602-01", "Die one", "ROW-2"),
                    new GenericRequestDieOption("FGT0999-01", "Die two", "ROW-3"),
                ],
                DateTimeOffset.UtcNow),
        };
        var navigator = Draft(GenericRequestJobFill.FromJob("WO-9001", dieNumber: "FGT0602-01"));
        var viewModel = new GenericRequestDieViewModel(data, navigator);

        await viewModel.LoadAsync();
        viewModel.ToggleDieCommand.Execute(viewModel.Options.Single(option => option.DieNumber == "FGT0602-01"));
        viewModel.ToggleDieCommand.Execute(viewModel.Options.Single(option => option.DieNumber == "FGT0999-01"));

        Assert.IsFalse(viewModel.HasFromJob, "a die the job does not carry is the requester's own choice (FR-007)");
        Assert.IsFalse(navigator.Draft.JobFill!.IsDieFromJob);
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestJobFillSeedingTests_DunnageStep_OffersTheJobsPartAndDropsTheQualifierWhenAnotherIsChosen()
    {
        var parts = new[]
        {
            DunnagePart("DNG0007788"),
            DunnagePart("DNG0007789"),
        };
        var navigator = Draft(GenericRequestJobFill.FromJob("WO-9001", dunnageType: "Rack", dunnagePart: "DNG0007788"));
        var viewModel = new GenericRequestDunnageViewModel(
            new DunnageCatalogueViewModel(new StubCatalogueReader(parts), new InMemoryScopedPreferenceStore()),
            navigator);

        await viewModel.LoadAsync();

        Assert.AreEqual("DNG0007788", viewModel.Catalogue.SelectedPart?.PartNumber, "the job's dunnage is chosen (FR-005)");
        Assert.IsTrue(viewModel.HasFromJob);

        viewModel.Catalogue.SelectPartNumber("DNG0007789");

        Assert.IsFalse(viewModel.HasFromJob, "a different dunnage part is the requester's own choice (FR-007)");
        Assert.IsFalse(navigator.Draft.JobFill!.IsDunnageFromJob);
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestJobFillSeedingTests_ItemStep_ChangingTheItemDropsTheFillAndItsAnswers()
    {
        var data = new StubGenericRequestStepData
        {
            Items = GenericRequestStepResult<GenericRequestItemOption>.Populated(
                [Item("pickup-part", needsPart: true), Item("pickup-part-two", needsPart: true)],
                DateTimeOffset.UtcNow),
        };
        var navigator = Draft(GenericRequestJobFill.FromJob("WO-9001", "PART-1", "20"));
        navigator.Draft.Category = "Pickup";
        navigator.Draft.ItemCode = "pickup-part";

        var viewModel = new GenericRequestItemViewModel(data, navigator);

        await viewModel.LoadAsync();
        viewModel.Selected = viewModel.Options.Single(option => option.Code == "pickup-part-two");

        Assert.IsNull(navigator.Draft.JobFill, "every answer the job supplied belonged to the previous item (FR-009)");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestJobFillSeedingTests_PartStep_AJobThatClosedStillLeavesTheValuesItSupplied()
    {
        var partData = new StubPartData { Completions = [Part("PART-1")], Sequences = [Sequence("20")] };
        var navigator = Draft(GenericRequestJobFill.FromJob("WO-9001", "PART-1", "20"));
        var viewModel = new GenericRequestPartViewModel(partData, navigator);

        await viewModel.LoadAsync();

        // The job is gone from the store now, and nothing asks about it again: the fill is a snapshot of what was
        // shown, not a live reference (FR-014).
        Assert.AreEqual("PART-1", navigator.Draft.PartNumber);
        Assert.AreEqual("20", navigator.Draft.Sequence);
        Assert.IsTrue(viewModel.ContinueCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestJobFillSeedingTests_ItemStep_TheFirstItemPickedKeepsTheFillAndAChangeDropsIt()
    {
        var data = new StubGenericRequestStepData
        {
            Items = GenericRequestStepResult<GenericRequestItemOption>.Populated(
                [Item("pickup-part", needsPart: true), Item("pickup-part-two", needsPart: true)],
                DateTimeOffset.UtcNow),
        };
        var navigator = Draft(GenericRequestJobFill.FromJob("WO-9001", "PART-1", "20"));
        navigator.Draft.Category = "Pickup";

        var viewModel = new GenericRequestItemViewModel(data, navigator);

        await viewModel.LoadAsync();

        // The first item a request ever picks is not a change: there is nothing before it to carry anything over from.
        viewModel.Selected = viewModel.Options.Single(option => option.Code == "pickup-part");

        Assert.IsNotNull(navigator.Draft.JobFill, "picking the first item must not throw the fill away");

        await viewModel.ContinueCommand.ExecuteAsync(null);
        viewModel.Selected = viewModel.Options.Single(option => option.Code == "pickup-part-two");

        Assert.IsNull(navigator.Draft.JobFill, "a changed item drops every answer that belonged to the previous one (FR-009)");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestJobFillSeedingTests_DieStep_ADieTheSearchDoesNotReturnIsNeverChosen()
    {
        var data = new StubGenericRequestStepData
        {
            Dies = GenericRequestStepResult<GenericRequestDieOption>.Empty(DateTimeOffset.UtcNow),
        };
        var navigator = Draft(GenericRequestJobFill.FromJob("WO-9001", dieNumber: "FGT0602-01"));
        var viewModel = new GenericRequestDieViewModel(data, navigator);

        await viewModel.LoadAsync();

        Assert.IsFalse(viewModel.HasFromJob, "a die the source does not record is not offered (FR-010)");
        Assert.IsFalse(viewModel.ContinueCommand.CanExecute(null), "and the step cannot advance with no die chosen");
        Assert.IsTrue(viewModel.HasNoMatch);
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestJobFillSeedingTests_PartStep_APartTheCompletionsDoNotReturnIsNeverWrittenToTheDraft()
    {
        var partData = new StubPartData { Completions = [Part("PART-2")] };
        var navigator = Draft(GenericRequestJobFill.FromJob("WO-9001", "PART-1", "20"));
        var viewModel = new GenericRequestPartViewModel(partData, navigator);

        await viewModel.LoadAsync();

        Assert.IsNull(viewModel.AcceptedPart, "an unproved part is not accepted (FR-010)");
        Assert.AreEqual(string.Empty, navigator.Draft.PartNumber, "and nothing is written to the draft");
        Assert.AreEqual(string.Empty, navigator.Draft.Sequence);
        Assert.IsFalse(viewModel.ContinueCommand.CanExecute(null), "so the step waits for the requester");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestJobFillSeedingTests_AValueTheJobDoesNotCarryIsStillAskedOfTheRequester()
    {
        // The job names itself and states nothing else, so every step still asks for its own answer (FR-008).
        var partData = new StubPartData { Completions = [Part("PART-1")], Sequences = [Sequence("20")] };
        var partNavigator = Draft(GenericRequestJobFill.FromJob("WO-9001"));
        var partViewModel = new GenericRequestPartViewModel(partData, partNavigator);

        await partViewModel.LoadAsync();

        Assert.IsFalse(partViewModel.HasFromJob, "the job stated no part, so nothing reads as the job's");
        Assert.IsNull(partViewModel.AcceptedPart, "and no part arrives from it");
        Assert.AreEqual(string.Empty, partNavigator.Draft.PartNumber, "the requester still has to state the part");
        Assert.IsFalse(partViewModel.ContinueCommand.CanExecute(null), "so the step waits for them");

        var dieData = new StubGenericRequestStepData
        {
            Dies = GenericRequestStepResult<GenericRequestDieOption>.Populated(
                [new GenericRequestDieOption("FGT0602-01", "Die one", "ROW-2")],
                DateTimeOffset.UtcNow),
        };
        var dieNavigator = Draft(GenericRequestJobFill.FromJob("WO-9001"));
        var dieViewModel = new GenericRequestDieViewModel(dieData, dieNavigator);

        await dieViewModel.LoadAsync();

        Assert.IsFalse(dieViewModel.HasFromJob, "the job stated no die, so nothing reads as the job's");
        Assert.AreEqual(0, dieViewModel.Options.Count(option => option.IsSelected), "no die is chosen for the requester");
        Assert.IsFalse(dieViewModel.ContinueCommand.CanExecute(null), "so the step still asks for one");

        var dunnageNavigator = Draft(GenericRequestJobFill.FromJob("WO-9001"));
        var dunnageViewModel = new GenericRequestDunnageViewModel(
            new DunnageCatalogueViewModel(
                new StubCatalogueReader([DunnagePart("DNG0007788")]),
                new InMemoryScopedPreferenceStore()),
            dunnageNavigator);

        await dunnageViewModel.LoadAsync();

        Assert.IsFalse(dunnageViewModel.HasFromJob, "the job stated no dunnage, so nothing reads as the job's");
        Assert.IsNull(dunnageViewModel.Catalogue.SelectedPart, "and no dunnage part is chosen for the requester");
    }

    private static GenericRequestPartOption Part(string partNumber) =>
        new(partNumber, string.Empty, SequenceSource.VisualOwnPart);

    private static GenericRequestSequenceOption Sequence(string sequence) =>
        new(sequence, string.Empty, [SequenceSource.VisualOwnPart], "R", false, string.Empty, HasCancelledOnlyOperations: false);

    private static GenericRequestItemOption Item(string code, bool needsPart) => new(
        Code: code,
        Name: code,
        NeedsPart: needsPart,
        NeedsDunnage: false,
        NeedsScrap: false,
        NeedsDie: false,
        PromptText: string.Empty,
        AnswerKind: GenericRequestAnswerKind.None,
        AnswerOptions: Array.Empty<string>(),
        MinLength: 0,
        MaxLength: 0,
        AllottedMinutes: null);

    private static DunnageCataloguePart DunnagePart(string partNumber) => new()
    {
        Id = $"ID-{partNumber}",
        TypeId = "TYPE-1",
        PartNumber = partNumber,
        DisplayName = partNumber,
        DunnageTypeName = "Rack",
        HomeLocation = "ROW-1",
    };

    private static StubGenericRequestNavigator Draft(GenericRequestJobFill? fill)
    {
        var navigator = new StubGenericRequestNavigator();
        navigator.Draft.WorkCenter = "WC-1";
        navigator.Draft.Building = "Expo Drive";
        navigator.Draft.JobFill = fill;
        navigator.Draft.JobFillAnswered = true;

        return navigator;
    }

    /// <summary>The part step's three answers, held in memory, counting what the step asked it.</summary>
    private sealed class StubPartData : IGenericRequestPartData
    {
        public IReadOnlyList<GenericRequestPartOption> Completions { get; init; } = Array.Empty<GenericRequestPartOption>();

        public IReadOnlyList<GenericRequestSequenceOption> Sequences { get; init; } = Array.Empty<GenericRequestSequenceOption>();

        public int CompletionReads { get; private set; }

        public int SequenceReads { get; private set; }

        public int QuantityReads { get; private set; }

        public Task<GenericRequestStepResult<GenericRequestPartOption>> ReadCompletionsAsync(
            string term,
            CancellationToken cancellationToken = default)
        {
            CompletionReads++;

            return Task.FromResult(Completions.Count == 0
                ? GenericRequestStepResult<GenericRequestPartOption>.Empty(DateTimeOffset.UtcNow)
                : GenericRequestStepResult<GenericRequestPartOption>.Populated(Completions, DateTimeOffset.UtcNow));
        }

        public Task<GenericRequestStepResult<GenericRequestSequenceOption>> ReadSequencesAsync(
            string partNumber,
            CancellationToken cancellationToken = default)
        {
            SequenceReads++;

            return Task.FromResult(Sequences.Count == 0
                ? GenericRequestStepResult<GenericRequestSequenceOption>.Empty(DateTimeOffset.UtcNow)
                : GenericRequestStepResult<GenericRequestSequenceOption>.Populated(Sequences, DateTimeOffset.UtcNow));
        }

        public Task<GenericRequestQuantityReading> ReadQuantityAsync(
            string partNumber,
            string sequence,
            CancellationToken cancellationToken = default)
        {
            QuantityReads++;

            return Task.FromResult(
                GenericRequestQuantityReading.OnHand(5m, SequenceSource.WipFloor, GenericRequestPartStage.Wip, DateTimeOffset.UtcNow));
        }
    }

    private sealed class StubCatalogueReader(DunnageCataloguePart[] parts) : IDunnageCatalogueReader
    {
        public Task<IReadOnlyList<DunnageCataloguePart>> ReadAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DunnageCataloguePart>>(parts);
    }
}
