using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class DeveloperAccessGuardTests
{
    [TestMethod]
    public void Default_AllowsDeveloperAdminAdministrator_CaseInsensitive()
    {
        var guard = new DeveloperAccessGuard();

        Assert.IsTrue(guard.CanAccessDeveloperSettings("developer"));
        Assert.IsTrue(guard.CanEditMockMasterData("Admin"));
        Assert.IsTrue(guard.CanEditRealCatalog("ADMINISTRATOR"));
        Assert.IsTrue(guard.CanChangeCentralMockConfig("Developer"));
    }

    [TestMethod]
    public void Default_DeniesOperatorRolesAndNull()
    {
        var guard = new DeveloperAccessGuard();

        Assert.IsFalse(guard.CanAccessDeveloperSettings("material handler"));
        Assert.IsFalse(guard.CanEditMockMasterData("supervisor"));
        Assert.IsFalse(guard.CanEditRealCatalog("quality inspector"));
        Assert.IsFalse(guard.CanChangeCentralMockConfig(null));
        Assert.IsFalse(guard.CanAccessDeveloperSettings(string.Empty));
        Assert.IsFalse(guard.CanAccessDeveloperSettings("   "));
    }

    [TestMethod]
    public void CustomRoles_AreHonored()
    {
        var guard = new DeveloperAccessGuard(new[] { "Developer", "Plant Manager" });

        Assert.IsTrue(guard.CanAccessDeveloperSettings("PLANT MANAGER"));
        Assert.IsTrue(guard.CanEditMockMasterData("developer"));
        Assert.IsFalse(guard.CanAccessDeveloperSettings("admin"));
    }

    [TestMethod]
    public void AllowedDeveloperRoles_AreNormalizedAndOrdered()
    {
        var guard = new DeveloperAccessGuard(new[] { "Admin", "Developer" });

        CollectionAssert.AreEqual(new[] { "admin", "developer" }, guard.AllowedDeveloperRoles.ToArray());
    }
}
