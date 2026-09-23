using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Permissions;
using MTM_Waitlist.Tests.Module_Mock;
using MTM_Waitlist.Tests.Module_Settings.Fixtures;

namespace MTM_Waitlist.Tests.Module_Mock_Service;

/// <summary>
/// The service host's operator gate stated as the permission it becomes, and the one subset rule that ties it to
/// the Settings screen's cache-refresh gate (T034, FR-057, decision 7).
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this file and not the parity file.</b> The subset rule spans two projects: the Settings screen's gate
/// and the service host's gate. Neither can see the other's code, so the rule is asserted against the shipped
/// baselines — data both can read — rather than against either side's implementation. This file is in the
/// service-host test module because the failure it prevents is the service refusing a caller the Settings panel
/// admitted, which is the direction a person actually meets.
/// </para>
/// <para>
/// <b>Only what is true before the migration.</b> The audited claim that the service host no longer keeps its
/// own role list is the gate-site audit's job (T035); this file states the relationship the replacement must
/// preserve, so a later change to either side's data fails here rather than at the service.
/// </para>
/// </remarks>
[TestClass]
public sealed class ServiceOperatorPermissionTests
{
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

    private static string SeedPath => Path.Combine(
        RepositoryPatternScan.FindRepositoryRoot(),
        "Database",
        "Seeds",
        "seed_permission_role_baselines",
        "create.sql");

    [TestMethod]
    public void TheShippedBaselines_AdmitToTheApiEveryRoleTheSettingsCacheRefreshGateAdmits()
    {
        var baselines = ReadBaselines();

        var admittedByTheSettingsGate = CatalogueRoleCodes
            .Where(roleCode => baselines[(PermissionKeys.SettingsCacheRefresh, roleCode)])
            .ToArray();

        Assert.IsTrue(
            admittedByTheSettingsGate.Length > 0,
            "The rule is vacuous if the Settings gate admits nobody.");

        foreach (var roleCode in admittedByTheSettingsGate)
        {
            Assert.IsTrue(
                baselines[(PermissionKeys.CacheRefreshApi, roleCode)],
                $"The Settings screen must never offer a cache refresh the service host would refuse: "
                    + $"{PermissionKeys.SettingsCacheRefresh} admits {roleCode} and {PermissionKeys.CacheRefreshApi} does not (FR-057).");
        }
    }

    [TestMethod]
    public void TheDeclaration_NamesTheServiceOperatorGateAsTheReaderOfTheApiPermission()
    {
        var entry = PermissionRegistry.Find(PermissionKeys.CacheRefreshApi);

        Assert.IsNotNull(entry, $"{PermissionKeys.CacheRefreshApi} must be declared: the service host gates on it.");
        CollectionAssert.Contains(
            entry!.GateSites.ToArray(),
            "ServiceOperatorRoles",
            "The declaration names the gate sites that read a key, and the service host's operator gate is the reader of this one.");
    }

    [TestMethod]
    public void TheDeclaredFallback_RefusesTheApiWhenTheStoreCannotBeReached()
    {
        // The service host fails closed: an unanswerable authorization question is a "no". The declaration's
        // fallback is what answers when neither a person's row nor a role baseline exists, which is also what
        // answers when the store cannot be read, so it must be false for this key.
        var entry = PermissionRegistry.Find(PermissionKeys.CacheRefreshApi);

        Assert.IsNotNull(entry);
        Assert.IsFalse(
            entry!.Fallback,
            $"{PermissionKeys.CacheRefreshApi} must fall back to a refusal, so an unreachable store cannot admit a caller.");
    }

    [TestMethod]
    public void TheRetiredOperatorList_IsStillTheListThisFeatureReplaces()
    {
        // The list the shipped baselines were derived from, read from the fixture rather than from the service
        // project, so the parity this feature claims is against what the service actually admitted.
        var baselines = ReadBaselines();

        foreach (var displayName in RetiredRoleListFixture.ServiceOperatorRolesApproved)
        {
            var roleCode = displayName.Trim().ToLowerInvariant() switch
            {
                "admin" => "it_department",
                "developer" => "developer",
                "plant manager" => "plant_manager",
                "production lead" => "production_lead",
                "setup lead" => "setup_lead",
                _ => throw new AssertFailedException($"'{displayName}' has no role code; the mapping must be extended."),
            };

            Assert.IsTrue(
                baselines[(PermissionKeys.CacheRefreshApi, roleCode)],
                $"The retired operator list admitted '{displayName}' ({roleCode}), so the shipped baseline must admit it too.");
        }
    }

    /// <summary>The shipped baselines, keyed by permission key and role code, read from the seed file itself.</summary>
    private static Dictionary<(string Key, string RoleCode), bool> ReadBaselines()
    {
        Assert.IsTrue(File.Exists(SeedPath), $"The shipped baseline seed is missing: {SeedPath}");

        var seed = File.ReadAllText(SeedPath);
        var baselines = new Dictionary<(string, string), bool>();

        foreach (Match match in Regex.Matches(
            seed,
            @"\(\s*UUID\(\)\s*,\s*'(?<key>[^']+)'\s*,\s*'role'\s*,\s*'role:(?<code>[a-z_]+)'\s*,\s*(?<value>[01])\s*,",
            RegexOptions.IgnoreCase))
        {
            baselines[(match.Groups["key"].Value, match.Groups["code"].Value)] = match.Groups["value"].Value == "1";
        }

        Assert.AreEqual(
            PermissionRegistry.All.Count * CatalogueRoleCodes.Length,
            baselines.Count,
            "The shipped baselines must hold every declared key for every role before this file's rules are meaningful.");

        return baselines;
    }
}
