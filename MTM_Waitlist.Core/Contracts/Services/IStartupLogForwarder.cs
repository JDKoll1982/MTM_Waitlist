namespace MTM_Waitlist.Module_Core.Contracts.Services;

public interface IStartupLogForwarder
{
    Task ForwardAsync(string jsonLine, CancellationToken cancellationToken = default);
}
