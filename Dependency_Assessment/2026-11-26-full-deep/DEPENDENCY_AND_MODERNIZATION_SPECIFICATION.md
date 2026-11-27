# Dependency and Modernization Specification — MTM_Waitlist

**Audit date**: 2026-11-26
**Invocation**: `/workspace-modernization-audit full --deep`
**Baseline commit**: `175cfc21` (`master`, clean working tree)
**Output folder**: this run is versioned beside the earlier 2026-11-26 AI-tooling run at
`Dependency_Assessment/`; that run's approved work is implemented and is not re-proposed here.
**Intended next step**: `/speckit.specify` against this file.

---

## 1. Overview

### Objectives

1. Bring every dependency to a supported, current release without breaking plant behaviour.
2. Replace hand-written code that a framework-native API or an already-referenced package already does.
3. Close the one credential exposure found in the repository.
4. Remove the identified interface stalls and plant-network round-trip amplifiers.
5. Make the AI development environment state only facts that measure true.
6. Repair documentation and design records that no longer match the code.

### Scope

Read-only assessment of all 11 audit areas across 17 projects, one solution, one application, one
on-host service, one database deployment tool, 14 instruction files, 92 agent files, 41 skill files and
1,079 resource keys.

This specification describes **approved work only** in its implementation sections. Thirty-two proposals
have a decision; the areas that produced nothing are recorded as verified healthy in section 13.

### What the audit does not do

It implements nothing. It changes no project file, package reference, source file, instruction file,
database artifact or configuration value. The only files written are this specification, the report, and
the HTML source the report is rendered from.

---

## 2. Workspace Context

| Item | Value |
| --- | --- |
| Repository | `JDKoll1982/MTM_Waitlist` |
| Branch / commit | `master` at `175cfc21` |
| Working tree | clean at audit start |
| Solution | `MTM_Waitlist.sln` |
| Projects | 17 |
| Target framework | `net10.0-windows10.0.19041.0` (16 projects), `net10.0` for `MTM_Database_Deployment.Core`, `net10.0-windows10.0.22621.0` for `MTM_Database_Deployment` |
| .NET SDK | 10.0.404 |
| Application version | 1.1.1 (`Directory.Build.props`) |
| UI framework | WinUI 3 on Windows App SDK 2.4.0 |
| Build result at audit | Build succeeded, 0 warnings, 0 errors |
| Package management | Per-project `PackageReference`; **no** `Directory.Packages.props`, **no** lock files. 22 distinct packages across 81 declarations |
| `NuGet.Config` | Present in the working tree only — **not tracked by git**, and never has been. It declares the single source `nuget.org` and no package source mapping |
| NuGet audit | Implicit (SDK default); reports no vulnerable package in any project |
| Database | MySQL Server 8.0.46 (plant host `172.16.1.104`, local `127.0.0.1`) |
| Tests | MSTest 3.7.0, 2,768 `[TestMethod]` across 363 classes, 17 `*AuditTests` guardrails |
| Editor | VS Code 1.148.0 |
| Workspace roots | 1 (single-root) |

### Project graph

`MTM_Waitlist` (app, composition root) references every other project except `MTM_Database_Deployment*`.
`MTM_Waitlist.Core` is the shared floor that 11 projects reference. No project-reference cycle exists.
Three dependency-direction problems were found and are addressed by MOD-006 and MOD-007:
`MTM_Waitlist.GenericRequest` → `MTM_Waitlist.Waitlist.View`, `MTM_Waitlist.GenericRequest.Data` →
`MTM_Waitlist.Waitlist.NewRequest`, and a `UserControl` hosted inside `MTM_Waitlist.Shared`.

### Existing behaviour that must survive every approved change

- Internal stores (`mtm_waitlist`, `mtm_wip_application_winforms`, `mtm_receiving_application`) are read and
  written live. No demo or mock mode may be introduced.
- Every data operation goes through a stored procedure.
- Only Infor Visual reads fall back to the `mtm_mock` mirror, and only on the reachability probe's settled
  verdict. A reachable-but-empty live answer is a real answer and is never replaced.
- The `MTM_Waitlist.Mock.Service` deployment script refuses to run off its own server host; that guard stays.
- There is no CI workflow, and warnings are not errors. No proposal adds an enforcement the build lacks.
- XAML views are included by SDK default globs; no explicit `<Page>` item may be added.

---

## 3. Dependency Inventory

Declared versions with resolution notes. "Latest" comes from per-project
`dotnet list package --outdated` and from package manifests on nuget.org, both checked 2026-11-26.

### Runtime and UI

| Package | Declared | Resolved | Latest stable | Support status |
| --- | --- | --- | --- | --- |
| Microsoft.WindowsAppSDK | 2.4.0 | 2.4.0 | 2.7.0 | Current, one major behind |
| CommunityToolkit.Mvvm | 8.4.2 | 8.4.2 | 8.4.2 | Current |
| CommunityToolkit.WinUI.UI.Controls | 7.1.2 | 7.1.2 | 7.1.2 (terminal) | Superseded by the 8.x line |
| CommunityToolkit.WinUI.UI.Animations | 7.1.2 | 7.1.2 | 7.1.2 (terminal) | Superseded by the 8.x line |
| CommunityToolkit.WinUI.Controls.SettingsControls | 8.2.251219 | 8.2.251219 | 8.2.251219 | Current |
| DevWinUI | 10.8.0 | 10.8.0 | 11.1.0 | One major behind |
| WinUIEx | 2.9.2 | 2.9.2 | 2.9.3 | Current, patch behind |
| Microsoft.Xaml.Behaviors.WinUI.Managed | 3.0.1 | 3.0.1 | 3.0.1 | Current |
| Microsoft.Windows.SDK.BuildTools | 10.0.28000.2270 | 10.0.26100.4654 (framework-pinned) | 10.0.28000.2957 | Patch behind |

### Data access

| Package | Declared | Resolved | Latest stable | Notes |
| --- | --- | --- | --- | --- |
| MySqlConnector | 2.6.1 | 2.6.1 | 3.0.0 | Major behind |
| Microsoft.Data.SqlClient | 6.1.1 | 6.1.1 | 8.0.0 | Published 2026-11-26 |
| System.Security.Cryptography.Pkcs | 9.0.21 (pin) | 9.0.21 | 11.0.0 | Pin removes NU1902 and NU1903 |
| System.Security.Cryptography.ProtectedData | 10.0.0 | 10.0.0 | 11.0.0 | Minor behind |

### Microsoft.Extensions

`Microsoft.Extensions.Hosting`, `.Configuration.Abstractions`, `.DependencyInjection.Abstractions`,
`.Logging.Abstractions` and `.Options` all declare 10.0.10 and resolve 10.0.10. Latest stable is 11.0.0,
whose package manifest carries a `net10.0` asset group, so no retarget is required.

### Test and build

| Package | Declared | Latest stable |
| --- | --- | --- |
| MSTest.TestAdapter / MSTest.TestFramework | 3.7.0 | 4.5.3 |
| Microsoft.NET.Test.Sdk | 17.13.0 | 18.12.0 |
| coverlet.collector | 6.0.2 | 10.1.1 |

### Transitive observations

- `Microsoft.Graphics.Win2D` resolves to **1.0.0.30** in `MTM_Waitlist.Core`, `MTM_Waitlist.Logging` and
  `MTM_Waitlist.Mock`, and to **1.4.0** in the other 14 projects. The 1.0.0.30 build ships
  version-specific `win10-x64`/`win10-x86`/`win10-arm64` runtime assets, which is the exact condition
  `UseRidGraph=true` exists to suppress. The 1.4.0 build ships portable `win-*` assets.
- `Microsoft.Extensions.Caching.Memory` and `Microsoft.Bcl.Cryptography` resolve at 9.0.4 through
  Microsoft.Data.SqlClient 6.1.1.
- No package resolves into a vulnerable range. `dotnet list package --vulnerable --include-transitive`
  reports "no vulnerable packages" for the app, the test project, the mock service and the deployment tool.

---

## 4. Approved Dependency Updates

All nine dependency proposals are approved. Detail per proposal follows; the approval register is
section 19.

### DEP-001 — Microsoft.WindowsAppSDK 2.4.0 → 2.7.0

| Field | Content |
| --- | --- |
| Target version | 2.7.0 |
| Projects | All 17 (declared in `MTM_Waitlist.csproj`, `MTM_Waitlist.Core`, `MTM_Waitlist.Shared`, `MTM_Waitlist.Logging`, `MTM_Waitlist.Settings`, `MTM_Waitlist.Setup`, `MTM_Waitlist.Startup`, `MTM_Waitlist.GenericRequest`, `MTM_Waitlist.GenericRequest.Data`, `MTM_Waitlist.Waitlist.Controls`, `MTM_Waitlist.Waitlist.View`, `MTM_Waitlist.Waitlist.NewRequest`, `MTM_Waitlist.Mock`, `MTM_Waitlist.Mock.Service`, `MTM_Database_Deployment`) |
| Required work | Version bump only if central package management has landed; otherwise one edit per project file. Full manual pass of sign-in, waitlist list and detail, setup, generic request, settings and the deployment tool. |
| Compatibility | Framework package for the whole UI. Must be validated against the WinUI 3 API surface actually used. |
| Breaking changes to watch | WinUI control templates and theming; the Windows App SDK sub-package split; app notification surface; title-bar behaviour used by `Helpers/TitleBarHelper.cs`. |
| Security impact | Neutral to positive: platform servicing. |
| Benefit | Platform currency, current WinUI fixes, continued support. |
| Migration | Single step; revert by restoring the previous version. |
| Source | nuget.org package metadata, checked 2026-11-26 |

### DEP-002 — Microsoft.Extensions.\* 10.0.10 → 11.0.0

Nine projects declare one or more of the five abstractions. The 11.0.0 manifest carries a `net10.0`
asset group, so the upgrade does not force a target-framework change. Watch configuration-binder behaviour
and logging filter evaluation. The benefit is currency across the hosting, logging and DI stack that
`App.xaml.cs` builds.

### DEP-003 — Test stack major upgrade

MSTest.TestAdapter and MSTest.TestFramework 3.7.0 → 4.5.3, Microsoft.NET.Test.Sdk 17.13.0 → 18.12.0,
coverlet.collector 6.0.2 → 10.1.1. Test project only. MSTest 4 is a major release: assertion overloads,
`[DataRow]` handling and the adapter's run settings changed. Acceptance requires the 2,768-method suite
green before and after with the same count.

### DEP-004 — Patch-level updates

WinUIEx 2.9.2 → 2.9.3, Microsoft.Windows.SDK.BuildTools 10.0.28000.2270 → 10.0.28000.2957,
System.Security.Cryptography.ProtectedData 10.0.0 → 11.0.0. Files: `MTM_Waitlist.csproj`,
`MTM_Waitlist.Core/MTM_Waitlist.Core.csproj`, `MTM_Waitlist.Mock.Service/MTM_Waitlist.Mock.Service.csproj`,
`MTM_Database_Deployment/MTM_Database_Deployment.csproj`. No behavioural expectation beyond the documented
fixes. Revert is a version-line revert.

### DEP-005 — Microsoft.Data.SqlClient 6.1.1 → 8.0.0, and retire the Pkcs pin

The `net10.0` asset group of SqlClient 8.0.0 requires `System.Security.Cryptography.Pkcs 10.0.15`, which is
outside the three vulnerable ranges (8.0.0–8.0.1, 9.0.0–9.0.20, 10.0.0–10.0.12) that the pin in
`Directory.Build.props` exists to escape. The pin therefore becomes unnecessary once this lands.

**Risk note carried forward**: 8.0.0 was published on the audit date and has no field history. The
implementer must prove Infor Visual connectivity through `IVisualQueryExecutor` and its live path, and must
keep the previous version recorded so a revert is one line.

### DEP-006 — MySqlConnector 2.6.1 → 3.0.0

The driver under all three internal stores. Watch connection-string option removals, `DateTime`/`TimeSpan`
mapping, cancellation behaviour and pool-size defaults. Acceptance is every stored-procedure call path
exercised against the local instance before the plant host is touched.

### DEP-007 — DevWinUI 10.8.0 → 11.1.0

Third-party control and composition library used by the app and the deployment tool. A major version that
overrides WinUI control styles, so every screen must be reviewed for visual regressions. Highest visual
risk on this list, therefore sequenced last among the runtime upgrades.

### DEP-008 — Central package management

Create `Directory.Packages.props` with `ManagePackageVersionsCentrally=true` and one `PackageVersion` per
package, and remove each `Version=` from the 17 project files. Genuine per-project exceptions use
`VersionOverride`. This is the enabler for the seven version changes above becoming one-line edits, and it
prevents two projects drifting onto different versions of the same package.

### DEP-009 — Commit a NuGet configuration with package source mapping

There is **no tracked NuGet configuration at all**. A `NuGet.Config` exists in the working tree that declares
only `nuget.org`, but git has never tracked it (`git ls-files` returns nothing for any NuGet configuration
file, and `git log --all -- NuGet.Config` is empty), so every clone falls back to whatever that machine
happens to have. The change is to commit a `NuGet.Config` that declares `nuget.org` and adds
`packageSourceMapping`: a `nuget.org` pattern set covering the packages in use. A wildcard fallback is added
only if a second feed is ever introduced.

This is two fixes in one line of work: the feed configuration becomes reproducible across machines, and the
dependency-confusion guard exists before it is needed. Four lines, no runtime effect.

---

## 5. Existing Custom Functionality

Every row is a place where the repository implements something a framework, a referenced package or a
supported platform API already provides, or where redundancy has accumulated.

| ID | Path and member | Current behaviour | Limitation | Reason to modernize |
| --- | --- | --- | --- | --- |
| MOD-001 | `MTM_Waitlist.csproj`, `MTM_Waitlist.Core/MTM_Waitlist.Core.csproj`, `Module_Waitlist/Views/WaitlistViewPage.xaml:6-7`, `Module_Waitlist/Views/WaitlistViewDetailPage.xaml:5`, `Module_Waitlist/Views/WaitlistViewDetailPage.xaml.cs:1`, `MTM_Waitlist.Core/Services/NavigationService.cs:3` | Two 7.1.2 Community Toolkit packages. The `Controls` namespace is declared but no control from it is used. `Animations` supplies only `RegisterElementForConnectedAnimation` and `SetListDataItemForNextConnectedAnimation`. | The 7.1.2 line is terminal; the successor line renamed the packages and is already adopted for SettingsControls. | One dead package reference, one terminal package, four files. |
| MOD-002 | `MTM_Waitlist.Core/Services/UserManagementRepository.cs:259,377`, `PermissionAdministrationService.cs:477`, `LuggerArrangementService.cs:403`, `MTM_Waitlist.Shared/Services/WorkCenterCatalogService.cs:295`, `ConnectionHealthService.cs:108` | Services open their own `MySqlConnection` and `MySqlCommand`. | Retry policy, timeout and availability tracking in `MySqlHelperServer` do not apply. | Ten sites duplicating infrastructure that already exists. |
| MOD-003 | `MTM_Waitlist.Core/Services/MySqlHelperServer.cs:122,143,262-269` | A `CommandType.Text` path accepts caller-supplied statement text; `ExecuteSqlQueryAsync` runs it through `ExecuteReaderAsync` with no row cap. | An inline-SQL escape hatch the audit test cannot see, plus an unbounded read. | The stored-procedure rule should be true, not nearly true. |
| MOD-004 | `MTM_Waitlist.Settings/Models/UserListFilter.cs:34`, `MTM_Waitlist.Mock.Service/Services/ServiceConfigurationStore.cs:187`, `BackupArtifactStore.cs:26`, `RefreshRunRecordStore.cs:33`, `RunHistoryStore.cs:73`, and about 17 further files | Reflection-based `JsonSerializer`; each store builds its own `JsonSerializerOptions`. | No source-generated context, so trimming and start-up cost are not addressed. | Mechanical, low risk, removes an AOT blocker. |
| MOD-005 | `MTM_Waitlist.Core/Services/InforVisualSqlQueryService.cs`, `MTM_Waitlist.Setup/Services/InforVisualSqlQueryService.cs` | Two implementations of the same Infor Visual query path. | Read behaviour, fallback and timeout policy can drift. | One owner for one path. |
| MOD-006 | `MTM_Waitlist.Shared/Dialogs/MessageDialogContent.xaml.cs:14` | A `UserControl` lives in a library intended to hold no view types. | Every library that references `Shared` links XAML. | The stated layering rule is violated. |
| MOD-007 | `MTM_Waitlist.GenericRequest/MTM_Waitlist.GenericRequest.csproj:23`, `Services/GenericRequestLiveStepData.cs:12-13`, `MTM_Waitlist.GenericRequest.Data/MTM_Waitlist.GenericRequest.Data.csproj:23`, `Services/GenericRequestDieData.cs:7` | A logic library and a data library reference UI projects. | A data project depends on a wizard project; the dependency direction is not one-way. | Removes a UI dependency from the data layer. |
| MOD-008 | `MTM_Waitlist.Settings/SettingsHub/ViewModels/SettingsViewModel.cs` | 2,623 lines and 106 public members; its test file is 1,124 lines. | Largest file in the repository; a top-three churn target. | Reviewability and testability. |
| MOD-009 | `Module_Shared/Controls/RotatingTextView.xaml.cs:138-195` | Hand-built `DoubleAnimation` and `Storyboard`. | A second animation idiom beside the toolkit's own. | Less code, consistent easing. |

---

## 6. Approved Replacements

| ID | Existing solution | Replacement | Version | Alternatives considered | Benefit | Risk | Effort | Approval |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| MOD-001 | `CommunityToolkit.WinUI.UI.Controls` and `.Animations` 7.1.2 | `CommunityToolkit.WinUI.Animations` 8.2.251219 for the connected-animation extensions, and removal of the unused `Controls` reference | 8.2.251219 | Keep 7.1.2 (terminal, no fix path); hand-write connected animations (reimplements a solved problem); drive `ConnectedAnimationService` directly (works, loses the element-registration helpers) | Removes a terminal package line and a dead reference; aligns with the 8.x line already in use | Connected animations must still run on the waitlist-to-detail transition | Small | APPROVED |
| MOD-002 | Direct `MySqlConnection`/`MySqlCommand` in five services | The `MySqlHelperServer` seam | existing | Build a second abstraction (rejected: two seams are worse than one); leave as-is (rejected: ten copies of retry policy) | One retry, timeout and failure surface | Callers must not lose behaviour the seam does not model | Medium | APPROVED |
| MOD-003 | The `CommandType.Text` path in `MySqlHelperServer` | Remove the path; keep stored-procedure execution only | existing | Keep and document (rejected: a documented escape hatch is still an escape hatch) | The stored-procedure rule becomes enforceable | A caller may depend on it, so confirm before removal | Small | APPROVED |
| MOD-004 | Reflection-based `JsonSerializer` | `JsonSerializerContext` source generation per store | .NET 10 built-in | Keep reflection (leaves the trimming blocker); add Newtonsoft (a package for no gain) | Faster serialisation, trimming readiness | Polymorphic types need explicit handling | Medium | APPROVED |
| MOD-005 | Two `InforVisualSqlQueryService` types | One implementation in `MTM_Waitlist.Core`, referenced by Setup | existing | Keep both with a shared base (keeps the drift) | One Visual read path | A host-specific difference may be load-bearing | Medium | APPROVED |
| MOD-006 | `MessageDialogContent` in `MTM_Waitlist.Shared` | Move to the app's `Module_*/` view layer | n/a | Keep in `Shared` (then `Shared` cannot be view-free); inline a `ContentDialog` per call site (duplicates a solved composition) | `Shared` becomes view-free | Every caller must be updated | Medium | APPROVED |
| MOD-007 | `GenericRequest[.Data]` → `Waitlist.View` and `Waitlist.NewRequest` | Extract the shared contracts into `Core` or `Shared` | n/a | Leave as-is (data depends on UI); merge the projects (destroys module boundaries) | One-way dependency direction | Large blast radius across the generic-request flow | Large | APPROVED |
| MOD-008 | The 2,623-line `SettingsViewModel` | Cohesive sub-view-models behind the existing page | n/a | Leave as-is (it is a top churn target after the resource file) | Reviewability, testability | The whole settings surface is bound to it | Large | APPROVED |
| MOD-009 | The hand-built `Storyboard` in `RotatingTextView` | A toolkit animation helper | 8.2.251219 | Leave as-is (two idioms for one job) | Less code | Visual parity must be checked | Small | APPROVED |

---

## 7. File-Level Change Requirements

Mandatory unless marked optional. "Owner" is the proposal that justifies the edit.

| Path | Existing implementation | Required modification | Owner | Mandatory | Expected outcome |
| --- | --- | --- | --- | --- | --- |
| `appsettings.json` | Waitlist and Receiving connection strings containing passwords; an Infor Visual server, user and password triple | Replace every credential value with an empty string; keep the `ConnectionStringEnvironmentVariable` names as the single documented path | SEC-001 | Yes | No secret value in the repository; each host supplies its own |
| `MTM_Waitlist.Settings/SettingsHub/ViewModels/SettingsViewModel.cs:1378` | `.GetAwaiter().GetResult()` during load | Await the call and make the initialising path async | PERF-002 | Yes | No UI-thread block during settings load |
| `MTM_Waitlist.Settings/Services/ImageStorageService.cs:403` | `.GetAwaiter().GetResult()` | Await; propagate async through the callers | PERF-002 | Yes | No UI-thread block |
| `App.xaml.cs:591` | `Task.Run(...).Wait(TimeSpan.FromSeconds(2))` at shutdown | Await with a cancellation-bounded flush | PERF-002 | Yes | Shutdown does not block the dispatcher |
| `Strings/en-us/Resources.resw` | Two referenced keys absent | Add `Setup_Review_Scrap_None` and `Setup_Review_Section_SubordinateParts_Count` | UX-001 | Yes | Setup review shows words, not key names |
| `MTM_Waitlist.Setup/ViewModels/SetupReviewViewModel.cs:95,109` | Resolves keys that do not exist | No code change expected; verify both resolve after the resource edit | UX-001 | Yes | Same |
| `.github/copilot-startup-steps.yaml:21` | `Microsoft.WindowsAppSDK 2.3.1` | `2.4.0`, and re-verify on each platform bump | AI-001 | Yes | Agents cite the right platform documentation |
| `Directory.Build.props:47` | Reasons about "the 2.3.1 in use" | Restate against 2.4.0, or replace with the rule and no version number | AI-001 | Yes | The comment matches the tree |
| `.github/memories/repo/github-artifact-staleness.md:37,56` | Records 2.3.1 as a reference fact | Correct to 2.4.0 and mark the figure as re-measured on 2026-11-26 | AI-001 | Yes | The memory note is trustworthy |
| `.github/hooks/speckit.json` | The `bash` arm calls `python3` | `python` in the `bash` arm, matching the `powershell` arm | AI-002 | Yes | The hook does not silently no-op |
| `.mcp.json` | `{"mcpServers": {}}` | Add `microsoft-learn` (HTTP `https://learn.microsoft.com/api/mcp`) and `context7` (stdio `npx @upstash/context7-mcp`) | AI-003 | Yes | Documentation lookup is available to every clone |
| `.github/memories/repo/mcp-tooling.md` | Does not record the NuGet MCP outage | Add the failure, its signature and the `dotnet list package` workaround | AI-004 | Yes | The dead end is not repeated |
| `docs/operations/operations-mock-service-install-script.md:79,103-112` | `./deploy/install-mock-service.ps1` | `MTM_Waitlist.Mock.Service/deploy/install-mock-service.ps1` | DEV-002 | Yes | The documented install command works |
| `docs/operations/operations-database-live-and-mock-updates.md:211` | The same wrong path | The same correction | DEV-002 | Yes | Same |
| `docs/living-specs/living-specs-drift-ledger.md:118` | Cites a memory note that does not exist | Point at the note that owns the statement, or drop the citation | DEV-002 | Yes | The link resolves |
| `docs/defects/defects-coil-partial-high-average-weight-is-resolved-for-one-hardcoded-part.md:77` | Cites a SQL file that does not exist | Point at the current query under `Database/InforVisual/Queues/` | DEV-002 | Yes | The citation resolves |
| `docs/startup/startup-rebuild-design-brief.md:229` | Describes a removed `tools/` script in the present tense | Mark it historical, with the date it was removed | DEV-002 | Yes | The reader is not sent to a missing tool |
| `docs/diagrams/tools/README.md:71` | Links a missing file | Fix or remove the link | DEV-002 | Yes | The link resolves |
| `MTM_Waitlist.Tests/Module_Startup/Services/VisualVerdictPrimingStepTests.cs:209` | `Task.Delay(TimeSpan.FromSeconds(3))` | Inject a clock or a bound; assert the verdict, not the wait | DEV-003 | Yes | The suite is faster and the assertion is stronger |
| `docs/ui-designs/startup-launch/sign-in/{name-the-authority,no-offer-explained,terminal-aware-offer}/*-comparison.pdf` | Predate their design JSON | Regenerate from the current design source | DEV-004 | Yes | The design record matches the built screens |
| `Directory.Build.props:50` | `<UseRidGraph>true</UseRidGraph>` | Remove after MOD-001 unifies Win2D to 1.4.0; verify a cold restore raises no `NETSDK1206` | DEV-001 | Yes, after MOD-001 | No tracked RID-graph accommodation |
| `Directory.Packages.props` (new) | Does not exist | Create central package management | DEP-008 | Yes | One version per package |
| All 17 `*.csproj` | An inline `Version=` on each `PackageReference` | Remove the attribute once central management holds it | DEP-008 | Yes | Same |
| `NuGet.Config` | Untracked working-tree file; git has never tracked a NuGet configuration | Create and commit a configuration declaring `nuget.org` with `packageSourceMapping` | DEP-009 | Yes | Packages are pinned to their feed, and the feed set is reproducible across clones |
| `MTM_Waitlist.GenericRequest/Views/GenericRequestPartPage.xaml.cs:31-45` | The search fires per keystroke | Debounce with a `CancellationToken` and cancel the superseded search | PERF-001 | Yes | One visual round trip per typing pause |
| `MTM_Waitlist.Waitlist.View/Services/AverageCoilWeightService.cs:56` | `InvariantCulture` plus a literal `" lb"` | Current-culture number formatting; unit from a resource | UX-003 | Yes | Correct separators; no hardcoded unit |
| 140 XAML attributes across `Module_Core`, `Module_Settings`, `Module_Waitlist`, `Module_Shared` | Literal prose in `Text`, `Header`, `Content`, `PlaceholderText` | Move to `.resw` and resolve through one convention | UX-002 | Yes | A translatable interface and one idiom |

---

## 8. Cross-Project Changes

| Change | Shared artifact | Projects affected | Ordering constraint |
| --- | --- | --- | --- |
| Central package management | `Directory.Packages.props` | All 17 | Must precede every version bump |
| Package source mapping | `NuGet.Config` (to be tracked for the first time) | All 17 | Independent |
| Security pin retirement | `Directory.Build.props` | All 17 (the pin is solution-wide) | After DEP-005 |
| RID-graph accommodation removal | `Directory.Build.props` | All 17 | After MOD-001 |
| Microsoft.Extensions bump | Five abstraction packages | 9 projects | Independent of the test stack |
| Data-access seam adoption | `MTM_Waitlist.Core/Services/MySqlHelperServer.cs` | 5 projects | After DEP-006, so the driver is settled first |
| Stored-procedure-only enforcement | `MySqlHelperServer` | All persistence callers | With or before MOD-002 |
| Community Toolkit replacement | Two package references plus four source files | App and Core | Before DEV-001 |

---

## 9. AI Development Environment

### What is installed today

| Surface | State at audit |
| --- | --- |
| `.mcp.json` | `{"mcpServers": {}}` — no repository-declared server |
| User-level MCP | Microsoft Learn (HTTP), Context7 (stdio), Playwright (stdio) — this workstation only, not in the repository |
| `.github/instructions` | 14 files: 4 always-applied (43,336 bytes), 10 glob-scoped (62,516 bytes) |
| `AGENTS.md` | 5,374 bytes, always loaded |
| `.github/agents` | 92 files, 638,227 bytes (90 `speckit.*`, 2 repository agents) |
| `.github/skills` | 41 files, 258,408 bytes across 19 skill folders |
| `.github/prompts` | 61 files, 4,528 bytes (thin pointer stubs) |
| `.github/memories` | 14 notes |
| `.github/hooks/speckit.json` | Two hooks; the `bash` arm is broken (AI-002) |
| Spec Kit | 1.1.3, active feature `specs/023-ai-tooling-remediation` |

Always-loaded context is therefore about 48,710 bytes (~12,000 tokens) per request before any file is
read. The previous audit reduced the two project instruction files and that work is not reopened here.

### What this audit proposes

| ID | Change | Separated from the installed state |
| --- | --- | --- |
| AI-001 | Correct a stale platform version in three agent-facing files | The files exist; the value is wrong |
| AI-002 | Fix the `python3` hook arm | The hook exists; one arm cannot run |
| AI-003 | Declare Microsoft Learn and Context7 in `.mcp.json` | A new repository-level declaration; user-level entries already work on this machine |
| AI-004 | Record the NuGet MCP outage in the tooling note | New content in an existing note |

### Permission requirements for AI-003

The Microsoft Learn server is an HTTP endpoint and needs outbound HTTPS to `learn.microsoft.com`. The
Context7 server runs `npx` and therefore needs Node.js on the machine plus outbound HTTPS to
`registry.npmjs.org` and the Context7 service. Both expose documentation search only; neither reads the
repository. A team that cannot reach either endpoint should reject AI-003 rather than partially declare it.

### Tooling defect recorded, not fixed

The NuGet MCP server cannot analyse this solution. Every one of its tools fails with
`Could not load file or assembly 'NuGet.Frameworks, Version=7.9.0.0'` while evaluating the target
framework against .NET SDK 10.0.404, and `dotnet list <solution> package --outdated` fails at solution
scope with "Sequence contains no matching element" (it works per project). All version evidence in this
specification therefore comes from per-project `dotnet list package --outdated` and `--vulnerable`, and
from package manifests fetched from the nuget.org flat container.

---

## 10. Security Requirements

### Findings

| ID | Finding | Severity | Decision |
| --- | --- | --- | --- |
| SEC-001 | `appsettings.json` is tracked in git and carries a Waitlist connection string containing a password, a Receiving connection string containing a password, and an Infor Visual server, user and password | High — credential exposure in version control | APPROVED |

No vulnerable package was found: `dotnet list package --vulnerable --include-transitive` reports none for the
app, the test project, the mock service or the deployment tool. The `System.Security.Cryptography.Pkcs` pin
in `Directory.Build.props` is doing its job and is retired only as part of DEP-005.

### Requirement for SEC-001

1. Commit the configuration *shape* only. No credential value remains in any tracked file.
2. Each deployment supplies values through the existing environment-variable mechanism
   (`ConnectionStringEnvironmentVariable` is already set for both internal stores).
3. A missing variable must fail loudly at start-up with a message naming the variable, never fall back to
   an empty connection string or a silent placeholder.
4. Every value that has been committed must be treated as disclosed and rotated on the plant host. This
   audit does not print, copy or transmit any value.
5. The rotation and the provisioning step are operational work outside this repository, recorded as a
   prerequisite rather than as code.

### Licensing

All approved packages are MIT, Apache-2.0 or Microsoft-licensed; no copyleft obligation is introduced.

---

## 11. Performance Requirements

| ID | Requirement | How it is validated |
| --- | --- | --- |
| PERF-001 | A part search reaches Infor Visual once per typing pause, not once per keystroke, and a superseded search cannot overwrite a newer result | Instrument the search call count for a scripted ten-character input; expect a small single-digit count rather than about ten, and assert last-writer-wins on out-of-order completion |
| PERF-002 | No `.GetAwaiter().GetResult()` or `.Wait(` blocks the interface thread during settings load or shutdown | Reproduce a slow configuration read and confirm the window keeps painting; assert no `.GetAwaiter().GetResult()` remains on the loader path |

No performance claim in this specification is a measured benchmark. Both requirements are stated as an
observable behaviour with a stated measurement, because neither was profiled in a running build: the audit
read code and did not execute the application.

### Performance findings deliberately not raised

- The per-work-centre picture resolution loop (`WaitlistViewViewModel.cs:1870`) runs once per work centre,
  not once per row, and a code comment states that choice explicitly.
- The `GetLocalized()` property getters in `ViewModels/ShellViewModel.cs:145-159` are evaluated once by
  `{x:Bind}` OneTime.
- The 70 `async void` handlers are covered by the global handler registered at `App.xaml.cs:200`.

---

## 12. UX and Maintainability Requirements

| ID | Requirement |
| --- | --- |
| UX-001 | The Setup review page displays words, not resource key names, in both places that currently fall back to the key |
| UX-002 | No user-visible sentence is a literal in XAML, and one resource convention applies across the app |
| UX-003 | A coil weight shows the machine's number formatting and takes its unit from a resource |

Maintainability requirements are the modernization rows in sections 5 and 6.

### Localization inventory

1,079 keys across three `.resw` files. About 298 keys are not referenced by any quoted literal and are not
reached through an `x:Uid` prefix; that figure counts dynamically composed permission and role keys as
unreferenced, so it is an upper bound. Thirteen keys carry the value `Back`, eight carry `Cancel`, eight
`Close` and eight `Continue`. Two referenced keys are missing entirely. 140 XAML attributes carry literal
prose. There are 159 `x:Uid` values against 1,721 `{x:Bind}` usages, so no single convention governs strings.

**Not proposed**: deleting the unused keys. That is about 298 deletions across a corpus that composes keys at
run time, the benefit is maintenance only, and a wrong deletion is a runtime regression. Recorded here so
the option is not lost.

---

## 13. Architecture and Compatibility Requirements

### Architecture: inspected, healthy, no proposal

| Check | Result |
| --- | --- |
| Project-reference cycles | None across the project-reference edges |
| Stored-procedure artifact coverage | 204 `create.sql` files with 204 matching `rollback.sql`. The 50 procedure folders holding only a rollback are deliberately retired procedures (commit `576492c4`), not gaps |
| Master-list aggregates | `Database/StoredProcedures/AllSPs.sql` holds exactly the 107 live procedure definitions and is consistent with all 107 `create.sql` files; `build_mtm_mock_masters.ps1 -Check` is the existing drift gate |
| Index coverage on the hot waitlist table | `Database/Tables/18_waitlist_requests_queue/create.sql:73-79` carries indexes for building plus status, work centre, requested time, status, item plus status, and generic |
| Guardrail tests | All 17 `*AuditTests` assert a real contract; several read source text, which is this repository's chosen idiom and is deliberate |
| Database area overall | No proposal. The area was inspected and produced nothing worth a change |

### Compatibility requirements per approved change

| Change | Constraint |
| --- | --- |
| DEP-001 | Windows App SDK 2.7.0 must support `net10.0-windows10.0.19041.0` |
| DEP-002 | 11.0.0 carries a `net10.0` asset group, verified from the manifest |
| DEP-003 | MSTest 4.5.3 targets `net9.0`; it runs on `net10.0` but ships no `net10.0` asset of its own |
| DEP-005 | Requires `System.Security.Cryptography.Pkcs 10.0.15`; the 9.0.21 pin must go at the same time |
| MOD-001 | `CommunityToolkit.WinUI.Animations` 8.2.251219 supplies the two connected-animation extensions |
| MOD-004 | Source-generated JSON needs an explicit context per store type |
| DEV-001 | Only after MOD-001, and only when Win2D resolves 1.4.0 in Core, Logging and Mock |

---

## 14. Implementation Plan

Eight ordered phases. Each phase ends green: build succeeded, test project green, and the phase's own
checks met. Nothing in a later phase begins before its prerequisites are met.

### Phase 1 — Immediate correctness and hygiene

**Objective**: remove the credential exposure and the small, independent defects with no architectural risk.
**Projects**: app, `MTM_Waitlist.Settings`, `MTM_Waitlist.Setup`, documentation, AI configuration.
**Changes**: SEC-001, PERF-002, UX-001, AI-001, AI-002, DEV-002, AI-004.
**Prerequisites**: an operational plan for supplying the environment variables on each host (SEC-001).
**Phase dependencies**: none. Every item is independent and separately revertable.
**Completion**: no credential value in a tracked file; both settings paths await; the Setup review shows
two real strings; three agent-facing files cite 2.4.0; the hook uses `python`; five documentation links
resolve; the tooling note records the NuGet outage.

### Phase 2 — Dependency infrastructure

**Objective**: make every later version change a one-line edit.
**Changes**: DEP-008, then DEP-009, then DEP-004.
**Prerequisites**: a green baseline build, confirmed at 0 warnings and 0 errors.
**Phase dependencies**: DEP-004 lands after DEP-008 so the patch bumps are single lines.
**Completion**: `Directory.Packages.props` holds every version once; a cold restore succeeds; the three
patch bumps are in place; `NuGet.Config` carries source mapping.

### Phase 3 — Runtime package upgrades

**Objective**: bring the runtime stack current in ascending order of blast radius.
**Changes**: DEP-002 → DEP-006 → DEP-005 → DEP-001 → DEP-007.
**Prerequisites**: Phase 2 complete.
**Phase dependencies**: strictly sequential; each upgrade is validated before the next begins.
**Completion**: each package at its target version; a full manual pass of sign-in, waitlist, setup, generic
request and settings after DEP-001 and again after DEP-007; Infor Visual reads proven after DEP-005;
internal-store reads and writes proven after DEP-006.

### Phase 4 — Test stack and test quality

**Objective**: current test tooling, and one slow test made both faster and stronger.
**Changes**: DEP-003 → DEV-003.
**Prerequisites**: Phases 1 to 3 green, so the suite measures settled code.
**Phase dependencies**: DEP-003 first, because DEV-003 edits a test the new adapter must still run.
**Completion**: 2,768 methods discovered and green under MSTest 4; no test waits on a real clock for more
than 500 ms.

### Phase 5 — Framework-native replacement

**Objective**: retire the terminal toolkit line and the duplicated infrastructure.
**Changes**: MOD-001 → DEV-001 → MOD-003 → MOD-002 → MOD-005 → MOD-004 → MOD-009.
**Prerequisites**: Phase 3 complete.
**Phase dependencies**: DEV-001 requires MOD-001 for Win2D unification. MOD-002 follows MOD-003 so the seam
is tightened before it is adopted more widely.
**Completion**: no 7.1.2 package remains; a cold restore raises no `NETSDK1206` with `UseRidGraph` gone;
`MySqlHelperServer` executes stored procedures only; the five named services route through it; one Visual
query service; JSON contexts generated; one animation idiom.

### Phase 6 — Architectural modernization

**Objective**: correct the dependency direction and break up the largest class.
**Changes**: MOD-006 → MOD-007 → MOD-008.
**Prerequisites**: Phase 5 complete, so data-access and toolkit churn is settled first.
**Phase dependencies**: sequential; each is its own review cycle.
**Completion**: `MTM_Waitlist.Shared` contains no view type; `GenericRequest[.Data]` references no UI
project; `SettingsViewModel` is decomposed with the settings surface unchanged.

### Phase 7 — Performance and localization

**Objective**: fewer plant round trips, correct formatting, one string convention.
**Changes**: PERF-001, UX-003, UX-002.
**Prerequisites**: Phase 6 complete, so the XAML being edited is the final layout.
**Completion**: the search-call measurement in section 11 holds; the coil weight formats for the machine;
no literal prose remains in XAML.

### Phase 8 — AI environment and design records

**Objective**: the repository declares the tools it needs, and the design record matches what was built.
**Changes**: AI-003, DEV-004.
**Prerequisites**: Phase 7 complete, so the design PDFs are regenerated from final screens.
**Completion**: a fresh clone opens the workspace with both documentation servers declared and one
answering; the three comparison PDFs are newer than their design source.

### Approval IDs by phase

| Phase | Proposals |
| --- | --- |
| 1 | SEC-001, PERF-002, UX-001, AI-001, AI-002, DEV-002, AI-004 |
| 2 | DEP-008, DEP-009, DEP-004 |
| 3 | DEP-002, DEP-006, DEP-005, DEP-001, DEP-007 |
| 4 | DEP-003, DEV-003 |
| 5 | MOD-001, DEV-001, MOD-003, MOD-002, MOD-005, MOD-004, MOD-009 |
| 6 | MOD-006, MOD-007, MOD-008 |
| 7 | PERF-001, UX-003, UX-002 |
| 8 | AI-003, DEV-004 |

---

## 15. Testing and Verification Strategy

| Gate | Applies to | Evidence required |
| --- | --- | --- |
| Cold restore | Every dependency phase | `dotnet restore` with `--force` from a cleared package cache; no new warning |
| Clean build | Every phase | `dotnet build MTM_Waitlist.sln` — 0 errors; warnings reported, never suppressed |
| Test suite | Every phase from 2 onward | `dotnet test MTM_Waitlist.Tests\MTM_Waitlist.Tests.csproj` green, method count unchanged unless the phase changes it |
| Guardrail tests | Every phase | The 17 `*AuditTests` pass; a change that trips one is a design decision, not a test to edit |
| Manual UI pass | DEP-001, DEP-007, MOD-001, MOD-006, MOD-008, UX-002 | Sign-in, waitlist list and detail, setup, generic request, settings and the deployment tool — each opened and checked per display scale |
| Connectivity | DEP-005, DEP-006 | Internal-store read and write against the local instance, then an Infor Visual read through the live path |
| Data integrity | SEC-001 | A first run with no environment variables fails with a message naming the variable; a run with them succeeds |
| Rollback | Every dependency change | The previous version recorded before the bump, so a revert is one line |

No test framework, runner or CI mechanism is introduced. The repository has no CI workflow and one is not
proposed.

---

## 16. Acceptance Criteria

1. Every approved dependency is at its target version (section 4), and no project declares a different
   version of the same package.
2. `dotnet restore` succeeds from a clean state and raises no `NETSDK1206` once DEV-001 has removed
   `UseRidGraph`.
3. `dotnet build MTM_Waitlist.sln` reports 0 errors in every phase.
4. `dotnet test MTM_Waitlist.Tests\MTM_Waitlist.Tests.csproj` is green after every phase, with 2,768 methods
   discovered unless a phase is approved to change that count.
5. The 17 `*AuditTests` guardrails pass, including `InlineSqlAuditTests` and `RetiredSymbolAuditTests`.
6. No credential value exists in any tracked file, and a host without the environment variables fails loudly
   at start-up.
7. No 7.1.2 Community Toolkit package remains, and connected animations still run from the waitlist list to
   the detail page.
8. `MySqlHelperServer` executes stored procedures only, and the five named services route through it.
9. `MTM_Waitlist.Shared` contains no `UserControl`, `Page` or `Window` type.
10. `MTM_Waitlist.GenericRequest` and `MTM_Waitlist.GenericRequest.Data` reference no `MTM_Waitlist.Waitlist.*`
    project.
11. No `.GetAwaiter().GetResult()` or `.Wait(` remains on the settings-load or shutdown path.
12. The Setup review page shows two real strings where it previously showed key names.
13. The part search issues one plant round trip per typing pause, and a superseded search cannot overwrite a
    newer result.
14. No literal prose `Text`, `Header`, `Content` or `PlaceholderText` attribute remains in XAML.
15. The three agent-facing files cite Windows App SDK 2.4.0, and the Spec Kit hook does not invoke `python3`.
16. The five documentation paths in DEV-002 resolve, and the three comparison PDFs are newer than their
    design JSON.
17. Every file listed in section 7 was changed by the proposal that owns it, and no proposal outside section
    19 was implemented.
18. No package, project or library outside the section 7 list changed.

---

## 17. Risks and Constraints

| Risk | Affects | Mitigation |
| --- | --- | --- |
| Windows App SDK 2.7.0 changes WinUI theming or a control used across every screen | DEP-001 | Full manual pass before merge; single-line revert under central package management |
| DevWinUI 11.x overrides control styles | DEP-007 | Sequenced last of the runtime upgrades; every screen reviewed |
| SqlClient 8.0.0 has no field history | DEP-005 | Approved with a stated connectivity gate and a one-line revert |
| MySqlConnector 3.0.0 changes date or connection-string behaviour | DEP-006 | Local-instance validation before the plant host is touched |
| MSTest 4 changes assertion or discovery behaviour | DEP-003 | Count and green-run comparison before and after |
| Central package management changes every project at once | DEP-008 | A restore-and-build gate before any version bump is applied |
| Removing credentials stops a machine that relied on the committed values | SEC-001 | Provision the environment variables first; fail loudly rather than silently |
| `SettingsViewModel` decomposition touches the whole settings surface | MOD-008 | Behaviour-preserving extraction with the 1,124-line test file as the safety net |
| Breaking the GenericRequest to Waitlist references has a wide blast radius | MOD-007 | Phase 6, after data access is settled; separate review cycle |
| Removing the inline-SQL path breaks an unknown caller | MOD-003 | Grep every caller before removal; the seam is small |
| Win2D unification does not happen as predicted | DEV-001 | The gate is an actual cold restore; if `NETSDK1206` appears, keep `UseRidGraph` and record why |
| Both documentation endpoints are unreachable on a locked-down machine | AI-003 | Reject rather than partially declare |

Constraints honoured throughout: no demo or mock mode; stored-procedure-only data access; no explicit
`<Page>` items; the mock-service deployment guard untouched; no CI enforcement added.

---

## 18. Out of Scope

1. Deleting the ~298 unreferenced resource keys (section 12).
2. Any UX redesign. No layout or interaction change is proposed, and any future one must carry a rendered
   BEFORE/AFTER design under the repository's `qol-ui-design` rule.
3. Trimming the 90 `speckit.*` agent files or the 61 prompt stubs. Their cost is metadata only, and the
   previous audit already removed the entries that could not load.
4. The user-level VS Code configuration on this workstation: user instruction files, user-level MCP servers
   and installed extensions were not audited, because `--user-config` was not passed.
5. Any change to a running store. No MySQL connection was opened and no plant host was contacted.
6. Migration to a new database engine, ORM, UI framework or test framework.
7. Profiles, benchmarks or load tests. No performance number in this document is measured.
8. Rewriting the historical `specs/**` documents that cite 2.3.1. They record what was true when each
   specification was written.
9. Any proposal that did not reach a decision. There are none: all 32 were decided.

---

## 19. User Decisions and Approval Register

Decided 2026-11-26 in six batched interviews. Thirty-two proposals, thirty-two decisions.

| Proposal ID | Proposed Change | Category | Priority | Decision | Notes |
| --- | --- | --- | --- | --- | --- |
| DEP-001 | Microsoft.WindowsAppSDK 2.4.0 to 2.7.0 | Dependency | high | APPROVED | Platform currency; full manual UI pass required |
| DEP-002 | Microsoft.Extensions.* 10.0.10 to 11.0.0 | Dependency | medium | APPROVED | 11.0.0 carries a net10.0 asset group |
| DEP-003 | MSTest 4.5.3, Test SDK 18.12.0, coverlet 10.1.1 | Dependency | medium | APPROVED | Test-only; count and green run compared |
| DEP-004 | Patch updates: WinUIEx 2.9.3, BuildTools 10.0.28000.2957, ProtectedData 11.0.0 | Dependency | low | APPROVED | Three low-risk bumps, one decision |
| DEP-005 | Microsoft.Data.SqlClient 8.0.0 and retire the Pkcs 9.0.21 pin | Dependency | medium | APPROVED | Approved with the stated connectivity gate; 8.0.0 is a same-day release |
| DEP-006 | MySqlConnector 2.6.1 to 3.0.0 | Dependency | medium | APPROVED | The driver under all three internal stores |
| DEP-007 | DevWinUI 10.8.0 to 11.1.0 | Dependency | medium | APPROVED | Highest visual risk; sequenced last |
| DEP-008 | Adopt central package management | Dependency | medium | APPROVED | Enabler for the version changes |
| DEP-009 | Add NuGet package source mapping | Dependency | medium | APPROVED | No NuGet configuration is tracked at all, so committing one fixes reproducibility as well as the guard |
| MOD-001 | Replace Community Toolkit 7.1.2 with the 8.x line | Modernization | high | APPROVED | Four files; one dead reference, one terminal package |
| MOD-002 | Route all data access through MySqlHelperServer | Modernization | medium | APPROVED | About ten sites |
| MOD-003 | Close the inline-SQL escape hatch | Modernization | high | APPROVED | Makes the stored-procedure rule true |
| MOD-004 | System.Text.Json source generation | Modernization | medium | APPROVED | About 22 files |
| MOD-005 | Consolidate InforVisualSqlQueryService | Modernization | medium | APPROVED | One Visual read path |
| MOD-006 | Move MessageDialogContent out of MTM_Waitlist.Shared | Modernization | medium | APPROVED | Makes Shared view-free |
| MOD-007 | Break GenericRequest[.Data] to Waitlist.* references | Modernization | medium | APPROVED | Large; Phase 6 |
| MOD-008 | Decompose SettingsViewModel | Modernization | low | APPROVED | 2,623 lines; the largest refactor |
| MOD-009 | Replace the hand-written animation in RotatingTextView | Modernization | low | APPROVED | One animation idiom |
| SEC-001 | Remove committed credentials from appsettings.json | Security | high | APPROVED | Shape only in the repository; values per host; rotate what was committed |
| PERF-001 | Debounce the part search | Performance | high | APPROVED | Cuts plant round trips per typing burst |
| PERF-002 | Remove the three blocking waits | Performance | high | APPROVED | Two on the settings-load path, one at shutdown |
| UX-001 | Add the two missing resource keys | User experience | medium | APPROVED | The Setup review shows key names today |
| UX-002 | Extract XAML prose into resources | User experience | low | APPROVED | 140 attributes; mechanical; no visual change |
| UX-003 | Culture-correct coil weight formatting | User experience | low | APPROVED | Also removes a hardcoded unit |
| AI-001 | Correct the stale SDK version facts | AI tooling | high | APPROVED | Three agent-facing files cite 2.3.1 |
| AI-002 | Fix the dead python3 hook call | AI tooling | low | APPROVED | The bash arm silently no-ops here |
| AI-003 | Declare the documentation MCP servers | AI tooling | medium | APPROVED | Requires Node and two outbound HTTPS endpoints |
| AI-004 | Record the NuGet MCP failure | AI tooling | low | APPROVED | Prevents a repeated dead end |
| DEV-001 | Remove UseRidGraph after MOD-001 | Development workflow | medium | APPROVED | The gate is a cold restore with no NETSDK1206 |
| DEV-002 | Repair five broken documentation paths | Development workflow | medium | APPROVED | Includes the documented mock-service install command |
| DEV-003 | Replace the three-second test sleep | Development workflow | low | APPROVED | Inject a clock and assert the verdict |
| DEV-004 | Regenerate the stale comparison PDFs | Development workflow | low | APPROVED | Three sign-in comparisons predate their design |

Decision counts: APPROVED 32 · REJECTED 0 · DEFERRED 0 · MORE_RESEARCH_REQUIRED 0 · KEEP_CURRENT 0 ·
PENDING_USER_DECISION 0.

---

## 20. Decisions Required

None outstanding. All 32 proposals received a decision on 2026-11-26.

One operational prerequisite, which is not a proposal: before SEC-001 can be merged, whoever runs the plant
deployments must be able to set the two connection-string environment variables on each host. That step
belongs to operations, not to this repository.

Open questions recorded but blocking no approved work:

1. Whether the ~298 unreferenced resource keys are genuinely dead cannot be settled without a runtime sweep,
   because some keys are composed at run time.
2. Whether Windows App SDK 2.7.0 or DevWinUI 11.1.0 require code changes could not be determined without
   attempting the upgrade; both carry a manual review gate instead of a prediction.
3. The user-level VS Code configuration was not audited, because `--user-config` was not passed.

---

## 21. Research References

| Source | Used for | Date checked |
| --- | --- | --- |
| `dotnet list <project> package --outdated`, run per project | Every declared-versus-latest figure in sections 3 and 4 | 2026-11-26 |
| `dotnet list <project> package --vulnerable --include-transitive` | The no-vulnerability verdict for four projects | 2026-11-26 |
| `https://api.nuget.org/v3-flatcontainer/communitytoolkit.winui.ui.controls/index.json` | Confirms 7.1.2 exists on nuget.org and is the last release of that line | 2026-11-26 |
| `https://api.nuget.org/v3-flatcontainer/microsoft.extensions.hosting/11.0.0/microsoft.extensions.hosting.nuspec` | 11.0.0 carries a `net10.0` asset group | 2026-11-26 |
| `https://api.nuget.org/v3-flatcontainer/mstest.testframework/4.5.3/mstest.testframework.nuspec` | MSTest 4.5.3 target frameworks | 2026-11-26 |
| `https://api.nuget.org/v3-flatcontainer/microsoft.data.sqlclient/8.0.0/microsoft.data.sqlclient.nuspec` | SqlClient 8.0.0 requires `System.Security.Cryptography.Pkcs 10.0.15` | 2026-11-26 |
| `https://github.com/communitytoolkit/windows/wiki/Migration-Guide-from-v7-to-v8` (via Context7) | `CommunityToolkit.WinUI.UI.*` to `CommunityToolkit.WinUI.*`; per-control packages | 2026-11-26 |
| `https://github.com/communitytoolkit/windows/blob/main/components/Animations/samples/ConnectedAnimations.md` (via Context7) | `RegisterElementForConnectedAnimation` and `SetListDataItemForNextConnectedAnimation` exist in 8.x | 2026-11-26 |
| `obj/project.assets.json` in all 17 projects | Resolved versions and the Win2D 1.0.0.30 versus 1.4.0 split | 2026-11-26 |
| `%USERPROFILE%\.nuget\packages\microsoft.graphics.win2d\{1.0.0.30,1.4.0}\runtimes` | 1.0.0.30 ships `win10-*` RID assets; 1.4.0 ships portable `win-*` | 2026-11-26 |
| Local: `MTM_Waitlist.sln`, 17 `*.csproj`, `Directory.Build.props`, `NuGet.Config` (untracked), `appsettings.json` | Inventory, versions, supply-chain posture, credential surface | 2026-11-26 |
| Local: `git log --since="6 months ago" --name-only` | Churn hotspots across 382 commits | 2026-11-26 |
| Local: `Database/**`, `build_mtm_mock_masters.ps1`, `Service_ObjectCatalog.cs` | Schema-artifact coverage and master-list consistency | 2026-11-26 |
| Local: `MTM_Waitlist.Tests/**` | 2,768 methods, 363 classes, 17 guardrails | 2026-11-26 |
| Local: `Strings/**/*.resw` and every `*.xaml` | 1,079 keys, 159 `x:Uid`, 140 literal prose attributes | 2026-11-26 |
| Local: `.github/**`, `.mcp.json`, `.vscode/settings.json`, `.specify/**` | AI environment inventory and context cost | 2026-11-26 |
