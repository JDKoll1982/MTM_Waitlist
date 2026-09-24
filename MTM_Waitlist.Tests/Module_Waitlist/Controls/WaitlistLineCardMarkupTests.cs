using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Waitlist.Controls;

/// <summary>
/// US1 check 3 (<c>contracts/verification-gates.md</c> G4 #2, contract C1). The waitlist card must offer no
/// control it cannot act on: a drawn button with no command is a promise the application does not keep.
/// </summary>
/// <remarks>
/// The check reads the markup rather than the visual tree, so it runs in the suite instead of needing a
/// signed-in shell. It is deliberately behavioural about the rule ("no Button without a Command") and
/// specific only about the two controls the defect names.
/// </remarks>
[TestClass]
public sealed class WaitlistLineCardMarkupTests
{
    private static readonly XNamespace s_presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    [TestMethod]
    public void Card_DrawsNoButtonThatCannotAct()
    {
        var card = LoadCard();
        var buttons = card.Descendants(s_presentation + "Button").ToList();

        foreach (var button in buttons)
        {
            Assert.IsNotNull(
                button.Attribute("Command"),
                $"The card draws a Button with no Command, so it cannot act: {button}");
        }
    }

    [TestMethod]
    public void Card_ActionButtonsAreGatedByTheRowsOwnFlags()
    {
        // The card used to draw a Cancel and an Accept button with no command, and the honest check then was
        // that neither existed. They are wired now, so the honest check is the stronger one: every button is
        // drawn only while a per-row gate says the action is available. Nothing here reaches the service —
        // the service re-checks every gate, so a stale screen can never widen what a viewer may do (FR-018).
        var card = LoadCard();
        var buttons = card.Descendants(s_presentation + "Button").ToList();

        Assert.IsTrue(buttons.Count > 0, "The card draws no action button at all, so this check proved nothing.");

        foreach (var button in buttons)
        {
            var visibility = button.Attribute("Visibility")?.Value;

            Assert.IsNotNull(
                visibility,
                $"Every action button must be hidden unless its gate is true: {button}");
            Assert.IsTrue(
                visibility!.Contains("CanAccept", StringComparison.Ordinal)
                    || visibility.Contains("CanCompleteOrRelease", StringComparison.Ordinal)
                    || visibility.Contains("CanCancelRequest", StringComparison.Ordinal),
                $"The button's visibility is not bound to a per-row action gate: {button}");
        }
    }

    [TestMethod]
    public void Card_ActionButtonsAreNamedFromTheRowNotFromALiteral()
    {
        // The accessible name has to be localized, so it comes from the row's own action text. A literal
        // name in markup is both an unlocalized string and a claim about what the button does.
        var card = LoadCard();

        foreach (var button in card.Descendants(s_presentation + "Button"))
        {
            var name = button.Attribute("AutomationProperties.Name")?.Value;

            Assert.IsNotNull(name, $"An action button carries no accessible name: {button}");
            Assert.IsTrue(
                name!.Contains("ActionText", StringComparison.Ordinal),
                $"The accessible name is a literal rather than the row's localized action text: {name}");
        }
    }

    [TestMethod]
    public void Card_ActionButtonsCarryATooltipFromTheRowNotFromALiteral()
    {
        // Accept and Complete both draw a green tick, so an icon alone does not say which action a button
        // performs. The tooltip is what makes the pair legible, and it comes from the row's localized action
        // text for the same reason the accessible name does — a literal here would be unlocalized.
        var card = LoadCard();

        foreach (var button in card.Descendants(s_presentation + "Button"))
        {
            var tooltip = button.Attribute("ToolTipService.ToolTip")?.Value;

            Assert.IsNotNull(tooltip, $"An action button carries no tooltip, so its icon has to speak for itself: {button}");
            Assert.IsTrue(
                tooltip!.Contains("ActionText", StringComparison.Ordinal),
                $"The tooltip is a literal rather than the row's localized action text: {tooltip}");
        }
    }

    [TestMethod]
    public void Card_AcceptAndCompleteAreUnmistakablyDifferent()
    {
        // They occupy the same slot in two states, so a viewer switching from one to the other has to be able to
        // see that the button changed. Both the colour and the glyph must differ, and E10B is avoided because the
        // Segoe Fluent Icons list marks the whole E0-E5 range deprecated.
        var card = LoadCard();
        var actionButtons = card
            .Descendants(s_presentation + "Button")
            .Where(button => button.Attribute("AutomationProperties.Name")?.Value.Contains("ActionText", StringComparison.Ordinal) == true)
            .ToList();

        var accept = actionButtons.Single(button => button.Attribute("AutomationProperties.Name")!.Value.Contains("AcceptActionText", StringComparison.Ordinal));
        var complete = actionButtons.Single(button => button.Attribute("AutomationProperties.Name")!.Value.Contains("CompleteActionText", StringComparison.Ordinal));

        var acceptBackground = accept.Attribute("Background")!.Value;
        var completeBackground = complete.Attribute("Background")!.Value;

        Assert.AreNotEqual(
            acceptBackground,
            completeBackground,
            "Accept and Complete must not share a background colour.");
        Assert.IsTrue(acceptBackground.Contains("Success", StringComparison.Ordinal), "Accept keeps the positive green.");
        Assert.IsTrue(completeBackground.Contains("Accent", StringComparison.Ordinal), "Complete uses the accent colour.");

        // The XAML parser decodes the &#xNNNN; entity, so the attribute value arrives as the character itself.
        var glyphOf = (System.Xml.Linq.XElement button) => button
            .Descendants(s_presentation + "FontIcon")
            .Single()
            .Attribute("Glyph")!
            .Value;

        var acceptGlyph = glyphOf(accept);
        var completeGlyph = glyphOf(complete);

        Assert.AreNotEqual(acceptGlyph, completeGlyph, "Accept and Complete must not share an icon.");
        Assert.AreEqual(
            "\uE8FB",
            acceptGlyph,
            "Accept uses the Accept glyph (E8FB) from the Segoe Fluent Icons list.");
        Assert.AreEqual(
            "\uE930",
            completeGlyph,
            "Complete uses the Completed glyph (E930) from the Segoe Fluent Icons list.");
    }

    [TestMethod]
    public void Card_DrawsGiveBackFromTheRowsOwnCommand()
    {
        // FR-047, contract C7 (superseded): a claimed request can be given back from its own card. Give back
        // is a card control like the others — command, gate, accessible name and tooltip all from the row —
        // and it is distinguishable from Complete, because the two sit side by side on the same row.
        var card = LoadCard();
        var release = card
            .Descendants(s_presentation + "Button")
            .SingleOrDefault(button => button.Attribute("Command")?.Value.Contains("ReleaseCommand", StringComparison.Ordinal) == true);

        Assert.IsNotNull(
            release,
            "The card draws no Give back button, so a claimed request cannot be handed back from its card (FR-047).");

        Assert.IsTrue(
            release!.Attribute("Visibility")!.Value.Contains("CanCompleteOrRelease", StringComparison.Ordinal),
            "Give back must be hidden unless the row's own gate says the viewer may hand this request back.");
        Assert.IsTrue(
            release.Attribute("AutomationProperties.Name")!.Value.Contains("ReleaseActionText", StringComparison.Ordinal),
            "Give back must be named from the row's localized action text, not from a literal.");
        Assert.AreEqual("44", (string?)release.Attribute("MinWidth"), "Give back must match the other icon buttons.");
        Assert.AreEqual("44", (string?)release.Attribute("MinHeight"), "Give back must match the other icon buttons.");

        var complete = card
            .Descendants(s_presentation + "Button")
            .Single(button => button.Attribute("AutomationProperties.Name")!.Value.Contains("CompleteActionText", StringComparison.Ordinal));

        Assert.AreNotEqual(
            complete.Attribute("Background")!.Value,
            release.Attribute("Background")!.Value,
            "Complete and Give back must not share a background colour.");

        var glyphOf = (System.Xml.Linq.XElement button) => button
            .Descendants(s_presentation + "FontIcon")
            .Single()
            .Attribute("Glyph")!
            .Value;

        Assert.AreNotEqual(
            glyphOf(complete),
            glyphOf(release),
            "Complete and Give back must not share an icon.");
        Assert.AreEqual(
            "\uE7A7",
            glyphOf(release),
            "Give back uses the Undo glyph (E7A7) from the Segoe Fluent Icons list.");
    }

    [TestMethod]
    public void Card_ShowsTheRequestsRealLifecycleStatusInTheActionArea()
    {
        var card = LoadCard();

        Assert.IsTrue(
            card.Descendants().Any(element => element.Attribute("Text")?.Value.Contains("StatusBadgeText", StringComparison.Ordinal) == true),
            "The card's action area must show the request's real lifecycle status now that the inert buttons are gone.");

        Assert.IsTrue(
            card.Descendants().Any(element => element.Attribute("Visibility")?.Value.Contains("HasStatusBadge", StringComparison.Ordinal) == true),
            "The status must be hidden when the request carries no lifecycle status, rather than rendering an empty pill.");
    }

    private static XDocument LoadCard()
    {
        var path = Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Module_Waitlist",
            "Controls",
            "WaitlistLineCardView.xaml");

        Assert.IsTrue(File.Exists(path), $"The card markup was not found at '{path}'.");

        return XDocument.Load(path);
    }

    // ── The approved anatomy (FR-021). Every dimension below was verified and approved on 2026-09-06 and is
    //    marked "DO NOT REGRESS"; these checks exist so a later edit has to be deliberate rather than quiet.

    [TestMethod]
    public void Card_KeepsTheFixedSquareRequestImage()
    {
        var host = LoadCard()
            .Descendants(s_presentation + "Border")
            .FirstOrDefault(border => (string?)border.Attribute("Width") == "96");

        Assert.IsNotNull(host, "The card's fixed 96-wide request image host is gone.");
        Assert.AreEqual("96", (string?)host!.Attribute("Height"), "The request image host must stay a fixed 96x96 square, not a stretched column.");
        Assert.IsTrue(
            host.Descendants().Any(element => element.Name.LocalName == "ClickToEnlargeImageView"),
            "The 96x96 host must still hold the request picture, drawn by the shared picture control (FR-019, 009 A3).");
    }

    [TestMethod]
    public void Card_KeepsTheTitleOnItsOwnTopRow()
    {
        var title = LoadCard()
            .Descendants(s_presentation + "TextBlock")
            .FirstOrDefault(text => ((string?)text.Attribute("Text"))?.Contains("Order.Title", StringComparison.Ordinal) == true);

        Assert.IsNotNull(title, "The card no longer renders the request title.");
        Assert.AreEqual("0", (string?)title!.Attribute("Grid.Row"), "The title must keep its own top row.");
    }

    [TestMethod]
    public void Card_KeepsTheFourMetadataRows()
    {
        var labels = LoadCard()
            .Descendants(s_presentation + "TextBlock")
            .Select(text => (string?)text.Attribute("Text"))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var label in new[] { "Requested by", "Press", "Remaining time", "Waiting" })
        {
            Assert.IsTrue(labels.Contains(label), $"The card's '{label}' metadata row is gone.");
        }
    }

    [TestMethod]
    public void Card_KeepsTheCompactStatusPillBelowTheButtons()
    {
        var pill = LoadCard()
            .Descendants(s_presentation + "Border")
            .FirstOrDefault(border => (string?)border.Attribute("Height") == "36");

        Assert.IsNotNull(pill, "The compact 36-high status pill is gone.");
        Assert.AreEqual(
            "1",
            (string?)pill!.Attribute("Grid.Row"),
            "The status pill must sit below the action buttons, not take over the action area.");
        Assert.IsTrue(
            pill.Descendants(s_presentation + "TextBlock").Any(text => ((string?)text.Attribute("Text"))?.Contains("StatusBadgeText", StringComparison.Ordinal) == true),
            "The 36-high element is no longer the status pill.");
    }

    // ── The one card shape (US2, FR-006/FR-007/FR-008). This is a recorded supersession of the
    //    `specs/003` "DO NOT REGRESS" per-type anatomy: the fifteen per-type controls and the selector that
    //    chose between them are deleted, and the freeze is lifted in writing rather than deleted, so the
    //    constraint still guards the *new* shape and an unintended card change still fails.

    [TestMethod]
    public void Card_DrawsLine1AndLine2()
    {
        var texts = LoadCard()
            .Descendants(s_presentation + "TextBlock")
            .Select(text => (string?)text.Attribute("Text"))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();

        Assert.IsTrue(
            texts.Any(value => value!.Contains("Order.Title", StringComparison.Ordinal)),
            "The card no longer draws Line 1 — the Item's umbrella phrase (FR-005).");
        Assert.IsTrue(
            texts.Any(value => value!.Contains("Order.Subtitle", StringComparison.Ordinal)),
            "The card no longer draws Line 2 — the Item's identifier (FR-005).");
    }

    [TestMethod]
    public void Card_RendersOneLayoutWithNoSpanChosenByTheItem()
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

    /// <summary>
    /// The card draws the <b>material part's</b> picture, and the one shared placeholder when that material
    /// cannot be pictured. What it must never draw is the picture for the kind of request, or the part's family's
    /// artwork standing in for a part that has none (FR-019, FR-020).
    /// </summary>
    /// <remarks>
    /// The row itself is what decides: <c>EffectiveImagePath</c> answers the material part's resolved picture and
    /// nothing else, and the row no longer carries any Item-derived picture at all, so a markup change back to
    /// one would not even compile. The markup assertion here is that the picture box still goes through the
    /// resolver that applies the application's one picture rule and the one placeholder.
    /// </remarks>
    [TestMethod]
    public void Card_CarriesTheMaterialPartsPictureAndItsPlaceholderFallback()
    {
        var image = LoadCard()
            .Descendants()
            .FirstOrDefault(candidate => candidate.Name.LocalName == "ClickToEnlargeImageView"
                && ((string?)candidate.Attribute("Source"))?.Contains("Order.EffectiveImagePath", StringComparison.Ordinal) == true);

        Assert.IsNotNull(image, "The card no longer draws the material part's picture (FR-019).");
        Assert.IsTrue(
            ((string?)image!.Attribute("Source"))!.Contains("ResolvedImagePathToSourceConverter", StringComparison.Ordinal),
            "The picture must go through the resolver that applies the application's one picture rule (FR-013).");
        Assert.IsFalse(
            ((string?)image.Attribute("Source"))!.Contains("ItemCode", StringComparison.Ordinal),
            "The card's picture must not resolve from the Item — that is the picture for the kind of request (FR-019).");
    }

    /// <summary>
    /// FR-007: an Item's own fields belong to the request's page and never appear on the card — so the
    /// per-type detail grids and their host presenter are gone, and the six per-type line views are deleted.
    /// </summary>
    [TestMethod]
    public void PerTypeDetailGrids_AndTheirPerTypeLineViews_AreGone()
    {
        var card = LoadCard();

        var detailHosts = card
            .Descendants()
            .Where(element => ((string?)element.Attribute("Content"))?.Contains("DetailsContent", StringComparison.Ordinal) == true)
            .ToList();

        Assert.AreEqual(
            0,
            detailHosts.Count,
            $"The card still hosts a per-type detail grid, which FR-007 moved to the request page: {detailHosts.FirstOrDefault()}");

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

    /// <summary>
    /// The single card template is what the list binds: the selector and the templates it chose between are
    /// gone, so there is nothing left to pick a layout with (FR-006).
    /// </summary>
    [TestMethod]
    public void ListPage_BindsOneCardTemplateAndNoSelector()
    {
        var page = XDocument.Load(Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Module_Waitlist",
            "Views",
            "WaitlistViewPage.xaml"));

        Assert.IsFalse(
            page.Descendants().Any(element => ((string?)element.Attribute("ItemTemplateSelector"))?.Contains("WaitlistLineTemplateSelector", StringComparison.Ordinal) == true),
            "The list still selects a card layout by Item (FR-006).");

        // `x:Key` lives in the XAML namespace, so it is matched by local name rather than by a prefixed name.
        var declaredKeys = page
            .Descendants()
            .Select(element => element.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == "Key")?.Value)
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .ToList();

        Assert.IsFalse(
            declaredKeys.Any(key => key!.Contains("WaitlistLineTemplate", StringComparison.Ordinal)),
            $"The per-type line templates are still declared on the list page: {string.Join(", ", declaredKeys)}");

        Assert.IsTrue(
            declaredKeys.Contains("WaitlistLineCardTemplate", StringComparer.Ordinal),
            "The list page no longer declares the one card template every row binds.");
    }
}