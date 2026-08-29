using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

public interface IComputerGateService
{
    Task<ComputerGateCheck> CheckAsync(CancellationToken cancellationToken = default);
}
