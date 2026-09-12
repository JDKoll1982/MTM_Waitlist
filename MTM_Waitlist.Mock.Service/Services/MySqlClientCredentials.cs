using System.Text;

using MySqlConnector;

namespace MTM_Waitlist.Mock.Service.Services;

/// <summary>
/// The credentials file a MySQL command-line client is handed, as a <c>--defaults-extra-file</c> argument.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why a file at all.</b> The password must never appear on a command line, where a process list, a log, or a
/// captured command line can read it (FR-026, research.md R7). An option file referenced with
/// <c>--defaults-extra-file</c> is the only mechanism <c>mysqldump</c> offers that satisfies that, which is why
/// <see cref="Models.MySqlConnectionSettings.PasswordFilePath"/> exists.
/// </para>
/// <para>
/// <b>What this type adds.</b> The operator-provisioned file is used when it is configured, and the service writes
/// nothing. When it is <i>not</i> configured but the resolved connection still carries a password — the normal case
/// on a host whose credentials arrive through <c>MTM_WAITLIST_DB_CONNECTION_STRING</c>, the same variable every other
/// service path already resolves through — a single-use file is written, handed to the one invocation, and deleted
/// again. Without that fallback the backup path alone could not authenticate on such a host.
/// </para>
/// <para>
/// <b>What it deliberately does not carry.</b> Only the password. The host, port, and login travel as ordinary
/// (non-secret) command-line arguments, so a file that outlives its invocation for any reason still discloses
/// nothing about which server or account it belongs to.
/// </para>
/// </remarks>
internal sealed class MySqlClientCredentials : IDisposable
{
    private readonly string? _temporaryPath;

    private MySqlClientCredentials(string? argument, string? temporaryPath)
    {
        Argument = argument;
        _temporaryPath = temporaryPath;
    }

    /// <summary>
    /// The <c>--defaults-extra-file=…</c> argument, or <see langword="null"/> when this invocation needs no
    /// credentials file.
    /// </summary>
    public string? Argument { get; }

    /// <summary>
    /// Builds the credentials argument for one invocation.
    /// </summary>
    /// <param name="configuredPasswordFilePath">The operator's option file, when one is configured.</param>
    /// <param name="connectionString">The resolved connection for the database being touched.</param>
    /// <param name="directory">
    /// Where a generated file may be written. It must be a per-user, service-owned location — the caller passes the
    /// service's own app-data root for that reason.
    /// </param>
    /// <returns>The credentials argument, which is <see cref="Dispose"/>d after the invocation.</returns>
    public static MySqlClientCredentials Create(
        string? configuredPasswordFilePath,
        string connectionString,
        string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        // The operator's own file wins: it is the deployment-sanctioned place for the password and the service
        // writes nothing to disk.
        if (!string.IsNullOrWhiteSpace(configuredPasswordFilePath))
        {
            return new MySqlClientCredentials(
                $"--defaults-extra-file={configuredPasswordFilePath.Trim()}",
                temporaryPath: null);
        }

        var password = new MySqlConnectionStringBuilder(connectionString).Password;

        // No password to carry means no file: mysqldump then connects exactly as it would with no --password,
        // which is what a passwordless login expects.
        if (string.IsNullOrEmpty(password))
        {
            return new MySqlClientCredentials(argument: null, temporaryPath: null);
        }

        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, $"mysql-client-{Guid.NewGuid():N}.cnf");
        File.WriteAllText(path, BuildOptionFile(password), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        return new MySqlClientCredentials($"--defaults-extra-file={path}", path);
    }

    /// <summary>Deletes a generated file. An operator-provisioned file is never touched.</summary>
    public void Dispose()
    {
        if (_temporaryPath is null)
        {
            return;
        }

        try
        {
            File.Delete(_temporaryPath);
        }
        catch (IOException)
        {
            // A file that cannot be deleted is not worth failing a completed backup over; it carries only a
            // password the account it belongs to already holds, in a per-user folder.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    /// <summary>An option file with one <c>[client]</c> group holding one value.</summary>
    private static string BuildOptionFile(string password) =>
        $"[client]{Environment.NewLine}password={QuoteOptionValue(password)}{Environment.NewLine}";

    /// <summary>
    /// Quotes an option-file value. MySQL's option files treat a backslash as an escape character, so backslashes
    /// and double quotes are escaped rather than passed through.
    /// </summary>
    private static string QuoteOptionValue(string value) =>
        "\"" + value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
}
