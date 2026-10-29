# Feature Specification: Generic Work Center — Optional Job Fill

**Feature Branch**: `021-generic-wc-optional-job-fill`
**Created**: 2026-10-29
**Status**: Draft
**Input**: Chunk 7 of 8 carved from the feature specification kept at [specs/015-generic-wc-ui-shell/PARENT-SPEC.md](../015-generic-wc-ui-shell/PARENT-SPEC.md). Owner decision, 2026-10-29, answering whether a generic work center may ever read a running job: **yes, optionally** — offered, never assumed, and declining changes nothing. Until now the generic flow never reads a job, even when one is running.

## User Scenarios & Testing

### User Story 1 - The operator may let a running job fill the request (Priority: P1)

A work center normally quoted-only has a one-off job on it. The operator opens the wizard, picks the work center, and is told a job is running and offered to fill the request from it. If they accept, the part, the die and the dunnage the job already carries appear in front of them; if they decline, the wizard proceeds exactly as it does with no job at all and nothing about the refusal is recorded as a problem.

**Why this priority**: it is the whole of this chunk. It saves a requester three transcriptions on the days a quoted-only work center does run a job, without taking the choice away on the days it does not.

**Independent Test**: with a job running at a generic work center, accept the offer and confirm values arrive pre-filled; decline it and confirm the request proceeds as generic with everything typed; then confirm a work center with no job never shows the offer.

**Acceptance Scenarios**:

1. **Given** a generic work center with a running job, **When** the work center step completes, **Then** the requester is offered the choice of filling the request from that job.
2. **Given** the offer, **When** the requester declines, **Then** the flow proceeds exactly as with no job, and the refusal is not recorded as a failure or shown again on every later step.
3. **Given** the offer, **When** the requester accepts, **Then** the values the job carries are pre-filled into the steps that need them.
4. **Given** pre-filled values, **Then** each one is shown as coming from the job and remains changeable before the request is raised.
5. **Given** a pre-filled value is changed, **Then** the changed value is what the request carries, and it no longer reads as coming from the job.
6. **Given** a generic work center with no running job, **When** a request is raised, **Then** the offer never appears and the requester experiences no change from today.
7. **Given** the offer was accepted, **When** the request is raised, **Then** the job is recorded as the request's provenance and shown on its detail page.

---

### User Story 2 - A filled request is still a generic request (Priority: P1)

Accepting the offer must not turn the request into a work-order request. It still has no work order as its identity, still draws the generic card, and still carries everything the generic flow promises — the fill is a convenience laid over the top, not a change of kind.

**Why this priority**: if accepting the fill produced a different kind of request, every later chunk's guarantees would have two shapes to keep true, and the board would need a third card.

**Independent Test**: accept the offer, raise the request, and confirm the card and detail are the generic ones, that no work order appears as identity, and that the recorded job reads as provenance.

**Acceptance Scenarios**:

1. **Given** a request filled from a job, **When** its card is drawn, **Then** it is the generic card and no work order appears on it.
2. **Given** a request filled from a job, **When** its detail is opened, **Then** the job appears as provenance, distinct from the request's identity.
3. **Given** a request filled from a job, **When** it is exported, **Then** the job is shown as provenance and not as an identity field.
4. **Given** the fill was accepted but a value the job does not carry is needed, **Then** the requester is asked for it as they would be with no job.
5. **Given** the job is completed or cancelled between the offer and the submission, **When** the request is raised, **Then** the values already shown are kept and the change is not treated as an error.

---

### Edge Cases

- The job is closed while the requester is filling the form: the values already taken are kept, and the request still raises.
- The job carries a value the generic flow does not ask for: it is not shown, because the flow shows only the answers its item requires.
- The job carries a part that Infor Visual no longer records: the value is shown as taken from the job and the part steps still prove it, so the request cannot carry a part that no longer exists.
- A second request is started at the same work center in the same shift: the offer is made again, because the choice belongs to each request.
- The operator accepts the fill and then changes the item: values that belonged to the previous item are cleared rather than left behind.
- A person with no permission to see the job's data starts a request: the offer is not made, and the flow behaves as it did before this chunk.

## Requirements

### Functional Requirements

**Offering**

- **FR-001**: Where a generic work center has a running job, the wizard MUST offer the requester the choice of filling the request from it.
- **FR-002**: The wizard MUST NOT make that choice for the requester and MUST NOT read the job before it is made.
- **FR-003**: Declining MUST leave the flow exactly as it is without a job, MUST NOT be recorded as a failure, and MUST NOT be offered again within the same request.
- **FR-004**: Where no job is running, the offer MUST NOT appear.

**Filling**

- **FR-005**: Accepting MUST pre-fill the answers the job carries for the item's questions.
- **FR-006**: Every pre-filled value MUST be shown as coming from the job.
- **FR-007**: Every pre-filled value MUST remain changeable before submission, and a changed value MUST be the one the request carries and MUST no longer read as coming from the job.
- **FR-008**: A value the job does not carry MUST still be asked of the requester.
- **FR-009**: Changing the item after a fill MUST clear the answers that belonged to the previous item.
- **FR-010**: A pre-filled answer that the owning chunk proves externally MUST still be proved — the fill supplies the candidate, never a bypass.

**Staying generic**

- **FR-011**: A filled request MUST keep the generic identity — its stated value and the work center — and MUST NOT take the job as its identity.
- **FR-012**: A filled request MUST draw the generic card, with no work order cell.
- **FR-013**: The job MUST be recorded as the request's provenance and shown as such on the detail page and in any export, distinct from its identity.
- **FR-014**: A job that closes or changes between the offer and submission MUST NOT invalidate the request; the values already shown are kept.

**Permission and failure**

- **FR-015**: Where the signed-in person may not see the job's data, the offer MUST NOT be made.
- **FR-016**: Where the job's values cannot be read after acceptance, the step MUST report it unavailable with the time of the attempt and a retry, and MUST NOT proceed with a partly filled request presented as complete.
- **FR-017**: This chunk MUST delete every remaining `Temp` artefact of this feature, MUST leave the build green afterwards, and MUST NOT rename such an artefact into a permanent-looking name in place of removing it.

### Key Entities

- **Job fill offer**: the choice presented when a running job exists at a generic work center, with the job it refers to.
- **Pre-filled answer**: a value taken from that job, marked as such, changeable, and cleared when its item changes.
- **Request provenance**: the job a request's values were taken from, recorded and displayed separately from the request's identity.

## Success Criteria

### Measurable Outcomes

- **SC-001**: On a day a quoted-only work center runs a job, the operator raises the same request with roughly a third fewer keystrokes than with no fill, measured on a real job.
- **SC-002**: Declining the offer leaves a request that is indistinguishable from one raised at a work center with no job at all.
- **SC-003**: Zero filled requests carry a work order as their identity, on any surface or in any export.
- **SC-004**: Zero pre-filled values reach a request without still being proved where their owning chunk proves them.
- **SC-005**: Every pre-filled value can be identified as taken from the job at submission, or as changed by the requester.

## Scope & Dependencies

**Builds on**: spec 016 (the generic flow, its confirmation step, the queue path and the card) — this chunk adds an offer to that flow and changes nothing else about it.

**Enriches, does not replace, later chunks**: where 017, 018, 019 or 020 have shipped, this chunk pre-fills their candidates and their proof still runs. Where they have not shipped, the fill supplies only the answers spec 016 already asks for.

**Deliberately absent**: nothing after this chunk. This is the last one.

**No earlier chunk needs this one**: 015 to 020 all behave correctly when this offer never appears.

## Assumptions

- **A1**: "A running job" means the same thing the work-order flow already means by an active setup job at a work center.
- **A2**: Reading a job's values after the requester accepts uses the same reads the work-order flow already performs; no new integration is introduced.
- **A3**: The offer is per request, not per work center and not remembered, so a requester who declined once is asked again on their next request.
- **A4**: Provenance is a stored field on the request and a rendered line on the detail page; it is not a card cell, because the card must not show a work order.
