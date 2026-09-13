using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

/// <summary>
/// Data access for the New Request wizard: the availability snapshot the picker filters on, the filtered
/// Category and Item lists the wizard binds, and work-centre image resolution.
/// </summary>
public interface INewRequestFlowService
{
    /// <summary>
    /// Resolves what the work centre's active setup job actually has. Called as the Category step is entered —
    /// <b>between</b> the Category and the Item step — so the Item list is filtered before it is built
    /// (FR-002, contract §3). It is never computed at registration time.
    /// </summary>
    Task<RequestJobPartAvailability> ResolveAvailabilityAsync(string workCenter, CancellationToken cancellationToken = default);

    /// <summary>
    /// The Categories that would offer at least one Item to this job, in canonical order. A Category that would
    /// open an empty Item step is not offered at all (FR-002, SC-005).
    /// </summary>
    IReadOnlyList<RequestCategory> GetVisibleCategories(RequestJobPartAvailability availability);

    /// <summary>
    /// The Category's Items this job supports, in catalog Order, built already filtered: an unsupported Item is
    /// never constructed, never bound and never offered (FR-002).
    /// </summary>
    IReadOnlyList<RequestItemDefinition> GetVisibleItems(RequestCategory category, RequestJobPartAvailability availability);

    /// <summary>Resolves each active work centre's image, keyed by display name, for the first wizard step.</summary>
    Task<Dictionary<string, string>> BuildWorkCenterImageLookupAsync(CancellationToken cancellationToken = default);
}
