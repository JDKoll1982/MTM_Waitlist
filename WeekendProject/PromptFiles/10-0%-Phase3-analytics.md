# 10 - Phase 3 — Analytics: Stock Snapshot + Plant Manager Waitlist Screen

> **Source:** Master `WeekendProject/PromptFiles/prompt.md` (Tasks 20, 21). Self-contained. Builds on audit/lifecycle data (file `01`) and stock lookup (file `02`/`06`).
> **Workflow:** checklist-execution skill; adopt each task's persona. Tick `- [x]` only when implemented, builds clean, and tests pass.
> **Task 0 scope:** green build + full suite pass before/after this file.

## Subphase 0 — Task 0: Green build & full-suite baseline

- [ ] **DevOps: `MTM_Waitlist.sln` builds clean (0 errors, 0 warnings)** via `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false`. | **Persona: DevOps Engineer**
- [ ] **QA: full `MTM_Waitlist.Tests` suite passes** (`dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64`). | **Persona: QA Engineer**
**GATE: 0 errors + 0 warnings + full green suite before/after this file.**

## UI Guidance (embedded — analytics screen)

- Add a role-gated (Plant Manager and above) analytics screen presented as summary cards + simple trend lists, following existing analytics/report page patterns. Use `Segoe UI Variable` type ramp (numbers large for KPIs), Fluent spacing, and accessible color only to encode status (on-time green vs overdue red). Keep it readable and uncluttered: KPI tiles (volumes, avg fill time, on-time %, cancellations), then supporting tables/trends.

## Project structure — new `MTM_Waitlist.Analytics` module
>
> **Decision:** analytics is its own per-module project `MTM_Waitlist.Analytics`, following the repo's **two-tier** convention (verified against `MTM_Waitlist.Setup`, the canonical "module that owns a screen"). Do **not** reuse the stub `MTM_Waitlist.Reporting` (service-only, no Views).

**Tier 1 — class library `MTM_Waitlist.Analytics\` (non-view code), `RootNamespace=MTM_Waitlist.Module_Analytics`:**

- `MTM_Waitlist.Analytics\MTM_Waitlist.Analytics.csproj` — clone the module csproj shape (`net10.0-windows10.0.19041.0`, `UseWinUI`, `PlatformTarget=x64`); baseline packages (`Microsoft.WindowsAppSDK`, `CommunityToolkit.Mvvm`, DI/Config Abstractions) + `<ProjectReference>` to `MTM_Waitlist.Core` and `MTM_Waitlist.Shared`; add `Microsoft.Data.SqlClient` only if a live Infor Visual query is needed (Setup precedent); add `<InternalsVisibleTo Include="MTM_Waitlist" />` + `<InternalsVisibleTo Include="MTM_Waitlist.Tests" />`.
- Folders/namespaces:
  - `Contracts\Services\` → `MTM_Waitlist.Module_Analytics.Contracts.Services` (e.g. `IStockSnapshotRecorder`, `IWaitlistAnalyticsService`, `IAnalyticsMetricCalculator`)
  - `Models\` → `MTM_Waitlist.Module_Analytics.Models` (snapshot + analytics metric result types)
  - `Services\` → `MTM_Waitlist.Module_Analytics.Services` (stock snapshot service, analytics query/metric service, mock/sample sources)
  - `Services\DependencyInjection\` → `MTM_Waitlist.Module_Analytics.Services.DependencyInjection` (`ModuleDependencyInjectionExtensions.cs` → `AddAnalyticsModuleServices`)
  - `ViewModels\` → `MTM_Waitlist.Module_Analytics.ViewModels` (`AnalyticsViewModel`)

**Tier 2 — XAML page lives in the main app at `Module_Analytics\Views\AnalyticsPage.xaml(.cs)`, `x:Class="MTM_Waitlist.Module_Analytics.Views.AnalyticsPage"`** (app-root pages compile into `MTM_Waitlist` automatically; keep them out of the class library).

**Wiring (all centralized in the main app / shell — files to touch):**

- `MTM_Waitlist.csproj`: add `<ProjectReference Include="..\MTM_Waitlist.Analytics\MTM_Waitlist.Analytics.csproj" />` **and** `<Compile Remove="MTM_Waitlist.Analytics\**\*.cs" />` (the app already Compile-Removes every sibling library).
- `MTM_Waitlist.sln`: add the new project entry + its Debug/Release × x64 config block (mirror an existing module).
- `Services\DependencyInjection\ServiceRegistrationExtensions.cs`: add `pageService.Configure<AnalyticsViewModel, AnalyticsPage>()`, `services.AddTransient<AnalyticsViewModel>()`, `services.AddTransient<AnalyticsPage>()`.
- `Services\DependencyInjection\ModuleDependencyInjectionExtensions.cs` (app root `AddModuleServices`): call `services.AddAnalyticsModuleServices(configuration)` and add the `using`.
- `Module_Core\Views\ShellPage.xaml`: add a `<NavigationViewItem x:Uid="Shell_Analytics" helpers:NavigationHelper.NavigateTo="MTM_Waitlist.Module_Analytics.ViewModels.AnalyticsViewModel">` (+ `FontIcon.Glyph`). Navigation works automatically once `pageService.Configure` exists (resolution is by ViewModel full name).
- `Strings\en-us\Resources.resw`: add `Shell_Analytics.Content` (nav label; the fallback header path in `ShellViewModel.OnNavigated` auto-titles the page from the selected item's localized content).
- `ViewModels\ShellViewModel.cs`: only if a custom header is required (the fallback path usually suffices).
- **Role gate (Plant Manager and above):** add a static `AllowedAnalyticsRoles` array + `CanViewAnalytics` bool (compare `StartupState.CurrentRole`) in `AnalyticsViewModel` per the Settings/Setup `Can*` pattern; **also** add role-based visibility for the `Shell_Analytics` nav item (new capability in `ShellPage`/`ShellViewModel`/`NavigationViewService`, since shell items are currently static) so non-Plant-Managers cannot see or open the screen.

**Cross-module snapshot integration (Subphase 0.1):** to avoid an Analytics→Waitlist dependency cycle, define the snapshot **contract** in Core (`IStockSnapshotRecorder`, `MTM_Waitlist.Core\Contracts\Services\`) with its **implementation in `MTM_Waitlist.Analytics\Services`**; the Waitlist request-creation path calls the Core contract (DI resolves the Analytics impl). For mock ON, reuse the sample on-hand/location source — move/share the file-06 sample inventory-location source into a location Core/Shared can reference (or expose it via a Core contract) so Analytics can snapshot without depending on `Waitlist.View`.

## Subphase 0.1 — Stock-shortage snapshot at request creation

**PREREQUISITE: Task 0 green.**

- [ ] **Service Layer: when a part/coil-bearing request is created, quietly record a snapshot of that coil/part's on-hand quantity and location** (Infor Visual per mock toggle) with no handler/requester action required. *(Ref: Master Task 20)* **[Artifacts: `IStockSnapshotRecorder` in `MTM_Waitlist.Core/Contracts/Services`; recorder + snapshot model/service in `MTM_Waitlist.Analytics/Services` + `Models`; the Waitlist request-creation path (file 01 submit) invokes the Core contract via DI]** | **Persona: Backend Engineer**
- [ ] **Testing: add tests in `MTM_Waitlist.Tests` that request creation writes the stock snapshot (mock ON/OFF)** **[Artifacts: `MTM_Waitlist.Tests/Analytics/…` for the recorder/analytics services and Core for the contract]**, then run the **full suite**; must pass. | **Persona: QA Engineer**

## Subphase 0.2 — Plant Manager waitlist analytics screen

- [ ] **Workflow/VM: add a role-gated (Plant Manager+) analytics screen** presenting: volumes by request type & over time; fill speed / average wait; on-time vs overdue; cancellations and reasons; handler workload/performance; and stock-shortage metrics from Subphase 0.1. **[Artifacts: `AnalyticsViewModel` in `MTM_Waitlist.Analytics/ViewModels`; `AnalyticsPage.xaml(.cs)` in `Module_Analytics/Views`; metrics/query service(s) in `MTM_Waitlist.Analytics/Services` computed from audit + snapshot data]** *(Ref: Master Task 21)* | **Persona: Full Stack Engineer**
- [ ] **Auth Logic: role-gate the screen to Plant Manager and above**; register page + view model via `PageService`/DI and add to shell navigation **[Artifacts: `AllowedAnalyticsRoles`/`CanViewAnalytics` in `AnalyticsViewModel`; `pageService.Configure<AnalyticsViewModel, AnalyticsPage>()` + `AddTransient` in `ServiceRegistrationExtensions.cs`; `AddAnalyticsModuleServices` in the app-root `ModuleDependencyInjectionExtensions.cs`; `<NavigationViewItem x:Uid="Shell_Analytics">` in `ShellPage.xaml` + role visibility; `Shell_Analytics.Content` in `Strings/en-us/Resources.resw`]**. *(Ref: Master Task 21)* | **Persona: Backend Engineer**
- [ ] **Testing: add tests in `MTM_Waitlist.Tests` for metric computation from audit/snapshot data and role gating** **[Artifacts: `MTM_Waitlist.Tests/Analytics/…` for `AnalyticsViewModel` + metric service; role gating per the Settings `Can*` tests]**, then run the **full suite**; must pass. | **Persona: QA Engineer**

**GATE: stock snapshots captured; Plant Manager analytics screen shows the defined metrics from persisted data; full suite green. Move to `11-Phase3-admin.md`.**
Next task: **Subphase 0.1 stock snapshot** | **Persona: Backend Engineer**

## Mock-data flow coverage (added 2026-09-05)
>
> Task 20 records a stock snapshot at request creation from the Infor Visual path per the mock toggle; there is no sample on-hand/location source to snapshot from in mock mode.

- [ ] **Service Layer: capture the request-creation stock snapshot from sample data when mock ON** using the sample coil/part on-hand + location (reuse the file 06 sample inventory-location source), so analytics scenario A is reproducible without a live DB. **[Artifacts: sample source shared via Core (so both `Waitlist.View` and `MTM_Waitlist.Analytics` can use it); recorder mock branch in `MTM_Waitlist.Analytics/Services`]** *(Mock parity)*
- [ ] **QA Engineer: add tests** that request creation writes a stock snapshot in mock mode (on-hand/location captured); run the **full suite**. **[Artifacts: `MTM_Waitlist.Tests/Analytics/…`]** *(Mock parity)*

## Mockups (UI references)

See `../Mockups/` for the target UI for this task:

- `../Mockups/analytics-waitlist-overview.svg` — Plant Manager waitlist analytics screen
