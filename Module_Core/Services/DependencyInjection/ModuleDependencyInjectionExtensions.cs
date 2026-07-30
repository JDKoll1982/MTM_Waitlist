using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_DevTools.Services.DependencyInjection;
using MTM_Waitlist.Module_Reporting.Services.DependencyInjection;
using MTM_Waitlist.Module_Settings.Services.DependencyInjection;
using MTM_Waitlist.Module_Shared.Services.DependencyInjection;
using MTM_Waitlist.Module_Startup.Services.DependencyInjection;
using MTM_Waitlist.Module_Waitlist.Services.DependencyInjection;

namespace MTM_Waitlist.Module_Core.Services.DependencyInjection;

public static class ModuleDependencyInjectionExtensions
{
    public static IServiceCollection AddModuleServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ModuleCoreSettingsOptions>(configuration.GetSection(nameof(ModuleCoreSettingsOptions)));
        services.AddCoreModuleServices(configuration);
        services.AddSharedModuleServices(configuration);
        services.AddWaitlistModuleServices(configuration);
        services.AddSettingsModuleServices(configuration);
        services.AddStartupModuleServices(configuration);
        services.AddReportingModuleServices(configuration);
        services.AddDevToolsModuleServices(configuration);
        return services;
    }
}
