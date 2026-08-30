namespace MTM_Waitlist.Module_Core.Models;

public sealed class StartupWindowOptions
{
    public int SplashWidth { get; set; } = 920;

    public int SplashHeight { get; set; } = 620;

    public int MainWidth { get; set; } = 1600;

    public int MainHeight { get; set; } = 980;

    public bool CenterOnModeSwitch { get; set; } = true;

    public int MainTransitionDelayMilliseconds { get; set; } = 120;
}
