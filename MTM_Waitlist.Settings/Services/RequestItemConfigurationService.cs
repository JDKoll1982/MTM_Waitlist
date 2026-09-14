using System.Text.Json;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Reads every Item's stored behaviour from <c>waitlist_request_item_configs</c> through
/// <c>sp_waitlist_request_item_configs_get</c> — <b>once</b>, as the wizard's Item step is entered, never per
/// keystroke and never per row render (FR-013, FR-024).
/// <para>
/// Both directions of disagreement are defined (data-model §3): a catalogued Item with no row is reported
/// <b>unavailable</b> in plain language, and a row naming an Item that is not in the catalog is ignored and
/// never offered.
/// </para>
/// </summary>
public interface IRequestItemConfigurationService
{
    /// <summary>
    /// Reads the whole configuration table once and returns a lookup keyed by Item code. Every catalogued Item
    /// appears in the result — one that had no row, or whose row could not be used, carries the unavailable
    /// report rather than being silently absent.
    /// </summary>
    Task<RequestItemConfigurationSet> GetConfigurationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// One Item's configuration, read through the same shape. Used where a single Item is inspected outside the
    /// wizard (the minutes editor), so a screen never re-reads the table per row.
    /// </summary>
    Task<RequestItemConfiguration> GetConfigurationAsync(string itemCode, CancellationToken cancellationToken = default);
}

/// <summary>
/// The read-once lookup returned by <see cref="IRequestItemConfigurationService.GetConfigurationsAsync"/>.
/// A pure value: it holds no connection and performs no read of its own.
/// </summary>
public sealed class RequestItemConfigurationSet
{
    private readonly IReadOnlyDictionary<string, RequestItemConfiguration> _byItem;

    internal RequestItemConfigurationSet(IReadOnlyDictionary<string, RequestItemConfiguration> byItem)
    {
        _byItem = byItem;
    }

    /// <summary>
    /// Builds a set from already-read rows. Used by the reader below and by callers that hold no connection,
    /// so a screen can be driven from a fixed set without a second read path.
    /// </summary>
    public static RequestItemConfigurationSet From(IEnumerable<RequestItemConfiguration> configurations)
    {
        var byItem = new Dictionary<string, RequestItemConfiguration>(StringComparer.OrdinalIgnoreCase);
        var all = new List<RequestItemConfiguration>();
        foreach (var configuration in configurations ?? Array.Empty<RequestItemConfiguration>())
        {
            if (configuration is null || string.IsNullOrWhiteSpace(configuration.Item))
            {
                continue;
            }

            byItem.TryAdd(configuration.Item, configuration);
            all.Add(configuration);
        }

        return new RequestItemConfigurationSet(byItem) { All = all };
    }

    /// <summary>Every catalogued Item's configuration, in catalog order.</summary>
    public IReadOnlyList<RequestItemConfiguration> All { get; internal set; } = Array.Empty<RequestItemConfiguration>();

    /// <summary>Whether the Item has a usable configuration row.</summary>
    public bool Contains(string? itemCode) =>
        itemCode is not null
        && _byItem.TryGetValue(itemCode.Trim(), out var configuration)
        && configuration.IsAvailable;

    /// <summary>
    /// The Item's configuration. An Item with no row (or an unusable one) is answered with the unavailable
    /// report — never null, so no caller can mistake absence for "nothing to report" (FR-026).
    /// </summary>
    public RequestItemConfiguration Get(string itemCode)
    {
        var normalized = itemCode?.Trim() ?? string.Empty;
        return _byItem.TryGetValue(normalized, out var configuration)
            ? configuration
            : RequestItemConfiguration.Missing(
                normalized,
                UnavailableMessageKey,
                ResolveUnavailableMessage());
    }

    /// <summary>The resource key of the unavailable report (FR-022).</summary>
    public const string UnavailableMessageKey = "RequestItem_Unavailable.Message";

    /// <summary>The resource key of the report shown when a reachable Item step has nothing to offer (FR-022).</summary>
    public const string NoItemsMessageKey = "RequestItem_NoneOffered.Message";

    /// <summary>
    /// The plain-language unavailable report, resolved through the resource mechanism, with a readable
    /// fallback so the person is never shown a bare resource key.
    /// </summary>
    public static string ResolveUnavailableMessage()
    {
        const string fallback =
            "This item isn't available right now. Its configuration is missing, so a supervisor needs to check it.";
        var localized = UnavailableMessageKey.GetLocalized();
        return string.IsNullOrWhiteSpace(localized)
            || string.Equals(localized, UnavailableMessageKey, StringComparison.Ordinal)
                ? fallback
                : localized;
    }

    /// <summary>
    /// The plain-language report for an Item step that has nothing to offer. The picker's availability pass makes
    /// this unreachable in practice (FR-002); it exists so an empty step is still reported rather than rendered
    /// blank, and never as a bare resource key.
    /// </summary>
    public static string ResolveNoItemsMessage()
    {
        const string fallback =
            "No item is available for this request right now. A supervisor needs to check the item configuration.";
        var localized = NoItemsMessageKey.GetLocalized();
        return string.IsNullOrWhiteSpace(localized)
            || string.Equals(localized, NoItemsMessageKey, StringComparison.Ordinal)
                ? fallback
                : localized;
    }
}

/// <inheritdoc cref="IRequestItemConfigurationService"/>
public sealed class RequestItemConfigurationService : IRequestItemConfigurationService
{
    private const string ConfigurationsProcedure = "sp_waitlist_request_item_configs_get";

    private readonly IMySqlHelperServer _mySqlHelperServer;
    private readonly IRequestItemCatalogService _catalog;

    public RequestItemConfigurationService(IMySqlHelperServer mySqlHelperServer, IRequestItemCatalogService catalog)
    {
        _mySqlHelperServer = mySqlHelperServer;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public async Task<RequestItemConfigurationSet> GetConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _mySqlHelperServer
            .ExecuteStoredProcedureQueryAsync(
                ConfigurationsProcedure,
                new Dictionary<string, object?>(),
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken)
            .ConfigureAwait(false);

        return BuildSet(rows);
    }

    /// <inheritdoc />
    public async Task<RequestItemConfiguration> GetConfigurationAsync(string itemCode, CancellationToken cancellationToken = default)
    {
        var set = await GetConfigurationsAsync(cancellationToken).ConfigureAwait(false);
        return set.Get(itemCode);
    }

    /// <summary>
    /// Builds the lookup from the read rows. A row whose <c>item</c> is not in the catalog is dropped — it is
    /// never offered and nothing breaks (spec Edge Cases, contract §7).
    /// </summary>
    internal RequestItemConfigurationSet BuildSet(IReadOnlyList<Dictionary<string, object?>> rows)
    {
        var byItem = new Dictionary<string, RequestItemConfiguration>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows ?? Array.Empty<Dictionary<string, object?>>())
        {
            var configuration = MapRow(row);
            if (configuration is null || !_catalog.GetAllItems().Any(i => string.Equals(i.Id, configuration.Item, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            // One row per Item is guaranteed by the table's unique key; a duplicate would be a store fault, and
            // the first row wins rather than throwing mid-wizard.
            byItem.TryAdd(configuration.Item, configuration);
        }

        var all = new List<RequestItemConfiguration>();
        foreach (var item in _catalog.GetAllItems())
        {
            all.Add(byItem.TryGetValue(item.Id, out var configuration)
                ? configuration
                : RequestItemConfiguration.Missing(
                    item.Id,
                    RequestItemConfigurationSet.UnavailableMessageKey,
                    RequestItemConfigurationSet.ResolveUnavailableMessage()));
        }

        return new RequestItemConfigurationSet(byItem) { All = all };
    }

    private static RequestItemConfiguration? MapRow(IReadOnlyDictionary<string, object?> row)
    {
        var item = ReadString(row, "item");
        if (string.IsNullOrWhiteSpace(item))
        {
            return null;
        }

        var optionsJson = NullIfBlank(ReadString(row, "options_json"));
        if (!TryParseOptions(optionsJson, out var options, out var optionsProblem))
        {
            return Unusable(item, $"its options payload is unusable: {optionsProblem}");
        }

        var detailFieldsJson = NullIfBlank(ReadString(row, "detail_fields_json"));
        if (!TryParseDetailFields(detailFieldsJson, out var detailFields, out var fieldsProblem))
        {
            return Unusable(item, $"its declared-fields payload is unusable: {fieldsProblem}");
        }

        var requiresAnswer = ReadBool(row, "requires_answer");
        var answerValueType = ParseValueType(ReadString(row, "answer_value_type"));
        if (requiresAnswer && answerValueType is null)
        {
            return Unusable(item, "it asks for an answer but declares no usable answer type");
        }

        return new RequestItemConfiguration
        {
            Item = item,
            Category = ReadString(row, "category"),
            ControlFlow = ReadStringOr(row, "control_flow", RequestItemConfiguration.DirectToConfirmation),
            RequiresAnswer = requiresAnswer,
            AnswerValueType = answerValueType,
            PromptText = NullIfBlank(ReadString(row, "prompt_text")),
            MinLength = ReadInt(row, "min_length") ?? 0,
            MaxLength = ReadInt(row, "max_length") ?? 200,
            OptionsJson = optionsJson,
            DetailFieldsJson = detailFieldsJson,
            Options = options,
            DetailFields = detailFields.OrderBy(field => field.Order).ToList(),
            AllottedMinutes = ReadInt(row, "allotted_minutes")
        };
    }

    /// <summary>
    /// Reports a row whose payload cannot be read. The Item is then unavailable rather than half-configured, the
    /// person is shown a plain-language sentence, and the reason is recorded for whoever maintains the
    /// configuration — never an empty list, never an exception escaping to the screen (FR-026).
    /// </summary>
    private static RequestItemConfiguration Unusable(string item, string problem)
    {
        StartupDebugLog.Error(
            "RequestItemConfiguration",
            new FormatException($"The configuration row for '{item}' cannot be used: {problem}."),
            $"The configuration row for '{item}' cannot be used, so the item is reported unavailable rather than half-configured (FR-026).");

        return RequestItemConfiguration.Malformed(item, RequestItemConfiguration.ResolveMalformedMessage());
    }

    /// <summary>
    /// Reads the ordered options payload. Absent is not a fault — an Item whose options arrive with the requesting
    /// job declares none here — but a payload that cannot be read as a list of text is (FR-026).
    /// </summary>
    private static bool TryParseOptions(string? payload, out List<string> options, out string problem)
    {
        options = [];
        problem = string.Empty;

        if (string.IsNullOrWhiteSpace(payload))
        {
            return true;
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                problem = "it is not a list";
                return false;
            }

            foreach (var element in document.RootElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(element.GetString()))
                {
                    problem = "a listed option is not text";
                    return false;
                }

                options.Add(element.GetString()!.Trim());
            }

            return true;
        }
        catch (JsonException ex)
        {
            problem = $"it is not valid JSON ({ex.Message})";
            return false;
        }
    }

    /// <summary>
    /// Reads the Item's declared page fields, validating each one's label, declared value type, source and
    /// declared order, so a field set the page cannot honour is reported rather than half-drawn (FR-013, FR-026).
    /// </summary>
    /// <remarks>
    /// The fields come back in the order the payload declares them in; the configuration's own <c>order</c> value
    /// is what the reader orders by, so a payload whose list order disagrees with its declared positions is still
    /// read the way the configuration declares it.
    /// </remarks>
    private static bool TryParseDetailFields(string? payload, out List<RequestItemFieldDefinition> fields, out string problem)
    {
        fields = [];
        problem = string.Empty;

        if (string.IsNullOrWhiteSpace(payload))
        {
            return true;
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                problem = "it is not a list of fields";
                return false;
            }

            foreach (var element in document.RootElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object)
                {
                    problem = "a listed field is not an object";
                    return false;
                }

                var label = ReadJsonString(element, "label");
                if (string.IsNullOrWhiteSpace(label))
                {
                    problem = "a declared field has no label";
                    return false;
                }

                if (!TryParseValueType(ReadJsonString(element, "value_type"), out var valueType))
                {
                    problem = $"the field '{label}' declares a value type that does not exist";
                    return false;
                }

                var source = ReadJsonString(element, "source");
                if (!IsKnownSource(source))
                {
                    problem = $"the field '{label}' declares a source that does not exist";
                    return false;
                }

                if (!TryReadDeclaredOrder(element, out var order))
                {
                    problem = $"the field '{label}' has no usable declared order";
                    return false;
                }

                fields.Add(new RequestItemFieldDefinition
                {
                    Label = label!.Trim(),
                    ValueType = valueType,
                    Source = source!.Trim(),
                    Order = order,
                    IsRequired = ReadJsonBool(element, "is_required"),
                });
            }

            return true;
        }
        catch (JsonException ex)
        {
            problem = $"it is not valid JSON ({ex.Message})";
            return false;
        }
    }

    /// <summary>Whether a declared source is one the page can resolve a value from.</summary>
    private static bool IsKnownSource(string? source)
        => !string.IsNullOrWhiteSpace(source)
           && (Has(source, RequestItemFieldDefinition.Sources.Job)
               || Has(source, RequestItemFieldDefinition.Sources.Answer)
               || Has(source, RequestItemFieldDefinition.Sources.Fixed));

    private static bool Has(string source, string candidate)
        => string.Equals(source.Trim(), candidate, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The field's declared position. The key lives inside the JSON payload and no bare <c>order</c> column is
    /// ever introduced (Database/Database-Ruleset.md); a position below one is not a position.
    /// </summary>
    private static bool TryReadDeclaredOrder(JsonElement field, out int order)
    {
        order = 0;
        return field.TryGetProperty("order", out var value)
               && value.ValueKind == JsonValueKind.Number
               && value.TryGetInt32(out order)
               && order >= 1;
    }

    private static string? ReadJsonString(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static bool ReadJsonBool(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;

    private static bool TryParseValueType(string? value, out RequestItemValueType valueType)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "string":
                valueType = RequestItemValueType.String;
                return true;
            case "enum":
                valueType = RequestItemValueType.Enum;
                return true;
            case "text":
                valueType = RequestItemValueType.Text;
                return true;
            default:
                valueType = RequestItemValueType.String;
                return false;
        }
    }

    private static RequestItemValueType? ParseValueType(string? value)
        => TryParseValueType(value, out var valueType) ? valueType : null;

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static string ReadString(IReadOnlyDictionary<string, object?> row, string key) =>
        row.TryGetValue(key, out var value) && value is not null
            ? Convert.ToString(value)?.Trim() ?? string.Empty
            : string.Empty;

    private static string ReadStringOr(IReadOnlyDictionary<string, object?> row, string key, string fallback)
    {
        var value = ReadString(row, key);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static bool ReadBool(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return false;
        }

        return value switch
        {
            bool b => b,
            _ => int.TryParse(Convert.ToString(value), out var number) ? number != 0 : false
        };
    }

    private static int? ReadInt(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null || value is DBNull)
        {
            return null;
        }

        return int.TryParse(Convert.ToString(value), out var number) ? number : null;
    }
}

/// <summary>
/// The choices an Item's enumerated answer draws on (FR-035).
/// <para>
/// The rule is a property of the <b>configuration row</b> and the requesting job — never of the Item's identity
/// (FR-013), so a second Item configured the same way behaves the same way:
/// </para>
/// <list type="number">
/// <item>a row that carries its own list uses that list, in the order it declares;</item>
/// <item>a row that declares an enumerated answer field sourced from the answer (<c>source = 'answer'</c>) and
/// carries no list of its own draws on the <b>requesting job's component list</b>, which is the path
/// <c>pickup-component</c> is configured for and the reason it was not raisable without it;</item>
/// <item>anything else yields no choices, and the details step reports that rather than inventing a list.</item>
/// </list>
/// </summary>
public static class RequestItemAnswerOptionsResolver
{
    /// <summary>
    /// The choices this Item's enumerated answer offers, or an empty list when the row declares none — an
    /// empty list is reported by the screen, never filled in with something plausible (FR-026, FR-035).
    /// </summary>
    public static IReadOnlyList<string> Resolve(
        RequestItemConfiguration configuration,
        RequestJobPartAvailability availability)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (availability is null || configuration.AnswerValueType != RequestItemValueType.Enum)
        {
            return Array.Empty<string>();
        }

        if (configuration.Options.Count > 0)
        {
            return configuration.Options;
        }

        var declaresEnumeratedAnswer = configuration.DetailFields.Any(field =>
            field.ValueType == RequestItemValueType.Enum
            && string.Equals(field.Source, RequestItemFieldDefinition.Sources.Answer, StringComparison.OrdinalIgnoreCase));

        return declaresEnumeratedAnswer
            ? availability.ComponentPartNumbers
            : Array.Empty<string>();
    }
}
