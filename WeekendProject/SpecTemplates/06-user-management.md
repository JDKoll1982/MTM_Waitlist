# Spec seed 06 — User management (Settings → Administration)

| Field | Value |
| --- | --- |
| **Suggested feature name** | `user-management-and-permissions` |
| **Source** | `WeekendProject/OPEN-WORK-NEXT-SPEC.md` §8 — from `PromptFiles/15-0%-UserManagement.md` (**39 carry-forward** after the obsolete and cancelled boxes were removed) |
| **Carry-forward boxes** | **39**, plus the **permission matrix** added by owner decision 2026-09-20 (§3.8) |
| **Depends on** | template 01 (it will edit the same Settings page and view model). Independent of the card workstream. **Runs BEFORE templates 04, 05, 07 and 08** — owner decision 2026-09-20, because every one of them gates on a role, and after this feature a role gate is a settings entry rather than a hand-written list. See `00-INDEX.md` and `04-waitlist-analytics.md` §8 decisions 7 and 8. |
| **Defect files it closes** | none directly — but it is the natural moment to fix any Settings-page defect still open (see §5) |
| **Validation** | the source's twelve required test cases (its §8.8) minus the obsolete mock-toggle parity case; build clean; suite `Failed: 0` |

---

## 1. Why this spec exists

There is no way to manage a user without touching the database, and the role model the whole
application gates on has no shared definition. The count was taken on 2026-09-20 rather than guessed, and
was **corrected the same day by searching the shipping code rather than reading a list**:
**twelve hand-written role lists across nine files**, and they already disagree with each other and with the
catalog — one uses `Admin` where the catalog is meant to hold `IT Department`, and another names
`Setup Tech`, which no role holds, so that entry can never match a live person. This workstream
is self-contained (its own schema, its own screens, its own tests), it carries a **locked requirements
section** that has already been through review, and it is the prerequisite for a trustworthy role model
everywhere else.

The second reason is the same problem seen from the other end, and it was found on 2026-09-20 while
specifying the analytics feature. **Twelve role lists live in nine files**, they already disagree with each
other and with the catalogue, and no single thing defines them. One of them names `Setup Tech`, which no
role holds, so that entry can never match a live person and the setup worker it appears to intend is refused
by the very list that names them. Adding a job title means editing up to twelve places, and a missed one fails
**silently** — the role is simply an outsider there, with no error raised anywhere. So this feature also
builds the piece those twelve lists should have been all along: **one named permission per gated action,
resolved from settings**, with a page where **Developer, IT Department and Plant Manager** decide what each
role may do. That is an
owner decision of 2026-09-20 and it deliberately re-opens this template's locked §1.2 once — see §3 there,
and `04-waitlist-analytics.md` §8 decisions 7 and 8.

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

The same feature also gives the application one named permission per gated action, in place of the ten
hand-written role lists it carries today. Each gated action is named by a settings key, and a role either
holds that key or does not: if the current user's key is false, they cannot perform the gated action. A new
Administration page in Settings, openable only by Plant Manager and above, shows the whole matrix of roles
against permissions as a grid of switches, and saving a change writes it to the settings store together with
an audit record naming the acting user, the permission, the role, the previous value, the new value and the
time. Every gate the application has today becomes an entry in that matrix — accepting requests, ignored
locations, hot work centres, part pictures, cache refresh, allotted minutes, Quick Add dunnage, work-centre
setup, and the badge beside the signed-in name — and the shipped default for each one reproduces exactly
what it allows today, so nobody's access changes on the day it ships. A permission row that is missing, or a
settings store that cannot be reached, yields the shipped default rather than a refusal, following the order
the image-storage configuration already uses: take the stored override when there is one, otherwise the
default. The set of permissions is declared in one place so the page can be drawn from it and so a newly
added gate cannot be introduced without a default, and a role with no row for a permission falls back to
that permission's default. The permission that opens and edits this page is itself not editable from the
page, so an administrator cannot remove their own way back in.
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
the rank rule), `sp_user_management_update` (display name / employee id / role / active / **sign-in name** —
decision 20 — enforcing rank and self-lockout), `sp_user_management_reset_password` (set a **random 4-digit
PIN** and `require_password_change = 1`, per decision 9 — the fixed `0000` is gone), and
`sp_auth_roles_list`; switch `username_normalized` to **UPPERCASE** and migrate existing lowercase rows
*(this affects the unique key, login credential reads and session snapshot reads)*; regenerate
`AllTables.sql` / `AllSeeds.sql` / `AllSPs.sql` and keep `update_table_descriptions.sql` in sync; QA
against a live `mtm_waitlist`.

**Shared role helper (1.2)** — add `RoleAuthorization` — the rank table plus `IsAtLeast(role,
required)` and a helper returning all roles at or above a rank; refactor the existing hard-coded
`Allowed*Roles` arrays (`SettingsViewModel`, `SetupWorkstationViewModel`, `DunnageWorkflowService`)
onto it; replace literal `"Admin"` references with the `IT Department` role; unit-test rank ordering,
`IsAtLeast` boundaries and the roles-at-or-above list.

> **AMENDED BY OWNER DECISION, 2026-09-20 — this locked section is re-opened once, deliberately.** The
> `Allowed*Roles` refactor in the paragraph above is **superseded**: those arrays are not collapsed onto a
> rank helper, they are **replaced by named permission entries resolved from settings** (§3.8).
> `RoleAuthorization` is still built, because this section's own rule needs it — "a person may not assign a
> role above their own rank" is exactly `IsAtLeast` — but the *second* half changes, because a Plant Manager
> must be able to change what each role may do without a code change. Building both halves would mean
> editing the same ten arrays twice.

**Data layer (1.3)** — `IUserManagementRepository` + implementation calling the new procedures (**no
inline SQL**); `IUserManagementService` + implementation for role checks, validation and audit writes,
DI-registered in `ServiceRegistrationExtensions.cs`; repository tests — a transactional create rolls
back **both** rows on a mid-way failure, a duplicate `username_normalized` returns a typed "already
exists" result, deactivation does not clear `auth_sessions_tokens`, and one audit row is written per
create/edit/reset.

**UI (1.4)** — add the **Administration** category to `SettingsPage.xaml` holding **two entries, both of which
navigate to a page**: Users (decision 18) and Permissions (decision 21). Each entry is shown by its own
permission via `x:Load` — `permission.admin.users` and `permission.admin.permissions` — so a reader entitled to
only one sees only that one, and a reader entitled to neither does not see the category at all. **Neither entry
is an expander**: they navigate, so they must look like they navigate (the `wct:SettingsExpander` pattern is for
panels that open in place, not for doorways). The pages are `UserManagementPage` and `PermissionsPage`
in `Module_Settings/Views`, with the user list's search and sort/filter by role on the list page rather than in
`SettingsViewModel`; add `CreateUserPage` and `EditUserPage` (+ view models) registered for navigation;
render role badges and active/inactive indicators with `TextTrimming="CharacterEllipsis"` + tooltip and
`Min`/`MaxWidth`, **never** a fixed width; localise every entry label, page title, field label and all
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

**Permission matrix (3.8) — added by owner decision 2026-09-20; not one of the source's 39 boxes.**

*Database.* Reuse `config_settings_values` (table 08) rather than adding a table: a permission is a
`setting_key`, the role is the `scope_key` with a new `scope_type` for a role scope, and the value is a
boolean. Declare that `scope_type` where the existing values are declared so
`vw_config_settings_scope_catalog` and `Database/Validation/settings_schema/validate.sql` pick it up. Add the
two procedures the page needs that the existing set does not cover — read every cell in one call, and write
one cell — as `sp_config_permissions_matrix_get` and `sp_config_permissions_cell_set`, each shipping
`create.sql` + `rollback.sql` + `update_table_descriptions.sql` in the same change (constitution III). Seed
every permission at every role in the matrix seed so the shipped defaults are **data**, not literals in code,
and regenerate `AllSeeds.sql` / `AllSPs.sql` / `AllTables.sql`. History: one `config_settings_history` row per
cell change (the table already exists) naming actor, permission, role, previous value, new value and time —
do not invent a second audit shape if that one is sufficient.

*The permission registry.* One place declares every permission: its key, a human label for the page, its
shipped default per role, and the screen it gates. The page is drawn from the registry, so a permission
cannot exist in code without appearing on the page, and cannot appear without a default. A test asserts the
registry is non-empty, that every key is unique, and that **no gate reads a key the registry does not
declare** — that test is what replaces the silent-outsider failure the ten arrays produce today.

*Resolution.* A permission resolves to the stored value for the caller's role; when the row is absent, or
the store cannot be reached, the **shipped default** applies. That is the order
`ImageStorageConfigurationResolver` already documents ("1. Database override … 2. default") and it means an
unreachable store does not lock the application out of its own buttons — the opposite of the service API's
fail-closed rule, which governs a network call, not a control. Cache the resolved values for the session with
explicit invalidation on save, and never read settings inside a layout pass on the UI thread.

*The page.* The permission screen is a **page of its own** (`PermissionsPage` in `Module_Settings/Views`),
reached from an Administration entry on `SettingsPage.xaml` (decision 21), openable by **Developer, IT
Department and Plant Manager** (decision 5). **Its shape is a role picker plus that role's permissions as a
list, not a grid** (decision 16): nine roles across a 1080px column leaves about 90px each, while a name like
"Material Handler Lead" needs about 140px before its switch, and the window can be dragged to 760px. Each row
carries the permission's plain-language label and what it gates. The roles that outrank the reader are **shown,
with their switches not changeable** and the reason stated in words (decision 6). Localise every string; no
fixed widths; guard Save against double-click; register its Settings entry with the search-refresh path so it
obeys the search defects recorded in §5. The permission that opens this page is **not** editable from it, so
nobody can remove their own way back in — the same shape as this feature's self-lockout guard for users.

*Migration.* **Twelve lists in nine files are deleted** and each is replaced at its call site by its
permission, with the default set to today's membership so nothing changes on the day it ships:
`RequestActionPolicy.HandlerRoles`; `ServiceOperatorRoles.Approved` in `MTM_Waitlist.Mock.Service` (a
separate project — it must resolve the same key from the same store rather than keep a copy);
`SettingsViewModel`'s four arrays; `UrgencyAllotmentEditorViewModel.AllowedUrgencyManageRoles`;
`DunnageWorkflowService.AllowedQuickAddRoles`; `SetupWorkCenterViewModel.AllowedManageRoles`;
`ShellViewModel.GetUserPresentation`, whose badge map keys on a vocabulary of its own (`supervisor`,
`manager`, `quality`, `quality inspector`) and therefore gives **both** existing leads the plain grey badge
today; and **two sites the earlier count missed** — `DefectTypeCatalogService.AllowedRoles` and
`ComputerManagementViewModel`'s array. The migration is complete only when a test proves that **no gate in the
application reads its own role list**, which is §6 gate 8.

*Carried in from `04-waitlist-analytics.md` §8, because this template now runs first.* The role
**Material Handler Lead** (`material_handler_lead` / "Material Handler Lead") is added to the catalogue there
and needs a default row in every permission; the owner's decision was to add it as a real title rather than
map it onto the Plant Manager. `Production Manager` is the Plant Manager and gets no row of its own. The
`Setup Tech` entry in `SetupWorkCenterViewModel.AllowedManageRoles` matches no role the catalogue holds, so
it must not be carried into work-centre setup's default — decide whether a plain `Setup` person was always
meant to have it and record the answer.

*Required tests.* Registry completeness and key uniqueness; a gate reading an undeclared key fails the
suite; an absent row yields the default; an unreachable store yields the default (a double that throws); a
stored override beats the default for the right role **and only that role**; a cell write produces exactly one
history row naming actor and both values; the panel is openable by Developer, IT Department and Plant Manager and invisible to every other role; the
open-the-page permission cannot be cleared from the page; and the badge map resolves a Material Handler Lead
without falling through to the unknown-role branch.

## 4. Explicitly out of scope — do not resurrect

- **The mock-toggle requirement** in the source's locked list, and its parity test case — obsolete.
- **The request-type/subtype editor** and any other cancelled catalog-administration UI.
- **Analytics on users** — template 04.
- **A second role per user, or role hierarchies deeper than the locked table** — the locked
  requirements say one role per user; do not extend the model opportunistically.
- **Reworking the sign-in, session or token model** beyond the uppercase normalisation the locked
  requirements demand. **One deliberate exception, required by decision 14:** the sign-in path gains a
  five-attempt limit, and it applies **only** to accounts holding a temporary password or PIN, because
  decision 9's 4-digit PIN has no strength without it. Nothing else about the sign-in, session or token model is
  in scope, and ordinary sign-in keeps today's behaviour — including having no attempt limit at all.
- **Self-service password reset.** Password reset belongs to the user management panel, performed by the three
  lead roles, the Plant Manager, IT Department and Developer. It is **not** offered on the sign-in screen, and
  a person cannot reset their own password.

## 5. Defects this spec closes

None. But two things must be checked while this spec edits `SettingsPage.xaml` and
`SettingsViewModel.cs` by hand:

- If template 01 has **not** yet run, do not re-introduce its defects: no new label may share an
  existing resource key, and any new search-aware panel must be registered with the search-refresh
  path (`defects/Closed-Medium-Settings-AboutLabelsCollideOnOneResourceKey.md`,
  `defects/Closed-Low-Settings-SearchIgnoresThreeSettingsPanels.md`).
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
7. **The matrix's defaults reproduce today's behaviour exactly** — provable, not asserted: for every one of
   the **twelve** gates, the seeded default membership must equal the list it replaces, and a test must compare
   the two rather than trusting the seed. Then the replacement of each list is a no-op by construction.
8. **The registry test is the safety net, not the page.** A new gate added in a later feature must fail the
   suite until it is declared with a default, or the silent-outsider failure simply moves.

## 7. Open decisions to resolve before `/speckit.specify`

**None.** Every item the source raised, and everything this brainstorm opened, is settled:

| The item | Settled as |
| --- | --- |
| Who may manage users | decision 1 — IT Department above Plant Manager, Developer above that, and the rest of the ladder in decision 4 |
| Rank values for each role | decision 4 — one rung per lead, gap-spaced so a future role can be inserted |
| `0000` as the reset value | decision 9 — replaced by a random 4-digit PIN shown once to the person doing the reset; decision 14 adds a five-attempt limit on the temporary value |
| Where the audit table lives, and its shape | decision 11 — its own table, one row per changed field |
| Whether `IT Department` replaces `Admin` in the seeded accounts and the service's operator list | decision 7 — yes, and the service list is one of the twelve gate sites |
| The registry's contents and defaults | decision 12 — namespace, keys and shipped defaults fixed |
| What happens to stored permission rows when the role is renamed | decision 7 — verified there are none to carry |
| Which answer wins where a panel already gates itself | decision 13 — the permission, and only the permission |
| Whether a permission change is confirmed or undone | decision 10 — both, with the history table as the durable path |

**The next step is `/speckit.specify`, with §2 as the input.** §3's carried-forward requirements, §3.8's
permission matrix and this log's decisions are the requirement set. The UI was designed deliberately in a
second pass rather than left to the implementer — see the log's session 2 summary.

---

## 8. Brainstorm log

### 2026-09-20 — session 1: the IT role's standing, and the hole it opens

**Target.** The template was brainstormed because decision 8 of `04-waitlist-analytics.md` moved it to the front
of the run order: every other feature gates on a role, and after this one a gate is a settings entry rather
than a hand-written list. Classified **architectural** — new schema, new procedures, two new pages, and a
change to an interface ten call sites depend on. The mode's output folds into this template's §8 rather than
into a separate design document, per `.specify/extensions/superspec/references/superpowers-bridge.md`.

### 2026-09-20 — decision 1: IT Department takes `Admin`'s authority; Developer outranks it

**Decided (owner, 2026-09-20): IT Department sits at the top of the operational hierarchy — it can manage user
accounts and change what every role may do — and the Developer role sits above IT Department.**

**Why this is consistent with the rest of the template.** §6 gate 7 requires the permission matrix's defaults
to reproduce today's behaviour *provably*, and `Admin` is currently in nearly every permissive list. Renaming
the role while carrying its authority across is what makes that gate passable; ranking IT Department low would
have silently removed access from the top role on ship day, which is the exact failure §6 gate 7 exists to
catch.

**Consequences to carry into the requirements.**

- **The permissions page's gate is "IT Department and above"**, which today means IT Department and Developer.
  Whether **Plant Manager** keeps the page is now a question, not an inherited fact — it was written as "Plant
  Manager and above" when Plant Manager was near the top and is now third down. Raised as §7 item 10.
- **The rank rule's upper edge changes shape.** "A person may not assign a role above their own rank" now means
  IT Department can assign everything up to, but not including, Developer. A Developer account cannot be
  created or promoted by IT Department.
- **The page's opening permission is fixed in the seed, not editable in the UI** (already required, to prevent
  self-lockout). Combined with this decision, the set of roles that can open the page is therefore
  **unchangeable without a shipped seed change**. That is the deliberate price of never being able to lock
  yourself out, and it is accepted rather than worked around.
- **The rename is not a find-and-replace.** Every one of the ten lists is **deleted** and replaced by its
  permission key (§3.8), so `Admin` survives only in the catalogue, the seeded accounts and the service's
  operator list — which is what §7 items 5 and 7 have to settle.

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| **An account that outranks the person administering it** | Developer sits above IT Department, and a password reset sets a known temporary value and forces a change at next sign-in — so whoever can reset a Developer's password can sign in as one | **Decision 2**: an account ranking above your own is read-only, so IT Department cannot reset a Developer's password. Enforced in the database transaction, not only on the screen |
| The last Developer account | If a Developer is the only one and is deactivated or lost | Nobody can act on it, by the rule above. State the recovery path rather than leaving it undefined — that is what the workstation-elevation note exists for |
| Two leads and the account floor | Unchanged by this decision | §7 item 10 |
| A person holding both roles | `auth_roles_assignments` allows several live roles, and decision 2 of the analytics template already fixes how one is snapshotted | The higher rank wins for authorisation, and the snapshot rule stays as recorded there — do not invent a second rule here |

### 2026-09-20 — decision 2: an account that ranks above you is read-only

**Decided (owner, 2026-09-20): you may act on people at or below your own level, never above it.** IT Department
can *see* a Developer's account and cannot change it — not its role, not its password, not its active flag.
**The rule is enforced inside the database transaction**, per this template's own standing requirement that the
screen cannot be trusted.

**Why this closes something real.** A password reset sets a value the resetter knows and forces a change at the
next sign-in, so whoever can reset a Developer's password can sign in as that Developer. That was the one route
from IT Department to the top of the hierarchy, and it is now shut.

**The exact rule, because two nearly-identical wordings behave very differently.** Rank **strictly above yours**
is read-only. **Peers are editable** — two Plant Managers may correct each other, which is what the owner chose.
And **your own account is not an account above you**, so a person's own password change still works.

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| **The rule read too literally forbids you from maintaining your own account** | "At or above your own rank" includes *your own* rank, so a careless implementation of this decision would stop a person editing themselves | State it as **strictly above**, and say plainly that a person's own password change is unaffected. This is the exact trap the two rules would otherwise collide on |
| **Peers editing peers** | Two people can hold the same role | Allowed, and stated — otherwise the implementation invents a stricter rule than the owner chose |
| The last Developer account | Nobody ranks above it, so by this rule nobody can act on it | State the recovery path rather than leaving it undefined. It is the workstation-elevation route, not a screen |
| A person holding two roles | Several live assignments are normal | The **higher** rank decides whether they may be edited and what they may edit |
| This is broader than the source's wording | The source's locked §1.6 says "`Role` ≤ the current user's rank", which governs *assigning* a role and says nothing about a password or the active flag | Record it as a deliberate amendment to a **locked** section, in the same way §3 already amends §1.2 — not as a quiet widening of a written requirement |
| Deactivation still does not revoke sessions | A standing locked requirement | Unchanged. But note that deactivating someone no longer stops them if they hold a session, and now **nobody below them can reset that password either** — worth stating together so the combination is not discovered in production |

### 2026-09-20 — decision 3: password reset lives in the user management panel

**Decided (owner, 2026-09-20): password reset stays in the new user management panel, and only Leads, Plant
Manager, IT Department and Developer may perform it.** There is no reset on the sign-in screen, and a person
cannot reset their own password.

**How the rule is expressed is the subtle part, and it is deliberate.**

- **Who may use the reset is a permission entry** in the matrix (§3.8), whose shipped default is granted to the
  four groups named. Expressing it as a permission rather than a rank comparison is what makes the owner's word
  **"Leads"** unambiguous: Setup Lead and Material Handler Lead have no fixed rank yet (§7 item 2), so a rank
  comparison would silently exclude whichever of them sorts below Production Lead. A permission has no such gap.
- **Whose account may be reset is decision 2's rule**: an account ranking strictly above your own is read-only.
  A Lead resets peers and juniors and never seniors; Plant Manager resets leads and below; IT Department and
  Developer reach everyone except a Developer account, which only another Developer may touch.
- The source's `ResetPassword`-behind-a-confirmation requirement is unchanged.

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| **"Leads" is plural and their ranks are not fixed** | §7 item 2 is still open, and Material Handler Lead was added by the analytics template's decision 5 | The default grants the reset permission to **every** lead role — Production Lead, Setup Lead and Material Handler Lead — and a test asserts **each of them by name**, so no future rank change can quietly drop one |
| A Lead and a peer | Decision 2 allows peers to be edited | Stated explicitly, so the implementation does not invent a stricter rule than the owner chose |
| A deactivated account | `is_active = 0` | State whether the reset is allowed, and state that a reset does **not** reactivate the account — the two are separate acts |
| The temporary value is known to the resetter | Inherent to any reset that sets a known default | The window closes at the forced change on next sign-in (§7 item 3). Say so, so the residual risk is written down rather than assumed away |
| The reset permission and the panel's own gate | §3.8 already fixes that the opening permission is not editable from the page | Unchanged, and the reset permission **is** editable — the two must not be confused, or the page's own gate becomes flippable |

### 2026-09-20 — decision 4: the ladder, one rung per lead

**Decided (owner, 2026-09-20): one rung per lead role, in this order, top to bottom.** Verified against the
actual seed rather than a summary: `auth_roles_catalog` holds exactly eight roles — `material_handler`,
`production`, `production_lead`, `setup`, `setup_lead`, `plant_manager`, `developer`, `admin`.

| Rung | Role(s) |
| --- | --- |
| 1 | Developer |
| 2 | IT Department (the renamed `admin`) |
| 3 | Plant Manager |
| 4 | Production Lead |
| 5 | Setup Lead |
| 6 | Material Handler Lead (added by the analytics template's decision 5) |
| 7 | Material Handler, Production, Setup — all three on the same rung |

**The numbers follow the order; they are not a second decision.** Give each rung a tens value and leave gaps
so a future role can be inserted without renumbering: `developer` 100, `it_department` 90, `plant_manager` 80,
`production_lead` 70, `setup_lead` 60, `material_handler_lead` 50, worker roles 10. The gaps are deliberate
and belong in the requirement, because these numbers are read by `IsAtLeast` and by the account rule.

**Consequences to carry into the requirements.**

- **A lead can no longer correct another lead's account unless they rank above them.** Decision 2 makes an
  account ranking strictly above yours read-only, so a Setup Lead cannot edit a Production Lead, and a Material
  Handler Lead cannot edit either of them. Each lead edits their own people and the worker roles.
- **The three worker roles share one rung**, so they are peers for every rank comparison.
- **`admin` stops being a rung.** It becomes IT Department at rung 2 — which is what §3's rename requires, and
  what §7 item 7 has to carry across.
- **Sharing the worker rung decides standing, not access.** Whether a plain worker may manage accounts at all
  is §7 item 10; the shared rung only says they are level with each other.
- **Four names in the badge code are not roles.** `ShellViewModel.GetUserPresentation` branches on
  `supervisor`, `manager`, `quality` and `quality inspector` — the catalogue holds none of them, so those
  branches cannot match a live person. It recognises only `developer`, `admin` and `material handler` of the
  eight real roles, so **five roles get the plain grey badge today, the Plant Manager among them**. Settled as
  decision 15: it is a lookup rather than a gate, and it is fixed as presentation.

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| **The gaps in the numbering** | A future role must be insertable without renumbering the others | The gaps are part of the decision, not an accident. A test asserts each rung's value, so an added role cannot silently land on top of an existing one |
| Two people holding the same lead role | Nothing prevents it | They are peers and may edit each other (decision 2). Stated, so the implementation does not invent a stricter rule |
| A renamed role | `role_code` is the key; the display name is not | The ladder is keyed on `role_code`. `admin` → IT Department is a rename that carries its rung, not a new role on a new rung |
| The ladder read by two rules | The account rule (decision 2) and any rank-based gate such as "rank ≥ Production Lead" | **One** table and **one** helper serve both readers. A second copy is the failure this feature exists to remove |
| A person holding two roles | Several live assignments are normal | The **highest** rung decides, consistent with decision 2 |
| `Production Manager` | The source names it and the analytics template uses it | It is the **Plant Manager** — no catalogue row of its own — so it sits at rung 3 |

### 2026-09-20 — decision 5: the permission screen belongs to three roles, and its gate is a permission

**Decided (owner, 2026-09-20): Developer, IT Department and Plant Manager can all change what roles may do.**

**The gate is a permission entry, not a rank comparison — and that is deliberate.** "Plant Manager and above"
happens to name exactly those three roles on today's ladder, so a rank expression would work *by coincidence*.
It is still the wrong mechanism, because decision 4's ladder is built with gaps precisely so roles can be
inserted: the day a role is added above the Plant Manager, a rank-based gate would silently hand it the
permission screen and nothing would report it. The matrix exists so that a gate is a named entry with an
explicit default.

**Consequences to carry into the requirements.**

- The page's opening permission ships with those three roles as its **default**, and cannot be edited from the
  page itself (§3.8), so nobody can lock themselves out of it.
- **The Plant Manager can grant themselves anything the matrix controls.** That is inherent in giving them the
  page and it is accepted. It is also exactly why the page's own opening permission is not editable: the one
  thing a Plant Manager cannot do is remove their own way back in.
- **The roles above you are read-only here too — settled as decision 6.** A Plant Manager cannot touch a
  Developer's account (decision 2), and the same rule now governs the grid, so they cannot switch off every
  permission the Developer and IT Department roles hold. That is sabotage rather than escalation, and it is
  silent, so it is stated explicitly rather than left to the general rule.

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| A role inserted above the Plant Manager | The ladder has deliberate gaps | The page's gate does **not** move, because it is an entry rather than a rank. The new role's other access comes from its own default row |
| The Plant Manager's own role | They can change their own access | They can grant themselves anything the matrix controls, and cannot remove the page's own opening permission — which is what keeps the change reversible |
| The page is drawn from the registry | §3.8 requires one declaration per permission | A permission cannot exist in code without appearing on the page, and cannot appear without a default. The page is generated, never hand-listed |
| A role with no row for a permission | Defaults are seeded, but a role added later may have none | Falls back to that permission's shipped default (§3.8), never to a refusal |

### 2026-09-20 — decision 6: a role that outranks you is read-only in the grid as well

**Decided (owner, 2026-09-20): you may change the permissions of every role at or below your own rung, and the
roles above you are visible but not changeable.** Decision 2 says the same thing about accounts, so the feature
has **one rule in two places** rather than two rules to remember.

| Who | May change |
| --- | --- |
| Developer | every role, including IT Department and Plant Manager |
| IT Department | every role except Developer |
| Plant Manager | Plant Manager and everything below it — the three Lead rungs and the three worker roles |

**Enforced inside the database transaction, not only on the screen** — the same standing requirement decision 2
carries, for the same reason: the screen cannot be trusted, so a client posting a cell change directly must
still be refused.

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| A dead switch with no explanation | The read-only cells look different from the editable ones | The screen says **why**, in the reader's words — *"you cannot change the Developer role"* — rather than rendering a control that silently does nothing. A greyed control with no reason is the failure this feature exists to remove |
| The Developer row | Only a Developer may change it, and no Developer may be available | Stated. The row stays readable, and when nobody holds the role, nobody can change it — the same shape as decision 2's "the last Developer account" |
| A refused change | Someone tries a row above them | The refusal is reported plainly to the person, and **no history row is written**, because nothing changed — the history records changes, not attempts |
| Two protections on one screen | The page's own opening permission is not editable (§3.8), *and* the rows above you are not editable | Keep them distinct and test both. They look identical on screen but have different causes, so a reader who conflates them will misreport a bug |
| A person holding two roles | Several live assignments are normal | The **highest** rung decides what they may change, consistent with decisions 2 and 4 |
| A role inserted above an existing one later | The ladder has deliberate gaps | Nothing to change: the new role is automatically above the roles it outranks, and automatically untouchable by them |

### 2026-09-20 — finding: the tally was short, and searching found two gates nobody had listed

**The earlier count — "ten role lists in seven files" — is wrong. Searching the shipping code for the literal
`"Admin"` finds twelve gates across nine files.** Two were never on any list:

- **`MTM_Waitlist.Settings/Services/DefectTypeCatalogService.cs`** —
  `private static readonly string[] AllowedRoles = { "admin", "administrator", "developer" };`
- **`MTM_Waitlist.Settings/ViewModels/ComputerManagementViewModel.cs`** — an `"Admin"` entry.

**Two further problems the same search turned up, neither of which any list records.**

1. **All twelve compare role *display names*, not codes.** The arrays hold `"Material Handler"`,
   `"Production Lead"`, `"Admin"` — and `ServiceOperatorRoles` documents it outright: *"Role display names
   (`auth_roles_catalog.role_name`) permitted to call the API."* The spelling is inconsistent — eight sites
   write `"Admin"`, two write `"admin"` — but the comparisons themselves are **case-insensitive**, verified
   in three of them: `RequestActionPolicy` says so in its own comment, `DefectTypeCatalogService`'s tests
   assert that `"Admin"` and `"ADMIN"` both pass, and `ComputerManagementViewModel` compares with
   `OrdinalIgnoreCase`. So the case difference is untidiness rather than a defect. **The display-name
   dependency is the real finding**, because it means a display-name rename ripples through every gate.
2. **`"administrator"` is named in two sites** (`DefectTypeCatalogService` and the badge map) and **is not a
   role the catalogue holds** — so those branches, exactly like `Setup Tech`, can never match a live person.

**Why this matters more than a corrected number.** The feature's claim is that a gate cannot be missed
silently. A hand count that missed two of twelve is that same failure one level up: if the migration works from
a list rather than from a search, the two missing gates keep their own answers and the application carries two
rule sets again. The requirement is therefore a **test that fails when any gate reads its own role list**
(§6 gate 8), not a longer list to maintain by hand.

### 2026-09-20 — decision 7: `Admin` is removed and `IT Department` added in its place

**Decided (owner, 2026-09-20): the `Admin` role is removed from the catalogue, `IT Department` is added as a new
role, and every account that held `Admin` is moved onto it.**

**Two facts that shrink the job, both verified against the store rather than assumed.**

- **There are no stored per-role permission values to carry.** `config_settings_values` holds three rows and
  every one is scoped to `all_users`; none is scoped to a role. The matrix this feature builds is the first
  thing in that table keyed by a role, so it is seeded against the **new** role and there is nothing older to
  migrate.
- **The only stored reference to the role is the account assignment.** `auth_roles_assignments` points at a role
  by its internal id, and the seed's `test.admin` account is the holder today.

**The accepted risk, stated once so it is not rediscovered as a bug.** Removal plus re-addition is two steps
around a single moment: if the change stops between them, every account that held `Admin` is left with **no role
at all**, and the application would present those people as having no access rather than reporting that
something went wrong. That is the silent-failure class this feature exists to remove, so it is bounded rather
than trusted.

**The safeguard is required, not optional.**

- **The removal, the insertion and the repointing run in one transaction.** This is achievable precisely
  because all three are row operations rather than schema changes — MySQL cannot roll back DDL, but it does
  roll back these together, so the half-run state cannot exist.
- **The change ships with its paired rollback** (`Database/Seeds/<NN_name>/rollback.sql`, per the repo's schema
  rules) restoring `Admin` and moving the accounts back. A rollback that has never been run is not a rollback,
  so it is exercised as part of the feature's verification.
- **Verification is a counted comparison, not a glance:** after the migration **no account has no role**, and
  the set of accounts on `IT Department` equals the set that was on `Admin` before.
- **The matrix is seeded after the rename, keyed to the new role.** Seeding its default rows before the rename
  would key them to a role that no longer exists — the same silent loss in a different place.

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| A run interrupted between the two steps | The whole reason the transaction is required | A test forces a failure mid-way and asserts that **nothing** changed — the technique the source already requires for the transactional user create |
| The reverse direction | The rollback exists but may never have been executed | The rollback is run as part of verification, and the same no-roleless-account comparison is made afterwards |
| A deactivated account holding `Admin` | `is_active = 0` does not remove a role | It still moves across. A deactivated person keeps their role; activation is a separate act |
| The service's approved-operator list | `MTM_Waitlist.Mock.Service` is a separate project | It is one of the twelve gate sites, rewritten in the same change, resolving the same key from the same store rather than keeping a copy |
| The two lowercase `"admin"` literals and the `"administrator"` branches | Eight sites write `"Admin"`, two write `"admin"`, and `"administrator"` is not a role at all | All twelve sites carry the new role, and the dead `administrator` branches are **removed** rather than carried forward |
| Nothing else in the store keys on a role | Checked rather than assumed | The only role-scoped store is the matrix, which does not exist yet. If a later feature adds another, the same check applies |

### 2026-09-20 — decision 8: an account can only be created at or below your own level

**Decided (owner, 2026-09-20): the role picker on the create screen offers only roles at or below the creator's
own rung.** The application therefore never offers to create something it will then refuse to maintain.

**This is what closes §7 item 10, and it changes the shape of the rule rather than just the threshold.** There
is no longer a separate seniority floor for account work:

- **Whether you may manage accounts at all** is a **permission entry**, defaulting to the three lead roles, the
  Plant Manager, IT Department and Developer.
- **Whose account you may touch** is decision 2's level rule, which already governs editing and now governs
  creation too.

**Consequences to carry into the requirements.**

- **The create screen and the edit screen finally agree.** Before this, creation was open to any lead for any
  level while editing was bounded by the rung — a person could make an account and then be unable to correct it.
- **Nobody can create a Developer account except a Developer**, and nobody can create an IT Department account
  except IT Department or Developer. The role picker is bounded by the rung above you.
- **Creating at your own rung is allowed**, because peers may already edit each other (decision 2). One rule,
  not a special case.
- **Enforced in the procedure, not only on the screen**, like decisions 2 and 6 — the screen cannot be trusted.

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| The picker silently omits the higher roles | A list that is quietly shorter than the catalogue invites "where is my role?" | The screen states the bound once, in the reader's words. It does not list the unavailable roles as dead entries, and it does not leave the bound unexplained either |
| No Developer exists to create the first Developer | Only a Developer may create one | State the recovery path — the same workstation-elevation route as decision 2's last-Developer case, not a screen |
| A role inserted into the ladder later | The ladder has deliberate gaps | The picker follows the rung automatically, with no change to this screen |
| The permission and the rung disagree | Someone holds the account-management permission but no rung above a worker — not possible on today's ladder | The two are read together: the permission decides **whether**, the rung decides **whose**. Never one instead of the other |

### 2026-09-20 — decision 9: the reset issues a random 4-digit PIN, shown once to the person doing the reset

**Decided (owner, 2026-09-20): a reset sets a random 4-digit PIN, displayed to the person who performed the
reset so they can write it down and hand it to the person whose password was reset.** The fixed `0000` default
is gone.

**What is required for this to be honest and safe.**

- **Shown once, at the moment of the reset, and never retrievable.** It is not stored anywhere a screen can read
  it back, because a temporary credential that can be re-read is a permanent one.
- **Never written to the audit trail or to a log** (decision 11 requires the reset row to record the *action*
  with no credential on either side of it).
- **`require_password_change = 1` still arms the change panel**, so the PIN is a one-time entry credential and
  not a password: the person signs in with it and is taken straight to setting a real password
  (`specs/001` T164 already opens on that panel before the sign-in form appears).
- **A second reset replaces the PIN**, so the first stops working. The screen says so, so nobody hands over a
  PIN that has already been superseded.
- **`'0000'` becomes a legacy value.** `sp_auth_password_reset_required_get` computes its answer from
  `require_password_change = 1` **or** a temporary-marker hash of `''` / `'0000'`. Accounts still holding
  `'0000'` must keep working until their owner changes it, so the marker branch is **kept** — it simply stops
  being produced. Deleting it would strand every account seeded or reset before this ships.

**The one consequence that needs a decision of its own, and it is asked below.** Four digits is ten thousand
possibilities, and a search of the whole application finds **no attempt limit, lockout or failed-attempt count
anywhere** — the sign-in path has never had one. So the strength of a 4-digit PIN rests entirely on how short
the window is, not on anything that stops guessing.

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| The person doing the reset loses the PIN | It is paper | They reset again and a new PIN replaces it. The screen says the old one stops working, so a lost PIN cannot become two live credentials |
| Two people reset the same account in a row | Nothing prevents it | The **later** reset wins, because the stored value is replaced. Stated, so the first helper does not assume their PIN still works |
| The PIN is read aloud or left on screen | It is displayed to be written down | The dialog is dismissed deliberately, is not a notification, and the value is not retained in the view model after it closes |
| An account already on `0000` | Pre-existing rows, and the seeded `test.*` accounts | Keep working until changed. The legacy marker branch stays |
| A PIN that is never used | The person never signs in | The account stays on the temporary value until the next reset. Bounded by the answer to the question below |
| The PIN appearing in a support conversation | It is a shared secret by design | The screen states plainly that it is for one person and should not be written where others can read it — a short line, not a lecture |

### 2026-09-20 — decision 10: a permission change is confirmed before it is written and can be undone after

**Decided (owner, 2026-09-20): both — a confirmation step and an undo.**

**The confirmation is about the sentence, not the click.** A dialog that says *"Save changes?"* prevents
nothing. The confirmation states **what is changing and for whom, in the reader's words** — *"Production Lead
will no longer be able to change hot work centres"* — because that is the sentence someone can disagree with.
One sentence per changed cell, and the count when there are several.

**The undo needs no new table.** `config_settings_history` already stores `previous_setting_value_*` alongside
`changed_setting_value_*` for every cell, one row per change, with `changed_by_user_id` and `changed_utc`. An
undo is therefore a read of that history and a write of the previous value back — which is also why the
history must be written in the same transaction as the cell (§3.8), or an undo could restore a value that was
never replaced.

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| The cell has moved since the change being undone | Someone else changed the same cell in between | The undo must **not** clobber silently. It says the value has changed since, shows what it is now, and asks before restoring |
| Undoing a change you are not entitled to make | The level rule (decision 6) governs the change, so it governs the reversal | The undo obeys the same rule as the change it reverses. A role that could not have made the change cannot undo it either |
| Nobody is looking when the undo window passes | An in-page undo is naturally short-lived | The page's own undo covers the change just made; the durable path is the history, where someone entitled can reverse any recorded change later. Both paths exist, and the requirement says which is which |
| A change made through a client that skipped the dialog | The confirmation is on the screen, and the screen cannot be trusted | The confirmation is a screen affordance; **the write is still validated in the transaction** (§3.8, decision 6). Confirmation is for the human, never for the rule |
| Many cells changed in one save | A save can touch several cells | One confirmation listing them, and an undo that reverses the whole save rather than leaving half of it applied |

### 2026-09-20 — decision 11: the user audit is its own table, one row per changed field

**Decided (owner invited this to be worked out; answered here): a separate table, `auth_user_management_audit`,
shaped as a field-change log — and it does not share a shape with the request audit.**

**Why separate, grounded in the two tables that already exist rather than in preference.** The store already
holds two different audit shapes, and they differ *because* they record different things:

| Existing table | Its shape | What it records |
| --- | --- | --- |
| `19_waitlist_requests_audit` | `request_public_id`, `from_status`, `to_status`, `event_type`, `actor_employee_number`, `actor_employee_name`, `details`, `occurred_utc` | **Events** — what happened to a request |
| `09_config_settings_history` | `setting_key`, `scope_type`, `scope_key`, `previous_setting_value_*`, `changed_setting_value_*`, `value_type`, `changed_by_user_id`, `changed_utc` | **Value changes** — a cell before and after |

A user edit is neither: it is a set of **field changes on a person**. It has no `from_status`, and its subject
is a user rather than a request or a setting key. Merging it into either would mean columns that mean nothing
for that record type and a table whose rows cannot be read without knowing which kind each one is — which is
the opposite of what an audit table is for.

| Decision | Value |
| --- | --- |
| Table | `auth_user_management_audit`, folder `Database/Tables/32_auth_user_management_audit/` (31 is the highest in use) |
| Granularity | **One row per changed field**, matching `config_settings_history`'s proven per-cell pairing |
| Pairing | `previous_value` → `changed_value`, both nullable, plus `field_name` |
| Grouping | A **change-group id** shared by the rows of one save, so one action can be read as one action |
| Actor | `changed_by_user_id` **plus** the actor's display name, employee number and `role_code` as they were |
| Password reset | Records that it happened, with **both value columns null** — the PIN is never stored (decision 9) |

**Why the actor is snapshotted as well as joined.** The analytics template's decision 2 already settled the
principle for this application: a later role change must not silently re-attribute someone's history, so the
actor's role is captured at write time rather than joined live. The same argument applies here, and the name
and employee number are captured with it so a rename or a deleted profile cannot erase who did something.

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| One save changes four fields | The granularity is per field | Four rows, one change group. A reader sees both the individual field changes and the single action that caused them |
| A reset has no value to show | Storing the PIN is forbidden | A row with the action, the target user, the actor and the time, and **no value on either side**. The absence is deliberate and the schema must allow it |
| The actor's own profile is later edited | Names change; `public_id` does not | The snapshot keeps the name as it was. The FK keeps the durable link |
| A change made by a person who has since been deactivated | `is_active = 0` does not erase a row | The audit still reads correctly, which is the whole point of snapshotting |
| The audit itself needs a rollback | Constitution III requires paired artifacts for every schema change | `create.sql` + `rollback.sql`, the `update_table_descriptions.sql` update, and the `AllTables.sql` regeneration ship together |
| Nobody may read it from the app | There is no audit screen in this feature | The table is written by the procedures and read by support; that is stated so its absence from the UI is not mistaken for an oversight |

### 2026-09-20 — decision 12: the permission registry — namespace, keys and shipped defaults

**Decided (owner invited this to be worked out; answered here).**

**The namespace is `permission.*`.** The existing keys are dot-namespaced (`sessions.retention_inactive_days`,
`waitlist.resolved_retention_days`, `settings.history_retention_days`) and permissions live in the same table,
so they need a prefix that cannot collide with an ordinary setting and that makes the registry enumerable by a
single query. That prefix is also what makes the registry test in §3.8 possible.

**All twelve memberships were read out of the shipping code, not transcribed from a summary** — the earlier
transcription was already found to be short by two gates.

| Permission key | What it gates | Shipped default |
| --- | --- | --- |
| `permission.requests.handle` | accept / complete / release a request | **all eight roles** |
| `permission.cache.refresh_api` | call the cache-refresh API (the service project) | IT Department, Developer, Plant Manager, Setup Lead, Production Lead |
| `permission.settings.ignored_locations` | ignored locations in inventory lists | every role **except Material Handler** |
| `permission.settings.hot_work_centers` | hot work centres | IT Department, Developer, Plant Manager, Setup Lead, Production Lead |
| `permission.settings.part_pictures` | part pictures | IT Department, Developer |
| `permission.settings.cache_refresh` | pulling a cache refresh from the Settings screen | IT Department, Developer, Plant Manager |
| `permission.settings.urgency_minutes` | allowed minutes per item | IT Department, Developer, Plant Manager |
| `permission.settings.defect_types` | the defect type catalogue | IT Department, Developer |
| `permission.settings.computers` | the computer registry | IT Department, Developer |
| `permission.setup.dunnage_quick_add` | Quick Add dunnage definitions | IT Department, Developer, Plant Manager, Setup Lead, Production Lead |
| `permission.setup.work_centers` | work-centre setup | IT Department, Developer, Plant Manager, Setup Lead, Production Lead |
| `permission.admin.users` | create / edit / deactivate a user | Production Lead, Setup Lead, Material Handler Lead, Plant Manager, IT Department, Developer |
| `permission.admin.reset_password` | reset a password | the same set as `permission.admin.users` today |
| `permission.admin.permissions` | edit the permission matrix — **not editable from the page** (decision 5) | IT Department, Developer, Plant Manager |

**Three findings from reading the twelve sites that change the design.**

1. **Every gate compares role *display names*, not codes.** The arrays hold `"Material Handler"`,
   `"Production Lead"`, `"Admin"` — and `ServiceOperatorRoles` says so outright: *"Role display names
   (`auth_roles_catalog.role_name`) permitted to call the API."* So the rename in decision 7 ripples through
   all twelve sites **because the strings they compare are the display names**. Moving to permission keys
   removes that dependency permanently, which is a second reason for the matrix beyond the one already recorded.
2. **`SetupWorkCenterViewModel` really does name a role that does not exist.** The array is
   `{ "Setup Tech", "Admin", "Developer", "Plant Manager", "Setup Lead", "Production Lead" }` —
   `"Setup Tech"` is simply the first entry, and the catalogue's role is `setup` / "Setup". It is **dropped**
   from the default: the six roles it actually admits are the five real ones, so keeping the dead string would
   carry a lie forward. Whether a plain Setup worker was ever meant to have work-centre setup is the owner's to
   decide on the screen.
3. **The Settings cache-refresh panel is deliberately a strict subset of the service's permission.** The code
   says so: the panel's trio is a subset of `ServiceOperatorRoles.Approved` *"so a caller this panel admits can
   never be refused by the service for its role"*. That becomes a rule the matrix must keep: **whatever
   `permission.settings.cache_refresh` grants must be a subset of what `permission.cache.refresh_api` grants**,
   or an operator is shown a control that cannot work. It is asserted by a test, because the two are in
   different projects and nothing else connects them.

**The rule for a role the matrix has no opinion about — `Material Handler Lead`.** Its shipped default is the
**most restrictive sensible one**: the worker level, plus only the grants the owner explicitly approved when the
role was created (`permission.requests.handle` and `permission.cache.refresh_api`). Nothing else. A brand-new
role must not arrive with powers nobody granted it, and because nobody holds the role on ship day the defaults
change nothing for anybody — which is what §6 gate 7 requires. The one place this looks odd — the new Lead is
*not* on `permission.settings.ignored_locations`, where even a plain Production worker is — is flagged for the
owner to flip on the screen rather than decided here.

**One list is not a permission, and does not become one.** The **badge** beside the signed-in name is
presentation, not access: `GetUserPresentation` turns a role string into a glyph and a colour for the signed-in
person's own name plate and is used nowhere else. Its map recognises only `developer`, `admin` and
`material handler`; its other branches name `administrator`, `supervisor`, `manager`, `quality` and
`quality inspector`, **none of which the catalogue holds**, so five real roles — Production, Production Lead,
Setup, Setup Lead and the Plant Manager — fall through to the grey default. **Settled as decision 15, by the
owner:** it is kept out of the matrix and fixed by rewriting the lookup against the real role codes. *(This
reconciles the analytics template, whose decision 7 had listed the badge among the entries the matrix would
take over.)*

### 2026-09-20 — decision 13: the matrix is the single answer, and one read serves both the panel and its action

**Decided (owner invited this to be worked out; answered here).**

- **The local list is deleted, not consulted.** Where a panel gates itself today, its own array is removed and
  the permission is the only answer. Two answers is the failure this feature exists to end.
- **One read per permission serves both the panel's visibility and the action it performs.** The
  `ComputerManagementViewModel` pattern is already the right shape: `CanManageComputers` and
  `IsComputersPanelVisible` both derive from one property. Deriving them separately is how a panel ends up
  visible with a dead control in it.
- **The only exception is the page's own opening permission**, which is not editable from the page (decision 5)
  and is fixed in the seed. That is not a second rule; it is the one place where reading the matrix to decide
  whether you may edit the matrix would be circular.
- **There is no conflict with the panels that edit settings**, because permissions live in their own
  `permission.*` namespace and no settings panel writes one. The "two answers" risk is therefore only the
  deleted lists.
- **An unreachable store yields the shipped default**, never a refusal (§3.8, following
  `ImageStorageConfigurationResolver`'s documented order).
- **Every gate is enforced in the procedure that performs the action**, not only where the screen was drawn —
  the same standing requirement decisions 2 and 6 carry.

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| A panel whose list is deleted but whose action still checks something | The list is the thing being removed, and a stale check can survive it | §6 gate 8's registry test is the safety net: a gate reading a role list fails the suite |
| Visibility and the action disagree | They were derived separately | One read, two consumers. A test asserts that a role refused the action is also refused the panel |
| The permission resolves but the API refuses | The cache-refresh subset rule above | The subset constraint is asserted, so the panel cannot offer a control the service will reject |
| A permission that no gate reads | Someone seeds a key after the gate is removed | The registry test's other direction: a declared permission with no gate is reported, so the registry cannot silently grow dead entries |

### 2026-09-20 — decision 14: five wrong tries, and the temporary value stops being accepted

**Decided (owner, 2026-09-20): after five failed sign-in attempts on an account holding a temporary password or
PIN, that temporary value stops being accepted, and someone entitled issues a fresh reset.** The count is not
limited by time, so waiting does not restore it.

**Where the count lives, and why it is not in memory.** It is stored against the account, because an in-memory
counter would reset the moment the application is closed and reopened — which is the first thing anyone
persistent would try. A column on `core_users_profiles` alongside `require_password_change`, so the credential
state and its guard sit together; the schema change ships with `create.sql` + `rollback.sql`, the
`update_table_descriptions.sql` update and the `AllTables.sql` regeneration (constitution III).

**What clears it: a successful sign-in, or a fresh reset — and nothing else.** A time-based expiry would
defeat the purpose entirely, so it is explicitly excluded rather than left to be assumed.

**The scope is what makes this safe to add.** The limit applies **only** to accounts holding a temporary value.
Ordinary sign-in is untouched, so this feature does not introduce any new way for a working user to lock
themselves out — which matters, because a lockout added to normal sign-in would be a far larger change than the
one decision 9 needs.

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| The person mistypes five times | Four digits on paper, typed on a floor terminal | The account is not locked — a fresh reset by someone entitled clears the count and issues a new PIN. The screen says that, so the person knows the way out |
| Showing the remaining attempts | Without a warning the fifth failure is a surprise | The remaining count is shown **once at least one attempt has failed**, in the reader's words. Telling a guesser their balance reveals nothing useful to them |
| The username does not exist | Ordinary failed sign-in | The behaviour stays exactly as it is today. The limit must not become a way to discover which usernames exist |
| Someone waits before trying again | The obvious way around a simple counter | The count does not expire with time. Only a successful sign-in or a fresh reset clears it |
| Closing and reopening the application | The cheapest way around an in-memory counter | The count is in the store, so it survives |
| An account already holding the legacy `0000` | Pre-existing rows, and the seeded `test.*` accounts | The limit applies to them too — the legacy marker is a temporary value like any other |
| A record of who tried | A burst of failures is a security signal | **Out of scope:** the counter is a guard, not an audit trail. If failed attempts should be recorded, that is a separate feature — stated so its absence is not read as an oversight |

### 2026-09-20 — decision 15: the badge is presentation, and stays out of the matrix

**Decided (owner, 2026-09-20): the badge does not become a permission. It is fixed by rewriting the lookup
against the eight real role codes, so every role has its own badge and nothing about it is gated.**

**What the code does, read rather than recalled.** `ShellViewModel.GetUserPresentation` lowercases the role and
returns a glyph and a colour, and its only caller sets the signed-in person's own name plate. It recognises
**three** of the eight real roles — `developer`, `admin`, `material handler`. Its other branches name
`administrator`, `supervisor`, `manager`, `quality` and `quality inspector`, **none of which the catalogue
holds**, so they can never match. Five real roles therefore get the fallback grey — Production, Production
Lead, Setup, Setup Lead **and the Plant Manager**.

**Why this does not become a permission, although the rule said every hand-written list does.** That rule
exists so adding a job title cannot silently leave someone an outsider somewhere. A switch does not serve that
purpose: it is still a list someone can forget to extend, except the omission becomes invisible because it
lives in data rather than in code. The defect is not that the list is hand-written — it is that the list is
**keyed on words that are not roles and misses five of the real ones**, and that is fixed by keying it on
`role_code` and covering all eight.

**The two costs that would have come with making it a permission.** Every other matrix row decides what a
person may *do*; this one would decide what a person *looks like*. And switching it off produces a person with
no badge, which reads as a fault rather than a decision, and the person affected cannot tell what happened —
the "silent outsider" shape this feature exists to remove.

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| A role with no badge entry | The bug today | The lookup is keyed on the real `role_code` values and covers **all eight**, asserted by a test, so no role can quietly fall to the grey default again |
| The fallback itself | It must exist for an unknown or blank role | Kept for genuinely unknown roles only, and a test asserts that **no role the catalogue holds** reaches it |
| `material_handler_lead` | Added by this feature | Gets its own badge. A new role appearing as the grey default is the same bug one role later |
| The `administrator`, `supervisor`, `manager`, `quality` branches | They name nothing | **Removed**, not left in place, so dead vocabulary cannot be mistaken for a supported one |
| A manager wanting to switch a badge off | It cannot be done, because it is not a permission | Stated, so nobody hunts for the switch and concludes it is missing |

### 2026-09-20 — decision 16: the permission screen is one role at a time

**Decided (owner, 2026-09-20): a role picker, then that role's permissions as a list — not a grid of roles
against permissions.**

**The arithmetic is what settled it, and the owner asked for the grid before the numbers were known.** The
Settings page is one centred column capped at 1080px, and its own comment states that sections are stacked and
never arranged as a row grid. Nine roles across that column, sharing it with the permission names down the
left, leaves roughly **90px per role** — while a name like "Material Handler Lead" needs about 140px before its
switch, and the window's measured restore size is **760px wide**. 126 switches also cannot be read as a whole
at any width.

**What the screen is now.** A role picker, and the chosen role's permissions grouped by area, each row carrying
the permission's plain-language label, what it gates, and a switch. It answers the question people arrive with —
*"what may this role do?"* — and it fits the page's existing shape at any window width.

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| The roles that outrank the reader | Decision 6 makes them visible but not changeable | They appear in the picker, and their switches are shown **locked with the reason in words** — never hidden, or the reader concludes the role does not exist |
| The page's own opening permission | Decision 5 fixes it in the seed | Shown in the list, locked, with the reason. It is the one row nobody can change, including a Developer |
| **A value that comes from the shipped default rather than a stored row** | §3.8 resolves an absent row to the default | The row says **which** it is — set here, or using the default. A switch that is on for either reason hides exactly the fact a manager needs when something is not behaving as expected |
| Unsaved changes | The switches are pending until Save (decision 10) | The page shows how many cells are pending, marks them, and **warns before leaving with unsaved changes** |
| Saving with nothing changed | An accidental press | Save is disabled, and no history row is written — the history records changes, not presses |
| Finding a permission in a long list | Fourteen permissions is enough to hunt through | Grouped by area, and the page still registers with the Settings search-refresh path, so searching "hot work centres" reaches it |
| A role with no stored row at all | Defaults are seeded, but a role added later may have none | Every row still renders from the default. No row can be missing, because the page is drawn from the registry |

### 2026-09-20 — decision 17: the PIN is revealed in a window that must be closed, and can be printed

**Decided (owner, 2026-09-20): a modal window shown after the reset is confirmed, with the ability to print.**

**What the window carries.** The PIN shown large, the person's name and sign-in name, one line saying it is
shown **only this once** and should be handed over in person, a **Print** button, and a single button that closes
it. It is not a notification, it does not persist in a notification centre, and **once closed the PIN is gone** —
if it is lost, the reset is simply performed again and issues a new one.

**Printing follows the repo's existing rule** — the operating-system print path, no direct PDF engine — and
produces a handover slip naming the person, the sign-in name, the PIN, when it was issued and by whom, and the
instruction to change it at first sign-in.

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| A stray click or Escape loses the PIN | "Shown once" is unforgiving | The window cannot be dismissed by clicking outside it or by Escape. Closing it is a deliberate act |
| The print is cancelled or fails | The OS print path can fail | **Printing never closes the window.** A failed print must not cost the credential — the same rule 04's decision 13 applies to its own print |
| The paper is a credential | The PIN is on it | Stated plainly: the slip is handed over, not filed. This is the risk the owner accepted by choosing a written-down PIN |
| A shared printer | The slip passes through a print queue | Accepted and recorded rather than hidden, alongside the existing rule that printing goes through the OS path |
| The resetter is not the person shown | It is handed over in person | The window names **who** it is for, so the slip cannot be handed to the wrong person silently |
| Printing the same PIN twice | Nothing stops two presses | Idempotent — the same one-time value, and the window stays open until closed |

### 2026-09-20 — decision 18: the user list is a page of its own, reached from a Settings entry

**Decided (owner, 2026-09-20): the user list gets its own page (`UserManagementPage`), and Settings keeps an
Administration entry that opens it.** This changes a line of §3's carried-forward UI requirement, deliberately:
the source placed the list on the Settings screen.

**Why the page wins.** The list needs a search box, a role filter, a role badge and an active indicator per row,
row actions, and room to virtualise; the Settings page is a single 1080px column and the list would also be
confined inside a collapsible section. More decisively, the list **navigates away** to a page to create or edit
someone and comes back, which makes it a doorway to a workflow rather than a settings section — the shape Setup
and the waitlist already use.

**Consequences to carry into the requirements.**

- Settings keeps an **Administration** entry, so discoverability is unchanged: there is still one place people
  look.
- The entry's visibility is the `permission.admin.users` permission, exactly as the panel's would have been.
- The journey is **list page → create or edit page → back to the list**, which the locked "pages, not dialogs"
  requirement makes the natural shape.
- `SettingsViewModel` keeps the category and entry; the list's own search, sort and filter move to the list page's
  view model, which is a change to §3's artifact list and is recorded here rather than left implicit.

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| A person hitting Back from the list | It is now a page in the shell's back stack | Back returns to Settings, where they came from — not out of the application |
| The list's search and filter after a trip to edit someone | The page is rebuilt on return | Raised as the next question; the answer is recorded as decision 19 |
| The reader's role changes while the page is open | Roles can change mid-session | The page re-evaluates the permission rather than staying open on a stale answer, consistent with the standing rule for role gates |
| Nothing to show | A search that matches nobody, or an empty store | An explicit empty state saying which of the two it is — not a blank region that looks broken |

### 2026-09-20 — decision 19 (SUPERSEDED BY DECISION 25): the list never remembers a filter

~~**Decided (owner, 2026-09-20): every time the list is shown after a create or an edit, its search and role
filter are clear, so it always shows everyone.**~~ **Superseded the same day by decision 25**, which saves the
filter per person and re-applies it on return. The reasoning below is kept only as the record of the trap it
closed — decision 25 has to handle that trap the other way.

**Why this beats a message.** The alternative closed the same trap by telling the reader that the person they
had just saved was hidden by their own filter. Clearing removes the trap by construction: there is no state in
which a saved person is absent from the list, so nothing needs explaining and no reader has to trust an
explanation. One rule, no exceptions, and it is the rule that cannot be got wrong.

**The cost, stated once.** Adding several people to the same team means setting the same filter again for each
one. A return **without** saving also clears it — the price of a rule with no cases.

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| A return via Back with nothing saved | The rule has no exceptions | The filter clears. Recorded as the deliberate cost rather than left to be discovered |
| Where the list sits after clearing | It is a fresh view | Back to the top, in the page's own sort order, so two visits in the same circumstances look the same |
| Clearing leaves nothing to show | The store may be empty | The empty state distinguishes **nobody exists** from **nobody matches**, even though the second should not be reachable after a clear |
| A reader who expected their filter back | Filters are remembered elsewhere in the app | The page says the filter is present-tense rather than remembered, so the behaviour is stated instead of inferred |

### 2026-09-20 — decision 20: the sign-in name can be corrected on the edit page

**Decided (owner, 2026-09-20): an entitled person can correct the sign-in name on the edit page.** This
deliberately **widens a locked requirement** — the source's editable fields are display name, employee id, role
and active — and is recorded as an amendment in the same way §3 already amends §1.2, rather than as a quiet
addition.

**Why it is worth amending a locked list.** Without it, a sign-in name typed wrongly at creation can never be
corrected: the only route is to deactivate the account and create another, which leaves a dead account carrying
a living person's employee number — inside the feature whose whole purpose is to stop people fixing identities by
editing the database.

**Consequences to carry into the requirements.**

- `sp_user_management_update` takes the sign-in name as well as the four existing fields.
- The **UPPERCASE normalisation** applies on update exactly as on create, so a corrected name behaves like an
  original one.
- A name already taken returns the **same typed "already exists" result** the create path returns — not an
  exception and not a silent retry.
- The **audit records the old value and the new one**, like every other field change (decision 11).
- The level rule and the self-lockout guard both still apply.

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| Changing **your own** sign-in name | The session you are using was established under the old one, and the session snapshot carries it | **Refused.** It is the same shape as the self-lockout guard: you may change anyone's name at or below your level, except the one you are signed in as. Stated, because it is the non-obvious case |
| A corrected name collides with an existing account | Sign-in names are unique | The typed duplicate result, presented in the reader's words, with both names shown so the collision is obvious |
| The old name is already in someone's notes | Names are copied into logs and conversations | The record of truth is the account id, and the audit holds the rename. Stated so nobody hunts for a way to rewrite history |
| A rename of someone above your rung | The level rule (decision 2) | Refused, with the reason in words — identically to every other field on that page |

### 2026-09-20 — decision 21: both administration screens are pages, reached from two Settings entries

**Decided (owner, 2026-09-20): the Administration category holds two entries and both open pages — the user list
and the permissions screen.** So the permission screen becomes a page rather than the panel §3.8 originally
described.

**Why this is the tidier of the two shapes.** It removes the asymmetry that the panel shape created: with one
entry expanding and the other navigating, two rows in the same list behaved differently for no reason the reader
could see. Both navigating means one expectation, and each screen gets the whole content frame instead of being
confined to a capped column inside a collapsible section.

**Consequences to carry into the requirements.**

- **Two pages**, `UserManagementPage` and `PermissionsPage`, both registered for navigation.
- **Two Settings entries**, each shown by its own permission, so entitlement is decided per entry and not by the
  category. A reader entitled to neither does not see the category.
- **Neither entry is an expander.** They navigate, so they must look like it. The `wct:SettingsExpander` pattern
  the source named is for panels that open in place, and using it here would tell the reader the wrong thing.
- **Back returns to Settings** from either page, not out of the application and not back into the same page.
- Both entries must be reachable from the **Settings search**, which is the defect class recorded in §5 — a
  category that search does not know about is a category people cannot find.

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| Entitled to one entry but not the other | The two permissions are separate | Only the entitled entry appears. The category is still shown, because one entry is real |
| Entitled to neither | Every role except the three | The category is absent entirely — not an empty category, and not a dead row |
| The same page opened twice in a row | Nothing prevents it | Back returns to Settings once, rather than walking back through repeats of the same page |
| A reader whose entitlement changes while a page is open | Roles can change mid-session | The page re-evaluates rather than staying open on a stale answer, as decision 18 already requires for the list |
| Two similar names in one list | "Users" and "Permissions" are easy to confuse at a glance | Each entry carries a short description of what it opens, in the reader's words, rather than relying on the label alone |

### 2026-09-20 — decision 22: a row has one job, and it is to open the person

**Decided (owner, 2026-09-20): the only thing a row in the user list does is open that person. Reset, deactivate
and reactivate live on the person's own page.**

**Why one action per row is right here rather than merely simpler.** A row carries five facts — sign-in name,
name, employee number, role and active — and the window can be dragged to 760px, so the three-action row we
considered does not fit at the width the application actually runs at. And a reset is no longer a click: it hands
somebody a credential, so it belongs on a screen that names the person it is for.

**What makes the extra click acceptable.** Deactivation is reversible, and deactivating yourself is already
refused, so routing the actions through the person's page costs one click and removes no protection.

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| The row as the target | With one action, a button per row is clutter | The **whole row** opens the person, and it stays a single keyboard-reachable target that announces what it opens |
| A person who outranks the reader | Decision 2 makes them read-only | The row still opens. A read-only page must be reachable, or the reader cannot check what they are looking at |
| Coming back from the person's page | Decision 19 clears the filter | Judge from §7's model: every row leads out and the list returns to showing everyone. The cost was accepted there |
| Finding the action that is now one click further away | The commonest job is a forgotten password | The page carries the actions prominently; the extra click is the deliberate price of a list that stays scannable at 760px |

### 2026-09-20 — decision 23: the person's page is fields, then actions, and a read-only page says so

**Decided (owner, 2026-09-20): the fields sit in a form at the top, the actions in a clearly separated area
below, and a page the reader cannot act on announces that at the top and shows its actions as unavailable.**

**Consequences to carry into the requirements.**

- **Two areas with headings**, so a consequential action is never adjacent to a text box.
- **Read-only is a state of this page, not a different page.** One page to build, one to test, and the reader
  who can act on nothing still gets somewhere to look — which decision 22 requires, since a row only opens the
  person.
- **Unavailable, never hidden.** The actions stay visible with the reason in words, the same rule decision 6
  applies to locked switches. Hiding them would make an entitlement problem look like a missing feature.
- **The create page carries no actions area at all**, because there is nothing to act on yet. The two are
  separate pages (locked requirement), and only the edit page holds actions.

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| **An action pressed while edits are unsaved** | The fields are pending and the actions act on the stored account | The page must not act silently on a half-edited person. It either saves first, with the confirmation, or says the changes are unsaved — the choice is stated, not left to the implementation |
| The reader's own page | Decision 2 makes your own account not "above you" | You may edit yourself, with two exceptions already decided: you cannot deactivate yourself, and cannot change your own sign-in name (decision 20). Both appear unavailable with the reason |
| A page for someone above your rung | Decision 2 | Fields shown and not editable, actions unavailable, and the top of the page says why — so it reads as a decision rather than a fault |
| An action that fails | The store can be unreachable | The page keeps what the reader had and says what failed (`specs/001` FR-021's per-screen state). It must not blank the page or lose unsaved edits |
| A field the source's contract forbids | Sign-in name was on that list until decision 20 | Anything still not editable is shown with its value and no control — not a control that refuses |
| Long values on a narrow window | Names and employee numbers vary | `TextTrimming` with a tooltip, never a fixed width — the rule §3 already sets for badges and indicators |

### 2026-09-20 — decision 24: the roles run down one side, in ladder order

**Decided (owner, 2026-09-20): the permission page lists the roles down one side in ladder order, with the
chosen role's permissions on the other side.**

**Why this over a menu of roles.** The ladder is a ladder on this screen for the first time anywhere in the
application — one definition of the hierarchy, visible, in order. A dropdown would hide it behind a control and
leave the reader able to see only the role they are already on. It also puts the roles above the reader in front
of them, which is what decision 6 requires: visible, marked, and not changeable.

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| A role above the reader | Decision 6 keeps it visible | It is **selectable to view** — a locked role the reader cannot open is a role they cannot check, which defeats the reason for showing it. Its switches are locked with the reason |
| The narrow window | The window can be 760px | The role column keeps a working minimum width and the permissions take the remainder. If the two cannot coexist, the screen **stacks** with the role choice above — it never squeezes either pane into a sliver |
| A role added to the catalogue later | The ladder is built with gaps on purpose | The list is drawn from the catalogue in ladder order, so a new role appears in its place with no code change here |
| Keyboard and screen-reader use | A role list is navigation, not decoration | Every role is reachable and its selection announced, and the mark that a role is locked is conveyed in words as well as by appearance |
| Which role is selected | Nine roles, one visible at a time | The selection is visually obvious and survives the permission list re-rendering beneath it, so the reader never loses their place |
| A value that is stored or defaulted | Decision 16 requires the difference to be visible | Selection does not change this: each row states which it is, whichever role is chosen |

### 2026-09-20 — decision 25: everyone is shown, and the filter is saved per person

**Decided (owner, 2026-09-20): the list shows everyone by default with the people who are switched off clearly
marked; it is filterable; and the filter is saved per person in the store. Returning from a save resets the view
to the saved filter, and to everyone when none has been saved. This supersedes decision 19.**

**What it costs, and how the cost is handled rather than hidden.** A saved filter can hide the person just
saved — the trap decision 19 closed by clearing. Since the filter now persists, the trap is handled the other
way: **the page names the filter that is active, says how many people it is hiding, and offers to show
everyone.** An invisible filter is what made a hidden person look like a failed save; a named filter with a
count does not.

**Where it is stored — verified against the schema, not assumed.** `config_settings_values` **already** has
`computer_id` and `user_id` columns with foreign keys, alongside `scope_type` and `scope_key`, under a unique
key on `(setting_key, scope_key)`. So a per-person preference needs **no new table**: it is one row carrying the
key, a user scope, and the person in the scope key.

**Requirements this creates.**

- **A user scope must be declared** where the existing scope types are declared, so the validation artefacts and
  the scope catalogue pick it up — exactly as §3.8 requires for the new role scope.
- **Precedence gains a step.** A user-scoped value must beat an `all-users` value for the same key, with the
  shipped default last: `user → all_users → default`. The resolver currently documents `override → default`.
  **Verify this against `sp_config_settings_get_effective` before relying on it**, rather than assuming the
  procedure already walks scopes in that order.
- **One row per person per key**, because of the unique key — so saving a filter is an upsert, not an insert.
- The new key ships with its paired schema artefacts and the aggregate regeneration (constitution III).

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| A reader with no saved filter | Everyone starts somewhere | Everyone is shown, and the page says no filter is applied rather than showing a filter as though one had been saved |
| The saved filter matches nobody | The roster changes under a saved filter | *"Nobody matches your filter"*, distinct from *"there are no people"*. The two mean completely different things to a reader |
| The store cannot be read | It can be unreachable | Everyone is shown, and the page says the saved filter could not be read. **Never an empty list**, which would look like an empty plant |
| Two people sharing one application account | The preference is scoped to the account, not the terminal | They share the saved filter. Stated, because the table also has a computer column and reaching for that instead would be the wrong scope — the filter belongs to the person |
| A saved filter naming a role that is later removed | Roles can be renamed away | It falls back to showing everyone and says the filter no longer applies, rather than showing an empty list forever |
| A person's role changes | The preference is keyed by the person | The saved filter does not move. It is a preference about a screen, not an entitlement |

### 2026-09-20 — decision 26: switching someone off says what it does and what it does not do

**Decided (owner, 2026-09-20): the confirmation that deactivates an account names both halves in one sentence**
— they will not be able to sign in again, and if they are signed in now they stay signed in until they close
the application.

**Finding recorded with it, because it changes how this should be read later.** `auth_sessions_tokens`
already carries a **`revoked_utc`** column, and `sp_auth_session_expiry_get`'s own comment defines a live session
as `is_active = 1 AND revoked_utc IS NULL`. So *"deactivation does not revoke tokens"* is a **policy choice,
not a limitation of the schema** — what is missing is a write path that stamps `revoked_utc`, not the storage or
the read. If that policy is ever revisited, the work is a procedure and a decision, not a redesign.

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| The wording overstating the act | It is easy to write "access removed" when it is not | The confirmation must **not** claim access is gone. It names the next sign-in and the current session separately |
| The fact being available after the dialog closes | A confirmation is seen once | The person's page carries the same sentence in its own description, so the limitation is discoverable later, not only at the moment of the decision |
| Reactivating | The reverse act | The reverse confirmation says their access returns from their next sign-in, so the pair read consistently |
| Deactivating yourself | Refused by the self-lockout guard | The confirmation never appears for that case, because the act is refused before it |
| The sentence being localised | It is user-facing text | It lives in the resource files like every other string, and it names the person it applies to |

### 2026-09-20 — decision 27: creating a person issues a PIN too, and the create page is a form only

**Derived from decisions 9, 17, 23 and the source's own create contract** — the source sets an initial
temporary password with `require_password_change = 1` at creation, and decision 9 replaced that value with a
random 4-digit PIN. So creation cannot skip the PIN: a new person has no way in without one.

**What follows.**

- **The PIN window from decision 17 is shown after a create as well as after a reset** — same window, same
  rules: it must be closed, it can be printed, and printing never closes it.
- **The create page is a form and nothing else** (decision 23): sign-in name, first name, last name, employee
  number, role. No actions area, because there is nothing to act on yet.
- **The role picker offers only roles at or below the creator's own rung** (decision 8), so the page cannot
  offer a role the save would then refuse.
- **The account is created active**, and switching it off is an action on the person's page like any other.
  There is no active control on the create form.

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| Creating several people in a row | Each needs a handover PIN | Each create shows its own PIN window, and each PIN belongs to one person. The window names who it is for, so two handovers cannot be confused |
| The creator closes the PIN window without writing it down | The window must be closable | The account exists and cannot be signed into until someone resets it. Stated, so the consequence is known before it happens |
| A duplicate sign-in name | Names are unique, and the sign-in name is now editable (decision 20) | The typed "already exists" result, with the name shown, and the form keeps what was typed |
| A role offered but not permitted | The picker is bounded by the rung | The bound is stated once in the reader's words; the unavailable roles are not listed as dead entries |
| The create succeeds and the PIN window fails to appear | The screen cannot be trusted to be infallible | The account is still created. The person's page can issue a reset to produce a PIN, which is the documented way out |

### 2026-09-20 — decision 28: a row folds onto a second line when the window is narrow

**Decided (owner, 2026-09-20): with room the row is one line; without it the row folds onto a second — name and
sign-in name first, employee number, role and status beneath — and the whole row stays a single click target.**

**Why folding rather than trimming.** The application's own layout rule is fluid, no hardcoded pixel widths,
and overflow that wraps rather than being cropped. Trimming keeps one consistent row height, but at the narrow
window it cuts several values at once and offers nothing on a touch screen, which is what a floor terminal is.
Folding costs a little vertical space and hides nothing.

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| All five facts present at every width | The point of folding | Asserted by a test at both widths, so a later change cannot quietly reintroduce cropping |
| The row staying one target | A folded row could gain a second hit area | The whole folded row opens the person. There is no second clickable region inside it |
| One-line and two-line rows in the same list | A wide window versus a narrow one | Both exist, so the list must **not** assume a fixed row height — which virtualisation makes load-bearing rather than cosmetic |
| Keyboard order | Folding is visual | The order a keyboard user tabs through does not change with the fold |
| The status marker | It is the fact people scan for | Visible in both shapes, not pushed off the second line |
| A long role name | Some are long | Wraps within its own line rather than forcing the row wider |

### 2026-09-20 — session 2 summary: the UI design pass

**Focus.** The owner asked for the UI to be designed rather than left to whoever implements it. Classified
**architectural**, like the rest of the feature; findings fold into this log rather than a separate document,
per the superspec bridge.

**What the pass settled — decisions 16 to 28.** The permission screen's shape (16) and its place as a page
(21); the PIN's reveal and its print (17), including its appearance on create (27); the two administration
pages and how they are reached (18, 21); the list's behaviour — everyone shown with the inactive marked, a
saved per-person filter (25), and rows folding when narrow (28); a row's single job (22); the person's page
layout (23); the role column (24); the sign-in name becoming editable (20); and the deactivation confirmation
(26).

**Three things the pass found that the requirements had not.**

1. **Two decisions contradicted each other** — clearing the filter after a save, and saving the filter — so
   decision 19 is struck through as superseded by decision 25, and the trap it closed is handled instead by
   naming the active filter and the number it hides.
2. **The permission grid could not fit.** Nine roles across the Settings column leaves about 90px per role
   against about 140px needed before the switch, and the window's measured restore size is 760px. The screen
   became one role at a time, down a column of roles in ladder order.
3. **Ending a session is a policy choice, not a schema limitation.** `auth_sessions_tokens.revoked_utc` already
   exists and the live-session read already honours it, so what is missing is a write path, not a design.

**State after this pass: every item is settled. The next step is `/speckit.specify`.**





