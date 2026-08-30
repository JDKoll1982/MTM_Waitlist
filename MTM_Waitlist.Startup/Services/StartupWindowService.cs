using MTM_Waitlist.Module_Core.Contracts.Services;

namespace MTM_Waitlist.Module_Startup.Services;

public sealed class StartupWindowService : IStartupWindowService
{
    private readonly IAppLifecycleService _lifecycle;

    public StartupWindowService(IAppLifecycleService lifecycle)
    {
        _lifecycle = lifecycle;
    }

    public void ShowMainWindowAndCloseLoginWindow()
    {
        _lifecycle.ShowMainWindowAndCloseLoginWindow();
    }

    public void Exit()
    {
        _lifecycle.Exit();
    }
}
