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
    private static readonly string[] DoneStatuses = { "Completed", "Done", "Canceled", "Cancelled" };

    /// <summary>
    /// Every role at or above Material Handler, i.e. every role that may handle requests. This is the same
    /// vocabulary the Settings screens gate their broadest management panel on, so the waitlist's UI gate and
    /// the store's own gate agree on who a handler is instead of drifting apart.
    /// </summary>
    private static readonly string[] HandlerRoles =
    {
        "Material Handler",
        "Production",
        "Production Lead",
        "Setup",
        "Setup Lead",
        "Plant Manager",
        "Admin",
        "Developer",
    };

    /// <summary>
    /// True when the viewer's role is Material Handler or above, which is the gate for being offered any
    /// handler action at all. Compared case-insensitively; a blank role may never handle requests.
    /// </summary>
    public static bool CanViewerHandleRequests(string? role)
        => In(HandlerRoles, role);

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
    /// The same rule, additionally gated on the viewer being at handler level, so a screen can express the
    /// whole rule in one call. The narrower form above cannot say this: a viewer below handler level can
    /// never legitimately be an assignee, because only a handler can accept, so "is the assignee" alone is
    /// not enough to decide what a screen may offer.
    /// </summary>
    public static bool CanViewerCompleteOrRelease(string? status, string? assignee, string viewerEmployeeNumber, bool viewerCanHandleRequests)
        => viewerCanHandleRequests && CanViewerCompleteOrRelease(status, assignee, viewerEmployeeNumber);

    private static bool In(string[] set, string? status)
        => !string.IsNullOrWhiteSpace(status)
           && set.Any(value => string.Equals(value, status.Trim(), StringComparison.OrdinalIgnoreCase));
}
