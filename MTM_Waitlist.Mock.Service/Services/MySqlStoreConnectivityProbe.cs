using Microsoft.Extensions.Logging;

using MTM_Waitlist.Mock.Service.Contracts;
using MTM_Waitlist.Mock.Service.Models;

namespace MTM_Waitlist.Mock.Service.Services;

/// <summary>
/// Probes a store by opening a connection to it and closing it.
/// </summary>
/// <remarks>
/// <para>
/// The connection string is resolved exactly as the store's read, backup and restore paths resolve it, so this
/// probe cannot disagree with them about which host or database a store lives on.
/// </para>
/// <para>
/// A store with no connection string is reported as <see cref="StoreProbeResult.NotConfigured"/> rather than
/// unreachable, because nothing has been proved about it — see the remarks on that type.
/// </para>
/// <para>
/// <b>The failure detail goes to the log, not to the reported reason.</b> Every log line stays on this
/// machine, while the reason travels into the status payload, the settings surface and the HTTP API, where a
/// provider exception message could echo a connection string (FR-026).
/// </para>
/// </remarks>
public sealed class MySqlStoreConnectivityProbe : IMySqlStoreConnectivityProbe
{
    /// <summary>Default cap on one probe, so an unreachable host cannot stall a loop or a status request.</summary>
    public static readonly TimeSpan DefaultProbeTimeout = TimeSpan.FromSeconds(5);

    private readonly MySqlConnectionStringResolver _resolver;
    private readonly ILogger<MySqlStoreConnectivityProbe> _logger;
    private readonly TimeSpan _probeTimeout;

    /// <summary>Creates the probe.</summary>
    /// <param name="resolver">Resolves each store's connection string.</param>
    /// <param name="logger">Logger; credential material is never passed to it.</param>
    /// <param name="probeTimeout">Cap on one probe; tests lower it.</param>
    public MySqlStoreConnectivityProbe(
        MySqlConnectionStringResolver resolver,
        ILogger<MySqlStoreConnectivityProbe> logger,
        TimeSpan? probeTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(logger);

        _resolver = resolver;
        _logger = logger;
        _probeTimeout = probeTimeout ?? DefaultProbeTimeout;

        if (_probeTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(probeTimeout), _probeTimeout, "The probe timeout must be greater than zero.");
        }
    }

    /// <inheritdoc />
    public async Task<StoreProbeResult> ProbeAsync(BackupStore store, CancellationToken cancellationToken = default)
    {
        var database = store.ToDatabaseName();
        var connectionString = _resolver.Resolve(database, store.ToConnectionStringEnvironmentVariable());

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return StoreProbeResult.NotConfigured;
        }

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(_probeTimeout);

        try
        {
            await using var connection = new MySqlConnector.MySqlConnection(connectionString);
            await connection.OpenAsync(timeoutSource.Token).ConfigureAwait(false);
            return StoreProbeResult.Reachable;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogInformation(
                exception,
                "Store {Store} could not be reached from this machine; its backup and restore are disabled.",
                database);

            return StoreProbeResult.Unreachable($"MySQL did not accept a connection to '{database}' from this machine.");
        }
    }
}
