namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// A one-time credential and everything the window that reveals it says about it (FR-028, FR-030, FR-032).
/// </summary>
/// <remarks>
/// This is the only shape the credential travels in, and it travels no further than the window that shows it: it
/// is written nowhere, logged nowhere and recorded in no audit row, and after the window closes the value is not
/// held anywhere in the application.
/// </remarks>
public sealed record IssuedCredential(
    string Pin,
    string PersonName,
    string SignInName,
    DateTimeOffset IssuedAtUtc,
    string IssuedBy);
