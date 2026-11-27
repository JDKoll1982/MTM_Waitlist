using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Permissions;
using MTM_Waitlist.Module_Shared.Helpers;
using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Logging;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;
using Windows.ApplicationModel;

namespace MTM_Waitlist.Module_Settings.ViewModels;

public partial class SettingsViewModel : ObservableRecipient, INavigationAware
{
    /// <summary>
    /// Loads this screen's permission answers when the page is reached, so the gates are read per visit rather
    /// than decided from stored session state at construction (FR-114).
    /// </summary>
    /// <remarks>
    /// The read is issued here rather than in a layout pass, so the screen is never drawn from an answer that has
    /// not arrived and the interface thread is never blocked waiting for one.
    /// </remarks>
    public void OnNavigatedTo(object parameter) => PermissionLoad = LoadPermissionsAsync();

    /// <summary>
    /// Nothing is torn down on the way out: the screen subscribes to no service of its own, so it holds nothing to
    /// release.
    /// </summary>
    /// <remarks>
    /// The permission answers are re-read on every entry rather than kept from the first one. That is not a store
    /// read per visit: the permission service caches a session's answers and is invalidated when one is saved, so
    /// the second ask is free unless somebody's permissions actually changed while this screen was away.
    /// </remarks>
    public void OnNavigatedFrom()
    {
    }

    /// <summary>
    /// Asks the permission service once for every gate this screen and its two child view models need, then
    /// applies the answers.
    /// </summary>
    /// <remarks>
    /// One call answers the whole screen, which is what keeps a store read out of a layout pass and off the
    /// interface thread (FR-056). Every gate stays false until its answer arrives, so a control is never offered
    /// on a guess; and because an unreachable store is answered by each key's shipped fallback rather than by a
    /// refusal (FR-050), the failure path hides the controls instead of breaking the screen. The failure is
    /// recorded rather than swallowed.
    /// </remarks>
    public async Task LoadPermissionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var answers = await _permissionService
                .HasPermissionsAsync(s_permissionKeys, cancellationToken)
                .ConfigureAwait(true);

            CanReadIgnoredLocations = answers[PermissionKeys.SettingsIgnoredLocations];
            CanManageIgnoredLocations = answers[PermissionKeys.SettingsIgnoredLocationsEdit];
            CanManageSessionLength = answers[PermissionKeys.SettingsSessionLength];
            CanOpenDeveloperLogPanel = answers[PermissionKeys.SettingsLogPanel];
            CanManageHotWorkCenters = answers[PermissionKeys.SettingsHotWorkCenters];
            CanManageBuildings = answers[PermissionKeys.SettingsBuildings];
            CanManageImageLocationSettings = answers[PermissionKeys.SettingsPartPictures];
            CanManageStoragePaths = answers[PermissionKeys.SettingsStoragePaths];
            CanRequestCacheRefresh = answers[PermissionKeys.SettingsCacheRefresh];
            CanOpenUserManagement = answers[PermissionKeys.AdminUsers];
            CanOpenPermissions = answers[PermissionKeys.AdminPermissions];
            CanOpenLuggerArrangement = answers[PermissionKeys.SettingsLuggerArrangement];

            UrgencyAllotments.ApplyPermission(answers[PermissionKeys.SettingsUrgencyMinutes]);
            ComputerManagement.ApplyPermission(answers[PermissionKeys.SettingsComputers]);

            RefreshSearchVisibility();
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "SettingsPermissions",
                ex,
                "The Settings screen's permission answers could not be read; every gated control stays hidden rather than being offered on a guess.");
        }
    }

    partial void OnCanManageHotWorkCentersChanged(bool value) =>
        OnPropertyChanged(nameof(CanManageDunnageTypeVisibility));

    // FIX: This partial method is automatically invoked by the MVVM Toolkit source generator 
    // whenever the ElementTheme property is modified, updating our custom XAML text field.
    partial void OnElementThemeChanged(ElementTheme value)
    {
        AppLog.Info("SettingsViewModel", $"Theme changed to '{value}'.");
        OnPropertyChanged(nameof(SelectedThemeText));
        RefreshSearchVisibility();
    }

    partial void OnNewRequestAlertsEnabledChanged(bool value)
    {
        if (_newRequestAlertInitializing)
        {
            return; // initial load; do not log/persist as if the user changed it
        }

        if (!IsNewRequestAlertsAvailable)
        {
            // The installation cannot deliver a notification, so there is no preference to record. The
            // toggle is disabled for this reason; this guard keeps that structural rather than cosmetic.
            AppLog.Info("SettingsViewModel", "NewRequestAlertsEnabled was asked for on an installation that cannot deliver a notification; nothing was stored.");
            return;
        }

        AppLog.Info("SettingsViewModel", $"NewRequestAlertsEnabled changed to {value}.");
        _ = _newRequestAlertService.SetEnabledAsync(value);
        RefreshSearchVisibility();
    }

    partial void OnSearchQueryChanged(string value)
    {
        AppLog.Info("SettingsViewModel", $"SearchQuery changed to '{value}'.");
        RefreshSearchVisibility();
    }

    /// <summary>
    /// Stores the enlarge-pictures preference for the signed-in person. Applied through the shared service, so the
    /// screens already open follow the change rather than needing a restart.
    /// </summary>
    partial void OnEnlargePicturesOnClickChanged(bool value)
    {
        if (_enlargePicturesInitializing)
        {
            return;
        }

        AppLog.Info("SettingsViewModel", $"EnlargePicturesOnClick changed to {value}.");
        _ = _pictureEnlargePreference?.SetEnabledAsync(value);
    }

    partial void OnSelectedWorkstationChanged(string value)
    {
        AppLog.Info("SettingsViewModel", $"SelectedWorkstation changed to '{value}'.");
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        _catalogLoad = LoadCatalogForWorkstationAsync(value);
    }
}
