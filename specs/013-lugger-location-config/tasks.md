# Tasks: Scrap Lugger Location

**Feature**: `specs/013-lugger-location-config/spec.md` | **Plan**: `specs/013-lugger-location-config/plan.md` | **Size**: normal | **Workflow**: companion

## How to read this list

- `- [ ]` is unticked. A task is ticked only once it is implemented and verified, and every task states its own verification.
- `[P]` marks a task independent of the others in its wave: a different file, no incomplete dependency.
- `[US#]` maps a task to a user story. `[US0]` marks shared schema, seed, resource, contract and validation work.
- `**Wave N — independent (different files):**` heads work that can be built in any order. `**⟶ Wait for Wave N to finish, then:**` is the join before work that depends on it. Same-file tasks are never in the same wave.
- **Every wave that touches code ends with `dotnet build MTM_Waitlist.sln`.** The gate task at the end of each phase records that build, run once with the whole phase in place.
- A task naming a stored procedure ships `create.sql` **and** `rollback.sql` in the same change, and that creation line is what `DatabaseAggregateRegistrationTests` looks for in the aggregate.
- No application code carries statement text (`InlineSqlAuditTests`). No retired type or resource survives (`RetiredSymbolAuditTests`).

**One owner per file.** The database artifacts all sit in Phase 2 because `DatabaseAggregateRegistrationTests` fails on any artifact missing from its aggregate, and the aggregate is one file: splitting procedures across stories would put `AllSPs.sql` under three owners. One source file is opened twice on purpose, by a later phase after the earlier one closes it, and it is named where it happens: `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs` (the placeholder branch in US1, the notice tick in US4).

---

## Phase 1: Setup

**Wave 1:**

- [x] **T001** [US0] Record the pre-change baseline on branch `013-lugger-location-config`: build the solution and run the full suite, and keep the result as the comparison for every later gate. · `MTM_Waitlist.sln`, `scripts/build/run-tests.ps1` · Verify: `dotnet build MTM_Waitlist.sln` and `scripts/build/run-tests.ps1` both exit 0, with the pass/skip/fail counts recorded.

---

## Phase 2: Foundational

The three tables, the eleven procedures, their aggregates and seed, the permission, the models and the arrangement service. **No story begins until this phase is done.**

**Wave 1 — independent (different files):**

- [x] **T002** [P] [US0] Create `waitlist_lugger_locations` and its rollback: `public_id` unique, `lugger_name` unique on the `utf8mb4_unicode_ci` collation, `status` defaulting to `Backup`, `status_reason`, `display_position`, and the two timestamps, with the display-position and status indexes. · `Database/Tables/36_waitlist_lugger_locations/create.sql`, `Database/Tables/36_waitlist_lugger_locations/rollback.sql` · FR-001, FR-004, FR-012 · Verify: both scripts apply and reverse on a local `mtm_waitlist`; a second `Door 6 Lugger` in any case is refused.
- [x] **T003** [P] [US0] Create `waitlist_lugger_scrap_assignments` and its rollback: the `lugger_id` foreign key with `ON DELETE CASCADE`, `scrap_type` beside `scrap_type_key`, `is_dump_point` defaulting to 0, `sort_position`, and the unique key on `(scrap_type_key, lugger_id)`. · `Database/Tables/37_waitlist_lugger_scrap_assignments/create.sql`, `Database/Tables/37_waitlist_lugger_scrap_assignments/rollback.sql` · FR-005, FR-006, FR-007 · Verify: both scripts apply and reverse; the same lugger cannot be assigned twice to one scrap type.
- [x] **T004** [P] [US0] Create `waitlist_lugger_arrangement_changes` and its rollback: no foreign key on `lugger_id`, `lugger_name` as durable text, the eight `change_kind` values, both values, the reason, and who and when. · `Database/Tables/38_waitlist_lugger_arrangement_changes/create.sql`, `Database/Tables/38_waitlist_lugger_arrangement_changes/rollback.sql` · FR-032 · Verify: both scripts apply and reverse; deleting a lugger leaves its change rows intact.

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2 — independent (different files):**

- [x] **T005** [P] [US0] Write the idempotent default-arrangement seed and its rollback: the seven named luggers at `display_position` 1 to 7 with the pinned statuses, and the seven assignments with `is_dump_point` and `sort_position`, inserted only when `waitlist_lugger_locations` is empty and inside one transaction. · `Database/Seeds/seed_waitlist_lugger_arrangement_defaults/create.sql`, `Database/Seeds/seed_waitlist_lugger_arrangement_defaults/rollback.sql` · FR-002, FR-008, FR-013 · Verify: a first run writes 7 and 7; a second run writes nothing; a renamed `Door 6 Lugger` is left alone.
- [x] **T006** [P] [US0] Amend the permission baseline seed and its rollback in place: nine rows for `permission.settings.lugger_arrangement`, held by `developer` through `material_handler_lead` and refused for the three worker roles. · `Database/Seeds/seed_permission_role_baselines/create.sql`, `Database/Seeds/seed_permission_role_baselines/rollback.sql` · FR-028 · Verify: the rollback removes exactly the nine rows it added, and the other keys' baselines are unchanged.
- [x] **T007** [P] [US0] Declare the permission: the constant `SettingsLuggerArrangement` and its `All` entry, plus one registry `Entry` with `Area.Settings`, a `false` fallback, and `SettingsViewModel` and `LuggerArrangementViewModel` as its gate sites. · `MTM_Waitlist.Core/Permissions/PermissionKeys.cs`, `MTM_Waitlist.Core/Permissions/PermissionRegistry.cs` · FR-028, FR-029 · Verify: `PermissionRegistryTests` green at twenty keys; the registry derives both resource keys from the suffix rule.
- [x] **T008** [P] [US0] Add every resource key this feature needs in one pass: the permission's label and gates, the settings entry and page title, the four status words, the editor's actions and its unavailable state, and the notice wording for the eight change kinds. Remove `Waitlist_Card_PendingSource.InDevelopment`, because the wording has no cell left to serve. · `Strings/en-us/Resources.resw` · FR-019, FR-035, FR-039 · Verify: the resource file parses; every key the code will name under `Permissions_Settings_Lugger_Arrangement`, `Settings_Lugger_Arrangement` and `Lugger_Notice` exists exactly once.
- [x] **T009** [P] [US0] Move the shipped scrap-type spellings into one catalogue the Setup workflow and the editor both read, including `5052 aluminum` as the setup list spells it, and excluding `Scrap Type Required` and `No Scrap`. · `MTM_Waitlist.Core/Services/ScrapTypeCatalog.cs` · FR-010, FR-011 · Verify: `ScrapTypeCatalogTests` green; the list holds the seven shipped types and neither non-type.
- [x] **T010** [P] [US0] Add the lugger model and its status vocabulary: `Lugger` with its public id, name, status, reason and position, and `LuggerStatus` over the four pinned strings with the "can receive" and "requires a reason" rules. · `MTM_Waitlist.Core/Models/Lugger/Lugger.cs`, `MTM_Waitlist.Core/Models/Lugger/LuggerStatus.cs` · FR-001, FR-012, FR-019 · Verify: `Full` and `Do Not Use` read as unable to receive and only `Do Not Use` reads as needing a reason.
- [x] **T011** [P] [US0] Add the assignment and change models: `ScrapAssignment` carrying the lugger, the entered spelling, the normalised key, the dump-point flag and the sort position, and `ArrangementChange` carrying the change kind and both values over `previous_value` and `new_value`. · `MTM_Waitlist.Core/Models/Lugger/ScrapAssignment.cs`, `MTM_Waitlist.Core/Models/Lugger/ArrangementChange.cs` · FR-006, FR-013, FR-032 · Verify: each model maps one contract row with no field left unread.

**⟶ Wait for Wave 2 to finish, then:**

**Wave 3 — independent (different files):**

- [x] **T012** [P] [US0] Write `sp_waitlist_lugger_locations_list` and its rollback: every lugger in `display_position` then `lugger_name` order, with the contract's eight columns. · `Database/StoredProcedures/sp_waitlist_lugger_locations_list/create.sql`, `Database/StoredProcedures/sp_waitlist_lugger_locations_list/rollback.sql` · FR-001 · Verify: an empty table returns no row; three luggers come back in position order.
- [x] **T013** [P] [US0] Write `sp_waitlist_lugger_locations_insert` and its rollback: trim the name, refuse a blank one and one already held, insert as `Backup` with a null reason, and append one `lugger_added` row in the same transaction. · `Database/StoredProcedures/sp_waitlist_lugger_locations_insert/create.sql`, `Database/StoredProcedures/sp_waitlist_lugger_locations_insert/rollback.sql` · FR-003, FR-004, FR-032 · Verify: the refusal is a `SIGNAL 45000` and writes no row; a success writes one lugger and one change row.
- [x] **T014** [P] [US0] Write `sp_waitlist_lugger_locations_rename` and its rollback: refuse a blank or duplicate name, write the new name, and append one `lugger_renamed` row per affected scrap type carrying the old and new names, or one row with both scrap-type columns null when the lugger takes none. · `Database/StoredProcedures/sp_waitlist_lugger_locations_rename/create.sql`, `Database/StoredProcedures/sp_waitlist_lugger_locations_rename/rollback.sql` · FR-003, FR-004, FR-035 · Verify: a rename of a lugger taking two types writes two change rows, each naming both names.
- [x] **T015** [P] [US0] Write `sp_waitlist_lugger_locations_delete` and its rollback: record the affected scrap types before the delete, delete the lugger so the cascade takes its assignments, and append one `lugger_removed` row per scrap type plus `dump_point_promoted` wherever another lugger took the role. · `Database/StoredProcedures/sp_waitlist_lugger_locations_delete/create.sql`, `Database/StoredProcedures/sp_waitlist_lugger_locations_delete/rollback.sql` · FR-003, FR-005, FR-026, FR-032 · Verify: no assignment survives the delete; the change rows keep the removed name.
- [x] **T016** [P] [US0] Write `sp_waitlist_lugger_locations_status_set` and its rollback, the transition engine: refuse any status outside the four, require and trim a reason for `Do Not Use` and clear it otherwise, hold each affected scrap type's assignment rows with `SELECT ... FOR UPDATE`, re-derive its dump point, sort every blocked lugger after every usable one, and append one `status_changed` row per affected scrap type plus one `dump_point_promoted` wherever the role moved, all in one transaction. · `Database/StoredProcedures/sp_waitlist_lugger_locations_status_set/create.sql`, `Database/StoredProcedures/sp_waitlist_lugger_locations_status_set/rollback.sql` · FR-013, FR-014, FR-015, FR-016, FR-017, FR-019, FR-020, FR-021, FR-032 · Verify: the six transitions in `plan.md` each leave exactly one dump point and no blocked lugger above it; an emptied lugger does not reclaim the role.
- [x] **T017** [P] [US0] Write `sp_waitlist_lugger_locations_promote` and its rollback: make the chosen lugger the dump point for one scrap type, set it `Available`, return the previous holder to `Backup`, refuse an unassigned lugger and one that is `Full` or `Do Not Use`, and append one `dump_point_promoted` row naming both. · `Database/StoredProcedures/sp_waitlist_lugger_locations_promote/create.sql`, `Database/StoredProcedures/sp_waitlist_lugger_locations_promote/rollback.sql` · FR-018, FR-032 · Verify: the two lugger rows and the one change row are written together, and both refusals leave the arrangement untouched.
- [x] **T018** [P] [US0] Write `sp_waitlist_lugger_scrap_assignments_list` and its rollback: the ten contract columns for one `scrap_type_key` in `sort_position` then `lugger_name` order, each carrying its lugger's current status, and no row for an unassigned type. · `Database/StoredProcedures/sp_waitlist_lugger_scrap_assignments_list/create.sql`, `Database/StoredProcedures/sp_waitlist_lugger_scrap_assignments_list/rollback.sql` · FR-006, FR-031 · Verify: an unassigned key returns no row; two assignments come back in position order with their statuses.
- [x] **T019** [P] [US0] Write `sp_waitlist_lugger_scrap_assignments_set` and its rollback: write the entered spelling and `LOWER(TRIM(...))` in one statement, update in place on `(scrap_type_key, lugger_id)`, refuse the two non-types after normalising, accept a type the shipped list does not name, re-derive the dump point from a promotion or the first usable lugger, and append `assignment_added` or `assignment_reordered` plus `dump_point_promoted` when the role moves. · `Database/StoredProcedures/sp_waitlist_lugger_scrap_assignments_set/create.sql`, `Database/StoredProcedures/sp_waitlist_lugger_scrap_assignments_set/rollback.sql` · FR-006, FR-010, FR-011, FR-013, FR-032 · Verify: the non-types are refused in any case or surrounding spaces; a reorder rewrites positions and re-derives the role in one transaction.
- [x] **T020** [P] [US0] Write `sp_waitlist_lugger_scrap_assignments_delete` and its rollback: remove the pairing, give the role to the first usable lugger remaining in order, leave the scrap type with no assignment when it was the last one, and append `assignment_removed` plus `dump_point_promoted` when the role moved. · `Database/StoredProcedures/sp_waitlist_lugger_scrap_assignments_delete/create.sql`, `Database/StoredProcedures/sp_waitlist_lugger_scrap_assignments_delete/rollback.sql` · FR-005, FR-013, FR-026, FR-032 · Verify: unassigning the dump point promotes the next usable lugger; unassigning the last one leaves the scrap type with no row.
- [x] **T021** [P] [US0] Write `sp_waitlist_lugger_arrangement_list` and its rollback: one call returning every lugger and assignment pairing in the contract's ten columns and order, with a lugger that takes nothing appearing once with its five assignment columns null. · `Database/StoredProcedures/sp_waitlist_lugger_arrangement_list/create.sql`, `Database/StoredProcedures/sp_waitlist_lugger_arrangement_list/rollback.sql` · FR-006, FR-030, FR-031 · Verify: the seven seeded luggers return eleven rows; an unassigned lugger returns one row with null assignment columns.
- [x] **T022** [P] [US0] Write `sp_waitlist_lugger_arrangement_changes_list` and its rollback: rows above `p_after_id` in `id` order with the contract's ten columns, and a null `p_after_id` returning exactly one row whose `id` is the current highest change id with every other column null. · `Database/StoredProcedures/sp_waitlist_lugger_arrangement_changes_list/create.sql`, `Database/StoredProcedures/sp_waitlist_lugger_arrangement_changes_list/rollback.sql` · FR-033, FR-037 · Verify: the watermark call returns one row on an empty table with `id` 0; a later call returns only the rows above the given id.

**⟶ Wait for Wave 3 to finish, then:**

**Wave 4 — independent (different files):**

- [x] **T023** [P] [US0] Append the three new tables to the table aggregate, each entry led by its own creation line. · `Database/Tables/AllTables.sql` · FR-001, FR-005, FR-032 · Verify: `DatabaseAggregateRegistrationTests` reports no unregistered table.
- [x] **T024** [P] [US0] Append the eleven new procedures to the procedure aggregate, each entry led by its own creation line. · `Database/StoredProcedures/AllSPs.sql` · FR-001, FR-014, FR-032 · Verify: `DatabaseAggregateRegistrationTests` reports no unregistered procedure.
- [x] **T025** [P] [US0] Append the new seed to the seed aggregate, led by its own first line. · `Database/Seeds/AllSeeds.sql` · FR-002, FR-008 · Verify: `DatabaseAggregateRegistrationTests` reports no unregistered seed.
- [x] **T026** [P] [US0] Add the three tables' descriptions beside the existing ones. · `Database/Bootstrap/update_table_descriptions.sql` · FR-001, FR-005, FR-032 · Verify: each of the three table names appears once with its description.
- [x] **T027** [P] [US0] Write the live schema validation: the three tables and their keys, the eleven procedures present, the seed's seven luggers and seven assignments with one dump point per scrap type, and the new permission's nine baseline rows. · `Database/Validation/lugger_arrangement_schema/validate.sql` · FR-002, FR-008, FR-013, FR-028 · Verify: every step reports against a freshly seeded `mtm_waitlist`, and the count assertions hold.

**⟶ Wait for Wave 4 to finish, then:**

**Wave 5 — independent (different files):**

- [x] **T028** [P] [US0] Add the arrangement snapshot and its one resolution rule: match the job's scrap type on `LOWER(TRIM(...))`, return the dump point's name, return the first lugger in order with its status in parentheses when none can receive, and return nothing for a non-type or an unassigned type while telling the caller it was a gap. · `MTM_Waitlist.Core/Models/Lugger/LuggerArrangementSnapshot.cs` · FR-009, FR-013, FR-022, FR-024, FR-025, FR-026 · Verify: `LuggerArrangementServiceTests` green for the default arrangement, the case and space variant, the all-blocked case, and both no-cell cases.
- [x] **T029** [P] [US0] Declare the arrangement seam: the snapshot over the arrangement read, the lugger, assignment and change reads, the seven writes, and the change watermark and poll, each carrying the actor the change row records. · `MTM_Waitlist.Core/Contracts/Services/ILuggerArrangementService.cs` · FR-014, FR-030, FR-032, FR-033 · Verify: every procedure in the contract has exactly one method, and no method takes statement text.
- [x] **T030** [P] [US0] Implement the arrangement service over `IMySqlHelperServer`: call the eleven procedures by name, map their rows into the models, surface a refusal as its plain message, and return `affected_rows` so a lost race reads as 0. · `MTM_Waitlist.Core/Services/LuggerArrangementService.cs` · FR-001, FR-014, FR-032 · Verify: `InlineSqlAuditTests` green; `LuggerArrangementServiceTests` green; no statement text in the file.

**⟶ Wait for Wave 5 to finish, then:**

**Wave 6 — independent (different files):**

- [x] **T031** [P] [US0] Read the catalogue instead of the local array, so Setup and the editor offer the same spellings and neither can drift. · `MTM_Waitlist.Setup/Services/SetupWorkflowService.cs` · FR-011 · Verify: the setup list is unchanged in content; `ScrapTypeCatalogTests` green.
- [x] **T032** [P] [US0] Register the arrangement service in its own contribution file, and declare and call both of the feature's partial registrations in the composition root so the page's file can be added later without a second edit there. · `Services/DependencyInjection/ServiceRegistrationExtensions.LuggerArrangement.cs`, `Services/DependencyInjection/ServiceRegistrationExtensions.cs` · FR-014 · Verify: the solution builds with the page registration unimplemented, and the service resolves.

**⟶ Wait for Wave 6 to finish, then:**

**Wave 7 — independent (different files):**

- [x] **T033** [P] [US0] Test the service's mapping and the snapshot's answer: the seven seeded assignments resolve to their lugger, `5052 aluminum` and `  5052 ALUMINUM ` resolve to `Door 8 Lugger`, an unassigned type resolves nothing, and a type whose luggers are all blocked resolves the first lugger with its status beside the name. · `MTM_Waitlist.Tests/Module_Core/Services/LuggerArrangementServiceTests.cs` · FR-009, FR-022, FR-024, FR-025, FR-026 · Verify: green, and each case fails if the matching or the fallback is removed.
- [x] **T034** [P] [US0] Test that the catalogue spells the shipped types exactly as the seed and the setup list do, including `5052 aluminum`, and that it never offers the two non-types. · `MTM_Waitlist.Tests/Module_Core/Services/ScrapTypeCatalogTests.cs` · FR-010, FR-011 · Verify: green, and a missing or renamed entry fails it.
- [x] **T035** [P] [US0] Extend the registry test to twenty keys and pin the new key's label, gates and `false` fallback. · `MTM_Waitlist.Tests/Module_Core/Permissions/PermissionRegistryTests.cs` · FR-028 · Verify: green, and an entry with a mismatched resource key fails it.

**⟶ Wait for Wave 7 to finish, then:**

- [x] **T036** [US0] Foundational gate: `dotnet build MTM_waitlist.sln` exits 0 after each wave above, `DatabaseAggregateRegistrationTests`, `InlineSqlAuditTests`, `RetiredSymbolAuditTests`, `PermissionRegistryTests`, `ScrapTypeCatalogTests` and `LuggerArrangementServiceTests` all pass, and the schema validator has been run against a local store. · Verify: every named command exits 0, with the output kept for the Polish comparison.

**Checkpoint**: the tables, procedures, seed, permission and service are in place, and every later story reads the arrangement through one seam.

---

## Phase 3: User Story 1 — the scrap card names where the material goes (P1)

Files: `MTM_Waitlist.Settings/Models/RequestJobPartAvailability.cs`,
`Services/RequestJobAvailabilityProvider.cs`,
`MTM_Waitlist.Settings/Services/RequestJobFieldValues.cs`,
`MTM_Waitlist.Waitlist.View/Helpers/WaitlistCardCellRules.cs`,
`MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs`.

### Tests

- [x] **T037** [P] [US1] Test the resolver arm: the label `Lugger Location` returns the availability's value, and returns nothing when the snapshot had no answer. · `MTM_Waitlist.Tests/Module_Settings/Services/RequestJobFieldValuesTests.cs` · FR-022, FR-025, FR-026 · Verify: red until T041 lands, then green.
- [x] **T038** [P] [US1] Amend the card test: a `pickup-scrap` card draws `Lugger Location` with the resolved lugger's name, draws no lugger cell when the availability carries none, and no cell anywhere reads `In development`. · `MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistLineCardDetailTemplateTests.cs` · FR-022, FR-025, FR-027, FR-039 · Verify: red until T039 to T043 land, then green.

### Implementation

**Wave 1 — independent (different files):**

- [x] **T039** [P] [US1] Add `LuggerLocation` to the availability record as a nullable string, beside `ScrapType`, so the card rule can omit the cell when it is null. · `MTM_Waitlist.Settings/Models/RequestJobPartAvailability.cs` · FR-022, FR-025 · Verify: the record still constructs for every existing caller, and the new member defaults to null.
- [x] **T040** [P] [US1] Read the arrangement once per list load in the provider: take one snapshot, resolve the job's scrap type, set `LuggerLocation`, and record a configuration gap once through `AppLog.Warn` with the item and the scrap type when the type has no assignment. · `Services/RequestJobAvailabilityProvider.cs` · FR-023, FR-026 · Verify: one snapshot call per load; a gap logs one warning and leaves the value null; an outage leaves the list working with no value.
- [x] **T041** [P] [US1] Add the `lugger location` arm and delete the TODO that described the one-arm plan this feature replaces. · `MTM_Waitlist.Settings/Services/RequestJobFieldValues.cs` · FR-022, FR-025, FR-026 · Verify: `RequestJobFieldValuesTests` green; no `TODO (feature 012` remains in the file.
- [x] **T042** [P] [US1] Retire the placeholder mechanism: delete `s_pendingSourceLabels`, `PendingSourceKey`, `PendingSourcePhrase` and `IsPendingSourceField` with their remarks, so no cell anywhere can be marked not-yet-built this way. · `MTM_Waitlist.Waitlist.View/Helpers/WaitlistCardCellRules.cs` · FR-039 · Verify: `InlineSqlAuditTests` and `RetiredSymbolAuditTests` green; a search for `PendingSource` finds nothing in the tree.
- [x] **T043** [P] [US1] Delete the branch that draws the placeholder from cell resolution, leaving the declared-field path to draw the resolver's value or omit the cell. · `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs` · FR-027, FR-039 · Verify: `WaitlistLineCardDetailTemplateTests` green; a scrap card draws the lugger name and draws nothing where the value is absent.

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T044** [US1] Build gate and focused verification: the solution builds after the wave above, and the two amended suites pass together with the card-rule and resolver tests. · Verify: `dotnet build MTM_waitlist.sln` exits 0; `dotnet test --filter "FullyQualifiedName~RequestJobFieldValues|FullyQualifiedName~WaitlistLineCardDetailTemplate|FullyQualifiedName~InlineSqlAudit"` green.

**Checkpoint**: with the shipped defaults and no configuration change, a `pickup-scrap` card for each of the six assigned types names the lugger the defaults assign, and the placeholder is gone from the application.

---

## Phase 4: User Story 2 — blocking a lugger moves the dump point by itself (P1)

The engine is SQL and landed in Phase 2, because the aggregate registration gate keeps every procedure in one phase. This phase is where the engine is proven, and it owns the proofs only.

Files: `MTM_Waitlist.Tests/Module_Core/Services/LuggerTransitionStoreRuleTests.cs`,
`MTM_Waitlist.Tests/Module_Core/Services/LuggerDumpPointResolutionTests.cs`.

### Tests

- [x] **T045** [P] [US2] Prove the transition engine against the shipped artifact: the four pinned statuses and only those, the required and cleared reason, the `SELECT ... FOR UPDATE` that makes two simultaneous edits one outcome, the promotion of the next usable lugger, the emptied lugger returning to `Backup` without reclaiming the role, and every blocked lugger sorting after every usable one. · `MTM_Waitlist.Tests/Module_Core/Services/LuggerTransitionStoreRuleTests.cs` · FR-012, FR-015, FR-016, FR-017, FR-019, FR-020 · Verify: each assertion names a statement in `sp_waitlist_lugger_locations_status_set/create.sql` and would fail if that statement were removed.
- [x] **T046** [P] [US2] Prove the owner's walk-through end to end over scripted snapshots: `Door 6 Lugger` available, then `Full` promoting `Extra Lugger (Outside)`, then emptied leaving it in place, then it becoming `Full` and handing the role back; and the explicit promotion returning the previous holder to `Backup`. Add the all-blocked case, which names the first lugger with its status, and the two-scrap-type case, where one blocked lugger promotes each type's own next lugger. · `MTM_Waitlist.Tests/Module_Core/Services/LuggerDumpPointResolutionTests.cs` · FR-013, FR-015, FR-016, FR-018, FR-021, FR-024 · Verify: all four steps of the walk-through read the stated lugger, and exactly one dump point exists at every step.

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T047** [US2] Gate: the solution builds and both proofs pass with the engine unchanged, so the story rests on its own evidence rather than on the seed. · Verify: `dotnet build MTM_waitlist.sln` exits 0; `dotnet test --filter "FullyQualifiedName~LuggerTransition|FullyQualifiedName~LuggerDumpPoint"` green.

**Checkpoint**: every sequence of status changes and promotions leaves each scrap type with exactly one dump point that can receive material, or with none only when every lugger is blocked.

---

## Phase 5: User Story 3 — the lead keeps the luggers straight (P2)

Files: `MTM_Waitlist.Settings/ViewModels/LuggerArrangementViewModel.cs`,
`MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs`,
`Module_Settings/Views/LuggerArrangementPage.xaml`, `Module_Settings/Views/LuggerArrangementPage.xaml.cs`,
`Module_Settings/Views/SettingsPage.xaml`,
`Services/DependencyInjection/ServiceRegistrationExtensions.LuggerArrangementPage.cs`.

### Tests

- [x] **T048** [P] [US3] Test the editor view model: the summary lists every lugger with its status and the scrap types it takes, each action calls its own service method with the signed-in actor, a blank reason on `Do Not Use` is refused before the call, the offered scrap types are the catalogue plus the ones already assigned plus free text, and the dump point is shown for each scrap type. · `MTM_Waitlist.Tests/Module_Settings/LuggerArrangementViewModelTests.cs` · FR-019, FR-030, FR-031, FR-032 · Verify: red until T050 lands, then green.
- [x] **T049** [P] [US3] Extend the settings test: the entry is drawn only when the lugger permission is held, is absent rather than disabled when it is not, and the route is refused when reached without it. · `MTM_Waitlist.Tests/Module_Settings/SettingsViewModelTests.cs` · FR-028, FR-029 · Verify: red until T051 lands, then green.

### Implementation

**Wave 1 — independent (different files):**

- [x] **T050** [P] [US3] Write the arrangement view model: the summary from one snapshot, the editor commands for add, rename, remove, set status with a required reason, assign, reorder, unassign and promote, the catalogue-plus-assigned scrap-type picker with free text, the dump point shown per scrap type, a refusal when the person does not hold the key, a plain refusal message from the store, and an unavailable state with a manual retry and no sample luggers. · `MTM_Waitlist.Settings/ViewModels/LuggerArrangementViewModel.cs` · FR-001, FR-003, FR-011, FR-019, FR-029, FR-030, FR-031, FR-032 · Verify: `LuggerArrangementViewModelTests` green; no statement text in the file.
- [x] **T051** [P] [US3] Add the entry and its rule: the route constant, the permission in the screened keys, and a card drawn only when `permission.settings.lugger_arrangement` is held. · `MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs` · FR-028, FR-029, FR-030 · Verify: `SettingsViewModelTests` green; a person below a lead sees no entry at all.
- [x] **T052** [P] [US3] Add the arrangement's entry card to the settings page, drawing only from the view model's own visibility answer. · `Module_Settings/Views/SettingsPage.xaml` · FR-029, FR-030 · Verify: the card appears and opens the route for a lead, and is absent otherwise.

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2 — independent (different files):**

- [x] **T053** [P] [US3] Build the arrangement page: the summary with each lugger's name, status and scrap types, the editor behind an explicit action, the status picker with a reason field enabled only for `Do Not Use`, and the unavailable state with its retry. · `Module_Settings/Views/LuggerArrangementPage.xaml`, `Module_Settings/Views/LuggerArrangementPage.xaml.cs` · FR-019, FR-030, FR-031 · Verify: the page loads from the summary, every action reaches its command, and the store-down state offers only a retry.
- [x] **T054** [P] [US3] Implement the page registration the composition root already declares: `pageService.Configure<LuggerArrangementViewModel, LuggerArrangementPage>()`. · `Services/DependencyInjection/ServiceRegistrationExtensions.LuggerArrangementPage.cs` · FR-030 · Verify: navigating by the view model's full name opens the page.

**⟶ Wait for Wave 2 to finish, then:**

- [x] **T055** [US3] Gate: the solution builds, the view model and settings suites pass, and a leading role's rename, remove, add and reassign are visible in the summary and on the next card load. · Verify: `dotnet build MTM_waitlist.sln` exits 0; `dotnet test --filter "FullyQualifiedName~LuggerArrangementViewModel|FullyQualifiedName~SettingsViewModel"` green; `validate-csharp-xaml-naming.ps1` exits 0.

**Checkpoint**: a Material Handler Lead renames, removes, adds and reassigns without help and without a restart, and nobody below that role can open the arrangement by any route.

---

## Phase 6: User Story 4 — handlers are told when the arrangement changes (P2)

Files: `MTM_Waitlist.Waitlist.View/Models/LuggerChangeNotice.cs`,
`MTM_Waitlist.Waitlist.View/Services/ILuggerChangeNoticeService.cs`,
`MTM_Waitlist.Waitlist.View/Services/LuggerChangeNoticeService.cs`,
`MTM_Waitlist.Waitlist.View/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs`,
`MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs`,
`Module_Waitlist/Views/WaitlistViewPage.xaml`, `Module_Waitlist/Views/WaitlistViewPage.xaml.cs`.

### Tests

- [x] **T056** [P] [US4] Test the notice service: opening the list takes the watermark from one null-`p_after_id` read and shows nothing at or below it, a later poll returns only rows above it, each notice reads its kind and both values in plain words with a rename naming both names, a dismissal is held for the session and never offered again, and a close for any reason other than the close button is not a dismissal. · `MTM_Waitlist.Tests/Module_Waitlist/Services/LuggerChangeNoticeServiceTests.cs` · FR-035, FR-036, FR-037 · Verify: red until T058 to T060 land, then green.
- [x] **T057** [P] [US4] Test the list's notice surface: only a person holding `permission.requests.handle` polls, the strip holds one bar per undismissed notice, and a dismissal removes that bar and only that bar. · `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewViewModelNoticeTests.cs` · FR-033, FR-036, FR-038 · Verify: red until T062 and T063 land, then green.

### Implementation

**Wave 1 — independent (different files):**

- [x] **T058** [P] [US4] Add the notice model the service hands the list: the change kind, the lugger, the scrap type, both values, the reason, who made the change and when. · `MTM_Waitlist.Waitlist.View/Models/LuggerChangeNotice.cs` · FR-035 · Verify: each field maps one column of `sp_waitlist_lugger_arrangement_changes_list`.
- [x] **T059** [P] [US4] Declare the notice seam: open the session's watermark, poll above it, and dismiss one notice for the session. · `MTM_Waitlist.Waitlist.View/Services/ILuggerChangeNoticeService.cs` · FR-036, FR-037 · Verify: the contract has no method that replays a change the watermark has already passed.
- [x] **T060** [P] [US4] Implement the notice service: take the watermark when the list opens, poll the change procedure above it, keep dismissals for the session, and turn each change kind into its plain-words sentence naming the lugger, the scrap type and both values where one changed. · `MTM_Waitlist.Waitlist.View/Services/LuggerChangeNoticeService.cs` · FR-033, FR-035, FR-036, FR-037, FR-038 · Verify: `LuggerChangeNoticeServiceTests` green; `InlineSqlAuditTests` green.

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2 — independent (different files):**

- [x] **T061** [P] [US4] Register the notice service with the module. · `MTM_Waitlist.Waitlist.View/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs` · FR-033 · Verify: the list resolves the service on navigation.
- [x] **T062** [P] [US4] Start the notice poll from the existing minute tick: take the watermark as the list opens, poll once per tick while the person holds `permission.requests.handle`, and expose the undismissed notices and the dismissal command. `WaitlistViewViewModel.cs` is opened here after Phase 3 closed it (sequential handoff, declared above). · `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs` · FR-033, FR-036, FR-038 · Verify: `WaitlistViewViewModelNoticeTests` green; a change recorded before the list opened is never shown.

**⟶ Wait for Wave 2 to finish, then:**

- [x] **T063** [US4] Draw the notice strip above the list: one `InfoBar` per notice at informational severity with its close button, and a dismissal recorded only when the close button raised the close. · `Module_Waitlist/Views/WaitlistViewPage.xaml`, `Module_Waitlist/Views/WaitlistViewPage.xaml.cs` · FR-033, FR-034, FR-035, FR-036 · Verify: two changes draw two bars; closing one leaves the other; no operating-system notification is raised.

**⟶ Wait for Wave 3 to finish, then:**

- [x] **T064** [US4] Gate: the solution builds, both notice suites pass, and the surface uses no MSIX-only API. · Verify: `dotnet build MTM_waitlist.sln` exits 0; `dotnet test --filter "FullyQualifiedName~LuggerChangeNotice|FullyQualifiedName~WaitlistViewViewModelNotice"` green.

**Checkpoint**: with two handler sessions open, a status change in a third shows a named notice in both within one tick, dismissal in one leaves the other's bar in place, and neither bar returns.

---

## Phase 7: Polish

### Implementation

**Wave 1 — independent (different files):**

- [x] **T065** [P] [US0] Correct the superseded assumption so the earlier spec no longer claims `Lugger Location` has no source. · `specs/012-waitlist-card-layout/spec.md` · FR-022, FR-039 · Verify: the assumption names configuration as the source and the drawn cell.
- [x] **T066** [P] [US0] Correct the superseded research row: the source is configuration, and the revival is a snapshot read plus one resolver arm, not one arm alone. · `specs/012-waitlist-card-layout/research.md` · FR-022 · Verify: the R1 row for `Lugger Location` carries the replacement.
- [x] **T067** [P] [US0] Correct the superseded contract entry that records the cell as a declaration with no resolver arm. · `specs/012-waitlist-card-layout/contracts/card-and-identifier.md` · FR-022 · Verify: the entry names the resolver arm and where the value comes from.
- [x] **T068** [P] [US0] Correct the superseded data-model note on the `pickup-scrap` row. · `specs/012-waitlist-card-layout/data-model.md` · FR-022 · Verify: the note no longer says no resolver arm exists.
- [x] **T069** [P] [US0] Remove the two TODO comments that describe the one-arm plan this feature replaces, keeping the declaration itself untouched. · `Database/Seeds/seed_waitlist_request_item_configs/create.sql` · FR-039 · Verify: the `Lugger Location` declaration still reads `job`, `string`, order 5, not required; no `TODO (feature 012` remains.
- [x] **T070** [P] [US0] Record the feature for its readers: the database ruleset's waitlist section for the three tables and eleven procedures, and a changelog entry for the cell and the settings page. · `Database/Database-Ruleset.md`, `CHANGELOG.md` · Verify: the ruleset names every new artifact and the changelog entry is present.

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T071** [US0] Validate against the Success Criteria: run the full suite, the naming validators and the schema validator, then record SC-001 to SC-007 each as met or not with the evidence. `.specify/companion.yml` is absent, so this phase owns the single suite run. · Verify: `scripts/build/run-tests.ps1`, `.github/scripts/validate-csharp-xaml-naming.ps1`, `.github/scripts/validate-sql-naming.ps1` and `.github/scripts/validate-database-schema.ps1` all exit 0, `InlineSqlAuditTests` and `RetiredSymbolAuditTests` green, and every criterion recorded.

---

## Dependencies & Execution Order

- **Setup (Phase 1)** records the baseline. Nothing depends on it, and every later gate compares against it.
- **Foundational (Phase 2)** blocks every story. Wave 1 creates the tables; Wave 2 adds the seed, permission, resources, catalogue and models; Wave 3 adds the eleven procedures; Wave 4 registers them in the three aggregates and adds the table descriptions and the validation script; Wave 5 adds the snapshot rule, the seam and the service; Wave 6 wires Setup and the composition root; Wave 7 builds. Wave 2 needs the tables, Wave 3 needs the tables, Wave 4 needs the procedures, Wave 5 needs the models, Wave 6 needs the service.
- **US1 (Phase 3)** depends on Foundational only. Its tests are one wave, its five implementation tasks are one wave (the rules retirement and the view-model branch must land together or the tree does not compile), then its gate.
- **US2 (Phase 4)** depends on Foundational only, and it adds no code outside its two proofs. It can run in parallel with US1.
- **US3 (Phase 5)** depends on Foundational only, and can run in parallel with US1 and US2. Its page needs its view model, so Wave 2 follows Wave 1.
- **US4 (Phase 6)** depends on Foundational, and on US1 for `WaitlistViewViewModel.cs`: US1 removes the placeholder branch there, US4 adds the notice tick to the same file afterwards. That is the one sequential handoff between stories, and it means US4 starts after US1 closes, not beside it.
- **Polish (Phase 7)** runs after every phase. Its suite run (T071) is the single validation point for the Success Criteria.
