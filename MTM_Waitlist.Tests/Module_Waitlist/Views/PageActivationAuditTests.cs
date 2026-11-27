using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Waitlist.Views;

/// <summary>
/// The page-activation gate: every navigable page must be constructible by the framework, because
/// <c>Frame.Navigate(pageType, parameter, transition)</c> activates a page through its XAML type and has no
/// way to supply a constructor argument.
/// </summary>
/// <remarks>
/// <para>
/// This guard exists because the unified card / item picker shipped a fatal crash that every other gate was
/// blind to. <c>NewRequestItemPage</c> declared only
/// <c>public NewRequestItemPage(NewRequestItemViewModel viewModel)</c>, so choosing a Category tile raised
/// <c>STATUS_STOWED_EXCEPTION</c> (<c>0xC000027B</c>) inside the <c>ItemClick</c> handler and killed the
/// process — with no WER report and no application log. The build was clean and the suite was green, because
/// nothing in it ever asked whether a page could actually be activated. A page's constructor shape is not a
/// compile-time contract, so it is enforced here instead.
/// </para>
/// <para>
/// <b>Read as source, not by reflection.</b> The pages live in the WinUI application project
/// (<c>MTM_Waitlist.csproj</c>), which the test project deliberately does not reference — it is
/// <c>UseWinUI=false</c> so it can run headless. Markup and policy assertions in this suite read XAML and
/// resources as text for the same reason, and the same approach works here.
/// </para>
/// <para>
/// <b>Two exempt categories, each pinned.</b> <c>ShellPage</c> is the shell host: it is constructed with its view
/// model and is never a <c>Frame.Navigate</c> target — that is the point of it, since it is what assigns
/// <c>NavigationService.Frame</c>. The launch surfaces (<c>ILaunchStageSurface</c>) are the other category: the
/// host builds each of them and sets it as the one window's content, so none of them is ever activated by a
/// frame. Both sets are pinned, so a second parameterised page cannot slip in unnoticed: adding one means editing
/// a list here, and the comment there has to say why it is not navigable.
/// </para>
/// </remarks>
[TestClass]
public sealed class PageActivationAuditTests
{
    /// <summary>The page types the wizard must be able to activate, in the order the flow visits them.</summary>
    private static readonly string[] s_newRequestWizardPages =
    [
        "NewRequestWorkCenterPage",
        "NewRequestJobTypePage",
        "NewRequestItemPage",
        "NewRequestDetailsPage",
        "NewRequestSummaryPage",
        "NewRequestResultPage",
    ];

    /// <summary>Page types that legitimately have no parameterless constructor, and why.</summary>
    private static readonly string[] s_pagesConstructedWithDependencies =
    [
        // The shell host. It is constructed with its view model and is never navigated to; it is the thing
        // that hands NavigationService its frame.
        "ShellPage",
    ];

    /// <summary>
    /// The launch surfaces: pages the host constructs with what they need and sets as the one window's content.
    /// </summary>
    /// <remarks>
    /// A launch surface is never a <c>Frame.Navigate</c> target, so the framework never has to activate one: the
    /// host resolves the state each of them drives and hands it over, which is what keeps a stage's view model out
    /// of reach of the page's own details. The set is pinned like the exemption above, so a fifth surface — or a
    /// stage that quietly became navigable — has to be reviewed here rather than pass unnoticed.
    /// </remarks>
    private static readonly string[] s_launchStagePages =
    [
        "SplashPage",
        "SignInPage",
        "MachineSetupPage",
        "BlockedStatePage",
    ];

    /// <summary>Matches a view class declaration and captures its name and base-type list.</summary>
    private static readonly Regex s_classDeclaration = new(
        @"\bclass\s+(?<name>\w+)\s*:\s*(?<bases>[^\r\n{]+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [TestMethod]
    public void Audit_PageActivationAuditTests_EveryPageInTheViewsFolders_DeclaresAParameterlessConstructor()
    {
        var repositoryRoot = RepositoryPatternScan.FindRepositoryRoot();
        var violations = new List<string>();
        var exempted = new List<string>();
        var launchSurfaces = new List<string>();

        foreach (var (relativePath, className, isLaunchStage) in EnumeratePageClasses(repositoryRoot))
        {
            if (s_pagesConstructedWithDependencies.Contains(className, StringComparer.Ordinal))
            {
                exempted.Add(className);
                continue;
            }

            if (isLaunchStage)
            {
                launchSurfaces.Add(className);
                continue;
            }

            var source = File.ReadAllText(Path.Combine(repositoryRoot, relativePath));
            if (!DeclaresParameterlessConstructor(source, className))
            {
                violations.Add(
                    $"{relativePath}: {className} declares no public parameterless constructor. "
                    + "Frame.Navigate activates a page through its XAML type and cannot pass an argument, so "
                    + "this page throws STATUS_STOWED_EXCEPTION the moment it is navigated to. Resolve the "
                    + "view model through App.GetService<T>() in a parameterless constructor, as every sibling "
                    + "page does.");
            }
        }

        Assert.AreEqual(
            0,
            violations.Count,
            "Every navigable page must be constructible by the framework:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, violations));

        CollectionAssert.AreEquivalent(
            s_pagesConstructedWithDependencies,
            exempted,
            "The exemption list is pinned to the pages that are genuinely constructed with dependencies. If a "
                + "page was deleted the list is stale; if a page was added it must justify itself here rather "
                + "than escape the audit.");

        CollectionAssert.AreEquivalent(
            s_launchStagePages,
            launchSurfaces,
            "The launch surfaces are pinned to the pages the host builds and sets as the one window's content. If "
                + "a surface was deleted or stopped implementing ILaunchStageSurface the list is stale; a fifth "
                + "one must be reviewed here rather than escape the audit.");
    }

    [TestMethod]
    public void Audit_PageActivationAuditTests_NewRequestWizardPages_AreAllCoveredByTheActivationAudit()
    {
        var repositoryRoot = RepositoryPatternScan.FindRepositoryRoot();
        var discovered = EnumeratePageClasses(repositoryRoot)
            .Select(page => page.ClassName)
            .ToHashSet(StringComparer.Ordinal);
        var missing = s_newRequestWizardPages.Where(page => !discovered.Contains(page)).ToList();

        Assert.AreEqual(
            0,
            missing.Count,
            "The activation audit no longer sees every step of the New Request wizard, so it can no longer "
                + "prove that the wizard can be driven end to end. Missing: "
                + string.Join(", ", missing)
                + ". A renamed or relocated page must be re-listed here, not dropped.");
    }

    /// <summary>
    /// Finds every <c>Page</c>-derived class declared in a view code-behind file.
    /// </summary>
    /// <param name="repositoryRoot">The repository root.</param>
    /// <returns>Each page's path relative to the root, its class name, and whether it is a launch surface.</returns>
    private static IEnumerable<(string RelativePath, string ClassName, bool IsLaunchStage)> EnumeratePageClasses(string repositoryRoot)
    {
        foreach (var file in Directory.EnumerateFiles(repositoryRoot, "*.xaml.cs", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(repositoryRoot, file);
            if (IsBuildOutput(relativePath) || !relativePath.Contains($"{Path.DirectorySeparatorChar}Views{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var match = s_classDeclaration.Match(File.ReadAllText(file));
            if (!match.Success)
            {
                continue;
            }

            // `Page`, `Page, IServiceSearchHost` and `Microsoft.UI.Xaml.Controls.Page` all count; a
            // Window, ContentDialog or UserControl does not — none of those is a Frame.Navigate target.
            var bases = match.Groups["bases"].Value.Split(',', StringSplitOptions.TrimEntries);
            if (!bases.Any(baseType => string.Equals(
                baseType.Split('.')[^1],
                "Page",
                StringComparison.Ordinal)))
            {
                continue;
            }

            // A launch surface is content of the one window, built by the host rather than activated by a frame.
            var isLaunchStage = bases.Any(baseType => string.Equals(
                baseType.Split('.')[^1],
                "ILaunchStageSurface",
                StringComparison.Ordinal));

            yield return (relativePath, match.Groups["name"].Value, isLaunchStage);
        }
    }

    /// <summary>Reports whether the file declares <c>public &lt;ClassName&gt;()</c>.</summary>
    /// <param name="source">The code-behind source.</param>
    /// <param name="className">The page class name.</param>
    /// <returns>True when a public parameterless constructor is declared.</returns>
    private static bool DeclaresParameterlessConstructor(string source, string className) =>
        Regex.IsMatch(
            source,
            $@"public\s+{Regex.Escape(className)}\s*\(\s*\)",
            RegexOptions.CultureInvariant);

    /// <summary>Reports whether the path is generated build output rather than source.</summary>
    /// <param name="relativePath">The path relative to the repository root.</param>
    /// <returns>True for <c>bin</c>, <c>obj</c> and test-results paths.</returns>
    private static bool IsBuildOutput(string relativePath)
    {
        var segments = relativePath.Split(Path.DirectorySeparatorChar);
        return segments.Any(segment => segment is "bin" or "obj" or "TestResults");
    }
}
