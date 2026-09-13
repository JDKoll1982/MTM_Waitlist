namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// The store seam for one Item's <b>configured</b> allotted minutes — the value the deadline derives from
/// (FR-016) and the one the minutes screen edits (FR-018).
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this seam exists.</b> The configured value lives in the store, beside the observed average that comes
/// from a stored procedure, so it is visible to everyone rather than to one Windows profile (§D4). This
/// interface is declared here, in <c>MTM_Waitlist.Core</c>, because <see cref="Services.UrgencySettingsService"/>
/// lives here and must not take a dependency on the Settings module that owns the configuration read. The
/// implementation is supplied by the composition root.
/// </para>
/// <para>
/// <b>Neither member may be answered by a per-Windows-user local key.</b> The retired
/// <c>Urgency.MaxAllottedMinutes.&lt;subtype&gt;</c> keys are gone; a value keyed to a profile cannot be shown
/// beside a value read through a stored procedure.
/// </para>
/// <para>
/// <b>The observed average never reaches <see cref="SetAllottedMinutesAsync"/>.</b> It is display data read
/// through <c>sp_waitlist_request_item_observed_average_get</c>, and writing it back as though it were
/// configured is exactly the conflation FR-018 and FR-019 forbid.
/// </para>
/// </summary>
public interface IRequestItemAllottedMinutesStore
{
    /// <summary>
    /// The Item's configured allotment in minutes, or <b>null</b> when the Item has none configured. Null is
    /// not zero: the caller answers the labelled 15-minute default instead (FR-017).
    /// </summary>
    Task<int?> GetAllottedMinutesAsync(string itemCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes the Item's configured allotment. This is the only writer of a configured allotment, and it is
    /// reached only from the minutes editor, only for a person who passes that screen's own role gate (FR-020).
    /// </summary>
    Task SetAllottedMinutesAsync(string itemCode, int minutes, CancellationToken cancellationToken = default);
}
