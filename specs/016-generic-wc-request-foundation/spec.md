# Feature Specification: Generic Work Center — Designation and Request Foundation

**Feature Branch**: `016-generic-wc-request-foundation`
**Created**: 2026-10-29
**Status**: Draft
**Input**: Chunk 2 of 8 carved from the feature specification kept at [specs/015-generic-wc-ui-shell/PARENT-SPEC.md](../015-generic-wc-ui-shell/PARENT-SPEC.md). Chunk 1 (spec 015) laid every screen out against preview data; this chunk makes the generic flow real for every request item that does **not** need an externally proved part, dunnage part, scrap type or die. Those four arrive in 017 to 020.

## User Scenarios & Testing

### User Story 1 - The generic designation is maintained, not built in (Priority: P1)

A setup lead opens the work center catalog and marks the work centers that raise generic requests, and clears the mark when a work center goes back to running jobs. A person without that permission never sees the control. No work center is generic because a build said so, and an unmarked work center behaves exactly as it does today.

**Why this priority**: nothing else in the feature can be reached until a work center can be marked, and the mark has to survive the plant changing its mind without a code change.

**Independent Test**: as a permitted user, mark a work center, raise a request, clear the mark, raise another; confirm the first is generic and the second follows the work-order flow; then confirm an unpermitted person cannot reach the control.

**Acceptance Scenarios**:

1. **Given** a person holding the permission that already governs the work center catalog, **When** they open it, **Then** they can mark a work center generic and clear the mark.
2. **Given** a work center marked generic, **When** a request is started there, **Then** the generic flow is used.
3. **Given** the mark cleared, **When** the next request is started there, **Then** the work-order flow is used and no generic-only step is offered.
4. **Given** an in-progress generic request, **When** the mark is cleared, **Then** that request completes as generic.
5. **Given** a person without the permission, **When** they reach any route into the catalog, **Then** the mark is not visible and cannot be changed — absent, not disabled.
6. **Given** a fresh install with no configuration, **When** any request is started, **Then** no work center is generic and every one follows the existing flow.

---

### User Story 2 - A work center with no work order can raise a real request (Priority: P1)

A press operator works at a work center with nothing on the board. They open the wizard, pick that work center, and instead of the app reading a job it asks them what they need: which category, which item, and the few answers that item requires. They confirm, and a real request lands in the queue on the same board as every other request — carrying no work order anywhere.

**Why this priority**: this is the feature's spine. The four externally proved answers attach to it in later chunks; without it there is nothing to attach them to.

**Independent Test**: mark a work center generic, raise a request for an item whose answers are configured text or choices, and confirm the request appears on the board with no work order on any surface while a non-generic work center still uses the work-order flow.

**Acceptance Scenarios**:

1. **Given** a generic work center, **When** it is selected, **Then** the wizard continues without an active job and never fails for a missing job.
2. **Given** a generic work center, **When** the wizard continues, **Then** the category step offers the same categories and the item step the same items a work-order request offers.
3. **Given** an item whose answers are configured text or choices, **When** the flow reaches them, **Then** the requester answers them and no job value is read.
4. **Given** every required answer given, **When** the requester confirms, **Then** one request is written to the queue and appears on the board.
5. **Given** a non-generic work center, **When** it is selected, **Then** the existing work-order flow runs unchanged.
6. **Given** a generic work center with a running job, **When** a request is raised there, **Then** the job is not read and the request is generic.
7. **Given** a generic request raised, **Then** no work order number is stored against it and none is displayed on its card, its detail or any export.
8. **Given** a request abandoned mid-flow or the app closed, **Then** nothing reaches the queue and no partial request is stored.
9. **Given** an item whose answer needs a proved part, dunnage part, scrap type or die, **When** the flow reaches that answer step, **Then** it reports that the answer source is not yet available and does not offer a text box or any substitute.

---

### User Story 3 - The request carries an honest time and an honest ask (Priority: P2)

A handler arranging the day sees how long the request should take, taken from the time already configured for that item; a generic request and a work-order request for the same item show the same figure. An item with no configured time shows nothing rather than a default. The requester's needed-by answer and their optional description travel with the request and appear on the card.

**Why this priority**: the estimate is what makes a request schedulable, and it is the one value that must be identical across both flows or the board cannot be trusted.

**Independent Test**: raise a generic request for an item with configured time and one without; confirm the first shows the configured minutes and the second shows nothing, and that a work-order request of the first item shows the same figure.

**Acceptance Scenarios**:

1. **Given** a generic request, **When** it is shown, **Then** its time is the allotted time configured for its item.
2. **Given** an item with no allotted time, **When** a generic request for it is shown, **Then** no estimate appears and no default is invented.
3. **Given** the same item raised through both flows, **Then** both show the same configured time.
4. **Given** the details step, **Then** a needed-by answer is required and an optional description is allowed.
5. **Given** a needed-by answer given, **When** the request is shown, **Then** it appears on the card, on the detail page and in the stored record.

---

### User Story 4 - The generic card tells the truth and is told apart (Priority: P2)

A handler reads the board. The generic card identifies the request by its stated value and the work center that asked, carries the category and item wording in the same place the other cards do, shows the needed-by time, and draws no work-order, die, coil, flatstock or component cell. Generic and work-order cards sit in one list and are told apart at a glance.

**Why this priority**: a card built for a work order draws empty cells where facts should be, which is the failure this feature exists to avoid.

**Independent Test**: raise one generic and one work-order request, view the board, and confirm each draws only facts it has, with no placeholder anywhere.

**Acceptance Scenarios**:

1. **Given** a generic request, **When** its card is drawn, **Then** it uses the generic layout and draws no work-order, die, coil, flatstock or component cell.
2. **Given** a generic request, **When** its card is drawn, **Then** its identity is its stated value and the requesting work center.
3. **Given** both kinds on one board, **Then** they are distinguishable without opening either and neither is drawn blank or placeholder-filled.
4. **Given** a generic request, **When** its detail page is opened, **Then** it shows the card's facts plus the requester, the time raised, the accepted warnings and a history area.
5. **Given** a generic request, **When** a handler works it, **Then** the existing fulfilment flow is used and no separate handling path is needed.

---

### Edge Cases

- A work center is marked generic while a work-order request is already open at it: that request finishes under the flow it started with.
- Two requesters raise the same item at the same work center: two requests exist, because two people are asking.
- A request item's configuration is missing or malformed: the flow reports the configuration as unavailable and does not silently skip its questions.
- The queue write fails partway: no partial request is left behind and the failure is reported with a retry.
- The category or item list cannot be read: the step reports it unavailable with a retry rather than offering a shorter list.
- A requester's identity fails the existing employee check: the flow refuses to raise the request, exactly as the work-order flow does.

## Requirements

### Functional Requirements

**Designation**

- **FR-001**: The work center catalog MUST carry a per-work-center designation saying whether requests raised there are generic, changeable without a code change.
- **FR-002**: Only a person holding the permission that already governs the work center catalog MUST be able to change that designation; for everyone else the control MUST be absent.
- **FR-003**: No work center MUST be generic by built-in list, build configuration or default.
- **FR-004**: Clearing the designation MUST NOT alter requests already raised, and an in-progress generic request MUST complete as generic.

**The flow**

- **FR-005**: Selecting a generic work center MUST continue the wizard without an active job and MUST NOT fail for a missing one.
- **FR-006**: A generic request MUST offer the same categories and items as a work-order request, from the same configured definitions.
- **FR-007**: Every answer a work-order request would take from the job MUST be asked of the requester, and no job value MUST be read, shown or inferred.
- **FR-008**: A generic request MUST NOT store a work order number or display one on any surface, including exports.
- **FR-009**: A generic request MUST enter the same queue and appear on the same board as every other request, through the same stored-procedure path.
- **FR-010**: The flow MUST be built in its own projects and MUST leave the work-order flow's behaviour unchanged.
- **FR-011**: Abandoning the flow MUST leave nothing in the queue.
- **FR-012**: An answer step that needs a proved part, dunnage part, scrap type or die, and whose owning chunk has not shipped, MUST report that the answer source is not available; it MUST NOT offer a free-text box, an offline list or any substitute.

**Time, needed-by and description**

- **FR-013**: A generic request's time MUST be the allotted time configured for its item, identical to the value the work-order flow uses.
- **FR-014**: An item with no allotted time MUST produce no estimate rather than a default.
- **FR-015**: The flow MUST require a needed-by answer and MUST carry it on the card, the detail page and the stored record.
- **FR-016**: The flow MUST allow an optional free-text description and MUST carry it wherever the needed-by answer is carried.

**Confirmation and record**

- **FR-017**: The confirmation step MUST list everything about to be raised, so the review cannot understate the submission.
- **FR-018**: Submitting MUST write the request in one operation so no partial request is left behind.
- **FR-019**: A generic request MUST record its requester, work center, item, answers, time raised and every warning the requester accepted.
- **FR-020**: The requester's identity MUST come from the signed-in session and the existing employee verification; the flow MUST NOT accept a typed identity.
- **FR-021**: An item whose configuration cannot be read MUST be reported as unavailable rather than presented without its questions.

**Cards and detail**

- **FR-022**: A generic request MUST draw the generic card layout delivered in spec 015 and MUST NOT draw work-order, die, coil, flatstock or component cells.
- **FR-023**: A generic card's identity MUST be its stated value and the requesting work center.
- **FR-024**: A generic card MUST show the category and item wording, the needed-by time and, when given, the description.
- **FR-025**: No generic card MUST draw a blank or placeholder cell in place of a fact the request does not have.
- **FR-026**: The detail page MUST add the requester, the time raised, the accepted warnings and the request's history to the card's facts.
- **FR-027**: Fulfilling a generic request MUST use the existing handler flow.

**Truthfulness**

- **FR-028**: Every value a generic request shows MUST come from a live read, the requester's answer or configuration; none MAY be a sample, a cached substitute for an internal store, or a guess.
- **FR-029**: Every failure to establish a required value MUST be reported on the step where it matters, with the time of the attempt and a retry, and MUST NOT become a persistent banner or a silent blank.
- **FR-030**: This chunk MUST delete every `Temp` artefact belonging to the screens it makes real, MUST leave the build green afterwards, and MUST NOT rename such an artefact into a permanent-looking name in place of removing it.

### Key Entities

- **Generic work center designation**: whether requests at a named work center are generic; held on the work center record, maintained by a permitted person.
- **Generic request**: a queued request with no work order, carrying its item, answers, needed-by time, description, requester, time raised and accepted warnings.
- **Stated value**: the one value the request is about — for this chunk, a configured answer rather than a proved part, dunnage part, scrap type or die.

## Success Criteria

### Measurable Outcomes

- **SC-001**: A permitted user can mark and clear a work center's designation without a code change or developer involvement.
- **SC-002**: A requester at a generic work center raises a complete request in under 90 seconds and it appears on the board.
- **SC-003**: Zero generic requests carry a work order number on any surface or in any export.
- **SC-004**: Zero generic cards or detail pages show a sample, placeholder or invented value.
- **SC-005**: The same item shows the identical time figure through both flows, in 100% of cases.
- **SC-006**: Every existing work-order request scenario still passes with the generic flow present.

## Scope & Dependencies

**Builds on**: spec 015 — every screen and both cards used here were laid out and reviewed there. No new visual design is introduced.

**Deliberately absent**: proving a part against Infor Visual or the WIP floor and choosing a sequence (017), the dunnage catalogue page (018), the stated scrap type and its lugger (019), the die search (020), and filling a request from a running job (021). Until those chunks land, an item that needs one of those answers has its answer step say the source is not yet available (FR-012) — it MUST NOT offer a text box or any substitute.

**Later chunks depend on this one**: 017 to 022 each require the generic flow, the queue path and the card delivered here — 022 in order to verify them.

## Assumptions

- **A1**: The designation is a field on the existing work center catalog, maintained on the surface that already governs it, under its existing permission.
- **A2**: The requester's identity continues to come from sign-in plus the existing employee verification.
- **A3**: Generic requests share the existing queue and board; only their card layout differs.
- **A4**: Categories, items and their configured questions are shared with the work-order flow, so a change to a request item applies to both.
- **A5**: A generic work center with a running job still uses the generic flow in this chunk; asking for the job is spec 021.
