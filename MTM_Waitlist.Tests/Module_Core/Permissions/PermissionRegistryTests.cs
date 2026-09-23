using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Permissions;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Core.Permissions;

/// <summary>
/// The declaration itself: every key spelled as the specification pins it, every key read by a named gate site,
/// and every key carrying the label and what-it-gates strings it points at (FR-045, FR-046, FR-047, FR-061,
/// FR-062).
/// </summary>
/// <remarks>
/// The two-direction check against the seeded baselines lives in
/// <c>Module_Settings/Services/PermissionBaselineParityTests</c>, because the baselines are seeded data that does
/// not exist until the seed that writes them does. Everything asserted here is provable from the declaration and
/// the resource file alone.
/// </remarks>
[TestClass]
public sealed class PermissionRegistryTests
{
    /// <summary>
    /// The fifteen keys, spelled exactly as the specification's Verbatim Constraints section pins them. Spelled
    /// out here on purpose: a test that read the list from the declaration could not catch a key the declaration
    /// misspells.
    /// </summary>
    private static readonly string[] PinnedKeys =
    {
        "permission.requests.handle",
        "permission.cache.refresh_api",
        "permission.settings.ignored_locations",
        "permission.settings.hot_work_centers",
        "permission.settings.part_pictures",
        "permission.settings.cache_refresh",
        "permission.settings.urgency_minutes",
        "permission.settings.defect_types",
        "permission.settings.computers",
        "permission.settings.storage_paths",
        "permission.setup.dunnage_quick_add",
        "permission.setup.work_centers",
        "permission.admin.users",
        "permission.admin.reset_password",
        "permission.admin.permissions",
    };

    /// <summary>
    /// The three keys that replace no retired role list, with the baseline sets they ship to (SC-001).
    /// </summary>
    private static readonly string[] KeysWithNoPredecessor =
    {
        "permission.admin.users",
        "permission.admin.reset_password",
        "permission.admin.permissions",
    };

    [TestMethod]
    public void Declaration_HoldsEveryPinnedKeyExactlyOnce()
    {
        var keys = PermissionRegistry.All.Select(entry => entry.Key).ToArray();

        CollectionAssert.AreEquivalent(PinnedKeys, keys);
        Assert.AreEqual(PinnedKeys.Length, keys.Distinct(StringComparer.Ordinal).Count());
        Assert.AreEqual(PinnedKeys.Length, PermissionKeys.All.Count);
        CollectionAssert.AreEquivalent(PinnedKeys, PermissionKeys.All.ToArray());
    }

    [TestMethod]
    public void Declaration_SpellsEveryKeyExactlyAsTheSpecificationPinsIt()
    {
        foreach (var pinned in PinnedKeys)
        {
            Assert.IsTrue(
                PermissionRegistry.All.Any(entry => string.Equals(entry.Key, pinned, StringComparison.Ordinal)),
                $"The declaration does not hold '{pinned}' spelled exactly as the specification pins it.");
        }

        // No key is re-cased, pluralised or paraphrased: the pinned form is lower case with underscores inside a
        // segment, and the `permission.` namespace is what the store's write path refuses to break.
        foreach (var entry in PermissionRegistry.All)
        {
            Assert.IsTrue(entry.Key.StartsWith("permission.", StringComparison.Ordinal), entry.Key);
            Assert.AreEqual(entry.Key.ToLowerInvariant(), entry.Key, entry.Key);
        }
    }

    [TestMethod]
    public void Declaration_GivesEveryKeyANamedGateSiteThatReadsIt()
    {
        // A declared key nothing reads would be a permission that decides nothing (FR-062).
        foreach (var entry in PermissionRegistry.All)
        {
            Assert.IsTrue(entry.GateSites.Count > 0, $"'{entry.Key}' names no gate site.");
            foreach (var gateSite in entry.GateSites)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(gateSite), $"'{entry.Key}' names a blank gate site.");
                Assert.IsFalse(gateSite.Contains('/', StringComparison.Ordinal), $"'{entry.Key}' names a path rather than a type: {gateSite}");
                Assert.IsFalse(gateSite.Contains('.', StringComparison.Ordinal), $"'{entry.Key}' names a qualified name rather than a type: {gateSite}");
            }
        }
    }

    [TestMethod]
    public void Declaration_NamesTheResetActionAsAReaderOfTheResetKey()
    {
        // permission.admin.reset_password exists only to gate the reset action on a person's page, so that action
        // has to be one of its named readers or the key is declared without a reader.
        var resetEntry = PermissionRegistry.Find(PermissionKeys.AdminResetPassword);

        Assert.IsNotNull(resetEntry);
        CollectionAssert.Contains(resetEntry.GateSites.ToArray(), "EditUserViewModel");
    }

    [TestMethod]
    public void Declaration_HoldsTheThreeKeysThatReplaceNoRetiredList()
    {
        foreach (var key in KeysWithNoPredecessor)
        {
            Assert.IsTrue(PermissionRegistry.IsDeclared(key), $"The declaration must hold '{key}'.");
        }
    }

    [TestMethod]
    public void Declaration_PutsEveryKeyInOneOfTheFiveAreas()
    {
        var areas = PermissionRegistry.All.Select(entry => entry.BelongsTo).Distinct().ToArray();

        CollectionAssert.AreEquivalent(
            new[]
            {
                PermissionRegistry.Area.Requests,
                PermissionRegistry.Area.Cache,
                PermissionRegistry.Area.Settings,
                PermissionRegistry.Area.Setup,
                PermissionRegistry.Area.Administration,
            },
            areas);
    }

    [TestMethod]
    public void Declaration_DerivesEachKeysResourceNamesFromTheKeyItself()
    {
        foreach (var entry in PermissionRegistry.All)
        {
            var suffix = entry.Key["permission.".Length..].Replace('.', '_');

            Assert.AreEqual($"Permission_{suffix}.Label", entry.LabelResourceKey, entry.Key);
            Assert.AreEqual($"Permission_{suffix}.Gates", entry.GatesResourceKey, entry.Key);
        }

        // No two labels share a resource key, and neither do the sentences.
        Assert.AreEqual(PinnedKeys.Length, PermissionRegistry.All.Select(entry => entry.LabelResourceKey).Distinct(StringComparer.Ordinal).Count());
        Assert.AreEqual(PinnedKeys.Length, PermissionRegistry.All.Select(entry => entry.GatesResourceKey).Distinct(StringComparer.Ordinal).Count());
    }

    [TestMethod]
    public void Declaration_HasAStringForEveryKeyInTheResourceFile()
    {
        var resourceKeys = ReadResourceKeys();

        foreach (var entry in PermissionRegistry.All)
        {
            Assert.IsTrue(resourceKeys.Contains(entry.LabelResourceKey), $"'{entry.LabelResourceKey}' is missing from the resource file.");
            Assert.IsTrue(resourceKeys.Contains(entry.GatesResourceKey), $"'{entry.GatesResourceKey}' is missing from the resource file.");
        }
    }

    [TestMethod]
    public void Declaration_NamesARoleForEveryRoleTheCatalogueWillHold()
    {
        // The nine codes the specification pins, each with its own display text, keyed on the code.
        var resourceKeys = ReadResourceKeys();
        var pinnedCodes = new[]
        {
            "developer",
            "it_department",
            "plant_manager",
            "production_lead",
            "setup_lead",
            "material_handler_lead",
            "material_handler",
            "production",
            "setup",
        };

        foreach (var code in pinnedCodes)
        {
            Assert.IsTrue(resourceKeys.Contains($"Role_{code}"), $"Role_{code} is missing from the resource file.");
        }
    }

    [TestMethod]
    public void Declaration_ExposesNoPerRoleBaseline()
    {
        // The baselines are seeded data so that what a role may do changes without a code change (FR-048, FR-046).
        // Nothing on the declaration may carry one, so the type has no member for it and no member returns a
        // per-role answer: the shape is checked because a rule about where data lives has to be visible in the
        // type it constrains.
        var entryType = typeof(PermissionRegistry.Entry);
        var memberNames = entryType.GetProperties().Select(property => property.Name).ToArray();

        CollectionAssert.DoesNotContain(memberNames, "Baseline");
        CollectionAssert.DoesNotContain(memberNames, "RoleBaselines");
        CollectionAssert.DoesNotContain(memberNames, "Roles");
    }

    private static List<string> ReadResourceKeys()
    {
        var path = Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Strings",
            "en-us",
            "Resources.resw");

        Assert.IsTrue(File.Exists(path), "The resource file is missing.");

        var document = System.Xml.Linq.XDocument.Load(path);
        System.Xml.Linq.XNamespace ns = document.Root!.Name.Namespace;

        return document.Root!
            .Elements(ns + "data")
            .Select(element => element.Attribute("name")?.Value)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .ToList();
    }
}
