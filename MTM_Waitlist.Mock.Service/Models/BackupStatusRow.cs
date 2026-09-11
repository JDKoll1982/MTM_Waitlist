namespace MTM_Waitlist.Mock.Service.Models;

/// <summary>
/// One store's row on the service status surface (FR-013, SC-008).
/// </summary>
public sealed record BackupStatusRow
{
    /// <summary>The store's database name.</summary>
    public required string Store { get; init; }

    /// <summary>Friendly name for the store, localised, for the status list.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Whether scheduled backups are enabled for this store.</summary>
    public required bool IsEnabled { get; init; }

    /// <summary>Local time of the last attempt, or the "never" text.</summary>
    public required string LastRunText { get; init; }

    /// <summary>Label for <see cref="LastRunText"/>.</summary>
    public required string LastRunLabelText { get; init; }

    /// <summary>The last outcome, as the API reports it.</summary>
    public required string OutcomeText { get; init; }

    /// <summary>How many artifacts the store currently retains.</summary>
    public required string ArtifactCountText { get; init; }

    /// <summary>Label for <see cref="ArtifactCountText"/>.</summary>
    public required string ArtifactsLabelText { get; init; }

    /// <summary>Path of the most recent artifact, when one exists.</summary>
    public required string ArtifactPathText { get; init; }

    /// <summary>Label for <see cref="ArtifactPathText"/>.</summary>
    public required string ArtifactPathLabelText { get; init; }

    /// <summary>The scheduled local time for this store.</summary>
    public required string ScheduleText { get; init; }

    /// <summary>Label for <see cref="ScheduleText"/>.</summary>
    public required string NextDueLabelText { get; init; }
}
