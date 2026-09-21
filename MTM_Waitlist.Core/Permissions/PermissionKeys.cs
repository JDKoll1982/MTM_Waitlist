namespace MTM_Waitlist.Module_Core.Permissions;

/// <summary>
/// The fourteen permission keys, one per gated action, spelled exactly as the specification's Verbatim
/// Constraints section pins them (FR-045, FR-046).
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
        SetupDunnageQuickAdd,
        SetupWorkCenters,
        AdminUsers,
        AdminResetPassword,
        AdminPermissions,
    };
}
