namespace MTM_Waitlist.Mock.Service.Models;

/// <summary>
/// The outcome of an emergency restore request (FR-010, FR-023, SC-009).
/// </summary>
public enum RestoreOutcomeKind
{
    /// <summary>The operator declined the confirmation prompt; nothing changed.</summary>
    NotConfirmed,

    /// <summary>The store was replaced and verified.</summary>
    Succeeded,

    /// <summary>Dropping the existing store failed.</summary>
    FailedDrop,

    /// <summary>Reloading the artifact failed; recover from the safety snapshot.</summary>
    FailedReload,

    /// <summary>Reload completed but row-count verification failed.</summary>
    FailedVerification
}
