using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Settings.Services;

/// <summary>
/// The shipped role ladder as Phase 3 leaves it: the eight rungs the seed writes, read from the seed file the
/// repository ships rather than from a copy of the numbers in this test (FR-015, FR-018).
/// </summary>
/// <remarks>
/// <para>
/// The retired <c>admin</c> row is deliberately left unranked by the seed, so it holds the column default of 0.
/// That is what makes the role rename's paired reversal determinate: the reversal re-inserts the row carrying
/// the rung it held when the migration removed it. This file asserts the row is written without a rank, which
/// is the fact the reversal's arithmetic rests on.
/// </para>
/// <para>
/// The nine-code assertion, with <c>admin</c> absent and <c>it_department</c> present, belongs to Phase 4 where
/// it is true. Here the catalogue still holds the retired role and does not yet hold IT Department.
/// </para>
/// </remarks>
[TestClass]
public sealed class RoleLadderSeedTests
{
    /// <summary>The eight rungs the shipped seed writes, as the specification pins them.</summary>
    private static readonly Dictionary<string, int> ExpectedLiveRungs = new(StringComparer.Ordinal)
    {
        ["developer"] = 100,
        ["plant_manager"] = 80,
        ["production_lead"] = 70,
        ["setup_lead"] = 60,
        ["material_handler_lead"] = 50,
        ["material_handler"] = 10,
        ["production"] = 10,
        ["setup"] = 10,
    };

    private const string RetiredRoleCode = "admin";

    private static string SeedPath => Path.Combine(
        RepositoryPatternScan.FindRepositoryRoot(),
        "Database",
        "Seeds",
        "seed_dev_masked_baseline",
        "create.sql");

    [TestMethod]
    public void TheShippedSeed_GivesEveryLiveRoleItHoldsTheDeclaredRung()
    {
        var ranked = RankedRoleRows(ReadSeed());

        CollectionAssert.AreEquivalent(
            ExpectedLiveRungs.Keys.ToArray(),
            ranked.Keys.ToArray(),
            "The seed must rank exactly the eight live roles: " + string.Join(", ", ranked.Keys.OrderBy(code => code, StringComparer.Ordinal)));

        foreach (var (code, rank) in ExpectedLiveRungs)
        {
            Assert.AreEqual(rank, ranked[code], $"{code} must be ranked {rank}.");
        }
    }

    [TestMethod]
    public void TheShippedSeed_AddsMaterialHandlerLeadAsARoleInItsOwnRight()
    {
        var seed = ReadSeed();

        StringAssert.Contains(seed, "'material_handler_lead',");
        StringAssert.Contains(seed, "'Material Handler Lead',");

        // Its own rung rather than Plant Manager's, which is what "not mapped onto Plant Manager" means.
        Assert.AreEqual(50, RankedRoleRows(seed)["material_handler_lead"]);
        Assert.AreNotEqual(RankedRoleRows(seed)["plant_manager"], RankedRoleRows(seed)["material_handler_lead"]);
    }

    [TestMethod]
    public void TheShippedSeed_WritesTheRetiredRoleWithoutARankSoItHoldsTheColumnDefault()
    {
        var seed = ReadSeed();

        StringAssert.Contains(seed, "'" + RetiredRoleCode + "',");

        // The retired role's own statement, which omits `role_rank` from its column list. A shared column list
        // could not omit the column for one row and supply it for the rest, which is why it is a second
        // statement, and why the value it will hold is the column's default rather than a written rung.
        var retiredStatement = Regex.Match(
            seed,
            @"INSERT INTO\s+auth_roles_catalog\s*\((?<columns>[^)]*)\)\s*VALUES\s*\((?<values>[^;]*?)'"
                + RetiredRoleCode
                + @"'(?<tail>[^;]*);",
            RegexOptions.Singleline);

        Assert.IsTrue(retiredStatement.Success, "The retired role's own statement must be in the seed.");

        var columns = retiredStatement.Groups["columns"].Value;
        var values = retiredStatement.Groups["values"].Value + retiredStatement.Groups["tail"].Value;

        Assert.IsFalse(columns.Contains("role_rank", StringComparison.Ordinal), "The retired role's statement must not name role_rank.");
        Assert.IsFalse(
            Regex.IsMatch(values, @"'?" + RetiredRoleCode + @"'?\s*,\s*\d+", RegexOptions.Singleline),
            "The retired role's statement must not write a rank beside the code.");

        // And no rank is written for it anywhere else in the seed either.
        Assert.IsFalse(
            RankedRoleRows(seed).ContainsKey(RetiredRoleCode),
            "The retired role must not appear as a ranked row.");
    }

    [TestMethod]
    public void TheShippedSeed_LeavesTheRenameEntirelyToItsOwnSeed()
    {
        // Comments may name the thing this seed deliberately does not do; only statements count.
        var statements = StatementsOnly(ReadSeed());

        // This seed does not insert IT Department, does not remove the retired role, and does not repoint the
        // masked account. That is what keeps the reversal exact, and it is checked here so a later edit to this
        // seed cannot quietly take over the rename.
        Assert.IsFalse(statements.Contains("it_department", StringComparison.Ordinal), "This seed must not name IT Department.");
        Assert.IsFalse(statements.Contains("IT Department", StringComparison.Ordinal), "This seed must not name IT Department.");
        Assert.IsFalse(
            Regex.IsMatch(statements, @"DELETE\s+FROM\s+auth_roles_catalog", RegexOptions.IgnoreCase),
            "This seed must not remove a catalogue row.");
        Assert.IsFalse(
            Regex.IsMatch(statements, @"role_code\s*=\s*'it_department'", RegexOptions.IgnoreCase),
            "This seed must not repoint an assignment to IT Department.");
    }

    [TestMethod]
    public void TheShippedSeed_LeavesRoomBetweenItsRungs()
    {
        var ranks = ExpectedLiveRungs.Values.Distinct().OrderBy(rank => rank).ToArray();

        for (var i = 1; i < ranks.Length; i++)
        {
            Assert.IsTrue(
                ranks[i] - ranks[i - 1] >= 10,
                $"There is no room to insert a role between {ranks[i - 1]} and {ranks[i]} without renumbering the others (FR-018).");
        }
    }

    [TestMethod]
    public void TheShippedSeed_LeavesTheWorkerRolesAtOneRung()
    {
        var ranked = RankedRoleRows(ReadSeed());
        var workerRanks = new[] { "material_handler", "production", "setup" }
            .Select(code => ranked[code])
            .Distinct()
            .ToArray();

        Assert.AreEqual(1, workerRanks.Length, "The three worker roles must share one rung.");
    }

    /// <summary>
    /// The ranked role rows the seed writes, keyed by role code. Parsed from the catalogue's own INSERT and from
    /// nowhere else, so a rank written to another table cannot be mistaken for a rung. Only rows that carry a
    /// rank are returned: the retired role's row is written without one, which is the point of its own statement.
    /// </summary>
    private static Dictionary<string, int> RankedRoleRows(string seed)
    {
        var rows = new Dictionary<string, int>(StringComparer.Ordinal);

        // The first catalogue INSERT and everything up to its terminating semicolon.
        var blockStart = Regex.Match(seed, @"INSERT\s+INTO\s+auth_roles_catalog\s*\(", RegexOptions.IgnoreCase);
        Assert.IsTrue(blockStart.Success, "The seed must write the role catalogue.");

        var blockEnd = seed.IndexOf(';', blockStart.Index);
        Assert.IsTrue(blockEnd > blockStart.Index, "The catalogue INSERT must terminate.");

        var block = seed[blockStart.Index..blockEnd];

        foreach (Match match in Regex.Matches(block, @"'([a-z_]+)',\s*\r?\n\s*'[^']*',\s*\r?\n\s*(\d+),"))
        {
            rows[match.Groups[1].Value] = int.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
        }

        return rows;
    }

    /// <summary>The seed's statements with its comment lines removed.</summary>
    private static string StatementsOnly(string seed) =>
        string.Join(
            Environment.NewLine,
            seed.Split('\n').Where(line => !line.TrimStart().StartsWith("--", StringComparison.Ordinal)));

    private static string ReadSeed()
    {
        Assert.IsTrue(File.Exists(SeedPath), $"The shipped seed is missing: {SeedPath}");

        return File.ReadAllText(SeedPath);
    }
}
