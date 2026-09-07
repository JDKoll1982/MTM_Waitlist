namespace MTM_Waitlist.Module_Core.Services;

/// <summary>
/// Pure role-/ownership-aware request-card action policy for the file-07 Accept / Complete / Release work.
/// Given a request's status and assignee and the viewer's employee number/role capability, decides which actions
/// the current viewer may take. Status strings are compared case-insensitively; the app supplies the inputs.
/// Deterministic and unit-testable.
/// </summary>
public static class RequestActionPolicy
{
    private static readonly string[] OpenStatuses = { "Pending", "Released" };
    private static readonly string[] TakenStatuses = { "Accepted", "In Progress" };
    private static readonly string[] DoneStatuses = { "Completed", "Done", "Cancelled" };

    /// <summary>True if the request has no assignee yet (open) and is not done.</summary>
    public static bool IsAvailable(string? status) => In(OpenStatuses, status);

    /// <summary>True if the request is currently claimed/taken.</summary>
    public static bool IsTaken(string? status) => In(TakenStatuses, status);

    /// <summary>True if the request reached a terminal state (done/cancelled).</summary>
    public static bool IsDone(string? status) => In(DoneStatuses, status);

    /// <summary>
    /// A handler-or-above viewer may Accept an unaccepted (available) request.
    /// </summary>
    public static bool CanViewerAccept(string? status, bool viewerCanHandleRequests)
        => !IsDone(status) && IsAvailable(status) && viewerCanHandleRequests;

    /// <summary>True if the request is taken by <paramref name="assignee"/> (the viewer).</summary>
    public static bool IsAssignedToViewer(string? status, string? assignee, string viewerEmployeeNumber)
        => IsTaken(status)
           && !string.IsNullOrWhiteSpace(assignee)
           && string.Equals(assignee.Trim(), viewerEmployeeNumber.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Only the assigned handler may Complete or Release; not possible after the request is done or cancelled.
    /// </summary>
    public static bool CanViewerCompleteOrRelease(string? status, string? assignee, string viewerEmployeeNumber)
        => !IsDone(status) && IsAssignedToViewer(status, assignee, viewerEmployeeNumber);

    /// <summary>
    /// The creator may Edit (kept as a TODO with no behavior per file 07). A request that is done/cancelled is
    /// treated as not editable.
    /// </summary>
    public static bool CanViewerEdit(string? status, string? creator, string viewerEmployeeNumber)
        => !IsDone(status)
           && !string.IsNullOrWhiteSpace(creator)
           && string.Equals(creator.Trim(), viewerEmployeeNumber.Trim(), StringComparison.OrdinalIgnoreCase);

    private static bool In(string[] set, string? status)
        => !string.IsNullOrWhiteSpace(status)
           && set.Any(value => string.Equals(value, status.Trim(), StringComparison.OrdinalIgnoreCase));
}
