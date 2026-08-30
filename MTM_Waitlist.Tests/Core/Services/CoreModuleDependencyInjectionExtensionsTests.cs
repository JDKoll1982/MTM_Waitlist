using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Core.Services.DependencyInjection;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class CoreModuleDependencyInjectionExtensionsTests
{
    [TestMethod]
    public void AddCoreModuleServices_RegistersAppModuleClock()
    {
        var services = new ServiceCollection();
        services.AddCoreModuleServices(new ConfigurationBuilder().Build());
        var provider = services.BuildServiceProvider();

        Assert.IsInstanceOfType<AppModuleClock>(provider.GetRequiredService<IAppModuleClock>());
    }

    [TestMethod]
    public void AddCoreModuleServices_RegistersModuleCoreService()
    {
        var services = new ServiceCollection();
        services.AddCoreModuleServices(new ConfigurationBuilder().Build());
        var provider = services.BuildServiceProvider();

        Assert.IsInstanceOfType<ModuleCoreService>(provider.GetRequiredService<IModuleCoreService>());
    }

    [TestMethod]
    public void AddCoreModuleServices_RegistersAppModuleClockAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddCoreModuleServices(new ConfigurationBuilder().Build());
        var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<IAppModuleClock>();
        var second = provider.GetRequiredService<IAppModuleClock>();

        Assert.AreSame(first, second);
    }

    [TestMethod]
    public void AddCoreModuleServices_ReturnsSameCollection()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var result = services.AddCoreModuleServices(configuration);

        Assert.AreSame(services, result);
    }
}
