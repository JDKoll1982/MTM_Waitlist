using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;

namespace MTM_Waitlist.Module_Setup.ViewModels;

public partial class SetupWorkOrderViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;
    private readonly ISetupWorkflowService _workflowService;

    [ObservableProperty]
    public partial string WorkOrderInput
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

    public SetupWorkflowState State => _workflowService.State;

    public string PageTitle => "Shell_ModuleSetup.Content".GetLocalized();

    public string ProgressText => "Setup_Progress.Step1".GetLocalized();

    public bool CanGoNext =>
        State.CurrentStep == SetupWorkflowStep.PartSelection
        || State.CurrentStep == SetupWorkflowStep.SequenceSelection;

    public SetupWorkOrderViewModel(INavigationService navigationService, ISetupWorkflowService workflowService)
    {
        _navigationService = navigationService;
        _workflowService = workflowService;
    }

    public void OnNavigatedTo(object parameter)
    {
        WorkOrderInput = State.WorkOrderInput;
        StatusMessage = State.ValidationMessage;
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(ProgressText));
        NextCommand.NotifyCanExecuteChanged();
    }

    public void OnNavigatedFrom()
    {
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        IsBusy = true;
        try
        {
            var result = await _workflowService.SearchWorkOrderAsync(WorkOrderInput).ConfigureAwait(true);
            StatusMessage = result.Message;

            if (result.Success && State.CurrentStep == SetupWorkflowStep.PartSelection)
            {
                _navigationService.NavigateTo(typeof(SetupPartSelectionViewModel).FullName!, null);
            }
            else if (result.Success && State.CurrentStep == SetupWorkflowStep.SequenceSelection)
            {
                _navigationService.NavigateTo(typeof(SetupSequenceSelectionViewModel).FullName!, null);
            }
        }
        finally
        {
            IsBusy = false;
            NextCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _ = _workflowService.ResetAsync();
        WorkOrderInput = string.Empty;
        StatusMessage = string.Empty;
        NextCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void Next()
    {
        if (State.CurrentStep == SetupWorkflowStep.PartSelection)
        {
            _navigationService.NavigateTo(typeof(SetupPartSelectionViewModel).FullName!, null);
            return;
        }

        if (State.CurrentStep == SetupWorkflowStep.SequenceSelection)
        {
            _navigationService.NavigateTo(typeof(SetupSequenceSelectionViewModel).FullName!, null);
        }
    }
}