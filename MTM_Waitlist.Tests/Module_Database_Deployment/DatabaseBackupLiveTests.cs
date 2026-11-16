using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Database_Deployment.Services;

namespace MTM_Waitlist.Tests.Module_Database_Deployment;

/// <summary>
/// Proves, against a real server, that a deployment copies the chosen database into
/// <c>{database}_backup</c> before its first statement runs.
/// </summary>
/// <remarks>
/// These are here rather than in a scratch harness because the copy is the only undo a deployment has. A copy
/// proven once by hand is a claim; a copy proven on every run is a guarantee.
/// </remarks>
[TestClass]
public sealed class DatabaseBackupLiveTests
{
    private const string SourceDatabase = DeploymentTestServer.ScratchDatabase;

    [TestMethod]
    public async Task Service_DatabaseBackupLiveTests_CreateAsync_CopiesEveryTableAndItsRowsIntoTheBackupDatabase()
    {
        var gateway = new Service_MySqlGateway(DeploymentTestServer.Resolve());
        var backupName = DatabaseBackup.BackupName(SourceDatabase);

        try
        {
            await DeploymentTestServer.DropAsync(gateway).ConfigureAwait(false);
            await DeploymentTestServer
                .ExecuteAsync(gateway, null, $"CREATE DATABASE `{SourceDatabase}`")
                .ConfigureAwait(false);
            await DeploymentTestServer
                .ExecuteAsync(gateway, SourceDatabase, "CREATE TABLE parent (id INT PRIMARY KEY, name VARCHAR(40)) ENGINE=InnoDB")
                .ConfigureAwait(false);
            await DeploymentTestServer
                .ExecuteAsync(gateway, SourceDatabase, "CREATE TABLE child (id INT PRIMARY KEY, parent_id INT) ENGINE=InnoDB")
                .ConfigureAwait(false);
            await DeploymentTestServer
                .ExecuteAsync(gateway, SourceDatabase, "INSERT INTO parent VALUES (1, 'a'), (2, 'b')")
                .ConfigureAwait(false);
            await DeploymentTestServer
                .ExecuteAsync(gateway, SourceDatabase, "INSERT INTO child VALUES (10, 1), (11, 2), (12, 1)")
                .ConfigureAwait(false);

            var outcome = await new DatabaseBackup(gateway)
                .CreateAsync(SourceDatabase, null, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsTrue(outcome.WasCreated, outcome.Summary);
            Assert.AreEqual(2, outcome.TableCount, "a table of the database was left out of the copy");
            Assert.AreEqual(5L, outcome.RowCount, "the copy does not hold every row of the database");
            Assert.AreEqual(
                3L,
                await DeploymentTestServer.CountRowsAsync(gateway, backupName, "child").ConfigureAwait(false),
                "the copied table does not hold the rows the source holds");
        }
        finally
        {
            await DeploymentTestServer.DropAsync(gateway).ConfigureAwait(false);
        }
    }

    [TestMethod]
    public async Task Service_DatabaseBackupLiveTests_RebuildAsync_KeepsWhatItErasesInTheBackupDatabase()
    {
        var gateway = new Service_MySqlGateway(DeploymentTestServer.Resolve());
        var engine = new Service_DeploymentEngine(
            gateway,
            new Service_ObjectCatalog(new Service_DeploymentSettings().SqlRoot),
            new DatabaseBackup(gateway));
        var backupName = DatabaseBackup.BackupName(SourceDatabase);

        try
        {
            await DeploymentTestServer.DropAsync(gateway).ConfigureAwait(false);

            // The first run brings the scratch database into being, so the second one has something to erase.
            var first = await engine.RebuildAsync(SourceDatabase, null, CancellationToken.None).ConfigureAwait(false);
            Assert.IsTrue(first.IsSuccess, first.ToReportText());

            await DeploymentTestServer
                .ExecuteAsync(
                    gateway,
                    SourceDatabase,
                    "CREATE TABLE zz_backup_probe (id INT PRIMARY KEY, note VARCHAR(20)) ENGINE=InnoDB")
                .ConfigureAwait(false);
            await DeploymentTestServer
                .ExecuteAsync(gateway, SourceDatabase, "INSERT INTO zz_backup_probe VALUES (1, 'kept'), (2, 'kept')")
                .ConfigureAwait(false);

            var second = await engine.RebuildAsync(SourceDatabase, null, CancellationToken.None).ConfigureAwait(false);

            Assert.IsTrue(
                second.Messages.Any(message => message.Contains(backupName, StringComparison.Ordinal)),
                "the run does not report the copy it took before erasing the database");
            Assert.AreEqual(
                0L,
                await DeploymentTestServer.CountTablesAsync(gateway, SourceDatabase, "zz_backup_probe").ConfigureAwait(false),
                "the rebuild left a table behind that only existed in the old database");
            Assert.AreEqual(
                2L,
                await DeploymentTestServer.CountRowsAsync(gateway, backupName, "zz_backup_probe").ConfigureAwait(false),
                "the copy does not hold the rows the rebuild erased");
        }
        finally
        {
            await DeploymentTestServer.DropAsync(gateway).ConfigureAwait(false);
        }
    }
}
