using Microsoft.Extensions.Logging;
using MySqlConnector;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Settings.Models;
using System.Globalization;
using System.Text;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Implementation of IImageOverrideWriteService.
/// Writes image location overrides to the config_images_locations table using MySqlHelperServer and raw transactions.
/// Provides comprehensive error handling, validation, and logging.
/// Thread-safe for concurrent access to the database.
/// </summary>
public sealed class ImageOverrideWriteService : IImageOverrideWriteService
{
    private readonly IMySqlHelperServer _mySqlHelperServer;
    private readonly ILogger<ImageOverrideWriteService> _logger;
    private readonly IImageOverrideReadService _readService;
    private readonly IImageLocationService _imageLocationService;

    // Valid scope values
    private static readonly HashSet<string> ValidScopes = new(StringComparer.OrdinalIgnoreCase)
    {
        "request_type",
        "request_subtype",
        "work_center"
    };

    private const int MaxImagePathLength = 500;

    /// <summary>
    /// Initializes a new ImageOverrideWriteService.
    /// Dependencies must be provided; null dependencies throw ArgumentNullException.
    /// </summary>
    /// <param name="mySqlHelperServer">Service for executing SQL queries</param>
    /// <param name="readService">Service for reading overrides (used for verification)</param>
    /// <param name="logger">Logger for diagnostics and error logging</param>
    /// <exception cref="ArgumentNullException">If any dependency is null</exception>
    public ImageOverrideWriteService(
        IMySqlHelperServer mySqlHelperServer,
        IImageOverrideReadService readService,
        IImageLocationService imageLocationService,
        ILogger<ImageOverrideWriteService> logger)
    {
        _mySqlHelperServer = mySqlHelperServer ?? throw new ArgumentNullException(nameof(mySqlHelperServer));
        _readService = readService ?? throw new ArgumentNullException(nameof(readService));
        _imageLocationService = imageLocationService ?? throw new ArgumentNullException(nameof(imageLocationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ImageOverrideWriteResult> CreateOverrideAsync(
        string scope,
        string scopeItemId,
        string imagePath,
        long? userId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            throw new ArgumentNullException(nameof(scope), "Scope cannot be null or empty");
        }

        if (string.IsNullOrWhiteSpace(scopeItemId))
        {
            throw new ArgumentNullException(nameof(scopeItemId), "Scope item ID cannot be null or empty");
        }

        if (string.IsNullOrWhiteSpace(imagePath))
        {
            throw new ArgumentNullException(nameof(imagePath), "Image path cannot be null or empty");
        }

        if (!ValidScopes.Contains(scope))
        {
            throw new ArgumentException(
                $"Invalid scope '{scope}'. Must be one of: {string.Join(", ", ValidScopes)}", 
                nameof(scope));
        }

        if (imagePath.Length > MaxImagePathLength)
        {
            throw new ArgumentException(
                $"Image path exceeds maximum length of {MaxImagePathLength} characters", 
                nameof(imagePath));
        }

        try
        {
            _logger.LogDebug("Creating override: scope={Scope}, scopeItemId={ScopeItemId}, path={ImagePath}", 
                           scope, scopeItemId, imagePath);

            var publicId = Guid.NewGuid().ToString("D");

            // The helper swallows MySqlException, so the unique key is checked up front
            // and the affected-row count is used as the failure signal.
            var existingRows = await _mySqlHelperServer.ExecuteSqlQueryAsync(
                @"SELECT is_active
FROM config_images_locations
WHERE scope = @p_scope
  AND scope_item_id = @p_scope_item_id
LIMIT 1;",
                new Dictionary<string, object?>
                {
                    ["p_scope"] = scope,
                    ["p_scope_item_id"] = scopeItemId
                },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken).ConfigureAwait(false);

            if (existingRows.Count > 0)
            {
                var isActive = existingRows[0].TryGetValue("is_active", out var activeValue)
                    && activeValue is not null
                    && Convert.ToInt32(activeValue) != 0;

                if (isActive)
                {
                    var duplicateMessage = $"An override already exists for scope '{scope}' and item '{scopeItemId}'";
                    _logger.LogWarning(duplicateMessage);
                    return new ImageOverrideWriteResult
                    {
                        Success = false,
                        ErrorMessage = duplicateMessage,
                        ErrorCode = "DUPLICATE_KEY",
                        OperationType = "CREATE",
                        AffectedScope = scope,
                        AffectedScopeItemId = scopeItemId
                    };
                }

                // The unique key spans (scope, scope_item_id) regardless of is_active,
                // so a soft-deleted row must be reactivated rather than re-inserted.
                return await ReactivateOverrideAsync(scope, scopeItemId, imagePath, userId, cancellationToken)
                    .ConfigureAwait(false);
            }

            var affectedRows = await _mySqlHelperServer.ExecuteSqlNonQueryAsync(
                @"INSERT INTO config_images_locations (
    public_id,
    scope,
    scope_item_id,
    image_path,
    is_active,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
) VALUES (
    @p_public_id,
    @p_scope,
    @p_scope_item_id,
    @p_image_path,
    1,
    @p_user_id,
    @p_user_id,
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
);",
                new Dictionary<string, object?>
                {
                    ["p_public_id"] = publicId,
                    ["p_scope"] = scope,
                    ["p_scope_item_id"] = scopeItemId,
                    ["p_image_path"] = imagePath,
                    ["p_user_id"] = userId.HasValue ? (object)userId.Value : DBNull.Value
                },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken).ConfigureAwait(false);

            if (affectedRows != 1)
            {
                var failureMessage = $"Failed to create override for scope '{scope}' and item '{scopeItemId}'";
                _logger.LogError(failureMessage);
                return new ImageOverrideWriteResult
                {
                    Success = false,
                    ErrorMessage = failureMessage,
                    ErrorCode = "DATABASE_ERROR",
                    OperationType = "CREATE",
                    AffectedScope = scope,
                    AffectedScopeItemId = scopeItemId
                };
            }

            _logger.LogInformation("Override created successfully: publicId={PublicId}, scope={Scope}, scopeItemId={ScopeItemId}", 
                                 publicId, scope, scopeItemId);

            _imageLocationService.RaiseImageLocationUpdated(scope, scopeItemId);

            // Read back the created override
            var createdOverride = await _readService.GetOverrideByPublicIdAsync(publicId, cancellationToken)
                .ConfigureAwait(false);

            return new ImageOverrideWriteResult
            {
                Success = true,
                Override = createdOverride,
                OperationType = "CREATE",
                AffectedScope = scope,
                AffectedScopeItemId = scopeItemId
            };
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Create operation was cancelled: scope={Scope}, scopeItemId={ScopeItemId}", 
                             scope, scopeItemId);
            throw;
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            // Unique key violation
            var message = $"An override already exists for scope '{scope}' and item '{scopeItemId}'";
            _logger.LogWarning(ex, message);
            return new ImageOverrideWriteResult
            {
                Success = false,
                ErrorMessage = message,
                ErrorCode = "DUPLICATE_KEY",
                OperationType = "CREATE",
                AffectedScope = scope,
                AffectedScopeItemId = scopeItemId
            };
        }
        catch (Exception ex)
        {
            var message = $"Failed to create override: {ex.Message}";
            _logger.LogError(ex, "Failed to create override: scope={Scope}, scopeItemId={ScopeItemId}", scope, scopeItemId);
            return new ImageOverrideWriteResult
            {
                Success = false,
                ErrorMessage = message,
                ErrorCode = "DATABASE_ERROR",
                OperationType = "CREATE",
                AffectedScope = scope,
                AffectedScopeItemId = scopeItemId
            };
        }
    }

    private async Task<ImageOverrideWriteResult> ReactivateOverrideAsync(
        string scope,
        string scopeItemId,
        string imagePath,
        long? userId,
        CancellationToken cancellationToken)
    {
        var affectedRows = await _mySqlHelperServer.ExecuteSqlNonQueryAsync(
            @"UPDATE config_images_locations
SET image_path = @p_image_path,
    is_active = 1,
    updated_by_user_id = @p_user_id,
    updated_utc = UTC_TIMESTAMP()
WHERE scope = @p_scope
  AND scope_item_id = @p_scope_item_id;",
            new Dictionary<string, object?>
            {
                ["p_scope"] = scope,
                ["p_scope_item_id"] = scopeItemId,
                ["p_image_path"] = imagePath,
                ["p_user_id"] = userId.HasValue ? (object)userId.Value : DBNull.Value
            },
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        if (affectedRows < 1)
        {
            var message = $"Failed to reactivate override for scope '{scope}' and item '{scopeItemId}'";
            _logger.LogError(message);
            return new ImageOverrideWriteResult
            {
                Success = false,
                ErrorMessage = message,
                ErrorCode = "DATABASE_ERROR",
                OperationType = "CREATE",
                AffectedScope = scope,
                AffectedScopeItemId = scopeItemId
            };
        }

        _logger.LogInformation("Reactivated soft-deleted override: scope={Scope}, scopeItemId={ScopeItemId}", scope, scopeItemId);
        _imageLocationService.RaiseImageLocationUpdated(scope, scopeItemId);

        return new ImageOverrideWriteResult
        {
            Success = true,
            Override = await _readService.GetOverrideAsync(scope, scopeItemId, cancellationToken).ConfigureAwait(false),
            OperationType = "CREATE",
            AffectedScope = scope,
            AffectedScopeItemId = scopeItemId
        };
    }

    /// <inheritdoc />
    public async Task<ImageOverrideWriteResult> UpdateOverrideAsync(
        string scope,
        string scopeItemId,
        string newImagePath,
        long? userId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            throw new ArgumentNullException(nameof(scope), "Scope cannot be null or empty");
        }

        if (string.IsNullOrWhiteSpace(scopeItemId))
        {
            throw new ArgumentNullException(nameof(scopeItemId), "Scope item ID cannot be null or empty");
        }

        if (string.IsNullOrWhiteSpace(newImagePath))
        {
            throw new ArgumentNullException(nameof(newImagePath), "Image path cannot be null or empty");
        }

        if (!ValidScopes.Contains(scope))
        {
            throw new ArgumentException(
                $"Invalid scope '{scope}'. Must be one of: {string.Join(", ", ValidScopes)}", 
                nameof(scope));
        }

        if (newImagePath.Length > MaxImagePathLength)
        {
            throw new ArgumentException(
                $"Image path exceeds maximum length of {MaxImagePathLength} characters", 
                nameof(newImagePath));
        }

        try
        {
            _logger.LogDebug("Updating override: scope={Scope}, scopeItemId={ScopeItemId}, newPath={ImagePath}", 
                           scope, scopeItemId, newImagePath);

            // Check if override exists first
            var existing = await _readService.GetOverrideAsync(scope, scopeItemId, cancellationToken)
                .ConfigureAwait(false);

            if (existing == null)
            {
                var message = $"No override found for scope '{scope}' and item '{scopeItemId}'";
                _logger.LogWarning(message);
                return new ImageOverrideWriteResult
                {
                    Success = false,
                    ErrorMessage = message,
                    ErrorCode = "NOT_FOUND",
                    OperationType = "UPDATE",
                    AffectedScope = scope,
                    AffectedScopeItemId = scopeItemId
                };
            }

            var rows = await _mySqlHelperServer.ExecuteSqlNonQueryAsync(
                @"UPDATE config_images_locations
SET image_path = @p_image_path,
    updated_by_user_id = @p_user_id,
    updated_utc = UTC_TIMESTAMP()
WHERE scope = @p_scope
  AND scope_item_id = @p_scope_item_id
  AND is_active = 1;",
                new Dictionary<string, object?>
                {
                    ["p_scope"] = scope,
                    ["p_scope_item_id"] = scopeItemId,
                    ["p_image_path"] = newImagePath,
                    ["p_user_id"] = userId.HasValue ? (object)userId.Value : DBNull.Value
                },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Override updated successfully: scope={Scope}, scopeItemId={ScopeItemId}, newPath={ImagePath}", 
                                 scope, scopeItemId, newImagePath);

            _imageLocationService.RaiseImageLocationUpdated(scope, scopeItemId);

            // Read back the updated override
            var updatedOverride = await _readService.GetOverrideAsync(scope, scopeItemId, cancellationToken)
                .ConfigureAwait(false);

            return new ImageOverrideWriteResult
            {
                Success = true,
                Override = updatedOverride,
                OperationType = "UPDATE",
                AffectedScope = scope,
                AffectedScopeItemId = scopeItemId
            };
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Update operation was cancelled: scope={Scope}, scopeItemId={ScopeItemId}", 
                             scope, scopeItemId);
            throw;
        }
        catch (Exception ex)
        {
            var message = $"Failed to update override: {ex.Message}";
            _logger.LogError(ex, "Failed to update override: scope={Scope}, scopeItemId={ScopeItemId}", scope, scopeItemId);
            return new ImageOverrideWriteResult
            {
                Success = false,
                ErrorMessage = message,
                ErrorCode = "DATABASE_ERROR",
                OperationType = "UPDATE",
                AffectedScope = scope,
                AffectedScopeItemId = scopeItemId
            };
        }
    }

    /// <inheritdoc />
    public async Task<ImageOverrideWriteResult> DeleteOverrideAsync(
        string scope,
        string scopeItemId,
        long? userId = null,
        CancellationToken cancellationToken = default)
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
            _logger.LogDebug("Deleting override: scope={Scope}, scopeItemId={ScopeItemId}", scope, scopeItemId);

            var rows = await _mySqlHelperServer.ExecuteSqlNonQueryAsync(
                @"UPDATE config_images_locations
SET is_active = 0,
    updated_by_user_id = @p_user_id,
    updated_utc = UTC_TIMESTAMP()
WHERE scope = @p_scope
  AND scope_item_id = @p_scope_item_id
  AND is_active = 1;",
                new Dictionary<string, object?>
                {
                    ["p_scope"] = scope,
                    ["p_scope_item_id"] = scopeItemId,
                    ["p_user_id"] = userId.HasValue ? (object)userId.Value : DBNull.Value
                },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken).ConfigureAwait(false);

            var affected = rows > 0;

            if (!affected)
            {
                var message = $"No override found for scope '{scope}' and item '{scopeItemId}'";
                _logger.LogWarning(message);
                return new ImageOverrideWriteResult
                {
                    Success = false,
                    ErrorMessage = message,
                    ErrorCode = "NOT_FOUND",
                    OperationType = "DELETE",
                    AffectedScope = scope,
                    AffectedScopeItemId = scopeItemId
                };
            }

            _logger.LogInformation("Override deleted successfully: scope={Scope}, scopeItemId={ScopeItemId}", scope, scopeItemId);

            _imageLocationService.RaiseImageLocationUpdated(scope, scopeItemId);

            return new ImageOverrideWriteResult
            {
                Success = true,
                OperationType = "DELETE",
                AffectedScope = scope,
                AffectedScopeItemId = scopeItemId
            };
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Delete operation was cancelled: scope={Scope}, scopeItemId={ScopeItemId}", 
                             scope, scopeItemId);
            throw;
        }
        catch (Exception ex)
        {
            var message = $"Failed to delete override: {ex.Message}";
            _logger.LogError(ex, "Failed to delete override: scope={Scope}, scopeItemId={ScopeItemId}", scope, scopeItemId);
            return new ImageOverrideWriteResult
            {
                Success = false,
                ErrorMessage = message,
                ErrorCode = "DATABASE_ERROR",
                OperationType = "DELETE",
                AffectedScope = scope,
                AffectedScopeItemId = scopeItemId
            };
        }
    }

    /// <inheritdoc />
    public async Task<ImageOverrideWriteResult> DeleteByPublicIdAsync(
        string publicId,
        long? userId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            throw new ArgumentNullException(nameof(publicId), "Public ID cannot be null or empty");
        }

        try
        {
            _logger.LogDebug("Deleting override by public ID: {PublicId}", publicId);

            var rows = await _mySqlHelperServer.ExecuteSqlQueryAsync(
                @"UPDATE config_images_locations
SET is_active = 0,
    updated_by_user_id = @p_user_id,
    updated_utc = UTC_TIMESTAMP()
WHERE public_id = @p_public_id
  AND is_active = 1;",
                new Dictionary<string, object?>
                {
                    ["p_public_id"] = publicId,
                    ["p_user_id"] = userId.HasValue ? (object)userId.Value : DBNull.Value
                },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken).ConfigureAwait(false);

            var affected = rows.Count > 0;

            if (!affected)
            {
                var message = $"No override found with public ID '{publicId}'";
                _logger.LogWarning(message);
                return new ImageOverrideWriteResult
                {
                    Success = false,
                    ErrorMessage = message,
                    ErrorCode = "NOT_FOUND",
                    OperationType = "DELETE"
                };
            }

            _logger.LogInformation("Override deleted successfully by public ID: {PublicId}", publicId);

            var deletedOverride = await _readService.GetOverrideByPublicIdAsync(publicId, cancellationToken).ConfigureAwait(false);
            if (deletedOverride != null)
            {
                _imageLocationService.RaiseImageLocationUpdated(deletedOverride.Scope, deletedOverride.ScopeItemId);
            }

            return new ImageOverrideWriteResult
            {
                Success = true,
                OperationType = "DELETE"
            };
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Delete operation was cancelled for public ID {PublicId}", publicId);
            throw;
        }
        catch (Exception ex)
        {
            var message = $"Failed to delete override: {ex.Message}";
            _logger.LogError(ex, "Failed to delete override by public ID {PublicId}", publicId);
            return new ImageOverrideWriteResult
            {
                Success = false,
                ErrorMessage = message,
                ErrorCode = "DATABASE_ERROR",
                OperationType = "DELETE"
            };
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteIfExistsAsync(
        string scope,
        string scopeItemId,
        long? userId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await DeleteOverrideAsync(scope, scopeItemId, userId, cancellationToken)
            .ConfigureAwait(false);

        return result.Success;
    }

    /// <inheritdoc />
    public async Task<int> PurgeInactiveOverridesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogWarning("Purging inactive overrides from database");

            var rows = await _mySqlHelperServer.ExecuteSqlQueryAsync(
                @"DELETE FROM config_images_locations
WHERE is_active = 0;",
                new Dictionary<string, object?>(),
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken).ConfigureAwait(false);

            var count = rows.Count;
            _logger.LogInformation("Purged {Count} inactive overrides from database", count);
            return count;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Purge operation was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to purge inactive overrides");
            throw new InvalidOperationException("Failed to purge inactive overrides", ex);
        }
    }

    /// <inheritdoc />
    public async Task<int> DeactivateAllForScopeAsync(
        string scope,
        long? userId = null,
        CancellationToken cancellationToken = default)
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
            _logger.LogWarning("Deactivating all overrides for scope: {Scope}", scope);

            var rows = await _mySqlHelperServer.ExecuteSqlQueryAsync(
                @"UPDATE config_images_locations
SET is_active = 0,
    updated_by_user_id = @p_user_id,
    updated_utc = UTC_TIMESTAMP()
WHERE scope = @p_scope
  AND is_active = 1;",
                new Dictionary<string, object?>
                {
                    ["p_scope"] = scope,
                    ["p_user_id"] = userId.HasValue ? (object)userId.Value : DBNull.Value
                },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken).ConfigureAwait(false);

            var count = rows.Count;
            _logger.LogInformation("Deactivated {Count} overrides for scope {Scope}", count, scope);
            return count;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Deactivate operation was cancelled for scope {Scope}", scope);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate overrides for scope {Scope}", scope);
            throw new InvalidOperationException($"Failed to deactivate overrides for scope '{scope}'", ex);
        }
    }
}
