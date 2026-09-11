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

    /// <summary>Friendly name for the shape, localised, for the status list.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Whether an operator has the shape enabled.</summary>
    public required bool IsEnabled { get; init; }

    /// <summary>Whether the shape failed startup validation and is excluded from refresh.</summary>
    public bool HasValidationError => !string.IsNullOrWhiteSpace(ValidationErrorText);

    /// <summary>Local time of the last attempt, or the "never" text.</summary>
    public required string LastRunText { get; init; }

    /// <summary>Label for <see cref="LastRunText"/>.</summary>
    public required string LastRunLabelText { get; init; }

    /// <summary>The last outcome, as the API reports it.</summary>
    public required string OutcomeText { get; init; }

    /// <summary>Rows loaded by the last successful refresh.</summary>
    public required string RowCountText { get; init; }

    /// <summary>Label for <see cref="RowCountText"/>.</summary>
    public required string RowsLabelText { get; init; }

    /// <summary>Local time the mirror was last refreshed, or the seed-content statement.</summary>
    public required string FreshnessText { get; init; }

    /// <summary>Label for <see cref="FreshnessText"/>.</summary>
    public required string FreshnessLabelText { get; init; }

    /// <summary>Heading for the exclusion-reason block.</summary>
    public required string ValidationErrorLabelText { get; init; }

    /// <summary>Why the shape is excluded, when startup validation rejected it.</summary>
    public string? ValidationErrorText { get; init; }
}
