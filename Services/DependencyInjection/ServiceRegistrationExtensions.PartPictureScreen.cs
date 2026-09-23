using Microsoft.Extensions.DependencyInjection;

using MTM_Waitlist.Module_Settings.ViewModels;
using MTM_Waitlist.Module_Settings.Views;

namespace MTM_Waitlist.Services.DependencyInjection;

/// <summary>
/// The part-picture screen's registration (T060): the write path, the coverage read, the view model and the route.
/// </summary>
/// <remarks>
/// One file per story phase, so no phase edits the composition root's factory. The page is configured here rather
/// than in the main list for the same reason each other feature's pages are: the registration and the markup it
/// needs land together.
/// </remarks>
public static partial class ServiceRegistrationExtensions
{
    /// <summary>
    /// Registers the part-picture screen and the services behind it.
    /// </summary>
    static partial void RegisterPartPictureScreen(IServiceCollection services)
    {
        services.AddSingleton<MTM_Waitlist.Module_Settings.Services.IPartPictureWriteService,
            MTM_Waitlist.Module_Settings.Services.PartPictureWriteService>();
        services.AddSingleton<MTM_Waitlist.Module_Settings.Services.IPartNumberSource,
            MTM_Waitlist.Module_Settings.Services.WipFloorPartNumberSource>();
        services.AddSingleton<MTM_Waitlist.Module_Settings.Services.PartPictureCoverageService>();
        services.AddTransient<PartPictureManagerViewModel>();
        services.AddTransient<PartPictureManagerPage>();

        // The route is contributed rather than written into the composition root, so the phase that owns the page
        // owns everything about it. The page service applies the contributions as it builds its route table.
        s_pageRouteRegistrations.Add(pageService =>
            pageService.Configure<PartPictureManagerViewModel, PartPictureManagerPage>());
    }
}
