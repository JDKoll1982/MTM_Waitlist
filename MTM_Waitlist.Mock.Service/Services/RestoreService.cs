using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;
using MTM_Waitlist.Mock.Service.Models;

namespace MTM_Waitlist.Mock.Service.Services;

/// <summary>
/// Host-only, confirmation-gated full replacement of one store from one backup artifact.
/// </summary>
/// <remarks>
/// <para>
/// <b>There is no network path here.</b> This type is reachable only from the service's own UI on the
/// database host: it is not referenced by <c>ServiceApiEndpoints</c>, and the API host is deliberately
/// able to start with this service unregistered (FR-010/FR-023,
/// <c>contracts/mock-service-http-api.md</c> §5).
/// </para>
/// <para>
/// <b>Nothing happens without an explicit confirmation.</b> <see cref="RequestRestore"/> only records the
/// intent; <see cref="ConfirmAndRestoreAsync"/> performs the sequence. An unconfirmed request changes
/// nothing, and even a confirmed one takes no destructive step before the safety snapshot exists.
/// </para>
/// <para>
/// <b>A safety snapshot comes first.</b> The sequence is: back up the current state → drop and recreate the
/// store (utf8mb4) → reload the artifact without <c>--force</c> so a real error stops the restore → verify
/// the store came back with its tables → record the outcome. A failed restore names its safety snapshot so
/// the operator has a recovery path (research.md R8, data-model.md §7).
/// </para>
/// <para>
/// <b>No statement text lives here.</b> The two SQL steps are reviewed artifacts under
/// <c>Database/Mock.Service/Restore</c> that are read, have the store's database name substituted, and are
/// streamed to the client on stdin (FR-015, SC-013). <c>replace_database.sql</c> is the one step of this
/// operation that cannot be a stored procedure — MySQL rejects <c>DROP</c>/<c>CREATE DATABASE</c> inside a
/// routine, and such a routine would live in the database being dropped. That deviation is recorded in
/// <c>Database/Mock.Service/Restore/README.md</c> and in the feature plan's Complexity Tracking.
/// </para>
/// <para>
/// The password is passed only through the MySQL option file, never on a command line (FR-026).
/// </para>
/// </remarks>
public sealed class RestoreService
{
    /// <summary>Placeholder in a restore script that is replaced with the store's database name.</summary>
    private const string DatabasePlaceholder = "{{database}}";

    /// <summary>Folder holding the reviewed restore scripts, relative to the service's base directory.</summary>
    private static readonly string s_restoreScriptFolder = Path.Combine("Database", "Mock.Service", "Restore");

    /// <summary>Step 2's script: replace the store wholesale, in utf8mb4.</summary>
    private const string ReplaceDatabaseScriptName = "replace_database.sql";

    /// <summary>Step 4's script: report the store's base-table count.</summary>
    private const string VerifyRestoreScriptName = "verify_restore.sql";

    private static readonly TimeSpan s_restoreTimeout = TimeSpan.FromHours(4);

    private readonly BackupEngine _backupEngine;
    private readonly BackupArtifactStore _artifactStore;
    private readonly ILogger<RestoreService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly Func<Models.ServiceConfiguration> _configurationAccessor;

    /// <summary>Creates the restore service.</summary>
    /// <param name="backupEngine">Takes the safety snapshot and locates the MySQL client tools.</param>
    /// <param name="artifactStore">Records the outcome and resolves the safety snapshot's identity.</param>
    /// <param name="configurationAccessor">Reads the live configuration, including the option-file path.</param>
    /// <param name="logger">Logger; credential material is never passed to it.</param>
    /// <param name="timeProvider">Time source for the recorded timestamps.</param>
    public RestoreService(
        BackupEngine backupEngine,
        BackupArtifactStore artifactStore,
        Func<Models.ServiceConfiguration> configurationAccessor,
        ILogger<RestoreService> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(backupEngine);
        ArgumentNullException.ThrowIfNull(artifactStore);
        ArgumentNullException.ThrowIfNull(configurationAccessor);
        ArgumentNullException.ThrowIfNull(logger);

        _backupEngine = backupEngine;
        _artifactStore = artifactStore;
        _configurationAccessor = configurationAccessor;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Records a restore request without changing anything.
    /// </summary>
    /// <param name="artifact">The artifact the operator selected.</param>
    /// <returns>An unconfirmed outcome, which can be handed to <see cref="ConfirmAndRestoreAsync"/>.</returns>
    public RestoreOutcome RequestRestore(BackupArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        return new RestoreOutcome
        {
            Store = artifact.Store,
            ArtifactId = artifact.ArtifactId,
            RequestedUtc = _timeProvider.GetUtcNow().UtcDateTime,
            Outcome = RestoreOutcomeKind.NotConfirmed
        };
    }

    /// <summary>
    /// Performs a confirmed, verified full replacement of the requested store.
    /// </summary>
    /// <param name="request">The outcome returned by <see cref="RequestRestore"/>.</param>
    /// <param name="artifact">The artifact to restore from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The recorded outcome, including the safety snapshot on failure.</returns>
    /// <exception cref="InvalidOperationException">
    /// The artifact is not the one that was requested, or it no longer exists on disk.
    /// </exception>
    public async Task<RestoreOutcome> ConfirmAndRestoreAsync(
        RestoreOutcome request,
        BackupArtifact artifact,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(artifact);

        if (request.ArtifactId != artifact.ArtifactId)
        {
            throw new InvalidOperationException("The confirmed artifact is not the artifact that was requested.");
        }

        if (!File.Exists(artifact.FilePath))
        {
            throw new InvalidOperationException($"The selected backup artifact no longer exists: '{artifact.FilePath}'.");
        }

        var database = artifact.Store.ToDatabaseName();
        var confirmedUtc = _timeProvider.GetUtcNow().UtcDateTime;

        _logger.LogWarning(
            "Restore confirmed for {Database} from {ArtifactPath}.",
            database,
            artifact.FilePath);

        // 1. Safety snapshot first: without it a failed reload has no recovery path (FR-010).
        var safetyRun = await _backupEngine
            .RunAsync(artifact.Store, isSafetySnapshot: true, cancellationToken)
            .ConfigureAwait(false);

        var safetySnapshotArtifactId = _artifactStore
            .GetArtifacts(artifact.Store)
            .FirstOrDefault(candidate => candidate.IsSafetySnapshot && candidate.FilePath == safetyRun.ArtifactPath)
            ?.ArtifactId;

        if (safetyRun.Outcome != BackupRunOutcome.Succeeded)
        {
            // Never destroy a store without a recovery point, even when the operator asked to.
            _logger.LogError(
                "Restore aborted for {Database}: the pre-restore safety snapshot could not be taken ({Outcome}).",
                database,
                safetyRun.Outcome);

            return request with
            {
                ConfirmedUtc = confirmedUtc,
                FinishedUtc = _timeProvider.GetUtcNow().UtcDateTime,
                SafetySnapshotArtifactId = safetySnapshotArtifactId,
                Outcome = RestoreOutcomeKind.FailedReload,
                VerificationSummary =
                    $"The pre-restore safety snapshot could not be taken ({safetyRun.Outcome}), so nothing was changed."
            };
        }

        var mysqlPath = ResolveClientTool("mysql.exe");
        if (mysqlPath is null)
        {
            _logger.LogError("Restore aborted: the mysql client could not be located.");

            return request with
            {
                ConfirmedUtc = confirmedUtc,
                FinishedUtc = _timeProvider.GetUtcNow().UtcDateTime,
                SafetySnapshotArtifactId = safetySnapshotArtifactId,
                Outcome = RestoreOutcomeKind.FailedReload,
                VerificationSummary =
                    $"The mysql client could not be found, so '{database}' was not replaced. " +
                    $"Safety snapshot: artifact {safetySnapshotArtifactId}."
            };
        }

        // 2. Full replacement: drop and recreate the store explicitly, in utf8mb4. The statements come from
        //    the reviewed artifact, not from this file (constitution III, FR-015).
        string replaceScriptPath;

        try
        {
            replaceScriptPath = await MaterializeScriptAsync(ReplaceDatabaseScriptName, database, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (FileNotFoundException exception)
        {
            _logger.LogError(exception, "Restore aborted: the replace script is not deployed.");

            return request with
            {
                ConfirmedUtc = confirmedUtc,
                FinishedUtc = _timeProvider.GetUtcNow().UtcDateTime,
                SafetySnapshotArtifactId = safetySnapshotArtifactId,
                Outcome = RestoreOutcomeKind.FailedDrop,
                VerificationSummary =
                    $"The restore script '{ReplaceDatabaseScriptName}' is not deployed beside the service, " +
                    $"so '{database}' was not replaced. Safety snapshot: artifact {safetySnapshotArtifactId}."
            };
        }

        int replaceExitCode;
        try
        {
            replaceExitCode = (await RunClientAsync(
                mysqlPath,
                BuildConnectionArguments(),
                cancellationToken,
                standardInputPath: replaceScriptPath).ConfigureAwait(false)).ExitCode;
        }
        finally
        {
            TryDeleteScript(replaceScriptPath);
        }

        if (replaceExitCode != 0)
        {
            _logger.LogError(
                "Restore failed while replacing {Database}. ExitCode={ExitCode}.",
                database,
                replaceExitCode);

            return request with
            {
                ConfirmedUtc = confirmedUtc,
                FinishedUtc = _timeProvider.GetUtcNow().UtcDateTime,
                SafetySnapshotArtifactId = safetySnapshotArtifactId,
                Outcome = RestoreOutcomeKind.FailedDrop,
                VerificationSummary =
                    $"Dropping and recreating '{database}' failed (exit {replaceExitCode}). " +
                    $"Safety snapshot: artifact {safetySnapshotArtifactId}."
            };
        }

        // 3. Reload without --force, so a genuine error stops the restore instead of being skipped.
        var reloadExitCode = (await RunClientAsync(
            mysqlPath,
            BuildReloadArguments(database),
            cancellationToken,
            standardInputPath: artifact.FilePath).ConfigureAwait(false)).ExitCode;

        if (reloadExitCode != 0)
        {
            _logger.LogError(
                "Restore failed while reloading {Database}. ExitCode={ExitCode}.",
                database,
                reloadExitCode);

            return request with
            {
                ConfirmedUtc = confirmedUtc,
                FinishedUtc = _timeProvider.GetUtcNow().UtcDateTime,
                SafetySnapshotArtifactId = safetySnapshotArtifactId,
                Outcome = RestoreOutcomeKind.FailedReload,
                VerificationSummary =
                    $"Reloading '{database}' failed (exit {reloadExitCode}). " +
                    $"Recover from the safety snapshot: artifact {safetySnapshotArtifactId}."
            };
        }

        // 4. Verify the store came back: a reload that "succeeded" but left nothing behind is visible here.
        var verification = await BuildVerificationSummaryAsync(mysqlPath, database, artifact.SizeBytes, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogWarning("Restore completed for {Database}. {Verification}", database, verification);

        return request with
        {
            ConfirmedUtc = confirmedUtc,
            FinishedUtc = _timeProvider.GetUtcNow().UtcDateTime,
            SafetySnapshotArtifactId = safetySnapshotArtifactId,
            Outcome = RestoreOutcomeKind.Succeeded,
            VerificationSummary = verification
        };
    }

    /// <summary>
    /// Locates a MySQL client tool: beside the resolved <c>mysqldump</c> first, then on <c>PATH</c>.
    /// </summary>
    /// <param name="toolName">The executable to find, for example <c>mysql.exe</c>.</param>
    /// <returns>The absolute path, or <see langword="null"/> when it cannot be located.</returns>
    public string? ResolveClientTool(string toolName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);

        var dumpPath = _backupEngine.ResolveToolPath();
        if (dumpPath is not null)
        {
            var sibling = Path.Combine(Path.GetDirectoryName(dumpPath) ?? string.Empty, toolName);
            if (File.Exists(sibling))
            {
                return sibling;
            }
        }

        var pathVariable = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathVariable))
        {
            return null;
        }

        foreach (var directory in pathVariable.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var candidate = Path.Combine(directory.Trim(), toolName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            catch (ArgumentException)
            {
            }
        }

        return null;
    }

    /// <summary>
    /// The connection arguments every client invocation shares. The password travels only through the
    /// option-file reference, never as a command-line value (FR-026).
    /// </summary>
    private List<string> BuildConnectionArguments()
    {
        var connection = _configurationAccessor().MySqlConnection;
        var arguments = new List<string>();

        if (connection.PasswordFilePath is { Length: > 0 } passwordFile)
        {
            arguments.Add($"--defaults-extra-file={passwordFile}");
        }

        arguments.Add($"--host={connection.Server}");
        arguments.Add($"--port={connection.Port.ToString(CultureInfo.InvariantCulture)}");

        if (!string.IsNullOrWhiteSpace(connection.UserId))
        {
            arguments.Add($"--user={connection.UserId}");
        }

        return arguments;
    }

    private List<string> BuildReloadArguments(string database)
    {
        var arguments = BuildConnectionArguments();

        // No --force: a reload error must stop the restore rather than be skipped over.
        arguments.Add("--database");
        arguments.Add(database);

        return arguments;
    }

    /// <summary>
    /// Writes a reviewed restore script with the store's database name substituted, and returns the
    /// temporary file's path. The caller deletes it.
    /// </summary>
    /// <param name="scriptName">File name of the script under <c>Database/Mock.Service/Restore</c>.</param>
    /// <param name="database">The store's database name, taken from the store enum.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Absolute path of the materialised script.</returns>
    /// <exception cref="FileNotFoundException">The script is not deployed with the service.</exception>
    /// <remarks>
    /// The statement text stays in the artifact: this method only substitutes a store name that comes from
    /// <c>BackupStore.ToDatabaseName()</c>, never from operator input.
    /// </remarks>
    private static async Task<string> MaterializeScriptAsync(
        string scriptName,
        string database,
        CancellationToken cancellationToken)
    {
        var scriptPath = Path.Combine(AppContext.BaseDirectory, s_restoreScriptFolder, scriptName);

        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException(
                $"The restore script '{scriptName}' is not deployed beside the service.",
                scriptPath);
        }

        var script = await File.ReadAllTextAsync(scriptPath, cancellationToken).ConfigureAwait(false);
        var materializedPath = Path.Combine(Path.GetTempPath(), $"mtm-restore-{Guid.NewGuid():N}.sql");

        await File
            .WriteAllTextAsync(
                materializedPath,
                script.Replace(DatabasePlaceholder, database, StringComparison.Ordinal),
                cancellationToken)
            .ConfigureAwait(false);

        return materializedPath;
    }

    private static void TryDeleteScript(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string Quote(string value) =>
        value.Contains(' ', StringComparison.Ordinal) ? $"\"{value}\"" : value;

    /// <summary>One client invocation's outcome.</summary>
    /// <param name="ExitCode">The client's exit code.</param>
    /// <param name="StandardOutput">Everything the client wrote to stdout.</param>
    private readonly record struct ClientResult(int ExitCode, string StandardOutput);

    private async Task<ClientResult> RunClientAsync(
        string fileName,
        List<string> arguments,
        CancellationToken cancellationToken,
        string? standardInputPath = null)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = string.Join(' ', arguments.Select(Quote)),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = standardInputPath is not null
            }
        };

        process.Start();

        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);

        if (standardInputPath is not null)
        {
            // The dump is streamed through stdin: that avoids quoting a path that may contain spaces and
            // avoids the client's `source` directive entirely.
            await using var dump = File.OpenRead(standardInputPath);
            await dump.CopyToAsync(process.StandardInput.BaseStream, cancellationToken).ConfigureAwait(false);
            await process.StandardInput.FlushAsync(cancellationToken).ConfigureAwait(false);
            process.StandardInput.Close();
        }

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(s_restoreTimeout);

        try
        {
            await process.WaitForExitAsync(timeoutSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);

            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            return new ClientResult(-1, string.Empty);
        }

        // The output is captured once, in order: the restore verification reads the count out of it.
        var standardOutput = await standardOutputTask.ConfigureAwait(false);
        var standardError = await standardErrorTask.ConfigureAwait(false);

        if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(standardError))
        {
            _logger.LogError(
                "MySQL client '{FileName}' exited {ExitCode}: {Error}",
                Path.GetFileName(fileName),
                process.ExitCode,
                RefreshRunRecordStore.Sanitize(standardError.Trim()));
        }

        return new ClientResult(process.ExitCode, standardOutput);
    }

    /// <summary>
    /// Demonstrates that the reload really produced a store, using the reviewed <c>verify_restore.sql</c>
    /// artifact rather than statement text built here (FR-010 verification step, FR-015).
    /// </summary>
    /// <param name="mysqlPath">The located MySQL client.</param>
    /// <param name="database">The store that was just replaced.</param>
    /// <param name="artifactSizeBytes">Size of the reloaded artifact, reported beside the table count.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An operator-facing, secret-free summary.</returns>
    /// <remarks>
    /// The check is deliberately store-agnostic. The earlier implementation counted rows in two hard-coded
    /// waitlist tables, so three of the four stores could only ever report "unverified" — and one of those
    /// tables no longer exists in the schema, so it proved nothing even for the waitlist store. Asking how
    /// many base tables the store now has is true for every store, and it still catches the failure that
    /// matters: a reload that reported success but left nothing behind.
    /// </remarks>
    private async Task<string> BuildVerificationSummaryAsync(
        string mysqlPath,
        string database,
        long artifactSizeBytes,
        CancellationToken cancellationToken)
    {
        string scriptPath;

        try
        {
            scriptPath = await MaterializeScriptAsync(VerifyRestoreScriptName, database, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (FileNotFoundException exception)
        {
            _logger.LogError(exception, "The restore verification script is not deployed.");
            return $"'{database}' was reloaded, but the verification script is missing.";
        }

        try
        {
            var result = await RunClientAsync(
                mysqlPath,
                BuildConnectionArguments(),
                cancellationToken,
                standardInputPath: scriptPath).ConfigureAwait(false);

            if (result.ExitCode != 0)
            {
                return $"'{database}' was reloaded, but its table count could not be read " +
                       $"(exit {result.ExitCode}).";
            }

            return TryReadTableCount(result.StandardOutput, out var tableCount)
                ? $"'{database}' replaced: {tableCount} tables, loaded from a {artifactSizeBytes} byte artifact."
                : $"'{database}' was reloaded, but its table count was not reported.";
        }
        finally
        {
            TryDeleteScript(scriptPath);
        }
    }

    /// <summary>Reads the single count out of the verification client's output.</summary>
    /// <param name="standardOutput">The client's stdout, with or without the column header.</param>
    /// <param name="tableCount">The parsed count.</param>
    /// <returns><see langword="true"/> when a count was found.</returns>
    private static bool TryReadTableCount(string standardOutput, out long tableCount)
    {
        tableCount = 0;

        foreach (var line in standardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (long.TryParse(line, NumberStyles.Integer, CultureInfo.InvariantCulture, out tableCount))
            {
                return true;
            }
        }

        return false;
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
        catch (System.ComponentModel.Win32Exception)
        {
        }
    }
}
