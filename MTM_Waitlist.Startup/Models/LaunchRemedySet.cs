namespace MTM_Waitlist.Module_Startup.Models;

/// <summary>
/// The actions a stop may offer (`contracts/launch-step-contract.md` §4, FR-016, FR-017, FR-018).
/// </summary>
/// <remarks>
/// <para>
/// <b>Only actions that could remove the cause are offered.</b> <see cref="CanRestoreDefaults"/> is false
/// wherever a reset could not remove the cause — a store that cannot be reached never offers a reset (FR-017) —
/// and when it is true, <see cref="RestoreDefaultsPreview"/> names exactly what will be reset before anything is
/// reset (FR-018).
/// </para>
/// <para>
/// <b>A fault repaired without the person never reaches this type.</b> Silent repair produces no prompt and no
/// remedy set (FR-019). The two static members below are the shapes a step needs without a remedy table: the
/// failure of any step is repeatable (FR-016), which is the one action always available.
/// </para>
/// </remarks>
/// <param name="CanRetry">Whether the work that stopped the launch can be repeated (FR-016).</param>
/// <param name="CanRestoreDefaults">
/// Whether resetting this machine's configuration could remove the cause (FR-017).
/// </param>
/// <param name="RestoreDefaultsPreview">
/// Exactly what a reset would touch, named before anything is reset, or <c>null</c> when no reset is offered
/// (FR-018).
/// </param>
public sealed record LaunchRemedySet(
    bool CanRetry,
    bool CanRestoreDefaults,
    string? RestoreDefaultsPreview)
{
    /// <summary>A set that offers nothing, for a fault the person cannot answer.</summary>
    public static LaunchRemedySet None { get; } = new(false, false, null);

    /// <summary>
    /// A set that offers exactly one action: repeat the work that failed. Any failed step is repeatable
    /// (FR-016), and no reset is offered, because a reset is only ever offered where it could remove the cause
    /// (FR-017).
    /// </summary>
    public static LaunchRemedySet RetryOnly { get; } = new(true, false, null);
}
