# Rename Scan Results - workstation -> Computer / Work Center

Generated: 2026-08-29 22:14:30 -05:00
Repo: C:\Users\johnk\source\repos\MTM_Waitlist
Pattern: `workstation` / `Work Station` (case-insensitive)
Total matches: 312 in 49 files
Files/folders to rename: 12

Excluded (generated/binary/SCM): bin, obj, .git, .vs, node_modules, TestResults, packages, .serena, pri_dump; binary exts: .png, .jpg, .jpeg, .gif, .webp, .ico, .dll, .exe, .pdb, .pri, .dgspec, .snupkg, .nupkg, .db, .bdb, .dat, .ttf, .otf; log/PRI/tool files: .log + Log.md, testout.txt, testerr.txt, pri_dump.xml, scan_workstation_rename.ps1, Rename_Scan_Results.md

> Category is a heuristic. Items marked **Review** need a human decision (computer vs work center).

## Summary by file

| File | Count | Categories |
|---|---|---|
| `.github\copilot-instructions.md` | 1 | Work Center |
| `.github\instructions\database-schema-rules.instructions.md` | 2 | Computer, Review |
| `.github\scripts\restructure-database-layout.ps1` | 5 | Computer, Review |
| `.github\scripts\Test-DeploymentPreflight.ps1` | 3 | Computer, Review |
| `Database\Database-Ruleset.md` | 7 | Computer, Review |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 25 | Computer, Review, Work Center |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 27 | Computer, Review, Work Center |
| `Documents\Development\CompletedImplementations\Complete-Waitlist-Request-Workflow-Checklist.md` | 4 | Review |
| `Documents\Development\CompletedImplementations\Image-Location-Settings-Spec.md` | 7 | Review, Work Center |
| `Documents\Development\UserManagement\User-Management-Clarifying-Questions.md` | 1 | Work Center |
| `Module_Settings\Views\SettingsPage.xaml` | 2 | Review |
| `Module_Setup\Views\SetupDunnageTypePage.xaml` | 1 | Review |
| `Module_Setup\Views\SetupWorkCenterPage.xaml` | 4 | Review |
| `Module_Setup\Views\SetupWorkCenterPage.xaml.cs` | 11 | Review |
| `Module_Setup\Views\SetupWorkOrderPage.xaml` | 1 | Review |
| `Module_Waitlist\Views\NewRequestWorkCenterPage.xaml` | 1 | Review |
| `MTM_Waitlist.Settings\Models\ConfigSettingValue.cs` | 4 | Review |
| `MTM_Waitlist.Settings\Models\ImageLocation.cs` | 5 | Review, Work Center |
| `MTM_Waitlist.Settings\Models\ImageLocationDefaults.cs` | 1 | Review |
| `MTM_Waitlist.Settings\Models\ImageLocationScope.cs` | 2 | Review, Work Center |
| `MTM_Waitlist.Settings\Models\ImageOverride.cs` | 1 | Work Center |
| `MTM_Waitlist.Settings\Models\ImageStorageOptions.cs` | 1 | Review |
| `MTM_Waitlist.Settings\Models\WorkCenterInventory.cs` | 12 | Review, Work Center |
| `MTM_Waitlist.Settings\Services\IImageLocationService.cs` | 2 | Work Center |
| `MTM_Waitlist.Settings\Services\IImageOverrideReadService.cs` | 1 | Work Center |
| `MTM_Waitlist.Settings\Services\ImageLocationService.cs` | 1 | Review |
| `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs` | 32 | Computer, Review |
| `MTM_Waitlist.Settings\ViewModels\WorkCenterImagesDialogViewModel.cs` | 1 | Work Center |
| `MTM_Waitlist.Setup\Contracts\Services\SetupContracts.cs` | 3 | Review |
| `MTM_Waitlist.Setup\Models\SetupModels.cs` | 6 | Review, Work Center |
| `MTM_Waitlist.Setup\Services\SetupPersistenceService.cs` | 1 | Work Center |
| `MTM_Waitlist.Setup\Services\SetupWorkCenterService.cs` | 19 | Review, Work Center |
| `MTM_Waitlist.Setup\ViewModels\SetupWorkCenterViewModel.cs` | 17 | Review, Work Center |
| `MTM_Waitlist.Shared\Models\WorkCenterDetail.cs` | 1 | Work Center |
| `MTM_Waitlist.Shared\Models\WorkCenterSelectionItem.cs` | 2 | Review, Work Center |
| `MTM_Waitlist.Shared\Services\IWorkCenterCatalogService.cs` | 2 | Review |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 41 | Computer, Review, Work Center |
| `MTM_Waitlist.Tests\Module_Settings\ImageOverrideDialogViewModelTests.cs` | 1 | Review |
| `MTM_Waitlist.Tests\Module_Settings\TestDoubles.cs` | 4 | Computer, Review |
| `MTM_Waitlist.Tests\Module_Waitlist\ViewModels\NewRequestWorkCenterViewModelTests.cs` | 5 | Computer, Review |
| `MTM_Waitlist.Tests\Services\StartupCoordinatorTests.cs` | 2 | Computer |
| `MTM_Waitlist.Tests\ViewModels\LoginViewModelTests.cs` | 2 | Review |
| `MTM_Waitlist.Waitlist.NewRequest\ViewModels\NewRequestWorkCenterViewModel.cs` | 9 | Computer, Review |
| `MTM_Waitlist.Waitlist.View\Models\SampleOrder.cs` | 1 | Review |
| `MTM_Waitlist.Waitlist.View\Services\WaitlistRequestService.cs` | 1 | Review |
| `Strings\en-us\Resources.resw` | 14 | Review |
| `Strings\en-us\TooltipResources.developer.resw` | 7 | Work Center |
| `Strings\en-us\TooltipResources.resw` | 7 | Work Center |
| `ViewModels\ShellViewModel.cs` | 2 | Review |

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

## Detailed edit map

| File | Line | Category | Matched text |
|---|---|---|---|
| `.github\copilot-instructions.md` | 44 | Work Center | - Interaction notes (2026-08-22): Setup/New Request work center card selection is **model- |
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
| `Documents\Computer_FirstLoad_Gate_Design.md` | 10 | Review | This design separates two concepts that both currently use the word "workstation", and **e |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 14 | Computer | \| **Computer** \| A physical machine (hostname e.g. `johnspc`, display name e.g. "John's Co |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 15 | Work Center | \| **Work Center** \| A press/work station (e.g. `100-3`, `100-6`). Selected in Module_Setup |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 17 | Computer | **Definition of Done (rename):** After the rename, the term "Workstation" / "workstation"  |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 21 | Computer | ## 2. Data Model — `core_workstations_registry` → `core_computers_registry` |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 23 | Review | Rename the table and `workstation_name` column; add two columns: |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 25 | Computer | - `computer_name VARCHAR(128) NOT NULL` (renamed from `workstation_name`) |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 37 | Computer | - `auth_sessions_tokens.workstation_id` → `computer_id` |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 38 | Computer | - `config_settings_values.workstation_id` → `computer_id` |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 39 | Computer | - `config_settings_history.workstation_id` → `computer_id` |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 40 | Work Center | - `config_workstation_hot_workcenters.core_workstation_id` → `computer_id` (computer side  |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 46 | Computer | The work-center concept is **renamed from "Workstation" to "Work Center"** (never "Compute |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 48 | Work Center | - `setup_workstations_catalog` → `setup_work_centers_catalog` |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 49 | Work Center | - `sp_setup_workstations_*` → `sp_setup_work_centers_*` |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 50 | Work Center | - `fn_setup_workstation_name_normalized` → `fn_setup_work_center_name_normalized` |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 51 | Work Center | - `vw_setup_workstations_active` → `vw_setup_work_centers_active` |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 52 | Work Center | - `SetupWorkstation*` classes/pages/view models/services → `SetupWorkCenter*` |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 53 | Work Center | - `config_workstation_hot_workcenters.setup_workstation_id` → `work_center_id` |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 54 | Work Center | - `waitlist_requests_queue.workstation_name` → `work_center_name` (requests happen at a wo |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 56 | Work Center | Work-center naming uses "work_center" / "Work Center" — never "workstation" and never "com |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 96 | Computer | Rename **every "workstation" occurrence** into either "Computer" or "Work Center": |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 100 | Review | - **Final sweep:** VS Code search for `Workstation`, `workstation`, and `Work Station` ret |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 114 | Computer | - `tools/scan_workstation_rename.ps1` scans every text file in the repo (excluding generat |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 117 | Review | `pwsh -NoProfile -File tools/scan_workstation_rename.ps1` |
| `Documents\Computer_FirstLoad_Gate_Design.md` | 119 | Work Center | - Items marked **Review** are ambiguous (computer vs work center) and need a human decisio |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 11 | Review | - [x] **Tech Lead: Run baseline rename scan** via `tools/scan_workstation_rename.ps1` and  |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 16 | Computer | - [x] **Database Migration: Rename table `core_workstations_registry` → `core_computers_re |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 17 | Computer | - [x] **Database Migration: Rename column `workstation_name` → `computer_name`** in `core_ |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 26 | Computer | - [x] **Database Migration: Rename `auth_sessions_tokens.workstation_id` → `computer_id`** |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 27 | Computer | - [x] **Database Migration: Rename `config_settings_values.workstation_id` → `computer_id` |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 28 | Computer | - [x] **Database Migration: Rename `config_settings_history.workstation_id` → `computer_id |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 29 | Work Center | - [x] **Database Migration: Rename `config_workstation_hot_workcenters.core_workstation_id |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 30 | Work Center | - [x] **Database Migration: Rename table `config_workstation_hot_workcenters` → `config_co |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 40 | Review | ### Subphase 1.4: Rename work-center catalog (Work Station → Work Center) (Ref: 1, 4) |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 42 | Work Center | - [x] **Database Migration: Rename table `setup_workstations_catalog` → `setup_work_center |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 43 | Work Center | - [x] **Database Migration: Rename `waitlist_requests_queue.workstation_name` → `work_cent |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 44 | Work Center | - [x] **Database Migration: Rename `sp_setup_workstations_*` → `sp_setup_work_centers_*`** |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 45 | Work Center | - [x] **Database Migration: Rename `fn_setup_workstation_name_normalized` → `fn_setup_work |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 48 | Work Center | **GATE: Dev DB recreates cleanly with `core_computers_registry`, `setup_work_centers_catal |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 54 | Computer | - [x] **Service Layer: Update `sp_config_settings_get_effective`** param `p_workstation_id |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 55 | Computer | - [x] **Service Layer: Update `sp_config_settings_upsert`** param `p_workstation_id` → `p_ |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 56 | Work Center | - [x] **Service Layer: Update `sp_config_hot_workcenters_*`** joins/params from `core_work |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 57 | Computer | - [x] **Service Layer: Audit all SPs in `Database/StoredProcedures/AllSPs.sql`** for remai |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 58 | Work Center | - [x] **Service Layer: Rename work-center SPs** (`sp_setup_work_centers_*`) and update `Al |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 60 | Work Center | **GATE: All stored procedures compile; `core_computers_registry`/`computer_id` for compute |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 67 | Computer | - [x] **Service Layer: Rename computer-related fields in `StartupState` / `StartupSessionS |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 71 | Work Center | - [x] **Full Stack Engineer: Rename Module_Setup work-center code** `SetupWorkstation*` →  |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 95 | Review | - [ ] **Full Stack Engineer: Keep stored data raw** — display format is presentation-only; |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 117 | Review | - [ ] **Tech Lead: Final sweep — run `tools/scan_workstation_rename.ps1` and VS Code searc |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 119 | Computer | **GATE: No user-facing or code "workstation" text remains anywhere; computer = "Computer", |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 141 | Work Center | - [x] **DevOps Engineer: Validate DB scripts against a dev instance** (recreate freely) an |
| `Documents\Computer_FirstLoad_Gate_Implementation_Checklist.md` | 147 | Computer | Next task: **Rename table `core_workstations_registry` → `core_computers_registry` in `Dat |
| `Documents\Development\CompletedImplementations\Complete-Waitlist-Request-Workflow-Checklist.md` | 28 | Review | - Proof: [Module_Waitlist/Models/WaitlistRequestDraft.cs](Module_Waitlist/Models/WaitlistR |
| `Documents\Development\CompletedImplementations\Complete-Waitlist-Request-Workflow-Checklist.md` | 31 | Review | - [x] Include the active setup-job identity, workstation name, and request timestamp in th |
| `Documents\Development\CompletedImplementations\Complete-Waitlist-Request-Workflow-Checklist.md` | 32 | Review | - Proof: both model files include `ActiveSetupJobId`, `WorkstationName`, and `RequestedUtc |
| `Documents\Development\CompletedImplementations\Complete-Waitlist-Request-Workflow-Checklist.md` | 51 | Review | - Proof: [Module_Waitlist/Services/WaitlistRequestService.cs](Module_Waitlist/Services/Wai |
| `Documents\Development\CompletedImplementations\Image-Location-Settings-Spec.md` | 37 | Review | \| Work centers \| `Assets\Images\default-workstation-image.png` \| |
| `Documents\Development\CompletedImplementations\Image-Location-Settings-Spec.md` | 113 | Work Center | Scope: all active rows in `setup_workstations_catalog`, retrieved through the existing |
| `Documents\Development\CompletedImplementations\Image-Location-Settings-Spec.md` | 115 | Review | path. The list is unfiltered by workstation; sorting and filtering happen in the dialog. |
| `Documents\Development\CompletedImplementations\Image-Location-Settings-Spec.md` | 117 | Work Center | Row key: `setup_workstations_catalog.id`. |
| `Documents\Development\CompletedImplementations\Image-Location-Settings-Spec.md` | 118 | Review | Default when unresolved: `Assets\Images\default-workstation-image.png`. |
| `Documents\Development\CompletedImplementations\Image-Location-Settings-Spec.md` | 125 | Work Center | \| Row key \| `setup_workstations_catalog.id` \| |
| `Documents\Development\CompletedImplementations\Image-Location-Settings-Spec.md` | 207 | Review | image selected on one workstation resolves on every other workstation. The stored |
| `Documents\Development\UserManagement\User-Management-Clarifying-Questions.md` | 111 | Work Center | 2. Refactor existing `Allowed*Roles` arrays (`SettingsViewModel`, `SetupWorkstationViewMod |
| `Module_Settings\Views\SettingsPage.xaml` | 415 | Review | ItemsSource="{x:Bind ViewModel.AvailableWorkstations, Mode=OneWay}" |
| `Module_Settings\Views\SettingsPage.xaml` | 418 | Review | SelectedValue="{x:Bind ViewModel.SelectedWorkstation, Mode=TwoWay}" /> |
| `Module_Setup\Views\SetupDunnageTypePage.xaml` | 33 | Review | <TextBlock x:Uid="Setup_DunnagePair.Header.WorkStation" Style="{StaticResource SetupFieldL |
| `Module_Setup\Views\SetupWorkCenterPage.xaml` | 87 | Review | <!-- Right pane: stacked workstation details with section dividers. --> |
| `Module_Setup\Views\SetupWorkCenterPage.xaml` | 142 | Review | <TextBlock x:Uid="Setup_Workstation.Title" Style="{StaticResource SetupPageTitleStyle}" Te |
| `Module_Setup\Views\SetupWorkCenterPage.xaml` | 143 | Review | <TextBlock x:Uid="Setup_Workstation.Subtitle" Style="{StaticResource SetupPageSubtitleStyl |
| `Module_Setup\Views\SetupWorkCenterPage.xaml` | 144 | Review | <TextBox x:Uid="Setup_Workstation.SearchInput" Header="Search" PlaceholderText="Search by  |
| `Module_Setup\Views\SetupWorkCenterPage.xaml.cs` | 112 | Review | Header = "Workstation", |
| `Module_Setup\Views\SetupWorkCenterPage.xaml.cs` | 113 | Review | PlaceholderText = "Enter workstation name", |
| `Module_Setup\Views\SetupWorkCenterPage.xaml.cs` | 118 | Review | var dialog = CreateWorkCenterDialog("New Workstation", nameInput, buildingInput); |
| `Module_Setup\Views\SetupWorkCenterPage.xaml.cs` | 133 | Review | if (!ViewModel.CanManageWorkCenters \|\| sender is not MenuFlyoutItem { Tag: SetupWorkCenter |
| `Module_Setup\Views\SetupWorkCenterPage.xaml.cs` | 138 | Review | ViewModel.SelectedWorkCenter = workstation; |
| `Module_Setup\Views\SetupWorkCenterPage.xaml.cs` | 142 | Review | Header = "Workstation", |
| `Module_Setup\Views\SetupWorkCenterPage.xaml.cs` | 143 | Review | Text = workstation.Name, |
| `Module_Setup\Views\SetupWorkCenterPage.xaml.cs` | 146 | Review | var buildingInput = CreateBuildingComboBox(workstation.Building); |
| `Module_Setup\Views\SetupWorkCenterPage.xaml.cs` | 148 | Review | var dialog = CreateWorkCenterDialog("Edit Workstation", nameInput, buildingInput); |
| `Module_Setup\Views\SetupWorkCenterPage.xaml.cs` | 163 | Review | if (!ViewModel.CanManageWorkCenters \|\| sender is not MenuFlyoutItem { Tag: SetupWorkCenter |
| `Module_Setup\Views\SetupWorkCenterPage.xaml.cs` | 168 | Review | ViewModel.SelectedWorkCenter = workstation; |
| `Module_Setup\Views\SetupWorkOrderPage.xaml` | 93 | Review | <Button x:Uid="Setup_Action.BackToWorkstations" Content="Back" Command="{x:Bind ViewModel. |
| `Module_Waitlist\Views\NewRequestWorkCenterPage.xaml` | 135 | Review | <TextBlock Style="{StaticResource SetupPageSubtitleStyle}" Text="{x:Bind ViewModel.Worksta |
| `MTM_Waitlist.Settings\Models\ConfigSettingValue.cs` | 26 | Review | /// Scope type (e.g., "all_users", "workstation", "user") |
| `MTM_Waitlist.Settings\Models\ConfigSettingValue.cs` | 32 | Review | /// Scope key (e.g., "all_users", workstation_id, user_id) |
| `MTM_Waitlist.Settings\Models\ConfigSettingValue.cs` | 38 | Review | /// Workstation ID (optional, for workstation-scoped settings) |
| `MTM_Waitlist.Settings\Models\ConfigSettingValue.cs` | 39 | Review | /// Nullable; used when scope_type is "workstation". |
| `MTM_Waitlist.Settings\Models\ImageLocation.cs` | 17 | Work Center | /// For work centers: numeric ID (from setup_workstations_catalog.id) |
| `MTM_Waitlist.Settings\Models\ImageLocation.cs` | 25 | Review | /// For work centers: The workstation_name (e.g., "Press 1") |
| `MTM_Waitlist.Settings\Models\ImageLocation.cs` | 185 | Work Center | /// Numeric ID from setup_workstations_catalog.id |
| `MTM_Waitlist.Settings\Models\ImageLocation.cs` | 191 | Work Center | /// From setup_workstations_catalog.workstation_name |
| `MTM_Waitlist.Settings\Models\ImageLocation.cs` | 197 | Work Center | /// From setup_workstations_catalog.building |
| `MTM_Waitlist.Settings\Models\ImageLocationDefaults.cs` | 31 | Review | public const string WorkCenterDefaultPath = "Assets\\Images\\default-workstation-image.png |
| `MTM_Waitlist.Settings\Models\ImageLocationScope.cs` | 30 | Review | /// Default image: Assets\Images\default-workstation-image.png |
| `MTM_Waitlist.Settings\Models\ImageLocationScope.cs` | 32 | Work Center | /// Inventory: Dynamic (from setup_workstations_catalog, live database) |
| `MTM_Waitlist.Settings\Models\ImageOverride.cs` | 31 | Work Center | /// For work centers: numeric ID string (from setup_workstations_catalog.id) |
| `MTM_Waitlist.Settings\Models\ImageStorageOptions.cs` | 33 | Review | /// Must be accessible from the app server and all workstations. |
| `MTM_Waitlist.Settings\Models\WorkCenterInventory.cs` | 6 | Work Center | /// work centers are dynamic and loaded from the database table setup_workstations_catalog |
| `MTM_Waitlist.Settings\Models\WorkCenterInventory.cs` | 11 | Work Center | /// Source: setup_workstations_catalog database table |
| `MTM_Waitlist.Settings\Models\WorkCenterInventory.cs` | 12 | Work Center | /// Row Key: setup_workstations_catalog.id (numeric BIGINT) |
| `MTM_Waitlist.Settings\Models\WorkCenterInventory.cs` | 14 | Review | /// Default Image: Assets\Images\default-workstation-image.png |
| `MTM_Waitlist.Settings\Models\WorkCenterInventory.cs` | 20 | Work Center | /// Loaded from setup_workstations_catalog at application startup and kept in sync. |
| `MTM_Waitlist.Settings\Models\WorkCenterInventory.cs` | 42 | Work Center | /// <param name="workCenterId">The numeric ID from setup_workstations_catalog.id</param> |
| `MTM_Waitlist.Settings\Models\WorkCenterInventory.cs` | 94 | Work Center | /// Represents a single work center from the setup_workstations_catalog. |
| `MTM_Waitlist.Settings\Models\WorkCenterInventory.cs` | 100 | Work Center | /// Numeric primary key from setup_workstations_catalog.id (BIGINT AUTO_INCREMENT). |
| `MTM_Waitlist.Settings\Models\WorkCenterInventory.cs` | 108 | Work Center | /// From setup_workstations_catalog.workstation_name. |
| `MTM_Waitlist.Settings\Models\WorkCenterInventory.cs` | 115 | Work Center | /// From setup_workstations_catalog.building (e.g., "Expo Drive"). |
| `MTM_Waitlist.Settings\Models\WorkCenterInventory.cs` | 122 | Work Center | /// From setup_workstations_catalog.sort_rank. |
| `MTM_Waitlist.Settings\Models\WorkCenterInventory.cs` | 129 | Work Center | /// From setup_workstations_catalog.is_active. |
| `MTM_Waitlist.Settings\Services\IImageLocationService.cs` | 82 | Work Center | /// <param name="workCenterId">The numeric ID from setup_workstations_catalog</param> |
| `MTM_Waitlist.Settings\Services\IImageLocationService.cs` | 146 | Work Center | /// Queries the setup_workstations_catalog table for all rows where is_active=1. |
| `MTM_Waitlist.Settings\Services\IImageOverrideReadService.cs` | 72 | Work Center | /// For example, a work_center override for an ID that's no longer in setup_workstations_c |
| `MTM_Waitlist.Settings\Services\ImageLocationService.cs` | 593 | Review | // An empty workstation name makes the catalog service resolve the current workstation. |
| `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs` | 74 | Review | public partial string SelectedWorkstation |
| `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs` | 103 | Computer | public ObservableCollection<ComputerOption> AvailableWorkstations { get; } = new(); |
| `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs` | 127 | Review | "workstation", |
| `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs` | 131 | Review | string.Join(" ", AvailableWorkstations.Select(option => option.Label))); |
| `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs` | 242 | Review | partial void OnSelectedWorkstationChanged(string value) |
| `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs` | 244 | Computer | StartupDebugLog.Info("SettingsViewModel", $"SelectedWorkstation changed to '{value}'."); |
| `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs` | 250 | Review | _ = LoadCatalogForWorkstationAsync(value); |
| `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs` | 267 | Computer | StartupDebugLog.Info("SettingsHotWorkCenters", $"AddHotWorkCenterAsync started. WorkCenter |
| `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs` | 300 | Computer | StartupDebugLog.Info("SettingsHotWorkCenters", $"RemoveHotWorkCenterAsync started. WorkCen |
| `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs` | 388 | Computer | var workstations = await _workCenterCatalogService.GetAvailableComputersAsync().ConfigureA |
| `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs` | 389 | Review | ReplaceCollectionValues(AvailableWorkstations, workstations); |
| `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs` | 391 | Computer | var currentWorkstation = _workCenterCatalogService.GetCurrentComputerName(); |
| `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs` | 392 | Review | var resolvedWorkstation = (AvailableWorkstations.FirstOrDefault(item => |
| `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs` | 393 | Review | string.Equals(item.Key, currentWorkstation, StringComparison.OrdinalIgnoreCase)) |
| `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs` | 394 | Review | ?? AvailableWorkstations.FirstOrDefault())?.Key |
| `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs` | 395 | Review | ?? currentWorkstation; |
| `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs` | 397 | Review | var workstationChanged = !string.Equals(SelectedWorkstation, resolvedWorkstation, StringCo |
| `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs` | 398 | Review | if (string.IsNullOrWhiteSpace(SelectedWorkstation)) |
| `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs` | 400 | Review | SelectedWorkstation = resolvedWorkstation; |
| `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs` | 402 | Review | else if (workstationChanged) |
| `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs` | 404 | Review | SelectedWorkstation = resolvedWorkstation; |
| `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs` | 408 | Review | await LoadCatalogForWorkstationAsync(SelectedWorkstation).ConfigureAwait(true); |
| `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs` | 413 | Computer | StartupDebugLog.Info("SettingsViewModel", $"InitializeHotWorkCentersAsync completed. Works |
| `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs` | 421 | Review | private async Task LoadCatalogForWorkstationAsync(string workstationName) |
| `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs` | 423 | Computer | StartupDebugLog.Info("SettingsViewModel", $"LoadCatalogForWorkstationAsync started. Workst |
| `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs` | 427 | Review | var catalog = await _workCenterCatalogService.GetCatalogAsync(workstationName).ConfigureAw |
| `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs` | 432 | Computer | StartupDebugLog.Info("SettingsViewModel", $"LoadCatalogForWorkstationAsync completed. Work |
| `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs` | 436 | Computer | StartupDebugLog.Error("SettingsViewModel", ex, $"LoadCatalogForWorkstationAsync failed. Wo |
| `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs` | 449 | Computer | StartupDebugLog.Info("SettingsHotWorkCenters", $"SaveCurrentHotWorkCentersAsync started. W |
| `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs` | 451 | Review | .SaveHotWorkCentersAsync(SelectedWorkstation, HotWorkCenters.ToArray()) |
| `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs` | 458 | Computer | StartupDebugLog.Info("SettingsHotWorkCenters", $"SaveCurrentHotWorkCentersAsync completed. |
| `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs` | 462 | Computer | StartupDebugLog.Error("SettingsHotWorkCenters", ex, $"SaveCurrentHotWorkCentersAsync faile |
| `MTM_Waitlist.Settings\ViewModels\WorkCenterImagesDialogViewModel.cs` | 9 | Work Center | /// setup_workstations_catalog.id. |
| `MTM_Waitlist.Setup\Contracts\Services\SetupContracts.cs` | 26 | Review | Task<SetupSelectionResult> AddWorkCenterAsync(string workstationName, string building, Can |
| `MTM_Waitlist.Setup\Contracts\Services\SetupContracts.cs` | 28 | Review | Task<SetupSelectionResult> UpdateWorkCenterAsync(string workstationId, string workstationN |
| `MTM_Waitlist.Setup\Contracts\Services\SetupContracts.cs` | 30 | Review | Task<SetupSelectionResult> RemoveWorkCenterAsync(string workstationId, CancellationToken c |
| `MTM_Waitlist.Setup\Models\SetupModels.cs` | 167 | Review | private const string DefaultWorkstationImagePath = "Assets/Images/default-workstation-imag |
| `MTM_Waitlist.Setup\Models\SetupModels.cs` | 172 | Review | /// Whether this workstation is the currently selected card in the selection grid. |
| `MTM_Waitlist.Setup\Models\SetupModels.cs` | 190 | Review | /// Resolved image path for the workstation's work center. Populated by the |
| `MTM_Waitlist.Setup\Models\SetupModels.cs` | 192 | Review | /// Falls back to the packaged default workstation image when unresolved. |
| `MTM_Waitlist.Setup\Models\SetupModels.cs` | 194 | Review | public string ImagePath { get; set; } = DefaultWorkstationImagePath; |
| `MTM_Waitlist.Setup\Models\SetupModels.cs` | 204 | Work Center | /// Read from <c>setup_workstations_catalog.updated_utc</c> and refreshed whenever a |
| `MTM_Waitlist.Setup\Services\SetupPersistenceService.cs` | 305 | Work Center | StartupDebugLog.Info("SetupPersistence", $"sp_setup_workstations_touch completed. WorkCent |
| `MTM_Waitlist.Setup\Services\SetupWorkCenterService.cs` | 42 | Work Center | var workstationName = GetValue(row, "work_center_name"); |
| `MTM_Waitlist.Setup\Services\SetupWorkCenterService.cs` | 43 | Review | jobsByWorkCenter.TryGetValue(workstationName, out var activeJobRow); |
| `MTM_Waitlist.Setup\Services\SetupWorkCenterService.cs` | 48 | Review | Name = workstationName, |
| `MTM_Waitlist.Setup\Services\SetupWorkCenterService.cs` | 62 | Review | public async Task<SetupSelectionResult> AddWorkCenterAsync(string workstationName, string  |
| `MTM_Waitlist.Setup\Services\SetupWorkCenterService.cs` | 64 | Review | if (string.IsNullOrWhiteSpace(workstationName)) |
| `MTM_Waitlist.Setup\Services\SetupWorkCenterService.cs` | 66 | Review | return new SetupSelectionResult { Success = false, Message = "Workstation name is required |
| `MTM_Waitlist.Setup\Services\SetupWorkCenterService.cs` | 79 | Work Center | ["p_work_center_name"] = workstationName.Trim(), |
| `MTM_Waitlist.Setup\Services\SetupWorkCenterService.cs` | 89 | Review | Message = affectedRows > 0 ? "Workstation added." : "Unable to add workstation." |
| `MTM_Waitlist.Setup\Services\SetupWorkCenterService.cs` | 93 | Review | public async Task<SetupSelectionResult> UpdateWorkCenterAsync(string workstationId, string |
| `MTM_Waitlist.Setup\Services\SetupWorkCenterService.cs` | 95 | Review | if (string.IsNullOrWhiteSpace(workstationId) \|\| string.IsNullOrWhiteSpace(workstationName) |
| `MTM_Waitlist.Setup\Services\SetupWorkCenterService.cs` | 97 | Review | return new SetupSelectionResult { Success = false, Message = "Workstation ID, name, and bu |
| `MTM_Waitlist.Setup\Services\SetupWorkCenterService.cs` | 104 | Work Center | ["p_work_center_id"] = workstationId.Trim(), |
| `MTM_Waitlist.Setup\Services\SetupWorkCenterService.cs` | 105 | Work Center | ["p_work_center_name"] = workstationName.Trim(), |
| `MTM_Waitlist.Setup\Services\SetupWorkCenterService.cs` | 115 | Review | Message = affectedRows > 0 ? "Workstation updated." : "Unable to update workstation." |
| `MTM_Waitlist.Setup\Services\SetupWorkCenterService.cs` | 119 | Review | public async Task<SetupSelectionResult> RemoveWorkCenterAsync(string workstationId, Cancel |
| `MTM_Waitlist.Setup\Services\SetupWorkCenterService.cs` | 121 | Review | if (string.IsNullOrWhiteSpace(workstationId)) |
| `MTM_Waitlist.Setup\Services\SetupWorkCenterService.cs` | 123 | Review | return new SetupSelectionResult { Success = false, Message = "Workstation ID is required." |
| `MTM_Waitlist.Setup\Services\SetupWorkCenterService.cs` | 130 | Work Center | ["p_work_center_id"] = workstationId.Trim(), |
| `MTM_Waitlist.Setup\Services\SetupWorkCenterService.cs` | 138 | Review | Message = affectedRows > 0 ? "Workstation removed." : "Unable to remove workstation." |
| `MTM_Waitlist.Setup\ViewModels\SetupWorkCenterViewModel.cs` | 19 | Review | private const string DefaultWorkCenterImagePath = "Assets/Images/default-workstation-image |
| `MTM_Waitlist.Setup\ViewModels\SetupWorkCenterViewModel.cs` | 92 | Review | public ObservableCollection<SetupWorkCenter> Workstations => State.WorkCenters; |
| `MTM_Waitlist.Setup\ViewModels\SetupWorkCenterViewModel.cs` | 151 | Review | StatusMessage = "Choose a workstation to continue."; |
| `MTM_Waitlist.Setup\ViewModels\SetupWorkCenterViewModel.cs` | 173 | Review | StatusMessage = "You do not have permission to manage workstations."; |
| `MTM_Waitlist.Setup\ViewModels\SetupWorkCenterViewModel.cs` | 192 | Review | StatusMessage = "You do not have permission to manage workstations."; |
| `MTM_Waitlist.Setup\ViewModels\SetupWorkCenterViewModel.cs` | 198 | Review | StatusMessage = "Select a workstation to edit."; |
| `MTM_Waitlist.Setup\ViewModels\SetupWorkCenterViewModel.cs` | 217 | Review | StatusMessage = "You do not have permission to manage workstations."; |
| `MTM_Waitlist.Setup\ViewModels\SetupWorkCenterViewModel.cs` | 223 | Review | StatusMessage = "Select a workstation to remove."; |
| `MTM_Waitlist.Setup\ViewModels\SetupWorkCenterViewModel.cs` | 307 | Work Center | StartupDebugLog.Info("SetupWorkstation", $"Local work centers loaded for the setup selecti |
| `MTM_Waitlist.Setup\ViewModels\SetupWorkCenterViewModel.cs` | 311 | Work Center | StartupDebugLog.Error("SetupWorkstation", ex, "Failed to load Local work centers for the s |
| `MTM_Waitlist.Setup\ViewModels\SetupWorkCenterViewModel.cs` | 325 | Review | private async Task<string> ResolveWorkCenterImagePathAsync(SetupWorkCenter workstation, Ca |
| `MTM_Waitlist.Setup\ViewModels\SetupWorkCenterViewModel.cs` | 329 | Review | \|\| string.IsNullOrWhiteSpace(workstation.Id)) |
| `MTM_Waitlist.Setup\ViewModels\SetupWorkCenterViewModel.cs` | 337 | Review | .ResolveWorkCenterImagePathAsync(workstation.Id, cancellationToken) |
| `MTM_Waitlist.Setup\ViewModels\SetupWorkCenterViewModel.cs` | 365 | Review | foreach (var workstation in filteredItems) |
| `MTM_Waitlist.Setup\ViewModels\SetupWorkCenterViewModel.cs` | 367 | Review | if (_hotWorkCenterNames.Contains(workstation.Name)) |
| `MTM_Waitlist.Setup\ViewModels\SetupWorkCenterViewModel.cs` | 369 | Review | _displayedHotWorkCenters.Add(workstation); |
| `MTM_Waitlist.Setup\ViewModels\SetupWorkCenterViewModel.cs` | 373 | Review | _displayedOtherWorkCenters.Add(workstation); |
| `MTM_Waitlist.Shared\Models\WorkCenterDetail.cs` | 6 | Work Center | /// <c>setup_workstations_catalog</c> metadata (building, updated_utc) with the latest |
| `MTM_Waitlist.Shared\Models\WorkCenterSelectionItem.cs` | 7 | Review | private const string DefaultWorkCenterImagePath = "Assets/Images/default-workstation-image |
| `MTM_Waitlist.Shared\Models\WorkCenterSelectionItem.cs` | 19 | Work Center | /// Read from <c>setup_workstations_catalog.updated_utc</c>. |
| `MTM_Waitlist.Shared\Services\IWorkCenterCatalogService.cs` | 11 | Review | Task<WorkCenterCatalogResult> GetCatalogAsync(string workstationName, CancellationToken ca |
| `MTM_Waitlist.Shared\Services\IWorkCenterCatalogService.cs` | 13 | Review | Task<string?> SaveHotWorkCentersAsync(string workstationName, IReadOnlyCollection<string>  |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 66 | Computer | var currentWorkstation = await ResolveCurrentComputerNameAsync(cancellationToken).Configur |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 67 | Computer | if (!computers.Any(option => string.Equals(option.Key, currentWorkstation, StringCompariso |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 69 | Computer | computers.Insert(0, new ComputerOption { Key = currentWorkstation, Label = currentWorkstat |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 75 | Review | public async Task<WorkCenterCatalogResult> GetCatalogAsync(string workstationName, Cancell |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 77 | Review | var normalizedWorkstationName = string.IsNullOrWhiteSpace(workstationName) |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 79 | Review | : workstationName.Trim(); |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 81 | Computer | StartupDebugLog.Info("WorkCenterCatalog", $"GetCatalogAsync started. Workstation='{normali |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 101 | Computer | cwr.computer_name = @p_workstation_name |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 102 | Review | OR cwr.hostname_normalized = @p_workstation_name |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 107 | Review | ["p_workstation_name"] = normalizedWorkstationName, |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 175 | Computer | ComputerName = normalizedWorkstationName, |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 182 | Computer | StartupDebugLog.Info("WorkCenterCatalog", $"GetCatalogAsync completed. Workstation='{norma |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 186 | Review | public async Task<string?> SaveHotWorkCentersAsync(string workstationName, IReadOnlyCollec |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 190 | Review | var normalizedWorkstationName = string.IsNullOrWhiteSpace(workstationName) |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 192 | Review | : workstationName.Trim(); |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 194 | Computer | StartupDebugLog.Info("WorkCenterCatalog", $"SaveHotWorkCentersAsync started. Workstation=' |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 196 | Review | var workstationRows = await _mySqlHelperServer.ExecuteSqlQueryAsync( |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 199 | Computer | WHERE computer_name = @p_workstation_name |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 200 | Review | OR hostname_normalized = @p_workstation_name |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 201 | Computer | ORDER BY CASE WHEN computer_name = @p_workstation_name THEN 0 ELSE 1 END |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 205 | Review | ["p_workstation_name"] = normalizedWorkstationName, |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 210 | Review | var workstationId = GetInt64(workstationRows.FirstOrDefault(), "id"); |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 211 | Review | if (workstationId <= 0) |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 213 | Computer | StartupDebugLog.Info("WorkCenterCatalog", $"SaveHotWorkCentersAsync aborted. Workstation ' |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 214 | Review | return "Unable to save Local workcenters: workstation not found."; |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 246 | Work Center | WorkCenterId = workCenterIdByName.TryGetValue(workCenterName, out var setupWorkstationId) |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 247 | Work Center | ? setupWorkstationId |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 256 | Computer | StartupDebugLog.Info("WorkCenterCatalog", $"SaveHotWorkCentersAsync aborted. No database c |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 274 | Computer | WHERE computer_id = @p_core_workstation_id;", connection, transaction)) |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 277 | Computer | deleteCommand.Parameters.AddWithValue("@p_core_workstation_id", workstationId); |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 304 | Work Center | insertSql.Append($"(@p_core_workstation_id, @p_setup_workstation_id_{parameterSuffix}, UUI |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 316 | Computer | insertCommand.Parameters.AddWithValue("@p_core_workstation_id", workstationId); |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 322 | Work Center | insertCommand.Parameters.AddWithValue($"@p_setup_workstation_id_{index}", item.WorkCenterI |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 330 | Computer | StartupDebugLog.Info("WorkCenterCatalog", $"SaveHotWorkCentersAsync completed. Workstation |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 335 | Computer | StartupDebugLog.Error("WorkCenterCatalog", ex, $"SaveHotWorkCentersAsync failed. Workstati |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 365 | Computer | WHERE computer_name = @p_workstation_name |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 366 | Review | OR hostname_normalized = @p_workstation_name |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 367 | Computer | ORDER BY CASE WHEN computer_name = @p_workstation_name THEN 0 ELSE 1 END |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 371 | Review | ["p_workstation_name"] = key, |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 376 | Computer | var workstationName = GetValue(rows.FirstOrDefault(), "computer_name"); |
| `MTM_Waitlist.Shared\Services\WorkCenterCatalogService.cs` | 377 | Review | return string.IsNullOrWhiteSpace(workstationName) ? key : workstationName; |
| `MTM_Waitlist.Tests\Module_Settings\ImageOverrideDialogViewModelTests.cs` | 378 | Review | ["workstation_name"] = name, |
| `MTM_Waitlist.Tests\Module_Settings\TestDoubles.cs` | 167 | Computer | public string GetCurrentComputerName() => "test-workstation"; |
| `MTM_Waitlist.Tests\Module_Settings\TestDoubles.cs` | 170 | Computer | Task.FromResult<IReadOnlyList<ComputerOption>>(new[] { new ComputerOption { Key = "test-wo |
| `MTM_Waitlist.Tests\Module_Settings\TestDoubles.cs` | 172 | Review | public Task<WorkCenterCatalogResult> GetCatalogAsync(string workstationName, CancellationT |
| `MTM_Waitlist.Tests\Module_Settings\TestDoubles.cs` | 175 | Review | public Task<string?> SaveHotWorkCentersAsync(string workstationName, IReadOnlyCollection<s |
| `MTM_Waitlist.Tests\Module_Waitlist\ViewModels\NewRequestWorkCenterViewModelTests.cs` | 299 | Computer | ComputerName = "test-workstation", |
| `MTM_Waitlist.Tests\Module_Waitlist\ViewModels\NewRequestWorkCenterViewModelTests.cs` | 311 | Computer | public string GetCurrentComputerName() => "test-workstation"; |
| `MTM_Waitlist.Tests\Module_Waitlist\ViewModels\NewRequestWorkCenterViewModelTests.cs` | 314 | Computer | Task.FromResult<IReadOnlyList<ComputerOption>>(new[] { new ComputerOption { Key = "test-wo |
| `MTM_Waitlist.Tests\Module_Waitlist\ViewModels\NewRequestWorkCenterViewModelTests.cs` | 316 | Review | public Task<WorkCenterCatalogResult> GetCatalogAsync(string workstationName, CancellationT |
| `MTM_Waitlist.Tests\Module_Waitlist\ViewModels\NewRequestWorkCenterViewModelTests.cs` | 319 | Review | public Task<string?> SaveHotWorkCentersAsync(string workstationName, IReadOnlyCollection<s |
| `MTM_Waitlist.Tests\Services\StartupCoordinatorTests.cs` | 223 | Computer | public async Task RunAsync_WhenUnknownWorkstation_RoutesToLoginAndRequiresNewUserActionAsy |
| `MTM_Waitlist.Tests\Services\StartupCoordinatorTests.cs` | 265 | Computer | public async Task RunAsync_WhenWorkstationStatusIsNotAuthoritative_RoutesToLoginWithoutNew |
| `MTM_Waitlist.Tests\ViewModels\LoginViewModelTests.cs` | 23 | Review | HostnameNormalized = "dev-workstation-001", |
| `MTM_Waitlist.Tests\ViewModels\LoginViewModelTests.cs` | 26 | Review | LoginHint = "This workstation is not registered. Choose New User to request access." |
| `MTM_Waitlist.Waitlist.NewRequest\ViewModels\NewRequestWorkCenterViewModel.cs` | 23 | Review | private const string DefaultWorkCenterImagePath = "Assets/Images/default-workstation-image |
| `MTM_Waitlist.Waitlist.NewRequest\ViewModels\NewRequestWorkCenterViewModel.cs` | 36 | Review | public partial string WorkstationName |
| `MTM_Waitlist.Waitlist.NewRequest\ViewModels\NewRequestWorkCenterViewModel.cs` | 146 | Computer | var workstationName = _workCenterCatalogService.GetCurrentComputerName(); |
| `MTM_Waitlist.Waitlist.NewRequest\ViewModels\NewRequestWorkCenterViewModel.cs` | 147 | Review | var catalog = await _workCenterCatalogService.GetCatalogAsync(workstationName).ConfigureAw |
| `MTM_Waitlist.Waitlist.NewRequest\ViewModels\NewRequestWorkCenterViewModel.cs` | 150 | Computer | WorkstationName = catalog.ComputerName; |
| `MTM_Waitlist.Waitlist.NewRequest\ViewModels\NewRequestWorkCenterViewModel.cs` | 171 | Computer | StartupDebugLog.Info("NewRequestWorkCenter", $"Catalog loaded. Workstation='{catalog.Compu |
| `MTM_Waitlist.Waitlist.NewRequest\ViewModels\NewRequestWorkCenterViewModel.cs` | 176 | Review | WorkstationName = string.Empty; |
| `MTM_Waitlist.Waitlist.NewRequest\ViewModels\NewRequestWorkCenterViewModel.cs` | 283 | Computer | StartupDebugLog.Info("NewRequestWorkCenter", $"Blocked workstation selection for '{normali |
| `MTM_Waitlist.Waitlist.NewRequest\ViewModels\NewRequestWorkCenterViewModel.cs` | 303 | Computer | StartupDebugLog.Info("NewRequestWorkCenter", $"Selected workstation '{normalizedWorkCenter |
| `MTM_Waitlist.Waitlist.View\Models\SampleOrder.cs` | 30 | Review | ? "Assets/Images/default-workstation-image.png" |
| `MTM_Waitlist.Waitlist.View\Services\WaitlistRequestService.cs` | 166 | Review | return WaitlistRequestSubmitResult.ValidationFailure("The current workstation name is requ |
| `Strings\en-us\Resources.resw` | 328 | Review | <data name="Setup_DunnagePair.Header.WorkStation.Text" xml:space="preserve"> |
| `Strings\en-us\Resources.resw` | 373 | Review | <data name="Setup_Workstation.Title.Text" xml:space="preserve"> |
| `Strings\en-us\Resources.resw` | 376 | Review | <data name="Setup_Workstation.Subtitle.Text" xml:space="preserve"> |
| `Strings\en-us\Resources.resw` | 379 | Review | <data name="Setup_Workstation.SearchInput.Header" xml:space="preserve"> |
| `Strings\en-us\Resources.resw` | 382 | Review | <data name="Setup_Workstation.SearchInput.PlaceholderText" xml:space="preserve"> |
| `Strings\en-us\Resources.resw` | 385 | Review | <data name="Setup_Workstation.ManageTitle.Text" xml:space="preserve"> |
| `Strings\en-us\Resources.resw` | 388 | Review | <data name="Setup_Workstation.NameInput.Header" xml:space="preserve"> |
| `Strings\en-us\Resources.resw` | 391 | Review | <data name="Setup_Workstation.NameInput.PlaceholderText" xml:space="preserve"> |
| `Strings\en-us\Resources.resw` | 394 | Review | <data name="Setup_Workstation.Add.Content" xml:space="preserve"> |
| `Strings\en-us\Resources.resw` | 397 | Review | <data name="Setup_Workstation.Update.Content" xml:space="preserve"> |
| `Strings\en-us\Resources.resw` | 400 | Review | <data name="Setup_Workstation.Remove.Content" xml:space="preserve"> |
| `Strings\en-us\Resources.resw` | 403 | Review | <data name="Setup_Workstation.New.Content" xml:space="preserve"> |
| `Strings\en-us\Resources.resw` | 406 | Review | <data name="Setup_Workstation.ManageHint.Text" xml:space="preserve"> |
| `Strings\en-us\Resources.resw` | 409 | Review | <data name="Setup_Action.BackToWorkstations.Content" xml:space="preserve"> |
| `Strings\en-us\TooltipResources.developer.resw` | 192 | Work Center | <data name="Setup_SetupWorkstationPage_TextBox1_Tooltip" xml:space="preserve"> |
| `Strings\en-us\TooltipResources.developer.resw` | 195 | Work Center | <data name="Setup_SetupWorkstationPage_GridView1_Tooltip" xml:space="preserve"> |
| `Strings\en-us\TooltipResources.developer.resw` | 198 | Work Center | <data name="Setup_SetupWorkstationPage_MenuFlyoutItem1_Tooltip" xml:space="preserve"> |
| `Strings\en-us\TooltipResources.developer.resw` | 201 | Work Center | <data name="Setup_SetupWorkstationPage_MenuFlyoutItem2_Tooltip" xml:space="preserve"> |
| `Strings\en-us\TooltipResources.developer.resw` | 204 | Work Center | <data name="Setup_SetupWorkstationPage_Button1_Tooltip" xml:space="preserve"> |
| `Strings\en-us\TooltipResources.developer.resw` | 207 | Work Center | <data name="Setup_SetupWorkstationPage_Button2_Tooltip" xml:space="preserve"> |
| `Strings\en-us\TooltipResources.developer.resw` | 210 | Work Center | <data name="Setup_SetupWorkstationPage_Button3_Tooltip" xml:space="preserve"> |
| `Strings\en-us\TooltipResources.resw` | 192 | Work Center | <data name="Setup_SetupWorkstationPage_TextBox1_Tooltip" xml:space="preserve"> |
| `Strings\en-us\TooltipResources.resw` | 195 | Work Center | <data name="Setup_SetupWorkstationPage_GridView1_Tooltip" xml:space="preserve"> |
| `Strings\en-us\TooltipResources.resw` | 198 | Work Center | <data name="Setup_SetupWorkstationPage_MenuFlyoutItem1_Tooltip" xml:space="preserve"> |
| `Strings\en-us\TooltipResources.resw` | 201 | Work Center | <data name="Setup_SetupWorkstationPage_MenuFlyoutItem2_Tooltip" xml:space="preserve"> |
| `Strings\en-us\TooltipResources.resw` | 204 | Work Center | <data name="Setup_SetupWorkstationPage_Button1_Tooltip" xml:space="preserve"> |
| `Strings\en-us\TooltipResources.resw` | 207 | Work Center | <data name="Setup_SetupWorkstationPage_Button2_Tooltip" xml:space="preserve"> |
| `Strings\en-us\TooltipResources.resw` | 210 | Work Center | <data name="Setup_SetupWorkstationPage_Button3_Tooltip" xml:space="preserve"> |
| `ViewModels\ShellViewModel.cs` | 43 | Review | "Work Station", |
| `ViewModels\ShellViewModel.cs` | 318 | Review | return ("Work Center Setup — Select Work Station", 1); |

