using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// The append-only sink every launch line goes to (`contracts/launch-step-contract.md` §3, FR-002).
/// </summary>
/// <remarks>
/// <para>
/// One line per thing the process does, with a timestamp, so a lock-up is attributable to a named line rather
/// than to a step number. The feed is never cleared while a launch is running, and an entry once appended is
/// never rewritten or removed — the surface a developer reads during a fault is the record of what happened,
/// in the order it happened.
/// </para>
/// <para>
/// The feed names the operation, its target (a store, a share, a server) and its outcome, which is what makes
/// "the store is slow" distinguishable from "the share is stalled" instead of both reading as "it hangs".
/// </para>
/// </remarks>
public interface ILaunchActivityFeed
{
    /// <summary>
    /// Every line appended so far, in the order it was appended, as a view that cannot be altered.
    /// </summary>
    IReadOnlyList<LaunchFeedEntry> Entries { get; }

    /// <summary>Raised once for each line, in the order the lines were appended.</summary>
    event EventHandler<LaunchFeedEntry>? EntryAppended;

    /// <summary>
    /// Adds one line to the end of the feed. A line is only ever added, never substituted.
    /// </summary>
    /// <param name="entry">The line to add. Its timestamp is the caller's, taken as the line is appended.</param>
    void Append(LaunchFeedEntry entry);
}
