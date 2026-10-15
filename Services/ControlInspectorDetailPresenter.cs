using Microsoft.UI.Xaml;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Shared.Views;

namespace MTM_Waitlist.Services;

/// <summary>
/// Opens the control inspector's window, and reuses it while it is open.
/// </summary>
/// <remarks>
/// <para>
/// <b>One window, reused.</b> The shortcut can be pressed on control after control without closing anything, so a
/// window per press would litter the desktop with near-identical windows and leave the person to work out which one
/// is current. The window this holds is brought to the front and re-filled instead, and the reference is dropped
/// when it closes so the next press opens a fresh one.
/// </para>
/// <para>
/// <b>Created on the thread that asked.</b> The shortcut runs on the UI thread, which is the only thread a window
/// may be created on; nothing here hops threads or defers the work, because a deferred window would open after the
/// person had already looked elsewhere for it.
/// </para>
/// </remarks>
public sealed class ControlInspectorDetailPresenter : IControlInspectorDetailPresenter
{
    private const string LogModule = "ControlInspector";

    private ControlInspectorDetailWindow? _window;

    public bool Present(ControlInspectorDetail detail)
    {
        ArgumentNullException.ThrowIfNull(detail);

        try
        {
            if (_window is null)
            {
                _window = new ControlInspectorDetailWindow();
                _window.Closed += OnWindowClosed;
                AppLog.Info(LogModule, "The control inspector's window was opened.");
            }

            _window.ShowDetail(detail);
            return true;
        }
        catch (Exception exception)
        {
            // A diagnostic surface that cannot open must not take the application with it: the shortcut reports
            // nothing rather than throwing out of a pointer handler. It is logged, though - a developer tool that
            // fails silently is worse than one that is absent, because the person concludes the app is broken.
            AppLog.Error(LogModule, exception, "The control inspector's window could not be opened.");
            _window = null;
            return false;
        }
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        if (_window is not null)
        {
            _window.Closed -= OnWindowClosed;
        }

        _window = null;
    }
}
