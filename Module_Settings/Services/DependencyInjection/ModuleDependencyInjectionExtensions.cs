using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Module_Settings.Services.DependencyInjection;

public static class ModuleDependencyInjectionExtensions
{
    public static IServiceCollection AddSettingsModuleServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<MTM_Waitlist.Module_Core.Contracts.Services.IAppModuleClock, MTM_Waitlist.Module_Core.Services.AppModuleClock>();
        services.AddSingleton<SettingsModuleService>();
        return services;
    }
}
