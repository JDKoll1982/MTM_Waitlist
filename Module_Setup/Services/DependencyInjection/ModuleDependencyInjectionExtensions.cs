using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;

namespace MTM_Waitlist.Module_Setup.Services.DependencyInjection;

public static class ModuleDependencyInjectionExtensions
{
    public static IServiceCollection AddSetupModuleServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<SetupWorkflowState>();
        services.AddSingleton<IWorkOrderValidationService, WorkOrderValidationService>();
        services.AddSingleton<IInforVisualLookupService, SetupLookupService>();
        services.AddSingleton<ISubordinatePartService, SetupLookupService>();
        services.AddSingleton<IDunnageWorkflowService, DunnageWorkflowService>();
        services.AddSingleton<IActiveJobCoordinatorService, SetupActiveJobCoordinatorService>();
        services.AddSingleton<ISetupPersistenceService, SetupPersistenceService>();
        services.AddSingleton<ISetupWorkflowService, SetupWorkflowService>();
        return services;
    }
}