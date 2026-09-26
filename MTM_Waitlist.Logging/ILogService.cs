namespace MTM_Waitlist.Module_Logging;

/// <summary>
/// The logging seam every call site writes through (FR-032, `contracts/logging-contract.md` §1).
/// </summary>
/// <remarks>
/// <para>
/// <b>Unconditional.</b> Nothing on this interface carries a conditional-compilation attribute, and nothing
/// behind it is wrapped in a debug-only guard. The type it replaces was marked so that a Release build compiled
/// its calls — and their arguments — out entirely, which is why a released build recorded nothing at all and a
/// production fault left no trace anywhere (SC-003, plan D6). A replacement that kept the attribute would leave
/// the store empty on a shop-floor machine and the panel showing nothing.
/// </para>
/// <para>
/// <b>The level helpers return; they do not await the store.</b> A write is queued and returns, so a slow or
/// unreachable store never delays the caller whose fault is being recorded (S13, plan D20).
/// <see cref="FlushAsync"/> is the only awaiting member, and it is bounded.
/// </para>
/// <para>
/// <b>The write path never throws to its caller.</b> The seam absorbs its own failures and records nothing
/// about them, so one broken write cannot produce a fault that produces a fault (FR-037, SC-016).
/// </para>
/// </remarks>
public interface ILogService
{
    /// <summary>
    /// Records an entry the caller has already described.
    /// </summary>
    /// <param name="entry">The entry. The seam fills in everything the call site cannot know.</param>
    void Write(LogEntry entry);

    /// <summary>
    /// Records that something happened as the caller expected.
    /// </summary>
    /// <param name="module">The part of the application the entry came from.</param>
    /// <param name="message">What the entry says.</param>
    /// <param name="target">The UI target the caller can name, when it can name one.</param>
    void Info(string module, string message, string? target = null);

    /// <summary>
    /// Records that something happened which did not stop the caller but is worth a reader's attention.
    /// </summary>
    /// <param name="module">The part of the application the entry came from.</param>
    /// <param name="message">What the entry says.</param>
    /// <param name="errorType">The fault type the warning is about, when it is about one.</param>
    void Warn(string module, string message, string? errorType = null);

    /// <summary>
    /// Records that the operation under way failed, with the fault that failed it.
    /// </summary>
    /// <param name="module">The part of the application the entry came from.</param>
    /// <param name="message">What the entry says.</param>
    /// <param name="exception">
    /// The fault, when there is one. The seam serializes its complete chain into <c>exception_detail</c> and
    /// derives the fingerprint from it; no call site serializes an exception (contract §1.1, §1.2).
    /// </param>
    void Error(string module, string message, Exception? exception = null);

    /// <summary>
    /// Records that the operation under way failed and the application could not continue as it was.
    /// </summary>
    /// <param name="module">The part of the application the entry came from.</param>
    /// <param name="message">What the entry says.</param>
    /// <param name="exception">The fault, when there is one.</param>
    void Critical(string module, string message, Exception? exception = null);

    /// <summary>
    /// Waits, within a stated maximum, for the entries already raised to reach the store.
    /// </summary>
    /// <remarks>
    /// The only bounded wait on the seam, and the one that makes a launch which ends immediately afterwards
    /// still record what it did (SC-003). It returns whether or not the store answered, and it never throws.
    /// </remarks>
    /// <param name="cancellationToken">The caller's cancellation.</param>
    /// <returns>A task that completes when the queue is empty or the flush bound is reached.</returns>
    Task FlushAsync(CancellationToken cancellationToken);
}
