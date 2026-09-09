using System.Text.Json;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;

namespace MTM_Waitlist.Module_Setup.Services;

/// <inheritdoc cref="IActiveJobItemResolverService"/>
public sealed class ActiveJobItemResolverService : IActiveJobItemResolverService
{
    private const string LatestActiveJobsProcedure = "sp_setup_active_jobs_latest_by_work_center_get";

    private readonly IMySqlHelperServer _mySqlHelperServer;

    public ActiveJobItemResolverService(IMySqlHelperServer mySqlHelperServer)
    {
        _mySqlHelperServer = mySqlHelperServer;
    }

    public async Task<SetupActiveJobSnapshot?> ResolveAsync(string workCenter, CancellationToken cancellationToken = default)
    {
        var normalizedWorkCenter = workCenter?.Trim() ?? string.Empty;
        if (normalizedWorkCenter.Length == 0)
        {
            return null;
        }

        var rows = await _mySqlHelperServer.ExecuteStoredProcedureQueryAsync(
            LatestActiveJobsProcedure,
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        var row = rows.FirstOrDefault(r =>
            string.Equals(ReadString(r, "work_center"), normalizedWorkCenter, StringComparison.OrdinalIgnoreCase));
        if (row is null)
        {
            return null;
        }

        return new SetupActiveJobSnapshot
        {
            WorkCenter = ReadString(row, "work_center"),
            WorkOrder = ReadString(row, "work_order"),
            PartNumber = ReadString(row, "part_number"),
            SequenceNumber = ReadString(row, "sequence_number"),
            SubordinateParts = DeserializeSubordinateParts(ReadString(row, "subordinate_parts_json")),
            DunnageParts = DeserializeDunnageParts(ReadString(row, "selected_dunnage_parts_json")),
        };
    }

    private static IReadOnlyList<SetupSubordinatePart> DeserializeSubordinateParts(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<SetupSubordinatePart>();
        }

        try
        {
            var parts = JsonSerializer.Deserialize<List<SetupSubordinatePart>>(json) ?? new List<SetupSubordinatePart>();
            foreach (var part in parts)
            {
                part.Category = CanonicalCategory(part);
            }

            return parts;
        }
        catch (JsonException)
        {
            return Array.Empty<SetupSubordinatePart>();
        }
    }

    private static IReadOnlyList<SetupDunnagePart> DeserializeDunnageParts(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<SetupDunnagePart>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<SetupDunnagePart>>(json)
                ?? new List<SetupDunnagePart>();
        }
        catch (JsonException)
        {
            return Array.Empty<SetupDunnagePart>();
        }
    }

    /// <summary>
    /// Canonicalizes a subordinate part's category per the Waitlist spec
    /// (GetSubordinateParts.sql + Request-Config-Template.csv Source columns):
    /// <c>MMC→Coil</c>, <c>MMF→Flatstock</c>, <c>FGT→Die</c>, otherwise the stored
    /// category when recognized, else <c>Component</c>. Prefix is authoritative so a
    /// legacy/mis-tagged part still lands in the correct bucket (e.g. the sample
    /// <c>MMF0001154</c> tagged Component still resolves as Flatstock).
    /// </summary>
    internal static string CanonicalCategory(SetupSubordinatePart part)
    {
        var partNumber = part.PartNumber?.Trim() ?? string.Empty;
        if (partNumber.StartsWith("MMC", StringComparison.OrdinalIgnoreCase))
        {
            return "Coil";
        }

        if (partNumber.StartsWith("MMF", StringComparison.OrdinalIgnoreCase))
        {
            return "Flatstock";
        }

        if (partNumber.StartsWith("FGT", StringComparison.OrdinalIgnoreCase))
        {
            return "Die";
        }

        var stored = part.Category?.Trim() ?? string.Empty;
        return IsKnownCategory(stored) ? stored : "Component";
    }

    private static bool IsKnownCategory(string category)
        => string.Equals(category, "Coil", StringComparison.OrdinalIgnoreCase)
            || string.Equals(category, "Flatstock", StringComparison.OrdinalIgnoreCase)
            || string.Equals(category, "Die", StringComparison.OrdinalIgnoreCase)
            || string.Equals(category, "Component", StringComparison.OrdinalIgnoreCase);

    private static string ReadString(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return string.Empty;
        }

        return Convert.ToString(value)?.Trim() ?? string.Empty;
    }
}
