namespace MTM_Waitlist.Tests.Module_Settings.Fixtures;

/// <summary>
/// The two preconditions the settings precedence re-rank rests on, recorded as fixture data rather than
/// re-derived at test time.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this is a snapshot.</b> The re-rank of <c>fn_config_settings_scope_rank</c> moves <c>user</c> from
/// third to sixth, above <c>role</c>, <c>admin</c> and <c>developer</c>. That change is only safe because
/// nothing in the store depends on the old order yet: every row today is <c>all_users</c>, so no row is
/// re-ranked by it. Recording that census here, before the change, is what makes the claim checkable
/// afterwards instead of remembered.
/// </para>
/// <para>
/// The second precondition is the account set the role rename has to move. It is a list of sign-in names and
/// deliberately not a count: a count would still pass if the rename moved one account out and a different one
/// in. Phase 4 compares the accounts on IT Department against <see cref="AccountsHoldingRetiredRole"/> for
/// exactly that reason.
/// </para>
/// <para>
/// Both facts come from the shipping seed <c>Database/Seeds/seed_dev_masked_baseline/create.sql</c>. If that
/// seed changes, this fixture is what fails, which is the point.
/// </para>
/// </remarks>
public static class PermissionMigrationBaseline
{
    /// <summary>The shipped seed these preconditions were read from, relative to the repository root.</summary>
    public const string ShippedSeedRelativePath = "Database/Seeds/seed_dev_masked_baseline/create.sql";

    /// <summary>The role code the rename retires, spelled as the catalogue holds it.</summary>
    public const string RetiredRoleCode = "admin";

    /// <summary>The role code the rename installs, spelled as the catalogue holds it.</summary>
    public const string ReplacementRoleCode = "it_department";

    /// <summary>The scope type every row of <c>config_settings_values</c> carries today.</summary>
    public const string TodayScopeType = "all_users";

    /// <summary>
    /// One seeded settings row: its key and the scope it is written for.
    /// </summary>
    /// <param name="SettingKey">The <c>setting_key</c> value.</param>
    /// <param name="ScopeType">The <c>scope_type</c> value.</param>
    /// <param name="ScopeKey">The <c>scope_key</c> value.</param>
    public readonly record struct SeededSettingRow(string SettingKey, string ScopeType, string ScopeKey);

    /// <summary>
    /// The census of <c>config_settings_values</c> as the shipping seed leaves it: three rows, every one
    /// scoped <c>all_users</c>, so the re-rank moves no row's effective answer.
    /// </summary>
    public static IReadOnlyList<SeededSettingRow> SettingsScopeCensus { get; } =
    [
        new("sessions.retention_inactive_days", "all_users", "all_users"),
        new("waitlist.resolved_retention_days", "all_users", "all_users"),
        new("settings.history_retention_days", "all_users", "all_users"),
    ];

    /// <summary>
    /// True when every seeded settings row is scoped <c>all_users</c>, which is what the re-rank's "nothing
    /// observable changes today" claim rests on.
    /// </summary>
    public static bool EverySeededSettingIsAllUsers =>
        SettingsScopeCensus.All(row => string.Equals(row.ScopeType, TodayScopeType, StringComparison.Ordinal));

    /// <summary>
    /// The sign-in names the shipping seed assigns the retired role, spelled as the seed stores them.
    /// </summary>
    public static IReadOnlyList<string> AccountsHoldingRetiredRole { get; } =
    [
        "test.admin",
    ];
}
