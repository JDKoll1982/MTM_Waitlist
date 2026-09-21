using Microsoft.Extensions.DependencyInjection;

using MTM_Waitlist.Module_Settings.ViewModels;
using MTM_Waitlist.Module_Settings.Views;

namespace MTM_Waitlist.Services.DependencyInjection;

/// <summary>
/// The user list's own registrations (T045). One file per story phase, so three phases never contend on the
/// composition root.
/// </summary>
public static partial class ServiceRegistrationExtensions
{
    /// <summary>
    /// Registers the user list's page and its view model, and contributes its route.
    /// </summary>
    static partial void RegisterUserManagementPages(IServiceCollection services)
    {
        services.AddTransient<UserManagementViewModel>();
        services.AddTransient<UserManagementPage>();

        // The route is contributed rather than written into the composition root, so a phase that owns a page owns
        // everything about it. The page service applies the contributions as it builds its route table.
        s_pageRouteRegistrations.Add(pageService =>
            pageService.Configure<UserManagementViewModel, UserManagementPage>());
    }
}
