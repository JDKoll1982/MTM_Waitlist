# Specification Analysis Report — Startup Rebuild

Feature: `010-startup-rebuild` · Analysed: 2026-09-26 · Constitution: v1.2.0  
Artifacts: `spec.md`, `plan.md`, `tasks.md`, `research.md`, `data-model.md`, `contracts/*`, `checklists/requirements.md`

Scope note: this report is produced by `/speckit.analyze` (read-only) and consumed by  
`/speckit.fix-findings.run`. Severities follow the analyze heuristic (CRITICAL / HIGH / MEDIUM / LOW).  
The fix-findings buckets are: CRITICAL → Critical, HIGH and MEDIUM → Warning, LOW → Info.

---

## Specification Analysis Report

| ID                                                                                                                                                                                                                                                                                                                                                                                         | Category           | Severity | Location(s)                                                                            | Summary                                                                                                                                                                                                                                                                                                                                                                         | Recommendation                                                                                                                                                            |
| ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------------------ | -------- | -------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| I1 - Use Recommended                                                                                                                                                                                                                                                                                                                                                                       | Inconsistency      | HIGH     | tasks.md T053, T054 · contracts/machine-configuration-contract.md §5 · research.md D11 | T053 and T054 baseline and grant **all four** new permission keys to `IT Department` **and** `Developer`. The contract (§5 table) and D11 give `permission.settings.log_panel` to `Developer` **alone** ("US5 and SC-005 both describe a developer reading one machine's history"). Executing T054 as written over-grants a developer-diagnostics gate.                         | Restate T053/T054 role-explicitly: `log_panel` = `Developer` only; `machine_configuration`, `ignored_locations_edit`, `session_length` = `IT Department` and `Developer`. |
| I2 - Reconcile the plan's constraint sentence with the documented user-story ordering                                                                                                                                                                                                                                                                                                      | Inconsistency      | MEDIUM   | plan.md "Delivery phases" preamble · tasks.md "Dependencies & Execution Order"         | plan.md says "The order below is the owner's checklist order. `tasks.md` must follow it rather than invent its own." tasks.md is ordered by **user-story priority** and states the divergence, citing "as the specification requires". One artifact forbids what the other does.                                                                                                | Reconcile the plan's constraint sentence with the documented user-story ordering (or record the ordering rule in one place only).                                         |
| G1 - add a task per uncovered case.                                                                                                                                                                                                                                                                                                                                                        | Coverage gap       | MEDIUM   | spec.md "Edge Cases" (17 bullets) · tasks.md T159, T167 · FR-031, SC-013               | No task maps the spec's **own** Edge Cases list. T159/T167/FR-031 are written around the brief's S13 sweeps. Four spec bullets have no task at all: credential reset during sign-in; credential reset while signed in elsewhere (session not ended); an account still on the legacy temporary value; a sign-out whose restart fails.                                            | Extend T159's list (and T167's exclusions record) to the spec's Edge Cases, or add a task per uncovered case.                                                             |
| U1 - Use Recommended                                                                                                                                                                                                                                                                                                                                                                       | Underspecification | MEDIUM   | contracts/machine-configuration-contract.md §1, §4 · tasks.md T075, T123               | Ownership of the targeted reset is ambiguous. The contract puts `ResetToDefaultsAsync` on `IMachineConfigurationService`; T075 gives `MachineConfigurationService` "the reset that touches this machine's configuration only"; T123 gives "the targeted reset and the silent repair" to `StartupRecoveryService`. Two services are each described as performing the same reset. | Name one owner for the reset and state what the other service does instead.                                                                                               |
| I3 - add the severity+machine case to T129                                                                                                                                                                                                                                                                                                                                                 | Inconsistency      | MEDIUM   | spec.md US5 scenario 2 · spec.md SC-005 · tasks.md T129                                | The filter pair drifts: US5 scenario 2 and T129 use "machine and **error kind** together"; SC-005 says "filtered by **severity** and machine". The contract supports both, so the wording, not the behaviour, is at fault.                                                                                                                                                      | Align SC-005's wording, or add the severity+machine case to T129.                                                                                                         |
| I4 - Correct T013 to Wave 15                                                                                                                                                                                                                                                                                                                                                               | Inconsistency      | LOW      | tasks.md T013                                                                          | Cross-reference is wrong. T013 says `StartupState` stays "until **Wave 11** deletes it"; deletion is T042, in Wave 15 ("⟶ Wait for Wave 14 to finish, then:"), and the Dependencies summary says "W15 `StartupState` deleted and confirmed". Wave 11 is the automation proof.                                                                                                   | Correct T013 to Wave 15, or drop the wave number.                                                                                                                         |
| I5 - Create and validate a table in md format houseing all test files slated for deletion, a section for each test file we are keeping, for each file include a table for what test methods from that file need to be removed with the starting and stoping line for each method, and in another table add what tests these files must have added to them and what task it will be done on | Inconsistency      | LOW      | plan.md Scope · tasks.md T157 · S11.5.5                                                | The local-settings migration size differs across artifacts: plan says "18 test files with 115 local-settings references", T157 says "the twelve behaviour-test files listed in S11.5.5". The tree holds 41 files naming `LocalSettings`, of which ~20 are test files.                                                                                                           | State one authoritative list/count and use it in both artifacts.                                                                                                          |
| I6 - Use Recommendation                                                                                                                                                                                                                                                                                                                                                                    | Inconsistency      | LOW      | plan.md Scope · tasks.md T066, T070                                                    | The "226 existing `ILogger` calls" figure is not reproducible: the tree holds 219 `_logger.` call sites across 23 files (19 of them in `MTM_Waitlist.Mock.Service`, a separate host that may not take the provider).                                                                                                                                                            | Re-derive the count before T070's confirmation is ticked.                                                                                                                 |
| G2 - Add the DI extension file to T017's outputs                                                                                                                                                                                                                                                                                                                                           | Coverage gap       | LOW      | tasks.md T017, T067 · plan.md project tree                                             | T067 registers the logging module via `MTM_Waitlist.Logging/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs`, but no task creates that file: T017 creates the project, its solution entry and the project references only.                                                                                                                                  | Add the DI extension file to T017's outputs, or make it explicit in T067.                                                                                                 |
| A1 - Reword the title                                                                                                                                                                                                                                                                                                                                                                      | Ambiguity          | LOW      | research.md D11 (title vs body)                                                        | The title reads "Three surfaces need a new catalogue key, and the fourth does not", but the body adds all four as new keys (the fourth being the new `_edit` key from D10). The title contradicts the decision.                                                                                                                                                                 | Reword the title.                                                                                                                                                         |
| I7 - Use Recommendation                                                                                                                                                                                                                                                                                                                                                                    | Inconsistency      | LOW      | plan.md project tree · data-model.md E3 · tasks.md T051                                | The plan's tree annotates `Tables/02_core_computers_registry/` as "unchanged, now carries the machine configuration". data-model.md E3 and T051 split it: identity in `core_computers_registry`, picture sources in `config_images_locations` at `computer` scope, "rather than columns on the computer registry".                                                              | Adjust the plan's tree comment.                                                                                                                                           |
| I8 - Record in T139/T149 that SC-012 pins the *read* restriction for ignored locations, with the write-narrowing as a named, deliberate exception.                                                                                                                                                                                                                                        | Inconsistency      | MEDIUM   | spec.md SC-012 · research.md D10 · tasks.md T139, T149                                 | D10 keeps read access on the existing `permission.settings.ignored_locations` key and adds a two-role `_edit` key, so the **write** restriction on that setting narrows from seven roles to two. SC-012 requires every role-restricted setting to keep the same roles and T139 verifies it "one setting at a time"; as written the inventory can fail on this one setting.      | Record in T139/T149 that SC-012 pins the *read* restriction for ignored locations, with the write-narrowing as a named, deliberate exception.                             |

---

## Coverage Summary Table

Every Functional Requirement and every Success Criterion has at least one task. Coverage is 44/44 (100%).

| Requirement Key                                          | Has Task? | Task IDs                                             | Notes                                                                      |
| -------------------------------------------------------- | --------- | ---------------------------------------------------- | -------------------------------------------------------------------------- |
| FR-001 launch ends at one destination                    | Yes       | T077                                                 |                                                                            |
| FR-002 name each piece of work first                     | Yes       | T071, T072, T073, T074, T076, T080, T085, T087, T116 |                                                                            |
| FR-003 every wait has a stated maximum                   | Yes       | T073, T074, T084                                     |                                                                            |
| FR-004 a stop states its cause                           | Yes       | T085, T087, T090, T124                               |                                                                            |
| FR-005 error strip at the bottom                         | Yes       | T090                                                 |                                                                            |
| FR-006 unconfigured machine cannot reach the shell       | Yes       | T077, T096, T103                                     |                                                                            |
| FR-007 setup authorised by two roles only                | Yes       | T095, T099, T100                                     | Affected by I1 (baseline wording)                                          |
| FR-008 every non-completion exit ends the process        | Yes       | T096, T102, T127                                     |                                                                            |
| FR-009 removed/revoked/unreadable ⇒ unconfigured         | Yes       | T097, T101                                           |                                                                            |
| FR-010 session judged on the store clock                 | Yes       | T044, T107, T111                                     |                                                                            |
| FR-011 session cleared on sign-out                       | Yes       | T044, T107, T111                                     |                                                                            |
| FR-012 five failed attempts, survives restart            | Yes       | T106, T110                                           |                                                                            |
| FR-013 forced password change after acceptance           | Yes       | T115                                                 |                                                                            |
| FR-014 remembered sign-in encrypted in the store         | Yes       | T108, T113, T117                                     |                                                                            |
| FR-015 unreadable hardware identity admits               | Yes       | T112                                                 |                                                                            |
| FR-016 a stop offers a repeat                            | Yes       | T121, T125                                           |                                                                            |
| FR-017 reset only where it could remove the cause        | Yes       | T119, T122, T124                                     |                                                                            |
| FR-018 reset states and confines itself                  | Yes       | T120, T122, T123, T126                               | Affected by U1 (reset ownership)                                           |
| FR-019 repairable faults repaired silently               | Yes       | T121, T123                                           | Affected by U1                                                             |
| FR-020 retry repeats only the failed work                | Yes       | T082, T121, T125                                     |                                                                            |
| FR-021 every diagnostic written to the store             | Yes       | T047, T048, T055, T064                               |                                                                            |
| FR-022 identity and machine facts read-only              | Yes       | T035–T039                                            | No task *asserts* read-only beyond the contract shape; noted, not raised - |
| FR-023 preferences held against the person               | Yes       | T138, T140, T141, T143, T144, T145                   |                                                                            |
| FR-024 locations list changeable by two roles            | Yes       | T137, T142, T146                                     | Affected by I1, I8                                                         |
| FR-025 nothing but store access stays local              | Yes       | T155, T156                                           |                                                                            |
| FR-026 picture copy is best effort                       | Yes       | T083, T088                                           |                                                                            |
| FR-027 external reachability settled before screens      | Yes       | T084, T089                                           |                                                                            |
| FR-028 replaced behaviour cannot return                  | Yes       | T001, T004, T005, T169                               |                                                                            |
| FR-029 session length changeable from settings           | Yes       | T147                                                 | Affected by I1                                                             |
| FR-030 existing role restrictions preserved              | Yes       | T139, T149                                           | Affected by I8                                                             |
| FR-031 edge cases recorded as the acceptance surface     | Yes       | T159, T167                                           | Affected by G1                                                             |
| SC-001 a stop is never silent                            | Yes       | T168                                                 |                                                                            |
| SC-002 no wait exceeds its maximum                       | Yes       | T074, T168                                           |                                                                            |
| SC-003 a release build records entries                   | Yes       | T130                                                 |                                                                            |
| SC-004 zero input sequences reach the shell unconfigured | Yes       | T096, T161                                           |                                                                            |
| SC-005 a fault history found in under a minute           | Yes       | T129                                                 | Affected by I3                                                             |
| SC-006 every tested setup exit ends the process          | Yes       | T096                                                 |                                                                            |
| SC-007 the five-attempt limit holds across restart       | Yes       | T106                                                 |                                                                            |
| SC-008 no preference lost across computers               | Yes       | T138                                                 |                                                                            |
| SC-009 a build fails if replaced behaviour returns       | Yes       | T160, T169                                           |                                                                            |
| SC-010 no useless remedy is offered                      | Yes       | T119                                                 |                                                                            |
| SC-011 a session-length change lands at next sign-in     | Yes       | T111                                                 |                                                                            |
| SC-012 every role-restricted setting unchanged           | Yes       | T137, T139, T149                                     | Affected by I8                                                             |
| SC-013 every acceptance case covered or retired          | Yes       | T159, T167                                           | Affected by G1                                                             |

---

## Constitution Alignment Issues

None found. Each of the seven principles and the three additional constraints was checked against the three  
artifacts and the design decisions in `research.md`:

- **I. Spec-First, Verified Delivery** — spec precedes plan precedes tasks; every gate demands build or test proof.
- **II. Live Data Integrity** — all seven preference migrations target `config_settings_values`; no mock mode is  
introduced; `MockServiceClient` is retained as a deployment endpoint, not a data substitution.
- **III. Stored-Procedure-First** — every new data operation is a procedure; new artifacts ship  
`create.sql` + `rollback.sql` and their aggregate entries; `InlineSqlAuditTests` is the gate.
- **IV. MCP-First** — plan names the two areas that must consult documentation before code (WinUI 3 surfaces;  
`System.Security.Cryptography` AES plus the `ILogger` provider pattern) as tasks.
- **V. WinUI 3 Conformance** — new views live under `Module_Startup/Views` and `Module_Settings/Views`, strings are  
localised, and automation ids are required on the placeholder and setup windows.
- **VI. Evidence-Based Gates** — the build and suite commands are the phase gates; `WMC9999` is treated as a masked  
XAML error.
- **VII. Readable Delivery** — every phase reports in plain English; the outstanding-action rule is respected.
- **Security & Secrets** — the key file is read-only, never written, rotated or logged; T055 proves it.
- **External Integration & Cache Boundaries** — Infor Visual stays read-only with a bounded probe; no AI endpoint.
- **Documentation & Extensibility** — T162–T166 update the embed guide, registry, changelog, README and instructions.

---

## Unmapped Tasks

Tasks that map to no single requirement, all legitimate and clustered in Setup, Polish and the database  
aggregates: T002, T003, T020–T024, T026–T034, T056–T063, T070, T158, T160, T164, T165, T166. No action needed.

---

## Metrics

- Total Requirements: **44** (31 FR + 13 SC)
- Total Tasks: **170**
- Coverage: **100%** (44/44 requirements have ≥1 task)
- Ambiguity Count: **1** (A1)
- Duplication Count: **0**
- Coverage-gap Count: **2** (G1, G2)
- Inconsistency Count: **7** (I1–I8)
- Underspecification Count: **1** (U1)
- Critical Issues Count: **0**

---

## Next Actions

- **No CRITICAL issue exists.** The artifacts are internally consistent enough to begin implementation.
- **Resolve I1 before Phase 7 (US5) is implemented** — it changes which roles hold `permission.settings.log_panel`,  
and a wrong baseline is a store change that has to be rolled back.
- **Resolve I8 and G1 before T139/T159 are executed**, because both change what those tasks must assert.
- **U1 needs a one-line design decision** (who owns the targeted reset) before T075 and T123 are written.
- Everything else is wording or count drift and can be corrected opportunistically.

Artifact edits are outside this extension's remit (it may change implementation code only, never `spec.md`,  
`plan.md` or `tasks.md`), so the findings above are recorded as deferred rather than applied. All of I1–I8, G1, G2,  
U1 and A1 need an edit to a spec artifact or a design decision, and no implementation code exists yet for this  
feature — the working tree still holds the old startup surface and `MTM_Waitlist.Logging` has not been created.

---

## Next Actions for the person

1. Decide the `permission.settings.log_panel` role set (I1) and correct T053/T054.
2. Decide who owns the targeted machine-configuration reset (U1) and correct T075/T123.
3. Extend T159/T167 to the spec's Edge Cases list (G1) and scope T139/T149 to the read restriction (I8).
4. Correct the wording and count drift (I2–I7, G2, A1) in a single pass over `plan.md` and `tasks.md`.
