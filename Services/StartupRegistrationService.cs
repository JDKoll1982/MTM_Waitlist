using MTM_Waitlist.Contracts.Services;
using MTM_Waitlist.Models;

namespace MTM_Waitlist.Services;

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
