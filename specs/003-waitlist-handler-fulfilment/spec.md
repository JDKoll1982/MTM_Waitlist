# Feature Specification: Waitlist Handler Fulfilment and Urgency Ordering

**Feature Branch**: `003-waitlist-handler-fulfilment`
**Created**: 2026-09-12
**Status**: Draft
**Input**: Spec seed `WeekendProject/SpecTemplates/02-handler-fulfilment-and-urgency.md` — from
`PromptFiles/07-8%-Phase2-fulfill.md`, `PromptFiles/08-70%-Phase2-urgency.md`, and `prompt.md` tasks 16–18.

## User Scenarios & Testing

The waitlist currently shows a request but nobody can act on it. The rules that decide who may do what
already exist and are tested; what is missing is the handler's side of the request lifecycle on the
screens people actually use. This feature adds that surface, and only that surface.

### User Story 1 - A handler takes a request (Priority: P1)

A material handler opens the waitlist, sees an unclaimed request and claims it for themselves.

**Why this priority**: This is the single action that turns the list from a read-only display into work
that can be done. Everything else in this feature builds on it. On its own it unblocks the floor.

**Independent Test**: As a handler, accept an unclaimed request and confirm it is claimed by that person,
that its status is In Progress, that it is still on the shared list, and that the Accept offer is gone
for everyone else.

**Acceptance Scenarios**:

1. **Given** an unclaimed request and a viewer at handler level, **When** the request is shown, **Then**
   an Accept affordance is offered as an icon button, and it is offered on the request detail as well.
2. **Given** an unclaimed request, **When** the viewer accepts it, **Then** the request records that
   viewer as its assignee, moves to In Progress, and the new state is persisted.
3. **Given** a request that a handler has just accepted, **When** any other viewer looks at the list,
   **Then** the request is still visible and unclaimed requests are distinguishable from claimed ones.
4. **Given** a request accepted by another handler, **When** a different handler looks at it, **Then**
   no Accept affordance is offered.
5. **Given** a request accepted by another handler, **When** the accepting handler looks at it, **Then**
   Complete and Release are offered to that person and to nobody else.
6. **Given** two handlers looking at the same available request, **When** both try to accept it at the same
   moment, **Then** only the first claim stands, the winning handler is told nothing went wrong, and the
   other handler is shown a warning they must acknowledge saying another handler already took the request
   and to choose a different one.

### User Story 2 - The assigned handler finishes or hands back the work (Priority: P2)

The handler who took a request either marks it done or puts it back on the open list for someone else.

**Why this priority**: Completing closes the loop; releasing is the escape hatch that keeps a mistaken
claim from stranding work. Both are meaningless before P1 exists.

**Independent Test**: As the assigned handler, complete a request and confirm it leaves the open list as
Done; then, on a second request, release it and confirm it returns to the open list as available with no
assignee — and that it is not recorded as cancelled.

**Acceptance Scenarios**:

1. **Given** a request the viewer accepted, **When** the viewer completes it, **Then** it moves to Done
   and leaves the open list.
2. **Given** a request the viewer accepted, **When** the viewer releases it, **Then** it returns to the
   open list as available, carries no assignee, and its history records a release rather than a
   cancellation.
3. **Given** a released request, **When** any handler looks at the list, **Then** Accept is offered again.
4. **Given** a request that is Done or Cancelled, **When** any viewer looks at it, **Then** no handler
   action is offered.
5. **Given** a request that the viewer did not accept, **When** the viewer looks at it, **Then** neither
   Complete nor Release is offered.

### User Story 3 - The list leads with the most urgent work (Priority: P3)

A lead opens the list and the oldest, most overdue work is at the top.

**Why this priority**: Ordering is what makes a long list actionable. It depends on nothing else here and
delivers value the moment it lands, but without the actions above a sorted list still cannot be worked.

**Independent Test**: With several open requests of differing ages and allotted times, confirm the list
reads most-overdue first, then least time remaining.

**Acceptance Scenarios**:

1. **Given** several open requests, **When** the list is built, **Then** the most overdue request is
   first, followed by the others in decreasing urgency.
2. **Given** no request is overdue, **When** the list is built, **Then** the request with the least time
   remaining is first.
3. **Given** a request that becomes overdue while the screen is open, **When** the display next refreshes,
   **Then** its position updates to match its new urgency.

### User Story 4 - A requester withdraws their own request (Priority: P4)

Someone who raised a request by mistake withdraws it before a handler picks it up.

**Why this priority**: It prevents wasted handler effort, but the handler actions above are what the
floor is waiting for; withdrawal only matters once work is being claimed.

**Independent Test**: Raise a request, cancel it from the request detail with a reason, and confirm it
leaves the active list, that the reason is retained, and that the record is still readable.

**Acceptance Scenarios**:

1. **Given** the viewer's own request while it is still waiting, **When** the viewer chooses to cancel,
   **Then** a confirmation is required and a reason can be supplied before anything happens.
2. **Given** a confirmed cancellation, **When** it is recorded, **Then** the request leaves the active
   list, keeps its reason and who cancelled it, and is never deleted.
3. **Given** a request the viewer did not raise, or one that is no longer waiting, **When** the viewer
   looks at it, **Then** no cancel affordance is offered.

### User Story 5 - A handler leaves a short note (Priority: P5)

Anyone signed in records a one-line message against a request — a handler noting what they found, or the person
who raised it adding something to what they asked for. The story was scoped to handlers when it was written and
was widened after the first build; see FR-012 and the Assumptions.

**Why this priority**: Useful context for the next person, but the request can be worked without it.

**Independent Test**: Send a message, reopen the request and its history, and confirm the message is shown in
both, named with its sender.

**Acceptance Scenarios**:

1. **Given** a request someone is working, **When** they send a message, **Then** it is stored and shown on
   the request detail.
2. **Given** a message that was sent, **When** the request's history is read, **Then** it appears there once,
   with the sender's name rather than the internal event name.
3. **Given** an existing message, **When** its author corrects it, **Then** the new text replaces the old and
   the change is recorded.
4. **Given** a message that has been sent, **When** the other party next looks at the list, **Then** the card
   carries the new-message indicator until they have read it.

### Edge Cases

- A request is accepted by handler A while handler B has the same request open and is about to accept it.
  Only the first claim may succeed; the second must be refused in plain language rather than appearing to
  work.
- A request is cancelled by its requester after a handler accepted it. The action must be refused, and the
  card must reflect that the request is no longer waiting.
- The request list is empty, or a filter such as "My Requests" leaves no rows. The existing friendly empty
  state must still be shown and must not be replaced by this feature's ordering or action work.
- A screen has been open for several minutes so the remaining-time values on screen are stale. Actions
  must be authorized against the stored state, not the stale screen, and a refused action must say so.
- The same row is refreshed while an action is in flight. The list must settle on the stored result rather
  than on whichever response lands last.
- A note is submitted empty or as whitespace. The request must not gain an empty note.
- A message is typed but the page refreshes before it is sent. The refresh must not discard what was typed,
  and a save must never report success for a message it did not store.
- A request type has no image configured by an administrator. The card must show the image the request itself
  carries rather than the resolver's "no image available" placeholder.
- The store is unreachable when an action is attempted. The action must fail with the existing per-screen
  unavailable state and a manual retry — never with a fabricated success.

## Requirements

### Functional Requirements

- **FR-001**: A viewer at Material Handler level or above MUST be offered an Accept affordance on a
  request that has no assignee and has not reached a finished or cancelled state; the affordance MUST be
  an icon button rather than text, and it MUST sit on the request's card in the list.
- **FR-002**: Accepting a request MUST record the signed-in viewer as the request's assignee, move the
  request to In Progress, and persist that state.
- **FR-003**: An accepted request MUST remain visible on the shared list to every viewer.
- **FR-004**: Once a request is accepted, its Accept affordance MUST be replaced on that card by a Complete
  affordance for the viewer who accepted it, and no Accept affordance MUST be offered on it to anyone else.
- **FR-005**: Complete MUST be offered only to the handler recorded as the request's assignee, and the
  Cancel affordance MUST be offered on a card whenever Complete is — so a handler who has claimed a request
  can refuse it as readily as they can finish it. A request assigned to another handler MUST offer the
  viewer no action at all.
- **FR-006**: Completing a request MUST move it to Done in a single step and persist that state.
- **FR-007**: Releasing a request MUST return it to the open list as available with no assignee, and MUST
  be recorded as a release, never as a cancellation. Release MUST remain available in the service, and no
  card MUST draw a Release control: the card's action area carries at most the primary action and Cancel.
- **FR-008**: A viewer who is not at handler level MUST NOT be offered any handler action.
- **FR-009**: No handler action MUST be offered on a request that has reached a finished or cancelled
  state, regardless of who the viewer is.
- **FR-010**: The requester MUST be able to cancel their own request while it is still waiting, and the
  handler recorded as a request's assignee MUST be able to cancel that request. Both MUST first confirm the
  cancellation and be able to give a reason.
- **FR-011**: A cancelled request MUST be recorded with its reason and the identity of whoever cancelled
  it, MUST leave the active list, and MUST NOT be deleted.
- **FR-012**: Anyone signed in MUST be able to send a message on a request, and MUST be able to correct the
  message they sent. Sending MUST be a single action that stores what the viewer has typed without their
  having to leave the field first, and a draft they have not sent MUST NOT be erased by the page's own refresh.
- **FR-013**: A stored note MUST be visible on the request detail and its addition MUST appear in the
  request's history. The history MUST name the person responsible for each entry and MUST NOT show the internal
  event type to the user; an entry with no person is attributed to the system. The history MUST be read from
  the store rather than from what this session happened to observe, so a request that was acted on before this
  session started still shows what happened to it; a history that could not be read MUST NOT be rendered as a
  request that has no history, and an entry MUST appear exactly once however many times the history is refreshed.
- **FR-014**: The request card MUST surface the handler-needed data the request actually carries —
  requester, destination press or work center, remaining or overdue time, waiting age, and the
  type-specific detail values — without adding sections to the card's fixed layout. Where the request
  carries no resolved material identifier, the card MUST show nothing in its place rather than a
  substitute.
- **FR-015**: The available-request list MUST be ordered most-urgent-first using the due, remaining and
  overdue values the application already computes, so the most overdue and oldest work appears first.
- **FR-016**: Every lifecycle action MUST be performed through the stored procedures the application
  already uses for the request lifecycle; no action may write inline statement text.
- **FR-017**: Every lifecycle action and every note change MUST record who performed it and when in the
  request's audit history.
- **FR-018**: Every action MUST be authorized by the same role and ownership rules the service enforces,
  independently of what the screen offers, so a stale or forged screen can never widen a viewer's rights.
- **FR-019**: A refused action MUST be reported to the user in plain, actionable language; no action may
  fail silently or appear to succeed when it did not.
- **FR-020**: No control may be drawn in the card's action area whose activation has no effect.
- **FR-021**: The accepted card anatomy MUST NOT be regressed (see Verbatim Constraints).
- **FR-022**: Every new user-visible string MUST be localized through the application's existing resource
  mechanism rather than written into markup.
- **FR-023**: After a successful action the affected list and the request detail MUST reflect the new
  stored state without the user reloading the screen.
- **FR-024**: When two handlers attempt to claim the same request at the same time, exactly one claim MUST
  stand — the one the store accepted first — and the handler whose claim was refused MUST be shown a warning
  they have to acknowledge, naming the request as already taken by another handler and directing them to
  choose a different request. The losing screen MUST NOT report success, and the winning claim MUST NOT be
  altered by the losing attempt.
- **FR-025**: The lifecycle actions MUST live on the request's card in the list, not on the request detail
  page. The detail page is for reading a request — its values, its sections, its note and its history — and
  MUST NOT draw an accept, complete, release or cancel control. There MUST be exactly one surface that
  decides what a viewer may do to a request.
- **FR-026**: The request detail page MUST reload the request and its history on a stated cadence (30
  seconds) while it is open, so activity another handler caused arrives without the viewer doing anything.
  Reaching the handler actions MUST NOT require the viewer to open the request: FR-025 is why, and the card
  they are already looking at is where the work is claimed.
- **FR-027**: A card MUST show a new-message indicator on its request-type image while the request changed
  after the viewer last read it, and the indicator MUST clear once they have read it. The marker it is
  compared against MUST be the newest entry the viewer has seen rather than the moment they looked, so an
  entry that lands while the page is open re-flags the card instead of being silently swallowed, and the
  indicator MUST persist across a restart.
- **FR-028**: The indicator MUST be raised for a message a person sent and MUST NOT be raised by the request
  moving through its lifecycle, so a request that was accepted, completed or cancelled without anybody
  speaking MUST NOT be reported as having something to read.
- **FR-029**: The card's status indicator MUST use a different colour for each lifecycle state, so the state
  is readable at a glance rather than only by reading its label, and each colour MUST carry its text legibly.
- **FR-030**: Accept and Complete MUST be distinguishable from each other by both colour and icon, because
  they occupy the same control in two states, and neither MUST use a glyph the icon font marks as deprecated.
- **FR-031**: A card MUST show the image its own request already resolves to and MUST NOT replace it with a
  generic placeholder; the placeholder is shown only when the request carries no image of its own. This is
  FR-014's rule about not substituting for a value the request does not carry, applied to the card's image.

**Provenance.** FR-001 … FR-023 were specified before any code existed. FR-024 … FR-031 were added
*afterwards*, each prompted by using the shipped build: FR-024 (two handlers claiming one request) came out
of the spec review; FR-025 … FR-027 (the actions belong on the card; the page refreshes itself; the
new-message indicator) from the first fix pass; and FR-028 … FR-031 from later passes — the indicator firing
on lifecycle changes, one colour for every status, Accept and Complete collapsing into one-looking control,
and a card's own image being replaced by the "no image available" placeholder. They are numbered in place
rather than folded into the earlier requirements so that what was asked for before the code existed, and what
was asked for after using it, both stay legible.

### Key Entities

- **Waitlist request** — a unit of work asked for by one person and, once claimed, owned by a handler. Its
  lifecycle status is one of waiting, in progress, done or cancelled; it carries the requester's identity,
  the destination press or work center, the request type and sub-type, the type-specific detail values,
  the requested time, the target time, an optional assignee, an optional cancellation reason and
  canceller, and an optional short note. It gains an assignee on acceptance, a completion time on
  completion, and a release time on release.
- **Audit entry** — an append-only record of something that happened to a request: what changed, who did
  it, and when. Cancellations, acceptances, completions, releases and note changes all produce one. Audit
  entries are never edited or removed, so the history is a faithful account of who did what.
- **Urgency state** — a derived, non-stored view of a request: when it is due, how long remains, and
  whether it is overdue. It is computed from the request's requested time and the allotted time configured
  for its sub-type, and it is the value the list is ordered by.
- **Viewer role** — the role carried by the signed-in person, used to decide whether they may handle
  requests at all. Material Handler is the lowest role that may.

## Success Criteria

### Measurable Outcomes

- **SC-001**: A handler can take an unclaimed request from the list in a single action, with no screen
  reload, and the claim is visible to a second user who opens the same list afterwards.
- **SC-002**: Of the actions a viewer is not entitled to perform, zero are offered — measured by tests
  covering every combination of viewer role (handler / non-handler) and request state (available, taken
  by viewer, taken by another, done, cancelled).
- **SC-003**: Zero requests can be completed or released by anyone other than the recorded assignee,
  proven by attempting each action as a non-assignee and confirming refusal.
- **SC-004**: A released request is back on the open list and accept-able by another handler; a cancelled
  request is not. Both outcomes are distinguishable in the request's history.
- **SC-005**: With a set of open requests of known ages and allotted times, the list order matches the
  expected most-urgent-first order in 100% of ordering test cases.
- **SC-006**: Every lifecycle action and note change leaves exactly one audit entry naming the actor and
  the time, verifiable for cancel, accept, complete, release and note.
- **SC-007**: Zero user-visible strings introduced by this feature are unlocalized.
- **SC-008**: The full test suite reports no failures and the solution builds with no warnings and no
  errors.
- **SC-009**: Of two handlers claiming the same request at the same instant, exactly one claim stands and the
  other handler is told — in a message they must acknowledge — that the request was already taken and to choose
  a different one. Verified by tests that run both claims against one store and assert both the stored assignee
  and both screens' outcomes.
- **SC-010**: Zero cards replace an image the request already resolves to with a generic placeholder. Verified
  by a test that asserts the placeholder is refused, and by a running-application pass that leaves the list and
  returns to it and finds every card's image unchanged.

## Assumptions

- **"Material Handler or above" is resolved by the waitlist screen itself for now.** The codebase has no
  shared role-rank helper; the lowest handler role is Material Handler, and the screen resolves the
  viewer's role against that same vocabulary the settings screens already use. Consolidating role ranks
  into one shared helper is a separate, later piece of work and is not required here.
- **The assignee is recorded on the request's existing assignee field**, which the model and service
  already carry and persist; releasing clears it. No new storage is introduced for assignment.
- **The creator's Edit affordance is dropped.** The source note describes it as visible with no behaviour.
  This feature draws only controls whose activation has an effect, so an Edit button with nothing behind it
  is not added; the creator's meaningful action is cancelling their own request.
- **Writing on a request is open to anyone signed in** — revised after the first build. The message box was
  originally gated on the handler role. Using it on the floor showed that the people who raise requests could
  not tell a handler anything, so the gate was removed from both the markup and the command. The stored column
  and the audit trail are unchanged; only who may write to them changed. The creator's own other actions
  remain cancellation and withdraw.
- **Handler-needed card data is limited to what the request truthfully carries.** Real material
  identifiers — the coil or part number, the quantity, and the stock location — are resolved by the
  separate unified-card and item-resolution workstream, which replaces the card's placeholder values with
  resolved ones. Until that lands, this feature surfaces the handler data the request already holds and
  shows nothing where a value is absent; it must never reintroduce a fabricated material identifier.
- **Multi-step handoff after completion is out of scope.** Completion is a single step now; seams are left
  for a future handoff, but no handoff behaviour is specified here.
- **The status model, the audit trail and the state transitions already exist and are tested.** This
  feature wires them to the screens; it does not re-implement them.
- **Ordering applies to the shared available-request list**, including when the "My Requests" filter is
  active, since that filter narrows the same list rather than producing a different one.
- **Refusal messages reuse the plain-language messaging the cancel path already produces**, extended with
  wording for the accept, complete and release refusals.

## Verbatim Constraints

The card anatomy below was verified and approved on 2026-09-06 and is explicitly marked "DO NOT REGRESS".
It stays authoritative; the action affordances this feature adds occupy the existing action area without
changing any of these dimensions. Reproduced verbatim from
`WeekendProject/PromptFiles/03-100%-Phase1-listdetail.md`:

> **VERIFIED CARD ANATOMY (2026-09-06, user-approved; DO NOT REGRESS):** The shared `WaitlistLineCardView.xaml` shell is laid out as a spreadsheet-style grid: (a) type/subtype **image is a FIXED 96×96 centered square** (do NOT stretch it full card height); (b) **title sits on its own top row**; (c) below it a content row splits into a **4-row label/value metadata grid** (Requested by / Press / Remaining time / Waiting, labels left, values right, aligned) on the left and the **per-type detail-field grid** on the right; (d) the **action buttons (Edit/Cancel/Accept) are vertically centered** on the right; (e) the **status pill is COMPACT** (fixed ~36px height), below the buttons, spanning the combined button width — do NOT make it fill remaining card height. Each per-type `*WaitlistLineView.xaml` `DetailsContent` renders its fields as a **2×3 four-column label/value grid** (`Auto/*/Auto/*` row pairs: Field0|Field1, Field2|Field3, Field4 spanning col1-3) — Coil uses the SAME grid pattern as the other types (do NOT swap it back to a single `ItemsControl` column). Overdue "Remaining time" is bold red.

The statuses named by the request lifecycle MUST be treated as the exact strings
`Pending`, `Accepted`, `In Progress`, `Completed`, `Done`, `Cancelled` and `Released` wherever a stored
status is produced or compared.
