using System.Collections.Concurrent;
using System.Net.Sockets;

using MySqlConnector;

namespace MTM_Waitlist.Module_Core.Services;

/// <summary>
/// Substitutes the local MySQL server for the configured one when the configured host cannot be reached.
/// </summary>
/// <remarks>
/// <para>
/// The application ships configured for the shared plant host. A workstation that is off the plant network
/// cannot reach it, and then every read fails with a connect timeout even though the same databases are
/// present locally. The rule is: use the configured server when it answers; otherwise use the local server
/// when <i>that</i> answers; otherwise leave the connection string alone, so the caller's own failure path
/// reports the outage rather than this helper inventing an outcome.
/// </para>
/// <para>
/// The check is a TCP connect rather than a login — it answers "is something listening?" in about a second,
/// against the fifteen a failed MySQL login costs — and the verdict is cached for a short window so a burst
/// of operations in the same startup does not repeat the probe. Nothing is cached beyond that window, so a
/// workstation that rejoins the plant network starts using the configured server again on its own.
/// </para>
/// </remarks>
public static class MySqlHostFallback
{
    /// <summary>The server substituted for an unreachable configured host.</summary>
    internal const string LocalHostName = "localhost";

    private const int ProbeTimeoutMilliseconds = 1_000;

    private static readonly TimeSpan s_verdictLifetime = TimeSpan.FromSeconds(30);

    private static readonly ConcurrentDictionary<string, (string Resolved, DateTimeOffset ProbedUtc)> s_verdicts =
        new(StringComparer.Ordinal);

    /// <summary>
    /// Returns the connection string to use: the given one when its server answers, otherwise the same
    /// string pointed at the local server when that answers, otherwise the given string unchanged.
    /// </summary>
    public static string? Apply(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        var now = DateTimeOffset.UtcNow;
        if (s_verdicts.TryGetValue(connectionString, out var verdict)
            && now - verdict.ProbedUtc < s_verdictLifetime)
        {
            return verdict.Resolved;
        }

        var resolved = Apply(connectionString, IsReachable);
        s_verdicts[connectionString] = (resolved ?? connectionString, now);
        return resolved;
    }

    /// <summary>
    /// The decision itself, with the reachability check supplied by the caller so every branch can be
    /// exercised without opening a socket (and without the verdict cache).
    /// </summary>
    /// <param name="connectionString">The connection string to consider.</param>
    /// <param name="isReachable">Reports whether a server is listening on the given host and port.</param>
    internal static string? Apply(string? connectionString, Func<string, uint, bool> isReachable)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        MySqlConnectionStringBuilder builder;
        try
        {
            builder = new MySqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException)
        {
            // A malformed connection string is the caller's to report, exactly as before this helper existed.
            return connectionString;
        }

        var configuredHost = builder.Server;
        if (string.IsNullOrWhiteSpace(configuredHost)
            || IsLocalHostName(configuredHost)
            || configuredHost.Contains('\\', StringComparison.Ordinal)
            || configuredHost.Contains('/', StringComparison.Ordinal))
        {
            // Nothing to substitute: the string already targets this machine, or it targets a pipe/socket
            // path that "localhost" could not replace.
            return connectionString;
        }

        if (isReachable(configuredHost, builder.Port) || !isReachable(LocalHostName, builder.Port))
        {
            return connectionString;
        }

        builder.Server = LocalHostName;
        return builder.ConnectionString;
    }

    private static bool IsLocalHostName(string host)
    {
        return host.Equals(LocalHostName, StringComparison.OrdinalIgnoreCase)
            || host.Equals("127.0.0.1", StringComparison.Ordinal)
            || host.Equals("::1", StringComparison.Ordinal)
            || host.Equals("(local)", StringComparison.OrdinalIgnoreCase)
            || host.Equals(".", StringComparison.Ordinal);
    }

    private static bool IsReachable(string host, uint port)
    {
        try
        {
            using var client = new TcpClient();
            var connect = client.ConnectAsync(host, (int)port);
            if (connect.Wait(ProbeTimeoutMilliseconds))
            {
                return client.Connected;
            }

            // Abandon the pending attempt without leaving an unobserved fault behind: the probe is a hint,
            // and the operation's own connect is the authority on whether the server answers.
            _ = connect.ContinueWith(
                static task => _ = task.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

            return false;
        }
        catch
        {
            return false;
        }
    }
}
