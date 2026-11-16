using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Database_Deployment.Models;

namespace MTM_Waitlist.Tests.Module_Database_Deployment;

/// <summary>
/// What the operator is told after a run.
/// </summary>
/// <remarks>
/// A second deploy of a converging database hits a benign duplicate on almost every seed insert. If those were
/// counted as failures the run would look broken, and if genuine failures were folded into them the run would look
/// clean, so the two channels are kept apart and pinned here.
/// </remarks>
[TestClass]
public sealed class Model_DeploymentResultTests
{
    [TestMethod]
    public void Model_DeploymentResultTests_MarkBenignSkip_CountsRepeatsPerObjectOnly()
    {
        var result = new Model_DeploymentResult();

        result.MarkBenignSkip("SeedData/03_seed_dunnage_data.sql");
        result.MarkBenignSkip("SeedData/03_seed_dunnage_data.sql");
        result.MarkBenignSkip("SeedData/03_seed_dunnage_data.sql");

        Assert.AreEqual(3, result.BenignSkipCount);
        Assert.AreEqual(1, result.BenignSkips.Count);
        Assert.AreEqual(3, result.BenignSkips["SeedData/03_seed_dunnage_data.sql"]);
        Assert.AreEqual(0, result.Messages.Count);
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public void Model_DeploymentResultTests_MarkBenignSkip_KeepsDifferentObjectsApart()
    {
        var result = new Model_DeploymentResult();

        result.MarkBenignSkip("SeedData/03_seed_dunnage_data.sql");
        result.MarkBenignSkip("StoredProcedures/Module_Waitlist/sp_setup_work_center_get.sql");

        Assert.AreEqual(2, result.BenignSkips.Count);
        Assert.AreEqual(1, result.BenignSkips["SeedData/03_seed_dunnage_data.sql"]);
        Assert.AreEqual(1, result.BenignSkips["StoredProcedures/Module_Waitlist/sp_setup_work_center_get.sql"]);
    }

    [TestMethod]
    public void Model_DeploymentResultTests_MarkFailed_MakesTheRunUnsuccessful()
    {
        var result = new Model_DeploymentResult();

        result.MarkApplied("Sql_Files/Schemas/04_Table_dunnage_types.sql");
        result.MarkFailed("Sql_Files/SeedData/03_seed_dunnage_data.sql: Column 'CustomFieldID' cannot be null");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(1, result.FailedCount);
        Assert.AreEqual(1, result.Failures.Count);
    }

    [TestMethod]
    public void Model_DeploymentResultTests_MarkFailed_ReportsARepeatedFailureOnceWithItsCount()
    {
        var result = new Model_DeploymentResult();

        for (var index = 0; index < 90; index++)
        {
            result.MarkFailed("SeedData/03_seed_dunnage_data.sql: Unknown column 'quantity_type' in 'field list'");
        }

        Assert.AreEqual(90, result.FailedCount);
        Assert.AreEqual(1, result.Failures.Count);
        Assert.AreEqual(
            90,
            result.FailureCounts["SeedData/03_seed_dunnage_data.sql: Unknown column 'quantity_type' in 'field list'"]);
    }

    [TestMethod]
    public void Model_DeploymentResultTests_MarkFailed_KeepsTheOrderFailuresWereFirstSeenIn()
    {
        var result = new Model_DeploymentResult();

        result.MarkFailed("first");
        result.MarkFailed("second");
        result.MarkFailed("first");

        CollectionAssert.AreEqual(new[] { "first", "second" }, result.Failures.ToArray());
    }

    [TestMethod]
    public void Model_DeploymentResultTests_ToReportText_ListsASkippedObjectOnceWhenItRepeats()
    {
        var result = new Model_DeploymentResult();
        result.MarkApplied("Sql_Files/Schemas/04_Table_dunnage_types.sql");
        result.MarkBenignSkip("Sql_Files/SeedData/03_seed_dunnage_data.sql");
        result.MarkBenignSkip("Sql_Files/SeedData/03_seed_dunnage_data.sql");

        var report = result.ToReportText();

        StringAssert.Contains(report, "Applied: 1 | Skipped: 0 | Already applied: 2 | Failed: 0");
        StringAssert.Contains(report, "Sql_Files/SeedData/03_seed_dunnage_data.sql (x2)");
        Assert.IsFalse(report.Contains("FAILURES", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Model_DeploymentResultTests_ToReportText_AddsAFailureSectionWhenSomethingFailed()
    {
        var result = new Model_DeploymentResult();
        result.MarkFailed("Sql_Files/Views/05_View_dunnage_label_data.sql: Unknown column 'dld.quantity_type'");

        var report = result.ToReportText();

        StringAssert.Contains(report, "FAILURES");
        StringAssert.Contains(report, "Unknown column 'dld.quantity_type'");
    }

    [TestMethod]
    public void Model_DeploymentResultTests_ToReportText_PrintsTheOccurrenceCountOfARepeatedFailure()
    {
        var result = new Model_DeploymentResult();
        result.MarkFailed("SeedData/03_seed_dunnage_data.sql: Unknown column 'quantity_type' in 'field list'");
        result.MarkFailed("SeedData/03_seed_dunnage_data.sql: Unknown column 'quantity_type' in 'field list'");

        var report = result.ToReportText();

        StringAssert.Contains(report, "Failed: 2");
        StringAssert.Contains(report, "Unknown column 'quantity_type' in 'field list' (x2)");
    }
}
