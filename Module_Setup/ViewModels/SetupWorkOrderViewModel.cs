using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;

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
    private readonly SemaphoreSlim _searchGate = new(1, 1);

    private static string LocalizeOrDefault(string key, string fallback)
    {
        var localized = key.GetLocalized();
        return string.Equals(localized, key, StringComparison.Ordinal) ? fallback : localized;
    }

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

    [ObservableProperty]
    public partial SetupPartResult? SelectedPart
    {
        get; set;
    }

    [ObservableProperty]
    public partial SetupSequenceResult? SelectedSequence
    {
        get; set;
    }

    public SetupWorkflowState State => _workflowService.State;

    public string PageTitle => LocalizeOrDefault("Shell_ModuleSetup.Content", "Module Setup");

    public string ProgressText => LocalizeOrDefault("Setup_Progress.Step1", "Step 1/5 · 20% complete");

    public Visibility PartSectionVisibility =>
        State.PartResults.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Visibility SequenceSectionVisibility =>
        State.SequenceResults.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;

    public string SelectedPartDisplay => string.IsNullOrWhiteSpace(State.SelectedPartNumber)
        ? LocalizeOrDefault("Setup_Common.None", "None")
        : State.SelectedPartNumber;

    public SetupWorkOrderViewModel(INavigationService navigationService, ISetupWorkflowService workflowService)
    {
        _navigationService = navigationService;
        _workflowService = workflowService;
    }

    public void OnNavigatedTo(object parameter)
    {
        WorkOrderInput = State.WorkOrderInput;
        StatusMessage = State.ValidationMessage;
        SelectedPart = State.PartResults.FirstOrDefault(part => string.Equals(part.PartNumber, State.SelectedPartNumber, StringComparison.OrdinalIgnoreCase));
        SelectedSequence = State.SequenceResults.FirstOrDefault(sequence => string.Equals(sequence.SequenceNumber, State.SelectedSequence, StringComparison.OrdinalIgnoreCase));
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(PartSectionVisibility));
        OnPropertyChanged(nameof(SequenceSectionVisibility));
        OnPropertyChanged(nameof(SelectedPartDisplay));
    }

    public void OnNavigatedFrom()
    {
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await ExecuteSearchAsync().ConfigureAwait(true);
    }

    public async Task AutoSearchOnWorkOrderBlurAsync()
    {
        if (string.IsNullOrWhiteSpace(WorkOrderInput))
        {
            return;
        }

        await ExecuteSearchAsync().ConfigureAwait(true);
    }

    private async Task ExecuteSearchAsync()
    {
        if (!await _searchGate.WaitAsync(0).ConfigureAwait(true))
        {
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _workflowService.SearchWorkOrderAsync(WorkOrderInput).ConfigureAwait(true);
            StatusMessage = result.Message;

            if (!string.IsNullOrWhiteSpace(State.NormalizedWorkOrder))
            {
                WorkOrderInput = State.NormalizedWorkOrder;
            }

            if (result.Success)
            {
                if (State.PartResults.Count == 1)
                {
                    SelectedPart = State.PartResults[0];
                }

                SelectedSequence = State.SequenceResults.FirstOrDefault(sequence => string.Equals(sequence.SequenceNumber, State.SelectedSequence, StringComparison.OrdinalIgnoreCase));
                OnPropertyChanged(nameof(PartSectionVisibility));
                OnPropertyChanged(nameof(SequenceSectionVisibility));
                OnPropertyChanged(nameof(SelectedPartDisplay));
            }
        }
        finally
        {
            IsBusy = false;
            _searchGate.Release();
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _ = _workflowService.ResetAsync();
        WorkOrderInput = string.Empty;
        StatusMessage = string.Empty;
        SelectedPart = null;
        SelectedSequence = null;
        OnPropertyChanged(nameof(PartSectionVisibility));
        OnPropertyChanged(nameof(SequenceSectionVisibility));
        OnPropertyChanged(nameof(SelectedPartDisplay));
    }

    [RelayCommand]
    private void BackToWorkstations()
    {
        _navigationService.NavigateTo(typeof(SetupWorkstationViewModel).FullName!, null);
    }

    [RelayCommand]
    private async Task SelectPartAsync(SetupPartResult? part)
    {
        if (part is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            SelectedPart = part;
            var result = await _workflowService.SelectPartAsync(part.PartNumber).ConfigureAwait(true);
            StatusMessage = result.Message;
            OnPropertyChanged(nameof(SelectedPartDisplay));
            OnPropertyChanged(nameof(SequenceSectionVisibility));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SelectSequenceAsync(SetupSequenceResult? sequence)
    {
        if (sequence is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            SelectedSequence = sequence;
            var result = await _workflowService.SelectSequenceAsync(sequence.SequenceNumber).ConfigureAwait(true);
            StatusMessage = result.Message;

            if (result.Success)
            {
                _navigationService.NavigateTo(typeof(SetupDunnageTypeViewModel).FullName!, null);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}