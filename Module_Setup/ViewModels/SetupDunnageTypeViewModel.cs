using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;

namespace MTM_Waitlist.Module_Setup.ViewModels;

public partial class SetupDunnageTypeViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;
    private readonly ISetupWorkflowService _workflowService;

    private static string LocalizeOrDefault(string key, string fallback)
    {
        var localized = key.GetLocalized();
        return string.Equals(localized, key, StringComparison.Ordinal) ? fallback : localized;
    }

    [ObservableProperty]
    public partial string StatusMessage
    {
        get; set;
    } = string.Empty;

    public SetupWorkflowState State => _workflowService.State;

    public IReadOnlyList<SetupDunnageType> DunnageTypes => State.DunnageTypes;

    public IReadOnlyList<SetupDunnagePart> PairAssignments => State.SelectedDunnageParts;

    public IReadOnlyList<string> ScrapTypes => State.ScrapTypes;

    public string SelectedScrapType
    {
        get => State.SelectedScrapType;
        set
        {
            if (State.SelectedScrapType == value)
            {
                return;
            }

            State.SelectedScrapType = value;
            State.HasUnsavedChanges = true;
            OnPropertyChanged(nameof(SelectedScrapType));
        }
    }

    public string PageTitle => "Setup_DunnageType.Title".GetLocalized();

    public string ProgressText => "Setup_Progress.Step4".GetLocalized();

    public string CurrentSelectionSummary => State.SelectedDunnageParts.Count == 0
        ? LocalizeOrDefault("Setup_Dunnage.Selection.None", "No dunnage selected")
        : State.SelectedDunnageSummary;

    public SetupDunnageTypeViewModel(
        INavigationService navigationService,
        ISetupWorkflowService workflowService,
        IDunnageWorkflowService dunnageWorkflowService)
    {
        _navigationService = navigationService;
        _workflowService = workflowService;
    }

    public void OnNavigatedTo(object parameter)
    {
        try
        {
            StartupDebugLog.Info("SetupDunnageTypeVm", "OnNavigatedTo started.");
            StatusMessage = State.StatusMessage;

            if (State.DunnageTypes.Count == 0)
            {
                StatusMessage = string.IsNullOrWhiteSpace(StatusMessage)
                    ? "No receiving dunnage definitions are available for this part and sequence."
                    : StatusMessage;
            }

            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(ProgressText));
            OnPropertyChanged(nameof(CurrentSelectionSummary));
            OnPropertyChanged(nameof(DunnageTypes));
            OnPropertyChanged(nameof(PairAssignments));
            OnPropertyChanged(nameof(ScrapTypes));
            OnPropertyChanged(nameof(SelectedScrapType));
            StartupDebugLog.Info("SetupDunnageTypeVm", $"OnNavigatedTo completed. AvailableTypeCount={State.DunnageTypes.Count}, PairAssignments={State.SelectedDunnageParts.Count}.");
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("SetupDunnageTypeVm", ex, "OnNavigatedTo failed.");
            throw;
        }
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
    private void AddDunnage()
    {
        _navigationService.NavigateTo(typeof(SetupDunnagePartViewModel).FullName!, null);
    }

    [RelayCommand]
    private async Task RemoveAssignedAsync(SetupDunnagePart? assignedPart)
    {
        if (assignedPart is null)
        {
            return;
        }

        var result = await _workflowService.RemoveDunnagePartAsync(assignedPart.Id).ConfigureAwait(true);
        StatusMessage = result.Message;
        OnPropertyChanged(nameof(CurrentSelectionSummary));
        OnPropertyChanged(nameof(PairAssignments));
    }

    [RelayCommand]
    private async Task ClearAllForPairAsync()
    {
        var result = await _workflowService.ClearAllDunnageForPairAsync().ConfigureAwait(true);
        StatusMessage = result.Message;
        OnPropertyChanged(nameof(CurrentSelectionSummary));
        OnPropertyChanged(nameof(PairAssignments));
    }

    [RelayCommand]
    private void ContinueToReview()
    {
        _navigationService.NavigateTo(typeof(SetupReviewViewModel).FullName!, null);
    }

    public void AddScrapType(string? scrapType)
    {
        var normalized = scrapType?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        if (!State.ScrapTypes.Any(existing => string.Equals(existing, normalized, StringComparison.OrdinalIgnoreCase)))
        {
            State.ScrapTypes.Add(normalized);
        }

        SelectedScrapType = State.ScrapTypes.FirstOrDefault() ?? string.Empty;
        State.HasUnsavedChanges = true;
        OnPropertyChanged(nameof(ScrapTypes));
    }
}