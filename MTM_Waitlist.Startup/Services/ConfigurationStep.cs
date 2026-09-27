using Microsoft.Extensions.Options;

using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// The launch's first step: it reads what this computer keeps so it can reach the store, and it answers for the
/// catalogue's <c>read-local-settings</c> entry (`contracts/launch-step-contract.md` §1.1, S8.4; FR-002).
/// </summary>
/// <remarks>
/// <para>
/// <b>It reads; it does not reach.</b> The step proves this computer's own settings could be read and reports what
/// they hold. Contacting the store is the next step's work, so a store that is configured but down is reported
/// where the store is actually consulted rather than here, and a machine whose connection comes from the
/// environment rather than from its saved settings is not refused for it.
/// </para>
/// <para>
/// <b>A step never announces itself.</b> The runner names the descriptor before it calls <see cref="RunAsync"/>,
/// so a step that named itself would write the line twice (FR-002). A step reports an outcome; what the launch
/// does about it belongs to the pipeline (FR-001).
/// </para>
/// </remarks>
public sealed class ConfigurationStep : ILaunchStep
{
    /// <summary>The catalogue entry this step answers for.</summary>
    private const string StepId = "read-local-settings";

    private readonly LaunchStep _descriptor;
    private readonly IOptions<WaitlistDatabaseOptions> _savedSettings;

    /// <summary>Creates the step over this computer's own saved settings.</summary>
    /// <param name="catalog">The sequence, which is where the step takes its descriptor from rather than restating it.</param>
    /// <param name="savedSettings">The local settings that let this computer reach the store (S8.4).</param>
    public ConfigurationStep(LaunchStepCatalog catalog, IOptions<WaitlistDatabaseOptions> savedSettings)
    {
        ArgumentNullException.ThrowIfNull(savedSettings);

        _descriptor = LaunchStepSupport.DescriptorFor(catalog, StepId);
        _savedSettings = savedSettings;
    }

    /// <inheritdoc />
    public LaunchStep Descriptor => _descriptor;

    /// <inheritdoc />
    public Task<LaunchStepOutcome> RunAsync(LaunchStepContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        return LaunchStepSupport.RunGuardedAsync(
            "This computer's saved settings could not be read",
            _ => Task.FromResult(ReadSavedSettings()),
            cancellationToken);
    }

    /// <summary>Reads the saved settings and reports what they hold.</summary>
    private LaunchStepOutcome ReadSavedSettings()
    {
        var connection = _savedSettings.Value.ConnectionString;

        return string.IsNullOrWhiteSpace(connection)
            ? new LaunchStepOutcome(
                LaunchStepStatus.Succeeded,
                "This computer's saved settings name no store connection, so the store is reached from the environment instead.",
                LaunchRemedySet.None)
            : new LaunchStepOutcome(
                LaunchStepStatus.Succeeded,
                "This computer's saved settings name the store it reaches.",
                LaunchRemedySet.None);
    }
}

/// <summary>
/// The pre-sign-in read of the sign-in this computer was asked to remember, answering for the catalogue's
/// <c>read-remembered-sign-in</c> entry (`contracts/launch-step-contract.md` §1.1, S8.2; FR-002, FR-014).
/// </summary>
/// <remarks>
/// <para>
/// <b>Nothing is read, and nothing is invented.</b> The read against the store — and the unlock through the shared
/// key file — belongs to the remembered-sign-in service (T113), and until that service exists the honest answer is
/// that no remembered sign-in was read and the ordinary sign-in form will ask. This step deliberately does not
/// stand in for that service: a placeholder that answered as though it had decrypted something would report a
/// fact the launch does not have.
/// </para>
/// <para>
/// <b>It cannot block the launch.</b> A remembered sign-in that cannot be read is a fall-back to the ordinary
/// form, never a stop (FR-014), so the step reports itself left undone and the launch carries on.
/// </para>
/// </remarks>
internal sealed class ReadRememberedSignInStep : ILaunchStep
{
    /// <summary>The catalogue entry this step answers for.</summary>
    private const string StepId = "read-remembered-sign-in";

    private readonly LaunchStep _descriptor;

    /// <summary>Creates the step over the sequence it takes its descriptor from.</summary>
    /// <param name="catalog">The sequence, which is where the step takes its descriptor from rather than restating it.</param>
    internal ReadRememberedSignInStep(LaunchStepCatalog catalog)
        => _descriptor = LaunchStepSupport.DescriptorFor(catalog, StepId);

    /// <inheritdoc />
    public LaunchStep Descriptor => _descriptor;

    /// <inheritdoc />
    public Task<LaunchStepOutcome> RunAsync(LaunchStepContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(new LaunchStepOutcome(
            LaunchStepStatus.Skipped,
            "No remembered sign-in is read yet, so the sign-in form will ask for the name and the password.",
            LaunchRemedySet.None));
    }
}

/// <summary>
/// The two things every pre-sign-in step needs and none of them should restate: where its descriptor comes from,
/// and how a dependency's failure is turned into a reported outcome.
/// </summary>
/// <remarks>
/// <para>
/// <b>A descriptor is never copied.</b> <see cref="DescriptorFor"/> resolves the step's entry from the shipped
/// catalogue, so a step cannot drift from the sequence the launch walks: the id, the name, the category, the
/// stated maximum, the best-effort marker and the target are the catalogue's, and a catalogue that lost the entry
/// fails immediately rather than naming a step the launch no longer holds (FR-002, FR-003).
/// </para>
/// <para>
/// <b>A failure is reported, never raised.</b> A step calls a service, and a service that cannot answer throws;
/// <see cref="RunGuardedAsync"/> turns that into a <see cref="LaunchStepStatus.Failed"/> outcome carrying the
/// plain-language diagnosis and the remedies, because an exception escaping a step would reach the person as an
/// unattributed crash instead of a named line (FR-004). Abandoning the launch is not a step failure, so a
/// cancelled caller's token is let through rather than recorded as one.
/// </para>
/// </remarks>
internal static class LaunchStepSupport
{
    /// <summary>
    /// The catalogue's entry for a step id, or an exception when the sequence no longer declares it.
    /// </summary>
    /// <param name="catalog">The sequence the launch walks.</param>
    /// <param name="stepId">The entry the step answers for.</param>
    /// <exception cref="InvalidOperationException">
    /// The catalogue holds no entry for <paramref name="stepId"/>, so the step could not be named before it runs.
    /// </exception>
    internal static LaunchStep DescriptorFor(LaunchStepCatalog catalog, string stepId)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentException.ThrowIfNullOrWhiteSpace(stepId);

        return catalog.Find(stepId)
            ?? throw new InvalidOperationException(
                $"The launch catalogue no longer declares '{stepId}', so the step could not be named before it runs (FR-002).");
    }

    /// <summary>
    /// Runs a step's work and reports a failure rather than letting it escape.
    /// </summary>
    /// <param name="whatCouldNotFinish">What the step was doing, in plain language, for the diagnosis (FR-004).</param>
    /// <param name="work">The step's work.</param>
    /// <param name="cancellationToken">Cancelled when the launch is abandoned, and when the step passes its stated maximum.</param>
    internal static async Task<LaunchStepOutcome> RunGuardedAsync(
        string whatCouldNotFinish,
        Func<CancellationToken, Task<LaunchStepOutcome>> work,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(work);

        try
        {
            return await work(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Abandoning the launch is not a step failure, so it is not reported as one (`contracts/launch-step-contract.md` §2).
            throw;
        }
        catch (Exception exception)
        {
            // Every failed step is repeatable (FR-016), and no reset is offered here: a reset could not remove
            // the cause of a step that could not reach what it needed (FR-017).
            return new LaunchStepOutcome(
                LaunchStepStatus.Failed,
                $"{whatCouldNotFinish}: {exception.Message}",
                LaunchRemedySet.RetryOnly);
        }
    }
}
