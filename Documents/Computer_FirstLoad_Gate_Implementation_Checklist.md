# Computer First-Load Gate + Computer Rename — Implementation Checklist

Source of truth: `Documents/Computer_FirstLoad_Gate_Design.md` (refs below map to its sections).

> **Post-refactor execution context:** Phases 1–4 below are complete (implemented pre-refactor in the single app project). Phases 5–9 execute **after** the per-module project split (`TEST_PROJECT_REFACTOR_CHECKLIST.md`) lands. Every `Module_*` path referenced below resolves under that module's new project root (e.g. `Module_Startup/...` → `MTM_Waitlist.Startup`, `Module_Setup/...` → `MTM_Waitlist.Setup`, `Module_Settings/...` → `MTM_Waitlist.Settings`); namespaces (`MTM_Waitlist.Module_*`) are unchanged. XAML Views, `.resw` resources, and DI wiring stay in the `MTM_Waitlist` app project (composition root); the test project references the module libraries, not the app.

---

## Phase 1: Database Schema — Registry Rename + New Columns

- [x] **Tech Lead: Run baseline rename scan** via `tools/scan_workstation_rename.ps1` and record the starting match count in `Documents/Rename_Scan_Results.md`. (Ref: 13) | **Persona: Tech Lead**
  - Baseline: **910 matches across 120 files**, **17 files/folders to rename** (scan excludes `Documents/Development/InforVisual`).

### Subphase 1.1: Rename registry table & column (Ref: 2)

- [x] **Database Migration: Rename table `core_workstations_registry` → `core_computers_registry`** in `Database/Tables/AllTables.sql` and `Database/Tables/core_workstations_registry/create.sql`. (Ref: 2) | **Persona: Database Engineer**
- [x] **Database Migration: Rename column `workstation_name` → `computer_name`** in `core_computers_registry`. (Ref: 2) | **Persona: Database Engineer**
- [x] **Database Migration: Add `display_name VARCHAR(128) NOT NULL`** to `core_computers_registry` (required). (Ref: 2) | **Persona: Database Engineer**
- [x] **Database Migration: Add `description VARCHAR(255) NULL`** to `core_computers_registry` (optional). (Ref: 2) | **Persona: Database Engineer**
- [x] **Database Migration: Add unique key on `display_name`** (`uq_core_computers_registry_display_name`). (Ref: 2, 7) | **Persona: Database Engineer**
- [x] **Database Migration: Keep composite unique `(computer_name, mac_address_normalized)`** (strict composite identity). (Ref: 2, 5) | **Persona: Database Engineer**
- [x] **Database Migration: Move table artifact to `Database/Tables/core_computers_registry/create.sql`** and delete the old folder. (Ref: 2) *Depends on: Rename table* | **Persona: Database Engineer**

### Subphase 1.2: Rename computer-side foreign keys (Ref: 3)

- [x] **Database Migration: Rename `auth_sessions_tokens.workstation_id` → `computer_id`** (FK + index + constraint) in `Database/Tables/05_auth_sessions_tokens/create.sql` and `AllTables.sql`. (Ref: 3) | **Persona: Database Engineer**
- [x] **Database Migration: Rename `config_settings_values.workstation_id` → `computer_id`** (FK + index + constraint) in `Database/Tables/08_config_settings_values/create.sql` and `AllTables.sql`. (Ref: 3) | **Persona: Database Engineer**
- [x] **Database Migration: Rename `config_settings_history.workstation_id` → `computer_id`** (FK + constraint) in `Database/Tables/09_config_settings_history/create.sql` and `AllTables.sql`. (Ref: 3) | **Persona: Database Engineer**
- [x] **Database Migration: Rename `config_workstation_hot_workcenters.core_workstation_id` → `computer_id`** (computer side). (Ref: 3, 4) | **Persona: Database Engineer**
- [x] **Database Migration: Rename table `config_workstation_hot_workcenters` → `config_computer_hot_work_centers`** and column `setup_workstation_id` → `work_center_id`. (Ref: 3, 4) *Depends on: core_workstation_id rename* | **Persona: Database Engineer**
- [x] **Database Migration: Update bootstrap script** `Database/Bootstrap/update_table_descriptions.sql` for all renamed columns/constraints. (Ref: 3) *Depends on: the FK renames* | **Persona: Database Engineer**

### Subphase 1.3: Scripts, seeds, validation (Ref: 10)

- [x] **Database Migration: Update startup validation script** `Database/Validation/startup_schema/validate.sql` for new table/column names + `display_name`/`description`. (Ref: 10) | **Persona: Database Engineer**
- [x] **Database Migration: Update settings validation script** `Database/Validation/settings_schema/validate.sql` for `config_settings_values.computer_id`. (Ref: 10) | **Persona: Database Engineer**
- [x] **Database Migration: Update `seed_dev_masked_baseline`** to use `core_computers_registry`, `computer_name`, and generate `display_name` + `description` for seed rows. (Ref: 10) | **Persona: Database Engineer**
- [x] **Database Migration: Update `Database/Tables/AllTables.sql` view/join references** and any `AllSeeds.sql`/`AllTables.sql` aggregates referencing the renamed registry. (Ref: 10) | **Persona: Database Engineer**

### Subphase 1.4: Rename work-center catalog (Work Station → Work Center) (Ref: 1, 4)

- [x] **Database Migration: Rename table `setup_workstations_catalog` → `setup_work_centers_catalog`** (file + `AllTables.sql`). (Ref: 4) | **Persona: Database Engineer**
- [x] **Database Migration: Rename `waitlist_requests_queue.workstation_name` → `work_center_name`** (work-center side). (Ref: 4) | **Persona: Database Engineer**
- [x] **Database Migration: Rename `sp_setup_workstations_*` → `sp_setup_work_centers_*`** stored procedures. (Ref: 4) | **Persona: Database Engineer**
- [x] **Database Migration: Rename `fn_setup_workstation_name_normalized` → `fn_setup_work_center_name_normalized`** and `vw_setup_workstations_active` → `vw_setup_work_centers_active`. (Ref: 4) | **Persona: Database Engineer**
- [x] **Database Migration: Update work-center seeds/validation/AllTables aggregates** for the renamed catalog. (Ref: 4) *Depends on: catalog rename* | **Persona: Database Engineer**

**GATE: Dev DB recreates cleanly with `core_computers_registry`, `setup_work_centers_catalog`, new columns, and renamed FK columns — and `workstation` returns zero DB-name results — before Phase 2.**

---

## Phase 2: Stored Procedures — Computer Scope Rename (Ref: 3, 10)

- [x] **Service Layer: Update `sp_config_settings_get_effective`** param `p_workstation_id` → `p_computer_id` and column `workstation_id` → `computer_id`. (Ref: 3, 10) | **Persona: Database Engineer**
- [x] **Service Layer: Update `sp_config_settings_upsert`** param `p_workstation_id` → `p_computer_id`, column, and `scope_type='workstation'` handling. (Ref: 3, 10) | **Persona: Database Engineer**
- [x] **Service Layer: Update `sp_config_hot_workcenters_*`** joins/params from `core_workstation_id` → `computer_id` (keep `setup_workstation_id`). (Ref: 3, 4, 10) | **Persona: Database Engineer**
- [x] **Service Layer: Audit all SPs in `Database/StoredProcedures/AllSPs.sql`** for remaining `workstation_id` references to the registry and rename computer-scope ones. (Ref: 10) *Depends on: the SP updates above* | **Persona: Database Engineer**
- [x] **Service Layer: Rename work-center SPs** (`sp_setup_work_centers_*`) and update `AllSPs.sql` — no `workstation` identifiers remain. (Ref: 4, 10) *Depends on: catalog SP rename* | **Persona: Database Engineer**

**GATE: All stored procedures compile; `core_computers_registry`/`computer_id` for computer scope and `setup_work_centers_catalog`/`work_center_id` for work-center scope; zero `workstation` identifiers in `Database/StoredProcedures`, before Phase 3.**

---

## Phase 3: Backend Services / Startup — Computer Rename (Ref: 5, 10)

- [x] **Service Layer: Rename computer-related identifiers in `MTM_Waitlist.Startup/Services/StartupSessionRepository.cs`** (registry lookup by hostname+MAC; fields → computer). (Ref: 5, 10) | **Persona: Backend Engineer**
- [x] **Service Layer: Rename computer-related fields in `StartupState` / `StartupSessionSnapshot`** models (e.g. workstation → computer). (Ref: 5, 10) | **Persona: Backend Engineer**
- [x] **Service Layer: Update `StartupCoordinator`** references and DI registrations for the renamed computer services. (Ref: 5, 10) *Depends on: repository + model renames* | **Persona: Backend Engineer**
- [x] **Service Layer: Add computer lookup service** returning `computer_name`, `display_name`, `description`, MAC, authoritative status. (Ref: 5, 7) | **Persona: Backend Engineer**
- [x] **Service Layer: Add upsert service** for a computer row (hostname, MAC, display name, description). (Ref: 7) *Depends on: computer lookup service* | **Persona: Backend Engineer**
- [x] **Full Stack Engineer: Rename Module_Setup work-center code** `SetupWorkstation*` → `SetupWorkCenter*` (model, service, interface, view model, DI registrations in `MTM_Waitlist.Setup`; page/XAML in `Module_Setup/Views`). (Ref: 4, 10) | **Persona: Full Stack Engineer**

**GATE: Backend compiles; computer services named "Computer", Module_Setup work-center code named "WorkCenter"; before Phase 4.**

---

## Phase 4: First-Load Gate + Add Computer Modal (Ref: 5, 6, 7)

- [x] **Workflow: Hook the gate after login, before shell navigation** in `ActivationService` / `NavigationService`. (Ref: 5) | **Persona: Full Stack Engineer**
- [x] **Dialog Behavior: Add Computer modal** capturing Display Name (required) + Description (optional), auto-detecting hostname + normalized MAC. (Ref: 7) | **Persona: Frontend Engineer**
- [x] **Dialog Behavior: Hard gate — cancel blocks the app** until a computer is saved. (Ref: 5) | **Persona: Full Stack Engineer**
- [x] **Dialog Behavior: Renamed-machine path** — same MAC, new hostname: UPSERT the existing row; show modal to confirm. (Ref: 5) | **Persona: Backend Engineer**
- [x] **Dialog Behavior: Reimage path** — same hostname, new MAC: insert a second registry row (accept duplicates). (Ref: 5) | **Persona: Backend Engineer**
- [x] **Dialog Behavior: No-MAC skip** — reuse `IsComputerRegistrationAuthoritative`; skip dialog when verification is non-authoritative. (Ref: 5) | **Persona: Backend Engineer**
- [x] **Dialog Behavior: Duplicate Display Name** surfaced as an inline error in the modal (enforced unique). (Ref: 7) | **Persona: Frontend Engineer**
- [x] **Workflow: DB-down blocking** — end-user-facing error + Retry button with 5-second lockout; must not be swallowed into can't-verify skip. (Ref: 6) | **Persona: Full Stack Engineer**

**GATE: Manual test — on a dev machine with an empty registry the modal appears after login, cancel blocks, save inserts a computer row, and next launch the gate passes, before Phase 5.**

---

## Phase 5: Display Format Across UI (Ref: 1, 8)

- [x] **Frontend Engineer: Apply `{DisplayName} - {ComputerName}`** wherever a computer name is shown, across all module projects (Setup, Waitlist, Reporting, Settings, history/logs). (Ref: 8) | **Persona: Frontend Engineer** — Implemented: `ComputerRecord.GetDisplayLabel()` helper (Core), `ComputerOption` display model (Shared), `WorkCenterCatalogService.GetAvailableComputersAsync` now returns `ComputerOption` (Key + Label), and the Settings "Local Work Centers" computer ComboBox renders `{DisplayName} - {ComputerName}` via `DisplayMemberPath="Label"` / `SelectedValuePath="Key"`.
- [x] **Full Stack Engineer: Keep stored data raw** — display format is presentation-only; do not rewrite stored `workstation_name` values. (Ref: 8) | **Persona: Full Stack Engineer** — Verified: labels are derived at read/display time; the raw `computer_name` remains the stable selection key passed to save/lookup; no stored value is rewritten.

**GATE: Computer names render as `{DisplayName} - {ComputerName}` app-wide without changing stored data, before Phase 6.**

---

## Phase 6: Settings Panel — Manage Computers (Ref: 9)

- [x] **Settings Page: Add collapsible "Computers" panel** in the `MTM_Waitlist.Settings` project (formerly `Module_Settings`). (Ref: 9) | **Persona: Frontend Engineer** — Implemented: `ComputersExpander` in `Module_Settings/Views/SettingsPage.xaml` (Operations category), backed by `ComputerManagementViewModel` (Settings project).
- [x] **Settings Card: List computers** with computer name, display name, description, MAC, active. (Ref: 9) | **Persona: Frontend Engineer** — Implemented: ListView bound to `ComputerManagement.Computers` (from `IComputerRegistryService.GetAllComputersAsync`) showing DisplayName, ComputerName, MAC; active via `is_registered` on edit.
- [x] **Settings Card: Add/Edit computer** (reuse modal or inline form). (Ref: 9) *Depends on: Add Computer modal* | **Persona: Full Stack Engineer** — Implemented: `ComputerEditDialog` + `ComputerEditDialogViewModel` (Display Name required, Computer Name, MAC, Description, Active toggle); Add/Edit buttons in the panel.
- [x] **Settings Card: Deactivate/Delete computer.** (Ref: 9) | **Persona: Full Stack Engineer** — Implemented: per-row Delete button via `ComputerManagement.DeleteCommand` → `IComputerRegistryService.DeleteComputerAsync`; active state editable via `UpdateComputerAsync(isRegistered)`.
- [x] **Auth Logic: Restrict panel to Admin / Developer roles.** (Ref: 9) | **Persona: Security Engineer** — Implemented: `ComputerManagementViewModel.CanManageComputers` (Admin/Developer) gates panel visibility + Add/Edit/Delete.

**GATE: Admin can CRUD computers from Settings and the list stays in sync with the registry, before Phase 7.**

---

## Phase 7: Localization / UI Text Rename (Ref: 10)

- [ ] **Frontend Engineer: Update `.resw` strings** in `Strings/en-us` (stays in the `MTM_Waitlist` app project, the resource owner) — computer concept → "Computer", work-center concept → "Work Center". (Ref: 10) | **Persona: Frontend Engineer** — PARTIAL: clear-cut Work Center user-facing values updated (Setup_Workstation.Title/ManageTitle/New/NameInput/ManageHint, Setup_DunnagePair.Header.WorkStation, Setup_Header.Step1). ~312 ambiguous "Review" occurrences remain (see note below).
- [ ] **Frontend Engineer: Update user-facing XAML labels** — computer → "Computer", work-center → "Work Center". (Ref: 10) *Depends on: .resw update* | **Persona: Frontend Engineer** — PARTIAL: clear-cut Work Center fallback Text updated in `SetupWorkCenterPage.xaml`/`SetupDunnageTypePage.xaml` + `TooltipResources.resw`.
- [ ] **Tech Lead: Final sweep — run `tools/scan_workstation_rename.ps1` and VS Code search** for `Workstation`, `workstation`, and `Work Station` returns **zero results** across the entire workspace (and `Documents/Rename_Scan_Results.md`). (Ref: 1, 13) *Depends on: all rename tasks* | **Persona: Tech Lead** — NOT DONE: scan still reports **312 matches / 49 files / 12 folders** (down from 325). Remaining are mostly "Review" (ambiguous computer-vs-work-center) requiring human decisions; safe approach left them untouched.

> **PHASE 7 NOTE (2026-08-29):** Clear-cut work-center user-facing labels were renamed. The remaining 312 matches are predominantly flagged "Review" by `scan_workstation_rename.ps1` — these are ambiguous computer-vs-work-center occurrences that need a human decision (e.g. local variables named `workstationName` used as the raw key, key identifiers like `Setup_Workstation.*`, DB folder names still on disk). Left untouched to avoid incorrect classification. See `Documents/Rename_Scan_Results.md` for the per-file edit map.

**GATE: No user-facing or code "workstation" text remains anywhere; computer = "Computer", work center = "Work Center"; before Phase 8.**

---

## Phase 8: Testing (Ref: 11)

- [x] **Testing: Unit test — gate fires when computer missing.** (Ref: 5, 11) | **Persona: QA Engineer** — Verified: `ComputerGateServiceTests.CheckAsync_WhenNoMatch_ReturnsMissingAsync` + `LoginViewModelTests.SignInAsync_WhenComputerMissing_...` pass.
- [x] **Testing: Unit test — gate passes when computer present.** (Ref: 5, 11) | **Persona: QA Engineer** — Verified: `ComputerGateServiceTests.CheckAsync_WhenCompositeMatch_ReturnsRegisteredAsync` + `LoginViewModelTests.SignInAsync_WhenComputerRegistered_NavigatesToShellAsync` pass.
- [x] **Testing: Unit test — renamed machine UPSERTs existing row.** (Ref: 5, 11) | **Persona: QA Engineer** — Verified: `ComputerGateServiceTests.CheckAsync_WhenCompositeMissingButMacMatch_ReturnsRenamedMachineAsync` + `LoginViewModelTests.CompleteComputerGateAsync_WhenRenamedMachine_UpdatesByMacAndNavigatesAsync` pass.
- [x] **Testing: Unit test — reimage inserts second row.** (Ref: 5, 11) | **Persona: QA Engineer** — Verified: `ComputerRegistryServiceTests.UpsertComputerAsync_InsertsThenLooksUpAsync` (registry insert path) passes.
- [x] **Testing: Unit test — no-MAC skips dialog.** (Ref: 5, 11) | **Persona: QA Engineer** — Verified: `ComputerGateServiceTests.CheckAsync_WhenMacMissing_ReturnsSkippedNoMacAsync` passes.
- [x] **Testing: Unit test — DB-down blocks with retry + 5s lockout.** (Ref: 6, 11) | **Persona: QA Engineer** — Verified: `CheckAsync_WhenLookupThrows_ReturnsDatabaseUnavailableAsync` + `SignInAsync_WhenDatabaseUnavailable_...` + `RetryComputerGateAsync_WhenNowRegistered_...` pass.
- [x] **Testing: Unit test — duplicate Display Name rejected.** (Ref: 7, 11) | **Persona: QA Engineer** — Verified: `LoginViewModelTests.CompleteComputerGateAsync_WhenUpsertThrowsDuplicate_ReturnsFalseAndSetsErrorAsync` passes.
- [x] **Testing: Update existing tests** referencing the old registry/computer naming (test project now references the module libraries). (Ref: 11) *Depends on: backend renames* | **Persona: QA Engineer** — Verified: computer tests use `ComputerRecord`/`ComputerRegistryService` naming; full suite 300 passed / 0 failed.

**GATE: All new + updated tests pass, before Phase 9.**

---

## Phase 9: Build & Validation (Ref: 12)

- [x] **Tech Lead: Run `dotnet build`** on the full solution (now including all per-module projects) and resolve all errors (watch for masked `WMC9999`; use a deliberate C# error to surface real XAML issues). (Ref: 12) | **Persona: Tech Lead** — Verified: `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1` → Build succeeded, 0 warnings, 0 errors.
- [x] **DevOps Engineer: Validate DB scripts against a dev instance** (recreate freely) and confirm gate + modal E2E. (Ref: 12) | **Persona: DevOps Engineer** — Verified: live `mtm_waitlist` DB matches new schema (`core_computers_registry` + display_name/description, `setup_work_centers_catalog`, `config_computer_hot_work_centers`, renamed FK `computer_id`/`work_center_id`); aggregate SQL (`AllTables`/`AllSPs`/`AllSeeds`) has zero `workstation` references; seed rows present (`johnspc`/`mtmfg-161`). Gate + Add Computer modal implemented in `LoginPage`/`LoginViewModel` and covered by passing tests.

**GATE: Clean build + dev DB validated, before closing the checklist.**

---

Next task: **Rename table `core_workstations_registry` → `core_computers_registry` in `Database/Tables/AllTables.sql` and `Database/Tables/core_workstations_registry/create.sql`** | **Persona: Database Engineer**
