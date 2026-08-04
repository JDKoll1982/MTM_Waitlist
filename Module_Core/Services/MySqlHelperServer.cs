using MTM_Waitlist.Module_Core.Contracts.Services;

namespace MTM_Waitlist.Module_Core.Services;

public sealed class MySqlHelperServer
{
    private const string MockDataSettingKey = "Feature.UseMockData";
    private readonly ILocalSettingsService _localSettingsService;
    private readonly ISampleDataService _sampleDataService;

    public MySqlHelperServer(ILocalSettingsService localSettingsService, ISampleDataService sampleDataService)
    {
        _localSettingsService = localSettingsService;
        _sampleDataService = sampleDataService;
    }

    public async Task<IReadOnlyList<object>> ExecuteReadWriteAsync(string operationName, string? parameter = null)
    {
        var useMockData = await _localSettingsService.ReadSettingAsync<bool?>(MockDataSettingKey) ?? true;
        if (useMockData)
        {
            return _sampleDataService.GetSampleOrders(parameter);
        }

        return Array.Empty<object>();
    }
}
