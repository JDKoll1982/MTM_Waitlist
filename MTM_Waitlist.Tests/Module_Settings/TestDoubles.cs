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

/// <summary>
/// A picture cache that records being asked, and can be told to fail or to report a particular result.
/// </summary>
internal sealed class FakeImageCacheSyncService : IImageCacheSyncService
{
    public int SynchronizeCallCount { get; private set; }

    public ImageCacheSyncResult Result { get; set; } = ImageCacheSyncResult.NothingToDo;

    public Exception? Failure { get; set; }

    public Task<ImageCacheSyncResult> SynchronizeAsync(CancellationToken cancellationToken = default)
    {
        SynchronizeCallCount++;

        return Failure is null
            ? Task.FromResult(Result)
            : Task.FromException<ImageCacheSyncResult>(Failure);
    }
}

internal sealed class FakeImageStorageConfigurationResolver : IImageStorageConfigurationResolver
{
    public string SharedFolderPath { get; set; } = Path.Combine(Path.GetTempPath(), "mtm-image-tests");

    public string KeysFolderPath { get; set; } = Path.Combine(Path.GetTempPath(), "mtm-image-tests", "key-files");

    public string CacheFolderPath { get; set; } = Path.Combine(Path.GetTempPath(), "mtm-image-tests", "cache");

    public bool CacheEnabled { get; set; } = true;

    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    public bool EnableArchiveVersioning { get; set; } = true;

    public int ArchiveKeepDays { get; set; } = 30;

    /// <summary>This machine's own configured folder, named when it differs from the folder every computer reads.</summary>
    public string MachineSharedFolderPath { get; set; } = string.Empty;

    public bool RequireSquareAspectRatio { get; set; } = true;

    public Task<string> GetSharedFolderPathAsync() => Task.FromResult(SharedFolderPath);

    public Task<string> GetKeysFolderPathAsync() => Task.FromResult(KeysFolderPath);

    public Task<string> GetImageCacheFolderPathAsync() => Task.FromResult(CacheFolderPath);

    public Task<bool> GetImageCacheEnabledAsync() => Task.FromResult(CacheEnabled);

    public Task<long> GetMaxFileSizeBytesAsync() => Task.FromResult(MaxFileSizeBytes);

    public Task<bool> GetEnableArchiveVersioningAsync() => Task.FromResult(EnableArchiveVersioning);

    public Task<int> GetArchiveKeepDaysAsync() => Task.FromResult(ArchiveKeepDays);

    public Task<SharedFolderResolution> GetSharedFolderResolutionAsync() =>
        Task.FromResult(new SharedFolderResolution(SharedFolderPath, MachineSharedFolderPath));

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

    /// <summary>When set, every whole-scope read fails. Used to prove a save is refused when the pictured set cannot be read.</summary>
    public Exception? FailScopeReads { get; set; }

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
        FailScopeReads is not null
            ? Task.FromException<IReadOnlyList<ImageOverride>>(FailScopeReads)
            : Task.FromResult<IReadOnlyList<ImageOverride>>(
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

internal static class TestDoubles
{
    /// <summary>
    /// The cascade tests never reach the database, but ImageLocationService requires a non-null helper.
    /// It no longer takes a settings/sample-data dependency: internal stores are never mocked (FR-001).
    /// </summary>
    public static MySqlHelperServer CreateUnusedMySqlHelperServer() => new();
}

/// <summary>
/// The Item-keyed allotment store, scripted. Records every read and every write so a test can prove which
/// Item was asked for, what was written, and — just as importantly — that nothing was written at all.
/// </summary>
internal sealed class FakeRequestItemAllottedMinutesStore : IRequestItemAllottedMinutesStore
{
    private readonly Dictionary<string, int> _configured = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>When set, every read fails. Used to prove the labelled default survives an unreachable store.</summary>
    public Exception? ReadFailure { get; set; }

    /// <summary>When set, every write fails. Used to prove a failed write is reported rather than swallowed.</summary>
    public Exception? WriteFailure { get; set; }

    public List<string> ItemsRead { get; } = new();

    public List<(string Item, int Minutes)> Writes { get; } = new();

    public void Configure(string itemCode, int minutes) => _configured[itemCode] = minutes;

    public Task<int?> GetAllottedMinutesAsync(string itemCode, CancellationToken cancellationToken = default)
    {
        ItemsRead.Add(itemCode);

        if (ReadFailure is not null)
        {
            return Task.FromException<int?>(ReadFailure);
        }

        return Task.FromResult(_configured.TryGetValue(itemCode ?? string.Empty, out var minutes)
            ? (int?)minutes
            : null);
    }

    public Task SetAllottedMinutesAsync(string itemCode, int minutes, CancellationToken cancellationToken = default)
    {
        Writes.Add((itemCode, minutes));

        return WriteFailure is not null
            ? Task.FromException(WriteFailure)
            : Task.CompletedTask;
    }
}

/// <summary>
/// The configuration read, scripted: it answers for the Items a test configures and reports a missing row the
/// way the real reader does, so a caller sees the same unavailable shape either way.
/// </summary>
internal sealed class FakeRequestItemConfigurationService : IRequestItemConfigurationService
{
    private readonly Dictionary<string, RequestItemConfiguration> _byItem = new(StringComparer.OrdinalIgnoreCase);

    public List<string> ItemsRead { get; } = new();

    public Exception? Failure { get; set; }

    public void Configure(RequestItemConfiguration configuration) => _byItem[configuration.Item] = configuration;

    public void Configure(string itemCode, int? allottedMinutes) => _byItem[itemCode] = new RequestItemConfiguration
    {
        Item = itemCode,
        Category = RequestItemCatalog.FindById(itemCode)?.Category.ToString() ?? string.Empty,
        AllottedMinutes = allottedMinutes,
    };

    public Task<RequestItemConfigurationSet> GetConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        if (Failure is not null)
        {
            return Task.FromException<RequestItemConfigurationSet>(Failure);
        }

        return Task.FromResult(RequestItemConfigurationSet.From(_byItem.Values));
    }

    public Task<RequestItemConfiguration> GetConfigurationAsync(string itemCode, CancellationToken cancellationToken = default)
    {
        ItemsRead.Add(itemCode);

        if (Failure is not null)
        {
            return Task.FromException<RequestItemConfiguration>(Failure);
        }

        var normalized = itemCode?.Trim() ?? string.Empty;

        return Task.FromResult(_byItem.TryGetValue(normalized, out var configuration)
            ? configuration
            : RequestItemConfiguration.Missing(
                normalized,
                RequestItemConfigurationSet.UnavailableMessageKey,
                RequestItemConfigurationSet.ResolveUnavailableMessage()));
    }
}

/// <summary>
/// The configured/observed pair source, scripted. Carries no store of its own: it answers exactly the pairs a
/// test hands it, so a screen's columns are proved from the pair rather than from a second read path.
/// </summary>
internal sealed class FakeRequestItemObservedTimeService : IRequestItemObservedTimeService
{
    public List<RequestItemObservedTime> Times { get; } = new();

    public Exception? Failure { get; set; }

    public void Add(string item, TimeSpan? configuredMinutes, TimeSpan? observedAverage, bool isConfiguredValueDefault = false)
    {
        Times.Add(new RequestItemObservedTime
        {
            Item = item,
            DisplayName = item,
            ConfiguredMinutes = configuredMinutes ?? TimeSpan.FromMinutes(15),
            IsConfiguredValueDefault = isConfiguredValueDefault || configuredMinutes is null,
            CompletedRequestCount = observedAverage.HasValue ? 1 : 0,
            ObservedAverage = observedAverage,
        });
    }

    public Task<IReadOnlyList<RequestItemObservedTime>> GetObservedTimesAsync(CancellationToken cancellationToken = default)
        => Failure is not null
            ? Task.FromException<IReadOnlyList<RequestItemObservedTime>>(Failure)
            : Task.FromResult<IReadOnlyList<RequestItemObservedTime>>(Times);
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

/// <summary>
/// The picture-row writer, recorded rather than executed. Proves whether a save created a row or replaced one,
/// and — for a refusal — that neither happened.
/// </summary>
/// <remarks>
/// The change record itself is written by the store's triggers, so it is proved against a live database in
/// <c>ConfigImagesLocationsHistoryIntegrationTests</c> rather than asserted here.
/// </remarks>
internal sealed class RecordingOverrideWriteService : IImageOverrideWriteService
{
    private readonly Dictionary<string, string> _paths = new(StringComparer.OrdinalIgnoreCase);

    public List<(string Scope, string Item, string Path)> Creates { get; } = new();

    public List<(string Scope, string Item, string Path)> Updates { get; } = new();

    public Exception? Failure { get; set; }

    /// <summary>
    /// Invoked after a write is recorded, so a test can mirror it into the reader it also passes to the service
    /// under test. A real write is visible to a real read; a fake write has to be told to be.
    /// </summary>
    public Action<string, string, string>? Written { get; set; }

    public void Seed(string scope, string scopeItemId, string imagePath) => _paths[Key(scope, scopeItemId)] = imagePath;

    public Task<ImageOverrideWriteResult> CreateOverrideAsync(
        string scope,
        string scopeItemId,
        string imagePath,
        long? userId = null,
        CancellationToken cancellationToken = default)
    {
        if (Failure is not null)
        {
            return Task.FromException<ImageOverrideWriteResult>(Failure);
        }

        Creates.Add((scope, scopeItemId, imagePath));
        _paths[Key(scope, scopeItemId)] = imagePath;
        Written?.Invoke(scope, scopeItemId, imagePath);

        return Task.FromResult(Result(scope, scopeItemId, "CREATE"));
    }

    public Task<ImageOverrideWriteResult> UpdateOverrideAsync(
        string scope,
        string scopeItemId,
        string newImagePath,
        long? userId = null,
        CancellationToken cancellationToken = default)
    {
        if (Failure is not null)
        {
            return Task.FromException<ImageOverrideWriteResult>(Failure);
        }

        Updates.Add((scope, scopeItemId, newImagePath));
        _paths[Key(scope, scopeItemId)] = newImagePath;
        Written?.Invoke(scope, scopeItemId, newImagePath);

        return Task.FromResult(Result(scope, scopeItemId, "UPDATE"));
    }

    public Task<ImageOverrideWriteResult> DeleteOverrideAsync(
        string scope,
        string scopeItemId,
        long? userId = null,
        CancellationToken cancellationToken = default)
    {
        _paths.Remove(Key(scope, scopeItemId));
        return Task.FromResult(Result(scope, scopeItemId, "DELETE"));
    }

    public Task<ImageOverrideWriteResult> DeleteByPublicIdAsync(
        string publicId,
        long? userId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new ImageOverrideWriteResult { Success = true, OperationType = "DELETE" });

    public Task<bool> DeleteIfExistsAsync(
        string scope,
        string scopeItemId,
        long? userId = null,
        CancellationToken cancellationToken = default)
    {
        var removed = _paths.Remove(Key(scope, scopeItemId));
        return Task.FromResult(removed);
    }

    public Task<int> PurgeInactiveOverridesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

    public Task<int> DeactivateAllForScopeAsync(
        string scope,
        long? userId = null,
        CancellationToken cancellationToken = default) => Task.FromResult(0);

    private static string Key(string scope, string scopeItemId) => $"{scope}|{scopeItemId}";

    private static ImageOverrideWriteResult Result(string scope, string scopeItemId, string operationType) => new()
    {
        Success = true,
        OperationType = operationType,
        AffectedScope = scope,
        AffectedScopeItemId = scopeItemId,
    };
}

/// <summary>
/// One source of part numbers the application can name, scripted, so the coverage subtraction can be proved
/// against exactly the parts a test hands it.
/// </summary>
internal sealed class StubPartNumberSource : IPartNumberSource
{
    public StubPartNumberSource(PartPictureSystem system, params string[] partNumbers)
    {
        System = system;
        PartNumbers = partNumbers.ToList();
    }

    public PartPictureSystem System { get; }

    public List<string> PartNumbers { get; }

    public Exception? Failure { get; set; }

    public Task<IReadOnlyList<string>> GetPartNumbersAsync(CancellationToken cancellationToken = default) =>
        Failure is null
            ? Task.FromResult<IReadOnlyList<string>>(PartNumbers)
            : Task.FromException<IReadOnlyList<string>>(Failure);
}
