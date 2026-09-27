using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MTM_Waitlist.Tests.Module_Mock;

/// <summary>
/// SC-013's retired-symbol gate: fails if any retired sample/demo type, setting key, or database object is
/// still present or referenced in code or database artifacts.
/// </summary>
/// <remarks>
/// <para>
/// <b>Scope.</b> This audit scans production and test source (`.cs`, `.xaml`, `.resw`, `.csproj`, `.json`)
/// and live database artifacts (`.sql`). Design documents under <c>specs/</c> and the workstream notes under
/// <c>WeekendProject/</c> legitimately record the retirement and are therefore excluded here; the
/// documentation sweep is tracked by its own tasks (T102, T122, T125). The CodeGraphy graph cache under
/// <c>.codegraphy/</c> is generated build output, like <c>bin/</c> and <c>obj/</c>: it holds a snapshot of
/// symbol names taken at index time and is rebuilt from the source, not written by hand.
/// </para>
/// <para>
/// <b>Two deliberate SQL exemptions.</b> A retired object's <c>rollback.sql</c> is the drop artifact the
/// retirement tasks explicitly retained, and <c>Database/Bootstrap/update_table_descriptions.sql</c>
/// carries the recorded "Retired objects" section. Both must name the retired objects, so neither is a
/// stale reference — they are the record <i>of</i> the removal.
/// </para>
/// <para>
/// <b>One generated-output exemption.</b> <c>tools/tooltip-inventory/</c> holds the artifacts
/// <c>tools/Generate-TooltipInventory.ps1</c> writes; it is gitignored, it is rebuilt from the XAML rather
/// than written by hand, and the generator already skips that folder when it scans. A snapshot taken before
/// a retirement therefore names symbols that no longer exist in source — a fact about the snapshot's age,
/// not about the code — so it is excluded here for the same reason <c>.codegraphy/</c> is.
/// </para>
/// </remarks>
[TestClass]
public sealed class RetiredSymbolAuditTests
{
    /// <summary>The retired type, key, and object names, as regular expressions.</summary>
    private static readonly (string Description, Regex Pattern)[] s_retiredSymbols =
    [
        ("sample-data contract", new Regex(@"\bISampleDataService\b", RegexOptions.Compiled)),
        ("sample-data service", new Regex(@"\bSampleDataService\b", RegexOptions.Compiled)),
        ("sample catalog", new Regex(@"\bSample\w*Catalog\b", RegexOptions.Compiled)),
        ("mock-toggle setting key", new Regex(@"Feature\.(InforVisualMockData|RecvMockData)", RegexOptions.Compiled)),
        ("mock-toggle service", new Regex(@"\bI?MockToggleService\b", RegexOptions.Compiled)),
        ("mock routing stack", new Regex(@"\bMockRouting\w*\b", RegexOptions.Compiled)),
        ("mock mode stack", new Regex(@"\bMockMode\w*\b", RegexOptions.Compiled)),
        ("mock configuration service", new Regex(@"\bMockConfigurationService\b", RegexOptions.Compiled)),
        ("mock master-data service", new Regex(@"\bI?MockMasterDataService\b", RegexOptions.Compiled)),
        ("settings mock control", new Regex(@"\b(UseMockData|MockDataExpander)\b", RegexOptions.Compiled)),
        ("retired mock procedure", new Regex(@"\bsp_mock_", RegexOptions.Compiled)),

        // ── feature 004-unified-card-item-picker (FR-023, SC-012): the request type and subtype vocabulary ──
        // Identifier-shaped on purpose. Honest retirement prose — "the type/subtype catalog retired with the
        // vocabulary" — must stay legal; a stray identifier, symbol or scope value must not.
        ("retired catalog table", new Regex(@"\bwaitlist_request_(types|subtypes)\b", RegexOptions.Compiled)),
        ("retired catalog read procedure", new Regex(@"\bsp_waitlist_request_(types|subtypes)_get\b", RegexOptions.Compiled)),
        ("retired catalog seed", new Regex(@"\bseed_waitlist_request_catalog\b", RegexOptions.Compiled)),
        ("retired type inventory", new Regex(@"\bRequest(Type|Subtype)Inventory\b", RegexOptions.Compiled)),
        ("retired display-label service", new Regex(@"\bI?Request(Type|Subtype)DisplayLabelService\b", RegexOptions.Compiled)),
        ("retired subtype name reader", new Regex(@"\bI?RequestSubtypeNameReadService\b", RegexOptions.Compiled)),
        ("retired legacy mapper", new Regex(@"\bRequestItemLegacyMapper\b", RegexOptions.Compiled)),
        ("retired image mapping model", new Regex(@"\bRequest(Type|Subtype)ImageMapping\b", RegexOptions.Compiled)),
        ("retired request-type picture dialog", new Regex(@"\bRequestTypeImagesDialog\w*\b", RegexOptions.Compiled)),
        ("retired subtype picture dialog", new Regex(@"\bRequestSubtypeImagesDialog\w*\b", RegexOptions.Compiled)),
        ("retired image scope value", new Regex(@"\brequest_(type|subtype)\b", RegexOptions.Compiled)),
        ("retired per-subtype local-settings key", new Regex(@"\bMaxAllottedMinutes\b", RegexOptions.Compiled)),
        ("retired user-visible picture wording", new Regex(@"Request Type & Subtype Images|Request Subtype Images|Manage subtypes|subtype images", RegexOptions.Compiled)),

        // ── feature 008-part-pictures (FR-014, FR-015, SC-002): the component card's stand-in claim ──
        // The component card used to say its placeholder stood in "until part pictures exist", which stopped being
        // true the moment every part could carry a picture of its own. A surface that says it has no picture to draw
        // is exactly what FR-014 forbids, so the wording must not come back in code, markup or a resource string.
        ("retired part-picture stand-in wording", new Regex(@"until part pictures exist", RegexOptions.Compiled | RegexOptions.IgnoreCase)),

        // ── feature 010-startup-rebuild (T001): the retired startup surface ──
        // Name-shaped on purpose. The rebuilt startup pipeline lives in MTM_Waitlist.Startup's own Services and
        // ViewModels namespaces, its views live in Module_Startup/Views, and the rebuilt types use new names, so a
        // namespace-shaped or folder-shaped pattern would forbid the replacement along with the removal. Only the
        // removed type and member names are forbidden; a design note, a defect record or a database comment that
        // names the retirement is the record of the removal and stays legal.
        ("retired activation contract", new Regex(@"\bIActivationService\b", RegexOptions.Compiled)),
        ("retired activation-handler contract", new Regex(@"\bIActivationHandler\b", RegexOptions.Compiled)),
        ("retired app-lifecycle contract", new Regex(@"\bIAppLifecycleService\b", RegexOptions.Compiled)),
        ("retired computer-gate contract", new Regex(@"\bIComputerGateService\b", RegexOptions.Compiled)),
        ("retired startup coordinator contract", new Regex(@"\bIStartupCoordinator\b", RegexOptions.Compiled)),
        ("retired startup recovery contract", new Regex(@"\bIStartupRecoveryService\b", RegexOptions.Compiled)),
        ("retired startup registration contract", new Regex(@"\bIStartupRegistrationService\b", RegexOptions.Compiled)),
        ("retired startup session-repository contract", new Regex(@"\bIStartupSessionRepository\b", RegexOptions.Compiled)),
        ("retired startup shell-state contract", new Regex(@"\bIStartupShellStateService\b", RegexOptions.Compiled)),
        ("retired startup window contract", new Regex(@"\bIStartupWindowService\b", RegexOptions.Compiled)),
        ("retired activation service", new Regex(@"\bActivationService\b", RegexOptions.Compiled)),
        ("retired activation-handler base", new Regex(@"\bActivationHandler\b", RegexOptions.Compiled)),
        ("retired notification activation handler", new Regex(@"\bAppNotificationActivationHandler\b", RegexOptions.Compiled)),
        ("retired default activation handler", new Regex(@"\bDefaultActivationHandler\b", RegexOptions.Compiled)),
        ("retired app-lifecycle service", new Regex(@"\bAppLifecycleService\b", RegexOptions.Compiled)),
        ("retired computer-gate service", new Regex(@"\bComputerGateService\b", RegexOptions.Compiled)),
        ("retired sign-in session keys", new Regex(@"\bSignInSessionKeys\b", RegexOptions.Compiled)),
        ("retired startup coordinator", new Regex(@"\bStartupCoordinator\b", RegexOptions.Compiled)),
        ("retired startup module service", new Regex(@"\bStartupModuleService\b", RegexOptions.Compiled)),
        ("retired startup recovery service", new Regex(@"\bStartupRecoveryService\b", RegexOptions.Compiled)),
        ("retired startup registration service", new Regex(@"\bStartupRegistrationService\b", RegexOptions.Compiled)),
        // Backtick-guarded for the same reason and just as narrowly: the nine procedure artifacts T020 deletes
        // name this class inside backtick-quoted provenance comments, and those files are still in the tree until
        // that task runs. A live reintroduction is not backtick-quoted.
        ("retired startup session repository", new Regex(@"(?<!`)\bStartupSessionRepository\b", RegexOptions.Compiled)),
        ("retired startup shell-state service", new Regex(@"\bStartupShellStateService\b", RegexOptions.Compiled)),
        ("retired startup window service", new Regex(@"\bStartupWindowService\b", RegexOptions.Compiled)),
        ("retired sign-in view model", new Regex(@"\bLoginViewModel\b", RegexOptions.Compiled)),
        ("retired splash view model", new Regex(@"\bSplashViewModel\b", RegexOptions.Compiled)),
        ("retired credential-check result model", new Regex(@"\bStartupCredentialCheckResult\b", RegexOptions.Compiled)),
        ("retired startup database options", new Regex(@"\bStartupDatabaseOptions\b", RegexOptions.Compiled)),
        ("retired startup development options", new Regex(@"\bStartupDevelopmentOptions\b", RegexOptions.Compiled)),
        ("retired password-reset requirement model", new Regex(@"\bStartupPasswordResetRequirement\b", RegexOptions.Compiled)),
        ("retired startup registration request model", new Regex(@"\bStartupRegistrationRequest\b", RegexOptions.Compiled)),
        ("retired startup result model", new Regex(@"\bStartupResult\b", RegexOptions.Compiled)),
        ("retired startup session snapshot model", new Regex(@"\bStartupSessionSnapshot\b", RegexOptions.Compiled)),
        ("retired launch-state object", new Regex(@"\bStartupState\b", RegexOptions.Compiled)),
        ("retired startup window options", new Regex(@"\bStartupWindowOptions\b", RegexOptions.Compiled)),
        ("retired sign-in window", new Regex(@"\bLoginWindow\b", RegexOptions.Compiled)),
        ("retired sign-in page", new Regex(@"\bLoginPage\b", RegexOptions.Compiled)),
        ("retired splash window", new Regex(@"\bSplashWindow\b", RegexOptions.Compiled)),
        ("retired splash page", new Regex(@"\bSplashPage\b", RegexOptions.Compiled)),
        ("retired splash view", new Regex(@"\bSplashView\b", RegexOptions.Compiled)),
        ("retired splash handoff member", new Regex(@"\bShowSplashWindow\b", RegexOptions.Compiled)),
        ("retired sign-in handoff member", new Regex(@"\bShowLoginWindowAndCloseSplash\b", RegexOptions.Compiled)),
        ("retired main-window handoff member", new Regex(@"\bShowMainWindowAndClose(LoginWindow|Splash)\b", RegexOptions.Compiled)),
        ("retired splash-close member", new Regex(@"\bCloseSplashWindow\b", RegexOptions.Compiled)),
        ("retired window-closed handler", new Regex(@"\b(SplashWindow|LoginWindow)_Closed\b", RegexOptions.Compiled)),
        // MainWindow.xaml.cs legitimately keeps an instance MainWindow_Closed of its own, so only the removed App
        // static handler is forbidden, and it is forbidden by shape rather than by name alone.
        ("retired static main-window closed handler", new Regex(@"\bstatic\s+void\s+MainWindow_Closed\b", RegexOptions.Compiled)),

        // The file-based logging seam the rebuilt store-backed seam replaced. Both names are still forbidden
        // even though T133 deletes the forwarder, its contract and the options model separately: a log service
        // that quietly came back would satisfy neither the store-backed contract nor the no-local-log-file rule.
        ("retired startup log-service contract", new Regex(@"\bIStartupLogService\b", RegexOptions.Compiled)),
        ("retired startup log service", new Regex(@"\bStartupLogService\b", RegexOptions.Compiled)),

        // The debug-only static logger T069 deleted in the same change as those two, and the omission this
        // pattern closes: a guard that names two of three removed logging types is inconsistent with its own
        // purpose, not deliberately narrower. The bare name is forbidden because the type was static, so every
        // reappearance is a call site (`StartupDebugLog.Info(...)`) rather than a declaration, and because its
        // methods are `[Conditional("DEBUG")]` a reintroduction would silently record nothing in a Release
        // build — the exact defect SC-003 exists to stop. T069 also rewrote the two raw-text doc-comment
        // mentions this whole-file scan would otherwise have flagged (`MTM_Waitlist.Mock.Service/Services/
        // ServiceLog.cs` and `MTM_Waitlist.Tests/Module_Mock_Service/ServiceLogTests.cs`); a reappearance of
        // either fails here too, since the scan matches raw text rather than resolved symbols.
        ("retired debug-only static logger", new Regex(@"\bStartupDebugLog\b", RegexOptions.Compiled)),
    ];

    /// <summary>File extensions this audit treats as code or database artifacts.</summary>
    private static readonly string[] s_scannedExtensions = [".cs", ".xaml", ".resw", ".sql", ".csproj", ".json"];

    /// <summary>Directories whose files are documentation or build output, not live code.</summary>
    private static readonly string[] s_excludedDirectories =
    [
        $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}.codegraphy{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}specs{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}WeekendProject{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}.github{Path.DirectorySeparatorChar}",

        // Generated tooltip-inventory output: gitignored, rebuilt from the XAML, and already skipped by its
        // own generator. See the remarks above.
        $"{Path.DirectorySeparatorChar}tools{Path.DirectorySeparatorChar}tooltip-inventory{Path.DirectorySeparatorChar}",
    ];

    /// <summary>The startup view files the feature 010 removal deleted, as repository-relative paths.</summary>
    private static readonly string[] s_removedStartupViewFiles =
    [
        "Module_Startup/Views/LoginPage.xaml",
        "Module_Startup/Views/LoginPage.xaml.cs",
        "Module_Startup/Views/LoginWindow.xaml",
        "Module_Startup/Views/LoginWindow.xaml.cs",
        "Module_Startup/Views/SplashPage.xaml",
        "Module_Startup/Views/SplashPage.xaml.cs",
        "Module_Startup/Views/SplashView.xaml",
        "Module_Startup/Views/SplashView.xaml.cs",
        "Module_Startup/Views/SplashWindow.xaml",
        "Module_Startup/Views/SplashWindow.xaml.cs",
    ];

    /// <summary>
    /// Every type the feature 010 removal deleted. The removal checks are keyed on this inventory of removed
    /// names rather than on a namespace or a folder: the rebuilt services and view models occupy
    /// <c>MTM_Waitlist.Startup</c>'s own <c>Services</c> and <c>ViewModels</c> namespaces, and the rebuilt views
    /// occupy <c>Module_Startup/Views</c>, so a namespace-shaped or folder-shaped pattern would forbid the
    /// replacement along with the removal.
    /// </summary>
    private static readonly string[] s_removedStartupTypeNames =
    [
        "IActivationService", "IActivationHandler", "IAppLifecycleService", "IComputerGateService",
        "IStartupCoordinator", "IStartupRecoveryService", "IStartupRegistrationService",
        "IStartupSessionRepository", "IStartupShellStateService", "IStartupWindowService",
        "ActivationService", "ActivationHandler", "AppNotificationActivationHandler",
        "DefaultActivationHandler", "AppLifecycleService", "ComputerGateService", "SignInSessionKeys",
        "StartupCoordinator", "StartupModuleService", "StartupRecoveryService", "StartupRegistrationService",
        "StartupSessionRepository", "StartupShellStateService", "StartupWindowService",
        "LoginViewModel", "SplashViewModel",
        "StartupCredentialCheckResult", "StartupDatabaseOptions", "StartupDevelopmentOptions",
        "StartupPasswordResetRequirement", "StartupRegistrationRequest", "StartupResult",
        "StartupSessionSnapshot", "StartupWindowOptions", "StartupLogService",
        "LoginWindow", "LoginPage", "SplashWindow", "SplashPage", "SplashView",
    ];

    /// <summary>
    /// The removed contract, model and option types that lived in <c>MTM_Waitlist.Core</c>, checked separately
    /// because a rebuilt service that quietly kept one of them would still compile.
    /// </summary>
    private static readonly string[] s_removedCoreStartupTypeNames =
    [
        "IActivationService", "IActivationHandler", "IAppLifecycleService", "IComputerGateService",
        "IStartupCoordinator", "IStartupRecoveryService", "IStartupRegistrationService",
        "IStartupSessionRepository", "IStartupShellStateService", "IStartupWindowService",
        "StartupResult", "StartupSessionSnapshot", "StartupCredentialCheckResult",
        "StartupPasswordResetRequirement", "StartupRegistrationRequest",
        "StartupDevelopmentOptions", "StartupWindowOptions", "StartupDatabaseOptions",
        "StartupState", "IStartupLogService",
    ];

    /// <summary>
    /// The scope the removal checks share: the production scope plus the design, tooling and generated
    /// directories the retired-symbol gate also skips.
    /// </summary>
    private static readonly RepositoryScanScope s_startupSurfaceScope = new()
    {
        ExcludedDirectories =
        [
            .. RepositoryScanScope.Default.ExcludedDirectories,
            ".codegraphy",
            "WeekendProject",
            ".github",
            "tooltip-inventory",
        ],
    };

    /// <summary>
    /// Fails when any retired symbol survives in code or in a live database artifact.
    /// </summary>
    [TestMethod]
    public void NoRetiredSymbolRemainsInCodeOrDatabaseArtifacts()
    {
        var repositoryRoot = FindRepositoryRoot();
        var violations = new List<string>();

        foreach (var file in EnumerateScannedFiles(repositoryRoot))
        {
            if (IsExemptFromSqlAudit(file))
            {
                continue;
            }

            var content = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(repositoryRoot, file);

            foreach (var (description, pattern) in s_retiredSymbols)
            {
                if (pattern.IsMatch(content))
                {
                    violations.Add($"{relativePath}: {description} ({pattern})");
                }
            }
        }

        Assert.AreEqual(
            0,
            violations.Count,
            "SC-013 requires no retired sample/demo symbol to survive in code or database artifacts:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
    }

    /// <summary>
    /// Excludes the drop artifacts and the maintenance file that record the retirement itself.
    /// </summary>
    /// <param name="file">The candidate file.</param>
    private static bool IsExemptFromSqlAudit(string file)
    {
        if (!file.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var fileName = Path.GetFileName(file);

        // The retained rollback artifact IS the drop statement for a retired object.
        if (string.Equals(fileName, "rollback.sql", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // The mandatory maintenance file records what was retired and when.
        return string.Equals(fileName, "update_table_descriptions.sql", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> EnumerateScannedFiles(string repositoryRoot)
    {
        foreach (var file in Directory.EnumerateFiles(repositoryRoot, "*", SearchOption.AllDirectories))
        {
            if (!s_scannedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            if (s_excludedDirectories.Any(excluded => file.Contains(excluded, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            // This audit necessarily names every retired symbol it forbids.
            if (string.Equals(Path.GetFileName(file), "RetiredSymbolAuditTests.cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return file;
        }
    }

    /// <summary>
    /// Walks up from the test binaries until the solution file is found.
    /// </summary>
    private static string FindRepositoryRoot() => RepositoryPatternScan.FindRepositoryRoot();

    /// <summary>
    /// T003 self-test. An empty pattern set must find nothing: every later use of the helper then proves
    /// something through its pattern set rather than accidentally through the scanner.
    /// </summary>
    [TestMethod]
    public void PatternScan_WithAnEmptyPatternSet_ReturnsNoHits()
    {
        var hits = RepositoryPatternScan.Scan(Array.Empty<(string Description, string Pattern)>());

        Assert.AreEqual(0, hits.Count, RepositoryPatternScan.Describe(hits));
    }

    /// <summary>
    /// The mirror of the scan, and the reason each retirement guard can be trusted: every pattern must bite on a
    /// representative re-introduction of the symbol it forbids.
    /// </summary>
    /// <remarks>
    /// A pattern that has quietly stopped matching — a widened escape, a renamed symbol, a mistyped group — is a
    /// guard that reports clean forever, which is worse than no guard at all. The sample set is required to cover
    /// every declared pattern, so adding a pattern without proving it bites fails the build.
    /// </remarks>
    [TestMethod]
    public void EveryRetiredPattern_BitesOnAReintroductionOfItsSymbol()
    {
        var samples = ReintroductionSamples();

        var unsampled = s_retiredSymbols
            .Select(entry => entry.Description)
            .Where(description => !samples.ContainsKey(description))
            .ToList();

        Assert.AreEqual(
            0,
            unsampled.Count,
            "These retired symbols have no re-introduction sample, so nothing proves their pattern still bites: "
                + string.Join(", ", unsampled));

        var blind = new List<string>();
        foreach (var (description, pattern) in s_retiredSymbols)
        {
            if (!pattern.IsMatch(samples[description]))
            {
                blind.Add($"{description} ({pattern}) does not match its own re-introduction: {samples[description]}");
            }
        }

        Assert.AreEqual(
            0,
            blind.Count,
            "A retired-symbol pattern that no longer matches its own symbol is a guard that can never fail:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, blind));
    }

    /// <summary>
    /// One representative re-introduction per retired symbol: the smallest piece of text that should make the
    /// scan fail if it were ever put back into the repository.
    /// </summary>
    private static Dictionary<string, string> ReintroductionSamples() => new(StringComparer.Ordinal)
    {
        ["sample-data contract"] = "public interface ISampleDataService { }",
        ["sample-data service"] = "public sealed class SampleDataService { }",
        ["sample catalog"] = "public static class SampleOrderCatalog { }",
        ["mock-toggle setting key"] = "\"Feature.InforVisualMockData\": true",
        ["mock-toggle service"] = "public interface IMockToggleService { }",
        ["mock routing stack"] = "public sealed class MockRoutingResolver { }",
        ["mock mode stack"] = "public enum MockModeState { }",
        ["mock configuration service"] = "public sealed class MockConfigurationService { }",
        ["mock master-data service"] = "public interface IMockMasterDataService { }",
        ["settings mock control"] = "<ToggleSwitch x:Name=\"UseMockData\" />",
        ["retired mock procedure"] = "\"sp_mock_parts_get\"",
        ["retired catalog table"] = "CREATE TABLE waitlist_request_types (",
        ["retired catalog read procedure"] = "CREATE PROCEDURE sp_waitlist_request_subtypes_get()",
        ["retired catalog seed"] = "-- Seed: seed_waitlist_request_catalog",
        ["retired type inventory"] = "RequestSubtypeInventory.Groups",
        ["retired display-label service"] = "public interface IRequestTypeDisplayLabelService { }",
        ["retired subtype name reader"] = "public interface IRequestSubtypeNameReadService { }",
        ["retired legacy mapper"] = "RequestItemLegacyMapper.Map(\"Forklift Assist\", null)",
        ["retired image mapping model"] = "public sealed class RequestSubtypeImageMapping { }",
        ["retired request-type picture dialog"] = "new RequestTypeImagesDialogViewModel(",
        ["retired subtype picture dialog"] = "new RequestSubtypeImagesDialogViewModel(",
        ["retired image scope value"] = "<value>request_subtype</value>",
        ["retired per-subtype local-settings key"] = "SaveSettingAsync(\"Urgency.MaxAllottedMinutes.Pickup Coil\", 45)",
        ["retired user-visible picture wording"] = "<value>Manage subtypes</value>",
        ["retired part-picture stand-in wording"] = "The no-image picture stands in until part pictures exist.",
        ["retired activation contract"] = "public interface IActivationService { }",
        ["retired activation-handler contract"] = "public interface IActivationHandler { }",
        ["retired app-lifecycle contract"] = "public interface IAppLifecycleService { }",
        ["retired computer-gate contract"] = "public interface IComputerGateService { }",
        ["retired startup coordinator contract"] = "public interface IStartupCoordinator { }",
        ["retired startup recovery contract"] = "public interface IStartupRecoveryService { }",
        ["retired startup registration contract"] = "public interface IStartupRegistrationService { }",
        ["retired startup session-repository contract"] = "public interface IStartupSessionRepository { }",
        ["retired startup shell-state contract"] = "public interface IStartupShellStateService { }",
        ["retired startup window contract"] = "public interface IStartupWindowService { }",
        ["retired activation service"] = "public class ActivationService : IActivationService",
        ["retired activation-handler base"] = "public abstract class ActivationHandler<T>",
        ["retired notification activation handler"] = "public class AppNotificationActivationHandler",
        ["retired default activation handler"] = "public class DefaultActivationHandler",
        ["retired app-lifecycle service"] = "public sealed class AppLifecycleService : IAppLifecycleService",
        ["retired computer-gate service"] = "public sealed class ComputerGateService : IComputerGateService",
        ["retired sign-in session keys"] = "SessionToken = SignInSessionKeys.SessionToken;",
        ["retired startup coordinator"] = "public sealed class StartupCoordinator : IStartupCoordinator",
        ["retired startup module service"] = "public sealed class StartupModuleService",
        ["retired startup recovery service"] = "public sealed class StartupRecoveryService : IStartupRecoveryService",
        ["retired startup registration service"] = "public sealed class StartupRegistrationService",
        ["retired startup session repository"] = "public sealed class StartupSessionRepository : IStartupSessionRepository",
        ["retired startup shell-state service"] = "public sealed class StartupShellStateService",
        ["retired startup window service"] = "public sealed class StartupWindowService : IStartupWindowService",
        ["retired sign-in view model"] = "public partial class LoginViewModel : ObservableRecipient",
        ["retired splash view model"] = "public partial class SplashViewModel : ObservableRecipient",
        ["retired credential-check result model"] = "public sealed record StartupCredentialCheckResult",
        ["retired startup database options"] = "public sealed class StartupDatabaseOptions",
        ["retired startup development options"] = "public sealed class StartupDevelopmentOptions",
        ["retired password-reset requirement model"] = "public sealed class StartupPasswordResetRequirement",
        ["retired startup registration request model"] = "public sealed class StartupRegistrationRequest",
        ["retired startup result model"] = "public sealed class StartupResult",
        ["retired startup session snapshot model"] = "public sealed class StartupSessionSnapshot",
        ["retired launch-state object"] = "public sealed class StartupState",
        ["retired startup window options"] = "public sealed class StartupWindowOptions",
        ["retired sign-in window"] = "public sealed partial class LoginWindow : WindowEx",
        ["retired sign-in page"] = "public sealed partial class LoginPage : Page",
        ["retired splash window"] = "public sealed partial class SplashWindow : WindowEx",
        ["retired splash page"] = "public sealed partial class SplashPage : Page",
        ["retired splash view"] = "public sealed partial class SplashView : UserControl",
        ["retired splash handoff member"] = "App.ShowSplashWindow();",
        ["retired sign-in handoff member"] = "App.ShowLoginWindowAndCloseSplash();",
        ["retired main-window handoff member"] = "App.ShowMainWindowAndCloseLoginWindow();",
        ["retired splash-close member"] = "CloseSplashWindow();",
        ["retired window-closed handler"] = "SplashWindow_Closed(this, args);",
        ["retired static main-window closed handler"] = "private static void MainWindow_Closed(object sender, WindowEventArgs args)",
        ["retired startup log-service contract"] = "public interface IStartupLogService { };",
        ["retired startup log service"] = "public sealed class StartupLogService : BackgroundService, IStartupLogService",
        ["retired debug-only static logger"] = "StartupDebugLog.Info(\"Startup\", \"Launching MTM Waitlist\");",
    };

    /// <summary>
    /// T026's placeholder-resource gate: no shipped resource <i>value</i> is a placeholder address, a sample
    /// organisation, filler text, or a developer marker.
    /// </summary>
    /// <remarks>
    /// Only <c>&lt;value&gt;</c> contents are scanned. Key names are deliberately not: the WinUI convention
    /// names many legitimate keys with the word "Placeholder" (a TextBox's watermark, for instance), so
    /// scanning names would fail on correct markup.
    /// </remarks>
    [TestMethod]
    public void NoPlaceholderResourceValueShips()
    {
        (string Description, Regex Pattern)[] patterns =
        [
            ("privacy placeholder address", new Regex(@"YourPrivacyUrlGoesHere", RegexOptions.Compiled | RegexOptions.CultureInvariant)),
            ("example domain", new Regex(@"\bexample\.(com|org|net)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
            ("sample organisation", new Regex(@"\bContoso\b", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
            ("filler text", new Regex(@"\blorem ipsum\b", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
            ("developer marker", new Regex(@"\bTODO\b", RegexOptions.Compiled | RegexOptions.CultureInvariant)),
        ];

        var violations = new List<string>();
        var valueCount = 0;

        foreach (var (fileName, value) in EnumerateResourceValues())
        {
            valueCount++;

            foreach (var (description, pattern) in patterns)
            {
                if (pattern.IsMatch(value))
                {
                    violations.Add($"{fileName}: {description} [{pattern}] -> {value.Trim()}");
                }
            }
        }

        Assert.IsTrue(
            valueCount > 100,
            $"The scan reached only {valueCount} resource values, so a clean result would prove nothing.");

        Assert.AreEqual(
            0,
            violations.Count,
            "No shipped resource value may be a placeholder:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
    }

    /// <summary>
    /// T036's retired-wording gate, and the standing check for FR-030: no demo, sample or mock mode may
    /// return, and no shipped text may present one as current.
    /// </summary>
    [TestMethod]
    public void NoRetiredWordingSurvives()
    {
        (string Description, Regex Pattern)[] retiredWording =
        [
            ("retired mock status key", new Regex(@"\bMockSaved\b", RegexOptions.Compiled | RegexOptions.CultureInvariant)),
            ("retired receiving mock toggle key", new Regex(@"\bRecvMockData\b", RegexOptions.Compiled | RegexOptions.CultureInvariant)),
            ("retired Infor Visual mock toggle key", new Regex(@"\bInforVisualMockData\b", RegexOptions.Compiled | RegexOptions.CultureInvariant)),
        ];

        var hits = RepositoryPatternScan.Scan(retiredWording);

        Assert.AreEqual(
            0,
            hits.Count,
            "No retired mock/sample wording may survive (FR-025/FR-029/FR-030):"
                + Environment.NewLine
                + RepositoryPatternScan.Describe(hits));

        // In a shipped resource value these words read as a product statement rather than as a comment saying
        // the mechanism is gone, so resource values are checked explicitly. Comments that state the absence
        // ("never replaced by sample rows") are the record of the removal, not a stale claim, which is why the
        // narrower scan is applied to values only.
        var sampleWording = new Regex(@"\bsample (data|rows)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var offenders = new List<string>();
        var valueCount = 0;

        foreach (var (fileName, value) in EnumerateResourceValues())
        {
            valueCount++;

            if (sampleWording.IsMatch(value))
            {
                offenders.Add($"{fileName} -> {value.Trim()}");
            }
        }

        Assert.IsTrue(
            valueCount > 100,
            $"The scan reached only {valueCount} resource values, so a clean result would prove nothing.");

        Assert.AreEqual(
            0,
            offenders.Count,
            "No shipped resource value may describe sample data or sample rows:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, offenders));
    }

    /// <summary>Every <c>&lt;value&gt;</c> shipped in a resource file, with the file it came from.</summary>
    private static IEnumerable<(string FileName, string Value)> EnumerateResourceValues()
    {
        var scope = new RepositoryScanScope { Extensions = [".resw"] };

        foreach (var file in RepositoryPatternScan.EnumerateScannedFiles(RepositoryPatternScan.FindRepositoryRoot(), scope))
        {
            foreach (var value in System.Xml.Linq.XDocument.Load(file).Descendants("value"))
            {
                yield return (Path.GetFileName(file), value.Value);
            }
        }
    }

    /// <summary>
    /// T003 self-test. The default scope must actually reach production source, so that a clean scan is
    /// evidence of compliance rather than of a silently empty enumeration — the failure mode that makes a
    /// scan check vacuous.
    /// </summary>
    [TestMethod]
    public void PatternScan_WithTheDefaultScope_EnumeratesProductionSource()
    {
        var files = RepositoryPatternScan
            .EnumerateScannedFiles(RepositoryPatternScan.FindRepositoryRoot(), RepositoryScanScope.Default)
            .ToList();

        Assert.IsTrue(files.Count > 100, $"The default scan scope reached only {files.Count} files.");

        Assert.IsTrue(
            files.Any(file => file.EndsWith("WaitlistViewViewModel.cs", StringComparison.OrdinalIgnoreCase)),
            "The default scan scope did not reach MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs.");

        Assert.IsTrue(
            files.Any(file => file.EndsWith("SettingsPage.xaml", StringComparison.OrdinalIgnoreCase)),
            "The default scan scope did not reach Module_Settings/Views/SettingsPage.xaml.");

        Assert.IsFalse(
            files.Any(file => file.Contains($"{Path.DirectorySeparatorChar}specs{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                || file.Contains($"{Path.DirectorySeparatorChar}defects{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)),
            "The default scan scope must exclude specs/ and defects/, which quote the removed values as evidence.");
    }

    /// <summary>
    /// T003 self-test. A literal that genuinely exists must be found, so a clean result from a real pattern
    /// set cannot come from a matcher that never matches.
    /// </summary>
    [TestMethod]
    public void PatternScan_WithAKnownPresentLiteral_FindsIt()
    {
        var hits = RepositoryPatternScan.Scan([("waitlist field population", new Regex(@"AddRequestFields", RegexOptions.Compiled))]);

        Assert.IsTrue(hits.Count > 0, "The scan helper failed to find a literal that is present in the repository.");
        Assert.IsTrue(hits.All(hit => hit.LineNumber > 0), "Every hit must report the line it was found on.");
    }

    /// <summary>
    /// T005 (checklist 1.1c). The removal is only real if the files it deleted are gone: a view file that
    /// survived would still compile into the app and a launch path could still show it.
    /// </summary>
    [TestMethod]
    public void RemovedStartupViewFiles_DoNotExist()
    {
        var repositoryRoot = RepositoryPatternScan.FindRepositoryRoot();
        var survivors = s_removedStartupViewFiles
            .Where(relative => File.Exists(Path.Combine(repositoryRoot, relative.Replace('/', Path.DirectorySeparatorChar))))
            .ToList();

        Assert.AreEqual(
            0,
            survivors.Count,
            "These removed startup view files are still present: " + string.Join(", ", survivors));
    }

    /// <summary>
    /// T005 (checklist 1.1c). No removed startup type, and none of the removed window-handoff members, may be
    /// declared again anywhere in the tree. The check is declaration-shaped so a live reintroduction fails while
    /// a design note or a database comment that names the retirement does not.
    /// </summary>
    [TestMethod]
    public void NoRemovedStartupTypeIsDeclaredAnywhere()
    {
        Assert.IsTrue(
            s_removedStartupTypeNames.Length >= 35,
            $"Only {s_removedStartupTypeNames.Length} removed startup types are listed, so a clean scan would prove little.");

        var hits = RepositoryPatternScan.Scan(DeclarationPatterns(s_removedStartupTypeNames), s_startupSurfaceScope);

        Assert.AreEqual(
            0,
            hits.Count,
            "No removed startup type or window-handoff member may be declared again:"
                + Environment.NewLine
                + RepositoryPatternScan.Describe(hits));
    }

    /// <summary>
    /// T005 (checklist 1.1d). No contract, model or option the removal deleted from
    /// <c>MTM_Waitlist.Core</c> may be declared again.
    /// </summary>
    [TestMethod]
    public void NoRemovedCoreStartupContractModelOrOptionIsDeclared()
    {
        var hits = RepositoryPatternScan.Scan(DeclarationPatterns(s_removedCoreStartupTypeNames), s_startupSurfaceScope);

        Assert.AreEqual(
            0,
            hits.Count,
            "No removed startup contract, model or option may be declared again:"
                + Environment.NewLine
                + RepositoryPatternScan.Describe(hits));
    }

    /// <summary>
    /// T005. The rebuilt view set is asserted positively rather than by absence: the module's view folder holds
    /// the placeholder window pair, and the placeholder is the one window a launch shows until the rebuilt
    /// pipeline lands.
    /// </summary>
    [TestMethod]
    public void RebuiltStartupViewSet_IsThePlaceholderPair()
    {
        var repositoryRoot = RepositoryPatternScan.FindRepositoryRoot();
        var viewFolder = Path.Combine(repositoryRoot, "Module_Startup", "Views");

        Assert.IsTrue(Directory.Exists(viewFolder), $"The rebuilt view folder '{viewFolder}' does not exist.");

        var markupFiles = Directory
            .EnumerateFiles(viewFolder)
            .Where(file => string.Equals(Path.GetExtension(file), ".xaml", StringComparison.OrdinalIgnoreCase))
            .Select(file => Path.GetFileName(file) ?? string.Empty)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEqual(
            new[] { "StartupPlaceholderWindow.xaml" },
            markupFiles,
            "The rebuilt startup view set is the placeholder window pair.");

        var markupPath = Path.Combine(viewFolder, "StartupPlaceholderWindow.xaml");
        Assert.IsTrue(File.Exists(markupPath), "The rebuilt placeholder markup is missing.");

        var codeBehindPath = Path.Combine(viewFolder, "StartupPlaceholderWindow.xaml.cs");
        Assert.IsTrue(File.Exists(codeBehindPath), "The rebuilt placeholder code-behind is missing.");

        StringAssert.Contains(
            File.ReadAllText(markupPath),
            "x:Class=\"MTM_Waitlist.Module_Startup.Views.StartupPlaceholderWindow\"",
            "The rebuilt placeholder markup must declare the placeholder window type.");

        StringAssert.Contains(
            File.ReadAllText(codeBehindPath),
            "StartupPlaceholder_CloseButton",
            "The rebuilt placeholder must keep the stable automation id the launch checks address the close button by.");
    }

    /// <summary>Builds the declaration-shaped pattern set for an inventory of removed type names.</summary>
    /// <param name="typeNames">The removed type names.</param>
    /// <returns>The pattern set, as description plus regular-expression source.</returns>
    private static IReadOnlyList<(string Description, string Pattern)> DeclarationPatterns(string[] typeNames)
    {
        var alternation = string.Join("|", typeNames.Select(Regex.Escape));

        return
        [
            ("removed startup type declaration", $@"\b(class|interface|record|enum|struct)\s+({alternation})\b"),
            (
                "removed window-handoff member declaration",
                @"\b(ShowSplashWindow|ShowLoginWindowAndCloseSplash|ShowMainWindowAndCloseSplash|ShowMainWindowAndCloseLoginWindow|CloseSplashWindow)\s*\("),
            (
                "removed window-closed handler declaration",
                @"\b(SplashWindow|LoginWindow)_Closed\b|\bstatic\s+void\s+MainWindow_Closed\b"),
        ];
    }
}

/// <summary>One pattern match found by <see cref="RepositoryPatternScan"/>.</summary>
/// <param name="RelativePath">The file, relative to the repository root.</param>
/// <param name="LineNumber">The 1-based line the match was found on.</param>
/// <param name="Description">What the pattern is looking for.</param>
/// <param name="Pattern">The pattern itself, so the failure message is self-explanatory.</param>
/// <param name="Text">The matching line, trimmed.</param>
internal sealed record RepositoryScanHit(string RelativePath, int LineNumber, string Description, string Pattern, string Text);

/// <summary>
/// Scoping rules for <see cref="RepositoryPatternScan"/>. Every caller states the scope it means rather
/// than inheriting another caller's, and <see cref="Default"/> is the repository-wide production scope.
/// </summary>
internal sealed class RepositoryScanScope
{
    /// <summary>
    /// Repository-wide over production source: the build-output, VCS and design-document directories are
    /// skipped, and <c>specs/</c> and <c>defects/</c> are excluded deliberately because those files quote
    /// the removed values as evidence of the removal.
    /// </summary>
    internal static RepositoryScanScope Default { get; } = new();

    /// <summary>Directory names that are never scanned, at any depth.</summary>
    internal string[] ExcludedDirectories { get; init; } = RepositoryPatternScan.DefaultExcludedDirectories;

    /// <summary>File extensions that are scanned.</summary>
    internal string[] Extensions { get; init; } = RepositoryPatternScan.DefaultScannedExtensions;

    /// <summary>When non-empty, only files under one of these repository-relative paths are scanned.</summary>
    internal string[] IncludeUnder { get; init; } = [];

    /// <summary>
    /// File names skipped even when in scope. The defaults are the two files that must name every pattern
    /// they forbid: this audit, and the fabricated-value guard.
    /// </summary>
    internal string[] ExcludedFileNames { get; init; } = RepositoryPatternScan.DefaultExcludedFileNames;

    /// <summary>Returns a copy of this scope with additional file names excluded.</summary>
    /// <param name="fileNames">The additional file names to skip.</param>
    internal RepositoryScanScope ExcludingFiles(params string[] fileNames)
        => new()
        {
            ExcludedDirectories = ExcludedDirectories,
            Extensions = Extensions,
            IncludeUnder = IncludeUnder,
            ExcludedFileNames = [.. ExcludedFileNames, .. fileNames],
        };
}

/// <summary>
/// T003's pattern-set scan: one helper that takes a set of regular expressions and reports every matching
/// line in the repository, so each later check declares a pattern set instead of writing its own traversal.
/// Used by the fabricated-literal, placeholder-resource and retired-wording gates.
/// </summary>
/// <remarks>
/// Matching is performed line by line so a hit can name its file and line. Patterns are therefore expected
/// to describe a single-line shape, which every gate in this feature does.
/// </remarks>
internal static class RepositoryPatternScan
{
    /// <summary>The file types this scan treats as code, resource or database artifacts.</summary>
    internal static readonly string[] DefaultScannedExtensions = [".cs", ".xaml", ".resw", ".sql", ".csproj", ".json"];

    /// <summary>Directories that are build output, VCS metadata, test output, or design record.</summary>
    internal static readonly string[] DefaultExcludedDirectories = ["bin", "obj", ".git", "TestResults", "specs", "defects"];

    /// <summary>The files that declare the patterns and so necessarily contain them.</summary>
    internal static readonly string[] DefaultExcludedFileNames = ["RetiredSymbolAuditTests.cs", "FabricatedValueGuard.cs"];

    /// <summary>Walks up from the test binaries until the solution file is found.</summary>
    /// <returns>The repository root.</returns>
    internal static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MTM_Waitlist.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail($"The repository root could not be located above '{AppContext.BaseDirectory}'.");
        return AppContext.BaseDirectory;
    }

    /// <summary>Scans the scoped repository for a string-pattern set.</summary>
    /// <param name="patterns">The pattern set, as description plus regular-expression source.</param>
    /// <param name="scope">The scope to scan; the production default when omitted.</param>
    /// <returns>One hit per matching line per pattern.</returns>
    internal static IReadOnlyList<RepositoryScanHit> Scan(
        IReadOnlyList<(string Description, string Pattern)> patterns,
        RepositoryScanScope? scope = null)
    {
        ArgumentNullException.ThrowIfNull(patterns);

        return Scan(
            [.. patterns.Select(entry => (entry.Description, new Regex(entry.Pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant)))],
            scope);
    }

    /// <summary>Scans the scoped repository for a regular-expression pattern set.</summary>
    /// <param name="patterns">The pattern set, as description plus compiled expression.</param>
    /// <param name="scope">The scope to scan; the production default when omitted.</param>
    /// <returns>One hit per matching line per pattern.</returns>
    internal static IReadOnlyList<RepositoryScanHit> Scan(
        IReadOnlyList<(string Description, Regex Pattern)> patterns,
        RepositoryScanScope? scope = null)
    {
        ArgumentNullException.ThrowIfNull(patterns);

        var hits = new List<RepositoryScanHit>();
        if (patterns.Count == 0)
        {
            return hits;
        }

        scope ??= RepositoryScanScope.Default;
        var repositoryRoot = FindRepositoryRoot();

        foreach (var file in EnumerateScannedFiles(repositoryRoot, scope))
        {
            var relativePath = Path.GetRelativePath(repositoryRoot, file);
            var lines = File.ReadAllLines(file);

            for (var index = 0; index < lines.Length; index++)
            {
                foreach (var (description, pattern) in patterns)
                {
                    if (pattern.IsMatch(lines[index]))
                    {
                        hits.Add(new RepositoryScanHit(relativePath, index + 1, description, pattern.ToString(), lines[index].Trim()));
                    }
                }
            }
        }

        return hits;
    }

    /// <summary>Enumerates the files a scope covers.</summary>
    /// <param name="repositoryRoot">The repository root.</param>
    /// <param name="scope">The scope to apply.</param>
    /// <returns>The files to scan.</returns>
    internal static IEnumerable<string> EnumerateScannedFiles(string repositoryRoot, RepositoryScanScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        foreach (var file in Directory.EnumerateFiles(repositoryRoot, "*", SearchOption.AllDirectories))
        {
            if (!scope.Extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(repositoryRoot, file);

            if (scope.IncludeUnder.Length > 0
                && !scope.IncludeUnder.Any(prefix => relativePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (scope.ExcludedFileNames.Contains(Path.GetFileName(file), StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            if (scope.ExcludedDirectories.Any(excluded => IsUnderDirectory(relativePath, excluded)))
            {
                continue;
            }

            yield return file;
        }
    }

    /// <summary>Renders a hit list for an assertion message.</summary>
    /// <param name="hits">The hits to render.</param>
    /// <returns>A readable multi-line report, or a marker when there are none.</returns>
    internal static string Describe(IReadOnlyList<RepositoryScanHit> hits)
    {
        ArgumentNullException.ThrowIfNull(hits);

        return hits.Count == 0
            ? "(no hits)"
            : string.Join(
                Environment.NewLine,
                hits.Select(hit => $"{hit.RelativePath}:{hit.LineNumber}: {hit.Description} [{hit.Pattern}] -> {hit.Text}"));
    }

    private static bool IsUnderDirectory(string relativePath, string directoryName)
        => relativePath.StartsWith(directoryName + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || relativePath.Contains(Path.DirectorySeparatorChar + directoryName + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
}
