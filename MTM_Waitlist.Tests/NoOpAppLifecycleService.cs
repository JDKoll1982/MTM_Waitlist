using MTM_Waitlist.Module_Core.Contracts.Services;

namespace MTM_Waitlist.Tests;

public sealed class NoOpAppLifecycleService : IAppLifecycleService
{
    public void Exit()
    {
    }

    public void ShowLoginWindowAndCloseSplash()
    {
    }

    public void ShowMainWindowAndCloseSplash()
    {
    }

    public void ShowMainWindowAndCloseLoginWindow()
    {
    }
}
