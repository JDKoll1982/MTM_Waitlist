using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;

namespace MTM_Waitlist.Module_Setup.ViewModels;

public partial class SetupDunnagePartViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;
    private readonly ISetupWorkflowService _workflowService;
    private readonly IDunnageWorkflowService _dunnageWorkflowService;
    private readonly ObservableCollection<SetupDunnageType> _displayedTypes = new();
    private bool _isNavigating;

    private static string LocalizeOrDefault(string key, string fallback)
    {
        var localized = key.GetLocalized();
        return string.Equals(localized, key, StringComparison.Ordinal) ? fallback : localized;
    }

    [ObservableProperty]
    public partial SetupDunnageType? SelectedDunnageType
    {
        get; set;
    }

    [ObservableProperty]
    public partial string StatusMessage
    {
        get; set;
    } = string.Empty;

    public SetupWorkflowState State => _workflowService.State;

    public ObservableCollection<SetupDunnageType> DisplayedTypes => _displayedTypes;

    public IReadOnlyList<string> SortOptions { get; } = new[] { "Name A-Z", "Name Z-A" };

    [ObservableProperty]
    public partial string SelectedSortOption
    {
        get; set;
    } = "Name A-Z";

    public string PageTitle => LocalizeOrDefault("Setup_AddDunnage.TypeSelection.Title", "Select Dunnage Type");

    public string ProgressText => LocalizeOrDefault("Setup_Progress.Step4", "Step 4/5 · 80% complete");

    public string CurrentSelectionSummary => State.SelectedDunnageParts.Count == 0
        ? LocalizeOrDefault("Setup_Dunnage.Selection.None", "No dunnage selected")
        : State.SelectedDunnageSummary;

    public SetupDunnagePartViewModel(
        INavigationService navigationService,
        ISetupWorkflowService workflowService,
        IDunnageWorkflowService dunnageWorkflowService)
    {
        _navigationService = navigationService;
        _workflowService = workflowService;
        _dunnageWorkflowService = dunnageWorkflowService;
    }

    public void OnNavigatedTo(object parameter)
    {
        StatusMessage = State.StatusMessage;
        RefreshDisplayedTypes();
        SelectedDunnageType = null;

        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(CurrentSelectionSummary));
    }

    public void OnNavigatedFrom()
    {
    }

    [RelayCommand]
    private void BackToPair()
    {
        _navigationService.GoBack();
    }

    [RelayCommand]
    private async Task RefreshTypesAsync()
    {
        var types = await _dunnageWorkflowService.GetDunnageTypesAsync(State.SelectedPartNumber, State.SelectedSequence).ConfigureAwait(true);
        State.DunnageTypes.Clear();
        foreach (var type in types)
        {
            State.DunnageTypes.Add(type);
        }

        RefreshDisplayedTypes();
    }

    [RelayCommand]
    private async Task SelectTypeAndNavigateAsync(SetupDunnageType selectedType)
    {
        if (_isNavigating)
        {
            return;
        }

        _isNavigating = true;
        try
        {
            var result = await _workflowService.SelectDunnageTypeAsync(selectedType.Id).ConfigureAwait(true);
            if (!result.Success)
            {
                StatusMessage = result.Message;
                return;
            }

            StatusMessage = result.Message;
            _navigationService.NavigateTo(typeof(SetupDunnageAddPartSelectionViewModel).FullName!, null);
        }
        finally
        {
            _isNavigating = false;
        }
    }

    partial void OnSelectedDunnageTypeChanged(SetupDunnageType? value)
    {
        if (value is null)
        {
            return;
        }

        _ = SelectTypeAndNavigateAsync(value);
    }

    partial void OnSelectedSortOptionChanged(string value)
    {
        RefreshDisplayedTypes();
    }

    private void RefreshDisplayedTypes()
    {
        _displayedTypes.Clear();

        var ordered = string.Equals(SelectedSortOption, "Name Z-A", StringComparison.OrdinalIgnoreCase)
            ? State.DunnageTypes.OrderByDescending(type => type.Name, StringComparer.OrdinalIgnoreCase)
            : State.DunnageTypes.OrderBy(type => type.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var dunnageType in ordered)
        {
            _displayedTypes.Add(dunnageType);
        }
    }
}