using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;
using MTM_Waitlist.Mock.Service.Models;

namespace MTM_Waitlist.Mock.Service.Services;

/// <summary>
/// Produces one <c>mysqldump</c> artifact per store, with an up-front tool probe and retention pruning.
/// </summary>
/// <remarks>
/// <para>
/// <b>Availability is reported, never worked around.</b> When <c>mysqldump</c> cannot be found the run
/// records <see cref="BackupRunOutcome.ToolUnavailable"/> and produces <b>no artifact at all</b> — a
/// partial or zero-length file is never recorded as a success (FR-013, SC-008).
/// </para>
/// <para>
/// <b>Success is two conditions, not one:</b> the process must exit 0 <i>and</i> the file must exist with
/// a non-zero size. A dump that exits 0 having written nothing is a failure.
/// </para>
/// <para>
/// <b>The password never appears on a command line.</b> It is passed only through a MySQL option file
/// referenced with <c>--defaults-extra-file</c>, so it cannot be read out of a process list, a log, or a
/// captured command line (FR-026, research.md R7).
/// </para>
/// <para>
/// <c>--result-file</c> is mandatory: without it the dump is written through the console, which on
/// Windows PowerShell produces UTF-16 output that cannot be reloaded (research.md R7).
/// </para>
/// </remarks>
public sealed class BackupEngine
{
    private static readonly TimeSpan s_versionProbeTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan s_dumpTimeout = TimeSpan.FromHours(2);

    private readonly BackupArtifactStore _artifactStore;
    private readonly MySqlConnectionStringResolver _connectionResolver;
    private readonly ILogger<BackupEngine> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly Func<Models.ServiceConfiguration> _configurationAccessor;

    private readonly SemaphoreSlim _toolProbeGate = new(1, 1);
    private readonly Dictionary<BackupStore, SemaphoreSlim> _storeGates = [];

    private bool? _toolAvailable;
    private string? _toolPath;

    /// <summary>Creates the engine.</summary>
    /// <param name="artifactStore">Records artifacts and last-run outcomes.</param>
    /// <param name="connectionResolver">Supplies host/port/login without any secret being persisted.</param>
    /// <param name="configurationAccessor">Reads the live configuration, so a settings save applies without a restart.</param>
    /// <param name="logger">Logger; credential material is never passed to it.</param>
    /// <param name="timeProvider">Time source for artifact naming and timestamps.</param>
    public BackupEngine(
        BackupArtifactStore artifactStore,
        MySqlConnectionStringResolver connectionResolver,
        Func<Models.ServiceConfiguration> configurationAccessor,
        ILogger<BackupEngine> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(artifactStore);
        ArgumentNullException.ThrowIfNull(connectionResolver);
        ArgumentNullException.ThrowIfNull(configurationAccessor);
        ArgumentNullException.ThrowIfNull(logger);

        _artifactStore = artifactStore;
        _connectionResolver = connectionResolver;
        _configurationAccessor = configurationAccessor;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;

        foreach (var store in BackupStoreExtensions.All)
        {
            _storeGates[store] = new SemaphoreSlim(1, 1);
        }
    }

    /// <summary>Whether a store's backup is currently running (the API reports this as a conflict).</summary>
    /// <param name="store">The store to check.</param>
    public bool IsRunning(BackupStore store) =>
        _storeGates.TryGetValue(store, out var gate) && gate.CurrentCount == 0;

    /// <summary>
    /// Reports whether <c>mysqldump</c> can be found and executed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="reprobe">Forces a fresh probe instead of using the cached result.</param>
    /// <returns><see langword="true"/> when the tool answered its version probe.</returns>
    public async Task<bool> IsToolAvailableAsync(CancellationToken cancellationToken = default, bool reprobe = false)
    {
        if (!reprobe && _toolAvailable is { } cached)
        {
            return cached;
        }

        await _toolProbeGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (!reprobe && _toolAvailable is { } alreadyKnown)
            {
                return alreadyKnown;
            }

            var path = ResolveToolPath();
            if (path is null)
            {
                _toolPath = null;
                _toolAvailable = false;
                return false;
            }

            _toolAvailable = await ProbeToolAsync(path, cancellationToken).ConfigureAwait(false);
            _toolPath = _toolAvailable == true ? path : null;
            return _toolAvailable.Value;
        }
        finally
        {
            _toolProbeGate.Release();
        }
    }

    /// <summary>
    /// Resolves the <c>mysqldump</c> executable: the configured path first, then <c>PATH</c>.
    /// </summary>
    /// <returns>The absolute path, or <see langword="null"/> when the tool cannot be located.</returns>
    /// <remarks>
    /// Only the configured path and <c>PATH</c> are searched. Guessing at installation directories would
    /// silently pick up an unrelated MySQL version, which is worse than reporting the tool as unavailable.
    /// </remarks>
    public string? ResolveToolPath()
    {
        var configured = _configurationAccessor().MysqldumpPath;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return File.Exists(configured) ? configured : null;
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
                var candidate = Path.Combine(directory.Trim(), "mysqldump.exe");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            catch (ArgumentException)
            {
                // A malformed PATH segment is skipped rather than failing the whole resolution.
            }
        }

        return null;
    }

    /// <summary>
    /// Runs one store's backup and enforces that store's retention.
    /// </summary>
    /// <param name="store">The store to back up.</param>
    /// <param name="isSafetySnapshot">
    /// Marks the artifact as the pre-restore safety snapshot. A safety snapshot is never pruned and is not
    /// counted against retention.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The recorded outcome.</returns>
    /// <remarks>
    /// A second concurrent request for the same store is rejected rather than overlapped, so two dumps
    /// cannot write the same destination at once. Different stores are independent and may run together.
    /// </remarks>
    public async Task<BackupRunRecord> RunAsync(
        BackupStore store,
        bool isSafetySnapshot = false,
        CancellationToken cancellationToken = default)
    {
        var gate = _storeGates[store];

        if (!await gate.WaitAsync(TimeSpan.Zero, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"A backup for '{store.ToDatabaseName()}' is already running.");
        }

        try
        {
            return await RunCoreAsync(store, isSafetySnapshot, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<BackupRunRecord> RunCoreAsync(
        BackupStore store,
        bool isSafetySnapshot,
        CancellationToken cancellationToken)
    {
        var configuration = _configurationAccessor();
        var policy = configuration.BackupPolicies[store];
        var startedUtc = _timeProvider.GetUtcNow().UtcDateTime;

        if (!await IsToolAvailableAsync(cancellationToken).ConfigureAwait(false))
        {
            var unavailable = new BackupRunRecord
            {
                Store = store,
                StartedUtc = startedUtc,
                FinishedUtc = _timeProvider.GetUtcNow().UtcDateTime,
                Outcome = BackupRunOutcome.ToolUnavailable,
                IsSafetySnapshot = isSafetySnapshot,
                ErrorMessage = "mysqldump could not be found or could not be executed; no artifact was produced."
            };

            _logger.LogWarning(
                "Backup for {Store} skipped: mysqldump is unavailable. No artifact was recorded.",
                store.ToDatabaseName());

            // Deliberately no artifact parameter: an unavailable tool must never leave a record that
            // looks like a produced backup (FR-013).
            await _artifactStore.RecordAsync(unavailable, artifact: null, cancellationToken).ConfigureAwait(false);
            return unavailable;
        }

        var toolPath = _toolPath ?? ResolveToolPath();
        if (toolPath is null)
        {
            var disappeared = new BackupRunRecord
            {
                Store = store,
                StartedUtc = startedUtc,
                FinishedUtc = _timeProvider.GetUtcNow().UtcDateTime,
                Outcome = BackupRunOutcome.ToolUnavailable,
                IsSafetySnapshot = isSafetySnapshot,
                ErrorMessage = "mysqldump could not be located when the backup started."
            };

            await _artifactStore.RecordAsync(disappeared, artifact: null, cancellationToken).ConfigureAwait(false);
            return disappeared;
        }

        Directory.CreateDirectory(policy.DestinationDirectory);

        var database = store.ToDatabaseName();
        var artifactPath = Path.Combine(
            policy.DestinationDirectory,
            BuildFileName(database, startedUtc, isSafetySnapshot));

        var arguments = BuildArguments(database, artifactPath, configuration);

        try
        {
            var (exitCode, standardError) = await RunProcessAsync(toolPath, arguments, cancellationToken).ConfigureAwait(false);
            var finishedUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var sizeBytes = File.Exists(artifactPath) ? new FileInfo(artifactPath).Length : 0;

            if (exitCode != 0 || sizeBytes == 0)
            {
                // A file may have been created but is not a usable backup; remove it so it can never be
                // mistaken for one, and record the run as a failure with no artifact.
                TryDeletePartialFile(artifactPath);

                var failed = new BackupRunRecord
                {
                    Store = store,
                    StartedUtc = startedUtc,
                    FinishedUtc = finishedUtc,
                    Outcome = BackupRunOutcome.Failed,
                    IsSafetySnapshot = isSafetySnapshot,
                    ErrorMessage = RefreshRunRecordStore.Sanitize(
                        $"mysqldump exited {exitCode} ({sizeBytes} bytes written). {Truncate(standardError)}")
                };

                _logger.LogError(
                    "Backup for {Store} failed. ExitCode={ExitCode}, Bytes={Bytes}.",
                    database,
                    exitCode,
                    sizeBytes);

                await _artifactStore.RecordAsync(failed, artifact: null, cancellationToken).ConfigureAwait(false);
                return failed;
            }

            var artifact = new BackupArtifact
            {
                Store = store,
                CreatedUtc = finishedUtc,
                FilePath = artifactPath,
                SizeBytes = sizeBytes,
                IsRetained = true,
                IsSafetySnapshot = isSafetySnapshot
            };

            var succeeded = new BackupRunRecord
            {
                Store = store,
                StartedUtc = startedUtc,
                FinishedUtc = finishedUtc,
                Outcome = BackupRunOutcome.Succeeded,
                ArtifactPath = artifactPath,
                IsSafetySnapshot = isSafetySnapshot
            };

            await _artifactStore.RecordAsync(succeeded, artifact, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Backup for {Store} produced {Bytes} bytes at {ArtifactPath}.",
                database,
                sizeBytes,
                artifactPath);

            if (!isSafetySnapshot)
            {
                await EnforceRetentionAsync(store, policy.RetentionCount, cancellationToken).ConfigureAwait(false);
            }

            return succeeded;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            TryDeletePartialFile(artifactPath);
            throw;
        }
        catch (Exception exception)
        {
            TryDeletePartialFile(artifactPath);

            var crashed = new BackupRunRecord
            {
                Store = store,
                StartedUtc = startedUtc,
                FinishedUtc = _timeProvider.GetUtcNow().UtcDateTime,
                Outcome = BackupRunOutcome.Failed,
                IsSafetySnapshot = isSafetySnapshot,
                ErrorMessage = RefreshRunRecordStore.Sanitize(exception.Message)
            };

            _logger.LogError(exception, "Backup for {Store} failed while running mysqldump.", database);
            await _artifactStore.RecordAsync(crashed, artifact: null, cancellationToken).ConfigureAwait(false);
            return crashed;
        }
    }

    /// <summary>
    /// Prunes one store's artifacts down to its retention count and reports what was removed.
    /// </summary>
    /// <param name="store">The store whose retention is enforced.</param>
    /// <param name="retentionCount">How many artifacts to keep.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task EnforceRetentionAsync(
        BackupStore store,
        int retentionCount,
        CancellationToken cancellationToken = default)
    {
        var pruned = await _artifactStore.PruneAsync(store, retentionCount, cancellationToken).ConfigureAwait(false);

        foreach (var artifact in pruned)
        {
            _logger.LogInformation(
                "Pruned backup artifact {ArtifactPath} for {Store} to honour retention {Retention}.",
                artifact.FilePath,
                store.ToDatabaseName(),
                retentionCount);
        }
    }

    private string BuildArguments(
        string database,
        string artifactPath,
        Models.ServiceConfiguration configuration)
    {
        var arguments = new List<string>();

        // The password travels only through the option file (FR-026, research.md R7).
        if (configuration.MySqlConnection.PasswordFilePath is { Length: > 0 } passwordFile)
        {
            arguments.Add($"--defaults-extra-file={passwordFile}");
        }

        arguments.Add($"--host={configuration.MySqlConnection.Server}");
        arguments.Add($"--port={configuration.MySqlConnection.Port.ToString(CultureInfo.InvariantCulture)}");

        if (!string.IsNullOrWhiteSpace(configuration.MySqlConnection.UserId))
        {
            arguments.Add($"--user={configuration.MySqlConnection.UserId}");
        }

        // Consistent, non-locking snapshot of an InnoDB store, including its routines.
        arguments.Add("--single-transaction");
        arguments.Add("--routines");
        arguments.Add("--databases");
        arguments.Add(database);
        arguments.Add($"--result-file={artifactPath}");

        return string.Join(' ', arguments.Select(QuoteArgument));

        // Only the option-file path, the host, and the port are ever named; no secret is added here.
        static string QuoteArgument(string argument) =>
            argument.Contains(' ', StringComparison.Ordinal) ? $"\"{argument}\"" : argument;
    }

    private static string BuildFileName(string database, DateTime startedUtc, bool isSafetySnapshot) =>
        isSafetySnapshot
            ? $"{database}_safety_{startedUtc:yyyyMMdd'T'HHmmss'Z'}.sql"
            : $"{database}_{startedUtc:yyyyMMdd'T'HHmmss'Z'}.sql";

    private static void TryDeletePartialFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string Truncate(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Length <= 500 ? value.Trim() : value[..500].Trim();

    private async Task<bool> ProbeToolAsync(string toolPath, CancellationToken cancellationToken)
    {
        try
        {
            var (exitCode, _) = await RunProcessAsync(toolPath, "--version", cancellationToken, s_versionProbeTimeout)
                .ConfigureAwait(false);

            return exitCode == 0;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "mysqldump at {ToolPath} could not be executed.", toolPath);
            return false;
        }
    }

    private static async Task<(int ExitCode, string StandardError)> RunProcessAsync(
        string fileName,
        string arguments,
        CancellationToken cancellationToken,
        TimeSpan? timeout = null)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            }
        };

        process.Start();

        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout ?? s_dumpTimeout);

        try
        {
            await process.WaitForExitAsync(timeoutSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                TryKill(process);
                throw;
            }

            TryKill(process);
            return (-1, "The backup tool did not finish within the allowed time.");
        }

        _ = await standardOutputTask.ConfigureAwait(false);
        var standardError = await standardErrorTask.ConfigureAwait(false);

        return (process.ExitCode, standardError);
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
