using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Module_Waitlist.Services.DependencyInjection;

public static class ModuleDependencyInjectionExtensions
{
    public static IServiceCollection AddWaitlistNewRequestServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<INewRequestFlowService, NewRequestFlowService>();
        services.AddSingleton<ICoilAvailabilityService, CoilAvailabilityService>();

        // The fuzzy incorrect-part search, which reads the plant system live and is never served from the mirror:
        // registering it here keeps the read's own composition beside the rest of this module's (FR-084, FR-085).
        services.AddSingleton<IPartMatchSearchService, PartMatchSearchService>();
        return services;
    }
}
