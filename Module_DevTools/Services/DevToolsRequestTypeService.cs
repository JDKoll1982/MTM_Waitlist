using Microsoft.Extensions.Options;

using System.Data;

using MySqlConnector;

using MTM_Waitlist.Module_DevTools.Models;

namespace MTM_Waitlist.Module_DevTools.Services;

public sealed class DevToolsRequestTypeService : IDevToolsRequestTypeService
{
    private readonly DevToolsDatabaseOptions _databaseOptions;

    public DevToolsRequestTypeService(IOptions<DevToolsDatabaseOptions> databaseOptions)
    {
        ArgumentNullException.ThrowIfNull(databaseOptions);
        _databaseOptions = databaseOptions.Value;
    }

    public async Task SaveRequestTypeAsync(
        RequestTypeDefinition requestTypeDefinition,
        string createdByUsername,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestTypeDefinition);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdByUsername);

        if (requestTypeDefinition.CardFields.Count > 5)
        {
            throw new InvalidOperationException("Card fields cannot exceed 5 entries.");
        }

        var normalizedRequestTypeName = NormalizeText(requestTypeDefinition.RequestTypeName);
        if (string.IsNullOrWhiteSpace(normalizedRequestTypeName))
        {
            throw new InvalidOperationException("Request type name is required.");
        }

        var connectionString = ResolveConnectionString();
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var requestTypeId = await CreateRequestTypeRowAsync(
                connection,
                transaction,
                requestTypeDefinition,
                createdByUsername.Trim(),
                normalizedRequestTypeName,
                cancellationToken);

            for (var index = 0; index < requestTypeDefinition.CardFields.Count; index++)
            {
                var cardField = requestTypeDefinition.CardFields[index];
                await CreateFieldRowAsync(
                    connection,
                    transaction,
                    "sp_ops_request_type_card_fields_create",
                    requestTypeId,
                    cardField,
                    index + 1,
                    cancellationToken);
            }

            for (var index = 0; index < requestTypeDefinition.DetailFields.Count; index++)
            {
                var detailField = requestTypeDefinition.DetailFields[index];
                await CreateFieldRowAsync(
                    connection,
                    transaction,
                    "sp_ops_request_type_detail_fields_create",
                    requestTypeId,
                    detailField,
                    index + 1,
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<long> CreateRequestTypeRowAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        RequestTypeDefinition requestTypeDefinition,
        string createdByUsername,
        string normalizedRequestTypeName,
        CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand(
            "sp_ops_request_types_create",
            connection,
            transaction)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@publicId", Guid.NewGuid().ToString());
        command.Parameters.AddWithValue("@requestTypeName", requestTypeDefinition.RequestTypeName.Trim());
        command.Parameters.AddWithValue("@requestTypeNameNormalized", normalizedRequestTypeName);

        if (string.IsNullOrWhiteSpace(requestTypeDefinition.ImageFilePath))
        {
            command.Parameters.AddWithValue("@imageFilePath", DBNull.Value);
        }
        else
        {
            command.Parameters.AddWithValue("@imageFilePath", requestTypeDefinition.ImageFilePath.Trim());
        }

        command.Parameters.AddWithValue("@createdByUsername", createdByUsername);
        command.Parameters.AddWithValue("@createdUtc", DateTime.UtcNow);
        command.Parameters.AddWithValue("@updatedUtc", DateTime.UtcNow);

        var outputParameter = new MySqlParameter("@requestTypeId", MySqlDbType.Int64)
        {
            Direction = System.Data.ParameterDirection.Output
        };

        command.Parameters.Add(outputParameter);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return Convert.ToInt64(outputParameter.Value);
    }

    private static async Task CreateFieldRowAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        string storedProcedureName,
        long requestTypeId,
        RequestTypeFieldDefinition field,
        int displayOrder,
        CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand(
            storedProcedureName,
            connection,
            transaction)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@publicId", Guid.NewGuid().ToString());
        command.Parameters.AddWithValue("@requestTypeId", requestTypeId);
        command.Parameters.AddWithValue("@fieldName", field.FieldName);
        command.Parameters.AddWithValue("@fieldNameNormalized", NormalizeText(field.FieldName));
        command.Parameters.AddWithValue("@dataTypeName", field.DataType.ToDatabaseValue());
        command.Parameters.AddWithValue("@displayOrder", displayOrder);
        command.Parameters.AddWithValue("@createdUtc", DateTime.UtcNow);
        command.Parameters.AddWithValue("@updatedUtc", DateTime.UtcNow);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private string ResolveConnectionString()
    {
        var environmentVariableName = _databaseOptions.ConnectionStringEnvironmentVariable?.Trim();
        if (!string.IsNullOrWhiteSpace(environmentVariableName))
        {
            var environmentConnectionString = Environment.GetEnvironmentVariable(environmentVariableName)?.Trim();
            if (!string.IsNullOrWhiteSpace(environmentConnectionString))
            {
                return BuildTimeoutConnectionString(environmentConnectionString);
            }
        }

        var configuredConnectionString = _databaseOptions.ConnectionString?.Trim();
        if (!string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            return BuildTimeoutConnectionString(configuredConnectionString);
        }

        throw new InvalidOperationException("No database connection string was configured for Module_DevTools.");
    }

    private string BuildTimeoutConnectionString(string connectionString)
    {
        var builder = new MySqlConnectionStringBuilder(connectionString)
        {
            ConnectionTimeout = (uint)Math.Max(1, _databaseOptions.ConnectionTimeoutSeconds)
        };

        return builder.ConnectionString;
    }

    private static string NormalizeText(string value)
    {
        return value.Trim().ToLowerInvariant();
    }
}
