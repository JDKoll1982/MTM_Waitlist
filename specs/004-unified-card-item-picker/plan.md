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

**Follow-up batch.** The feature shipped at `status: completed`; the section **Follow-Up Batch — Sign Out, the
Request Population and the Fulfilment Pass** at the end of this document records the second batch of decisions taken
on top of that build — sign out, the request population, the fixture work centres' removal and the fulfilment pass —
carried by `spec.md`'s appended **FR-033 … FR-045** and **SC-013 … SC-020**. Writing that section changed no code.

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

## Follow-Up Batch — Sign Out, the Request Population and the Fulfilment Pass

**Status.** The feature above shipped at `status: completed`. This section records a **second batch of decisions**
against the same specification: one new capability, one preparation pass and one verification pass. The
specification carries them as **FR-033 … FR-045** and **SC-013 … SC-020**, appended under a new **US6**. **No code,
no database artifact and no test was changed when this section was written** — it is documentation. `.spec-context.json`
refuses edits on a completed spec, and reopening it is the owner's action, so it was left alone.

### F1 — Sign out, a new capability

**Where.** The shell's user badge already exists: `Module_Core/Views/ShellPage.xaml` declares
`ShellPage_CurrentUserButton` (`CurrentUserButton`) in the header grid's column 4, bound to `CurrentUserDisplayName`,
`CurrentUserIconGlyph` and `CurrentUserBadgeBrush` on `ViewModels/ShellViewModel.cs`. It has **no `Flyout` today**,
which is why there is no sign-out anywhere in the application. The batch adds one `MenuFlyout` carrying a single
**Sign out** entry, the command and its state on `ShellViewModel`, its registration in the composition root, and the
localized label in `Strings/en-us/Resources.resw`.

**What it does.** Sign out must *end the session*, not hide it: the application relaunches and comes back at the
sign-in screen, on a computer where "remember me" is set and the session would otherwise have been restored
(FR-034). The sign-in path reads five local keys — `Login.RememberPassword`, `Login.RememberedUsername`,
`Login.RememberedPassword`, `Startup.Session.Token`, `Startup.Session.ExpiresUtc`, all declared in
`MTM_Waitlist.Startup/ViewModels/LoginViewModel.cs`. Clearing the remembered-credential keys is what makes the
restarted instance ask for credentials rather than restore the session.

**Mechanism, grounded.** `MTM_Waitlist.Startup/Services/StartupRecoveryService.cs` already relaunches the
application for the corrupt-settings recovery path: it starts `Environment.ProcessPath` (handling the `dotnet`-hosted
case), then calls `IAppLifecycleService.Exit()`. Sign out is those same two steps with the keys cleared first, so the
batch adds `ISignOutService` (`MTM_Waitlist.Core/Contracts/Services/`, beside `IStartupRecoveryService`) and its
implementation in `MTM_Waitlist.Startup/Services/`. Microsoft Learn documents the platform-supported alternative —
`AppInstance.Restart(String)` in `Microsoft.Windows.AppLifecycle` terminates and restarts a packaged **or unpackaged**
desktop app on command and returns an `AppRestartFailureReason` (*Windows App SDK 1.1 release notes*; *Restart API*).
Either mechanism satisfies FR-034; the returned failure reason is what FR-026's plain-language report carries when the
restart cannot be performed.

**Why not simply navigate to the sign-in window.** `IAppLifecycleService.ShowLoginWindowAndCloseSplash()` exists and
would be less code, but it leaves the process — and therefore the session state the person asked to end — alive in
memory. FR-034 rules it out in as many words.

### F2 — The nineteen requests, the ten people and the eight roles

**Facts.** Nineteen in-scope Items, one request each, raised by the **ten accounts that already exist** — no account
may be created (FR-036). `johnk` and `jkoll` are Developer; the other eight are the `test.*` accounts, one per role,
sharing the test password `0000`. The distribution is weighted towards ordinary floor people with a couple of leads,
and **every one of the eight roles raises at least one** request, so each kind of login opens onto a populated list
(FR-037). The requester's own name is the card's "Requested by" row (FR-008), which is the point of spreading them.

**Raised, not seeded.** The requests are raised through the application by the account that will own them, because
the lifecycle times must be the application's own: FR-044 forbids a time the application did not produce, and a
seeded row is exactly that. This also keeps the existing demo seed (`seed_waitlist_requests_default`) out of the
picture — it is not this batch's population, and no task in the batch rewrites it.

**Work centres.** Spread across more work centres than the seven that carry the prepared situations (FR-038), and
every Item whose availability depends on material on the job is raised from a work centre whose job actually carries
that material. The availability rule still decides what is offered; it is not bypassed to make a request raisable.

**Handler.** `johnk` (Developer) is the handler for the pass, so every handler-centric control is visible to them.
FR-041's self-accept case follows from the same fact: the handler is also one of the ten requesters.

### F3 — The fixture work centres retire, the prepared situations move to real ones

`Database/Seeds/seed_setup_work_centers_fixture_stations/{create,rollback}.sql` declares seven fixture work centres,
`900-1` … `900-7`, and `Database/Seeds/seed_setup_active_jobs_eight_configurations/{create,rollback}.sql` assigns one
prepared job situation to each. The batch **removes the seven fixtures** and re-points the seven situations at
**real** work centres: five on Expo Drive and two on Vits Drive (FR-039). The eighth case — a work centre with no
active job — stays what it is today: the absence of a job, not a row. The situation shapes do not change (coil only;
flatstock only; die only; component only; dunnage only; everything at once; no subordinate parts), and
`subordinate_parts_json` keeps the exact shape `sp_setup_save_setup` writes, which is what makes
`MTM_Waitlist.Tests/Module_Setup/Services/ActiveJobSeedRoundTripTests.cs` a proof about the real read path rather
than about a hand-written seed.

**A consequence worth carrying into the pass.** The fixture seed declared its stations on purpose: a live Setup save
**overwrites `setup_active_jobs` for the work centre it writes**. The owner has accepted real work centres anyway, so
the seven situations are no longer immune to a Setup walk: the fulfilment pass runs against them **before** any Setup
walk on the same work centre, and the round-trip test is re-pointed rather than deleted so the matrix is still
asserted.

**Amendment (2026-09-14) — the re-point was not survivable, so the test now owns its data.** The predicted
overwrite happened: work centre `100-03` carried a live Setup save for `WO-060954` / `22-77723-125-Raw`, and
`Seeded100_3_CoilOnly_NormalisesToCoil` failed with zero coils — a red gate caused by the shop floor doing its job.
`ActiveJobSeedRoundTripTests` therefore writes its own seven rows under a per-run `IT-ROUNDTRIP-*` work centre and
deletes them in teardown. The matrix, the shapes and the deserializer it is driven through are unchanged, so it
remains a proof about the real read path; what it no longer does is depend on a row the live database is free to
replace. The deployed seed keeps its real work centres for the fulfilment pass. The one live-row claim that remains
is the retirement assertion, whose subject is the absence of rows at the seven `900-*` names.

**Files.** `seed_setup_work_centers_fixture_stations/{create,rollback}.sql` retire; `AllSeeds.sql`'s entry for them is
removed and `Bootstrap/update_table_descriptions.sql` follows if it names them (FR-025, constitution III);
`ActiveJobSeedRoundTripTests.cs` moves to the new work centres.

*Which* of the Expo Drive and Vits Drive work centres carries *which* situation is **not** fixed by the decisions —
only the split, five and two. The implementation picks the five and the two and records them in the seed's own header
comment, as that seed does today.

### F4 — The `pickup-component` defect that blocks the sweep

`Database/Seeds/seed_waitlist_request_item_configs/create.sql` gives `pickup-component` `requires_answer = 1`,
`answer_value_type = 'enum'` and **`options_json = NULL`**, with a `detail_fields_json` element declaring a required
`enum` field sourced from the captured answer. The row's own comment records the intent — "the enumerated values
arrive with the job snapshot rather than from configuration" — but the details step has no list to draw, so the step
cannot be completed and the Item cannot be raised. That is one of the nineteen (FR-035), so it blocks the whole sweep.

**Repair direction.** The choices must reach the details step. The primary direction is the one the seed records: the
job's component list (`subordinate_parts_json → category='Component'`) is the source, which makes `pickup-component`
the same *class* of Item as `pickup-die` — an answer chosen from a list — with the list derived from the job rather
than fixed in `options_json`. **FR-013 is absolute here**: the repair may not branch on the Item's identity; it must
be a property of the configuration row (`answer_value_type = 'enum'` plus the field's declared `source`), so a second
such Item added later behaves the same way. The alternative — seeding an `options_json` list — is recorded and
rejected unless the component list genuinely cannot be reached from the details step, because it would freeze a
job-derived list into configuration.

**Files.** `MTM_Waitlist.Settings/Services/RequestItemConfigurationService.cs` (the options payload),
`MTM_Waitlist.Waitlist.NewRequest/ViewModels/NewRequestDetailsViewModel.cs` (the option pick), and
`Database/Seeds/seed_waitlist_request_item_configs/{create,rollback}.sql` plus `AllSeeds.sql` if the configuration row
itself changes.

### F5 — The fulfilment pass, outcome by outcome

All nineteen requests are raised and left unhelped first (FR-040), so the queue is populated before any of it is
handled. Then every one of the seven outcomes is exercised and recorded (SC-017).

| # | Outcome | How it is produced | What must be true |
|---|---|---|---|
| 1 | accept, then finish | `AcceptRequestCommand` → `CompleteRequestCommand` as `johnk` | status `Accepted` then `Completed`; `accepted_utc` and `completed_utc` set once each; the row leaves the open list |
| 2 | accept, hand back, accept again, finish | `AcceptRequestCommand` → `ReleaseRequestCommand` → `AcceptRequestCommand` → `CompleteRequestCommand` | the release returns the row to `Pending` with `released_utc` set and the handler cleared; the second accept completes normally; still **one** request (FR-042) |
| 3 | the raiser cancels before anyone accepts | signed in as the request's own raiser, `CancelRequestCommand` → `CancelOwnRequestAsync` | only the raiser may do it, only while `Pending`; the row is `Canceled` with the reason and `canceled_by` set |
| 4 | overdue | give one Item the **smallest allowance the minutes screen accepts** through `sp_waitlist_request_item_allotted_minutes_update`, raise that Item's request, and let its deadline genuinely expire | the row reads overdue because its own deadline passed (FR-044); **no back-dated `requested_utc`, no hand-edited `target_time_utc`** |
| 5 | one handler hands back, a different handler takes it over | `johnk` releases; a second handler account accepts | the second handler becomes `assigned_material_handler`; the first is gone |
| 6 | a handler accepts a request they raised themselves | `johnk` raises a request, then `johnk` accepts it | **permitted** (FR-041). `RequestActionPolicy.CanViewerAccept(status, viewerCanHandleRequests)` carries no requester check, so this should already hold — the task is to *prove* it, and to widen the gate only if the proof fails |
| 7 | two handlers at the same moment | **two copies of the application side by side**, both on the same request | exactly one outcome (FR-043, SC-019): one assignment, one `accepted_utc`, and the losing handler told their accept did not take (FR-026) |

**Outcome 7 is not merely a UI test.** `Database/StoredProcedures/sp_waitlist_request_status_update/create.sql`
updates by `public_id` alone — `WHERE public_id = p_public_id` — so a second accept **overwrites** the first
handler's assignment rather than failing. The transition therefore has to become conditional on the state the handler
saw: the guard belongs in the procedure, with the affected-row count reported through the non-query seam so the losing
handler is told (constitution III), and that count is what the unit test asserts. Two copies of the same build are
used because the race is between processes, not between two clicks — the application is multi-instanced by default and
nothing in the repository enforces single-instance for it.

**Everything stays.** The nineteen requests and the ten accounts remain in place after the pass so the owner can
browse them (FR-045). Nothing prunes them and no seed is authored from them.

*Open, and reported rather than guessed:* whether the existing demo rows in `seed_waitlist_requests_default`
(requester `6229 "John Koll"` / `5000 "Other User"`) are cleared before the nineteen are raised, or left in place
beside them. The decisions do not say, and the pass's evidence reads differently either way.

### F6 — How sign-out and the sweep are verified

Sign out is verified as behaviour, not as a label: the badge offers it, choosing it restarts the application, and the
restarted instance asks for credentials **on a computer where "remember me" was set** (SC-013). The display name
alone proves nothing, which is exactly the failure FR-034 names — so the evidence is the restart and the credential
prompt, recorded against the `UI` gate. The sweep's seven outcomes are recorded as rows against that same gate, and
the population is proven by reading the nineteen rows back with their `category`, `item`, `status` and requester.
`contracts/verification-gates.md` is left as written: its gate list already covers these shapes, and this batch adds
no gate.

## Second Follow-Up Batch — Attribution and Give Back

Phase 10 of `tasks.md`, against the appended **FR-046**, **FR-047** and **SC-021**. Both defects were surfaced by the
Phase 9 walk and both blocked its checkpoint; neither is a new story, because one is US1's raiser and the other is
US2's card.

### G1 — The request carries the person who raised it (FR-046)

**What was wrong.** `NewRequestWorkCenterViewModel.SelectWorkCenter` called
`NewRequestFlowRules.VerifyEmployeeIdentity("6229")` — a literal — and the rule answered with the literal name
`"John Koll"`. The signed-in person never reached the request, so every row in the store says `6229 John Koll`
whoever raised it, and FR-037's spread across the ten accounts cannot be produced at all.

**The design decision: the lookup is the authority and the identifier is the key.** The rule no longer decides
whether a person exists — it is handed the record the directory returned for the signed-in number and refuses a
number with no active record, so nothing was loosened. `IEmployeeDirectoryService` reads `core_users_profiles`
through a new `sp_core_employee_by_identifier_get` (paired rollback, block appended to `AllSPs.sql`, constitution
III), matched on **`employee_identifier`** rather than on a name, because a name is not what an account is found by
and `johnk` and `jkoll` deliberately share `6229`. `SelectWorkCenter` became an awaited command that resolves the
signed-in `StartupState.EmployeeNumber` and attributes the request from the answer.

**Rejected:** comparing a name against a stored display name — it would have "worked" until two people shared a
name. Also rejected: letting the flow continue when the lookup returns nothing, which is the loosening FR-046
forbids.

**Consequences.** An unreadable store is reported as the outage it is rather than as a refusal the person did not
earn (FR-026). An account with no stored display name falls back to its identifier, because a request must carry a
name at all. The eight test accounts take `1111` as their first-use password change (`0000` is the temporary-password
marker), so the population can be raised by each of them in turn.

### G2 — A claimed request can be given back (FR-047)

**What was wrong.** `ReleaseRequestCommand` existed, was tested and documented — and nothing bound it, because the
card's action area was capped at two buttons. A handler who claimed a request and then could not work it had no way
to return it to the queue, so it sat with them.

**The design decision: bind the existing command, and supersede the earlier decision in writing.** `SampleOrder`
carries `ReleaseCommand` and `ReleaseActionText`, set from the same `CanCompleteOrRelease` gate as Complete — Give
back is the assignee's other option on their own claim, never anybody else's — and the card draws a third 44×44
button on `SystemFillColorSolidNeutralBrush` with the Undo glyph `E7A7`. Both the glyph (Segoe Fluent Icons list) and
the brush (the SDK's own `generic.xaml`) were read from their sources rather than recalled, and `E7A7` sits outside
the deprecated `E0`–`E5` range. The **solid** neutral variant is used rather than `SystemFillColorNeutralBrush`,
which resolves to a translucent `#72000000`/`#8BFFFFFF` overlay on which a white glyph would be nearly invisible.
`OfferedActionCount` now counts the buttons the row offers rather than the flags it holds, so the count and the
markup cannot disagree.

**Supersession.** `specs/003-waitlist-handler-fulfilment/contracts/action-contracts.md` C1 ("Release has no
surface"), C7 ("Release — not drawn", the two-button ceiling) and the `ReleaseRequestAsync` remark are amended in
place, with the earlier reasoning kept beside the new behaviour. C1's own instruction — that a future change binds
this command rather than adding a second one — was followed exactly.

**Rejected:** giving Give back a gate of its own. A second flag would have widened what the card offers without
widening what the service allows, duplicating a gate that already means precisely this.

### G3 — How the two are verified

The rule is proved against the directory's record: a non-`6229` employee resolves to their own name, and an unknown
number, a known-but-inactive account and a row whose stored identifier disagrees are each refused. The step is
proved to attribute the signed-in person — and to block, in plain language and without navigating, a signed-in
number the directory does not account for. The row is proved to carry the view model's **own generated**
`ReleaseRequestCommand` (the name is read from the view model, never assumed, because `[RelayCommand]` strips the
trailing `Async`), to reach the release path exactly once as the signed-in handler, and to be offered to nobody the
gate excludes. The card is proved by markup to draw the control from the row, gate, command, accessible name and
tooltip alike. Build and test are T149 and T150; T151 applies and reverses the new procedure pair against a local
`mtm_waitlist`, reads the ten identifiers back through it, and confirms an unknown number returns no row.
