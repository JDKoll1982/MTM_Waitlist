# Specification Analysis Report — Startup Rebuild

Feature: `010-startup-rebuild` · Analysed: 2026-09-26 · Constitution verified: **v1.2.0** (`Ratified 2026-09-09 | Last Amended 2026-09-14`), 7 principles + 3 additional constraints (Security & Secrets; External Integration & Cache Boundaries; Documentation & Extensibility)
Artifacts read: `spec.md`, `plan.md`, `tasks.md` (core, all present), `research.md`, `data-model.md`, `contracts/{launch-step,identity,logging,machine-configuration,sql}-contracts.md`, `checklists/requirements.md`, `local-state-test-migration.md`, `MTM_Waitlist_Exception_Logging_Reference.md`, `.specify/memory/constitution.md`.
Scope: read-only cross-artifact consistency pass. `analysis.md`, `analysis-pre-fix.md` and `analysis-after-fixes.md` were treated as history only, and every prior finding was re-verified from the current text.
ID policy: prior IDs (`I1`–`I12`, `G1`–`G2`, `U1`, `A1`) are reserved for the verification section, so new findings continue the series (`C1`, `G3`–`G4`, `I13`–`I21`, `U3`–`U4`, `A2`).

## Specification Analysis Report

| ID | Category | Severity | Location(s) | Summary | Recommendation |
|----|----------|----------|-------------|---------|----------------|
| C1 | Constitution | CRITICAL | `plan.md` Constitution Check row "Additional: Security & Secrets" · `spec.md` FR-014 and Assumptions ("The means to decrypt a remembered sign-in already exists as a shared secret file") · `data-model.md` E5 (`payload_ciphertext VARBINARY(512)`, `payload_iv`, `key_fingerprint`) · `research.md` D12/D13 · `tasks.md` T046, T113 | The remembered sign-in is a stored, decryption-capable credential (AES-256 with a 32-byte key read from `\\mtmanu-fs01\...\MTM_AUTH_USER_SECRET_KEY.txt`) held in the new `auth_remembered_sign_ins` table. The constitution's Security & Secrets constraint requires credentials to be "stored DPAPI-protected (`ProtectedData`, `CurrentUser` scope) and never displayed in full". The plan asserts **PASS** for that constraint, describes the mechanism without addressing the DPAPI clause, and then states "No constitution violation is necessary, so there is no Complexity Tracking table" — so the deviation is reasoned only in `research.md` D12's "Alternatives considered", not recorded where the constitution's Review rule requires it. `grep DPAPI|ProtectedData` across the feature artifacts finds only two hits, both *rejected* alternatives (D12(c), D27); the constitution's clause is never cited. | Record the deviation in `plan.md` → Complexity Tracking (DPAPI is per-user/per-machine and cannot protect a value another machine must decrypt, so a store-held AES payload was chosen deliberately), or amend the constraint via `/speckit.constitution`. Either way, do not leave the `Security & Secrets` **PASS** unqualified. |
| G3 | Coverage gap | HIGH | `tasks.md` T057, T060, T062 (no `update_table_descriptions` mention anywhere in tasks) · `Database/Database-Ruleset.md:134` ("mandatory whenever any SQL artifact is added, modified, or removed") and `:159` ("Retired objects" sections) · constitution III · `contracts/sql-contracts.md` §5 | No task keeps `Database/Bootstrap/update_table_descriptions.sql` in sync, although the repo ruleset makes it mandatory and the constitution names it beside the master lists. This feature adds 2 tables and ~10 procedures, amends `ops_startup_logs`, deletes 3 tables and 10 procedures, and rewrites seeds and validation scripts. The file exists (`Database/Bootstrap/update_table_descriptions.sql`), and `sql-contracts.md` §5 lists it only as an artifact *not to delete*. Searching `update_table_descriptions` across `tasks.md`, `plan.md`, `contracts/*.md` and `data-model.md` returns **one** hit, and it is the protected-list line in `sql-contracts.md:214`. | Add the description-file update (including the retired-objects sections) to T057/T062, or make it its own database task. |
| G4 | Coverage gap | MEDIUM | `capabilities/startup-diagnostics/spec.md` · `capabilities/DRIFT.md:31` (registry matches `MTM_Waitlist.Startup/Services/StartupLog*.cs`) · `tasks.md` T026, T133, T163 | The `startup-diagnostics` capability exists on disk and in the drift note's registry description, and its matched files (`StartupLogService.cs`, `StartupLogForwarder.cs`) are deleted by T133 — but no task retires the folder or its registry/DRIFT rows. `grep startup-diagnostics` across all `specs/010-startup-rebuild/*.md` returns **zero** hits; T026 deletes only `capabilities/startup/`, and T163's file list names only `capabilities/startup-launch/spec.md`, `living-specs.yml` and `capabilities/DRIFT.md`. | Add `capabilities/startup-diagnostics/` (folder, registry row, `DRIFT.md` row) to T026 or T163. |
| I13 | Inconsistency | MEDIUM | `research.md` D9 line 117 · `contracts/sql-contracts.md` §5 line 212 · `data-model.md` E10 ("`sp_config_settings_values_get|upsert`") · `tasks.md` T140–T147 | The settings write procedure is named `sp_config_settings_values_upsert`, which does not exist. The tree holds `sp_config_settings_upsert` (called at `MTM_Waitlist.Settings/Services/ConfigSettingsValueService.cs:93`), plus `sp_config_settings_values_get` and `sp_config_settings_values_delete`; a `Database`-wide search for `sp_config_settings\w*` returns exactly those four names. D9's rationale ("the scoped store, its scope rank function and its procedures already exist") and the seven preference migrations rest on the wrong name. | Correct the name in D9, `sql-contracts.md` §5 and `data-model.md` E10 (`sp_config_settings_upsert`), or write "the settings upsert procedure". |
| I14 | Inconsistency | MEDIUM | `tasks.md` T023 (`Database/AllFunct.sql`), T057 (`Database/AllTables.sql`), T060 (`Database/AllTables.sql`), T062 (`Database/AllTables.sql`, `Database/AllFunct.sql`, `Database/AllViews.sql`) · `plan.md:184` · `data-model.md` E10 · `contracts/sql-contracts.md` §5 | Three of the five hand-maintained aggregate paths are wrong. Verified by listing `Database/**/All*.sql`: `AllTables.sql` is at `Database/Tables/`, `AllFunct.sql` at `Database/Functions/`, `AllViews.sql` at `Database/Views/`; only `Database/StoredProcedures/AllSPs.sql` and `Database/Seeds/AllSeeds.sql` are cited correctly. `Test-Path Database/AllTables.sql`, `Database/AllFunct.sql`, `Database/AllViews.sql` all return **False**; the correct files exist elsewhere. | Correct the three paths in `tasks.md` (T023, T057, T060, T062) and `plan.md`'s Database tree. |
| I18 | Inconsistency | MEDIUM | `plan.md` "Scale/Scope" ("a test surface of six deletions and eleven re-points") · `local-state-test-migration.md` §1B header ("Five of these are also in §1A") and its `In §1A?` column (four `yes`) · `:§4` ("T025 covers five of the six §1A deletions") · `:§5` | The migration map's own arithmetic does not reconcile. §1B's table marks exactly four rows `yes` (StartupCoordinatorTests, StartupRecoveryServiceTests, SplashViewModelTests, SignOutServiceTests) while its header says five; §4 says T025 covers five of §1A's six, but T157 covers one (`LocalSettingsServiceTests`) and T134 covers one (`StartupLogServiceTests`), leaving T025 four — 4+1+1=6. Distinct deleted test files are 11 (§1A ∪ §1B), not the plan's six. Independently checked: `ILocalSettingsService|LocalSettingsService|LocalSettingsOptions|SettingsStorageExtensions` across `MTM_Waitlist.Tests/**` returns **20 files / 233 matches**, matching §4's own mechanical figure. | State the distinct totals (11 deleted, 11 kept) in `plan.md`, and fix §1B's header and §4's "five" to four. |
| I15 | Inconsistency | LOW | `tasks.md` T159 ("record the seventeen cases in `spec.md`'s Edge Cases section") vs `spec.md` lines 157–192 | The spec's Edge Cases section holds **23** bullets (counted with `^- ` between `## Edge Cases` at line 155 and `## Requirements` at line 194). "Seventeen" reproduces neither 23 nor any stated subset (the only obvious subset — the six logging bullets — is never named). No task names the bullet "The launch window is closed while a launch is running", so a 17-case catalogue can silently drop cases that FR-031 says may not be dropped silently. | Re-derive the count and either state 23 or define the subset explicitly; confirm each bullet is in T159's catalogue. |
| I16 | Inconsistency | LOW | `spec.md` FR-001 (four outcomes) · `contracts/launch-step-contract.md` §5 and `tasks.md` T077 (five: `…Blocked, Ended`) · `data-model.md` E9 ("Six terminating outcomes … a completed configuration") | The terminal-outcome set is enumerated three different ways: FR-001's four (main screens, sign-in, machine setup, a stated stop), the contract enum's five (`LaunchOutcome` adds `Ended`), and data-model's six (adds "a completed configuration", which the state machine treats as a transition `MachineSetup → Configured`, not a terminus). | Adopt the contract's enum as the single statement and make FR-001's and data-model's prose agree with it. |
| I17 | Inconsistency | LOW | `tasks.md` T066 and T070 · `contracts/logging-contract.md` §4 | The "226 call sites across 26 files (219 `_logger.` plus 7 `logger.`/`Logger.`)" figure re-derives exactly (measured: `_logger.` = **219** across **23** files; non-underscore `logger.`/`Logger.` real call sites = **7** across **3** files), but its composition contradicts T066's own exclusion: 58 of the 219 `_logger.` sites sit in `MTM_Waitlist.Mock.Service` (BackupEngine 8, BackupScheduler 8, RefreshEngine 11, RestoreService 12, ServiceCapabilityProbe 9, ServiceApiHost 5, RefreshRunRecordRecorder 2, MySqlStoreConnectivityProbe 1, RunHistoryStore 1, VisualShapePayloadSource 1) and 6 of the 7 non-underscore sites are in that host, which the same task calls "a separate host with its own file logger and is unaffected". The app-side figure the provider serves is 161. | State the scope of the 226 (tree-wide, including the unaffected host) or give the app-side figure T070 should confirm. |
| I19 | Inconsistency | LOW | `spec.md` SC-005 ("filtered by severity and machine") vs US5 scenario 2 ("filters by machine and by error kind together") · `tasks.md` T129 | Residual wording drift, left deliberately by the I3 fix (T129 now tests both pairs, so behaviour is covered either way). The spec still disagrees with itself, and `analysis-after-fixes.md` records it as the one open residual. | Align SC-005's wording with US5 scenario 2 (a `spec.md` edit owned by the requester). |
| I20 | Inconsistency | LOW | `tasks.md` T160 ("Confirm the suite holds no disabled, skipped or commented-out startup test (checklist 9.1c, SC-009)") vs `spec.md` SC-009 | T160 cites SC-009, which is "A build fails when any replaced launch behaviour is reintroduced" — the criterion T169 (retired-symbol audit) actually verifies. T160's own subject (deleted-not-disabled tests) is really the brief's S16 rule. | Point T160 at the criterion it verifies, or drop the SC reference. |
| I21 | Inconsistency | LOW | `tasks.md` T163 (premise "confirm the registry matches the tree") · `capabilities/DRIFT.md:31` ("The registry (`living-specs.yml` at the repo root)") · working tree | `living-specs.yml` is tracked (`git ls-files` lists it) but **deleted in the working tree**: `git status --short` reports ` D living-specs.yml`, and no root-level `.yml` file exists. T163's premise (an existing registry needing the deferred Phase 2 edit) does not hold as the tree stands. | Restore the registry before T163 runs, or restate T163 to create it. |
| U3 | Underspecification | LOW | `spec.md` FR-025 ("Nothing except what is needed to reach the store … apart from the local copy of pictures and the one reviewed exception") · `contracts/machine-configuration-contract.md` §6 · `tasks.md` T156 | FR-025 permits only what reaches *the store* (plus two named exceptions), but §6 and T156 also keep the Infor Visual connection string, which reaches Infor Visual rather than the store. The plan's own Constraints sentence ("only connection strings and the reviewed `MockServiceClient` exception stay on the machine") is coherent; the requirement's wording is the outlier. | Widen FR-025 to "what is needed to reach the store or the external read-only system", or record the retention as a third named exception. |
| U4 | Underspecification | LOW | `spec.md` US6 (five preferences) and SC-008 (four: "theme, waitlist order, alerts and seen-requests") · `data-model.md` E6 (five person rows) · `tasks.md` T138 and T145 | Five person-scoped preferences exist — `AppBackgroundRequestedTheme`, `Waitlist.SortOrder`, `User.NewRequestAlertsEnabled`, `Waitlist.MessageSeen`, `Setup.DunnageImageSearch.ShowPartsWithoutImages` — but SC-008 and T138 enumerate four, so the dunnage show-parts preference migrated by T145 has no "follows the person to another computer" assertion, while the plan calls the set "the seven scoped preferences". | Add the fifth to SC-008 and T138, or state why it is excluded from the portability criterion. |
| A2 | Ambiguity | LOW | `plan.md` "Scale/Scope" ("12 database artifacts added, repurposed or deleted") · `plan.md` Scale note and `tasks.md` preamble ("nine procedures deleted, three tables deleted") · `contracts/sql-contracts.md` §4 · `data-model.md` E10 | The Database arithmetic cannot be reconciled. Contracts §4 and data-model E10 delete **ten** procedures (nine startup-only plus `sp_core_buildings_upsert`, deleted by T061) and three tables, and add two tables plus ten procedures; "nine procedures deleted" is short by one, and "12" matches neither the enumerated total (26) nor the plan's own header (19 counting the four keys). Only "nine procedures + three tables deleted" = 12, which is not what the phrase says. | State the counts per direction (added / extended / deleted) explicitly, or drop the number. |

## Prior Findings Verification

- **I1** — **CLOSED.** `tasks.md` T053/T054 now name the roles explicitly: `machine_configuration`, `ignored_locations_edit`, `session_length` at 1 for `IT Department` and `Developer`, and `log_panel` "at 1 for `Developer` alone" (T053) / "to `Developer` alone" (T054), matching `contracts/machine-configuration-contract.md` §5 and `research.md` D11.
- **I2** — **CLOSED.** `plan.md`'s "Delivery phases" preamble now reads "…`tasks.md` carries its content, its phases and its gates without contradiction, but is sequenced by the specification's user-story priority; the three divergences that follow from that are named in its Dependencies & Execution Order section." The forbidding sentence ("must follow it rather than invent its own") is gone.
- **G1** — **CLOSED.** T171–T174 were added in Phase 9's second wave for the four spec cases (reset during sign-in; reset while signed in elsewhere; legacy temporary value; sign-out whose restart fails), and T159 now covers "the cases the three read-only sweeps found … as part of the same catalogue". The residual defect is the catalogue's stated size (raised as **I15**).
- **U1** — **CLOSED.** T075 now scopes `MachineConfigurationService` to "the reset that `IMachineConfigurationService.ResetToDefaultsAsync` performs … the mechanism, and the only code that writes these rows", and T123 gives `StartupRecoveryService` the policy and drives it "through `IMachineConfigurationService.ResetToDefaultsAsync` rather than writing configuration rows here, so the targeted reset has one implementation".
- **I3** — **CLOSED.** T129 now tests both pairs: "filtering by machine and by error kind together lists only matching entries, filtering by severity and machine together does the same". The underlying `spec.md` wording still disagrees with itself and is raised as **I19**.
- **I4** — **CLOSED.** T013 now reads "leaving `StartupState` standing until Wave 15 deletes it"; T042 sits in Foundational W15 and the Dependencies summary still says "W15 `StartupState` deleted and confirmed".
- **I5** — **STILL OPEN (partially).** The map exists, is cited from `plan.md` Scope and T025/T137/T139/T152/T157, and the "18 files / 115 references" figures are removed — but the map's own counts still disagree with each other (§1B header "Five of these are also in §1A" vs its table's four `yes` rows; §4 "T025 covers five of the six §1A deletions" vs four). Raised as **I18**.
- **I6** — **CLOSED.** The figure is reproducible: `_logger.` = **219** across **23** files, and non-underscore `logger.`/`Logger.` call sites = **7** across **3** files (App.xaml.cs ×3, ServiceOperatorAuthenticationHandler.cs ×3, ServiceLogTests.cs ×1) → 226 across 26 files. (Two further `logger.` matches are XML-doc words and three are `IsEnabled` assertions.) The scope question is raised as **I17**.
- **G2** — **CLOSED.** T017 now creates the project "with its solution entry and its dependency-injection extension file" and lists `MTM_Waitlist.Logging/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs` among its outputs.
- **A1** — **CLOSED.** `research.md` D11's title now reads "Four surfaces need a new catalogue key, and one existing key is deliberately not reused", matching its body.
- **I7** — **CLOSED.** `plan.md`'s tree now annotates `Tables/02_core_computers_registry/` as "unchanged in shape; carries the machine's identity, while the picture sources live in `config_images_locations` at computer scope", matching `data-model.md` E3 and T051.
- **I8** — **CLOSED.** T139 now records "For the ignored-locations list, SC-012 pins the read restriction — `permission.settings.ignored_locations`, whose grants do not change — and the narrowed write … is a named, deliberate exception (research D10)", and T149 repeats it.
- **I9** — **CLOSED.** `local-state-test-migration.md` §1C records the correction with line ranges (642–685, 43–64, 143–190) and states "The kept list is therefore eleven files, not twelve"; T025 cites §1C. Confirmed on disk: `MTM_Waitlist.Tests/ViewModels/LoginViewModelTests.cs` exists.
- **I10** — **CLOSED.** T137 now cites `MTM_Waitlist.Tests/Module_Settings/SettingsViewModelTests.cs`, which exists; the previously cited `Module_Settings/ViewModels/SettingsViewModelTests.cs` does not.
- **I11** — **CLOSED.** T152 now names `MTM_Waitlist.Tests/Core/Models/StartupOptionsModelsTests.cs` and `MTM_Waitlist.Tests/Models/StartupModelsTests.cs`; both exist.
- **I12** — **CLOSED.** `MTM_Waitlist_Exception_Logging_Reference.md` now opens with the reconciliation note and maps every value onto `ops_startup_logs` / `payload_json`, states "Every write goes through a stored procedure", "Nothing is written to the machine" (in-memory bounded queue, dropped on store refusal), and keeps the triage fields only "as a record of what was considered".

## Coverage Summary Table

55 requirements (38 FR + 17 SC); every one has ≥1 task. Coverage 55/55 = 100%.

| Requirement Key | Has Task? | Task IDs | Notes |
|-----------------|-----------|----------|-------|
| FR-001 launch ends at one destination | Yes | T077 | Enumerated three ways — see I16 |
| FR-002 name each piece of work first | Yes | T071, T072, T073, T074, T076, T080, T085, T087, T116 | |
| FR-003 every wait has a stated maximum | Yes | T071, T073, T074, T084 | |
| FR-004 a stop states its cause | Yes | T085, T087, T090, T124 | |
| FR-005 error strip at the bottom | Yes | T090 | |
| FR-006 unconfigured machine cannot reach the shell | Yes | T077, T096, T103 | |
| FR-007 setup authorised by two roles only | Yes | T095, T099, T100 | I1 closed: `log_panel` narrowed, `machine_configuration` unchanged |
| FR-008 every non-completion exit ends the process | Yes | T096, T102, T127 | |
| FR-009 removed/revoked/unreadable ⇒ unconfigured | Yes | T097, T101 | |
| FR-010 session judged on the store clock | Yes | T044, T107, T111 | |
| FR-011 session cleared on sign-out | Yes | T044, T107, T111, T174 | |
| FR-012 five failed attempts, survives restart | Yes | T106, T110, T173 | |
| FR-013 forced password change after acceptance | Yes | T115, T173 | |
| FR-014 remembered sign-in encrypted in the store | Yes | T108, T113, T117 | Mechanism conflicts with the DPAPI clause — see C1 |
| FR-015 unreadable hardware identity admits | Yes | T112 | |
| FR-016 a stop offers a repeat | Yes | T121, T125 | |
| FR-017 reset only where it could remove the cause | Yes | T119, T122, T124 | |
| FR-018 reset states and confines itself | Yes | T120, T122, T123, T126 | U1 closed |
| FR-019 repairable faults repaired silently | Yes | T121, T123 | |
| FR-020 retry repeats only the failed work | Yes | T082, T121, T125 | |
| FR-021 every diagnostic written to the store | Yes | T047, T048, T055, T064 | |
| FR-022 identity and machine facts read-only | Yes | T035, T036, T037, T038, T039, T042 | Contract shape only; no task asserts read-onlyness at runtime (noted, not raised) |
| FR-023 preferences held against the person | Yes | T138, T140, T141, T143, T144, T145 | SC-008/T138 cover four of five — see U4 |
| FR-024 locations list changeable by two roles | Yes | T052, T053, T137, T142, T146 | I8 closed (read key untouched, new edit key) |
| FR-025 nothing but store access stays local | Yes | T150, T151, T153, T155, T156 | See U3 (Infor Visual connection string) |
| FR-026 picture copy is best effort | Yes | T083, T088 | |
| FR-027 external reachability settled before screens | Yes | T084, T089 | |
| FR-028 replaced behaviour cannot return | Yes | T001, T004, T005, T169 | |
| FR-029 session length changeable from settings | Yes | T137, T147 | |
| FR-030 existing role restrictions preserved | Yes | T139, T149 | I8 closed |
| FR-031 edge cases recorded as the acceptance surface | Yes | T159, T167 | Catalogue size wrong — see I15 |
| FR-032 the fault recorded whole, chain included | Yes | T175, T180 | |
| FR-033 stable fingerprint on the entry | Yes | T047, T048, T176, T181 | |
| FR-034 version/runtime/machine context gathered by the seam | Yes | T177, T182 | `contracts/logging-contract.md` §1.3 carries the field list |
| FR-035 action, window, screen, control | Yes | T064, T177, T182 | |
| FR-036 store diagnostics, no values | Yes | T177, T178, T182 | |
| FR-037 recording never hides the fault | Yes | T065, T179, T180 | |
| FR-038 panel copy of one entry or the listed view | Yes | T131, T132, T183, T184 | |
| SC-001 a stop is never silent | Yes | T121, T168 | |
| SC-002 no wait exceeds its maximum | Yes | T074, T168 | |
| SC-003 a release build records entries | Yes | T067, T130 | |
| SC-004 zero input sequences reach the shell unconfigured | Yes | T096, T161 | |
| SC-005 a fault history found in under a minute | Yes | T129, T132 | Wording drift vs US5 scenario 2 — see I19 |
| SC-006 every tested setup exit ends the process | Yes | T096 | |
| SC-007 the five-attempt limit holds across restart | Yes | T106 | |
| SC-008 no preference lost across computers | Yes | T138 | Four of five preferences — see U4 |
| SC-009 a build fails if replaced behaviour returns | Yes | T169, T160 | T160 cites the wrong criterion — see I20 |
| SC-010 no useless remedy is offered | Yes | T119 | |
| SC-011 a session-length change lands at next sign-in | Yes | T111, T147 | |
| SC-012 every role-restricted setting unchanged | Yes | T137, T139, T149 | I8 closed |
| SC-013 every acceptance case covered or retired | Yes | T159, T167 | See I15 |
| SC-014 repeated faults group as one | Yes | T049, T131, T181 | |
| SC-015 store failure carries provider code, no values | Yes | T055, T182 | |
| SC-016 unreachable store ⇒ recorded nowhere locally, fault unchanged | Yes | T065, T130, T179, T182 | |
| SC-017 one entry copies whole and pastes | Yes | T184 | |

## Constitution Alignment Issues

One issue, raised as **C1** (CRITICAL): the Security & Secrets constraint's DPAPI mandate is not met or recorded by the remembered-sign-in design, and `plan.md` asserts PASS for that constraint while declaring that no Complexity Tracking table is needed.

The other nine checks hold and were verified against the constitution's actual text:

- **I. Spec-First, Verified Delivery** — `spec.md` precedes `plan.md` precedes `tasks.md`; every checkpoint demands build or test proof, and tasks are ticked only with proof.
- **II. Live Data Integrity** — the seven preference migrations target `config_settings_values` in the live store; no manual mock mode is introduced; FR-028 plus the extended `RetiredSymbolAuditTests` guards the boundary; `MockServiceClient` is a deployment endpoint, not a data substitution.
- **III. Stored-Procedure-First** — the new/newly-used procedures exist as artifacts (`fn_server_utc_now` retained; the nine startup-only procedures and `sp_core_buildings_upsert` verified present in `Database/StoredProcedures/AllSPs.sql` and schedulable for deletion); new artifacts ship `create.sql` + `rollback.sql`. **But the `update_table_descriptions.sql` element of this principle is uncovered — G3**, and the procedure named at `research.md` D9 does not exist — **I13**.
- **IV. MCP-First** — the plan names the two implementation areas that must consult documentation (WinUI 3 surfaces; `System.Security.Cryptography` AES plus the `ILogger` provider) and grounds D29's clipboard decisions in documented API behaviour.
- **V. WinUI 3 Platform Conformance** — new views under `Module_Startup/Views` and `Module_Settings/Views`, resource keys added in T018, localised abort reason in T102, automation ids on the placeholder and setup windows, no hardcoded pixel boundaries.
- **VI. Evidence-Based Verification Gates** — the phase gates are the constitution's commands, `WMC9999` is treated as a masked XAML error, and the deferral decisions exist specifically to keep the removal gate reachable.
- **VII. Readable, End-User-Facing Delivery** — the plan requires plain-English reporting and the `checklist-execution` skill; the deferred-action rule is respected.
- **External Integration & Cache Boundaries** — Infor Visual stays read-only, the verdict is settled before a screen opens, and no AI endpoint is introduced.
- **Documentation & Extensibility** — T162–T166 add the embed guide and update changelog, README, registry and instructions. (The `startup-diagnostics` capability is the gap — **G4**.)

## Unmapped Tasks

Legitimate infrastructure, removal, registry, document and verification tasks that map to no single FR/SC: T002, T003, T006–T011, T015–T019, T020–T024, T026–T029, T030–T034, T041, T043, T050, T051, T054, T056–T063, T066, T068–T070, T078, T079, T093, T094, T104, T105, T118, T128, T133–T136, T148, T150–T154, T158, T160–T166, T168, T170. No action needed; T070 (the 226-count confirmation) is the only one whose *figure* is ambiguous — see **I17**.

## Metrics

- Total Requirements: **55** (38 FR + 17 SC) — verified by counting distinct `**FR-nnn**` / `**SC-nnn**` markers: FR 001–038, SC 001–017, no gaps.
- Total Tasks: **184** — verified by counting distinct `**Tnnn**` markers: T001–T184 contiguous, no gaps, no duplicates (184 bullet lines).
- Coverage: **100%** (55/55 requirements have ≥1 task)
- Ambiguity Count: **1** (A2)
- Duplication Count: **0** (no near-duplicate requirements found; UR-016/UR-020 and FR-002/FR-003 are distinct)
- Coverage-gap Count: **2** (G3, G4)
- Inconsistency Count: **9** (I13–I21)
- Underspecification Count: **2** (U3, U4)
- Critical Issues Count: **1** (C1)

Repository checks run this pass (read-only, working tree as at 2026-09-26):

- `check-prerequisites.ps1 -Json -IncludeTasks` → `FEATURE_DIR = specs\010-startup-rebuild`; all three core artifacts present.
- `_logger.` across `*.cs` (excluding `bin`/`obj`) → **219** hits in **23** files (58 of them in `MTM_Waitlist.Mock.Service`).
- `logger.`/`Logger.` not preceded by `_` → **12** matching lines: **7** real call sites in **3** files, 2 XML-doc words, 3 `IsEnabled` assertions.
- `using Microsoft.Extensions.Logging;` (exact) → **31** files (the contract's figure is correct; the wider prefix matches 56 files across `.Abstractions` and other sub-namespaces).
- `StartupDebugLog.` → **489** call sites in **83** files (matches the artifacts); the bare token appears in 86 files, the extra three being the definition and two passive mentions. Breakout Info 335 / Error 147 / Configure 7 exactly as stated.
- `StartupState` → **48** files tree-wide (25 production, 23 test; 129 references), consistent with "roughly twenty consumers" once the definition and deleted startup files are excluded.
- `ILocalSettingsService|LocalSettingsService|LocalSettingsOptions|SettingsStorageExtensions` under `MTM_Waitlist.Tests/**` → **20** files / **233** matches, matching `local-state-test-migration.md` §4.
- `sp_config_settings\w*` under `Database/**` → only `sp_config_settings_get_effective`, `sp_config_settings_upsert`, `sp_config_settings_values_delete`, `sp_config_settings_values_get`.
- `Database/**/All*.sql` → `Tables/AllTables.sql`, `Functions/AllFunct.sql`, `Views/AllViews.sql`, `StoredProcedures/AllSPs.sql`, `Seeds/AllSeeds.sql` (plus the `Mock/` copies).
- `Test-Path` over 43 cited paths → all resolve except the three aggregate paths (I14), `MTM_Waitlist.Logging` (expected), `Database/Tables/34_…`/`35_…` (expected), `Module_Settings/Views/DeveloperLogPanelView.xaml` (expected), the new test files (expected), and the previously cited `Module_Settings/ViewModels/SettingsViewModelTests.cs` (I10 closed).
- `grep -i '^(GATE|\*\*GATE'` and `^## ` in `STARTUP-REBUILD-CHECKLIST.md` → **11** gates, **11** phases (Phase 0–10), **181** checkbox lines (the artifacts call it "179 tasks"; 5 are ticked in Phase 0) — a two-task discrepancy in a source instrument, not carried into `tasks.md`.
- `grep DPAPI|ProtectedData` over the feature artifacts → **2** hits, both rejected alternatives (C1).
- `grep startup-diagnostics` over `specs/010-startup-rebuild/*.md` → **0** hits (G4).
- `grep update_table_descriptions` over the feature artifacts → **1** hit, the protected-list line in `sql-contracts.md:214` (G3).
- `git ls-files` / `git status --short living-specs.yml` → tracked, but reported ` D` (deleted in the working tree) (I21).

## Next Actions

- **C1 must be dispositioned before `/speckit.implement`**: either record the remembered-sign-in deviation in `plan.md` → Complexity Tracking with its rationale (DPAPI cannot protect a value another machine must decrypt), or amend the Security & Secrets constraint via `/speckit.constitution`. Leaving the constraint marked PASS with no recorded deviation is the one item this pass classes as blocking.
- **G3 before the database phase**: add the `Database/Bootstrap/update_table_descriptions.sql` update (including its retired-objects sections) to T057/T062 or as its own task — the constitution's Principle III and the repo ruleset both require it, and it would otherwise fail a phase gate.
- **G4 before Phase 9 W1**: add `capabilities/startup-diagnostics/` and its registry/`DRIFT.md` rows to T026 or T163, so a retired capability does not outlive the code it describes.
- **I13 and I14 before the SQL tasks run**: correct `sp_config_settings_values_upsert` → `sp_config_settings_upsert` (D9, `sql-contracts.md` §5, `data-model.md` E10) and the three aggregate paths (T023/T057/T060/T062, `plan.md` tree). Both are one-pass edits that prevent an implementer editing a file that does not exist.
- **I18 before T137/T152/T157 are executed**: state the distinct totals (11 deleted, 11 kept) in `plan.md` and fix §1B's header and §4's "five" to four.
- The remaining LOW items (I15, I16, I17, I19, I20, I21, U3, U4, A2) are wording, scope or count drift and can be corrected opportunistically in the same pass.
- If you want to fix the two spec-only items, they are `spec.md` edits owned by you: SC-005's wording (I19) and FR-025's "reach the store" scope (U3).
- Would you like me to suggest concrete remediation edits for the top issues (C1, G3, I13, I14, I18)? I will not apply anything without your approval.

## Machine summary

```
C1|CRITICAL|Constitution|plan.md Constitution Check "Security & Secrets" · spec.md FR-014 · data-model.md E5 · research.md D12/D13|Remembered sign-in is a stored, decryption-capable credential under AES with a UNC-file key; the constitution's DPAPI (ProtectedData, CurrentUser) mandate for stored credentials is unmet and the plan asserts PASS with no Complexity Tracking entry
G3|HIGH|Coverage gap|tasks.md T057/T060/T062 · Database/Database-Ruleset.md:134 · contracts/sql-contracts.md §5 · constitution III|No task keeps Database/Bootstrap/update_table_descriptions.sql in sync although the ruleset makes it mandatory for every SQL artifact added, modified or removed
G4|MEDIUM|Coverage gap|capabilities/startup-diagnostics/spec.md · capabilities/DRIFT.md:31 · tasks.md T026, T133, T163|The startup-diagnostics capability matches StartupLog*.cs which T133 deletes, but no task retires its folder, registry row or DRIFT row
I13|MEDIUM|Inconsistency|research.md D9:117 · contracts/sql-contracts.md §5:212 · data-model.md E10 · tasks.md T140-T147|The settings write procedure is named sp_config_settings_values_upsert, which does not exist; the tree has sp_config_settings_upsert
I14|MEDIUM|Inconsistency|tasks.md T023/T057/T060/T062 · plan.md:184 · contracts/sql-contracts.md §5 · data-model.md E10|Three aggregate paths are wrong: Database/AllTables.sql, Database/AllFunct.sql and Database/AllViews.sql do not exist; they live under Tables/, Functions/ and Views/
I18|MEDIUM|Inconsistency|plan.md "Scale/Scope" · local-state-test-migration.md §1B header, §4, §5|Local-state test-surface counts disagree: plan says six deletions (11 distinct), §1B header says five overlap while its table marks four, §4 says T025 covers five of six while it covers four
I15|LOW|Inconsistency|tasks.md T159 vs spec.md lines 157-192|T159 says "the seventeen cases" in the spec's Edge Cases section, which holds 23 bullets; the launch-window-closed case maps to no task
I16|LOW|Inconsistency|spec.md FR-001 · contracts/launch-step-contract.md §5 · tasks.md T077 · data-model.md E9|Terminal outcomes enumerated three ways: four (FR-001), five (contract/T077), six (data-model adds "a completed configuration")
I17|LOW|Inconsistency|tasks.md T066, T070 · contracts/logging-contract.md §4|226/26 re-derives exactly, but 64 of the 226 sites are in MTM_Waitlist.Mock.Service, which T066 calls "unaffected"; the app-side figure is 161
I19|LOW|Inconsistency|spec.md SC-005 vs US5 scenario 2 · tasks.md T129|Residual wording drift left by the I3 fix: SC-005 says "severity and machine", US5 scenario 2 says "machine and by error kind"
I20|LOW|Inconsistency|tasks.md T160 vs spec.md SC-009|T160 (no disabled/skipped tests) cites SC-009 (build fails if replaced behaviour returns), which T169 verifies
I21|LOW|Inconsistency|tasks.md T163 · capabilities/DRIFT.md:31 · git status|living-specs.yml is tracked but deleted in the working tree (" D"), so T163's premise that the registry exists to be edited does not hold
U3|LOW|Underspecification|spec.md FR-025 · contracts/machine-configuration-contract.md §6 · tasks.md T156|FR-025 allows only what reaches the store, but the Infor Visual connection string is also retained and reaches a different system
U4|LOW|Underspecification|spec.md US6 and SC-008 · data-model.md E6 · tasks.md T138, T145|Five person-scoped preferences exist but SC-008 and T138 enumerate four, leaving the dunnage show-parts preference without a portability assertion
A2|LOW|Ambiguity|plan.md "Scale/Scope" and Scale note · tasks.md preamble · contracts/sql-contracts.md §4 · data-model.md E10|Database arithmetic unreconcilable: "nine procedures deleted" (ten are deleted) and "12 database artifacts added, repurposed or deleted" (26 enumerated)
```

COUNTS|critical=1|high=1|medium=4|low=9|total=15