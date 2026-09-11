namespace MTM_Waitlist.Mock.Service.Models;

/// <summary>
/// The MySQL host details the service needs for the cache and for per-store backups (FR-009/FR-012).
/// </summary>
/// <remarks>
/// <para>
/// <b>No password is stored here.</b> The password reaches MySqlConnector through the connection
/// string resolved from the environment — the same path the application already uses — and reaches
/// <c>mysqldump</c> only through an option file, never a command line or a settings file (FR-026,
/// research.md R7).
/// </para>
/// <para>
/// This section exists so an operator can point the service at a different host/port/login without
/// editing environment variables, and so the backup engine knows where the database host is.
/// </para>
/// </remarks>
public sealed record MySqlConnectionSettings
{
    /// <summary>MySQL host or instance name.</summary>
    public string Server { get; init; } = "localhost";

    /// <summary>MySQL TCP port.</summary>
    public int Port { get; init; } = 3306;

    /// <summary>Login name used for reads and for <c>mysqldump</c>.</summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Absolute path of the MySQL option file holding the password, passed to <c>mysqldump</c> with
    /// <c>--defaults-extra-file</c>. <see langword="null"/> means "no password is needed".
    /// </summary>
    /// <remarks>
    /// The file is read by the MySQL client itself; the service never reads its contents, so the
    /// password never enters this process's configuration, logs, or the API payload.
    /// </remarks>
    public string? PasswordFilePath { get; init; }
}
