using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;

namespace MTM_Waitlist.Module_Setup.ViewModels;

public partial class SetupPartSelectionViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;
    private readonly ISetupWorkflowService _workflowService;

    [ObservableProperty]
    public partial SetupPartResult? SelectedPart
    {
        get; set;
    }

    [ObservableProperty]
    public partial string StatusMessage
    {
        get; set;
    } = string.Empty;

    public SetupWorkflowState State => _workflowService.State;

    public ObservableCollection<SetupPartResult> Parts => State.PartResults;

    public string PageTitle => "Setup_Part.Title".GetLocalized();

    public string ProgressText => "Setup_Progress.Step2".GetLocalized();

    public SetupPartSelectionViewModel(INavigationService navigationService, ISetupWorkflowService workflowService)
    {
        _navigationService = navigationService;
        _workflowService = workflowService;
    }

    public void OnNavigatedTo(object parameter)
    {
        StatusMessage = State.ValidationMessage;
        SelectedPart = State.PartResults.FirstOrDefault(part => string.Equals(part.PartNumber, State.SelectedPartNumber, StringComparison.OrdinalIgnoreCase));
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(ProgressText));
    }

    public void OnNavigatedFrom()
    {
    }

    [RelayCommand]
    private void Back()
    {
        _navigationService.GoBack();
    }

    [RelayCommand]
    private async Task ContinueAsync()
    {
        if (SelectedPart is null)
        {
            StatusMessage = "Setup_Part.Validation.SelectPart".GetLocalized();
            return;
        }

        var result = await _workflowService.SelectPartAsync(SelectedPart.PartNumber).ConfigureAwait(true);
        StatusMessage = result.Message;

        if (result.Success)
        {
            _navigationService.NavigateTo(typeof(SetupSequenceSelectionViewModel).FullName!, null);
        }
    }
}