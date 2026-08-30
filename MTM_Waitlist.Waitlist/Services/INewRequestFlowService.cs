using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

/// <summary>
/// Data access for the New Request wizard: loads the request-type catalog and
/// resolves request-type/subtype/work-center image paths through the shared
/// image-location service. Replaces the dialog-era helpers that lived on
/// <c>WaitlistNewRequestDialogService</c> and <c>WorkCenterSelectionDialogService</c>.
/// </summary>
public interface INewRequestFlowService
{
    Task<IReadOnlyList<NewRequestTypeDefinition>> LoadRequestTypesAsync(CancellationToken cancellationToken = default);

    Task<string> ResolveRequestTypeImagePathAsync(string requestTypeName, CancellationToken cancellationToken = default);

    Task<string> ResolveRequestSubtypeImagePathAsync(string requestTypeName, string subtypeName, CancellationToken cancellationToken = default);

    Task<Dictionary<string, string>> BuildWorkCenterImageLookupAsync(CancellationToken cancellationToken = default);
}
