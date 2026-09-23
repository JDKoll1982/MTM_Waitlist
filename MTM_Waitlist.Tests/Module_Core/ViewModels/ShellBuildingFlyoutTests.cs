using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Core.ViewModels;

/// <summary>
/// US5 (FR-021, FR-022, FR-023): the shell's building choice is a flyout of cards, and the building in effect is
/// not among them.
/// </summary>
/// <remarks>
/// <c>ShellViewModel</c> cannot be constructed without the seven services the shell is built from, and the rule
/// under test is about what the flyout offers rather than about a rendered control, so the source of the view
/// model and the markup of the shell are read directly — the same approach the shell's own tests take.
/// </remarks>
[TestClass]
public sealed class ShellBuildingFlyoutTests
{
    [TestMethod]
    public void TheShell_BuildsTheBuildingListAsCards()
    {
        var markup = Markup();

        StringAssert.Contains(markup, "<Flyout Opening=\"FacilityFlyout_Opening\">", "The choice is made in a flyout.");
        StringAssert.Contains(markup, "ItemTemplate=\"{StaticResource PartPictureCardTemplate}\"", "Its entries are the shared card.");
        StringAssert.Contains(markup, "ItemsSource=\"{Binding BuildingCards}\"", "It offers the cards, not the plain names.");
        StringAssert.Contains(markup, "ItemClick=\"FacilityCard_Click\"", "Choosing a card makes the choice.");
        StringAssert.Contains(markup, "SelectionMode=\"None\"", "A card is chosen by clicking it.");
    }

    [TestMethod]
    public void TheShell_NoLongerOffersADropDownForTheBuilding()
    {
        var markup = Markup();

        Assert.IsFalse(markup.Contains("FacilitySelectorComboBox", StringComparison.Ordinal), "Both drop-downs are replaced.");
        Assert.IsFalse(
            markup.Contains("ItemsSource=\"{Binding Buildings}\"", StringComparison.Ordinal),
            "The raw name list is no longer a control's source; the cards are.");
    }

    [TestMethod]
    public void TheBuildingInEffect_IsNotAmongTheCards()
    {
        var source = ViewModelSource();

        StringAssert.Contains(source, "public ObservableCollection<BuildingCardOption> BuildingCards", "The cards are the flyout's source.");
        StringAssert.Contains(
            source,
            "!string.Equals(building, current, StringComparison.OrdinalIgnoreCase)",
            "The building currently in effect is left out (FR-022).");
        StringAssert.Contains(source, "RefreshBuildingCards();", "The list is rebuilt when the building changes and as the flyout opens.");
    }

    [TestMethod]
    public void ABuildingWithNoPicture_IsStillOfferedAsASelectableCard()
    {
        var card = new BuildingCardOption("Expo Drive", "ShellPage_FacilityCard");

        Assert.AreEqual("Expo Drive", card.Title);
        Assert.AreEqual("Assets/Placeholders/default-no-image.png", card.ImagePath, "The one shared placeholder, never a blank space (FR-023).");
        Assert.IsFalse(card.IsSelected, "The building in effect is not offered, so no card is ever the chosen one.");
        StringAssert.Contains(card.AutomationId, "Expo Drive");
        Assert.AreNotEqual(card.AutomationId, card.ImageAutomationId);
    }

    private static string Markup() =>
        File.ReadAllText(Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Module_Core",
            "Views",
            "ShellPage.xaml"));

    private static string ViewModelSource() =>
        File.ReadAllText(Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "ViewModels",
            "ShellViewModel.cs"));
}
