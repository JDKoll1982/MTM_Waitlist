namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Settles the Infor Visual reachability verdict while the startup screen is still showing.
/// </summary>
/// <remarks>
/// <para>
/// The verdict is what lets a read skip a live attempt it would only lose, so the cost of establishing it has
/// to be paid somewhere. Paying it during startup keeps it off every screen: the hysteresis needs two
/// consecutive probe failures, and the steady-state cadence would otherwise let the second failure land after
/// the shell is already accepting input — leaving the application briefly attempting a source it is about to
/// declare unreachable, which is exactly the lag the user feels on the first read of a session.
/// </para>
/// <para>
/// Implemented by the probe owner, because that is the only thing permitted to touch Infor Visual for
/// reachability. Priming must not become a second probe path with its own idea of what "unreachable" means.
/// </para>
/// </remarks>
public interface IVisualVerdictPrimer
{
    /// <summary>
    /// Probes until the verdict is settled, or the attempt budget is exhausted. Never throws.
    /// </summary>
    /// <param name="cancellationToken">Stops priming.</param>
    /// <remarks>
    /// An inconclusive prime is a normal outcome: the steady-state probe loop continues regardless, so the
    /// worst case is the behaviour the application had before priming existed.
    /// </remarks>
    Task PrimeAsync(CancellationToken cancellationToken = default);
}
