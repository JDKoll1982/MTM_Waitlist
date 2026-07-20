namespace MTM_Waitlist.Contracts.Services;

public interface IStartupShellStateService
{
    event EventHandler? StateChanged;

    bool IsNavigationVisible { get; }

    void EnterSplashMode();

    Task EnterMainModeAsync(CancellationToken cancellationToken = default);
}
