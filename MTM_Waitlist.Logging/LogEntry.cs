namespace MTM_Waitlist.Module_Logging;

/// <summary>
/// One entry as a call site describes it (`contracts/logging-contract.md` §1).
/// </summary>
/// <remarks>
/// <para>
/// <b>What a call site supplies is only what a call site knows.</b> The severity, the module it is writing
/// about, what it was doing, how it ended and the fault it hit. Everything else — the machine, the person, the
/// runtime and store context, the serialized exception chain and the fingerprint that groups it — is captured by
/// the seam at write time, because a call site that has to remember to pass the machine eventually will not
/// (FR-021, FR-034).
/// </para>
/// <para>
/// <b>The values the seam owns are the nullable ones.</b> <see cref="Action"/> defaults to
/// <see cref="Module"/>, <see cref="Outcome"/> defaults from the severity, <see cref="CorrelationId"/> is
/// generated when the caller names none, and <see cref="ExceptionDetail"/> and
/// <see cref="ExceptionFingerprint"/> are produced by the seam's serializer rather than by a call site
/// (contract §1.1, §1.2).
/// </para>
/// </remarks>
/// <param name="Severity">The entry's severity, from the one vocabulary the store's <c>level</c> column holds.</param>
/// <param name="Module">
/// The part of the application the entry came from. It becomes the entry's <c>module</c> column, which is what
/// the panel filters on, and it is also the action of last resort for an entry whose caller named none.
/// </param>
/// <param name="Message">
/// What the entry says. The table's <c>message</c> column is <c>TEXT NOT NULL</c>, so this is never null; the
/// seam writes a marker rather than dropping the entry when a caller passes nothing.
/// </param>
/// <param name="Outcome">
/// How the operation under way ended: <c>Success</c>, <c>Failure</c>, <c>Blocked</c> or <c>Retried</c>. The
/// table declares the column <c>NOT NULL</c> (contract §1).
/// </param>
/// <param name="Action">
/// The operation under way, which becomes <c>event_action</c> — also <c>NOT NULL</c>. An entry no one can
/// filter by action is an entry no one can find, so the seam substitutes <see cref="Module"/> when this is
/// blank (FR-035).
/// </param>
/// <param name="ErrorType">
/// The fault's type, when there was a fault. It becomes <c>error_type</c> and never <c>NULL</c> for an entry
/// raised with an exception, whether the caller named it or the seam read it from the exception.
/// </param>
/// <param name="ExceptionDetail">
/// The serialized exception chain. A call site leaves this null and passes the exception to the seam's level
/// helper instead; it is a member so an entry that has already been through a serializer can be re-written
/// without losing its chain.
/// </param>
/// <param name="ExceptionFingerprint">
/// The grouping hash. A call site never computes it — the seam derives it from the entry's fault type, module,
/// action and the shape of its message (FR-033).
/// </param>
/// <param name="Target">
/// The UI target the caller can name: the window, screen, control or command. It lands in <c>payload_json</c>
/// under <c>ui</c> rather than in a column of its own.
/// </param>
/// <param name="CorrelationId">
/// The caller's correlation, when it has one, so an entry can be tied to the workflow that raised it.
/// </param>
public sealed record LogEntry(
    LogSeverity Severity,
    string Module,
    string Message,
    string Outcome,
    string? Action,
    string? ErrorType,
    string? ExceptionDetail,
    string? ExceptionFingerprint,
    string? Target,
    string? CorrelationId);
