# Contract - The schema and procedure seam this change adds or reshapes

The persistence surface behind `012-waitlist-card-layout`: one reshaped table, one new stored procedure, the three
existing procedures that carry the new values, three Infor Visual read shapes (two new, one corrected) plus one
extended WIP-application read, and the artifact conventions that bind them.
Names and paths below come from `plan.md` and `research.md` (R4, R5, R6, R7).

## 1. The table

`Database/Tables/18_waitlist_requests_queue/create.sql` (with `rollback.sql` beside it) reshapes
**`waitlist_requests_queue`** with two new nullable columns:

| Column | Type | Holds |
|---|---|---|
| `handler_status` | `VARCHAR(32)` NULL | the one handler-set condition currently set against the request, or NULL when none is set. Cleared with the assignee, so a request returned to the open pool carries none (FR-106) |
| `answers_json` | nullable JSON | a label-to-value map of the request's captured answers, keyed by the declared field label |

`handler_status` exists so a condition can be restored onto its row while the lifecycle status stays intact
underneath it, which the precedence rule in `contracts/status-and-conditions.md` needs. `answers_json` exists because
the die Items now ask two questions and each of the four returning pickups asks one, so the single scalar
`input_value` can no longer hold everything an Item asks. It follows the schema's existing structured-declaration
pattern (`detail_fields_json`, `options_json`).

`handler_status` and `answers_json` are both nullable; a request that carries neither is unchanged in meaning.

## 2. The stored procedures

| Procedure | State | Contract |
|---|---|---|
| `sp_waitlist_request_handler_status_update` | new | writes `handler_status`, gated on the expected handler, and reports its affected-row count so a lost race is told rather than shown as success. The gate is **never** relaxed: unlike `sp_waitlist_request_status_update`'s `p_expected_status`, an absent or empty `p_expected_material_handler` matches nothing rather than meaning "unconditional", so a caller that does not know the handler cannot write a condition |
| `sp_waitlist_request_list` | extended in place | returns `handler_status` and `answers_json` on every row, so the list read carries the new values back to the card |
| `sp_waitlist_request_insert` | extended in place | carries `answers_json`, so the answers captured during New Request are persisted with the request |
| `sp_waitlist_request_status_update` | extended in place | clears `handler_status` whenever the incoming assignee is empty, which is what returning a request to the open pool does (FR-106). **Keyed on the assignee parameter and not on the incoming status**: a request sits in `Pending` while its note is written, so a status-keyed clear would remove a condition on a note. Accept and Complete carry the handler, so both leave the condition alone. The clear reads the parameter rather than the `assigned_material_handler` column because a single-table `UPDATE` evaluates its `SET` list left to right, so the column would already hold the empty value this same statement wrote |

Every data operation goes through a stored procedure. **No application code holds statement text**: there is no inline
or hard-coded SQL in application code, and DML goes through the seam that reports the affected-row count, the same
way `sp_waitlist_request_status_update` already does. `InlineSqlAuditTests` enforces the rule, and the tasks that
carry it are **T026**, **T032** and **T085**.

## 3. The read shapes

| Artifact | State | Contract |
|---|---|---|
| `Database/InforVisual/Queues/Module_Waitlist/Queries/GetPartWarehouseQuantities.sql` | new | **one row per part, never one row per location**: `WarehouseQuantity`, the part-wide sum of `PART_LOCATION.QTY` over every location the part is stocked in, and the same sum restricted to finished-goods locations. A location counts as finished goods when its own `DESCRIPTION` says so, when its `WAREHOUSE.DESCRIPTION` says so, or when its code is `FG` / `DC-FG`. Never `PART.QTY_ON_HAND` — that is the single plant-wide figure the caller must not read — and never the job's per-location figure, never invented. The WIP application's floor buckets are **not** this script's job; they come from `GetWipFloorQuantities.sql` below |
| `Database/InforVisual/Queues/Module_Waitlist/Queries/GetPartMatches.sql` | new | the fuzzy incorrect-part search: an exact match, or failing that every part whose number contains the term. Live only |
| `Database/InforVisual/Queues/Module_Waitlist/Queries/GetDispositionInput.sql` | corrected | stops passing `PART.QTY_ON_HAND` and passes a sum filtered to finished-goods locations, matching the rule the classifier already documents. The classifier's priority is untouched |
| `Database/MTMWipApp/Queues/Module_Waitlist/Queues/GetWipFloorQuantities.sql` | extended | the WIP application's floor buckets for the WIP quantity case and the NCM and item-type buckets |

**Fallback and mirroring.** Only Infor Visual reads use the cached fallback, and only the mirrorable shapes get one.
`GetPartWarehouseQuantities.sql` and the corrected `GetDispositionInput.sql` are Infor Visual reads and each gets a
mirror declaration per shape, including
`Database/InforVisual/Queues/Module_Mock/Populations/part_warehouse_quantities_population.sql` and its declaration in
`MTM_Waitlist.Mock/Services/VisualReadShapeCatalog.cs`. `GetPartMatches.sql` is deliberately **live only** and gets no
mirror and no cached read shape: the operator is told the search is unavailable and the typed value stands as
entered. The handler status, the answers and the dunnage home location are live internal reads and writes and are
never served from the cache.

**One read shape the delivered card does not yet consume.** `part_warehouse_quantities` is registered, mirrorable and
warm-kept, and the mock fallback for it is wired, but **no card cell reads it**: the resolver that feeds the card
(`MTM_Waitlist.Settings/Services/RequestJobFieldValues.cs`) has no arm for a quantity label, and
`RequestJobPartAvailability` carries no quantity. So the three labels that shape was designed to feed —
`Quantity in house` on the coil Items, `Quantity` on `deliver-flatstock` and `Total Qty In House` on the component
Items — are declared but hidden. The exact set is listed in `contracts/card-and-identifier.md` §7.

## 4. The seed and its upkeep

`Database/Seeds/seed_waitlist_request_item_configs/create.sql` (with `rollback.sql`) carries the label corrections
and the new answer cells, so a label the card shows is the label the Item configuration declares. The seed and the
spec agree by design: the card's cells come from the pre-specification, and the seed is brought into line only where
the card must show something the seed does not declare or declares under another label.

**A second seed carries the label move** (also `create.sql` + `rollback.sql`):
`Database/Seeds/seed_waitlist_request_item_configs_label_move/` moves the four labels the card now reads onto the
spellings it reads them under — `Pickup location` → `Die Location`, `Home location` → `Home Location`,
`Scrap type` → `Scrap Type`, and `Coil number` → `Coil Number` on `assist-coil-turn` alone. It exists because
`seed_waitlist_request_item_configs` **truncates and reinserts**, which would wipe operator edits to a live store, so
an installed store is moved in place instead: each statement rewrites one label inside `detail_fields_json` through
`JSON_SEARCH`, so the path is the path the label actually sits at rather than a guessed array index, and a store that
does not hold the label is left untouched. It is rerunnable — the guard is the presence of the label being moved —
and it contains no `TRUNCATE`. The coil Items keep `Coil number` and are deliberately not moved: that spelling is the
label their own cell is declared and read under.

## 5. File-per-artifact convention

- Every schema artifact ships **`create.sql` plus `rollback.sql` in the same change**. That covers the table, the new
  stored procedure and its rollback, and the seed and its rollback.
- The aggregate and description files move in the same change: `Database/StoredProcedures/AllSPs.sql` and
  `Database/Bootstrap/update_table_descriptions.sql`, together with `Database/Seeds/AllSeeds.sql`.
- No application-side statement text is added anywhere in the change.

## 6. What this change does not touch

The mirror and fallback model is unchanged apart from the one new mirrorable read shape above. No new dependency, no
new stack, no feature toggle and no demo or mock mode is added. Internal stores (`mtm_waitlist`,
`mtm_wip_application_winforms`, `mtm_receiving_application`) are always read and written live. The Setup module and
the visual-fallback surfaces are untouched.

**Settings is touched, and this contract says so.** The change extends three files in
`MTM_Waitlist.Settings` in place rather than leaving them alone: `Services/RequestJobFieldValues.cs` (the new
resolver arms and the answer-beats-job rule), `Services/RequestItemPickerRules.cs` (the four returning Items gated on
the work the job holds, replacing the retired hard-coded out-of-scope array), and the `RequestDisposition` model
(which gains `Ncm`). Nothing else in Settings changes, and no new Settings surface is added.
