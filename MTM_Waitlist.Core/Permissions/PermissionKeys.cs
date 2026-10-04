namespace MTM_Waitlist.Module_Core.Permissions;

/// <summary>
/// The nineteen permission keys, one per gated action. The fifteen originals are spelled exactly as the
/// specification's Verbatim Constraints section pins them (FR-045, FR-046); the four added by 010-startup-rebuild
/// are pinned by the machine-configuration contract's section 5 and plan D11.
/// </summary>
/// <remarks>
/// This is the one canonical source for those spellings. Nothing else in the application restates the literals:
/// the declaration reads these constants, and a seed or a test that needs a key takes it from here or from the
/// shipped data, never from a second copy. A key is never re-cased, pluralised, renamed or paraphrased, because
/// it is also the <c>setting_key</c> the store holds and the name a baseline row is written under.
/// </remarks>
public static class PermissionKeys
{
    /// <summary>Accepting, completing and releasing a request.</summary>
    public const string RequestsHandle = "permission.requests.handle";

    /// <summary>Calling the service host's cache-refresh API.</summary>
    public const string CacheRefreshApi = "permission.cache.refresh_api";

    /// <summary>Ignored locations in inventory lists.</summary>
    public const string SettingsIgnoredLocations = "permission.settings.ignored_locations";

    /// <summary>Hot work centres.</summary>
    public const string SettingsHotWorkCenters = "permission.settings.hot_work_centers";

    /// <summary>Part pictures.</summary>
    public const string SettingsPartPictures = "permission.settings.part_pictures";

    /// <summary>Pulling a cache refresh from the Settings screen.</summary>
    public const string SettingsCacheRefresh = "permission.settings.cache_refresh";

    /// <summary>How many minutes each item is allowed.</summary>
    public const string SettingsUrgencyMinutes = "permission.settings.urgency_minutes";

    /// <summary>The defect-type catalogue.</summary>
    public const string SettingsDefectTypes = "permission.settings.defect_types";

    /// <summary>The computer registry.</summary>
    public const string SettingsComputers = "permission.settings.computers";

    /// <summary>
    /// Where the application keeps its pictures and its key files. The site's layout, so it belongs to IT
    /// Department and Developer rather than to the screens it feeds (a wrong picture root stops every configured
    /// picture in the application being read).
    /// </summary>
    public const string SettingsStoragePaths = "permission.settings.storage_paths";

    /// <summary>
    /// Opening machine setup and saving its configuration, before anyone signs in. Deliberately not
    /// <see cref="SettingsComputers"/>: the two surfaces have different blast radii (plan D11).
    /// </summary>
    public const string SettingsMachineConfiguration = "permission.settings.machine_configuration";

    /// <summary>
    /// Changing the plant-wide list of locations to hide. Reading stays on
    /// <see cref="SettingsIgnoredLocations"/>, whose grants are deliberately unchanged (research D10).
    /// </summary>
    public const string SettingsIgnoredLocationsEdit = "permission.settings.ignored_locations_edit";

    /// <summary>The developer log panel in Settings.</summary>
    public const string SettingsLogPanel = "permission.settings.log_panel";

    /// <summary>Changing how long a session lasts.</summary>
    public const string SettingsSessionLength = "permission.settings.session_length";

    /// <summary>Quick Add dunnage definitions.</summary>
    public const string SetupDunnageQuickAdd = "permission.setup.dunnage_quick_add";

    /// <summary>Work-centre setup.</summary>
    public const string SetupWorkCenters = "permission.setup.work_centers";

    /// <summary>Opening the user list, and creating, editing or deactivating a person.</summary>
    public const string AdminUsers = "permission.admin.users";

    /// <summary>Resetting a person's password.</summary>
    public const string AdminResetPassword = "permission.admin.reset_password";

    /// <summary>Opening and editing the permissions page. Fixed: never editable from that page.</summary>
    public const string AdminPermissions = "permission.admin.permissions";

    /// <summary>
    /// Every key, in the order the specification pins them. This is what the declaration's checks iterate.
    /// </summary>
    public static IReadOnlyList<string> All { get; } = new[]
    {
        RequestsHandle,
        CacheRefreshApi,
        SettingsIgnoredLocations,
        SettingsHotWorkCenters,
        SettingsPartPictures,
        SettingsCacheRefresh,
        SettingsUrgencyMinutes,
        SettingsDefectTypes,
        SettingsComputers,
        SettingsStoragePaths,
        SettingsMachineConfiguration,
        SettingsIgnoredLocationsEdit,
        SettingsLogPanel,
        SettingsSessionLength,
        SetupDunnageQuickAdd,
        SetupWorkCenters,
        AdminUsers,
        AdminResetPassword,
        AdminPermissions,
    };
}
