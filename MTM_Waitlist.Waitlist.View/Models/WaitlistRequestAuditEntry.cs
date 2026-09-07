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
}
