using MTM_Database_Deployment.Models;

namespace MTM_Database_Deployment.Services;

/// <summary>
/// Compares the SQL object tree against a live database.
///
/// The expected schema is materialised in a temporary scratch schema built from the same
/// table scripts that are deployed, so the comparison always uses real MySQL metadata
/// instead of a hand written DDL parser.
/// </summary>
public sealed class Service_SchemaComparer
{
    private readonly Service_MySqlGateway _gateway;
    private readonly Service_ObjectCatalog _catalog;

    public Service_SchemaComparer(Service_MySqlGateway gateway, Service_ObjectCatalog catalog)
    {
        _gateway = gateway;
        _catalog = catalog;
    }

    public async Task<Model_DeploymentPlan> BuildPlanAsync(
        string database,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var resolution = await _gateway.ResolveServerAsync(cancellationToken).ConfigureAwait(false);
        progress?.Report(resolution);

        var objects = _catalog.LoadObjects();
        var schemaObjects = objects.Where(item => item.Kind == Enum_SqlObjectKind.Schema).ToList();
        if (schemaObjects.Count == 0)
        {
            throw new InvalidOperationException($"No table scripts were found under {_catalog.SqlRoot}.");
        }

        var scratchDatabase = "mtm_expected_" + Guid.NewGuid().ToString("N")[..8];
        var changes = new List<Model_SchemaChange>();

        try
        {
            progress?.Report("Building expected schema from table scripts...");
            await _gateway.ExecuteWithoutDatabaseAsync(
                $"DROP DATABASE IF EXISTS `{scratchDatabase}`; CREATE DATABASE `{scratchDatabase}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;",
                cancellationToken).ConfigureAwait(false);

            foreach (var schemaObject in schemaObjects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var script = await File.ReadAllTextAsync(schemaObject.FullPath, cancellationToken).ConfigureAwait(false);
                try
                {
                    await _gateway.ExecuteStatementsAsync(
                        scratchDatabase,
                        Service_SqlScriptParser.SplitStatements(script),
                        continueOnError: false,
                        onError: null,
                        disableForeignKeyChecks: true,
                        cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException($"Table script {schemaObject.RelativePath} could not be evaluated: {exception.Message}", exception);
                }
            }

            progress?.Report("Reading expected schema metadata...");
            var expectedTables = await _gateway.ReadTableSnapshotsAsync(scratchDatabase, cancellationToken).ConfigureAwait(false);

            progress?.Report($"Reading {database} schema metadata...");
            var liveTables = await _gateway.ReadTableSnapshotsAsync(database, cancellationToken).ConfigureAwait(false);

            BuildTableChanges(expectedTables, liveTables, database, changes);
            BuildObjectChanges(objects, database, changes);

            return new Model_DeploymentPlan
            {
                Server = _gateway.ActiveServer,
                Port = _gateway.Port,
                Database = database,
                GeneratedAt = DateTimeOffset.Now,
                Changes = changes
            };
        }
        finally
        {
            try
            {
                await _gateway.ExecuteWithoutDatabaseAsync($"DROP DATABASE IF EXISTS `{scratchDatabase}`;", CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Scratch cleanup is best effort.
            }
        }
    }

    private static void BuildTableChanges(
        Dictionary<string, Model_TableSnapshot> expected,
        Dictionary<string, Model_TableSnapshot> live,
        string database,
        List<Model_SchemaChange> changes)
    {
        foreach (var (tableName, expectedTable) in expected.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (!live.TryGetValue(tableName, out var liveTable))
            {
                changes.Add(new Model_SchemaChange
                {
                    Kind = Enum_ChangeKind.CreateTable,
                    Target = tableName,
                    Detail = "table does not exist yet"
                });
                continue;
            }

            foreach (var column in expectedTable.Columns)
            {
                var liveColumn = liveTable.FindColumn(column.Name);
                var definition = column.ToColumnDefinitionSql();

                if (liveColumn is null)
                {
                    changes.Add(new Model_SchemaChange
                    {
                        Kind = Enum_ChangeKind.AddColumn,
                        Target = $"{tableName}.{column.Name}",
                        Detail = column.DataType,
                        Sql = $"ALTER TABLE `{database}`.`{tableName}` ADD COLUMN `{column.Name}` {definition};"
                    });
                    continue;
                }

                if (!string.Equals(liveColumn.Signature, column.Signature, StringComparison.Ordinal))
                {
                    changes.Add(new Model_SchemaChange
                    {
                        Kind = Enum_ChangeKind.AlterColumn,
                        Target = $"{tableName}.{column.Name}",
                        Detail = $"live: {liveColumn.DataType} -> expected: {column.DataType}",
                        Sql = $"ALTER TABLE `{database}`.`{tableName}` MODIFY COLUMN `{column.Name}` {definition};"
                    });
                }
            }

            foreach (var index in expectedTable.Indexes)
            {
                var liveIndex = liveTable.Indexes.FirstOrDefault(item => string.Equals(item.Name, index.Name, StringComparison.OrdinalIgnoreCase));
                if (liveIndex is null)
                {
                    changes.Add(new Model_SchemaChange
                    {
                        Kind = Enum_ChangeKind.AddIndex,
                        Target = $"{tableName}.{index.Name}",
                        Detail = (index.IsUnique ? "unique " : string.Empty) + string.Join(", ", index.Columns),
                        Sql = index.ToCreateSql(tableName, database)
                    });
                }
                else if (!string.Equals(liveIndex.Signature, index.Signature, StringComparison.Ordinal))
                {
                    changes.Add(new Model_SchemaChange
                    {
                        Kind = Enum_ChangeKind.AddIndex,
                        Target = $"{tableName}.{index.Name}",
                        Detail = "definition differs",
                        Sql = $"ALTER TABLE `{database}`.`{tableName}` DROP INDEX `{index.Name}`;" + Environment.NewLine + index.ToCreateSql(tableName, database)
                    });
                }
            }

            foreach (var foreignKey in expectedTable.ForeignKeys)
            {
                var liveForeignKey = liveTable.ForeignKeys.FirstOrDefault(item => string.Equals(item.Name, foreignKey.Name, StringComparison.OrdinalIgnoreCase));
                if (liveForeignKey is null)
                {
                    changes.Add(new Model_SchemaChange
                    {
                        Kind = Enum_ChangeKind.AddForeignKey,
                        Target = $"{tableName}.{foreignKey.Name}",
                        Detail = $"references {foreignKey.ReferencedTable}",
                        Sql = foreignKey.ToCreateSql(tableName, database)
                    });
                }
                else if (!string.Equals(liveForeignKey.Signature, foreignKey.Signature, StringComparison.Ordinal))
                {
                    changes.Add(new Model_SchemaChange
                    {
                        Kind = Enum_ChangeKind.AddForeignKey,
                        Target = $"{tableName}.{foreignKey.Name}",
                        Detail = "definition differs",
                        Sql = $"ALTER TABLE `{database}`.`{tableName}` DROP FOREIGN KEY `{foreignKey.Name}`;" + Environment.NewLine + foreignKey.ToCreateSql(tableName, database)
                    });
                }
            }

            foreach (var liveColumn in liveTable.Columns)
            {
                if (expectedTable.FindColumn(liveColumn.Name) is null)
                {
                    var blocksInserts = !liveColumn.IsNullable && string.IsNullOrWhiteSpace(liveColumn.DefaultValue);
                    changes.Add(new Model_SchemaChange
                    {
                        Kind = Enum_ChangeKind.ExtraColumn,
                        Target = $"{tableName}.{liveColumn.Name}",
                        Detail = blocksInserts
                            ? $"{liveColumn.DataType} exists in the database only; no default, so inserts that omit it fail"
                            : $"{liveColumn.DataType} exists in the database only",
                        Sql = $"ALTER TABLE `{database}`.`{tableName}` DROP COLUMN `{liveColumn.Name}`;",
                        IsBlocking = blocksInserts
                    });
                }
            }
        }

        foreach (var tableName in live.Keys.Where(name => !expected.ContainsKey(name)).OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
        {
            changes.Add(new Model_SchemaChange
            {
                Kind = Enum_ChangeKind.ExtraTable,
                Target = tableName,
                Detail = "table exists in the database only",
                Sql = $"DROP TABLE `{database}`.`{tableName}`;"
            });
        }
    }

    private static void BuildObjectChanges(
        IReadOnlyList<Model_SqlObject> objects,
        string database,
        List<Model_SchemaChange> changes)
    {
        foreach (var view in objects.Where(item => item.Kind == Enum_SqlObjectKind.View))
        {
            changes.Add(new Model_SchemaChange
            {
                Kind = Enum_ChangeKind.UpdateView,
                Target = view.RelativePath,
                Detail = "CREATE OR REPLACE VIEW",
                SourcePath = view.FullPath
            });
        }

        foreach (var procedure in objects.Where(item => item.Kind == Enum_SqlObjectKind.Procedure))
        {
            changes.Add(new Model_SchemaChange
            {
                Kind = Enum_ChangeKind.UpdateProcedure,
                Target = procedure.RelativePath,
                Detail = "DROP + CREATE PROCEDURE",
                SourcePath = procedure.FullPath
            });
        }

        foreach (var trigger in objects.Where(item => item.Kind == Enum_SqlObjectKind.Trigger))
        {
            changes.Add(new Model_SchemaChange
            {
                Kind = Enum_ChangeKind.UpdateTrigger,
                Target = trigger.RelativePath,
                Detail = "DROP + CREATE TRIGGER",
                SourcePath = trigger.FullPath
            });
        }

        foreach (var seed in objects.Where(item => item.Kind == Enum_SqlObjectKind.Seed))
        {
            changes.Add(new Model_SchemaChange
            {
                Kind = Enum_ChangeKind.RunSeed,
                Target = seed.RelativePath,
                Detail = "idempotent seed data",
                SourcePath = seed.FullPath
            });
        }
    }
}
