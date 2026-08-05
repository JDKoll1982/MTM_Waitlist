using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Services;

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

    public DunnageWorkflowService(MySqlHelperServer mySqlHelperServer)
    {
        _mySqlHelperServer = mySqlHelperServer;
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
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Id) && !string.IsNullOrWhiteSpace(item.Name))
            .ToArray();

        StartupDebugLog.Info("SetupDunnage", $"Dunnage type projection completed. Result count={results.Length}.");

        return results;
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
                    ImagePath = GetValueAsString(row, "image_path"),
                    Metadata = BuildMetadata(quantityType, homeLocation),
                };
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Id) && !string.IsNullOrWhiteSpace(item.PartNumber))
            .ToArray();

            StartupDebugLog.Info("SetupDunnage", $"Dunnage part projection completed. Result count={results.Length}.");

            return results;
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