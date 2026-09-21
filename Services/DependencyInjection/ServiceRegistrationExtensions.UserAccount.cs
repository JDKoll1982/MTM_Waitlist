using Microsoft.Extensions.DependencyInjection;

using MTM_Waitlist.Module_Settings.ViewModels;
using MTM_Waitlist.Module_Settings.Views;

namespace MTM_Waitlist.Services.DependencyInjection;

/// <summary>
/// The person's page, the create form and the one-time credential window's own registrations (T052). One file per
/// story phase, so three phases never contend on the composition root.
/// </summary>
public static partial class ServiceRegistrationExtensions
{
    /// <summary>
    /// Registers the create form, the person's page and the credential window, and contributes their two routes.
    /// The credential window is a dialog rather than a destination, so it has no route: the pages that issue a
    /// credential construct it and show it themselves.
    /// </summary>
    static partial void RegisterUserAccountPages(IServiceCollection services)
    {
        services.AddTransient<CreateUserViewModel>();
        services.AddTransient<CreateUserPage>();

        services.AddTransient<EditUserViewModel>();
        services.AddTransient<EditUserPage>();

        services.AddTransient<PinRevealDialogViewModel>();
        services.AddTransient<PinRevealDialog>();

        // The routes are contributed rather than written into the composition root, so a phase that owns a page
        // owns everything about it. The page service applies the contributions as it builds its route table.
        s_pageRouteRegistrations.Add(pageService =>
            pageService.Configure<CreateUserViewModel, CreateUserPage>());
        s_pageRouteRegistrations.Add(pageService =>
            pageService.Configure<EditUserViewModel, EditUserPage>());
    }
}
