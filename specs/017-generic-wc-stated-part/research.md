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
number, a description, its operation status code, whether it is the last operation the work order still
has to run, the resource the part goes to next, and which relation named it. `GetPartSequenceState.sql`
re-reads those same facts for one part and one sequence, and is what the quantity read consults.

**Rationale**: FR-004 wants the union of Visual's own-part sequences and Visual's sequences where the
part is a requirement at an operation; one script with a `SequenceSource` column answers both in one
read and lets the app merge with the floor's list. FR-005 needs the status in the list so a
cancelled-only sequence is never offered. But the spec's edge case says a sequence's operation can
close between the list and the reading: the owner must be decided from the state *at read time*, so the
quantity path cannot reuse the list's status.

**Corrected 2026-11-04 — one operation speaks for a sequence, and it is the open order's.** Both
scripts originally rolled status and outside-service up across *every* work order the part has ever
had, taking a max. A part keeps years of orders and its routing changes between them, so one closed
subcontracted order branded a live in-house sequence as outside service. Part `21-28841-006` runs
sequence 90 at in-house press `096-03` on the order open today (`21-28841-006` lot 0, status `U`) and ran
it at `OUTSIDE_SERVICE` on orders closed years ago (`WO-036612`, `WO-036964`, `WO-037245`, …); the WIP
floor holds **573 units at operation 90**, all `ItemType = WIP`. The app therefore told the requester
that a part it holds 573 of was not inventoried. Both scripts now take the facts from the operation on
the part's **open** work order (status `R`, `U` or `F`), and only when the part has no open order at all
from the most recently created closed one. Verified live: sequence 90 answers `R` / outside-service `0`
and sequence 95 answers `C` / `1`.

**A sequence number is not a part type.** Measured across `OPERATION`: of 157 distinct sequence numbers,
49 are *always* routed to `OUTSIDE_SERVICE`, 53 *never*, and **55 are mixed**. Sequence 900 is
effectively the subcontracted step (21,222 of 21,272 rows); sequence 20 is the busiest mixed one
(2,470 of 15,959). `A22-77724-100` routes sequence 20 to `OUTSIDE_SERVICE` on its live order while
`21-28841-006` routes its own sequence 20 in-house. No lookup table from number to part type is
defensible; the routing on the part's own order is the only answer.

**Where outside-service stock actually is.** `inv_inventory` holds 3,185 units across 21 parts at
`O/S - VITS`, every row `ItemType = WIP` with a numeric operation, and `md_part_ids` classifies all 21
as `WIP`. The one `OUTSIDE SERVICE` part in the master (`MCML00360`) is a migrated placeholder with no
description and no operations. So outside service is a **location state of WIP stock that has a
quantity**, not the absence of inventory that assumption A3 describes. The row names `WipFloor` as a
source whenever the floor itself names the sequence — never invented when the floor holds nothing.

**Superseded the same day by the plant's own tagging rule.** IT explained how the tags are produced
(2026-11-04): *the application reviews the operations; if this is the last operation available of the
work order it prints the FG format, otherwise the WIP format; the Outside Service label is still the WIP
label; every WIP tag prints a "Next resource" line, and the resource id for O/S is "Outside Service".*
So there are exactly two types, decided by position in the routing rather than by a routing target, and
outside service is a **next-resource name**, never a type. Consequences, all now implemented:

- Both scripts return `IsLastOperation` (nothing later on the work order is still to be run, i.e. no
  `OPERATION` row with a greater `SEQUENCE_NO` whose status is not `C`) and `NextResource` (the resource
  the following operation runs at) **in place of** the outside-service column.
- `GenericRequestPartStage` loses `OutsideService`; the stage is `FinishedGood` when `IsLastOperation`,
  otherwise `Wip`. Who owns the quantity stays FR-009's question, answered by the status.
- The "nothing is read for outside service" rule is withdrawn, because it was derived from the same
  wrong premise and contradicts the floor's own stock. An outside-service sequence is read like any
  other, and its figure comes from wherever its status says.
- The step shows the operation's next resource, so a requester who picks a subcontracted step can see
  that the part goes to Outside Service next — the same line the printed tag carries.

Verified live: `21-28841-006` sequence 90 answers `R` / `IsLastOperation = 0` / `Outside Service`,
sequence 900 answers `R` / `IsLastOperation = 1` / no next resource, sequence 95 answers `C` / last;
`A22-77724-100` sequence 5 answers `R` / `Outside Service`, sequence 20 answers `R` / last;
`21-28841-Blank` sequence 19 answers `U` / last.


**Alternatives considered**: one script doing both, refused because it would decide the owner from a
value read at a different moment than the reading itself; and reading the state inside the quantity
scripts, refused because the same decision then lives in two places for two systems.

## R5 — The operation status vocabulary is reused, not restated

**Decision**: open and closed are decided by `RequestDispositionStatusCodes.OpenStatusCodes`
(`R`, `U`, `F`) and `ClosedStatusCodes` (`C`); `X` (cancelled) stays in neither set. Nothing here decides
the part's *type* any more — that comes from FR-014's last-operation rule — and outside service is not a
state of its own, so the `OUTSIDE_SERVICE` resource id is not consulted by this chunk.

**Rationale**: spec assumption A2 says the owner is taken from the states the plant already relies on
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
(on hand or could not be read), a decimal figure, and the system that answered.

**Rationale**: two answers must stay distinguishable — a figure (including `0`) and could not be read —
and a kind alongside the presentation expresses that without overloading `Empty`. The card already has a
`Quantity` (what the requester asked for), so on-hand is recorded as its own value or the two numbers
would overwrite each other.

**Alternatives considered**: reusing `GenericRequestStepResult<decimal>` (rejected: a figure of `0` and a
failed read would look alike), and folding the reading into the sequence option (rejected by FR-007,
which requires the sequence to be chosen before any quantity is read).

## R9 — The stage becomes derived, not answered

**Decision**: remove the WIP / finished-good selector from the part screen and derive the stage from
the chosen sequence's position in its routing. `GenericRequestPartStage` keeps its meaning but is set by
the reading, not by the requester.

**Rationale**: FR-012 forbids asking the requester which system to use; choosing WIP or finished good
is choosing the owning system by another name. FR-009 assigns the owner from the operation state, and
FR-014 requires the type to follow the plant's own tagging rule, which the requester cannot be expected
to apply.

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

## R11 — The floor's operation number: `inv_inventory.Operation`, confirmed 2026-11-03

**Decision**: the two floor sequence scripts read `inv_inventory.Operation`, and treat a blank, `N/A`
or non-numeric value as no sequence. **Resolved**: the column name was read from the live schema
rather than guessed.

**Rationale**: `RequestDispositionStatusCodes.cs` states the floor's inventory is keyed by part,
location and operation, and this spec's edge case quotes `N/A` appearing in live data. No schema
export for the floor exists under `docs/database` (that directory holds the Infor Visual export only),
so the column was read from the store itself: `INFORMATION_SCHEMA.COLUMNS` on
`mtm_wip_application_winforms` answers `inv_inventory (ID, PartID, Location, Operation, Quantity,
ItemType, ReceiveDate, LastUpdated, User, BatchNumber, Notes, ColorCode, WorkOrder)` with
`Operation varchar(100)`, and the live values are the plant's operation numbers (`20`, `90`, `19`,
`30`, …) alongside 32 rows carrying the literal `N/A`. `md_locations` carries no operation column, so
the floor's key is `(PartID, Location, Operation)` exactly as the disposition research states.

**Alternatives considered**: deriving a stage from the location name (rejected: the spec requires
distinct usable sequence numbers, and a location is not a sequence); and omitting the floor from the
sequence list until later (rejected: US1 scenario 3 and FR-004 both depend on it, so the owner must
decide rather than have the chunk quietly under-deliver).
