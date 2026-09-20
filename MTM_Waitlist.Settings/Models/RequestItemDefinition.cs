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
    /// The card's Line 1 as a <c>{token}</c> template, or empty when the Item's first line is the umbrella phrase
    /// alone. It is the same deliberately-small language the identifier uses, plus one token of its own:
    /// <c>{umbrella}</c>, which is the Item's own <see cref="UmbrellaVerb"/>.
    /// <para>
    /// It exists because a first line sometimes has to say <b>which</b> job part the request is about. The die
    /// Items read <c>{umbrella} {job_part_number}</c>, so their cards say <c>Pickup Die: PART-9003</c> instead of
    /// leaving the handler to work out which part the die is for. A template whose tokens cannot all be resolved
    /// degrades to the umbrella phrase, so the first line always says what kind of request it is (FR-005).
    /// </para>
    /// </summary>
    public string CardLine1Template { get; init; } = string.Empty;

    /// <summary>
    /// The card's Line 2 (the Item's identifier) as a <c>{token}</c> template (CSV col 11), resolved by
    /// <c>RequestItemLine2Resolver</c> against the active-job snapshot and the captured answer.
    /// <para>
    /// <b>Syntax.</b> A bare <c>{token}</c> is replaced by that token's value. That is the whole language —
    /// deliberately not a general expression evaluator. Literal text outside braces is copied through unchanged.
    /// A conditional alternative (<c>{primary:secondary=conditionValue}</c>) was the language's one further rule,
    /// evaluated against the captured <c>destination</c>; it retired with the question it existed for on
    /// 2026-09-20 (FR-054, D22), so no row may name <c>destination</c> and no template may carry a conditional.
    /// </para>
    /// <para>
    /// <b>Token set (closed).</b> Job-derived: <c>part_number</c>, <c>part_description</c>,
    /// <c>die_number</c>, <c>die_location</c>, <c>die</c>, <c>dunnage_part</c>, <c>sequence_number</c>,
    /// <c>scrap_type</c>, <c>job_part_number</c>. Captured during the flow: <c>answer</c>, <c>component</c>,
    /// <c>defect</c>. An unknown token is a reportable configuration problem, never a silent blank
    /// (FR-026).
    /// </para>
    /// </summary>
    public string CardLine2Template { get; init; } = string.Empty;

    /// <summary>The value persisted when this request is submitted (CSV col 18).</summary>
    public string ProducedValue { get; init; } = string.Empty;
}
