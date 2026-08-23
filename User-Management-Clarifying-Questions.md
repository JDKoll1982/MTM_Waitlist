# User Management Panel — Decisions & Spec

> Goal: Add a new **collapsible panel in Settings** that lets **Production-Lead-and-above** roles
> **create new users** and **edit existing users** (including their privileges).
> This document is the **locked requirements/spec** derived from the answered clarifying questions,
> plus an implementation plan and follow-ups.

---

## 1. Confirmed Decisions

### A. Scope & Access Control

- **A1 — Role hierarchy (rank order, low → high):**
  `Production < Material Handler < Setup < Production Lead < Setup Lead < Plant Manager < IT Department < Developer`
  This becomes a **single shared helper** (`RoleAuthorization`) that replaces the hardcoded per-feature arrays.
- **A2 — Who may open the User Management panel:** **Production Lead, Setup Lead, Plant Manager, IT Department, Developer**
  (i.e. rank ≥ Production Lead). Implemented via the shared helper.
- **A3 — Replace `Admin` with `IT Department`:** there is no `Admin` role seeded. We will
  **add `IT Department` to `auth_roles_catalog`** (in `Admin`'s place) and update all code that
  currently references the literal `"Admin"` role.
- **A4 — Single role per user:** no multi-role support. Login keeps reading the one assigned role.

### B. Workflow / UX Behavior

- **B1 — Panel layout:** a **user list + search**, an **"Edit"** action per row, and a **"New User"** button.
- **B2 — New-user fields:**
  - **User Name** (username)
  - **First Name** + **Last Name** (combined into `display_name`)
  - **Employee ID** (4-digit number)
  - **Initial role** — must be **≤ the creating user's role** (e.g. a Production Lead cannot assign Developer or IT Department).
  - Initial password = temp **`0000`** + `require_password_change = 1` (matches existing login flow).
- **B3 — Editing scope (all in scope):**
  (a) role/privilege change, (b) display name / employee id, (c) activate/deactivate (`is_active`),
  (d) reset password, (e) all of the above.
- **B4 — Self-lockout guard:** a user **cannot deactivate their own account or change their own role.**
- **B5 — Audit trail:** yes — record who created/edited users (reuse `assigned_by_user_id` + `assigned_utc` where applicable).

### C. UI Details

- **C1 — Placement:** a **new "Administration" category** in Settings (not inside Operations).
- **C2 — Icon / header:** to be chosen by implementer (see §2).
- **C3 — Create/Edit interaction:** **separate Pages** for Create and Edit (not inline, not a `ContentDialog`).
- **C4 — List display:** show **role badges**, **active/inactive status**, and support **sort/filter by role**.
- **B6 — Search criteria:** the user list search matches on **username, display name, employee ID, and role** (see F5).
- **B7 — Employee ID:** 4-digit number, **NOT unique** — multiple logins may share an employee ID
  (e.g. same person as `Developer` and as `Production` for lockout testing) (see F6).

### D. Database Logic

- **D1 — Target DB:** MySQL `mtm_waitlist` (same as login). The Infor Visual DB is **strictly read-only** — no writes/sync there.
- **D2 — Transactions:** user-profile insert **and** role assignment in the **same transaction**.
- **D3 — Stored procedures:** introduce new SPs under `Database/StoredProcedures/` (repo convention) for create/update/list/get/reset.
- **D4 — Password reset:** reuse the `UpdatePasswordAsync` path; add an optional **admin reset-to-`0000`** action
  (sets `require_password_change = 1`) for when the user is not present to set their own password.
- **D5 — Username normalization:** change to **UPPERCASE** to match Infor Visual
  (currently the login normalizes to **lowercase**). **This is in scope now** as part of this feature
  (see F1).

### E. Codebase / Operations Changes

- **E1 — Services:** add `IUserManagementRepository` (data) + `UserManagementService` (role checks/logic),
  DI-registered in `ServiceRegistrationExtensions.cs`.
- **E2 — Shared role helper:** add `RoleAuthorization` and refactor existing hardcoded `Allowed*Roles` arrays to use it.
- **E3 — Tests:** add a `Module_Settings` test suite for the new VM + repository.
- **E4 — Migration:** `Database/Tables/...` + seed updates + regenerate `AllTables.sql` / `AllSeeds.sql`.
- **E5 — Mock-data toggle:** user-management CRUD honors the mock short-circuit pattern
  (`Feature.RecvMockData` / Infor mock toggle) for dev, but can hit the real DB.

---

## 2. Proposed UI (to confirm)

- **Category header:** "Administration"
- **Expander header text:** "User Management"
- **Expander icon:** `People` (or `Contact`) — `Symbol="People"`
- **Page names:** `UserListPage` (embedded in the Settings expander), plus
  `CreateUserPage` and `EditUserPage` (navigated pages), OR a single `UserEditorPage` with a mode flag.
  - **Decision needed:** separate `CreateUserPage` + `EditUserPage`, or one shared editor page.

---

## 3. Implementation Plan

### Phase 1 — Database (Database Engineer)

1. Add `IT Department` role to `auth_roles_catalog` seed (`Database/Seeds/.../create.sql` + `AllSeeds.sql`).
2. **New audit table** for field-level user-management changes — e.g. `auth_user_management_audit`
   (confirmed: F4) capturing display name / employee id / active / role / password-reset changes + acting user + timestamp.
3. New stored procedures under `Database/StoredProcedures/`:
   - `sp_user_management_list` (optional filter by role / search, incl. role + active status)
   - `sp_user_management_get` (by user id)
   - `sp_user_management_create` (insert profile + role assignment in one transaction; enforce role-rank rule)
   - `sp_user_management_update` (display name / employee id / role / active; enforce rank + self-lockout rules)
   - `sp_user_management_reset_password` (set to `0000`, `require_password_change = 1`)
   - `sp_auth_roles_list` (available roles for the picker)
4. Regenerate `AllTables.sql` / `AllSeeds.sql` / `AllSPs.sql`.

### Phase 2 — Shared role helper (Backend Engineer)

1. Add `RoleAuthorization` (rank table + `IsAtLeast(role, required)` + list of roles ≥ given role).
2. Refactor existing `Allowed*Roles` arrays (`SettingsViewModel`, `SetupWorkstationViewModel`, `DunnageWorkflowService`) to use it.
3. Replace literal `"Admin"` references with the `IT Department` role.

### Phase 3 — Data layer (Backend Engineer)

1. Add `IUserManagementRepository` + `UserManagementRepository` (calls the new SPs).
2. Add `IUserManagementService` + `UserManagementService` (role checks, validation, audit).
3. DI-register in `ServiceRegistrationExtensions.cs`.
4. Wire mock-data short-circuit (E5).

### Phase 4 — ViewModel + Views (Frontend Engineer)

1. `SettingsViewModel`: add `Administration` category visibility, `CanManageUsers` (rank ≥ Production Lead), user list/search, sort/filter by role.
2. `SettingsPage.xaml`: add the Administration category + `User Management` expander (list, search, New User, Edit rows).
3. New `CreateUserPage` / `EditUserPage` (+ VMs), registered for navigation in DI/NavigationService.
4. Role badges, active/inactive indicators.

### Phase 5 — Tests & Validation

1. `Module_Settings` tests: VM (list/search/sort/filter, role-rank guard, self-lockout guard, create/edit flows) + repository (SP-backed).
2. Unit tests for `RoleAuthorization`.
3. Build + run Module_Setup / Module_Settings test suites.

---

## 4. Resolved Follow-ups (Final Answers)

- **F1 — Username case change:** do it **now** as part of this feature. Changes `CheckCredentialsAsync`,
  `ReadSessionSnapshotAsync`, the `username_normalized` unique key, and the login/registration code
  from **lowercase → uppercase**; includes a data migration of existing lowercase rows.
- **F2 — Rank rule on edit:** **yes**, the same "cannot set a role higher than my own" rule applies when
  re-assigning an existing user's role; and **yes**, a user may be promoted to their **own** rank.
- **F3 — Deactivate + active sessions:** deactivating (`is_active = 0`) does **NOT** revoke existing
  `auth_sessions_tokens`. Only login gating changes going forward.
- **F4 — Audit scope:** add a **new dedicated audit table** for field-level changes
  (display name, employee id, active, password reset) in addition to `auth_roles_assignments.assigned_by_user_id` for role changes.
- **F5 — Search/filter:** search matches on **username, display name, employee ID, AND role**; list is
  sortable/filterable by role.
- **F6 — Employee ID uniqueness:** employee ID is **NOT unique** — multiple logins may share the same
  4-digit employee ID (e.g. one person as `Developer` and as `Production` for lockout testing).

---

## 5. Fully Locked Requirements (summary)

- Panel **Administration → User Management** in Settings, visible to **rank ≥ Production Lead**
  (Production Lead, Setup Lead, Plant Manager, IT Department, Developer).
- Role rank: `Production < Material Handler < Setup < Production Lead < Setup Lead < Plant Manager < IT Department < Developer`.
- `IT Department` replaces `Admin`; add to `auth_roles_catalog` seed + update all `"Admin"` literals.
- One role per user; create/edit via **separate pages**; list with search (username/display/employee id/role),
  role badges, active status, sort/filter by role.
- Create: username, first+last name (→ display), 4-digit employee id (not unique), initial role ≤ creator's role,
  temp `0000` + `require_password_change=1`.
- Edit: role (≤ editor's role, own-rank allowed), display name, employee id, active, reset password.
- Self-lockout guard: cannot deactivate self or change own role.
- Deactivation does **not** revoke existing sessions.
- DB: MySQL `mtm_waitlist`, Infor Visual read-only, transactional create, new SPs, new audit table.
- Username normalization switches to **UPPERCASE** (with migration) in this feature.
- New `IUserManagementRepository` + `IUserManagementService` + shared `RoleAuthorization` helper; DI-registered;
  refactor existing hardcoded role arrays; honor mock-data toggle; add tests + migrations.
