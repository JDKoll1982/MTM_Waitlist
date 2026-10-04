using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Module_Settings.Services.DependencyInjection;

public static class ModuleDependencyInjectionExtensions
{
    public static IServiceCollection AddSettingsModuleServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<MTM_Waitlist.Module_Core.Contracts.Services.IAppModuleClock, MTM_Waitlist.Module_Core.Services.AppModuleClock>();
        services.AddSingleton<SettingsModuleService>();
        services.AddSingleton<IConfigSettingsValueService, ConfigSettingsValueService>();

        // The "Computers" screen's registry seam moved into this module with the launch rebuild (T187), so it
        // is registered where its screen lives rather than in the app's own registration file.
        services.AddSingleton<IComputerRegistryService, ComputerRegistryService>();
        services.AddImageLocationServices(configuration);

        // A singleton on purpose: the controls read it while a click is being handled, and the settings screen
        // writes through the same instance, so a change applies to the screen already open.
        services.AddSingleton<
            MTM_Waitlist.Module_Core.Contracts.Services.IPictureEnlargePreference,
            MTM_Waitlist.Module_Settings.Services.PictureEnlargePreference>();

        // Transient so each dialog opens with a clean set of pending edits.
        services.AddTransient<MTM_Waitlist.Module_Settings.ViewModels.WorkCenterImagesDialogViewModel>();
        services.AddTransient<MTM_Waitlist.Module_Settings.ViewModels.RequestItemImagesDialogViewModel>();
        services.AddTransient<MTM_Waitlist.Module_Settings.ViewModels.ComputerEditDialogViewModel>();
        services.AddTransient<MTM_Waitlist.Module_Settings.ViewModels.ComputerManagementViewModel>();

        // The developer log panel, registered through a factory rather than by constructor selection: its third
        // parameter is the clipboard writer, a delegate the container cannot supply, and the factory is what says
        // the panel takes the shipped clipboard behaviour unless a caller hands in another one.
        services.AddTransient<MTM_Waitlist.Module_Settings.ViewModels.DeveloperLogPanelViewModel>(provider =>
            new MTM_Waitlist.Module_Settings.ViewModels.DeveloperLogPanelViewModel(
                provider.GetRequiredService<MTM_Waitlist.Module_Core.Contracts.Services.IMySqlHelperServer>(),
                provider.GetRequiredService<MTM_Waitlist.Module_Core.Contracts.Services.IPermissionService>()));

        // The scoped-preference seam the Core readers take, declared in Core because those readers cannot see this
        // module, and supplied here because this is where the configuration read lives (FR-023, FR-025).
        services.AddSingleton<
            MTM_Waitlist.Module_Core.Contracts.Services.IScopedPreferenceStore,
            MTM_Waitlist.Module_Settings.Services.ScopedPreferenceStore>();

        // The Item-keyed allotment seam (§D4). UrgencySettingsService lives in MTM_Waitlist.Core and cannot see
        // this module's configuration read, so the composition supplies the store through the Core-side contract.
        services.AddSingleton<
            MTM_Waitlist.Module_Core.Contracts.Services.IRequestItemAllottedMinutesStore,
            MTM_Waitlist.Module_Settings.Services.RequestItemAllottedMinutesStore>();

        return services;
    }
}
