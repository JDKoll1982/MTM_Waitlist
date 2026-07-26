namespace MTM_Waitlist.Contracts.Services;

public interface IStartupLogForwarder
{
    Task ForwardAsync(string jsonLine, CancellationToken cancellationToken = default);
}
