using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Startup.Services;

public sealed class StartupRegistrationService : IStartupRegistrationService
{
    private const string StartupRegistrationRequestKey = "Startup.NewUserRegistrationRequest";

    private readonly ILocalSettingsService _localSettingsService;

    public StartupRegistrationService(ILocalSettingsService localSettingsService)
    {
        ArgumentNullException.ThrowIfNull(localSettingsService);
        _localSettingsService = localSettingsService;
    }

    public async Task SubmitNewUserRequestAsync(StartupState startupState, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(startupState);
        cancellationToken.ThrowIfCancellationRequested();

        var request = new StartupRegistrationRequest
        {
            Username = startupState.Username,
            HostnameNormalized = startupState.HostnameNormalized,
            MacAddressNormalized = startupState.MacAddressNormalized,
            RequestedUtc = DateTimeOffset.UtcNow
        };

        await _localSettingsService.SaveSettingAsync(StartupRegistrationRequestKey, request);
    }
}
