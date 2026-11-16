using System.Diagnostics;
using MTM_Database_Deployment.Models;

namespace MTM_Database_Deployment.Services;

/// <summary>
/// Applies a deployment plan. Two modes are supported:
///  - UpdateInPlace: never drops data; adds missing tables/columns/indexes and refreshes code objects.
///  - FreshRebuild: drops and recreates the database, then deploys every object in order.
/// </summary>
public sealed class Service_DeploymentEngine
{
    private readonly Service_MySqlGateway _gateway;
    private readonly Service_ObjectCatalog _catalog;

    public Service_DeploymentEngine(Service_MySqlGateway gateway, Service_ObjectCatalog catalog)
    {
        _gateway = gateway;
        _catalog = catalog;
    }

    public async Task<Model_DeploymentResult> ApplyAsync(
        Model_DeploymentPlan plan,
        bool dropConfirmedExtras,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new Model_DeploymentResult();
        var objects = _catalog.LoadObjects();
        var scopedUse = false;

        await _gateway.ResolveServerAsync(cancellationToken).ConfigureAwait(false);
        progress?.Report(_gateway.ResolutionMessage);

        // 1. Create any missing tables first so later alterations can reference them.
        var schemaObjects = objects.Where(item => item.Kind == Enum_SqlObjectKind.Schema).ToList();
        foreach (var schemaObject in schemaObjects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report($"Ensuring table: {schemaObject.RelativePath}");

            var script = await File.ReadAllTextAsync(schemaObject.FullPath, cancellationToken).ConfigureAwait(false);
            try
            {
                await _gateway.ExecuteStatementsAsync(
                    plan.Database,
                    ScopeToDatabase(
                        Service_SqlScriptParser.SplitStatements(script), plan.Database, ref scopedUse),
                    continueOnError: false,
                    onError: null,
                    disableForeignKeyChecks: true,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                result.MarkFailed($"{schemaObject.RelativePath}: {exception.Message}");
                throw;
            }
        }

        result.MarkApplied($"{schemaObjects.Count} table script(s) ensured.");

        // 2. Apply column, index, and foreign key changes computed by the comparer.
        foreach (var change in plan.Changes.Where(item => item.Kind is Enum_ChangeKind.AddColumn
                     or Enum_ChangeKind.AlterColumn or Enum_ChangeKind.AddIndex or Enum_ChangeKind.AddForeignKey))
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report($"{change.KindLabel}: {change.Target}");

            var failed = false;
            foreach (var statement in Service_SqlScriptParser.SplitStatements(change.Sql ?? string.Empty))
            {
                try
                {
                    await _gateway.ExecuteAsync(plan.Database, statement, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception) when (IsBenign(exception))
                {
                    result.MarkBenignSkip($"{change.KindLabel} {change.Target}");
                }
                catch (Exception exception)
                {
                    failed = true;
                    result.MarkFailed($"{change.KindLabel} {change.Target}: {exception.Message}");
                }
            }

            if (!failed)
            {
                result.MarkApplied($"{change.KindLabel}: {change.Target}");
            }
        }

        // 2b. Blocking removals run before anything writes. A database-only NOT NULL column with no
        // default fails every insert that omits it, so a confirmed cleanup must land in this run
        // rather than after the seeds it would otherwise break.
        await ApplyRemovalsAsync(plan, dropConfirmedExtras, blocking: true, progress, result, cancellationToken)
            .ConfigureAwait(false);

        // A column that could not be added or altered invalidates every view, procedure, and seed
        // that depends on it. Stopping here keeps the real error visible instead of burying it under
        // a cascade of downstream failures.
        if (result.FailedCount > 0)
        {
            result.AddMessage(
                $"Stopped after schema reconciliation: {result.FailedCount} change(s) failed, so views, procedures, "
                + "triggers, seeds, and the remaining removals were not run.");
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            return result;
        }

        // 3. Refresh views, procedures, and triggers.
        foreach (var kind in new[]
                 {
                     Enum_SqlObjectKind.View,
                     Enum_SqlObjectKind.Procedure,
                     Enum_SqlObjectKind.Function,
                     Enum_SqlObjectKind.Trigger
                 })
        {
            foreach (var sqlObject in objects.Where(item => item.Kind == kind))
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report($"Deploying: {sqlObject.RelativePath}");

                var script = await File.ReadAllTextAsync(sqlObject.FullPath, cancellationToken).ConfigureAwait(false);
                var errors = new List<string>();

                await _gateway.ExecuteStatementsAsync(
                    plan.Database,
                    ScopeToDatabase(
                        Service_SqlScriptParser.SplitStatements(script), plan.Database, ref scopedUse),
                    continueOnError: true,
                    onError: errors.Add,
                    disableForeignKeyChecks: false,
                    cancellationToken).ConfigureAwait(false);

                foreach (var error in errors)
                {
                    if (IsBenignMessage(error))
                    {
                        result.MarkBenignSkip(sqlObject.RelativePath);
                    }
                    else
                    {
                        result.MarkFailed($"{sqlObject.RelativePath}: {error}");
                    }
                }

                if (errors.Count == 0)
                {
                    result.MarkApplied(sqlObject.RelativePath);
                }
            }
        }

        // 4. Seed data (idempotent; duplicates are expected and reported as already applied).
        foreach (var seed in objects.Where(item => item.Kind == Enum_SqlObjectKind.Seed))
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report($"Seeding: {seed.RelativePath}");

            var script = await File.ReadAllTextAsync(seed.FullPath, cancellationToken).ConfigureAwait(false);
            var errors = new List<string>();

            await _gateway.ExecuteStatementsAsync(
                plan.Database,
                ScopeToDatabase(Service_SqlScriptParser.SplitStatements(script), plan.Database, ref scopedUse),
                continueOnError: true,
                onError: errors.Add,
                disableForeignKeyChecks: false,
                cancellationToken).ConfigureAwait(false);

            foreach (var error in errors)
            {
                if (IsBenignMessage(error))
                {
                    result.MarkBenignSkip(seed.RelativePath);
                }
                else
                {
                    result.MarkFailed($"{seed.RelativePath}: {error}");
                }
            }

            if (errors.Count == 0)
            {
                result.MarkApplied(seed.RelativePath);
            }
        }

        if (scopedUse)
        {
            result.AddMessage(
                $"Scripts that name a database were pointed at '{plan.Database}', the database chosen on the first "
                + "page, so nothing was written anywhere else.");
        }

        // 5. Remaining removals last, so a failed deploy never destroys anything early.
        await ApplyRemovalsAsync(plan, dropConfirmedExtras, blocking: false, progress, result, cancellationToken)
            .ConfigureAwait(false);

        stopwatch.Stop();
        result.Duration = stopwatch.Elapsed;
        return result;
    }

    /// <summary>
    /// Points every <c>USE</c> statement in a script at the database the operator chose.
    /// </summary>
    /// <remarks>
    /// The repository's scripts open with <c>USE mtm_waitlist;</c> so they can be pasted into a client. In a
    /// deployment that line would override the picker: choosing the practice copy and silently writing to the live
    /// store is the one mistake this tool must not make, so the statement is kept but re-pointed.
    /// </remarks>
    public static List<string> ScopeToDatabase(
        IReadOnlyList<string> statements,
        string database,
        ref bool scoped)
    {
        var result = new List<string>(statements.Count);
        foreach (var statement in statements)
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                statement,
                @"^USE\s+`?([^`\s;]+)`?\s*$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (match.Success)
            {
                if (!string.Equals(match.Groups[1].Value, database, StringComparison.OrdinalIgnoreCase))
                {
                    scoped = true;
                }

                result.Add($"USE `{database}`");
                continue;
            }

            result.Add(statement);
        }

        return result;
    }

    private async Task ApplyRemovalsAsync(
        Model_DeploymentPlan plan,
        bool dropConfirmedExtras,
        bool blocking,
        IProgress<string>? progress,
        Model_DeploymentResult result,
        CancellationToken cancellationToken)
    {
        if (!dropConfirmedExtras)
        {
            return;
        }

        foreach (var change in plan.Changes.Where(item =>
                     item.IsRemoval && item.IsBlocking == blocking && !string.IsNullOrWhiteSpace(item.Sql)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report($"Removing: {change.Target}");

            try
            {
                await _gateway.ExecuteAsync(plan.Database, change.Sql!, cancellationToken).ConfigureAwait(false);
                result.MarkApplied($"Removed {change.KindLabel}: {change.Target}");
            }
            catch (Exception exception)
            {
                result.MarkFailed($"Remove {change.Target}: {exception.Message}");
            }
        }
    }

    public async Task<Model_DeploymentResult> RebuildAsync(
        string database,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new Model_DeploymentResult();

        await _gateway.ResolveServerAsync(cancellationToken).ConfigureAwait(false);
        progress?.Report(_gateway.ResolutionMessage);
        progress?.Report($"Dropping and recreating {database}...");

        await _gateway.ExecuteWithoutDatabaseAsync(
            $"DROP DATABASE IF EXISTS `{database}`; CREATE DATABASE `{database}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;",
            cancellationToken).ConfigureAwait(false);

        var objects = _catalog.LoadObjects();
        foreach (var kind in new[] { Enum_SqlObjectKind.Schema, Enum_SqlObjectKind.View, Enum_SqlObjectKind.Procedure, Enum_SqlObjectKind.Trigger, Enum_SqlObjectKind.Seed })
        {
            foreach (var sqlObject in objects.Where(item => item.Kind == kind))
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report($"Deploying: {sqlObject.RelativePath}");

                var script = await File.ReadAllTextAsync(sqlObject.FullPath, cancellationToken).ConfigureAwait(false);
                var errors = new List<string>();

                await _gateway.ExecuteStatementsAsync(
                    database,
                    Service_SqlScriptParser.SplitStatements(script),
                    continueOnError: true,
                    onError: errors.Add,
                    disableForeignKeyChecks: kind == Enum_SqlObjectKind.Schema,
                    cancellationToken).ConfigureAwait(false);

                foreach (var error in errors)
                {
                    if (IsBenignMessage(error))
                    {
                        result.MarkBenignSkip(sqlObject.RelativePath);
                    }
                    else
                    {
                        result.MarkFailed($"{sqlObject.RelativePath}: {error}");
                    }
                }

                if (errors.Count == 0)
                {
                    result.MarkApplied(sqlObject.RelativePath);
                }
            }
        }

        stopwatch.Stop();
        result.Duration = stopwatch.Elapsed;
        return result;
    }

    private static bool IsBenign(Exception exception) => IsBenignMessage(exception.Message);

    private static bool IsBenignMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var normalized = message.ToLowerInvariant();
        string[] benignPatterns =
        {
            "already exists",
            "duplicate column name",
            "duplicate key name",
            "duplicate foreign key constraint name",
            "multiple primary key defined",
            "check that column/key exists",
            "duplicate entry",
            "can't drop"
        };

        return benignPatterns.Any(normalized.Contains);
    }
}
