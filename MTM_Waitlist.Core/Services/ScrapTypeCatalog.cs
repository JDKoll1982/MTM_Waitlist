namespace MTM_Waitlist.Module_Core.Services;

/// <summary>
/// The one list of the scrap types the plant ships with, read by the Setup workflow's scrap picker and by the
/// lugger arrangement editor so the two cannot disagree about what a scrap type is (FR-011).
/// <para>
/// It holds the shipped <b>scrap types</b> and nothing else. <see cref="ScrapDecisionRules.RequiredPlaceholder"/>
/// and <see cref="ScrapDecisionRules.NoScrap"/> are deliberately absent: they answer whether there is scrap, they
/// are not a destination for it, so neither may be assigned to a lugger or produce one on a card (FR-010).
/// </para>
/// <para>
/// The spellings are the plant's own, including <c>5052 aluminum</c> in lower case, which is how
/// <c>SetupWorkflowService</c>'s list already spells it. <see cref="Contains"/> compares without regard to case,
/// because a job's stored scrap type reaches the arrangement in whatever case Visual recorded it (FR-009).
/// </para>
/// <para>
/// It lives here because <c>MTM_Waitlist.Core</c> is the only project both <c>MTM_Waitlist.Setup</c> and
/// <c>MTM_Waitlist.Settings</c> reference — the arrangement <see cref="ScrapDecisionRules"/> already uses.
/// </para>
/// </summary>
public static class ScrapTypeCatalog
{
    private static readonly string[] s_types =
    [
        "3003 Aluminum",
        "5052 aluminum",
        "Galvanized Steel",
        "Steel",
        "Skeleton",
        "Gaylord",
    ];

    /// <summary>The shipped scrap types, in the order the pickers list them.</summary>
    public static IReadOnlyList<string> All => s_types;

    /// <summary>
    /// Whether the value names one of the shipped scrap types, ignoring case and surrounding spaces. A type outside
    /// the list may still be recorded against a lugger (FR-011), so this answers what the list recognises, never
    /// what is allowed.
    /// </summary>
    public static bool Contains(string? scrapType)
    {
        var candidate = scrapType?.Trim();
        if (string.IsNullOrEmpty(candidate))
        {
            return false;
        }

        foreach (var type in s_types)
        {
            if (string.Equals(type, candidate, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
