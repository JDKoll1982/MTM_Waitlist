using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// One line of the arrangement's summary: a lugger, its status with the reason it carries, and the scrap types it
/// takes (FR-001, FR-019, FR-030).
/// </summary>
/// <remarks>
/// It is a projection of two reads rather than a stored shape: the lugger comes from
/// <c>sp_waitlist_lugger_locations_list</c> and the scrap types from <c>sp_waitlist_lugger_arrangement_list</c>, so
/// a line is only ever as current as the load that built it. Nothing here is written back — every change goes
/// through the arrangement's own procedures.
/// </remarks>
public sealed class LuggerArrangementRow
{
    /// <summary><c>public_id</c>: the lugger every write names, because the numeric id is not the store's handle.</summary>
    public string PublicId { get; init; } = string.Empty;

    /// <summary><c>lugger_name</c>, as the lead last set it.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>One of the four <see cref="LuggerStatus"/> constants.</summary>
    public string Status { get; init; } = LuggerStatus.Backup;

    /// <summary>Why the lugger is <see cref="LuggerStatus.DoNotUse"/>; null for the other three statuses.</summary>
    public string? StatusReason { get; init; }

    /// <summary>Where the lugger sits in the arrangement's own order (FR-001).</summary>
    public int DisplayPosition { get; init; }

    /// <summary>The scrap types this lugger takes, as they are spelled, in that type's try order.</summary>
    public IReadOnlyList<string> ScrapTypes { get; init; } = Array.Empty<string>();

    /// <summary>Whether the lugger takes any scrap type at all, which is stated rather than left blank.</summary>
    public bool HasScrapTypes => ScrapTypes.Count > 0;

    /// <summary>The scrap types in one line, or the shipped sentence for a lugger that takes none.</summary>
    public string ScrapTypesText => HasScrapTypes
        ? string.Join(", ", ScrapTypes)
        : "Settings_Lugger_Arrangement.NoScrapTypes".GetLocalized();

    /// <summary>The status in the reader's words, from the shipped status labels.</summary>
    public string StatusText => Status switch
    {
        LuggerStatus.Available => "Settings_Lugger_Arrangement.StatusAvailable".GetLocalized(),
        LuggerStatus.Backup => "Settings_Lugger_Arrangement.StatusBackup".GetLocalized(),
        LuggerStatus.Full => "Settings_Lugger_Arrangement.StatusFull".GetLocalized(),
        LuggerStatus.DoNotUse => "Settings_Lugger_Arrangement.StatusDoNotUse".GetLocalized(),
        _ => Status,
    };

    /// <summary>The status with its reason beside it while one is required, so a blocked lugger explains itself.</summary>
    public string StatusAndReasonText => RequiresReason && !string.IsNullOrWhiteSpace(StatusReason)
        ? $"{StatusText} — {StatusReason}"
        : StatusText;

    /// <summary>The colour the card's status badge and accent bar are painted in (FR-012).</summary>
    public string StatusBadgeBackground => LuggerStatusBadge.BackgroundFor(Status);

    /// <summary>The colour the badge's text is drawn in, chosen to read over <see cref="StatusBadgeBackground"/>.</summary>
    public string StatusBadgeForeground => LuggerStatusBadge.Foreground;

    /// <summary>
    /// The card's Edit action. It sits on the row rather than on the page because an item template cannot reach the
    /// page's view model reliably, and a button whose text resolves to nothing draws an empty box.
    /// </summary>
    public string EditActionText => "Settings_Lugger_Arrangement.EditAction".GetLocalized();

    /// <summary>The card's Rename action, read the same way as <see cref="EditActionText"/>.</summary>
    public string RenameActionText => "Settings_Lugger_Arrangement.RenameAction".GetLocalized();

    /// <summary>Whether this status requires a reason (FR-019).</summary>
    public bool RequiresReason => LuggerStatus.RequiresReason(Status);
}
