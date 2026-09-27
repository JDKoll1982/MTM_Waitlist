using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// Runs one launch step: names it before it runs, reports how it ended, and holds it to its stated maximum
/// (`contracts/launch-step-contract.md` §2, §3; FR-002, FR-003, SC-002).
/// </summary>
/// <remarks>
/// <para>
/// <b>Every line names what the step is about.</b> A step's own lines carry its
/// <see cref="LaunchStep.Target"/>, so once the catalogue holds one entry per operation — several of which touch
/// the store and several of which touch nothing — the feed says which is which rather than leaving every line
/// reading the same (FR-002). A step that declares no target writes none, which is the honest answer for an
/// operation that touches nothing a person could name.
/// </para>
/// <para>
/// <b>The step is named before it runs.</b> <see cref="RunAsync"/> appends the
/// <see cref="LaunchFeedEntryKind.StepStarted"/> line first and only then calls the step, so the announcement is
/// on the feed even if the step throws on its first statement. A stall therefore has a name attached to it
/// rather than leaving the last line the previous step wrote (FR-002).
/// </para>
/// <para>
/// <b>Every step is held to its own maximum.</b> The step is started with a token that is cancelled when it
/// passes <see cref="LaunchStep.MaximumWait"/>, and the runner stops waiting on it at that moment. A step that
/// honours the token is stopped; a step that ignores it is left to unwind on its own, with its failure observed
/// so it cannot surface as an unhandled exception later. What is recorded is the same either way: a
/// <see cref="LaunchFeedEntryKind.StepFailed"/> line naming the step and the bound it passed, so the launch never
/// sits on a step without end and never sits on one silently (FR-003, SC-002).
/// </para>
/// <para>
/// <b>A best-effort step cannot stop the launch, whatever it does.</b> When a step marked
/// <see cref="LaunchStep.IsBestEffort"/> fails, throws or overruns, its outcome is reported as
/// <see cref="LaunchStepStatus.Skipped"/> so the caller carries on — but the failure is still a line on the feed
/// with its diagnosis, because a best-effort failure is reported and never hidden (FR-026).
/// </para>
/// <para>
/// <b>Abandoning the launch is not a step failure.</b> When the caller's token is cancelled the runner lets the
/// <see cref="OperationCanceledException"/> through without recording a terminal line for the step, so a person
/// who closes the launch does not leave a step reading as if it had failed.
/// </para>
/// </remarks>
public sealed class LaunchStepRunner
{
    private readonly ILaunchActivityFeed _feed;

    public LaunchStepRunner(ILaunchActivityFeed feed)
    {
        _feed = feed ?? throw new ArgumentNullException(nameof(feed));
    }

    /// <summary>
    /// Names the step, runs it under its own stated maximum, and reports how it ended.
    /// </summary>
    /// <param name="step">The step to run.</param>
    /// <param name="context">The person (once resolved), the machine's facts and the feed.</param>
    /// <param name="cancellationToken">Cancelled when the launch is abandoned.</param>
    /// <returns>
    /// The step's outcome, with the status the caller may act on: a failed step is
    /// <see cref="LaunchStepStatus.Failed"/> unless it is best effort, in which case it is
    /// <see cref="LaunchStepStatus.Skipped"/> and the launch carries on (FR-026).
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The step declares no positive maximum. Every launch step must state one and none may be unbounded
    /// (FR-003); the runner refuses to invent a bound rather than leaving the step unbounded.
    /// </exception>
    public async Task<LaunchStepOutcome> RunAsync(
        ILaunchStep step,
        LaunchStepContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(context);

        var descriptor = step.Descriptor
            ?? throw new ArgumentException("A launch step must carry its descriptor.", nameof(step));

        if (descriptor.MaximumWait <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(step),
                descriptor.MaximumWait,
                $"'{descriptor.Id}' declares no maximum wait, and no wait on the launch path may be unbounded (FR-003).");
        }

        // The step is named before its work begins, so a stall has a name attached to it (FR-002, US1 scenario 1).
        Append(descriptor, LaunchFeedEntryKind.StepStarted, descriptor.Name, succeeded: null);

        using var bound = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var work = StartWork(step, context, bound.Token);

        var finished = await Task
            .WhenAny(work, Task.Delay(descriptor.MaximumWait, CancellationToken.None))
            .ConfigureAwait(false);

        if (!ReferenceEquals(finished, work))
        {
            // The step passed its stated maximum. Stop it if it will stop, and do not wait on it if it will not.
            bound.Cancel();
            ObserveFaults(work);
            cancellationToken.ThrowIfCancellationRequested();
            return RecordOverrun(descriptor);
        }

        return await ReportAsync(descriptor, work, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Starts the step's work, turning a step that throws before it returns its task into a failed task, so a
    /// synchronous throw is reported like any other failure rather than escaping the runner.
    /// </summary>
    private static Task<LaunchStepOutcome> StartWork(
        ILaunchStep step,
        LaunchStepContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            return step.RunAsync(context, cancellationToken)
                ?? Task.FromException<LaunchStepOutcome>(
                    new InvalidOperationException($"'{step.Descriptor.Id}' returned no work to run."));
        }
        catch (Exception exception)
        {
            return Task.FromException<LaunchStepOutcome>(exception);
        }
    }

    /// <summary>Reports how the step ended, recording exactly one terminal line for it.</summary>
    private async Task<LaunchStepOutcome> ReportAsync(
        LaunchStep descriptor,
        Task<LaunchStepOutcome> work,
        CancellationToken cancellationToken)
    {
        LaunchStepOutcome? outcome;

        try
        {
            outcome = await work.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The launch is being abandoned, not failing. No terminal line is recorded for this step.
            throw;
        }
        catch (Exception exception)
        {
            return RecordFailure(descriptor, $"{descriptor.Name} could not finish: {exception.Message}");
        }

        if (outcome is null)
        {
            return RecordFailure(descriptor, $"{descriptor.Name} reported no outcome.");
        }

        return outcome.Status switch
        {
            LaunchStepStatus.Succeeded => RecordCompletion(descriptor, outcome),
            LaunchStepStatus.Skipped => RecordSkipped(descriptor, outcome),
            _ => RecordFailure(
                descriptor,
                outcome.Diagnosis ?? $"{descriptor.Name} failed.",
                outcome.Remedies),
        };
    }

    /// <summary>Records a step that did its work.</summary>
    private LaunchStepOutcome RecordCompletion(LaunchStep descriptor, LaunchStepOutcome outcome)
    {
        Append(
            descriptor,
            LaunchFeedEntryKind.StepCompleted,
            outcome.Diagnosis ?? $"{descriptor.Name} finished.",
            succeeded: true);

        return outcome;
    }

    /// <summary>Records a step whose work was left undone, which is not a failure of the launch.</summary>
    private LaunchStepOutcome RecordSkipped(LaunchStep descriptor, LaunchStepOutcome outcome)
    {
        Append(
            descriptor,
            LaunchFeedEntryKind.StepCompleted,
            outcome.Diagnosis ?? $"{descriptor.Name} was left undone.",
            succeeded: null);

        return outcome;
    }

    /// <summary>
    /// Records a step that failed, and reports the status the caller may act on: a best-effort step is skipped
    /// rather than failed, so it cannot stop the launch (FR-026).
    /// </summary>
    private LaunchStepOutcome RecordFailure(
        LaunchStep descriptor,
        string diagnosis,
        LaunchRemedySet? remedies = null)
    {
        Append(descriptor, LaunchFeedEntryKind.StepFailed, diagnosis, succeeded: false);

        return new LaunchStepOutcome(
            descriptor.IsBestEffort ? LaunchStepStatus.Skipped : LaunchStepStatus.Failed,
            diagnosis,
            remedies ?? LaunchRemedySet.RetryOnly);
    }

    /// <summary>Records a step that passed its stated maximum, naming the bound it passed.</summary>
    private LaunchStepOutcome RecordOverrun(LaunchStep descriptor)
    {
        var diagnosis =
            $"{descriptor.Name} did not finish within the {DescribeMaximum(descriptor.MaximumWait)} it is allowed.";

        return RecordFailure(descriptor, diagnosis);
    }

    /// <summary>Adds one line to the feed, stamped as it is written and naming what the step is about.</summary>
    private void Append(LaunchStep descriptor, LaunchFeedEntryKind kind, string text, bool? succeeded)
        => _feed.Append(new LaunchFeedEntry(
            DateTimeOffset.UtcNow,
            descriptor.Id,
            kind,
            text,
            descriptor.Target,
            succeeded));

    /// <summary>Observes an abandoned step's failure, so it cannot surface later as an unhandled exception.</summary>
    private static void ObserveFaults(Task abandoned)
    {
        _ = abandoned.ContinueWith(
            static task => _ = task.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    /// <summary>Describes a bound the way a person would say it, for the diagnosis.</summary>
    private static string DescribeMaximum(TimeSpan maximum)
        => maximum.TotalSeconds >= 1
            ? $"{maximum.TotalSeconds:0.#} second(s)"
            : $"{maximum.TotalMilliseconds:0} millisecond(s)";
}
