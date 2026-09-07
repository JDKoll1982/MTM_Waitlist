using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Read/write editor for the real (non-mock) request-type/subtype catalog in <c>mtm_waitlist</c>
/// (<c>waitlist_request_types</c> / <c>waitlist_request_subtypes</c>). This is the backend the Developer
/// Settings catalog editor UI consumes. DB access is SP-only against the dedicated real-catalog SPs and is
/// deliberately separate from any mock-data service or allow-list. Consumers must enforce role authorization
/// (Admin/Developer) before calling the mutating methods.
/// </summary>
public interface IRequestTypeEditorService
{
    /// <summary>
    /// Enumerates the full catalog (types with their subtypes), INCLUDING inactive rows, preserving the
    /// seeded/display order. Reads via <c>sp_waitlist_request_types_get_all</c>/<c>sp_waitlist_request_subtypes_get_all</c>.
    /// </summary>
    Task<IReadOnlyList<RequestTypeEditorItem>> GetCatalogAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a subtype to a type via <c>sp_waitlist_request_subtypes_insert</c>. Returns rows affected.</summary>
    Task<int> AddSubtypeAsync(RequestSubtypeEditorItem subtype, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing subtype via <c>sp_waitlist_request_subtypes_update</c>. Returns rows affected.</summary>
    Task<int> UpdateSubtypeAsync(RequestSubtypeEditorItem subtype, CancellationToken cancellationToken = default);

    /// <summary>Deletes a subtype by id via <c>sp_waitlist_request_subtypes_delete</c>. Returns rows affected.</summary>
    Task<int> DeleteSubtypeAsync(long subtypeId, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing type (incl. active flag) via <c>sp_waitlist_request_types_update</c>. Returns rows affected.</summary>
    Task<int> UpdateTypeAsync(RequestTypeEditorItem type, CancellationToken cancellationToken = default);
}
