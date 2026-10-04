using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// The launch's machine-readiness read: it reads what this computer needs and whether it is configured, and it
/// answers for the catalogue's <c>read-machine-configuration</c> entry
/// (`contracts/launch-step-contract.md` §1.1, `contracts/machine-configuration-contract.md` §1; FR-002, FR-009).
/// </summary>
/// <remarks>
/// <para>
/// <b>It reports the verdict; it does not act on it.</b> A machine that is not configured is a finding, not a
/// failure of the read, so the step says what it found and the pipeline — not this step and not a screen —
/// refuses to continue while the machine is unconfigured (FR-006, FR-001).
/// </para>
/// <para>
/// <b>A store that could not be read is told apart from a machine that needs setting up.</b> The configuration
/// service reports an unreadable store as its own unconfigured reason rather than raising it, and this step
/// reports that one as a stop with a retry: a read that failed may succeed on the next check, and offering setup
/// for it would invite an answer that was never the problem (FR-009, FR-017).
/// </para>
/// </remarks>
public sealed class MachineReadinessStep : ILaunchStep
{
    /// <summary>The catalogue entry this step answers for.</summary>
    private const string StepId = "read-machine-configuration";

    private readonly LaunchStep _descriptor;
    private readonly IMachineConfigurationService _configuration;

    /// <summary>Creates the step over the service that owns this machine's configuration rows.</summary>
    /// <param name="catalog">The sequence, which is where the step takes its descriptor from rather than restating it.</param>
    /// <param name="configuration">This machine's configuration, read only.</param>
    public MachineReadinessStep(LaunchStepCatalog catalog, IMachineConfigurationService configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _descriptor = LaunchStepSupport.DescriptorFor(catalog, StepId);
        _configuration = configuration;
    }

    /// <inheritdoc />
    public LaunchStep Descriptor => _descriptor;

    /// <inheritdoc />
    public Task<LaunchStepOutcome> RunAsync(LaunchStepContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        return LaunchStepSupport.RunGuardedAsync(
            "This computer's configuration could not be read",
            token => ReadConfigurationAsync(token),
            cancellationToken);
    }

    /// <summary>Reads the configuration and reports whether this computer is configured.</summary>
    private async Task<LaunchStepOutcome> ReadConfigurationAsync(CancellationToken cancellationToken)
    {
        var state = await _configuration.GetStateAsync(cancellationToken).ConfigureAwait(false);

        if (state.IsConfigured)
        {
            return new LaunchStepOutcome(
                LaunchStepStatus.Succeeded,
                $"This computer is configured as '{state.DisplayName}'.",
                LaunchRemedySet.None);
        }

        if (string.Equals(state.UnconfiguredReason, MachineConfigurationReasons.Unreadable, StringComparison.Ordinal))
        {
            // There is no verdict about this machine, only that it could not be shown to be configured. That is a
            // stop with a retry rather than a machine to set up (FR-009, FR-017).
            return new LaunchStepOutcome(
                LaunchStepStatus.Failed,
                "This computer's configuration could not be read from the store, so the store cannot be shown to hold it.",
                LaunchRemedySet.RetryOnly);
        }

        return new LaunchStepOutcome(
            LaunchStepStatus.Succeeded,
            $"This computer is not configured yet — {DescribeUnconfigured(state.UnconfiguredReason)} — so setup names it.",
            LaunchRemedySet.None);
    }

    /// <summary>The reason a machine is unconfigured, in the reader's own words (FR-004).</summary>
    private static string DescribeUnconfigured(string? reason) => reason switch
    {
        MachineConfigurationReasons.NeverConfigured => "it has never been set up",
        MachineConfigurationReasons.Removed => "its configuration is no longer complete",
        MachineConfigurationReasons.Revoked => "its registry row has been retired",
        _ => "no configuration is recorded for it",
    };
}

/// <summary>
/// This computer's own name and the hardware address it presents, answering for the catalogue's
/// <c>read-hardware-identity</c> entry (`contracts/launch-step-contract.md` §1.1; FR-002, FR-015, FR-022).
/// </summary>
/// <remarks>
/// <para>
/// <b>Nothing is asked of a service.</b> The machine's own name and address are the facts it presents for
/// matching rather than facts the store holds, and the context already carries them, so the step reads the
/// read-only contract rather than reaching for one.
/// </para>
/// <para>
/// <b>An unreadable address is admitted rather than refused.</b> A fact that could not be read is not a failed
/// check (FR-015), so the step reports its work left undone and the launch carries on to identify the machine by
/// name alone.
/// </para>
/// </remarks>
internal sealed class ReadHardwareIdentityStep : ILaunchStep
{
    /// <summary>The catalogue entry this step answers for.</summary>
    private const string StepId = "read-hardware-identity";

    private readonly LaunchStep _descriptor;

    /// <summary>Creates the step over the sequence it takes its descriptor from.</summary>
    /// <param name="catalog">The sequence, which is where the step takes its descriptor from rather than restating it.</param>
    public ReadHardwareIdentityStep(LaunchStepCatalog catalog)
        => _descriptor = LaunchStepSupport.DescriptorFor(catalog, StepId);

    /// <inheritdoc />
    public LaunchStep Descriptor => _descriptor;

    /// <inheritdoc />
    public Task<LaunchStepOutcome> RunAsync(LaunchStepContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var machine = context.Machine;

        return Task.FromResult(machine.HardwareIdentityReadable
            ? new LaunchStepOutcome(
                LaunchStepStatus.Succeeded,
                $"This computer is '{machine.Hostname}' and presented the hardware address it identifies itself with.",
                LaunchRemedySet.None)
            : new LaunchStepOutcome(
                LaunchStepStatus.Skipped,
                $"This computer is '{machine.Hostname}' but no usable hardware address could be read, so it is identified by name alone.",
                LaunchRemedySet.None));
    }
}

/// <summary>
/// The store read that finds this computer by its name and hardware address, answering for the catalogue's
/// <c>read-computer-record</c> entry (`contracts/launch-step-contract.md` §1.1; FR-002, FR-015, FR-022).
/// </summary>
/// <remarks>
/// <para>
/// <b>The step is not the writer.</b> The store read lives in the machine facts service, which is the launch
/// pipeline's one writer for the resolved registry row; the step asks the service and reports what it answered,
/// rather than reaching for the store itself (constitution III; `contracts/launch-step-contract.md` §2).
/// </para>
/// <para>
/// <b>Not being in the store is a finding, not a failure.</b> A machine the registry does not hold is set up
/// against the store rather than stopped, so the read reports what it found and the pipeline decides (FR-006).
/// A read that could not be made at all — because there is no address to match on — is reported as left undone,
/// because a fact that could not be read is not a failed check (FR-015).
/// </para>
/// </remarks>
internal sealed class ReadComputerRecordStep : ILaunchStep
{
    /// <summary>The catalogue entry this step answers for.</summary>
    private const string StepId = "read-computer-record";

    private readonly LaunchStep _descriptor;
    private readonly MachineFactsService _machineFacts;

    /// <summary>Creates the step over the machine facts service, which is the pipeline's one writer for the row.</summary>
    /// <param name="catalog">The sequence, which is where the step takes its descriptor from rather than restating it.</param>
    /// <param name="machineFacts">The concrete service, because the store read is its write, and the read-only contract has none.</param>
    public ReadComputerRecordStep(LaunchStepCatalog catalog, MachineFactsService machineFacts)
    {
        ArgumentNullException.ThrowIfNull(machineFacts);

        _descriptor = LaunchStepSupport.DescriptorFor(catalog, StepId);
        _machineFacts = machineFacts;
    }

    /// <inheritdoc />
    public LaunchStep Descriptor => _descriptor;

    /// <inheritdoc />
    public Task<LaunchStepOutcome> RunAsync(LaunchStepContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        return LaunchStepSupport.RunGuardedAsync(
            "This computer's record could not be read from the store",
            token => ReadRecordAsync(token),
            cancellationToken);
    }

    /// <summary>Reads this computer's registry row and reports what the store answered.</summary>
    private async Task<LaunchStepOutcome> ReadRecordAsync(CancellationToken cancellationToken)
    {
        if (!_machineFacts.HardwareIdentityReadable)
        {
            // The read cannot be performed at all: without a usable address there is nothing to confirm a
            // name match against, and a read that would only be a guess is not attempted (FR-015).
            return new LaunchStepOutcome(
                LaunchStepStatus.Skipped,
                "This computer's hardware address could not be read, so its record cannot be confirmed and setup names it instead.",
                LaunchRemedySet.None);
        }

        var found = await _machineFacts.RefreshAsync(cancellationToken).ConfigureAwait(false);

        return found
            ? new LaunchStepOutcome(
                LaunchStepStatus.Succeeded,
                "The store holds a record for this computer.",
                LaunchRemedySet.None)
            : new LaunchStepOutcome(
                LaunchStepStatus.Succeeded,
                "The store holds no record for this computer, so setup names it.",
                LaunchRemedySet.None);
    }
}

/// <summary>
/// The save that names this computer and points it at its shared picture sources, answering for the catalogue's
/// <c>save-machine-configuration</c> entry (`contracts/launch-step-contract.md` §1.1,
/// `contracts/machine-configuration-contract.md` §1; FR-002, FR-007, FR-018).
/// </summary>
/// <remarks>
/// <para>
/// <b>One writer, and this is where the launch drives it.</b> The configuration service is the only code that
/// writes these rows; the step hands it the draft the setup surface captured and reports what it answered,
/// rather than writing anything itself (constitution III).
/// </para>
/// <para>
/// <b>Nothing to save is not a failure.</b> A machine that already holds its configuration, or one no draft has
/// been captured for yet, is reported as left undone and the launch carries on, because a save with nothing to
/// write is not a stop (FR-001, FR-019).
/// </para>
/// <para>
/// <b>A refusal is reported, not swallowed.</b> The service answers a display name another machine already holds
/// with a refusal rather than raising it, so the step turns that answer into a stated failure naming what to
/// change, and keeps the draft waiting so the corrected save can land.
/// </para>
/// </remarks>
internal sealed class SaveMachineConfigurationStep : ILaunchStep
{
    /// <summary>The catalogue entry this step answers for.</summary>
    private const string StepId = "save-machine-configuration";

    private readonly LaunchStep _descriptor;
    private readonly IMachineConfigurationService _configuration;
    private readonly IPendingMachineConfiguration _pending;

    /// <summary>Creates the step over the configuration service and the draft the setup surface captured.</summary>
    /// <param name="catalog">The sequence, which is where the step takes its descriptor from rather than restating it.</param>
    /// <param name="configuration">The service that owns these rows, and the only writer of them.</param>
    /// <param name="pending">The configuration captured and not yet saved.</param>
    public SaveMachineConfigurationStep(
        LaunchStepCatalog catalog,
        IMachineConfigurationService configuration,
        IPendingMachineConfiguration pending)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(pending);

        _descriptor = LaunchStepSupport.DescriptorFor(catalog, StepId);
        _configuration = configuration;
        _pending = pending;
    }

    /// <inheritdoc />
    public LaunchStep Descriptor => _descriptor;

    /// <inheritdoc />
    public Task<LaunchStepOutcome> RunAsync(LaunchStepContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        return LaunchStepSupport.RunGuardedAsync(
            "This computer's configuration could not be saved",
            token => SaveConfigurationAsync(token),
            cancellationToken);
    }

    /// <summary>Saves the captured configuration when there is one, and reports what the store answered.</summary>
    private async Task<LaunchStepOutcome> SaveConfigurationAsync(CancellationToken cancellationToken)
    {
        var state = await _configuration.GetStateAsync(cancellationToken).ConfigureAwait(false);

        if (state.IsConfigured)
        {
            return new LaunchStepOutcome(
                LaunchStepStatus.Skipped,
                $"This computer is already saved as '{state.DisplayName}', so there is nothing left to save.",
                LaunchRemedySet.None);
        }

        var draft = _pending.Pending;

        if (draft is null)
        {
            return new LaunchStepOutcome(
                LaunchStepStatus.Skipped,
                "This computer has not been given a name yet, so there is nothing to save until setup captures it.",
                LaunchRemedySet.None);
        }

        var result = await _configuration.SaveAsync(draft, cancellationToken).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            // The draft is deliberately left waiting: the answer the store refused is the answer to correct, so
            // the corrected save lands on the same draft rather than on nothing.
            return new LaunchStepOutcome(
                LaunchStepStatus.Failed,
                $"This computer's configuration was not saved because {DescribeRefusal(result.RefusalReason)}.",
                LaunchRemedySet.RetryOnly);
        }

        _pending.Clear();

        return new LaunchStepOutcome(
            LaunchStepStatus.Succeeded,
            $"This computer was saved as '{draft.DisplayName}'.",
            LaunchRemedySet.None);
    }

    /// <summary>Why a save was refused, in the reader's own words (FR-004).</summary>
    private static string DescribeRefusal(string? refusalReason) => refusalReason switch
    {
        MachineConfigurationRefusals.DisplayNameRequired => "it needs a name people will recognise",
        MachineConfigurationRefusals.DisplayNameInUse => "another computer already holds that name",
        _ => "the store refused it",
    };
}

/// <summary>
/// The configuration the machine-setup surface captured and has not yet saved.
/// </summary>
/// <remarks>
/// <para>
/// <b>The draft has to live somewhere between the screen and the save.</b> Setup runs before sign-in and the
/// launch step runs after it, so the captured configuration is kept here rather than in either of them: the
/// screen hands it over when the operator saves, and the step takes it and writes it once.
/// </para>
/// <para>
/// <b>It is not machine configuration.</b> This holds a draft, never a stored value: nothing here is read by a
/// consumer, and the store's rows are written only through <c>IMachineConfigurationService</c>.
/// </para>
/// </remarks>
internal interface IPendingMachineConfiguration
{
    /// <summary>The configuration captured and not yet saved, or <c>null</c> when nothing is waiting.</summary>
    MachineConfigurationDraft? Pending { get; }

    /// <summary>Keeps the configuration the setup surface captured, ready for the save that follows it.</summary>
    /// <param name="draft">What the setup surface captured.</param>
    void Hold(MachineConfigurationDraft draft);

    /// <summary>Clears what was waiting, once it has been saved.</summary>
    void Clear();
}

/// <inheritdoc />
internal sealed class PendingMachineConfiguration : IPendingMachineConfiguration
{
    private readonly object _gate = new();

    private MachineConfigurationDraft? _pending;

    /// <inheritdoc />
    public MachineConfigurationDraft? Pending
    {
        get
        {
            lock (_gate)
            {
                return _pending;
            }
        }
    }

    /// <inheritdoc />
    public void Hold(MachineConfigurationDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        lock (_gate)
        {
            _pending = draft;
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_gate)
        {
            _pending = null;
        }
    }
}
