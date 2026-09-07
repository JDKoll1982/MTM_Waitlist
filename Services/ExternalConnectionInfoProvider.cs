using Microsoft.Extensions.Configuration;
using MySqlConnector;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Services;

/// <summary>
/// Resolves the external (non-<c>mtm_waitlist</c>) connection strings for the connection-health
/// service, mirroring the existing resolution used by <c>InforVisualSqlQueryService</c> (Infor SQL
/// Server) and <c>MySqlHelperServer</c> (receiving MySQL). Only used for reachability probes.
/// </summary>
public sealed class ExternalConnectionInfoProvider : IExternalConnectionInfoProvider
{
    private const string InforVisualServerEnv = "INFOR_VISUAL_SQL_SERVER";
    private const string InforVisualDatabaseEnv = "INFOR_VISUAL_SQL_DATABASE";
    private const string InforVisualUserEnv = "INFOR_VISUAL_SQL_USER";
    private const string InforVisualPasswordEnv = "INFOR_VISUAL_SQL_PASSWORD";
    private const string ReceivingConnectionStringEnv = "MTM_RECEIVING_APPLICATION_DB_CONNECTION_STRING";
    private const string WaitlistConnectionStringEnv = "MTM_WAITLIST_DB_CONNECTION_STRING";
    private const string WaitlistStartupConnectionStringEnv = "MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING";

    private readonly IConfiguration _configuration;

    public ExternalConnectionInfoProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool IsSqlServer(ConnectionSource source) => source == ConnectionSource.InforVisual;

    public string? ResolveConnectionString(ConnectionSource source) => source switch
    {
        ConnectionSource.InforVisual => ResolveInforVisualConnectionString(),
        ConnectionSource.Receiving => ResolveReceivingConnectionString(),
        _ => null,
    };

    private string? ResolveInforVisualConnectionString()
    {
        var server = Environment.GetEnvironmentVariable(InforVisualServerEnv)?.Trim()
            ?? _configuration["InforVisualDatabaseOptions:Server"];
        var database = Environment.GetEnvironmentVariable(InforVisualDatabaseEnv)?.Trim()
            ?? _configuration["InforVisualDatabaseOptions:Database"];
        var user = Environment.GetEnvironmentVariable(InforVisualUserEnv)?.Trim()
            ?? _configuration["InforVisualDatabaseOptions:User"];
        var password = Environment.GetEnvironmentVariable(InforVisualPasswordEnv)?.Trim()
            ?? _configuration["InforVisualDatabaseOptions:Password"];

        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(database))
        {
            return null;
        }

        return new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = database,
            UserID = user,
            Password = password,
            TrustServerCertificate = true,
        }.ConnectionString;
    }

    private string? ResolveReceivingConnectionString()
    {
        var environment = Environment.GetEnvironmentVariable(ReceivingConnectionStringEnv)?.Trim();
        var fallback = Environment.GetEnvironmentVariable(WaitlistConnectionStringEnv)?.Trim()
            ?? Environment.GetEnvironmentVariable(WaitlistStartupConnectionStringEnv)?.Trim()
            ?? _configuration["StartupDatabaseOptions:ConnectionString"];
        var resolved = string.IsNullOrWhiteSpace(environment) ? fallback : environment;
        if (string.IsNullOrWhiteSpace(resolved))
        {
            return null;
        }

        return new MySqlConnectionStringBuilder(resolved)
        {
            Database = "mtm_receiving_application",
        }.ConnectionString;
    }
}
