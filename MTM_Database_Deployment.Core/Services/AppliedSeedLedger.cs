namespace MTM_Database_Deployment.Services;

/// <summary>
/// Remembers which seed scripts a database has already received, so bringing a store up to date cannot re-seed it.
/// </summary>
/// <remarks>
/// <para>
/// A seed is written to be re-runnable, but "re-runnable" is not "harmless": several of them insert with
/// <c>ON DUPLICATE KEY UPDATE</c>, so a second pass writes the shipped default over whatever an operator has since
/// set. Safe update is the mode an operator uses on a store they are using, so it must not touch a seed that is
/// already in place.
/// </para>
/// <para>
/// The record lives in the database itself rather than on the machine that ran the tool: the next person to deploy
/// may be at a different workstation, and a ledger beside the executable would tell them nothing. The table is this
/// tool's own bookkeeping — no application reads it, and the comparison ignores it, so it never appears as a
/// leftover to be removed.
/// </para>
/// </remarks>
public static class AppliedSeedLedger
{
    /// <summary>The table the record is kept in. Tool bookkeeping, not application schema.</summary>
    public const string TableName = "deployment_seed_ledger";

    /// <summary>Every seed this database has already received, by the seed script's relative path.</summary>
    public static async Task<HashSet<string>> ReadAsync(
        Service_MySqlGateway gateway,
        string database,
        CancellationToken cancellationToken)
    {
        var applied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var rows = await gateway
                .QueryAsync(database, $"SELECT seed_name FROM `{TableName}`;", cancellationToken)
                .ConfigureAwait(false);

            foreach (var row in rows)
            {
                applied.Add(row[0]);
            }
        }
        catch (Exception)
        {
            // No ledger yet — reading it cannot fail a check, and an empty set is the truth for a store that has
            // never been deployed to by this tool.
        }

        return applied;
    }

    /// <summary>
    /// What this store has already received, and whether that answer came from the store itself rather than a record.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A store that was set up before this tool kept a record — the live database, the cache mirror, anything an
    /// earlier installer built — has already been seeded, but its ledger is empty because nobody wrote one. Running
    /// every seed against it would put shipped defaults back over values an operator has since chosen, which is the
    /// thing a safe update must not do.
    /// </para>
    /// <para>
    /// So an existing store with tables and no ledger is treated as seeded, and
    /// <c>NeedsRecord</c> is returned so the caller can write the record it just inferred. A genuinely new store has
    /// no tables, so it is not treated as seeded and its seeds run.
    /// </para>
    /// </remarks>
    public static async Task<(HashSet<string> AlreadySeeded, bool NeedsRecord)> ResolveAsync(
        Service_MySqlGateway gateway,
        string database,
        IReadOnlyCollection<string> seedPaths,
        CancellationToken cancellationToken)
    {
        var recorded = await ReadAsync(gateway, database, cancellationToken).ConfigureAwait(false);
        if (recorded.Count > 0 || seedPaths.Count == 0)
        {
            return (recorded, false);
        }

        if (!await StoreHasTablesAsync(gateway, database, cancellationToken).ConfigureAwait(false))
        {
            return (recorded, false);
        }

        return (new HashSet<string>(seedPaths, StringComparer.OrdinalIgnoreCase), true);
    }

    private static async Task<bool> StoreHasTablesAsync(
        Service_MySqlGateway gateway,
        string database,
        CancellationToken cancellationToken)
    {
        try
        {
            var rows = await gateway
                .QueryAsync(
                    "information_schema",
                    "SELECT COUNT(*) FROM TABLES "
                    + $"WHERE TABLE_SCHEMA = '{database.Replace("'", "''", StringComparison.Ordinal)}' "
                    + "AND TABLE_TYPE = 'BASE TABLE';",
                    cancellationToken)
                .ConfigureAwait(false);

            return rows.Count > 0 && long.TryParse(rows[0][0], out var count) && count > 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static Task EnsureAsync(Service_MySqlGateway gateway, string database, CancellationToken cancellationToken) =>
        gateway.ExecuteAsync(
            database,
            $"CREATE TABLE IF NOT EXISTS `{TableName}` ("
            + "seed_name VARCHAR(191) NOT NULL PRIMARY KEY, "
            + "applied_utc DATETIME NOT NULL) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",
            cancellationToken);

    public static Task RecordAsync(
        Service_MySqlGateway gateway,
        string database,
        string seedName,
        CancellationToken cancellationToken)
    {
        var literal = seedName.Replace("'", "''", StringComparison.Ordinal);

        return gateway.ExecuteAsync(
            database,
            $"INSERT INTO `{TableName}` (seed_name, applied_utc) VALUES ('{literal}', UTC_TIMESTAMP()) "
            + "ON DUPLICATE KEY UPDATE applied_utc = VALUES(applied_utc);",
            cancellationToken);
    }
}
