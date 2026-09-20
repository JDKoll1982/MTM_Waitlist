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

    /// <summary>
    /// How many artifacts to keep for this store; older ones are pruned.
    /// </summary>
    /// <remarks>
    /// The default is sized against the criterion, not chosen for convenience. SC-008 asks whether every
    /// scheduled window in the last thirty days produced a restorable artifact, so a store that keeps fewer
    /// than thirty daily backups has already pruned the artifact the question is about — the criterion would be
    /// unanswerable for reasons that have nothing to do with whether backups work (T230). See
    /// <see cref="ReliabilityCriteria.BackupRetentionCount"/>.
    /// </remarks>
    public int RetentionCount { get; init; } = ReliabilityCriteria.BackupRetentionCount;

    /// <summary>
    /// Destination directory for this store's artifacts. Must be writable; validated at save time.
    /// </summary>
    public required string DestinationDirectory { get; init; }

    /// <summary>UTC time of the last backup attempt, if any.</summary>
    public DateTime? LastRunUtc { get; init; }

    /// <summary>Outcome of the last backup attempt.</summary>
    public BackupRunOutcome LastOutcome { get; init; } = BackupRunOutcome.Never;
}
