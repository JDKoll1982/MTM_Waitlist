namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// One line of the arrangement's scrap-type list: the type as it is spelled and the lugger that currently holds its
/// dump point (FR-031).
/// </summary>
/// <remarks>
/// The dump point is what the arrangement read resolves, not a column of this row: a type whose luggers can all
/// receive material names the one marked as the dump point, and a type whose luggers are all blocked names the
/// first in its order with that status beside it, which is the answer the handler would be given.
/// </remarks>
public sealed class ScrapTypeDumpPointRow
{
    /// <summary>The scrap type as the assignment row spells it.</summary>
    public string ScrapType { get; init; } = string.Empty;

    /// <summary>The normalised key the procedures match on: trimmed and lower-cased (FR-009).</summary>
    public string ScrapTypeKey { get; init; } = string.Empty;

    /// <summary>The lugger handlers walk to, or null when the type has no lugger at all.</summary>
    public string? DumpPointName { get; init; }

    /// <summary>Whether there is a lugger to name, so the page can say so rather than showing a blank.</summary>
    public bool HasDumpPoint => !string.IsNullOrWhiteSpace(DumpPointName);
}
