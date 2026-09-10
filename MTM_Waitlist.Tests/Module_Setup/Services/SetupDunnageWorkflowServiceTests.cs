using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Setup.Services;

namespace MTM_Waitlist.Tests.Module_Setup.Services;

[TestClass]
public sealed class SetupDunnageWorkflowServiceTests
{
    [TestMethod]
    public async Task AddDunnageTypeAsync_WhenRoleIsNotAllowed_ReturnsFailure()
    {
        var service = CreateService();

        var result = await service.AddDunnageTypeAsync("TestType", "Operator");

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Message.Contains("permission", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task AddDunnagePartAsync_WhenRoleIsNotAllowed_ReturnsFailure()
    {
        var service = CreateService();

        var result = await service.AddDunnagePartAsync("1", "TestPart", "Operator");

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Message.Contains("permission", StringComparison.OrdinalIgnoreCase));
    }

    private static DunnageWorkflowService CreateService()
    {
        // The receiving store is always read live: the mock toggle and its sample data are gone
        // (FR-001, FR-014), so the service needs only the MySQL helper.
        return new DunnageWorkflowService(new MySqlHelperServer());
    }
}
