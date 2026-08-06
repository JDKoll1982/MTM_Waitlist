using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MTM_Waitlist.Module_Core.Helpers;
using System.Runtime.InteropServices;

namespace MTM_Waitlist.Module_Setup.Services;

public sealed class InforVisualSqlQueryService
{
    private const string InforVisualConnectionStringEnvironmentVariable = "INFOR_VISUAL_SQL_CONNECTION_STRING";
    private const string InforVisualServerEnvironmentVariable = "INFOR_VISUAL_SQL_SERVER";
    private const string InforVisualDatabaseEnvironmentVariable = "INFOR_VISUAL_SQL_DATABASE";
    private const string InforVisualUserEnvironmentVariable = "INFOR_VISUAL_SQL_USER";
    private const string InforVisualPasswordEnvironmentVariable = "INFOR_VISUAL_SQL_PASSWORD";

    private readonly IConfiguration _configuration;

    public InforVisualSqlQueryService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteQueueAsync(
        string scriptName,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        StartupDebugLog.Info("SetupLookup.Sql", $"ExecuteQueueAsync started. Script='{scriptName}', ParamCount={parameters.Count}.");
        var script = await SetupSqlScriptStore.LoadAsync(scriptName, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(script))
        {
            StartupDebugLog.Info("SetupLookup.Sql", $"Script load returned empty content for '{scriptName}'.");
            return Array.Empty<Dictionary<string, object?>>();
        }

        var connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            StartupDebugLog.Info("SetupLookup.Sql", $"Connection string resolved empty for script '{scriptName}'.");
            return Array.Empty<Dictionary<string, object?>>();
        }

        var parameterSummary = string.Join(", ", parameters.Select(entry => $"{entry.Key}='{Convert.ToString(entry.Value) ?? string.Empty}'"));
        StartupDebugLog.Info("SetupLookup.Sql", $"Executing script '{scriptName}' with parameters: {parameterSummary}.");

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = new SqlCommand(script, connection)
            {
                CommandType = System.Data.CommandType.Text,
                CommandTimeout = 15,
            };

            foreach (var parameter in parameters)
            {
                var parameterName = parameter.Key.StartsWith("@", StringComparison.Ordinal)
                    ? parameter.Key
                    : $"@{parameter.Key}";

                _ = command.Parameters.AddWithValue(parameterName, parameter.Value ?? DBNull.Value);
            }

            var rows = new List<Dictionary<string, object?>>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                for (var index = 0; index < reader.FieldCount; index++)
                {
                    row[reader.GetName(index)] = reader.IsDBNull(index) ? null : reader.GetValue(index);
                }

                rows.Add(row);
            }

            StartupDebugLog.Info("SetupLookup.Sql", $"Script '{scriptName}' executed successfully. RowCount={rows.Count}.");

            return rows;
        }
        catch (SqlException sqlException)
        {
            StartupDebugLog.Error(
                "SetupLookup.Sql",
                sqlException,
                $"SQL error executing '{scriptName}'. Number={sqlException.Number}, State={sqlException.State}, Line={sqlException.LineNumber}.");
            return Array.Empty<Dictionary<string, object?>>();
        }
        catch (COMException comException)
        {
            StartupDebugLog.Error(
                "SetupLookup.Sql",
                comException,
                $"COM error executing '{scriptName}'. HResult=0x{comException.HResult:X8}.");
            return Array.Empty<Dictionary<string, object?>>();
        }
        catch (Exception exception)
        {
            StartupDebugLog.Error("SetupLookup.Sql", exception, $"Unhandled error executing '{scriptName}'.");
            return Array.Empty<Dictionary<string, object?>>();
        }
    }

    private string ResolveConnectionString()
    {
        var environmentConnectionString = Environment.GetEnvironmentVariable(InforVisualConnectionStringEnvironmentVariable)?.Trim();
        if (!string.IsNullOrWhiteSpace(environmentConnectionString))
        {
            return environmentConnectionString;
        }

        var defaultServer = _configuration["InforVisualDatabaseOptions:Server"];
        var defaultDatabase = _configuration["InforVisualDatabaseOptions:Database"];
        var defaultUser = _configuration["InforVisualDatabaseOptions:User"];
        var defaultPassword = _configuration["InforVisualDatabaseOptions:Password"];
        var defaultTimeoutText = _configuration["InforVisualDatabaseOptions:ConnectionTimeoutSeconds"];
        var defaultTimeout = int.TryParse(defaultTimeoutText, out var parsedTimeout) ? parsedTimeout : 10;

        var server = Environment.GetEnvironmentVariable(InforVisualServerEnvironmentVariable)?.Trim();
        var database = Environment.GetEnvironmentVariable(InforVisualDatabaseEnvironmentVariable)?.Trim();
        var user = Environment.GetEnvironmentVariable(InforVisualUserEnvironmentVariable)?.Trim();
        var password = Environment.GetEnvironmentVariable(InforVisualPasswordEnvironmentVariable)?.Trim();

        server = string.IsNullOrWhiteSpace(server) ? defaultServer : server;
        database = string.IsNullOrWhiteSpace(database) ? defaultDatabase : database;
        user = string.IsNullOrWhiteSpace(user) ? defaultUser : user;
        password = string.IsNullOrWhiteSpace(password) ? defaultPassword : password;

        if (string.IsNullOrWhiteSpace(server)
            || string.IsNullOrWhiteSpace(database)
            || string.IsNullOrWhiteSpace(user)
            || string.IsNullOrWhiteSpace(password))
        {
            return string.Empty;
        }

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = database,
            UserID = user,
            Password = password,
            TrustServerCertificate = true,
            Encrypt = false,
            ConnectTimeout = defaultTimeout,
        };

        return builder.ConnectionString;
    }
}
