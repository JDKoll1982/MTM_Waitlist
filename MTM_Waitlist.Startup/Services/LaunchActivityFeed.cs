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
/// <see cref="Entries"/> view is a <see cref="ReadOnlyCollection{T}"/> over the single list the feed grows, so a
/// reader cannot add to it, clear it or take an earlier line away — an attempt throws
/// <see cref="NotSupportedException"/> rather than silently succeeding. And an entry is an immutable record, so
/// even a reader that already holds one cannot rewrite it.
/// </para>
/// <para>
/// <b>The feed is never cleared while a launch is running.</b> There is no clear member at all, which is what
/// makes the rule hold rather than being remembered: the surface a developer reads during a fault is the record
/// of what happened, in the order it happened.
/// </para>
/// <para>
/// <b>Order is the order of occurrence.</b> Entries are added under one lock, so two threads appending at once
/// cannot interleave into a feed whose order disagrees with its timestamps.
/// </para>
/// </remarks>
public sealed class LaunchActivityFeed : ILaunchActivityFeed
{
    private readonly List<LaunchFeedEntry> _entries = [];
    private readonly ReadOnlyCollection<LaunchFeedEntry> _readOnlyEntries;
    private readonly object _gate = new();

    public LaunchActivityFeed()
    {
        // One read-only wrapper over the list the feed grows, not a copy. A reader that asked for Entries before
        // a line was appended keeps watching the same list, which only ever grows at its end.
        _readOnlyEntries = _entries.AsReadOnly();
    }

    /// <inheritdoc />
    public event EventHandler<LaunchFeedEntry>? EntryAppended;

    /// <inheritdoc />
    public IReadOnlyList<LaunchFeedEntry> Entries
    {
        get
        {
            lock (_gate)
            {
                return _readOnlyEntries;
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
