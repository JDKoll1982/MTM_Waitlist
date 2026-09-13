using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MTM_Waitlist.Mock.Contracts;

namespace MTM_Waitlist.Mock.Services;

/// <summary>
/// Resolves the Infor Visual connection string from the environment, then configuration.
/// </summary>
/// <remarks>
/// Consolidates the resolution that was duplicated in the retired Module_Setup and Module_Core
/// executors. A complete connection string in <c>INFOR_VISUAL_SQL_CONNECTION_STRING</c> wins; otherwise
/// the individual server/database/user/password values are taken from environment variables and
/// fall back to the <c>InforVisualDatabaseOptions</c> configuration section. The resolved value is
/// never logged.
/// </remarks>
public sealed class VisualConnectionStringProvider : IVisualConnectionStringProvider
{
    private const string ConnectionStringEnvironmentVariable = "INFOR_VISUAL_SQL_CONNECTION_STRING";
    private const string ServerEnvironmentVariable = "INFOR_VISUAL_SQL_SERVER";
    private const string DatabaseEnvironmentVariable = "INFOR_VISUAL_SQL_DATABASE";
    private const string UserEnvironmentVariable = "INFOR_VISUAL_SQL_USER";
    private const string PasswordEnvironmentVariable = "INFOR_VISUAL_SQL_PASSWORD";

    private const int DefaultConnectionTimeoutSeconds = 10;

    /// <summary>
    /// Connect attempts a resolved connection string is allowed to carry.
    /// </summary>
    /// <remarks>
    /// SqlClient's default is <b>one</b> blind retry ten seconds after the first failure
    /// (<c>Connect Retry Count=1</c> / <c>Connect Retry Interval=10</c>). Paired with the configured
    /// ten-second connect timeout that turned an absent Infor Visual host into an eleven-second stall on a
    /// probe that runs on a timer, which is what made the shell appear to lock up. Reads and probes alike
    /// always go through this provider, so the retry is disabled once, here, for both.
    /// </remarks>
    private const int ConnectRetryCount = 0;

    private readonly IConfiguration? _configuration;

    /// <summary>Creates the provider.</summary>
    /// <param name="configuration">Optional configuration supplying the fallback values.</param>
    public VisualConnectionStringProvider(IConfiguration? configuration = null)
    {
        _configuration = configuration;
    }

    /// <inheritdoc />
    public string Resolve()
    {
        var completeConnectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable)?.Trim();
        if (!string.IsNullOrWhiteSpace(completeConnectionString))
        {
            // A complete override still gets the connection policy applied: an operator who supplies the whole
            // string must not silently reinstate the blind retry the application depends on being absent.
            return Normalize(completeConnectionString);
        }

        var server = FirstNonEmpty(
            Environment.GetEnvironmentVariable(ServerEnvironmentVariable),
            _configuration?["InforVisualDatabaseOptions:Server"]);
        var database = FirstNonEmpty(
            Environment.GetEnvironmentVariable(DatabaseEnvironmentVariable),
            _configuration?["InforVisualDatabaseOptions:Database"]);
        var user = FirstNonEmpty(
            Environment.GetEnvironmentVariable(UserEnvironmentVariable),
            _configuration?["InforVisualDatabaseOptions:User"]);
        var password = FirstNonEmpty(
            Environment.GetEnvironmentVariable(PasswordEnvironmentVariable),
            _configuration?["InforVisualDatabaseOptions:Password"]);

        if (string.IsNullOrWhiteSpace(server)
            || string.IsNullOrWhiteSpace(database)
            || string.IsNullOrWhiteSpace(user)
            || string.IsNullOrWhiteSpace(password))
        {
            return string.Empty;
        }

        var timeoutText = _configuration?["InforVisualDatabaseOptions:ConnectionTimeoutSeconds"];
        var timeout = int.TryParse(timeoutText, out var parsedTimeout)
            ? parsedTimeout
            : DefaultConnectionTimeoutSeconds;

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = database,
            UserID = user,
            Password = password,
            TrustServerCertificate = true,
            Encrypt = false,
            ConnectTimeout = timeout,
        };

        return Normalize(builder.ConnectionString);
    }

    /// <summary>
    /// Applies this provider's connection policy to a resolved connection string.
    /// </summary>
    /// <param name="connectionString">The resolved connection string.</param>
    /// <returns>The connection string the application actually opens.</returns>
    /// <remarks>
    /// An override the provider cannot parse is returned unchanged rather than throwing: the failure then
    /// surfaces from the connection attempt, where it is classified as unreachable or failed, instead of from
    /// configuration resolution. The value is never logged.
    /// </remarks>
    private static string Normalize(string connectionString)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                ConnectRetryCount = ConnectRetryCount,
            };

            return builder.ConnectionString;
        }
        catch (ArgumentException)
        {
            return connectionString;
        }
    }

    private static string? FirstNonEmpty(string? environmentValue, string? configuredValue)
    {
        var trimmedEnvironmentValue = environmentValue?.Trim();
        return string.IsNullOrWhiteSpace(trimmedEnvironmentValue) ? configuredValue : trimmedEnvironmentValue;
    }
}
