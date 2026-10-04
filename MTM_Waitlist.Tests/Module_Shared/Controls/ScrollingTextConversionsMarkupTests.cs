using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Shared.Controls;

/// <summary>
/// The surfaces that switched from a wrapping or truncated <c>TextBlock</c> to the one-line scrolling control
/// (<c>SCROLLING-TEXT-CANDIDATES.md</c>, group 1; 009 B1/B2). The inventory named each element by file and binding,
/// and this is that list as a guard: a later edit that hand-rolls the value back into a <c>TextBlock</c> — the wrap
/// that pushed a card out of shape, or the cut-off line with no way to read it — fails here rather than quietly
/// undoing the conversion.
/// </summary>
/// <remarks>
/// Only the elements the inventory actually listed are asserted. The same binding can legitimately still be drawn by
/// a <c>TextBlock</c> elsewhere in a file (a field label, a prose sentence, a value that is meant to wrap), so the
/// check is per file and per binding, never a blanket ban on either type.
/// </remarks>
[TestClass]
public sealed class ScrollingTextConversionsMarkupTests
{
    /// <summary>
    /// Each file the inventory converted, and the bindings that are now drawn by the scrolling control in it.
    /// </summary>
    private static readonly (string File, string[] Bindings)[] s_converted =
    [
        (
            "Module_Setup/Views/SetupWorkCenterPage.xaml",
            [
                "{x:Bind Name}",
                "{x:Bind Building}",
                "{x:Bind CurrentJobSummary}",
                "{x:Bind CurrentPartSummary}",
                "{x:Bind LastUpdatedDisplay}",
            ]),
        ("Module_Setup/Views/SetupWorkOrderPage.xaml", ["{x:Bind Description}"]),
        ("Module_Setup/Views/SetupReviewPage.xaml", ["{x:Bind Description}", "{x:Bind DisplayName}"]),
        ("Module_Setup/Views/SetupPartSelectionPage.xaml", ["{x:Bind Description}"]),
        ("Module_Setup/Views/SetupSequenceSelectionPage.xaml", ["{x:Bind Description}"]),
        ("Module_Setup/Views/SetupDunnageImageSearchDialog.xaml", ["{x:Bind DunnageTypeName}"]),
        (
            "Module_Waitlist/Views/NewRequestWorkCenterPage.xaml",
            [
                "{x:Bind WorkCenterName}",
                "{x:Bind Building}",
                "{x:Bind CurrentJobSummary}",
                "{x:Bind CurrentPartSummary}",
                "{x:Bind LastUpdatedDisplay}",
            ]),
        ("Module_Waitlist/Views/NewRequestComponentPage.xaml", ["{x:Bind Title}"]),
        ("Module_Waitlist/Views/NewRequestDunnagePage.xaml", ["{x:Bind Title}", "{x:Bind Summary}"]),
        ("Module_Waitlist/Views/NewRequestDiePage.xaml", ["{x:Bind Title}", "{x:Bind Summary}"]),
        ("Module_Waitlist/Views/NewRequestItemPage.xaml", ["{Binding DisplayName}", "{Binding Summary}"]),
        ("Module_Waitlist/Views/NewRequestJobTypePage.xaml", ["{Binding Name}", "{Binding Summary}"]),
        (
            "Module_Settings/Views/PartPictureManagerPage.xaml",
            ["{x:Bind Title}", "{x:Bind Detail}", "{x:Bind RelativePath}"]),
        (
            "Module_Settings/Views/PermissionHoldersView.xaml",
            [
                "{x:Bind}",
                "{x:Bind DisplayName}",
                "{x:Bind RoleText}",
                "{x:Bind MarkText}",
                "{x:Bind SwitchedOffText}",
            ]),
        (
            "Module_Settings/Views/PermissionsPage.xaml",
            [
                "{x:Bind PendingText, Mode=OneWay}",
                "{x:Bind AreaHeadingText}",
                "{x:Bind LabelText}",
                "{x:Bind GatesText}",
                "{x:Bind Person.SignInName}",
                "{x:Bind Person.RoleText}",
                "{x:Bind Person.PendingText, Mode=OneWay}",
                "{x:Bind Person.LockReasonText}",
            ]),
        (
            "Module_Settings/Views/UserManagementPage.xaml",
            ["{x:Bind UsernameNormalized}", "{x:Bind EmployeeIdentifier}", "{x:Bind RoleText}"]),
        (
            "Module_Settings/Views/ImageOverrideEditorControl.xaml",
            ["{x:Bind DisplayName}", "{x:Bind WarningMessage, Mode=OneWay}"]),
        ("Module_Core/Views/ShellPage.xaml", ["{Binding CurrentUserDisplayName}"]),
    ];

    /// <summary>
    /// Every converted value is drawn by the scrolling control. The control gets its text from the same binding the
    /// <c>TextBlock</c> had, so the surface keeps reading from the same model property.
    /// </summary>
    [TestMethod]
    public void EveryConvertedValue_IsDrawnByTheScrollingControl()
    {
        var missing = new List<string>();

        foreach (var (file, bindings) in s_converted)
        {
            var elements = Load(file);
            var drawn = TextOf(elements, "AutoScrollTextView");

            missing.AddRange(bindings
                .Where(binding => !drawn.Contains(binding, StringComparer.Ordinal))
                .Select(binding => $"{file}: {binding} is not drawn by the scrolling control"));
        }

        Assert.AreEqual(
            0,
            missing.Count,
            "These values were converted to the one-line scrolling control and are no longer drawn by it:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, missing));
    }

    /// <summary>
    /// Not one of the converted values is drawn by a <c>TextBlock</c> any more — the whole point of the conversion
    /// is that the wrap and the ellipsis are gone from these lines.
    /// </summary>
    [TestMethod]
    public void NoConvertedValue_IsDrawnByATextBlockAnyMore()
    {
        var left = new List<string>();

        foreach (var (file, bindings) in s_converted)
        {
            var lines = TextOf(Load(file), "TextBlock");

            left.AddRange(bindings
                .Where(binding => lines.Contains(binding, StringComparer.Ordinal))
                .Select(binding => $"{file}: a TextBlock still draws {binding}"));
        }

        Assert.AreEqual(
            0,
            left.Count,
            "These lines are drawn by a TextBlock again, which is what wrapped or cut the value off:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, left));
    }

    /// <summary>The text a control or a line draws, from its own <c>Text</c> attribute.</summary>
    private static List<string> TextOf(XDocument document, string localName) =>
        document
            .Descendants()
            .Where(element => element.Name.LocalName == localName)
            .Select(element => (string?)element.Attribute("Text") ?? string.Empty)
            .ToList();

    private static XDocument Load(string relativePath)
    {
        var path = Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));

        Assert.IsTrue(File.Exists(path), $"The markup was not found at '{path}'.");

        return XDocument.Load(path);
    }
}
