using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using MTM_Waitlist.Module_Core.Contracts.Services;

namespace MTM_Waitlist.Module_Logging.Services.DependencyInjection;

/// <summary>
/// The logging module's registrations.
/// </summary>
/// <remarks>
/// <para>
/// Logging is deliberately not part of startup: the shell, Settings and the module libraries that never
/// reference the startup project all need it. This is the one place both logging surfaces are wired, so the
/// store has one writer and one queue rather than one per consumer.
/// </para>
/// <para>
/// <b>Nothing registered here writes to the machine.</b> There is no file provider, no rolling file sink and no
/// disk queue: what the machine keeps is what it needs to reach the store, and a diagnostic the store refuses is
/// dropped rather than substituted (FR-025, plan D27). The store writer is also the host's bounded shutdown
/// flush.
/// </para>
/// </remarks>
public static class ModuleDependencyInjectionExtensions
{
    /// <summary>
    /// Registers the seam, its writer, and the provider that serves every <c>ILogger</c> call site.
    /// </summary>
    /// <param name="services">The container.</param>
    /// <param name="configuration">The host configuration.</param>
    /// <returns>The same container, so registrations can be chained.</returns>
    public static IServiceCollection AddLoggingModuleServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // The writer is the only thing that touches the store, and it is also the host's bounded shutdown flush.
        // It is built by a factory rather than by the container's constructor selection so its queue capacity and
        // flush ceiling keep their stated defaults instead of being resolved as unregistered services.
        services.AddSingleton(sp => new StoreLogWriter(sp.GetRequiredService<IMySqlHelperServer>()));
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<StoreLogWriter>());

        // One seam instance behind both surfaces. One instance is one queue, which is what makes the entries in
        // the store read as a single history of what the application did.
        services.AddSingleton<LogService>();
        services.AddSingleton<ILogService>(sp => sp.GetRequiredService<LogService>());

        // Every ILogger call site in this application resolves its ILogger from this container, so registering the
        // provider is the whole of that surface's migration: no call site is edited (plan D5). The provider is
        // registered as an ILoggerProvider, which is how the container's ILoggerFactory collects its providers.
        services.AddSingleton<ILoggerProvider, StoreLoggerProvider>();

        return services;
    }
}

