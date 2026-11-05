using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Shared;

/// <summary>
/// There is one dunnage list, one catalogue loader and one row shape. This audit is what would fail if a second one
/// appeared (FR-002, SC-003).
/// </summary>
/// <remarks>
/// Every check names the offending path when it fails, so the next person is told where the second list is rather
/// than only that one exists.
/// </remarks>
[TestClass]
public sealed class DunnageCatalogueSourceAuditTests
{
    private const string ControlPath = "Module_Shared/Controls/DunnageCatalogueView.xaml";

    private static readonly string[] s_hosts =
    [
        "Module_Setup/Views/SetupDunnageImageSearchDialog.xaml",
        "Module_GenericRequest/Views/GenericRequestDunnagePage.xaml",
    ];

    private static readonly string[] s_automationIds =
    [
        "SetupDunnageImageSearchDialog_FilterInput",
        "SetupDunnageImageSearchDialog_RefreshButton",
        "SetupDunnageImageSearchDialog_ShowAllToggle",
        "SetupDunnageImageSearchDialog_PartTiles",
    ];

    private static string Root => RepositoryPatternScan.FindRepositoryRoot();

    [TestMethod]
    public void Audit_DunnageCatalogueSourceAuditTests_BothHostsDrawTheOneControl()
    {
        foreach (var host in s_hosts)
        {
            var markup = File.ReadAllText(Path.Combine(Root, host.Replace('/', Path.DirectorySeparatorChar)));

            StringAssert.Contains(
                markup,
                "DunnageCatalogueView",
                $"'{host}' no longer hosts the shared catalogue, so it draws a list of its own.");
        }
    }

    [TestMethod]
    public void Audit_DunnageCatalogueSourceAuditTests_NoMarkupOutsideTheControlDeclaresItsAutomationIds()
    {
        var offenders = new List<string>();

        foreach (var file in EnumerateFiles(".xaml"))
        {
            if (string.Equals(file, ControlPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var markup = File.ReadAllText(Path.Combine(Root, file.Replace('/', Path.DirectorySeparatorChar)));
            if (s_automationIds.Any(id => markup.Contains(id, StringComparison.Ordinal)))
            {
                offenders.Add(file);
            }
        }

        Assert.AreEqual(
            0,
            offenders.Count,
            "these files declare the catalogue's own automation ids, so a second catalogue markup has appeared: "
                + string.Join(", ", offenders));
    }

    [TestMethod]
    public void Audit_DunnageCatalogueSourceAuditTests_OnlyTheSharedViewModelLoadsTheCatalogue()
    {
        var offenders = EnumerateFiles(".cs")
            .Where(file => Path.GetFileName(file).EndsWith("ViewModel.cs", StringComparison.OrdinalIgnoreCase))
            .Where(file => !string.Equals(
                Path.GetFileName(file),
                "DunnageCatalogueViewModel.cs",
                StringComparison.OrdinalIgnoreCase))
            .Where(file => File
                .ReadAllText(Path.Combine(Root, file.Replace('/', Path.DirectorySeparatorChar)))
                .Contains("GetAllDunnagePartsAsync", StringComparison.Ordinal))
            .ToList();

        Assert.AreEqual(
            0,
            offenders.Count,
            "these view models read the dunnage catalogue themselves instead of through the shared one: "
                + string.Join(", ", offenders));
    }

    [TestMethod]
    public void Audit_DunnageCatalogueSourceAuditTests_TheSeamProjectsOneTypeOnly()
    {
        var read = typeof(IDunnageCatalogueReader).GetMethod(nameof(IDunnageCatalogueReader.ReadAllAsync));

        Assert.IsNotNull(read, "the seam declares no read, so nothing can be asserted about what it returns.");

        var projected = read!.ReturnType.GetGenericArguments()[0].GetGenericArguments()[0];

        Assert.AreEqual(
            typeof(DunnageCataloguePart),
            projected,
            "the seam returns something other than the shared row, so a second dunnage shape has appeared.");
    }

    /// <summary>Every source file under the shipped trees, relative to the repository root.</summary>
    private static IEnumerable<string> EnumerateFiles(string extension)
    {
        string[] trees =
        [
            "MTM_Waitlist.Core",
            "MTM_Waitlist.Shared",
            "MTM_Waitlist.Setup",
            "MTM_Waitlist.Settings",
            "MTM_Waitlist.Waitlist.View",
            "MTM_Waitlist.Waitlist.NewRequest",
            "MTM_Waitlist.Waitlist.Controls",
            "MTM_Waitlist.Startup",
            "MTM_Waitlist.GenericRequest",
            "MTM_Waitlist.GenericRequest.Data",
            "MTM_Waitlist.GenericRequest.Controls",
            "MTM_Waitlist.Logging",
            "Module_Core",
            "Module_Shared",
            "Module_Setup",
            "Module_GenericRequest",
            "Module_Waitlist",
            "Module_Settings",
            "Module_Startup",
            "Services",
            "ViewModels",
            "Views",
        ];

        foreach (var tree in trees)
        {
            var directory = Path.Combine(Root, tree);
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(directory, "*" + extension, SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                yield return Path.GetRelativePath(Root, file).Replace('\\', '/');
            }
        }
    }
}
