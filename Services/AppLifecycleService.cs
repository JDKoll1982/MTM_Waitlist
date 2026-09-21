using MTM_Waitlist.Module_Core.Contracts.Services;

namespace MTM_Waitlist.Services;

public sealed class AppLifecycleService : IAppLifecycleService
{
    public void Exit() => App.ExitApplication();

    public void ShowLoginWindowAndCloseSplash() => App.ShowLoginWindowAndCloseSplash();

    public void ShowMainWindowAndCloseSplash() => App.ShowMainWindowAndCloseSplash();

    public void ShowMainWindowAndCloseLoginWindow() => App.ShowMainWindowAndCloseLoginWindow();
}
