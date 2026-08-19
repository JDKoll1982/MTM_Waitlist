namespace MTM_Waitlist.Module_Waitlist.Views;

public sealed class JobTypeDialogItem
{
    public string RequestType { get; set; } = string.Empty;

    public string SubtypeSummary { get; set; } = string.Empty;

    /// <summary>Resolved request-type image path (custom override from settings or default asset).</summary>
    public string ImagePath { get; set; } = string.Empty;
}
