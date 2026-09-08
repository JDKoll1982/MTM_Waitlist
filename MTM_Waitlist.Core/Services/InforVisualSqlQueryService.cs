using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MTM_Waitlist.Module_Core.Helpers;
using System.Runtime.InteropServices;
using System.Text;

namespace MTM_Waitlist.Module_Core.Services;

/// <summary>
/// Shared executor for Infor Visual SQL Server queue scripts. This is the Core-owned, reusable
/// executor for module queue scripts (mirrors the Module_Setup copy) so any module (Waitlist,
/// and later Setup) can run checked-in scripts from <c>Database/InforVisual/Queues</c> against the
/// on-premises Infor Visual database. Connection details resolve from environment variables first,
/// then the <c>InforVisualDatabaseOptions</c> configuration section.
///
/// This instance resolves scripts under the <c>Module_Waitlist</c> queue folder; it never throws —
/// any missing script, missing connection, or SQL error yields an empty result so callers can fall
/// back to an empty grid without crashing.
/// </summary>
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
        StartupDebugLog.Info("WaitlistInventory.Sql", $"ExecuteQueueAsync started. Script='{scriptName}', ParamCount={parameters.Count}.");
        var script = await WaitlistInforVisualSqlScriptStore.LoadAsync(scriptName, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(script))
        {
            StartupDebugLog.Info("WaitlistInventory.Sql", $"Script load returned empty content for '{scriptName}'.");
            return Array.Empty<Dictionary<string, object?>>();
        }

        var connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            StartupDebugLog.Info("WaitlistInventory.Sql", $"Connection string resolved empty for script '{scriptName}'.");
            return Array.Empty<Dictionary<string, object?>>();
        }

        var parameterSummary = string.Join(", ", parameters.Select(entry => $"{entry.Key}='{Convert.ToString(entry.Value) ?? string.Empty}'"));
        StartupDebugLog.Info("WaitlistInventory.Sql", $"Executing script '{scriptName}' with parameters: {parameterSummary}.");

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

            StartupDebugLog.Info("WaitlistInventory.Sql", $"Script '{scriptName}' executed successfully. RowCount={rows.Count}.");

            return rows;
        }
        catch (SqlException sqlException)
        {
            StartupDebugLog.Error(
                "WaitlistInventory.Sql",
                sqlException,
                $"SQL error executing '{scriptName}'. Number={sqlException.Number}, State={sqlException.State}, Line={sqlException.LineNumber}.");
            return Array.Empty<Dictionary<string, object?>>();
        }
        catch (COMException comException)
        {
            StartupDebugLog.Error(
                "WaitlistInventory.Sql",
                comException,
                $"COM error executing '{scriptName}'. HResult=0x{comException.HResult:X8}.");
            return Array.Empty<Dictionary<string, object?>>();
        }
        catch (Exception exception)
        {
            StartupDebugLog.Error("WaitlistInventory.Sql", exception, $"Unhandled error executing '{scriptName}'.");
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

/// <summary>
/// Resolves checked-in Infor Visual queue scripts for the Waitlist module from the app output,
/// mirroring Module_Setup's <c>SetupSqlScriptStore</c> path layout.
/// </summary>
internal static class WaitlistInforVisualSqlScriptStore
{
    private const string ScriptFolderName = "Database";
    private const string QueryFolderName = "InforVisual";
    private const string ModuleFolderName = "Queues";
    private const string ScriptModuleFolderName = "Module_Waitlist";
    private const string ScriptQueryFolderName = "Queries";

    public static async Task<string> LoadAsync(string scriptName, CancellationToken cancellationToken = default)
    {
        var scriptPath = GetScriptPath(scriptName);
        if (!File.Exists(scriptPath))
        {
            return string.Empty;
        }

        await using var stream = File.OpenRead(scriptPath);
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        cancellationToken.ThrowIfCancellationRequested();
        return await reader.ReadToEndAsync().ConfigureAwait(false);
    }

    private static string GetScriptPath(string scriptName)
    {
        var normalizedName = scriptName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)
            ? scriptName
            : $"{scriptName}.sql";

        return Path.Combine(
            AppContext.BaseDirectory,
            ScriptFolderName,
            QueryFolderName,
            ModuleFolderName,
            ScriptModuleFolderName,
            ScriptQueryFolderName,
            normalizedName);
    }
}
