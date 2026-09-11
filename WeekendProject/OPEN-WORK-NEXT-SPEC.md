# WeekendProject — Open Work (seed for the next spec)

**Purpose.** This is the input seed for the **second** `/speckit.specify` run: everything still open in
`WeekendProject/` after `specs/001-module-mock-visual-fallback` shipped. It answers two questions —
*was the WeekendProject context absorbed into `specs/001`?* (partly; see §1) and *what is left?* (§4–§10).

**How to use it.** Run `/speckit.specify` against one **workstream** at a time (§4–§10), not the whole file.
Each workstream is sized to be one feature. The workstreams are grouped the way the source checklists already
group them, so the task text stays traceable to its origin file.

**Provenance.** Every task below was read out of the named source file. Box counts are exact counts of
`- [ ]` / `+ [ ]` markers in the live files at the time of the sweep (2026-09-11).

---

## 1. Answer: was `WeekendProject` absorbed into `specs/001`?

**Partly — in three distinct states, and it matters which is which.**

| State | Content | Action |
| --- | --- | --- |
| **Fully absorbed** | `Module_Mock/**` (Spec, Plan, Tasks, Discovery/01–03, Planning-Progress, README) → `specs/001` FR-001…FR-027, T001–T111, T115–T126 | Nothing to do. Do **not** re-run it. |
| **Absorbed only as a *removal*** | `PromptFiles/13` Phases 1–2 (mock routing / auto-force / central config / client polling / toast) and `PromptFiles/12` Subphases 0.3–0.5 (mock master tables, registry, `MockMasterDataService`, sample catalogs) | **Superseded, not open.** `specs/001` T034/T035/T038–T042 deleted these stacks and rebuilt the capability differently (`VisualReachabilityDetector`, `IReadStatusProvider`). Close them as retired. |
| **Not represented at all** | `PromptFiles/07`, `08`, `10`, `11`, `13`, `14-57%`, `15`, `App-Validation-Checklist` §4–§8, `Documents/Request-Config-Template.csv` | **This is the open work.** §4–§10 below. |

A topic search across all ten files of `specs/001` for `unified`, `User Management`, `IT Department`,
`RoleAuthorization`, `defect_type`, `urgency`, `allot`, `ignored location`, `My Requests`, `NCM`, `analytics`,
`Accept`, `Complete`, `Release`, `retention`, `deep-link`, `toast`, `Request-Config-Template`, `PromptFiles/0`,
`PromptFiles/1` and `Developer Settings` found **one incidental hit** (the word "unified" in
`checklists/requirements.md`, describing a status model). The open work below is genuinely unrepresented.

**Owner decision (2026-09-11): no request-type/subtype editor will be built.** The catalog-administration
workstream is therefore **cancelled**, not deferred — see §3. Do not resurrect the Developer Settings editor
page, the type edit view, or the guided wizard modal when writing the new spec.

`specs/001/tasks.md` itself stands at **139 boxes: 138 `[x]`, 1 `[ ]`** — the single open one is **T106** (§10).

---

## 2. Do NOT use these as backlogs (accounting traps)

Three sets of open boxes **overstate** the remaining work. Carrying them forward would mean re-implementing
shipped code.

| Source | Open boxes | Why it must not be treated as a backlog |
| --- | --- | --- |
| `WeekendProject/Module_Mock/Tasks.md` | 72 | The **seed** for `specs/001`. The identical work is 138/139 complete there. Ticked exactly once (Phase 5.3, `done 2026-09-10 (spec task T097)`) and never reconciled since. |
| `WeekendProject/PromptFiles/prompt.md` | 23 | The **master Task 1–23 index**. Tasks 1–15 and 19 are 100% checked in their per-task files; only 16–18 and 20–23 are genuinely open, and those are listed in §4/§6/§7. |
| `PromptFiles/14-30%`, `14-45%`, `14-47%`, `14-53%`, `14-55%` | 115 combined | Earlier **snapshots** of the same 47-item list that `14-57%` holds live (each revision reworded 3–8 items). Retire them; do not merge their counts. |

**Suggested handling in the new spec:** add a short "retired sources" note naming these six files, so a future
reader does not open `14-30%` and start re-implementing a 30% snapshot of work already at 57%.

---

## 3. Obsolete requirements — must be amended, not implemented

These appear as open tasks in the source files but **cannot** be built, because the thing they depend on was
deliberately deleted. `specs/001` FR-003/FR-014 and constitution II forbid reintroducing any demo/mock mode, and
`RetiredSymbolAuditTests` fails the build if a retired symbol returns.

| Obsolete task | Source | Replace with |
| --- | --- | --- |
| Handler-data + note in **mock mode**; matching QA tests | `07` "Mock-data flow coverage" (2) | Delete both boxes. |
| Per-subtype max-allotted defaults in **mock mode** | `08` "Mock-data flow coverage" (1) | Delete; the data is already on the real rows. |
| Stock-snapshot-from-sample-data + its QA test | `10` §0.6 mock parity (2) | Delete; replace with a **test-double** policy consistent with `specs/001`. |
| Aged resolved **sample** request + its QA test | `11` "Mock-data flow coverage" (2) | Delete. |
| **Mock-data toggle** in user-management CRUD (a *locked* requirement) + "Mock-toggle parity" test case | `15` locked requirements + Subphase 1.7 | Amending a locked requirement — do it explicitly in the new spec, not silently. |
| Mock-master read service + mock-master grid editor + its QA test | `12` Subphase 0.4/0.5 (3) | Delete — the mock tables are gone. The real request-type catalog's **data** half is already done (`specs/001` T100/T101); its editor UI is cancelled (see the editor row below). |
| Central mock config + client refresh/race guard | `13` Subphase 2.2 (1) | Delete; superseded by `specs/001` T035. |
| "Mock toggles behave: `Feature.RecvMockData` ON shows sample coil weight" | `App-Validation-Checklist` §3 | Rewrite as a negative check: the key no longer exists. **Applied 2026-09-11** by `specs/001` T142 — the item is struck through and points at the live checks that now cover it. No §10 work needed. |
| **Request-type/subtype editor** — Developer Settings page, Request-Type master list, type edit view, 4-step guided wizard modal, per-control card, and their tests | `PromptFiles/13` Phase 4.3 (6) + `PromptFiles/12` Subphase 0.5/0.6 UI (2) | **Cancelled by owner decision 2026-09-11 — no request-type editor will be used.** Not rebuilt, not rescoped. The editor backend is already gone from the codebase and the editor SPs (`sp_waitlist_request_{types,subtypes}_{insert,update,delete,get_all}`) are already deleted from `AllSPs.sql`, so no code removal is needed — this is a documentation retirement only. **Applied 2026-09-11:** `13` (all of Phase 4 removed), `12` (Subphase 0.5 retired, 0.6 editor tasks removed) and the ChangeLog's Workflow 12/13 appendix. Dated ChangeLog entries from 2026-09-06 were left intact as history. |

**Also stale (not an obsolete task, but a documentation defect) — APPLIED 2026-09-11:** `WeekendProject/ChangeLog.Simple.md`
documented the removed manual Infor Visual toggle as current behaviour (line 29 and feature 19). `specs/001`
T102 swept `ChangeLog.md` + `Module_Mock/*` + `PromptFiles/*`, and T125 swept root `README.md` /
`CHANGELOG.md` / `RELEASE-NOTES.md` — **`ChangeLog.Simple.md` was in neither list**, so SC-016's "0 stale
references in the project documentation" was not met. **Fixed by `specs/001` T140:** the fallback section now
describes the automatic-only fallback, feature 19 is marked withdrawn, and the "still in progress" entry
claiming the cache was a bundled sample is gone. No §10 work needed.

---

## 4. Workstream 1 — Waitlist handler fulfilment & urgency ordering

**Source:** `WeekendProject/PromptFiles/07-8%-Phase2-fulfill.md` (11 open), `08-70%-Phase2-urgency.md` (3 open),
`prompt.md` Tasks 16–18. **Live open boxes: 9 + 2** (after §3 retireals).
**Smallest, highest-value, and unblocked** — it depends only on Phase-1 work that is already complete.

### 4.1 Baseline gates (verification)
- DevOps: `MTM_Waitlist.sln` builds clean (0 errors, 0 warnings) via
  `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false`.
- QA: full `MTM_Waitlist.Tests` suite passes (`dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64`).

### 4.2 Handler-facing data (UI + code)
- Surface the handler-needed data on the request card **and** the detail page: coil/part + requested quantity,
  the coil's location/stock, destination press/work center, requester, and urgency/remaining.
  *(Note: the source's "per mock toggle" parenthetical is stale — data is always live.)*
- QA: tests for handler-data population and note add/persist, then the full suite.

### 4.3 Role- and ownership-aware Accept / Complete / Release (UI + code + auth)
- **Accept**: unaccepted request + viewer is Material Handler or above → show an **icon button** (not text);
  auto-assign to the signed-in user; status → `In Progress`; persisted.
- After accept the job **stays on the shared list**; Accept disappears for other handlers; only the **assigned**
  handler sees Complete and Release.
- **Complete** → `Done` (single step now; leave TODO hooks for a future multi-step handoff).
- **Release** → returns the job to the open list (**available**, *not* a cancellation).
- **Edit** visible to the creator only, behaviour intentionally a TODO (no behaviour).
- Only the assigned handler can Complete/Release; non-handler and non-owner see **no** actions.
- Handler can add a per-request **Note**, visible on the detail page and in history.
- QA: tests covering accept assignment, others-see-taken, assignee-only complete/release, release-vs-cancel.

### 4.4 Urgency ordering (UI + code)
- Wire the existing `UrgencyCalculator.OrderMostUrgentFirst` helper into the Waitlist list view model so the
  available/handler list is ordered most-urgent-first (least remaining / overdue first). *The due/remaining/
  overdue math and the per-subtype max-allotted settings editor are already done and ticked.*
- QA: tests for due/remaining/overdue math **and** list ordering, then the full suite.

### Constraint to carry (do not regress)
`PromptFiles/03-100%-Phase1-listdetail.md` contains a **"VERIFIED CARD ANATOMY … DO NOT REGRESS"** block. It is
still authoritative — reproduce it in the new spec's constraints rather than restating the card layout from
scratch.

---

## 5. Workstream 2 — Unified 2-line card + Category/Item taxonomy refactor

**Source:** `PromptFiles/14-57%-Phase2-UnifiedWaitlistCard.md` (**20 open**, the live revision),
`Documents/Request-Config-Template.csv`, `Plan-Design-UnifiedWaitlistCard.md`,
`Plan-Design-TypeCategoryActionRefactor.md`, `Plan-Design-TypesSubtypesCatalog.md`, and
`App-Validation-Checklist.md` §4–§8 (the validation for this work).
**The largest UI surface**, and the prerequisite for much of Workstream 3's card-level metrics.

The source file's own status: *"27/47 = 57%. The open boxes are now the interactive in-app UI phases …
those require running the WinUI app (+ XamlMcp) to build and visually verify."* Its ordering is
Phase 3 (picker) → Phase 4 (card) → Phase 5 (NCM frontend) → Phase 6.2 → Phase 7.

### 5.1 Authoritative input — carry verbatim as requirements
`Documents/Request-Config-Template.csv` is the **23-row row spec** (Pickup 11 / Deliver 8 / Assist 3 / Other 1)
and the "SOURCE OF TRUTH" the checklists defer to ("CSV wins" where docs diverge). Its **`Notes / to-build`
column carries requirements found nowhere else** and must be transcribed into the spec, notably:
- `pickup-fg` / `pickup-wip` / `pickup-outside-service` — *"No persisted source yet; must build Infor Visual
  order lookup (or capture in Module_Setup refactor)"*.
- `pickup-ncm` — *"needs new mysql table + stored procedures + settings panel"*.
- `deliver-wrong-coil` / `deliver-wrong-flatstock` — user-entered explanation.
- `other` — card spans Row 1 with `RowSpan = 2`.

### 5.2 Subphase 1.2 — Category/Item → request model (code)
- Replace `ResolveImagePath`'s ad-hoc subtype keyword matching with the explicit Category/Item model.
  (`ResolveImagePath` is a private static in `WaitlistViewViewModel` and still drives live badge→template
  selection.)
- Update `NewRequestFlowRules.GetDefaultTypes()` to order by Category then Item.

### 5.3 Phase 3 — New Request picker (Category → Item) (UI)
Data core is done; these are the interactive UI steps.
- Re-lay New Request category selection to **Category (Pickup/Deliver/Assist/Other) → Item**, matching CSV
  `Order`. *(Increment #1 landed the canonical two-stage picker; it needs in-app visual verification.)*
- **Conditional visibility**: auto-populated Items (coil/flatstock/die/component/dunnage) appear only if the
  requesting job actually has that part. *Requires the composition-root bridge to `IActiveJobItemResolverService`
  (the NewRequest library references Settings but not Setup).*
- **Dunnage image-card selection**, same card style as Module_Setup; the user always selects the dunnage part.
- **User-entry Item paths**: Die destination, Component pick, NCM defect, Other free text — validated against
  Infor Visual where the CSV says so.
- Keep **destination = requesting work center** for all Deliver rows (no user entry).
- **Zero-payload Items** (Riser Table, Hopper) as pure flag selections.

### 5.4 Phase 4 — Uniform 2-line card (UI + code)
- Build the single uniform 2-line card control: **Line 1 = umbrella verb, Line 2 = item identifier**.
- Move type-specific detail fields **off** the card and onto the detail page.
- Render the full-width **'Other'** card on Row 1 with `RowSpan = 2`.
- Re-resolve item data at render for coil/flatstock/die/dunnage rows from work center/job.
- Update badge/selector so Category maps to the correct image family.

### 5.5 Phase 5 — NCM defect feature (UI + code + tests)
Backend (`waitlist_defect_types` table + CRUD SPs + `DefectTypeCatalogService`) is done.
- Settings panel: **defect-types editor** in Module_Settings (searchable list, add/remove).
- Wire NCM Item **Line 2** = `{PartNumber} / {Defect(User Entry)}` from the managed list.
- QA: NCM defect table + SP + picker tests.

### 5.6 Phase 6.2 — Item resolution implementation (code)
- Implement **FG / WIP / Outside** Item resolution from the confirmed derivation, reusing the
  `GetInventoryLocations.sql` / `LookupWorkOrder.sql` / `GetSubordinateParts.sql` join patterns
  (VISUAL / MTMFG), feeding the classifier's `DispositionInput`.
- Wire `{PartNumber} / {Sequence}` from `setup_active_jobs.sequence_number` and the derived type.
- **Gate:** FG/WIP/Outside items resolve a real part/sequence/disposition — **no hard-coded
  `FG-10042` / `WO-073112 / RM-48190`**.

### 5.7 Phase 7 — Validation & cleanup
- Tests covering catalog load, resolvers, the New Request picker, and card render across **all 23 canonical
  CSV rows**.
- Remove the now-obsolete mock field hard-coding in `WaitlistViewViewModel.AddRequestFields`, replaced by the
  resolvers.
- Security review: role gating and Infor Visual validation for user-entry Items.
- Then run `App-Validation-Checklist.md` §4–§8 (**27 checks**) as the acceptance suite for this workstream.
  *§0–§3 of that file are its `[NOW]` gates and are already executed by `specs/001` T104/T105 — do not re-list
  them here.*

---

## 6. Workstream 3 — Waitlist analytics

**Source:** `PromptFiles/10-0%-Phase3-analytics.md` (**23 open**) + `prompt.md` Tasks 20–21.
**Prerequisite:** resolve the five open decisions in §6.5 **before** specifying.

### 6.1 Baseline gates (verification)
- DevOps: solution builds clean. QA: full suite passes.

### 6.2 Subphase 0.1 — Stock-shortage snapshot at request creation (code + data)
- When a part/coil-bearing request is created, **quietly record a snapshot** of that coil/part's on-hand quantity
  and location, with no handler/requester action. Artifacts: `IStockSnapshotRecorder` in
  `MTM_Waitlist.Core/Contracts/Services`; recorder + snapshot model/service in `MTM_Waitlist.Analytics/Services`
  and `/Models`; the request-creation path invokes the Core contract via DI.
- QA: tests that request creation writes the snapshot, then the full suite.

### 6.3 Subphase 0.2 — Plant Manager waitlist analytics screen (UI + code + auth + nav)
- Role-gated (Plant Manager+) screen presenting: volumes by request type and over time; fill speed / average
  wait; on-time vs overdue; cancellations and reasons; handler workload/performance; and the stock-shortage
  metrics from §6.2. Artifacts: `AnalyticsViewModel` in `MTM_Waitlist.Analytics/ViewModels`; `AnalyticsPage.xaml(.cs)`
  in `Module_Analytics/Views`; metric/query services in `MTM_Waitlist.Analytics/Services`.
- Role-gate to Plant Manager and above; register page + view model via `PageService`/DI and add to shell
  navigation. Artifacts: `AllowedAnalyticsRoles`/`CanViewAnalytics`; `pageService.Configure<AnalyticsViewModel,
  AnalyticsPage>()` + `AddTransient` in `ServiceRegistrationExtensions.cs`; `AddAnalyticsModuleServices` in the
  app-root `ModuleDependencyInjectionExtensions.cs`; `<NavigationViewItem x:Uid="Shell_Analytics">` in
  `ShellPage.xaml` + role visibility; `Shell_Analytics.Content` in `Strings/en-us/Resources.resw`.
- QA: tests for metric computation from audit/snapshot data and role gating.

### 6.4 Subphase 0.3–0.6 — Supervisor analytics, preferences, export, localization
*(Prerequisite: §6.2 + §6.3 complete.)*
- **Supervisor Analytics page + view model**, role-gated at rank ≥ Production Lead, reusing the Waitlist
  building-filter interaction model.
- **Role gate** = Production Lead, Setup Lead, Plant Manager, IT Department, Developer, with explicit **deny**
  behaviour for every other role.
- **Segment** by the day-one user types — Material Handler, Production, Setup — as composable panels so each
  user type renders its own KPI + table set without reshaping page architecture.
- **Filter contract**: User Type, Shift, Building (All / Expo Drive / Vits Drive), plant-wide aggregation, and
  role-switch behaviour. Keep `BuildingSelectionService` as the building source of truth.
- **Database**: persist per-user analytics preferences (auto-refresh interval, visible KPI/table/chart toggles,
  default filters) per `database-schema-rules.instructions.md` — table under `Database/Tables/<NN_name>/` with
  `create.sql` + `rollback.sql` plus aggregate regeneration.
- **Backend**: stored procedures for user-scoped preference read/write, behind a repository/service boundary
  that fails gracefully when a preference row is absent.
- **VM**: default auto-refresh cadence **5 minutes**, with a manual refresh action and a persisted per-user
  override.
- **Print-friendly export**: OS/browser print-to-PDF scoped to the currently filtered data, excluding hidden
  sections. *(A direct PDF generation engine is explicitly out of scope for v1.)*
- **Localization**: `.resw` keys for navigation labels, filter labels, KPI labels and export action text — no
  literal UI text.
- **QA**: unit tests for role access, filter composition, segmentation, role-switch behaviour, preference
  serialization round-trip, refresh cadence, and that printed output reflects active filters and excludes
  hidden sections; then full suite + build green and summarize v1 limitations.

### 6.5 Open decisions to resolve first (blockers, not checkboxes)
1. App User GUID → name mapping.
2. Charting strategy.
3. Building source of truth.
4. Access-enforcement depth.
5. User creation is **excluded** here → routed to Workstream 5.

---

## 7. Workstream 4 — Administration: cancellations monitor & request retention

**Source:** `PromptFiles/11-0%-Phase3-admin.md` (**10 open**) + `prompt.md` Tasks 22–23. **Live: 8** after §3.

### 7.1 Baseline gates (verification)
- DevOps: solution builds clean. QA: full suite passes.

### 7.2 Subphase 0.1 — Admin monitor of cancelled requests (UI + code + auth)
- Role-gated (Developer/admin-level) view monitoring cancelled requests — **who** cancelled, **when**, request
  type/subtype, and **reason** — read from retained cancelled records that are never purged.
- Role-gate the monitor; register page + view model via `PageService`/DI and add to shell navigation.
- QA: tests that the monitor lists retained cancellations with the correct fields and enforces the role gate.

### 7.3 Subphase 0.2 — Resolved-request retention & archival (code + DB + UI)
- Honour `waitlist.resolved_retention_days` (**default 90**): resolved (`Done`/`Cancelled`) requests older than
  the window are **hidden from the active Waitlist list but preserved** — never deleted — for analytics/admin.
- Add active-list filtering so aged resolved requests do not appear, plus a housekeeping/archival routine;
  localize the UI strings.
- QA: tests for retention-window filtering and archival.

---

## 8. Workstream 5 — User management (Settings → Administration)

**Source:** `PromptFiles/15-0%-UserManagement.md` (**41 open**, all unchecked). **Live: 39** after §3.
**Recommendation: its own spec** — it is a self-contained workstream with a locked requirements section, its own
schema, and no dependency on the card work.

Its "**Locked requirements (do not re-litigate)**" section is fully specified: rank hierarchy; `Admin` →
`IT Department`; one role per user; create/edit fields; self-lockout guard; rank rule; audit table; deactivation
does not revoke tokens; search matches username/display name/employee ID/role; **Employee ID is NOT unique**;
username normalization → UPPERCASE plus a data migration; same-transaction profile + role; a new
"Administration" Settings category; **separate Pages**, not dialogs.

### 8.1 Baseline gates (verification)
- DevOps: solution builds clean. QA: full suite passes.

### 8.2 Subphase 1.1 — Database (DB)
- Add `IT Department` to the `auth_roles_catalog` seed (in `Admin`'s place) and regenerate `AllSeeds.sql`.
  Artifact: `Database/Seeds/<NN_name>/create.sql`.
- Add a **field-level** user-management audit table (e.g. `auth_user_management_audit`) capturing display name /
  employee id / active / role / password-reset changes plus acting user and timestamp, per
  `database-schema-rules.instructions.md`.
- Add stored procedures under `Database/StoredProcedures/`: `sp_user_management_list` (optional role filter /
  search, incl. role + active status), `sp_user_management_get`, `sp_user_management_create` (profile + role
  assignment **in one transaction**; enforce the role-rank rule), `sp_user_management_update` (display name /
  employee id / role / active; enforce rank + self-lockout), `sp_user_management_reset_password` (set `0000`,
  `require_password_change = 1`), and `sp_auth_roles_list` (roles for the picker).
- Switch `username_normalized` to **UPPERCASE** and migrate existing lowercase rows. *Affects the unique key,
  login credential reads, and session snapshot reads.*
- Regenerate `AllTables.sql` / `AllSeeds.sql` / `AllSPs.sql`; keep `update_table_descriptions.sql` in sync.
- QA: validate the new schema + SPs against a live `mtm_waitlist` (create/update/reset paths, rank rule,
  self-lockout rejection).

### 8.3 Subphase 1.2 — Shared role helper (code)
- Add `RoleAuthorization` — the rank table plus `IsAtLeast(role, required)` and a helper returning all roles
  ≥ a given rank.
- Refactor the existing hardcoded `Allowed*Roles` arrays (`SettingsViewModel`, `SetupWorkstationViewModel`,
  `DunnageWorkflowService`) onto it.
- Replace literal `"Admin"` references with the `IT Department` role.
- QA: unit tests for rank ordering, `IsAtLeast` boundaries, and the roles-at-or-above list.

### 8.4 Subphase 1.3 — Data layer (code)
- Add `IUserManagementRepository` + implementation calling the new stored procedures (**no inline SQL**).
- Add `IUserManagementService` + implementation for role checks, validation and audit writes; DI-register in
  `ServiceRegistrationExtensions.cs`.
- QA: repository tests — transactional create rolls back **both** rows on mid-way failure; duplicate
  `username_normalized` returns a typed "already exists" result; deactivation does not clear
  `auth_sessions_tokens`; one audit row per create/edit/reset.

### 8.5 Subphase 1.4 — ViewModel + Views (UI)
- Add the **Administration** category and a `CanManageUsers` gate (rank ≥ Production Lead), user list, search
  and sort/filter by role to `SettingsViewModel`.
- Add the Administration category and the **User Management** expander to `SettingsPage.xaml` (list, search,
  New User, per-row Edit) following the existing `muxc:Expander` + `x:Load` pattern.
- Add `CreateUserPage` and `EditUserPage` (+ view models), registered for navigation via DI / `PageService`.
- Render role badges and active/inactive indicators with `TextTrimming="CharacterEllipsis"` + tooltip and
  `Min`/`MaxWidth` — **never** a fixed width.
- Localization: `.resw` keys for category/expander headers, field labels and all action/error text.
- QA: `Module_Settings` view-model tests — list/search/sort/filter, role-rank guard, self-lockout guard,
  create/edit flows.

### 8.6 Subphase 1.5 — Critical mitigations (must implement)
- Do **not** copy the fire-and-forget init pattern (`_ = InitializeHotWorkCentersAsync();`) — use
  `async Task`, never `async void`.
- Do **not** use sync-over-async on the UI thread — read settings/roles with `await` in
  `OnNavigatedTo`/init, never `.Result` / `.GetAwaiter().GetResult()`.
- Keep `ObservableCollection` mutation strictly on the UI thread; marshal with `DispatcherQueue.TryEnqueue`
  (otherwise `RPC_E_WRONG_THREAD`).
- Do **not** wrap the transactional create in blind retry — return a typed "username already exists" on
  duplicate key (1062) or make the SP idempotent.
- Guard the Save command against reentrancy/double-click (`CanExecute` + `IsSaving`).
- Avoid hardcoded widths in the create/edit forms; verify at 150 %+ scaling.
- Use a virtualizing list for the user grid.

### 8.7 Subphase 1.6 — Input validation contract
`Username` 1–128 UPPERCASE · `FirstName`/`LastName` 1–128 → `display_name` ≤ 256 · `EmployeeId` exactly `\d{4}`
and **not unique** · `Role` ≤ current user's rank · `Active` cannot deactivate self · `ResetPassword` behind
confirmation · `SearchQuery` matches username/display name/employee id/role.
**Actor rank MUST be re-validated in the SP/service, not only in the UI.**

### 8.8 Subphase 1.7 — Required test cases
RoleAuthorization rank refusal · self-lockout rejection · transactional rollback · duplicate-username typed
result · uppercase normalization (`jsmith` → `JSMITH`, login matches either case) · Employee ID
non-uniqueness (`6229` shared by two users) · search matches all four fields · UI-thread collection safety ·
deactivate without session revocation · double-click Save creates exactly one row · audit trail writes a row
with acting user + timestamp.
*(The source's twelfth case, "mock-toggle parity", is obsolete — see §3.)*

---

## 9. Workstream 6 — Startup gate polish

**Source:** `PromptFiles/13-63%-Developer-UI.md` (startup-gate items only).

> **Cancelled by owner decision (2026-09-11): no request-type/subtype editor will be used.** That removes the
> entire catalog-administration workstream — the role-gated Developer Settings page, the Request-Type master
> list, the type edit view, the 4-step guided wizard modal, the per-control card, the real-catalog grid editor,
> and their tests (`13` Phase 4.3 ×6, `12` Subphase 0.5/0.6 UI ×2). Nothing is deferred or rescoped here; the
> editor is simply not being built. See §3. The only surviving item from this workstream is the startup gate
> below.

### 9.1 Splash messaging when `mtm_waitlist` is unreachable (UI)
- Confirm and polish the **splash messaging** — a clear, non-technical message ("Cannot connect to the MTM
  Waitlist server…") with a **Retry** option and a **Cancel** that exits.
  *`specs/001` FR-021 covers per-screen internal-store unavailable state; the splash/startup gate copy is not
  specified anywhere else.*

### 9.2 Verification
- QA: verify the hard no-start-without-`mtm_waitlist` gate with the user-facing message; full suite green.

---

## 10. Workstream 7 — Close out `specs/001` and documentation hygiene

**Source:** `specs/001/tasks.md` T106 (the only open box in that file) plus the gaps found in §1/§3.

### 10.1 Spec 001 T106 — end-to-end acceptance (verification, environment-blocked)
- Publish and run `MTM_Waitlist.Mock.Service` **on the database host**, then execute `quickstart.md` §1–§8:
  fallback proof needs Infor Visual made unreachable; defect proof needs a signed-in build; the backup/restore
  drill needs a throwaway store.
- Start the **SC-007 / SC-008 30-day observation windows** (≥ 95 % of scheduled refresh cycles succeeding;
  100 % of scheduled backup windows producing a restorable artifact per enabled store).

### 10.2 Documentation hygiene (docs)
- Fix `WeekendProject/ChangeLog.Simple.md` — it still documents the removed manual Infor Visual toggle as
  current (line 29, feature 19). This is the concrete gap behind `specs/001` SC-016.
- Retire the stale backlog sources named in §2 (add a "retired sources" note so they are not reopened).
- Reconcile `WeekendProject/Module_Mock/Tasks.md`, which is 72 boxes behind reality.

---

## 11. Recommended sequencing

| Order | Workstream | Why here |
| --- | --- | --- |
| 1 | **§10 Close out `specs/001`** | One task plus an infrastructure action, and it **starts the SC-007/SC-008 clocks** that need 30 days of wall time. |
| 2 | **§4 Handler fulfilment & urgency** | Smallest, already unblocked, no dependency on the taxonomy refactor, immediate user value. |
| 3 | **§5 Unified 2-line card + taxonomy** | Largest UI surface; prerequisite for much of §6's card-level metrics. |
| 4 | **§6 Analytics** | Needs §5 for card-level metrics; resolve its five open decisions first. |
| 5 | **§7 Admin monitor & retention** | Independent, small. |
| 6 | **§8 User management** | Self-contained; can run in parallel with §5–§7 if capacity allows. |
| 7 | **§9 Startup gate polish** | Small and independent. Previously blocked on the editor decision; now that the editor is cancelled it can be done at any time. |

---

## 12. Summary of live open boxes

| Live source file | Open (as written) | Retired (§3 obsolete or cancelled) | **Carry forward** |
| --- | ---: | ---: | ---: |
| `PromptFiles/07-8%-Phase2-fulfill.md` | 11 | 2 | 9 |
| `PromptFiles/08-70%-Phase2-urgency.md` | 3 | 1 | 2 |
| `PromptFiles/10-0%-Phase3-analytics.md` | 23 | 3 | 20 |
| `PromptFiles/11-0%-Phase3-admin.md` | 10 | 2 | 8 |
| `PromptFiles/12-71%-MockMasterData-DbDriven.md` | 6 | 6 | **0** |
| `PromptFiles/13-63%-Developer-UI.md` | 10 | 8 | 2 |
| `PromptFiles/14-57%-Phase2-UnifiedWaitlistCard.md` | 20 | 0 | 20 |
| `PromptFiles/15-0%-UserManagement.md` | 41 | 2 | 39 |
| `PromptFiles/App-Validation-Checklist.md` §4–§8 | 27 | 0 | 27 |
| **Total** | **151** | **24** | **127** |

*`12` now contributes nothing: its 4 mock-parity boxes are obsolete (§3) and its 2 editor boxes are cancelled
by the 2026-09-11 decision. `13` keeps only the two startup-gate items; its 6 editor boxes are cancelled and
its 1 race-guard box is obsolete.*

*Reconciling the validation checklist:* the file holds **46** unchecked boxes in total. §0 (3), §0a (1), §1 (8),
§2 (1) and §3 (6) = **19** are its `[NOW]` pre-flight gates, and `specs/001` T104/T105 already executed them
(Phase 19: 699 passed / 0 failed / 0 skipped). Only §4–§8 = **27** belong to this backlog, which is why 27 —
not 46 — appears above.

Excluded by design (not work — see §2): `Module_Mock/Tasks.md` (72), `prompt.md` (23), the five `14-*`
archives (115).
Also to fold in: `Documents/Request-Config-Template.csv` (23-row spec + `Notes / to-build`), the 19 SVGs in
`Mockups/` (UI reference art, already cited by the checklists), and the three `Plan-Design-*` reference docs.
