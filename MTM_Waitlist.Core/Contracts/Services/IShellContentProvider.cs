using Microsoft.UI.Xaml;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Creates the root shell UIElement for the main window. Implemented by the app
/// so Core services can host the shell without referencing the concrete ShellPage view.
/// </summary>
public interface IShellContentProvider
{
    FrameworkElement CreateShellContent();
}
