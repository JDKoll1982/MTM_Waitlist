using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;

namespace MTM_Waitlist.Module_Setup.ViewModels;

/// <summary>Shows the recorded setup history of the one work center chosen on the Work Center page.</summary>
public partial class SetupWorkCenterHistoryViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;
    private readonly ISetupWorkCenterService _workCenterService;

    public ObservableCollection<SetupWorkCenterHistoryEntry> Entries { get; } = new();

    [ObservableProperty]
    public partial string WorkCenterName
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string Building
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy
    {
        get; set;
    }

    public SetupWorkCenterHistoryViewModel(INavigationService navigationService, ISetupWorkCenterService workCenterService)
    {
        _navigationService = navigationService;
        _workCenterService = workCenterService;
    }

    public void OnNavigatedTo(object parameter)
    {
        if (parameter is SetupWorkCenterHistoryNavigationData data)
        {
            WorkCenterName = data.WorkCenterName;
            Building = data.Building;
        }

        _ = LoadAsync();
    }

    public void OnNavigatedFrom()
    {
    }

    [RelayCommand]
    private void Back()
    {
        if (!_navigationService.GoBack())
        {
            _navigationService.NavigateTo(typeof(SetupWorkCenterViewModel).FullName!, null, true);
        }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync().ConfigureAwait(true);

    private async Task LoadAsync()
    {
        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            var items = await _workCenterService.GetWorkCenterHistoryAsync(WorkCenterName).ConfigureAwait(true);
            Entries.Clear();
            foreach (var item in items)
            {
                Entries.Add(item);
            }

            StatusMessage = Entries.Count == 0 ? "No setup history has been recorded for this work center." : string.Empty;
        }
        catch (Exception ex)
        {
            AppLog.Error("SetupWorkCenterHistory", ex, "The setup history for the work center could not be loaded.");
            StatusMessage = "Setup history is unavailable right now. Use Refresh to try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
