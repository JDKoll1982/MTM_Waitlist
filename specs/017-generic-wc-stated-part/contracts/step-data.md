# Contract: the part-shaped data seam

**Feature**: 017 Generic Work Center — Stated Part, Sequences and Quantity
**Plan**: [plan.md](../plan.md) | **Research**: [research.md](../research.md) | **Data model**: [data-model.md](../data-model.md)

The part step asks three questions and nothing else. This contract fixes their names, their arguments
and their return shapes, so the screen, its live implementation and its test double cannot drift.

## 1. The seam

`MTM_Waitlist.GenericRequest/Contracts/IGenericRequestPartData.cs`

| Member | Argument | Returns |
| --- | --- | --- |
| `ReadCompletionsAsync` | `string term` | `Task<GenericRequestStepResult<GenericRequestPartOption>>` |
| `ReadSequencesAsync` | `string partNumber` | `Task<GenericRequestStepResult<GenericRequestSequenceOption>>` |
| `ReadQuantityAsync` | `string partNumber, string sequence` | `Task<GenericRequestQuantityReading>` |

Rules that hold for every member:

- A read never throws for an unreachable source. It reports `Unavailable` with the attempt time
  (FR-017). Only `OperationCanceledException` propagates.
- An `Unavailable` result carries no items, so nothing unverified can be accepted as a part, a
  sequence or a quantity (FR-017).
- An `Empty` result means the read succeeded and found nothing. It is not a failure and offers no
  retry.
- No member reads a mirror, a sample or a cached shape (FR-017, R1).

The wide `IGenericRequestStepData` from spec 015 is untouched. `GenericRequestLiveStepData` forwards
its `ReadPartsAsync` and `ReadSequencesAsync` to this seam so a caller holding the wide interface
reaches the same live implementation (R7).

## 2. Row shapes

### `GenericRequestPartOption`

| Field | Type | Rule |
| --- | --- | --- |
| `PartNumber` | `string` | the accepted number |
| `Description` | `string` | empty when the source holds none |
| `Source` | `SequenceSource` | the system that supplied the row (FR-001) |

`QuantityOnHand` is **removed**: a quantity belongs to a chosen sequence, not to a part (FR-007).

### `GenericRequestSequenceOption`

| Field | Type | Rule |
| --- | --- | --- |
| `Sequence` | `string` | a usable sequence number; `N/A`, blank and non-numeric floor operations never appear (FR-006) |
| `Description` | `string` | empty when no source holds one |
| `Sources` | `IReadOnlyList<SequenceSource>` | every source that supplied the number, once each, in the fixed order below (FR-004) |
| `OperationStatus` | `string` | the Visual status code, or empty when Visual records none |
| `IsLastOperation` | `bool` | true when the work order has nothing later still to run, which is what makes it the finished good (FR-014, R4) |
| `NextResource` | `string` | the resource the part goes to after this operation, which is the line a WIP tag prints; empty for the last operation |
| `HasCancelledOnlyOperations` | `bool` | true when every operation behind the number is cancelled (`X`), which keeps it off the list (FR-005) |
| `SourcesDisplay` | `string` (computed) | the joined source names, localized, which is what the list draws |
| `SourceCodes` | `string` (computed) | the joined source names by their stable names, which is what the request records (FR-016) |

Source order is fixed: `VisualOwnPart`, `VisualRequirement`, `WipFloor`.

## 3. `SequenceSource`

```text
VisualOwnPart       the part is the work order's own part in Infor Visual
VisualRequirement   the part appears as a requirement at an operation in Infor Visual
WipFloor            the WIP floor holds the part at that operation
```

## 4. `GenericRequestQuantityReading`

Three answers must stay distinguishable, so the reading carries a kind beside the presentation
(R8). It is not a `GenericRequestStepResult<decimal>`, because `Empty` there cannot say whether the
owner holds none or could not be read.

| Field | Type | Rule |
| --- | --- | --- |
| `Kind` | `GenericRequestQuantityKind` | `OnHand` when the presentation is `Populated`; meaningless otherwise |
| `Figure` | `decimal?` | units on hand, `0` where none; null when `Unavailable` |
| `AnsweredBy` | `SequenceSource?` | `WipFloor` or `VisualOwnPart` (the system, not the relation); null when `Unavailable` |
| `Stage` | `GenericRequestPartStage` | the stage the operation state gave at this read, which the review shows and the request records (FR-009, FR-016) |
| `Presentation` | `GenericRequestPresentation` | `Populated` for a successful read, `Unavailable` for a failed one (FR-017) |
| `AttemptedAtUtc` | `DateTimeOffset` | when the read was attempted |

Validation:

- `OnHand` carries a `Figure`, an `AnsweredBy` and the derived `Stage`. A figure of `0` is a fact, not an
  absence (FR-013).
- `Unavailable` carries neither, and the caller shows the attempt time with a retry (FR-017).

Factory members: `OnHand(figure, answeredBy, stage, attemptedAtUtc)`, `Unavailable(attemptedAtUtc)`.

## 5. `GenericRequestPartStage`

The requester no longer answers this; the quantity read derives it from the chosen sequence's
operation state, re-read at the moment of the reading (FR-009, FR-014, R4).

Two types, decided the way the plant's own tagging decides them (IT, 2026-11-04): review the
operations, and the **last operation the work order still has to run** produces the finished good;
every other operation is work in progress. Who owns the quantity is a separate question, settled by
the operation's state.

| Value | Derived from | Quantity owner |
| --- | --- | --- |
| `Wip` | any operation with something later still to run | the state: open reads the floor by part and sequence, closed reads Infor Visual by part number |
| `FinishedGood` | the last operation the work order still has to run | the state, as above |
| `Unknown` | Visual records no state for that sequence | the floor first, Visual by part number only when the floor holds none |

There is no `OutsideService` stage. Outside service is the name of a shop resource, and a WIP tag
names it on its **next resource** line; a part at outside service is work in progress like any
other, and its material is floor stock at an outside-service location.

`Unknown` is new. `Wip` and `FinishedGood` keep their names but are now derived from the operation's
position in the routing rather than from its status alone.

## 6. The draft

`GenericRequestDraft` gains `SequenceSource` (the joined source string, FR-016),
`PartStage` (now written by the reading, not by the requester) and `QuantityReading`. Changing the
part clears `Sequence`, `SequenceSource`, `PartStage` and `QuantityReading` (FR-008).

## 7. Live implementation

`MTM_Waitlist.GenericRequest.Data/Services/GenericRequestPartData.cs` implements the seam.

| Read | Infor Visual | WIP floor |
| --- | --- | --- |
| completions | `GetPartMatches.sql` (`PartNumber`, `Description`) | `GetWipInventoryPartMatches.sql` |
| sequences | `GetPartSequences.sql` | `GetWipPartOperations.sql` |
| quantity | `GetPartWarehouseQuantities.sql` (closed) and `GetPartSequenceState.sql` (state at read time) | `GetWipOperationQuantity.sql` (open) |

The completion read asks both systems and merges the rows; the sequence read merges three sources and
counts a number once. A read that one system could not answer still returns the other's rows when it
found any, and reports `Unavailable` only when neither system answered.
