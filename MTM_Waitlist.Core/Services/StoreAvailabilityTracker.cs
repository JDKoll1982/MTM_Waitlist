using System.Collections.Concurrent;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Services;

/// <inheritdoc cref="IStoreAvailabilityTracker"/>
/// <remarks>
/// In-memory and session-scoped on purpose: availability is a live observation, not persisted state, and it
/// must never survive a restart as a stale claim about a store that is now healthy. Reads and writes are
/// thread-safe because the seam is called from background continuations as well as the UI thread.
/// </remarks>
public sealed class StoreAvailabilityTracker : IStoreAvailabilityTracker
{
    private readonly ConcurrentDictionary<MySqlDatabaseTarget, InternalStoreAvailability> _states = new();

    /// <inheritdoc />
    public event EventHandler<InternalStoreAvailability>? Changed;

    /// <inheritdoc />
    public InternalStoreAvailability Get(MySqlDatabaseTarget store) =>
        _states.TryGetValue(store, out var availability)
            ? availability
            : InternalStoreAvailability.Unknown(store);

    /// <inheritdoc />
    public IReadOnlyList<InternalStoreAvailability> GetAll() =>
        _states.Values
            .OrderBy(availability => availability.Store)
            .ToArray();

    /// <inheritdoc />
    public void RecordAvailable(MySqlDatabaseTarget store, DateTime attemptedUtc, int attemptCount)
    {
        Publish(new InternalStoreAvailability
        {
            Store = store,
            Status = InternalStoreStatus.Available,
            LastAttemptUtc = attemptedUtc,
            RetryCount = Math.Max(0, attemptCount - 1),
            Message = string.Empty,
        });
    }

    /// <inheritdoc />
    public void RecordUnavailable(
        MySqlDatabaseTarget store,
        DateTime attemptedUtc,
        int attemptCount,
        DateTime? nextRetryUtc,
        string message)
    {
        Publish(new InternalStoreAvailability
        {
            Store = store,
            Status = InternalStoreStatus.Unavailable,
            LastAttemptUtc = attemptedUtc,
            RetryCount = Math.Max(0, attemptCount - 1),
            NextRetryUtc = nextRetryUtc,
            Message = message,
        });
    }

    private void Publish(InternalStoreAvailability availability)
    {
        var previous = Get(availability.Store);
        _states[availability.Store] = availability;

        // A screen is told when a store's state changes, and again on every failed read so a manual retry that
        // fails a second time refreshes the timestamps and the retry count it is showing. A healthy read is
        // reported once, so a busy screen does not raise a notification storm.
        var isChange = previous.Status != availability.Status;
        if (!isChange && availability.Status != InternalStoreStatus.Unavailable)
        {
            return;
        }

        if (isChange)
        {
            StartupDebugLog.Info(
                nameof(StoreAvailabilityTracker),
                $"Store '{availability.StoreName}' is now {availability.Status}.");
        }

        Changed?.Invoke(this, availability);
    }
}
