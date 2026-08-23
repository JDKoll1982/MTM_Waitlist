# XAML Localization → per-File `.resw` Checklist

**Source of truth:** User workflow request (2026-08-23). "Go through every XAML file, view, dialog, page, hardcoded UI element." Requirements:

- **(Req 1)** Each XAML file gets its own individual `.resw` file at `Strings/en-us/{Module}/{File}.resw`.
- **(Req 2)** If not already implemented, create a Module_Core service usable by all modules to pull required resource data from its `.resw`. **Decision (confirmed by user): reuse the existing static helper `ResourceExtensions.GetLocalized()` as-is — no new service.**
- **(Req 3)** Update all files (XAML markup + code-behind + ViewModels) to use their `.resw`.
- **(Req 4)** Validate nothing was missed.
- **(Req 5)** Hardcoded user-facing strings in XAML **and** code-behind **and** ViewModels are in scope.

**Key style:** infer from existing conventions (e.g. `Setup_DunnageType.Title`, `Setup_Progress.Step4`, `Setup_WorkOrder.Validation.InvalidFormat`).

**Target:** All modules — Module_Core, Module_Startup, Module_Setup, Module_Waitlist (Views + Controls), Module_Settings, Module_Shared.

---

## Phase 1: Blueprint Discovery & Convention

### Subphase 1.1: Inventory & Scope Decision

- [ ] **Configuration: Confirm the full XAML inventory (55 files) is captured in this checklist** — root (2), Styles (7), Module_Core (1), Module_Startup (5), Module_Setup (9), Module_Waitlist Views (9), Module_Waitlist Controls (15), Module_Settings (5), Module_Shared (1). (Ref: Req 1) | **Persona: Tech Lead**
- [ ] **Configuration: Decide `.resw` placement for non-module XAML** — `App.xaml`, `MainWindow.xaml` (root), and `Styles/*.xaml` (7 resource dictionaries). Verify whether these contain user-facing strings needing extraction vs. only resource/style definitions. (Ref: Req 1) | **Persona: Tech Lead**
- [ ] **Configuration: Confirm `ResourceExtensions.GetLocalized()` in `Module_Core/Helpers/ResourceExtensions.cs` is the shared localization path** (already DI-agnostic static, no new service required). (Ref: Req 2) | **Persona: Tech Lead** — verified 2026-08-23: helper exists and is used across modules
- [ ] **Configuration: Verify `ResourceLoader` default `new ResourceLoader()` resolves per-file subtree keys** (e.g. `Setup_DunnageType.Title`) against the merged PRI map so per-file `.resw` keys resolve. (Ref: Req 2) | **Persona: Backend Engineer**
- [ ] **Configuration: Establish `.resw` key-naming template per file** — `<PagePrefix>.<ControlPurpose>` / `<PagePrefix>.Validation.<Key>` matching existing `Setup_*` conventions; record it in repo memory once approved. (Ref: Req 5) | **Persona: Tech Lead**

**GATE: Inventory and convention confirmed (all 55 files accounted for, naming template approved) before Phase 2.**

---

## Phase 2: Localization Service Confirmation

- [ ] **Service Layer: Verify `ResourceExtensions.GetLocalized()` handles missing keys gracefully** (returns key on empty / `COMException` / `FileNotFoundException` paths) so future `.resw` gaps degrade safely. (Ref: Req 2) | **Persona: Backend Engineer** — verified 2026-08-23: `ResourceExtensions.cs` has try/catch + empty-key fallback
- [ ] **Service Layer: Confirm no additional Module_Core service is required** — existing helper covers all modules; document the "reuse as-is" decision (no `ILocalizationService` to register). (Ref: Req 2) | **Persona: Backend Engineer**
- [ ] **Service Layer: Verify the named-map path used by `TooltipService`** (`ResourceManager.MainResourceMap` subtree lookup) is compatible with per-file `.resw` maps. (Ref: Req 2) | **Persona: Backend Engineer**

**GATE: Shared localization path confirmed as `ResourceExtensions.GetLocalized()` for all modules before Phase 3.**

---

## Phase 3: Module-by-Module `.resw` Creation & Migration

> Per module: **(A)** create `Strings/en-us/{Module}/` + one `.resw` per XAML file with extracted keys; **(B)** migrate XAML markup, code-behind, and ViewModel user-facing strings to `"...".GetLocalized()` (or the appropriate binding). Each file has one task for (A) and one for (B).

### Subphase 3.1: Module_Core

- [ ] **Settings Page: Create `Strings/en-us/Module_Core/ShellPage.resw`** with keys for `ShellPage.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Migrate `Module_Core/Views/ShellPage.xaml` (+ code-behind `ShellPage.xaml.cs`, `ShellViewModel.cs`) strings to resw.** (Ref: Req 3) | **Persona: Frontend Engineer**

### Subphase 3.2: Module_Startup

- [ ] **Settings Page: Create `Strings/en-us/Module_Startup/SplashWindow.resw`** and migrate `SplashWindow.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Startup/SplashView.resw`** and migrate `SplashView.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Startup/SplashPage.resw`** and migrate `SplashPage.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Startup/LoginWindow.resw`** and migrate `LoginWindow.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Startup/LoginPage.resw`** and migrate `LoginPage.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**

### Subphase 3.3: Module_Setup

- [ ] **Settings Page: Create `Strings/en-us/Module_Setup/SetupWorkstationPage.resw`** and migrate `SetupWorkstationPage.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Setup/SetupWorkOrderPage.resw`** and migrate `SetupWorkOrderPage.xaml` (+ `SetupWorkOrderViewModel.cs`, `WorkOrderValidationService.cs`). (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Setup/SetupSequenceSelectionPage.resw`** and migrate `SetupSequenceSelectionPage.xaml` (+ `SetupSequenceSelectionViewModel.cs`). (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Setup/SetupReviewPage.resw`** and migrate `SetupReviewPage.xaml` (+ `SetupReviewViewModel.cs`). (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Setup/SetupPartSelectionPage.resw`** and migrate `SetupPartSelectionPage.xaml` (+ `SetupPartSelectionViewModel.cs`). (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Setup/SetupDunnageTypePage.resw`** and migrate `SetupDunnageTypePage.xaml` (+ `SetupDunnageTypeViewModel.cs`). (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Setup/SetupDunnagePartPage.resw`** and migrate `SetupDunnagePartPage.xaml` (+ `SetupDunnagePartViewModel.cs`). (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Setup/SetupDunnageAddPartSelectionPage.resw`** and migrate `SetupDunnageAddPartSelectionPage.xaml` (+ `SetupDunnageAddPartSelectionViewModel.cs`). (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Setup/SetupCompletionPage.resw`** and migrate `SetupCompletionPage.xaml` (+ `SetupCompletionViewModel.cs`). (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Service Layer: Ensure Module_Setup shared services** (`SetupLookupService`, `SetupPersistenceService`, `SetupWorkflowService`) route all user-facing message strings through resw keys. (Ref: Req 5) | **Persona: Backend Engineer**

### Subphase 3.4: Module_Waitlist — Views

- [ ] **Settings Page: Create `Strings/en-us/Module_Waitlist/WaitlistViewPage.resw`** and migrate `WaitlistViewPage.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Waitlist/WaitlistViewDetailPage.resw`** and migrate `WaitlistViewDetailPage.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Waitlist/NewRequestWorkCenterPage.resw`** and migrate `NewRequestWorkCenterPage.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Waitlist/NewRequestSummaryPage.resw`** and migrate `NewRequestSummaryPage.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Waitlist/NewRequestSubtypePage.resw`** and migrate `NewRequestSubtypePage.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Waitlist/NewRequestResultPage.resw`** and migrate `NewRequestResultPage.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Waitlist/NewRequestPreviewPage.resw`** and migrate `NewRequestPreviewPage.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Waitlist/NewRequestJobTypePage.resw`** and migrate `NewRequestJobTypePage.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Waitlist/NewRequestDetailsPage.resw`** and migrate `NewRequestDetailsPage.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**

### Subphase 3.5: Module_Waitlist — Controls

- [ ] **Settings Page: Create `Strings/en-us/Module_Waitlist/WaitlistLineCardView.resw`** and migrate `Controls/WaitlistLineCardView.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Waitlist/TableHandlingRequestTypeImageView.resw`** and migrate `Controls/TableHandling/TableHandlingRequestTypeImageView.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Waitlist/CoilWaitlistLineView.resw`** and migrate `Controls/Coil/CoilWaitlistLineView.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Waitlist/CoilRequestTypeImageView.resw`** and migrate `Controls/Coil/CoilRequestTypeImageView.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Waitlist/ScrapWaitlistLineView.resw`** and migrate `Controls/Scrap/ScrapWaitlistLineView.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Waitlist/ScrapRequestTypeImageView.resw`** and migrate `Controls/Scrap/ScrapRequestTypeImageView.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Waitlist/PickupFgWaitlistLineView.resw`** and migrate `Controls/PickupFg/PickupFgWaitlistLineView.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Waitlist/ForkliftAssistRequestTypeImageView.resw`** and migrate `Controls/ForkliftAssist/ForkliftAssistRequestTypeImageView.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Waitlist/PickupWipWaitlistLineView.resw`** and migrate `Controls/PickupWip/PickupWipWaitlistLineView.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Waitlist/PickupRequestTypeImageView.resw`** and migrate `Controls/Pickup/PickupRequestTypeImageView.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Waitlist/FlatstockRequestTypeImageView.resw`** and migrate `Controls/Flatstock/FlatstockRequestTypeImageView.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Waitlist/PickupOsWaitlistLineView.resw`** and migrate `Controls/PickupOs/PickupOsWaitlistLineView.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Waitlist/OtherRequestTypeImageView.resw`** and migrate `Controls/Other/OtherRequestTypeImageView.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Waitlist/PickupNcmWaitlistLineView.resw`** and migrate `Controls/PickupNcm/PickupNcmWaitlistLineView.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Waitlist/DieHandlingRequestTypeImageView.resw`** and migrate `Controls/DieHandling/DieHandlingRequestTypeImageView.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**

### Subphase 3.6: Module_Settings

- [ ] **Settings Page: Create `Strings/en-us/Module_Settings/WorkCenterImagesDialog.resw`** and migrate `WorkCenterImagesDialog.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Settings/SettingsPage.resw`** and migrate `SettingsPage.xaml` (+ `SettingsViewModel.cs`). (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Settings/RequestTypeImagesDialog.resw`** and migrate `RequestTypeImagesDialog.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Settings/RequestSubtypeImagesDialog.resw`** and migrate `RequestSubtypeImagesDialog.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Create `Strings/en-us/Module_Settings/ImageOverrideEditorControl.resw`** and migrate `ImageOverrideEditorControl.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**

### Subphase 3.7: Module_Shared

- [ ] **Settings Page: Create `Strings/en-us/Module_Shared/ControlInspectorDetailPage.resw`** and migrate `ControlInspectorDetailPage.xaml`. (Ref: Req 1) | **Persona: Frontend Engineer**

### Subphase 3.8: Root & Styles (as decided in 1.1)

- [ ] **Settings Page: Extract `MainWindow.xaml` title string to a `.resw`** (currently `"AppDisplayName".GetLocalized()` — verify it routes through the shared path). (Ref: Req 3) | **Persona: Frontend Engineer**
- [ ] **Settings Page: Confirm `App.xaml` and `Styles/*.xaml` (7 files) need no `.resw`** (resource/style definitions only, no user-facing literals) or extract any found literals. (Ref: Req 1) | **Persona: Frontend Engineer**

**GATE: Every XAML file now has a matching `Strings/en-us/{Module}/{File}.resw` and uses it before Phase 4.**

---

## Phase 4: Validation — "Did we miss anything?"

- [ ] **Testing: Verify every XAML file has a corresponding `.resw`** — script/scan `Strings/en-us/**` vs XAML inventory; report orphans on either side. (Ref: Req 4) | **Persona: QA Engineer**
- [ ] **Testing: Grep for remaining hardcoded user-facing strings in XAML markup** (`Text="…"`, `Content="…"`, `PlaceholderText="…"`, `Header="…"`, `Title="…"`, tooltip literals) across all modules and confirm none remain. (Ref: Req 4) | **Persona: QA Engineer**
- [ ] **Testing: Grep for hardcoded user-facing strings in code-behind and ViewModels** (`new string`, string literals assigned to UI/`StatusMessage`/`Message`/`Title`) and confirm none remain. (Ref: Req 4) | **Persona: QA Engineer**
- [ ] **Testing: Verify every `.resw` key resolves** — spot-check via `GetLocalized()` (empty/`COMException` returns the raw key); confirm no page falls back to showing a raw key. (Ref: Req 4) | **Persona: QA Engineer**
- [ ] **Testing: Confirm `ResourceExtensions.GetLocalized()` reuse covers all modules** (no module bypasses it with a raw string). (Ref: Req 2) | **Persona: QA Engineer**
- [ ] **Testing: Build the app** (`dotnet build`) cleanly and run the `Module_Setup` / affected module test suites. (Ref: Req 4) *Depends on: Phase 3* | **Persona: QA Engineer**
- [ ] **Security Review: Confirm no `x:Uid`/resw refactor introduced XAML compile failures** (watch for the masked `WMC9999` / `PRI175` quirks — see repo instructions). (Ref: Req 4) | **Persona: Tech Lead**

**GATE: All validation passes (0 orphans, 0 remaining hardcoded strings, clean build+tests) before proceeding to Phase 5.**

---

## Phase 5: Regression Guard — Automated Localization Tests

> Create an automated xUnit test suite in `MTM_Waitlist.Tests` that enforces the workflow's invariants so it cannot silently revert. **Decision (confirmed by user): automated test suite only (no separate CLI tool), run locally via `dotnet test` — NOT wired into CI.**

- [ ] **Testing: Create a localization regression test fixture** in `MTM_Waitlist.Tests` that enumerates the XAML inventory (55 files) and the `Strings/en-us/**/*.resw` inventory at test time (filesystem scan, not hardcoded list). (Ref: Req 4) | **Persona: Backend Engineer**
- [ ] **Testing: Implement `resw` ↔ XAML parity test** — assert every `.xaml` under each module has a matching `Strings/en-us/{Module}/{File}.resw`, and every `.resw` has a matching XAML file (no orphans on either side). (Ref: Req 4) *Depends on: fixture* | **Persona: Backend Engineer**
- [ ] **Testing: Implement no-hardcoded-strings test for XAML markup** — scan `Text=`, `Content=`, `PlaceholderText=`, `Header=`, `Title=`, and tooltip literals across module XAML; assert none reference raw user-facing literals. (Ref: Req 4) *Depends on: fixture* | **Persona: Backend Engineer**
- [ ] **Testing: Implement no-hardcoded-strings test for code-behind + ViewModels** — scan `.cs` in module Views/ViewModels/Services for user-facing string literals assigned to UI/`StatusMessage`/`Message`/`Title`; assert they route through `GetLocalized()`/resw. (Ref: Req 4) *Depends on: fixture* | **Persona: Backend Engineer**
- [ ] **Testing: Implement key-resolution test** — for every key referenced in code and present in a `.resw`, assert `ResourceExtensions.GetLocalized(key)` resolves to a non-empty value (no raw-key fallback). (Ref: Req 4) *Depends on: fixture* | **Persona: Backend Engineer**
- [ ] **Testing: Implement `Resources.resw` cleanliness test** — assert the central `Strings/en-us/Resources.resw` has no duplicate keys and no keys orphaned/migrated to per-file `.resw` while still unreferenced. (Ref: Req 4) *Depends on: fixture* | **Persona: Backend Engineer**
- [ ] **Testing: Run the localization regression suite locally via `dotnet test`** and confirm all tests pass green against the completed migration. (Ref: Req 4) *Depends on: all test tasks above* | **Persona: QA Engineer**

**GATE: The localization regression suite exists, passes green locally, and catches reversion (parity, hardcoded strings, key resolution, `Resources.resw` cleanliness) before Phase 6.**

---

## Phase 6: Central `Resources.resw` Dedup & Cleanup (driven by Phase 5 regression guard)

> After Phase 3 migrates keys into per-file `.resw` files, the legacy shared `Strings/en-us/Resources.resw` may hold duplicate or now-orphaned keys. **This phase uses the Phase 5 `Resources.resw` cleanliness test to find and drive the cleanup** — fix the failures it reports, re-run until green.

- [ ] **Testing: Run the Phase 5 `Resources.resw` cleanliness test** against the current central `Resources.resw` to enumerate duplicate and orphaned keys (it already reports keys appearing more than once and keys migrated to per-file `.resw` that are no longer referenced). (Ref: Req 4) *Depends on: Phase 5 test suite* | **Persona: QA Engineer**
- [ ] **Configuration: Remove duplicate `<data>` entries from `Strings/en-us/Resources.resw`** flagged by the test — keep exactly one canonical entry per key (prefer the value already used by code). (Ref: Req 4) *Depends on: test run* | **Persona: Frontend Engineer**
- [ ] **Configuration: Remove orphaned/migrated keys from `Strings/en-us/Resources.resw`** flagged by the test — delete entries fully superseded by per-file `.resw` files; retain still-referenced keys (e.g. `AppDisplayName`, `AppNotificationSamplePayload`, shared `Setup_Action.*`/`Setup_Header.*`/`NewRequest_Header.*` if still used). (Ref: Req 4) *Depends on: test run* | **Persona: Frontend Engineer**
- [ ] **Testing: Re-run the Phase 5 regression suite** (parity, hardcoded strings, key resolution, `Resources.resw` cleanliness) after cleanup and confirm all green — no raw-key fallback and no broken references. (Ref: Req 4) *Depends on: removal tasks* | **Persona: QA Engineer**
- [ ] **Testing: Build the app** (`dotnet build`) cleanly and run the affected module test suites after `Resources.resw` cleanup. (Ref: Req 4) *Depends on: cleanup* | **Persona: QA Engineer**

**GATE: The Phase 5 regression suite passes green (0 duplicate keys, 0 orphaned keys still referenced) and the build/tests pass before declaring the workflow complete.**

---

## Notes / Decisions Logged

- **Service:** Reuse existing `ResourceExtensions.GetLocalized()` (Module_Core) — user confirmed, no new `ILocalizationService`.
- **Scope:** XAML + code-behind + ViewModels hardcoded user-facing strings.
- **Key style:** Infer existing conventions (`Setup_DunnageType.Title`, `Setup_Progress.Step4`).
- **Target:** All modules (55 XAML files enumerated above).

Next task: **Confirm the full XAML inventory (55 files) is captured in this checklist** | **Persona: Tech Lead**
