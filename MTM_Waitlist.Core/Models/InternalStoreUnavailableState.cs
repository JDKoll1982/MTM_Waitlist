using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// One screen's <c>Unavailable</c> state for an internal store (FR-021, `data-model.md` §10).
/// </summary>
/// <remarks>
/// <para>
/// <b>Per screen, not a banner.</b> Each screen that reads an internal store owns one of these and shows it
/// in that screen's own surface, so a store problem is reported where the operator is working and clears as
/// soon as a read succeeds. Nothing about it is persisted, and there is no app-wide banner.
/// </para>
/// <para>
/// <b>Nothing is substituted.</b> When a store is unavailable the screen shows this state; it never shows
/// sample rows and never reads the <c>mtm_mock</c> cache (FR-001, FR-027).
/// </para>
/// <para>
/// <b>Localized by the screen.</b> This object exposes the neutral values
/// (<see cref="StoreName"/>, <see cref="LastAttemptUtc"/>, <see cref="RetryCount"/>,
/// <see cref="NextRetryUtc"/>, <see cref="Message"/>) plus <see cref="RetryCommand"/>. The screen composes
/// the sentence from its own localized labels, so no user-facing English is produced here
/// (constitution V).
/// </para>
/// </remarks>
public sealed partial class InternalStoreUnavailableState : ObservableObject, IDisposable
{
    private readonly MySqlDatabaseTarget _store;
    private readonly Func<CancellationToken, Task> _retry;
    private readonly IStoreAvailabilityTracker? _tracker;
    private bool _disposed;

    /// <summary>Creates the state.</summary>
    /// <param name="store">The internal store this screen reads.</param>
    /// <param name="retry">Re-runs this screen's load; the manual retry action.</param>
    /// <param name="tracker">The availability tracker the seam records into, when one is installed.</param>
    public InternalStoreUnavailableState(
        MySqlDatabaseTarget store,
        Func<CancellationToken, Task> retry,
        IStoreAvailabilityTracker? tracker = null)
    {
        ArgumentNullException.ThrowIfNull(retry);
        _store = store;
        _retry = retry;
        _tracker = tracker;

        if (_tracker is not null)
        {
            _tracker.Changed += OnAvailabilityChanged;
            Report(_tracker.Get(_store));
        }
    }

    /// <summary>Whether the screen should show its unavailable state.</summary>
    [ObservableProperty]
    public partial bool IsUnavailable { get; private set; }

    /// <summary>The store's database name, for the operator-facing message.</summary>
    [ObservableProperty]
    public partial string StoreName { get; private set; } = string.Empty;

    /// <summary>When the last attempt started.</summary>
    [ObservableProperty]
    public partial DateTime? LastAttemptUtc { get; private set; }

    /// <summary>How many retries the bounded policy consumed on the failing read.</summary>
    [ObservableProperty]
    public partial int RetryCount { get; private set; }

    /// <summary>When the next attempt is worth making, as guidance for the manual retry.</summary>
    [ObservableProperty]
    public partial DateTime? NextRetryUtc { get; private set; }

    /// <summary>The operator-facing explanation recorded by the seam.</summary>
    [ObservableProperty]
    public partial string Message { get; private set; } = string.Empty;

    /// <summary>Whether a manual retry is in flight, so the screen can disable its own button.</summary>
    [ObservableProperty]
    public partial bool IsRetrying { get; private set; }

    /// <summary>Re-runs this screen's load. Bound by the screen as the manual retry action.</summary>
    [RelayCommand]
    private async Task RetryAsync(CancellationToken cancellationToken)
    {
        if (IsRetrying)
        {
            return;
        }

        IsRetrying = true;
        try
        {
            await _retry(cancellationToken).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // A retry that fails again is already recorded by the seam; the screen must not crash on top of a
            // store problem it is in the middle of reporting.
            StartupDebugLog.Error(nameof(InternalStoreUnavailableState), ex, $"Manual retry failed for store '{StoreName}'.");
        }
        finally
        {
            IsRetrying = false;
            Report(_tracker?.Get(_store) ?? InternalStoreAvailability.Unknown(_store));
        }
    }

    private void OnAvailabilityChanged(object? sender, InternalStoreAvailability availability)
    {
        if (availability.Store == _store)
        {
            Report(availability);
        }
    }

    private void Report(InternalStoreAvailability availability)
    {
        IsUnavailable = availability.Status == InternalStoreStatus.Unavailable;
        StoreName = availability.StoreName;
        LastAttemptUtc = availability.LastAttemptUtc;
        RetryCount = availability.RetryCount;
        NextRetryUtc = availability.NextRetryUtc;
        Message = availability.Message;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_tracker is not null)
        {
            _tracker.Changed -= OnAvailabilityChanged;
        }

        _disposed = true;
    }
}
