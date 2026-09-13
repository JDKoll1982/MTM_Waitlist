# Implementation Plan: Shared Fuzzy Match Picker and Work Order Search

**Branch**: `005-setup-work-order-search` | **Date**: 2026-09-13 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/005-setup-work-order-search/spec.md`

## Summary

Add one **shared, caller-described match picker** and point it at the work-order entry step, so a person who types
part of a work order gets a modal listing the work orders that begin with it. The picker is a closed
**catalog of registered sources**: a source is declared once as data (its key, the read that serves it, the
column matched, the columns shown, the default and maximum number of matches), the read is resolved through a
registry of reader kinds, and the picker itself knows nothing about work orders. What the picker *does* hold — and
what every screen therefore stops writing for itself — is the one implementation of search orchestration: resolve
the named source, ask the reader for rows, map them to matches, trim, count, and classify the outcome.

The work-order source is the first registration and the only one shipped. It rides the **existing Infor Visual
boundary unchanged**: a new read shape `work_order_search` (shape 6) whose per-pattern live query is attempted
first and whose results are served from the `mtm_mock` mirror only on genuine unreachability, with an identical
projection — so a reachable-but-empty search is a real answer and is never replaced by cached rows. The mirror's
match-count and cap are computed inside the two stored procedures, not in C#, and the pattern and cap travel as
**parameters**, so nothing is assembled into statement text.

On the entry page the work-order text rules stay where they already live — the classification that says "this is a
partial" and the derivation of the search pattern go into `WorkOrderValidationService`, beside the normalisation
rule they must not disturb. The step keeps only the handoff: on leaving the field, if the entry cannot be resolved
as a complete work order, ask the picker and show the modal.

## Technical Context

**Language/Version**: C# 13 / .NET 10 (`net10.0-windows10.0.19041.0`)
**Primary Dependencies**: WinUI 3 / Windows App SDK 2.3.1, CommunityToolkit.Mvvm 8.x (source generators), MySQL
through the repo's `IMySqlHelperServer` stored-procedure seam, SQL Server (read-only) for Infor Visual
**Storage**: MySQL 5.7 — the `mtm_mock` mirror gains one table and its stage twin; the internal stores are not
touched. Every read goes through a stored procedure; no inline statement text, and no statement assembled at run
time from a supplied name.
**Testing**: MSTest 3.7 (`MTM_Waitlist.Tests`), with the modal behind an injectable seam so the picker's behaviour
is proven headless; the live/mirror projection parity is proven by the shape's roundtrip validation against MySQL
**Target Platform**: Windows desktop, WinUI 3 (unpackaged and MSIX)
**Project Type**: desktop app as composition root with per-module class libraries; the capability is a **library
concern** (no new project)
**Performance Goals**: one search per field exit, never per keystroke, and the read is capped **in the store** — the
page never materialises an unbounded list. A partial that matches thousands of orders costs one capped read.
**Constraints**: MySQL 5.7 has no window functions, so the mirror's match count is a scalar subquery rather than
`COUNT(*) OVER ()`; `LIKE` wildcard semantics are shared by both engines; `WMC9999` is a real masked XAML error;
`[RelayCommand]` strips a trailing `Async` (`XAsync` binds as `XCommand`)
**Scale/Scope**: 1 shared capability · 1 registered source · 1 new read shape (mirror + stage twin + pair of
procedures) · 3 live scripts (search, population, and the mirror's own artifacts) · 2 changed services · 1 new
dialog · 0 new projects · 0 internal-store schema changes

## Constitution Check

*GATE: evaluated before Phase 0 research and re-checked after Phase 1 design.*

| Principle | Assessment | Evidence |
|---|---|---|
| **I. Spec-First, Verified Delivery** (NON-NEGOTIABLE) | **PASS** | `spec.md` and its checklist are in place before this plan; `plan` precedes `tasks` precedes `implement`. Far past the single-file exception. Every task will be ticked only with a build or test result. |
| **II. Live Data Integrity** | **PASS** | Nothing internal is mocked and no internal store is touched. The **only** source shipped is an Infor Visual read, which is exactly the one external source the constitution permits to fall back — through the existing automatic cached fallback, with no manual or demo mode. A reachable-but-empty search is returned as a real answer (FR-019). |
| **III. Stored-Procedure-First Database Discipline** | **PASS** | Every data operation is a procedure: the live read is a checked-in parameterized script, and the mirror is served by `sp_visual_work_order_search_get` and refreshed by `sp_visual_work_order_search_refresh`. The pattern and cap are **parameters**; no name supplied by a caller reaches a statement, and no statement is assembled at run time (FR-018). Each added artifact ships paired `create.sql` / `rollback.sql`, `Database/Mock/Bootstrap/update_table_descriptions.sql` is updated in the same change, and the `mtm_mock` master lists are **regenerated** by `Database/CopilotScripts/build_mtm_mock_masters.ps1` — the approved generated-masters deviation, whose `-Check` mode is the staleness gate (FR-020, SC-005). `InlineSqlAuditTests` must stay green. |
| **IV. MCP-First, Grounded Decisions** (NON-NEGOTIABLE) | **PASS** | The genuinely new surfaces were grounded, not recalled: the archived `ContentDialog` pattern and its state/`AutomationId` conventions against Microsoft Learn, and the CommunityToolkit.Mvvm `[RelayCommand]` naming rule against the toolkit docs. Every other decision follows a pattern already read in this repo — the shape catalog, the read fallback, the dialog seam and the DI extension. Sources are recorded in [research.md](research.md). |
| **V. WinUI 3 Platform Conformance** | **PASS** | `Microsoft.UI.Xaml` only; the modal is a `ContentDialog` with an `XamlRoot` from the live window, raised through the same injectable seam the dunnage dialog uses. CommunityToolkit.Mvvm generators and `INavigationAware` are preserved and navigation stays on `INavigationService`. Every new string comes from the resource mechanism (FR-021), any new converter is registered in `App.xaml` in the same change, and the list is fluid: a scrollable `ListView` with `Auto`/`*` sizing and no layout-critical pixel boundary. `WMC9999` is treated as a masked XAML error. |
| **VI. Evidence-Based Verification Gates** | **PASS** | The documented build and test commands are the gates; the shape's roundtrip validation and projection parity are proven against a live MySQL, and the "unreachable with mirror → matches; reachable-but-empty → none" pair is an explicit scenario (SC-007) rather than an assumption. Both of the repository's audit tests (`InlineSqlAuditTests`, `RetiredSymbolAuditTests`) stay green. |

**No gate failure.** The design adds no project, no internal table, and no assembly-time statement text, so
Complexity Tracking is **omitted**. The two choices worth recording as decisions rather than violations — a new
read shape rather than a second read procedure on the existing one, and the reader-kind registry — are justified
against their alternatives in [research.md](research.md).

## Project Structure

### Documentation (this feature)

```text
specs/005-setup-work-order-search/
├── spec.md
├── plan.md                       # this file
├── research.md                   # Phase 0 — decisions and the alternatives rejected
├── data-model.md                 # Phase 1 — the entities and the read contracts
├── contracts/                    # Phase 1 — the identifier and behaviour contract
│   ├── search-capability.md
│   ├── work-order-source.md
│   └── verification-gates.md
├── checklists/requirements.md
└── .token-budget/scope-plan.md   # reading manifest for this phase
```

### Source Code (repository root)

```text
MTM_Waitlist.Core/                        # the capability's contracts and its one implementation
├── Contracts/Services/
│   ├── ISearchableSourceCatalog.cs        # NEW — the closed source registry; unknown key ⇒ unavailable (FR-015)
│   ├── ISearchSourceReader.cs             # NEW — the fetch seam behind one reader kind
│   └── ISearchPickerService.cs            # NEW — the capability's public contract (FR-013, FR-016)
├── Models/                                # NEW — SearchSource, SearchReadKind, SearchRequest,
│                                          #       SearchMatch, SearchResult, SearchOutcome
└── Services/
    ├── SearchableSourceCatalog.cs         # NEW — resolution over an explicit list of declared sources
    └── SearchPickerService.cs             # NEW — the ONE search implementation (FR-012, SC-003)

MTM_Waitlist.Mock/                        # the shipped reader kind, beside the catalog it resolves from
├── Models/                                # NEW — VisualWorkOrderSearchRequest, VisualWorkOrderSearchRow
├── Services/
│   ├── VisualWorkOrderSearchFallback.cs   # NEW — shape 6; live-then-mirror, identical projection (FR-019)
│   ├── VisualShapeSearchSourceReader.cs   # NEW — resolves a source's ReadTarget through the shape catalog
│   └── VisualReadShapeCatalog.cs          # CHANGED — one additive shape entry (FR-014)
└── DependencyInjection/                   # CHANGED — registers the fallback and the reader kind

MTM_Waitlist.Setup/                       # the first caller
├── Contracts/Services/
│   ├── IWorkOrderValidationService.cs     # CHANGED — classify the entry; derive the search pattern
│   ├── IWorkOrderSearchService.cs         # NEW — the caller-side seam the view model talks to
│   └── ISearchPickerDialogService.cs      # NEW — the modal seam, so the gates run headless
├── Services/
│   ├── WorkOrderValidationService.cs      # CHANGED — TryBuildSearchPattern; TryNormalize UNTOUCHED (FR-004/FR-006)
│   ├── WorkOrderSearchService.cs          # NEW — declares the source and calls the picker
│   └── DependencyInjection/               # CHANGED — the one additive source registration (FR-014)
└── ViewModels/SetupWorkOrderViewModel.cs  # CHANGED — raise the modal on blur; no matching logic (FR-012)

Module_Shared/Views/                       # the shared modal, composition root
└── SearchPickerDialog.xaml(.cs)           # NEW — matches list, count line, the empty and the capped states

Module_Setup/Views/SetupWorkOrderPage.xaml(.cs)   # CHANGED — the existing LostFocus handoff
Services/
├── SearchPickerDialogService.cs           # NEW — app-side ISearchPickerDialogService
└── DependencyInjection/ServiceRegistrationExtensions.cs  # CHANGED — catalog, reader kind, dialog seam

Strings/en-us/Module_Setup/                # CHANGED — new picker strings through the resource mechanism (FR-021)

Database/
├── InforVisual/Queues/
│   ├── Module_Setup/Queries/SearchWorkOrders.sql        # NEW — the live per-pattern read
│   └── Module_Mock/Populations/work_order_search_population.sql  # NEW — the set-based snapshot read
└── Mock/
    ├── Tables/visual_work_order_search_result/{create,rollback}.sql        # NEW — the mirror
    ├── Tables/visual_work_order_search_result_stage/{create,rollback}.sql  # NEW — the stage twin
    ├── StoredProcedures/sp_visual_work_order_search_get/{create,rollback}.sql      # NEW
    ├── StoredProcedures/sp_visual_work_order_search_refresh/{create,rollback}.sql  # NEW
    ├── Seeds/seed_visual_mirror_baseline/{create,rollback}.sql   # CHANGED — baseline rows for the new shape
    ├── Validation/all_shapes_roundtrip/validate.sql              # CHANGED — shape 6 joins the roundtrip
    ├── Bootstrap/update_table_descriptions.sql                   # CHANGED — mandatory same-change update
    └── {AllTables,AllSPs,AllSeeds}.sql                           # REGENERATED by the masters script

MTM_Waitlist.Tests/                        # the gates
├── Module_Setup/Services/WorkOrderValidationServiceSearchPatternTests.cs  # NEW — 0451 ⇒ WO-0451% (not WO-000451%)
├── Module_Setup/Services/SearchPickerServiceTests.cs                     # NEW — outcome classification
├── Module_Setup/ViewModels/SetupWorkOrderViewModelPickerTests.cs          # NEW — modal on blur, never on complete
└── Module_Mock/VisualWorkOrderSearchFallbackTests.cs                     # NEW — live / mirror / empty parity
```

**Structure Decision**: no new project. The capability's **contracts and its single implementation** live in
`MTM_Waitlist.Core`, the existing home for cross-module service contracts, so the picker depends on nothing but
abstractions. The **shipped reader kind** lives in `MTM_Waitlist.Mock`, beside the shape catalog it resolves
through — the same place the shape-definition precedent already lives, and the reason the picker never references
the fallback itself. The **caller's source declaration, its dialogue seam and its view-model changes** stay in
`MTM_Waitlist.Setup`, and only the **modal view** lives in the composition root under `Module_Shared/Views`, where
an app-owned `ContentDialog` needs the live `XamlRoot`.

## Post-Design Constitution Re-Check

*Re-evaluated against the completed Phase 1 design: [data-model.md](data-model.md),
[contracts/search-capability.md](contracts/search-capability.md),
[contracts/work-order-source.md](contracts/work-order-source.md),
[contracts/verification-gates.md](contracts/verification-gates.md).*

| Principle | Assessment | What the design added, and why it still passes |
|---|---|---|
| **I. Spec-First, Verified Delivery** | **PASS** | The design changed nothing about the sequence. Every gate in `contracts/verification-gates.md` §3 is a *driven* scenario, so the tasks that follow can be ticked only against a result. |
| **II. Live Data Integrity** | **PASS** | The design's only stored artefact is a **cache of an external system** in its own dedicated store, and the whole projection section exists to keep it a fallback rather than a system of record. No internal store is read, written, or mocked, and no toggle is introduced. |
| **III. Stored-Procedure-First** | **PASS** | Every data operation in the design is a procedure or a checked-in parameterized script. The extension playbook in `contracts/search-capability.md` §4 is explicit that a source may not be registered by naming a table, a column, or a statement — only by declaring a `ReadTarget` in reviewed code. The pair-`create.sql`/`rollback.sql` rule and the generated-masters `-Check` gate are both named as gates. |
| **IV. MCP-First, Grounded Decisions** | **PASS** | Three design choices were grounded rather than recalled, and the sources are named in [research.md](research.md): the WinUI 3 `ContentDialog` `XamlRoot`-before-`ShowAsync` rule and the one-dialog-per-thread limit (Microsoft Learn, *Dialog controls*); the CommunityToolkit.Mvvm trailing-`Async` stripping (repo instruction file, corroborated); and MySQL's stored-program `LIMIT` rule plus the 5.7 absence of window functions. Nothing in the design rests on model memory alone. |
| **V. WinUI 3 Platform Conformance** | **PASS** | The modal is `Microsoft.UI.Xaml.Controls.ContentDialog`, raised through an injectable seam with the `XamlRoot` taken from the live window (and a `null` guard for a headless host, which is also what makes the gates runnable). Every new string is a named resource key with a fallback (FR-021). No layout-critical pixel boundary is introduced — the list is a scrollable `ListView` in a star-sized region — and no new converter is added, so nothing new needs registering in `App.xaml`. |
| **VI. Evidence-Based Verification Gates** | **PASS** | The gates are named with their commands, and the five that a visual review would miss (G-10 through G-14) are called out as gates rather than samples. `WMC9999` handling and the `[RelayCommand]` naming trap are both written into the contract so the build gate is not misread. |

**No violation, and therefore no Complexity Tracking.** Two design choices read as added structure and are
justified in [research.md](research.md) against their simpler alternatives rather than as exceptions: the
reader-kind registry (§R2 — the alternative makes FR-014 invasive), and the new read shape (§R5 — the alternative
breaks the derived-artifact-name invariant the refresh engine and startup validation depend on). Both add a type
or an artifact, not a rule, and neither weakens a principle.

