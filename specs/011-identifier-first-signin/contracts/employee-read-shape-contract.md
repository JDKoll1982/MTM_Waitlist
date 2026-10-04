# Contract: The Sixth Plant-System Read Shape

## 1. Identity

| Property | Value |
|---|---|
| Key | `employee_by_number` |
| Module | the launch/session module, folder `Module_Startup` |
| Live source script | `Database/InforVisual/Queues/Module_Startup/Queries/GetEmployeeByNumber.sql` |
| Population script | `Database/InforVisual/Queues/Module_Mock/Populations/employee_by_number_population.sql` |

## 2. Input and output

| Parameter | Type | Required |
|---|---|---|
| `EmployeeNumber` | `nvarchar(32)` | yes |

| Column | Type |
|---|---|
| `EmployeeNumber` | `nvarchar(32)` |
| `FirstName` | `nvarchar(128)` |
| `LastName` | `nvarchar(128)` |

## 3. Artifacts (each with `create.sql` and `rollback.sql`)

- `Database/Mock/Tables/visual_employee_by_number_result/`
- `Database/Mock/Tables/visual_employee_by_number_result_stage/`
  (No `..._result_prev/` artifact: that name is transient inside the refresh procedure's single three-name
  rename, exactly as it is for the other five shapes. A `_prev` table that exists before a refresh makes the
  swap fail with "table already exists", so shipping one as an artifact makes the shape unrefreshable.)
- `Database/Mock/StoredProcedures/sp_visual_employee_by_number_get/`
- `Database/Mock/StoredProcedures/sp_visual_employee_by_number_refresh/`
- Aggregates regenerated: `Database/Mock/AllTables.sql`, `Database/Mock/AllSPs.sql`,
  `Database/Mock/Bootstrap/update_table_descriptions.sql`.

## 4. Catalog and service

- `MTM_Waitlist.Mock/Services/VisualReadShapeCatalog.cs` gains the sixth entry.
- `VisualReadShapeModule` gains the launch module value.
- `MTM_Waitlist.Mock/Services/VisualEmployeeByNumberFallback.cs` is the in-app fallback.
- The service's refresh engine is catalog-driven, so it needs no change beyond the new entry; startup validation
  proves the artifacts exist.

## 5. Counts to update deliberately

- The test that asserts the catalog count changes five to six.
- `Database/Database-Ruleset.md` changes "five" to "six".
- `VisualReadRouteAuditTests` and `ShapeAdditionNoDowntimeTests` are checked and updated with the count.

## 6. Fallback rule

The shape resolves from the mirror only when the reachability probe's settled verdict is `Cached` or the live
attempt finds the source unreachable. A reachable-but-empty live result is a real answer and is never replaced by
mirrored data.
