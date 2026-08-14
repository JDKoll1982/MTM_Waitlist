namespace MTM_Waitlist.Module_Waitlist.Models;

public sealed class WaitlistRequest
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Building { get; init; } = string.Empty;
    public string WorkCenter { get; init; } = string.Empty;
    public string RequestType { get; init; } = string.Empty;
    public string? Subtype { get; init; }
    public string? InputValue { get; init; }
    public string Status { get; init; } = "Pending";
    public DateTimeOffset RequestedUtc { get; init; } = DateTimeOffset.UtcNow;
}