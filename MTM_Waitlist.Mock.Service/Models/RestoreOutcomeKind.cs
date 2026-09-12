namespace MTM_Waitlist.Mock.Service.Models;

/// <summary>
/// The outcome of an emergency restore request (FR-010, FR-023, SC-009).
/// </summary>
public enum RestoreOutcomeKind
{
    /// <summary>The operator declined the confirmation prompt; nothing changed.</summary>
    NotConfirmed,

    /// <summary>
    /// The store's database is not reachable from this machine, so restore is disabled for it here; nothing
    /// was changed and no safety snapshot was taken.
    /// </summary>
    StoreUnavailable,

    /// <summary>The store was replaced and verified.</summary>
    Succeeded,

    /// <summary>Dropping the existing store failed.</summary>
    FailedDrop,

    /// <summary>Reloading the artifact failed; recover from the safety snapshot.</summary>
    FailedReload,

    /// <summary>Reload completed but the post-reload verification could not confirm the store.</summary>
    FailedVerification
}
