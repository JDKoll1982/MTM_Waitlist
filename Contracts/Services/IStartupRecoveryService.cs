namespace MTM_Waitlist.Contracts.Services;

public interface IStartupRecoveryService
{
    Task ResetSettingAsync(string key, CancellationToken cancellationToken = default);

    Task ResetToDefaultsAsync(CancellationToken cancellationToken = default);

    Task CorruptAndRestartAsync(CancellationToken cancellationToken = default);
}