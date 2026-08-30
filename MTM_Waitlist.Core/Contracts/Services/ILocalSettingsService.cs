namespace MTM_Waitlist.Module_Core.Contracts.Services;

public interface ILocalSettingsService
{
    Task<T?> ReadSettingAsync<T>(string key);

    Task SaveSettingAsync<T>(string key, T value);

    Task ResetSettingAsync(string key, CancellationToken cancellationToken = default);

    Task ResetAsync();

    Task CorruptForTestAsync();
}
