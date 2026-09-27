namespace MTM_Waitlist.Module_Startup.Models;

/// <summary>
/// One line of the activity feed: one thing the launch did, when it did it, and how it turned out
/// (`contracts/launch-step-contract.md` §3, FR-002).
/// </summary>
/// <remarks>
/// <para>
/// <b>The feed is the debugging surface for a live fault.</b> It names the operation, its target (a store, a
/// share, a server) and its outcome, so a lock-up is attributable to a named line rather than to a step number
/// (S5). <see cref="Target"/> is the target the step declared — a runner writes <see cref="LaunchStep.Target"/> on
/// every line it appends — or <c>null</c> for a line that has no particular target, such as a sub-operation inside
/// a step that touches nothing a person could name.
/// </para>
/// <para>
/// <b>An entry is immutable once appended.</b> The record carries no setter and no member that changes anything,
/// so a reader that holds an entry keeps exactly the entry that was written; appending a later line can never
/// rewrite an earlier one. That is the second half of the feed being append-only — the feed itself never removes
/// or reorders, and an entry once handed out cannot be edited in place.
/// </para>
/// <para>
/// <b>A best-effort failure still gets a line.</b> It is recorded with <see cref="LaunchFeedEntryKind.StepFailed"/>
/// and <c>Succeeded</c> false, because a failure that is reported is not a failure that is hidden (FR-026).
/// </para>
/// </remarks>
/// <param name="TimestampUtc">When the line was written, taken when it is appended rather than by the caller.</param>
/// <param name="StepId">The step the line belongs to, matching <see cref="LaunchStep.Id"/>.</param>
/// <param name="Kind">Whether this is a step transition or a sub-operation within one.</param>
/// <param name="Text">What the line says, in plain language.</param>
/// <param name="Target">
/// The store, share or server the line is about — the step's own <see cref="LaunchStep.Target"/> where it declared
/// one — or <c>null</c> when there is none.
/// </param>
/// <param name="Succeeded">
/// The outcome, or <c>null</c> where the line reports no outcome — an announcement, or a step that was left
/// undone rather than done or failed.
/// </param>
public sealed record LaunchFeedEntry(
    DateTimeOffset TimestampUtc,
    string StepId,
    LaunchFeedEntryKind Kind,
    string Text,
    string? Target,
    bool? Succeeded);
