using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Settings.Services.DependencyInjection;
using MTM_Waitlist.Module_Shared.Services.DependencyInjection;
using MTM_Waitlist.Module_Startup.Services.DependencyInjection;
using MTM_Waitlist.Module_Waitlist.Services.DependencyInjection;

namespace MTM_Waitlist.Module_Core.Services.DependencyInjection;

public static class CoreModuleDependencyInjectionExtensions
{
    public static IServiceCollection AddCoreModuleServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IAppModuleClock, AppModuleClock>();
        services.AddSingleton<IModuleCoreService, ModuleCoreService>();

        services.AddSettingsModuleServices(configuration);
        services.AddSharedModuleServices(configuration);
        services.AddStartupModuleServices(configuration);
        services.AddWaitlistModuleServices(configuration);

        return services;
    }
}
