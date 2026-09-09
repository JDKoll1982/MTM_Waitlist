using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>Outcome of a defect-type mutation (add/update/delete).</summary>
public sealed record DefectTypeMutationResult(bool Success, string Message)
{
    public static DefectTypeMutationResult Ok(string message) => new(true, message);

    public static DefectTypeMutationResult Fail(string message) => new(false, message);
}

/// <summary>
/// Read/write editor for the managed NCM defect-type list
/// (<c>mtm_waitlist.waitlist_defect_types</c>) consumed by the Module_Settings defect editor (Phase 5)
/// and, later, the Pickup NCM Item picker. SP-only access. Mutations are role-gated to Admin/Developer.
/// </summary>
public interface IDefectTypeCatalogService
{
    /// <summary>Lists the active defect types (worker + editor list) ordered by sort_order then name.</summary>
    Task<IReadOnlyList<DefectTypeDefinition>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<DefectTypeMutationResult> AddAsync(
        string name,
        string? description,
        int sortOrder,
        string? currentUserRole,
        CancellationToken cancellationToken = default);

    Task<DefectTypeMutationResult> UpdateAsync(
        long id,
        string name,
        string? description,
        int sortOrder,
        string? currentUserRole,
        CancellationToken cancellationToken = default);

    Task<DefectTypeMutationResult> DeleteAsync(
        long id,
        string? currentUserRole,
        CancellationToken cancellationToken = default);

    /// <summary>Whether the given role may manage defect types (Admin/Developer).</summary>
    bool CanManage(string? currentUserRole);
}

/// <inheritdoc cref="IDefectTypeCatalogService"/>
public sealed class DefectTypeCatalogService : IDefectTypeCatalogService
{
    private const MySqlDatabaseTarget DatabaseTarget = MySqlDatabaseTarget.MtmWaitlist;

    private static readonly string[] AllowedRoles = { "admin", "administrator", "developer" };

    private readonly IMySqlHelperServer _mySqlHelperServer;

    public DefectTypeCatalogService(IMySqlHelperServer mySqlHelperServer)
    {
        _mySqlHelperServer = mySqlHelperServer;
    }

    public async Task<IReadOnlyList<DefectTypeDefinition>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _mySqlHelperServer
            .ExecuteStoredProcedureQueryAsync("sp_waitlist_defect_types_get_all", new Dictionary<string, object?>(), DatabaseTarget, cancellationToken)
            .ConfigureAwait(false);

        return rows
            .Select(row => new DefectTypeDefinition
            {
                Id = AsLong(row, "id"),
                PublicId = AsString(row, "public_id"),
                Name = AsString(row, "defect_name"),
                Description = AsString(row, "description"),
                SortOrder = AsInt(row, "sort_order"),
                IsActive = AsBool(row, "is_active"),
            })
            .ToArray();
    }

    public async Task<DefectTypeMutationResult> AddAsync(
        string name, string? description, int sortOrder, string? currentUserRole, CancellationToken cancellationToken = default)
    {
        if (!CanManage(currentUserRole))
        {
            return DefectTypeMutationResult.Fail("Only Admin/Developer roles can add defect types.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return DefectTypeMutationResult.Fail("A defect name is required.");
        }

        var affected = await ExecuteNonQueryAsync(
            "sp_waitlist_defect_types_insert",
            new Dictionary<string, object?>
            {
                ["p_defect_name"] = name.Trim(),
                ["p_description"] = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                ["p_sort_order"] = sortOrder,
                ["p_created_by_user_id"] = null,
            },
            cancellationToken).ConfigureAwait(false);

        return affected > 0
            ? DefectTypeMutationResult.Ok("Defect type added.")
            : DefectTypeMutationResult.Fail("Unable to add defect type (duplicate name?).");
    }

    public async Task<DefectTypeMutationResult> UpdateAsync(
        long id, string name, string? description, int sortOrder, string? currentUserRole, CancellationToken cancellationToken = default)
    {
        if (!CanManage(currentUserRole))
        {
            return DefectTypeMutationResult.Fail("Only Admin/Developer roles can edit defect types.");
        }

        if (id <= 0 || string.IsNullOrWhiteSpace(name))
        {
            return DefectTypeMutationResult.Fail("A valid id and defect name are required.");
        }

        var affected = await ExecuteNonQueryAsync(
            "sp_waitlist_defect_types_update",
            new Dictionary<string, object?>
            {
                ["p_id"] = id,
                ["p_defect_name"] = name.Trim(),
                ["p_description"] = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                ["p_sort_order"] = sortOrder,
                ["p_updated_by_user_id"] = null,
            },
            cancellationToken).ConfigureAwait(false);

        return affected > 0
            ? DefectTypeMutationResult.Ok("Defect type updated.")
            : DefectTypeMutationResult.Fail("Unable to update defect type (not found or duplicate name?).");
    }

    public async Task<DefectTypeMutationResult> DeleteAsync(long id, string? currentUserRole, CancellationToken cancellationToken = default)
    {
        if (!CanManage(currentUserRole))
        {
            return DefectTypeMutationResult.Fail("Only Admin/Developer roles can remove defect types.");
        }

        if (id <= 0)
        {
            return DefectTypeMutationResult.Fail("A valid defect type id is required.");
        }

        var affected = await ExecuteNonQueryAsync(
            "sp_waitlist_defect_types_delete",
            new Dictionary<string, object?> { ["p_id"] = id },
            cancellationToken).ConfigureAwait(false);

        return affected > 0
            ? DefectTypeMutationResult.Ok("Defect type removed.")
            : DefectTypeMutationResult.Fail("Unable to remove defect type (not found).");
    }

    public bool CanManage(string? currentUserRole)
        => !string.IsNullOrWhiteSpace(currentUserRole)
            && AllowedRoles.Contains(currentUserRole.Trim(), StringComparer.OrdinalIgnoreCase);

    private async Task<int> ExecuteNonQueryAsync(
        string procedure, IReadOnlyDictionary<string, object?> parameters, CancellationToken cancellationToken)
        => await _mySqlHelperServer
            .ExecuteStoredProcedureNonQueryAsync(procedure, parameters, DatabaseTarget, cancellationToken)
            .ConfigureAwait(false);

    private static string AsString(IReadOnlyDictionary<string, object?> row, string key)
        => row.TryGetValue(key, out var value) && value is not null && value != DBNull.Value
            ? Convert.ToString(value)?.Trim() ?? string.Empty
            : string.Empty;

    private static long AsLong(IReadOnlyDictionary<string, object?> row, string key)
        => row.TryGetValue(key, out var value) && value is not null && value != DBNull.Value
            ? Convert.ToInt64(value)
            : 0;

    private static int AsInt(IReadOnlyDictionary<string, object?> row, string key)
        => row.TryGetValue(key, out var value) && value is not null && value != DBNull.Value
            ? Convert.ToInt32(value)
            : 0;

    private static bool AsBool(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null || value == DBNull.Value)
        {
            return false;
        }

        return value switch
        {
            bool b => b,
            byte by => by != 0,
            sbyte sb => sb != 0,
            long l => l != 0,
            _ => Convert.ToInt32(value) != 0,
        };
    }
}
