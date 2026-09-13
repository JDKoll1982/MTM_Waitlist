using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

public sealed class NewRequestFlowService : INewRequestFlowService
{
    private readonly IImageLocationService _imageLocationService;
    private readonly IRequestJobPartAvailabilityProvider _availabilityProvider;
    private readonly INewRequestPickerService _pickerService;

    public NewRequestFlowService(
        IImageLocationService imageLocationService,
        IRequestJobPartAvailabilityProvider availabilityProvider,
        INewRequestPickerService pickerService)
    {
        _imageLocationService = imageLocationService;
        _availabilityProvider = availabilityProvider;
        _pickerService = pickerService;
    }

    /// <inheritdoc />
    public async Task<RequestJobPartAvailability> ResolveAvailabilityAsync(string workCenter, CancellationToken cancellationToken = default)
    {
        var normalized = (workCenter ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return RequestJobPartAvailability.None;
        }

        try
        {
            var availability = await _availabilityProvider.GetAvailabilityAsync(normalized, cancellationToken).ConfigureAwait(false);
            StartupDebugLog.Info(
                "WaitlistNewRequest",
                $"Availability for work center '{normalized}': ActiveJob={availability.HasActiveJob}, Coil={availability.HasCoil}, Flatstock={availability.HasFlatstock}, Die={availability.HasDie}, Component={availability.HasComponent}, Dunnage={availability.HasDunnage}, ScrapDecision={availability.HasScrapDecision}.");
            return availability;
        }
        catch (Exception ex)
        {
            // A snapshot that cannot be read is reported as "no parts", which keeps the job-independent Items
            // offerable rather than opening an empty step (FR-002).
            StartupDebugLog.Error("WaitlistNewRequest", ex, $"Reading the availability snapshot for work center '{normalized}' failed. Offering the job-independent Items only.");
            return RequestJobPartAvailability.None;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<RequestCategory> GetVisibleCategories(RequestJobPartAvailability availability) =>
        _pickerService.GetVisibleCategories(availability ?? RequestJobPartAvailability.None);

    /// <inheritdoc />
    public IReadOnlyList<RequestItemDefinition> GetVisibleItems(RequestCategory category, RequestJobPartAvailability availability) =>
        _pickerService.GetVisibleItems(category, availability ?? RequestJobPartAvailability.None);

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
