# Implementation Plan: Truthful Data and Truthful Controls

**Branch**: `002-truthful-data-and-controls` | **Date**: 2026-09-12 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-truthful-data-and-controls/spec.md`
**Research**: [research.md](./research.md) | **Model**: [data-model.md](./data-model.md) | **Contracts**: [contracts/](./contracts/) | **Validation**: [quickstart.md](./quickstart.md)

## Summary

Stop the application showing anything it cannot substantiate and offering anything it cannot honour, on five surfaces: the waitlist card, the request detail page, the New Request confirm step, Settings, and the notification-activation path. The work is subtraction plus proof: remove the fabricated literals and their fallback defaults listed in `data-model.md` §2 from the two waitlist view models; give each detail section a three-state block (rows present / hidden on a successful empty read / failed with an inline error and retry); remove the inert Cancel and Accept buttons; delete the confirm step's queue-and-wait card and the constant-keyed coil-weight lookup; gate the New Request Alerts toggle on a capability the settings view model exposes; give the three colliding Settings labels their own keys and drop the placeholder privacy entry with its resource values; register the three missing search-aware panels behind a declared list enforced by a coverage test; delete both placeholder activation dialogs and route recognised activations through the existing deep-link parser; and extend the retired-symbol audit to the retired wording and placeholder-resource patterns. Wording and documents describing removed behaviour are corrected here too. No database artifact is added.

## Technical Context

**Language/Version**: C# 13 / .NET 10 (`net10.0-windows10.0.19041.0`), x64 only  
**Primary Dependencies**: WinUI 3 on Windows App SDK 2.3.x (self-contained), CommunityToolkit.Mvvm (source generators), Microsoft.Extensions.DependencyInjection, MSTest 3.7.0 with Microsoft.NET.Test.Sdk 17.13.0  
**Storage**: MySQL (`mtm_waitlist`, `mtm_wip_application_winforms`, `mtm_receiving_application`, and the `mtm_mock` cache) plus read-only Infor Visual. This feature adds no table, seed or stored procedure and changes no stored-procedure call.  
**Testing**: MSTest 3.7 in `MTM_Waitlist.Tests` (the app grants it `InternalsVisibleTo`), folders mirroring the production modules; the retired-wording and placeholder scans extend `MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs`  
**Target Platform**: Windows 10 19041+ / Windows 11, x64, unpackaged and self-contained (`WindowsPackageType=None`, `WindowsAppSDKSelfContained=true`)
**Project Type**: desktop application — the WinUI 3 app is the composition root (Views, XAML, DI, navigation) with testable non-view code in per-module class libraries  
**Performance Goals**: none added — the change removes work (fewer elements, fewer lookups). The only new runtime cost is one capability check when the Settings page loads, evaluated once in the view model rather than per binding.  
**Constraints**: no database artifact (constitution III applies only if one is introduced); all new text resource-backed and localized; every `x:Load` element carries `x:Name` and its one-way binding re-evaluates only on `PropertyChanged`; nothing inside a list template is deferred (research R2); the notification capability is reported per installation, never as a platform rule (research R3); build and test run single-threaded (`/m:1 /nodeReuse:false`) because the solution build otherwise races on the XAML compiler  
**Scale/Scope**: 5 surfaces — 6 card templates plus `WaitlistLineCardView`; 6 section loaders in `WaitlistViewDetailViewModel`; `NewRequestSummaryPage` with its view model; `SettingsPage` with `SettingsViewModel`; and the two notification entry points. Roughly 30 fabricated literals removed, 7 controls disposed of or gated, 12 new automated checks covering the ten defect classes, and 16 documents corrected.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Gate | Status |
| --- | --- | --- |
| I. Spec-First, Verified Delivery | Spec written and clarified before code; work driven by `tasks.md`; nothing ticked until verified | **PASS** — spec, four recorded clarifications, this plan |
| II. Live Data Integrity | No demo/sample/mock mode; internal stores always live; only Infor Visual reads may fall back to the cache | **PASS** — the change removes values rather than substituting them; FR-030 forbids a mode, and `RetiredSymbolAuditTests` fails the build if one returns |
| III. Stored-Procedure-First | Every data operation through a stored procedure; paired `create.sql`/`rollback.sql` for any schema artifact | **PASS** — no database artifact is added (research R8); nothing here introduces inline SQL |
| IV. MCP-First, Grounded Decisions | Decisions grounded in Serena/Context7/Microsoft Learn rather than recall | **PASS** — research R2/R3 cite the Microsoft Learn `x:Load` and app-notification pages; the remaining decisions cite the defect documents and the repo instruction files |
| V. WinUI 3 Platform Conformance | `Microsoft.UI.Xaml` only; MVVM Toolkit generators; localized resource-backed text; converters registered in `App.xaml`; fluid layout | **PASS** — `x:Load`/`x:Bind` with the globally registered `BoolToVisibilityConverter`; new keys in `.resw`; no layout redesign (FR-033) |
| VI. Evidence-Based Verification Gates | Clean solution build and green suite; `WMC9999` treated as a real error | **PASS** — `contracts/verification-gates.md` G1–G6, including ten per-defect checks that must fail before the change |
| Security & Secrets | No hardcoded credentials; no credential displayed or logged | **PASS** — no credential, endpoint or auth path is touched |
| External Integration & Cache Boundaries | Infor Visual never written; cached data is fallback only and swapped as a complete snapshot | **PASS** — FR-006 and FR-031 keep the fallback, its indicator, its age and its retry exactly as delivered |
| Documentation & Extensibility | Affected documentation updated in the same change | **PASS** — FR-026/FR-027/FR-028 name the sixteen documents (data-model §6) |

The post-Phase-1 re-check finds the same result: the design adds no principle-affecting mechanism, and the
two deviations of scope are recorded under Complexity Tracking rather than here.

## Project Structure

### Documentation (this feature)

```text
specs/002-truthful-data-and-controls/
├── spec.md                     # Feature specification (clarified 2026-09-12)
├── plan.md                     # This file (/speckit.plan command output)
├── research.md                 # Phase 0 output — 9 resolved decisions
├── data-model.md               # Phase 1 output — block states, removal and control inventories, records
├── quickstart.md               # Phase 1 output — walkthrough and gates
├── contracts/                  # Phase 1 output — surface, activation, verification contracts
├── checklists/requirements.md  # Spec quality checklist (/speckit.checklist)
└── tasks.md                    # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
MTM_Waitlist/                                   # WinUI 3 composition root (Views, XAML, DI, navigation)
├── Module_Waitlist/
│   ├── Views/          WaitlistViewPage.xaml · WaitlistViewDetailPage.xaml · NewRequestSummaryPage.xaml
│   └── Controls/       WaitlistLineCardView.xaml(.cs) + the six per-type *WaitlistLineView.xaml
├── Module_Settings/Views/  SettingsPage.xaml(.cs)
├── MTM_Waitlist.Waitlist.View/
│   ├── ViewModels/     WaitlistViewViewModel.cs · WaitlistViewDetailViewModel.cs
│   ├── Models/         SampleOrder.cs
│   └── Converters/     RemainingTimeToBrushConverter.cs · StringToImageSourceConverter.cs
├── MTM_Waitlist.Waitlist.NewRequest/ViewModels/  NewRequestSummaryViewModel.cs
├── MTM_Waitlist.Settings/
│   ├── ViewModels/     SettingsViewModel.cs
│   ├── Converters/     ResolvedImagePathToSourceConverter.cs (also declares BoolToVisibilityConverter)
│   └── Services/       LocalSettingsService.cs
├── MTM_Waitlist.Core/
│   ├── Services/       AppNotificationService.cs · WaitlistRequestLink.cs
│   ├── Activation/     AppNotificationActivationHandler.cs
│   ├── Helpers/        RuntimeHelper.cs · ResourceExtensions.cs
│   └── Models/         InternalStoreUnavailableState.cs
├── Strings/en-us/      Resources.resw · TooltipResources.resw
└── MTM_Waitlist.Tests/
    ├── Module_Waitlist/ · Module_Settings/         # MSTest suites beside the existing ones
    └── Module_Mock/RetiredSymbolAuditTests.cs      # extended by this feature
```

**Structure Decision**: No new project, folder, module or route — every change lands in a file that already exists: the two waitlist view models, the six card templates and `WaitlistLineCardView`, the New Request confirm page and its view model, the Settings page and its view model, the two notification entry points and their shared deep-link parser, `Strings/en-us/Resources.resw`, the existing MSTest suites, and the sixteen documents in `data-model.md` §6 (one feature inventory, ten defect files, two changelogs, three backlog and module files). The existing split (XAML views in the app, testable non-view code in `MTM_Waitlist.*` libraries) is preserved, which is what keeps every check in `contracts/verification-gates.md` runnable from the test project.
## Complexity Tracking

**No constitution violation requires a justification row.** Two scope decisions belong here because they shape the task list rather than the design:

| Scope decision | Why | What was rejected, and why |
| --- | --- | --- |
| The wider audit of **every** screen was dropped | It made a ten-defect closure spec too congested and would have reached screens owned by other specifications | Keeping it (spec `Assumptions` → Scope of enforcement) — rejected by the owner 2026-09-12; the rule still applies to the surfaces this feature names |
| This feature **removes** rather than implements | Real item values, a real queue figure, per-coil weight resolution and notification delivery belong to templates 03, 04 and 08 | Implementing them here — rejected: it would put unreviewed stop-gaps behind production figures and duplicate work those specs already specify |

<!-- token-budget: compacted (level=medium) on 2026-09-12T23:51:47Z; original at plan.full.md -->
