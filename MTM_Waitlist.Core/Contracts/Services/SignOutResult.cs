namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// What a sign-out attempt did (FR-034, FR-026).
/// <para>
/// <see cref="Message"/> is the plain-language report shown when the sign-out could not be completed, resolved
/// through the resource mechanism. It is empty on success: nothing needs saying when the application is on its
/// way back to the sign-in screen.
/// </para>
/// </summary>
public sealed record SignOutResult(bool Succeeded, string Message)
{
    /// <summary>The session was ended and the application is restarting.</summary>
    public static SignOutResult Success() => new(true, string.Empty);

    /// <summary>The session's keys were cleared, but the application could not be relaunched.</summary>
    public static SignOutResult Refused(string message) => new(false, message);
}
