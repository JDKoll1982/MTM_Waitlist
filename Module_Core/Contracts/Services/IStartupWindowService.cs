namespace MTM_Waitlist.Module_Core.Contracts.Services;

public interface IStartupWindowService
{
    void ShowMainWindowAndCloseLoginWindow();

    void Exit();
}
