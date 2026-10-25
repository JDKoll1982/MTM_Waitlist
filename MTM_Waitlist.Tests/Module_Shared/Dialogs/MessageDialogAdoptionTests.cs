using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Shared.Dialogs;

/// <summary>
/// No screen builds a dialog of its own (014-adopt-lab-design-system, FR-001, FR-006, FR-012, FR-026).
/// </summary>
/// <remarks>
/// <para>
/// <b>What this pins.</b> Before this change eighteen dialogs were built in fourteen files, each choosing its own
/// button order, its own default and its own size. That is how one screen came to make its destructive answer the
/// default and another came to open a dialog wider than the window it belonged to.
/// </para>
/// <para>
/// One place builds a dialog now — <c>WindowedDialogHost</c> in the shared library — and every other surface either
/// asks through <c>MessageDialogHost</c> or hands its own body to the host. This audit reads the source, because a
/// dialog built at runtime is invisible to a test that only runs pure code.
/// </para>
/// </remarks>
[TestClass]
public sealed class MessageDialogAdoptionTests
{
    /// <summary>The one file allowed to construct a dialog window.</summary>
    private const string TheHostPath = "MTM_Waitlist.Shared/Dialogs/WindowedDialogHost.cs";

    [TestMethod]
    public void Audit_MessageDialogAdoptionTests_NoProductionFileBuildsAContentDialog()
    {
        var offenders = new List<string>();

        foreach (var file in EnumerateProductionSources())
        {
            var text = File.ReadAllText(file);

            var buildsOne = text.Contains("new ContentDialog", StringComparison.Ordinal)
                || (file.EndsWith(".xaml", StringComparison.Ordinal)
                    && text.Contains("<ContentDialog", StringComparison.Ordinal));

            if (buildsOne)
            {
                offenders.Add(Relative(file));
            }
        }

        Assert.AreEqual(
            0,
            offenders.Count,
            "these files build a dialog of their own instead of asking through the shared set, so their button order, "
                + "their default and their size are their own again: "
                + string.Join(", ", offenders));
    }

    [TestMethod]
    public void Audit_MessageDialogAdoptionTests_OnlyTheHostBuildsADialogWindow()
    {
        var offenders = new List<string>();

        foreach (var file in EnumerateProductionSources().Where(f => f.EndsWith(".cs", StringComparison.Ordinal)))
        {
            var relative = Relative(file);

            if (string.Equals(relative, TheHostPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var text = File.ReadAllText(file);

            // Referring to the result is fine; constructing the window is not.
            if (text.Contains("new DevWinUI.WindowedContentDialog", StringComparison.Ordinal)
                || text.Contains("new WindowedContentDialog", StringComparison.Ordinal))
            {
                offenders.Add(relative);
            }
        }

        Assert.AreEqual(
            0,
            offenders.Count,
            "a dialog window is built in one place so its owner and its default can never be forgotten: "
                + string.Join(", ", offenders));
    }

    [TestMethod]
    public void Audit_MessageDialogAdoptionTests_TheHostIsReachableFromEveryModule()
    {
        var markup = File.ReadAllText(Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), "MTM_Waitlist.Shared", "Dialogs", "MessageDialogHost.cs"));

        StringAssert.Contains(
            markup,
            "public static async Task<MessageDialogOutcome> AskAsync(Window owner, MessageDialogRequest request)",
            "the one entry point every caller uses, taking the owning window and the request");
    }

    [TestMethod]
    public void Audit_MessageDialogAdoptionTests_TheStandardSetIsDeclaredOnce()
    {
        var declarations = EnumerateProductionSources()
            .Where(f => f.EndsWith(".cs", StringComparison.Ordinal))
            .Count(f => File.ReadAllText(f).Contains("enum MessageKind", StringComparison.Ordinal));

        Assert.AreEqual(1, declarations, "a second copy of the standard set would let one module answer differently from the rest");
    }

    private static IEnumerable<string> EnumerateProductionSources()
    {
        var root = RepositoryPatternScan.FindRepositoryRoot();
        var scope = new RepositoryScanScope();

        return RepositoryPatternScan
            .EnumerateScannedFiles(root, scope)
            .Where(f => f.EndsWith(".cs", StringComparison.Ordinal) || f.EndsWith(".xaml", StringComparison.Ordinal))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}MTM_Waitlist.Tests{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));
    }

    private static string Relative(string path) =>
        Path.GetRelativePath(RepositoryPatternScan.FindRepositoryRoot(), path).Replace('\\', '/');
}
