# Research: Generic Work Center — Stated Part, Sequences and Quantity

Phase 0 answers the questions the plan would otherwise leave open. Each entry records what was chosen,
why, and what was rejected. Repository facts name the file they were read from.

## R1 — Every part, sequence and quantity read is live-only; no mirror shape is added

**Decision**: all five reads this chunk adds run against Infor Visual or the WIP floor directly. None
gets an `mtm_mock` mirror shape, population script, result table or `sp_visual_*` procedure.

**Rationale**: FR-017 says a system that cannot be reached must report unavailable and "MUST NOT
substitute a cached, mirror, sample or fabricated sequence list or quantity". A mirror shape is
therefore not merely unnecessary here, it is forbidden for these values. The reachability probe and
the settled-verdict short-circuit in `VisualReadFallback` stay untouched and unused by this chunk.

**Alternatives considered**: extending the shipped shapes (`VisualOperationSequencesFallback`, whose
`operation_sequences` shape is keyed by work order and part) and adding new ones. Rejected twice over:
the existing shape needs a work-order number the stated part does not have, and a new shape would
serve cached values for exactly the two answers the spec forbids caching.

## R2 — Closed-sequence quantity reuses `GetPartWarehouseQuantities.sql`, run live-only

**Decision**: the Visual quantity read runs the existing
`Database/InforVisual/Queues/Module_Waitlist/Queries/GetPartWarehouseQuantities.sql` through
`IVisualQueryExecutor` and takes its `WarehouseQuantity` column, which is the sum of `PART_LOCATION.QTY`
over every location the part is stocked in. The shipped
`VisualPartWarehouseQuantitiesFallback` is **not** used.

**Rationale**: the script's own header says it answers a part-wide on-hand figure, which is what
FR-013 asks for. It also returns one row of zeros for a part stocked nowhere and no row for a part the
plant does not know, which is exactly the "0 where none" and "unknown part" distinction the screen
needs. Running the script directly keeps the mirror out of the path (R1).

**Alternatives considered**: `PART.QTY_ON_HAND` (rejected by the script's own header: it is a single
plant-wide figure, not the part-wide sum over locations), and `inventory_locations`, which is
per-location and would make the caller sum rows.

## R3 — Part completions extend `GetPartMatches.sql` with the description

**Decision**: add `part.DESCRIPTION AS Description` to the existing live-only fuzzy part search script,
and read both columns. The generic part step's completion read is that script.

**Rationale**: it is already the repository's one part-number search: a live-only, `TOP (200)`,
exact-match-first, wildcard-escaped `PART` read with no shape entry and no mirror, written for the
incorrect-part correction step (`PartMatchSearchService`, whose header calls out that a contains-search
over every part is not a mirrorable shape). Its existing caller reads only `PartNumber`, so a second
column is additive. The part step needs the description to draw the row the skeleton already draws.

**Alternatives considered**: a new `GetPartCompletions.sql`. Rejected as a near-copy of the same query
over the same table, with the two destined to drift apart; and a shape-based read, rejected by R1.

## R4 — Two Visual scripts: one for the list, one for the state at choice time

**Decision**: `GetPartSequences.sql` answers every attributable Visual sequence for a part with its
number, a description, its operation status code, its outside-service signal and which relation named
it. `GetPartSequenceState.sql` re-reads the status and outside-service signal for one part and one
sequence, and is what the quantity read consults.

**Rationale**: FR-004 wants the union of Visual's own-part sequences and Visual's sequences where the
part is a requirement at an operation; one script with a `SequenceSource` column answers both in one
read and lets the app merge with the floor's list. FR-005 needs the status in the list so a
cancelled-only sequence is never offered. But the spec's edge case says a sequence's operation can
close between the list and the reading: the owner must be decided from the state *at read time*, so the
quantity path cannot reuse the list's status.

**Alternatives considered**: one script doing both, refused because it would decide the owner from a
value read at a different moment than the reading itself; and reading the state inside the quantity
scripts, refused because the same decision then lives in two places for two systems.

## R5 — The operation status vocabulary is reused, not restated

**Decision**: open and closed are decided by `RequestDispositionStatusCodes.OpenStatusCodes`
(`R`, `U`, `F`) and `ClosedStatusCodes` (`C`); outside service is the `OUTSIDE_SERVICE` shop resource
or a non-empty vendor/service field on the operation; `X` (cancelled) stays in neither set.

**Rationale**: spec assumption A2 says the stage is taken from the states the plant already relies on
for disposition, and that class is the repository's single editable source for those codes, confirmed
against live data. Restating the letters in the generic flow would create a second place to edit.

**Alternatives considered**: a generic-flow copy of the code sets, rejected as drift waiting to happen.

## R6 — The floor's three reads join the existing floor service, and report success

**Decision**: `WipFloorInventoryService` gains three methods, one per new script (part matches by
search term, distinct operations for a part, quantity for a part and operation). Unlike its three
existing methods, each new one returns whether the read succeeded alongside its rows.

**Rationale**: that service is already the one place that loads the floor's checked-in scripts and
talks to `mtm_wip_application_winforms`. Its existing methods swallow a failure into an empty list,
which was right for a coverage read that renders no parts. Here the difference between "the floor holds
none" and "the floor could not be asked" is the difference between an answer and FR-017's unavailable
state, so the new methods must not collapse it.

**Alternatives considered**: a second floor service, rejected as a second seam over the same database;
and throwing from the new methods, rejected because the caller would then have to catch to render a
state the seam already expresses as a value.

## R7 — A narrow part-shaped seam, not an edit to the wide one

**Decision**: add `IGenericRequestPartData` with three members (completions, sequences, quantity) to
`MTM_Waitlist.GenericRequest/Contracts`, and have `GenericRequestPartViewModel` depend on it. The wide
`IGenericRequestStepData` from spec 015 is left exactly as it is.

**Rationale**: the part step's dependency is the three reads it makes, and that is also the shape a
test fakes. Editing the wide interface would either break 016's unbuilt implementation or force this
chunk to write the other nine steps' methods. When 016's live implementation lands, its part methods
forward to the same data class, which is a one-line delegation rather than a redesign.

**Alternatives considered**: extending `IGenericRequestStepData` with a quantity member and implementing
the whole interface here (rejected: it makes this chunk depend on 016's unbuilt work), and adding the
part methods to a partial class beside 016's implementation (rejected: it presumes a file and class
name 016 has not chosen).

## R8 — A dedicated quantity reading type, and the card's quantity is not it

**Decision**: add `GenericRequestQuantityReading`, a `GenericRequestStepResult` carrying a kind
(on hand or not inventoried), a decimal figure, and the system that answered. The card model gains its
own on-hand fields beside the requested quantity it already carries.

**Rationale**: three answers must stay distinguishable — a figure (including `0`), not inventoried, and
could not be read. Two of them are successes and one is a failure, so `Populated`/`Empty` cannot express
them; a kind alongside the presentation can. And the card already has a `Quantity` (what the requester
asked for), so on-hand must be a separate field or the two numbers would overwrite each other.

**Alternatives considered**: reusing `GenericRequestStepResult<decimal>` (rejected: an `Empty` result
would have to mean "not inventoried", which reads as a failed lookup and contradicts
`Empty`'s documented meaning), and folding the reading into the sequence option (rejected by FR-007,
which requires the sequence to be chosen before any quantity is read).

## R9 — The stage becomes derived, not answered

**Decision**: remove the WIP / finished-good selector from the part screen and derive the stage from
the chosen sequence's operation state. `GenericRequestPartStage` keeps its meaning but is set by the
reading, not by the requester.

**Rationale**: FR-012 forbids asking the requester which system to use; choosing WIP or finished good
is choosing the owning system by another name. FR-009 assigns the owner from the operation state, and
US2 scenario 4 shows outside service as a third outcome the two-way choice cannot express.

**Alternatives considered**: keeping the selector and cross-checking it against the state (rejected:
two sources for one fact, and the screen would have to explain a disagreement the requester cannot
resolve).

## R10 — `AutoSuggestBox` for the completion box

**Decision**: replace the part screen's plain text box with `AutoSuggestBox`
(`Microsoft.UI.Xaml.Controls`), which offers suggestions as the requester types.

**Rationale**: FR-001 asks for completions as the number is typed. Microsoft Learn's `AutoSuggestBox`
reference describes it as the control that makes suggestions as text is entered and notifies the app
when the text changes, which is the behaviour asked for; the existing `TextBox` plus `ListView` pair
filters a list that must already be loaded.

**Alternatives considered**: keeping `TextBox` plus `ListView` and loading every part up front (the
Visual read has no "all parts" script, and the floor's is an unfiltered whole-inventory read).

## R11 — Where the floor's operation number comes from is unresolved

**Decision**: treat the floor's operation column as **unconfirmed** and block the two floor sequence
scripts on confirming it. Write the scripts so a blank, `N/A` or non-numeric operation yields no
sequence.

**Rationale**: `RequestDispositionStatusCodes.cs` states the floor's inventory is keyed by part,
location and operation, and this spec's edge case quotes `N/A` appearing in live data. But every floor
script in the repository reads `inv_inventory (PartID, Location, Quantity, ItemType)` and
`md_locations (Location, Building)`, and no schema export for the floor exists under `docs/database`
(that directory holds the Infor Visual export only). The column name, therefore, is not a fact this
repository can supply, and guessing it would put a wrong column into a checked-in script.

**Alternatives considered**: deriving a stage from the location name (rejected: the spec requires
distinct usable sequence numbers, and a location is not a sequence); and omitting the floor from the
sequence list until later (rejected: US1 scenario 3 and FR-004 both depend on it, so the owner must
decide rather than have the chunk quietly under-deliver).
