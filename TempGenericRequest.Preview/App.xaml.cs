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
        _window = new MainWindow(ReadMarkedWorkCenter(args.Arguments));
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
    private static string? ReadMarkedWorkCenter(string? arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return null;
        }

        const string marker = "--work-center";

        var index = arguments.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return null;
        }

        var remainder = arguments[(index + marker.Length)..].Trim();
        if (remainder.Length == 0)
        {
            return null;
        }

        if (remainder[0] == '"')
        {
            var closing = remainder.IndexOf('"', 1);
            return closing > 1 ? remainder[1..closing] : remainder[1..];
        }

        return remainder.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
    }
}
