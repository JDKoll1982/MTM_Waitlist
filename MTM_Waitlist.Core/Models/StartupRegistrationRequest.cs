namespace MTM_Waitlist.Module_Core.Models;

public sealed class StartupRegistrationRequest
{
    public string Username { get; init; } = string.Empty;

    public string HostnameNormalized { get; init; } = string.Empty;

    public string MacAddressNormalized { get; init; } = string.Empty;

    public DateTimeOffset RequestedUtc { get; init; }
}
