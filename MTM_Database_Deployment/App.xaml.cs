using Microsoft.UI.Xaml;

namespace MTM_Database_Deployment;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();

        UnhandledException += OnUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            _window = new MainWindow();
            _window.Activate();
        }
        catch (Exception exception)
        {
            // A crash before the window exists shows the operator nothing at all, so write it down.
            Services.Service_FailureLog.Write("Startup", exception, BuildContext());
            throw;
        }
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e) =>
        Services.Service_FailureLog.Write("User interface", e.Exception, BuildContext());

    private void OnDomainUnhandledException(object sender, System.UnhandledExceptionEventArgs e) =>
        Services.Service_FailureLog.Write(
            "Background",
            e.ExceptionObject as Exception ?? new InvalidOperationException(e.ExceptionObject?.ToString()),
            BuildContext());

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e) =>
        Services.Service_FailureLog.Write("Unobserved task", e.Exception, BuildContext());

    /// <summary>Enough context to reproduce, without the database password.</summary>
    private static string BuildContext()
    {
        try
        {
            var settings = new Services.Service_DeploymentSettings();
            var loaded = settings.Load();
            return $"Server: {loaded.PreferredServer} (spare {loaded.FallbackServer}) Port: {loaded.Port} "
                + $"Database: {loaded.Database} User: {loaded.User}{Environment.NewLine}"
                + $"Settings: {settings.SettingsPath}{Environment.NewLine}"
                + $"Sql root: {settings.SqlRoot}";
        }
        catch (Exception exception)
        {
            return "Context unavailable: " + exception.Message;
        }
    }
}