using MTM_Waitlist.Module_Core.Contracts.Services;
using WinUIEx;

namespace MTM_Waitlist.Services;

public sealed class AppWindowProvider : IAppWindowProvider
{
    public WindowEx MainWindow => App.MainWindow;
}
