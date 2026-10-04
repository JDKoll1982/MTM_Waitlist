using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Permissions;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Core.Permissions;

/// <summary>
/// T139 and T149, US6: the inventory of every setting that is restricted to particular roles, taken one setting at
/// a time, so that FR-030's "each keeps the same restriction" is a statement about named settings rather than about
/// a table (FR-030, SC-012).
/// </summary>
/// <remarks>
/// <para>
/// <b>The holder sets are read from the shipped seed, not written here.</b>
/// <c>Database/Seeds/seed_permission_role_baselines/create.sql</c> is the data the store is built from, so a
/// restriction changed in the file fails here, and a change made only in this test proves nothing.
/// </para>
/// <para>
/// <b>One deliberate exception, recorded rather than smoothed over.</b> The list of locations to hide keeps its
/// existing read restriction on <c>permission.settings.ignored_locations</c>, whose nine grants are unchanged, and
/// the <b>write</b> is narrowed to <c>IT Department</c> and <c>Developer</c> through the new
/// <c>permission.settings.ignored_locations_edit</c> key. That narrowing is the named exception research D10
/// records: FR-024 needs two roles to change the list while FR-030 needs every existing restriction to hold, so
/// the restriction is not removed but split rather than narrowed in place.
/// </para>
/// </remarks>
[TestClass]
public sealed class RoleRestrictionInventoryTests
{
    /// <summary>One role-restricted setting: what it is, the gate that reads its key, and the key it reads.</summary>
    private sealed record RestrictedSetting(string Setting, string GateType, string PermissionKey);

    /// <summary>
    /// Every setting that is restricted to particular roles today, named as a person would name it, with the gate
    /// that decides and the key it asks. The list is the inventory T149 asks for. It is asserted to be complete
    /// against the declaration below, so a setting added later cannot ship without appearing here.
    /// </summary>
    private static readonly RestrictedSetting[] s_theInventory =
    [
        new("Ignored locations, read", "SettingsViewModel", PermissionKeys.SettingsIgnoredLocations),
        new("Ignored locations, change", "SettingsViewModel", PermissionKeys.SettingsIgnoredLocationsEdit),
        new("Hot work centres", "SettingsViewModel", PermissionKeys.SettingsHotWorkCenters),
        new("Dunnage-type visibility", "SettingsViewModel", PermissionKeys.SettingsHotWorkCenters),
        new("Part pictures", "SettingsViewModel", PermissionKeys.SettingsPartPictures),
        new("Picture and key folders", "SettingsViewModel", PermissionKeys.SettingsStoragePaths),
        new("Picture cache", "SettingsViewModel", PermissionKeys.SettingsStoragePaths),
        new("Cached Infor Visual data", "SettingsViewModel", PermissionKeys.SettingsCacheRefresh),
        new("How long a sign-in lasts", "SettingsViewModel", PermissionKeys.SettingsSessionLength),
        new("The developer log panel", "DeveloperLogPanelViewModel", PermissionKeys.SettingsLogPanel),
        new("Machine setup and its configuration", "MachineSetupGate", PermissionKeys.SettingsMachineConfiguration),
        new("Minutes allowed per item", "UrgencyAllotmentEditorViewModel", PermissionKeys.SettingsUrgencyMinutes),
        new("The defect-type catalogue", "DefectTypeCatalogService", PermissionKeys.SettingsDefectTypes),
        new("The computer registry", "ComputerManagementViewModel", PermissionKeys.SettingsComputers),
        new("Dunnage Quick Add", "DunnageWorkflowService", PermissionKeys.SetupDunnageQuickAdd),
        new("Work-centre setup", "SetupWorkCenterViewModel", PermissionKeys.SetupWorkCenters),
        new("The user list", "UserManagementViewModel", PermissionKeys.AdminUsers),
        new("Issuing a password reset", "EditUserViewModel", PermissionKeys.AdminResetPassword),
        new("The permissions page", "PermissionsViewModel", PermissionKeys.AdminPermissions),
    ];

    /// <summary>The two keys that gate an action rather than a settings screen; they are inventoried above.</summary>
    private static readonly string[] s_nonSettingsKeys =
    [
        PermissionKeys.RequestsHandle,
        PermissionKeys.CacheRefreshApi,
    ];

    /// <summary>
    /// The restriction on the plant-wide locations list, as research D10 leaves it. The read key keeps its nine
    /// grants; the write key is held by exactly the two roles the requirement names.
    /// </summary>
    private static readonly string[] s_ignoredLocationsReadHolders =
    [
        "developer", "it_department", "plant_manager", "production_lead", "setup_lead", "production", "setup",
    ];

    private static readonly string[] s_ignoredLocationsWriteHolders = ["developer", "it_department"];

    [TestMethod]
    public void TheInventory_CoversEveryDeclaredSettingExactlyOncePerKeyAndGate()
    {
        var declaredKeys = PermissionRegistry.All
            .Where(entry => entry.Key != PermissionKeys.RequestsHandle && entry.Key != PermissionKeys.CacheRefreshApi)
            .Select(entry => entry.Key)
            .ToHashSet(StringComparer.Ordinal);

        var inventoriedKeys = s_theInventory
            .Select(setting => setting.PermissionKey)
            .ToHashSet(StringComparer.Ordinal);

        CollectionAssert.AreEquivalent(
            declaredKeys.ToArray(),
            inventoriedKeys.ToArray(),
            "Every declared setting is in the inventory, so a new one cannot ship without a named restriction.");

        var missingSites = s_theInventory
            .Where(setting => PermissionRegistry.Find(setting.PermissionKey) is null)
            .Select(setting => $"{setting.Setting} reads '{setting.PermissionKey}', which the declaration does not hold.")
            .ToArray();

        Assert.AreEqual(0, missingSites.Length, string.Join(Environment.NewLine, missingSites));
    }

    [TestMethod]
    public void EveryRestrictedSetting_ReadsAKeyWhoseDeclarationNamesItsGate()
    {
        var offenders = new List<string>();

        // One setting at a time, so a failure names the setting rather than a table row.
        foreach (var setting in s_theInventory)
        {
            var entry = PermissionRegistry.Find(setting.PermissionKey);

            if (entry is null)
            {
                offenders.Add($"{setting.Setting}: '{setting.PermissionKey}' is not declared.");
                continue;
            }

            if (!entry.GateSites.Contains(setting.GateType, StringComparer.Ordinal))
            {
                offenders.Add(
                    $"{setting.Setting}: the declaration does not name '{setting.GateType}' as a reader of '{setting.PermissionKey}'.");
            }
        }

        Assert.AreEqual(0, offenders.Count, string.Join(Environment.NewLine, offenders));
    }

    [TestMethod]
    public void EveryRestrictedSetting_HoldsItsRestrictionInTheShippedBaseline()
    {
        var baselines = ReadShippedBaselines();

        var offenders = new List<string>();

        foreach (var setting in s_theInventory)
        {
            if (!baselines.TryGetValue(setting.PermissionKey, out var grants))
            {
                offenders.Add($"{setting.Setting}: '{setting.PermissionKey}' has no baseline row at all.");
                continue;
            }

            if (grants.Count == 0)
            {
                offenders.Add($"{setting.Setting}: '{setting.PermissionKey}' is held by nobody, so it gates nothing.");
            }
        }

        Assert.AreEqual(0, offenders.Count, string.Join(Environment.NewLine, offenders));
    }

    /// <summary>
    /// SC-012's read half, and the exception's other half, in one place: the read restriction keeps its grants
    /// exactly as they were, and the narrowed write is the two roles FR-024 names.
    /// </summary>
    [TestMethod]
    public void TheLocationsToHide_KeepTheirReadRestrictionAndNarrowOnlyTheWrite()
    {
        var baselines = ReadShippedBaselines();

        CollectionAssert.AreEquivalent(
            s_ignoredLocationsReadHolders,
            baselines[PermissionKeys.SettingsIgnoredLocations].Keys.ToArray(),
            "The existing read key keeps its grants untouched: narrowing it would take the read away from five roles (FR-030, research D10).");

        CollectionAssert.AreEquivalent(
            s_ignoredLocationsWriteHolders,
            baselines[PermissionKeys.SettingsIgnoredLocationsEdit].Keys.ToArray(),
            "The write restriction is the deliberate exception, held by exactly the two roles FR-024 names.");

        CollectionAssert.IsSubsetOf(
            s_ignoredLocationsWriteHolders,
            s_ignoredLocationsReadHolders,
            "Everybody who may change the list may also read it, so the split adds a restriction rather than a contradiction.");
    }

    /// <summary>
    /// The two keys that are not settings screens are still role-restricted, and they keep their restrictions too.
    /// They are listed separately because the inventory above is about the surfaces a person finds in Settings.
    /// </summary>
    [TestMethod]
    public void TheTwoKeysThatAreNotSettingsScreens_AreStillRestricted()
    {
        var baselines = ReadShippedBaselines();

        foreach (var key in s_nonSettingsKeys)
        {
            Assert.IsTrue(
                baselines.TryGetValue(key, out var grants) && grants.Count > 0,
                $"'{key}' must still be restricted to particular roles.");
        }
    }

    /// <summary>
    /// The shipped baseline rows, as key to role to granted, read out of the file the repository ships rather than
    /// written here.
    /// </summary>
    private static Dictionary<string, Dictionary<string, bool>> ReadShippedBaselines()
    {
        var seedPath = Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Database",
            "Seeds",
            "seed_permission_role_baselines",
            "create.sql");

        var rows = Regex.Matches(
            File.ReadAllText(seedPath),
            @"\(\s*UUID\(\)\s*,\s*'(?<key>[^']+)'\s*,\s*'role'\s*,\s*'role:(?<role>[a-z_]+)'\s*,\s*(?<grant>[01])\s*,");

        var baselines = new Dictionary<string, Dictionary<string, bool>>(StringComparer.Ordinal);

        foreach (Match row in rows)
        {
            var key = row.Groups["key"].Value;
            var role = row.Groups["role"].Value;
            var granted = row.Groups["grant"].Value == "1";

            if (!baselines.TryGetValue(key, out var grants))
            {
                grants = new Dictionary<string, bool>(StringComparer.Ordinal);
                baselines[key] = grants;
            }

            if (!granted)
            {
                // A role the key is denied is not a holder, so it does not appear in the holder set. The denial
                // is still checked: a role that was granted the key and then denied it would otherwise look the
                // same as one that was never mentioned.
                continue;
            }

            grants[role] = true;
        }

        return baselines;
    }
}
