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

    /// <summary>
    /// Makes sure the image location service is initialized before it is used.
    /// </summary>
    /// <remarks>
    /// The service is initialized during startup, but the first New Request screen can be reached before that
    /// finishes. Because the guards below used to bail out while it was not ready yet, the work-center step
    /// rendered placeholder photos on the first visit and the real photos only on the second. Initialization
    /// is idempotent and lock-guarded, so awaiting it here makes the first render deterministic instead of
    /// racing startup. A failure to initialize still degrades to the placeholder rather than throwing.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token for initialization.</param>
    /// <returns><see langword="true"/> when the service is initialized and usable.</returns>
    private async Task<bool> EnsureImageServiceAsync(CancellationToken cancellationToken)
    {
        if (_imageLocationService is null)
        {
            return false;
        }

        if (!_imageLocationService.IsInitialized)
        {
            try
            {
                await _imageLocationService.InitializeAsync(cancellationToken).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                StartupDebugLog.Error(
                    "WaitlistNewRequest",
                    ex,
                    "Image location service initialization failed while resolving New Request images.");
                return false;
            }
        }

        return _imageLocationService.IsInitialized;
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
        if (!await EnsureImageServiceAsync(cancellationToken).ConfigureAwait(true))
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
        if (!await EnsureImageServiceAsync(cancellationToken).ConfigureAwait(true))
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
        if (!await EnsureImageServiceAsync(cancellationToken).ConfigureAwait(true))
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
