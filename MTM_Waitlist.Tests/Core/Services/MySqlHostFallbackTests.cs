using System.Net;
using System.Net.Sockets;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MySqlConnector;

using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

/// <summary>
/// Verifies the configured-host → local-host substitution rule: the configured server when it answers,
/// otherwise the local server when that answers, otherwise nothing changes.
/// </summary>
/// <remarks>
/// The tests that decide which host to use call the overload that takes the reachability check, so no socket is
/// opened and no decision is remembered. The two that exercise the remembering itself call
/// <see cref="MySqlHostFallback.Apply(string?)"/> against a real listener on this machine, each with its own
/// port so one test's decision cannot answer the other's question.
/// </remarks>
[TestClass]
public sealed class MySqlHostFallbackTests
{
    private const string ConfiguredConnectionString =
        "Server=172.16.1.104;Port=3306;Database=mtm_waitlist;User Id=root;Password=root;";

    [TestMethod]
    public void Apply_WhenTheConfiguredServerAnswers_LeavesTheConnectionStringAlone()
    {
        var result = MySqlHostFallback.Apply(ConfiguredConnectionString, (host, _) => host == "172.16.1.104");

        Assert.AreEqual(ConfiguredConnectionString, result);
    }

    [TestMethod]
    public void Apply_WhenOnlyTheLocalServerAnswers_PointsAtLocalhostAndKeepsEverythingElse()
    {
        var result = MySqlHostFallback.Apply(
            ConfiguredConnectionString,
            (host, _) => host == MySqlHostFallback.LocalHostName);

        Assert.IsNotNull(result);
        var builder = new MySqlConnectionStringBuilder(result);
        Assert.AreEqual(MySqlHostFallback.LocalHostName, builder.Server);
        Assert.AreEqual("mtm_waitlist", builder.Database);
        Assert.AreEqual("root", builder.UserID);
        Assert.AreEqual(3306u, builder.Port);
    }

    [TestMethod]
    public void Apply_WhenNeitherServerAnswers_LeavesTheConnectionStringAlone()
    {
        // Failing is the caller's job: this helper must not invent a target, so the operation fails and is
        // reported exactly as it was before the fallback existed.
        var result = MySqlHostFallback.Apply(ConfiguredConnectionString, (_, _) => false);

        Assert.AreEqual(ConfiguredConnectionString, result);
    }

    [TestMethod]
    public void Apply_WhenTheStringAlreadyTargetsThisMachine_DoesNotProbeAtAll()
    {
        const string localConnectionString = "Server=localhost;Port=3306;Database=mtm_mock;User Id=root;Password=root;";
        var probes = 0;

        var result = MySqlHostFallback.Apply(
            localConnectionString,
            (_, _) =>
            {
                probes++;
                return false;
            });

        Assert.AreEqual(localConnectionString, result);
        Assert.AreEqual(0, probes, "A string that already targets this machine needs no reachability check.");
    }

    [TestMethod]
    public void Apply_WhenThereIsNothingToResolve_ReturnsTheInputUnchanged()
    {
        Assert.IsNull(MySqlHostFallback.Apply(null, (_, _) => true));
        Assert.AreEqual(string.Empty, MySqlHostFallback.Apply(string.Empty, (_, _) => true));
        Assert.AreEqual("   ", MySqlHostFallback.Apply("   ", (_, _) => true));
    }

    [TestMethod]
    public void ShouldRemember_WhenNeitherServerAnswered_SaysNo()
    {
        // A decision that describes an outage is not a verdict, so it must not be remembered: the retry a
        // surface offers after the person switches the local server on has to check again rather than inherit
        // the outage and fail the same way.
        var outage = MySqlHostFallback.Decide(ConfiguredConnectionString, (_, _) => false);

        Assert.IsFalse(
            MySqlHostFallback.ShouldRemember(outage),
            "Nothing answered, so the next attempt has to probe again.");

        var answered = MySqlHostFallback.Decide(
            ConfiguredConnectionString,
            (host, _) => host == MySqlHostFallback.LocalHostName);

        Assert.IsTrue(
            MySqlHostFallback.ShouldRemember(answered),
            "A host answered, so the decision may be reused for the cache's window.");
    }

    [TestMethod]
    public void Apply_AfterAnOutage_ChecksAgainSoALocalServerStartedLaterIsFound()
    {
        var port = ReserveFreePort();
        var configured = $"Server=127.0.0.2;Port={port};Database=mtm_waitlist;User Id=root;Password=root;";
        using var localServer = new LoopbackServer(port);

        // Nothing answers yet: 127.0.0.2 is a loopback address with no listener on it, and the local server
        // has not been started. The string is left alone for the caller's failure path to report.
        Assert.AreEqual(configured, MySqlHostFallback.Apply(configured));

        // The person switches the local server on and asks again, which is the retry a stop offers. The
        // outage was not remembered, so this attempt checks again and finds this machine.
        localServer.Start();

        var resolved = MySqlHostFallback.Apply(configured);

        Assert.IsNotNull(resolved);
        Assert.AreEqual(
            MySqlHostFallback.LocalHostName,
            new MySqlConnectionStringBuilder(resolved).Server,
            "A decision where nothing answered is not a verdict, so the next attempt checks again.");
    }

    [TestMethod]
    public void Apply_WhenAHostAnswered_RemembersTheDecisionForItsWindow()
    {
        var port = ReserveFreePort();
        var configured = $"Server=127.0.0.2;Port={port};Database=mtm_waitlist;User Id=root;Password=root;";
        using var localServer = new LoopbackServer(port);

        localServer.Start();

        Assert.AreEqual(
            MySqlHostFallback.LocalHostName,
            new MySqlConnectionStringBuilder(MySqlHostFallback.Apply(configured)!).Server);

        // The local server goes away, and the remembered decision still stands inside its window: a burst of
        // operations in one startup must not re-probe per read.
        localServer.Stop();

        Assert.AreEqual(
            MySqlHostFallback.LocalHostName,
            new MySqlConnectionStringBuilder(MySqlHostFallback.Apply(configured)!).Server,
            "A decision a host answered is remembered, so reads in the same startup do not re-probe.");
    }

    /// <summary>A port nothing is listening on, reserved by binding it and letting it go again.</summary>
    private static int ReserveFreePort()
    {
        var reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.Start();
        var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
        reservation.Stop();
        return port;
    }

    /// <summary>
    /// A server listening on this machine's loopback, on both families, so "localhost" finds it whichever
    /// address that name resolves to first. It is started and stopped by the test, which is how an outage is
    /// staged and then removed.
    /// </summary>
    private sealed class LoopbackServer : IDisposable
    {
        private readonly TcpListener _ipv4;
        private readonly TcpListener _ipv6;

        public LoopbackServer(int port)
        {
            _ipv4 = new TcpListener(IPAddress.Loopback, port);
            _ipv6 = new TcpListener(IPAddress.IPv6Loopback, port);
        }

        public void Start()
        {
            _ipv4.Start();
            _ipv6.Start();
        }

        public void Stop()
        {
            _ipv4.Stop();
            _ipv6.Stop();
        }

        public void Dispose() => Stop();
    }
}
