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
    /// Reads the session length the launch will apply, so the panel opens on the value in force rather than on the
    /// shipped default (FR-029).
    /// </summary>
    private async Task InitializeSessionLengthAsync()
    {
        if (!CanManageSessionLength)
        {
            return;
        }

        try
        {
            var stored = await _preferences
                .ReadNumberAsync(ScopedPreferenceKeys.SessionLengthMinutes, PreferenceScope.Plant)
                .ConfigureAwait(true);

            SessionLengthMinutes = stored is > 0 ? stored.Value : ScopedPreferenceKeys.DefaultSessionLengthMinutes;
            SessionLengthStatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            AppLog.Error("SettingsSessionLength", ex, "The session length could not be read; the shipped default is shown.");
            SessionLengthMinutes = ScopedPreferenceKeys.DefaultSessionLengthMinutes;
        }

        RefreshSearchVisibility();
    }

    /// <summary>
    /// Stores the session length for the whole plant, and refuses an unauthorised change where the write happens.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The gate is read here rather than inferred from whether the control was drawn, so a caller that reaches the
    /// command without going through the panel is still refused (FR-029).
    /// </para>
    /// <para>
    /// The value is clamped to the declared floor and ceiling before it is stored. A length below the floor would
    /// sign everybody out mid-shift, and the store refuses a non-positive length as well, so neither half of the
    /// pair is the only guard.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private async Task SaveSessionLengthAsync()
    {
        if (!CanManageSessionLength)
        {
            SessionLengthStatusMessage = "Settings_SessionLength_EditRefused.Text".GetLocalized();
            return;
        }

        var requested = (int)Math.Round(SessionLengthMinutes, MidpointRounding.AwayFromZero);
        var clamped = Math.Clamp(
            requested,
            ScopedPreferenceKeys.MinimumSessionLengthMinutes,
            ScopedPreferenceKeys.MaximumSessionLengthMinutes);

        try
        {
            await _preferences
                .WriteNumberAsync(ScopedPreferenceKeys.SessionLengthMinutes, PreferenceScope.Plant, clamped)
                .ConfigureAwait(true);

            SessionLengthMinutes = clamped;
            SessionLengthStatusMessage = "Settings_SessionLength_Saved.Text".GetLocalized();
            AppLog.Info("SettingsSessionLength", $"The session length was stored as {clamped} minute(s).");
        }
        catch (Exception ex)
        {
            AppLog.Error("SettingsSessionLength", ex, "The session length could not be stored.");
            SessionLengthStatusMessage = "Settings_SessionLength_SaveFailed.Text".GetLocalized();
        }
    }


    /// <summary>
    /// Reads the level the log store keeps entries at, so the section opens on the value in force rather than on
    /// the shipped default (FR-032).
    /// </summary>
    private async Task InitializeLogStoreLevelAsync()
    {
        if (_logStoreLevelService is null)
        {
            return;
        }

        try
        {
            var severity = await _logStoreLevelService.ReadStoredLevelAsync().ConfigureAwait(true);

            LogStoreMinimumLevel = severity.ToString();
            LogStoreMinimumLevelInput = severity.ToString();
            LogStoreStatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            // The shipped default is what the store is already doing in this case, so the box is seeded from it
            // rather than left empty, and the failure is recorded rather than swallowed.
            LogStoreMinimumLevel = LogStoreLevel.ShippedMinimum.ToString();
            LogStoreMinimumLevelInput = LogStoreLevel.ShippedMinimum.ToString();
            AppLog.Error("SettingsLogStore", ex, "The log store's level could not be read; the shipped default is shown.");
        }

        RefreshSearchVisibility();
    }

    /// <summary>
    /// Stores how much the log store keeps, for every computer, and applies it to the store straight away.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The gate is read here rather than inferred from whether the control was drawn, so a caller that reaches the
    /// command without going through the panel is refused, and refused in the words the hidden control implies
    /// (FR-054, FR-117).
    /// </para>
    /// <para>
    /// <b>The change needs no restart.</b> The logging module holds one threshold and both doors onto the store
    /// read it, so adopting the new value is what puts it in force for the next entry raised — on this computer
    /// at once, and on every other computer the next time it reads the setting (FR-032).
    /// </para>
    /// <para>
    /// A name this cannot parse is refused rather than quietly stored as the default, because a silent change to
    /// what is recorded is the one outcome a reader could never detect.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private async Task SaveLogStoreLevelAsync()
    {
        if (!CanOpenDeveloperLogPanel || _logStoreLevelService is null)
        {
            LogStoreStatusMessage = "Settings_LogStoreLevel_EditRefused.Text".GetLocalized();
            return;
        }

        if (IsLogStoreBusy)
        {
            return;
        }

        if (Enum.TryParse<LogSeverity>(LogStoreMinimumLevelInput?.Trim(), ignoreCase: true, out var severity) is false
            || Enum.IsDefined(severity) is false)
        {
            LogStoreStatusMessage = "Settings_LogStoreLevel_Invalid.Text".GetLocalized();
            return;
        }

        IsLogStoreBusy = true;

        try
        {
            await _logStoreLevelService
                .SaveLevelAsync(severity, _personIdentity.UserId > 0 ? _personIdentity.UserId : null)
                .ConfigureAwait(true);

            LogStoreMinimumLevel = severity.ToString();
            LogStoreMinimumLevelInput = severity.ToString();
            LogStoreStatusMessage = "Settings_LogStoreLevel_Saved.Text".GetLocalized();

            AppLog.Info("SettingsLogStore", $"The log store's level was stored as '{severity}' for every computer.");
        }
        catch (Exception ex)
        {
            LogStoreStatusMessage = "Settings_LogStoreLevel_SaveFailed.Text".GetLocalized();
            AppLog.Error("SettingsLogStore", ex, "The log store's level could not be stored.");
        }
        finally
        {
            IsLogStoreBusy = false;
        }
    }
}
