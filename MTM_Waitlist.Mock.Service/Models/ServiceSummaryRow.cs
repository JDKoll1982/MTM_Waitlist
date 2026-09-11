namespace MTM_Waitlist.Mock.Service.Models;

/// <summary>
/// One row of the service summary on the status surface: what the value is, a note explaining it, and the
/// value itself.
/// </summary>
/// <remarks>
/// Pre-formatted for display so the page carries no formatting logic and no localized literal is
/// assembled in XAML (constitution V). The note is the point of the row: "Started" alone does not tell
/// an operator what they are looking at.
/// </remarks>
public sealed record ServiceSummaryRow
{
    /// <summary>What the row reports, for example "Refresh schedule".</summary>
    public required string LabelText { get; init; }

    /// <summary>A one-line note explaining what the value means.</summary>
    public required string DescriptionText { get; init; }

    /// <summary>The value itself, already formatted for display.</summary>
    public required string ValueText { get; init; }
}
