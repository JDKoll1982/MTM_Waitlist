using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MTM_Waitlist.Module_Shared.Services;

namespace MTM_Waitlist.Module_Shared.Services.DependencyInjection;

public static class ModuleDependencyInjectionExtensions
{
    public static IServiceCollection AddSharedModuleServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<MTM_Waitlist.Module_Core.Contracts.Services.IAppModuleClock, MTM_Waitlist.Module_Core.Services.AppModuleClock>();
        services.Configure<ModuleSharedOptions>(configuration.GetSection(nameof(ModuleSharedOptions)));
        services.AddSingleton<ISharedModuleService, SharedModuleServiceImplementation>();
        services.AddSingleton<SharedModuleService>();
        services.AddSingleton<ISharedConfigurationService, SharedConfigurationService>();
        services.AddSingleton<ITooltipService, TooltipService>();
        services.AddSingleton<IControlInspectorService, ControlInspectorService>();
        services.AddSingleton<IWorkCenterCatalogService, WorkCenterCatalogService>();
        services.AddSingleton<IDunnageTypeVisibilityCatalogService, DunnageTypeVisibilityCatalogService>();
        return services;
    }
}
