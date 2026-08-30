using MTM_Waitlist.Module_Core.Helpers;
using Windows.UI.ViewManagement;

namespace MTM_Waitlist;

public sealed partial class MainWindow : WindowEx
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue? _dispatcherQueue;
    private readonly UISettings? _settings;
    private bool _isDisposed;

    public MainWindow()
    {
        InitializeComponent();
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/WindowIcon.ico"));
        Content = null;
        Title = "AppDisplayName".GetLocalized();

        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        try
        {
            _settings = new UISettings();
            _settings.ColorValuesChanged += Settings_ColorValuesChanged;
        }
        catch (Exception)
        {
            _settings = null;
        }

        Closed += MainWindow_Closed;
    }

    private void Settings_ColorValuesChanged(UISettings sender, object args)
    {
        if (_isDisposed)
        {
            return;
        }

        // FIX 1: Explicitly check if the captured dispatcher queue is active and accepting work
        if (_dispatcherQueue is null || _dispatcherQueue.HasThreadAccess == false && _isDisposed)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            if (_isDisposed)
            {
                return;
            }
            TitleBarHelper.ApplySystemThemeToCaptionButtons(App.MainWindow, App.AppTitlebar);
        });
    }

    private void MainWindow_Closed(object sender, Microsoft.UI.Xaml.WindowEventArgs args)
    {
        // FIX 2: Set the disposal barrier flag first before anything else executes
        _isDisposed = true;

        if (_settings is not null)
        {
            try
            {
                _settings.ColorValuesChanged -= Settings_ColorValuesChanged;
            }
            catch
            {
                // Guard against native shell registration crashes during rapid app exit routines
            }
        }

        Closed -= MainWindow_Closed;
    }
}
