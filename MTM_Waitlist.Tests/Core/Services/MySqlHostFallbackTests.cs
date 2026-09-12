using Microsoft.VisualStudio.TestTools.UnitTesting;

using MySqlConnector;

using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

/// <summary>
/// Verifies the configured-host → local-host substitution rule: the configured server when it answers,
/// otherwise the local server when that answers, otherwise nothing changes.
/// </summary>
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
}
