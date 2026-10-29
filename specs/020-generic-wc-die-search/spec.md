# Feature Specification: Generic Work Center — Die Search

**Feature Branch**: `020-generic-wc-die-search`
**Created**: 2026-10-29
**Status**: Draft
**Input**: Chunk 6 of 8 carved from the feature specification kept at [specs/015-generic-wc-ui-shell/PARENT-SPEC.md](../015-generic-wc-ui-shell/PARENT-SPEC.md). The owner's requirement: a generic die request must "enter a new search die page that validates against Visual, can search by FGT or Part Number, some part numbers have multiple dies so show all dies + locations in the results page."

## User Scenarios & Testing

### User Story 1 - A die is found by its own number or by the part it makes (Priority: P1)

A work center needs a die. The requester knows either the die's FGT number or the part the die stamps, so the die step is a search page that accepts either. A number that exists in Infor Visual is accepted; a number that exists nowhere is refused, because a wrong die sends two people to the tool crib for nothing.

**Why this priority**: dies are the expensive, easily misdirected item in this feature, and there is no job to say which die is meant. It is the whole of this chunk.

**Independent Test**: search with a die number, then with a part number, then with a number that exists nowhere; confirm the first two match and the third is refused.

**Acceptance Scenarios**:

1. **Given** the die item on a generic request, **When** the step is entered, **Then** it is a search page accepting a die (FGT) number or a part number, and nothing is read from a job.
2. **Given** a typed die number that exists in Infor Visual, **When** the search runs, **Then** that die is matched.
3. **Given** a typed die number that exists nowhere, **When** the requester tries to continue, **Then** the flow refuses to advance and says no die matched, naming Infor Visual as the source searched.
4. **Given** Infor Visual cannot be reached, **When** the search runs, **Then** the page reports it unavailable with the time of the attempt and a retry, and accepts no unverified die.

---

### User Story 2 - Every die a part has is listed, with its location (Priority: P1)

A busy part usually has several dies. Searching by that part number must return all of them, each with the location it lives in, so the requester can pick the right one instead of guessing — and so no die is silently left out.

**Why this priority**: returning one die for a part that owns six would look like a complete answer and be wrong, which is more damaging than returning none.

**Independent Test**: search a part number known to own several dies and confirm all of them appear with locations, with a visible count.

**Acceptance Scenarios**:

1. **Given** a part number several dies belong to, **When** the search runs, **Then** every die Visual associates with that part is listed with its own location.
2. **Given** the results, **Then** the number of matches is visible, so a partial list cannot pass for a full one.
3. **Given** a die with no recorded location, **When** it is listed, **Then** it states that no location is recorded rather than drawing an empty cell.
4. **Given** a part number that matches no die, **When** the requester tries to continue, **Then** the flow refuses to advance and says no die matched.
5. **Given** more dies than fit one screen, **Then** every match is reachable and the count still shows the full number.

---

### User Story 3 - One die or several, each with its own request (Priority: P2)

The requester picks the die or dies they need. Each becomes its own request, exactly as a work-order request with several dies already behaves, and the confirmation step lists every one of them so the review never understates what is about to be raised.

**Why this priority**: a run that is about to raise four requests and shows one is the class of understatement this repository has already had to fix.

**Independent Test**: choose three dies and confirm the confirmation step lists all three and three requests are raised, each naming one die and its location.

**Acceptance Scenarios**:

1. **Given** the results, **When** the requester chooses one die, **Then** that die is the request's die.
2. **Given** the results, **When** the requester chooses several dies, **Then** one request is raised per chosen die.
3. **Given** several dies chosen, **When** the confirmation step is shown, **Then** it lists every one of them.
4. **Given** several dies chosen, **When** the requests are drawn, **Then** each card names its own die and that die's location.
5. **Given** the requester returns to the results, **Then** the dies they chose are still marked as chosen.

---

### Edge Cases

- A part number matches dies whose locations are recorded differently — one from a stock row, one from the part's primary location: both are shown as the die's location without one being preferred silently.
- A die number is typed with the FGT prefix missing or in lower case: the search accepts the same normalisation the existing part search applies.
- The same die is reachable from two different part numbers: it is listed once under each search, and choosing it twice raises two requests, because two people or two runs asked.
- A die's description reads as a placeholder the plant uses — "Die still needs location" — the results show that text as the die's own description rather than treating it as a location.
- Infor Visual returns an error mid-search: the page reports it with the time of the attempt and a retry, and keeps the requester's term.
- A very large result set: the count is shown before the list, so the requester knows what they are looking at.

## Requirements

### Functional Requirements

**Search**

- **FR-001**: The die step MUST be a search page accepting either a die (FGT) number or a part number, and MUST read no job.
- **FR-002**: A die number MUST be accepted only when Infor Visual records it; a term matching no die MUST be refused with a message naming Infor Visual as the source searched, and the flow MUST NOT advance.
- **FR-003**: A search by part number MUST return every die Infor Visual associates with that part; where Visual records an explicit association and where it records only the part's work-order history, the explicit association MUST be preferred and the history used to complete the list.
- **FR-004**: A typed die number MUST be normalised the way the existing part search normalises a term, so a missing prefix or a different letter case still matches.
- **FR-005**: Where Infor Visual cannot be reached, the page MUST report it unavailable with the time of the attempt and a retry, and MUST keep the requester's term for the retry.

**Results**

- **FR-006**: Each die MUST be listed with its own location, and a die with no recorded location MUST state that rather than drawing an empty cell.
- **FR-007**: The number of matches MUST be visible, so a partial list cannot pass for a complete one.
- **FR-008**: Every match MUST be reachable, however many there are.
- **FR-009**: A die whose description is a placeholder the plant uses MUST show that text as its description and MUST NOT treat it as a location.

**Choosing**

- **FR-010**: The requester MUST be able to choose one die or several, and the chosen dies MUST remain marked when the results are revisited.
- **FR-011**: Each chosen die MUST become its own request, and the confirmation step MUST list every one of them.
- **FR-012**: Each resulting card MUST name its own die and that die's location.
- **FR-013**: Choosing no die MUST NOT advance the flow.
- **FR-014**: This chunk MUST delete the `Temp` preview source behind the die search page and the die card variant, MUST leave the build green afterwards, and MUST NOT rename such an artefact into a permanent-looking name in place of removing it.

### Key Entities

- **Die**: a Visual `FGT` part, with its number, its description and its location where one is recorded.
- **Die search result**: the complete set of dies a term matches, with a visible count, each die carrying its own location.
- **Die selection**: the one or several dies the requester chose, each producing its own request.

## Success Criteria

### Measurable Outcomes

- **SC-001**: A requester with either a die number or a part number reaches the right die in one search, in under 30 seconds.
- **SC-002**: A part number owning several dies returns all of them with locations, with no die omitted, verified against a part known to own six or more.
- **SC-003**: Zero generic die requests are raised naming a die Infor Visual does not record.
- **SC-004**: Choosing three dies raises three requests, and the confirmation step and the cards name each die and its location.
- **SC-005**: Zero die result rows draw a blank location cell.

## Scope & Dependencies

**Builds on**: spec 015 (the die search page's layout and preview states) and spec 016 (the generic flow, the confirmation step and the queue path, which this chunk uses once per chosen die).

**Deliberately absent**: the stated part and its quantity (017), the dunnage page (018), the scrap type (019), the optional job fill (021). None is needed to find a die.

**Not changed by this chunk**: the work-order flow's die step keeps reading the job's dies as it does today.

## Assumptions

- **A1**: A die is a Visual `FGT` part, which is how the existing subordinate-parts read already classifies it.
- **A2**: A die's home location is its primary location where Visual records one, falling back to a stock location, as the existing subordinate-parts read already resolves it.
- **A3**: Choosing several dies means several requests, matching the existing work-order behaviour.
- **A4**: The die search is a live Visual read with no cached mirror, as the existing part search is.
