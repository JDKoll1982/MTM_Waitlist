using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// The launch sequence as data: every step's name, description, category, stated maximum and best-effort marker
/// (`contracts/launch-step-contract.md` §1; plan D21; FR-002, FR-003, SC-002).
/// </summary>
/// <remarks>
/// <para>
/// <b>Steps are data, not a hard-coded five.</b> This catalogue is the list the pipeline walks, and the displayed
/// count is derived from it — <see cref="TotalCount"/> is the length of <see cref="Steps"/> and
/// <see cref="CompletedCount"/> counts the steps the feed says have ended. Nothing anywhere stores a total, so
/// adding or removing a step cannot leave a stale "of 5".
/// </para>
/// <para>
/// <b>The catalogue is bounded, and it proves it.</b> The construction validates the shipped list, so a future
/// edit that adds a step without a maximum, or a best-effort step at the ceiling, fails immediately rather than
/// shipping an unbounded wait. <see cref="Ceiling"/> is the 30 seconds FR-003 and SC-002 call the ceiling for any
/// single wait on the launch path; the two best-effort steps — the picture refresh and the external-system
/// priming — are bounded more tightly because neither may hold the launch (FR-026, FR-027).
/// </para>
/// <para>
/// <b>The sequence:</b> the machine's configuration and the store that holds it; this computer's readiness and
/// its configuration when it needs one; who is signed in and whether their session still stands; the sign-in, the
/// machine check and a temporary credential's replacement when they apply; the picture copies and the
/// external-system verdict, both best effort; and finally the hand-over to the first screen.
/// </para>
/// </remarks>
public sealed class LaunchStepCatalog
{
    /// <summary>
    /// The ceiling for any single wait on the launch path: 30 seconds (FR-003, SC-002).
    /// </summary>
    public static TimeSpan Ceiling { get; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The launch sequence, in the order it runs. Declared after <see cref="Ceiling"/> so a ceiling-sized bound
    /// reads as the ceiling rather than as a second 30.
    /// </summary>
    private static readonly LaunchStep[] s_steps =
    [
        new(
            "configuration",
            "Reading this computer's configuration",
            "Reads what this machine needs before anyone signs in.",
            LaunchStepCategory.Configuration,
            TimeSpan.FromSeconds(10),
            false),
        new(
            "store-reachability",
            "Contacting the store",
            "Confirms the store that holds the records answers.",
            LaunchStepCategory.Configuration,
            TimeSpan.FromSeconds(15),
            false),
        new(
            "machine-readiness",
            "Checking this computer",
            "Reads this computer's identity and confirms it is one the store knows.",
            LaunchStepCategory.Machine,
            TimeSpan.FromSeconds(10),
            false),
        new(
            "machine-setup",
            "Configuring this computer",
            "Names this computer and points it at the shared picture sources.",
            LaunchStepCategory.Machine,
            Ceiling,
            false),
        new(
            "session",
            "Working out who is signed in",
            "Resolves the person and whether their session still stands.",
            LaunchStepCategory.Session,
            TimeSpan.FromSeconds(15),
            false),
        new(
            "sign-in",
            "Signing in",
            "Takes the person's credentials and checks them against the store.",
            LaunchStepCategory.Session,
            Ceiling,
            false),
        new(
            "machine-gate",
            "Checking this computer against the store",
            "Confirms the computer the person signed in on is one the store holds.",
            LaunchStepCategory.Session,
            TimeSpan.FromSeconds(15),
            false),
        new(
            "password-change",
            "Setting a new password",
            "Replaces a temporary credential before the person carries on.",
            LaunchStepCategory.Session,
            Ceiling,
            false),
        new(
            "picture-cache",
            "Refreshing the picture copies",
            "Copies the shared pictures this computer keeps. Recorded, never blocking.",
            LaunchStepCategory.Pictures,
            TimeSpan.FromSeconds(10),
            true),
        new(
            "visual-priming",
            "Asking whether the external system answers",
            "Settles whether the read-only external system can be reached. Recorded, never blocking.",
            LaunchStepCategory.ExternalSystem,
            TimeSpan.FromSeconds(5),
            true),
        new(
            "shell",
            "Opening the first screen",
            "Hands the launch over to the first screen.",
            LaunchStepCategory.Shell,
            TimeSpan.FromSeconds(10),
            false),
    ];

    /// <summary>
    /// Builds the catalogue and proves the shipped sequence is complete and bounded before a launch can use it.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// A step declares no maximum, exceeds the ceiling, is best effort at the ceiling, repeats another step's id,
    /// or carries no name or description — each of which would leave the launch unbounded or unreadable.
    /// </exception>
    public LaunchStepCatalog()
    {
        Validate(s_steps);
        Steps = s_steps;
    }

    /// <summary>The sequence, in the order it runs.</summary>
    public IReadOnlyList<LaunchStep> Steps { get; }

    /// <summary>
    /// The number of steps the launch will run, derived from the list rather than stored, which is what keeps the
    /// displayed count from going stale when the list changes.
    /// </summary>
    public int TotalCount => Steps.Count;

    /// <summary>
    /// The step with the given id, or <c>null</c> when the sequence holds none.
    /// </summary>
    /// <param name="id">The step id a retry resumes at (FR-020).</param>
    public LaunchStep? Find(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        foreach (var step in Steps)
        {
            if (string.Equals(step.Id, id, StringComparison.Ordinal))
            {
                return step;
            }
        }

        return null;
    }

    /// <summary>
    /// How many of this catalogue's steps the feed says have ended, counted from the feed rather than tracked, so
    /// the progress shown cannot disagree with the lines the person is reading.
    /// </summary>
    /// <param name="feed">The activity feed for the launch in progress.</param>
    /// <remarks>
    /// A step that failed and was then repeated counts once: the count is of steps that have reached an end, not
    /// of lines. A terminal line for a step this catalogue does not hold is ignored, so a stray id cannot make
    /// the progress exceed the total.
    /// </remarks>
    public int CompletedCount(ILaunchActivityFeed feed)
    {
        ArgumentNullException.ThrowIfNull(feed);

        var ended = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in feed.Entries)
        {
            if (entry.Kind is LaunchFeedEntryKind.StepCompleted or LaunchFeedEntryKind.StepFailed
                && Find(entry.StepId) is not null)
            {
                ended.Add(entry.StepId);
            }
        }

        return ended.Count;
    }

    /// <summary>
    /// Refuses a sequence that could leave the launch unbounded or unreadable (FR-002, FR-003, FR-026, FR-027,
    /// SC-002).
    /// </summary>
    /// <param name="steps">The sequence to check.</param>
    /// <exception cref="InvalidOperationException">The sequence breaks one of the rules above.</exception>
    internal static void Validate(IReadOnlyList<LaunchStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);

        if (steps.Count == 0)
        {
            throw new InvalidOperationException(
                "A launch with no steps cannot be watched or diagnosed, so the sequence must hold at least one.");
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var step in steps)
        {
            if (string.IsNullOrWhiteSpace(step.Id) || !seen.Add(step.Id))
            {
                throw new InvalidOperationException(
                    $"'{step.Id}' is not a stable, unique step id, and an id is what a retry resumes at (FR-020).");
            }

            if (string.IsNullOrWhiteSpace(step.Name) || string.IsNullOrWhiteSpace(step.Description))
            {
                throw new InvalidOperationException(
                    $"'{step.Id}' must carry a name and a description: both are shown before its work runs (FR-002).");
            }

            if (step.MaximumWait <= TimeSpan.Zero)
            {
                throw new InvalidOperationException(
                    $"'{step.Id}' declares no maximum wait. Every wait on the launch path must have one, and none may be unbounded (FR-003).");
            }

            if (step.MaximumWait > Ceiling)
            {
                throw new InvalidOperationException(
                    $"'{step.Id}' waits {step.MaximumWait.TotalSeconds:0.#} seconds, past the {Ceiling.TotalSeconds:0}-second ceiling for any single wait (FR-003, SC-002).");
            }

            if (step.IsBestEffort && step.MaximumWait >= Ceiling)
            {
                throw new InvalidOperationException(
                    $"'{step.Id}' is best effort, so it must be bounded more tightly than the {Ceiling.TotalSeconds:0}-second ceiling: it may not hold the launch at all (FR-026, FR-027).");
            }
        }
    }
}
