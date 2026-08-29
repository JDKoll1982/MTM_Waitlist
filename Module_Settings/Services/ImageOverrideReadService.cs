using Microsoft.Extensions.Logging;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Implementation of IImageOverrideReadService.
/// Reads image location overrides from the config_images_locations table using MySqlHelperServer.
/// Provides comprehensive error handling, validation, and logging.
/// Thread-safe for concurrent access to the database.
/// </summary>
public sealed class ImageOverrideReadService : IImageOverrideReadService
{
    private readonly IMySqlHelperServer _mySqlHelperServer;
    private readonly ILogger<ImageOverrideReadService> _logger;
    // Valid scope values
    private static readonly HashSet<string> ValidScopes = new(StringComparer.OrdinalIgnoreCase)
    {
        "request_type",
        "request_subtype",
        "work_center"
    };

    /// <summary>
    /// Initializes a new ImageOverrideReadService.
    /// Dependencies must be provided; null dependencies throw ArgumentNullException.
    /// </summary>
    /// <param name="mySqlHelperServer">Service for executing SQL queries</param>
    /// <param name="logger">Logger for diagnostics and error logging</param>
    /// <exception cref="ArgumentNullException">If any dependency is null</exception>
    public ImageOverrideReadService(
        IMySqlHelperServer mySqlHelperServer,
        ILogger<ImageOverrideReadService> logger)
    {
        _mySqlHelperServer = mySqlHelperServer ?? throw new ArgumentNullException(nameof(mySqlHelperServer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ImageOverride?> GetOverrideAsync(string scope, string scopeItemId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            throw new ArgumentNullException(nameof(scope), "Scope cannot be null or empty");
        }

        if (string.IsNullOrWhiteSpace(scopeItemId))
        {
            throw new ArgumentNullException(nameof(scopeItemId), "Scope item ID cannot be null or empty");
        }

        if (!ValidScopes.Contains(scope))
        {
            throw new ArgumentException(
                $"Invalid scope '{scope}'. Must be one of: {string.Join(", ", ValidScopes)}", 
                nameof(scope));
        }

        try
        {
            _logger.LogDebug("Loading override: scope={Scope}, scopeItemId={ScopeItemId}", scope, scopeItemId);

            var rows = await _mySqlHelperServer.ExecuteSqlQueryAsync(
                @"SELECT
    id,
    public_id,
    scope,
    scope_item_id,
    image_path,
    is_active,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
FROM config_images_locations
WHERE scope = @p_scope
  AND scope_item_id = @p_scope_item_id
  AND is_active = 1
LIMIT 1;",
                new Dictionary<string, object?>
                {
                    ["p_scope"] = scope,
                    ["p_scope_item_id"] = scopeItemId
                },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken).ConfigureAwait(false);

            if (rows.Count == 0)
            {
                _logger.LogDebug("No active override found: scope={Scope}, scopeItemId={ScopeItemId}", scope, scopeItemId);
                return null;
            }

            var resolvedOverride = ParseImageOverride(rows[0]);
            _logger.LogDebug("Retrieved override: {PublicId}, path={ImagePath}", resolvedOverride.PublicId, resolvedOverride.ImagePath);
            return resolvedOverride;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Override query was cancelled: scope={Scope}, scopeItemId={ScopeItemId}", scope, scopeItemId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get override: scope={Scope}, scopeItemId={ScopeItemId}", scope, scopeItemId);
            throw new InvalidOperationException(
                $"Failed to query image override for scope '{scope}' and item '{scopeItemId}'", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ImageOverride>> GetOverridesByScopeAsync(string scope, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            throw new ArgumentNullException(nameof(scope), "Scope cannot be null or empty");
        }

        if (!ValidScopes.Contains(scope))
        {
            throw new ArgumentException(
                $"Invalid scope '{scope}'. Must be one of: {string.Join(", ", ValidScopes)}",
                nameof(scope));
        }

        try
        {
            _logger.LogDebug("Loading all overrides for scope: {Scope}", scope);

            var rows = await _mySqlHelperServer.ExecuteSqlQueryAsync(
                @"SELECT
    id,
    public_id,
    scope,
    scope_item_id,
    image_path,
    is_active,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
FROM config_images_locations
WHERE scope = @p_scope
  AND is_active = 1
ORDER BY updated_utc DESC;",
                new Dictionary<string, object?>
                {
                    ["p_scope"] = scope
                },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken).ConfigureAwait(false);

            var overrides = rows.Select(ParseImageOverride).ToList();
            _logger.LogDebug("Retrieved {Count} active overrides for scope {Scope}", overrides.Count, scope);
            return overrides;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Override query was cancelled for scope {Scope}", scope);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get overrides for scope {Scope}", scope);
            throw new InvalidOperationException($"Failed to query overrides for scope '{scope}'", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> HasOverrideAsync(string scope, string scopeItemId, CancellationToken cancellationToken = default)
    {
        var @override = await GetOverrideAsync(scope, scopeItemId, cancellationToken).ConfigureAwait(false);
        return @override != null;
    }

    /// <inheritdoc />
    public async Task<int> CountAllActiveOverridesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Counting all active overrides");

            var rows = await _mySqlHelperServer.ExecuteSqlQueryAsync(
                @"SELECT COUNT(*) as count
FROM config_images_locations
WHERE is_active = 1;",
                new Dictionary<string, object?>(),
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken).ConfigureAwait(false);

            var count = GetInt32(rows.FirstOrDefault(), "count");
            _logger.LogDebug("Active override count: {Count}", count);
            return count;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Count query was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to count active overrides");
            throw new InvalidOperationException("Failed to count active overrides", ex);
        }
    }

    /// <inheritdoc />
    public async Task<int> CountActiveOverridesByScopeAsync(string scope, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            throw new ArgumentNullException(nameof(scope), "Scope cannot be null or empty");
        }

        if (!ValidScopes.Contains(scope))
        {
            throw new ArgumentException(
                $"Invalid scope '{scope}'. Must be one of: {string.Join(", ", ValidScopes)}",
                nameof(scope));
        }

        try
        {
            _logger.LogDebug("Counting active overrides for scope {Scope}", scope);

            var rows = await _mySqlHelperServer.ExecuteSqlQueryAsync(
                @"SELECT COUNT(*) as count
FROM config_images_locations
WHERE scope = @p_scope
  AND is_active = 1;",
                new Dictionary<string, object?>
                {
                    ["p_scope"] = scope
                },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken).ConfigureAwait(false);

            var count = GetInt32(rows.FirstOrDefault(), "count");
            _logger.LogDebug("Active override count for scope {Scope}: {Count}", scope, count);
            return count;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Count query was cancelled for scope {Scope}", scope);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to count overrides for scope {Scope}", scope);
            throw new InvalidOperationException($"Failed to count overrides for scope '{scope}'", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ImageOverride>> DetectOrphanedOverridesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Detecting orphaned overrides");

            // Get all active overrides
            var allRows = await _mySqlHelperServer.ExecuteSqlQueryAsync(
                @"SELECT
    id,
    public_id,
    scope,
    scope_item_id,
    image_path,
    is_active,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
FROM config_images_locations
WHERE is_active = 1;",
                new Dictionary<string, object?>(),
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken).ConfigureAwait(false);

            var orphaned = new List<ImageOverride>();

            foreach (var row in allRows)
            {
                var scope = GetValue(row, "scope");
                var scopeItemId = GetValue(row, "scope_item_id");

                if (!await ValidateScopeItemExistsAsync(scope, scopeItemId, cancellationToken))
                {
                    orphaned.Add(ParseImageOverride(row));
                    _logger.LogWarning("Detected orphaned override: scope={Scope}, scopeItemId={ScopeItemId}", scope, scopeItemId);
                }
            }

            _logger.LogInformation("Detected {Count} orphaned overrides", orphaned.Count);
            return orphaned;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Orphan detection was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect orphaned overrides");
            throw new InvalidOperationException("Failed to detect orphaned overrides", ex);
        }
    }

    /// <inheritdoc />
    public async Task<ImageOverride?> GetOverrideByPublicIdAsync(string publicId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            throw new ArgumentNullException(nameof(publicId), "Public ID cannot be null or empty");
        }

        try
        {
            _logger.LogDebug("Loading override by public ID: {PublicId}", publicId);

            var rows = await _mySqlHelperServer.ExecuteSqlQueryAsync(
                @"SELECT
    id,
    public_id,
    scope,
    scope_item_id,
    image_path,
    is_active,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
FROM config_images_locations
WHERE public_id = @p_public_id
  AND is_active = 1
LIMIT 1;",
                new Dictionary<string, object?>
                {
                    ["p_public_id"] = publicId
                },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken).ConfigureAwait(false);

            if (rows.Count == 0)
            {
                _logger.LogDebug("No active override found with public ID: {PublicId}", publicId);
                return null;
            }

            return ParseImageOverride(rows[0]);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Override query was cancelled for public ID {PublicId}", publicId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get override by public ID {PublicId}", publicId);
            throw new InvalidOperationException($"Failed to query override with public ID '{publicId}'", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ImageOverride>> GetRecentlyUpdatedOverridesAsync(int maxRecordCount = 100, CancellationToken cancellationToken = default)
    {
        if (maxRecordCount < 1)
        {
            throw new ArgumentException("Record count must be at least 1", nameof(maxRecordCount));
        }

        try
        {
            _logger.LogDebug("Loading recently updated overrides (max {Count})", maxRecordCount);

            var rows = await _mySqlHelperServer.ExecuteSqlQueryAsync(
                $@"SELECT
    id,
    public_id,
    scope,
    scope_item_id,
    image_path,
    is_active,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
FROM config_images_locations
ORDER BY updated_utc DESC
LIMIT {maxRecordCount};",
                new Dictionary<string, object?>(),
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken).ConfigureAwait(false);

            var overrides = rows.Select(ParseImageOverride).ToList();
            _logger.LogDebug("Retrieved {Count} recently updated overrides", overrides.Count);
            return overrides;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Query was cancelled while loading recently updated overrides");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recently updated overrides");
            throw new InvalidOperationException("Failed to query recently updated overrides", ex);
        }
    }

    /// <summary>
    /// Helper method to validate that a scope item actually exists in its source table.
    /// </summary>
    private async Task<bool> ValidateScopeItemExistsAsync(string scope, string scopeItemId, CancellationToken cancellationToken)
    {
        try
        {
            return scope?.ToLowerInvariant() switch
            {
                "request_type" => await ValidateRequestTypeExistsAsync(scopeItemId, cancellationToken),
                "request_subtype" => await ValidateSubtypeExistsAsync(scopeItemId, cancellationToken),
                "work_center" => await ValidateWorkCenterExistsAsync(scopeItemId, cancellationToken),
                _ => false
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate scope item {Scope}:{ScopeItemId}", scope, scopeItemId);
            return false;
        }
    }

    /// <summary>
    /// Validates that a request type ID exists in the static RequestTypeInventory.
    /// </summary>
    private Task<bool> ValidateRequestTypeExistsAsync(string requestTypeId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(requestTypeId, out var guid))
        {
            return Task.FromResult(false);
        }

        var exists = RequestTypeInventory.IsValidId(guid);
        return Task.FromResult(exists);
    }

    /// <summary>
    /// Validates that a subtype ID exists in the static RequestSubtypeInventory.
    /// </summary>
    private Task<bool> ValidateSubtypeExistsAsync(string subtypeId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(subtypeId, out var guid))
        {
            return Task.FromResult(false);
        }

        var exists = RequestSubtypeInventory.IsValidId(guid);
        return Task.FromResult(exists);
    }

    /// <summary>
    /// Validates that a work center ID exists in the database.
    /// </summary>
    private async Task<bool> ValidateWorkCenterExistsAsync(string workCenterId, CancellationToken cancellationToken)
    {
        if (!long.TryParse(workCenterId, out var id))
        {
            return false;
        }

        try
        {
            var rows = await _mySqlHelperServer.ExecuteSqlQueryAsync(
                @"SELECT id FROM setup_work_centers_catalog WHERE id = @p_id LIMIT 1;",
                new Dictionary<string, object?> { ["p_id"] = id },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken).ConfigureAwait(false);

            return rows.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Parses a database row into an ImageOverride object.
    /// </summary>
    private static ImageOverride ParseImageOverride(IReadOnlyDictionary<string, object?>? row)
    {
        if (row == null)
        {
            return new ImageOverride();
        }

        return new ImageOverride
        {
            RecordId = GetInt64(row, "id"),
            PublicId = GetValue(row, "public_id"),
            Scope = GetValue(row, "scope"),
            ScopeItemId = GetValue(row, "scope_item_id"),
            ImagePath = GetValue(row, "image_path"),
            IsActive = GetBoolean(row, "is_active"),
            CreatedByUserId = GetNullableInt64(row, "created_by_user_id"),
            UpdatedByUserId = GetNullableInt64(row, "updated_by_user_id"),
            CreatedUtc = GetDateTime(row, "created_utc"),
            UpdatedUtc = GetDateTime(row, "updated_utc")
        };
    }

    /// <summary>
    /// Helper methods for safely extracting values from database rows.
    /// </summary>
    private static string GetValue(IReadOnlyDictionary<string, object?>? row, string key)
    {
        if (row is null || !row.TryGetValue(key, out var value) || value is null)
        {
            return string.Empty;
        }

        return value.ToString() ?? string.Empty;
    }

    private static long GetInt64(IReadOnlyDictionary<string, object?>? row, string key)
    {
        if (row is null || !row.TryGetValue(key, out var value) || value is null)
        {
            return 0;
        }

        return value is long longValue ? longValue : Convert.ToInt64(value);
    }

    private static long? GetNullableInt64(IReadOnlyDictionary<string, object?>? row, string key)
    {
        if (row is null || !row.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value is long longValue ? longValue : Convert.ToInt64(value);
    }

    private static int GetInt32(IReadOnlyDictionary<string, object?>? row, string key)
    {
        if (row is null || !row.TryGetValue(key, out var value) || value is null)
        {
            return 0;
        }

        return value is int intValue ? intValue : Convert.ToInt32(value);
    }

    private static bool GetBoolean(IReadOnlyDictionary<string, object?>? row, string key)
    {
        if (row is null || !row.TryGetValue(key, out var value) || value is null)
        {
            return false;
        }

        return value is bool boolValue ? boolValue : (Convert.ToInt32(value) != 0);
    }

    private static DateTime GetDateTime(IReadOnlyDictionary<string, object?>? row, string key)
    {
        if (row is null || !row.TryGetValue(key, out var value) || value is null)
        {
            return DateTime.UtcNow;
        }

        return value is DateTime dateTime ? dateTime : Convert.ToDateTime(value);
    }
}
