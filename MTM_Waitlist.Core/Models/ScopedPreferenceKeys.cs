namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// The scoped preferences whose key has to be shared by two modules, declared once here so neither module carries
/// a second copy of the string (FR-029).
/// </summary>
/// <remarks>
/// <para>
/// <b>Only the shared keys live here.</b> A preference read and written by one type keeps its key beside that
/// type, where the reader can see what it means. The session length is the exception: the launch resolves it when
/// it issues a session, in <c>MTM_Waitlist.Startup</c>, and the settings panel writes it, in
/// <c>MTM_Waitlist.Settings</c>, and the two modules do not reference each other. Declaring it here is what makes
/// the writer and the reader agree by construction rather than by a comment saying they must.
/// </para>
/// <para>
/// <b>The scope is plant, not person.</b> How long a session lasts is one answer for the site, so an authorised
/// change applies to the next sign-in anywhere rather than only on the machine it was made on (SC-011).
/// </para>
/// </remarks>
public static class ScopedPreferenceKeys
{
    /// <summary>
    /// The setting key the session length in minutes is stored under, as the launch reads it and the settings
    /// panel writes it.
    /// </summary>
    public const string SessionLengthMinutes = "auth.session_length_minutes";

    /// <summary>
    /// The session length in force when the store holds no value: eight hours, which is the documented default.
    /// </summary>
    /// <remarks>
    /// The value here is the fallback for a missing row, not a ceiling. A stored value of zero or less is also
    /// answered by this default, because a session that expires the moment it is issued is not a session.
    /// </remarks>
    public const int DefaultSessionLengthMinutes = 480;

    /// <summary>The smallest session length the settings panel will store, in minutes: a quarter of an hour.</summary>
    /// <remarks>
    /// A floor rather than a free number, so a slip of the keyboard cannot leave everybody signed out mid-shift.
    /// The store refuses a non-positive length as well, so neither half of the pair is the only guard.
    /// </remarks>
    public const int MinimumSessionLengthMinutes = 15;

    /// <summary>The largest session length the settings panel will store, in minutes: twenty-four hours.</summary>
    public const int MaximumSessionLengthMinutes = 1440;
}
