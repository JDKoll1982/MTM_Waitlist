using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// One parallel group: a set of catalogue entries that start together and end when all of them have ended
/// (`contracts/launch-concurrency-contract.md` §1; FR-026, FR-027, FR-031).
/// </summary>
/// <param name="Id">A stable name for the group, so a reader can tell which overlap a line belongs to.</param>
/// <param name="Members">The catalogue ids that run at the same time.</param>
/// <remarks>
/// <b>A group changes when work starts, not how long it may take.</b> Every member still runs under its own
/// stated maximum through the existing runner, so one member passing its bound is reported and not waited on
/// whatever the others are doing — the group cannot become an unbounded wait (FR-036).
/// </remarks>
public sealed record LaunchStepGroup(string Id, IReadOnlyList<string> Members);

/// <summary>
/// The launch's parallel groups, declared as data (`contracts/launch-concurrency-contract.md` §1; FR-026, FR-027).
/// </summary>
/// <remarks>
/// <para>
/// <b>Two groups, and why each is safe.</b> The local reads and this computer's own identity touch nothing the
/// store holds, so neither depends on the first contact with the store and all three may run together. The three
/// session checks each read the store independently — the session, this computer's record and the temporary
/// credential are separate questions about the same person — so they may run together too (SC-006).
/// </para>
/// <para>
/// <b>Everything else keeps its order.</b> A member of a group starts at the group's first catalogue position and
/// nothing else moves: <c>visual-priming</c> stays last before the hand-over, because its verdict must be settled
/// before the shell opens (FR-029).
/// </para>
/// </remarks>
public static class LaunchStepGroups
{
    /// <summary>The groups this launch runs concurrently, in catalogue order.</summary>
    public static IReadOnlyList<LaunchStepGroup> Shipped { get; } =
    [
        new(
            "first-contact",
            ["read-local-settings", "read-hardware-identity", "store-reachability"]),
        new(
            "session-checks",
            ["judge-session", "check-computer-against-store", "check-temporary-credential"]),
    ];

    /// <summary>
    /// The group whose <b>first</b> catalogue position is <paramref name="index"/>, or <c>null</c> when no group
    /// starts here.
    /// </summary>
    /// <param name="steps">The catalogue's ordered sequence.</param>
    /// <param name="index">The position the walk has reached.</param>
    /// <returns>The group to run, or <c>null</c> for an ordinary single step.</returns>
    /// <remarks>
    /// Only the group's first position answers here. A walk that resumed at a member inside a group — a retry of
    /// one failed check, say — runs that member on its own and does not re-run its siblings, so "a retry resumes"
    /// stays true (FR-020).
    /// </remarks>
    internal static LaunchStepGroup? GroupStartingAt(IReadOnlyList<LaunchStep> steps, int index)
    {
        ArgumentNullException.ThrowIfNull(steps);

        if (index < 0 || index >= steps.Count)
        {
            return null;
        }

        var stepId = steps[index].Id;

        foreach (var group in Shipped)
        {
            if (FirstMemberIndex(steps, group) == index
                && group.Members.Contains(stepId, StringComparer.Ordinal))
            {
                return group;
            }
        }

        return null;
    }

    /// <summary>The catalogue position of a group's earliest member, or <c>-1</c> when the catalogue holds none.</summary>
    internal static int FirstMemberIndex(IReadOnlyList<LaunchStep> steps, LaunchStepGroup group)
    {
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentNullException.ThrowIfNull(group);

        var first = -1;

        for (var index = 0; index < steps.Count; index++)
        {
            if (group.Members.Any(member => string.Equals(member, steps[index].Id, StringComparison.Ordinal)))
            {
                first = index;
                break;
            }
        }

        return first;
    }
}
