using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Database_Deployment.Models;
using MTM_Database_Deployment.Services;

namespace MTM_Waitlist.Tests.Module_Database_Deployment;

/// <summary>
/// What the deploy is allowed to see, and in which order it applies it.
/// </summary>
/// <remarks>
/// The catalog is the tool's whole permission surface: a file it does not discover is never applied, and a file it
/// discovers out of order is applied before the thing it depends on. The rollback beside every object and the
/// aggregates beside every folder must both stay invisible, or a deployment would undo itself or run twice.
/// </remarks>
[TestClass]
public sealed class Service_ObjectCatalogTests
{
    private string _root = string.Empty;

    [TestInitialize]
    public void TestInitialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "mtm-catalog-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_root);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [TestMethod]
    public void Service_ObjectCatalogTests_LoadObjects_ReadsOneCreateScriptPerObjectFolder()
    {
        WriteFile("Tables/04_core_users/create.sql");
        WriteFile("Views/05_v_waitlist/create.sql");
        WriteFile("StoredProcedures/sp_setup_work_center_get/create.sql");
        WriteFile("Functions/fn_config_settings_scope_rank/create.sql");
        WriteFile("Seeds/seed_plant_buildings_default/create.sql");

        var objects = new Service_ObjectCatalog(_root).LoadObjects();

        Assert.AreEqual(5, objects.Count);
        CollectionAssert.AreEquivalent(
            new[]
            {
                "Tables/04_core_users/create.sql",
                "Views/05_v_waitlist/create.sql",
                "StoredProcedures/sp_setup_work_center_get/create.sql",
                "Functions/fn_config_settings_scope_rank/create.sql",
                "Seeds/seed_plant_buildings_default/create.sql"
            },
            objects.Select(item => item.RelativePath).ToArray());
    }

    [TestMethod]
    public void Service_ObjectCatalogTests_LoadObjects_IgnoresRollbacksAggregatesAndArchivedObjects()
    {
        WriteFile("Tables/04_core_users/create.sql");
        WriteFile("Tables/04_core_users/rollback.sql");
        WriteFile("Tables/AllTables.sql");
        WriteFile("Tables/_Archive/99_retired/create.sql");
        WriteFile("Seeds/seed_dev_masked_baseline/WHY-ARCHIVED.md");

        var objects = new Service_ObjectCatalog(_root).LoadObjects();

        Assert.AreEqual(1, objects.Count);
        Assert.AreEqual("Tables/04_core_users/create.sql", objects[0].RelativePath);
    }

    [TestMethod]
    public void Service_ObjectCatalogTests_LoadObjects_LeavesOutADevelopmentReset()
    {
        WriteFile("Seeds/seed_plant_buildings_default/create.sql");
        WriteFile("Seeds/seed_dev_masked_baseline/create.sql");

        var objects = new Service_ObjectCatalog(_root).LoadObjects();

        Assert.AreEqual(1, objects.Count);
        Assert.AreEqual("seed_plant_buildings_default", objects[0].Name);
    }

    [TestMethod]
    public void Service_ObjectCatalogTests_LoadObjects_NamesEachObjectAfterItsFolder()
    {
        WriteFile("StoredProcedures/sp_setup_work_center_get/create.sql");

        Assert.AreEqual("sp_setup_work_center_get", new Service_ObjectCatalog(_root).LoadObjects()[0].Name);
    }

    [TestMethod]
    public void Service_ObjectCatalogTests_LoadObjects_ClassifiesEachFolderAndIgnoresTheRest()
    {
        WriteFile("Tables/04_core_users/create.sql");
        WriteFile("Views/05_v_waitlist/create.sql");
        WriteFile("StoredProcedures/sp_setup_work_center_get/create.sql");
        WriteFile("Functions/fn_config_settings_scope_rank/create.sql");
        WriteFile("Triggers/01_trg_demo/create.sql");
        WriteFile("Seeds/seed_plant_buildings_default/create.sql");
        WriteFile("Validation/check_users/validate.sql");
        WriteFile("Bootstrap/create_database.sql");

        var objects = new Service_ObjectCatalog(_root).LoadObjects();

        Assert.AreEqual(6, objects.Count);
        Assert.AreEqual(
            Enum_SqlObjectKind.Schema,
            objects.Single(item => item.RelativePath.StartsWith("Tables/", StringComparison.Ordinal)).Kind);
        Assert.AreEqual(
            Enum_SqlObjectKind.View,
            objects.Single(item => item.RelativePath.StartsWith("Views/", StringComparison.Ordinal)).Kind);
        Assert.AreEqual(
            Enum_SqlObjectKind.Procedure,
            objects.Single(item => item.RelativePath.StartsWith("StoredProcedures/", StringComparison.Ordinal)).Kind);
        Assert.AreEqual(
            Enum_SqlObjectKind.Function,
            objects.Single(item => item.RelativePath.StartsWith("Functions/", StringComparison.Ordinal)).Kind);
        Assert.AreEqual(
            Enum_SqlObjectKind.Trigger,
            objects.Single(item => item.RelativePath.StartsWith("Triggers/", StringComparison.Ordinal)).Kind);
        Assert.AreEqual(
            Enum_SqlObjectKind.Seed,
            objects.Single(item => item.RelativePath.StartsWith("Seeds/", StringComparison.Ordinal)).Kind);
    }

    [TestMethod]
    public void Service_ObjectCatalogTests_LoadObjects_OrdersTablesFirstAndSeedsLast()
    {
        WriteFile("Seeds/seed_plant_buildings_default/create.sql");
        WriteFile("Functions/fn_config_settings_scope_rank/create.sql");
        WriteFile("StoredProcedures/sp_setup_work_center_get/create.sql");
        WriteFile("Views/05_v_waitlist/create.sql");
        WriteFile("Tables/04_core_users/create.sql");

        var kinds = new Service_ObjectCatalog(_root).LoadObjects().Select(item => item.Kind).ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                Enum_SqlObjectKind.Schema,
                Enum_SqlObjectKind.View,
                Enum_SqlObjectKind.Procedure,
                Enum_SqlObjectKind.Function,
                Enum_SqlObjectKind.Seed
            },
            kinds);
    }

    [TestMethod]
    public void Service_ObjectCatalogTests_LoadObjects_RecursesIntoNestedObjectFolders()
    {
        WriteFile("StoredProcedures/Module_Waitlist/sp_waitlist_request_get/create.sql");

        var objects = new Service_ObjectCatalog(_root).LoadObjects();

        Assert.AreEqual(1, objects.Count);
        Assert.AreEqual("sp_waitlist_request_get", objects[0].Name);
    }

    [TestMethod]
    public void Service_ObjectCatalogTests_LoadObjects_ReturnsNothingWhenTheSqlRootIsMissing()
    {
        var missing = Path.Combine(_root, "does-not-exist");

        Assert.AreEqual(0, new Service_ObjectCatalog(missing).LoadObjects().Count);
    }

    private void WriteFile(string relativePath)
    {
        var path = Path.Combine(_root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "SELECT 1;");
    }
}
