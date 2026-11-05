using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.ViewModels;
using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Shared.ViewModels;
using MTM_Waitlist.Tests.Module_Settings;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// The dunnage step: it draws the shared catalogue, maps its three outcomes onto the frame's presentations, gates
/// the forward button on an actual choice, and writes that choice into the draft.
/// </summary>
[TestClass]
public sealed class GenericRequestDunnageViewModelTests
{
    [TestMethod]
    public async Task ViewModel_GenericRequestDunnageViewModelTests_APopulatedCatalogueIsPopulatedAndAnEmptyOneIsEmpty()
    {
        var populated = Create([Part("DNG-1")], out _);
        await populated.LoadAsync();
        Assert.AreEqual(GenericRequestPresentation.Populated, populated.Presentation);

        var empty = Create([], out _);
        await empty.LoadAsync();
        Assert.AreEqual(
            GenericRequestPresentation.Empty,
            empty.Presentation,
            "A catalogue the plant holds nothing in is an answer, not a failure (FR-012).");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestDunnageViewModelTests_AFailedCatalogueIsUnavailableWithTheAttemptTime()
    {
        var viewModel = Create([], out _, fail: true);

        await viewModel.LoadAsync();

        Assert.AreEqual(GenericRequestPresentation.Unavailable, viewModel.Presentation);
        Assert.IsNotNull(viewModel.AttemptedAtUtc, "The unavailable state carries when the read was attempted.");

        // The words come from the pinned key and the attempt time rides alongside them (FR-007). A test host
        // resolves no resources, so GetLocalized answers the key itself and there is no {0} for the time to fill;
        // what is asserted here is that the step owns a sentence of its own for this state, rather than leaving
        // the frame's generic heading as the whole of what a person is told.
        StringAssert.Contains(viewModel.UnavailableText, "GenericRequest_Dunnage_Unavailable.Text");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestDunnageViewModelTests_TheForwardButtonIsClosedUntilAPartIsChosen()
    {
        var viewModel = Create([Part("DNG-1")], out _);
        await viewModel.LoadAsync();

        Assert.IsFalse(
            viewModel.ContinueCommand.CanExecute(null),
            "A dunnage step with nothing chosen must not be left: a request carrying no part names nothing (FR-006).");

        viewModel.Catalogue.SelectPart(Part("DNG-1"));

        Assert.IsTrue(
            viewModel.ContinueCommand.CanExecute(null),
            "Choosing a part is what opens the forward button (FR-004).");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestDunnageViewModelTests_ChoosingAPartWritesItsTypeAndNumberIntoTheDraft()
    {
        var viewModel = Create([Part("DNG-1", typeName: "Rack")], out var navigator);
        await viewModel.LoadAsync();

        viewModel.Catalogue.SelectPart(Part("DNG-1", typeName: "Rack"));
        await viewModel.ContinueCommand.ExecuteAsync(null);

        Assert.AreEqual("DNG-1", navigator.Draft.DunnagePart);
        Assert.AreEqual("Rack", navigator.Draft.DunnageType);
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestDunnageViewModelTests_RevisitingShowsThePartTheDraftAlreadyRemembers()
    {
        var viewModel = Create([Part("DNG-1"), Part("DNG-2")], out var navigator);
        navigator.Draft.DunnagePart = "DNG-2";

        await viewModel.LoadAsync();

        Assert.AreEqual("DNG-2", viewModel.Catalogue.SelectedPart?.PartNumber);
        StringAssert.Contains(viewModel.SelectedPartDisplay, "DNG-2", "The page shows the part as the chosen one.");
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestDunnageViewModelTests_NothingIsChosenOnAFreshVisit()
    {
        var viewModel = Create([Part("DNG-1")], out _);

        await viewModel.LoadAsync();

        Assert.IsNull(viewModel.Catalogue.SelectedPart, "Nothing is pre-selected, so nothing unchosen can be recorded.");
        Assert.AreEqual(string.Empty, viewModel.SelectedPartDisplay);
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestDunnageViewModelTests_APartGoneFromAFreshReadKeepsTheDraftValue()
    {
        var viewModel = Create([Part("DNG-1")], out var navigator);
        navigator.Draft.DunnagePart = "DNG-GONE";
        navigator.Draft.DunnageType = "Rack";

        await viewModel.LoadAsync();

        Assert.AreEqual(
            "DNG-GONE",
            navigator.Draft.DunnagePart,
            "A part that has left the catalogue is kept on the request rather than silently dropped (FR-009).");
        Assert.IsNull(viewModel.Catalogue.SelectedPart);
    }

    [TestMethod]
    public async Task ViewModel_GenericRequestDunnageViewModelTests_LeavingWithoutAPartRecordsNothing()
    {
        var viewModel = Create([Part("DNG-1")], out var navigator);
        await viewModel.LoadAsync();

        // The command cannot execute without a choice, which is the gate; executing it anyway must still record
        // nothing, so a caller that bypasses the gate cannot raise a request naming no dunnage (FR-006).
        await viewModel.ContinueCommand.ExecuteAsync(null);

        Assert.AreEqual(string.Empty, navigator.Draft.DunnagePart);
        Assert.AreEqual(string.Empty, navigator.Draft.DunnageType);
    }

    private static GenericRequestDunnageViewModel Create(
        DunnageCataloguePart[] parts,
        out StubGenericRequestNavigator navigator,
        bool fail = false)
    {
        var reader = new StubCatalogueReader
        {
            Read = fail
                ? _ => throw new InvalidOperationException("the receiving store is unreachable")
                : _ => Task.FromResult<IReadOnlyList<DunnageCataloguePart>>(parts),
        };

        navigator = new StubGenericRequestNavigator();

        return new GenericRequestDunnageViewModel(
            new DunnageCatalogueViewModel(reader, new InMemoryScopedPreferenceStore()),
            navigator);
    }

    private static DunnageCataloguePart Part(string partNumber, string typeName = "Rack") =>
        new()
        {
            Id = $"ID-{partNumber}",
            TypeId = "TYPE-1",
            PartNumber = partNumber,
            DisplayName = partNumber,
            DunnageTypeName = typeName,
            HomeLocation = "ROW-1",
        };

    private sealed class StubCatalogueReader : IDunnageCatalogueReader
    {
        public Func<CancellationToken, Task<IReadOnlyList<DunnageCataloguePart>>> Read { get; set; } =
            _ => Task.FromResult<IReadOnlyList<DunnageCataloguePart>>(Array.Empty<DunnageCataloguePart>());

        public Task<IReadOnlyList<DunnageCataloguePart>> ReadAllAsync(CancellationToken cancellationToken = default) =>
            Read(cancellationToken);
    }
}
