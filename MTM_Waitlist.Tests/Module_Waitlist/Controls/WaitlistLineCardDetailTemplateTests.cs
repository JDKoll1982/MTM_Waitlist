using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Waitlist.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Waitlist.Controls;

/// <summary>
/// US3: every Item draws its own cells, in the same places (FR-024 to FR-060, FR-103, FR-104).
/// </summary>
/// <remarks>
/// <para>
/// Two halves. The <b>markup</b> half reads the per-Item template dictionary from disk as XML, because it
/// lives in the application project this suite deliberately does not reference: it pins that the dictionary
/// carries one template for each of the twenty-four Items, that every template draws its cells through the
/// one shared cell (label and value on one line, no label at all on the die-shop cell), and that each slot's
/// span and visibility are read off the row's own cell count.
/// </para>
/// <para>
/// The <b>resolution</b> half drives <see cref="WaitlistViewViewModel.ResolveDeclaredCells"/> against the
/// <b>shipped</b> declaration seed, so what is checked is the cells the real configuration produces rather
/// than a copy of it: every drawn label is one of the Item's settled cells, every declared field the card
/// does not show stays undrawn, and the one cell whose value the requesting job's arrangement decides —
/// <c>Lugger Location</c> on <c>pickup-scrap</c> — names the lugger the arrangement settled (FR-022 to FR-027).
/// </para>
/// </remarks>
[TestClass]
public sealed class WaitlistLineCardDetailTemplateTests
{
    private static readonly XNamespace s_presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    private const string CellTemplateKey = "WaitlistLineCardDetailCell";
    private const string ScrollingCellTemplateKey = "WaitlistLineCardDetailScrollingCell";
    private const string PictureCellTemplateKey = "WaitlistLineCardDetailPictureCell";
    private const string CellTemplateSelectorKey = "WaitlistCardCellTemplateSelector";
    private const string SpanConverterKey = "WaitlistCardCellSpanConverter";
    private const string VisibilityConverterKey = "WaitlistCardCellVisibilityConverter";
    private const string LabelConverterKey = "WaitlistCardCellLabelConverter";

    /// <summary>The one answer every request shape carries, which the Items that ask one resolve from.</summary>
    private const string DefaultInput = "FGT0002000";

    /// <summary>
    /// The settled cells of every Item, as <c>data-model.md</c> §9 and <c>docs/research/research-waitlist-card-layout-pre-spec.md</c>
    /// §5.1 settle them. Where a label is being moved by the seed reconciliation (D33, T080) both spellings
    /// are listed, because both are a cell of that Item and neither may fall out of the card.
    /// </summary>
    private static readonly Dictionary<string, string[]> s_settledCells = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pickup-coil"] = ["Coil number", "Quantity in house", "Coil description", "Average coil weight"],
        ["pickup-die"] = ["Die", "Die Location", "Pickup location"],
        ["pickup-component"] = ["Component", "Total Qty In House", "Item Type"],
        ["pickup-fg"] = ["Part", "Part Type", "# of Skids"],
        ["pickup-ncm"] = ["Part", "Part Type", "# of Skids"],
        ["pickup-wip"] = ["Part", "Part Type", "# of Skids"],
        ["pickup-outside-service"] = ["Part", "Part Type", "# of Skids"],
        ["pickup-riser-table"] = [],
        ["pickup-dunnage"] = ["Dunnage part", "Home Location", "Home location"],
        ["pickup-scrap"] = ["Scrap Type", "Scrap type", "Lugger Location"],
        ["pickup-hopper"] = ["Size"],
        ["deliver-coil"] = ["Coil number", "Quantity in house", "Coil description", "Average coil weight"],
        ["deliver-flatstock"] = ["Part", "Quantity", "Destination"],
        ["deliver-component"] = ["Part description", "Total Qty In House", "Item Type"],
        ["deliver-die"] = ["Die", "Die Location", "Pickup location"],
        ["deliver-dunnage"] = ["Dunnage part", "Home Location", "Home location"],
        ["deliver-wrong-coil"] = ["Coil number", "Correct coil"],
        ["deliver-wrong-flatstock"] = ["Part", "Correct flatstock"],
        ["deliver-riser-table"] = [],
        ["deliver-hopper"] = ["Size"],
        ["assist-coil-turn"] = ["Coil Number", "Coil number", "Weight of Coil"],
        ["assist-table-place"] = [],
        ["assist-table-remove"] = [],
        ["other"] = ["Request description"],
    };

    /// <summary>
    /// The declared fields the card must never draw for one Item, as §9 lists them: the fields FR-056 names
    /// and the ones with no source of their own. They stay declared (FR-095) and stay undrawn.
    /// </summary>
    private static readonly Dictionary<string, string[]> s_declaredButNotCells = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pickup-die"] = ["Quantity"],
        ["deliver-die"] = ["Quantity"],
        ["pickup-component"] = ["Part description", "Quantity"],
        ["deliver-component"] = ["Component", "Quantity"],
        ["pickup-dunnage"] = ["Description", "Quantity on hand"],
        ["deliver-dunnage"] = ["Description", "Quantity on hand"],
        ["pickup-scrap"] = ["Quantity involved", "Scrap lugger"],
        ["assist-coil-turn"] = ["Quantity in house"],
        ["deliver-wrong-coil"] = ["Reason"],
        ["deliver-wrong-flatstock"] = ["Reason"],
    };

    /// <summary>The work-centre labels, which are never a cell on any Item (FR-005, FR-056, D19).</summary>
    private static readonly string[] s_workCentreLabels =
    [
        "Requesting work center",
        "Pickup work center",
        "Work center",
        "Requested work center",
    ];

    // ── The dictionary: one template per Item, and the cells it draws ─────────────────────────────────

    /// <summary>
    /// The dictionary carries one detail template for each of the twenty-four Items, keyed exactly the way the
    /// card's own selector looks an Item up, and the application's resources merge the dictionary so the
    /// selector can find it (FR-025, FR-030 to FR-053, D31).
    /// </summary>
    [TestMethod]
    public void UI_WaitlistLineCardDetailTemplateTests_Templates_CarryOneDetailTemplateForEveryItem()
    {
        var templates = DetailTemplates();
        var prefix = SelectorTemplateKeyPrefix();

        foreach (var item in RequestItemCatalog.Items)
        {
            var key = prefix + item.Id;

            Assert.IsTrue(
                templates.ContainsKey(key),
                $"`{item.Id}` has no detail template under the key the selector looks up (`{key}`), so its card would draw the empty fallback (FR-025, FR-030 to FR-053).");
        }

        Assert.AreEqual(
            RequestItemCatalog.TotalCount,
            templates.Count,
            $"The dictionary carries {templates.Count} detail templates for {RequestItemCatalog.TotalCount} Items: "
                + string.Join(", ", templates.Keys.Except(RequestItemCatalog.Items.Select(item => prefix + item.Id))));

        var app = XDocument.Load(Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), "App.xaml"));

        Assert.IsTrue(
            app
                .Descendants()
                .Where(element => element.Name.LocalName == "ResourceDictionary")
                .Select(element => (string?)element.Attribute("Source") ?? string.Empty)
                .Any(source => source.EndsWith("WaitlistLineCardDetailTemplates.xaml", StringComparison.Ordinal)),
            "The application's resources no longer merge the per-Item detail dictionary, so the selector finds no template and every card draws the empty fallback (D31).");
    }

    /// <summary>
    /// The three cell templates share one type scale, one gap between the label and its value, and one left edge,
    /// so a description cell and a plain cell beside it do not read at two different sizes or start at two
    /// different places (FR-028, FR-053).
    /// </summary>
    /// <remarks>
    /// The left edge is the cells' own; the card centres the grid they sit in, which is what keeps a panel whose
    /// lower row is empty in the middle of its section rather than against the top of it (FR-027). Centring each
    /// pair inside its cell was tried and rejected: it puts the six cells' contents on six different axes.
    /// </remarks>
    [TestMethod]
    public void UI_WaitlistLineCardDetailTemplateTests_EveryCellTemplate_SharesTheOneTypeScaleGapAndLeftEdge()
    {
        foreach (var (key, anatomy) in new[]
        {
            (CellTemplateKey, "the plain cell"),
            (ScrollingCellTemplateKey, "the scrolling cell"),
            (PictureCellTemplateKey, "the picture cell"),
        })
        {
            var root = TemplateFor(LoadTemplateDictionary(), key).Elements().Single();
            var grid = root.Elements().Single();

            Assert.AreEqual(
                "12,5",
                (string?)root.Attribute("Padding"),
                $"{anatomy} does not carry the cell's own padding, so its text sits against the rule beside it (FR-028).");
            Assert.IsTrue(
                double.Parse((string?)grid.Attribute("ColumnSpacing") ?? "0", CultureInfo.InvariantCulture) >= 6d,
                $"{anatomy} draws its label and value with no column gap, so the separator's trailing space is collapsed and the pair reads run-together (FR-028).");

            Assert.AreEqual(
                "Stretch",
                (string?)grid.Attribute("HorizontalAlignment"),
                $"{anatomy} does not start its label and value at the cell's left edge, so one row of cells starts at one place and the next at another (FR-028).");

            var label = root
                .Descendants(s_presentation + "TextBlock")
                .Single(text => ((string?)text.Attribute("Text"))?.Contains("Label", StringComparison.Ordinal) == true);

            Assert.AreEqual(
                "12",
                (string?)label.Attribute("FontSize"),
                $"{anatomy} draws its label at a different size from the other cells, so the panel has no single type scale (FR-028).");

            var value = root
                .Descendants()
                .Single(element => ((string?)element.Attribute("Text"))?.Contains("Value", StringComparison.Ordinal) == true);

            Assert.AreEqual(
                "15",
                (string?)value.Attribute("FontSize"),
                $"{anatomy} draws its value at a different size from the other cells, so the panel has no single type scale (FR-028).");
        }
    }

    /// <summary>
    /// Every template draws its cells through the one shared cell: the label on the same line as its value, the
    /// two read as `Label: value`, the label taken from the cell's own declaration rather than written into the
    /// markup, and no label at all on the label-less die-shop cell (FR-028, FR-055, FR-059, FR-104).
    /// </summary>
    [TestMethod]
    public void UI_WaitlistLineCardDetailTemplateTests_EveryTemplate_DrawsItsCellsLabelAndValueOnOneLine()
    {
        var dictionary = LoadTemplateDictionary();
        var cell = TemplateFor(dictionary, CellTemplateKey);

        var label = cell
            .Descendants(s_presentation + "TextBlock")
            .SingleOrDefault(text => ((string?)text.Attribute("Text"))?.Contains("Label", StringComparison.Ordinal) == true);

        Assert.IsNotNull(label, "The shared cell draws no label, so no cell shows what it is (FR-028).");
        StringAssert.Contains(
            (string?)label!.Attribute("Text") ?? string.Empty,
            LabelConverterKey,
            "The cell's label is not resolved through the resource mechanism, so a declaration naming a resource would show its key (FR-104).");
        StringAssert.Contains(
            (string?)label.Attribute("Text") ?? string.Empty,
            "ConverterParameter=separator",
            "The label carries no separator, so it would run straight into its value instead of reading `Label: value` (FR-028).");

        var values = cell
            .Descendants(s_presentation + "TextBlock")
            .Where(text => ((string?)text.Attribute("Text"))?.Contains("Value", StringComparison.Ordinal) == true)
            .ToList();

        Assert.AreEqual(1, values.Count, "The shared cell must draw the cell's value exactly once (FR-028).");
        Assert.IsTrue(
            label.IsBefore(values[0]),
            "The label is not drawn ahead of the value (FR-028).");

        var row = label.Parent!;

        Assert.AreEqual(
            "Grid",
            row.Name.LocalName,
            "The label and its value are stacked rather than read on one line (FR-028).");
        Assert.AreEqual(
            "1",
            (string?)values[0].Attribute("Grid.Column"),
            "The value is not the second column of the label's own row, so the two do not read as `Label: value` (FR-028).");

        // The gap between the label and its value is layout, not a character: the separator the label carries ends
        // with a space, and a trailing space at the end of a line is collapsed, so `Part Type: WIP` was drawn as
        // `Part Type:WIP`. A real column gap is the only thing that survives that.
        Assert.IsTrue(
            double.Parse((string?)row.Attribute("ColumnSpacing") ?? "0", CultureInfo.InvariantCulture) >= 6d,
            "The label and its value are drawn with no column gap, so the separator's trailing space is collapsed and the pair reads run-together (FR-028).");

        // The cell's two halves are told apart by weight, size and colour rather than by position alone: the label
        // sits small and secondary, the value larger, semibold and in the primary text colour, so the answer reads
        // first and the question that produced it second.
        Assert.AreEqual(
            "12",
            (string?)label.Attribute("FontSize"),
            "The cell's label is drawn at the wrong size for the card's type scale (FR-028).");
        Assert.AreEqual(
            "15",
            (string?)values[0].Attribute("FontSize"),
            "The cell's value is drawn at the wrong size for the card's type scale (FR-028).");
        StringAssert.Contains(
            (string?)values[0].Attribute("FontWeight") ?? string.Empty,
            "SemiBold",
            "The cell's value is not drawn a weight heavier than the body, so the label and the value read as one run of text (FR-028).");

        // The one cell with no label at all is the die-shop cell, and its label is hidden rather than blanked.
        StringAssert.Contains(
            (string?)label.Attribute("Visibility") ?? string.Empty,
            "IsLabelLess",
            "The cell's label is not hidden for the label-less cell, so the die-shop phrase would carry an empty label above it (FR-055).");

        // Each cell closes itself off from the cell above it and the cell to its left, and from nothing else, so
        // the cell area carries no border around its outside edge: the thickness is decided by the slot the cell
        // was laid out in, through one converter, rather than by an offset per slot in every Item's template
        // (FR-028).
        foreach (var (key, anatomy) in new[] { (CellTemplateKey, "the plain cell"), (ScrollingCellTemplateKey, "the scrolling cell"), (PictureCellTemplateKey, "the picture cell") })
        {
            var root = TemplateFor(dictionary, key).Elements().Single();

            Assert.AreEqual("Border", root.Name.LocalName, $"{anatomy} is not closed off by a border, so nothing separates two cells (FR-028).");
            StringAssert.Contains(
                (string?)root.Attribute("BorderThickness") ?? string.Empty,
                "WaitlistCardCellBorderConverter",
                $"{anatomy}'s separator is a fixed thickness rather than the one its slot calls for, so the cell area would keep a border around its outside edge (FR-028).");
            Assert.IsNotNull(
                root.Attribute("BorderBrush"),
                $"{anatomy}'s separator is drawn in no colour, so it would be invisible (FR-028).");
        }

        foreach (var (key, template) in DetailTemplates())
        {
            var slots = Slots(template);

            Assert.AreEqual(
                WaitlistCardCellLayout.PanelSlotCount,
                slots.Count,
                $"`{key}` does not carry the panel's four slots, so a fourth cell would have nowhere to go (FR-026).");

            // The hairlines each cell draws have to meet: a grid that also spaces its cells apart would leave the
            // rule between two cells broken into pieces (FR-028).
            var grid = (System.Xml.Linq.XElement)template.Elements().Single();

            Assert.AreEqual(
                "0",
                (string?)grid.Attribute("ColumnSpacing") ?? "0",
                $"`{key}` spaces its columns apart, so the line between two cells is broken (FR-028).");
            Assert.AreEqual(
                "0",
                (string?)grid.Attribute("RowSpacing") ?? "0",
                $"`{key}` spaces its rows apart, so the line between two rows of cells is broken (FR-028).");

            foreach (var slot in slots)
            {
                // A cell's line is chosen by the cell itself through the cell selector, which resolves to one of
                // the two shared line templates; the hopper Items' pictured cell names its template outright.
                var presenter = (string?)slot.Attribute("ContentTemplateSelector") is { Length: > 0 } selector
                    ? selector
                    : (string?)slot.Attribute("ContentTemplate") ?? string.Empty;

                Assert.IsTrue(
                    presenter.Contains(CellTemplateKey, StringComparison.Ordinal)
                        || presenter.Contains(ScrollingCellTemplateKey, StringComparison.Ordinal)
                        || presenter.Contains(PictureCellTemplateKey, StringComparison.Ordinal)
                        || presenter.Contains(CellTemplateSelectorKey, StringComparison.Ordinal),
                    $"`{key}` draws a cell through something other than a shared cell, so the anatomy differs per Item (FR-028).");

                // A cell that kept to its own height would leave the rule between two rows of cells stopping at
                // the taller of the two.
                Assert.AreEqual(
                    "Stretch",
                    (string?)slot.Attribute("VerticalAlignment"),
                    $"`{key}` does not let a cell fill its row, so the rule between the rows stops short (FR-028).");
                Assert.AreEqual(
                    "Stretch",
                    (string?)slot.Attribute("VerticalContentAlignment"),
                    $"`{key}` does not let the cell's own border fill its row (FR-028).");
            }
        }
    }

    /// <summary>
    /// Each template's four slots take their content from a slot on the row, in the order the row filled them,
    /// and never from an index into the row's cell collection (FR-026).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The slots are bound by name rather than as <c>Cells[0]</c> to <c>Cells[3]</c> because most rows fill
    /// fewer than four of them: an index the row never filled is a failed lookup, and the binding engine
    /// reported <i>"Failed to connect to index '3"</i> once per slot per card per load — 102 failures in one
    /// list of eleven rows — which no card showed, because <c>FallbackValue</c> kept the drawing right, but
    /// which buried the debug output a real binding fault would have to appear in.
    /// </para>
    /// <para>
    /// The slot is read from its position in the markup rather than from the binding's own number, so this
    /// also pins that the fourth slot did not take the third slot's cell: the binding's ordinal has to match
    /// the slot's ordinal for every template.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void UI_WaitlistLineCardDetailTemplateTests_EveryTemplate_TakesItsSlotsFromTheRowBySlotRatherThanByIndex()
    {
        foreach (var (key, template) in DetailTemplates())
        {
            var slots = Slots(template);

            for (var slot = 0; slot < slots.Count; slot++)
            {
                var content = (string?)slots[slot].Attribute("Content") ?? string.Empty;

                StringAssert.Contains(
                    content,
                    $"Binding Cell{slot}",
                    $"`{key}`'s slot {slot} does not take its cell from `Cell{slot}`, so its cells are drawn in "
                        + "the wrong places or a cell the row never filled fails to resolve (FR-026).");

                Assert.IsFalse(
                    content.Contains("Cells[", StringComparison.Ordinal),
                    $"`{key}`'s slot {slot} indexes the row's cell collection, so every row that fills fewer "
                        + "than four cells reports a binding failure for each empty slot (FR-026).");
            }
        }
    }

    /// <summary>
    /// Every cell whose value is a description is drawn in full and scrolling rather than clipped, through the
    /// application's shared scrolling text control, and the choice is made by the cell rather than pinned per slot
    /// (FR-053).
    /// </summary>
    /// <remarks>
    /// <c>other</c> was the first description on a card, and its template named the scrolling cell outright — which
    /// left the part description on <c>deliver-component</c> and the coil description on both coil Items clipped
    /// with an ellipsis. Every slot now asks the cell selector, and the cell's own label decides, so an Item added
    /// later inherits the behaviour without its template being edited.
    /// </remarks>
    [TestMethod]
    public void UI_WaitlistLineCardDetailTemplateTests_EveryDescriptionCell_IsDrawnInFullThroughTheScrollingText()
    {
        var dictionary = LoadTemplateDictionary();

        // The selector is declared after the two line templates it names, and both are wired: a selector with no
        // templates would draw nothing where a cell belongs.
        var selector = dictionary.Root!
            .Elements()
            .SingleOrDefault(element => element.Name.LocalName == "WaitlistCardCellTemplateSelector");
        Assert.IsNotNull(selector, "The cell selector is not declared, so a cell's own label cannot choose its line (FR-053).");
        StringAssert.Contains((string?)selector!.Attribute("PlainTemplate") ?? string.Empty, CellTemplateKey, "The selector's plain line is not the shared cell.");
        StringAssert.Contains((string?)selector.Attribute("ScrollingTemplate") ?? string.Empty, ScrollingCellTemplateKey, "The selector's scrolling line is not the shared scrolling cell.");

        // Every slot asks it: no Item pins a line template of its own any more, and no Item can forget to.
        foreach (var (key, template) in DetailTemplates())
        {
            foreach (var slot in Slots(template))
            {
                if (((string?)slot.Attribute("ContentTemplate") ?? string.Empty).Contains(PictureCellTemplateKey, StringComparison.Ordinal))
                {
                    continue;
                }

                StringAssert.Contains(
                    (string?)slot.Attribute("ContentTemplateSelector") ?? string.Empty,
                    CellTemplateSelectorKey,
                    $"`{key}` draws a cell through a pinned template, so a description in that slot would be clipped rather than scrolled (FR-053).");
            }
        }

        // The scrolling line itself is the shared control, drawing the cell's own value.
        var scrolling = TemplateFor(dictionary, ScrollingCellTemplateKey);
        var scroller = scrolling
            .Descendants()
            .SingleOrDefault(element => element.Name.LocalName == "AutoScrollTextView");

        Assert.IsNotNull(
            scroller,
            "The scrolling cell does not use the application's shared scrolling text control (FR-053).");
        StringAssert.Contains(
            (string?)scroller!.Attribute("Text") ?? string.Empty,
            "Value",
            "The scrolling cell does not draw the cell's own value (FR-053).");
    }

    /// <summary>
    /// The rule the selector reads: a cell whose label says its value is a description scrolls, and no other cell
    /// does (FR-053).
    /// </summary>
    [TestMethod]
    public void UI_WaitlistLineCardDetailTemplateTests_OnlyDescriptionValues_AreDrawnByTheScrollingText()
    {
        // Every description label the shipped declarations carry, drawn on a card or not.
        foreach (var label in new[] { "Request description", "Part description", "Coil description", "Description", "COIL DESCRIPTION" })
        {
            Assert.IsTrue(
                WaitlistCardCellRules.DrawsScrollingText(label),
                $"'{label}' is a description, so its value must scroll rather than be clipped (FR-053).");
            Assert.IsTrue(
                new WaitlistLineCardCell { Label = label, Value = "a sentence" }.UsesScrollingText,
                $"A cell declared as '{label}' does not ask the card for the scrolling line (FR-053).");
        }

        // Nothing else does: a code, a count, a place and a phrase stay on the fixed line.
        foreach (var label in new[] { "Part", "Total Qty In House", "Item Type", "Die Location", "Lugger Location", "Scrap Type", "Size", "# of Skids", "" })
        {
            Assert.IsFalse(
                WaitlistCardCellRules.DrawsScrollingText(label),
                $"'{label}' is not a description, so it must not take the scrolling line (FR-053).");
            Assert.IsFalse(
                new WaitlistLineCardCell { Label = label, Value = "a value" }.UsesScrollingText,
                $"A cell declared as '{label}' asks for the scrolling line and does not hold a description (FR-053).");
        }
    }

    /// <summary>
    /// The panel's geometry follows the number of cells the Item filled: four as two rows of two, three with
    /// the third spanning the bottom row, two with the bottom row collapsed, one spanning the full width, and
    /// none with both lower rows removed (FR-024, FR-026, FR-027, FR-103).
    /// </summary>
    [TestMethod]
    public void UI_WaitlistLineCardDetailTemplateTests_Geometry_FollowsTheCellCountForFourThreeTwoOneAndNoCells()
    {
        // The rule itself, at every count the panel can hold.
        (int Cells, int[] Spans, bool[] Drawn)[] shapes =
        [
            (0, [1, 1, 1, 1], [false, false, false, false]),
            (1, [2, 1, 1, 1], [true, false, false, false]),
            (2, [1, 1, 1, 1], [true, true, false, false]),
            (3, [1, 1, 2, 1], [true, true, true, false]),
            (4, [1, 1, 1, 1], [true, true, true, true]),
        ];

        foreach (var (cells, spans, drawn) in shapes)
        {
            for (var slot = 0; slot < WaitlistCardCellLayout.PanelSlotCount; slot++)
            {
                Assert.AreEqual(
                    spans[slot],
                    WaitlistCardCellLayout.ColumnSpanFor(slot, cells),
                    $"With {cells} cells, slot {slot} spans the wrong number of columns (FR-026).");
                Assert.AreEqual(
                    drawn[slot],
                    WaitlistCardCellLayout.IsDrawn(slot, cells),
                    $"With {cells} cells, slot {slot} is drawn or hidden when it should not be (FR-006, FR-026).");
                Assert.AreEqual(slot / 2, WaitlistCardCellLayout.RowFor(slot), $"Slot {slot} is on the wrong row (FR-024).");
                Assert.AreEqual(slot % 2, WaitlistCardCellLayout.ColumnFor(slot), $"Slot {slot} is in the wrong column (FR-024).");
            }

            Assert.AreEqual(
                cells > 2,
                WaitlistCardCellLayout.CarriesBottomRow(cells),
                $"With {cells} cells the panel's bottom row is drawn when it should collapse, or the reverse (FR-026).");
        }

        // And every template reads that rule off the row's own cell count rather than hard-setting a shape.
        foreach (var (key, template) in DetailTemplates())
        {
            var slots = Slots(template);

            Assert.AreEqual(
                2,
                template.Descendants(s_presentation + "RowDefinition").Count(),
                $"`{key}` does not hold the panel's two rows, so a four-cell Item could not fill two rows of two (FR-026).");
            Assert.AreEqual(
                2,
                template.Descendants(s_presentation + "ColumnDefinition").Count(),
                $"`{key}` does not hold the panel's two columns (FR-024, FR-026).");

            for (var slot = 0; slot < slots.Count; slot++)
            {
                var element = slots[slot];

                Assert.AreEqual(
                    WaitlistCardCellLayout.RowFor(slot).ToString(CultureInfo.InvariantCulture),
                    (string?)element.Attribute("Grid.Row"),
                    $"`{key}` puts slot {slot} on the wrong row (FR-024).");
                Assert.AreEqual(
                    WaitlistCardCellLayout.ColumnFor(slot).ToString(CultureInfo.InvariantCulture),
                    (string?)element.Attribute("Grid.Column"),
                    $"`{key}` puts slot {slot} in the wrong column (FR-024).");

                var span = (string?)element.Attribute("Grid.ColumnSpan") ?? string.Empty;

                StringAssert.Contains(span, "Cells.Count", $"`{key}`'s slot {slot} does not follow the row's own cell count (FR-103).");
                StringAssert.Contains(span, SpanConverterKey, $"`{key}`'s slot {slot} does not read its span from the panel's geometry (FR-026).");
                StringAssert.Contains(span, $"ConverterParameter={slot}", $"`{key}`'s slot {slot} does not name the slot it is (FR-026).");

                var visibility = (string?)element.Attribute("Visibility") ?? string.Empty;

                StringAssert.Contains(visibility, "Cells.Count", $"`{key}`'s slot {slot} is not collapsed by the cell count (FR-006).");
                StringAssert.Contains(
                    visibility,
                    VisibilityConverterKey,
                    $"`{key}`'s slot {slot} does not hide itself when the Item has no cell for it (FR-006, FR-026).");
                StringAssert.Contains(
                    visibility,
                    $"ConverterParameter={slot}",
                    $"`{key}`'s slot {slot} does not name the slot it is (FR-026).");
            }
        }
    }

    // ── The cells the shipped declarations produce ────────────────────────────────────────────────────

    /// <summary>
    /// Every Item draws its own settled cells and nothing else: each drawn label is one of the Item's cells,
    /// and every declared field the card does not show stays undrawn rather than being explained or filled
    /// with a substitute (FR-030 to FR-053, FR-056, FR-060).
    /// </summary>
    [TestMethod]
    public void ViewModel_WaitlistLineCardDetailTemplateTests_ResolveDeclaredCells_DrawsTheSettledCellsAndExcludesTheRest()
    {
        foreach (var item in RequestItemCatalog.Items)
        {
            var drawn = DrawnCells(item.Id);

            foreach (var cell in drawn)
            {
                Assert.IsTrue(
                    s_settledCells[item.Id].Contains(cell.Label, StringComparer.OrdinalIgnoreCase),
                    $"`{item.Id}` draws '{cell.Label}', which is not one of its settled cells (FR-030 to FR-053).");
            }

            foreach (var label in DeclaredLabels(item.Id))
            {
                if (!s_settledCells[item.Id].Contains(label, StringComparer.OrdinalIgnoreCase))
                {
                    Assert.IsFalse(
                        drawn.Any(cell => string.Equals(cell.Label, label, StringComparison.OrdinalIgnoreCase)),
                        $"`{item.Id}` draws its declared field '{label}', which the card must not show (FR-056).");
                }
            }

            foreach (var label in s_workCentreLabels)
            {
                Assert.IsFalse(
                    drawn.Any(cell => string.Equals(cell.Label, label, StringComparison.OrdinalIgnoreCase)),
                    $"`{item.Id}` draws a '{label}' cell, which no Item may show (FR-005, D19).");
            }
        }

        // The cells whose drawn count the settled assignment fixes, so a card cannot quietly gain or lose one.
        Assert.AreEqual(0, DrawnCells("pickup-riser-table").Count, "`pickup-riser-table` must draw no cells (FR-040).");
        Assert.AreEqual(0, DrawnCells("deliver-riser-table").Count, "`deliver-riser-table` must draw no cells (FR-048).");
        Assert.AreEqual(0, DrawnCells("assist-table-place").Count, "`assist-table-place` must draw no cells (FR-051).");
        Assert.AreEqual(0, DrawnCells("assist-table-remove").Count, "`assist-table-remove` must draw no cells (FR-052).");
        Assert.AreEqual(1, DrawnCells("other").Count, "`other` must draw its request description alone (FR-053).");

        // The incorrect coil and the incorrect flatstock are answers the operator types through the fuzzy search
        // (FR-082), so the request under test carries them: a request with no such answer hides the cell rather
        // than filling it from the job's own part (FR-006, D41).
        var incorrectParts = Answers(
            (NewRequestFlowRules.IncorrectCoilLabel, "MMC0001000"),
            (NewRequestFlowRules.IncorrectFlatstockLabel, "MMF0000500"));

        Assert.AreEqual(
            2,
            DrawnCells("deliver-wrong-coil", incorrectParts).Count,
            "`deliver-wrong-coil` must draw the incorrect coil and the correct one (FR-046).");
        Assert.AreEqual(
            2,
            DrawnCells("deliver-wrong-flatstock", incorrectParts).Count,
            "`deliver-wrong-flatstock` must draw the incorrect part and the correct one (FR-047).");
    }

    /// <summary>
    /// A declared field the card does not show is not a fault: it is excluded rather than reported, so the
    /// store's log is asked to chase up something that is working as designed (FR-056, FR-101).
    /// </summary>
    [TestMethod]
    public void ViewModel_WaitlistLineCardDetailTemplateTests_ResolveDeclaredCells_ExcludesADeclaredFieldWithoutRecordingAGap()
    {
        var sink = new RecordingAppLogSink();
        AppLog.Configure(sink);

        try
        {
            // `Reason` is declared on a wrong-part Item and is deliberately not one of its cells: the wrong
            // material is typed, so nothing is asked for. That it carries no answer is therefore not a gap.
            var declaration = Declaration("deliver-wrong-coil", ("Reason", RequestItemFieldDefinition.Sources.Answer, 1, true));

            var order = WaitlistViewViewModel.CreateSessionOrder(
                Shape("deliver-wrong-coil", inputValue: null),
                jobAvailability: null,
                declaration);

            Assert.AreEqual(0, order.Cells.Count, "An excluded declared field is not a cell (FR-056).");
            Assert.AreEqual(
                0,
                sink.Warnings.Count,
                "The card reported an excluded declared field as a gap, which asks the store to chase up what is working as designed (FR-056).");
        }
        finally
        {
            AppLog.Configure(null);
        }
    }

    /// <summary>
    /// A declared cell whose source cannot be read is absent rather than explained, and the gap is written to
    /// the database log (FR-058, FR-060, FR-101).
    /// </summary>
    [TestMethod]
    public void ViewModel_WaitlistLineCardDetailTemplateTests_ResolveDeclaredCells_HidesAnUnreadableCellAndRecordsTheGap()
    {
        var sink = new RecordingAppLogSink();
        AppLog.Configure(sink);

        try
        {
            // The die is asked for, so a request carrying no die is a source that exists and cannot be read.
            var declaration = Declaration("pickup-die", ("Die", RequestItemFieldDefinition.Sources.Answer, 1, true));

            var order = WaitlistViewViewModel.CreateSessionOrder(
                Shape("pickup-die", inputValue: null),
                jobAvailability: null,
                declaration);

            Assert.AreEqual(0, order.Cells.Count, "A cell whose source cannot be read is hidden rather than drawn blank (FR-058).");
            Assert.AreEqual(1, sink.Warnings.Count, "The gap is not recorded, so it can never be chased up (FR-101).");
            StringAssert.Contains(sink.Warnings[0], "Die", "The record does not name the cell that went missing (FR-101).");
        }
        finally
        {
            AppLog.Configure(null);
        }
    }

    /// <summary>
    /// The die Items' third cell is the phrase itself, carries no label, and is drawn only when the die-shop
    /// answer was given: `Take To Die Shop` on a pickup and `Located In Die Shop` on a deliver (FR-031, FR-044,
    /// FR-055, FR-081).
    /// </summary>
    [TestMethod]
    public void ViewModel_WaitlistLineCardDetailTemplateTests_ResolveDeclaredCells_DrawsTheDieShopCellOnlyWhenTheAnswerWasGiven()
    {
        var declared = Declaration(
            "pickup-die",
            ("Die", RequestItemFieldDefinition.Sources.Answer, 1, true),
            ("Pickup location", RequestItemFieldDefinition.Sources.Job, 2, false),
            (WaitlistCardCellRules.DieShopAnswerLabel, RequestItemFieldDefinition.Sources.Answer, 3, false));

        var unticked = WaitlistViewViewModel.CreateSessionOrder(
            Shape("pickup-die"),
            Job(),
            declared);

        Assert.AreEqual(2, unticked.Cells.Count, "An unticked die request draws two cells, not three (FR-031).");

        var ticked = WaitlistViewViewModel.CreateSessionOrder(
            Shape("pickup-die", Answers([(WaitlistCardCellRules.DieShopAnswerLabel, "true")])),
            Job(),
            declared);

        Assert.AreEqual(3, ticked.Cells.Count, "A ticked die request draws its third, label-less cell (FR-031).");

        var dieShop = ticked.Cells.OrderBy(cell => cell.Order).Last();

        Assert.IsTrue(dieShop.IsLabelLess, "The die-shop cell carries no label (FR-028, FR-055).");
        Assert.AreEqual(
            WaitlistCardCellRules.DieShopPhrase("pickup-die"),
            dieShop.Value,
            "The die-shop cell draws the phrase itself, not the answer's own text (FR-055).");
        Assert.AreNotEqual("true", dieShop.Value, "The answer's raw flag is drawn instead of the phrase (FR-055).");

        var deliver = WaitlistViewViewModel.CreateSessionOrder(
            Shape("deliver-die", Answers([(WaitlistCardCellRules.DieShopAnswerLabel, "true")])),
            Job(),
            Declaration("deliver-die", (WaitlistCardCellRules.DieShopAnswerLabel, RequestItemFieldDefinition.Sources.Answer, 1, false)));

        Assert.AreEqual(
            WaitlistCardCellRules.DieShopPhrase("deliver-die"),
            deliver.Cells.Single().Value,
            "A deliver die reads `Located In Die Shop`, not the pickup phrase (FR-044, FR-055).");
        Assert.AreNotEqual(
            WaitlistCardCellRules.DieShopPhrase("pickup-die"),
            WaitlistCardCellRules.DieShopPhrase("deliver-die"),
            "The two die Items draw the same phrase, so the Item's direction is not what decides it (FR-055).");

        foreach (var notTicked in new[] { "false", "no", "0", string.Empty })
        {
            var leftUnticked = WaitlistViewViewModel.CreateSessionOrder(
                Shape("pickup-die", Answers([(WaitlistCardCellRules.DieShopAnswerLabel, notTicked)])),
                Job(),
                declared);

            Assert.AreEqual(
                2,
                leftUnticked.Cells.Count,
                $"An answer of '{notTicked}' is a checkbox that was not ticked, so the third cell stays absent (FR-055).");
        }
    }

    /// <summary>
    /// The hopper Items' one cell pictures the size it shows: the size word is also the key that size's
    /// reference picture is filed under, read from the Visual collection, so the card opens the same picture the
    /// New Request hopper step drew (FR-077, FR-078, D22). Every other Item's cells open no picture at all, so
    /// no card offers an enlarge affordance over a value that has nothing to open.
    /// </summary>
    [TestMethod]
    public void ViewModel_WaitlistLineCardDetailTemplateTests_ResolveDeclaredCells_TheHopperSizeCellOpensThatSizesReferencePicture()
    {
        foreach (var hopper in new[] { "pickup-hopper", "deliver-hopper" })
        {
            var sizeCell = WaitlistViewViewModel
                .CreateSessionOrder(
                    Shape(hopper, Answers([("Size", "Large")])),
                    Job(),
                    SeedDeclarations()[hopper])
                .Cells
                .Single();

            Assert.AreEqual("Large", sizeCell.Value, "The hopper card still reads the size as a word (FR-077).");
            Assert.IsTrue(sizeCell.OpensReferencePicture, $"`{hopper}`'s Size cell opens that size's reference image (FR-078).");
            Assert.AreEqual(
                "Large",
                sizeCell.ReferencePartNumber,
                "The size word is the key its picture is filed under, so the card and the wizard cannot ask for two different pictures (D22).");
            Assert.AreEqual(
                WaitlistCardCellRules.ReferencePictureScope,
                sizeCell.ReferencePartScope,
                "The picture is read from the one collection the hopper step reads it from (D22).");
        }

        // No other Item's cells open a picture: the coil's number, the die's location and the counter's type are
        // values, not pictures, so nothing on those cards offers an enlarge affordance (FR-078).
        foreach (var other in new[] { "pickup-coil", "pickup-die", "pickup-fg", "other" })
        {
            var cells = WaitlistViewViewModel
                .CreateSessionOrder(Shape(other), Job(), SeedDeclarations()[other])
                .Cells;

            Assert.IsFalse(
                cells.Any(cell => cell.OpensReferencePicture),
                $"`{other}` draws a cell that opens a picture, and only the hopper's Size cell does (FR-078).");
        }
    }

    /// <summary>
    /// A size whose reference picture is missing or unusable draws the application's one shared placeholder
    /// rather than a blank, before anything has resolved and after a path that resolves nothing was written
    /// (FR-080).
    /// </summary>
    [TestMethod]
    public void Model_WaitlistLineCardDetailTemplateTests_TheHopperSizeCell_DrawsTheSharedPlaceholderUntilAPictureResolves()
    {
        var sizeCell = WaitlistViewViewModel
            .CreateSessionOrder(
                Shape("pickup-hopper", Answers([("Size", "Small")])),
                Job(),
                SeedDeclarations()["pickup-hopper"])
            .Cells
            .Single();

        Assert.AreEqual(
            MTM_Waitlist.Module_Shared.Helpers.ImagePicturePolicy.NoImagePath,
            sizeCell.EffectiveReferenceImagePath,
            "Nothing resolved yet, so the cell draws the shared placeholder (FR-080).");

        sizeCell.ResolvedReferenceImagePath = @"C:\pictures\Small.png";

        Assert.AreEqual(
            @"C:\pictures\Small.png",
            sizeCell.EffectiveReferenceImagePath,
            "A picture that resolved is the one the cell draws (FR-078).");
    }

    /// <summary>
    /// The two hopper arrangements draw their one cell through the cell that pictures it, and that shared cell
    /// draws the picture through the application's one enlarge control — so the size enlarges exactly the way the
    /// card's other pictures do rather than through a second overlay (FR-078, FR-080).
    /// </summary>
    [TestMethod]
    public void UI_WaitlistLineCardDetailTemplateTests_TheHopperTemplates_DrawTheSizeCellThroughThePictureCell()
    {
        var dictionary = LoadTemplateDictionary();
        var prefix = SelectorTemplateKeyPrefix();

        foreach (var hopper in new[] { "pickup-hopper", "deliver-hopper" })
        {
            var slot = Slots(TemplateFor(dictionary, prefix + hopper))[0];

            StringAssert.Contains(
                (string?)slot.Attribute("ContentTemplate") ?? string.Empty,
                PictureCellTemplateKey,
                $"`{hopper}`'s Size cell is drawn by the plain text cell, so the size on its card opens nothing (FR-078).");
        }

        var pictureCell = TemplateFor(dictionary, PictureCellTemplateKey);
        var picture = pictureCell
            .Descendants()
            .SingleOrDefault(element => element.Name.LocalName == "ClickToEnlargeImageView");

        Assert.IsNotNull(
            picture,
            "The picture cell draws no enlarge control, so the hopper's size cannot be enlarged at all (FR-078).");

        StringAssert.Contains(
            (string?)picture!.Attribute("Source") ?? string.Empty,
            "EffectiveReferenceImagePath",
            "The cell's picture is not the reference image the row resolved, so the card would draw something else (FR-078).");

        Assert.IsNotNull(
            pictureCell
                .Descendants(s_presentation + "TextBlock")
                .FirstOrDefault(text => ((string?)text.Attribute("Text") ?? string.Empty).Contains("Value", StringComparison.Ordinal)),
            "The cell no longer draws the size as a word, so the size the requester chose is unreadable (FR-077).");
    }

    /// <summary>
    /// `Weight of Coil` is the weight the operator supplies on an assist coil turn, never the job's average
    /// coil weight, and a cell with no answer is absent rather than filled with the average (FR-050, FR-054).
    /// </summary>
    [TestMethod]
    public void ViewModel_WaitlistLineCardDetailTemplateTests_ResolveDeclaredCells_DrawsWeightOfCoilFromTheAnswerAndNeverTheJobsAverage()
    {
        var declared = Declaration(
            "assist-coil-turn",
            ("Coil number", RequestItemFieldDefinition.Sources.Job, 1, false),
            ("Weight of Coil", RequestItemFieldDefinition.Sources.Answer, 2, true));

        var answered = WaitlistViewViewModel.CreateSessionOrder(
            Shape("assist-coil-turn", Answers([("Weight of Coil", "5120 lb")])),
            Job(),
            declared);

        Assert.AreEqual("5120 lb", answered.Cells.Last().Value, "The weighted cell is not the weight the operator gave (FR-054, FR-086).");
        Assert.IsFalse(
            answered.Cells.Any(cell => string.Equals(cell.Value, Job().CoilAverageWeight, StringComparison.OrdinalIgnoreCase)),
            "The job's average coil weight is drawn, so the two weights have been conflated (FR-054).");

        var unanswered = WaitlistViewViewModel.CreateSessionOrder(
            Shape("assist-coil-turn", inputValue: null),
            Job(),
            Declaration(
                "assist-coil-turn",
                ("Coil number", RequestItemFieldDefinition.Sources.Job, 1, false),
                ("Weight of Coil", RequestItemFieldDefinition.Sources.Answer, 2, true)));

        Assert.AreEqual(1, unanswered.Cells.Count, "A weight the operator never gave leaves the cell absent (FR-054).");
        Assert.IsFalse(
            unanswered.Cells.Any(cell => string.Equals(cell.Label, "Weight of Coil", StringComparison.OrdinalIgnoreCase)),
            "The empty weighted cell is drawn with the job's average in it, which FR-054 forbids.");
    }

    /// <summary>
    /// `Destination` is drawn on `deliver-flatstock` alone, resolved from the request's own requesting work
    /// centre rather than from anything the requester was asked, and the scrap card's `Lugger Location` names the
    /// lugger the arrangement settled for the job's scrap type (FR-022, FR-027, FR-042, FR-057).
    /// </summary>
    [TestMethod]
    public void ViewModel_WaitlistLineCardDetailTemplateTests_ResolveDeclaredCells_DrawsDestinationFromTheRequestAndNamesTheScrapsLugger()
    {
        var answers = Answers([("Destination", "Somewhere else"), (WaitlistCardCellRules.DieShopAnswerLabel, "true")]);

        foreach (var item in RequestItemCatalog.Items)
        {
            var drawn = DrawnCells(item.Id, answers);
            var destinations = drawn.Where(cell => string.Equals(cell.Label, "Destination", StringComparison.OrdinalIgnoreCase)).ToArray();

            if (!string.Equals(item.Id, "deliver-flatstock", StringComparison.Ordinal))
            {
                Assert.AreEqual(
                    0,
                    destinations.Length,
                    $"`{item.Id}` draws a Destination cell, which FR-057 allows on `deliver-flatstock` alone.");
                continue;
            }

            Assert.AreEqual(1, destinations.Length, "`deliver-flatstock` must draw its Destination cell (FR-042).");
            Assert.AreEqual(
                Shape(item.Id).WorkCenter,
                destinations[0].Value,
                "Destination is not the request's own requesting work centre, so the Deliver rule is not what resolves it (FR-042).");
            Assert.AreNotEqual(
                "Somewhere else",
                destinations[0].Value,
                "Destination is read from an answer, which is the question that was retired (FR-057, FR-089).");
        }

        // `Lugger Location` stays declared on the scrap Item and is drawn from the value the arrangement read
        // handed the snapshot, so the handler reads the lugger to walk to; the label is still the declared one and
        // the card's layout does not change (FR-022, FR-027).
        Assert.IsTrue(
            WaitlistCardCellRules.IsCell("pickup-scrap", "Lugger Location"),
            "Lugger Location is no longer a cell of `pickup-scrap`, so the resolved lugger could never reach the card (FR-027).");

        var scrapDeclaration = Declaration(
            "pickup-scrap",
            ("Scrap type", RequestItemFieldDefinition.Sources.Job, 1, false),
            ("Lugger Location", RequestItemFieldDefinition.Sources.Job, 2, false));

        var scrap = WaitlistViewViewModel.CreateSessionOrder(
            Shape("pickup-scrap"),
            ScrapJob(),
            scrapDeclaration);

        var lugger = scrap.Cells.SingleOrDefault(cell => string.Equals(cell.Label, "Lugger Location", StringComparison.OrdinalIgnoreCase));

        Assert.IsNotNull(
            lugger,
            "The scrap card draws no Lugger Location cell although the arrangement named a lugger for the job's scrap type (FR-022).");
        Assert.AreEqual(
            "Door 6 Lugger",
            lugger!.Value,
            "The Lugger Location cell does not draw the lugger the snapshot carries, so the handler is sent nowhere (FR-022).");
        Assert.IsTrue(scrap.Cells.Any(cell => string.Equals(cell.Label, "Scrap type", StringComparison.OrdinalIgnoreCase)), "The scrap Item's own Scrap Type cell went missing with it (FR-030).");
    }

    /// <summary>
    /// The scrap card reads as one statement — the material, then where it is dumped — so `Lugger Location` is
    /// drawn in the second column beside `Scrap type`, in the top row, whatever order the Item's declaration
    /// happens to carry.
    /// </summary>
    [TestMethod]
    public void ViewModel_WaitlistLineCardDetailTemplateTests_ResolveDeclaredCells_PlacesTheLuggerInTheSecondColumnBesideTheScrapType()
    {
        // The shipped declaration: scrap type first, the lugger last, with the three fields the card excludes
        // sitting between them.
        var shipped = WaitlistViewViewModel.CreateSessionOrder(
            Shape("pickup-scrap"),
            ScrapJob(),
            Declaration(
                "pickup-scrap",
                ("Scrap Type", RequestItemFieldDefinition.Sources.Job, 1, false),
                ("Quantity involved", RequestItemFieldDefinition.Sources.Job, 2, false),
                ("Scrap lugger", RequestItemFieldDefinition.Sources.Job, 3, false),
                ("Pickup work center", RequestItemFieldDefinition.Sources.Fixed, 4, false),
                ("Lugger Location", RequestItemFieldDefinition.Sources.Job, 5, false)));

        Assert.AreEqual(2, shipped.Cells.Count, "The scrap card draws a cell other than the scrap type and the lugger (FR-030, FR-027).");
        Assert.AreEqual("Scrap Type", shipped.Cells[0].Label, "The scrap card no longer opens with the scrap type, so the lugger is not the second column (FR-027).");
        Assert.AreEqual("Lugger Location", shipped.Cells[1].Label, "The lugger is not drawn beside the scrap type (FR-022, FR-027).");

        // A store whose declaration lists the two the other way round draws the same card: the pair's order is
        // the card's rule, not the configuration's.
        var reversed = WaitlistViewViewModel.CreateSessionOrder(
            Shape("pickup-scrap"),
            ScrapJob(),
            Declaration(
                "pickup-scrap",
                ("Lugger Location", RequestItemFieldDefinition.Sources.Job, 1, false),
                ("Scrap Type", RequestItemFieldDefinition.Sources.Job, 2, false)));

        Assert.AreEqual("Scrap Type", reversed.Cells[0].Label, "The lugger took the first column because the declaration listed it first.");
        Assert.AreEqual("Lugger Location", reversed.Cells[1].Label, "The lugger is not the second column when the declaration lists it first.");

        // And the second column of the top row is where the template draws the second cell.
        Assert.AreEqual(0, WaitlistCardCellLayout.RowFor(1), "The second cell is not on the panel's top row.");
        Assert.AreEqual(1, WaitlistCardCellLayout.ColumnFor(1), "The second cell is not in the panel's second column.");
        Assert.AreEqual(1, WaitlistCardCellLayout.ColumnSpanFor(1, 2), "A two-cell panel draws each cell in its own column rather than spanning.");
    }

    /// <summary>
    /// A scrap card whose job carries no lugger — no scrap decision, or a scrap type the arrangement has no lugger
    /// for — draws no lugger cell at all, never a dash and never a blank, and no card anywhere still draws the
    /// retired "In development" placeholder (FR-025, FR-026, FR-039).
    /// </summary>
    [TestMethod]
    public void ViewModel_WaitlistLineCardDetailTemplateTests_ResolveDeclaredCells_OmitsTheLuggerCellWhenNothingResolvedAndNeverDrawsInDevelopment()
    {
        var scrapDeclaration = Declaration(
            "pickup-scrap",
            ("Scrap type", RequestItemFieldDefinition.Sources.Job, 1, false),
            ("Lugger Location", RequestItemFieldDefinition.Sources.Job, 2, false));

        // The job's scrap type is real, and the arrangement answered no lugger for it: the cell is absent rather
        // than filled with a substitute value (FR-026).
        var gap = WaitlistViewViewModel.CreateSessionOrder(
            Shape("pickup-scrap"),
            Job() with { ScrapType = "Galvanized Steel" },
            scrapDeclaration);

        Assert.IsFalse(
            gap.Cells.Any(cell => string.Equals(cell.Label, "Lugger Location", StringComparison.OrdinalIgnoreCase)),
            "A scrap type with no lugger still draws a Lugger Location cell, so a substitute value reached the card (FR-026).");
        Assert.IsTrue(
            gap.Cells.Any(cell => string.Equals(cell.Label, "Scrap type", StringComparison.OrdinalIgnoreCase)),
            "The scrap card lost its Scrap Type cell along with the missing lugger (FR-030).");

        // A job with no real scrap decision is the other way the value stays null (FR-025).
        var noDecision = WaitlistViewViewModel.CreateSessionOrder(
            Shape("pickup-scrap"),
            Job() with { ScrapType = "Scrap Type Required" },
            scrapDeclaration);

        Assert.IsFalse(
            noDecision.Cells.Any(cell => string.Equals(cell.Label, "Lugger Location", StringComparison.OrdinalIgnoreCase)),
            "A job with no scrap decision still draws a Lugger Location cell (FR-025).");

        // The placeholder's wording has no cell left to serve: no Item draws it anywhere. The phrase's resource key
        // is checked too, because a resurrected branch would draw the key itself once the retired resource is gone
        // (FR-039).
        foreach (var item in RequestItemCatalog.Items)
        {
            foreach (var cell in DrawnCells(item.Id))
            {
                Assert.IsFalse(
                    cell.Value.Contains("In development", StringComparison.OrdinalIgnoreCase)
                        || cell.Value.Contains("PendingSource", StringComparison.OrdinalIgnoreCase),
                    $"`{item.Id}` draws '{cell.Value}', which is the retired in-development placeholder rather than a value (FR-039).");
            }
        }
    }

    /// <summary>
    /// The shipped configuration never asks the requester for `Destination`: it stays declared `fixed` on the
    /// one Item that shows it, so the retired question cannot come back as an answer (FR-057, FR-089).
    /// </summary>
    [TestMethod]
    public void Audit_WaitlistLineCardDetailTemplateTests_ShippedSeed_NeverAsksTheRequesterForDestination()
    {
        var seed = File.ReadAllText(Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Database",
            "Seeds",
            "seed_waitlist_request_item_configs",
            "create.sql"));

        Assert.IsFalse(
            seed.Contains("'label','Destination','value_type','string','source','answer'", StringComparison.Ordinal),
            "The shipped configuration asks the requester for Destination, which is the question that was retired (FR-057).");

        Assert.IsTrue(
            SeedRow(seed, "deliver-flatstock").Contains("'label','Destination','value_type','string','source','fixed'", StringComparison.Ordinal),
            "`deliver-flatstock` no longer declares Destination as a fixed field, so its card has nothing to resolve it from (FR-042).");
    }

    /// <summary>
    /// Every new string is a resource key rather than a literal: the two die-shop phrases are added to the
    /// resource file and the card's cell labels are resolved through it (FR-104).
    /// </summary>
    [TestMethod]
    public void UI_WaitlistLineCardDetailTemplateTests_DieShopPhrases_ResolveThroughTheResourceFile()
    {
        var resources = XDocument
            .Load(Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), "Strings", "en-us", "Resources.resw"))
            .Descendants("data")
            .Where(data => data.Attribute("name") is not null)
            .ToDictionary(
                data => (string)data.Attribute("name")!,
                data => (string?)data.Element("value") ?? string.Empty,
                StringComparer.Ordinal);

        foreach (var key in new[] { WaitlistCardCellRules.TakeToDieShopKey, WaitlistCardCellRules.LocatedInDieShopKey })
        {
            Assert.IsTrue(resources.ContainsKey(key), $"The resource key '{key}' is missing from Resources.resw (FR-104).");
            Assert.IsFalse(string.IsNullOrWhiteSpace(resources[key]), $"The resource key '{key}' has no copy (FR-104).");

            Assert.IsNotNull(
                WaitlistCardCellRules.DieShopPhrase(key == WaitlistCardCellRules.TakeToDieShopKey ? "pickup-die" : "deliver-die"),
                $"'{key}' is resolved for no Item, so nothing draws it (FR-055).");
        }

        // The two directions ask different questions, and both are in the resource file: one wording for both
        // would make the same tick mean "take it there" on a pickup and "it is there" on a deliver (FR-081).
        var prompts = new[]
        {
            (Item: "pickup-die", Key: WaitlistCardCellRules.TakeToDieShopPromptKey),
            (Item: "deliver-die", Key: WaitlistCardCellRules.LocatedInDieShopPromptKey),
        };

        foreach (var (item, key) in prompts)
        {
            Assert.IsTrue(resources.ContainsKey(key), $"The resource key '{key}' is missing from Resources.resw (FR-022, FR-081).");
            Assert.IsFalse(string.IsNullOrWhiteSpace(resources[key]), $"The die-shop question a {item} asks has no copy (FR-022).");
        }

        Assert.AreNotEqual(
            resources[prompts[0].Key],
            resources[prompts[1].Key],
            "The two die directions ask the same question, so one tick means two different things (FR-081).");
    }

    /// <summary>
    /// Every label the card can draw resolves through the resource file (FR-104). A cell's label IS a resource key,
    /// and `GetLocalized()` falls back to the key itself when the key is absent - so a label with no entry here
    /// draws correct-looking English while never being a resolved string. That is how 22 declared labels went
    /// unnoticed, so this walks the shipped seed's own declarations rather than a hand-written list: whatever the
    /// seed declares as a cell is exactly what has to resolve.
    /// </summary>
    /// <remarks>
    /// Keys are compared <b>case-insensitively</b>, because the resource index is: a second entry differing only
    /// in case from an existing one fails the build with `PRI277: Conflicting values`. That is also why the three
    /// labels this work renames need no old-spelling entry - the pre-migration spelling resolves through the
    /// canonical key.
    /// </remarks>
    [TestMethod]
    public void UI_WaitlistLineCardDetailTemplateTests_EveryDeclaredCellLabel_ResolvesThroughTheResourceFile()
    {
        var resources = XDocument
            .Load(Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), "Strings", "en-us", "Resources.resw"))
            .Descendants("data")
            .Where(data => data.Attribute("name") is not null && !string.IsNullOrWhiteSpace((string?)data.Element("value")))
            .Select(data => (string)data.Attribute("name")!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var unresolved = new List<string>();

        foreach (var (itemCode, declaration) in SeedDeclarations())
        {
            foreach (var field in declaration.DetailFields)
            {
                var label = field.Label;

                // A field the card does not draw is not a cell, so it needs no label to resolve (FR-056).
                if (string.IsNullOrWhiteSpace(label) || !WaitlistCardCellRules.IsCell(itemCode, label))
                {
                    continue;
                }

                if (!resources.Contains(label))
                {
                    unresolved.Add($"`{itemCode}` declares the cell label '{label}', which resolves to nothing");
                }
            }
        }

        Assert.AreEqual(
            0,
            unresolved.Count,
            "A declared cell label has no entry in Resources.resw, so the cell draws its own key instead of a "
            + "resolved string (FR-104):"
            + Environment.NewLine
            + string.Join(Environment.NewLine, unresolved.Distinct()));
    }

    // ── Fixtures ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The detail selector must answer <b>both</b> overloads of `SelectTemplateCore`.
    /// </summary>
    /// <remarks>
    /// This is a source-level check because no unit test can catch the failure: the framework is what asks the
    /// question, and a `ContentControl` asks the <i>two</i>-argument one. When only the one-argument overload was
    /// overridden the base implementation answered the two-argument question with null, so the presenter was
    /// handed no template and drew its content instead - which put
    /// `MTM_Waitlist.Module_Waitlist.Models.SampleOrder` on every card where the cells belong and meant the
    /// per-Item cell templates were resolved correctly and then discarded. It took reading the running card's
    /// automation tree to find, so it is pinned here rather than left to be rediscovered the same way (FR-025).
    /// </remarks>
    [TestMethod]
    public void UI_WaitlistLineCardDetailTemplateTests_Selector_AnswersBothTemplateQuestionOverloads()
    {
        var path = Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Module_Waitlist",
            "Controls",
            "WaitlistLineCardDetailTemplateSelector.cs");

        Assert.IsTrue(File.Exists(path), $"The detail template selector is gone from '{path}' (FR-025).");

        var source = File.ReadAllText(path);

        StringAssert.Contains(
            source,
            "SelectTemplateCore(object item, DependencyObject container)",
            "The selector overrides only the one-argument template question. A ContentControl asks the two-argument "
            + "one, so the base implementation answers it with null, the per-Item cell template is thrown away, and "
            + "the card draws the row's type name where its cells belong (FR-025, FR-026).");

        StringAssert.Contains(
            source,
            "SelectTemplateCore(object item)",
            "The selector no longer answers the one-argument template question, which other presenters ask (FR-025).");
    }

    /// <summary>
    /// The cell selector, which every slot of every Item's template asks, has to answer both template questions for
    /// the same reason the per-Item selector does: a <c>ContentControl</c> asks the two-argument one, and the base
    /// implementation answers that with null — which makes the presenter draw the cell's type name where its label
    /// and value belong (FR-028, FR-053).
    /// </summary>
    [TestMethod]
    public void UI_WaitlistLineCardDetailTemplateTests_CellSelector_AnswersBothTemplateQuestionOverloads()
    {
        var path = Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Module_Waitlist",
            "Controls",
            "WaitlistCardCellTemplateSelector.cs");

        Assert.IsTrue(File.Exists(path), $"The cell template selector is gone from '{path}' (FR-053).");

        var source = File.ReadAllText(path);

        StringAssert.Contains(
            source,
            "SelectTemplateCore(object item, DependencyObject container)",
            "The cell selector overrides only the one-argument template question, so a ContentControl would be handed "
            + "no cell template at all and every card would draw a type name where its label and value belong (FR-053).");

        StringAssert.Contains(
            source,
            "SelectTemplateCore(object item)",
            "The cell selector no longer answers the one-argument template question (FR-053).");
    }

    /// <summary>
    /// Every description the shipped seed puts on a card asks for the scrolling line, and nothing else on those
    /// cards does — the rule is read from the declarations the app actually ships, not from a copy of them
    /// (FR-053).
    /// </summary>
    [TestMethod]
    public void UI_WaitlistLineCardDetailTemplateTests_EveryShippedDescriptionCell_AsksForTheScrollingLine()
    {
        // The job a component's two cells read, so `deliver-component`'s part description resolves.
        var job = Job() with
        {
            ComponentFacts = new Dictionary<string, RequestComponentFacts>(StringComparer.OrdinalIgnoreCase)
            {
                ["FGT0002000"] = new RequestComponentFacts(
                    "BLANK, 16GA, UNPAINTED, 42.5 X 18.25",
                    "2,885"),
            },
        };

        foreach (var (itemCode, label) in new[]
        {
            ("pickup-coil", "Coil description"),
            ("deliver-coil", "Coil description"),
            ("deliver-component", "Part description"),
            ("other", "Request description"),
        })
        {
            var cells = WaitlistLineCardCell.CellsToDraw(
                WaitlistViewViewModel
                    .CreateSessionOrder(Shape(itemCode), job, SeedDeclarations()[itemCode])
                    .Cells);

            var description = cells.SingleOrDefault(cell => string.Equals(cell.Label?.Trim(), label, StringComparison.OrdinalIgnoreCase));

            Assert.IsNotNull(description, $"`{itemCode}` no longer draws its '{label}' cell, so this test proves nothing about it.");
            Assert.IsTrue(
                description!.UsesScrollingText,
                $"`{itemCode}`'s '{label}' is drawn as a fixed cell, so a long description is clipped rather than scrolled (FR-053).");
            Assert.AreEqual(
                1,
                cells.Count(cell => cell.UsesScrollingText),
                $"`{itemCode}` asks for the scrolling line on more than its description (FR-053).");
        }
    }

    /// <summary>The per-Item detail templates, keyed exactly the way the card's selector looks them up.</summary>
    private static Dictionary<string, XElement> DetailTemplates() =>
        LoadTemplateDictionary()
            .Descendants()
            .Where(element => element.Name.LocalName == "DataTemplate")
            .Select(element => (Key: (string?)element.Attribute(XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml")) ?? string.Empty, Element: element))
            .Where(template => template.Key.StartsWith("WaitlistLineCardDetail_", StringComparison.Ordinal))
            .ToDictionary(template => template.Key, template => template.Element, StringComparer.Ordinal);

    private static XDocument LoadTemplateDictionary() =>
        XDocument.Load(TemplateDictionaryPath());

    private static string TemplateDictionaryPath() => Path.Combine(
        RepositoryPatternScan.FindRepositoryRoot(),
        "Module_Waitlist",
        "Controls",
        "WaitlistLineCardDetailTemplates.xaml");

    /// <summary>
    /// The key prefix the card's selector declares, read from its own source so this suite checks the
    /// dictionary against the selector's rule rather than against a copy of it.
    /// </summary>
    private static string SelectorTemplateKeyPrefix()
    {
        var source = File.ReadAllText(Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Module_Waitlist",
            "Controls",
            "WaitlistLineCardDetailTemplateSelector.cs"));

        var match = Regex.Match(
            source,
            @"\bTemplateKeyPrefix\s*=\s*""(?<value>[^""]+)""",
            RegexOptions.CultureInvariant);

        Assert.IsTrue(match.Success, "The selector no longer declares the key prefix its templates are looked up by.");

        return match.Groups["value"].Value;
    }

    private static XElement TemplateFor(XDocument dictionary, string key) =>
        dictionary
            .Descendants()
            .FirstOrDefault(element => (string?)element.Attribute(XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml")) == key)
        ?? throw new AssertFailedException($"The dictionary carries no template keyed '{key}'.");

    /// <summary>The cell presenters a template draws, in document order, which is slot order.</summary>
    private static IReadOnlyList<XElement> Slots(XElement template) =>
        template
            .Descendants(s_presentation + "ContentControl")
            .ToList();

    /// <summary>The cells a shipped Item draws, read from the declaration the seed ships.</summary>
    private static IReadOnlyList<WaitlistLineCardCell> DrawnCells(
        string itemCode,
        IReadOnlyDictionary<string, string>? answers = null)
    {
        var order = WaitlistViewViewModel.CreateSessionOrder(
            Shape(itemCode, answers),
            Job(),
            SeedDeclarations()[itemCode]);

        return WaitlistLineCardCell.CellsToDraw(order.Cells);
    }

    /// <summary>The labels the shipped seed declares for one Item.</summary>
    private static IReadOnlyList<string> DeclaredLabels(string itemCode) =>
        SeedDeclarations()[itemCode].DetailFields.Select(field => field.Label).ToArray();

    /// <summary>
    /// The shipped declaration seed, parsed once into the configuration the card is handed at run time: every
    /// catalogued Item's declared fields, in declared order, with the source each declares.
    /// </summary>
    private static readonly Lazy<Dictionary<string, RequestItemConfiguration>> s_seedDeclarations =
        new(ReadSeedDeclarations);

    private static Dictionary<string, RequestItemConfiguration> SeedDeclarations() => s_seedDeclarations.Value;

    private static Dictionary<string, RequestItemConfiguration> ReadSeedDeclarations()
    {
        var seed = File.ReadAllText(Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Database",
            "Seeds",
            "seed_waitlist_request_item_configs",
            "create.sql"));

        var declarations = new Dictionary<string, RequestItemConfiguration>(StringComparer.OrdinalIgnoreCase);
        var fields = 0;

        foreach (var item in RequestItemCatalog.Items)
        {
            var row = SeedRow(seed, item.Id);
            var parsed = Regex
                .Matches(
                    row,
                    @"JSON_OBJECT\('label','(?<label>[^']*)',(?<body>[^)]*)\)",
                    RegexOptions.CultureInvariant | RegexOptions.Singleline)
                .Select(match => new RequestItemFieldDefinition
                {
                    Label = match.Groups["label"].Value,
                    Source = Regex.Match(match.Groups["body"].Value, @"'source','(?<source>[^']*)'", RegexOptions.CultureInvariant).Groups["source"].Value,
                    Order = int.Parse(
                        Regex.Match(match.Groups["body"].Value, @"'order',(?<order>\d+)", RegexOptions.CultureInvariant).Groups["order"].Value,
                        CultureInfo.InvariantCulture),
                    IsRequired = string.Equals(
                        Regex.Match(match.Groups["body"].Value, @"'is_required',(?<required>TRUE|FALSE)", RegexOptions.CultureInvariant).Groups["required"].Value,
                        "TRUE",
                        StringComparison.Ordinal),
                })
                .ToArray();

            Assert.IsTrue(parsed.Length > 0, $"The shipped seed no longer declares any field for `{item.Id}`.");

            fields += parsed.Length;
            declarations[item.Id] = new RequestItemConfiguration { Item = item.Id, DetailFields = parsed };
        }

        Assert.IsTrue(fields >= 60, $"The seed parse reached only {fields} declared fields, so a clean result would prove nothing.");

        return declarations;
    }

    /// <summary>
    /// The shipped INSERT row for one Item, from its Item code to the row's own terminator. Line endings are
    /// normalised first, because the row boundary is the blank line between two <c>VALUES</c> entries.
    /// </summary>
    private static string SeedRow(string seedText, string itemCode)
    {
        var normalized = seedText.Replace("\r\n", "\n", StringComparison.Ordinal);
        var start = normalized.IndexOf("'" + itemCode + "'", StringComparison.Ordinal);

        Assert.IsTrue(start >= 0, $"The shipped configuration seed no longer carries a row for {itemCode}.");

        var end = normalized.IndexOf("),\n\n", start, StringComparison.Ordinal);
        return end < 0 ? normalized[start..] : normalized[start..end];
    }

    /// <summary>An Item declaration carrying the fields under test, with nothing else claimed about the Item.</summary>
    private static RequestItemConfiguration Declaration(
        string item,
        params (string Label, string Source, int Order, bool IsRequired)[] fields) => new()
    {
        Item = item,
        DetailFields = fields
            .Select(field => new RequestItemFieldDefinition
            {
                Label = field.Label,
                Source = field.Source,
                Order = field.Order,
                IsRequired = field.IsRequired,
            })
            .ToArray(),
    };

    /// <summary>Builds a request of the given Item's shape carrying distinctive values a source can supply.</summary>
    /// <param name="itemCode">The Item code under test.</param>
    /// <param name="answers">The answers the request captured beyond its one answer, or null for none.</param>
    /// <param name="inputValue">The request's one captured answer, or null for a request that captured none.</param>
    private static WaitlistRequest Shape(
        string itemCode,
        IReadOnlyDictionary<string, string>? answers = null,
        string? inputValue = DefaultInput) => new()
    {
        Building = "Expo Drive",
        WorkCenter = "Expo Line 7",
        Category = RequestItemCatalog.FindById(itemCode)!.Category.ToString(),
        Item = itemCode,
        InputValue = inputValue,
        Answers = answers,
        ActiveSetupJobId = "JOB-9001",
        WorkCenterName = "Expo Line 7",
        RequesterEmployeeNumber = "6331",
        RequesterEmployeeName = "Dana Whitfield",
        Status = "Pending",
        RequestedUtc = DateTimeOffset.UtcNow.AddMinutes(-12),
        TargetTimeUtc = DateTimeOffset.UtcNow.AddMinutes(48),
    };

    private static Dictionary<string, string> Answers(params (string Label, string Value)[] answers) =>
        answers.ToDictionary(answer => answer.Label, answer => answer.Value, StringComparer.OrdinalIgnoreCase);

    /// <summary>The requesting job's material, so a declared job label has something to resolve to.</summary>
    private static RequestJobPartAvailability Job() => RequestJobPartAvailability.All with
    {
        CoilPartNumber = "MMC0001000",
        CoilDescription = "38 in x 4200 lb",
        CoilAverageWeight = "4200 lb",
        FlatstockPartNumber = "MMF0000500",
        JobPartNumber = "MMC0001000",
        ScrapType = "Scrap Type Required",
        DieNumber = "FGT0002000",
        DieLocation = "DIE SHOP",
        DunnageParts = [new RequestDunnagePart { PartNumber = "DUN0000100", HomeLocation = "A-12" }],
    };

    /// <summary>
    /// The same job with a real scrap decision and the lugger the arrangement settled for it, which is what the
    /// scrap card's `Lugger Location` cell reads (FR-022).
    /// </summary>
    private static RequestJobPartAvailability ScrapJob() => Job() with
    {
        ScrapType = "3003 Aluminum",
        LuggerLocation = "Door 6 Lugger",
    };

    /// <summary>Captures what the facade forwards to the store's log, so a warning can be observed without a store.</summary>
    private sealed class RecordingAppLogSink : IAppLogSink
    {
        public List<string> Warnings { get; } = [];

        public void Info(string module, string message, string? target = null)
        {
        }

        public void Error(string module, string message, Exception? exception = null)
        {
        }

        public void Warn(string module, string message, string? errorType = null) => Warnings.Add(message);
    }
}
