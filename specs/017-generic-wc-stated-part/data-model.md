# Data Model: Generic Work Center — Stated Part, Sequences and Quantity

**Date**: 2026-11-03 | **Plan**: [plan.md](./plan.md) | **Research**: [research.md](./research.md) | **Spec**: [spec.md](./spec.md)

This feature persists nothing. Every entity below is an in-memory shape: what the part step needs in
order to prove a part, list its sequences and read its quantity, and what the card draws. All three
reads are live-only (FR-017): no mirror shape, result table or `sp_visual_*` procedure is added for any
of them, and the shipped `VisualReadFallback` is bypassed for these values.

---

## 1. Step state machine (the part step)

The part screen is one step with three independent reads. Its state is the part read's state; the
sequence list and the quantity region each carry their own inline unavailable line (plan OD-1).

| State | Cause | Rule | Source |
| --- | --- | --- | --- |
| `Idle` | no term typed | nothing asked of either system | — |
| `Completing` | a term is typed | both sources are being read | FR-001 |
| `NoMatch` | read succeeded, neither system named the term | refuse to advance, name both sources | FR-002 |
| `Searched` | at least one system named the part | every row shows the system that supplied it | FR-001 |
| `PartUnavailable` | one or both systems could not be reached for the part read | carry attempt time + retry, accept no unverified part | FR-017 |
| `Sequences` | a part is accepted | every attributable sequence listed once with its sources | FR-004 |
| `NoSequence` | the part is known, no sequence attributable | refuse to advance, say so | FR-003 |
| `SequenceChosen` | a sequence is chosen | its quantity is being read | FR-007 |
| `OnHand` | the owner holds a figure, including zero | show the figure, name the system that answered | FR-013 |
| `NotInventoried` | the sequence is outside service | show not-inventoried, no number | FR-010, FR-014 |
| `QuantityUnavailable` | the owning system could not be reached for the quantity | carry attempt time + retry, do not advance | FR-017 |
| `Cleared` | the part changed | chosen sequence and reading are gone, back at `Sequences` | FR-008 |

**Transitions**: `Idle → Completing →` (`NoMatch` · `Searched` · `PartUnavailable`) ·
`Searched → Sequences → SequenceChosen →` (`OnHand` · `NotInventoried` · `QuantityUnavailable`) ·
`Sequences → NoSequence` · any state after `Sequences` `→ Cleared → Sequences` when the part changes.

**Invariants** — violations are defects:

1. `NoMatch` and `PartUnavailable` never advance; neither caches, fabricates or estimates the part.
2. `OnHand` shows `0` when the owner holds none, never a blank, a dash or an estimate (FR-013).
3. `NotInventoried` never reads a system and never shows a number (FR-010).
4. `QuantityUnavailable` is per value: the sequence list stays visible while the quantity region
   reports its own attempt time and retry (plan OD-1).

---

## 2. Stated part and attributable sequence

The three entities the spec names, reshaped from spec 015.

### GenericRequestPartOption (reshaped)

The completion row the requester picks, drawn from both systems. Spec 015 carried `PartNumber`,
`Description`, `QuantityOnHand`; the quantity leaves this shape because it belongs to a chosen
sequence (FR-007), and a source is added because every row must say which system supplied it (FR-001).

| Field | Type | Rule | Source |
| --- | --- | --- | --- |
| `PartNumber` | string | the accepted number; must be named by at least one source | FR-001, FR-002 |
| `Description` | string? | shown where one exists; omitted where it does not | spec edge case |
| `Source` | `SequenceSource` | `InforVisual` or `WipFloor` — the system that supplied the row | FR-001 |

Validation: a term is accepted only when at least one returned row carries it; a number neither system
named is `NoMatch` (FR-002).

### GenericRequestSequenceOption (reshaped)

One attributable sequence number with every source that supplied it. Spec 015 carried `Sequence`,
`Description`; this chunk adds the ordered source set and keeps the description optional.

| Field | Type | Rule | Source |
| --- | --- | --- | --- |
| `Sequence` | string | a usable sequence number; `N/A`, blank and non-numeric floor operations are excluded | FR-004, FR-006 |
| `Description` | string? | shown where one exists | FR-004 |
| `Sources` | ordered set of `SequenceSource` | every source that supplied the number, once each | FR-004 |
| `OperationStatus` | string? | the Visual status code, present only for the list's cancel filter | FR-005 |

Validation:
- A sequence whose every operation is cancelled (`X` is cancelled, in neither open nor closed set) is
  not offered (FR-005, R5).
- The same part number carrying different sequence numbers on different orders yields the distinct
  union, each number once (spec edge case).
- The source order is fixed — `InforVisual (own part)`, `InforVisual (requirement)`, `WIP floor` —
  and the joined string is what is recorded on the request (plan OD-5).

### SequenceSource (new)

Enumeration naming who supplied a sequence. Values: `VisualOwnPart`, `VisualRequirement`, `WipFloor`.
`VisualOwnPart` is Visual's sequence where the part is the work order's own part; `VisualRequirement`
is Visual's sequence where the part is a requirement at an operation; `WipFloor` is the floor's held
operation (FR-004, R4).

---

## 3. Quantity reading

### GenericRequestQuantityReading (new)

The answer the quantity read produces. Three outcomes must stay distinguishable — a figure (including
`0`), not inventoried, and could not be read — so the reading is its own result type rather than a
reused `GenericRequestStepResult<decimal>` (R8).

| Field | Type | Rule | Source |
| --- | --- | --- | --- |
| `Kind` | `OnHand` · `NotInventoried` | one of the two success kinds | FR-013, FR-014 |
| `Figure` | decimal? | units on hand, `0` where none; null when `NotInventoried` | FR-013, FR-014 |
| `AnsweredBy` | `WipFloor` · `InforVisual` | the system that supplied the figure; null when `NotInventoried` | FR-013, FR-016 |
| `Presentation` | `Populated` · `Empty` · `Unavailable` | inherited from the step result; `Unavailable` carries the attempt time and yields no figure | FR-017 |

Validation: `OnHand` requires a `Figure` and an `AnsweredBy`; `NotInventoried` requires neither.

### GenericRequestPartStage (reshaped meaning)

Spec 015's `Wip` / `FinishedGood` pair is no longer chosen by the requester. It keeps its meaning but
is derived from the chosen sequence's operation state, read again at quantity time because the state
may have closed since the list was drawn (FR-009, R4, spec edge case).

| Value | Operation state | Quantity owner | Source |
| --- | --- | --- | --- |
| `Wip` | open — released (`R`), unreleased (`U`) or firmed (`F`) | read the WIP floor by part and sequence | FR-009, R5 |
| `FinishedGood` | closed (`C`) | read Infor Visual by part number | FR-009, R5 |
| `OutsideService` | `OUTSIDE_SERVICE` shop resource or non-empty vendor/service field | read nothing; `NotInventoried` | FR-010, R5 |
| `Unknown` | Visual records no state — including a floor-supplied sequence | floor first, Visual by part only when the floor holds none | FR-011 |

Validation: `Unknown` names the system that answered; when the floor holds none and Visual has none,
the figure is `0` and `AnsweredBy` is the system that returned the zeros (FR-011, FR-013).

---

## 4. Draft and card

### GenericRequestDraft (reshaped)

Spec 015's answer bag gains the values this chunk produces, and loses the requester-chosen stage.

| Field | Type | Rule | Source |
| --- | --- | --- | --- |
| `PartNumber` | string? | set by the part read | FR-001 |
| `ChosenSequence` | string? | must be chosen before any quantity is read | FR-007 |
| `SequenceSource` | string? | the joined, ordered source string recorded with the sequence | FR-016, plan OD-5 |
| `PartStage` | `GenericRequestPartStage` | derived by the reading, not set by the requester | FR-009, R9 |
| `QuantityReading` | `GenericRequestQuantityReading`? | the on-hand reading, or not-inventoried | FR-013, FR-014 |
| `Quantity` | decimal? | the requested quantity, unchanged from spec 015 and distinct from on-hand | R8 |

Validation: changing the part clears `ChosenSequence`, `SequenceSource`, `PartStage` and
`QuantityReading` (FR-008). A field set by its step is never cleared by navigation (spec 015 US1 §4).

### GenericRequestCardModel (reshaped)

Spec 015's card gains the chosen sequence and the on-hand figure beside the requested `Quantity`.

| Field | Type | Rule | Source |
| --- | --- | --- | --- |
| `ChosenSequence` | string? | the sequence the request carries | FR-018 |
| `OnHandQuantity` | decimal? | on-hand figure, `0` where none | FR-018 |
| `QuantitySystem` | `WipFloor` · `InforVisual`? | the system that supplied the figure | FR-018 |
| `NotInventoried` | bool | true when the part is outside service; then no number and no system | FR-019 |

Validation (`GenericRequestCardRules`):
- A reading of zero draws the cell, because zero is a fact.
- An unread quantity draws no cell, and the gap is never filled with a placeholder (FR-020).
- An outside-service request draws one not-inventoried cell instead of a number (FR-019).
- A cell whose value is absent from the model is not returned (spec 015 FR-018).

---

## 5. Live-read shapes (query projections)

These are the row shapes the checked-in scripts return; they feed the entities above and persist
nothing. `SequenceSource` below is the column `GetPartSequences.sql` adds; the floor's operation
column is unconfirmed and must yield no sequence when `N/A`, blank or non-numeric (R11, FR-006).

| Script | System | Returns | Feeds |
| --- | --- | --- | --- |
| `GetPartMatches.sql` (extended) | Infor Visual | `PartNumber`, `Description` | `GenericRequestPartOption` |
| `GetWipInventoryPartMatches.sql` (new) | WIP floor | `PartNumber`, `Description?` | `GenericRequestPartOption` |
| `GetPartSequences.sql` (new) | Infor Visual | `Sequence`, `Description?`, `OperationStatus`, `OutsideService`, `SequenceSource` | `GenericRequestSequenceOption` |
| `GetWipPartOperations.sql` (new) | WIP floor | `Sequence` (distinct, usable) | `GenericRequestSequenceOption` |
| `GetPartSequenceState.sql` (new) | Infor Visual | `OperationStatus`, `OutsideService` | `GenericRequestPartStage` at read time |
| `GetWipOperationQuantity.sql` (new) | WIP floor | quantity by part and sequence, plus success | `GenericRequestQuantityReading` (open) |
| `GetPartWarehouseQuantities.sql` (reused, live-only) | Infor Visual | `WarehouseQuantity` (part-wide sum) | `GenericRequestQuantityReading` (closed) |

Rules: `GetPartSequenceState.sql` re-reads the state because the owner is decided from the state at
read time, never from the list's status (R4). Every floor read reports whether it succeeded, so "the
floor holds none" and "the floor could not be asked" stay distinct (R6, FR-017).

---

## 6. Data seam

### IGenericRequestPartData (new)

The narrow seam the part view model depends on, with exactly the three reads the step makes (R7).

| Member | Returns |
| --- | --- |
| `Completions(term)` | `GenericRequestStepResult<GenericRequestPartOption>` |
| `Sequences(part)` | `GenericRequestStepResult<GenericRequestSequenceOption>` |
| `Quantity(part, sequence)` | `GenericRequestQuantityReading` |

The wide `IGenericRequestStepData` from spec 015 is left untouched; spec 016's live implementation
forwards its part methods to the same data class (R7). `Unavailable` results carry the attempt time
and a retry, and never yield items (spec 015 FR-006, FR-017).
