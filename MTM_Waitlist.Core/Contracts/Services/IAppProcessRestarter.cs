namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Starts a fresh copy of this application.
/// <para>
/// It exists so the relaunch is a seam: signing out has to relaunch the process (FR-034), and a process that
/// relaunches itself cannot be observed by a test that is running inside it. The application implements this
/// over <see cref="Environment.ProcessPath"/> — including the <c>dotnet</c>-hosted case — exactly as the
/// corrupt-settings recovery path already does.
/// </para>
/// </summary>
public interface IAppProcessRestarter
{
    /// <summary>
    /// Starts a fresh copy of the application and returns whether it was started. <c>false</c> — or a thrown
    /// exception — is reported to the person rather than swallowed (FR-026).
    /// </summary>
    bool Restart();
}
