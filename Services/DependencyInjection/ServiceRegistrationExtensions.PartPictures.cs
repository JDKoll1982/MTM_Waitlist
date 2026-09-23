using Microsoft.Extensions.DependencyInjection;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Services.DependencyInjection;

/// <summary>
/// The part-picture reader's registration (T019). One file per story phase, so no phase edits the composition root.
/// </summary>
/// <remarks>
/// The reader is registered behind its contract, which is the same arrangement <c>IWorkCenterImageService</c> and
/// <c>IImageLocationService</c> already use: the surfaces depend on an interface declared in
/// <c>MTM_Waitlist.Core</c> and the implementation stays beside the picture store in
/// <c>MTM_Waitlist.Settings</c>. Its own dependencies — the override read service and the storage configuration
/// resolver — are registered by the settings module's own collection extension and are not repeated here.
/// </remarks>
public static partial class ServiceRegistrationExtensions
{
    /// <summary>
    /// Registers the reader every surface that draws a part calls.
    /// </summary>
    static partial void RegisterPartPictureServices(IServiceCollection services)
    {
        services.AddSingleton<IPartPictureResolver, PartPictureResolver>();
    }
}
