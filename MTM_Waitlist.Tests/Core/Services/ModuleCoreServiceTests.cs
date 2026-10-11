using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class ModuleCoreServiceTests
{
    [TestMethod]
    public void Service_ModuleCoreServiceTests_GetModuleName_ReturnsModuleCore()
    {
        var service = new ModuleCoreService();

        Assert.AreEqual("Module_Core", service.GetModuleName());
    }
}
