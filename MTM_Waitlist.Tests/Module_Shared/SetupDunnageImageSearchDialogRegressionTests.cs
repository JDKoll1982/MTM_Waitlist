using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Setup.ViewModels;
using MTM_Waitlist.Tests.Module_GenericRequest;
using MTM_Waitlist.Tests.Module_Mock;
using MTM_Waitlist.Tests.Module_Settings;

namespace MTM_Waitlist.Tests.Module_Shared;

/// <summary>
/// The substitute dialog after the catalogue was extracted from it: same parts, same sort, same one box, same
/// pictures choice, same accept rule, same words.
/// </summary>
/// <remarks>
/// The extraction is the risky part of this change, because the dialog is the surface the plant already uses every
/// shift. These checks pin what a person and an automation can observe of it, so a later change to the shared
/// control cannot quietly alter the dialog while the page moves on (FR-002, FR-003).
/// </remarks>
[TestClass]
public sealed class SetupDunnageImageSearchDialogRegressionTests
{
    private static readonly string[] s_automationIds =
    [
        "SetupDunnageImageSearchDialog_FilterInput",
        "SetupDunnageImageSearchDialog_RefreshButton",
        "SetupDunnageImageSearchDialog_ShowAllToggle",
        "SetupDunnageImageSearchDialog_PartTiles",
    ];

    private static string Root => RepositoryPatternScan.FindRepositoryRoot();

    [TestMethod]
    public void Markup_SetupDunnageImageSearchDialogRegressionTests_TheFourAutomationIdsSurviveTheExtraction()
    {
        var control = Read("Module_Shared/Controls/DunnageCatalogueView.xaml");

        foreach (var automationId in s_automationIds)
        {
            StringAssert.Contains(
                control,
                automationId,
                $"'{automationId}' is what the dialog's automation targets, so it has to move with the markup.");
        }

        StringAssert.Contains(
            Read("Module_Setup/Views/SetupDunnageImageSearchDialog.xaml"),
            "<sharedControls:DunnageCatalogueView",
            "The dialog no longer hosts the shared catalogue, so it has become a second list.");
    }

    [TestMethod]
    public void Markup_SetupDunnageImageSearchDialogRegressionTests_TheAcceptButtonIsClosedUntilAPartIsChosen()
    {
        var codeBehind = Read("Module_Setup/Views/SetupDunnageImageSearchDialog.xaml.cs");

        StringAssert.Contains(codeBehind, "IsPrimaryButtonEnabled = false");
        StringAssert.Contains(
            codeBehind,
            "ViewModel.SelectedPart is not null",
            "The accept button is no longer the chosen-part gate, so a dismissed-with-nothing pick could be accepted.");
    }

    [TestMethod]
    public async Task Service_SetupDunnageImageSearchDialogRegressionTests_TheCatalogueSortsAndFiltersAsItAlwaysHas()
    {
        var reader = new FakeDunnageCatalogueReader
        {
            Parts =
            [
                FakeDunnageCataloguePart.Of("dng-3", typeName: "Tote", imagePath: "Parts/tote.png"),
                FakeDunnageCataloguePart.Of("DNG-1", typeName: "Rack", imagePath: "Parts/rack.png"),
                FakeDunnageCataloguePart.Of("Dng-2", typeName: "Crate", imagePath: "Parts/crate.png"),
            ],
        };
        var viewModel = new SetupDunnageImageSearchDialogViewModel(reader, new InMemoryScopedPreferenceStore());

        await viewModel.InitializeAsync();

        CollectionAssert.AreEqual(
            new[] { "DNG-1", "Dng-2", "dng-3" },
            viewModel.DisplayedParts.Select(part => part.PartNumber).ToArray(),
            "The dialog still sorts by part number, case-insensitively.");

        viewModel.FilterText = "tote";
        CollectionAssert.AreEqual(
            new[] { "dng-3" },
            viewModel.DisplayedParts.Select(part => part.PartNumber).ToArray(),
            "The one box still matches the dunnage type.");

        viewModel.FilterText = "ROW-1";
        Assert.AreEqual(3, viewModel.DisplayedParts.Count, "The same box still matches the home location.");
    }

    [TestMethod]
    public async Task Service_SetupDunnageImageSearchDialogRegressionTests_ThePicturesChoiceStillStartsOnAndTravelsWithThePerson()
    {
        var preferences = new InMemoryScopedPreferenceStore();
        var reader = new FakeDunnageCatalogueReader
        {
            Parts =
            [
                FakeDunnageCataloguePart.Of("DNG-1", imagePath: "Parts/rack.png"),
                FakeDunnageCataloguePart.Of("DNG-2"),
            ],
        };
        var viewModel = new SetupDunnageImageSearchDialogViewModel(reader, preferences);

        await viewModel.InitializeAsync();

        Assert.IsTrue(viewModel.ShowPartsWithoutImages, "The dialog still opens showing every part.");
        Assert.AreEqual(2, viewModel.DisplayedParts.Count);

        await viewModel.HandleShowPartsWithoutImagesChangedAsync(false);

        Assert.AreEqual(
            false,
            preferences.FlagFor(SetupDunnageImageSearchDialogViewModel.SettingsKey, PreferenceScope.Person),
            "The choice is still remembered against the person under the same key.");

        var nextShift = new SetupDunnageImageSearchDialogViewModel(reader, preferences);
        await nextShift.InitializeAsync();

        Assert.IsFalse(nextShift.ShowPartsWithoutImages);
        Assert.AreEqual(1, nextShift.DisplayedParts.Count, "Images-only still hides the uncaptured part.");
    }

    [TestMethod]
    public async Task Service_SetupDunnageImageSearchDialogRegressionTests_ChoosingAPartStillRaisesTheAcceptGate()
    {
        var viewModel = new SetupDunnageImageSearchDialogViewModel(
            new FakeDunnageCatalogueReader { Parts = [FakeDunnageCataloguePart.Of("DNG-1")] },
            new InMemoryScopedPreferenceStore());
        await viewModel.InitializeAsync();

        var raised = new List<string?>();
        viewModel.PropertyChanged += (_, args) => raised.Add(args.PropertyName);

        Assert.IsNull(viewModel.SelectedPart, "Nothing is chosen until a card is clicked.");

        viewModel.SelectPart(viewModel.DisplayedParts[0]);

        Assert.AreEqual("DNG-1", viewModel.SelectedPart?.PartNumber);
        CollectionAssert.Contains(
            raised,
            nameof(SetupDunnageImageSearchDialogViewModel.SelectedPart),
            "The host is told, or the accept button never opens and the picker can never return a part.");
    }

    [TestMethod]
    public async Task Service_SetupDunnageImageSearchDialogRegressionTests_AFailedReadStillSaysTheOneFlatSentence()
    {
        var viewModel = new SetupDunnageImageSearchDialogViewModel(
            new FakeDunnageCatalogueReader { Failure = new InvalidOperationException("unreachable") },
            new InMemoryScopedPreferenceStore());

        await viewModel.InitializeAsync();

        Assert.AreEqual("Failed to load Dunnage parts.", viewModel.EmptyStateMessage);
        StringAssert.DoesNotMatch(
            viewModel.EmptyStateMessage,
            new System.Text.RegularExpressions.Regex("could not be read at"),
            "The dialog keeps its flat sentence; the page is the surface that names the attempt time.");
    }

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
}
