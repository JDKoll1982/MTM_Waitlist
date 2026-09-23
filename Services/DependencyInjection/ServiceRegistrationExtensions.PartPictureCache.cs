using Microsoft.Extensions.DependencyInjection;

using MTM_Waitlist.Module_Shared.Services;

namespace MTM_Waitlist.Services.DependencyInjection;

/// <summary>
/// The part-picture local copy store's registration (T082). One file per story phase, so no phase edits the
/// composition root.
/// </summary>
/// <remarks>
/// The store is registered behind its contract because the reader in <c>MTM_Waitlist.Settings</c> depends on the
/// interface while the store itself stays in <c>MTM_Waitlist.Shared</c>, beside the cache paths it writes into.
/// </remarks>
public static partial class ServiceRegistrationExtensions
{
    /// <summary>
    /// Registers the store that copies one part's picture onto this computer the first time that part is drawn.
    /// </summary>
    static partial void RegisterPartPictureCache(IServiceCollection services)
    {
        services.AddSingleton<IPartPictureCacheStore, PartPictureCacheStore>();
    }
}
