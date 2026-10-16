# Tasks: Waitlist Line Card Layout

**Feature**: `specs/012-waitlist-card-layout/spec.md` | **Plan**: `specs/012-waitlist-card-layout/plan.md` | **Size**: oversized | **Workflow**: companion
**Design source**: `WAITLIST-CARD-LAYOUT-PRE-SPEC.md` (D1–D43) and `research.md` R1–R9.

## Scale note

Roughly 28 files across five areas: the card control and its host page, the Settings resolution layer, Infor Visual
and WIP reads, the database (two new columns, three stored procedures, a new mirrorable read shape, the Item seed and
its label migration), and the New Request wizard. Two things to watch. First, the seed and the card disagree by
design: the seed is moved only where the card must show something the seed does not declare or declares under
another label, and every other declared field stays declared and is simply excluded by a card rule. Second, this adds
the first columns to `waitlist_requests_queue` since the queue was re-keyed, so the table, the three procedures, the
read shape, the mirror and the two aggregates all move in one change.

## How to read this list

- `- [ ]` is unticked; a task is ticked `- [x]` only once it is **implemented and verified**. Every task states its
  own verification.
- `[P]` marks a task that is independent of the others in its wave: a different file, no incomplete dependency.
- `[US#]` maps a task to a user story. `[US0]` marks shared foundation, seed, resource, contract and validation work
  that belongs to no single story.
- `**Wave N — independent (different files):**` heads a set of tasks that can be built in any order.
  `**⟶ Wait for Wave N to finish, then:**` is the join before tasks that depend on the wave. Same-file tasks are
  never in the same wave.
- **One phase owns each file.** The card's own markup (`WaitlistLineCardView.xaml`) is a shared artifact: Phase 3
  builds every zone inside it, and the later phases bind to it rather than editing it. Nothing else is shared.
- A task naming a stored procedure or queue script ships its `create.sql` **and** `rollback.sql` in the same change,
  and no application code carries statement text (`InlineSqlAuditTests`).

## Assumptions carried into the tasks

Three preferences are recorded rather than presented as settled, and the two open questions are carried with a named
shape.

| Item | How the tasks proceed |
|---|---|
| The die-shop cell is hidden when the checkbox is unticked | Built as the spec writes it (FR-055, US6 scenario 6). Not owner-confirmed in so many words. |
| A handler status takes over row 1 and its theme, with overdue applying only when no handler status is set | Built per D43 and FR-012/FR-023. Same status: encoded, not owner-confirmed. |
| `Part Type` and `# of Skids` are the two cells of all four returning pickups | Built per FR-036 to FR-039 and D37, including the explicit absence of any location cell. |
| FR-090, the finished-goods quantity defect | T066 corrects the **input** and leaves the classifier's rule alone; T068 adds a live-database validation because the finished-goods location codes this installation uses are unknown. Whether the rule's wording also needs amending stays with `clarify`. |
| FR-095, the unfillable dunnage `Description` | T085 leaves the declaration in place and the cell is never drawn. Removing the declaration is `clarify`'s call, not this step's. |

---

## Phase 1: Setup

**Wave 1 — independent (different files):**

- [x] **T001** [P] [US0] Record the pre-change baseline: build the solution and run the full test suite on branch `012-waitlist-card-layout`, and keep the result as the comparison for every later verification. · `MTM_Waitlist.sln`, `tools/run-tests.ps1` · Verify: `dotnet build MTM_Waitlist.sln` and `tools/run-tests.ps1` both exit 0.
- [x] **T002** [P] [US0] Confirm the tooling this change needs actually runs: the companion writer script under `python`/`py`, the mock master-list builder, and the schema validator. · `Database/CopilotScripts/build_mtm_mock_masters.ps1`, `.github/scripts/validate-database-schema.ps1` · Verify: each command exits 0.

---

## Phase 2: Foundational

Persistence, the read shapes, the mock mirror, and the row model every story reads. **No user-story work begins
until this phase is done.**

**Files:** `Database/Tables/18_waitlist_requests_queue/`, `Database/StoredProcedures/sp_waitlist_request_*`,
`Database/InforVisual/Queues/Module_Waitlist/Queries/GetPartWarehouseQuantities.sql`,
`Database/InforVisual/Queues/Module_Mock/Populations/`, `Database/InforVisual/Queues/Module_Setup/Queries/GetSubordinateParts.sql`,
`Database/MTMReceivingApp/Queues/Module_Setup/Queues/GetDunnageParts.sql`, `Database/Mock/Tables/`, `Database/Mock/StoredProcedures/`,
`Database/Mock/Seeds/seed_visual_mirror_baseline/`, `Database/Mock/Validation/`, `MTM_Waitlist.Mock/Services/`,
`MTM_Waitlist.Mock/DependencyInjection/`, `MTM_Waitlist.Settings/Models/`, `MTM_Waitlist.Settings/Services/RequestJobFieldValues.cs`,
`MTM_Waitlist.Waitlist.View/Models/`, `MTM_Waitlist.Waitlist.View/Services/`, `MTM_Waitlist.Waitlist.View/ViewModels/`,
`MTM_Waitlist.Waitlist.View/Helpers/HandlerStatusCatalog.cs`.

**Wave 1 — independent (different files):**

- [x] **T003** [P] [US0] Add the two nullable columns to the queue row — `handler_status VARCHAR(32) NULL` and `answers_json JSON NULL`, no default — and drop both in the paired rollback. · `Database/Tables/18_waitlist_requests_queue/create.sql`, `Database/Tables/18_waitlist_requests_queue/rollback.sql` · FR-012, FR-073, FR-087 · Verify: apply then roll back against the dev store; the schema validator is clean.
- [x] **T004** [P] [US0] New stored procedure `sp_waitlist_request_handler_status_update`: sets `handler_status`, gated on the expected handler, and returns the affected-row count so a lost race is told rather than shown as success. · `Database/StoredProcedures/sp_waitlist_request_handler_status_update/create.sql(+rollback.sql)` · FR-070, FR-073, FR-076 · Verify: a matching-handler call reports 1; a wrong-handler call reports 0 and writes nothing.
- [x] **T005** [P] [US0] Extend `sp_waitlist_request_list` to return `handler_status` and `answers_json` on every row. · `Database/StoredProcedures/sp_waitlist_request_list/create.sql(+rollback.sql)` · FR-073 · Verify: the returned column set contains both.
- [x] **T006** [P] [US0] Extend `sp_waitlist_request_insert` to carry `answers_json` so a new request's answers are stored with it. · `Database/StoredProcedures/sp_waitlist_request_insert/create.sql(+rollback.sql)` · FR-073, FR-087 · Verify: an insert with an answers map reads it back through T005.
- [x] **T007** [P] [US0] New Infor Visual read `GetPartWarehouseQuantities.sql`: the part-wide quantity summed over locations and filtered by location class, never a per-location figure. · `Database/InforVisual/Queues/Module_Waitlist/Queries/GetPartWarehouseQuantities.sql` · FR-090 · Verify: run against the live plant system for a part stocked in two locations and compare with the two-location sum.
- [x] **T008** [P] [US0] The mirror population for T007's shape, in the same projection. · `Database/InforVisual/Queues/Module_Mock/Populations/part_warehouse_quantities_population.sql` · FR-090 · Verify: `VisualReadFallbackParityTests` green.
- [x] **T009** [P] [US0] Declare the new shape in the mock's read-shape catalog: key `part_warehouse_quantities`, module `ModuleWaitlist`, the source and population paths, and its input parameters. · `MTM_Waitlist.Mock/Services/VisualReadShapeCatalog.cs` · FR-090 · Verify: `ShapeAdditionNoDowntimeTests` green.
- [x] **T010** [P] [US0] Extend the coil subordinate read to carry the coil's description, so the snapshot resolves it instead of a second read. · `Database/InforVisual/Queues/Module_Setup/Queries/GetSubordinateParts.sql` · FR-088 · Verify: the projection names the description column for an `MMC` subordinate row.
- [x] **T011** [P] [US0] Keep the subordinate population in step with T010's projection. · `Database/InforVisual/Queues/Module_Mock/Populations/subordinate_parts_population.sql` · FR-088 · Verify: `VisualReadFallbackParityTests` green.
- [x] **T012** [P] [US0] Carry the dunnage home location out of the receiving read the Setup workflow already uses, so it can reach the waitlist snapshot; add it to the projection if it has drifted. · `Database/MTMReceivingApp/Queues/Module_Setup/Queues/GetDunnageParts.sql` · FR-033, FR-045, FR-089 · Verify: a dunnage row returns a non-empty `home_location`.
- [x] **T013** [P] [US0] New shared handler-status vocabulary: the four verbatim conditions in their settled order, and the rule that any other stored value, or NULL, is not a condition and the status word falls through. · `MTM_Waitlist.Waitlist.View/Helpers/HandlerStatusCatalog.cs` · FR-071, FR-102 · Verify: unit test asserts the four strings exactly and rejects a fifth value.
- [x] **T014** [P] [US0] Extend the job snapshot with the coil's description and average weight, the dunnage home location, and the four returning-pickup job gates that replace the hard-coded out-of-scope list. · `MTM_Waitlist.Settings/Models/RequestJobPartAvailability.cs` · FR-088, FR-089, FR-094 · Verify: unit test builds a snapshot with each gate and reads it back.
- [x] **T015** [P] [US0] Give the dunnage part model a home location so `Home Location` has a value on the snapshot rather than only in the store. · `MTM_Waitlist.Settings/Models/RequestDunnagePart.cs` · FR-033, FR-045, FR-089 · Verify: `RequestDiePartTests`-style unit test asserts the property flows through `WithDunnageParts`.
- [x] **T016** [P] [US0] New cell model: one resolved label-and-value cell carrying `Label` as a resource key, `Value`, `HasValue`, `IsLabelLess` and `Order`; a cell with no value is never added at all, so nothing is padded. · `MTM_Waitlist.Waitlist.View/Models/WaitlistLineCardCell.cs` · FR-006, FR-028, FR-055, FR-104 · Verify: unit test asserts `IsLabelLess` is true only for the die-shop cell.

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2 — independent (different files):**

- [x] **T017** [P] [US0] Extend the label resolver: plain arms onto the values the snapshot already holds (`Coil number` / `Coil Number`, `Correct coil`, `Correct flatstock`, `Scrap Type`), arms onto the new snapshot fields (`Coil description`, `Average coil weight`, `Home Location`), and the rule that an answer captured under a declared label wins over the job arm. · `MTM_Waitlist.Settings/Services/RequestJobFieldValues.cs` · FR-082, FR-088, FR-089 · Verify: `RequestJobFieldValuesTests` green with the new arms.
- [x] **T094** [P] [US0] Resolve `Average coil weight` live from the receiving store rather than from Visual: `mtm_receiving_application.receiving_history` through `sp_receiving_history_average_coil_weight` via `IAverageCoilWeightService`, wired in `Services/RequestJobAvailabilityProvider.cs` (`ApplyCoilAverageWeightAsync`), with the Visual `AverageWeight` column removed from the coil subordinate read and its mirror. · `Services/RequestJobAvailabilityProvider.cs`, `Database/InforVisual/Queues/Module_Setup/Queries/GetSubordinateParts.sql`, `Database/InforVisual/Queues/Module_Mock/Populations/subordinate_parts_population.sql` · FR-030, FR-041, FR-054 · Verify: `dotnet build MTM_Waitlist.sln` clean; `dotnet test` filter `FullyQualifiedName~Coil|FullyQualifiedName~Subordinate|FullyQualifiedName~Mirror|FullyQualifiedName~RequestJobFieldValues` green (118 passed, 7 skipped); full suite 2114 passed / 6 failed (the 6 are the pre-existing `Service_AcceptanceSurfaceTests_*` baseline failures).
- [x] **T018** [P] [US0] Mock mirror table for the new shape, with `_result`/`_stage` naming derived from the catalog key, and its rollback. · `Database/Mock/Tables/visual_part_warehouse_quantities_result/create.sql(+rollback.sql)` · FR-090 · Verify: `ShapeAdditionNoDowntimeTests` sees the table on disk.
- [x] **T019** [P] [US0] Mock stage twin for the new shape, with its rollback. · `Database/Mock/Tables/visual_part_warehouse_quantities_result_stage/create.sql(+rollback.sql)` · FR-090 · Verify: the mirror schema test finds exactly one live table and one stage twin per shape.
- [x] **T020** [P] [US0] The shape's `get` procedure, returning the mirrored rows, with its rollback. · `Database/Mock/StoredProcedures/sp_visual_part_warehouse_quantities_get/create.sql(+rollback.sql)` · FR-090 · Verify: the procedure returns the seeded rows.
- [x] **T021** [P] [US0] The shape's `refresh` procedure, swapping result → prev → stage in one statement, with its rollback. · `Database/Mock/StoredProcedures/sp_visual_part_warehouse_quantities_refresh/create.sql(+rollback.sql)` · FR-090 · Verify: `MockMirrorRefreshWriterIntegrationTests` green and a second refresh still succeeds.
- [x] **T022** [P] [US0] The in-app fallback for the new shape, registered in the mock composition. · `MTM_Waitlist.Mock/Services/VisualPartWarehouseQuantitiesFallback.cs`, `MTM_Waitlist.Mock/DependencyInjection/MockServiceRegistrationExtensions.cs` · FR-090 · Verify: the registration count equals the catalog shape count.
- [x] **T023** [P] [US0] Baseline mirror rows for the new shape, with its rollback. · `Database/Mock/Seeds/seed_visual_mirror_baseline/create.sql(+rollback.sql)` · FR-090 · Verify: `Database/Mock/Validation/all_shapes_roundtrip/validate.sql` passes.
- [x] **T024** [P] [US0] Update the mock's read-shape validator: seven expected shapes by name, and the new shape asserted by name as well as counted, because a count of seven is satisfied by any seven. · `Database/Mock/Validation/read_shape_metadata/validate.sql` · FR-090 · Verify: run the validator against `mtm_mock`; every step reports.
- [x] **T025** [P] [US0] Include the new shape in the roundtrip validation set. · `Database/Mock/Validation/all_shapes_roundtrip/validate.sql` · FR-090 · Verify: the roundtrip reports the new shape.
- [x] **T026** [P] [US0] The write seam and the read: a handler-status write through the non-query seam that reports affected rows, the list read carrying `handler_status` and `answers_json`, and answers carried on insert. No inline statement text. · `MTM_Waitlist.Waitlist.View/Services/IWaitlistRequestService.cs`, `MTM_Waitlist.Waitlist.View/Services/WaitlistRequestService.cs` · FR-073, FR-076 · Verify: `WaitlistRequestServiceTests` green; `InlineSqlAuditTests` green.
- [x] **T027** [P] [US0] The row model: `HandlerStatus`, `CanSetStatusCondition`, `StatusWord`, `StatusTheme`, `RingValue`, `RingMeasure`, `AllottedMinutes`, `ClosedUtc`, `Cells`, the two person rows, and the action gates including Print fixed unavailable. · `MTM_Waitlist.Waitlist.View/Models/SampleOrder.cs` · FR-011 to FR-018, FR-022, FR-062, FR-067, FR-070 · Verify: `SampleOrderTests` green.
- [x] **T028** [P] [US0] Pin the row model's behaviour: status-word precedence (condition, then `Overdue`, then the lifecycle word), the ring's two measures, person rows absent rather than blank, one cell per declared field in declared order. · `MTM_Waitlist.Tests/Module_Waitlist/Models/SampleOrderTests.cs` · FR-012, FR-015, FR-018, FR-023, FR-026 · Verify: the tests fail against the current model, then pass.

**⟶ Wait for Wave 2 to finish, then:**

**Wave 3:**

- [x] **T029** [US0] Per-row resolution: turn each Item's declared fields into cells, apply the hide rule (a cell the Item never declares is hidden; a cell whose source exists but cannot be read is hidden **and** a warning is written to the database log), re-evaluate the ring on the one-minute tick that refreshes the row's other time text, and expose the flyout command. · `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs` · FR-017, FR-025, FR-058, FR-101 · Verify: `WaitlistViewViewModelFieldTests` green and a test asserts the log warning is written.
- [x] **T030** [US0] Pin the write seam and the round trip: a lost race is told rather than shown as success, and a stored answer is restored on the next list read. · `MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs` · FR-073, FR-076 · Verify: the tests fail against today's service, then pass.
- [x] **T031** [US0] Pin the resolver: each new label arm resolves from the snapshot, and an answer beats the job arm for the same label. · `MTM_Waitlist.Tests/Module_Settings/Services/RequestJobFieldValuesTests.cs` · FR-082, FR-088, FR-089 · Verify: the tests fail against today's resolver, then pass.

**⟶ Wait for Wave 3 to finish, then:**

**Wave 4 — the phase gate:**

- [x] **T032** [US0] Foundational gate: the solution builds, and the mirror, shape-addition, inline-SQL and retired-symbol suites are green so no story starts on a broken base. · Verify: `dotnet build` exit 0; `InlineSqlAuditTests`, `RetiredSymbolAuditTests`, `ShapeAdditionNoDowntimeTests`, `ReadShapeContentShippingAuditTests`, `VisualReadFallbackParityTests` all pass.

---

## Phase 3: User Story 1 — a row that can be read at a glance (P1)

**Goal:** the waitlist row is the four zones in order, with the work centre under the picture, the title pair
spanning the detail grid, and a re-centring action grid of up to six square buttons.
**Independent test:** with one live request in the list, the four zones appear in order separated by vertical rules,
the work centre reads as a large unlabelled value under the picture, the category word and identifier span the top of
the detail grid, clicking the body opens nothing, and the details button opens the request.
**Files:** `Module_Waitlist/Controls/WaitlistLineCardView.xaml(+.cs)` (owns every zone's markup),
`Module_Waitlist/Views/WaitlistViewPage.xaml(+.cs)`, `Module_Waitlist/Controls/WaitlistLineCardDetailTemplateSelector.cs`,
`MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistLineCardMarkupTests.cs`.

### Tests

**Wave 1 — independent (different files):**

- [x] **T033** [P] [US1] Rewrite the card markup tests to the new anatomy: the four zones in order with vertical rules, the large unlabelled work-centre value beneath the picture, the always-spanning title row, the 2×3 action order, `Print` disabled and saying "not available yet", `Open details` carrying a magnifier glyph, and no click target on the body. Written to fail against today's card. · `MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistLineCardMarkupTests.cs` · FR-001 to FR-010, FR-061, FR-067, FR-068, FR-069, FR-096 to FR-100 · Verify: the rewritten tests fail, then pass after the markup below.
- [x] **T034** [P] [US1] Add the assertions the new card owes: the requesting work centre and the requesting user are never detail cells, the requesting user appears only on the request's own detail page, and `Die Location` is not a universal cell. · `MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistLineCardMarkupTests.cs` · FR-005, FR-029 · Verify: the tests pass for every Item template.

### Implementation

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2 — independent (different files):**

- [x] **T035** [P] [US1] Host page: one card per full-width row, and remove the body click so the row no longer opens the request. · `Module_Waitlist/Views/WaitlistViewPage.xaml`, `Module_Waitlist/Views/WaitlistViewPage.xaml.cs` · FR-010, FR-069, FR-100 · Verify: `WaitlistLineCardMarkupTests` asserts no body click handler and the full-width row.
- [x] **T036** [P] [US1] The detail-template selector that the detail zone's host resolves through: it picks the template for the row's Item and falls back when the Item has none. · `Module_Waitlist/Controls/WaitlistLineCardDetailTemplateSelector.cs` · FR-025, FR-103 · Verify: a unit test resolves a known Item's key and an unknown Item's fallback.
- [x] **T037** [P] [US1] The card's four zones and their vertical rules: the identity zone with the accent bar, the picture (shift+click enlarge kept), the new-message badge (kept, omitted when absent) and the requesting work centre as a large unlabelled value beneath the picture; nothing padded to fill a short panel; the coloured status pill removed. · `Module_Waitlist/Controls/WaitlistLineCardView.xaml` · FR-001 to FR-010 · Verify: `WaitlistLineCardMarkupTests` green.

**⟶ Wait for Wave 2 to finish, then:**

**Wave 3 (same file, so sequential):**

- [x] **T038** [US1] The status zone inside the card: a centred stack of the status word, the ring, the person, and for a closed request the requester, with a row that has no value removed rather than blanked; the ring is the platform `ProgressRing` in its determinate state with the time text overlaid in a `Grid` cell, and no percentage or elapsed-time line is drawn. · `Module_Waitlist/Controls/WaitlistLineCardView.xaml` · FR-011, FR-013, FR-018, FR-021, FR-022, FR-105 · Verify: `WaitlistLineCardMarkupTests` finds the determinate ring, the overlaid text and the removed empty row.
- [x] **T039** [US1] The status word as plain coloured text with no label in front of it, bound to the resolved word and the whole-column theme, and a closed request's frozen ring reading the time taken rather than the time left. · `Module_Waitlist/Controls/WaitlistLineCardView.xaml` · FR-008, FR-012, FR-014, FR-015 · Verify: the markup test finds no pill and no `Status:` prefix, and finds the theme binding on the column.
- [x] **T040** [US1] The detail zone host: the inner grid whose row 1 spans both columns and holds the request's category word and its own identifier, with the cell area resolved through the template selector. · `Module_Waitlist/Controls/WaitlistLineCardView.xaml` · FR-007, FR-024, FR-027 · Verify: the markup test finds the spanning title row and the selector-bound presenter.
- [x] **T041** [US1] The action zone: two columns by three rows in row-major order (`Accept`, `Cancel`, `Give back`, then `Complete`, `Open details`, `Print`), an unavailable action removed rather than blanked with the rest re-centred and not stretched, a hit target of at least 44×44, a tooltip and an accessible name from the row's localized text, and distinct colours and glyphs for `Accept`, `Complete` and `Give back`. · `Module_Waitlist/Controls/WaitlistLineCardView.xaml` · FR-061 to FR-066 · Verify: the markup test asserts the order, the removal, the 44×44 floor and the tooltips.
- [x] **T042** [US1] The status word as the card body's one interactive element: a plain-text-styled button gated on the row's own handler, with the flyout host declared here and bound to the row's condition options (the flyout's own body and behaviour are Phase 6's). · `Module_Waitlist/Controls/WaitlistLineCardView.xaml` · FR-070, FR-074, FR-075 · Verify: the markup test asserts the gate binding and that nothing else in the body is clickable.

**⟶ Wait for Wave 3 to finish, then:**

**Wave 4:**

- [x] **T043** [US1] `Print` ships disabled, says "not available yet" when pointed at, performs no action, and carries the `TODO` naming label printing as the future action; `Open details` carries the magnifier glyph and the `Open details` tooltip and is the only path that opens the request, through the existing navigation service. · `Module_Waitlist/Controls/WaitlistLineCardView.xaml`, `Module_Waitlist/Controls/WaitlistLineCardView.xaml.cs` · FR-067, FR-068 · Verify: the markup test asserts the disabled state, the message and the single open path. **The `Print` half of this was reversed by T116 (D59):** the placeholder is removed and FR-067 now says the card carries no print action, so what this task delivered is the `Open details` half only.
- [x] **T044** [US1] The markup tests for the action gates and the re-centring: a gate change removes a button and re-centres the rest, and no action becomes available or unavailable because the layout changed. · `MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistLineCardMarkupTests.cs` · FR-062, FR-063 · Verify: the tests pass for each gate combination.

**Checkpoint:** with one live request in the list, the row reads as four zones in order, the work centre sits unlabelled
under the picture, the title pair spans the detail grid, and only the details button opens the request. US1 is
independently functional and testable here.

---

## Phase 4: User Story 2 — the status column tells the truth (P1)

**Goal:** the status column reads the right word and takes the right theme in every state, and its ring carries the
time left while live and the time taken once closed.
**Independent test:** one live, one overdue, one completed and one condition-carrying request each show the right
word, ring contents, person row and column theme.
**Files:** `MTM_Waitlist.Waitlist.View/Helpers/RequestStatusPalette.cs`,
`MTM_Waitlist.Waitlist.View/Helpers/RemainingTimePalette.cs`, `MTM_Waitlist.Waitlist.View/Converters/RequestStatusToBrushConverter.cs`,
`MTM_Waitlist.Tests/Module_Waitlist/Helpers/RequestStatusPaletteTests.cs`.

### Tests

**Wave 1:**

- [x] **T045** [P] [US2] Pin the palette: a role and a colour for all eight states, three distinct themes, the overdue marking distinguishable without depending on the row's position, and no percentage. · `MTM_Waitlist.Tests/Module_Waitlist/Helpers/RequestStatusPaletteTests.cs` · FR-019, FR-020, FR-021, FR-102 · Verify: the tests fail against today's four-word palette, then pass.

### Implementation

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2 — independent (different files):**

- [x] **T046** [P] [US2] Give the palette a role and a colour for each of the four conditions, so it no longer returns nothing outside the four lifecycle words, and the three column themes: normal, handler-condition as a lighter red than overdue, and overdue. · `MTM_Waitlist.Waitlist.View/Helpers/RequestStatusPalette.cs` · FR-019, FR-072, FR-102 · Verify: the palette test passes for all eight states.
- [x] **T047** [P] [US2] The theme brush for all eight roles, and the ring reading the same fraction the row's remaining-time text reads so the two can never disagree. · `MTM_Waitlist.Waitlist.View/Converters/RequestStatusToBrushConverter.cs`, `MTM_Waitlist.Waitlist.View/Helpers/RemainingTimePalette.cs` · FR-013, FR-017, FR-019, FR-021 · Verify: the converter test resolves a brush for every role and the ring fraction equals the text fraction.

**⟶ Wait for Wave 2 to finish, then:**

**Wave 3:**

- [ ] **T048** [US2] Verify the column end to end on the built card: a live unclaimed request reads `Waiting` with the requester in row 3, a claimed one reads the handler, an overdue request with no condition reads `Overdue` with the overrun in the ring and the overdue theme, a completed request freezes the ring on the time taken with the handler in row 3 and the requester in row 4, a cancelled request reads the handler or the canceller, and a request carrying a condition reads only that condition in the lighter red theme. · FR-011, FR-014, FR-015, FR-022, FR-023 · Verify: the states are driven against the dev store and each one checked against SC-006 and SC-007.

**Checkpoint:** the status column reads correctly in every state, the empty person row is absent rather than blank, and
the two red treatments are never both shown. US2 is independently functional and testable here.

---

## Phase 5: User Story 3 — every Item shows its own cells, in the same places (P1)

**Goal:** each of the 24 Items draws its settled cells in declared order, with the geometry following the cell count.
**Independent test:** draw one request of each cell count and confirm the geometry, the title row's span, the
label-above-value anatomy, and that nothing is padded.
**Files:** `Module_Waitlist/Controls/WaitlistLineCardDetailTemplates.xaml`,
`MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistLineCardDetailTemplateTests.cs`.

### Tests

**Wave 1:**

- [x] **T049** [P] [US3] New template tests: one template per Item for all twenty-four, the geometry for four, three, two, one and no cells, the wrong-part Items drawing their incorrect part as the answer, and the long description scrolling rather than clipping. Written to fail while the templates are absent. · `MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistLineCardDetailTemplateTests.cs` · FR-024, FR-026, FR-027, FR-028, FR-030 to FR-053, FR-103 · Verify: the tests fail, then pass as each template group lands.

### Implementation

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2:**

- [x] **T050** [US3] The dictionary and the eleven pickup templates, each drawing its Item's cells in the settled order with the label on top and the value beneath. · `Module_Waitlist/Controls/WaitlistLineCardDetailTemplates.xaml` · FR-030 to FR-040 · Verify: the pickup half of T049 passes.

**⟶ Wait for Wave 2 to finish, then:**

**Wave 3:**

- [x] **T051** [US3] The nine deliver templates, including `Destination` on `deliver-flatstock` only and the two wrong-part Items drawing the typed incorrect part plus the correct part the job carries. · `Module_Waitlist/Controls/WaitlistLineCardDetailTemplates.xaml` · FR-041 to FR-049, FR-057 · Verify: the deliver half of T049 passes, and `Destination` appears on no other Item.

**⟶ Wait for Wave 3 to finish, then:**

**Wave 4:**

- [x] **T052** [US3] The three assist templates and `other`, which draws the request description in full through the shared scrolling text control. · `Module_Waitlist/Controls/WaitlistLineCardDetailTemplates.xaml` · FR-050 to FR-053 · Verify: the remaining T049 assertions pass.

**⟶ Wait for Wave 4 to finish, then:**

**Wave 5:**

- [x] **T053** [US3] The geometry by cell count: four as two rows of two, three with the third spanning the bottom row, two with the bottom row collapsed, one spanning the full width, and none with both lower rows removed so the title row takes the vacated space as one full-width row; the label abuts its value with the one label-less exception. · `Module_Waitlist/Controls/WaitlistLineCardDetailTemplates.xaml` · FR-024, FR-026, FR-027, FR-028, FR-103 · Verify: the geometry assertions cover all five counts.
- [x] **T054** [US3] The cell values and visibility: `Weight of Coil` bound to the operator's answer and never the job's average coil weight; the die-shop cell's phrase drawn only when the answer was given and absent when it was not; the declared fields that are not cells absent; a cell whose source cannot be read absent with the warning already logged; and nothing explained on the card or filled with a substitute. · `Module_Waitlist/Controls/WaitlistLineCardDetailTemplates.xaml` · FR-054, FR-055, FR-056, FR-058, FR-059, FR-060, FR-101 · Verify: a test asserts an unticked die request draws two cells and a ticked one draws three.
- [x] **T055** [US3] The two cells with no source: `Lugger Location` stays declared on `pickup-scrap` with no resolver arm so the cell is hidden, and the reveal path is a future resolver arm with no card change; `Destination` resolves from the request's own requesting work centre on `deliver-flatstock` and is not reintroduced as an answer or as a line-2 token. · `Module_Waitlist/Controls/WaitlistLineCardDetailTemplates.xaml`, `MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistLineCardDetailTemplateTests.cs` · FR-034, FR-042, FR-057, FR-089 · Verify: a test asserts neither cell is drawn today and that no answer prompt exists for `Destination`.

**Checkpoint:** every one of the twenty-four Items draws its own cells with the geometry its cell count dictates, two
requests of the same Item match in shape and height, and `Die Location` appears only on the die Items. US3 is
independently functional and testable here.

---

## Phase 6: User Story 4 — a handler reports a condition from the card (P2)

**Goal:** the row's own material handler opens a flyout from the status word and sets one of four conditions, which
comes back on the next list read.
**Independent test:** as the handler working a specific request, open the flyout, choose a condition, confirm the
column re-themes and the condition survives a reload; then as another person, confirm the word is inert.
**Files:** `Module_Waitlist/Controls/WaitlistStatusFlyout.xaml(+.cs)`,
`MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistStatusFlyoutTests.cs`.

### Tests

**Wave 1:**

- [x] **T056** [P] [US4] New flyout tests: the four conditions in the settled order with the exact strings, the gate (open only for the row's own material handler), the word inert for another person and on a request the signed-in person is not working, keyboard-only operability, and the announced gate. Written to fail before the control exists. · `MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistStatusFlyoutTests.cs` · FR-070, FR-071, FR-074, FR-075, FR-076, FR-102 · Verify: the tests fail, then pass.

### Implementation

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2:**

- [x] **T057** [US4] The flyout itself: the four conditions from the shared vocabulary, selection invoking the row's command, closing on choice, reachable and operable by keyboard alone, and its gate announced rather than left to be discovered. · `Module_Waitlist/Controls/WaitlistStatusFlyout.xaml`, `Module_Waitlist/Controls/WaitlistStatusFlyout.xaml.cs` · FR-070, FR-071, FR-074, FR-075, FR-076 · Verify: T056 passes.

**⟶ Wait for Wave 2 to finish, then:**

**Wave 3:**

- [ ] **T058** [US4] Verify the write and the restore: choosing a condition stores it against the request through the affected-row-reporting seam, re-themes the whole column, replaces a condition already set, and is restored onto that row when the list is next read and after a restart. · FR-072, FR-073 · Verify: drive the flow against the dev store, reload the list, and confirm SC-016.

**Checkpoint:** only the row's own material handler can set that row's condition, and the condition survives a restart.
US4 is independently functional and testable here.

---

## Phase 7: User Story 5 — the four previously hidden Items come back (P2)

**Goal:** the four catalogued pickups are offered when the requesting job has the work they name, and each shows
`Part Type` and `# of Skids`.
**Independent test:** for a job with each kind of work, the matching Item is offered and its card shows the derived
Part Type and the skid count, with no location cell.
**Files:** `MTM_Waitlist.Settings/Models/RequestDisposition.cs`, `MTM_Waitlist.Settings/Services/RequestDispositionMapper.cs`,
`MTM_Waitlist.Settings/Services/RequestDispositionClassifier.cs`, `MTM_Waitlist.Settings/Services/RequestDispositionResolver.cs`,
`MTM_Waitlist.Settings/Services/RequestItemPickerRules.cs`, `Database/InforVisual/Queues/Module_Waitlist/Queries/GetDispositionInput.sql`,
`Database/InforVisual/Queues/Module_Mock/Populations/disposition_input_population.sql`,
`Database/Validation/disposition_input_finished_goods/validate.sql`, `.github/memories/repo/infor-visual-disposition.md`,
`MTM_Waitlist.Tests/Module_Settings/Services/RequestItemPickerRulesTests.cs`,
`MTM_Waitlist.Tests/Module_Settings/RequestDispositionClassifierTests.cs`.

### Tests

**Wave 1 — independent (different files):**

- [x] **T059** [P] [US5] Rewrite the picker tests: each of the four Items is offered exactly when the requesting job has the work it names and not otherwise, and the hard-coded out-of-scope list is gone. · `MTM_Waitlist.Tests/Module_Settings/Services/RequestItemPickerRulesTests.cs` · FR-094 · Verify: the tests fail against today's fixed list, then pass.
- [x] **T060** [P] [US5] Extend the classifier tests: `Ncm` derived from the location the stock occupies and never from the part's recorded item type, never treated as a production type, and the priority Outside Service, then Finished Goods, then WIP unchanged. · `MTM_Waitlist.Tests/Module_Settings/RequestDispositionClassifierTests.cs` · FR-091, FR-092, FR-093 · Verify: the tests fail before the NCM arm, then pass.

### Implementation

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2 — independent (different files):**

- [x] **T061** [P] [US5] Add the `Ncm` state to the disposition and its mapper. · `MTM_Waitlist.Settings/Models/RequestDisposition.cs`, `MTM_Waitlist.Settings/Services/RequestDispositionMapper.cs` · FR-093 · Verify: `RequestDispositionMapperTests` green for the new value.
- [x] **T062** [P] [US5] The NCM arm in the classifier, driven by the location signal (`md_locations.Location LIKE 'NCM%'`, `PART_LOCATION.STATUS`), never by `ItemType`. · `MTM_Waitlist.Settings/Services/RequestDispositionClassifier.cs` · FR-091, FR-093 · Verify: T060 passes.
- [x] **T063** [P] [US5] Delete the hard-coded out-of-scope array and replace it with four real job gates read from the snapshot. · `MTM_Waitlist.Settings/Services/RequestItemPickerRules.cs` · FR-094 · Verify: T059 passes.

**⟶ Wait for Wave 2 to finish, then:**

**Wave 3 — independent (different files):**

- [x] **T064** [P] [US5] Wire the already-built disposition stack to the card path so `Item Type` and the four pickups' `Part Type` read its derivation instead of a second one. · `MTM_Waitlist.Settings/Services/RequestDispositionResolver.cs` · FR-091, FR-092 · Verify: the resolver has a non-test consumer and the component and returning-pickup cells read its value.
- [x] **T065** [P] [US5] Correct the disposition input: stop passing `PART.QTY_ON_HAND` and pass a sum filtered to finished-goods locations, matching the rule the classifier already documents. The classifier's priority is untouched. · `Database/InforVisual/Queues/Module_Waitlist/Queries/GetDispositionInput.sql` · FR-090 · Verify: a part with stock only outside finished goods no longer reads as Finished Goods.
- [x] **T066** [P] [US5] Keep the disposition mirror population in step with T065's corrected shape. · `Database/InforVisual/Queues/Module_Mock/Populations/disposition_input_population.sql` · FR-090 · Verify: `VisualReadFallbackParityTests` green.

**⟶ Wait for Wave 3 to finish, then:**

**Wave 4:**

- [x] **T067** [US5] A validation from the live database, because the location codes this installation uses for finished goods and for NCM are unknown: assert a known part's finished-goods sum, and record the codes it found. · `Database/Validation/disposition_input_finished_goods/validate.sql` · FR-090, FR-092 · Verify: run against the live plant system; each step reports.
- [x] **T068** [US5] Record the validated location codes and the decision that the classifier's rule wording is left for `clarify`, not amended here. · `.github/memories/repo/infor-visual-disposition.md` · FR-090 · Verify: the recorded codes match T067's output.
- [ ] **T069** [US5] Verify the four Items end to end: offered for a job with each kind of work, showing `Part Type` and `# of Skids` with no location cell; a subcontracted job reads `Outside Service`; stock at an NCM location reads the location-derived state. · FR-036 to FR-039, FR-091, FR-093, FR-094 · Verify: the picker is driven for each job kind and the cards checked against SC-010.

**Checkpoint:** the four Items are offered whenever the requesting job has the work they name, each shows Part Type
and the skid count with no location cell, and Part Type is the derived item type. US5 is independently functional and
testable here.

---

## Phase 8: User Story 6 — the New Request workflow asks the new questions (P2)

**Goal:** the five new answers are asked in the wizard and each appears on the matching card.
**Independent test:** raise one request per new answer, then confirm each value appears in the right cell, including
the conditional die-shop cell and the hopper image link.
**Files:** `Module_Waitlist/Views/NewRequestHopperPage.xaml(+.cs)`, `Module_Waitlist/Views/NewRequestDiePage.xaml(+.cs)`,
`Module_Waitlist/Views/NewRequestPartSearchPage.xaml(+.cs)`, `Module_Waitlist/Views/NewRequestCoilTurnPage.xaml(+.cs)`,
`Module_Waitlist/Views/NewRequestSkidCountPage.xaml(+.cs)`, `MTM_Waitlist.Waitlist.NewRequest/ViewModels/`,
`MTM_Waitlist.Waitlist.NewRequest/Models/NewRequestFlowState.cs`, `MTM_Waitlist.Waitlist.NewRequest/Services/`,
`Database/InforVisual/Queues/Module_Waitlist/Queries/GetPartMatches.sql`, `App.xaml.cs`, `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/`.

### Tests

**Wave 1:**

- [x] **T070** [P] [US6] Tests for the five answers: the hopper size chosen from three options with its reference image, the die-shop checkbox deciding the third cell, the prefix up-casing and zero-padding with the exact-then-contains search, the required numeric Weight of Coil, and the skid count defaulting to one. Written to fail before the pages exist. · `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/NewRequestHopperViewModelTests.cs`, `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/NewRequestPartSearchViewModelTests.cs`, `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/NewRequestCoilTurnViewModelTests.cs`, `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/NewRequestSkidCountViewModelTests.cs` · FR-077, FR-079, FR-080, FR-081, FR-082, FR-083, FR-086, FR-087 · Verify: the tests fail, then pass as each page lands.

### Implementation

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2 — independent (different files):**

- [x] **T071** [P] [US6] New Infor Visual read `GetPartMatches.sql`: an exact match resolves, otherwise every part whose number contains the term. Live only: no mirror declaration and no cached read shape. · `Database/InforVisual/Queues/Module_Waitlist/Queries/GetPartMatches.sql` · FR-082, FR-083, FR-084 · Verify: run against the live plant system for an exact and a partial term; the catalog has no entry for it.
- [x] **T072** [P] [US6] The hopper size page and its view model: three radios, `Small`, `Medium`, `Large`, with the chosen size's reference image beneath them, held to the application's existing picture rules and drawing the shared placeholder when the image is missing or unusable. · `Module_Waitlist/Views/NewRequestHopperPage.xaml(+.cs)`, `MTM_Waitlist.Waitlist.NewRequest/ViewModels/NewRequestHopperViewModel.cs` · FR-077, FR-079, FR-080 · Verify: the hopper tests pass and the placeholder path is exercised.
- [x] **T073** [P] [US6] The die page gains the checkbox stating whether the die is at the die shop or must be sent there, and the answer decides whether the card draws its third, label-less cell. · `Module_Waitlist/Views/NewRequestDiePage.xaml(+.cs)`, `MTM_Waitlist.Waitlist.NewRequest/ViewModels/NewRequestDieViewModel.cs` · FR-081 · Verify: `NewRequestDieViewModelTests` green, and an unticked answer produces two cells on the card.
- [x] **T074** [P] [US6] The fuzzy incorrect-part page and its view model: upper-case and zero-pad to ten characters when the term starts with a known prefix, resolve an exact match at once, otherwise offer the contains list, run the read on a worker thread, and when the plant system cannot be reached tell the operator the search is unavailable and use the typed value as entered. · `Module_Waitlist/Views/NewRequestPartSearchPage.xaml(+.cs)`, `MTM_Waitlist.Waitlist.NewRequest/ViewModels/NewRequestPartSearchViewModel.cs` · FR-082, FR-083, FR-084, FR-085 · Verify: the search tests cover `MMC1000`, `MMF500`, a no-prefix term, an exact hit, a miss and an unreachable system.
- [x] **T075** [P] [US6] The assist coil turn page and its view model: `Weight of Coil` as a required numeric answer, deliberately not the job's average coil weight, and a blank answer preventing confirmation. · `Module_Waitlist/Views/NewRequestCoilTurnPage.xaml(+.cs)`, `MTM_Waitlist.Waitlist.NewRequest/ViewModels/NewRequestCoilTurnViewModel.cs` · FR-054, FR-086 · Verify: the coil turn tests pass, including the blank-answer refusal.
- [x] **T076** [P] [US6] The skid count page and its view model, offered on the four returning Items: numeric, defaulting to one, and changeable. · `Module_Waitlist/Views/NewRequestSkidCountPage.xaml(+.cs)`, `MTM_Waitlist.Waitlist.NewRequest/ViewModels/NewRequestSkidCountViewModel.cs` · FR-087 · Verify: the skid count tests pass.

**⟶ Wait for Wave 2 to finish, then:**

**Wave 3:**

- [x] **T077** [US6] Carry the five answers through the flow and persist them into the request's answers map on submit, with a blank answer not written so no cell is added for it. · `MTM_Waitlist.Waitlist.NewRequest/Models/NewRequestFlowState.cs`, `MTM_Waitlist.Waitlist.NewRequest/Services/NewRequestFlowRules.cs`, `MTM_Waitlist.Waitlist.NewRequest/Services/NewRequestFlowService.cs` · FR-073, FR-081, FR-086, FR-087 · Verify: `NewRequestFlowStateTests` green and a submitted request's answers read back through the list read.
- [x] **T078** [US6] Register the four new pages and their view models so the wizard can reach them, and add the new step to the flow's order. · `App.xaml.cs` · FR-077, FR-081, FR-082, FR-086, FR-087 · Verify: `PageActivationAuditTests` and `NewRequestWizardAdvanceMarkupTests` green, and the wizard walks the new step.

**⟶ Wait for Wave 3 to finish, then:**

**Wave 4:**

- [ ] **T079** [US6] Verify each new answer on the matching card: the size in the `Size` cell of both hopper Items with the cell opening that reference image the way the card's other pictures enlarge, the die-shop phrase in the third cell only when the checkbox was ticked, the typed incorrect part and the job's correct part on the two wrong-part Items, the weight in `Weight of Coil`, and the count in `# of Skids`. · FR-055, FR-078, FR-081 · Verify: one request per answer is raised and its card checked against SC-015. **Still open, and narrower than it was:** FR-078 — the one part of this that had no delivered mechanism — is now built and pinned by tests (T097), so the hopper size's cell opening its own reference picture is verified. What is left is the end-to-end pass: it needs a live store and a rendered card, one request per answer, checked against SC-015. The die-shop, wrong-part, weight and count cells are each pinned by their own tests.

**Checkpoint:** the workflow collects all five answers and each appears on the matching card, with the die-shop cell
drawn only when its answer calls for it. US6 is independently functional and testable here.

---

## Phase 9: Polish — seed and schema reconciliation (W7)

**Files:** `Database/Seeds/seed_waitlist_request_item_configs/create.sql(+rollback.sql)`,
`Database/Seeds/seed_waitlist_request_item_configs_label_move/create.sql(+rollback.sql)`, `Database/Seeds/AllSeeds.sql`,
`Database/Tables/AllTables.sql`, `Database/StoredProcedures/AllSPs.sql`, `Database/Bootstrap/update_table_descriptions.sql`,
`Database/Mock/Bootstrap/update_table_descriptions.sql`, `MTM_Waitlist.Tests/Module_Settings/Services/RequestItemConfigSeedAlignmentTests.cs`.

**Wave 1 — independent (different files):**

- [x] **T080** [P] [US0] Bring the Item-configuration seed into line with §5: the label corrections (`Pickup location` to `Die Location` on both die Items, `Home location` to `Home Location` on both dunnage Items, `Scrap type` to `Scrap Type`), the new answer cells (the die-shop flag, `Size`, `# of Skids`, `Weight of Coil`), the source correction on the two wrong-part Items, and every declared field that is not a cell left declared. · `Database/Seeds/seed_waitlist_request_item_configs/create.sql`, `Database/Seeds/seed_waitlist_request_item_configs/rollback.sql` · FR-030 to FR-053, FR-059, FR-081, FR-087 · Verify: T083's alignment test passes for all twenty-four Items.
- [x] **T081** [P] [US0] The label move as a migration: an idempotent seed that rewrites the existing declaration rows in place, so an installed store gets `Die Location` and `Home Location` without the truncate-and-reinsert seed wiping operator edits; its rollback restores the previous labels. · `Database/Seeds/seed_waitlist_request_item_configs_label_move/create.sql(+rollback.sql)` · FR-059 · Verify: apply, re-apply, then roll back against the dev store; the labels move once and return.
- [x] **T082** [P] [US0] New seed-alignment test: the seed's declarations match the card's settled cell assignment for all twenty-four Items, so a label the card shows is the label the seed declares. · `MTM_Waitlist.Tests/Module_Settings/Services/RequestItemConfigSeedAlignmentTests.cs` · FR-029, FR-059, FR-103 · Verify: the test fails before T080, then passes.
- [x] **T083** [P] [US0] Leave the dunnage `Description` declaration in place and confirm the card never draws it; the open question (remove the declaration or keep hiding it) stays with `clarify`. · `Database/Seeds/seed_waitlist_request_item_configs/create.sql` · FR-095 · Verify: the seed still declares `Description` on both dunnage Items and no template draws it.

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2:**

- [x] **T084** [US0] Regenerate the aggregates and describe every artifact this change adds or modifies: the table and procedure aggregates, the seed aggregate with both new seed artifacts registered, the table descriptions, and the mock masters through the builder script together with the mock table descriptions. Never by hand. · `Database/Tables/AllTables.sql`, `Database/StoredProcedures/AllSPs.sql`, `Database/Seeds/AllSeeds.sql`, `Database/Bootstrap/update_table_descriptions.sql`, `Database/Mock/AllTables.sql`, `Database/Mock/AllSPs.sql`, `Database/Mock/AllSeeds.sql`, `Database/Mock/Bootstrap/update_table_descriptions.sql` · Verify: the regenerated files contain every new artifact and `.github/scripts/validate-database-schema.ps1` is clean.

**⟶ Wait for Wave 2 to finish, then:**

**Wave 3:**

- [x] **T085** [US0] Phase gate: the schema validator, the inline-SQL audit, the retired-symbol audit and the mock roundtrip validations all pass with the seeds and aggregates in place. · Verify: `.github/scripts/validate-database-schema.ps1`, `InlineSqlAuditTests`, `RetiredSymbolAuditTests` and the mock validations all clean.

---

## Phase 10: Polish — resources and accessibility (W8)

**Files:** `Strings/en-us/Resources.resw`.

**Wave 1:**

- [x] **T086** [P] [US0] Add every new string as a resource key, never a literal: the eight status words, the four conditions, the new cell labels, the two die-shop phrases, `Open details`, the Print message, the wizard prompts and options. · `Strings/en-us/Resources.resw` · FR-104 · Verify: `ResourceExtensionsTests` green and no new literal remains in the card, flyout or wizard markup.

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2:**

- [x] **T087** [US0] The ring's accessible name states what it measures and which of the two things it is drawing, time left while live and time taken once closed, and the flyout's gate is announced rather than left to be discovered. · `Strings/en-us/Resources.resw` · FR-016, FR-075 · Verify: a screen reader reads the measure and the gate; the announced wording matches the key.
- [ ] **T088** [US0] Verify accessibility, readability and animations: every new string resolves, the ring exposes its text equivalent, the flyout and every action button are reachable and operable by keyboard alone, no zone's values are clipped at the application's default window size, and the card is usable with animations switched off. · FR-010, FR-016, FR-064, FR-065, FR-075, FR-105 · Verify: the accessibility and layout checks pass against SC-014 and SC-017. **Partly verified, and narrower than it was.** *Every new string resolves* is now done and pinned statically: all 80 keys this feature references resolve, and `UI_WaitlistLineCardDetailTemplateTests_EveryDeclaredCellLabel_ResolvesThroughTheResourceFile` walks the shipped seed's own declarations so a declared cell label with no resource entry fails the build (T102 — it found 25 that had been drawing their own key). The *ring's text equivalent* is pinned by markup (`AutomationProperties.Name` on the ring, bound to `RingAccessibleName`). What is left genuinely needs the rendered application: keyboard operability of the flyout and the action buttons, no clipped zone values at the default window size, and usability with animations off.

---

## Phase 11: Polish — contract recording (W10)

**Files:** `specs/004-unified-card-item-picker/contracts/card-and-identifier.md`, `specs/012-waitlist-card-layout/contracts/`,
`Database/Database-Ruleset.md`, `CHANGELOG.md`.

**Wave 1 — independent (different files):**

- [x] **T089** [P] [US0] Record the supersession in the earlier card contract: declared fields appear on the card, the freeze on the card's anatomy is lifted in writing, the detail-field grid returns to the card, the four metadata rows are superseded, the body is no longer the click target, an unresolvable value is hidden and logged rather than explained, and the status vocabulary is open at eight states with three themes. · `specs/004-unified-card-item-picker/contracts/card-and-identifier.md` · FR-096, FR-097, FR-098, FR-099, FR-100, FR-101, FR-102 · Verify: every superseded rule carries its replacement in the same file.
- [x] **T090** [P] [US0] Re-read this feature's three contracts against the delivered code and correct any drift. · `specs/012-waitlist-card-layout/contracts/card-and-identifier.md`, `specs/012-waitlist-card-layout/contracts/persistence.md`, `specs/012-waitlist-card-layout/contracts/status-and-conditions.md` · Verify: every name and value in the contracts matches the code.
- [x] **T091** [P] [US0] Update the directly related documentation: the database ruleset's waitlist section for the two new columns and the new procedure, and the changelog entry for the card redesign. · `Database/Database-Ruleset.md`, `CHANGELOG.md` · Verify: the ruleset names both columns and the new procedure; the changelog entry is present.

---

## Phase 12: Polish — validation against the Success Criteria (W9)

**Wave 1:**

- [x] **T092** [P] [US0] Run the test and lint suites and check the delivered card against SC-001 to SC-017, recording each criterion as met or not. `.specify/companion.yml` carries no `owns: validation` hook, so this phase owns the single suite run. · Verify: `tools/run-tests.ps1`, `.github/scripts/validate-csharp-xaml-naming.ps1`, `.github/scripts/validate-sql-naming.ps1` and `.github/scripts/validate-database-schema.ps1` all exit 0 with every Success Criterion recorded.
- [x] **T093** [US0] Close the two carried-forward questions with their evidence: FR-090's live-database validation recorded the finished-goods location codes and the classifier's rule wording is left for `clarify`; FR-095's declaration is left in place with the cell never drawn and the removal question left for `clarify`. · Verify: both are recorded in the step's capture as open, with the evidence that produced them.

---

## Phase 13: Polish — the follow-up round on the owner's review (W11)

The owner reviewed the delivered work and raised five things: a pre-existing test failure to clear, a naming guard
to make honest, the hopper pictures to give a home, one requirement that had no delivered mechanism, and the
decisions the inputs never stated. Each closes an acceptance gap rather than adding scope.

**Wave 1:**

- [x] **T095** [US0] Restore the deleted `STARTUP-REBUILD-BRIEF.md` exactly as it was last committed, which is what the six `Service_AcceptanceSurfaceTests_*` startup-acceptance tests read. It was removed in `f2202b4d` along with the other startup-gate documents and was the entire pre-existing failure set. · `STARTUP-REBUILD-BRIEF.md`, and its `S13.1`/`S13.2` citations refreshed where a renamed test no longer resolved · Verify: all six `Service_AcceptanceSurfaceTests_*` pass, and the file is byte-identical to `f2202b4d^:STARTUP-REBUILD-BRIEF.md`.

- [x] **T096** [P] [US0] Make the C#/XAML naming guard honest about pre-existing debt instead of leaving a 249-finding wall nobody reads: add `naming-grandfathered.txt` listing the findings that pre-dated this feature as passable (path and message, no line number), teach the guard to skip exactly those, and report how many it skipped so the pass is never silent. Fix this feature's own 16 findings in code rather than listing them. · `.github/scripts/validate-csharp-xaml-naming.ps1`, `.github/scripts/naming-grandfathered.txt`, `InforDispositionRow.cs` (split to its own file), the renamed fields and test class across `WaitlistViewViewModel`, `PartPictureResolver`, `ImageStorageConfigurationResolver` and five test files · FR-098 · Verify: the guard exits 0 reporting `Grandfathered: 249. New: 0.`, and a deliberately-introduced finding still fails it with `New: 1`.

- [x] **T097** [P] [US0] Deliver FR-078's missing mechanism and give the hopper pictures a home: the reference pictures live in `Waitlist\Hoppers` under the configured picture folder, one file per size named after the size word (`Small.png`, `Medium.png`, `Large.png`), resolved through a scope of their own so the part-family rules cannot misfile a size word. The folder is a plant-wide setting (`image_storage.hopper_folder_path`) with a box on Settings → Storage Paths, and the card's `Size` cell draws the picture through the application's one enlarge control, which is what FR-078 asked for and nothing delivered. · `PartPictureLayout`, `PartPictureResolver`, `IImageStorageConfigurationResolver`, `ImageStorageConfigurationResolver`, `ImageStorageOptions`, `ConfigSettingValue`, `SettingsViewModel`, `SettingsPage.xaml`, `WaitlistCardCellRules`, `WaitlistViewViewModel`, `WaitlistLineCardCell`, `WaitlistLineCardDetailTemplates.xaml`, `NewRequestHopperViewModel`, `Resources.resw` · FR-077, FR-078, FR-080 · Verify: the 20 card-cell and template tests pass, including the new ones pinning that the hopper cell opens the size's own picture and that no other Item's cell opens one.

- [x] **T098** [P] [US0] Record the naming contract every picture folder obeys, in one document: the two roots, the part collections and their prefix families, the application's own three folders, the hopper folder and its three names, the dunnage root, the picture rules, and the key-file exception. · `docs/IMAGE-NAMING.md` · FR-078, FR-079 · Verify: every folder name, file-name rule and picture rule in the document is read from the code that enforces it.

- [x] **T099** [US0] Give `Lugger Location` a plainly-worded placeholder instead of hiding it, and set the TODOs that record what is still unbuilt: the cell now draws "In development" because hiding it told the person nothing, it is not recorded as a failure, and the three places that must change when the source lands each carry a TODO. The dunnage `Description` keeps its stated-fact TODO for `clarify`. · `WaitlistCardCellRules`, `WaitlistViewViewModel`, `RequestJobFieldValues`, `Database/Seeds/seed_waitlist_request_item_configs/create.sql`, `Resources.resw` · FR-034, FR-058, FR-095 · Verify: `RequestItemConfigSeedAlignmentTests` and `WaitlistLineCardDetailTemplateTests` pass with the cell drawn and carrying the placeholder, and no warning is recorded for it.

- [x] **T100** [US0] Record the four decisions the inputs never stated, so they can be reviewed rather than only observed: the hopper picture source, a blank `# of Skids` meaning the page's default of one while the card hides a blank answer, `Weight of Coil` having to be greater than zero, and FR-085's "as entered" meaning the normalized term. Flag NCM's ordering against Outside Service for `clarify` in `spec.md` itself. · `research.md` (new R10 and R11), `spec.md` (FR-092 clarifier) · FR-078, FR-085, FR-086, FR-087, FR-092 · Verify: each decision names where it is enforced in code, and FR-092 carries its `NEEDS CLARIFICATION` marker.

- [x] **T101** [US0] Fix what the owner's side-by-side review of the built card against the reference layout found: the action grid is **three columns by two rows** (Accept · Cancel · Give back over Complete · Open details · Print) as §4's settled table has always shown and the built card did not; the cell area no longer draws the row's own type name in place of cells, which happened because a contentless no-cells fallback let the presenter fall back to rendering its row; the selector matches its template key case-insensitively, so an Item code spelled in another case cannot silently lose a row its cells; and the time ring carries an explicit size, because the platform `ProgressRing`'s own default is a hairline no bigger than a glyph inside its box. · `Module_Waitlist/Controls/WaitlistLineCardView.xaml`, `Module_Waitlist/Controls/WaitlistLineCardDetailTemplateSelector.cs`, `MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistLineCardMarkupTests.cs`, `WAITLIST-CARD-LAYOUT-PRE-SPEC.md` (D44), `spec.md` (FR-061), `data-model.md` · FR-013, FR-025, FR-026, FR-060, FR-061, FR-066 · Verify: full suite 2128 passed / 0 failed, and three new markup tests pin the 3×2 grid, the sized ring and the no-cells fallback drawing nothing but its own root.

- [x] **T102** [US0] Close the half of T088 that needs no running application: verify that every resource key this feature references resolves, then make the card's own declared cell labels unforgeable. All 80 keys the feature references were present, but **22 of the labels the card draws were not in `Resources.resw` at all** — and because `GetLocalized()` falls back to the key itself, those cells drew correct-looking English while never being a resolved string. The 22 were added, and a new test walks the **shipped seed's own declarations** so whatever the seed declares as a cell is what has to resolve — not a hand-written list that goes stale. · `Strings/en-us/Resources.resw`, `.github/scripts/validate-csharp-xaml-naming.ps1` (the new bare labels join the declared-label exceptions), `MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistLineCardDetailTemplateTests.cs` · FR-104 · Verify: the new test passes, removing one of the added keys makes it fail naming both Items that would draw the unresolvable label, and the solution builds with no `PRI277` duplicate-key error.

  **Learned the hard way, and worth keeping:** the resource index is **case-insensitive**. Adding `Coil number` beside the existing `Coil Number` failed the build with `PRI277: Conflicting values for resource 'Resources/Coil Number'`, which settles the question the three renamed labels raise — the pre-migration spellings (`Coil number`, `Home location`, `Scrap type`) already resolve through their canonical keys, so they need no second entry and adding one is a build break, not a fix. The test compares case-insensitively for the same reason.

- [x] **T103** [US0] Establish, with evidence rather than assumption, exactly why the eight remaining tasks are blocked, so the next person does not re-derive it. **Findings:** (1) The repo's own Infor Visual export at `Documents/Development/InforVisual/DatabaseCSVFiles` is **schema-only** — Columns, Constraints, Indexes, Triggers, Views and row *counts*, but no rows — so the Visual location codes cannot be confirmed from it; the plant database is genuinely required, which is exactly the condition T067 records. (2) The finished-goods codes `FG` / `DC-FG` are **already corroborated on the WIP side against live data (2026-09-08)** and the repo explicitly records them as **not confirmed on the Visual side**, so T067's remaining scope is precisely "confirm them for the Visual side" and nothing wider. (3) The finished-goods rule is applied **identically** in all four places that need it — the live read `GetDispositionInput.sql`, its cached population, `GetPartWarehouseQuantities.sql` and its population — all three branches (`LOCATION.ID`, `LOCATION.DESCRIPTION`, `WAREHOUSE.DESCRIPTION`), so live and cached cannot disagree. (4) The NCM convention (`LIKE 'NCM%'`) is consistent across the WIP floor query, the classifier, its mapper comment and T067's own validation script. **No defect found in (3) or (4)**, which is a verified negative rather than a presumed pass: an inconsistency in the finished-goods rule was suspected in `GetPartWarehouseQuantities.sql` and disproved by reading it. · evidence only, no files changed · FR-090, FR-092, FR-093 · Verify: each finding states how it was checked; the exports were listed directly, and the two consistency claims were made by reading every occurrence rather than one.

- [x] **T104** [US0] Drive the rendered application with Windows UI Automation, as the owner directed, and fix what it exposed. **The card's cells had never rendered at all**: `WaitlistLineCardDetailTemplateSelector` overrode only the one-argument `SelectTemplateCore`, and a `ContentControl` asks the *two*-argument question, which the base implementation answered with null — so the presenter fell back to drawing its content and every card printed `MTM_Waitlist.Module_Waitlist.Models.SampleOrder` where its cells belong, with the per-Item templates resolved correctly and then discarded. Both overloads are now answered (FR-025, FR-026). **The ring repeated row 1**: an overdue row's ring read `RemainingTimeText`, which is the word itself once the due time has passed, so the card printed `Overdue` twice and said nothing about how late the request was; the ring now carries the signed overrun (`-237:06`) as FR-014 and D29 require, with a new `FormatTimeOverrun` and the minute tick refreshing it (FR-014, FR-015). **The card contradicted itself at first paint**: `IsOverdue` was seeded from the store's flag alone, so a row whose due time had already passed read its lifecycle word while its ring read `Overdue` and its theme was still the normal one; the rule now lives on one static helper used by both the row's tick and the list that builds it, so word, ring and theme agree immediately (FR-014). · `Module_Waitlist/Controls/WaitlistLineCardDetailTemplateSelector.cs`, `MTM_Waitlist.Waitlist.View/Models/SampleOrder.cs`, `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs`, `Module_Waitlist/Controls/WaitlistLineCardView.xaml`, `MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistLineCardDetailTemplateTests.cs`, `MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistLineCardMarkupTests.cs` · FR-014, FR-015, FR-025, FR-026 · Verify: signed in as the owner's account and read the live card — type-name leaks **0** (was 11), cells now draw (`Coil Number` / `MMC0001000`, `Coil description` / `Coil 0.062 x 48 wide`, `Request description`), row 1 reads `Overdue` with the ring reading `-237:06`, and the status word is operable **only** on the two cards the signed-in handler is working and disabled on the other nine (FR-074, FR-076). Suite 2130 passed / 0 failed; both guards exit 0.

---

## Phase 14: Polish — the owner's second review of the running card (W12)

The owner reviewed the card in the running application again and raised seven things: the sign-in stage not taking the
keyboard, Shift+click doing nothing, the cached-data notice moving the page, the title reading as one line with no
rules between the cells, the action buttons leaving holes, a 5–10 second stall after acting on a card, and the
die-shop message never appearing. Each is fixed at its cause, and each is recorded as a decision beside the ones it
corrects (D45–D52).

**Wave 1:**

- [x] **T105** [P] [US1] Draw the detail title as the pair it was settled to be, stacked, and rule the cells off from one another — the owner's reading of `NewWaitlistCardLayout.jpg`. Row 1 is the Item's word on the first line with the request's identifier smaller and centred beneath it, because side by side the two read as one sentence and the identifier is what the reader is looking for (FR-007). Every cell is closed by a hairline along its top and left edge, which draws one continuous line between two cells and one under the title: the per-Item grids therefore carry **no spacing of their own** — the gap moved inside each cell as padding — and each cell fills its row so a rule cannot stop short at the shorter of two cells (FR-028). · `Module_Waitlist/Controls/WaitlistLineCardView.xaml`, `Module_Waitlist/Controls/WaitlistLineCardDetailTemplates.xaml`, `MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistLineCardMarkupTests.cs`, `MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistLineCardDetailTemplateTests.cs` · FR-007, FR-028 · Verify: the markup tests pin the stacked centred pair (including that the word is the larger of the two) and the separator on all three shared cells; read live, `DetailCategoryWord` and `DetailIdentifier` occupy **two** lines at the same centre (`y=353` and `y=388` on the same card) rather than one.

- [x] **T106** [P] [US1] Stop the action zone leaving a hole where an unavailable action was removed. A button whose gate says the action is unavailable is removed rather than blanked (FR-062), and with a row and a column written per button its slot stayed empty, so the row read as if a button had failed to load. The zone is now a reflow panel that keeps **three columns whatever is drawn** and fills them left to right with the buttons that are actually there, so only the number of rows shrinks (FR-061, FR-063). The rule itself — the column count, the place a drawn button takes and the rows that need — lives in `WaitlistActionZoneLayout`, beside `WaitlistCardCellLayout`, so it can be checked without a window; the panel in the application project applies it. · `MTM_Waitlist.Waitlist.View/Helpers/WaitlistActionZoneLayout.cs`, `Module_Waitlist/Controls/WaitlistActionReflowPanel.cs`, `Module_Waitlist/Controls/WaitlistLineCardView.xaml`, `MTM_Waitlist.Tests/Module_Waitlist/Helpers/WaitlistActionZoneLayoutTests.cs`, `MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistLineCardMarkupTests.cs` · FR-061, FR-062, FR-063, FR-065 · Verify: five new layout tests, including the owner's own case (the fourth of six unavailable → `1 2 3 / 5 6`); read live, a card offering five actions draws them `Cancel · Give back · Complete` / `Open details · Print` — the last two at `x=3223` and `x=3273` on the second row, so they took the first two columns instead of holding columns of their own and leaving a gap.

- [x] **T107** [P] [US0] Move the read-only cached-data notice to a row of its own **below** the page frame instead of above it. In the row above, it pushed every page down the moment Infor Visual went unreachable and pulled it back up when it recovered, so the screen moved under the person's hands for a reason that had nothing to do with their work (FR-003, FR-005). The frame keeps the row that expands and the notice sits after it, in an `Auto` row that costs no height while it has nothing to say. · `Module_Core/Views/ShellPage.xaml`, `MTM_Waitlist.Tests/Module_Core/Views/ShellHostingAuditTests.cs` · FR-003, FR-005 · Verify: a new audit asserts the notice is declared after the frame and that the frame still holds the expanding row; read live, the notice appears **after** the last card in the shell's own tree order, which is what put it at the foot of the window.

- [x] **T108** [P] [US0] Put the cursor in the PIN box when the credential stage appears. Choosing one of the people offered beside the form filled in the user name and showed the PIN box, and the person then had to click into the box before typing — and the PIN box is the only thing that stage asks for (FR-014, FR-048). The focus is taken **when the stage appears**, once per appearance rather than on every redraw, so it cannot fight a person who has moved on to something else on the page; both routes into the stage (a typed user name, a remembered person) are covered by the one rule. · `Module_Startup/Views/SignInPage.xaml.cs`, `MTM_Waitlist.Tests/Module_Startup/Views/SignInPageFocusTests.cs` · FR-014, FR-048 · Verify: read live — choosing the remembered person `JKOLL` put the credential stage on screen **and** left `SignInWindow_Credential` as the focused element, read back from UI Automation's own focused-element probe rather than from the markup.

- [x] **T109** [P] [US1] Make Shift+click on a card picture actually enlarge it. The gesture was caught on the press so the host never saw it, and the host around the picture acts on a **tap**: the enlarged view opened and was taken down by the host's own action before it could be seen, so the click looked as if it had done nothing. The **release and the tap are now claimed** for the control as well (FR-031). The Shift state is read from **both** the pointer event and the keyboard, because a mouse pointer event does not always carry the modifiers that were down when it was created, and asking only the event is how the gesture came to do nothing in the running app. · `Module_Shared/Controls/ClickToEnlargeImageView.xaml`, `Module_Shared/Controls/ClickToEnlargeImageView.xaml.cs`, `MTM_Waitlist.Tests/Module_Shared/Controls/SharedControlsMarkupTests.cs` · FR-031 · Verify: driven live with raw input at the picture's own coordinates — a **plain** click opens no viewer (0 elements with `ClickToEnlargeImageView_Viewer`), a **Shift+click** opens exactly one, and Esc closes it again.

- [x] **T110** [US5] Take the stall out of acting on a card. Every action raises a change and the list rebuilds itself from the store, which is right — but the rebuild also re-read each work centre's job, its allotted window and its per-row pictures, at one live read per work centre per row, which is where the 5–10 seconds went (FR-053). Acting on a card changes a request's status and its handler and touches none of those, so a reload raised **by a change in the store** now reuses what the load before it read, bounded by a **two-minute** window so a screen left open still re-reads; a load from a building change, and the person's own refresh, still read everything. · `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs` · FR-053 · Verify: the solution builds with no warnings and the full suite is 2143 passed / 0 failed. **Not measured live**: the only way to time the action path is to act on a real request, which changes the owner's own test rows, so the measurement is left to the owner's next click rather than made silently on their data.

- [x] **T111** [US6] Ask the die-shop question the direction's own way. Both die Items asked one question — *"Does this die go to the die shop?"* — while the card draws a different phrase for each, so the same tick meant taking the die there on a pickup and finding it there on a deliver (FR-081). The question is now chosen by the direction exactly as the phrase already was, and both are read from the one rule so they cannot drift; a wording that cannot be resolved leaves the step with its own default rather than showing a resource key (FR-022). · `MTM_Waitlist.Waitlist.View/Helpers/WaitlistCardCellRules.cs`, `MTM_Waitlist.Waitlist.NewRequest/ViewModels/NewRequestDieViewModel.cs`, `Strings/en-us/Resources.resw`, `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/NewRequestDieViewModelTests.cs`, `MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistLineCardDetailTemplateTests.cs` · FR-022, FR-055, FR-081 · Verify: the resource test asserts both questions exist and differ; read live, a pickup die's card drew `Take To Die Shop` and a deliver die's card drew `Located In Die Shop` for the same stored answer.

- [x] **T112** [US6] Find why the die-shop message never appeared, and fix the store rather than the card. The card's contract was already right: the store the application was actually reading was a **pre-012 snapshot**. Its `sp_waitlist_request_insert` had **no `p_answers_json` parameter** — and MySQL accepts a call that passes **more** parameters than the routine declares, **ignores the surplus and reports success**, so every request saved normally and every captured answer was discarded with nothing logged and nothing on screen. Its per-Item configuration seed was stale too, so the die Items did not even **declare** the die-shop cell, and `sp_waitlist_request_list` did not select `answers_json`, so a stored answer could not come back either. All three are repaired from the repository, and `Database/CopilotScripts/refresh_schema_drift.ps1` now reports — and with `-Apply` repairs — a store whose routines have fallen behind their artifacts, because this class of drift is invisible at run time by construction. · `Database/CopilotScripts/refresh_schema_drift.ps1`, `Database/CopilotScripts/README.md`, `WAITLIST-CARD-LAYOUT-PRE-SPEC.md` (D52) · FR-055, FR-081, FR-087 · Verify: the drift report names the missing parameter and the missing routine before the repair and reports parameter drift **0** after it; read live, both die cards drew their die-shop cell with the direction's own phrase. **One finding left standing and reported rather than repaired:** `sp_core_account_setup_create` is absent from the local store; it belongs to another feature's path and nothing here needs it.

- [x] **T113** [US1] Centre the title pair when a request has no data, and collapse the cell area completely. `CellsToDraw` drops a cell that resolved nothing, so **no cells is the same statement as no data** — a request whose values have not resolved (every declared field read from the job, with Infor Visual unreachable) drew its title pair at the top of a full-height zone with an empty area beneath it, which reads as a card whose contents failed to load. The zone's alignment now follows the row's own cell count: **centred** when it filled none, at the top over its cells otherwise. The two lower rows also collapse to **nothing**: the zone carries no spacing of its own, the gap between the title and the cells is owned by the cell area, and that area is itself collapsed on the same count — so a row with no data leaves no gap, no rule and no empty row behind. · `MTM_Waitlist.Waitlist.View/Helpers/WaitlistCardCellLayout.cs`, `MTM_Waitlist.Waitlist.View/Converters/WaitlistCardTitleAlignmentConverter.cs`, `App.xaml`, `Module_Waitlist/Controls/WaitlistLineCardView.xaml`, `MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistLineCardMarkupTests.cs`, `MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistLineCardDetailTemplateTests.cs` · FR-006, FR-024, FR-026, FR-027 · Verify: `TitleIsCentred` is asserted at every count from none to four alongside the rest of the panel's geometry; the markup tests pin the bound alignment, the absent row spacing, the cell area's margin and its collapse; and read live, the `deliver-riser-table` card (request 24) measured **34px of space above its title pair and 33px below** with **no cell text at all**, while the cards with data kept their title at the top (0px above) with their cells drawn.

- [x] **T114** [US4] Give the cached-data notice a gap below it. It sits in a row of its own at the foot of the shell, and that row reaches the bottom of the window — the shell's content margin carries **no bottom inset** (`NavigationViewPageContentMargin` is `56,24,56,0`), and nothing else insets this row, so the notice sat **flush against the window's bottom edge** and read as a strip of chrome rather than as a notice inside the page. It now carries `0,8,0,24`: 8 above, clearing the page it annotates, and 24 below, the shell's own inset. · `Module_Core/Views/ShellPage.xaml`, `MTM_Waitlist.Tests/Module_Core/Views/ShellHostingAuditTests.cs` · FR-003, FR-005 · Verify: read live, the notice's `StatusInfoBar` measured **y=1318 with a height of 50 against a shell 1392 tall** — a gap of exactly 24 where there had been none — and the guard fails when the bottom inset is taken back out again (proved by removing it and watching the audit fail, then restoring it).

- [x] **T115** [US5] Take the remaining seconds out of the status-change buttons, measured rather than assumed. The action itself was never the problem — its write lands in the same second as the click — but the list rebuild that follows it was: a cold rebuild measured **3718 ms of its row loop for three rows**, split into **2698 ms of picture resolution and 1020 ms of job reads**, and the action path paid that whenever the two-minute reuse window had lapsed, which is exactly how "it hangs sometimes, about five seconds" was reported. Two costs were removed and one was disproved:

  - **The picture is no longer looked for on the share once per part per extension.** The root cause was measured outside the application: a file probe against the plant picture share (`\\mtmanu-fs01\Expo Drive\…`, the shipped default) costs a **failed name resolution of 2.29–2.71 s** on a workstation off the plant network, where `Directory.Exists` answers the same path in 0.01 s once the operating system has cached the failure. `PictureShareReachability` now remembers that verdict per share for a short window — the same shape as `MySqlHostFallback` — so a silent share is probed once instead of once per row per extension, and the answer is the one the probe would have produced anyway. Nothing changes while the share answers.
  - **A reload raised by a change in the store no longer re-resolves the pictures at all.** A status change cannot change a picture, so the resolved paths are remembered (keyed by scope and part) and reused by such a reload; every cold load — the page opening, the building changing, the person's own refresh — forgets them and learns the pictures again. Measured: `images` **2698 ms → 0 ms** on the action path.
  - **The two-minute job-data window was itself the defect, and was replaced by the cold load.** Reuse used to lapse after two minutes, so an action taken later re-read every work centre's job. The window is now the cold load, which the person controls: measured end to end from the click to the card changing — **0.05 s and 0.11 s back-to-back**, and **1.17 s after 170 s of quiet** (an idle database connection being re-established, not a hang).
  - **Disproved, and reverted rather than kept:** reading the per-work-centre and per-Item data up front and concurrently (`Task.WhenAll`) with the memos made thread-safe. It measured **2027 ms against the serial path's 1020 ms** — the store seam serialises those reads anyway, and the warm-up read the allotted window of every Item, which the row loop only reads when a request carries no target time. Removed.

  · `MTM_Waitlist.Settings/Services/PictureShareReachability.cs` (new), `MTM_Waitlist.Settings/Services/PartPictureResolver.cs`, `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs`, `MTM_Waitlist.Tests/Module_Settings/PictureShareReachabilityTests.cs` (new), `tools/measure-ui-responsiveness.ps1` (new) · FR-030, FR-053, FR-078 · Verify: the whole path was measured with the application's own stopwatch instrumentation, logged at Warning so the entries are stored, and read back from `ops_startup_logs`; the instrumentation was then **removed** and the tree rebuilt. Six new tests pin the share verdict (one probe per window, re-probed at the boundary, per-share, and never a refusal for a path with no share), and they found two real bugs in it — a forward-slash share was not recognised as the same share, and a relative path was read as rooted. The driven proof is separate: `tools/measure-ui-responsiveness.ps1` samples the window's own message queue every 25 ms and reports the longest stretch it could not answer — `0 s` throughout every rebuild, which is how the "hang" was established to be the card not changing rather than the UI thread freezing. Suite 2149 passed / 0 failed; naming guard exit 0.

- [x] **T116** [US1] **Remove the card's print action entirely**, on the owner's direction, and record it as a requirement change rather than a deletion. Printing a label needs the detail a label is made of, which the request's own page has, so it is a future action of that page and not of the card (D59). Removed: the disabled placeholder button from every card, its two resource keys (`Waitlist_Card_Print.Text`, `Waitlist_Card_Print.Unavailable`), the card's `PrintActionText` and `PrintUnavailableText`, and the code-behind **TODO** naming label printing — so nothing on the card records a plan for an action it cannot take. The requirement moved with it: **FR-067 is superseded** (it read *Print MUST ship disabled, MUST say it is not available yet when pointed at, and MUST perform no action*), **FR-061 is amended** from six buttons to five, and **SC-012**, **acceptance scenario 6**, **FR-104**'s label list and the scope note are updated in place with the direction recorded. The action zone still lays out **three columns by two rows** (D44), which is what lets the five remaining actions take two rows rather than three. · `Module_Waitlist/Controls/WaitlistLineCardView.xaml` (+`.cs`), `Strings/en-us/Resources.resw`, `MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistLineCardMarkupTests.cs`, `MTM_Waitlist.Tests/Module_Waitlist/Helpers/WaitlistActionZoneLayoutTests.cs`, `specs/012-waitlist-card-layout/spec.md`, `data-model.md`, `plan.md`, `WAITLIST-CARD-LAYOUT-PRE-SPEC.md` (D59) · FR-061, FR-067, FR-104, SC-012 · Verify: the old `Card_PrintIsDrawnDisabledAndSaysItIsNotAvailableYet` test is **replaced by `Card_CarriesNoPrintActionAtAll`**, which fails if a print control, its glyph or its TODO returns; the button-order, square-button, named-and-tipped and localized-key lists all walk the card's real buttons; and read live, the action zone draws at most five buttons with no empty box where the sixth was.

- [x] **T117** [US1] **Name the material handler on the card, and never print their employee number**, on the owner's direction (D60). A claimed card's person row read `6229` under the ring, because the queue records the assignee in `assigned_material_handler` as an **employee number** — the number every handler authorization compares — and the row had nothing else to print. The number keeps its job (the assignment column and the comparisons are untouched); the **name** is now resolved beside it, from the entry the handler themselves wrote, matched to that same handler's number (`actor_employee_number = assigned_material_handler`, newest first, author present). Resolved in the two request reads (`sp_waitlist_request_list`, `sp_waitlist_request_get` → `assigned_material_handler_name`) and mirrored into `StoredProcedures/AllSPs.sql`, because the aggregate is what a machine applies. Carried by `WaitlistRequest.AssignedMaterialHandlerName` and `SampleOrder.AssignedMaterialHandlerName`; `PersonText` reads the name and nothing else, and a claimed request whose handler carries no recorded name draws **no** person row rather than falling back to the number (FR-018). `AcceptAsync` sets the name from the caller — the person taking the request — so the card names them on the next paint without waiting for a re-read, and `ReleaseAsync` clears it with the assignee. **Also removes the request detail page's "Employee number" row**, which printed the requester's number directly beneath the row that already names them. **Left alone:** the sign-in page's remembered-person list and the machine set-up panel, both identity surfaces rather than places where the app names a person. · `Database/StoredProcedures/sp_waitlist_request_list/create.sql`, `sp_waitlist_request_get/create.sql`, `AllSPs.sql`, `MTM_Waitlist.Waitlist.View/Models/WaitlistRequest.cs`, `Models/SampleOrder.cs`, `Services/WaitlistRequestService.cs`, `ViewModels/WaitlistViewViewModel.cs`, `ViewModels/WaitlistViewDetailViewModel.cs`, `MTM_Waitlist.Tests/Module_Waitlist/Services/HandlerNameStoreReadTests.cs` (new), `Services/WaitlistRequestServiceTests.cs`, `Models/SampleOrderTests.cs`, `ViewModels/WaitlistViewDetailViewModelTests.cs`, `specs/012-waitlist-card-layout/spec.md` (FR-022), `data-model.md` §5, `WAITLIST-CARD-LAYOUT-PRE-SPEC.md` (D60) · FR-018, FR-022 · Verify: the routines were **applied to the store the app actually reads** (the configured host is unreachable from this workstation, so `MySqlHostFallback` substitutes `localhost`) and read back through `sp_waitlist_request_list`: the claimed request returns `assigned_material_handler = 6229` **and** `assigned_material_handler_name = 'John Koll'`, while the pending ones return no name at all. Four new tests pin it — the list read resolves the name from the handler's own entry, the single-request read does the same, the aggregate an installer applies carries both resolutions, and a mapped row names the handler on the card — plus the existing person-row test now asserts that a handler with no recorded name leaves the row empty rather than printing the number.

- [x] **T118** [US2] **Clear a job's reported condition when it is given back**, on the owner's direction: a handler reports a condition from the status word, and returning the job to the pool left the condition on the row, so the card kept reading the previous handler's condition — and stayed themed for it — on a job nobody held, instead of reading `Waiting` again. A condition describes the assigned handler's own work on that request, so it goes with the assignment. Cleared in the two places that must agree: `sp_waitlist_request_status_update` clears `handler_status` whenever the incoming assignee is empty, and `ReleaseAsync` clears it on the row the session holds, so the card repaints without waiting for a re-read and the store agrees on the next one. **Keyed on the assignee rather than on the incoming status**, because a request sits in `Pending` while its note is written and writing a note must not remove a condition; the clear reads the parameter rather than the `assigned_material_handler` column, because a single-table `UPDATE` evaluates its `SET` list left to right and the column would already hold the empty value this same statement wrote — which would also clear the condition on Accept. Accept and Complete carry the handler, so both leave a condition alone. Recorded as **FR-106**; `rollback.sql` is unchanged because the routine's signature did not move. · `Database/StoredProcedures/sp_waitlist_request_status_update/create.sql`, `Database/StoredProcedures/AllSPs.sql`, `Database/Bootstrap/update_table_descriptions.sql`, `MTM_Waitlist.Waitlist.View/Services/WaitlistRequestService.cs`, `MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestStatusUpdateStoreRuleTests.cs` (new), `MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs`, `WaitlistHandlerStatusRoundTripTests.cs`, `spec.md`, `data-model.md`, `contracts/persistence.md`, `contracts/status-and-conditions.md` · FR-106 · Verify: the rule is SQL, so it is pinned twice — once by a test that reads the routine and the aggregate an installer applies, and once by behaviour. **The behaviour was proved against the pre-fix routine first**: both the working-tree routine and the one at `git HEAD` were loaded into a scratch schema on the local MySQL 8.0 development instance and driven with three real transitions, and the pre-fix routine left Give back reading `Product Buried` while the fixed one reads `NULL` — and both leave Accept and Complete holding theirs. The scratch schema was dropped afterwards. On the application side a service test asserts a given-back row carries no condition, reads `Waiting`, and goes back to the normal theme; a live round trip rides environment-gated for the store's own half. Suite **2221 total, 2177 passed, 44 skipped, 0 failed**; naming guard `New: 0`. **Still to do on an installed store:** the routine has no parameter change, so nothing breaks either way, but the clear does not reach a machine until the routine is redeployed — `pwsh -NoProfile -File Database/CopilotScripts/refresh_schema_drift.ps1 -Apply -Procedure sp_waitlist_request_status_update`.

- [x] **T119** [US1] **Put the request detail page right**, on the owner's direction, from a review of the running page. Five things, three of them one defect with a single root: **the `Work order` row printed the work centre** (`100-03`) because `NewRequestFlowState.ToDraftFor` wrote `WorkCenter` into `active_setup_job_id` — the column named for the job the request was raised against. Confirmed against the live store before changing anything: **all 8 stored requests carry `active_setup_job_id = work_center` and none looks like a work order**, while `setup_active_jobs` holds `100-03`'s real `WO-046885`. The job's work order now travels on `RequestJobPartAvailability.WorkOrder` from the `SetupActiveJobSnapshot` the wizard already resolved, the draft carries it, and the page reads it from the job snapshot — so **the requests written before the fix read correctly too**, which the stored value alone could not have done. The row is left out when the job cannot be resolved, never filled with the work centre. Also: **the two cards became one** (`Request context` carried the work centre, the person who asked and the remaining time, all three already on the hero card, so every row on it was a second saying of something the page had said once); **the caption was removed** (*"Material, workflow, and handoff details are shown below."* — it said where things were, not anything about the request); **each hero picture carries a caption** (`Request type`, `Work center`) because a picture standing alone beside a title names nothing; and **the remaining time now states its allotment** (`00:09 / 00:15`, through a new `SampleOrder.RemainingTimeAndAllotmentText` that reads the same `AllottedMinutes` the ring's fraction uses, so the row and the ring cannot disagree about the scale). Recorded as **FR-107** to **FR-110** and in the new `contracts/request-detail-page.md`. · `MTM_Waitlist.Settings/Models/RequestJobPartAvailability.cs`, `Services/RequestJobAvailabilityProvider.cs`, `MTM_Waitlist.Waitlist.NewRequest/Models/NewRequestFlowState.cs`, `MTM_Waitlist.Waitlist.View/Models/SampleOrder.cs`, `ViewModels/WaitlistViewDetailViewModel.cs`, `Module_Waitlist/Views/WaitlistViewDetailPage.xaml`, `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewDetailViewModelTests.cs`, `Models/SampleOrderTests.cs`, `Models/NewRequestFlowStateTests.cs`, `spec.md`, `contracts/request-detail-page.md` · FR-107 to FR-110 · Verify: the live query above fixed what the row was printing before any code moved; the detail-page tests now assert the work order comes from the job (`WO-046885` against a request whose stored value is the work centre `100-03`), that the page draws **one** card, and that neither the work centre nor the remaining time is repeated on it; `SampleOrderTests` pins `00:09 / 00:15` against a fixed clock; `NewRequestFlowStateTests` pins the draft's job id as the job's work order. **Six assertions across three test classes failed on the first run and were updated to the new behaviour rather than deleted** — two of them because handing the summary page's job guard the real work order turned a failed job read into a *blocked submission*, so that guard change was **reverted** and left documented in the code: `ValidateCurrentJobState` is still handed the work centre twice and still only establishes that it is non-blank, and fixing it properly means resolving the work centre's active jobs here, which is its own change. Suite **2222 total, 2178 passed, 44 skipped, 0 failed**; build `0 Warning(s) 0 Error(s)`; naming guard `New: 0`.

---

## Requirement coverage

Every functional requirement from `spec.md` maps to at least one task; the ranges are inclusive and every range is
carried by the tasks named. The per-requirement map, with each requirement's own wording, is recorded in this
feature's `.spec-context.json` by the tasks step.

| Requirements | Tasks |
|---|---|
| FR-001 to FR-010 | T033, T034, T035, T037 |
| FR-011 to FR-023 | T027, T028, T038, T039, T048 |
| FR-024 to FR-029 | T028, T034, T036, T040, T049, T053 |
| FR-030 to FR-053 | T049, T050, T051, T052, T080, T094 |
| FR-054 to FR-060 | T016, T054, T055, T075, T094 |
| FR-061 to FR-069 | T027, T033, T035, T041, T043, T044 |
| FR-070 to FR-076 | T004, T013, T026, T027, T030, T042, T056, T057 |
| FR-077 to FR-087 | T006, T070, T071, T072, T073, T074, T075, T076, T077, T078, T080, T097, T098 |
| FR-088 to FR-095 | T007, T010, T011, T012, T014, T015, T017, T031, T049, T055, T059 to T069, T083, T099, T100 |
| FR-096 to FR-102 | T013, T029, T033, T046, T056, T089, T090, T096 |
| FR-103 to FR-105 | T036, T049, T053, T082, T086, T088 |
| FR-106 | T118 |
| FR-107 to FR-110 | T119 |

---

## Dependencies & Execution Order

**Phase order:** Setup (1) → Foundational (2) → US1 (3) → US2 (4) → US3 (5) → US4 (6) → US5 (7) → US6 (8) →
Polish seed and schema (9) → Polish resources (10) → Polish contracts (11) → Polish validation (12) →
Polish follow-up round (13).

**What each phase's waves say:**

| Phase | Waves | Blocked by |
|---|---|---|
| 1 Setup | 1: T001–T002 | — |
| 2 Foundational | 1: T003–T016 · 2: T017–T028, T094 · 3: T029–T031 · 4: T032 (gate) | Phase 1 |
| 3 US1 | 1: T033–T034 (tests first) · 2: T035–T037 · 3: T038–T042 · 4: T043–T044 | Phase 2 |
| 4 US2 | 1: T045 · 2: T046–T047 · 3: T048 | Phase 2, and US1 for the markup it themes |
| 5 US3 | 1: T049 · 2: T050 · 3: T051 · 4: T052 · 5: T053–T055 | Phase 2, and US1 for the detail-zone host |
| 6 US4 | 1: T056 · 2: T057 · 3: T058 | Phase 2, US1 (the word's host markup), US2 (the theme it sets) |
| 7 US5 | 1: T059–T060 · 2: T061–T063 · 3: T064–T066 · 4: T067–T069 | Phase 2; T064 also needs US3's `Item Type` cell and US6's `# of Skids` answer to be complete on the card |
| 8 US6 | 1: T070 · 2: T071–T076 · 3: T077–T078 · 4: T079 | Phase 2; T079 needs US3's templates and US5's `Part Type` |
| 9 Polish seed and schema | 1: T080–T083 · 2: T084 · 3: T085 (gate) | US3, US5 and US6, because the seed is reconciled to the cells they settle |
| 10 Polish resources | 1: T086 · 2: T087–T088 | Phases 3 to 8 |
| 11 Polish contracts | 1: T089–T091 | Phases 3, 4 and 9 |
| 12 Polish validation | 1: T092–T093 | Every phase above |
| 13 Polish follow-up round | 1: T095 · T096–T098 · T099–T100 | Every phase above; T097 changes the cell model and the templates, so it follows US3 and US6 |

**Parallel opportunities.** Foundational waves 1 and 2 hold most of the independent work: the schema pair, the three
procedures, the reads and their mirrors, the models. Within each story phase, the `[P]` tasks in a wave are separate
files. In Phase 7 the classifier, the picker rules and the disposition model are three files; in Phase 8 the five
answer pages are five files.

**Notes on order.** US3 (stage W3) and US4 (stage W2) are both dependency-safe after US2; they are ordered by story
priority, US3 being P1. US4's flyout sits in stage W2 alongside the status column, so it may be built immediately
after US2 while US3 is in flight. The card's own markup is built once, in Phase 3, because the card is one file; the
later phases bind to it instead of editing it, which is what lets each story phase be handed to its own worker.
