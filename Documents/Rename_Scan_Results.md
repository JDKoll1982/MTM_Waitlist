# Rename Scan Results - workstation -> Computer / Work Center

Generated: 2026-08-24 06:27:36 -05:00
Repo: C:\Users\johnk\source\repos\MTM_Waitlist
Pattern: `workstation` / `Work Station` (case-insensitive)
Total matches: 952 in 126 files
Files/folders to rename: 17

Excluded (generated/binary/SCM): bin, obj, .git, .vs, node_modules, TestResults, packages, .serena, pri_dump; binary exts: .png, .jpg, .jpeg, .gif, .webp, .ico, .dll, .exe, .pdb, .pri, .dgspec, .snupkg, .nupkg, .db, .bdb, .dat, .ttf, .otf; log/PRI/tool files: .log + Log.md, testout.txt, testerr.txt, pri_dump.xml, scan_workstation_rename.ps1, Rename_Scan_Results.md

> Category is a heuristic. Items marked **Review** need a human decision (computer vs work center).

## Summary by file

| File | Count | Categories |
|---|---|---|
| `.github\copilot-instructions.md` | 1 | Work Center |
| `.github\instructions\database-schema-rules.instructions.md` | 2 | Computer, Review |
| `.github\scripts\restructure-database-layout.ps1` | 5 | Computer, Review |
| `.github\scripts\Test-DeploymentPreflight.ps1` | 3 | Computer, Review |
| `checklist.md` | 3 | Review, Work Center |
| `Database\Bootstrap\update_table_descriptions.sql` | 31 | Computer, Review, Work Center |
| `Database\Database-Ruleset.md` | 7 | Computer, Review |
| `Database\Functions\AllFunct.sql` | 6 | Review, Work Center |
| `Database\Functions\fn_config_settings_scope_rank\create.sql` | 1 | Review |
| `Database\Functions\fn_setup_workstation_name_normalized\create.sql` | 5 | Work Center |
| `Database\Functions\fn_setup_workstation_name_normalized\rollback.sql` | 2 | Work Center |
| `Database\Seeds\AllSeeds.sql` | 8 | Computer, Review, Work Center |
| `Database\Seeds\seed_dev_masked_baseline\create.sql` | 4 | Computer, Review |
| `Database\Seeds\seed_dev_masked_baseline\rollback.sql` | 2 | Computer, Review |
| `Database\Seeds\seed_setup_workstations_default\create.sql` | 5 | Work Center |
| `Database\Seeds\seed_setup_workstations_default\rollback.sql` | 3 | Work Center |
| `Database\StoredProcedures\AllSPs.sql` | 62 | Computer, Review, Work Center |
| `Database\StoredProcedures\sp_config_hot_workcenters_delete_for_workstation\create.sql` | 6 | Work Center |
| `Database\StoredProcedures\sp_config_hot_workcenters_delete_for_workstation\rollback.sql` | 2 | Work Center |
| `Database\StoredProcedures\sp_config_hot_workcenters_get_for_workstation\create.sql` | 11 | Work Center |
| `Database\StoredProcedures\sp_config_hot_workcenters_get_for_workstation\rollback.sql` | 2 | Work Center |
| `Database\StoredProcedures\sp_config_hot_workcenters_upsert\create.sql` | 7 | Work Center |
| `Database\StoredProcedures\sp_config_settings_get_effective\create.sql` | 3 | Review |
| `Database\StoredProcedures\sp_config_settings_upsert\create.sql` | 4 | Review |
| `Database\StoredProcedures\sp_setup_save_setup\create.sql` | 1 | Review |
| `Database\StoredProcedures\sp_setup_workstations_delete\create.sql` | 6 | Work Center |
| `Database\StoredProcedures\sp_setup_workstations_delete\rollback.sql` | 2 | Work Center |
| `Database\StoredProcedures\sp_setup_workstations_get_all\create.sql` | 6 | Work Center |
| `Database\StoredProcedures\sp_setup_workstations_get_all\rollback.sql` | 2 | Work Center |
| `Database\StoredProcedures\sp_setup_workstations_touch\create.sql` | 5 | Work Center |
| `Database\StoredProcedures\sp_setup_workstations_touch\rollback.sql` | 2 | Work Center |
| `Database\StoredProcedures\sp_setup_workstations_upsert\create.sql` | 10 | Work Center |
| `Database\StoredProcedures\sp_setup_workstations_upsert\rollback.sql` | 2 | Work Center |
| `Database\StoredProcedures\sp_waitlist_request_insert\create.sql` | 3 | Review |
| `Database\Tables\02_core_workstations_registry\create.sql` | 5 | Computer |
| `Database\Tables\02_core_workstations_registry\rollback.sql` | 2 | Computer |
| `Database\Tables\05_auth_sessions_tokens\create.sql` | 3 | Computer |
| `Database\Tables\08_config_settings_values\create.sql` | 3 | Computer |
| `Database\Tables\09_config_settings_history\create.sql` | 2 | Computer |
| `Database\Tables\13_setup_workstations_catalog\create.sql` | 7 | Work Center |
| `Database\Tables\13_setup_workstations_catalog\rollback.sql` | 2 | Work Center |
| `Database\Tables\14_config_workstation_hot_workcenters\create.sql` | 12 | Work Center |
| `Database\Tables\14_config_workstation_hot_workcenters\rollback.sql` | 2 | Work Center |
| `Database\Tables\18_waitlist_requests_queue\create.sql` | 1 | Review |
| `Database\Tables\AllTables.sql` | 33 | Computer, Review, Work Center |
| `Database\Validation\settings_schema\validate.sql` | 2 | Computer, Review |
| `Database\Validation\startup_schema\validate.sql` | 17 | Computer, Review |
| `Database\Views\AllViews.sql` | 6 | Review, Work Center |
| `Database\Views\vw_config_settings_scope_catalog\create.sql` | 1 | Review |
| `Database\Views\vw_setup_workstations_active\create.sql` | 5 | Work Center |
| `Database\Views\vw_setup_workstations_active\rollback.sql` | 2 | Work Center |
| `Documents\Complete-NewRequest_WorkCenter_Redesign_Checklist.md` | 27 | Review, Work Center |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 26 | Computer, Review, Work Center |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 27 | Computer, Review, Work Center |
| `Documents\Development\InforVisual\InforVisualGuide.md` | 6 | Review |
| `Documents\Development\InforVisual\Interaction_Guides\UI_Interaction_Guide.md` | 5 | Review |
| `Documents\Development\InforVisual\Interaction_Guides\VBScript_Guide.md` | 2 | Review |
| `Documents\Development\Waitlist\NewRequestFeature\RequiredWorkflow.md` | 2 | Review |
| `Documents\Weekend-User-Update-Email.md` | 1 | Review |
| `Image-Location-Settings-Spec.md` | 7 | Review, Work Center |
| `Module_Core\Services\DependencyInjection\ServiceRegistrationExtensions.cs` | 2 | Work Center |
| `Module_Core\Services\PageService.cs` | 1 | Work Center |
| `Module_Core\ViewModels\ShellViewModel.cs` | 4 | Review, Work Center |
| `Module_Core\Views\ShellPage.xaml` | 1 | Work Center |
| `Module_Settings\Models\ConfigSettingValue.cs` | 5 | Review |
| `Module_Settings\Models\ImageLocation.cs` | 5 | Review, Work Center |
| `Module_Settings\Models\ImageLocationDefaults.cs` | 1 | Review |
| `Module_Settings\Models\ImageLocationScope.cs` | 2 | Review, Work Center |
| `Module_Settings\Models\ImageOverride.cs` | 1 | Work Center |
| `Module_Settings\Models\ImageStorageOptions.cs` | 1 | Review |
| `Module_Settings\Models\WorkCenterInventory.cs` | 12 | Review, Work Center |
| `Module_Settings\Services\ConfigSettingsValueService.cs` | 3 | Review |
| `Module_Settings\Services\IImageLocationService.cs` | 2 | Work Center |
| `Module_Settings\Services\IImageOverrideReadService.cs` | 1 | Work Center |
| `Module_Settings\Services\ImageLocationService.cs` | 6 | Review, Work Center |
| `Module_Settings\Services\ImageOverrideReadService.cs` | 1 | Work Center |
| `Module_Settings\ViewModels\SettingsViewModel.cs` | 32 | Computer, Review |
| `Module_Settings\ViewModels\WorkCenterImagesDialogViewModel.cs` | 1 | Work Center |
| `Module_Settings\Views\SettingsPage.xaml` | 2 | Review |
| `Module_Setup\Contracts\Services\SetupContracts.cs` | 5 | Review, Work Center |
| `Module_Setup\Models\SetupModels.cs` | 12 | Review, Work Center |
| `Module_Setup\Services\DependencyInjection\ModuleDependencyInjectionExtensions.cs` | 1 | Work Center |
| `Module_Setup\Services\SetupPersistenceService.cs` | 2 | Work Center |
| `Module_Setup\Services\SetupWorkstationService.cs` | 28 | Work Center |
| `Module_Setup\ViewModels\SetupCompletionViewModel.cs` | 1 | Work Center |
| `Module_Setup\ViewModels\SetupWorkOrderViewModel.cs` | 2 | Review, Work Center |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 74 | Work Center |
| `Module_Setup\Views\SetupDunnageTypePage.xaml` | 1 | Review |
| `Module_Setup\Views\SetupWorkOrderPage.xaml` | 1 | Review |
| `Module_Setup\Views\SetupWorkstationPage.xaml` | 22 | Work Center |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 45 | Work Center |
| `Module_Shared\Models\WorkCenterCatalogResult.cs` | 1 | Review |
| `Module_Shared\Models\WorkCenterDetail.cs` | 1 | Work Center |
| `Module_Shared\Models\WorkCenterSelectionItem.cs` | 2 | Review, Work Center |
| `Module_Shared\Services\IWorkCenterCatalogService.cs` | 4 | Review |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 74 | Computer, Review, Work Center |
| `Module_Startup\Models\StartupSessionSnapshot.cs` | 2 | Computer |
| `Module_Startup\Models\StartupState.cs` | 2 | Computer |
| `Module_Startup\Services\StartupCoordinator.cs` | 7 | Computer |
| `Module_Startup\Services\StartupSessionRepository.cs` | 9 | Computer |
| `Module_Waitlist\Models\NewRequestFlowState.cs` | 1 | Review |
| `Module_Waitlist\Models\SampleOrder.cs` | 1 | Review |
| `Module_Waitlist\Models\WaitlistRequest.cs` | 1 | Review |
| `Module_Waitlist\Models\WaitlistRequestDraft.cs` | 1 | Review |
| `Module_Waitlist\Services\WaitlistRequestService.cs` | 5 | Review |
| `Module_Waitlist\ViewModels\NewRequestWorkCenterViewModel.cs` | 9 | Computer, Review |
| `Module_Waitlist\Views\NewRequestWorkCenterPage.xaml` | 1 | Review |
| `MTM_Waitlist.Tests\Models\StartupModelsTests.cs` | 1 | Review |
| `MTM_Waitlist.Tests\Module_Settings\ImageOverrideDialogViewModelTests.cs` | 4 | Review |
| `MTM_Waitlist.Tests\Module_Settings\TestDoubles.cs` | 5 | Review |
| `MTM_Waitlist.Tests\Module_Waitlist\Models\NewRequestFlowStateTests.cs` | 1 | Review |
| `MTM_Waitlist.Tests\Module_Waitlist\Services\WaitlistRequestServiceTests.cs` | 5 | Review |
| `MTM_Waitlist.Tests\Module_Waitlist\ViewModels\NewRequestWorkCenterViewModelTests.cs` | 6 | Review |
| `MTM_Waitlist.Tests\Services\StartupCoordinatorTests.cs` | 14 | Computer |
| `MTM_Waitlist.Tests\ViewModels\LoginViewModelTests.cs` | 2 | Review |
| `StartupPhases\Phase-01-Startup-Shell-and-Splash-Complete.md` | 1 | Review |
| `StartupPhases\Phase-03-Identity-and-Workstation-Checks.md` | 19 | Computer, Review |
| `StartupPhases\Phase-05-Session-Validation-and-Routing-Complete.md` | 3 | Review |
| `StartupPhases\Phase-09-Role-Enforcement-and-Final-Polish.md` | 3 | Review |
| `StartupPhases\README.md` | 13 | Computer, Review |
| `StartupPhases\StartupPhases-Prompt.md` | 1 | Computer |
| `Strings\en-us\Resources.resw` | 23 | Review |
| `Strings\en-us\TooltipResources.developer.resw` | 7 | Work Center |
| `Strings\en-us\TooltipResources.resw` | 9 | Review, Work Center |
| `User-Management-Clarifying-Questions.md` | 1 | Work Center |
| `WAITLIST_REQUEST_WORKFLOW_TASKS.md` | 4 | Review |

## Files / folders to rename

| Type | Path | Category | Suggested name |
|---|---|---|---|
| Folder | `Database\Functions\fn_setup_workstation_name_normalized` | Work Center | `fn_setup_work_center_name_normalized` |
| Folder | `Database\Seeds\seed_setup_workstations_default` | Work Center | `seed_setup_work_centers_default` |
| Folder | `Database\StoredProcedures\sp_config_hot_workcenters_delete_for_workstation` | Work Center | `sp_config_hot_workcenters_delete_for_work_center` |
| Folder | `Database\StoredProcedures\sp_config_hot_workcenters_get_for_workstation` | Work Center | `sp_config_hot_workcenters_get_for_work_center` |
| Folder | `Database\StoredProcedures\sp_setup_workstations_delete` | Work Center | `sp_setup_work_centers_delete` |
| Folder | `Database\StoredProcedures\sp_setup_workstations_get_all` | Work Center | `sp_setup_work_centers_get_all` |
| Folder | `Database\StoredProcedures\sp_setup_workstations_touch` | Work Center | `sp_setup_work_centers_touch` |
| Folder | `Database\StoredProcedures\sp_setup_workstations_upsert` | Work Center | `sp_setup_work_centers_upsert` |
| Folder | `Database\Tables\02_core_workstations_registry` | Computer | `02_core_computers_registry` |
| Folder | `Database\Tables\13_setup_workstations_catalog` | Work Center | `13_setup_work_centers_catalog` |
| Folder | `Database\Tables\14_config_workstation_hot_workcenters` | Work Center | `14_config_work_center_hot_workcenters` |
| Folder | `Database\Views\vw_setup_workstations_active` | Work Center | `vw_setup_work_centers_active` |
| File | `Module_Setup\Services\SetupWorkstationService.cs` | Work Center | `SetupWorkCenterService.cs` |
| File | `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | Work Center | `SetupWorkCenterViewModel.cs` |
| File | `Module_Setup\Views\SetupWorkstationPage.xaml` | Work Center | `SetupWorkCenterPage.xaml` |
| File | `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | Work Center | `SetupWorkCenterPage.xaml.cs` |
| File | `StartupPhases\Phase-03-Identity-and-Workstation-Checks.md` | Review | `Phase-03-Identity-and-work_center-Checks.md` |

## Detailed edit map

| File | Line | Category | Matched text |
|---|---|---|---|
| `checklist.md` | 28 | Work Center | - [x] **Data Model: Capture the work-center catalog identity and map each row to `setup_wo |
| `checklist.md` | 199 | Review | - [x] **Pipeline: Add a deployment check that confirms the image share is reachable from t |
| `checklist.md` | 358 | Review | **Tone Example:** *"I've added a pre-deployment gate that validates the share path is reac |
| `Image-Location-Settings-Spec.md` | 37 | Review | \| Work centers \| `Assets\Images\default-workstation-image.png` \| |
| `Image-Location-Settings-Spec.md` | 113 | Work Center | Scope: all active rows in `setup_workstations_catalog`, retrieved through the existing |
| `Image-Location-Settings-Spec.md` | 115 | Review | path. The list is unfiltered by workstation; sorting and filtering happen in the dialog. |
| `Image-Location-Settings-Spec.md` | 117 | Work Center | Row key: `setup_workstations_catalog.id`. |
| `Image-Location-Settings-Spec.md` | 118 | Review | Default when unresolved: `Assets\Images\default-workstation-image.png`. |
| `Image-Location-Settings-Spec.md` | 125 | Work Center | \| Row key \| `setup_workstations_catalog.id` \| |
| `Image-Location-Settings-Spec.md` | 207 | Review | image selected on one workstation resolves on every other workstation. The stored |
| `User-Management-Clarifying-Questions.md` | 111 | Work Center | 2. Refactor existing `Allowed*Roles` arrays (`SettingsViewModel`, `SetupWorkstationViewMod |
| `WAITLIST_REQUEST_WORKFLOW_TASKS.md` | 28 | Review | - Proof: [Module_Waitlist/Models/WaitlistRequestDraft.cs](Module_Waitlist/Models/WaitlistR |
| `WAITLIST_REQUEST_WORKFLOW_TASKS.md` | 31 | Review | - [x] Include the active setup-job identity, workstation name, and request timestamp in th |
| `WAITLIST_REQUEST_WORKFLOW_TASKS.md` | 32 | Review | - Proof: both model files include `ActiveSetupJobId`, `WorkstationName`, and `RequestedUtc |
| `WAITLIST_REQUEST_WORKFLOW_TASKS.md` | 51 | Review | - Proof: [Module_Waitlist/Services/WaitlistRequestService.cs](Module_Waitlist/Services/Wai |
| `.github\copilot-instructions.md` | 43 | Work Center | - Interaction notes (2026-08-22): Setup/New Request work center card selection is **model- |
| `.github\instructions\database-schema-rules.instructions.md` | 23 | Review | - Foreign key columns use custom relationship descriptors (for example, `user_id`, `employ |
| `.github\instructions\database-schema-rules.instructions.md` | 44 | Computer | - `core_workstations_registry` |
| `.github\scripts\restructure-database-layout.ps1` | 94 | Computer | 'core_workstations_registry', |
| `.github\scripts\restructure-database-layout.ps1` | 288 | Computer | core_workstations_registry ( |
| `.github\scripts\restructure-database-layout.ps1` | 290 | Review | workstation_name, |
| `.github\scripts\restructure-database-layout.ps1` | 307 | Review | workstation_name = VALUES(workstation_name), |
| `.github\scripts\restructure-database-layout.ps1` | 318 | Computer | DELETE FROM core_workstations_registry WHERE workstation_name = 'johnspc'; |
| `.github\scripts\Test-DeploymentPreflight.ps1` | 3 | Review | Verifies that a target workstation can reach everything the image-location feature needs. |
| `.github\scripts\Test-DeploymentPreflight.ps1` | 78 | Computer | Write-Host ("Workstation: {0}    User: {1}" -f $env:COMPUTERNAME, $env:USERNAME) |
| `.github\scripts\Test-DeploymentPreflight.ps1` | 200 | Review | -Detail ("'{0}' is not reachable from this workstation." -f $effectiveShare) |
| `Database\Database-Ruleset.md` | 18 | Review | - FK column format: relationship descriptor + `_id` (for example, `user_id`, `workstation_ |
| `Database\Database-Ruleset.md` | 39 | Computer | - `core_workstations_registry` |
| `Database\Database-Ruleset.md` | 50 | Review | - Workstation identity relies on normalized hostname + MAC fields. |
| `Database\Database-Ruleset.md` | 59 | Review | - Supported scope types are `workstation`, `all_users`, `user`, `admin`, and `developer`. |
| `Database\Database-Ruleset.md` | 60 | Review | - Scope resolution is ordered from fallback to override: workstation -> all_users -> user  |
| `Database\Database-Ruleset.md` | 61 | Review | - `scope_key` is required and is `workstation:<id>`, `all_users`, `user:<id>`, `admin`, or |
| `Database\Database-Ruleset.md` | 62 | Review | - Workstation and user scopes use `workstation_id` and `user_id` foreign keys respectively |
| `Database\Bootstrap\update_table_descriptions.sql` | 21 | Computer | ALTER TABLE core_workstations_registry COMMENT = 'Registered workstation and host identity |
| `Database\Bootstrap\update_table_descriptions.sql` | 23 | Computer | ALTER TABLE core_workstations_registry |
| `Database\Bootstrap\update_table_descriptions.sql` | 25 | Review | MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for workstation record.', |
| `Database\Bootstrap\update_table_descriptions.sql` | 26 | Computer | MODIFY COLUMN workstation_name VARCHAR(128) NOT NULL COMMENT 'Friendly workstation/compute |
| `Database\Bootstrap\update_table_descriptions.sql` | 28 | Review | MODIFY COLUMN mac_address_normalized VARCHAR(64) NOT NULL COMMENT 'Normalized MAC address  |
| `Database\Bootstrap\update_table_descriptions.sql` | 29 | Review | MODIFY COLUMN is_registered TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Whether the workstation |
| `Database\Bootstrap\update_table_descriptions.sql` | 59 | Computer | MODIFY COLUMN workstation_id BIGINT NULL COMMENT 'Optional foreign key to core_workstation |
| `Database\Bootstrap\update_table_descriptions.sql` | 101 | Review | MODIFY COLUMN scope_type VARCHAR(16) NOT NULL DEFAULT 'all_users' COMMENT 'Scope kind such |
| `Database\Bootstrap\update_table_descriptions.sql` | 103 | Review | MODIFY COLUMN workstation_id BIGINT NULL COMMENT 'Optional workstation scope foreign key.' |
| `Database\Bootstrap\update_table_descriptions.sql` | 123 | Review | MODIFY COLUMN workstation_id BIGINT NULL COMMENT 'Optional workstation scope foreign key s |
| `Database\Bootstrap\update_table_descriptions.sql` | 151 | Review | MODIFY COLUMN host_id VARCHAR(128) NULL COMMENT 'Host/workstation identifier captured for  |
| `Database\Bootstrap\update_table_descriptions.sql` | 195 | Work Center | ALTER TABLE setup_workstations_catalog COMMENT = 'Setup work center catalog used by setup  |
| `Database\Bootstrap\update_table_descriptions.sql` | 203 | Work Center | AND table_name = 'setup_workstations_catalog' |
| `Database\Bootstrap\update_table_descriptions.sql` | 210 | Work Center | 'ALTER TABLE setup_workstations_catalog ADD COLUMN building VARCHAR(64) NOT NULL DEFAULT ' |
| `Database\Bootstrap\update_table_descriptions.sql` | 211 | Work Center | 'SELECT ''setup_workstations_catalog.building already exists''' |
| `Database\Bootstrap\update_table_descriptions.sql` | 226 | Work Center | AND table_name = 'setup_workstations_catalog' |
| `Database\Bootstrap\update_table_descriptions.sql` | 227 | Work Center | AND index_name = 'uq_setup_workstations_catalog_workstation_name' |
| `Database\Bootstrap\update_table_descriptions.sql` | 233 | Work Center | 'ALTER TABLE setup_workstations_catalog DROP INDEX uq_setup_workstations_catalog_workstati |
| `Database\Bootstrap\update_table_descriptions.sql` | 234 | Work Center | 'SELECT ''uq_setup_workstations_catalog_workstation_name not present''' |
| `Database\Bootstrap\update_table_descriptions.sql` | 249 | Work Center | AND table_name = 'setup_workstations_catalog' |
| `Database\Bootstrap\update_table_descriptions.sql` | 250 | Work Center | AND index_name = 'uq_setup_workstations_catalog_building_workstation_name' |
| `Database\Bootstrap\update_table_descriptions.sql` | 256 | Work Center | 'ALTER TABLE setup_workstations_catalog ADD UNIQUE KEY uq_setup_workstations_catalog_build |
| `Database\Bootstrap\update_table_descriptions.sql` | 257 | Work Center | 'SELECT ''uq_setup_workstations_catalog_building_workstation_name already exists''' |
| `Database\Bootstrap\update_table_descriptions.sql` | 266 | Work Center | ALTER TABLE setup_workstations_catalog |
| `Database\Bootstrap\update_table_descriptions.sql` | 270 | Review | MODIFY COLUMN workstation_name VARCHAR(64) NOT NULL COMMENT 'Work center or press display  |
| `Database\Bootstrap\update_table_descriptions.sql` | 278 | Work Center | ALTER TABLE config_workstation_hot_workcenters COMMENT = 'Per-computer Local work center p |
| `Database\Bootstrap\update_table_descriptions.sql` | 280 | Work Center | ALTER TABLE config_workstation_hot_workcenters |
| `Database\Bootstrap\update_table_descriptions.sql` | 283 | Computer | MODIFY COLUMN core_workstation_id BIGINT NOT NULL COMMENT 'Foreign key to core_workstation |
| `Database\Bootstrap\update_table_descriptions.sql` | 284 | Work Center | MODIFY COLUMN setup_workstation_id BIGINT NOT NULL COMMENT 'Foreign key to setup_workstati |
| `Database\Bootstrap\update_table_descriptions.sql` | 285 | Review | MODIFY COLUMN sort_rank INT NOT NULL DEFAULT 100 COMMENT 'Display order for Local work cen |
| `Database\Bootstrap\update_table_descriptions.sql` | 347 | Review | MODIFY COLUMN workstation_name VARCHAR(64) NOT NULL COMMENT 'Workstation that submitted th |
| `Database\Functions\AllFunct.sql` | 12 | Review | WHEN 'workstation' THEN 1 |
| `Database\Functions\AllFunct.sql` | 35 | Work Center | -- Function: fn_setup_workstation_name_normalized |
| `Database\Functions\AllFunct.sql` | 40 | Work Center | DROP FUNCTION IF EXISTS fn_setup_workstation_name_normalized; |
| `Database\Functions\AllFunct.sql` | 42 | Work Center | CREATE FUNCTION fn_setup_workstation_name_normalized( |
| `Database\Functions\AllFunct.sql` | 43 | Review | p_workstation_name VARCHAR(64) |
| `Database\Functions\AllFunct.sql` | 47 | Review | RETURN TRIM(REPLACE(REPLACE(REPLACE(IFNULL(p_workstation_name, ''), '\r', ' '), '\n', ' ') |
| `Database\Functions\fn_config_settings_scope_rank\create.sql` | 12 | Review | WHEN 'workstation' THEN 1 |
| `Database\Functions\fn_setup_workstation_name_normalized\create.sql` | 1 | Work Center | -- Function: fn_setup_workstation_name_normalized |
| `Database\Functions\fn_setup_workstation_name_normalized\create.sql` | 6 | Work Center | DROP FUNCTION IF EXISTS fn_setup_workstation_name_normalized; |
| `Database\Functions\fn_setup_workstation_name_normalized\create.sql` | 8 | Work Center | CREATE FUNCTION fn_setup_workstation_name_normalized( |
| `Database\Functions\fn_setup_workstation_name_normalized\create.sql` | 9 | Work Center | p_workstation_name VARCHAR(64) |
| `Database\Functions\fn_setup_workstation_name_normalized\create.sql` | 13 | Work Center | RETURN TRIM(REPLACE(REPLACE(REPLACE(IFNULL(p_workstation_name, ''), '\r', ' '), '\n', ' ') |
| `Database\Functions\fn_setup_workstation_name_normalized\rollback.sql` | 1 | Work Center | -- Rollback function: fn_setup_workstation_name_normalized |
| `Database\Functions\fn_setup_workstation_name_normalized\rollback.sql` | 5 | Work Center | DROP FUNCTION IF EXISTS fn_setup_workstation_name_normalized; |
| `Database\Seeds\AllSeeds.sql` | 223 | Computer | TRUNCATE TABLE core_workstations_registry; |
| `Database\Seeds\AllSeeds.sql` | 226 | Computer | core_workstations_registry ( |
| `Database\Seeds\AllSeeds.sql` | 228 | Review | workstation_name, |
| `Database\Seeds\AllSeeds.sql` | 254 | Review | workstation_name = VALUES(workstation_name), |
| `Database\Seeds\AllSeeds.sql` | 257 | Work Center | -- Seed: seed_setup_workstations_default |
| `Database\Seeds\AllSeeds.sql` | 262 | Work Center | TRUNCATE TABLE setup_workstations_catalog; |
| `Database\Seeds\AllSeeds.sql` | 265 | Work Center | setup_workstations_catalog ( |
| `Database\Seeds\AllSeeds.sql` | 267 | Review | workstation_name, |
| `Database\Seeds\seed_dev_masked_baseline\create.sql` | 223 | Computer | TRUNCATE TABLE core_workstations_registry; |
| `Database\Seeds\seed_dev_masked_baseline\create.sql` | 226 | Computer | core_workstations_registry ( |
| `Database\Seeds\seed_dev_masked_baseline\create.sql` | 228 | Review | workstation_name, |
| `Database\Seeds\seed_dev_masked_baseline\create.sql` | 254 | Review | workstation_name = VALUES(workstation_name), |
| `Database\Seeds\seed_dev_masked_baseline\rollback.sql` | 14 | Computer | DELETE FROM core_workstations_registry |
| `Database\Seeds\seed_dev_masked_baseline\rollback.sql` | 16 | Review | workstation_name = 'johnspc'; |
| `Database\Seeds\seed_setup_workstations_default\create.sql` | 1 | Work Center | -- Seed: seed_setup_workstations_default |
| `Database\Seeds\seed_setup_workstations_default\create.sql` | 3 | Work Center | -- Source: live Visual SHOP_RESOURCE workstation list |
| `Database\Seeds\seed_setup_workstations_default\create.sql` | 9 | Work Center | TRUNCATE TABLE setup_workstations_catalog; |
| `Database\Seeds\seed_setup_workstations_default\create.sql` | 12 | Work Center | setup_workstations_catalog ( |
| `Database\Seeds\seed_setup_workstations_default\create.sql` | 14 | Work Center | workstation_name, |
| `Database\Seeds\seed_setup_workstations_default\rollback.sql` | 1 | Work Center | -- Rollback seed: seed_setup_workstations_default |
| `Database\Seeds\seed_setup_workstations_default\rollback.sql` | 5 | Work Center | DELETE FROM setup_workstations_catalog |
| `Database\Seeds\seed_setup_workstations_default\rollback.sql` | 7 | Work Center | workstation_name IN ( |
| `Database\StoredProcedures\AllSPs.sql` | 10 | Review | IN p_workstation_id BIGINT, |
| `Database\StoredProcedures\AllSPs.sql` | 17 | Review | workstation_id, |
| `Database\StoredProcedures\AllSPs.sql` | 31 | Review | (scope_type = 'workstation' AND workstation_id = p_workstation_id) |
| `Database\StoredProcedures\AllSPs.sql` | 49 | Review | IN p_workstation_id BIGINT, |
| `Database\StoredProcedures\AllSPs.sql` | 64 | Review | workstation_id, |
| `Database\StoredProcedures\AllSPs.sql` | 80 | Review | WHEN 'workstation' THEN CONCAT('workstation:', p_workstation_id) |
| `Database\StoredProcedures\AllSPs.sql` | 86 | Review | p_workstation_id, |
| `Database\StoredProcedures\AllSPs.sql` | 146 | Review | -- Purpose: Persist workstation setup state. |
| `Database\StoredProcedures\AllSPs.sql` | 371 | Work Center | -- Stored Procedure: sp_setup_workstations_delete |
| `Database\StoredProcedures\AllSPs.sql` | 376 | Work Center | DROP PROCEDURE IF EXISTS sp_setup_workstations_delete; |
| `Database\StoredProcedures\AllSPs.sql` | 378 | Work Center | CREATE PROCEDURE sp_setup_workstations_delete( |
| `Database\StoredProcedures\AllSPs.sql` | 379 | Review | IN p_workstation_id VARCHAR(32) |
| `Database\StoredProcedures\AllSPs.sql` | 381 | Work Center | DELETE FROM setup_workstations_catalog |
| `Database\StoredProcedures\AllSPs.sql` | 382 | Review | WHERE id = CAST(TRIM(p_workstation_id) AS UNSIGNED); |
| `Database\StoredProcedures\AllSPs.sql` | 384 | Work Center | -- Stored Procedure: sp_setup_workstations_get_all |
| `Database\StoredProcedures\AllSPs.sql` | 389 | Work Center | DROP PROCEDURE IF EXISTS sp_setup_workstations_get_all; |
| `Database\StoredProcedures\AllSPs.sql` | 391 | Work Center | CREATE PROCEDURE sp_setup_workstations_get_all() |
| `Database\StoredProcedures\AllSPs.sql` | 395 | Review | workstation_name, |
| `Database\StoredProcedures\AllSPs.sql` | 398 | Work Center | FROM vw_setup_workstations_active |
| `Database\StoredProcedures\AllSPs.sql` | 399 | Review | ORDER BY sort_rank ASC, workstation_name ASC; |
| `Database\StoredProcedures\AllSPs.sql` | 401 | Work Center | -- Stored Procedure: sp_setup_workstations_upsert |
| `Database\StoredProcedures\AllSPs.sql` | 406 | Work Center | DROP PROCEDURE IF EXISTS sp_setup_workstations_upsert; |
| `Database\StoredProcedures\AllSPs.sql` | 408 | Work Center | CREATE PROCEDURE sp_setup_workstations_upsert( |
| `Database\StoredProcedures\AllSPs.sql` | 409 | Review | IN p_workstation_id VARCHAR(32), |
| `Database\StoredProcedures\AllSPs.sql` | 411 | Review | IN p_workstation_name VARCHAR(64), |
| `Database\StoredProcedures\AllSPs.sql` | 414 | Work Center | INSERT INTO setup_workstations_catalog ( |
| `Database\StoredProcedures\AllSPs.sql` | 418 | Review | workstation_name, |
| `Database\StoredProcedures\AllSPs.sql` | 427 | Review | CAST(NULLIF(TRIM(p_workstation_id) COLLATE utf8mb4_unicode_ci, '') AS UNSIGNED), |
| `Database\StoredProcedures\AllSPs.sql` | 430 | Work Center | NULLIF(fn_setup_workstation_name_normalized(p_workstation_name) COLLATE utf8mb4_unicode_ci |
| `Database\StoredProcedures\AllSPs.sql` | 440 | Review | workstation_name = VALUES(workstation_name), |
| `Database\StoredProcedures\AllSPs.sql` | 444 | Work Center | -- Stored Procedure: sp_setup_workstations_touch |
| `Database\StoredProcedures\AllSPs.sql` | 450 | Work Center | DROP PROCEDURE IF EXISTS sp_setup_workstations_touch; |
| `Database\StoredProcedures\AllSPs.sql` | 452 | Work Center | CREATE PROCEDURE sp_setup_workstations_touch( |
| `Database\StoredProcedures\AllSPs.sql` | 456 | Work Center | UPDATE setup_workstations_catalog |
| `Database\StoredProcedures\AllSPs.sql` | 460 | Work Center | WHERE workstation_name = TRIM(p_work_center); |
| `Database\StoredProcedures\AllSPs.sql` | 462 | Work Center | -- Stored Procedure: sp_config_hot_workcenters_get_for_workstation |
| `Database\StoredProcedures\AllSPs.sql` | 467 | Work Center | DROP PROCEDURE IF EXISTS sp_config_hot_workcenters_get_for_workstation; |
| `Database\StoredProcedures\AllSPs.sql` | 469 | Work Center | CREATE PROCEDURE sp_config_hot_workcenters_get_for_workstation( |
| `Database\StoredProcedures\AllSPs.sql` | 470 | Review | IN p_workstation_name VARCHAR(128) |
| `Database\StoredProcedures\AllSPs.sql` | 473 | Work Center | swc.workstation_name AS work_center_name, |
| `Database\StoredProcedures\AllSPs.sql` | 475 | Work Center | FROM config_workstation_hot_workcenters cwhc |
| `Database\StoredProcedures\AllSPs.sql` | 476 | Computer | INNER JOIN core_workstations_registry cwr ON cwr.id = cwhc.core_workstation_id |
| `Database\StoredProcedures\AllSPs.sql` | 477 | Work Center | INNER JOIN setup_workstations_catalog swc ON swc.id = cwhc.setup_workstation_id |
| `Database\StoredProcedures\AllSPs.sql` | 481 | Review | cwr.workstation_name = TRIM(p_workstation_name) |
| `Database\StoredProcedures\AllSPs.sql` | 482 | Review | OR cwr.hostname_normalized = TRIM(p_workstation_name) |
| `Database\StoredProcedures\AllSPs.sql` | 484 | Review | ORDER BY cwhc.sort_rank ASC, swc.workstation_name ASC; |
| `Database\StoredProcedures\AllSPs.sql` | 486 | Work Center | -- Stored Procedure: sp_config_hot_workcenters_delete_for_workstation |
| `Database\StoredProcedures\AllSPs.sql` | 491 | Work Center | DROP PROCEDURE IF EXISTS sp_config_hot_workcenters_delete_for_workstation; |
| `Database\StoredProcedures\AllSPs.sql` | 493 | Work Center | CREATE PROCEDURE sp_config_hot_workcenters_delete_for_workstation( |
| `Database\StoredProcedures\AllSPs.sql` | 494 | Computer | IN p_core_workstation_id BIGINT |
| `Database\StoredProcedures\AllSPs.sql` | 496 | Work Center | DELETE FROM config_workstation_hot_workcenters |
| `Database\StoredProcedures\AllSPs.sql` | 497 | Computer | WHERE core_workstation_id = p_core_workstation_id; |
| `Database\StoredProcedures\AllSPs.sql` | 507 | Computer | IN p_core_workstation_id BIGINT, |
| `Database\StoredProcedures\AllSPs.sql` | 508 | Work Center | IN p_setup_workstation_id BIGINT, |
| `Database\StoredProcedures\AllSPs.sql` | 512 | Work Center | INSERT INTO config_workstation_hot_workcenters ( |
| `Database\StoredProcedures\AllSPs.sql` | 513 | Computer | core_workstation_id, |
| `Database\StoredProcedures\AllSPs.sql` | 514 | Work Center | setup_workstation_id, |
| `Database\StoredProcedures\AllSPs.sql` | 524 | Computer | p_core_workstation_id, |
| `Database\StoredProcedures\AllSPs.sql` | 525 | Work Center | p_setup_workstation_id, |
| `Database\StoredProcedures\AllSPs.sql` | 552 | Review | IN p_workstation_name VARCHAR(64), |
| `Database\StoredProcedures\AllSPs.sql` | 570 | Review | workstation_name, |
| `Database\StoredProcedures\AllSPs.sql` | 590 | Review | TRIM(p_workstation_name), |
| `Database\StoredProcedures\sp_config_hot_workcenters_delete_for_workstation\create.sql` | 1 | Work Center | -- Stored Procedure: sp_config_hot_workcenters_delete_for_workstation |
| `Database\StoredProcedures\sp_config_hot_workcenters_delete_for_workstation\create.sql` | 6 | Work Center | DROP PROCEDURE IF EXISTS sp_config_hot_workcenters_delete_for_workstation; |
| `Database\StoredProcedures\sp_config_hot_workcenters_delete_for_workstation\create.sql` | 8 | Work Center | CREATE PROCEDURE sp_config_hot_workcenters_delete_for_workstation( |
| `Database\StoredProcedures\sp_config_hot_workcenters_delete_for_workstation\create.sql` | 9 | Work Center | IN p_core_workstation_id BIGINT |
| `Database\StoredProcedures\sp_config_hot_workcenters_delete_for_workstation\create.sql` | 11 | Work Center | DELETE FROM config_workstation_hot_workcenters |
| `Database\StoredProcedures\sp_config_hot_workcenters_delete_for_workstation\create.sql` | 12 | Work Center | WHERE core_workstation_id = p_core_workstation_id; |
| `Database\StoredProcedures\sp_config_hot_workcenters_delete_for_workstation\rollback.sql` | 1 | Work Center | -- Rollback Stored Procedure: sp_config_hot_workcenters_delete_for_workstation |
| `Database\StoredProcedures\sp_config_hot_workcenters_delete_for_workstation\rollback.sql` | 5 | Work Center | DROP PROCEDURE IF EXISTS sp_config_hot_workcenters_delete_for_workstation; |
| `Database\StoredProcedures\sp_config_hot_workcenters_get_for_workstation\create.sql` | 1 | Work Center | -- Stored Procedure: sp_config_hot_workcenters_get_for_workstation |
| `Database\StoredProcedures\sp_config_hot_workcenters_get_for_workstation\create.sql` | 6 | Work Center | DROP PROCEDURE IF EXISTS sp_config_hot_workcenters_get_for_workstation; |
| `Database\StoredProcedures\sp_config_hot_workcenters_get_for_workstation\create.sql` | 8 | Work Center | CREATE PROCEDURE sp_config_hot_workcenters_get_for_workstation( |
| `Database\StoredProcedures\sp_config_hot_workcenters_get_for_workstation\create.sql` | 9 | Work Center | IN p_workstation_name VARCHAR(128) |
| `Database\StoredProcedures\sp_config_hot_workcenters_get_for_workstation\create.sql` | 12 | Work Center | swc.workstation_name AS work_center_name, |
| `Database\StoredProcedures\sp_config_hot_workcenters_get_for_workstation\create.sql` | 14 | Work Center | FROM config_workstation_hot_workcenters cwhc |
| `Database\StoredProcedures\sp_config_hot_workcenters_get_for_workstation\create.sql` | 15 | Work Center | INNER JOIN core_workstations_registry cwr ON cwr.id = cwhc.core_workstation_id |
| `Database\StoredProcedures\sp_config_hot_workcenters_get_for_workstation\create.sql` | 16 | Work Center | INNER JOIN setup_workstations_catalog swc ON swc.id = cwhc.setup_workstation_id |
| `Database\StoredProcedures\sp_config_hot_workcenters_get_for_workstation\create.sql` | 20 | Work Center | cwr.workstation_name = TRIM(p_workstation_name) |
| `Database\StoredProcedures\sp_config_hot_workcenters_get_for_workstation\create.sql` | 21 | Work Center | OR cwr.hostname_normalized = TRIM(p_workstation_name) |
| `Database\StoredProcedures\sp_config_hot_workcenters_get_for_workstation\create.sql` | 23 | Work Center | ORDER BY cwhc.sort_rank ASC, swc.workstation_name ASC; |
| `Database\StoredProcedures\sp_config_hot_workcenters_get_for_workstation\rollback.sql` | 1 | Work Center | -- Rollback Stored Procedure: sp_config_hot_workcenters_get_for_workstation |
| `Database\StoredProcedures\sp_config_hot_workcenters_get_for_workstation\rollback.sql` | 5 | Work Center | DROP PROCEDURE IF EXISTS sp_config_hot_workcenters_get_for_workstation; |
| `Database\StoredProcedures\sp_config_hot_workcenters_upsert\create.sql` | 9 | Work Center | IN p_core_workstation_id BIGINT, |
| `Database\StoredProcedures\sp_config_hot_workcenters_upsert\create.sql` | 10 | Work Center | IN p_setup_workstation_id BIGINT, |
| `Database\StoredProcedures\sp_config_hot_workcenters_upsert\create.sql` | 14 | Work Center | INSERT INTO config_workstation_hot_workcenters ( |
| `Database\StoredProcedures\sp_config_hot_workcenters_upsert\create.sql` | 15 | Work Center | core_workstation_id, |
| `Database\StoredProcedures\sp_config_hot_workcenters_upsert\create.sql` | 16 | Work Center | setup_workstation_id, |
| `Database\StoredProcedures\sp_config_hot_workcenters_upsert\create.sql` | 26 | Work Center | p_core_workstation_id, |
| `Database\StoredProcedures\sp_config_hot_workcenters_upsert\create.sql` | 27 | Work Center | p_setup_workstation_id, |
| `Database\StoredProcedures\sp_config_settings_get_effective\create.sql` | 10 | Review | IN p_workstation_id BIGINT, |
| `Database\StoredProcedures\sp_config_settings_get_effective\create.sql` | 17 | Review | workstation_id, |
| `Database\StoredProcedures\sp_config_settings_get_effective\create.sql` | 31 | Review | (scope_type = 'workstation' AND workstation_id = p_workstation_id) |
| `Database\StoredProcedures\sp_config_settings_upsert\create.sql` | 11 | Review | IN p_workstation_id BIGINT, |
| `Database\StoredProcedures\sp_config_settings_upsert\create.sql` | 26 | Review | workstation_id, |
| `Database\StoredProcedures\sp_config_settings_upsert\create.sql` | 42 | Review | WHEN 'workstation' THEN CONCAT('workstation:', p_workstation_id) |
| `Database\StoredProcedures\sp_config_settings_upsert\create.sql` | 48 | Review | p_workstation_id, |
| `Database\StoredProcedures\sp_setup_save_setup\create.sql` | 3 | Review | -- Purpose: Persist workstation setup state. |
| `Database\StoredProcedures\sp_setup_workstations_delete\create.sql` | 1 | Work Center | -- Stored Procedure: sp_setup_workstations_delete |
| `Database\StoredProcedures\sp_setup_workstations_delete\create.sql` | 6 | Work Center | DROP PROCEDURE IF EXISTS sp_setup_workstations_delete; |
| `Database\StoredProcedures\sp_setup_workstations_delete\create.sql` | 8 | Work Center | CREATE PROCEDURE sp_setup_workstations_delete( |
| `Database\StoredProcedures\sp_setup_workstations_delete\create.sql` | 9 | Work Center | IN p_workstation_id VARCHAR(32) |
| `Database\StoredProcedures\sp_setup_workstations_delete\create.sql` | 11 | Work Center | DELETE FROM setup_workstations_catalog |
| `Database\StoredProcedures\sp_setup_workstations_delete\create.sql` | 12 | Work Center | WHERE id = CAST(TRIM(p_workstation_id) AS UNSIGNED); |
| `Database\StoredProcedures\sp_setup_workstations_delete\rollback.sql` | 1 | Work Center | -- Rollback Stored Procedure: sp_setup_workstations_delete |
| `Database\StoredProcedures\sp_setup_workstations_delete\rollback.sql` | 5 | Work Center | DROP PROCEDURE IF EXISTS sp_setup_workstations_delete; |
| `Database\StoredProcedures\sp_setup_workstations_get_all\create.sql` | 1 | Work Center | -- Stored Procedure: sp_setup_workstations_get_all |
| `Database\StoredProcedures\sp_setup_workstations_get_all\create.sql` | 6 | Work Center | DROP PROCEDURE IF EXISTS sp_setup_workstations_get_all; |
| `Database\StoredProcedures\sp_setup_workstations_get_all\create.sql` | 8 | Work Center | CREATE PROCEDURE sp_setup_workstations_get_all() |
| `Database\StoredProcedures\sp_setup_workstations_get_all\create.sql` | 12 | Work Center | workstation_name, |
| `Database\StoredProcedures\sp_setup_workstations_get_all\create.sql` | 15 | Work Center | FROM vw_setup_workstations_active |
| `Database\StoredProcedures\sp_setup_workstations_get_all\create.sql` | 16 | Work Center | ORDER BY sort_rank ASC, workstation_name ASC; |
| `Database\StoredProcedures\sp_setup_workstations_get_all\rollback.sql` | 1 | Work Center | -- Rollback Stored Procedure: sp_setup_workstations_get_all |
| `Database\StoredProcedures\sp_setup_workstations_get_all\rollback.sql` | 5 | Work Center | DROP PROCEDURE IF EXISTS sp_setup_workstations_get_all; |
| `Database\StoredProcedures\sp_setup_workstations_touch\create.sql` | 1 | Work Center | -- Stored Procedure: sp_setup_workstations_touch |
| `Database\StoredProcedures\sp_setup_workstations_touch\create.sql` | 7 | Work Center | DROP PROCEDURE IF EXISTS sp_setup_workstations_touch; |
| `Database\StoredProcedures\sp_setup_workstations_touch\create.sql` | 9 | Work Center | CREATE PROCEDURE sp_setup_workstations_touch( |
| `Database\StoredProcedures\sp_setup_workstations_touch\create.sql` | 13 | Work Center | UPDATE setup_workstations_catalog |
| `Database\StoredProcedures\sp_setup_workstations_touch\create.sql` | 17 | Work Center | WHERE workstation_name = TRIM(p_work_center); |
| `Database\StoredProcedures\sp_setup_workstations_touch\rollback.sql` | 1 | Work Center | -- Rollback Stored Procedure: sp_setup_workstations_touch |
| `Database\StoredProcedures\sp_setup_workstations_touch\rollback.sql` | 6 | Work Center | DROP PROCEDURE IF EXISTS sp_setup_workstations_touch; |
| `Database\StoredProcedures\sp_setup_workstations_upsert\create.sql` | 1 | Work Center | -- Stored Procedure: sp_setup_workstations_upsert |
| `Database\StoredProcedures\sp_setup_workstations_upsert\create.sql` | 6 | Work Center | DROP PROCEDURE IF EXISTS sp_setup_workstations_upsert; |
| `Database\StoredProcedures\sp_setup_workstations_upsert\create.sql` | 8 | Work Center | CREATE PROCEDURE sp_setup_workstations_upsert( |
| `Database\StoredProcedures\sp_setup_workstations_upsert\create.sql` | 9 | Work Center | IN p_workstation_id VARCHAR(32), |
| `Database\StoredProcedures\sp_setup_workstations_upsert\create.sql` | 11 | Work Center | IN p_workstation_name VARCHAR(64), |
| `Database\StoredProcedures\sp_setup_workstations_upsert\create.sql` | 14 | Work Center | INSERT INTO setup_workstations_catalog ( |
| `Database\StoredProcedures\sp_setup_workstations_upsert\create.sql` | 18 | Work Center | workstation_name, |
| `Database\StoredProcedures\sp_setup_workstations_upsert\create.sql` | 27 | Work Center | CAST(NULLIF(TRIM(p_workstation_id) COLLATE utf8mb4_unicode_ci, '') AS UNSIGNED), |
| `Database\StoredProcedures\sp_setup_workstations_upsert\create.sql` | 30 | Work Center | NULLIF(fn_setup_workstation_name_normalized(p_workstation_name) COLLATE utf8mb4_unicode_ci |
| `Database\StoredProcedures\sp_setup_workstations_upsert\create.sql` | 40 | Work Center | workstation_name = VALUES(workstation_name), |
| `Database\StoredProcedures\sp_setup_workstations_upsert\rollback.sql` | 1 | Work Center | -- Rollback Stored Procedure: sp_setup_workstations_upsert |
| `Database\StoredProcedures\sp_setup_workstations_upsert\rollback.sql` | 5 | Work Center | DROP PROCEDURE IF EXISTS sp_setup_workstations_upsert; |
| `Database\StoredProcedures\sp_waitlist_request_insert\create.sql` | 15 | Review | IN p_workstation_name VARCHAR(64), |
| `Database\StoredProcedures\sp_waitlist_request_insert\create.sql` | 33 | Review | workstation_name, |
| `Database\StoredProcedures\sp_waitlist_request_insert\create.sql` | 53 | Review | TRIM(p_workstation_name), |
| `Database\Tables\AllTables.sql` | 29 | Computer | -- Create table: core_workstations_registry |
| `Database\Tables\AllTables.sql` | 38 | Computer | CREATE TABLE IF NOT EXISTS core_workstations_registry ( |
| `Database\Tables\AllTables.sql` | 41 | Review | workstation_name VARCHAR(128) NOT NULL, |
| `Database\Tables\AllTables.sql` | 48 | Computer | UNIQUE KEY uq_core_workstations_registry_public_id (public_id), |
| `Database\Tables\AllTables.sql` | 49 | Computer | UNIQUE KEY uq_core_workstations_registry_hostname_mac_address ( |
| `Database\Tables\AllTables.sql` | 121 | Review | workstation_id BIGINT NULL, |
| `Database\Tables\AllTables.sql` | 134 | Computer | KEY idx_auth_sessions_tokens_workstation_id_expires_utc (workstation_id, expires_utc), |
| `Database\Tables\AllTables.sql` | 137 | Computer | CONSTRAINT fk_sessions_tokens_workstations_workstation_id FOREIGN KEY (workstation_id) REF |
| `Database\Tables\AllTables.sql` | 206 | Review | workstation_id BIGINT NULL, |
| `Database\Tables\AllTables.sql` | 220 | Computer | KEY idx_config_settings_values_workstation_id (workstation_id), |
| `Database\Tables\AllTables.sql` | 223 | Computer | CONSTRAINT fk_values_ws_registry_workstation_id FOREIGN KEY (workstation_id) REFERENCES co |
| `Database\Tables\AllTables.sql` | 245 | Review | workstation_id BIGINT NULL, |
| `Database\Tables\AllTables.sql` | 274 | Computer | CONSTRAINT fk_history_ws_registry_workstation_id FOREIGN KEY (workstation_id) REFERENCES c |
| `Database\Tables\AllTables.sql` | 381 | Work Center | -- Create table: setup_workstations_catalog |
| `Database\Tables\AllTables.sql` | 390 | Work Center | CREATE TABLE IF NOT EXISTS setup_workstations_catalog ( |
| `Database\Tables\AllTables.sql` | 394 | Review | workstation_name VARCHAR(64) NOT NULL, |
| `Database\Tables\AllTables.sql` | 402 | Work Center | UNIQUE KEY uq_setup_workstations_catalog_public_id (public_id), |
| `Database\Tables\AllTables.sql` | 403 | Work Center | UNIQUE KEY uq_setup_workstations_catalog_building_workstation_name (building, workstation_ |
| `Database\Tables\AllTables.sql` | 404 | Work Center | KEY idx_setup_workstations_catalog_is_active (is_active), |
| `Database\Tables\AllTables.sql` | 405 | Work Center | KEY idx_setup_workstations_catalog_sort_rank (sort_rank) |
| `Database\Tables\AllTables.sql` | 410 | Work Center | -- Create table: config_workstation_hot_workcenters |
| `Database\Tables\AllTables.sql` | 419 | Work Center | CREATE TABLE IF NOT EXISTS config_workstation_hot_workcenters ( |
| `Database\Tables\AllTables.sql` | 422 | Computer | core_workstation_id BIGINT NOT NULL, |
| `Database\Tables\AllTables.sql` | 423 | Work Center | setup_workstation_id BIGINT NOT NULL, |
| `Database\Tables\AllTables.sql` | 431 | Work Center | UNIQUE KEY uq_config_workstation_hot_workcenters_public_id (public_id), |
| `Database\Tables\AllTables.sql` | 432 | Work Center | UNIQUE KEY uq_config_hot_workcenters_core_workstation_setup_workstation ( |
| `Database\Tables\AllTables.sql` | 433 | Computer | core_workstation_id, |
| `Database\Tables\AllTables.sql` | 434 | Work Center | setup_workstation_id |
| `Database\Tables\AllTables.sql` | 436 | Work Center | KEY idx_config_hot_workcenters_core_workstation_active_sort ( |
| `Database\Tables\AllTables.sql` | 437 | Computer | core_workstation_id, |
| `Database\Tables\AllTables.sql` | 441 | Work Center | CONSTRAINT fk_config_hot_workcenters_core_workstation_id FOREIGN KEY (core_workstation_id) |
| `Database\Tables\AllTables.sql` | 442 | Work Center | CONSTRAINT fk_config_hot_workcenters_setup_workstation_id FOREIGN KEY (setup_workstation_i |
| `Database\Tables\AllTables.sql` | 554 | Review | workstation_name VARCHAR(64) NOT NULL, |
| `Database\Tables\02_core_workstations_registry\create.sql` | 1 | Computer | -- Create table: core_workstations_registry |
| `Database\Tables\02_core_workstations_registry\create.sql` | 10 | Computer | CREATE TABLE IF NOT EXISTS core_workstations_registry ( |
| `Database\Tables\02_core_workstations_registry\create.sql` | 13 | Computer | workstation_name VARCHAR(128) NOT NULL, |
| `Database\Tables\02_core_workstations_registry\create.sql` | 20 | Computer | UNIQUE KEY uq_core_workstations_registry_public_id (public_id), |
| `Database\Tables\02_core_workstations_registry\create.sql` | 21 | Computer | UNIQUE KEY uq_core_workstations_registry_hostname_mac_address ( |
| `Database\Tables\02_core_workstations_registry\rollback.sql` | 1 | Computer | -- Rollback for table: core_workstations_registry |
| `Database\Tables\02_core_workstations_registry\rollback.sql` | 7 | Computer | DROP TABLE IF EXISTS core_workstations_registry; |
| `Database\Tables\05_auth_sessions_tokens\create.sql` | 14 | Computer | workstation_id BIGINT NULL, |
| `Database\Tables\05_auth_sessions_tokens\create.sql` | 27 | Computer | KEY idx_auth_sessions_tokens_workstation_id_expires_utc (workstation_id, expires_utc), |
| `Database\Tables\05_auth_sessions_tokens\create.sql` | 30 | Computer | CONSTRAINT fk_sessions_tokens_workstations_workstation_id FOREIGN KEY (workstation_id) REF |
| `Database\Tables\08_config_settings_values\create.sql` | 16 | Computer | workstation_id BIGINT NULL, |
| `Database\Tables\08_config_settings_values\create.sql` | 30 | Computer | KEY idx_config_settings_values_workstation_id (workstation_id), |
| `Database\Tables\08_config_settings_values\create.sql` | 33 | Computer | CONSTRAINT fk_values_ws_registry_workstation_id FOREIGN KEY (workstation_id) REFERENCES co |
| `Database\Tables\09_config_settings_history\create.sql` | 17 | Computer | workstation_id BIGINT NULL, |
| `Database\Tables\09_config_settings_history\create.sql` | 46 | Computer | CONSTRAINT fk_history_ws_registry_workstation_id FOREIGN KEY (workstation_id) REFERENCES c |
| `Database\Tables\13_setup_workstations_catalog\create.sql` | 1 | Work Center | -- Create table: setup_workstations_catalog |
| `Database\Tables\13_setup_workstations_catalog\create.sql` | 10 | Work Center | CREATE TABLE IF NOT EXISTS setup_workstations_catalog ( |
| `Database\Tables\13_setup_workstations_catalog\create.sql` | 14 | Work Center | workstation_name VARCHAR(64) NOT NULL, |
| `Database\Tables\13_setup_workstations_catalog\create.sql` | 22 | Work Center | UNIQUE KEY uq_setup_workstations_catalog_public_id (public_id), |
| `Database\Tables\13_setup_workstations_catalog\create.sql` | 23 | Work Center | UNIQUE KEY uq_setup_workstations_catalog_building_workstation_name (building, workstation_ |
| `Database\Tables\13_setup_workstations_catalog\create.sql` | 24 | Work Center | KEY idx_setup_workstations_catalog_is_active (is_active), |
| `Database\Tables\13_setup_workstations_catalog\create.sql` | 25 | Work Center | KEY idx_setup_workstations_catalog_sort_rank (sort_rank) |
| `Database\Tables\13_setup_workstations_catalog\rollback.sql` | 1 | Work Center | -- Rollback table: setup_workstations_catalog |
| `Database\Tables\13_setup_workstations_catalog\rollback.sql` | 5 | Work Center | DROP TABLE IF EXISTS setup_workstations_catalog; |
| `Database\Tables\14_config_workstation_hot_workcenters\create.sql` | 1 | Work Center | -- Create table: config_workstation_hot_workcenters |
| `Database\Tables\14_config_workstation_hot_workcenters\create.sql` | 10 | Work Center | CREATE TABLE IF NOT EXISTS config_workstation_hot_workcenters ( |
| `Database\Tables\14_config_workstation_hot_workcenters\create.sql` | 13 | Work Center | core_workstation_id BIGINT NOT NULL, |
| `Database\Tables\14_config_workstation_hot_workcenters\create.sql` | 14 | Work Center | setup_workstation_id BIGINT NOT NULL, |
| `Database\Tables\14_config_workstation_hot_workcenters\create.sql` | 22 | Work Center | UNIQUE KEY uq_config_workstation_hot_workcenters_public_id (public_id), |
| `Database\Tables\14_config_workstation_hot_workcenters\create.sql` | 23 | Work Center | UNIQUE KEY uq_config_hot_workcenters_core_workstation_setup_workstation ( |
| `Database\Tables\14_config_workstation_hot_workcenters\create.sql` | 24 | Work Center | core_workstation_id, |
| `Database\Tables\14_config_workstation_hot_workcenters\create.sql` | 25 | Work Center | setup_workstation_id |
| `Database\Tables\14_config_workstation_hot_workcenters\create.sql` | 27 | Work Center | KEY idx_config_hot_workcenters_core_workstation_active_sort ( |
| `Database\Tables\14_config_workstation_hot_workcenters\create.sql` | 28 | Work Center | core_workstation_id, |
| `Database\Tables\14_config_workstation_hot_workcenters\create.sql` | 32 | Work Center | CONSTRAINT fk_config_hot_workcenters_core_workstation_id FOREIGN KEY (core_workstation_id) |
| `Database\Tables\14_config_workstation_hot_workcenters\create.sql` | 33 | Work Center | CONSTRAINT fk_config_hot_workcenters_setup_workstation_id FOREIGN KEY (setup_workstation_i |
| `Database\Tables\14_config_workstation_hot_workcenters\rollback.sql` | 1 | Work Center | -- Rollback table: config_workstation_hot_workcenters |
| `Database\Tables\14_config_workstation_hot_workcenters\rollback.sql` | 7 | Work Center | DROP TABLE IF EXISTS config_workstation_hot_workcenters; |
| `Database\Tables\18_waitlist_requests_queue\create.sql` | 19 | Review | workstation_name VARCHAR(64) NOT NULL, |
| `Database\Validation\settings_schema\validate.sql` | 67 | Computer | SELECT 'missing_column', 'config_settings_values.workstation_id', 'BIGINT NULL', IF( |
| `Database\Validation\settings_schema\validate.sql` | 74 | Review | AND column_name = 'workstation_id' |
| `Database\Validation\startup_schema\validate.sql` | 43 | Computer | SELECT 'missing_table', 'core_workstations_registry', 'table exists', 'missing' |
| `Database\Validation\startup_schema\validate.sql` | 50 | Computer | AND table_name = 'core_workstations_registry' |
| `Database\Validation\startup_schema\validate.sql` | 330 | Computer | SELECT 'missing_column', 'core_workstations_registry.workstation_name', 'VARCHAR(128) NOT  |
| `Database\Validation\startup_schema\validate.sql` | 337 | Computer | AND table_name = 'core_workstations_registry' |
| `Database\Validation\startup_schema\validate.sql` | 338 | Review | AND column_name = 'workstation_name' |
| `Database\Validation\startup_schema\validate.sql` | 344 | Computer | SELECT 'missing_column', 'core_workstations_registry.hostname_normalized', 'VARCHAR(255) N |
| `Database\Validation\startup_schema\validate.sql` | 351 | Computer | AND table_name = 'core_workstations_registry' |
| `Database\Validation\startup_schema\validate.sql` | 358 | Computer | SELECT 'missing_column', 'core_workstations_registry.mac_address_normalized', 'VARCHAR(64) |
| `Database\Validation\startup_schema\validate.sql` | 365 | Computer | AND table_name = 'core_workstations_registry' |
| `Database\Validation\startup_schema\validate.sql` | 372 | Computer | SELECT 'missing_column', 'core_workstations_registry.is_registered', 'TINYINT(1) NOT NULL' |
| `Database\Validation\startup_schema\validate.sql` | 379 | Computer | AND table_name = 'core_workstations_registry' |
| `Database\Validation\startup_schema\validate.sql` | 385 | Computer | SELECT 'missing_column', 'core_workstations_registry.created_utc', 'DATETIME NOT NULL', 'm |
| `Database\Validation\startup_schema\validate.sql` | 392 | Computer | AND table_name = 'core_workstations_registry' |
| `Database\Validation\startup_schema\validate.sql` | 398 | Computer | SELECT 'missing_column', 'core_workstations_registry.updated_utc', 'DATETIME NOT NULL', 'm |
| `Database\Validation\startup_schema\validate.sql` | 405 | Computer | AND table_name = 'core_workstations_registry' |
| `Database\Validation\startup_schema\validate.sql` | 424 | Computer | SELECT 'missing_column', 'auth_sessions_tokens.workstation_id', 'BIGINT NULL', 'missing' |
| `Database\Validation\startup_schema\validate.sql` | 432 | Review | AND column_name = 'workstation_id' |
| `Database\Views\AllViews.sql` | 11 | Review | workstation_id, |
| `Database\Views\AllViews.sql` | 24 | Work Center | -- View: vw_setup_workstations_active |
| `Database\Views\AllViews.sql` | 29 | Work Center | DROP VIEW IF EXISTS vw_setup_workstations_active; |
| `Database\Views\AllViews.sql` | 31 | Work Center | CREATE VIEW vw_setup_workstations_active AS |
| `Database\Views\AllViews.sql` | 36 | Review | workstation_name, |
| `Database\Views\AllViews.sql` | 41 | Work Center | FROM setup_workstations_catalog |
| `Database\Views\vw_config_settings_scope_catalog\create.sql` | 11 | Review | workstation_id, |
| `Database\Views\vw_setup_workstations_active\create.sql` | 1 | Work Center | -- View: vw_setup_workstations_active |
| `Database\Views\vw_setup_workstations_active\create.sql` | 6 | Work Center | DROP VIEW IF EXISTS vw_setup_workstations_active; |
| `Database\Views\vw_setup_workstations_active\create.sql` | 8 | Work Center | CREATE VIEW vw_setup_workstations_active AS |
| `Database\Views\vw_setup_workstations_active\create.sql` | 13 | Work Center | workstation_name, |
| `Database\Views\vw_setup_workstations_active\create.sql` | 18 | Work Center | FROM setup_workstations_catalog |
| `Database\Views\vw_setup_workstations_active\rollback.sql` | 1 | Work Center | -- Rollback view: vw_setup_workstations_active |
| `Database\Views\vw_setup_workstations_active\rollback.sql` | 5 | Work Center | DROP VIEW IF EXISTS vw_setup_workstations_active; |
| `Documents\Complete-NewRequest_WorkCenter_Redesign_Checklist.md` | 3 | Review | This checklist applies the workstation-selection card workflow already implemented on |
| `Documents\Complete-NewRequest_WorkCenter_Redesign_Checklist.md` | 4 | Work Center | `Module_Setup/Views/SetupWorkstationPage.xaml` (and `SetupWorkstationViewModel`) to |
| `Documents\Complete-NewRequest_WorkCenter_Redesign_Checklist.md` | 18 | Work Center | - `Module_Setup/Views/SetupWorkstationPage.xaml` — `WorkstationCardItemContainerStyle`, `W |
| `Documents\Complete-NewRequest_WorkCenter_Redesign_Checklist.md` | 19 | Work Center | - `Module_Setup/Views/SetupWorkstationPage.xaml.cs` — responsive sizing. |
| `Documents\Complete-NewRequest_WorkCenter_Redesign_Checklist.md` | 20 | Work Center | - `Module_Setup/ViewModels/SetupWorkstationViewModel.cs` — filter/section logic. |
| `Documents\Complete-NewRequest_WorkCenter_Redesign_Checklist.md` | 21 | Work Center | - `Module_Setup/Models/SetupModels.cs` — `SetupWorkstation` observable model pattern. |
| `Documents\Complete-NewRequest_WorkCenter_Redesign_Checklist.md` | 35 | Work Center | - [x] **Model: Add computed display props `CurrentJobSummary`, `CurrentPartSummary`, and ` |
| `Documents\Complete-NewRequest_WorkCenter_Redesign_Checklist.md` | 40 | Work Center | Next task: **SQL: Extend the `WorkCenterCatalogService` catalog queries to also select `bu |
| `Documents\Complete-NewRequest_WorkCenter_Redesign_Checklist.md` | 44 | Work Center | - [x] **SQL: Extend the `WorkCenterCatalogService` catalog queries to also select `buildin |
| `Documents\Complete-NewRequest_WorkCenter_Redesign_Checklist.md` | 45 | Work Center | - [x] **SQL: Load the latest active job (work order / part / sequence) per work center by  |
| `Documents\Complete-NewRequest_WorkCenter_Redesign_Checklist.md` | 49 | Review | Next task: **XAML: Copy `WorkstationCardItemContainerStyle` (strips GridView selection vis |
| `Documents\Complete-NewRequest_WorkCenter_Redesign_Checklist.md` | 61 | Work Center | - [x] **XAML: Copy `WorkstationCardItemContainerStyle` (strips GridView selection visuals  |
| `Documents\Complete-NewRequest_WorkCenter_Redesign_Checklist.md` | 62 | Work Center | - [x] **XAML: Add a `WorkstationCardTemplate` DataTemplate (`x:DataType="WorkCenterSelecti |
| `Documents\Complete-NewRequest_WorkCenter_Redesign_Checklist.md` | 63 | Work Center | - [x] **XAML: Bind card rows to `WorkCenterName`, `Building`, `CurrentJobSummary`, `Curren |
| `Documents\Complete-NewRequest_WorkCenter_Redesign_Checklist.md` | 64 | Work Center | - [x] **XAML: Include a blue selection-outline `Border` overlay (2px, CornerRadius 12, `Is |
| `Documents\Complete-NewRequest_WorkCenter_Redesign_Checklist.md` | 70 | Work Center | - [x] **XAML: Wrap the "Local Work Centers" section `Border` with `Visibility` bound to `I |
| `Documents\Complete-NewRequest_WorkCenter_Redesign_Checklist.md` | 71 | Work Center | - [x] **XAML: Replace the "Other Work Centers" `Border` with an `Expander`** (`IsExpanded` |
| `Documents\Complete-NewRequest_WorkCenter_Redesign_Checklist.md` | 72 | Work Center | - [x] **XAML: Add a search `TextBox` (TwoWay to `FilterText`, `UpdateSourceTrigger=Propert |
| `Documents\Complete-NewRequest_WorkCenter_Redesign_Checklist.md` | 73 | Work Center | - [x] **XAML: Set both GridViews to `SelectionMode="None"`, `IsItemClickEnabled="True"`, a |
| `Documents\Complete-NewRequest_WorkCenter_Redesign_Checklist.md` | 88 | Work Center | - [x] **VM: Inject `IBuildingSelectionService` into `NewRequestWorkCenterViewModel`** and  |
| `Documents\Complete-NewRequest_WorkCenter_Redesign_Checklist.md` | 89 | Work Center | - [x] **VM: Add `ApplyFilter()` that filters by selected building + filter text (name / wo |
| `Documents\Complete-NewRequest_WorkCenter_Redesign_Checklist.md` | 90 | Work Center | - [x] **VM: Add `UpdateWorkCenterSectionsVisibility()`** — `IsLocalWorkCentersVisible = ha |
| `Documents\Complete-NewRequest_WorkCenter_Redesign_Checklist.md` | 91 | Work Center | - [x] **VM: In `OnNavigatedTo`, set `SelectedBuilding` from the building service and subsc |
| `Documents\Complete-NewRequest_WorkCenter_Redesign_Checklist.md` | 111 | Work Center | - [x] **Code-behind: Add responsive constants (`MinItemWidth = 520`, `MaxItemWidth = 580`, |
| `Documents\Complete-NewRequest_WorkCenter_Redesign_Checklist.md` | 112 | Work Center | - [x] **Code-behind: Add `SizeChanged` handlers on both GridViews calling `UpdateItemSize( |
| `Documents\Complete-NewRequest_WorkCenter_Redesign_Checklist.md` | 113 | Work Center | - [x] **Code-behind: Retry via `DispatcherQueue.TryEnqueue(UpdateItemSize)` only when a gr |
| `Documents\Complete-NewRequest_WorkCenter_Redesign_Checklist.md` | 138 | Review | - [x] **Verification: Manually confirm on the New Request wizard** — Local section shows f |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 8 | Review | This design separates two concepts that both currently use the word "workstation", and **e |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 12 | Computer | \| **Computer** \| A physical machine (hostname e.g. `johnspc`, display name e.g. "John's Co |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 13 | Work Center | \| **Work Center** \| A press/work station (e.g. `100-3`, `100-6`). Selected in Module_Setup |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 15 | Computer | **Definition of Done (rename):** After the rename, the term "Workstation" / "workstation"  |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 19 | Computer | ## 2. Data Model — `core_workstations_registry` → `core_computers_registry` |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 21 | Review | Rename the table and `workstation_name` column; add two columns: |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 23 | Computer | - `computer_name VARCHAR(128) NOT NULL` (renamed from `workstation_name`) |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 35 | Computer | - `auth_sessions_tokens.workstation_id` → `computer_id` |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 36 | Computer | - `config_settings_values.workstation_id` → `computer_id` |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 37 | Computer | - `config_settings_history.workstation_id` → `computer_id` |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 38 | Work Center | - `config_workstation_hot_workcenters.core_workstation_id` → `computer_id` (computer side  |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 44 | Computer | The work-center concept is **renamed from "Workstation" to "Work Center"** (never "Compute |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 46 | Work Center | - `setup_workstations_catalog` → `setup_work_centers_catalog` |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 47 | Work Center | - `sp_setup_workstations_*` → `sp_setup_work_centers_*` |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 48 | Work Center | - `fn_setup_workstation_name_normalized` → `fn_setup_work_center_name_normalized` |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 49 | Work Center | - `vw_setup_workstations_active` → `vw_setup_work_centers_active` |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 50 | Work Center | - `SetupWorkstation*` classes/pages/view models/services → `SetupWorkCenter*` |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 51 | Work Center | - `config_workstation_hot_workcenters.setup_workstation_id` → `work_center_id` |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 52 | Work Center | - `waitlist_requests_queue.workstation_name` → `work_center_name` (requests happen at a wo |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 54 | Work Center | Work-center naming uses "work_center" / "Work Center" — never "workstation" and never "com |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 67 | Review | - **No stable MAC** (VM / no NIC): fall back to hostname-only; if still not authoritative, |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 84 | Work Center | - Does NOT change stored data (e.g. `waitlist_requests_queue.workstation_name` remains raw |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 94 | Computer | Rename **every "workstation" occurrence** into either "Computer" or "Work Center": |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 98 | Review | - **Final sweep:** VS Code search for `Workstation`, `workstation`, and `Work Station` ret |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 112 | Computer | - `tools/scan_workstation_rename.ps1` scans every text file in the repo (excluding generat |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 114 | Review | `pwsh -NoProfile -File tools/scan_workstation_rename.ps1` |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 9 | Review | - [ ] **Tech Lead: Run baseline rename scan** via `tools/scan_workstation_rename.ps1` and  |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 12 | Computer | - [ ] **Database Migration: Rename table `core_workstations_registry` → `core_computers_re |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 13 | Computer | - [ ] **Database Migration: Rename column `workstation_name` → `computer_name`** in `core_ |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 21 | Computer | - [ ] **Database Migration: Rename `auth_sessions_tokens.workstation_id` → `computer_id`** |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 22 | Computer | - [ ] **Database Migration: Rename `config_settings_values.workstation_id` → `computer_id` |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 23 | Computer | - [ ] **Database Migration: Rename `config_settings_history.workstation_id` → `computer_id |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 24 | Work Center | - [ ] **Database Migration: Rename `config_workstation_hot_workcenters.core_workstation_id |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 25 | Work Center | - [ ] **Database Migration: Rename table `config_workstation_hot_workcenters` → `config_co |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 34 | Review | ### Subphase 1.4: Rename work-center catalog (Work Station → Work Center) (Ref: 1, 4) |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 35 | Work Center | - [ ] **Database Migration: Rename table `setup_workstations_catalog` → `setup_work_center |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 36 | Work Center | - [ ] **Database Migration: Rename `waitlist_requests_queue.workstation_name` → `work_cent |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 37 | Work Center | - [ ] **Database Migration: Rename `sp_setup_workstations_*` → `sp_setup_work_centers_*`** |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 38 | Work Center | - [ ] **Database Migration: Rename `fn_setup_workstation_name_normalized` → `fn_setup_work |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 41 | Work Center | **GATE: Dev DB recreates cleanly with `core_computers_registry`, `setup_work_centers_catal |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 47 | Computer | - [ ] **Service Layer: Update `sp_config_settings_get_effective`** param `p_workstation_id |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 48 | Computer | - [ ] **Service Layer: Update `sp_config_settings_upsert`** param `p_workstation_id` → `p_ |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 49 | Work Center | - [ ] **Service Layer: Update `sp_config_hot_workcenters_*`** joins/params from `core_work |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 50 | Computer | - [ ] **Service Layer: Audit all SPs in `Database/StoredProcedures/AllSPs.sql`** for remai |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 51 | Work Center | - [ ] **Service Layer: Rename work-center SPs** (`sp_setup_work_centers_*`) and update `Al |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 53 | Work Center | **GATE: All stored procedures compile; `core_computers_registry`/`computer_id` for compute |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 60 | Computer | - [ ] **Service Layer: Rename computer-related fields in `StartupState` / `StartupSessionS |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 64 | Work Center | - [ ] **Full Stack Engineer: Rename Module_Setup work-center code** `SetupWorkstation*` →  |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 77 | Review | - [ ] **Dialog Behavior: No-MAC skip** — reuse `IsWorkstationRegistrationAuthoritative`; s |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 88 | Review | - [ ] **Full Stack Engineer: Keep stored data raw** — display format is presentation-only; |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 110 | Review | - [ ] **Tech Lead: Final sweep — run `tools/scan_workstation_rename.ps1` and VS Code searc |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 112 | Computer | **GATE: No user-facing or code "workstation" text remains anywhere; computer = "Computer", |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 140 | Computer | Next task: **Rename table `core_workstations_registry` → `core_computers_registry` in `Dat |
| `Documents\Weekend-User-Update-Email.md` | 45 | Review | - Responsive cards keep the screen usable on any workstation or screen size. |
| `Documents\Development\InforVisual\InforVisualGuide.md` | 1166 | Review | - In the Macros section, specify how to store macros and where to read macros. Macros can  |
| `Documents\Development\InforVisual\InforVisualGuide.md` | 1173 | Review | To store macros on and read them from the workstation, clear the check box. After you clea |
| `Documents\Development\InforVisual\InforVisualGuide.md` | 1175 | Review | - - When a user runs a macro, the macro is read from the workstation. |
| `Documents\Development\InforVisual\InforVisualGuide.md` | 1176 | Review | - Any macros created after the check box is selected are stored on the workstation. |
| `Documents\Development\InforVisual\InforVisualGuide.md` | 1178 | Review | You cannot read certain macros from the database and other macros from the workstation. If |
| `Documents\Development\InforVisual\InforVisualGuide.md` | 1765 | Review | Use the Default Sign In Profile function to set default sign in information for the work s |
| `Documents\Development\InforVisual\Interaction_Guides\UI_Interaction_Guide.md` | 57 | Review | No local Infor/MAPICS registry keys found on this workstation. Visual is a network-hosted |
| `Documents\Development\InforVisual\Interaction_Guides\UI_Interaction_Guide.md` | 176 | Review | **Both are present and functional.** VBScript can be invoked on this workstation. |
| `Documents\Development\InforVisual\Interaction_Guides\UI_Interaction_Guide.md` | 213 | Review | **Conclusion:** `wscript.exe` can run unsigned `.vbs` files on this workstation without re |
| `Documents\Development\InforVisual\Interaction_Guides\UI_Interaction_Guide.md` | 253 | Review | Windows services (e.g., `LanmanWorkstation`, `SessionEnv`, Chrome/Edge elevation services) |
| `Documents\Development\InforVisual\Interaction_Guides\UI_Interaction_Guide.md` | 269 | Review | **ION is not installed, licensed, or reachable from this workstation.** |
| `Documents\Development\InforVisual\Interaction_Guides\VBScript_Guide.md` | 23 | Review | The infrastructure — VBScript runtime, `ADODB`, user credentials, network access to Visual |
| `Documents\Development\InforVisual\Interaction_Guides\VBScript_Guide.md` | 277 | Review | \| 4 \| Does Visual's macro security policy on production workstations allow `wscript.exe` t |
| `Documents\Development\Waitlist\NewRequestFeature\RequiredWorkflow.md` | 14 | Review | - Workstation identity at the top (current workstation name). |
| `Documents\Development\Waitlist\NewRequestFeature\RequiredWorkflow.md` | 128 | Review | - workstationName |
| `Module_Core\Services\PageService.cs` | 39 | Work Center | Configure<MTM_Waitlist.Module_Setup.ViewModels.SetupWorkstationViewModel, SetupWorkstation |
| `Module_Core\Services\DependencyInjection\ServiceRegistrationExtensions.cs` | 77 | Work Center | services.AddTransient<SetupWorkstationViewModel>(); |
| `Module_Core\Services\DependencyInjection\ServiceRegistrationExtensions.cs` | 86 | Work Center | services.AddTransient<SetupWorkstationPage>(); |
| `Module_Core\ViewModels\ShellViewModel.cs` | 44 | Review | "Work Station", |
| `Module_Core\ViewModels\ShellViewModel.cs` | 241 | Work Center | Selected = NavigationViewService.GetSelectedItem(typeof(SetupWorkstationPage)); |
| `Module_Core\ViewModels\ShellViewModel.cs` | 317 | Work Center | if (pageType == typeof(SetupWorkstationPage)) |
| `Module_Core\ViewModels\ShellViewModel.cs` | 319 | Review | return ("Work Center Setup — Select Work Station", 1); |
| `Module_Core\Views\ShellPage.xaml` | 266 | Work Center | <NavigationViewItem x:Uid="Shell_ModuleSetup" helpers:NavigationHelper.NavigateTo="MTM_Wai |
| `Module_Settings\Models\ConfigSettingValue.cs` | 26 | Review | /// Scope type (e.g., "all_users", "workstation", "user") |
| `Module_Settings\Models\ConfigSettingValue.cs` | 32 | Review | /// Scope key (e.g., "all_users", workstation_id, user_id) |
| `Module_Settings\Models\ConfigSettingValue.cs` | 38 | Review | /// Workstation ID (optional, for workstation-scoped settings) |
| `Module_Settings\Models\ConfigSettingValue.cs` | 39 | Review | /// Nullable; used when scope_type is "workstation". |
| `Module_Settings\Models\ConfigSettingValue.cs` | 41 | Review | public long? WorkstationId { get; init; } |
| `Module_Settings\Models\ImageLocation.cs` | 17 | Work Center | /// For work centers: numeric ID (from setup_workstations_catalog.id) |
| `Module_Settings\Models\ImageLocation.cs` | 25 | Review | /// For work centers: The workstation_name (e.g., "Press 1") |
| `Module_Settings\Models\ImageLocation.cs` | 185 | Work Center | /// Numeric ID from setup_workstations_catalog.id |
| `Module_Settings\Models\ImageLocation.cs` | 191 | Work Center | /// From setup_workstations_catalog.workstation_name |
| `Module_Settings\Models\ImageLocation.cs` | 197 | Work Center | /// From setup_workstations_catalog.building |
| `Module_Settings\Models\ImageLocationDefaults.cs` | 31 | Review | public const string WorkCenterDefaultPath = "Assets\\Images\\default-workstation-image.png |
| `Module_Settings\Models\ImageLocationScope.cs` | 30 | Review | /// Default image: Assets\Images\default-workstation-image.png |
| `Module_Settings\Models\ImageLocationScope.cs` | 32 | Work Center | /// Inventory: Dynamic (from setup_workstations_catalog, live database) |
| `Module_Settings\Models\ImageOverride.cs` | 31 | Work Center | /// For work centers: numeric ID string (from setup_workstations_catalog.id) |
| `Module_Settings\Models\ImageStorageOptions.cs` | 33 | Review | /// Must be accessible from the app server and all workstations. |
| `Module_Settings\Models\WorkCenterInventory.cs` | 6 | Work Center | /// work centers are dynamic and loaded from the database table setup_workstations_catalog |
| `Module_Settings\Models\WorkCenterInventory.cs` | 11 | Work Center | /// Source: setup_workstations_catalog database table |
| `Module_Settings\Models\WorkCenterInventory.cs` | 12 | Work Center | /// Row Key: setup_workstations_catalog.id (numeric BIGINT) |
| `Module_Settings\Models\WorkCenterInventory.cs` | 14 | Review | /// Default Image: Assets\Images\default-workstation-image.png |
| `Module_Settings\Models\WorkCenterInventory.cs` | 20 | Work Center | /// Loaded from setup_workstations_catalog at application startup and kept in sync. |
| `Module_Settings\Models\WorkCenterInventory.cs` | 42 | Work Center | /// <param name="workCenterId">The numeric ID from setup_workstations_catalog.id</param> |
| `Module_Settings\Models\WorkCenterInventory.cs` | 94 | Work Center | /// Represents a single work center from the setup_workstations_catalog. |
| `Module_Settings\Models\WorkCenterInventory.cs` | 100 | Work Center | /// Numeric primary key from setup_workstations_catalog.id (BIGINT AUTO_INCREMENT). |
| `Module_Settings\Models\WorkCenterInventory.cs` | 108 | Work Center | /// From setup_workstations_catalog.workstation_name. |
| `Module_Settings\Models\WorkCenterInventory.cs` | 115 | Work Center | /// From setup_workstations_catalog.building (e.g., "Expo Drive"). |
| `Module_Settings\Models\WorkCenterInventory.cs` | 122 | Work Center | /// From setup_workstations_catalog.sort_rank. |
| `Module_Settings\Models\WorkCenterInventory.cs` | 129 | Work Center | /// From setup_workstations_catalog.is_active. |
| `Module_Settings\Services\ConfigSettingsValueService.cs` | 45 | Review | workstation_id, |
| `Module_Settings\Services\ConfigSettingsValueService.cs` | 106 | Review | ["p_workstation_id"] = setting.WorkstationId, |
| `Module_Settings\Services\ConfigSettingsValueService.cs` | 174 | Review | WorkstationId = GetNullableInt64(row, "workstation_id"), |
| `Module_Settings\Services\IImageLocationService.cs` | 82 | Work Center | /// <param name="workCenterId">The numeric ID from setup_workstations_catalog</param> |
| `Module_Settings\Services\IImageLocationService.cs` | 146 | Work Center | /// Queries the setup_workstations_catalog table for all rows where is_active=1. |
| `Module_Settings\Services\IImageOverrideReadService.cs` | 72 | Work Center | /// For example, a work_center override for an ID that's no longer in setup_workstations_c |
| `Module_Settings\Services\ImageLocationService.cs` | 581 | Review | // An empty workstation name makes the catalog service resolve the current workstation. |
| `Module_Settings\Services\ImageLocationService.cs` | 650 | Review | workstation_name, |
| `Module_Settings\Services\ImageLocationService.cs` | 654 | Work Center | FROM setup_workstations_catalog |
| `Module_Settings\Services\ImageLocationService.cs` | 656 | Review | AND workstation_name IN ({string.Join(", ", placeholders)}) |
| `Module_Settings\Services\ImageLocationService.cs` | 657 | Review | ORDER BY building ASC, sort_rank ASC, workstation_name ASC;"; |
| `Module_Settings\Services\ImageLocationService.cs` | 671 | Review | DisplayName = ReadString(row, "workstation_name"), |
| `Module_Settings\Services\ImageOverrideReadService.cs` | 477 | Work Center | @"SELECT id FROM setup_workstations_catalog WHERE id = @p_id LIMIT 1;", |
| `Module_Settings\ViewModels\SettingsViewModel.cs` | 72 | Review | public partial string SelectedWorkstation |
| `Module_Settings\ViewModels\SettingsViewModel.cs` | 101 | Review | public ObservableCollection<string> AvailableWorkstations { get; } = new(); |
| `Module_Settings\ViewModels\SettingsViewModel.cs` | 125 | Review | "workstation", |
| `Module_Settings\ViewModels\SettingsViewModel.cs` | 129 | Review | string.Join(" ", AvailableWorkstations)); |
| `Module_Settings\ViewModels\SettingsViewModel.cs` | 230 | Review | partial void OnSelectedWorkstationChanged(string value) |
| `Module_Settings\ViewModels\SettingsViewModel.cs` | 232 | Computer | StartupDebugLog.Info("SettingsViewModel", $"SelectedWorkstation changed to '{value}'."); |
| `Module_Settings\ViewModels\SettingsViewModel.cs` | 238 | Review | _ = LoadCatalogForWorkstationAsync(value); |
| `Module_Settings\ViewModels\SettingsViewModel.cs` | 255 | Computer | StartupDebugLog.Info("SettingsHotWorkCenters", $"AddHotWorkCenterAsync started. WorkCenter |
| `Module_Settings\ViewModels\SettingsViewModel.cs` | 288 | Computer | StartupDebugLog.Info("SettingsHotWorkCenters", $"RemoveHotWorkCenterAsync started. WorkCen |
| `Module_Settings\ViewModels\SettingsViewModel.cs` | 376 | Review | var workstations = await _workCenterCatalogService.GetAvailableWorkstationsAsync().Configu |
| `Module_Settings\ViewModels\SettingsViewModel.cs` | 377 | Review | ReplaceCollectionValues(AvailableWorkstations, workstations); |
| `Module_Settings\ViewModels\SettingsViewModel.cs` | 379 | Review | var currentWorkstation = _workCenterCatalogService.GetCurrentWorkstationName(); |
| `Module_Settings\ViewModels\SettingsViewModel.cs` | 380 | Review | var resolvedWorkstation = AvailableWorkstations.FirstOrDefault(item => |
| `Module_Settings\ViewModels\SettingsViewModel.cs` | 381 | Review | string.Equals(item, currentWorkstation, StringComparison.OrdinalIgnoreCase)) |
| `Module_Settings\ViewModels\SettingsViewModel.cs` | 382 | Review | ?? AvailableWorkstations.FirstOrDefault() |
| `Module_Settings\ViewModels\SettingsViewModel.cs` | 383 | Review | ?? currentWorkstation; |
| `Module_Settings\ViewModels\SettingsViewModel.cs` | 385 | Review | var workstationChanged = !string.Equals(SelectedWorkstation, resolvedWorkstation, StringCo |
| `Module_Settings\ViewModels\SettingsViewModel.cs` | 386 | Review | if (string.IsNullOrWhiteSpace(SelectedWorkstation)) |
| `Module_Settings\ViewModels\SettingsViewModel.cs` | 388 | Review | SelectedWorkstation = resolvedWorkstation; |
| `Module_Settings\ViewModels\SettingsViewModel.cs` | 390 | Review | else if (workstationChanged) |
| `Module_Settings\ViewModels\SettingsViewModel.cs` | 392 | Review | SelectedWorkstation = resolvedWorkstation; |
| `Module_Settings\ViewModels\SettingsViewModel.cs` | 396 | Review | await LoadCatalogForWorkstationAsync(SelectedWorkstation).ConfigureAwait(true); |
| `Module_Settings\ViewModels\SettingsViewModel.cs` | 401 | Computer | StartupDebugLog.Info("SettingsViewModel", $"InitializeHotWorkCentersAsync completed. Works |
| `Module_Settings\ViewModels\SettingsViewModel.cs` | 409 | Review | private async Task LoadCatalogForWorkstationAsync(string workstationName) |
| `Module_Settings\ViewModels\SettingsViewModel.cs` | 411 | Computer | StartupDebugLog.Info("SettingsViewModel", $"LoadCatalogForWorkstationAsync started. Workst |
| `Module_Settings\ViewModels\SettingsViewModel.cs` | 415 | Review | var catalog = await _workCenterCatalogService.GetCatalogAsync(workstationName).ConfigureAw |
| `Module_Settings\ViewModels\SettingsViewModel.cs` | 420 | Computer | StartupDebugLog.Info("SettingsViewModel", $"LoadCatalogForWorkstationAsync completed. Work |
| `Module_Settings\ViewModels\SettingsViewModel.cs` | 424 | Computer | StartupDebugLog.Error("SettingsViewModel", ex, $"LoadCatalogForWorkstationAsync failed. Wo |
| `Module_Settings\ViewModels\SettingsViewModel.cs` | 437 | Computer | StartupDebugLog.Info("SettingsHotWorkCenters", $"SaveCurrentHotWorkCentersAsync started. W |
| `Module_Settings\ViewModels\SettingsViewModel.cs` | 439 | Review | .SaveHotWorkCentersAsync(SelectedWorkstation, HotWorkCenters.ToArray()) |
| `Module_Settings\ViewModels\SettingsViewModel.cs` | 446 | Computer | StartupDebugLog.Info("SettingsHotWorkCenters", $"SaveCurrentHotWorkCentersAsync completed. |
| `Module_Settings\ViewModels\SettingsViewModel.cs` | 450 | Computer | StartupDebugLog.Error("SettingsHotWorkCenters", ex, $"SaveCurrentHotWorkCentersAsync faile |
| `Module_Settings\ViewModels\WorkCenterImagesDialogViewModel.cs` | 9 | Work Center | /// setup_workstations_catalog.id. |
| `Module_Settings\Views\SettingsPage.xaml` | 414 | Review | ItemsSource="{x:Bind ViewModel.AvailableWorkstations, Mode=OneWay}" |
| `Module_Settings\Views\SettingsPage.xaml` | 415 | Review | SelectedItem="{x:Bind ViewModel.SelectedWorkstation, Mode=TwoWay}" /> |
| `Module_Setup\Contracts\Services\SetupContracts.cs` | 22 | Work Center | public interface ISetupWorkstationService |
| `Module_Setup\Contracts\Services\SetupContracts.cs` | 24 | Work Center | Task<IReadOnlyList<SetupWorkstation>> GetWorkstationsAsync(CancellationToken cancellationT |
| `Module_Setup\Contracts\Services\SetupContracts.cs` | 26 | Review | Task<SetupSelectionResult> AddWorkstationAsync(string workstationName, string building, Ca |
| `Module_Setup\Contracts\Services\SetupContracts.cs` | 28 | Review | Task<SetupSelectionResult> UpdateWorkstationAsync(string workstationId, string workstation |
| `Module_Setup\Contracts\Services\SetupContracts.cs` | 30 | Review | Task<SetupSelectionResult> RemoveWorkstationAsync(string workstationId, CancellationToken  |
| `Module_Setup\Models\SetupModels.cs` | 9 | Review | WorkstationSelection, |
| `Module_Setup\Models\SetupModels.cs` | 32 | Review | private SetupWorkflowStep _currentStep = SetupWorkflowStep.WorkstationSelection; |
| `Module_Setup\Models\SetupModels.cs` | 36 | Work Center | public ObservableCollection<SetupWorkstation> Workstations { get; } = new(); |
| `Module_Setup\Models\SetupModels.cs` | 151 | Review | CurrentStep = SetupWorkflowStep.WorkstationSelection; |
| `Module_Setup\Models\SetupModels.cs` | 153 | Review | Workstations.Clear(); |
| `Module_Setup\Models\SetupModels.cs` | 165 | Work Center | public sealed class SetupWorkstation : ObservableObject |
| `Module_Setup\Models\SetupModels.cs` | 167 | Review | private const string DefaultWorkstationImagePath = "Assets/Images/default-workstation-imag |
| `Module_Setup\Models\SetupModels.cs` | 172 | Review | /// Whether this workstation is the currently selected card in the selection grid. |
| `Module_Setup\Models\SetupModels.cs` | 190 | Review | /// Resolved image path for the workstation's work center. Populated by the |
| `Module_Setup\Models\SetupModels.cs` | 192 | Review | /// Falls back to the packaged default workstation image when unresolved. |
| `Module_Setup\Models\SetupModels.cs` | 194 | Review | public string ImagePath { get; set; } = DefaultWorkstationImagePath; |
| `Module_Setup\Models\SetupModels.cs` | 204 | Work Center | /// Read from <c>setup_workstations_catalog.updated_utc</c> and refreshed whenever a |
| `Module_Setup\Services\SetupPersistenceService.cs` | 296 | Work Center | "sp_setup_workstations_touch", |
| `Module_Setup\Services\SetupPersistenceService.cs` | 305 | Work Center | StartupDebugLog.Info("SetupPersistence", $"sp_setup_workstations_touch completed. WorkCent |
| `Module_Setup\Services\SetupWorkstationService.cs` | 9 | Work Center | public sealed class SetupWorkstationService : ISetupWorkstationService |
| `Module_Setup\Services\SetupWorkstationService.cs` | 13 | Work Center | public SetupWorkstationService(MySqlHelperServer mySqlHelperServer) |
| `Module_Setup\Services\SetupWorkstationService.cs` | 18 | Work Center | public async Task<IReadOnlyList<SetupWorkstation>> GetWorkstationsAsync(CancellationToken  |
| `Module_Setup\Services\SetupWorkstationService.cs` | 21 | Work Center | "sp_setup_workstations_get_all", |
| `Module_Setup\Services\SetupWorkstationService.cs` | 42 | Work Center | var workstationName = GetValue(row, "workstation_name"); |
| `Module_Setup\Services\SetupWorkstationService.cs` | 43 | Work Center | jobsByWorkCenter.TryGetValue(workstationName, out var activeJobRow); |
| `Module_Setup\Services\SetupWorkstationService.cs` | 45 | Work Center | return new SetupWorkstation |
| `Module_Setup\Services\SetupWorkstationService.cs` | 48 | Work Center | Name = workstationName, |
| `Module_Setup\Services\SetupWorkstationService.cs` | 62 | Work Center | public async Task<SetupSelectionResult> AddWorkstationAsync(string workstationName, string |
| `Module_Setup\Services\SetupWorkstationService.cs` | 64 | Work Center | if (string.IsNullOrWhiteSpace(workstationName)) |
| `Module_Setup\Services\SetupWorkstationService.cs` | 66 | Work Center | return new SetupSelectionResult { Success = false, Message = "Workstation name is required |
| `Module_Setup\Services\SetupWorkstationService.cs` | 75 | Work Center | "sp_setup_workstations_upsert", |
| `Module_Setup\Services\SetupWorkstationService.cs` | 78 | Work Center | ["p_workstation_id"] = null, |
| `Module_Setup\Services\SetupWorkstationService.cs` | 79 | Work Center | ["p_workstation_name"] = workstationName.Trim(), |
| `Module_Setup\Services\SetupWorkstationService.cs` | 89 | Work Center | Message = affectedRows > 0 ? "Workstation added." : "Unable to add workstation." |
| `Module_Setup\Services\SetupWorkstationService.cs` | 93 | Work Center | public async Task<SetupSelectionResult> UpdateWorkstationAsync(string workstationId, strin |
| `Module_Setup\Services\SetupWorkstationService.cs` | 95 | Work Center | if (string.IsNullOrWhiteSpace(workstationId) \|\| string.IsNullOrWhiteSpace(workstationName) |
| `Module_Setup\Services\SetupWorkstationService.cs` | 97 | Work Center | return new SetupSelectionResult { Success = false, Message = "Workstation ID, name, and bu |
| `Module_Setup\Services\SetupWorkstationService.cs` | 101 | Work Center | "sp_setup_workstations_upsert", |
| `Module_Setup\Services\SetupWorkstationService.cs` | 104 | Work Center | ["p_workstation_id"] = workstationId.Trim(), |
| `Module_Setup\Services\SetupWorkstationService.cs` | 105 | Work Center | ["p_workstation_name"] = workstationName.Trim(), |
| `Module_Setup\Services\SetupWorkstationService.cs` | 115 | Work Center | Message = affectedRows > 0 ? "Workstation updated." : "Unable to update workstation." |
| `Module_Setup\Services\SetupWorkstationService.cs` | 119 | Work Center | public async Task<SetupSelectionResult> RemoveWorkstationAsync(string workstationId, Cance |
| `Module_Setup\Services\SetupWorkstationService.cs` | 121 | Work Center | if (string.IsNullOrWhiteSpace(workstationId)) |
| `Module_Setup\Services\SetupWorkstationService.cs` | 123 | Work Center | return new SetupSelectionResult { Success = false, Message = "Workstation ID is required." |
| `Module_Setup\Services\SetupWorkstationService.cs` | 127 | Work Center | "sp_setup_workstations_delete", |
| `Module_Setup\Services\SetupWorkstationService.cs` | 130 | Work Center | ["p_workstation_id"] = workstationId.Trim(), |
| `Module_Setup\Services\SetupWorkstationService.cs` | 138 | Work Center | Message = affectedRows > 0 ? "Workstation removed." : "Unable to remove workstation." |
| `Module_Setup\Services\DependencyInjection\ModuleDependencyInjectionExtensions.cs` | 16 | Work Center | services.AddSingleton<ISetupWorkstationService, SetupWorkstationService>(); |
| `Module_Setup\ViewModels\SetupCompletionViewModel.cs` | 91 | Work Center | _navigationService.NavigateTo(typeof(SetupWorkstationViewModel).FullName!, null, true); |
| `Module_Setup\ViewModels\SetupWorkOrderViewModel.cs` | 187 | Review | private void BackToWorkstations() |
| `Module_Setup\ViewModels\SetupWorkOrderViewModel.cs` | 189 | Work Center | _navigationService.NavigateTo(typeof(SetupWorkstationViewModel).FullName!, null); |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 18 | Work Center | public partial class SetupWorkstationViewModel : ObservableRecipient, INavigationAware |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 20 | Work Center | private const string DefaultWorkstationImagePath = "Assets/Images/default-workstation-imag |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 34 | Work Center | private readonly ISetupWorkstationService _workstationService; |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 40 | Work Center | private readonly ObservableCollection<SetupWorkstation> _displayedHotWorkstations = new(); |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 41 | Work Center | private readonly ObservableCollection<SetupWorkstation> _displayedOtherWorkstations = new( |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 44 | Work Center | public partial SetupWorkstation? SelectedWorkstation |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 50 | Work Center | public partial string WorkstationNameInput |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 93 | Work Center | public ObservableCollection<SetupWorkstation> Workstations => State.Workstations; |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 95 | Work Center | public ObservableCollection<SetupWorkstation> DisplayedHotWorkstations => _displayedHotWor |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 97 | Work Center | public ObservableCollection<SetupWorkstation> DisplayedOtherWorkstations => _displayedOthe |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 110 | Work Center | public bool CanManageWorkstations => AllowedManageRoles.Any(role => string.Equals(role, _s |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 112 | Work Center | public SetupWorkstationViewModel( |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 115 | Work Center | ISetupWorkstationService workstationService, |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 123 | Work Center | _workstationService = workstationService; |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 136 | Work Center | SelectedWorkstation = State.Workstations.FirstOrDefault(item => |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 139 | Work Center | _ = LoadWorkstationsAsync(); |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 150 | Work Center | if (SelectedWorkstation is null) |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 152 | Work Center | StatusMessage = "Choose a workstation to continue."; |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 156 | Work Center | State.SelectedWorkCenter = SelectedWorkstation.Name; |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 166 | Work Center | await LoadWorkstationsAsync().ConfigureAwait(true); |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 170 | Work Center | private async Task AddWorkstationAsync() |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 172 | Work Center | if (!CanManageWorkstations) |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 174 | Work Center | StatusMessage = "You do not have permission to manage workstations."; |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 178 | Work Center | var result = await _workstationService.AddWorkstationAsync(WorkstationNameInput, BuildingI |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 182 | Work Center | WorkstationNameInput = string.Empty; |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 184 | Work Center | await LoadWorkstationsAsync().ConfigureAwait(true); |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 189 | Work Center | private async Task UpdateWorkstationAsync() |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 191 | Work Center | if (!CanManageWorkstations) |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 193 | Work Center | StatusMessage = "You do not have permission to manage workstations."; |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 197 | Work Center | if (SelectedWorkstation is null) |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 199 | Work Center | StatusMessage = "Select a workstation to edit."; |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 203 | Work Center | var result = await _workstationService.UpdateWorkstationAsync(SelectedWorkstation.Id, Work |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 207 | Work Center | WorkstationNameInput = string.Empty; |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 209 | Work Center | await LoadWorkstationsAsync().ConfigureAwait(true); |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 214 | Work Center | private async Task RemoveWorkstationAsync() |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 216 | Work Center | if (!CanManageWorkstations) |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 218 | Work Center | StatusMessage = "You do not have permission to manage workstations."; |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 222 | Work Center | if (SelectedWorkstation is null) |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 224 | Work Center | StatusMessage = "Select a workstation to remove."; |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 228 | Work Center | var result = await _workstationService.RemoveWorkstationAsync(SelectedWorkstation.Id).Conf |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 232 | Work Center | WorkstationNameInput = string.Empty; |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 233 | Work Center | await LoadWorkstationsAsync().ConfigureAwait(true); |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 237 | Work Center | partial void OnSelectedWorkstationChanged(SetupWorkstation? value) |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 239 | Work Center | foreach (var item in State.Workstations) |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 249 | Work Center | WorkstationNameInput = value.Name; |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 266 | Work Center | private async Task LoadWorkstationsAsync() |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 271 | Work Center | var items = await _workstationService.GetWorkstationsAsync().ConfigureAwait(true); |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 272 | Work Center | State.Workstations.Clear(); |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 275 | Work Center | item.ImagePath = await ResolveWorkstationImagePathAsync(item).ConfigureAwait(true); |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 276 | Work Center | State.Workstations.Add(item); |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 282 | Work Center | var allVisible = _displayedHotWorkstations.Concat(_displayedOtherWorkstations).ToArray(); |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 283 | Work Center | if (SelectedWorkstation is null && allVisible.Length > 0) |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 285 | Work Center | SelectedWorkstation = allVisible.FirstOrDefault(item => string.Equals(item.Name, State.Sel |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 301 | Work Center | .GetCatalogAsync(_workCenterCatalogService.GetCurrentWorkstationName(), cancellationToken) |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 308 | Work Center | StartupDebugLog.Info("SetupWorkstation", $"Local work centers loaded for the setup selecti |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 312 | Work Center | StartupDebugLog.Error("SetupWorkstation", ex, "Failed to load Local work centers for the s |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 326 | Work Center | private async Task<string> ResolveWorkstationImagePathAsync(SetupWorkstation workstation,  |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 330 | Work Center | \|\| string.IsNullOrWhiteSpace(workstation.Id)) |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 332 | Work Center | return DefaultWorkstationImagePath; |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 338 | Work Center | .ResolveWorkCenterImagePathAsync(workstation.Id, cancellationToken) |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 341 | Work Center | ? DefaultWorkstationImagePath |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 347 | Work Center | return DefaultWorkstationImagePath; |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 353 | Work Center | _displayedHotWorkstations.Clear(); |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 354 | Work Center | _displayedOtherWorkstations.Clear(); |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 358 | Work Center | var filteredItems = State.Workstations.Where(item => |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 366 | Work Center | foreach (var workstation in filteredItems) |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 368 | Work Center | if (_hotWorkCenterNames.Contains(workstation.Name)) |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 370 | Work Center | _displayedHotWorkstations.Add(workstation); |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 374 | Work Center | _displayedOtherWorkstations.Add(workstation); |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 378 | Work Center | if (SelectedWorkstation is not null |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 379 | Work Center | && !_displayedHotWorkstations.Contains(SelectedWorkstation) |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 380 | Work Center | && !_displayedOtherWorkstations.Contains(SelectedWorkstation)) |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 382 | Work Center | SelectedWorkstation = _displayedHotWorkstations.FirstOrDefault() |
| `Module_Setup\ViewModels\SetupWorkstationViewModel.cs` | 383 | Work Center | ?? _displayedOtherWorkstations.FirstOrDefault(); |
| `Module_Setup\Views\SetupDunnageTypePage.xaml` | 33 | Review | <TextBlock x:Uid="Setup_DunnagePair.Header.WorkStation" Style="{StaticResource SetupFieldL |
| `Module_Setup\Views\SetupWorkOrderPage.xaml` | 93 | Review | <Button x:Uid="Setup_Action.BackToWorkstations" Content="Back" Command="{x:Bind ViewModel. |
| `Module_Setup\Views\SetupWorkstationPage.xaml` | 2 | Work Center | x:Class="MTM_Waitlist.Module_Setup.Views.SetupWorkstationPage" |
| `Module_Setup\Views\SetupWorkstationPage.xaml` | 11 | Work Center | <Style x:Key="WorkstationCardItemContainerStyle" TargetType="GridViewItem"> |
| `Module_Setup\Views\SetupWorkstationPage.xaml` | 35 | Work Center | <DataTemplate x:Key="WorkstationCardTemplate" x:DataType="models:SetupWorkstation"> |
| `Module_Setup\Views\SetupWorkstationPage.xaml` | 46 | Work Center | <MenuFlyoutItem Text="Edit" Tag="{x:Bind}" Click="OnEditWorkstationClick" /> |
| `Module_Setup\Views\SetupWorkstationPage.xaml` | 47 | Work Center | <MenuFlyoutItem Text="Remove" Tag="{x:Bind}" Click="OnRemoveWorkstationClick" /> |
| `Module_Setup\Views\SetupWorkstationPage.xaml` | 87 | Work Center | <!-- Right pane: stacked workstation details with section dividers. --> |
| `Module_Setup\Views\SetupWorkstationPage.xaml` | 142 | Work Center | <TextBlock x:Uid="Setup_Workstation.Title" Style="{StaticResource SetupPageTitleStyle}" Te |
| `Module_Setup\Views\SetupWorkstationPage.xaml` | 143 | Work Center | <TextBlock x:Uid="Setup_Workstation.Subtitle" Style="{StaticResource SetupPageSubtitleStyl |
| `Module_Setup\Views\SetupWorkstationPage.xaml` | 144 | Work Center | <TextBox x:Uid="Setup_Workstation.SearchInput" Header="Search" PlaceholderText="Search by  |
| `Module_Setup\Views\SetupWorkstationPage.xaml` | 156 | Work Center | x:Name="HotWorkstationsGridView" |
| `Module_Setup\Views\SetupWorkstationPage.xaml` | 158 | Work Center | ItemsSource="{x:Bind ViewModel.DisplayedHotWorkstations, Mode=OneWay}" |
| `Module_Setup\Views\SetupWorkstationPage.xaml` | 160 | Work Center | ItemClick="WorkstationCard_ItemClick" |
| `Module_Setup\Views\SetupWorkstationPage.xaml` | 161 | Work Center | ItemContainerStyle="{StaticResource WorkstationCardItemContainerStyle}" |
| `Module_Setup\Views\SetupWorkstationPage.xaml` | 162 | Work Center | ItemTemplate="{StaticResource WorkstationCardTemplate}" |
| `Module_Setup\Views\SetupWorkstationPage.xaml` | 166 | Work Center | SizeChanged="WorkstationGridView_SizeChanged"> |
| `Module_Setup\Views\SetupWorkstationPage.xaml` | 187 | Work Center | x:Name="OtherWorkstationsGridView" |
| `Module_Setup\Views\SetupWorkstationPage.xaml` | 189 | Work Center | ItemsSource="{x:Bind ViewModel.DisplayedOtherWorkstations, Mode=OneWay}" |
| `Module_Setup\Views\SetupWorkstationPage.xaml` | 191 | Work Center | ItemClick="WorkstationCard_ItemClick" |
| `Module_Setup\Views\SetupWorkstationPage.xaml` | 192 | Work Center | ItemContainerStyle="{StaticResource WorkstationCardItemContainerStyle}" |
| `Module_Setup\Views\SetupWorkstationPage.xaml` | 193 | Work Center | ItemTemplate="{StaticResource WorkstationCardTemplate}" |
| `Module_Setup\Views\SetupWorkstationPage.xaml` | 197 | Work Center | SizeChanged="WorkstationGridView_SizeChanged"> |
| `Module_Setup\Views\SetupWorkstationPage.xaml` | 217 | Work Center | <Button Grid.Column="0" Content="New Workstation" MinWidth="150" Click="OnNewWorkstationCl |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 11 | Work Center | public sealed partial class SetupWorkstationPage : Page |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 21 | Work Center | public SetupWorkstationViewModel ViewModel { get; } |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 23 | Work Center | public SetupWorkstationPage() |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 25 | Work Center | ViewModel = App.GetService<SetupWorkstationViewModel>(); |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 28 | Work Center | ViewModel.DisplayedHotWorkstations.CollectionChanged += OnWorkstationsCollectionChanged; |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 29 | Work Center | ViewModel.DisplayedOtherWorkstations.CollectionChanged += OnWorkstationsCollectionChanged; |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 33 | Work Center | ViewModel.DisplayedHotWorkstations.CollectionChanged -= OnWorkstationsCollectionChanged; |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 34 | Work Center | ViewModel.DisplayedOtherWorkstations.CollectionChanged -= OnWorkstationsCollectionChanged; |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 38 | Work Center | private void WorkstationGridView_SizeChanged(object sender, SizeChangedEventArgs e) |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 43 | Work Center | private void WorkstationCard_ItemClick(object sender, ItemClickEventArgs e) |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 45 | Work Center | if (e.ClickedItem is SetupWorkstation workstation) |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 47 | Work Center | ViewModel.SelectedWorkstation = workstation; |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 51 | Work Center | private void OnWorkstationsCollectionChanged(object? sender, NotifyCollectionChangedEventA |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 58 | Work Center | if (HotWorkstationsGridView is null \|\| OtherWorkstationsGridView is null) |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 63 | Work Center | var availableWidth = Math.Max(HotWorkstationsGridView.ActualWidth, OtherWorkstationsGridVi |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 74 | Work Center | var hotPanel = HotWorkstationsGridView.ItemsPanelRoot; |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 75 | Work Center | var otherPanel = OtherWorkstationsGridView.ItemsPanelRoot; |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 83 | Work Center | if ((hotPanel is null && HotWorkstationsGridView.Items.Count > 0 && HotWorkstationsGridVie |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 84 | Work Center | \|\| (otherPanel is null && OtherWorkstationsGridView.Items.Count > 0 && OtherWorkstationsGr |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 99 | Work Center | private async void OnNewWorkstationClick(object sender, RoutedEventArgs e) |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 101 | Work Center | if (!ViewModel.CanManageWorkstations) |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 106 | Work Center | ViewModel.SelectedWorkstation = null; |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 107 | Work Center | ViewModel.WorkstationNameInput = string.Empty; |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 112 | Work Center | Header = "Workstation", |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 113 | Work Center | PlaceholderText = "Enter workstation name", |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 118 | Work Center | var dialog = CreateWorkstationDialog("New Workstation", nameInput, buildingInput); |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 122 | Work Center | ViewModel.WorkstationNameInput = nameInput.Text; |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 124 | Work Center | if (ViewModel.AddWorkstationCommand.CanExecute(null)) |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 126 | Work Center | await ViewModel.AddWorkstationCommand.ExecuteAsync(null); |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 131 | Work Center | private async void OnEditWorkstationClick(object sender, RoutedEventArgs e) |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 133 | Work Center | if (!ViewModel.CanManageWorkstations \|\| sender is not MenuFlyoutItem { Tag: SetupWorkstati |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 138 | Work Center | ViewModel.SelectedWorkstation = workstation; |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 142 | Work Center | Header = "Workstation", |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 143 | Work Center | Text = workstation.Name, |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 146 | Work Center | var buildingInput = CreateBuildingComboBox(workstation.Building); |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 148 | Work Center | var dialog = CreateWorkstationDialog("Edit Workstation", nameInput, buildingInput); |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 152 | Work Center | ViewModel.WorkstationNameInput = nameInput.Text; |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 154 | Work Center | if (ViewModel.UpdateWorkstationCommand.CanExecute(null)) |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 156 | Work Center | await ViewModel.UpdateWorkstationCommand.ExecuteAsync(null); |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 161 | Work Center | private async void OnRemoveWorkstationClick(object sender, RoutedEventArgs e) |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 163 | Work Center | if (!ViewModel.CanManageWorkstations \|\| sender is not MenuFlyoutItem { Tag: SetupWorkstati |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 168 | Work Center | ViewModel.SelectedWorkstation = workstation; |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 169 | Work Center | if (ViewModel.RemoveWorkstationCommand.CanExecute(null)) |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 171 | Work Center | await ViewModel.RemoveWorkstationCommand.ExecuteAsync(null); |
| `Module_Setup\Views\SetupWorkstationPage.xaml.cs` | 186 | Work Center | private ContentDialog CreateWorkstationDialog(string title, TextBox nameInput, ComboBox bu |
| `Module_Shared\Models\WorkCenterCatalogResult.cs` | 5 | Review | public string WorkstationName { get; init; } = string.Empty; |
| `Module_Shared\Models\WorkCenterDetail.cs` | 6 | Work Center | /// <c>setup_workstations_catalog</c> metadata (building, updated_utc) with the latest |
| `Module_Shared\Models\WorkCenterSelectionItem.cs` | 7 | Review | private const string DefaultWorkCenterImagePath = "Assets/Images/default-workstation-image |
| `Module_Shared\Models\WorkCenterSelectionItem.cs` | 19 | Work Center | /// Read from <c>setup_workstations_catalog.updated_utc</c>. |
| `Module_Shared\Services\IWorkCenterCatalogService.cs` | 7 | Review | string GetCurrentWorkstationName(); |
| `Module_Shared\Services\IWorkCenterCatalogService.cs` | 9 | Review | Task<IReadOnlyList<string>> GetAvailableWorkstationsAsync(CancellationToken cancellationTo |
| `Module_Shared\Services\IWorkCenterCatalogService.cs` | 11 | Review | Task<WorkCenterCatalogResult> GetCatalogAsync(string workstationName, CancellationToken ca |
| `Module_Shared\Services\IWorkCenterCatalogService.cs` | 13 | Review | Task<string?> SaveHotWorkCentersAsync(string workstationName, IReadOnlyCollection<string>  |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 29 | Review | public string GetCurrentWorkstationName() |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 39 | Review | public async Task<IReadOnlyList<string>> GetAvailableWorkstationsAsync(CancellationToken c |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 42 | Review | @"SELECT workstation_name |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 43 | Computer | FROM core_workstations_registry |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 45 | Review | ORDER BY workstation_name ASC;", |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 50 | Review | var workstations = rows |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 51 | Review | .Select(row => GetValue(row, "workstation_name")) |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 57 | Review | var currentWorkstation = await ResolveCurrentWorkstationNameAsync(cancellationToken).Confi |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 58 | Review | if (!workstations.Any(value => string.Equals(value, currentWorkstation, StringComparison.O |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 60 | Review | workstations.Insert(0, currentWorkstation); |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 63 | Review | return workstations; |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 66 | Review | public async Task<WorkCenterCatalogResult> GetCatalogAsync(string workstationName, Cancell |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 68 | Review | var normalizedWorkstationName = string.IsNullOrWhiteSpace(workstationName) |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 69 | Review | ? await ResolveCurrentWorkstationNameAsync(cancellationToken).ConfigureAwait(false) |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 70 | Review | : workstationName.Trim(); |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 72 | Computer | StartupDebugLog.Info("WorkCenterCatalog", $"GetCatalogAsync started. Workstation='{normali |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 76 | Review | .Select(row => GetValue(row, "workstation_name")) |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 84 | Work Center | swc.workstation_name AS work_center_name, |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 86 | Work Center | FROM config_workstation_hot_workcenters cwhc |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 87 | Computer | INNER JOIN core_workstations_registry cwr ON cwr.id = cwhc.core_workstation_id |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 88 | Work Center | INNER JOIN setup_workstations_catalog swc ON swc.id = cwhc.setup_workstation_id |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 92 | Review | cwr.workstation_name = @p_workstation_name |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 93 | Review | OR cwr.hostname_normalized = @p_workstation_name |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 95 | Review | ORDER BY cwhc.sort_rank ASC, swc.workstation_name ASC;", |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 98 | Review | ["p_workstation_name"] = normalizedWorkstationName, |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 146 | Review | var workCenterName = GetValue(row, "workstation_name"); |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 166 | Review | WorkstationName = normalizedWorkstationName, |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 173 | Computer | StartupDebugLog.Info("WorkCenterCatalog", $"GetCatalogAsync completed. Workstation='{norma |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 177 | Review | public async Task<string?> SaveHotWorkCentersAsync(string workstationName, IReadOnlyCollec |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 181 | Review | var normalizedWorkstationName = string.IsNullOrWhiteSpace(workstationName) |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 182 | Review | ? await ResolveCurrentWorkstationNameAsync(cancellationToken).ConfigureAwait(false) |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 183 | Review | : workstationName.Trim(); |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 185 | Computer | StartupDebugLog.Info("WorkCenterCatalog", $"SaveHotWorkCentersAsync started. Workstation=' |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 187 | Review | var workstationRows = await _mySqlHelperServer.ExecuteSqlQueryAsync( |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 189 | Computer | FROM core_workstations_registry |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 190 | Review | WHERE workstation_name = @p_workstation_name |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 191 | Review | OR hostname_normalized = @p_workstation_name |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 192 | Review | ORDER BY CASE WHEN workstation_name = @p_workstation_name THEN 0 ELSE 1 END |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 196 | Review | ["p_workstation_name"] = normalizedWorkstationName, |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 201 | Review | var workstationId = GetInt64(workstationRows.FirstOrDefault(), "id"); |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 202 | Review | if (workstationId <= 0) |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 204 | Computer | StartupDebugLog.Info("WorkCenterCatalog", $"SaveHotWorkCentersAsync aborted. Workstation ' |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 205 | Review | return "Unable to save Local workcenters: workstation not found."; |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 209 | Review | @"SELECT id, workstation_name |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 210 | Work Center | FROM setup_workstations_catalog |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 220 | Review | Name = GetValue(row, "workstation_name"), |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 237 | Work Center | SetupWorkstationId = workCenterIdByName.TryGetValue(workCenterName, out var setupWorkstati |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 238 | Work Center | ? setupWorkstationId |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 241 | Work Center | .Where(item => item.SetupWorkstationId > 0) |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 247 | Computer | StartupDebugLog.Info("WorkCenterCatalog", $"SaveHotWorkCentersAsync aborted. No database c |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 264 | Work Center | DELETE FROM config_workstation_hot_workcenters |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 265 | Computer | WHERE core_workstation_id = @p_core_workstation_id;", connection, transaction)) |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 268 | Computer | deleteCommand.Parameters.AddWithValue("@p_core_workstation_id", workstationId); |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 275 | Work Center | insertSql.AppendLine("INSERT INTO config_workstation_hot_workcenters ("); |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 276 | Computer | insertSql.AppendLine("    core_workstation_id,"); |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 277 | Work Center | insertSql.AppendLine("    setup_workstation_id,"); |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 295 | Work Center | insertSql.Append($"(@p_core_workstation_id, @p_setup_workstation_id_{parameterSuffix}, UUI |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 307 | Computer | insertCommand.Parameters.AddWithValue("@p_core_workstation_id", workstationId); |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 313 | Work Center | insertCommand.Parameters.AddWithValue($"@p_setup_workstation_id_{index}", item.SetupWorkst |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 321 | Computer | StartupDebugLog.Info("WorkCenterCatalog", $"SaveHotWorkCentersAsync completed. Workstation |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 326 | Computer | StartupDebugLog.Error("WorkCenterCatalog", ex, $"SaveHotWorkCentersAsync failed. Workstati |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 334 | Review | @"SELECT workstation_name, building, updated_utc |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 335 | Work Center | FROM setup_workstations_catalog |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 337 | Review | ORDER BY sort_rank ASC, workstation_name ASC;", |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 345 | Review | private async Task<string> ResolveCurrentWorkstationNameAsync(CancellationToken cancellati |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 347 | Review | var key = GetCurrentWorkstationName(); |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 354 | Review | @"SELECT workstation_name |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 355 | Computer | FROM core_workstations_registry |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 356 | Review | WHERE workstation_name = @p_workstation_name |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 357 | Review | OR hostname_normalized = @p_workstation_name |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 358 | Review | ORDER BY CASE WHEN workstation_name = @p_workstation_name THEN 0 ELSE 1 END |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 362 | Review | ["p_workstation_name"] = key, |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 367 | Review | var workstationName = GetValue(rows.FirstOrDefault(), "workstation_name"); |
| `Module_Shared\Services\WorkCenterCatalogService.cs` | 368 | Review | return string.IsNullOrWhiteSpace(workstationName) ? key : workstationName; |
| `Module_Startup\Models\StartupSessionSnapshot.cs` | 7 | Computer | public bool IsWorkstationRegistered { get; init; } |
| `Module_Startup\Models\StartupSessionSnapshot.cs` | 9 | Computer | public bool IsWorkstationRegistrationAuthoritative { get; init; } = true; |
| `Module_Startup\Models\StartupState.cs` | 33 | Computer | public bool IsWorkstationRegistered { get; set; } |
| `Module_Startup\Models\StartupState.cs` | 35 | Computer | public bool IsWorkstationRegistrationAuthoritative { get; set; } |
| `Module_Startup\Services\StartupCoordinator.cs` | 168 | Computer | var isWorkstationRegistered = sessionSnapshot.IsWorkstationRegistered; |
| `Module_Startup\Services\StartupCoordinator.cs` | 169 | Computer | _startupState.IsWorkstationRegistered = isWorkstationRegistered; |
| `Module_Startup\Services\StartupCoordinator.cs` | 170 | Computer | _startupState.IsWorkstationRegistrationAuthoritative = sessionSnapshot.IsWorkstationRegist |
| `Module_Startup\Services\StartupCoordinator.cs` | 225 | Computer | _startupState.IsWorkstationRegistrationAuthoritative |
| `Module_Startup\Services\StartupCoordinator.cs` | 227 | Computer | && !isWorkstationRegistered; |
| `Module_Startup\Services\StartupCoordinator.cs` | 240 | Computer | ? "This workstation is not registered. Choose New User to request access." |
| `Module_Startup\Services\StartupCoordinator.cs` | 246 | Computer | $"Startup routed to login. UserMatched={isUserMatched}, WorkstationRegistered={isWorkstati |
| `Module_Startup\Services\StartupSessionRepository.cs` | 65 | Computer | IsWorkstationRegistered = false, |
| `Module_Startup\Services\StartupSessionRepository.cs` | 66 | Computer | IsWorkstationRegistrationAuthoritative = false, |
| `Module_Startup\Services\StartupSessionRepository.cs` | 79 | Computer | var workstationRegistered = await ReadWorkstationRegisteredAsync(connection, hostnameNorma |
| `Module_Startup\Services\StartupSessionRepository.cs` | 87 | Computer | IsWorkstationRegistered = workstationRegistered, |
| `Module_Startup\Services\StartupSessionRepository.cs` | 88 | Computer | IsWorkstationRegistrationAuthoritative = true, |
| `Module_Startup\Services\StartupSessionRepository.cs` | 100 | Computer | IsWorkstationRegistered = workstationRegistered, |
| `Module_Startup\Services\StartupSessionRepository.cs` | 101 | Computer | IsWorkstationRegistrationAuthoritative = true, |
| `Module_Startup\Services\StartupSessionRepository.cs` | 228 | Computer | private static async Task<bool> ReadWorkstationRegisteredAsync( |
| `Module_Startup\Services\StartupSessionRepository.cs` | 237 | Computer | FROM core_workstations_registry |
| `Module_Waitlist\Models\NewRequestFlowState.cs` | 36 | Review | WorkstationName = WorkCenter.Trim(), |
| `Module_Waitlist\Models\SampleOrder.cs` | 30 | Review | ? "Assets/Images/default-workstation-image.png" |
| `Module_Waitlist\Models\WaitlistRequest.cs` | 12 | Review | public string WorkstationName { get; init; } = string.Empty; |
| `Module_Waitlist\Models\WaitlistRequestDraft.cs` | 11 | Review | public string WorkstationName { get; init; } = string.Empty; |
| `Module_Waitlist\Services\WaitlistRequestService.cs` | 100 | Review | WorkstationName = existing.WorkstationName, |
| `Module_Waitlist\Services\WaitlistRequestService.cs` | 164 | Review | if (string.IsNullOrWhiteSpace(draft.WorkstationName)) |
| `Module_Waitlist\Services\WaitlistRequestService.cs` | 166 | Review | return WaitlistRequestSubmitResult.ValidationFailure("The current workstation name is requ |
| `Module_Waitlist\Services\WaitlistRequestService.cs` | 196 | Review | WorkstationName = draft.WorkstationName.Trim(), |
| `Module_Waitlist\Services\WaitlistRequestService.cs` | 227 | Review | ["p_workstation_name"] = request.WorkstationName, |
| `Module_Waitlist\ViewModels\NewRequestWorkCenterViewModel.cs` | 23 | Review | private const string DefaultWorkCenterImagePath = "Assets/Images/default-workstation-image |
| `Module_Waitlist\ViewModels\NewRequestWorkCenterViewModel.cs` | 36 | Review | public partial string WorkstationName |
| `Module_Waitlist\ViewModels\NewRequestWorkCenterViewModel.cs` | 146 | Review | var workstationName = _workCenterCatalogService.GetCurrentWorkstationName(); |
| `Module_Waitlist\ViewModels\NewRequestWorkCenterViewModel.cs` | 147 | Review | var catalog = await _workCenterCatalogService.GetCatalogAsync(workstationName).ConfigureAw |
| `Module_Waitlist\ViewModels\NewRequestWorkCenterViewModel.cs` | 150 | Review | WorkstationName = catalog.WorkstationName; |
| `Module_Waitlist\ViewModels\NewRequestWorkCenterViewModel.cs` | 171 | Computer | StartupDebugLog.Info("NewRequestWorkCenter", $"Catalog loaded. Workstation='{catalog.Works |
| `Module_Waitlist\ViewModels\NewRequestWorkCenterViewModel.cs` | 176 | Review | WorkstationName = string.Empty; |
| `Module_Waitlist\ViewModels\NewRequestWorkCenterViewModel.cs` | 283 | Computer | StartupDebugLog.Info("NewRequestWorkCenter", $"Blocked workstation selection for '{normali |
| `Module_Waitlist\ViewModels\NewRequestWorkCenterViewModel.cs` | 303 | Computer | StartupDebugLog.Info("NewRequestWorkCenter", $"Selected workstation '{normalizedWorkCenter |
| `Module_Waitlist\Views\NewRequestWorkCenterPage.xaml` | 135 | Review | <TextBlock Style="{StaticResource SetupPageSubtitleStyle}" Text="{x:Bind ViewModel.Worksta |
| `MTM_Waitlist.Tests\Models\StartupModelsTests.cs` | 44 | Review | Assert.IsFalse(state.IsWorkstationRegistered); |
| `MTM_Waitlist.Tests\Module_Settings\ImageOverrideDialogViewModelTests.cs` | 324 | Review | WorkstationName = "test", |
| `MTM_Waitlist.Tests\Module_Settings\ImageOverrideDialogViewModelTests.cs` | 345 | Review | _catalog.Catalog = new WorkCenterCatalogResult { WorkstationName = "test" }; |
| `MTM_Waitlist.Tests\Module_Settings\ImageOverrideDialogViewModelTests.cs` | 360 | Review | WorkstationName = "test", |
| `MTM_Waitlist.Tests\Module_Settings\ImageOverrideDialogViewModelTests.cs` | 378 | Review | ["workstation_name"] = name, |
| `MTM_Waitlist.Tests\Module_Settings\TestDoubles.cs` | 167 | Review | public string GetCurrentWorkstationName() => "test-workstation"; |
| `MTM_Waitlist.Tests\Module_Settings\TestDoubles.cs` | 169 | Review | public Task<IReadOnlyList<string>> GetAvailableWorkstationsAsync(CancellationToken cancell |
| `MTM_Waitlist.Tests\Module_Settings\TestDoubles.cs` | 170 | Review | Task.FromResult<IReadOnlyList<string>>(new[] { "test-workstation" }); |
| `MTM_Waitlist.Tests\Module_Settings\TestDoubles.cs` | 172 | Review | public Task<WorkCenterCatalogResult> GetCatalogAsync(string workstationName, CancellationT |
| `MTM_Waitlist.Tests\Module_Settings\TestDoubles.cs` | 175 | Review | public Task<string?> SaveHotWorkCentersAsync(string workstationName, IReadOnlyCollection<s |
| `MTM_Waitlist.Tests\Module_Waitlist\Models\NewRequestFlowStateTests.cs` | 34 | Review | Assert.AreEqual("Press 12", draft.WorkstationName); |
| `MTM_Waitlist.Tests\Module_Waitlist\Services\WaitlistRequestServiceTests.cs` | 58 | Review | WorkstationName = draft.WorkstationName, |
| `MTM_Waitlist.Tests\Module_Waitlist\Services\WaitlistRequestServiceTests.cs` | 118 | Review | WorkstationName = "Press 12", |
| `MTM_Waitlist.Tests\Module_Waitlist\Services\WaitlistRequestServiceTests.cs` | 132 | Review | Assert.AreEqual("Press 12", result.Request.WorkstationName); |
| `MTM_Waitlist.Tests\Module_Waitlist\Services\WaitlistRequestServiceTests.cs` | 153 | Review | WorkstationName = "Press 12", |
| `MTM_Waitlist.Tests\Module_Waitlist\Services\WaitlistRequestServiceTests.cs` | 649 | Review | WorkstationName = "Press 12", |
| `MTM_Waitlist.Tests\Module_Waitlist\ViewModels\NewRequestWorkCenterViewModelTests.cs` | 299 | Review | WorkstationName = "test-workstation", |
| `MTM_Waitlist.Tests\Module_Waitlist\ViewModels\NewRequestWorkCenterViewModelTests.cs` | 311 | Review | public string GetCurrentWorkstationName() => "test-workstation"; |
| `MTM_Waitlist.Tests\Module_Waitlist\ViewModels\NewRequestWorkCenterViewModelTests.cs` | 313 | Review | public Task<IReadOnlyList<string>> GetAvailableWorkstationsAsync(CancellationToken cancell |
| `MTM_Waitlist.Tests\Module_Waitlist\ViewModels\NewRequestWorkCenterViewModelTests.cs` | 314 | Review | Task.FromResult<IReadOnlyList<string>>(new[] { "test-workstation" }); |
| `MTM_Waitlist.Tests\Module_Waitlist\ViewModels\NewRequestWorkCenterViewModelTests.cs` | 316 | Review | public Task<WorkCenterCatalogResult> GetCatalogAsync(string workstationName, CancellationT |
| `MTM_Waitlist.Tests\Module_Waitlist\ViewModels\NewRequestWorkCenterViewModelTests.cs` | 319 | Review | public Task<string?> SaveHotWorkCentersAsync(string workstationName, IReadOnlyCollection<s |
| `MTM_Waitlist.Tests\Services\StartupCoordinatorTests.cs` | 59 | Computer | IsWorkstationRegistered = true, |
| `MTM_Waitlist.Tests\Services\StartupCoordinatorTests.cs` | 98 | Computer | IsWorkstationRegistered = true, |
| `MTM_Waitlist.Tests\Services\StartupCoordinatorTests.cs` | 144 | Computer | IsWorkstationRegistered = true, |
| `MTM_Waitlist.Tests\Services\StartupCoordinatorTests.cs` | 197 | Computer | IsWorkstationRegistered = true, |
| `MTM_Waitlist.Tests\Services\StartupCoordinatorTests.cs` | 223 | Computer | public async Task RunAsync_WhenUnknownWorkstation_RoutesToLoginAndRequiresNewUserActionAsy |
| `MTM_Waitlist.Tests\Services\StartupCoordinatorTests.cs` | 238 | Computer | IsWorkstationRegistered = false, |
| `MTM_Waitlist.Tests\Services\StartupCoordinatorTests.cs` | 259 | Computer | Assert.IsTrue(startupState.IsWorkstationRegistrationAuthoritative); |
| `MTM_Waitlist.Tests\Services\StartupCoordinatorTests.cs` | 261 | Computer | Assert.AreEqual("This workstation is not registered. Choose New User to request access.",  |
| `MTM_Waitlist.Tests\Services\StartupCoordinatorTests.cs` | 265 | Computer | public async Task RunAsync_WhenWorkstationStatusIsNotAuthoritative_RoutesToLoginWithoutNew |
| `MTM_Waitlist.Tests\Services\StartupCoordinatorTests.cs` | 280 | Computer | IsWorkstationRegistered = false, |
| `MTM_Waitlist.Tests\Services\StartupCoordinatorTests.cs` | 281 | Computer | IsWorkstationRegistrationAuthoritative = false, |
| `MTM_Waitlist.Tests\Services\StartupCoordinatorTests.cs` | 302 | Computer | Assert.IsFalse(startupState.IsWorkstationRegistrationAuthoritative); |
| `MTM_Waitlist.Tests\Services\StartupCoordinatorTests.cs` | 323 | Computer | IsWorkstationRegistered = true, |
| `MTM_Waitlist.Tests\Services\StartupCoordinatorTests.cs` | 595 | Computer | IsWorkstationRegistered = true, |
| `MTM_Waitlist.Tests\ViewModels\LoginViewModelTests.cs` | 22 | Review | HostnameNormalized = "dev-workstation-001", |
| `MTM_Waitlist.Tests\ViewModels\LoginViewModelTests.cs` | 25 | Review | LoginHint = "This workstation is not registered. Choose New User to request access." |
| `StartupPhases\Phase-01-Startup-Shell-and-Splash-Complete.md` | 70 | Review | - Unknown workstation routing: Show Login first, including a New User button. |
| `StartupPhases\Phase-03-Identity-and-Workstation-Checks.md` | 1 | Review | # Phase 03 - Identity and Workstation Checks |
| `StartupPhases\Phase-03-Identity-and-Workstation-Checks.md` | 7 | Computer | - Database-backed startup session repository resolves workstation registration using hostn |
| `StartupPhases\Phase-03-Identity-and-Workstation-Checks.md` | 9 | Computer | - `New User` routing is now gated by authoritative workstation registration status to avoi |
| `StartupPhases\Phase-03-Identity-and-Workstation-Checks.md` | 10 | Computer | - Startup runtime context now persists `IsWorkstationRegistrationAuthoritative` for explic |
| `StartupPhases\Phase-03-Identity-and-Workstation-Checks.md` | 16 | Review | - End-to-end role and workstation verification against live production-like data sets. |
| `StartupPhases\Phase-03-Identity-and-Workstation-Checks.md` | 20 | Computer | - `MTM_Waitlist.Tests/Services/StartupCoordinatorTests.cs` (`RunAsync_WhenUnknownWorkstati |
| `StartupPhases\Phase-03-Identity-and-Workstation-Checks.md` | 21 | Computer | - `Database/Bootstrap/create_database.sql`, `Database/Tables/auth_roles_catalog/create.sql |
| `StartupPhases\Phase-03-Identity-and-Workstation-Checks.md` | 30 | Computer | Implement startup identity lookups and workstation registration checks. |
| `StartupPhases\Phase-03-Identity-and-Workstation-Checks.md` | 35 | Review | - Validate workstation using hostname plus MAC as composite key. |
| `StartupPhases\Phase-03-Identity-and-Workstation-Checks.md` | 36 | Review | - Enforce no manual override for unknown workstation. |
| `StartupPhases\Phase-03-Identity-and-Workstation-Checks.md` | 41 | Computer | - Create and organize startup schema artifacts under `./Database` (users, workstations, se |
| `StartupPhases\Phase-03-Identity-and-Workstation-Checks.md` | 43 | Review | - Add workstation lookup query using hostname and MAC. |
| `StartupPhases\Phase-03-Identity-and-Workstation-Checks.md` | 44 | Computer | - Add explicit startup branch for unregistered workstation that routes to Login first and  |
| `StartupPhases\Phase-03-Identity-and-Workstation-Checks.md` | 54 | Computer | - Startup context includes `userMatched` and `workstationMatched` results. |
| `StartupPhases\Phase-03-Identity-and-Workstation-Checks.md` | 55 | Computer | - Startup context includes authoritative verification state for workstation registration. |
| `StartupPhases\Phase-03-Identity-and-Workstation-Checks.md` | 56 | Review | - Unknown workstation routing follows authoritative-status rules with no override. |
| `StartupPhases\Phase-03-Identity-and-Workstation-Checks.md` | 62 | Review | 1. Known user plus known workstation returns matched state. |
| `StartupPhases\Phase-03-Identity-and-Workstation-Checks.md` | 63 | Review | 2. Unknown user plus known workstation routes to Login branch. |
| `StartupPhases\Phase-03-Identity-and-Workstation-Checks.md` | 64 | Review | 3. Unknown workstation routes to Login first with a New User option and does not offer an  |
| `StartupPhases\Phase-05-Session-Validation-and-Routing-Complete.md` | 10 | Review | - Unknown workstation and unmatched user branch now sets a `New User` action requirement s |
| `StartupPhases\Phase-05-Session-Validation-and-Routing-Complete.md` | 31 | Review | - Route to Main Window or Login (with New User available from Login for unknown users/work |
| `StartupPhases\Phase-05-Session-Validation-and-Routing-Complete.md` | 58 | Review | 3. Unmatched user plus unregistered workstation routes to Login first, with New User avail |
| `StartupPhases\Phase-09-Role-Enforcement-and-Final-Polish.md` | 9 | Review | - Explicit role gates for Waitlist and Work Stations module actions. |
| `StartupPhases\Phase-09-Role-Enforcement-and-Final-Polish.md` | 26 | Review | - Apply RBAC in Waitlist and Work Stations modules. |
| `StartupPhases\Phase-09-Role-Enforcement-and-Final-Polish.md` | 35 | Review | - Enforce role gates for Work Stations actions. |
| `StartupPhases\README.md` | 9 | Review | - Phase 03: [Phase-03-Identity-and-Workstation-Checks.md](Phase-03-Identity-and-Workstatio |
| `StartupPhases\README.md` | 26 | Review | \| Phase 03 \| In Progress \| Database-backed username/workstation checks are implemented wit |
| `StartupPhases\README.md` | 56 | Review | ### Phase 03 - Identity and Workstation Checks |
| `StartupPhases\README.md` | 59 | Review | - [x] Validate workstation using hostname plus MAC as a composite key. |
| `StartupPhases\README.md` | 61 | Review | - [x] Gate `New User` routing behind authoritative workstation registration status. |
| `StartupPhases\README.md` | 62 | Computer | - [x] Persist `IsWorkstationRegistrationAuthoritative` in startup runtime state. |
| `StartupPhases\README.md` | 65 | Review | - [ ] Validate role and workstation behavior against live production-like data sets. |
| `StartupPhases\README.md` | 83 | Review | - [x] Surface `New User` as part of the Login branch for unknown workstation or user cases |
| `StartupPhases\README.md` | 117 | Review | - [ ] Enforce role gates for Work Stations actions. |
| `StartupPhases\README.md` | 142 | Review | - Unknown workstation routing: Show Login first. Only show `New User` when workstation sta |
| `StartupPhases\README.md` | 147 | Review | - Workstation Validation: Uses standard Windows APIs to form a composite key (Hostname + M |
| `StartupPhases\README.md` | 176 | Review | ## Phase 03: Identity and Workstation Checks |
| `StartupPhases\README.md` | 180 | Review | - No Override Policy: If the workstation record is unknown, no manual override path is all |
| `StartupPhases\StartupPhases-Prompt.md` | 31 | Computer | 4. **In Progress:** `StartupPhases/Phase-03-Identity-and-Workstation-Checks.md` |
| `Strings\en-us\Resources.resw` | 328 | Review | <data name="Setup_DunnagePair.Header.WorkStation.Text" xml:space="preserve"> |
| `Strings\en-us\Resources.resw` | 329 | Review | <value>WORK STATION</value> |
| `Strings\en-us\Resources.resw` | 373 | Review | <data name="Setup_Workstation.Title.Text" xml:space="preserve"> |
| `Strings\en-us\Resources.resw` | 374 | Review | <value>Select Workstation</value> |
| `Strings\en-us\Resources.resw` | 376 | Review | <data name="Setup_Workstation.Subtitle.Text" xml:space="preserve"> |
| `Strings\en-us\Resources.resw` | 379 | Review | <data name="Setup_Workstation.SearchInput.Header" xml:space="preserve"> |
| `Strings\en-us\Resources.resw` | 382 | Review | <data name="Setup_Workstation.SearchInput.PlaceholderText" xml:space="preserve"> |
| `Strings\en-us\Resources.resw` | 383 | Review | <value>Search by workstation, work order, sequence, or part number</value> |
| `Strings\en-us\Resources.resw` | 385 | Review | <data name="Setup_Workstation.ManageTitle.Text" xml:space="preserve"> |
| `Strings\en-us\Resources.resw` | 386 | Review | <value>Manage Workstations</value> |
| `Strings\en-us\Resources.resw` | 388 | Review | <data name="Setup_Workstation.NameInput.Header" xml:space="preserve"> |
| `Strings\en-us\Resources.resw` | 389 | Review | <value>Workstation</value> |
| `Strings\en-us\Resources.resw` | 391 | Review | <data name="Setup_Workstation.NameInput.PlaceholderText" xml:space="preserve"> |
| `Strings\en-us\Resources.resw` | 392 | Review | <value>Enter workstation name</value> |
| `Strings\en-us\Resources.resw` | 394 | Review | <data name="Setup_Workstation.Add.Content" xml:space="preserve"> |
| `Strings\en-us\Resources.resw` | 397 | Review | <data name="Setup_Workstation.Update.Content" xml:space="preserve"> |
| `Strings\en-us\Resources.resw` | 400 | Review | <data name="Setup_Workstation.Remove.Content" xml:space="preserve"> |
| `Strings\en-us\Resources.resw` | 403 | Review | <data name="Setup_Workstation.New.Content" xml:space="preserve"> |
| `Strings\en-us\Resources.resw` | 404 | Review | <value>New Workstation</value> |
| `Strings\en-us\Resources.resw` | 406 | Review | <data name="Setup_Workstation.ManageHint.Text" xml:space="preserve"> |
| `Strings\en-us\Resources.resw` | 407 | Review | <value>Setup Tech, Admin, and above can manage workstations.</value> |
| `Strings\en-us\Resources.resw` | 409 | Review | <data name="Setup_Action.BackToWorkstations.Content" xml:space="preserve"> |
| `Strings\en-us\Resources.resw` | 508 | Review | <value>Work Station</value> |
| `Strings\en-us\TooltipResources.developer.resw` | 192 | Work Center | <data name="Setup_SetupWorkstationPage_TextBox1_Tooltip" xml:space="preserve"> |
| `Strings\en-us\TooltipResources.developer.resw` | 195 | Work Center | <data name="Setup_SetupWorkstationPage_GridView1_Tooltip" xml:space="preserve"> |
| `Strings\en-us\TooltipResources.developer.resw` | 198 | Work Center | <data name="Setup_SetupWorkstationPage_MenuFlyoutItem1_Tooltip" xml:space="preserve"> |
| `Strings\en-us\TooltipResources.developer.resw` | 201 | Work Center | <data name="Setup_SetupWorkstationPage_MenuFlyoutItem2_Tooltip" xml:space="preserve"> |
| `Strings\en-us\TooltipResources.developer.resw` | 204 | Work Center | <data name="Setup_SetupWorkstationPage_Button1_Tooltip" xml:space="preserve"> |
| `Strings\en-us\TooltipResources.developer.resw` | 207 | Work Center | <data name="Setup_SetupWorkstationPage_Button2_Tooltip" xml:space="preserve"> |
| `Strings\en-us\TooltipResources.developer.resw` | 210 | Work Center | <data name="Setup_SetupWorkstationPage_Button3_Tooltip" xml:space="preserve"> |
| `Strings\en-us\TooltipResources.resw` | 192 | Work Center | <data name="Setup_SetupWorkstationPage_TextBox1_Tooltip" xml:space="preserve"> |
| `Strings\en-us\TooltipResources.resw` | 193 | Review | <value>Search by workstation, work order, sequence, or part number.</value> |
| `Strings\en-us\TooltipResources.resw` | 195 | Work Center | <data name="Setup_SetupWorkstationPage_GridView1_Tooltip" xml:space="preserve"> |
| `Strings\en-us\TooltipResources.resw` | 198 | Work Center | <data name="Setup_SetupWorkstationPage_MenuFlyoutItem1_Tooltip" xml:space="preserve"> |
| `Strings\en-us\TooltipResources.resw` | 201 | Work Center | <data name="Setup_SetupWorkstationPage_MenuFlyoutItem2_Tooltip" xml:space="preserve"> |
| `Strings\en-us\TooltipResources.resw` | 204 | Work Center | <data name="Setup_SetupWorkstationPage_Button1_Tooltip" xml:space="preserve"> |
| `Strings\en-us\TooltipResources.resw` | 205 | Review | <value>New Workstation.</value> |
| `Strings\en-us\TooltipResources.resw` | 207 | Work Center | <data name="Setup_SetupWorkstationPage_Button2_Tooltip" xml:space="preserve"> |
| `Strings\en-us\TooltipResources.resw` | 210 | Work Center | <data name="Setup_SetupWorkstationPage_Button3_Tooltip" xml:space="preserve"> |

