# Discovery 01 — Current Mock/Sample Logic Inventory (MTM_Waitlist)

> Read-only discovery, 2026-09-09. Source: repo exploration. Feeds the Module_Mock removal + rebuild.

## Executive: TWO separate "mock" families (do not conflate)
- **A. In-app sample / short-circuit system** — legacy dev/demo source driven by `Feature.InforVisualMockData`
  / `Feature.RecvMockData` toggles. This is the system being **replaced**. Lives in `SampleDataService`,
  `Sample*Catalog` classes, the helper-server `mockAction`/`backendAction` branching, and the auto-force routing stack.
- **B. DB-backed mock master-tables system (already DB-backed)** — Developer-editable CRUD over real MySQL
  tables in `mtm_waitlist`: `mock_parts`, `mock_work_orders`, `mock_work_centers`, `mock_locations`,
  `mock_inventory_locations`, `mock_requesters`, `mock_request_types`, `mock_master_tables_registry`, via
  `sp_mock_*` SPs + `IMockMasterDataService`/`MockMasterDataService`. **No production UI consumer found**
  (DI + tests only). Architectural decision: reuse/migrate this or supersede/remove it.

## 1. In-app sample / catalog services (family A — removal candidates)
All are hardcoded C# data providers (no DB reads). Paths abbreviated from repo root.
| File | What it returns | Consumer | Toggle-gated? |
|---|---|---|---|
| `MTM_Waitlist.Waitlist.View/Services/SampleDataService.cs` | display-only `SampleOrder` rows by building | `SqlHelperServer`/`MySqlHelperServer`, Waitlist VMs, `IWaitlistRequestService` | Yes (both keys) |
| `MTM_Waitlist.Core/Contracts/Services/ISampleDataService.cs` | `GetSampleOrders(string?)` contract | injected broadly | — |
| `MTM_Waitlist.Waitlist.View/Services/SampleWaitlistRequestCatalog.cs` | full-lifecycle `WaitlistRequest`s | tests only today | (InforVisualMockData) |
| `MTM_Waitlist.Waitlist.View/Services/SampleInventoryLocationCatalog.cs` | per-part inventory rows | `WaitlistInventoryService` | (InforVisualMockData) |
| `MTM_Waitlist.Waitlist.View/Services/SampleAverageCoilWeightCatalog.cs` | avg skid weight (`MMC0001000`=5000) | `AverageCoilWeightService` | (RecvMockData) |
| `MTM_Waitlist.Waitlist.NewRequest/Services/SampleJobCoilCatalog.cs` | `WaitlistCoilInfo` has-coil/weight | `CoilAvailabilityService` | (InforVisualMockData) |
| `MTM_Waitlist.Waitlist.View/Models/SampleOrder.cs` | list/card + detail display model | Waitlist VMs/selectors/controls | n/a (model) |
| `MTM_Waitlist.Setup/Services/SetupDataCatalog.cs` | sample parts/sequences/subordinates/dunnage | Setup mock actions + `SaveMockAsync` | (RecvMockData) |
| `MTM_Waitlist.Waitlist.NewRequest/Models/WaitlistCoilInfo.cs` | model surfaced when InforVisualMockData ON | `CoilAvailabilityService` | — |

## 2. Mock toggles / settings
- Keys (source of truth `MTM_Waitlist.Core/Services/MockSettingKeys.cs`):
  `ConnectionSource.InforVisual → "Feature.InforVisualMockData"`, `ConnectionSource.Receiving → "Feature.RecvMockData"`.
  **No `Feature.UseMockData` key exists.**
- `MockToggleService` (`MasterKeyName = Feature.InforVisualMockData`, `MirrorKeyName = Feature.RecvMockData`)
  writes BOTH keys equal; `GetEffectiveAsync` reads master only.
- Raw-key readers bypassing the service (constants duplicated ~6 files): `SqlHelperServer.cs` (7,19,34),
  `MySqlHelperServer.cs` (18,335–343), `WaitlistRequestService.cs` (12–13 + `IsMockDataEnabled()` 834 ORs both),
  `AverageCoilWeightService.cs` (12,34), `CoilAvailabilityService.cs` (9,22), `SampleDataService.cs` (10–11,57).
- Settings UI: `Module_Settings/Views/SettingsPage.xaml` ~179–239 (`MockDataExpander`,
  `ToggleSwitch IsOn="{x:Bind ViewModel.UseMockData}"`); `SettingsViewModel.cs` `UseMockData` (78), loads
  via `_mockToggleService.GetEffectiveAsync` (262), mirrors via `SetAsync` (265), `OnUseMockDataChanged` (300),
  `IsMockDataPanelVisible` (162). `LocalSettingsService.cs` persists to ApplicationData.LocalSettings.

## 3. Mock routing / auto-force stack (Core) — all singletons DI at `Services/DependencyInjection/ServiceRegistrationExtensions.cs:110–121`
`MockRoutingService`, `MockRoutingCoordinator`, `MockConfigurationService` (central DB setting via
`sp_config_settings_get_effective`/`_upsert`), `MockRoutingRefreshService`, `MockRoutingMonitorService`,
`MockModePollingHost`, `MockFallbackDebouncer`, `MockModeChangeDetector` (static), `MockModeSummaryProvider`
(static), `MockSettingKeys`, `MockToggleService`, `DispatcherPollScheduler` (app, UI-thread DispatcherTimer 30s),
`MockModeToastCoordinator` (app, MSIX-only toasts). Contracts under `MTM_Waitlist.Core/Contracts/Services/`,
models under `MTM_Waitlist.Core/Models/` (`MockRoutingDecision`, `MockModeChange`, `ConnectionHealthState` with
`ConnectionSource` InforVisual/Receiving). Wired from `Module_Core/Views/ShellPage.xaml.cs` (21,31–35,52);
probed by `ConnectionHealthService.cs`.

## 4. Helper-server short-circuit routing
- `SqlHelperServer.cs` (InforVisual key): `ExecuteReadOnlyQueueAsync(queueName,param)` (17) returns
  `_sampleDataService.GetSampleOrders(param).Take(3)` when mock; `ExecuteReadOnlyQueueAsync<T>(...,mock,backend)` (28)
  = the Infor Visual short-circuit.
- `MySqlHelperServer.cs` (RecvMockData key; `MySqlDatabaseTarget` MtmWaitlist/MtmReceivingApplication/MtmWipApplication):
  `ExecuteReadWriteAsync` (47/58/67); `IsMockDataEnabledAsync(target)` (335–343) **reads `Feature.RecvMockData`
  for EVERY target incl. MtmWaitlist**.
- **Mock-gating of the app's own `mtm_waitlist`:** `SetupPersistenceService.SaveAsync` (63) uses default
  `MtmWaitlist` target → setup saves are mock-gated by `Feature.RecvMockData`; `SaveMockAsync` (200) = no-op
  success stub. **THIS is the setup-save-doesn't-update root cause.**
- Callers: `SetupLookupService.cs` (27/50/68, InforVisual via `*FromMockAsync`); `DunnageWorkflowService.cs`
  (35/46/57, Receiving via `SetupDataCatalog`); `WaitlistInventoryService.cs` (41, InforVisual);
  `WaitlistRequestService.cs` (`IsMockDataEnabled()` 834 **skips `sp_waitlist_request_list` read (47) and ALL DB
  persists** 274/392/597/789 for accept/note/status/cancel/audit) — mock-gates the app's own request DB;
  `AverageCoilWeightService.cs` (34, Receiving); `CoilAvailabilityService.cs` (22, InforVisual, live source not
  wired → "coil assumed available").
- `RequestTypeCatalogService.cs` reads real `mtm_waitlist` SPs — NOT mock-gated (DB replaced JSON for the wizard).

## 5. Mock DB artifacts (family B)
- Tables `Database/Tables/`: `27_mock_request_types`, `26_mock_inventory_locations`, `25_mock_requesters`,
  `24_mock_locations`, `23_mock_work_centers`, plus `mock_parts`, `mock_work_orders`, `mock_master_tables_registry`
  (in `AllTables.sql` ~612, `update_table_descriptions.sql` ~381).
- SPs `Database/StoredProcedures/sp_mock_*`: `sp_mock_inventory_locations_*`, `sp_mock_locations_*`,
  `sp_mock_parts_*`, `sp_mock_request_types_*`, `sp_mock_requesters_*`, `sp_mock_work_centers_*`,
  `sp_mock_work_orders_*`, `sp_mock_master_tables_registry_*`, `sp_mock_master_table_columns_get`
  (aggregated in `AllSPs.sql` ~841+).
- Seeds `Database/Seeds/seed_mock_master_default/` + `AllSeeds.sql`; `MTMReceivingApp/Seeds/seed_receiving_history_coil_weights/`.
- C# service `MockMasterDataService.cs` maps `table_name → sp_mock_*_get/insert/update/delete` (112–138);
  `MockMasterRowEditValidator.cs`; DI `ServiceRegistrationExtensions.cs:108`.

## 6. Mock JSON / config assets
- `Assets/Config/waitlist-request-types.json` — request-type/subtype defs + image-override GUIDs; consumed by
  `ImageLocationService.cs` (772) + models; tests assert it. Real wizard catalog now from DB SPs.
- `Assets/RequestTypeImages/*.png` (die-handling, flatstock, forklift-assist, other, pickup, table-handling);
  default `Assets/Images/default-request-type.png`.
- Sample images at `Assets/` root: coil.png, pickup_fg/os/ncm/wip.png, scrap.png + empty-state/default-workstation.

## 7. DI wiring
- Root `Services/DependencyInjection/ServiceRegistrationExtensions.cs`: ISampleDataService (96), SqlHelperServer
  (98), MySqlHelperServer (100–102), IMockMasterDataService (108), mock routing stack (110–121).
- Module extensions: Setup (`SetupLookupService`, `DunnageWorkflowService`, `SetupPersistenceService`,
  `SetupWorkflowService`), Waitlist.View (`WaitlistRequestService`, `WaitlistInventoryService`,
  `AverageCoilWeightService`), Waitlist.NewRequest (`NewRequestFlowService`, `RequestTypeCatalogService`,
  `CoilAvailabilityService`), Core module services.

## 8. Tests exercising mock behavior (rework/removal candidates)
- Core: `MockModeQaTests`, `MockConfigurationServiceTests`, `MockFallbackDebouncerTests`,
  `MockModeChangeDetectorTests`, `MockModePollingHostTests`, `MockModeSummaryProviderTests`,
  `MockRoutingCoordinatorTests`, `MockRoutingMonitorServiceTests`, `MockRoutingRefreshServiceTests`,
  `MockRoutingServiceTests`, `MockToggleServiceTests`, `HelperServersTests`, `SampleDataServiceTests`,
  `MockMasterDataServiceTests`, `MockMasterRowEditValidatorTests`, `Core/Models/SampleModelsTests`.
- Waitlist: `SampleWaitlistRequestCatalogTests`, `SampleJobCoilCatalogTests`, `UrgencyMockParityTests`,
  `WaitlistInventoryServiceTests`, `AverageCoilWeightServiceTests`, `CoilAvailabilityServiceTests`,
  `WaitlistRequestServiceTests`, `InventoryLocationFilteringTests`, `RequestTypeCatalogServiceIntegrationTests`,
  `SampleOrderTests`, `WaitlistViewDetailInventoryTests`, `WaitlistViewDetailViewModelTests`.
- Setup: `SetupPersistenceServiceTests`, `SetupWorkflowServiceTests`, `SetupDunnageWorkflowServiceTests`,
  `ActiveJobItemResolverServiceTests`, `ActiveJobReadBackSpIntegrationTests`.
- Shared/Settings doubles: `Module_Settings/TestDoubles.cs` `FakeSampleDataService` (192), `SettingsViewModelTests`.

## Cross-cutting notes
1. Two toggle keys kept in lockstep by `MockToggleService`, but many services read raw keys directly; a single
   source of truth was never fully adopted. `WaitlistRequestService.IsMockDataEnabled` ORs both.
2. Family B (`mock_*` tables + `MockMasterDataService`) is already DB-backed but unwired to a production UI —
   decide reuse vs supersede.
3. Biggest risk: `WaitlistRequestService` mock mode currently skips ALL `mtm_waitlist` request reads+writes;
   under "mtm_waitlist always live" this gating must be removed so the request lifecycle always persists.
4. `RequestTypeCatalogService` reads real DB SPs while `ImageLocationService` still consumes the legacy JSON for
   request-type card images — two competing request-type sources to reconcile.
