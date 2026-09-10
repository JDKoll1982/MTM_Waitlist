namespace MTM_Waitlist.Mock.Service.Models;

/// <summary>
/// The outcome of one backup cycle for one store (FR-013, SC-008).
/// </summary>
public enum BackupRunOutcome
{
    /// <summary>No backup has been attempted yet.</summary>
    Never,

    /// <summary>The dump completed and produced a non-zero artifact.</summary>
    Succeeded,

    /// <summary><c>mysqldump</c> could not be found; no artifact was recorded (FR-013).</summary>
    ToolUnavailable,

    /// <summary>The dump process failed or produced a zero-length file; no artifact is a success.</summary>
    Failed
}
