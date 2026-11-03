using System.Globalization;

namespace MTM_Waitlist.GenericRequest.Models;

/// <summary>A work center the request can belong to.</summary>
public sealed record GenericRequestWorkCenterOption(string Name, string Building);

/// <summary>A request category offered by a work center.</summary>
/// <param name="Code">The category's stable catalog code, which is what its items are grouped by.</param>
/// <param name="Name">The name a person reads.</param>
/// <param name="Description">One line saying what the category covers.</param>
public sealed record GenericRequestCategoryOption(string Code, string Name, string Description);

/// <summary>
/// An item a category offers. The four flags say which extra steps this item's request needs, so the flow can skip a
/// page that does not apply instead of showing it empty. Every flag is read from the item's own declaration in the
/// catalogue, never from a list in code.
/// </summary>
/// <param name="Code">The item's stable catalogue code, which is what the store is written from.</param>
/// <param name="Name">The name a person reads.</param>
/// <param name="PromptText">
/// The question the item's configuration asks, or empty when it asks nothing. The requester answers it on the item
/// step, which is where the work-order flow asks the same question (FR-007).
/// </param>
/// <param name="AnswerKind">Whether the question takes free text, one of a fixed set, or nothing.</param>
/// <param name="AnswerOptions">The options a choice question offers, in the order the configuration lists them.</param>
/// <param name="MinLength">The shortest text answer the configuration accepts.</param>
/// <param name="MaxLength">The longest text answer the configuration accepts.</param>
/// <param name="AllottedMinutes">
/// The allotment configured for this item, or null when it configures none. Null is not zero: an item with no
/// configured time produces no estimate rather than a default (FR-013, FR-014).
/// </param>
public sealed record GenericRequestItemOption(
    string Code,
    string Name,
    bool NeedsPart,
    bool NeedsDunnage,
    bool NeedsScrap,
    bool NeedsDie,
    string PromptText,
    GenericRequestAnswerKind AnswerKind,
    IReadOnlyList<string> AnswerOptions,
    int MinLength,
    int MaxLength,
    int? AllottedMinutes)
{
    /// <summary>Whether answering this item's question is required before the flow continues.</summary>
    public bool RequiresAnswer => AnswerKind != GenericRequestAnswerKind.None;
}

/// <summary>What kind of answer an item's configuration asks for.</summary>
public enum GenericRequestAnswerKind
{
    /// <summary>The item asks nothing.</summary>
    None,

    /// <summary>The item asks for free text.</summary>
    Text,

    /// <summary>The item asks for one of a fixed set of options.</summary>
    Choice,
}

/// <summary>A part the item's request can name.</summary>
public sealed record GenericRequestPartOption(string PartNumber, string Description, string QuantityOnHand);

/// <summary>A sequence a part can be requested at.</summary>
public sealed record GenericRequestSequenceOption(string Sequence, string Description);

/// <summary>One dunnage type, with the part that stands in for it.</summary>
public sealed record GenericRequestDunnageOption(string TypeName, string PartNumber);

/// <summary>A scrap type, with how many luggers are free to use.</summary>
public sealed record GenericRequestScrapOption(string ScrapType, int AvailableLuggerCount)
{
    /// <summary>The lugger count, ready to draw.</summary>
    public string AvailableLuggerDisplay => AvailableLuggerCount.ToString(CultureInfo.CurrentCulture);
}

/// <summary>A die the search matched.</summary>
public sealed record GenericRequestDieOption(string DieNumber, string Description, string Location);
