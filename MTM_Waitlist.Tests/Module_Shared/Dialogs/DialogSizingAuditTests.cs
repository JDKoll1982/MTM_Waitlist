using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Shared.Dialogs;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Shared.Dialogs;

/// <summary>
/// Every dialog takes its size from one declared scale (014-adopt-lab-design-system, FR-003, FR-004, FR-026).
/// </summary>
/// <remarks>
/// <para>
/// <b>The defect this pins.</b> Before this change a dialog could declare a width of its own, and they did: 520, 640,
/// 720, 920 and 1000 pixels, most of them also overriding the platform keys that exist to cap a dialog. On a 15-inch
/// screen at 150% scaling a 1000-wide dialog is wider than the window that owns it, so it opened over the window's
/// edges and its own buttons sat off-screen.
/// </para>
/// <para>
/// The rule is one scale, read through <see cref="DialogSizing"/>. This audit reads both sides — the declared resource
/// and the numbers in code — so the two cannot drift apart silently, and it reads the top of every dialog body, which is
/// where a dialog's own size is declared. A size on a control inside the body (a 24-pixel progress ring, a 96-pixel
/// thumbnail, a grid column) is that control's, not the dialog's, and is left alone.
/// </para>
/// </remarks>
[TestClass]
public sealed class DialogSizingAuditTests
{
    /// <summary>Every XAML file that is the body of a dialog.</summary>
    private static readonly string[] s_dialogBodies =
    [
        "MTM_Waitlist.Shared/Dialogs/MessageDialogContent.xaml",
        "Module_Settings/Views/ComputerEditDialog.xaml",
        "Module_Settings/Views/LuggerEditorDialog.xaml",
        "Module_Settings/Views/LuggerNameDialog.xaml",
        "Module_Settings/Views/PinRevealDialog.xaml",
        "Module_Settings/Views/RequestItemImagesDialog.xaml",
        "Module_Settings/Views/WorkCenterImagesDialog.xaml",
        "Module_Setup/Views/SetupDunnageImageSearchDialog.xaml",
        "Module_Setup/Views/SetupWorkOrderSearchDialog.xaml",
        "Module_Waitlist/Views/PartStockDialog.xaml",
    ];

    /// <summary>The attributes that decide how large a whole dialog opens.</summary>
    private static readonly string[] s_sizeAttributes = ["Width", "Height", "MinWidth", "MinHeight", "MaxWidth", "MaxHeight"];

    [TestMethod]
    public void Audit_DialogSizingAuditTests_TheScaleIsDeclaredOnce()
    {
        var dictionary = Read("Styles", "Dialogs.xaml");

        Assert.AreEqual(480, Declared(dictionary, "DialogMinWidth"));
        Assert.AreEqual(720, Declared(dictionary, "DialogPreferredWidth"));
        Assert.AreEqual(880, Declared(dictionary, "DialogMaxWidth"));
        Assert.AreEqual(640, Declared(dictionary, "DialogMaxBodyHeight"));
        Assert.AreEqual(120, Declared(dictionary, "DialogBodyMinHeight"));
    }

    [TestMethod]
    public void Audit_DialogSizingAuditTests_TheCodeAndTheDictionaryAgree()
    {
        var dictionary = Read("Styles", "Dialogs.xaml");

        Assert.AreEqual(DialogSizing.MinWidthFallback, Declared(dictionary, "DialogMinWidth"));
        Assert.AreEqual(DialogSizing.PreferredWidthFallback, Declared(dictionary, "DialogPreferredWidth"));
        Assert.AreEqual(DialogSizing.MaxWidthFallback, Declared(dictionary, "DialogMaxWidth"));
        Assert.AreEqual(DialogSizing.MaxBodyHeightFallback, Declared(dictionary, "DialogMaxBodyHeight"));
        Assert.AreEqual(DialogSizing.BodyMinHeightFallback, Declared(dictionary, "DialogBodyMinHeight"));
    }

    [TestMethod]
    public void Audit_DialogSizingAuditTests_ThePreferredWidthFitsTheSmallestSupportedWindow()
    {
        // A 15-inch display at 150% scaling is about 1280 by 720 effective pixels. A dialog wider than that cannot be
        // shown whole, and one taller than the window leaves its buttons below the screen edge.
        Assert.IsTrue(DialogSizing.PreferredWidth <= 1280, "the width a dialog opens at does not fit the smallest supported screen.");
        Assert.IsTrue(DialogSizing.MaxWidth <= 1280, "a dialog may grow wider than the smallest supported screen.");
        Assert.IsTrue(DialogSizing.MaxBodyHeight < 720, "a dialog's body may grow taller than the smallest supported screen.");
        Assert.IsTrue(DialogSizing.MinWidth <= DialogSizing.PreferredWidth, "a dialog's floor is above the width it opens at.");
        Assert.IsTrue(DialogSizing.PreferredWidth <= DialogSizing.MaxWidth, "a dialog opens wider than it is allowed to grow.");
    }

    [TestMethod]
    public void Audit_DialogSizingAuditTests_NoDialogDeclaresASizeOfItsOwn()
    {
        var offenders = new List<string>();

        foreach (var body in s_dialogBodies)
        {
            var root = Load(body);

            offenders.AddRange(NumericSizes(root).Select(size => $"{body}: <{root.Name.LocalName}> {size}"));

            if (FirstElementChild(root) is { } child)
            {
                offenders.AddRange(NumericSizes(child).Select(size => $"{body}: <{child.Name.LocalName}> {size}"));
            }
        }

        Assert.AreEqual(
            0,
            offenders.Count,
            "a dialog that declares its own width opens larger than the window that owns it, and every one of these "
                + "numbers is a second copy of a scale that is meant to be declared once: "
                + string.Join(", ", offenders));
    }

    [TestMethod]
    public void Audit_DialogSizingAuditTests_EveryDialogBodyTakesItsSizeFromTheScale()
    {
        var offenders = s_dialogBodies
            .Where(body => !Read(RelativeCodeBehind(body)).Contains("DialogSizing.", StringComparison.Ordinal))
            .ToList();

        Assert.AreEqual(
            0,
            offenders.Count,
            "a dialog body that never reads DialogSizing is choosing its own size, or has none: "
                + string.Join(", ", offenders));
    }

    private static IEnumerable<string> NumericSizes(XElement element) =>
        element
            .Attributes()
            .Where(attribute => s_sizeAttributes.Contains(attribute.Name.LocalName, StringComparer.Ordinal))
            .Where(attribute => double.TryParse(attribute.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            .Select(attribute => $"{attribute.Name.LocalName}={attribute.Value}");

    private static XElement? FirstElementChild(XElement root) =>
        root.Elements().FirstOrDefault();

    private static string RelativeCodeBehind(string xamlPath) => xamlPath + ".cs";

    private static double Declared(string dictionary, string key)
    {
        var match = Regex.Match(
            dictionary,
            "<x:Double x:Key=\"" + Regex.Escape(key) + "\">([0-9.]+)</x:Double>",
            RegexOptions.CultureInvariant);

        Assert.IsTrue(match.Success, $"the dialog scale does not declare {key}.");

        return double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
    }

    private static XElement Load(string xamlPath) =>
        XDocument.Parse(Read(xamlPath.Split('/'))).Root
        ?? throw new AssertFailedException($"the dialog body has no root element: {xamlPath}");

    private static string Read(params string[] parts)
    {
        var path = Path.Combine(new[] { RepositoryPatternScan.FindRepositoryRoot() }.Concat(parts).ToArray());

        Assert.IsTrue(File.Exists(path), $"the file this audit reads is not where it was: {path}");

        return File.ReadAllText(path);
    }
}
