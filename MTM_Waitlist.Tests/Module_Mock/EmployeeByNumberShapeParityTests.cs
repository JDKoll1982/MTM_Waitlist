using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Mock.Services;

namespace MTM_Waitlist.Tests.Module_Mock;

/// <summary>
/// The sixth read shape's proofs (`contracts/employee-read-shape-contract.md`; FR-032, FR-033): the catalog holds
/// six shapes and one of them returns an employee by number, the mirrored row has the same shape as the live read,
/// and every artifact the shape needs exists with its rollback.
/// </summary>
/// <remarks>
/// Parity is proved against the shipped scripts rather than a database, because what must not drift is the
/// projection: the live read and <c>sp_visual_employee_by_number_get</c> answer the same three column names in
/// the same order, which is what lets a caller of the fallback be unable to tell which source answered.
/// </remarks>
[TestClass]
public sealed class EmployeeByNumberShapeParityTests
{
    private const string ShapeKey = "employee_by_number";

    private static readonly string[] ExpectedColumns = ["EmployeeNumber", "FirstName", "LastName"];

    [TestMethod]
    public void TheCatalog_HoldsSixShapesIncludingTheEmployeeByNumber()
    {
        var shapes = VisualReadShapeCatalog.Create();

        Assert.AreEqual(6, shapes.Count, "FR-033: the count moved from five to six deliberately");
        Assert.AreEqual(
            1,
            shapes.Count(shape => string.Equals(shape.Key, ShapeKey, StringComparison.Ordinal)),
            "the catalog holds exactly one employee_by_number shape");
    }

    [TestMethod]
    public void TheEmployeeByNumberShape_TakesANumberAndAnswersTheThreeNames()
    {
        var shape = VisualReadShapeCatalog.Create().Single(s => s.Key == ShapeKey);

        CollectionAssert.AreEqual(
            new[] { "EmployeeNumber" },
            shape.InputParameters.Select(parameter => parameter.Name).ToArray());

        CollectionAssert.AreEqual(
            ExpectedColumns,
            shape.OutputColumns.Select(column => column.Name).ToArray(),
            "the shape's declared output is the row contract");
    }

    [TestMethod]
    public void TheLiveReadAndTheMirror_ProjectTheSameColumnsInTheSameOrder()
    {
        var live = File.ReadAllText(RepoPath(
            "Database", "InforVisual", "Queues", "Module_Startup", "Queries", "GetEmployeeByNumber.sql"));
        var mirror = File.ReadAllText(RepoPath(
            "Database", "Mock", "StoredProcedures", "sp_visual_employee_by_number_get", "create.sql"));

        CollectionAssert.AreEqual(
            ExpectedColumns,
            AliasesIn(live),
            "the live read projects the agreed names");
        CollectionAssert.AreEqual(
            ExpectedColumns,
            AliasesIn(mirror),
            "the mirror projects the agreed names, so a caller cannot tell which answered (FR-032)");
    }

    [TestMethod]
    [DataRow("Database/Mock/Tables/visual_employee_by_number_result")]
    [DataRow("Database/Mock/Tables/visual_employee_by_number_result_stage")]
    [DataRow("Database/Mock/Tables/visual_employee_by_number_result_prev")]
    [DataRow("Database/Mock/StoredProcedures/sp_visual_employee_by_number_get")]
    [DataRow("Database/Mock/StoredProcedures/sp_visual_employee_by_number_refresh")]
    public void EveryArtifactTheShapeNeeds_ShipsItsCreateAndItsRollback(string relativeFolder)
    {
        var folder = RepoPath(relativeFolder.Split('/'));

        Assert.IsTrue(File.Exists(Path.Combine(folder, "create.sql")), $"{relativeFolder} has no create.sql");
        Assert.IsTrue(File.Exists(Path.Combine(folder, "rollback.sql")), $"{relativeFolder} has no rollback.sql");
    }

    [TestMethod]
    public void TheShapeNamesItsLiveReadAndItsPopulationScript()
    {
        var shape = VisualReadShapeCatalog.Create().Single(s => s.Key == ShapeKey);

        StringAssert.Contains(shape.SourceScriptRelativePath, "Module_Startup");
        StringAssert.Contains(shape.PopulationScriptRelativePath, "employee_by_number_population.sql");
        Assert.IsTrue(File.Exists(RepoPath(shape.SourceScriptRelativePath.Split('/'))));
        Assert.IsTrue(File.Exists(RepoPath(shape.PopulationScriptRelativePath.Split('/'))));
    }

    /// <summary>The column aliases a script's projection answers, in the order the script writes them.</summary>
    private static string[] AliasesIn(string script)
    {
        var aliases = new List<string>();
        var inProjection = false;

        foreach (var rawLine in script.Split('\n'))
        {
            var line = rawLine.Trim();

            if (line.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                inProjection = true;

                continue;
            }

            if (!inProjection)
            {
                continue;
            }

            if (line.StartsWith("FROM", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("WHERE", StringComparison.OrdinalIgnoreCase))
            {
                inProjection = false;

                continue;
            }

            foreach (var alias in ExpectedColumns)
            {
                if (line.EndsWith($"AS {alias}", StringComparison.Ordinal)
                    || line.EndsWith($"AS {alias},", StringComparison.Ordinal))
                {
                    aliases.Add(alias);
                }
            }
        }

        return [.. aliases];
    }

    private static string RepoPath(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MTM_Waitlist.sln")))
        {
            directory = directory.Parent;
        }

        Assert.IsNotNull(directory, "the repository root could not be found from the test output directory");

        return Path.Combine([directory!.FullName, .. parts]);
    }
}
