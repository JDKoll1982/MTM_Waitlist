using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Core.Services;

/// <summary>
/// The precedence order of the settings scopes, asserted against the function that actually ships rather than
/// against a copy of its answers (FR-053, decision 29).
/// </summary>
/// <remarks>
/// <para>
/// The rule the feature depends on is that a person's own stored value beats every wider scope, and the scope
/// ranked highest that applies is the one that wins. The function used to put <c>user</c> third, below
/// <c>admin</c> and <c>developer</c>, so a value stored for an individual lost to an inherited one. Reading the
/// shipped artifact is the point: a test that restated the numbers could agree with itself while the shipped
/// SQL said something else.
/// </para>
/// <para>
/// These assertions are the tie that keeps the re-rank ahead of the first per-person permission row: they fail if
/// somebody reorders the scopes without meaning to, and the ordering is what a person's own row resolves under.
/// </para>
/// </remarks>
[TestClass]
public sealed class SettingsScopeRankTests
{
    private const string ScopeRankFunctionPath = "Database/Functions/fn_config_settings_scope_rank/create.sql";

    [TestMethod]
    public void ShippedScopeRankFunction_DeclaresEveryScopeAtItsCorrectedRung()
    {
        var createScript = ReadScopeRankFunction();

        Assert.AreEqual(1, RungOf(createScript, "computer"), "computer must resolve first.");
        Assert.AreEqual(2, RungOf(createScript, "all_users"), "all_users is the widest scope and resolves next.");
        Assert.AreEqual(3, RungOf(createScript, "role"), "role is the new baseline scope.");
        Assert.AreEqual(4, RungOf(createScript, "admin"), "admin is a legacy broad scope.");
        Assert.AreEqual(5, RungOf(createScript, "developer"), "developer is a legacy broad scope.");
        Assert.AreEqual(6, RungOf(createScript, "user"), "a person's own value must win.");
    }

    [TestMethod]
    public void ShippedScopeRankFunction_PutsAPersonsOwnValueAboveEveryWiderScope()
    {
        var createScript = ReadScopeRankFunction();

        var userRung = RungOf(createScript, "user");

        Assert.IsTrue(userRung > RungOf(createScript, "role"), "a person's own row must beat their role's baseline.");
        Assert.IsTrue(userRung > RungOf(createScript, "admin"), "a person's own row must beat the admin scope.");
        Assert.IsTrue(userRung > RungOf(createScript, "developer"), "a person's own row must beat the developer scope.");
        Assert.IsTrue(userRung > RungOf(createScript, "all_users"), "a person's own row must beat everyone.");
    }

    [TestMethod]
    public void ShippedScopeRankFunction_AnswersZeroForAScopeItDoesNotKnow()
    {
        var createScript = ReadScopeRankFunction();

        // Zero is the lowest rung, so an unrecognised scope loses to everything rather than silently winning.
        Assert.IsTrue(createScript.Contains("ELSE 0", StringComparison.Ordinal));
        Assert.AreEqual(0, RungOf(createScript, "nonsense_scope_that_is_not_in_the_case"));
    }

    /// <summary>
    /// The rung the shipped CASE expression gives <paramref name="scopeType"/>, or zero when no branch names it.
    /// </summary>
    private static int RungOf(string createScript, string scopeType)
    {
        var marker = $"WHEN '{scopeType}' THEN ";
        var index = createScript.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return 0;
        }

        var digits = new string(createScript[(index + marker.Length)..]
            .TakeWhile(char.IsAsciiDigit)
            .ToArray());

        return digits.Length == 0 ? 0 : int.Parse(digits, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string ReadScopeRankFunction()
    {
        var path = Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            ScopeRankFunctionPath.Replace('/', Path.DirectorySeparatorChar));

        Assert.IsTrue(File.Exists(path), $"The shipped scope-rank function is missing: {ScopeRankFunctionPath}");

        return File.ReadAllText(path);
    }
}
