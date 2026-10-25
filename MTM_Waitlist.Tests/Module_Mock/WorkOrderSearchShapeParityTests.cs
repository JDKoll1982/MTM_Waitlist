using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Mock.Services;

namespace MTM_Waitlist.Tests.Module_Mock;

/// <summary>
/// The eighth read shape's proofs (<c>work_order_search</c>; playbook steps 1–3b): the catalog declares it, the
/// live read and the mirror project the same columns in the same order, and every artifact it needs ships with
/// its rollback.
/// </summary>
/// <remarks>
/// Parity is proved against the shipped scripts rather than a database, because what must not drift is the
/// projection: the live search and <c>sp_visual_work_order_search_get</c> answer the same seven column names in
/// the same order, which is what lets a caller of the fallback be unable to tell which source answered.
/// </remarks>
[TestClass]
public sealed class WorkOrderSearchShapeParityTests
{
    private const string ShapeKey = "work_order_search";

    private static readonly string[] s_expectedInputs = ["WorkOrderText", "PartFilter", "CustomerFilter"];

    private static readonly string[] s_expectedColumns =
    [
        "WorkOrder",
        "PartNumber",
        "PartDescription",
        "CustomerName",
        "WorkCenter",
        "Status",
        "WantDate",
    ];

    [TestMethod]
    public void Service_WorkOrderSearchShapeParityTests_TheCatalog_HoldsEightShapesIncludingTheWorkOrderSearch()
    {
        var shapes = VisualReadShapeCatalog.Create();

        Assert.AreEqual(8, shapes.Count, "the count moved from seven to eight deliberately");
        Assert.AreEqual(
            1,
            shapes.Count(shape => string.Equals(shape.Key, ShapeKey, StringComparison.Ordinal)),
            "the catalog holds exactly one work_order_search shape");
    }

    [TestMethod]
    public void Service_WorkOrderSearchShapeParityTests_TheShape_TakesThreeFiltersAndAnswersTheOrderWithItsCustomer()
    {
        var shape = VisualReadShapeCatalog.Create().Single(s => s.Key == ShapeKey);

        CollectionAssert.AreEqual(
            s_expectedInputs,
            shape.InputParameters.Select(parameter => parameter.Name).ToArray());

        CollectionAssert.AreEqual(
            s_expectedColumns,
            shape.OutputColumns.Select(column => column.Name).ToArray(),
            "the shape's declared output is the row contract");
    }

    [TestMethod]
    public void Service_WorkOrderSearchShapeParityTests_TheLiveReadAndTheMirror_ProjectTheSameColumnsInTheSameOrder()
    {
        var live = File.ReadAllText(RepoPath(
            "Database", "InforVisual", "Queues", "Module_Setup", "Queries", "GetWorkOrderSearch.sql"));
        var mirror = File.ReadAllText(RepoPath(
            "Database", "Mock", "StoredProcedures", "sp_visual_work_order_search_get", "create.sql"));

        CollectionAssert.AreEqual(s_expectedColumns, AliasesInOrder(live), "the live read projects the agreed names");
        CollectionAssert.AreEqual(
            s_expectedColumns,
            AliasesInOrder(mirror),
            "the mirror projects the agreed names, so a caller cannot tell which answered (FR-004)");
    }

    [TestMethod]
    public void Service_WorkOrderSearchShapeParityTests_TheCustomerFilter_MatchesTheCustomersNameAndNeverItsId()
    {
        var live = File.ReadAllText(RepoPath(
            "Database", "InforVisual", "Queues", "Module_Setup", "Queries", "GetWorkOrderSearch.sql"));

        StringAssert.Contains(live, "CUST_NAME", "the customer is resolved from the allocation's name column");
        StringAssert.Contains(live, "SUPPLY_TYPE = 'WO'", "only the allocations that name a work order are joined");
        StringAssert.Contains(live, "a.CUST_NAME LIKE", "the customer filter matches the name the row displays");
        Assert.IsFalse(
            live.Contains("CUST_ID LIKE", StringComparison.OrdinalIgnoreCase),
            "a customer search must never match the customer's id (operator requirement)");
    }

    [TestMethod]
    public void Service_WorkOrderSearchShapeParityTests_TheSearchIsLimitedToWhatTheEntryPageCanAccept()
    {
        var live = File.ReadAllText(RepoPath(
            "Database", "InforVisual", "Queues", "Module_Setup", "Queries", "GetWorkOrderSearch.sql"));

        StringAssert.Contains(
            live,
            "LEN(LTRIM(RTRIM(wo.BASE_ID))) = 9",
            "only canonical WO-###### orders are returned, because that is the only form the entry page resolves");
        StringAssert.Contains(live, "wo.STATUS IN ('R', 'U', 'F')", "closed and cancelled orders are not offered");
    }

    // There is deliberately no `..._result_prev` artifact: the `_prev` name is transient inside
    // sp_visual_work_order_search_refresh's single rename, exactly as it is for the other shapes.
    [TestMethod]
    [DataRow("Database/Mock/Tables/visual_work_order_search_result")]
    [DataRow("Database/Mock/Tables/visual_work_order_search_result_stage")]
    [DataRow("Database/Mock/StoredProcedures/sp_visual_work_order_search_get")]
    [DataRow("Database/Mock/StoredProcedures/sp_visual_work_order_search_refresh")]
    public void Service_WorkOrderSearchShapeParityTests_EveryArtifactTheShapeNeeds_ShipsItsCreateAndItsRollback(string relativeFolder)
    {
        var folder = RepoPath(relativeFolder.Split('/'));

        Assert.IsTrue(File.Exists(Path.Combine(folder, "create.sql")), $"{relativeFolder} has no create.sql");
        Assert.IsTrue(File.Exists(Path.Combine(folder, "rollback.sql")), $"{relativeFolder} has no rollback.sql");
    }

    [TestMethod]
    public void Service_WorkOrderSearchShapeParityTests_TheShapeNamesItsLiveReadAndItsPopulationScript()
    {
        var shape = VisualReadShapeCatalog.Create().Single(s => s.Key == ShapeKey);

        StringAssert.Contains(shape.SourceScriptRelativePath, "Module_Setup");
        StringAssert.Contains(shape.PopulationScriptRelativePath, "work_order_search_population.sql");
        Assert.IsTrue(File.Exists(RepoPath(shape.SourceScriptRelativePath.Split('/'))));
        Assert.IsTrue(File.Exists(RepoPath(shape.PopulationScriptRelativePath!.Split('/'))));
    }

    /// <summary>The column aliases a script projects, in the order the script writes them, for the agreed names.</summary>
    private static string[] AliasesInOrder(string script)
    {
        var found = new List<(string Name, int Index)>();

        foreach (var name in s_expectedColumns)
        {
            var index = script.IndexOf($"AS {name}", StringComparison.Ordinal);
            if (index >= 0)
            {
                found.Add((name, index));
            }
        }

        return found.OrderBy(entry => entry.Index).Select(entry => entry.Name).ToArray();
    }

    private static string RepoPath(params string[] segments)
    {
        var root = RepositoryPatternScan.FindRepositoryRoot();
        return Path.Combine([root, .. segments]);
    }
}
