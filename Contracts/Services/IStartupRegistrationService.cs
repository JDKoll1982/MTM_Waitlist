using MTM_Waitlist.Models;

namespace MTM_Waitlist.Contracts.Services;

public interface IStartupRegistrationService
{
    Task SubmitNewUserRequestAsync(StartupState startupState, CancellationToken cancellationToken = default);
}
