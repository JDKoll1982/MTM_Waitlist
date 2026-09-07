using System.Text.Json;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Services;

/// <inheritdoc cref="IRequestTypeEditorService"/>
public sealed class RequestTypeEditorService : IRequestTypeEditorService
{
    private const MySqlDatabaseTarget DatabaseTarget = MySqlDatabaseTarget.MtmWaitlist;

    private readonly IMySqlHelperServer _mySqlHelperServer;

    public RequestTypeEditorService(IMySqlHelperServer mySqlHelperServer)
    {
        _mySqlHelperServer = mySqlHelperServer;
    }

    public async Task<IReadOnlyList<RequestTypeEditorItem>> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        var types = await _mySqlHelperServer
            .ExecuteStoredProcedureQueryAsync(
                "sp_waitlist_request_types_get_all",
                new Dictionary<string, object?>(),
                DatabaseTarget,
                cancellationToken)
            .ConfigureAwait(false);

        var subtypes = await _mySqlHelperServer
            .ExecuteStoredProcedureQueryAsync(
                "sp_waitlist_request_subtypes_get_all",
                new Dictionary<string, object?>(),
                DatabaseTarget,
                cancellationToken)
            .ConfigureAwait(false);

        var subtypesByType = subtypes
            .GroupBy(row => As<long>(row, "request_type_id"))
            .ToDictionary(g => g.Key, g => g.Select(MapSubtype).ToList());

        var result = new List<RequestTypeEditorItem>(types.Count);
        foreach (var row in types)
        {
            var id = As<long>(row, "id");
            var item = MapType(row);
            if (subtypesByType.TryGetValue(id, out var children))
            {
                item.Subtypes.AddRange(children);
            }

            result.Add(item);
        }

        return result;
    }

    public Task<int> AddSubtypeAsync(RequestSubtypeEditorItem subtype, CancellationToken cancellationToken = default)
        => ExecuteNonQueryAsync("sp_waitlist_request_subtypes_insert", SubtypeParameters(subtype, includeId: false), cancellationToken);

    public Task<int> UpdateSubtypeAsync(RequestSubtypeEditorItem subtype, CancellationToken cancellationToken = default)
        => ExecuteNonQueryAsync("sp_waitlist_request_subtypes_update", SubtypeParameters(subtype, includeId: true), cancellationToken);

    public Task<int> DeleteSubtypeAsync(long subtypeId, CancellationToken cancellationToken = default)
        => ExecuteNonQueryAsync(
            "sp_waitlist_request_subtypes_delete",
            new Dictionary<string, object?> { ["p_id"] = subtypeId },
            cancellationToken);

    public Task<int> UpdateTypeAsync(RequestTypeEditorItem type, CancellationToken cancellationToken = default)
        => ExecuteNonQueryAsync(
            "sp_waitlist_request_types_update",
            new Dictionary<string, object?>
            {
                ["p_id"] = type.Id,
                ["p_request_type"] = type.Name,
                ["p_control"] = type.Control,
                ["p_flow"] = type.Flow,
                ["p_requires_text_input"] = ToByte(type.RequiresTextInput),
                ["p_prompt_text"] = type.PromptText,
                ["p_min_length"] = type.MinLength,
                ["p_max_length"] = type.MaxLength,
                ["p_default_image_path"] = type.DefaultImagePath,
                ["p_center_data_grid_fields_json"] = SerializeFields(type.CenterDataGridFields),
                ["p_is_active"] = ToByte(type.IsActive),
            },
            cancellationToken);

    private async Task<int> ExecuteNonQueryAsync(
        string procedure,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken)
        => await _mySqlHelperServer
            .ExecuteStoredProcedureNonQueryAsync(procedure, parameters, DatabaseTarget, cancellationToken)
            .ConfigureAwait(false);

    private static Dictionary<string, object?> SubtypeParameters(RequestSubtypeEditorItem s, bool includeId)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["p_request_type_id"] = s.RequestTypeId,
            ["p_subtype_name"] = s.Name,
            ["p_control"] = s.Control,
            ["p_flow"] = s.Flow,
            ["p_requires_text_input"] = ToByte(s.RequiresTextInput),
            ["p_prompt_text"] = s.PromptText,
            ["p_min_length"] = s.MinLength,
            ["p_max_length"] = s.MaxLength,
            ["p_default_image_path"] = s.DefaultImagePath,
            ["p_center_data_grid_fields_json"] = SerializeFields(s.CenterDataGridFields),
            ["p_is_active"] = ToByte(s.IsActive),
        };

        if (includeId)
        {
            parameters["p_id"] = s.Id;
        }

        return parameters;
    }

    private static RequestTypeEditorItem MapType(IReadOnlyDictionary<string, object?> row) => new()
    {
        Id = As<long>(row, "id"),
        PublicId = As<string>(row, "public_id"),
        Name = As<string>(row, "request_type") ?? string.Empty,
        Control = As<string>(row, "control") ?? string.Empty,
        Flow = AsString(row, "flow", "direct-to-confirmation"),
        RequiresTextInput = As<byte>(row, "requires_text_input") != 0,
        PromptText = As<string>(row, "prompt_text") ?? string.Empty,
        MinLength = AsInt(row, "min_length"),
        MaxLength = AsInt(row, "max_length", 200),
        DefaultImagePath = As<string>(row, "default_image_path"),
        CenterDataGridFields = ReadStringList(row, "center_data_grid_fields_json"),
        IsActive = As<byte>(row, "is_active", 1) != 0,
    };

    private static RequestSubtypeEditorItem MapSubtype(IReadOnlyDictionary<string, object?> row) => new()
    {
        Id = As<long>(row, "id"),
        PublicId = As<string>(row, "public_id"),
        RequestTypeId = As<long>(row, "request_type_id"),
        Name = As<string>(row, "subtype_name") ?? string.Empty,
        Control = As<string>(row, "control") ?? string.Empty,
        Flow = AsString(row, "flow", "direct-to-confirmation"),
        RequiresTextInput = As<byte>(row, "requires_text_input") != 0,
        PromptText = As<string>(row, "prompt_text") ?? string.Empty,
        MinLength = AsInt(row, "min_length"),
        MaxLength = AsInt(row, "max_length", 200),
        DefaultImagePath = As<string>(row, "default_image_path"),
        CenterDataGridFields = ReadStringList(row, "center_data_grid_fields_json"),
        IsActive = As<byte>(row, "is_active", 1) != 0,
    };

    private static string? SerializeFields(List<string>? fields)
    {
        if (fields is null || fields.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(fields);
    }

    private static List<string> ReadStringList(IReadOnlyDictionary<string, object?> row, string column)
    {
        if (!row.TryGetValue(column, out var value) || value is null || value == DBNull.Value)
        {
            return new List<string>();
        }

        var json = value as string;
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<List<string>>(json);
            return parsed ?? new List<string>();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }

    private static byte ToByte(bool value) => (byte)(value ? 1 : 0);

    private static string AsString(IReadOnlyDictionary<string, object?> row, string column, string fallback)
        => As<string>(row, column) is { Length: > 0 } value ? value : fallback;

    private static int AsInt(IReadOnlyDictionary<string, object?> row, string column, int fallback = 0)
    {
        if (!row.TryGetValue(column, out var value) || value is null || value == DBNull.Value)
        {
            return fallback;
        }

        try
        {
            return Convert.ToInt32(value);
        }
        catch (Exception)
        {
            return fallback;
        }
    }

    private static T? As<T>(IReadOnlyDictionary<string, object?> row, string column, T? fallback = default)
    {
        if (!row.TryGetValue(column, out var value) || value is null || value == DBNull.Value)
        {
            return fallback;
        }

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch (Exception)
        {
            return fallback;
        }
    }
}
