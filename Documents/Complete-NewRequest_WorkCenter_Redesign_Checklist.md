# New Request Work Center Selection — Card Redesign Checklist

This checklist applies the workstation-selection card workflow already implemented on
`Module_Setup/Views/SetupWorkstationPage.xaml` (and `SetupWorkstationViewModel`) to
`Module_Waitlist/Views/NewRequestWorkCenterPage.xaml` so both selection screens look and behave the same.

**Decisions (locked 2026-08-22):**

- **Card content**: Full parity with Setup — Name, Building, Last Updated, plus active-job detail (Current Job / Part Number) rows with 1px dividers. No "View History" button on this page.
- **Selection flow**: KEEP click-to-navigate. A single click validates (active job + employee verification) then navigates to the Job Type step. The blue card highlight is transient.
- **Other section**: "Other Work Centers" becomes a collapsible `Expander` (collapsed by default, header toggles "Show/Hide Other Work Centers"). When this computer has no Local work centers, hide the Local section and auto-expand Other.
- **Search/filter**: Full parity — add the Setup-style search box AND building filter.
- **Responsive**: Reuse the same 520–580 × 300 `ItemsWrapGrid` sizing logic.
- **Preserve**: The `InfoBar` warnings (no active setup job, employee verification) and the Cancel action bar are unchanged.

Reference implementations (copy the pattern, do not duplicate logic in new layers):

- `Module_Setup/Views/SetupWorkstationPage.xaml` — `WorkstationCardItemContainerStyle`, `WorkstationCardTemplate`.
- `Module_Setup/Views/SetupWorkstationPage.xaml.cs` — responsive sizing.
- `Module_Setup/ViewModels/SetupWorkstationViewModel.cs` — filter/section logic.
- `Module_Setup/Models/SetupModels.cs` — `SetupWorkstation` observable model pattern.
- `Module_Shared/Services/WorkCenterCatalogService.cs` — catalog + per-work-center detail source.

---

**GATE: Phase 1 enriches the shared catalog so every work center has Building, Last Updated, and active-job detail. The UI phases (2–4) depend on this data being available. Phase 1 must not regress the Setup page or WaitlistView consumers of `IWorkCenterCatalogService`.**

---

## Phase 1: Data Layer — Enrich Work Center Detail

### Subphase 1.1: Shared Model & Catalog Result

- [x] **Model: Add `Building`, `LastUpdatedUtc` (DateTime?), `HasActiveJob`, `CurrentWorkOrder`, `CurrentPartNumber`, and `CurrentSequenceNumber` properties (with safe defaults) to `WorkCenterSelectionItem`** so existing callers compile unchanged. (Ref: Module_Shared/Models/WorkCenterSelectionItem.cs) | **Persona: Backend Engineer**
- [x] **Model: Add computed display props `CurrentJobSummary`, `CurrentPartSummary`, and `LastUpdatedDisplay` (format `MMM d, yyyy h:mm tt` local, `Never` when null) to `WorkCenterSelectionItem`, mirroring `SetupWorkstation`.** (Ref: Module_Setup/Models/SetupModels.cs) | **Persona: Backend Engineer**
- [x] **Model: Change `WorkCenterSelectionItem` to derive from `ObservableObject` and add an observable `IsSelected` property** so the card can render the selection border/frame. (Ref: Module_Setup/Models/SetupModels.cs) | **Persona: Backend Engineer**
- [x] **Model: Add a `WorkCenterDetail` record (Building, LastUpdatedUtc, HasActiveJob, CurrentWorkOrder, CurrentPartNumber, CurrentSequenceNumber) in Module_Shared/Models.** (Ref: Module_Shared/Models/) | **Persona: Backend Engineer**
- [x] **Catalog: Add `IReadOnlyDictionary<string, WorkCenterDetail> WorkCenterDetails` to `WorkCenterCatalogResult`** (case-insensitive keyed by work-center name). (Ref: Module_Shared/Models/WorkCenterCatalogResult.cs) | **Persona: Backend Engineer**

Next task: **SQL: Extend the `WorkCenterCatalogService` catalog queries to also select `building` and `updated_utc` from `setup_workstations_catalog` for every returned work center.** | **Persona: Database Engineer**

### Subphase 1.2: Catalog SQL & Mapping

- [x] **SQL: Extend the `WorkCenterCatalogService` catalog queries to also select `building` and `updated_utc` from `setup_workstations_catalog` for every returned work center.** (Ref: Module_Shared/Services/WorkCenterCatalogService.cs, Database/Tables/) | **Persona: Database Engineer**
- [x] **SQL: Load the latest active job (work order / part / sequence) per work center by executing `sp_setup_active_jobs_latest_by_work_center_get`** (existing SP used by `ISetupWorkstationService.GetWorkstationsAsync`). (Ref: Database/StoredProcedures/sp_setup_active_jobs_latest_by_work_center_get/create.sql) | **Persona: Database Engineer**
- [x] **Service: Populate `WorkCenterDetails` in `GetCatalogAsync`**, merging building/updated_utc with the active-job detail, keyed by work-center name. (Ref: Module_Shared/Services/WorkCenterCatalogService.cs) | **Persona: Backend Engineer**
- [x] **Service: Confirm `HotWorkCenters`, `OtherWorkCenters`, and `ActiveJobWorkCenters` behavior is unchanged** — no regression for the Setup page or WaitlistView. (Ref: Module_Shared/Services/WorkCenterCatalogService.cs) | **Persona: Backend Engineer**

Next task: **XAML: Copy `WorkstationCardItemContainerStyle` (strips GridView selection visuals — transparent pointer/selected backgrounds, `SelectedBorderThickness="0"`, `SelectionCheckMarkVisualEnabled="False"`) into `NewRequestWorkCenterPage.xaml` Page.Resources.** | **Persona: Frontend Engineer**

---

**GATE: `GetCatalogAsync` now returns per-work-center Building + Last Updated + active-job detail, and all existing consumers still pass their tests. Proceed to the UI phases only after Phase 1 is verified.**

---

## Phase 2: Card UI & Section Layout (XAML)

### Subphase 2.1: Card Template

- [x] **XAML: Copy `WorkstationCardItemContainerStyle` (strips GridView selection visuals — transparent pointer/selected backgrounds, `SelectedBorderThickness="0"`, `SelectionCheckMarkVisualEnabled="False"`) into `NewRequestWorkCenterPage.xaml` Page.Resources.** (Ref: Module_Setup/Views/SetupWorkstationPage.xaml) | **Persona: Frontend Engineer**
- [x] **XAML: Add a `WorkstationCardTemplate` DataTemplate (`x:DataType="WorkCenterSelectionItem"`) mirroring Setup**: 300px square photo pane (ImageBrush `UniformToFill` + neutral `CardStrokeColorDefaultBrush` frame + blue `SystemControlHighlightAccentBrush` frame overlay on `IsSelected`), 1px divider column, and a text panel with Name/Building header, then Current Job, Part Number, and Last Updated rows separated by 1px `CardStrokeColorDefaultBrush` dividers. (Ref: Module_Setup/Views/SetupWorkstationPage.xaml) | **Persona: Frontend Engineer**
- [x] **XAML: Bind card rows to `WorkCenterName`, `Building`, `CurrentJobSummary`, `CurrentPartSummary`, `LastUpdatedDisplay`, and `IsSelected`** (using the global `ResolvedImagePathToSourceConverter` for the image and `BoolToVisibilityConverter` for the selection overlays). (Ref: Module_Setup/Views/SetupWorkstationPage.xaml) | **Persona: Frontend Engineer**
- [x] **XAML: Include a blue selection-outline `Border` overlay (2px, CornerRadius 12, `IsHitTestVisible="False"`) on the card, visible only when `IsSelected`** — do NOT add a "View History" button. (Ref: Module_Setup/Views/SetupWorkstationPage.xaml) | **Persona: Frontend Engineer**

Next task: **XAML: Wrap the "Local Work Centers" section `Border` with `Visibility` bound to `IsLocalWorkCentersVisible` (via `BoolToVisibilityConverter`).** | **Persona: Frontend Engineer**

### Subphase 2.2: Sections, Search & Filter

- [x] **XAML: Wrap the "Local Work Centers" section `Border` with `Visibility` bound to `IsLocalWorkCentersVisible` (via `BoolToVisibilityConverter`).** (Ref: Module_Setup/Views/SetupWorkstationPage.xaml) | **Persona: Frontend Engineer**
- [x] **XAML: Replace the "Other Work Centers" `Border` with an `Expander`** (`IsExpanded` TwoWay-bound to `IsOtherWorkCentersExpanded`; header `TextBlock` bound to `OtherWorkCentersHeader`; content = the Other GridView inside a `StackPanel` with top margin). (Ref: Module_Setup/Views/SetupWorkstationPage.xaml) | **Persona: Frontend Engineer**
- [x] **XAML: Add a search `TextBox` (TwoWay to `FilterText`, `UpdateSourceTrigger=PropertyChanged`) and a building `ComboBox` (ItemsSource `Buildings`, TwoWay to `SelectedBuilding`) to the page header**, matching Setup's search UX. (Ref: Module_Setup/Views/SetupWorkstationPage.xaml) | **Persona: Frontend Engineer**
- [x] **XAML: Set both GridViews to `SelectionMode="None"`, `IsItemClickEnabled="True"`, apply `WorkstationCardItemContainerStyle` + `WorkstationCardTemplate`, and replace the fixed `MaximumRowsOrColumns` `ItemsWrapGrid` with an orientation-only `ItemsWrapGrid`** (the code-behind drives widths). (Ref: Module_Setup/Views/SetupWorkstationPage.xaml) | **Persona: Frontend Engineer**
- [x] **XAML: Keep both `InfoBar` warnings (no active setup job, employee verification) and the Cancel action bar unchanged.** (Ref: Module_Waitlist/Views/NewRequestWorkCenterPage.xaml) | **Persona: Frontend Engineer**

Next task: **VM: Inject `IBuildingSelectionService` into `NewRequestWorkCenterViewModel` and expose `Buildings`, `SelectedBuilding`, `FilterText`, `IsOtherWorkCentersExpanded` (default `false`), `IsLocalWorkCentersVisible` (default `true`), and `OtherWorkCentersHeader` (+ `OnIsOtherWorkCentersExpandedChanged` raising `OnPropertyChanged`).** | **Persona: Backend Engineer**

---

**GATE: The page renders Setup-style cards, Local section hides when empty, Other collapses, and search + building filter are present. Bindings compile cleanly (no WMC0001 — any new resources are registered in App.xaml).**

---

## Phase 3: ViewModel Behavior

### Subphase 3.1: Filter & Section Visibility

- [x] **VM: Inject `IBuildingSelectionService` into `NewRequestWorkCenterViewModel`** and expose `Buildings`, `SelectedBuilding`, `FilterText`, `IsOtherWorkCentersExpanded` (default `false`), `IsLocalWorkCentersVisible` (default `true`), and `OtherWorkCentersHeader` (+ `OnIsOtherWorkCentersExpandedChanged` raising `OnPropertyChanged`). (Ref: Module_Setup/ViewModels/SetupWorkstationViewModel.cs) | **Persona: Backend Engineer**
- [x] **VM: Add `ApplyFilter()` that filters by selected building + filter text (name / work order / part), then splits into `HotWorkCenters` / `OtherWorkCenters`** preserving the hot set membership. (Ref: Module_Setup/ViewModels/SetupWorkstationViewModel.cs) | **Persona: Backend Engineer**
- [x] **VM: Add `UpdateWorkCenterSectionsVisibility()`** — `IsLocalWorkCentersVisible = hasLocalWorkCenters`; `IsOtherWorkCentersExpanded = !hasLocalWorkCenters`. (Ref: Module_Setup/ViewModels/SetupWorkstationViewModel.cs) | **Persona: Backend Engineer**
- [x] **VM: In `OnNavigatedTo`, set `SelectedBuilding` from the building service and subscribe to `BuildingChanged`; unsubscribe in `OnNavigatedFrom`** (call `ApplyFilter` on building change). (Ref: Module_Setup/ViewModels/SetupWorkstationViewModel.cs) | **Persona: Backend Engineer**

Next task: **VM: Extend `CreateSelectionItems` to map each work-center name to its `WorkCenterDetail` (Building, LastUpdatedUtc, HasActiveJob, job detail) and default `IsSelected=false`.** | **Persona: Backend Engineer**

### Subphase 3.2: Selection & Data Mapping

- [x] **VM: Extend `CreateSelectionItems` to map each work-center name to its `WorkCenterDetail`** (Building, LastUpdatedUtc, HasActiveJob, job detail) and default `IsSelected=false`. (Ref: Module_Waitlist/ViewModels/NewRequestWorkCenterViewModel.cs) | **Persona: Backend Engineer**
- [x] **VM: In `SelectWorkCenter`, set `IsSelected` on the clicked item (clearing the others) before validation** — the highlight is transient because the page navigates on click. (Ref: Module_Waitlist/ViewModels/NewRequestWorkCenterViewModel.cs) | **Persona: Backend Engineer**
- [x] **VM: Preserve the active-job block, employee verification, and navigation to `NewRequestJobTypeViewModel` unchanged.** (Ref: Module_Waitlist/ViewModels/NewRequestWorkCenterViewModel.cs) | **Persona: Backend Engineer**

Next task: **Code-behind: Add responsive constants (`MinItemWidth = 520`, `MaxItemWidth = 580`, `ItemHeight = 300`, `ItemGap = 16`) and subscribe to both `HotWorkCenters` / `OtherWorkCenters` `CollectionChanged` plus `Loaded`/`Unloaded` (unsubscribe on Unloaded).** | **Persona: Frontend Engineer**

---

**GATE: The page filters by building/text, section visibility toggles correctly, and a click still validates then navigates. Phase 4 is only possible after Phase 3 so the grids actually have items to size.**

---

## Phase 4: Responsive Sizing (Code-Behind)

- [x] **Code-behind: Add responsive constants (`MinItemWidth = 520`, `MaxItemWidth = 580`, `ItemHeight = 300`, `ItemGap = 16`) and subscribe to both `HotWorkCenters` / `OtherWorkCenters` `CollectionChanged` plus `Loaded`/`Unloaded` (unsubscribe on Unloaded).** (Ref: Module_Setup/Views/SetupWorkstationPage.xaml.cs) | **Persona: Frontend Engineer**
- [x] **Code-behind: Add `SizeChanged` handlers on both GridViews calling `UpdateItemSize()`** — compute columns from the max grid `ActualWidth`, clamp item width, and set `ItemWidth`/`ItemHeight` on both `ItemsWrapGrid` via `ApplyItemSize`. (Ref: Module_Setup/Views/SetupWorkstationPage.xaml.cs) | **Persona: Frontend Engineer**
- [x] **Code-behind: Retry via `DispatcherQueue.TryEnqueue(UpdateItemSize)` only when a grid has items AND `ActualWidth > 0`** so a collapsed Expander never spins the loop. (Ref: Module_Setup/Views/SetupWorkstationPage.xaml.cs) | **Persona: Frontend Engineer**
- [x] **Code-behind: Update the `ItemClick` handlers to set the clicked item as the selected one (highlight) then execute `SelectWorkCenterCommand`.** (Ref: Module_Waitlist/Views/NewRequestWorkCenterPage.xaml.cs) | **Persona: Frontend Engineer**

Next task: **Tests: Create `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/NewRequestWorkCenterViewModelTests.cs` covering: catalog load populates hot/other with detail, building filter, filter text, no-local → hide Local + expand Other, header toggling, click selects + navigates, and the active-job block.** | **Persona: QA Engineer**

---

**GATE: Cards never clip at any window size or resolution and look identical to the Setup page. Then proceed to tests.**

---

## Phase 5: Tests & Validation

### Subphase 5.1: Unit Tests

- [x] **Tests: Create `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/NewRequestWorkCenterViewModelTests.cs`** covering: catalog load populates hot/other with detail, building filter, filter text, no-local → hide Local + expand Other, header toggling, click selects + navigates, and the active-job block. (Ref: MTM_Waitlist.Tests/Module_Waitlist/) | **Persona: QA Engineer**
- [x] **Tests: Add coverage for `WorkCenterSelectionItem` computed displays (`CurrentJobSummary`, `CurrentPartSummary`, `LastUpdatedDisplay` incl. `Never`) and the catalog `WorkCenterDetails` mapping.** (Ref: MTM_Waitlist.Tests/) | **Persona: QA Engineer**
- [x] **Tests: Verify existing `IWorkCenterCatalogService` consumers (Setup page, WaitlistView) still pass.** (Ref: MTM_Waitlist.Tests/Module_Setup/, MTM_Waitlist.Tests/Module_Waitlist/) | **Persona: QA Engineer**

Next task: **Build: Stop any running `MTM_Waitlist.exe`, then `dotnet build` (Debug, `net10.0-windows10.0.19041.0`, `WindowsPackageType=None`, `WinUISDKReferences=false`); resolve any `PRI175`/`PRI224` by cleaning stale `*.pri` under `obj/`/`bin/`.** | **Persona: Full Stack Engineer**

### Subphase 5.2: Build & Test Run

- [x] **Build: Stop any running `MTM_Waitlist.exe`, then `dotnet build` (Debug, `net10.0-windows10.0.19041.0`, `WindowsPackageType=None`, `WinUISDKReferences=false`)**; resolve any `PRI175`/`PRI224` by cleaning stale `*.pri` under `obj/`/`bin/`. (Ref: MTM_Waitlist.csproj) | **Persona: Full Stack Engineer**
- [x] **Tests: Run the `Module_Setup` and `Module_Waitlist` test filters; all pass.** (Ref: MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj) | **Persona: Full Stack Engineer**
- [x] **Verification: Manually confirm on the New Request wizard** — Local section shows for a configured workstation, Other collapses by default, search + building filter work, clicking a card validates and navigates, and cards look identical to the Setup page. (Ref: Module_Waitlist/Views/NewRequestWorkCenterPage.xaml) | **Persona: Full Stack Engineer**

---

**GATE: All phases complete, build is clean, and both `Module_Setup` + `Module_Waitlist` test filters pass.**

---

## Personas Reference

| Persona | Role in this checklist |
| --------- | ------------------------ |
| **Database Engineer** | Extend catalog SQL to select building/updated_utc; load latest active job per work center. |
| **Backend Engineer** | Shared models, catalog result/detail mapping, view-model filter/section/selection logic. |
| **Frontend Engineer** | Card template + container style, Expander/search/filter XAML, responsive sizing code-behind. |
| **QA Engineer** | View-model and model unit tests; consumer regression checks. |
| **Full Stack Engineer** | Build + full test filters + manual wizard verification. |
