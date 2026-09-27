## Specification Analysis Report

**Feature**: `010-startup-rebuild` · **Date**: 2026-09-26 · **Constitution**: v1.2.0 (ratified 2026-09-09, last amended 2026-09-14) — version string in `.specify/memory/constitution.md` verified

| ID | Category | Severity | Location(s) | Summary | Recommendation |
|----|----------|----------|-------------|---------|----------------|
| C1 | Constitution | CRITICAL | `plan.md:243` (Security & Secrets row), `tasks.md:120-121,206-208` (T020/T021/T061/T062/T063) | The Security & Secrets clause "A destructive operation (database restore, bulk delete) MUST require explicit confirmation" is never addressed, yet the feature drops 3 tables and 10 procedures (`Database/Tables/05,06,07`, nine startup-only SPs plus `sp_core_buildings_upsert`) and applies that to a **live** store at T034/T063. `plan.md`'s Constitution Check records only the DPAPI deviation and gives Security & Secrets a "PASS with one recorded deviation" — an unqualified PASS for a second, unrecorded deviation. | Add one confirmation/approval task (or a recorded line stating the clause is scoped to runtime operations) to `plan.md` → Complexity Tracking and `research.md`, then reference it from T063 |
| I40 | Inconsistency | HIGH | `tasks.md:45` (T005), `STARTUP-REBUILD-CHECKLIST.md:36`; contradicted by `plan.md:131-141`, `STARTUP-REBUILD-BRIEF.md:21,51,95`, `tasks.md:129,227-228,250,252,267,308,349` | T005 pins "Assert **no file remains under `Module_Startup/Views`**" into `RetiredSymbolAuditTests`, but the target architecture keeps its views there: brief S2 decision 1 says "views stay in `Module_Startup/Views`", the plan calls the folder "DELETED, then REPLACED with the new surfaces", and T029/T086/T092/T098/T114/T124 build `StartupPlaceholderWindow`, `SplashWindow`, `SplashPage`, `SignInWindow`, `MachineSetupWindow`, `BlockedStateWindow` inside it. No later task relaxes the assertion, so T169 ("run the retired-symbol audit one final time") and T170 (clean build + suite) cannot both pass. The second clause has the same exposure: existing views declare `namespace MTM_Waitlist.Module_Startup.Views` (`SplashWindow.xaml.cs:8`, `LoginPage.xaml.cs:9`), which sits inside `MTM_Waitlist.Module_Startup`. | Narrow T005 (and checklist 1.1c) to the removed artefacts — the five old file names and the old `…Module_Startup.Services`/`…ViewModels` namespaces — and assert the *rebuilt* view set positively, so the final gate stays reachable |
| I41 | Inconsistency | MEDIUM | `contracts/sql-contracts.md:232`, `STARTUP-REBUILD-CHECKLIST.md:337`; contradicted by `contracts/sql-contracts.md:222-225`, `contracts/machine-configuration-contract.md:120-126`, `research.md` D11, `tasks.md:167` (T053) | "A fresh store must grant `IT Department` and `Developer` the four keys" (sql-contracts §6) and "a fresh store grants IT Department and Developer the four keys" (checklist 7.4) contradict the table three lines above them and D11: `permission.settings.log_panel` is `Developer 1, every other role 0`. T053 states the correct split (three keys to both roles, `log_panel` to `Developer` alone). | Rewrite both sentences as "grants `IT Department` and `Developer` three keys and `Developer` alone the log-panel key", matching T053 |
| I42 | Inconsistency | MEDIUM | `tasks.md:13` | The preamble still says "**the four shared database artifacts** that must never be deleted by the dead-weight audit" — the identical underived count that finding A3 fixed in `plan.md`. It contradicts the enumeration T024 carries and the plan's Scale note: five shared tables, eleven shared procedures plus `fn_server_utc_now`, three shared seeds, five aggregates and the descriptions file. | Replace the phrase with the T024 enumeration, exactly as `plan.md`'s Scale note now does |
| I43 | Inconsistency | MEDIUM | `tasks.md:584` (Foundational addendum), `tasks.md:625`, versus the Phase 2 wave body at `tasks.md:69-84` | The addendum states T185–T187 "execute in Foundational W2, **beside T010 and T012**", but T012 is in Wave 3, not W2. Worse, T185 says "before they go" and T187 says "Settle … before T010 deletes it" while sharing T010's wave — under the file's own convention that a wave completes before the next begins, a peer placement cannot guarantee the stated precondition. | Place T185–T187 in Phase 2 **Wave 1** (or state explicitly that they run before T010 inside W2) and drop the T012 reference |
| I44 | Inconsistency | LOW | `STARTUP-REBUILD-CHECKLIST.md:19`, `data-model.md:428`; versus `STARTUP-REBUILD-BRIEF.md:60-63`, `plan.md:13`, `tasks.md:102` (T024) | Protected-seed counts drift: the checklist's Phase 0 evidence records "12 shared procedures, 5 shared tables and **4 shared seeds** flagged"; data-model E10 says "…and **the five seeds and aggregates**"; brief S3.2 lists three seeds (`seed_dev_masked_baseline`, `seed_username_upper_normalization`, `seed_role_admin_to_it_department`) plus five files, and plan/T024 both say three seeds. | State "three shared seeds and the five aggregate/descriptions files" in the checklist and data-model, or name the fourth seed if `seed_permission_role_baselines` is meant |
| I45 | Inconsistency | LOW | `contracts/logging-contract.md:145-146` ("226 call sites across 26 files … and 31 files import `Microsoft.Extensions.Logging`") | The `ILogger` figures reproduce exactly (226 = 219 `_logger.` + 7 standalone; 26 files; 64 in `MTM_Waitlist.Mock.Service` = 58 + 6; 162 app-side), but the import figure does not: a tree-wide search finds **56** files importing `Microsoft.Extensions.Logging` (14 `Mock.Service` + 15 `Settings` + 27 tests), and neither "non-test" (29) nor "excluding Mock.Service" (42) yields 31. | Correct the figure to 56, or scope the sentence (for example "29 production files") |
| I46 | Inconsistency | LOW | `plan.md:213` ("exemption list re-pinned to the new page set") versus `tasks.md:132` (T031, "the placeholder's page set") | The only re-pin task targets the **placeholder's** page set, which Phase 3 retires (T094); the plan claims the list is re-pinned to the **new** page set. `PageActivationAuditTests` enumerates page classes under the Views folders with a pinned exempt list (`ShellPage` today), so a rebuilt `SplashPage` taking a view-model parameter would fail the audit with no task owning a second re-pin. | Re-pin once, at close-out (or in T092), against the final page set, and align the plan's wording |
| I47 | Inconsistency | LOW | `tasks.md:590-591` (addendum `**Files**`), against `tasks.md:603-613` (T186/T187), `tasks.md:625` | The addendum's Files list omits two paths its own tasks name — `MTM_Waitlist.Core/Contracts/Services/IComputerRegistryService.cs` (T187) and `MTM_Waitlist.Startup/Services/` (T186) — and omits `research.md`, which T187 is told to record into. It also adds nothing extra. The Dependencies W2 line says "the **two** consumers they strand (T185–T187)", attaching a count of two to three identifiers. | Extend the Files list to the union of T185–T187's paths (plus `research.md`) and say "the consumers they strand (T185–T187)" |
| I48 | Inconsistency | LOW | `plan.md:141` ("the four old window/page pairs # DELETED") | The count matches neither the tree nor the deletion set: `Module_Startup/Views` holds **five** old XAML pairs (`SplashWindow`, `SplashView`, `SplashPage`, `LoginWindow`, `LoginPage` — all verified present), two of which (`SplashWindow`, `SplashPage`) the plan marks REBUILT and one (`LoginWindow`) is replaced by `SignInWindow`, leaving **three** deleted outright (`SplashView`, `LoginWindow`, `LoginPage`). | Name the three deleted files instead of a count |
| I49 | Inconsistency | LOW | `tasks.md:74` | Phase 2's Files list writes "the `MTM_waitlist.Core` activation folder" — the database spelling (`mtm_waitlist`) in a code-path position; T011 and the plan use `MTM_Waitlist.Core/Activation/`. | Correct to `MTM_Waitlist.Core/Activation/` |
| D1 | Duplication | LOW | `tasks.md:186` (T065) and `tasks.md:551` (T179), both `MTM_Waitlist.Logging/StoreLogWriter.cs`, both FR-037 | T065 already requires "absorb the writer's own failures so no failure to record produces a second diagnostic"; T179 re-states the same clause for the same file and adds "no local substitute record, and no added wait". Two tasks, one clause, one file. | Fold T179's first clause into T065 or state explicitly that T179 adds only the substitute-record and no-added-wait obligations |
| G8 | Coverage | LOW | `spec.md` FR-022; `tasks.md` | FR-022 ("person's identity and the machine's facts … for reading only") is the **only** FR with no explicit reference anywhere in `tasks.md`; it is satisfied by inference by T035/T036 ("read-only, no writer but the launch pipeline") and by `contracts/identity-contracts.md`. Not a real gap, but the traceability is implicit. | Add `FR-022` to T035 and T036 |

### Prior Findings Verification

- **G5 — CLOSED.** `tasks.md:593` **T185** exists and names `ViewModels/ShellViewModel.cs`, `Module_Core/Views/ShellPage.xaml.cs`, `MTM_Waitlist.Tests/Module_Core/ViewModels/ShellViewModelTests.cs`; `tasks.md:600` **T186** provides the process-restart path. Premise verified in the current tree: `ShellViewModel.cs:29` `private readonly ISignOutService _signOutService;`, `:338` `public Task<SignOutResult> SignOutAsync(CancellationToken cancellationToken = default)`, and `ShellPage.xaml.cs:207` reads verbatim `result = SignOutResult.Refused(MTM_Waitlist.Module_Startup.Services.SignOutService.ResolveRestartFailedMessage());` — the only `MTM_Waitlist.Module_Startup` namespace use outside the startup module. All three files are in Phase 2's Files list (`tasks.md:75-77`) and the W2 wave line names T185–T187 (`tasks.md:625`). Residual placement caveat raised separately as I43.
- **G6 — CLOSED.** `tasks.md:603` **T187** exists, settles the interface's fate before T010 deletes it, and names `ComputerManagementViewModel.cs`, `ComputerEditDialogViewModel.cs`, `SettingsViewModelTests.cs` **and** `IComputerRegistryService.cs`. Premise verified: `MTM_Waitlist.Tests/Module_Settings/SettingsViewModelTests.cs:832` `private sealed class FakeComputerRegistryService : IComputerRegistryService`, and both Settings view models reference the interface. `plan.md:127-129` no longer lists it as unambiguously DELETED and instead says "the Settings computer screens still inject it, so its fate is settled before deletion (T187)".
- **G7 — CLOSED.** `tasks.md:210` T069 names "the two doc-comment mentions a member-call script cannot see, `MTM_Waitlist.Mock.Service/Services/ServiceLog.cs:13` and `MTM_Waitlist.Tests/Module_Mock_Service/ServiceLogTests.cs:15`", and both files appear in its Files list. Verified: `ServiceLog.cs:13` carries `<see cref="MTM_Waitlist.Module_Core.Helpers.StartupDebugLog"/>`; `ServiceLogTests.cs:15` carries "The previous path was `<c>StartupDebugLog</c>`".
- **U5 — CLOSED.** T137 and Phase 8's Files entry both use `MTM_Waitlist.Tests/Module_Settings/SettingsViewModelTests.cs`, which exists; a search of `tasks.md` for `ViewModels/SettingsViewModelTests` returns **0** matches. `local-state-test-migration.md` §4 item 3 records the correction and why the coverage stays in one file.
- **I33 — CLOSED.** `STARTUP-REBUILD-CHECKLIST.md:342`: "**GATE: an unconfigured machine stops at the setup screen, it cannot be bypassed by any input or by any code path, and the four keys are seeded and enforced.**" The phase body, `plan.md` and both contracts all say four.
- **I34 — CLOSED.** `contracts/logging-contract.md` §3 now reads "matching the table's existing shape **plus the four new fields**" and then lists exactly four: `module`, `error_type`, `exception_detail`, `error_fingerprint`.
- **I35 — CLOSED.** `data-model.md` E4 now reads "that table is deleted in the same phase **(E10)**", and the removal table is E10.
- **I36 — CLOSED.** `tasks.md` T157: "ten doubles across eleven kept files, **five replaced and five deleted outright, four of them already dead**", matching `local-state-test-migration.md` §5 ("10 (5 replaced, 5 deleted outright)") and §4 (four named doubles "never constructed anywhere in the tree").
- **I37 — CLOSED.** The preamble now says "**most** tasks name their checklist reference (`checklist 2.4a`), and the ones added after the checklist was written name the decision or the artifact they come from instead". Derived: **39 of 187** task lines contain no `checklist` token, and those 39 include T171–T187 — exactly the set the narrowed wording allows.
- **I38 — REJECTED (agree: false positive).** `git ls-files` returns `STARTUP-FLOW.html`, `STARTUP-FLOW.md`, `STARTUP-FLOW.pdf` — all three tracked; `git status --porcelain` shows only ` D STARTUP-FLOW.html` and ` D STARTUP-FLOW.pdf` (working-tree deletions). T027's instruction ("all three are tracked, and the `.html` and `.pdf` are already deleted in the working tree, so this task commits the removal") is accurate. Do not re-raise.
- **I39 — CLOSED.** `local-state-test-migration.md` §2.6 now lists the remaining `RecordingLocalSettingsService` mentions as "… 533, 545, 575, **589, 609, 640**", including all three lines previously omitted.
- **A3 — CLOSED (for `plan.md`).** The Scale note now enumerates instead of asserting a count: "the list T024 carries and brief S3.2 enumerates: five shared tables, eleven shared procedures plus the retained `fn_server_utc_now`, three shared seeds, and the five aggregate and descriptions files." The same underived count survives in `tasks.md`'s own Scale note and is raised as **I42**, not as a re-opening of A3.

### Coverage Summary

| Requirement Key | Has Task? | Task IDs | Notes |
|-----------------|-----------|----------|-------|
| FR-001 (one terminal outcome) | Yes | T077, T170 | Explicit |
| FR-002 (name work before doing it) | Yes | T071, T072, T073, T076, T080, T085, T087, T116 | Explicit |
| FR-003 (every wait bounded) | Yes | T071, T073, T074, T080, T084 | Explicit |
| FR-004 (cause specific to the stop) | Yes | T085, T087, T124 | Explicit |
| FR-005 (bottom error strip) | Yes | T090 | Explicit |
| FR-006 (unconfigured never reaches shell) | Yes | T075, T077, T096, T103 | Explicit |
| FR-007 (two-role authority, no shell) | Yes | T095, T099, T100 | Explicit |
| FR-008 (every abort ends the process, reason first) | Yes | T078, T096, T102, T127, T174 | Explicit |
| FR-009 (removed/revoked/unreadable ⇒ unconfigured) | Yes | T075, T097, T101 | Explicit |
| FR-010 (store clock) | Yes | T044, T107, T111 | Explicit |
| FR-011 (session cleared before restart) | Yes | T044, T107, T111, T172, T174, T185, T186 | Explicit |
| FR-012 (five-attempt limit, survives restart) | Yes | T106, T110, T173 | Explicit |
| FR-013 (password change only after acceptance) | Yes | T115, T173 | Explicit |
| FR-014 (remembered sign-in in store, encrypted, fallback) | Yes | T108, T113, T117 | Explicit |
| FR-015 (unreadable hardware identity admitted) | Yes | T112 | Explicit |
| FR-016 (offer to repeat failed work) | Yes | T121, T125 | Explicit |
| FR-017 (reset only where it could help) | Yes | T119, T122, T124 | Explicit |
| FR-018 (preview, machine-only scope) | Yes | T075, T120, T122, T123, T126 | Explicit |
| FR-019 (silent repair) | Yes | T121, T123 | Explicit |
| FR-020 (repeat only failed work and after) | Yes | T077, T082, T121, T125 | Explicit |
| FR-021 (every diagnostic to the store) | Yes | T064 | Explicit; T047/T065/T067/T130/T182 support |
| FR-022 (identity/facts read-only; only launch writes) | **Inferred** | T035, T036 | **No explicit reference — see G8** |
| FR-023 (preferences held against the person) | Yes | T138, T140, T141, T142, T143, T144, T145 | Explicit |
| FR-024 (plant-wide list, two roles) | Yes | T137, T142, T146 | Explicit |
| FR-025 (nothing local but what reaches the store) | Yes | T155, T156 | Explicit |
| FR-026 (picture copy best effort) | Yes | T081, T083, T088 | Explicit |
| FR-027 (verdict settled before screens) | Yes | T084, T089 | Explicit |
| FR-028 (retired behaviour cannot return) | Yes | T169 | Explicit; guard built by T001/T004/T005 |
| FR-029 (session length, two roles) | Yes | T137, T147 | Explicit |
| FR-030 (existing role restrictions kept) | Yes | T139, T149 | Explicit |
| FR-031 (acceptance surface recorded and covered) | Yes | T159, T160, T167, T171 | Explicit |
| FR-032 (fault recorded whole incl. chain) | Yes | T047, T175, T180 | Explicit |
| FR-033 (stable fingerprint) | Yes | T047, T176, T181 | Explicit |
| FR-034 (gathered runtime context) | Yes | T177, T182 | Explicit |
| FR-035 (action and view context) | Yes | T064, T177 | Explicit |
| FR-036 (store diagnostics, no parameter values) | Yes | T178, T182 | Explicit |
| FR-037 (recording never hides the fault) | Yes | T065, T179, T180 | Explicit; T065/T179 overlap — see D1 |
| FR-038 (panel copy as pasteable text) | Yes | T131, T170, T183, T184 | Explicit |
| SC-001 (no silent stop) | Yes | T168, T170, T174, T185 | Explicit |
| SC-002 (no wait beyond its stated maximum) | Yes | T074, T168 | Explicit |
| SC-003 (release build records a launch) | Yes | T130 | Explicit |
| SC-004 (no input reaches the shell unconfigured) | Yes | T096, T161 | Explicit |
| SC-005 (filter by machine and error kind) | Yes | T129 | Explicit |
| SC-006 (every setup exit ends the app) | Yes | T096 | Explicit |
| SC-007 (limit survives restart) | Yes | T106 | Explicit |
| SC-008 (no preference lost across computers) | Yes | T138 | Explicit |
| SC-009 (build fails when behaviour returns) | Yes | T169 | Explicit |
| SC-010 (no remedy that could not help) | Yes | T119 | Explicit |
| SC-011 (session-length change at next sign-in) | Yes | T111 | Explicit |
| SC-012 (each restriction preserved) | Yes | T137, T139, T149 | Explicit |
| SC-013 (every case covered or retired with reason) | Yes | T159, T160, T167, T171 | Explicit |
| SC-014 (repeat faults group as one) | Yes | T049, T131, T181 | Explicit |
| SC-015 (provider code; no parameter values) | Yes | T182 | Explicit |
| SC-016 (outage recorded nowhere local, fault unchanged) | Yes | T130, T182 | Explicit |
| SC-017 (copy names type, chain, machine, action, link) | Yes | T170, T184 | Explicit |

### Constitution Alignment Issues

- **C1 (CRITICAL)** — the Additional Constraint **Security & Secrets** clause "A destructive operation (database restore, bulk delete) MUST require explicit confirmation" is unaddressed for the live removal of 3 tables and 10 procedures. `plan.md:243` gives Security & Secrets "PASS with one recorded deviation" and records only the DPAPI deviation, so the check reads as unqualified compliance for a second, unrecorded departure. Either satisfy the clause (a named confirmation step before T063, or a recorded owner approval) or record why the clause does not reach source-artifact schema removals.
- **DPAPI deviation — Review rule satisfied.** `plan.md` → `### Complexity Tracking` carries a well-formed four-column table with exactly one deviation row, naming the constraint verbatim ("service credentials MUST be stored DPAPI-protected (`ProtectedData`, `CurrentUser` scope)"), the reason, four rejected alternatives, and a pointer to `research.md` D12; `research.md` D12 carries the same recorded deviation. No other constraint row asserts an unqualified PASS where a deviation exists (External Integration, Documentation, Principles I–VII are all scoped or qualified).
- **Principle VI gates** are reproduced as concrete commands in `plan.md:230-236` and Phase checkpoints; **Principle III** is enforced by `InlineSqlAuditTests` plus `create.sql`/`rollback.sql` pairs (T044–T051, T057); **Principle V** obligations (localisation, `AutomationProperties.AutomationId`, no hardcoded pixel boundaries) are tasked in T018, T029, T098. No further alignment issues found.

### Unmapped Tasks

99 of the 187 tasks carry no `FR-###`/`SC-###` reference. All are process, removal, registration, aggregate or verification work whose mapping is to a phase checkpoint rather than to a requirement, and every one still names its checklist reference, its plan decision or its artefact: T001–T034 (guard, baseline, host/core/DB removal, placeholder), T035–T043 (identity contracts and `StartupState` migration, which carry FR-022 by inference), T045–T063 (new tables/procedures, permission keys, seeds, security review, aggregates, dead-weight deletions, validator), T066–T070 (provider registration, call-site migration, old type deletion, counts), T079, T086, T091–T094, T098, T104–T105, T109, T114, T118, T128 (host registrations and surface wiring), T132–T136 (panel gating and the deferred log path), T148, T150–T158 (panel hosting, local mechanism deletion, `appsettings.json`, test migration), T162–T166 (embed guide, living spec, changelog, README, UI-automation docs), T185–T187 (the new stranded-consumer tasks; T185/T186 are tagged `[US3]`). No task is unreachable from a story or a phase checkpoint.

### Metrics

- **Total Requirements**: 55 (FR-001–FR-038 = 38; SC-001–SC-017 = 17); contiguous, no missing identifiers
- **Total Tasks**: 187 (T001–T187, contiguous, no gaps, no duplicate identifiers; 187 checkbox lines)
- **Coverage %**: 100% (55/55) counting FR-022's inferred coverage by T035/T036; **98.2%** (54/55) on explicit in-task references
- **Ambiguity Count**: 0 (no unmeasurable adjectives in `spec.md`; the sweep's four unbounded waits are replaced by stated maxima; the only "minimal" is a plain description of the placeholder window)
- **Duplication Count**: 1 (D1)
- **Coverage-gap Count**: 1 (G8 — traceability only; no requirement is without a task)
- **Inconsistency Count**: 10 (I40–I49)
- **Underspecification Count**: 0 (independently raised; the two documentation-completeness rows are recorded as inconsistencies I47/I48)
- **Critical Issues Count**: 1 (C1)

**Re-derived counts (read-only searches, this working tree, 2026-09-26):**

| Claim | Expected | Derived | Verdict |
|---|---|---|---|
| Task identifiers | T001–T187 contiguous | 187 unique, min 1, max 187, 0 missing, 0 duplicates | ✅ |
| Checklist boxes / phases / gates | 181 / 11 / 11 | 181 (176 `[ ]` + 5 `[x]`) / 11 / 11 | ✅ |
| FR / SC | 38 / 17 | 38 / 17, both contiguous | ✅ |
| Edge Cases bullets | 23 | 23 | ✅ |
| `Startup_*` tooltip keys | 15 per tooltip file | 15 in `TooltipResources.resw`, 15 in `TooltipResources.developer.resw` | ✅ |
| `StartupDebugLog` call sites | 489 across 83 files | 489 across 83 (335 `Info`, 147 `Error`, 7 `Configure`) | ✅ |
| `ILogger` tree-wide | 226 / 219 `_logger.` / 7 standalone | 226 / 219 / 7 | ✅ |
| `ILogger` host split | 64 Mock.Service / 162 app-side | 64 (58 `_logger.` + 6 standalone) / 162 | ✅ |
| Files importing `Microsoft.Extensions.Logging` | 31 (contract §4) | 56 | ❌ → I45 |
| Local-state test arithmetic | 6 + 9 − 4 = 11 deleted, 11 kept | 6 / 9 / 11 distinct / 11 kept; 10 doubles (5 replaced, 5 deleted), 4 already dead | ✅ |
| Database arithmetic | 2 + 10 + 1 + 3 + 10 = 26 | 2 tables + 10 procedures added, 1 table repurposed, 3 tables + 10 procedures deleted = 26 | ✅ |
| Tasks with no checklist reference | 39 of 187 | 39 (and all are T171–T187 or post-checklist additions) | ✅ |
| Cited paths | — | 188 distinct file paths in `tasks.md`: 92 existing and verified present; 96 are feature-created files, feature-relative fragments, or regex truncations — no bogus existing path | ✅ |
| New tasks name real files | T185–T187 | all 7 named files exist; `ShellPage.xaml.cs:207`, `ShellViewModel.cs:29/338`, `SettingsViewModelTests.cs:832` verified verbatim | ✅ |
| Plan heading structure | no duplicates; `### Complexity Tracking` intact | single `### Complexity Tracking`, 4-column table with header, separator and one row; no duplicate headings in `plan.md` or `tasks.md` | ✅ |
| Constitution version | 1.2.0 | `**Version**: 1.2.0`, and `plan.md` assesses against v1.2.0 | ✅ |
| `STARTUP-FLOW.*` tracked | 3 tracked | `git ls-files` lists all three; only `.html`/`.pdf` deleted in the working tree | ✅ |

### Next Actions

1. **Before `/speckit.implement`**: resolve **C1** (one confirmation task or one recorded line in `plan.md` → Complexity Tracking plus `research.md`), and **I40** (narrow T005/checklist 1.1c so the final audit can pass). These are the two findings that block a green close-out gate.
2. **Cheap text fixes, same pass**: **I41** (`sql-contracts.md:232`, checklist `:337`), **I42** (`tasks.md:13`), **I43** (move T185–T187 to W2 Wave 1 and drop "and T012"), **I44** (seed counts), **I45** (31 → 56), **I46** (one page-set re-pin), **I47** (addendum Files list), **I48** (three named pairs), **I49** (typo), **D1** (fold T179's first clause), **G8** (add `FR-022` to T035/T036).
3. The remaining artefacts are otherwise consistent: `spec.md` ← `plan.md` ← `tasks.md` agree on the five terminal outcomes, the five-attempt limit, the 30-second ceiling, the four permission keys and their holders, the 26 database artefacts and the eleven-kept/eleven-deleted test surface; `checklist`/`brief`/`capabilities` introduce no contradiction of their own beyond I44; every cited existing path resolves; and the constitution's Review rule is satisfied for the recorded DPAPI deviation.
4. `/speckit.analyze` was run **read-only**; no file was modified. The `before_analyze` and `after_analyze` hooks in `.specify/extensions.yml` are both `optional: true` (git commits), and were deliberately not executed for this pass.

## Machine summary

C1|CRITICAL|Constitution|plan.md:243 + tasks.md T020/T061/T062/T063|Security & Secrets "bulk delete MUST require explicit confirmation" is unaddressed for the 13 live schema deletions and the Constitution Check records only the DPAPI deviation
I40|HIGH|Inconsistency|tasks.md:45 (T005) / checklist:36 vs plan.md:131-141, brief:21,51,95, tasks.md:129-129,250,252,267,308,349|T005 asserts "no file remains under Module_Startup/Views" while the rebuilt surfaces are built in that folder, making the T169/T170 close-out gate unreachable
I41|MEDIUM|Inconsistency|contracts/sql-contracts.md:232, STARTUP-REBUILD-CHECKLIST.md:337|"IT Department and Developer the four keys" contradicts the same section's table and T053/research D11 where log_panel is Developer-only
I42|MEDIUM|Inconsistency|tasks.md:13|Preamble still says "the four shared database artifacts" — the underived count A3 fixed in plan.md, contradicted by the plan/T024 enumeration
I43|MEDIUM|Inconsistency|tasks.md:584, tasks.md:625, tasks.md:69-84|T185-T187 are placed in W2 "beside T010 and T012" although T012 is in Wave 3 and T185/T187 require action before T010 deletes the contracts
I44|LOW|Inconsistency|checklist:19, data-model.md:428 vs brief:60-63, plan.md:13, tasks.md:102|Protected-seed count drifts between four and three (and data-model's "five seeds and aggregates")
I45|LOW|Inconsistency|contracts/logging-contract.md:145-146|"31 files import Microsoft.Extensions.Logging" does not reproduce; a tree-wide search returns 56
I46|LOW|Inconsistency|plan.md:213 vs tasks.md:132 (T031)|The page-activation exemption list is re-pinned to the placeholder's page set only, which Phase 3 retires, while the plan claims the new page set
I47|LOW|Inconsistency|tasks.md:590-591 vs tasks.md:603-613, 625|The addendum Files list omits IComputerRegistryService.cs, MTM_Waitlist.Startup/Services/ and research.md, and "two consumers (T185-T187)" counts three identifiers
I48|LOW|Inconsistency|plan.md:141|"the four old window/page pairs" matches neither the five old XAML pairs in the tree nor the three deleted outright
I49|LOW|Inconsistency|tasks.md:74|Phase 2 Files list misspells the activation folder as `MTM_waitlist.Core`
D1|LOW|Duplication|tasks.md:186 (T065) and tasks.md:551 (T179)|Both require absorbing the writer's own failures in StoreLogWriter.cs for FR-037, with T179 restating T065's clause
G8|LOW|Coverage|spec.md FR-022 vs tasks.md|FR-022 is the only FR with no explicit task reference; covered by inference via T035/T036
COUNTS|critical=1|high=1|medium=3|low=8|total=13