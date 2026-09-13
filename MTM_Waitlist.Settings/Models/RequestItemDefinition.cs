namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// Canonical request Item (the thing being handled) in the Category/Item model.
/// Type/Category refactor (2026-09-07): Item hangs under a Category umbrella and is shown as the
/// card Line 2 identifier. Mirror of one row of the catalog recorded in
/// specs/004-unified-card-item-picker/contracts/request-picker-flow.md §4.
///
/// This row carries <b>identity only</b> — existence, order, Category, the umbrella phrase, the card's
/// Line 2 template and the stored value. The Item's <i>behaviour</i> (control flow, whether an answer is
/// required, the prompt, the length limits, the options and the page's declared fields) lives on
/// <see cref="RequestItemConfiguration"/> and is data, changeable without a rebuild (FR-013, FR-015).
/// </summary>
public sealed class RequestItemDefinition
{
    /// <summary>Umbrella category this item belongs to (Pickup/Deliver/Assist/Other).</summary>
    public RequestCategory Category { get; init; }

    /// <summary>Display order within the category (CSV col 2).</summary>
    public int Order { get; init; }

    /// <summary>Stable normalized id (CSV col 3), e.g. "pickup-coil", "other".</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Resource key for the human-friendly display name (replaces the literal, FR-022). Resolved through
    /// the existing resource mechanism, e.g. <c>RequestItem.pickup-coil.DisplayName</c>. This is also the
    /// string the card falls back to when a Line 2 token cannot be resolved (FR-026).
    /// </summary>
    public string DisplayNameResourceKey { get; init; } = string.Empty;

    /// <summary>Normalized item name (CSV col 5).</summary>
    public string NormalizedName { get; init; } = string.Empty;

    /// <summary>The umbrella verb shown as card Line 1 (CSV col 6).</summary>
    public string UmbrellaVerb { get; init; } = string.Empty;

    /// <summary>
    /// The card's Line 2 (the Item's identifier) as a <c>{token}</c> template (CSV col 11), resolved by
    /// <c>RequestItemLine2Resolver</c> against the active-job snapshot and the captured answer.
    /// <para>
    /// <b>Syntax.</b> A bare <c>{token}</c> is replaced by that token's value. A token may carry
    /// <b>one</b> conditional alternative, written <c>{primary:secondary=conditionValue}</c>, which renders
    /// <c>secondary</c> when the captured value <c>destination</c> equals <c>conditionValue</c> and
    /// <c>primary</c> otherwise. That is the whole language — deliberately not a general expression
    /// evaluator, because exactly one rule needs it (<c>pickup-die</c>: the die's location when the
    /// captured destination is <c>Home Location</c>, otherwise the die's number). Literal text outside
    /// braces is copied through unchanged.
    /// </para>
    /// <para>
    /// <b>Token set (closed).</b> Job-derived: <c>part_number</c>, <c>part_description</c>,
    /// <c>die_number</c>, <c>die_location</c>, <c>dunnage_part</c>, <c>sequence_number</c>,
    /// <c>scrap_type</c>. Captured during the flow: <c>answer</c>, <c>destination</c>, <c>component</c>,
    /// <c>defect</c>. An unknown token is a reportable configuration problem, never a silent blank
    /// (FR-026).
    /// </para>
    /// </summary>
    public string CardLine2Template { get; init; } = string.Empty;

    /// <summary>The value persisted when this request is submitted (CSV col 18).</summary>
    public string ProducedValue { get; init; } = string.Empty;
}
