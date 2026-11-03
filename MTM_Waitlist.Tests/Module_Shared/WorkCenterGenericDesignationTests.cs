using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Shared;

/// <summary>
/// The generic designation is a fact the store holds and the wizard reads, and no work centre is generic because a
/// build or a default said so (016-generic-wc-request-foundation, FR-001, FR-003).
/// </summary>
/// <remarks>
/// The designation itself is maintained on the work centre form and is covered by
/// <c>SetupWorkCenterGenericMarkupTests</c>. What is proved here is the other half of FR-001: the read the New
/// Request step already loads carries the designation, and a row nobody designated reports not generic rather than
/// falling into the generic flow.
/// </remarks>
[TestClass]
public sealed class WorkCenterGenericDesignationTests
{
    private static string Root => RepositoryPatternScan.FindRepositoryRoot();

    [TestMethod]
    public void Audit_WorkCenterGenericDesignationTests_AFreshCatalogRowIsNotGeneric()
    {
        var createSql = File.ReadAllText(Path.Combine(Root, "Database", "Tables", "13_setup_work_centers_catalog", "create.sql"));

        StringAssert.Contains(
            createSql,
            "is_generic TINYINT(1) NOT NULL DEFAULT 0",
            "a fresh install must leave every work centre at not generic (FR-003)");
    }

    [TestMethod]
    public void Audit_WorkCenterGenericDesignationTests_TheReadTheWizardUsesCarriesTheDesignation()
    {
        var getSql = File.ReadAllText(Path.Combine(Root, "Database", "StoredProcedures", "sp_setup_work_centers_get_all", "create.sql"));

        StringAssert.Contains(
            getSql,
            "is_generic",
            "the catalog read the New Request step loads must project the designation, or a generic work centre can never be recognised (FR-001, FR-005)");
    }

    [TestMethod]
    public void Audit_WorkCenterGenericDesignationTests_AnUndesignatedWorkCentreReportsNotGenericOnBothShapes()
    {
        Assert.IsFalse(new WorkCenterDetail().IsGeneric, "a detail built without a designation must not claim to be generic");
        Assert.IsFalse(new WorkCenterSelectionItem().IsGeneric, "a selection item built without a designation must not claim to be generic");
    }

    [TestMethod]
    public void Audit_WorkCenterGenericDesignationTests_TheDesignationTravelsOnBothShapes()
    {
        Assert.IsTrue(new WorkCenterDetail { IsGeneric = true }.IsGeneric);
        Assert.IsTrue(new WorkCenterSelectionItem { IsGeneric = true }.IsGeneric);
    }
}
