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
/// present locally. The rule is, in the order the two hosts are tried: use the configured server when it
/// answers; otherwise use the local server when <i>that</i> answers; otherwise leave the connection string
/// alone, so the caller's own failure path reports the outage rather than this helper inventing an outcome.
/// </para>
/// <para>
/// The check is a TCP connect rather than a login — it answers "is something listening?" in about a second,
/// against the fifteen a failed MySQL login costs — and a decision <i>that a host answered</i> is cached for a
/// short window so a burst of operations in the same startup does not repeat the probe. A decision where
/// <b>nothing</b> answered is deliberately not cached: that is an outage rather than a verdict, and remembering
/// one would keep every later operation pointed at a host that was silent a moment ago. Not remembering it is
/// what makes a surface's own retry work — the local server a person switches on after the stop is picked up by
/// the very next attempt instead of waiting for the window to lapse. A workstation that rejoins the plant
/// network returns to the configured server on its own for the same reason.
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

        var decision = Decide(connectionString, IsReachable);

        // Only a decision that a host answered is remembered. An outage is not a verdict (see the type's
        // remarks): remembering one would leave the next attempt — including a surface's retry, after the
        // person has switched the local server on — pointed at the host that did not answer.
        if (ShouldRemember(decision))
        {
            s_verdicts[connectionString] = (decision.Resolved ?? connectionString, now);
        }

        return decision.Resolved;
    }

    /// <summary>
    /// The decision itself, with the reachability check supplied by the caller so every branch can be
    /// exercised without opening a socket (and without the verdict cache).
    /// </summary>
    /// <param name="connectionString">The connection string to consider.</param>
    /// <param name="isReachable">Reports whether a server is listening on the given host and port.</param>
    internal static string? Apply(string? connectionString, Func<string, uint, bool> isReachable)
        => Decide(connectionString, isReachable).Resolved;

    /// <summary>
    /// Whether a decision is worth remembering, which is exactly when a host answered it.
    /// </summary>
    /// <param name="decision">The decision to judge.</param>
    /// <returns>
    /// <c>true</c> when the configured host or this machine answered; <c>false</c> when neither did, because a
    /// decision describing an outage has to be made again by the next attempt.
    /// </returns>
    internal static bool ShouldRemember(HostDecision decision) => decision.AnyHostAnswered;

    /// <summary>
    /// Works out which host to use, in the order the rule states: the configured host first, then this machine,
    /// and then no substitution at all.
    /// </summary>
    /// <param name="connectionString">The connection string to consider.</param>
    /// <param name="isReachable">Reports whether a server is listening on the given host and port.</param>
    /// <returns>The connection string to use, and whether any host answered.</returns>
    internal static HostDecision Decide(string? connectionString, Func<string, uint, bool> isReachable)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new HostDecision(connectionString, AnyHostAnswered: false);
        }

        MySqlConnectionStringBuilder builder;
        try
        {
            builder = new MySqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException)
        {
            // A malformed connection string is the caller's to report, exactly as before this helper existed.
            return new HostDecision(connectionString, AnyHostAnswered: false);
        }

        var configuredHost = builder.Server;
        if (string.IsNullOrWhiteSpace(configuredHost)
            || IsLocalHostName(configuredHost)
            || configuredHost.Contains('\\', StringComparison.Ordinal)
            || configuredHost.Contains('/', StringComparison.Ordinal))
        {
            // Nothing to substitute: the string already targets this machine, or it targets a pipe/socket
            // path that "localhost" could not replace. Nothing is probed, so nothing is remembered either.
            return new HostDecision(connectionString, AnyHostAnswered: false);
        }

        if (isReachable(configuredHost, builder.Port))
        {
            // The configured host answered, which is the first host the rule tries and the answer while it
            // works.
            return new HostDecision(connectionString, AnyHostAnswered: true);
        }

        if (!isReachable(LocalHostName, builder.Port))
        {
            // Neither host answered, so the string is left alone and the caller's own failure path reports the
            // outage against the host the application is configured for. Nothing is remembered: the next
            // attempt checks again rather than inheriting this outage.
            return new HostDecision(connectionString, AnyHostAnswered: false);
        }

        builder.Server = LocalHostName;
        return new HostDecision(builder.ConnectionString, AnyHostAnswered: true);
    }

    /// <summary>
    /// A host decision: the connection string to use, and whether any host answered the reachability check.
    /// </summary>
    /// <param name="Resolved">The connection string to use.</param>
    /// <param name="AnyHostAnswered">
    /// Whether the configured host or this machine answered. <c>false</c> describes an outage rather than a
    /// host, and a decision like that is never remembered.
    /// </param>
    internal readonly record struct HostDecision(string? Resolved, bool AnyHostAnswered);

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
