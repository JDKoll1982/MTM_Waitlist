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

        return new RequestItemConfiguration
        {
            Item = item,
            Category = ReadString(row, "category"),
            ControlFlow = ReadStringOr(row, "control_flow", RequestItemConfiguration.DirectToConfirmation),
            RequiresAnswer = ReadBool(row, "requires_answer"),
            AnswerValueType = ParseValueType(ReadString(row, "answer_value_type")),
            PromptText = NullIfBlank(ReadString(row, "prompt_text")),
            MinLength = ReadInt(row, "min_length") ?? 0,
            MaxLength = ReadInt(row, "max_length") ?? 200,
            OptionsJson = NullIfBlank(ReadString(row, "options_json")),
            DetailFieldsJson = NullIfBlank(ReadString(row, "detail_fields_json")),
            AllottedMinutes = ReadInt(row, "allotted_minutes")
        };
    }

    private static RequestItemValueType? ParseValueType(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "string" => RequestItemValueType.String,
        "enum" => RequestItemValueType.Enum,
        "text" => RequestItemValueType.Text,
        _ => null
    };

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
