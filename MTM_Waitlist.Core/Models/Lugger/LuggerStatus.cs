namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// The four statuses a lugger may carry, pinned to the spellings the stored procedures accept.
/// </summary>
/// <remarks>
/// The procedures refuse any other value (FR-012), so this type never invents a fifth: a value that does not
/// match one of the four constants exactly is not a status, and <see cref="Parse"/> answers null for it.
/// </remarks>
public static class LuggerStatus
{
    public const string Available = "Available";

    public const string Backup = "Backup";

    public const string Full = "Full";

    public const string DoNotUse = "Do Not Use";

    /// <summary>
    /// True for the two statuses that admit material: <see cref="Available"/> and <see cref="Backup"/>.
    /// <see cref="Full"/> and <see cref="DoNotUse"/> cannot receive material (FR-013).
    /// </summary>
    public static bool CanReceiveMaterial(string? status) => status is Available or Backup;

    /// <summary>
    /// True for <see cref="DoNotUse"/> alone: it is the only status whose reason is required (FR-019).
    /// </summary>
    public static bool RequiresReason(string? status) => status is DoNotUse;

    /// <summary>
    /// Returns the pinned spelling this value is, or null when it is neither. The comparison is
    /// case-sensitive and does not trim, because the procedures compare the same way.
    /// </summary>
    public static string? Parse(string? value) => value switch
    {
        Available => Available,
        Backup => Backup,
        Full => Full,
        DoNotUse => DoNotUse,
        _ => null,
    };
}
