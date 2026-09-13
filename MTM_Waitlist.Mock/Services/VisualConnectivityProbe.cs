using System.Diagnostics;

using Microsoft.Data.SqlClient;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Mock.Contracts;

namespace MTM_Waitlist.Mock.Services;

/// <summary>
/// Probes Infor Visual by opening a connection and immediately closing it.
/// </summary>
/// <remarks>
/// <para>
/// The probe issues no statement text at all, so it is cheap, cannot be affected by a bad read script,
/// and keeps the SP-first rule intact with no inline-SQL audit exemption.
/// </para>
/// <para>
/// <b>The probe is bounded.</b> It asks one question — "is Infor Visual reachable right now?" — and a
/// question with a timer behind it may not cost what a data read costs. On the reporting workstation
/// (2026-09-12) an absent server made each probe take ~11.0 s: the configured ten-second connect timeout
/// plus SqlClient's default blind retry (<c>Connect Retry Count=1</c>, <c>Connect Retry Interval=10</c>).
/// The probe therefore imposes its own connect timeout, disables the retry, and caps the whole attempt, so
/// a dead or black-holed host is reported in seconds and the read-status loop keeps its cadence.
/// </para>
/// </remarks>
public sealed class VisualConnectivityProbe : IVisualConnectivityProbe
{
    /// <summary>
    /// Hard ceiling on one probe attempt, independent of the connect timeout in the connection string.
    /// </summary>
    internal static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Connect timeout the probe imposes. It is never applied when the caller asked for less.
    /// </summary>
    internal const int ProbeConnectTimeoutSeconds = 2;

    private readonly IVisualConnectionStringProvider _connectionStringProvider;
    private readonly TimeSpan _probeTimeout;

    /// <summary>Creates the probe.</summary>
    /// <param name="connectionStringProvider">Supplies the Infor Visual connection string.</param>
    /// <param name="probeTimeout">Overrides <see cref="ProbeTimeout"/>; for tests with a tighter budget.</param>
    public VisualConnectivityProbe(
        IVisualConnectionStringProvider connectionStringProvider,
        TimeSpan? probeTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(connectionStringProvider);

        _connectionStringProvider = connectionStringProvider;
        _probeTimeout = probeTimeout ?? ProbeTimeout;

        if (_probeTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(probeTimeout), _probeTimeout, "The probe timeout must be greater than zero.");
        }
    }

    /// <inheritdoc />
    public async Task<bool> ProbeAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = _connectionStringProvider.Resolve();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(_probeTimeout);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await using var connection = new SqlConnection(BuildProbeConnectionString(connectionString));
            await connection.OpenAsync(timeoutSource.Token).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Any failure to open the connection — including this probe's own timeout — means the source is
            // not reachable right now, which is exactly what the detector records. The elapsed time is logged
            // because an unreachable source is otherwise silent here, and that silence is what made the
            // original stall hard to attribute.
            StartupDebugLog.Info(
                "VisualProbe",
                $"Infor Visual did not answer within {stopwatch.ElapsedMilliseconds} ms. {exception.GetType().Name}: {exception.Message}");
            return false;
        }
    }

    /// <summary>
    /// Prepares the connection string the probe opens: no blind retry, and a connect timeout no longer than
    /// the probe's own bound.
    /// </summary>
    /// <param name="connectionString">The resolved Infor Visual connection string.</param>
    /// <returns>The connection string the probe opens.</returns>
    internal static string BuildProbeConnectionString(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            ConnectRetryCount = 0,
        };

        builder.ConnectTimeout = Math.Min(builder.ConnectTimeout, ProbeConnectTimeoutSeconds);

        return builder.ConnectionString;
    }
}
