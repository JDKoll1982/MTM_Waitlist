# Feature Specification: Unified Waitlist Card and Category/Item Request Picker

**Feature Branch**: `004-unified-card-item-picker`
**Created**: 2026-09-13
**Status**: Draft
**Input**: `WeekendProject/OPEN-WORK-NEXT-SPEC.md` §5 (the workstream), §13 and §15 (the brainstorm and the
deep dive into the configuration spreadsheet and the seeded database), §16 (the approved design).

The two documents of record that deep dive produced: `WeekendProject/Documents/Request-Config-Template.csv` —
the per-Item configuration, the sole source of the field definitions in FR-027 — and
`WeekendProject/Documents/unified-item-picker-workflows.md`, which records each Item's flow and the availability
rule that gates it.

## User Scenarios & Testing

Today a request is described by two words — a **request type** and a **subtype** — and the list draws a different
card for each of them. This feature replaces that with **a Category and an Item**: four Categories, twenty-three
Items, and one card that looks the same whatever the request is. The people who raise requests pick the two
things they actually mean; the people who work the list read one layout instead of fifteen.

### User Story 1 - Raise a request by saying what it is (Priority: P1)

Someone who needs something picks the kind of thing they need — Pickup, Deliver, Assist or Other — and then the
exact thing, from the short list of things their job actually has. When the thing they picked needs one more
answer from them — where a die is going, which component, why a coil is wrong — they are asked for that one
thing, and nothing else.

**Why this priority**: this is how work arrives. Nothing else in the feature matters if a request cannot be
raised in the new vocabulary, and the picker is the half a person touches every time.

**Independent Test**: open New Request, choose a work centre, choose a Category, choose an Item, finish the flow.
Confirm the stored request carries the chosen Category and Item, and that no screen anywhere asked for a type or
a subtype.

**Acceptance Scenarios**:

1. **Given** a job with a coil on it, **When** the person chooses Pickup, **Then** the coil is offered as a
   choice.
2. **Given** a job that has nothing the person could need, **When** they choose a Category, **Then** the Items
   that do not depend on the job are still offered, so they are never stuck with an empty step.
3. **Given** a job with no active setup at all, **When** the person reaches the Item step, **Then** the
   job-independent Items are still offered.
4. **Given** an Item that needs no extra answer, **When** it is chosen, **Then** the details step asks for
   nothing.
5. **Given** an Item that needs one extra answer, **When** it is chosen, **Then** exactly that one thing is
   asked for — a choice from a list, or a short text with stated limits.
6. **Given** a finished flow, **When** the request is stored, **Then** it carries that Category and that Item
   and no request type or subtype.

### User Story 2 - Every request reads the same way (Priority: P1)

Every request on the list is one card with two lines: what kind of thing it is, and which thing it is. The card
is the same shape for every request, so nobody has to learn a different layout per kind of work.

**Why this priority**: it is the visible half of the feature, and the reporting work that follows depends on the
card being uniform.

**Independent Test**: get requests for several different Items onto the list and confirm each draws the same card
shape, with the umbrella word first, the identifier second, and the right picture.

**Acceptance Scenarios**:

1. **Given** requests for different Items, **When** the list is shown, **Then** each is a card whose first line
   is the Item's umbrella phrase and whose second line is the Item's identifier.
2. **Given** a request raised for a die, **When** its card is shown, **Then** the second line is the die's
   location if the destination captured was Home Location, and the die's number otherwise.
3. **Given** a request, **When** its card is shown, **Then** the picture is the Item's picture, and the
   Category's picture only when the Item has none of its own.
4. **Given** a request, **When** its card is shown, **Then** it still shows who asked, the work centre, the time
   remaining and how long it has waited.
5. **Given** an Item whose page shows particular fields, **When** its card is shown, **Then** those fields are
   not on the card.
6. **Given** a request that another handler has claimed, **When** its card is shown, **Then** it offers the same
   actions it offers today for that state.

### User Story 3 - Sort the list the way I need it (Priority: P2)

A viewer chooses what the list is sorted by, from a control beside the ones already there. Most urgent is what it
starts as, and whatever they pick is remembered for them next time.

**Why this priority**: it is a real convenience, but the list is usable without it — and the default keeps it
correct for anyone who never touches it.

**Independent Test**: pick each sort in turn, confirm the order follows; restart the application and confirm the
choice came back.

**Acceptance Scenarios**:

1. **Given** the list, **When** it is first shown, **Then** it is ordered most-urgent-first.
2. **Given** a viewer chooses a different sort, **When** the list is shown again, **Then** rows follow that
   sort.
3. **Given** a viewer chose a sort, **When** they next use the application, **Then** their choice is still
   applied.
4. **Given** work that is overdue, **When** the list is sorted by anything other than urgency, **Then** the
   overdue rows are still visibly marked as overdue, and the most overdue are still findable.

### User Story 4 - An Item is configured, not coded (Priority: P2)

How far the flow goes before confirmation, whether an answer is required, what the prompt says, how long the
answer may be, which options are offered and which fields the page shows — all of that belongs to the **Item**,
and can be changed afterwards without changing the program.

**Why this priority**: it is what makes the twenty-three Items maintainable instead of twenty-three special
cases. It is P2 rather than P1 because the first cut of the values ships with the feature.

**Independent Test**: change an Item's configuration in the database — add a field, change a limit, add an
option — and confirm the flow reflects it with no code change and no rebuild.

**Acceptance Scenarios**:

1. **Given** the stored configuration, **When** an Item is chosen, **Then** the flow follows that Item's
   configuration.
2. **Given** a configuration changed in the store, **When** the flow is used again, **Then** the change is
   reflected, with no rebuild.
3. **Given** an Item with no configuration row, **When** it is chosen, **Then** it is reported as unavailable in
   plain language rather than half-built or crashing.
4. **Given** a configuration row for an Item that does not exist, **When** the flow runs, **Then** nothing
   breaks and the stray row is simply never offered.

### User Story 5 - Set an Item's minutes and picture, with real numbers in front of you (Priority: P3)

The people who set the floor's expectations set each Item's allotted minutes and its picture — and they see what
that Item has actually been taking, so the number they set is informed rather than guessed.

**Why this priority**: the feature works before anyone opens these screens, because every Item ships with a
sensible default. Getting the numbers right is a tuning exercise that follows.

**Independent Test**: open both screens, set an Item's minutes and picture, and confirm a request for that Item
is then measured and pictured accordingly. In the minutes screen, confirm each Item shows both its configured
minutes and the average it has actually taken.

**Acceptance Scenarios**:

1. **Given** the minutes screen, **Then** each Item shows its configured minutes and the average its completed
   requests actually took, as two clearly separate values.
2. **Given** an Item with no minutes set, **When** a request for it is raised, **Then** it is measured using the
   default and is labelled as a default rather than as a configured value.
3. **Given** the picture screen, **When** a picture is chosen for an Item, **Then** requests for that Item show
   it — and a request that already had a real picture never loses it to a placeholder.
4. **Given** a person without the rights to configure the floor, **When** they look for either screen, **Then**
   neither is offered.
5. **Given** an Item nobody has configured, **Then** it does not silently behave like a different Item.

### Edge Cases

- **A job with no parts on it, or no active job at all.** The Item step must still offer the job-independent
  Items rather than coming up empty — and after the scrap correction that set is **five**: both Riser Table
  Items, both Hopper Items, and Other.
- **A job whose scrap type is `No Scrap`.** That is a real answer, not an absence: it means the job has no scrap,
  so the Scrap Item is not offered.
- **A job whose scrap value is still the `Scrap Type Required` placeholder.** No decision was made, so the Scrap
  Item is not offered — the placeholder must never be presented as though it were a scrap type.
- **An Item with no configuration row.** Must be reported as unavailable, never half-configured, never a crash.
- **A configuration row for an Item that does not exist.** Ignored, never offered.
- **An Item with no picture configured.** The Category's picture is used; a request that already resolves a real
  picture must never have it replaced by a placeholder.
- **An Item with no minutes configured.** The default is used and labelled as a default; the request keeps its
  place in the urgency order.
- **Two requests raised at the same moment for Items with different allotted minutes.** They have waited the same
  time but are not equally urgent, and the urgency order must reflect that.
- **The list sorted by something other than urgency while work goes overdue.** Overdue work must stay visibly
  overdue.
- **A request raised before this feature existed**, carrying no Category and no Item. The database is reinstalled
  from seed for this change, so there are none to carry; the specification does not invent a migration path.
- **An Item whose value source does not exist yet.** Not offered at all, rather than offered and unfillable.
- **The store is unreachable.** Reported as the existing per-screen unavailable state with a manual retry —
  never as an empty list and never as a fabricated row.
- **An Item whose page shows fields that the request does not carry.** The field is omitted, never substituted.

## Requirements

### Functional Requirements

- **FR-001**: A person raising a request MUST choose a Category, and then an Item, in that order.
- **FR-002**: The Item step MUST offer the Items the requesting job supports plus the Items that do not depend on
  the job, and MUST never be empty. An Item the job cannot support MUST NOT appear in the list at all — the
  check runs before the person can choose an Item, never after, so an unsupported Item is never offered and then
  refused.
- **FR-003**: No screen may offer a request type or a subtype, and the wizard's type and subtype steps MUST be
  gone.
- **FR-004**: A stored request MUST carry the chosen Category and Item, and MUST NOT carry a request type or a
  subtype.
- **FR-005**: Every request on the list MUST render as one card whose first line is the **Item's umbrella
  phrase** — the Category's own word, or an Item-specific phrase where the Item defines one — and whose second
  line is the **Item's identifier**, which may be a fixed value, a value read from the job, or a value that
  depends on an answer captured during the flow.
- **FR-006**: The card MUST use one layout for every Item; no layout variant may be selected by Item.
- **FR-007**: An Item's own fields MUST appear on the request's page and MUST NOT appear on the card.
- **FR-008**: The card MUST keep the four metadata rows — who asked, the work centre, the time remaining, and how
  long it has waited.
- **FR-009**: A card's picture MUST be the Item's picture, falling back to the Category's picture only when the
  Item has none; a placeholder MUST NOT replace a picture the request already resolves.
- **FR-010**: The list MUST default to most-urgent-first.
- **FR-011**: A viewer MUST be able to change the list's sort from a control on the shell beside the existing
  My Requests and building controls, and their choice MUST be remembered for them.
- **FR-012**: Overdue requests MUST remain visibly marked as overdue whatever the sort.
- **FR-013**: An Item's behaviour — how far the flow goes before confirmation, whether an answer is required,
  the prompt, the length limits, the options offered, and the fields its page shows — MUST come from stored
  configuration rather than from compiled-in code.
- **FR-014**: Every Item MUST have a configuration row, and an Item with no row MUST be reported as unavailable
  rather than half-configured.
- **FR-015**: An Item's configuration MUST be changeable without a code change and without a rebuild.
- **FR-016**: A request's deadline MUST be derived from its Item's allotted minutes, and MUST NOT be derived from
  a request type or subtype.
- **FR-017**: An Item with no configured minutes MUST use a 15-minute default, labelled as a default.
- **FR-018**: The minutes screen MUST show, for each Item, its configured minutes and the average its completed
  requests actually took, as visibly distinct values.
- **FR-019**: The average MUST count only completed requests, measured from acceptance to completion, with a
  request that was released and later completed contributing exactly once.
- **FR-020**: Both configuration screens MUST reuse the role gate that already governs them, and MUST NOT
  introduce a new one.
- **FR-021**: An Item's picture MUST be configurable per Item, and a "nothing configured" answer MUST NOT replace
  an image the request already resolves.
- **FR-022**: Every user-visible string introduced or changed MUST come from the existing resource mechanism.
- **FR-023**: The request type and subtype vocabulary — its catalog tables, its seed, its read procedure and the
  services named for it — MUST be removed, and the schema master lists kept in sync.
- **FR-024**: Every data operation MUST go through a stored procedure; no inline statement text may be
  introduced.
- **FR-025**: Every schema artifact added, changed or removed MUST ship with its matching rollback, and the
  schema master lists MUST be kept in sync.
- **FR-026**: An unavailable Item, a refused action or a failed read MUST be reported in plain, actionable
  language; nothing may fail silently or appear to succeed when it did not.
- **FR-027**: The per-Item field definitions MUST come solely from the request-configuration spreadsheet, and the
  application MUST NOT read, embed or ship that spreadsheet.
- **FR-028**: The four Items whose value source does not exist yet MUST NOT be offered until that work lands.
- **FR-029**: The wrong-material Items MUST each read as a **single Deliver request**, not a compound one, whose
  first line is the Item's own bring phrase and whose second line carries the **correct** material's identifier —
  the material being brought, not the wrong one being collected.
- **FR-030**: The Scrap Item's value MUST be the scrap type already set on the job; the user MUST NOT be asked to
  enter it.
- **FR-031**: The Scrap Item MUST be offered only when the job has a **real scrap decision** — a value that is
  set, is not `No Scrap`, and is not the `Scrap Type Required` placeholder, which the workflow falls back to when
  nothing has been saved and which therefore means no decision was made. A job that chose `No Scrap` has nothing
  to collect.
- **FR-032**: Choosing an Item MUST NOT change the request's lifecycle: the stored statuses, the legal transitions
  and who may perform them stay exactly as they are, whichever Item was chosen.

### Key Entities

- **Category** — one of four kinds of request: Pickup, Deliver, Assist, Other. It carries the umbrella word the
  card's first line shows and the image family a card falls back to.
- **Item** — one of twenty-three specific things a request can be, belonging to exactly one Category, with a
  position in that Category's order, a display name, an identifier for the card's second line — a fixed value, a
  value read from the job, or one that depends on an answer the flow captured — and whether it needs anything
  from the person raising it.
- **Item configuration** — an Item's behaviour: how far the flow goes before confirmation, whether an answer is
  required, the prompt shown, the limits on the answer, the options offered, and the fields the Item's page
  shows. One row per Item.
- **Item setting** — an Item's allotted minutes and its picture. Configurable, and separate from the request
  data.
- **Item observed time** — derived, not stored: the average time an Item's completed requests actually took,
  from acceptance to completion. Read alongside the configured minutes, never written back as if it were the
  configured value.
- **Request** — a unit of work. It carries its Category, its Item, the value that Item produced, the four
  metadata values the card shows, its status and the assignment and timestamp fields it already carries.
- **Sort preference** — the order a particular viewer has chosen for the list, remembered per person.

## Success Criteria

### Measurable Outcomes

- **SC-001**: A person can raise a request by choosing a Category and an Item, in one pass, without ever being
  asked for a type or a subtype.
- **SC-002**: Zero screens, controls or stored fields refer to a request type or a subtype.
- **SC-003**: Every request reachable in the list renders the same card shape — verified across all nineteen
  Items in scope, not a sample.
- **SC-004**: Zero Items are offered that the job does not support, and zero supported Items are withheld —
  proven for each of the eight job configurations, not by inspection.
- **SC-005**: The Item step offers at least one Item in every case, including a job with no parts and a work
  centre with no active job.
- **SC-006**: An Item's fields, options, prompts and limits can all be changed without a code change and without
  a rebuild, demonstrated by making one such change.
- **SC-007**: A request whose Item has no configured minutes still takes its place in the urgency order and is
  marked as using the default.
- **SC-008**: The minutes screen shows an observed average for every Item that has completed requests, and the
  value differs per Item where the underlying times differ.
- **SC-009**: A viewer's sort choice is still applied after the application is restarted.
- **SC-010**: Zero user-visible strings introduced or changed by this feature are unlocalized.
- **SC-011**: The full test suite reports no failures, and the solution builds with no warnings and no errors.
- **SC-012**: Zero references to the retired type and subtype vocabulary remain in application code, database
  artifacts, or project documentation.
## Assumptions

- **The database is reinstalled from seed; no migration is written.** The sample data is recreated in the new
  shape and the local database is reinstalled before the first screen test. There is therefore no historical
  data to carry, and no back-fill, derivation or migration procedure is specified. The reinstall is the owner's
  action, never the agent's.
- **The list of Items lives in code; the behaviour of each Item lives in the store.** The twenty-three Items,
  their Categories, order and display text are asserted by tests as they are today. Their configuration is data
  so that it can change without a build.
- **The spreadsheet is a design document, not a shipped input.** Its field definitions are used once, when the
  configuration is authored. It is not read, embedded, parsed or shipped by the application, and no build step
  depends on it.
- **The workflow document is a design document, not a shipped input.** `unified-item-picker-workflows.md` records
  each Item's flow and the availability rule that gates it. It is read when the picker is built; the application
  does not read, embed or ship it either.
- **Because Item fields can change, no test may assert a fixed field list.** Tests assert that fields are read
  from configuration with the right order, types and labels — never what today's fields are.
- **The observed average covers all of an Item's completed requests.** No time window is applied.
- **The card's four metadata rows stay.** They are the same for every Item, so they do not break uniformity, and
  the time remaining is the number the default order is based on.
- **The old per-type card design is retired deliberately, and its freeze is lifted in writing.** The previous
  specification's "do not regress" constraint and its matching test are superseded — amended as a recorded
  supersession, and rewritten to the new shape rather than deleted, so an unintended change still fails.
- **The real material identifiers stay deferred.** The coil or part number, quantity and stock location remain
  the property of the later item-resolution work, so the card continues to show only what a request truthfully
  carries.
- **The four Items with no value source are out of scope** and are hidden until their own work lands.
- **Releasing a request keeps no screen control.** It remains available to the service, as it is today.
- **Screen verification uses the scripted UI tests**, as the previous specification did. The requirement to
  verify with the in-app inspector is superseded, because that tool is not wired in this repository — and the
  replacement is stronger evidence, since it drives the same artifact a user runs.
- **The two configuration screens keep working for whoever uses them today.** Only their subject changes, from
  subtype to Item.

## Verbatim Constraints

These strings are stored values or identifiers that the result must match exactly. None may be renamed, recased
or pluralized.

**The two new stored columns**: `category`, `item` — replacing `request_type` and `subtype`.

**The four Categories**: `Pickup`, `Deliver`, `Assist`, `Other`.

**The twenty-three Item codes**, exactly as the canonical catalog spells them:

- Pickup — `pickup-coil`, `pickup-die`, `pickup-component`, `pickup-fg`, `pickup-ncm`, `pickup-wip`,
  `pickup-outside-service`, `pickup-riser-table`, `pickup-dunnage`, `pickup-scrap`, `pickup-hopper`
- Deliver — `deliver-coil`, `deliver-riser-table`, `deliver-hopper`, `deliver-flatstock`, `deliver-die`,
  `deliver-dunnage`, `deliver-wrong-coil`, `deliver-wrong-flatstock`
- Assist — `assist-coil-turn`, `assist-table-place`, `assist-table-remove`
- Other — `other`

**The four Items out of scope**: `pickup-fg`, `pickup-ncm`, `pickup-wip`, `pickup-outside-service`.

**The stored status vocabulary is unchanged** and MUST NOT be extended: `Pending`, `Accepted`, `Completed`,
`Canceled`. The friendly labels the card shows — `Waiting`, `In Progress`, `Done`, `Cancelled` — remain display
text only.

**The default allotment is 15 minutes.**

**The wrong-material first lines**: `Wrong Coil Bring:` and `Wrong Flatstock Bring:` — the Item's own first line,
not the plain Category word.

**The Die destination options**: `Die Shop`, `Home Location`, `Other` — and `Home Location` is the value that
switches the card's second line from the die's number to the die's location.

**The scrap values that suppress the Scrap Item**: `No Scrap` — a real selectable scrap type meaning the job has no
scrap — and `Scrap Type Required` — the placeholder the workflow falls back to when no decision has been saved.
Neither may be offered as a Scrap request.

**The five sort options**, most urgent first: most urgent (the default), longest waiting, press, requested by,
status.
