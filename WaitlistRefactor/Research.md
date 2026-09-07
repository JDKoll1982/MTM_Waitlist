# Waitlist Recode — Research (Dependency & Blast-Radius Chart)

> Companion research doc for `WaitlistRefactor/MasterTaskList.md`. Generated with the CodeGraphy CLI
> per the official query surfaces (`map`, `query`, `dependencies`, `dependents`, `path`) rather than raw
> bulk `edges` dumps — bulk dumps are a broad inventory view only; the per-node commands give bounded,
> interpretable incoming/outgoing relationship evidence for blast-radius analysis.

## 1. Domain scope (~102 unique files, app-only index)
- **Module_Waitlist/** — composition root (XAML Views + Controls, app project, DI-registered)
  - Views: WaitlistViewPage, WaitlistViewDetailPage, NewRequestPreviewPage, NewRequestJobTypePage,
    NewRequestDetailsPage, NewRequestSubtypePage, NewRequestResultPage, NewRequestSummaryPage,
    NewRequestWorkCenterPage
  - Controls: WaitlistLineCardView + per-request-type image/line views (Coil, DieHandling, Flatstock,
    ForkliftAssist, Other, Pickup, PickupFg, PickupNcm, PickupOs, PickupWip, Scrap, TableHandling)
- **MTM_Waitlist.Waitlist.Controls/** — per-request-type Model+ViewModel pairs (Coil, DieHandling,
  Flatstock, ForkliftAssist, Other, Pickup, Scrap, TableHandling) + DI extension
- **MTM_Waitlist.Waitlist.NewRequest/** — New Request wizard: models, services (CoilAvailabilityService,
  NewRequestFlowRules/Service, RequestTypeCatalogService, SampleJobCoilCatalog), per-step ViewModels
- **MTM_Waitlist.Waitlist.View/** — list/detail viewing: models (WaitlistRequest, WaitlistRequestDraft,
  WaitlistRequestTitles, SampleOrder, etc.), services (WaitlistInventoryService, WaitlistRequestService,
  AverageCoilWeightService, sample catalogs), ViewModels (List/Detail), converters/selectors
  (StringToImageSourceConverter, WaitlistLineTemplateSelector)

## 2. Internal edges — the flow to preserve
- Module_Waitlist Views -> matching ViewModels in Waitlist.NewRequest / Waitlist.View.
- NewRequest ViewModels -> NewRequestFlowRules + NewRequestFlowState/Models (shared flow state machine).
- NewRequest Result/Summary ViewModels -> Waitlist.View's `IWaitlistRequestService` /
  `WaitlistRequestSubmitResult` (hand-off point from wizard into submit).
- View project: ViewModels -> Services -> Models (standard MVVM layering).
- `WaitlistLineCardView.xaml.cs` -> `Waitlist.View/Models/SampleOrder.cs`.

## 3. External dependencies — Waitlist -> rest of app
- **MTM_Waitlist.Core** (heaviest): `ILocalSettingsService`, `INavigationService`, `INavigationAware`,
  `IMySqlHelperServer`, `IBuildingSelectionService`, `IIgnoredLocationsService`, `IUrgencyDeadlineService`,
  `RuntimeHelper`, `StartupDebugLog`, `StartupState`, `MySqlHelperServer`/`SqlHelperServer`.
- **MTM_Waitlist.Settings**: `RequestSubtypeInventory`, `RequestTypeInventory`, `IImageLocationService`.
- **MTM_Waitlist.Shared**: `WorkCenterDetail`, `IWorkCenterCatalogService`.
- NuGet/.NET: CommunityToolkit.Mvvm, CommunityToolkit.WinUI.UI.Animations, Microsoft.Extensions.*,
  Microsoft.UI.* (WinUI), System.* base libraries.

## 4. Incoming dependents (non-test) — the integration seam to rewire
- `Module_Core/Views/ShellPage.xaml.cs` — shell hosts/frames Waitlist pages.
- `Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs` — DI registration.
- `Services/DependencyInjection/ServiceRegistrationExtensions.cs` — DI registration.
- `Services/NavigationViewService.cs` — nav menu entries pointing at Waitlist pages.
- `ViewModels/ShellViewModel.cs` — shell-level VM references Waitlist navigation.

## 5. Test-coupling / blast-radius (verified per-file via `codegraphy dependents <file>`)
`MTM_Waitlist.Tests/**` was originally missing from `.codegraphy/settings.json`'s `include` list —
fixed, then re-indexed (433 -> 529 files, 2793 edges) before running these queries.

| Target file | Test dependents found | Churn risk |
| --- | --- | --- |
| `WaitlistRequestService.cs` | Many methods in `WaitlistRequestServiceTests.cs` (audit trail, cancel-own, duplicate-warning, submit lifecycle) | HIGH |
| `WaitlistViewViewModel.cs` | 3 methods (stale-refresh guard, refresh-on-submit, ShowMyRequestsOnly) + used as ctor/property type elsewhere | MEDIUM |
| `WaitlistViewDetailViewModel.cs` | 6+ methods across 2 test files (inventory load, detail values, empty-state, back command) | MEDIUM |
| `CoilAvailabilityService.cs` | 3 methods (mock on/off, coil-bearing vs no-coil job) | MEDIUM |
| `AverageCoilWeightService.cs` | 5 methods (mock on/off, empty part, seeded weight, unknown part) | MEDIUM |
| `WaitlistLineTemplateSelector.cs` | **Zero** | LOW (no safety net today, also low-churn to replace) |
| `Module_Waitlist/Views/WaitlistViewPage.xaml.cs` | Zero via CodeGraphy (navigated by string page-key, not a `using`/type edge — analyzer limitation, not evidence of dead code) | N/A |

## 6. Methodology notes / limitations
- Only the `using` edge type reliably captures file-to-file/package relationships for C# in this
  CodeGraphy version; `call`/`reference`/`type` edges exist but are sparser for some seams (e.g. page
  navigation by string key is invisible to the graph).
- `.codegraphy/settings.json` `nodeVisibility` hides symbol-level nodes in the visual Graph View by
  design (declutter) — this does not affect CLI query commands.
- Bounded/paginated output is not evidence that omitted results don't exist; re-run targeted
  `dependents`/`dependencies`/`path` queries per file during implementation rather than relying solely on
  this snapshot.
- Full CodeGraphy re-index command used: `codegraphy index` (after editing `include` in
  `.codegraphy/settings.json` to add `MTM_Waitlist.Tests/**`).
