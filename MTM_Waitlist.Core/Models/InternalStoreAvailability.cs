using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// How an internal store last responded to the data-access seam (FR-021).
/// </summary>
public enum InternalStoreStatus
{
    /// <summary>The store has not been read yet in this session; nothing is claimed about it.</summary>
    Unknown = 0,

    /// <summary>The most recent read succeeded.</summary>
    Available = 1,

    /// <summary>
    /// The most recent read failed after every bounded retry. This is the state a screen reports; it is
    /// never replaced by sample data (FR-001) and never turned into a persistent banner (FR-021).
    /// </summary>
    Unavailable = 2,
}

/// <summary>
/// The last observed availability of one internal MySQL store, with everything a screen needs to explain
/// the state and offer a manual retry (FR-021, `data-model.md` §10).
/// </summary>
/// <remarks>
/// Internal stores (<c>mtm_waitlist</c>, <c>mtm_wip_application_winforms</c>,
/// <c>mtm_receiving_application</c>) are always read and written live. A failure is reported here and is
/// deliberately <b>not</b> substituted with a cached answer — the cache exists for Infor Visual reads only
/// (FR-001, FR-027).
/// </remarks>
public sealed record InternalStoreAvailability
{
    /// <summary>The store this observation is about.</summary>
    public required MySqlDatabaseTarget Store { get; init; }

    /// <summary>The observed state.</summary>
    public required InternalStoreStatus Status { get; init; }

    /// <summary>
    /// When the last attempt started. <see langword="null"/> until the store has been read once, so a screen
    /// can tell "never tried" from "tried and failed".
    /// </summary>
    public DateTime? LastAttemptUtc { get; init; }

    /// <summary>How many attempts the bounded retry policy consumed on the last failing read (0 after a success).</summary>
    public int RetryCount { get; init; }

    /// <summary>
    /// When the next attempt is worth making. Set only while <see cref="Status"/> is
    /// <see cref="InternalStoreStatus.Unavailable"/>, and it is guidance for a manual retry, not a schedule —
    /// the application never retries a store on a timer behind the operator's back.
    /// </summary>
    public DateTime? NextRetryUtc { get; init; }

    /// <summary>Operator-facing explanation, localized by the presenting screen.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>The database name, for the operator-facing message.</summary>
    public string StoreName => Store switch
    {
        MySqlDatabaseTarget.MtmReceivingApplication => "mtm_receiving_application",
        MySqlDatabaseTarget.MtmWipApplication => "mtm_wip_application_winforms",
        MySqlDatabaseTarget.MtmMock => "mtm_mock",
        _ => "mtm_waitlist",
    };

    /// <summary>The state before the store has been read, or when no tracker is installed.</summary>
    public static InternalStoreAvailability Unknown(MySqlDatabaseTarget store) =>
        new() { Store = store, Status = InternalStoreStatus.Unknown };
}
