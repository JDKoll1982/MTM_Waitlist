using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Module_Waitlist.Models;

public sealed class WaitlistRequestAuditEntry
{
    public Guid RequestId { get; init; }
    public string? FromStatus { get; init; }
    public string? ToStatus { get; init; }
    public string EventType { get; init; } = string.Empty;
    public DateTimeOffset OccurredUtc { get; init; } = DateTimeOffset.UtcNow;
    public string? EmployeeNumber { get; init; }
    public string? EmployeeName { get; init; }
    public string? Details { get; init; }

    /// <summary>
    /// The entry's local time, formatted for display on the request history. Kept here rather than in the
    /// view so the list and any future reader format it the same way.
    /// </summary>
    public string OccurredLocalText => OccurredUtc.ToLocalTime().ToString("g", System.Globalization.CultureInfo.CurrentCulture);

    /// <summary>
    /// Who the history row is attributed to, by full name. An entry with no author is a change the system made
    /// on its own — the request being created, accepted, completed or canceled — so it is labelled as such
    /// rather than rendering a blank where a person's name belongs.
    /// </summary>
    public string ActorDisplayName => string.IsNullOrWhiteSpace(EmployeeName)
        ? "Waitlist_History.SystemActor".GetLocalized()
        : EmployeeName!;
}
