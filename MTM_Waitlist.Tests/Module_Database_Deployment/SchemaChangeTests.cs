using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Database_Deployment.Models;

namespace MTM_Waitlist.Tests.Module_Database_Deployment;

/// <summary>
/// The flags that decide whether a drift report may reach its destructive branch.
/// </summary>
/// <remarks>
/// Nothing additive may require confirmation, and every removal must; the two rules are what keep a schema sync
/// from quietly dropping an operator's column.
/// </remarks>
[TestClass]
public sealed class Model_SchemaChangeTests
{
    [DataTestMethod]
    [DataRow(Enum_ChangeKind.ExtraTable)]
    [DataRow(Enum_ChangeKind.ExtraColumn)]
    [DataRow(Enum_ChangeKind.ExtraIndex)]
    [DataRow(Enum_ChangeKind.ExtraObject)]
    public void Model_SchemaChangeTests_IsRemoval_IsTrueForAnObjectTheDatabaseHasAndTheScriptsDoNot(
        Enum_ChangeKind kind)
    {
        Assert.IsTrue(CreateChange(kind).IsRemoval);
    }

    [DataTestMethod]
    [DataRow(Enum_ChangeKind.CreateTable)]
    [DataRow(Enum_ChangeKind.AddColumn)]
    [DataRow(Enum_ChangeKind.AlterColumn)]
    [DataRow(Enum_ChangeKind.AddIndex)]
    [DataRow(Enum_ChangeKind.AddForeignKey)]
    [DataRow(Enum_ChangeKind.UpdateView)]
    [DataRow(Enum_ChangeKind.UpdateProcedure)]
    [DataRow(Enum_ChangeKind.UpdateTrigger)]
    [DataRow(Enum_ChangeKind.RunSeed)]
    public void Model_SchemaChangeTests_IsRemoval_IsFalseForAnythingThatOnlyAddsOrReplaces(Enum_ChangeKind kind)
    {
        Assert.IsFalse(CreateChange(kind).IsRemoval);
    }

    [TestMethod]
    public void Model_SchemaChangeTests_RequiresConfirmation_IsFalseForAnAddedColumn()
    {
        Assert.IsFalse(CreateChange(Enum_ChangeKind.AddColumn).RequiresConfirmation);
    }

    [TestMethod]
    public void Model_SchemaChangeTests_RequiresConfirmation_IsTrueForARebuiltTable()
    {
        Assert.IsTrue(CreateChange(Enum_ChangeKind.RebuildTable).RequiresConfirmation);
    }

    [TestMethod]
    public void Model_SchemaChangeTests_DisplayText_AppendsTheDetailWhenThereIsOne()
    {
        var change = new Model_SchemaChange
        {
            Kind = Enum_ChangeKind.AddColumn,
            Target = "dunnage_types.icon",
            Detail = "varchar(50)"
        };

        Assert.AreEqual("Add column: dunnage_types.icon - varchar(50)", change.DisplayText);
    }

    [TestMethod]
    public void Model_SchemaChangeTests_DisplayText_OmitsTheSeparatorWhenTheDetailIsBlank()
    {
        var change = new Model_SchemaChange
        {
            Kind = Enum_ChangeKind.ExtraTable,
            Target = "legacy_table"
        };

        Assert.AreEqual("Extra table: legacy_table", change.DisplayText);
    }

    [TestMethod]
    public void Model_SchemaChangeTests_IsBlocking_IsFalseUntilSomethingFlagsIt()
    {
        Assert.IsFalse(CreateChange(Enum_ChangeKind.ExtraColumn).IsBlocking);
    }

    [TestMethod]
    public void Model_SchemaChangeTests_Summary_NamesTheColumnsThatBlockInserts()
    {
        var plan = new Model_DeploymentPlan
        {
            Server = "127.0.0.1",
            Port = 3306,
            Database = "sample",
            GeneratedAt = DateTimeOffset.Now,
            Changes =
            [
                new Model_SchemaChange
                {
                    Kind = Enum_ChangeKind.ExtraColumn,
                    Target = "dunnage_custom_fields.DatabaseColumnName",
                    IsBlocking = true
                },
                new Model_SchemaChange
                {
                    Kind = Enum_ChangeKind.ExtraColumn,
                    Target = "dunnage_types.icon"
                }
            ]
        };

        Assert.AreEqual(1, plan.BlockingExtraCount);
        Assert.AreEqual(2, plan.ExtraCount);
        StringAssert.Contains(plan.Summary, "extras 2");
        StringAssert.Contains(plan.Summary, "(1 database-only column(s) block inserts until removed)");
    }

    [TestMethod]
    public void Model_SchemaChangeTests_Summary_SaysNothingAboutBlockingWhenNoColumnBlocksInserts()
    {
        var plan = new Model_DeploymentPlan
        {
            Server = "127.0.0.1",
            Port = 3306,
            Database = "sample",
            GeneratedAt = DateTimeOffset.Now,
            Changes = [new Model_SchemaChange { Kind = Enum_ChangeKind.AddColumn, Target = "t.c" }]
        };

        Assert.AreEqual(0, plan.BlockingExtraCount);
        Assert.IsFalse(plan.Summary.Contains("block inserts", StringComparison.Ordinal));
    }

    private static Model_SchemaChange CreateChange(Enum_ChangeKind kind) => new() { Kind = kind, Target = "sample" };
}
