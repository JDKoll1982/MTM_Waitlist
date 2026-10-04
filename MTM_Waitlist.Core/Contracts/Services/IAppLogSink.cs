namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// The two members the static <c>AppLog</c> facade forwards to, so a static call site can reach the store-backed
/// seam without naming the logging module (contract §5).
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this contract is here rather than in <c>MTM_Waitlist.Logging</c>.</b> The seam itself is
/// <c>MTM_Waitlist.Module_Logging.ILogService</c>, but the logging module references
/// <c>MTM_Waitlist.Core</c> for the machine, person and store contracts it composes an entry from, so
/// <c>MTM_Waitlist.Core</c> — and every module library beneath the app — cannot reference the logging module
/// back without a circular project reference. This interface is the bridge: it is declared on the lower side of
/// that edge, and the seam implements it.
/// </para>
/// <para>
/// <b>Signatures are the seam's, not the facade's.</b> They match
/// <c>MTM_Waitlist.Module_Logging.ILogService.Info</c> and <c>.Error</c> exactly, so <c>ILogService</c>
/// satisfies this contract with no adapter between them. The static facade takes the retired type's argument
/// order instead, because its 489 call sites are repointed by a token replacement that never touches arguments.
/// </para>
/// </remarks>
public interface IAppLogSink
{
    /// <summary>
    /// Records that something happened as the caller expected.
    /// </summary>
    /// <param name="module">The part of the application the entry came from.</param>
    /// <param name="message">What the entry says.</param>
    /// <param name="target">The UI target the caller can name, when it can name one.</param>
    void Info(string module, string message, string? target = null);

    /// <summary>
    /// Records that the operation under way failed, with the fault that failed it.
    /// </summary>
    /// <param name="module">The part of the application the entry came from.</param>
    /// <param name="message">What the entry says.</param>
    /// <param name="exception">The fault, when there is one.</param>
    void Error(string module, string message, Exception? exception = null);
}
