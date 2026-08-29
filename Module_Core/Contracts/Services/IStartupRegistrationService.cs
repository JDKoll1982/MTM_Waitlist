using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

public interface IStartupRegistrationService
{
    Task SubmitNewUserRequestAsync(StartupState startupState, CancellationToken cancellationToken = default);
}
