using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Database_Deployment.Services;

namespace MTM_Waitlist.Tests.Module_Database_Deployment;

/// <summary>
/// Where a script's own <c>USE</c> line is allowed to send a deployment.
/// </summary>
/// <remarks>
/// Every object script under <c>Database/</c> opens with <c>USE mtm_waitlist;</c> so it can be pasted into a client.
/// Left alone that line overrides the database the operator picked on the first page, so a run meant for the practice
/// copy would write to the live store.
/// </remarks>
[TestClass]
public sealed class Service_DeploymentEngineScopeTests
{
    [TestMethod]
    public void Service_DeploymentEngineTests_ScopeToDatabase_PointsAUseStatementAtTheChosenDatabase()
    {
        var scoped = false;

        var statements = Service_DeploymentEngine.ScopeToDatabase(
            ["USE mtm_waitlist", "SELECT 1"], "mtm_waitlist_test", ref scoped);

        Assert.AreEqual("USE `mtm_waitlist_test`", statements[0]);
        Assert.AreEqual("SELECT 1", statements[1]);
        Assert.IsTrue(scoped);
    }

    [TestMethod]
    public void Service_DeploymentEngineTests_ScopeToDatabase_FindsTheUseLineUnderneathAScriptHeader()
    {
        var scoped = false;

        var statements = Service_DeploymentEngine.ScopeToDatabase(
            ["-- Create table: config_images_locations\n-- Engine: MySQL 5.7\n\nUSE mtm_waitlist"],
            "mtm_waitlist_test",
            ref scoped);

        Assert.AreEqual("USE `mtm_waitlist_test`", statements[0]);
        Assert.IsTrue(scoped, "a USE line under a script's header comment still names the live store");
    }

    [TestMethod]
    public void Service_DeploymentEngineTests_ScopeToDatabase_LeavesEveryOtherStatementAlone()
    {
        var scoped = false;

        var statements = Service_DeploymentEngine.ScopeToDatabase(
            ["SELECT 1", "CALL sp_thing()"], "mtm_waitlist_test", ref scoped);

        CollectionAssert.AreEqual(new[] { "SELECT 1", "CALL sp_thing()" }, statements.ToArray());
        Assert.IsFalse(scoped);
    }

    [TestMethod]
    public void Service_DeploymentEngineTests_ScopeToDatabase_ReportsNothingWhenTheScriptAlreadyAgrees()
    {
        var scoped = false;

        var statements = Service_DeploymentEngine.ScopeToDatabase(["USE `mtm_waitlist`"], "mtm_waitlist", ref scoped);

        Assert.AreEqual("USE `mtm_waitlist`", statements[0]);
        Assert.IsFalse(scoped);
    }

    [TestMethod]
    public void Service_DeploymentEngineTests_ScopeToDatabase_LeavesAStatementThatOnlyMentionsUseMidLine()
    {
        var scoped = false;

        var statements = Service_DeploymentEngine.ScopeToDatabase(
            ["SELECT 'USE mtm_waitlist' AS note"], "mtm_waitlist_test", ref scoped);

        Assert.AreEqual("SELECT 'USE mtm_waitlist' AS note", statements[0]);
        Assert.IsFalse(scoped);
    }
}
