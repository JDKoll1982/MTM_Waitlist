using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.ViewModels;

/// <summary>
/// Intermediate "request preview" step of the New Request wizard. Replaces the inline
/// <c>ShowRequestSummaryAsync</c> dialog and is shown only for request types that have
/// no subtype, letting the user review their selection before the final confirmation.
/// </summary>
public partial class NewRequestPreviewViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;

    private NewRequestFlowState? _state;

    [ObservableProperty]
    public partial string WorkCenter
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string RequestType
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string Detail
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial bool HasDetail
    {
        get; set;
    }

    public NewRequestPreviewViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    public void OnNavigatedTo(object parameter)
    {
        if (parameter is not NewRequestFlowState state || state.RequestType is null)
        {
            _navigationService.GoBack();
            return;
        }

        _state = state;
        WorkCenter = state.WorkCenter;
        RequestType = state.RequestType.RequestType;
        Detail = state.InputValue ?? string.Empty;
        HasDetail = !string.IsNullOrWhiteSpace(Detail);
    }

    public void OnNavigatedFrom()
    {
    }

    [RelayCommand]
    private void Continue()
    {
        if (_state is null)
        {
            return;
        }

        _navigationService.NavigateTo(typeof(NewRequestSummaryViewModel).FullName!, _state);
    }

    [RelayCommand]
    private void Back()
    {
        _navigationService.GoBack();
    }
}
