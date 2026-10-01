using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Startup.Services;

namespace MTM_Waitlist.Module_Startup.ViewModels;

/// <summary>
/// A stopped launch's state: what stopped it, what may be done about it, and what a reset would touch before
/// anything is reset (`contracts/launch-step-contract.md` §4, §5; FR-004, FR-016, FR-017, FR-018, FR-019).
/// </summary>
/// <remarks>
/// <para>
/// <b>Repeating the failed work means starting the application again.</b> A launch is one pass, and this state
/// never resumes a stopped one: the surface's repeat starts a replacement instance, which runs the launch from
/// its first step and finds the fault either put right or still there. Carrying a half-run sequence on from a
/// step id was the thing that left a stop that could be repeated and never recovered, so the repeat is a restart
/// (FR-016).
/// </para>
/// <para>
/// <b>The cause is the failing line the launch already wrote.</b> The pipeline hands the host an outcome and not a
/// reason, so this state reads the feed for the last line a step failed on and takes that line's own words as the
/// diagnosis. The words are the step's, written for a person to read, so the surface repeats the cause rather
/// than showing a step number (FR-004).
/// </para>
/// <para>
/// <b>A best-effort line never counts as the stop.</b> The picture refresh and the external-system priming report
/// their failures on the same feed and cannot stop the launch (FR-026), so a line of theirs is passed over.
/// Anything else would let a recorded, harmless failure be shown as the reason a person cannot carry on.
/// </para>
/// <para>
/// <b>The last such failure is the one that stopped the launch.</b> A retry that succeeded and a later step that
/// failed both leave lines behind, and the surface is rebuilt for each stop, so reading the last one shows the
/// stop that just happened rather than the first one ever seen.
/// </para>
/// <para>
/// <b>Only remedies that could remove the cause are drawn.</b> Whether a reset is offered is the table's answer
/// and never this class's (FR-017), and the parts a reset would touch come from the same table, so the sentence
/// the person agrees to and the reset that runs describe one set (FR-018).
/// </para>
/// <para>
/// <b>A repair is made before the person is asked.</b> <see cref="PrepareAsync"/> offers the fault to the repair
/// policy first, and only a fault that policy cannot put right without asking reaches the window (FR-019).
/// </para>
/// </remarks>
internal sealed partial class BlockedStateViewModel : ObservableObject
{
    private readonly LaunchStepCatalog _catalog;
    private readonly StartupRecoveryService _recovery;
    private readonly IProcessRestarter _restarter;

    /// <summary>Creates the stopped launch's state over the lines it wrote, the sequence, the policy and the restarter.</summary>
    /// <param name="catalog">The sequence, which is how a failing line is resolved to the step that wrote it.</param>
    /// <param name="feed">The lines the launch wrote, which is where the cause comes from.</param>
    /// <param name="recovery">The policy that decides what a reset may touch and what is repaired without asking.</param>
    /// <param name="restarter">How a replacement instance is started, which is how this surface repeats the work.</param>
    /// <exception cref="ArgumentNullException">Any argument is <c>null</c>.</exception>
    public BlockedStateViewModel(
        LaunchStepCatalog catalog,
        ILaunchActivityFeed feed,
        StartupRecoveryService recovery,
        IProcessRestarter restarter)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(feed);
        ArgumentNullException.ThrowIfNull(recovery);
        ArgumentNullException.ThrowIfNull(restarter);

        _catalog = catalog;
        _recovery = recovery;
        _restarter = restarter;

        var stop = FindStop(feed);

        FailedStepId = stop.StepId;
        Diagnosis = stop.Diagnosis;
        Cause = BlockedStateRemedies.Classify(_catalog.Find(FailedStepId));
        Remedies = BlockedStateRemedies.For(Cause);
        ResetParts = BlockedStateRemedies.ResetPartsFor(Cause);
        SilentRepairParts = BlockedStateRemedies.SilentRepairPartsFor(Cause);
    }

    /// <summary>The catalogue entry that failed, or <c>null</c> when the feed named no failure.</summary>
    /// <remarks>It names the failing line for the diagnosis and for the log; the repeat no longer resumes at it.</remarks>
    internal string? FailedStepId { get; }

    /// <summary>What stopped the launch, in the failing step's own words (FR-004).</summary>
    internal string Diagnosis { get; }

    /// <summary>What stopped the launch, as the coarse cause a remedy is matched to.</summary>
    internal BlockedStateCause Cause { get; }

    /// <summary>The actions this stop may offer, and the preview of what a reset would touch (FR-016, FR-017, FR-018).</summary>
    internal LaunchRemedySet Remedies { get; }

    /// <summary>Exactly the parts a reset would restore, which is the set the preview names (FR-018).</summary>
    internal IReadOnlyList<string> ResetParts { get; }

    /// <summary>The parts a repair may put right without asking about this cause, or an empty list (FR-019).</summary>
    internal IReadOnlyList<string> SilentRepairParts { get; }

    /// <summary>True while the surface is working, which guards a repeated press rather than a slow store.</summary>
    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    /// <summary>What the surface says when a reset was refused or could not be made, or <c>null</c> when it has nothing to add.</summary>
    [ObservableProperty]
    public partial string? Message { get; set; }

    /// <summary>Whether there is anything for the surface to say beyond the diagnosis.</summary>
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

    /// <summary>Whether the failed work may be repeated, which is every stop: one restart costs nothing (FR-016).</summary>
    public bool CanRetry => Remedies.CanRetry;

    /// <summary>Whether a reset could remove this cause, which is what makes the restore control visible (FR-017).</summary>
    public bool CanRestoreDefaults => Remedies.CanRestoreDefaults;

    /// <summary>Exactly what a reset would touch, named before anything is reset, or <c>null</c> when none is offered.</summary>
    public string? RestoreDefaultsPreview => Remedies.RestoreDefaultsPreview;

    /// <summary>The surface's heading.</summary>
    public string HeadingText => "Startup_BlockedState.Heading".GetLocalized();

    /// <summary>What the surface says about why it is asking.</summary>
    public string SubtitleText => "Startup_BlockedState.Subtitle".GetLocalized();

    /// <summary>The label over the cause.</summary>
    public string DiagnosisLabelText => "Startup_BlockedState.DiagnosisLabel".GetLocalized();

    /// <summary>The action that starts the application again, which is how the failed work is repeated (FR-016).</summary>
    public string RetryActionText => "Startup_BlockedState.RetryAction".GetLocalized();

    /// <summary>The action that restores this computer's defaults once the person has seen what it will touch (FR-018).</summary>
    public string RestoreDefaultsActionText => "Startup_BlockedState.RestoreDefaultsAction".GetLocalized();

    /// <summary>The action that ends the process.</summary>
    public string CloseActionText => "Startup_BlockedState.CloseAction".GetLocalized();

    /// <summary>The title of the popup that lists what a reset will touch before anything is reset (FR-018).</summary>
    public string ResetPreviewTitleText => "Startup_BlockedState.ResetPreviewTitle".GetLocalized();

    /// <summary>The action that agrees to the reset the popup listed (FR-018).</summary>
    public string ResetPreviewConfirmText => "Startup_BlockedState.ResetPreviewConfirm".GetLocalized();

    /// <summary>The action that turns the reset down, which leaves everything as it is.</summary>
    public string ResetPreviewCancelText => "Startup_BlockedState.ResetPreviewCancel".GetLocalized();

    /// <summary>
    /// Why the process is ending when the person closes this surface, said before it goes so a closed window is
    /// never reported as a crash (FR-008).
    /// </summary>
    public string CloseReasonText => "Startup_BlockedState.CloseReason".GetLocalized();

    /// <summary>Whether the surface has anything to say follows every change to what it says.</summary>
    /// <param name="value">The new value, which is not read here.</param>
    partial void OnMessageChanged(string? value) => OnPropertyChanged(nameof(HasMessage));

    /// <summary>
    /// Offers the fault to the repair policy and answers whether the person is needed at all (FR-019).
    /// </summary>
    /// <param name="cancellationToken">Cancels the repair.</param>
    /// <returns>
    /// <c>true</c> when the surface has to be shown, because the fault is one only a person can decide about, or
    /// because the repair changed nothing, or because the replacement instance could not be started. <c>false</c>
    /// when the fault was put right without asking, in which case the application is already starting again.
    /// </returns>
    /// <remarks>
    /// This runs before the window is shown, so a fault repaired without asking produces no prompt at all rather
    /// than a surface that appears and takes itself away again. The launch that starts now is a second pass over
    /// the same sequence, which is what carries the repaired fault on (FR-019).
    /// </remarks>
    internal async Task<bool> PrepareAsync(CancellationToken cancellationToken)
    {
        if (SilentRepairParts.Count == 0)
        {
            return true;
        }

        var repaired = await _recovery
            .RepairWithoutAskingAsync(SilentRepairParts, cancellationToken)
            .ConfigureAwait(true);

        if (repaired.Reset.Count == 0)
        {
            // The repair was not made, so the fault stands and the person is the answer rather than a second
            // attempt at the same thing (FR-019).
            return true;
        }

        return !StartAgain();
    }

    /// <summary>
    /// Starts the application again and ends this one, which is how this surface repeats the failed work
    /// (FR-016).
    /// </summary>
    /// <remarks>
    /// A replacement instance that could not be started is reported rather than swallowed: the person is looking
    /// at a surface, and a surface whose only action does nothing is worse than one that says it could not. The
    /// ending goes through the same lifecycle seam signing out uses, so the process that leaves is one that knows
    /// it is being replaced.
    /// </remarks>
    [RelayCommand]
    private void Retry()
    {
        Message = null;

        _ = StartAgain();
    }

    /// <summary>Starts a replacement instance, and answers whether it was started.</summary>
    /// <returns>
    /// <c>true</c> when a replacement instance started, in which case this process is already on its way out.
    /// <c>false</c> when it could not, in which case <see cref="Message"/> says so.
    /// </returns>
    private bool StartAgain()
    {
        try
        {
            if (_restarter.Restart())
            {
                AppLifecycleHost.ExitApplication();

                return true;
            }
        }
        catch (Exception exception)
        {
            AppLog.Error("BlockedState", exception, "The application could not be started again.");
        }

        Message = "Startup_BlockedState.RestartFailed".GetLocalized();

        return false;
    }

    /// <summary>
    /// Restores the parts the person agreed to, and then starts the application again so the launch runs once
    /// more against the configuration that was just restored (FR-018).
    /// </summary>
    /// <remarks>
    /// The reset goes through the policy, which narrows it to this computer's own configuration before the one
    /// implementation runs, so nothing outside this machine can be touched by agreeing to this popup (FR-018).
    /// </remarks>
    [RelayCommand]
    private async Task RestoreDefaultsAsync()
    {
        if (IsBusy || !CanRestoreDefaults)
        {
            return;
        }

        IsBusy = true;
        Message = null;

        try
        {
            var reset = await _recovery.RestoreDefaultsAsync(ResetParts, CancellationToken.None).ConfigureAwait(true);

            if (!reset.Succeeded)
            {
                Message = "Startup_BlockedState.ResetRefused".GetLocalized();
                return;
            }

            _ = StartAgain();
        }
        catch (Exception exception)
        {
            // A store that refused the reset leaves everything as it was, so the person is told and the surface
            // stays open rather than the launch carrying on with a configuration that was never restored.
            AppLog.Error("BlockedState", exception, "This computer's configuration could not be restored.");
            Message = "Startup_BlockedState.ResetFailed".GetLocalized();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// The stop the feed last states: the failing line's step and its own words, or an empty answer when the feed
    /// names no failure.
    /// </summary>
    /// <param name="feed">The lines the launch wrote.</param>
    /// <returns>The step that failed and the words its line carries.</returns>
    private (string? StepId, string Diagnosis) FindStop(ILaunchActivityFeed feed)
    {
        string? stepId = null;
        var diagnosis = string.Empty;

        foreach (var entry in feed.Entries)
        {
            if (entry.Kind is not LaunchFeedEntryKind.StepFailed)
            {
                continue;
            }

            var descriptor = _catalog.Find(entry.StepId);

            // A best-effort line is a recorded failure the launch carries on from, so it is not a stop (FR-026),
            // and a line for a step this sequence does not hold cannot be resumed at (FR-020).
            if (descriptor is null || descriptor.IsBestEffort)
            {
                continue;
            }

            stepId = entry.StepId;
            diagnosis = entry.Text;
        }

        return (stepId, diagnosis);
    }
}
