# Phase 0 Research: Scrap Lugger Location

Every decision below closes a choice this plan had to make. The spec settled what the product
does; these settle how the code and the schema deliver it.

## D1. The card's value comes from an arrangement snapshot read once per list load

**Decision**: `ILuggerArrangementService.SnapshotAsync` reads the whole arrangement once at the
start of a list load. `RequestJobAvailabilityProvider` asks it for the requesting job's scrap type
and puts the answer on the availability record. The `Lugger Location` declaration keeps its
`source` of `job`, and `RequestJobFieldValues` gains a `lugger location` arm that returns that
value.

**Rationale**: `ResolveDeclaredCell` is static and synchronous, and it already reads values the
availability record was handed rather than running its own derivation. Reading the arrangement once
per load keeps the card path free of a per-card database call, and the arrangement is seven luggers
and at most a few assignments.

**Alternatives considered**: making cell resolution asynchronous and injecting the service into the
card path, which spreads a database dependency through a pure static helper for no gain. Declaring
the cell with `source` `fixed`, which is wrong: the value is not the request's own field. Resolving
per card, which costs one live read per card instead of one per load.

## D2. The dump-point role lives on the assignment, the status lives on the lugger

**Decision**: `waitlist_lugger_locations.status` holds one of `Available`, `Backup`, `Full`,
`Do Not Use`. `waitlist_lugger_scrap_assignments.is_dump_point` holds the role, one true row per
scrap type. The status shown for a lugger is its own operating state; the role is per scrap type.

**Rationale**: status is a property of the lugger (FR-021) and a lugger may take two scrap types,
so one status cannot encode a per-type role: a lugger can be the dump point for one scrap type and
a backup for another. Separately, FR-016 forbids deriving the role from the order, because an
emptied lugger must come back as a backup rather than reclaim the role it held. The role therefore
has to be stored, and it can only be stored against the pairing.

**Alternatives considered**: deriving the dump point as "the first usable lugger in order", which
passes FR-015 and fails FR-016, because emptying the blocked lugger would hand the role straight
back. Storing a status per (lugger, scrap type) pair, which contradicts FR-012's one status per
lugger and FR-021's "the lugger itself is what fills up".

## D3. Removing a lugger deletes the row

**Decision**: removal deletes the lugger and cascades to its assignments. The arrangement-change row
keeps the lugger's name as text, so the audit outlives the row.

**Rationale**: FR-003 requires a removed lugger to be re-creatable, so a seeded or retired row must
not block the name. FR-004 makes the name unique. MySQL 5.7 has no partial unique index, so a soft
`is_active` flag would make re-creating a retired name fail on the uniqueness rule while the name is
still visible in the table.

**Alternatives considered**: a soft `is_active` column, which is what the spec's Key Entities
sketch implies, but which reintroduces the name collision the moment a lugger is re-created. Keeping
the row and nulling its name, which breaks the audit trail a lead would read.

## D4. One idempotent seed serves a fresh install and an installed store

**Decision**: `seed_waitlist_lugger_arrangement_defaults` inserts the seven luggers and the seven
assignments only when `waitlist_lugger_locations` is empty, inside one transaction. It never
truncates and never overwrites.

**Rationale**: the defaults are "a starting arrangement, not a fixed one", so a second run must
neither duplicate them nor put back one the lead renamed. Guarding on emptiness gives exactly that
and lands on an installed store without a reinstall. The earlier label-move seed exists separately
only because the seed it corrects truncates its table.

**Alternatives considered**: a truncating base seed plus a separate migration seed, which is two
artifacts to keep in step for one behaviour. `ON DUPLICATE KEY UPDATE` keyed by name, which would
re-create every default a lead had renamed away on the next run.

## D5. Scrap types are matched on case and surrounding spaces only

**Decision**: an assignment stores the spelling the lead entered plus a normalised key of
`LOWER(TRIM(scrap_type))`, and the card lookup normalises the job's value the same way. Uniqueness is
`(scrap_type_key, lugger_id)`.

**Rationale**: FR-009 asks for exactly case and surrounding spaces, and it is load-bearing here:
the setup workflow's own default list spells `5052 aluminum`, while the spec pins the assignment as
`5052 Aluminum`. Collapsing inner whitespace as well would let a genuinely different spelling match
and would go further than the requirement.

**Alternatives considered**: matching on the raw value, which would make the shipped default fail to
match the setup list. A full fuzzy match, which would invent assignments the lead never made.

## D6. Each mutating procedure writes its own change row

**Decision**: the transaction that inserts, renames, deletes, sets a status, promotes, assigns,
reorders or unassigns also inserts into `waitlist_lugger_arrangement_changes`, one row per affected
scrap type. No separate audit procedure exists, and the application never writes the audit itself.

**Rationale**: FR-032 and FR-033 cannot then drift apart, and a write that changes the arrangement
but records nothing is impossible to author. The change table is both the audit trail and the source
of the notice, which is what the spec's Key Entities says it is.

**Alternatives considered**: a shared `sp_waitlist_lugger_change_record` that each caller invokes,
which moves the obligation back into application code and lets one call site forget it.
Application-level logging, which is not the audit the spec asks for.

## D7. Notices are polled, with a per-session watermark

**Decision**: `LuggerChangeNoticeService` holds the highest change id seen when the waitlist list
opens, and on each later tick asks the store for changes above it. Nothing at or below the watermark
is ever shown. Dismissal is held in the service for the session.

**Rationale**: FR-037 is a statement about the client's starting point, not about the person. A
watermark taken when the list opens means a change made while nobody was using the application is
never replayed, and it also makes a per-person dismissal table unnecessary: a later session starts
above every older change anyway. The list already ticks once a minute, so the poll costs one small
read per client per minute and no new timer.

**Alternatives considered**: a per-user read cursor persisted in the store, which replays anything
recorded while the person was away, precisely what FR-037 forbids. A dismissal table, which answers
a question the watermark already answers. An operating-system toast, which FR-034 forbids. A push
channel, for which the repository has no broker, and which would also be an MSIX-only dependency.

## D8. Reordering or assigning moves the dump point to the first usable lugger in the new order

**Decision**: when a lead assigns, unassigns, reorders or promotes, the affected scrap type
re-derives its dump point: the explicitly promoted lugger if the lead promoted one, otherwise the
first usable lugger in the new order.

**Rationale**: FR-006 says the order is the order the system tries them in. An order where the
second lugger held the role would make the order and the role disagree, and FR-017 already forbids
that disagreement for the blocked case. FR-018 covers the explicit promotion and says the previous
dump point returns to `Backup`.

**Alternatives considered**: leaving the role on whatever lugger already held it after a reorder,
which makes the editor's own order misleading. Always promoting the first lugger even when the lead
named one, which discards FR-018.

## Documentation grounding

The only platform API this plan pins that the repository did not already use is the InfoBar's
dismissal signal. Microsoft Learn, `InfoBar` (Windows App SDK controls gallery) and
`InfoBarClosingEventArgs.Reason`, confirm that the `Closing` event fires for both a user close and a
programmatic one, and that its `Reason` distinguishes them. The plan uses that event with a
`CloseButton` reason to record a dismissal, and `IsClosable` to expose the close button. The page
also confirms `IsOpen`, `Severity`, `Title`, `Message` and `ActionButton`, which the repository's
existing `ReadStatusIndicator` usage already relies on.

Nothing here needed a Context7 lookup: the libraries this feature touches (CommunityToolkit.Mvvm,
`DispatcherQueueTimer`, MySQL through the repository's own helper) are already in use in the
codebase, and the plan follows those existing call sites rather than introducing a library.

## Open questions

1. **Should the editor offer scrap types that setup accepted but the shipped list does not name?**
   The setup workflow keeps an unrecognised type on the workstation's job state, and nothing reads
   that state outside setup. The editor offers the shipped spellings plus every type already
   assigned, and accepts free text, so a lead can record one. Whether the per-job values should also
   be offered for picking cannot be answered from the specification, and this plan does not add that
   read.
2. **The notice cadence is up to one minute.** The poll rides the list's existing tick. A change
   recorded between the list opening and its first poll is treated as having happened before the
   person was using it and is not shown. That follows FR-037's wording, and it is recorded here
   because it is an observable behaviour rather than an accident.
