using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;

namespace MTM_Waitlist.Module_Setup.Services.DependencyInjection;

public static class ModuleDependencyInjectionExtensions
{
    public static IServiceCollection AddSetupModuleServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<SetupWorkflowState>();

        // One owner of the Infor Visual read path: the setup tool resolves its own queue scripts through
        // Core's executor, which is the same type the rest of the application uses.
        services.AddSingleton(sp => new InforVisualSqlQueryService(
            sp.GetRequiredService<IConfiguration>(),
            InforVisualSqlModule.Setup));
        services.AddSingleton<IWorkOrderValidationService, WorkOrderValidationService>();
        services.AddSingleton<ISetupWorkCenterService, SetupWorkCenterService>();
        services.AddSingleton<IInforVisualLookupService, SetupLookupService>();
        services.AddSingleton<ISubordinatePartService, SetupLookupService>();
        services.AddSingleton<IWorkOrderSearchService, SetupLookupService>();
        services.AddSingleton<IDunnageWorkflowService, DunnageWorkflowService>();
        services.AddSingleton<IActiveJobCoordinatorService, SetupActiveJobCoordinatorService>();
        services.AddSingleton<ISetupPersistenceService, SetupPersistenceService>();
        services.AddSingleton<ISetupWorkflowService, SetupWorkflowService>();
        services.AddSingleton<IActiveJobItemResolverService, ActiveJobItemResolverService>();
        return services;
    }
}