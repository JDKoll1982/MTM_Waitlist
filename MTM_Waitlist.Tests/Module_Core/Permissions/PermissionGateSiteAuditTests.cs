using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Permissions;
using MTM_Waitlist.Module_Settings.ViewModels;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Core.Permissions;

/// <summary>
/// The gate-site audit (T035, FR-054, FR-058, FR-061, SC-002, SC-003, §6 gate 8): the proof that no gate in the
/// application reads a hand-written list of role names, and that every gate asks for a permission the one
/// declaration holds.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every one of the thirteen sites the specification's Context section counts is enumerated here</b>: the
/// twelve retired role lists — the eleven that became named permissions, plus the badge map, which is re-keyed to
/// role codes and deliberately does <i>not</i> become a permission — and <c>StartupState.IsDeveloper</c>, the
/// thirteenth, which used to compare a display name and is the one a hand count of twelve missed.
/// </para>
/// <para>
/// <b>What makes this an audit rather than a restatement.</b> The role display names it looks for are read from
/// the shipped resource file, keyed on the role code, rather than typed here; the permission keys are read from
/// the declaration rather than restated; and the design names of the two non-permission sites are asserted to
/// carry no permission key at all. A file that grows a role-name array, or a gate that asks for an undeclared
/// key, fails this suite rather than failing silently at a screen.
/// </para>
/// </remarks>
[TestClass]
public sealed class PermissionGateSiteAuditTests
{
    /// <summary>
    /// One gate site: where it lives, the type that decides there, and the permission it reads. A site whose
    /// <see cref="PermissionKey"/> is <see langword="null"/> is one of the two that must not become a permission.
    /// </summary>
    private sealed record GateSite(string Label, string RelativePath, string TypeName, string? PermissionKey);

    /// <summary>
    /// The thirteen sites, in the order the specification's Context section counts them: twelve retired role lists
    /// in nine files, and the developer check as the thirteenth site in a tenth file.
    /// </summary>
    private static readonly GateSite[] s_theThirteenSites =
    [
        new("Request actions", "MTM_Waitlist.Core/Services/RequestActionPolicy.cs", "RequestActionPolicy", PermissionKeys.RequestsHandle),
        new("The service host's operator gate", "MTM_Waitlist.Mock.Service/Api/ServiceOperatorRoles.cs", "ServiceOperatorRoles", PermissionKeys.CacheRefreshApi),
        new("Ignored locations", "MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs", "SettingsViewModel", PermissionKeys.SettingsIgnoredLocations),
        new("Hot work centres", "MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs", "SettingsViewModel", PermissionKeys.SettingsHotWorkCenters),
        new("Part pictures", "MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs", "SettingsViewModel", PermissionKeys.SettingsPartPictures),
        new("The Settings cache refresh", "MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs", "SettingsViewModel", PermissionKeys.SettingsCacheRefresh),
        new("The minutes per item", "MTM_Waitlist.Settings/ViewModels/UrgencyAllotmentEditorViewModel.cs", "UrgencyAllotmentEditorViewModel", PermissionKeys.SettingsUrgencyMinutes),
        new("The defect-type catalogue", "MTM_Waitlist.Settings/Services/DefectTypeCatalogService.cs", "DefectTypeCatalogService", PermissionKeys.SettingsDefectTypes),
        new("The computer registry", "MTM_Waitlist.Settings/ViewModels/ComputerManagementViewModel.cs", "ComputerManagementViewModel", PermissionKeys.SettingsComputers),
        new("Dunnage Quick Add", "MTM_Waitlist.Setup/Services/DunnageWorkflowService.cs", "DunnageWorkflowService", PermissionKeys.SetupDunnageQuickAdd),
        new("Work-centre setup", "MTM_Waitlist.Setup/ViewModels/SetupWorkCenterViewModel.cs", "SetupWorkCenterViewModel", PermissionKeys.SetupWorkCenters),
        new("The badge map", "MTM_Waitlist.Core/Permissions/RoleBadgeCatalog.cs", "RoleBadgeCatalog", PermissionKey: null),
        new("The developer check", "MTM_Waitlist.Core/Models/StartupState.cs", "StartupState", PermissionKey: null),
    ];

    /// <summary>
    /// The strings that name roles the catalogue has never held, pinned by the specification's Verbatim
    /// Constraints section. They must be gone rather than carried forward (FR-107).
    /// </summary>
    private static readonly string[] s_retiredRoleVocabulary =
    [
        "administrator",
        "supervisor",
        "manager",
        "quality",
        "quality inspector",
        "Setup Tech",
        "Admin",
    ];

    [TestMethod]
    public void TheThirteenSites_AreTheOnesTheSpecificationCounts()
    {
        Assert.AreEqual(13, s_theThirteenSites.Length, "The specification's Context section counts thirteen gate sites.");

        var files = s_theThirteenSites.Select(site => site.RelativePath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        Assert.AreEqual(10, files.Length, "The thirteen sites sit in ten files.");

        var deciding = s_theThirteenSites.Count(site => site.PermissionKey is not null);
        Assert.AreEqual(
            11,
            deciding,
            "Eleven of the twelve retired lists decide access and became named permissions; the twelfth is the badge map.");

        foreach (var site in s_theThirteenSites)
        {
            Assert.IsTrue(
                File.Exists(Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), site.RelativePath.Replace('/', Path.DirectorySeparatorChar))),
                $"'{site.RelativePath}' is one of the audited gate sites and must exist.");
        }
    }

    [TestMethod]
    public void EverySiteThatDecidesAccess_IsNamedByTheDeclarationAsTheReaderOfItsKey()
    {
        foreach (var site in s_theThirteenSites.Where(site => site.PermissionKey is not null))
        {
            var entry = PermissionRegistry.Find(site.PermissionKey!);

            Assert.IsNotNull(entry, $"'{site.PermissionKey}' must be declared; the {site.Label} gate reads it.");
            CollectionAssert.Contains(
                entry!.GateSites.ToArray(),
                site.TypeName,
                $"The declaration must name '{site.TypeName}' as the reader of '{site.PermissionKey}', or the two checks over the declaration lose their subject.");
        }
    }

    [TestMethod]
    public void EverySiteThatDecidesAccess_AsksForAKeyTheDeclarationHolds()
    {
        // The keys are read from the declaration, so a gate that invents a spelling fails here rather than
        // refusing everybody silently at run time (FR-061).
        var declaredKeys = PermissionKeys.All.ToHashSet(StringComparer.Ordinal);
        var declaredNames = ConstantNamesByKey();

        var offenders = new List<string>();

        foreach (var (relativePath, code) in ProductionCodeFiles())
        {
            foreach (Match match in Regex.Matches(code, @"PermissionKeys\.(?<name>[A-Za-z_][A-Za-z0-9_]*)"))
            {
                var name = match.Groups["name"].Value;

                if (!declaredNames.TryGetValue(name, out var key))
                {
                    offenders.Add($"{relativePath}: PermissionKeys.{name} is not a constant the declaration holds.");
                }
                else if (!declaredKeys.Contains(key))
                {
                    offenders.Add($"{relativePath}: PermissionKeys.{name} names '{key}', which the declaration does not hold.");
                }
            }
        }

        Assert.AreEqual(0, offenders.Count, string.Join(Environment.NewLine, offenders));

        // A gate may also name a key through the declaration lookup. Every such literal must be declared too.
        foreach (var (relativePath, code) in ProductionCodeFiles())
        {
            foreach (Match match in Regex.Matches(code, @"PermissionRegistry\.(?:Find|IsDeclared)\(\s*""(?<key>[^""]+)"""))
            {
                Assert.IsTrue(
                    declaredKeys.Contains(match.Groups["key"].Value),
                    $"{relativePath} asks about '{match.Groups["key"].Value}', which the declaration does not hold.");
            }
        }
    }

    [TestMethod]
    public void NoProductionFile_HoldsAHandWrittenListOfRoleNames()
    {
        var displayNames = CatalogueRoleDisplayNames();
        Assert.AreEqual(
            9,
            displayNames.Count,
            "The role catalogue holds nine roles, so nine display names is what a role list would name.");

        var offenders = new List<string>();

        foreach (var (relativePath, code) in ProductionCodeFiles())
        {
            var named = displayNames
                .Where(name => Regex.IsMatch(code, $"\u0022{Regex.Escape(name)}\u0022"))
                .ToArray();

            if (named.Length >= 2)
            {
                offenders.Add(
                    $"{relativePath} holds {named.Length} role display names ({string.Join(", ", named)}): a hand-written list of role names deciding access is exactly what FR-054 forbids. "
                        + "The answer belongs in the seeded baselines, read through the declaration.");
            }
        }

        Assert.AreEqual(
            0,
            offenders.Count,
            string.Join(Environment.NewLine, offenders) + Environment.NewLine
                + "A single display name is allowed: the default developer account's own name is not a gate.");
    }

    [TestMethod]
    public void TheRetiredRoleVocabulary_IsGoneFromTheApplication()
    {
        var offenders = new List<string>();

        foreach (var (relativePath, code) in ProductionCodeFiles())
        {
            foreach (var retired in s_retiredRoleVocabulary)
            {
                if (Regex.IsMatch(code, $"\u0022{Regex.Escape(retired)}\u0022"))
                {
                    offenders.Add($"{relativePath} still holds the retired role name \"{retired}\".");
                }
            }
        }

        Assert.AreEqual(
            0,
            offenders.Count,
            string.Join(Environment.NewLine, offenders) + Environment.NewLine
                + "These name roles the catalogue has never held or has retired, so they can never match a live person (FR-107).");
    }

    [TestMethod]
    public void TheTwoSitesThatAreNotPermissions_DoNotBecomeOne()
    {
        var badge = ReadProductionFile("MTM_Waitlist.Core/Permissions/RoleBadgeCatalog.cs");
        var developerCheck = ReadProductionFile("MTM_Waitlist.Core/Models/StartupState.cs");

        Assert.IsFalse(
            badge.Contains("PermissionKeys.", StringComparison.Ordinal),
            "The badge is presentation, not access: it must not become a permission (FR-058).");
        Assert.IsTrue(
            badge.Contains("RoleCode", StringComparison.Ordinal),
            "The badge map is keyed on the role code, which is what makes a rename a presentation change.");

        Assert.IsTrue(
            developerCheck.Contains(nameof(StartupState.CurrentRoleCode), StringComparison.Ordinal),
            "The developer check is the thirteenth site and must read the role code.");

        // The display-name property stays, because the screens that show text still use it. What must not stay is
        // the check reading it, so the assertion is over the check's own body rather than over the whole file.
        var developerCheckBody = Regex.Match(developerCheck, @"IsDeveloper\s*=>[^;]+;").Value;

        Assert.IsFalse(
            string.IsNullOrWhiteSpace(developerCheckBody),
            "StartupState.IsDeveloper is one of the thirteen sites; it moved, so update this audit rather than deleting it.");
        Assert.IsTrue(
            developerCheckBody.Contains(nameof(StartupState.CurrentRoleCode), StringComparison.Ordinal),
            "The developer check must read the role code.");
        Assert.IsFalse(
            Regex.IsMatch(developerCheckBody, @"CurrentRole\b(?!Code)"),
            "The developer check must not read the display name, which is presentation (FR-054).");
    }

    [TestMethod]
    public void TheThirteenSites_EachHaveTheirOwnRetiredListGone()
    {
        // A direct statement of the same claim from the other direction: for each of the eleven sites, none of the
        // declared field names a retired list used may survive beside its permission. The badge map and the
        // developer check are excluded because they were never a permission and their own check is above.
        string[] retiredFieldNames =
        [
            "HandlerRoles",
            "Approved",
            "AllowedIgnoredLocationManageRoles",
            "AllowedHotWorkCenterManageRoles",
            "AllowedImageLocationManageRoles",
            "AllowedCacheRefreshRoles",
            "AllowedUrgencyManageRoles",
            "AllowedRoles",
            "AllowedComputerManageRoles",
            "AllowedQuickAddRoles",
            "AllowedManageRoles",
        ];

        var offenders = new List<string>();

        foreach (var site in s_theThirteenSites.Where(site => site.PermissionKey is not null))
        {
            var code = ReadProductionFile(site.RelativePath);

            foreach (var fieldName in retiredFieldNames)
            {
                if (Regex.IsMatch(code, $@"\b{Regex.Escape(fieldName)}\b"))
                {
                    offenders.Add($"{site.RelativePath} still declares '{fieldName}': {site.Label} would be half-replaced.");
                }
            }
        }

        Assert.AreEqual(0, offenders.Count, string.Join(Environment.NewLine, offenders));
    }

    /// <summary>
    /// The nine role display names, read from the shipped resource file that holds them keyed on the role code, so
    /// this audit does not restate role names it is supposed to be looking for.
    /// </summary>
    private static IReadOnlyList<string> CatalogueRoleDisplayNames()
    {
        var resources = ReadProductionFile("Strings/en-us/Resources.resw");

        return [.. Regex.Matches(resources, @"<data name=""Role_(?<code>[a-z_]+)""[^>]*>\s*<value>(?<name>[^<]+)</value>")
            .Select(match => match.Groups["name"].Value.Trim())];
    }

    /// <summary>Maps each <c>PermissionKeys</c> constant's name to its value, read from the declaration's source.</summary>
    private static Dictionary<string, string> ConstantNamesByKey()
    {
        var source = ReadProductionFile("MTM_Waitlist.Core/Permissions/PermissionKeys.cs");

        return Regex.Matches(source, @"public const string (?<name>[A-Za-z_][A-Za-z0-9_]*) = ""(?<value>[^""]+)""")
            .ToDictionary(match => match.Groups["name"].Value, match => match.Groups["value"].Value, StringComparer.Ordinal);
    }

    private static string ReadProductionFile(string relativePath)
    {
        var fullPath = Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            relativePath.Replace('/', Path.DirectorySeparatorChar));

        Assert.IsTrue(File.Exists(fullPath), $"'{relativePath}' must exist for the gate-site audit to mean anything.");

        return StripComments(File.ReadAllText(fullPath));
    }

    /// <summary>
    /// Every production C# file in the repository, with its comments removed.
    /// </summary>
    /// <remarks>
    /// The test project is out of scope: its fixtures record the retired lists verbatim on purpose, which is what
    /// makes the parity comparison possible. A comment that explains why a role name was removed is not a role
    /// name deciding access, so comments are stripped before anything is matched.
    /// </remarks>
    private static IEnumerable<(string RelativePath, string Code)> ProductionCodeFiles()
    {
        var root = RepositoryPatternScan.FindRepositoryRoot();
        var scope = new RepositoryScanScope
        {
            Extensions = [".cs"],
            ExcludedFileNames = ["PermissionGateSiteAuditTests.cs"],
        };

        foreach (var file in RepositoryPatternScan.EnumerateScannedFiles(root, scope))
        {
            var relativePath = Path.GetRelativePath(root, file);

            if (relativePath.StartsWith("MTM_Waitlist.Tests" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return (relativePath, StripComments(File.ReadAllText(file)));
        }
    }

    /// <summary>
    /// The picture cache's gate reads the storage entitlement, not the picture one (008-part-pictures, FR-017,
    /// FR-025).
    /// </summary>
    /// <remarks>
    /// The two keys stopped having the same members when the picture entitlement was widened to Setup Lead and
    /// Plant Manager, so a panel still reading the picture key would hand a Setup Lead the folder this machine
    /// copies pictures from — a storage control FR-017 keeps with the IT Department and Developer. The read is
    /// proved at the assignment as well as at the gate, because a panel gated on a property whose own name means
    /// nothing is the failure a check of the gate alone would miss.
    /// </remarks>
    [TestMethod]
    public void ThePictureCacheGate_ReadsTheStorageEntitlementAndNotThePictureOne()
    {
        var settings = ReadProductionFile("MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs");

        var gate = Regex.Match(settings, @"IsPictureCachePanelVisible\s*=>[^;]+;").Value;

        Assert.IsFalse(
            string.IsNullOrWhiteSpace(gate),
            "IsPictureCachePanelVisible is the moved gate; it moved, so update this audit rather than deleting it.");
        StringAssert.Contains(
            gate,
            nameof(SettingsViewModel.CanManageStoragePaths),
            "The picture cache's panel must be gated on the storage entitlement (FR-017).");
        Assert.IsFalse(
            gate.Contains(nameof(SettingsViewModel.CanManageImageLocationSettings), StringComparison.Ordinal),
            "The panel must not be gated on the picture entitlement, which FR-025 widened past the storage roles.");
        Assert.IsFalse(
            gate.Contains("PermissionKeys.", StringComparison.Ordinal),
            "A gate reads a permission through the property that already resolved it, never a key of its own (FR-054).");

        var assignment = Regex.Match(settings, @"CanManageStoragePaths\s*=\s*answers\[(?<key>PermissionKeys\.[A-Za-z_]+)\]");

        Assert.IsTrue(
            assignment.Success,
            "CanManageStoragePaths must be answered from the permission read, or the gate decides on a guess.");
        Assert.AreEqual(
            $"PermissionKeys.{nameof(PermissionKeys.SettingsStoragePaths)}",
            assignment.Groups["key"].Value,
            "The storage entitlement is what the picture cache's panel belongs to (FR-017).");
    }

    private static string StripComments(string source) =>
        Regex.Replace(
            Regex.Replace(source, @"/\*.*?\*/", " ", RegexOptions.Singleline),
            @"//[^\r\n]*",
            " ");
}
