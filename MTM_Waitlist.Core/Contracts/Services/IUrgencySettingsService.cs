namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// One Item's <b>configured</b> allotted minutes — the window its request has before it counts as overdue.
/// A request's due time is its created time plus its Item's allotted minutes, and this service is the seam the
/// deadline service reads it through.
/// </summary>
/// <remarks>
/// <para>
/// <b>The key is an Item, never a request type or a subtype</b> (FR-016). The allotment used to be stored per
/// request subtype in <c>%LOCALAPPDATA%</c>, which made it invisible to everyone else and unusable beside a
/// value that comes from a stored procedure (§D4).
/// </para>
/// <para>
/// <b>An Item with no configured minutes is not excluded from the urgency order.</b> It is measured by the
/// <see cref="Services.UrgencySettingsService.DefaultMinutes">15-minute fallback</see>, which is a value
/// someone must be able to recognise as a <i>default</i> rather than as a configuration (FR-017, SC-007).
/// </para>
/// </remarks>
public interface IUrgencySettingsService
{
    /// <summary>The default max-allotted duration applied to an Item with no stored allotment.</summary>
    TimeSpan DefaultMaxAllotted { get; }

    /// <summary>
    /// The Item's allotted minutes, falling back to <see cref="DefaultMaxAllotted"/> when the Item has none
    /// configured or the store cannot be reached.
    /// </summary>
    Task<TimeSpan> GetMaxAllottedAsync(string? item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets (in minutes) the Item's allotted time. Values are clamped to a positive minimum and a twenty-four
    /// hour maximum. This is the only writer of a configured allotment, and it is reached only from the minutes
    /// editor, only for a person who passes that screen's own role gate (FR-020).
    /// </summary>
    Task SetMaxAllottedAsync(string? item, int minutes, CancellationToken cancellationToken = default);
}
