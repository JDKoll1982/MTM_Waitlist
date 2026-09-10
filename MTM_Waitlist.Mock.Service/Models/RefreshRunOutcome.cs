namespace MTM_Waitlist.Mock.Service.Models;

/// <summary>
/// The outcome of one shape refresh cycle.
/// </summary>
/// <remarks>
/// <see cref="SkippedSourceUnreachable"/> is a <b>normal</b> outcome, not a failure: the source
/// being down must not alter the live snapshot and must not stop the next scheduled cycle (FR-008).
/// </remarks>
public enum RefreshRunOutcome
{
    /// <summary>Not yet run in this cycle.</summary>
    Pending,

    /// <summary>Currently running.</summary>
    Running,

    /// <summary>The snapshot was replaced successfully.</summary>
    Succeeded,

    /// <summary>Infor Visual was unreachable; the previous snapshot was left in place.</summary>
    SkippedSourceUnreachable,

    /// <summary>The source returned an unexpected column set; the previous snapshot was left in place.</summary>
    FailedSchemaMismatch,

    /// <summary>The staged load failed validation; the previous snapshot was left in place.</summary>
    FailedLoad,

    /// <summary>An unexpected error occurred; the previous snapshot was left in place.</summary>
    FailedUnknown
}
