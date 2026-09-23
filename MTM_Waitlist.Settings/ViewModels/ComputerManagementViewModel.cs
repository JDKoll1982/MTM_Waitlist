using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Settings.ViewModels;

/// <summary>
/// Drives the Settings "Computers" management panel (Ref: 9): lists the computer
/// registry and supports add / edit / deactivate / delete. Restricted to
/// Admin / Developer roles.
/// </summary>
public sealed partial class ComputerManagementViewModel : ObservableRecipient
{
    private readonly IComputerRegistryService _computerRegistryService;
    private readonly StartupState _startupState;

    public ComputerManagementViewModel(
        IComputerRegistryService computerRegistryService,
        StartupState startupState)
    {
        _computerRegistryService = computerRegistryService;
        _startupState = startupState;
    }

    /// <summary>
    /// Whether the signed-in person may manage the computer registry, answered from
    /// <c>permission.settings.computers</c> rather than from a list of role names kept here (FR-054).
    /// </summary>
    /// <remarks>
    /// The Settings screen asks the permission service once for every gate it and its child view models need, so
    /// this is an answer handed in by <see cref="ApplyPermission"/> rather than a second read. It stays false
    /// until that answer arrives, which is what keeps the registry from being listed for somebody who may not see
    /// it.
    /// </remarks>
    [ObservableProperty]
    public partial bool CanManageComputers
    {
        get; set;
    }

    /// <summary>
    /// Applies the answer the Settings screen read for this screen's one permission and, when it admits the
    /// person, loads the registry.
    /// </summary>
    /// <param name="canManage">Whether the signed-in person holds it.</param>
    public void ApplyPermission(bool canManage)
    {
        CanManageComputers = canManage;

        if (canManage)
        {
            // The registry is only readable by somebody who may manage it, so the load is the answer's
            // consequence rather than a second decision.
            _ = LoadAsync();
        }
    }

    public bool IsComputersPanelVisible => CanManageComputers && MatchesSearch(
        "computer",
        "computers",
        "registry",
        "mac",
        "display name",
        string.Join(" ", Computers.Select(record => record.GetDisplayLabel())));

    [ObservableProperty]
    public partial bool IsBusy
    {
        get; set;
    }

    [ObservableProperty]
    public partial string StatusMessage
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial ComputerRecord? SelectedComputer
    {
        get; set;
    }

    public bool CanEditSelected => CanManageComputers && SelectedComputer is not null;

    partial void OnSelectedComputerChanged(ComputerRecord? value) => OnPropertyChanged(nameof(CanEditSelected));

    public ObservableCollection<ComputerRecord> Computers { get; } = new();

    [ObservableProperty]
    public partial string SearchQuery
    {
        get; set;
    } = string.Empty;

    partial void OnSearchQueryChanged(string value) => OnPropertyChanged(nameof(IsComputersPanelVisible));

    public async Task LoadAsync()
    {
        if (!CanManageComputers)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var currentComputerName = ResolveCurrentComputerName();
            var computers = await _computerRegistryService.GetAllComputersAsync().ConfigureAwait(true);
            foreach (var computer in computers)
            {
                computer.IsCurrentComputer = !string.IsNullOrWhiteSpace(currentComputerName)
                    && string.Equals(computer.ComputerName, currentComputerName, StringComparison.OrdinalIgnoreCase);
            }

            ReplaceCollectionValues(Computers, computers);
            SelectedComputer = Computers.FirstOrDefault();
            StatusMessage = string.Empty;
            OnPropertyChanged(nameof(IsComputersPanelVisible));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Unable to load computers: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private string ResolveCurrentComputerName()
    {
        if (!string.IsNullOrWhiteSpace(_startupState.HostnameNormalized))
        {
            return _startupState.HostnameNormalized.Trim();
        }

        return Environment.MachineName.Trim();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync(ComputerRecord? computer)
    {
        if (!CanManageComputers || computer is null)
        {
            return;
        }

        try
        {
            var deleted = await _computerRegistryService.DeleteComputerAsync(computer.Id).ConfigureAwait(true);
            if (deleted)
            {
                Computers.Remove(computer);
                if (ReferenceEquals(SelectedComputer, computer))
                {
                    SelectedComputer = Computers.FirstOrDefault();
                }

                StatusMessage = $"Computer '{computer.GetDisplayLabel()}' deleted.";
            }
            else
            {
                StatusMessage = "Unable to delete this computer.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Unable to delete this computer: {ex.Message}";
        }
    }

    private bool MatchesSearch(params string[] values)
    {
        var query = SearchQuery?.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return values.Any(value =>
            !string.IsNullOrWhiteSpace(value) && value.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    private static void ReplaceCollectionValues(ObservableCollection<ComputerRecord> targetCollection, IEnumerable<ComputerRecord> values)
    {
        targetCollection.Clear();
        foreach (var value in values
                     .GroupBy(item => item.Id)
                     .Select(group => group.First())
                     .OrderBy(item => item.GetDisplayLabel(), StringComparer.OrdinalIgnoreCase))
        {
            targetCollection.Add(value);
        }
    }
}
