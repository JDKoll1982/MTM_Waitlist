namespace MTM_Waitlist.Module_Core.Models;

/// <summary>The answer to one lookup: the lugger a handler walks to, or why there is none.</summary>
/// <param name="LuggerLocation">The lugger's name, or null when nothing is named.</param>
/// <param name="Outcome">Why the answer is what it is.</param>
public sealed record LuggerLocationResolution(string? LuggerLocation, LuggerLocationOutcome Outcome)
{
    /// <summary>Whether the scrap type is real but the arrangement has no lugger for it (FR-026).</summary>
    public bool IsConfigurationGap => Outcome == LuggerLocationOutcome.NotConfigured;
}
