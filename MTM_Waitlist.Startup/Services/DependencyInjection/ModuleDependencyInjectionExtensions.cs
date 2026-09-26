using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MTM_Waitlist.Module_Startup.Services.DependencyInjection;

public static class ModuleDependencyInjectionExtensions
{
    /// <summary>
    /// The rebuilt startup module's registrations. The old surface's registrations were removed with it; each
    /// startup phase appends its own registrations here in phase order.
    /// </summary>
    public static IServiceCollection AddStartupModuleServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        return services;
    }
}
