using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Database_Deployment.Models;
using MTM_Database_Deployment.Services;

namespace MTM_Waitlist.Tests.Module_Database_Deployment;

/// <summary>
/// Safe update must not re-seed a store it has already seeded.
/// </summary>
/// <remarks>
/// The seed in these cases is written the way several shipped ones are — an insert that updates the value when the
/// row is already there — so a second pass writes the shipped default over whatever the operator set. Bringing a
/// live store up to date has to leave that value alone, and a seed the store has never received still has to run.
/// </remarks>
[TestClass]
public sealed class SeedLedgerLiveTests
{
    private const string Database = DeploymentTestServer.ScratchDatabase;

    [TestMethod]
    public async Task Service_SeedLedgerLiveTests_ApplyAsync_LeavesAnOperatorsValueAloneOnASecondRun()
    {
        var gateway = new Service_MySqlGateway(DeploymentTestServer.Resolve());
        var root = CreateCatalog(seedCount: 1);

        try
        {
            await PrepareDatabaseAsync(gateway).ConfigureAwait(false);

            var first = await DeployAsync(gateway, root).ConfigureAwait(false);
            Assert.IsTrue(first.IsSuccess, first.ToReportText());
            Assert.AreEqual("shipped", await ReadValueAsync(gateway).ConfigureAwait(false), "the first run did not seed");

            // The operator changes the value the seed set.
            await DeploymentTestServer
                .ExecuteAsync(gateway, Database, "UPDATE zz_setting SET setting_value = 'operator' WHERE setting_key = 'mode'")
                .ConfigureAwait(false);

            var plan = await BuildPlanAsync(gateway, root).ConfigureAwait(false);
            Assert.IsFalse(
                plan.Changes.Any(change => change.Kind == Enum_ChangeKind.RunSeed),
                "a seed the store has already received was offered again");
            Assert.IsFalse(
                plan.Changes.Any(change => string.Equals(change.Target, AppliedSeedLedger.TableName, StringComparison.OrdinalIgnoreCase)),
                "the tool's own ledger was offered as a leftover to remove");

            var second = await DeployAsync(gateway, root).ConfigureAwait(false);
            Assert.IsTrue(second.IsSuccess, second.ToReportText());

            Assert.AreEqual(
                "operator",
                await ReadValueAsync(gateway).ConfigureAwait(false),
                "the second run put the shipped default back over the operator's value");
        }
        finally
        {
            DeleteCatalog(root);
            await DeploymentTestServer.DropAsync(gateway).ConfigureAwait(false);
        }
    }

    [TestMethod]
    public async Task Service_SeedLedgerLiveTests_BuildPlanAsync_StillOffersASeedTheStoreHasNeverReceived()
    {
        var oneSeed = CreateCatalog(seedCount: 1);
        var twoSeeds = CreateCatalog(seedCount: 2);
        var gateway = new Service_MySqlGateway(DeploymentTestServer.Resolve());

        try
        {
            await PrepareDatabaseAsync(gateway).ConfigureAwait(false);
            var first = await DeployAsync(gateway, oneSeed).ConfigureAwait(false);
            Assert.IsTrue(first.IsSuccess, first.ToReportText());

            var plan = await BuildPlanAsync(gateway, twoSeeds).ConfigureAwait(false);
            var seedsOffered = plan.Changes
                .Where(change => change.Kind == Enum_ChangeKind.RunSeed)
                .Select(change => change.Target)
                .ToArray();

            CollectionAssert.AreEqual(
                new[] { "Seeds/seed_two/create.sql" },
                seedsOffered,
                "the plan should offer exactly the seed this store has not received");
        }
        finally
        {
            DeleteCatalog(oneSeed);
            DeleteCatalog(twoSeeds);
            await DeploymentTestServer.DropAsync(gateway).ConfigureAwait(false);
        }
    }

    [TestMethod]
    public async Task Service_SeedLedgerLiveTests_ApplyAsync_RecordsAStoreSetUpBeforeTheLedgerWithoutRunningItsSeeds()
    {
        var gateway = new Service_MySqlGateway(DeploymentTestServer.Resolve());
        var root = CreateCatalog(seedCount: 1);

        try
        {
            await PrepareDatabaseAsync(gateway).ConfigureAwait(false);

            // A store that already holds the table and an operator's own value, and has no record of being seeded —
            // which is the state of every database built by an earlier installer.
            await DeploymentTestServer
                .ExecuteAsync(
                    gateway,
                    Database,
                    "CREATE TABLE zz_setting (setting_key VARCHAR(64) NOT NULL PRIMARY KEY, "
                    + "setting_value VARCHAR(64) NOT NULL) ENGINE=InnoDB")
                .ConfigureAwait(false);
            await DeploymentTestServer
                .ExecuteAsync(gateway, Database, "INSERT INTO zz_setting VALUES ('mode', 'operator')")
                .ConfigureAwait(false);

            var result = await DeployAsync(gateway, root).ConfigureAwait(false);
            Assert.IsTrue(result.IsSuccess, result.ToReportText());

            Assert.AreEqual(
                "operator",
                await ReadValueAsync(gateway).ConfigureAwait(false),
                "a store set up before the ledger existed was re-seeded");

            var ledger = await AppliedSeedLedger.ReadAsync(gateway, Database, CancellationToken.None).ConfigureAwait(false);
            CollectionAssert.Contains(
                ledger.ToArray(),
                "Seeds/seed_one/create.sql",
                "the store was not recorded as seeded, so the next run would seed it");
        }
        finally
        {
            DeleteCatalog(root);
            await DeploymentTestServer.DropAsync(gateway).ConfigureAwait(false);
        }
    }

    private static async Task PrepareDatabaseAsync(Service_MySqlGateway gateway)
    {
        await DeploymentTestServer.DropAsync(gateway).ConfigureAwait(false);
        await DeploymentTestServer
            .ExecuteAsync(gateway, null, $"CREATE DATABASE `{Database}`")
            .ConfigureAwait(false);
    }

    private static async Task<Model_DeploymentPlan> BuildPlanAsync(Service_MySqlGateway gateway, string root)
    {
        var comparer = new Service_SchemaComparer(gateway, new Service_ObjectCatalog(root));
        return await comparer.BuildPlanAsync(Database, null, CancellationToken.None).ConfigureAwait(false);
    }

    private static async Task<Model_DeploymentResult> DeployAsync(Service_MySqlGateway gateway, string root)
    {
        var plan = await BuildPlanAsync(gateway, root).ConfigureAwait(false);
        var engine = new Service_DeploymentEngine(
            gateway,
            new Service_ObjectCatalog(root),
            new DatabaseBackup(gateway));

        return await engine.ApplyAsync(plan, dropConfirmedExtras: false, null, CancellationToken.None).ConfigureAwait(false);
    }

    private static async Task<string> ReadValueAsync(Service_MySqlGateway gateway)
    {
        var rows = await gateway
            .QueryAsync(Database, "SELECT setting_value FROM zz_setting WHERE setting_key = 'mode';", CancellationToken.None)
            .ConfigureAwait(false);

        return rows.Count > 0 ? rows[0][0] : string.Empty;
    }

    /// <summary>
    /// A throw-away catalog with one table and one or two seeds. Every script names the scratch database, exactly as
    /// the shipped ones name theirs.
    /// </summary>
    private static string CreateCatalog(int seedCount)
    {
        var root = Path.Combine(Path.GetTempPath(), "seed-ledger-" + Guid.NewGuid().ToString("N")[..8]);

        var tableFolder = Path.Combine(root, "Tables", "zz_setting");
        Directory.CreateDirectory(tableFolder);
        File.WriteAllText(
            Path.Combine(tableFolder, "create.sql"),
            $"-- Create table: zz_setting{Environment.NewLine}USE {Database};{Environment.NewLine}"
            + "CREATE TABLE IF NOT EXISTS zz_setting (setting_key VARCHAR(64) NOT NULL PRIMARY KEY, "
            + "setting_value VARCHAR(64) NOT NULL) ENGINE=InnoDB;");

        WriteSeed(root, "seed_one", "shipped");
        if (seedCount > 1)
        {
            WriteSeed(root, "seed_two", "second");
        }

        return root;
    }

    private static void WriteSeed(string root, string name, string value)
    {
        var folder = Path.Combine(root, "Seeds", name);
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, "create.sql"),
            $"-- Seed: {name}{Environment.NewLine}USE {Database};{Environment.NewLine}"
            + "INSERT INTO zz_setting (setting_key, setting_value) VALUES ('mode', '" + value + "') "
            + "ON DUPLICATE KEY UPDATE setting_value = VALUES(setting_value);");
    }

    private static void DeleteCatalog(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
