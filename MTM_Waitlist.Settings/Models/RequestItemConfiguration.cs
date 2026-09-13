using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// One Item's <b>stored behaviour</b> — the row of <c>waitlist_request_item_configs</c> that says how far
/// the flow goes, whether an answer is required, what the prompt and the limits are, which options are
/// offered and which fields the Item's page shows (FR-013). It is data: changing it changes the flow with
/// no code change and no rebuild (FR-015).
/// <para>
/// The Item's <i>identity</i> — existence, Category, order, the umbrella phrase and the card's Line 2 —
/// stays on <see cref="RequestItemDefinition"/>. This row is the behaviour half.
/// </para>
/// <para>
/// Column mapping is fixed by specs/004-unified-card-item-picker/data-model.md §3.
/// </para>
/// </summary>
public sealed class RequestItemConfiguration
{
    /// <summary>The Item's flow runs straight to confirmation; nothing is asked for.</summary>
    public const string DirectToConfirmation = "direct-to-confirmation";

    /// <summary>The Item collects one answer before confirmation.</summary>
    public const string CollectInputThenConfirm = "collect-input-then-confirm";

    /// <summary>The Item code this configuration belongs to (the table's <c>item</c> column).</summary>
    public string Item { get; init; } = string.Empty;

    /// <summary>The Category code the Item belongs to (the table's <c>category</c> column).</summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>One of <see cref="DirectToConfirmation"/> or <see cref="CollectInputThenConfirm"/>.</summary>
    public string ControlFlow { get; init; } = DirectToConfirmation;

    /// <summary>Whether the Item asks for exactly one answer.</summary>
    public bool RequiresAnswer { get; init; }

    /// <summary>The answer's declared value type (<c>enum</c> or <c>text</c>), or null where none is asked for.</summary>
    public RequestItemValueType? AnswerValueType { get; init; }

    /// <summary>The question shown when an answer is asked for.</summary>
    public string? PromptText { get; init; }

    /// <summary>Minimum length of a text answer (0 when the column is absent).</summary>
    public int MinLength { get; init; }

    /// <summary>Maximum length of a text answer (200 by default, per the table's default).</summary>
    public int MaxLength { get; init; } = 200;

    /// <summary>The raw ordered options payload for an enumerated answer, or null.</summary>
    public string? OptionsJson { get; init; }

    /// <summary>The raw declared-fields payload for the Item's page, or null.</summary>
    public string? DetailFieldsJson { get; init; }

    /// <summary>
    /// The Item's configured allotment in minutes, or <b>null when it is not configured</b>. Null is not
    /// "zero": the caller falls back to the labelled 15-minute default (FR-017).
    /// </summary>
    public int? AllottedMinutes { get; init; }

    /// <summary>The ordered options for an enumerated answer (parsed from <see cref="OptionsJson"/>).</summary>
    public IReadOnlyList<string> Options { get; init; } = Array.Empty<string>();

    /// <summary>The Item's declared page fields, in declared order (parsed from <see cref="DetailFieldsJson"/>).</summary>
    public IReadOnlyList<RequestItemFieldDefinition> DetailFields { get; init; } = Array.Empty<RequestItemFieldDefinition>();

    /// <summary>
    /// False when the Item has no configuration row at all, or when the row cannot be used. The Item is then
    /// reported <b>unavailable</b> in plain language rather than half-configured (FR-014, FR-026).
    /// </summary>
    public bool IsAvailable { get; init; } = true;

    /// <summary>
    /// Resource key of the plain-language report shown when <see cref="IsAvailable"/> is false (FR-022); the
    /// resolved text is <see cref="UnavailableMessage"/>.
    /// </summary>
    public string UnavailableMessageKey { get; init; } = string.Empty;

    /// <summary>
    /// The plain-language report itself, resolved through the resource mechanism with a readable fallback.
    /// Empty while the Item is available and nothing is wrong.
    /// </summary>
    public string UnavailableMessage { get; init; } = string.Empty;

    /// <summary>
    /// Resource key of the report shown when the row exists but its payload cannot be read. A malformed payload
    /// is a different fault from a missing row — the Item is configured, the configuration just cannot be used —
    /// so the person is told which of the two they are looking at (FR-026).
    /// </summary>
    public const string MalformedMessageKey = "RequestItem_ConfigurationMalformed.Message";

    /// <summary>
    /// A stand-in for a catalogued Item whose row cannot be used: the declared-fields or options payload is not
    /// readable, so nothing the row claims about the Item's behaviour can be trusted.
    /// </summary>
    public static RequestItemConfiguration Malformed(string item, string message) => new()
    {
        Item = item,
        IsAvailable = false,
        UnavailableMessageKey = MalformedMessageKey,
        UnavailableMessage = message,
    };

    /// <summary>
    /// The plain-language report for a payload that cannot be read, resolved through the resource mechanism with
    /// a readable fallback so the person is never shown a bare resource key.
    /// </summary>
    public static string ResolveMalformedMessage()
    {
        const string fallback =
            "This item's configuration isn't readable, so a supervisor needs to check it before it can be requested.";
        var localized = MalformedMessageKey.GetLocalized();
        return string.IsNullOrWhiteSpace(localized)
            || string.Equals(localized, MalformedMessageKey, StringComparison.Ordinal)
                ? fallback
                : localized;
    }

    /// <summary>
    /// A stand-in for a catalogued Item that has no configuration row: available nowhere, and honest about it.
    /// </summary>
    public static RequestItemConfiguration Missing(string item, string messageKey, string message) => new()
    {
        Item = item,
        IsAvailable = false,
        UnavailableMessageKey = messageKey,
        UnavailableMessage = message
    };
}
