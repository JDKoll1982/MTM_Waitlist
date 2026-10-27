using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;

using MySqlConnector;

namespace MTM_Waitlist.Module_Core.Services;

/// <summary>
/// The plant's building list and each computer's building, read and written through the stored procedures that own
/// those two tables (constitution III).
/// </summary>
/// <remarks>
/// <para>
/// <b>A duplicate name is a refusal, not an error.</b> The store enforces
/// <c>uq_core_buildings_catalog_building_name</c>, so a name that is already taken raises MySQL's duplicate-key
/// error (1062). That is caught here and turned into a sentence the settings panel can show, because "that name
/// is already used" is an answer to what the person asked for rather than a fault.
/// </para>
/// <para>
/// <b>An unreadable store is an empty answer, not a fault.</b> The two reads answer with nothing when the store
/// cannot be reached, which is the same shape a first run has; the callers fall back to their own default rather
/// than the shell opening with no building at all.
/// </para>
/// </remarks>
public sealed class BuildingCatalogService : IBuildingCatalogService
{
    /// <summary>Every building the catalog holds, in display order.</summary>
    private const string BuildingsGetProcedure = "sp_core_buildings_get";

    /// <summary>Adds a building, or renames one and rewrites the rows that name it.</summary>
    private const string BuildingsUpsertProcedure = "sp_core_buildings_upsert";

    /// <summary>One computer's building, by computer name or normalised hostname.</summary>
    private const string ComputerBuildingGetProcedure = "sp_core_computer_building_get";

    /// <summary>Records one computer's building.</summary>
    private const string ComputerBuildingSetProcedure = "sp_core_computer_building_set";

    /// <summary>MySQL's duplicate-key error, which is how a taken building name answers.</summary>
    private const int DuplicateKeyErrorNumber = 1062;

    private readonly IMySqlHelperServer _mySqlHelperServer;
    private readonly IMachineFacts _machineFacts;

    public BuildingCatalogService(IMySqlHelperServer mySqlHelperServer, IMachineFacts machineFacts)
    {
        _mySqlHelperServer = mySqlHelperServer ?? throw new ArgumentNullException(nameof(mySqlHelperServer));
        _machineFacts = machineFacts ?? throw new ArgumentNullException(nameof(machineFacts));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BuildingRecord>> GetBuildingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await _mySqlHelperServer.ExecuteStoredProcedureQueryAsync(
                BuildingsGetProcedure,
                new Dictionary<string, object?>(),
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken).ConfigureAwait(false);

            return rows
                .Select(row => new BuildingRecord
                {
                    BuildingName = GetValue(row, "building_name"),
                    IsActive = IsTrue(GetValue(row, "is_active")),
                    SortRank = ParseInt(GetValue(row, "sort_rank")),
                })
                .Where(building => !string.IsNullOrWhiteSpace(building.BuildingName))
                .ToArray();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AppLog.Error(
                "Buildings",
                ex,
                "The building list could not be read, so every chooser falls back to the default this machine would have shown on a first run.");
            return Array.Empty<BuildingRecord>();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetSelectableBuildingNamesAsync(CancellationToken cancellationToken = default)
    {
        var buildings = await GetBuildingsAsync(cancellationToken).ConfigureAwait(false);

        return buildings
            .Where(building => building.IsActive)
            .Select(building => building.BuildingName)
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<string?> GetComputerBuildingAsync(string computerName, CancellationToken cancellationToken = default)
    {
        var normalizedName = computerName?.Trim() ?? string.Empty;
        if (normalizedName.Length == 0)
        {
            return null;
        }

        try
        {
            var rows = await _mySqlHelperServer.ExecuteStoredProcedureQueryAsync(
                ComputerBuildingGetProcedure,
                new Dictionary<string, object?> { ["p_computer_name"] = normalizedName },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken).ConfigureAwait(false);

            var building = rows.Count == 0 ? string.Empty : GetValue(rows[0], "building_name");
            return string.IsNullOrWhiteSpace(building) ? null : building;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AppLog.Error(
                "Buildings",
                ex,
                $"The building for computer '{normalizedName}' could not be read, so the default is used instead.");
            return null;
        }
    }

    /// <inheritdoc />
    public Task<string?> GetCurrentComputerBuildingAsync(CancellationToken cancellationToken = default) =>
        GetComputerBuildingAsync(GetCurrentComputerName(), cancellationToken);

    /// <inheritdoc />
    public async Task<bool> SetComputerBuildingAsync(
        string computerName,
        string buildingName,
        CancellationToken cancellationToken = default)
    {
        var normalizedComputer = computerName?.Trim() ?? string.Empty;
        var normalizedBuilding = buildingName?.Trim() ?? string.Empty;

        if (normalizedComputer.Length == 0 || normalizedBuilding.Length == 0)
        {
            return false;
        }

        await _mySqlHelperServer.ExecuteStoredProcedureNonQueryAsync(
            ComputerBuildingSetProcedure,
            new Dictionary<string, object?>
            {
                ["p_computer_name"] = normalizedComputer,
                ["p_building_name"] = normalizedBuilding,
            },
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        // The answer is read back rather than inferred from a changed-row count: a build that sets a computer to
        // the building it already stands in changes nothing and would read as a failure.
        var stored = await GetComputerBuildingAsync(normalizedComputer, cancellationToken).ConfigureAwait(false);
        return string.Equals(stored, normalizedBuilding, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public async Task<BuildingSaveResult> SaveBuildingAsync(
        string? originalName,
        string buildingName,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = buildingName?.Trim() ?? string.Empty;
        if (normalizedName.Length == 0)
        {
            return BuildingSaveResult.Refused("A building name is required.");
        }

        var normalizedOriginal = string.IsNullOrWhiteSpace(originalName) ? null : originalName.Trim();

        try
        {
            var affectedRows = await _mySqlHelperServer.ExecuteStoredProcedureNonQueryAsync(
                BuildingsUpsertProcedure,
                new Dictionary<string, object?>
                {
                    ["p_original_name"] = normalizedOriginal,
                    ["p_building_name"] = normalizedName,
                },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken).ConfigureAwait(false);

            if (affectedRows > 0)
            {
                return BuildingSaveResult.Saved(normalizedName);
            }

            // A rename that changes nothing reports no changed rows, which is a save that landed rather than a
            // refusal: the building exists under the name that was asked for.
            var buildings = await GetBuildingsAsync(cancellationToken).ConfigureAwait(false);
            return buildings.Any(building => string.Equals(building.BuildingName, normalizedName, StringComparison.Ordinal))
                ? BuildingSaveResult.Saved(normalizedName)
                : BuildingSaveResult.Refused("The building could not be saved.");
        }
        catch (MySqlException ex) when (ex.Number == DuplicateKeyErrorNumber)
        {
            return BuildingSaveResult.Refused($"A building named '{normalizedName}' already exists.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AppLog.Error("Buildings", ex, $"Saving the building '{normalizedName}' failed.");
            return BuildingSaveResult.Refused($"Unable to save the building: {ex.Message}");
        }
    }

    /// <summary>This computer's name as the registry knows it, which is what the building row is keyed by.</summary>
    private string GetCurrentComputerName() =>
        string.IsNullOrWhiteSpace(_machineFacts.Hostname) ? Environment.MachineName : _machineFacts.Hostname;

    private static bool IsTrue(string value) =>
        string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

    private static int ParseInt(string value) =>
        int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;

    private static string GetValue(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return string.Empty;
        }

        if (value is bool boolValue)
        {
            return boolValue ? "1" : "0";
        }

        return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
    }
}
