using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Tests.Models;

[TestClass]
public sealed class StartupModelsTests
{
    /// <summary>
    /// The module-core options still bind to their defaults, which is the confirmation T156 asks for before either
    /// of the two option types is deleted: neither key appears in appsettings.json, and neither carries machine or
    /// person state, so they are left in place rather than deleted blind.
    /// </summary>
    [TestMethod]
    public void ModuleCoreSettingsOptions_Defaults()
    {
        var options = new ModuleCoreSettingsOptions();

        Assert.AreEqual(30, options.DefaultRefreshIntervalSeconds);
        Assert.IsTrue(options.EnableModuleDiagnostics);
    }
}