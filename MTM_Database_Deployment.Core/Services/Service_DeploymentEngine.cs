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
    private readonly DatabaseBackup _backup;

    public Service_DeploymentEngine(
        Service_MySqlGateway gateway,
        Service_ObjectCatalog catalog,
        DatabaseBackup backup)
    {
        _gateway = gateway;
        _catalog = catalog;
        _backup = backup;
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

        // 0. Copy what is there before the first statement runs, so an operator's own rows survive a run that
        //    turns out to be aimed at the wrong database, and a column removal can be undone by hand.
        result.AddMessage(await TakeBackupAsync(plan.Database, progress, cancellationToken).ConfigureAwait(false));

        // Decided from the store as it is now, before this run creates anything: a store that already holds tables
        // and has no seed record was seeded by an earlier installer, so its seeds are recorded rather than re-run.
        var seeds = objects.Where(item => item.Kind == Enum_SqlObjectKind.Seed).ToList();
        var (alreadySeeded, needsSeedRecord) = await AppliedSeedLedger
            .ResolveAsync(_gateway, plan.Database, seeds.Select(seed => seed.RelativePath).ToList(), cancellationToken)
            .ConfigureAwait(false);

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

        // 2c. Descriptions, and the guarded column and table additions a store that already exists still needs.
        //     This runs before the routines are replaced, because a guarded block in it can add a column a routine
        //     in this same deployment reads.
        await ApplyDescriptionsAsync(plan.Database, progress, result, cancellationToken).ConfigureAwait(false);

        // 3. Refresh functions, then the objects that may call them: a view that ranks by a function, a procedure
        //    that calls one, a trigger that writes through one. Functions come first because a fresh deployment
        //    has none until they are created.
        foreach (var kind in new[]
                 {
                     Enum_SqlObjectKind.Function,
                     Enum_SqlObjectKind.View,
                     Enum_SqlObjectKind.Procedure,
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

        // 4. Seed data. A seed runs at most once per database: the ledger says what this store has already
        //    received, so a safe update cannot put a shipped default back over an operator's own value.
        if (seeds.Count > 0)
        {
            await AppliedSeedLedger.EnsureAsync(_gateway, plan.Database, cancellationToken).ConfigureAwait(false);
        }

        if (needsSeedRecord)
        {
            // Nothing is run: this store was seeded before the tool kept a record, so every seed it ships is taken as
            // already in place and the record is written now.
            foreach (var seed in seeds)
            {
                await RecordSeedAsync(plan.Database, seed.RelativePath, result, cancellationToken).ConfigureAwait(false);
            }

            result.AddMessage(
                $"{seeds.Count} seed script(s) were recorded as already applied. This database was set up before the "
                + "tool kept a record, so nothing was run over it.");
        }

        foreach (var seed in seeds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (alreadySeeded.Contains(seed.RelativePath))
            {
                progress?.Report($"Already seeded: {seed.RelativePath}");
                result.MarkSkipped($"{seed.RelativePath} - seeded by an earlier run, so it was left alone");
                continue;
            }

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
                await RecordSeedAsync(plan.Database, seed.RelativePath, result, cancellationToken).ConfigureAwait(false);
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
    /// <para>
    /// The repository's scripts open with <c>USE mtm_waitlist;</c> so they can be pasted into a client. In a
    /// deployment that line would override the picker: choosing the practice copy and silently writing to the live
    /// store is the one mistake this tool must not make, so the statement is kept but re-pointed.
    /// </para>
    /// <para>
    /// The line arrives here underneath the script's header comment — the splitter keeps comments inside the
    /// statement they precede — so the comment block is stepped over before the statement is recognised. Without
    /// that, every script with a header keeps its own <c>USE</c> and the deployment writes to the live store the
    /// moment one is named there.
    /// </para>
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
                SkipLeadingComments(statement),
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

    /// <summary>
    /// Returns a statement with the comment block and whitespace in front of its first keyword removed.
    /// </summary>
    private static string SkipLeadingComments(string statement)
    {
        var index = 0;

        while (index < statement.Length)
        {
            if (char.IsWhiteSpace(statement[index]))
            {
                index++;
                continue;
            }

            // MySQL only reads '--' as a comment when whitespace follows it; otherwise it is a minus sign.
            if (statement[index] == '-'
                && index + 1 < statement.Length
                && statement[index + 1] == '-'
                && (index + 2 >= statement.Length || char.IsWhiteSpace(statement[index + 2])))
            {
                var lineEnd = statement.IndexOf('\n', index);
                index = lineEnd < 0 ? statement.Length : lineEnd + 1;
                continue;
            }

            if (statement[index] == '/' && index + 1 < statement.Length && statement[index + 1] == '*')
            {
                var blockEnd = statement.IndexOf("*/", index + 2, StringComparison.Ordinal);
                index = blockEnd < 0 ? statement.Length : blockEnd + 2;
                continue;
            }

            break;
        }

        return index == 0 ? statement : statement[index..];
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

        // The rebuild erases the database, so the copy is taken first — and if it cannot be taken, nothing is
        // dropped. A rebuild with no copy is the one path that cannot be walked back at all.
        result.AddMessage(await TakeBackupAsync(database, progress, cancellationToken).ConfigureAwait(false));

        await _gateway.ExecuteWithoutDatabaseAsync(
            $"DROP DATABASE IF EXISTS `{database}`; CREATE DATABASE `{database}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;",
            cancellationToken).ConfigureAwait(false);

        var objects = _catalog.LoadObjects();
        var scopedUse = false;
        var seeds = objects.Where(item => item.Kind == Enum_SqlObjectKind.Seed).ToList();

        // A rebuild has just created the database, so nothing has been seeded into it: the ledger is read for the
        // record only, and no seed is taken as already applied.
        var alreadySeeded = seeds.Count == 0
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : await AppliedSeedLedger.ReadAsync(_gateway, database, cancellationToken).ConfigureAwait(false);

        if (seeds.Count > 0)
        {
            await AppliedSeedLedger.EnsureAsync(_gateway, database, cancellationToken).ConfigureAwait(false);
        }

        foreach (var kind in new[]
                 {
                     Enum_SqlObjectKind.Schema,
                     Enum_SqlObjectKind.Function,
                     Enum_SqlObjectKind.View,
                     Enum_SqlObjectKind.Procedure,
                     Enum_SqlObjectKind.Trigger,
                     Enum_SqlObjectKind.Seed
                 })
        {
            foreach (var sqlObject in objects.Where(item => item.Kind == kind))
            {
                cancellationToken.ThrowIfCancellationRequested();

                // A rebuild that reuses an existing database name — the practice copy — keeps the ledger trimmed to
                // what survives the drop, and a seed already recorded must not be applied a second time.
                if (kind == Enum_SqlObjectKind.Seed && alreadySeeded.Contains(sqlObject.RelativePath))
                {
                    progress?.Report($"Already seeded: {sqlObject.RelativePath}");
                    result.MarkSkipped($"{sqlObject.RelativePath} - seeded by an earlier run, so it was left alone");
                    continue;
                }

                progress?.Report($"Deploying: {sqlObject.RelativePath}");

                var script = await File.ReadAllTextAsync(sqlObject.FullPath, cancellationToken).ConfigureAwait(false);
                var errors = new List<string>();

                // A script's own USE line names the live store so it can be pasted into a client. Left alone here it
                // would send this rebuild's procedures and seeds back to mtm_waitlist while the tables it just
                // created sit in the database the operator chose.
                await _gateway.ExecuteStatementsAsync(
                    database,
                    ScopeToDatabase(
                        Service_SqlScriptParser.SplitStatements(script), database, ref scopedUse),
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

                    if (kind == Enum_SqlObjectKind.Seed)
                    {
                        await RecordSeedAsync(database, sqlObject.RelativePath, result, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
            }

            // The description file carries the wording a column is documented with, plus the guarded column and
            // table blocks a store that already existed still needs. A rebuild creates the shape from the table
            // scripts, so this is what makes a fresh store read the same as one that has been updated in place.
            if (kind == Enum_SqlObjectKind.Schema)
            {
                scopedUse |= await ApplyDescriptionsAsync(database, progress, result, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        if (scopedUse)
        {
            result.AddMessage(
                $"Scripts that name a database were pointed at '{database}', the database chosen on the first "
                + "page, so nothing was written anywhere else.");
        }

        stopwatch.Stop();
        result.Duration = stopwatch.Elapsed;
        return result;
    }

    /// <summary>
    /// Applies the repository's maintained description file, which also carries the guarded column and table
    /// additions a store that already exists still needs.
    /// </summary>
    /// <remarks>
    /// Without this step a deployment produces the right shape and the wrong documentation: 230 columns come out
    /// with the wording the table script was written with, while the store that has been updated in place carries
    /// the wording this file last set. The file is idempotent by construction — every block asks
    /// <c>information_schema</c> whether the change is needed and selects a no-op message when it is not — so it is
    /// applied on every run rather than tracked as a one-off.
    /// </remarks>
    private async Task<bool> ApplyDescriptionsAsync(
        string database,
        IProgress<string>? progress,
        Model_DeploymentResult result,
        CancellationToken cancellationToken)
    {
        var path = Service_ObjectCatalog.DescriptionsScriptPath(_catalog.SqlRoot);
        if (!File.Exists(path))
        {
            return false;
        }

        progress?.Report("Applying the column and table descriptions...");
        var script = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        var errors = new List<string>();
        var scopedUse = false;

        await _gateway.ExecuteStatementsAsync(
            database,
            ScopeToDatabase(Service_SqlScriptParser.SplitStatements(script), database, ref scopedUse),
            continueOnError: true,
            onError: errors.Add,
            disableForeignKeyChecks: true,
            cancellationToken).ConfigureAwait(false);

        foreach (var error in errors)
        {
            result.MarkFailed($"Descriptions: {error}");
        }

        if (errors.Count == 0)
        {
            result.MarkApplied("Column and table descriptions applied.");
        }

        return scopedUse;
    }

    /// <summary>
    /// Records a seed the store has now received. A failure to record is reported rather than thrown: the seed
    /// itself worked, and the worst outcome of a missing line is that the next run seeds again.
    /// </summary>
    private async Task RecordSeedAsync(
        string database,
        string seedName,
        Model_DeploymentResult result,
        CancellationToken cancellationToken)
    {
        try
        {
            await AppliedSeedLedger.RecordAsync(_gateway, database, seedName, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            result.AddMessage(
                $"'{seedName}' was applied but could not be recorded as applied ({exception.Message}), so a later "
                + "run will apply it again.");
        }
    }

    private async Task<string> TakeBackupAsync(        string database,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        try
        {
            var outcome = await _backup.CreateAsync(database, progress, cancellationToken).ConfigureAwait(false);
            return outcome.Summary;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new InvalidOperationException(
                $"A copy of {database} could not be made, so nothing was changed. {exception.Message}",
                exception);
        }
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
