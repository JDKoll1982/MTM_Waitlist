using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MTM_Waitlist.Module_Core.Helpers;
using System.Runtime.InteropServices;
using System.Text;

namespace MTM_Waitlist.Module_Core.Services;

/// <summary>
/// Which module's checked-in queue scripts an <see cref="InforVisualSqlQueryService"/> resolves.
/// </summary>
public enum InforVisualSqlModule
{
    /// <summary><c>Database/InforVisual/Queues/Module_Waitlist/Queries</c>.</summary>
    Waitlist,

    /// <summary><c>Database/InforVisual/Queues/Module_Setup/Queries</c>.</summary>
    Setup,
}

/// <summary>
/// Shared executor for Infor Visual SQL Server queue scripts. This is the one owner of the Infor Visual read
/// path: any module (Waitlist, and the setup tool) runs its checked-in scripts from
/// <c>Database/InforVisual/Queues/&lt;module&gt;/Queries</c> against the on-premises Infor Visual database.
/// Connection details resolve from environment variables first, then the <c>InforVisualDatabaseOptions</c>
/// configuration section.
///
/// The module chooses the script folder and nothing else, so one type serves both. It never throws — any
/// missing script, missing connection, or SQL error yields an empty result so callers can fall back without
/// crashing.
/// </summary>
public sealed class InforVisualSqlQueryService
{
    private const string InforVisualConnectionStringEnvironmentVariable = "INFOR_VISUAL_SQL_CONNECTION_STRING";
    private const string InforVisualServerEnvironmentVariable = "INFOR_VISUAL_SQL_SERVER";
    private const string InforVisualDatabaseEnvironmentVariable = "INFOR_VISUAL_SQL_DATABASE";
    private const string InforVisualUserEnvironmentVariable = "INFOR_VISUAL_SQL_USER";
    private const string InforVisualPasswordEnvironmentVariable = "INFOR_VISUAL_SQL_PASSWORD";

    private readonly IConfiguration _configuration;
    private readonly InforVisualSqlModule _module;
    private readonly string _logModule;

    public InforVisualSqlQueryService(IConfiguration configuration)
        : this(configuration, InforVisualSqlModule.Waitlist)
    {
    }

    public InforVisualSqlQueryService(IConfiguration configuration, InforVisualSqlModule module)
    {
        _configuration = configuration;
        _module = module;
        _logModule = module == InforVisualSqlModule.Setup ? "SetupLookup.Sql" : "WaitlistInventory.Sql";
    }

    public async Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteQueueAsync(
        string scriptName,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        AppLog.Info(_logModule, $"ExecuteQueueAsync started. Script='{scriptName}', ParamCount={parameters.Count}.");
        var script = await InforVisualSqlScriptStore.LoadAsync(_module, scriptName, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(script))
        {
            AppLog.Info(_logModule, $"Script load returned empty content for '{scriptName}'.");
            return Array.Empty<Dictionary<string, object?>>();
        }

        var connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            AppLog.Info(_logModule, $"Connection string resolved empty for script '{scriptName}'.");
            return Array.Empty<Dictionary<string, object?>>();
        }

        var parameterSummary = string.Join(", ", parameters.Select(entry => $"{entry.Key}='{Convert.ToString(entry.Value) ?? string.Empty}'"));
        AppLog.Info(_logModule, $"Executing script '{scriptName}' with parameters: {parameterSummary}.");

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

            AppLog.Info(_logModule, $"Script '{scriptName}' executed successfully. RowCount={rows.Count}.");

            return rows;
        }
        catch (SqlException sqlException)
        {
            AppLog.Error(
                _logModule,
                sqlException,
                $"SQL error executing '{scriptName}'. Number={sqlException.Number}, State={sqlException.State}, Line={sqlException.LineNumber}.");
            return Array.Empty<Dictionary<string, object?>>();
        }
        catch (COMException comException)
        {
            AppLog.Error(
                _logModule,
                comException,
                $"COM error executing '{scriptName}'. HResult=0x{comException.HResult:X8}.");
            return Array.Empty<Dictionary<string, object?>>();
        }
        catch (Exception exception)
        {
            AppLog.Error(_logModule, exception, $"Unhandled error executing '{scriptName}'.");
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
/// Resolves a module's checked-in Infor Visual queue scripts from the app output.
/// </summary>
internal static class InforVisualSqlScriptStore
{
    private const string ScriptFolderName = "Database";
    private const string QueryFolderName = "InforVisual";
    private const string ModuleFolderName = "Queues";
    private const string ScriptQueryFolderName = "Queries";

    public static Task<string> LoadAsync(
        InforVisualSqlModule module,
        string scriptName,
        CancellationToken cancellationToken = default)
    {
        var scriptModuleFolderName = module == InforVisualSqlModule.Setup ? "Module_Setup" : "Module_Waitlist";
        return LoadAsync(scriptModuleFolderName, scriptName, cancellationToken);
    }

    private static async Task<string> LoadAsync(
        string scriptModuleFolderName,
        string scriptName,
        CancellationToken cancellationToken)
    {
        var scriptPath = GetScriptPath(scriptModuleFolderName, scriptName);
        if (!File.Exists(scriptPath))
        {
            return string.Empty;
        }

        await using var stream = File.OpenRead(scriptPath);
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        cancellationToken.ThrowIfCancellationRequested();
        return await reader.ReadToEndAsync().ConfigureAwait(false);
    }

    private static string GetScriptPath(string scriptModuleFolderName, string scriptName)
    {
        var normalizedName = scriptName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)
            ? scriptName
            : $"{scriptName}.sql";

        return Path.Combine(
            AppContext.BaseDirectory,
            ScriptFolderName,
            QueryFolderName,
            ModuleFolderName,
            scriptModuleFolderName,
            ScriptQueryFolderName,
            normalizedName);
    }
}
