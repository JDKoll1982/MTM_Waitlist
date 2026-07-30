using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Tests.Models;

[TestClass]
public sealed class StartupModelsTests
{
    [TestMethod]
    public void StartupResult_SuccessAndBlockedFactories_SetExpectedValues()
    {
        var success = StartupResult.Success(typeof(WaitlistViewViewModel).FullName!, "Ready");
        var blocked = StartupResult.Blocked("Missing config");

        Assert.IsTrue(success.IsSuccess);
        Assert.IsFalse(success.IsBlocked);
        Assert.AreEqual(typeof(WaitlistViewViewModel).FullName, success.RouteTarget);
        Assert.AreEqual("Ready", success.StatusMessage);

        Assert.IsFalse(blocked.IsSuccess);
        Assert.IsTrue(blocked.IsBlocked);
        Assert.AreEqual(string.Empty, blocked.RouteTarget);
        Assert.AreEqual("Missing config", blocked.StatusMessage);
    }

    [TestMethod]
    public void StartupState_DefaultValues_AndDeveloperCheckWork()
    {
        var state = new StartupState();

        Assert.IsTrue(state.IsBusy);
        Assert.AreEqual("Preparing startup checks...", state.StatusText);
        Assert.IsFalse(state.ConfigurationLoaded);
        Assert.AreEqual(string.Empty, state.Username);
        Assert.AreEqual(string.Empty, state.ConfigurationFolder);
        Assert.AreEqual(string.Empty, state.ConfigurationFile);
        Assert.AreEqual(string.Empty, state.HostnameNormalized);
        Assert.AreEqual(string.Empty, state.MacAddressNormalized);
        Assert.AreEqual(string.Empty, state.CurrentRole);
        Assert.IsFalse(state.IsUserMatched);
        Assert.IsFalse(state.IsWorkstationRegistered);
        Assert.IsFalse(state.IsSessionValid);
        Assert.AreEqual(StartupState.SessionTokenSourceNone, state.SessionTokenSource);
        Assert.IsFalse(state.RequireNewUserAction);
        Assert.AreEqual(string.Empty, state.LoginHint);
        Assert.IsFalse(state.IsDeveloper);

        state.CurrentRole = "Developer";

        Assert.IsTrue(state.IsDeveloper);
    }

    [TestMethod]
    public void StartupWindowOptions_DefaultValuesMatchStartupLayout()
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
    public void LocalSettingsOptions_AcceptsConfiguredValues()
    {
        var options = new LocalSettingsOptions
        {
            ApplicationDataFolder = "MTM_Waitlist/ApplicationData",
            LocalSettingsFile = "LocalSettings.json"
        };

        Assert.AreEqual("MTM_Waitlist/ApplicationData", options.ApplicationDataFolder);
        Assert.AreEqual("LocalSettings.json", options.LocalSettingsFile);
    }
}