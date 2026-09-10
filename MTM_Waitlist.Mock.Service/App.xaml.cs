using Microsoft.UI.Xaml;

namespace MTM_Waitlist.Mock.Service;

/// <summary>
/// Entry point for the on-host Module_Mock service application.
/// </summary>
/// <remarks>
/// Phase 1 (Setup) creates a minimal, buildable host. The tray-only, single-instance lifetime and the
/// background engines (scheduled refresh, backups, API host) are implemented in later phases
/// (tasks T059, T062, T071-T084).
/// </remarks>
public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // T059 replaces this with the WinUIEx tray-only lifetime and the background engines.
    }
}
