namespace MTM_Waitlist.Module_Core.Services;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Permissions;

/// <summary>
/// Pure role-/ownership-aware request-card action policy for the file-07 Accept / Complete / Release work.
/// Given a request's status and assignee and the viewer's employee number, decides which actions the current
/// viewer may take. Status strings are compared case-insensitively; the app supplies the inputs. Deterministic
/// and unit-testable.
/// </summary>
/// <remarks>
/// <para>
/// <b>The handler question is a named permission, not a list of role names.</b> This type used to carry
/// <c>HandlerRoles</c> — eight display names compared case-insensitively — and answer
/// <c>CanViewerHandleRequests(role)</c> from it. That list is gone: who may handle requests is
/// <see cref="PermissionKeys.RequestsHandle"/>, resolved from the one declaration, so a change to who may
/// handle is a change to stored data and never a change to this file (FR-050, FR-054).
/// </para>
/// <para>
/// The status and ownership rules below stay here and stay pure: they are about a request, not about a person,
/// and they take the handler answer as an input rather than deciding it.
/// </para>
/// </remarks>
public static class RequestActionPolicy
{
    private static readonly string[] OpenStatuses = { "Pending", "Released" };
    private static readonly string[] TakenStatuses = { "Accepted", "In Progress" };
    private static readonly string[] DoneStatuses = { "Completed", "Done", "Canceled", "Cancelled" };

    /// <summary>
    /// The one permission that admits a viewer to the handler actions. Read from the declaration rather than
    /// restated, so the key a gate asks for and the key a baseline is written under cannot drift.
    /// </summary>
    public static string HandleRequestsPermissionKey => PermissionKeys.RequestsHandle;

    /// <summary>
    /// The answer used when no permission service is available, which is the declaration's own shipped
    /// fallback. It is read from the declaration rather than written here, so the single decision about what an
    /// unanswerable store admits lives in one place (FR-050).
    /// </summary>
    public static bool FallbackCanHandleRequests =>
        PermissionRegistry.Find(PermissionKeys.RequestsHandle)?.Fallback ?? false;

    /// <summary>
    /// Whether the signed-in person holds <see cref="HandleRequestsPermissionKey"/>.
    /// </summary>
    /// <param name="permissionService">
    /// The permission service, or <see langword="null"/> when the host has none — a headless test host, for
    /// instance. A missing service is answered from the shipped fallback rather than treated as a refusal, so a
    /// failure to ask the question never silently disables the shop floor's work.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task<bool> CanViewerHandleRequestsAsync(
        IPermissionService? permissionService,
        CancellationToken cancellationToken = default)
    {
        if (permissionService is null)
        {
            return FallbackCanHandleRequests;
        }

        return await permissionService
            .HasPermissionAsync(HandleRequestsPermissionKey, cancellationToken)
            .ConfigureAwait(false);
    }

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
