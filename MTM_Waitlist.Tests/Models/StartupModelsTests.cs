using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Tests.Models;

[TestClass]
public sealed class StartupModelsTests
{
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
        Assert.AreEqual(string.Empty, state.CurrentRoleCode);
        Assert.IsFalse(state.IsUserMatched);
        Assert.IsFalse(state.IsComputerRegistered);
        Assert.IsFalse(state.IsSessionValid);
        Assert.AreEqual(StartupState.SessionTokenSourceNone, state.SessionTokenSource);
        Assert.IsFalse(state.RequireNewUserAction);
        Assert.AreEqual(string.Empty, state.LoginHint);
        Assert.IsFalse(state.IsDeveloper);

        state.CurrentRoleCode = "developer";

        Assert.IsTrue(state.IsDeveloper);
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