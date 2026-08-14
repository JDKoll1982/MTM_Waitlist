using System.Collections.ObjectModel;
using System.Collections.Specialized;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.ViewModels;

public partial class WaitlistViewViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;
    private readonly ISampleDataService _sampleDataService;
    private readonly MTM_Waitlist.Module_Waitlist.Services.IWaitlistRequestService _waitlistRequestService;
    private readonly IBuildingSelectionService _buildingSelectionService;
    private bool _isSubscribed;

    public ObservableCollection<SampleOrder> Source { get; } = new ObservableCollection<SampleOrder>();
    public ObservableCollection<SampleOrder> SearchSuggestions { get; } = new();
    public bool IsWaitlistEmpty => Source.Count == 0;
    public string SelectedBuilding => _buildingSelectionService.SelectedBuilding;

    public WaitlistViewViewModel(
        INavigationService navigationService,
        ISampleDataService sampleDataService,
        IBuildingSelectionService buildingSelectionService,
        MTM_Waitlist.Module_Waitlist.Services.IWaitlistRequestService waitlistRequestService)
    {
        ArgumentNullException.ThrowIfNull(navigationService);
        ArgumentNullException.ThrowIfNull(sampleDataService);
        ArgumentNullException.ThrowIfNull(buildingSelectionService);
        ArgumentNullException.ThrowIfNull(waitlistRequestService);

        _navigationService = navigationService;
        _sampleDataService = sampleDataService;
        _buildingSelectionService = buildingSelectionService;
        _waitlistRequestService = waitlistRequestService;

        Source.CollectionChanged += OnSourceCollectionChanged;
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (!_isSubscribed)
        {
            _buildingSelectionService.BuildingChanged += OnBuildingChanged;
            _isSubscribed = true;
        }

        await LoadOrdersAsync(_buildingSelectionService.SelectedBuilding);
    }

    public void OnNavigatedFrom()
    {
        if (_isSubscribed)
        {
            _buildingSelectionService.BuildingChanged -= OnBuildingChanged;
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

    private async Task LoadOrdersAsync(string building)
    {
        Source.Clear();

        var data = _sampleDataService.GetSampleOrders(building);
        var sampleCount = 0;
        foreach (var item in data)
        {
            if (item is SampleOrder sampleOrder)
            {
                Source.Add(sampleOrder);
                sampleCount++;
            }
        }

        var activeRequests = _waitlistRequestService.GetActiveRequests(building);
        foreach (var request in activeRequests)
        {
            var item = new SampleOrder
            {
                Id = request.Id.GetHashCode(),
                Title = string.IsNullOrWhiteSpace(request.Subtype) ? request.RequestType : $"{request.RequestType} / {request.Subtype}",
                Status = request.Status,
                RequestedByName = "Current user",
                RequestedPressName = request.WorkCenter,
                RemainingTimeText = "New",
                ImagePath = ResolveImagePath(request.RequestType, request.Subtype),
            };
            AddRequestFields(item, request);
            Source.Add(item);
        }
        StartupDebugLog.Info("Waitlist", $"Loaded building '{building}'. SampleRows={sampleCount}, SessionRequests={activeRequests.Count}, TotalRows={Source.Count}, SearchQuery='{SearchQuery}'.");
        UpdateSearchSuggestions(SearchQuery);
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

    private static void AddRequestFields(SampleOrder item, WaitlistRequest request)
    {
        var requestType = request.RequestType.Trim().ToLowerInvariant();
        var subtype = request.Subtype?.Trim() ?? string.Empty;
        var details = string.IsNullOrWhiteSpace(request.InputValue) ? "Not provided" : request.InputValue.Trim();

        if (requestType == "scrap")
        {
            item.Fields.Add(new WaitlistField { Label = "Part number", Value = "Not provided" });
            item.Fields.Add(new WaitlistField { Label = "Pickup work center", Value = request.WorkCenter });
            item.Fields.Add(new WaitlistField { Label = "Quantity involved", Value = "1" });
            item.Fields.Add(new WaitlistField { Label = "Scrap lugger", Value = subtype == "Empty" ? "Not selected" : subtype });
            item.Fields.Add(new WaitlistField { Label = "Scrap reason", Value = details });
            return;
        }

        if (requestType == "coil")
        {
            item.Fields.Add(new WaitlistField { Label = "Requested coil", Value = subtype == "Wrong Coil" ? "Wrong coil" : subtype });
            item.Fields.Add(new WaitlistField { Label = "Quantity in house", Value = "Not provided" });
            item.Fields.Add(new WaitlistField { Label = "Coil description", Value = details });
            item.Fields.Add(new WaitlistField { Label = "Average coil weight", Value = "Not provided" });
            item.Fields.Add(new WaitlistField { Label = "Requesting work center", Value = request.WorkCenter });
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
        OnPropertyChanged(nameof(IsWaitlistEmpty));
    }
}
