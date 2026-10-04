# Feature Specification: Identifier-First Sign-In and a Faster Launch

**Feature Branch**: `011-identifier-first-signin`
**Created**: 2026-10-04
**Status**: Draft
**Input**: Three related improvements to the launch and sign-in sequence, delivered as one feature: identifier-first sign-in with self set-up, one window for the whole life of the application, and a faster, more resilient launch.

## Clarifications

### Session 2026-10-04

- Q: What happens when the store holds the employee number but that account is inactive? → A: The person is told the employee is not active, no set-up panel is offered, and no duplicate account is created, even when the plant system also knows the number.
- Q: When a launch passes the 45-second launch-wide budget, what happens? → A: The launch stops at a stated stop whose cause names the budget. Each step's own stated maximum still applies independently, so a step is still abandoned at its own maximum first.
- Q: Where does the lowest rank come from when an account is created at set-up? → A: The store's own rank list is read at creation and its lowest entry, by the store's own ordering, is assigned. No rank is named anywhere in code.
- Q: Can a person leave the set-up panel without creating an account? → A: Yes. Leaving creates nothing, leaves no partial account, and returns to the identifier stage with the form still usable.
- Q: Is the PIN chosen at set-up subject to the temporary-credential attempt limit? → A: No. The chosen PIN is an ordinary credential from creation, so the account never enters the temporary-credential state and the five-attempt rule never applies to it.

## User Scenarios & Testing

### User Story 1 - Signing in by identifier, in two stages (Priority: P1)

A person opens the application and is asked who they are before being asked for a secret. They type either their
user name or their four-digit employee number. If the store already holds them, the second stage asks for their
PIN and everything that follows is exactly what happens today: the attempt count, the session check, the machine
check, the temporary-credential handling and the write of a new PIN all behave as they do now.

This is the smallest slice that delivers value, because it changes who the first field accepts without touching
the credential machinery that is already trusted.

**Why this priority**: it is the foundation the set-up path hangs off, and it can ship and be used on its own for
every person the store already knows, which is the overwhelming majority of sign-ins.

**Independent Test**: sign in with a user name the store holds and a correct PIN, and reach the main screens;
then sign in again with that same person's four-digit employee number and prove the same result and the same
attempt and session behaviour.

**Acceptance Scenarios**:

1. **Given** a person the store holds, **When** they type exactly four digits and continue, **Then** the store is
   searched by employee number and the second stage asks for their PIN.
2. **Given** a person the store holds, **When** they type anything other than four digits and continue, **Then**
   the store is searched by user name and the second stage asks for their PIN.
3. **Given** a person the store holds, **When** they complete the PIN stage, **Then** the attempt count, session
   check, machine check, temporary-credential handling and new-PIN write behave exactly as they do today.
4. **Given** a person who has exhausted their attempts on a temporary credential, **When** they sign in again
   after a restart, **Then** they are still refused, exactly as today.

### User Story 2 - A person the plant system knows sets themselves up (Priority: P1)

A person types their employee number, but the store does not hold them. The application asks the plant system who
that number is. If the plant system knows them, the person sees a panel that says they are not yet set up for the
waitlist, shows their first and last name from the plant system for confirmation (pre-filled and editable), and
asks them to choose a PIN twice. Choosing the account this way replaces the old temporary-PIN step: the PIN they
choose at creation is their credential from the very first time they sign in, and they never pass through the
temporary-credential state.

**Why this priority**: without this, somebody the plant system employs but the waitlist has never seen cannot use
the application, and would need IT to create them by hand each time a new starter arrives.

**Independent Test**: take an employee number the plant system knows and the store does not, complete the set-up
panel with a chosen PIN, then sign in with that employee number and that PIN straight away, confirming no
temporary-credential prompt appeared.

**Acceptance Scenarios**:

1. **Given** an employee number the store does not hold, **When** the plant system knows that number, **Then** a
   set-up panel appears stating the person is not yet set up for the waitlist.
2. **Given** the set-up panel, **When** it appears, **Then** it shows the first and last name from the plant
   system, both pre-filled and editable.
3. **Given** the set-up panel, **When** the person chooses a PIN and confirms it, **Then** the account is created
   on that PIN, so the person never enters the temporary-credential state.
4. **Given** a newly created account, **When** it is stored, **Then** it holds the lowest permission rank, and
   that rank is read from the store rather than named anywhere in code.
5. **Given** the set-up panel, **When** it is shown, **Then** it tells the person that a higher permission level
   needs to be requested from IT.
6. **Given** an employee number in neither the store nor the plant system, **When** the person continues,
   **Then** they are refused in plain words and the form stays open.
7. **Given** the plant system cannot be reached and the local copy of plant data has never held that employee,
   **When** the person continues, **Then** set-up says it cannot be completed right now and does not guess a
   name.

### User Story 3 - A person is not set up, but the form still behaves (Priority: P2)

The set-up panel and the sign-in form are driven by the keyboard in the way a person expects. Enter submits,
exactly as clicking the button does; on the set-up panel Enter moves to the next field and submits only at the
end. A blank field shows its hint instead of doing nothing, so a person is told what is missing rather than
wondering why nothing happened.

**Why this priority**: it is what makes the new stages feel like the rest of the application rather than a
half-finished form, and it is cheap and independently testable.

**Independent Test**: drive the identifier stage, the PIN stage and the set-up panel from the keyboard alone,
including submitting with Enter and submitting a blank field.

**Acceptance Scenarios**:

1. **Given** the identifier stage, **When** the person presses Enter with a value present, **Then** it behaves
   exactly as clicking the continue button.
2. **Given** the PIN stage, **When** the person presses Enter with a value present, **Then** it behaves exactly
   as clicking the submit button.
3. **Given** the set-up panel, **When** the person presses Enter, **Then** focus moves to the next field, and on
   the last field Enter submits the panel.
4. **Given** any stage with a blank required field, **When** the person submits, **Then** the field shows its
   hint rather than silently doing nothing.

### User Story 4 - One window for the whole life of the application (Priority: P2)

Today the launch shows four separate windows: the splash, sign-in, machine setup and blocked. They become stages
of a single window, which hosts the launch and then becomes the shell. While the launch runs, the navigation
panel is hidden, so the window is an empty shell until the launch hands over, at which point the window maximises
and the normal application appears. The window resizes between the big phases and re-centres itself each time it
resizes, and it always opens centred on the screen it starts on.

**Why this priority**: it removes a jarring succession of windows and it is what makes the launch look like a
single, deliberate experience; it is separable from the sign-in work and can land on its own.

**Independent Test**: open the application, watch a full launch, and confirm one window is present throughout,
that the navigation panel is absent until the shell appears, and that the window is centred when it opens and
after each resize.

**Acceptance Scenarios**:

1. **Given** the application is starting, **When** the launch runs, **Then** exactly one window is present, and
   the splash, sign-in, machine-setup and blocked surfaces appear as stages within it.
2. **Given** the launch is running, **When** the window is shown, **Then** the navigation panel is hidden.
3. **Given** the launch hands over, **When** the shell appears, **Then** the window maximises and the navigation
   panel is shown.
4. **Given** the window resizes between big phases, **When** the resize completes, **Then** the window is
   re-centred on its screen.
5. **Given** the application opens, **When** the first window appears, **Then** it is centred on the screen it
   started on.
6. **Given** any point before the hand-over, **When** the application runs, **Then** nothing assumes a signed-in
   person, and the shell cannot be reached from any outcome other than the main-screens outcome.

### User Story 5 - A faster, more resilient launch (Priority: P2)

The launch gains a launch-wide budget of about 45 seconds to reach either the shell or a stated stop. That budget
sits on top of each step's own stated maximum rather than replacing it, so no wait becomes unbounded. Independent
work runs at the same time instead of one after another: the three session checks run together, and the local
reads run alongside the first contact with the store. The picture cache starts at the very beginning of the
launch so it no longer adds waiting time at the end. The external-system priming deliberately stays exactly where
it is, last before the hand-over, because its verdict must be settled before the shell opens.

**Why this priority**: startup time is what people feel every single time they open the application, and the
saving is largest on the slowest day, when the store is slow to answer.

**Independent Test**: instrument a launch and confirm the three session checks overlap, that the local reads
overlap the first store contact, that the picture cache begins before the first store contact finishes, that the
external-system priming is still last, and that a launch that exceeds the budget stops with a stated reason.

**Acceptance Scenarios**:

1. **Given** a launch, **When** it runs normally, **Then** it reaches the shell within the launch-wide budget.
2. **Given** a launch that cannot finish in the budget, **When** the budget is passed, **Then** the launch stops
   with a cause specific to it.
3. **Given** a step that is still running, **When** it passes its own stated maximum, **Then** it is reported
   and not waited on, whatever the launch-wide budget still allows.
4. **Given** the three session checks, **When** they run, **Then** they run at the same time.
5. **Given** the local reads and the first store contact, **When** they run, **Then** they run at the same time.
6. **Given** a launch, **When** it starts, **Then** the picture cache starts at the beginning of the launch.
7. **Given** a launch, **When** the hand-over approaches, **Then** the external-system priming still runs last,
   immediately before the hand-over.
8. **Given** any launch, **When** it runs, **Then** the application makes no request that refreshes the cached
   plant data.

### User Story 6 - The launch surface shows several operations at once (Priority: P3)

The launch surface today assumes one operation is in flight: a single current step and a single flag for a remote
wait. Because the launch now runs some work at the same time, the surface must be able to show more than one line
at once, while keeping the existing rule that every individual operation holds its own line and appears before
the work it describes.

**Why this priority**: it is what makes the concurrency visible and diagnosable; without it a stalled concurrent
launch is no longer attributable to a named line.

**Independent Test**: force a launch in which more than one operation is in flight and confirm the surface shows
each as its own line, each appearing before its work begins.

**Acceptance Scenarios**:

1. **Given** two operations are in flight at once, **When** the surface updates, **Then** both appear as their
   own lines.
2. **Given** an operation, **When** it begins, **Then** its line appears before the work it describes.
3. **Given** several operations have ended, **When** the surface updates, **Then** it does not present a group of
   operations behind a single line.

### User Story 7 - A sixth plant-system read shape (Priority: P3)

The employee lookup the set-up path needs is a new plant-system read shape, and it gets the same cached-fallback
treatment as the five shapes that already exist: a mirror table, a records-twin table, get and refresh
procedures, an entry in the read-shape catalog, a refresh path in the caching service, and a population query in
the module's population folder. The test that asserts the shape count and the database ruleset that says "five"
are both updated deliberately to six.

**Why this priority**: it is real database and service work rather than a one-file change, and it must not be
smuggled in as an afterthought; it exists so the set-up path keeps working when the plant system is slow or down.

**Independent Test**: take the plant system unreachable, run the population query, then exercise the set-up path
for a mirrored employee number and confirm the shape resolves from the mirror.

**Acceptance Scenarios**:

1. **Given** the read-shape catalog, **When** it is read, **Then** it holds six shapes, one of which returns an
   employee by number.
2. **Given** the plant system unreachable and the mirror holding an employee number, **When** that number is
   resolved, **Then** the answer comes from the mirror with the same shape as the live read.
3. **Given** the database ruleset, **When** it is read, **Then** it states six read shapes rather than five.

### Edge Cases

- The identifier is blank, whitespace, four characters that are not digits (for example `12 3`), or more than
  four digits; only exactly four digits is an employee number.
- Two people legitimately share an employee number; the store lookup must answer with the active account first.
- The store holds the employee number but the account is inactive; the person must be told they are not active
  rather than that they do not exist, and no set-up panel is offered for that number, even when the plant system
  also knows it.
- The plant system answers with a name for an employee whose store account exists but is inactive; the set-up
  path must not create a duplicate account.
- The person edits the pre-filled name on the set-up panel; the edited name is what the account is created with.
- The person abandons the set-up panel; nothing is created and the sign-in form is still usable.
- The set-up write fails part-way; no person is left with a credential but no rank, or a rank but no credential.
- The plant system is unreachable and the mirror has never held the employee; set-up says it cannot be completed
  now.
- A launch exceeds the launch-wide budget while a best-effort step is still running; the best-effort step must
  not be what holds the launch.
- The window is resized by the person during the launch rather than by the application; re-centring applies only
  to resizes the launch makes.
- The application is closed during the set-up panel; no partial account is left behind.

### Requirements

#### Functional Requirements

**Sign-in stages**

- FR-001: Sign-in MUST collect an identifier in one stage and a credential in a second stage, in that order.
- FR-002: An identifier of exactly four digits MUST be treated as an employee number; any other identifier MUST
  be treated as a user name.
- FR-003: An employee number MUST be looked up in the store's profile records first.
- FR-004: When the store does not hold the employee number, the number MUST be looked up in the plant system for
  the person's first and last name.
- FR-005: A person the store recognises MUST proceed to the credential stage and thereafter behave exactly as
  today, with the attempt counting, session check, machine check, temporary-credential handling and new-PIN write
  unchanged.
- FR-006: A person the plant system knows but the store does not MUST be offered a set-up panel stating they are
  not yet set up for the waitlist.
- FR-007: The set-up panel MUST show the first and last name from the plant system, pre-filled and editable.
- FR-008: The set-up panel MUST ask for a PIN twice and MUST reuse the existing rules: at least four characters,
  no upper bound, both values typed the same way, and both values travelling as one argument and never as
  properties of the screen.
- FR-009: Creating an account MUST bypass the default PIN of `4330`; the chosen PIN MUST be the credential from
  creation, so the person never enters the temporary-credential state.
- FR-010: A created account MUST be given the lowest permission rank, read at creation from the store's own rank
  list and chosen by the store's ordering rather than named in code.
- FR-011: The set-up panel MUST tell the person that a higher permission level must be requested from IT.
- FR-012: A person in neither the store nor the plant system MUST be refused in plain words, and the form MUST
  stay open.
- FR-013: When the plant system is unreachable and the mirror has never held the employee, set-up MUST state that
  it cannot be completed right now and MUST NOT invent a name.
- FR-014: Enter MUST submit at the identifier and credential stages exactly as clicking the button does; on the
  set-up panel Enter MUST move to the next field and submit only at the last field.
- FR-015: A blank required field MUST show its hint rather than silently doing nothing.

**One window**

- FR-016: The splash, sign-in, machine-setup and blocked surfaces MUST become stages of one window.
- FR-017: That window MUST host the launch stages and then become the shell.
- FR-018: The navigation panel MUST be hidden while the launch runs.
- FR-019: The window MUST maximise when the launch hands over to the shell.
- FR-020: The window MUST resize between the big phases and re-centre after each resize it makes.
- FR-021: The window MUST open centred on the screen it starts on.
- FR-022: The shell MUST remain reachable only from the main-screens outcome.
- FR-023: Nothing in the application MUST assume a signed-in person before the hand-over.

**Faster launch**

- FR-024: The launch MUST carry a launch-wide budget of approximately 45 seconds to reach either the shell or a
  stated stop, and passing that budget MUST end the launch at a stated stop whose cause names the budget.
- FR-024a: Leaving the set-up panel MUST create no account and MUST return the person to the identifier stage
  with the form still usable.
- FR-025: The budget MUST be additional to each step's own stated maximum; every wait MUST keep its own stated
  maximum.
- FR-026: The three session checks MUST run together.
- FR-027: The local reads MUST run alongside the first store contact.
- FR-028: The picture cache MUST start at the beginning of the launch.
- FR-029: The external-system priming MUST stay where it is, last before the hand-over.
- FR-030: The application MUST NOT trigger a refresh of the cached plant data on any launch path.
- FR-031: The launch surface MUST present more than one operation at a time, while every individual operation
  MUST still hold its own line that appears before its work begins.

**New database and service work**

- FR-032: A sixth plant-system read shape MUST return an employee by number and MUST carry the same
  cached-fallback treatment as the other five: a mirror table, a records-twin table, get and refresh procedures,
  a read-shape catalog entry, a refresh path in the caching service, and a population query.
- FR-033: The test that asserts the read-shape count and the database ruleset's statement of the count MUST both
  be updated deliberately to six.
- FR-034: A stored procedure MUST create a person's profile, credential and rank assignment together, and MUST
  ship its paired create and rollback scripts in the same change.

**Reconciliation**

- FR-035: The application MUST NOT copy or keep any shared key file on this computer. The plant's shared key is
  read from its share to honour a remembered sign-in and is never written, copied or held locally, and its path
  MUST never be recorded. Where an instruction document claims that no key is read at all, the discrepancy MUST
  be reported and the stale statement corrected, with no change to behaviour.

**Invariants that must not break**

- FR-036: Every wait on the launch path MUST keep a stated maximum, with thirty seconds the ceiling for any
  single wait and the best-effort steps bounded more tightly.
- FR-037: A stop MUST state a cause specific to it and MUST offer only remedies that could remove it.
- FR-038: The pipeline, not a screen, MUST refuse to continue while this machine is unconfigured.
- FR-039: Roles MUST come from the store only; there MUST be no local role override and no demo or mock mode.
- FR-040: Every data operation MUST go through a stored procedure, and every schema artifact MUST ship its
  rollback in the same change.
- FR-041: The replaced launch behaviour MUST NOT return.
- FR-042: Nothing MUST be kept on a computer except the local picture copy.
- FR-043: A launch MUST end at exactly one of five outcomes, and the shell MUST be reachable only from the
  main-screens outcome.
- FR-044: The two-stage form reveals whether an identifier exists. This is accepted for an internal application
  and MUST be recorded rather than silently closed.
- FR-045: The PIN chosen at set-up MUST be an ordinary credential from creation, so the account MUST never enter
  the temporary-credential state and the five-attempt rule MUST never apply to it.

### Key Entities

- **Identifier**: what a person signs in with. Either a user name or a four-digit employee number. An employee
  number is deliberately not unique by itself.
- **Account (profile)**: a person the waitlist knows, holding a display name, an employee identifier, an active
  flag and a permission rank.
- **Credential**: the stored secret a person signs in with, and whether it is temporary. A temporary credential
  carries the five-attempt limit and the requirement to choose a new PIN.
- **Permission rank**: the permission level an account holds. Ranks exist only in the store; the lowest rank is
  what a self-created account receives.
- **Set-up request**: the short-lived state in which the plant system has confirmed a person by employee number
  and the application is waiting for a chosen PIN.
- **Launch step**: one piece of work the launch performs, with a name, a category, a stated maximum and a
  best-effort marker. The sequence is data, and the count shown is derived from it.
- **Launch budget**: the launch-wide ceiling of approximately 45 seconds, separate from each step's own maximum.
- **Window stage**: one of the phases the single window passes through: splash, sign-in, machine setup, blocked,
  shell.
- **Plant-system read shape**: one of the named reads the caching service mirrors. Six exist after this feature.

### Success Criteria

#### Measurable Outcomes

- SC-001: A person the store knows signs in with their user name and PIN, and separately with their employee
  number and PIN, and reaches the main screens both times, with the attempt and session behaviour identical to
  today.
- SC-002: A person the plant system knows but the store does not is set up in one pass on their own chosen PIN
  and signs in with it immediately afterwards, without ever seeing a temporary-credential prompt.
- SC-003: An identifier held by neither the store nor the plant system is refused with a plain message, and the
  form is still usable afterwards.
- SC-004: A launch presents exactly one window, with the navigation panel absent until the shell appears and the
  window maximised at the hand-over.
- SC-005: On the reference workstation, a normal launch reaches the shell or a stated stop within the launch-wide
  budget, and every individual step stays within its own stated maximum.
- SC-006: A launch shows the three session checks overlapping one another, and the local reads overlapping the
  first store contact.
- SC-007: The picture cache begins before the first store contact finishes, and the external-system priming is
  the last step before the hand-over.
- SC-008: No request that refreshes the cached plant data is observed during a launch.
- SC-009: The launch surface shows at least two operations at once while each operation still has its own line.
- SC-010: The read-shape catalog and the database ruleset both state six shapes.
- SC-011: A self-created account holds a profile, a credential and a rank, written in a single stored-procedure
  call, and a failure part-way leaves nothing behind.
- SC-012: The build passes the inline-SQL audit and the retired-symbol audit.
- SC-013: The set-up panel is completable from the keyboard alone, with Enter advancing and submitting at the
  end.

### Assumptions

- **Informed defaults.** An identifier is an employee number only when it is exactly four digits; nothing else is
  inferred from its shape. The launch-wide budget is 45 seconds and is reported as an overrun of the budget, not
  as a step failure, when a bounded step has not itself failed. Where a name is confirmed on the set-up panel,
  the confirmed value is what is stored.
- **Accepted risks (recorded, not fixed).** Two-stage sign-in reveals whether an identifier exists; this is
  accepted for an internal application. Hiding the navigation panel means nothing in the shell may assume a
  signed-in person before the hand-over. Re-centring on resize is only smooth if resizes happen at the big
  phases. The sixth read shape is real database and service work, not a one-file change.
- **Scope.** Machine setup, the blocked surface and the shell's own behaviour are unchanged except where this
  feature names them. The picture cache's contents, the external-system priming's behaviour, and the five
  existing read shapes are unchanged.

### Verbatim Constraints

Values the request pinned exactly; downstream steps and the implementation must use these strings and numbers
verbatim.

- Step identifiers: `judge-session`, `check-computer-against-store`, `check-temporary-credential`,
  `read-local-settings`, `read-hardware-identity`, `store-reachability`, `picture-cache`, `visual-priming`,
  `read-remembered-sign-in`.
- The default PIN to bypass: `4330`.
- The lowest rank to assign: `Production`.
- The profile column the employee number is stored in: `employee_identifier`.
- The launch outcome that admits the shell: `MainScreens`.
- The launch-wide budget: 45 seconds. The per-step ceiling: 30 seconds. An employee number: exactly four digits.
  The minimum PIN length: four characters. The read-shape count before and after: five, then six.
