namespace MTM_Waitlist.Mock.Service.Models;

/// <summary>
/// One backup artifact, as offered by the restore picker and the settings surface (FR-009, FR-010).
/// </summary>
public sealed record BackupArtifactRow
{
    /// <summary>The artifact's identity, used to confirm the exact artifact before a restore.</summary>
    public required Guid ArtifactId { get; init; }

    /// <summary>The store the artifact backs up.</summary>
    public required BackupStore Store { get; init; }

    /// <summary>The original artifact, so a confirmed restore uses the authoritative record.</summary>
    public required BackupArtifact Artifact { get; init; }

    /// <summary>Local creation time, for display.</summary>
    public required string CreatedText { get; init; }

    /// <summary>Size on disk, for display.</summary>
    public required string SizeText { get; init; }

    /// <summary>The full path, shown so the operator can verify which file they are about to restore.</summary>
    public required string FilePath { get; init; }

    /// <summary>Whether the artifact is still retained under the store's retention count.</summary>
    public required bool IsRetained { get; init; }

    /// <summary>Whether this artifact is a pre-restore safety snapshot.</summary>
    public required bool IsSafetySnapshot { get; init; }

    /// <summary>The label the picker shows.</summary>
    public required string DisplayText { get; init; }
}
