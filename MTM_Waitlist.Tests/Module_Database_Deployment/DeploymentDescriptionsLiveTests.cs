using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Database_Deployment.Services;

namespace MTM_Waitlist.Tests.Module_Database_Deployment;

/// <summary>
/// A deployment leaves the store documented, not only shaped: the maintained description file is applied, so a
/// fresh rebuild reads the same as a store that has been updated in place.
/// </summary>
/// <remarks>
/// The probe is a column the table script declares without a comment and only the description file documents.
/// Without the description step a rebuild produces the right shape and an undocumented store, which is exactly the
/// difference this case exists to catch.
/// </remarks>
[TestClass]
public sealed class DeploymentDescriptionsLiveTests
{
    [TestMethod]
    public async Task Service_DeploymentDescriptionsLiveTests_RebuildAsync_DocumentsAColumnTheTableScriptLeavesBare()
    {
        var gateway = new Service_MySqlGateway(DeploymentTestServer.Resolve());
        var engine = new Service_DeploymentEngine(
            gateway,
            new Service_ObjectCatalog(new Service_DeploymentSettings().SqlRoot),
            new DatabaseBackup(gateway));

        try
        {
            await DeploymentTestServer.DropAsync(gateway).ConfigureAwait(false);

            var result = await engine
                .RebuildAsync(DeploymentTestServer.ScratchDatabase, null, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsTrue(result.IsSuccess, result.ToReportText());

            var comment = await DeploymentTestServer
                .ReadColumnCommentAsync(
                    gateway,
                    DeploymentTestServer.ScratchDatabase,
                    "core_users_profiles",
                    "first_name")
                .ConfigureAwait(false);

            Assert.AreEqual(
                "The person given name, 1 to 128 characters.",
                comment,
                "the description file was not applied, so a column the table script leaves bare stays undocumented");
        }
        finally
        {
            await DeploymentTestServer.DropAsync(gateway).ConfigureAwait(false);
        }
    }
}
