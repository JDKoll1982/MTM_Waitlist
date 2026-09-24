using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// The per-person "click a picture to enlarge it" preference, kept in <c>config_settings_values</c> against the
/// signed-in account's own scope key (FR-030).
/// </summary>
/// <remarks>
/// <para>
/// This is the one flag stored per account rather than per machine. It is a reading preference, so it belongs to
/// the person: two operators sharing a workstation should not have to agree about it, and an operator who signs in
/// anywhere should find the same answer.
/// </para>
/// <para>
/// The value is cached after it is read. A click on a picture must not wait on a database round trip, and the
/// service is a singleton precisely so that the settings screen's write and the controls' reads are the same
/// value — switching the feature off applies to the screen already open.
/// </para>
/// <para>
/// Every failure path answers the declared default (<b>on</b>). Nothing here throws: a picture that cannot be
/// enlarged because the store is unreachable would be a worse outcome than one that can.
/// </para>
/// </remarks>
public sealed class PictureEnlargePreference : IPictureEnlargePreference
{
    /// <summary>The scope type for a value that belongs to one account.</summary>
    private const string UserScopeType = "user";

    private readonly IConfigSettingsValueService _settingsValueService;
    private readonly StartupState _startupState;

    private bool _isEnabled = true;
    private bool _hasLoaded;

    public PictureEnlargePreference(
        IConfigSettingsValueService settingsValueService,
        StartupState startupState)
    {
        _settingsValueService = settingsValueService ?? throw new ArgumentNullException(nameof(settingsValueService));
        _startupState = startupState ?? throw new ArgumentNullException(nameof(startupState));
    }

    /// <inheritdoc />
    public bool IsEnabled => _isEnabled;

    /// <inheritdoc />
    public async Task LoadAsync()
    {
        if (_hasLoaded)
        {
            return;
        }

        if (_startupState.UserId <= 0)
        {
            // Nobody is signed in, so there is no account to read a preference for and no account to write one
            // against. The declared default stands, and the load is *not* marked done: a later call, once the
            // session exists, still reads the stored value.
            return;
        }

        _hasLoaded = true;

        try
        {
            var stored = await _settingsValueService
                .GetSettingValueAsync(ConfigSettingKeys.EnlargePicturesOnClick, UserScopeKey())
                .ConfigureAwait(true);

            if (stored?.SettingValueBool is { } value)
            {
                _isEnabled = value;
            }
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error(
                "PictureEnlargePreference",
                ex,
                "The enlarge-pictures preference could not be read; pictures will enlarge, which is the shipped default.");
        }
    }

    /// <inheritdoc />
    public async Task SetEnabledAsync(bool enabled)
    {
        // Applied first, so the screen the reader is looking at changes even if the store then refuses the write.
        _isEnabled = enabled;
        _hasLoaded = true;

        if (_startupState.UserId <= 0)
        {
            return;
        }

        try
        {
            await _settingsValueService.SetSettingValueAsync(
                new ConfigSettingValue
                {
                    SettingKey = ConfigSettingKeys.EnlargePicturesOnClick,
                    ScopeType = UserScopeType,
                    ScopeKey = UserScopeKey(),
                    UserId = _startupState.UserId,
                    SettingValueBool = enabled,
                    ValueType = "bool",
                },
                _startupState.UserId).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error(
                "PictureEnlargePreference",
                ex,
                "The enlarge-pictures preference could not be stored; it applies to this session only.");
        }
    }

    /// <summary>The scope key that names this person: <c>user:&lt;id&gt;</c>, exactly as the list filter uses it.</summary>
    private string UserScopeKey() => $"user:{_startupState.UserId}";
}
