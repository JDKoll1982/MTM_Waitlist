using MTM_Waitlist.Module_Core.Helpers;

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

/// <summary>Which system supplied a part row, or a sequence, to the part step.</summary>
/// <remarks>
/// The three values are also the fixed order the sources are listed in, so a sequence two systems agree on
/// always reads the same way (plan OD-5).
/// </remarks>
public enum SequenceSource
{
    /// <summary>The part is the work order's own part in Infor Visual.</summary>
    VisualOwnPart,

    /// <summary>The part appears as a requirement at an operation in Infor Visual.</summary>
    VisualRequirement,

    /// <summary>The WIP floor holds the part at that operation.</summary>
    WipFloor,
}

/// <summary>The name a person reads for each source, in the one place that decides it.</summary>
public static class SequenceSourceNames
{
    /// <summary>The source's name, localized.</summary>
    public static string Of(SequenceSource source) => source switch
    {
        SequenceSource.VisualOwnPart => "GenericRequest_Part_Source_VisualOwnPart.Text".GetLocalized(),
        SequenceSource.VisualRequirement => "GenericRequest_Part_Source_VisualRequirement.Text".GetLocalized(),
        SequenceSource.WipFloor => "GenericRequest_Part_Source_WipFloor.Text".GetLocalized(),
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, "That source has no name."),
    };
}

/// <summary>
/// A part the requester can state, with the system that supplied it.
/// </summary>
/// <param name="PartNumber">The accepted number.</param>
/// <param name="Description">Its description, or empty when the supplying system holds none.</param>
/// <param name="Source">The system that supplied this row.</param>
/// <remarks>
/// The quantity on hand is deliberately absent: a quantity belongs to a chosen sequence, not to a part
/// (FR-007).
/// </remarks>
public sealed record GenericRequestPartOption(string PartNumber, string Description, SequenceSource Source)
{
    /// <summary>The supplying system's name, ready to draw.</summary>
    public string SourceDisplay => SequenceSourceNames.Of(Source);

    /// <summary>
    /// The part number, because a control that fills a text field from an item falls back to this when nothing
    /// declares a display member. A text box showing a record's own shape is a display bug, not information.
    /// </summary>
    public override string ToString() => PartNumber;
}

/// <summary>
/// A sequence one or more systems attribute to the accepted part.
/// </summary>
/// <param name="Sequence">The sequence number, always a usable one.</param>
/// <param name="Description">What stands at that sequence, or empty when nothing is recorded.</param>
/// <param name="Sources">Every system that supplied this number, once each, in the fixed source order.</param>
/// <param name="OperationStatus">The Infor Visual operation status code, or empty when it records none.</param>
/// <param name="IsLastOperation">
/// True when nothing later on the same work order is still to be run, which is the plant's own test for the
/// finished good: its tag prints the FG format for the last operation and the WIP format for every other one.
/// </param>
/// <param name="NextResource">
/// The resource the part goes to after this operation, which is the line a WIP tag prints. "Outside Service" is
/// one of those names and not a type of its own, because subcontracted work is routed to the OUTSIDE_SERVICE shop
/// resource. Empty for the last operation, which has nowhere further to go.
/// </param>
/// <param name="HasCancelledOnlyOperations">True when every operation behind the number is cancelled.</param>
public sealed record GenericRequestSequenceOption(
    string Sequence,
    string Description,
    IReadOnlyList<SequenceSource> Sources,
    string OperationStatus,
    bool IsLastOperation,
    string NextResource,
    bool HasCancelledOnlyOperations)
{
    /// <summary>Every source that supplied this number, joined the way it is drawn (FR-004).</summary>
    public string SourcesDisplay => string.Join(", ", Sources.Select(SequenceSourceNames.Of));

    /// <summary>
    /// Every source that supplied this number, joined by its stable name, which is what the request records
    /// (FR-016). The recorded value does not change with the machine's language; only the drawn one does.
    /// </summary>
    public string SourceCodes => string.Join(", ", Sources.Select(source => source.ToString()));
}

/// <summary>A scrap type the requester can state.</summary>
public sealed record GenericRequestScrapOption(string ScrapType);

/// <summary>A die the search matched.</summary>
public sealed record GenericRequestDieOption(string DieNumber, string Description, string Location);
