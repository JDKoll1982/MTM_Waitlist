namespace MTM_Waitlist.Mock.Service.Models;

/// <summary>
/// One store's backup configuration. Every store is configured independently: disabling or
/// rescheduling one store must not change any other store's schedule or artifacts
/// (FR-009, US6 acceptance 2).
/// </summary>
public sealed record BackupPolicy
{
    /// <summary>Which store this policy governs.</summary>
    public required BackupStore Store { get; init; }

    /// <summary>Whether scheduled backups run for this store.</summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>Local host time of day the backup runs.</summary>
    public TimeOnly ScheduleLocalTime { get; init; } = new(1, 0);

    /// <summary>How many artifacts to keep for this store; older ones are pruned.</summary>
    public int RetentionCount { get; init; } = 14;

    /// <summary>
    /// Destination directory for this store's artifacts. Must be writable; validated at save time.
    /// </summary>
    public required string DestinationDirectory { get; init; }

    /// <summary>UTC time of the last backup attempt, if any.</summary>
    public DateTime? LastRunUtc { get; init; }

    /// <summary>Outcome of the last backup attempt.</summary>
    public BackupRunOutcome LastOutcome { get; init; } = BackupRunOutcome.Never;
}
