using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Module_Core.Services.DependencyInjection;

public static class CoreModuleDependencyInjectionExtensions
{
    public static IServiceCollection AddCoreModuleServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IAppModuleClock, AppModuleClock>();
        services.AddSingleton<IModuleCoreService, ModuleCoreService>();
        return services;
    }
}
