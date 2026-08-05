using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Module_Setup.ViewModels;

public partial class SetupDunnagePartViewModel : ObservableRecipient, INavigationAware
{
    private static readonly string[] AllowedQuickAddRoles =
    {
        "Admin",
        "Developer",
        "Plant Manager",
        "Setup Lead",
        "Production Lead",
    };

    private readonly INavigationService _navigationService;
    private readonly ISetupWorkflowService _workflowService;
    private readonly IDunnageWorkflowService _dunnageWorkflowService;
    private readonly StartupState _startupState;
    private readonly ObservableCollection<SetupDunnagePart> _filteredDunnageParts = new();

    [ObservableProperty]
    public partial SetupDunnagePart? SelectedDunnagePart
    {
        get; set;
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

    public ObservableCollection<SetupDunnagePart> FilteredDunnageParts => _filteredDunnageParts;

    public ObservableCollection<SetupDunnagePart> SelectedDunnageParts => State.SelectedDunnageParts;

    public bool CanManageDefinitions => AllowedQuickAddRoles.Any(role => string.Equals(role, _startupState.CurrentRole, StringComparison.OrdinalIgnoreCase));

    public string PageTitle => "Setup_DunnagePart.Title".GetLocalized();

    public string ProgressText => "Setup_Progress.Step4".GetLocalized();

    public string CurrentSelectionSummary => SelectedDunnagePart is null
        ? (State.SelectedDunnageParts.Count == 0
            ? "Setup_Dunnage.Selection.None".GetLocalized()
            : State.SelectedDunnageSummary)
        : $"{SelectedDunnagePart.DisplayName} ({SelectedDunnagePart.PartNumber})";

    public SetupDunnagePartViewModel(
        INavigationService navigationService,
        ISetupWorkflowService workflowService,
        IDunnageWorkflowService dunnageWorkflowService,
        StartupState startupState)
    {
        _navigationService = navigationService;
        _workflowService = workflowService;
        _dunnageWorkflowService = dunnageWorkflowService;
        _startupState = startupState;
    }

    public void OnNavigatedTo(object parameter)
    {
        StatusMessage = State.StatusMessage;
        FilterText = string.Empty;
        ApplyFilter();
        SelectedDunnagePart = State.DunnageParts.FirstOrDefault(part => string.Equals(part.Id, State.SelectedDunnagePartId, StringComparison.OrdinalIgnoreCase));
        if (_filteredDunnageParts.Count == 0)
        {
            StatusMessage = string.IsNullOrWhiteSpace(StatusMessage)
                ? "Setup_DunnagePart.EmptyAvailable".GetLocalized()
                : StatusMessage;
        }

        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(CurrentSelectionSummary));
        OnPropertyChanged(nameof(CanManageDefinitions));
    }

    public void OnNavigatedFrom()
    {
    }

    [RelayCommand]
    private void BackToTypes()
    {
        _navigationService.GoBack();
    }

    partial void OnFilterTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSelectedDunnagePartChanged(SetupDunnagePart? value)
    {
        OnPropertyChanged(nameof(CurrentSelectionSummary));
    }

    [RelayCommand]
    private async Task AddSelectedToPairAsync()
    {
        if (SelectedDunnagePart is null)
        {
            StatusMessage = "Setup_DunnagePart.Validation.SelectPart".GetLocalized();
            return;
        }

        var result = await _workflowService.SelectDunnagePartAsync(SelectedDunnagePart.Id).ConfigureAwait(true);
        StatusMessage = result.Message;
        OnPropertyChanged(nameof(CurrentSelectionSummary));
    }

    [RelayCommand]
    private async Task RemoveAssignedPartAsync(SetupDunnagePart? assignedPart)
    {
        if (assignedPart is null)
        {
            return;
        }

        var result = await _workflowService.RemoveDunnagePartAsync(assignedPart.Id).ConfigureAwait(true);
        StatusMessage = result.Message;
        OnPropertyChanged(nameof(CurrentSelectionSummary));
    }

    [RelayCommand]
    private async Task RemoveAllForTypeAsync()
    {
        var typeId = State.SelectedDunnageTypeId;
        if (string.IsNullOrWhiteSpace(typeId))
        {
            StatusMessage = "Setup_DunnagePart.Validation.SelectTypeFirst".GetLocalized();
            return;
        }

        var result = await _workflowService.RemoveAllDunnageForTypeAsync(typeId).ConfigureAwait(true);
        StatusMessage = result.Message;
        OnPropertyChanged(nameof(CurrentSelectionSummary));
    }

    [RelayCommand]
    private async Task ClearAllForPairAsync()
    {
        var result = await _workflowService.ClearAllDunnageForPairAsync().ConfigureAwait(true);
        StatusMessage = result.Message;
        OnPropertyChanged(nameof(CurrentSelectionSummary));
    }

    [RelayCommand]
    private async Task ReviewAsync()
    {
        if (SelectedDunnagePart is not null)
        {
            var addResult = await _workflowService.SelectDunnagePartAsync(SelectedDunnagePart.Id).ConfigureAwait(true);
            if (!addResult.Success)
            {
                StatusMessage = addResult.Message;
                return;
            }
        }

        _navigationService.NavigateTo(typeof(SetupReviewViewModel).FullName!, null);
    }

    public async Task<SetupSelectionResult> QuickAddPartAsync(string partName)
    {
        var result = await _dunnageWorkflowService
            .AddDunnagePartAsync(State.SelectedDunnageTypeId, partName, _startupState.CurrentRole)
            .ConfigureAwait(true);

        if (!result.Success)
        {
            StatusMessage = result.Message;
            return result;
        }

        var refreshedParts = await _dunnageWorkflowService
            .GetDunnagePartsAsync(State.SelectedDunnageTypeId, State.SelectedPartNumber, State.SelectedSequence)
            .ConfigureAwait(true);

        State.DunnageParts.Clear();
        foreach (var dunnagePart in refreshedParts)
        {
            State.DunnageParts.Add(dunnagePart);
        }

        ApplyFilter();
        SelectedDunnagePart = State.DunnageParts.FirstOrDefault(part => string.Equals(part.PartNumber, partName.Trim(), StringComparison.OrdinalIgnoreCase));
        StatusMessage = result.Message;
        return result;
    }

    private void ApplyFilter()
    {
        _filteredDunnageParts.Clear();

        var normalizedFilter = FilterText.Trim();
        var filteredItems = string.IsNullOrWhiteSpace(normalizedFilter)
            ? State.DunnageParts
            : new ObservableCollection<SetupDunnagePart>(State.DunnageParts.Where(part =>
                part.DisplayName.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase)
                || part.PartNumber.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase)
                || part.Metadata.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase)));

        foreach (var part in filteredItems)
        {
            _filteredDunnageParts.Add(part);
        }
    }
}