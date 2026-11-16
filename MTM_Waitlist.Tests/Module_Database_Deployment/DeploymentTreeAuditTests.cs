using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Database_Deployment.Models;
using MTM_Database_Deployment.Services;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Database_Deployment;

/// <summary>
/// The deployed shape of everything the tool ships.
/// </summary>
/// <remarks>
/// These cases read the repository's real object tree rather than a sample, because the deploy tool's worst failure
/// is silent: a script it never discovered is simply absent from the database while the report reads clean, and a
/// body the parser cut in half is rejected at deploy time with the file on disk still looking perfect.
/// </remarks>
[TestClass]
public sealed class DeploymentTreeAuditTests
{
    private static readonly Lazy<Service_ObjectCatalog> s_catalog = new(() =>
        new Service_ObjectCatalog(Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), "Database")));

    private static readonly Lazy<IReadOnlyList<Model_SqlObject>> s_objects = new(() => s_catalog.Value.LoadObjects());

    [TestMethod]
    public void Audit_DeploymentTreeAuditTests_Catalog_FindsEveryShippedKind()
    {
        var kinds = s_objects.Value.Select(item => item.Kind).Distinct().OrderBy(kind => kind).ToArray();

        CollectionAssert.AreEquivalent(
            new[]
            {
                Enum_SqlObjectKind.Schema,
                Enum_SqlObjectKind.View,
                Enum_SqlObjectKind.Procedure,
                Enum_SqlObjectKind.Function,
                Enum_SqlObjectKind.Seed
            },
            kinds);
        Assert.IsTrue(s_objects.Value.Count > 140, $"only {s_objects.Value.Count} scripts were discovered");
    }

    [TestMethod]
    public void Audit_DeploymentTreeAuditTests_Catalog_DeploysTablesFirstAndSeedsLast()
    {
        var kinds = s_objects.Value.Select(item => item.Kind).ToArray();

        CollectionAssert.AreEqual(
            kinds.OrderBy(kind => kind).ToArray(),
            kinds,
            "the catalog must hand the engine one kind at a time, in deployment order");
        Assert.IsTrue(
            Array.LastIndexOf(kinds, Enum_SqlObjectKind.Schema) < Array.IndexOf(kinds, Enum_SqlObjectKind.Seed),
            "a seed would run before the last table exists");
    }

    [TestMethod]
    public void Audit_DeploymentTreeAuditTests_Catalog_EveryCreateScriptHasItsRollback()
    {
        var missing = s_objects.Value
            .Select(item => Path.GetDirectoryName(
                Path.Combine(s_catalog.Value.SqlRoot, item.RelativePath.Replace('/', Path.DirectorySeparatorChar)))
                ?? string.Empty)
            .Where(folder => !File.Exists(Path.Combine(folder, "rollback.sql")))
            .Select(folder => Path.GetRelativePath(s_catalog.Value.SqlRoot, folder))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.AreEqual(
            0,
            missing.Length,
            "every shipped object needs the rollback that undoes it: " + string.Join(", ", missing));
    }

    [TestMethod]
    public void Audit_DeploymentTreeAuditTests_Split_KeepsEveryLineOfEveryCreateScript()
    {
        var problems = new List<string>();

        foreach (var item in s_objects.Value)
        {
            var text = File.ReadAllText(item.FullPath);
            var statements = Service_SqlScriptParser.SplitStatements(text);
            if (statements.Count == 0)
            {
                problems.Add($"{item.RelativePath} holds no statement at all");
                continue;
            }

            var joined = string.Join("\n", statements);
            foreach (var line in text.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0
                    || trimmed.StartsWith("--", StringComparison.Ordinal)
                    || trimmed.StartsWith('#')
                    || trimmed.StartsWith("DELIMITER", StringComparison.OrdinalIgnoreCase)
                    || trimmed.StartsWith("USE ", StringComparison.OrdinalIgnoreCase)
                    || trimmed is "$$" or ";")
                {
                    continue;
                }

                if (!joined.Contains(trimmed, StringComparison.Ordinal)
                    && !joined.Contains(trimmed.TrimEnd('$', ';', ' '), StringComparison.Ordinal))
                {
                    problems.Add($"{item.RelativePath} drops this line when it splits: {trimmed}");
                    break;
                }
            }
        }

        Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
    }

    [TestMethod]
    public void Audit_DeploymentTreeAuditTests_Split_NeverEndsACompoundStatementMidBody()
    {
        var problems = new List<string>();

        foreach (var item in s_objects.Value)
        {
            foreach (var statement in Service_SqlScriptParser.SplitStatements(File.ReadAllText(item.FullPath)))
            {
                // The prose above a script talks about BEGIN and END too, so the body is judged without comments.
                var code = string.Join(
                    '\n',
                    statement.Split('\n').Where(line => !line.TrimStart().StartsWith("--", StringComparison.Ordinal)));

                if (!System.Text.RegularExpressions.Regex.IsMatch(
                        code,
                        @"\bBEGIN\b",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    continue;
                }

                if (!System.Text.RegularExpressions.Regex.IsMatch(
                        code,
                        @"\bEND(\s+(IF|LOOP|WHILE|CASE|REPEAT|FOR))?\s*$",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    problems.Add(
                        $"{item.RelativePath} hands the server a compound statement that does not close: "
                        + code[^Math.Min(80, code.Length)..].ReplaceLineEndings(" "));
                }
            }
        }

        Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
    }

    [TestMethod]
    public void Audit_DeploymentTreeAuditTests_Seed_NeverEmptiesATableBeforeFillingIt()
    {
        // A seed may delete a row it owns, because that delete finds nothing on a second run. What it may never do is
        // empty a whole table, which destroys rows an operator added.
        var offenders = s_objects.Value
            .Where(item => item.Kind == Enum_SqlObjectKind.Seed)
            .Where(item => ContainsCommand(File.ReadAllText(item.FullPath), "TRUNCATE"))
            .Select(item => item.RelativePath)
            .ToArray();

        Assert.AreEqual(
            0,
            offenders.Length,
            "a seed inserts only what is missing, so a second run changes nothing: " + string.Join(", ", offenders));
    }

    [TestMethod]
    public void Audit_DeploymentTreeAuditTests_View_EveryViewScriptReplacesWhatIsThere()
    {
        // Either spelling is idempotent: a drop before the create, or a create that replaces in place.
        var offenders = s_objects.Value
            .Where(item => item.Kind == Enum_SqlObjectKind.View)
            .Where(item =>
            {
                var script = File.ReadAllText(item.FullPath);
                return !System.Text.RegularExpressions.Regex.IsMatch(
                           script,
                           @"CREATE\s+OR\s+REPLACE\s+VIEW",
                           System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                       && !System.Text.RegularExpressions.Regex.IsMatch(
                           script,
                           @"DROP\s+VIEW\s+IF\s+EXISTS",
                           System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            })
            .Select(item => item.RelativePath)
            .ToArray();

        Assert.AreEqual(
            0,
            offenders.Length,
            "a view that is neither replaced nor dropped first never changes on a later run: "
            + string.Join(", ", offenders));
    }

    private static bool ContainsCommand(string script, string command) =>
        script
            .Split('\n')
            .Select(line => line.Trim())
            .Any(line =>
                !line.StartsWith("--", StringComparison.Ordinal)
                && line.Contains(command, StringComparison.OrdinalIgnoreCase));
}
