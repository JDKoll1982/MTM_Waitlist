# Tasks: User Management and Permissions

**Feature**: `specs/006-user-management-and-permissions`
**Spec**: `user-management-and-permissions.spec.md` — 117 functional requirements, 9 user stories, 20 success criteria
**Plan**: `plan.md` · **Research**: `research.md` · **Data model**: `data-model.md` · **Contracts**: `contracts/`
**Recorded change size**: `oversized`
**Seed of record**: `WeekendProject/SpecTemplates/06-user-management.md` §3.8, §6, §8 decisions 1 to 30

## Scale note

This change spans about sixty files across five areas: the schema and its seeds, a new shared permission layer in
`MTM_Waitlist.Core`, the four administration pages with their view models, the sign-in path, and the gate sites the
specification's Context counts, whose one canonical statement is that Context section. A reader
should watch two orderings above all. First, the precedence re-rank of the
settings scopes (T006) must land before the first per-person permission row is ever written (T054), or a person's own
value resolves under the wrong order and is silently overridden. Second, the role rename must run and its paired
reversal must actually be executed (Phase 4) before the permission baselines are seeded against `it_department`
(T036), and the forward rename must then be re-applied, because the reversal leaves the retired role in the
catalogue: a baseline keyed to a role that does not exist is a baseline nobody holds.

## Ownership

One owner per path, as each phase's `Files:` line states. Three paths are deliberately shared, and are named here
rather than hidden:

- `Database/AllTables.sql`, `Database/AllSeeds.sql`, `Database/AllSPs.sql` and `Database/AllFunct.sql` are
  **generated roll-ups**, so every phase that changes `Database/**` ends with a regeneration task. They are rewritten
  at phase boundaries, never inside a wave.
- `Strings/en-us/Resources.resw` is a **single resource map**: a second `.resw` would change every key's lookup path.
  Foundational owns it and adds every new key for the whole feature. No story phase edits it.
- `Module_Settings/Views/PermissionsPage.xaml` is created by Phase 8 and hosts the who-holds-this view in Phase 9,
  because the UI contract puts both views on one page. Phase 9 runs strictly after Phase 8, so that edit is
  sequential rather than concurrent.

Phases 3 to 11 each begin only once Phase 2 is complete. Phases 10 and 11 need Phase 2 alone and may be built
alongside Phases 3 to 9.

---

## Phase 1: Setup

**Files**: `MTM_Waitlist.Tests/Module_Settings/Fixtures/PermissionMigrationBaseline.cs`,
`MTM_Waitlist.Tests/Module_Settings/Fixtures/RetiredRoleListFixture.cs`

**Wave 1 — independent (two different new files):**

- [x] **T001** [P] Record the two preconditions the precedence change rests on, as a checked-in fixture the later tests read: the settings table's current scope census (every row in it today is `all_users`) and the set of accounts on `admin` taken as a list of sign-in names and not as a count, so Phase 4 has something to compare against · `MTM_Waitlist.Tests/Module_Settings/Fixtures/PermissionMigrationBaseline.cs` · FR-053, FR-014, quickstart steps 1–2
- [x] **T002** [P] Capture the twelve retired role lists verbatim, from the shipping code, as fixture data the baseline parity test compares the shipped seed against: one array per retired list, and `Setup Tech` and `administrator` recorded as entries matching no role the catalogue holds · `MTM_Waitlist.Tests/Module_Settings/Fixtures/RetiredRoleListFixture.cs` · FR-052, SC-001, §6 gate 7

**⟶ Wait for Wave 1 to finish, then:** Phase 2, which depends on both.

---

## Phase 2: Foundational

**Blocks every story.** No work in Phases 3 to 11 begins until this phase is complete.

**Files**: `Database/Tables/01_core_users_profiles/`, `Database/Tables/03_auth_roles_catalog/`,
`Database/Tables/32_auth_user_management_audit/`, `Database/Functions/fn_config_settings_scope_rank/`,
`Database/Bootstrap/create_database.sql`, `Database/Bootstrap/update_table_descriptions.sql`,
`Database/Validation/user_management_schema/`, `Database/Validation/settings_schema/`,
`Database/StoredProcedures/sp_auth_credentials_check/`, `sp_auth_user_row_get/`,
`sp_auth_password_reset_required_get/`, `sp_auth_temporary_credential_attempt_record/`,
`sp_user_management_list/`, `sp_user_management_get/`, `sp_user_management_create/`,
`sp_user_management_update/`, `sp_user_management_reset_password/`, `sp_config_permissions_user_get/`,
`sp_config_settings_upsert/`, `Database/Seeds/seed_username_upper_normalization/`, `Database/AllTables.sql`,
`Database/AllSeeds.sql`, `Database/AllSPs.sql`, `Database/AllFunct.sql`,
`MTM_Waitlist.Core/Permissions/PermissionKeys.cs`, `MTM_Waitlist.Core/Permissions/PermissionRegistry.cs`,
`MTM_Waitlist.Core/Models/StartupState.cs`, `MTM_Waitlist.Core/Models/StartupCredentialCheckResult.cs`,
`MTM_Waitlist.Core/Models/StartupSessionSnapshot.cs`,
`MTM_Waitlist.Core/Models/StartupPasswordResetRequirement.cs`,
`MTM_Waitlist.Core/Contracts/Services/IPermissionService.cs`,
`MTM_Waitlist.Core/Services/PermissionService.cs`, `MTM_Waitlist.Core/Contracts/Services/IUserManagementService.cs`,
`MTM_Waitlist.Core/Services/UserManagementService.cs`, `MTM_Waitlist.Core/Services/UserManagementRepository.cs`,
`MTM_Waitlist.Startup/Services/StartupSessionRepository.cs`, `MTM_Waitlist.Startup/Services/StartupCoordinator.cs`,
`MTM_Waitlist.Startup/ViewModels/SplashViewModel.cs`, `MTM_Waitlist.Shared/Services/ControlInspectorService.cs`,
`MTM_Waitlist.Shared/Services/TooltipService.cs`,
`Services/DependencyInjection/ServiceRegistrationExtensions.cs`, `Strings/en-us/Resources.resw`,
`MTM_Waitlist.Tests/Module_Core/Permissions/PermissionRegistryTests.cs`,
`MTM_Waitlist.Tests/Module_Core/Permissions/PermissionResolutionTests.cs`,
`MTM_Waitlist.Tests/Module_Core/Services/SettingsScopeRankTests.cs`,
`MTM_Waitlist.Tests/Core/Models/CoreModelsTests.cs`, `MTM_Waitlist.Tests/Models/StartupModelsTests.cs`,
`MTM_Waitlist.Tests/Core/Models/StartupOptionsModelsTests.cs`,
`MTM_Waitlist.Tests/Module_Shared/Services/ControlInspectorServiceTests.cs`,
`MTM_Waitlist.Tests/Module_Shared/Services/TooltipServiceTests.cs`,
`MTM_Waitlist.Tests/Services/StartupCoordinatorTests.cs`,
`MTM_Waitlist.Tests/ViewModels/SplashViewModelTests.cs`

**Wave 1 — independent (four different schema artifacts):**

- [x] **T003** [P] Add `first_name VARCHAR(128) NOT NULL`, `last_name VARCHAR(128) NOT NULL` and `temporary_credential_failed_attempts INT NOT NULL DEFAULT 0` to the person table, shipping `create.sql` and `rollback.sql` in the same change, with the rollback dropping exactly those three columns and nothing else · `Database/Tables/01_core_users_profiles/` · FR-001, FR-037, constitution III, `database-schema-rules`
- [x] **T004** [P] Add `role_rank INT NOT NULL DEFAULT 0` to the role catalogue, with `create.sql`, `rollback.sql` and every constraint or index name inside the 64-character limit, so the rung of every role is a readable row rather than a value copied into code · `Database/Tables/03_auth_roles_catalog/` · FR-010, FR-018
- [x] **T005** [P] Create the account-change audit table at `32_`, following the highest folder number in use (31), shaped as one row per changed field with `change_group_id`, `field_name`, both value columns, the three actor snapshots (`actor_display_name`, `actor_employee_identifier`, `actor_role_code`) and `occurred_utc`, shipping `create.sql` and `rollback.sql` · `Database/Tables/32_auth_user_management_audit/` · FR-108, FR-109, FR-110
- [x] **T006** [P] Re-rank the settings scope precedence so a person's own value wins: `computer` 1, `all_users` 2, **`role` 3**, `admin` 4, `developer` 5, **`user` 6**, keeping the reader's highest-rank-applicable-row rule. This ships **before** any per-person permission row exists anywhere in this feature, and it is not done until T054, which writes the first such row, is still ahead of it in this list · `Database/Functions/fn_config_settings_scope_rank/` · FR-053, decision 29, quickstart step 3

**⟶ Wait for Wave 1 to finish, then:** the bootstrap, the validators and the database procedures, all of which name the columns and the function above.

**Wave 2 — independent (different artifacts, all needing Wave 1):**

- [x] **T007** [P] Apply the four new columns — the three on the person table and `role_rank` on the role catalogue — and the new table to a store that already exists as well as to a fresh one: guarded `information_schema` checks and prepared `ALTER TABLE` statements in the bootstrap's update path, following the block that already adds `setup_work_centers_catalog.building`, plus the fresh-store create path, plus the two schema validators — the new user-management one and the extended settings one, which now declares the `role` scope and asserts the corrected rank order. `CREATE TABLE IF NOT EXISTS` alone cannot add a column to a live table, which is why the guard is required rather than optional · `Database/Bootstrap/create_database.sql`, `Database/Bootstrap/update_table_descriptions.sql`, `Database/Validation/user_management_schema/`, `Database/Validation/settings_schema/` · FR-048, constitution III, quickstart step 3
- [x] **T008** [P] Give the three existing sign-in reads their role code — and the wrong-attempt count on the credential read — appending the new columns and moving the calling code's ordinals in the same change, and correcting the two header comments that document the lower-case sign-in contract. The verdict that decides whether an account holds a temporary value keeps its two existing signals and is not widened: `require_password_change = 1`, or a stored value that is empty or is the legacy `0000` · `Database/StoredProcedures/sp_auth_credentials_check/`, `Database/StoredProcedures/sp_auth_user_row_get/`, `Database/StoredProcedures/sp_auth_password_reset_required_get/` · FR-105, FR-002, FR-031, database contract
- [x] **T009** [P] Add the one-statement procedure that either raises the wrong-attempt count or clears it — nothing else clears it and nothing expires it, and it is a non-query so the affected-row count reaches the caller — and add the header comment that records why the settings upsert is deliberately not the permission write path and why its `ELSE 'developer'` scope-key branch stays, so the next reader finds that trap by reading · `Database/StoredProcedures/sp_auth_temporary_credential_attempt_record/`, `Database/StoredProcedures/sp_config_settings_upsert/` · FR-036, FR-039, research "where a permission is written"
- [x] **T010** [P] Add the roster read and the one-person read: search across sign-in name, display name, employee number and role name, an optional role filter with everyone returned by default and switched-off people included, and a single-person read returning the identity fields, the role code and name, the rung, the active flag and the wrong-attempt count. Neither refuses a reader: an account the reader cannot change is still readable · `Database/StoredProcedures/sp_user_management_list/`, `Database/StoredProcedures/sp_user_management_get/` · FR-027, FR-086, FR-087, FR-088, FR-101
- [x] **T011** [P] Add the create procedure: in one transaction, refuse a role above the actor's own rung, insert the profile with the upper-cased sign-in name, the derived display name, `is_active = 1` and the temporary credential, insert the role assignment — exactly one, so a person cannot come into existence with two — and write one audit row per written field sharing `p_change_group_id`. The actor's rung is read from the catalogue inside the procedure and never taken from its caller, and a duplicate sign-in name is left to MySQL's own `1062` error so no blind retry exists anywhere in the path · `Database/StoredProcedures/sp_user_management_create/` · FR-006, FR-007, FR-008, FR-009, FR-019, FR-025, FR-108, FR-110
- [x] **T012** [P] Add the update procedure: in one transaction, refuse a target whose rung is strictly above the actor's, refuse a role above the actor's own rung, refuse deactivating the actor's own account, refuse changing the sign-in name of the account the actor is signed in as, and write one audit row per field that actually changed, so a save that changed nothing writes nothing · `Database/StoredProcedures/sp_user_management_update/` · FR-019, FR-022, FR-023, FR-024, FR-025, FR-026, FR-110
- [x] **T013** [P] Add the reset procedure: in one transaction, refuse a target that outranks the actor, store the hash and salt of the freshly generated PIN — the salted hash only, never the PIN itself, which is what makes "readable back" mean plaintext in FR-030 — set `require_password_change = 1`, clear the wrong-attempt count, leave `is_active` exactly as it was, leave every existing session for that person alone because a reset ends none (FR-035), and write one audit row recording the reset with **both value columns null**, because the credential is never stored · `Database/StoredProcedures/sp_user_management_reset_password/` · FR-029, FR-030, FR-035, FR-039, FR-111
- [x] **T014** [P] Add the read that returns the permission rows stored for one person and for their role, resolving the person's role from their own assignment inside the procedure so no caller can ask about a role that is not theirs · `Database/StoredProcedures/sp_config_permissions_user_get/` · FR-049, FR-053
- [x] **T015** [P] Add the guard for the sign-in-name rule: upper-case the stored sign-in name and report rather than fail when two rows would collide on the unique key, with a rollback that restores the values it recorded · `Database/Seeds/seed_username_upper_normalization/` · FR-003, FR-002

**⟶ Wait for Wave 2 to finish, then:** the declaration and the session change, which the services below consume.

**Wave 3 — independent (two different files):**

- [x] **T016** [P] Write the one declaration: the fourteen keys as constants spelled exactly as the specification's Verbatim Constraints pins them — this declaration is the one canonical source for those spellings, and no other task restates the literals — and per key the label's resource key, the sentence saying what it gates, the area it belongs to, the shipped fallback, and the gate sites that read it. The declaration carries **no** per-role baseline: the baselines are the seeded rows T036 writes, and the two are tied by the check in T024, which is the deliberate resolution of the seed's contradiction recorded in the specification's Clarifications. Nothing else may add a permission, and the gate-site list is what both directional checks compare against · `MTM_Waitlist.Core/Permissions/PermissionKeys.cs`, `MTM_Waitlist.Core/Permissions/PermissionRegistry.cs` · FR-045, FR-046, FR-047, FR-048, FR-061, FR-062, contract "the declaration"
- [x] **T017** [P] Extend `StartupState` with the signed-in person's role code and id, carrying the code through the Core models that hand the sign-in identity over (`StartupCredentialCheckResult`, `StartupSessionSnapshot`, `StartupPasswordResetRequirement`), keeping the existing display-name property for the places that show text, and move the developer check off the display name and onto the role code. The producers and the readers of the display name move in the same change, or the build and the suite break: `MTM_Waitlist.Startup/Services/StartupCoordinator.cs`, which sets `CurrentRole = "Developer"` on the default-developer path, `MTM_Waitlist.Startup/ViewModels/SplashViewModel.cs`, `MTM_Waitlist.Shared/Services/ControlInspectorService.cs` and `MTM_Waitlist.Shared/Services/TooltipService.cs`. This is the thirteenth gate site — the one a hand count of the twelve retired lists missed, per the specification's Context · `MTM_Waitlist.Core/Models/StartupState.cs`, `MTM_Waitlist.Core/Models/StartupCredentialCheckResult.cs`, `MTM_Waitlist.Core/Models/StartupSessionSnapshot.cs`, `MTM_Waitlist.Core/Models/StartupPasswordResetRequirement.cs`, `MTM_Waitlist.Startup/Services/StartupCoordinator.cs`, `MTM_Waitlist.Startup/ViewModels/SplashViewModel.cs`, `MTM_Waitlist.Shared/Services/ControlInspectorService.cs`, `MTM_Waitlist.Shared/Services/TooltipService.cs` · FR-054, FR-105, research "where the role code reaches the screens"

**⟶ Wait for Wave 3 to finish, then:** the services that compose them.

**Wave 4 — independent (three different types):**

- [x] **T018** [P] Implement permission resolution for one person: their own stored row, otherwise their role's baseline, otherwise the shipped fallback, so an unreachable store answers from the fallback rather than turning every control in the application into a refusal. One lookup answers both a control's visibility and the action it guards, the answer is cached for the session with explicit invalidation on a save and on a sign-in that resolves a different person, and it is never read inside a layout pass on the interface thread · `MTM_Waitlist.Core/Contracts/Services/IPermissionService.cs`, `MTM_Waitlist.Core/Services/PermissionService.cs` · FR-049, FR-050, FR-051, FR-053, FR-055, FR-056, FR-060
- [x] **T019** [P] Implement the user-management service and repository behind the six procedures above: validation of the identity rules, the derived display name, the typed duplicate-sign-in-name result read from the provider's `1062`, each store refusal token turned into one typed reason, and every write through the non-query seam that reports affected rows. No statement text enters this file · `MTM_Waitlist.Core/Contracts/Services/IUserManagementService.cs`, `MTM_Waitlist.Core/Services/UserManagementService.cs`, `MTM_Waitlist.Core/Services/UserManagementRepository.cs` · FR-001, FR-004, FR-005, FR-008, FR-025, constitution III
- [x] **T020** [P] Make the sign-in repository store and compare sign-in names in upper case — asserted with a lower-case name stored as `JSMITH` that then signs in typed either way, which is §6 gate 2's uppercase-normalisation case and the only place it is asserted — carry the role code through every credential read, and record a temporary-credential attempt through the new procedure, so the increment happens in one statement and no screen can forget to record a failure. The ordinals of every changed read move in this same change · `MTM_Waitlist.Startup/Services/StartupSessionRepository.cs` · FR-002, FR-036, FR-037, FR-042, research "where the wrong-attempt count is held"

**⟶ Wait for Wave 4 to finish, then:** registration and resources.

**Wave 5 — independent (two different files):**

- [x] **T021** [P] Register the new services in the composition root, and make the registration class `partial` with three page-registration methods declared and called — `RegisterUserManagementPages`, `RegisterUserAccountPages` and `RegisterPermissionsPages` — so each story phase owns its own registration file instead of three phases contending on one · `Services/DependencyInjection/ServiceRegistrationExtensions.cs` · FR-080, constitution V
- [x] **T022** [P] Add every new resource key for this feature under the pinned prefixes: `UserManagement_*`, `CreateUser_*`, `EditUser_*`, `Permissions_*`, `PinReveal_*`, `Administration_*`, `Role_<role_code>` for all nine roles and `Permission_<suffix>` for all fourteen keys. No two labels share a key, and no new key collides with an existing entry. This is the only phase that edits this file · `Strings/en-us/Resources.resw` · FR-112, FR-113, SC-020, contract "resource keys", defects `Closed-Medium-Settings-AboutLabelsCollideOnOneResourceKey`

**⟶ Wait for Wave 5 to finish, then:** the aggregates and the suites.

**Wave 6 — aggregate regeneration (single task, runs alone):**

- [x] **T023** Regenerate the hand-maintained roll-ups so everything above is in the shipped aggregates, and confirm the number of table, procedure and function entries in each matches the folders on disk · `Database/AllTables.sql`, `Database/AllSeeds.sql`, `Database/AllSPs.sql`, `Database/AllFunct.sql` · constitution III, §6 gate 4

**⟶ Wait for Wave 6 to finish, then:** the tests.

**Wave 7 — independent (three different test files):**

- [x] **T024** [P] Assert the declaration: every key unique, the declaration non-empty, every key spelled exactly as the specification's Verbatim Constraints pins it, every declared key read by at least one named gate site — including `permission.admin.reset_password`, which the reset action on the person's page reads (T048, T051), so no key can be declared without a reader — and nothing about the baselines, because they do not exist yet. **The declaration-versus-baseline comparison in both directions, and the three keys with no predecessor, are deliberately not asserted here:** both need the seeded baselines, which T036 writes three phases later, so both belong to T034, where the data is. This file asserts only what is provable once T016 lands · `MTM_Waitlist.Tests/Module_Core/Permissions/PermissionRegistryTests.cs` · FR-046, FR-048, FR-060, FR-061, FR-062, SC-003, §3.8 *Required tests*
- [x] **T025** [P] Assert resolution and the precedence trap: a person with no row of their own gets their role's baseline and holds nothing beyond it; a person's own row beats their role's baseline and changes nobody else's; a person's own row resolves ahead of a `role`-scoped row and of the two legacy scopes; a role with no baseline row answers from the shipped fallback rather than a refusal; and a store that throws answers from the shipped fallback · `MTM_Waitlist.Tests/Module_Core/Permissions/PermissionResolutionTests.cs`, `MTM_Waitlist.Tests/Module_Core/Services/SettingsScopeRankTests.cs` · FR-049, FR-050, FR-051, FR-053, FR-060, SC-013, decision 29, §3.8 *Required tests*
- [x] **T026** [P] Update the existing `StartupState` assertions to the role code, so the developer check is proved to read the code and the display name is proved not to decide it, and update every suite that constructs a state by setting only `CurrentRole`, so none of them keeps compiling against the display name: `CoreModelsTests`, `StartupModelsTests`, `StartupOptionsModelsTests`, `ControlInspectorServiceTests`, `TooltipServiceTests`, `StartupCoordinatorTests` and `SplashViewModelTests` · `MTM_Waitlist.Tests/Core/Models/CoreModelsTests.cs`, `MTM_Waitlist.Tests/Models/StartupModelsTests.cs`, `MTM_Waitlist.Tests/Core/Models/StartupOptionsModelsTests.cs`, `MTM_Waitlist.Tests/Module_Shared/Services/ControlInspectorServiceTests.cs`, `MTM_Waitlist.Tests/Module_Shared/Services/TooltipServiceTests.cs`, `MTM_Waitlist.Tests/Services/StartupCoordinatorTests.cs`, `MTM_Waitlist.Tests/ViewModels/SplashViewModelTests.cs` · FR-054

**⟶ Wait for Wave 7 to finish, then:** Phase 3. **Checkpoint — Foundational is complete**: the schema, the re-rank, the declaration, the resolution, the user-management data layer and the session role code all exist and are proved by the three suites above; no gate has been replaced and no per-person permission row has been written.

---

## Phase 3: US3 — One definition of who outranks whom (Priority: P1)

**Goal**: one ladder keyed on the role code, every rung checkable, and Material Handler Lead added as a role in its
own right rather than mapped onto Plant Manager.
**Independent test**: for two roles the rank comparison and the roles-at-or-above list come from the one definition
and agree; each of the eight rungs the shipped seed writes reads its declared value, and the renamed role's rung is not
asserted here because the catalogue does not hold that role until Phase 4; a role added between two rungs needs no renumbering.
**Files**: `Database/Seeds/seed_dev_masked_baseline/`, `Database/StoredProcedures/sp_auth_roles_list/`,
`MTM_Waitlist.Core/Permissions/RoleAuthorization.cs`,
`MTM_Waitlist.Core/Contracts/Services/IRoleCatalogService.cs`, `MTM_Waitlist.Core/Services/RoleCatalogService.cs`,
`Database/AllSeeds.sql`, `Database/AllSPs.sql`,
`MTM_Waitlist.Tests/Module_Core/Permissions/RoleAuthorizationTests.cs`,
`MTM_Waitlist.Tests/Module_Settings/Services/RoleCatalogServiceTests.cs`,
`MTM_Waitlist.Tests/Module_Settings/Services/RoleLadderSeedTests.cs`

### Implementation

**Wave 1 — independent (two different artifacts):**

- [x] **T027** [P] [US3] Extend the shipped role seed by adding Material Handler Lead as a role in its own right and giving every **live** role the seed holds its rung — `developer` 100, `plant_manager` 80, `production_lead` 70, `setup_lead` 60, `material_handler_lead` 50, and `material_handler`, `production` and `setup` at 10. **That is eight rungs, not nine:** the retired `admin` row is deliberately left unranked, so it holds the column default of 0 until T032 removes it, and the reversal's "the rung that row held" is therefore determinate. `admin` and `it_department` are left entirely alone here: this seed does not remove the retired role, does not insert IT Department and does not repoint the masked `test.admin` account, so `seed_role_admin_to_it_department` (T032) stays the only place the rename happens and the reason its reversal is exact — that `it_department` did not exist before the migration — stays true. The rollback removes exactly the rows it added · `Database/Seeds/seed_dev_masked_baseline/` · FR-015, FR-018
- [x] **T028** [P] [US3] Add the one read behind the picker, the ladder and the rank comparison: `role_id`, `role_code`, `role_name`, `role_rank`, ordered by rung descending then code, with the retired role absent · `Database/StoredProcedures/sp_auth_roles_list/` · FR-010, FR-016

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2 — the readers (single task, no parallel sibling):**

- [x] **T029** [US3] Implement the ladder readers and nothing else: `IsAtLeast`, the roles at or above a rung, and the highest rung a person holds when more than one live role is held. Every comparison reads the numbers it was given from the one catalogue read and compares nothing itself, so the rank comparison and the roles-at-or-above list cannot disagree; add the session-cached service that reads the one procedure and serves the picker, the ladder and the rank comparison from it, so there is no second source of ranks in the application · `MTM_Waitlist.Core/Permissions/RoleAuthorization.cs`, `MTM_Waitlist.Core/Contracts/Services/IRoleCatalogService.cs`, `MTM_Waitlist.Core/Services/RoleCatalogService.cs` · FR-010, FR-016, FR-017

**⟶ Wait for Wave 2 to finish, then:**

**Wave 3 — aggregate regeneration (single task, runs alone):**

- [x] **T030** [US3] Regenerate `Database/AllSeeds.sql` and `Database/AllSPs.sql` for the extended seed and the new read · `Database/AllSeeds.sql`, `Database/AllSPs.sql` · constitution III, §6 gate 4

**⟶ Wait for Wave 3 to finish, then:**

**Wave 4 — the suite (single task, runs alone):**

- [x] **T031** [US3] Assert the ladder as Phase 3 leaves it: each of the **eight** rungs the shipped seed writes reads its declared value — `developer` 100, `plant_manager` 80, `production_lead` 70, `setup_lead` 60, `material_handler_lead` 50, and `material_handler`, `production` and `setup` at 10 — `IsAtLeast` agrees at and around each boundary, peers at one rung are mutually at-or-above, the three worker roles share one rung, the gaps are wide enough for a role to be inserted between two rungs without renumbering the others, the catalogue read returns those eight codes and no more — the retired role's
catalogue row is still in the catalogue in this phase, and the read excludes it (T028) — **the retired role's own row reads the column default of 0, which is what makes the reversal's arithmetic determinate (T032)** — and the shipped seed file
read from the repository matches the pinned rung table for the rungs it writes. The **nine**-code assertion — the codes with `admin` absent and `it_department` present — is not made here, because the catalogue still holds the retired role and does not yet hold IT Department until T032 removes one and inserts the other; that assertion lives in Phase 4, in T033, where it is true · `MTM_Waitlist.Tests/Module_Core/Permissions/RoleAuthorizationTests.cs`, `MTM_Waitlist.Tests/Module_Settings/Services/RoleCatalogServiceTests.cs`, `MTM_Waitlist.Tests/Module_Settings/Services/RoleLadderSeedTests.cs` · FR-015, FR-016, FR-017, FR-018

**Checkpoint** — the application holds one ladder and one catalogue; every rung is checkable from a row, the rank
comparison and the roles-at-or-above list come from that one definition, and Material Handler Lead exists as its own
title. This story is independently functional and testable here.

---

## Phase 4: US9 — The role rename cannot strand an account (Priority: P1)

**Goal**: the administrator role is removed, IT Department takes its place with its rung, the accounts move across in
one all-or-nothing act, and the paired reversal is executed rather than merely written — after which the forward
rename is re-applied, so what ships is the renamed role and not the retired one.
**Independent test**: run the change against a live store, compare the accounts on IT Department with the set that
held the administrator role and the count of accounts with no role against zero, and confirm the catalogue holds the
nine codes with the renamed role at its rung; then run the paired reversal and repeat both comparisons; then
re-apply the change and confirm IT Department is back at its rung with the retired role gone.
**Files**: `Database/Seeds/seed_role_admin_to_it_department/`, `Database/AllSeeds.sql`,
`MTM_Waitlist.Tests/Module_Settings/Services/RoleRenameMigrationTests.cs`

### Implementation

**Wave 1 — the change, its reversal and the aggregate (single task, no parallel sibling):**

- [x] **T032** [US9] Write the rename as one transaction: remove the `admin` catalogue row, insert `it_department` **with `role_rank = 90`**, and repoint every assignment from the removed row to the new one, under an exit handler that rolls back and resignals, so the interrupted state in which an account holds no role cannot exist. The rung is written explicitly and not left implied, because the column is `NOT NULL DEFAULT 0` (T004): an insert that omitted it would put the renamed top role **below** the worker roles at 10, and the rank rule would then refuse it the accounts it exists to own. Ship the paired `rollback.sql` in the same change and regenerate `AllSeeds.sql` for both: the reversal re-inserts `admin` carrying the rung that row held when the migration removed it — which is the column's default, because T027 deliberately leaves the retired role unranked — so the reversal restores the row it replaced rather than inventing a rung for it. State in both headers that the reversal is exact only while no account has been given IT Department since the migration, because that window is the reason it is exact · `Database/Seeds/seed_role_admin_to_it_department/`, `Database/AllSeeds.sql` · FR-011, FR-012, FR-013, FR-014, constitution III

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2 — the proof (single task, runs alone):**

- [x] **T033** [US9] Prove both halves against a live store in one opt-in, environment-gated run: apply the change, assert the set of accounts on IT Department equals the set that held the administrator role and that no account has no role, assert the catalogue read returns the nine codes with `admin` absent and `it_department` present at its rung of 90 alongside `material_handler_lead`, then **actually run** the paired reversal and assert the administrator role is back with those accounts on it and that still no account has no role, and assert a switched-off account that held the administrator role holds IT Department and remains switched off, because activation is a separate act. Then **re-apply the change**, because the reversal has just left the retired role in the catalogue and Phase 5 seeds the baselines against `it_department`: assert IT Department is in place with its rung of 90, the retired role is gone again, and the accounts are back on IT Department, so the run ends with the store holding the renamed role and not the retired one. Record skipped, never passed, when the gate variable is unset · `MTM_Waitlist.Tests/Module_Settings/Services/RoleRenameMigrationTests.cs` · FR-013, FR-014, SC-005, SC-006, §6 gate 3

**Checkpoint** — the rename is proved rather than asserted, its reversal has been run, and the forward rename has then
been re-applied, so this checkpoint is passed with the store holding `it_department` at rung 90 and not `admin`.
**Ordering gate**: the permission baselines may be seeded only after this checkpoint, because they are keyed to
`it_department`, which is what this checkpoint leaves in place.

---

## Phase 5: US4 — One named permission for every gated action (Priority: P1)

**Goal**: the eleven role lists that decide access are replaced by named permissions, the twelfth — the badge map —
is re-keyed to role codes and stays out of the permission set, the baselines reproduce today's access exactly, and no
gate reads its own list of role names.
**Independent test**: for each retired list that becomes a permission, confirm the shipped baseline gives that role
exactly what the list gave it; confirm the badge map answers from role codes and holds no permission of its own;
confirm the audit reports all thirteen sites the specification's Context counts and that no gate reads its own role
list; confirm a person with no row of their own receives their role's baseline.
**Files**: `MTM_Waitlist.Tests/Module_Settings/Services/PermissionBaselineParityTests.cs`,
`MTM_Waitlist.Tests/Module_Core/Permissions/PermissionGateSiteAuditTests.cs`,
`MTM_Waitlist.Tests/Module_Mock_Service/ServiceOperatorPermissionTests.cs`,
`Database/Seeds/seed_permission_role_baselines/`, `Database/AllSeeds.sql`,
`MTM_Waitlist.Core/Services/RequestActionPolicy.cs`, `MTM_Waitlist.Tests/Core/Services/RequestActionPolicyTests.cs`,
`MTM_Waitlist.Mock.Service/Api/ServiceOperatorRoles.cs`,
`MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs`,
`MTM_Waitlist.Settings/ViewModels/UrgencyAllotmentEditorViewModel.cs`,
`MTM_Waitlist.Settings/ViewModels/ComputerManagementViewModel.cs`,
`MTM_Waitlist.Settings/Services/DefectTypeCatalogService.cs`,
`MTM_Waitlist.Tests/Module_Settings/SettingsViewModelTests.cs`,
`MTM_Waitlist.Tests/Module_Settings/UrgencyAllotmentEditorViewModelTests.cs`,
`MTM_Waitlist.Tests/Module_Settings/Services/DefectTypeCatalogServiceTests.cs`,
`MTM_Waitlist.Tests/Module_Settings/RequestItemImagesDialogViewModelTests.cs`,
`MTM_Waitlist.Setup/Services/DunnageWorkflowService.cs`,
`MTM_Waitlist.Setup/ViewModels/SetupWorkCenterViewModel.cs`,
`MTM_Waitlist.Tests/Module_Setup/Services/SetupDunnageWorkflowServiceTests.cs`

### Tests

**Wave 1 — the tests that must fail before the migration (two different new files):**

- [x] **T034** [P] [US4] Write the comparison the seed demands, in both directions, and the subset rule beside it: every seeded baseline row equals the retired list it replaces for that role and every retired list's membership is reproduced exactly, including the two lists that were not on the earlier count; and whatever `permission.settings.cache_refresh` admits is also admitted by `permission.cache.refresh_api`, asserted against the shipped baselines rather than against either side's code, because the two permissions live in different projects and neither can see the other. The comparison is per role and per permission, and zero differences are tolerated. The three keys with no predecessor — `permission.admin.users` and `permission.admin.reset_password`, whose baselines ship to Production Lead, Setup Lead, Material Handler Lead, Plant Manager, IT Department and Developer, and `permission.admin.permissions`, whose baseline ships to IT Department, Plant Manager and Developer only — are asserted against those stated sets rather than against a retired list, which is what makes SC-001 evaluable for them. **It also carries the declaration-versus-baseline comparison in both directions, moved here from T024 because the baselines do not exist until T036 writes them:** every key the declaration holds carries a baseline row for every role the catalogue holds, and every baseline row names a declared key, so the two are checked against each other rather than one being trusted to describe the other · `MTM_Waitlist.Tests/Module_Settings/Services/PermissionBaselineParityTests.cs`, `MTM_Waitlist.Tests/Module_Mock_Service/ServiceOperatorPermissionTests.cs` · FR-047, FR-052, FR-057, SC-001, §6 gate 7
- [x] **T035** [P] [US4] Write the audit that proves no gate reads its own role list, enumerating **all thirteen** sites the specification's Context counts: the twelve retired role lists in nine files — the eleven that become named permissions, plus the badge map, which is re-keyed to role codes and must not become a permission — and `StartupState.IsDeveloper`, the thirteenth site in a tenth file, which compares a display name and is the one a hand count of twelve missed. The audit fails when a source file outside the declaration holds a role-name array used to decide access, and it fails when a gate asks for a key the declaration does not hold. It reads the keys from the declaration (T016), which is the one canonical source for the pinned spellings, rather than restating the literals · `MTM_Waitlist.Tests/Module_Core/Permissions/PermissionGateSiteAuditTests.cs` · FR-054, FR-058, FR-061, SC-002, SC-003, §6 gate 8

**⟶ Wait for Wave 1 to finish, then:**

### Implementation

**Wave 2 — the shipped baselines (single task, no parallel sibling):**

- [x] **T036** [US4] Seed one `role`-scoped row per permission per role, keyed `role:<role_code>`, `value_type` `bool`, with answers set to today's membership for the eleven keys that replace a retired list, so nobody's access changes on ship day, and with the three keys that have no predecessor set to their stated baseline sets: `permission.admin.users` and `permission.admin.reset_password` ship to Production Lead, Setup Lead, Material Handler Lead, Plant Manager, IT Department and Developer, and `permission.admin.permissions` ships to IT Department, Plant Manager and Developer only. The rows are keyed to **IT Department and not the retired role**, which is why this task sits after Phase 4, where T033 re-applies the forward rename once the reversal has been proved and leaves `it_department` in the catalogue and `admin` out of it. This is the seeded data the declaration's two-direction check ties to the declaration, so the baselines are data and not code (FR-046, FR-048). The dead `Setup Tech` entry is dropped from work-centre setup's baseline because it matches no catalogue role, and Material Handler Lead's baseline is the most restrictive sensible one: the worker level, plus handling requests and the service cache refresh, and nothing else. No per-person row is seeded anywhere · `Database/Seeds/seed_permission_role_baselines/`, `Database/AllSeeds.sql` · FR-046, FR-047, FR-048, FR-051, FR-052, SC-004, §6 gate 7

**⟶ Wait for Wave 2 to finish, then:** the gate-site replacements, which read the permissions the baselines answer.

**Wave 3 — the gate-site replacements, independent (four different projects):**

- [x] **T037** [P] [US4] Replace the request action policy's handler list with `permission.requests.handle`, refused at the action as well as at the control so a caller that never draws the screen is still refused, and update its tests including the case where the store cannot be reached and the fallback answers instead of refusing. **Three call sites are reached through that policy and must move with it, or the handle gate is only half-replaced:** `WaitlistViewViewModel`, `WaitlistViewDetailViewModel` and `WaitlistRequestService` each compare a role name today, and each becomes a reader of `permission.requests.handle` in the same change — leaving them behind would mean the policy is replaced while the screens that call it keep their own answers · `MTM_Waitlist.Core/Services/RequestActionPolicy.cs`, `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs`, `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewDetailViewModel.cs`, `MTM_Waitlist.Waitlist.View/Services/WaitlistRequestService.cs`, `MTM_Waitlist.Tests/Core/Services/RequestActionPolicyTests.cs` · FR-050, FR-054, FR-055, FR-056, FR-117
- [x] **T038** [P] [US4] Replace the service host's operator list with `permission.cache.refresh_api`, resolved from the same store through the same declaration rather than kept as a copy, so a person the Settings panel admits is admitted here too · `MTM_Waitlist.Mock.Service/Api/ServiceOperatorRoles.cs` · FR-057, decision 7
- [x] **T039** [P] [US4] Replace the four Settings lists — ignored locations, hot work centres, part pictures and the cache refresh — with their four named permissions, and add the two Administration entries' visibility flags (`permission.admin.users` and `permission.admin.permissions`) to the same view model, so the Settings screen asks the permission service once for all six answers and a reader entitled to neither entry does not see the category at all. Do the same for the urgency editor and the defect-type catalogue, removing the `administrator` entry that matches no catalogue role rather than carrying it forward, and for the computer registry. Update the four Settings test files in the same change, including the reflection lookup that names the two arrays this task deletes · `MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs`, `MTM_Waitlist.Settings/ViewModels/UrgencyAllotmentEditorViewModel.cs`, `MTM_Waitlist.Settings/ViewModels/ComputerManagementViewModel.cs`, `MTM_Waitlist.Settings/Services/DefectTypeCatalogService.cs`, `MTM_Waitlist.Tests/Module_Settings/SettingsViewModelTests.cs`, `MTM_Waitlist.Tests/Module_Settings/UrgencyAllotmentEditorViewModelTests.cs`, `MTM_Waitlist.Tests/Module_Settings/Services/DefectTypeCatalogServiceTests.cs`, `MTM_Waitlist.Tests/Module_Settings/RequestItemImagesDialogViewModelTests.cs` · FR-054, FR-056, FR-081, FR-107, FR-117
- [x] **T040** [P] [US4] Replace the dunnage Quick Add list with `permission.setup.dunnage_quick_add` and the work-centre setup list with `permission.setup.work_centers`, dropping the `Setup Tech` entry so a plain Setup person is not given work-centre setup by default and the owner flips it on the permissions page instead, and update the suite that used the deleted list · `MTM_Waitlist.Setup/Services/DunnageWorkflowService.cs`, `MTM_Waitlist.Setup/ViewModels/SetupWorkCenterViewModel.cs`, `MTM_Waitlist.Tests/Module_Setup/Services/SetupDunnageWorkflowServiceTests.cs` · FR-054, FR-056, FR-107

**⟶ Wait for Wave 3 to finish, then:**

**Wave 4 — the audit run (single task, runs alone):**

- [x] **T041** [US4] Run the audit, the parity test and the subset test together and record the result: all thirteen sites the specification's Context counts reported and none reading its own role list, every key declared and every declared key read, the parity comparison at zero differences for the eleven keys that replace a retired list, the three keys with no predecessor matching their stated baseline sets, and the subset rule holding. The per-person side of the ship-day comparison needs a live store and belongs to T072; record it here as deferred rather than as passed · `MTM_Waitlist.Tests/Module_Core/Permissions/PermissionGateSiteAuditTests.cs`, `MTM_Waitlist.Tests/Module_Settings/Services/PermissionBaselineParityTests.cs` · SC-001, SC-002, SC-003, §6 gates 7–8

**Checkpoint** — every gated action is one named permission, the baselines reproduce today's access exactly with zero
per-person rows stored, and the audit that a missed gate fails the suite is green. This story is independently
functional and testable here.

---

## Phase 6: US1 — Find a person and open their account (Priority: P1)

**Goal**: an Administration category in Settings whose Users entry opens a roster of everyone, searchable across the
four facts people know and filterable by role.
**Independent test**: open Settings, open Users, confirm the roster appears with each person's role and status;
search by sign-in name, name, employee number and role; apply a role filter, open a person, return, and confirm the
filter is applied again and the page says so.
**Files**: `MTM_Waitlist.Settings/Models/UserSummary.cs`, `MTM_Waitlist.Settings/Models/UserListFilter.cs`,
`MTM_Waitlist.Settings/ViewModels/UserManagementViewModel.cs`,
`Module_Settings/Views/UserManagementPage.xaml`, `Module_Settings/Views/UserManagementPage.xaml.cs`,
`Module_Settings/Views/SettingsPage.xaml`, `Module_Settings/Views/SettingsPage.xaml.cs`,
`Services/DependencyInjection/ServiceRegistrationExtensions.UserManagement.cs`,
`MTM_Waitlist.Tests/Module_Settings/ViewModels/UserManagementViewModelTests.cs`,
`MTM_Waitlist.Tests/Module_Settings/SettingsPageMarkupTests.cs`

### Implementation

**Wave 1 — independent (two different files):**

- [x] **T042** [P] [US1] Add the list's row and filter models: the five facts a row always shows — sign-in name, name, employee number, role and active state — and the saved filter's shape, which holds the search text and the chosen role code as one small payload so a half-saved filter cannot exist · `MTM_Waitlist.Settings/Models/UserSummary.cs`, `MTM_Waitlist.Settings/Models/UserListFilter.cs` · FR-086, FR-087, FR-089, FR-094
- [x] **T043** [P] [US1] Add the Settings view's two Administration rows as clickable cards that navigate rather than expanders, each wrapped in `x:Load` and carrying an `x:Name` because `x:Load` requires one, each with a short description of what it opens, both reachable from the Settings search, and neither shown to a reader who cannot use it, so a reader entitled to one sees that one and still sees the category and a reader entitled to neither does not see the category at all · `Module_Settings/Views/SettingsPage.xaml`, `Module_Settings/Views/SettingsPage.xaml.cs` · FR-080, FR-081, FR-082, FR-084, FR-117, contract "routes"

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2 — the roster view model (single task, no parallel sibling):**

- [x] **T044** [US1] Implement the roster view model: load the list without blocking the interface thread, search across sign-in name, display name, employee number and role, filter by role, remember the filter for the person and apply it on return while naming it and saying how many people it is hiding, and give the page its five distinct states — everyone with switched-off people marked, a filter matching nobody stated distinctly from an empty roster, an unreadable filter falling back to everyone with the failure stated, a filter naming a role that no longer exists falling back to everyone and saying the filter no longer applies, and an unreachable store showing an unavailable state with a manual retry, never an empty list and never a sample row. A row keeps all five facts at every width and folds onto a second line rather than cropping, a row's single job is to open that person, and the page re-checks the reader's entitlement when it is activated rather than acting on the answer it was opened with · `MTM_Waitlist.Settings/ViewModels/UserManagementViewModel.cs` · FR-085 to FR-096, FR-114

**⟶ Wait for Wave 2 to finish, then:** the page, its registration and the suites.

**Wave 3 — independent (two different file sets):**

- [x] **T045** [P] [US1] Build the roster page — a virtualised list, each row one keyboard-reachable target that announces what it opens, no control with a fixed width, no sample row anywhere, the active filter named with a way to show everyone, and Back returning to Settings without walking back through repeats of the page — and register it through the partial method declared in T021, in this phase's own registration file · `Module_Settings/Views/UserManagementPage.xaml`, `Module_Settings/Views/UserManagementPage.xaml.cs`, `Services/DependencyInjection/ServiceRegistrationExtensions.UserManagement.cs` · FR-083, FR-094, FR-095, FR-113, FR-115, FR-116, constitution V
- [x] **T046** [P] [US1] Assert the roster's behaviour and the Settings markup: search matching each of the four fields in turn, the role filter narrowed and restored with its statement, all five row states including the two that must never render as an empty list, the row count staying responsive with a large roster, the two Administration rows carrying their own resource keys with no two labels sharing one, and neither row being an expander · `MTM_Waitlist.Tests/Module_Settings/ViewModels/UserManagementViewModelTests.cs`, `MTM_Waitlist.Tests/Module_Settings/SettingsPageMarkupTests.cs` · FR-080, FR-082, FR-084, FR-086 to FR-096, SC-011, SC-012, SC-017, SC-020

**Checkpoint** — a reader entitled to it can open the Administration category, open Users, see everyone with
switched-off people marked, search the four facts, filter by role, open a person, come back with the filter applied
and named, and meet a stated unavailable state rather than an empty list when the store cannot be read. This story is
independently functional and testable here.

---

## Phase 7: US2 — Create a person, reset a password, and correct one (Priority: P1)

**Goal**: creating, correcting, deactivating, reactivating and resetting a person, with a one-time PIN handed over in
a window that must be closed deliberately and can be printed.
**Independent test**: create a person, confirm the PIN window appears and can be printed, correct their name and
sign-in name, reset their password, confirm a new PIN is issued, and confirm each change is recorded against the
actor.
**Files**: `MTM_Waitlist.Settings/Models/UserDetail.cs`, `MTM_Waitlist.Settings/Models/UserEditRequest.cs`,
`MTM_Waitlist.Settings/Models/UserManagementResults.cs`, `MTM_Waitlist.Settings/ViewModels/CreateUserViewModel.cs`,
`MTM_Waitlist.Settings/ViewModels/EditUserViewModel.cs`,
`MTM_Waitlist.Settings/ViewModels/PinRevealDialogViewModel.cs`,
`Module_Settings/Views/CreateUserPage.xaml`, `Module_Settings/Views/CreateUserPage.xaml.cs`,
`Module_Settings/Views/EditUserPage.xaml`, `Module_Settings/Views/EditUserPage.xaml.cs`,
`Module_Settings/Views/PinRevealDialog.xaml`, `Module_Settings/Views/PinRevealDialog.xaml.cs`,
`Services/DependencyInjection/ServiceRegistrationExtensions.UserAccount.cs`,
`MTM_Waitlist.Tests/Module_Settings/ViewModels/CreateUserViewModelTests.cs`,
`MTM_Waitlist.Tests/Module_Settings/ViewModels/EditUserViewModelTests.cs`,
`MTM_Waitlist.Tests/Module_Settings/ViewModels/PinRevealDialogViewModelTests.cs`

### Implementation

**Wave 1 — independent (three different files):**

- [x] **T047** [P] [US2] Add the person's detail, the edit request and the typed outcomes a caller reads — a duplicate sign-in name arriving as one typed "already taken" result from the provider's `1062`, each store refusal arriving as its `mtm_*` token turned into one typed reason, and one typed unavailable outcome that keeps the reader's work — and implement the create form's view model beside them: the five fields, validation of the length rules and the four-digit employee number with the limit stated in the reader's words and what was typed kept, no active control at all, a role picker offering only roles at or below the reader's own rung and stating that bound in words rather than listing withheld roles as dead entries, and a save guarded against a repeated press so one press creates exactly one person · `MTM_Waitlist.Settings/Models/UserDetail.cs`, `MTM_Waitlist.Settings/Models/UserEditRequest.cs`, `MTM_Waitlist.Settings/Models/UserManagementResults.cs`, `MTM_Waitlist.Settings/ViewModels/CreateUserViewModel.cs` · FR-004, FR-005, FR-007, FR-008, FR-022, FR-026, FR-101, FR-104, SC-010
- [x] **T048** [P] [US2] Implement the person's page view model: fields edited above and actions below as one page with a read-only state rather than a second page, a reader who cannot act on the person told so at the top with the fields and actions shown unavailable and the reason in words rather than hidden, deactivating the reader's own account and changing its own sign-in name shown unavailable with their reasons, an action chosen while edits are unsaved requiring an explicit save or discard rather than acting on a half-edited person, a failed action keeping what the reader had, the sign-in name corrected through this same page, the deactivation sentence naming both halves — the person cannot sign in again and a session already open stays open until they close the application — and repeated on the page itself so it is findable after the confirmation is gone, reactivation saying access returns from the next sign-in, and save guarded against a repeated press, with the reset action gated on `permission.admin.reset_password` so the control and the action answer from the same lookup and a reader without the key is never offered it · `MTM_Waitlist.Settings/ViewModels/EditUserViewModel.cs` · FR-019 to FR-027, FR-028, FR-097, FR-098, FR-100, FR-101, FR-102, FR-103, FR-104, FR-117
- [x] **T049** [P] [US2] Implement the one-time PIN window's view model: a random four-digit PIN held only while the window is open and written nowhere, the person it is for, the sign-in name, when it was issued, who issued it, the instruction to change it at first sign-in and to hand it over rather than file it, printing that never closes the window and costs nothing when it fails or is cancelled, a close that did not come from its own button cancelled, and the statement that a second reset replaces the earlier PIN · `MTM_Waitlist.Settings/ViewModels/PinRevealDialogViewModel.cs` · FR-028, FR-030, FR-032, FR-033, FR-034, SC-007

**⟶ Wait for Wave 1 to finish, then:** the three views and their registration.

**Wave 2 — independent (three different file sets):**

- [x] **T050** [P] [US2] Build the create page as a form only with no actions area, its fields localised, no fixed width, and its role picker reading the one catalogue · `Module_Settings/Views/CreateUserPage.xaml`, `Module_Settings/Views/CreateUserPage.xaml.cs` · FR-099, FR-113, FR-116
- [x] **T051** [P] [US2] Build the person's page with the fields in a form at the top and the actions in a clearly separated area below, each with its own heading, and a reset action behind a confirmation, shown only to a reader holding `permission.admin.reset_password` and refused at the action as well as absent from the control · `Module_Settings/Views/EditUserPage.xaml`, `Module_Settings/Views/EditUserPage.xaml.cs` · FR-028, FR-097, FR-117
- [x] **T052** [P] [US2] Build the PIN dialog with one close button and one print button, cancelling every close that did not come from its own button in the closing handler so Escape, a tap outside and every other dismissal take the same refused path, with printing routed through the existing report print service so it never closes the window; and register the create page, the person's page and the PIN dialog through the partial method declared in T021 · `Module_Settings/Views/PinRevealDialog.xaml`, `Module_Settings/Views/PinRevealDialog.xaml.cs`, `Services/DependencyInjection/ServiceRegistrationExtensions.UserAccount.cs` · FR-034, FR-080, constitution V, research "how a dismissal of the PIN window is refused"

**⟶ Wait for Wave 2 to finish, then:**

**Wave 3 — the suites (single task, runs alone):**

- [x] **T053** [US2] Assert the three flows: one press creates exactly one person; a duplicate sign-in name becomes the one typed "already taken" answer naming the name with the typed value kept and no retry; an over-length name is refused with the limit stated and the typed value kept; the picker offers nothing above the reader's rung; the derived display name stays inside its limit; the read-only state for an account that outranks the reader states its reason; the two self-lockout refusals appear on the reader's own account; an action with unsaved edits requires save or discard; a failed action keeps the reader's work; peers are editable; the PIN is four digits and is reachable nowhere after the window closes; the window survives a refused dismissal and a failed print; two people sharing one employee number (`6229`) are both created and both listed, because the number is deliberately not unique; and a second reset states that the earlier PIN no longer works · `MTM_Waitlist.Tests/Module_Settings/ViewModels/CreateUserViewModelTests.cs`, `MTM_Waitlist.Tests/Module_Settings/ViewModels/EditUserViewModelTests.cs`, `MTM_Waitlist.Tests/Module_Settings/ViewModels/PinRevealDialogViewModelTests.cs` · FR-004, FR-005, FR-007, FR-008, FR-019 to FR-027, FR-030, FR-032, FR-034, FR-100, FR-101, FR-104, SC-007, SC-010

**Checkpoint** — a person can be created, corrected, switched off and back on, and have a password reset, each with a
one-time PIN handed over in a window that must be closed deliberately and can be printed, and each change recorded
against the actor. This story is independently functional and testable here.

---

## Phase 8: US5 — Change what one person may do (Priority: P2)

**Goal**: a Plant Manager changes one permission for one person, sees what the change means before it is written, and
can undo it afterwards.
**Independent test**: as a Plant Manager, change one permission for one person, confirm the confirmation names that
person and that feature, confirm the change takes effect for that person and for nobody else, then undo it.
**Files**: `Database/StoredProcedures/sp_config_permissions_user_set/`,
`Database/StoredProcedures/sp_config_permissions_feature_holders_get/`, `Database/AllSPs.sql`,
`MTM_Waitlist.Core/Contracts/Services/IPermissionAdministrationService.cs`,
`MTM_Waitlist.Core/Services/PermissionAdministrationService.cs`,
`MTM_Waitlist.Settings/Models/PermissionRow.cs`, `MTM_Waitlist.Settings/Models/PermissionChangeSet.cs`,
`MTM_Waitlist.Settings/ViewModels/PermissionsViewModel.cs`, `Module_Settings/Views/PermissionsPage.xaml`,
`Module_Settings/Views/PermissionsPage.xaml.cs`,
`Services/DependencyInjection/ServiceRegistrationExtensions.Permissions.cs`,
`MTM_Waitlist.Tests/Module_Settings/Services/PermissionChangeSetServiceTests.cs`,
`MTM_Waitlist.Tests/Module_Settings/ViewModels/PermissionsViewModelTests.cs`

### Implementation

**Wave 1 — the two procedures (single task, no parallel sibling):**

- [x] **T054** [US5] Add the change-set procedure: in one transaction, refuse a target that outranks the actor and a reversal by somebody who could not have made the change, refuse any entry naming the permission that opens the page, refuse an entry outside the permission namespace, refuse when the stored value is not the `from` the caller last saw and report which key moved, write one row per changed key with one history row per changed key carrying the actor, the key, the scope, both values and the time, and write nothing at all when the set is empty. The actor's rung is read inside the procedure and never taken from its caller. **This is the first place a per-person permission row is ever written, and it must not run before T006 has shipped.** Add the holders read beside it — every person whose stored value differs from their role's baseline, with the facts the second view needs and nothing else, so that view reads only the people who differ — and regenerate `AllSPs.sql` for both · `Database/StoredProcedures/sp_config_permissions_user_set/`, `Database/StoredProcedures/sp_config_permissions_feature_holders_get/`, `Database/AllSPs.sql` · FR-025, FR-053, FR-059, FR-069, FR-070, FR-071, FR-072, FR-076, FR-079, constitution III, decision 30

**⟶ Wait for Wave 1 to finish, then:** the page-facing service and the page.

**Wave 2 — independent (three different file sets):**

- [x] **T055** [P] [US5] Implement the page-facing permissions service behind the two procedures: read one person's rows with each row stating what the feature is, what it gates, the value in force and whether that value came from their role's baseline or from a choice made for them; the change-set write as one call, so a save of several permissions and its reversal are the same atomic call; the reversal reading the recorded history and obeying the same rule as the change it reverses; and the invalidation of the resolution cache on a successful save · `MTM_Waitlist.Core/Contracts/Services/IPermissionAdministrationService.cs`, `MTM_Waitlist.Core/Services/PermissionAdministrationService.cs` · FR-063, FR-065, FR-069, FR-070, FR-071, FR-072
- [x] **T056** [P] [US5] Add the page's models and its view model: one row per permission carrying its declared label, its what-it-gates sentence, its value, its provenance and its pending state, and a change set carrying both values per entry so the store can refuse a value that has moved; then the view model itself — one person at a time with a column of people and the chosen person's features as a list rather than a grid of roles against features, a person who outranks the reader selectable to view with their rows shown unavailable and the reason in words, the fixed row present and locked with its reason and clearable by nobody, pending changes marked until saved, saving with nothing changed unavailable and writing no history record, a confirmation naming in the reader's words what changes and for whom with one sentence per changed row and a count when several change, an undo that reverses the whole save rather than half of it and that shows what the value is now and asks before restoring when it has moved, and a warning stating how many rows are pending before the page is left · `MTM_Waitlist.Settings/Models/PermissionRow.cs`, `MTM_Waitlist.Settings/Models/PermissionChangeSet.cs`, `MTM_Waitlist.Settings/ViewModels/PermissionsViewModel.cs` · FR-059, FR-063 to FR-074, SC-010
- [x] **T057** [P] [US5] Build the permissions page — the column of people, the chosen person's feature list, the fixed row rendered locked with its reason, no control with a fixed width, and Back returning to Settings without walking back through repeats of the page — and register it through the partial method declared in T021, in this phase's own registration file · `Module_Settings/Views/PermissionsPage.xaml`, `Module_Settings/Views/PermissionsPage.xaml.cs`, `Services/DependencyInjection/ServiceRegistrationExtensions.Permissions.cs` · FR-063, FR-080, FR-083, FR-113, FR-116, constitution V

**⟶ Wait for Wave 2 to finish, then:** the suites.

**Wave 3 — independent (two different test files):**

- [x] **T058** [P] [US5] Assert the change-set service against a live store in one opt-in, environment-gated run: a save of several permissions writes exactly its own number of history rows, each naming the actor and both values; the whole save reverses and restores every one of them; a value that has moved is refused with the key reported; the entry naming the permission that opens the page is refused; an entry outside the namespace is refused; an empty set writes nothing; and a refused change writes no history row while still being reported plainly · `MTM_Waitlist.Tests/Module_Settings/Services/PermissionChangeSetServiceTests.cs` · FR-026, FR-059, FR-068, FR-070, FR-071, FR-072, SC-009
- [x] **T059** [P] [US5] Assert the page's behaviour: the confirmation's wording and its count, saving with nothing changed unavailable, pending rows marked, the undo reversing the whole save, the moved-value prompt before restoring, the outranking person shown unavailable with the reason, the fixed row locked and clearable by nobody, the unsaved-change warning stating its count, and one impatient press of save writing exactly one change, with no screen in this feature freezing or refusing interaction while it loads or saves · `MTM_Waitlist.Tests/Module_Settings/ViewModels/PermissionsViewModelTests.cs` · FR-066, FR-067, FR-068, FR-069, FR-070, FR-073, FR-074, FR-114, SC-010

**Checkpoint** — a permission can be changed for one person, the change takes effect for that person and for nobody
else, it is confirmed in the reader's words before it is written, exactly one history record per changed permission is
created, and the whole save can be undone. This story is independently functional and testable here.

---

## Phase 9: US6 — Ask who holds a feature (Priority: P2)

**Goal**: a second view on the same page answering which roles' baselines give a feature, plus only the people who
differ from their role.
**Independent test**: choose a feature, confirm the roles whose baselines give it are listed from the seeded
baselines the declaration's check ties to it rather than from a second copy of the answers, confirm only the people
who differ are named individually, and confirm that opening one of them leads to their page.
**Files**: `MTM_Waitlist.Settings/ViewModels/PermissionHoldersViewModel.cs`,
`MTM_Waitlist.Settings/Models/PermissionHoldersView.cs`,
`Module_Settings/Views/PermissionHoldersView.xaml`, `Module_Settings/Views/PermissionHoldersView.xaml.cs`,
`Module_Settings/Views/PermissionsPage.xaml`,
`MTM_Waitlist.Tests/Module_Settings/ViewModels/PermissionHoldersViewModelTests.cs`

### Implementation

**Wave 1 — independent (two different file sets):**

- [x] **T060** [P] [US6] Add the view's models — the roles whose baselines give the chosen feature, taken from the seeded baselines that the declaration's two-direction check ties to it and read once rather than restated beside the view so the two cannot drift, and the differing people each marked granted or denied and marked switched off where they are — and the view model that answers who holds a feature: the roles from the seeded baselines, then only the people who differ from their role's baseline in either direction, a feature nobody holds stated in words rather than shown as an empty region, a switched-off person listed and marked switched off, and nothing changed anywhere in the view because it is a view and every change still happens on the person's own page · `MTM_Waitlist.Settings/Models/PermissionHoldersView.cs`, `MTM_Waitlist.Settings/ViewModels/PermissionHoldersViewModel.cs` · FR-046, FR-047, FR-048, FR-075, FR-076, FR-077, FR-078, FR-079
- [x] **T061** [P] [US6] Build the who-holds-this control: the feature picker, the role list, the differing people each opening their own page, no fixed width, and the "nobody holds this" statement in words · `Module_Settings/Views/PermissionHoldersView.xaml`, `Module_Settings/Views/PermissionHoldersView.xaml.cs` · FR-075, FR-077, FR-078, FR-079, FR-113

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2 — the host and the suite:**

- [x] **T062** [US6] Host the who-holds-this view on the permissions page, so the two views of the same data sit on one page as the UI contract requires. This is the one path two phases share, and Phase 9 runs strictly after Phase 8 created the page · `Module_Settings/Views/PermissionsPage.xaml` · FR-075, contract "the permissions page"
- [x] **T063** [US6] Assert the view: the roles come from the seeded baselines that the declaration's check ties to it, and not from a second copy of the answers, only differing people are named, a person who differs in the denied direction is named and marked, a feature nobody holds is stated in words, a switched-off holder is listed and marked, and nothing in the view writes anything · `MTM_Waitlist.Tests/Module_Settings/ViewModels/PermissionHoldersViewModelTests.cs` · FR-075 to FR-079, SC-014

**Checkpoint** — the question "who can change hot work centres?" has a short answer naming every role whose baseline
gives it and only the people who differ. This story is independently functional and testable here.

---

## Phase 10: US7 — A temporary credential stops after five wrong tries (Priority: P2)

**Goal**: five wrong tries with a temporary value stop it being accepted, the count survives a restart and does not
expire with time, and the remaining attempts are shown in the reader's words.
**Independent test**: five wrong tries with a PIN stop it working; the sixth attempt with the correct PIN is refused;
closing and reopening the application does not restore it; a fresh reset clears it and issues a working PIN.
**Files**: `MTM_Waitlist.Startup/ViewModels/LoginViewModel.cs`, `Module_Startup/Views/LoginPage.xaml`,
`MTM_Waitlist.Tests/ViewModels/LoginViewModelTests.cs`,
`MTM_Waitlist.Tests/Module_Startup/Services/TemporaryCredentialLimitTests.cs`

### Implementation

**Wave 1 — the surface (single task, no parallel sibling):**

- [x] **T064** [US7] Show the remaining attempts on the sign-in view model, once at least one attempt has failed and never before, and tell the person that someone entitled must issue a fresh reset when the count has been reached. Behaviour for an ordinary account with a real password, and for a sign-in name that does not exist, is exactly as it is today, so the limit cannot be used to discover which names exist. Add the remaining-attempts line to the sign-in page, localised, shown only when there is something to say, and with no reset offered anywhere on the screen · `MTM_Waitlist.Startup/ViewModels/LoginViewModel.cs`, `Module_Startup/Views/LoginPage.xaml` · FR-028, FR-040, FR-041, FR-042, FR-044, FR-112

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2 — independent (two different test files):**

- [x] **T065** [P] [US7] Update the sign-in view-model tests: the remaining-attempts message appears after the first failure and not before, the reached-the-limit message says who can issue a fresh reset, and an ordinary account and an unknown sign-in name both behave exactly as they did before · `MTM_Waitlist.Tests/ViewModels/LoginViewModelTests.cs` · FR-040, FR-041, FR-042, FR-044
- [x] **T066** [P] [US7] Prove the limit end to end: five failed attempts against a temporary value stop it being accepted; a sixth attempt with the correct value is refused; the count survives closing and reopening the application because it is held with the account; waiting does not restore it because nothing expires it; a successful sign-in clears it; a fresh reset clears it and issues a working value; the legacy temporary value `0000` is limited exactly like a new one; a reset performed while that person is signed in leaves their session running until they close the application, because a reset ends no session (FR-035); and the count is never presented as a record of who tried · `MTM_Waitlist.Tests/Module_Startup/Services/TemporaryCredentialLimitTests.cs` · FR-031, FR-035, FR-036, FR-037, FR-038, FR-039, FR-043, SC-008

**Checkpoint** — a PIN handed over on paper has an attempt limit that survives a restart and does not expire with
time, and the person is told both how many attempts remain and how a fresh credential is issued. This story is
independently functional and testable here.

---

## Phase 11: US8 — Every role gets its own badge (Priority: P3)

**Goal**: the badge lookup is keyed on the role code and covers every role the catalogue holds, and the retired
vocabulary is gone rather than left in place.
**Independent test**: sign in as a holder of each role in the catalogue and confirm each shows its own badge, with
none falling to the grey default.
**Files**: `MTM_Waitlist.Core/Permissions/RoleBadgeCatalog.cs`, `ViewModels/ShellViewModel.cs`,
`MTM_Waitlist.Tests/Module_Core/Permissions/RoleBadgeCatalogTests.cs`,
`MTM_Waitlist.Tests/Module_Core/ViewModels/ShellViewModelTests.cs`

### Implementation

**Wave 1 — independent (two different files):**

- [x] **T067** [P] [US8] Build the badge lookup keyed on the role code with one entry per role the catalogue holds, a fallback reachable only by a genuinely unknown or blank role, and the branches naming roles the catalogue has never held — `administrator`, `supervisor`, `manager`, `quality`, `quality inspector` and `Setup Tech` — removed rather than carried forward · `MTM_Waitlist.Core/Permissions/RoleBadgeCatalog.cs` · FR-105, FR-106, FR-107
- [x] **T068** [P] [US8] Point the shell's badge at the lookup and at the signed-in person's role code, so every role the catalogue holds gets its own badge instead of the grey default, and the badge stays presentation rather than becoming a permission · `ViewModels/ShellViewModel.cs` · FR-058, FR-105

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2 — the suites (single task, runs alone):**

- [x] **T069** [US8] Assert the lookup covers every role the catalogue holds, that the count of roles reaching the grey default is zero, and that the fallback is reachable only by a genuinely unknown or blank role; and update the shell's tests to the role code, adding the case that the badge does not appear on the permissions page because it decides how a person looks rather than what they may do · `MTM_Waitlist.Tests/Module_Core/Permissions/RoleBadgeCatalogTests.cs`, `MTM_Waitlist.Tests/Module_Core/ViewModels/ShellViewModelTests.cs` · FR-058, FR-105, FR-106, SC-015, SC-020

**Checkpoint** — every role in the catalogue has its own badge and none reaches the grey default. This story is
independently functional and testable here.

---

## Phase 12: Polish

**Goal**: the documentation, the live-database proofs and the success-criteria validation that no single story owns.

**Files**: `Database/Database-Ruleset.md`, `specs/006-user-management-and-permissions/quickstart.md`, `FEATURES.md`,
`CHANGELOG.md`

**Wave 1 — independent (two different file sets):**

- [x] **T070** [P] Record the `role` scope type and the corrected precedence order in the database ruleset, and write the two orderings the quickstart proves into the quickstart itself — the re-rank before any per-person row, and the rename, its reversal **and the re-application of the rename** before the baselines — so both are found by reading rather than remembered · `Database/Database-Ruleset.md`, `specs/006-user-management-and-permissions/quickstart.md` · FR-053, FR-013, constitution "Documentation & Extensibility"
- [x] **T071** [P] Remove the retired role vocabulary and add this feature's catalogue entry, in the same change as the code rather than after it · `FEATURES.md`, `CHANGELOG.md` · FR-107, constitution "Documentation & Extensibility"

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2 — the live proofs (single task, runs alone):**

- [x] **T072** Run the opt-in live-database integration checks against a live `mtm_waitlist` in one gated pass: a transactional create writes the profile and the role assignment together and rolls both back on a mid-way failure; a duplicate sign-in name arrives as `1062` and becomes one typed answer; the rank rule refuses a target above the actor with no screen involved; the self-lockout and self-rename refusals are refused in the store; a reset writes one audit row with no value on either side; deactivating a person leaves their existing sessions alone and a reset does not end one, so the session case in §6 gate 2 and FR-035's reset case both have an owner; the audit shares one change identifier across the records of a single save; and every person's effective set on ship day equals exactly what their role's retired list gave them for the eleven keys that replace one, with the three keys that have no predecessor matching their stated baseline sets, and with zero per-person permission rows stored · `MTM_Waitlist.Tests/Module_Settings/Services/PermissionBaselineParityTests.cs`, `MTM_Waitlist.Tests/Module_Settings/Services/PermissionChangeSetServiceTests.cs`, `MTM_Waitlist.Tests/Module_Settings/Services/RoleRenameMigrationTests.cs` · FR-006, FR-008, FR-014, FR-025, FR-035, FR-052, FR-102, FR-111, SC-001, SC-004, SC-005, SC-006, SC-016, SC-019, §6 gate 3

**⟶ Wait for Wave 2 to finish, then:**

**Wave 3 — T073, T074 and T075 all record their outcome in the one evidence document, so they run in that order;
T076 reads the screens only and may run alongside any of them:**

- [x] **T073** Search the application, its logs and the store for a reset PIN after a reset and assert it is reachable nowhere as plaintext, and that the store holds no more than a salted hash of it, which is the one success criterion no unit test can make on its own · `specs/006-user-management-and-permissions/quickstart.md` · FR-030, SC-007
- [x] **T074** Run the whole suite and a clean solution build, stopping any running application first and clearing the build nodes. The suite must cover the eleven live cases §6 gate 2 names: rank refusal; self-lockout rejection; transactional rollback; duplicate-username typed result; uppercase normalisation (`jsmith` → `JSMITH`, signing in matching either case); employee-id non-uniqueness (`6229` shared by two people); search matching all four fields; UI-thread collection safety; deactivate without session revocation; double-click Save creating exactly one row; and an audit row carrying the acting user and the timestamp. `InlineSqlAuditTests` and `RetiredSymbolAuditTests` must be green, per §6 gate 4 and the plan's Constitution Check. Record an environment-gated case as skipped rather than as passing, and surface a `WMC9999` here rather than skipping it, because on this machine it masks a real XAML error. This task records its outcome in the evidence document and modifies no project or solution file · `specs/006-user-management-and-permissions/quickstart.md` · SC-018, §6 gates 1–2, 4, 6
- [x] **T075** Walk the running application once against a live store: the Administration category with both entries navigating rather than expanding, the roster with its filter remembered, the person's page with the two self-lockout refusals on the reader's own account, a create ending in the PIN window that survives a click outside it and Escape with printing leaving it open, the permissions page with its fixed row locked and saving nothing unavailable, and the five-attempt limit refusing the sixth correct attempt after a restart · `specs/006-user-management-and-permissions/quickstart.md` · SC-008, SC-012, SC-017, §6 gate 6
- [ ] **T076** [P] Walk the three new screens between 150 and 200 percent scaling and at the narrowest window the application permits, confirming all five facts of a list row stay visible at both widths and no control has a fixed width · `Module_Settings/Views/UserManagementPage.xaml`, `Module_Settings/Views/EditUserPage.xaml`, `Module_Settings/Views/PermissionsPage.xaml` · FR-113, FR-116, SC-011, §6 gate 5

**Checkpoint** — every success criterion in the specification has been validated by a build, a test or a verified live
check, and nothing is reported as passing on the strength of having been written.

---

## Dependencies & Execution Order

**Phase dependencies**

```text
Phase 1 Setup ──> Phase 2 Foundational ──┬──> Phase 3 US3 ladder ──> Phase 4 US9 rename ──> Phase 5 US4 gates
                                         │                                                   │
                                         │                                                   v
                                         │                                     Phase 6 US1 roster
                                         │                                                   │
                                         │                                                   v
                                         │                                    Phase 7 US2 accounts
                                         │                                                   │
                                         │                                  ┌────────────────┘
                                         │                                  v
                                         │                    Phase 8 US5 change one person
                                         │                                  │
                                         │                                  v
                                         │                     Phase 9 US6 who holds this
                                         │
                                         ├──> Phase 10 US7 attempt limit   (Phase 2 only)
                                         └──> Phase 11 US8 badges          (Phase 2 only)

Phase 12 Polish ── after every phase above
```

**Wave restatement, one line per phase**

- **Phase 1** — wave 1 (two fixtures) unblocks Phase 2.
- **Phase 2** — wave 1 (four schema artifacts, including the re-rank) → wave 2 (bootstrap and validators, eight procedures and one seed) → wave 3 (the declaration, `StartupState`) → wave 4 (permission service, user-management service and repository, sign-in repository) → wave 5 (registration, resources) → wave 6 (aggregates) → wave 7 (three suites). Each wave blocks the next.
- **Phase 3** — wave 1 (catalogue seed, catalogue read) → wave 2 (ladder readers) → wave 3 (aggregates) → wave 4 (the suite).
- **Phase 4** — wave 1 (rename, reversal, aggregate) → wave 2 (the live proof, which ends by re-applying the forward rename). **This phase gates Phase 5's baselines.**
- **Phase 5** — wave 1 (the failing tests) → wave 2 (the baselines) → wave 3 (four projects' gate sites) → wave 4 (the audit run).
- **Phase 6** — wave 1 (models, Settings rows) → wave 2 (roster view model) → wave 3 (page, registration, suites).
- **Phase 7** — wave 1 (models, create, edit, PIN view models) → wave 2 (three views, registration) → wave 3 (the suites).
- **Phase 8** — wave 1 (two procedures, aggregate) → wave 2 (service, models, view model, page, registration) → wave 3 (live suite, view-model suite).
- **Phase 9** — wave 1 (models, view model, control) → wave 2 (its host on the permissions page, the suite). Phase 9 follows Phase 8 because it hosts its view there.
- **Phase 10** — wave 1 (view model and view) → wave 2 (two suites). Needs Phase 2 only.
- **Phase 11** — wave 1 (badge lookup, shell) → wave 2 (the suites). Needs Phase 2 only.
- **Phase 12** — wave 1 (two documentation changes) → wave 2 (the live proofs) → wave 3 (the PIN search, the suite and build run, the application walk, the scaling pass).

**The two orderings that must not be reversed**

1. **T006 before T054.** The precedence re-rank ships in Phase 2's first wave; the first per-person permission row is
   written by the change-set procedure in Phase 8. Nothing in T054 may be applied to a store whose scope rank still
   puts `user` below `admin` and `developer`.
2. **T032 and T033 before T036.** The rename runs and its paired reversal is actually executed in Phase 4, and T033
   then re-applies the forward rename in the same run, so the phase ends with `it_department` in the catalogue and
   `admin` out of it; only then are the permission baselines seeded against `it_department` in Phase 5, so no
   baseline is written against a role that does not exist and none is written against the retired role.

**Parallel opportunities**

- Every `[P]` task sits in a wave whose files no other task in that wave touches, so an implementer may build them in
  any order or hand each to its own worker.
- Phases 10 and 11 need Phase 2 alone, so both may be built alongside Phases 3 to 9.
- Inside Phases 3 to 9 the waves are the only dependency order: a later wave may not start before the join line above
  it.
