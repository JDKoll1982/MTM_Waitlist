using Microsoft.UI.Xaml;

namespace MTM_Waitlist.GenericRequest.Temp;

/// <summary>
/// The preview application. It opens one window and reads nothing.
/// </summary>
public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow(ReadMarkedWorkCenter());
        _window.Activate();
    }

    /// <summary>
    /// The <c>--work-center</c> value the setup screen passes, or null when the preview was started on its own.
    /// </summary>
    /// <remarks>
    /// The setup screen opens the preview for the work center someone just marked generic, so the window says which
    /// one it is showing the flow for. Nothing is read from the plant: the name is only a label, and the preview's
    /// values stay in memory.
    /// </remarks>
    private static string? ReadMarkedWorkCenter()
    {
        // The process command line, not LaunchActivatedEventArgs.Arguments: an unpackaged WinUI app receives no
        // activation arguments here, so the parsed name was always empty and the window showed a blank line where the
        // marked work center belongs.
        var arguments = Environment.GetCommandLineArgs();
        var index = Array.FindIndex(
            arguments,
            argument => string.Equals(argument, "--work-center", StringComparison.OrdinalIgnoreCase));

        return index >= 0 && index + 1 < arguments.Length && !string.IsNullOrWhiteSpace(arguments[index + 1])
            ? arguments[index + 1].Trim('"')
            : null;
    }
}
