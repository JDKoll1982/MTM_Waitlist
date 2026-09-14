using System.Diagnostics;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Mock.Services;

namespace MTM_Waitlist.Tests.Module_Mock;

/// <summary>
/// Verifies that an Infor Visual host this machine cannot reach costs the application a short, bounded
/// wait — the defect behind the "SQL errors and the app locks up on every view change" report.
/// </summary>
/// <remarks>
/// <para>
/// Measured on the reporting workstation (2026-09-12): each probe of an absent Infor Visual server took
/// ~11.0 s. The application's own log bounds it exactly — the probe host starts and the
/// <c>sp_visual_read_shape_freshness_get</c> that follows the probe lands 11.07 s and 11.04 s later — and
/// the unlogged bursts of first-chance <c>SocketException</c>/<c>SqlException</c> are that probe, since
/// <see cref="VisualConnectivityProbe"/> is the only Infor Visual caller that logs nothing.
/// </para>
/// <para>
/// Two defaults caused the cost, both read from the shipped assembly rather than assumed:
/// <c>Connect Retry Count = 1</c> with <c>Connect Retry Interval = 10</c> seconds (SqlClient's blind
/// retry), on top of the configured 10-second connect timeout. A probe runs on a timer, so the retry is
/// pure cost, and the timeout must not be allowed to exceed what a reachability question deserves.
/// </para>
/// <para>
/// These checks are deliberately network-free apart from the last one, which asserts a bound and so
/// passes whether the host is refused instantly or black-holed.
/// </para>
/// </remarks>
[TestClass]
public sealed class VisualUnreachableFastFailTests
{
    private const string ConnectionStringEnvironmentVariable = "INFOR_VISUAL_SQL_CONNECTION_STRING";
    private const string ServerEnvironmentVariable = "INFOR_VISUAL_SQL_SERVER";
    private const string DatabaseEnvironmentVariable = "INFOR_VISUAL_SQL_DATABASE";
    private const string UserEnvironmentVariable = "INFOR_VISUAL_SQL_USER";
    private const string PasswordEnvironmentVariable = "INFOR_VISUAL_SQL_PASSWORD";

    /// <summary>
    /// TEST-NET-1, reserved for documentation. Nothing answers, so a connection attempt is either
    /// refused or black-holed — both of which the probe must report quickly.
    /// </summary>
    private const string NonRoutableServer = "192.0.2.1,1433";

    private static readonly string[] s_connectionEnvironmentVariables =
    [
        ConnectionStringEnvironmentVariable,
        ServerEnvironmentVariable,
        DatabaseEnvironmentVariable,
        UserEnvironmentVariable,
        PasswordEnvironmentVariable,
    ];

    private readonly Dictionary<string, string?> _originalEnvironment = new(StringComparer.OrdinalIgnoreCase);

    [TestInitialize]
    public void TestInitialize()
    {
        foreach (var name in s_connectionEnvironmentVariables)
        {
            _originalEnvironment[name] = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, null);
        }
    }

    [TestCleanup]
    public void TestCleanup()
    {
        foreach (var entry in _originalEnvironment)
        {
            Environment.SetEnvironmentVariable(entry.Key, entry.Value);
        }
    }

    [TestMethod]
    public void Resolve_WithACompleteConnectionStringInTheEnvironment_DisablesTheBlindConnectRetry()
    {
        Environment.SetEnvironmentVariable(
            ConnectionStringEnvironmentVariable,
            "Server=VISUAL;Database=MTMFG;User ID=SHOP2;Password=SHOP;TrustServerCertificate=true;Encrypt=false;Connect Timeout=10");

        var resolved = new SqlConnectionStringBuilder(new VisualConnectionStringProvider().Resolve());

        Assert.AreEqual(0, resolved.ConnectRetryCount, "A timer-driven probe must not pay SqlClient's blind connect retry.");
        Assert.AreEqual("VISUAL", resolved.DataSource, "Normalizing must not lose the configured server.");
        Assert.AreEqual("MTMFG", resolved.InitialCatalog, "Normalizing must not lose the configured database.");
        Assert.AreEqual(10, resolved.ConnectTimeout, "The caller's own connect timeout is theirs to choose.");
    }

    [TestMethod]
    public void Resolve_WithACompleteConnectionStringThatAsksForRetries_StillDisablesThem()
    {
        Environment.SetEnvironmentVariable(
            ConnectionStringEnvironmentVariable,
            "Server=VISUAL;Database=MTMFG;User ID=SHOP2;Password=SHOP;Connect Retry Count=3;Connect Timeout=15");

        var resolved = new SqlConnectionStringBuilder(new VisualConnectionStringProvider().Resolve());

        Assert.AreEqual(0, resolved.ConnectRetryCount, "The connection string the application opens never retries a dead host.");
    }

    [TestMethod]
    public void Resolve_WithAnUnparseableConnectionStringInTheEnvironment_ReturnsItUnchangedWithoutThrowing()
    {
        const string Unparseable = "Server=VISUAL;NotAKeywordThisProviderKnows=1";
        Environment.SetEnvironmentVariable(ConnectionStringEnvironmentVariable, Unparseable);

        string? resolved = null;
        try
        {
            resolved = new VisualConnectionStringProvider().Resolve();
        }
        catch (Exception exception)
        {
            Assert.Fail($"Resolving an override the provider cannot parse must not throw; it threw {exception.GetType().Name}.");
        }

        Assert.AreEqual(
            Unparseable,
            resolved,
            "An unparseable override is passed through so the connection reports its own failure rather than the provider throwing.");
    }

    [TestMethod]
    public void Resolve_FromConfiguration_DisablesTheBlindConnectRetryAndKeepsTheConfiguredTimeout()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["InforVisualDatabaseOptions:Server"] = "VISUAL",
                ["InforVisualDatabaseOptions:Database"] = "MTMFG",
                ["InforVisualDatabaseOptions:User"] = "SHOP2",
                ["InforVisualDatabaseOptions:Password"] = "SHOP",
                ["InforVisualDatabaseOptions:ConnectionTimeoutSeconds"] = "10",
            })
            .Build();

        var resolved = new SqlConnectionStringBuilder(new VisualConnectionStringProvider(configuration).Resolve());

        Assert.AreEqual(0, resolved.ConnectRetryCount, "The configuration path must not leave the retry at its default of one.");
        Assert.AreEqual(10, resolved.ConnectTimeout, "The configured connect timeout is preserved.");
    }

    [TestMethod]
    public void BuildProbeConnectionString_DisablesTheBlindConnectRetry()
    {
        var prepared = new SqlConnectionStringBuilder(
            VisualConnectivityProbe.BuildProbeConnectionString(
                "Server=VISUAL;Database=MTMFG;User ID=SHOP2;Password=SHOP;Connect Retry Count=1;Connect Timeout=10"));

        Assert.AreEqual(0, prepared.ConnectRetryCount, "The probe never retries.");
        Assert.AreEqual("VISUAL", prepared.DataSource, "The probe reuses the configured server.");
    }

    [TestMethod]
    public void BuildProbeConnectionString_ShortensALongerConnectTimeoutToTheProbeBound()
    {
        var prepared = new SqlConnectionStringBuilder(
            VisualConnectivityProbe.BuildProbeConnectionString(
                "Server=VISUAL;Database=MTMFG;User ID=SHOP2;Password=SHOP;Connect Timeout=30"));

        Assert.AreEqual(
            VisualConnectivityProbe.ProbeConnectTimeoutSeconds,
            prepared.ConnectTimeout,
            "Answering 'is Infor Visual reachable right now?' may not cost a full data-read timeout.");
    }

    [TestMethod]
    public void BuildProbeConnectionString_KeepsAShorterCallerConnectTimeout()
    {
        var prepared = new SqlConnectionStringBuilder(
            VisualConnectivityProbe.BuildProbeConnectionString(
                "Server=VISUAL;Database=MTMFG;User ID=SHOP2;Password=SHOP;Connect Timeout=1"));

        Assert.AreEqual(1, prepared.ConnectTimeout, "A caller asking for less than the probe bound keeps their shorter timeout.");
    }

    [TestMethod]
    public void BoundConnectAttempt_ShortensALongerConfiguredTimeoutToTheProbeBound()
    {
        var prepared = new SqlConnectionStringBuilder(
            VisualQueryExecutor.BoundConnectAttempt(
                "Server=VISUAL;Database=MTMFG;User ID=SHOP2;Password=SHOP;Connect Timeout=10"));

        Assert.AreEqual(
            VisualConnectivityProbe.ProbeConnectTimeoutSeconds,
            prepared.ConnectTimeout,
            "A read that is going to fall back may not spend the data-read timeout discovering the source is down.");
    }

    [TestMethod]
    public void BoundConnectAttempt_KeepsAShorterCallerConnectTimeout()
    {
        var prepared = new SqlConnectionStringBuilder(
            VisualQueryExecutor.BoundConnectAttempt(
                "Server=VISUAL;Database=MTMFG;User ID=SHOP2;Password=SHOP;Connect Timeout=1"));

        Assert.AreEqual(1, prepared.ConnectTimeout, "A caller asking for less than the read bound keeps their shorter timeout.");
    }

    [TestMethod]
    public void BoundConnectAttempt_WithAnUnparseableValue_IsReturnedUnchangedRatherThanThrowing()
    {
        const string Unparseable = "Server=VISUAL;NotAKeywordThisProviderKnows=1";

        Assert.AreEqual(
            Unparseable,
            VisualQueryExecutor.BoundConnectAttempt(Unparseable),
            "Bounding is best-effort: the fault must surface from the connection attempt, not from here.");
    }

    [TestMethod]
    public async Task ExecuteAsync_WithAnUnreachableHost_GivesUpWithinTheReadBound()
    {
        Environment.SetEnvironmentVariable(
            ConnectionStringEnvironmentVariable,
            $"Server={NonRoutableServer};Database=MTMFG;User ID=SHOP2;Password=SHOP;TrustServerCertificate=true;Encrypt=false;Connect Timeout=30");

        var executor = new VisualQueryExecutor(new StubScriptStore(), new VisualConnectionStringProvider());
        var stopwatch = Stopwatch.StartNew();

        var outcome = await executor.ExecuteAsync(
            "Database/InforVisual/Queues/Module_Setup/Queries/LookupWorkOrder.sql",
            new Dictionary<string, object?>());

        stopwatch.Stop();
        Assert.AreEqual(
            VisualQueryStatus.Unreachable,
            outcome.Status,
            "An absent host is unreachability, which is what routes the read to the mtm_mock mirror.");
        Assert.IsTrue(
            stopwatch.Elapsed < TimeSpan.FromSeconds(6),
            $"A read that is going to be served from the mirror must not spend the configured data-read timeout first; it took {stopwatch.Elapsed.TotalSeconds:F1} s.");
    }

    [TestMethod]
    public async Task ProbeAsync_WithNoConnectionConfigured_ReturnsFalse()
    {
        var probe = new VisualConnectivityProbe(new VisualConnectionStringProvider());

        Assert.IsFalse(await probe.ProbeAsync(), "An unconfigured source is not reachable.");
    }

    [TestMethod]
    public async Task ProbeAsync_WithAnUnreachableHost_GivesUpWithinTheProbeBound()
    {
        Environment.SetEnvironmentVariable(
            ConnectionStringEnvironmentVariable,
            $"Server={NonRoutableServer};Database=MTMFG;User ID=SHOP2;Password=SHOP;TrustServerCertificate=true;Encrypt=false;Connect Timeout=30");

        var probe = new VisualConnectivityProbe(new VisualConnectionStringProvider());
        var stopwatch = Stopwatch.StartNew();

        var reachable = await probe.ProbeAsync();

        stopwatch.Stop();
        Assert.IsFalse(reachable, "Nothing answers on TEST-NET-1, so the probe reports unreachable.");
        Assert.IsTrue(
            stopwatch.Elapsed < TimeSpan.FromSeconds(6),
            $"A probe must not hold the read-status loop for a connect timeout plus a blind retry; it took {stopwatch.Elapsed.TotalSeconds:F1} s.");
    }

    /// <summary>Supplies a script without touching the content root, so a bound can be measured in isolation.</summary>
    private sealed class StubScriptStore : IInforVisualScriptStore
    {
        public Task<string> LoadAsync(string relativePath, CancellationToken cancellationToken = default) =>
            Task.FromResult("SELECT 1 AS Probe");
    }
}
