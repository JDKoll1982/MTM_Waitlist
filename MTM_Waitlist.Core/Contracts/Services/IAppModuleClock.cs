namespace MTM_Waitlist.Module_Core.Contracts.Services;

public interface IAppModuleClock
{
    DateTimeOffset UtcNow { get; }
}
