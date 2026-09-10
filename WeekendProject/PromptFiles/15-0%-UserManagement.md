# 15 - User Management — Administration Panel (Settings)

> **Source:** the locked requirements from `Documents/Development/UserManagement/User-Management-Clarifying-Questions.md`
> plus the stress-test findings from `User-Management-Edge-Case-Report.md`, both folded into this file on
> 2026-09-09 (the two loose documents were then deleted).
> **Not part of the Waitlist master task list** — this is a standalone Settings workstream with no master task numbers.
> **Workflow:** checklist-execution skill; adopt each task's persona. Tick `- [x]` only when implemented, builds clean, and tests pass.
> **Task 0 scope:** green build + full suite pass before/after this file.

## Subphase 0 — Task 0: Green build & full-suite baseline

- [ ] **DevOps: `MTM_Waitlist.sln` builds clean (0 errors, 0 warnings)** via `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false`. | **Persona: DevOps Engineer**
- [ ] **QA: full `MTM_Waitlist.Tests` suite passes** (`dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64`). | **Persona: QA Engineer**
**GATE: 0 errors + 0 warnings + full green suite before/after this file.**

## Locked requirements (do not re-litigate)

Outcome: a collapsible **Settings → Administration → User Management** panel letting **rank >= Production Lead**
create and edit users, including privileges.

- **Role hierarchy (rank order, low to high):**
  `Production < Material Handler < Setup < Production Lead < Setup Lead < Plant Manager < IT Department < Developer`
  This becomes a **single shared helper** (`RoleAuthorization`) that replaces the hardcoded per-feature role arrays.
- **Who may open the panel:** Production Lead, Setup Lead, Plant Manager, IT Department, Developer.
- **`Admin` is replaced by `IT Department`:** no `Admin` role is seeded. Add `IT Department` to
  `auth_roles_catalog` in its place and update every literal `"Admin"` reference.
- **One role per user** — no multi-role support; login keeps reading the single assigned role.
- **Panel layout:** user list + search, a per-row **Edit** action, and a **New User** button.
- **Create fields:** User Name; First Name + Last Name (combined into `display_name`); 4-digit Employee ID;
  initial role **<= the creating user's role**; initial password temp `0000` with `require_password_change = 1`.
- **Edit scope (all in scope):** role/privilege, display name / employee id, activate/deactivate (`is_active`),
  reset password.
- **Self-lockout guard:** a user **cannot deactivate their own account or change their own role**.
- **Rank rule on edit:** the same "cannot set a role higher than my own" rule applies when re-assigning an
  existing user's role; promoting to the editor's **own** rank is allowed.
- **Audit trail:** record who created/edited users. Add a **new dedicated field-level audit table**
  (display name, employee id, active, password reset) in addition to
  `auth_roles_assignments.assigned_by_user_id` for role changes.
- **Deactivation does NOT revoke existing `auth_sessions_tokens`** — only login gating changes going forward.
- **Search/filter:** search matches **username, display name, employee ID, and role**; list is sortable and
  filterable by role, showing role badges and active/inactive status.
- **Employee ID is NOT unique** — multiple logins may share the same 4-digit ID (e.g. one person as
  `Developer` and as `Production` for lockout testing).
- **Username normalization switches to UPPERCASE** (to match Infor Visual) **as part of this feature** —
  currently the login normalizes to lowercase. Includes a data migration of existing lowercase rows.
- **Database:** MySQL `mtm_waitlist` (same as login). Infor Visual is **strictly read-only** — never write or sync to it.
- **Transactions:** user-profile insert **and** role assignment in the **same transaction**.
- **UI placement:** a **new "Administration" category** in Settings (not inside Operations).
- **Create/Edit interaction:** **separate Pages** (not inline, not a `ContentDialog`).
- **Mock-data toggle:** user-management CRUD honors the mock short-circuit pattern
  (`Feature.RecvMockData` / Infor mock toggle) for dev, but can hit the real DB.

## Subphase 1.1 — Database

- [ ] **Database: add `IT Department` to the `auth_roles_catalog` seed** (in `Admin`'s place) and regenerate `AllSeeds.sql`. **[Artifacts: `Database/Seeds/<NN_name>/create.sql`]** | **Persona: Database Engineer**
- [ ] **Database: add a new field-level user-management audit table** — e.g. `auth_user_management_audit` — capturing display name / employee id / active / role / password-reset changes plus acting user and timestamp. Follow `database-schema-rules.instructions.md` (`create.sql` + `rollback.sql`, aggregate regeneration). | **Persona: Database Engineer**
- [ ] **Database: add stored procedures under `Database/StoredProcedures/`** — `sp_user_management_list` (optional role filter / search, incl. role + active status), `sp_user_management_get`, `sp_user_management_create` (profile + role assignment in one transaction; enforce the role-rank rule), `sp_user_management_update` (display name / employee id / role / active; enforce rank + self-lockout), `sp_user_management_reset_password` (set `0000`, `require_password_change = 1`), and `sp_auth_roles_list` (roles for the picker). | **Persona: Database Engineer**
- [ ] **Database: switch `username_normalized` to UPPERCASE** and migrate existing lowercase rows. **[Affects: the unique key, login credential reads, and session snapshot reads]** | **Persona: Database Engineer**
- [ ] **Database: regenerate `AllTables.sql` / `AllSeeds.sql` / `AllSPs.sql`** and keep `update_table_descriptions.sql` in sync. | **Persona: Database Engineer**
- [ ] **QA: validate the new schema + SPs against a live `mtm_waitlist` database** (create/update/reset paths, rank rule, self-lockout rejection). | **Persona: QA Engineer**

**GATE: schema + SPs validated against a live database before any code depends on them.**

## Subphase 1.2 — Shared role helper

- [ ] **Backend: add `RoleAuthorization`** — the rank table plus `IsAtLeast(role, required)` and a helper returning all roles >= a given rank. | **Persona: Backend Engineer**
- [ ] **Backend: refactor the existing hardcoded `Allowed*Roles` arrays** (`SettingsViewModel`, `SetupWorkstationViewModel`, `DunnageWorkflowService`) to use `RoleAuthorization`. | **Persona: Backend Engineer**
- [ ] **Backend: replace literal `"Admin"` references with the `IT Department` role.** | **Persona: Backend Engineer**
- [ ] **Testing: unit tests for `RoleAuthorization`** (rank ordering, `IsAtLeast` boundaries, roles-at-or-above list). | **Persona: QA Engineer**

## Subphase 1.3 — Data layer

- [ ] **Backend: add `IUserManagementRepository` + implementation** calling the new stored procedures (no inline SQL). | **Persona: Backend Engineer**
- [ ] **Backend: add `IUserManagementService` + implementation** for role checks, validation, and audit writes; DI-register in `ServiceRegistrationExtensions.cs`. | **Persona: Backend Engineer**
- [ ] **Backend: wire the mock-data short-circuit** so CRUD can run against sample data when the mock toggle is ON. | **Persona: Backend Engineer**
- [ ] **Testing: repository tests** — transactional create rolls back both rows on mid-way failure; duplicate `username_normalized` returns a typed "already exists" result; deactivation does not clear `auth_sessions_tokens`; one audit row per create/edit/reset. | **Persona: QA Engineer**

## Subphase 1.4 — ViewModel + Views

- [ ] **Workflow/VM: add the Administration category + `CanManageUsers` gate (rank >= Production Lead), user list, search, and sort/filter by role** to `SettingsViewModel`. | **Persona: Full Stack Engineer**
- [ ] **Frontend: add the Administration category and the `User Management` expander to `SettingsPage.xaml`** (list, search, New User, per-row Edit) following the existing `muxc:Expander` + `x:Load` pattern. | **Persona: Frontend Engineer**
- [ ] **Frontend: add the `CreateUserPage` and `EditUserPage` (+ view models)**, registered for navigation via DI / `PageService`. | **Persona: Frontend Engineer**
- [ ] **Frontend: render role badges and active/inactive indicators** with `TextTrimming="CharacterEllipsis"` + tooltip and `Min`/`MaxWidth` (never fixed width). | **Persona: Frontend Engineer**
- [ ] **Localization: add `.resw` keys** for the category/expander headers, field labels, and all action/error text (no literal UI text). | **Persona: Frontend Engineer**
- [ ] **Testing: `Module_Settings` view-model tests** — list/search/sort/filter, role-rank guard, self-lockout guard, create/edit flows. | **Persona: QA Engineer**

## Subphase 1.5 — Critical mitigations (must implement)

> These are the must-fix findings from the WinUI 3 stress test. They exist because the current `SettingsViewModel`
> contains patterns that are high-risk to copy.

- [ ] **Do NOT copy the fire-and-forget init pattern** (`_ = InitializeHotWorkCentersAsync();`) into the new VM. Wrap init in `try/catch`, surface a status/busy state, log via `StartupDebugLog`, and use `async Task` — never `async void`. *(Severity: Critical)* | **Persona: Backend Engineer**
- [ ] **Do NOT use sync-over-async on the UI thread.** Read settings/roles with `await` in `OnNavigatedTo`/init — never `.Result` or `.GetAwaiter().GetResult()`. *(Severity: Critical)* | **Persona: Backend Engineer**
- [ ] **Keep `ObservableCollection` mutation strictly on the UI thread.** Ensure continuations resume on the UI context; if any `ConfigureAwait(false)` is introduced, marshal with `DispatcherQueue.TryEnqueue`. *(Severity: Critical — otherwise `RPC_E_WRONG_THREAD` crash)* | **Persona: Full Stack Engineer**
- [ ] **Do NOT wrap the transactional create in blind retry.** `ExecuteWithRetryAsync` retries on `MySqlException`/`TimeoutException`; a commit-then-drop would re-run the insert. Rely on the unique key and return a typed "username already exists" on duplicate-key (1062), or make the SP idempotent. *(Severity: Critical — duplicate users / lost audit)* | **Persona: Backend Engineer**
- [ ] **Guard the Save command against reentrancy/double-click** (`CanExecute` + `IsSaving`, disable Save while saving) and surface duplicate-key as guidance, not a raw error. *(Severity: High)* | **Persona: Frontend Engineer**
- [ ] **Avoid hardcoded widths in the create/edit forms** — use `Auto`/`*` columns, wrap forms in a `ScrollViewer`, and add `MaxWidth`; verify at 150%+ scaling. *(Severity: High)* | **Persona: Frontend Engineer**
- [ ] **Use a virtualizing list for the user grid** (`ListView`/`GridView` with bounded `MaxHeight` inside a `ScrollViewer`) so large catalogs stay reachable and performant. *(Severity: High)* | **Persona: Frontend Engineer**

## Subphase 1.6 — Input validation contract

| Field | Type | Required | Rule |
| --- | --- | --- | --- |
| `Username` | string | Yes | 1–128; normalized to **UPPERCASE**; trim; reject whitespace-only and control chars |
| `FirstName` / `LastName` | string | Yes | 1–128 each; combined into `display_name` (<= 256); collapse duplicate spaces |
| `EmployeeId` | string | Yes | Exactly 4 digits (`\d{4}`); **not unique** |
| `Role` | ComboBox | Yes | Must be **<= the current user's rank** (own rank allowed) |
| `Active` | bool | No | **Cannot deactivate self** |
| `ResetPassword` | action | Optional | Set temp `0000` + `require_password_change = 1`, behind a confirmation |
| `SearchQuery` | string | No | Matches username / display name / employee id / **role**; case-insensitive |

Actor rank MUST be re-validated in the **SP/service**, not only in the UI.

## Subphase 1.7 — Required test cases

- [ ] RoleAuthorization: a Production Lead cannot create/assign Developer or IT Department; own rank allowed. | **Persona: QA Engineer**
- [ ] Self-lockout: edit where target == actor rejects deactivation and role change. | **Persona: QA Engineer**
- [ ] Transactional create rolls back both rows on forced mid-way failure. | **Persona: QA Engineer**
- [ ] Duplicate username returns a typed "already exists" result (no crash, no duplicate row). | **Persona: QA Engineer**
- [ ] Uppercase normalization: entering `jsmith` stores `JSMITH`; login matches either case. | **Persona: QA Engineer**
- [ ] Employee ID non-uniqueness: two users may share `6229` and both appear in the list. | **Persona: QA Engineer**
- [ ] Search/filter matches username, display name, employee id, and role. | **Persona: QA Engineer**
- [ ] UI-thread collection safety: awaited SP load then collection add does not throw cross-threading. | **Persona: QA Engineer**
- [ ] Deactivate without session revocation: `is_active = 0` keeps tokens but the next login is rejected. | **Persona: QA Engineer**
- [ ] Double-click Save creates exactly one row. | **Persona: QA Engineer**
- [ ] Mock-toggle parity: CRUD short-circuits with the toggle ON and hits real SPs with it OFF. | **Persona: QA Engineer**
- [ ] Audit trail: every create/edit/reset writes a row with acting user + timestamp. | **Persona: QA Engineer**

**GATE: all Subphase 1.7 cases green, build 0 errors / 0 warnings, full suite green.**
Next task: **Subphase 1.1 IT Department seed** | **Persona: Database Engineer**
