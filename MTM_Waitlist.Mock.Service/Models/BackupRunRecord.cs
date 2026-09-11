namespace MTM_Waitlist.Mock.Service.Models;

/// <summary>
/// The most recent backup attempt for one store (FR-009, FR-013, SC-008).
/// </summary>
/// <remarks>
/// Kept service-local and out of MySQL for the same reason as <see cref="RefreshRunRecord"/>: a restore
/// replaces an entire store, so operational history inside a store would be rewound by the operation it
/// has to describe (data-model.md §4/§6).
/// </remarks>
public sealed record BackupRunRecord
{
    /// <summary>The store this attempt targeted.</summary>
    public required BackupStore Store { get; init; }

    /// <summary>UTC time the attempt started.</summary>
    public required DateTime StartedUtc { get; init; }

    /// <summary>UTC time the attempt finished.</summary>
    public DateTime? FinishedUtc { get; init; }

    /// <summary>How the attempt ended.</summary>
    public BackupRunOutcome Outcome { get; init; } = BackupRunOutcome.Never;

    /// <summary>Absolute path of the artifact produced, when one was produced.</summary>
    public string? ArtifactPath { get; init; }

    /// <summary>Whether the artifact is a pre-restore safety snapshot.</summary>
    public bool IsSafetySnapshot { get; init; }

    /// <summary>Secret-free failure detail.</summary>
    public string? ErrorMessage { get; init; }
}
