# Data Model: Generic Work Center — Screen Layer and Preview Host

This feature stores nothing. Every entity below is an in-memory shape: what a screen needs in order to render,
and what a later chunk will fill from a real read.

## GenericRequestStep

Enumeration of the flow's steps, in order.

| Value | Screen |
| --- | --- |
| `WorkCenter` | pick the work center |
| `Category` | pick the category |
| `Item` | pick the item |
| `Part` | state part, stage and sequence |
| `Dunnage` | choose dunnage from the catalogue |
| `Scrap` | state the scrap type |
| `Die` | search for and pick a die |
| `Details` | needed-by time, quantity, description |
| `Summary` | review |
| `Result` | outcome |

Validation: `Successor` exists for every value except `Result`, which has none.

## GenericRequestPresentation

Enumeration of the three presentations one screen can show: `Populated`, `Empty`, `Unavailable`. Exactly one
value is in force at a time (FR-006).

## GenericRequestStepResult<T>

The reply one read produces. Fields: `Presentation`, `Items`, `AttemptedAtUtc`.

| Rule | Source |
| --- | --- |
| `Unavailable` carries the attempt time and yields no items | FR-006 |
| `Empty` carries the attempt time and yields no items | FR-006 |
| `Populated` is only produced when at least one item was returned | derived from FR-006, so an empty list is never labelled populated |

## GenericRequestStepAvailability

The reply a step that reads no rows produces. Fields: `Presentation`, `AttemptedAtUtc`, no items.

Used by the details, summary and result steps, so every screen reports the same three presentations (FR-006)
without inventing an empty list to say "there is nothing to show".

## GenericRequestDraft

The answers given so far. Fields: `WorkCenter`, `Category`, `Item`, `PartNumber`, `PartStage`, `Sequence`,
`DunnageType`, `DunnagePart`, `ScrapType`, `DieNumber`, `Quantity`, `NeededBy`, `Description`.

Rules: a field is set by the step that owns it and is never cleared by navigation, so going back keeps every
answer (US1 scenario 4). A field is only read by a screen that shows it.

## Option entities

One row shape per pickable list, each carrying only what its screen draws.

| Entity | Fields |
| --- | --- |
| `GenericRequestWorkCenterOption` | `Name`, `Building`, `IsSelected` |
| `GenericRequestCategoryOption` | `Name`, `Description` |
| `GenericRequestItemOption` | `Code`, `Name`, `PicturePath` |
| `GenericRequestPartOption` | `PartNumber`, `Description`, `QuantityOnHand` |
| `GenericRequestSequenceOption` | `Sequence`, `Description` |
| `GenericRequestDunnageOption` | `TypeName`, `PartNumber`, `PicturePath` |
| `GenericRequestScrapOption` | `ScrapType`, `AvailableLuggerCount` |
| `GenericRequestDieOption` | `DieNumber`, `Description`, `Location` |

Rule: `PartStage` is one of `Wip` or `FinishedGood`, and the part screen is the only one that sets it.

## GenericRequestCardModel

What the generic card and its detail page read.

Fields: `RequestNumber`, `Variant` (`Part`, `Dunnage`, `Scrap`, `Die`), `WorkCenter`, `ItemName`,
`PartNumber`, `DunnageType`, `ScrapType`, `DieNumber`, `Quantity`, `NeededBy`, `RaisedBy`, `RaisedAt`,
`History` (list of `GenericRequestCardHistoryEntry` with `OccurredAt` and `Summary`).

Rules:
- `GenericRequestCardRules.CellsFor(variant)` returns the cells that variant draws, and nothing else
  (FR-015, FR-016).
- A cell whose value is absent from the model is not returned, so no cell renders blank or placeholder
  (FR-018).
- `RaisedBy`, `RaisedAt` and `History` appear on the detail page only, never on the card (US2 scenario 4).
