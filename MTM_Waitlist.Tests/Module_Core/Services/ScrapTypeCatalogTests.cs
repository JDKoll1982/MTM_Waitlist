using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Core.Services;

/// <summary>
/// The one catalogue the Setup picker and the lugger arrangement editor both read: it spells the shipped types
/// the way the seed and the setup list spell them, and it never offers either decision word (FR-010, FR-011).
/// </summary>
[TestClass]
public sealed class ScrapTypeCatalogTests
{
    [TestMethod]
    public void Service_ScrapTypeCatalogTests_Catalogue_HoldsTheShippedTypesInTheOrderThePickersListThem()
    {
        CollectionAssert.AreEqual(
            new[]
            {
                "3003 Aluminum",
                "5052 aluminum",
                "Galvanized Steel",
                "Steel",
                "Skeleton",
                "Gaylord",
            },
            ScrapTypeCatalog.All.ToArray());
    }

    [TestMethod]
    public void Service_ScrapTypeCatalogTests_Catalogue_Spells5052TheWayTheSetupListAlreadyDid()
    {
        // The setup workflow spells this one in lower case, and the arrangement's pinned assignment is
        // `5052 Aluminum`. Case-insensitive matching is what makes them the same scrap type, so the catalogue
        // keeps the setup list's spelling rather than silently re-casing a plant word (FR-009, research D5).
        Assert.IsTrue(ScrapTypeCatalog.All.Contains("5052 aluminum", StringComparer.Ordinal));
        Assert.IsTrue(ScrapTypeCatalog.Contains("5052 Aluminum"), "Matching ignores case, so either spelling is the type.");
        Assert.IsTrue(ScrapTypeCatalog.Contains("  5052 ALUMINUM  "), "Surrounding spaces are ignored too.");
    }

    [TestMethod]
    public void Service_ScrapTypeCatalogTests_Catalogue_SpellsEverySeedAssignmentTheSameWay()
    {
        var seed = File.ReadAllText(Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Database", "Seeds", "seed_waitlist_lugger_arrangement_defaults", "create.sql"));

        foreach (var scrapType in ScrapTypeCatalog.All)
        {
            Assert.IsTrue(
                seed.Contains($"'{scrapType}'", StringComparison.OrdinalIgnoreCase),
                $"The seed assigns no lugger to '{scrapType}', so the catalogue offers a type the defaults do not use.");
        }
    }

    [TestMethod]
    public void Service_ScrapTypeCatalogTests_Catalogue_NeverOffersEitherDecisionWord()
    {
        foreach (var value in new[] { ScrapDecisionRules.RequiredPlaceholder, ScrapDecisionRules.NoScrap })
        {
            Assert.IsFalse(
                ScrapTypeCatalog.All.Contains(value, StringComparer.OrdinalIgnoreCase),
                $"'{value}' decides whether there is scrap; it is not a scrap type and may never be assigned (FR-010).");
            Assert.IsFalse(ScrapTypeCatalog.Contains(value));
        }
    }
}
