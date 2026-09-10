namespace MTM_Waitlist.Mock.Service.Models;

/// <summary>
/// The recorded result of an emergency restore request (FR-010, FR-023, SC-009).
/// </summary>
/// <remarks>
/// This type is produced only by a local UI action inside the service process. No contract in
/// <c>contracts/</c> exposes it over the network — restore is host-only (FR-023).
/// </remarks>
public sealed record RestoreOutcome
{
    /// <summary>The store that was targeted.</summary>
    public required BackupStore Store { get; init; }

    /// <summary>The chosen backup artifact.</summary>
    public required Guid ArtifactId { get; init; }

    /// <summary>
    /// The safety snapshot taken before the destructive step. This is the recovery path when a
    /// restore fails partway, so a failure outcome names it (FR-010).
    /// </summary>
    public Guid? SafetySnapshotArtifactId { get; init; }

    /// <summary>UTC time the operator requested the restore.</summary>
    public required DateTime RequestedUtc { get; init; }

    /// <summary>UTC time the operator confirmed; <see langword="null"/> when they declined.</summary>
    public DateTime? ConfirmedUtc { get; init; }

    /// <summary>UTC time the restore finished.</summary>
    public DateTime? FinishedUtc { get; init; }

    /// <summary>The restore outcome.</summary>
    public RestoreOutcomeKind Outcome { get; init; } = RestoreOutcomeKind.NotConfirmed;

    /// <summary>Row counts of the store's core tables compared before and after.</summary>
    public string? VerificationSummary { get; init; }
}
