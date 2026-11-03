# Implementation Plan: Generic Work Center — Stated Part, Sequences and Quantity

## Scale note

This is an oversized change. It touches roughly thirty files across two UI libraries, one new data
library, five checked-in query scripts against two external systems, one new seam, one extended card
model and a new test area. A reader should watch two lines. The **live line**: every part, sequence and
quantity is read live at the moment it is shown, and no mirror shape is added for any of them (FR-017).
The **stage line**: the requester no longer says whether the part is work in progress or a finished
good; the chosen sequence's operation state decides, and the screen shows that decision rather than
asking for it (FR-009, FR-012).

## Summary

Make the part answer real. The requester states a part, completed from Infor Visual and from the WIP
floor's own records; the app answers with every sequence it can attribute to that part, each once, with
the source that supplied it; the chosen sequence's operation state then decides which system owns the
quantity, and the quantity is shown as units on hand with the system that answered, or as not
inventoried for outside-service work. Every read is live and none is served from the `mtm_mock` mirror,
because FR-017 forbids substituting a cached sequence list or quantity. The part screen already exists
in skeleton form; this chunk fills its three reads and extends the existing card rather than replacing
either.

## What must land first

The generic flow itself is not built yet. No implementation of `IGenericRequestStepData` exists, no
`IGenericRequestNavigator` implementation exists, and the shipped application neither references
`MTM_Waitlist.GenericRequest` nor hosts its pages (spec 016 owns that work and has no plan yet). This
chunk therefore adds a part-shaped data seam and its live implementation, which can be built and tested
on its own, and it leaves the wiring of the screens into the shell to 016. Implementation of this chunk
cannot be verified end to end until 016 lands.

## Project Structure

```text
MTM_Waitlist.GenericRequest/
  Contracts/            IGenericRequestPartData (new), GenericRequestStep (stage meaning documented)
  Models/               GenericRequestOptions (part, sequence, source, reading), GenericRequestDraft,
                        GenericRequestQuantityReading (new result type)
  ViewModels/           GenericRequestPartViewModel (its body rewritten around three reads)
  Views/                GenericRequestPartPage (completion box, sequence list, quantity region)
  Strings/en-us/        Resources.resw (new part, sequence and quantity keys)
MTM_Waitlist.GenericRequest.Controls/
  Models/               GenericRequestCardModel (chosen sequence, on-hand figure with its system)
  Helpers/              GenericRequestCardRules (draws the new cells, keeps the no-placeholder rule)
  Strings/en-us/        Resources.resw (new cell labels)
MTM_Waitlist.GenericRequest.Data/          (new project: the live side of the seam)
  Services/             GenericRequestPartDataSource, part and sequence row mapping
  Services/DependencyInjection/  ModuleDependencyInjectionExtensions
Database/InforVisual/Queues/Module_Waitlist/Queries/
  GetPartMatches.sql                       (extended: adds the part description column)
  GetPartWarehouseQuantities.sql           (reused unchanged, run live-only)
  GetPartSequences.sql                     (new)
  GetPartSequenceState.sql                 (new)
Database/MTMWipApp/Queues/Module_Waitlist/Queues/
  GetWipInventoryPartMatches.sql           (new)
  GetWipPartOperations.sql                 (new)
  GetWipOperationQuantity.sql              (new)
MTM_Waitlist.Tests/Module_GenericRequest/
  GenericRequestPartDataSourceTests.cs     (new)
  GenericRequestPartViewModelTests.cs      (new)
  GenericRequestCardRulesTests.cs          (extended)
```

**Structure Decision**: the live reads go in a new `MTM_Waitlist.GenericRequest.Data` library rather
than inside the screens library. The screens library references only `MTM_Waitlist.Core`; a live reader
needs the Infor Visual executor and the floor scripts, and putting those behind the screens would make
every data test drag the WinUI assembly with it. Every other module follows the same split between a
view library and its services, and the new project gives 016 the natural home for the rest of the live
step data.

## Interaction path

```text
calls prove-part
The completion box hands the typed term to the part seam, which reads Infor Visual and the WIP floor and merges both answers, each row carrying the system that supplied it.
A row the requester picks becomes the stated part, and the part is only accepted when at least one source named it.
The seam then reads every sequence attributable to that part: Visual's own-part sequences, Visual's sequences where the part is a requirement at an operation, and the floor's held operations.
The merge counts a sequence number once and keeps every source that supplied it, so a number two systems agree on is one row with two sources.
```

```text
calls read-quantity
Choosing a sequence asks the seam for the quantity, which re-reads that sequence's operation state because it may have closed since the list was drawn.
An open operation reads the floor by part and sequence; a closed one reads Infor Visual by part number; an outside-service operation is answered as not inventoried without reading anything.
Where Visual records no state for the sequence, the floor is read first and Visual only when the floor holds none, and the reading names the system that answered.
```

```text
calls draw-card
The card model carries the chosen sequence and the on-hand reading, and the rules add a cell only for a value the request actually has.
A reading of zero draws the cell, because zero is a fact; an unread quantity draws no cell; an outside-service part draws one not-inventoried cell instead of a number.
```

## States

```text
Idle: No term has been typed, so nothing has been asked of either system. shows part
Completing: A term is typed and both sources are being read for it. shows part
NoMatch: The read succeeded and neither system named the term; the flow refuses to advance and names both sources. shows part
Searched: At least one system named the part; each row shows the system that supplied it. shows part
PartUnavailable: One or both systems could not be reached for the part read; the step carries the attempt time and a retry and accepts no unverified part. shows part
Sequences: A part is accepted and every attributable sequence is listed once with its sources. shows part
NoSequence: The part is known and no sequence can be attributed to it; the step says so and refuses to advance. shows part
SequenceChosen: A sequence is chosen and its quantity is being read. shows part
OnHand: The owner holds a figure, including zero, and the step names the system that answered. shows part
NotInventoried: The sequence is outside service, so the step says the part is not inventoried and shows no number. shows part
QuantityUnavailable: The owning system could not be reached for the quantity; the step carries the attempt time and a retry and the flow does not advance. shows part
Cleared: The part changed, so the chosen sequence and the reading are gone and the step is back at Sequences. shows part
```

```text
Readable: The card draws the part, the chosen sequence and the on-hand figure with its system, or one not-inventoried cell, or no quantity cell at all. shows card
```

## Screens

```screen part State the part, choose its sequence and read the quantity on hand.```
```screen card Read one generic part request: its part, its sequence and its quantity on hand with the system that answered.```

## Open design decisions — owner confirmation required before the screen is built

The spec's design note of 2026-11-03 records that the review host which rendered this screen has been
deleted, so no approved rendering exists. The decisions below are **recommendations, not settled
design**. They are listed here so the owner can confirm or correct each one before implementation.

| # | Question the spec leaves open | Recommended resolution | Why |
|---|---|---|---|
| OD-1 | How one screen carries three independent reads (part, sequences, quantity) when `GenericRequestStepFrame` presents one state for the whole body | The frame keeps governing the part read; the sequence list and the quantity region each carry their own inline unavailable line with that read's own attempt time and retry | A step-level state cannot say "the list is fine, the quantity read failed" without hiding the list the requester is looking at, and FR-017 is per value: no unverified part, sequence or quantity is accepted |
| OD-2 | Whether the WIP / finished-good stage selector stays on screen | Remove the selector; show the derived stage as a read-only label beside the chosen sequence | FR-012 forbids asking which system to use and FR-009 derives the owner from the operation state, so the selector asks a question the app must not ask. Removing it entirely rather than labelling it is the smaller change if the owner prefers |
| OD-3 | The exact wording of the refusal, not-inventoried and quantity lines | Use the five keys pinned in `contracts/ui-identifiers.md`, English only for now | These are the words a person reads at the machine; the review host that would have shown them is gone, so they need sign-off rather than a guess |
| OD-4 | The control the requester types a part into | `AutoSuggestBox` from `Microsoft.UI.Xaml.Controls`, suggestions drawn from both systems with the source named in each row | The current `TextBox` plus `ListView` pair filters a list but does not complete as you type, which FR-001 asks for; `AutoSuggestBox` is the platform control for exactly this |
| OD-5 | How "every source it came from" appears in a sequence row | Secondary line under the sequence number, sources joined in a fixed order (`Infor Visual (own part)`, `Infor Visual (requirement)`, `WIP floor`), and the same joined string recorded on the request | FR-004 wants every source visible and FR-016 wants the source recorded; rendering one ordered set into both keeps the list and the record from disagreeing |

## Open questions that are not screen design

1. **The floor's operation column is unconfirmed, and FR-004, FR-006 and FR-011 depend on it.** The
   three checked-in floor scripts read `inv_inventory (PartID, Location, Quantity, ItemType)` and
   `md_locations (Location, Building)`; the confirmed disposition research in
   `RequestDispositionStatusCodes.cs` states the floor's inventory is *keyed by part, location and
   operation*, and this spec's edge case says the live operation value is sometimes `N/A`. The exact
   column name has never been read by this repository. **Recommended resolution:** confirm the column
   against the live floor schema before the two sequence scripts are written, and write them so an
   `N/A`, blank or non-numeric operation yields no sequence (FR-006). Until it is confirmed, the floor
   contributes no sequences, and US1 scenario 3 cannot pass.
2. **Whether the floor's part records carry a description.** The spec's edge case says a floor record
   may have none, which implies a part master this repository has never read. **Recommended
   resolution:** read the description where it exists and render the number and source alone where it
   does not; do not add a floor table to this chunk.

## Constitution Check

| Principle | Assessment |
| --- | --- |
| I. Spec-First, Verified Delivery | PASS — this plan and the task list precede the code; no task is ticked before the build and the suite are green. |
| II. Live Data Integrity — Internal Stores Are Never Mocked | PASS — no internal store is read or written by this chunk. Infor Visual is read live through the existing executor, and FR-017 forbids the mirror for a sequence or a quantity, so no fallback path is added. The floor is read live, as Principle II requires. |
| III. Stored-Procedure-First Database Discipline | PASS — no `mtm_waitlist` artefact and no inline SQL. The new scripts are checked-in external-system query scripts run through the existing executors, which is the established pattern for Infor Visual and the WIP floor, and the request record rides on the existing `answers_json` column and `sp_waitlist_request_insert`. |
| IV. MCP-First, Grounded Decisions | PASS — `AutoSuggestBox` and its namespace were checked against Microsoft Learn; every repository fact here was read from source or from the schema exports, including the floor's key columns and the confirmed Visual status codes. |
| V. WinUI 3 Platform Conformance | PASS — `Microsoft.UI.Xaml` only, MVVM Toolkit generators, `ThemeResource` brushes, text styles, localized strings from `.resw` with `GetLocalized`, automation names on interactive elements, and no literal colours or sizes. |
| VI. Evidence-Based Verification Gates | PASS — a clean solution build and a green suite are the completion gate, and the query scripts are exercised by unit tests over a fake executor. |
| VII. Readable, End-User-Facing Delivery | PASS — the reply says what changed and what is still open, in plain language. |

No violations, so there is no Complexity Tracking table.

## Research Summary

Full entries are in [research.md](./research.md). The load-bearing choices are: all three reads are
live-only because FR-017 forbids a cached sequence or quantity; the existing Visual scripts are reused
where their projection already answers the question, and the shipped fallback classes are bypassed so
the mirror cannot answer; the floor's reads join the existing floor service and, unlike their siblings,
report whether they succeeded; the part step gets a narrow seam rather than an edit to the wide 015
seam; and the quantity reading is its own result type, because zero, not-inventoried and unread are
three different answers and the card must not conflate them.

## Contracts

- [contracts/step-data.md](./contracts/step-data.md) — the part-shaped data seam and its reading types.
- [contracts/navigation.md](./contracts/navigation.md) — what the part step must have before it advances.
- [contracts/ui-identifiers.md](./contracts/ui-identifiers.md) — the pinned names a consumer, the card or
  a test codes against: seam members, row shapes, script paths, resource keys and card labels.
