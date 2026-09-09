namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Read-only enumeration of active request sub-type names from the real catalog
/// (<c>sp_waitlist_request_subtypes_get</c>). Consumed by the Settings "Max Allotted Time"
/// editor, which previously read through the (now removed) request-type catalog editor service.
/// </summary>
public interface IRequestSubtypeNameReadService
{
    Task<IReadOnlyList<string>> GetSubtypeNamesAsync(CancellationToken cancellationToken = default);
}
