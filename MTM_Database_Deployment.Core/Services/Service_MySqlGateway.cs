using System.Net.Sockets;
using MTM_Database_Deployment.Models;
using MySqlConnector;

namespace MTM_Database_Deployment.Services;

/// <summary>
/// Connection resolution (preferred server, then localhost) and all MySQL access used by the tool.
/// </summary>
public sealed class Service_MySqlGateway
{
    private readonly Model_ConnectionSettings _settings;

    public Service_MySqlGateway(Model_ConnectionSettings settings)
    {
        _settings = settings;
    }

    public string ActiveServer { get; private set; } = string.Empty;

    public int Port => _settings.Port;

    public bool UsingFallback { get; private set; }

    public string ResolutionMessage { get; private set; } = string.Empty;

    /// <summary>
    /// Probes the configured server, then the local fallback. Throws when neither answers.
    /// </summary>
    public async Task<string> ResolveServerAsync(CancellationToken cancellationToken)
    {
        var preferred = _settings.PreferredServer?.Trim() ?? string.Empty;
        var fallback = _settings.FallbackServer?.Trim() ?? string.Empty;

        if (await CanConnectAsync(preferred, cancellationToken).ConfigureAwait(false))
        {
            ActiveServer = preferred;
            UsingFallback = false;
            ResolutionMessage = $"Connected to {preferred}:{_settings.Port}.";
            return ResolutionMessage;
        }

        if (!string.Equals(preferred, fallback, StringComparison.OrdinalIgnoreCase)
            && await CanConnectAsync(fallback, cancellationToken).ConfigureAwait(false))
        {
            ActiveServer = fallback;
            UsingFallback = true;
            ResolutionMessage = $"{preferred}:{_settings.Port} is unavailable. Using local server {fallback}:{_settings.Port}.";
            return ResolutionMessage;
        }

        throw new InvalidOperationException(
            $"Neither {preferred}:{_settings.Port} nor the local fallback {fallback}:{_settings.Port} is reachable. Start MySQL Server and retry.");
    }

    private async Task<bool> CanConnectAsync(string host, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        using var client = new TcpClient();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));

        try
        {
            await client.ConnectAsync(host, _settings.Port, timeout.Token).ConfigureAwait(false);
            return client.Connected;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public string BuildConnectionString(string? database)
    {
        var builder = new MySqlConnectionStringBuilder
        {
            Server = string.IsNullOrWhiteSpace(ActiveServer) ? _settings.PreferredServer : ActiveServer,
            Port = (uint)_settings.Port,
            UserID = _settings.User,
            Password = _settings.Password,
            CharacterSet = "utf8mb4",
            ConnectionTimeout = (uint)Math.Max(3, _settings.ConnectTimeoutSeconds),
            SslMode = MySqlSslMode.Preferred,
            AllowUserVariables = true,
            // MySQL 8's default authentication plugin needs this when no certificate is configured, which is how
            // every MTM host is set up. Without it the first sign-in fails on a server that is working perfectly.
            AllowPublicKeyRetrieval = true
        };

        if (!string.IsNullOrWhiteSpace(database))
        {
            builder.Database = database;
        }

        return builder.ConnectionString;
    }

    public async Task<MySqlConnection> OpenAsync(string? database, CancellationToken cancellationToken)
    {
        var connection = new MySqlConnection(BuildConnectionString(database));
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    public async Task ExecuteAsync(string database, string sql, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(database, cancellationToken).ConfigureAwait(false);
        await using var command = new MySqlCommand(sql, connection) { CommandTimeout = 0 };
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ExecuteWithoutDatabaseAsync(string sql, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(null, cancellationToken).ConfigureAwait(false);
        await using var command = new MySqlCommand(sql, connection) { CommandTimeout = 0 };
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a batch of statements on a single connection so session settings (such as
    /// FOREIGN_KEY_CHECKS) stay in effect for the whole batch.
    /// </summary>
    public async Task ExecuteStatementsAsync(
        string database,
        IReadOnlyList<string> statements,
        bool continueOnError,
        Action<string>? onError,
        bool disableForeignKeyChecks,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(database, cancellationToken).ConfigureAwait(false);

        if (disableForeignKeyChecks)
        {
            await using var toggleOff = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 0;", connection) { CommandTimeout = 0 };
            await toggleOff.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            foreach (var statement in statements)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await using var command = new MySqlCommand(statement, connection) { CommandTimeout = 0 };
                    await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception) when (continueOnError)
                {
                    onError?.Invoke(exception.Message);
                }
            }
        }
        finally
        {
            if (disableForeignKeyChecks)
            {
                try
                {
                    await using var toggleOn = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 1;", connection) { CommandTimeout = 0 };
                    await toggleOn.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // The connection is closing anyway.
                }
            }
        }
    }

    public async Task<List<string[]>> QueryAsync(string database, string sql, CancellationToken cancellationToken)
    {
        var rows = new List<string[]>();
        await using var connection = await OpenAsync(database, cancellationToken).ConfigureAwait(false);
        await using var command = new MySqlCommand(sql, connection) { CommandTimeout = 0 };
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var values = new string[reader.FieldCount];
            for (var field = 0; field < reader.FieldCount; field++)
            {
                values[field] = reader.IsDBNull(field) ? string.Empty : Convert.ToString(reader.GetValue(field)) ?? string.Empty;
            }

            rows.Add(values);
        }

        return rows;
    }

    public async Task<Dictionary<string, Model_TableSnapshot>> ReadTableSnapshotsAsync(
        string database,
        CancellationToken cancellationToken)
    {
        var snapshots = new Dictionary<string, Model_TableSnapshot>(StringComparer.OrdinalIgnoreCase);

        var tableRows = await QueryAsync(
            "information_schema",
            $"SELECT TABLE_NAME FROM TABLES WHERE TABLE_SCHEMA = '{Escape(database)}' AND TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME;",
            cancellationToken).ConfigureAwait(false);

        foreach (var tableRow in tableRows)
        {
            snapshots[tableRow[0]] = new Model_TableSnapshot { Name = tableRow[0] };
        }

        if (snapshots.Count == 0)
        {
            return snapshots;
        }

        var columnRows = await QueryAsync(
            "information_schema",
            "SELECT TABLE_NAME, COLUMN_NAME, COLUMN_TYPE, IS_NULLABLE, COLUMN_DEFAULT, EXTRA, COLUMN_COMMENT, " +
            "CHARACTER_SET_NAME, COLLATION_NAME, ORDINAL_POSITION FROM COLUMNS " +
            $"WHERE TABLE_SCHEMA = '{Escape(database)}' ORDER BY TABLE_NAME, ORDINAL_POSITION;",
            cancellationToken).ConfigureAwait(false);

        var columnsByTable = new Dictionary<string, List<Model_ColumnDefinition>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in columnRows)
        {
            var table = row[0];
            if (!columnsByTable.TryGetValue(table, out var list))
            {
                list = new List<Model_ColumnDefinition>();
                columnsByTable[table] = list;
            }

            list.Add(new Model_ColumnDefinition
            {
                Name = row[1],
                DataType = row[2],
                IsNullable = string.Equals(row[3], "YES", StringComparison.OrdinalIgnoreCase),
                DefaultValue = string.IsNullOrEmpty(row[4]) ? null : row[4],
                Extra = row[5],
                Comment = row[6],
                CharacterSet = string.IsNullOrEmpty(row[7]) ? null : row[7],
                Collation = string.IsNullOrEmpty(row[8]) ? null : row[8],
                OrdinalPosition = int.TryParse(row[9], out var position) ? position : 0
            });
        }

        var indexRows = await QueryAsync(
            "information_schema",
            "SELECT TABLE_NAME, INDEX_NAME, NON_UNIQUE, COLUMN_NAME FROM STATISTICS " +
            $"WHERE TABLE_SCHEMA = '{Escape(database)}' ORDER BY TABLE_NAME, INDEX_NAME, SEQ_IN_INDEX;",
            cancellationToken).ConfigureAwait(false);

        var indexesByTable = new Dictionary<string, Dictionary<string, List<string>>>(StringComparer.OrdinalIgnoreCase);
        var indexUniqueByTable = new Dictionary<string, Dictionary<string, bool>>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in indexRows)
        {
            var table = row[0];
            var indexName = row[1];
            if (!indexesByTable.TryGetValue(table, out var indexMap))
            {
                indexMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                indexesByTable[table] = indexMap;
            }

            if (!indexMap.TryGetValue(indexName, out var columnList))
            {
                columnList = new List<string>();
                indexMap[indexName] = columnList;
            }

            if (!string.IsNullOrWhiteSpace(row[3]))
            {
                columnList.Add(row[3]);
            }

            if (!indexUniqueByTable.TryGetValue(table, out var uniqueMap))
            {
                uniqueMap = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                indexUniqueByTable[table] = uniqueMap;
            }

            uniqueMap[indexName] = string.Equals(row[2], "0", StringComparison.Ordinal);
        }

        var keyRows = await QueryAsync(
            "information_schema",
            "SELECT k.TABLE_NAME, k.CONSTRAINT_NAME, k.COLUMN_NAME, k.REFERENCED_TABLE_NAME, k.REFERENCED_COLUMN_NAME, " +
            "r.UPDATE_RULE, r.DELETE_RULE FROM KEY_COLUMN_USAGE k JOIN REFERENTIAL_CONSTRAINTS r " +
            "ON r.CONSTRAINT_SCHEMA = k.CONSTRAINT_SCHEMA AND r.CONSTRAINT_NAME = k.CONSTRAINT_NAME " +
            $"WHERE k.TABLE_SCHEMA = '{Escape(database)}' AND k.REFERENCED_TABLE_NAME IS NOT NULL " +
            "ORDER BY k.TABLE_NAME, k.CONSTRAINT_NAME, k.ORDINAL_POSITION;",
            cancellationToken).ConfigureAwait(false);

        var foreignKeys = new Dictionary<string, List<Model_ForeignKeyDefinition>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in keyRows)
        {
            var table = row[0];
            if (!foreignKeys.TryGetValue(table, out var list))
            {
                list = new List<Model_ForeignKeyDefinition>();
                foreignKeys[table] = list;
            }

            list.Add(new Model_ForeignKeyDefinition
            {
                Name = row[1],
                Columns = new[] { row[2] },
                ReferencedTable = row[3],
                ReferencedColumns = new[] { row[4] },
                UpdateRule = row[5],
                DeleteRule = row[6]
            });
        }

        var result = new Dictionary<string, Model_TableSnapshot>(StringComparer.OrdinalIgnoreCase);
        foreach (var (tableName, snapshot) in snapshots)
        {
            var columns = columnsByTable.TryGetValue(tableName, out var columnList)
                ? (IReadOnlyList<Model_ColumnDefinition>)columnList
                : Array.Empty<Model_ColumnDefinition>();

            var indexes = new List<Model_IndexDefinition>();
            if (indexesByTable.TryGetValue(tableName, out var indexMap))
            {
                foreach (var (indexName, indexColumns) in indexMap)
                {
                    if (string.Equals(indexName, "PRIMARY", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    indexes.Add(new Model_IndexDefinition
                    {
                        Name = indexName,
                        IsUnique = indexUniqueByTable.TryGetValue(tableName, out var uniqueMap)
                            && uniqueMap.TryGetValue(indexName, out var unique)
                            && unique,
                        Columns = indexColumns
                    });
                }
            }

            var tableForeignKeys = foreignKeys.TryGetValue(tableName, out var foreignKeyList)
                ? (IReadOnlyList<Model_ForeignKeyDefinition>)MergeForeignKeyColumns(foreignKeyList)
                : Array.Empty<Model_ForeignKeyDefinition>();

            result[tableName] = new Model_TableSnapshot
            {
                Name = tableName,
                Columns = columns,
                Indexes = indexes,
                ForeignKeys = tableForeignKeys
            };
        }

        return result;
    }

    private static List<Model_ForeignKeyDefinition> MergeForeignKeyColumns(List<Model_ForeignKeyDefinition> source)
    {
        return source
            .GroupBy(foreignKey => foreignKey.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var first = group.First();
                return new Model_ForeignKeyDefinition
                {
                    Name = first.Name,
                    Columns = group.Select(item => item.Columns[0]).ToArray(),
                    ReferencedTable = first.ReferencedTable,
                    ReferencedColumns = group.Select(item => item.ReferencedColumns[0]).ToArray(),
                    UpdateRule = first.UpdateRule,
                    DeleteRule = first.DeleteRule
                };
            })
            .ToList();
    }

    private static string Escape(string value) => value.Replace("'", "''", StringComparison.Ordinal);
}
