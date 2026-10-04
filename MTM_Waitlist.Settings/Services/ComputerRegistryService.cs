using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// The registry seam's implementation, over the shared <c>sp_core_computers_registry_*</c> procedures the
/// brief protects (T187; brief S3.2).
/// </summary>
/// <remarks>
/// <para>
/// <b>The procedures are the shared ones, not new ones.</b> The list, upsert, update and delete procedures
/// survive the launch rebuild because the brief keeps them for Settings and Work Center Catalogue, so this
/// type reads and writes through them rather than introducing a parallel set.
/// </para>
/// <para>
/// <b>A save is followed by a read.</b> The upsert and update procedures report affected rows and return no
/// row, so the row a caller receives is re-read through
/// <c>sp_core_computers_registry_lookup_by_name_get</c>. That read resolves a machine by its computer name or
/// its normalized hostname, which is the pair the screen actually has.
/// </para>
/// </remarks>
public sealed class ComputerRegistryService : IComputerRegistryService
{
    private const string GetAllProcedure = "sp_core_computers_registry_get_all";
    private const string UpsertProcedure = "sp_core_computers_registry_upsert";
    private const string UpdateProcedure = "sp_core_computers_registry_update";
    private const string DeleteProcedure = "sp_core_computers_registry_delete";
    private const string LookupByNameProcedure = "sp_core_computers_registry_lookup_by_name_get";

    private readonly IMySqlHelperServer _mySqlHelperServer;

    public ComputerRegistryService(IMySqlHelperServer mySqlHelperServer)
    {
        _mySqlHelperServer = mySqlHelperServer ?? throw new ArgumentNullException(nameof(mySqlHelperServer));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ComputerRecord>> GetAllComputersAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _mySqlHelperServer.ExecuteStoredProcedureQueryAsync(
            GetAllProcedure,
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        return rows
            .Select(Map)
            .Where(record => record is not null)
            .Select(record => record!)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<ComputerRecord> UpsertComputerAsync(
        string computerName,
        string hostnameNormalized,
        string macAddressNormalized,
        string displayName,
        string? description,
        CancellationToken cancellationToken = default)
    {
        _ = await _mySqlHelperServer.ExecuteStoredProcedureNonQueryAsync(
            UpsertProcedure,
            new Dictionary<string, object?>
            {
                ["p_computer_name"] = computerName.Trim(),
                ["p_hostname_normalized"] = hostnameNormalized.Trim(),
                ["p_mac_address_normalized"] = macAddressNormalized.Trim(),
                ["p_display_name"] = displayName.Trim(),
                ["p_description"] = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            },
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        return await ReadByNameAsync(computerName, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Computer upsert failed: no registry row returned.");
    }

    /// <inheritdoc />
    public async Task<ComputerRecord> UpdateComputerAsync(
        long id,
        string computerName,
        string hostnameNormalized,
        string macAddressNormalized,
        string displayName,
        string? description,
        bool isRegistered,
        CancellationToken cancellationToken = default)
    {
        _ = await _mySqlHelperServer.ExecuteStoredProcedureNonQueryAsync(
            UpdateProcedure,
            new Dictionary<string, object?>
            {
                ["p_id"] = id,
                ["p_computer_name"] = computerName.Trim(),
                ["p_hostname_normalized"] = hostnameNormalized.Trim(),
                ["p_mac_address_normalized"] = macAddressNormalized.Trim(),
                ["p_display_name"] = displayName.Trim(),
                ["p_description"] = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                ["p_is_registered"] = isRegistered ? 1 : 0,
            },
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        return await ReadByNameAsync(computerName, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Computer update failed: no registry row returned.");
    }

    /// <inheritdoc />
    public async Task<bool> DeleteComputerAsync(long id, CancellationToken cancellationToken = default)
    {
        var affected = await _mySqlHelperServer.ExecuteStoredProcedureNonQueryAsync(
            DeleteProcedure,
            new Dictionary<string, object?>
            {
                ["p_id"] = id,
            },
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        return affected > 0;
    }

    private async Task<ComputerRecord?> ReadByNameAsync(string name, CancellationToken cancellationToken)
    {
        var rows = await _mySqlHelperServer.ExecuteStoredProcedureQueryAsync(
            LookupByNameProcedure,
            new Dictionary<string, object?>
            {
                ["p_name"] = name.Trim(),
            },
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        return Map(rows.FirstOrDefault());
    }

    private static ComputerRecord? Map(IReadOnlyDictionary<string, object?>? row)
    {
        if (row is null)
        {
            return null;
        }

        return new ComputerRecord
        {
            Id = ReadInt64(row, "id"),
            ComputerName = ReadString(row, "computer_name"),
            DisplayName = ReadString(row, "display_name"),
            Description = ReadString(row, "description"),
            MacAddressNormalized = ReadString(row, "mac_address_normalized"),
            IsRegistered = ReadInt64(row, "is_registered") == 1,
        };
    }

    private static long ReadInt64(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return 0;
        }

        // TINYINT(1)/BIT columns are surfaced by the MySQL driver as a bool. Convert.ToInt64
        // on a bool throws InvalidCastException, so guard the type BEFORE converting so the
        // exception is never raised (not merely caught).
        if (value is bool boolValue)
        {
            return boolValue ? 1 : 0;
        }

        if (value is long longValue)
        {
            return longValue;
        }

        try
        {
            return Convert.ToInt64(value);
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
            return 0;
        }
    }

    private static string ReadString(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return string.Empty;
        }

        return Convert.ToString(value)?.Trim() ?? string.Empty;
    }
}
