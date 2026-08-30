using CommunityToolkit.Mvvm.ComponentModel;

namespace MTM_Waitlist.Module_Shared.Models;

public sealed partial class WorkCenterSelectionItem : ObservableObject
{
    private const string DefaultWorkCenterImagePath = "Assets/Images/default-workstation-image.png";

    private bool _isSelected;

    public string WorkCenterName { get; init; } = string.Empty;

    public string ResolvedImagePath { get; init; } = DefaultWorkCenterImagePath;

    public string Building { get; init; } = string.Empty;

    /// <summary>
    /// UTC timestamp of the last setup activity or catalog update for this work center.
    /// Read from <c>setup_workstations_catalog.updated_utc</c>.
    /// </summary>
    public DateTime? LastUpdatedUtc { get; init; }

    public bool HasActiveJob { get; init; }

    public string CurrentWorkOrder { get; init; } = string.Empty;

    public string CurrentPartNumber { get; init; } = string.Empty;

    public string CurrentSequenceNumber { get; init; } = string.Empty;

    /// <summary>
    /// Whether this work center is the currently selected card in the selection grid.
    /// Drives the card's selection outline and blue photo frame.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    /// <summary>Value-only work order/sequence summary for the grouped card layout.</summary>
    public string CurrentJobSummary => string.IsNullOrWhiteSpace(CurrentWorkOrder)
        ? "None"
        : string.IsNullOrWhiteSpace(CurrentSequenceNumber)
            ? CurrentWorkOrder
            : $"{CurrentWorkOrder}/{CurrentSequenceNumber}";

    /// <summary>Value-only part number summary for the grouped card layout.</summary>
    public string CurrentPartSummary => string.IsNullOrWhiteSpace(CurrentPartNumber)
        ? "None"
        : CurrentPartNumber;

    /// <summary>Local date-and-time label for the "Last Updated" row.</summary>
    public string LastUpdatedDisplay => LastUpdatedUtc is null
        ? "Never"
        : LastUpdatedUtc.Value.ToLocalTime().ToString("MMM d, yyyy h:mm tt");
}
