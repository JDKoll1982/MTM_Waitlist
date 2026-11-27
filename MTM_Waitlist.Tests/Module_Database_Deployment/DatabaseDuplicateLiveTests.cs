using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Database_Deployment.Services;

namespace MTM_Waitlist.Tests.Module_Database_Deployment;

/// <summary>
/// A duplicate is only a duplicate if it carries what makes the source what it is: the keys and comments on the
/// tables, every row, the views, the saved instructions and the triggers.
/// </summary>
/// <remarks>
/// The trigger is the case worth having. It lives on a table, so a copy that recreated tables and rows but not
/// triggers would look complete and then silently stop recording history — which is the failure this proves against.
/// </remarks>
[TestClass]
public sealed class DatabaseDuplicateLiveTests
{
    private const string Source = DeploymentTestServer.ScratchDatabase;
    private const string Copy = DeploymentTestServer.ScratchCopyDatabase;

    [TestMethod]
    public async Task Service_DatabaseDuplicateLiveTests_DuplicateAsync_CarriesKeysRowsViewsInstructionsAndTriggers()
    {
        var gateway = new Service_MySqlGateway(DeploymentTestServer.Resolve());
        var backup = new DatabaseBackup(gateway);

        try
        {
            await DeploymentTestServer.DropAsync(gateway).ConfigureAwait(false);
            await BuildSourceAsync(gateway).ConfigureAwait(false);

            var outcome = await backup
                .DuplicateAsync(Source, Copy, replaceExisting: false, progress: null, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsTrue(outcome.WasCreated, outcome.Summary);
            Assert.AreEqual(3, outcome.TableCount, "a table of the source was left out of the duplicate");
            Assert.AreEqual(5L, outcome.RowCount, "the duplicate does not hold every row of the source");
            Assert.AreEqual(1, outcome.ViewCount, "the view was not copied");
            Assert.AreEqual(1, outcome.RoutineCount, "the saved instruction was not copied");

            Assert.AreEqual(
                2L,
                await DeploymentTestServer.CountRowsAsync(gateway, Copy, "child").ConfigureAwait(false),
                "the duplicated table does not hold the rows the source holds");

            var foreignKeys = await gateway
                .QueryAsync(
                    "information_schema",
                    $"SELECT COUNT(*) FROM KEY_COLUMN_USAGE WHERE TABLE_SCHEMA = '{Copy}' "
                    + "AND REFERENCED_TABLE_NAME IS NOT NULL;",
                    CancellationToken.None)
                .ConfigureAwait(false);
            Assert.AreEqual("1", foreignKeys[0][0], "the duplicate lost the foreign key");

            var comments = await gateway
                .QueryAsync(
                    "information_schema",
                    $"SELECT IFNULL(COLUMN_COMMENT,'') FROM COLUMNS WHERE TABLE_SCHEMA = '{Copy}' "
                    + "AND TABLE_NAME = 'parent' AND COLUMN_NAME = 'id';",
                    CancellationToken.None)
                .ConfigureAwait(false);
            Assert.AreEqual("A comment worth keeping", comments[0][0], "the duplicate lost a column comment");

            // The view has to read the copy. The server stores its tables fully qualified, so an unrewritten
            // definition would silently read the source instead.
            var viewDefinitions = await gateway
                .QueryAsync(
                    "information_schema",
                    $"SELECT IFNULL(VIEW_DEFINITION,'') FROM VIEWS WHERE TABLE_SCHEMA = '{Copy}';",
                    CancellationToken.None)
                .ConfigureAwait(false);
            StringAssert.Contains(viewDefinitions[0][0], Copy, "the duplicated view still reads the source database");

            // The trigger has to fire in the copy, not only exist there.
            await DeploymentTestServer
                .ExecuteAsync(gateway, Copy, "INSERT INTO child VALUES (99, 1)")
                .ConfigureAwait(false);
            Assert.AreEqual(
                3L,
                await DeploymentTestServer.CountRowsAsync(gateway, Copy, "child_log").ConfigureAwait(false),
                "the duplicated trigger did not fire, so the copy silently stops recording history");

            Assert.AreEqual(
                2L,
                await DeploymentTestServer.CountRowsAsync(gateway, Source, "child").ConfigureAwait(false),
                "copying changed the source database");
        }
        finally
        {
            await DeploymentTestServer.DropAsync(gateway).ConfigureAwait(false);
        }
    }

    [TestMethod]
    public async Task Service_DatabaseDuplicateLiveTests_DuplicateAsync_RefusesToOverwriteWithoutBeingToldTo()
    {
        var gateway = new Service_MySqlGateway(DeploymentTestServer.Resolve());
        var backup = new DatabaseBackup(gateway);

        try
        {
            await DeploymentTestServer.DropAsync(gateway).ConfigureAwait(false);
            await BuildSourceAsync(gateway).ConfigureAwait(false);
            await backup
                .DuplicateAsync(Source, Copy, replaceExisting: false, progress: null, CancellationToken.None)
                .ConfigureAwait(false);

            var thrown = await Assert
                .ThrowsExactlyAsync<InvalidOperationException>(
                    () => backup.DuplicateAsync(Source, Copy, replaceExisting: false, progress: null, CancellationToken.None))
                .ConfigureAwait(false);

            StringAssert.Contains(
                thrown.Message,
                "already exists",
                "the refusal has to say why, so the operator knows which box to tick");
        }
        finally
        {
            await DeploymentTestServer.DropAsync(gateway).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Two related tables, a third that a trigger fills, a view and a procedure — enough that a duplicate which
    /// skipped any one of them would fail a different assertion.
    /// </summary>
    private static async Task BuildSourceAsync(Service_MySqlGateway gateway)
    {
        await DeploymentTestServer.ExecuteAsync(gateway, null, $"CREATE DATABASE `{Source}`").ConfigureAwait(false);
        await DeploymentTestServer
            .ExecuteAsync(
                gateway,
                Source,
                "CREATE TABLE parent (id INT PRIMARY KEY COMMENT 'A comment worth keeping', name VARCHAR(40)) ENGINE=InnoDB")
            .ConfigureAwait(false);
        await DeploymentTestServer
            .ExecuteAsync(
                gateway,
                Source,
                "CREATE TABLE child (id INT PRIMARY KEY, parent_id INT, "
                + "CONSTRAINT fk_child_parent FOREIGN KEY (parent_id) REFERENCES parent(id)) ENGINE=InnoDB")
            .ConfigureAwait(false);
        await DeploymentTestServer
            .ExecuteAsync(gateway, Source, "CREATE TABLE child_log (id INT PRIMARY KEY, child_id INT) ENGINE=InnoDB")
            .ConfigureAwait(false);

        // Created before the rows go in, so the source's own log is filled the same way the copy's is.
        await DeploymentTestServer
            .ExecuteAsync(
                gateway,
                Source,
                "CREATE TRIGGER trg_child_log AFTER INSERT ON child FOR EACH ROW "
                + "INSERT INTO child_log VALUES (NEW.id, NEW.id)")
            .ConfigureAwait(false);
        await DeploymentTestServer
            .ExecuteAsync(gateway, Source, "CREATE VIEW vw_parents AS SELECT id, name FROM parent")
            .ConfigureAwait(false);
        await DeploymentTestServer
            .ExecuteAsync(gateway, Source, "CREATE PROCEDURE sp_child_count() SELECT COUNT(*) AS n FROM child")
            .ConfigureAwait(false);

        await DeploymentTestServer
            .ExecuteAsync(gateway, Source, "INSERT INTO parent VALUES (1, 'a')")
            .ConfigureAwait(false);
        await DeploymentTestServer
            .ExecuteAsync(gateway, Source, "INSERT INTO child VALUES (10, 1), (11, 1)")
            .ConfigureAwait(false);
    }
}
