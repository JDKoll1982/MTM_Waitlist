using System.Globalization;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Module_Waitlist.Services;

/// <inheritdoc cref="IAverageCoilWeightService"/>
public sealed class AverageCoilWeightService : IAverageCoilWeightService
{
    private const string RecvMockDataSettingKey = "Feature.RecvMockData";

    private const string AverageWeightQuery =
        "SELECT ROUND(AVG(quantity), 0) AS AverageWeight FROM receiving_history WHERE part_id = @partId";

    private readonly ILocalSettingsService _localSettingsService;
    private readonly IMySqlHelperServer _mySqlHelperServer;

    public AverageCoilWeightService(ILocalSettingsService localSettingsService, IMySqlHelperServer mySqlHelperServer)
    {
        _localSettingsService = localSettingsService;
        _mySqlHelperServer = mySqlHelperServer;
    }

    public async Task<string> ResolveAverageCoilWeightTextAsync(string partId, CancellationToken cancellationToken = default)
    {
        var normalizedPart = partId?.Trim() ?? string.Empty;
        if (normalizedPart.Length == 0)
        {
            return string.Empty;
        }

        var useMockData = await _localSettingsService.ReadSettingAsync<bool?>(RecvMockDataSettingKey).ConfigureAwait(false) ?? false;
        if (useMockData)
        {
            var mockText = SampleAverageCoilWeightCatalog.GetAverageCoilWeightText(normalizedPart);
            StartupDebugLog.Info("AverageCoilWeight", $"Mock ON. Part='{normalizedPart}' -> '{mockText}'.");
            return mockText;
        }

        StartupDebugLog.Info("AverageCoilWeight", $"Querying receiving_history. Part='{normalizedPart}'.");
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal) { ["@partId"] = normalizedPart };
        var rows = await _mySqlHelperServer
            .ExecuteSqlQueryAsync(AverageWeightQuery, parameters, MySqlDatabaseTarget.MtmReceivingApplication, cancellationToken)
            .ConfigureAwait(false);

        if (rows.Count == 0 || !rows[0].TryGetValue("AverageWeight", out var raw) || raw is null)
        {
            StartupDebugLog.Info("AverageCoilWeight", $"No rows for Part='{normalizedPart}'. Returning empty.");
            return string.Empty;
        }

        var average = Convert.ToDecimal(raw, CultureInfo.InvariantCulture);
        var rounded = decimal.Round(average, 0);
        var text = $"{rounded.ToString("N0", CultureInfo.InvariantCulture)} lb";
        StartupDebugLog.Info("AverageCoilWeight", $"Part='{normalizedPart}' average skid weight = '{text}'.");
        return text;
    }
}
