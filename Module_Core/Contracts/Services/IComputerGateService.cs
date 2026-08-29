using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

public interface IComputerGateService
{
    Task<ComputerGateCheck> CheckAsync(CancellationToken cancellationToken = default);
}
