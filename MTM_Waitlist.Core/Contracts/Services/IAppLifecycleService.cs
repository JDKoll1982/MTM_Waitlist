namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// App-lifetime/window actions. Implemented by the app (composition root) so
/// feature modules never reference the App static directly.
/// </summary>
public interface IAppLifecycleService
{
    void Exit();

    void ShowLoginWindowAndCloseSplash();

    void ShowMainWindowAndCloseSplash();

    void ShowMainWindowAndCloseLoginWindow();
}
