namespace MTM_Waitlist.Mock.Service.Models;

/// <summary>
/// One store's row on the service status surface (FR-013, SC-008).
/// </summary>
public sealed record BackupStatusRow
{
    /// <summary>The store's database name.</summary>
    public required string Store { get; init; }

    /// <summary>Whether scheduled backups are enabled for this store.</summary>
    public required bool IsEnabled { get; init; }

    /// <summary>Local time of the last attempt, or the "never" text.</summary>
    public required string LastRunText { get; init; }

    /// <summary>The last outcome, as the API reports it.</summary>
    public required string OutcomeText { get; init; }

    /// <summary>How many artifacts the store currently retains.</summary>
    public required string ArtifactCountText { get; init; }

    /// <summary>Path of the most recent artifact, when one exists.</summary>
    public required string ArtifactPathText { get; init; }

    /// <summary>The scheduled local time for this store.</summary>
    public required string ScheduleText { get; init; }
}
