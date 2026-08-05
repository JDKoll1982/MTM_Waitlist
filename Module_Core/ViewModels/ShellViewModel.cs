using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Views;
using MTM_Waitlist.Module_Settings.Views;
using MTM_Waitlist.Module_Setup.Views;
using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Views;

using Windows.UI;

namespace MTM_Waitlist.Module_Core.ViewModels;

public partial class ShellViewModel : ObservableRecipient
{
    private readonly IBuildingSelectionService _buildingSelectionService;
    private readonly StartupState _startupState;
    private bool _isWaitlistPageActive;

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

    [ObservableProperty]
    public partial string HeaderText
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string CurrentUserDisplayName
    {
        get; set;
    } = "Not signed in";

    [ObservableProperty]
    public partial string CurrentUserRole
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string CurrentUserIconGlyph
    {
        get; set;
    } = "\uE77B";

    [ObservableProperty]
    public partial Brush CurrentUserBadgeBrush
    {
        get; set;
    } = CreateUserBadgeBrush("#FF5C5C5C");

    public IReadOnlyList<string> Buildings => _buildingSelectionService.Buildings;

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
        HeaderText = "MTM Waitlist";
        RefreshUserInfo();
    }

    public void RefreshUserInfo()
    {
        CurrentUserDisplayName = string.IsNullOrWhiteSpace(_startupState.Username)
            ? "Not signed in"
            : _startupState.Username;
        CurrentUserRole = _startupState.CurrentRole;
        var userPresentation = GetUserPresentation(_startupState.CurrentRole);
        CurrentUserIconGlyph = userPresentation.Glyph;
        CurrentUserBadgeBrush = CreateUserBadgeBrush(userPresentation.ColorHex);
    }

    private static (string Glyph, string ColorHex) GetUserPresentation(string? role)
    {
        return role?.Trim().ToLowerInvariant() switch
        {
            "developer" => ("\uE713", "#FF0078D4"),
            "admin" or "administrator" => ("\uE7EF", "#FFC4314B"),
            "supervisor" or "manager" => ("\uE716", "#FFD67D00"),
            "quality" or "quality inspector" => ("\uE73E", "#FF107C10"),
            "material handler" => ("\uE7B8", "#FF008272"),
            _ => ("\uE77B", "#FF5C5C5C")
        };
    }

    private static SolidColorBrush CreateUserBadgeBrush(string colorHex)
    {
        var hex = colorHex.TrimStart('#');
        var alpha = byte.Parse(hex[..2], System.Globalization.NumberStyles.HexNumber);
        var red = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        var green = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        var blue = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
        return new SolidColorBrush(Color.FromArgb(alpha, red, green, blue));
    }

    private void OnNavigated(object sender, NavigationEventArgs e)
    {
        IsBackEnabled = NavigationService.CanGoBack;

        if (e.SourcePageType == typeof(WaitlistViewDetailPage))
        {
            _isWaitlistPageActive = false;
            Selected = null;
            UpdateWaitlistDetailHeader();
            return;
        }

        _isWaitlistPageActive = e.SourcePageType == typeof(WaitlistViewPage);

        if (_isWaitlistPageActive)
        {
            Selected = NavigationViewService.GetSelectedItem(e.SourcePageType);
            UpdateWaitlistHeader();
            return;
        }

        if (e.SourcePageType == typeof(SettingsPage))
        {
            Selected = NavigationViewService.SettingsItem;
            HeaderText = "Settings";
            return;
        }

        if (e.SourcePageType.Namespace?.StartsWith("MTM_Waitlist.Module_Setup.Views", StringComparison.Ordinal) == true)
        {
            Selected = NavigationViewService.GetSelectedItem(typeof(SetupWorkOrderPage));
            HeaderText = "Module Setup";
            return;
        }

        var selectedItem = NavigationViewService.GetSelectedItem(e.SourcePageType);
        if (selectedItem != null)
        {
            Selected = selectedItem;
            if (selectedItem is ContentControl contentControl)
            {
                HeaderText = contentControl.Content?.ToString() ?? string.Empty;
            }
            else
            {
                HeaderText = string.Empty;
            }
            return;
        }

        HeaderText = string.Empty;
    }

    private void UpdateWaitlistHeader()
    {
        HeaderText = string.IsNullOrWhiteSpace(SelectedBuilding)
            ? "Waitlist"
            : $"Waitlist for \"{SelectedBuilding}\"";
    }

    private void UpdateWaitlistDetailHeader()
    {
        if (NavigationService.Frame?.Content is not WaitlistViewDetailPage detailPage
            || detailPage.ViewModel.Item is not SampleOrder item)
        {
            HeaderText = "Details";
            return;
        }

        var jobType = item.ImagePath.Trim().ToLowerInvariant() switch
        {
            "coil.png" => "Coil",
            "pickup_fg.png" => "Finished Goods",
            "pickup_ncm.png" => "NCM",
            "pickup_os.png" => "Outside Service",
            "pickup_wip.png" => "WIP",
            "scrap.png" => "Scrap",
            _ => item.Title
        };
        var workCenter = string.IsNullOrWhiteSpace(item.RequestedPressName)
            ? "Unknown Work Center"
            : item.RequestedPressName;

        HeaderText = $"Details for {jobType}, requested by {workCenter}";
    }

    partial void OnSelectedBuildingChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        _buildingSelectionService.SelectedBuilding = value;

        if (_isWaitlistPageActive)
        {
            UpdateWaitlistHeader();
        }
    }
}
