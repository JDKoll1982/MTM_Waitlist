using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Tests.Core.Models;

/// <summary>
/// The option models the launch and the database read still carry: their defaults, which are the values a host
/// with no configuration runs on.
/// </summary>
/// <remarks>
/// <para>
/// <b>What this file no longer asserts, and what replaced it.</b> It held a
/// <c>StartupLoggingOptions_Defaults</c> case for the file-based logging path's options, whose defaults named a
/// local log directory, a retention window and a forwarded copy's destination. Those options, the log service
/// that read them and the forwarder that wrote a file were all deleted, so there is no default left to assert:
/// the destination is the store, and it is chosen by the store-backed seam rather than configured. What a reader
/// once learned from this case is now proved by <c>MTM_Waitlist.Tests/Module_Logging</c>, which pins the seam and
/// its provider, and by <c>StoreLogCoverageTests</c>, which pins that a launch records an entry through that seam
/// and that no local log file appears.
/// </para>
/// </remarks>
[TestClass]
public sealed class StartupOptionsModelsTests
{
    [TestMethod]
    public void WaitlistDatabaseOptions_Defaults()
    {
        var options = new WaitlistDatabaseOptions();

        Assert.AreEqual("MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING", options.ConnectionStringEnvironmentVariable);
        Assert.AreEqual(string.Empty, options.ConnectionString);
        Assert.AreEqual(10, options.ConnectionTimeoutSeconds);
        Assert.AreEqual(2, options.MaxRetryCount);
        Assert.AreEqual(500, options.RetryBaseDelayMilliseconds);
    }

    [TestMethod]
    public void ModuleCoreSettingsOptions_Defaults()
    {
        var options = new ModuleCoreSettingsOptions();

        Assert.AreEqual(30, options.DefaultRefreshIntervalSeconds);
        Assert.IsTrue(options.EnableModuleDiagnostics);
    }
}
