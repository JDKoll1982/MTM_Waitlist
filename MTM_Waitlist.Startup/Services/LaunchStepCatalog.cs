using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// The launch sequence as data: every individual operation's name, description, category, stated maximum,
/// best-effort marker and target (`contracts/launch-step-contract.md` §1; plan D21; FR-002, FR-003, SC-002).
/// </summary>
/// <remarks>
/// <para>
/// <b>Steps are data, not a hard-coded five.</b> This catalogue is the list the pipeline walks, and the displayed
/// count is derived from it — <see cref="TotalCount"/> is the length of <see cref="Steps"/> and
/// <see cref="CompletedCount"/> counts the steps the feed says have ended. Nothing anywhere stores a total, so
/// adding or removing a step cannot leave a stale "of 5".
/// </para>
/// <para>
/// <b>One entry per operation, not one per phase.</b> A phase that bundled several distinct operations has been
/// split until each thing the launch does holds its own entry, so every one of them is individually reportable on
/// the splash rather than standing behind a group's single line. More, smaller entries is the deliberate
/// direction: a grouped step hides which part of it stalled, and the point of the feed is that a stall has a name.
/// </para>
/// <para>
/// <b>The catalogue is bounded, and it proves it.</b> The construction validates the shipped list, so a future
/// edit that adds a step without a maximum, or a best-effort step at the ceiling, fails immediately rather than
/// shipping an unbounded wait. <see cref="Ceiling"/> is the 30 seconds FR-003 and SC-002 call the ceiling for any
/// single wait on the launch path; the two best-effort steps — the picture refresh and the external-system
/// priming — are bounded more tightly because neither may hold the launch (FR-026, FR-027).
/// </para>
/// <para>
/// <b>The sequence:</b> what this computer keeps locally; whether the store answers; this computer's own name and
/// hardware address, its record and its configuration; the save that configures it when it needs one; the
/// remembered sign-in and the key that unlocks it; who is signing in and the roles they hold; the credential
/// check, the session, the machine gate and a temporary credential's replacement when they apply; the picture
/// copies and the external-system verdict, both best effort; and finally the hand-over to the first screen.
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
    /// <remarks>
    /// <b>One entry per thing the launch does.</b> Where this list used to hold a phase — "reading this computer's
    /// configuration", "working out who is signed in" — that bundled several operations, each operation now holds
    /// its own entry, so a person watching the launch window sees every one of them progress rather than a single
    /// line standing for a group. The order is the launch order, and the straight runs are: what this computer
    /// keeps; whether the store answers; this computer's identity and its record; this computer's configuration
    /// and the save that sets it; the remembered sign-in; who is signing in and the roles they hold; the
    /// credential, the session, the machine gate and a temporary credential's replacement; the picture copies and
    /// the external-system verdict, both best effort; and the hand-over to the first screen.
    /// </remarks>
    private static readonly LaunchStep[] s_steps =
    [
        new(
            "read-local-settings",
            "Reading this computer's saved settings",
            "Reads what this computer keeps so it can reach the store.",
            LaunchStepCategory.Configuration,
            TimeSpan.FromSeconds(10),
            false),
        new(
            "store-reachability",
            "Contacting the store",
            "Confirms the store that holds the records answers.",
            LaunchStepCategory.Configuration,
            TimeSpan.FromSeconds(15),
            false,
            "the store"),
        new(
            "read-hardware-identity",
            "Reading this computer's identity",
            "Reads this computer's name and the hardware address it presents.",
            LaunchStepCategory.Machine,
            TimeSpan.FromSeconds(5),
            false),
        new(
            "read-computer-record",
            "Reading this computer's record",
            "Finds this computer in the store by its name and hardware address.",
            LaunchStepCategory.Machine,
            TimeSpan.FromSeconds(10),
            false,
            "the store"),
        new(
            "read-machine-configuration",
            "Reading this computer's configuration",
            "Reads what this computer needs and whether it is configured.",
            LaunchStepCategory.Machine,
            TimeSpan.FromSeconds(10),
            false,
            "the store"),
        new(
            "save-machine-configuration",
            "Saving this computer's configuration",
            "Names this computer and points it at the shared picture sources.",
            LaunchStepCategory.Machine,
            Ceiling,
            false,
            "the store"),
        new(
            "read-remembered-sign-in",
            "Reading the remembered sign-in",
            "Reads, and unlocks, the sign-in this computer was asked to remember.",
            LaunchStepCategory.Session,
            TimeSpan.FromSeconds(10),
            false,
            "the shared key file"),
        new(
            "resolve-person",
            "Working out who is signed in",
            "Resolves the person from the store by their sign-in name.",
            LaunchStepCategory.Session,
            TimeSpan.FromSeconds(15),
            false,
            "the store"),
        new(
            "resolve-roles",
            "Reading the roles they hold",
            "Reads every role the person holds, so their actions carry the right authority.",
            LaunchStepCategory.Session,
            TimeSpan.FromSeconds(15),
            false,
            "the store"),
        new(
            "check-credential",
            "Checking the sign-in",
            "Checks the credential the person presented against the store.",
            LaunchStepCategory.Session,
            Ceiling,
            false,
            "the store"),
        new(
            "judge-session",
            "Checking whether their session still stands",
            "Judges the session against the store's clock rather than this computer's.",
            LaunchStepCategory.Session,
            TimeSpan.FromSeconds(15),
            false,
            "the store"),
        new(
            "check-computer-against-store",
            "Checking this computer against the store",
            "Confirms the computer the person signed in on is one the store holds.",
            LaunchStepCategory.Session,
            TimeSpan.FromSeconds(15),
            false,
            "the store"),
        new(
            "check-temporary-credential",
            "Checking whether a new password is needed",
            "Checks whether the account is still on a temporary credential.",
            LaunchStepCategory.Session,
            TimeSpan.FromSeconds(15),
            false,
            "the store"),
        new(
            "set-new-password",
            "Setting a new password",
            "Replaces a temporary credential before the person carries on.",
            LaunchStepCategory.Session,
            Ceiling,
            false,
            "the store"),
        new(
            "picture-cache",
            "Refreshing the picture copies",
            "Copies the shared pictures this computer keeps. Recorded, never blocking.",
            LaunchStepCategory.Pictures,
            TimeSpan.FromSeconds(10),
            true,
            "the picture share"),
        new(
            "visual-priming",
            "Asking whether the external system answers",
            "Settles whether the read-only external system can be reached. Recorded, never blocking.",
            LaunchStepCategory.ExternalSystem,
            TimeSpan.FromSeconds(5),
            true,
            "the external system"),
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
    /// declares a blank target, or carries no name or description — each of which would leave the launch
    /// unbounded or unreadable.
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

            if (step.Target is not null && string.IsNullOrWhiteSpace(step.Target))
            {
                throw new InvalidOperationException(
                    $"'{step.Id}' declares a blank target, so its lines would name nothing where the whole point is that a line names what it is about (FR-002).");
            }

            if (step.IsBestEffort && step.MaximumWait >= Ceiling)
            {
                throw new InvalidOperationException(
                    $"'{step.Id}' is best effort, so it must be bounded more tightly than the {Ceiling.TotalSeconds:0}-second ceiling: it may not hold the launch at all (FR-026, FR-027).");
            }
        }
    }
}
