> **This is not the active specification.** It is the combined feature specification as it stood before the
> split, kept so the whole feature can be read in one place and so no decision is lost. It is decomposed into
> eight specification files, 015 through 022, listed in [BREAKDOWN.md](./BREAKDOWN.md). Where this file and a
> chunk disagree, the chunk is authoritative.

# Feature Specification: Generic Work Center Request

**Feature Branch**: `015-generic-work-center-request` — *the pre-split feature; its eight chunks are 015 to 022*
**Created**: 2026-10-27
**Status**: Draft
**Input**: Owner request: "A Work Center that is not reliant on a Work Order. Instead of pulling request data from the work order the requester is responsible for stating what they need. Part Numbers must be validated against part numbers in Visual. Dunnage uses the Substitute as a page. If picking up scrap the requester is responsible for stating the scrap type during the request process. A new Generic Request Flow and Generic Request cards need to be established as new modules/projects."

The decisions below were settled with the owner: the generic designation is a flag on the work center catalog; a stated part is proved against Infor Visual **and** the WIP floor, which together supply the sequences the part runs at, and the chosen sequence decides which system owns the quantity — the WIP floor by part and sequence for work in process, Infor Visual by part number for a finished good (owner changes, 2026-10-29); the dunnage step *is* the substitute catalogue, shown as a page; scrap types come from the plant's one scrap-type list; generic requests share the existing queue and board but draw their own card; the flow and its views are built as new projects; and a die is found through a new Visual-backed search by die number or part number that lists every die a part has, with locations. Three questions were answered on 2026-10-29: a running job may be read **only if the requester asks for it** (Q1), the floor is read before Visual when a sequence has no operation state (Q2), and a part the floor holds but Visual does not know **is** requestable from the floor's own records (Q3).

## User Scenarios & Testing

### User Story 1 - A work center with no work order can still ask for what it needs (Priority: P1)

A press operator works at a work center that has nothing on the board — no active setup job, no work order to point at. Until now the request wizard stopped at that work center because it had nothing to read. The operator opens the wizard, picks that work center, and instead of the app reading a job, the app asks them what they need: which category and item, which part, and the few questions that item requires. They confirm, and the request appears on the same waitlist board as every other request, ready for a handler. If a job does happen to be running at that work center, the wizard offers to fill the request from it — a convenience the operator can decline, never a requirement.

**Why this priority**: this is the feature. Every other story exists to make one of those answers provable or to draw the result correctly. Without it, a work center can only ask for material by first inventing a work order.

**Independent Test**: mark one work center as generic, raise a request at it with nothing but a stated part, and confirm the request lands on the board with no work order anywhere on it, while a non-generic work center still follows the work-order flow unchanged.

**Acceptance Scenarios**:

1. **Given** a work center is designated generic, **When** the requester selects it on the work center step, **Then** the wizard continues without an active job being required and never refuses the request for a missing job.
2. **Given** a generic work center is selected, **When** the wizard continues, **Then** the Category step offers the same categories a work-order request offers, and the Item step offers every item a work-order request offers.
3. **Given** a generic work center is selected, **When** the wizard reaches any step that a work-order request fills from the job (die, component, coil, hopper, dunnage, scrap), **Then** the requester answers that step themselves and no job value is read, shown, or guessed.
4. **Given** a requester has answered everything the chosen item requires, **When** they confirm, **Then** one request is raised into the waitlist queue and appears on the board.
5. **Given** a work center that is **not** designated generic, **When** it is selected, **Then** the existing work-order flow runs exactly as it does today.
6. **Given** a generic work center that happens to have an active job, **When** a request is raised there, **Then** the generic flow is used and the job is not read unless the requester asks for it.
7. **Given** a generic work center with a running job, **When** the requester declines the offer to fill the request from it, **Then** the request proceeds exactly as it would with no job at all, and the refusal is not recorded as a failure.
8. **Given** the requester accepts that offer, **When** the request is filled from the job, **Then** every value taken from it is marked as coming from the job, each one can still be changed before the request is raised, and the job is recorded as the request's provenance rather than as its identity.

---

### User Story 2 - The stated part's sequence decides where its quantity comes from (Priority: P1)

A machinist needs a part brought to their work center. They type the number into a box that completes as they type, and the app answers with the sequences it can attribute to that part — from Visual where Visual records them, and from the floor's own records where the floor holds the part at a sequence. They pick the sequence the part is actually standing at. That one answer tells the app where the quantity lives: a sequence still open means the part is work in process, so the floor's own record is asked by part and sequence; a sequence already closed means the part is a finished good, so Infor Visual is asked by part number; a sequence the floor supplied answers for itself. Either way the app shows a number — the units on hand, or zero — and zero is a warning rather than a wall, because the machinist may know something the stock figure does not.

**Why this priority**: it is the only thing standing between a free-text part number and a handler walking to a bin that does not exist. Everything downstream — the card, the pick, the audit — assumes the number is real and its quantity has an owner.

**Independent Test**: enter a part that carries several sequences, choose an open one and then a closed one, and confirm the two quantities come from different systems; then enter an unknown number, and a real part with nothing on hand, and confirm the first is refused while the second warns and proceeds.

**Acceptance Scenarios**:

1. **Given** the part step is shown, **When** the requester types a number, **Then** completions are drawn from Infor Visual and from the WIP floor's part records, and the accepted number must be one either knows.
2. **Given** an accepted part, **When** the sequence list is returned, **Then** it lists every sequence attributable to that part — those Visual records (where the part is the order's own part, and where it appears as a requirement at an operation) together with those the floor holds the part at — each once, with its sequence number, its description where one exists, and the source that supplied it.
3. **Given** a part Infor Visual does not know but the floor holds at one or more sequences, **When** the requester types it and the step continues, **Then** the part is accepted, its sequences come from the floor's records, and the floor is named as the source.
4. **Given** a part number neither Infor Visual nor the floor knows, **When** the requester tries to continue, **Then** the flow refuses to advance and names both sources it searched.
5. **Given** a part both know but no sequence can be attributed to, **When** the requester tries to continue, **Then** the flow refuses to advance and says no sequence was found.
6. **Given** the sequence list, **When** no sequence has been chosen yet, **Then** no quantity is read, and the flow does not advance.
7. **Given** a chosen sequence whose operation is open — released, unreleased or firmed — **When** the quantity is read, **Then** it comes from the WIP floor by part and sequence.
8. **Given** a chosen sequence whose operation is closed, **When** the quantity is read, **Then** it comes from Infor Visual by part number.
9. **Given** a quantity was read, **When** the step is shown, **Then** it states the units on hand — `0` where none is on hand — with the system that answered, and never a blank, a dash, or an estimate.
10. **Given** a chosen sequence whose operation is routed to outside service, **When** the step is shown, **Then** it says the part is not inventoried while it is outside, shows no number rather than `0`, and the requester may still raise the request having seen that.
11. **Given** a part with nothing on hand, **When** the requester continues, **Then** they may still raise the request, having seen a warning, and the request carries the part, the chosen sequence, the quantity shown and the system that answered.
12. **Given** the owning system cannot be reached, **When** the quantity is read, **Then** no unverified part or sequence is accepted, the step reports that system unavailable with the time of the attempt and a retry, and the flow does not fall back to a cached, sample or fabricated sequence list or quantity.
13. **Given** a part has been accepted and a sequence chosen, **When** the requester changes the part, **Then** the chosen sequence and the quantity are cleared and a sequence must be chosen again.
14. **Given** a sequence whose every operation is cancelled, **When** the list is returned, **Then** that sequence is not offered, because no material can be requested at a stage that will not run.
15. **Given** a chosen sequence for which Visual records no operation state — including one the floor supplied — **When** the quantity is read, **Then** the WIP floor is read first for that part and sequence, Infor Visual by part number only when the floor holds none, and the step states which system answered.

---

### User Story 3 - Dunnage is chosen from the substitute catalogue, as a page (Priority: P1)

A material handler needs dunnage for an upcoming run, and the app cannot read a job to tell them which dunnage the run carries. The dunnage step opens the same dunnage catalogue that the substitute picker shows today — the image-backed list of parts — but as a page of the wizard rather than a dialog laid over it. They pick the part they need and carry on, one dunnage part per request.

**Why this priority**: without a page, the generic flow has no way to name dunnage at all, and dunnage is one of the request categories the plant raises every shift.

**Independent Test**: raise a generic dunnage request by choosing a part from the catalogue page, and confirm the chosen part appears on the confirmation step and on the card.

**Acceptance Scenarios**:

1. **Given** the dunnage item is chosen on a generic request, **When** the dunnage step is entered, **Then** the dunnage catalogue opens as a full page of the wizard.
2. **Given** the catalogue page, **When** the requester picks a part, **Then** that part becomes the request's dunnage and the wizard offers the next step.
3. **Given** the catalogue page, **When** the requester leaves it without choosing a part, **Then** no dunnage part is recorded and the confirmation step lists none.
4. **Given** a generic dunnage request, **Then** exactly one dunnage part is carried, and a second part is a second request.
5. **Given** the catalogue cannot be read, **When** the page opens, **Then** it says the catalogue is unavailable with a retry, and offers no invented or sample parts.

---

### User Story 4 - Scrap: the requester states the scrap type (Priority: P2)

A work center has scrap to move. There is no job to read a scrap decision from, so the wizard asks the operator which scrap type it is. The list they choose from is the same configured list the plant already maintains, and the card tells the handler which lugger that scrap type goes to, following the lugger arrangement's own rules for a filled lugger.

**Why this priority**: scrap is a physical pickup that cannot be deferred; naming the wrong scrap type sends a handler to the wrong lugger.

**Independent Test**: raise a generic scrap request, choose each configured scrap type in turn, and confirm the card names the lugger the arrangement assigns for that type; then mark that lugger full and confirm the card follows the arrangement to the backup.

**Acceptance Scenarios**:

1. **Given** the scrap item is chosen on a generic request, **When** the wizard reaches the scrap step, **Then** it asks the requester to state the scrap type and reads no job.
2. **Given** the scrap type question, **Then** the choices are the plant's single scrap-type list — the same list the Setup scrap picker and the lugger arrangement editor already use — shown in that list's order, together with any type the arrangement records that is outside it.
3. **Given** a chosen scrap type, **When** the card is drawn, **Then** it names that scrap type and the lugger the arrangement assigns to it.
4. **Given** a configured scrap type whose luggers are all blocked, **When** the card is drawn, **Then** it names the scrap type and the lugger in the arrangement's order with its status beside the name, and does not draw blank.
5. **Given** a scrap type that has been removed from the arrangement after a request carrying it was raised, **When** the card is drawn, **Then** it still names the scrap type the requester stated and records the missing lugger as a configuration gap rather than inventing a lugger.
6. **Given** the requester does not choose a scrap type, **When** they try to continue, **Then** the flow does not advance.

---

### User Story 5 - A die is found by searching Visual (Priority: P2)

A work center needs a die. The requester knows either the die's own FGT number or the part the die makes, so the wizard offers a search page that accepts either. A die number that exists in Infor Visual is accepted; searching by part number lists every die Visual ties to that part, each with the location it lives in — because a busy part usually has several dies. The requester picks the die or dies they need, and each becomes its own request, exactly as a work-order request with several dies already behaves.

**Why this priority**: dies are the expensive, easily-misdirected item on this list; an unverified die number sends two people to the tool crib for nothing.

**Independent Test**: search with a die number, then with a part number that owns several dies, and confirm every die and location is listed; then try a number that exists nowhere and confirm it is refused.

**Acceptance Scenarios**:

1. **Given** the die item is chosen on a generic request, **When** the die step is entered, **Then** it is a search page that accepts either a die number or a part number, and nothing is read from a job.
2. **Given** a search term, **When** it matches, **Then** the results list every matching die with its own location, or state that the die's location is not recorded.
3. **Given** a part number that several dies are associated with, **When** the search runs, **Then** all of those dies are listed together with their locations, not just one.
4. **Given** the results list, **When** the requester chooses one die, **Then** that die is the request's die.
5. **Given** the results list, **When** the requester chooses several dies, **Then** one request is raised per chosen die and the confirmation step lists them all, so the review never understates what is about to be raised.
6. **Given** a term that matches no die, or a typed die number that does not exist in Infor Visual, **When** the requester tries to continue, **Then** the flow refuses to advance and says no die matched.
7. **Given** Infor Visual cannot be reached, **When** the die search runs, **Then** the page reports the source unavailable with a retry and accepts no unverified die.

---

### User Story 6 - A generic request still carries an honest time estimate (Priority: P2)

A handler arranging the day looks at a generic request and sees how long it should take, taken from the time already configured for that request type — not from a work order, and not from a guess. If the request type has no time configured, the request shows no estimate rather than a made-up one.

**Why this priority**: the estimate is what makes a request schedulable; a wrong or invented figure is worse than an empty one.

**Independent Test**: raise a generic request for a type with configured time and one without, and confirm the first shows the configured minutes and the second shows nothing.

**Acceptance Scenarios**:

1. **Given** a generic request, **When** it is shown on the board or its detail, **Then** the time it needs is the allotted time configured for its item — the same value a work-order request of that item uses.
2. **Given** an item with no allotted time configured, **When** a generic request for it is shown, **Then** no estimate appears and no default is invented.
3. **Given** the same item raised as a work-order request and as a generic request, **When** both are shown, **Then** both carry the same configured time.

---

### User Story 7 - A generic request's card says what it is (Priority: P2)

A material handler reads the board. A generic request's card cannot show a work order, a die, a coil or a flatstock, because none of those exist for it. Instead the card identifies the request by the part number and the work center that asked for it, carries the category and item wording in the same place the other cards carry theirs, and shows scrap type, dunnage part and the needed-by time when they apply. Generic and work-order cards sit in the same list and are told apart at a glance.

**Why this priority**: the card is the only surface most handlers ever see, and a card built for a work order would draw empty cells where facts should be.

**Independent Test**: raise one generic and one work-order request, view the board, and confirm each card shows only facts that exist for it and that the generic card carries no work-order value anywhere.

**Acceptance Scenarios**:

1. **Given** a generic request, **When** its card is drawn, **Then** it uses the Generic Request card layout and draws no work-order, die, coil, flatstock or component cell.
2. **Given** a generic request, **When** its card is drawn, **Then** its identity is the part number and the requesting work center, and no work order number appears anywhere on it.
3. **Given** a generic request for scrap, **When** its card is drawn, **Then** the scrap type and the assigned lugger appear as they do on a work-order scrap card.
4. **Given** a generic request for dunnage, **When** its card is drawn, **Then** the chosen dunnage part appears.
5. **Given** a generic request, **When** its card is drawn, **Then** the needed-by time the requester gave appears.
6. **Given** a board holding both kinds, **When** both cards are drawn, **Then** the generic card is distinguishable from the work-order card without opening either, and neither card is drawn blank or placeholder-filled.
7. **Given** a generic request, **When** its detail page is opened, **Then** it shows the same facts as the card plus the requester, the time the request was raised, and the request's history.

---

### User Story 8 - The generic designation is maintained, not hard-coded (Priority: P3)

A setup lead opens the work center catalog and marks the work centers that raise generic requests, and clears the mark when a work center goes back to running jobs full time. A person without that permission cannot change it. No work center is generic because a build said so.

**Why this priority**: the mark has to survive the plant changing its mind, and it must not be a code change.

**Independent Test**: as a permitted user, mark a work center generic, raise a request, clear the mark, raise another, and confirm the first used the generic flow and the second the work-order flow; then confirm an unpermitted user cannot reach the control.

**Acceptance Scenarios**:

1. **Given** a person holding the permission that already governs the work center catalog, **When** they open it, **Then** they can mark a work center as generic and clear that mark.
2. **Given** a work center is marked generic, **When** a request is started there, **Then** the generic flow is used.
3. **Given** the mark is cleared, **When** the next request is started there, **Then** the work-order flow is used and no generic-only step is offered.
4. **Given** a generic request is in progress, **When** the mark is cleared mid-request, **Then** the in-progress request completes as generic.
5. **Given** a person without that permission, **When** they reach any route into the catalog, **Then** they cannot see or change the generic mark — not shown-but-disabled.

---

### Edge Cases

- A generic work center has an active job and the requester wants the job anyway: no job-derived path is offered from a generic work center; the request is generic (User Story 1, scenario 6).
- Both the owning systems are unreachable: a part cannot be proved and no quantity can be read, so no request can be raised from that step; the step reports the failures with retries, and offers no cached sequence list and no cached quantity.
- A part exists in Infor Visual and on the WIP floor at once: the chosen sequence decides which system answers, so the presence of a part in either system never picks the answer for the requester.
- The same part number carries different sequence numbers on different orders: the list is the distinct union, so each number appears once, and the chosen sequence is what the request carries.
- A chosen sequence's operation closes between the list being shown and the quantity being read: the read uses the state as it stands at read time and states which system answered, rather than reporting a stage that no longer applies.
- A chosen sequence is routed to an outside vendor: nothing inventories it, so the step says the part is not inventoried rather than reporting `0`, and the request may still be raised.
- The WIP floor holds a part at a sequence Infor Visual records no operation for: that floor record IS a legitimate sequence source (Q3), so it is offered with the floor named as its source and the floor answers the quantity directly.
- The WIP floor holds a part against an operation that is not a usable sequence number — the live data carries `N/A` — so it cannot distinguish a stage: it is not offered as a sequence, and the part is refused only if no other source yields one.
- The requester fills a generic request from a running job and then edits one of the values: the edit wins, and that value no longer reads as taken from the job.
- A scrap type is renamed or removed from the arrangement after a generic request carries it: the request keeps the wording the requester stated, and the lugger cell reports the missing assignment rather than an invented lugger.
- A die has no location recorded: the result row lists the die and states that no location is recorded, rather than an empty cell.
- A die search returns more dies than fit one screen: every match is reachable, and the requester can see how many matched.
- Two requesters raise the same part at the same work center: two requests exist, because two people are asking.
- A generic request is cancelled or the app closes mid-flow: nothing reaches the queue, and no partial request is stored.
- The dunnage catalogue opens while the substitute picker is unavailable in Settings: the page reports the same unavailability rather than offering a shorter sample list.
- A requester raises a generic request for an item whose configured questions no longer exist: the flow reports the item's configuration as unavailable rather than silently skipping the questions.

## Requirements

### Functional Requirements

**Designation of a generic work center**

- **FR-001**: The work center catalog MUST carry a per-work-center designation that says whether requests raised there are generic, editable without a code change.
- **FR-002**: Only a person holding the permission that already governs the work center catalog MUST be able to set or clear that designation; for everyone else the control MUST be absent, not disabled.
- **FR-003**: No work center MUST be treated as generic because of a built-in list, a build configuration, or a default; an unmarked work center MUST always use the work-order flow.

**The generic flow**

- **FR-004**: Selecting a generic work center in the request wizard MUST continue the wizard without requiring an active job and without failing the work center because it has none. Where an active job does exist at that work center, the wizard MUST offer the requester the choice of filling the request from it, MUST NOT take that choice for them, and MUST NOT fail or block the request when they decline.
- **FR-005**: A generic request MUST offer the same categories and the same items a work-order request offers, driven by the same configured request-item definitions.
- **FR-006**: Every answer a work-order request takes from the job MUST instead be asked of the requester, and no job value MUST be read, shown or inferred during a generic request unless the requester chose the optional job fill; where they did choose it, every value taken from the job MUST be shown as taken from it and MUST remain changeable before the request is raised.
- **FR-007**: A generic request MUST NOT use a work order as its identity and MUST NOT display one on its card; where the requester filled the request from a running job, that job MUST be recorded as the request's provenance and shown on its detail page, never as the request's identity.
- **FR-008**: A request raised at a generic work center MUST enter the same waitlist queue and appear on the same board as every other request.
- **FR-009**: The generic flow MUST be built as its own flow, and its views as their own projects, so that the work-order flow is not modified to accommodate it; the existing work-order flow's behaviour MUST remain unchanged.
- **FR-010**: Clearing a work center's designation MUST NOT alter requests already raised there; an in-progress generic request MUST complete as generic.

**A stated part**

- **FR-011**: The part-number entry MUST offer completions drawn from Infor Visual and from the WIP floor's own part records, and the accepted part number MUST be one of them; a number neither knows MUST be refused with a message naming both sources searched, and the flow MUST NOT advance.
- **FR-012**: For an accepted part, the system MUST list every sequence it can attribute to that part — the sequences Infor Visual records (where the part is the work order's own part, and where it appears as a requirement at an operation) together with the sequences the WIP floor holds the part at — each listed once, with its sequence number, its description where one exists, and the source that supplied it; a sequence whose every operation is cancelled MUST NOT be offered, and a floor record whose operation is not a usable sequence number MUST NOT be turned into one.
- **FR-013**: A part for which neither Infor Visual nor the WIP floor yields a sequence MUST be refused with a message saying so, because no quantity can be attributed to it, and the flow MUST NOT advance.
- **FR-014**: The requester MUST choose one sequence before any quantity is read, the chosen sequence MUST travel with the request, and changing the part MUST clear the chosen sequence and any quantity already read for it.
- **FR-015**: The chosen sequence's own state in Infor Visual MUST decide which system owns the quantity — an open operation (released, unreleased or firmed) is work in process and its quantity MUST come from the WIP floor by part and sequence; a closed operation is a finished good and its quantity MUST come from Infor Visual by part number. A sequence routed to outside service MUST NOT have a quantity read at all, because outside-service material is not inventoried anywhere. The requester MUST NOT be asked which system to use.
- **FR-016**: Where Infor Visual records no operation state for the chosen sequence — including a sequence the WIP floor supplied — the WIP floor MUST be read first for that part and sequence, and Infor Visual by part number only when the floor holds none, and the step MUST state which system answered.
- **FR-017**: The quantity MUST be shown as a number of units on hand — `0` when the owning system holds none — and MUST NOT be left blank on that ground, drawn as a dash, or estimated; where the chosen sequence is outside service the step MUST say the part is not inventoried instead of showing `0`; the flow MUST NOT ask the requester for a quantity.
- **FR-018**: Finding `0` on hand MUST warn and MUST NOT block the request, and the request MUST record the part, the chosen sequence, the quantity shown and the system that answered.
- **FR-019**: Where the owning system cannot be reached, the flow MUST report that system unavailable with the time of the attempt and a retry, MUST accept no unverified part or sequence, and MUST NOT substitute a cached, mirror, sample or fabricated sequence list or quantity.

**Dunnage**

- **FR-020**: For an item that asks for dunnage, the generic flow's dunnage step MUST be the dunnage catalogue rendered as a page of the wizard, showing the same catalogue the substitute picker shows today.
- **FR-021**: A chosen dunnage part MUST become the request's dunnage and MUST appear on the confirmation step and the card.
- **FR-022**: A generic dunnage request MUST carry exactly one dunnage part.
- **FR-023**: Leaving the dunnage page without choosing a part MUST record no dunnage part, and MUST NOT advance past a step that requires one.
- **FR-024**: A catalogue that cannot be read MUST be reported as unavailable with a retry, and MUST NOT be replaced with a sample or fabricated list.

**Scrap**

- **FR-025**: For the scrap item, the generic flow MUST ask the requester to state the scrap type and MUST read no job.
- **FR-026**: The scrap-type choices MUST come from the single scrap-type list the plant already maintains for the Setup scrap picker and the lugger arrangement editor, offered in that list's order, plus any type the arrangement records that falls outside the list; no second scrap-type list MAY be introduced.
- **FR-027**: A stated scrap type MUST be carried on the confirmation step, on the card, and in the stored request.
- **FR-028**: A generic scrap card MUST name the lugger the lugger arrangement assigns to the stated type, following the arrangement's blocking and backup rules rather than reading a job.
- **FR-029**: A scrap type with no lugger assigned MUST be shown with the type named and the missing assignment recorded as a configuration gap, never with an invented lugger.
- **FR-030**: A scrap type removed from the arrangement after a request carries it MUST leave that request's stated type intact.
- **FR-031**: The flow MUST NOT advance without a scrap type where the item requires one.

**Die**

- **FR-032**: For the die item, the generic flow MUST present a die search page that accepts a die (FGT) number or a part number and reads no job.
- **FR-033**: A die number MUST be accepted only when it exists in Infor Visual, and a search by part number MUST list every die Visual associates with that part.
- **FR-034**: Each die in the results MUST be listed with its location, and a die with no recorded location MUST say so rather than showing an empty cell.
- **FR-035**: The results MUST list every matched die, including every die belonging to a part that has several, with a visible count of how many matched.
- **FR-036**: Choosing several dies MUST raise one request per chosen die, and the confirmation step MUST list every one of them.
- **FR-037**: A term matching no die, or a typed die number that does not exist in Visual, MUST be refused with a message naming the source searched, and the flow MUST NOT advance.
- **FR-038**: Where Infor Visual cannot be reached, the die search MUST report it unavailable with a retry and accept no unverified die.

**Time, urgency and description**

- **FR-039**: A generic request's time estimate MUST be the allotted time configured for its item, identical to the value a work-order request of that item uses.
- **FR-040**: An item with no allotted time configured MUST produce no estimate rather than a default.
- **FR-041**: The generic flow MUST ask the requester for a needed-by time or urgency, and the answer MUST appear on the confirmation step, the card and the stored request.
- **FR-042**: The generic flow MUST allow a free-text description, and MUST NOT require one.

**Confirmation, queue and record**

- **FR-043**: The confirmation step MUST list everything that will be raised — every die entry, the stated part and its type, the dunnage part, the scrap type, the needed-by time and the description — so the review cannot understate the submission.
- **FR-044**: Submitting MUST write the request through the same queue path every other request uses, in one operation, so that a partial request is never left behind.
- **FR-045**: A generic request MUST record its requester, the work center, the item, the answers given, the time it was raised, and every warning the requester saw and accepted.
- **FR-046**: The requester's identity MUST come from the signed-in session and the existing employee-verification rules; the generic flow MUST NOT invent or accept a typed identity.
- **FR-047**: Fulfilment of a generic request MUST use the existing handler flow; a generic request MUST NOT need a separate handling path to be worked.

**Cards and surfaces**

- **FR-048**: A generic request MUST draw its own card layout, and that layout MUST NOT draw work-order, die, coil, flatstock or component cells.
- **FR-049**: A generic card's identity MUST be the stated part number and the requesting work center.
- **FR-050**: A generic card MUST show the category and item wording, the needed-by time, and, where they apply, the scrap type with its assigned lugger and the chosen dunnage part.
- **FR-051**: Generic and work-order cards MUST appear in the same list and MUST be distinguishable without opening either.
- **FR-052**: No generic card MUST draw an empty or placeholder cell in place of a fact the request does not have.
- **FR-053**: A generic request's detail page MUST show the same facts as its card plus the requester, the time raised, the accepted warnings, and the request's history.

**Truthfulness**

- **FR-054**: Every value a generic request shows MUST come from a read of the system that owns it, from the requester's own answer, or from configuration; no cell MAY be filled with a sample, a cached substitute for an internal store, or a guess.
- **FR-055**: Every failure to establish a required value MUST be reported on the step where it matters, with the time of the attempt and a retry, and MUST NOT be turned into a persistent banner or a silent blank.

### Key Entities

- **Generic work center designation**: whether requests raised at a named work center are generic; held against the work center record itself, editable by a permitted person.
- **Stated part**: the part number the requester named, the sequence they chose for it and which source supplied that sequence, which system supplied the quantity, and the quantity on hand that system returned when the request was raised — or the fact that the part is not inventoried because the sequence is outside service.
- **Dunnage selection**: the single dunnage part chosen from the catalogue page.
- **Stated scrap type**: the scrap type the requester named, with the lugger the arrangement assigns at the time the request is read.
- **Die selection**: one or more dies found by search, each with its own number and location, each producing its own request.
- **Generic request**: a queued request carrying no work order, identified by its stated part and requesting work center, with its item, answers, needed-by time, description, requester, time raised and accepted warnings.
- **Generic request card**: the board presentation of a generic request, drawing only the facts that request has.

## Success Criteria

### Measurable Outcomes

- **SC-001**: A requester at a generic work center can raise a complete request without a work order, in under 90 seconds for a part request and under 2 minutes for a dunnage, scrap or die request.
- **SC-002**: Zero generic requests are accepted carrying a part number, sequence, dunnage part, scrap type or die that could not be proved against Infor Visual or the WIP floor, or attributed to a sequence either can supply.
- **SC-003**: Zero generic requests carry a work order number, on any surface, in any export.
- **SC-004**: Zero generic cards or detail pages show a sample, placeholder, or invented value in place of a fact the request does not have.
- **SC-005**: A handler can tell a generic request from a work-order request on the board without opening either, and can act on it through the existing fulfilment flow.
- **SC-006**: A part with no stock on hand is still raisable, with the warning visible on the request afterwards, in 100% of cases.
- **SC-007**: The work-order flow's behaviour is unchanged: every existing request scenario still passes with the generic flow present.
- **SC-008**: A part number that several dies belong to returns every die and location in one search, with no die silently omitted.
- **SC-009**: A part number returns every sequence Infor Visual records for it in one search with none omitted, and the quantity shown for a chosen sequence comes from the system that sequence's state assigns — with nothing on hand shown as `0`, never blank, and outside-service work shown as not inventoried rather than `0`.

## Assumptions

- **A1**: The generic designation is a field on the existing work center catalog, maintained through the setup/settings surface that already governs that catalog, under its existing permission.
- **A2**: The requester's identity continues to come from sign-in plus the existing employee verification; the generic flow adds no second identity step.
- **A3**: Generic requests share the existing waitlist queue and board; only their card layout differs.
- **A4**: A generic request captures no requested quantity; it records the quantity on hand it was shown and the system that supplied it.
- **A5**: Choosing several dies raises one request per die, matching how a work-order request with several dies already behaves.
- **A6**: One dunnage part per request is the scope; several dunnage parts on one request is out of scope.
- **A7**: The scrap types offered are the plant's one existing scrap-type list, the list the Setup scrap picker and the lugger arrangement editor already share; the generic flow introduces no second list.
- **A8**: Categories, items and their configured questions are shared with the work-order flow, so a change to a request item applies to both.
- **A9**: A generic work center that still has an active job still uses the generic flow; the job is offered as an optional fill and is never read unless the requester chooses it (Q1, answered 2026-10-29).
- **A10**: A die's association with a part is established from Infor Visual, preferring an association Visual records explicitly and falling back to the part's work-order history; the outcome FR-033 requires is that every die Visual ties to the part is listed.
- **A11**: "Every available sequence for a part" means the distinct union of the sequences Infor Visual records (where that part is the work order's own part and where it appears as a requirement at an operation) and the sequences the WIP floor holds the part at, because Visual holds sequences per routing rather than per part; the same number appearing more than once is listed once, with the sources it came from.
- **A12**: The sequence list is the union of what Infor Visual records for the part and what the WIP floor holds it at, by owner decision on 2026-10-29, so a part Infor Visual does not know MAY be requested from the floor's own records; a floor record whose operation is not a usable sequence number cannot supply one.
- **A13**: A sequence's stage is taken from the operation states the plant already relies on for disposition — released, unreleased and firmed count as open (work in process); closed counts as a finished good; cancelled sequences are excluded rather than offered; an operation routed to outside service is never inventoried.
- **A14**: How inventory is split across the two systems, as the owner stated it on 2026-10-29: WIP items are inventoried only in the WIP application, by part number and sequence into a location; Infor Visual holds what is left that is neither work in process nor outside service; outside-service material is not inventoried at all.
- **A15**: The requesting work center is always where a stated part should go — an owner decision taken on 2026-10-29 — so no delivery-point question is asked and the request's work center doubles as its destination.
- **A16**: Where Infor Visual records no operation state for a chosen sequence, the WIP floor is read first and Infor Visual only when the floor holds none — owner decision on 2026-10-29 (Q2), which FR-016 and FR-019 carry.

## Questions and Decisions

None outstanding. Three questions were answered by the owner on 2026-10-29, and each answer is carried by
requirements rather than left as a note:

| Question, in the words a person using the app would use | Answer | Carried by |
|---|---|---|
| At a work center the plant has marked as generic, may the person say "actually, read the job that is running here" and have the request filled from it? | **Yes, optionally** — offered, never assumed, and declining changes nothing. The job is recorded as provenance and never becomes the request's identity. | [FR-004](./spec.md#L203), [FR-006](./spec.md#L205), [FR-007](./spec.md#L206), [A9](./spec.md#L314) |
| Someone picks a part and a sequence, and Visual knows the part but not the stage of that sequence. Keep looking on the floor first and fall back to Visual, or refuse and ask for the routing to be fixed? | **Floor first**, then Visual when the floor holds none. | [FR-016](./spec.md#L218), [A16](./spec.md#L321) |
| The WIP app holds a part at a sequence that Infor Visual does not know at all. Requestable from the floor's own records, or turned away? | **Yes** — the floor is a legitimate sequence source, so such a part is requestable and its sequence comes from the floor. | [FR-011](./spec.md#L213), [FR-012](./spec.md#L214), [A12](./spec.md#L317) |
