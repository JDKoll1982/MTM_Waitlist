using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Navigation;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Shared.Helpers;
using MTM_Waitlist.Module_Waitlist.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;
using MTM_Waitlist.Tests.Module_Settings;
using MTM_Waitlist.Tests.Module_Waitlist.Helpers;

namespace MTM_Waitlist.Tests.Module_Waitlist.ViewModels;

/// <summary>
/// FR-077, FR-079, FR-080: both hopper Items ask for the size as three choices — Small, Medium and Large — with a
/// reference image of the chosen size drawn beneath them, held to the application's existing picture rules, and
/// drawing the one shared placeholder when that size's image is missing or unusable.
/// </summary>
/// <remarks>
/// The step resolves each picture through the same reader every other New Request surface uses, and records the
/// answer under the label the Item's configuration declares — which is what the card's <c>Size</c> cell resolves
/// (<c>data-model.md</c> §9, §10).
/// </remarks>
[TestClass]
public sealed class NewRequestHopperViewModelTests
{
    private string _directory = string.Empty;

    [TestInitialize]
    public void TestInitialize() => _directory = ImageHeaderFixtures.CreateTemporaryDirectory();

    [TestCleanup]
    public void TestCleanup() => ImageHeaderFixtures.DeleteTemporaryDirectory(_directory);

    [TestMethod]
    public void ViewModel_NewRequestHopperViewModelTests_OnNavigatedTo_OffersTheThreeSizesWithNothingChosen()
    {
        var viewModel = CreateViewModel();

        viewModel.OnNavigatedTo(HopperState());

        CollectionAssert.AreEqual(
            new[] { "Small", "Medium", "Large" },
            viewModel.Sizes.ToArray(),
            "The size is asked as three choices (FR-077).");
        Assert.AreEqual(string.Empty, viewModel.SelectedSize, "Nothing is chosen on the operator's behalf.");
        Assert.AreEqual(
            ImagePicturePolicy.NoImagePath,
            viewModel.SelectedImagePath,
            "With nothing chosen there is no reference image yet, so the step draws the shared placeholder (FR-080).");
        Assert.IsFalse(viewModel.IsUnavailableVisible, "A job-independent Item has nothing to report.");
    }

    [TestMethod]
    public void ViewModel_NewRequestHopperViewModelTests_SelectSize_DrawsThatSizesReferenceImage()
    {
        var small = Picture("small.png", 64, 64);
        var medium = Picture("medium.png", 64, 64);
        var large = Picture("large.png", 64, 64);
        var viewModel = CreateViewModel(new StubPartPictureResolver
        {
            ["Small"] = small,
            ["Medium"] = medium,
            ["Large"] = large,
        });
        viewModel.OnNavigatedTo(HopperState());

        viewModel.IsLargeSelected = true;

        Assert.AreEqual("Large", viewModel.SelectedSize, "Choosing a size is choosing that size.");
        Assert.AreEqual(
            large,
            viewModel.SelectedImagePath,
            "The picture drawn beneath the choices is the chosen size's own reference image (FR-077).");
    }

    [TestMethod]
    public void ViewModel_NewRequestHopperViewModelTests_OnNavigatedTo_AsksForThePictureOfEverySize()
    {
        var resolver = new StubPartPictureResolver
        {
            ["Small"] = Picture("small.png", 64, 64),
            ["Medium"] = Picture("medium.png", 64, 64),
            ["Large"] = Picture("large.png", 64, 64),
        };

        CreateViewModel(resolver).OnNavigatedTo(HopperState());

        CollectionAssert.AreEquivalent(
            new[] { "Small", "Medium", "Large" },
            resolver.Asked.ToArray(),
            "Each size's reference image is resolved as the step loads, so choosing one draws immediately.");
    }

    [TestMethod]
    public void ViewModel_NewRequestHopperViewModelTests_AnUnusablePicture_DrawsTheOneSharedPlaceholder()
    {
        var viewModel = CreateViewModel(new StubPartPictureResolver
        {
            ["Small"] = Picture("tiny.png", 1, 1),
            ["Medium"] = null,
            ["Large"] = Path.Combine(_directory, "missing.png"),
        });
        viewModel.OnNavigatedTo(HopperState());

        viewModel.IsSmallSelected = true;
        Assert.AreEqual(
            ImagePicturePolicy.NoImagePath,
            viewModel.SelectedImagePath,
            "A size whose image is too small to be drawn draws the shared placeholder rather than a blank (FR-079, FR-080).");

        viewModel.IsMediumSelected = true;
        Assert.AreEqual(
            ImagePicturePolicy.NoImagePath,
            viewModel.SelectedImagePath,
            "A size with no image at all draws the shared placeholder (FR-080).");

        viewModel.IsLargeSelected = true;
        Assert.AreEqual(
            ImagePicturePolicy.NoImagePath,
            viewModel.SelectedImagePath,
            "A size whose image file is missing draws the shared placeholder (FR-080).");
    }

    [TestMethod]
    public void ViewModel_NewRequestHopperViewModelTests_Continue_WithNothingChosenRefusesAndStaysOnTheStep()
    {
        var navigation = new RecordingNavigationService();
        var state = HopperState();
        var viewModel = CreateViewModel(navigation: navigation);
        viewModel.OnNavigatedTo(state);

        viewModel.ContinueCommand.Execute(null);

        Assert.IsFalse(state.HasAnswer(NewRequestFlowRules.HopperSizeLabel), "No answer is recorded for nothing chosen.");
        Assert.AreEqual(0, navigation.Navigations.Count, "The step must not advance without a size (FR-026).");
        Assert.IsTrue(viewModel.IsUnavailableVisible, "The step says why it will not move on.");
        Assert.IsFalse(
            viewModel.UnavailableMessage.StartsWith("NewRequest_", StringComparison.Ordinal),
            "The report is plain language, never a bare resource key (FR-022).");
    }

    [TestMethod]
    public void ViewModel_NewRequestHopperViewModelTests_Continue_RecordsTheChosenSizeAndMovesOn()
    {
        var navigation = new RecordingNavigationService();
        var state = HopperState();
        var viewModel = CreateViewModel(navigation: navigation);
        viewModel.OnNavigatedTo(state);

        viewModel.IsMediumSelected = true;
        viewModel.ContinueCommand.Execute(null);

        Assert.AreEqual(
            "Medium",
            state.AnswerFor(NewRequestFlowRules.HopperSizeLabel),
            "The chosen size is captured under the label the Item declares, which is what the card's Size cell reads (FR-077).");
        Assert.AreEqual(
            typeof(NewRequestSummaryViewModel).FullName,
            navigation.Navigations[0].PageKey,
            "With the size captured the flow moves on rather than asking the same question again.");
    }

    [TestMethod]
    public void ViewModel_NewRequestHopperViewModelTests_OnNavigatedTo_ComingBackWithASizeChosenShowsItAgain()
    {
        var state = HopperState();
        state.RecordAnswer(NewRequestFlowRules.HopperSizeLabel, "Large");
        var viewModel = CreateViewModel(new StubPartPictureResolver
        {
            ["Large"] = Picture("large.png", 64, 64),
        });

        viewModel.OnNavigatedTo(state);

        Assert.AreEqual("Large", viewModel.SelectedSize, "Coming back to the step shows the size already chosen.");
        Assert.IsTrue(viewModel.IsLargeSelected, "The chosen size's radio is the chosen one.");
    }

    [TestMethod]
    public void ViewModel_NewRequestHopperViewModelTests_OnNavigatedTo_WithoutAStateLeavesTheStep()
    {
        var navigation = new RecordingNavigationService();
        var viewModel = CreateViewModel(navigation: navigation);

        viewModel.OnNavigatedTo("not a flow state");

        Assert.AreEqual(1, navigation.GoBackCount, "The step cannot run without the request in progress.");
    }

    [TestMethod]
    public void ViewModel_NewRequestHopperViewModelTests_TheSizeLabel_IsACellTheCardDrawsForBothHopperItems()
    {
        // The key the answer is captured under is the label the Item declares AND the label the card draws a
        // cell for; asserting both here is what stops the wizard and the card from drifting apart (FR-035,
        // FR-049, FR-077).
        Assert.IsTrue(
            WaitlistCardCellRules.IsCell("pickup-hopper", NewRequestFlowRules.HopperSizeLabel),
            "The pickup hopper's Size cell is drawn from the answer the step captures.");
        Assert.IsTrue(
            WaitlistCardCellRules.IsCell("deliver-hopper", NewRequestFlowRules.HopperSizeLabel),
            "The deliver hopper's Size cell is drawn from the answer the step captures.");
    }

    private string Picture(string fileName, int width, int height)
    {
        var path = Path.Combine(_directory, fileName);
        TestPngWriter.Write(path, width, height);
        return path;
    }

    private static NewRequestFlowState HopperState() => new()
    {
        WorkCenter = "100-07",
        Category = RequestCategory.Pickup,
        Item = RequestItemCatalog.FindById("pickup-hopper"),
        ItemConfiguration = new RequestItemConfiguration
        {
            Item = "pickup-hopper",
            ControlFlow = RequestItemConfiguration.DirectToConfirmation,
            DetailFields =
            [
                new RequestItemFieldDefinition
                {
                    Label = NewRequestFlowRules.HopperSizeLabel,
                    ValueType = RequestItemValueType.String,
                    Source = RequestItemFieldDefinition.Sources.Answer,
                    Order = 1,
                },
            ],
        },
    };

    private static NewRequestHopperViewModel CreateViewModel(
        StubPartPictureResolver? resolver = null,
        RecordingNavigationService? navigation = null) =>
        new(navigation ?? new RecordingNavigationService(), resolver);

    private sealed class StubPartPictureResolver : IPartPictureResolver
    {
        private readonly Dictionary<string, string?> _paths = new(StringComparer.OrdinalIgnoreCase);

        public List<string> Asked { get; } = [];

        public string? this[string size]
        {
            get => _paths[size];
            set => _paths[size] = value;
        }

        public Task<string?> ResolvePartPicturePathAsync(
            string scope,
            string partNumber,
            CancellationToken cancellationToken = default)
        {
            Asked.Add(partNumber);
            return Task.FromResult(_paths.TryGetValue(partNumber, out var path) ? path : null);
        }

        public string? ResolveRelativeStoredPath(string scope, string partNumber, string extension) => null;
    }

    private sealed class RecordingNavigationService : INavigationService
    {
        public List<(string PageKey, object? Parameter)> Navigations { get; } = new();

        public int GoBackCount { get; private set; }

        public event NavigatedEventHandler? Navigated
        {
            add { }
            remove { }
        }

        public bool CanGoBack => true;

        public Microsoft.UI.Xaml.Controls.Frame? Frame { get; set; }

        public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false)
        {
            Navigations.Add((pageKey, parameter));
            return true;
        }

        public bool GoBack()
        {
            GoBackCount++;
            return true;
        }

        public void SetListDataItemForNextConnectedAnimation(object item)
        {
        }
    }
}
