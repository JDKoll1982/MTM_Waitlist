using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Module_Core.Services;

/// <inheritdoc cref="IMockMasterDataService"/>
public sealed class MockMasterDataService : IMockMasterDataService
{
    private readonly IMySqlHelperServer _mySqlHelperServer;

    public MockMasterDataService(IMySqlHelperServer mySqlHelperServer)
    {
        _mySqlHelperServer = mySqlHelperServer;
    }

    public async Task<IReadOnlyList<MockMasterTableDefinition>> GetMasterTablesAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _mySqlHelperServer
            .ExecuteStoredProcedureQueryAsync("sp_mock_master_tables_registry_get", new Dictionary<string, object?>(), MySqlDatabaseTarget.MtmWaitlist, cancellationToken)
            .ConfigureAwait(false);

        var result = new List<MockMasterTableDefinition>(rows.Count);
        foreach (var row in rows)
        {
            result.Add(new MockMasterTableDefinition
            {
                Id = As<long>(row, "id"),
                TableName = As<string>(row, "table_name") ?? string.Empty,
                UiDisplayName = As<string>(row, "ui_display_name") ?? string.Empty,
                DescriptionText = As<string>(row, "description_text") ?? string.Empty,
                IsActive = As<byte>(row, "is_active") != 0,
                SortRank = As<int>(row, "sort_rank"),
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ReadTableRowsAsync(
        string tableName,
        CancellationToken cancellationToken = default)
    {
        var normalized = tableName?.Trim() ?? string.Empty;
        var procedureName = ResolveGetProcedure(normalized);
        if (procedureName is null)
        {
            return Array.Empty<IReadOnlyDictionary<string, object?>>();
        }

        var rows = await _mySqlHelperServer
            .ExecuteStoredProcedureQueryAsync(procedureName, new Dictionary<string, object?>(), MySqlDatabaseTarget.MtmWaitlist, cancellationToken)
            .ConfigureAwait(false);

        var result = new List<IReadOnlyDictionary<string, object?>>(rows.Count);
        foreach (var row in rows)
        {
            result.Add(new Dictionary<string, object?>(row, StringComparer.OrdinalIgnoreCase));
        }

        return result;
    }

    public async Task<IReadOnlyList<MockMasterColumnDefinition>> GetTableColumnsAsync(
        string tableName,
        CancellationToken cancellationToken = default)
    {
        var normalized = tableName?.Trim() ?? string.Empty;
        if (ResolveGetProcedure(normalized) is null)
        {
            return Array.Empty<MockMasterColumnDefinition>();
        }

        var rows = await _mySqlHelperServer
            .ExecuteStoredProcedureQueryAsync(
                "sp_mock_master_table_columns_get",
                new Dictionary<string, object?> { ["@table_name"] = normalized },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken)
            .ConfigureAwait(false);

        var result = new List<MockMasterColumnDefinition>(rows.Count);
        foreach (var row in rows)
        {
            var name = As<string>(row, "column_name") ?? string.Empty;
            if (name.Length == 0)
            {
                continue;
            }

            result.Add(new MockMasterColumnDefinition
            {
                ColumnName = name,
                DataType = As<string>(row, "data_type") ?? string.Empty,
                IsNullable = string.Equals(As<string>(row, "is_nullable"), "YES", StringComparison.OrdinalIgnoreCase),
                IsAudit = As<byte>(row, "is_audit") != 0,
            });
        }

        return result;
    }

    /// <summary>
    /// Maps a mock master table name to its dedicated <c>get</c> stored procedure, or null when the
    /// table is not an allowed mock master table. Real (non-mock) catalog tables
    /// (<c>waitlist_request_types</c>/<c>waitlist_request_subtypes</c>) are intentionally excluded:
    /// they are NOT mock data and have their own dedicated SPs + services.
    /// </summary>
    private static string? ResolveGetProcedure(string tableName) => tableName.ToLowerInvariant() switch
    {
        "mock_master_tables_registry" => "sp_mock_master_tables_registry_get",
        "mock_parts" => "sp_mock_parts_get",
        "mock_work_orders" => "sp_mock_work_orders_get",
        "mock_work_centers" => "sp_mock_work_centers_get",
        "mock_locations" => "sp_mock_locations_get",
        "mock_requesters" => "sp_mock_requesters_get",
        "mock_inventory_locations" => "sp_mock_inventory_locations_get",
        "mock_request_types" => "sp_mock_request_types_get",
        _ => null,
    };

    /// <summary>
    /// The editable business columns accepted by each mock table's insert/update stored procedures,
    /// keyed by table name. These match the generated per-table SP signatures exactly (the SPs manage
    /// <c>public_id</c>, <c>is_active</c> default, and audit timestamps internally, so those are not
    /// accepted as parameters). The mock registry itself is not user-edited.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string[]> EditableColumnsByTable =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["mock_parts"] = new[] { "part_number", "part_category", "part_description", "unit_of_measure" },
            ["mock_work_orders"] = new[] { "work_order_number", "part_number", "work_center_code", "is_coil_bearing" },
            ["mock_work_centers"] = new[] { "work_center_code", "work_center_description", "building" },
            ["mock_locations"] = new[] { "location_code", "location_description", "is_ignored" },
            ["mock_requesters"] = new[] { "employee_number", "display_name" },
            ["mock_inventory_locations"] = new[] { "part_number", "location_code", "on_hand_quantity" },
            ["mock_request_types"] = new[] { "request_type", "subtype" },
        };

    public async Task<bool> AddTableRowAsync(string tableName, IReadOnlyDictionary<string, object?> values, CancellationToken cancellationToken = default)
    {
        var normalized = tableName?.Trim() ?? string.Empty;
        if (!EditableColumnsByTable.TryGetValue(normalized, out var editable) || editable.Length == 0)
        {
            return false;
        }

        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var column in editable)
        {
            parameters[$"p_{column}"] = ReadValue(values, column);
        }

        var affected = await _mySqlHelperServer
            .ExecuteStoredProcedureNonQueryAsync($"sp_{normalized.ToLowerInvariant()}_insert", parameters, MySqlDatabaseTarget.MtmWaitlist, cancellationToken)
            .ConfigureAwait(false);
        return affected > 0;
    }

    public async Task<bool> UpdateTableRowAsync(string tableName, long id, IReadOnlyDictionary<string, object?> values, CancellationToken cancellationToken = default)
    {
        var normalized = tableName?.Trim() ?? string.Empty;
        if (!EditableColumnsByTable.TryGetValue(normalized, out var editable) || editable.Length == 0)
        {
            return false;
        }

        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["p_id"] = id,
        };
        foreach (var column in editable)
        {
            parameters[$"p_{column}"] = ReadValue(values, column);
        }

        var affected = await _mySqlHelperServer
            .ExecuteStoredProcedureNonQueryAsync($"sp_{normalized.ToLowerInvariant()}_update", parameters, MySqlDatabaseTarget.MtmWaitlist, cancellationToken)
            .ConfigureAwait(false);
        return affected > 0;
    }

    public async Task<bool> DeleteTableRowAsync(string tableName, long id, CancellationToken cancellationToken = default)
    {
        var normalized = tableName?.Trim() ?? string.Empty;
        if (!EditableColumnsByTable.ContainsKey(normalized))
        {
            return false;
        }

        var affected = await _mySqlHelperServer
            .ExecuteStoredProcedureNonQueryAsync($"sp_{normalized.ToLowerInvariant()}_delete", new Dictionary<string, object?> { ["p_id"] = id }, MySqlDatabaseTarget.MtmWaitlist, cancellationToken)
            .ConfigureAwait(false);
        return affected > 0;
    }

    private static object? ReadValue(IReadOnlyDictionary<string, object?> values, string column)
    {
        if (!values.TryGetValue(column, out var value) || value is null || value == DBNull.Value)
        {
            return DBNull.Value;
        }

        return value;
    }

    private static T? As<T>(IReadOnlyDictionary<string, object?> row, string column)
    {
        if (!row.TryGetValue(column, out var value) || value is null || value == DBNull.Value)
        {
            return default;
        }

        return (T)Convert.ChangeType(value, typeof(T));
    }
}
