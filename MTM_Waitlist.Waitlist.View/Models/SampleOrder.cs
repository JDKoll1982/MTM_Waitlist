using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MTM_Waitlist.Module_Waitlist.Models;

public sealed class SampleOrder : INotifyPropertyChanged
{
    public int Id { get; set; }

    /// <summary>
    /// When this row maps to a live waitlist request, the underlying request's Guid. Used to
    /// target cancel-own and other request-scoped actions from the list/detail UI.
    /// </summary>
    public Guid? RequestId { get; set; }

    /// <summary>The underlying request's requester employee number; empty when the row carries none.</summary>
    public string RequesterEmployeeNumber { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RequestedByName { get; set; } = string.Empty;
    public string RequestedPressName { get; set; } = string.Empty;
    /// <summary>
    /// The request's creation instant, kept so the waiting-age text can be recomputed as the clock
    /// advances instead of being frozen at the moment the row was mapped.
    /// </summary>
    public DateTimeOffset? RequestedUtc { get; set; }

    /// <summary>
    /// The request's due instant, kept so the remaining-time countdown can be recomputed on the same tick.
    /// </summary>
    public DateTimeOffset? TargetTimeUtc { get; set; }

    private string _remainingTimeText = string.Empty;

    /// <summary>
    /// Countdown text for the due time ("01:20"), or "Overdue"/"New". Re-evaluated every minute by
    /// <see cref="RefreshTimeDerivedText"/>.
    /// </summary>
    public string RemainingTimeText
    {
        get => _remainingTimeText;
        set => SetField(ref _remainingTimeText, value);
    }

    private bool _isOverdue;

    /// <summary>
    /// Whether the row is displayed as overdue: either the store's flag, or the clock having passed
    /// <see cref="TargetTimeUtc"/>.
    /// </summary>
    public bool IsOverdue
    {
        get => _isOverdue;
        set => SetField(ref _isOverdue, value);
    }

    /// <summary>
    /// The overdue flag as supplied by the request store, kept separate from <see cref="IsOverdue"/> so a
    /// later tick cannot clear a flag the store set.
    /// </summary>
    public bool IsOverdueAtSource { get; set; }

    public string ImagePath { get; set; } = string.Empty;
    public Guid? RequestTypeStableId { get; set; }
    public Guid? SubtypeStableId { get; set; }
    public long? WorkCenterCatalogId { get; set; }
    public string ResolvedImagePath { get; set; } = string.Empty;
    public string WorkCenterImagePath { get; set; } = string.Empty;
    public string EffectiveImagePath =>
        !string.IsNullOrWhiteSpace(ResolvedImagePath)
            ? ResolvedImagePath
            : string.IsNullOrWhiteSpace(ImagePath)
                ? "Assets/Placeholders/default-request-type.png"
                : $"Assets/{ImagePath}";

    public string EffectiveWorkCenterImagePath =>
        string.IsNullOrWhiteSpace(WorkCenterImagePath)
            ? "Assets/Placeholders/default-workstation-image.png"
            : WorkCenterImagePath;

    /// <summary>
    /// Friendly lifecycle label shown as the card's status badge/pill. Maps the stored status
    /// (Pending/Accepted/Completed/Canceled) to Waiting/In Progress/Done/Cancelled; empty when
    /// the row carries no recognised lifecycle status.
    /// </summary>
    public string StatusBadgeText =>
        Status.Trim().ToLowerInvariant() switch
        {
            "pending" => "Waiting",
            "accepted" => "In Progress",
            "completed" => "Done",
            "canceled" => "Cancelled",
            _ => string.Empty,
        };

    public bool HasStatusBadge => !string.IsNullOrWhiteSpace(StatusBadgeText);

    private string _waitingForText = string.Empty;

    /// <summary>
    /// Human-friendly "how long this request has been waiting" text (from its created time), for example
    /// "Waiting 35m". Re-evaluated every minute by <see cref="RefreshTimeDerivedText"/>.
    /// </summary>
    public string WaitingForText
    {
        get => _waitingForText;
        set
        {
            if (SetField(ref _waitingForText, value))
            {
                OnPropertyChanged(nameof(HasWaitingFor));
            }
        }
    }

    public bool HasWaitingFor => !string.IsNullOrWhiteSpace(WaitingForText);

    public ObservableCollection<WaitlistField> Fields { get; } = new();

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Recomputes the two time-derived texts (<see cref="WaitingForText"/> and
    /// <see cref="RemainingTimeText"/>) as of <paramref name="now"/>.
    /// </summary>
    /// <remarks>
    /// Called once a minute while the row is on screen. Change notifications are raised only for values
    /// that actually changed, so a tick that does not cross a minute boundary neither repaints the card
    /// nor reports a change. Rows that carry no timestamps are left untouched.
    /// </remarks>
    /// <param name="now">The instant to measure the row against.</param>
    /// <returns><see langword="true"/> when at least one displayed value changed.</returns>
    public bool RefreshTimeDerivedText(DateTimeOffset now)
    {
        var changed = false;

        if (RequestedUtc is DateTimeOffset requested)
        {
            var waiting = FormatWaitingAge(requested, now);
            if (!string.Equals(waiting, WaitingForText, StringComparison.Ordinal))
            {
                WaitingForText = waiting;
                changed = true;
            }
        }

        // The store's flag is sticky, and the clock can additionally make a row overdue while it is open.
        var overdue = IsOverdueAtSource || (TargetTimeUtc is DateTimeOffset target && target <= now);
        if (overdue != IsOverdue)
        {
            IsOverdue = overdue;
            changed = true;
        }

        if (TargetTimeUtc is DateTimeOffset due)
        {
            var remaining = FormatRemainingTime(due, overdue, now);
            if (!string.Equals(remaining, RemainingTimeText, StringComparison.Ordinal))
            {
                RemainingTimeText = remaining;
                changed = true;
            }
        }

        return changed;
    }

    /// <summary>
    /// Human-friendly waiting-age for a request, e.g. "Waiting 35m" / "Waiting 1h 20m" / "Waiting 2d",
    /// so leads can prioritize the oldest requests first.
    /// </summary>
    /// <param name="requestedUtc">The instant the request was created.</param>
    /// <param name="referenceUtc">The instant to measure against; defaults to now.</param>
    /// <returns>The formatted waiting-age text.</returns>
    public static string FormatWaitingAge(DateTimeOffset requestedUtc, DateTimeOffset? referenceUtc = null)
    {
        var reference = referenceUtc ?? DateTimeOffset.UtcNow;
        var elapsed = reference - requestedUtc;
        if (elapsed < TimeSpan.Zero)
        {
            elapsed = TimeSpan.Zero;
        }

        if (elapsed.TotalMinutes < 1)
        {
            return "Waiting < 1m";
        }

        if (elapsed.TotalHours < 1)
        {
            return $"Waiting {(int)elapsed.TotalMinutes}m";
        }

        if (elapsed.TotalDays < 1)
        {
            return $"Waiting {(int)elapsed.TotalHours}h {(int)(elapsed.TotalMinutes % 60)}m";
        }

        return $"Waiting {(int)elapsed.TotalDays}d";
    }

    /// <summary>
    /// Countdown text for a request's due time, e.g. "01:20", or "Overdue" once it has passed.
    /// </summary>
    /// <param name="targetTimeUtc">The due instant, or <see langword="null"/> when unset.</param>
    /// <param name="isOverdue">The store's overdue flag, which wins regardless of the clock.</param>
    /// <param name="referenceUtc">The instant to measure against; defaults to now.</param>
    /// <returns>The formatted remaining-time text.</returns>
    public static string FormatRemainingTime(DateTimeOffset? targetTimeUtc, bool isOverdue, DateTimeOffset? referenceUtc = null)
    {
        if (isOverdue)
        {
            return "Overdue";
        }

        if (targetTimeUtc is not DateTimeOffset target)
        {
            return "New";
        }

        var remaining = target - (referenceUtc ?? DateTimeOffset.UtcNow);
        if (remaining <= TimeSpan.Zero)
        {
            return "Overdue";
        }

        var totalMinutes = (int)Math.Ceiling(remaining.TotalMinutes);
        return totalMinutes <= 0 ? "Overdue" : $"{totalMinutes / 60:00}:{totalMinutes % 60:00}";
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged(string? propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
}

/// <summary>
/// One attribute row on a waitlist card's detail area. A slot with no value is not a row: the label goes
/// with the value, so nothing renders an empty labelled shell (FR-002).
/// </summary>
public sealed class WaitlistField : INotifyPropertyChanged
{
    private string _label = string.Empty;
    private string _value = string.Empty;

    /// <summary>The row's label, or empty for a padding slot.</summary>
    public string Label
    {
        get => _label;
        set => SetField(ref _label, value);
    }

    /// <summary>The row's value, or empty when the request carries no source for it.</summary>
    public string Value
    {
        get => _value;
        set
        {
            if (SetField(ref _value, value))
            {
                OnPropertyChanged(nameof(HasValue));
            }
        }
    }

    /// <summary>An optional second value for the row.</summary>
    public string? SecondaryValue { get; set; }

    /// <summary>
    /// Whether this slot carries a value. The card templates hide both the label and the value when it is
    /// false, and notify when <see cref="Value"/> changes so a row that gains or loses its source re-renders.
    /// </summary>
    public bool HasValue => !string.IsNullOrWhiteSpace(Value);

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged(string? propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
}
