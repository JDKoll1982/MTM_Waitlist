# Implementation Plan: User Management and Permissions

**Feature Branch**: `006-user-management-and-permissions`
**Spec**: `user-management-and-permissions.spec.md` (117 functional requirements, 9 user stories, 20 success criteria)
**Created**: 2026-09-20

## Scale note

This change spans about sixty files across five areas: the schema and its seeds, a new shared permission layer in
`MTM_Waitlist.Core`, the two administration pages with their view models, the sign-in path, and the gate sites the
specification's Context counts. A reader should watch three things above all.

First, the precedence re-rank of the settings scopes must ship **before** the first per-person permission row
exists, or a person's own value resolves under the wrong order and is silently overridden.

Second, the role rename and its **executed** paired reversal must complete before the permission baselines are
seeded, and the forward rename must then be re-applied, because the reversal puts the retired role back: a baseline
keyed to a role that does not exist is a baseline nobody holds.

Third, the gate-site work is what the specification's Context counts: of the twelve retired role lists, eleven
become named permissions and the twelfth — the badge map — is re-keyed to role codes and stays out of the
permission set, and the audit test also counts the thirteenth site that a hand count of the twelve missed. The only
thing that proves the replacement is complete is that audit test; a hand count has already been wrong once, having
missed two of the twelve.

## Summary

Give the application user management under a new Administration area in Settings, and replace the eleven of the
twelve hand-written role lists it carries today that decide access with one named permission per gated action,
re-keying the twelfth — the badge map — to role codes so it stays presentation rather than becoming a permission.
A permission belongs to the person, and the person's role supplies the baseline they start from, so changing what
somebody may do never means changing their rank. The plan reuses the existing scoped-settings store rather than
adding a table for permissions,
adds one ladder of role ranks to the role catalogue so both the screens and the stored procedures compare the same
numbers, and adds one audit table shaped as a field-change log. The stored procedures enforce the rank rule, the
self-lockout rule and the sign-in-name rule, because the screen cannot be trusted. The permission declaration, the
ladder readers and the role badge live in `MTM_Waitlist.Core`, which is the only project every reader can see: the
app, `MTM_Waitlist.Settings`, `MTM_Waitlist.Setup`, `MTM_Waitlist.Waitlist.View` and the separate
`MTM_Waitlist.Mock.Service` host all reference it.

## Project Structure

```text
Database/
  Tables/01_core_users_profiles/            first name, last name, temporary-credential attempt count
  Tables/03_auth_roles_catalog/             role_rank, the one rung per role
  Tables/32_auth_user_management_audit/     new: one row per changed field
  Functions/fn_config_settings_scope_rank/  the precedence re-rank
  StoredProcedures/sp_user_management_list/          user list, search and role filter
  StoredProcedures/sp_user_management_get/           one person
  StoredProcedures/sp_user_management_create/        profile + role in one transaction
  StoredProcedures/sp_user_management_update/        rank, self-lockout and sign-in-name rules
  StoredProcedures/sp_user_management_reset_password/ random PIN, forced change, count cleared
  StoredProcedures/sp_auth_roles_list/               the catalogue with its rungs
  StoredProcedures/sp_config_permissions_user_get/   one person's stored permission rows
  StoredProcedures/sp_config_permissions_user_set/   one change set, its history rows, one transaction
  StoredProcedures/sp_config_permissions_feature_holders_get/  only the people who differ
  StoredProcedures/sp_auth_temporary_credential_attempt_record/  count up, or clear
  StoredProcedures/sp_auth_credentials_check/        role code and the attempt count added
  StoredProcedures/sp_auth_user_row_get/             role code added
  StoredProcedures/sp_auth_password_reset_required_get/ role code added
  StoredProcedures/sp_config_settings_upsert/        header comment only: not the permission write path
  Seeds/seed_dev_masked_baseline/           the shipped roles, plus material_handler_lead and the rungs;
                                            leaves admin and it_department entirely to the rename seed
  Seeds/seed_role_admin_to_it_department/   remove `admin`, insert `it_department` at rung 90, repoint, one transaction
  Seeds/seed_permission_role_baselines/     the baselines as data
  Seeds/seed_username_upper_normalization/  migrate the older lower-case rows
  Bootstrap/create_database.sql             the fresh-store create path, with the new table and columns
  Bootstrap/update_table_descriptions.sql   the guarded column additions for a store that already exists
  Validation/user_management_schema/        new: the new table, columns and procedures
  Validation/settings_schema/               extended: the role scope and the rank order
MTM_Waitlist.Core/
  Permissions/PermissionKeys.cs             the fourteen keys, spelled exactly as the spec pins them
  Permissions/PermissionRegistry.cs         the one declaration: key, label, what it gates, area, shipped
                                            fallback and gate sites (no baselines — those are seeded data)
  Permissions/RoleAuthorization.cs          the ladder readers, and nothing else
  Permissions/RoleBadgeCatalog.cs           one badge per role code, plus the unknown-role fallback
  Models/StartupState.cs                    the role code and the signed-in person's id
  Contracts/Services/IRoleCatalogService.cs Services/RoleCatalogService.cs     sp_auth_roles_list, session-cached
  Contracts/Services/IPermissionService.cs  Services/PermissionService.cs      resolution, cache, invalidation
  Contracts/Services/IPermissionAdministrationService.cs
  Services/PermissionAdministrationService.cs    the page-facing read, the change-set write, the reversal
  Contracts/Services/IUserManagementService.cs  Services/UserManagementService.cs
  Services/UserManagementRepository.cs      the user-management procedures, and no statement text
MTM_Waitlist.Settings/
  ViewModels/UserManagementViewModel.cs     the list: search, role filter, remembered filter, states
  ViewModels/CreateUserViewModel.cs         the form only
  ViewModels/EditUserViewModel.cs           fields above, actions below, read-only announced at the top
  ViewModels/PermissionsViewModel.cs        the matrix: one card per area, the people as the rows
  ViewModels/PermissionHoldersViewModel.cs  the who-holds-this view
  Models/PermissionHoldersView.cs           the roles that hold it, and the people who differ
  ViewModels/PinRevealDialogViewModel.cs    the one-time PIN, its print and its deliberate close
  Models/                                   UserSummary, UserDetail, UserEditRequest, PermissionCell,
                                            PermissionColumn, PermissionCardRow, PermissionAreaCard,
                                            PermissionMatrixRow, PermissionChangeSet, UserListFilter,
                                            typed results
MTM_Waitlist.Startup/
  Services/StartupSessionRepository.cs      upper-case sign-in names, the attempt count, the role code
  ViewModels/LoginViewModel.cs              the remaining-attempts message
Module_Settings/Views/                      UserManagementPage, CreateUserPage, EditUserPage,
                                            PermissionsPage, PermissionHoldersView, PinRevealDialog,
                                            SettingsPage (Administration)
Module_Startup/Views/LoginPage.xaml         the remaining-attempts line
ViewModels/ShellViewModel.cs                the badge, keyed on the role code
Services/DependencyInjection/ServiceRegistrationExtensions.cs  registration and four page routes
Strings/en-us/Resources.resw                every new label, title, action and message
MTM_Waitlist.Tests/Module_*/                the new suites, plus the extended audits
```

**Structure Decision**: the app project stays the composition root and keeps every view, while the permission
declaration, the ladder and the user-management rules live in `MTM_Waitlist.Core` so the sign-in path, the Setup
and Settings libraries, the waitlist and the separate service host all read one implementation rather than a copy.
The two new administration pages follow the existing Setup and New Request page-and-view-model shape rather than
being dialogs, because both navigate.

## Constitution Check

| Principle | Verdict | Why |
| --- | --- | --- |
| I. Spec-First, Verified Delivery | PASS | This plan follows a written spec of 117 requirements and a settled owner decision log. No implementation starts before it. Every task is ticked only with a build, a test or a verified live check behind it. |
| II. Live Data Integrity — Internal Stores Are Never Mocked | PASS, with a note | Every account, role, permission and audit read and write goes to `mtm_waitlist` live. There is no demo mode and no sample row anywhere in this feature. The note: a gate must answer from the shipped fallback when the store cannot be reached (FR-050), and the roster must show an unavailable state with a retry (FR-096). Neither substitutes data: the first answers an access question, the second reports the failure. |
| III. Stored-Procedure-First Database Discipline | PASS | Ten new procedures, four changed procedures — the three credential reads and `sp_config_settings_upsert`, whose change is a header comment and nothing else — one function change, one new table and three new seeds, each with `create.sql` and `rollback.sql` in the same change, plus the aggregate regenerations and the bootstrap's `create_database.sql` and `update_table_descriptions.sql`. No statement text enters application code; every write goes through the non-query seam that reports affected rows. |
| IV. MCP-First, Grounded Decisions | PASS | Every platform claim in `research.md` that could not be read from this repository was checked against Microsoft Learn or the Windows Community Toolkit documentation, and the source is named beside it. |
| V. WinUI 3 Platform Conformance | PASS | All XAML uses `Microsoft.UI.Xaml` and the CommunityToolkit settings controls already in use. New pages route through the existing `PageService` and `NavigationService`, every new string is localised, no new converter or resource key is introduced, and no control carries a fixed width. |
| VI. Evidence-Based Verification Gates | PASS | The plan's gates are the documented ones: a clean solution build with zero warnings, the full suite green, the two audit tests extended rather than weakened, an opt-in live-database check for the procedures, the rename's paired reversal actually run, and the forward rename re-applied once that reversal has been proved. |
| VII. Readable, End-User-Facing Delivery | PASS | Applies to every chat reply made while executing this plan. |
| Additional Constraints — Security & Secrets | PASS | The PIN is never stored, never logged and never written to the audit row, which carries no value on either side of a reset. No credential is hardcoded or displayed in full. |
| Additional Constraints — External Integration & Cache Boundaries | PASS | No external system is written to, and no cached external data is involved. Permission values are internal configuration read live. |
| Additional Constraints — Documentation & Extensibility | PASS | `Database/Database-Ruleset.md` gains the role scope and the corrected precedence order, `FEATURES.md` and `CHANGELOG.md` are updated in the same change, and the retired role vocabulary is removed from the affected documents rather than left behind. |

No principle is violated, so there is no Complexity Tracking table.

**Two orderings are load-bearing, and both are gates rather than advice.** The precedence re-rank (T006) ships
before the first per-person permission row is written (T054). The role rename and its paired reversal are both
actually executed (T032, T033) before the permission baselines are seeded (T036), and T033 then re-applies the
forward rename in the same run, because the reversal leaves the retired role in the catalogue: a baseline keyed to a
role that does not exist is a baseline nobody holds, and one keyed to the retired role is a baseline nobody should.
The phases encode both: the re-rank sits in Phase 2's first wave and the first per-person write in Phase 8; the
rename is Phase 4, which ends with the rename standing again, and the baselines Phase 5.

**Re-checked after Phase 1 design.** The design added three things the gate above was written before: the
`role_rank` column on a version-one core table, a new audit table, and one stored procedure that takes a whole
change set as JSON rather than one row at a time. None of the three changes a verdict. The column and the table are
ordinary schema artifacts with their paired rollbacks and their aggregate regenerations, so principle III still
holds. The change-set procedure is still a stored procedure called by name with `CommandType.StoredProcedure`, and
the JSON travels as a parameter rather than as statement text, so principle III holds for it too. The one thing
worth a reader's attention is that the change-set procedure is where the rank rule and the fixed permission row are
enforced, so it is the single place both the save and its reversal are refused, which is what FR-025 and FR-069
ask for.
