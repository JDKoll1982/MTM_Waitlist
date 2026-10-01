using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// The launch: it walks the catalogued steps in order, holds each to its stated maximum through the runner, and
/// ends at exactly one of the five terminal outcomes (`contracts/launch-step-contract.md` §5; FR-001, FR-006,
/// FR-020).
/// </summary>
/// <remarks>
/// <para>
/// <b>It routes; the steps report.</b> A step answers what it did and never where the launch goes next, so the
/// two decisions the launch makes — "this machine needs setting up" and "a person is needed now" — are made
/// here, once, rather than being spread across the steps that happen to notice. That is also what keeps the
/// unconfigured refusal out of reach of a screen (FR-006, S10.1).
/// </para>
/// <para>
/// <b>A step that is not built yet is passed over, not faked.</b> The catalogue is the launch's whole sequence,
/// and the startup phases in this feature supply an implementation for each entry. A catalogue entry with no
/// implementation is not run, because inventing a step that reported success would put a line on the feed for
/// work that never happened. The count the surface shows is the catalogue's, not the number of implementations,
/// so the gap is visible rather than hidden.
/// </para>
/// <para>
/// <b>Retry resumes; it does not restart.</b> <see cref="RetryFromAsync"/> runs the named step and every step
/// after it and nothing before it, so a store-only retry never repeats reading this computer's own settings and
/// a retry after machine setup resumes at the save (FR-020).
/// </para>
/// <para>
/// <b>A new PIN is where this process stops and the application starts again.</b> The step that writes the new
/// credential leaves this process holding an identity that was accepted on the credential it has just replaced,
/// so the launch does not carry on over it: a replacement instance is started and this launch ends, which puts
/// the person back at the sign-in form with the PIN they chose now the one in use (FR-013, FR-008). This is the
/// same rule the sign-out and the stopped launch's repeat follow, and for the same reason — the work that
/// follows belongs to a process that was started on what the store now holds.
/// </para>
/// <para>
/// <b>One machine, one launch.</b> A second entry while a sequence is running starts nothing and answers the run
/// already under way, so a re-entrant navigation cannot put two pipelines against one machine.
/// </para>
/// <para>
/// <b>Every wait is bounded, including the launch's own reads.</b> The routing read described below runs under
/// the stated maximum of the step it follows rather than unbounded, because a wait the person cannot see is
/// still a wait (FR-003, SC-002).
/// </para>
/// </remarks>
internal sealed class LaunchPipeline : ILaunchPipeline
{
    /// <summary>The step a person must be resolved at, which is where the launch stops for a sign-in.</summary>
    private const string ResolvePersonStepId = "resolve-person";

    /// <summary>The step that hands the launch over, which the launch itself performs.</summary>
    private const string ShellStepId = "shell";

    /// <summary>The step that replaces a temporary credential, after which this process starts again (FR-013).</summary>
    private const string SetNewPasswordStepId = "set-new-password";

    /// <summary>The area this launch's own lines are recorded under.</summary>
    private const string LogModule = "Launch";

    /// <summary>The step whose verdict decides whether this machine is configured (FR-006).</summary>
    private const string MachineReadinessStepId = "read-machine-configuration";

    private readonly LaunchStepCatalog _catalog;
    private readonly LaunchStepRunner _runner;
    private readonly ILaunchActivityFeed _feed;
    private readonly IMachineFacts _machine;
    private readonly PersonIdentityService _person;
    private readonly IMachineConfigurationService _configuration;
    private readonly IPendingSignIn _pendingSignIn;
    private readonly IProcessRestarter _restarter;
    private readonly Dictionary<string, ILaunchStep> _steps;
    private readonly object _gate = new();

    private Task<LaunchOutcome>? _inFlight;

    /// <summary>Creates the launch over the sequence, the steps that implement it, and the state it routes on.</summary>
    /// <param name="catalog">The sequence, which is the order the launch walks.</param>
    /// <param name="runner">Runs one step under its stated maximum and records how it ended.</param>
    /// <param name="feed">Where the launch's lines go.</param>
    /// <param name="steps">One implementation per catalogued entry, supplied by the phases that build them.</param>
    /// <param name="machine">This computer's facts, for handing to each step (FR-022).</param>
    /// <param name="person">The one writer of who is signed in (FR-022).</param>
    /// <param name="configuration">This machine's configuration, read for the routing decision (FR-006).</param>
    /// <param name="pendingSignIn">What the launch holds to present, read for the routing decision (FR-014).</param>
    /// <param name="restarter">
    /// The restart seam, used once: after a new credential has been written, so a launch that replaced a
    /// temporary PIN ends here and the application comes back on the new one (FR-013).
    /// </param>
    public LaunchPipeline(
        LaunchStepCatalog catalog,
        LaunchStepRunner runner,
        ILaunchActivityFeed feed,
        IEnumerable<ILaunchStep> steps,
        IMachineFacts machine,
        PersonIdentityService person,
        IMachineConfigurationService configuration,
        IPendingSignIn pendingSignIn,
        IProcessRestarter restarter)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(feed);
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentNullException.ThrowIfNull(person);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(pendingSignIn);
        ArgumentNullException.ThrowIfNull(restarter);

        _catalog = catalog;
        _runner = runner;
        _feed = feed;
        _machine = machine;
        _person = person;
        _configuration = configuration;
        _pendingSignIn = pendingSignIn;
        _restarter = restarter;

        _steps = new Dictionary<string, ILaunchStep>(StringComparer.Ordinal);

        foreach (var step in steps)
        {
            if (!_steps.TryAdd(step.Descriptor.Id, step))
            {
                // Two implementations for one entry would leave whichever the container happened to resolve
                // doing the work, which is not something a launch may be wrong about.
                throw new InvalidOperationException(
                    $"The launch holds two implementations of its '{step.Descriptor.Id}' step, so which one runs cannot be relied on.");
            }
        }

        // The hand-over is the launch's own last act rather than a service: it has no work to do beyond letting
        // the host know the first screen is ready, and a phase that registers its own step for the entry wins.
        _steps.TryAdd(ShellStepId, new ShellHandoverStep(catalog));
    }

    /// <inheritdoc />
    public event EventHandler<LaunchOutcome>? ShellReady;

    /// <inheritdoc />
    public event EventHandler<string>? ProcessEnding;

    /// <inheritdoc />
    public Task<LaunchOutcome> RunAsync(CancellationToken cancellationToken) => Enter(0, cancellationToken);

    /// <inheritdoc />
    public Task<LaunchOutcome> RetryFromAsync(string failedStepId, CancellationToken cancellationToken)
    {
        var index = IndexOf(failedStepId);

        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(failedStepId),
                failedStepId,
                "The launch declares no step with that id, so there is nothing to resume at (FR-020).");
        }

        return Enter(index, cancellationToken);
    }

    /// <inheritdoc />
    public LaunchOutcome End(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        // The reason is stated before the process goes, and it is stated through the one event the host listens
        // on, so an abort reaches the person as a reason rather than as a crash (FR-008).
        ProcessEnding?.Invoke(this, reason);

        return LaunchOutcome.Ended;
    }

    /// <summary>
    /// Starts a sequence from <paramref name="startIndex"/>, or joins the one already running.
    /// </summary>
    /// <remarks>
    /// The join is deliberate: a re-entrant entry starts nothing, so two sequences cannot be run against one
    /// machine, and the caller still receives the answer to the run that is under way.
    /// </remarks>
    private Task<LaunchOutcome> Enter(int startIndex, CancellationToken cancellationToken)
    {
        Task<LaunchOutcome> run;

        lock (_gate)
        {
            if (_inFlight is not null)
            {
                return _inFlight;
            }

            run = RunSequenceAsync(startIndex, cancellationToken);
            _inFlight = run;
        }

        // Cleared as soon as it finishes, so the next launch — after a retry, or the next time the surface is
        // entered — starts a sequence of its own rather than joining one that has already ended.
        _ = run.ContinueWith(
            _ =>
            {
                lock (_gate)
                {
                    _inFlight = null;
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        return run;
    }

    /// <summary>Walks the catalogue from <paramref name="startIndex"/> and answers where the launch ended.</summary>
    private async Task<LaunchOutcome> RunSequenceAsync(int startIndex, CancellationToken cancellationToken)
    {
        var descriptors = _catalog.Steps;

        for (var index = startIndex; index < descriptors.Count; index++)
        {
            var descriptor = descriptors[index];

            // A person is needed from the point identity is resolved. Read before the step rather than after it,
            // so the launch stops for the sign-in form rather than trying to resolve somebody who has not
            // arrived. A sign-in held already — the remembered path — passes straight through (FR-014).
            if (string.Equals(descriptor.Id, ResolvePersonStepId, StringComparison.Ordinal)
                && !_person.IsSignedIn
                && !_pendingSignIn.HasSignInToPresent)
            {
                return HandOver(LaunchOutcome.SignIn);
            }

            // The shell is only ever opened for a signed-in person (S4): there is no continue-anyway path past
            // this, and a launch that reached here without a person is handed to the sign-in surface instead.
            if (string.Equals(descriptor.Id, ShellStepId, StringComparison.Ordinal) && !_person.IsSignedIn)
            {
                return HandOver(LaunchOutcome.SignIn);
            }

            if (!_steps.TryGetValue(descriptor.Id, out var step))
            {
                // Not built yet. The entry stays in the count so the surface shows the sequence honestly.
                continue;
            }

            var outcome = await _runner.RunAsync(step, BuildContext(), cancellationToken).ConfigureAwait(false);

            if (outcome.Status is LaunchStepStatus.Failed)
            {
                return HandOver(LaunchOutcome.Blocked);
            }

            // A new PIN replaces the credential this process signed in with, and the identity this process holds
            // was accepted on the credential that has just been replaced. So this launch does not carry on over
            // it: the application is started again and the person signs in with the PIN they chose (FR-013). The
            // reason goes through the ending route, which is what states it before the process goes (FR-008).
            if (string.Equals(descriptor.Id, SetNewPasswordStepId, StringComparison.Ordinal)
                && outcome.Status is LaunchStepStatus.Succeeded
                && StartAgainAfterTheNewPin())
            {
                return End("The new PIN has been set, so the application is starting again on it.");
            }

            if (string.Equals(descriptor.Id, MachineReadinessStepId, StringComparison.Ordinal))
            {
                var state = await ReadConfigurationStateAsync(descriptor, cancellationToken).ConfigureAwait(false);

                if (state is null)
                {
                    // The verdict could not be read within the step's own bound, so the machine cannot be shown
                    // to be configured and the launch may not carry on as though it were (FR-006, FR-009).
                    return HandOver(LaunchOutcome.Blocked);
                }

                if (!state.IsConfigured)
                {
                    // The pipeline, not a screen, is what refuses to continue here (FR-006, S10.1).
                    return HandOver(LaunchOutcome.MachineSetup);
                }
            }

            if (string.Equals(descriptor.Id, ShellStepId, StringComparison.Ordinal))
            {
                return HandOver(LaunchOutcome.MainScreens);
            }
        }

        // The sequence ended without reaching its hand-over, which means the entries after the last one built
        // are still to come. A signed-in person is handed to the main screens; anybody else is asked to sign in.
        return HandOver(_person.IsSignedIn ? LaunchOutcome.MainScreens : LaunchOutcome.SignIn);
    }

    /// <summary>
    /// Hands the launch's terminal outcome to whoever is listening, and answers it, so every surface a launch can
    /// end at reaches the host by the one event it subscribes to (`contracts/launch-step-contract.md` sections 5
    /// and 6; FR-001, FR-006).
    /// </summary>
    /// <param name="outcome">Where the launch ended, which is one of the five terminal outcomes.</param>
    /// <remarks>
    /// The hand-off carries the outcome itself rather than only telling the host that something is ready, because
    /// the host's whole job is to show the surface that outcome names. An ending is the one outcome that does not
    /// come through here: it is stated through <see cref="ProcessEnding"/> before the process goes (FR-008).
    /// </remarks>
    private LaunchOutcome HandOver(LaunchOutcome outcome)
    {
        ShellReady?.Invoke(this, outcome);

        return outcome;
    }

    /// <summary>
    /// Starts a fresh copy of the application after a new PIN has been written, and answers whether one is on
    /// its way.
    /// </summary>
    /// <returns>Whether a replacement instance was started, which is what lets this process go.</returns>
    /// <remarks>
    /// <b>A restart that did not happen leaves the person where they are.</b> The rule the sign-out and the
    /// stopped launch's repeat both follow: nothing is taken away until its replacement exists. A relaunch that
    /// could not be started is recorded and <c>false</c> is answered, so the launch carries on to the main
    /// screens and the person is left with a working application on the PIN they just chose rather than with no
    /// application at all. The credential is in the store either way, so what is lost is the convenience of the
    /// restart, not the new PIN.
    /// </remarks>
    private bool StartAgainAfterTheNewPin()
    {
        bool started;

        try
        {
            started = _restarter.Restart();
        }
        catch (Exception exception)
        {
            AppLog.Error(
                LogModule,
                exception,
                "The application could not be started again after the new PIN was set, so this launch is carrying on.");

            return false;
        }

        if (!started)
        {
            AppLog.Error(
                LogModule,
                new InvalidOperationException("The application could not be started again."),
                "The application could not be started again after the new PIN was set, so this launch is carrying on.");

            return false;
        }

        AppLog.Info(
            LogModule,
            "The new PIN has been set, so the application is starting again on it and this launch is ending.");

        return true;
    }

    /// <summary>
    /// Reads this machine's configuration for the routing decision, under the same stated maximum the step it
    /// follows declares, so the launch's own reads are bounded like every other wait (FR-003, SC-002).
    /// </summary>
    /// <returns>The state, or <c>null</c> when it could not be read in time.</returns>
    private async Task<MachineConfigurationState?> ReadConfigurationStateAsync(
        LaunchStep descriptor,
        CancellationToken cancellationToken)
    {
        using var bound = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        bound.CancelAfter(descriptor.MaximumWait);

        try
        {
            return await _configuration.GetStateAsync(bound.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Abandoning the launch is not a failure of the read.
            throw;
        }
        catch (Exception exception)
        {
            AppendRouteFailure(descriptor, exception);

            return null;
        }
    }

    /// <summary>Records a routing read that did not answer, so the stop is attributable to a named line.</summary>
    private void AppendRouteFailure(LaunchStep descriptor, Exception exception)
        => _feed.Append(new LaunchFeedEntry(
            DateTimeOffset.UtcNow,
            descriptor.Id,
            LaunchFeedEntryKind.SubOperation,
            $"This computer's configuration could not be confirmed within its {descriptor.MaximumWait.TotalSeconds:0.#} second(s): {exception.Message}",
            descriptor.Target,
            false));

    /// <summary>What each step is handed: the person once there is one, this computer's facts, and the feed.</summary>
    private LaunchStepContext BuildContext()
        => new(_person.IsSignedIn ? _person : null, _machine, _feed);

    /// <summary>The catalogue index of a step id, or <c>-1</c> when the sequence does not declare it.</summary>
    private int IndexOf(string stepId)
    {
        if (string.IsNullOrWhiteSpace(stepId))
        {
            return -1;
        }

        var descriptors = _catalog.Steps;

        for (var index = 0; index < descriptors.Count; index++)
        {
            if (string.Equals(descriptors[index].Id, stepId, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }

    /// <summary>
    /// The launch's own hand-over to the first screen, answering for the catalogue's <c>shell</c> entry.
    /// </summary>
    /// <remarks>
    /// It does no work of its own: the host shows the first screen when <see cref="ShellReady"/> fires, so all
    /// this step has to do is answer that the hand-over happened, which is what puts the line on the feed.
    /// </remarks>
    private sealed class ShellHandoverStep : ILaunchStep
    {
        internal ShellHandoverStep(LaunchStepCatalog catalog)
            => Descriptor = LaunchStepSupport.DescriptorFor(catalog, ShellStepId);

        /// <inheritdoc />
        public LaunchStep Descriptor { get; }

        /// <inheritdoc />
        public Task<LaunchStepOutcome> RunAsync(LaunchStepContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(new LaunchStepOutcome(
                LaunchStepStatus.Succeeded,
                "The first screen is ready.",
                LaunchRemedySet.None));
        }
    }
}
