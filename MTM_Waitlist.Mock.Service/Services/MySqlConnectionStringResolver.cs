using MTM_Waitlist.Mock.Service.Models;

namespace MTM_Waitlist.Mock.Service.Services;

/// <summary>
/// Resolves a MySQL connection string for one database without persisting any credential.
/// </summary>
/// <remarks>
/// <para>
/// Resolution order (first match wins):
/// </para>
/// <list type="number">
///   <item><description>the store's dedicated environment variable, when set to a complete connection string;</description></item>
///   <item><description>the shared fallback environment variable — the cache and the four stores normally
///     share one MySQL host, so the unset case reuses it with the database name overridden;</description></item>
///   <item><description>the configured <see cref="MySqlConnectionSettings"/> host/port/login, with the
///     password taken from <c>MTM_MYSQL_PASSWORD</c> when present.</description></item>
/// </list>
/// <para>
/// This mirrors the application-side resolution in <c>MySqlHelperServer</c> — the same environment
/// variable names — so the app and the service cannot disagree about which host a store lives on
/// (FR-009/FR-012). The resolved value is never logged (FR-026).
/// </para>
/// </remarks>
public sealed class MySqlConnectionStringResolver
{
    /// <summary>Environment variable naming a complete connection string for the <c>mtm_mock</c> cache.</summary>
    public const string MockConnectionStringEnvironmentVariable = "MTM_MOCK_DB_CONNECTION_STRING";

    /// <summary>Environment variable naming a complete connection string for the application's own store.</summary>
    public const string WaitlistConnectionStringEnvironmentVariable = "MTM_WAITLIST_DB_CONNECTION_STRING";

    /// <summary>Environment variable naming a complete connection string for the floor/WIP store.</summary>
    public const string WipApplicationConnectionStringEnvironmentVariable = "MTM_WIP_APPLICATION_DB_CONNECTION_STRING";

    /// <summary>Environment variable naming a complete connection string for the receiving store.</summary>
    public const string ReceivingApplicationConnectionStringEnvironmentVariable = "MTM_RECEIVING_APPLICATION_DB_CONNECTION_STRING";

    /// <summary>Environment variable holding the MySQL password when no complete connection string is set.</summary>
    public const string PasswordEnvironmentVariable = "MTM_MYSQL_PASSWORD";

    /// <summary>
    /// The message used when an operation needs the cache and no connection has been configured.
    /// </summary>
    /// <remarks>
    /// This is a <b>reported condition</b>, not a startup failure: the service must come up with its tray and
    /// settings surface so an operator can configure the host, and a missing connection is then surfaced by the
    /// operation that needs it (FR-012).
    /// </remarks>
    public const string NotConfiguredMessage =
        "No mtm_mock connection is configured. Set MTM_MOCK_DB_CONNECTION_STRING or " +
        "MTM_WAITLIST_DB_CONNECTION_STRING, or configure the MySQL host in the service settings.";

    private readonly MySqlConnectionSettings _settings;

    /// <summary>Creates the resolver.</summary>
    /// <param name="settings">The configured, non-secret host/login details.</param>
    public MySqlConnectionStringResolver(MySqlConnectionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings;
    }

    /// <summary>
    /// Resolves a connection string for one database.
    /// </summary>
    /// <param name="databaseName">The database the connection must default to.</param>
    /// <param name="dedicatedEnvironmentVariable">
    /// The store's own environment variable, or <see langword="null"/> to go straight to the shared path.
    /// </param>
    /// <returns>The connection string, or <see langword="null"/> when nothing is configured.</returns>
    public string? Resolve(string databaseName, string? dedicatedEnvironmentVariable = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        var candidate = FirstNonEmpty(
            dedicatedEnvironmentVariable is null ? null : Environment.GetEnvironmentVariable(dedicatedEnvironmentVariable),
            Environment.GetEnvironmentVariable(WaitlistConnectionStringEnvironmentVariable));

        if (!string.IsNullOrWhiteSpace(candidate))
        {
            return WithDatabase(candidate, databaseName);
        }

        return BuildFromSettings(databaseName);
    }

    /// <summary>
    /// Resolves the <c>mtm_mock</c> cache connection used by the shape metadata reader and the mirror writer.
    /// </summary>
    public string? ResolveMockCache() => Resolve("mtm_mock", MockConnectionStringEnvironmentVariable);

    private string? BuildFromSettings(string databaseName)
    {
        // A host with no login is NOT a configuration. Returning a string anyway meant every read ran as
        // an anonymous local connection and failed with
        // "Access denied for user ''@'localhost' (using password: NO)" — which tells an operator nothing
        // about what to fix. Returning null makes the callers report NotConfiguredMessage instead.
        if (string.IsNullOrWhiteSpace(_settings.Server) || string.IsNullOrWhiteSpace(_settings.UserId))
        {
            return null;
        }

        var builder = new MySqlConnector.MySqlConnectionStringBuilder
        {
            Server = _settings.Server,
            Port = (uint)_settings.Port,
            Database = databaseName
        };

        if (!string.IsNullOrWhiteSpace(_settings.UserId))
        {
            builder.UserID = _settings.UserId;
        }

        var password = Environment.GetEnvironmentVariable(PasswordEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(password))
        {
            builder.Password = password;
        }

        return builder.ConnectionString;
    }

    private static string WithDatabase(string connectionString, string databaseName)
    {
        try
        {
            return new MySqlConnector.MySqlConnectionStringBuilder(connectionString)
            {
                Database = databaseName
            }.ConnectionString;
        }
        catch (ArgumentException)
        {
            // A malformed configured value must not be silently used with the wrong database.
            throw new InvalidOperationException(
                $"The configured MySQL connection string could not be parsed for database '{databaseName}'.");
        }
    }

    private static string? FirstNonEmpty(string? first, string? second) =>
        string.IsNullOrWhiteSpace(first) ? second : first;
}
