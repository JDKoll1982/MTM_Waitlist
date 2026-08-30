using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Implementation of IRequestSubtypeDisplayLabelService.
/// Tracks request subtype display name changes to ensure renaming never orphans image overrides.
/// Handles non-unique display names by requiring parent request type context.
/// All database operations use globally unique stable IDs; display names are audit-only.
/// </summary>
public sealed class RequestSubtypeDisplayLabelService : IRequestSubtypeDisplayLabelService
{
    private readonly ILogger<RequestSubtypeDisplayLabelService> _logger;
    private readonly ConcurrentDictionary<Guid, RequestSubtypeDisplayLabelRecord> _labelRecords;

    /// <summary>
    /// Initializes a new RequestSubtypeDisplayLabelService.
    /// </summary>
    /// <param name="logger">Logger for diagnostics and errors</param>
    /// <exception cref="ArgumentNullException">If logger is null</exception>
    public RequestSubtypeDisplayLabelService(ILogger<RequestSubtypeDisplayLabelService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _labelRecords = new ConcurrentDictionary<Guid, RequestSubtypeDisplayLabelRecord>();
    }

    /// <inheritdoc />
    public string GetCurrentDisplayName(Guid subtypeId)
    {
        if (!_labelRecords.TryGetValue(subtypeId, out var record))
        {
            var message = $"Subtype ID '{subtypeId}' not found in display label registry. " +
                         $"This may indicate the JSON configuration is out of sync or the service was not initialized. " +
                         $"Call InitializeFromJsonAsync() at application startup.";
            _logger.LogError(message);
            throw new ArgumentException(message, nameof(subtypeId));
        }

        _logger.LogDebug("Retrieved display name for subtype {SubtypeId}: {DisplayName}",
                         subtypeId, record.CurrentDisplayName);
        return record.CurrentDisplayName;
    }

    /// <inheritdoc />
    public Guid GetParentRequestTypeId(Guid subtypeId)
    {
        if (!_labelRecords.TryGetValue(subtypeId, out var record))
        {
            var message = $"Subtype ID '{subtypeId}' not found in display label registry.";
            _logger.LogError(message);
            throw new ArgumentException(message, nameof(subtypeId));
        }

        _logger.LogDebug("Retrieved parent request type ID for subtype {SubtypeId}: {ParentId}",
                         subtypeId, record.ParentRequestTypeId);
        return record.ParentRequestTypeId;
    }

    /// <inheritdoc />
    public Guid? GetIdByDisplayName(Guid parentRequestTypeId, string subtypeDisplayName)
    {
        if (string.IsNullOrWhiteSpace(subtypeDisplayName))
        {
            _logger.LogWarning("Subtype display name lookup called with null or empty name");
            return null;
        }

        // Defensive: Only match current names, not historical names
        // Match by parent AND display name (to handle non-unique names like "Bring")
        var record = _labelRecords.Values.FirstOrDefault(r =>
            r.ParentRequestTypeId == parentRequestTypeId &&
            string.Equals(r.CurrentDisplayName, subtypeDisplayName, StringComparison.OrdinalIgnoreCase));

        if (record == null)
        {
            _logger.LogDebug("No subtype found under parent {ParentId} with current display name '{DisplayName}'. " +
                           "This may indicate the name was recently changed.", parentRequestTypeId, subtypeDisplayName);
            return null;
        }

        _logger.LogDebug("Retrieved subtype ID for parent {ParentId} and display name '{DisplayName}': {SubtypeId}",
                         parentRequestTypeId, subtypeDisplayName, record.SubtypeId);
        return record.SubtypeId;
    }

    /// <inheritdoc />
    public bool HasDisplayNameChanged(Guid subtypeId)
    {
        if (!_labelRecords.TryGetValue(subtypeId, out var record))
        {
            _logger.LogWarning("Change check requested for unknown subtype ID {SubtypeId}", subtypeId);
            return false;
        }

        var hasChanged = record.HasChanged;
        if (hasChanged)
        {
            _logger.LogInformation("Subtype {SubtypeId} display name changed: '{OldName}' → '{NewName}'",
                                 subtypeId, record.PreviousDisplayName, record.CurrentDisplayName);
        }

        return hasChanged;
    }

    /// <inheritdoc />
    public string? GetPreviousDisplayName(Guid subtypeId)
    {
        if (!_labelRecords.TryGetValue(subtypeId, out var record))
        {
            _logger.LogWarning("Previous name lookup requested for unknown subtype ID {SubtypeId}", subtypeId);
            return null;
        }

        return record.PreviousDisplayName;
    }

    /// <inheritdoc />
    public async Task InitializeFromJsonAsync()
    {
        _logger.LogInformation("Initializing request subtype display label registry from JSON configuration");

        try
        {
            // Validate that RequestSubtypeInventory is properly loaded
            if (!RequestSubtypeInventory.Groups.Any())
            {
                var message = "Request subtype inventory is empty. JSON configuration may not be loaded correctly.";
                _logger.LogError(message);
                throw new InvalidOperationException(message);
            }

            _labelRecords.Clear();
            var recordCount = 0;

            foreach (var group in RequestSubtypeInventory.Groups)
            {
                foreach (var item in group.Subtypes)
                {
                    var record = new RequestSubtypeDisplayLabelRecord
                    {
                        SubtypeId = item.StableId,
                        ParentRequestTypeId = group.ParentRequestTypeId,
                        CurrentDisplayName = item.DisplayName,
                        PreviousDisplayName = null, // First initialization: no previous name
                        LastNameChangeUtc = null
                    };

                    if (!_labelRecords.TryAdd(item.StableId, record))
                    {
                        _logger.LogError("Duplicate subtype ID detected during initialization: {SubtypeId}",
                                       item.StableId);
                        throw new InvalidOperationException(
                            $"Duplicate subtype ID '{item.StableId}' in JSON configuration");
                    }

                    recordCount++;
                    _logger.LogDebug("Registered subtype {Id} under parent {ParentId}: {DisplayName}",
                                   item.StableId, group.ParentRequestTypeId, item.DisplayName);
                }
            }

            _logger.LogInformation("Successfully initialized {Count} request subtype display label records", recordCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize request subtype display label registry");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> DetectDisplayNameChangesAsync()
    {
        _logger.LogInformation("Detecting display name changes in subtype configuration");

        try
        {
            if (!_labelRecords.Any())
            {
                _logger.LogWarning("Display label registry is empty. Call InitializeFromJsonAsync() first.");
                return 0;
            }

            var changeCount = 0;
            var timestamp = DateTime.UtcNow;

            foreach (var group in RequestSubtypeInventory.Groups)
            {
                foreach (var item in group.Subtypes)
                {
                    if (!_labelRecords.TryGetValue(item.StableId, out var record))
                    {
                        _logger.LogWarning("Subtype ID {Id} found in JSON but not in registry. " +
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

                        _logger.LogWarning(
                            "Detected display name change for subtype {Id} under parent {ParentId}: '{OldName}' → '{NewName}'",
                            item.StableId, group.ParentRequestTypeId, oldName, item.DisplayName);
                    }
                }
            }

            if (changeCount > 0)
            {
                _logger.LogInformation("Detected {Count} display name changes in subtype configuration", changeCount);
            }
            else
            {
                _logger.LogDebug("No display name changes detected in subtypes");
            }

            return changeCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect display name changes in subtypes");
            throw;
        }
    }

    /// <summary>
    /// Internal method for testing: Gets all registered display label records.
    /// </summary>
    internal IReadOnlyDictionary<Guid, RequestSubtypeDisplayLabelRecord> GetAllRecords() =>
        _labelRecords.AsReadOnly();

    /// <summary>
    /// Internal method for testing: Clears all records.
    /// </summary>
    internal void ClearRecords() => _labelRecords.Clear();
}
