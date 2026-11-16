using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Database_Deployment.Services;

namespace MTM_Waitlist.Tests.Module_Database_Deployment;

/// <summary>
/// Which scripts a database is deployed from. The waitlist store and the cache mirror have different schemas, and
/// sending one application's tree to the other's database is the failure this pins.
/// </summary>
[TestClass]
public sealed class DeploymentScopeTests
{
    private const string Root = @"C:\tool\Database";

    [TestMethod]
    public void Service_DeploymentScopeTests_SqlRootFor_GivesAWaitlistStoreTheWaitlistTree()
    {
        Assert.AreEqual(Root, DeploymentScope.SqlRootFor(Root, "mtm_waitlist"));
        Assert.AreEqual(Root, DeploymentScope.SqlRootFor(Root, "mtm_waitlist_test"));
    }

    [TestMethod]
    public void Service_DeploymentScopeTests_SqlRootFor_GivesACacheMirrorItsOwnTree()
    {
        var expected = Path.Combine(Root, "Mock");

        Assert.AreEqual(expected, DeploymentScope.SqlRootFor(Root, "mtm_mock"));
        Assert.AreEqual(expected, DeploymentScope.SqlRootFor(Root, "mtm_mock_test"));
        Assert.AreEqual(expected, DeploymentScope.SqlRootFor(Root, "MTM_MOCK_COPY"));
    }

    [TestMethod]
    public void Service_DeploymentScopeTests_SqlRootFor_RefusesAnotherApplicationsStore()
    {
        Assert.IsNull(DeploymentScope.SqlRootFor(Root, "mtm_receiving_application"));
        Assert.IsNull(DeploymentScope.SqlRootFor(Root, "mtm_wip_application_winforms"));
        Assert.IsNull(DeploymentScope.SqlRootFor(Root, "sakila"));
        Assert.IsNull(DeploymentScope.SqlRootFor(Root, "  "));
    }

    [TestMethod]
    public void Service_DeploymentScopeTests_UnsupportedDatabaseMessage_NamesTheDatabaseAndSaysNothingWillChange()
    {
        var message = DeploymentScope.UnsupportedDatabaseMessage("mtm_receiving_application");

        StringAssert.Contains(message, "mtm_receiving_application");
        StringAssert.Contains(message, "no scripts");
    }
}
