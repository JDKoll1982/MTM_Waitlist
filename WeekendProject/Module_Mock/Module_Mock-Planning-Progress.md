# Module_Mock — Planning Progress (working notes)

> **Status:** In design/planning. Not implemented. This is a scratch doc capturing converged
> decisions + open questions as we plan **from the ground up**. The formal artifacts (`Spec.md`,
> `Plan.md`, `Tasks.md`) will be authored once the shape is locked.
> **Last updated:** 2026-09-09 (collaborative planning session).

---

## 1. Problem being solved

- **Original bug:** In `MTM_Waitlist.Setup/Services/SetupPersistenceService.SaveAsync`, the setup save is
  routed through `MySqlHelperServer.ExecuteReadWriteAsync(..., mockAction: SaveMockAsync, ...)`. When the
  MySQL/mock toggle is ON it calls `SaveMockAsync()`, which returns `Success = true` but writes **nothing**.
  Meanwhile the work-center card's "Current Job / Part Number" is populated by
  `WorkCenterCatalogService.GetCatalogAsync`, which reads the **live** `mtm_waitlist` tables
  (`setup_work_centers_catalog` + `sp_setup_active_jobs_latest_by_work_center_get`) and is **not** mock-aware.
  Net effect: mock-mode setup save reports success but no data lands where the card reads → card stays "None."
- **Root cause:** the mock short-circuit is applied to the app's **own** `MtmWaitlist` DB target. The app's own
  store should never be mocked.

## 2. Converged architecture (narrowed through Q&A)

- **Only Infor Visual is mocked.** The WIP app, Receiving app, and `mtm_waitlist` all share one MySQL server
  and are effectively always up → they are **always live, never mocked**.
- **`mtm_waitlist` is the app's own live store, always** — setup saves, `waitlist_requests`, work-center
  catalog, defect types all write/read it for real regardless of mock state. **This fixes the save bug.**
- **Infor Visual is read-only to the app** (it runs SQL *lookup* queue scripts; the app never writes to it), so
  mock-for-Visual is purely a **read** concern.
- **Mock for Visual = a real-data read cache.** It holds a mirror of the Visual data the app reads. The app
  switches to the cache when it **detects Visual is unreachable**; the Settings "Mock Data" toggle remains as a
  manual dev override (frontend-facing item is retained).
- **Cache location:** dedicated **`mtm_mock`** MySQL DB on the same MySQL server, namespaced `visual_*`.

## 3. Mirror shape & refresh (locked)

- **Mirror shape:** *flattened per-read result tables*. Each `mtm_mock.visual_*` table stores the result rows of
  a specific app read (same columns the live query returns); a mock read = `SELECT` from that table.
- **Refresh:** pull real data from live Visual when it is reachable (startup + a manual "Refresh mirror"
  action in the app) **and** a scheduled warm refresh.
- **Seed:** committed baseline seed rows so the mirror works on a fresh machine before any live refresh.

## 4. NEW: lightweight DB service app (requested during planning)

A separate lightweight app that runs **on/near the MySQL server** and provides:
- The **scheduled warm refresh** of `mtm_mock` (pull Visual → mirror) — the "doer" of the scheduled refresh.
- A **settings frontend** (e.g. how often it updates / other tunables).
- **Periodic backups of all four MySQL DBs** (`mtm_wip_application_winforms`, `mtm_receiving_application`,
  `mtm_waitlist`, `mtm_mock`).
- **Restore a DB from a backup** (full drop → restore for simplicity; emergency use only).
- No privilege/role control needed — it runs on the server where only a few people have access.

## 4b. NEW decisions (round 2, 2026-09-09)

- **Service app tech:** a *mix* — a WinUI 3 settings/control frontend **plus** a headless background
  worker/service engine. Research (Context7 + MS Learn) pending to pick the best hosting/architecture route.
- **Service app runs on the MySQL host** (Windows box hosting MySQL, with network access to Infor Visual
  for refresh).
- **Refresh ownership split:** the MTM_Waitlist app does **no** startup/manual refresh. The service app's
  scheduled warm refresh is authoritative. The waitlist app may **call an API on the service app to force a
  refresh** when needed.
- **Manual Settings "Mock Data" toggle is REMOVED** — fallback to the mirror is **fully automatic** based on
  Visual reachability. (Open nuance: replace the toggle with an automatic status *indicator*? see Q&A.)

## 4c. Service-app architecture (researched recommendation, 2026-09-09)

Grounded in MS Learn ("Run code in the background"; "Create Windows Service using BackgroundService";
"Host ASP.NET Core in a Windows Service"): for headless always-on work without MSIX, **.NET Worker
Services** is the recommended route; a Worker can run as a **Windows Service** (`AddWindowsService`) and
can self-host an **ASP.NET Core minimal API (Kestrel)** alongside its hosted background services.

**Recommended = two cooperating pieces:**
1. **Windows Service engine (headless, always-on)** — .NET Worker (ASP.NET Core `WebApplication`) running
   on the MySQL host as a Windows Service. Owns:
   - the **scheduled warm refresh** (Infor Visual → `mtm_mock`),
   - **periodic backups** of the 4 MySQL DBs (via `mysqldump`),
   - **restore** (full drop → restore; emergency),
   - a **Kestrel HTTP API** (`POST /api/refresh/visual`, `GET /api/status`, backup/restore endpoints),
   - config in `appsettings.json` (refresh interval, backup schedule/retention/location).
2. **WinUI 3 control/settings app** — frontend only; talks to the service over HTTP (localhost) to view/edit
   settings, trigger refresh, and manage backups/restore.

**MTM_Waitlist app role:** does **no** refresh; reads `mtm_mock` as the fallback when Infor Visual is
unreachable; may `POST /api/refresh` to the service to force a refresh on demand.

## 4d. Round-3 decisions (2026-09-09)

- **Service app = SINGLE WinUI 3 app that does both** (settings/control UI **and** the background engine —
  scheduled refresh + backups/restore + local HTTP API) started from its own entry. NOT a separate Windows
  service + control app. Implication: it must stay running/logged-in on the MySQL host to do scheduled work.
- **MTM_Waitlist app:** read-only auto **status indicator** when "Infor Visual unreachable — using mirror".
  No manual toggle.
- **Still planning** — more questions before full discovery.

## 4e. Round-4 decisions (2026-09-09)

- **Service runtime:** auto-start on logon, minimized to system tray, keeps background engine + API alive.
- **API reachability:** MTM_Waitlist clients are **mostly on the network** (not the MySQL host). The service
  app can run from the actual server if needed; its refresh/status API must be **network-reachable**, gated by
  a simple shared token (no heavy privilege control).
- **Backups:** all 4 MySQL DBs (wip, receiving, waitlist, mock), **configurable per-DB** (schedule + retention).
- **Behavior change confirmed:** drop today's sample-catalog mock behavior; **real DBs always** (New Request
  offerings, waitlist board, coil reads). Only Infor Visual reads fall back to the `mtm_mock` mirror.

## 4f. Round-5 decisions (2026-09-09)

- **Project layout:** new class library **`MTM_Waitlist.Mock`** (mirror read-fallback client + reachability/
  status) referenced by app modules, **plus** new WinUI 3 service app **`MTM_Waitlist.Mock.Service`**
  (background engine + settings UI + API). Follows repo per-module-library + app-Views convention.
- **Reachability detection:** build **new** Visual→mirror fallback detection (do NOT reuse existing
  `MockRoutingService` auto-force). The old `MockRoutingService`/`MockToggleService`/`MockMode*`/sample-catalog
  stack is therefore a **removal** candidate (confirm in discovery/removal).

## 4g. Round-6 decisions (2026-09-09)

- **Mirror naming:** tables `mtm_mock.visual_<readShape>_result`; procs `sp_visual_<readShape>_{get,refresh}`.
- **Refresh semantics (user priority: low server strain, fast, NO client lag):** build each snapshot into a
  staging table then **atomically swap** (RENAME) into place — readers always see a complete old snapshot until
  the instant swap; avoids empty/partial reads and long locks.
- **Visual backup:** MySQL 4 only (wip, receiving, waitlist, mock). Infor Visual (SQL Server) is **not** backed
  up by this tool (read-only to us, separate system).

## 8. Discovery results (2026-09-09) — see `Discovery/` folder for full detail

- **`Discovery/01-MockLogic-Inventory.md`** — every current mock/sample artifact (sample catalogs, toggles,
  routing stack, helper-server short-circuits, mock DB tables/SPs, JSON assets, DI wiring, tests).
- **`Discovery/02-InforVisual-ReadShapes.md`** — the **5** Infor Visual read shapes (with C# caller:line, SQL
  script, inputs, outputs, suggested mirror table).
- **`Discovery/03-Hardcoded-MySQL-Sql.md`** — all inline/hard-coded MySQL SQL with file:line + whether an
  existing SP covers it or a new one is needed.

### Key discovery findings that affect the plan
1. **Only 5 Visual read shapes** need mirroring: `visual_work_order_lookup_result`,
   `visual_operation_sequences_result`, `visual_subordinate_parts_result`,
   `visual_inventory_locations_result`, `visual_disposition_input_result`.
   WIP-floor stock (`GetWipFloorQuantities.sql`) is a **different** source (`mtm_wip_application_winforms`) →
   stays live (not mocked).
2. **A second, already-DB-backed "mock" family (B)** exists: `mock_parts/_work_orders/_work_centers/_locations/
   _inventory_locations/_requesters/_request_types` + `mock_master_tables_registry` and `sp_mock_*` +
   `IMockMasterDataService`/`MockMasterDataService` — in `mtm_waitlist`, **no production UI consumer**.
   Must decide: reuse/migrate vs supersede/remove.
3. **`WaitlistRequestService` mock mode skips ALL `mtm_waitlist` request reads AND writes** (`IsMockDataEnabled()`
   ORs both keys; skips `sp_waitlist_request_list` + every persist). Under "mtm_waitlist always live" this
   gating must be removed so the request lifecycle always persists.
4. **`SetupPersistenceService.SaveAsync` mock-gates a write to the app's own DB** via the default `MtmWaitlist`
   target → the setup-save bug. `MySqlHelperServer.IsMockDataEnabledAsync` reads `Feature.RecvMockData` for
   every target incl. `MtmWaitlist`.
5. **Hard-coded MySQL SQL exists widely** (esp. Startup auth/registry, Settings image overrides, Shared dunnage
   visibility) and several callers duplicate existing SPs (`WorkCenterCatalogService` hot-WCs;
   `AverageCoilWeightService` → unused `sp_receiving_history_average_coil_weight`). Plan must convert these to
   SPs (existing or new). See `Discovery/03-...md`.
6. **`CoilAvailabilityService` live source is not wired** (returns "coil assumed available" when mock off) —
   a "reads sample when it should read real data" gap.
7. **Two competing request-type sources** to reconcile: DB SPs (`RequestTypeCatalogService`) vs legacy
   `Assets/Config/waitlist-request-types.json` (`ImageLocationService`).

## 9. Newly-surfaced decisions (RESOLVED 2026-09-09)

- D1 → **REMOVE/SUPERSEDE family B** (`mock_*` tables + `sp_mock_*` + `MockMasterDataService`) as part of cleanup.
- D2 → **REMOVE the `WaitlistRequestService` mock gating**; request reads/writes always persist to `mtm_waitlist`.
- D3 → **Mirror scope = the 5 Visual read shapes.** PLUS: the plan must include **detailed instructions for
  adding more shapes later** (Spec §11 / Plan §7 playbook + Tasks Phase 7 doc task).
- D4 → **Convert ALL hard-coded MySQL SQL** to SPs (existing where one fits, else new).

## 10. Documents authored (2026-09-09)

- `Spec.md` — specification (purpose, architecture, `mtm_mock` data model, in-app lib, service app, removal
  scope, SP-first policy, extensibility §11, behavior changes).
- `Plan.md` — phased plan (0–8), dependencies, risks/mitigations, rollout, and the "add a new read shape" playbook §7.
- `Tasks.md` — execution checklist (required repo format: phases → subphases → persona-tagged tasks, refs,
  gates, next-task pointers).
- `Discovery/01..03` — persisted discovery reports.

**Status:** ready to execute `Tasks.md` (or continue refining the docs on request).

## 7. Consolidated end-to-end design (draft — for review)

1. **Always-live (never mocked):** `mtm_waitlist`, `mtm_wip_application_winforms`,
   `mtm_receiving_application`. New Request offerings, waitlist board, setup saves, defect types all read/write
   `mtm_waitlist` for real. (Removes the `MtmWaitlist` mock short-circuit → fixes the setup-save bug.)
2. **`mtm_mock`** (same MySQL server) holds flattened `visual_<readShape>_result` mirror tables + SPs.
3. **`MTM_Waitlist.Mock`** lib (in app): new Visual-reachability detector (auto → fallback), mirror read
   services (read `mtm_mock` when Visual is down), read-only status indicator.
4. **`MTM_Waitlist.Mock.Service`** WinUI app (MySQL host, auto-start→tray): scheduled warm refresh
   (Visual→`mtm_mock`, staging+atomic swap), periodic per-DB backups (all 4 MySQL, configurable), emergency
   restore (drop→restore), network/token-gated API (`/refresh`, `/status`, backup/restore) for clients.
5. **Removal:** today's sample catalogs, `Feature.InforVisualMockData`/`RecvMockData` keys + Settings toggle,
   `MockRoutingService`/`MockToggleService`/`MockMode*` stack, `mock_request_types` table + in-app mock
   short-circuits. Replaced by the above. (Frontend mock *toggle* removed; replaced by the auto status indicator.)

## 5. Current mock logic that exists today (to be inventoried & reconciled in discovery)

- App-data mock toggles: `Feature.InforVisualMockData` (master/visual), `Feature.RecvMockData` (receiving/MySQL).
- Helper servers that short-circuit: `SqlHelperServer` (Visual reads, `InforVisualMockData`),
  `MySqlHelperServer` (`RecvMockData`, incl. the `MtmWaitlist` target that wrongly gates setup save).
- Mock routing / auto-force / toast: `MockRoutingService`, `MockRoutingRefreshService`, `MockToggleService`,
  `MockModeChangeDetector`, `MockModePollingHost`, `MockModeToastCoordinator`, `MockSettingKeys`.
- In-app sample catalogs (dev/demo sources): `SampleDataService`, `SampleWaitlistRequestCatalog`,
  `SampleInventoryLocationCatalog`, `SampleJobCoilCatalog`, `SampleAverageCoilWeightCatalog`, etc.
- DB artifacts: `mock_request_types` table + `27_mock_request_types`; Infor Visual queue SQL under
  `Database/InforVisual/Queues/**`; receiving/WIP SQL under `Database/MTMReceivingApp/**`, `Database/MTMWipApp/**`.
- To be **fully inventoried** in the discovery step (all mock tables, SPs, sample JSON/config, DI wiring,
  sample-service short-circuits) before the removal step.

## 6. Next questions (pending)

1. ~~Standalone service app tech~~ -> mix of WinUI UI + headless worker; **research best route (Context7/MS Learn)**.
2. ~~Boundary~~ -> waitlist app does no refresh; service warm-refresh authoritative; app calls service API to force refresh.
3. ~~Manual toggle~~ -> **removed; fallback fully automatic.** Open nuance: status *indicator* in app UI.
4. Catalog of Visual reads to mirror (drives discovery + mirror schema).
5. Whether `MtmWaitlist` mock gating removal affects anything that legitimately wanted waitlist reads mocked.
6. Service app backup/restore: schedule, retention, backup location, which DBs, restore safety.
7. How the service app talks to Infor Visual (SQL Server) for refresh — connection/config on the MySQL host.
8. Discovery step: enumerate all current mock logic + the app's actual Visual read query shapes.
