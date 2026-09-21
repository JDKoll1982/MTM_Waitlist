using Microsoft.Extensions.DependencyInjection;

using MTM_Waitlist.Module_Settings.ViewModels;
using MTM_Waitlist.Module_Settings.Views;

namespace MTM_Waitlist.Services.DependencyInjection;

/// <summary>
/// The permissions page's own registrations (T057). One file per story phase, so three phases never contend on the
/// composition root.
/// </summary>
public static partial class ServiceRegistrationExtensions
{
    /// <summary>
    /// Registers the permissions page and its view model, and contributes its route.
    /// </summary>
    static partial void RegisterPermissionsPages(IServiceCollection services)
    {
        services.AddTransient<PermissionsViewModel>();
        services.AddTransient<PermissionsPage>();

        // The who-holds-this view is a control on the same page rather than a destination, so it has no route of
        // its own: the page hosts it and the control builds itself.
        services.AddTransient<PermissionHoldersViewModel>();

        // The route is contributed rather than written into the composition root, so a phase that owns a page owns
        // everything about it. The page service applies the contributions as it builds its route table.
        s_pageRouteRegistrations.Add(pageService =>
            pageService.Configure<PermissionsViewModel, PermissionsPage>());
    }
}
