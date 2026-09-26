using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MTM_Waitlist.Module_Logging.Services.DependencyInjection;

/// <summary>
/// The logging module's registrations.
/// </summary>
/// <remarks>
/// Logging is deliberately not part of startup: the shell, Settings and the module libraries that never
/// reference <c>MTM_Waitlist.Startup</c> all need it. The seam and the store writer land in the feature's
/// logging phase; this is the project and the one registration point they are added through.
/// </remarks>
public static class ModuleDependencyInjectionExtensions
{
    public static IServiceCollection AddLoggingModuleServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        return services;
    }
}
