using WinUIEx;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Provides access to the application's main window. Implemented by the app
/// (composition root) so Core services never need to reference the App static.
/// </summary>
public interface IAppWindowProvider
{
    WindowEx MainWindow { get; }
}
