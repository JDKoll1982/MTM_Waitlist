namespace MTM_Waitlist.Mock.Service.Models;

/// <summary>
/// The service's network API settings (FR-011, FR-012).
/// </summary>
public sealed record ApiSettings
{
    /// <summary>
    /// Address Kestrel binds to. Defaults to <c>0.0.0.0</c> because clients are mostly on the
    /// network rather than on the MySQL host; loopback-only is a valid operator choice.
    /// </summary>
    public string BindAddress { get; init; } = "0.0.0.0";

    /// <summary>Port Kestrel binds to.</summary>
    public int Port { get; init; } = 5760;

    /// <summary>
    /// The shared credential gating every API endpoint. <see langword="null"/> until generated on
    /// first run. Never displayed or logged (FR-026).
    /// </summary>
    public SharedCredential? Credential { get; init; }
}
