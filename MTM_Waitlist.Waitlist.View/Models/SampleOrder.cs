using System.Collections.ObjectModel;

namespace MTM_Waitlist.Module_Waitlist.Models;

public sealed class SampleOrder
{
    public int Id { get; set; }

    /// <summary>
    /// When this row is a live waitlist request (not a static sample row), the underlying
    /// request's Guid. Used to target cancel-own and other request-scoped actions from the
    /// list/detail UI.
    /// </summary>
    public Guid? RequestId { get; set; }

    /// <summary>The underlying request's requester employee number (empty for static sample rows).</summary>
    public string RequesterEmployeeNumber { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RequestedByName { get; set; } = string.Empty;
    public string RequestedPressName { get; set; } = string.Empty;
    public string RemainingTimeText { get; set; } = string.Empty;
    public bool IsOverdue { get; set; }
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
                ? "Assets/Images/default-request-type.png"
                : $"Assets/{ImagePath}";

    public string EffectiveWorkCenterImagePath =>
        string.IsNullOrWhiteSpace(WorkCenterImagePath)
            ? "Assets/Images/default-workstation-image.png"
            : WorkCenterImagePath;

    /// <summary>
    /// Friendly lifecycle label shown as the card's status badge/pill. Maps the stored status
    /// (Pending/Accepted/Completed/Canceled) to Waiting/In Progress/Done/Cancelled; empty when
    /// the row is a static sample row with no lifecycle status.
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

    /// <summary>Human-friendly "how long this request has been waiting" text (from its created time).</summary>
    public string WaitingForText { get; set; } = string.Empty;

    public bool HasWaitingFor => !string.IsNullOrWhiteSpace(WaitingForText);

    public ObservableCollection<WaitlistField> Fields { get; } = new();
}

public sealed class WaitlistField
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? SecondaryValue { get; set; }
}
