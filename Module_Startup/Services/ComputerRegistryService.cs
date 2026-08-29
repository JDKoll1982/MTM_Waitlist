using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Startup.Services;

public sealed class ComputerRegistryService : IComputerRegistryService
{
    private readonly IMySqlHelperServer _mySqlHelperServer;

    public ComputerRegistryService(IMySqlHelperServer mySqlHelperServer)
    {
        _mySqlHelperServer = mySqlHelperServer;
    }

    public async Task<ComputerRecord?> LookupComputerAsync(
        string computerName,
        string macAddressNormalized,
        CancellationToken cancellationToken = default)
    {
        var rows = await _mySqlHelperServer.ExecuteSqlQueryAsync(
            """
            SELECT id, computer_name, display_name, description, mac_address_normalized, is_registered
            FROM core_computers_registry
            WHERE computer_name = @computer_name
              AND mac_address_normalized = @mac_address
            LIMIT 1;
            """,
            new Dictionary<string, object?>
            {
                ["computer_name"] = computerName.Trim(),
                ["mac_address"] = macAddressNormalized.Trim(),
            },
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        return Map(rows.FirstOrDefault());
    }

    public async Task<ComputerRecord?> LookupComputerByMacAsync(
        string macAddressNormalized,
        CancellationToken cancellationToken = default)
    {
        var rows = await _mySqlHelperServer.ExecuteSqlQueryAsync(
            """
            SELECT id, computer_name, display_name, description, mac_address_normalized, is_registered
            FROM core_computers_registry
            WHERE mac_address_normalized = @mac_address
            ORDER BY updated_utc DESC
            LIMIT 1;
            """,
            new Dictionary<string, object?>
            {
                ["mac_address"] = macAddressNormalized.Trim(),
            },
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        return Map(rows.FirstOrDefault());
    }

    public async Task<ComputerRecord> UpsertComputerAsync(
        string computerName,
        string hostnameNormalized,
        string macAddressNormalized,
        string displayName,
        string? description,
        CancellationToken cancellationToken = default)
    {
        _ = await _mySqlHelperServer.ExecuteSqlNonQueryAsync(
            """
            INSERT INTO core_computers_registry (
                public_id,
                computer_name,
                hostname_normalized,
                mac_address_normalized,
                display_name,
                description,
                is_registered,
                created_utc,
                updated_utc
            )
            VALUES (
                UUID(),
                @computer_name,
                @hostname,
                @mac_address,
                @display_name,
                @description,
                1,
                UTC_TIMESTAMP(),
                UTC_TIMESTAMP()
            )
            ON DUPLICATE KEY UPDATE
                computer_name = VALUES(computer_name),
                display_name = VALUES(display_name),
                description = VALUES(description),
                is_registered = 1,
                updated_utc = UTC_TIMESTAMP();
            """,
            new Dictionary<string, object?>
            {
                ["computer_name"] = computerName.Trim(),
                ["hostname"] = hostnameNormalized.Trim(),
                ["mac_address"] = macAddressNormalized.Trim(),
                ["display_name"] = displayName.Trim(),
                ["description"] = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            },
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        var record = await LookupComputerAsync(computerName, macAddressNormalized, cancellationToken).ConfigureAwait(false);
        return record ?? throw new InvalidOperationException("Computer upsert failed: no registry row returned.");
    }

    public async Task<ComputerRecord> UpdateComputerByMacAsync(
        string macAddressNormalized,
        string newComputerName,
        string hostnameNormalized,
        string displayName,
        string? description,
        CancellationToken cancellationToken = default)
    {
        _ = await _mySqlHelperServer.ExecuteSqlNonQueryAsync(
            """
            UPDATE core_computers_registry
            SET computer_name = @computer_name,
                hostname_normalized = @hostname,
                display_name = @display_name,
                description = @description,
                updated_utc = UTC_TIMESTAMP()
            WHERE mac_address_normalized = @mac_address
            ORDER BY id DESC
            LIMIT 1;
            """,
            new Dictionary<string, object?>
            {
                ["computer_name"] = newComputerName.Trim(),
                ["hostname"] = hostnameNormalized.Trim(),
                ["mac_address"] = macAddressNormalized.Trim(),
                ["display_name"] = displayName.Trim(),
                ["description"] = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            },
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        var record = await LookupComputerAsync(newComputerName, macAddressNormalized, cancellationToken).ConfigureAwait(false);
        return record ?? throw new InvalidOperationException("Computer update failed: no registry row returned.");
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

        return Convert.ToInt64(value);
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
