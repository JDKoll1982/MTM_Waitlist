using CommunityToolkit.Mvvm.ComponentModel;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.ViewModels;

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
            var data = _sampleDataService.GetSampleOrders(_buildingSelectionService.SelectedBuilding);
            Item = data.OfType<SampleOrder>().FirstOrDefault(i => i.Id == orderID);
        }
    }

    public void OnNavigatedFrom()
    {
    }
}
