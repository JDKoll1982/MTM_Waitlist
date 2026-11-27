# Tasks: Dependency Currentness, Credential Removal and Platform Modernization

**Feature directory**: `specs/024-dependency-modernization`
**Plan**: `plan.md` — 8 phases, 30 waves, one prerequisite gate and one evidence set per phase
**Spec**: `spec.md` — FR-001 to FR-036
**Size**: oversized (`.spec-context.json` → `size`)

## Scale note

This list carries 32 approved proposals over about 75 files in all 17 projects, and five proposals reach
shared build configuration that every project inherits. Read a phase gate before the tasks above it: a wave in
a later phase is often blocked by measured evidence in an earlier one, and four waves change a shared file
rather than the behaviour that depends on it.

## How to read this list

- **A task** is one closable unit: `- [ ] **T###** [P?] [US#] [PROPOSAL] description · exact/file/path`.
  `[US#]` traces to a user story in `spec.md`; `[PROPOSAL]` is the approved register entry that authorises the
  change. No task exists outside the 32 proposals plus FR-033 to FR-036.
- **`[P]`** marks a task independent of the `[P]` tasks beside it in the same step: a different file, no
  incomplete dependency, buildable in any order. A task without `[P]` waits for the task above it.
- **`⟶ then:`** is a join inside a wave. A wave holding a join is a sequential chain whose files depend on each
  other; it still closes as one unit.
- **A wave** groups the tasks sharing one review and one verification run and is the unit that is closed:
  `**Wave W1 — SEC-001 — closes on T001, T006-T008**`. Never close two waves in one call.
- **`### Tests`** comes first in each phase and holds the tasks that fail before the change. A test task rides
  in the wave named beside it, so every wave's close set lists its own tests.
- **A phase gate** is its own closable block at the end of each phase, never a wave, and is the only thing that
  opens the next phase.
- **`⛔ blocked-here:`** marks a task this workstation cannot verify. Implement reports it blocked with the
  stated reason and leaves the box unchecked. All of them are collected in the register at the end.
- **`(no file change)`** marks a task whose instrument is a read, a grep or a command against existing files.

The eight phases are FR-033's own table in FR-033's order, and the wave numbering is `plan.md`'s. User-story
tags carry the per-story traceability without reordering the phases, because the order is a requirement and
these phases are not independently deliverable slices.

## Shared files, sequenced ownership

Six files are edited by more than one phase. Each phase is strictly sequential, so no two waves ever edit one
of these at the same time, and each wave owns a disjoint region, key set or version line:

| File | Waves, in order | What each owns |
|---|---|---|
| `Directory.Packages.props` | W6, W8-W13, W14, W16 | W6 creates it with every version; each later wave changes one or more `PackageVersion` lines |
| `Directory.Build.props` | W4, W6, W17 | line 47 the platform-version comment; line 77 the `Pkcs` pin relocation; line 56 `UseRidGraph` |
| `Strings/en-us/Resources.resw` | W3, W27, W28 | W3 two dot-less keys; W27 the weight unit; W28 the extracted literals |
| `MTM_Waitlist.Settings/SettingsHub/ViewModels/SettingsViewModel.cs` | W2, W25 | W2 line 1378 only; W25 the split |
| `.github/copilot-startup-steps.yaml` | W4, W12 | W4 states the version in use; W12 re-verifies it against 2.7.0 |
| `.github/memories/repo/github-artifact-staleness.md` | W4, W12 | W4 corrects the figure; W12 re-verifies it against 2.7.0 |

Every other path is owned by exactly one phase. A gate or audit task sometimes names one of these files, or a
file owned by another phase, as a read - a phase gate asserting no CI was added reads `Directory.Build.props`,
it does not edit it - so only the waves above claim ownership. These six are ordered by FR-033, not parallel.

## Phase ordering constraints

Encoded as task dependencies, not left to reading order:

1. **Central package management before every version bump.** `T028` creates `Directory.Packages.props`; every
   later version edit (`T032`, `T038`, `T040`, `T043`, `T046`, `T049`, `T054`, `T060`) edits that file and so
   inherits from `T028`. Removing an inline `Version` (`T029`) is impossible before it exists, because NuGet
   raises NU1008 the moment both are present.
2. **DEP-005 after the `Pkcs` pin relocation.** `T043` retires the pin from its new home and needs `T030`,
   which moved it there in Phase 2. Retiring it before the relocation restores NU1902 and NU1903 on every
   restore.
3. **DEV-001 after MOD-001.** `T065` runs only after `T060` to `T063`, because the unified portable Win2D is
   what lets the cold restore be clean without the accommodation.
4. **MOD-002 after MOD-003.** `T073` to `T077` adopt a seam already narrowed by `T068` to `T072`, so the five
   services move onto the interface that survives.
5. **DEP-003 before DEV-003.** `T055` edits a test that the adapter bumped in `T054` must still run.

## Verification commands

Every gate runs these, with nothing added and no CI introduced:

```text
dotnet restore MTM_Waitlist.sln --force
dotnet build   MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false
dotnet test    MTM_Waitlist.Tests\MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64
```

A cold restore is that restore after `dotnet nuget locals all --clear`. `WMC9999` is a real masked XAML error,
never a warning to pass. The 17 guardrail tests are the `*AuditTests` files under `MTM_Waitlist.Tests`.

---

## Phase 1 — Immediate correctness and hygiene (W1-W5)

SEC-001, PERF-002, UX-001, AI-001, AI-002, DEV-002, AI-004.

Files: `appsettings.json`; `MTM_Waitlist.Startup/Services/ConfigurationRequirement.cs` (new),
`ConfigurationStep.cs`, `StoreReachabilityStep.cs`; `App.xaml.cs`;
`MTM_Waitlist.Settings/Services/IImageStorageService.cs`, `ImageStorageService.cs`,
`MTM_Waitlist.Settings/Pictures/ViewModels/ImageOverrideDialogViewModel.cs`,
`MTM_Waitlist.Settings/SettingsHub/ViewModels/SettingsViewModel.cs`; `Strings/en-us/Resources.resw`;
`.github/copilot-startup-steps.yaml`; `Directory.Build.props`;
`.github/memories/repo/github-artifact-staleness.md`, `.github/memories/repo/mcp-tooling.md`;
`.github/hooks/speckit.json`; six `docs/` files.

**Prerequisite gate**: a green baseline. The operational prerequisite — two connection-string variables
provisioned and the disclosed values rotated — sits outside the repository and is task `T025`.

### Tests

Written first, to fail, one per requirement a test can see. Where a proposal changes a declaration or a
document, no test can see it and the task says so.

- [x] **T001** [P] [US1] [SEC-001] Test: a host with no value for an internal store refuses to start and names the missing variable, never falling back to an empty string · `MTM_Waitlist.Tests/Module_Startup/Services/ConfigurationRequirementTests.cs` — rides in W1
- [x] **T002** [P] [US3] [PERF-002] Rewrite the two flush assertions to the awaited bounded form and let them fail until it lands; a guardrail that trips here is the recorded design decision, not a test to delete · `MTM_Waitlist.Tests/Module_Core/Views/FaultRecordAuditTests.cs:52`, `MTM_Waitlist.Tests/Module_Startup/Services/LocalStateFreeTests.cs:182` — rides in W2
- [x] **T003** [P] [US6] [UX-001] Test: both keys resolve through `GetLocalized` to a non-blank sentence and neither returns its own name · `MTM_Waitlist.Tests/Module_Setup/ViewModels/SetupReviewResourceKeyTests.cs` — rides in W3
- [x] **T004** [P] [US7] [AI-001, AI-002] Verify, `(no file change)`: each of the three agent-facing files states the platform version in use or carries none, and both arms of both hooks name `python` · `.github/copilot-startup-steps.yaml`, `Directory.Build.props:47`, `.github/memories/repo/github-artifact-staleness.md`, `.github/hooks/speckit.json` — rides in W4
- [x] **T005** [P] [US8] [DEV-002, AI-004] Verify, `(no file change)`: all six cited paths resolve to an artifact that exists, and the tooling note records the failure, its signature and the working command · the six `docs/` targets, `.github/memories/repo/mcp-tooling.md` — rides in W5

### Implementation

**Wave W1 — SEC-001 — closes on T001, T006-T008:**
- [x] **T006** [P] [US1] [SEC-001] Replace every credential value with an empty string and keep the shape: both `ConnectionString` values plus the Visual `Server`, `User` and `Password`; `ConnectionStringEnvironmentVariable` stays as the single documented path · `appsettings.json`
- [x] **T007** [P] [US1] [SEC-001] New start-up requirement that names the missing variable and refuses: no empty string, no placeholder, and no throw added to the seam, whose unconfigured-store contract is unchanged · `MTM_Waitlist.Startup/Services/ConfigurationRequirement.cs`
- **⟶ then:**
- [x] **T008** [US1] [SEC-001] Run the requirement before the store read, so a blank connection can no longer report that the store answered · `MTM_Waitlist.Startup/Services/ConfigurationStep.cs:59`, `MTM_Waitlist.Startup/Services/StoreReachabilityStep.cs:60`

**⟶ Wait for Wave W1 to finish, then:**

**Wave W2 — PERF-002 — closes on T002, T009-T011.** Three independent waits, three files.
- [x] **T009** [P] [US3] [PERF-002] Replace the blocked wait with an awaited bounded off-thread flush, keeping both the two-second bound and the `Task.Run` hand-off that stops a deadlock on the interface thread's context · `App.xaml.cs:580-591`
- [x] **T010** [P] [US3] [PERF-002] Make the share-path read awaitable through the interface and its implementation and follow it at both production call sites · `MTM_Waitlist.Settings/Services/IImageStorageService.cs:103`, `MTM_Waitlist.Settings/Services/ImageStorageService.cs:403,424`, `MTM_Waitlist.Settings/Pictures/ViewModels/ImageOverrideDialogViewModel.cs:260`
- [x] **T011** [P] [US3] [PERF-002] Await the read on the initialising path, following the `HotWorkCentersLoad` and `BuildingsLoad` task-property idiom already in the class · `MTM_Waitlist.Settings/SettingsHub/ViewModels/SettingsViewModel.cs:1378`

**⟶ Wait for Wave W2 to finish, then:**

**Wave W3 — UX-001 — closes on T003, T012-T013:**
- [x] **T012** [US6] [UX-001] Add `Setup_Review_Scrap_None` and `Setup_Review_Section_SubordinateParts_Count` without a suffix, wording matching the `.Text` twins, neither blank nor a placeholder; the twins stay · `Strings/en-us/Resources.resw`
- **⟶ then:**
- [x] **T013** [US6] [UX-001] Confirm both call sites resolve against the new keys with no code change; if one does not, the key is wrong, not the call · `MTM_Waitlist.Setup/ViewModels/SetupReviewViewModel.cs:95,109` `(no file change expected)`

**⟶ Wait for Wave W3 to finish, then:**

**Wave W4 — AI-001 and AI-002 — closes on T004, T014-T017.** Four files, no shared module.
- [x] **T014** [P] [US7] [AI-001] State the platform version in use and mark it re-verified on each bump · `.github/copilot-startup-steps.yaml:21`
- [x] **T015** [P] [US7] [AI-001] Correct the comment to the version in use, or state the rule with no version at all · `Directory.Build.props:47`
- [x] **T016** [P] [US7] [AI-001] Correct the figure and date it as re-measured · `.github/memories/repo/github-artifact-staleness.md:37,55-56`
- [x] **T017** [P] [US7] [AI-002] Change both bash arms to the interpreter that exists here, matching the powershell arms: `python`, never `python3` · `.github/hooks/speckit.json`

**⟶ Wait for Wave W4 to finish, then:**

**Wave W5 — DEV-002 and AI-004 — closes on T005, T018-T024.** Seven files, no file read by another.
- [x] **T018** [P] [US8] [DEV-002] Correct the install-script path to `MTM_Waitlist.Mock.Service/deploy/install-mock-service.ps1`, which exists · `docs/operations/operations-mock-service-install-script.md:79,103-112`
- [x] **T019** [P] [US8] [DEV-002] Apply the same correction so the two runbooks name one path · `docs/operations/operations-database-live-and-mock-updates.md:211`
- [x] **T020** [P] [US8] [DEV-002] Cite the note that owns the statement, or drop it · `docs/living-specs/living-specs-drift-ledger.md:118`
- [x] **T021** [P] [US8] [DEV-002] Cite the current query under `Database/InforVisual/Queues/` · `docs/defects/defects-coil-partial-high-average-weight-is-resolved-for-one-hardcoded-part.md:77`
- [x] **T022** [P] [US8] [DEV-002] Mark the statement historical with the date it stopped being true · `docs/startup/startup-rebuild-design-brief.md:229`
- [x] **T023** [P] [US8] [DEV-002] Fix or remove the link to the tool note · `docs/diagrams/tools/README.md:71`
- [x] **T024** [P] [US7] [AI-004] Record the dependency-analysis failure, its signature and the command that works instead · `.github/memories/repo/mcp-tooling.md`

**⟶ Wait for Wave W5 to finish, then:**

- [ ] **T025** [US1] [SEC-001] Record the disclosed-credential prerequisite for the plant operator: name both variables, print no value, and state that the committed values are treated as disclosed and are rotated off-repository · release note in the phase report `(no file change)` ⛔ blocked-here: needs the plant operator and the deployment runbook owner, neither on this workstation

**Phase 1 gate — closes on T026, after every wave above:**
- [x] **T026** [US1, US3, US6, US7, US8] [FR-033, FR-036] Phase 1 evidence: a search of every tracked file finds no credential value; restore, build with zero errors and warnings reported, then the suite green with 2,768 methods and all 17 guardrails, by the three documented commands; record each result as the measurement that opens Phase 2 · `MTM_Waitlist.sln`, `MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj`

---

## Phase 2 — Dependency infrastructure (W6-W8)

DEP-008, DEP-009, DEP-004.

Files: `Directory.Packages.props` (new); all 17 `*.csproj`; `Directory.Build.props:77`; `NuGet.Config`.

**Prerequisite gate**: Phase 1 gate green, at zero errors with warnings reported rather than suppressed.

### Tests

No test can see a declaration file. The instruments are NU1008, a cold restore and the mapping's coverage, so
this phase's verification is a read of the declarations.

- [x] **T027** [P] [US2] [DEP-009] Verify, `(no file change)`: every package id the solution declares is matched by a pattern under the `nuget.org` mapping, and no wildcard fallback exists · `NuGet.Config`, all 17 `*.csproj` — rides in W7

### Implementation

**Wave W6 — DEP-008 — closes on T028-T030:**
- [x] **T028** [US2] [DEP-008] Create central package management: `ManagePackageVersionsCentrally` true and one `PackageVersion` per package id, covering all 81 declarations over 22 packages, with the inherited `System.Security.Cryptography.Pkcs` 9.0.21 relocated here as a `PackageVersion` rather than retired · `Directory.Packages.props`
- **⟶ then, two independent removals:**
- [x] **T029** [P] [US2] [DEP-008] Remove every inline `Version` from every `PackageReference` across the 17 projects so NU1008 is clean; a real exception is declared as `VersionOverride`, never as a silent override · all 17 `*.csproj`
- [x] **T030** [P] [US2] [DEP-008] Strip the `Version` from the relocated `Pkcs` `PackageReference`, leaving no other version-carrying item in the file · `Directory.Build.props:77`

**⟶ Wait for Wave W6 to finish, then:**

**Wave W7 — DEP-009 — closes on T027, T031:**
- [x] **T031** [US2] [DEP-009] Add `packageSourceMapping` over the existing `nuget.org` entry, with a pattern set covering every declared package id and no wildcard · `NuGet.Config`
- **⟶ then:** `T027` verifies the coverage it just created.

**⟶ Wait for Wave W7 to finish, then:**

**Wave W8 — DEP-004 — closes on T032-T033.** Three single-line bumps, only because W6 centralised them.
- [x] **T032** [US2] [DEP-004] Bump WinUIEx 2.9.2 to 2.9.3, Microsoft.Windows.SDK.BuildTools 10.0.28000.2270 to 10.0.28000.2957 and System.Security.Cryptography.ProtectedData 10.0.0 to 11.0.0, recording each previous release here before the edit · `Directory.Packages.props`
- **⟶ then:**
- [x] **T033** [US2] [DEP-004] Cold restore from a cleared cache, then build: no new warning, and the three packages resolve at their targets · `MTM_Waitlist.sln` `(no file change)`

**Phase 2 gate — closes on T034, after every wave above:**
- [x] **T034** [US2] [FR-033, FR-036] Phase 2 evidence: `Directory.Packages.props` holds one version per package, no project carries an inline `Version`, a cold restore succeeds, the tracked `NuGet.Config` carries its mapping, and the suite plus all 17 guardrails are green by the three documented commands · `MTM_Waitlist.sln`, `MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj`

---

## Phase 3 — Runtime package upgrades (W9-W13)

DEP-002, DEP-006, DEP-005, DEP-001, DEP-007. Strictly sequential: each upgrade is validated before the next.

Files: `Directory.Packages.props` (one edit per wave); `.github/copilot-startup-steps.yaml` and
`.github/memories/repo/github-artifact-staleness.md` re-verified at W12.

**Prerequisite gate**: Phase 2 gate green.

### Tests

Each wave's evidence is a restore, a build and the full suite at that wave, plus a connectivity proof or a
screen pass where the wave changes observable behaviour. The local-instance proof runs before the plant one.

- [x] **T035** [P] [US2] [DEP-002] Verify, `(no file change)`: restore and build with zero errors, then the suite green at the unchanged discovered count · `MTM_Waitlist.sln` — rides in W9
- [x] **T036** [P] [US2] [DEP-001] Verify, `(no file change)`: restore and build with zero errors, then the suite green at the unchanged discovered count · `MTM_Waitlist.sln` — rides in W12
- [ ] **T037** [P] [US2] [DEP-007] Verify, `(no file change)`: restore and build with zero errors, then the suite green at the unchanged discovered count · `MTM_Waitlist.sln` — rides in W13

### Implementation

**Wave W9 — DEP-002 — closes on T035, T038-T039:**
- [x] **T038** [US2] [DEP-002] Bump Microsoft.Extensions.Hosting, .Configuration.Abstractions, .DependencyInjection.Abstractions, .Logging.Abstractions and .Options from 10.0.10 to 11.0.0, recording 10.0.10 here before the edit · `Directory.Packages.props`
- **⟶ then:**
- [ ] **T039** [US2] [DEP-002] Confirm the application starts, binds configuration and filters logs as before · `MTM_Waitlist.Startup/Services/ConfigurationStep.cs` `(no file change)` ⛔ blocked-here: needs the two connection-string variables provisioned and a reachable store

**⟶ Wait for Wave W9 to finish, then:**

**Wave W10 — DEP-006 — closes on T040-T042:**
- [x] **T040** [US2] [DEP-006] Bump MySqlConnector from 2.6.1 to 3.0.0, recording 2.6.1 here before the edit · `Directory.Packages.props`
- **⟶ then:**
- [ ] **T041** [US2] [DEP-006] Exercise every stored-procedure call path against the local instance before the plant is touched: an internal-store read and write, then a mirror refresh · `MTM_Waitlist.Core/Services/MySqlHelperServer.cs` `(no file change; needs a local MySQL instance)`
- [ ] **T042** [US2] [DEP-006] Repeat the same read and write against the plant host · `MTM_Waitlist.Core/Services/MySqlHelperServer.cs` `(no file change)` ⛔ blocked-here: the plant host is not reachable from this workstation

**⟶ Wait for Wave W10 to finish, then:**

**Wave W11 — DEP-005 — closes on T043-T045.** Needs `T030`, which moved the pin, before it can retire it.
- [x] **T043** [US2] [DEP-005] Bump Microsoft.Data.SqlClient from 6.1.1 to 8.0.0 and retire the `System.Security.Cryptography.Pkcs` 9.0.21 `PackageVersion` in the same change, recording 6.1.1 and 9.0.21 here before the edit · `Directory.Packages.props`
- **⟶ then:**
- [ ] **T044** [US2] [DEP-005] Prove an Infor Visual read through the live path against the local instance, and confirm the restore no longer raises NU1902 or NU1903 · `MTM_Waitlist.Core/Services/InforVisualSqlQueryService.cs` `(no file change; needs a local instance)`
- [ ] **T045** [US2] [DEP-005] Repeat the Infor Visual read against the plant host · `MTM_Waitlist.Core/Services/InforVisualSqlQueryService.cs` `(no file change)` ⛔ blocked-here: the plant host is not reachable from this workstation

**⟶ Wait for Wave W11 to finish, then:**

**Wave W12 — DEP-001 — closes on T036, T046-T048:**
- [x] **T046** [US2] [DEP-001] Bump Microsoft.WindowsAppSDK from 2.4.0 to 2.7.0 in every project that declares it, recording 2.4.0 here before the edit · `Directory.Packages.props`
- **⟶ then:**
- [ ] **T047** [US2] [DEP-001] Manual screen pass on sign-in, waitlist list and detail, setup, generic request, settings and the deployment tool, each at each display scale in use; a defect found is fixed inside this phase; `WMC9999` is treated as a real masked XAML error · the six screens `(no file change)` ⛔ blocked-here: needs an interactive run at each display scale, which this workstation cannot drive
- [x] **T048** [US2] [AI-001] Re-verify the three agent-facing version statements against 2.7.0 now that the platform has moved · `.github/copilot-startup-steps.yaml`, `Directory.Build.props:47`, `.github/memories/repo/github-artifact-staleness.md` `(no file change unless a figure is wrong)`

**⟶ Wait for Wave W12 to finish, then:**

**Wave W13 — DEP-007 — closes on T037, T049-T050:**
- [x] **T049** [US2] [DEP-007] Bump DevWinUI from 10.8.0 to 11.1.0, last among the runtime upgrades, recording 10.8.0 here before the edit · `Directory.Packages.props`
  - The `CS1705` that blocked this wave was **not** a missing Windows platform release. DevWinUI 11.1.0 is compiled against `WinRT.Runtime` 2.3.1.0; every `Microsoft.Windows.SDK.NET.Ref` release the .NET SDK picks from the target platform version up to `10.0.26100.57` ships `2.2.0.0`, and `10.0.28000.87` is the first that ships `2.3.1.0`. The projection package is a per-project override, so `Directory.Build.props` now pins `WindowsSdkPackageVersion` to `10.0.28000.87` — the override Microsoft documents for exactly this case (a package newer than the .NET SDK's paired Windows SDK package). Verified: `dotnet build MTM_Waitlist.sln -p:Platform=x64` is 0 warnings / 0 errors, the suite is 2,915 passed / 66 skipped / 0 failed, and the running application reaches sign-in, the shell, Settings, Work Center Setup and the `WindowedContentDialog` form.
  - The pin also brings the `CsWinRT1032` authoring analyzer, which flagged six collection expressions in the test project. Each now carries the explicit array cast the analyzer documents, so the warning count stayed at 0.
- **⟶ then:**
- [ ] **T050** [US2] [DEP-007] Review every screen for visual regression: a control template or theme change is the expected failure, and it is not caught by the build · the six screens `(no file change)` ⛔ blocked-here: partially driven. Sign-in, the waitlist shell and its card, Settings, Work Center Setup and the work center form were driven through UI Automation on 2026-11-27 and render correctly. Still outstanding: the deployment tool window, and each screen re-read at each display scale the plant uses.

**Phase 3 gate — closes on T051, after every wave above:**
- [x] **T051** [US2] [FR-033, FR-036] Phase 3 evidence: every package at its target, DevWinUI included at 11.1.0 through the `WindowsSdkPackageVersion` pin recorded in `T049`; the pin gone from its new home; a cold restore and the suite green at each wave; the local-instance proofs recorded; the plant-host and display-scale screen passes recorded as blocked rather than ticked · `MTM_Waitlist.sln`, `MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj`

---

## Phase 4 — Test stack and test quality (W14-W15)

DEP-003, DEV-003.

Files: `Directory.Packages.props`; `MTM_Waitlist.Tests/Module_Startup/Services/VisualVerdictPrimingStepTests.cs`.

**Prerequisite gate**: Phases 1 to 3 green, so the suite measures settled code.

### Tests

- [x] **T052** [P] [US2] [DEP-003] Verify, `(no file change)`: the adapter discovers 2,768 methods, every one runs, and every result matches the run before the bump · `MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj` — rides in W14
- [x] **T053** [P] [US9] [DEV-003] Break the covered behaviour deliberately, confirm the test now fails, then restore it: the assertion does the work, not the wait · `MTM_Waitlist.Tests/Module_Startup/Services/VisualVerdictPrimingStepTests.cs` — rides in W15

### Implementation

**Wave W14 — DEP-003 — closes on T052, T054.** Before `T055`, which edits a test this adapter must still run.
- [x] **T054** [US2] [DEP-003] Bump MSTest.TestAdapter and MSTest.TestFramework from 3.7.0 to 4.5.3, Microsoft.NET.Test.Sdk from 17.13.0 to 18.12.0 and coverlet.collector from 6.0.2 to 10.1.1, recording each previous release here before the edit · `Directory.Packages.props`
- **⟶ then:** `T052` proves the discovery and every result.

**⟶ Wait for Wave W14 to finish, then:**

**Wave W15 — DEV-003 — closes on T053, T055:**
- [x] **T055** [US9] [DEV-003] Replace the three-second real-clock wait on a deliberately unstoppable step with an injected time source or a bound, asserting the verdict through it · `MTM_Waitlist.Tests/Module_Startup/Services/VisualVerdictPrimingStepTests.cs:209`
- **⟶ then:** `T053` proves the assertion carries the test.

**Phase 4 gate — closes on T056, after every wave above:**
- [x] **T056** [US2, US9] [FR-033, FR-036] Phase 4 evidence: 2,768 methods discovered and green under MSTest 4.5.3; no test waits on a real clock for more than 500 ms; the replaced test fails when its behaviour is broken; restore, build and suite by the three documented commands · `MTM_Waitlist.sln`, `MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj`

---

## Phase 5 — Framework-native replacement (W16-W22)

MOD-001, DEV-001, MOD-003, MOD-002, MOD-005, MOD-004, MOD-009.

Files: `Directory.Packages.props`; `MTM_Waitlist.csproj`, `MTM_Waitlist.Core/MTM_Waitlist.Core.csproj`;
`Module_Waitlist/Views/WaitlistViewPage.xaml`, `WaitlistViewDetailPage.xaml`, `WaitlistViewDetailPage.xaml.cs`;
`MTM_Waitlist.Core/Services/NavigationService.cs`, `MySqlHelperServer.cs`,
`MTM_Waitlist.Core/Contracts/Services/IMySqlHelperServer.cs`, `UserManagementRepository.cs`,
`PermissionAdministrationService.cs`, `LuggerArrangementService.cs`, `ConnectionHealthService.cs`;
`MTM_Waitlist.Shared/Services/WorkCenterCatalogService.cs`; `Directory.Build.props:56`;
`MTM_Waitlist.Setup/Services/InforVisualSqlQueryService.cs` and the Setup module registration; the settings,
mock-service and mock JSON stores; `Module_Shared/Controls/RotatingTextView.xaml.cs`.

**Prerequisite gate**: Phase 3 gate green. Inside the phase, `T065` needs `T060`-`T063`, and `T073`-`T077`
follow `T068`-`T072`.

### Tests

- [x] **T057** [P] [US4] [MOD-003] Test: a read past the shared row cap is bounded, and the bounded result is what the caller gets · `MTM_Waitlist.Tests/Core/Services/MySqlHelperServerRowCapTests.cs` — rides in W18
- [x] **T058** [P] [US4] [MOD-005] Construct the surviving Core type instead of the deleted Setup copy, asserting the same queue behaviour · `MTM_Waitlist.Tests/Module_Setup/Services/InforVisualSqlQueryServiceTests.cs:115` — rides in W20
- [x] **T059** [P] [US5] [MOD-004] Test: a value written by the old reflection-based path is read by the generated metadata with its optional and derived values intact · `MTM_Waitlist.Tests/Module_Settings/Services/JsonRoundTripTests.cs` — rides in W21

### Implementation

**Wave W16 — MOD-001 — closes on T060-T064:**
- [x] **T060** [US5] [MOD-001] Replace the retired toolkit line with `CommunityToolkit.WinUI.Animations` 8.2.251219 and drop the `Controls` reference nothing uses, recording 7.1.2 here before the edit · `Directory.Packages.props`, `MTM_Waitlist.csproj`, `MTM_Waitlist.Core/MTM_Waitlist.Core.csproj`
- **⟶ then, three source files, no one read by another:**
- [x] **T061** [P] [US5] [MOD-001] Move to the 8.x namespace and delete the dead namespace declaration · `Module_Waitlist/Views/WaitlistViewPage.xaml:6-7`
- [x] **T062** [P] [US5] [MOD-001] Same namespace move · `Module_Waitlist/Views/WaitlistViewDetailPage.xaml:5`, `Module_Waitlist/Views/WaitlistViewDetailPage.xaml.cs:1`
- [x] **T063** [P] [US5] [MOD-001] Same namespace move · `MTM_Waitlist.Core/Services/NavigationService.cs:3`
- **⟶ then:**
- [ ] **T064** [US5] [MOD-001] Confirm the waitlist-to-detail connected animation still runs, and keep `RetiredSymbolAuditTests` passing without weakening it · the waitlist list and detail screens `(no file change)` ⛔ blocked-here: needs an interactive run to observe the transition

**⟶ Wait for Wave W16 to finish, then:**

**Wave W17 — DEV-001 — closes on T065-T067.** Gated on W16, which unifies Win2D.
- [x] **T065** [US5] [DEV-001] Cold restore with `UseRidGraph` still present, recording the result as the baseline the removal is judged against · `MTM_Waitlist.sln` `(no file change)`
- **⟶ then:**
- [x] **T066** [US5] [DEV-001] Remove `UseRidGraph` and the comment block carrying its rationale · `Directory.Build.props:56`
- **⟶ then:**
- [x] **T067** [US5] [DEV-001] Cold restore after removal: no NETSDK1206; if one appears, restore the property and record why it stays rather than forcing the removal · `Directory.Build.props`, `MTM_Waitlist.sln` `(no file change unless the property returns)`

**⟶ Wait for Wave W17 to finish, then:**

**Wave W18 — MOD-003 — closes on T057, T068-T072.** The seam narrows before five services adopt it.
- [x] **T068** [US4] [MOD-003] Identify every caller of the statement-text path before removing anything: `ExecuteSqlNonQueryAsync` has no production caller, and `ExecuteSqlQueryAsync` has exactly one - the allowlisted queue-script reader whose justification records that its C# carries script names and never statement text; record the finding and the stop condition before the next task · `MTM_Waitlist.Core/Services/WipFloorInventoryService.cs:97,140,193,335`, `MTM_Waitlist.Tests/Module_Mock/InlineSqlAuditTests.cs:71-81` `(no file change)`
- **⟶ then:**
- [x] **T069** [US4] [MOD-003] Remove `ExecuteSqlNonQueryAsync`, which no production caller reaches · `MTM_Waitlist.Core/Services/MySqlHelperServer.cs:128`, `MTM_Waitlist.Core/Contracts/Services/IMySqlHelperServer.cs:29`
- [x] **T070** [US4] [MOD-003] Bound `ReadRowsAsync` with the one row cap both paths share, and make `T057` green · `MTM_Waitlist.Core/Services/MySqlHelperServer.cs:286`
- **⟶ then:**
- [x] **T071** [US4] [MOD-003] Narrow `ExecuteSqlQueryAsync` and `CreateTextCommand` so no unreviewed caller can supply statement text, and shrink the raw-SQL allowlist to the entries that still describe code that exists · `MTM_Waitlist.Core/Services/MySqlHelperServer.cs:107,262`, `MTM_Waitlist.Core/Contracts/Services/IMySqlHelperServer.cs:23`, `MTM_Waitlist.Tests/Module_Mock/InlineSqlAuditTests.cs`
- **⟶ then:**
- [x] **T072** [US4] [MOD-003] Re-run `InlineSqlAuditTests` and the suite, then record what the removal could not reach: the one reviewed queue-script reader keeps a script-text route because turning its checked-in scripts into procedures is database authoring, which this feature does not do; the record names the finding, the reason and a `// simplified:` note at the route · `MTM_Waitlist.Tests/Module_Mock/InlineSqlAuditTests.cs`, `MTM_Waitlist.Core/Services/MySqlHelperServer.cs` `(no file change beyond the note)`

**⟶ Wait for Wave W18 to finish, then:**

**Wave W19 — MOD-002 — closes on T073-T079.** Five services, five files, no shared file: five parallel tasks.
- [x] **T073** [P] [US4] [MOD-002] Route both sites through the seam; the `InvalidOperationException` on an unconfigured store stays at the caller, because the seam returns a neutral result there - done by `T123`: both sites now call `ExecuteStrictAsync`, and an unconfigured store answers `MySqlStoreNotConfiguredException` (an `InvalidOperationException` carrying the same sentence), which the reader lets through and the writer maps to its existing unavailable result · `MTM_Waitlist.Core/Services/UserManagementRepository.cs:259,377`
- [x] **T074** [P] [US4] [MOD-002] Route through the seam, keeping the caller's own throw - done by `T123`: the `1644` refusal still reaches the caller · `MTM_Waitlist.Core/Services/PermissionAdministrationService.cs:477`
- [x] **T075** [P] [US4] [MOD-002] Route through the seam, keeping the caller's own throw - done by `T123`: writes use the strict route (scalar `affected_rows`), reads stay lenient, and 1305/1146 are still distinguished · `MTM_Waitlist.Core/Services/LuggerArrangementService.cs:403`
- [x] **T076** [P] [US4] [MOD-002] Route through the seam, losing no behaviour - done by `T123`: the clear and the upsert share one `UseTransaction: true` scope, with the timeouts the service applied itself · `MTM_Waitlist.Shared/Services/WorkCenterCatalogService.cs:295`
- [x] **T077** [P] [US4] [MOD-002] Route through the seam, losing no behaviour · `MTM_Waitlist.Core/Services/ConnectionHealthService.cs:108` - **deliberately not moved.** The probe opens a connection and calls no procedure, applies its own 5-second `ConnectionTimeout` rather than the waitlist options', and one of the two sources it probes is SQL Server. The seam cannot express that without changing the probe's timeout and reach, so the service keeps its own connection and the reason is recorded at the code as a `// simplified:` note
- **⟶ then:**
- [x] **T078** [US4] [MOD-002] Confirm no direct `MySqlConnection` or `MySqlCommand` construction remains in an application service, and that the test doubles still satisfy the narrowed interface - confirmed: the only construction under `MTM_Waitlist.Core/Services/` is the seam itself plus the deliberately unmoved health probe, `MTM_Waitlist.Shared/Services/` has none, and `ExecuteStrictAsync` carries a refusing default so the 40 existing doubles still satisfy the interface · `MTM_Waitlist.Core/Services/`, `MTM_Waitlist.Shared/Services/` `(no file change)`
- [x] **T079** [US4] [MOD-002] Restore, build, and run the suite and all 17 guardrails · `MTM_Waitlist.sln` `(no file change)` - run 2026-11-27: `dotnet build MTM_Waitlist.sln --nologo` reports `0 Warning(s) 0 Error(s)`, and `dotnet test` reports `Failed: 0, Passed: 2908, Skipped: 66`. The four added seam cases are `Core/Services/MySqlHelperServerStrictRouteTests.cs`; the rollback case is environment-gated on `MTM_WAITLIST_TEST_DB_CONNECTION_STRING` and is the one skip that is new.

**⟶ Wait for Wave W19 to finish, then:**

**Wave W20 — MOD-005 — closes on T058, T080:**
- [x] **T080** [US4] [MOD-005] Delete the Setup copy and register Core's type in its place, leaving one owner of the Infor Visual read path and the fallback rule untouched; `T058` retargets the test that constructs the deleted type · `MTM_Waitlist.Setup/Services/InforVisualSqlQueryService.cs`, `MTM_Waitlist.Setup` module registration, `MTM_Waitlist.Tests/Module_Setup/Services/InforVisualSqlQueryServiceTests.cs`

**⟶ Wait for Wave W20 to finish, then:**

**Wave W21 — MOD-004 — closes on T059, T081-T084:**
- [x] **T081** [US5] [MOD-004] Enumerate every JSON store, its serializer call site and its options construction across the three libraries, so the next two tasks name real files · `MTM_Waitlist.Settings/`, `MTM_Waitlist.Mock.Service/`, `MTM_Waitlist.Mock/` `(no file change)`
- **⟶ then, two independent libraries:**
- [x] **T082** [P] [US5] [MOD-004] Add the generated per-type context and one options definition for the settings stores, the first being the list filter · `MTM_Waitlist.Settings/Models/UserListFilter.cs` plus the settings files `T081` lists
- [x] **T083** [P] [US5] [MOD-004] Add the generated per-type context and one options definition for the mock-service and mock stores · the `MTM_Waitlist.Mock.Service/` and `MTM_Waitlist.Mock/` files `T081` lists
- **⟶ then:**
- [x] **T084** [US5] [MOD-004] Confirm no store builds serializer options per call and that every stored value round-trips, optional and derived ones included · the stores `T081` lists `(no file change)`

**⟶ Wait for Wave W21 to finish, then:**

**Wave W22 — MOD-009 — closes on T085-T086:**
- [x] **T085** [US5] [MOD-009] Replace the hand-built `Storyboard` and `DoubleAnimation` with the toolkit's animation idiom, same timing and same frames · `Module_Shared/Controls/RotatingTextView.xaml.cs:138-195`
- **⟶ then:**
- [ ] **T086** [US5] [MOD-009] Confirm visual parity on screen · the setup review screen `(no file change)` ⛔ blocked-here: needs an interactive run to compare the rotation

**Phase 5 gate — closes on T087, after every wave above:**
- [ ] **T087** [US1, US4, US5] [FR-033, FR-036] Phase 5 evidence: no 7.1.2 package remains and the transition still animates; a cold restore raises no NETSDK1206 with the accommodation gone; the five services route through the seam; one Infor Visual query type; generated metadata per store with values round-tripping; one animation idiom; the raw-SQL allowlist describes exactly the code that exists; restore, build and suite green with all 17 guardrails by the three documented commands · `MTM_Waitlist.sln`, `MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj`

---

## Phase 6 — Architectural modernization (W23-W25)

MOD-006, MOD-007, MOD-008. Sequential; each is its own review cycle.

Files: `MTM_Waitlist.Shared/Dialogs/MessageDialogContent.xaml` and `.xaml.cs`, `MessageDialogHost.cs`,
`WindowedDialogHost.cs`, `DialogSizing.cs`; `MTM_Waitlist.GenericRequest/MTM_Waitlist.GenericRequest.csproj`,
`MTM_Waitlist.GenericRequest.Data/MTM_Waitlist.GenericRequest.Data.csproj`,
`MTM_Waitlist.GenericRequest/Services/GenericRequestLiveStepData.cs`;
`MTM_Waitlist.Settings/SettingsHub/ViewModels/SettingsViewModel.cs` and the parts it splits into.

**Prerequisite gate**: Phase 5 gate green, so data-access and toolkit churn is settled.

### Tests

- [x] **T088** [P] [US5] [MOD-006] Move the body-path assertion to the composed body while keeping the sizing contract asserted · `MTM_Waitlist.Tests/Module_Shared/Dialogs/DialogSizingAuditTests.cs` — rides in W23

### Implementation

**Wave W23 — MOD-006 — closes on T088-T091:**
- [x] **T089** [US5] [MOD-006] Compose the dialog body in code inside `Shared` and remove the XAML view type, keeping `MessageDialogHost`'s signature and its 22 production call sites unchanged; the body cannot move to a module because `MTM_Waitlist.Mock.Service` also shows this dialog and the app references the service; add no page item, the default globs still apply · `MTM_Waitlist.Shared/Dialogs/MessageDialogContent.xaml`, `MessageDialogContent.xaml.cs`, `MessageDialogHost.cs`, `DialogSizing.cs`
- **⟶ then:**
- [x] **T090** [US5] [MOD-006] Confirm `MTM_Waitlist.Shared` declares no `UserControl`, `Page` or `Window` type and that all 22 callers still compile · `MTM_Waitlist.Shared/` `(no file change)`
- [ ] **T091** [US5] [MOD-006] Confirm every caller still shows the dialog with the same content and result, `ServiceSettingsPage` included · the 22 call sites `(no file change)` ⛔ blocked-here: needs an interactive run to open the dialog from its call sites

**⟶ Wait for Wave W23 to finish, then:**

**Wave W24 — MOD-007 — closes on T092-T094:**
- [x] **T092** [US5] [MOD-007] Remove the waitlist UI project references from both generic-request libraries so the dependency runs one way · `MTM_Waitlist.GenericRequest/MTM_Waitlist.GenericRequest.csproj`, `MTM_Waitlist.GenericRequest.Data/MTM_Waitlist.GenericRequest.Data.csproj`
- **⟶ then:**
- [x] **T093** [US5] [MOD-007] Move the reads the removed references carried onto a shared contract the generic-request libraries can see, adding no project · `MTM_Waitlist.GenericRequest/Services/GenericRequestLiveStepData.cs`
- **⟶ then:**
- [ ] **T094** [US5] [MOD-007] Confirm the generic-request flow still works end to end · the generic request screens `(no file change)` ⛔ blocked-here: needs an interactive run against a reachable store

**⟶ Wait for Wave W24 to finish, then:**

**Wave W25 — MOD-008 — closes on T095-T096:**
- [x] **T095** [US10] [MOD-008] Split the 3,023-line settings class into cohesive parts behind the existing page, changing no behaviour, no binding and no validation message · `MTM_Waitlist.Settings/SettingsHub/ViewModels/SettingsViewModel.cs`
- **⟶ then:**
- [x] **T096** [US10] [MOD-008] Run the settings tests and the full suite: every existing settings test passes, and a test that had to change changed only to follow the split · `MTM_Waitlist.Tests/Module_Settings/` `(no file change)`

**Phase 6 gate — closes on T097, after every wave above:**
- [ ] **T097** [US5, US10] [FR-033, FR-036] Phase 6 evidence: no view type in `MTM_Waitlist.Shared`; no waitlist UI project reference from either generic-request library; the settings surface unchanged with its tests passing; the manual passes recorded as blocked rather than ticked; restore, build and suite green with all 17 guardrails · `MTM_Waitlist.sln`, `MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj`

---

## Phase 7 — Performance and localization (W26-W28)

PERF-001, UX-003, UX-002.

Files: `MTM_Waitlist.GenericRequest/Views/GenericRequestPartPage.xaml.cs`;
`MTM_Waitlist.Waitlist.View/Services/AverageCoilWeightService.cs` and the view that composes its text;
`Strings/en-us/Resources.resw`; the view definitions under `Module_Core`, `Module_Settings`,
`Module_Waitlist`, `Module_Shared`.

**Prerequisite gate**: Phase 6 gate green, so the XAML being edited is the final layout.

### Tests

- [x] **T098** [P] [US3] [PERF-001] Test: a scripted ten-character part number with pauses reaches the reader once per pause, a superseded answer never overwrites a newer one, and an abandoned search never reaches the interface · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestPartSearchTimingTests.cs` — rides in W26
- [x] **T099** [P] [US6] [UX-003] Test: the weight renders with the current culture's separators and the unit resolves from a resource, not from the code · `MTM_Waitlist.Tests/Module_Waitlist/Services/AverageCoilWeightServiceTests.cs` — rides in W27
- [ ] **T100** [P] [US6] [UX-002] Verify, `(no file change)`: a search of every view definition for a literal `Text`, `Header`, `Content` or `PlaceholderText` attribute returns nothing, and `PermissionGateSiteAuditTests` and `SurfaceInventoryAuditTests` are green - extraction changes attributes, not files · all view definitions, `MTM_Waitlist.Tests/Module_Core/Permissions/PermissionGateSiteAuditTests.cs`, `MTM_Waitlist.Tests/Module_Core/Views/SurfaceInventoryAuditTests.cs` — rides in W28

### Implementation

**Wave W26 — PERF-001 — closes on T098, T101-T102:**
- [x] **T101** [US3] [PERF-001] Replace the per-keystroke search with one read per typing pause, holding a cancellation token that is cancelled when typing resumes · `MTM_Waitlist.GenericRequest/Views/GenericRequestPartPage.xaml.cs:31-45`
- **⟶ then:**
- [x] **T102** [US3] [PERF-001] Hand each pause a sequence number so an older answer is discarded by identity rather than arrival order - a read the plant already answered cannot be cancelled - and keep the failure state stating the attempt time · `MTM_Waitlist.GenericRequest/Views/GenericRequestPartPage.xaml.cs`

**⟶ Wait for Wave W26 to finish, then:**

**Wave W27 — UX-003 — closes on T099, T103-T105:**
- [x] **T103** [US6] [UX-003] Format with the current culture instead of the invariant one, leaving the unit out of the service: it returns the number · `MTM_Waitlist.Waitlist.View/Services/AverageCoilWeightService.cs:56`
- **⟶ then:**
- [x] **T104** [US6] [UX-003] Resolve the unit from a resource where the text is composed, and record the chosen key, which the spec does not pin · the view that composes the weight text, `Strings/en-us/Resources.resw`
- **⟶ then:**
- [ ] **T105** [US6] [UX-003] Run on a machine whose culture uses a decimal comma and read the weight: that machine's separators, and a translatable unit · the waitlist detail screen `(no file change)` ⛔ blocked-here: needs a run under a culture other than this workstation's, with the screen read

**⟶ Wait for Wave W27 to finish, then:**

**Wave W28 — UX-002 — closes on T100, T106-T112:**
- [ ] **T106** [US6] [UX-002] Measure the literal prose attributes before editing, so the before and after counts are the same measurement · all view definitions `(no file change)`
- **⟶ then, four modules, four disjoint file sets:**
- [ ] **T107** [P] [US6] [UX-002] Extract every literal `Text`, `Header`, `Content` and `PlaceholderText` to the resource file through the one convention; change no layout, wording or control · `Module_Core` view definitions, `Strings/en-us/Resources.resw`
- [ ] **T108** [P] [US6] [UX-002] Same extraction, same convention · `Module_Settings` view definitions
- [ ] **T109** [P] [US6] [UX-002] Same extraction, same convention · `Module_Waitlist` view definitions
- [ ] **T110** [P] [US6] [UX-002] Same extraction, same convention, leaving the two tooltip resource files alone; a key composed at run time from a permission or role name is not renamed · `Module_Shared` view definitions
- **⟶ then:**
- [ ] **T111** [US6] [UX-002] Confirm the search returns nothing, both guardrails are green and the surface inventory still lists each XAML file once, by the verification above · all view definitions `(no file change)`
- [ ] **T112** [US6] [UX-002] Read every screen under a culture other than the developer's · the six screens `(no file change)` ⛔ blocked-here: needs a run under another culture with the screens read

**Phase 7 gate — closes on T113, after every wave above:**
- [ ] **T113** [US3, US6] [FR-033, FR-036] Phase 7 evidence: a scripted part number produces a single-digit round-trip count with the newest result shown; the weight uses the machine's separators and a resource unit; no literal user-visible attribute remains and one convention resolves every string; the locale runs recorded as blocked rather than ticked; restore, build and suite green with all 17 guardrails · `MTM_Waitlist.sln`, `MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj`

---

## Phase 8 — AI environment and design records (W29-W30), then closure

AI-003, DEV-004, then the cross-cutting closure FR-033 to FR-036.

Files: `.mcp.json`; the three `docs/ui-designs/startup-launch/sign-in/*/*-comparison.pdf` files and their
design JSON.

**Prerequisite gate**: Phase 7 gate green, so the design records are generated from final screens.

### Tests

- [x] **T114** [P] [US7] [AI-003] Verify, `(no file change)`: a fresh clone's configuration declares both servers, the prerequisites are recorded beside them, and one answers a documentation query - or neither is declared and AI-003 is reported rejected rather than half-declared · `.mcp.json` — rides in W29
- [ ] **T115** [P] [US8] [DEV-004] Verify, `(no file change)`: each of the three comparison documents is newer than its design source · the three `*-comparison.pdf` files and their design JSON — rides in W30

### Implementation

**Wave W29 — AI-003 — closes on T114, T116-T117:**
- [x] **T116** [US7] [AI-003] Declare `microsoft-learn` over HTTP at `https://learn.microsoft.com/api/mcp` and `context7` over stdio through `npx @upstash/context7-mcp`; both or neither · `.mcp.json`
- **⟶ then:**
- [x] **T117** [US7] [AI-003] Record the prerequisites beside the declaration: Node.js for `context7`; outbound HTTPS to `learn.microsoft.com`, `registry.npmjs.org` and the Context7 service; and that neither server reads this repository · `.mcp.json`

**⟶ Wait for Wave W29 to finish, then:**

**Wave W30 — DEV-004 — closes on T115, T118:**
- [ ] **T118** [US8] [DEV-004] Regenerate the three sign-in comparison documents from their current design source, and confirm no design source was edited to match a document · `docs/ui-designs/startup-launch/sign-in/*/*-comparison.pdf` ⛔ blocked-here: needs the design toolchain that produced the PDFs, which is not installed on this workstation

**⟶ Wait for Wave W30 to finish, then:**

### Closure (FR-033 to FR-036)

- [ ] **T119** [US1-US10] [FR-035] Register audit, `(no file change)`: walk the completed tasks and confirm each maps to one of the 32 approved proposal IDs, that no approved proposal is left unimplemented, and that nothing outside the register changed; an unplanned change found mid-phase is recorded as a new proposal rather than folded into an approved one · this file and the working diff
- [ ] **T120** [US1-US10] [FR-034] Behaviour audit, `(no file change)`: no demo or mock mode introduced; every data operation still a stored procedure; the Infor Visual fallback rule and its reachability verdict untouched; the mock-service deploy script still refuses to run off its own host; views still included by the default globs with no explicit page item; and no CI workflow, no warnings-as-errors and no new build enforcement added · `Database/`, `MTM_Waitlist.Mock.Service/deploy/install-mock-service.ps1`, `Directory.Build.props`, all `*.csproj`, the absent `.github/workflows`

**Phase 8 gate — closes on T121, after every wave above:**
- [ ] **T121** [US1-US10] [FR-033, FR-036] Final validation against the Success Criteria, by the three documented commands: cold restore, build with zero errors and warnings reported, then the suite green with 2,768 methods and all 17 guardrails; walk SC-001 to SC-018 and record each as measured or as blocked with its reason; confirm every dependency change's previous release is recorded in this file so a revert is one edit · `MTM_Waitlist.sln`, `MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj`

---

## Dependencies and execution order

**Phase chain.** Phase 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8, each phase opening only on the previous phase gate. No
phase runs concurrently with another, so the four shared files above can never contend.

**Wave chains, in order:**

| Phase | Wave chain | Gate that blocks the next phase |
|---|---|---|
| 1 | W1 → W2 → W3 → W4 → W5, then `T025` | T026 |
| 2 | W6 (`T028` → `T029` ∥ `T030`) → W7 → W8 (`T032` → `T033`) | T034 |
| 3 | W9 → W10 → W11 → W12 → W13, each `bump → prove` | T051 |
| 4 | W14 (`T054` → `T052`) → W15 (`T055` → `T053`) | T056 |
| 5 | W16 → W17 (`T065` → `T066` → `T067`) → W18 → W19 (five parallel → `T078` → `T079`) → W20 → W21 → W22 | T087 |
| 6 | W23 → W24 → W25 | T097 |
| 7 | W26 → W27 → W28 | T113 |
| 8 | W29 → W30, then closure `T119`, `T120` | T121 |

**Cross-phase constraints, restated where they bite:** `T028` before every version edit; `T030` before `T043`;
`T060`-`T063` before `T065`; `T068`-`T071` before `T073`-`T077`; `T054` before `T055`.

**Genuinely parallel sets,** each inside one wave: W4's four files, W5's seven, W6's `T029`/`T030`, W16's three
source files, W19's five services, W21's two libraries, W28's four modules. Everything else is a chain.

## Blocked on this workstation

Implement reports these as blocked with the reason given and leaves them unchecked. None is skippable: each
is required evidence that has to be produced somewhere else.

| Task | What it needs |
|---|---|
| T025 | The plant operator and the deployment runbook owner, to provision two variables and rotate disclosed values |
| T049 | **Resolved 2026-11-27.** The block was the Windows SDK projection package, not a missing platform release: DevWinUI 11.1.0 needs `WinRT.Runtime` 2.3.1.0, and `WindowsSdkPackageVersion` 10.0.28000.87 supplies it. DevWinUI is at 11.1.0; `T050`'s deployment-tool and display-scale passes remain open |
| T039 | The two connection-string variables, provisioned, and a reachable store |
| T042 | The plant host, reachable |
| T045 | The plant host, reachable |
| T047 | An interactive run of six screens at each display scale in use |
| T050 | An interactive screen review across all six screens |
| T064 | An interactive run to observe the waitlist-to-detail transition |
| T086 | An interactive run to compare the rotation on screen |
| T091 | An interactive run to open the dialog from its call sites |
| T094 | An interactive run of the generic-request flow against a reachable store |
| T105 | A run under a culture other than this workstation's, with the weight read |
| T112 | A run under another culture, with every screen read |
| T118 | The design toolchain that produced the three comparison PDFs |

Two tasks are verifiable here only against the local instance, and are marked that way rather than blocked:
`T041` and `T044`. If the local instance is absent, they join this list with the reason.

---

## Phase 9: Convergence

Appended by `/speckit.converge` after the implement pass stopped with four waves still open. Nothing above this
line was changed. The unbuilt waves keep their own tasks and are the next pass's work list — W13 `T037`/`T049`,
W19 `T073`-`T079`, W28 `T106`-`T112`, W30 `T115`/`T118`, and the closure audits `T119`-`T121` — so no
task below restates one of them. These five are the gaps those waves do not name, found by reading the tree
against `spec.md`, `plan.md` and this file on 2026-11-27, with the solution at zero warnings and zero errors and
the suite green at 2,909 passed, 65 skipped, 0 failed.

- [ ] **T122** [US1] [SEC-001] Remove the credential values that are still committed, or record the dated owner-scoped
  deviation the constitution requires where one genuinely applies. `.github/memories/repo/workstation-secrets.md:20-23,29-34,38-40`
  carries a full `Server=172.16.1.104;…;User Id=root;Password=root;` connection string beside the `SHOP`/`SHOP2`
  passwords, and `MTM_Database_Deployment/deployment.settings.json:2,6` carries `"User": "root"` next to a plant server
  address. Keep each file's shape, its variable names and the deployment tool's own default server, and state in
  each file what replaced the value and which variable now supplies it. Also correct the Phase 1 gate `T026`, whose
  "a search of every tracked file finds no credential value" the tree does not support ·
  `.github/memories/repo/workstation-secrets.md`, `MTM_Database_Deployment/deployment.settings.json` · **CRITICAL** — per FR-019 and Constitution → Security & Secrets (contradicts)

- [x] **T123** [US4] [MOD-002] Add to the shared data-access seam the two things MOD-002 needs before any of the five
  services can route through it: a transaction scope that carries two stored-procedure calls as one unit, and a result
  that hands the caller the provider's own answer rather than the seam's neutral one. `IMySqlHelperServer` exposes only
  `ExecuteStoredProcedureQueryAsync` and `ExecuteStoredProcedureNonQueryAsync` and the implementation catches every
  exception, while `UserManagementRepository.cs:282-310` branches on `1062` and on a signalled refusal,
  `PermissionAdministrationService.cs:199`, `LuggerArrangementService.cs:416-420` and `ConnectionHealthService.cs:129-131`
  branch on the provider's own numbers, and `WorkCenterCatalogService.cs:289-315` runs its delete and its upsert inside one
  `MySqlTransaction`. Follow `research.md` R-13 — a behaviour the seam does not model is added to the seam, not dropped
  at the caller — and prove the new surface with test doubles, because no live store is reachable here ·
  `MTM_Waitlist.Core/Contracts/Services/IMySqlHelperServer.cs`, `MTM_Waitlist.Core/Services/MySqlHelperServer.cs` — opens `T073`-`T077` — per FR-011 (missing)

- [ ] **T124** [US6] [UX-002] Extract the literal `Text`, `Header`, `Content` and `PlaceholderText` prose from the two
  view sets W28 does not own, through the same convention and with no layout or wording change: the nine view files under
  `Module_Setup/Views` (87 attributes, 44 of them with no `x:Uid`) and `MTM_Database_Deployment/MainWindow.xaml` (36,
  none with an `x:Uid`). `T107`-`T110` name only `Module_Core`, `Module_Settings`, `Module_Waitlist` and
  `Module_Shared`, so the search in `T100` and `T111` cannot return nothing until these are done. The tree measures 289
  such attributes over 92 view definitions without an `x:Uid` and 332 in total, not the 140 SC-008 cites, so take the
  before count from `T106`'s own search rather than the audit's figure · `Module_Setup/Views`,
  `MTM_Database_Deployment/MainWindow.xaml`, `Strings/en-us/Resources.resw` — per FR-023 (partial)

- [x] **T125** [US2] [DEP-007] Record the DEP-007 outcome, its reason and the condition that closed it. **Closed 2026-11-27.**
  The original reading was wrong about the cause: the platform did not need to ship a newer release, because the
  Windows SDK projection package is a per-project override rather than a machine-level fact. DevWinUI 11.1.0 is built
  against `WinRT.Runtime` 2.3.1.0; `Microsoft.Windows.SDK.NET.Ref` 10.0.28000.87 is the first release carrying it, so
  `Directory.Build.props` pins `WindowsSdkPackageVersion` to that version and DevWinUI sits at 11.1.0. The pin is the
  only thing to revisit: drop it once the .NET SDK on the build machines supplies `WinRT.Runtime` 2.3.1.0 or later from
  the target platform version on its own. `T050`'s deployment-tool and display-scale passes stay open and are stated as
  such · `Directory.Packages.props`, `Directory.Build.props`, `MTM_Waitlist.sln` — per FR-007

- [x] **T126** [US5] [MOD-004] Give the two JSON-backed stores that still serialize by reflection the generated metadata the
  other stores got, or record a reasoned exception at each. `MTM_Waitlist.Waitlist.View/Services/LocalWaitlistMessageSeenStore.cs:66,92`
  writes and reads through `MTM_Waitlist.Core/Helpers/Json.cs:9,14`, which calls `JsonSerializer` with neither a context
  nor options, and `MTM_Database_Deployment.Core/Services/Service_DeploymentSettings.cs:12` defines its options once but
  builds them by reflection. `SettingsJsonContext` and `MockServiceJsonContext` cover the three libraries `T081`
  enumerated and not these two, so FR-013's "each store" is not yet true · those four files — per FR-013 (partial)

