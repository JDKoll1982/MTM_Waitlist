# Feature Specification: A Picture for Every Part

**Feature Branch**: `008-part-pictures`
**Created**: 2026-09-22
**Status**: Draft — brainstormed, registered as the active feature; nothing implemented and no branch created
**Input**: The owner's request: *"help me implement having images for all WIP and Visual Items. Including updating UI
elements to include the image for areas that don't, updating dropdown menus to use the card structure like you just
did with component parts, the folder structure for where images save on the Network drive, and Settings page
creation for adding the images."* Refined by the brainstorm recorded at the end of this file.

> Written from a brainstorm, so the shape below is settled but the details are not yet specified. Requirements are
> technology-agnostic by design; no file, type or column names appear here. The literal layout the owner dictated is
> recorded verbatim in the Brainstorm Log and belongs to the plan, not to this specification.

---

## User Scenarios & Testing

### User Story 1 - The part is in front of me, not just its number (Priority: P1)

A person choosing or checking a part sees its picture beside its number — on the Setup screens where a part is
picked and reviewed, on the die card in the wizard, on the confirmation step, and on the waitlist card itself.

**Why this priority**: it is the whole point of the request. Every surface that names a part today asks a person to
recognise a number; a picture is what makes the number recognisable at a glance, on a shop floor where the same
person handles coils, flatstock, dies and components in one shift.

**Independent Test**: photograph one part, then open each surface that names it and confirm the picture is drawn —
and that a part with no picture draws the one shared placeholder rather than a blank space.

**Acceptance Scenarios**:

1. **Given** a part with a picture, **When** it appears on the Setup part list, **Then** its picture is drawn with
   its number.
2. **Given** the same part, **When** a job's parts are reviewed grouped by family, **Then** the picture is drawn
   beside it.
3. **Given** the same part, **When** the die card that carries it is drawn, **Then** the card shows the part's
   picture — the die card is no longer text-only.
4. **Given** the same part, **When** the confirmation step lists what is about to be asked for, **Then** the picture
   is drawn.
5. **Given** a request whose material is that part, **When** the waitlist card is drawn, **Then** the card shows the
   **part's** picture rather than the picture for the kind of request it is.
6. **Given** a part with no picture, **When** any of the above is drawn, **Then** the application's one no-image
   picture is drawn, and never a blank space and never artwork chosen from the part's family.

### User Story 2 - A Visual part and a WIP part are pictured apart (Priority: P1)

The Infor Visual part and the WIP floor part are two things with their own pictures, even when they share a number.

**Why this priority**: the owner asked for them to be kept apart. It decides the storage key for the whole feature,
so it ranks with the story above rather than below it.

**Independent Test**: give the same number a picture under Visual and a different picture under WIP, then open a
surface that shows each and confirm neither picture appears where the other belongs.

**Acceptance Scenarios**:

1. **Given** the same part number under both systems, **When** a picture is set for the Visual part, **Then** the WIP
   part's picture is unchanged.
2. **Given** a picture for one system only, **When** the part is shown as coming from the other system, **Then** the
   shared placeholder is drawn.
3. **Given** both are pictured, **When** each is shown, **Then** each draws its own picture.

### User Story 3 - A person who may set pictures sets them (Priority: P2)

Someone holding the entitlement opens Settings, finds a part — or a list of the parts that still have no picture —
sets its picture, and can export that list to work through it off the shop floor.

**Why this priority**: without it the pictures can only be placed by putting files on the share by hand, which is
exactly the situation the Settings screen exists to improve. It is P2 rather than P1 because the feature shows
pictures before anyone opens a settings screen.

**Independent Test**: as an entitled account, set a part's picture and see it appear on the surfaces above; as an
unentitled account, confirm the section is not offered at all.

**Acceptance Scenarios**:

1. **Given** an account holding the picture entitlement, **When** Settings is opened, **Then** the part-picture
   section is offered.
2. **Given** an account without it, **When** Settings is opened, **Then** the section is not offered.
3. **Given** a picture is set, **When** it is saved, **Then** the parts that still have none can be listed and that
   list can be exported.
4. **Given** a picture is replaced, **When** the change is saved, **Then** who changed it, when, and what it
   replaced are recorded and can be read back.
5. **Given** a part whose number is not usable as a file name, **When** its picture is saved, **Then** the number is
   made safe, and if the result would belong to a different part the save is refused and says why.

### User Story 4 - Pictures can be added without opening the application (Priority: P2)

Files named after their part are dropped into the right folder and are used the next time a screen draws that part.

**Why this priority**: photographing hundreds of parts is a job for a file copy, not for a dialog opened once per
part. It is P2 because the individual Settings path covers the first pictures.

**Independent Test**: drop a correctly named file into the folder, then open a surface that names that part and
confirm its picture is drawn without the application having been used to add it.

**Acceptance Scenarios**:

1. **Given** a file named exactly after a part, **When** it is placed in that part's family folder, **Then** the
   next draw of that part uses it.
2. **Given** the same picture placed for a different system's folder, **When** the part is drawn, **Then** that
   picture is **not** used, because the two systems are kept apart.
3. **Given** files the application does not draw, **When** they are placed in those folders, **Then** they are
   ignored rather than drawn or reported as an error.

### User Story 5 - The lists that ask by name show the part (Priority: P2)

The lists that ask a person to choose by name — the dunnage types that are shown or hidden, the Setup part list, the
remaining answer list in the wizard, and the building choice — offer the entries as selectable cards.

**Why this priority**: it is the second half of the owner's request, and it is the same pattern the component step
already uses. It ranks below the pictures themselves because a card with no picture on it is a smaller improvement
than a part with no picture.

**Independent Test**: open each converted list and choose an entry; confirm the choice still takes effect and that
the entries are cards where the entry has a picture.

**Acceptance Scenarios**:

1. **Given** the dunnage types that may be shown or hidden, **When** the lists are drawn, **Then** each type is a
   card carrying the type's picture.
2. **Given** the Setup part list, **When** it is drawn, **Then** each part is a card carrying the part's picture.
3. **Given** the building choice, **When** it is opened, **Then** it is a flyout listing every building as a
   selectable card, **and the building currently in effect is not among them**.
4. **Given** an entry that has no picture, **When** its card is drawn, **Then** the shared placeholder is drawn and
   the entry is still selectable.

### User Story 6 - Where the pictures live is a setting, from a screen (Priority: P3)

The person entitled to change the application's picture arrangement changes the picture folder and the key folder
from Settings, without a new release.

**Why this priority**: it is the one requirement the previous picture work recorded and never built — the stored
values, the entitlement and every reader already exist, so what is missing is a screen. It ranks below the pictures
because nothing is drawn incorrectly without it.

**Independent Test**: change the picture folder from Settings, then confirm a picture stored afterwards resolves for
a second computer under that computer's own configured folder.

**Acceptance Scenarios**:

1. **Given** an account holding the storage entitlement, **When** Settings is opened, **Then** both folders are
   offered and changeable.
2. **Given** an account without it, **When** Settings is opened, **Then** neither is offered.
3. **Given** a changed folder, **When** a picture is stored, **Then** the recorded value does not name the folder.

### User Story 7 - A picture appears fast and survives being replaced (Priority: P3)

The first time a part is drawn, its picture is copied to the computer, and later draws use the copy. A replaced
picture is kept for a period rather than destroyed.

**Why this priority**: it changes how quickly and how reliably the pictures appear, not what is shown. It ranks
below the pictures because a picture that is slow is still a picture.

**Independent Test**: draw a part on a machine that has never drawn it and confirm the copy appears; replace the
picture at the source and confirm the previous file is kept and then removed once its period has passed.

**Acceptance Scenarios**:

1. **Given** a part drawn on a computer for the first time, **When** the screen draws it, **Then** the picture is
   copied to that computer and drawn.
2. **Given** a part already copied and unchanged, **When** it is drawn again, **Then** nothing is copied.
3. **Given** a picture that is replaced, **When** the replacement is saved, **Then** the previous file is kept
   beside it and is removed once its retention period has passed.
4. **Given** a picture source that cannot be reached, **When** a screen opens, **Then** the shared placeholder is
   drawn, the failure is recorded, and the screen still opens.

### Edge Cases

- **A part number that is not safe as a file name.** The unsafe characters are replaced. If the result already
  belongs to a different part, the save is refused and says which part it collides with.
- **Two part numbers that differ only by letter case.** File names on the machine are not case-sensitive, so they
  are two parts competing for one file. The second is refused and says which part already owns the name.
- **A part pictured under one system and not the other.** The pictured system draws its picture; the other draws the
  shared placeholder. Neither borrows the other's picture.
- **A part the application can name but has no family folder for.** Its picture is stored in the catch-all folder, so
  an unrecognised part is still picturable rather than silently unpicturable.
- **A picture that exists but must not be drawn** — missing, unreadable, smaller than the accepted size, or not
  square. The shared placeholder is drawn, and the miss is recorded.
- **A part number longer than the storage allows.** It is refused when the picture is set, with the limit stated,
  rather than stored truncated and resolved to the wrong file.
- **A source share that is unreachable or empty.** An empty source is not evidence that the pictures were deleted:
  nothing already copied is removed.
- **A picture placed for a part that no longer exists.** It is never drawn and is reported as an orphan rather than
  being matched to a different part.
- **A work centre and an item whose names are the same**, or two names differing only by letter case — the item
  `other` beside the category `Other`. File names are not case-sensitive, so one folder could never hold both: each
  kind keeps its own folder, and the two pictures live apart without either one being renamed (FR-038).
- **The same picture file placed in two family folders.** Each folder is read for its own parts only; a part looks
  in the folder its family resolves to and nowhere else.
- **A part whose family the application recognises but whose stored family disagrees with its number.** The number's
  prefix decides which folder the picture belongs in, so a mis-tagged part still finds its picture.

## Requirements

### Functional Requirements

**What a picture belongs to**

- **FR-001**: Every part the application can name SHALL be able to carry a picture: the parts read from Infor Visual
  and the parts the WIP floor inventory holds.
- **FR-002**: A Visual part and a WIP part SHALL be pictured separately, even where they share a part number. One
  system's picture SHALL never be drawn for the other.
- **FR-003**: A part's picture SHALL be the same picture wherever that part is named, within its own system.

**Where pictures are kept**

- **FR-004**: A stored picture SHALL be kept under the folder for the collection it belongs to: everything the
  application pictures for its own screens (the request items, the work centres and the categories), the Visual
  parts, or the WIP parts.
- **FR-005**: Within that folder, a picture SHALL be kept in the folder named for the part's family, and a part whose
  family is not recognised SHALL be kept in the folder that holds unrecognised families.
- **FR-006**: One picture SHALL be kept per part per system. A part's picture file SHALL be named after the part.
- **FR-007**: A part number containing characters a file name cannot hold SHALL have those characters replaced. If
  the replacement would collide with a different part, the save SHALL be refused and SHALL name the part it collides
  with.
- **FR-008**: A part number longer than the storage permits SHALL be refused when a picture is set, with the limit
  stated, and SHALL NOT be stored truncated.
- **FR-009**: The folder holding part pictures SHALL be a setting and SHALL NOT be compiled into the application.
- **FR-010**: A stored picture SHALL be recorded relative to that folder, so the same recorded value resolves under
  whichever folder a computer is configured with.
- **FR-011**: A picture replaced by a later one SHALL be kept, and SHALL be removed once the configured retention
  period (FR-037) has passed.
- **FR-012**: The prefix of a part's number SHALL decide the family folder a picture is stored in and read from, so a
  part whose stored family disagrees with its number still finds its picture.

**How a picture is drawn**

- **FR-013**: A part picture SHALL be drawn only when the file exists, can be read as a picture, and meets the
  application's existing acceptance rule for size and shape.
- **FR-014**: Where a part has no picture, or its picture cannot be drawn, the application SHALL draw the one shared
  no-image picture. It SHALL NOT draw a blank space.
- **FR-015**: The application SHALL NOT draw category or family artwork standing in for a part that has no picture.
  One placeholder, and no other stand-in.
- **FR-016**: The folder holding the key files SHALL remain a setting, changeable from the same screen as the
  picture folder.
- **FR-017**: Changing either storage folder SHALL remain restricted to the IT Department and Developer roles.

**Where a picture is shown**

- **FR-018**: The Setup part list, the Setup review of a job's parts, the wizard's die card, and the wizard's
  confirmation step SHALL each draw the part's picture beside its number.
- **FR-019**: The waitlist card SHALL draw the **part's** picture rather than the picture for the kind of request,
  falling back to the shared placeholder.
- **FR-020**: A waitlist request whose material the application cannot picture SHALL show the shared placeholder, and
  SHALL NOT show the family's picture as a stand-in.

**Lists that become cards**

- **FR-021**: The lists that ask a person to choose by name SHALL present their entries as selectable cards: the
  dunnage types that may be shown or hidden, the Setup part list, the remaining answer list in the wizard, and the
  building choice.
- **FR-022**: The building choice SHALL be a flyout listing every building as a selectable card, and the building
  currently in effect SHALL NOT appear in that list.
- **FR-023**: An entry that has no picture SHALL still be offered as a card, drawing the shared placeholder.

**Adding and changing pictures**

- **FR-024**: A screen SHALL set or replace one part's picture, and SHALL be offered only to an account holding the
  picture entitlement.
- **FR-025**: The picture entitlement SHALL cover the IT Department, the Developer role, **the Setup Lead role and the
  Plant Manager role**.
- **FR-026**: The screen SHALL list the parts that have no picture, and that list SHALL be exportable.
- **FR-027**: Setting or replacing a picture SHALL record who did it, when, and what the previous picture was, and
  that record SHALL be readable.
- **FR-028**: A picture placed in the storage folders by hand, named after its part, SHALL be used without the
  application having been used to add it.
- **FR-029**: Files the application cannot draw SHALL be ignored, and SHALL NOT be reported as a fault.

**Speed and resilience**

- **FR-030**: A part's picture SHALL be copied to the computer the first time that part is drawn, rather than all
  pictures being copied when the application starts.
- **FR-031**: A source that cannot be reached SHALL be recorded, SHALL leave existing copies in place, and SHALL NOT
  prevent a screen from opening.
- **FR-032**: An empty source SHALL NOT be treated as evidence that pictures were removed.

**Boundaries this feature keeps**

- **FR-033**: Dunnage pictures SHALL be unchanged by this feature: they keep their own storage and their own
  arrangement.
- **FR-034**: The application's existing acceptance rule for a picture SHALL NOT be relaxed for part pictures, and
  SHALL NOT be tightened for pictures this feature does not touch.
- **FR-035**: Every change this feature makes to the stored data SHALL ship with its own reversal, and the schema
  master lists SHALL be kept in step.

**Moving what is already stored**

- **FR-036**: Every picture the application already stores SHALL be moved under the new arrangement, and every
  recorded value SHALL resolve to its moved file. No picture may be left unresolvable by the move, and none may need
  to be set again by hand.
- **FR-037**: The period a replaced picture is kept before it is cleaned up SHALL be a setting, changeable from the
  same screen as the folders, and SHALL default to ninety days.

**Keeping the kinds apart**

- **FR-038**: The pictures the application stores for its own screens SHALL be kept apart by kind — the request
  items, the work centres and the categories each in their own folder inside the collection — so that no two kinds
  can ever resolve to one file. That SHALL hold for two names that differ only by letter case, such as the item
  `other` beside the category `Other`, without either name being changed.

### Key Entities

- **Part picture**: the picture for one part within one system. Identified by the system and the part number
  together, never by the part number alone.
- **Part family**: the group a part's number puts it in — coil, flatstock, die, component — with one folder each and
  a further folder for a number no family recognises.
- **System folder**: which of the three collections a picture lives in: the request items, the Visual parts, the WIP
  parts.
- **Picture source**: where a picture is read from before any copy is made on the computer.
- **Local copy**: the computer's own copy of a picture, made the first time that part is drawn.
- **Picture change record**: who set or replaced a picture, when, and what it replaced.
- **Storage setting**: the configured picture folder and key folder, both belonging to the organisation.

## Assumptions

- The three collections are: everything the application pictures for its own screens (the request items, the work
  centres and the categories), the Infor Visual parts, and the WIP floor parts.
- **Every picture the application stores moves under this arrangement**, including the pictures stored today. The
  configured picture folder itself does not change; what changes is where each picture sits inside it.
- One picture per part per system. Several pictures for one part is not in scope.
- The existing one-placeholder rule stands. No new artwork is shipped, and a picture per family is explicitly **not**
  taken: the owner confirmed the shared placeholder.
- The acceptance rule for a picture (smallest size, squareness, permitted file types) is the existing one.
- The period a replaced picture is kept is ninety days to begin with, and is a setting rather than a constant.
- The three kinds the application pictures for its own screens — the request items, the work centres and the
  categories — are kept apart by folder, never by renaming a picture or its file (FR-038).

## Open Questions

| # | Question | Why it matters | Current assumption |
|---|---|---|---|
| **OQ-1** | ~~How are the kinds of picture the application stores for itself kept apart inside its own collection?~~ **Answered 2026-09-22**: each kind gets its own folder inside the collection — the request items, the work centres and the categories never share one folder. | It is **FR-038** now, and no longer open. | Closed. |
| **OQ-2** | ~~What is the retention period for a replaced part picture?~~ **Answered 2026-09-22**: ninety days by default, settable on the Settings screen. | It is FR-037 now, and no longer open. | Closed. |
| **OQ-3** | Which of the two disagreeing stored picture folders is the truth — the drive letter seeded in the database, or the network path the application and its settings file carry? | The database value wins today, so a computer reads a drive letter that may not be mapped, and part pictures would inherit that. | The storage-path screen this feature builds is where it gets settled. |
| **OQ-4** | What is the longest part number in real use? | It decides whether the storage column is wide enough before pictures are set and refused. | To be measured against real data before the plan is written. |
| **OQ-5** | The setup job data already carries a per-part picture value that is always empty and is copied forward on every save. What does it mean once part pictures exist? | Two answers for one part is how a card and a page end up disagreeing. | It is left alone and read by nothing; the new part picture is the only answer. |

## Success Criteria

### Measurable Outcomes

- **SC-001**: Every surface this specification names draws a part's picture when the part has one, proved per surface
  by an automated check.
- **SC-002**: No surface draws a blank space where a part's picture belongs; every one of them draws the shared
  placeholder instead.
- **SC-003**: A Visual part and a WIP part with the same number never show each other's picture, proved by setting
  different pictures for both and reading them back on every surface that shows either.
- **SC-004**: Zero part pictures are stored outside their system's folder or their family's folder.
- **SC-005**: No part number is ever silently turned into another part's file name: every collision is refused, and
  every refusal names the part already holding the name.
- **SC-006**: A renewed picture is visible on a surface without a restart, and the picture it replaced is still
  present until its period has passed.
- **SC-007**: A screen opens and draws the shared placeholder when the picture source cannot be reached.
- **SC-008**: The parts that have no picture can be listed and exported in one action.
- **SC-009**: Every list this specification converts still performs the choice it performed before, with the entries
  drawn as cards.
- **SC-010**: The full test suite reports no failures and the solution builds with no warnings and no errors.

## Brainstorm Log

### 2026-09-22 — first session

**Target**: none existed. The feature description was brainstormed into this specification, the seed for the
`specify` → `plan` → `tasks` → `implement` sequence.

**What the owner settled, and why each mattered.**

| Decision | The answer | What it changed |
|---|---|---|
| Which part numbers can be pictured | Every part the application can show — Visual and WIP floor inventory | Made this a new key universe rather than an extension of the 23 request Items, which is what the existing picture store holds |
| One picture or two per part number | **Separate** pictures for the Visual part and the WIP part | Fixed the storage key as system + part number, and made "one picture per part" the wrong shorthand anywhere it appears |
| Folder layout | `Images\{Waitlist, Visual or WIP}\{Categorized Parts, MMC, MMF, …}\MMC0001000.png`; one folder per prefix, with `Categorized Parts` as the catch-all | Recorded verbatim here and turned into FR-004/005/012; belongs to the plan as the literal layout |
| Dunnage pictures | Left alone — a separate world | Removed the largest source of rework: dunnage pictures are not under the picture rule today and stay that way |
| The missing storage-path screen (the previous picture work's one open requirement) | In scope here | Added User Story 6, and the folder-disagreement question with it |
| Who may add a picture | The existing entitlement, widened to **Setup Lead and Plant Manager** | Needed a permission change, not just a screen; the entitlement today covers IT Department and Developer only |
| Replacing a picture | Archived, then cleaned up after a set period | Added FR-011 and the retention open question, because at part scale an archive doubles the file count |
| Unsafe part numbers as file names | Replace the unsafe characters, and refuse on a collision | Chose safety over convenience: two parts may never share one file |

### 2026-09-22 — second pass

Two questions the first pass left open were answered, and one of them made the feature materially larger.

| Decision | The answer | What it changed |
|---|---|---|
| Do today's pictures move under the new arrangement? | **Everything moves** — the request items, the work centres and the categories as well as the parts | Turned a part-picture feature into a re-layout of the whole picture store: FR-004 now names all three kinds in the application's own collection, and **FR-036** requires every already-stored picture to move without becoming unresolvable. It also exposed **OQ-1**: two kinds can carry the same token, and the item `other` beside the category `Other` differ only by letter case, which a file name cannot tell apart |
| How long is a replaced picture kept? | **Ninety days by default, settable in the Settings screen** | Made the period a setting rather than a constant (**FR-037**), so the archive's size is the owner's to bound without a new release |

### 2026-09-22 — third pass

One question was answered, and the answer was the reason it was asked.

| Decision | The answer | What it changed |
|---|---|---|
| How the kinds the application pictures for itself are kept apart (OQ-1) | **One folder per kind** inside the application's own collection — the request items, the work centres and the categories each in their own place | Closed OQ-1 as **FR-038**, and turned the case-only collision into a rule rather than a risk: the item `other` and the category `Other` both keep their names and live apart, instead of one of them being renamed or one overwriting the other. Renaming was rejected deliberately — a picture's name is how a person finds the file by hand, and the bulk-import path in User Story 4 depends on that name being the name they already know |

**Still open.** OQ-3 (which of the two disagreeing picture folders is the truth), OQ-4 (the longest real part number), and
OQ-5 (what the setup job's always-empty per-part picture value means now). None of the three blocks the plan.

**Nice-to-haves the owner chose.** Copy a picture to the computer when the part is first drawn, not all at startup ·
bulk import by dropping correctly named files into the folder · a history of who changed a picture and what it
replaced · an exportable list of the parts still missing a picture.

**Nice-to-haves the owner declined.** A "parts with no picture" worklist with a count (the export covers it) ·
copying a picture from another part (substitutes look alike) · storing a smaller thumbnail beside the original.

**Surfaces the owner chose to gain a picture.** The Setup part list · the Setup review of a job's parts · the
wizard's die card · the wizard's confirmation step · the waitlist card, which switches from the kind-of-request
picture to the part's own picture. **Declined**: the urgency minutes list, the request detail's inventory rows, and
the shell's search results.

**Lists the owner chose to convert to cards.** The dunnage types that may be shown or hidden · the Setup part list ·
the remaining answer list in the wizard · the building choice. For the building choice the owner asked for a flyout
of selectable building cards with the building currently in effect left out of the list.

**Decisions taken against my recommendation, recorded so they are not mistaken for oversights.** Separate pictures
per system (I recommended one shared picture) — it costs a second photograph per part but keeps two systems' truths
apart. Setup Lead and Plant Manager gaining the picture entitlement — it means more people can change what the floor
sees. Replacing a picture with a cleanup period rather than a permanent archive — it bounds the archive at part
scale.

**Grounding facts found and carried into the questions.** Today's pictures are keyed by request-Item code, never by
a part number; the picture store's scopes are request item, request category and work centre; the seeded picture
folder disagrees with the application's own; the only fallback picture is not tracked in the repository, so a fresh
clone has none; dunnage pictures already live on a different share under no size or shape rule; the previous picture
work deliberately forbids built-in artwork standing in for a picture, which is why the family-placeholder idea was
raised as a decision rather than adopted; the setup job data already carries a per-part picture value that is always
empty.
