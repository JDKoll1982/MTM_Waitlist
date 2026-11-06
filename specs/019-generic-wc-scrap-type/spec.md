# Feature Specification: Generic Work Center — Stated Scrap Type and Lugger

**Feature Branch**: `019-generic-wc-scrap-type`
**Created**: 2026-10-29
**Status**: Draft
**Input**: Chunk 5 of 8 carved from the feature specification kept at [specs/015-generic-wc-ui-shell/PARENT-SPEC.md](../015-generic-wc-ui-shell/PARENT-SPEC.md). The owner's requirement: "If picking up scrap the requester is responsible for stating the scrap type during the request process." A work-order scrap request reads the scrap decision from its job; at a generic work center there is no job, so the requester states it. The lugger that receives it already comes from the arrangement shipped in spec 013.

## User Scenarios & Testing

### User Story 1 - The requester states the scrap type (Priority: P1)

A work center has scrap to move. There is no job to read a scrap decision from, so the wizard asks the operator which scrap type it is. The list is the plant's one scrap-type list — the same one the Setup scrap picker and the lugger arrangement editor already use — plus any type the arrangement records that falls outside it. No choice, no advance.

**Why this priority**: scrap is a physical pickup that cannot be deferred, and the stated type is the only thing that decides where it goes. It is the whole of this chunk.

**Independent Test**: raise a generic scrap request and confirm the choices match the shared scrap-type list in its own order; then try to continue without choosing and confirm the flow does not advance.

**Acceptance Scenarios**:

1. **Given** the scrap item on a generic request, **When** the step is entered, **Then** it asks the requester to state the scrap type and reads no job.
2. **Given** the scrap-type question, **Then** the choices are the plant's shared scrap-type list in that list's own order, together with any type the arrangement records that is outside the list.
3. **Given** the question, **When** the requester chooses nothing and tries to continue, **Then** the flow does not advance.
4. **Given** a chosen type, **When** the requester continues, **Then** the type is carried on the confirmation step, on the card and in the stored request.
5. **Given** the flow, **Then** the requester is never offered the not-a-scrap answers — the "scrap type required" placeholder and "no scrap" — because they answer whether there is scrap, not where it goes.
6. **Given** a chosen type, **When** the requester continues, **Then** the request goes straight to the confirmation and is never asked for a description.
7. **Given** a chosen type, **When** the requester goes back to the step, **Then** the type they chose is still shown as chosen.

---

### User Story 2 - The card says where the scrap goes (Priority: P1)

A handler reads a generic scrap card and sees the scrap type and the lugger it dumps into, resolved by the arrangement that already governs every other scrap card — including its rule that a full lugger hands over to its backup.

**Why this priority**: a scrap card that names the type but not the lugger sends the handler to ask somebody, and naming a blocked lugger is worse than naming none.

**Independent Test**: raise generic scrap requests for each configured type and confirm each card names the lugger the arrangement assigns; then mark that lugger full and confirm the card follows the arrangement to its backup.

**Acceptance Scenarios**:

1. **Given** a chosen scrap type, **When** the card is drawn, **Then** it names the type and the lugger the arrangement assigns to it.
2. **Given** that lugger is set to full, **When** the card is drawn, **Then** it names the backup the arrangement promotes.
3. **Given** a type whose luggers are all blocked, **When** the card is drawn, **Then** it names the type and the first lugger in the arrangement's order with its status beside the name, and draws nothing blank.
4. **Given** a type with no lugger assigned at all, **When** the card is drawn, **Then** it names the type and records the missing assignment as a configuration gap rather than inventing a lugger.
5. **Given** a generic scrap card and a work-order scrap card for the same type, **Then** both name the same lugger.

---

### User Story 3 - The stated type survives the arrangement changing (Priority: P2)

A scrap type is edited or removed from the arrangement after a generic request carrying it was raised. The request keeps the wording the requester stated, because that is what the plant was told, and its lugger cell reports the missing assignment rather than showing a lugger that no longer applies.

**Why this priority**: a request that quietly rewrites itself is worse than one that says it has a gap; the handler needs to know to ask, not to walk somewhere wrong.

**Independent Test**: raise a generic scrap request, remove its type from the arrangement, and confirm the card still names the stated type and reports the lugger as unassigned.

**Acceptance Scenarios**:

1. **Given** a scrap type removed from the arrangement, **When** an existing request carrying it is drawn, **Then** the stated type is unchanged.
2. **Given** that request, **When** its card is drawn, **Then** the lugger cell reports the missing assignment and does not show an old lugger.
3. **Given** a scrap type renamed in the arrangement, **When** an existing request carrying the old wording is drawn, **Then** the request keeps the wording the requester stated.
4. **Given** the type is reassigned to a different lugger, **When** an existing request is drawn, **Then** it names the lugger the arrangement assigns now, because the dump point is a live fact.

---

### Edge Cases

- Two scrap types differ only in letter case or surrounding spaces: they resolve to the same arrangement entry, as the existing scrap matching already does.
- The arrangement cannot be read, so the types it adds to the shared list are unknown: the step reports the choices unavailable, with the time of the attempt and a retry, and offers no list at all rather than the shared list alone, because a shorter list would silently drop types the plant ships against.
- The lugger arrangement cannot be read: the card names the type and reports the lugger as temporarily unavailable, and does not invent one.
- A requester picks a type the arrangement does not carry: the choices are the list, so this can only happen through the add control, which writes the type to the arrangement before it is offered — a stored request carrying an outside type must still draw with its type named.
- The requester raises several scrap requests for different types in one shift: each is its own request with its own lugger.

## Requirements

### Functional Requirements

**Stating the type**

- **FR-001**: For the scrap item, the step MUST ask the requester to state the scrap type and MUST read no job.
- **FR-002**: The choices MUST come from the plant's single shared scrap-type list, in that list's order, plus any type the arrangement records that falls outside it; no second scrap-type list MAY be introduced.
- **FR-003**: The choices MUST NOT include the answers that mean "there is no scrap" — the required placeholder and the no-scrap value — because they are not destinations.
- **FR-004**: The flow MUST NOT advance without a chosen type.
- **FR-005**: A chosen type MUST be carried on the confirmation step, on the card and in the stored request, in the wording the requester chose.

**Resolving the lugger**

- **FR-006**: A generic scrap card MUST name the lugger the arrangement assigns to the stated type, following the arrangement's blocking and backup rules, and MUST NOT read a job to find it.
- **FR-007**: The lugger resolution MUST be the same one the work-order scrap card already uses, so the two cannot disagree for the same type.
- **FR-008**: A type whose luggers are all blocked MUST draw the type and the first lugger in the arrangement's order with its status beside the name, never blank.
- **FR-009**: A type with no lugger assigned MUST draw the type and record the missing assignment as a configuration gap, never an invented lugger.
- **FR-010**: Where the arrangement cannot be read, the card MUST name the type and report the lugger as unavailable, and MUST NOT show a stale or guessed lugger.

**Surviving change**

- **FR-011**: A type removed from, or renamed in, the arrangement MUST leave an existing request's stated wording intact.
- **FR-012**: A type reassigned to a different lugger MUST show the lugger assigned now, because the dump point is a live fact.
- **FR-013**: A stored request carrying a type outside the shared list MUST still draw with that type named.
- **FR-014**: This chunk MUST delete the `Temp` preview source behind the scrap screen and the scrap card variant, MUST leave the build green afterwards, and MUST NOT rename such an artefact into a permanent-looking name in place of removing it.

**Where the request goes**

- **FR-015**: A scrap request MUST go from the scrap step straight to the confirmation, MUST NOT visit the details step, and MUST record no description.
- **FR-016**: Going back from the confirmation MUST return to the scrap step with the chosen type still selected.

**Adding a type the list does not carry**

- **FR-017**: The scrap step MUST offer an add control through which a type the list does not carry can be given to a lugger the person names, writing the same arrangement assignment the lugger arrangement editor writes. The type MUST be offered on the step afterwards and chosen, and MUST be offered to every later scrap request because it is now part of the arrangement. A type the list already carries MUST be chosen rather than written, so the arrangement gains no duplicate its own matching would treat as the same entry.
- **FR-018**: The add control MUST be drawn only for a person who holds the permission that governs the lugger arrangement, because assigning a type decides where every scrap request for that type dumps, the work-order cards included. For everyone else it MUST be absent, not disabled.
- **FR-019**: A failed write MUST be reported on the step and MUST NOT add the type to the choices, choose it, or advance the flow.

### Key Entities

- **Stated scrap type**: the scrap type the requester chose, kept in the wording they chose.
- **Scrap-type list**: the plant's one shared list, already used by the Setup scrap picker and the lugger arrangement editor.
- **Lugger resolution**: the lugger the arrangement assigns to a stated type at the time the card is read, with the arrangement's blocking rules applied, or an explicit configuration gap.
- **Added scrap type**: a type the list does not carry, given to a lugger through the step's add control by a person who maintains the arrangement, after which it is part of the arrangement and offered to every later request.

## Success Criteria

### Measurable Outcomes

- **SC-001**: A requester at a generic work center states a scrap type and raises the request in under 30 seconds from opening the step.
- **SC-002**: Zero generic scrap cards are drawn without a scrap type, and zero are drawn blank where a lugger should be named.
- **SC-003**: For the same scrap type, a generic card and a work-order card name the identical lugger, in 100% of cases.
- **SC-004**: Blocking a lugger moves the generic card to its backup without any other edit, verified by walking the arrangement's four transitions.
- **SC-005**: Zero generic scrap requests lose their stated type when the arrangement changes.
- **SC-006**: Zero generic scrap requests are asked for a description, and zero visit the details step.

## Scope & Dependencies

**Builds on**: spec 015 (the scrap screen's layout and preview states) and spec 016 (the generic flow, the confirmation step, the queue path and the card that this chunk fills).

**Uses, already shipped**: spec 013 — the lugger arrangement, its blocking and backup rules, and the shared scrap-type list.

**Deliberately absent**: the stated part and its quantity (017), the dunnage page (018), the die search (020), the optional job fill (021). None is needed to state a scrap type or resolve its lugger.

**Not changed by this chunk**: the work-order scrap card keeps behaving as it does today; this chunk makes the generic card use the same resolution.

## Assumptions

- **A1**: The plant's shared scrap-type list is the one the Setup scrap picker and the lugger arrangement editor already ship; the generic flow reads that list and does not restate it.
- **A2**: The lugger a type dumps into is a live read of the arrangement, not a value copied onto the request when it is raised — with the stated type being the value the request keeps.
- **A3**: Scrap types recorded against a lugger but outside the shared list remain valid, as they already are for the arrangement editor.
- **A4**: The confirmation step already names the chosen type, and the route from an answer step straight past the details step is already approved by spec 016. This chunk supplies the answer that feeds both, and adds neither.
- **A5**: The shared list always ships with the plant's types, so the choices can only be empty when a read failed, which is the unavailable state above. There is no separate "no scrap types" state.

## Design note (2026-11-03)

The review host that rendered this screen against in-memory values has been deleted, so no approved rendering of it can
be opened any more; only this specification and the review record remain. **Clarification about design is required when
this chunk is implemented**, before the screen is built: what the step shows, what it shows when its source is
unavailable, and the exact wording.
