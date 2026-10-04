using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MTM_Waitlist.Tests.Module_Mock;

/// <summary>
/// The registration gate for the shipped database artifacts: every artifact that ships a <c>create.sql</c>
/// must also appear in the aggregate an installer actually applies.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this exists.</b> The local installer applies the aggregates (<c>Tables/AllTables.sql</c>,
/// <c>StoredProcedures/AllSPs.sql</c>, <c>Functions/AllFunct.sql</c>, <c>Views/AllViews.sql</c>,
/// <c>Seeds/AllSeeds.sql</c>), never the per-artifact folders. An artifact that is written but never
/// registered therefore exists in the repository, satisfies every parity test that reads it from disk, and is
/// invisible on every installed machine.
/// </para>
/// <para>
/// <b>The failure it was written for.</b> Feature 011 shipped
/// <c>sp_core_account_setup_create</c> with its create and rollback scripts and never added it to
/// <c>StoredProcedures/AllSPs.sql</c>, so self set-up failed on click with
/// <c>PROCEDURE mtm_waitlist.sp_core_account_setup_create does not exist</c> while the artifact sat in the
/// tree. The same omission in the desktop project's content list and in the service's deployment manifest was
/// found in the same session, so this gate covers every aggregate the deploy path reads.
/// </para>
/// <para>
/// <b>Identified by the artifact's own first line.</b> Every artifact begins by naming the object it creates
/// ("Create table: core_users_profiles", "Stored Procedure: sp_core_account_setup_create", "Seed: Image Storage
/// Configuration Overrides"), so presence is judged by that line. A renamed or rewritten artifact whose
/// aggregate has not been regenerated is reported the same way, and a seed's folder name — which never appears
/// in the aggregate — cannot produce a false hit.
/// </para>
/// </remarks>
[TestClass]
public sealed class DatabaseAggregateRegistrationTests
{
    /// <summary>Every aggregate and the folder of artifacts that is concatenated into it.</summary>
    private static readonly (string Aggregate, string ArtifactFolder)[] s_aggregates =
    [
        ("Database/Tables/AllTables.sql", "Database/Tables"),
        ("Database/StoredProcedures/AllSPs.sql", "Database/StoredProcedures"),
        ("Database/Functions/AllFunct.sql", "Database/Functions"),
        ("Database/Views/AllViews.sql", "Database/Views"),
        ("Database/Seeds/AllSeeds.sql", "Database/Seeds"),
        ("Database/Mock/AllTables.sql", "Database/Mock/Tables"),
        ("Database/Mock/AllSPs.sql", "Database/Mock/StoredProcedures"),
        ("Database/Mock/AllSeeds.sql", "Database/Mock/Seeds"),
    ];

    [TestMethod]
    public void EveryArtifactWithACreateScript_IsRegisteredInItsAggregate()
    {
        var repositoryRoot = RepositoryPatternScan.FindRepositoryRoot();
        var violations = new List<string>();
        var examined = 0;

        foreach (var (aggregateRelativePath, artifactFolderRelativePath) in s_aggregates)
        {
            var aggregatePath = Path.Combine([repositoryRoot, .. aggregateRelativePath.Split('/')]);
            var artifactFolder = Path.Combine([repositoryRoot, .. artifactFolderRelativePath.Split('/')]);

            if (!File.Exists(aggregatePath) || !Directory.Exists(artifactFolder))
            {
                continue;
            }

            var aggregate = File.ReadAllText(aggregatePath);

            foreach (var artifact in Directory.EnumerateDirectories(artifactFolder))
            {
                var createScript = Path.Combine(artifact, "create.sql");
                if (!File.Exists(createScript))
                {
                    // A rollback-only artifact has nothing to apply, so it has nothing to register.
                    continue;
                }

                examined++;

                var declaration = FirstNonBlankLine(createScript);
                if (!aggregate.Contains(declaration, StringComparison.Ordinal))
                {
                    violations.Add(
                        $"{artifactFolderRelativePath}/{Path.GetFileName(artifact)} is not registered in "
                        + $"{aggregateRelativePath} (expected to find \"{declaration}\"). Add its create.sql to "
                        + "the aggregate, or the installer will never apply it.");
                }
            }
        }

        Assert.AreNotEqual(
            0,
            examined,
            "No artifacts were examined, so this gate would have passed without checking anything.");

        Assert.AreEqual(
            0,
            violations.Count,
            "An artifact that ships a create.sql must also be registered in the aggregate its installer reads."
            + Environment.NewLine
            + string.Join(Environment.NewLine, violations));
    }

    /// <summary>The first line of a script that carries any text — the line naming what it creates.</summary>
    private static string FirstNonBlankLine(string path) =>
        File.ReadLines(path).First(line => line.Trim().Length > 0).Trim();
}
