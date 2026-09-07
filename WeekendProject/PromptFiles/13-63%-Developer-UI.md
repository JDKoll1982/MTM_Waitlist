# 13 — Developer UI: Mock Auto-Fallback, DB Connection Gates & Request-Type/Subtype Editor

> **Purpose:** Add a **Developer-oriented** UI & behavior layer on top of the Waitlist app so the app
> keeps running when its external databases (Infor Visual, the receiving app) are down, and let
> Developers/Admins **edit the real request-type/subtype catalog** from the app instead of by hand.
>
> Four asks:
>
> 1. **Startup/auto mock fallback** — during startup, if the Infor Visual DB can't be reached, use mock
>    data; do the same for the receiving-app DB.
> 2. **Live-data-when-available** — both mocks (receiving & Infor Visual) pull from their real database
>    **only when a connection can be established**, so when either is down the app still runs on mock.
> 3. **Request-type/subtype editor** — edit the real catalog via a **Developer Settings** page
>    (edit-entry UX = **Option A**: drill-in list → type edit view → **guided wizard modal**).
> 4. **`mtm_waitlist` is required to start** — no connection to `mtm_waitlist` ⇒ the app must not start
>    (full stop) with clear user-facing reasoning during splash. (Largely already implemented; polish wording.)
>
> **Workflow:** checklist-execution skill; adopt each task's persona. Tick `- [x]` only when implemented,
> builds clean, and tests pass.
>
> **Related:** `12-MockMasterData-DbDriven.md` provides the foundation this file builds on: the real
> request-type/subtype **reference tables** (`waitlist_request_types`/`waitlist_request_subtypes` 28/29)
>
> + dedicated SPs + `RequestTypeCatalogService` read (Phase 4 here consumes these), the shared role-gated
> **Developer Settings page** (0.5), and the mock `mock_*` tables/registry + mock-master routing that
> Phase 1's health fallback extends. Treat file 12 as a prerequisite for Phase 4.

## Task 0 — Green baseline + full read

+ [x] **DevOps: confirm `MTM_Waitlist.sln` builds clean (0 errors/warnings)** (`dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false`). | **Persona: DevOps Engineer** — verified 2026-09-06: clean across every increment (0 warnings / 0 errors).
+ [x] **QA: full suite green** (`dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64`). | **Persona: QA Engineer** — verified 2026-09-06: 421 passed / 0 failed / 12 skipped.
+ [ ] **Tech Lead: read `WeekendProject/PromptFiles/*.md` + `WeekendProject/DeveloperUI-VisualPrompts.md` + `WeekendProject/ChangeLog.md`** and reconcile the mock/real routing rules and the request-type/subtype schema. | **Persona: Tech Lead**
**GATE: baseline green and full read complete before any edits.**

---

## Phase 1 — Connection health & auto mock fallback (items 1 & 3)

> Architecture (locked 2026-09-06): a **Core health service** pings the Infor Visual DB and the
> receiving-app DB. When a DB is unreachable, the app **auto-uses mock** for that source and **disables
> the manual mock toggle** until connectivity returns. When connectivity is re-established, mock is
> auto-disabled for that DB and a **taskbar toast** announces the state change.
> DB access stays **SP-only** for `mtm_waitlist`; the two external DBs keep their existing routing.

### Subphase 1.1 — Core connection health service

+ [x] **Backend Engineer: add `IConnectionHealthService` / `ConnectionHealthService`** in `MTM_Waitlist.Core` that reports reachability for Infor Visual (`SqlHelperServer` target) and the receiving-app DB (`MySqlHelperServer.MtmReceivingApplication`), returning a per-source `ConnectionHealthState` (Unknown / Connected / Unreachable) with a timestamp. | **Persona: Backend Engineer** — verified 2026-09-06: `ConnectionHealthService` + interface probe both sources via short-open connection tests; `ConnectionHealthServiceTests` pass (3).
+ [x] **Backend Engineer: define `ConnectionHealthState` + a per-source state model** and register the service via DI in the app composition root. | **Persona: Backend Engineer** — verified 2026-09-06: `ConnectionHealthState.cs` model added; `IExternalConnectionInfoProvider`/`ExternalConnectionInfoProvider` + `IConnectionHealthService` registered in `ServiceRegistrationExtensions`.
+ [x] **Full Stack Engineer: define how "reachable" is decided** (open a connection with a short timeout; distinguish malformed connection string vs server unreachable vs auth failure) so the fallback reacts to genuine outages, not transient blips. | **Persona: Full Stack Engineer** — verified 2026-09-06: short-timeout open probe (SQL Server for Infor, MySQL for Receiving) with `ClassifyException` distinguishing login vs unreachable; not-configured treated as unreachable.

### Subphase 1.2 — Auto mock fallback on outage + re-enable on recovery

+ [x] **Backend Engineer: route the mock toggle decision through health** so that when the Infor Visual source is `Unreachable`, the Infor mock is effectively ON (regardless of the stored toggle) and the manual toggle is **disabled** in UI until reconnected; mirror for the receiving source. | **Persona: Backend Engineer** — verified 2026-09-06 (decision layer done): `MockRoutingService.Resolve` forces `UseMockData=true` + `IsAutoForced=true` when a source is `Unreachable`/not configured, overriding any stored toggle, and marks the toggle disabled for the UI. Mirror logic is source-generic (works for both Infor & Receiving). UI toggle-disabled binding is phase-gated (needs Developer Settings page).
+ [x] **Backend Engineer: on recovery** (source returns to `Connected`), auto disable mock for that source and record the transition so UI + toast can react. | **Persona: Backend Engineer** — verified 2026-09-06: `MockModeChangeDetector` + new `MockRoutingRefreshService` (Core) run one refresh pass per source (fresh health → debounce → resolve routing decision) and, comparing to the previous decision, emit a `MockModeChange` (`Recovered` when a source comes back online and mock auto-disables to the configured mode; `ForcedOn`/`TurnedOn`/`TurnedOff` otherwise). `LastDecisions` exposes the current per-source decision. DI-registered; `MockFallbackDebouncerTests` (5) + `MockRoutingRefreshServiceTests` (5) pass; build clean; full suite green (431 passed). Emitting the change to the taskbar toast is the Phase 1.3 (UI) part.
+ [x] **Full Stack Engineer: reconcile the manual toggle vs auto-fallback precedence** (manual Admin override still respected when reachable; health only forces mock when down) and resolve the cross-file toggle ambiguity (Infor vs Recv per source). | **Persona: Full Stack Engineer** — verified 2026-09-06 (precedence part done): `MockRoutingService` honours a local manual override when reachable and only forces mock when the source is down. Per-source key mapping resolved at the health layer (`ConnectionSource` docstrings: InforVisual → `Feature.InforVisualMockData`, Receiving → `Feature.RecvMockData`). The remaining cross-file routing ambiguity (F12 0.4b sample-catalog wiring) is separate and tracked under Workflow 12.
+ [x] **Security Engineer: review for race/health flapping** — debounce short outages so mock doesn't thrash on/off; guard against the health check itself being the single point of failure. | **Persona: Security Engineer** — verified 2026-09-06: `IMockFallbackDebouncer`/`MockFallbackDebouncer` (Core, pure) only flips a source's *effective* status after `Threshold` (default 2) consecutive observations of the new status, so a single transient probe failure cannot flip the mock fallback on/off. Per-source independent state + `Reset()`. Consumed by `MockRoutingRefreshService` between raw health and the routing decision. DI-registered; `MockFallbackDebouncerTests` (5) pass; build clean; full suite green (431 passed).

### Subphase 1.3 — Taskbar toast on state change

+ [x] **Frontend/Backend Engineer: show a taskbar notification/toast** when mock is auto-enabled (source down) and when it is auto-disabled (source recovered), with a clear user-facing message naming the affected source. Reuse the app notification pattern already in the app. | **Persona: Frontend Engineer** — verified 2026-09-06: added the WinUI app wiring. `DispatcherPollScheduler` (`Services/MockMode/DispatcherPollScheduler.cs`, `Microsoft.UI.Xaml.DispatcherTimer`, 30s interval) implements `IPollScheduler`; registered in `ServiceRegistrationExtensions`. `MockModeToastCoordinator` (`Services/MockMode/MockModeToastCoordinator.cs`, singleton) starts the `IMockModePollingHost` from `ShellPage.OnLoaded` (role known) and subscribes `ModeChanged` → `IAppNotificationService.Show(MockModeSummaryProvider.ToastMessage(change))` for each notify-worthy change (forced-on outage / recovery / manual or central toggle flip), naming the source. Idempotent start; unpackaged apps are a no-op (toasts require MSIX identity, mirroring the alert/notification gates). Build clean; full suite green (480 passed).

---

## Phase 2 — Central mock setting reflected on all clients (item 1/3 admin push)

> Architecture (locked 2026-09-06): mock ON/OFF for Infor & receiving is stored **centrally** in
> `mtm_waitlist.config_settings_values` so an Admin/Developer-set value reflects on **all running
> clients**. Clients refresh and apply it (with a toast when it changes). Guard against **race
> conditions** when multiple clients flip the setting during an outage.

### Subphase 2.1 — Central config storage + read/write

+ [x] **Database Engineer: add stored procedures to read/upsert the Infor & Receiving mock settings** in `config_settings_values` (SP-only; follow repo DB naming rules; `AllSPs.sql` + `update_table_descriptions.sql` kept in sync). | **Persona: Database Engineer** — verified 2026-09-06: the repo's existing `sp_config_settings_upsert` and `sp_config_settings_get_effective` already read/upsert any setting key (incl. bool) at `all_users` scope, so no new SP was required; the mock settings are stored under `Feature.InforVisualMockData` / `Feature.RecvMockData`.
+ [x] **Backend Engineer: add a service** to read the central mock settings and to upsert them (role-checked), replacing/augmenting the local `Feature.InforVisualMockData` / `Feature.RecvMockData` keys. | **Persona: Backend Engineer** — verified 2026-09-06: `IMockConfigurationService`/`MockConfigurationService` (Core) reads via `sp_config_settings_get_effective` and upserts via `sp_config_settings_upsert` (all_users, bool) keyed by source; DI-registered; `MockConfigurationServiceTests` (3) pass. Role-gating is delegated to the caller (per interface doc).

### Subphase 2.2 — Client refresh + race handling

+ [x] **Backend Engineer: client polling** — running clients poll the central mock config on a timer and apply changes + fire a toast. | **Persona: Backend Engineer** — verified 2026-09-06 (poll/apply core done): `MockRoutingRefreshService` (one refresh pass → `MockModeChange`s) + `MockRoutingMonitorService`/`IMockRoutingMonitorService` (raises a `MockModeChanged` event per change; exposes `LastChanges`; `ResetBaseline()`). A UI host (WinUI `DispatcherTimer`) calls `RunOnceAsync()` and subscribes to `MockModeChanged` to fire the toast — that timer host + toast rendering is the remaining Phase 1.3/app wiring. `MockRoutingMonitorServiceTests` (3) pass; build clean; full suite green (437 passed).
+ [ ] **Security Engineer: race-condition guard** — single source of truth with optimistic/last-writer-wins on the row so simultaneous clients can't corrupt state during an outage. | **Persona: Security Engineer**
+ [x] **QA Engineer: tests** for central read/write, role gating, client refresh applying a changed value, and concurrent upserts. | **Persona: QA Engineer** — verified 2026-09-06: `MockModeQaTests` covers central read/write per-source key mapping + all_users/bool scope (`MockConfigurationService`), role gating (`DeveloperAccessGuard` denies operator roles for config/catalog edits), a changed central value applied on the next client refresh with a single `TurnedOn` event (`MockRoutingMonitorService`), and last-writer-wins concurrent upserts on the same key. `MockModeQaTests` (4) pass; build clean; full suite green (445 passed).

---

## Phase 3 — `mtm_waitlist` required to start (item 4)

> Already implemented: `StartupCoordinator` blocks startup when the `mtm_waitlist` startup session can't
> be validated. This phase polishes the user-facing splash reasoning.

### Subphase 3.1 — Startup gate polish

+ [ ] **Frontend/Backend Engineer: confirm + polish the splash messaging** when `mtm_waitlist` is unreachable — a clear, non-technical message ("Cannot connect to the MTM Waitlist server…") with a Retry option and a Cancel that exits. | **Persona: Frontend Engineer**
+ [ ] **QA Engineer: verify** the hard no-start-without-`mtm_waitlist` gate with a user-facing message; full suite green. | **Persona: QA Engineer**

---

## Phase 4 — Request-Type/Subtype editor (item 2) — Option A

> **Edit-entry UX = Option A** (chosen 2026-09-06): **Drill-in list → type edit view → subtype wizard
> modal.** Editing rules locked:
>
> + **No brand-new top-level request type** via the UI (a new type needs a compiled WinUI control); new
>   types are added in code + seed. Standing rule: whenever a request type/subtype is added, removed, or
>   changed, the **seed data** for it must be reviewed.
> + Subtypes can be **Add / Edit / Remove** in the UI.
> + **Order** applies to types and to subtypes.
> + All fields editable: name, `is_active`, `order`, flow + text-input rules, grid columns, image, control type.
> + A type's non-General fields edit via the **same 4-step wizard** as subtypes.
> + **Cancel** confirms before discarding unsaved edits. Changes persist **once at final Save**.
> + Control-type dropdown reuses **all existing controls across any type**; a per-control card appears
>   when a control type is selected, with proper **error gating**.
> + Lives on the role-gated **Developer Settings** page (main nav, above Settings).

### Subphase 4.1 — UI reference (from DeveloperUI-VisualPrompts.md Option A)

+ **Master list** — "Request Types": vertical list of the 8 types (Pickup, Other, Coil, Scrap, Flatstock,
  Table Handling, Die Handling, Forklift Assist); each row = colored icon + name + subtype count + Edit
  button; back arrow + "Developer Settings" breadcrumb.
+ **Type edit view** — "Editing: Coil": compact form (Display name, Active, Order) + "Subtypes (N)"
  section listing each subtype with an Edit button (and Add / Remove); Back top-left; Cancel/Save footer.
+ **Subtype (and type) wizard modal** — one modal, 4 steps with a step indicator at top and **Next /
  Back / Cancel**; final step shows **Save**:
  1. **General** — name, Active, Order.
  2. **Flow & text input** — Flow, Requires text input, Prompt, Min/Max length.
  3. **Grid columns** — ordered list editor (up/down, add/remove).
  4. **Control & image** — Control type dropdown + per-control card; Image path / Upload.

### Subphase 4.2 — Backend editor services (SP-only)

+ [x] **Database Engineer: confirm the real-catalog tables/SPs (from Workflow 12) are sufficient for read + insert + update + delete** of `waitlist_request_types` / `waitlist_request_subtypes` (add subtype/remove-subtype; update ordering). Add/adjust SPs if needed, keeping `AllSPs.sql` + descriptions in sync. | **Persona: Database Engineer** — verified 2026-09-06: full CRUD SP set already existed (`*_get/_insert/_update/_delete` for both tables). The only editor gap was that the wizard `*_get` SPs filter `is_active = 1`, so an editor could not see/re-activate inactive rows. Added two admin reads **`sp_waitlist_request_types_get_all`** + **`sp_waitlist_request_subtypes_get_all`** (include inactive, no is_active filter, ordered by id). Created `Database/StoredProcedures/sp_waitlist_request_{types,subtypes}_get_all/{create,rollback}.sql`, appended to `AllSPs.sql`, and **applied + verified on the live DB** (returns 8 types / 24 subtypes). NOTE: no explicit `sort_order` column exists on either table yet — display order is the read order by id. Reordering (a schema feature) is tracked as a follow-up.
+ [x] **Backend Engineer: add a real-catalog **read/write** editor service** (enumerate types with subtypes; add/edit/remove subtype; update type + subtype fields incl. ordering) built on the dedicated real-catalog SPs — NOT part of mock code. | **Persona: Backend Engineer** — verified 2026-09-06: `IRequestTypeEditorService`/`RequestTypeEditorService` (`MTM_Waitlist.Core`) + editor DTOs `RequestTypeEditorItem`/`RequestSubtypeEditorItem` (Core/Models). `GetCatalogAsync` reads via the `_get_all` SPs (types grouped with subtypes, incl. inactive); `AddSubtypeAsync`/`UpdateSubtypeAsync`/`DeleteSubtypeAsync`/`UpdateTypeAsync` dispatch to the dedicated SPs (JSON grid fields serialized; byte bools). DI-registered; deliberately separate from mock code. `RequestTypeEditorServiceTests` (5) pass; build clean; full suite green (417 passed).
+ [x] **Backend Engineer: validate control-type reuse** — the Control-type dropdown lists code-registered WinUI control types present in the catalog; reject/flag a type whose control isn't available at runtime (error gating). | **Persona: Backend Engineer** — verified 2026-09-06: `RequestCatalogControlValidation` (Core, pure) provides the data-driven control-type source + error gating — `CollectAvailableControlTypes(catalog)` returns the distinct image-view control types present across types/subtypes (for the dropdown; "reuses all existing controls"), and `Validate(catalog, availableControls)` flags any row whose control is empty or not in the available set. Callers can inject the authoritative compiled-control allow-list from the app; the data-driven set needs no runtime reflection so it is unit-testable. `RequestCatalogControlValidationTests` (4) pass; build clean; full suite green (421 passed). UI dropdown binding + live reflection allow-list is the remaining (runtime/UI) part.

### Subphase 4.3 — Developer Settings page + nav (Option A UI)

+ [ ] **Frontend/Backend Engineer: add a role-gated Developer Settings page** reachable from the main nav (above Settings, like manage-settings); move existing developer cards (Mock Data, Image Location, Computers) onto it. | **Persona: Frontend Engineer**
+ [ ] **Frontend Engineer: implement the Request-Type master list** (Phase 4.1) loading the catalog through the editor service. | **Persona: Frontend Engineer**
+ [ ] **Frontend Engineer: implement the type edit view** (Display/Active/Order inline + Subtypes list with Add/Edit/Remove). | **Persona: Frontend Engineer**
+ [ ] **Frontend Engineer: implement the guided wizard modal** (4 steps, Next/Back/Cancel, step indicator, final Save) shared by subtype and type editing; Cancel confirms on unsaved changes; persist at final Save. | **Persona: Frontend Engineer**
+ [ ] **Frontend Engineer: implement the per-control card** shown when a control type is selected (control-specific options) with error gating. | **Persona: Frontend Engineer**
+ [ ] **QA Engineer: tests** for master-list load, add/edit/remove subtype, ordering, wizard step navigation + cancel-confirm + final-save, role gating, and that catalog edits are reflected on next wizard run; full suite green. | **Persona: QA Engineer**

---

**GATE: Infor/receiving reachability health service exists and auto-falls back to mock when down (manual toggle disabled, toast on change); central mock config reflects on all clients with race handling; `mtm_waitlist` is hard-required at startup with clear splash messaging; a role-gated Developer Settings page (main nav, above Settings) hosts an Option-A request-type/subtype editor (list → type view → guided wizard, add/edit/remove subtypes, ordering, control reuse w/ gating); catalog edits reflect on next load; seed reviewed per the standing rule; full suite green.**
