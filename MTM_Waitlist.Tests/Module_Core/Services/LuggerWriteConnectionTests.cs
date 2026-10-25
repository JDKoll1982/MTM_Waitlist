using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Services;

using MySqlConnector;

namespace MTM_Waitlist.Tests.Module_Core.Services;

/// <summary>
/// The connection the lugger arrangement writes through: the same host decision every other reader and writer makes,
/// and always the waitlist store (FR-001, FR-032).
/// </summary>
/// <remarks>
/// This is the defect that made every change to the arrangement fail on a workstation off the plant network: the
/// read path falls back to the local server, and the write path did not, so the page drew the arrangement and then
/// timed out on the first edit. The reachability check is handed in here, so both branches are proven without a
/// socket.
/// </remarks>
[TestClass]
public sealed class LuggerWriteConnectionTests
{
    private const string Configured =
        "Server=172.16.1.104;Port=3306;Database=mtm_waitlist;User ID=root;Password=secret;";

    [TestMethod]
    public void Service_LuggerWriteConnectionTests_UnreachableConfiguredHost_IsWrittenThroughTheLocalServer()
    {
        var resolved = LuggerWriteConnection.Resolve(Configured, (host, _) => host != "172.16.1.104");

        Assert.IsNotNull(resolved, "A configured store answered no connection string at all.");

        var builder = new MySqlConnectionStringBuilder(resolved);

        Assert.AreEqual(
            "localhost",
            builder.Server,
            "The write still points at the plant host the read path had to fall back from, so every change times out.");
        Assert.AreEqual(
            LuggerWriteConnection.DatabaseName,
            builder.Database,
            "The write is not aimed at the waitlist store.");
    }

    [TestMethod]
    public void Service_LuggerWriteConnectionTests_ReachableConfiguredHost_IsKept()
    {
        var resolved = LuggerWriteConnection.Resolve(Configured, (_, _) => true);

        Assert.IsNotNull(resolved);

        Assert.AreEqual(
            "172.16.1.104",
            new MySqlConnectionStringBuilder(resolved).Server,
            "A reachable plant host is replaced by the local server, which is not this rule.");
    }

    [TestMethod]
    public void Service_LuggerWriteConnectionTests_TheDatabase_IsAlwaysTheWaitlistStore()
    {
        var elsewhere = "Server=localhost;Port=3306;Database=mtm_mock;User ID=root;Password=secret;";

        var resolved = LuggerWriteConnection.Resolve(elsewhere, (_, _) => true);

        Assert.IsNotNull(resolved);

        Assert.AreEqual(
            LuggerWriteConnection.DatabaseName,
            new MySqlConnectionStringBuilder(resolved).Database,
            "An arrangement write could reach whatever store the connection string happened to name.");
    }

    [TestMethod]
    public void Service_LuggerWriteConnectionTests_NothingConfigured_ResolvesNothing()
    {
        Assert.IsNull(LuggerWriteConnection.Resolve(null, (_, _) => true), "A missing store resolves no connection.");
        Assert.IsNull(LuggerWriteConnection.Resolve("   ", (_, _) => true), "A blank store resolves no connection.");
    }
}
