using Microsoft.Extensions.Options;

namespace MTM_Waitlist.Module_Shared.Services;

public sealed class SharedConfigurationService : ISharedConfigurationService
{
    private readonly ModuleSharedOptions _options;

    public SharedConfigurationService(IOptions<ModuleSharedOptions> options)
    {
        _options = options.Value;
    }

    public string GetConfiguredPrefix() => _options.SharedPrefix;
}
