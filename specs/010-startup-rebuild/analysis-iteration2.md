# `/speckit.analyze` — Startup Rebuild (`010-startup-rebuild`), independent verification pass

**Feature**: `specs/010-startup-rebuild` · **Date**: 2026-09-26 · **Constitution**: **v1.2.0** (ratified 2026-09-09, last amended 2026-09-14, seven principles + three additional constraints, as `plan.md`'s Constitution Check states) · **Mode**: READ-ONLY — no file was created, edited or deleted.

## Specification Analysis Report

| ID | Category | Severity | Location(s) | Summary | Recommendation |
|----|----------|----------|-------------|---------|----------------|
| U5 | Underspecification | MEDIUM | `tasks.md`:428 (T137) · `local-state-test-migration.md` §4 item 3 | T137 names `MTM_Waitlist.Tests/Module_Settings/ViewModels/SettingsViewModelTests.cs`. That path does not exist; the file is `MTM_Waitlist.Tests/Module_Settings/SettingsViewModelTests.cs` (`Test-Path` on the named path = False). The migration map already recorded this and says the path "must be corrected before T137 is executed"; it has not been. | Correct T137's path to `MTM_Waitlist.Tests/Module_Settings/SettingsViewModelTests.cs`. |
| I22 | Inconsistency | MEDIUM | `data-model.md`:232-233 (E6) vs `data-model.md`:244 (E7) · `plan.md`:70 · `contracts/machine-configuration-contract.md` §5 · `contracts/sql-contracts.md` §6 | E6 reads "The four new keys **and the one new edit key** are additions" — five keys. E7, plan.md ("four new permission keys"), both contracts and D11 all settle on **four**, with `ignored_locations_edit` being one of them. | Change E6 to "The four new keys are additions" (or "three new keys and the one new edit key"). |
| I23 | Inconsistency | MEDIUM | `tasks.md` T018 · `STARTUP-REBUILD-CHECKLIST.md`:105 · `STARTUP-REBUILD-BRIEF.md`:61-66 (S3.1) | "the fourteen `Startup_*` tooltip keys". Measured: **15** `Startup_*` keys in `TooltipResources.resw` and **15** in `TooltipResources.developer.resw` (10 login keys + 3 SplashPage + 3 SplashView). A deletion scoped to fourteen leaves one behind. | Say fifteen, or name the key that is intentionally kept. |
| I24 | Inconsistency | MEDIUM | brief S3.2:82 · `tasks.md` T150, T151 · `tasks.md`:113 (T024) · `plan.md` Core tree | The brief's protected list names `ILocalSettingsService` and `IFileService` under "Core services" as artifacts that "must survive untouched", while T150/T151 delete both, `plan.md`'s tree marks them `DELETED`, and E6 says the whole mechanism goes. Nothing records that S3.2's entry is superseded, and T024's protected-list confirmation counts only procedures, tables and seeds. | Record the narrowing (brief or `research.md`) and name the two services in T024. |
| I25 | Inconsistency | LOW | `STARTUP-REBUILD-BRIEF.md`:61 (S3.1) | Brief S3.1 says `StartupDebugLog` has "**491** call sites"; brief S9 says 489; a read-only count of `StartupDebugLog.` returns exactly **489 across 83 files** (335 `Info`, 147 `Error`, 7 `Configure`). | Correct S3.1 to 489. |
| I26 | Inconsistency | LOW | `plan.md`:133 · `research.md`:68 | Both present all 226 `ILogger` call sites as the new provider's ("so all 226 existing `ILogger` call sites stay untouched" / "226 calls in 26 files is served by a provider"), while the I17 fix (`contracts/logging-contract.md` §4, T066, T070) scopes 64 of the 226 to `MTM_Waitlist.Mock.Service`, "a separate host … that this provider does not serve". | Qualify both lines with the tree-wide / 162-app-side split. |
| I27 | Inconsistency | LOW | `tasks.md`:512 (T170) | The single Success-Criteria validation task cites "FR-001–FR-030, SC-001–SC-012", but the artifact set now carries FR-031–FR-038 and SC-013–SC-017 (logging reference plus the copy addendum). | Extend T170's citation, or state why the new criteria are validated elsewhere. |
| I28 | Inconsistency | LOW | `local-state-test-migration.md` §4 item 5 · `tasks.md` T152 | The map's open "does not reconcile" item 5 asserts that two tests compile against `LocalSettingsOptions` and "no task in `tasks.md` rewrites or deletes them"; T152 now names both files and both method names (`LocalSettingsOptions_Defaults`, `LocalSettingsOptions_AcceptsConfiguredValues` — verified present in those files). | Update or close §4 item 5. |
| I29 | Inconsistency | LOW | `tasks.md`:28 (Phase 2 Files) · `tasks.md`:478 (Phase 9 Files) · `tasks.md`:117 (T026) | Phase 2's file list names only `capabilities/startup/**` although T026 deletes both capability folders; Phase 9's list names `capabilities/startup-diagnostics/`, a folder Phase 2 removes (only its registry row is retired in Phase 9). | Add `capabilities/startup-diagnostics/**` to Phase 2's list and drop it from Phase 9's. |
| I30 | Inconsistency | LOW | `tasks.md`:113 (T024) · brief S3.2:73-84 · `STARTUP-REBUILD-CHECKLIST.md`:18 | T024 asks to confirm "twelve shared procedures, five shared tables and four shared seeds". Brief S3.2 names **11** procedures (the 12th item is `fn_server_utc_now`, a function) and **3** seeds (plus five aggregate files). The figures come from the checklist's Phase 0 audit, so T024 cannot be checked against a list. | State the protected list once and have T024 point at that list. |
| I31 | Inconsistency | LOW | `plan.md`:229 · `capabilities/startup/spec.md`:118 · `spec.md`:198-200 (FR-001) | plan.md carries the living spec's "one destination out of three" forward as a satisfied invariant while FR-001 and `LaunchOutcome` now define **five** terminal outcomes. Unlike the living spec's other reversal, this count change is not noted. | Note that FR-001 supersedes the three-destination rule. |
| I32 | Inconsistency | LOW | `tasks.md`:5-12 (Scale note) · `plan.md`:11-18, 74-77 | The A2 fix is complete in plan.md's Scale/Scope (26 = 2+10 added, 1 repurposed, 3+10 deleted), but the plan's Scale **note** and tasks.md's preamble carry the deletion direction only — no added-procedure count and no 26 total. | Add the per-direction line to tasks.md's preamble, or narrow the claim. |
| D1 | Duplication | LOW | `tasks.md`:483-487 (T159) · `tasks.md`:495-508 (T171–T174) | T159's mandate — "write a test for each case that still applies, as one test per case" — subsumes the four spec cases T171–T174 are created to own, so two tasks can produce the same tests ("Wave 2: the four spec edge cases no other task covers"). | Scope T159 to the expanded S13 catalogue and let T171–T174 own the four spec cases, or say so in both lines. |

## Prior Findings Verification

- **C1 — CLOSED.** `plan.md` has `### Complexity Tracking` under `## Constitution Check` (lines 233-247) with one deviation row: the AES-256 remembered-sign-in payload vs the DPAPI `ProtectedData`/`CurrentUser` requirement, its necessity, and four rejected alternatives; the Review rule's wording is met ("the Review rule requires it to be recorded here rather than left as an unqualified PASS"). The Security & Secrets row reads "**PASS with one recorded deviation.**"; `research.md` D12 carries a "**Recorded deviation.**" paragraph pointing at `plan.md` → Complexity Tracking; `data-model.md` E5 has the matching bullet. Re-checked every other Constitution Check row: the only other qualified row is the living-spec row ("PASS, with one deliberate reversal recorded"), and the post-table notes record the two plan-level resolutions. No constraint is left asserting an unqualified PASS where a deviation exists.
- **G3 — CLOSED.** `Database/Bootstrap/update_table_descriptions.sql` (exists) is now named in T023, T057 and T062 with the retired-objects sections, in `plan.md`:186 and in `data-model.md` E10:419-420. Search for `update_table_descriptions` returns 6 artifact hits, not 1.
- **G4 — CLOSED, with a placement defect (I29).** T026 deletes `capabilities/startup/` **and** `capabilities/startup-diagnostics/`; T163 retires both registry rows ("the `startup` and `startup-diagnostics` rows in `living-specs.yml` and in `capabilities/DRIFT.md`'s two-capability table"); `plan.md`'s tree marks both `DELETED`. `capabilities/DRIFT.md` really does declare exactly two capabilities, the second matching `MTM_Waitlist.Startup/Services/StartupLog*.cs` (both files exist).
- **I13 — CLOSED.** `sp_config_settings_upsert` in `research.md` D9:118 ("which is the procedure that exists (`sp_config_settings_values_upsert` does not)"), `contracts/sql-contracts.md` §5:212, `data-model.md` E10:426, `STARTUP-REBUILD-BRIEF.md`:316 and `STARTUP-REBUILD-CHECKLIST.md`:182. Verified read-only: `sp_config_settings_upsert` exists in `Database/StoredProcedures/`, no `sp_config_settings_values_upsert` directory exists.
- **I14 — CLOSED.** `Database/Tables/AllTables.sql`, `Database/Functions/AllFunct.sql`, `Database/Views/AllViews.sql` are correct in T023, T057, T060, T062, in `plan.md`:187-190 and in `data-model.md` E10:418-419. Verified: all five aggregates plus the descriptions file exist at those paths; no `Database/All*.sql` reference remains.
- **I15 — CLOSED.** T159:483-487 says "the twenty-three cases"; `spec.md`'s Edge Cases section holds exactly **23** bullets (counted).
- **I16 — CLOSED for the three named artifacts.** FR-001:198-200 names five outcomes and points at `LaunchOutcome` as "the single statement of the terminal outcomes"; `contracts/launch-step-contract.md` §5's enum has five; `data-model.md` E9 says "Five terminating outcomes … matches FR-001" and explains "`Configured` is a transition rather than a terminus". A fourth enumeration survives at `plan.md`:229 — see I31.
- **I17 — CLOSED.** `contracts/logging-contract.md` §4:151-155, T066:181 and T070:189 all state the tree-wide figure, the 64-site host split (58 `_logger.` + 6 standalone in `MTM_Waitlist.Mock.Service`) and the app-side 162. Re-derived read-only: `_logger.` = **219** tree-wide, **58** in `MTM_Waitlist.Mock.Service`; standalone `logger.`/`Logger.` call sites = **7** in 3 files (3 + 3 in that host, 1 in a test), the other three matches being `IsEnabled` assertions. plan.md:133 and `research.md`:68 still lack the qualification — see I26.
- **I18 — CLOSED.** `plan.md`:74 says "eleven deletions and eleven re-points"; the migration map's §1B header says nine files with "Four of these are also in §1A"; §4 says "T025 covers four of the six §1A deletions"; §5 has the distinct-total row. Arithmetic verified: 6 + 9 − 4 = **11**, and §5's kept count is 11.
- **I19 — CLOSED.** SC-005:302-303 covers both pairs ("filtered by machine and by error kind, and by severity and machine"), matching US5 scenario 2 and T129.
- **I20 — CLOSED.** T160:488 cites FR-031 and SC-013 and brief S16, not SC-009; T169:510 is the task that cites FR-028/SC-009.
- **I21 — CLOSED.** `living-specs.yml` is tracked (`git ls-files` lists it) and deleted in the working tree (`git status --porcelain` → ` D living-specs.yml`); T163:494 says "restoring `living-specs.yml` first if the working tree still has it deleted, since it is tracked".
- **U3 — CLOSED.** All five places say "the store or the external read-only system": FR-025:235-237, `contracts/machine-configuration-contract.md` §6 ("Only the connection strings used to reach the store and the external read-only system, plus one reviewed exception", with the `InforVisualDatabaseOptions` row), `data-model.md` E6, `research.md` D27 and `plan.md`'s principle III row.
- **U4 — CLOSED.** SC-008:305-306 lists "theme, waitlist order, alerts, seen-requests and the parts-without-pictures choice"; T138:439 lists the same five.
- **A2 — CLOSED.** `plan.md`:74-77 states "26 database artifacts touched: two tables and ten procedures added, one table repurposed, and three tables and ten procedures deleted" (2+10+1+3+10 = **26**), and the Scale note gives the deletion direction as "ten procedures — the nine startup-only ones plus `sp_core_buildings_upsert`". Independently corroborated: `STARTUP-REBUILD-CHECKLIST.md`:18 (the ticked Phase 0 audit) says "10 startup-only procedures identified"; T020 names nine and T061 names the tenth. Verified `sp_core_buildings_upsert` and all nine exist as directories. Caveat recorded as I32.
- **Checklist count (181) — CLOSED.** Counted directly: **181** lines matching `^\s*-\s*\[[ xX]\]`, **11** `##` phase headers (Phase 0–10), **11** `GATE` lines (24, 47, 110, 132, 166, 233, 294, 342, 405, 426, 454). `plan.md`:53 and :225 and `tasks.md`:14 now say 181; no surviving "179 tasks" claim anywhere in the artifacts, the brief or the checklist.

## Coverage Summary

| Requirement Key | Has Task? | Task IDs | Notes |
|---|---|---|---|
| FR-001 one of five terminal outcomes | Yes | T077, T170 | Contract §5 is the single statement |
| FR-002 name work before doing it | Yes | T071–T074, T076, T080, T085, T116 | |
| FR-003 every wait bounded | Yes | T071, T073, T074, T080, T084 | |
| FR-004 cause specific to the stop | Yes | T085, T087, T122, T124 | |
| FR-005 error strip, not covering the feed | Yes | T090 | |
| FR-006 unconfigured machine cannot reach shell | Yes | T075, T077, T096, T103 | |
| FR-007 setup authorised by IT/Developer only | Yes | T095, T099, T100 | |
| FR-008 every non-completion route ends the app | Yes | T078, T096, T102, T127, T174 | |
| FR-009 removed/revoked/unreadable ⇒ unconfigured | Yes | T075, T097, T101 | |
| FR-010 store clock judges sessions | Yes | T044, T107, T111 | |
| FR-011 sign-out clears the session | Yes | T044, T107, T111, T172, T174 | |
| FR-012 five-attempt limit survives restart | Yes | T106, T110, T173 | |
| FR-013 password step after temp credential | Yes | T115, T173 | |
| FR-014 remembered sign-in encrypted, safe fallback | Yes | T108, T113, T117 | |
| FR-015 unreadable hardware identity admitted | Yes | T112 | |
| FR-016 offer to repeat the failed work | Yes | T121, T125 | |
| FR-017 reset only where it could help | Yes | T119, T122, T124 | |
| FR-018 reset previews and stays on this machine | Yes | T120, T122, T126 | |
| FR-019 silent repair | Yes | T121, T123 | |
| FR-020 repeat only the failed work and after | Yes | T077, T082, T121, T125 | |
| FR-021 every diagnostic reaches the store | Yes | T047, T048, T055, T064 | |
| FR-022 identity/machine facts read-only | Yes | T035–T038, T040 | |
| FR-023 preferences held against the person | Yes | T138, T140, T141, T143, T144, T145 | |
| FR-024 plant list changeable by two roles | Yes | T137, T142, T146 | |
| FR-025 only store-reach stays local | Yes | T155, T156, T157 | |
| FR-026 picture copy cannot stop the launch | Yes | T081, T083, T088 | |
| FR-027 verdict settled before screens | Yes | T084, T089 | |
| FR-028 replaced behaviour fails the build | Yes | T001, T169 | |
| FR-029 session length changeable, gated | Yes | T137, T147 | |
| FR-030 role restrictions unchanged | Yes | T139, T149 | |
| FR-031 sweep cases recorded, none dropped | Yes | T159, T160, T167 | |
| FR-032 fault recorded whole incl. chain | Yes | T047, T175, T180 | |
| FR-033 stable fingerprint | Yes | T047, T048, T176, T181 | |
| FR-034 version + gathered runtime context | Yes | T177, T182 | |
| FR-035 action/view/control | Yes | T064, T177, T182 | |
| FR-036 provider diagnostics, no secrets | Yes | T177, T178, T182 | |
| FR-037 recording never hides the fault | Yes | T065, T179, T180 | |
| FR-038 panel copy as pasteable text | Yes | T131, T183, T184 | |
| SC-001 no silent stop | Yes | T085, T168, T174 | |
| SC-002 no wait past its maximum, 30 s ceiling | Yes | T074, T084, T168 | |
| SC-003 released build records entries | Yes | T047, T130 | |
| SC-004 zero sequences reach the shell | Yes | T096, T161 | |
| SC-005 filter by machine+kind and severity+machine | Yes | T129, T131 | |
| SC-006 every route out ends the app | Yes | T096 | |
| SC-007 attempt limit across restart | Yes | T106 | |
| SC-008 no preference lost on another computer | Yes | T138 | |
| SC-009 build fails on reintroduction | Yes | T169 | |
| SC-010 no useless remedy offered | Yes | T119 | |
| SC-011 session-length change takes effect next sign-in | Yes | T111, T137 | |
| SC-012 role restrictions verified one at a time | Yes | T137, T139, T149 | |
| SC-013 every case tested or retired with reason | Yes | T159, T160, T167, T171–T174 | |
| SC-014 repeats group as one | Yes | T049, T131, T181 | |
| SC-015 provider code present, no parameter values | Yes | T182 | |
| SC-016 dropped, not local, fault reaches caller | Yes | T065, T130, T179, T182 | |
| SC-017 copy one entry and paste it whole | Yes | T183, T184 | |

## Constitution Alignment Issues

None. Every principle and constraint is either satisfied or carries a recorded deviation, and the one required deviation is recorded exactly where the Review rule demands it:

- **I, IV, V, VI, VII, II, External Integration & Cache Boundaries, Documentation & Extensibility** — no conflicting requirement found in the 38 FRs, the 17 SCs or the task set. The `MTM_Waitlist.Mock.Service` host split (I26) and the `StartupDebugLog` cache path are logging-scope questions, not data-substitution or cache-boundary questions.
- **III (Stored-Procedure-First)** — satisfied; the G3 element (`update_table_descriptions.sql`) is now covered, the nine-plus-one deletions exist as artifacts, and the new procedures ship `create.sql`/`rollback.sql`. The only residue is the count phrasing in T024 (I30).
- **Additional: Security & Secrets** — the DPAPI departure is recorded in `plan.md` → Complexity Tracking, `research.md` D12 and `data-model.md` E5. That is the sole deviation and it is not left as an unqualified PASS.
- **Review rule** — satisfied: one deviation, with rationale and rejected alternatives, inside the plan rather than in a note beside it.

## Unmapped Tasks

184 tasks exist. Every FR and SC has at least one task. The following carry no single-requirement citation because they are removal, infrastructure, registry, documentation or verification work; they are legitimate and no action is needed:

T002, T003, T005–T011, T015–T019, T020–T024, T026–T029, T030–T034, T041, T043, T050, T051, T054, T056–T063, T066, T068, T069, T078, T079, T093, T094, T104, T105, T118, T128, T133–T136, T148, T150–T154, T158, T161–T166, T168, T170.

(T160, previously unmapped, now cites FR-031/SC-013. T170 is listed here for its citation range, not for a missing mapping — see I27.)

## Metrics

- **Total Requirements**: 55 (38 FR + 17 SC)
- **Total Tasks**: 184 (T001–T184, no duplicates, no gaps in the sequence)
- **Coverage %**: 100% (55 of 55 requirements have ≥1 task)
- **Ambiguity Count**: 0
- **Duplication Count**: 1
- **Coverage-gap Count**: 0
- **Inconsistency Count**: 11
- **Underspecification Count**: 1
- **Critical Issues Count**: 0

## Next Actions

No CRITICAL or HIGH finding exists, so `/speckit.implement` is not blocked. Four MEDIUM findings are worth clearing first because each would send an implementer to the wrong file or leave work undone:

1. **I22** — one-line correction in `data-model.md` E6 (four keys, not four-plus-one).
2. **U5** — correct T137's test path to `MTM_Waitlist.Tests/Module_Settings/SettingsViewModelTests.cs`; the migration map already says this must happen before T137 runs.
3. **I23** — change "fourteen" to "fifteen" in T018 and checklist 2.5g (or name the kept key), so one tooltip key does not survive the removal.
4. **I24** — record in `research.md` or `plan.md` that the brief's S3.2 "core services" entry is superseded by FR-025, and name the two services in T024.

The nine LOW findings are documentation hygiene; fix them opportunistically when the file next changes (I25, I26, I31 and I32 are one-line edits; I27, I28, I29, I30 and D1 reword an existing line).

Manual edits are appropriate for all of these — none needs a new `/speckit.specify` or `/speckit.plan` pass, since no requirement, entity or task boundary changes.

Would you like me to suggest concrete remediation edits for the top four (I22, U5, I23, I24)? I will not apply them; this pass is read-only and wrote no file.

## Extension Hooks

**Optional Pre-Hook** (`.specify/extensions.yml` → `hooks.before_analyze`), not executed because this pass is read-only:

```text
Command: /speckit.git.commit
Description: Auto-commit before analysis
Prompt: Commit outstanding changes before analysis?
To execute: /speckit.git.commit
```

**Optional Hook** (`.specify/extensions.yml` → `hooks.after_analyze`):

```text
Command: /speckit.git.commit
Description: Auto-commit after analysis
Prompt: Commit analysis results?
To execute: /speckit.git.commit
```

Nothing needed from you: this pass wrote no file.

## Machine summary

U5|MEDIUM|Underspecification|tasks.md T137:428|T137 names MTM_Waitlist.Tests/Module_Settings/ViewModels/SettingsViewModelTests.cs, which does not exist (real path has no ViewModels/ folder); the migration map already required this correction
I22|MEDIUM|Inconsistency|data-model.md:232-233 (E6) vs :244 (E7); plan.md:70; machine-configuration-contract.md §5|E6 counts five permission keys ("four new keys and the one new edit key") where E7, plan.md and both SQL/configuration contracts settle on four
I23|MEDIUM|Inconsistency|tasks.md T018; STARTUP-REBUILD-CHECKLIST.md:105; STARTUP-REBUILD-BRIEF.md S3.1|"fourteen Startup_* tooltip keys" but 15 exist in each of the two tooltip resw files, so a deletion of fourteen leaves one behind
I24|MEDIUM|Inconsistency|STARTUP-REBUILD-BRIEF.md:82 (S3.2); tasks.md T150/T151/T024|Brief lists ILocalSettingsService and IFileService as must-survive while T150/T151 delete both; no artifact records the supersession and T024's protected-list check omits them
I25|LOW|Inconsistency|STARTUP-REBUILD-BRIEF.md:61 (S3.1)|Brief S3.1 says 491 StartupDebugLog call sites; brief S9 and a read-only count both give 489 across 83 files
I26|LOW|Inconsistency|plan.md:133; research.md:68|plan.md's tree and research D5 present all 226 ILogger call sites as provider-served, while T066/T070/logging-contract §4 scope 64 of them to MTM_Waitlist.Mock.Service, which the provider does not serve
I27|LOW|Inconsistency|tasks.md T170:512|The single Success-Criteria validation run cites only FR-001–FR-030 and SC-001–SC-012, omitting FR-031–FR-038 and SC-013–SC-017
I28|LOW|Inconsistency|local-state-test-migration.md §4 item 5; tasks.md T152|The map still declares the two LocalSettingsOptions test files uncovered, but T152 now names both files and both method names
I29|LOW|Inconsistency|tasks.md Phase 2 Files; tasks.md Phase 9 Files; tasks.md T026|Phase 2's file list omits capabilities/startup-diagnostics/ (deleted by T026) while Phase 9's list names that already-deleted folder
I30|LOW|Inconsistency|tasks.md T024:113; STARTUP-REBUILD-BRIEF.md S3.2; STARTUP-REBUILD-CHECKLIST.md:18|T024's "twelve shared procedures, four shared seeds" does not re-derive from brief S3.2 (11 procedures plus fn_server_utc_now; 3 seeds plus 5 aggregate files)
I31|LOW|Inconsistency|plan.md:229; capabilities/startup/spec.md:118; spec.md FR-001|plan.md carries "one destination out of three" forward as satisfied while FR-001 and LaunchOutcome define five terminal outcomes
I32|LOW|Inconsistency|tasks.md Scale note; plan.md Scale note and Scale/Scope|Per-direction database counts (2 tables + 10 procedures added; 3 tables + 10 procedures deleted = 26) appear only in plan.md's Scale/Scope; the plan's Scale note and tasks.md's preamble give the deletion direction alone
D1|LOW|Duplication|tasks.md T159:483-487; tasks.md T171–T174:495-508|T159's "one test per case" covers every case that still applies, overlapping T171–T174's four named spec cases, so two tasks can own the same tests
COUNTS|critical=0|high=0|medium=4|low=9|total=13