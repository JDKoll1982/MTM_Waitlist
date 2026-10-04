using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// The machine gate: it answers whether the store knows the computer this person has signed in on, and it
/// admits a computer whose hardware identity could not be read (`contracts/identity-contracts.md` section 2;
/// FR-015).
/// </summary>
/// <remarks>
/// <para>
/// <b>A fact that could not be read is not a failed check.</b> A machine that presents no usable hardware
/// address is admitted rather than refused (FR-015). Refusing would turn "this workstation could not be asked
/// who it is" into "this person may not sign in", and the person cannot do anything about either.
/// </para>
/// <para>
/// <b>A machine the store does not hold is not admitted, and neither is one the store could not be asked
/// about.</b> Those two are the same answer for the same reason: nothing has been shown to be known about this
/// computer, and a gate that cannot check has found nothing. The two stay separate verdicts so the surface can
/// say which of them happened, because one is the person's call and the other is not.
/// </para>
/// <para>
/// <b>It reads, it does not write.</b> The registry read is <see cref="MachineFactsService.RefreshAsync"/>, the
/// same one the machine-readiness steps use, so the gate and the rest of the launch cannot end up with two
/// opinions about whether this computer is registered.
/// </para>
/// </remarks>
internal sealed class MachineGateService
{
    /// <summary>The area a store failure is recorded under, so the fault has a name beside it.</summary>
    private const string LogModule = "MachineGate";

    private readonly MachineFactsService _machine;

    /// <summary>Creates the gate over this computer's facts, which is where the registry read lives.</summary>
    /// <param name="machine">This computer's facts, including the store read that confirms it is registered.</param>
    public MachineGateService(MachineFactsService machine)
        => _machine = machine ?? throw new ArgumentNullException(nameof(machine));

    /// <summary>
    /// Checks this computer against the store and answers with one of the gate's four verdicts.
    /// </summary>
    /// <param name="cancellationToken">Cancels the store read when the step's stated maximum passes.</param>
    /// <returns>The verdict, whether it admits the person, and the plain-language reason behind it.</returns>
    internal async Task<MachineGateResult> CheckAsync(CancellationToken cancellationToken)
    {
        if (!_machine.HardwareIdentityReadable)
        {
            // Nothing to match on, and nothing the person could do about it. An unreadable fact is admitted
            // (FR-015), and it is said out loud so the admission is not mistaken for a confirmed match.
            return new MachineGateResult(
                MachineGateVerdict.IdentityUnreadable,
                IsAdmitted: true,
                Reason: "This computer presents no hardware address, so it could not be checked against the store and is admitted.");
        }

        bool isRegistered;

        try
        {
            isRegistered = await _machine.RefreshAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Abandoning the launch is not a gate verdict.
            throw;
        }
        catch (Exception exception)
        {
            AppLog.Error(LogModule, exception, "This computer could not be checked against the store.");

            return new MachineGateResult(
                MachineGateVerdict.StoreUnreadable,
                IsAdmitted: false,
                Reason: "The store could not be asked whether it knows this computer, so the check was not passed.");
        }

        return isRegistered
            ? new MachineGateResult(
                MachineGateVerdict.Known,
                IsAdmitted: true,
                Reason: "The store knows this computer.")
            : new MachineGateResult(
                MachineGateVerdict.Unregistered,
                IsAdmitted: false,
                Reason: "The store holds no registered record for this computer.");
    }
}

/// <summary>
/// The four answers the machine gate can give (FR-015).
/// </summary>
internal enum MachineGateVerdict
{
    /// <summary>The store holds this computer and admits the person.</summary>
    Known,

    /// <summary>This computer presents a hardware address and the store still does not hold it.</summary>
    Unregistered,

    /// <summary>This computer presents no usable hardware address, so it is admitted on a fact that could not be read.</summary>
    IdentityUnreadable,

    /// <summary>The store could not be asked, so nothing was confirmed and nothing is admitted.</summary>
    StoreUnreadable,
}

/// <summary>
/// What the gate answered: which of its four verdicts, whether that admits the person, and why (FR-015).
/// </summary>
/// <param name="Verdict">Which of the four answers the check produced.</param>
/// <param name="IsAdmitted">
/// Whether the person carries on. It is true for a known computer and for one whose hardware identity could not
/// be read, and false for a computer the store does not hold and for a store that could not answer.
/// </param>
/// <param name="Reason">The plain-language reason, written so the launch feed can state it rather than imply it.</param>
internal sealed record MachineGateResult(
    MachineGateVerdict Verdict,
    bool IsAdmitted,
    string Reason);
