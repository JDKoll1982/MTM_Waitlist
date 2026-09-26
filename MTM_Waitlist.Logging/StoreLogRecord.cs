namespace MTM_Waitlist.Module_Logging;

/// <summary>
/// One entry as the store's single writer takes it: the columns of <c>ops_startup_logs</c> that
/// <c>sp_ops_startup_logs_insert</c> accepts (`contracts/logging-contract.md` §3).
/// </summary>
/// <remarks>
/// <para>
/// <b>The seam composes this, not a call site.</b> It is the point at which the parts a call site cannot know —
/// the machine, the person, the runtime and store context, the serialized exception chain and the fingerprint —
/// have been filled in, so the writer's whole job is to hand the values to the procedure in the order they were
/// raised.
/// </para>
/// <para>
/// <b>What is deliberately absent.</b> No <c>created_utc</c>, no <c>previous_hash</c> and no <c>entry_hash</c>:
/// the procedure stamps the server's clock and computes the chain itself, inside one transaction, so
/// concurrent writers cannot fork it. The application never computes or supplies the chain (contract §3).
/// </para>
/// </remarks>
/// <param name="PublicId">The entry's own identifier, unique per row.</param>
/// <param name="CorrelationId">The caller's correlation, or one the seam generated when the caller named none.</param>
/// <param name="Level">The severity, in the one vocabulary the store's <c>level</c> column holds.</param>
/// <param name="EventAction">The operation under way; never blank, because the column is <c>NOT NULL</c>.</param>
/// <param name="Outcome">How the operation ended: <c>Success</c>, <c>Failure</c>, <c>Blocked</c> or <c>Retried</c>.</param>
/// <param name="ActorKind">
/// Who was acting: <c>user</c> for a signed-in person, <c>system</c> for a fault raised before sign-in or after
/// sign-out. A person is never named by their display name — the internal account identifier is what the store
/// holds.
/// </param>
/// <param name="ActorId">The internal account identifier of the person, or null when nobody was signed in.</param>
/// <param name="HostId">This machine's hostname.</param>
/// <param name="MacAddress">This machine's hardware address in the store's normalised form, when it could be read.</param>
/// <param name="Module">The part of the application the entry came from.</param>
/// <param name="ErrorType">The fault's type, or null for an entry raised without a fault.</param>
/// <param name="Message">What the entry says.</param>
/// <param name="ExceptionDetail">The serialized exception chain, or null for an entry raised without a fault.</param>
/// <param name="ErrorFingerprint">The grouping hash, or null for an entry raised without a fault.</param>
/// <param name="PayloadJson">The gathered context, as JSON.</param>
public sealed record StoreLogRecord(
    string PublicId,
    string CorrelationId,
    string Level,
    string EventAction,
    string Outcome,
    string? ActorKind,
    string? ActorId,
    string? HostId,
    string? MacAddress,
    string? Module,
    string? ErrorType,
    string Message,
    string? ExceptionDetail,
    string? ErrorFingerprint,
    string? PayloadJson);
