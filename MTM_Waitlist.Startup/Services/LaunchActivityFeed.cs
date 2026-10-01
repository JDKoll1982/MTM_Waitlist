using System.Collections.ObjectModel;

using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// The activity feed: an append-only list of what the launch is doing (`contracts/launch-step-contract.md` §3).
/// </summary>
/// <remarks>
/// <para>
/// <b>Append-only is structural here, not a convention.</b> The only member that changes the feed adds one entry
/// to the end; nothing removes, replaces or reorders anything, and there is no member that could. The
/// <see cref="Entries"/> view is a <see cref="ReadOnlyCollection{T}"/> over a copy of the lines written so far, so
/// a reader cannot add to it, clear it or take an earlier line away — an attempt throws
/// <see cref="NotSupportedException"/> rather than silently succeeding. And an entry is an immutable record, so
/// even a reader that already holds one cannot rewrite it.
/// </para>
/// <para>
/// <b>A reader is handed a copy, and that is what makes reading the feed safe while the launch is writing it.</b>
/// The launch appends from the step thread while the surface reads from the interface thread, and a reader that
/// walked the one list the feed grows would throw <c>Collection was modified; enumeration operation may not
/// execute</c> the moment a step appended a line while it was mid-read — which ends the process as a stowed fault
/// with no account of itself. The copy is taken under the same lock the append takes, so a reader sees the feed as
/// it stood at one instant and never half a line.
/// </para>
/// <para>
/// <b>The feed is never cleared while a launch is running.</b> There is no clear member at all, which is what
/// makes the rule hold rather than being remembered: the surface a developer reads during a fault is the record
/// of what happened, in the order it happened. A line written after a reader took its copy reaches that reader
/// through <see cref="EntryAppended"/>, so the copy costs nothing that the subscription does not already supply.
/// </para>
/// <para>
/// <b>Order is the order of occurrence.</b> Entries are added under one lock, so two threads appending at once
/// cannot interleave into a feed whose order disagrees with its timestamps.
/// </para>
/// </remarks>
public sealed class LaunchActivityFeed : ILaunchActivityFeed
{
    private readonly List<LaunchFeedEntry> _entries = [];
    private readonly object _gate = new();

    /// <inheritdoc />
    public event EventHandler<LaunchFeedEntry>? EntryAppended;

    /// <inheritdoc />
    public IReadOnlyList<LaunchFeedEntry> Entries
    {
        get
        {
            // Copied under the lock the append takes, so a reader enumerating this cannot race a step appending
            // its next line. The copy is exposed read-only, so the view still refuses every change.
            lock (_gate)
            {
                return Array.AsReadOnly(_entries.ToArray());
            }
        }
    }

    /// <inheritdoc />
    public void Append(LaunchFeedEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        EventHandler<LaunchFeedEntry>? appended;

        lock (_gate)
        {
            _entries.Add(entry);
            appended = EntryAppended;
        }

        // Raised after the line is in the feed and outside the lock, so a handler that reads Entries sees its own
        // line and a handler that appends cannot deadlock the feed.
        appended?.Invoke(this, entry);
    }
}
