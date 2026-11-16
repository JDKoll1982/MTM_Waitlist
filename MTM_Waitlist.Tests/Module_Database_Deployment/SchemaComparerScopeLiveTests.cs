using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Database_Deployment.Services;

namespace MTM_Waitlist.Tests.Module_Database_Deployment;

/// <summary>
/// Checking a database must not write to any database.
/// </summary>
/// <remarks>
/// A plan is built by applying the table scripts to a scratch schema and reading back what they made. Every shipped
/// script opens with <c>USE &lt;its own database&gt;;</c> so it can be pasted into a client — so unless that line is
/// re-pointed, building the expected schema writes the tables into the real store instead. That is how a check
/// silently created eight tables in the live cache mirror.
/// </remarks>
[TestClass]
public sealed class SchemaComparerScopeLiveTests
{
    [TestMethod]
    public async Task Service_SchemaComparerScopeLiveTests_BuildPlanAsync_LeavesTheDatabaseTheScriptNamesUntouched()
    {
        var gateway = new Service_MySqlGateway(DeploymentTestServer.Resolve());
        var named = DeploymentTestServer.ScratchDatabase;
        var target = DeploymentTestServer.ScratchCopyDatabase;
        var root = Path.Combine(Path.GetTempPath(), "deployment-scope-" + Guid.NewGuid().ToString("N")[..8]);

        try
        {
            await DeploymentTestServer.DropAsync(gateway).ConfigureAwait(false);
            await DeploymentTestServer.ExecuteAsync(gateway, null, $"CREATE DATABASE `{named}`").ConfigureAwait(false);
            await DeploymentTestServer.ExecuteAsync(gateway, null, $"CREATE DATABASE `{target}`").ConfigureAwait(false);

            // A table script that names a database of its own, exactly as every shipped script does.
            var folder = Path.Combine(root, "Tables", "zz_probe");
            Directory.CreateDirectory(folder);
            File.WriteAllText(
                Path.Combine(folder, "create.sql"),
                $"-- Create table: zz_probe{Environment.NewLine}{Environment.NewLine}USE {named};{Environment.NewLine}{Environment.NewLine}"
                + "CREATE TABLE IF NOT EXISTS zz_probe (id INT PRIMARY KEY) ENGINE=InnoDB;");

            var comparer = new Service_SchemaComparer(gateway, new Service_ObjectCatalog(root));
            var plan = await comparer.BuildPlanAsync(target, null, CancellationToken.None).ConfigureAwait(false);

            Assert.IsTrue(plan.Changes.Count > 0, "the plan found nothing, so the probe script was not read at all");
            Assert.AreEqual(
                0L,
                await DeploymentTestServer
                    .CountTablesAsync(gateway, named, "zz_probe")
                    .ConfigureAwait(false),
                "building a plan created a table in the database the script names, so a check wrote to a live store");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }

            await DeploymentTestServer.DropAsync(gateway).ConfigureAwait(false);
        }
    }
}
