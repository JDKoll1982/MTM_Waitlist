using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Shared.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Helpers;

/// <summary>
/// The card's own rules for turning an Item's declared fields into the cells it draws — the half of the
/// assignment that belongs to the card rather than to configuration
/// (<c>specs/012-waitlist-card-layout/data-model.md</c> §9, <c>docs/research/research-waitlist-card-layout-pre-spec.md</c> §5).
/// </summary>
/// <remarks>
/// <para>
/// A declaration says which fields an Item's <i>page</i> has; the card draws a settled subset of them, and
/// the fields it does not draw are <b>left declared</b> — the exclusion is a card rule, not a quiet edit to
/// configuration, which is what FR-056 asks for and what keeps the two from drifting apart silently. Three
/// rules make up that subset: the work-centre labels are never cells because the work centre is the identity
/// zone's own value (FR-005, D19); four Items collapse their panel outright (FR-040, FR-048, FR-051, FR-052);
/// and the four returning pickups draw only the derived type and the skid count (FR-036 to FR-039), which
/// means every field they declare today is excluded until the seed and the answer arrive.
/// </para>
/// <para>
/// Two cells have no source today and are handled here rather than in configuration: <c>Destination</c> on
/// <c>deliver-flatstock</c> resolves from the request's own requesting work centre, because every Deliver
/// Item is delivered to the work centre that asked for it and nothing asks the requester where it goes
/// (FR-042, FR-057); and the die Items' die-shop cell is drawn label-less, as the phrase itself, only when
/// that checkbox's answer was given (FR-055, FR-081). <c>Lugger Location</c> on <c>pickup-scrap</c> is the third
/// such cell and is listed in <see cref="s_pendingSourceLabels"/>: its source is designed but not yet built, so
/// the cell is drawn with a plainly-worded placeholder instead of being hidden (FR-034). A future source deletes
/// that one entry, adds its resolver arm, and the real value appears with no card change at all (FR-058).
/// </para>
/// </remarks>
public static class WaitlistCardCellRules
{
    /// <summary>
    /// The label the two die Items declare their die-shop direction answer under. The card reads the answer
    /// under this label and draws the label-less phrase instead of the answer's own text (FR-055, FR-081).
    /// </summary>
    public const string DieShopAnswerLabel = "Die Shop Direction";

    /// <summary>The resource key of the phrase a pickup die's die-shop cell draws (FR-055, FR-104).</summary>
    public const string TakeToDieShopKey = "Waitlist_Card_DieShop.TakeToDieShop";

    /// <summary>The resource key of the phrase a deliver die's die-shop cell draws (FR-055, FR-104).</summary>
    public const string LocatedInDieShopKey = "Waitlist_Card_DieShop.LocatedInDieShop";

    /// <summary>
    /// The resource key of the question a pickup die's die-shop checkbox asks: the die has to be taken there.
    /// </summary>
    public const string TakeToDieShopPromptKey = "NewRequest_Die.DieShopPrompt";

    /// <summary>
    /// The resource key of the question a deliver die's die-shop checkbox asks: the die is already there. The
    /// two directions ask different questions because the one tick means "take it there" on a pickup and "it is
    /// there" on a deliver, and a single wording would make the same tick answer two different things (FR-081).
    /// </summary>
    public const string LocatedInDieShopPromptKey = "NewRequest_Die.DieShopPromptLocated";

    /// <summary>
    /// The resource key of the phrase drawn in the place of a cell whose source is designed but not yet built.
    /// </summary>
    /// <remarks>
    /// It is a message, never a data value: it says plainly that this cell is still being built, so nobody reads it
    /// as the scrap lugger's location. It is deliberately not a dash or a blank, because a blank cell is
    /// indistinguishable from a source that answered nothing.
    /// </remarks>
    public const string PendingSourceKey = "Waitlist_Card_PendingSource.InDevelopment";

    /// <summary>
    /// The labels whose declared field is never a cell, whatever the Item: the requesting work centre is the
    /// identity zone's own value and the requesting user belongs to the request's page (FR-005, FR-056, D19).
    /// They are the spellings the shipped declarations actually use, compared case-insensitively.
    /// </summary>
    private static readonly string[] s_workCentreLabels =
    [
        "requesting work center",
        "pickup work center",
        "work center",
        "requested work center",
    ];

    /// <summary>
    /// The Items whose panel collapses entirely, so no declared field of theirs is ever a cell
    /// (FR-040, FR-048, FR-051, FR-052).
    /// </summary>
    private static readonly string[] s_collapsingItems =
    [
        "pickup-riser-table",
        "deliver-riser-table",
        "assist-table-place",
        "assist-table-remove",
    ];

    /// <summary>
    /// The four previously-hidden pickups, whose panel draws the derived type and the skid count and nothing
    /// else — so every field they declare today is excluded (FR-036 to FR-039, FR-056).
    /// </summary>
    private static readonly string[] s_returningPickupItems =
    [
        "pickup-fg",
        "pickup-ncm",
        "pickup-wip",
        "pickup-outside-service",
    ];

    /// <summary>The two cells a returning pickup draws, as the labels the declarations will carry.</summary>
    private static readonly string[] s_returningPickupCells = ["part type", "# of skids"];

    /// <summary>
    /// The declared fields whose <b>source is designed but not yet built</b>, per Item, so their cell is drawn with
    /// a plainly-worded placeholder instead of being hidden.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>TODO (feature 012 follow-up, FR-034): implement the scrap lugger's location.</b> <c>Lugger Location</c> on
    /// <c>pickup-scrap</c> is declared by the seed with source <c>job</c>, and nothing yet reads it: the Scrap Item's
    /// location has to come from wherever the scrap lugger is standing, which no read shape answers today. Until
    /// that source exists the cell shows <see cref="PendingSourceKey"/> so the person can see the cell is coming
    /// rather than seeing nothing.
    /// </para>
    /// <para>
    /// <b>When the source lands, this list is the only thing that changes.</b> Delete the entry, add the resolver
    /// arm in <c>RequestJobFieldValues</c> (module <c>MTM_Waitlist.Settings</c>), and the cell starts drawing the
    /// real location with no card change at all — the template and the panel already carry the slot (FR-058).
    /// </para>
    /// </remarks>
    private static readonly Dictionary<string, string[]> s_pendingSourceLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pickup-scrap"] = ["lugger location"],
    };

    /// <summary>
    /// The declared fields the card does not draw, per Item, as each declaration spells them today. Every one
    /// is left declared in configuration (§9): <c>Description</c> on the dunnage Items is provably unfillable
    /// (FR-095), <c>Quantity</c> on the die Items and <c>Reason</c> on the wrong-part Items are not part of
    /// their cards (FR-056), and the coil's quantity in house is not one of the assist coil turn's two cells.
    /// </summary>
    private static readonly Dictionary<string, string[]> s_excludedLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pickup-die"] = ["quantity"],
        ["deliver-die"] = ["quantity"],
        ["pickup-component"] = ["part description", "quantity"],
        ["deliver-component"] = ["part description", "quantity"],
        ["pickup-dunnage"] = ["description", "quantity on hand"],
        ["deliver-dunnage"] = ["description", "quantity on hand"],
        ["pickup-scrap"] = ["quantity involved", "scrap lugger"],
        ["assist-coil-turn"] = ["quantity in house"],
        ["deliver-wrong-coil"] = ["reason"],
        ["deliver-wrong-flatstock"] = ["reason"],
    };

    /// <summary>The declared label whose value is the request's own requesting work centre (FR-042, FR-057).</summary>
    private const string RequestWorkCentreLabel = "destination";

    /// <summary>
    /// The declared label the two hopper Items answer their size under. The cell's value is the size word, and
    /// that same word is the key the size's reference picture is filed under (FR-077, FR-078, D22).
    /// </summary>
    private const string SizeAnswerLabel = "Size";

    /// <summary>Whether anything at all is drawn for an Item: the four collapsing Items draw no cells.</summary>
    /// <param name="itemCode">The request's Item code.</param>
    public static bool DrawsNoCells(string? itemCode) =>
        Matches(s_collapsingItems, itemCode);

    /// <summary>
    /// Whether one declared field of an Item is a cell the card draws, so every declared field the card does
    /// not show is excluded here rather than removed from configuration (FR-056).
    /// </summary>
    /// <param name="itemCode">The request's Item code.</param>
    /// <param name="label">The label the field was declared under.</param>
    /// <returns><see langword="false" /> for a field the card does not draw.</returns>
    public static bool IsCell(string? itemCode, string? label)
    {
        if (string.IsNullOrWhiteSpace(label) || DrawsNoCells(itemCode))
        {
            return false;
        }

        var declared = label.Trim();

        if (Matches(s_workCentreLabels, declared))
        {
            return false;
        }

        if (Matches(s_returningPickupItems, itemCode))
        {
            return Matches(s_returningPickupCells, declared);
        }

        return !(s_excludedLabels.TryGetValue(itemCode?.Trim() ?? string.Empty, out var excluded)
            && Matches(excluded, declared));
    }

    /// <summary>
    /// Whether a declared field is one the request's own requesting work centre answers, which is the
    /// destination of every Deliver Item and is asked of nobody (research R1, FR-042).
    /// </summary>
    /// <param name="label">The label the field was declared under.</param>
    public static bool IsRequestWorkCentreField(string? label) =>
        !string.IsNullOrWhiteSpace(label)
        && string.Equals(label.Trim(), RequestWorkCentreLabel, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Whether a declared field is the die Items' die-shop direction, whose answer decides whether the card
    /// draws its third, label-less cell (FR-055, FR-081).
    /// </summary>
    /// <param name="itemCode">The request's Item code.</param>
    /// <param name="label">The label the field was declared under.</param>
    public static bool IsDieShopField(string? itemCode, string? label) =>
        IsDieItem(itemCode)
        && !string.IsNullOrWhiteSpace(label)
        && string.Equals(label.Trim(), DieShopAnswerLabel, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The label-less phrase a die Item's die-shop cell draws, resolved from its own resource, or
    /// <see langword="null" /> for any other Item (FR-055, FR-104).
    /// </summary>
    /// <param name="itemCode">The request's Item code.</param>
    public static string? DieShopPhrase(string? itemCode)
    {
        var key = ItemCode(itemCode) switch
        {
            "pickup-die" => TakeToDieShopKey,
            "deliver-die" => LocatedInDieShopKey,
            _ => null,
        };

        if (key is null)
        {
            return null;
        }

        var phrase = key.GetLocalized();

        return string.IsNullOrWhiteSpace(phrase) ? key : phrase;
    }

    /// <summary>
    /// The question a die Item's die-shop checkbox asks, chosen by the Item's direction, or
    /// <see langword="null" /> for any other Item or when the wording cannot be resolved (FR-081).
    /// </summary>
    /// <remarks>
    /// It sits beside <see cref="DieShopPhrase"/> so the question and the phrase the card draws for the same
    /// answer are chosen from the same place: a pickup asks whether the die has to be taken to the die shop and
    /// its card then reads "Deliver to die shop"; a deliver asks whether the die is already there and its card
    /// reads the other phrase. Reading the direction once, here, is what stops the two drifting apart.
    /// </remarks>
    /// <param name="itemCode">The request's Item code.</param>
    public static string? DieShopPrompt(string? itemCode)
    {
        var key = ItemCode(itemCode) switch
        {
            "pickup-die" => TakeToDieShopPromptKey,
            "deliver-die" => LocatedInDieShopPromptKey,
            _ => null,
        };

        if (key is null)
        {
            return null;
        }

        var prompt = key.GetLocalized();

        // A missing wording is reported as no wording, so the step keeps its own default rather than showing a
        // resource key where a question belongs.
        return string.IsNullOrWhiteSpace(prompt) || string.Equals(prompt, key, StringComparison.Ordinal)
            ? null
            : prompt;
    }

    /// <summary>
    /// Whether a die-shop answer means the checkbox was ticked. A checkbox the requester never ticked is not a
    /// value the card draws: a blank answer is no answer, and the words that spell "not this" are not one
    /// either (FR-055, §10).
    /// </summary>
    /// <param name="answer">The answer the request captured under <see cref="DieShopAnswerLabel"/>, or null.</param>
    public static bool IsDieShopAnswerGiven(string? answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
        {
            return false;
        }

        return !Matches(["false", "no", "0", "unchecked", "unticked"], answer.Trim());
    }

    /// <summary>
    /// The value the request's own requesting work centre gives a declared field, or <see langword="null" />
    /// when the field is one the work centre does not answer.
    /// </summary>
    /// <param name="label">The label the field was declared under.</param>
    /// <param name="request">The request the row was built from.</param>
    public static string? RequestWorkCentreValue(string? label, WaitlistRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return IsRequestWorkCentreField(label) ? request.WorkCenter : null;
    }

    /// <summary>
    /// The part whose reference picture a cell opens, or <see langword="null" /> when the cell opens none
    /// (FR-078).
    /// </summary>
    /// <remarks>
    /// On the hopper Items the size word <b>is</b> the key its reference picture is filed under, and it is read
    /// from the Visual collection — the same key and the same scope the New Request hopper step reads, so the
    /// card and the wizard cannot ask for two different pictures for one size (D22, FR-077). A cell whose value
    /// resolved nothing opens nothing: there is no size to picture, and no placeholder is invented to stand in
    /// for the missing answer (FR-089).
    /// </remarks>
    /// <param name="itemCode">The request's Item code.</param>
    /// <param name="label">The label the field was declared under.</param>
    /// <param name="value">The value the cell resolved.</param>
    public static string? ReferencePicturePart(string? itemCode, string? label, string? value) =>
        IsHopperSizeField(itemCode, label) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;

    /// <summary>The picture collection a cell's reference picture is read from (FR-078).</summary>
    public static string ReferencePictureScope => PartPictureLayout.HopperSizeScope;

    /// <summary>
    /// Whether a declared field is the hopper Items' size answer, the one cell on a hopper card that pictures
    /// the answer it shows (FR-077, FR-078).
    /// </summary>
    /// <param name="itemCode">The request's Item code.</param>
    /// <param name="label">The label the field was declared under.</param>
    public static bool IsHopperSizeField(string? itemCode, string? label) =>
        IsHopperItem(itemCode)
        && !string.IsNullOrWhiteSpace(label)
        && string.Equals(label.Trim(), SizeAnswerLabel, StringComparison.OrdinalIgnoreCase);

    /// <summary>The die Items, the only two whose card can draw the label-less die-shop cell.</summary>
    /// <param name="itemCode">The request's Item code.</param>
    private static bool IsDieItem(string? itemCode) =>
        ItemCode(itemCode) is "pickup-die" or "deliver-die";

    /// <summary>
    /// Whether a declared field is one whose source is <b>designed but not yet built</b>, so its cell is drawn with
    /// the plainly-worded placeholder rather than being hidden or filled with a guess (FR-034).
    /// </summary>
    /// <param name="itemCode">The request's Item code.</param>
    /// <param name="label">The label the field was declared under.</param>
    /// <returns><see langword="true"/> for a cell that is waiting on its source.</returns>
    public static bool IsPendingSourceField(string? itemCode, string? label) =>
        !string.IsNullOrWhiteSpace(label)
        && s_pendingSourceLabels.TryGetValue(itemCode?.Trim() ?? string.Empty, out var labels)
        && Matches(labels, label);

    /// <summary>
    /// The placeholder a cell whose source is not yet built draws, resolved from its own resource, or
    /// <see langword="null"/> when the field is not one of those cells (FR-104).
    /// </summary>
    /// <param name="itemCode">The request's Item code.</param>
    /// <param name="label">The label the field was declared under.</param>
    public static string? PendingSourcePhrase(string? itemCode, string? label)
    {
        if (!IsPendingSourceField(itemCode, label))
        {
            return null;
        }

        var phrase = PendingSourceKey.GetLocalized();

        return string.IsNullOrWhiteSpace(phrase) ? PendingSourceKey : phrase;
    }

    /// <summary>
    /// The declared fields whose value is <b>known to be unfillable</b>, per Item, so their absence is a stated
    /// fact rather than a gap nobody has looked at.
    /// </summary>
    /// <remarks>
    /// <b>TODO (feature 012, for the clarify step — FR-095):</b> the dunnage Items declare a <c>Description</c> and
    /// nothing can answer it. There is no dunnage table in <c>mtm_receiving_application</c> that holds a
    /// description, so the field cannot be filled from any source that exists. It is left <b>declared</b> on
    /// purpose — deleting a declaration is a configuration change, and that is the clarify step's call, not this
    /// one's. Until then the field stays declared and its cell is simply never produced (it is listed in
    /// <see cref="s_excludedLabels"/>), which is why no warning is recorded for it.
    /// </remarks>
    public static bool IsKnownUnfillableField(string? itemCode, string? label) =>
        !string.IsNullOrWhiteSpace(label)
        && IsDunnageItem(itemCode)
        && Matches(["description"], label);

    /// <summary>The dunnage Items, the two that declare a field nothing can answer (FR-095).</summary>
    /// <param name="itemCode">The request's Item code.</param>
    private static bool IsDunnageItem(string? itemCode) =>
        ItemCode(itemCode) is "pickup-dunnage" or "deliver-dunnage";

    /// <summary>The hopper Items, the only two whose card pictures the size it shows.</summary>
    /// <param name="itemCode">The request's Item code.</param>
    private static bool IsHopperItem(string? itemCode) =>
        ItemCode(itemCode) is "pickup-hopper" or "deliver-hopper";

    private static string ItemCode(string? itemCode) => itemCode?.Trim().ToLowerInvariant() ?? string.Empty;

    private static bool Matches(IEnumerable<string> candidates, string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && candidates.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase);
}
