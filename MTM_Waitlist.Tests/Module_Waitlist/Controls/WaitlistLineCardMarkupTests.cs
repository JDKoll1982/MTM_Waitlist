using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Waitlist.Controls;

/// <summary>
/// The waitlist card's own markup, read as markup. Each check pins one part of the four-zone anatomy the
/// request asks for, and each is written so that a later edit has to be deliberate rather than quiet.
/// </summary>
/// <remarks>
/// The card lives in the application project, which this suite deliberately does not reference, so the card is
/// read from disk as XML. Everything the card *cannot* decide for itself - the four status column themes, the
/// per-Item cells - is checked here only as far as its binding, because the palette and the templates are other
/// slices' files.
/// </remarks>
[TestClass]
public sealed class WaitlistLineCardMarkupTests
{
    private static readonly XNamespace s_presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private static readonly XNamespace s_xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

    // ── The four zones, left to right, separated by vertical rules (FR-001 to FR-010) ──────────────────

    [TestMethod]
    public void UI_WaitlistLineCardMarkupTests_Card_DrawsTheFourZonesInOrderBetweenVerticalRules()
    {
        var card = LoadCard();

        var zones = new[] { "IdentityZone", "StatusZone", "DetailZone", "ActionZone" }
            .Select(name => (Name: name, Element: Named(card, name)))
            .ToArray();

        var columns = zones.Select(zone => Column(zone.Element)).ToArray();

        Assert.IsTrue(
            columns.SequenceEqual(columns.OrderBy(value => value)),
            $"The four zones do not read left to right: {string.Join(", ", zones.Select((zone, index) => $"{zone.Name} at column {columns[index]}"))}.");
        Assert.AreEqual(
            4,
            columns.Distinct().Count(),
            "Two zones share a column, so the row is not four zones side by side (FR-001).");

        foreach (var (rule, zonesToItsLeft) in new[]
        {
            ("IdentityStatusRule", 1),
            ("StatusDetailRule", 2),
            ("DetailActionRule", 3),
        })
        {
            var divider = Named(card, rule);
            var column = Column(divider);

            Assert.AreEqual("1", (string?)divider.Attribute("Width"), $"{rule} is not a one-pixel vertical rule.");
            Assert.AreEqual(
                zonesToItsLeft,
                columns.Count(value => value < column),
                $"{rule} does not sit between the zones it was placed between (FR-001).");
            Assert.AreEqual(
                4 - zonesToItsLeft,
                columns.Count(value => value > column),
                $"{rule} does not sit between the zones it was placed between (FR-001).");
        }

        // The accent bar belongs to the identity zone: it is the leftmost column of the row (FR-002).
        var accent = card
            .Descendants(s_presentation + "Border")
            .FirstOrDefault(border => ((string?)border.Attribute("Background"))?.Contains("AccentBrush", StringComparison.Ordinal) == true);

        Assert.IsNotNull(accent, "The card no longer draws its accent bar (FR-002).");
        Assert.AreEqual(0, Column(accent!), "The accent bar is no longer the leftmost element of the row (FR-002).");
    }

    [TestMethod]
    public void UI_WaitlistLineCardMarkupTests_Card_ShowsTheWorkCentreAsALargeUnlabelledValueBeneathThePicture()
    {
        var identity = Named(LoadCard(), "IdentityZone");

        var value = identity
            .Descendants(s_presentation + "TextBlock")
            .FirstOrDefault(text => ((string?)text.Attribute("Text"))?.Contains("Order.RequestedPressName", StringComparison.Ordinal) == true);

        Assert.IsNotNull(
            value,
            "The requesting work centre is not drawn in the identity zone, so nothing stands beneath the picture (FR-004).");

        var fontSize = double.Parse((string?)value!.Attribute("FontSize") ?? "0", System.Globalization.CultureInfo.InvariantCulture);
        Assert.IsTrue(fontSize >= 18d, $"The work centre reads as body text ({fontSize}pt) rather than a large value (FR-004).");

        // Nothing labels it: the value is the whole element, which is what makes it unlabelled (FR-004).
        Assert.IsFalse(
            value.Attributes().Any(attribute => attribute.Name.LocalName == "Uid" && attribute.Name.Namespace == s_xaml),
            "The work centre carries a label identifier (FR-004).");
        Assert.IsFalse(
            identity
                .Descendants(s_presentation + "TextBlock")
                .Any(text => text != value && string.IsNullOrWhiteSpace((string?)text.Attribute("Text")) == false
                    && ((string?)text.Attribute("Text"))!.Contains("work centre", StringComparison.OrdinalIgnoreCase)),
            "The work centre is labelled in front of, which FR-004 forbids.");

        // It sits beneath the picture: the picture is earlier in the same stack.
        var picture = identity
            .Descendants()
            .FirstOrDefault(element => element.Name.LocalName == "ClickToEnlargeImageView");

        Assert.IsNotNull(picture, "The identity zone no longer holds the material picture (FR-002).");
        Assert.IsTrue(
            picture!.IsBefore(value),
            "The work centre is not drawn beneath the picture (FR-004).");
    }

    [TestMethod]
    public void UI_WaitlistLineCardMarkupTests_Card_CarriesTheAccentBarThePictureAndTheNewMessageBadge()
    {
        var card = LoadCard();
        var identity = Named(card, "IdentityZone");

        var host = identity
            .Descendants(s_presentation + "Border")
            .FirstOrDefault(border => (string?)border.Attribute("Width") == "96" && (string?)border.Attribute("Height") == "96");

        Assert.IsNotNull(host, "The identity zone no longer holds a fixed 96x96 picture host (FR-002, FR-009).");

        var picture = host!
            .Descendants()
            .FirstOrDefault(element => element.Name.LocalName == "ClickToEnlargeImageView");

        Assert.IsNotNull(picture, "The picture host no longer draws the request's picture (FR-002).");
        StringAssert.Contains(
            (string?)picture!.Attribute("Source") ?? string.Empty,
            "Order.EffectiveImagePath",
            "The picture must be the request's material part, resolved through the application's one picture rule (FR-002).");
        StringAssert.Contains(
            (string?)picture.Attribute("Source") ?? string.Empty,
            "ResolvedImagePathToSourceConverter",
            "The picture must go through the resolver that applies the application's one picture rule (FR-002).");
        Assert.IsFalse(
            ((string?)picture.Attribute("Source"))!.Contains("ItemCode", StringComparison.Ordinal),
            "The picture must not resolve from the Item - that is the picture for the kind of request (FR-002).");

        // The badge keeps its meaning and is omitted when the request has no new messages (FR-002, FR-003).
        var badge = host
            .Descendants(s_presentation + "Border")
            .FirstOrDefault(border =>
                ((string?)border.Attribute("Visibility"))?.Contains("Order.HasNewMessages", StringComparison.Ordinal) == true);

        Assert.IsNotNull(badge, "The new-message badge is gone (FR-002).");
        StringAssert.Contains(
            TipOf(badge!),
            "Order.NewMessageTooltip",
            "The badge must announce what it means from the row, not from a literal (FR-002, FR-104).");
    }

    /// <summary>
    /// The no-cells fallback must DRAW NOTHING, and it must be a template with a real root rather than a
    /// contentless one. The cell area is a `ContentControl` holding the row, and a presenter with no usable
    /// template falls back to rendering its content - so a contentless fallback printed the row's own type name
    /// (`MTM_Waitlist.Module_Waitlist.Models.SampleOrder`) on the card where the Item's cells belong. A template
    /// whose root is an empty panel draws nothing, which is what "no cells" has to mean (FR-026, FR-060).
    /// </summary>
    [TestMethod]
    public void UI_WaitlistLineCardMarkupTests_Card_TheNoCellsFallbackDrawsNothingRatherThanTheRowsTypeName()
    {
        var card = LoadCard();

        var fallback = card
            .Descendants(s_presentation + "DataTemplate")
            .FirstOrDefault(template => (string?)template.Attribute(s_xaml + "Key") == "WaitlistLineCardDetailFallback");

        Assert.IsNotNull(fallback, "The card declares no no-cells fallback template (FR-060).");
        Assert.IsTrue(
            fallback!.Elements().Any(),
            "The no-cells fallback is a contentless template. A presenter given no usable template then renders its content - the row - and the row's type name is drawn where the cells belong (FR-060).");
        Assert.AreEqual(
            0,
            fallback.Descendants(s_presentation + "TextBlock").Count(),
            "The no-cells fallback draws text, so an Item with no template would show a value the card never resolved (FR-060).");
        Assert.AreEqual(
            0,
            fallback.Descendants(s_presentation + "ContentControl").Count(),
            "The no-cells fallback draws a cell presenter, so an Item with no template would draw cells anyway (FR-060).");

        // The selector is handed that fallback, so the empty template is the one it actually draws.
        var selector = card
            .Descendants()
            .FirstOrDefault(element => ((string?)element.Attribute(s_xaml + "Key"))?.Contains("WaitlistLineCardDetailTemplateSelector", StringComparison.Ordinal) == true);

        Assert.IsNotNull(selector, "The card no longer declares the detail template selector (FR-025).");
        StringAssert.Contains(
            (string?)selector!.Attribute("FallbackTemplate") ?? string.Empty,
            "WaitlistLineCardDetailFallback",
            "The selector is not handed the card's no-cells fallback, so it falls back to a contentless template of its own (FR-060).");
    }

    /// <summary>
    /// The ring and its box carry an explicit size. The platform `ProgressRing` has no size of its own and draws
    /// at a small default, so an unsized ring inside a sized box renders as a hairline no bigger than a glyph -
    /// which is what the card was doing (FR-013, D44). The track is painted too, so the unfilled part of the
    /// allotment is visible instead of invisible.
    /// </summary>
    [TestMethod]
    public void UI_WaitlistLineCardMarkupTests_Card_SizesTheTimeRingRatherThanLeavingItAtThePlatformDefault()
    {
        var host = Named(LoadCard(), "TimeRingHost");

        Assert.IsFalse(
            string.IsNullOrWhiteSpace((string?)host.Attribute("Width")) || string.IsNullOrWhiteSpace((string?)host.Attribute("Height")),
            "The ring's box carries no size, so nothing bounds the ring (FR-013, D44).");

        var ring = host
            .Descendants()
            .Single(element => element.Name.LocalName == "ProgressRing");

        Assert.IsFalse(
            string.IsNullOrWhiteSpace((string?)ring.Attribute("Width")) || string.IsNullOrWhiteSpace((string?)ring.Attribute("Height")),
            "The ring carries no explicit size, so it draws at the platform default - a hairline no bigger than a glyph inside its box (FR-013, D44).");

        Assert.AreEqual(
            (string?)host.Attribute("Width"),
            (string?)ring.Attribute("Width"),
            "The ring is not the size of its box, so it floats inside it rather than filling it (D44).");

        Assert.IsFalse(
            string.IsNullOrWhiteSpace((string?)ring.Attribute("Background")),
            "The ring paints no track, so the unfilled part of the allotment is invisible and the ring reads as a bare arc (FR-013, D44).");

        var overlay = host
            .Descendants(s_presentation + "TextBlock")
            .Single(text => ((string?)text.Attribute("Text"))?.Contains("Order.RingText", StringComparison.Ordinal) == true);

        Assert.IsTrue(
            double.TryParse((string?)overlay.Attribute("FontSize"), out var size) && size >= 15,
            "The time text inside the ring is too small to read at the ring's own size (FR-013, D44).");
    }

    // ── What the card never shows (FR-005, FR-029, D19) ───────────────────────────────────────────────

    [TestMethod]
    public void UI_WaitlistLineCardMarkupTests_Card_NeverDrawsTheWorkCentreOrTheRequestingUserAsADetailCell()
    {
        var card = LoadCard();
        var detail = Named(card, "DetailZone");
        var identity = Named(card, "IdentityZone");

        Assert.IsTrue(
            detail
                .Descendants(s_presentation + "ContentControl")
                .Any(presenter => ((string?)presenter.Attribute("ContentTemplateSelector"))?.Contains("WaitlistLineCardDetailTemplateSelector", StringComparison.Ordinal) == true),
            "The detail zone's cells are not resolved through the per-Item template selector, so the card decides them itself (FR-025).");

        // A cell the card names would be one every Item draws: the requesting work centre is the identity
        // zone's value and the requesting user belongs to the request's own page (FR-005, D19).
        foreach (var item in RequestItemCatalog.Items)
        {
            Assert.IsFalse(
                detail.ToString().Contains(item.Id, StringComparison.Ordinal),
                $"The detail zone names the Item '{item.Id}', so a cell is chosen per Item rather than from the Item's declaration (FR-025).");
        }

        Assert.IsFalse(
            card.Descendants().Any(element => element.Attributes()
                .Any(attribute => attribute.Value.Contains("Order.RequestedByName", StringComparison.Ordinal))),
            "The card binds the requesting user: they appear only on the request's own detail page (FR-005).");
        Assert.IsFalse(
            detail.ToString().Contains("Order.RequestedPressName", StringComparison.Ordinal),
            "The requesting work centre is drawn as a detail cell (FR-005, D19).");
        Assert.IsTrue(
            identity.ToString().Contains("Order.RequestedPressName", StringComparison.Ordinal),
            "The requesting work centre is not the identity zone's own value (FR-004).");

        // The label a person reads is an attribute of an element: a comment that happens to name these things
        // is not a drawn cell.
        var drawnValues = card
            .Descendants()
            .SelectMany(element => element.Attributes().Select(attribute => attribute.Value))
            .ToList();

        foreach (var label in new[] { "Requested by", "Requesting user", "Requesting work centre", "Requested work centre" })
        {
            Assert.IsFalse(
                drawnValues.Any(value => value.Contains(label, StringComparison.OrdinalIgnoreCase)),
                $"The card draws a '{label}' cell, which FR-005 forbids on every Item.");
        }
    }

    [TestMethod]
    public void UI_WaitlistLineCardMarkupTests_Card_DieLocationIsNotAUniversalCell()
    {
        var card = LoadCard();

        Assert.IsFalse(
            card.ToString().Contains("Die Location", StringComparison.Ordinal),
            "`Die Location` is drawn by the card itself, so every Item would show it; it belongs to the two die Items' own templates (FR-029, D16).");

        // The only labels the card may draw are its own chrome; a cell's label is its Item's declaration.
        var labels = card
            .Descendants(s_presentation + "TextBlock")
            .Select(text => (string?)text.Attribute("Text"))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();

        Assert.IsTrue(
            labels.All(value => value!.StartsWith('{') || value.Contains("Order.", StringComparison.Ordinal)),
            $"The card draws a literal cell label, which is neither the Item's declaration nor a resource: {string.Join(", ", labels.Where(value => !value!.StartsWith('{') && !value.Contains("Order.", StringComparison.Ordinal)))}.");
    }

    // ── The status column: the word, the ring, the person (FR-008, FR-011 to FR-023) ──────────────────

    [TestMethod]
    public void UI_WaitlistLineCardMarkupTests_Card_KeepsNoMetadataRowAndNoStatusPill()
    {
        var card = LoadCard();

        foreach (var label in new[] { "Requested by", "Press", "Remaining time", "Waiting" })
        {
            Assert.IsFalse(
                card
                    .Descendants(s_presentation + "TextBlock")
                    .Any(text => string.Equals((string?)text.Attribute("Text"), label, StringComparison.Ordinal)),
                $"The card still draws its '{label}' metadata row; the four-zone anatomy supersedes the four metadata rows (FR-099, FR-001).");
        }

        Assert.IsFalse(
            card.ToString().Contains("StatusBadgeText", StringComparison.Ordinal),
            "The card still draws the coloured status pill from the row's badge text (FR-008).");
        Assert.IsFalse(
            card.ToString().Contains("HasStatusBadge", StringComparison.Ordinal),
            "The card still hides a status pill by the badge flag (FR-008).");

        var pill = card
            .Descendants(s_presentation + "Border")
            .FirstOrDefault(border => (string?)border.Attribute("Height") == "36" && border.Attribute("CornerRadius") is not null);

        Assert.IsNull(pill, "The compact 36-high status pill is still drawn (FR-008).");
    }

    [TestMethod]
    public void UI_WaitlistLineCardMarkupTests_Card_TheStatusWordPaintsNoBackgroundInAnyState()
    {
        var word = Named(Named(LoadCard(), "StatusZone"), "StatusWordButton");

        var brushes = word
            .Descendants()
            .Where(element => element.Name.LocalName == "SolidColorBrush" && (string?)element.Attribute(s_xaml + "Key") is { } key
                && key.StartsWith("ButtonBackground", StringComparison.Ordinal))
            .ToList();

        foreach (var state in new[] { "ButtonBackground", "ButtonBackgroundPointerOver", "ButtonBackgroundPressed", "ButtonBackgroundDisabled" })
        {
            var painted = brushes.Where(brush => (string?)brush.Attribute(s_xaml + "Key") == state).ToList();

            Assert.IsTrue(painted.Count >= 3, $"The status word's button overrides `{state}` in fewer than the three theme dictionaries, so a theme still paints the platform's own fill behind the word (FR-008).");
            Assert.IsTrue(
                painted.All(brush => (string?)brush.Attribute("Color") == "Transparent"),
                $"The status word's button paints a `{state}` fill behind the word (FR-008).");
        }
    }

    [TestMethod]
    public void UI_WaitlistLineCardMarkupTests_Card_DrawsTheStatusWordAsPlainTextWithNoLabel()
    {
        var status = Named(LoadCard(), "StatusZone");

        var word = status
            .Descendants(s_presentation + "TextBlock")
            .FirstOrDefault(text => ((string?)text.Attribute("Text"))?.Contains("Order.StatusWord", StringComparison.Ordinal) == true);

        Assert.IsNotNull(word, "The status zone draws no status word (FR-011, FR-012).");
        Assert.IsTrue(
            word!.Descendants().Count() == 0 && (string?)word.Attribute("Background") is null,
            "The status word is drawn as a pill rather than plain coloured text (FR-008).");
        Assert.IsNotNull(
            word.Attribute("Foreground"),
            "The status word carries no colour of its own, so the column cannot read as its own state (FR-008).");

        Assert.IsFalse(
            status.ToString().Contains("Status:", StringComparison.Ordinal),
            "A label stands in front of the status word, and the word must stand alone (FR-008, D11).");
    }

    [TestMethod]
    public void UI_WaitlistLineCardMarkupTests_Card_CarriesTheWholeColumnThemeOnTheStatusColumn()
    {
        var card = LoadCard();
        var status = Named(card, "StatusZone");

        var themeBinding = status
            .DescendantsAndSelf()
            .SelectMany(element => element.Attributes())
            .FirstOrDefault(attribute => attribute.Value.Contains("Order.StatusTheme", StringComparison.Ordinal));

        Assert.IsNotNull(
            themeBinding,
            "The whole status column carries no theme binding, so the three themes FR-019 asks for have nothing to read.");

        // The column's colour comes from the card's own themed brush, so the word and the ring cannot drift
        // apart and neither is a per-element scale (FR-019).
        var brushConsumers = card
            .Descendants()
            .Where(element => element.Attributes()
                .Any(attribute => attribute.Value.Contains("StatusColumnBrush", StringComparison.Ordinal)))
            .ToList();

        Assert.IsTrue(brushConsumers.Count >= 2, "The status column's theme brush colours fewer than the word and the ring (FR-019).");
    }

    [TestMethod]
    public void UI_WaitlistLineCardMarkupTests_Card_TheRingIsThePlatformProgressRingInItsDeterminateState()
    {
        var host = Named(LoadCard(), "TimeRingHost");
        var ring = host
            .Descendants()
            .SingleOrDefault(element => element.Name.LocalName == "ProgressRing");

        Assert.IsNotNull(ring, "The status zone draws no ProgressRing, so the time ring is not the platform control (FR-013).");
        Assert.AreEqual("False", (string?)ring!.Attribute("IsIndeterminate"), "The ring must be determinate so it can carry a value (FR-013).");
        Assert.AreEqual("0", (string?)ring.Attribute("Minimum"), "The ring's scale must be the row's fraction, from zero (FR-013).");
        Assert.AreEqual("1", (string?)ring.Attribute("Maximum"), "The ring's scale must be the row's fraction, to one (FR-013).");
        StringAssert.Contains(
            (string?)ring.Attribute("Value") ?? string.Empty,
            "Order.RingValue",
            "The ring must fill from the row's own fraction (FR-013).");
        StringAssert.Contains(
            (string?)ring.Attribute("AutomationProperties.Name") ?? string.Empty,
            "RingAccessibleName",
            "The ring must state which of the two things it measures (FR-016).");
        Assert.IsNull(
            ring.Attribute("Click"),
            "The ring is the click target, and the status word is the element that becomes clickable (FR-070).");
    }

    [TestMethod]
    public void UI_WaitlistLineCardMarkupTests_Card_OverlaysTheRowsTimeTextOnTheRingAndDrawsNoPercentageOrElapsedLine()
    {
        var host = Named(LoadCard(), "TimeRingHost");

        var overlay = host
            .Descendants(s_presentation + "TextBlock")
            .FirstOrDefault(text => ((string?)text.Attribute("Text"))?.Contains("Order.RingText", StringComparison.Ordinal) == true);

        Assert.IsNotNull(
            overlay,
            "The ring carries no time text inside it, or carries a text other than the row's own ring text: the overlay must be Order.RingText, which reads the time left while the request is live and the time taken once it has closed (FR-013, FR-015).");
        Assert.AreEqual(
            1,
            host.Descendants().Count(element => element.Name.LocalName == "ProgressRing"),
            "The ring host draws more than the one ring, so the overlaid text is not a cell on top of it.");

        var card = LoadCard();

        Assert.IsFalse(
            card.Descendants(s_presentation + "TextBlock").Any(text => ((string?)text.Attribute("Text"))?.Contains("Order.WaitingForText", StringComparison.Ordinal) == true),
            "The card draws an elapsed-time line, and the status zone draws none (FR-021).");
        Assert.IsFalse(
            card.ToString().Contains("Elapsed", StringComparison.Ordinal),
            "The card draws an elapsed-time line (FR-021).");
        Assert.IsFalse(
            card.ToString().Contains("%", StringComparison.Ordinal),
            "The card draws a percentage, and the status zone draws none (FR-021).");
    }

    [TestMethod]
    public void UI_WaitlistLineCardMarkupTests_Card_RemovesAStatusRowWithNoValueRatherThanLeavingItBlank()
    {
        var status = Named(LoadCard(), "StatusZone");

        var valueRows = new (string Name, string Visibility)[]
        {
            ("StatusWordButton", "HasStatusWord"),
            ("StatusPersonText", "Order.HasPerson"),
            ("StatusRequesterText", "Order.HasRequester"),
        };

        foreach (var (name, expected) in valueRows)
        {
            var row = Named(status, name);

            Assert.IsTrue(
                ((string?)row.Attribute("Visibility"))?.Contains(expected, StringComparison.Ordinal) == true,
                $"{name} is drawn even when it has no value; a status row with no value is removed rather than left blank (FR-018).");
        }
    }

    [TestMethod]
    public void UI_WaitlistLineCardMarkupTests_Card_PersonRowReadsTheRowsPersonAndTheClosedRequestsRequester()
    {
        var status = Named(LoadCard(), "StatusZone");

        StringAssert.Contains(
            LineText(Named(status, "StatusPersonText")),
            "Order.PersonText",
            "The person row must read the row's own person, which is the requester while unclaimed and the handler once claimed (FR-022).");
        StringAssert.Contains(
            LineText(Named(status, "StatusRequesterText")),
            "Order.RequesterText",
            "The fourth row must read the requester, which the row answers only once the request has closed (FR-022).");
    }

    /// <summary>
    /// The value a scrolling row draws. The row is a host for a line, so the binding is the line's <c>Text</c> rather
    /// than an attribute of the row itself.
    /// </summary>
    private static string LineText(System.Xml.Linq.XElement row) =>
        (string?)row.Descendants()
            .FirstOrDefault(element => element.Name.LocalName == "TextBlock")
            ?.Attribute("Text") ?? string.Empty;

    [TestMethod]
    public void UI_WaitlistLineCardMarkupTests_Card_OpensAFlyoutFromTheStatusWordOnlyForTheRowsOwnHandler()
    {
        var status = Named(LoadCard(), "StatusZone");
        var word = Named(status, "StatusWordButton");

        Assert.AreEqual(
            "Button",
            word.Name.LocalName,
            "The status word is not a button, so the row's own handler has no way to report a condition (FR-070).");
        StringAssert.Contains(
            (string?)word.Attribute("IsEnabled") ?? string.Empty,
            "Order.CanSetStatusCondition",
            "The status word is not gated on the row's own handler, so the picker would be offered to people the store refuses (FR-070, FR-074).");
        Assert.IsNotNull(
            word.Descendants(s_presentation + "Flyout").FirstOrDefault() ?? word.Descendants().FirstOrDefault(element => element.Name.LocalName == "Flyout"),
            "The status word declares no flyout host (FR-071).");
        StringAssert.Contains(
            (string?)word.Attribute("Tag") ?? string.Empty,
            "ConditionOptions",
            "The flyout host does not carry the row's condition options (FR-071).");
        StringAssert.Contains(
            (string?)word.Attribute("AutomationProperties.HelpText") ?? string.Empty,
            "StatusWordHelpText",
            "Whether the word is operable is not announced (FR-075).");
    }

    [TestMethod]
    public void UI_WaitlistLineCardMarkupTests_Card_TheStatusWordIsTheOnlyInteractiveElementInTheBody()
    {
        var card = LoadCard();
        var body = new[] { "IdentityZone", "StatusZone", "DetailZone" }
            .Select(name => Named(card, name))
            .ToList();

        foreach (var zone in body)
        {
            foreach (var element in zone.DescendantsAndSelf())
            {
                var clickable = element
                    .Attributes()
                    .Where(attribute => attribute.Name.LocalName.Contains("Click", StringComparison.Ordinal)
                        || attribute.Name.LocalName.Contains("Tapped", StringComparison.Ordinal)
                        || attribute.Name.LocalName == "Command")
                    .ToList();

                // The part link is a button too, so it takes its own click rather than opening the request.
                var isTheOneInteractiveElement = (string?)element.Attribute(s_xaml + "Name") is "StatusWordButton" or "DetailIdentifierLink";

                Assert.IsTrue(
                    clickable.Count == 0 || isTheOneInteractiveElement,
                    $"The card body draws another interactive element, and the card body no longer opens the request: {element.Name} {string.Join(", ", clickable.Select(attribute => attribute.Name.LocalName))} (FR-069).");
            }
        }

        Assert.IsTrue(
            Named(card, "StatusZone").Descendants(s_presentation + "Button").Count() == 1,
            "The status zone draws more than the one interactive element the body allows (FR-069).");
    }

    // ── The detail zone (FR-007, FR-024, FR-025, FR-027) ──────────────────────────────────────────────

    [TestMethod]
    public void UI_WaitlistLineCardMarkupTests_Card_DetailTitleRowSpansBothColumnsOverTheSelectorBoundCells()
    {
        var detail = Named(LoadCard(), "DetailZone");

        Assert.AreEqual("Grid", detail.Name.LocalName, "The detail zone is not an inner grid (FR-024).");
        Assert.AreEqual(
            2,
            detail.Elements(s_presentation + "Grid.ColumnDefinitions").Elements(s_presentation + "ColumnDefinition").Count(),
            "The detail zone's inner grid is not two columns wide (FR-024).");
        Assert.AreEqual(
            3,
            detail.Elements(s_presentation + "Grid.RowDefinitions").Elements(s_presentation + "RowDefinition").Count(),
            "The detail zone's inner grid is not three rows tall (FR-024).");

        var title = Named(detail, "DetailTitleRow");

        Assert.AreEqual("0", (string?)title.Attribute("Grid.Row"), "The title pair is not the detail grid's top row (FR-024).");
        Assert.AreEqual("2", (string?)title.Attribute("Grid.ColumnSpan"), "The title pair does not span both columns (FR-007, FR-024).");

        var category = Named(detail, "DetailCategoryWord");
        var identifier = Named(detail, "DetailIdentifier");

        StringAssert.Contains(
            (string?)category.Attribute("Text") ?? string.Empty,
            "Order.Title",
            "The title row does not carry the request's category word (FR-007).");
        StringAssert.Contains(
            (string?)identifier.Attribute("Text") ?? string.Empty,
            "Order.Subtitle",
            "The title row does not carry the request's own identifier (FR-007).");

        // The pair reads on one line - `Pickup 22-74371-075` - centred as a pair, both halves drawn the same way so
        // the line reads as one title rather than as a heading with a subtitle beside it (FR-007).
        Assert.AreEqual(
            "Center",
            (string?)title.Attribute("HorizontalAlignment"),
            "The title pair is not centred over the cells (FR-007).");
        Assert.AreEqual(
            "1",
            (string?)identifier.Attribute("Grid.Column"),
            "The identifier is not drawn beside the Item's word, so the title reads as two lines rather than one (FR-007).");
        Assert.IsTrue(
            category.IsBefore(identifier),
            "The identifier is drawn before the Item's word, so the line does not lead with the Item (FR-007).");
        Assert.AreEqual(
            "Center",
            (string?)category.Attribute("VerticalAlignment"),
            "The Item's word is not centred on the title line (FR-007).");
        Assert.AreEqual(
            "Center",
            (string?)identifier.Attribute("VerticalAlignment"),
            "The identifier is not centred on the title line (FR-007).");
        Assert.AreEqual(
            (string?)category.Attribute("FontSize"),
            (string?)identifier.Attribute("FontSize"),
            "The Item's word and its identifier are drawn at different sizes, so the one line reads as two (FR-007).");
        Assert.AreEqual(
            (string?)category.Attribute("FontWeight"),
            (string?)identifier.Attribute("FontWeight"),
            "The Item's word and its identifier are drawn at different weights, so the one line reads as two (FR-007).");

        var cells = Named(detail, "DetailCells");

        Assert.AreEqual("1", (string?)cells.Attribute("Grid.Row"), "The cells are not drawn beneath the title row (FR-024).");
        Assert.AreEqual("2", (string?)cells.Attribute("Grid.RowSpan"), "The cell area does not cover the grid's two lower rows (FR-024).");
        StringAssert.Contains(
            (string?)cells.Attribute("ContentTemplateSelector") ?? string.Empty,
            "WaitlistLineCardDetailTemplateSelector",
            "The cell area is not resolved through the per-Item template selector (FR-025).");
        StringAssert.Contains(
            (string?)cells.Attribute("Content") ?? string.Empty,
            "Order",
            "The cell area is not handed the row it must draw (FR-025).");

        // The grid as a whole is centred in its area, whether or not its lower rows are filled: a card holding two
        // cells was pinned to the top of a taller section and its grid read as dropped in from above rather than
        // placed in its area (FR-027).
        Assert.AreEqual(
            "Center",
            (string?)detail.Attribute("VerticalAlignment") ?? string.Empty,
            "The detail grid is not centred in its area, so a grid whose lower row is empty sits against the top of the section (FR-027).");

        Assert.IsFalse(
            ((string?)detail.Attribute("VerticalAlignment") ?? string.Empty).Contains("Cells.Count", StringComparison.Ordinal),
            "The grid's alignment still follows the row's cell count, so a short grid and a full one are placed differently (FR-027).");

        // The rows a row with no data vacates leave nothing behind: the zone adds no spacing of its own, and the
        // gap between the title and the cells is owned by the cell area, which is collapsed on the same count
        // (FR-006, FR-026).
        Assert.AreEqual(
            "0",
            (string?)detail.Attribute("RowSpacing") ?? "0",
            "The detail zone spaces its rows apart, so a row with no cells leaves a gap below its title (FR-026).");
        Assert.AreEqual(
            "0,4,0,0",
            (string?)cells.Attribute("Margin"),
            "The gap between the title and the cells is not owned by the cell area, so it survives a row that has no cells (FR-006).");

        var collapse = (string?)cells.Attribute("Visibility") ?? string.Empty;

        StringAssert.Contains(
            collapse,
            "WaitlistCardCellVisibilityConverter",
            "The cell area is drawn even when the row filled no cells (FR-006, FR-026).");
        StringAssert.Contains(
            collapse,
            "Cells.Count",
            "The cell area's collapse is not decided by the number of cells the row filled (FR-006).");
    }

    // ── The action zone (FR-061 to FR-069) ────────────────────────────────────────────────────────────

    [TestMethod]
    public void UI_WaitlistLineCardMarkupTests_Card_ActionZoneIsThreeColumnsByTwoRowsInRowMajorOrder()
    {
        var card = LoadCard();
        var action = Named(card, "ActionZone");

        // The zone is laid out by the one panel that owns the rule, not by a row and a column written per
        // button: a fixed grid leaves a hole where an unavailable action was removed and the row then reads as
        // if a button had failed to load (FR-061, FR-063). The rule's arithmetic is pinned by the layout's own
        // tests; what is pinned here is that the card is using that panel at all.
        Assert.AreEqual(
            "WaitlistActionReflowPanel",
            action.Name.LocalName,
            "The action zone is not laid out by the reflow panel, so an unavailable action leaves a hole (FR-061, FR-063).");
        Assert.IsFalse(
            action.Descendants(s_presentation + "ColumnDefinition").Any(),
            "The action zone declares columns of its own, so the three-column rule is not the one in effect (FR-061).");

        // Row-major order, which is the order the panel lays the drawn buttons out in: Accept, Cancel and Give
        // back across the first row, then Complete and Open details (FR-061, D44). Printing is not among them:
        // it belongs to the request's own page rather than to the card (FR-067, D59).
        var expected = new (string Id, string? Gate)[]
        {
            ("WaitlistLineCardView_AcceptButton", "Order.CanAccept"),
            ("WaitlistLineCardView_CancelButton", "Order.CanCancelRequest"),
            ("WaitlistLineCardView_ReleaseButton", "Order.CanCompleteOrRelease"),
            ("WaitlistLineCardView_CompleteButton", "Order.CanCompleteOrRelease"),
            ("WaitlistLineCardView_OpenDetailsButton", "Order.CanOpenDetails"),
        };

        CollectionAssert.AreEqual(
            expected.Select(entry => entry.Id).ToArray(),
            action.Descendants(s_presentation + "Button")
                .Select(button => (string?)button.Attribute("AutomationProperties.AutomationId"))
                .ToArray(),
            "The action buttons are not declared in the row-major order the panel lays them out in (FR-061, D44).");

        foreach (var (id, gate) in expected)
        {
            if (gate is null)
            {
                continue;
            }

            StringAssert.Contains(
                (string?)Button(card, id).Attribute("Visibility") ?? string.Empty,
                gate,
                $"{id} is drawn regardless of whether the row offers the action (FR-062).");
        }
    }

    [TestMethod]
    public void UI_WaitlistLineCardMarkupTests_Card_RemovesAnUnavailableActionAndRecentresTheRestWithoutStretching()
    {
        var card = LoadCard();
        var action = Named(card, "ActionZone");

        // The button zone re-centres rather than stretching to fill the width it is given (FR-063, D30).
        Assert.AreEqual("Center", (string?)action.Attribute("HorizontalAlignment"), "The action grid stretches instead of re-centring (FR-063).");
        Assert.AreEqual("Center", (string?)action.Attribute("VerticalAlignment"), "The action grid stretches instead of re-centring (FR-063).");

        foreach (var id in new[]
        {
            "WaitlistLineCardView_AcceptButton",
            "WaitlistLineCardView_CancelButton",
            "WaitlistLineCardView_ReleaseButton",
            "WaitlistLineCardView_CompleteButton",
            "WaitlistLineCardView_OpenDetailsButton",
        })
        {
            var button = Button(card, id);

            Assert.IsNull(
                button.Attribute("Grid.Row"),
                $"{id} is pinned to a row of its own, so an unavailable action leaves a hole (FR-063).");
            Assert.IsNull(
                button.Attribute("Grid.Column"),
                $"{id} is pinned to a column of its own, so an unavailable action leaves a hole (FR-063).");
            Assert.AreEqual("44", (string?)button.Attribute("MinWidth"), $"{id} is smaller than the 44x44 hit target (FR-065).");
            Assert.AreEqual("44", (string?)button.Attribute("MinHeight"), $"{id} is smaller than the 44x44 hit target (FR-065).");
            Assert.AreEqual("44", (string?)button.Attribute("Width"), $"{id} is not a square button, so it would stretch into the column the zone gives it (FR-061, FR-063).");
            Assert.AreEqual("44", (string?)button.Attribute("Height"), $"{id} is not a square button, so it would stretch into the column the zone gives it (FR-061, FR-063).");
            Assert.AreNotEqual("Stretch", (string?)button.Attribute("HorizontalAlignment"), $"{id} stretches when its neighbours are unavailable (FR-063).");
        }

        // Every gated action is removed, not blanked: the gate is the element's visibility, never an
        // enabled state (FR-062, FR-063).
        foreach (var (id, gate) in new[]
        {
            ("WaitlistLineCardView_AcceptButton", "Order.CanAccept"),
            ("WaitlistLineCardView_CancelButton", "Order.CanCancelRequest"),
            ("WaitlistLineCardView_ReleaseButton", "Order.CanCompleteOrRelease"),
            ("WaitlistLineCardView_CompleteButton", "Order.CanCompleteOrRelease"),
            ("WaitlistLineCardView_OpenDetailsButton", "Order.CanOpenDetails"),
        })
        {
            var button = Button(card, id);

            StringAssert.Contains((string?)button.Attribute("Visibility") ?? string.Empty, gate, $"{id} is blanked rather than removed (FR-063).");
            Assert.IsNull(
                button.Attribute("IsEnabled"),
                $"{id}'s availability is decided by whether the button is enabled, and the card's rule is removal (FR-063).");
        }

        // The gate is the row's own, so no action is offered because the layout changed (FR-062).
        Assert.IsFalse(
            card.ToString().Contains("OfferedActionCount", StringComparison.Ordinal),
            "The card decides an action's availability from a count rather than from the row's own gate (FR-062).");
    }

    [TestMethod]
    public void UI_WaitlistLineCardMarkupTests_Card_EveryActionButtonIsNamedAndTippedFromLocalizedText()
    {
        var card = LoadCard();

        foreach (var (id, text) in new[]
        {
            ("WaitlistLineCardView_AcceptButton", "Order.AcceptActionText"),
            ("WaitlistLineCardView_CancelButton", "Order.CancelActionText"),
            ("WaitlistLineCardView_ReleaseButton", "Order.ReleaseActionText"),
            ("WaitlistLineCardView_CompleteButton", "Order.CompleteActionText"),
            ("WaitlistLineCardView_OpenDetailsButton", "OpenDetailsText"),
        })
        {
            var button = Button(card, id);

            StringAssert.Contains((string?)button.Attribute("AutomationProperties.Name") ?? string.Empty, text, $"{id} carries no accessible name from localized text (FR-064).");
            StringAssert.Contains(TipOf(button), text, $"{id} carries no tooltip, so its icon has to speak for itself (FR-064).");
        }
    }

    [TestMethod]
    public void UI_WaitlistLineCardMarkupTests_Card_AcceptCompleteAndGiveBackAreUnmistakablyDifferent()
    {
        var card = LoadCard();

        var accept = Button(card, "WaitlistLineCardView_AcceptButton");
        var complete = Button(card, "WaitlistLineCardView_CompleteButton");
        var release = Button(card, "WaitlistLineCardView_ReleaseButton");

        var backgrounds = new[]
        {
            (string?)accept.Attribute("Background"),
            (string?)complete.Attribute("Background"),
            (string?)release.Attribute("Background"),
        };

        Assert.IsTrue(
            backgrounds.Distinct(StringComparer.Ordinal).Count() == 3,
            "Accept, Complete and Give back do not keep three distinct colours (FR-066).");
        StringAssert.Contains(backgrounds[0]!, "Success", "Accept keeps the positive green.");
        StringAssert.Contains(backgrounds[1]!, "Accent", "Complete keeps the accent colour.");
        StringAssert.Contains(backgrounds[2]!, "Neutral", "Give back keeps the quieter solid neutral.");

        var glyphs = new[]
        {
            GlyphOf(accept),
            GlyphOf(complete),
            GlyphOf(release),
        };

        Assert.IsTrue(glyphs.Distinct(StringComparer.Ordinal).Count() == 3, "Accept, Complete and Give back do not keep three distinct glyphs (FR-066).");
        Assert.AreEqual("\uE8FB", glyphs[0], "Accept uses the Accept glyph (E8FB) from the Segoe Fluent Icons list.");
        Assert.AreEqual("\uE930", glyphs[1], "Complete uses the Completed glyph (E930) from the Segoe Fluent Icons list.");
        Assert.AreEqual("\uE7A7", glyphs[2], "Give back uses the Undo glyph (E7A7) from the Segoe Fluent Icons list.");
    }

    /// <summary>
    /// The card carries no print action at all.
    /// </summary>
    /// <remarks>
    /// A print control was drawn here as a disabled placeholder that said it was not available yet, and it was
    /// removed on the owner's direction (2026-10-11, pre-spec D59): printing a label belongs to the request's own
    /// page, where the detail a label needs is. The placeholder earned its removal twice over — it performed
    /// nothing, and the platform's disabled state faded it until its glyph was invisible, so it read as an empty
    /// box on every card rather than as something coming. This check exists so it cannot come back by habit: the
    /// card offers only actions that do something, and nothing on it records a TODO about one that does not.
    /// </remarks>
    [TestMethod]
    public void UI_WaitlistLineCardMarkupTests_Card_CarriesNoPrintActionAtAll()
    {
        var card = LoadCard();

        Assert.IsNull(
            card.Descendants(s_presentation + "Button").FirstOrDefault(button =>
                ((string?)button.Attribute("AutomationProperties.AutomationId") ?? string.Empty).Contains("Print", StringComparison.Ordinal)),
            "The card draws a print control again, which does nothing and reads as an empty box (FR-067).");

        // Nor the glyph it drew: a disabled button that performs no action is not something to keep as a
        // placeholder. This is Print's glyph (E749) from the Segoe Fluent Icons list.
        Assert.IsFalse(
            card.ToString().Contains("\uE749", StringComparison.Ordinal),
            "The card still draws the print glyph somewhere, so a print control is only partly removed (FR-067).");

        var codeBehind = LoadCardCodeBehind();

        Assert.IsFalse(
            codeBehind.Contains("PrintActionText", StringComparison.Ordinal)
                || codeBehind.Contains("PrintUnavailableText", StringComparison.Ordinal),
            "The card still carries a print control's own labels (FR-067).");
        Assert.IsFalse(
            codeBehind.Contains("label printing", StringComparison.Ordinal),
            "The card still carries the TODO naming label printing as an action it will take (FR-067, D59).");
    }

    [TestMethod]
    public void UI_WaitlistLineCardMarkupTests_Card_OpenDetailsCarriesTheMagnifierAndIsTheOnlyWayTheRequestOpens()
    {
        var card = LoadCard();
        var openDetails = Button(card, "WaitlistLineCardView_OpenDetailsButton");

        Assert.AreEqual("\uE721", GlyphOf(openDetails), "Open details must carry a magnifier glyph (FR-068).");
        StringAssert.Contains(
            TipOf(openDetails),
            "OpenDetailsText",
            "Open details must carry the `Open details` tooltip, from a resource rather than a literal (FR-068, FR-104).");
        Assert.IsNotNull(
            openDetails.Attribute("Click"),
            "Open details carries no handler, so the only way to open the request from the card is gone (FR-068).");

        var page = LoadListPage();

        Assert.IsFalse(
            page.ToString().Contains("ItemClick", StringComparison.Ordinal),
            "The list still opens the request on the row's own click; the card body must no longer do that (FR-069, FR-100).");
    }

    // ── The one card shape (FR-006, FR-103) ───────────────────────────────────────────────────────────

    [TestMethod]
    public void UI_WaitlistLineCardMarkupTests_Card_RendersOneLayoutWithNoSpanChosenByTheItem()
    {
        var card = LoadCard();

        // FR-006 admits no Item-selected variant, so no layout-critical attribute may be bound to the row's
        // identity. A span, a size or a visibility that keyed on the Item would be precisely that variant.
        foreach (var element in card.Descendants())
        {
            foreach (var name in new[] { "Grid.ColumnSpan", "Grid.RowSpan", "Width", "Height", "Visibility" })
            {
                var value = (string?)element.Attribute(name);

                Assert.IsFalse(
                    value?.Contains("ItemCode", StringComparison.Ordinal) == true
                        || value?.Contains("Order.Title", StringComparison.Ordinal) == true
                        || value?.Contains("Order.Subtitle", StringComparison.Ordinal) == true,
                    $"{name}=\"{value}\" selects part of the layout from the Item, which FR-006 forbids: {element.Name}");
            }
        }
    }

    // ── The detail template selector (FR-025, FR-103) ─────────────────────────────────────────────────

    [TestMethod]
    public void UI_WaitlistLineCardMarkupTests_Selector_ResolvesEachItemsOwnTemplateKey()
    {
        var source = LoadSelectorSource();
        var prefix = ReadConstant(source, "TemplateKeyPrefix");
        var fallbackKey = ReadConstant(source, "FallbackTemplateKey");

        Assert.AreEqual(
            "WaitlistLineCardDetail_",
            prefix,
            "The template key prefix moved, and every per-Item key moves with it.");
        Assert.AreEqual(
            "WaitlistLineCardDetailFallback",
            fallbackKey,
            "The fallback key moved, and the card declares its fallback template under the name the selector reads.");

        var keys = RequestItemCatalog.Items
            .Select(item => prefix + item.Id)
            .ToArray();

        Assert.AreEqual(
            RequestItemCatalog.TotalCount,
            keys.Distinct(StringComparer.Ordinal).Count(),
            "Two Items resolve to one template key, so their cells would not be their own (FR-025).");
        Assert.IsFalse(
            keys.Contains(fallbackKey, StringComparer.Ordinal),
            "The fallback key is also one Item's key, so an Item's own template and the fallback are the same template.");
    }

    [TestMethod]
    public void UI_WaitlistLineCardMarkupTests_Selector_FallsBackToTheCardsOwnFallbackTemplate()
    {
        var source = LoadSelectorSource();
        var fallbackKey = ReadConstant(source, "FallbackTemplateKey");
        var card = LoadCard();

        StringAssert.Contains(
            source,
            "ItemCode",
            "The selector does not resolve the row's own Item, so it cannot pick an Item's template (FR-025).");
        StringAssert.Contains(
            source,
            "FallbackTemplate",
            "The selector holds no fallback, so an Item with no template of its own draws nothing at all (FR-103).");

        var fallback = card
            .Descendants()
            .FirstOrDefault(element => (string?)element.Attribute(s_xaml + "Key") == fallbackKey);

        Assert.IsNotNull(
            fallback,
            $"The card declares no template under '{fallbackKey}', so the selector's fallback path is not the one it draws with.");
        Assert.AreEqual(
            "DataTemplate",
            fallback!.Name.LocalName,
            "The card's fallback is not a template, so the selector cannot return it.");

        // An Item whose template is absent draws no cells at all: no substituted value and no reason (FR-060).
        //
        // "Draws nothing" is a statement about what is VISIBLE, not about the template being empty. A contentless
        // `DataTemplate` does not draw nothing: the cell area is a `ContentControl` holding the row, and a
        // presenter with no usable template falls back to rendering its content - so a contentless fallback
        // printed the row's type name on the card where the cells belong. The fallback therefore has a root, and
        // the root draws nothing.
        Assert.IsTrue(
            fallback.Elements().Any(),
            "The fallback template is contentless. A presenter given no usable template then renders its content, so the row's own type name is drawn where the cells belong (FR-060).");
        Assert.AreEqual(
            0,
            fallback.Descendants(s_presentation + "TextBlock").Count() + fallback.Descendants(s_presentation + "ContentControl").Count(),
            "The fallback template draws text or a cell presenter, and an Item with no cells draws neither (FR-026, FR-060).");
    }

    // ── The list page (FR-010, FR-069, FR-100) ────────────────────────────────────────────────────────

    [TestMethod]
    public void UI_WaitlistLineCardMarkupTests_ListPage_DrawsOneCardPerFullWidthRowAndNoBodyClick()
    {
        var page = LoadListPage();

        var list = page
            .Descendants(s_presentation + "ListView")
            .FirstOrDefault(candidate => (string?)candidate.Attribute(s_xaml + "Name") == "WaitlistListView");

        Assert.IsNotNull(list, "The list page no longer draws the waitlist list.");
        Assert.IsNull(
            list!.Attribute("IsItemClickEnabled"),
            "The list still answers a row click, so the card body still opens the request (FR-069, FR-100).");
        Assert.IsNull(
            list.Attribute("ItemClick"),
            "The list still raises ItemClick, so the row still opens the request (FR-069, FR-100).");

        var container = list
            .Descendants(s_presentation + "Style")
            .SelectMany(style => style.Descendants(s_presentation + "Setter"))
            .FirstOrDefault(setter => (string?)setter.Attribute("Property") == "HorizontalContentAlignment");

        Assert.IsNotNull(container, "The row's container no longer states how its content is aligned (FR-010).");
        Assert.AreEqual("Stretch", (string?)container!.Attribute("Value"), "One card per row does not stretch the full width (FR-010).");
    }

    [TestMethod]
    public void UI_WaitlistLineCardMarkupTests_ListPage_BindsOneCardTemplateAndNoSelector()
    {
        var page = LoadListPage();

        Assert.IsFalse(
            page.Descendants().Any(element => ((string?)element.Attribute("ItemTemplateSelector"))?.Contains("WaitlistLineTemplateSelector", StringComparison.Ordinal) == true),
            "The list still selects a card layout by Item (FR-006).");
        Assert.IsFalse(
            page.ToString().Contains("WaitlistLineTemplateSelector", StringComparison.Ordinal),
            "The per-type card templates are still declared on the list page (FR-006).");

        var declaredKeys = page
            .Descendants()
            .Select(element => element.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == "Key")?.Value)
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .ToList();

        Assert.IsTrue(
            declaredKeys.Contains("WaitlistLineCardTemplate", StringComparer.Ordinal),
            "The list page no longer declares the one card template every row binds.");
    }

    [TestMethod]
    public void UI_WaitlistLineCardMarkupTests_PerTypeDetailGrids_AndTheirPerTypeLineViewsAreGone()
    {
        var card = LoadCard();

        var detailHosts = card
            .Descendants()
            .Where(element => ((string?)element.Attribute("Content"))?.Contains("DetailsContent", StringComparison.Ordinal) == true)
            .ToList();

        Assert.AreEqual(
            0,
            detailHosts.Count,
            $"The card still hosts a per-type detail grid, which the per-Item template selector replaced: {detailHosts.FirstOrDefault()}");

        var root = RepositoryPatternScan.FindRepositoryRoot();

        string[] retired =
        [
            Path.Combine("Coil", "CoilWaitlistLineView.xaml"),
            Path.Combine("PickupFg", "PickupFgWaitlistLineView.xaml"),
            Path.Combine("PickupNcm", "PickupNcmWaitlistLineView.xaml"),
            Path.Combine("PickupOs", "PickupOsWaitlistLineView.xaml"),
            Path.Combine("PickupWip", "PickupWipWaitlistLineView.xaml"),
            Path.Combine("Scrap", "ScrapWaitlistLineView.xaml"),
        ];

        foreach (var relative in retired)
        {
            var path = Path.Combine(root, "Module_Waitlist", "Controls", relative);

            Assert.IsFalse(
                File.Exists(path),
                $"{relative} still exists; every Item renders the one card shape now (FR-006).");
        }
    }

    // ── Every label the card adds is a resource (FR-104) ──────────────────────────────────────────────

    [TestMethod]
    public void UI_WaitlistLineCardMarkupTests_Card_ResolvesEveryNewStringThroughTheResourceFile()
    {
        var root = RepositoryPatternScan.FindRepositoryRoot();
        var resourcePath = Path.Combine(root, "Strings", "en-us", "Resources.resw");

        Assert.IsTrue(File.Exists(resourcePath), $"The resource file was not found at '{resourcePath}'.");

        var values = XDocument
            .Load(resourcePath)
            .Descendants("data")
            .Where(data => data.Attribute("name") is not null)
            .ToDictionary(
                data => (string)data.Attribute("name")!,
                data => (string?)data.Element("value") ?? string.Empty,
                StringComparer.Ordinal);

        var codeBehind = LoadCardCodeBehind();

        string[] keys =
        [
            "Waitlist_Card_OpenDetails.Text",
            "Waitlist_Card_Ring.TimeLeft",
            "Waitlist_Card_Ring.TimeTaken",
            "Waitlist_Card_StatusWord.SetCondition",
        ];

        foreach (var key in keys)
        {
            Assert.IsTrue(values.ContainsKey(key), $"The resource key '{key}' is missing from Resources.resw.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(values[key]), $"The resource key '{key}' has no copy.");
            Assert.IsTrue(
                codeBehind.Contains(key, StringComparison.Ordinal),
                $"The resource key '{key}' is never referenced, so nothing shows it.");
        }
    }

    // ── Fixtures ─────────────────────────────────────────────────────────────────────────────────────

    private static string CardDirectory() => Path.Combine(
        RepositoryPatternScan.FindRepositoryRoot(),
        "Module_Waitlist",
        "Controls");

    private static XDocument LoadCard() => XDocument.Load(Path.Combine(CardDirectory(), "WaitlistLineCardView.xaml"));

    private static string LoadCardCodeBehind() => File.ReadAllText(Path.Combine(CardDirectory(), "WaitlistLineCardView.xaml.cs"));

    private static string LoadSelectorSource() => File.ReadAllText(Path.Combine(CardDirectory(), "WaitlistLineCardDetailTemplateSelector.cs"));

    private static XDocument LoadListPage() => XDocument.Load(Path.Combine(
        RepositoryPatternScan.FindRepositoryRoot(),
        "Module_Waitlist",
        "Views",
        "WaitlistViewPage.xaml"));

    private static XElement Named(XContainer root, string name) =>
        root
            .Descendants()
            .FirstOrDefault(element => (string?)element.Attribute(s_xaml + "Name") == name)
        ?? throw new AssertFailedException($"The card draws no element named '{name}'.");

    private static XElement Button(XContainer root, string automationId) =>
        root
            .Descendants(s_presentation + "Button")
            .SingleOrDefault(element => (string?)element.Attribute("AutomationProperties.AutomationId") == automationId)
        ?? throw new AssertFailedException($"The card draws no button with the automation id '{automationId}'.");

    private static int Column(XElement element) =>
        int.Parse((string?)element.Attribute("Grid.Column") ?? "0", System.Globalization.CultureInfo.InvariantCulture);

    private static string GlyphOf(XElement button) =>
        button
            .Descendants(s_presentation + "FontIcon")
            .Single()
            .Attribute("Glyph")!
            .Value;

    /// <summary>
    /// The tooltip a control declares, whichever half of it the control carries.
    /// </summary>
    /// <remarks>
    /// Every tooltip in the application is declared through <c>TooltipBehavior</c> rather than
    /// <c>ToolTipService.ToolTip</c>, so that the same declaration feeds the localized text, the English fallback
    /// and the developer control inspector. Those attributes carry the shared namespace, so they cannot be read by
    /// qualified name the way a framework attribute is: the prefix is <c>shared</c> and everything after the colon
    /// is the attribute's own name.
    /// </remarks>
    private static string TipOf(XElement element) =>
        element
            .Attributes()
            .Where(attribute => attribute.Name.LocalName.StartsWith("TooltipBehavior.", StringComparison.Ordinal))
            .Select(attribute => attribute.Value)
            .FirstOrDefault() ?? string.Empty;

    /// <summary>Reads a <c>public const string</c> the selector declares, so the test reads its rule, not a copy of it.</summary>
    private static string ReadConstant(string source, string name)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            source,
            $@"\b{name}\s*=\s*""(?<value>[^""]+)""",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        Assert.IsTrue(match.Success, $"The selector declares no '{name}' constant.");

        return match.Groups["value"].Value;
    }
}
