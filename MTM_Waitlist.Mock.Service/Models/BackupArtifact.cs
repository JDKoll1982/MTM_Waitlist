namespace MTM_Waitlist.Mock.Service.Models;

/// <summary>
/// One produced backup file (FR-009, SC-008).
/// </summary>
/// <remarks>
/// An artifact is recorded as successful <b>only</b> when the <c>mysqldump</c> process exited 0
/// <b>and</b> the file exists with a non-zero size. A missing tool records no artifact at all
/// (FR-013).
/// </remarks>
public sealed record BackupArtifact
{
    /// <summary>Unique identifier of this artifact.</summary>
    public Guid ArtifactId { get; init; } = Guid.NewGuid();

    /// <summary>The store this artifact backs up.</summary>
    public required BackupStore Store { get; init; }

    /// <summary>UTC time the artifact was created.</summary>
    public required DateTime CreatedUtc { get; init; }

    /// <summary>Absolute path of the <c>.sql</c> dump produced with <c>--result-file</c>.</summary>
    public required string FilePath { get; init; }

    /// <summary>Artifact size in bytes. Zero means failure, never a success record.</summary>
    public required long SizeBytes { get; init; }

    /// <summary>Whether this artifact is still retained under the store's <c>RetentionCount</c>.</summary>
    public bool IsRetained { get; init; } = true;

    /// <summary>Whether this is the pre-restore safety snapshot taken by the restore path.</summary>
    public bool IsSafetySnapshot { get; init; }
}
