using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using MTM_Waitlist.Module_DevTools.Models;

namespace MTM_Waitlist.Module_DevTools.Services.DependencyInjection;

public static class ModuleDependencyInjectionExtensions
{
    public static IServiceCollection AddDevToolsModuleServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DevToolsDatabaseOptions>(configuration.GetSection(nameof(DevToolsDatabaseOptions)));
        services.AddSingleton<MTM_Waitlist.Module_Core.Contracts.Services.IAppModuleClock, MTM_Waitlist.Module_Core.Services.AppModuleClock>();
        services.AddSingleton<DevToolsModuleService>();
        services.AddSingleton<IDevToolsRequestTypeService, DevToolsRequestTypeService>();
        return services;
    }
}
