using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Module_Startup.Services;

/// <inheritdoc />
public sealed class PictureCopyHold : IPictureCopyHold
{
    private readonly object _gate = new();

    private TaskCompletionSource? _acknowledged;
    private bool _isHolding;

    /// <summary>
    /// Whether the person has already acknowledged this launch's statement. Acknowledging answers *this* launch:
    /// the hold comes back on the next launch that cannot refresh the copies, not on a retry that ends the same one.
    /// </summary>
    private bool _acknowledgedByThePerson;

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    public bool IsHolding
    {
        get
        {
            lock (_gate)
            {
                return _isHolding;
            }
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Asked of the line rather than of the step's own answer, because the runner is what writes the outcome: a
    /// best-effort step's failure is recorded as a failed line and then turned into a skip, so the line is the only
    /// place the refresh's failure survives the launch carrying on.
    /// </remarks>
    public bool HoldIfTheRefreshFailed(IReadOnlyList<LaunchFeedEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var failed = entries.Any(entry =>
            string.Equals(entry.StepId, PictureCacheStep.StepId, StringComparison.Ordinal)
            && (entry.Kind is LaunchFeedEntryKind.StepFailed || entry.Succeeded is false));

        if (failed is false)
        {
            return false;
        }

        lock (_gate)
        {
            if (_acknowledgedByThePerson)
            {
                // Already read on this launch: a retry that ends at the same surface does not ask again.
                return false;
            }

            _isHolding = true;
            _acknowledged = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        Changed?.Invoke(this, EventArgs.Empty);

        return true;
    }

    /// <inheritdoc />
    public void Acknowledge()
    {
        TaskCompletionSource? acknowledged;

        lock (_gate)
        {
            if (_isHolding is false)
            {
                return;
            }

            _isHolding = false;
            _acknowledgedByThePerson = true;
            acknowledged = _acknowledged;
            _acknowledged = null;
        }

        Changed?.Invoke(this, EventArgs.Empty);
        acknowledged?.TrySetResult();
    }

    /// <inheritdoc />
    public Task WaitAsync(CancellationToken cancellationToken = default)
    {
        TaskCompletionSource? acknowledged;

        lock (_gate)
        {
            acknowledged = _acknowledged;
        }

        return acknowledged is null
            ? Task.CompletedTask
            : acknowledged.Task.WaitAsync(cancellationToken);
    }
}
