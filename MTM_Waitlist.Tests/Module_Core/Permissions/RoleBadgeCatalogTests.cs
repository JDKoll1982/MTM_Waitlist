using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Permissions;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Core.Permissions;

/// <summary>
/// The badge lookup: keyed on the role code, one entry per role the catalogue holds, the grey default reachable
/// only by a genuinely unknown or blank role (FR-105, FR-106, FR-107, SC-015, SC-020).
/// </summary>
/// <remarks>
/// <para>
/// <b>The roles come from the shipped seeds, not from a list in this file.</b> The baseline seed ranks the eight
/// live roles and the rename seed inserts IT Department, so reading both is how "every role the catalogue holds"
/// is answered from the data rather than restated here. A rung added to a seed without a badge fails this file.
/// </para>
/// <para>
/// <b>The count of roles reaching the grey default is asserted as zero</b>, because that is the defect this
/// change removes: five of the eight roles fell to the grey default while the lookup recognised three.
/// </para>
/// </remarks>
[TestClass]
public sealed class RoleBadgeCatalogTests
{
    /// <summary>The nine codes the catalogue holds, as the contract pins them.</summary>
    private static readonly string[] PinnedCatalogueCodes =
    {
        "developer", "it_department", "plant_manager", "production_lead", "setup_lead",
        "material_handler_lead", "material_handler", "production", "setup",
    };

    /// <summary>
    /// The vocabulary that names roles the catalogue has never held. These must reach the grey default because
    /// the branches that named them are gone (FR-107), not because they were quietly re-pointed at a real role.
    /// </summary>
    private static readonly string[] RetiredVocabulary =
        { "admin", "administrator", "supervisor", "manager", "quality", "quality inspector", "Setup Tech", "Admin" };

    [TestMethod]
    public void All_HoldsOneEntryPerRoleTheCatalogueHolds()
    {
        var catalogueCodes = ShippedCatalogueCodes();

        CollectionAssert.AreEquivalent(
            catalogueCodes,
            RoleBadgeCatalog.All.Select(entry => entry.RoleCode).ToArray(),
            "The lookup must hold exactly one entry per role the catalogue holds.");

        foreach (var code in catalogueCodes)
        {
            Assert.AreNotEqual(
                RoleBadgeCatalog.Fallback,
                RoleBadgeCatalog.For(code),
                $"'{code}' must have its own badge rather than reaching the grey default.");
        }
    }

    [TestMethod]
    public void All_LeavesNoRoleReachingTheGreyDefault()
    {
        var fallingToTheDefault = RoleBadgeCatalog.All
            .Where(entry => string.Equals(entry.ColorHex, RoleBadgeCatalog.Fallback.ColorHex, StringComparison.Ordinal)
                && string.Equals(entry.Glyph, RoleBadgeCatalog.Fallback.Glyph, StringComparison.Ordinal))
            .Select(entry => entry.RoleCode)
            .ToArray();

        Assert.AreEqual(
            0,
            fallingToTheDefault.Length,
            "The count of roles reaching the grey default must be zero (SC-015): " + string.Join(", ", fallingToTheDefault));
    }

    [TestMethod]
    public void All_GivesEachRoleItsOwnGlyphAndItsOwnColour()
    {
        Assert.AreEqual(
            RoleBadgeCatalog.All.Count,
            RoleBadgeCatalog.All.Select(entry => entry.Glyph).Distinct(StringComparer.Ordinal).Count(),
            "Each role's badge must be distinguishable from every other role's.");
        Assert.AreEqual(
            RoleBadgeCatalog.All.Count,
            RoleBadgeCatalog.All.Select(entry => entry.ColorHex).Distinct(StringComparer.Ordinal).Count(),
            "Each role's badge must be distinguishable from every other role's.");
    }

    [TestMethod]
    public void All_HoldsNoCodeOutsideTheCatalogue()
    {
        foreach (var entry in RoleBadgeCatalog.All)
        {
            CollectionAssert.Contains(
                PinnedCatalogueCodes,
                entry.RoleCode,
                $"'{entry.RoleCode}' is not a role the catalogue holds, so it must not have a branch of its own.");
        }
    }

    [DataTestMethod]
    [DataRow(null, DisplayName = "no role at all")]
    [DataRow("", DisplayName = "an empty role")]
    [DataRow("   ", DisplayName = "whitespace only")]
    public void For_ABlankRole_ReturnsTheGreyDefault(string? roleCode)
    {
        Assert.AreEqual(RoleBadgeCatalog.Fallback, RoleBadgeCatalog.For(roleCode));
    }

    [DataTestMethod]
    [DataRow("supervisor")]
    [DataRow("quality inspector")]
    [DataRow("Setup Tech")]
    [DataRow("zz-not-a-role")]
    public void For_ARoleTheCatalogueHasNeverHeld_ReturnsTheGreyDefault(string roleCode)
    {
        // Not because the vocabulary was re-pointed at a real role: the lookup holds no branch for any of these.
        Assert.AreEqual(RoleBadgeCatalog.Fallback, RoleBadgeCatalog.For(roleCode));
        Assert.IsFalse(
            RoleBadgeCatalog.All.Any(entry => string.Equals(entry.RoleCode, roleCode, StringComparison.OrdinalIgnoreCase)),
            $"'{roleCode}' must not have an entry in the lookup.");
    }

    [DataTestMethod]
    [DataRow("administrator")]
    [DataRow("admin")]
    [DataRow("Admin")]
    public void For_TheRetiredAdministratorVocabulary_ReturnsTheGreyDefault(string roleCode)
    {
        // IT Department carries the badge the administrator role used to carry, and the retired spellings reach
        // nothing. Anything else would let a retired name keep a live badge.
        Assert.AreEqual(RoleBadgeCatalog.Fallback, RoleBadgeCatalog.For(roleCode));
    }

    [TestMethod]
    public void For_IsKeyedOnTheRoleCodeAndNotOnTheDisplayName()
    {
        StringAssert.Contains(
            RoleBadgeCatalog.For("plant_manager").RoleCode,
            "plant_manager",
            "The role code must reach its badge.");

        Assert.AreEqual(
            RoleBadgeCatalog.Fallback,
            RoleBadgeCatalog.For("Plant Manager"),
            "A display name is presentation: renaming a role's displayed name must not decide its badge.");
    }

    [TestMethod]
    public void For_AcceptsTheCodeInAnyCaseOrWithSurroundingSpace()
    {
        // Codes arrive from the store and from typed input; case and padding are not part of the identity.
        Assert.AreEqual(RoleBadgeCatalog.For("developer"), RoleBadgeCatalog.For("  DEVELOPER "));
    }

    [TestMethod]
    public void TheBadge_IsNotAPermission()
    {
        Assert.IsFalse(
            PermissionRegistry.All.Any(entry => entry.Key.Contains("badge", StringComparison.OrdinalIgnoreCase)),
            "The badge must not become a permission: it decides how a person looks, not what they may do (FR-058).");

        // And no gate reads it, so it cannot appear on the permissions page through a gate site either.
        Assert.IsFalse(
            PermissionRegistry.All
                .SelectMany(entry => entry.GateSites)
                .Any(site => site.Contains("Badge", StringComparison.OrdinalIgnoreCase)),
            "No permission may name the badge as a gate site (FR-058).");
    }

    /// <summary>
    /// Every role code the shipped catalogue seeds write: the eight the baseline seed ranks, plus the role the
    /// rename seed inserts.
    /// </summary>
    private static string[] ShippedCatalogueCodes()
    {
        var root = RepositoryPatternScan.FindRepositoryRoot();

        var baselineSeed = File.ReadAllText(Path.Combine(
            root, "Database", "Seeds", "seed_dev_masked_baseline", "create.sql"));

        var blockStart = Regex.Match(baselineSeed, @"INSERT\s+INTO\s+auth_roles_catalog\s*\(", RegexOptions.IgnoreCase);
        Assert.IsTrue(blockStart.Success, "The baseline seed must write the role catalogue.");

        var blockEnd = baselineSeed.IndexOf(';', blockStart.Index);
        Assert.IsTrue(blockEnd > blockStart.Index, "The catalogue INSERT must terminate.");

        var ranked = Regex
            .Matches(baselineSeed[blockStart.Index..blockEnd], @"'([a-z_]+)',\s*\r?\n\s*'[^']*',\s*\r?\n\s*(\d+),")
            .Select(match => match.Groups[1].Value);

        var renameSeed = File.ReadAllText(Path.Combine(
            root, "Database", "Seeds", "seed_role_admin_to_it_department", "create.sql"));

        var renamed = Regex
            .Matches(renameSeed, @"'it_department',\s*\r?\n\s*'IT Department',\s*\r?\n\s*(\d+),")
            .Select(_ => "it_department");

        var codes = ranked.Concat(renamed).Distinct(StringComparer.Ordinal).OrderBy(code => code, StringComparer.Ordinal).ToArray();

        CollectionAssert.AreEquivalent(
            PinnedCatalogueCodes,
            codes,
            "The shipped seeds must write exactly the nine pinned role codes; this check is what makes the coverage assertion above meaningful.");

        return codes;
    }
}
