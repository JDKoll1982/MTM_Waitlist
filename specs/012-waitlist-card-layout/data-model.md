# Phase 1 Data Model: Waitlist Line Card Layout

This feature adds no table. It adds two columns to `waitlist_requests_queue`, one stored procedure, one
mirrorable Infor Visual read shape plus its mirror population, one corrected Infor Visual input, one live-only
fuzzy search, extensions to four item models, a reshaped row model, and two new view models. The model below is
divided the way the work is: the persisted state, then the shapes the card resolves and draws, then the per-Item
cell assignment that ties an Item's declaration to what the card shows.

## 1. Persisted state

### WaitlistRequestRow

The queue row, as extended. Everything below is in `Database/Tables/18_waitlist_requests_queue/create.sql`.

Existing columns the card reads: `public_id`, `work_center`, `work_center_name`, `category`, `item`,
`input_value`, `requester_employee_number`, `requester_employee_name`, `status`, `requested_utc`,
`target_time_utc`, `is_overdue`, `assigned_material_handler`, `canceled_utc`, `canceled_by_employee_number`,
`accepted_utc`, `completed_utc`.

Two new columns:

| Column | Type | Null | Meaning |
|---|---|---|---|
| `handler_status` | `VARCHAR(32)` | yes | The condition the row's own material handler reported, or NULL. One of the four verbatim conditions. |
| `answers_json` | `JSON` | yes | The answers the request captured beyond the single `input_value`, keyed by the label the Item declared. |

Rules:

- `handler_status` is separate from `status` on purpose. The lifecycle word underneath a condition is still
  needed, because the card has to know it when no condition is set (FR-012, research R4).
- `handler_status` is cleared with the assignee: a condition describes the assigned handler's own work on the
  request, so returning the request to the open pool removes it and the card reads the lifecycle word again
  (FR-106). The clear is keyed on the incoming assignee rather than on the status, because a request sits in
  `Pending` while its note is written and a note must not remove a condition.
- `answers_json` exists because `input_value` holds exactly one answer and the die Items now ask two questions,
  while the four returning Items each ask one. The map is a label-to-value object, the same shape the schema
  already uses for structured declaration data (`detail_fields_json`, `options_json`) (research R5).
- The card writes `handler_status` and no other column. The lifecycle columns are read-only to this feature,
  and the existing lifecycle writes stay where they are.
- Both columns are nullable with no default. A row written before this change reads as NULL for both, which is
  the same as "no condition" and "no extra answers" rather than a special case.
- The table ships `create.sql` and `rollback.sql` in the same change, and the aggregate/description files move
  with it (research R4, plan W7).

### Stored procedures

| Procedure | Change | Contract |
|---|---|---|
| `sp_waitlist_request_handler_status_update` | New | Gated on the expected handler, sets `handler_status`, reports the affected row count so a lost race is told rather than shown as success (FR-070, FR-076). |
| `sp_waitlist_request_list` | Changed | Returns `handler_status` and `answers_json` alongside the columns it already returns (FR-073). |
| `sp_waitlist_request_insert` | Changed | Carries `answers_json`, so a new request's answers are stored with it (FR-073, FR-087). |

Every one of these is a stored procedure because application-side statement text is banned
(`InlineSqlAuditTests`). The update seam reports affected rows, matching `sp_waitlist_request_status_update`.

## 2. HandlerStatus

The condition a handler reports from the card.

- **Values**, verbatim: `Product Buried`, `Cant Find Product`, `Changing Forklift`, `Changing Propane`. Nothing
  else is a condition.
- **Storage**: `waitlist_requests_queue.handler_status`, NULL when unset.
- **Write gate**: only the row's own material handler may set it (FR-070, FR-076). The signed-in person is
  compared with the row's `assigned_material_handler`, and the procedure is gated on that same value, so a
  client that bypasses the card's own check cannot write somebody else's row.
- **Validation**: a stored value that is not one of the four verbatim strings, or that is NULL, is not a
  condition and the status word falls through to the overdue marking or the lifecycle word.
- **Transitions**: `None` to a condition is the only transition the card offers for an unset row. Choosing a
  condition on a row that already carries one replaces it, because the flyout writes the chosen value. The
  feature defines no path that clears a condition back to none.
- **Restoration**: the value is read back by `sp_waitlist_request_list` and restored onto the same row when the
  list is next read, so it survives a restart (FR-073, SC-016).

## 3. CardStatusWord

What row 1 of the status zone reads, and which theme the whole column takes.

- **States**: eight. The four lifecycle words `Waiting`, `In Progress`, `Done`, `Cancelled`; the overdue marking
  `Overdue`; and the four conditions listed above (FR-012, FR-102).
- **Precedence**, in order: the handler condition when one is set, otherwise `Overdue` when the request is
  overdue, otherwise the lifecycle word (FR-012, FR-023). A condition and the overdue marking are never both
  shown, and the condition wins for as long as it is set.
- **Lifecycle mapping**, from the stored status: `Pending` to `Waiting`, `Accepted` to `In Progress`,
  `Completed` to `Done`, `Canceled` (either spelling) to `Cancelled`. An unrecognised stored status yields no
  word, and an empty status row is removed rather than left blank (FR-018).
- **Themes**: three. Normal, handler condition as a lighter red, and overdue (FR-019, FR-072).
- **Transitions**: the word changes only as its inputs change, which is the lifecycle status, the overdue flag,
  and the condition. Nothing on the card writes the first two. The lifecycle transitions the words reflect are
  unchanged by this feature: `Pending` to `Accepted` on accept, `Accepted` to `Completed` on complete,
  `Accepted` back to `Pending` on give back, and `Pending` or `Accepted` to `Canceled` on cancel.
- **Verbatim constraint**: the eight strings are pinned by the spec and are used exactly (FR-104).

## 4. TimeRing

Row 2 of the status zone.

- **Control**: the platform `ProgressRing` in its determinate state, read-only, with the time text overlaid
  (research R2).
- **Inputs**: `Elapsed` and `Allotted`, from which `Value` is derived over the ring's `Minimum` and `Maximum`
  (FR-013, FR-017).
- **Allotted**: the Item's configured `allotted_minutes`, or the labelled fifteen-minute default when the Item
  configures none. `assist-table-remove` is the seeded case with no allotment, so the default is exercised by
  shipped data.
- **Measure**: time left while the request is live, time taken once it is closed (FR-015). The ring states which
  of the two it is, and its accessible name says the same (FR-016).
- **Transitions**: live to closed, at which point the ring freezes at the moment the request closed and keeps
  the time taken (FR-015). An overdue live request shows the time overrun in the ring (FR-014).
- **Tick**: the ring re-evaluates on the same one-minute tick that refreshes the row's remaining-time text, so
  the two never disagree (FR-017). The existing `RemainingTimePalette` is retained and reads the same fraction.
- **Validation**: no percentage and no elapsed-time line are drawn (FR-021).

## 5. PersonRows

Rows 3 and 4 of the status zone (FR-011, FR-022).

- **Unclaimed**: row 3 reads the requester.
- **Claimed**: row 3 reads the material handler.
- **Closed**: row 3 reads the material handler, or the person who cancelled, and row 4 reads the requester.
- **Named, not numbered**: both rows carry a person's **name**. The queue records the assignee in
  `assigned_material_handler` as an employee **number**, because that number is what the handler authorizations
  compare; the name is resolved from the entry that handler wrote (`actor_employee_name`, matched to
  `actor_employee_number = assigned_material_handler`, newest first) and returned beside the number by
  `sp_waitlist_request_list` and `sp_waitlist_request_get` as `assigned_material_handler_name`.
  `SampleOrder.AssignedMaterialHandlerName` carries it; `PersonText` reads it and nothing else.
- **Validation**: a person row with no value is removed rather than left blank (FR-018). An unclaimed request
  with no handler therefore draws no empty row, and neither does a claimed request whose handler carries no
  recorded name — the number is never printed as a substitute, because an employee number is an identifier and
  not a name.

## 6. ItemDeclaration and DetailField

`waitlist_request_item_configs` is unchanged as a table; what changes is the declaration data it holds for
sixteen of the twenty-four Items (section 9).

- **ItemDeclaration**: one row per Item. Carries `item`, `category`, `control_flow`, `requires_answer`,
  `answer_value_type`, `prompt_text`, `min_length`, `max_length`, `options_json`, `detail_fields_json`,
  `allotted_minutes`. One row per Item, enforced by `uq_waitlist_request_item_configs_item`; a missing row is a
  reportable state rather than a silently merged one.
- **DetailField**: one element of `detail_fields_json`, in declared order. Carries `label`, `value_type`,
  `source` (`job`, `fixed` or `answer`), `order`, `is_required`, and, for answer-sourced enum fields, `list`,
  which names the requesting job's list the choices come from.
- **Rules**: `order` lives inside the JSON payload and no bare `order` column exists. The label a cell shows is
  the label the declaration carries, never a card-side override, which is why the seed's labels are corrected
  where they disagree (FR-059). An Item's declaration is read live from the internal store.

## 7. CardRow (SampleOrder) and DetailCell

### CardRow

`MTM_Waitlist.Waitlist.View/Models/SampleOrder.cs`, one instance per waitlist row. It already carries the
identity, lifecycle, timing, picture and gate values. It gains:

- **`ItemCode` usage**: the row's Item decides its cells, its template and its Category word; the cells are never
  chosen per row (FR-025).
- **`HandlerStatus`**: the restored condition, or null.
- **`CanSetStatusCondition`**: true only when the signed-in person is the row's own material handler (FR-070).
- **`StatusWord`** and **`StatusTheme`**: the resolved word and its one of three themes.
- **`RingValue`**, **`RingMeasure`**: the ring's fraction and which of time left or time taken it measures.
- **`Cells`**: an `ObservableCollection<WaitlistLineCardCell>`, in declared order (FR-026).
- **Person rows**: the handler row's value and the requester row's value, each absent when empty.
- **Action gates**: one gate per action button. Long-standing gates stay (`CanAccept`,
  `CanCompleteOrRelease`, `CanCancelRequest`); give back and open details read their own. There is no print gate, because the card carries no print action (FR-067, D59): printing is the request detail page's later
  unavailable (FR-062, FR-067).
- **`AllottedMinutes`**, **`ClosedUtc`**: the ring's two extra inputs.

### DetailCell

`MTM_Waitlist.Waitlist.View/Models/WaitlistLineCardCell.cs`, new. One resolved label-and-value cell.

- **Label**: a localized resource key resolved at bind time, never a literal (FR-104).
- **Value**: the resolved text, or empty.
- **HasValue**: false when nothing resolved. A cell with no value is not added to `Cells` at all, so nothing is
  padded and no empty cell is drawn (FR-006, FR-018).
- **IsLabelLess**: true for the die-shop cell only, which draws its phrase as the value with no label on top
  (FR-028, FR-055).
- **Order**: the declared order, so the grid fills in declaration order.
- It is a distinct model from the existing `WaitlistField`, which the request detail page uses.
- **Resolution**: a cell is produced for each declared field the Item's assignment includes, minus whatever the
  card excludes (section 8). Its value comes from the job snapshot, the Item's captured answer, the request
  itself, or a read.

## 8. PanelGeometry

The detail zone is an inner grid of three rows and two columns; row 1 is the title pair and always spans both
columns (FR-024, FR-007).

| Cells the Item fills | Geometry |
|---|---|
| 4 | two rows of two |
| 3 | cells 1 and 2 in the top row, cell 3 spanning the bottom row |
| 2 | two cells, bottom row collapsed |
| 1 | the single cell spanning the full width |
| 0 | both lower rows removed, the title row taking the vacated space as one full-width row |

- **Validation**: the geometry follows the number of cells, so two requests of the same Item always render the
  same shape and the same height, and an Item with fewer cells is shorter rather than padded (FR-009, FR-026,
  FR-027, FR-103).
- **Template**: the per-Item arrangement is one `DataTemplate` per Item in a single `ResourceDictionary`,
  chosen by a `DataTemplateSelector` keyed on the row's Item (research R3). Identity, status and action stay in
  the card's own markup; only the detail column is templated.

## 9. Per-Item cell reconciliation

Section 5 of `docs/research/research-waitlist-card-layout-pre-spec.md` governs the cells. The seed
(`Database/Seeds/seed_waitlist_request_item_configs/create.sql`) is brought into line only where the card must
show something the seed does not declare, or declares under another label. The seed's declared fields are read
from that file; the work-centre fields it declares for many Items are never cells (FR-005).

| Item | Seed declares today | Cells the card requires | Reconciliation action |
|---|---|---|---|
| `pickup-coil` | Coil number, Quantity in house, Coil description, Average coil weight, Requesting work center | Coil number, Quantity in house, Coil description, Average coil weight | No label change. One label correction is not needed here: the resolver gains an arm onto `CoilPartNumber` for `Coil number`, `Coil description` comes from the extended coil read, `Average coil weight` is read live from the receiving store's `receiving_history` through `sp_receiving_history_average_coil_weight` via `IAverageCoilWeightService`, and `Quantity in house` from the new quantity shape. |
| `pickup-die` | Die, Pickup location, Quantity | Die, `Die Location`, plus the label-less die-shop cell when the answer calls for it | Label correction: `Pickup location` to `Die Location` (FR-059). Drop `Quantity` from the card (FR-056). Add the answer-backed die-shop flag that gates the third cell (FR-081). |
| `pickup-component` | Component, Part description, Quantity, Requesting work center | Component, Total Qty In House, Item Type (WIP or Finished Goods) | Add cell `Total Qty In House` (read) and cell `Item Type` (derivation). Drop `Part description` and `Quantity` from the card. |
| `pickup-fg` | Part number, Quantity remaining, Part description, Customer, Packlist | Part Type, # of Skids | Drop all five declared fields from the card (FR-056). Add cell `Part Type` (derivation) and answer-backed cell `# of Skids` (FR-087). |
| `pickup-ncm` | Part, Quantity to move, Pickup location, Destination, Traceability ID | Part Type, # of Skids | Drop the five declared fields from the card (FR-056). Add cell `Part Type` (location-derived for NCM) and answer-backed cell `# of Skids`. |
| `pickup-wip` | Part and quantity, Pickup work center, WIP destination, Operation sequence | Part Type, # of Skids | Drop the four declared fields from the card. Add cell `Part Type` (derivation) and answer-backed cell `# of Skids`. |
| `pickup-outside-service` | Part, Quantity to move, Pickup location, Destination, Traceability ID | Part Type, # of Skids | Drop the five declared fields from the card. Add cell `Part Type` (derivation) and answer-backed cell `# of Skids`. |
| `pickup-riser-table` | Requesting work center | none, the panel collapses | No declaration change. The one declared field is not a cell (FR-005). |
| `pickup-dunnage` | Dunnage part, Description, Home location, Quantity on hand | Dunnage part, `Home Location` | Label correction: `Home location` to `Home Location` (FR-059). Drop `Description` from the card: it is declared but provably unfillable, so the declaration is left in place and the cell is never drawn (FR-095, research R8). Drop `Quantity on hand` from the card. |
| `pickup-scrap` | Scrap type, Quantity involved, Scrap lugger, Pickup work center | Scrap Type, Lugger Location | Label correction: `Scrap type` to `Scrap Type`, which is also the label the resolver's new arm answers. Add declared cell `Lugger Location`: no resolver arm exists for it today, so the cell is hidden and a future resolver arm reveals it with no card change (FR-058, research R1). Drop `Quantity involved` and `Scrap lugger` from the card. |
| `pickup-hopper` | Pickup work center | Size | Add answer-backed cell `Size`. The declared work-centre field is not a cell (FR-005, FR-077). |
| `deliver-coil` | Coil number, Quantity in house, Coil description, Average coil weight, Requesting work center | Coil number, Quantity in house, Coil description, Average coil weight | No label change. Same sourcing as `pickup-coil`: `Coil description` from the extended coil read, `Average coil weight` from the receiving store. |
| `deliver-flatstock` | Part, Quantity, Work center, Destination | Part, Quantity, Destination | Add cell `Quantity` (read). `Destination` keeps its declared label and resolves from the request's requesting work centre, which is the repository's documented deliver rule, so nothing asks for it and it does not rejoin the answer token set (research R1, FR-042, FR-057). Drop `Work center` from the card. |
| `deliver-component` | Component, Part description, Quantity, Requesting work center | Component, Total Qty In House, Item Type (WIP or Finished Goods) | Add cell `Total Qty In House` (read) and cell `Item Type` (derivation). Drop `Part description` and `Quantity` from the card. |
| `deliver-die` | Die, Pickup location, Quantity | Die, `Die Location`, plus the label-less die-shop cell when the answer calls for it | Label correction: `Pickup location` to `Die Location` (FR-059). Drop `Quantity` from the card. Add the answer-backed die-shop flag. |
| `deliver-dunnage` | Dunnage part, Description, Home location, Quantity on hand | Dunnage part, `Home Location` | Label correction: `Home location` to `Home Location`. Drop `Description` from the card with the declaration left in place (FR-095). Drop `Quantity on hand` from the card. |
| `deliver-wrong-coil` | Coil number, Reason, Correct coil | Coil number (the incorrect coil, typed through the fuzzy search), `Correct coil` | Source correction: `Coil number` from `job` to `answer`, backed by the live fuzzy part search (FR-082). Drop `Reason` from the card (FR-056). The resolver gains an arm onto `CoilPartNumber` for `Correct coil`. |
| `deliver-wrong-flatstock` | Part, Reason, Correct flatstock | Part (the incorrect flatstock, typed through the fuzzy search), `Correct flatstock` | Source correction: `Part` from `job` to `answer`, backed by the live fuzzy part search. Drop `Reason` from the card. The resolver gains an arm onto `FlatstockPartNumber` for `Correct flatstock`. |
| `deliver-riser-table` | Requesting work center | none, the panel collapses | No declaration change. The one declared field is not a cell (FR-005). |
| `deliver-hopper` | Requesting work center | Size | Add answer-backed cell `Size`. The declared work-centre field is not a cell (FR-005, FR-077). |
| `assist-coil-turn` | Coil number, Quantity in house | Coil Number, Weight of Coil | Label correction: `Coil number` to `Coil Number` (FR-050). Drop `Quantity in house` from the card. Add required, answer-backed cell `Weight of Coil`, which is the weight the operator supplies and is deliberately not the job's average coil weight (FR-054, FR-086). |
| `assist-table-place` | Part, Quantity, Work center, Destination | none, the panel collapses | No declaration change. |
| `assist-table-remove` | Part, Quantity, Work center, Destination | none, the panel collapses | No declaration change. This row carries no configured allotment, so its ring uses the labelled fifteen-minute default. |
| `other` | Request description, Requested work center | Request description | No declaration change. The description is drawn in full and scrolls rather than clipping when it is long (FR-053). The requested work centre is not a cell (FR-005). |

Seed-wide rules that follow from the table:

- Every declared field the card does not show is left declared and simply excluded from the cells, which is
  what FR-056 requires and what keeps the exclusion a card rule rather than a silent configuration edit.
- The two `Destination` declarations on `pickup-ncm` and `pickup-outside-service` are not cells, so no third
  location label arises from them (FR-057).
- The seed change is one change with its seed aggregate, its rollback and the table-description upkeep (W7).

## 10. RequestAnswer

A value the requester supplied when the request was raised.

- **Storage**: the Item's single required answer stays in `input_value` (the die choice, the component choice,
  the dunnage choice, `other`'s description, and both wrong-part Items' `Reason`); every further answer goes
  into `answers_json` under the label the Item declared.
- **Items that capture one**: both hopper Items (`Size`), both die Items (the die-shop flag), both wrong-part
  Items (the incorrect part, typed through the fuzzy search), `assist-coil-turn` (`Weight of Coil`), and the four
  returning Items (`# of Skids`).
- **Validation**:
  - `Size` is one of `Small`, `Medium` or `Large`, chosen as three radios, with the chosen size's reference
    image drawn beneath them (FR-077). The image follows the application's existing picture rules, and a missing
    or unusable image draws the shared placeholder rather than a blank (FR-079, FR-080).
  - The die-shop answer is a checkbox stating whether the die is at the die shop or must be sent there, and it
    is what decides whether the card draws its third, label-less cell (FR-081).
  - The incorrect part is not free text. It is typed through a fuzzy search over a live plant-system read
    (FR-082, FR-084).
  - `Weight of Coil` is required and numeric (FR-086). It is not the job's `Average coil weight` (FR-054).
  - `# of Skids` is numeric, defaults to one, and is not required to be changed (FR-087).
  - An answer that is blank is not written, so no cell is added for it (FR-006).
- **Persistence**: `answers_json` is carried by `sp_waitlist_request_insert` and returned by
  `sp_waitlist_request_list`, so an answer survives the request and is restored on its card (FR-073).

## 11. FuzzyPartSearch

The live-only read behind the two wrong-part Items.

- **Read**: `Database/InforVisual/Queues/Module_Waitlist/Queries/GetPartMatches.sql`, new.
- **Behaviour**: a term matching a part exactly resolves at once; a term matching none exactly offers every part
  whose number contains the term (FR-083).
- **Input rule**: the term is upper-cased and zero-padded to ten characters when it starts with a known prefix
  (FR-082).
- **Live only**: the read is never cached and has no mirror declaration, because a contains-search over every
  part is not a mirrorable shape (FR-084, research R7).
- **Unreachable plant system**: the operator is told the search is unavailable and the typed value is used as
  entered. That is the designed behaviour, not an error path (FR-085).

## 12. Read shapes

| Shape | Kind | Holds | Mirror |
|---|---|---|---|
| `GetPartWarehouseQuantities.sql` | New Infor Visual read | The part-wide quantity, summed over locations and filtered by location class. Feeds `Quantity in house` on the coil Items, `Quantity` on `deliver-flatstock`, and `Total Qty In House` on the component Items (FR-090, research R6). | Mirrorable, with a population under `Module_Mock/Populations`. |
| `GetDispositionInput.sql` | Corrected Infor Visual read | Stops passing `PART.QTY_ON_HAND` and passes a sum filtered to finished-goods locations, matching the rule the classifier already documents. The classifier's priority is untouched (FR-090, research R8). | Unchanged. |
| `GetPartMatches.sql` | New Infor Visual read | The fuzzy part search rows (section 11). | None, deliberately. |
| `GetWipFloorQuantities.sql` | Extended WIP-application read | NCM and item-type buckets, which is the floor cross-check the WIP case of `Total Qty In House` reads (research R6). | Unchanged. |

- **Validation**: an Infor Visual read is the only kind that uses the cached fallback; the handler status, the
  answers and the dunnage home location are live internal reads and writes (FR-089, research R9). A reachable
  but empty live result is a real answer and is never replaced by cached data.

## 13. JobSnapshot extensions

`MTM_Waitlist.Settings/Models/RequestJobPartAvailability.cs` is the requesting job's material, and it is the
source most of the job-sourced cells resolve from.

- **Gains**: the coil's description, so `Coil description` resolves from the snapshot rather than from a second read
  (research R1). No Visual weight rides the snapshot: `Average coil weight` is read live from the receiving store's
  `receiving_history` through `sp_receiving_history_average_coil_weight` via `IAverageCoilWeightService`.
- **Gains**: the dunnage home location, so `Home Location` resolves for the dunnage Items.
- **Gains**: the four returning-pickup gates, which are the real job-work signals that replace the hard-coded
  out-of-scope list.
- **Already carries**: `CoilPartNumber`, `FlatstockPartNumber`, `ScrapType`, `ComponentPartNumbers` and
  `DunnageParts`. The resolver gains plain arms onto these for `Coil number` / `Coil Number`, `Correct coil`,
  `Correct flatstock` and `Scrap Type`, which are values the snapshot holds today and the resolver returns null
  for (FR-088, research R1).
- **Does not carry**: any quantity, `Destination` or `Item Type`. Those are read, derived or captured instead,
  and nothing is invented to fill the gap (FR-089).
- **Resolver rule**: an answer captured under a declared label wins over the job arm for that label, which is the
  case the two wrong-part Items need for their incorrect part.

`MTM_Waitlist.Settings/Models/RequestDunnagePart.cs` gains a location, so `Home Location` has a value on the
snapshot rather than only in the store.

## 14. RequestDisposition

The derivation behind `Item Type` and the four returning Items' `Part Type`.

- **Kind**: `Finished Goods`, `Outside Service` or `WIP`, derived with the priority Outside Service, then
  Finished Goods, then WIP (FR-091, FR-092). Outside Service comes from the subcontracted operation, WIP from an
  open step earlier than the routing's last, and Finished Goods only when the part is actually at a
  finished-goods location.
- **Gains `Ncm`**: a fourth value derived from the location the stock occupies, never from the part's recorded
  item type, and never treated as a production type (FR-093, research R1).
- **Consumer**: the card path. The stack is built and registered today and has no consumer outside its own tests;
  wiring it to the card is what gives it one, rather than writing a second derivation.
- **The four returning Items**: offered when the requesting job has the work they name, and no longer hidden by
  a fixed list (FR-094). The hard-coded out-of-scope array is deleted in favour of four real job gates.

## 15. CardAction

The action zone's model.

- **One entry per action**: Accept, Cancel, Give back, Complete, Open details, laid out in row-major
  order over three columns and two rows (FR-061, D44: Accept, Cancel and Give back across the top).
- **Gate**: each entry is drawn only while the row's own gate says the action is available, and no gate changes
  because the layout changed (FR-062).
- **Validation**: an unavailable entry is removed rather than blanked and the rest re-centre without stretching
  (FR-063). Every entry keeps a tooltip and an accessible name from the row's localized text (FR-064) and a hit
  target of at least 44 by 44 (FR-065). Accept, Complete and Give back keep distinct colours and glyphs (FR-066).
- **Print**: not on the card at all. It was a placeholder that performed nothing and read as an empty box, and it was removed on the owner's direction (FR-067, D59): printing belongs to the request detail page, later.
- **Open details**: the only way the card opens the request; the card body no longer does (FR-068, FR-069).

## 16. Relationships

- One `WaitlistRequestRow` has one `handler_status` and one `answers_json`, and belongs to exactly one Item.
- One Item has exactly one `ItemDeclaration`, and the declaration has many `DetailField` entries.
- One `CardRow` is built from one `WaitlistRequestRow` and one job snapshot.
- One `CardRow` has many `DetailCell` entries, one per declared field its Item's assignment includes, in
  declared order, and its `PanelGeometry` is decided by that count alone.
- One `CardRow` has one `CardStatusWord`, one `TimeRing`, up to two person rows, and up to six `CardAction`
  entries.
- One job snapshot is the source of the job-sourced cells; the read shapes of section 12 supply the rest; the
  request's own `answers_json` supplies the rest again.

## 17. Cross-cutting validation rules

- Nothing is padded to fill a short panel, and no universal fallback value is invented (FR-006).
- An empty status row, an empty cell and an unavailable action are removed rather than blanked (FR-018,
  FR-063).
- A cell the Item never declares is hidden; a cell whose source exists but cannot be read is hidden and also
  writes a warning to the database log, and neither case is explained on the card or filled with a substitute
  (FR-058, FR-060, FR-101).
- The requesting work centre and the requesting user are never detail cells (FR-005).
- Two requests of the same Item render identically in shape and height (FR-103).
- Every new label, the ring's accessible name, `Open details`, the new cell labels, the two
  die-shop phrases, the four conditions and the eight status words are localized resource keys, never literals
  (FR-104).
- Any animation the card adds respects the system's animations-off setting (FR-105).
