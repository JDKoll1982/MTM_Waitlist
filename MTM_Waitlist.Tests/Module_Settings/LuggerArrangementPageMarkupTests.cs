using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Settings;

/// <summary>
/// The lugger arrangement page read as markup: the arrangement is a grid of lugger cards, each card carries its own
/// actions, and everything a card changes is edited in a modal window opened from that card.
/// </summary>
/// <remarks>
/// The page lives in the application project, which this suite deliberately does not reference, so it is read from
/// disk as XML. What is pinned is the shape a reader depends on — one card per lugger, drag-to-reorder, an editor
/// that names the lugger it was opened for, and no second list of scrap types beside it.
/// </remarks>
[TestClass]
public sealed class LuggerArrangementPageMarkupTests
{
    private static readonly XNamespace s_presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private static readonly XNamespace s_xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

    /// <summary>
    /// The luggers are one grid of cards that wrap, the grid accepts a dragged card and reorders itself, and the
    /// new order is published when the drag finishes (FR-001, FR-030).
    /// </summary>
    [TestMethod]
    public void UI_LuggerArrangementPageMarkupTests_TheArrangement_IsAGridOfCardsThatCanBeDraggedIntoOrder()
    {
        var page = LoadPage();
        var grid = ListWithId(page, "LuggerArrangementPage_Summary");

        Assert.AreEqual("None", (string?)grid.Attribute("SelectionMode"), "A card is read and edited, never selected.");
        Assert.AreEqual("False", (string?)grid.Attribute("IsItemClickEnabled"), "Nothing on this page opens from a click on a card.");
        Assert.AreEqual(
            "{StaticResource ArrangementTileItemContainerStyle}",
            (string?)grid.Attribute("ItemContainerStyle"),
            "The framework's own item chrome would frame every card twice.");

        Assert.AreEqual("True", (string?)grid.Attribute("CanReorderItems"), "The cards cannot be dragged into a new order (FR-001).");
        Assert.AreEqual("True", (string?)grid.Attribute("AllowDrop"), "Reordering needs the grid to accept what is dropped on it.");
        Assert.AreEqual("True", (string?)grid.Attribute("CanDragItems"), "Without draggable items no drag ever starts.");
        Assert.AreEqual(
            "OnLuggerDragItemsCompleted",
            (string?)grid.Attribute("DragItemsCompleted"),
            "A drag that is not published leaves the store's order different from the order the lead arranged.");

        var panel = grid
            .Element(s_presentation + "GridView.ItemsPanel")
            ?.Descendants(s_presentation + "ItemsWrapGrid")
            .SingleOrDefault();

        Assert.IsNotNull(panel, "The cards do not wrap, so they run off the panel instead of forming a grid.");
        Assert.AreEqual("Horizontal", (string?)panel.Attribute("Orientation"), "The cards are laid out down the panel rather than across it.");

        // The column count and the card width come from the container on every measure, so neither is pinned in
        // markup and the page no longer carries a SizeChanged handler of its own to compute them.
        Assert.IsNull(
            panel.Attribute("MaximumRowsOrColumns"),
            "A fixed column count cannot follow the container, so the grid stops short of its panel or runs past it.");
        Assert.IsNull(
            panel.Attribute("ItemWidth"),
            "The card width is pinned in markup, so the columns cannot span the panel they are drawn in.");
        Assert.AreEqual(
            "208",
            (string?)panel.Attribute("ItemHeight"),
            "The tile height is declared on the panel: only the width follows the container, never the row height.");
        Assert.AreEqual(
            "240",
            grid.Attributes()
                .FirstOrDefault(attribute => attribute.Name.LocalName == "CardGridSizing.MinimumCardWidth")
                ?.Value,
            "The grid does not declare the narrowest card it may draw, so nothing sizes it from the container.");
        Assert.AreEqual(
            "3",
            grid.Attributes()
                .FirstOrDefault(attribute => attribute.Name.LocalName == "CardGridSizing.MaximumColumns")
                ?.Value,
            "The arrangement grid draws more than three cards across, so a wide window turns the luggers into a wall of thumbnails.");
        Assert.IsNull(
            grid.Attribute("SizeChanged"),
            "The page computes card widths itself again instead of letting the shared sizing follow the container.");
        Assert.AreEqual("Disabled", (string?)grid.Attribute("ScrollViewer.VerticalScrollMode"), "The list scrolls inside the page's own scroll viewer.");

        var template = grid.Element(s_presentation + "GridView.ItemTemplate")!.Descendants(s_presentation + "DataTemplate").Single();
        StringAssert.EndsWith(
            (string?)template.Attribute(s_xaml + "DataType") ?? string.Empty,
            "LuggerArrangementRow",
            "The cards are typed for the wrong row.");

        var tile = template.Elements().Single();
        Assert.AreEqual(s_presentation + "Border", tile.Name, "The items are drawn as something other than a card.");
        Assert.AreEqual("{StaticResource ArrangementTile}", (string?)tile.Attribute("Style"), "The cards do not wear the shared tile.");
        Assert.AreEqual("{x:Bind Name}", (string?)tile.Attribute("AutomationProperties.Name"), "A card has no accessible name.");
    }

    /// <summary>
    /// Each card carries its own styling and its own actions: the name, the status in the status's colour, the
    /// scrap types it takes, and an Edit and a Rename button side by side (FR-030).
    /// </summary>
    [TestMethod]
    public void UI_LuggerArrangementPageMarkupTests_EachCard_CarriesItsOwnStylingAndActions()
    {
        var tile = TileOf(LoadPage());

        var texts = tile
            .Descendants(s_presentation + "TextBlock")
            .Select(block => (string?)block.Attribute("Text"))
            .Where(text => text is not null)
            .ToArray();

        CollectionAssert.AreEquivalent(
            new[] { "{x:Bind Name}", "{x:Bind StatusAndReasonText}", "{x:Bind ScrapTypesText}" },
            texts,
            "A card no longer reads the lugger, its status and the scrap types it takes.");

        // The status is painted in its own colour, on both the badge and the accent bar beside the name.
        var statusPainted = tile
            .Descendants()
            .Count(element => ((string?)element.Attribute("Background"))?.Contains("StatusBadgeBackground", StringComparison.Ordinal) == true);

        Assert.IsTrue(statusPainted >= 2, "The card draws nothing in the lugger's own status colour, so a blocked lugger looks like any other.");

        var buttons = tile
            .Descendants(s_presentation + "Button")
            .Select(button => (Text: (string?)button.Attribute("Content"), Id: (string?)button.Attribute("AutomationProperties.AutomationId")))
            .ToArray();

        Assert.AreEqual(2, buttons.Length, "A card offers something other than its own Edit and Rename actions.");
        Assert.AreEqual(
            "LuggerArrangementPage_OpenEditor",
            buttons[0].Id,
            "The card's first action is not the Edit that opens its editor.");
        Assert.AreEqual(
            "LuggerArrangementPage_Rename",
            buttons[1].Id,
            "Rename does not sit beside Edit on the card (FR-030).");
        // A button whose text is bound to the page's view model draws an empty box: an item template cannot reach
        // the page reliably, so each action reads its word from the row it was drawn for.
        Assert.AreEqual("{x:Bind EditActionText}", buttons[0].Text, "The Edit action's word does not come from the card's own row, so the button draws empty.");
        Assert.AreEqual("{x:Bind RenameActionText}", buttons[1].Text, "The Rename action's word does not come from the card's own row, so the button draws empty.");
    }

    /// <summary>
    /// The page offers adding a lugger at the bottom of the arrangement, and there is no second list of scrap types
    /// beside the cards: which lugger a scrap type dumps into is read from that lugger's own card (FR-030, FR-031).
    /// </summary>
    [TestMethod]
    public void UI_LuggerArrangementPageMarkupTests_ThePage_AddsALuggerAndDrawsNoScrapTypeList()
    {
        var page = LoadPage();

        var add = page
            .Descendants(s_presentation + "Button")
            .SingleOrDefault(button => (string?)button.Attribute("AutomationProperties.AutomationId") == "LuggerArrangementPage_Add");

        Assert.IsNotNull(add, "The page offers no way to add a lugger (FR-003).");
        Assert.AreEqual("OnAddLuggerClick", (string?)add.Attribute("Click"), "The Add action does not open the window that asks for a name.");

        Assert.IsFalse(
            page.Descendants(s_presentation + "GridView")
                .Any(grid => (string?)grid.Attribute("AutomationProperties.AutomationId") == "LuggerArrangementPage_ScrapTypes"),
            "The scrap-type list is still drawn beside the lugger cards.");

        Assert.IsFalse(
            page.Descendants(s_presentation + "AutoSuggestBox").Any(),
            "A scrap type is still typed freely somewhere on the page.");
    }

    /// <summary>
    /// A card's two actions sit in a bordered band of their own, inside a five-row layout whose star rows centre
    /// the facts and the actions in the card (owner decision, 2026-10-26).
    /// </summary>
    [TestMethod]
    public void UI_LuggerArrangementPageMarkupTests_EachCard_PutsItsTwoActionsInABorderedBandOfTheirOwn()
    {
        var tile = TileOf(LoadPage());

        // The content grid is the card's own words-and-actions column: five rows, star / auto / star / auto / star.
        var content = tile
            .Descendants(s_presentation + "Grid")
            .SingleOrDefault(grid => grid.Elements(s_presentation + "Grid.RowDefinitions").Any());

        Assert.IsNotNull(content, "The card no longer declares the rows its content is laid out in.");

        var heights = content
            .Element(s_presentation + "Grid.RowDefinitions")!
            .Elements(s_presentation + "RowDefinition")
            .Select(row => (string?)row.Attribute("Height"))
            .ToArray();

        CollectionAssert.AreEqual(
            new[] { "*", "Auto", "*", "Auto", "*" },
            heights,
            "The card's content is not laid out as star / auto / star / auto / star.");

        // The actions are drawn inside a border of their own, and that border takes the second auto row.
        var band = content
            .Elements(s_presentation + "Border")
            .SingleOrDefault(border => border.Descendants(s_presentation + "Button").Any());

        Assert.IsNotNull(band, "The card's two actions are not inside a card of their own.");
        Assert.AreEqual("3", (string?)band!.Attribute("Grid.Row"), "The actions' band is not in the second auto row.");
        StringAssert.Contains(
            (string?)band.Attribute("BorderBrush") ?? string.Empty,
            "CardStrokeColorDefaultBrush",
            "The actions' band draws no border, so it does not read as a card.");
        StringAssert.Contains(
            (string?)band.Attribute("Background") ?? string.Empty,
            "SolidBackgroundFillColor",
            "The actions' band is see-through, so the card's picture shows through the buttons.");

        Assert.AreEqual(2, band.Descendants(s_presentation + "Button").Count(), "The band holds something other than the card's two actions.");
    }

    /// <summary>
    /// The editor is a modal window of its own, opened by a card's own Edit, so it carries no lugger picker, offers
    /// the scrap types as a choice rather than free text, and shows "Make dump point" only when there is a backup to
    /// promote ahead of (FR-006, FR-011, FR-018, FR-030).
    /// </summary>
    [TestMethod]
    public void UI_LuggerArrangementPageMarkupTests_TheEditor_IsAModalThatNamesItsOwnLugger()
    {
        var page = LoadPage();

        Assert.IsFalse(
            page.Descendants(s_presentation + "ContentDialog").Any(),
            "The editor is still drawn inside the page rather than opened as a window of its own.");

        var dialog = LoadDialog("LuggerEditorDialog.xaml");

        Assert.IsFalse(
            dialog.Descendants(s_presentation + "ComboBox")
                .Any(combo => (string?)combo.Attribute("AutomationProperties.AutomationId") == "LuggerArrangementPage_Lugger"),
            "The editor still asks which lugger it is about, although the card that opened it already named one.");

        var scrapType = dialog
            .Descendants(s_presentation + "ComboBox")
            .SingleOrDefault(combo => (string?)combo.Attribute("AutomationProperties.AutomationId") == "LuggerArrangementPage_ScrapType");

        Assert.IsNotNull(scrapType, "The scrap type is no longer offered as a choice in the editor (FR-011).");
        Assert.AreEqual("{x:Bind ViewModel.ScrapTypeOptions, Mode=OneWay}", (string?)scrapType.Attribute("ItemsSource"), "The scrap-type choice is not the arrangement's own list.");
        Assert.AreEqual("{x:Bind ViewModel.ScrapTypeInput, Mode=TwoWay}", (string?)scrapType.Attribute("SelectedItem"), "The chosen scrap type does not reach the view model.");

        var promote = dialog
            .Descendants(s_presentation + "Button")
            .SingleOrDefault(button => (string?)button.Attribute("AutomationProperties.AutomationId") == "LuggerArrangementPage_Promote");

        Assert.IsNotNull(promote, "The editor offers no way to make a lugger the dump point (FR-018).");
        var visibility = (string?)promote.Attribute("Visibility") ?? string.Empty;
        StringAssert.Contains(visibility, "CanPromote", "Make dump point is offered without asking whether there is a backup to promote ahead of.");
        StringAssert.Contains(visibility, "BoolToVisibilityConverter", "Make dump point is not hidden when it cannot do anything.");
    }

    /// <summary>
    /// Adding and renaming ask the same question, so they are the same window — which means a name field exists in
    /// exactly one place (FR-003).
    /// </summary>
    [TestMethod]
    public void UI_LuggerArrangementPageMarkupTests_AddingAndRenaming_ShareOneNameWindow()
    {
        var page = LoadPage();

        var nameFields = page
            .Descendants(s_presentation + "TextBox")
            .Count(field => (string?)field.Attribute("AutomationProperties.AutomationId") == "LuggerArrangementPage_Name");

        Assert.AreEqual(0, nameFields, "The name is asked for on the page itself rather than in the window that asks it.");

        var dialog = LoadDialog("LuggerNameDialog.xaml");

        // The question is the body of a windowed dialog now: its own window, owned by the main one, with the accept
        // button supplied by the host (014-adopt-lab-design-system, FR-001). The accept action therefore sits on the
        // show call rather than on the markup.
        Assert.AreEqual(s_presentation + "UserControl", dialog.Name, "The name question is no longer one hostable body.");
        Assert.AreEqual(
            1,
            dialog.Descendants(s_presentation + "TextBox")
                .Count(field => (string?)field.Attribute("Text") == "{x:Bind ViewModel.LuggerNameInput, Mode=TwoWay}"),
            "The name window's field is not the view model's, or there is more than one of them.");
    }

    private static XElement TileOf(XElement page) =>
        ListWithId(page, "LuggerArrangementPage_Summary")
            .Element(s_presentation + "GridView.ItemTemplate")!
            .Descendants(s_presentation + "DataTemplate")
            .Single()
            .Elements()
            .Single();

    private static XElement ListWithId(XElement page, string automationId)
    {
        var grid = page
            .Descendants(s_presentation + "GridView")
            .SingleOrDefault(candidate => (string?)candidate.Attribute("AutomationProperties.AutomationId") == automationId);

        Assert.IsNotNull(grid, $"'{automationId}' is not a GridView, so the arrangement is no longer drawn as cards.");

        return grid;
    }

    private static XElement LoadDialog(string fileName)
    {
        var path = Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Module_Settings",
            "Views",
            fileName);

        Assert.IsTrue(File.Exists(path), $"'{fileName}' was not found at '{path}'.");

        return XDocument.Load(path).Root!;
    }

    private static XElement LoadPage()
    {
        var path = Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Module_Settings",
            "Views",
            "LuggerArrangementPage.xaml");

        Assert.IsTrue(File.Exists(path), $"The lugger arrangement page was not found at '{path}'.");

        return XDocument.Load(path).Root!;
    }
}
