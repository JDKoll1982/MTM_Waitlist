using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Shared.Services;

namespace MTM_Waitlist.Tests.Module_Settings;

/// <summary>
/// Records the calls made to it so tests can assert which cascade branch was taken.
/// </summary>
internal sealed class FakeConfigSettingsValueService : IConfigSettingsValueService
{
    private readonly Dictionary<string, ConfigSettingValue> _values = new(StringComparer.OrdinalIgnoreCase);

    public int GetCallCount { get; private set; }

    public List<ConfigSettingValue> SavedValues { get; } = new();

    public List<string> DeletedKeys { get; } = new();

    public void SetText(string settingKey, string value) =>
        _values[settingKey] = new ConfigSettingValue { SettingKey = settingKey, SettingValue = value, ValueType = "text" };

    public void SetInt(string settingKey, long value) =>
        _values[settingKey] = new ConfigSettingValue { SettingKey = settingKey, SettingValueInt = value, ValueType = "int" };

    public void SetBool(string settingKey, bool value) =>
        _values[settingKey] = new ConfigSettingValue { SettingKey = settingKey, SettingValueBool = value, ValueType = "bool" };

    public Task<ConfigSettingValue?> GetSettingValueAsync(string settingKey, string scopeKey)
    {
        GetCallCount++;
        return Task.FromResult(_values.TryGetValue(settingKey, out var value) ? value : null);
    }

    public Task SetSettingValueAsync(ConfigSettingValue setting, long? updatedByUserId = null)
    {
        SavedValues.Add(setting);
        _values[setting.SettingKey] = setting;
        return Task.CompletedTask;
    }

    public Task DeleteSettingValueAsync(string settingKey, string scopeKey)
    {
        DeletedKeys.Add(settingKey);
        _values.Remove(settingKey);
        return Task.CompletedTask;
    }
}

internal sealed class FakeImageStorageConfigurationResolver : IImageStorageConfigurationResolver
{
    public string SharedFolderPath { get; set; } = Path.Combine(Path.GetTempPath(), "mtm-image-tests");

    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    public bool EnableArchiveVersioning { get; set; } = true;

    public int ArchiveKeepDays { get; set; } = 30;

    public bool RequireSquareAspectRatio { get; set; } = true;

    public Task<string> GetSharedFolderPathAsync() => Task.FromResult(SharedFolderPath);

    public Task<long> GetMaxFileSizeBytesAsync() => Task.FromResult(MaxFileSizeBytes);

    public Task<bool> GetEnableArchiveVersioningAsync() => Task.FromResult(EnableArchiveVersioning);

    public Task<int> GetArchiveKeepDaysAsync() => Task.FromResult(ArchiveKeepDays);

    public Task<ImageStorageOptions> GetEffectiveConfigurationAsync() => Task.FromResult(new ImageStorageOptions
    {
        SharedFolderPath = SharedFolderPath,
        MaxFileSizeBytes = MaxFileSizeBytes,
        AllowedExtensions = new[] { ".png", ".jpg", ".jpeg" },
        RequireSquareAspectRatio = RequireSquareAspectRatio,
        EnableArchiveVersioning = EnableArchiveVersioning,
        ArchiveKeepDays = ArchiveKeepDays
    });

    public void InvalidateCache()
    {
    }
}

internal sealed class FakeImageOverrideReadService : IImageOverrideReadService
{
    private readonly Dictionary<string, ImageOverride> _overrides = new(StringComparer.OrdinalIgnoreCase);

    public void AddOverride(string scope, string scopeItemId, string imagePath) =>
        _overrides[Key(scope, scopeItemId)] = new ImageOverride
        {
            Scope = scope,
            ScopeItemId = scopeItemId,
            ImagePath = imagePath,
            IsActive = true
        };

    public Task<ImageOverride?> GetOverrideAsync(string scope, string scopeItemId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_overrides.TryGetValue(Key(scope, scopeItemId), out var value) ? value : null);

    public Task<IReadOnlyList<ImageOverride>> GetOverridesByScopeAsync(string scope, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ImageOverride>>(
            _overrides.Values.Where(o => string.Equals(o.Scope, scope, StringComparison.OrdinalIgnoreCase)).ToList());

    public Task<bool> HasOverrideAsync(string scope, string scopeItemId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_overrides.ContainsKey(Key(scope, scopeItemId)));

    public Task<int> CountAllActiveOverridesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_overrides.Count);

    public Task<int> CountActiveOverridesByScopeAsync(string scope, CancellationToken cancellationToken = default) =>
        Task.FromResult(_overrides.Values.Count(o => string.Equals(o.Scope, scope, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<ImageOverride>> DetectOrphanedOverridesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ImageOverride>>(Array.Empty<ImageOverride>());

    public Task<ImageOverride?> GetOverrideByPublicIdAsync(string publicId, CancellationToken cancellationToken = default) =>
        Task.FromResult<ImageOverride?>(null);

    public Task<IReadOnlyList<ImageOverride>> GetRecentlyUpdatedOverridesAsync(int maxRecordCount = 100, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ImageOverride>>(Array.Empty<ImageOverride>());

    private static string Key(string scope, string scopeItemId) => $"{scope}|{scopeItemId}";
}

internal sealed class FakeRequestTypeDisplayLabelService : IRequestTypeDisplayLabelService
{
    public string GetCurrentDisplayName(Guid requestTypeId) => requestTypeId.ToString();

    public Guid? GetIdByCurrentDisplayName(string displayName) => null;

    public bool HasDisplayNameChanged(Guid requestTypeId) => false;

    public string? GetPreviousDisplayName(Guid requestTypeId) => null;

    public Task InitializeFromJsonAsync() => Task.CompletedTask;

    public Task<int> DetectDisplayNameChangesAsync() => Task.FromResult(0);
}

internal sealed class FakeRequestSubtypeDisplayLabelService : IRequestSubtypeDisplayLabelService
{
    public Guid ParentRequestTypeId { get; set; } = Guid.NewGuid();

    public string GetCurrentDisplayName(Guid subtypeId) => subtypeId.ToString();

    public Guid GetParentRequestTypeId(Guid subtypeId) => ParentRequestTypeId;

    public Guid? GetIdByDisplayName(Guid parentRequestTypeId, string subtypeDisplayName) => null;

    public bool HasDisplayNameChanged(Guid subtypeId) => false;

    public string? GetPreviousDisplayName(Guid subtypeId) => null;

    public Task InitializeFromJsonAsync() => Task.CompletedTask;

    public Task<int> DetectDisplayNameChangesAsync() => Task.FromResult(0);
}

internal sealed class FakeWorkCenterCatalogService : IWorkCenterCatalogService
{
    public WorkCenterCatalogResult Catalog { get; set; } = new();

    public string GetCurrentComputerName() => "test-workstation";

    public Task<IReadOnlyList<ComputerOption>> GetAvailableComputersAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ComputerOption>>(new[] { new ComputerOption { Key = "test-workstation", Label = "Test Workstation - test-workstation" } });

    public Task<WorkCenterCatalogResult> GetCatalogAsync(string workstationName, CancellationToken cancellationToken = default) =>
        Task.FromResult(Catalog);

    public Task<string?> SaveHotWorkCentersAsync(string workstationName, IReadOnlyCollection<string> hotWorkCenters, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);
}

internal sealed class FakeLocalSettingsService : ILocalSettingsService
{
    public Task<T?> ReadSettingAsync<T>(string key) => Task.FromResult<T?>(default);

    public Task SaveSettingAsync<T>(string key, T value) => Task.CompletedTask;

    public Task ResetSettingAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task ResetAsync() => Task.CompletedTask;

    public Task CorruptForTestAsync() => Task.CompletedTask;
}

internal sealed class FakeSampleDataService : ISampleDataService
{
    public IReadOnlyList<object> GetSampleOrders(string? building = null) => Array.Empty<object>();
}

internal static class TestDoubles
{
    /// <summary>
    /// The cascade tests never reach the database, but ImageLocationService requires a non-null helper.
    /// </summary>
    public static MySqlHelperServer CreateUnusedMySqlHelperServer() =>
        new(new FakeLocalSettingsService(), new FakeSampleDataService());
}

internal sealed record ExecutedStatement(string Sql, IReadOnlyDictionary<string, object?> Parameters);

/// <summary>
/// Scripted <see cref="IMySqlHelperServer"/> that records every statement it is asked to run.
/// Query and non-query results are queued independently, matching the two execution paths.
/// </summary>
internal sealed class FakeMySqlHelperServer : IMySqlHelperServer
{
    private readonly Queue<IReadOnlyList<Dictionary<string, object?>>> _queryResults = new();
    private readonly Queue<int> _nonQueryResults = new();

    public List<ExecutedStatement> ExecutedQueries { get; } = new();

    public List<ExecutedStatement> ExecutedNonQueries { get; } = new();

    public void EnqueueQueryResult(params Dictionary<string, object?>[] rows) =>
        _queryResults.Enqueue(rows.ToList());

    public void EnqueueEmptyQueryResult() =>
        _queryResults.Enqueue(Array.Empty<Dictionary<string, object?>>());

    public void EnqueueNonQueryResult(int affectedRows) =>
        _nonQueryResults.Enqueue(affectedRows);

    public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
        string sql,
        IReadOnlyDictionary<string, object?> parameters,
        MySqlDatabaseTarget databaseTarget,
        CancellationToken cancellationToken = default)
    {
        ExecutedQueries.Add(new ExecutedStatement(sql, parameters));
        var result = _queryResults.Count > 0
            ? _queryResults.Dequeue()
            : Array.Empty<Dictionary<string, object?>>();
        return Task.FromResult(result);
    }

    public Task<int> ExecuteSqlNonQueryAsync(
        string sql,
        IReadOnlyDictionary<string, object?> parameters,
        MySqlDatabaseTarget databaseTarget,
        CancellationToken cancellationToken = default)
    {
        ExecutedNonQueries.Add(new ExecutedStatement(sql, parameters));
        return Task.FromResult(_nonQueryResults.Count > 0 ? _nonQueryResults.Dequeue() : 0);
    }

    public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
        string storedProcedureName,
        IReadOnlyDictionary<string, object?> parameters,
        MySqlDatabaseTarget databaseTarget,
        CancellationToken cancellationToken = default)
    {
        ExecutedQueries.Add(new ExecutedStatement(storedProcedureName, parameters));
        var result = _queryResults.Count > 0
            ? _queryResults.Dequeue()
            : Array.Empty<Dictionary<string, object?>>();
        return Task.FromResult(result);
    }

    public Task<int> ExecuteStoredProcedureNonQueryAsync(
        string storedProcedureName,
        IReadOnlyDictionary<string, object?> parameters,
        MySqlDatabaseTarget databaseTarget,
        CancellationToken cancellationToken = default)
    {
        ExecutedNonQueries.Add(new ExecutedStatement(storedProcedureName, parameters));
        return Task.FromResult(_nonQueryResults.Count > 0 ? _nonQueryResults.Dequeue() : 0);
    }

    public static Dictionary<string, object?> OverrideRow(
        string scope,
        string scopeItemId,
        string imagePath,
        bool isActive = true,
        long id = 1,
        string? publicId = null) => new(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = id,
            ["public_id"] = publicId ?? Guid.NewGuid().ToString("D"),
            ["scope"] = scope,
            ["scope_item_id"] = scopeItemId,
            ["image_path"] = imagePath,
            ["is_active"] = isActive ? 1 : 0,
            ["created_by_user_id"] = null,
            ["updated_by_user_id"] = null,
            ["created_utc"] = DateTime.UtcNow,
            ["updated_utc"] = DateTime.UtcNow
        };
}
