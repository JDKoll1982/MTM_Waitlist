using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
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
    /// <summary>
    /// The active work-center catalog with its display rank, ordered the way the Settings screen shows it.
    /// Deliberately not <c>sp_setup_work_centers_get_all</c>: that procedure does not select <c>sort_rank</c>,
    /// orders by rank rather than building first, and takes no parameters, so it cannot serve this caller (T090).
    /// </summary>
    private const string WorkCentersCatalogProcedure = "sp_setup_work_centers_catalog_get";

    /// <summary>
    /// The authoritative request-type catalog read. It replaced <c>Assets/Config/waitlist-request-types.json</c>
    /// as the request-type source (T100) and returns each row's stable <c>public_id</c> GUID beside its
    /// configured <c>default_image_path</c>.
    /// </summary>
    private readonly ILogger<ImageLocationService> _logger;
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
    /// <param name="imageOverrideReadService">Service for reading stored image overrides</param>
    /// <param name="configurationResolver">Service for resolving image storage configuration</param>
    /// <param name="workCenterCatalogService">Service for accessing work center catalog data</param>
    /// <exception cref="ArgumentNullException">If any dependency is null</exception>
    public ImageLocationService(
        ILogger<ImageLocationService> logger,
        IImageOverrideReadService imageOverrideReadService,
        IImageStorageConfigurationResolver configurationResolver,
        IWorkCenterCatalogService workCenterCatalogService,
        IMySqlHelperServer mySqlHelperServer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
    public async Task<string> ResolveRequestItemImagePathAsync(string itemCode, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            var message = "Image location service not initialized. Call InitializeAsync() first.";
            _logger.LogError(message);
            throw new InvalidOperationException(message);
        }

        if (string.IsNullOrWhiteSpace(itemCode))
        {
            throw new ArgumentException("Item code cannot be null or empty.", nameof(itemCode));
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var defaultPath = ImageLocationDefaults.RequestItemDefaultPath;
            var normalizedItem = itemCode.Trim();

            // Cascade order: Item override -> the Item's Category family -> the existing placeholder
            // (contracts/card-and-identifier.md §4). The Item is keyed by its own code, not by a stable GUID:
            // it is the same identity the request is stored with.
            var overridePath = await _imageOverrideReadService
                .GetOverrideAsync(ImageLocationScope.RequestItem.ToDatabaseString(), normalizedItem, cancellationToken)
                .ConfigureAwait(false);
            if (overridePath is not null && !string.IsNullOrWhiteSpace(overridePath.ImagePath))
            {
                return await ResolveExistingPathAsync(overridePath.ImagePath, defaultPath, "request_item", normalizedItem).ConfigureAwait(false);
            }

            // The family hop comes from the catalog, so an Item that is not catalogued simply has no family
            // rather than being given a borrowed one.
            var category = RequestItemCatalog.FindById(normalizedItem)?.Category.ToString();
            if (!string.IsNullOrWhiteSpace(category))
            {
                var familyPath = await _imageOverrideReadService
                    .GetOverrideAsync(ImageLocationScope.RequestCategory.ToDatabaseString(), category, cancellationToken)
                    .ConfigureAwait(false);
                if (familyPath is not null && !string.IsNullOrWhiteSpace(familyPath.ImagePath))
                {
                    return await ResolveExistingPathAsync(familyPath.ImagePath, defaultPath, "request_category", category).ConfigureAwait(false);
                }
            }

            // Nothing is configured for this Item, so the caller gets the placeholder. Said out loud at Debug
            // because the substitution is otherwise invisible: the card cannot tell a placeholder from a real
            // answer, and a caller that prefers any resolved path will show "no image available" over a good one.
            _logger.LogDebug(
                "No override for request_item:{ItemCode} and no family image for its category; returning the default placeholder {DefaultPath}",
                normalizedItem,
                defaultPath);
            return defaultPath;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve request item image path for {ItemCode}", itemCode);
            return ImageLocationDefaults.RequestItemDefaultPath;
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
    /// <remarks>
    /// There is nothing left to detect: display-name drift was a hazard of the request-type and subtype catalogs,
    /// whose overrides were keyed by a stable GUID precisely so a renamed label could not orphan them. Those
    /// scopes are retired with the vocabulary (FR-023) and the live Item and work-center overrides are keyed by
    /// the same Item code and work-center name the request is stored with, so a rename cannot orphan them in the
    /// first place. The method stays on the contract and answers zero rather than throwing.
    /// </remarks>
    public Task<int> DetectConfigurationChangesAsync()
    {
        if (!_isInitialized)
        {
            _logger.LogWarning("DetectConfigurationChangesAsync called before initialization");
        }

        _logger.LogDebug("No display-name drift to detect: the live image scopes are keyed by the stored identity.");

        return Task.FromResult(0);
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

        // The catalog comes from its own procedure; the requested name set is applied here. A variable-length
        // name list has no safe parameter form in MySQL 5.7, and the active catalog is small and bounded, so
        // filtering it costs nothing and cannot break on a name that happens to contain a comma (FR-015).
        var requestedNames = workCenterNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var rows = await _mySqlHelperServer.ExecuteStoredProcedureQueryAsync(
            WorkCentersCatalogProcedure,
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        var items = new List<WorkCenterItem>(rows.Count);
        foreach (var row in rows)
        {
            if (!requestedNames.Contains(ReadString(row, "work_center_name")))
            {
                continue;
            }

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

        if (value is bool boolValue)
        {
            return boolValue ? 1 : 0;
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
