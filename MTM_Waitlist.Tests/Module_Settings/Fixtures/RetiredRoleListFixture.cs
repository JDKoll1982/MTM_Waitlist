namespace MTM_Waitlist.Tests.Module_Settings.Fixtures;

/// <summary>
/// The twelve hand-written role lists that decide access today, captured verbatim from the shipping code so a
/// later test can compare the shipped permission baselines against what each list actually admitted.
/// </summary>
/// <remarks>
/// <para>
/// <b>Verbatim, including the entries the catalogue has never held.</b> Two of these lists carry entries that
/// match no role: <c>DefectTypeCatalogService.AllowedRoles</c> holds <c>administrator</c> and
/// <c>SetupWorkCenterViewModel.AllowedManageRoles</c> holds <c>Setup Tech</c>. They are recorded here as
/// written, not cleaned up, because a comparison against a tidied copy would not be a comparison.
/// </para>
/// <para>
/// <b>Eleven of the twelve become named permissions.</b> The twelfth, <see cref="ShellBadgeRoleVocabulary"/>,
/// is the badge map. It is re-keyed to role codes and stays presentation, so it must not become a permission.
/// It is listed here because the audit that proves no gate reads its own role list counts thirteen sites, and
/// this is the one a hand count of twelve missed alongside <c>StartupState.IsDeveloper</c>.
/// </para>
/// <para>
/// <b>The case is part of the data.</b> Some lists compare display names case-insensitively and store them in
/// title case; <c>DefectTypeCatalogService.AllowedRoles</c> stores lower case. Both spellings are kept exactly
/// as the source holds them.
/// </para>
/// </remarks>
public static class RetiredRoleListFixture
{
    /// <summary>Every role entry in <see cref="DefectTypeCatalogAllowedRoles"/> and <see cref="SetupWorkCenterManageRoles"/> that matches no catalogue role.</summary>
    public static IReadOnlyList<string> EntriesMatchingNoCatalogRole { get; } =
    [
        "administrator",
        "Setup Tech",
    ];

    /// <summary>`MTM_Waitlist.Core/Services/RequestActionPolicy.cs` — <c>HandlerRoles</c>.</summary>
    public static IReadOnlyList<string> RequestActionPolicyHandlerRoles { get; } =
    [
        "Material Handler",
        "Production",
        "Production Lead",
        "Setup",
        "Setup Lead",
        "Plant Manager",
        "Admin",
        "Developer",
    ];

    /// <summary>`MTM_Waitlist.Mock.Service/Api/ServiceOperatorRoles.cs` — <c>Approved</c>.</summary>
    public static IReadOnlyList<string> ServiceOperatorRolesApproved { get; } =
    [
        "Admin",
        "Developer",
        "Plant Manager",
        "Setup Lead",
        "Production Lead",
    ];

    /// <summary>`MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs` — <c>AllowedIgnoredLocationManageRoles</c>.</summary>
    public static IReadOnlyList<string> SettingsIgnoredLocationManageRoles { get; } =
    [
        "Admin",
        "Developer",
        "Plant Manager",
        "Production",
        "Production Lead",
        "Setup",
        "Setup Lead",
    ];

    /// <summary>`MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs` — <c>AllowedHotWorkCenterManageRoles</c>.</summary>
    public static IReadOnlyList<string> SettingsHotWorkCenterManageRoles { get; } =
    [
        "Admin",
        "Developer",
        "Plant Manager",
        "Setup Lead",
        "Production Lead",
    ];

    /// <summary>`MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs` — <c>AllowedImageLocationManageRoles</c>.</summary>
    public static IReadOnlyList<string> SettingsImageLocationManageRoles { get; } =
    [
        "Admin",
        "Developer",
    ];

    /// <summary>`MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs` — <c>AllowedCacheRefreshRoles</c>.</summary>
    public static IReadOnlyList<string> SettingsCacheRefreshRoles { get; } =
    [
        "Admin",
        "Developer",
        "Plant Manager",
    ];

    /// <summary>`MTM_Waitlist.Settings/ViewModels/UrgencyAllotmentEditorViewModel.cs` — <c>AllowedUrgencyManageRoles</c>.</summary>
    public static IReadOnlyList<string> UrgencyAllotmentManageRoles { get; } =
    [
        "Admin",
        "Developer",
        "Plant Manager",
    ];

    /// <summary>`MTM_Waitlist.Settings/Services/DefectTypeCatalogService.cs` — <c>AllowedRoles</c>.</summary>
    public static IReadOnlyList<string> DefectTypeCatalogAllowedRoles { get; } =
    [
        "admin",
        "administrator",
        "developer",
    ];

    /// <summary>`MTM_Waitlist.Settings/ViewModels/ComputerManagementViewModel.cs` — <c>AllowedComputerManageRoles</c>.</summary>
    public static IReadOnlyList<string> ComputerManageRoles { get; } =
    [
        "Admin",
        "Developer",
    ];

    /// <summary>`MTM_Waitlist.Setup/Services/DunnageWorkflowService.cs` — <c>AllowedQuickAddRoles</c>.</summary>
    public static IReadOnlyList<string> DunnageQuickAddRoles { get; } =
    [
        "Admin",
        "Developer",
        "Plant Manager",
        "Setup Lead",
        "Production Lead",
    ];

    /// <summary>`MTM_Waitlist.Setup/ViewModels/SetupWorkCenterViewModel.cs` — <c>AllowedManageRoles</c>.</summary>
    public static IReadOnlyList<string> SetupWorkCenterManageRoles { get; } =
    [
        "Setup Tech",
        "Admin",
        "Developer",
        "Plant Manager",
        "Setup Lead",
        "Production Lead",
    ];

    /// <summary>`ViewModels/ShellViewModel.cs` — <c>GetUserPresentation</c>, the badge map. Not a permission.</summary>
    public static IReadOnlyList<string> ShellBadgeRoleVocabulary { get; } =
    [
        "developer",
        "admin",
        "administrator",
        "supervisor",
        "manager",
        "quality",
        "quality inspector",
        "material handler",
    ];

    /// <summary>
    /// The twelve lists by the source member they were read from, so a parity failure can name the gate rather
    /// than a test-local variable.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> BySource { get; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            ["RequestActionPolicy.HandlerRoles"] = RequestActionPolicyHandlerRoles,
            ["ServiceOperatorRoles.Approved"] = ServiceOperatorRolesApproved,
            ["SettingsViewModel.AllowedIgnoredLocationManageRoles"] = SettingsIgnoredLocationManageRoles,
            ["SettingsViewModel.AllowedHotWorkCenterManageRoles"] = SettingsHotWorkCenterManageRoles,
            ["SettingsViewModel.AllowedImageLocationManageRoles"] = SettingsImageLocationManageRoles,
            ["SettingsViewModel.AllowedCacheRefreshRoles"] = SettingsCacheRefreshRoles,
            ["UrgencyAllotmentEditorViewModel.AllowedUrgencyManageRoles"] = UrgencyAllotmentManageRoles,
            ["DefectTypeCatalogService.AllowedRoles"] = DefectTypeCatalogAllowedRoles,
            ["ComputerManagementViewModel.AllowedComputerManageRoles"] = ComputerManageRoles,
            ["DunnageWorkflowService.AllowedQuickAddRoles"] = DunnageQuickAddRoles,
            ["SetupWorkCenterViewModel.AllowedManageRoles"] = SetupWorkCenterManageRoles,
            ["ShellViewModel.GetUserPresentation"] = ShellBadgeRoleVocabulary,
        };
}
