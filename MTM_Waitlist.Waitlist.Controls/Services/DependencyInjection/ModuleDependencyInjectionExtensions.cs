using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MTM_Waitlist.Module_Waitlist.Services.DependencyInjection;

public static class ModuleDependencyInjectionExtensions
{
    public static IServiceCollection AddWaitlistControlsServices(this IServiceCollection services, IConfiguration configuration)
    {
        // The request-type controls are passive view models/models; no service registrations are required.
        return services;
    }
}
