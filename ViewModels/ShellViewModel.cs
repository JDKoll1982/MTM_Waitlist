using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Navigation;
using MTM_Waitlist.Contracts.Services;
using MTM_Waitlist.Models;
using MTM_Waitlist.Views;

namespace MTM_Waitlist.ViewModels;

public partial class ShellViewModel : ObservableRecipient
{
    private readonly IBuildingSelectionService _buildingSelectionService;
    private readonly StartupState _startupState;

    [ObservableProperty]
    public partial bool IsBackEnabled
    {
        get; set;
    }

    [ObservableProperty]
    public partial object? Selected
    {
        get; set;
    }

    [ObservableProperty]
    public partial string? SelectedBuilding
    {
        get; set;
    }

    public IReadOnlyList<string> Buildings => _buildingSelectionService.Buildings;

    public bool IsDeveloperModeVisible => _startupState.IsDeveloper;

    public INavigationService NavigationService
    {
        get;
    }

    public INavigationViewService NavigationViewService
    {
        get;
    }

    public ShellViewModel(
        INavigationService navigationService,
        INavigationViewService navigationViewService,
        IBuildingSelectionService buildingSelectionService,
        StartupState startupState)
    {
        ArgumentNullException.ThrowIfNull(navigationService);
        ArgumentNullException.ThrowIfNull(navigationViewService);
        ArgumentNullException.ThrowIfNull(buildingSelectionService);
        ArgumentNullException.ThrowIfNull(startupState);

        NavigationService = navigationService;
        NavigationService.Navigated += OnNavigated;
        NavigationViewService = navigationViewService;
        _buildingSelectionService = buildingSelectionService;
        _startupState = startupState;
        SelectedBuilding = _buildingSelectionService.SelectedBuilding;
    }

    private void OnNavigated(object sender, NavigationEventArgs e)
    {
        IsBackEnabled = NavigationService.CanGoBack;

        if (e.SourcePageType == typeof(SettingsPage))
        {
            Selected = NavigationViewService.SettingsItem;
            return;
        }

        var selectedItem = NavigationViewService.GetSelectedItem(e.SourcePageType);
        if (selectedItem != null)
        {
            Selected = selectedItem;
        }
    }

    partial void OnSelectedBuildingChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        _buildingSelectionService.SelectedBuilding = value;
    }
}
