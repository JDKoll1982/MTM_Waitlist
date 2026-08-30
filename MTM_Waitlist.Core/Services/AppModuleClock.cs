using MTM_Waitlist.Module_Core.Contracts.Services;

namespace MTM_Waitlist.Module_Core.Services;

public sealed class AppModuleClock : IAppModuleClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
