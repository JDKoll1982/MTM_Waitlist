# Feature Specification: Startup Rebuild

## User Scenarios & Testing

### User Story 1 - A launch that can be watched and diagnosed (Priority: P1)

Someone opens the application. The launch window shows, line by line and in plain language, everything the
application is doing while it starts: reading its configuration, contacting the store, checking this computer,
working out who is signed in, and preparing the first screen. If the launch stops, the window says which line
stopped it and why.

**Why this priority**: today a stopped launch is indistinguishable from a slow one. Support is told "it hangs",
and nobody can say where. Making the launch readable turns every other fault in this feature from a report into
a diagnosis, and it delivers that on its own, before any other part lands.

**Independent Test**: launch with the store reachable and confirm each line appears before the work it
describes. Then launch with the store unreachable and confirm the window states the cause instead of sitting on
the last line.

**Acceptance Scenarios**:

1. **Given** the application is starting, **When** it begins a piece of work, **Then** the window names that
   work before it begins, so a stall is attributable to a named line.
2. **Given** the launch stops, **When** the reason is known, **Then** the window states the cause in plain
   language rather than leaving a step number on screen.
3. **Given** a piece of the launch is slow or unreachable, **When** its allowed time passes, **Then** the launch
   reports that and either carries on or stops with a cause. It never waits without end.
4. **Given** the launch has an error to report, **When** the message appears, **Then** it appears in a strip at
   the bottom of the window and does not cover the list of work.
5. **Given** the launch is under way, **When** it performs an individual operation, **Then** that operation is
   shown as its own line on the window, so every individual thing the launch processes is visible rather than a
   single line standing for a group of them.

### User Story 2 - A new machine is configured deliberately, and cannot be skipped (Priority: P1)

Someone takes delivery of a computer that has never run the application. Before anyone can use it, a person
holding `IT Department` or `Developer` authority signs in, names the machine, points it at the shared picture
sources, and saves. Only then does the machine become usable. If
that person abandons setup, the application closes and says why.

**Why this priority**: a machine that runs without its configuration is a machine nobody can support. This is
also the step that replaces a settings file that used to live on the machine, so without it the application has
nowhere to keep the facts about the machine it is running on.

**Independent Test**: run the application on a machine with no configuration and confirm it stops at setup.
Then confirm that no combination of closing, cancelling or dismissing reaches the main screens.

**Acceptance Scenarios**:

1. **Given** a machine with no configuration, **When** the application starts, **Then** it stops at machine
   setup before any operator signs in.
2. **Given** setup is open, **When** a person who does not hold `IT Department` or `Developer` authority tries
   to continue, **Then** setup is refused.
3. **Given** setup is under way, **When** the person abandons it by any route, **Then** the application ends and
   states why it ended.
4. **Given** a machine is configured, **When** its configuration is later removed, revoked or cannot be read,
   **Then** setup returns at the next check.
5. **Given** a machine with no configuration, **When** any input sequence is tried, **Then** none of them
   reaches the main screens.

### User Story 3 - Signing in identifies both the person and the machine (Priority: P2)

A person signs in and the application decides three things: whether the store recognises them, whether their
session is still good, and whether this machine is one it knows. A session is judged by the store's clock. An
account still on a temporary credential is asked for that credential, allowed five tries, and then required to
set a new password before it can carry on. A person may choose to be remembered on that machine, and if the
application cannot read what it needs to honour that, the ordinary sign-in form appears instead.

**Why this priority**: every action on the shop floor is attributed to a person, and an attribution made on a
half-checked identity is worse than no attribution at all.

**Independent Test**: sign in with a temporary credential, fail it five times and confirm the sixth attempt is
refused even with the correct value, then restart and confirm it is still refused.

**Acceptance Scenarios**:

1. **Given** the store's clock and the machine's clock disagree, **When** a session is judged, **Then** the
   store's clock decides.
2. **Given** an account holds a temporary credential, **When** five attempts have failed, **Then** a sixth
   attempt is refused even with the correct value, and it stays refused across a restart.
3. **Given** a temporary credential has been accepted, **When** the person carries on, **Then** they are asked
   to set a new password before anything else.
4. **Given** what is needed to honour a remembered sign-in cannot be read, **When** the person arrives at
   sign-in, **Then** the ordinary form is offered and nothing is kept on the machine in its place.
5. **Given** a machine's hardware identity cannot be read, **When** the machine check runs, **Then** the person
   is admitted, because a fact that could not be read is not a failed check.
6. **Given** a person signs out, **When** the application restarts, **Then** nothing about their session
   survives and they are asked to sign in again.

### User Story 4 - A stop offers only the remedies that can help (Priority: P2)

When the launch stops, it names the cause and offers the actions that could actually resolve it. Trying again
repeats only the piece that failed. Restoring defaults says exactly what it is about to reset and then resets
only this machine's configuration, never people, roles or permissions. Where a fault can be put right without
the person's involvement, it is put right quietly.

**Why this priority**: yesterday's experience was an operator staring at a step number with no way forward, and
a reset button that would have thrown away settings it could not have repaired.

**Independent Test**: stop a launch on a store fault and confirm no reset is offered. Stop one on a broken
machine setting and confirm the reset names that setting before it acts.

**Acceptance Scenarios**:

1. **Given** the launch stopped because the store could not be reached, **When** the person looks at the
   actions, **Then** resetting is not among them.
2. **Given** resetting is offered, **When** the person chooses it, **Then** they are told exactly what will be
   reset before anything is reset, and only this machine's configuration is affected.
3. **Given** a fault can be repaired without the person's involvement, **When** the launch meets it, **Then**
   it is repaired quietly and the launch carries on.
4. **Given** the launch stopped at a named piece of work, **When** the person tries again, **Then** only that
   piece and what follows it are repeated.

### User Story 5 - Support can see what happened on any machine (Priority: P2)

Everything the application records while it runs goes to the store, each entry carrying its severity, the area
of the application it came from, the machine, the person, the kind of error and the message. A developer opens a
panel and filters by any of those.

**Why this priority**: in a released build the application currently records nothing at all, so a fault reported
from the floor leaves no trace for anyone to read.

**Independent Test**: run a released build, then confirm the launch appears in the store and can be found in the
panel by machine and by severity.

**Acceptance Scenarios**:

1. **Given** a released build, **When** the application completes a launch, **Then** an entry is recorded in
   the store, where today none is.
2. **Given** entries exist, **When** a developer filters by machine and by error kind together, **Then** only
   matching entries are listed.
3. **Given** the store cannot be reached, **When** the application refuses to start, **Then** the window states
   the cause and nothing is kept on the machine as a substitute record.
4. **Given** a developer has found a fault in the panel, **When** they copy it, **Then** they hold the whole
   entry as text they can paste to someone who has no access to the store.

### User Story 6 - Preferences follow the person, not the computer (Priority: P3)

A person's choices travel with them: the theme, the order of the waitlist, whether new-request alerts are on,
whether parts without pictures are listed, and which requests they have already seen. The list of locations to
hide belongs to the whole plant and only `IT Department` and `Developer` may change it. The only thing left on
a computer is what is needed to reach the store.

**Why this priority**: swapping or re-imaging a computer currently loses a person's preferences, and a shared
computer carries the last person's choices.

**Independent Test**: set preferences on one computer, sign in on another, and confirm they are already there.

**Acceptance Scenarios**:

1. **Given** a person sets preferences on one computer, **When** they sign in on another, **Then** their
   preferences are already there.
2. **Given** someone without `IT Department` or `Developer` authority opens the locations-to-hide list, **When**
   they look at it, **Then** they can read it and cannot change it.
3. **Given** a computer holds nothing but what it needs to reach the store, **When** the application starts,
   **Then** it starts normally and finds everything else in the store.

## Edge Cases

- The store cannot be reached at launch: the launch stops, states the cause, keeps no record on the machine, and
  offers to close.
- The share holding pictures cannot be reached: the launch records it, leaves existing copies in place, and
  carries on.
- A machine's configuration is deleted while the application is running: setup returns at the next check.
- The machine's display name is already in use: the save is refused and a different name is asked for.
- What is needed to honour a remembered sign-in cannot be read: sign-in works normally and remember-me quietly
  does not.
- A reset of a temporary credential happens while the person is signing in with the old one.
- The launch window is closed while a launch is running.
- The machine has no readable hardware address.
- A sign-out whose restart fails: the session is still ended, and the person is told to open the application
  again.
- Two copies of the application are launched on one computer.
- The store answers in seconds rather than milliseconds.
- The copy of pictures takes longer than the launch allows: the launch carries on, and the screen reads pictures
  from their source until the copy catches up.
- A person signs out at a shared computer, leaving nothing of theirs behind.
- Someone without `IT Department` or `Developer` authority tries to change how long a session lasts: the change
  is refused and the person is told they may not make it.
- The launch window's saved size cannot be applied: the window still opens and the launch carries on, because a
  sizing failure is not a launch failure.
- An account still holding the legacy temporary value signs in: that value is recognised, as it is today.
- A credential reset is issued while the person is signed in elsewhere: the session already running is not
  ended by the reset.
- A fault carries so much detail that the entry would exceed the store's ceiling: the detail is truncated with
  an explicit marker, and the entry is still written.
- A fault happens while the application is already recording one: the recorder does not raise a second
  diagnostic and does not re-enter itself.
- An aggregate fault holds several independent failures: every one of them is captured, not only the first.
- A fault's own message contains a credential, a token or a statement's parameter value: the value is removed
  before the entry is written.
- The store refuses the write: the entry is dropped, nothing is kept on the machine in its place, and the
  original fault is reported to the person exactly as it would have been.
- The clipboard refuses a copy, because another process is holding it: the panel says the copy did not happen,
  and nothing else about the list changes.

## Requirements

### Functional Requirements

- **FR-001**: The launch MUST end at exactly one of: the main screens, sign-in, machine setup, a stated stop, or
  an ended process. Those five are `LaunchOutcome` in `contracts/launch-step-contract.md` §5, which is the
  single statement of the terminal outcomes.
- **FR-002**: The launch MUST name each piece of work before it performs it.
- **FR-003**: Every wait on the launch path MUST have a stated maximum, and no wait may be unbounded.
- **FR-004**: A stopped launch MUST state a cause specific to what stopped it.
- **FR-005**: An error MUST be shown in a strip at the bottom of the launch window, without covering the list of
  work.
- **FR-006**: A machine with no configuration MUST NOT reach the main screens by any route.
- **FR-007**: Machine setup MUST be authorised by a person holding `IT Department` or `Developer` authority, and
  that authorisation MUST NOT open the main screens.
- **FR-008**: Every route out of machine setup that is not completion MUST end the application, and the ending
  MUST be preceded by a statement of why.
- **FR-009**: A machine whose configuration is removed, revoked or unreadable MUST be treated as unconfigured at
  the next check.
- **FR-010**: Session validity MUST be judged by the store's clock, not the computer's.
- **FR-011**: A session MUST be cleared when the person signs out, before the application restarts.
- **FR-012**: An account holding a temporary credential MUST be refused after five failed attempts, including
  attempts presenting the correct value, and the refusal MUST survive a restart.
- **FR-013**: The set-a-new-password step MUST open only after the temporary credential has been accepted.
- **FR-014**: A remembered sign-in MUST be held against the person in the store and MUST be encrypted, and a
  failure to read what protects it MUST fall back to the ordinary sign-in form.
- **FR-015**: A computer whose hardware identity cannot be read MUST be admitted.
- **FR-016**: A stopped launch MUST offer to repeat the failed work.
- **FR-017**: Resetting MUST be offered only where resetting could remove the cause.
- **FR-018**: Resetting MUST state what it will reset before it acts, and MUST affect only this machine's
  configuration. It MUST NOT change people, roles or permissions.
- **FR-019**: A fault that can be repaired without the person's involvement MUST be repaired without asking.
- **FR-020**: Repeating failed work MUST repeat only that work and what follows it.
- **FR-021**: Every diagnostic the application records MUST be written to the store, carrying at least the time
  it happened, its severity, the area it came from, the machine, the person, the kind of error, the message and
  its place in the tamper-evident chain.
- **FR-022**: The person's identity and the machine's facts MUST be available to the rest of the application for
  reading only, and only the launch may change them.
- **FR-023**: A person's preferences MUST be held against the person in the store.
- **FR-024**: The list of locations to hide MUST belong to the whole plant and MUST be changeable only by
  `IT Department` or `Developer`.
- **FR-025**: Nothing except what is needed to reach the store or the external read-only system MAY be kept on
  a computer, apart from the local copy of pictures and the one reviewed exception, which holds neither a secret
  nor anything belonging to a person or a machine.
- **FR-026**: The copy of pictures MUST be best effort and MUST NOT be able to stop the launch.
- **FR-027**: Whether the external system can be reached MUST be settled before the main screens open.
- **FR-028**: The launch behaviour this feature replaces MUST NOT be reintroduced, and a build MUST fail if it
  is.
- **FR-029**: How long a session lasts MUST be changeable from the settings panel, and only by `IT Department`
  or `Developer`.
- **FR-030**: Every setting that is restricted to particular roles today MUST keep the same restriction.
- **FR-031**: The cases found by the three read-only sweeps MUST be recorded in the repository as the
  acceptance surface, and every case MUST be either covered by a test or recorded as no longer applying, with
  its reason. A case MAY NOT be dropped silently.
- **FR-032**: A diagnostic MUST record the fault it came from in full: the exception's type, its message, its
  stack trace and every exception in its chain, including each of an aggregate's independent failures, rather
  than the outermost message alone.
- **FR-033**: A diagnostic MUST carry a stable fingerprint derived from the fault itself, so repeated
  occurrences of one fault can be grouped and counted.
- **FR-034**: A diagnostic MUST carry the application's version and the context of the machine and process it
  happened on — the operating system and its architecture, the process architecture, the runtime, the Windows
  App SDK version, whether the build is packaged, the process and thread identity, and how long the application
  had been running — gathered by the recording mechanism rather than supplied by the place that raised it.
- **FR-035**: A diagnostic MUST carry the action under way, and the window, screen and control involved where
  the caller can name them.
- **FR-036**: A diagnostic raised by store work MUST carry the provider's own diagnostics where the provider
  supplies them — the provider's error code, the SQL state, the server's error number, the procedure, a
  fingerprint of the statement that contains no values, how long it took and which retry it was on — and MUST
  NOT carry credentials, tokens, connection strings or statement parameter values.
- **FR-037**: A fault MUST NOT be hidden by the act of recording it. A failure to record a diagnostic MUST NOT
  raise another diagnostic, MUST NOT be reported in place of the original fault, and MUST NOT delay the caller.
- **FR-038**: The developer log panel MUST be able to copy what it is showing — one entry, or the entries
  currently listed, which the panel has already bounded — as plain text that can be pasted elsewhere, carrying
  for each entry everything the store holds for it: the fault in full with its chain, the recorded context, the
  store diagnostics and the entry's place in the chain. Copying MUST write nothing to the machine.
- **FR-039**: The launch MUST show every individual operation it processes on the launch window — each operation
  holding its own line rather than standing behind a group's single line — so what the person watches matches
  what the launch does.

## Key Entities

- **Person**: who is signed in, with their name, their employee number and the roles they hold. Read from the
  store and never editable from the application during a launch.
- **Machine**: the computer the application is running on, identified by its name and a stable hardware address,
  with a display name and description that people recognise.
- **Machine configuration**: what a machine needs before it may be used: its own identity, and where its
  pictures come from. Records are not part of it, because records live only in the store.
- **Session**: a person's signed-in period on one machine, with an end time, judged against the store's clock.
- **Remembered sign-in**: a person's choice to be recognised on one machine, held against the person and kept
  unreadable to anyone without the means to decrypt it.
- **Scoped preference**: a choice that belongs to a person, a machine, a role or the whole plant.
- **Diagnostic entry**: one thing the application recorded, with the time it happened, its severity, origin,
  machine, person, action, the fault in full — its type, message, stack and complete exception chain — a
  fingerprint for grouping, the application's version and the machine's runtime context, the caller's view
  context, the store's own diagnostics where the fault came from store work, and its place in the
  tamper-evident chain.
- **Launch step**: one named piece of work in the launch, with a name, a description and a category.

## Success Criteria

### Measurable Outcomes

- **SC-001**: A launch that stops never does so silently. Every stop states a cause, verified for each stopping
  condition.
- **SC-002**: No launch wait exceeds its stated maximum, and no wait on the launch path is unbounded. A stalled
  store or share cannot hold the launch window beyond 30 seconds without either progress or a stated cause.
- **SC-003**: A released build records at least one entry per completed launch in the store, where it currently
  records none.
- **SC-004**: From a machine with no configuration, zero tested input sequences reach the main screens.
- **SC-005**: A developer can find one machine's fault history in the panel, filtered by machine and by error
  kind, and by severity and machine, in under a minute without asking anyone.
- **SC-006**: Every tested route out of machine setup ends the application: all routes, not most.
- **SC-007**: The five-attempt limit holds across a restart, verified with the correct value on the sixth
  attempt.
- **SC-008**: No preference is lost when a person moves to a different computer: theme, waitlist order, alerts,
  seen-requests and the parts-without-pictures choice all follow them.
- **SC-009**: A build fails when any replaced launch behaviour is reintroduced, verified by reintroducing one on
  purpose.
- **SC-010**: A stopped launch offers no action that could not remove the cause, verified for each stopping
  condition.
- **SC-011**: A change to how long a session lasts, made by an authorised role, takes effect at the next
  sign-in, verified end to end.
- **SC-012**: Every setting that is role-restricted today is still restricted to the same roles, verified one
  setting at a time.
- **SC-013**: Every case in the acceptance surface is either covered by a test or recorded as retired with its
  reason. Verified by working the whole catalogue end to end and accounting for each case.
- **SC-014**: Two occurrences of the same fault produce the same fingerprint and are found as one group,
  verified by raising the same fault twice and comparing what the store holds.
- **SC-015**: A diagnostic written from a store failure carries the provider's error code, and its recorded
  statement fingerprint contains no parameter values, verified against a deliberately failing procedure.
- **SC-016**: A fault raised while the store is unreachable is recorded nowhere on the machine and still
  reaches the caller unchanged, verified for a store outage and for a failing recording path.
- **SC-017**: A developer can copy one entry from the panel and paste it whole, and the pasted text names the
  exception type, every exception in the chain, the machine, the action and the entry's chain link. Verified by
  raising a knowing fault, copying its entry and reading the pasted text.

## Assumptions

- A session lasts eight hours by default, and that default is changeable from the settings panel by `IT
  Department` or `Developer` only.
- The store's clock is authoritative wherever a time matters.
- Thirty seconds is the ceiling for any single wait on the launch path, and the copy of pictures is bounded more
  tightly than that because it is optional.
- Roles come from the store, and the local list that used to grant a developer everything on a machine is
  retired. A person needing developer access needs it in the store. The development seed grants `Developer` to
  `JKoll` and `JohnK`, so each of the two existing seeded machines has a developer who can sign in.
- Records live only in the store, so machine configuration carries no destination path for them. What a machine
  is configured with is its own identity and where its pictures come from.
- The means to decrypt a remembered sign-in already exists as a shared secret file. The application reads it to
  obtain the key. It is not created, changed or rotated by this work.
- Preferences belong to the person, except the list of locations to hide, which belongs to the plant. Every
  setting that is restricted to particular roles today keeps that same restriction.
- The launch window is the only surface shown while a launch runs, except when machine setup is triggered: that
  is a surface of its own and it appears before any operator signs in.
- Severity is the five-level vocabulary the recording mechanism defines, and it is the one the store's column
  and the panel both use.
- Structured detail is held as JSON inside the store's existing text columns, matching how the store already
  holds it, rather than in a separate table per shape.
- The queue that carries diagnostics to the store is held in memory. An unrecorded diagnostic is therefore
  lost rather than kept on the machine, and that loss is the deliberate price of keeping nothing local.
- Grouping is in scope and triage is not: this feature groups repeated faults by their fingerprint, and does
  not add assignment, resolution or fixed-version tracking, which need their own specification.
- The store's provider is MySqlConnector.
- The panel's copy goes to the clipboard and writes no file, because a saved diagnostic is application behaviour
  kept on the machine and FR-025 removes it. A file export would need its own decision rather than arriving as a
  convenience.
- The copy is excluded from the clipboard's history and from syncing to other devices. A diagnosis names a
  machine and a person, and both the platform's guidance and the defaults work against leaving that in a
  clipboard history.

## Verbatim Constraints

These were pinned by the request and must match exactly:

- The shared key file: `\\mtmanu-fs01\Expo Drive\Software Development\Live Applications\MTM_Application_Keys\MTM_AUTH_USER_SECRET_KEY.txt`
- The session table: `user_active_sessions`
- The records table reused for diagnostics: `ops_startup_logs`
- The two roles that may configure a machine or change the plant-wide list: `IT Department` and `Developer`
- The one reviewed deployment setting that may still remain on a machine: `MockServiceClient`
