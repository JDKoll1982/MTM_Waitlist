using MTM_Waitlist.Module_Core.Contracts.Services;

namespace MTM_Waitlist.Module_Core.Services;

public sealed class SqlHelperServer
{
    private const string InforVisualMockDataSettingKey = "Feature.InforVisualMockData";
    private readonly ILocalSettingsService _localSettingsService;
    private readonly ISampleDataService _sampleDataService;

    public SqlHelperServer(ILocalSettingsService localSettingsService, ISampleDataService sampleDataService)
    {
        _localSettingsService = localSettingsService;
        _sampleDataService = sampleDataService;
    }

    public async Task<IReadOnlyList<object>> ExecuteReadOnlyQueueAsync(string queueName, string? parameter = null)
    {
        var useMockData = await _localSettingsService.ReadSettingAsync<bool?>(InforVisualMockDataSettingKey) ?? false;
        if (useMockData)
        {
            return _sampleDataService.GetSampleOrders(parameter);
        }

        return Array.Empty<object>();
    }

    public async Task<T> ExecuteReadOnlyQueueAsync<T>(
        string queueName,
        string? parameter,
        Func<Task<T>> mockAction,
        Func<Task<T>> backendAction)
    {
        var useMockData = await _localSettingsService.ReadSettingAsync<bool?>(InforVisualMockDataSettingKey) ?? false;
        if (useMockData)
        {
            _ = _sampleDataService.GetSampleOrders(parameter);
            return await mockAction().ConfigureAwait(false);
        }

        return await backendAction().ConfigureAwait(false);
    }
}
