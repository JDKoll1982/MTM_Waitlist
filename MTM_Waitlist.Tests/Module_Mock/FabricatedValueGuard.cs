using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MTM_Waitlist.Tests.Module_Mock;

/// <summary>
/// T004's plausible-value guard: the shape test that answers "does this rendered value look like a value
/// nobody could have sourced?".
/// </summary>
/// <remarks>
/// <para>
/// <b>Why shapes and not literals.</b> A check that only forbids the exact strings removed today cannot see
/// tomorrow's substitute. The guard therefore tests the <i>shapes</i> a fabricated material attribute takes
/// in this repository — a currency amount, a measured weight, a counted quantity, a dimension — plus the
/// absence-rendered-as-a-value wording that <c>data-model.md</c> §2 puts in the same removal inventory.
/// The tests for US1 and US2 assert every rendered field value is <i>not</i> fabricated-looking, so a
/// literal that reappears under a new spelling still fails.
/// </para>
/// <para>
/// <b>Deliberately not a general "does this look like a number" test.</b> Values that are legitimately
/// shaped like a bare number (an operation sequence, a quantity the requester actually typed, a request id)
/// are sourced and must pass; the guard only fires on values that carry an invented-looking unit or an
/// explicit absence placeholder.
/// </para>
/// <para>
/// This file declares the patterns, so <see cref="RepositoryScanScope"/>'s default file exclusions skip it.
/// </para>
/// </remarks>
internal static class FabricatedValueGuard
{
    /// <summary>The value shapes this guard treats as fabricated-looking, as regular expressions.</summary>
    internal static readonly (string Description, Regex Pattern)[] s_patterns =
    [
        ("currency amount", new Regex(@"[$£€]\s?\d", RegexOptions.Compiled | RegexOptions.CultureInvariant)),

        ("measured weight or mass", new Regex(
            @"\b\d[\d,]*(?:\.\d+)?\s*(?:lb|lbs|pounds?|kg|kgs|kilograms?|g|grams?)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),

        ("counted quantity", new Regex(
            @"\b\d[\d,]*(?:\.\d+)?\s*(?:pieces?|each|sheets?|containers?|bins?|skids?|pcs|units?)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),

        ("dimension", new Regex(
            @"\b\d+(?:\.\d+)?\s*[x×]\s*\d+(?:\.\d+)?\s*(?:in\b|in\.|inch|inches|""|mm\b|cm\b|ft\b)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),

        ("bare measurement", new Regex(
            @"\b\d+(?:\.\d+)?\s*(?:inch|inches|in\.|mm\b|cm\b|ft\b)\s*(?!\w)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),

        ("absence rendered as a value", new Regex(
            @"\bNot (?:provided|available)\b",
            RegexOptions.Compiled | RegexOptions.CultureInvariant)),
    ];

    /// <summary>Whether a rendered value looks fabricated rather than sourced.</summary>
    /// <param name="value">The rendered value; null or blank is not fabricated, it is simply absent.</param>
    /// <returns>True when the value matches one of the fabricated shapes.</returns>
    internal static bool IsFabricatedLooking(string? value)
        => !string.IsNullOrWhiteSpace(value) && s_patterns.Any(pattern => pattern.Pattern.IsMatch(value));

    /// <summary>Filters a set of rendered values down to the fabricated-looking ones.</summary>
    /// <param name="values">The rendered values to test.</param>
    /// <returns>The offending values, in order; empty when every value is plausible.</returns>
    internal static IReadOnlyList<string> FindFabricatedLooking(IEnumerable<string?> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        return [.. values.Where(value => IsFabricatedLooking(value)).Select(value => value!)];
    }
}

/// <summary>
/// T004's self-tests: the guard is green on an empty value set, and it is not vacuous — it fires on a
/// synthetic value of each shape it claims to cover.
/// </summary>
[TestClass]
public sealed class FabricatedValueGuardTests
{
    /// <summary>
    /// An empty set finds nothing, which is what every later caller asserts over a populated set.
    /// </summary>
    [TestMethod]
    public void FindFabricatedLooking_WithAnEmptyValueSet_ReturnsNothing()
    {
        var offenders = FabricatedValueGuard.FindFabricatedLooking([]);

        Assert.AreEqual(0, offenders.Count, $"The guard flagged values in an empty set: {string.Join(", ", offenders)}");
    }

    /// <summary>
    /// Absence is not fabrication: a null or blank value is what the surfaces should render instead of one.
    /// </summary>
    [TestMethod]
    public void IsFabricatedLooking_WithAnAbsentValue_IsFalse()
    {
        Assert.IsFalse(FabricatedValueGuard.IsFabricatedLooking(null));
        Assert.IsFalse(FabricatedValueGuard.IsFabricatedLooking(string.Empty));
        Assert.IsFalse(FabricatedValueGuard.IsFabricatedLooking("   "));
    }

    /// <summary>
    /// The guard is not vacuous: a synthetic value of each declared shape is caught. The samples are
    /// deliberately invented here and are not any value the repository has ever shipped.
    /// </summary>
    [TestMethod]
    public void IsFabricatedLooking_WithASyntheticValueOfEachShape_IsTrue()
    {
        string[] samples =
        [
            "$1,204.00",
            "123.5 lb",
            "17 pieces",
            "0.25 x 36 in",
            "9 inches",
            "Not provided",
            "Not available",
        ];

        foreach (var sample in samples)
        {
            Assert.IsTrue(
                FabricatedValueGuard.IsFabricatedLooking(sample),
                $"The guard failed to flag the sample '{sample}'.");
        }
    }

    /// <summary>
    /// Sourced values that merely contain a number must pass, or the guard would forbid truthful data.
    /// </summary>
    [TestMethod]
    public void IsFabricatedLooking_WithASourcedValue_IsFalse()
    {
        string[] sourced =
        [
            "Work Center 12",
            "Coil",
            "Wrong Coil",
            "Bring",
            "Jane Doe",
            "WO-072368",
            "3f7a1c62-2f0b-4d5e-8a91-6c2e4f7b9d10",
        ];

        foreach (var value in sourced)
        {
            Assert.IsFalse(
                FabricatedValueGuard.IsFabricatedLooking(value),
                $"The guard wrongly flagged the sourced value '{value}'.");
        }
    }
}
