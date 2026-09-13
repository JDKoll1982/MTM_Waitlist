# Startup diagnostics — Living Spec

> [DRAFT] Surface-first draft from existing code — every requirement is observed from the code surface unless tagged otherwise. Review before trusting.

## Purpose

Startup is the hardest part of this application to debug in place: it runs before the shell exists, it runs on a shop-floor workstation that nobody is watching a console on, and its commonest failure — a machine that cannot reach the store — is exactly the failure that makes the application's own database-backed records unavailable. This capability exists so that a failed launch still leaves a readable account of what was attempted and in what order. Without it, the only evidence of a startup fault is what the operator remembers of the splash.

## Requirements

### Recording a diagnostic never blocks or fails the application

Writing a diagnostic SHALL be best-effort. No part of the application SHALL wait on the logging destination, and no part SHALL fail because a destination is slow, missing, or unwritable. A message that cannot be recorded SHALL be dropped rather than raised.

#### Scenario: the destination is unwritable
- **WHEN** the destination cannot be written to
- **THEN** the application continues exactly as if the message had been recorded
- **AND** nothing is shown to the operator

#### Scenario: no destination is configured
- **WHEN** no forwarded destination has been configured
- **THEN** forwarding is skipped in silence
- **AND** the local record is unaffected

#### Scenario: messages arrive faster than they can be written
- **WHEN** the backlog exceeds what the writer can hold
- **THEN** the oldest queued messages give way to the newest
- **AND** the caller is never made to wait for room

#### Scenario: forwarding fails
- **WHEN** the centralized copy cannot be written
- **THEN** the attempt is retried a bounded number of times with a growing delay
- **AND** the machine's own record is written first, so it survives the failure

#### Scenario: the machine's own record cannot be written
- **WHEN** the daily record itself cannot be written
- **THEN** the failure is swallowed and startup is unaffected
- **AND** the forwarded copy is not attempted for that entry either, because the two writes are sequential

### Every entry names its area and its severity

Every entry SHALL carry a severity and an area, so a reader can filter one launch down to the part of the application that spoke. An entry that supplies neither SHALL still be classified rather than arriving unattributable, and an error SHALL retain the underlying failure's detail while a routine entry carries none.

#### Scenario: an entry is written without an area
- **WHEN** a caller supplies no area
- **THEN** the entry is still assigned one
- **AND** it remains attributable in the record

#### Scenario: a failure is recorded
- **WHEN** an error is recorded
- **THEN** the entry carries the failure's detail alongside the message
- **AND** routine entries are not burdened with that detail

### Entries form a tamper-evident sequence

Each entry SHALL be linked to the entry before it, so that a line edited, reordered, or removed after the fact can be detected by a reader walking the file. The link SHALL be derived from the entry's own content combined with its predecessor's link, not from a secret, so any reader can recompute it. It SHALL be a standard cryptographic digest — SHA-256 over the entry's canonical content and the predecessor's link — so verification needs no application code.

#### Scenario: a reader verifies a record
- **WHEN** a reader walks the record from the first entry onwards
- **THEN** every entry's link agrees with its predecessor
- **AND** a gap or an edit breaks that agreement

#### Scenario: the chain starts
- **WHEN** the record's first entry is written
- **THEN** it declares no predecessor
- **AND** it does not have to be special-cased by the reader

### The machine's own record and the forwarded copy are never confused

The record a machine keeps for itself and the copy pushed to a shared destination SHALL be distinguishable by name, so a reader always knows whether they are looking at one machine's full account or the forwarded subset. The distinction SHALL be carried by a role word in the filename rather than by directory, because both settings default to the same kind of destination and can legitimately resolve to one folder.

#### Scenario: both exist for the same day
- **WHEN** a machine has both written its own record and forwarded on the same day
- **THEN** the two files are named differently, by role and by date
- **AND** neither can be mistaken for the other at a glance

#### Scenario: a destination is given as a relative path
- **WHEN** the configured destination is not an absolute path
- **THEN** it is resolved against the machine's local application data
- **AND** the directory is created if it does not exist

### Queued diagnostics survive shutdown

Entries accepted before shutdown SHALL still be written during shutdown. The last moments before the application closes are precisely the ones worth having, so a shutdown SHALL NOT discard work that was already accepted.

#### Scenario: the application closes with entries queued
- **WHEN** the application shuts down while entries are still queued
- **THEN** those entries are written before the writer stops
- **AND** an interrupted drain does not lose what was already accepted

### Retention is bounded by age and by size, and covers only this application's own records

The application SHALL bound its own records both by an age window and by a total size for the directory,
removing the oldest first, so a workstation that is never maintained cannot fill its disk. Cleanup SHALL NOT
be a prerequisite for recording new entries, and SHALL NOT be abandoned part-way through a shutdown, so a
closing application cannot leave the sweep half-done.

Retention prunes only the dated records this application writes for itself. The copy it forwards to a shared
destination is **out of scope**, so a deployment that points both settings at one folder accumulates forwarded
files that nothing here removes — a deployment decision this capability makes visible rather than one it
silently absorbs.

#### Scenario: a machine has accumulated old records
- **WHEN** the application starts
- **THEN** records older than the age window are removed
- **AND** the directory is then reduced to its size bound, oldest first

#### Scenario: cleanup fails
- **WHEN** the directory cannot be pruned
- **THEN** recording continues unaffected
- **AND** nothing is shown to the operator

#### Scenario: both settings resolve to one folder
- **WHEN** the hosted log directory and the shared destination are the same folder
- **THEN** retention removes only the dated records
- **AND** the forwarded copies remain, because managing them is the shared destination's business

### The logging seam tolerates being used before it is configured

The application's diagnostic seam SHALL be safe to call from the earliest part of launch — before the logging destination is known and before the shell exists — so that a launch which dies early can still leave a record, and so that instrumentation placed at the very start of startup is not itself a hazard.

#### Scenario: a message is written before logging is ready
- **WHEN** a diagnostic is written before a destination has been configured
- **THEN** the call returns without throwing
- **AND** startup is not disturbed by the attempt

#### Scenario: the shell never appears
- **WHEN** launch fails before any surface is shown
- **THEN** what was attempted up to that point has been recorded
- **AND** the record is the evidence available after the fact

## Uncovered

`StartupLogService.cs` was read in full for every path this spec asserts — the entry contract, enqueue and shutdown (1–95), and payload, hashing, persistence, forwarding and retention (96–235) — but not its tail (236–259: the directory resolution's remainder and the payload declarations). `StartupLogForwarder.cs` was read in full except its destination-resolution fallbacks beyond the first source.

Not read, so nothing above depends on them: `IStartupLogService`'s registration and configuration in `ModuleDependencyInjectionExtensions.cs` (13 lines), and the concrete values behind the logging options — channel capacity, retention window, size bound, and forward-retry count. Those are asserted here as *behaviour and bounds* ("bounded by age and by size", "retried a bounded number of times"), not as figures. A reader who needs the numbers should read the options rather than this draft.

One consequence a reviewer should weigh: because the two writes for an entry are sequential inside a single
best-effort handler, a failure to write the machine's own record also costs that entry's forwarded copy. The
requirement and scenario above state this deliberately, so it is a documented trade-off rather than a surprise.
