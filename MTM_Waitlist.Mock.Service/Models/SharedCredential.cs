namespace MTM_Waitlist.Mock.Service.Models;

/// <summary>
/// The shared credential that gates the service's network API (FR-026, SC-010).
/// </summary>
/// <remarks>
/// <para>
/// The credential exists <b>only</b> as a DPAPI <c>CurrentUser</c>-protected blob. It is generated
/// on first run, rotated only by explicit operator action, never rendered in any UI, never written
/// to any log or status payload, and compared in constant time by the API authentication handler.
/// </para>
/// <para>
/// <see cref="ProtectedValue"/> is the sole stored form — there is deliberately no plaintext
/// property on this type, so a caller cannot accidentally log or serialize the secret.
/// </para>
/// </remarks>
public sealed record SharedCredential
{
    /// <summary>DPAPI <c>CurrentUser</c> ciphertext. The only stored form of the credential.</summary>
    public required byte[] ProtectedValue { get; init; }

    /// <summary>UTC time the credential was created (or last rotated).</summary>
    public required DateTime CreatedUtc { get; init; }
}
