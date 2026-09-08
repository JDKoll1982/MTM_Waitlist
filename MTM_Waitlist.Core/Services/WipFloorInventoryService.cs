using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using System.Text;

namespace MTM_Waitlist.Module_Core.Services;

/// <summary>
/// Executes the checked-in MTM WIP Application queue script <c>GetWipFloorQuantities.sql</c> against
/// the <c>mtm_wip_application_winforms</c> database (via <see cref="IMySqlHelperServer"/> with
/// <see cref="MySqlDatabaseTarget.MtmWipApplication"/>) and maps the single result row to a
/// <see cref="WipFloorQuantitySnapshot"/>. Never throws: a missing script/connection or SQL error
/// yields <c>null</c> so the disposition resolver can fall back to the Infor-only snapshot.
/// </summary>
public sealed class WipFloorInventoryService
{
    private const string FloorSnapshotScriptName = "GetWipFloorQuantities";

    private readonly IMySqlHelperServer _mysqlHelperServer;

    public WipFloorInventoryService(IMySqlHelperServer mysqlHelperServer)
    {
        _mysqlHelperServer = mysqlHelperServer;
    }

    /// <summary>Returns the live floor snapshot for a part, or <c>null</c> when unavailable.</summary>
    public async Task<WipFloorQuantitySnapshot?> GetFloorSnapshotAsync(
        string partNumber,
        CancellationToken cancellationToken = default)
    {
        var normalized = (partNumber ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var script = await WaitlistWipMySqlScriptStore.LoadAsync(FloorSnapshotScriptName, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(script))
        {
            StartupDebugLog.Info("WipFloorInventory", $"Script '{FloorSnapshotScriptName}' loaded empty; returning null.");
            return null;
        }

        StartupDebugLog.Info("WipFloorInventory", $"GetFloorSnapshotAsync executing '{FloorSnapshotScriptName}' for part '{normalized}'.");
        try
        {
            var rows = await _mysqlHelperServer.ExecuteSqlQueryAsync(
                script,
                new Dictionary<string, object?>
                {
                    ["PartNumber"] = normalized,
                },
                MySqlDatabaseTarget.MtmWipApplication,
                cancellationToken).ConfigureAwait(false);

            if (rows.Count == 0)
            {
                return null;
            }

            var snapshot = MapToSnapshot(rows[0]);
            StartupDebugLog.Info(
                "WipFloorInventory",
                $"GetFloorSnapshotAsync completed for part '{normalized}': FG={snapshot.FinishedGoodsFloorQuantity}, O/S={snapshot.OutsideServiceFloorQuantity}, NCM={snapshot.NonConformingFloorQuantity}, WIP={snapshot.WipFloorQuantity}.");
            return snapshot;
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("WipFloorInventory", ex, $"GetFloorSnapshotAsync failed for part '{normalized}'.");
            return null;
        }
    }

    /// <summary>Maps one GetWipFloorQuantities result row to a snapshot (column names are case-insensitive).</summary>
    internal static WipFloorQuantitySnapshot MapToSnapshot(IReadOnlyDictionary<string, object?> row)
    {
        return new WipFloorQuantitySnapshot
        {
            FinishedGoodsFloorQuantity = GetDecimal(row, "FinishedGoodsFloorQuantity"),
            OutsideServiceFloorQuantity = GetDecimal(row, "OutsideServiceFloorQuantity"),
            NonConformingFloorQuantity = GetDecimal(row, "NonConformingFloorQuantity"),
            WipFloorQuantity = GetDecimal(row, "WipFloorQuantity"),
        };
    }

    private static decimal GetDecimal(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return 0m;
        }

        return value switch
        {
            decimal decimalValue => decimalValue,
            double doubleValue => Convert.ToDecimal(doubleValue),
            float floatValue => Convert.ToDecimal(floatValue),
            int intValue => intValue,
            long longValue => longValue,
            _ => decimal.TryParse(Convert.ToString(value), out var parsed) ? parsed : 0m,
        };
    }
}

/// <summary>
/// Resolves checked-in MTM WIP Application MySQL queue scripts for the Waitlist module from the app
/// output (<c>Database/MTMWipApp/Queues/Module_Waitlist/Queues</c>).
/// </summary>
internal static class WaitlistWipMySqlScriptStore
{
    private const string ScriptFolderName = "Database";
    private const string QueryFolderName = "MTMWipApp";
    private const string ModuleFolderName = "Queues";
    private const string ScriptModuleFolderName = "Module_Waitlist";
    private const string ScriptQueryFolderName = "Queues";

    public static async Task<string> LoadAsync(string scriptName, CancellationToken cancellationToken = default)
    {
        var scriptPath = GetScriptPath(scriptName);
        if (!File.Exists(scriptPath))
        {
            return string.Empty;
        }

        await using var stream = File.OpenRead(scriptPath);
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        cancellationToken.ThrowIfCancellationRequested();
        return await reader.ReadToEndAsync().ConfigureAwait(false);
    }

    private static string GetScriptPath(string scriptName)
    {
        var normalizedName = scriptName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)
            ? scriptName
            : $"{scriptName}.sql";

        return Path.Combine(
            AppContext.BaseDirectory,
            ScriptFolderName,
            QueryFolderName,
            ModuleFolderName,
            ScriptModuleFolderName,
            ScriptQueryFolderName,
            normalizedName);
    }
}
