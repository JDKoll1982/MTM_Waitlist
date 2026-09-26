using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Tests.Models;

[TestClass]
public sealed class StartupModelsTests
{
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