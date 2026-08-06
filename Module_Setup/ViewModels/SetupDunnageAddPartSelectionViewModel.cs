using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;

namespace MTM_Waitlist.Module_Setup.ViewModels;

public partial class SetupDunnageAddPartSelectionViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;
    private readonly ISetupWorkflowService _workflowService;
    private readonly ObservableCollection<SetupDunnagePart> _displayedParts = new();
    private bool _isSelecting;

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

    [ObservableProperty]
    public partial string FilterText
    {
        get; set;
    } = string.Empty;

    public SetupWorkflowState State => _workflowService.State;

    public ObservableCollection<SetupDunnagePart> DisplayedParts => _displayedParts;

    public string PageTitle => LocalizeOrDefault("Setup_AddDunnage.PartSelection.Title", "Select Dunnage Part");

    public string ProgressText => LocalizeOrDefault("Setup_Progress.Step4", "Step 4/5 · 80% complete");

    public SetupDunnageAddPartSelectionViewModel(INavigationService navigationService, ISetupWorkflowService workflowService)
    {
        _navigationService = navigationService;
        _workflowService = workflowService;
    }

    public void OnNavigatedTo(object parameter)
    {
        StatusMessage = State.StatusMessage;
        FilterText = string.Empty;

        foreach (var dunnagePart in State.DunnageParts)
        {
            dunnagePart.IsSelectedForPair = State.SelectedDunnageParts.Any(assigned => string.Equals(assigned.Id, dunnagePart.Id, StringComparison.OrdinalIgnoreCase));
        }

        ApplyFilter();
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
    private async Task SelectPartAsync(SetupDunnagePart? selectedPart)
    {
        if (_isSelecting || selectedPart is null)
        {
            return;
        }

        _isSelecting = true;
        try
        {
            var result = await _workflowService.SelectDunnagePartAsync(selectedPart.Id).ConfigureAwait(true);
            StatusMessage = result.Message;

            if (result.Success)
            {
                _navigationService.NavigateTo(typeof(SetupDunnageTypeViewModel).FullName!, null);
            }
        }
        finally
        {
            _isSelecting = false;
        }
    }

    partial void OnFilterTextChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        _displayedParts.Clear();
        var normalizedFilter = FilterText.Trim();

        var filteredItems = string.IsNullOrWhiteSpace(normalizedFilter)
            ? State.DunnageParts
            : new ObservableCollection<SetupDunnagePart>(State.DunnageParts.Where(part =>
                part.DisplayName.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase)
                || part.PartNumber.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase)
                || part.Metadata.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase)));

        foreach (var dunnagePart in filteredItems)
        {
            _displayedParts.Add(dunnagePart);
        }
    }
}
