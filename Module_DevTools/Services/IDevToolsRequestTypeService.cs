using MTM_Waitlist.Module_DevTools.Models;

namespace MTM_Waitlist.Module_DevTools.Services;

public interface IDevToolsRequestTypeService
{
    Task SaveRequestTypeAsync(RequestTypeDefinition requestTypeDefinition, string createdByUsername, CancellationToken cancellationToken = default);
}
