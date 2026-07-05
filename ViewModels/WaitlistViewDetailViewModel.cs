using CommunityToolkit.Mvvm.ComponentModel;

using MTM_Waitlist.Contracts.ViewModels;
using MTM_Waitlist.Core.Contracts.Services;
using MTM_Waitlist.Core.Models;

namespace MTM_Waitlist.ViewModels;

public partial class WaitlistViewDetailViewModel : ObservableRecipient, INavigationAware
{
    private readonly ISampleDataService _sampleDataService;

    [ObservableProperty]
    public partial SampleOrder? Item
    {
        get; set;
    }

    public WaitlistViewDetailViewModel(ISampleDataService sampleDataService)
    {
        _sampleDataService = sampleDataService;
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (parameter is long orderID)
        {
            var data = await _sampleDataService.GetContentGridDataAsync();
            Item = data.First(i => i.OrderID == orderID);
        }
    }

    public void OnNavigatedFrom()
    {
    }
}
