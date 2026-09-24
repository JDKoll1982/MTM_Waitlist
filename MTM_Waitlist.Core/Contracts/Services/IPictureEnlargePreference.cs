namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Whether clicking a picture enlarges it, for the person signed in.
/// </summary>
/// <remarks>
/// <para>
/// A preference, not a permission: it is stored against the person's own account so it follows them to any
/// computer, and no role gate stands in front of it.
/// </para>
/// <para>
/// The value is held in memory after the first read, because a click must not wait on a database round trip. The
/// same instance is what the settings screen writes through, so switching the feature off takes effect on the
/// open screen rather than at the next start.
/// </para>
/// <para>
/// The declared default is <b>on</b>: a value that has never been set, a store that cannot be reached, and a
/// reader who is not signed in all answer "on" (FR-030, FR-031).
/// </para>
/// </remarks>
public interface IPictureEnlargePreference
{
    /// <summary>Whether a click on a picture should enlarge it. Never throws.</summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Reads the stored value once. Called at start-up; safe to call again, and safe to call when the store is
    /// unreachable — the declared default is kept and the failure is recorded rather than raised.
    /// </summary>
    Task LoadAsync();

    /// <summary>Stores the value for the signed-in person and applies it immediately.</summary>
    Task SetEnabledAsync(bool enabled);
}
