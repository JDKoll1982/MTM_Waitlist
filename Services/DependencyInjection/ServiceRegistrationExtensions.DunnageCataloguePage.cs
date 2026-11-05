using Microsoft.Extensions.DependencyInjection;

using MTM_Waitlist.GenericRequest.ViewModels;

namespace MTM_Waitlist.Services.DependencyInjection;

/// <summary>
/// The generic request's dunnage page and its route (018, FR-001).
/// </summary>
/// <remarks>
/// The page moved to the application project, so its route is contributed here rather than written into the
/// composition root's factory. The key is the view model's full name, which is what the flow's navigator asks for.
/// </remarks>
public static partial class ServiceRegistrationExtensions
{
    /// <summary>
    /// Registers the dunnage page in the application project and its route.
    /// </summary>
    static partial void RegisterDunnageCataloguePage(IServiceCollection services)
    {
        services.AddTransient<MTM_Waitlist.Module_GenericRequest.Views.GenericRequestDunnagePage>();

        s_pageRouteRegistrations.Add(pageService =>
            pageService.Configure<GenericRequestDunnageViewModel, MTM_Waitlist.Module_GenericRequest.Views.GenericRequestDunnagePage>());
    }
}
