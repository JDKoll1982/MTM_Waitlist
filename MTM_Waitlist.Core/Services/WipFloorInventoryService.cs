using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using System.Text;

namespace MTM_Waitlist.Module_Core.Services;

/// <summary>One location the WIP floor holds stock of a part in, and how much.</summary>
/// <param name="Location">The location name.</param>
/// <param name="Quantity">The quantity held there.</param>
public sealed record WipLocationQuantity(string Location, decimal Quantity);

/// <summary>How much the WIP floor holds of one part at one operation, and how many rows that sum came from.</summary>
/// <param name="Quantity">The quantity summed over every location holding the part at that operation.</param>
/// <param name="LocationCount">
/// How many locations contributed. Zero means the floor holds the part nowhere at that operation, which is a
/// different answer from the floor not having been asked.
/// </param>
public sealed record WipOperationQuantity(decimal Quantity, int LocationCount);

/// <summary>
/// What one floor read answered: whether it succeeded, and the rows when it did.
/// </summary>
/// <remarks>
/// The three reads added for the generic part step report success where the older three do not. A coverage
/// read that renders no parts can afford to swallow a failure into an empty list; a part, sequence or quantity
/// cannot, because "the floor holds none" and "the floor could not be asked" are different answers and only one
/// of them may be shown (FR-011, FR-017).
/// </remarks>
/// <typeparam name="T">The row shape the read answers.</typeparam>
public sealed record WipFloorReadResult<T>
{
    private WipFloorReadResult(bool succeeded, IReadOnlyList<T> rows)
    {
        Succeeded = succeeded;
        Rows = rows;
    }

    /// <summary>Whether the floor answered at all.</summary>
    public bool Succeeded { get; }

    /// <summary>The rows it answered with. Empty when it did not answer.</summary>
    public IReadOnlyList<T> Rows { get; }

    /// <summary>The floor answered, possibly with no rows.</summary>
    public static WipFloorReadResult<T> Answered(IReadOnlyList<T> rows) => new(true, rows);

    /// <summary>The floor could not be asked.</summary>
    public static WipFloorReadResult<T> Unavailable() => new(false, Array.Empty<T>());
}

/// <summary>
/// Executes the checked-in MTM WIP Application queue scripts <c>GetWipFloorQuantities.sql</c>,
/// <c>GetWipInventoryPartNumbers.sql</c> and <c>GetWipInventoryLocations.sql</c> against the <c>mtm_wip_application_winforms</c> database (via
/// <see cref="IMySqlHelperServer"/> with <see cref="MySqlDatabaseTarget.MtmWipApplication"/>), and maps
/// <c>GetWipFloorQuantities</c>' single result row to a <see cref="WipFloorQuantitySnapshot"/>. Never throws: a
/// missing script/connection or SQL error yields <c>null</c> (or no part numbers) so a caller can fall back.
/// </summary>
public sealed class WipFloorInventoryService
{
    private const string FloorSnapshotScriptName = "GetWipFloorQuantities";
    private const string InventoryPartNumbersScriptName = "GetWipInventoryPartNumbers";
    private const string InventoryLocationsScriptName = "GetWipInventoryLocations";
    private const string PartMatchesScriptName = "GetWipInventoryPartMatches";
    private const string PartOperationsScriptName = "GetWipPartOperations";
    private const string OperationQuantityScriptName = "GetWipOperationQuantity";

    private readonly IMySqlHelperServer _mysqlHelperServer;

    public WipFloorInventoryService(IMySqlHelperServer mysqlHelperServer)
    {
        _mysqlHelperServer = mysqlHelperServer;
    }

    /// <summary>
    /// The distinct part numbers the WIP floor holds inventory for, in the script's own order, or an empty list
    /// when the floor cannot be read.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The part numbers the WIP floor can name; empty when the store or the script is unavailable.</returns>
    /// <remarks>
    /// A part the application can name is a part it can picture (FR-001), so this is the WIP half of the
    /// missing-picture list. An unreachable floor answers with no parts rather than throwing: a coverage read that
    /// cannot reach one system reports the systems it could reach, and never invents the ones it could not.
    /// </remarks>
    public async Task<IReadOnlyList<string>> GetInventoryPartNumbersAsync(CancellationToken cancellationToken = default)
    {
        var script = await WaitlistWipMySqlScriptStore.LoadAsync(InventoryPartNumbersScriptName, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(script))
        {
            AppLog.Info("WipFloorInventory", $"Script '{InventoryPartNumbersScriptName}' loaded empty; returning no part numbers.");
            return Array.Empty<string>();
        }

        try
        {
            var rows = await _mysqlHelperServer.ExecuteSqlQueryAsync(
                script,
                new Dictionary<string, object?>(),
                MySqlDatabaseTarget.MtmWipApplication,
                cancellationToken).ConfigureAwait(false);

            var partNumbers = rows
                .Select(row => GetString(row, "PartNumber"))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            AppLog.Info("WipFloorInventory", $"GetInventoryPartNumbersAsync returned {partNumbers.Length} part number(s).");
            return partNumbers;
        }
        catch (Exception ex)
        {
            AppLog.Error("WipFloorInventory", ex, "GetInventoryPartNumbersAsync failed.");
            return Array.Empty<string>();
        }
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
            AppLog.Info("WipFloorInventory", $"Script '{FloorSnapshotScriptName}' loaded empty; returning null.");
            return null;
        }

        AppLog.Info("WipFloorInventory", $"GetFloorSnapshotAsync executing '{FloorSnapshotScriptName}' for part '{normalized}'.");
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
            AppLog.Info(
                "WipFloorInventory",
                $"GetFloorSnapshotAsync completed for part '{normalized}': FG={snapshot.FinishedGoodsFloorQuantity}, O/S={snapshot.OutsideServiceFloorQuantity}, NCM={snapshot.NonConformingFloorQuantity}, WIP={snapshot.WipFloorQuantity}.");
            return snapshot;
        }
        catch (Exception ex)
        {
            AppLog.Error("WipFloorInventory", ex, $"GetFloorSnapshotAsync failed for part '{normalized}'.");
            return null;
        }
    }

    /// <summary>
    /// The locations the WIP floor holds stock of a part in, with the quantity at each, or an empty list when the
    /// part has none or the floor cannot be read.
    /// </summary>
    /// <param name="partNumber">The part to look up.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>One <see cref="WipLocationQuantity"/> per location that holds stock.</returns>
    public async Task<IReadOnlyList<WipLocationQuantity>> GetInventoryLocationsAsync(
        string partNumber,
        CancellationToken cancellationToken = default)
    {
        var normalized = (partNumber ?? string.Empty).Trim();
        if (normalized.Length == 0)
        {
            return Array.Empty<WipLocationQuantity>();
        }

        var script = await WaitlistWipMySqlScriptStore.LoadAsync(InventoryLocationsScriptName, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(script))
        {
            AppLog.Info("WipFloorInventory", $"Script '{InventoryLocationsScriptName}' loaded empty; returning no locations.");
            return Array.Empty<WipLocationQuantity>();
        }

        try
        {
            var rows = await _mysqlHelperServer.ExecuteSqlQueryAsync(
                script,
                new Dictionary<string, object?> { ["PartNumber"] = normalized },
                MySqlDatabaseTarget.MtmWipApplication,
                cancellationToken).ConfigureAwait(false);

            return rows
                .Select(row => new WipLocationQuantity(GetString(row, "Location"), GetDecimal(row, "Quantity")))
                .Where(item => item.Location.Length > 0 && item.Quantity > 0m)
                .ToArray();
        }
        catch (Exception ex)
        {
            AppLog.Error("WipFloorInventory", ex, $"GetInventoryLocationsAsync failed for part '{normalized}'.");
            return Array.Empty<WipLocationQuantity>();
        }
    }

    /// <summary>
    /// The parts the floor holds that a typed term matches, or <see cref="WipFloorReadResult{T}.Unavailable"/>
    /// when the floor could not be asked.
    /// </summary>
    /// <param name="term">The term the requester typed.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    public async Task<WipFloorReadResult<string>> FindPartMatchesAsync(
        string term,
        CancellationToken cancellationToken = default)
    {
        var normalized = (term ?? string.Empty).Trim();
        if (normalized.Length == 0)
        {
            return WipFloorReadResult<string>.Answered(Array.Empty<string>());
        }

        return await ReadAsync(
            PartMatchesScriptName,
            new Dictionary<string, object?> { ["PartNumber"] = normalized },
            rows => rows
                .Select(row => GetString(row, "PartNumber"))
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// The distinct operations the floor holds one part at, or
    /// <see cref="WipFloorReadResult{T}.Unavailable"/> when the floor could not be asked.
    /// </summary>
    /// <param name="partNumber">The part to look up.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    public async Task<WipFloorReadResult<string>> GetPartOperationsAsync(
        string partNumber,
        CancellationToken cancellationToken = default)
    {
        var normalized = (partNumber ?? string.Empty).Trim();
        if (normalized.Length == 0)
        {
            return WipFloorReadResult<string>.Answered(Array.Empty<string>());
        }

        return await ReadAsync(
            PartOperationsScriptName,
            new Dictionary<string, object?> { ["PartNumber"] = normalized },
            rows => rows
                .Select(row => GetString(row, "Sequence"))
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// How much of one part the floor holds at one operation, or
    /// <see cref="WipFloorReadResult{T}.Unavailable"/> when the floor could not be asked.
    /// </summary>
    /// <param name="partNumber">The part to look up.</param>
    /// <param name="operation">The operation, which is the sequence the requester chose.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <remarks>
    /// A zero is a real answer and carries a location count of zero with it: the floor answered, and it holds
    /// the part nowhere at that operation. That is deliberately different from a failed read, which reports no
    /// quantity at all.
    /// </remarks>
    public async Task<WipFloorReadResult<WipOperationQuantity>> GetOperationQuantityAsync(
        string partNumber,
        string operation,
        CancellationToken cancellationToken = default)
    {
        var normalizedPart = (partNumber ?? string.Empty).Trim();
        var normalizedOperation = (operation ?? string.Empty).Trim();
        if (normalizedPart.Length == 0 || normalizedOperation.Length == 0)
        {
            return WipFloorReadResult<WipOperationQuantity>.Answered(Array.Empty<WipOperationQuantity>());
        }

        return await ReadAsync(
            OperationQuantityScriptName,
            new Dictionary<string, object?>
            {
                ["PartNumber"] = normalizedPart,
                ["Operation"] = normalizedOperation,
            },
            rows => rows
                .Select(row => new WipOperationQuantity(
                    GetDecimal(row, "OperationQuantity"),
                    (int)GetDecimal(row, "LocationCount")))
                .ToArray(),
            cancellationToken).ConfigureAwait(false);
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

    /// <summary>
    /// Load the named script and map its rows, reporting a missing script, an unreachable store and a SQL fault
    /// all as one <see cref="WipFloorReadResult{T}.Unavailable"/>.
    /// </summary>
    private async Task<WipFloorReadResult<T>> ReadAsync<T>(
        string scriptName,
        IReadOnlyDictionary<string, object?> parameters,
        Func<IReadOnlyList<Dictionary<string, object?>>, IReadOnlyList<T>> map,
        CancellationToken cancellationToken)
    {
        var script = await WaitlistWipMySqlScriptStore.LoadAsync(scriptName, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(script))
        {
            AppLog.Info("WipFloorInventory", $"Script '{scriptName}' loaded empty; the floor is reported unavailable.");
            return WipFloorReadResult<T>.Unavailable();
        }

        try
        {
            var rows = await _mysqlHelperServer.ExecuteSqlQueryAsync(
                script,
                parameters,
                MySqlDatabaseTarget.MtmWipApplication,
                cancellationToken).ConfigureAwait(false);

            return WipFloorReadResult<T>.Answered(map(rows));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            AppLog.Error("WipFloorInventory", ex, $"'{scriptName}' could not be read; the floor is reported unavailable.");
            return WipFloorReadResult<T>.Unavailable();
        }
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

    private static string GetString(IReadOnlyDictionary<string, object?> row, string key) =>
        row.TryGetValue(key, out var value) && value is not null
            ? Convert.ToString(value)?.Trim() ?? string.Empty
            : string.Empty;
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
