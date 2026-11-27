using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Data-access surface of <see cref="MySqlHelperServer"/>.
/// Exists so services that execute SQL can be unit tested without a live database.
/// </summary>
/// <remarks>
/// <para>
/// Every operation here is a stored-procedure call. There is no member that executes caller-supplied statement
/// text: the one reviewed route that runs a checked-in queue script is
/// <see cref="IReviewedQueueScriptReader"/>, which takes a name from the reviewed set and nothing else.
/// </para>
/// <para>
/// <b>Two postures, deliberately kept apart.</b> The two <c>ExecuteStoredProcedure*Async</c> members are the
/// <i>lenient</i> route: they retry a bounded number of times, record the store's availability, and answer a
/// failure with a neutral result (no rows, no affected rows), which is what a screen that shows its own
/// unavailable state needs. <see cref="ExecuteStrictAsync{T}"/> is the <i>strict</i> route: the provider's own
/// exception reaches the caller unchanged, nothing is retried and no availability state is recorded, so a caller
/// that has to read the provider's answer — a duplicate key, a signalled refusal, a missing procedure — still
/// gets it. A behaviour the seam cannot carry is not dropped at the caller; it is added here.
/// </para>
/// </remarks>
public interface IMySqlHelperServer
{
    Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
        string storedProcedureName,
        IReadOnlyDictionary<string, object?> parameters,
        MySqlDatabaseTarget databaseTarget,
        CancellationToken cancellationToken = default);

    Task<int> ExecuteStoredProcedureNonQueryAsync(
        string storedProcedureName,
        IReadOnlyDictionary<string, object?> parameters,
        MySqlDatabaseTarget databaseTarget,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens one connection to <paramref name="request"/>'s store, hands <paramref name="operation"/> a scope
    /// bound to it, and — when the request asks for one — carries every call the scope makes inside a single
    /// transaction that is committed on success and rolled back on any exception.
    /// </summary>
    /// <typeparam name="T">What <paramref name="operation"/> answers.</typeparam>
    /// <param name="request">The store, and whether the calls are one unit.</param>
    /// <param name="operation">The stored-procedure calls to make, in order.</param>
    /// <param name="cancellationToken">A token to cancel the connection open, the calls and the commit.</param>
    /// <returns>What <paramref name="operation"/> answered, after a successful commit.</returns>
    /// <exception cref="MySqlStoreNotConfiguredException">No connection is configured for the store.</exception>
    /// <remarks>
    /// <para>
    /// <b>Nothing is swallowed.</b> A provider exception — a duplicate key, a <c>SIGNAL SQLSTATE '45000'</c>
    /// refusal, a missing procedure or table, a dropped connection — leaves this method as it was raised, so a
    /// caller keeps its own <c>MySqlException</c> handling instead of re-deriving the reason from a neutral answer.
    /// </para>
    /// <para>
    /// <b>Nothing is retried, and that is why this is a separate route.</b> These callers are writes and decisions
    /// a person asked for: a retried write is how one press becomes two rows, and a retried transaction would
    /// replay half of a unit that had already partly applied. The lenient route's bounded retry stays where it
    /// belongs, on reads a screen can show as unavailable.
    /// </para>
    /// <para>
    /// <b>Nothing is recorded as an outage.</b> Availability is the lenient route's side channel to the screens; a
    /// strict caller reports its own failure to its own caller, so a store observation recorded here would claim
    /// an outage on behalf of a service that never asked for one.
    /// </para>
    /// <para>
    /// The default implementation refuses, so a test double handed to a strict caller fails loudly rather than
    /// answering a neutral value. <see cref="MySqlHelperServer"/> is the only type that overrides it.
    /// </para>
    /// </remarks>
    Task<T> ExecuteStrictAsync<T>(
        MySqlStrictScopeRequest request,
        Func<IMySqlStrictScope, CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException(
            $"{nameof(IMySqlHelperServer)}.{nameof(ExecuteStrictAsync)} is implemented by the real seam only. "
            + "A test double handed to a strict caller must implement it.");
}

/// <summary>
/// One strict call: which store, and whether the scope's calls are a single unit.
/// </summary>
/// <param name="Target">The store the scope is bound to.</param>
/// <param name="UseTransaction">
/// Whether the scope's calls run inside one transaction, committed when the operation returns and rolled back on
/// any exception. <see langword="false" /> keeps every call as its own implicit unit, which is all a single
/// statement needs.
/// </param>
/// <param name="ConnectionTimeoutSeconds">
/// The connect timeout to apply over the connection string's own, or <see langword="null" /> to leave the
/// string's value alone. A caller that already configured its own timeout passes it here instead of losing it.
/// </param>
/// <param name="CommandTimeoutSeconds">
/// The per-statement timeout, or <see langword="null" /> for the seam's own default. Passed explicitly so a
/// caller that overrode it keeps the value it had before it moved onto the seam.
/// </param>
public sealed record MySqlStrictScopeRequest(
    MySqlDatabaseTarget Target,
    bool UseTransaction = false,
    int? ConnectionTimeoutSeconds = null,
    int? CommandTimeoutSeconds = null);

/// <summary>
/// The stored-procedure calls one strict connection carries, already bound to that connection and to its
/// transaction when the request asked for one.
/// </summary>
/// <remarks>
/// There is no member that takes statement text, for the same reason the seam itself has none: every call names a
/// procedure. The scope is valid only for the duration of the operation it was handed to; after that the
/// connection is closed and its transaction committed or rolled back.
/// </remarks>
public interface IMySqlStrictScope
{
    /// <summary>Runs one read and answers its rows, addressed by column name.</summary>
    /// <param name="storedProcedureName">The procedure to call.</param>
    /// <param name="parameters">Its parameters, with or without a leading <c>@</c>.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The rows read, bounded by the seam's shared row cap.</returns>
    Task<IReadOnlyList<Dictionary<string, object?>>> QueryAsync(
        string storedProcedureName,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default);

    /// <summary>Runs one write and answers the affected-row count.</summary>
    /// <param name="storedProcedureName">The procedure to call.</param>
    /// <param name="parameters">Its parameters, with or without a leading <c>@</c>.</param>
    /// <param name="cancellationToken">A token to cancel the write.</param>
    /// <returns>The number of rows the call affected.</returns>
    Task<int> NonQueryAsync(
        string storedProcedureName,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs one call and answers the first column of its first row, which is how a procedure that reports
    /// <c>affected_rows</c> says a lost race wrote nothing rather than reporting a false success.
    /// </summary>
    /// <param name="storedProcedureName">The procedure to call.</param>
    /// <param name="parameters">Its parameters, with or without a leading <c>@</c>.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The value answered, or <see langword="null" /> when the procedure answered no value.</returns>
    Task<object?> ScalarAsync(
        string storedProcedureName,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// No connection is configured for the store a strict call named.
/// </summary>
/// <remarks>
/// The lenient route answers an unconfigured store with its neutral result, because a store that was never
/// configured is not an unavailable one. The strict route cannot do that — a caller that reads the provider's own
/// answer has nothing to read — so it says the one thing it knows. It derives from
/// <see cref="InvalidOperationException"/> because that is what the callers on this route already threw for this
/// condition, and they keep that throw.
/// </remarks>
public sealed class MySqlStoreNotConfiguredException : InvalidOperationException
{
    /// <summary>Creates the exception for one store.</summary>
    /// <param name="databaseName">The database name that has no configured connection.</param>
    public MySqlStoreNotConfiguredException(string databaseName)
        : base($"No connection is configured for the {databaseName} store.")
        => DatabaseName = databaseName;

    /// <summary>The database name that has no configured connection.</summary>
    public string DatabaseName { get; }
}
