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
    public string ImageUri => string.IsNullOrWhiteSpace(ImagePath) ? "ms-appx:///Assets/coil.png" : $"ms-appx:///Assets/{ImagePath}";
    public ObservableCollection<WaitlistField> Fields { get; } = new();
}

public sealed class WaitlistField
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? SecondaryValue { get; set; }
}
