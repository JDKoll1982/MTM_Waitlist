using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Core.Views;

/// <summary>
/// The inventory of every surface matches the files on disk, in both directions (014-adopt-lab-design-system, FR-023,
/// FR-024, FR-025, FR-026).
/// </summary>
/// <remarks>
/// An inventory nobody checks is a list that was right once. These tests read
/// <c>docs/guides/guides-xaml-surface-inventory.md</c> and the repository at the same time: a file added without a row
/// fails, a row with no file fails, and a row whose kind disagrees with the file it names fails.
/// </remarks>
[TestClass]
public sealed class SurfaceInventoryAuditTests
{
    private const string InventoryPath = "docs/guides/guides-xaml-surface-inventory.md";

    /// <summary>One row of the inventory table.</summary>
    private static readonly Regex s_row = new(
        "^\\|\\s*(?<kind>[A-Za-z]+)\\s*\\|\\s*(?<area>[^|]+?)\\s*\\|\\s*(?<surface>[^|]+?)\\s*\\|\\s*`(?<path>[^`]+)`\\s*\\|\\s*(?<modal>yes|no)\\s*\\|\\s*$",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.CultureInvariant);

    [TestMethod]
    public void Audit_SurfaceInventoryAuditTests_EveryXamlFileIsListedOnce()
    {
        var listed = ListedPaths();
        var onDisk = XamlFilesOnDisk();

        var missing = onDisk.Where(file => !listed.Contains(file, StringComparer.OrdinalIgnoreCase)).ToList();
        var stale = listed.Where(file => !onDisk.Contains(file, StringComparer.OrdinalIgnoreCase)).ToList();

        Assert.AreEqual(
            0,
            missing.Count,
            "these XAML files are not in the inventory, so nobody has checked them against the rules: "
                + string.Join(", ", missing));

        Assert.AreEqual(
            0,
            stale.Count,
            "these inventory rows name files that are no longer there: " + string.Join(", ", stale));

        Assert.AreEqual(
            onDisk.Count,
            listed.Count,
            "a file is listed more than once, so the inventory and the repository no longer describe the same set");
    }

    [TestMethod]
    public void Audit_SurfaceInventoryAuditTests_EveryDialogIsModalAndEveryModalIsADialog()
    {
        foreach (var (kind, path, modal) in Rows())
        {
            Assert.AreEqual(
                kind == "Dialog",
                modal,
                $"'{path}' is listed as a {kind} with modal={modal}, which contradicts itself: a modal surface is a dialog "
                    + "and a dialog is modal, because it opens in its own window over the window that raised it");
        }
    }

    [TestMethod]
    public void Audit_SurfaceInventoryAuditTests_EveryResourceDictionaryDeclaresNoType()
    {
        foreach (var (kind, path, _) in Rows().Where(row => row.Kind == "ResourceDictionary"))
        {
            var markup = Read(path);

            Assert.IsFalse(
                markup.Contains("x:Class=", StringComparison.Ordinal),
                $"'{path}' is listed as a styles file but declares a type, so it is a visual surface wearing a "
                    + "dictionary's name");
        }
    }

    [TestMethod]
    public void Audit_SurfaceInventoryAuditTests_EveryDialogBodyIsHostable()
    {
        foreach (var (_, path, _) in Rows().Where(row => row.Kind == "Dialog"))
        {
            var markup = Read(path);

            Assert.IsFalse(
                markup.TrimStart().Contains("<ContentDialog", StringComparison.Ordinal),
                $"'{path}' is a dialog that still declares itself as the platform dialog, so it cannot be presented in its "
                    + "own window the way FR-001 requires");

            StringAssert.Contains(
                Read(path + ".cs"),
                "ShowWindowedAsync",
                $"'{path}' has no way to be opened in its own window");
        }
    }

    [TestMethod]
    public void Audit_SurfaceInventoryAuditTests_TheInventoryListsTheServiceConsole()
    {
        var paths = ListedPaths();

        Assert.IsTrue(
            paths.Any(path => path.StartsWith("MTM_Waitlist.Mock.Service/", StringComparison.OrdinalIgnoreCase)),
            "the service console's own window and pages are surfaces too, and FR-026 allows no exclusion for them");
    }

    private static IReadOnlyList<(string Kind, string Path, bool Modal)> Rows()
    {
        var inventory = Read(InventoryPath);
        var rows = new List<(string, string, bool)>();

        foreach (Match match in s_row.Matches(inventory))
        {
            rows.Add((
                match.Groups["kind"].Value.Trim(),
                match.Groups["path"].Value.Trim(),
                string.Equals(match.Groups["modal"].Value.Trim(), "yes", StringComparison.Ordinal)));
        }

        Assert.IsTrue(rows.Count > 0, "the inventory lists no surfaces at all, which cannot be right");

        return rows;
    }

    private static HashSet<string> ListedPaths() =>
        Rows().Select(row => row.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static HashSet<string> XamlFilesOnDisk()
    {
        var root = RepositoryPatternScan.FindRepositoryRoot();
        var scope = new RepositoryScanScope { Extensions = [".xaml"] };

        return RepositoryPatternScan
            .EnumerateScannedFiles(root, scope)
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string Read(string relativePath)
    {
        var path = Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));

        Assert.IsTrue(File.Exists(path), $"the file this audit reads is not where it was: {path}");

        return File.ReadAllText(path);
    }
}
