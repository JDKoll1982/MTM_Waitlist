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

public partial class SetupWorkstationViewModel : ObservableRecipient, INavigationAware
{
    private static readonly string[] AllowedManageRoles =
    {
        "Setup Tech",
        "Admin",
        "Developer",
        "Plant Manager",
        "Setup Lead",
        "Production Lead",
    };

    private readonly INavigationService _navigationService;
    private readonly ISetupWorkflowService _workflowService;
    private readonly ISetupWorkstationService _workstationService;
    private readonly StartupState _startupState;
    private readonly ObservableCollection<SetupWorkstation> _displayedWorkstations = new();

    [ObservableProperty]
    public partial SetupWorkstation? SelectedWorkstation
    {
        get; set;
    }

    [ObservableProperty]
    public partial string WorkstationNameInput
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
    public partial string FilterText
    {
        get; set;
    } = string.Empty;

    public SetupWorkflowState State => _workflowService.State;

    public ObservableCollection<SetupWorkstation> Workstations => State.Workstations;

    public ObservableCollection<SetupWorkstation> DisplayedWorkstations => _displayedWorkstations;

    public bool CanManageWorkstations => AllowedManageRoles.Any(role => string.Equals(role, _startupState.CurrentRole, StringComparison.OrdinalIgnoreCase));

    public SetupWorkstationViewModel(
        INavigationService navigationService,
        ISetupWorkflowService workflowService,
        ISetupWorkstationService workstationService,
        StartupState startupState)
    {
        _navigationService = navigationService;
        _workflowService = workflowService;
        _workstationService = workstationService;
        _startupState = startupState;
    }

    public void OnNavigatedTo(object parameter)
    {
        StatusMessage = State.StatusMessage;
        FilterText = string.Empty;
        SelectedWorkstation = State.Workstations.FirstOrDefault(item => string.Equals(item.Name, State.SelectedWorkCenter, StringComparison.OrdinalIgnoreCase));
        _ = LoadWorkstationsAsync();
    }

    public void OnNavigatedFrom()
    {
    }

    [RelayCommand]
    private async Task ContinueAsync()
    {
        if (SelectedWorkstation is null)
        {
            StatusMessage = "Choose a workstation to continue.";
            return;
        }

        State.SelectedWorkCenter = SelectedWorkstation.Name;
        State.StatusMessage = string.Empty;
        State.CurrentStep = SetupWorkflowStep.WorkOrderEntry;
        await Task.CompletedTask;
        _navigationService.NavigateTo(typeof(SetupWorkOrderViewModel).FullName!, null);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadWorkstationsAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task AddWorkstationAsync()
    {
        if (!CanManageWorkstations)
        {
            StatusMessage = "You do not have permission to manage workstations.";
            return;
        }

        var result = await _workstationService.AddWorkstationAsync(WorkstationNameInput).ConfigureAwait(true);
        StatusMessage = result.Message;
        if (result.Success)
        {
            WorkstationNameInput = string.Empty;
            await LoadWorkstationsAsync().ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private async Task UpdateWorkstationAsync()
    {
        if (!CanManageWorkstations)
        {
            StatusMessage = "You do not have permission to manage workstations.";
            return;
        }

        if (SelectedWorkstation is null)
        {
            StatusMessage = "Select a workstation to edit.";
            return;
        }

        var result = await _workstationService.UpdateWorkstationAsync(SelectedWorkstation.Id, WorkstationNameInput).ConfigureAwait(true);
        StatusMessage = result.Message;
        if (result.Success)
        {
            WorkstationNameInput = string.Empty;
            await LoadWorkstationsAsync().ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private async Task RemoveWorkstationAsync()
    {
        if (!CanManageWorkstations)
        {
            StatusMessage = "You do not have permission to manage workstations.";
            return;
        }

        if (SelectedWorkstation is null)
        {
            StatusMessage = "Select a workstation to remove.";
            return;
        }

        var result = await _workstationService.RemoveWorkstationAsync(SelectedWorkstation.Id).ConfigureAwait(true);
        StatusMessage = result.Message;
        if (result.Success)
        {
            WorkstationNameInput = string.Empty;
            await LoadWorkstationsAsync().ConfigureAwait(true);
        }
    }

    partial void OnSelectedWorkstationChanged(SetupWorkstation? value)
    {
        if (value is null)
        {
            return;
        }

        WorkstationNameInput = value.Name;
    }

    partial void OnFilterTextChanged(string value)
    {
        ApplyFilter();
    }

    private async Task LoadWorkstationsAsync()
    {
        IsBusy = true;
        try
        {
            var items = await _workstationService.GetWorkstationsAsync().ConfigureAwait(true);
            State.Workstations.Clear();
            foreach (var item in items)
            {
                State.Workstations.Add(item);
            }

            ApplyFilter();

            if (SelectedWorkstation is null && _displayedWorkstations.Count > 0)
            {
                SelectedWorkstation = _displayedWorkstations.FirstOrDefault(item => string.Equals(item.Name, State.SelectedWorkCenter, StringComparison.OrdinalIgnoreCase))
                    ?? _displayedWorkstations[0];
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFilter()
    {
        _displayedWorkstations.Clear();

        var normalizedFilter = FilterText.Trim();
        var filteredItems = string.IsNullOrWhiteSpace(normalizedFilter)
            ? State.Workstations
            : new ObservableCollection<SetupWorkstation>(State.Workstations.Where(item =>
                item.Name.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase)
                || item.CurrentWorkOrder.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase)
                || item.CurrentSequenceNumber.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase)
                || item.CurrentPartNumber.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase)));

        foreach (var workstation in filteredItems)
        {
            _displayedWorkstations.Add(workstation);
        }

        if (SelectedWorkstation is not null
            && !_displayedWorkstations.Any(item => string.Equals(item.Id, SelectedWorkstation.Id, StringComparison.OrdinalIgnoreCase)))
        {
            SelectedWorkstation = _displayedWorkstations.FirstOrDefault();
        }
    }
}
