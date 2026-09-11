using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Tracks the last observed availability of the internal MySQL stores and reports changes, so a screen can
/// show its own <c>Unavailable</c> state without polling (FR-021).
/// </summary>
/// <remarks>
/// <para>
/// The data-access seam (<see cref="MySqlHelperServer"/>) is the only writer: it records the outcome of every
/// read after its bounded retries. A screen reads <see cref="Get"/> and subscribes to <see cref="Changed"/>.
/// </para>
/// <para>
/// This tracker is about internal stores only. It says nothing about Infor Visual reachability — that is
/// <c>IVisualReachabilityDetector</c> in <c>MTM_Waitlist.Mock</c>, which drives the cached-data indicator.
/// </para>
/// </remarks>
public interface IStoreAvailabilityTracker
{
    /// <summary>Raised whenever an observation changes a store's availability.</summary>
    event EventHandler<InternalStoreAvailability>? Changed;

    /// <summary>Returns the last observation for one store; <see cref="InternalStoreStatus.Unknown"/> before it is read.</summary>
    /// <param name="store">The store to report on.</param>
    InternalStoreAvailability Get(MySqlDatabaseTarget store);

    /// <summary>Returns the last observation for every store that has been read.</summary>
    IReadOnlyList<InternalStoreAvailability> GetAll();

    /// <summary>Records that a read of the store succeeded.</summary>
    /// <param name="store">The store that answered.</param>
    /// <param name="attemptedUtc">When the successful attempt started.</param>
    /// <param name="attemptCount">How many attempts the read consumed (1 when it worked first time).</param>
    void RecordAvailable(MySqlDatabaseTarget store, DateTime attemptedUtc, int attemptCount);

    /// <summary>Records that a read failed after every bounded retry.</summary>
    /// <param name="store">The store that did not answer.</param>
    /// <param name="attemptedUtc">When the final attempt started.</param>
    /// <param name="attemptCount">How many attempts were consumed.</param>
    /// <param name="nextRetryUtc">When the next attempt is worth making, for the manual retry guidance.</param>
    /// <param name="message">Operator-facing explanation.</param>
    void RecordUnavailable(MySqlDatabaseTarget store, DateTime attemptedUtc, int attemptCount, DateTime? nextRetryUtc, string message);
}
