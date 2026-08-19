using System.Collections.ObjectModel;

namespace MTM_Waitlist.Module_Waitlist.Models;

public sealed class SampleOrder
{
    public int Id { get; set; }
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
    public ObservableCollection<WaitlistField> Fields { get; } = new();
}

public sealed class WaitlistField
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? SecondaryValue { get; set; }
}
