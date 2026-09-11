using System.Globalization;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Module_Waitlist.Services;

/// <inheritdoc cref="IAverageCoilWeightService"/>
/// <remarks>
/// The average skid weight is read through <c>sp_receiving_history_average_coil_weight</c> in the
/// receiving store rather than through a statement embedded here, per the SP-first rule (FR-015,
/// constitution III). The procedure's semantics are documented beside its artifact at
/// <c>Database/MTMReceivingApp/StoredProcedures/sp_receiving_history_average_coil_weight/create.sql</c>.
/// </remarks>
public sealed class AverageCoilWeightService : IAverageCoilWeightService
{
    /// <summary>The receiving store's procedure that returns one part's average skid weight.</summary>
    private const string AverageWeightProcedure = "sp_receiving_history_average_coil_weight";

    private readonly IMySqlHelperServer _mySqlHelperServer;

    public AverageCoilWeightService(IMySqlHelperServer mySqlHelperServer)
    {
        ArgumentNullException.ThrowIfNull(mySqlHelperServer);
        _mySqlHelperServer = mySqlHelperServer;
    }

    public async Task<string> ResolveAverageCoilWeightTextAsync(string partId, CancellationToken cancellationToken = default)
    {
        var normalizedPart = partId?.Trim() ?? string.Empty;
        if (normalizedPart.Length == 0)
        {
            return string.Empty;
        }

        // The receiving store is always read live (FR-001): there is no sample/mock short-circuit.
        StartupDebugLog.Info("AverageCoilWeight", $"Querying receiving_history. Part='{normalizedPart}'.");
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal) { ["@p_part_id"] = normalizedPart };
        var rows = await _mySqlHelperServer
            .ExecuteStoredProcedureQueryAsync(
                AverageWeightProcedure,
                parameters,
                MySqlDatabaseTarget.MtmReceivingApplication,
                cancellationToken)
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
