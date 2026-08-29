using MTM_Waitlist.Module_Core.Contracts.Services;

namespace MTM_Waitlist.Module_Startup.Services;

public sealed class StartupWindowService : IStartupWindowService
{
    public void ShowMainWindowAndCloseLoginWindow()
    {
        App.ShowMainWindowAndCloseLoginWindow();
    }

    public void Exit()
    {
        App.Current.Exit();
    }
}
