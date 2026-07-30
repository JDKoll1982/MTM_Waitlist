using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.ViewModels;

public partial class WaitlistViewViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;
    private readonly ISampleDataService _sampleDataService;
    private readonly IBuildingSelectionService _buildingSelectionService;
    private bool _isSubscribed;

    public ObservableCollection<SampleOrder> Source { get; } = new ObservableCollection<SampleOrder>();

    public WaitlistViewViewModel(
        INavigationService navigationService,
        ISampleDataService sampleDataService,
        IBuildingSelectionService buildingSelectionService)
    {
        ArgumentNullException.ThrowIfNull(navigationService);
        ArgumentNullException.ThrowIfNull(sampleDataService);
        ArgumentNullException.ThrowIfNull(buildingSelectionService);

        _navigationService = navigationService;
        _sampleDataService = sampleDataService;
        _buildingSelectionService = buildingSelectionService;
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
        foreach (var item in data)
        {
            if (item is SampleOrder sampleOrder)
            {
                Source.Add(sampleOrder);
            }
        }
    }
}
