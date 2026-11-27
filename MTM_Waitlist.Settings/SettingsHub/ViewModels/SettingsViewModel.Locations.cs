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
    /// Seeds the editor from the plant-wide list the service reads, so this screen and every filter that hides a
    /// location agree by construction rather than by two reads of one key happening to match.
    /// </summary>
    /// <remarks>
    /// The duplicate read and write this screen used to keep are gone with the local settings mechanism (T146).
    /// The service is the one reader of the stored list, and the one writer of it.
    /// </remarks>
    private async Task InitializeIgnoredLocationsAsync()
    {
        try
        {
            var stored = await _ignoredLocationsService.GetIgnoredLocationsAsync().ConfigureAwait(true);
            ReplaceCollectionValues(IgnoredLocations, stored);
            IgnoredLocationsStatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            AppLog.Error("SettingsIgnoredLocations", ex, "The ignored-locations list could not be read; the shipped defaults are shown.");
            ReplaceCollectionValues(IgnoredLocations, IgnoredLocationDefaults.Locations);
        }

        RefreshSearchVisibility();
    }

    [RelayCommand]
    private async Task AddIgnoredLocationAsync()
    {
        if (!CanManageIgnoredLocations)
        {
            // Refused where the write would happen rather than only where the control is drawn, and refused with a
            // reason the reader can act on (FR-024, FR-029).
            IgnoredLocationsStatusMessage = "Settings_IgnoredLocations_EditRefused.Text".GetLocalized();
            return;
        }

        var code = NormalizeLocationCode(IgnoredLocationInput);
        if (string.IsNullOrWhiteSpace(code))
        {
            IgnoredLocationsStatusMessage = "Enter a valid location code (letters, digits, hyphens).";
            return;
        }

        if (IgnoredLocations.Any(value => string.Equals(value, code, StringComparison.OrdinalIgnoreCase)))
        {
            IgnoredLocationsStatusMessage = $"{code} is already ignored.";
            IgnoredLocationInput = string.Empty;
            return;
        }

        AppLog.Info("SettingsIgnoredLocations", $"AddIgnoredLocationAsync adding '{code}'.");
        IgnoredLocations.Add(code);
        SortCollection(IgnoredLocations);
        IgnoredLocationInput = string.Empty;
        IgnoredLocationsStatusMessage = string.Empty;
        RefreshSearchVisibility();
        await SaveIgnoredLocationsAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task RemoveIgnoredLocationAsync(string? locationCode)
    {
        if (!CanManageIgnoredLocations)
        {
            IgnoredLocationsStatusMessage = "Settings_IgnoredLocations_EditRefused.Text".GetLocalized();
            return;
        }

        var code = (locationCode ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            return;
        }

        var updated = IgnoredLocations
            .Where(value => !string.Equals(value, code, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        ReplaceCollectionValues(IgnoredLocations, updated);
        IgnoredLocationsStatusMessage = string.Empty;
        RefreshSearchVisibility();
        AppLog.Info("SettingsIgnoredLocations", $"RemoveIgnoredLocationAsync removed '{code}'. Remaining={IgnoredLocations.Count}.");
        await SaveIgnoredLocationsAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// Stores the plant-wide list through the service, which is where the edit entitlement is enforced.
    /// </summary>
    /// <remarks>
    /// A refusal is a permission answer rather than a fault, so it is stated in words and is not thrown. A fault is
    /// still reported, because a list that did not save is something the reader needs to know about.
    /// </remarks>
    private async Task SaveIgnoredLocationsAsync()
    {
        try
        {
            var saved = await _ignoredLocationsService
                .SaveIgnoredLocationsAsync(IgnoredLocations.ToList())
                .ConfigureAwait(true);

            if (!saved)
            {
                AppLog.Info("SettingsIgnoredLocations", "The list was not saved because this person may not change the plant-wide list.");
                IgnoredLocationsStatusMessage = "Settings_IgnoredLocations_EditRefused.Text".GetLocalized();
                return;
            }

            IgnoredLocationsStatusMessage = string.Empty;
            AppLog.Info("SettingsIgnoredLocations", $"SaveIgnoredLocationsAsync saved {IgnoredLocations.Count} location(s).");
        }
        catch (Exception ex)
        {
            AppLog.Error("SettingsIgnoredLocations", ex, "SaveIgnoredLocationsAsync failed.");
            IgnoredLocationsStatusMessage = $"Unable to save ignored locations: {ex.Message}";
        }
    }
}
