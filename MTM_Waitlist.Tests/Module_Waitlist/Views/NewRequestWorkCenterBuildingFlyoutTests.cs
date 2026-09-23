using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Waitlist.Views;

/// <summary>
/// US5 (FR-021, FR-022): the wizard's own building choice is the same flyout of cards, and it leaves out the
/// building in effect too.
/// </summary>
[TestClass]
public sealed class NewRequestWorkCenterBuildingFlyoutTests
{
    [TestMethod]
    public void TheWizard_BuildsTheBuildingListAsCards()
    {
        var markup = Markup();

        StringAssert.Contains(markup, "<Flyout Opening=\"BuildingFlyout_Opening\">", "The choice is made in a flyout.");
        StringAssert.Contains(markup, "ItemTemplate=\"{StaticResource PartPictureCardTemplate}\"", "Its entries are the shared card.");
        StringAssert.Contains(markup, "ItemsSource=\"{x:Bind ViewModel.BuildingCards, Mode=OneWay}\"", "It offers the cards, not the plain names.");
        StringAssert.Contains(markup, "ItemClick=\"BuildingCard_Click\"", "Choosing a card makes the choice.");
        StringAssert.Contains(markup, "SelectionMode=\"None\"", "A card is chosen by clicking it.");
    }

    [TestMethod]
    public void TheWizard_NoLongerOffersADropDownForTheBuilding()
    {
        var markup = Markup();

        Assert.IsFalse(markup.Contains("BuildingCombo", StringComparison.Ordinal), "The drop-down is replaced, not left beside the cards.");
        Assert.IsFalse(markup.Contains("ComboBox", StringComparison.Ordinal), "No drop-down remains on the step.");
    }

    [TestMethod]
    public void TheBuildingInEffect_IsNotAmongTheWizardsCards()
    {
        var source = ViewModelSource();

        StringAssert.Contains(source, "public ObservableCollection<BuildingCardOption> BuildingCards", "The cards are the flyout's source.");
        StringAssert.Contains(
            source,
            "!string.Equals(building, current, StringComparison.OrdinalIgnoreCase)",
            "The building currently in effect is left out (FR-022).");
        StringAssert.Contains(source, "RefreshBuildingCards();", "The list is rebuilt when the building changes and as the flyout opens.");
        StringAssert.Contains(source, "public void SelectBuilding(string? building)", "Choosing a card selects the building.");
    }

    [TestMethod]
    public void ChoosingACard_SelectsTheBuildingRatherThanRestatingIt()
    {
        var codeBehind = CodeBehind();

        StringAssert.Contains(codeBehind, "is BuildingCardOption card", "The handler reads the building off the card.");
        StringAssert.Contains(codeBehind, "ViewModel.SelectBuilding(card.Building)", "The building the card carries is the building chosen.");
    }

    private static string Markup() =>
        File.ReadAllText(Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Module_Waitlist",
            "Views",
            "NewRequestWorkCenterPage.xaml"));

    private static string CodeBehind() =>
        File.ReadAllText(Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Module_Waitlist",
            "Views",
            "NewRequestWorkCenterPage.xaml.cs"));

    private static string ViewModelSource() =>
        File.ReadAllText(Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "MTM_Waitlist.Waitlist.NewRequest",
            "ViewModels",
            "NewRequestWorkCenterViewModel.cs"));
}
