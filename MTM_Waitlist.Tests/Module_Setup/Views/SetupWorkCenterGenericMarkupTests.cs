using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Setup.Views;

/// <summary>
/// The generic designation is offered on the work center step under the permission that already governs it, and no
/// other way (016-generic-wc-request-foundation, FR-001, FR-002).
/// </summary>
/// <remarks>
/// <para>
/// FR-002 says the control must be <b>absent</b> for anyone without <c>permission.setup.work_centers</c>, not disabled:
/// a greyed-out control still tells an unauthorised person the setting exists. That is a property of the markup, and
/// a binding is the only thing that can express it, so it is asserted here rather than left to a manual pass.
/// </para>
/// <para>
/// The second test pins that the designation is written through its own procedure. Routing it through the work-center
/// upsert would have made every designation change carry the row's name and building, so a save could rename a work
/// center as a side effect of marking it generic.
/// </para>
/// </remarks>
[TestClass]
public sealed class SetupWorkCenterGenericMarkupTests
{
    private const string PagePath = "Module_Setup/Views/SetupWorkCenterPage.xaml";
    private const string ServicePath = "MTM_Waitlist.Setup/Services/SetupWorkCenterService.cs";

    private static string Root => RepositoryPatternScan.FindRepositoryRoot();

    [TestMethod]
    public void Audit_SetupWorkCenterGenericMarkupTests_TheDesignationControlIsAbsentWithoutThePermission()
    {
        var markup = Read(PagePath);
        var toggle = Around(markup, "SetupWorkCenterPage_GenericToggle");

        StringAssert.Contains(
            toggle,
            "Visibility=\"{x:Bind ViewModel.CanManageWorkCenters",
            "the generic toggle is not bound to the work-center permission, so an unauthorised person would see it");

        Assert.IsFalse(
            Regex.IsMatch(toggle, @"IsEnabled=""\{x:Bind ViewModel\.CanManageWorkCenters", RegexOptions.CultureInvariant),
            "a disabled control is not absent: FR-002 requires the control to be gone, not greyed out");

        StringAssert.Contains(
            toggle,
            "IsOn=\"{x:Bind ViewModel.SelectedWorkCenterIsGeneric",
            "the toggle does not carry the designation it is there to change");
    }

    [TestMethod]
    public void Audit_SetupWorkCenterGenericMarkupTests_TheBadgeIsReadOnlyInformation()
    {
        var markup = Read(PagePath);

        // The badge sits inside the card template, which is the item's own view: it declares no command and no click
        // handler, because the only control that changes the designation is the toggle on the page.
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
    /// The markup around the element carrying one automation id, so an attribute on it can be asserted without
    /// depending on how the element is laid out across lines.
    /// </summary>
    private static string Around(string markup, string automationId)
    {
        var index = markup.IndexOf($"AutomationId=\"{automationId}\"", StringComparison.Ordinal);
        Assert.IsTrue(index >= 0, $"'{automationId}' is not in {PagePath}");

        var start = Math.Max(0, index - 400);
        var end = Math.Min(markup.Length, index + 600);

        return markup[start..end];
    }

    private static string Read(string relativePath)
    {
        var path = Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));

        Assert.IsTrue(File.Exists(path), $"the file this audit reads is not where it was: {path}");

        return File.ReadAllText(path);
    }
}
