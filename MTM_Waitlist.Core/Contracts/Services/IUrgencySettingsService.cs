namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Per-request-subtype "max allotted time" settings for the file-08 urgency rules. A request's due time is its
/// created time plus its sub-type's max allotted time; this service owns the persisted values (with sensible
/// defaults) that <c>UrgencyCalculator</c> consumes. Persisted per Windows user via <see cref="ILocalSettingsService"/>.
/// </summary>
public interface IUrgencySettingsService
{
    /// <summary>The default max-allotted duration applied to a sub-type with no stored override.</summary>
    TimeSpan DefaultMaxAllotted { get; }

    /// <summary>Gets the max-allotted time for a sub-type, falling back to <see cref="DefaultMaxAllotted"/>.</summary>
    Task<TimeSpan> GetMaxAllottedAsync(string subtype, CancellationToken cancellationToken = default);

    /// <summary>Sets (in minutes) the max-allotted time for a sub-type; values are clamped to a positive minimum.</summary>
    Task SetMaxAllottedAsync(string subtype, int minutes, CancellationToken cancellationToken = default);
}
