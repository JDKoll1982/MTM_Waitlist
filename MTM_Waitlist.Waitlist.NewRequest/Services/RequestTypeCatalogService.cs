using System.Text.Json;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

/// <summary>
/// Reads the real (non-mock) request-type/subtype catalog from the <c>mtm_waitlist</c> database
/// (<c>waitlist_request_types</c> + <c>waitlist_request_subtypes</c>), which replaced
/// <c>Assets/Config/waitlist-request-types.json</c> as the source of truth for the New-Request wizard.
/// Returns the same <see cref="NewRequestTypeDefinition"/> shape the JSON parser produced so the
/// wizard's call sites are unchanged.
/// </summary>
public interface IRequestTypeCatalogService
{
    Task<IReadOnlyList<NewRequestTypeDefinition>> LoadRequestTypesAsync(CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="IRequestTypeCatalogService"/>
public sealed class RequestTypeCatalogService : IRequestTypeCatalogService
{
    private readonly IMySqlHelperServer _mySqlHelperServer;

    public RequestTypeCatalogService(IMySqlHelperServer mySqlHelperServer)
    {
        _mySqlHelperServer = mySqlHelperServer;
    }

    public async Task<IReadOnlyList<NewRequestTypeDefinition>> LoadRequestTypesAsync(CancellationToken cancellationToken = default)
    {
        var requestTypes = await _mySqlHelperServer
            .ExecuteStoredProcedureQueryAsync("sp_waitlist_request_types_get", new Dictionary<string, object?>(), MySqlDatabaseTarget.MtmWaitlist, cancellationToken)
            .ConfigureAwait(false);

        var subtypes = await _mySqlHelperServer
            .ExecuteStoredProcedureQueryAsync("sp_waitlist_request_subtypes_get", new Dictionary<string, object?>(), MySqlDatabaseTarget.MtmWaitlist, cancellationToken)
            .ConfigureAwait(false);

        var result = new List<NewRequestTypeDefinition>(requestTypes.Count);
        var subtypesByParent = subtypes
            .GroupBy(row => As<long>(row, "request_type_id"))
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var row in requestTypes)
        {
            var id = As<long>(row, "id");
            var definition = new NewRequestTypeDefinition
            {
                RequestType = As<string>(row, "request_type") ?? string.Empty,
                Control = As<string>(row, "control") ?? string.Empty,
                Flow = AsString(row, "flow", "direct-to-confirmation"),
                RequiresTextInput = As<byte>(row, "requires_text_input") != 0,
                PromptText = As<string>(row, "prompt_text") ?? string.Empty,
                MinLength = AsInt(row, "min_length"),
                MaxLength = AsInt(row, "max_length", 200),
                CenterDataGridFields = ReadStringList(row, "center_data_grid_fields_json"),
            };

            if (subtypesByParent.TryGetValue(id, out var subtypeRows))
            {
                foreach (var subtypeRow in subtypeRows)
                {
                    definition.Subtypes.Add(new NewRequestSubtypeDefinition
                    {
                        Name = As<string>(subtypeRow, "subtype_name") ?? string.Empty,
                        Control = As<string>(subtypeRow, "control") ?? string.Empty,
                        Flow = AsString(subtypeRow, "flow", "direct-to-confirmation"),
                        RequiresTextInput = As<byte>(subtypeRow, "requires_text_input") != 0,
                        PromptText = As<string>(subtypeRow, "prompt_text") ?? string.Empty,
                        MinLength = AsInt(subtypeRow, "min_length"),
                        MaxLength = AsInt(subtypeRow, "max_length", 200),
                        CenterDataGridFields = ReadStringList(subtypeRow, "center_data_grid_fields_json"),
                    });
                }
            }

            result.Add(definition);
        }

        return result;
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

    private static string AsString(IReadOnlyDictionary<string, object?> row, string column, string fallback)
        => As<string>(row, column) is { Length: > 0 } value ? value : fallback;

    private static int AsInt(IReadOnlyDictionary<string, object?> row, string column, int fallback = 0)
    {
        if (!row.TryGetValue(column, out var value) || value is null || value == DBNull.Value)
        {
            return fallback;
        }

        if (value is bool boolValue)
        {
            return boolValue ? 1 : 0;
        }

        if (value is int intValue)
        {
            return intValue;
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

    private static T? As<T>(IReadOnlyDictionary<string, object?> row, string column)
    {
        if (!row.TryGetValue(column, out var value) || value is null || value == DBNull.Value)
        {
            return default;
        }

        // TINYINT(1)/BIT columns come back from the driver as bool; Convert.ChangeType on a
        // bool to a numeric target throws InvalidCastException. Guard BEFORE converting so the
        // exception is never raised (not merely caught).
        if (value is bool boolValue)
        {
            if (typeof(T) == typeof(bool))
            {
                return (T)(object)boolValue;
            }

            try
            {
                return (T)Convert.ChangeType(boolValue ? 1 : 0, typeof(T));
            }
            catch (Exception)
            {
                return default;
            }
        }

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch (Exception)
        {
            return default;
        }
    }
}
