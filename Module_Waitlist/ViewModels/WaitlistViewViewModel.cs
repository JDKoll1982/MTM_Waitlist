using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.ViewModels;

public partial class WaitlistViewViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;
    private readonly ISampleDataService _sampleDataService;
    private readonly MTM_Waitlist.Module_Waitlist.Services.IWaitlistRequestService _waitlistRequestService;
    private readonly IImageLocationService? _imageLocationService;
    private readonly IBuildingSelectionService _buildingSelectionService;
    private readonly DispatcherQueue? _dispatcherQueue;
    private IDisposable? _imageLocationSubscription;
    private long _refreshVersion;
    private bool _isSubscribed;

    public ObservableCollection<SampleOrder> Source { get; } = new ObservableCollection<SampleOrder>();
    public ObservableCollection<SampleOrder> SearchSuggestions { get; } = new();

    [ObservableProperty]
    public partial bool IsWaitlistEmpty { get; private set; } = true;

    public string SelectedBuilding => _buildingSelectionService.SelectedBuilding;

    public WaitlistViewViewModel(
        INavigationService navigationService,
        ISampleDataService sampleDataService,
        IBuildingSelectionService buildingSelectionService,
        MTM_Waitlist.Module_Waitlist.Services.IWaitlistRequestService waitlistRequestService,
        IImageLocationService? imageLocationService = null,
        DispatcherQueue? dispatcherQueue = null)
    {
        ArgumentNullException.ThrowIfNull(navigationService);
        ArgumentNullException.ThrowIfNull(sampleDataService);
        ArgumentNullException.ThrowIfNull(buildingSelectionService);
        ArgumentNullException.ThrowIfNull(waitlistRequestService);

        _navigationService = navigationService;
        _sampleDataService = sampleDataService;
        _buildingSelectionService = buildingSelectionService;
        _waitlistRequestService = waitlistRequestService;
        _imageLocationService = imageLocationService;
        _dispatcherQueue = dispatcherQueue;

        Source.CollectionChanged += OnSourceCollectionChanged;
        IsWaitlistEmpty = Source.Count == 0;
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (!_isSubscribed)
        {
            _buildingSelectionService.BuildingChanged += OnBuildingChanged;
            _waitlistRequestService.RequestsChanged += OnRequestsChanged;
            if (_imageLocationService is not null && _imageLocationService.IsInitialized)
            {
                _imageLocationSubscription = _imageLocationService.SubscribeToImageLocationChanges(OnImageLocationChanged);
            }

            _isSubscribed = true;
        }

        await LoadOrdersAsync(_buildingSelectionService.SelectedBuilding);
    }

    public void OnNavigatedFrom()
    {
        if (_isSubscribed)
        {
            _buildingSelectionService.BuildingChanged -= OnBuildingChanged;
            _waitlistRequestService.RequestsChanged -= OnRequestsChanged;
            _imageLocationSubscription?.Dispose();
            _imageLocationSubscription = null;
            _isSubscribed = false;
        }
    }

    [RelayCommand]
    private void OnItemClick(SampleOrder? clickedItem)
    {
        if (clickedItem != null)
        {
            _navigationService.SetListDataItemForNextConnectedAnimation(clickedItem);
            _navigationService.NavigateTo(typeof(WaitlistViewDetailViewModel).FullName!, clickedItem.Id);
        }
    }

    private async void OnBuildingChanged(object? sender, EventArgs e)
    {
        await LoadOrdersAsync(_buildingSelectionService.SelectedBuilding);
    }

    private async void OnRequestsChanged(object? sender, EventArgs e)
    {
        await LoadOrdersAsync(_buildingSelectionService.SelectedBuilding);
    }

    private void OnImageLocationChanged(ImageLocationChangedEventArgs args)
    {
        _ = RefreshAsync();
    }

    private async Task LoadOrdersAsync(string building)
    {
        var refreshVersion = Interlocked.Increment(ref _refreshVersion);
        await Task.Yield();

        if (refreshVersion != Volatile.Read(ref _refreshVersion))
        {
            return;
        }

        // Fetch data on the current context (background thread when called via async void).
        var data = _sampleDataService.GetSampleOrders(building);
        var sampleCount = 0;
        var newItems = new List<SampleOrder>(data.Count());
        foreach (var item in data)
        {
            if (item is SampleOrder sampleOrder)
            {
                newItems.Add(sampleOrder);
                sampleCount++;
            }
        }

        if (refreshVersion != Volatile.Read(ref _refreshVersion))
        {
            return;
        }

        var activeRequests = _waitlistRequestService.GetActiveRequests(building);
        var activeRequestCount = activeRequests.Count;
        var workCenterImageLookup = await BuildWorkCenterImageLookupAsync(cancellationToken: default).ConfigureAwait(false);
        foreach (var request in activeRequests)
        {
            var sessionOrder = CreateSessionOrder(request);
            await ApplyResolvedImagesAsync(sessionOrder, request, workCenterImageLookup).ConfigureAwait(false);
            newItems.Add(sessionOrder);
        }

        if (refreshVersion != Volatile.Read(ref _refreshVersion))
        {
            return;
        }

        await ApplySourceUpdateAsync(newItems, refreshVersion);
        StartupDebugLog.Info("Waitlist", $"Loaded building '{building}'. SampleRows={sampleCount}, SessionRequests={activeRequestCount}, TotalRows={Source.Count}, SearchQuery='{SearchQuery}'.");
        UpdateSearchSuggestions(SearchQuery);
    }

    private async Task ApplySourceUpdateAsync(List<SampleOrder> newItems, long refreshVersion)
    {
        void Apply()
        {
            if (refreshVersion != Volatile.Read(ref _refreshVersion))
            {
                return;
            }

            Source.CollectionChanged -= OnSourceCollectionChanged;
            try
            {
                Source.Clear();
                foreach (var item in newItems)
                {
                    Source.Add(item);
                }
            }
            finally
            {
                Source.CollectionChanged += OnSourceCollectionChanged;
            }

            IsWaitlistEmpty = Source.Count == 0;
        }

        if (_dispatcherQueue is DispatcherQueue dispatcher)
        {
            var tcs = new TaskCompletionSource();
            dispatcher.TryEnqueue(() =>
            {
                try
                {
                    Apply();
                }
                finally
                {
                    tcs.SetResult();
                }
            });
            await tcs.Task;
        }
        else
        {
            Apply();
        }
    }

    public static SampleOrder CreateSessionOrder(WaitlistRequest request)
    {
        var subtypeId = ResolveSubtypeStableId(request);
        var requestTypeId = ResolveRequestTypeStableId(request.RequestType);

        var item = new SampleOrder
        {
            Id = request.Id.GetHashCode(),
            Title = string.IsNullOrWhiteSpace(request.Subtype) ? request.RequestType : $"{request.RequestType} / {request.Subtype}",
            Status = request.Status,
            RequestedByName = "Current user",
            RequestedPressName = request.WorkCenter,
            RemainingTimeText = GetRemainingTimeText(request.TargetTimeUtc, request.IsOverdue),
            ImagePath = ResolveImagePath(request.RequestType, request.Subtype),
            IsOverdue = request.IsOverdue,
            RequestTypeStableId = requestTypeId,
            SubtypeStableId = subtypeId,
        };

        AddRequestFields(item, request);
        return item;
    }

    private static string GetRemainingTimeText(DateTimeOffset? targetTimeUtc, bool isOverdue)
    {
        if (isOverdue)
        {
            return "Overdue";
        }

        if (targetTimeUtc is null)
        {
            return "New";
        }

        var remaining = targetTimeUtc.Value - DateTimeOffset.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            return "Overdue";
        }

        var totalMinutes = (int)Math.Ceiling(remaining.TotalMinutes);
        return totalMinutes <= 0 ? "Overdue" : $"{totalMinutes / 60:00}:{totalMinutes % 60:00}";
    }

    private static string ResolveImagePath(string requestType, string? subtype)
    {
        var normalizedSubtype = subtype?.Trim().ToLowerInvariant() ?? string.Empty;
        if (normalizedSubtype.Contains("ncm", StringComparison.Ordinal))
        {
            return "pickup_ncm.png";
        }

        if (normalizedSubtype.Contains("wip", StringComparison.Ordinal))
        {
            return "pickup_wip.png";
        }

        if (normalizedSubtype.Contains("fg", StringComparison.Ordinal) || normalizedSubtype.Contains("finished", StringComparison.Ordinal))
        {
            return "pickup_fg.png";
        }

        if (normalizedSubtype.Contains("outside", StringComparison.Ordinal) || normalizedSubtype.Contains("service", StringComparison.Ordinal))
        {
            return "pickup_os.png";
        }

        return requestType.Trim().ToLowerInvariant() switch
        {
            "coil" => "coil.png",
            "scrap" => "scrap.png",
            "pickup" => "pickup_wip.png",
            _ => "pickup_wip.png",
        };
    }

    private async Task<Dictionary<string, (long WorkCenterId, string ResolvedPath)>> BuildWorkCenterImageLookupAsync(CancellationToken cancellationToken)
    {
        var lookup = new Dictionary<string, (long WorkCenterId, string ResolvedPath)>(StringComparer.OrdinalIgnoreCase);
        if (_imageLocationService is null || !_imageLocationService.IsInitialized)
        {
            return lookup;
        }

        try
        {
            var workCenters = await _imageLocationService.GetActiveWorkCentersAsync(cancellationToken).ConfigureAwait(false);
            if (workCenters is null)
            {
                return lookup;
            }

            foreach (var workCenter in workCenters)
            {
                var resolvedPath = await _imageLocationService.ResolveWorkCenterImagePathAsync(
                    workCenter.WorkCenterId.ToString(),
                    cancellationToken).ConfigureAwait(false);
                lookup[workCenter.DisplayName] = (workCenter.WorkCenterId, resolvedPath);
            }
        }
        catch
        {
            // Keep fallback images when resolver metadata is temporarily unavailable.
        }

        return lookup;
    }

    private async Task ApplyResolvedImagesAsync(
        SampleOrder order,
        WaitlistRequest request,
        IReadOnlyDictionary<string, (long WorkCenterId, string ResolvedPath)> workCenterImageLookup,
        CancellationToken cancellationToken = default)
    {
        if (_imageLocationService is not null && _imageLocationService.IsInitialized)
        {
            try
            {
                var resolvedImagePath = await ResolveRequestImagePathAsync(request, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(resolvedImagePath))
                {
                    order.ResolvedImagePath = resolvedImagePath;
                }
            }
            catch
            {
                // Keep legacy fallback image when resolver path cannot be resolved.
            }
        }

        if (workCenterImageLookup.TryGetValue(request.WorkCenter, out var workCenterImage))
        {
            order.WorkCenterCatalogId = workCenterImage.WorkCenterId;
            order.WorkCenterImagePath = workCenterImage.ResolvedPath;
        }
    }

    private async Task<string?> ResolveRequestImagePathAsync(WaitlistRequest request, CancellationToken cancellationToken)
    {
        if (_imageLocationService is null || !_imageLocationService.IsInitialized)
        {
            return null;
        }

        var subtypeName = request.Subtype?.Trim();
        if (!string.IsNullOrWhiteSpace(subtypeName))
        {
            var subtypeMatch = RequestSubtypeInventory.GetByDisplayNames(request.RequestType, subtypeName);
            if (subtypeMatch.item is not null)
            {
                return await _imageLocationService.ResolveRequestSubtypeImagePathAsync(
                    subtypeMatch.item.StableId.ToString(),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        var requestTypeMatch = RequestTypeInventory.GetByDisplayName(request.RequestType);
        if (requestTypeMatch is null)
        {
            return null;
        }

        return await _imageLocationService.ResolveRequestTypeImagePathAsync(
            requestTypeMatch.StableId.ToString(),
            cancellationToken).ConfigureAwait(false);
    }

    private static Guid? ResolveRequestTypeStableId(string requestTypeName)
    {
        var requestType = RequestTypeInventory.GetByDisplayName(requestTypeName);
        return requestType?.StableId;
    }

    private static Guid? ResolveSubtypeStableId(WaitlistRequest request)
    {
        var subtypeName = request.Subtype?.Trim();
        if (string.IsNullOrWhiteSpace(subtypeName))
        {
            return null;
        }

        var subtype = RequestSubtypeInventory.GetByDisplayNames(request.RequestType, subtypeName);
        return subtype.item?.StableId;
    }

    private static void AddRequestFields(SampleOrder item, WaitlistRequest request)
    {
        var requestType = request.RequestType.Trim().ToLowerInvariant();
        var subtype = request.Subtype?.Trim() ?? string.Empty;
        var details = string.IsNullOrWhiteSpace(request.InputValue) ? "Not provided" : request.InputValue.Trim();
        var normalizedSubtype = subtype.ToLowerInvariant();

        if (requestType == "scrap")
        {
            item.Fields.Add(new WaitlistField { Label = "Part number", Value = "Not provided" });
            item.Fields.Add(new WaitlistField { Label = "Pickup work center", Value = request.WorkCenter });
            item.Fields.Add(new WaitlistField { Label = "Quantity involved", Value = "1" });
            item.Fields.Add(new WaitlistField { Label = "Scrap lugger", Value = normalizedSubtype.Contains("empty") ? "Not selected" : (string.IsNullOrWhiteSpace(subtype) ? "Not selected" : subtype) });
            item.Fields.Add(new WaitlistField { Label = "Scrap reason", Value = details });
            return;
        }

        if (requestType == "coil")
        {
            item.Fields.Add(new WaitlistField { Label = "Requested coil", Value = normalizedSubtype.Contains("wrong") ? "Wrong coil" : (string.IsNullOrWhiteSpace(subtype) ? "Not provided" : subtype) });
            item.Fields.Add(new WaitlistField { Label = "Quantity in house", Value = "Not provided" });
            item.Fields.Add(new WaitlistField { Label = "Coil description", Value = details });
            item.Fields.Add(new WaitlistField { Label = "Average coil weight", Value = "Not provided" });
            item.Fields.Add(new WaitlistField { Label = "Requesting work center", Value = request.WorkCenter });
            return;
        }

        if (requestType == "pickup")
        {
            if (normalizedSubtype.Contains("fg") || normalizedSubtype.Contains("finished"))
            {
                item.Fields.Add(new WaitlistField { Label = "Subtype", Value = subtype });
                item.Fields.Add(new WaitlistField { Label = "Part number", Value = "FG-10042" });
                item.Fields.Add(new WaitlistField { Label = "Part description", Value = "Finished bracket assembly" });
                item.Fields.Add(new WaitlistField { Label = "Quantity remaining", Value = "24 each" });
                item.Fields.Add(new WaitlistField { Label = "Customer", Value = "Northstar Manufacturing" });
                item.Fields.Add(new WaitlistField { Label = "Packlist", Value = "PL-80421" });
                item.Fields.Add(new WaitlistField { Label = "Work center", Value = request.WorkCenter });
                return;
            }

            if (normalizedSubtype.Contains("ncm"))
            {
                item.Fields.Add(new WaitlistField { Label = "Subtype", Value = subtype });
                item.Fields.Add(new WaitlistField { Label = "Part", Value = "RM-50218 / Customer RM-77" });
                item.Fields.Add(new WaitlistField { Label = "Quantity to move", Value = "2 containers" });
                item.Fields.Add(new WaitlistField { Label = "Pickup location", Value = request.WorkCenter });
                item.Fields.Add(new WaitlistField { Label = "Destination", Value = "NCM Area" });
                item.Fields.Add(new WaitlistField { Label = "Traceability ID", Value = "NCM-260803-014" });
                item.Fields.Add(new WaitlistField { Label = "Work center", Value = request.WorkCenter });
                return;
            }

            if (normalizedSubtype.Contains("wip"))
            {
                item.Fields.Add(new WaitlistField { Label = "Subtype", Value = subtype });
                item.Fields.Add(new WaitlistField { Label = "Work order", Value = "WO-072368" });
                item.Fields.Add(new WaitlistField { Label = "Part and quantity", Value = "WIP-218 / 12 pieces" });
                item.Fields.Add(new WaitlistField { Label = "Pickup work center", Value = request.WorkCenter });
                item.Fields.Add(new WaitlistField { Label = "WIP destination", Value = "WIP Area / Rack B-14" });
                item.Fields.Add(new WaitlistField { Label = "Operation sequence", Value = "30" });
                return;
            }

            if (normalizedSubtype.Contains("coil"))
            {
                item.Fields.Add(new WaitlistField { Label = "Subtype", Value = subtype });
                item.Fields.Add(new WaitlistField { Label = "Requested coil", Value = "COIL-204" });
                item.Fields.Add(new WaitlistField { Label = "Quantity in house", Value = "18 coils" });
                item.Fields.Add(new WaitlistField { Label = "Coil description", Value = "0.060 x 48 in galvanized coil" });
                item.Fields.Add(new WaitlistField { Label = "Average coil weight", Value = "1,240 lb" });
                item.Fields.Add(new WaitlistField { Label = "Requesting work center", Value = request.WorkCenter });
                return;
            }

            if (normalizedSubtype.Contains("outside") || normalizedSubtype.Contains("service"))
            {
                item.Fields.Add(new WaitlistField { Label = "Subtype", Value = subtype });
                item.Fields.Add(new WaitlistField { Label = "Part or work order", Value = "WO-073112 / RM-48190" });
                item.Fields.Add(new WaitlistField { Label = "Quantity to move", Value = "6 pieces" });
                item.Fields.Add(new WaitlistField { Label = "Pickup work center", Value = request.WorkCenter });
                item.Fields.Add(new WaitlistField { Label = "Outside-service destination", Value = "Heat Treat Section" });
                item.Fields.Add(new WaitlistField { Label = "Vendor or service", Value = "Midwest Heat Treat" });
                return;
            }

            if (normalizedSubtype.Contains("other"))
            {
                item.Fields.Add(new WaitlistField { Label = "Subtype", Value = subtype });
                item.Fields.Add(new WaitlistField { Label = "Request description", Value = details });
                item.Fields.Add(new WaitlistField { Label = "Requested work center", Value = request.WorkCenter });
                return;
            }
        }

        if (requestType == "other")
        {
            item.Fields.Add(new WaitlistField { Label = "Subtype", Value = string.IsNullOrWhiteSpace(subtype) ? "General Text Entry" : subtype });
            item.Fields.Add(new WaitlistField { Label = "Request description", Value = details });
            item.Fields.Add(new WaitlistField { Label = "Requested work center", Value = request.WorkCenter });
            return;
        }

        if (requestType == "flatstock")
        {
            item.Fields.Add(new WaitlistField { Label = "Part", Value = "RM-8201" });
            item.Fields.Add(new WaitlistField { Label = "Quantity", Value = "12 sheets" });
            item.Fields.Add(new WaitlistField { Label = "Work center", Value = request.WorkCenter });
            item.Fields.Add(new WaitlistField { Label = "Destination", Value = "Flatstock staging" });
            return;
        }

        if (requestType == "table handling" || requestType == "die handling")
        {
            item.Fields.Add(new WaitlistField { Label = requestType == "die handling" ? "Die" : "Part", Value = requestType == "die handling" ? "Die 4402" : "Part A-12" });
            item.Fields.Add(new WaitlistField { Label = "Quantity", Value = "1" });
            item.Fields.Add(new WaitlistField { Label = "Pickup location", Value = request.WorkCenter });
            item.Fields.Add(new WaitlistField { Label = "Destination", Value = requestType == "die handling" ? "Die shop" : "Table staging" });
            return;
        }

        if (requestType == "forklift assist")
        {
            item.Fields.Add(new WaitlistField { Label = "Description", Value = details });
            item.Fields.Add(new WaitlistField { Label = "Work center", Value = request.WorkCenter });
            item.Fields.Add(new WaitlistField { Label = "Requested by", Value = request.RequesterEmployeeName });
            return;
        }

        item.Fields.Add(new WaitlistField { Label = "Request details", Value = details });
        item.Fields.Add(new WaitlistField { Label = "Request type", Value = request.RequestType });
        item.Fields.Add(new WaitlistField { Label = "Subtype", Value = string.IsNullOrWhiteSpace(subtype) ? "Not provided" : subtype });
        item.Fields.Add(new WaitlistField { Label = "Work center", Value = request.WorkCenter });
        item.Fields.Add(new WaitlistField { Label = "Request ID", Value = request.Id.ToString("N") });
    }

    public Task RefreshAsync() => LoadOrdersAsync(_buildingSelectionService.SelectedBuilding);
    public string SearchQuery
    {
        get; private set;
    } = string.Empty;

    public void UpdateSearchSuggestions(string? query)
    {
        SearchQuery = query?.Trim() ?? string.Empty;
        SearchSuggestions.Clear();

        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            return;
        }

        foreach (var order in Source.Where(order => MatchesSearch(order, SearchQuery)).Take(8))
        {
            SearchSuggestions.Add(order);
        }
    }

    public void SubmitSearch(string? query, SampleOrder? selectedSuggestion = null)
    {
        var order = selectedSuggestion
            ?? Source.FirstOrDefault(candidate => string.Equals(candidate.Title, query?.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? Source.FirstOrDefault(candidate => MatchesSearch(candidate, query?.Trim() ?? string.Empty));

        if (order is not null)
        {
            OpenOrder(order);
        }
    }
    private void OpenOrder(SampleOrder order)
    {
        _navigationService.SetListDataItemForNextConnectedAnimation(order);
        _navigationService.NavigateTo(typeof(WaitlistViewDetailViewModel).FullName!, order.Id);
    }

    private static bool MatchesSearch(SampleOrder order, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return false;
        }

        return Contains(order.Title, query)
            || Contains(order.Status, query)
            || Contains(order.RequestedByName, query)
            || Contains(order.RequestedPressName, query)
            || order.Fields.Any(field => Contains(field.Label, query) || Contains(field.Value, query));
    }

    private static bool Contains(string value, string query) =>
        value.Contains(query, StringComparison.OrdinalIgnoreCase);

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var value = Source.Count == 0;
        if (value != IsWaitlistEmpty)
        {
            IsWaitlistEmpty = value;
        }
    }
}
