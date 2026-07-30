using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MTM_Waitlist.Tests.Module_Core;

[TestClass]
public class ModuleCoreServiceTests
{
    [TestMethod]
    public void AddCoreModuleServices_RegistersModuleCoreService()
    {
        var services = new ServiceCollection();
        services.AddCoreModuleServices(new ConfigurationBuilder().AddInMemoryCollection().Build());

        using var provider = services.BuildServiceProvider();
        var service = provider.GetService<IModuleCoreService>();

        Assert.IsNotNull(service);
        Assert.AreEqual("Module_Core", service!.GetModuleName());
    }
}
