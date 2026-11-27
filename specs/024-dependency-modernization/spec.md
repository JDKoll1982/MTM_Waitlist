# Feature Specification: Dependency Currentness, Credential Removal and Platform Modernization

**Source**: Dependency_Assessment/2026-11-26-full-deep/DEPENDENCY_AND_MODERNIZATION_SPECIFICATION.md

**Feature Directory**: `specs/024-dependency-modernization`
**Created**: 2026-11-27
**Status**: Draft

## Overview

The plant application has drifted away from the platform it runs on. Several of its components are a
full major release behind, two sit on a line the vendor has stopped shipping, one set of live database
credentials is stored in the repository in plain text, a handful of places re-implement what the
framework or a package already provides, three blocking waits freeze the interface, some screens show
resource key names where words belong, and the files that brief an AI agent state a platform version
that is no longer in use.

This feature is the approved work of the 2026-11-26 dependency and modernization audit: **32 proposals,
all approved, zero rejected**, grouped into **8 ordered phases**. Every proposal ID is preserved here as
a requirement label so a later phase of work can trace a change back to the decision that authorised it.

Nothing the plant depends on changes behaviour. The internal stores are still read and written live,
every data operation still goes through a stored procedure, the interface still looks and works the way
it does today, and no new framework, database engine or test runner is introduced. The value is that
the application runs on a supported, current stack; that no secret lives in version control; that the
interface stays responsive; and that the team can maintain and verify the code it has.

## User Scenarios & Testing

### User Story 1 - No secret is stored in the repository (Priority: P1)

Someone with read access to the source repository, or anyone who finds a clone or a backup of it, opens
the application configuration. They find the shape of the configuration and the name of the environment
variable that supplies each value, and no credential of any kind. A host that has not been given its
values refuses to start and says which variable is missing, instead of limping on with an empty or
guessed connection.

**Why this priority**: A committed password is already disclosed. It is the one finding in the audit with
a live blast radius, and every day it stays costs another rotation.

**Independent Test**: Search every tracked file for the current credential values; the search returns
nothing. Start the application on a machine with the variables unset; it stops with a message naming the
variable. Set them; it starts and connects.

**Acceptance Scenarios**:

1. **Given** the repository as committed, **When** every tracked file is searched for a database password,
   server name, user name or connection string value, **Then** no credential value is found.
2. **Given** the configuration file, **When** it is read, **Then** it retains every connection setting,
   store name and server reference it needs, with only the secret values replaced by an empty value.
3. **Given** a host with no connection-string environment variable set, **When** the application starts,
   **Then** it fails with a message that names the missing variable, and it does not fall back to an empty
   connection string or a placeholder.
4. **Given** a host with the variables set, **When** the application starts, **Then** it connects exactly
   as it does today and no feature is disabled.
5. **Given** the values that were committed, **When** the change is delivered, **Then** an operational
   record exists that those values are treated as disclosed and are rotated on the plant host.

### User Story 2 - The application runs on a supported, current stack and behaves exactly as before (Priority: P1)

A maintainer takes the repository, clears the local package cache, restores and builds, and gets the same
application running on current, supported components. One version of each package is declared once for
the whole solution, so no two projects can quietly drift apart, and the restore pulls only from a feed the
repository itself names. Every screen, every stored-procedure path and every plant read behaves the way it
did before the change.

**Why this priority**: A platform that is behind stops receiving fixes, and version drift between projects
turns every future upgrade into an investigation. This is also the change that makes the rest cheap: once
a version lives in one place, everything after it is a one-line edit.

**Independent Test**: Restore from a cleared cache, build the solution, run the test suite, and walk the
screens (sign-in, waitlist list and detail, setup, generic request, settings, deployment tool). The build
reports no errors, the suite is green with the same number of tests, and every screen behaves as before.

**Acceptance Scenarios**:

1. **Given** the solution, **When** each package is counted across all projects, **Then** it appears at
   exactly one version, and any per-project exception is declared as an exception rather than a silent
   override.
2. **Given** a cleared package cache, **When** a restore runs, **Then** it succeeds using only the feed the
   repository configuration names, and raises no new warning.
3. **Given** a clone that has never been built, **When** it restores, **Then** the feed set is the same as
   on every other clone, and a package whose source is not mapped to a feed is refused.
4. **Given** each approved dependency upgrade, **When** it lands, **Then** the solution builds with no
   errors and the full test suite is green with the discovered test count unchanged.
5. **Given** the upgraded interface stack, **When** each screen is opened at each display scale the plant
   uses, **Then** it renders and behaves as it did before.
6. **Given** the upgraded database drivers, **When** an internal-store read and write and an Infor Visual
   read are exercised, **Then** both succeed against the local instance before the plant host is touched.
7. **Given** the test tooling upgrade, **When** the suite runs, **Then** every test that ran before runs
   now, with the same count and the same result.

### User Story 3 - The interface never freezes while it works (Priority: P1)

A person types a part number into a request. The search happens once they pause, not once per keystroke,
so the plant system is asked a handful of times instead of a dozen, and a slow earlier answer cannot
overwrite the correct one. When they open settings or close the application, the window keeps painting
instead of locking up while a read or a flush completes.

**Why this priority**: A frozen window on a plant floor reads as a broken system, and a search per
keystroke multiplies load on the plant system for every user.

**Independent Test**: Script a ten-character part number, count the plant round trips and confirm a
single-digit count with the final result correct. Reproduce a slow configuration read and confirm the
window keeps responding; close the application and confirm shutdown does not block the interface thread.

**Acceptance Scenarios**:

1. **Given** a person typing a part number, **When** they type ten characters with several pauses, **Then**
   the plant system is asked no more than once per pause.
2. **Given** two searches in flight where the older one answers last, **When** both complete, **Then** the
   result shown belongs to the newer search and the older answer is discarded.
3. **Given** a search the user has abandoned by continuing to type, **When** it would have returned,
   **Then** it is cancelled and does not reach the interface.
4. **Given** a configuration read that takes several seconds, **When** the settings screen loads, **Then**
   the window keeps painting and accepts input.
5. **Given** the application closing, **When** pending work is flushed, **Then** the flush is bounded and
   the window does not stop responding while it completes.

### User Story 4 - Every data path goes through one enforced seam (Priority: P2)

A maintainer changes retry policy, a timeout, or how a failed store is reported. One place is edited and
every data call in the application follows, including the five services that used to open their own
connections. The rule that every operation is a stored procedure is now true rather than nearly true,
and the Infor Visual read path has one owner instead of two copies that can drift apart.

**Why this priority**: Duplicated infrastructure means a policy change silently misses sites, and an
inline-statement escape hatch means the rule the team states is not the rule the code follows.

**Independent Test**: Point a service at an unreachable store and confirm the shared policy applies.
Search the codebase for direct connection construction; only the seam remains. Confirm the seam refuses
anything that is not a stored procedure. Break one Visual read path and confirm both callers break, which
proves there is only one.

**Acceptance Scenarios**:

1. **Given** the services that previously opened their own connections, **When** they are read, **Then**
   each routes through the existing seam and none constructs a connection itself.
2. **Given** the seam, **When** a caller supplies statement text instead of a stored-procedure name,
   **Then** the call is refused.
3. **Given** every caller that used the removed statement path, **When** the path is removed, **Then** each
   caller was identified and resolved first, and none fails at run time.
4. **Given** a read that returns more rows than the interface can use, **When** it runs, **Then** the read
   is bounded rather than unbounded.
5. **Given** the Infor Visual query path, **When** any caller in the application or the setup tool needs
   it, **Then** both use the same implementation.

### User Story 5 - The codebase holds one idiom and one direction of dependency (Priority: P2)

A maintainer opening the shared library finds helpers and contracts, not screens. A data library does not
point at a wizard screen. JSON is serialized through generated metadata rather than reflection at run
time. Animations use one library. A package line the vendor has stopped shipping is gone, along with a
reference nothing uses, and the accommodation the build carried for the old graphics runtime is gone with
a clean restore to prove it.

**Why this priority**: Dependency direction is what makes a codebase changeable. Each of these items is
small alone; together they decide whether the next feature costs a day or a week.

**Independent Test**: List the types in the shared library and confirm no view type is there. List each
project's references and confirm the generic-request libraries name no waitlist UI project. Confirm no
package from the retired line is declared anywhere. Confirm generated serialization metadata exists per
store. Remove the build accommodation and restore cold.

**Acceptance Scenarios**:

1. **Given** the shared library, **When** its types are listed, **Then** it contains no control, page or
   window type, and every caller of the dialog that moved still shows it.
2. **Given** the generic-request logic library and its data library, **When** their project references are
   listed, **Then** neither names a waitlist UI project, and the generic-request flow still works end to end.
3. **Given** the retired toolkit line, **When** the package references are listed, **Then** none remains, and
   the waitlist-to-detail transition still animates.
4. **Given** JSON-backed stores, **When** the code is read, **Then** each uses generated serialization
   metadata and defines its options once, and every stored value round-trips unchanged.
5. **Given** the rotating-text control, **When** its source is read, **Then** it uses the toolkit's
   animation idiom and looks the same on screen.
6. **Given** the graphics runtime resolves to one portable version, **When** the build accommodation is
   removed and a cold restore runs, **Then** the restore raises no runtime-identifier compatibility warning;
   if it does, the accommodation stays and the reason is recorded.

### User Story 6 - The interface speaks to the user, in the machine's conventions (Priority: P2)

A person reviewing a setup or request reads a sentence. Nobody sees a resource key name where a word
belongs, no sentence in a screen is a literal that cannot be translated, and a coil weight shows separators
the machine expects and a unit that comes from a resource rather than being typed into the code.

**Why this priority**: A key name on screen is a visible defect in a plant application, and a weight
formatted for the wrong culture is a number a person can misread.

**Independent Test**: Open the setup review for an item with no scrap and for one with subordinate parts;
both read as sentences. Search the view definitions for literal prose; the search returns nothing. Show a
coil weight on a machine with a non-default separator and confirm both the format and the unit.

**Acceptance Scenarios**:

1. **Given** a review with no scrap, **When** the setup review is shown, **Then** it reads a sentence, not
   a key name.
2. **Given** a review with subordinate parts, **When** the setup review is shown, **Then** the count reads a
   sentence with the count in it, not a key name.
3. **Given** every screen in the application, **When** its view definition is searched for literal
   user-visible text, **Then** no such literal remains and each string resolves through one convention.
4. **Given** a coil weight on a machine whose culture uses a decimal comma, **When** the weight is shown,
   **Then** the number uses that machine's separators and the unit comes from a resource.

### User Story 7 - The AI development environment states only what measures true (Priority: P2)

A developer or an agent opens this repository in a fresh clone. The platform version the briefing files
cite is the version actually in use. The automation hook that records work runs rather than silently doing
nothing. The documentation lookup tools the team relies on are declared by the repository itself, so a new
clone gets them, and the note that records tooling limits records the failure the team actually hit.

**Why this priority**: An agent that is told a wrong version produces confidently wrong work, and a hook
that silently no-ops loses the record of what happened. Both cost more than the fix.

**Independent Test**: Read every agent-facing file that states a platform version and compare it against the
solution. Trigger the hook and confirm it records rather than erroring out. Open a fresh clone and confirm
both documentation servers are declared and one answers.

**Acceptance Scenarios**:

1. **Given** the agent-facing files that state a platform version, **When** each is read, **Then** each names
   the version in use, or states the rule with no version at all, and none states a superseded version as a
   current fact.
2. **Given** the automation hook, **When** it fires on this platform, **Then** it runs its interpreter and
   records the work instead of failing or doing nothing.
3. **Given** a fresh clone, **When** the repository configuration is read, **Then** it declares both
   documentation lookup servers, and the runtime and network prerequisites they need are recorded.
4. **Given** the tooling note, **When** it is read, **Then** it records the dependency-analysis tool failure,
   the signature of that failure and the command that works instead.

### User Story 8 - Documentation leads to things that exist (Priority: P2)

A person following an operations runbook runs the command it gives and it works. A defect record cites the
query that is actually in the repository. A design record's comparison documents are newer than the design
they compare, so a reader can trust that what is pictured is what was built.

**Why this priority**: A wrong path in a runbook is discovered at the worst moment, during an install. The
fix is trivial; the cost of leaving it is not.

**Independent Test**: Open each of the five references named by the audit and confirm the target exists.
Compare the timestamps of the three comparison documents against their design source.

**Acceptance Scenarios**:

1. **Given** the mock-service install documentation, **When** the command it gives is followed, **Then** the
   script it names exists at the named path and runs.
2. **Given** the live-and-mock update documentation, **When** its install path is followed, **Then** it
   names the same, existing path.
3. **Given** the drift ledger, the defect record and the startup design brief, **When** each citation is
   followed, **Then** the cited artifact exists, or the statement is marked historical with its date.
4. **Given** a documentation index that links a tool note, **When** the link is followed, **Then** the target
   exists.
5. **Given** the three sign-in comparison documents, **When** they are compared with their design source,
   **Then** each document is newer than the design it was generated from.

### User Story 9 - The test suite asserts behaviour instead of waiting on a clock (Priority: P3)

A developer runs the suite and it finishes sooner, and the one test that used to sleep for three seconds
now proves its point by controlling time directly. When it fails, it fails for the right reason.

**Why this priority**: A real sleep is both slow and weak. It is worth fixing, and it is not urgent.

**Independent Test**: Run the suite and confirm no test waits on a real clock for more than half a second.
Make the condition under test wrong and confirm the test still fails.

**Acceptance Scenarios**:

1. **Given** the test that previously slept, **When** it runs, **Then** it controls time through an injected
   source or a bound rather than a real sleep.
2. **Given** that test, **When** the behaviour it covers is broken deliberately, **Then** it fails, which
   proves the assertion, not the wait, is doing the work.
3. **Given** the whole suite, **When** it runs, **Then** no test waits on a real clock for more than 500 ms.

### User Story 10 - The settings screen can be reviewed and tested (Priority: P3)

A maintainer asked to change one settings behaviour opens the settings code and finds a class of a
reasonable size with cohesive parts, not one file of more than two and a half thousand lines. The screen
behaves exactly as it does today, and the existing tests that cover it still pass.

**Why this priority**: This is the largest single refactor in the register and delivers no user-visible
change. It earns its place by making every later settings change reviewable.

**Independent Test**: Open each settings category and exercise it; behaviour is unchanged. Read the settings
code and confirm it is split into cohesive parts behind the existing page. Run the settings tests.

**Acceptance Scenarios**:

1. **Given** the settings screen, **When** each category is opened, read and changed, **Then** every
   behaviour and validation message is the same as before.
2. **Given** the settings code, **When** it is read, **Then** the logic is split into cohesive parts behind
   the existing page rather than concentrated in one class.
3. **Given** the existing settings tests, **When** they run, **Then** they pass, and any test that must
   change changes only to follow the split, not to weaken an assertion.

## Edge Cases

- A package version reaches a project that a per-project exception must hold at a different version. The
  exception is declared as an exception; the shared declaration does not silently lose.
- A dependency upgrade changes a control template, a theme, or a date-mapping rule. The change is caught by
  the phase's manual screen pass, not by the build, and the phase does not close until it is.
- A restore cannot reach a feed the configuration maps. The restore fails saying which feed is unreachable,
  rather than falling back to whatever the machine has configured.
- A host is upgraded before its environment variables are provisioned. Start-up fails loudly and names the
  variable; it never appears to work with a blank connection string.
- The inline-statement path turns out to have a caller nobody knew about. The caller is found and resolved
  before the path is removed; the path is not kept "just in case".
- The graphics runtime does not unify as predicted. The build accommodation stays and the reason is
  recorded; the phase closes on the actual restore result, not on the prediction.
- A settings value written by the old serialization is read by the new generated metadata. It round-trips
  unchanged, including the optional and derived values.
- A resource key is composed at run time from a permission or role name. Extraction of literals must not
  assume every key is a quoted literal.
- A search is abandoned, then superseded, then the older answer arrives. The newer result stands and the
  older is discarded.
- The process closes while a flush is pending and does not finish. Shutdown still completes within the
  bound rather than hanging.
- A documentation server cannot be reached from a locked-down machine. The declaration is rejected rather
  than half-declared, and the reason is recorded.
- A later phase is started while an earlier phase is not green. The earlier phase is finished first; the
  order is a requirement, not a suggestion.

## Requirements

### Functional Requirements

Each requirement carries its approved proposal ID, so any change can be traced to its decision.

**Dependency currentness**

- **FR-001** (DEP-001): The Windows platform UI framework the interface is built on MUST reach its approved
  target release in every project that declares it, and every screen MUST behave as it does today afterwards.
- **FR-002** (DEP-002): The hosting, configuration, dependency-injection, logging and options abstractions
  MUST reach their approved target release, and the application MUST start, bind configuration and filter
  logs as it does today.
- **FR-003** (DEP-003): The test tooling MUST reach its approved target releases, and the suite MUST discover
  the same number of tests and pass with them.
- **FR-004** (DEP-004): The window helper library, the Windows SDK build tools and the protected-data library
  MUST each reach their approved target release, with no behavioural change beyond their documented fixes.
- **FR-005** (DEP-005): The SQL Server client MUST reach its approved target release, the version pin that
  existed only to escape a vulnerable dependency range MUST be retired in the same change, and Infor Visual
  reads MUST be proven through the live path afterwards.
- **FR-006** (DEP-006): The MySQL driver MUST reach its approved target release, and every stored-procedure
  call path MUST be exercised against the local instance before the plant host is touched.
- **FR-007** (DEP-007): The third-party WinUI control library MUST reach its approved target release, and
  every screen MUST be reviewed for visual regression, with this upgrade sequenced last among the runtime
  upgrades.
- **FR-008** (DEP-008): Each package version MUST be declared exactly once for the whole solution, with
  genuine per-project exceptions declared as exceptions, so that a later version change is a single edit and
  two projects cannot hold different versions of one package.
- **FR-009** (DEP-009): A configuration tracked by the repository MUST declare the approved package feed and
  MUST map package patterns to that feed, so that a clone restores from the same feed set as every other
  clone and a package from an unmapped source is refused.

**Modernization**

- **FR-010** (MOD-001): The community toolkit line the vendor has stopped shipping MUST be replaced by the
  current line, the package reference nothing uses MUST be removed, and the waitlist-to-detail connected
  animation MUST still run.
- **FR-011** (MOD-002): The services that open their own database connections MUST route through the existing
  shared data-access seam, so one retry, timeout and availability policy applies, and no behaviour those
  services had MUST be lost.
- **FR-012** (MOD-003): The data-access seam MUST execute stored procedures only; the path that accepted
  caller-supplied statement text MUST be gone, and every caller that depended on it MUST be identified and
  resolved before removal.
- **FR-013** (MOD-004): JSON serialization MUST use generated per-type metadata instead of run-time
  reflection, with each store defining its serialization options once, and every stored value MUST
  round-trip unchanged.
- **FR-014** (MOD-005): The Infor Visual read path MUST have one owner, and the setup tool MUST use that
  owner rather than a second copy of the same query logic.
- **FR-015** (MOD-006): The shared library MUST contain no view type, and every caller of the dialog that
  moves MUST still show it with the same content and result.
- **FR-016** (MOD-007): The generic-request logic library and its data library MUST reference no waitlist UI
  project, so the dependency direction between them is one-way, and the generic-request flow MUST still work
  end to end.
- **FR-017** (MOD-008): The settings logic MUST be split into cohesive parts behind the existing page, with
  the settings surface and every validation message unchanged.
- **FR-018** (MOD-009): The hand-built rotating-text animation MUST use the toolkit's animation idiom, with
  visual parity confirmed on screen.

**Security**

- **FR-019** (SEC-001): No credential value MUST remain in any tracked file; configuration MUST keep its
  shape and name the environment variable that supplies each value; a host without a value MUST fail at
  start-up with a message naming it and MUST NOT fall back to an empty string or a placeholder; and the
  values already committed MUST be treated as disclosed and rotated by an operational step outside the
  repository.

**Performance**

- **FR-020** (PERF-001): A part search MUST reach the plant system no more than once per typing pause, a
  superseded search MUST NOT be able to overwrite a newer result, and an abandoned search MUST be cancelled.
- **FR-021** (PERF-002): No blocking wait on the interface thread MUST remain on the settings-load or
  shutdown path, and a slow configuration read MUST leave the window painting and accepting input.

**User experience**

- **FR-022** (UX-001): The setup review MUST display words rather than a resource key name in each of the two
  places that currently fall back to the key.
- **FR-023** (UX-002): No user-visible sentence MUST be a literal in a view definition, and one resource
  convention MUST apply across the application.
- **FR-024** (UX-003): A coil weight MUST use the current machine's number formatting and MUST take its unit
  from a resource rather than a literal in code.

**AI development environment**

- **FR-025** (AI-001): Every agent-facing statement of the platform version MUST name the version in use or
  carry no version, and MUST be re-verified whenever the platform moves.
- **FR-026** (AI-002): The specification-workflow hook MUST run its interpreter-wired arm with an interpreter
  that exists on this platform, so the hook records work instead of silently doing nothing.
- **FR-027** (AI-003): The repository MUST declare both documentation lookup servers so a fresh clone has
  them, and the runtime and network prerequisites each needs MUST be recorded next to the declaration.
- **FR-028** (AI-004): The tooling note MUST record the dependency-analysis tool's failure on this solution,
  the signature of that failure and the command that works instead.

**Development workflow**

- **FR-029** (DEV-001): The solution-wide runtime-identifier graph accommodation MUST be removed once the
  graphics runtime resolves to one portable version, gated on a cold restore that raises no
  runtime-identifier compatibility warning; if it does raise one, the accommodation stays and the reason is
  recorded.
- **FR-030** (DEV-002): Each of the five documentation references named by the audit MUST resolve to an
  artifact that exists, or be marked historical with the date it stopped being true.
- **FR-031** (DEV-003): The test that waits on a real clock MUST assert its verdict through an injected time
  source or a bound, and MUST be faster and stronger than before.
- **FR-032** (DEV-004): The three comparison documents MUST be regenerated from their current design source,
  so each is newer than the design it was generated from.

**Cross-cutting**

- **FR-033**: The work MUST proceed in the eight ordered phases below. A phase MUST NOT begin before its
  prerequisites are met, and each phase MUST end with a successful restore, a build with no errors and a
  green test suite before the next begins.

  | Phase | Theme | Proposals (in order within the phase) |
  |---|---|---|
  | 1 | Immediate correctness and hygiene | SEC-001, PERF-002, UX-001, AI-001, AI-002, DEV-002, AI-004 |
  | 2 | Dependency infrastructure | DEP-008, DEP-009, DEP-004 |
  | 3 | Runtime package upgrades | DEP-002, DEP-006, DEP-005, DEP-001, DEP-007 |
  | 4 | Test stack and test quality | DEP-003, DEV-003 |
  | 5 | Framework-native replacement | MOD-001, DEV-001, MOD-003, MOD-002, MOD-005, MOD-004, MOD-009 |
  | 6 | Architectural modernization | MOD-006, MOD-007, MOD-008 |
  | 7 | Performance and localization | PERF-001, UX-003, UX-002 |
  | 8 | AI environment and design records | AI-003, DEV-004 |

- **FR-034**: Existing behaviour the plant depends on MUST survive every change: internal stores are read and
  written live with no demo or mock mode; every data operation goes through a stored procedure; only Infor
  Visual reads may fall back to the local mirror, and only on the reachability probe's settled verdict, never
  replacing a reachable but empty live answer; the mock-service deployment script keeps its own-server host
  guard; views stay included by the build's default globs with no explicit page item; and no enforcement the
  build does not already have is added.
- **FR-035**: Only work owned by an approved proposal MAY change, and nothing outside the approved register
  MAY be implemented. Each requirement above is one proposal; a change that serves none of them does not
  belong in this feature.
- **FR-036**: Every phase boundary MUST carry the evidence the verification plan names, and every dependency
  change MUST have the previous release recorded before it changes, so a revert is a single edit.

### Key Entities

None. This feature introduces no stored data, no new record and no changed format. It changes how existing
artifacts are declared, referenced and verified.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Zero credential values in tracked files, down from the three credential sets found by the audit.
  A host with the variables unset fails at start-up naming the variable; a host with them set starts normally.
- **SC-002**: All 32 approved proposals reach their stated end state, and zero proposals outside the register
  are implemented.
- **SC-003**: Every one of the 8 phases ends with a restore from a cleared cache that succeeds, and a build
  with zero errors and no new warning.
- **SC-004**: The test suite is green after every phase from phase 2 onward, with the discovered test count
  unchanged unless the phase's own proposal changes it.
- **SC-005**: All 17 repository guardrail tests pass after every phase, including the inline-statement audit
  and the retired-symbol audit.
- **SC-006**: A ten-character scripted part number with several typing pauses produces a single-digit count of
  plant round trips, no more than one per pause, with the newest result shown.
- **SC-007**: Zero blocking waits remain on the settings-load and shutdown paths; a slow configuration read
  leaves the window painting throughout.
- **SC-008**: Zero literal user-visible text attributes remain in view definitions, down from the 140 the audit
  counted.
- **SC-009**: Zero unresolved resource key names are visible on the setup review, down from two.
- **SC-010**: A coil weight renders with the current machine's separators and a unit that comes from a
  resource, on a machine whose culture differs from the developer's.
- **SC-011**: Zero packages remain on the retired toolkit line, and every package resolves at exactly one
  version across all 17 projects.
- **SC-012**: A restore from a cleared cache succeeds with no runtime-identifier compatibility warning after
  the build accommodation is removed.
- **SC-013**: Zero agent-facing files state a superseded platform version as current, down from three files.
  The workflow hook completes and records rather than failing or doing nothing.
- **SC-014**: A fresh clone declares both documentation lookup servers, and at least one of them answers a
  documentation query.
- **SC-015**: All five documentation references resolve, and all three comparison documents are newer than
  their design source.
- **SC-016**: Zero view types in the shared library, and zero waitlist UI project references from the
  generic-request libraries.
- **SC-017**: The settings surface behaves identically (every existing settings test passes) and no settings
  class remains at the size that made review impractical.
- **SC-018**: No test waits on a real clock for more than 500 ms, including the one that previously waited
  three seconds.

## Non-goals

- Deleting the roughly 298 resource keys the audit could not prove were referenced. The count is an upper
  bound, keys are composed at run time, and a wrong deletion is a run-time regression.
- Any user-interface redesign. No layout, control or interaction change is in scope; a future one carries its
  own rendered before-and-after design.
- Trimming the specification-workflow agent files or the prompt stubs. Their cost is metadata only.
- Auditing or changing the user-level editor configuration of any workstation, including user instruction
  files, user-level tool servers and installed extensions.
- Changing a running database: no store is altered, no MySQL connection is opened, and no plant host is
  contacted by this work.
- Migrating to a different database engine, object-relational mapper, UI framework or test framework.
- Profiling, benchmarking or load testing. No number in the source audit is a measured benchmark.
- Rewriting historical specification documents that cite the old platform version. They record what was true
  when they were written.
- Implementing a proposal that did not reach a decision. There are none.

## Change-level rules

- No demo or mock mode is introduced. Internal stores are read and written live.
- Every data operation goes through a stored procedure.
- Only Infor Visual reads may fall back to the local mirror, and only on the reachability probe's settled
  verdict. A reachable but empty live answer is a real answer and is never replaced.
- The mock-service deployment script continues to refuse to run off its own server host.
- No continuous-integration workflow is added, warnings are not promoted to errors, and no enforcement is
  introduced that the build does not already have.
- Views remain included by the build's default globs; no explicit page item is added to any project.
- Every dependency change records the previous release before it changes, so a revert is one edit.
- No credential value is printed, copied or transmitted by this work; values already committed are treated as
  disclosed.
- No package change may introduce a copyleft licence obligation.
- A phase does not close while any earlier requirement it depends on is unmet, and an unplanned change found
  mid-phase is recorded as a new proposal rather than folded silently into an approved one.

## Verification plan

Every gate below is run at the stated point, and its result is the evidence that closes the phase.

| Gate | Applies to | Evidence |
|---|---|---|
| Cold restore | every dependency phase | A restore with a cleared package cache succeeds and raises no new warning |
| Clean build | every phase | A solution build with zero errors; warnings reported, never suppressed |
| Test suite | every phase from phase 2 | The suite is green and the discovered method count matches, unless the phase's proposal changes it |
| Guardrail suite | every phase | All 17 repository audit tests pass; one that trips is a design decision, not a test to edit |
| Manual screen pass | the platform upgrade, the control library upgrade, the toolkit replacement, the dialog move, the settings split, the string extraction | Sign-in, waitlist list and detail, setup, generic request, settings and the deployment tool, each opened at each display scale in use |
| Connectivity | the SQL client upgrade and the MySQL driver upgrade | An internal-store read and write against the local instance, then an Infor Visual read through the live path |
| Data integrity | the credential removal | A first run with no variables fails naming the variable; a run with them succeeds |
| Rollback readiness | every dependency change | The previous release recorded before the change, so a revert is one line |
| Reference resolution | the documentation repairs and the AI version corrections | Each cited path opened, each stated version compared against the solution |
| Timing | the search change and the test change | A scripted typing sequence counted; the suite timed and the sleep-based test inspected |
| Locale | the weight formatting and the string extraction | A run on a machine whose culture differs from the developer's, with every screen read |

## Assumptions

- **Unattended run.** This specification was drafted without a person available to answer questions. Every
  open question the source audit recorded was already non-blocking, so each is settled here as an assumption
  rather than raised. None changes an approved proposal.
- **The decision register is the authority.** All 32 proposals were decided on the source audit's date, with
  zero rejected, zero deferred and none outstanding. No further approval is needed to plan or implement them,
  and no proposal may be dropped or added without a new decision.
- **One audit finding has already moved.** The source audit records that no NuGet configuration is tracked by
  the repository at all. A root configuration is now tracked (commit `598c66cd`), but it declares only the
  feed and carries no package pattern mapping. DEP-009 therefore becomes: add the package pattern mapping to
  the tracked configuration, keeping the feed set reproducible. The substance of the requirement is
  unchanged; only the starting point is.
- **The plant host is not reachable during this work.** Every store-dependent acceptance is proven against
  the local database instance first, and the plant host is touched only after the local proof passes.
- **The environment variables are an operational prerequisite.** Before the credential removal can be
  delivered, whoever runs plant deployments provisions the two connection-string variables on each host, and
  the exposed values are rotated. That work is outside the repository and is recorded as a prerequisite, not
  as a change.
- **Two upgrades may need code changes that cannot be predicted.** The platform upgrade and the third-party
  control library upgrade each carry a manual review gate instead of a prediction about which control or
  theme changes. A defect found in that review is fixed inside the same phase.
- **No continuous integration exists and none is added,** so every gate in the verification plan runs on a
  developer machine or a plant test machine, recorded as evidence.
- **The earlier audit's work is not reopened.** The AI-tooling remediation recorded separately in this
  repository is implemented and is not re-proposed here; only the four gaps the current audit names are in
  scope.
- **The unreferenced resource keys stay.** Whether those keys are genuinely dead cannot be settled without a
  run-time sweep, because some are composed at run time. They are left in place deliberately.

## Verbatim Constraints

These literals are exact. Do not paraphrase, recase or rename them.

- Approved target releases, keyed by proposal ID: `DEP-001` Microsoft.WindowsAppSDK 2.4.0 to 2.7.0;
  `DEP-002` Microsoft.Extensions.Hosting, .Configuration.Abstractions, .DependencyInjection.Abstractions,
  .Logging.Abstractions and .Options 10.0.10 to 11.0.0; `DEP-003` MSTest.TestAdapter and MSTest.TestFramework
  3.7.0 to 4.5.3, Microsoft.NET.Test.Sdk 17.13.0 to 18.12.0, coverlet.collector 6.0.2 to 10.1.1;
  `DEP-004` WinUIEx 2.9.2 to 2.9.3, Microsoft.Windows.SDK.BuildTools 10.0.28000.2270 to 10.0.28000.2957,
  System.Security.Cryptography.ProtectedData 10.0.0 to 11.0.0; `DEP-005` Microsoft.Data.SqlClient 6.1.1 to
  8.0.0 with the `System.Security.Cryptography.Pkcs` 9.0.21 pin retired; `DEP-006` MySqlConnector 2.6.1 to
  3.0.0; `DEP-007` DevWinUI 10.8.0 to 11.1.0.
- Resource keys to add: `Setup_Review_Scrap_None`, `Setup_Review_Section_SubordinateParts_Count`, in
  `Strings/en-us/Resources.resw`.
- Configuration property that carries each value's environment variable name:
  `ConnectionStringEnvironmentVariable`.
- Central package management: file `Directory.Packages.props`, property
  `ManagePackageVersionsCentrally`, per-project exception `VersionOverride`.
- Package source mapping element: `packageSourceMapping`.
- Build property to retire: `UseRidGraph` in `Directory.Build.props`, gated on
  `NETSDK1206` not appearing in a cold restore.
- Repository tool-server declaration: `.mcp.json`, with `microsoft-learn` at
  `https://learn.microsoft.com/api/mcp` and `context7` run through `npx @upstash/context7-mcp`.
- Interpreter name for the workflow hook's other arm: `python` (never `python3`).
- Documented mock-service install command path: `MTM_Waitlist.Mock.Service/deploy/install-mock-service.ps1`.
- Guardrail tests that must keep passing: `InlineSqlAuditTests`, `RetiredSymbolAuditTests`.
- Baseline counts this feature is measured against: 17 projects, 32 approved proposals, 2,768 test methods,
  17 guardrail tests, 1,079 resource keys, 140 literal prose attributes, 2 missing keys, 3 files stating a
  superseded platform version, 5 broken documentation references, 3 stale comparison documents.
