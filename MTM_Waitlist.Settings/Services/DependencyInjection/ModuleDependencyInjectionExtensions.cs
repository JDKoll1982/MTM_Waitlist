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
        services.AddImageLocationServices(configuration);

        // Transient so each dialog opens with a clean set of pending edits.
        services.AddTransient<MTM_Waitlist.Module_Settings.ViewModels.WorkCenterImagesDialogViewModel>();
        services.AddTransient<MTM_Waitlist.Module_Settings.ViewModels.RequestItemImagesDialogViewModel>();
        services.AddTransient<MTM_Waitlist.Module_Settings.ViewModels.ComputerEditDialogViewModel>();
        services.AddTransient<MTM_Waitlist.Module_Settings.ViewModels.ComputerManagementViewModel>();

        // The Item-keyed allotment seam (§D4). UrgencySettingsService lives in MTM_Waitlist.Core and cannot see
        // this module's configuration read, so the composition supplies the store through the Core-side contract.
        services.AddSingleton<
            MTM_Waitlist.Module_Core.Contracts.Services.IRequestItemAllottedMinutesStore,
            MTM_Waitlist.Module_Settings.Services.RequestItemAllottedMinutesStore>();

        return services;
    }
}
