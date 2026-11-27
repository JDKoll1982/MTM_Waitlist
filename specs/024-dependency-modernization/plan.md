# Implementation Plan: Dependency Currentness, Credential Removal and Platform Modernization

**Feature Directory**: `specs/024-dependency-modernization`
**Source**: `Dependency_Assessment/2026-11-26-full-deep/DEPENDENCY_AND_MODERNIZATION_SPECIFICATION.md`
**Plan date**: 2026-11-27
**Size**: oversized (recorded in `.spec-context.json`)

## Scale note

This change touches about 75 files across all 17 projects, 8 ordered phases and 32 approved proposals, and
five of those proposals reach into shared build configuration that every project inherits. Read the phase
table before any file list: a file named in one phase is often blocked by a gate in an earlier one, and
four places in the source document name a narrower change than the requirement it is supposed to satisfy.
Those four are called out where they occur and collected in `research.md`.

## Summary

Bring every dependency to its approved target release, remove the one committed credential, finish the
framework-native replacements, and make the AI and documentation records true, in the eight phases the
audit fixed, without changing plant behaviour. The plan keeps the same stack: WinUI 3 on Windows App SDK,
MSTest, MySqlConnector and Microsoft.Data.SqlClient, and the existing `MySqlHelperServer` seam. Two things
are new to the tree rather than replaced in it: `Directory.Packages.props` for central package management,
and a start-up check that refuses a host whose connection values are missing and names the missing
variable. Nothing in this plan adds a project, a test framework, a mocking mode or a CI workflow.

## Phase ordering

The order below is section 14 of the source, unchanged. Each phase names the gate that must be green before
it starts and the evidence that closes it, per FR-033 and FR-036. A phase does not open while an earlier
phase is not green, and the phase's own proposal order is the order inside it.

Waves group the changes that share one review and one verification run. There are 30 waves over 8 phases.
Most waves are one proposal; a wave carries two only where two adjacent text-only proposals need the same
review, which is Phase 1 waves 4 and 5.

| Phase | Waves | Proposals, in order | Objective |
|---|---|---|---|
| 1 | W1 to W5 | SEC-001, PERF-002, UX-001, AI-001, AI-002, DEV-002, AI-004 | Immediate correctness and hygiene |
| 2 | W6 to W8 | DEP-008, DEP-009, DEP-004 | Dependency infrastructure |
| 3 | W9 to W13 | DEP-002, DEP-006, DEP-005, DEP-001, DEP-007 | Runtime package upgrades |
| 4 | W14 to W15 | DEP-003, DEV-003 | Test stack and test quality |
| 5 | W16 to W22 | MOD-001, DEV-001, MOD-003, MOD-002, MOD-005, MOD-004, MOD-009 | Framework-native replacement |
| 6 | W23 to W25 | MOD-006, MOD-007, MOD-008 | Architectural modernization |
| 7 | W26 to W28 | PERF-001, UX-003, UX-002 | Performance and localization |
| 8 | W29 to W30 | AI-003, DEV-004 | AI environment and design records |

### Phase 1, immediate correctness and hygiene

W1 SEC-001, W2 PERF-002, W3 UX-001, W4 AI-001 and AI-002, W5 DEV-002 and AI-004.

**Prerequisite gate**: none beyond a green baseline. The operational prerequisite sits outside the
repository: whoever runs plant deployments can provision the two connection-string variables before W1 is
delivered. The repository work does not block on it, but the phase's delivery does.
**Completion evidence**: a search of every tracked file finds no credential value; a start on a host with
the variables unset stops and names the missing variable; the setup review shows two sentences; the three
agent-facing files state the platform version in use; `.github/hooks/speckit.json` invokes `python` in both
arms; six documentation paths resolve; the tooling note records the dependency-analysis failure. Build with
no errors and the full suite green, 2,768 methods, 17 guardrails.

### Phase 2, dependency infrastructure

W6 DEP-008, W7 DEP-009, W8 DEP-004.

**Prerequisite gate**: a green baseline build at 0 errors, with warnings reported rather than suppressed.
DEP-004 lands after DEP-008 so its three bumps are single lines.
**Completion evidence**: `Directory.Packages.props` holds one version per package and no project carries an
inline `Version` on a `PackageReference` (NU1008 clean); a cold restore from a cleared cache succeeds; the
tracked `NuGet.Config` carries `packageSourceMapping`; the three patch bumps resolve at their target
versions. Suite green and 17 guardrails green.

### Phase 3, runtime package upgrades

W9 DEP-002, W10 DEP-006, W11 DEP-005, W12 DEP-001, W13 DEP-007. Strictly sequential: each upgrade is
validated before the next starts.

**Prerequisite gate**: Phase 2 complete.
**Completion evidence**: each package at its target version; an internal-store read and write against the
local instance after W10; an Infor Visual read through the live path after W11; a full manual screen pass
after W12 and again after W13, at each display scale in use; the `System.Security.Cryptography.Pkcs` pin
gone from its new home in `Directory.Packages.props` after W11. Cold restore and suite green at each wave.

### Phase 4, test stack and test quality

W14 DEP-003, W15 DEV-003. DEP-003 first, because DEV-003 edits a test the new adapter must still run.

**Prerequisite gate**: Phases 1 to 3 green, so the suite measures settled code.
**Completion evidence**: 2,768 methods discovered and green under MSTest 4.5.3; no test waits on a real
clock for more than 500 ms; the replaced test fails when the behaviour it covers is broken deliberately.

### Phase 5, framework-native replacement

W16 MOD-001, W17 DEV-001, W18 MOD-003, W19 MOD-002, W20 MOD-005, W21 MOD-004, W22 MOD-009.

**Prerequisite gate**: Phase 3 complete. Inside the phase, DEV-001 needs MOD-001 so Win2D unifies first,
and MOD-002 follows MOD-003 so the seam is narrowed before it is adopted more widely.
**Completion evidence**: no 7.1.2 toolkit package remains and the waitlist-to-detail transition still
animates; a cold restore raises no NETSDK1206 with `UseRidGraph` gone; the seam executes stored procedures
only and its raw-SQL allowlist is empty of service sites; the five named services route through it; one
Infor Visual query type; JSON contexts generated per store with values round-tripping unchanged; one
animation idiom with visual parity. Manual screen pass. Suite and 17 guardrails green.

### Phase 6, architectural modernization

W23 MOD-006, W24 MOD-007, W25 MOD-008. Sequential; each is its own review cycle.

**Prerequisite gate**: Phase 5 complete, so data-access and toolkit churn is settled.
**Completion evidence**: `MTM_Waitlist.Shared` declares no `UserControl`, `Page` or `Window` type; the
generic-request logic and data libraries reference no waitlist UI project and the flow works end to end;
the settings logic is split behind the existing page with the settings surface unchanged and the existing
settings tests passing. Manual screen pass after W23 and after W25.

### Phase 7, performance and localization

W26 PERF-001, W27 UX-003, W28 UX-002.

**Prerequisite gate**: Phase 6 complete, so the XAML being edited is the final layout.
**Completion evidence**: a scripted ten-character part number produces a single-digit plant round-trip
count with the newest result shown; a coil weight renders with the machine's separators and a unit that
comes from a resource; no literal user-visible text attribute remains in a view definition and one
convention resolves every string. Locale run on a machine whose culture differs from the developer's.

### Phase 8, AI environment and design records

W29 AI-003, W30 DEV-004.

**Prerequisite gate**: Phase 7 complete, so the design records are generated from final screens.
**Completion evidence**: a fresh clone declares both documentation lookup servers in `.mcp.json` with their
runtime and network prerequisites recorded, and one answers a query; the three comparison documents are
newer than their design source.

## Project Structure

Every path below was read in this planning run.

```text
MTM_Waitlist.sln
Directory.Build.props                                  version 1.1.1, NoWarn CS0618, UseRidGraph, Pkcs pin
Directory.Packages.props                               NEW (W6): one version per package
NuGet.Config                                           tracked: sources only, mapping added (W7)
.mcp.json                                              tracked: empty server map, two servers added (W29)
appsettings.json                                       credential values replaced by empty (W1)
App.xaml.cs                                            shutdown flush (W2)

Strings/en-us/Resources.resw                           two dot-less keys added (W3)
Strings/en-us/TooltipResources.resw
Strings/en-us/TooltipResources.developer.resw

MTM_Waitlist.Core/
  Services/MySqlHelperServer.cs                        text path removed, row cap (W18)
  Services/InforVisualSqlQueryService.cs               the surviving Visual query owner (W20)
  Services/UserManagementRepository.cs                 } five services routed through the seam (W19)
  Services/PermissionAdministrationService.cs          }
  Services/LuggerArrangementService.cs                 }
  Helpers/ResourceExtensions.cs                        unchanged: the GetLocalized contract (W3, W28)
  Services/NavigationService.cs                        toolkit 8.x namespace (W16)

MTM_Waitlist.Shared/
  Dialogs/MessageDialogContent.xaml                    removed as a view type (W23)
  Dialogs/MessageDialogContent.xaml.cs                 body composed in code instead (W23)
  Dialogs/MessageDialogHost.cs                         unchanged signature, 22 call sites (W23)
  Dialogs/WindowedDialogHost.cs
  Dialogs/DialogSizing.cs                              the one sizing scale (W23)
  Services/WorkCenterCatalogService.cs                 routed through the seam (W19)

MTM_Waitlist.Settings/
  SettingsHub/ViewModels/SettingsViewModel.cs          blocking read removed (W2), then split (W25)
  Services/ImageStorageService.cs                      blocking read removed (W2)
  Services/IImageStorageService.cs                     share path becomes awaitable (W2)
  Services/InforVisualSqlQueryService.cs               deleted, Core's type registered (W20)
  Models/UserListFilter.cs                             JSON context (W21)

MTM_Waitlist.Setup/
  ViewModels/SetupReviewViewModel.cs                   unchanged, verified against the new keys (W3)
  Services/InforVisualSqlQueryService.cs               deleted (W20)

MTM_Waitlist.GenericRequest/
  MTM_Waitlist.GenericRequest.csproj                   waitlist project references removed (W24)
  Services/GenericRequestLiveStepData.cs               reads move to a shared contract (W24)
  Views/GenericRequestPartPage.xaml.cs                 debounced search (W26)

MTM_Waitlist.Waitlist.View/Services/AverageCoilWeightService.cs    culture and unit (W27)
Module_Shared/Controls/RotatingTextView.xaml.cs                    toolkit animation (W22)
Module_Waitlist/Views/WaitlistViewPage.xaml                        toolkit 8.x namespace (W16)
Module_Waitlist/Views/WaitlistViewDetailPage.xaml                  dead namespace removed (W16)
Module_Waitlist/Views/WaitlistViewDetailPage.xaml.cs               toolkit 8.x namespace (W16)

MTM_Waitlist.Startup/Services/                                     the start-up connection guard (W1)

.github/copilot-startup-steps.yaml                                 platform version (W4)
.github/hooks/speckit.json                                         python, not python3 (W4)
.github/memories/repo/github-artifact-staleness.md                 platform version (W4)
.github/memories/repo/mcp-tooling.md                               dependency-analysis failure (W5)

docs/operations/, docs/living-specs/, docs/defects/,
docs/startup/, docs/diagrams/tools/                                six documented paths (W5)
docs/ui-designs/startup-launch/sign-in/*/                          three comparison documents (W30)

MTM_Waitlist.Tests/
  Module_Startup/Services/VisualVerdictPrimingStepTests.cs          the three-second wait (W15)
  Module_Core/Views/FaultRecordAuditTests.cs                        asserted flush signature (W2)
  Module_Startup/Services/LocalStateFreeTests.cs                    asserted flush signature (W2)
  Module_Shared/Dialogs/DialogSizingAuditTests.cs                   asserted body path (W23)
  Module_Setup/Services/InforVisualSqlQueryServiceTests.cs          constructs the surviving type (W20)
```

**Structure Decision**: the eight phase directories are not a code layout, they are a review order. The
code stays where it is except in two places: `MessageDialogContent` stops being a XAML view type inside
`MTM_Waitlist.Shared`, and the two generic-request libraries stop referencing waitlist UI projects. Nothing
else moves between projects, and no project is added.

One structural finding changes the source's remedy for MOD-006. `MessageDialogHost.AskAsync` has 22
production call sites across the app **and** `MTM_Waitlist.Mock.Service/Views/ServiceSettingsPage.xaml.cs:63`,
so the dialog body cannot move into the app project: the app references the service, not the reverse. The
plan therefore composes the body in code inside `Shared` rather than moving the XAML into `Module_*`. See
`research.md` decision R-05.

## Constitution Check

Checked before Phase 0 research and re-checked after Phase 1 design.

| Principle | Verdict | Assessment |
|---|---|---|
| I. Spec-First, Verified Delivery | PASS | The spec exists and this plan precedes any code. Tasks follow, and no task is ticked without a build and a verification run. Every phase closes on measured evidence, not on a prediction. |
| II. Live Data Integrity | PASS | No demo or mock mode is introduced, and MOD-002 and MOD-003 tighten the path rather than widen it. The Infor Visual fallback rule and its reachability verdict are untouched. |
| III. Stored-Procedure-First Discipline | PASS, strengthened | MOD-003 removes the `CommandType.Text` entry points, so the seam's rule becomes true. No schema, seed or procedure artifact is changed by this feature, so no database authoring is required. |
| IV. MCP-First, Grounded Decisions | PASS | Two decisions were grounded in Microsoft Learn rather than memory: NU1008 for the central-package-management collision with the Pkcs pin (`learn.microsoft.com/nuget/reference/errors-and-warnings/nu1008`), and NETSDK1206 for the `UseRidGraph` gate (`learn.microsoft.com/dotnet/core/tools/sdk-errors/netsdk1206`). Package versions come from the source audit, which cites nuget.org manifests checked 2026-11-26. AI-003 is what makes the same lookup available to every future clone. |
| V. WinUI 3 Platform Conformance | PASS | UI work stays on `Microsoft.UI.Xaml` and the existing MVVM and navigation patterns. String extraction (UX-002) keeps the `x:Uid` plus `GetLocalized` convention rather than inventing a second one, and no converter or resource dictionary is added. DEP-001 and DEP-007 carry the manual screen gate that this principle's failures show up in. |
| VI. Evidence-Based Verification Gates | PASS | Every gate uses the documented commands with `/m:1 /nodeReuse:false`. `WMC9999` is treated as a real masked XAML error. Connectivity gates are environment-gated and recorded as such, which the principle allows. |
| VII. Readable, End-User-Facing Delivery | PASS | Progress is reported as what changed and what is open. The operator prerequisites are the last thing in each report, naming the environment variables without printing any value. |

No violation is taken from the plan's own work. Complexity Tracking is therefore omitted.

Three governance points are recorded rather than waived:

1. **Two guardrail assertions change.** `FaultRecordAuditTests.cs:52` asserts the literal `TryFlushLogStore()`
   and `LocalStateFreeTests.cs:182` asserts `private static void TryFlushLogStore`. Making the flush
   awaitable changes that signature. The repository's rule is that a guardrail that trips is a design
   decision, not a test to edit, so the decision is recorded here: the flush keeps its name, its bounded
   off-thread behaviour and its best-effort failure handling, and only its signature becomes awaitable. The
   two assertions are updated in W2 as part of PERF-002.
2. **One connectivity gate cannot run on this machine.** The plant host is not reachable during this work,
   so W10 and W11 prove a local-instance round trip and record the live-path read against the plant as
   environment-gated evidence, naming what was run and what was not. This is the principle's own allowance,
   not a substitute for it.
3. **One Security & Secrets deviation is taken, at the owner's explicit instruction (put to the owner and
   answered on 2026-11-27).** SEC-001 blanks every credential in `appsettings.json`, and start-up refuses
   when the environment variable that replaces one is missing. Four values nevertheless stay committed in
   two tracked files: `.github/memories/repo/workstation-secrets.md` carries the shared development MySQL
   connection string, `root`, and the Infor Visual pair, and
   `MTM_Database_Deployment/deployment.settings.json` names `root` beside the plant address. The owner was
   shown the choice between removing them and recording the exception, and chose the exception. It is
   therefore scoped, dated and bounded:

   - **Scope:** the pre-existing development credentials of `172.16.1.104` / `V-MTMFG-5` only — `root`/`root`
     for the four MySQL stores and `SHOP2`/`SHOP` for Infor Visual. It does not extend to production values,
     to any other host, or to any new credential.
   - **Reason:** the mock service's User-scope variables are derived from that note, and the note is the
     durable, in-repo record of an approval the owner granted on 2026-09-11. Deleting the values would delete
     the evidence of the approval without making the credentials unknown.
   - **Compensating controls:** the application reads no committed credential any more; the note forbids
     carrying the pattern into code, tests, other documentation or another host, and forbids routing the
     values through a prompt, command line or log; and the disclosed values must still be rotated on the
     plant host before this ships, which remains an operational prerequisite (T025).
   - **What ends it:** rotation on the host, or the moment production credentials replace these development
     ones — at which point this exception does not apply and the note must be re-decided.

   This is a deviation from the constitution's Security & Secrets rule, recorded as the constitution requires
   rather than silently waived. The remaining exposure is tracked as `T122` in `tasks.md`.

## Call paths

Three behaviours cross functions and files. Line numbers are from this run.

```calls start-up connection refusal (SEC-001)
  ConfigurationStep.RunAsync @ MTM_Waitlist.Startup/Services/ConfigurationStep.cs:48
~   ReadSavedSettings() @ MTM_Waitlist.Startup/Services/ConfigurationStep.cs:59
+   ConfigurationRequirement **new** @ MTM_Waitlist.Startup/Services/ConfigurationRequirement.cs
~   StoreReachabilityStep.RunAsync @ MTM_Waitlist.Startup/Services/StoreReachabilityStep.cs:60
~   ExecuteStoredProcedureQueryAsync @ MTM_Waitlist.Core/Services/MySqlHelperServer.cs:65
~   ExecuteWithBoundedRetryAsync @ MTM_Waitlist.Core/Services/MySqlHelperServer.cs:163
```
note: the blank-connection branch at line 163 returns a neutral result, so the reachability step reports "the store answered" on nothing. W1 closes that hole.

```calls narrowing and adopting the data seam (MOD-003 then MOD-002)
  ExecuteStoredProcedureQueryAsync @ MTM_Waitlist.Core/Services/MySqlHelperServer.cs:65
~   ReadRowsAsync @ MTM_Waitlist.Core/Services/MySqlHelperServer.cs:286
-   ExecuteSqlQueryAsync @ MTM_Waitlist.Core/Services/MySqlHelperServer.cs:107
-   ExecuteSqlNonQueryAsync @ MTM_Waitlist.Core/Services/MySqlHelperServer.cs:128
-   CreateTextCommand @ MTM_Waitlist.Core/Services/MySqlHelperServer.cs:262
~   UserManagementRepository @ MTM_Waitlist.Core/Services/UserManagementRepository.cs
~   PermissionAdministrationService @ MTM_Waitlist.Core/Services/PermissionAdministrationService.cs
~   LuggerArrangementService @ MTM_Waitlist.Core/Services/LuggerArrangementService.cs
~   WorkCenterCatalogService @ MTM_Waitlist.Shared/Services/WorkCenterCatalogService.cs
~   ConnectionHealthService @ MTM_Waitlist.Core/Services/ConnectionHealthService.cs
```
note: `ReadRowsAsync` gains the row cap both paths share, and the raw-SQL allowlist that `InlineSqlAuditTests` asserts must shrink as each service moves.

```calls removing three blocking waits (PERF-002)
  TryFlushLogStore @ App.xaml.cs:580
~   FlushAsync @ App.xaml.cs:591
~   GetConfiguredSharePath @ MTM_Waitlist.Settings/Services/ImageStorageService.cs:403
~   GetConfiguredSharePath @ MTM_Waitlist.Settings/Services/IImageStorageService.cs:103
~   GetConfiguredSharePath @ MTM_Waitlist.Settings/Pictures/ViewModels/ImageOverrideDialogViewModel.cs:260
~   GetConfiguredSharePath @ MTM_Waitlist.Settings/Services/ImageStorageService.cs:424
~   NewRequestAlertsEnabled @ MTM_Waitlist.Settings/SettingsHub/ViewModels/SettingsViewModel.cs:1378
```
note: the settings class already exposes `HotWorkCentersLoad` and `BuildingsLoad` as awaitable task properties at lines 1395 and 1396; the third wait follows that existing idiom rather than inventing one.

## Code pin

One line needs a note, because the obvious rewrite of it destroys what it is for.

```csharp App.xaml.cs:589-591 hl=591
            // Off the caller's thread so this cannot deadlock on the interface thread's context, and bounded so a
            // store that will not answer does not hold the process open.
            Task.Run(() => log.FlushAsync(CancellationToken.None)).Wait(TimeSpan.FromSeconds(2));
```
pin 591: the bound and the off-thread hand-off both have to survive, so this becomes an awaited bounded flush with a cancellation source, not a bare `await`.

## States

The part search gains a lifecycle. Nothing else in this feature adds state: the launch, the dialog and the
settings surface keep the states they have.

```states one part search, from keystroke to shown result
Idle: Nothing typed, or fewer than two characters. (start)
Pending: Typing has paused; one plant read is on its way. (proposed)
Superseded: A newer search started; this answer is no longer wanted. (proposed)
Abandoned: Typing resumed past the pause; this answer is cancelled. (proposed)
Shown: The newest result is on screen.
Failed: The read did not answer; the frame states the attempt time. (proposed)
Idle -> Pending: pause
Pending -> Superseded: newer search
Pending -> Abandoned: typing resumes
Pending -> Shown: newest answer accepted
Pending -> Failed: read fails
Superseded -> Shown: older answer discarded
Abandoned -> Pending: next pause
Shown -> Pending: next pause
Failed -> Pending: retry
grid:
Idle  | Pending | Superseded | Abandoned
Shown | Failed  | .          | .
```
note: cancelled work never reaches the interface, and an answer whose sequence number is older than the newest one is discarded rather than shown.

## Screens

One screen changes. UX-002 changes no screen, it changes where the words come from.

```screen review Setup review
title: Setup review
row:
  text: the section header, now the count as words from a resource (changed) (1)
  field: Scrap type (unchanged)
  text: the scrap value, now from a resource (changed) (2)
  button: Back (unchanged)
  button: Save setup (unchanged)
```
1: **The header.** The count reads as a sentence with the number in it, from a resource key rather than a key name.
2: **The scrap value.** With no scrap recorded, the line reads words instead of the key name it shows today. Both places fail for the same reason: the values in the resource file carry a `.Text` suffix that a code-side lookup cannot reach.

No layout, control or interaction change is in scope, so no other screen block is drawn.

## File-level change traceability

Every row is a change from section 7 of the source, with the owner that authorises it, the phase and wave
that carries it, and the current state of the file as read in this run.

| File | Owner | Phase / wave | Change | Verified today |
|---|---|---|---|---|
| `appsettings.json` | SEC-001 | 1 / W1 | Every credential value replaced by an empty string; `ConnectionStringEnvironmentVariable` kept as the documented path | Two `ConnectionString` values and the Visual server, user and password carry real values |
| start-up connection guard (`MTM_Waitlist.Startup/Services/`) | SEC-001 | 1 / W1 | Refuse a host with no connection value and name the missing variable | Not present; a blank connection is silently tolerated |
| `MTM_Waitlist.Settings/SettingsHub/ViewModels/SettingsViewModel.cs:1378` | PERF-002 | 1 / W2 | Await the read on the initialising path | `GetAwaiter().GetResult()` in the constructor |
| `MTM_Waitlist.Settings/Services/ImageStorageService.cs:403` | PERF-002 | 1 / W2 | Await, and propagate through `IImageStorageService` | Synchronous interface method, two production call sites |
| `App.xaml.cs:591` | PERF-002 | 1 / W2 | Bounded, awaited, off-thread flush | `Task.Run(...).Wait(TimeSpan.FromSeconds(2))` |
| `Strings/en-us/Resources.resw` | UX-001 | 1 / W3 | Add `Setup_Review_Scrap_None` and `Setup_Review_Section_SubordinateParts_Count` | Only the `.Text` twins exist, at lines 301 and 316 |
| `MTM_Waitlist.Setup/ViewModels/SetupReviewViewModel.cs:95,109` | UX-001 | 1 / W3 | No code change expected; verify both resolve | As described |
| `.github/copilot-startup-steps.yaml:21` | AI-001 | 1 / W4 | Platform version stated in use, re-verified on each bump | States 2.3.1 |
| `Directory.Build.props:47` | AI-001 | 1 / W4 | Restate against the version in use, or state the rule with none | Cites "the 2.3.1 in use" |
| `.github/memories/repo/github-artifact-staleness.md:37,56` | AI-001 | 1 / W4 | Correct the figure and date it as re-measured | States 2.3.1 at line 37, 2.3.0 and 2.3.1 at lines 55 and 56 |
| `.github/hooks/speckit.json` | AI-002 | 1 / W4 | `python` in the bash arm, matching the powershell arm | Two hooks; bash arms call `python3` |
| `.github/memories/repo/mcp-tooling.md` | AI-004 | 1 / W5 | Record the failure, its signature and the working command | Does not record it |
| `docs/operations/operations-mock-service-install-script.md:79,103-112` | DEV-002 | 1 / W5 | Correct path to `MTM_Waitlist.Mock.Service/deploy/install-mock-service.ps1` | File and script both exist |
| `docs/operations/operations-database-live-and-mock-updates.md:211` | DEV-002 | 1 / W5 | Same correction | Exists |
| `docs/living-specs/living-specs-drift-ledger.md:118` | DEV-002 | 1 / W5 | Cite the note that owns the statement, or drop it | Exists |
| `docs/defects/defects-coil-partial-high-average-weight-is-resolved-for-one-hardcoded-part.md:77` | DEV-002 | 1 / W5 | Cite the current query under `Database/InforVisual/Queues/` | Both exist |
| `docs/startup/startup-rebuild-design-brief.md:229` | DEV-002 | 1 / W5 | Mark historical with the removal date | Exists |
| `docs/diagrams/tools/README.md:71` | DEV-002 | 1 / W5 | Fix or remove the link | Exists |
| `Directory.Packages.props` | DEP-008 | 2 / W6 | New: one `PackageVersion` per package | Absent |
| All 17 `*.csproj` | DEP-008 | 2 / W6 | Remove each inline `Version` | 81 declarations across 22 packages |
| `Directory.Build.props:77` | DEP-008 | 2 / W6 | Relocate the Pkcs pin as a `PackageVersion`, strip its `Version` | `PackageReference Version="9.0.21"` in an ItemGroup |
| `NuGet.Config` | DEP-009 | 2 / W7 | Add `packageSourceMapping` over `nuget.org` | Tracked; sources only, no mapping |
| `MTM_Waitlist.csproj`, `MTM_Waitlist.Core`, `MTM_Waitlist.Mock.Service`, `MTM_Database_Deployment` | DEP-004 | 2 / W8 | WinUIEx 2.9.3, BuildTools 10.0.28000.2957, ProtectedData 11.0.0 | Declared at 2.9.2, 10.0.28000.2270, 10.0.0 |
| nine projects declaring Microsoft.Extensions 10.0.10 | DEP-002 | 3 / W9 | 11.0.0 | 10.0.10 |
| `MTM_Waitlist.csproj`, `MTM_Waitlist.Core`, `MTM_Waitlist.Mock`, `MTM_Waitlist.Mock.Service`, `MTM_Waitlist.Startup`, `MTM_Database_Deployment.Core` | DEP-006 | 3 / W10 | MySqlConnector 3.0.0 | 2.6.1 |
| `MTM_Waitlist.csproj`, `MTM_Waitlist.Core`, `MTM_Waitlist.Mock`, `MTM_Waitlist.Mock.Service`, `MTM_Waitlist.Setup` | DEP-005 | 3 / W11 | SqlClient 8.0.0; retire the Pkcs pin | 6.1.1 |
| all 17 projects declaring Windows App SDK | DEP-001 | 3 / W12 | 2.7.0 | 2.4.0 |
| `MTM_Waitlist.csproj`, `MTM_Waitlist.Shared`, `MTM_Database_Deployment` | DEP-007 | 3 / W13 | DevWinUI 11.1.0 | 10.8.0 |
| `MTM_Waitlist.Tests` | DEP-003 | 4 / W14 | MSTest 4.5.3, Test SDK 18.12.0, coverlet 10.1.1 | 3.7.0, 17.13.0, 6.0.2 |
| `MTM_Waitlist.Tests/Module_Startup/Services/VisualVerdictPrimingStepTests.cs:209` | DEV-003 | 4 / W15 | Injected clock or bound; assert the verdict | `Task.Delay(3s)` in the step that is deliberately unstoppable |
| `MTM_Waitlist.csproj`, `MTM_Waitlist.Core` toolkit references plus four source files | MOD-001 | 5 / W16 | `CommunityToolkit.WinUI.Animations` 8.2.251219; drop the unused Controls reference | `WaitlistViewPage.xaml:6-7`, `WaitlistViewDetailPage.xaml:5`, `WaitlistViewDetailPage.xaml.cs:1`, `NavigationService.cs:3` |
| `Directory.Build.props:56` | DEV-001 | 5 / W17 | Remove `UseRidGraph` after a cold restore proves no NETSDK1206 | Present, with a long rationale |
| `MTM_Waitlist.Core/Services/MySqlHelperServer.cs:107,128,258` | MOD-003 | 5 / W18 | Remove the text path; bound the read | Two public text entry points and `CreateTextCommand` |
| five services | MOD-002 | 5 / W19 | Route through the seam | `UserManagementRepository:259,377`, `PermissionAdministrationService:477`, `LuggerArrangementService:403`, `WorkCenterCatalogService:295`, `ConnectionHealthService:108` |
| `MTM_Waitlist.Setup/Services/InforVisualSqlQueryService.cs` | MOD-005 | 5 / W20 | Delete; register Core's type | Two independent implementations, both `IConfiguration`-based |
| about 22 files across settings, mock service and mock | MOD-004 | 5 / W21 | Generated per-type metadata, one options definition per store | Reflection-based, options built per store |
| `Module_Shared/Controls/RotatingTextView.xaml.cs:138-195` | MOD-009 | 5 / W22 | Toolkit animation idiom | Hand-built `Storyboard` and `DoubleAnimation` |
| `MTM_Waitlist.Shared/Dialogs/MessageDialogContent.xaml{,.cs}` | MOD-006 | 6 / W23 | No view type in the shared library; body composed in code | The only view type in `Shared`; one production caller |
| `MTM_Waitlist.GenericRequest`, `MTM_Waitlist.GenericRequest.Data` | MOD-007 | 6 / W24 | No waitlist UI project reference; contracts extracted | `Waitlist.View`, `Waitlist.NewRequest`, `Settings`, `Mock` references |
| `MTM_Waitlist.Settings/SettingsHub/ViewModels/SettingsViewModel.cs` | MOD-008 | 6 / W25 | Split into cohesive parts behind the page | 3,023 lines, 161 public members today |
| `MTM_Waitlist.GenericRequest/Views/GenericRequestPartPage.xaml.cs:31-45` | PERF-001 | 7 / W26 | Debounce, cancel the superseded search, last writer wins | One `SearchAsync` per `TextChanged` past two characters |
| `MTM_Waitlist.Waitlist.View/Services/AverageCoilWeightService.cs:56` | UX-003 | 7 / W27 | Current-culture formatting; unit from a resource | `ToString("N0", InvariantCulture)` plus a literal unit |
| literal prose in view definitions | UX-002 | 7 / W28 | Move to the resource file, one convention | Present across `Module_Core`, `Module_Settings`, `Module_Waitlist`, `Module_Shared` |
| `.mcp.json` | AI-003 | 8 / W29 | Declare `microsoft-learn` and `context7` with prerequisites | `{"mcpServers": {}}` |
| three `*-comparison.pdf` files | DEV-004 | 8 / W30 | Regenerate from the current design source | All three dated 2026-11-25 05:27; their design JSON dated 05:50 |

Two files outside section 7 are touched, and both are owned: the guardrail assertions named under
Constitution Check, which PERF-002 and MOD-006 change to follow their own change, and
`MTM_Waitlist.Tests/Module_Setup/Services/InforVisualSqlQueryServiceTests.cs`, which MOD-005 must retarget
at the surviving type because it constructs the deleted one at line 115.

## Verification notes

Run every gate with the documented commands and the build parameters the constitution fixes.

```text
dotnet restore MTM_Waitlist.sln --force
dotnet build   MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false
dotnet test    MTM_Waitlist.Tests\MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64
```

Four quirks that will otherwise be misread as this feature's own regressions:

- `WMC9999` masks the real XAML error. Treat every occurrence as a genuine error and surface the underlying
  message before closing any wave. Phase 3 W12 and W13 and Phases 5 to 7 are where it is most likely.
- `PRI175` and `PRI224` mean stale PRI artifacts or a running instance of the application. Close the app and
  re-run the build before treating either as a defect.
- `[RelayCommand]` strips a trailing `Async` from the generated command name. A test that names the method
  rather than the command will look for a name that does not exist.
- Never add an explicit `<Page>` item. Views are included by the default globs, and an explicit item raises
  `NETSDK1022`. This matters for W23, where a XAML file is removed rather than added.

## Baselines

The spec pins baseline counts. Where a pinned number disagrees with the tree today, the number is the
audit's measurement on 2026-11-26 and the gate re-measures it rather than adopting it. This matters because
FR-036 requires the phase evidence to be a measurement, and a stale expected value would make a correct
change look wrong.

| Baseline | Pinned | Measured in this run | Note |
|---|---|---|---|
| Projects | 17 | 17 | matches |
| Approved proposals | 32 | 32 | matches |
| Test methods | 2,768 | 2,768 | matches exactly |
| Guardrail tests | 17 | 17 | matches |
| Resource keys | 1,079 across three files | 1,357 across five files | `Resources.resw` 923, Tooltip pair 78 each, generic request 109, mock service 169 |
| Literal prose attributes | 140 | not recounted | W28 measures before it edits |
| Missing resource keys | 2 | 2, but present under a `.Text` suffix | see research decision R-03 |
| Files stating a superseded platform version | 3 | 3 | confirmed by reading all three |
| Broken documentation references | 5 | 6 files to open, both targets exist | the sixth is the README link |
| Stale comparison documents | 3 | 3 | each older than its design JSON by about 23 minutes |

## Out of scope, confirmed by reading

`MTM_Waitlist.Mock.Service/App.xaml.cs:434,438` also carries a `Wait(` and a `GetAwaiter().GetResult()` on
its shutdown path. PERF-002 names three sites on the application's own settings-load and shutdown path.
The on-host service's pair is not owned by any approved proposal, so it stays, and FR-035 keeps it out of
this feature. It is recorded here so the next audit finds it rather than rediscovers it.

## Output

`plan.md` plus `research.md` and `contracts/`. No `data-model.md`: the spec's Key Entities section is None,
and this feature introduces no stored data, record or format. No prior spec in this repository wrote one
without an entity to record.
