using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Composes the two numbers a screen shows side by side for every Item: the <b>configured</b> allotted minutes
/// and the <b>observed</b> average of its completed requests (FR-017, FR-018, SC-007).
/// <para>
/// They stay two distinct values. The observed one is display data read through
/// <c>sp_waitlist_request_item_observed_average_get</c> and is <b>never written back</b> (FR-019); an Item with
/// no configured allotment is measured by the <see cref="UrgencySettingsService.DefaultMinutes">15-minute
/// fallback</see>, <b>labelled as a default</b> rather than as configured (FR-017).
/// </para>
/// </summary>
public interface IRequestItemObservedTimeService
{
    /// <summary>
    /// The configured/observed pair for every catalogued Item, in catalog order. An Item with no completed
    /// request carries <b>no observed value</b> rather than a fabricated zero (FR-026).
    /// </summary>
    Task<IReadOnlyList<RequestItemObservedTime>> GetObservedTimesAsync(CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="IRequestItemObservedTimeService"/>
public sealed class RequestItemObservedTimeService : IRequestItemObservedTimeService
{
    private const string ObservedAverageProcedure = "sp_waitlist_request_item_observed_average_get";

    private readonly IMySqlHelperServer _mySqlHelperServer;
    private readonly IRequestItemCatalogService _catalog;
    private readonly IRequestItemConfigurationService _configurationService;

    public RequestItemObservedTimeService(
        IMySqlHelperServer mySqlHelperServer,
        IRequestItemCatalogService catalog,
        IRequestItemConfigurationService configurationService)
    {
        _mySqlHelperServer = mySqlHelperServer;
        _catalog = catalog;
        _configurationService = configurationService;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RequestItemObservedTime>> GetObservedTimesAsync(CancellationToken cancellationToken = default)
    {
        var configurations = await _configurationService
            .GetConfigurationsAsync(cancellationToken)
            .ConfigureAwait(false);

        var rows = await _mySqlHelperServer
            .ExecuteStoredProcedureQueryAsync(
                ObservedAverageProcedure,
                new Dictionary<string, object?>(),
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken)
            .ConfigureAwait(false);

        return Compose(rows, configurations);
    }

    /// <summary>Composes the pairs. Pure, so the composition is unit-testable without a live store.</summary>
    internal IReadOnlyList<RequestItemObservedTime> Compose(
        IReadOnlyList<Dictionary<string, object?>> rows,
        RequestItemConfigurationSet configurations)
    {
        var observedByItem = new Dictionary<string, (int Count, TimeSpan? Average)>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows ?? Array.Empty<Dictionary<string, object?>>())
        {
            var item = ReadString(row, "item");
            if (string.IsNullOrWhiteSpace(item) || observedByItem.ContainsKey(item))
            {
                continue;
            }

            observedByItem[item] = (ReadInt(row, "completed_request_count") ?? 0, ReadSeconds(row, "average_seconds"));
        }

        var result = new List<RequestItemObservedTime>();
        foreach (var item in _catalog.GetAllItems())
        {
            var configuration = configurations.Get(item.Id);
            var configured = configuration.AllottedMinutes;

            observedByItem.TryGetValue(item.Id, out var observed);
            result.Add(new RequestItemObservedTime
            {
                Item = item.Id,
                DisplayName = RequestItemCatalog.ResolveDisplayName(item),
                CompletedRequestCount = observed.Count,
                ConfiguredMinutes = TimeSpan.FromMinutes(configured ?? UrgencySettingsService.DefaultMinutes),
                IsConfiguredValueDefault = !configured.HasValue,
                ObservedAverage = observed.Average
            });
        }

        return result;
    }

    private static string ReadString(IReadOnlyDictionary<string, object?> row, string key) =>
        row.TryGetValue(key, out var value) && value is not null
            ? Convert.ToString(value)?.Trim() ?? string.Empty
            : string.Empty;

    private static int? ReadInt(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null || value is DBNull)
        {
            return null;
        }

        return int.TryParse(Convert.ToString(value), out var number) ? number : null;
    }

    private static TimeSpan? ReadSeconds(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null || value is DBNull)
        {
            return null;
        }

        return double.TryParse(
            Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture),
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out var seconds)
                ? TimeSpan.FromSeconds(seconds)
                : null;
    }
}
