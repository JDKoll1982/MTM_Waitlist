namespace MTM_Waitlist.Contracts.Services;

public interface IStartupRecoveryService
{
    Task ResetToDefaultsAsync(CancellationToken cancellationToken = default);

    Task CorruptAndRestartAsync(CancellationToken cancellationToken = default);
}