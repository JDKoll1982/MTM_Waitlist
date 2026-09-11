# Module_Mock — what shipped (2026-09-11)

This folder holds the Module_Mock workstream documents. This file is the **shipped record**: it says what the
code and database actually contain now, and which of the sibling documents describe the world as it was before.

Authoritative requirements and design live in `specs/001-module-mock-visual-fallback/` (`spec.md`, `plan.md`,
`data-model.md`, `research.md`, `contracts/`, `quickstart.md`, `tasks.md`). Where a document here and a document
there disagree, **the spec folder wins**.

## Shipped architecture

| Piece | What it is | Where |
|---|---|---|
| Mirror cache | The `mtm_mock` database: 5 `visual_*_result` tables + 5 `_stage` twins, 10 `sp_visual_*` procedures, baseline seed rows | `Database/Mock/` |
| In-app fallback | One `IVisualReadFallback<TRequest,TRow>` per read shape, plus the shared shape catalog and the classified live-read executor | `MTM_Waitlist.Mock/` |
| Status surface | Non-interactive `InfoBar` in the existing shell, bound to `IReadStatusProvider` | `Module_Mock/Views/ReadStatusIndicator.xaml`, `Module_Core/Views/ShellPage.xaml` |
| On-host service | Tray-only service: scheduled refresh, token-gated API, per-store backups, host-only restore | `MTM_Waitlist.Mock.Service/` (see its `README.md`) |
| Readiness check | The ordered six-step playbook for adding a read shape | `Spec.md` §11, `Plan.md` §7, `contracts/mock-service-configuration.md` §4 |

## Rules that are now enforced by tests

- **No demo/mock mode.** `MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs` fails the build if any retired
  sample/demo type, setting key, or `sp_mock_*` object is present or referenced.
- **No inline SQL.** `MTM_Waitlist.Tests/Module_Mock/InlineSqlAuditTests.cs` fails the build if MySQL statement text
  appears in application code, or if a raw-SQL seam is used by a file that is not on its reviewed allowlist.
- **Cached matches live.** `MTM_Waitlist.Tests/Module_Mock/VisualReadFallbackParityTests.cs` asserts the mirror and
  the live read have the same column set and order, that an unreachable source is served from the mirror, and that a
  reachable-but-empty live result is **not** replaced by cached data.
- **One cycle at a time.** `MTM_Waitlist.Tests/Module_Mock_Service/RefreshCycleGateTests.cs` asserts a racing refresh
  request is refused rather than overlapped, on both the scheduled and the on-demand path.
- **Internal stores are never cached.** The cache is consulted only for the five Infor Visual read shapes; no
  internal-store read can reach it.

## Removal record (what was deleted, by family)

**Family A — in-app sample / short-circuit.** Deleted: `ISampleDataService`, `SampleDataService`,
`SampleWaitlistRequestCatalog`, `SampleInventoryLocationCatalog`, `SampleAverageCoilWeightCatalog`,
`SampleJobCoilCatalog`, `SetupDataCatalog`, the `Feature.InforVisualMockData` / `Feature.RecvMockData` keys,
`MockSettingKeys`, `MockToggleService`, the Settings "Mock Data" expander and switch, and every helper-server
`mockAction` / `backendAction` branch.

**Family A′ — routing / auto-force / monitoring.** Deleted: `MockRoutingService`, `MockRoutingCoordinator`,
`MockRoutingRefreshService`, `MockRoutingMonitorService`, `MockConfigurationService`, `MockModePollingHost`,
`MockModeChangeDetector`, `MockModeSummaryProvider`, `MockModeToastCoordinator`, `MockFallbackDebouncer`,
`IPollScheduler`, `DispatcherPollScheduler`, and the app-side `Services/MockMode/**`.

**Family B — DB-backed demo data.** Deleted: the `mock_master_tables_registry`, `mock_work_orders`,
`mock_work_centers`, `mock_locations`, `mock_requesters`, `mock_inventory_locations`, `mock_request_types` tables
and `mock_parts`; the 33 `sp_mock_*` procedures; the `seed_mock_master_default` seed; and
`MockMasterDataService` with its validator and models.

Each retired table/procedure artifact kept its `rollback.sql` (the matching drop artifact) and lost its
`create.sql`, so a DBA can still promote the removal. The retired objects are listed in
`Database/Bootstrap/update_table_descriptions.sql` and the blocks were removed from `AllTables.sql`,
`AllSPs.sql`, and `AllSeeds.sql`.

**Deliberately retained (not sample data).** `SampleOrder` is the waitlist list/card/detail display model
populated from real requests, and `WaitlistCoilInfo` is the coil DTO returned by `ICoilAvailabilityService`.
Deleting them would have rewritten the whole waitlist card UI for no behavioural gain.

## These documents are the historical record

- `Module_Mock-Planning-Progress.md` — the planning conversation as it happened.
- `Discovery/01-MockLogic-Inventory.md`, `Discovery/02-InforVisual-ReadShapes.md`,
  `Discovery/03-Hardcoded-MySQL-Sql.md` — read-only inventories of the code as it was **before** the removal.
  They name the retired types deliberately: they are the record of what was removed and why. They also document
  the sites that were converted to stored procedures. Do not treat them as a description of current code.
- `PromptFiles/*.md` — archived workstream checklists from the earlier phases. References there to the mock toggle
  or the sample catalogs describe work done at the time; the toggles and catalogs no longer exist.
- `Spec.md` / `Plan.md` / `Tasks.md` — the plan, kept as written, with a shipped-status note where the worker chose
  a different mechanism than the plan assumed (see `Tasks.md` Phase 1's playbook note and the spec folder's
  `plan.md` → Complexity Tracking).

## Where the deleted systems used to be reachable from

The removal was validated the same way `RetiredSymbolAuditTests` validates it today: by scanning production code
and live database artifacts for the retired names. If a future change reintroduces one of them, that test fails
the build rather than a reviewer having to notice.
