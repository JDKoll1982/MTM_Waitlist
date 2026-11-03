using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Setup.Views;

/// <summary>
/// The generic designation is offered inside the work center's own Add / Edit form, under the permission that already
/// governs it, and no other way (016-generic-wc-request-foundation, FR-001, FR-002).
/// </summary>
/// <remarks>
/// <para>
/// FR-002 says the control must be <b>absent</b> for anyone without <c>permission.setup.work_centers</c>, not disabled:
/// a greyed-out control still tells an unauthorised person the setting exists. The dialog is built in code-behind, so
/// what enforces it is the permission check each handler runs before it creates the control, and that is what is
/// asserted here rather than left to a manual pass.
/// </para>
/// <para>
/// The last test pins that the designation is written through its own procedure. Routing it through the work-center
/// upsert would have made every designation change carry the row's name and building, so a save could rename a work
/// center as a side effect of marking it generic.
/// </para>
/// </remarks>
[TestClass]
public sealed class SetupWorkCenterGenericMarkupTests
{
    private const string PagePath = "Module_Setup/Views/SetupWorkCenterPage.xaml";
    private const string PageCodeBehindPath = "Module_Setup/Views/SetupWorkCenterPage.xaml.cs";
    private const string ServicePath = "MTM_Waitlist.Setup/Services/SetupWorkCenterService.cs";

    private static string Root => RepositoryPatternScan.FindRepositoryRoot();

    [TestMethod]
    public void Audit_SetupWorkCenterGenericMarkupTests_TheDesignationIsOfferedInsideTheWorkCenterForm()
    {
        // FR-002 says the control must be absent, not disabled, for anyone without permission.setup.work_centers. The
        // designation is now part of the Add / Edit work center dialog, and both handlers that build that dialog
        // return before constructing it unless the permission is held — so the control is never created for someone
        // who may not change it, which is the same guarantee the page-level visibility binding used to carry.
        var code = Read(PageCodeBehindPath);
        var markup = Read(PagePath);

        Assert.IsFalse(
            markup.Contains("SetupWorkCenterPage_GenericToggle", StringComparison.Ordinal),
            "the page still carries a generic toggle beside the work center list");

        foreach (var handler in new[] { "OnNewWorkCenterClick(object sender", "OnEditWorkCenterClick(object sender" })
        {
            var body = Method(code, handler);
            var permission = body.IndexOf("!ViewModel.CanManageWorkCenters", StringComparison.Ordinal);
            var control = body.IndexOf("CreateGenericInput(", StringComparison.Ordinal);

            Assert.IsTrue(
                permission >= 0,
                $"{handler} does not refuse before it builds the designation control");

            Assert.IsTrue(
                control >= 0,
                $"{handler} does not offer the designation when it saves a work center");

            Assert.IsTrue(
                permission < control,
                $"{handler} builds the designation control before it checks the permission");
        }

        StringAssert.Contains(
            Method(code, "CreateGenericInput(bool isGeneric)"),
            "IsOn = isGeneric",
            "the designation control does not carry the value it is there to change");
    }

    [TestMethod]
    public void Audit_SetupWorkCenterGenericMarkupTests_TheBadgeIsReadOnlyInformation()
    {
        var markup = Read(PagePath);

        // The badge sits inside the card template, which is the item's own view: it declares no command and no click
        // handler, because the only control that changes the designation is the one inside the Add / Edit form.
        StringAssert.Contains(markup, "x:Uid=\"Setup_WorkCenter_GenericBadge\"", "the card carries no generic marker");
        StringAssert.Contains(
            markup,
            "Visibility=\"{x:Bind IsGeneric, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}\"",
            "the marker is not shown only when the work center is generic");
    }

    [TestMethod]
    public void Audit_SetupWorkCenterGenericMarkupTests_TheDesignationUsesItsOwnWrite()
    {
        var source = Read(ServicePath);

        StringAssert.Contains(
            source,
            "sp_setup_work_centers_generic_set",
            "the designation write does not use its own procedure");
    }

    [TestMethod]
    public void Audit_SetupWorkCenterGenericMarkupTests_NoResourceKeyIsReadAsAPropertyTheControlLacks()
    {
        // x:Uid reads every resource key under an element's uid as a property to set on that element at load time. A
        // key whose suffix names no property on the control throws in InitializeComponent, so the page never loads and
        // the shell silently stays where it was — which is exactly how a resource key named "...GenericToggle.None"
        // broke the work-center screen. This pins the suffixes this page may use.
        var allowedSuffixes = new HashSet<string>(StringComparer.Ordinal)
        {
            "Text",
            "Content",
            "Header",
            "PlaceholderText",
            "OnContent",
            "OffContent",
            "AutomationProperties.Name",
        };

        var pageUids = Regex
            .Matches(Read(PagePath), "x:Uid=\"(?<uid>[^\"]+)\"", RegexOptions.CultureInvariant)
            .Select(match => match.Groups["uid"].Value)
            .ToHashSet(StringComparer.Ordinal);

        Assert.IsTrue(pageUids.Count > 0, "the page declares no resource uids at all, so this audit would check nothing");

        var resourceKeys = Regex
            .Matches(Read("Strings/en-us/Resources.resw"), "<data name=\"(?<key>[^\"]+)\"", RegexOptions.CultureInvariant)
            .Select(match => match.Groups["key"].Value);

        var offenders = new List<string>();

        foreach (var key in resourceKeys)
        {
            foreach (var uid in pageUids.Where(uid => key.StartsWith(uid + ".", StringComparison.Ordinal)))
            {
                var suffix = key[(uid.Length + 1)..];

                if (!allowedSuffixes.Contains(suffix))
                {
                    offenders.Add($"{key} (uid {uid} would be asked for a '{suffix}' property)");
                }
            }
        }

        Assert.AreEqual(
            0,
            offenders.Count,
            "these resource keys would be applied as properties the control does not have, which stops the page from "
                + "loading: " + string.Join(", ", offenders));
    }

    /// <summary>
    /// One method's source, from its signature to its closing brace, so an assertion can be made about what that
    /// method does without depending on where it sits in the file.
    /// </summary>
    /// <param name="source">The file to search.</param>
    /// <param name="signature">
    /// Enough of the declaration to be unambiguous — call sites carry the bare name, so the parameter list is what
    /// tells the declaration apart from the places it is called.
    /// </param>
    private static string Method(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, $"no declaration of '{signature}' in {PageCodeBehindPath}");

        var open = source.IndexOf('{', start);
        Assert.IsTrue(open >= 0, $"'{signature}' has no body in {PageCodeBehindPath}");

        var depth = 0;
        for (var index = open; index < source.Length; index++)
        {
            if (source[index] == '{')
            {
                depth++;
            }
            else if (source[index] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source[start..(index + 1)];
                }
            }
        }

        Assert.Fail($"'{signature}' has no balanced body in {PageCodeBehindPath}");

        return string.Empty;
    }

    private static string Read(string relativePath)
    {
        var path = Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));

        Assert.IsTrue(File.Exists(path), $"the file this audit reads is not where it was: {path}");

        return File.ReadAllText(path);
    }
}
