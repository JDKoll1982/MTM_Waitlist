using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Shared.Services;
using System.Text.Json;

namespace MTM_Waitlist.Module_Setup.Services;

public sealed class DunnageWorkflowService : IDunnageWorkflowService
{
    private static readonly string[] AllowedQuickAddRoles =
    {
        "Admin",
        "Developer",
        "Plant Manager",
        "Setup Lead",
        "Production Lead",
    };

    private readonly MySqlHelperServer _mySqlHelperServer;
    private readonly IDunnageTypeVisibilityCatalogService? _dunnageTypeVisibilityCatalogService;

    public DunnageWorkflowService(
        MySqlHelperServer mySqlHelperServer,
        IDunnageTypeVisibilityCatalogService? dunnageTypeVisibilityCatalogService = null)
    {
        _mySqlHelperServer = mySqlHelperServer;
        _dunnageTypeVisibilityCatalogService = dunnageTypeVisibilityCatalogService;
    }

    public async Task<IReadOnlyList<SetupDunnageType>> GetDunnageTypesAsync(string partNumber, string sequenceNumber, CancellationToken cancellationToken = default)
    {
        StartupDebugLog.Info("SetupDunnage", $"GetDunnageTypesAsync started. Part='{partNumber}', Sequence='{sequenceNumber}'.");
        return await _mySqlHelperServer.ExecuteReadWriteAsync(
            "Setup.DunnageTypes.Load",
            partNumber,
            MySqlDatabaseTarget.MtmReceivingApplication,
            () => Task.FromResult(SetupDataCatalog.GetDunnageTypes(partNumber, sequenceNumber)),
            () => GetDunnageTypesFromBackendAsync(partNumber, sequenceNumber, cancellationToken)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SetupDunnagePart>> GetDunnagePartsAsync(string dunnageTypeId, string partNumber, string sequenceNumber, CancellationToken cancellationToken = default)
    {
        StartupDebugLog.Info("SetupDunnage", $"GetDunnagePartsAsync started. DunnageTypeId='{dunnageTypeId}', Part='{partNumber}', Sequence='{sequenceNumber}'.");
        return await _mySqlHelperServer.ExecuteReadWriteAsync(
            "Setup.DunnageParts.Load",
            dunnageTypeId,
            MySqlDatabaseTarget.MtmReceivingApplication,
            () => Task.FromResult(SetupDataCatalog.GetDunnageParts(dunnageTypeId, partNumber, sequenceNumber)),
            () => GetDunnagePartsFromBackendAsync(dunnageTypeId, partNumber, sequenceNumber, cancellationToken)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SetupDunnagePart>> GetAllDunnagePartsAsync(CancellationToken cancellationToken = default)
    {
        StartupDebugLog.Info("SetupDunnage", "GetAllDunnagePartsAsync started.");
        return await _mySqlHelperServer.ExecuteReadWriteAsync(
            "Setup.DunnageParts.LoadAll",
            null,
            MySqlDatabaseTarget.MtmReceivingApplication,
            () => Task.FromResult(SetupDataCatalog.GetAllDunnageParts()),
            () => GetAllDunnagePartsFromBackendAsync(cancellationToken)).ConfigureAwait(false);
    }

    public async Task<SetupSelectionResult> AddDunnageTypeAsync(string typeName, string currentUserRole, CancellationToken cancellationToken = default)
    {
        StartupDebugLog.Info("SetupDunnage", $"AddDunnageTypeAsync started. TypeName='{typeName}', Role='{currentUserRole}'.");

        if (!CanManageDefinitions(currentUserRole))
        {
            return new SetupSelectionResult
            {
                Success = false,
                Message = "You do not have permission to add dunnage types."
            };
        }

        if (string.IsNullOrWhiteSpace(typeName))
        {
            return new SetupSelectionResult
            {
                Success = false,
                Message = "Dunnage type name is required."
            };
        }

        _ = await SetupReceivingStoredProcedureScriptStore.LoadAsync("sp_setup_dunnage_type_insert", cancellationToken).ConfigureAwait(false);

        var affectedRows = await _mySqlHelperServer.ExecuteStoredProcedureNonQueryAsync(
            "sp_Dunnage_Types_Insert",
            new Dictionary<string, object?>
            {
                ["p_type_name"] = typeName.Trim(),
                ["p_icon"] = "PackageVariantClosed",
                ["p_image_path"] = string.Empty,
                ["p_user"] = GetCurrentUserName(),
                ["p_new_id"] = DBNull.Value,
            },
            MySqlDatabaseTarget.MtmReceivingApplication,
            cancellationToken).ConfigureAwait(false);

        StartupDebugLog.Info("SetupDunnage", $"AddDunnageTypeAsync completed. AffectedRows={affectedRows}.");

        return new SetupSelectionResult
        {
            Success = affectedRows >= 0,
            Message = affectedRows >= 0
                ? $"Dunnage type '{typeName.Trim()}' added."
                : "Unable to add dunnage type."
        };
    }

    public async Task<SetupSelectionResult> AddDunnagePartAsync(string dunnageTypeId, string partName, string currentUserRole, CancellationToken cancellationToken = default)
    {
        StartupDebugLog.Info("SetupDunnage", $"AddDunnagePartAsync started. TypeId='{dunnageTypeId}', PartName='{partName}', Role='{currentUserRole}'.");

        if (!CanManageDefinitions(currentUserRole))
        {
            return new SetupSelectionResult
            {
                Success = false,
                Message = "You do not have permission to add dunnage parts."
            };
        }

        if (!int.TryParse(dunnageTypeId, out var typeId))
        {
            return new SetupSelectionResult
            {
                Success = false,
                Message = "A valid dunnage type must be selected before adding a part."
            };
        }

        if (string.IsNullOrWhiteSpace(partName))
        {
            return new SetupSelectionResult
            {
                Success = false,
                Message = "Dunnage part name is required."
            };
        }

        _ = await SetupReceivingStoredProcedureScriptStore.LoadAsync("sp_setup_dunnage_part_insert", cancellationToken).ConfigureAwait(false);

        var affectedRows = await _mySqlHelperServer.ExecuteStoredProcedureNonQueryAsync(
            "sp_Dunnage_Parts_Insert",
            new Dictionary<string, object?>
            {
                ["p_part_id"] = partName.Trim(),
                ["p_type_id"] = typeId,
                ["p_spec_values"] = "{}",
                ["p_image_path"] = string.Empty,
                ["p_quantity_type"] = "Quantity",
                ["p_home_location"] = string.Empty,
                ["p_user"] = GetCurrentUserName(),
                ["p_new_id"] = DBNull.Value,
            },
            MySqlDatabaseTarget.MtmReceivingApplication,
            cancellationToken).ConfigureAwait(false);

        StartupDebugLog.Info("SetupDunnage", $"AddDunnagePartAsync completed. AffectedRows={affectedRows}.");

        return new SetupSelectionResult
        {
            Success = affectedRows >= 0,
            Message = affectedRows >= 0
                ? $"Dunnage part '{partName.Trim()}' added."
                : "Unable to add dunnage part."
        };
    }

    private async Task<IReadOnlyList<SetupDunnageType>> GetDunnageTypesFromBackendAsync(string partNumber, string sequenceNumber, CancellationToken cancellationToken)
    {
        StartupDebugLog.Info("SetupDunnage", "Loading receiving SQL script GetDunnageTypes.");
        _ = await SetupReceivingMySqlScriptStore.LoadAsync("GetDunnageTypes", cancellationToken).ConfigureAwait(false);

        var typeImageOverrides = await GetDunnageTypeImageOverridesAsync(cancellationToken).ConfigureAwait(false);

        var rows = await _mySqlHelperServer.ExecuteStoredProcedureQueryAsync(
            "sp_Dunnage_Types_GetAll",
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmReceivingApplication,
            cancellationToken).ConfigureAwait(false);

        StartupDebugLog.Info("SetupDunnage", $"sp_Dunnage_Types_GetAll returned {rows.Count} row(s).");

        if (rows.Count == 0)
        {
            StartupDebugLog.Info("SetupDunnage", "No dunnage types returned from receiving database.");
            return Array.Empty<SetupDunnageType>();
        }

        var results = rows
            .Select(row => new SetupDunnageType
            {
                Id = GetValueAsString(row, "id"),
                Name = GetValueAsString(row, "type_name"),
                IconGlyph = ResolveGlyph(GetValueAsString(row, "icon")),
                ImagePath = ResolveTypeImagePath(row, typeImageOverrides),
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Id) && !string.IsNullOrWhiteSpace(item.Name))
            .ToArray();

        if (_dunnageTypeVisibilityCatalogService is null)
        {
            StartupDebugLog.Info("SetupDunnage", $"Dunnage type projection completed. Result count={results.Length}. Visibility filter not configured.");
            return results;
        }

        var visibilityMap = await _dunnageTypeVisibilityCatalogService.GetVisibilityMapAsync(cancellationToken).ConfigureAwait(false);
        if (visibilityMap.Count == 0)
        {
            StartupDebugLog.Info("SetupDunnage", $"Dunnage type projection completed. Result count={results.Length}. No visibility overrides found.");
            return results;
        }

        var filteredResults = results
            .Where(item => !visibilityMap.TryGetValue(item.Id, out var isVisible) || isVisible)
            .ToArray();

        StartupDebugLog.Info("SetupDunnage", $"Dunnage type projection completed. Result count={results.Length}, FilteredCount={filteredResults.Length}.");

        return filteredResults;
    }

    private async Task<IReadOnlyList<SetupDunnagePart>> GetDunnagePartsFromBackendAsync(string dunnageTypeId, string partNumber, string sequenceNumber, CancellationToken cancellationToken)
    {
        StartupDebugLog.Info("SetupDunnage", "Loading receiving SQL script GetDunnageParts.");
        _ = await SetupReceivingMySqlScriptStore.LoadAsync("GetDunnageParts", cancellationToken).ConfigureAwait(false);

        if (!int.TryParse(dunnageTypeId, out var typeId))
        {
            StartupDebugLog.Info("SetupDunnage", $"DunnageTypeId '{dunnageTypeId}' is not numeric; returning empty backend result.");
            return Array.Empty<SetupDunnagePart>();
        }

        var rows = await _mySqlHelperServer.ExecuteStoredProcedureQueryAsync(
            "sp_Dunnage_Parts_GetByType",
            new Dictionary<string, object?>
            {
                ["p_type_id"] = typeId,
            },
            MySqlDatabaseTarget.MtmReceivingApplication,
            cancellationToken).ConfigureAwait(false);

        StartupDebugLog.Info("SetupDunnage", $"sp_Dunnage_Parts_GetByType returned {rows.Count} row(s) for typeId={typeId}.");

        if (rows.Count == 0)
        {
            StartupDebugLog.Info("SetupDunnage", "No dunnage parts returned from receiving database.");
            return Array.Empty<SetupDunnagePart>();
        }

        var results = rows
            .Select(row =>
            {
                var quantityType = GetValueAsString(row, "quantity_type");
                var homeLocation = GetValueAsString(row, "home_location");

                return new SetupDunnagePart
                {
                    Id = GetValueAsString(row, "id"),
                    TypeId = GetValueAsString(row, "type_id"),
                    PartNumber = GetValueAsString(row, "part_id"),
                    DisplayName = GetValueAsString(row, "part_id"),
                    ImagePath = ResolvePartOwnImagePath(row),
                    Metadata = BuildMetadata(quantityType, homeLocation),
                };
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Id) && !string.IsNullOrWhiteSpace(item.PartNumber))
            .ToArray();

            StartupDebugLog.Info("SetupDunnage", $"Dunnage part projection completed. Result count={results.Length}.");

            return results;
    }

    private async Task<IReadOnlyList<SetupDunnagePart>> GetAllDunnagePartsFromBackendAsync(CancellationToken cancellationToken)
    {
        StartupDebugLog.Info("SetupDunnage", "Loading receiving SQL script GetDunnageParts.");
        _ = await SetupReceivingMySqlScriptStore.LoadAsync("GetDunnageParts", cancellationToken).ConfigureAwait(false);

        var rows = await _mySqlHelperServer.ExecuteStoredProcedureQueryAsync(
            "sp_Dunnage_Parts_GetAll",
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmReceivingApplication,
            cancellationToken).ConfigureAwait(false);

        StartupDebugLog.Info("SetupDunnage", $"sp_Dunnage_Parts_GetAll returned {rows.Count} row(s).");

        if (rows.Count == 0)
        {
            StartupDebugLog.Info("SetupDunnage", "No dunnage parts returned from receiving database.");
            return Array.Empty<SetupDunnagePart>();
        }

        // Each part uses its own image (spec_values JSON "image_path" or
        // dunnage_parts.image_path). Parts without an image show a no-image
        // placeholder in the dialog rather than inheriting the parent type's image.
        var results = rows
            .Select(row =>
            {
                var quantityType = GetValueAsString(row, "quantity_type");
                var homeLocation = GetValueAsString(row, "home_location");

                return new SetupDunnagePart
                {
                    Id = GetValueAsString(row, "id"),
                    TypeId = GetValueAsString(row, "type_id"),
                    PartNumber = GetValueAsString(row, "part_id"),
                    DisplayName = GetValueAsString(row, "part_id"),
                    DunnageTypeName = GetValueAsString(row, "type_name"),
                    HomeLocation = homeLocation,
                    ImagePath = ResolvePartOwnImagePath(row),
                    Metadata = BuildMetadata(quantityType, homeLocation),
                };
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Id) && !string.IsNullOrWhiteSpace(item.PartNumber))
            .ToArray();

        StartupDebugLog.Info("SetupDunnage", $"Dunnage all-parts projection completed. Result count={results.Length}.");

        return results;
    }

    private async Task<IReadOnlyDictionary<string, string>> GetDunnageTypeImageOverridesAsync(CancellationToken cancellationToken)
    {
        var rows = await _mySqlHelperServer.ExecuteStoredProcedureQueryAsync(
            "sp_Dunnage_Parts_GetAll",
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmReceivingApplication,
            cancellationToken).ConfigureAwait(false);

        var overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var typeId = GetValueAsString(row, "type_id");
            if (string.IsNullOrWhiteSpace(typeId) || overrides.ContainsKey(typeId))
            {
                continue;
            }

            var imagePath = ResolvePartOwnImagePath(row);
            if (!string.IsNullOrWhiteSpace(imagePath))
            {
                overrides[typeId] = imagePath;
            }
        }

        StartupDebugLog.Info("SetupDunnage", $"Built dunnage type image overrides from part spec JSON. Count={overrides.Count}.");
        return overrides;
    }

    private static string ResolveTypeImagePath(IReadOnlyDictionary<string, object?> row, IReadOnlyDictionary<string, string> typeImageOverrides)
    {
        var typeId = GetValueAsString(row, "id");
        if (!string.IsNullOrWhiteSpace(typeId) && typeImageOverrides.TryGetValue(typeId, out var overridePath) && !string.IsNullOrWhiteSpace(overridePath))
        {
            return overridePath;
        }

        return GetValueAsString(row, "image_path");
    }

    private static string ResolvePartOwnImagePath(IReadOnlyDictionary<string, object?> row)
    {
        var fromSpecJson = TryGetImagePathFromJson(GetValueAsString(row, "spec_values"));
        if (!string.IsNullOrWhiteSpace(fromSpecJson))
        {
            return fromSpecJson;
        }

        return GetValueAsString(row, "image_path");
    }

    private static string TryGetImagePathFromJson(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return string.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(rawJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return string.Empty;
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!string.Equals(property.Name, "image_path", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var value = property.Value.GetString();
                return value?.Trim() ?? string.Empty;
            }
        }
        catch
        {
            return string.Empty;
        }

        return string.Empty;
    }

    private static string GetValueAsString(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return string.Empty;
        }

        return Convert.ToString(value)?.Trim() ?? string.Empty;
    }

    private static string ResolveGlyph(string sourceIcon)
    {
        if (string.IsNullOrWhiteSpace(sourceIcon))
        {
            return "\uE8B7";
        }

        return sourceIcon switch
        {
            "PackageVariantClosed" => "\uE7C1",
            "Folder" => "\uE8B7",
            _ => "\uE8B7",
        };
    }

    private static string BuildMetadata(string quantityType, string homeLocation)
    {
        var quantitySegment = string.IsNullOrWhiteSpace(quantityType)
            ? "Quantity"
            : quantityType;

        var locationSegment = string.IsNullOrWhiteSpace(homeLocation)
            ? "Unassigned"
            : homeLocation;

        return $"Quantity Type: {quantitySegment} | Home Location: {locationSegment}";
    }

    private static bool CanManageDefinitions(string currentUserRole)
    {
        return AllowedQuickAddRoles.Any(role => string.Equals(role, currentUserRole, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetCurrentUserName()
    {
        var userName = Environment.UserName?.Trim();
        if (string.IsNullOrWhiteSpace(userName))
        {
            return "waitlist_user";
        }

        return userName.Length > 50 ? userName[..50] : userName;
    }
}