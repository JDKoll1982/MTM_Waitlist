# Feature Specification: Scrap Lugger Location

**Feature Branch**: `013-lugger-location-config`
**Created**: 2026-10-17
**Status**: Draft
**Input**: Close the one cell on the waitlist line card that has never had a source. Spec 012 (`specs/012-waitlist-card-layout`) requires `pickup-scrap` to show `Scrap Type` and `Lugger Location` (FR-034), and recorded the second as "no source anywhere — absent by design", to be filled later by a single resolver arm. The owner has now settled what a lugger actually is, who maintains it, and how it behaves, so the cell is fed by a small configuration capability rather than a read of the job. Decisions below are the owner's, taken in review on 2026-10-17.

## User Scenarios & Testing

### User Story 1 - The scrap card names where the material goes (Priority: P1)

A material handler opens the waitlist and reads a scrap request's card. The `Lugger Location` cell names the lugger that job's scrap is dumped into — `Door 6 Lugger` for a 3003 Aluminum job — instead of the "In development" placeholder it has always drawn. The handler knows where to walk without asking anybody.

**Why this priority**: this is the whole of the cell that spec 012 left unfinished, and it is the only part of this feature a material handler standing in the plant sees. Everything else exists to keep this value true.

**Independent Test**: with the shipped defaults and no configuration change, draw a `pickup-scrap` card for each of the seven assigned scrap types and confirm each names the lugger the defaults assign, and that no card draws the placeholder.

**Acceptance Scenarios**:

1. **Given** a scrap request whose job recorded `3003 Aluminum`, **When** its card is drawn, **Then** `Lugger Location` reads `Door 6 Lugger`.
2. **Given** a scrap request whose job recorded `Skeleton`, **When** its card is drawn, **Then** `Lugger Location` reads `Skeleton Lugger`.
3. **Given** a scrap request whose job recorded `Gaylord`, **When** its card is drawn, **Then** `Lugger Location` reads `Scrap Trailer`.
4. **Given** a scrap request whose job recorded `5052 aluminum` in a different letter case or with surrounding spaces, **When** its card is drawn, **Then** `Lugger Location` reads `Door 8 Lugger`, because the assignment is matched without regard to case or spacing.
5. **Given** any scrap request, **When** its card is drawn, **Then** the cell is labelled `Lugger Location` and no cell reads `In development`.
6. **Given** a job with no real scrap decision — the `Scrap Type Required` placeholder or `No Scrap` — **When** its card is drawn, **Then** no lugger cell appears at all, and nothing about its absence is recorded as a failure.

---

### User Story 2 - Blocking a lugger moves the dump point by itself (Priority: P1)

A lugger fills up. The Material Handler Lead sets it to `Full`, and the system sends the scrap type to that lugger's backup without the lead editing anything else. When the full lugger is emptied, it comes back as a backup rather than grabbing the job back, so a handler is never sent to a lugger nobody has confirmed is ready. When the second lugger fills in turn, the first takes over again.

**Why this priority**: a card that names a lugger which cannot take material is worse than no card at all — it sends a handler across the plant to a closed door. The transitions are what keep the answer true between configuration edits, and the owner's worked example is the acceptance criterion for the feature.

**Independent Test**: with `Door 6 Lugger` available and `Extra Lugger (Outside)` its backup for 3003 Aluminum, walk the four steps below and read the card after each one; then confirm the same transitions hold when the lead promotes a lugger explicitly.

**Acceptance Scenarios**:

1. **Given** `Door 6 Lugger` is the dump point for 3003 Aluminum and `Extra Lugger (Outside)` is its backup, **When** the card is drawn, **Then** it reads `Door 6 Lugger`.
2. **Given** that state, **When** the lead sets `Door 6 Lugger` to `Full`, **Then** `Door 6 Lugger` is `Full`, `Extra Lugger (Outside)` becomes the dump point, and the card reads `Extra Lugger (Outside)`.
3. **Given** `Door 6 Lugger` is `Full` and `Extra Lugger (Outside)` is the dump point, **When** `Door 6 Lugger` is emptied, **Then** `Door 6 Lugger` becomes a backup, `Extra Lugger (Outside)` remains the dump point, and the card still reads `Extra Lugger (Outside)`.
4. **Given** that state, **When** the lead sets `Extra Lugger (Outside)` to `Full`, **Then** `Door 6 Lugger` becomes the dump point and the card reads `Door 6 Lugger`.
5. **Given** a lugger is set to `Do Not Use` with a reason, **Then** it is treated exactly as `Full` for promotion, and its reason is readable by a handler.
6. **Given** a scrap type whose luggers are all blocked, **When** a card for that scrap type is drawn, **Then** the cell names the first lugger in the order with its status beside the name, and does not draw blank or absent.
7. **Given** a lead chooses a lugger and promotes it explicitly, **Then** that lugger becomes the dump point and the previous one becomes a backup.
8. **Given** a scrap type with at least one lugger that can receive material, **When** any sequence of status changes completes, **Then** exactly one lugger holds the dump point and no blocked lugger is ordered above it.

---

### User Story 3 - The lead keeps the luggers straight (Priority: P2)

A Material Handler Lead opens the application's settings and finds the lugger arrangement: the seven luggers with their statuses, and which lugger each scrap type dumps into. From there they rename a door that was repainted, remove a lugger that has been taken away, add one that has arrived, and change which scrap types a lugger takes. Nobody else can open it.

**Why this priority**: the plant's luggers are not permanent. Without this, the defaults are the only arrangement that can ever exist and the first reorganisation silently makes the card wrong.

**Independent Test**: as a Material Handler Lead, rename `Door 6 Lugger`, remove `Scrap Trailer`, add `Door 10 Lugger` and assign `Skeleton` to it; confirm each change is visible in the summary list and on a scrap card; then confirm a person below that role cannot reach the editor by any route.

**Acceptance Scenarios**:

1. **Given** a Material Handler Lead opens the lugger arrangement from settings, **Then** the summary lists every lugger with its name, its status, and the scrap types it takes.
2. **Given** that list, **When** the lead renames `Door 6 Lugger` to another name, **Then** the new name appears on every scrap card for a scrap type that dumps there.
3. **Given** that list, **When** the lead adds a lugger, **Then** it appears in the list and is offered for assignment.
4. **Given** that list, **When** the lead removes a lugger, **Then** it is gone from the list, its assignments are gone with it, and no scrap card names it.
5. **Given** that list, **When** the lead assigns a scrap type to a lugger and orders the assignment, **Then** the order shown is the order the transitions follow (User Story 2).
6. **Given** a person who does not hold the lugger permission, **When** they use any route into the settings area, **Then** they cannot open the arrangement or the editor at all — not shown-but-disabled.
7. **Given** a lead sets a lugger to `Do Not Use`, **Then** a reason is required and cannot be left blank.

---

### User Story 4 - Handlers are told when the arrangement changes (Priority: P2)

A lead moves a scrap type to a different lugger during a shift. Every material handler who is using the application is told, in the application, which lugger changed and what it is now — so nobody spends the rest of the shift walking to the old door.

**Why this priority**: a handler who read the card an hour ago is holding an instruction that has since been overtaken. Without the notice the live value is correct but unread.

**Independent Test**: with two material handler sessions open, change a lugger's status in a third; confirm both sessions show a notice naming the change, that dismissing it in one leaves it dismissible in the other, and that it does not return in either.

**Acceptance Scenarios**:

1. **Given** a material handler is using the application, **When** a lead adds, renames or removes a lugger, or changes its status, or changes an assignment, **Then** that handler is shown an in-application notice naming what changed.
2. **Given** the notice names a rename, **Then** it names both the old and the new name.
3. **Given** a handler dismisses a notice, **When** they continue working, **Then** that notice is not shown to them again.
4. **Given** a change is made while no material handler is using the application, **When** one next signs in, **Then** they are not shown that change as if it had just happened.
5. **Given** a person who does not hold the permission to handle requests, **When** an arrangement changes, **Then** they are not interrupted by the notice.
6. **Given** any arrangement change, **Then** no operating-system notification is raised for it.

---

### Edge Cases

- **Every lugger for a scrap type is blocked.** The card names the first lugger in the order with its status beside the name, so the handler reads "no usable dump point" rather than nothing.
- **A lugger is removed while it is the only lugger for a scrap type.** That scrap type then has no assignment; its cards draw no lugger cell and the gap is recorded for whoever maintains the configuration. No substitute is invented.
- **A removal does not rewrite history.** An already-raised request stores nothing about the lugger, so this must be a deliberate consequence of reading live: an open card reflects the current arrangement, not the one that was in force when it was raised.
- **A lugger takes two scrap types.** Its status is a property of the lugger, so blocking it blocks both, and each scrap type promotes its own next lugger.
- **Two leads change the same scrap type's status at the same moment.** One outcome is applied and nothing is left half-moved: never two dump points for one scrap type, and never a blocked lugger left holding the role of dump point while another could receive.
- **A job's scrap type has never been configured at all.** The card draws no lugger cell; the absence is a configuration gap worth recording, never a value worth inventing.
- **A scrap type is renamed in the plant's vocabulary.** A type the configuration has never heard of simply has no lugger; the lead adds the assignment to fix it.
- **A lugger is renamed while a handler is mid-shift.** The notice names both names, so the handler recognises the lugger they were told about.
- **The `Scrap Type Required` placeholder and `No Scrap`.** Neither is a scrap type and neither may be assigned a lugger.

## Requirements

### Functional Requirements

**The lugger set and its defaults**

- **FR-001**: The application MUST maintain a managed set of named luggers, each with a name, a status, and a position in the display order.
- **FR-002**: A fresh installation MUST ship seven luggers, named exactly `Door 6 Lugger`, `Door 7 Lugger`, `Door 8 Lugger`, `Door 9 Lugger`, `Extra Lugger (Outside)`, `Skeleton Lugger` and `Scrap Trailer`.
- **FR-003**: Every one of the seven MUST be renameable, removable and re-creatable by an authorised role; none is fixed by the application.
- **FR-004**: A lugger's name MUST be unique across the set, so a lead and a handler always mean the same door by the same word.
- **FR-005**: Removing a lugger MUST also remove its assignments, and MUST NOT leave an assignment pointing at a lugger that no longer exists.

**Assignment of scrap types**

- **FR-006**: A scrap type MUST be assignable to an ordered set of luggers, and the order MUST be the order the system tries them in.
- **FR-007**: A lugger MAY take more than one scrap type, and a scrap type MAY name more than one lugger.
- **FR-008**: A fresh installation MUST ship these assignments: `3003 Aluminum` to `Door 6 Lugger`; `Galvanized Steel` to `Door 7 Lugger`; `5052 Aluminum` to `Door 8 Lugger`; `Steel` to `Door 9 Lugger`; `3003 Aluminum` to `Extra Lugger (Outside)` as the backup to `Door 6 Lugger`; `Skeleton` to `Skeleton Lugger`; and `Gaylord` to `Scrap Trailer`.
- **FR-009**: A job's stored scrap type MUST be matched to its assignment without regard to letter case or surrounding spaces, so a value the plant records in a different case still finds its lugger.
- **FR-010**: The two values that are not scrap types — `Scrap Type Required` and `No Scrap` — MUST NOT be assignable to a lugger and MUST never produce a lugger on a card.
- **FR-011**: The editor MUST offer the scrap types the setup workflow already recognises, and MUST allow one that is not among them to be recorded, because the setup workflow accepts a scrap type it does not recognise and keeps it.

**Status and the dump point**

- **FR-012**: A lugger MUST carry exactly one status at a time: `Available`, `Backup`, `Full`, or `Do Not Use` with a recorded reason.
- **FR-013**: For each scrap type, exactly one of its luggers MUST hold the role of dump point — the one a handler is sent to — whenever at least one of them can receive material. The rest MUST hold `Backup`, `Full`, or `Do Not Use`.
- **FR-014**: The system MUST perform the transitions between statuses; a lead MUST NOT have to keep the set consistent by hand.
- **FR-015**: Setting the dump point to `Full` or `Do Not Use` MUST promote the next lugger in that scrap type's order to be the new dump point.
- **FR-016**: A lugger that has been emptied MUST return to `Backup` and MUST NOT reclaim the role of dump point by itself; the lugger that holds the role MUST keep it until it is blocked or another is promoted.
- **FR-017**: A blocked lugger MUST NOT be ordered above the dump point, so the order and the role never disagree and a reader may use either.
- **FR-018**: A Material Handler Lead MUST be able to promote a chosen lugger explicitly, and promoting it MUST return the previous dump point to `Backup`.
- **FR-019**: `Do Not Use` MUST require a reason, and the reason MUST be readable by a handler asking about that lugger.
- **FR-020**: A status change that affects several scrap types MUST leave every affected scrap type in a completed state — never two dump points, and never a dump point it cannot use while another lugger could receive.
- **FR-021**: A lugger's status MUST apply to every scrap type assigned to it, because the lugger itself is what fills up.

**What the card shows**

- **FR-022**: The `Lugger Location` cell on `pickup-scrap` MUST show the name of the lugger holding the dump point for the requesting job's scrap type.
- **FR-023**: The value MUST be read when the card is drawn, not stored when the request was raised, so a status change is reflected the next time the list loads.
- **FR-024**: When no lugger for the scrap type can receive material, the cell MUST name the first lugger in the order with its status beside the name, rather than drawing nothing.
- **FR-025**: When the job has no real scrap decision, the card MUST draw no lugger cell, and its absence MUST NOT be recorded as a failure.
- **FR-026**: When the scrap type has no assignment at all, the card MUST draw no lugger cell and MUST record the gap so the configuration can be corrected; it MUST NOT show a substitute value.
- **FR-027**: The cell's label MUST remain `Lugger Location`, and the card's layout MUST NOT change.

**Who may manage the arrangement**

- **FR-028**: A permission of its own MUST govern the lugger arrangement, held by Material Handler Lead and above and withheld from everybody else.
- **FR-029**: A person without that permission MUST be unable to open the arrangement or its editor by any route — not merely shown a disabled control.
- **FR-030**: The arrangement MUST be reachable from the application's settings area, as a summary of the luggers with their status and the scrap types each takes, with the editor opened by an explicit action from there.
- **FR-031**: The editor MUST show, for each scrap type, which lugger currently holds the dump point.
- **FR-032**: Every change to the arrangement MUST record who made it and when, so a wrong lugger can be traced to the edit that caused it.

**Telling the handlers**

- **FR-033**: When a lugger is added, renamed or removed, when its status changes, or when an assignment is added, removed or reordered, every person who holds the permission to handle requests and is using the application at that moment MUST be told, inside the application, what changed.
- **FR-034**: The notice MUST be an in-application notice; no operating-system notification is used for it.
- **FR-035**: A notice MUST name what changed in plain words, naming the lugger and the scrap type, and naming both the previous and the new value where one changed.
- **FR-036**: Each person MUST be shown a given notice once, and dismissing it MUST NOT allow it to return for that person.
- **FR-037**: A person who was not using the application when a change was made MUST NOT be shown that change later as though it had just happened.
- **FR-038**: A person who does not hold the permission to handle requests MUST NOT be interrupted by the notice.

**Retiring what this replaces**

- **FR-039**: The "In development" placeholder for this cell MUST be retired completely: the cell's entry in the placeholder list, the placeholder's own wording, and the branch that draws it MUST all go, so no cell anywhere can be marked not-yet-built by that mechanism.

### Statements this feature supersedes

These statements in earlier artifacts are now wrong and must be amended deliberately in a later step; this specification does not edit them.

- `specs/012-waitlist-card-layout/spec.md`, the assumption **"Cells with no source yet"**, which claims `Lugger Location` has no source and is hidden until one exists. It now has a source and is drawn.
- `specs/012-waitlist-card-layout/research.md`, the R1 row for `Lugger Location` — "no source anywhere … absent by design … a future source adds a resolver arm". The source is configuration, not the job, and the revival is not one resolver arm alone.
- `specs/012-waitlist-card-layout/contracts/card-and-identifier.md`, the entry recording `Lugger Location` as a declaration with no resolver arm.
- `specs/012-waitlist-card-layout/data-model.md`, the `pickup-scrap` row's "no resolver arm exists for it today" note.
- The TODO comments in the request Item configuration seed and in the job-field value resolver, both of which describe the one-arm plan this feature replaces.
- The card-rule test and the resolver test that assert the placeholder is drawn and that the label resolves to nothing.

**Not superseded:** spec 012's FR-034 itself. The card still shows `Scrap Type` and `Lugger Location`; only where the second value comes from changes.

### Key Entities

- **Lugger**: a named dump point. Carries a name, a status, a reason when it is `Do Not Use`, a position in the display order, and whether it is still in use.
- **Scrap-type assignment**: the statement that a scrap type dumps through a given lugger, and where that lugger sits in the order for that scrap type.
- **Dump point**: the role one lugger holds for one scrap type at a time — the lugger a handler is sent to. Not a separate thing to maintain; a consequence of the statuses.
- **Arrangement change**: a recorded edit to the set or to an assignment — what changed, from what to what, by whom and when. It is both the audit trail and the source of the notice.
- **Scrap type**: the value a job already records, which the setup workflow offers as a list and accepts beyond that list. It is free text in practice, matched without regard to case.

## Success Criteria

### Measurable Outcomes

- **SC-001**: On a fresh installation, all seven assigned scrap types draw the lugger their defaults name, and no scrap card draws `In development`.
- **SC-002**: The owner's four-step walk-through — available, blocked, emptied, second blocked — produces the stated lugger at every step.
- **SC-003**: At no point, through any sequence of edits, does one scrap type show two dump points, or a dump point that cannot receive while another lugger could.
- **SC-004**: A lead renames, removes and adds a lugger, and reassigns a scrap type, without help, without a restart, and with the change visible on the next card load.
- **SC-005**: Every material handler using the application sees a change notice within one refresh of the change, sees it once, and is not shown it again after dismissing it.
- **SC-006**: A person who does not hold the lugger permission cannot open the arrangement or its editor by any route.
- **SC-007**: No handler is ever shown a lugger the system derived by guessing: every value the cell shows comes from the configuration, and a gap in the configuration is recorded rather than filled.

## Assumptions

- **Status belongs to the lugger, not to the pairing.** A lugger that is full is full for everything it takes, so blocking it blocks all its scrap types at once. The alternative — a status per scrap type — would let one physical lugger be both usable and unusable.
- **The value is live.** An already-raised request names the lugger the arrangement currently sends it to, because the request stores nothing about the lugger. This keeps the card useful and gives up the ability to answer "where was this actually dumped" — recorded here as a deliberate trade rather than an oversight.
- **A gap is hidden and logged.** A scrap type with no assignment draws no cell and is recorded, following the application's existing rule that a value it cannot obtain is absent rather than blank or invented — except where every lugger is blocked, where a status is shown because it tells the handler what to do.
- **"Currently using the application"** means signed in with the waitlist in use at the time of the change. A change made while nobody is present is not replayed to whoever arrives next.
- **A lead may type a scrap type the list does not contain.** The setup workflow already accepts and keeps an unrecognised scrap type, so refusing it here would let a job exist that the configuration can never serve.
- **The seven defaults are a starting arrangement, not a fixed one.** They are seeded once; a second run of the seed must not duplicate them.
- **Existing stores get the arrangement too.** The seed must land on an installed store the way the earlier label correction did, without a reinstall.
- **The notice is not a toast.** Because in-application delivery is the requirement, nothing here depends on the application being packaged.
- **Nothing outside the scrap card changes.** The other Items, the request detail page, the card's layout and the way requests are raised are untouched.

## Verbatim Constraints

Values the owner pinned exactly; the implementation must use these strings.

- The seven default lugger names: `Door 6 Lugger`, `Door 7 Lugger`, `Door 8 Lugger`, `Door 9 Lugger`, `Extra Lugger (Outside)`, `Skeleton Lugger`, `Scrap Trailer`.
- The four statuses: `Available`, `Backup`, `Full`, `Do Not Use`.
- The default assignments, by scrap type spelling: `3003 Aluminum` → `Door 6 Lugger` (primary) and → `Extra Lugger (Outside)` (backup); `Galvanized Steel` → `Door 7 Lugger`; `5052 Aluminum` → `Door 8 Lugger`; `Steel` → `Door 9 Lugger`; `Skeleton` → `Skeleton Lugger`; `Gaylord` → `Scrap Trailer`.
- The cell's label: `Lugger Location`.
- The role that must hold the arrangement permission: `Material Handler Lead`.
