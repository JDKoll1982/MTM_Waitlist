using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Shared.Helpers;

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

    /// <summary>
    /// The request's recorded assignee, or null while it is still available. Drives the Complete/Release
    /// gate, so a row built from the store's own answer is what decides who may finish the work.
    /// </summary>
    public string? AssignedMaterialHandler { get; set; }

    /// <summary>The request's short handler note, or null when it has none.</summary>
    public string? Note { get; set; }

    /// <summary>
    /// Card <b>Line 1</b> — the Item's umbrella phrase: the Category's own word, or the Item's own phrase where
    /// the Item defines one. Read from the Item catalog, never from a stored request type (FR-005).
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Card <b>Line 2</b> — the Item's identifier: a fixed value, a value read from the job, or the value the
    /// flow captured. Resolved from the Item's own template; an unresolvable token renders the Item's display
    /// name and reports the problem through <see cref="Line2Problem"/> (FR-005, FR-026).
    /// </summary>
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

    /// <summary>
    /// The Item code the request asked for (FR-004). The row's identity: the picture, the request page's
    /// sections and the sort all resolve from it, so no reader has to be told what the request "really" was
    /// through a second, derived pair.
    /// </summary>
    public string ItemCode { get; set; } = string.Empty;

    /// <summary>
    /// The plain-language report of an Item whose identifier could not be resolved (FR-026), or null when the
    /// card's second line resolved cleanly. Never blank in place of a value: a card that cannot show the
    /// identifier says why instead of silently showing nothing.
    /// </summary>
    public string? Line2Problem { get; set; }

    /// <summary>True when the Item's identifier could not be resolved and <see cref="Line2Problem"/> says why.</summary>
    public bool HasLine2Problem => !string.IsNullOrWhiteSpace(Line2Problem);

    public long? WorkCenterCatalogId { get; set; }
    public string ResolvedImagePath { get; set; } = string.Empty;
    public string WorkCenterImagePath { get; set; } = string.Empty;

    /// <summary>
    /// The picture the card draws: the picture configured for this request's Item, and the application's
    /// no-image placeholder when none is configured.
    /// </summary>
    /// <remarks>
    /// There is no built-in artwork to fall back to. An Item's picture is a setting, so a request for an Item
    /// nobody has given a picture to says so — with the placeholder — rather than borrowing a picture of some
    /// other thing. The converter applies the picture rule as well, so a path that names a missing, unreadable
    /// or empty file lands on the placeholder too.
    /// </remarks>
    public string EffectiveImagePath =>
        string.IsNullOrWhiteSpace(ResolvedImagePath)
            ? ImagePicturePolicy.NoImagePath
            : ResolvedImagePath;

    /// <summary>The work centre's picture, or the application's no-image placeholder when none resolved.</summary>
    public string EffectiveWorkCenterImagePath =>
        string.IsNullOrWhiteSpace(WorkCenterImagePath)
            ? ImagePicturePolicy.NoImagePath
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

    /// <summary>
    /// The urgency state this row was ordered by on the list, or null when no due value could be derived.
    /// Held on the row so the card's "Remaining time" and the list order can never come from two different
    /// numbers, and so the ordering helper is the only place the ordering rule lives.
    /// </summary>
    public UrgencyState? Urgency { get; set; }

    /// <summary>
    /// Whether the current viewer is offered Accept: a handler-or-above on an available, unfinished request.
    /// A view of <c>RequestActionPolicy.CanViewerAccept</c>, never a wider rule than the service enforces.
    /// </summary>
    public bool CanAccept { get; set; }

    /// <summary>
    /// Whether the current viewer is offered Complete and Release: they are the request's recorded assignee.
    /// A view of <c>RequestActionPolicy.CanViewerCompleteOrRelease</c>.
    /// </summary>
    public bool CanCompleteOrRelease { get; set; }

    /// <summary>
    /// Whether the current viewer is offered Cancel: their own request while it is still waiting, or a request
    /// they have claimed and can no longer work. The card shows it whenever the Complete button is live, so the
    /// two appear together.
    /// </summary>
    public bool CanCancelRequest { get; set; }

    /// <summary>
    /// Whether a message arrived on this request since the viewer last opened its details — the card's
    /// new-message indicator. Derived from the newest human-authored message against the viewer's last-seen time.
    /// </summary>
    public bool HasNewMessages { get; set; }

    /// <summary>Localized tooltip for the new-message indicator; empty when there is nothing to announce.</summary>
    public string NewMessageTooltip { get; set; } = string.Empty;

    /// <summary>The list's Accept command, carried to the card through this row. Null when the gate is false.</summary>
    public ICommand? AcceptCommand { get; set; }

    /// <summary>The list's Complete command, carried to the card through this row. Null when the gate is false.</summary>
    public ICommand? CompleteCommand { get; set; }

    /// <summary>
    /// The list's Give back command, carried to the card through this row. Null when the gate is false. It is the
    /// same gate as Complete: handing a claimed request back is the assignee's other option, so the two are
    /// offered together and never to anybody else (FR-047).
    /// </summary>
    public ICommand? ReleaseCommand { get; set; }

    /// <summary>The list's Cancel command, carried to the card through this row. Null when the gate is false.</summary>
    public ICommand? CancelCommand { get; set; }

    /// <summary>
    /// When the newest message written by a person was added — a note, not a lifecycle change. This is the
    /// cheap "somebody said something" signal behind the new-message indicator, so the list does not need a
    /// history read for every row. Null means no person has written on the request, which never raises the
    /// indicator: job created, accepted, completed and canceled are system events, not messages.
    /// </summary>
    public DateTimeOffset? LastMessageUtc { get; set; }

    /// <summary>Localized accessible name for the Accept icon button; empty when the action is not offered.</summary>
    public string AcceptActionText { get; set; } = string.Empty;

    /// <summary>Localized accessible name for the Complete icon button; empty when the action is not offered.</summary>
    public string CompleteActionText { get; set; } = string.Empty;

    /// <summary>Localized accessible name for the Give back icon button; empty when the action is not offered.</summary>
    public string ReleaseActionText { get; set; } = string.Empty;

    /// <summary>Localized accessible name for the Cancel icon button; empty when the action is not offered.</summary>
    public string CancelActionText { get; set; } = string.Empty;

    /// <summary>
    /// The number of buttons this row offers — the one number the card's action area can be checked against, so
    /// a gate flag with no control and a control with no gate are both visible at once.
    /// </summary>
    /// <remarks>
    /// The card draws [primary][Give back][Cancel]: the primary is Accept while the request is available and
    /// becomes Complete once the viewer has claimed it — so Accept and Complete are never both live — and Complete
    /// and Give back are both live exactly when the viewer is the assignee, which is why the claimed state
    /// contributes two. Cancel accompanies a claim.
    /// </remarks>
    public int OfferedActionCount =>
        (CanAccept ? 1 : 0) + (CanCompleteOrRelease ? 2 : 0) + (CanCancelRequest ? 1 : 0);

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
