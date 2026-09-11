namespace MTM_Waitlist.Mock.Service.Contracts;

/// <summary>
/// Reads and writes the per-user logon auto-start registration (FR-007).
/// </summary>
/// <remarks>
/// An abstraction rather than direct registry access so the reconciliation logic in
/// <c>ServiceConfigurationStore</c> is verifiable without touching the real
/// <c>HKCU\Software\Microsoft\Windows\CurrentVersion\Run</c> key. That key is the mechanism deliberately
/// chosen over the MSIX-only <c>StartupTask</c> API, because the service ships unpackaged
/// (<c>WindowsPackageType=None</c>, research.md R4).
/// </remarks>
public interface IStartupRegistrationStore
{
    /// <summary>
    /// Reads the currently registered command for the service.
    /// </summary>
    /// <returns>The registered command line, or <see langword="null"/> when the service is not registered.</returns>
    string? GetRegisteredCommand();

    /// <summary>
    /// Registers or replaces the service's auto-start command.
    /// </summary>
    /// <param name="command">The command line Windows will run at logon.</param>
    void SetRegisteredCommand(string command);

    /// <summary>Removes the service's auto-start registration; a no-op when it is absent.</summary>
    void RemoveRegistration();
}
