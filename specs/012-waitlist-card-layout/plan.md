# Implementation Plan: Waitlist Line Card Layout

**Feature**: `specs/012-waitlist-card-layout/spec.md` | **Size**: oversized | **Workflow**: companion
**Design source**: `docs/research/research-waitlist-card-layout-pre-spec.md` (decisions D1–D43; the spec is behavioural by design, so the technical substance of the per-Item cells lives in that record)

## Scale note

This change spans roughly 28 files across five areas: the waitlist card control and its host page, the Settings
resolution layer (job snapshot, picker rules, disposition stack), Infor Visual and WIP-application reads, the
database (one new column, three stored procedures, the Item-configuration seed and its aggregate), and the New
Request wizard. Two things deserve a reader's attention. First, the seed and the spec disagree by design: §5 of the
pre-spec governs the card's cells and the seed is brought into line only where the card must *show* something the
seed does not declare or declares under another label. Second, this adds the first new column to
`waitlist_requests_queue` since the queue was re-keyed, so the read, the write and the seed all move together.

## Summary

The waitlist list row becomes four zones fed by the request's own Item: identity (picture, badge, requesting work
centre), status (status word, time ring, person), a detail grid whose cells and geometry follow the Item, and an
action grid of up to six square buttons that re-centres as buttons become unavailable. The status vocabulary grows
from four lifecycle words to eight and the column gains three themes; the card body stops opening the request and a
dedicated Open details button takes over. The work behind it is mostly resolution, not layout: the job snapshot and
its label resolver are extended so the Item's declared fields resolve, anything the job cannot carry is read from
Infor Visual or the WIP application, the already-built but unconsumed disposition stack is wired to the card for
Part Type, the four hidden pickups are unhidden behind real job gates, and five new answers join the New Request
workflow. No new dependency and no new stack: the ring is the platform `ProgressRing` in its determinate state, and
the per-Item detail sections are templates in one resource dictionary rather than a file per card.

## Project Structure

```text
Module_Waitlist/
  Controls/WaitlistLineCardView.xaml(+.cs)              four zones, column rules, ring, action grid, Open details
  Controls/WaitlistLineCardDetailTemplates.xaml         D31: one detail template per Item, all in this file
  Controls/WaitlistLineCardDetailTemplateSelector.cs    picks the template for the row's Item
  Controls/WaitlistStatusFlyout.xaml(+.cs)              the four handler conditions, gated on the row's handler
  Views/WaitlistViewPage.xaml(+.cs)                     hosts one card per row; ItemClick stops opening the request
  Views/NewRequestHopperPage.xaml(+.cs)                 Size, three radios, reference image (new)
  Views/NewRequestDiePage.xaml(+.cs)                    gains the die-shop checkbox answer
  Views/NewRequestPartSearchPage.xaml(+.cs)             fuzzy incorrect-part entry (new)
  Views/NewRequestCoilTurnPage.xaml(+.cs)               required Weight of Coil (new)
  Views/NewRequestSkidCountPage.xaml(+.cs)              # of Skids, defaulting to 1 (new)

MTM_Waitlist.Waitlist.View/
  Models/SampleOrder.cs                                 ring inputs, status rows, handler status, resolved cells
  Models/WaitlistLineCardCell.cs                        one resolved label-and-value cell (new)
  ViewModels/WaitlistViewViewModel.cs                   per-row cell resolution, flyout command, one-minute tick
  Helpers/RequestStatusPalette.cs                       eight roles, three column themes
  Helpers/RemainingTimePalette.cs                       retained; the ring reads the same fraction
  Services/WaitlistRequestService.cs(+I-)               handler-status write, list read carrying status + answers

MTM_Waitlist.Settings/
  Services/RequestJobFieldValues.cs                     the label map; the file most of §5 depends on
  Models/RequestJobPartAvailability.cs                  gains coil description, home location
  Models/RequestDunnagePart.cs                          gains Home Location
  Services/RequestItemPickerRules.cs                    the out-of-scope array is deleted; four real job gates
  Services/RequestDispositionResolver.cs(+Mapper)       consumed by the card path, not only by its own tests
  Models/RequestDisposition.cs                          gains Ncm
  Services/RequestDispositionClassifier.cs              NCM arm from the location signal

Database/
  Tables/18_waitlist_requests_queue/create.sql(+rollback)   handler_status, answers_json
  StoredProcedures/sp_waitlist_request_handler_status_update/   new; reports affected rows
  StoredProcedures/sp_waitlist_request_list/                    returns handler_status and answers_json
  StoredProcedures/sp_waitlist_request_insert/                   carries answers_json
  InforVisual/Queues/Module_Waitlist/Queries/GetDispositionInput.sql        FG-location input fix (FR-090)
  InforVisual/Queues/Module_Waitlist/Queries/GetPartWarehouseQuantities.sql new: full part-wide quantity
  InforVisual/Queues/Module_Mock/Populations/part_warehouse_quantities_population.sql   mirror for that shape
  InforVisual/Queues/Module_Waitlist/Queries/GetPartMatches.sql             fuzzy part search, live only
  MTMWipApp/Queues/Module_Waitlist/Queues/GetWipFloorQuantities.sql         NCM + item-type buckets
  Seeds/seed_waitlist_request_item_configs/create.sql(+rollback)            label corrections, new answer cells
  Seeds/AllSeeds.sql, Bootstrap/update_table_descriptions.sql               kept in step in the same change

Strings/en-us/Resources.resw                            every new label, phrase and status word
MTM_Waitlist.Mock/Services/VisualReadShapeCatalog.cs     the new quantity shape's mirror declaration
MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistLineCardMarkupTests.cs   rewritten to the new anatomy
```

**Structure Decision**: the card keeps its present home (`Module_Waitlist/Controls`), its host stays
`Module_Waitlist/Views/WaitlistViewPage`, and every non-XAML piece stays in the library the same logic already
lives in (`MTM_Waitlist.Waitlist.View` for the row and its view model, `MTM_Waitlist.Settings` for resolution and
rules). The one new arrangement is the detail column: because a WinUI `UserControl` needs its own XAML file, D31's
"one control per card, all in one file" is realised as one `DataTemplate` per Item in a single
`ResourceDictionary`, selected by a `DataTemplateSelector`. That keeps the 24 arrangements in one file without
adding 24 files, and it is the documented WinUI mechanism for choosing a template from an item's own properties.

## Requirement Coverage

Every functional requirement from `spec.md`, traced to the work that carries it. Ranges are inclusive; together
they cover 001–105 with no gap and no overlap.

| Requirements | Carried by |
|---|---|
| FR-001–FR-010 | Card anatomy and the four zones: `WaitlistLineCardView.xaml`, the host page's template, the work-centre value, the spanning title row, removal of the status pill and of the body click (W1) |
| FR-011–FR-023 | The status zone: `SampleOrder`'s ring inputs and person rows, the ring in `WaitlistLineCardView.xaml`, three themes in `RequestStatusPalette`, the one-minute tick in `WaitlistViewViewModel` (W2) |
| FR-024–FR-029 | The detail grid: the 3×2 inner grid and its row merges by cell count, the label-above-value anatomy, `Die Location` gated to the die Items (`WaitlistLineCardDetailTemplates.xaml`) (W3) |
| FR-030–FR-053 | The 24 per-Item cell assignments: one detail template per Item, `Die Location` plus the conditional die-shop cell, the collapsing panels, `other`'s scrolling description (W3) |
| FR-054–FR-060 | Cell values and visibility: Weight of Coil distinct from the job average, the die-shop phrases, the declared-but-not-shown fields, `Destination` on `deliver-flatstock` only, hide-and-omit, hide-and-log (W4) |
| FR-061–FR-069 | The action zone: the 3×2 grid, removal and re-centring, tooltips and accessible names, the 44×44 floor, and no print action on the card (D59), Open details replacing the body click (W1) |
| FR-070–FR-076 | The handler condition and its flyout: the gate on the row's own handler, the four conditions, the theme, persistence and restore, keyboard operability, the announced gate (W2) |
| FR-077–FR-087 | The five new answers: hopper Size with its image, the die-shop checkbox, the fuzzy incorrect-part search, the required Weight of Coil, the skid count defaulting to one (W6) |
| FR-088–FR-095 | Where values come from: the resolver's label map, the reads behind quantity and item type, the §5.3 derivation, the four returning Items unhidden by real gates, the dunnage `Description` question (W4, W5) |
| FR-096–FR-102 | The reversed contracts: the superseded freeze and metadata rows recorded in `contracts/card-and-identifier.md`, the config-declares-it rule, the hide-and-log rule, the opened status vocabulary (W1, W2, W4, W10) |
| FR-103–FR-105 | The invariants: one shape per Item (a geometry test over all 24), localized labels only, animations-off respected (W3, W8) |

## Constitution Check

`.specify/memory/constitution.md` is present but is the **unratified stock template**: every principle name and
description is still a `[PLACEHOLDER]`, with no version, ratification date or governance text. There is therefore no
ratified principle to gate against, and the gate is taken against the repository's ratified engineering rules
instead: the SQL-artifact and stored-procedure rules in
`.github/instructions/database-schema-rules.instructions.md` (enforced by `InlineSqlAuditTests`), the naming rules in
`.github/instructions/csharp-xaml-naming-rules.instructions.md`, the WinUI rules in
`.github/instructions/winui3-api-rules.instructions.md`, and the no-demo-mode rule enforced by
`RetiredSymbolAuditTests`.

| Rule | Assessment |
|---|---|
| Every data operation goes through a stored procedure; no inline or hard-coded statement text in application code | PASS. The handler-status write is a new stored procedure; the list read and the request insert are extended in place; the new Infor Visual and WIP reads are queue SQL files. No application-side statement text is added. |
| DML goes through the seam that reports affected rows | PASS. `sp_waitlist_request_handler_status_update` returns its affected-row count, and the caller is told when the gated update matched nothing, exactly as `sp_waitlist_request_status_update` already does. |
| Every schema artifact ships `create.sql` + `rollback.sql` in the same change | PASS. The table, the new procedure and its rollback, the seed and its rollback, and the aggregate/description files all move together. |
| No manual demo or mock mode, and no feature toggle that substitutes demo data | PASS. Nothing here adds a mode or a toggle; the four returning Items are gated on the requesting job's real work, not on a switch. |
| Internal stores are read and written live; only Infor Visual reads use the cached fallback | PASS. The handler status, the answers and the dunnage home location are live internal reads and writes. The new quantity read and the corrected disposition input are Infor Visual reads and get a mirror declaration per shape; the fuzzy part search is deliberately live-only and gets none. |
| WinUI 3 / Windows App SDK on .NET 10, MVVM Toolkit source generators, DI and page registration in `App.xaml.cs`, XAML views in `Module_*/Views` with view models in libraries | PASS. New wizard pages go under `Module_Waitlist/Views`, their view models into the libraries, and new registrations into the existing extension methods. No `Windows.UI.Xaml` namespace and no parallel navigation stack is introduced. |
| Naming rules and WinUI API rules | PASS. Names follow the locked conventions (`Id`/`Utc`/`Ui` casing, `XxxPage`/`XxxView` suffixes, `Feature_Element.Property` resource keys, lowercase snake_case SQL). The one WinUI API claim the design leans on, the determinate `ProgressRing`, was grounded against Microsoft Learn before being written down. |
| Localization and accessibility invariants | PASS. Every new string is a resource key, and the ring and the flyout carry accessible names and keyboard operation. |

No violation is required, so there is no Complexity Tracking table.

**Re-checked after Phase 1 design.** The final design adds one nullable column and two stored procedures, one
mirrorable read shape, one live-only read shape and one corrected query, and it deletes a hard-coded hide array.
Every row above stays at PASS: the new writes go through the affected-row-reporting seam, each schema artifact ships
its rollback in the same change, the internal reads and writes are live, the fuzzy search is deliberately stricter
than the fallback policy rather than looser, and no mode, toggle or demo substitution is introduced.

## Assumptions carried into implementation

Three preferences are recorded in this plan rather than presented as owner-confirmed, and two open questions are
carried forward with a named shape instead of a silent rule.

| Item | How the plan proceeds |
|---|---|
| The die-shop cell is hidden when the checkbox is unticked | Implemented as written in the spec (FR-055, US6 scenario 6). Recorded here because the pre-spec review settled it without an explicit owner confirmation. |
| A handler status takes over row 1 and its theme, with overdue applying only when no handler status is set | Implemented per D43 and FR-012/FR-023. Same status: encoded in the spec, not confirmed by the owner in so many words. |
| `Part Type` and `# of Skids` are the two cells of all four returning pickups | Implemented per FR-036 to FR-039 and D37, including the explicit absence of any location cell. |
| FR-090, the finished-goods quantity defect | The **input** is corrected, not the rule: `GetDispositionInput.sql` stops passing `PART.QTY_ON_HAND` and passes a sum filtered to finished-goods locations, matching the rule the classifier already documents. The classifier's priority is left untouched. Whether the rule's wording also needs amending is `clarify`'s call; this plan does not amend it. |
| FR-095, the unfillable dunnage `Description` | The declaration is left in place and the cell is never drawn, which is the spec's own edge case ("the configuration is left alone") and the inverse of inventing a value. Removing the declaration is rejected as a silent scope cut that `clarify` should own. |

## Stage sequence

| Stage | Work | Depends on |
|---|---|---|
| W1 | Card anatomy: four zones, column rules, work-centre value, title row, action grid, Open details, body click removed | — |
| W4 | Resolution: label map, snapshot fields, quantity reads, node/shape for each §5.2 cell, hide-and-log rule | — |
| W2 | Status zone: ring, themes, person rows, handler status column plus its procedure and read, the flyout | W1, W4 |
| W3 | Detail templates: one per Item, grid geometry by cell count, label-above-value | W1, W4 |
| W5 | Item type: wire the disposition stack, correct the disposition input, add the NCM state, unhide the four pickups behind real gates | W4 |
| W6 | New Request answers: hopper size and image, die-shop checkbox, fuzzy search, weight of coil, skid count, plus their persistence | W4 |
| W7 | Seed and schema reconciliation, aggregate and description upkeep | W3, W5, W6 |
| W8 | Resources and accessibility: every new label, the ring's accessible name, animations-off | W1–W6 |
| W9 | Tests: card markup rewritten, picker rules, disposition, seed, ring arithmetic, flyout gating | W1–W8 |
| W10 | Contract recording: the superseded card contract in `specs/004-unified-card-item-picker/contracts/` and this feature's own contract | W1–W3 |
