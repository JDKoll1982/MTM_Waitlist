using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Tests.Core.Models;

[TestClass]
public sealed class StartupOptionsModelsTests
{
    [TestMethod]
    public void StartupRegistrationRequest_Defaults()
    {
        var request = new StartupRegistrationRequest();

        Assert.AreEqual(string.Empty, request.Username);
        Assert.AreEqual(string.Empty, request.HostnameNormalized);
        Assert.AreEqual(string.Empty, request.MacAddressNormalized);
        Assert.AreEqual(default, request.RequestedUtc);
    }

    [TestMethod]
    public void StartupRegistrationRequest_CanBeInitialized()
    {
        var now = DateTimeOffset.UtcNow;
        var request = new StartupRegistrationRequest
        {
            Username = "johnk",
            HostnameNormalized = "JOHNSPC",
            MacAddressNormalized = "AA:BB",
            RequestedUtc = now
        };

        Assert.AreEqual("johnk", request.Username);
        Assert.AreEqual("JOHNSPC", request.HostnameNormalized);
        Assert.AreEqual("AA:BB", request.MacAddressNormalized);
        Assert.AreEqual(now, request.RequestedUtc);
    }

    [TestMethod]
    public void StartupDatabaseOptions_Defaults()
    {
        var options = new StartupDatabaseOptions();

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
    public void StartupDevelopmentOptions_Defaults()
    {
        var options = new StartupDevelopmentOptions();

        Assert.IsNotNull(options.DefaultDeveloperUsernames);
        Assert.AreEqual(0, options.DefaultDeveloperUsernames.Count);
    }

    [TestMethod]
    public void StartupWindowOptions_Defaults()
    {
        var options = new StartupWindowOptions();

        Assert.AreEqual(920, options.SplashWidth);
        Assert.AreEqual(620, options.SplashHeight);
        Assert.AreEqual(1600, options.MainWidth);
        Assert.AreEqual(980, options.MainHeight);
        Assert.IsTrue(options.CenterOnModeSwitch);
        Assert.AreEqual(120, options.MainTransitionDelayMilliseconds);
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

    [TestMethod]
    public void StartupCredentialCheckResult_Failed()
    {
        var result = StartupCredentialCheckResult.Failed();

        Assert.IsFalse(result.IsAuthenticated);
        Assert.IsFalse(result.RequiresPasswordChange);
        Assert.AreEqual(0, result.UserId);
        Assert.AreEqual(string.Empty, result.CurrentRole);
    }

    [TestMethod]
    public void StartupCredentialCheckResult_Success()
    {
        var result = StartupCredentialCheckResult.Success(42, "Developer", requiresPasswordChange: true);

        Assert.IsTrue(result.IsAuthenticated);
        Assert.IsTrue(result.RequiresPasswordChange);
        Assert.AreEqual(42, result.UserId);
        Assert.AreEqual("Developer", result.CurrentRole);
    }
}
