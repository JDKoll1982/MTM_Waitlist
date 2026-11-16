namespace MTM_Database_Deployment.Services;

/// <summary>
/// Which artifact tree belongs to which database.
/// </summary>
/// <remarks>
/// <para>
/// The tool ships two sets of scripts: the waitlist store's under <c>Database/</c>, and the Infor Visual cache
/// mirror's under <c>Database/Mock/</c>. They describe different schemas, so applying the wrong one is not a small
/// mistake — deploying the waitlist tree into the mirror replaced a perfectly good cache with twenty-seven tables
/// the mirror does not use.
/// </para>
/// <para>
/// The family is decided by the database's name, because that is all the tool has before it connects. A name outside
/// both families returns null and the tool refuses to deploy rather than guessing: another application's store
/// (<c>mtm_receiving_application</c>, <c>mtm_wip_application_winforms</c>) is not this tool's to change.
/// </para>
/// </remarks>
public static class DeploymentScope
{
    /// <summary>The waitlist store's tree, which sits at the tool's SQL root itself.</summary>
    public const string WaitlistFolder = "";

    /// <summary>The cache mirror's tree, one folder down.</summary>
    public const string MockFolder = "Mock";

    /// <summary>
    /// The folder, under <paramref name="sqlRoot"/>, whose scripts the named database is deployed from, or null when
    /// this tool has no scripts for it.
    /// </summary>
    public static string? SqlRootFor(string sqlRoot, string database)
    {
        if (string.IsNullOrWhiteSpace(database))
        {
            return null;
        }

        var name = database.Trim();

        if (name.StartsWith("mtm_waitlist", StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrEmpty(WaitlistFolder) ? sqlRoot : Path.Combine(sqlRoot, WaitlistFolder);
        }

        if (name.StartsWith("mtm_mock", StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(sqlRoot, MockFolder);
        }

        return null;
    }

    /// <summary>
    /// The sentence an operator is shown when the chosen database is not one this tool deploys.
    /// </summary>
    public static string UnsupportedDatabaseMessage(string database) =>
        $"This tool updates the waitlist database and the Infor Visual cache mirror. It has no scripts for "
        + $"'{database}', so nothing can be checked or saved while it is selected.";
}
