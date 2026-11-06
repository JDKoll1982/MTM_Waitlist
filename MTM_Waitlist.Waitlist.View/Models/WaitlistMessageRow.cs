using Microsoft.UI.Xaml;

namespace MTM_Waitlist.Module_Waitlist.Models;

/// <summary>
/// One line of the request page's message card, ready to draw: a chat bubble in the thread, or a row in the audit
/// view the same card switches to.
/// </summary>
/// <remarks>
/// <para>
/// The row carries the <b>side</b> it is drawn on rather than leaving each template to work it out: a message the
/// signed-in person sent sits on the right, and anything anyone else sent sits on the left, which is the shape every
/// chat surface uses. A row with no author is a change the application recorded on its own — a request created,
/// accepted, completed or cancelled — so it is marked as system rather than left looking like a message from nobody.
/// </para>
/// <para>
/// Only the audit view reads <see cref="OccurredLocalText"/>. The thread deliberately draws neither the time nor
/// anything else beside the words.
/// </para>
/// </remarks>
public sealed class WaitlistMessageRow
{
    /// <summary>What the row says, in the words that were entered or the event's own name.</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>Who said it, by name, or the system's own attribution when nobody did.</summary>
    public string SenderDisplayName { get; init; } = string.Empty;

    /// <summary>When it happened, formatted the way the application formats every other local time.</summary>
    public string OccurredLocalText { get; init; } = string.Empty;

    /// <summary>Whether the signed-in person is the one who sent it.</summary>
    public bool IsMine { get; init; }

    /// <summary>
    /// Whether a change recorded itself: the request being created, accepted, completed or cancelled. Such a row has
    /// no author, so it never joins the thread and is only reachable through the audit view.
    /// </summary>
    public bool IsSystem { get; init; }

    /// <summary>The side the row is drawn on: the sender's own messages right, everyone else's left.</summary>
    public HorizontalAlignment Alignment => IsMine ? HorizontalAlignment.Right : HorizontalAlignment.Left;

    /// <summary>
    /// Builds a row from one audit entry.
    /// </summary>
    /// <param name="entry">The entry the store recorded.</param>
    /// <param name="currentEmployeeNumber">The signed-in person's number, or empty when nobody is known.</param>
    /// <param name="fallbackText">
    /// What to say when the entry carries no words of its own, or null to leave the text empty. Only the audit view
    /// passes this, because the thread holds words and nothing else.
    /// </param>
    public static WaitlistMessageRow From(
        WaitlistRequestAuditEntry entry,
        string? currentEmployeeNumber,
        string? fallbackText = null)
    {
        System.ArgumentNullException.ThrowIfNull(entry);

        var details = entry.Details?.Trim() ?? string.Empty;
        var me = currentEmployeeNumber?.Trim() ?? string.Empty;

        return new WaitlistMessageRow
        {
            Text = details.Length > 0 ? details : fallbackText ?? string.Empty,
            SenderDisplayName = entry.ActorDisplayName,
            OccurredLocalText = entry.OccurredLocalText,
            IsMine = me.Length > 0
                && string.Equals(entry.EmployeeNumber?.Trim(), me, System.StringComparison.OrdinalIgnoreCase),
            IsSystem = string.IsNullOrWhiteSpace(entry.EmployeeName),
        };
    }

    /// <summary>
    /// An event's own name as a reader would say it: <c>NoteUpdated</c> becomes <c>Note updated</c>, so the audit view
    /// shows a word rather than the store's spelling of a message type.
    /// </summary>
    public static string DescribeEvent(string? eventType)
    {
        var name = (eventType ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            return string.Empty;
        }

        var spelled = new System.Text.StringBuilder(name.Length + 4);
        for (var index = 0; index < name.Length; index++)
        {
            var current = name[index];
            if (index > 0 && char.IsUpper(current))
            {
                spelled.Append(' ');
                spelled.Append(char.ToLowerInvariant(current));
                continue;
            }

            spelled.Append(current);
        }

        return spelled.ToString();
    }
}
