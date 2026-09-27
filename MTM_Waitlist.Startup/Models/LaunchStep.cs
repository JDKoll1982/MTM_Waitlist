namespace MTM_Waitlist.Module_Startup.Models;

/// <summary>
/// One named piece of work in the launch: what it is called, what it does, where it belongs, what it touches, and
/// how long it may take (`contracts/launch-step-contract.md` §1, FR-002, FR-003).
/// </summary>
/// <remarks>
/// <para>
/// <b>Steps are data.</b> The sequence is a list of these records, not a hard-coded five, so adding or removing
/// a step cannot leave a stale "of 5" on the launch window: the displayed count is computed from the list
/// (see <c>LaunchStepCatalog.TotalCount</c>) and is never stored here. Nothing on this record counts anything.
/// </para>
/// <para>
/// <b>One record is one operation, not one phase.</b> Each individual thing the launch processes is its own
/// record, so a person watching the launch window sees every operation progress rather than a grouped phase that
/// hides several. <see cref="Target"/> is what keeps the resulting lines distinguishable: once the sequence is
/// this fine, an operation that names what it is about (the store, a share, the external system) can be told apart
/// from one that names nothing (FR-002).
/// </para>
/// <para>
/// <b>The name is shown before the work runs.</b> <see cref="Name"/> is the line the launch window shows while
/// the step is under way, which is what makes a stall attributable to a named line rather than to a step number
/// (FR-002). <see cref="Description"/> is plain language and carries no step number (FR-002).
/// </para>
/// <para>
/// <b>Every step states a maximum.</b> <see cref="MaximumWait"/> is always set, the ceiling is 30 seconds, and
/// no wait on the launch path is unbounded (FR-003, SC-002). <see cref="IsBestEffort"/> marks the two steps that
/// may not stop the launch — the picture refresh and the external-system priming — and those two are bounded
/// more tightly than the ceiling (FR-026, FR-027).
/// </para>
/// </remarks>
/// <param name="Id">The stable, unique key, and the value a retry resumes at (FR-020).</param>
/// <param name="Name">The line shown while the step runs, never after it has (FR-002).</param>
/// <param name="Description">Plain language, and no step number (FR-002).</param>
/// <param name="Category">The area the step belongs to, for grouping the feed and matching a remedy (FR-004).</param>
/// <param name="MaximumWait">The stated maximum for this step's wait. Always set (FR-003).</param>
/// <param name="IsBestEffort">Whether the step is best effort and so cannot stop the launch (FR-026).</param>
/// <param name="Target">
/// What the operation is about — the store, a share, the external system, a screen — written on every line the
/// step produces, or <c>null</c> when the operation touches nothing the person could name (reading this
/// computer's own name, for instance). It is what lets the feed say "the store answered" rather than only
/// "finished" (FR-002).
/// </param>
public sealed record LaunchStep(
    string Id,
    string Name,
    string Description,
    LaunchStepCategory Category,
    TimeSpan MaximumWait,
    bool IsBestEffort,
    string? Target = null);
