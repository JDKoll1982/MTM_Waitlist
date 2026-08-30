using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Shared.Services;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Implementation of IImageLocationService.
/// Orchestrates all image location management including inventories, display labels, configuration, and notifications.
/// Thread-safe for concurrent access; maintains initialization state.
/// </summary>
public sealed class ImageLocationService : IImageLocationService, IWorkCenterImageService, IDisposable
{
    private readonly ILogger<ImageLocationService> _logger;
    private readonly IRequestTypeDisplayLabelService _requestTypeDisplayLabelService;
    private readonly IRequestSubtypeDisplayLabelService _requestSubtypeDisplayLabelService;
    private readonly IImageOverrideReadService _imageOverrideReadService;
    private readonly IImageStorageConfigurationResolver _configurationResolver;
    private readonly IWorkCenterCatalogService _workCenterCatalogService;
    private readonly IMySqlHelperServer _mySqlHelperServer;

    private volatile bool _isInitialized;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private bool _disposed;

    // Change notification event
    private event EventHandler<ImageLocationChangedEventArgs>? ImageLocationChanged;

    /// <summary>
    /// Initializes a new ImageLocationService.
    /// All dependencies must be provided; null dependencies throw ArgumentNullException.
    /// </summary>
    /// <param name="logger">Logger for diagnostics and error logging</param>
    /// <param name="requestTypeDisplayLabelService">Service for request type display labels</param>
    /// <param name="requestSubtypeDisplayLabelService">Service for subtype display labels</param>
    /// <param name="configurationResolver">Service for resolving image storage configuration</param>
    /// <param name="workCenterCatalogService">Service for accessing work center catalog data</param>
    /// <exception cref="ArgumentNullException">If any dependency is null</exception>
    public ImageLocationService(
        ILogger<ImageLocationService> logger,
        IRequestTypeDisplayLabelService requestTypeDisplayLabelService,
        IRequestSubtypeDisplayLabelService requestSubtypeDisplayLabelService,
        IImageOverrideReadService imageOverrideReadService,
        IImageStorageConfigurationResolver configurationResolver,
        IWorkCenterCatalogService workCenterCatalogService,
        IMySqlHelperServer mySqlHelperServer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _requestTypeDisplayLabelService = requestTypeDisplayLabelService ?? 
            throw new ArgumentNullException(nameof(requestTypeDisplayLabelService));
        _requestSubtypeDisplayLabelService = requestSubtypeDisplayLabelService ?? 
            throw new ArgumentNullException(nameof(requestSubtypeDisplayLabelService));
        _imageOverrideReadService = imageOverrideReadService ??
            throw new ArgumentNullException(nameof(imageOverrideReadService));
        _configurationResolver = configurationResolver ?? 
            throw new ArgumentNullException(nameof(configurationResolver));
        _workCenterCatalogService = workCenterCatalogService ?? 
            throw new ArgumentNullException(nameof(workCenterCatalogService));
        _mySqlHelperServer = mySqlHelperServer ??
            throw new ArgumentNullException(nameof(mySqlHelperServer));

        _isInitialized = false;
    }

    /// <inheritdoc />
    public bool IsInitialized => _isInitialized;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            _logger.LogInformation("Image location service already initialized; skipping re-initialization");
            return;
        }

        await _initializationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_isInitialized)
            {
                _logger.LogInformation("Image location service already initialized (verified under lock); skipping");
                return;
            }

            _logger.LogInformation("Initializing image location service...");

            try
            {
                // Initialize all sub-services
                _logger.LogDebug("Loading request type display labels...");
                await _requestTypeDisplayLabelService.InitializeFromJsonAsync().ConfigureAwait(false);

                _logger.LogDebug("Loading subtype display labels...");
                await _requestSubtypeDisplayLabelService.InitializeFromJsonAsync().ConfigureAwait(false);

                // Validate that inventories are loaded
                if (!RequestTypeInventory.Items.Any())
                {
                    throw new InvalidOperationException(
                        "Request type inventory is empty after initialization. JSON configuration may not be loaded.");
                }

                if (!RequestSubtypeInventory.Groups.Any())
                {
                    throw new InvalidOperationException(
                        "Request subtype inventory is empty after initialization. JSON configuration may not be loaded.");
                }

                // Validate configuration
                _logger.LogDebug("Validating image storage configuration...");
                var config = await _configurationResolver.GetEffectiveConfigurationAsync().ConfigureAwait(false);
                config.Validate();

                _isInitialized = true;
                _logger.LogInformation("Successfully initialized image location service");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize image location service");
                _isInitialized = false;
                throw new InvalidOperationException(
                    "Image location service initialization failed. Application cannot proceed.", ex);
            }
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _initializationLock.Dispose();
        _disposed = true;
    }

    /// <inheritdoc />
    public string GetRequestTypeDisplayName(Guid requestTypeId)
    {
        if (!_isInitialized)
        {
            var message = "Image location service not initialized. Call InitializeAsync() first.";
            _logger.LogError(message);
            throw new InvalidOperationException(message);
        }

        if (requestTypeId == Guid.Empty)
        {
            throw new ArgumentException("Request type ID cannot be empty.", nameof(requestTypeId));
        }

        try
        {
            return _requestTypeDisplayLabelService.GetCurrentDisplayName(requestTypeId);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Request type ID '{Id}' not found in inventory", requestTypeId);
            throw;
        }
    }

    /// <inheritdoc />
    public string GetSubtypeDisplayName(Guid subtypeId)
    {
        if (!_isInitialized)
        {
            var message = "Image location service not initialized. Call InitializeAsync() first.";
            _logger.LogError(message);
            throw new InvalidOperationException(message);
        }

        if (subtypeId == Guid.Empty)
        {
            throw new ArgumentException("Subtype ID cannot be empty.", nameof(subtypeId));
        }

        try
        {
            return _requestSubtypeDisplayLabelService.GetCurrentDisplayName(subtypeId);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Subtype ID '{Id}' not found in inventory", subtypeId);
            throw;
        }
    }

    /// <inheritdoc />
    public Guid GetSubtypeParentId(Guid subtypeId)
    {
        if (!_isInitialized)
        {
            var message = "Image location service not initialized. Call InitializeAsync() first.";
            _logger.LogError(message);
            throw new InvalidOperationException(message);
        }

        if (subtypeId == Guid.Empty)
        {
            throw new ArgumentException("Subtype ID cannot be empty.", nameof(subtypeId));
        }

        try
        {
            return _requestSubtypeDisplayLabelService.GetParentRequestTypeId(subtypeId);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Subtype ID '{Id}' not found in inventory", subtypeId);
            throw;
        }
    }

    /// <inheritdoc />
    public bool IsValidRequestTypeId(Guid requestTypeId)
    {
        if (!_isInitialized)
        {
            _logger.LogWarning("IsValidRequestTypeId called before initialization");
            return false;
        }

        if (requestTypeId == Guid.Empty)
        {
            return false;
        }

        try
        {
            _ = _requestTypeDisplayLabelService.GetCurrentDisplayName(requestTypeId);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public bool IsValidSubtypeId(Guid subtypeId)
    {
        if (!_isInitialized)
        {
            _logger.LogWarning("IsValidSubtypeId called before initialization");
            return false;
        }

        if (subtypeId == Guid.Empty)
        {
            return false;
        }

        try
        {
            _ = _requestSubtypeDisplayLabelService.GetCurrentDisplayName(subtypeId);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public bool IsValidWorkCenterId(long workCenterId)
    {
        if (!_isInitialized)
        {
            _logger.LogWarning("IsValidWorkCenterId called before initialization");
            return false;
        }

        if (workCenterId <= 0)
        {
            return false;
        }

        // TODO: Implement work center validation against IWorkCenterCatalogService
        // For now, accept all positive IDs; validation will be done in next phase
        _logger.LogDebug("Validating work center ID: {WorkCenterId}", workCenterId);
        return true;
    }

    /// <inheritdoc />
    public async Task<string> ResolveRequestTypeImagePathAsync(string requestTypeId, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            var message = "Image location service not initialized. Call InitializeAsync() first.";
            _logger.LogError(message);
            throw new InvalidOperationException(message);
        }

        if (string.IsNullOrWhiteSpace(requestTypeId))
        {
            throw new ArgumentException("Request type ID cannot be null or empty.", nameof(requestTypeId));
        }

        if (!Guid.TryParse(requestTypeId, out var typeId))
        {
            throw new ArgumentException("Request type ID must be a valid GUID.", nameof(requestTypeId));
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var defaultPath = ImageLocationDefaults.RequestTypeDefaultPath;

            // Cascade order: database override -> JSON imagePath -> default asset.
            var overridePath = await _imageOverrideReadService.GetOverrideAsync("request_type", typeId.ToString(), cancellationToken).ConfigureAwait(false);
            if (overridePath is not null && !string.IsNullOrWhiteSpace(overridePath.ImagePath))
            {
                return await ResolveExistingPathAsync(overridePath.ImagePath, defaultPath, "request_type", requestTypeId).ConfigureAwait(false);
            }

            var jsonPath = await TryResolveJsonRequestTypeImagePathAsync(typeId).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(jsonPath))
            {
                return await ResolveExistingPathAsync(jsonPath, defaultPath, "request_type", requestTypeId).ConfigureAwait(false);
            }

            return defaultPath;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve request type image path for {RequestTypeId}", requestTypeId);
            return ImageLocationDefaults.RequestTypeDefaultPath;
        }
    }

    /// <inheritdoc />
    public async Task<string> ResolveRequestSubtypeImagePathAsync(string subtypeId, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            var message = "Image location service not initialized. Call InitializeAsync() first.";
            _logger.LogError(message);
            throw new InvalidOperationException(message);
        }

        if (string.IsNullOrWhiteSpace(subtypeId))
        {
            throw new ArgumentException("Subtype ID cannot be null or empty.", nameof(subtypeId));
        }

        if (!Guid.TryParse(subtypeId, out var typeId))
        {
            throw new ArgumentException("Subtype ID must be a valid GUID.", nameof(subtypeId));
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var defaultPath = ImageLocationDefaults.RequestSubtypeDefaultPath;
            var overridePath = await _imageOverrideReadService.GetOverrideAsync("request_subtype", typeId.ToString(), cancellationToken).ConfigureAwait(false);
            if (overridePath is not null && !string.IsNullOrWhiteSpace(overridePath.ImagePath))
            {
                return await ResolveExistingPathAsync(overridePath.ImagePath, defaultPath, "request_subtype", subtypeId).ConfigureAwait(false);
            }

            var jsonPath = await TryResolveJsonSubtypeImagePathAsync(typeId).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(jsonPath))
            {
                return await ResolveExistingPathAsync(jsonPath, defaultPath, "request_subtype", subtypeId).ConfigureAwait(false);
            }

            var parentRequestTypeId = _requestSubtypeDisplayLabelService.GetParentRequestTypeId(typeId);
            var parentImagePath = await ResolveRequestTypeImagePathAsync(parentRequestTypeId.ToString(), cancellationToken).ConfigureAwait(false);
            return await ResolveExistingPathAsync(parentImagePath, defaultPath, "request_subtype", subtypeId).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve subtype image path for {SubtypeId}", subtypeId);
            return ImageLocationDefaults.RequestSubtypeDefaultPath;
        }
    }

    /// <inheritdoc />
    public async Task<string> ResolveWorkCenterImagePathAsync(string workCenterId, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            var message = "Image location service not initialized. Call InitializeAsync() first.";
            _logger.LogError(message);
            throw new InvalidOperationException(message);
        }

        if (string.IsNullOrWhiteSpace(workCenterId))
        {
            throw new ArgumentException("Work center ID cannot be null or empty.", nameof(workCenterId));
        }

        if (!long.TryParse(workCenterId, out var id) || id <= 0)
        {
            throw new ArgumentException("Work center ID must be a positive integer.", nameof(workCenterId));
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var defaultPath = ImageLocationDefaults.WorkCenterDefaultPath;
            var overridePath = await _imageOverrideReadService.GetOverrideAsync("work_center", id.ToString(), cancellationToken).ConfigureAwait(false);
            if (overridePath is not null && !string.IsNullOrWhiteSpace(overridePath.ImagePath))
            {
                return await ResolveExistingPathAsync(overridePath.ImagePath, defaultPath, "work_center", workCenterId).ConfigureAwait(false);
            }

            return defaultPath;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve work center image path for {WorkCenterId}", workCenterId);
            return ImageLocationDefaults.WorkCenterDefaultPath;
        }
    }

    /// <inheritdoc />
    public async Task<string> GetSharedFolderPathAsync()
    {
        if (!_isInitialized)
        {
            var message = "Image location service not initialized. Call InitializeAsync() first.";
            _logger.LogError(message);
            throw new InvalidOperationException(message);
        }

        try
        {
            return await _configurationResolver.GetSharedFolderPathAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve shared folder path");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> DetectConfigurationChangesAsync()
    {
        if (!_isInitialized)
        {
            _logger.LogWarning("DetectConfigurationChangesAsync called before initialization");
            return 0;
        }

        try
        {
            _logger.LogInformation("Detecting configuration changes...");

            var requestTypeChanges = await _requestTypeDisplayLabelService.DetectDisplayNameChangesAsync();
            var subtypeChanges = await _requestSubtypeDisplayLabelService.DetectDisplayNameChangesAsync();

            var totalChanges = requestTypeChanges + subtypeChanges;

            if (totalChanges > 0)
            {
                _logger.LogWarning("Detected {Count} configuration changes: {RequestTypeChanges} request types, {SubtypeChanges} subtypes",
                                 totalChanges, requestTypeChanges, subtypeChanges);
            }
            else
            {
                _logger.LogInformation("No configuration changes detected");
            }

            return totalChanges;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect configuration changes");
            throw;
        }
    }

    /// <inheritdoc />
    public void RaiseImageLocationUpdated(string scope, string scopeId)
    {
        if (string.IsNullOrWhiteSpace(scope) || string.IsNullOrWhiteSpace(scopeId))
        {
            _logger.LogWarning("RaiseImageLocationUpdated called with null or empty scope/scopeId");
            return;
        }

        try
        {
            _logger.LogInformation("Raising image location updated notification: scope={Scope}, scopeId={ScopeId}",
                                 scope, scopeId);

            var args = new ImageLocationChangedEventArgs
            {
                Scope = scope,
                ScopeId = scopeId,
                ChangeType = "updated",
                ChangedAtUtc = DateTime.UtcNow
            };

            ImageLocationChanged?.Invoke(this, args);

            _logger.LogDebug("Image location change notification raised to {SubscriberCount} subscribers",
                           ImageLocationChanged?.GetInvocationList().Length ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error raising image location updated notification");
            // Don't throw; notification failures shouldn't break the calling code
        }
    }

    /// <inheritdoc />
    public IDisposable SubscribeToImageLocationChanges(Action<ImageLocationChangedEventArgs> handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        _logger.LogDebug("Subscribing to image location change notifications");

        // Wrap the handler to enable unsubscription
        EventHandler<ImageLocationChangedEventArgs> wrappedHandler = (sender, args) =>
        {
            try
            {
                handler(args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in image location change handler");
                // Don't propagate; handler errors shouldn't break the notification system
            }
        };

        ImageLocationChanged += wrappedHandler;

        // Return a disposable that unsubscribes. Uses a closure-based token so no nested type
        // holds a back-reference to this service; keeps the dependency graph acyclic.
        bool disposed = false;
        return new SubscriptionToken(() =>
        {
            if (disposed)
            {
                return;
            }

            _logger.LogDebug("Unsubscribing from image location change notifications");
            ImageLocationChanged -= wrappedHandler;
            disposed = true;
        });
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WorkCenterItem>?> GetActiveWorkCentersAsync(CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            var message = "Image location service not initialized. Call InitializeAsync() first.";
            _logger.LogError(message);
            throw new InvalidOperationException(message);
        }

        try
        {
            _logger.LogInformation("Loading active work centers from catalog...");

            // An empty workstation name makes the catalog service resolve the current workstation.
            var catalogResult = await _workCenterCatalogService.GetCatalogAsync(string.Empty, cancellationToken);

            if (catalogResult == null)
            {
                _logger.LogWarning("Work center catalog returned null result; database may be unavailable");
                return null;
            }

            // Get all work centers (Local + other) from the catalog
            var allWorkCenterNames = catalogResult.HotWorkCenters
                .Concat(catalogResult.OtherWorkCenters)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            _logger.LogDebug("Retrieved {Count} work centers from catalog: {Hot} hot, {Other} other",
                           allWorkCenterNames.Count, catalogResult.HotWorkCenters.Count, catalogResult.OtherWorkCenters.Count);

            // Convert work center names to WorkCenterItem objects by querying the database
            // We need to fetch the full work center details (ID, building, sort_rank, is_active)
            var workCenterItems = await LoadWorkCenterDetailsAsync(allWorkCenterNames, cancellationToken);

            if (workCenterItems == null || workCenterItems.Count == 0)
            {
                _logger.LogWarning("No work centers found or database query failed");
                return null;
            }

            _logger.LogInformation("Successfully loaded {Count} active work centers", workCenterItems.Count);
            return workCenterItems;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Work center loading was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load active work centers");
            return null;
        }
    }

    /// <summary>
    /// Loads full WorkCenterItem details (ID, building, sort_rank) for given work center names.
    /// This helper method queries the database to get the complete work center inventory.
    /// </summary>
    private async Task<IReadOnlyList<WorkCenterItem>?> LoadWorkCenterDetailsAsync(
        IReadOnlyList<string> workCenterNames, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Loading work center details for {Count} work centers", workCenterNames.Count);

        if (workCenterNames.Count == 0)
        {
            return new List<WorkCenterItem>();
        }

        // Parameter names are generated, never interpolated from the caller's values.
        var parameters = new Dictionary<string, object?>();
        var placeholders = new List<string>(workCenterNames.Count);
        for (var i = 0; i < workCenterNames.Count; i++)
        {
            var parameterName = $"@p_name_{i}";
            placeholders.Add(parameterName);
            parameters[parameterName] = workCenterNames[i];
        }

        var sql = $@"SELECT
    id,
    work_center_name,
    building,
    sort_rank,
    is_active
FROM setup_work_centers_catalog
WHERE is_active = 1
  AND work_center_name IN ({string.Join(", ", placeholders)})
ORDER BY building ASC, sort_rank ASC, work_center_name ASC;";

        var rows = await _mySqlHelperServer.ExecuteSqlQueryAsync(
            sql,
            parameters,
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        var items = new List<WorkCenterItem>(rows.Count);
        foreach (var row in rows)
        {
            items.Add(new WorkCenterItem
            {
                WorkCenterId = ReadInt64(row, "id"),
                DisplayName = ReadString(row, "work_center_name"),
                Building = ReadString(row, "building"),
                SortRank = (int)ReadInt64(row, "sort_rank"),
                IsActive = ReadBoolean(row, "is_active")
            });
        }

        _logger.LogDebug("Loaded {Count} work center detail rows", items.Count);
        return items;
    }

    private static string ReadString(IReadOnlyDictionary<string, object?> row, string key) =>
        row.TryGetValue(key, out var value) && value is not null ? value.ToString() ?? string.Empty : string.Empty;

    private static long ReadInt64(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return 0;
        }

        return value is long longValue ? longValue : Convert.ToInt64(value);
    }

    private static bool ReadBoolean(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return false;
        }

        return value is bool boolValue ? boolValue : Convert.ToInt32(value) != 0;
    }

    private async Task<string> ResolveExistingPathAsync(string candidatePath, string fallbackPath, string scope, string scopeItemId)
    {
        if (string.IsNullOrWhiteSpace(candidatePath))
        {
            _logger.LogWarning("No path available for {Scope}:{ScopeItemId}; falling back to default asset", scope, scopeItemId);
            return fallbackPath;
        }

        var normalized = candidatePath.Trim();
        var exists = DoesPathExist(normalized);
        if (exists)
        {
            return normalized;
        }

        _logger.LogWarning("Resolved image path does not exist for {Scope}:{ScopeItemId}; using default asset. Path={Path}", scope, scopeItemId, normalized);
        return fallbackPath;
    }

    private static bool DoesPathExist(string candidatePath)
    {
        if (string.IsNullOrWhiteSpace(candidatePath))
        {
            return false;
        }

        var target = candidatePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(target))
        {
            return File.Exists(target);
        }

        var appRelative = Path.Combine(AppContext.BaseDirectory, target);
        return File.Exists(appRelative);
    }

    private static async Task<string?> TryResolveJsonRequestTypeImagePathAsync(Guid requestTypeId)
    {
        var requestTypes = await LoadJsonImageConfigAsync().ConfigureAwait(false);
        return requestTypes.TryGetValue(requestTypeId.ToString(), out var value) ? value : null;
    }

    private static async Task<string?> TryResolveJsonSubtypeImagePathAsync(Guid subtypeId)
    {
        var requestTypes = await LoadJsonImageConfigAsync().ConfigureAwait(false);
        return requestTypes.TryGetValue(subtypeId.ToString(), out var value) ? value : null;
    }

    private static async Task<Dictionary<string, string>> LoadJsonImageConfigAsync()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Config", "waitlist-request-types.json");
        if (!File.Exists(configPath))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        await using var stream = File.OpenRead(configPath);
        using var document = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);
        var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return mappings;
        }

        foreach (var rootItem in document.RootElement.EnumerateArray())
        {
            if (rootItem.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (rootItem.TryGetProperty("id", out var requestTypeIdElement) &&
                rootItem.TryGetProperty("imagePath", out var requestTypePathElement) &&
                !string.IsNullOrWhiteSpace(requestTypePathElement.GetString()))
            {
                mappings[requestTypeIdElement.GetString() ?? string.Empty] = requestTypePathElement.GetString() ?? string.Empty;
            }

            if (rootItem.TryGetProperty("subtypes", out var subtypesElement) && subtypesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var subtype in subtypesElement.EnumerateArray())
                {
                    if (subtype.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    if (subtype.TryGetProperty("id", out var subtypeIdElement) &&
                        subtype.TryGetProperty("imagePath", out var subtypePathElement) &&
                        !string.IsNullOrWhiteSpace(subtypePathElement.GetString()))
                    {
                        mappings[subtypeIdElement.GetString() ?? string.Empty] = subtypePathElement.GetString() ?? string.Empty;
                    }
                }
            }
        }

        return mappings;
    }

    /// <summary>
    /// Lightweight closure-based disposable token for unsubscribing from change notifications.
    /// Depends only on a dispose delegate (not the owning service), keeping the dependency
    /// graph free of the previous nested-type back-reference cycle.
    /// </summary>
    private sealed class SubscriptionToken : IDisposable
    {
        private readonly Action _dispose;
        private bool _disposed;

        public SubscriptionToken(Action dispose)
        {
            _dispose = dispose ?? throw new ArgumentNullException(nameof(dispose));
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _dispose();
            _disposed = true;
        }
    }
}
