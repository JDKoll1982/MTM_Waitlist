using System.Runtime.InteropServices;
using Microsoft.Data.SqlClient;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Mock.Services;

/// <summary>
/// Executes an Infor Visual read script and classifies how the attempt ended.
/// </summary>
/// <remarks>
/// <para>
/// The retired Module_Setup and Module_Core executors returned an empty list for <em>every</em> failure
/// path, so "Visual is unreachable" and "Visual answered with zero rows" were indistinguishable. The
/// fallback contract requires those to be different outcomes — an empty live answer is a real answer
/// that must never be replaced by cache, while only unreachability permits fallback (FR-024). This
/// executor therefore reports a <see cref="VisualQueryOutcome"/> instead of swallowing errors.
/// </para>
/// <para>
/// Classification is deliberately narrow: only connectivity, timeout, and login failures count as
/// unreachable. A missing script and a missing connection are configuration faults, and a syntax or
/// permission error is a genuine read failure — all three surface so the cache cannot mask them.
/// </para>
/// </remarks>
public sealed class VisualQueryExecutor : IVisualQueryExecutor
{
    private const int DefaultCommandTimeoutSeconds = 15;

    /// <summary>
    /// SQL Server error numbers meaning the server could not be reached or authenticated, as opposed to
    /// a query that ran and failed.
    /// </summary>
    private static readonly HashSet<int> s_unreachableErrorNumbers =
    [
        -2,     // client-side command timeout
        20,     // instance does not support encryption
        53,     // network path not found
        64,     // connection reset by peer
        121,    // semaphore timeout period has expired
        233,    // no process is on the other end of the pipe
        258,    // wait operation timed out
        1231,   // network-related error instantiating the server
        4060,   // cannot open database requested by the login
        10053,  // transport-level error, connection aborted
        10054,  // transport-level error, reset by peer
        10060,  // connection attempt timed out
        10061,  // connection refused
        11001,  // host not found
        18456   // login failed
    ];

    private readonly IInforVisualScriptStore _scriptStore;
    private readonly IVisualConnectionStringProvider _connectionStringProvider;

    /// <summary>Creates the executor.</summary>
    /// <param name="scriptStore">Loads the read script from the content root.</param>
    /// <param name="connectionStringProvider">Supplies the Infor Visual connection string.</param>
    public VisualQueryExecutor(
        IInforVisualScriptStore scriptStore,
        IVisualConnectionStringProvider connectionStringProvider)
    {
        ArgumentNullException.ThrowIfNull(scriptStore);
        ArgumentNullException.ThrowIfNull(connectionStringProvider);

        _scriptStore = scriptStore;
        _connectionStringProvider = connectionStringProvider;
    }

    /// <inheritdoc />
    public async Task<VisualQueryOutcome> ExecuteAsync(
        string sourceScriptRelativePath,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceScriptRelativePath);
        ArgumentNullException.ThrowIfNull(parameters);

        var script = await _scriptStore.LoadAsync(sourceScriptRelativePath, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(script))
        {
            StartupDebugLog.Info("VisualQuery", $"Read script '{sourceScriptRelativePath}' was not found in the content root.");
            return VisualQueryOutcome.Failed($"Infor Visual read script '{sourceScriptRelativePath}' was not found.");
        }

        var connectionString = _connectionStringProvider.Resolve();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            StartupDebugLog.Info("VisualQuery", "No Infor Visual connection is configured.");
            return VisualQueryOutcome.Failed("No Infor Visual connection is configured.");
        }

        // Bounded before it is opened. Without this the read inherited the configured connect timeout, so the
        // first Visual read of a session spent ~12 s discovering a host the reachability probe had already
        // declared unreachable in two.
        connectionString = BoundConnectAttempt(connectionString);

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = new SqlCommand(script, connection)
            {
                CommandType = System.Data.CommandType.Text,
                CommandTimeout = DefaultCommandTimeoutSeconds,
            };

            foreach (var parameter in parameters)
            {
                var parameterName = parameter.Key.StartsWith('@') ? parameter.Key : $"@{parameter.Key}";
                _ = command.Parameters.AddWithValue(parameterName, parameter.Value ?? DBNull.Value);
            }

            var rows = new List<IReadOnlyDictionary<string, object?>>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                for (var index = 0; index < reader.FieldCount; index++)
                {
                    row[reader.GetName(index)] = reader.IsDBNull(index) ? null : reader.GetValue(index);
                }

                rows.Add(row);
            }

            StartupDebugLog.Info("VisualQuery", $"Read script '{sourceScriptRelativePath}' returned {rows.Count} row(s).");
            return VisualQueryOutcome.Ok(rows);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException exception)
        {
            return ClassifyAsUnreachable(sourceScriptRelativePath, exception);
        }
        catch (SqlException exception) when (s_unreachableErrorNumbers.Contains(exception.Number))
        {
            return ClassifyAsUnreachable(sourceScriptRelativePath, exception);
        }
        catch (SqlException exception)
        {
            StartupDebugLog.Error(
                "VisualQuery",
                exception,
                $"Read script '{sourceScriptRelativePath}' failed with SQL error {exception.Number}.");
            return VisualQueryOutcome.Failed($"Infor Visual query failed (SQL error {exception.Number}).");
        }
        catch (COMException exception)
        {
            return ClassifyAsUnreachable(sourceScriptRelativePath, exception);
        }
        catch (Exception exception)
        {
            StartupDebugLog.Error("VisualQuery", exception, $"Read script '{sourceScriptRelativePath}' failed.");
            return VisualQueryOutcome.Failed(exception.Message);
        }
    }

    /// <summary>
    /// Caps how long a read may spend failing to reach Infor Visual, so the wait before the mirror answers is
    /// the same short bound the reachability probe already uses.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The read used to inherit the configured connect timeout (<c>ConnectionTimeoutSeconds</c>, ten seconds by
    /// default) — and on an absent host the first Visual read of a session measured <b>12.5 s</b> before the
    /// fallback could serve the same question from <c>mtm_mock</c> in microseconds. The probe asks the identical
    /// question of the identical host and answers it in two seconds.
    /// </para>
    /// <para>
    /// <b>The bound is shared with the probe rather than stated again</b>, so the two cannot drift into
    /// disagreeing about how long "unreachable" takes to establish: a change to either moves both. It costs
    /// nothing when the host is up — a reachable server completes the handshake in milliseconds — and it is the
    /// entire wait when the host is absent.
    /// </para>
    /// <para>
    /// An unparseable value is returned unchanged rather than throwing, matching
    /// <see cref="IVisualConnectionStringProvider.Resolve"/>'s documented behaviour: the fault then surfaces
    /// from the connection attempt, where it is classified as unreachable or failed.
    /// </para>
    /// </remarks>
    internal static string BoundConnectAttempt(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            builder.ConnectTimeout = Math.Min(builder.ConnectTimeout, VisualConnectivityProbe.ProbeConnectTimeoutSeconds);
            return builder.ConnectionString;
        }
        catch (ArgumentException ex)
        {
            StartupDebugLog.Info(
                "VisualQuery",
                $"The Infor Visual connection string could not be bounded ({ex.GetType().Name}), so the read keeps its configured timeout.");
            return connectionString;
        }
    }

    private static VisualQueryOutcome ClassifyAsUnreachable(string sourceScriptRelativePath, Exception exception)
    {
        StartupDebugLog.Error(
            "VisualQuery",
            exception,
            $"Read script '{sourceScriptRelativePath}' could not reach Infor Visual.");
        return VisualQueryOutcome.Unreachable(exception.Message);
    }
}
