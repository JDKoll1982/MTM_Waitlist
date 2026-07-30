namespace MTM_Waitlist.Module_Core.Contracts.Services;

public interface IStartupShellStateService
{
    event EventHandler? StateChanged;

    bool IsNavigationVisible { get; }

    void EnterSplashMode();

    Task EnterMainModeAsync(CancellationToken cancellationToken = default);
}
