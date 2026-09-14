namespace MTM_Waitlist.Module_Core.Services;

/// <summary>
/// The one definition of what a job's stored die row means, shared by Module_Setup and the New Request picker so
/// the two cannot diverge (FR-055).
/// <para>
/// A die row is <b>not</b> always a die. The Infor Visual subordinate-parts query
/// (<c>GetSubordinateParts.sql</c>) writes <c>No Die</c> as the description of an <c>FGT</c> row whose die still
/// has no home location, so a job that has no die at all still comes back carrying a die row — often the default
/// <c>FGT0001-01</c> with an empty location. That row is the <i>absence</i> of a die, not a die: offering it lets
/// an operator raise a die request against a job that has none, and puts an empty location on the card.
/// </para>
/// <para>
/// It lives here because <c>MTM_Waitlist.Core</c> is the only project both <c>MTM_Waitlist.Setup</c> (the
/// workflow) and <c>MTM_Waitlist.Settings</c> (the picker) reference, so neither has to depend on the other —
/// the same arrangement <see cref="ScrapDecisionRules"/> already uses for the scrap placeholder.
/// </para>
/// </summary>
public static class DieDecisionRules
{
    /// <summary>
    /// The description the subordinate-parts query writes on a die row that stands for "this job has no die". The
    /// query emits it whenever the die part's own description is still <c>Die still needs location</c>.
    /// </summary>
    public const string NoDiePlaceholder = "No Die";

    /// <summary>Whether the description is the "this job has no die" marker.</summary>
    public static bool IsNoDiePlaceholder(string? description) =>
        string.Equals(description?.Trim(), NoDiePlaceholder, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Whether the row is a <b>real die</b>: one an operator can actually ask for. A row carrying the placeholder
    /// is not, and neither is a row with no part number to identify it by — the die's <c>FGT</c> number is what
    /// every card and every stored request shows, so a row without one is not usable as a die. Only a job with at
    /// least one real die should be offered the Die Items at all.
    /// </summary>
    public static bool HasRealDie(string? partNumber, string? description) =>
        !string.IsNullOrWhiteSpace(partNumber) && !IsNoDiePlaceholder(description);
}
