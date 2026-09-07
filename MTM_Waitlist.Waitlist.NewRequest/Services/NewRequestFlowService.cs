using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

public sealed class NewRequestFlowService : INewRequestFlowService
{
    private readonly IRequestTypeCatalogService _requestTypeCatalogService;
    private readonly IImageLocationService _imageLocationService;

    public NewRequestFlowService(IImageLocationService imageLocationService, IRequestTypeCatalogService requestTypeCatalogService)
    {
        _imageLocationService = imageLocationService;
        _requestTypeCatalogService = requestTypeCatalogService;
    }

    public async Task<IReadOnlyList<NewRequestTypeDefinition>> LoadRequestTypesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var catalog = await _requestTypeCatalogService.LoadRequestTypesAsync(cancellationToken).ConfigureAwait(false);
            if (catalog.Count > 0)
            {
                return catalog;
            }
        }
        catch (Exception ex)
        {
            StartupDebugLog.Info("WaitlistNewRequest", $"Failed to load request type catalog from DB. Falling back to defaults. Error={ex.Message}");
        }

        return NewRequestFlowRules.GetDefaultTypes();
    }

    public async Task<string> ResolveRequestTypeImagePathAsync(string requestTypeName, CancellationToken cancellationToken = default)
    {
        if (_imageLocationService is null || !_imageLocationService.IsInitialized)
        {
            return string.Empty;
        }

        try
        {
            var requestType = RequestTypeInventory.GetByDisplayName(requestTypeName);
            if (requestType is null)
            {
                return string.Empty;
            }

            return await _imageLocationService
                .ResolveRequestTypeImagePathAsync(requestType.StableId.ToString(), cancellationToken)
                .ConfigureAwait(true);
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    public async Task<string> ResolveRequestSubtypeImagePathAsync(
        string requestTypeName,
        string subtypeName,
        CancellationToken cancellationToken = default)
    {
        if (_imageLocationService is null || !_imageLocationService.IsInitialized)
        {
            return string.Empty;
        }

        try
        {
            var (_, subtype) = RequestSubtypeInventory.GetByDisplayNames(requestTypeName, subtypeName);
            if (subtype is null)
            {
                return string.Empty;
            }

            return await _imageLocationService
                .ResolveRequestSubtypeImagePathAsync(subtype.StableId.ToString(), cancellationToken)
                .ConfigureAwait(true);
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    public async Task<Dictionary<string, string>> BuildWorkCenterImageLookupAsync(CancellationToken cancellationToken = default)
    {
        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (_imageLocationService is null || !_imageLocationService.IsInitialized)
        {
            return lookup;
        }

        var activeWorkCenters = await _imageLocationService.GetActiveWorkCentersAsync(cancellationToken).ConfigureAwait(true);
        if (activeWorkCenters is null)
        {
            return lookup;
        }

        foreach (var workCenter in activeWorkCenters)
        {
            var resolvedPath = await _imageLocationService
                .ResolveWorkCenterImagePathAsync(workCenter.WorkCenterId.ToString(), cancellationToken)
                .ConfigureAwait(true);
            lookup[workCenter.DisplayName] = resolvedPath;
        }

        return lookup;
    }
}
