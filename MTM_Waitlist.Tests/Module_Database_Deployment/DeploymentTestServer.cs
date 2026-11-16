using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Database_Deployment.Models;
using MTM_Database_Deployment.Services;

using MySqlConnector;

namespace MTM_Waitlist.Tests.Module_Database_Deployment;

/// <summary>
/// The one place the deployment suite reaches a real server: it reads the connection from an environment variable
/// and refuses anything that is not this machine.
/// </summary>
/// <remarks>
/// The cases that use this drop and recreate a database, so they are gated twice. The variable is not set by
/// default, and a connection string naming another host is refused rather than honoured: the plant host runs with
/// binary logging off, so a run aimed at it could not be undone.
/// </remarks>
internal static class DeploymentTestServer
{
    internal const string ConnectionVariable = "MTM_DEPLOYMENT_TEST_CONNECTION_STRING";

    /// <summary>The scratch database the suite creates, drops and reads back.</summary>
    internal const string ScratchDatabase = "mtm_deployment_check";

    /// <summary>The database a duplicate of the scratch database is written into.</summary>
    internal const string ScratchCopyDatabase = "mtm_deployment_check_copy";

    internal static Model_ConnectionSettings Resolve()
    {
        var raw = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(raw))
        {
            Assert.Inconclusive(
                $"{ConnectionVariable} is not set, so there is no practice server to deploy into. Set it to a "
                + "localhost connection string and run these cases again.");
            throw new InvalidOperationException("unreachable");
        }

        var builder = new MySqlConnectionStringBuilder(raw);
        if (!IsThisMachine(builder.Server))
        {
            Assert.Inconclusive(
                $"'{builder.Server}' is not this machine. These cases drop and recreate a database, so they only run "
                + "against the local server.");
            throw new InvalidOperationException("unreachable");
        }

        return new Model_ConnectionSettings
        {
            PreferredServer = builder.Server,
            FallbackServer = builder.Server,
            Port = (int)builder.Port,
            User = builder.UserID,
            Password = builder.Password,
            Database = ScratchDatabase,
            ConnectTimeoutSeconds = 15,
            AvailableDatabases = [ScratchDatabase]
        };
    }

    internal static async Task ExecuteAsync(Service_MySqlGateway gateway, string? database, string statement)
    {
        await using var connection = await gateway.OpenAsync(database, CancellationToken.None).ConfigureAwait(false);
        await using var command = new MySqlCommand(statement, connection);
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    internal static async Task<long> CountRowsAsync(Service_MySqlGateway gateway, string database, string table)
    {
        var rows = await gateway
            .QueryAsync(database, $"SELECT COUNT(*) FROM `{table}`;", CancellationToken.None)
            .ConfigureAwait(false);

        return rows.Count > 0 && long.TryParse(rows[0][0], out var count) ? count : -1;
    }

    internal static async Task<long> CountTablesAsync(Service_MySqlGateway gateway, string database, string table)
    {
        var rows = await gateway
            .QueryAsync(
                "information_schema",
                $"SELECT COUNT(*) FROM TABLES WHERE TABLE_SCHEMA = '{database}' AND TABLE_NAME = '{table}';",
                CancellationToken.None)
            .ConfigureAwait(false);

        return rows.Count > 0 && long.TryParse(rows[0][0], out var count) ? count : -1;
    }

    internal static async Task<string> ReadColumnCommentAsync(
        Service_MySqlGateway gateway,
        string database,
        string table,
        string column)
    {
        var rows = await gateway
            .QueryAsync(
                "information_schema",
                "SELECT IFNULL(COLUMN_COMMENT,'') FROM COLUMNS "
                + $"WHERE TABLE_SCHEMA = '{database}' AND TABLE_NAME = '{table}' AND COLUMN_NAME = '{column}';",
                CancellationToken.None)
            .ConfigureAwait(false);

        return rows.Count > 0 ? rows[0][0] : string.Empty;
    }

    /// <summary>Drops the scratch database, the copy a deployment keeps of it, and the duplicate the suite makes.</summary>
    internal static async Task DropAsync(Service_MySqlGateway gateway)
    {
        await using var connection = await gateway.OpenAsync(null, CancellationToken.None).ConfigureAwait(false);

        string[] names =
        [
            ScratchDatabase,
            DatabaseBackup.BackupName(ScratchDatabase),
            ScratchCopyDatabase
        ];

        foreach (var name in names)
        {
            await using var command = new MySqlCommand($"DROP DATABASE IF EXISTS `{name}`;", connection);
            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
    }

    private static bool IsThisMachine(string server) =>
        string.Equals(server, "localhost", StringComparison.OrdinalIgnoreCase)
        || string.Equals(server, ".", StringComparison.Ordinal)
        || server == "127.0.0.1"
        || server == "::1";
}
