namespace MTM_Waitlist.Mock.Service.Models;

/// <summary>
/// One read shape's row on the service status surface (FR-013).
/// </summary>
/// <remarks>
/// Pre-formatted for display so the page carries no formatting logic and no localized literal is
/// assembled in XAML (constitution V).
/// </remarks>
public sealed record ShapeStatusRow
{
    /// <summary>The shape's catalog key.</summary>
    public required string ShapeKey { get; init; }

    /// <summary>Whether the operator has the shape enabled.</summary>
    public required bool IsEnabled { get; init; }

    /// <summary>Local time of the last attempt, or the "never" text.</summary>
    public required string LastRunText { get; init; }

    /// <summary>The last outcome, as the API reports it.</summary>
    public required string OutcomeText { get; init; }

    /// <summary>Rows loaded by the last successful refresh.</summary>
    public required string RowCountText { get; init; }

    /// <summary>Local time the mirror was last refreshed, or the seed-content statement.</summary>
    public required string FreshnessText { get; init; }

    /// <summary>Why the shape is excluded, when startup validation rejected it.</summary>
    public string? ValidationErrorText { get; init; }
}
