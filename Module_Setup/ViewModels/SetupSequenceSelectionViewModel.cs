using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;

namespace MTM_Waitlist.Module_Setup.ViewModels;

public partial class SetupSequenceSelectionViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;
    private readonly ISetupWorkflowService _workflowService;

    [ObservableProperty]
    public partial SetupSequenceResult? SelectedSequence
    {
        get; set;
    }

    [ObservableProperty]
    public partial string StatusMessage
    {
        get; set;
    } = string.Empty;

    public SetupWorkflowState State => _workflowService.State;

    public ObservableCollection<SetupSequenceResult> Sequences => State.SequenceResults;

    public string PageTitle => "Setup_Sequence.Title".GetLocalized();

    public string ProgressText => "Setup_Progress.Step3".GetLocalized();

    public SetupSequenceSelectionViewModel(INavigationService navigationService, ISetupWorkflowService workflowService)
    {
        _navigationService = navigationService;
        _workflowService = workflowService;
    }

    public void OnNavigatedTo(object parameter)
    {
        StatusMessage = State.StatusMessage;
        SelectedSequence = State.SequenceResults.FirstOrDefault(sequence => string.Equals(sequence.SequenceNumber, State.SelectedSequence, StringComparison.OrdinalIgnoreCase));
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
        if (SelectedSequence is null)
        {
            StatusMessage = "Setup_Sequence.Validation.SelectSequence".GetLocalized();
            return;
        }

        var result = await _workflowService.SelectSequenceAsync(SelectedSequence.SequenceNumber).ConfigureAwait(true);
        StatusMessage = result.Message;

        if (result.Success)
        {
            _navigationService.NavigateTo(typeof(SetupDunnageTypeViewModel).FullName!, null);
        }
    }
}