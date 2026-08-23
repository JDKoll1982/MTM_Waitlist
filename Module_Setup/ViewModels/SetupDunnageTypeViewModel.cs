using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;

namespace MTM_Waitlist.Module_Setup.ViewModels;

public partial class SetupDunnageTypeViewModel : ObservableRecipient, INavigationAware
{
    /// <summary>
    /// Placeholder scrap value used by the workflow to indicate "no scrap choice has
    /// been made yet". It is excluded from the picker so the Continue gate can
    /// require an explicit decision.
    /// </summary>
    private const string RequiredScrapPlaceholder = "Scrap Type Required";

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

    /// <summary>
    /// Scrap options shown in the picker: the workflow's scrap types (which include
    /// the "No Scrap" option), excluding the "Scrap Type Required" placeholder so the
    /// Continue gate still requires an explicit scrap decision.
    /// </summary>
    public IEnumerable<string> DisplayScrapTypes
    {
        get
        {
            var displayed = new List<string>();
            foreach (var scrapType in State.ScrapTypes)
            {
                if (!string.Equals(scrapType, RequiredScrapPlaceholder, StringComparison.OrdinalIgnoreCase))
                {
                    displayed.Add(scrapType);
                }
            }

            return displayed;
        }
    }

    /// <summary>
    /// Whether the user has made an explicit scrap decision (picked a real scrap
    /// type or explicitly chose "No Scrap").
    /// </summary>
    public bool HasScrapDecision =>
        !string.IsNullOrWhiteSpace(SelectedScrapType)
        && !string.Equals(SelectedScrapType, RequiredScrapPlaceholder, StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether the user still needs to make a scrap decision.</summary>
    public bool IsScrapSelectionMissing => !HasScrapDecision;

    /// <summary>Visibility of the inline scrap notification next to the picker.</summary>
    public Visibility ScrapSelectionNotificationVisibility => IsScrapSelectionMissing ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>Whether Continue can be used (a scrap decision has been made).</summary>
    public bool CanContinue => HasScrapDecision;

    /// <summary>Inline notification shown next to the scrap picker when no decision has been made.</summary>
    public string ScrapSelectionMessage => LocalizeOrDefault(
        "Setup_DunnagePair.ScrapSelection.Message",
        "Choose a scrap type or select \"No Scrap\" to continue.");

    public string SelectedScrapType
    {
        get => State.SelectedScrapType;
        set
        {
            if (State.SelectedScrapType == value)
            {
                StartupDebugLog.Info("SetupDunnageTypeVm", $"SelectedScrapType setter skipped because value is unchanged. Value='{value}'.");
                return;
            }

            var previous = State.SelectedScrapType;
            State.SelectedScrapType = value;
            State.HasUnsavedChanges = true;
            StartupDebugLog.Info("SetupDunnageTypeVm", $"SelectedScrapType changed. Previous='{previous}', New='{State.SelectedScrapType}', HasUnsavedChanges={State.HasUnsavedChanges}.");
            OnPropertyChanged(nameof(SelectedScrapType));
            OnPropertyChanged(nameof(HasScrapDecision));
            OnPropertyChanged(nameof(IsScrapSelectionMissing));
            OnPropertyChanged(nameof(ScrapSelectionNotificationVisibility));
            OnPropertyChanged(nameof(CanContinue));
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
            var requiredPlaceholder = State.ScrapTypes.FirstOrDefault() ?? string.Empty;
            var currentSetting = string.IsNullOrWhiteSpace(State.SelectedScrapType)
                ? requiredPlaceholder
                : State.SelectedScrapType;
            StartupDebugLog.Info("SetupDunnageTypeVm", $"OnNavigatedTo scrap bootstrap. Placeholder='{requiredPlaceholder}', CurrentSetting='{currentSetting}', ScrapTypeCount={State.ScrapTypes.Count}, ScrapTypes='{string.Join(" | ", State.ScrapTypes)}'.");

            // Preserve whatever the workflow already selected (a saved value or an
            // explicit choice). The "Scrap Type Required" placeholder is excluded
            // from the picker and is represented as an empty selection so the
            // Continue gate requires an explicit scrap decision.
            State.SelectedScrapType = string.Equals(currentSetting, RequiredScrapPlaceholder, StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : currentSetting;
            StartupDebugLog.Info("SetupDunnageTypeVm", $"OnNavigatedTo final scrap selection applied. SelectedScrapType='{State.SelectedScrapType}'.");

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
            OnPropertyChanged(nameof(DisplayScrapTypes));
            OnPropertyChanged(nameof(HasScrapDecision));
            OnPropertyChanged(nameof(IsScrapSelectionMissing));
            OnPropertyChanged(nameof(ScrapSelectionNotificationVisibility));
            OnPropertyChanged(nameof(CanContinue));
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
    private async Task ContinueToReviewAsync()
    {
        if (State.SelectedDunnageParts.Count > 0 || await ConfirmNoDunnageAsync().ConfigureAwait(true))
        {
            _navigationService.NavigateTo(typeof(SetupReviewViewModel).FullName!, null);
        }
    }

    protected virtual Task<bool> ConfirmNoDunnageAsync()
    {
        return ConfirmNoDunnageCoreAsync();
    }

    private async Task<bool> ConfirmNoDunnageCoreAsync()
    {
        var xamlRoot = (App.MainWindow?.Content as FrameworkElement)?.XamlRoot;
        if (xamlRoot is null)
        {
            // No active XAML root (for example a headless test host); proceed.
            return true;
        }

        var dialog = new ContentDialog
        {
            Title = LocalizeOrDefault("Setup_NoDunnage.DialogTitle", "Continue without dunnage?"),
            Content = LocalizeOrDefault("Setup_NoDunnage.DialogMessage", "No dunnage was selected for this job. Do you want to continue without dunnage?"),
            PrimaryButtonText = LocalizeOrDefault("Setup_NoDunnage.Confirm", "Yes"),
            CloseButtonText = LocalizeOrDefault("Setup_NoDunnage.Cancel", "No"),
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = xamlRoot,
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    public void AddScrapType(string? scrapType)
    {
        StartupDebugLog.Info("SetupDunnageTypeVm", $"AddScrapType invoked. RawInput='{scrapType}'.");
        var normalized = scrapType?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            StartupDebugLog.Info("SetupDunnageTypeVm", "AddScrapType ignored because input was empty after normalization.");
            return;
        }

        var existed = State.ScrapTypes.Any(existing => string.Equals(existing, normalized, StringComparison.OrdinalIgnoreCase));
        if (!State.ScrapTypes.Any(existing => string.Equals(existing, normalized, StringComparison.OrdinalIgnoreCase)))
        {
            State.ScrapTypes.Add(normalized);
            StartupDebugLog.Info("SetupDunnageTypeVm", $"AddScrapType appended new scrap type. Value='{normalized}'.");
        }
        else
        {
            StartupDebugLog.Info("SetupDunnageTypeVm", $"AddScrapType found existing value and did not append. Value='{normalized}'.");
        }

        SelectedScrapType = normalized;
        State.HasUnsavedChanges = true;
        StartupDebugLog.Info("SetupDunnageTypeVm", $"AddScrapType completed. AddedNewValue={!existed}, FinalSelectedScrapType='{SelectedScrapType}', ScrapTypeCount={State.ScrapTypes.Count}, ScrapTypes='{string.Join(" | ", State.ScrapTypes)}'.");
        OnPropertyChanged(nameof(ScrapTypes));
        OnPropertyChanged(nameof(DisplayScrapTypes));
    }
}