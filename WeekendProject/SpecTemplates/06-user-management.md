# Spec seed 06 — User management (Settings → Administration)

| Field | Value |
| --- | --- |
| **Suggested feature name** | `user-management` |
| **Source** | `WeekendProject/OPEN-WORK-NEXT-SPEC.md` §8 — from `PromptFiles/15-0%-UserManagement.md` (**39 carry-forward** after the obsolete and cancelled boxes were removed) |
| **Carry-forward boxes** | **39** |
| **Depends on** | template 01 (it will edit the same Settings page and view model). Independent of the card and analytics workstreams. |
| **Defect files it closes** | none directly — but it is the natural moment to fix any Settings-page defect still open (see §5) |
| **Validation** | the source's twelve required test cases (its §8.8) minus the obsolete mock-toggle parity case; build clean; suite `Failed: 0` |

---

## 1. Why this spec exists

There is no way to manage a user without touching the database, and the role model the whole
application gates on has no shared definition — three different files hold three hand-written role
lists, and one of them uses `Admin` while the catalog is meant to hold `IT Department`. This workstream
is self-contained (its own schema, its own screens, its own tests), it carries a **locked requirements
section** that has already been through review, and it is the prerequisite for a trustworthy role model
everywhere else.

## 2. Input to paste into `/speckit.specify`

```text
Add user management to MTM Waitlist under a new Administration category in Settings, and give the
application a single definition of role ranking. Today a user can only be created or changed by
editing the database directly, and each screen that gates on a role carries its own hand-written list
of allowed roles, so the application has several definitions of who is senior to whom. This feature
adds a user list with search and filtering by role, a create flow and an edit flow as separate pages
rather than dialogs, and per-user fields for username, first name, last name, employee identifier,
role and active status, with password reset behind a confirmation that sets the temporary default
password and forces a change at next sign-in. Editing is governed by rank: a person may not assign a
role above their own rank, may not deactivate themselves, and may not escalate anyone past themselves,
and that rule is enforced in the database transaction as well as on the screen, because the screen
cannot be trusted. Creating a user writes the profile and the role assignment in one transaction, so a
half-created user can never exist, and a duplicate user name returns a clear, typed result rather than
retrying. Every create, edit and password reset writes an audit record naming the actor, the field
that changed and the time. User names are normalised to upper case so that signing in matches either
case, and the existing lower-case rows are migrated. Employee identifiers are deliberately not unique,
because two people can share one, and the search matches user name, display name, employee identifier
and role. Deactivating a user does not revoke their existing session tokens. The feature introduces a
roles catalog that replaces the retired administrator role with an IT Department role, adds a shared
rank helper with a comparison operation and a list of roles at or above a rank, and refactors the
existing allowed-role lists onto it so there is exactly one definition of the hierarchy. The new
screens follow the application's existing settings pattern, render role badges and active indicators
without fixed widths, virtualise the list, guard the save action against a double click, never block
the interface thread, and localise every string.
```

## 3. Carried-forward requirements — the source's **locked requirements** (do not re-litigate)

Rank hierarchy; `Admin` → `IT Department`; one role per user; create/edit fields; self-lockout guard;
the rank rule; an audit table; deactivation does not revoke tokens; search matches user name / display
name / employee ID / role; **Employee ID is NOT unique**; username normalisation → UPPERCASE plus a
data migration; same-transaction profile + role; a new **Administration** Settings category; and
**separate Pages, not dialogs**.

**Database (1.1)** — add `IT Department` to the `auth_roles_catalog` seed in `Admin`'s place and
regenerate `AllSeeds.sql` (`Database/Seeds/<NN_name>/create.sql`); add a **field-level**
user-management audit table (for example `auth_user_management_audit`) capturing display name /
employee id / active / role / password-reset changes plus acting user and timestamp, per
`database-schema-rules.instructions.md`; add the stored procedures under `Database/StoredProcedures/`
— `sp_user_management_list` (optional role filter / search, including role and active status),
`sp_user_management_get`, `sp_user_management_create` (profile + role **in one transaction**, enforcing
the rank rule), `sp_user_management_update` (display name / employee id / role / active, enforcing rank
and self-lockout), `sp_user_management_reset_password` (set `0000`, `require_password_change = 1`), and
`sp_auth_roles_list`; switch `username_normalized` to **UPPERCASE** and migrate existing lowercase rows
*(this affects the unique key, login credential reads and session snapshot reads)*; regenerate
`AllTables.sql` / `AllSeeds.sql` / `AllSPs.sql` and keep `update_table_descriptions.sql` in sync; QA
against a live `mtm_waitlist`.

**Shared role helper (1.2)** — add `RoleAuthorization` — the rank table plus `IsAtLeast(role,
required)` and a helper returning all roles at or above a rank; refactor the existing hard-coded
`Allowed*Roles` arrays (`SettingsViewModel`, `SetupWorkstationViewModel`, `DunnageWorkflowService`)
onto it; replace literal `"Admin"` references with the `IT Department` role; unit-test rank ordering,
`IsAtLeast` boundaries and the roles-at-or-above list.

**Data layer (1.3)** — `IUserManagementRepository` + implementation calling the new procedures (**no
inline SQL**); `IUserManagementService` + implementation for role checks, validation and audit writes,
DI-registered in `ServiceRegistrationExtensions.cs`; repository tests — a transactional create rolls
back **both** rows on a mid-way failure, a duplicate `username_normalized` returns a typed "already
exists" result, deactivation does not clear `auth_sessions_tokens`, and one audit row is written per
create/edit/reset.

**UI (1.4)** — add the **Administration** category and a `CanManageUsers` gate (rank ≥ Production
Lead), the user list, and search plus sort/filter by role to `SettingsViewModel`; add the category and
the **User Management** expander to `SettingsPage.xaml` following the existing `muxc:Expander` +
`x:Load` pattern; add `CreateUserPage` and `EditUserPage` (+ view models) registered for navigation;
render role badges and active/inactive indicators with `TextTrimming="CharacterEllipsis"` + tooltip and
`Min`/`MaxWidth`, **never** a fixed width; localise category/expander headers, field labels and all
action/error text; `Module_Settings` view-model tests for list/search/sort/filter, the role-rank guard,
the self-lockout guard, and the create/edit flows.

**Critical mitigations (1.5) — must implement** — do **not** copy the fire-and-forget init pattern
(`_ = Initialize…Async();`): use `async Task`, never `async void`; do **not** use sync-over-async on
the UI thread (no `.Result` / `.GetAwaiter().GetResult()`); keep `ObservableCollection` mutation on
the UI thread and marshal with `DispatcherQueue.TryEnqueue` (otherwise `RPC_E_WRONG_THREAD`); do **not**
wrap the transactional create in blind retry — return a typed "username already exists" on duplicate
key (1062) or make the procedure idempotent; guard the Save command against reentrancy/double-click
(`CanExecute` + `IsSaving`); avoid hard-coded widths in the create/edit forms and verify at 150 %+
scaling; use a virtualising list for the user grid.

**Input validation contract (1.6)** — `Username` 1–128 UPPERCASE; `FirstName`/`LastName` 1–128 →
`display_name` ≤ 256; `EmployeeId` exactly `\d{4}` and **not unique**; `Role` ≤ the current user's rank;
`Active` cannot deactivate self; `ResetPassword` behind a confirmation; `SearchQuery` matches user
name / display name / employee id / role. **Actor rank must be re-validated in the procedure/service,
not only in the UI.**

**Required test cases (1.7)** — rank refusal; self-lockout rejection; transactional rollback;
duplicate-username typed result; uppercase normalisation (`jsmith` → `JSMITH`, login matches either
case); employee-id non-uniqueness (`6229` shared by two users); search matching all four fields;
UI-thread collection safety; deactivate without session revocation; double-click Save creating exactly
one row; an audit row with acting user and timestamp. *(The source's twelfth case, "mock-toggle
parity", is obsolete — see `OPEN-WORK-NEXT-SPEC.md` §3.)*

## 4. Explicitly out of scope — do not resurrect

- **The mock-toggle requirement** in the source's locked list, and its parity test case — obsolete.
- **The request-type/subtype editor** and any other cancelled catalog-administration UI.
- **Analytics on users** — template 04.
- **A second role per user, or role hierarchies deeper than the locked table** — the locked
  requirements say one role per user; do not extend the model opportunistically.
- **Reworking the sign-in, session or token model** beyond the uppercase normalisation the locked
  requirements demand.

## 5. Defects this spec closes

None. But two things must be checked while this spec edits `SettingsPage.xaml` and
`SettingsViewModel.cs` by hand:

- If template 01 has **not** yet run, do not re-introduce its defects: no new label may share an
  existing resource key, and any new search-aware panel must be registered with the search-refresh
  path (`defects/Medium-Settings-AboutLabelsCollideOnOneResourceKey.md`,
  `defects/Low-Settings-SearchIgnoresThreeSettingsPanels.md`).
- The new Administration panels must obey template 01's rule: no control may be shown for a role that
  cannot use it.

## 6. Verification / gates

1. Build clean (`0 Warning(s) 0 Error(s)`); full suite `Failed: 0`.
2. `MTM_Waitlist.Tests` covers the source's §1.7 list (eleven live cases).
3. Live-database validation of the new schema and procedures (create / update / reset, the rank rule,
   the self-lockout rejection) — the source asks for this explicitly.
4. Regenerate `AllTables.sql` / `AllSeeds.sql` / `AllSPs.sql` and update
   `update_table_descriptions.sql` in the same change; `InlineSqlAuditTests` and
   `RetiredSymbolAuditTests` stay green.
5. UI pass at 150 %+ scaling, with the list virtualised and no fixed widths.
6. Beware the documented build traps: `WMC9999` masks a real XAML error, `x:Name` is required on any
   element using `x:Load`, and a shell that has launched the app must have its `MTM_*` variables
   cleared before running the suite.

## 7. Open decisions to resolve before `/speckit.specify`

1. **Who may manage users** — the source says rank ≥ Production Lead; confirm against the intended
   IT Department role (which would otherwise be unable to manage users unless it is ranked high).
2. **Rank values** for each role — the locked requirements name a hierarchy but not the numbers;
   fix them before the helper is written.
3. **`0000` as the reset value** — confirm it stays the temporary default, including its interaction
   with the sign-in change-password flow (`specs/001` T164) and the seeded `test.*` accounts.
4. **Where the audit table lives** relative to the existing request audit table, and whether the two
   should share a shape.
5. **Whether `IT Department` also replaces `Admin` in the seeded accounts** and in the service's
   approved-operator role list (`MTM_Waitlist.Mock.Service/Api/ServiceOperatorRoles.cs`).
