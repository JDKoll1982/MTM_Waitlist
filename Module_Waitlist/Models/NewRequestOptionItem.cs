namespace MTM_Waitlist.Module_Waitlist.Models;

/// <summary>
/// A single selectable tile shown on the New Request wizard selection pages
/// (Job Type and Subtype). Replaces the dialog-era <c>JobTypeDialogItem</c>.
/// </summary>
public sealed class NewRequestOptionItem
{
    public string Name { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    /// <summary>Resolved image path (custom override from settings or default asset).</summary>
    public string ImagePath { get; set; } = string.Empty;
}
