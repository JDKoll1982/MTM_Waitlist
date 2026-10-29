# Feature Specification: Generic Work Center — Stated Part, Sequences and Quantity

**Feature Branch**: `017-generic-wc-stated-part`
**Created**: 2026-10-29
**Status**: Draft
**Input**: Chunk 3 of 8 carved from the feature specification kept at [specs/015-generic-wc-ui-shell/PARENT-SPEC.md](../015-generic-wc-ui-shell/PARENT-SPEC.md). Chunk 2 (spec 016) made the generic flow real and left every answer that needs an externally proved value reporting its source as unavailable. This chunk makes the part answer real: the requester states a part, the app returns the sequences attributable to it, and the chosen sequence decides which system owns the quantity.

## User Scenarios & Testing

### User Story 1 - A stated part is proved, and its sequences are listed (Priority: P1)

A machinist needs a part brought to their work center. They type the number into a box that completes as they type, drawing on Infor Visual and on the WIP floor's own records. The app then answers with the sequences it can attribute to that part — from Visual where Visual records them, from the floor where the floor holds the part at a sequence. Every sequence appears once, with its number, its description where one exists, and the source that supplied it.

**Why this priority**: it is the only thing standing between a free-typed part number and a handler walking to a bin that does not exist, and the sequence is what every later value hangs on.

**Independent Test**: type a part that carries several sequences and confirm all of them are listed once with sources; type a part Visual does not know but the floor holds and confirm it is accepted with the floor named; type a number neither knows and confirm it is refused.

**Acceptance Scenarios**:

1. **Given** the part step, **When** the requester types a number, **Then** completions are drawn from Infor Visual and from the WIP floor's part records, and the accepted number must be one of them.
2. **Given** an accepted part, **When** the sequence list returns, **Then** it lists every attributable sequence once — Visual's own-part sequences, Visual's requirement-at-an-operation sequences, and the floor's held sequences — each with its number, its description where one exists, and its source.
3. **Given** a part Infor Visual does not know but the floor holds at one or more sequences, **When** the step continues, **Then** the part is accepted, its sequences come from the floor, and the floor is named as the source.
4. **Given** a part number neither Infor Visual nor the floor knows, **When** the requester tries to continue, **Then** the flow refuses to advance and names both sources searched.
5. **Given** a part both know but no sequence can be attributed to, **When** the requester tries to continue, **Then** the flow refuses to advance and says no sequence was found.
6. **Given** a sequence whose every operation is cancelled, **When** the list returns, **Then** it is not offered.
7. **Given** a floor record whose operation is not a usable sequence number — the live data carries `N/A` — **Then** it is not offered as a sequence.
8. **Given** the sequence list, **When** the requester changes the part, **Then** the chosen sequence and any quantity already read are cleared.

---

### User Story 2 - The chosen sequence decides where the quantity comes from (Priority: P1)

The requester picks the sequence the part is actually standing at, and the app asks the system that owns it. A sequence still open means the part is work in process, so the floor is asked by part and sequence. A sequence already closed means the part is a finished good, so Visual is asked by part number. A sequence routed to an outside vendor is inventoried nowhere, and the app says so rather than claiming zero.

**Why this priority**: the quantity is the fact the handler acts on, and attributing it to the wrong system produces a confident wrong number — the exact failure this repository's truthfulness rules exist to prevent.

**Independent Test**: choose an open sequence and a closed sequence for the same part and confirm the two quantities came from different systems; choose an outside-service sequence and confirm the step says not inventoried; choose a sequence Visual has no state for and confirm the floor is tried first.

**Acceptance Scenarios**:

1. **Given** no sequence chosen yet, **When** the step is shown, **Then** no quantity is read and the flow does not advance.
2. **Given** a chosen sequence whose operation is open — released, unreleased or firmed — **When** the quantity is read, **Then** it comes from the WIP floor by part and sequence.
3. **Given** a chosen sequence whose operation is closed, **When** the quantity is read, **Then** it comes from Infor Visual by part number.
4. **Given** a chosen sequence routed to outside service, **When** the step is shown, **Then** it says the part is not inventoried while it is outside, shows no number rather than `0`, and the requester may still raise the request.
5. **Given** a chosen sequence for which Visual records no operation state — including one the floor supplied — **When** the quantity is read, **Then** the floor is read first for that part and sequence, Visual by part number only when the floor holds none, and the step names the system that answered.
6. **Given** a quantity read, **When** the step is shown, **Then** it states the units on hand — `0` where none — with the system that answered, and never a blank, a dash or an estimate.
7. **Given** nothing on hand, **When** the requester continues, **Then** they may still raise the request having seen a warning, and the request carries the part, the sequence, the quantity shown and the system that answered.
8. **Given** the flow, **Then** the requester is never asked which system to use and never asked to type a quantity.

---

### User Story 3 - The part answer reaches the card (Priority: P2)

A handler reads a generic part request's card and sees the part, the sequence, and the quantity with the system that supplied it — or, for outside-service work, that the part is not inventoried. Nothing is blank and nothing is guessed.

**Why this priority**: the card is the surface the handler trusts; a quantity that never reaches it leaves the request unusable in practice.

**Independent Test**: raise a generic part request, view its card and detail, and confirm the part, sequence, quantity and source all appear, and that an outside-service request says not inventoried.

**Acceptance Scenarios**:

1. **Given** a generic part request, **When** its card is drawn, **Then** the part number, the chosen sequence and the quantity on hand with its source appear.
2. **Given** an outside-service request, **When** its card is drawn, **Then** it says the part is not inventoried and shows no number.
3. **Given** a generic part request whose quantity could not be read at raise time, **When** its card is drawn, **Then** the quantity cell is absent and the gap is not filled with a placeholder.

---

### Edge Cases

- The same part number carries different sequence numbers on different orders: the list is the distinct union and each number appears once, with every source it came from.
- A chosen sequence's operation closes between the list being shown and the quantity being read: the read uses the state at read time and names the system that answered.
- The owning system is unreachable when the quantity is read: the step reports it unavailable with the time of the attempt and a retry, and accepts no unverified part or sequence.
- The floor holds a part at a sequence Visual records no operation for: the floor answers directly and is named as the source.
- The floor holds a part against an operation that is not a usable sequence number: it cannot distinguish a stage, so it is not offered; the part is refused only when no other source yields a sequence.
- Both systems are unreachable: no request can be raised from the step, and neither a cached sequence list nor a cached quantity is offered.
- A part's description is missing in the floor's records: the row shows the number and source without an invented description.

## Requirements

### Functional Requirements

**Proving the part**

- **FR-001**: The part entry MUST offer completions drawn from Infor Visual and from the WIP floor's part records, and the accepted number MUST be one of them.
- **FR-002**: A number neither system knows MUST be refused with a message naming both sources searched, and the flow MUST NOT advance.
- **FR-003**: A part for which neither system yields a sequence MUST be refused with a message saying so, and the flow MUST NOT advance.

**Listing the sequences**

- **FR-004**: For an accepted part the system MUST list every attributable sequence — Visual's sequences where the part is the work order's own part, Visual's sequences where it appears as a requirement at an operation, and the sequences the floor holds it at — each once, with its sequence number, its description where one exists, and the source that supplied it.
- **FR-005**: A sequence whose every operation is cancelled MUST NOT be offered.
- **FR-006**: A floor record whose operation is not a usable sequence number MUST NOT be offered as a sequence.

**Choosing the sequence**

- **FR-007**: The requester MUST choose one sequence before any quantity is read, and the chosen sequence MUST travel with the request.
- **FR-008**: Changing the part MUST clear the chosen sequence and any quantity already read for it.

**Attributing the quantity**

- **FR-009**: The chosen sequence's operation state in Infor Visual MUST decide the owner: open (released, unreleased or firmed) reads the WIP floor by part and sequence; closed reads Infor Visual by part number.
- **FR-010**: A sequence routed to outside service MUST NOT have a quantity read, because outside-service material is inventoried nowhere.
- **FR-011**: Where Infor Visual records no operation state for the chosen sequence — including one the floor supplied — the floor MUST be read first for that part and sequence, and Infor Visual only when the floor holds none.
- **FR-012**: The requester MUST NOT be asked which system to use, and MUST NOT be asked to type a quantity.

**Reporting the quantity**

- **FR-013**: The quantity MUST be shown as units on hand — `0` when the owner holds none — with the system that answered; it MUST NOT be blank on that ground, a dash, or an estimate.
- **FR-014**: An outside-service sequence MUST be reported as not inventoried rather than as `0`.
- **FR-015**: Nothing on hand MUST warn and MUST NOT block the request.
- **FR-016**: The request MUST record the part, the chosen sequence, which source supplied the sequence, the quantity shown and the system that supplied it — or that the part is not inventoried.
- **FR-017**: Where a system cannot be reached, the step MUST report it unavailable with the time of the attempt and a retry, MUST accept no unverified part or sequence, and MUST NOT substitute a cached, mirror, sample or fabricated sequence list or quantity.

**Reaching the card**

- **FR-018**: A generic part request's card MUST show the part, the chosen sequence and the quantity with its source.
- **FR-019**: An outside-service request's card MUST say the part is not inventoried and show no number.
- **FR-020**: A quantity that could not be read MUST leave the cell absent rather than filled with a placeholder.
- **FR-021**: This chunk MUST delete the `Temp` preview source behind the part and sequence screens, MUST leave the build green afterwards, and MUST NOT rename such an artefact into a permanent-looking name in place of removing it.

### Key Entities

- **Stated part**: the part number the requester named, the sequence they chose, which source supplied that sequence, which system supplied the quantity, and the quantity on hand — or the fact that the part is not inventoried.
- **Attributable sequence**: a sequence number the part can stand at, with its description when one exists and the source or sources that supplied it.
- **Quantity reading**: a number of units on hand together with the system that returned it, or an explicit not-inventoried outcome.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Zero generic requests are accepted carrying a part number or sequence that neither Infor Visual nor the WIP floor can attribute.
- **SC-002**: A part number returns every attributable sequence in one search with none omitted, and no cancelled-only sequence is offered.
- **SC-003**: The quantity shown always comes from the system the chosen sequence's state assigns, verified by choosing an open and a closed sequence for the same part.
- **SC-004**: Zero part cards show a blank, dash or estimated quantity; nothing on hand reads as `0`, and outside service reads as not inventoried.
- **SC-005**: A part with nothing on hand is still raisable, with the warning visible afterwards, in 100% of cases.

## Scope & Dependencies

**Builds on**: spec 015 (the part and sequence screens) and spec 016 (the generic flow, the queue path and the card that this chunk fills).

**Deliberately absent**: the dunnage catalogue page (018), the stated scrap type (019), the die search (020), the optional job fill (021). None of them is needed to prove a part or read its quantity.

**Later chunks depend on this one only where they say so**: 018, 019 and 020 do not need the part sequence; 022 verifies this chunk's screens. No earlier chunk needs this one.

## Assumptions

- **A1**: "Every attributable sequence" is the distinct union of Visual's sequences and the floor's held sequences, because Visual holds sequences per routing rather than per part.
- **A2**: A sequence's stage is taken from the operation states the plant already relies on for disposition: released, unreleased and firmed are open; closed is a finished good; cancelled is excluded.
- **A3**: WIP is inventoried only in the WIP application, by part number and sequence into a location; Infor Visual holds what is left that is neither work in progress nor outside service; outside-service material is inventoried nowhere.
- **A4**: A by-sequence floor read is new work: the existing floor reads take only a part number and neither filters nor reports the operation.
