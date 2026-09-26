using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using MTM_Waitlist.Module_Core.Contracts.Services;

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

        // The two identity contracts (FR-022). Both are registered as singletons under their own type as well as
        // under the contract, so the launch pipeline — the only writer — resolves the concrete type and every
        // consumer resolves the read-only interface, and both reach the same instance rather than two copies of
        // one person or one machine.
        services.AddSingleton<PersonIdentityService>();
        services.AddSingleton<IPersonIdentity>(sp => sp.GetRequiredService<PersonIdentityService>());
        services.AddSingleton<MachineFactsService>();
        services.AddSingleton<IMachineFacts>(sp => sp.GetRequiredService<MachineFactsService>());

        return services;
    }
}
