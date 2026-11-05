using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Shared.ViewModels;
using MTM_Waitlist.Tests.Module_Settings;

namespace MTM_Waitlist.Tests.Module_Shared.ViewModels;

/// <summary>
/// The shared dunnage catalogue: one read, one filter, one picture choice, one selection.
/// </summary>
/// <remarks>
/// Both surfaces draw this view model, so these checks are the assertions the page's gates rely on: a failed read
/// is never reported as an empty catalogue, a read still in flight is never reported as empty, and nothing is
/// chosen until somebody chooses it.
/// </remarks>
[TestClass]
public sealed class DunnageCatalogueViewModelTests
{
    [TestMethod]
    public async Task ViewModel_DunnageCatalogueViewModelTests_ReportsReadyForAPopulatedRead()
    {
        var viewModel = Create(ReaderReturning(Part("DNG-1"), Part("DNG-2")));

        var result = await viewModel.LoadAsync();

        Assert.AreEqual(DunnageCatalogueLoadOutcome.Ready, result.Outcome);
        Assert.AreEqual(2, viewModel.DisplayedParts.Count);
    }

    [TestMethod]
    public async Task ViewModel_DunnageCatalogueViewModelTests_ReportsEmptyForAReadThatFoundNoParts()
    {
        var viewModel = Create(ReaderReturning());

        var result = await viewModel.LoadAsync();

        Assert.AreEqual(
            DunnageCatalogueLoadOutcome.Empty,
            result.Outcome,
            "A catalogue the plant holds nothing in is an answer, not a failure (FR-012).");
        Assert.IsTrue(viewModel.HasNoResults);
    }

    [TestMethod]
    public async Task ViewModel_DunnageCatalogueViewModelTests_NeverReportsAFailedReadAsEmpty()
    {
        var viewModel = Create(new StubCatalogueReader
        {
            Read = _ => throw new InvalidOperationException("the receiving store is unreachable"),
        });

        var result = await viewModel.LoadAsync();

        Assert.AreEqual(
            DunnageCatalogueLoadOutcome.Unavailable,
            result.Outcome,
            "A read that failed knows nothing about what the plant holds, so it cannot say it holds nothing (FR-007).");
        Assert.AreNotEqual(default, result.AttemptedAtUtc, "The unavailable state carries the time of the attempt.");
        Assert.AreEqual("Failed to load Dunnage parts.", viewModel.EmptyStateMessage);
    }

    [TestMethod]
    public async Task ViewModel_DunnageCatalogueViewModelTests_ReportsNothingAboutThePlantWhileTheReadIsStillInFlight()
    {
        var gate = new TaskCompletionSource<IReadOnlyList<DunnageCataloguePart>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var viewModel = Create(new StubCatalogueReader { Read = _ => gate.Task });

        var pending = viewModel.LoadAsync();

        Assert.IsTrue(viewModel.IsBusy, "A read in flight is reported as busy.");
        Assert.IsFalse(
            viewModel.EmptyStateMessage.Contains("No Dunnage parts", StringComparison.OrdinalIgnoreCase),
            $"A read still loading must not claim the plant holds nothing: '{viewModel.EmptyStateMessage}' (FR-010).");

        gate.SetResult([Part("DNG-1")]);

        Assert.AreEqual(DunnageCatalogueLoadOutcome.Ready, (await pending).Outcome);
        Assert.IsFalse(viewModel.IsBusy);
    }

    [TestMethod]
    public async Task ViewModel_DunnageCatalogueViewModelTests_OneBoxMatchesPartNumberTypeOrLocation()
    {
        var viewModel = Create(ReaderReturning(
            Part("DNG-100", typeName: "Rack", homeLocation: "ROW-1"),
            Part("DNG-200", typeName: "Tote", homeLocation: "ROW-2"),
            Part("BOX-300", typeName: "Crate", homeLocation: "BAY-3")));
        await viewModel.LoadAsync();

        viewModel.FilterText = "dng";
        Assert.AreEqual(2, viewModel.DisplayedParts.Count, "The filter matches the part number, case-insensitively.");

        viewModel.FilterText = "tote";
        CollectionAssert.AreEqual(
            new[] { "DNG-200" },
            viewModel.DisplayedParts.Select(part => part.PartNumber).ToArray());

        viewModel.FilterText = "BAY";
        CollectionAssert.AreEqual(
            new[] { "BOX-300" },
            viewModel.DisplayedParts.Select(part => part.PartNumber).ToArray(),
            "The same box matches the home location.");
    }

    [TestMethod]
    public async Task ViewModel_DunnageCatalogueViewModelTests_SortsByPartNumberIgnoringCase()
    {
        var viewModel = Create(ReaderReturning(Part("dng-3"), Part("DNG-1"), Part("Dng-2")));

        await viewModel.LoadAsync();

        CollectionAssert.AreEqual(
            new[] { "DNG-1", "Dng-2", "dng-3" },
            viewModel.DisplayedParts.Select(part => part.PartNumber).ToArray());
    }

    [TestMethod]
    public async Task ViewModel_DunnageCatalogueViewModelTests_ThePictureChoiceHidesNoPicturePartsBeforeTheFilterApplies()
    {
        var viewModel = Create(ReaderReturning(
            Part("DNG-1", imagePath: "Parts/rack.png"),
            Part("DNG-2"),
            Part("DNG-3", imagePath: "Parts/tote.png")));
        await viewModel.LoadAsync();

        Assert.AreEqual(3, viewModel.DisplayedParts.Count, "The choice shows every part by default.");

        await viewModel.SetShowPartsWithoutImagesAsync(false);

        CollectionAssert.AreEqual(
            new[] { "DNG-1", "DNG-3" },
            viewModel.DisplayedParts.Select(part => part.PartNumber).ToArray());

        viewModel.FilterText = "DNG-2";
        Assert.AreEqual(
            0,
            viewModel.DisplayedParts.Count,
            "Narrowing the pictures first leaves nothing for a filter that only the hidden part matched.");
    }

    [TestMethod]
    public async Task ViewModel_DunnageCatalogueViewModelTests_NothingIsSelectedUntilSomebodySelectsIt()
    {
        var viewModel = Create(ReaderReturning(Part("DNG-1")));

        await viewModel.LoadAsync();

        Assert.IsNull(
            viewModel.SelectedPart,
            "The old step pre-selected the first part, which is how a part nobody chose reached the request.");
    }

    [TestMethod]
    public async Task ViewModel_DunnageCatalogueViewModelTests_ThePictureChoiceIsRememberedAgainstThePerson()
    {
        var preferences = new InMemoryScopedPreferenceStore();
        var viewModel = new DunnageCatalogueViewModel(ReaderReturning(Part("DNG-1")), preferences);

        await viewModel.SetShowPartsWithoutImagesAsync(false);

        Assert.AreEqual(
            false,
            preferences.FlagFor(DunnageCatalogueViewModel.ShowPartsWithoutImagesSettingsKey, PreferenceScope.Person));

        var onAnotherComputer = new DunnageCatalogueViewModel(ReaderReturning(Part("DNG-1")), preferences);
        await onAnotherComputer.InitializeAsync();

        Assert.IsFalse(onAnotherComputer.ShowPartsWithoutImages, "The choice travels with the person.");
    }

    [TestMethod]
    public async Task ViewModel_DunnageCatalogueViewModelTests_SelectPartNumberChoosesThePartTheDraftRemembers()
    {
        var viewModel = Create(ReaderReturning(Part("DNG-1"), Part("DNG-2")));
        await viewModel.LoadAsync();

        Assert.IsTrue(viewModel.SelectPartNumber("dng-2"), "A part already in the catalogue is chosen again on a revisit.");
        Assert.AreEqual("DNG-2", viewModel.SelectedPart?.PartNumber);

        Assert.IsFalse(
            viewModel.SelectPartNumber("DNG-GONE"),
            "A part that has left the catalogue cannot be chosen, and the caller keeps the draft value it had.");
    }

    private static DunnageCatalogueViewModel Create(StubCatalogueReader reader) =>
        new(reader, new InMemoryScopedPreferenceStore());

    private static StubCatalogueReader ReaderReturning(params DunnageCataloguePart[] parts) =>
        new() { Read = _ => Task.FromResult<IReadOnlyList<DunnageCataloguePart>>(parts) };

    private static DunnageCataloguePart Part(
        string partNumber,
        string typeName = "Rack",
        string homeLocation = "ROW-1",
        string imagePath = "") =>
        new()
        {
            Id = $"ID-{partNumber}",
            TypeId = "TYPE-1",
            PartNumber = partNumber,
            DisplayName = partNumber,
            DunnageTypeName = typeName,
            HomeLocation = homeLocation,
            ImagePath = imagePath,
        };

    private sealed class StubCatalogueReader : IDunnageCatalogueReader
    {
        public Func<CancellationToken, Task<IReadOnlyList<DunnageCataloguePart>>> Read { get; set; } =
            _ => Task.FromResult<IReadOnlyList<DunnageCataloguePart>>(Array.Empty<DunnageCataloguePart>());

        public int ReadCount { get; private set; }

        public Task<IReadOnlyList<DunnageCataloguePart>> ReadAllAsync(CancellationToken cancellationToken = default)
        {
            ReadCount++;
            return Read(cancellationToken);
        }
    }
}
