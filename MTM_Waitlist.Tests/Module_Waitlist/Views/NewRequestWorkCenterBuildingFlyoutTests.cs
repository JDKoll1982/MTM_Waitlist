using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Waitlist.Views;

/// <summary>
/// The work-center step takes its building from the shell, and offers no second choice of its own.
/// </summary>
/// <remarks>
/// The step used to carry its own building button and flyout beside the shell's header selector. Two choosers for one
/// value is how the header comes to say one building while the cards below it come from another, so the page-level one
/// is gone: the shell chooses, and the step reads and follows that choice.
/// </remarks>
[TestClass]
public sealed class NewRequestWorkCenterBuildingFlyoutTests
{
    [TestMethod]
    public void UI_NewRequestWorkCenterBuildingFlyoutTests_TheStep_OffersNoBuildingChoiceOfItsOwn()
    {
        var markup = Markup();

        foreach (var forbidden in new[]
                 {
                     "NewRequestWorkCenterPage_BuildingButton",
                     "NewRequestWorkCenterPage_BuildingCards",
                     "BuildingFlyout_Opening",
                     "BuildingCard_Click",
                     "ViewModel.BuildingCards",
                 })
        {
            Assert.IsFalse(
                markup.Contains(forbidden, StringComparison.Ordinal),
                $"The step still offers its own building choice ('{forbidden}'), so two places can set the same value.");
        }
    }

    [TestMethod]
    public void UI_NewRequestWorkCenterBuildingFlyoutTests_TheStep_ReadsTheShellsBuilding()
    {
        var source = ViewModelSource();

        StringAssert.Contains(
            source,
            "public string SelectedBuilding => _buildingSelectionService.SelectedBuilding;",
            "The step does not read the shell's building, so it has a value of its own again.");

        Assert.IsFalse(
            source.Contains("public void SelectBuilding(", StringComparison.Ordinal),
            "The step can still set the building itself, which is the second chooser coming back under another name.");

        StringAssert.Contains(
            source,
            "var selectedBuilding = _buildingSelectionService.SelectedBuilding;",
            "The filter does not compare each work center against the shell's building.");
    }

    [TestMethod]
    public void UI_NewRequestWorkCenterBuildingFlyoutTests_TheStep_FollowsAChangeMadeWhileItIsOpen()
    {
        var source = ViewModelSource();

        StringAssert.Contains(
            source,
            "_buildingSelectionService.BuildingChanged += OnBuildingChanged;",
            "The step does not listen for the shell changing building, so its cards would stay on the old one.");

        StringAssert.Contains(
            source,
            "private void OnBuildingChanged(object? sender, EventArgs e) => ApplyFilter();",
            "A change of building does not re-apply the filter, so the cards do not follow the header.");
    }

    [TestMethod]
    public void UI_NewRequestWorkCenterBuildingFlyoutTests_TheShell_StillCarriesTheOneChooser()
    {
        var shell = File.ReadAllText(Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Module_Core",
            "Views",
            "ShellPage.xaml"));

        StringAssert.Contains(
            shell,
            "ShellPage_FacilitySelector",
            "The shell no longer offers a building, so nothing on the screen can change it.");

        StringAssert.Contains(
            shell,
            "FacilityCard_Click",
            "The shell's building cards no longer make the choice.");
    }

    private static string Markup() =>
        File.ReadAllText(Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Module_Waitlist",
            "Views",
            "NewRequestWorkCenterPage.xaml"));

    private static string ViewModelSource() =>
        File.ReadAllText(Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "MTM_Waitlist.Waitlist.NewRequest",
            "ViewModels",
            "NewRequestWorkCenterViewModel.cs"));
}
