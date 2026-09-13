using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MTM_Waitlist.Module_Waitlist.Services.DependencyInjection;

public static class ModuleDependencyInjectionExtensions
{
    public static IServiceCollection AddWaitlistControlsServices(this IServiceCollection services, IConfiguration configuration)
    {
        // The per-type request controls and their models/view models are retired (FR-003, FR-006): every Item
        // renders the one card. Nothing is left to register, so this extension stays as the composition seam
        // it always was rather than being removed — the module still participates in the host's DI graph.
        return services;
    }
}
