namespace MTM_Waitlist.Module_Core.Permissions;

using MTM_Waitlist.Module_Core.Helpers;

/// <summary>
/// The one declaration of every permission: its key, the resource key of its label and of the sentence saying
/// what it gates, the area the permissions page groups it under, the answer used when neither a person's own row
/// nor a role baseline exists, and the gate sites that read it (FR-046).
/// </summary>
/// <remarks>
/// <para>
/// Nothing else may add a permission. A gated action that reads a key this declaration does not hold is a
/// failure rather than a silent refusal (FR-061), and a key nothing reads is reported so the declaration cannot
/// quietly grow entries nothing uses (FR-062).
/// </para>
/// <para>
/// This declaration deliberately carries <b>no</b> per-role baseline. The baselines are seeded rows, written by
/// <c>seed_permission_role_baselines</c> into <c>config_settings_values</c> with <c>scope_type</c> <c>role</c>
/// and <c>scope_key</c> <c>role:&lt;role_code&gt;</c>, so what a role may do changes without a code change
/// (FR-048). Every declared key has a baseline row for every role the catalogue holds, every baseline row names a
/// declared key, and the two directions are checked against each other rather than one being derived from the
/// other (FR-047, FR-060).
/// </para>
/// <para>
/// The label and gates resource keys are derived from the key's own name, so a new key cannot be declared without
/// its two strings following it and a reviewer can find a permission's wording by reading the key. They are
/// <c>Permission_&lt;suffix&gt;.Label</c> and <c>Permission_&lt;suffix&gt;.Gates</c>, where the suffix is the
/// key's own name below the <c>permission.</c> namespace with its dots written as underscores, for example
/// <c>Permission_requests_handle.Label</c>.
/// </para>
/// </remarks>
public static class PermissionRegistry
{
    /// <summary>The namespace every permission key lives under, which is what makes a key recognisable as one.</summary>
    private const string Namespace = "permission.";

    /// <summary>The group the permissions page shows an entry under.</summary>
    public enum Area
    {
        /// <summary>Acting on a request.</summary>
        Requests,

        /// <summary>The service host's cache.</summary>
        Cache,

        /// <summary>The Settings screen's own subjects.</summary>
        Settings,

        /// <summary>Setup workflows.</summary>
        Setup,

        /// <summary>User and permission administration.</summary>
        Administration,
    }

    /// <summary>
    /// One declared permission. <paramref name="Fallback"/> is the answer used when neither a person's own stored
    /// row nor a role baseline exists, and also when the store cannot be reached (FR-049, FR-050, FR-060).
    /// <paramref name="GateSites"/> names the types that read the key, so the two checks over the declaration
    /// compare against a list rather than against a hand count.
    /// </summary>
    public sealed record Entry(
        string Key,
        string LabelResourceKey,
        string GatesResourceKey,
        Area BelongsTo,
        bool Fallback,
        IReadOnlyList<string> GateSites);

    /// <summary>
    /// The fourteen permissions, one per gated action, each spelled as the specification pins it.
    /// </summary>
    /// <remarks>
    /// The fallback column is a deliberate choice rather than a copy of any role's baseline: an unreachable store
    /// must not turn every control in the application into a refusal (FR-050), and it must not hand out anything
    /// new either. Exactly one key is answered <c>true</c>, <c>permission.requests.handle</c>, because accepting
    /// and completing requests is the shop floor's work and an outage must not stop it; every other key falls back
    /// to <c>false</c>, so an outage grants nobody administration, and each of those screens shows its own
    /// unavailable state because it needs the same store to load.
    /// </remarks>
    public static IReadOnlyList<Entry> All { get; } = new[]
    {
        new Entry(
            PermissionKeys.RequestsHandle,
            LabelResourceKey(PermissionKeys.RequestsHandle),
            GatesResourceKey(PermissionKeys.RequestsHandle),
            Area.Requests,
            Fallback: true,
            new[] { "RequestActionPolicy" }),
        new Entry(
            PermissionKeys.CacheRefreshApi,
            LabelResourceKey(PermissionKeys.CacheRefreshApi),
            GatesResourceKey(PermissionKeys.CacheRefreshApi),
            Area.Cache,
            Fallback: false,
            new[] { "ServiceOperatorRoles" }),
        new Entry(
            PermissionKeys.SettingsIgnoredLocations,
            LabelResourceKey(PermissionKeys.SettingsIgnoredLocations),
            GatesResourceKey(PermissionKeys.SettingsIgnoredLocations),
            Area.Settings,
            Fallback: false,
            new[] { "SettingsViewModel" }),
        new Entry(
            PermissionKeys.SettingsHotWorkCenters,
            LabelResourceKey(PermissionKeys.SettingsHotWorkCenters),
            GatesResourceKey(PermissionKeys.SettingsHotWorkCenters),
            Area.Settings,
            Fallback: false,
            new[] { "SettingsViewModel" }),
        new Entry(
            PermissionKeys.SettingsPartPictures,
            LabelResourceKey(PermissionKeys.SettingsPartPictures),
            GatesResourceKey(PermissionKeys.SettingsPartPictures),
            Area.Settings,
            Fallback: false,
            new[] { "SettingsViewModel" }),
        new Entry(
            PermissionKeys.SettingsCacheRefresh,
            LabelResourceKey(PermissionKeys.SettingsCacheRefresh),
            GatesResourceKey(PermissionKeys.SettingsCacheRefresh),
            Area.Settings,
            Fallback: false,
            new[] { "SettingsViewModel" }),
        new Entry(
            PermissionKeys.SettingsUrgencyMinutes,
            LabelResourceKey(PermissionKeys.SettingsUrgencyMinutes),
            GatesResourceKey(PermissionKeys.SettingsUrgencyMinutes),
            Area.Settings,
            Fallback: false,
            new[] { "UrgencyAllotmentEditorViewModel" }),
        new Entry(
            PermissionKeys.SettingsDefectTypes,
            LabelResourceKey(PermissionKeys.SettingsDefectTypes),
            GatesResourceKey(PermissionKeys.SettingsDefectTypes),
            Area.Settings,
            Fallback: false,
            new[] { "DefectTypeCatalogService" }),
        new Entry(
            PermissionKeys.SettingsComputers,
            LabelResourceKey(PermissionKeys.SettingsComputers),
            GatesResourceKey(PermissionKeys.SettingsComputers),
            Area.Settings,
            Fallback: false,
            new[] { "ComputerManagementViewModel" }),
        new Entry(
            PermissionKeys.SetupDunnageQuickAdd,
            LabelResourceKey(PermissionKeys.SetupDunnageQuickAdd),
            GatesResourceKey(PermissionKeys.SetupDunnageQuickAdd),
            Area.Setup,
            Fallback: false,
            new[] { "DunnageWorkflowService" }),
        new Entry(
            PermissionKeys.SetupWorkCenters,
            LabelResourceKey(PermissionKeys.SetupWorkCenters),
            GatesResourceKey(PermissionKeys.SetupWorkCenters),
            Area.Setup,
            Fallback: false,
            new[] { "SetupWorkCenterViewModel" }),
        new Entry(
            PermissionKeys.AdminUsers,
            LabelResourceKey(PermissionKeys.AdminUsers),
            GatesResourceKey(PermissionKeys.AdminUsers),
            Area.Administration,
            Fallback: false,
            new[] { "SettingsViewModel", "UserManagementViewModel" }),
        new Entry(
            PermissionKeys.AdminResetPassword,
            LabelResourceKey(PermissionKeys.AdminResetPassword),
            GatesResourceKey(PermissionKeys.AdminResetPassword),
            Area.Administration,
            Fallback: false,
            new[] { "EditUserViewModel", "EditUserPage" }),
        new Entry(
            PermissionKeys.AdminPermissions,
            LabelResourceKey(PermissionKeys.AdminPermissions),
            GatesResourceKey(PermissionKeys.AdminPermissions),
            Area.Administration,
            Fallback: false,
            new[] { "SettingsViewModel", "PermissionsViewModel" }),
    };

    /// <summary>
    /// The resource key of <paramref name="permissionKey"/>'s plain-language label.
    /// </summary>
    public static string LabelResourceKey(string permissionKey) =>
        $"Permission_{Suffix(permissionKey)}.Label";

    /// <summary>
    /// What <paramref name="permissionKey"/> is called, in the reader's words. Every screen that names a
    /// permission reads it from here, so a permission is never called two things and its key is never shown to a
    /// person.
    /// </summary>
    public static string Label(string permissionKey) => Resolve(LabelResourceKey(permissionKey));

    /// <summary>
    /// The sentence saying what <paramref name="permissionKey"/> gates, in the reader's words.
    /// </summary>
    public static string Gates(string permissionKey) => Resolve(GatesResourceKey(permissionKey));

    /// <summary>
    /// The text for a resource key, or a readable form of the key's own name when the map holds no entry. A key is
    /// never shown to a person, so the fallback is the key's own name with its namespace, its suffix and its
    /// underscores opened out rather than the key itself.
    /// </summary>
    private static string Resolve(string resourceKey)
    {
        var localized = resourceKey.GetLocalized();
        if (!string.Equals(localized, resourceKey, StringComparison.Ordinal))
        {
            return localized;
        }

        var suffix = resourceKey.StartsWith(Namespace, StringComparison.Ordinal)
            ? resourceKey[Namespace.Length..]
            : resourceKey;

        suffix = suffix.EndsWith(".Label", StringComparison.Ordinal)
            ? suffix[..^".Label".Length]
            : suffix.EndsWith(".Gates", StringComparison.Ordinal)
                ? suffix[..^".Gates".Length]
                : suffix;

        return suffix.Replace('.', ' ').Replace('_', ' ');
    }

    /// <summary>
    /// The resource key of <paramref name="permissionKey"/>'s sentence saying what it gates.
    /// </summary>
    public static string GatesResourceKey(string permissionKey) =>
        $"Permission_{Suffix(permissionKey)}.Gates";

    /// <summary>
    /// The declaration for <paramref name="permissionKey"/>, or <c>null</c> when the declaration does not hold
    /// it. A caller that receives <c>null</c> has read a key that is not declared (FR-061).
    /// </summary>
    public static Entry? Find(string permissionKey) =>
        string.IsNullOrWhiteSpace(permissionKey)
            ? null
            : All.FirstOrDefault(entry => string.Equals(entry.Key, permissionKey, StringComparison.Ordinal));

    /// <summary>
    /// Whether the declaration holds <paramref name="permissionKey"/>.
    /// </summary>
    public static bool IsDeclared(string permissionKey) => Find(permissionKey) is not null;

    private static string Suffix(string permissionKey)
    {
        var suffix = permissionKey.StartsWith(Namespace, StringComparison.Ordinal)
            ? permissionKey[Namespace.Length..]
            : permissionKey;

        return suffix.Replace('.', '_');
    }
}
