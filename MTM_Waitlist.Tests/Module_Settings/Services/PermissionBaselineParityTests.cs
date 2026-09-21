using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Permissions;
using MTM_Waitlist.Tests.Module_Mock;
using MTM_Waitlist.Tests.Module_Settings.Fixtures;

namespace MTM_Waitlist.Tests.Module_Settings.Services;

/// <summary>
/// The shipped permission baselines compared against the twelve hand-written role lists they replace, in both
/// directions, and compared against the declaration in both directions too (T034, FR-047, FR-052, FR-057,
/// SC-001, §6 gate 7).
/// </summary>
/// <remarks>
/// <para>
/// <b>What is under test is the shipped data, not a copy of it.</b> Every assertion below reads the rows out of
/// <c>Database/Seeds/seed_permission_role_baselines/create.sql</c> — the file the repository ships — so a
/// baseline edited in the file fails here, and a baseline edited only in this test proves nothing. The retired
/// lists come from <see cref="RetiredRoleListFixture"/>, which holds them verbatim as the shipping code wrote
/// them, including the two entries that match no role.
/// </para>
/// <para>
/// <b>Zero differences, per role and per permission.</b> The comparison is not a count of members and not a
/// summary: each of the eleven keys is compared against its own retired list, for every role the catalogue
/// holds, and the expected answer for every pair is computed here rather than read from the seed.
/// </para>
/// <para>
/// <b>One stated row, because the retired lists cannot describe it.</b> <c>material_handler_lead</c> was added as
/// a role in its own right by Phase 3 and appears in none of the twelve lists, so there is no list to compare it
/// against. Its whole row is the specification's Assumptions: the worker level, plus handling requests and the
/// service cache refresh, and nothing else — including deliberately not the ignored-locations feature. It is
/// asserted against that stated set. Nobody holds the role on ship day.
/// </para>
/// <para>
/// <b>Three keys have no predecessor at all</b> (<c>permission.admin.users</c>,
/// <c>permission.admin.reset_password</c> and <c>permission.admin.permissions</c>), so they are asserted against
/// their stated sets rather than against a retired list. This is what makes SC-001 evaluable for them.
/// </para>
/// <para>
/// <b>The per-person side is deliberately absent.</b> The ship-day claim that somebody relying on their role
/// baseline gets exactly what their retired list gave them, and that zero per-person rows are stored, needs a
/// live store; it belongs to T072, where the store is available. This file asserts the shipped data.
/// </para>
/// </remarks>
[TestClass]
public sealed class PermissionBaselineParityTests
{
    /// <summary>
    /// The nine role codes the catalogue holds once the rename has run, pinned verbatim by the permission
    /// contract. A baseline keyed to anything else would be a baseline nobody holds.
    /// </summary>
    private static readonly string[] CatalogueRoleCodes =
    [
        "developer",
        "it_department",
        "plant_manager",
        "production_lead",
        "setup_lead",
        "material_handler_lead",
        "material_handler",
        "production",
        "setup",
    ];

    /// <summary>
    /// The retired display name each role code stands for, so a list written in display names can be compared
    /// against rows written in codes. The two entries mapped to <c>null</c> are the ones
    /// <see cref="RetiredRoleListFixture.EntriesMatchingNoCatalogRole"/> records: they name roles the catalogue
    /// has never held, so they are dropped rather than mapped onto the nearest role.
    /// </summary>
    private static readonly Dictionary<string, string?> RetiredDisplayNameToRoleCode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Admin"] = "it_department",
        ["Developer"] = "developer",
        ["Plant Manager"] = "plant_manager",
        ["Production Lead"] = "production_lead",
        ["Setup Lead"] = "setup_lead",
        ["Material Handler"] = "material_handler",
        ["Production"] = "production",
        ["Setup"] = "setup",
        ["administrator"] = null,
        ["Setup Tech"] = null,
    };

    /// <summary>
    /// The eleven keys that replace a retired list, each bound to the list it replaces. This binding is the
    /// contract's own table, restated here as the thing under test rather than as an expectation of the code.
    /// </summary>
    private static readonly (string Key, IReadOnlyList<string> RetiredList)[] ReplacedLists =
    [
        (PermissionKeys.RequestsHandle, RetiredRoleListFixture.RequestActionPolicyHandlerRoles),
        (PermissionKeys.CacheRefreshApi, RetiredRoleListFixture.ServiceOperatorRolesApproved),
        (PermissionKeys.SettingsIgnoredLocations, RetiredRoleListFixture.SettingsIgnoredLocationManageRoles),
        (PermissionKeys.SettingsHotWorkCenters, RetiredRoleListFixture.SettingsHotWorkCenterManageRoles),
        (PermissionKeys.SettingsPartPictures, RetiredRoleListFixture.SettingsImageLocationManageRoles),
        (PermissionKeys.SettingsCacheRefresh, RetiredRoleListFixture.SettingsCacheRefreshRoles),
        (PermissionKeys.SettingsUrgencyMinutes, RetiredRoleListFixture.UrgencyAllotmentManageRoles),
        (PermissionKeys.SettingsDefectTypes, RetiredRoleListFixture.DefectTypeCatalogAllowedRoles),
        (PermissionKeys.SettingsComputers, RetiredRoleListFixture.ComputerManageRoles),
        (PermissionKeys.SetupDunnageQuickAdd, RetiredRoleListFixture.DunnageQuickAddRoles),
        (PermissionKeys.SetupWorkCenters, RetiredRoleListFixture.SetupWorkCenterManageRoles),
    ];

    /// <summary>The keys with no predecessor list, and the roles their baselines are stated to ship to.</summary>
    private static readonly Dictionary<string, string[]> StatedSetsWithNoPredecessor = new(StringComparer.Ordinal)
    {
        [PermissionKeys.AdminUsers] =
            ["production_lead", "setup_lead", "material_handler_lead", "plant_manager", "it_department", "developer"],
        [PermissionKeys.AdminResetPassword] =
            ["production_lead", "setup_lead", "material_handler_lead", "plant_manager", "it_department", "developer"],
        [PermissionKeys.AdminPermissions] =
            ["it_department", "plant_manager", "developer"],
    };

    /// <summary>
    /// The keys <c>material_handler_lead</c> holds, which is its whole row: no retired list can describe it
    /// because the role was added by Phase 3, so the specification's Assumptions state it instead. Two of the
    /// four come from the eleven lists it is absent from (handling requests, and the service cache API, which
    /// the Assumptions add at the worker level); the other two are the stated account-management sets, which
    /// name this role individually.
    /// </summary>
    private static readonly string[] MaterialHandlerLeadGranted =
    [
        PermissionKeys.RequestsHandle,
        PermissionKeys.CacheRefreshApi,
        PermissionKeys.AdminUsers,
        PermissionKeys.AdminResetPassword,
    ];

    /// <summary>The role whose whole row is stated rather than compared against a retired list.</summary>
    private const string StatedRoleCode = "material_handler_lead";

    private static string SeedPath => Path.Combine(
        RepositoryPatternScan.FindRepositoryRoot(),
        "Database",
        "Seeds",
        "seed_permission_role_baselines",
        "create.sql");

    [TestMethod]
    public void TheShippedBaselines_ReproduceEveryRetiredListExactly_ForEveryRoleAndEveryPermission()
    {
        var baselines = ReadShippedBaselines();

        foreach (var (key, retiredList) in ReplacedLists)
        {
            var expectedTrue = ExpectedHolders(key, retiredList);

            foreach (var roleCode in CatalogueRoleCodes)
            {
                // The one role no retired list mentions at all: added by Phase 3, so it has no list to be
                // compared against and its row is the specification's stated one instead.
                var expected = roleCode == StatedRoleCode
                    ? MaterialHandlerLeadGranted.Contains(key)
                    : expectedTrue.Contains(roleCode);
                var actual = baselines[(key, roleCode)];

                Assert.AreEqual(
                    expected,
                    actual,
                    $"{key} for {roleCode}: the retired list it replaces {(expected ? "admitted" : "did not admit")} this role, "
                        + $"so the shipped baseline must be {(expected ? "true" : "false")} "
                        + $"(list: {string.Join(", ", retiredList)}).");
            }
        }
    }

    [TestMethod]
    public void TheShippedBaselines_GiveMaterialHandlerLeadItsStatedRowAndNothingMore()
    {
        var baselines = ReadShippedBaselines();

        foreach (var entry in PermissionRegistry.All)
        {
            var expected = MaterialHandlerLeadGranted.Contains(entry.Key);
            Assert.AreEqual(
                expected,
                baselines[(entry.Key, StatedRoleCode)],
                $"{StatedRoleCode} holds {string.Join(", ", MaterialHandlerLeadGranted)} and nothing else.");
        }

        // The one place the most-restrictive rule looks odd, recorded by the specification rather than decided
        // silently: a plain worker in a production role is granted ignored locations, and this lead role is not.
        Assert.IsFalse(
            baselines[(PermissionKeys.SettingsIgnoredLocations, StatedRoleCode)],
            "Material Handler Lead is deliberately not granted the ignored-locations feature.");
    }

    [TestMethod]
    public void TheShippedBaselines_GiveTheThreeKeysWithNoPredecessorTheirStatedSets()
    {
        var baselines = ReadShippedBaselines();

        foreach (var (key, statedRoles) in StatedSetsWithNoPredecessor)
        {
            var expectedTrue = new HashSet<string>(statedRoles, StringComparer.Ordinal);

            foreach (var roleCode in CatalogueRoleCodes)
            {
                Assert.AreEqual(
                    expectedTrue.Contains(roleCode),
                    baselines[(key, roleCode)],
                    $"{key} ships to {string.Join(", ", statedRoles)} and to nobody else.");
            }
        }
    }

    [TestMethod]
    public void TheShippedBaselines_AndTheDeclaration_AgreeInBothDirections()
    {
        var baselines = ReadShippedBaselines();

        // Direction one: every declared key carries a row for every role, so no key is absent for a role that
        // exists and no permission silently refuses a whole role.
        foreach (var entry in PermissionRegistry.All)
        {
            foreach (var roleCode in CatalogueRoleCodes)
            {
                Assert.IsTrue(
                    baselines.ContainsKey((entry.Key, roleCode)),
                    $"The declaration holds {entry.Key}, so the shipped baselines must carry a row for every role, "
                        + $"including {roleCode}.");
            }
        }

        // Direction two: every baseline row names a declared key, so the data cannot hold a permission the
        // declaration does not.
        foreach (var (key, roleCode) in baselines.Keys)
        {
            Assert.IsTrue(
                PermissionRegistry.IsDeclared(key),
                $"The shipped baselines hold a row for '{key}' ({roleCode}), which the declaration does not name.");
        }

        // And nothing beyond the declaration's own key set is seeded, so the two sets are equal and not merely
        // nested one way.
        Assert.AreEqual(
            PermissionRegistry.All.Count,
            baselines.Keys.Select(pair => pair.Key).Distinct(StringComparer.Ordinal).Count(),
            "The shipped baselines must name exactly the declared keys.");
    }

    [TestMethod]
    public void TheShippedBaselines_StoreNoRowForAnybodyInParticular()
    {
        var seed = ReadSeed();

        // The prose in the seed's header explains what it deliberately does not write, so only a written value
        // counts: a `user:<id>` scope key, or a `user` scope type in a value position.
        Assert.IsFalse(
            Regex.IsMatch(seed, @"'user:\d*'", RegexOptions.IgnoreCase),
            "No per-person row may be seeded: the first one is written by the change-set procedure, after the scope re-rank ships (FR-051, FR-052).");

        Assert.IsFalse(
            Regex.IsMatch(seed, @"'user'\s*,", RegexOptions.IgnoreCase),
            "The baseline seed writes `role`-scoped rows only.");
    }

    [TestMethod]
    public void TheSubsetRule_HoldsInTheShippedBaselines()
    {
        var baselines = ReadShippedBaselines();

        var admittedByTheSettingsGate = CatalogueRoleCodes
            .Where(roleCode => baselines[(PermissionKeys.SettingsCacheRefresh, roleCode)])
            .ToArray();

        Assert.IsTrue(
            admittedByTheSettingsGate.Length > 0,
            "The subset rule is vacuous if the Settings cache-refresh gate admits nobody; the shipped baseline admits somebody.");

        foreach (var roleCode in admittedByTheSettingsGate)
        {
            Assert.IsTrue(
                baselines[(PermissionKeys.CacheRefreshApi, roleCode)],
                $"Whatever {PermissionKeys.SettingsCacheRefresh} admits must also be admitted by "
                    + $"{PermissionKeys.CacheRefreshApi}, so the Settings screen can never show a control the "
                    + $"service host would refuse. It admits {roleCode} and the API does not (FR-057).");
        }
    }

    [TestMethod]
    public void TheRetiredLists_ContainExactlyTheTwoEntriesThatMatchNoCatalogueRole()
    {
        var unmatched = RetiredRoleListFixture.BySource
            .Where(pair => pair.Key != "ShellViewModel.GetUserPresentation")
            .SelectMany(pair => pair.Value)
            .Where(name => !RetiredDisplayNameToRoleCode.TryGetValue(name, out var code) || code is null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        CollectionAssert.AreEquivalent(
            RetiredRoleListFixture.EntriesMatchingNoCatalogRole.ToArray(),
            unmatched,
            "The mapping above must cover every entry the retired lists hold, and drop exactly the two that name no role.");
    }

    /// <summary>
    /// The roles a retired list admits, expressed as the catalogue's own codes, with the two entries that match
    /// no role dropped rather than mapped onto the nearest one.
    /// </summary>
    private static HashSet<string> ExpectedHolders(string key, IReadOnlyList<string> retiredList)
    {
        var holders = new HashSet<string>(StringComparer.Ordinal);

        foreach (var displayName in retiredList)
        {
            Assert.IsTrue(
                RetiredDisplayNameToRoleCode.TryGetValue(displayName, out var roleCode),
                $"'{displayName}' in {key}'s retired list has no mapping; a new entry must be mapped or recorded as matching no role.");

            if (roleCode is not null)
            {
                holders.Add(roleCode);
            }
        }

        return holders;
    }

    /// <summary>
    /// The baselines the repository ships, keyed by permission key and role code, read out of the seed file
    /// itself. A duplicate row fails rather than silently overwriting: the table's own unique key would refuse
    /// it, so the seed must not rely on that.
    /// </summary>
    private static Dictionary<(string Key, string RoleCode), bool> ReadShippedBaselines()
    {
        var seed = ReadSeed();
        var baselines = new Dictionary<(string, string), bool>();

        foreach (Match match in Regex.Matches(
            seed,
            @"\(\s*UUID\(\)\s*,\s*'(?<key>[^']+)'\s*,\s*'(?<scope_type>[^']*)'\s*,\s*'(?<scope_key>[^']*)'\s*,\s*(?<value>[01])\s*,",
            RegexOptions.IgnoreCase))
        {
            var key = match.Groups["key"].Value;
            var scopeType = match.Groups["scope_type"].Value;
            var scopeKey = match.Groups["scope_key"].Value;
            var value = match.Groups["value"].Value == "1";

            Assert.AreEqual("role", scopeType, $"Every row this seed writes is role-scoped; '{key}' is not.");
            Assert.IsTrue(
                scopeKey.StartsWith("role:", StringComparison.Ordinal),
                $"'{scopeKey}' must be keyed `role:<role_code>`, which is what the read matches on.");

            var roleCode = scopeKey["role:".Length..];
            Assert.IsTrue(
                CatalogueRoleCodes.Contains(roleCode, StringComparer.Ordinal),
                $"'{roleCode}' is not one of the nine codes the catalogue holds, so a baseline keyed to it is one nobody holds.");

            Assert.IsTrue(
                baselines.TryAdd((key, roleCode), value),
                $"The seed writes {key} for {roleCode} more than once.");
        }

        Assert.AreEqual(
            PermissionRegistry.All.Count * CatalogueRoleCodes.Length,
            baselines.Count,
            "The seed must write every declared key for every role, and nothing else.");

        return baselines;
    }

    private static string ReadSeed()
    {
        Assert.IsTrue(File.Exists(SeedPath), $"The shipped baseline seed is missing: {SeedPath}");

        return File.ReadAllText(SeedPath);
    }
}
