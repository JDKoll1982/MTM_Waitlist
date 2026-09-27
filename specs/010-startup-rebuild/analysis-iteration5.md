# Specification Analysis Report: 010-startup-rebuild

**Feature**: `specs/010-startup-rebuild` | **Date**: 2026-09-26 | **Constitution**: v1.2.0 (ratified 2026-09-09, last amended 2026-09-14) — and `plan.md`'s Constitution Check opens with "Gate assessed against the constitution at v1.2.0", which matches the constitution's own version line exactly.

## Specification Analysis Report

| ID | Category | Severity | Location(s) | Summary | Recommendation |
|----|----------|----------|-------------|---------|----------------|
| CG1 | Coverage Gap | HIGH | `MTM_Waitlist.Tests/Module_Settings/Services/UserManagementLiveIntegrationTests.cs`:13, :324 · `tasks.md`:107 (T015), :114 (T020), :599 (T185) | A consumer of the removed surface is owned by no task. Line 13 imports `MTM_Waitlist.Module_Startup.Services` and line 324 constructs `StartupSessionRepository`, a type that lives in `MTM_Waitlist.Startup` and is emptied out by T015, and whose six procedures (`sp_server_utc_now_get`, `sp_auth_credentials_check`, `sp_auth_user_password_update`, `sp_auth_computer_registered_get`, `sp_auth_session_expiry_get`, `sp_auth_password_reset_required_get`) T020 deletes. T015's own checkpoint is a green Foundational build, so this file strands that gate — exactly the class the Foundational addendum exists to prevent. Related: T185 claims `ShellPage.xaml.cs` is "the only place outside the startup module that names `MTM_Waitlist.Module_Startup.Services`"; ten other files name it. | Add a task beside T185–T187 (Foundational W4 or US3) that re-points this file onto the rebuilt session service or removes its session case, and correct T185's uniqueness claim to "the only production place outside the module". |
| CA1 | Constitution Alignment | MEDIUM | `plan.md`:245–249 · `tasks.md`:114 (T020, Foundational W5), :174 (T056, Foundational W17) | The destructive-operation clause is now addressed rather than silently passed, but the approval does not precede every deletion. `plan.md` asserts "T056 records the explicit approval for the deletions before they run", yet the nine startup-only procedures are dropped by T020 in Foundational W5 while the recorded approval (T056) is obtained in W17. Nine of the ten procedure deletions therefore have no recorded confirmation ahead of them, though they are checklist-gated and ship `rollback.sql`. | Either split the approval so the procedure deletions are covered before T020, or narrow the plan's sentence to the deletions T056 actually precedes. |
| UN1 | Underspecification | MEDIUM | `tasks.md`:47 (T005) · `plan.md`:105, :135–137 · existing `MTM_Waitlist.Startup/ViewModels/SplashViewModel.cs`, `…/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs` | T005 requires that "no type remains in the retired `MTM_Waitlist.Module_Startup.Services` and `MTM_Waitlist.Module_Startup.ViewModels` namespaces", but no artifact states which namespaces the rebuilt module will use, and the rebuild is delivered at the paths that carry those namespaces today: T087 rebuilds `MTM_Waitlist.Startup/ViewModels/SplashViewModel.cs`, which declares `namespace MTM_Waitlist.Module_Startup.ViewModels`, and the DI extension keeps `…Module_Startup.Services.DependencyInjection`. If the rebuilt types keep those namespaces, the guard assertion is contradicted by the delivery and T169/T170 stay unreachable — the I40 defect relocated from `.Views` to `.Services`/`.ViewModels`. | State the rebuilt module's namespace in `plan.md` and the module guide. If it changes, name the new one; if it does not, re-word T005 to assert the *removed types* are gone rather than their namespaces. |
| AM1 | Ambiguity | LOW | `contracts/sql-contracts.md`:233 | The I41 fix ends with "per section 5's table", but inside this file §5 is "Artifacts that must not be deleted" and holds no table; the four-key role table is in `contracts/machine-configuration-contract.md` §5. | Name the file: "per `machine-configuration-contract.md` §5". |
| IN1 | Inconsistency | LOW | `data-model.md`:427 (E10) | E10 still says the never-delete set ends with "the five seeds and aggregates". Every other artifact now says three seeds: checklist Phase 0 line 18 "3 shared seeds with the 5 aggregate and descriptions files", `tasks.md`:14 and :122 (T024) "the three seeds", `plan.md`'s scale note, and brief S3.2, which lists exactly three seed files. | Replace with "the three seeds and the five aggregate and descriptions files". |
| IN2 | Inconsistency | LOW | `tasks.md`:587 (addendum) · `tasks.md`:652 (Dependencies & Execution Order) | The addendum places T185/T186 "in US3 beside T111" (US3 Wave 1), while the D&EO puts them in US3 Wave 3: "W3 … the registration, with the shell's sign-out re-point (T185–T186)". T187's half ("Foundational W4 beside T015") agrees with the wave line and is fine. | Make the addendum name Wave 3 (or move the wave-line mention), so the two statements agree. |
| IN3 | Inconsistency | LOW | `plan.md`:49 | "the four decisions it produced are D25 to D29" — D25–D29 is five identifiers. The reference's four are D25–D28, and D29 is the owner's panel-copy decision recorded in the separate US5 addendum. | Say "D25 to D28, with D29 added at the owner's direction". |
| IN4 | Inconsistency | LOW | `STARTUP-REBUILD-CHECKLIST.md`:20 | Phase 0's evidence line records "verified 2026-09-25: 20 decisions across three question rounds", while brief S2 holds 24 numbered decisions and `research.md`:4 says the brief "locks twenty-four decisions in its S2". | Update the count, or date-stamp it as the count at the moment it was recorded. |
| US1 | Underspecification | LOW | `contracts/logging-contract.md`:227 · `tasks.md`:583 (T183), :585 (T184) | The panel's copy is "cut with an explicit marker at a stated ceiling", but no artifact ever states the ceiling — the reference document says "a stated ceiling" too. T183/T184 therefore cannot be verified against a number. | State the ceiling (bytes or characters) in the logging contract and cite it from T183. |

## Prior Findings Verification

**C1 — CLOSED (with the CA1 caveat).** `plan.md`:245 now carries "**Second clause, checked and not deviated from.** The same constraint requires that a destructive operation — a database restore or a bulk delete — be explicitly confirmed", and :249 "T056 records the explicit approval for the deletions before they run. That is why the row above is not a second deviation." The Security & Secrets row says "**PASS with one recorded deviation.** … The constraint's other clause … is addressed in the same place and is **not** deviated from." T056 (`tasks.md`:174) reads "obtain the recorded approval for the deletions that follow — the Security & Secrets constraint requires a destructive database operation to be explicitly confirmed". The clause is genuinely addressed; only its *sequencing* relative to T020 is imperfect (see CA1).

**I40 — STILL OPEN (half closed).** The folder claim is fixed: T005 (`tasks.md`:47) now says "Assert the removed view files are gone — not that the folder is empty, because the rebuilt surfaces live under `Module_Startup/Views` … Assert the rebuilt view set positively instead of by absence", and checklist 1.1c matches ("Assert the removed view files are gone and no type remains in the retired `…Module_Startup.Services` or `…Module_Startup.ViewModels` namespaces — not that the views folder is empty"). The namespace claim is not: it moved to `…Module_Startup.Services`/`…Module_Startup.ViewModels`, which the rebuilt services and view models occupy under the delivered paths, and no artifact states a namespace change — see UN1.

**I41 — CLOSED.** `contracts/sql-contracts.md`:233 "A fresh store must grant `IT Department` and `Developer` three of the four keys, and `Developer` alone the log-panel key"; checklist 7.4 "a fresh store grants IT Department and Developer three of the four keys and Developer alone the log-panel key". Both now agree with `machine-configuration-contract.md` §5 and `log_panel` = `Developer` alone. (The new cross-reference is loose — AM1.)

**I42 — CLOSED.** `tasks.md`:14 now enumerates: "the five tables, the eleven procedures plus the retained `fn_server_utc_now`, the three seeds and the five aggregate and descriptions files T024 enumerates", and T024 (`:122`) repeats the same arithmetic ("the eleven procedures and the retained `fn_server_utc_now` (twelve data operations in all)"). The underived "four shared database artifacts" count is gone.

**I43 — STILL OPEN (half closed).** The placement now follows the deliberate-deferral pattern and no longer sits "beside T010 and T012": T010 keeps the four listings standing ("`ISignOutService`, `SignOutResult` and `IAppProcessRestarter` until US3 replaces them (T185, T186) … and `IComputerRegistryService` until its consumers are settled (T187)"), T015 hands `ComputerRegistryService` to T187, and the D&EO paragraph agrees. The residual is IN2: the addendum says "beside T111" (US3 W1) while the wave line says W3.

**I44 — STILL OPEN (half closed).** Checklist Phase 0 line 18 now reads "3 shared seeds with the 5 aggregate and descriptions files"; `tasks.md`:14 and T024 agree; `plan.md`'s scale note says "three shared seeds, and the five aggregate and descriptions files". `data-model.md`:427 still says "the five seeds and aggregates" — see IN1.

**I45 — REJECTED, and the rejection is correct.** My own search of the tree for an exact `^using Microsoft\.Extensions\.Logging;\s*$` line returns **31 files**; a substring search for `Microsoft.Extensions.Logging` returns **56 files**. `contracts/logging-contract.md` §4's "31 files import `Microsoft.Extensions.Logging`" is therefore the exact-import figure and is accurate; the 56 figure counted files merely containing the substring. (The same block re-derives as 219 `_logger.` call sites in 23 files plus 7 standalone `logger.`/`Logger.` logging calls in 3 files = 226 across 26 files, of which Mock.Service holds 58 + 6 = 64, leaving the application's own 162. A raw regex for `logger.`/`Logger.` finds 12 matches, the extra five being three `logger.IsEnabled(...)` calls in `ServiceLogTests.cs` and two `<param name="logger">Logger.</param>` doc comments — neither is a log-writing call site.)

**I46 — CLOSED.** T031: "Re-pin the page-activation exemption list to the placeholder's page set, which is all this phase can pin; the list is re-pinned to the rebuilt page set when the placeholder is retired (T094)". T094: "Retire the placeholder window and its code-behind once the splash is the launch surface, and re-pin the page-activation exemption list to the rebuilt page set in the same pass, which is the second re-pin T031 owed (the Phase 2 hand-off)."

**I47 — CLOSED.** The addendum's **Files** list is now the union of T185–T187's paths plus `research.md` (`ViewModels/ShellViewModel.cs`, `Module_Core/Views/ShellPage.xaml.cs`, `MTM_Waitlist.Tests/Module_Core/ViewModels/ShellViewModelTests.cs`, `MTM_Waitlist.Startup/Services/`, `MTM_Waitlist.Core/Contracts/Services/IComputerRegistryService.cs`, `MTM_Waitlist.Settings/ViewModels/ComputerManagementViewModel.cs`, `MTM_Waitlist.Settings/ViewModels/ComputerEditDialogViewModel.cs`, `MTM_Waitlist.Tests/Module_Settings/SettingsViewModelTests.cs`, `research.md`), and the US3 wave line reads "with the shell's sign-out re-point (T185–T186)" with no count.

**I48 — CLOSED.** `plan.md`:135 now reads "└── SplashView, LoginWindow, LoginPage # DELETED — the old window and page pairs. SplashWindow and SplashPage are REBUILT in place, and SignInWindow replaces LoginWindow", and `SplashWindow.xaml, SplashPage.xaml` is its own "REBUILT" entry. The line is a tree entry at column 0 inside the block's fence pair (four fences in `plan.md`, balanced), not a table row.

**I49 — CLOSED, and not half-corrected.** `tasks.md` Phase 2's Files list now says "the `MTM_Waitlist.Core/Activation/` folder" (T011 likewise: "`MTM_Waitlist.Core/Services/ActivationService.cs`, `MTM_Waitlist.Core/Activation/`"), and the directory exists. A search of `tasks.md` for `MTM_waitlist` returns **0** hits, so the database name was not left in either spelling.

**D1 — CLOSED.** T065 (`tasks.md`:254) now owns the clause: "…absorb the writer's own failures so no failure to record produces a second diagnostic — the second-diagnostic clause is this task's; T179 adds only the no-substitute-record and no-added-wait obligations on the same file"; T179 (`:553`) carries only "the two clauses T065 does not carry: no local substitute record, and no added wait for the caller whose fault is being recorded". No duplication remains, and the pair covers all four bullets of `logging-contract.md` §1.4.

**G8 — CLOSED.** FR-022 is no longer the only un-cited FR: T035 reads "Define the person identity contract: read-only, no writer but the launch pipeline … (checklist 4.1a, plan D14, FR-022)" and T036 "(checklist 4.1b, plan D14, FR-022)". Every FR-001…FR-038 now appears in at least one task.

**Coverage Summary:**

| Requirement Key | Has Task? | Task IDs | Notes |
|-----------------|-----------|----------|-------|
| FR-001 terminal outcomes | Yes | T077, T170 | `LaunchOutcome`, five outcomes |
| FR-002 name work before running it | Yes | T071–T073, T076, T080, T085, T087, T116 | |
| FR-003 stated maximum, no unbounded wait | Yes | T071, T073, T074, T080, T084 | 30-second ceiling in T074 |
| FR-004 cause specific to the stop | Yes | T085, T087, T124 | |
| FR-005 error strip at the bottom | Yes | T090 | |
| FR-006 unconfigured never reaches main screens | Yes | T075, T077, T096, T103 | |
| FR-007 setup authorised by two roles only | Yes | T095, T099, T100 | |
| FR-008 every non-completion route ends the app | Yes | T078, T096, T102, T127, T174 | |
| FR-009 removed/revoked/unreadable = unconfigured | Yes | T075, T097, T101 | |
| FR-010 store clock decides | Yes | T044, T107, T111 | |
| FR-011 sign-out clears session first | Yes | T044, T107, T111, T172, T174 | |
| FR-012 five-attempt limit survives restart | Yes | T106, T110, T173 | |
| FR-013 password change only after acceptance | Yes | T115, T173 | |
| FR-014 remembered sign-in in store, encrypted, fallback | Yes | T108, T113, T117 | |
| FR-015 unreadable hardware identity admitted | Yes | T112 | |
| FR-016 offer to repeat the failed work | Yes | T121, T125 | |
| FR-017 reset only where it could help | Yes | T119, T122, T124 | |
| FR-018 reset states what it resets, machine-only | Yes | T075, T120, T122, T123, T126 | |
| FR-019 silent repair | Yes | T121, T123 | |
| FR-020 retry repeats only that work and after | Yes | T077, T082, T121, T125 | |
| FR-021 every diagnostic reaches the store | Yes | T064 | |
| FR-022 identity/machine facts read-only | Yes | T035, T036 | closed by G8's fix |
| FR-023 preferences held against the person | Yes | T138, T140, T141, T143, T144, T145 | |
| FR-024 plant-wide list, two roles may change it | Yes | T137, T142, T146 | |
| FR-025 nothing local but store access | Yes | T155, T156 | plus T150–T154, T157 deletions |
| FR-026 picture copy best effort | Yes | T081, T083, T088 | |
| FR-027 verdict settled before the shell | Yes | T084, T089 | |
| FR-028 removed launch behaviour cannot return | Yes | T169 | with T001, T004, T005 |
| FR-029 session length changeable by two roles | Yes | T137, T147 | |
| FR-030 role restrictions preserved | Yes | T139, T149 | |
| FR-031 sweeps recorded; no case dropped silently | Yes | T159, T160, T167, T171 | T159/T167 own the S13 walk |
| FR-032 fault recorded whole, with chain | Yes | T047, T175, T180 | |
| FR-033 stable fingerprint | Yes | T047, T176, T181 | |
| FR-034 version and runtime context gathered by the seam | Yes | T177, T182 | |
| FR-035 action and view context | Yes | T064, T177 | |
| FR-036 provider diagnostics, no values/credentials | Yes | T177, T178, T182 | |
| FR-037 recording must not hide the fault | Yes | T065, T179, T180 | |
| FR-038 the panel's copy | Yes | T131, T170, T183, T184 | |
| SC-001 no silent stop | Yes | T168, T170, T174 | |
| SC-002 no wait exceeds its maximum | Yes | T074, T168 | |
| SC-003 released build records an entry | Yes | T130 | |
| SC-004 zero input sequences reach the shell | Yes | T096, T161 | |
| SC-005 dev finds a machine's history in under a minute | Yes | T129 | |
| SC-006 every tested abort route ends the app | Yes | T096 | |
| SC-007 five-attempt limit across restart | Yes | T106 | |
| SC-008 no preference lost across computers | Yes | T138 | |
| SC-009 build fails when old behaviour returns | Yes | T169 | |
| SC-010 no remedy that cannot remove the cause | Yes | T119 | |
| SC-011 session-length change takes effect next sign-in | Yes | T111 | with T147 |
| SC-012 role-restricted settings unchanged | Yes | T137, T139, T149 | |
| SC-013 acceptance surface fully accounted for | Yes | T159, T160, T167, T171 | |
| SC-014 fingerprint groups repeats | Yes | T049, T131, T181 | |
| SC-015 store-fault diagnostics carry no values | Yes | T182 | |
| SC-016 nothing local on a store outage | Yes | T130, T182 | |
| SC-017 copy pastes whole, writes nothing | Yes | T170, T184 | |

## Constitution Alignment Issues

- **CA1 (MEDIUM)** — the destructive-operation clause of *Security & Secrets* is addressed, not silently passed, but its approval lands after the first destructive step (T020 in Foundational W5, T056 in W17). This is the only constitution-relevant finding. No constraint in the Constitution Check asserts an unqualified PASS over a deviation: Security & Secrets says "PASS with one recorded deviation", the design-invariants row says "PASS, with one deliberate reversal recorded", and both deviations are recorded in `### Complexity Tracking` with a Why and rejected alternatives, satisfying the Review rule ("a deviation MUST be recorded in `plan.md` → Complexity Tracking with its rationale").
- *Complexity Tracking* is structurally sound: one deviation row (AES-256 remembered sign-in instead of DPAPI `CurrentUser`), the "**Second clause, checked and not deviated from**" paragraph, the two named plan-level resolutions, and three dated re-check paragraphs, all under the `### Complexity Tracking` heading.
- Out of scope but worth naming: `.github/instructions/spec-kit.instructions.md` still says the constitution "is ratified at v1.1.1 (last amended 2026-09-10)", while `.specify/memory/constitution.md` reads "**Version**: 1.2.0 | **Ratified**: 2026-09-09 | **Last Amended**: 2026-09-14" — and v1.2.0's Sync Impact Report claims propagation to that file. The feature artifacts themselves cite v1.2.0 correctly.

## Unmapped Tasks

None in the strict sense: every one of the 38 FRs and 17 SCs is cited by at least one task, and every task sits inside a story phase or a documented foundational phase. For completeness, 71 tasks carry neither an `FR-###`/`SC-###` citation nor a `[US#]` tag; they are the Phase 1/2 removal, store-foundation and Phase 9 close-out tasks (T001–T034, T037–T063, T066–T070, T079, T150, T162–T166, T187), and each cites a checklist reference (`checklist 2.4a`, `S3.1`, …) inside a phase whose story or checkpoint owns it.

## Metrics

- Total Requirements: **55** (38 FR + 17 SC)
- Total Tasks: **187** (T001–T187; contiguous, no gaps, no duplicates)
- Coverage %: **100%** (55/55 requirements cited by ≥1 task)
- Ambiguity Count: **1**
- Duplication Count: **0**
- Coverage-gap Count: **1**
- Inconsistency Count: **4**
- Underspecification Count: **2**
- Constitution-alignment Count: **1**
- Critical Issues Count: **0**
- Severity split: 1 HIGH, 2 MEDIUM, 6 LOW (9 findings)

**Re-derived counts (read-only, this pass):**

| Figure | Search | Result | Expected |
|---|---|---|---|
| Tasks | `^- \[ \] \*\*T(\d{3})\*\*` over `tasks.md` | 187, min T001, max T187, 187 distinct, no missing | 187, T001–T187 contiguous — match |
| Checklist | `^- \[[ x]\]`, `^## Phase `, `\*\*GATE:` | 181 checkboxes / 11 phases / 11 gates (176 open, 5 closed) | 181/11/11 — match |
| Requirements | `\*\*FR-\d{3}\*\*:` / `\*\*SC-\d{3}\*\*:` in `spec.md` | 38 FR / 17 SC | 38/17 — match |
| Edge cases | bullets between `## Edge Cases` and `## Requirements` | 23 | 23 — match |
| Tooltip keys | `name="Startup_` in each `.resw` | 15 in `TooltipResources.resw`, 15 in `TooltipResources.developer.resw` | 15 each — match |
| `StartupDebugLog` | token over all `.cs` (excl. `bin`/`obj`) | 493 token hits in 86 files, of which **489 call sites (335 `Info`, 147 `Error`, 7 `Configure`) in 83 files**; the 3 leftover files are `StartupDebugLog.cs` itself and the two doc-comment mentions T069 names | 489 across 83 — match |
| `ILogger` split | `_logger\.` and standalone `logger.`/`Logger.` | 219 `_logger.` in 23 files + 7 standalone logging calls in 3 files = 226 across 26 files; 64 in `Mock.Service` (58 + 6); app's own 162 | 226/219/7/64/162 — match |
| Exact imports | `^using Microsoft\.Extensions\.Logging;\s*$` | **31 files** | 31 — match (56 is the substring count) |
| Local-state arithmetic | migration map §1A/§1B/§5 | 6 + 9 − 4 = 11 deleted (§1A ∪ §1B), 11 kept; 10 doubles across the 11 kept files, 5 replaced + 5 deleted, 4 already dead | 11/11 — match |
| Database arithmetic | T020 (9) + T061 (1) procedures; T060+T061 (3) tables; T044 (4) + T046 (3) + T048 (1) + T049 (2) procedures; T044+T046 tables; T047 repurposed | 2 + 10 + 1 + 3 + 10 = 26 | 26 — match |
| Cited paths | every backticked path in `tasks.md` | all existing deletion targets and kept files resolve; every miss is a file or folder the feature creates (`MTM_Waitlist.Logging/`, `MTM_Waitlist.Startup/Options/`, `MTM_Waitlist.Tests/Module_Startup/ViewModels/`, `…/EdgeCases/`, `capabilities/startup-launch/`, the four output docs); `living-specs.yml` is tracked and deleted in the working tree, which T163 handles | — |
| T185 claim | `MTM_Waitlist.Module_Startup.Services` over non-startup `.cs` | 11 files name it (ShellPage.xaml.cs, 8 test files, 2 DI files) | claim of "the only place" — refuted (CG1) |

## Next Actions

- **CG1 is the one thing to fix before `/speckit.implement`.** It is not a critical constitution issue, but it is build-breaking at the Foundational gate and it is invisible to every task as written. Add the task (beside T185–T187 is the natural home) or accept that the implementer will meet an unexplained compile error.
- **CA1 and UN1 are one-edit fixes.** Move or split T056's approval so it precedes T020, and state the rebuilt module's namespace so T005's assertion is verifiable — the second one decides whether T169/T170 are reachable at all.
- **The four LOW inconsistencies (AM1, IN1–IN4) do not block implementation** and can be corrected in place; IN1 and IN2 are the residue of the I44 and I43 fixes and are best closed in the same pass so the fifth round has nothing to reopen.
- **US1 (the copy's ceiling) needs a number** before T183/T184 can be verified; it needs no design decision beyond picking a limit.
- No CRITICAL issues exist, the constitution's Review rule is satisfied, and 9 of the 13 prior findings are genuinely closed — the artifact set is consistent enough to proceed once CG1 is scheduled.

## Machine summary

CG1|HIGH|Coverage Gap|MTM_Waitlist.Tests/Module_Settings/Services/UserManagementLiveIntegrationTests.cs:13,324 (T015/T020/T185)|An unowned consumer constructs the removed `StartupSessionRepository`, stranding the Foundational build gate, and T185's "only place" claim is refuted by ten other files.
UN1|MEDIUM|Underspecification|tasks.md:47 (T005); plan.md:105,135-137|T005 declares `…Module_Startup.Services`/`…ViewModels` retired and empty, but the rebuilt types are delivered at the paths that carry those namespaces and no artifact states a change, so the guard assertion stays contradicted.
CA1|MEDIUM|Constitution Alignment|plan.md:245-249; tasks.md:114,174|The destructive-operation clause is addressed, but T056's recorded approval (Foundational W17) follows the nine procedure deletions T020 performs in W5, so the plan's "before they run" is inaccurate for them.
AM1|LOW|Ambiguity|contracts/sql-contracts.md:233|"per section 5's table" points at a section with no table in that file; the table is in machine-configuration-contract.md §5.
IN1|LOW|Inconsistency|data-model.md:427|E10 still says "the five seeds and aggregates" where every other artifact says three seeds and five aggregate and descriptions files.
IN2|LOW|Inconsistency|tasks.md:587,652|The addendum places T185/T186 "in US3 beside T111" (Wave 1) while the Dependencies & Execution Order line places them in US3 Wave 3.
IN3|LOW|Inconsistency|plan.md:49|"the four decisions it produced are D25 to D29" — five identifiers for four decisions; D29 is the owner's separate copy decision.
IN4|LOW|Inconsistency|STARTUP-REBUILD-CHECKLIST.md:20|Phase 0 records "20 decisions" while brief S2 holds 24 numbered decisions and research.md says twenty-four.
US1|LOW|Underspecification|contracts/logging-contract.md:227; tasks.md:583,585|The panel's copy is cut "at a stated ceiling" that no artifact states, so T183/T184 cannot be checked against a number.

COUNTS|critical=0|high=1|medium=2|low=6|total=9