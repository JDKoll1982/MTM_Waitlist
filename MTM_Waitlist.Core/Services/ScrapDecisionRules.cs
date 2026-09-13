namespace MTM_Waitlist.Module_Core.Services;

/// <summary>
/// The one definition of what a job's stored scrap value means, shared by Module_Setup and the New Request
/// picker so the two cannot diverge (FR-031, contract §5).
/// <para>
/// The stored value has <b>three</b> states, not two, and conflating them is a real defect:
/// <list type="bullet">
/// <item>a <b>real scrap type</b> — a decision was made and something has to be collected;</item>
/// <item><see cref="NoScrap"/> — also a real answer, but an answer meaning there is <i>no</i> scrap to
/// collect; and</item>
/// <item><see cref="RequiredPlaceholder"/> — the workflow's fallback whenever nothing has been saved, so a
/// stored placeholder means <b>no decision was made</b> and must never be presented as a scrap type.</item>
/// </list>
/// </para>
/// <para>
/// It lives here because <c>MTM_Waitlist.Core</c> is the only project both <c>MTM_Waitlist.Settings</c> (the
/// picker) and <c>MTM_Waitlist.Setup</c> (the workflow) reference, so neither has to depend on the other.
/// </para>
/// </summary>
public static class ScrapDecisionRules
{
    /// <summary>A real, selectable scrap type meaning the job has no scrap. A real answer, not an absence.</summary>
    public const string NoScrap = "No Scrap";

    /// <summary>
    /// The placeholder the workflow falls back to when no scrap decision has been saved. It is excluded from
    /// the picker and is never a scrap decision.
    /// </summary>
    public const string RequiredPlaceholder = "Scrap Type Required";

    /// <summary>
    /// Whether the operator has decided at all — a real scrap type <i>or</i> an explicit
    /// <see cref="NoScrap"/>. This is the gate the Setup flow's Continue button uses.
    /// <para>
    /// It is <b>not</b> the rule for offering a Scrap request: it is true for <see cref="NoScrap"/>, and a job
    /// with no scrap has nothing to collect. Use <see cref="HasRealScrapDecision"/> for that.
    /// </para>
    /// </summary>
    public static bool HasScrapDecision(string? scrapType) =>
        !string.IsNullOrWhiteSpace(scrapType) && !IsRequiredPlaceholder(scrapType);

    /// <summary>
    /// Whether the job has a <b>real scrap decision</b>: a value that is set, is not <see cref="NoScrap"/> and
    /// is not <see cref="RequiredPlaceholder"/>. Only such a job should be offered the Scrap Item.
    /// </summary>
    public static bool HasRealScrapDecision(string? scrapType) =>
        HasScrapDecision(scrapType) && !IsNoScrap(scrapType);

    /// <summary>Whether the stored value is the explicit "no scrap" answer.</summary>
    public static bool IsNoScrap(string? scrapType) =>
        string.Equals(scrapType?.Trim(), NoScrap, StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether the stored value is the "no decision saved yet" placeholder.</summary>
    public static bool IsRequiredPlaceholder(string? scrapType) =>
        string.Equals(scrapType?.Trim(), RequiredPlaceholder, StringComparison.OrdinalIgnoreCase);
}
