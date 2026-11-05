using Microsoft.Extensions.DependencyInjection;

using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Shared.ViewModels;

namespace MTM_Waitlist.Services.DependencyInjection;

/// <summary>
/// The dunnage catalogue's registration: the one read and the one shared view model.
/// </summary>
/// <remarks>
/// One file, so no later phase edits the composition root's service list. The interface is bound to the Setup
/// implementation because that is where the read of the receiving store already lives; no consumer names that
/// implementation, which is what lets the generic request flow read the catalogue without referencing it.
/// </remarks>
public static partial class ServiceRegistrationExtensions
{
    /// <summary>
    /// Registers the catalogue read and the view model both surfaces draw it through.
    /// </summary>
    static partial void RegisterDunnageCatalogue(IServiceCollection services)
    {
        services.AddSingleton<IDunnageCatalogueReader, MTM_Waitlist.Module_Setup.Services.DunnageCatalogueReader>();
        services.AddTransient<DunnageCatalogueViewModel>();
    }
}
