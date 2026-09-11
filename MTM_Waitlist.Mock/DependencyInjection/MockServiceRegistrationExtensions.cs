using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Mock.Services;

namespace MTM_Waitlist.Mock.DependencyInjection;

/// <summary>
/// Registers the in-app Infor Visual read fallback.
/// </summary>
/// <remarks>
/// The detector, the read-status surface, its probe driver, the freshness read, and the five shape fallbacks
/// are registered here rather than in the application so the library owns its own composition, and so a host
/// that only needs the cache read can opt in. The fallbacks resolve the mirror through whatever
/// <c>IMySqlHelperServer</c> the host has already registered, so this package never constructs database
/// access itself.
/// <para>
/// The probe host is <b>probing only</b>: the application never performs a scheduled refresh, because
/// refresh scheduling is owned by the on-host service (FR-025). The host is started by the application, not
/// by this registration.
/// </para>
/// </remarks>
public static class MockServiceRegistrationExtensions
{
    /// <summary>
    /// Adds the reachability detector, the read-status surface, the probe driver, and the five
    /// <c>IVisualReadFallback</c> implementations.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddVisualReadFallback(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Live-read plumbing, owned by this library instead of duplicated per module.
        services.AddSingleton<IInforVisualScriptStore>(_ => new InforVisualScriptStore());
        services.AddSingleton<IVisualConnectionStringProvider>(provider =>
            new VisualConnectionStringProvider(provider.GetService<IConfiguration>()));
        services.AddSingleton<IVisualConnectivityProbe, VisualConnectivityProbe>();
        services.AddSingleton<IVisualQueryExecutor, VisualQueryExecutor>();
        services.AddSingleton<IVisualReachabilityDetector, VisualReachabilityDetector>();

        // Read-status surface: the freshness read and the provider the indicator renders.
        services.AddSingleton<IVisualShapeFreshnessReader, MySqlVisualShapeFreshnessReader>();
        services.AddSingleton<ReadStatusProvider>(provider => new ReadStatusProvider(
            provider.GetRequiredService<IVisualReachabilityDetector>(),
            provider.GetService<IVisualShapeFreshnessReader>()));
        services.AddSingleton<IReadStatusProvider>(provider => provider.GetRequiredService<ReadStatusProvider>());

        // The probe driver. Without it nothing probes, and the state can never leave Unknown (T123); it is
        // probing-only, because refresh scheduling belongs to the on-host service (FR-025).
        services.AddSingleton<IVisualReachabilityProbeHost>(provider => new VisualReachabilityProbeHost(
            provider.GetRequiredService<IVisualReachabilityDetector>(),
            provider.GetRequiredService<ReadStatusProvider>()));

        // One fallback per read shape. A sixth shape adds one line here and changes nothing else.
        services.AddSingleton<IVisualReadFallback<VisualWorkOrderLookupRequest, VisualWorkOrderLookupRow>, VisualWorkOrderLookupFallback>();
        services.AddSingleton<IVisualReadFallback<VisualOperationSequenceRequest, VisualOperationSequenceRow>, VisualOperationSequencesFallback>();
        services.AddSingleton<IVisualReadFallback<VisualSubordinatePartRequest, VisualSubordinatePartRow>, VisualSubordinatePartsFallback>();
        services.AddSingleton<IVisualReadFallback<VisualInventoryLocationRequest, VisualInventoryLocationRow>, VisualInventoryLocationsFallback>();
        services.AddSingleton<IVisualReadFallback<VisualDispositionInputRequest, VisualDispositionInputRow>, VisualDispositionInputFallback>();

        return services;
    }
}
