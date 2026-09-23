# Feature Specification: One Picture Policy, Configurable Picture Storage, and a Local Picture Cache

**Feature Branch**: `007-picture-storage-and-cache`
**Created**: 2026-09-22
**Status**: Implemented, with one requirement outstanding (FR-021)
**Input**: User description: "Every picture in the application falls back to one no-image picture. Where the
pictures and the key files live is a setting rather than a value built into the application, and only the IT
Department and the Developer role may change it. Each computer keeps a local copy of the pictures it draws, so a
screen does not reach across the network for every tile."

> This specification records work that has already been built and verified. It is written as the requirement
> statement it should have been, so the behaviour is reviewable and testable on its own terms rather than only as
> a description of the change. The delivered inventory, with file-level evidence and the verification runs, is in
> `progress.md` beside this file. Requirements are technology-agnostic by design; no file or type names appear here.

---

## User Scenarios & Testing

### User Story 1 - A picture that cannot be drawn says so, on every screen (Priority: P1)

A person looking at a request, a work centre or a dunnage search sees either a real picture or the application's
single no-image picture. They never see a blank space where a picture belongs, and they never see a picture of a
different thing standing in for the one nobody has configured.

**Why this priority**: a blank tile is indistinguishable from a picture that failed to load, and the plant floor
reads a blank tile as a fault. Built-in artwork standing in for an item's picture is worse than a blank tile: it
shows something that looks authoritative and is not what the item looks like.

**Independent Test**: for each surface that draws a picture, configure nothing, then configure a missing file, then
configure a file that is unreadable, too small, or the wrong shape. Each case must draw the one no-image picture.

**Acceptance Scenarios**:

1. **Given** nothing is configured for a picture, **When** a screen draws it, **Then** the no-image picture is drawn.
2. **Given** a configured picture whose file is not there, **When** a screen draws it, **Then** the no-image picture
   is drawn and the miss is recorded.
3. **Given** a configured file that exists but cannot be read as a picture, **When** a screen draws it, **Then** the
   no-image picture is drawn.
4. **Given** a configured file smaller than 48 by 48 pixels, or not square, **When** a screen draws it, **Then** the
   no-image picture is drawn.
5. **Given** an item with no picture of its own, **When** its card is drawn, **Then** the no-image picture is drawn
   rather than artwork chosen from the item's identity.
6. **Given** a machine that has just started, **When** the first screen opens, **Then** the pictures already
   configured for that screen are drawn, without needing a second visit.

### User Story 2 - Where pictures and key files live is a setting (Priority: P2)

IT changes the folder the application keeps its pictures in, or the folder the key files are read from, without a
new release. A picture stored by the application is found by every computer, not only the one that stored it.

**Why this priority**: the folders are the one part of this feature that operations genuinely move. A value compiled
into the application turns a share migration into a development task.

**Independent Test**: change the configured picture folder, store a picture, and confirm a second computer resolves
the same stored value to the same file under its own configured folder.

**Acceptance Scenarios**:

1. **Given** a configured picture folder, **When** a picture is stored, **Then** what is recorded does not name the
   folder, so the value resolves under any folder configured on any computer.
2. **Given** a picture is replaced by a later one, **When** the new picture is stored, **Then** the previous file is
   archived beside it rather than overwritten.
3. **Given** a person who is neither IT Department nor Developer, **When** the storage settings are read from the
   permission store, **Then** changing those folders is refused.
4. **Given** catalogue rows naming pictures the application does not ship, **When** the seed is applied, **Then**
   those rows are removed and are not recreated.

### User Story 3 - The pictures are already on the computer when a screen opens (Priority: P3)

Each computer keeps its own copy of the pictures the application draws, refreshed while the application starts. A
screen draws the local copy, so the first list after a start does not wait for the network and keeps working if the
share is briefly unavailable.

**Why this priority**: it is the difference between a list that opens immediately and one that fetches every tile
across the network. It does not change what is shown, only how quickly and how reliably, so it ranks below the two
stories that decide what is drawn at all.

**Independent Test**: start the application with a reachable source, confirm copies appear; start again unchanged
and confirm nothing is copied; remove a picture from the source and confirm its copy is removed; make the source
unreachable and confirm the application still opens.

**Acceptance Scenarios**:

1. **Given** a picture source with pictures not yet copied, **When** the application starts, **Then** each picture is
   copied and the start continues.
2. **Given** every picture already copied and unchanged, **When** the application starts again, **Then** nothing is
   copied and nothing is removed.
3. **Given** a copied picture whose source file has been replaced, **When** the application starts, **Then** the copy
   is replaced, judged by the file's size or its last-written time.
4. **Given** a copy whose picture no longer exists at the source, **When** the application starts, **Then** the copy
   is removed.
5. **Given** a source that cannot be reached, **When** the application starts, **Then** the failure is recorded, the
   existing copies are left alone, and the application opens.
6. **Given** caching is switched off for the organisation, **When** the application starts, **Then** nothing is
   copied and nothing is removed, and the existing copies stay on disk.
7. **Given** an operator with the entitlement, **When** they ask for the copies to be refreshed, **Then** the refresh
   runs without a restart and reports what was copied, what was removed, and any source that could not be reached.

### User Story 4 - An operator can see and change the caching without a restart (Priority: P3)

The person entitled to change the application's picture arrangement can switch the local cache on or off, say where
the copies are kept, and run the refresh at once, from the Settings screen.

**Why this priority**: it makes an existing behaviour operable rather than requiring a restart to correct a wrong
folder. It is worth having only once the cache exists.

**Independent Test**: as an entitled account, switch the cache off and confirm the switch is stored for the whole
organisation; as an unentitled account, confirm the section is not offered at all.

**Acceptance Scenarios**:

1. **Given** an account holding the picture entitlement, **When** Settings is opened, **Then** the caching section is
   offered with its switch, its folder and its refresh action.
2. **Given** an account without that entitlement, **When** Settings is opened, **Then** the section is not offered.
3. **Given** the switch was changed on one computer, **When** the setting is read on another, **Then** the same value
   is read, because the setting belongs to the organisation and not to the person.
4. **Given** no caching facility is available to the screen at all, **When** Settings is opened, **Then** the section
   is not offered rather than being offered and doing nothing.

### Edge Cases

- A picture path is recorded as an absolute path by an earlier version. It is still resolved as written, and the
  local-copy preference still applies to it.
- A picture path names a file outside the configured picture folder. No local copy can belong to it, so the file is
  drawn from where it is, and no copy is claimed.
- Two stored pictures share an identifier under different scopes. They are archived in their own scope's archive
  folder, so one replacement never overwrites another scope's history.
- The copy folder and the picture folder are the same folder. Nothing is copied over itself and nothing is removed.
- A picture source contains file types the application does not draw. They are skipped rather than copied.
- The application folder holding the default pictures is absent from a machine. The no-image picture is still
  resolved from the application's own packaged resources.
- The source share is reachable but contains no pictures. Nothing is copied and nothing already copied is removed,
  because an empty source is not evidence that the pictures were deleted.

## Requirements

### Functional Requirements

- **FR-001**: A picture SHALL be drawn only when the configured file exists, can be read as a picture, measures at
  least 48 by 48 pixels, and is square within a small tolerance.
- **FR-002**: When nothing is configured, or the configured file is missing, unreadable, undersized or the wrong
  shape, the application SHALL draw one single no-image picture, and SHALL NOT draw a blank space.
- **FR-003**: The application SHALL NOT draw built-in artwork standing in for a picture that is not configured.
- **FR-004**: The same picture decision SHALL apply on every surface that draws a picture: request cards, detail
  pages, wizard tiles for work centres and categories, setup work-centre cards, dunnage image search, and picture
  previews in Settings.
- **FR-005**: A machine that has just started SHALL resolve configured pictures on the first screen it shows.
- **FR-006**: The folder holding the pictures SHALL be a setting, and SHALL NOT be a value compiled into the
  application.
- **FR-007**: The folder holding the key files SHALL be a setting, and each key file SHALL be named after its key.
- **FR-008**: A picture stored by the application SHALL be recorded relative to the picture folder, so the same
  recorded value resolves under whichever folder a computer is configured with.
- **FR-009**: Changing either storage folder SHALL be restricted to the IT Department and the Developer role.
- **FR-010**: A stored picture replaced by a later one SHALL be archived beside it, within its own scope, rather than
  overwritten.
- **FR-011**: Catalogue rows naming pictures the application does not ship SHALL be removed, and the seed SHALL NOT
  recreate them.
- **FR-012**: Each computer running the application SHALL keep a local copy of the pictures the application draws,
  refreshed while the application starts and before the first screen is shown.
- **FR-013**: A picture SHALL be copied only when no local copy exists, or when the copy's size or last-written time
  differs from the source's.
- **FR-014**: A local copy whose picture no longer exists at the source SHALL be removed. Nothing else on the
  computer SHALL be removed.
- **FR-015**: The refresh SHALL be best effort. An unreachable source SHALL be recorded, SHALL leave existing copies
  in place, and SHALL NOT prevent the application from opening.
- **FR-016**: A screen SHALL draw the local copy when one exists, and SHALL draw the picture at its source when one
  does not.
- **FR-017**: Whether caching is in use SHALL be one setting for the whole organisation, and switching it off SHALL
  leave existing copies on disk.
- **FR-018**: Where the copies are kept SHALL be one setting for the whole organisation, defaulting to a folder the
  signed-in person can always write to without elevated rights.
- **FR-019**: Both caching settings SHALL be changeable by the IT Department and the Developer role only.
- **FR-020**: An operator holding that entitlement SHALL be able to refresh the copies without restarting the
  application, and SHALL be told what was copied, what was removed, and which source could not be reached.
- **FR-021** *(outstanding, not built)*: The two storage folders SHALL be changeable from a screen in Settings by the
  roles entitled to change them. The entitlement and the stored values exist; no screen writes them yet.

### Key Entities

- **Picture policy**: the single rule deciding whether a file may be drawn, and the one picture drawn when it may
  not. Applies to every surface.
- **Picture folder**: the configured root that stored picture values are resolved against. One setting for the whole
  organisation.
- **Key folder**: the configured root holding the key files, one per key, named after the key. One setting for the
  whole organisation.
- **Stored picture value**: what is recorded when a picture is configured. Names the file's path within a scope, not
  a machine's folder.
- **Picture cache**: the per-computer copy of the picture sources. Two sources are mirrored: the organisation's
  pictures, and the dunnage pictures the setup workflow draws.
- **Cache settings**: whether caching is in use, and where the copies are kept. Both belong to the organisation.

## Success Criteria

### Measurable Outcomes

- **SC-001**: For every surface that draws a picture, a missing, unreadable, undersized or non-square file draws the
  same no-image picture, proved per surface by an automated check.
- **SC-002**: No surface draws a blank space where a picture belongs.
- **SC-003**: A picture configured for an item appears on the first screen a freshly started machine shows.
- **SC-004**: A second start with an unchanged source copies nothing.
- **SC-005**: A start with an unreachable source completes, and the application opens.
- **SC-006**: A stored picture value recorded on one computer resolves to the same picture on another computer
  configured with the same folder.
- **SC-007**: Switching the cache off or repointing the copy folder takes effect on every computer, and takes effect
  without a new release.
- **SC-008**: The whole automated suite reports no failures after the change, including the guards that fail the
  build if retired placeholder behaviour returns.

## Assumptions

- The picture folder and the key folder are reachable by every machine that runs the application, by the same path,
  and are administered outside this application.
- The dunnage pictures are the ones the receiving application already maintains, and its own local copy is reused
  when that computer already has one rather than being duplicated.
- A picture's last-written time at the source is the signal that it changed. Hashing every picture on every start
  would read the whole share, which is the cost the cache exists to avoid.
- The IT Department and the Developer role are the two roles that administer shared locations; no other role needs
  them.

## Outstanding

- **FR-021**: no Settings screen writes the two storage folders yet. The entitlement
  (`permission.settings.storage_paths`), the stored values and the resolution everywhere else are in place, so the
  remaining work is a screen and nothing else.
- The picture areas are not registered as living capabilities, so drift across them is invisible to the drift check
  by design. `living-specs.yml` records this blind spot; adopting an area is a deliberate, separate decision.

## Living-Spec Deltas

*A delta for the watched capabilities this feature changed. The startup capability receives the one requirement
below. The capability is named on the marker so the fold routes it, and the same heading is already present in
`capabilities/startup/spec.md`, so a later fold replaces it in place rather than adding it twice.*

## ADDED Requirements
<!-- capability: startup -->

### The pictures a screen will need are copied onto the computer while it starts

Startup SHALL refresh the local copy of the pictures the application draws, on the way past the session check and
before the first screen is shown, and SHALL report a line naming that refresh. The refresh SHALL be best effort: a
picture source that cannot be reached SHALL be recorded and SHALL NOT prevent the application from opening or the
remaining checks from running.

#### Scenario: the pictures are already on the computer
- **WHEN** the application starts and every source picture already has a copy that has not changed
- **THEN** nothing is copied and nothing is removed
- **AND** the line naming the refresh is reported anyway, so the splash does not appear to skip a step

#### Scenario: a picture source cannot be reached
- **WHEN** a source folder is unavailable while the pictures are being refreshed
- **THEN** the failure is recorded and the existing copies are left in place
- **AND** the application still opens
