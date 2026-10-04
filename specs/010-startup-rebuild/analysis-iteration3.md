## Extension Hooks

**Optional Pre-Hook** (`.specify/extensions.yml` → `hooks.before_analyze`), not executed because this pass is read-only:

```text
Command: /speckit.git.commit
Description: Auto-commit before analysis
Prompt: Commit outstanding changes before analysis?
To execute: /speckit.git.commit
```

---

# `/speckit.analyze` — Startup Rebuild (`010-startup-rebuild`), second independent verification pass

**Feature**: `specs/010-startup-rebuild` · **Date**: 2026-09-26 · **Constitution**: **v1.2.0** (verified from `.specify/memory/constitution.md`, footer `Version: 1.2.0 | Ratified 2026-09-09 | Last Amended 2026-09-14`; seven principles plus three additional constraints, as `plan.md`'s Constitution Check states) · **Mode**: READ-ONLY — no file was created, edited or deleted.

## Specification Analysis Report

| ID | Category | Severity | Location(s) | Summary | Recommendation |
|----|----------|----------|-------------|---------|----------------|
| U5 | Underspecification | MEDIUM | `tasks.md`:425 (Phase 8 Files) vs `:428` (T137) · `local-state-test-migration.md` §4 item 3 | **Half-fixed.** T137's own line now carries the real path `MTM_Waitlist.Tests/Module_Settings/SettingsViewModelTests.cs`, but the same wrong path survives one line above it in Phase 8's **Files** inventory: `MTM_Waitlist.Tests/Module_Settings/ViewModels/SettingsViewModelTests.cs`. `Test-Path` = True for the real path, False for the `ViewModels/` variant, so the file set T137 is executed against still names a file that does not exist. The migration map calls this resolved; it is resolved for T137's line only. | Delete `/ViewModels` from the Phase 8 Files entry so both references agree. |
| G5 | Coverage gap | HIGH | `tasks.md` T010, T015, T025, T040, T078 · `ViewModels/ShellViewModel.cs`:29,302,338 · `Module_Core/Views/ShellPage.xaml.cs`:199,207 · `Module_Core/Views/ShellPage.xaml`:430,619 | T010 deletes `ISignOutService`, `SignOutResult` and `IAppProcessRestarter`, and T015 empties the project that implements them, but **no task re-points the shell that consumes them**. `ShellViewModel.cs` injects `ISignOutService` and exposes `Task<SignOutResult> SignOutAsync`, and `ShellPage.xaml.cs:207` spells the deleted `MTM_Waitlist.Module_Startup.Services.SignOutService.ResolveRestartFailedMessage()` — the exact namespace T005 asserts must not remain. No replacement for the process restart is tasked either, although FR-011 ("before the application restarts") and the sign-out edge case T174 tests require one. The Phase 2 gate ("the solution builds") is unreachable on the task set as written. | Add a task that re-points the shell's sign-out at the new session service and provides the restart path, and name `ViewModels/ShellViewModel.cs` and `Module_Core/Views/ShellPage.xaml.cs` in the Phase 2 Files list. |
| G6 | Coverage gap | MEDIUM | `tasks.md` T010, T015 · `MTM_Waitlist.Settings/ViewModels/ComputerManagementViewModel.cs`:16,20 · `.../ComputerEditDialogViewModel.cs`:12,13,17,19 · `MTM_Waitlist.Tests/Module_Settings/SettingsViewModelTests.cs`:832 | T010 deletes `IComputerRegistryService`; its only implementation is `MTM_Waitlist.Startup/Services/ComputerRegistryService.cs`, which T015 removes. The Settings computer screens still inject the interface (`ComputerManagementViewModel`, `ComputerEditDialogViewModel`) and the **kept** test file still declares `FakeComputerRegistryService : IComputerRegistryService`. `tasks.md` never names those three files, and T040's "Settings computer screens" is scoped to `StartupState` consumers, not to this dependency. | Either keep the interface and move its implementation out of the Startup project, or add the re-point to T012/T040 and name the three files. |
| G7 | Coverage gap | MEDIUM | `MTM_Waitlist.Mock.Service/Services/ServiceLog.cs`:13 · `MTM_Waitlist.Tests/Module_Mock_Service/ServiceLogTests.cs`:15 · `tasks.md` T001, T069, T135 | `StartupDebugLog` survives as **comment text** in the only two files the migration script cannot see (they contain no member call): a `<see cref="MTM_Waitlist.Module_Core.Helpers.StartupDebugLog"/>` and a `<c>StartupDebugLog</c>` mention. `RetiredSymbolAuditTests` is a raw text regex scan over `.cs` (comments included), and T001 adds patterns for "the entire old startup surface", so a `StartupDebugLog` pattern fails T005's assertion and T169's final audit on both files. No task edits either file. | Name the two files and their lines in T069 or T135, or exempt them explicitly with a stated reason. |
| I33 | Inconsistency | MEDIUM | `STARTUP-REBUILD-CHECKLIST.md`:342 (Phase 7 gate) vs `:331-341` (7.3, 7.4) · `plan.md`:74 · `contracts/machine-configuration-contract.md` §5 · `contracts/sql-contracts.md` §6 · `data-model.md` E7 | The Phase 7 gate reads "**the three keys** are seeded and enforced", while the same phase's tasks say "Add the **four** permission keys" and "Baseline the **four** keys", and every other artifact settles on four (`machine_configuration`, `ignored_locations_edit`, `log_panel`, `session_length`). A gate that counts three cannot be ticked against four seeded keys. | Change "the three keys" to "the four keys". |
| I34 | Inconsistency | LOW | `contracts/logging-contract.md`:144 vs `:146-147` · `data-model.md` E8 · `contracts/sql-contracts.md` §3 · `tasks.md` T047, T050 | §3 says the columns match the table's shape "plus the **three** new fields" and then lists **four** (`module`, `error_type`, `exception_detail`, `error_fingerprint`). `Database/Tables/10_ops_startup_logs/create.sql` holds 15 columns today; four are added, and every other artifact says four. | Change "three new fields" to "four new fields". |
| I35 | Inconsistency | LOW | `data-model.md`:146 | E4 says `auth_sessions_tokens` "is deleted in the same phase (**E9**)", but E9 is "Launch step"; the removal table is E10. The cross-reference sends the reader to an unrelated entity. | Change "(E9)" to "(E10)". |
| I36 | Inconsistency | LOW | `tasks.md` T157 vs `local-state-test-migration.md` §2.6 and §4 | T157 says the ten doubles are "five replaced and **five already dead** and deleted outright", but the map records only **four** as never constructed (`FakeLocalSettingsService`, and the three `InMemory…` fakes) and lists `RecordingLocalSettingsService` in `SettingsViewModelTests.cs` as live — it is passed by the two `BuildViewModel` helpers and used at lines 27, 38, 60, 71, 85, 103, 140 and more (verified). "Already dead" is true of four, not five. | Say "five replaced and five deleted, four of them already dead", or align the wording with §4. |
| I37 | Inconsistency | LOW | `tasks.md`:14-16 (preamble) vs the 184 task lines | The preamble states "each task names its checklist reference (`checklist 2.4a`)", but **36 of the 184 task lines carry no checklist reference** (T051, T075, T092–T095, T097, T104, T107–T108, T118–T121, T127–T130, T136, T138–T139, T148, T171–T174, T175–T184). The claim is absolute where the reality is 80 %. | Narrow the claim ("most tasks name their checklist reference") or add the missing references. |
| I38 | Inconsistency | LOW | `tasks.md` T027 · `STARTUP-REBUILD-BRIEF.md` S3.1 · `plan.md` tree · `STARTUP-REBUILD-CHECKLIST.md` 2.5d | T027 deletes `STARTUP-FLOW.md`, `STARTUP-FLOW.html` and `STARTUP-FLOW.pdf`; a recursive glob shows **only `STARTUP-FLOW.md` exists** — there is no `.html` and no `.pdf` anywhere outside `bin`/`obj`. Three artifacts list two files that are not in the tree. | Drop the two non-existent names, or note they are already absent. |
| I39 | Inconsistency | LOW | `local-state-test-migration.md` §2.6 | The sentence enumerating "the remaining `RecordingLocalSettingsService` mentions (lines …)" claims completeness but omits lines **589, 609 and 640** (the class is also constructed there; 655/681 are the `BuildViewModel` parameters, which the table covers as a range). | Add 589, 609 and 640, or soften "the remaining mentions" to "the mentions at …". |
| A3 | Ambiguity | LOW | `plan.md`:14 (Scale note) | "the **four** shared database artifacts that must never be deleted by the dead-weight audit" is a count with no referent and no derivation, while T024 and brief S3.2 enumerate five tables, eleven procedures plus the retained function, three seeds and five aggregate/descriptions files. | Replace "four shared database artifacts" with the enumerated protected list T024 carries, or point at brief S3.2. |

## Prior Findings Verification

- **U5 — STILL OPEN.** T137's own text is now correct (`tasks.md`:428 ends with `MTM_Waitlist.Tests/Module_Settings/SettingsViewModelTests.cs`; `Test-Path` True there and False for the `ViewModels/` variant), **but** the same wrong path is still present in the same file, one line above: `tasks.md`:425, inside Phase 8's **Files** block. The fix thus names the right file in the task and the wrong file in the inventory — exactly the "right words in one artifact, contradiction in another" case. `local-state-test-migration.md` §4 item 3 declares it resolved; the inventory says otherwise.
- **I22 — CLOSED.** `data-model.md` E6 now reads: "The four new keys are additions — `machine_configuration`, `log_panel` and `session_length`, plus the `ignored_locations_edit` key D10 adds — and no existing key's grants change." This matches E7's four rows, `machine-configuration-contract.md` §5, `sql-contracts.md` §6, `plan.md`:74 and `research.md` D11. No five-key claim survives.
- **I23 — CLOSED.** `Select-String -Pattern 'name="Startup_'` returns **15** in `Strings/en-us/TooltipResources.resw` and **15** in `Strings/en-us/TooltipResources.developer.resw` — the same names in both (10 login/splash-name keys, 3 `Startup_SplashPage_*`, 3 `Startup_SplashView_*`). All three artifacts now say fifteen: `tasks.md` T018 ("the fifteen `Startup_*` tooltip keys"), `STARTUP-REBUILD-BRIEF.md` S3.1 ("the fifteen `Startup_*` tooltip keys (in **both** `TooltipResources.resw` and `TooltipResources.developer.resw`)") and `STARTUP-REBUILD-CHECKLIST.md`:105.
- **I24 — CLOSED.** Brief S3.2's Core-services line now reads: "`ILocalSettingsService` and `IFileService` stood in this list in the original audit and are **not** protected: the local-state removal deletes both with the mechanism they serve (see S11.5.1 and their own rows below)." T024 names the narrowing ("The two listings this feature deliberately narrows rather than protects — `ILocalSettingsService` and `IFileService` — are deleted with the local-state mechanism, and that narrowing is recorded in the brief's S3.2"). Nothing still claims they survive.
- **I25 — CLOSED.** Brief S3.1 now says "`StartupDebugLog` — see S9 for its 489 call sites". Re-derived read-only: `StartupDebugLog\.(Info|Error|Configure)` matches **489** across **83** files — **335 `Info`, 147 `Error`, 7 `Configure`** (the union of files is exactly 83, and the production-only subset is 480 in 81 files, which no artifact contradicts).
- **I26 — CLOSED.** `plan.md`'s tree now reads "the `ILogger` provider: 226 tree-wide `ILogger` call sites stay untouched, 162 of them the app's own", and `research.md` D5 reads "226 calls across 26 files tree-wide, of which 162 are the application's own — the other 64 sit in `MTM_Waitlist.Mock.Service`, a separate host the provider does not serve". Re-derived: `_logger.` = **219** tree-wide and **58** in `MTM_Waitlist.Mock.Service`; standalone `logger.`/`Logger.` invocations = **7** (3 `App.xaml.cs`, 3 `ServiceOperatorAuthenticationHandler.cs`, 1 `ServiceLogTests.cs`), the other matches being two doc-comment mentions and three `logger.IsEnabled` assertions; `using Microsoft.Extensions.Logging;` is present in exactly **31** files; combined logger call sites span **26** files. The split is consistent everywhere it appears.
- **I27 — CLOSED.** T170 now cites "FR-001–FR-038, SC-001–SC-017"; the spec holds exactly **38** FR headings and **17** SC headings (counted).
- **I28 — CLOSED.** `local-state-test-migration.md` §4 item 3 is headed "T137's file path was wrong — **resolved 2026-09-26**" and item 5 "Two test files compile against `LocalSettingsOptions` — **resolved 2026-09-26**"; T152 does name `Core/Models/StartupOptionsModelsTests.cs` and `Models/StartupModelsTests.cs` with both method names. (The *fix* for U5 is incomplete — see U5 — but §4's two entries are marked.)
- **I29 — CLOSED.** Phase 2's Files list now contains both `capabilities/startup/**` and `capabilities/startup-diagnostics/**`; Phase 9's Files list no longer names the diagnostics folder (it names `capabilities/startup-launch/spec.md`, `living-specs.yml` and `capabilities/DRIFT.md`). T026 deletes both folders; T163 retires both registry rows. `capabilities/` verified to hold `startup/spec.md` and `startup-diagnostics/spec.md`, with `DRIFT.md`'s two-capability table matching them.
- **I30 — CLOSED.** T024 now enumerates the protected list explicitly: "the five tables (`core_users_profiles`, `core_computers_registry`, `auth_roles_catalog`, `auth_roles_assignments`, `config_settings_values`), the eleven procedures and the retained `fn_server_utc_now` (twelve data operations in all), and the three seeds with the five aggregate and descriptions files." Re-derived from brief S3.2: `sp_auth_user_row_get` + `sp_auth_temporary_credential_attempt_record` + six `sp_core_computers_registry_*` + `sp_auth_roles_list` + `sp_config_settings_values_get` + `sp_config_settings_upsert` = **11**, plus `fn_server_utc_now` = **12**; all eleven and the function verified present as files. Nuance, not a finding: `STARTUP-REBUILD-CHECKLIST.md`:18's Phase 0 record still reads "12 shared procedures, 5 shared tables and 4 shared seeds" — that line reports what the *audit flagged as shared* (it counts the function among the twelve and a fourth shared seed), a different measure from T024's *protected* list, so it does not contradict it.
- **I31 — CLOSED, with a wording nuance.** The contradiction is gone: the Design-invariants row now says the living spec's "one destination out of three" "is the second count this plan supersedes rather than carries forward: FR-001 now defines five terminal outcomes, and `LaunchOutcome` in `contracts/launch-step-contract.md` §5 is their single statement". FR-001 names five outcomes, the `LaunchOutcome` enum has five members and `data-model.md` E9 says "Five terminating outcomes". The fix's own description ("the phrase is removed") overstates what landed — the phrase is still quoted — but it is no longer carried forward as satisfied, which is what the finding was about.
- **I32 — CLOSED.** Both now give the per-direction breakdown. `plan.md`'s Scale note: "two tables, one repurposed table, ten new procedures, four new permission keys, ten procedures deleted … and three tables deleted"; `plan.md`'s Scale/Scope: "two tables and ten procedures added, a table repurposed, and three tables and ten procedures deleted"; `tasks.md`'s preamble: "26 artifacts touched: two tables and ten procedures added, one table repurposed, and three tables and ten procedures deleted". Arithmetic re-derived: 2 + 10 + 1 + 3 + 10 = **26**; the ten added are the four session, three remembered-sign-in and three log procedures in `contracts/sql-contracts.md` §§1–3; the ten deleted are T020's nine plus `sp_core_buildings_upsert` (T061).
- **D1 — CLOSED.** T159 now says "the four spec cases T171 to T174 already own are covered by those tasks and are not written twice", while Wave 2's heading still reads "the four spec edge cases no other task covers". No task pair produces the same tests.

**Additional re-derivations requested (all read-only):**

- Local-state arithmetic: §1A = **6**, §1B = **9**, overlap = **4**, distinct = **11** (6 + 9 − 4); kept and re-pointed = **11**; doubles removed from kept files = **10** (5 replaced, 5 removed). The 6/9/4/11 figures are stated the same way in the map, `plan.md`:74 and T157.
- Database artifacts: **26** (see I32). `sp_config_settings_upsert` exists as `Database/StoredProcedures/sp_config_settings_upsert`; `sp_config_settings_values_upsert` does not. All nine startup-only procedures, `sp_core_buildings_upsert` and `fn_server_utc_now` exist today.
- Aggregates: `Database/Tables/AllTables.sql`, `Database/StoredProcedures/AllSPs.sql`, `Database/Functions/AllFunct.sql`, `Database/Views/AllViews.sql`, `Database/Seeds/AllSeeds.sql` and `Database/Bootstrap/update_table_descriptions.sql` all exist at the cited paths. `Database/Tables` peaks at ordinal **33**, so 34 and 35 are the next free ordinals as T044/T046 state.
- Spec/checklist counts: Edge Cases = **23** bullets; FR = **38**, SC = **17**; tasks = **184** contiguous identifiers (T001–T184, no gaps, no duplicates); checklist = **181** checkbox lines, **11** `## Phase N` headings (Phase 0–10), **11** `**GATE:` lines`.
- `plan.md` markdown: one `## Constitution Check` heading, no duplicate heading in the file, every Constitution Check row has exactly 3 pipes (a 2-column table), `### Complexity Tracking` follows the table, and the post-table notes sit inside `## Constitution Check` — the additions did not break or duplicate a section.
- Constitution Review rule: the single DPAPI deviation is recorded in `plan.md` → Complexity Tracking with its necessity and four rejected alternatives; the Security & Secrets row reads "**PASS with one recorded deviation**"; `research.md` D12 and `data-model.md` E5 point at the same record; the only other qualified row is the living-spec row ("PASS, with one deliberate reversal recorded"). No constraint asserts an unqualified PASS where a deviation exists.

## Coverage Summary

| Requirement Key | Has Task? | Task IDs | Notes |
|---|---|---|---|
| FR-001 exactly one of five terminal outcomes | Yes | T077, T170 | Contract §5 is the single statement |
| FR-002 name each piece of work before it runs | Yes | T071, T072, T073, T076, T080, T085, T087, T116 | |
| FR-003 every wait bounded, none unbounded | Yes | T071, T073, T074, T080, T084 | |
| FR-004 cause specific to what stopped it | Yes | T085, T087, T124 | |
| FR-005 error in a bottom strip, not covering the feed | Yes | T090 | |
| FR-006 unconfigured machine cannot reach main screens | Yes | T075, T077, T096, T103 | |
| FR-007 setup authorised by IT/Developer only | Yes | T095, T099, T100 | |
| FR-008 every non-completion route ends the app | Yes | T078, T096, T102, T127, T174 | G5: the restart half has no implementing task |
| FR-009 removed/revoked/unreadable ⇒ unconfigured | Yes | T075, T097, T101 | |
| FR-010 session judged on the store clock | Yes | T044, T107, T111 | |
| FR-011 sign-out clears the session before restart | Yes | T044, T107, T111, T172, T174 | G5: restart path unassigned |
| FR-012 five-attempt limit, survives restart | Yes | T106, T110, T173 | |
| FR-013 password step only after temp credential | Yes | T115, T173 | |
| FR-014 remembered sign-in encrypted, safe fallback | Yes | T108, T113, T117 | |
| FR-015 unreadable hardware identity admitted | Yes | T112 | |
| FR-016 offer to repeat the failed work | Yes | T121, T125 | |
| FR-017 reset only where it could remove the cause | Yes | T119, T122, T124 | |
| FR-018 reset previews and stays on this machine | Yes | T075, T120, T122, T123, T126 | |
| FR-019 repair without asking | Yes | T121, T123 | |
| FR-020 repeat only the failed work and what follows | Yes | T077, T082, T121, T125 | |
| FR-021 every diagnostic reaches the store | Yes | T064 | Field-level detail added by FR-032–FR-036 |
| FR-022 identity and machine facts read-only | Yes | T035, T036, T037, T038, T040, T042 | No task cites FR-022 by ID (T170 cites the range FR-001–FR-038); the substance is covered by T035/T036's "read-only, no writer but the launch pipeline" and T042's deletion |
| FR-023 preferences held against the person | Yes | T138, T140, T141, T143, T144, T145 | |
| FR-024 plant list changeable by two roles | Yes | T137, T142, T146 | |
| FR-025 only store-reach stays local | Yes | T155, T156 | |
| FR-026 picture copy best effort | Yes | T081, T083, T088 | |
| FR-027 verdict settled before screens open | Yes | T084, T089 | |
| FR-028 replaced behaviour must fail the build | Yes | T169 | G7 weakens the guard: two `StartupDebugLog` comment references survive |
| FR-029 session length gated to two roles | Yes | T137, T147 | |
| FR-030 role restrictions unchanged | Yes | T139, T149 | |
| FR-031 sweep cases recorded, none dropped | Yes | T159, T160, T171, T167 | |
| FR-032 fault recorded whole, with its chain | Yes | T047, T175, T180 | |
| FR-033 stable fingerprint groups repeats | Yes | T047, T181, T176 | |
| FR-034 version and gathered runtime context | Yes | T182, T177 | |
| FR-035 action, view and control | Yes | T064, T177 | |
| FR-036 provider diagnostics, no values | Yes | T182, T177, T178 | |
| FR-037 recording never hides the fault | Yes | T065, T180, T179 | |
| FR-038 panel copies pasteable text | Yes | T131, T170, T183, T184 | |
| SC-001 no silent stop | Yes | T174, T168, T170 | |
| SC-002 no wait past its maximum | Yes | T074, T168 | |
| SC-003 released build records entries | Yes | T130 | Implemented by T047/T064/T065 |
| SC-004 zero input sequences reach the shell | Yes | T096, T161 | |
| SC-005 panel finds a machine's history in under a minute | Yes | T129 | |
| SC-006 every route out of setup ends the app | Yes | T096 | |
| SC-007 attempt limit across a restart | Yes | T106 | |
| SC-008 no preference lost moving computers | Yes | T138 | |
| SC-009 build fails on reintroduction | Yes | T169 | G7 applies |
| SC-010 no useless remedy offered | Yes | T119 | |
| SC-011 session-length change takes effect next sign-in | Yes | T111 | T137 also exercises the refusal |
| SC-012 role restrictions verified one at a time | Yes | T137, T139, T149 | |
| SC-013 every case tested or retired with reason | Yes | T159, T160, T171, T167 | |
| SC-014 repeats group as one | Yes | T049, T131, T181 | |
| SC-015 provider code present, no parameter values | Yes | T182 | |
| SC-016 dropped, not local, fault reaches caller | Yes | T130, T182 | T065/T179 implement |
| SC-017 copy one entry and paste it whole | Yes | T170, T184 | T183 implements |

## Constitution Alignment Issues

None. Every principle and every additional constraint is either satisfied or carries a recorded, reasoned deviation.

- **I, II, IV, V, VI, VII · External Integration & Cache Boundaries · Documentation & Extensibility** — no conflicting requirement or plan element found across the 38 FRs, the 17 SCs, the task set or the supporting artifacts. Constitution v1.2.0 is the version the plan assesses against, and the plan's own numbering matches (seven principles, three additional constraints).
- **III (Stored-Procedure-First)** — satisfied: the ten added operations are procedures, the log write is `sp_ops_startup_logs_insert`, the fingerprint is a column written by procedure, and the aggregates plus `update_table_descriptions.sql` are named for every addition and deletion.
- **Additional: Security & Secrets** — the single deliberate departure (the remembered sign-in stored as decryptable AES-256 ciphertext rather than DPAPI `ProtectedData`/`CurrentUser`) is recorded in `plan.md` → Complexity Tracking with its necessity and four rejected alternatives, in `research.md` D12 as a "**Recorded deviation**" paragraph, and in `data-model.md` E5. The Constitution Check row for that constraint reads "**PASS with one recorded deviation**", so it is not an unqualified PASS, and no other constraint claims PASS over an existing deviation.
- **Review rule** — satisfied: the deviation and its rationale sit inside `plan.md` rather than in a note beside it.
- **Principle VI risk (not a violation).** G5, G6 and G7 do not breach a principle, but they mean the mandated build gate ("the solution builds, the guard passes, and no removed symbol resolves anywhere") cannot be reached at Phase 2 on the task set as written. That is why G5 is the one HIGH finding.

## Unmapped Tasks

184 tasks exist. Every FR and every SC has at least one task (55/55). **96** task lines carry no FR/SC citation; they are removal, infrastructure, schema, registry, documentation and verification work and are legitimate:

T001–T005, T006–T034, T035–T043, T045, T046, T048, T050–T063, T066–T070, T079, T086, T091–T094, T098, T104, T105, T109, T114, T118, T128, T132–T136, T148, T150–T154, T157, T158, T162–T166.

Separately, **36** task lines carry no checklist reference (see I37): T051, T075, T092–T095, T097, T104, T107–T108, T118–T121, T127–T130, T136, T138–T139, T148, T171–T174, T175–T184.

## Metrics

- **Total Requirements**: 55 (38 FR + 17 SC)
- **Total Tasks**: 184 (T001–T184, contiguous, no duplicates, no gaps)
- **Coverage %**: 100 % (55 of 55 requirements have ≥1 task; FR-022's coverage is by substance — T035/T036/T040/T042 — with no task citing it by ID)
- **Ambiguity Count**: 1 (A3)
- **Duplication Count**: 0 (D1 closed)
- **Coverage-gap Count**: 3 (G5, G6, G7)
- **Inconsistency Count**: 7 (I33, I34, I35, I36, I37, I38, I39)
- **Underspecification Count**: 1 (U5, still open)
- **Critical Issues Count**: 0

## Next Actions

One HIGH finding exists, so it is worth clearing before `/speckit.implement` because it reaches the removal phase itself:

1. **G5 (HIGH)** — schedule the shell's sign-out and the process-restart path: re-point `ViewModels/ShellViewModel.cs` and `Module_Core/Views/ShellPage.xaml.cs` off `ISignOutService`/`SignOutResult`, give `IAppProcessRestarter`'s replacement a task, and name both files in Phase 2's Files list. Without it, the Phase 2 build gate cannot go green and T005's "no type remains in namespace `MTM_Waitlist.Module_Startup`" assertion fails on `ShellPage.xaml.cs:207`.
2. **U5 (MEDIUM)** — delete `/ViewModels` from the Phase 8 Files entry so the inventory and T137 agree.
3. **G6 (MEDIUM)** — decide `IComputerRegistryService`'s fate: keep it and move its implementation out of the Startup project, or add its two production consumers and the kept test fake to T040's migration.
4. **G7 (MEDIUM)** — name `MTM_Waitlist.Mock.Service/Services/ServiceLog.cs:13` and `MTM_Waitlist.Tests/Module_Mock_Service/ServiceLogTests.cs:15` in T069 or T135, so the retired-symbol audit has nothing to trip on.
5. **I33 (MEDIUM)** — change the Phase 7 gate's "three keys" to "four keys".

The seven LOW findings (I34–I39, A3) are documentation hygiene and can be fixed opportunistically when each file next changes. All twelve are manual edits: none changes a requirement, an entity or a task boundary, so no new `/speckit.specify` or `/speckit.plan` pass is warranted.

Would you like me to suggest concrete remediation edits for the five findings above (G5, U5, G6, G7, I33)? I will not apply them — this pass is read-only and wrote no file.

## Extension Hooks

**Optional Hook** (`.specify/extensions.yml` → `hooks.after_analyze`):

```text
Command: /speckit.git.commit
Description: Auto-commit after analysis
Prompt: Commit analysis results?
To execute: /speckit.git.commit
```

Nothing needed from you: this pass wrote no file.

## Machine summary

U5|MEDIUM|Underspecification|tasks.md:425 (Phase 8 Files) vs :428 (T137)|T137's own path is corrected but the same non-existent Module_Settings/ViewModels/SettingsViewModelTests.cs path survives in the Phase 8 Files inventory
G5|HIGH|Coverage gap|tasks.md T010/T015/T040 · ViewModels/ShellViewModel.cs:29,302,338 · Module_Core/Views/ShellPage.xaml.cs:199,207|ISignOutService, SignOutResult and IAppProcessRestarter are deleted with no task re-pointing the shell consumers, and ShellPage.xaml.cs:207 spells the deleted MTM_Waitlist.Module_Startup namespace T005 asserts must be gone
G6|MEDIUM|Coverage gap|tasks.md T010/T015 · MTM_Waitlist.Settings/ViewModels/ComputerManagementViewModel.cs:16,20 · ComputerEditDialogViewModel.cs:12-19 · Module_Settings/SettingsViewModelTests.cs:832|IComputerRegistryService is deleted while its Settings consumers and the kept test fake still implement or inject it, and no task names them
G7|MEDIUM|Coverage gap|MTM_Waitlist.Mock.Service/Services/ServiceLog.cs:13 · MTM_Waitlist.Tests/Module_Mock_Service/ServiceLogTests.cs:15|StartupDebugLog survives as comment text in two Mock.Service files no task edits, which the raw-text retired-symbol audit will flag
I33|MEDIUM|Inconsistency|STARTUP-REBUILD-CHECKLIST.md:342 vs :331-341; plan.md:74; machine-configuration-contract.md §5; sql-contracts.md §6|The Phase 7 gate says three permission keys while the same phase's tasks, the plan and both contracts say four
I34|LOW|Inconsistency|contracts/logging-contract.md:144 vs :146-147|"plus the three new fields" is followed by four new fields, and data-model E8, sql-contracts §3 and T047/T050 all say four
I35|LOW|Inconsistency|data-model.md:146|E4 cites "(E9)" for the auth_sessions_tokens deletion but E9 is "Launch step"; the removal table is E10
I36|LOW|Inconsistency|tasks.md T157 vs local-state-test-migration.md §2.6 and §4|T157 calls five removed doubles "already dead" where the map documents four as never constructed and lists RecordingLocalSettingsService as live
I37|LOW|Inconsistency|tasks.md:14-16 vs the 184 task lines|The preamble claims every task names its checklist reference but 36 of 184 carry none
I38|LOW|Inconsistency|tasks.md T027 · STARTUP-REBUILD-BRIEF.md S3.1 · plan.md tree · checklist 2.5d|STARTUP-FLOW.html and STARTUP-FLOW.pdf are named for deletion but do not exist anywhere in the tree
I39|LOW|Inconsistency|local-state-test-migration.md §2.6|The "remaining RecordingLocalSettingsService mentions" list omits lines 589, 609 and 640
A3|LOW|Ambiguity|plan.md:14|"the four shared database artifacts that must never be deleted" is an underived count against the enumerated protected list in brief S3.2 and T024
COUNTS|critical=0|high=1|medium=4|low=7|total=12