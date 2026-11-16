using MTM_Database_Deployment.Models;

namespace MTM_Database_Deployment.Services;

/// <summary>
/// Copies every table of the chosen database into a database called <c>{database}_backup</c> before a deployment
/// writes anything.
/// </summary>
/// <remarks>
/// <para>
/// There is no second copy to fall back on: the plant host runs with binary logging off, so it has no point-in-time
/// recovery, and the Mock service's daily dump is a day old. A run that drops a database, or that removes a column
/// an operator confirmed, therefore has no undo unless the tool makes one. This is that copy, and it happens before
/// the first statement of every run.
/// </para>
/// <para>
/// The copy is made on the server rather than written to a file: <c>CREATE TABLE ... LIKE</c> carries the columns
/// and indexes and <c>INSERT ... SELECT</c> carries the rows, so nothing leaves the host and no external client is
/// needed on an operator's machine. Foreign-key constraints are deliberately not carried over — a backup is there
/// to be read when the live schema is already broken, so it must be readable on its own.
/// </para>
/// </remarks>
public sealed class DatabaseBackup
{
    private readonly Service_MySqlGateway _gateway;

    public DatabaseBackup(Service_MySqlGateway gateway)
    {
        _gateway = gateway;
    }

    /// <summary>The database a copy of <paramref name="database"/> is kept in.</summary>
    public static string BackupName(string database) => $"{database}_backup";

    /// <summary>
    /// Fails unless <paramref name="target"/> is a name a database of its own can carry next to
    /// <paramref name="source"/>.
    /// </summary>
    /// <remarks>
    /// The name reaches a statement, so it is checked here rather than trusted: letters, digits and underscores
    /// only, within MySQL's 64-character limit, and never the source itself.
    /// </remarks>
    public static void EnsureCopyNameIsUsable(string source, string target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            throw new ArgumentException("Type a name for the copy first.", nameof(target));
        }

        if (target.Length > 64)
        {
            throw new ArgumentException("A database name cannot be longer than 64 characters.", nameof(target));
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(target, "^[A-Za-z0-9_]+$"))
        {
            throw new ArgumentException(
                "Use only letters, numbers and underscores in the copy's name.", nameof(target));
        }

        if (string.Equals(source, target, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The copy needs a different name from the database it copies.", nameof(target));
        }
    }

    /// <summary>
    /// Makes an exact duplicate of a database — tables with their keys, indexes and comments, every row, the views,
    /// the routines and the triggers — into a database of its own.
    /// </summary>
    /// <param name="sourceDatabase">The database to copy. It is only read.</param>
    /// <param name="targetDatabase">The database to create, or to replace when <paramref name="replaceExisting"/> is set.</param>
    /// <param name="replaceExisting">
    /// Drops an existing target first. Off by default, because the name is typed by hand and the database it lands
    /// on may hold somebody's work.
    /// </param>
    /// <param name="progress">Receives one plain sentence per phase.</param>
    /// <param name="cancellationToken">Stops between statements.</param>
    /// <returns>What was copied, and the sentence the operator is shown.</returns>
    /// <remarks>
    /// Everything is read from the server with <c>SHOW CREATE</c> and written back through the same connection, so
    /// the duplicate is exact without needing a client tool on the operator's machine: the DDL carries the foreign
    /// keys, indexes, comments and the <c>AUTO_INCREMENT</c> counter that <c>CREATE TABLE ... LIKE</c> would lose.
    /// </remarks>
    public async Task<BackupOutcome> DuplicateAsync(
        string sourceDatabase,
        string targetDatabase,
        bool replaceExisting,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        EnsureCopyNameIsUsable(sourceDatabase, targetDatabase);

        var outcome = new BackupOutcome
        {
            SourceDatabase = sourceDatabase,
            BackupDatabase = targetDatabase
        };

        if (!await DatabaseExistsAsync(sourceDatabase, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"{sourceDatabase} does not exist on that computer, so there is nothing to copy.");
        }

        var targetExists = await DatabaseExistsAsync(targetDatabase, cancellationToken).ConfigureAwait(false);
        if (targetExists && !replaceExisting)
        {
            throw new InvalidOperationException(
                $"{targetDatabase} already exists. Tick 'Replace it if a database with that name already exists' "
                + "to replace it, or type a different name.");
        }

        progress?.Report(targetExists
            ? $"Replacing {targetDatabase} with a copy of {sourceDatabase}..."
            : $"Creating {targetDatabase} as a copy of {sourceDatabase}...");

        if (targetExists)
        {
            await _gateway
                .ExecuteWithoutDatabaseAsync($"DROP DATABASE IF EXISTS `{Quote(targetDatabase)}`;", cancellationToken)
                .ConfigureAwait(false);
        }

        var charset = await ReadSchemaCharacterSetAsync(sourceDatabase, cancellationToken).ConfigureAwait(false);
        await _gateway
            .ExecuteWithoutDatabaseAsync(
                $"CREATE DATABASE `{Quote(targetDatabase)}` CHARACTER SET {charset.CharacterSet} "
                + $"COLLATE {charset.Collation};",
                cancellationToken)
            .ConfigureAwait(false);

        var tables = await ReadBaseTablesAsync(sourceDatabase, cancellationToken).ConfigureAwait(false);

        // 1. The tables, with their keys and comments. Foreign-key checks are off for the batch because a table can
        //    name a table that has not been created yet.
        var tableDdl = new List<string>(tables.Count);
        foreach (var table in tables)
        {
            cancellationToken.ThrowIfCancellationRequested();
            tableDdl.Add(await ReadDefinitionAsync("TABLE", sourceDatabase, table, cancellationToken).ConfigureAwait(false));
        }

        await _gateway
            .ExecuteStatementsAsync(targetDatabase, tableDdl, false, null, true, cancellationToken)
            .ConfigureAwait(false);

        // 2. The rows, inserted from the source so no value is re-encoded on the way.
        progress?.Report($"Copying the rows of {tables.Count} table(s)...");
        var inserts = tables
            .Select(table =>
                $"INSERT INTO `{Quote(targetDatabase)}`.`{Quote(table)}` "
                + $"SELECT * FROM `{Quote(sourceDatabase)}`.`{Quote(table)}`;")
            .ToList();

        await _gateway
            .ExecuteStatementsAsync(targetDatabase, inserts, false, null, true, cancellationToken)
            .ConfigureAwait(false);

        foreach (var table in tables)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sourceRows = await CountRowsAsync(sourceDatabase, table, cancellationToken).ConfigureAwait(false);
            var copiedRows = await CountRowsAsync(targetDatabase, table, cancellationToken).ConfigureAwait(false);

            if (sourceRows != copiedRows)
            {
                throw new InvalidOperationException(
                    $"The copy of '{table}' holds {copiedRows} of its {sourceRows} rows, so {targetDatabase} is not "
                    + "a complete copy.");
            }

            outcome.TableCount++;
            outcome.RowCount += copiedRows;
        }

        // 3. The views, then the routines, then the triggers. Triggers come last so they cannot fire while the rows
        //    are being inserted; a view is rewritten only where it names the source schema, which the server stores
        //    fully qualified.
        var views = await ReadProgrammableObjectsAsync("VIEW", sourceDatabase, targetDatabase, cancellationToken)
            .ConfigureAwait(false);
        if (views.Count > 0)
        {
            progress?.Report($"Copying {views.Count} view(s)...");
            await _gateway
                .ExecuteStatementsAsync(
                    targetDatabase, views.Select(item => item.Definition).ToList(), false, null, false, cancellationToken)
                .ConfigureAwait(false);
            outcome.ViewCount = views.Count;
        }

        var routines = await ReadProgrammableObjectsAsync("PROCEDURE", sourceDatabase, targetDatabase, cancellationToken)
            .ConfigureAwait(false);
        routines.AddRange(
            await ReadProgrammableObjectsAsync("FUNCTION", sourceDatabase, targetDatabase, cancellationToken)
                .ConfigureAwait(false));
        if (routines.Count > 0)
        {
            progress?.Report($"Copying {routines.Count} stored instruction(s)...");
            await _gateway
                .ExecuteStatementsAsync(
                    targetDatabase, routines.Select(item => item.Definition).ToList(), false, null, false, cancellationToken)
                .ConfigureAwait(false);
            outcome.RoutineCount = routines.Count;
        }

        var triggers = await ReadProgrammableObjectsAsync("TRIGGER", sourceDatabase, targetDatabase, cancellationToken)
            .ConfigureAwait(false);
        if (triggers.Count > 0)
        {
            progress?.Report($"Copying {triggers.Count} trigger(s)...");
            await _gateway
                .ExecuteStatementsAsync(
                    targetDatabase, triggers.Select(item => item.Definition).ToList(), false, null, false, cancellationToken)
                .ConfigureAwait(false);
        }

        outcome.WasCreated = true;
        outcome.Summary =
            $"Copied {outcome.TableCount} table(s) holding {outcome.RowCount} row(s), {outcome.ViewCount} view(s) and "
            + $"{outcome.RoutineCount} stored instruction(s) into {targetDatabase}.";
        progress?.Report(outcome.Summary);
        return outcome;
    }

    private async Task<(string CharacterSet, string Collation)> ReadSchemaCharacterSetAsync(
        string database,
        CancellationToken cancellationToken)
    {
        var rows = await _gateway
            .QueryAsync(
                "information_schema",
                $"SELECT DEFAULT_CHARACTER_SET_NAME, DEFAULT_COLLATION_NAME FROM SCHEMATA "
                + $"WHERE SCHEMA_NAME = '{Literal(database)}';",
                cancellationToken)
            .ConfigureAwait(false);

        return rows.Count > 0 ? (rows[0][0], rows[0][1]) : ("utf8mb4", "utf8mb4_unicode_ci");
    }

    /// <summary>
    /// Reads one object's own definition, as the server renders it, so the copy carries exactly what the source
    /// carries.
    /// </summary>
    private async Task<string> ReadDefinitionAsync(
        string kind,
        string database,
        string name,
        CancellationToken cancellationToken)
    {
        var rows = await _gateway
            .QueryAsync(
                "information_schema",
                $"SHOW CREATE {kind} `{Quote(database)}`.`{Quote(name)}`;",
                cancellationToken)
            .ConfigureAwait(false);

        var ddl = rows
            .SelectMany(row => row)
            .FirstOrDefault(value => value.StartsWith("CREATE", StringComparison.OrdinalIgnoreCase));

        return ddl ?? throw new InvalidOperationException($"The server did not return a definition for {name}.");
    }

    /// <summary>
    /// Reads every view, routine or trigger of a database, with any mention of the source schema pointed at the
    /// copy: the server stores a view's tables fully qualified, so an unedited definition would read the source.
    /// </summary>
    private async Task<List<(string Name, string Definition)>> ReadProgrammableObjectsAsync(
        string kind,
        string sourceDatabase,
        string targetDatabase,
        CancellationToken cancellationToken)
    {
        var names = await _gateway
            .QueryAsync(
                "information_schema",
                kind switch
                {
                    "PROCEDURE" => $"SELECT ROUTINE_NAME FROM ROUTINES WHERE ROUTINE_SCHEMA = '{Literal(sourceDatabase)}' AND ROUTINE_TYPE = 'PROCEDURE' ORDER BY ROUTINE_NAME;",
                    "FUNCTION" => $"SELECT ROUTINE_NAME FROM ROUTINES WHERE ROUTINE_SCHEMA = '{Literal(sourceDatabase)}' AND ROUTINE_TYPE = 'FUNCTION' ORDER BY ROUTINE_NAME;",
                    "VIEW" => $"SELECT TABLE_NAME FROM VIEWS WHERE TABLE_SCHEMA = '{Literal(sourceDatabase)}' ORDER BY TABLE_NAME;",
                    _ => $"SELECT TRIGGER_NAME FROM TRIGGERS WHERE TRIGGER_SCHEMA = '{Literal(sourceDatabase)}' ORDER BY TRIGGER_NAME;"
                },
                cancellationToken)
            .ConfigureAwait(false);

        var objects = new List<(string Name, string Definition)>(names.Count);
        foreach (var row in names)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var name = row[0];
            var definition = await ReadDefinitionAsync(kind, sourceDatabase, name, cancellationToken).ConfigureAwait(false);
            objects.Add((name, PointAtCopy(definition, sourceDatabase, targetDatabase)));
        }

        return objects;
    }

    private static string PointAtCopy(string definition, string sourceDatabase, string targetDatabase) =>
        definition.Replace($"`{sourceDatabase}`", $"`{targetDatabase}`", StringComparison.OrdinalIgnoreCase);

    public async Task<BackupOutcome> CreateAsync(
        string database,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var backupName = BackupName(database);
        var outcome = new BackupOutcome
        {
            SourceDatabase = database,
            BackupDatabase = backupName
        };

        if (!await DatabaseExistsAsync(database, cancellationToken).ConfigureAwait(false))
        {
            outcome.Summary = $"{database} does not exist yet, so there was nothing to copy.";
            return outcome;
        }

        var tables = await ReadBaseTablesAsync(database, cancellationToken).ConfigureAwait(false);
        if (tables.Count == 0)
        {
            outcome.Summary = $"{database} holds no tables yet, so there was nothing to copy.";
            return outcome;
        }

        progress?.Report(
            $"Copying {tables.Count} table(s) of {database} into {backupName} before anything changes...");

        await _gateway
            .ExecuteWithoutDatabaseAsync(
                $"DROP DATABASE IF EXISTS `{Quote(backupName)}`; "
                + $"CREATE DATABASE `{Quote(backupName)}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;",
                cancellationToken)
            .ConfigureAwait(false);

        foreach (var table in tables)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report($"Copying {table}...");

            await _gateway
                .ExecuteStatementsAsync(
                    backupName,
                    [
                        $"CREATE TABLE `{Quote(backupName)}`.`{Quote(table)}` LIKE `{Quote(database)}`.`{Quote(table)}`;",
                        $"INSERT INTO `{Quote(backupName)}`.`{Quote(table)}` "
                        + $"SELECT * FROM `{Quote(database)}`.`{Quote(table)}`;"
                    ],
                    continueOnError: false,
                    onError: null,
                    disableForeignKeyChecks: true,
                    cancellationToken)
                .ConfigureAwait(false);

            // A copy nobody counted is a copy nobody can rely on: the usual failure is a silent truncation,
            // and this is the one place where finding it costs a query rather than an operator's data.
            var sourceRows = await CountRowsAsync(database, table, cancellationToken).ConfigureAwait(false);
            var copiedRows = await CountRowsAsync(backupName, table, cancellationToken).ConfigureAwait(false);

            if (sourceRows != copiedRows)
            {
                throw new InvalidOperationException(
                    $"The copy of '{table}' holds {copiedRows} of its {sourceRows} rows, so the backup is not "
                    + $"complete and {database} was left untouched.");
            }

            outcome.TableCount++;
            outcome.RowCount += copiedRows;
        }

        outcome.WasCreated = true;
        outcome.Summary =
            $"Copied {outcome.TableCount} table(s) holding {outcome.RowCount} row(s) into {backupName}.";
        progress?.Report(outcome.Summary);
        return outcome;
    }

    private async Task<bool> DatabaseExistsAsync(string database, CancellationToken cancellationToken)
    {
        var rows = await _gateway
            .QueryAsync(
                "information_schema",
                $"SELECT SCHEMA_NAME FROM SCHEMATA WHERE SCHEMA_NAME = '{Literal(database)}';",
                cancellationToken)
            .ConfigureAwait(false);

        return rows.Count > 0;
    }

    private async Task<List<string>> ReadBaseTablesAsync(string database, CancellationToken cancellationToken)
    {
        var rows = await _gateway
            .QueryAsync(
                "information_schema",
                $"SELECT TABLE_NAME FROM TABLES WHERE TABLE_SCHEMA = '{Literal(database)}' "
                + "AND TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME;",
                cancellationToken)
            .ConfigureAwait(false);

        return rows.Select(row => row[0]).ToList();
    }

    private async Task<long> CountRowsAsync(string database, string table, CancellationToken cancellationToken)
    {
        var rows = await _gateway
            .QueryAsync(
                database,
                $"SELECT COUNT(*) FROM `{Quote(database)}`.`{Quote(table)}`;",
                cancellationToken)
            .ConfigureAwait(false);

        return rows.Count > 0 && long.TryParse(rows[0][0], out var count) ? count : 0;
    }

    private static string Quote(string identifier) => identifier.Replace("`", "``", StringComparison.Ordinal);

    private static string Literal(string value) => value.Replace("'", "''", StringComparison.Ordinal);
}
