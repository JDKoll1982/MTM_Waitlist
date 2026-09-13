# Implementation Plan: Unified Waitlist Card and Category/Item Request Picker

**Branch**: `004-unified-card-item-picker` | **Date**: 2026-09-13 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/004-unified-card-item-picker/spec.md`

## Summary

Replace the request **type/subtype** vocabulary with a **Category and an Item** on every request, and draw every
request on the waitlist with the same two-line card. Four Categories, twenty-three catalogued Items of which
nineteen are in scope; per-Item behaviour — how far the flow goes, whether an answer is required, the prompt, the
length limits, the options, and the fields its page shows — moves off the retiring subtype rows into a new Item
configuration table so it can change as data; both Settings screens re-key from subtype to Item; and the
type/subtype tables, their seeds, their read procedure and the services named for them retire.

The Category/Item vocabulary is **already half-built in code and is not a green field**: `RequestItemCatalog`
holds all twenty-three rows with Category, order, umbrella verb and value type; `RequestCategory`,
`RequestItemPickerRules`, `RequestJobPartAvailability` and `INewRequestPickerService` encode visibility;
`NewRequestCanonicalPicker` already groups the database's request-type tree into Category tiles that
`NewRequestJobTypeViewModel` draws. What is still legacy is everything around it: the wizard's flow metadata is
read from `waitlist_request_types` / `waitlist_request_subtypes`, the request row still stores
`request_type` + `subtype`, the list still chooses one of seven per-type card views by subtype, and both Settings
screens are keyed on subtype. So the work is a **re-key, not a rewrite** — move the flow metadata onto the Item in
the store, store `category` + `item` on the request, collapse the per-type cards into the existing
`WaitlistLineCardView` with no Item-based layout selector, and re-key the two Settings screens and the image
override scope.

One consequential move follows from the spec rather than from taste: the Item's allotted minutes leave the
per-Windows-user local settings store (`UrgencySettingsService`) and become seeded data, because FR-018 requires
the configured value and the observed average side by side and the observed average is a read over request history
that has to go through a stored procedure (FR-024). The 15-minute fallback survives as the app-side default for an
Item with no configured minutes, labelled as a default.

The database is reinstalled from seed by the owner; **no migration code is written** and none may be — the legacy
catalog's populated `category` / `item_id` columns are consumed at design time when the new seed is authored.

## Technical Context

**Language/Version**: C# 13 / .NET 10 (`net10.0-windows10.0.19041.0`)
**Primary Dependencies**: WinUI 3 / Windows App SDK 2.3.1, CommunityToolkit.Mvvm 8.x (source generators), WinUIEx, MySQL through the repo's `IMySqlHelperServer` stored-procedure seam
**Storage**: MySQL 5.7, database `mtm_waitlist`. Every read and write goes through a stored procedure; no inline statement text. Pictures stay in `config_images_locations` under a new scope.
**Testing**: MSTest 3.7 (`MTM_Waitlist.Tests`), plus the repo's scripted UI automation for what only the running app proves
**Target Platform**: Windows desktop, WinUI 3 (unpackaged and MSIX)
**Project Type**: desktop app as composition root with per-module class libraries
**Performance Goals**: not a driver — the list renders tens of rows. The one hard requirement is that an Item's configuration is read **once per wizard entry**, never per keystroke, and the per-Item allotted-minutes lookup is memoised per list load (as it already is per subtype).
**Constraints**: MySQL 5.7-compatible DDL and routines; no hardcoded pixel boundaries for layout-critical containers; every user-visible string localized; a `WMC9999` build error treated as a real masked XAML error, never an environment problem
**Scale/Scope**: 4 Categories · 19 in-scope Items (23 catalogued) · 1 new table · 3 request procedures re-keyed and 1 retired, 2 procedures added · 2 Settings screens re-keyed · 7 per-type card views replaced by 1

## Constitution Check

*GATE: evaluated before Phase 0 research and re-checked after Phase 1 design.*

| Principle | Assessment | Evidence |
|---|---|---|
| **I. Spec-First, Verified Delivery** (NON-NEGOTIABLE) | **PASS** | Spec first, `plan` before `tasks` before `implement`; this feature is far past the single-file exception. Every task in `tasks.md` will be ticked only with a build or test result. |
| **II. Live Data Integrity** | **PASS** | Nothing internal is mocked. The waitlist store is read and written live; the per-screen unavailable state (`Store` / `LastAttemptUtc` / `RetryCount` / `NextRetryUtc` + manual retry) is reused for every failure path, and FR-026 forbids a silent or fabricated result. No demo/mock mode is introduced. |
| **III. Stored-Procedure-First Database Discipline** | **PASS** | New reads and writes are procedures: `sp_waitlist_request_item_configs_get`, `sp_waitlist_request_item_allotted_minutes_update`, `sp_waitlist_request_item_observed_average_get`. `sp_waitlist_request_get`, `_insert` and `_list` are re-keyed to `category` + `item`; `sp_waitlist_request_subtypes_get` and `sp_waitlist_request_types_get` are removed. Every added, changed and removed artifact ships paired `create.sql` / `rollback.sql`, and `AllTables.sql`, `AllSPs.sql`, `AllSeeds.sql` and `Bootstrap/update_table_descriptions.sql` are updated in the same change (FR-023, FR-025). `InlineSqlAuditTests` must stay green. |
| **IV. MCP-First, Grounded Decisions** (NON-NEGOTIABLE) | **PASS** | The one genuinely new API surface — a mutually-exclusive single-choice flyout on the shell — was grounded in Microsoft Learn (*Menu flyout and menu bar*, `RadioMenuFlyoutItem` with `GroupName` + `IsChecked`), and the generated command naming was grounded in the CommunityToolkit.Mvvm docs (`[RelayCommand]` on `private async Task XAsync()` generates `XCommand`). Every other decision follows a pattern already read in this repo. Sources are recorded in [research.md](research.md). |
| **V. WinUI 3 Platform Conformance** | **PASS** | `Microsoft.UI.Xaml` only (no `Windows.UI.Xaml`); the CommunityToolkit.Mvvm generator pattern (`[ObservableProperty]` partial properties, `[RelayCommand]`) and `INavigationAware` are preserved; navigation stays on the existing `INavigationService`. The new Item step and sort control are localized via the resource mechanism, any new converter is registered in `App.xaml` in the same change, and no layout-critical container gains a pixel boundary — the one card keeps the existing `Auto`/`*` grid with `MinHeight` and star widths, and the 'Other' span is a grid span, not a dimension. |
| **VI. Evidence-Based Verification Gates** | **PASS** | The gates are the documented build and test commands; `WMC9999` is treated as a masked XAML error and surfaced by the documented technique; live-DB validation of the schema artifacts happens against the local `mtm_waitlist`. The visibility matrix is driven as a gate (SC-004), not sampled (FR-002 + §16.10 of the seed). |

**No violations. Complexity Tracking is therefore omitted** — every structural choice below (one new table, one
replaced card, no new project) reduces complexity rather than adding it, and each is justified against its
alternative in [research.md](research.md).

## Project Structure

### Documentation (this feature)

```text
specs/004-unified-card-item-picker/
├── spec.md
├── plan.md                       # this file
├── research.md                   # Phase 0 — decisions and the alternatives rejected
├── data-model.md                 # Phase 1 — the entities this feature reshapes
├── contracts/                    # Phase 1 — the identifier and behaviour contract
│   ├── item-configuration.md
│   ├── request-picker-flow.md
│   ├── card-and-identifier.md
│   └── verification-gates.md
├── checklists/requirements.md
└── .token-budget/scope-plan.md   # reading manifest for this phase
```

### Source Code (repository root)

```text
Database/
├── Bootstrap/update_table_descriptions.sql                     # mandatory same-change update
├── Tables/
│   ├── AllTables.sql                                           # master list, kept in sync
│   ├── 18_waitlist_requests_queue/{create,rollback}.sql        # category + item replace request_type + subtype
│   ├── 28_waitlist_request_types/                              # RETIRED
│   ├── 29_waitlist_request_subtypes/                           # RETIRED
│   └── 31_waitlist_request_item_configs/{create,rollback}.sql  # NEW — one row per Item
├── StoredProcedures/
│   ├── AllSPs.sql
│   ├── sp_waitlist_request_get/{create,rollback}.sql            # re-keyed
│   ├── sp_waitlist_request_insert/{create,rollback}.sql         # re-keyed
│   ├── sp_waitlist_request_list/{create,rollback}.sql           # re-keyed
│   ├── sp_waitlist_request_types_get/                           # RETIRED
│   ├── sp_waitlist_request_subtypes_get/                        # RETIRED
│   ├── sp_waitlist_request_item_configs_get/{create,rollback}.sql            # NEW
│   ├── sp_waitlist_request_item_allotted_minutes_update/{create,rollback}.sql # NEW
│   └── sp_waitlist_request_item_observed_average_get/{create,rollback}.sql    # NEW
└── Seeds/
    ├── AllSeeds.sql
    ├── seed_waitlist_request_item_configs/{create,rollback}.sql   # NEW — 23 rows: behaviour + allotted minutes
    ├── seed_waitlist_request_item_images/{create,rollback}.sql    # NEW — config_images_locations, item + category scope
    ├── seed_setup_active_jobs_eight_configurations/{create,rollback}.sql  # NEW — the visibility matrix input
    ├── seed_waitlist_requests_default/{create,rollback}.sql       # REWRITTEN in the new vocabulary
    └── seed_waitlist_request_catalog/                             # RETIRED once its item_id mapping is consumed

MTM_Waitlist.Settings/
├── Models/
│   ├── RequestItemCatalog.cs                     # 23 rows; display text becomes a resource key
│   ├── RequestItemDefinition.cs                  # + Line 2 template, + display-name resource key
│   ├── RequestItemConfiguration.cs               # NEW — one Item's stored behaviour
│   ├── RequestItemFieldDefinition.cs             # NEW — one declared field of an Item's page
│   ├── RequestItemObservedTime.cs                # NEW — configured minutes beside the observed average
│   ├── RequestJobPartAvailability.cs             # reused unchanged
│   ├── ImageLocationScope.cs                     # + RequestItem, + RequestCategory
│   └── RequestSubtypeInventory.cs                # RETIRED
└── Services/
    ├── RequestItemConfigurationService.cs        # NEW — reads the configuration once per wizard entry
    ├── RequestItemLine2Resolver.cs               # NEW — the card's second line from its template
    ├── RequestItemObservedTimeService.cs         # NEW — the observed average through its procedure
    ├── RequestItemPickerRules.cs                 # visibility, incl. scrap decision and out-of-scope Items
    ├── NewRequestPickerService.cs                # the Item list, filtered before it is built
    ├── ImageLocationService.cs                   # + ResolveRequestItemImagePathAsync
    ├── RequestItemLegacyMapper.cs                # design-time only; retired when the seed rewrite lands
    ├── RequestSubtypeDisplayLabelService.cs      # RETIRED
    └── RequestTypeDisplayLabelService.cs         # inspected; retired with the request-type scope
└── ViewModels/
    ├── RequestItemImagesDialogViewModel.cs       # RENAMED from RequestSubtypeImagesDialogViewModel
    ├── RequestSubtypeImagesDialogViewModel.cs    # RETIRED
    ├── UrgencyAllotmentEditorViewModel.cs        # re-keyed to Item; configured + observed columns
    └── SettingsViewModel.cs                      # panel visibility, search, role gates

MTM_Waitlist.Core/
├── Contracts/Services/
│   ├── IUrgencySettingsService.cs                # re-keyed to item, store-backed
│   ├── IUrgencyDeadlineService.cs                # re-keyed to item
│   └── IWaitlistSortPreferenceService.cs         # NEW — per-viewer, remembered
└── Services/
    ├── UrgencySettingsService.cs                 # reads/writes the Item's allotted minutes
    ├── UrgencyDeadlineService.cs
    ├── UrgencyCalculator.cs                      # five order keys; most-urgent stays the default
    └── WaitlistSortPreferenceService.cs          # NEW — via ILocalSettingsService

MTM_Waitlist.Waitlist.NewRequest/
├── Models/
│   ├── NewRequestFlowState.cs                    # Category + Item replace RequestType + Subtype
│   ├── NewRequestItemOption.cs                   # NEW — one Item tile, from configuration
│   ├── NewRequestTypeDefinition.cs               # RETIRED
│   ├── NewRequestSubtypeDefinition.cs            # RETIRED
│   └── NewRequestCanonical*.cs                   # RETIRED with the tree grouping
├── Services/
│   ├── NewRequestFlowRules.cs                    # Work Centre → Category → Item → Details → Preview → Summary → Result
│   ├── NewRequestFlowService.cs                  # no request-type catalog read
│   ├── NewRequestCanonicalPicker.cs              # RETIRED
│   └── RequestTypeCatalogService.cs              # RETIRED
└── ViewModels/
    ├── NewRequestJobTypeViewModel.cs             # becomes the Category step
    ├── NewRequestItemViewModel.cs                # NEW — the Item step
    ├── NewRequestSubtypeViewModel.cs             # RETIRED
    ├── NewRequestDetailsViewModel.cs             # per-Item fields and the one captured answer
    └── NewRequest{Preview,Summary,Result}ViewModel.cs

Module_Waitlist/
├── Controls/
│   ├── WaitlistLineCardView.xaml                 # the one card, no Item-based layout variant
│   ├── Coil/ Scrap/ PickupFg/ PickupNcm/ PickupOs/ PickupWip/ …  # RETIRED — 7 per-type card views, models and VMs
│   └── WaitlistLineViewSelector.cs               # RETIRED with the per-type views
└── Views/
    ├── NewRequestJobTypePage.xaml                # the Category step
    ├── NewRequestItemPage.xaml                   # NEW — the Item step
    ├── NewRequestSubtypePage.xaml                # RETIRED
    ├── NewRequestDetailsPage.xaml                # renders the Item's declared fields
    └── WaitlistViewPage.xaml

MTM_Waitlist.Waitlist.View/
├── Models/
│   ├── WaitlistRequest.cs                        # Category + Item replace RequestType + Subtype
│   ├── WaitlistRequestDraft.cs                   # same
│   ├── WaitlistRequestTitles.cs                  # ResolveLine1 / ResolveLine2, no legacy fallback
│   └── SampleOrder.cs                            # the card's row model
└── ViewModels/
    ├── WaitlistViewViewModel.cs                  # build one card per row; sort from the preference
    └── WaitlistViewDetailViewModel.cs            # the Item's fields, read from configuration

Module_Core/Views/ShellPage.xaml                  # + the sort flyout beside My Requests and the building selector
ViewModels/ShellViewModel.cs                      # the sort control's state and the preference write
Services/DependencyInjection/ServiceRegistrationExtensions.cs   # composition root: availability snapshot, new services
Strings/en-us/Resources.resw                      # new keys; retired type/subtype keys removed

MTM_Waitlist.Tests/
├── Services/{UrgencySettingsServiceTests,WaitlistSortPreferenceServiceTests,UrgencyCalculatorTests}.cs
├── Module_Settings/{RequestItemPickerRulesTests,NewRequestPickerServiceTests,UrgencyAllotmentEditorViewModelTests,RequestItemImagesDialogViewModelTests,RequestItemConfigurationServiceTests}.cs
├── Module_Waitlist/{NewRequestItemPickerTests,ViewModels/NewRequestItemViewModelTests,ViewModels/WaitlistViewViewModelActionTests}.cs
├── Module_Mock/{InlineSqlAuditTests,RetiredSymbolAuditTests}.cs   # both extended
└── Module_Setup/Services/ActiveJobSeedRoundTripTests.cs           # NEW — seed JSON through the writer's deserializer
```

**Structure Decision**: no new project and no project-reference change. The Item catalog, its rules and its new
configuration service stay in `MTM_Waitlist.Settings`, where `RequestItemCatalog` and
`INewRequestPickerService` already live and which both `MTM_Waitlist.Waitlist.NewRequest` and
`MTM_Waitlist.Waitlist.View` already reference. The wizard keeps its view models in
`MTM_Waitlist.Waitlist.NewRequest` and its XAML in `Module_Waitlist/Views`; the card and list stay in
`MTM_Waitlist.Waitlist.View` + `Module_Waitlist/Controls`; the shared ordering and per-viewer preference services
stay in `MTM_Waitlist.Core`. The availability snapshot is filled **at the composition root**
(`Services/DependencyInjection/ServiceRegistrationExtensions.cs`), the one place permitted to see both
`IActiveJobItemResolverService` (Setup) and `RequestJobPartAvailability` (Settings) — which is why nothing has to
move between projects.

## Post-Design Constitution Re-Check

Re-evaluated after Phase 1. Unchanged: still **no violations**, no Complexity Tracking, and one point sharpened by
the design —

- **Principle III** is the principle this design leans on hardest, and Phase 1 confirmed the procedure count rather
  than assuming it: the request's two stored columns are re-keyed through the three existing request procedures, the
  Item's configuration arrives through one new read, the allotted minutes through one new update, and the observed
  average through one new read. Nothing is read inline, and the two spool-of-artifact master lists plus
  `update_table_descriptions.sql` move with every one of them.
- **Principle V** gained one concrete obligation from Phase 1: the sort preference is per viewer, so the shell's
  new control must survive a restart through `ILocalSettingsService`, and the Item and Category display text moves
  to resource keys — which means the `.resw` change ships in the same change as the catalog, not after it.
