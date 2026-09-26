using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Tests.Core.Models;

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
    public void StartupLoggingOptions_Defaults()
    {
        var options = new StartupLoggingOptions();

        Assert.AreEqual("Startup.Logging.CentralizedDestination", StartupLoggingOptions.CentralizedDestinationSettingKey);
        Assert.AreEqual("Startup.Logging.HostedVmLogDirectory", StartupLoggingOptions.HostedVmLogDirectorySettingKey);
        Assert.AreEqual("MTM_Waitlist/Logs/Startup", options.HostedVmLogDirectory);
        Assert.AreEqual(string.Empty, options.CentralizedDestination);
        Assert.AreEqual(14, options.RetentionDays);
        Assert.AreEqual(250, options.MaxDirectorySizeMb);
        Assert.AreEqual(4096, options.ChannelCapacity);
        Assert.AreEqual(2, options.ForwardRetryCount);
    }

    [TestMethod]
    public void ModuleCoreSettingsOptions_Defaults()
    {
        var options = new ModuleCoreSettingsOptions();

        Assert.AreEqual(30, options.DefaultRefreshIntervalSeconds);
        Assert.IsTrue(options.EnableModuleDiagnostics);
    }

    [TestMethod]
    public void LocalSettingsOptions_Defaults()
    {
        var options = new LocalSettingsOptions();

        Assert.IsNull(options.ApplicationDataFolder);
        Assert.IsNull(options.LocalSettingsFile);
    }
}
