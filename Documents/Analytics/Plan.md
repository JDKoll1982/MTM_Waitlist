## Plan: Supervisor Analytics Page V1

Create a new role-gated Supervisor Analytics page integrated into the existing WinUI 3 shell navigation and MVVM/DI patterns, using mock analytics data first, with user/shift/building/plant filtering, near-real-time refresh defaults (5 min), print-friendly export, and user-scoped persisted preferences in MySQL 5.7. Reuse existing building-selection UX patterns from Waitlist and design extension points so additional analytics can be added without reshaping page architecture.

**Execution Gate**
- This plan is intentionally deferred until startup phases 01 through 09 are completed.
- Start implementation only when `StartupPhases/README.md` reflects completion of all startup phases.
- No analytics page routing, UI, services, or settings persistence work should be merged before that milestone.

**Steps**
1. Phase 1 - Blueprint and Contract Discovery
	1. Confirm existing navigation and role gates to reuse from Shell and StartupState patterns. Depends on existing symbols in App startup and shell view model.
	2. Confirm Waitlist building-filter interaction model and settings persistence touchpoints to avoid parallel filter systems. Parallel with role-gate review.
	3. Research authoritative guidance for: WinUI 3 print-friendly patterns, MySQL 5.7 user-scoped settings schema/access strategy, and resilient timer/refresh patterns for MVVM. Use context7 + Microsoft Learn before implementation starts.

2. Phase 2 - Feature Contract and Data Shapes
	1. Define Supervisor Analytics feature contract with these access roles: Production Lead, Setup Lead, Plant Manager, Developer.
	2. Define analytics segmentation contract for day-one user types: Material Handler, Production, Setup.
	3. Define filter contract: User Type, Shift, Building selector values (All, Expo Drive, Vits Drive), Plant-wide aggregation behavior, and role-switch behavior.
	4. Define mock-data contract to mirror expected future user-creation shift data and ensure smooth swap to real source later.
	5. Define settings contract: per-user persisted preferences including auto-refresh interval default 5 minutes, visible KPI cards/tables/charts toggles, default filters, and building list management intent.

3. Phase 3 - UI/VM Architecture Plan
	1. Add a dedicated Supervisor Analytics page + view model pair following existing page registration patterns and INavigationAware loading lifecycle. Depends on Phase 2 contracts.
	2. Add role-based visibility logic in the view model, with explicit deny behavior for non-approved roles.
	3. Design page sections as composable role/user-type analytics panels so each user type renders a distinct KPI + table + chart set.
	4. Plan chart/table customization entry in settings workflow as per-user options, keeping options bindable and testable.
	5. Plan print-friendly view path for PDF workflow (OS/browser print to PDF), scoped to currently filtered data.

4. Phase 4 - Services and Persistence Plan
	1. Introduce analytics service abstraction returning role/user-type segmented data with filter parameters and async loading.
	2. Implement mock analytics provider for v1 with deterministic seeded data suitable for tests and demos.
	3. Define MySQL 5.7 persistence for per-user analytics settings, including schema migration location under Database and data access boundary in service layer.
	4. Reuse or extend existing settings and startup services to load/save analytics preferences safely and fail gracefully.

5. Phase 5 - Navigation, Localization, and Access Wiring
	1. Add a top-level shell navigation item for Supervisor Analytics, visible only for approved roles.
	2. Register page/view model/service dependencies in DI and page mapping.
	3. Add localized resource keys for navigation labels, filter labels, KPI labels, and export action text.

6. Phase 6 - Test and Validation Plan
	1. Add unit tests for role access logic, filter composition, segmentation logic, and refresh cadence behavior.
	2. Add service tests for mock provider outputs and settings persistence serialization/deserialization.
	3. Add integration-level navigation test coverage for page registration and route resolution.
	4. Run build and test validation and document known v1 limitations.

7. Phase 7 - Documentation and Future Extensibility
	1. Add implementation notes in Database/Documents covering user-scoped settings in MySQL 5.7, schema, and operational constraints.
	2. Add contributor documentation describing how to add new analytics modules to the page without breaking existing MVVM composition.
	3. Document v1 assumptions: mock shift source, print-friendly export path, and eventual migration path to real user/shift data source.

**Relevant files**
- c:/Users/johnk/source/repos/MTM_Waitlist/App.xaml.cs — DI host setup and app service registration root.
- c:/Users/johnk/source/repos/MTM_Waitlist/Services/ServiceRegistrationExtensions.cs — register new analytics page/viewmodel/services in existing pattern.
- c:/Users/johnk/source/repos/MTM_Waitlist/Services/PageService.cs — map view model full name to analytics page type.
- c:/Users/johnk/source/repos/MTM_Waitlist/Views/ShellPage.xaml — top-level nav item placement and conditional visibility hook.
- c:/Users/johnk/source/repos/MTM_Waitlist/ViewModels/ShellViewModel.cs — role-aware nav visibility pattern reference.
- c:/Users/johnk/source/repos/MTM_Waitlist/Models/StartupState.cs — current role source and access checks baseline.
- c:/Users/johnk/source/repos/MTM_Waitlist/Views/WaitlistViewPage.xaml — building dropdown/filter UX baseline to reuse.
- c:/Users/johnk/source/repos/MTM_Waitlist/ViewModels/WaitlistViewViewModel.cs — filter-state and service-binding pattern to mirror.
- c:/Users/johnk/source/repos/MTM_Waitlist/Services/BuildingSelectionService.cs — current building source and change event model.
- c:/Users/johnk/source/repos/MTM_Waitlist/Contracts/Services/IBuildingSelectionService.cs — contract baseline for All/plant-wide extension.
- c:/Users/johnk/source/repos/MTM_Waitlist/Strings/en-us/Resources.resw — localization keys for nav and analytics UI strings.
- c:/Users/johnk/source/repos/MTM_Waitlist/MTM_Waitlist.Tests/Services/BuildingSelectionServiceTests.cs — existing service test style.
- c:/Users/johnk/source/repos/MTM_Waitlist/MTM_Waitlist.Tests/Services/PageServiceTests.cs — page registration test pattern.
- c:/Users/johnk/source/repos/MTM_Waitlist/Database/Documents — required documentation target for MySQL user-scoped settings guidance.

**Verification**
1. Validate role gate behavior by testing each relevant role and confirming navigation visibility + access enforcement.
2. Validate filter behavior matrix: user type, shift, building (All/Expo Drive/Vits Drive), and plant-wide aggregation combinations.
3. Validate refresh loop: default 5-minute cadence, manual refresh button behavior, and per-user persisted override.
4. Validate print-friendly output reflects active filters and excludes hidden sections.
5. Validate localization coverage for all new labels/actions and fallback behavior.
6. Validate build/tests pass for app and test projects.
7. Validate documentation completeness in Database/Documents with setup and extension instructions.

**Decisions**
- Included scope: new supervisor analytics page with role-gated top-level navigation and role-dependent views.
- Included scope: day-one analytics for Material Handler, Production, and Setup user types.
- Included scope: near-real-time with manual refresh button and persisted per-user cadence default.
- Included scope: print-friendly export path for PDF via system print.
- Included scope: mock data-first design compatible with future real shift/user source.
- Enforcement for v1: UI visibility + ViewModel guard.
- Building source of truth for v1: keep BuildingSelectionService and persist changes.
- Charting approach for v1: mixed strategy while keeping implementation package-agnostic (native controls + optional lightweight package abstraction + table-first placeholders) using placeholder data initially.
- Settings schema location for v1 planning: Database/Documents only (design spec).
- User identity strategy (pending implementation): resolve App User GUID first, then map to first/last name.
- Add a local lookup-table contract now (mock-friendly) for GUID-to-name resolution, to be swapped with real user-management source later.
- Delivery sequence lock: finish all 9 startup phases first, then begin analytics implementation.
- Excluded scope: implementing full user-creation workflow and authoritative shift source in this phase.
- Excluded scope: direct PDF generation engine in v1.

**Further Considerations**
1. Building management source of truth: Option A keep in existing building-selection service and persist changes, Option B move to dedicated Plant/Building settings service, Option C hybrid adapter preserving existing API.
2. Access enforcement depth: Option A UI visibility only, Option B UI + view-model guard, Option C UI + view-model + service-level authorization checks.
3. Charting strategy: Option A native WinUI controls only, Option B lightweight chart package vetted via context7 docs, Option C table-first with chart placeholders for staged rollout.