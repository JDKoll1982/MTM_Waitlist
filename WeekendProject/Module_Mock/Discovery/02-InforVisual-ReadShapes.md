# Discovery 02 — Infor Visual Read Shapes (mirror targets for mtm_mock)

> Read-only discovery, 2026-09-09. Infor Visual = SQL Server `VISUAL`, DB `MTMFG`. Only reads (app never writes).
> These 5 read shapes define the `mtm_mock.visual_*_result` mirror tables.

## Executors (SQL Server, CommandType.Text, never throw → empty on failure)
- Core: `MTM_Waitlist.Core/Services/InforVisualSqlQueryService.cs` — `ExecuteQueueAsync` L35, scripts under
  `Database/InforVisual/Queues/Module_Waitlist/Queries/` via `WaitlistInforVisualSqlScriptStore`.
- Setup: `MTM_Waitlist.Setup/Services/InforVisualSqlQueryService.cs` — `ExecuteQueueAsync` L23, scripts under
  `Database/InforVisual/Queues/Module_Setup/Queries/` via `SetupSqlScriptStore`.
- `SqlHelperServer` is NOT a real executor — it is the mock/backend router (gated on `Feature.InforVisualMockData`);
  when mock OFF the `backendAction` reaches `InforVisualSqlQueryService`.
- Other `SqlConnection` uses (`ConnectionHealthService.cs:120`, `ExternalConnectionInfoProvider.cs`) are
  reachability probes only, not reads.

## The 5 read shapes → mirror tables

### Feature: Setup — active job / sequence / subordinate parts
Caller: `MTM_Waitlist.Setup/Services/SetupLookupService.cs` (`IInforVisualLookupService`, `ISubordinatePartService`);
each routes via `SqlHelperServer.ExecuteReadOnlyQueueAsync` → backend → Setup executor.
Consumers: `MTM_Waitlist.Setup/Services/SetupWorkflowService.cs` (lookup :80, sequences :154, subordinate :193).
Work-center CARDS/active-job read `mtm_waitlist` (`setup_active_jobs`, `setup_work_centers_catalog`) — NOT Infor.

| C# caller:line | SQL script | Inputs | Outputs | Mirror table |
|---|---|---|---|---|
| `SetupLookupService.cs:123` LookupWorkOrderFromBackendAsync | `Module_Setup/Queries/LookupWorkOrder.sql` | NormalizedWorkOrder | PartNumber, Description, WorkCenter | `visual_work_order_lookup_result` |
| `SetupLookupService.cs:158` GetSequencesFromBackendAsync | `Module_Setup/Queries/GetSequences.sql` | NormalizedWorkOrder, PartNumber | SequenceNumber, Description | `visual_operation_sequences_result` |
| `SetupLookupService.cs:186` GetSubordinatePartsFromBackendAsync | `Module_Setup/Queries/GetSubordinateParts.sql` | NormalizedWorkOrder, PartNumber, SequenceNumber | Category, PartNumber, Description, Location, User8, OnHandQuantity | `visual_subordinate_parts_result` |

### Feature: Waitlist — detail inventory (location) grid
Caller: `MTM_Waitlist.Waitlist.View/Services/WaitlistInventoryService.cs` (Core executor); applies on-hand ≥1 +
ignored-locations filter afterward.

| C# caller:line | SQL script | Inputs | Outputs | Mirror table |
|---|---|---|---|---|
| `WaitlistInventoryService.cs:71` GetInventoryLocationsFromBackendAsync | `Module_Waitlist/Queries/GetInventoryLocations.sql` | PartNumber | PartNumber, Location, OnHandQuantity | `visual_inventory_locations_result` |

### Feature: Request disposition (FG/WIP/Outside)
Caller: `MTM_Waitlist.Settings/Services/RequestDispositionResolver.cs` (Core executor + MySQL floor snapshot);
pure classification in `RequestDispositionMapper.cs`/`RequestDispositionClassifier.cs`. Status codes source of
truth `RequestDispositionStatusCodes.cs` (Open={R,U,F}, Closed={C}, X never FG).

| C# caller:line | SQL script | Inputs | Outputs | Mirror table |
|---|---|---|---|---|
| `RequestDispositionResolver.cs:45` GetDispositionInputAsync | `Module_Waitlist/Queries/GetDispositionInput.sql` | WorkOrder, PartNumber | WorkOrderStatus, OpenWorkOrderQuantity, FinishedGoodsQuantity, HasOutsideVendorOperation | `visual_disposition_input_result` |

## IMPORTANT scope note — WIP floor / FG stock is NOT Infor Visual
Real floor FG/WIP/location stock comes from the **MTM WIP MySQL DB** (`mtm_wip_application_winforms`) via
`MTM_Waitlist.Core/Services/WipFloorInventoryService.cs` → `Database/MTMWipApp/Queues/Module_Waitlist/Queues/GetWipFloorQuantities.sql`
(snapshot `WipFloorQuantitySnapshot.cs`). Per the decision "don't mock WIP/Receiving (always live)", this stays
**live** and gets NO mirror. Only Infor Visual reads fall back.

## Connection configuration (Infor Visual)
- `appsettings.json` → `InforVisualDatabaseOptions` (lines 20–26): Server=VISUAL, Database=MTMFG, User=SHOP2.
- Env overrides: `INFOR_VISUAL_SQL_CONNECTION_STRING`; `INFOR_VISUAL_SQL_SERVER/_DATABASE/_USER/_PASSWORD`.
- Reachability: `Services/ExternalConnectionInfoProvider.cs`, `ConnectionHealthService.cs` (`ConnectionSource.InforVisual`).
- DI: Core executor `ServiceRegistrationExtensions.cs:99`; Setup executor `Setup/.../ModuleDependencyInjectionExtensions.cs:14`.

## Existing related infrastructure
`mock_inventory_locations` (family B, mtm_waitlist) closely parallels `GetInventoryLocations`. The new
`mtm_mock.visual_*_result` tables are a fresh per-read-shape mirroring layer and do not exist yet.
