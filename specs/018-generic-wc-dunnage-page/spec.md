# Feature Specification: Generic Work Center — Dunnage Catalogue Page

**Feature Branch**: `018-generic-wc-dunnage-page`
**Created**: 2026-10-29
**Status**: Draft
**Input**: Chunk 4 of 8 carved from the feature specification kept at [specs/015-generic-wc-ui-shell/PARENT-SPEC.md](../015-generic-wc-ui-shell/PARENT-SPEC.md). The owner's requirement: "Dunnage uses the Substitute as a page." The substitute catalogue that exists today is shown as a dialog laid over the wizard; on a generic request there is no job to say which dunnage the run carries, so the catalogue itself becomes the step.

## User Scenarios & Testing

### User Story 1 - Dunnage is chosen from the real catalogue, as a page (Priority: P1)

A material handler needs dunnage for an upcoming run and the app cannot read a job to say which dunnage it carries. The dunnage step opens the same image-backed dunnage catalogue the substitute picker already shows — but as a page of the wizard rather than a dialog over it. They pick the part they need and carry on.

**Why this priority**: without it a generic request cannot ask for dunnage at all, and dunnage is one of the categories the plant raises every shift. It is the whole of this chunk.

**Independent Test**: raise a generic dunnage request by choosing a part from the catalogue page, and confirm the chosen part appears on the confirmation step and on the card; then confirm the same catalogue content appears as the substitute dialog on a work-order request.

**Acceptance Scenarios**:

1. **Given** the dunnage item is chosen on a generic request, **When** the dunnage step is entered, **Then** the catalogue opens as a full page of the wizard, not a dialog.
2. **Given** the catalogue page, **When** the requester picks a part, **Then** that part becomes the request's dunnage and the wizard offers the next step.
3. **Given** the catalogue page, **When** the requester leaves it without choosing, **Then** no dunnage part is recorded and the flow does not advance past a step that requires one.
4. **Given** a generic dunnage request, **Then** exactly one dunnage part is carried, and a second part is a second request.
5. **Given** the catalogue page, **When** the requester searches or filters, **Then** they find a part by the same means the substitute picker offers today.
6. **Given** a part is chosen, **When** the requester goes back to the catalogue, **Then** the part they chose is still shown as chosen.
7. **Given** the catalogue cannot be read, **When** the page opens, **Then** it says the catalogue is unavailable with the time of the attempt and a retry, and offers no invented or shortened list.

---

### User Story 2 - Both surfaces show the one catalogue (Priority: P2)

The catalogue is the plant's single dunnage list with its pictures. The page in the generic flow and the dialog the work-order flow opens must never disagree about what a dunnage part is, so neither can drift from the other.

**Why this priority**: two lists would let the two flows offer different parts for the same job, which is the class of defect the plant has already paid for elsewhere.

**Independent Test**: compare the part list, the pictures and the search behaviour of the generic page against the substitute dialog and confirm they are the same source.

**Acceptance Scenarios**:

1. **Given** the generic catalogue page, **When** it lists parts, **Then** its content comes from the same source the substitute dialog uses, with the same pictures.
2. **Given** a dunnage part added to that source, **When** either surface is opened, **Then** the new part appears on both.
3. **Given** a dunnage part removed from that source, **When** either surface is opened, **Then** it is offered by neither.

---

### User Story 3 - The chosen dunnage is visible afterwards (Priority: P2)

A handler reads the board and sees which dunnage part the requester needs, and opening the request shows the same part.

**Why this priority**: a dunnage request whose card does not name the dunnage is not actionable; the handler would have to ask.

**Independent Test**: raise a generic dunnage request from the page and confirm the part appears on the confirmation step, the card and the detail.

**Acceptance Scenarios**:

1. **Given** a chosen dunnage part, **When** the confirmation step is shown, **Then** it names the part.
2. **Given** a generic dunnage request, **When** its card is drawn, **Then** the dunnage part appears.
3. **Given** a generic dunnage request, **When** its detail is opened, **Then** the same part appears with its picture where one exists.

---

### Edge Cases

- A part is chosen, then removed from the catalogue before the request is raised: the request keeps the part the requester chose and records that it is no longer in the catalogue, rather than silently dropping it.
- The catalogue loads slowly: the page shows progress and does not present an empty list as though the plant had no dunnage.
- A dunnage part has no picture: the page shows the part with its name and no placeholder image that implies a picture exists.
- The requester opens the page and immediately leaves: nothing is recorded and no warning is raised.
- Two dunnage items are wanted: each is its own request, and the flow says so rather than letting the requester add a second part in place.

## Requirements

### Functional Requirements

- **FR-001**: For an item that asks for dunnage, the step MUST be the dunnage catalogue rendered as a page of the wizard rather than a dialog.
- **FR-002**: The catalogue page MUST draw its parts, their pictures and its search from the same source the existing substitute picker uses; it MUST NOT introduce a second dunnage list.
- **FR-003**: The page MUST offer whatever finding means the substitute picker already offers, so a part reachable there is reachable here.
- **FR-004**: A chosen part MUST become the request's dunnage and MUST be shown as chosen when the page is revisited.
- **FR-005**: A generic dunnage request MUST carry exactly one dunnage part, and the flow MUST say that a second part is a second request rather than allowing one to be added.
- **FR-006**: Leaving the page without choosing MUST record no dunnage part and MUST NOT advance past a step that requires one.
- **FR-007**: A catalogue that cannot be read MUST be reported as unavailable with the time of the attempt and a retry, and MUST NOT be replaced with a shortened, cached or fabricated list.
- **FR-008**: The chosen part MUST appear on the confirmation step, on the card and on the detail page, with its picture where one exists.
- **FR-009**: A part that has disappeared from the catalogue since it was chosen MUST be kept on the request and marked as no longer offered, rather than silently dropped or replaced.
- **FR-010**: Loading MUST be distinguishable from an empty catalogue: the page MUST show progress while loading and MUST NOT present an empty list as a real answer.
- **FR-011**: This chunk MUST delete the `Temp` preview source behind the dunnage page, MUST leave the build green afterwards, and MUST NOT rename such an artefact into a permanent-looking name in place of removing it.

### Key Entities

- **Dunnage selection**: the single dunnage part chosen from the catalogue page, with its name, its picture reference and, when it has since left the catalogue, the fact that it is no longer offered.
- **Dunnage catalogue**: the plant's one list of dunnage parts with pictures, shared by the substitute dialog and the generic page.

## Success Criteria

### Measurable Outcomes

- **SC-001**: A requester at a generic work center can choose a dunnage part in under 30 seconds from opening the step.
- **SC-002**: Zero generic dunnage requests are raised carrying more than one part without the extra part becoming its own request.
- **SC-003**: The generic page and the substitute dialog show an identical part list and identical pictures, verified after a part is added and after one is removed.
- **SC-004**: Zero dunnage cards omit the dunnage part, and none shows a placeholder where a picture does not exist.

## Scope & Dependencies

**Builds on**: spec 015 (the dunnage page's layout and its preview states) and spec 016 (the generic flow, the confirmation step, the queue path and the card that this chunk fills).

**Deliberately absent**: the stated part and its quantity (017), the scrap type (019), the die search (020), the optional job fill (021). None is needed to choose dunnage.

**Not changed by this chunk**: the work-order flow's substitute dialog keeps behaving as it does today; this chunk adds a page that draws the same catalogue, it does not replace the dialog.

## Assumptions

- **A1**: The substitute picker's catalogue is reachable as a reusable view rather than only through a dialog, so the page can host it; where it is not, extracting it is part of this chunk and MUST NOT change the dialog's behaviour.
- **A2**: A dunnage part's picture is found the same way it is found for the substitute dialog today; no new picture source is introduced.
- **A3**: One dunnage part per request is the agreed scope; several dunnage parts on one request is out of scope.
