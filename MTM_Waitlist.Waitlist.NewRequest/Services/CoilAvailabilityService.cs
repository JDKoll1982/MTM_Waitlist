using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

/// <inheritdoc cref="ICoilAvailabilityService"/>
/// <remarks>
/// <para>
/// <b>Real source (FR-019).</b> The coil on the job is read from the saved Work Center Setup record for the
/// work center — <c>setup_active_jobs</c> in the internal <c>mtm_waitlist</c> store, through
/// <c>sp_setup_active_jobs_latest_by_work_center_get</c> (the same procedure the Setup-side
/// <c>ActiveJobItemResolverService</c> reads). Nothing is assumed: when no active job is saved for the work
/// center, or the job carries no coil, the answer is "no coil" rather than a default.
/// </para>
/// <para>
/// <b>Why not a Visual queue script.</b> The coil is carried on the job's subordinate-part payload, which the
/// Setup workflow already populated from Infor Visual (including its own fallback path). Reading the saved
/// record avoids introducing a second, unverified Visual query for the same fact and keeps the waitlist store
/// the single source for "what job is set up on this work center".
/// </para>
/// <para>
/// <b>Coil identity.</b> A subordinate part is a coil when its part number carries the <c>MMC</c> prefix —
/// the same rule <c>ActiveJobItemResolverService.CanonicalCategory</c> applies when the Setup record is read
/// — so the rule is applied here to the same payload and the two readers agree.
/// </para>
/// </remarks>
public sealed class CoilAvailabilityService : ICoilAvailabilityService
{
    /// <summary>The internal store's procedure that returns the latest active job per work center.</summary>
    private const string LatestActiveJobsProcedure = "sp_setup_active_jobs_latest_by_work_center_get";

    /// <summary>The part-number prefix that identifies a coil subordinate part.</summary>
    private const string CoilPartNumberPrefix = "MMC";

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IMySqlHelperServer _mySqlHelperServer;
    private readonly IAverageCoilWeightService _averageCoilWeightService;

    public CoilAvailabilityService(
        IMySqlHelperServer mySqlHelperServer,
        IAverageCoilWeightService averageCoilWeightService)
    {
        ArgumentNullException.ThrowIfNull(mySqlHelperServer);
        ArgumentNullException.ThrowIfNull(averageCoilWeightService);
        _mySqlHelperServer = mySqlHelperServer;
        _averageCoilWeightService = averageCoilWeightService;
    }

    public async Task<WaitlistCoilInfo> GetCoilForJobAsync(string? workCenter, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedWorkCenter = workCenter?.Trim() ?? string.Empty;
        if (normalizedWorkCenter.Length == 0)
        {
            StartupDebugLog.Info("NewRequestJobType", "No work center supplied; the active job cannot be resolved.");
            return NoCoil();
        }

        var rows = await _mySqlHelperServer
            .ExecuteStoredProcedureQueryAsync(
                LatestActiveJobsProcedure,
                new Dictionary<string, object?>(),
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken)
            .ConfigureAwait(false);

        var row = rows.FirstOrDefault(candidate =>
            string.Equals(ReadString(candidate, "work_center"), normalizedWorkCenter, StringComparison.OrdinalIgnoreCase));

        if (row is null)
        {
            StartupDebugLog.Info("NewRequestJobType", $"No saved active job for work center '{normalizedWorkCenter}'; no coil to report.");
            return NoCoil();
        }

        var coil = ReadCoilParts(ReadString(row, "subordinate_parts_json")).FirstOrDefault();
        if (coil is null)
        {
            StartupDebugLog.Info("NewRequestJobType", $"Active job on work center '{normalizedWorkCenter}' has no coil part.");
            return NoCoil();
        }

        var averageWeight = await _averageCoilWeightService
            .ResolveAverageCoilWeightTextAsync(coil.PartNumber, cancellationToken)
            .ConfigureAwait(false);

        var quantityOnHand = coil.OnHandQuantity.ToString("0.##", CultureInfo.InvariantCulture);
        StartupDebugLog.Info(
            "NewRequestJobType",
            $"Active job on work center '{normalizedWorkCenter}' carries coil '{coil.PartNumber}' with {quantityOnHand} on hand.");

        return new WaitlistCoilInfo
        {
            HasCoil = true,
            CoilNumber = coil.PartNumber,
            QuantityOnHand = quantityOnHand,
            Description = coil.Description,
            AverageWeight = averageWeight,
        };
    }

    private static WaitlistCoilInfo NoCoil() => new() { HasCoil = false };

    private static IReadOnlyList<CoilPart> ReadCoilParts(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<CoilPart>();
        }

        try
        {
            var parts = JsonSerializer.Deserialize<List<CoilPart>>(json, s_jsonOptions) ?? [];
            return parts
                .Where(part => !string.IsNullOrEmpty(part.PartNumber)
                    && part.PartNumber.TrimStart().StartsWith(CoilPartNumberPrefix, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        catch (JsonException)
        {
            // A malformed payload means the coil cannot be determined; it is never a reason to assume one.
            return Array.Empty<CoilPart>();
        }
    }

    private static string ReadString(IReadOnlyDictionary<string, object?> row, string column)
    {
        if (!row.TryGetValue(column, out var value) || value is null || value == DBNull.Value)
        {
            return string.Empty;
        }

        return Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// The subset of the saved subordinate-part payload this service needs. Keys match the PascalCase JSON
    /// written by <c>SetupPersistenceService</c> when the setup is saved.
    /// </summary>
    private sealed class CoilPart
    {
        [JsonPropertyName("PartNumber")]
        public string PartNumber { get; set; } = string.Empty;

        [JsonPropertyName("Description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("OnHandQuantity")]
        public decimal OnHandQuantity { get; set; }
    }
}
