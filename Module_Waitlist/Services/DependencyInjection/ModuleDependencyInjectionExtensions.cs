using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Module_Waitlist.Services.DependencyInjection;

public static class ModuleDependencyInjectionExtensions
{
    public static IServiceCollection AddWaitlistModuleServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<MTM_Waitlist.Module_Core.Contracts.Services.IAppModuleClock, MTM_Waitlist.Module_Core.Services.AppModuleClock>();
        services.AddSingleton<WaitlistModuleService>();
        return services;
    }
}
