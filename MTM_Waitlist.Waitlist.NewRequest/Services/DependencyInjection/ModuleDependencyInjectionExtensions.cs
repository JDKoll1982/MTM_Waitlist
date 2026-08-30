using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Module_Waitlist.Services.DependencyInjection;

public static class ModuleDependencyInjectionExtensions
{
    public static IServiceCollection AddWaitlistNewRequestServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<INewRequestFlowService, NewRequestFlowService>();
        return services;
    }
}
