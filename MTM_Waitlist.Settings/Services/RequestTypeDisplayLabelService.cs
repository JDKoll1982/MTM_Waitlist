using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Implementation of IRequestTypeDisplayLabelService.
/// Tracks request type display name changes to ensure renaming never orphans image overrides.
/// All database operations use stable IDs; display names are audit-only.
/// </summary>
public sealed class RequestTypeDisplayLabelService : IRequestTypeDisplayLabelService
{
    private readonly ILogger<RequestTypeDisplayLabelService> _logger;
    private readonly ConcurrentDictionary<Guid, RequestTypeDisplayLabelRecord> _labelRecords;

    /// <summary>
    /// Initializes a new RequestTypeDisplayLabelService.
    /// </summary>
    /// <param name="logger">Logger for diagnostics and errors</param>
    /// <exception cref="ArgumentNullException">If logger is null</exception>
    public RequestTypeDisplayLabelService(ILogger<RequestTypeDisplayLabelService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _labelRecords = new ConcurrentDictionary<Guid, RequestTypeDisplayLabelRecord>();
    }

    /// <inheritdoc />
    public string GetCurrentDisplayName(Guid requestTypeId)
    {
        if (!_labelRecords.TryGetValue(requestTypeId, out var record))
        {
            var message = $"Request type ID '{requestTypeId}' not found in display label registry. " +
                         $"This may indicate the JSON configuration is out of sync or the service was not initialized. " +
                         $"Call InitializeFromJsonAsync() at application startup.";
            _logger.LogError(message);
            throw new ArgumentException(message, nameof(requestTypeId));
        }

        _logger.LogDebug("Retrieved display name for request type {RequestTypeId}: {DisplayName}",
                         requestTypeId, record.CurrentDisplayName);
        return record.CurrentDisplayName;
    }

    /// <inheritdoc />
    public Guid? GetIdByCurrentDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            _logger.LogWarning("Display name lookup called with null or empty name");
            return null;
        }

        // Defensive: Only match current names, not historical names (to avoid confusion after renames)
        var record = _labelRecords.Values.FirstOrDefault(r =>
            string.Equals(r.CurrentDisplayName, displayName, StringComparison.OrdinalIgnoreCase));

        if (record == null)
        {
            _logger.LogDebug("No request type found with current display name '{DisplayName}'. " +
                           "This may indicate the name was recently changed.", displayName);
            return null;
        }

        _logger.LogDebug("Retrieved ID for display name '{DisplayName}': {RequestTypeId}",
                         displayName, record.RequestTypeId);
        return record.RequestTypeId;
    }

    /// <inheritdoc />
    public bool HasDisplayNameChanged(Guid requestTypeId)
    {
        if (!_labelRecords.TryGetValue(requestTypeId, out var record))
        {
            _logger.LogWarning("Change check requested for unknown request type ID {RequestTypeId}", requestTypeId);
            return false;
        }

        var hasChanged = record.HasChanged;
        if (hasChanged)
        {
            _logger.LogInformation("Request type {RequestTypeId} display name changed: '{OldName}' → '{NewName}'",
                                 requestTypeId, record.PreviousDisplayName, record.CurrentDisplayName);
        }

        return hasChanged;
    }

    /// <inheritdoc />
    public string? GetPreviousDisplayName(Guid requestTypeId)
    {
        if (!_labelRecords.TryGetValue(requestTypeId, out var record))
        {
            _logger.LogWarning("Previous name lookup requested for unknown request type ID {RequestTypeId}",
                             requestTypeId);
            return null;
        }

        return record.PreviousDisplayName;
    }

    /// <inheritdoc />
    public async Task InitializeFromJsonAsync()
    {
        _logger.LogInformation("Initializing request type display label registry from JSON configuration");

        try
        {
            // Validate that RequestTypeInventory is properly loaded
            if (!RequestTypeInventory.Items.Any())
            {
                var message = "Request type inventory is empty. JSON configuration may not be loaded correctly.";
                _logger.LogError(message);
                throw new InvalidOperationException(message);
            }

            _labelRecords.Clear();
            var recordCount = 0;

            foreach (var item in RequestTypeInventory.Items)
            {
                var record = new RequestTypeDisplayLabelRecord
                {
                    RequestTypeId = item.StableId,
                    CurrentDisplayName = item.DisplayName,
                    PreviousDisplayName = null, // First initialization: no previous name
                    LastNameChangeUtc = null
                };

                if (!_labelRecords.TryAdd(item.StableId, record))
                {
                    _logger.LogError("Duplicate request type ID detected during initialization: {RequestTypeId}",
                                   item.StableId);
                    throw new InvalidOperationException(
                        $"Duplicate request type ID '{item.StableId}' in JSON configuration");
                }

                recordCount++;
                _logger.LogDebug("Registered request type {Id}: {DisplayName}",
                               item.StableId, item.DisplayName);
            }

            _logger.LogInformation("Successfully initialized {Count} request type display label records", recordCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize request type display label registry");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> DetectDisplayNameChangesAsync()
    {
        _logger.LogInformation("Detecting display name changes in JSON configuration");

        try
        {
            if (!_labelRecords.Any())
            {
                _logger.LogWarning("Display label registry is empty. Call InitializeFromJsonAsync() first.");
                return 0;
            }

            var changeCount = 0;
            var timestamp = DateTime.UtcNow;

            foreach (var item in RequestTypeInventory.Items)
            {
                if (!_labelRecords.TryGetValue(item.StableId, out var record))
                {
                    _logger.LogWarning("Request type ID {Id} found in JSON but not in registry. " +
                                     "This indicates JSON was modified after last initialization.",
                                     item.StableId);
                    continue;
                }

                if (!string.Equals(record.CurrentDisplayName, item.DisplayName, StringComparison.Ordinal))
                {
                    var oldName = record.CurrentDisplayName;
                    record.PreviousDisplayName = oldName;
                    record.CurrentDisplayName = item.DisplayName;
                    record.LastNameChangeUtc = timestamp;
                    changeCount++;

                    _logger.LogWarning("Detected display name change for request type {Id}: '{OldName}' → '{NewName}'",
                                     item.StableId, oldName, item.DisplayName);
                }
            }

            if (changeCount > 0)
            {
                _logger.LogInformation("Detected {Count} display name changes in request type configuration", changeCount);
            }
            else
            {
                _logger.LogDebug("No display name changes detected");
            }

            return changeCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect display name changes");
            throw;
        }
    }

    /// <summary>
    /// Internal method for testing: Gets all registered display label records.
    /// </summary>
    internal IReadOnlyDictionary<Guid, RequestTypeDisplayLabelRecord> GetAllRecords() =>
        _labelRecords.AsReadOnly();

    /// <summary>
    /// Internal method for testing: Clears all records.
    /// </summary>
    internal void ClearRecords() => _labelRecords.Clear();
}
