# Discovery 03 — Hard-coded / Inline MySQL SQL (file:line) → route to SP or create one

> Read-only discovery, 2026-09-09. Inline SQL string literals embedded in C# (via
> `IMySqlHelperServer.ExecuteSqlQueryAsync/ExecuteSqlNonQueryAsync`, raw `MySqlCommand`, or `CommandType.Text`).
> Goal per user: for each, either find the relevant SP or create one; add to the plan with file + line number.
> Paths abbreviated from repo root.

## A. Inline SQL → mtm_waitlist (no/partial SP coverage)
**`MTM_Waitlist.Startup/Services/ComputerRegistryService.cs`** — CRUD on `core_computers_registry`, no SP. New SP candidates.
- L21–39 SELECT lookup by computer_name+mac (LookupComputerAsync)
- L44–62 SELECT lookup by mac, latest (LookupByMac)
- L70–119 INSERT ... ON DUPLICATE KEY UPDATE (UpsertComputer)
- L124–150 UPDATE by mac (UpdateByMac)
- L153–178 SELECT all ordered by display_name (GetAll)
- L180–208 UPDATE by id (UpdateComputer)
- L211–216 DELETE by id (DeleteComputer)

**`MTM_Waitlist.Startup/Services/StartupSessionRepository.cs`** — direct text SQL on auth/core tables. New SP candidates.
- L41 `SELECT fn_server_utc_now();`
- L135–151 credentials check (CheckCredentials)
- L215–224 update password (UpdatePassword)
- L242–249 read computer registered (ReadComputerRegistered)
- L265–278 read user row (ReadUserRow)
- L301–310 read session expiry (ReadSessionExpiry)

**`MTM_Waitlist.Settings/Services/ConfigSettingsValueService.cs`** — `config_settings_values`.
- L38–66 SELECT by (setting_key, scope_key) (GetSettingValueAsync) — ⚠️ `sp_config_settings_get_effective` exists but semantics may differ
- L142–148 DELETE by (key,scope) (DeleteSettingValueAsync) — new delete SP candidate (upsert already uses `sp_config_settings_upsert`)

**`MTM_Waitlist.Settings/Services/ImageLocationService.cs`** — `setup_work_centers_catalog`.
- L650–674 dynamic SELECT ... WHERE work_center_name IN (...) (LoadWorkCenterDetailsAsync) — ⚠️ partial: `sp_setup_work_centers_get_all`

**`MTM_Waitlist.Settings/Services/ImageOverrideReadService.cs`** — `config_images_locations`, no SPs. New SP candidates.
- L66–97 GetOverride; L133–161 GetOverridesByScope; L186 CountAllActive; L229 CountByScope; L265 DetectOrphaned;
  L324–351 GetByPublicId; L379–401 GetRecentlyUpdated (interpolated limit); L476–481 validate work center exists

**`MTM_Waitlist.Settings/Services/ImageOverrideWriteService.cs`** — `config_images_locations`, no SPs. New SP candidates.
- L101–123 dupe pre-check; L144–180 Create; L254–274 Reactivate; L361–382 Update; L447–467 soft Delete;
  L530–552 DeleteByPublicId; L613–628 PurgeInactive; L658–679 DeactivateAllForScope

**`MTM_Waitlist.Shared/Services/DunnageTypeVisibilityCatalogService.cs`** — `config_dunnage_types_visibility`.
- L52 SELECT visibility; L85 DELETE all; L120–139 dynamic multi-row INSERT (VALUES (UUID(), ...)) — new SP candidates

**`MTM_Waitlist.Shared/Services/WorkCenterCatalogService.cs`** — mixes inline + direct MySqlCommand.
- L41–48 computers (GetAvailableComputers); L91–113 hot WCs read (overlaps `sp_config_hot_workcenters_get_for_workstation`);
  L196–206 workstation resolve; L217–220 catalog lookup; **L272–279 direct DELETE hot WCs (→ `sp_config_hot_workcenters_delete_for_workstation`)**;
  **L284–333 direct multi-row INSERT ... ON DUPLICATE KEY UPDATE (→ `sp_config_hot_workcenters_upsert`)**;
  L342–350 available WCs (→ `sp_setup_work_centers_get_all`); L362–372 resolve computer name

## B. Inline SQL → mtm_receiving_application
**`MTM_Waitlist.Waitlist.View/Services/AverageCoilWeightService.cs`** — top inline-SQL replace candidate.
- L14 `AverageWeightQuery = SELECT ROUND(AVG(quantity),0) AS AverageWeight FROM receiving_history WHERE part_id = @partId` (exec L45).
  **Existing SP already covers it (authored, UNUSED):** `Database/MTMReceivingApp/StoredProcedures/sp_receiving_history_average_coil_weight.sql`. → route to that SP.

**`MTM_Waitlist.Shared/Services/DunnageTypeVisibilityCatalogService.cs`**
- L139 `SELECT id, type_name FROM dunnage_types ...` (MtmReceivingApplication) — no local SP artifact (external `sp_Dunnage_Types_GetAll` exists)

## C. mtm_wip_application_winforms — no inline SQL found
- `MTM_Waitlist.Core/Services/WipFloorInventoryService.cs` L37–47 loads `.sql` FILE (`GetWipFloorQuantities.sql`) from disk — file load, not inline literal. (Stays live; not mocked.)

## D. `.sql` queue/script file loads (not inline — noted)
- Infor Visual (SQL Server): Core `InforVisualSqlQueryService.cs` L41/56/65; Setup `InforVisualSqlQueryService.cs` L29;
  `RequestDispositionResolver.cs` (GetDispositionInput.sql + an SP).
- Receiving queue scripts: `DunnageWorkflowService.cs` L176/L230/L282 load `Database/MTMReceivingApp/Queues/Module_Setup/Queues/*.sql`.
- WIP: `WipFloorInventoryService.cs` L37.
- SP-wrapper warm-up file loads (executed as SPs): `DunnageWorkflowService.cs` L87/L144; `SetupPersistenceService.cs` L219.

## E. SP names called in C# that may lack a matching local artifact
- `DunnageWorkflowService.cs`: `sp_Dunnage_Types_Insert` L90, `sp_Dunnage_Parts_Insert` L147,
  `sp_Dunnage_Types_GetAll` L181, `sp_Dunnage_Parts_GetByType` L239, `sp_Dunnage_Parts_GetAll` L285/L330
  — all `MtmReceivingApplication`; naming mismatch to verify vs local wrapper files `sp_setup_dunnage_*`.
- Verify folder-vs-body name mismatches for `sp_config_hot_workcenters_*_for_workstation`,
  `sp_setup_workstations_*` (folder name ≠ procedure name inside).

## Highest-value replace candidates
| Inline SQL | Target | Existing SP |
|---|---|---|
| `AverageCoilWeightService.cs` L14 | receiving | ✅ `sp_receiving_history_average_coil_weight` (authored, unused) |
| `WorkCenterCatalogService.cs` L272–333 | waitlist | ✅ `sp_config_hot_workcenters_delete_for_workstation` + `_upsert` |
| `WorkCenterCatalogService.cs` L342 | waitlist | ✅ `sp_setup_work_centers_get_all` |
| `WorkCenterCatalogService.cs` L91 | waitlist | ✅ `sp_config_hot_workcenters_get_for_workstation` |
| `ConfigSettingsValueService.cs` L38 | waitlist | ⚠️ `sp_config_settings_get_effective` |
| `ImageLocationService.cs` L671 | waitlist | ⚠️ `sp_setup_work_centers_get_all` (partial) |
| ComputerRegistryService, StartupSessionRepository, ImageOverrideRead/Write, DunnageTypeVisibility | waitlist | ❌ none → new SP candidates |
