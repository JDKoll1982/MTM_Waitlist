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
    private readonly IWorkOrderValidationService _workOrderValidationService;
    private readonly SemaphoreSlim _searchGate = new(1, 1);
    private CancellationTokenSource _lifecycleCts = new();
    private bool _isClosing;

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

    public bool IsClosing => _isClosing;

    public string PageTitle => LocalizeOrDefault("Shell_ModuleSetup.Content", "Work Center Setup");

    public string ProgressText => LocalizeOrDefault("Setup_Progress.Step1", "Step 1/5 · 20% complete");

    public Visibility PartSectionVisibility =>
        State.PartResults.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Visibility SequenceSectionVisibility =>
        State.SequenceResults.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;

    /// <summary>
    /// Whether the screen is idle, and therefore whether its actions may be used.
    /// </summary>
    /// <remarks>
    /// A lookup against an unreachable Infor Visual spends seconds failing over to the <c>mtm_mock</c> mirror, so
    /// the Search action is held for that whole time instead of letting a second search be issued behind the
    /// first — which the search gate would silently drop, leaving the operator believing nothing happened.
    /// </remarks>
    public bool IsIdle => !IsBusy;

    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(IsIdle));

    /// <summary>
    /// Clears the busy flag, but only while this view still exists.
    /// </summary>
    /// <remarks>
    /// A command can outlive its page: the window may close while a read is still in flight, and the
    /// continuation then runs against a XAML tree that has already been torn down. Raising a change
    /// notification at that moment writes a bound member through <c>x:Bind</c> and throws
    /// <c>E_UNEXPECTED</c> (0x8000FFFF) — which is how an ordinary shutdown produced a logged first-chance
    /// COMException, from <c>SelectSequenceAsync</c>'s <c>finally</c>. A closed view has no busy state to
    /// show, and the next navigation resolves a fresh view model, so the flag is simply left as it was.
    /// </remarks>
    private void ClearBusyIfViewIsLive()
    {
        if (!_isClosing && !_lifecycleCts.IsCancellationRequested)
        {
            IsBusy = false;
        }
    }

    public string SelectedPartDisplay => string.IsNullOrWhiteSpace(State.SelectedPartNumber)
        ? LocalizeOrDefault("Setup_Common.None", "None")
        : State.SelectedPartNumber;

    public SetupWorkOrderViewModel(
        INavigationService navigationService,
        ISetupWorkflowService workflowService,
        IWorkOrderValidationService workOrderValidationService)
    {
        _navigationService = navigationService;
        _workflowService = workflowService;
        _workOrderValidationService = workOrderValidationService;
    }

    public void OnNavigatedTo(object parameter)
    {
        _isClosing = false;
        if (_lifecycleCts.IsCancellationRequested)
        {
            _lifecycleCts.Dispose();
            _lifecycleCts = new CancellationTokenSource();
        }

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
        NotifyViewClosing();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await ExecuteSearchAsync().ConfigureAwait(true);
    }

    public async Task AutoSearchOnWorkOrderBlurAsync()
    {
        if (_isClosing || _lifecycleCts.IsCancellationRequested || string.IsNullOrWhiteSpace(WorkOrderInput))
        {
            return;
        }

        await ExecuteSearchAsync().ConfigureAwait(true);
    }

    private async Task ExecuteSearchAsync()
    {
        if (_isClosing || _lifecycleCts.IsCancellationRequested)
        {
            return;
        }

        if (!await _searchGate.WaitAsync(0, _lifecycleCts.Token).ConfigureAwait(true))
        {
            return;
        }

        IsBusy = true;
        try
        {
            // Canonicalise the box before the lookup is awaited, not after it returns. This is the operator's
            // first confirmation that their number was understood, so it has to be immediate: on a machine that
            // cannot reach Infor Visual the lookup spends seconds failing over to the mtm_mock mirror, and
            // canonicalising afterwards leaves them staring at raw digits — '55691' — for the whole search.
            // The rule itself stays in WorkOrderValidationService, the same singleton the workflow uses, so what
            // the box shows and what the lookup is handed cannot drift apart. Input the rule rejects is left
            // exactly as typed so the operator can correct it.
            if (_workOrderValidationService.TryNormalize(WorkOrderInput, out var normalizedWorkOrder, out _))
            {
                WorkOrderInput = normalizedWorkOrder;
            }

            var result = await _workflowService.SearchWorkOrderAsync(WorkOrderInput, _lifecycleCts.Token).ConfigureAwait(true);
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
        catch (OperationCanceledException)
        {
            // Ignore cancellations caused by page unload or app shutdown.
        }
        finally
        {
            ClearBusyIfViewIsLive();
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
    private void BackToWorkCenters()
    {
        _navigationService.NavigateTo(typeof(SetupWorkCenterViewModel).FullName!, null);
    }

    [RelayCommand]
    private async Task SelectPartAsync(SetupPartResult? part)
    {
        if (part is null || _isClosing || _lifecycleCts.IsCancellationRequested)
        {
            return;
        }

        IsBusy = true;
        try
        {
            SelectedPart = part;
            var result = await _workflowService.SelectPartAsync(part.PartNumber, _lifecycleCts.Token).ConfigureAwait(true);
            StatusMessage = result.Message;
            OnPropertyChanged(nameof(SelectedPartDisplay));
            OnPropertyChanged(nameof(SequenceSectionVisibility));
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellations caused by page unload or app shutdown.
        }
        finally
        {
            ClearBusyIfViewIsLive();
        }
    }

    [RelayCommand]
    private async Task SelectSequenceAsync(SetupSequenceResult? sequence)
    {
        if (sequence is null || _isClosing || _lifecycleCts.IsCancellationRequested)
        {
            return;
        }

        IsBusy = true;
        try
        {
            SelectedSequence = sequence;
            var result = await _workflowService.SelectSequenceAsync(sequence.SequenceNumber, _lifecycleCts.Token).ConfigureAwait(true);
            StatusMessage = result.Message;

            if (result.Success)
            {
                _navigationService.NavigateTo(typeof(SetupDunnageTypeViewModel).FullName!, null);
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellations caused by page unload or app shutdown.
        }
        finally
        {
            ClearBusyIfViewIsLive();
        }
    }

    public void NotifyViewClosing()
    {
        if (_isClosing)
        {
            return;
        }

        _isClosing = true;

        if (!_lifecycleCts.IsCancellationRequested)
        {
            _lifecycleCts.Cancel();
        }
    }
}