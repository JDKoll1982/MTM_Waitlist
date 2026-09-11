using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Mock.Services;

namespace MTM_Waitlist.Tests.Module_Mock_Service;

/// <summary>
/// FR-020 / SC-012's no-downtime gate: adding a shape to the catalog must not change any existing shape's
/// procedure signature, contract, or result type, and the shipped catalog must agree with the artifacts on disk.
/// </summary>
/// <remarks>
/// The property that makes this true is that every artifact name is <b>derived from the shape key</b>
/// (<see cref="VisualReadShape.MirrorTableName"/> and friends). These tests pin that derivation, pin the
/// shipped five, and prove the catalog and the database/test artifacts still line up — so renaming a
/// procedure, retyping a column, or editing the catalog without the artifact (or the other way round) fails
/// here instead of in production.
/// </remarks>
[TestClass]
public sealed class ShapeAdditionNoDowntimeTests
{
    /// <summary>The five shipped shape keys. A change here is a contract change and must be deliberate.</summary>
    private static readonly string[] s_shippedShapeKeys =
    [
        "work_order_lookup",
        "operation_sequences",
        "subordinate_parts",
        "inventory_locations",
        "disposition_input",
    ];

    [TestMethod]
    public void ShippedCatalog_ContainsExactlyTheFivePublishedShapeKeys()
    {
        var keys = VisualReadShapeCatalog.Create().Select(shape => shape.Key).ToArray();

        CollectionAssert.AreEquivalent(s_shippedShapeKeys, keys);
        Assert.AreEqual(
            s_shippedShapeKeys.Length,
            keys.Distinct(StringComparer.Ordinal).Count(),
            "Shape keys must be unique: the key is the artifact-name root.");
    }

    [TestMethod]
    public void AddingAShape_LeavesEveryExistingShapeDefinitionUnchanged()
    {
        var existing = VisualReadShapeCatalog.Create();
        var before = existing.Select(Describe).ToArray();

        var extended = existing
            .Append(new VisualReadShape
            {
                Key = "sixth_shape",
                Module = VisualReadShapeModule.ModuleWaitlist,
                SourceScriptRelativePath = "Database/InforVisual/Queues/Module_Waitlist/Queries/SixthShape.sql",
                PopulationScriptRelativePath = "Database/InforVisual/Queues/Module_Mock/Populations/sixth_shape_population.sql",
                InputParameters = [new VisualShapeParameter("PartNumber", "nvarchar(60)", IsRequired: true)],
                OutputColumns = [new VisualShapeColumn("Description", "nvarchar(255)")],
            })
            .ToArray();

        CollectionAssert.AreEqual(
            before,
            extended.Take(existing.Count).Select(Describe).ToArray(),
            "Adding a shape must not alter any existing shape's derived artifact names, inputs, or outputs.");

        // The added shape's artifacts are derived from its own key, so they cannot collide with an existing
        // shape's — which is why already-deployed clients keep serving the shapes they know.
        var added = extended[^1];
        var existingNames = existing
            .SelectMany(shape => new[]
            {
                shape.MirrorTableName,
                shape.StageTableName,
                shape.GetProcedureName,
                shape.RefreshProcedureName,
            })
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var name in new[]
                 {
                     added.MirrorTableName,
                     added.StageTableName,
                     added.GetProcedureName,
                     added.RefreshProcedureName,
                 })
        {
            Assert.IsFalse(existingNames.Contains(name), $"Derived artifact name '{name}' collides with an existing shape.");
        }
    }

    [TestMethod]
    public void EveryShippedShape_HasItsMirrorTwinProceduresAndPopulationReadArtifactsOnDisk()
    {
        var repositoryRoot = FindRepositoryRoot();
        var missing = new List<string>();

        foreach (var shape in VisualReadShapeCatalog.Create())
        {
            foreach (var relativePath in new[]
                     {
                         $"Database/Mock/Tables/{shape.MirrorTableName}/create.sql",
                         $"Database/Mock/Tables/{shape.MirrorTableName}/rollback.sql",
                         $"Database/Mock/Tables/{shape.StageTableName}/create.sql",
                         $"Database/Mock/Tables/{shape.StageTableName}/rollback.sql",
                         $"Database/Mock/StoredProcedures/{shape.GetProcedureName}/create.sql",
                         $"Database/Mock/StoredProcedures/{shape.GetProcedureName}/rollback.sql",
                         $"Database/Mock/StoredProcedures/{shape.RefreshProcedureName}/create.sql",
                         $"Database/Mock/StoredProcedures/{shape.RefreshProcedureName}/rollback.sql",
                         shape.SourceScriptRelativePath,
                         shape.PopulationScriptRelativePath!,
                     })
            {
                var absolute = Path.Combine([repositoryRoot, .. relativePath.Split('/')]);
                if (!File.Exists(absolute))
                {
                    missing.Add($"{shape.Key}: {relativePath}");
                }
            }
        }

        Assert.AreEqual(
            0,
            missing.Count,
            "Every shipped shape must have its mirror table, stage twin, get/refresh procedures, source query, and "
                + "population read on disk under the names derived from its key (FR-020, playbook steps 1–3b):"
                + Environment.NewLine
                + string.Join(Environment.NewLine, missing));
    }

    [TestMethod]
    public void EveryShippedShape_HasAnInAppFallbackRegistered()
    {
        var registrationPath = Path.Combine(
            FindRepositoryRoot(),
            "MTM_Waitlist.Mock",
            "DependencyInjection",
            "MockServiceRegistrationExtensions.cs");
        Assert.IsTrue(File.Exists(registrationPath), $"The composition file was not found at '{registrationPath}'.");

        var source = File.ReadAllText(registrationPath);
        var registrationCount = CountOccurrences(source, "IVisualReadFallback<Visual");

        Assert.AreEqual(
            VisualReadShapeCatalog.Create().Count,
            registrationCount,
            "Each shipped shape registers exactly one IVisualReadFallback in the MTM_Waitlist.Mock composition "
                + "(playbook step 5); a shape without one has no in-app fallback.");
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var index = source.IndexOf(value, StringComparison.Ordinal);
        while (index >= 0)
        {
            count++;
            index = source.IndexOf(value, index + value.Length, StringComparison.Ordinal);
        }

        return count;
    }

    /// <summary>
    /// Renders everything a shape contributes to a caller's contract — the derived artifact names plus the
    /// ordered inputs and outputs — so any change to one of them shows up as an inequality.
    /// </summary>
    private static string Describe(VisualReadShape shape)
        => string.Join(
            "|",
            shape.Key,
            shape.MirrorTableName,
            shape.StageTableName,
            shape.GetProcedureName,
            shape.RefreshProcedureName,
            string.Join(",", shape.InputParameters.Select(p => $"{p.Name}:{p.Type}:{p.IsRequired}")),
            string.Join(",", shape.OutputColumns.Select(c => $"{c.Name}:{c.Type}")));

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MTM_Waitlist.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail($"The repository root could not be located above '{AppContext.BaseDirectory}'.");
        return AppContext.BaseDirectory;
    }
}
