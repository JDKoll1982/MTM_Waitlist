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

public partial class SetupDunnageTypeViewModel : ObservableRecipient, INavigationAware
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
    private readonly ObservableCollection<SetupDunnageType> _dunnageTypes = new();

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

    public ObservableCollection<SetupDunnageType> DunnageTypes => _dunnageTypes;

    public bool CanManageDefinitions => AllowedQuickAddRoles.Any(role => string.Equals(role, _startupState.CurrentRole, StringComparison.OrdinalIgnoreCase));

    public string PageTitle => "Setup_DunnageType.Title".GetLocalized();

    public string ProgressText => "Setup_Progress.Step4".GetLocalized();

    public string CurrentSelectionSummary => State.SelectedDunnageParts.Count == 0
        ? "Setup_Dunnage.Selection.None".GetLocalized()
        : State.SelectedDunnageSummary;

    public SetupDunnageTypeViewModel(
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
        StartupDebugLog.Info("SetupDunnageTypeVm", "OnNavigatedTo started.");
        StatusMessage = State.StatusMessage;

        _dunnageTypes.Clear();
        foreach (var dunnageType in State.DunnageTypes)
        {
            _dunnageTypes.Add(dunnageType);
        }

        SelectedDunnageType = _dunnageTypes.FirstOrDefault(type => string.Equals(type.Id, State.SelectedDunnageTypeId, StringComparison.OrdinalIgnoreCase));

        if (_dunnageTypes.Count == 0)
        {
            StatusMessage = string.IsNullOrWhiteSpace(StatusMessage)
                ? "No receiving dunnage types are available for this part and sequence."
                : StatusMessage;
            StartupDebugLog.Info("SetupDunnageTypeVm", "No dunnage types available for current context.");
        }

        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(CurrentSelectionSummary));
        OnPropertyChanged(nameof(CanManageDefinitions));
        StartupDebugLog.Info("SetupDunnageTypeVm", $"OnNavigatedTo completed. LocalDunnageTypeCount={_dunnageTypes.Count}.");
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
        if (SelectedDunnageType is null)
        {
            StatusMessage = "Setup_DunnageType.Validation.SelectType".GetLocalized();
            return;
        }

        var result = await _workflowService.SelectDunnageTypeAsync(SelectedDunnageType.Id).ConfigureAwait(true);
        StatusMessage = result.Message;

        if (result.Success)
        {
            _navigationService.NavigateTo(typeof(SetupDunnagePartViewModel).FullName!, null);
        }
    }

    [RelayCommand]
    private async Task RemoveAllForSelectedTypeAsync()
    {
        if (SelectedDunnageType is null)
        {
            StatusMessage = "Setup_DunnageType.Validation.SelectType".GetLocalized();
            return;
        }

        var result = await _workflowService.RemoveAllDunnageForTypeAsync(SelectedDunnageType.Id).ConfigureAwait(true);
        StatusMessage = result.Message;
        OnPropertyChanged(nameof(CurrentSelectionSummary));
    }

    public async Task<SetupSelectionResult> QuickAddTypeAsync(string typeName)
    {
        var result = await _dunnageWorkflowService
            .AddDunnageTypeAsync(typeName, _startupState.CurrentRole)
            .ConfigureAwait(true);

        if (!result.Success)
        {
            StatusMessage = result.Message;
            return result;
        }

        var refreshedTypes = await _dunnageWorkflowService
            .GetDunnageTypesAsync(State.SelectedPartNumber, State.SelectedSequence)
            .ConfigureAwait(true);

        State.DunnageTypes.Clear();
        foreach (var dunnageType in refreshedTypes)
        {
            State.DunnageTypes.Add(dunnageType);
        }

        _dunnageTypes.Clear();
        foreach (var dunnageType in State.DunnageTypes)
        {
            _dunnageTypes.Add(dunnageType);
        }

        SelectedDunnageType = _dunnageTypes.FirstOrDefault(type => string.Equals(type.Name, typeName.Trim(), StringComparison.OrdinalIgnoreCase));
        StatusMessage = result.Message;
        return result;
    }
}