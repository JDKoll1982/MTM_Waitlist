namespace MTM_Waitlist.Mock.Service.Models;

/// <summary>
/// The recorded result of one refresh attempt for one read shape (FR-008, FR-013).
/// </summary>
/// <remarks>
/// Persisted in a <b>service-local durable</b> store, deliberately not in MySQL: a restore replaces
/// an entire store, so operational history kept inside a store would be rewound by the very
/// operation it needs to describe, and refresh status must stay reportable while Infor Visual is
/// down (data-model.md §4).
/// </remarks>
public sealed record RefreshRunRecord
{
    /// <summary>Unique identifier of this run.</summary>
    public Guid RunId { get; init; } = Guid.NewGuid();

    /// <summary>The <see cref="MTM_Waitlist.Mock.Models.VisualReadShape.Key"/> this run belongs to.</summary>
    public required string ShapeKey { get; init; }

    /// <summary>UTC time the run started.</summary>
    public required DateTime StartedUtc { get; init; }

    /// <summary>UTC time the run finished; <see langword="null"/> while still running.</summary>
    public DateTime? FinishedUtc { get; init; }

    /// <summary>The run outcome.</summary>
    public RefreshRunOutcome Outcome { get; init; } = RefreshRunOutcome.Pending;

    /// <summary>Rows loaded into the stage twin on success.</summary>
    public int? RowCount { get; init; }

    /// <summary>Wall-clock duration of the run.</summary>
    public long DurationMs { get; init; }

    /// <summary>
    /// Sanitized error text. Must never contain a credential (FR-026).
    /// </summary>
    public string? ErrorMessage { get; init; }
}
