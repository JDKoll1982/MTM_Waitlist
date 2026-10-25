# Phase 0 Research: Waitlist Line Card Layout

The spec settles behaviour and the pre-spec settles the design, so the open questions here are the technical ones
the spec deliberately leaves out: how each cell that has no source today gets one, how the ring is drawn, how the
per-Item detail sections are grouped, and where two new pieces of state live. Everything below is resolved; nothing
is left as `NEEDS CLARIFICATION`.

## R1. Per-cell value mechanisms (§5.2 of the pre-spec)

The pre-spec lists the cells that resolve today, the labels the resolver ignores, and the values nothing carries.
The table below settles the mechanism for every cell class. "Mechanism" is one of: extend the label map (the value
already exists on the snapshot), extend the snapshot (a read must bring the value in), read from the plant system,
derive, captured answer, or absent by design.

| Cell | Today | Mechanism chosen |
|---|---|---|
| `Die`, `Die Location`, `Part` / `Part number` | resolves | Keep the existing label map arms; add only the corrected `Die Location` label (D33) |
| `Coil number` / `Coil Number`, `Correct coil`, `Correct flatstock`, `Scrap Type` | snapshot holds it, resolver returns null | Extend `RequestJobFieldValues.Resolve` with plain arms onto `CoilPartNumber`, `FlatstockPartNumber`, `ScrapType`. No new read |
| `Component`, `Dunnage part`, `Request description` | captured answers | Unchanged |
| `Coil description` | nothing carries it | Extend the coil subordinate read shape so the snapshot carries the coil's description, then resolve by label. It is a property of the coil part the job already names, so it belongs on the existing snapshot rather than on a second read |
| `Average coil weight` | nothing carries it on the snapshot | Read live from the receiving store: `mtm_receiving_application.receiving_history` through the existing `sp_receiving_history_average_coil_weight` via `IAverageCoilWeightService`, then resolve by label. Never Infor Visual's per-part `PART.WEIGHT`, and never the assist coil turn's own operator-supplied `Weight of Coil` (FR-054) |
| `Quantity in house` (coil Items), `Quantity` (`deliver-flatstock`), `Total Qty In House` (component Items) | nothing carries them | One new Infor Visual read shape, `GetPartWarehouseQuantities.sql`, returning the part-wide sum filtered by location class, plus the WIP application's floor sum for the WIP case. Which source is read follows the item type (D38): finished-goods stock from Infor Visual's `PART_LOCATION`, floor/WIP stock from `inv_inventory`. Never the job's per-location figure, never invented |
| `Item Type` (component Items), `Part Type` (the four returning pickups) | nothing derives it on the card | Wire the existing `RequestDispositionResolver` to the card path and read its disposition. The stack is built, registered and unconsumed today; this gives it its first consumer instead of writing a second derivation |
| NCM's Part Type | the classifier has no NCM value | Add `Ncm` to `RequestDisposition` and derive it from the stock's location (`md_locations.Location LIKE 'NCM%'`, `LOCATION`/`PART_LOCATION.STATUS`), never from the part's recorded item type |
| `Home Location` (dunnage Items) | `RequestDunnagePart` has no location | Extend the dunnage-parts read so the snapshot carries `dunnage_parts.home_location`, then resolve by the corrected label. Internal store, live |
| `Destination` (`deliver-flatstock`) | declared `fixed`, no fixed resolver | Resolve it from the request's own requesting work centre, which is the repository's documented deliver rule ("every Deliver Item is delivered to the requesting work center, no user entry"). No new read and no new question. The retirement that removed the old answer-sourced `Destination` stands: it is not reintroduced as an answer and does not rejoin the line-2 token set |
| `Size` (hopper Items) | nothing | Captured answer, added to the Item's declaration as an answer-sourced field |
| `# of Skids` (the four returning pickups) | nothing | Captured answer, defaulting to 1, added to each of the four declarations |
| `Weight of Coil` (`assist-coil-turn`) | nothing | Required captured numeric answer, added to the declaration. Deliberately not the job's average coil weight (FR-054) |
| The die-shop cell (both die Items) | nothing | Captured answer: the checkbox's flag, added to the declaration, with the phrase chosen by the Item's category from a resource key |
| `Lugger Location` (`pickup-scrap`) | no source in this feature | Superseded by 013-lugger-location-config. The source is configuration: the lugger arrangement assigns each scrap type an ordered set of luggers, and the card draws the one holding that type's dump point. The reveal path is a snapshot read per list load plus one resolver arm reading the availability's `LuggerLocation`, so the cell appears on the next load with no card change |
| `Description` (dunnage Items) | declared, provably unfillable | Absent by design, same path as above (see R8) |

**Rationale**: the pre-spec's own rule is that anything the job cannot carry is read from the source that holds it,
nothing is invented and nothing is blanked. Every mechanism above is one of its three named routes (extend the
resolver, read from Infor Visual, or a real captured answer), and each picks the smallest one that works.

**Alternatives considered**: deriving all quantities from the job snapshot was rejected because the snapshot has no
quantity and inventing one would violate invariant 2. A per-cell Infor Visual read was rejected as four queries
where one shape serves three cells. Reading `Total Qty In House` from `PART.QTY_ON_HAND` was rejected outright: it
is the defect the spec's FR-090 records.

**Correction (supersedes an earlier claim)**: an earlier version of this table attributed `Average coil weight` to
Infor Visual's `PART.WEIGHT`, carried on the extended coil subordinate read as an `AverageWeight` column. That was
wrong — Visual holds a single per-part weight figure, not the average of the part's received skids — and it is
superseded by the row above. The card's value is the receiving store's own weighing history, and the `AverageWeight`
column has been removed from the subordinate read and from its mirror, so the job snapshot carries no Visual weight
at all. Only the coil's `Description` still rides that read.

## R2. How the time ring is drawn

**Decision**: `Microsoft.UI.Xaml.Controls.ProgressRing` in its determinate state, with `Value` over its
`Minimum`/`Maximum` set from `elapsed ÷ allotted`, and the time text overlaid in a `Grid` cell on top of it.

**Rationale**: grounded against Microsoft Learn, whose progress-control guidance states that both `ProgressBar` and
`ProgressRing` have a determinate state that "shows the percentage completed of a task", and that a determinate
progress control does not block interaction. The control is also explicitly read-only, which is right here: the
ring is never the click target, the status word beside it is.

**Alternatives considered**: drawing an arc by hand with a `Path` and `ArcSegment` was rejected as custom geometry
for something the platform already renders, with worse accessibility and no theming path. A `ProgressBar` was
rejected because the design is a ring carrying its own time inside it.

## R3. Grouping the per-Item detail sections (D31)

**Decision**: one `DataTemplate` per Item, all in a single `ResourceDictionary`
(`WaitlistLineCardDetailTemplates.xaml`), selected by a `DataTemplateSelector` keyed on the row's Item.

**Rationale**: a WinUI `UserControl` requires its own XAML file, so D31's "one control per card, all in one file"
cannot be realised as 24 controls in one file. A resource dictionary with 24 templates is the same arrangement
without the file-per-card bloat, and template selection by an item's own properties is the documented WinUI
mechanism for it. Identity, status and action sections stay in the card's own XAML; only the detail column moves.

**Alternatives considered**: 24 `UserControl`s (rejected: 24 files, which is exactly what D31 forbids); one control
with a `switch` in code-behind building cells imperatively (rejected: it throws away XAML binding and the markup
tests that read it); a single generic cell list bound to a collection (rejected: the grid geometry differs per cell
count, which is a layout concern the template expresses declaratively).

## R4. Where a handler-set condition is stored

**Decision**: a nullable `handler_status VARCHAR(32)` column on `waitlist_requests_queue`, written by a new
`sp_waitlist_request_handler_status_update` (gated on the expected handler, reporting affected rows) and returned by
`sp_waitlist_request_list`.

**Rationale**: the condition is set on the card and must come back on the next list read, so it belongs to the
request row. A separate column keeps the lifecycle status intact, which FR-012's precedence rule needs: the card
still has to know the lifecycle word underneath a condition. The write is a stored procedure because that is the
repository's rule for every data operation, and it reports affected rows so a lost race is told rather than shown as
success.

**Alternatives considered**: overloading `status` (rejected: the lifecycle word would be lost, and the palette
would stop being able to tell a condition from a state); a history table (rejected: the audit table already exists
for that, and the card reads one current value); a client-side store (rejected: FR-073 requires it to survive a
restart and be restored on the row).

## R5. Where the new answers are stored

**Decision**: a nullable `answers_json` column on `waitlist_requests_queue`, carried by
`sp_waitlist_request_insert` and returned by `sp_waitlist_request_list`, holding a label-to-value map.

**Rationale**: the die Items now ask two questions (which die, and whether it is at the die shop) and the four
returning pickups each ask one, so a single scalar `input_value` can no longer hold everything an Item asks. One
nullable JSON column keyed by the declared label lets a declaration with `source = answer` resolve without a table
per answer, and it matches the way this schema already stores structured declaration data (`detail_fields_json`,
`options_json`).

**Alternatives considered**: widening `input_value` (rejected: it holds one answer by definition and the die Items
already use it); a `waitlist_request_answers` table (rejected: a join and a second write for at most two values per
request); storing answers in `note` (rejected: that column is the operator's own text).

## R6. Sourcing the full part quantity

**Decision**: one new Infor Visual read shape returning the part-wide quantity, filtered by location class, with its
mirror population declared; the WIP case reads the WIP application's floor buckets.

**Rationale**: FR-090 requires the full sum for the part "read from the source that holds it for the item's type",
and D38 names those two sources. Summing `PART_LOCATION.QTY` for the part (joined to `LOCATION`) is the
location-aware shape that already exists in `GetInventoryLocations.sql`; summing `inv_inventory.Quantity` with the
dunnage exclusion is what `GetWipFloorQuantities.sql` already does.

**Alternatives considered**: `PART.QTY_ON_HAND` (rejected: total on-hand with no location filter, which is the
FR-090 defect); a per-location figure (rejected by FR-090 explicitly).

## R7. The fuzzy incorrect-part search

**Decision**: a new `GetPartMatches.sql` Infor Visual query that returns an exact match or, failing that, every part
whose number contains the term; a modal presentation in the wizard running on a worker thread; no mirror
declaration and no cached shape.

**Rationale**: D24 settles the behaviour and the spec's Assumptions settle the live-only choice, because a
contains-search over every part is not a mirrorable shape. Degrading to the typed value when the plant system is
unreachable is therefore the designed behaviour, not an error path.

**Alternatives considered**: reusing the receiving application's WinForms picker (rejected by the pre-spec: it
cannot be reused verbatim in WinUI 3); caching the reply (rejected: it would serve a stale part list for a
correction the operator is making now).

## R8. FR-090 and FR-095, the two carried-forward questions

**Decision (FR-090)**: correct the query's input, leave the rule alone. `GetDispositionInput.sql` stops passing
`PART.QTY_ON_HAND` and passes a sum filtered to finished-goods locations, matching the rule the classifier already
documents.

**Rationale**: the classifier's documented priority ("on-hand > 0 at a finished-goods / shipping location") is
already correct; only the value handed to it is wrong. Fixing the input is the smallest change that makes Part Type
truthful, which the four returning pickups need, and it does not silently rewrite a ratified rule.

**Alternatives considered**: amending the classifier's rule text (rejected: `clarify` owns that question and nothing
here requires it); leaving the defect and hiding the affected cells (rejected: FR-092 requires the derivation, and a
hidden Part Type is a silent regression for four newly-offered Items).

**Decision (FR-095)**: leave the dunnage `Description` declaration in place and never draw the cell.

**Rationale**: it is the spec's own edge case ("the Item's configuration declaring a field the card must not show:
the field is not drawn, and the configuration is left alone") and it invents nothing. Removing the declaration is a
scope cut that `clarify` should decide with the owner.

**Alternatives considered**: deleting the declaration (rejected as a silent change to shipped configuration);
backing it with a substitute value (rejected by invariant 2).

## R10. Decisions taken where the inputs were silent (recorded 2026-10-11)

Four questions had no answer in the spec or the pre-spec, so the implementation decided them. They are recorded
here, with what they bind, because a decision that lives only in the code is a decision nobody can review.

**1. Where a hopper size's reference image comes from.** The picture lives in a folder of its own,
**`Waitlist\Hoppers`**, under the configured picture folder, one file per size named after the size word —
`Small.png`, `Medium.png`, `Large.png`. The folder is configurable on Settings → Storage Paths
(`image_storage.hopper_folder_path`, default `Waitlist\Hoppers`), and the naming contract is
`docs/guides/guides-pictures-image-naming.md`. One source serves both surfaces: the New Request hopper step and the card's `Size` cell
resolve through the same scope and the same key, so they cannot ask for two different pictures (FR-078).

*Rationale*: the size word is already the answer the flow records and the key the card reads, so making it the
file name means one spelling serves the step, the share and the card. A folder of its own is needed because the
value is not a part number and must not be filed by the part-family prefix rules.
*Rejected*: filing hopper pictures under the Visual part scope (a size word is not a part, and the resolver would
look for a part called "Large"); a second picture store (the application already has one).

**2. A blank `# of Skids`.** The skid-count step **starts at 1** and a blank answer is accepted as that default of
one, but the card **hides a blank answer rather than writing a 1 into the cell**. What the operator submitted is
what the card shows (FR-087, FR-089); the default belongs to the page, not to the row.

*Rationale*: the page's default is a convenience for the person, while the card is a record of the request.
Substituting 1 on the card would show a number nobody answered.

**3. `Weight of Coil` must be greater than zero.** A blank, non-numeric, zero or negative answer is refused and the
operator cannot continue (FR-086). Guard: `weight <= 0` in `NewRequestCoilTurnViewModel`.

*Rationale*: a coil weighing nothing is not a measurement, and a zero would be indistinguishable downstream from
"not recorded".

**4. FR-085's "use the typed value as entered".** This means the **normalized** term — upper-cased and zero-padded
to ten characters when it starts with a known prefix — not the raw keystrokes. The operator is told the part search
is unavailable; the value that travels on is the one the application would have searched for
(`PartMatchSearchInput.Normalize`).

*Rationale*: "as entered" distinguishes the operator's own typed value from a value the search would have supplied,
which is the point of the requirement. It does not mean bypassing the normalization every other answer gets, or the
request would carry a term no other surface would recognise (FR-082, FR-085).

## R12. Which returning pickups an active job is offered (recorded 2026-10-20)

A **finished-parts** (`pickup-fg`) and a **WIP** (`pickup-wip`) pickup are offered on **any active job**; the
**NCM** and **outside-service** pickups stay gated on the signal each one names. Before this, all four were offered
only on the derived disposition, one state lighting exactly one Item.

*Rationale*: the two production pickups name **the job's own product** — the parts the work centre has just made —
so the job is the work they name. The derivation that separates them leans on finished-goods **location names that
are not confirmed for this installation** (`GetDispositionInput.sql`, T067) and needs the plant system to answer at
all, so a job whose parts were sitting somewhere the query does not recognise, or whose Visual read did not answer,
had no finished-parts pickup at all. The owner hit exactly that and reported the option missing.

*Not changed*: the part's derived type. `Part Type` on those cards still shows what the derivation found and stays
hidden when it found nothing (FR-089, FR-091), so the looser availability does not claim a disposition the reads did
not support. NCM and outside service keep their gates because their signals are confirmed — the location the stock
occupies (`md_locations.Location LIKE 'NCM%'`) and the operation's vendor fields.

*Rejected*: offering all four on any job (NCM pickup with no NCM stock on the floor wastes a handler trip); leaving
the finished-parts pickup gated on the derivation and repairing only the location codes (the codes are unconfirmed,
so nothing could be repaired from the repository).

## R11. Carried into `clarify`: NCM's priority for a part that has both

FR-092 orders the derived item type Outside Service → Finished Goods → WIP. **It never says where NCM sits**, and
the case that matters is a part that is both sitting at an NCM location and has subcontracted work outstanding.

This work places **NCM after Outside Service and before Finished Goods/WIP**, and flags the question rather than
treating the placement as settled. The reasoning: NCM is a statement about *where the stock is*, whereas Outside
Service, Finished Goods and WIP are statements about *what work remains*, and the location-derived state is the one
a material handler acts on first. But nothing in the spec, the pre-spec or the plant's own documentation confirms
that ordering, so it is recorded as an assumption in `spec.md` under FR-092 and left for `clarify` to ratify or
overrule.


## R9. What the change does not do

No new package and no new stack. No feature toggle, no demo or mock mode, and no demo-data substitution, which
FR-003 and FR-014 forbid and `RetiredSymbolAuditTests` guards. No parallel navigation stack: Open details goes
through the existing navigation service. Setup, Settings and the mock/visual-fallback surfaces are untouched, and
the mirror model is unchanged apart from the one new mirrorable read shape in R6.
