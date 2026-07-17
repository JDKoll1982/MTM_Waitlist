using CommunityToolkit.Mvvm.ComponentModel;

using MTM_Waitlist.Contracts.Services;
using MTM_Waitlist.Contracts.ViewModels;
using MTM_Waitlist.Core.Contracts.Services;
using MTM_Waitlist.Core.Models;

namespace MTM_Waitlist.ViewModels;

public partial class WaitlistViewDetailViewModel : ObservableRecipient, INavigationAware
{
    private readonly ISampleDataService _sampleDataService;
    private readonly IBuildingSelectionService _buildingSelectionService;

    [ObservableProperty]
    public partial SampleOrder? Item
    {
        get; set;
    }

    public WaitlistViewDetailViewModel(ISampleDataService sampleDataService, IBuildingSelectionService buildingSelectionService)
    {
        ArgumentNullException.ThrowIfNull(sampleDataService);
        ArgumentNullException.ThrowIfNull(buildingSelectionService);

        _sampleDataService = sampleDataService;
        _buildingSelectionService = buildingSelectionService;
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (parameter is long orderID)
        {
            var data = await _sampleDataService.GetContentGridDataAsync(_buildingSelectionService.SelectedBuilding);
            Item = data.FirstOrDefault(i => i.OrderID == orderID);
        }
    }

    public void OnNavigatedFrom()
    {
    }
}
