# Feature Specification: Generic Work Center — Screen Layer and Preview Host

**Feature Branch**: `015-generic-wc-ui-shell`
**Created**: 2026-10-29
**Status**: Draft
**Input**: Owner request, 2026-10-29: "I want this group of specs to first create the base UI elements with mock data derived from a mock model so I can easily see the layout before you dig into actual db reads and logic coding." This is chunk 1 of 8 carved from the feature specification kept beside this file at [PARENT-SPEC.md](./PARENT-SPEC.md); the plain-language map across all eight chunks is [BREAKDOWN.md](./BREAKDOWN.md).

This chunk delivers **every screen and both cards of the generic request feature, fully laid out and navigable, rendering from an in-memory preview host** — with no database read, no queue write and no permission check anywhere. Its whole purpose is to let the owner review layout, wording, spacing and card content before any data work is written.

**A constraint that shapes the design.** This repository's constitution (Principle II) and `RetiredSymbolAuditTests` forbid a demo/mock mode inside the application: `ISampleDataService`, `SampleDataService`, `Sample*Catalog`, `MockToggle*`, `MockMode*`, `MockRouting*`, `MockMasterDataService`, `UseMockData` and the retired feature keys all fail the build. Preview data therefore cannot be a mode of the shipping app. It is delivered instead as a **separate preview host project** that the shipping app does not reference and cannot reach, supplying the same seams the real feature will use. The owner gets the same review experience; the application stays truthful.

**Everything temporary is named `Temp`** — the owner's convention, adopted here as a rule rather than a habit: every type, namespace, file, control name, resource key and database object that stands in for data the real feature will supply carries `Temp` as its first segment. That makes the temporary material findable by one search, and it is what lets the guard below be enforced instead of hoped for. The exact shapes are fixed in FR-021 to FR-026, because this repository's naming guard allows `Temp` in a PascalCase type but not a `temp_` prefix on one.

## User Scenarios & Testing

### User Story 1 - Every screen of the generic flow can be walked before any data work exists (Priority: P1)

The owner runs one command, a window opens, and every screen of a generic request is there: pick a work center, pick a category, pick an item, state a part and choose a sequence, choose dunnage from the catalogue page, state a scrap type, search for a die, give a needed-by time, review, and see the result. Each screen is populated as if a real person had filled it. Nothing is read from a database and nothing is saved. Every screen can also be seen in its empty and its unavailable state, so the reviewer can judge those too.

**Why this priority**: it is the entire point of this chunk. Every later chunk replaces one screen's preview data with a real read; without this, layout decisions would be made while the data work is being debugged, which is the order the owner asked to avoid.

**Independent Test**: run the preview host with no database reachable at all, walk every screen in order and back again, and confirm each renders populated, empty and unavailable without an error dialog.

**Acceptance Scenarios**:

1. **Given** the preview host is started with no database configured, **When** the reviewer walks the flow, **Then** every screen renders and no step reports a connection failure.
2. **Given** a screen is showing, **When** the reviewer chooses a preview scenario for that screen, **Then** it renders the populated, the empty and the unavailable presentation in turn without restarting.
3. **Given** the reviewer walks forward from the work center step to the result step, **When** they use only the on-screen buttons, **Then** every step is reachable and no step is a dead end.
4. **Given** the reviewer is on any step, **When** they go back, **Then** the answers they already gave are still there.
5. **Given** an item is chosen that needs a part, dunnage, scrap or die answer, **When** the flow reaches that step, **Then** the matching screen is the one shown.
6. **Given** the reviewer opens the preview on a screen with long part numbers, long work center names and multi-line descriptions, **When** it renders, **Then** nothing is clipped, overlapped or truncated without an ellipsis.

---

### User Story 2 - The generic card and the work-order card can be compared side by side (Priority: P1)

The owner looks at both cards in one list. The generic card shows only the facts a generic request has; the work-order card is unchanged. They can tell them apart at a glance, and they can see the generic card in its part, dunnage, scrap and die variants, because each request type draws different cells.

**Why this priority**: the card is what handlers read all day and the only surface most of them ever see. It is cheaper to argue about it now, from preview data, than after the queue write exists.

**Independent Test**: open the card preview, confirm the generic and work-order cards appear in one list, distinguishable, and that each generic variant draws only its own cells with no blank or placeholder cell.

**Acceptance Scenarios**:

1. **Given** the card preview is open, **When** it renders, **Then** a generic card and a work-order card appear in one list and are distinguishable without opening either.
2. **Given** the generic card preview, **When** its variant is changed to part, dunnage, scrap and die in turn, **Then** each variant draws its own cells and no cell belonging to another request type.
3. **Given** any generic card in preview, **Then** no cell is drawn blank or with placeholder text, and no work-order value appears.
4. **Given** the generic detail page in preview, **When** it is opened, **Then** it shows the card's facts plus the requester, the time raised and a history area.

---

### User Story 3 - The shipping application carries no preview code (Priority: P2)

The owner, or a reviewer, checks that the shipped application can neither reach nor display preview data: the app does not reference the preview host, builds and runs without it, and the audit that forbids sample-and-mock symbols still passes. Every temporary thing is named so that one search finds all of it, and the build fails if any of it appears where the app can reach.

**Why this priority**: without it the whole chunk is a constitutional violation, however useful the preview is. It is P2 only because it is a guard rather than a feature.

**Independent Test**: delete the preview host project from the solution, build the application, and confirm it compiles and runs; then run the audits and confirm they pass, then deliberately add a `Temp` type to the application and confirm the build fails.

**Acceptance Scenarios**:

1. **Given** the shipped application, **When** its project references are inspected, **Then** the preview host is not among them, directly or transitively.
2. **Given** the preview host is removed from the solution, **When** the application is built and run, **Then** it builds and runs unchanged.
3. **Given** the preview host exists, **When** the retired-symbol audit runs, **Then** it passes — no forbidden sample or mock identifier appears anywhere.
4. **Given** the preview host, **When** it is run, **Then** it opens no database connection and writes to no store.
5. **Given** any artefact that stands in for data the real feature will supply, **When** its name is inspected, **Then** it begins with `Temp` in the shape FR-021 fixes for its kind, so one search lists every temporary thing.
6. **Given** a `Temp`-named type, file, resource key or database object placed inside the shipping application, **When** the build runs, **Then** it fails with a message naming the offending artefact.
7. **Given** the real implementation of one seam has landed, **When** its `Temp` counterpart is removed, **Then** nothing else needs editing to make the build pass.

---

### Edge Cases

- A screen's preview scenario is switched while that screen is mid-animation: the new scenario replaces the content without leaving two states drawn at once.
- The preview is resized to the narrowest supported width and to a very wide monitor: every screen adapts and no control is pushed out of reach.
- The preview is run on a machine with no Infor Visual, no WIP store and no waitlist store configured: it still starts and walks, because it reads nothing.
- A preview scenario supplies a value with characters the plant actually uses — a part number with a slash, a scrap type in lower case, `5052 aluminum` — and the layout holds it.
- The reviewer leaves the preview on a screen that shows an unavailable state and switches scenario to populated: the retry control disappears with the state, not beside it.
- The design system's theme is switched to dark while previewing: every screen and both cards remain legible.

## Requirements

### Functional Requirements

**Structure**

- **FR-001**: The flow's views MUST be built as their own projects, separate from the work-order wizard, so no existing screen is modified to host them.
- **FR-002**: The generic card and its detail page MUST be built as their own project, separate from the existing card controls.
- **FR-003**: The preview host MUST be a separate project that the shipped application does not reference, directly or transitively.

**Screens**

- **FR-004**: Every step of a generic request MUST exist as a laid-out view: work center, category, item, part with its WIP/finished-good stage and sequence, dunnage catalogue page, scrap type, die search results, details including needed-by and description, summary, and result.
- **FR-005**: Each view MUST be bound to a view model that exposes observable state and commands; no view MUST read a database, a service or a file directly.
- **FR-006**: Each view MUST render a populated, an empty and an unavailable presentation, and the unavailable presentation MUST carry the time of the attempt and a retry control.
- **FR-007**: Long values MUST be accommodated without overlap or clipping: names, part numbers and descriptions wrap or ellipsize deliberately.
- **FR-008**: Every screen MUST adapt from the narrowest supported width to a wide monitor without a control leaving the viewport.
- **FR-009**: Every user-visible string MUST come from a resource key, and interactive elements MUST carry an automation name.

**Seams and preview data**

- **FR-010**: Every data need of the flow MUST be expressed as an interface declared outside the application, so a preview implementation and a live implementation can be swapped without touching a view.
- **FR-011**: The preview host MUST supply those interfaces from in-memory scenarios, so every screen renders without a database, a network call or a queue write.
- **FR-012**: The preview host MUST offer the reviewer a way to choose a screen, a variant and a scenario, so each presentation can be inspected on demand.
- **FR-013**: The preview host MUST NOT connect to any store, write to any store, or begin a request submission.
- **FR-014**: Preview scenarios MUST be able to present the awkward real values — a part number containing a slash, a scrap type in lower case, a long work center name, an outside-service part with no quantity — so the layout is proved against them.

**Cards**

- **FR-015**: The generic card MUST draw its own layout and MUST NOT draw work-order, die, coil, flatstock or component cells.
- **FR-016**: The generic card MUST be previewable in its part, dunnage, scrap and die variants.
- **FR-017**: Generic and work-order cards MUST be viewable in one list and MUST be distinguishable without opening either.
- **FR-018**: No generic card MUST draw a blank or placeholder cell for a fact the request does not have.

**Guard**

- **FR-019**: No runtime switch, setting, feature key or fallback path MAY select preview data, and the identifiers the retired-symbol audit forbids MUST remain absent.
- **FR-020**: The shipped application MUST build and run with the preview host absent from the solution.

**Naming and boundary of temporary things**

- **FR-021**: Every artefact that stands in for a value the real feature will supply MUST begin with `Temp` in the shape its kind allows: a type, interface or enumeration as a PascalCase `Temp` prefix (`TempGenericRequestPartSource`, `ITempGenericRequestPartSource`); a namespace as a trailing `.Temp` segment; a project, folder or file with `Temp` as its first segment; an `x:Name` as a PascalCase `Temp` prefix; a resource key with `Temp` as its feature segment (`TempGenericRequest_Step.Title`); a database object with a `temp_` prefix. The prefix MUST NOT be `temp` in lower case on a C# identifier, because the repository's naming guard requires PascalCase for types and would fail the build.
- **FR-022**: The preview host's own project, assembly and root namespace MUST carry the `Temp` naming, so that everything temporary is confined to one identifiable assembly.
- **FR-023**: A build-time check MUST fail, naming the offending artefact, if any `Temp`-prefixed type, namespace, resource key, file or database object appears inside the shipped application or anywhere in its reference graph.
- **FR-024**: The check MUST allow a `Temp` artefact inside the preview host's own project and inside test code, and MUST list any other exemption explicitly rather than by pattern, so an exemption cannot silently widen.
- **FR-025**: The check MUST NOT rely on the words the retired-symbol audit forbids: the temporary material is identified by the `Temp` convention, and it MUST NOT be described or named as sample data or mock data anywhere.
- **FR-026**: Removing a `Temp` artefact MUST require no other edit to keep the build green — no registration, key, reference or test may outlive it — so that the real implementation replaces it by deletion rather than by renaming.

### Key Entities

- **Generic request screen**: one step of the flow, with its own view and view model, previewable in three presentations.
- **Preview scenario**: a named set of in-memory values a screen can render, including the awkward real values.
- **Seam**: an interface, declared outside the application, through which a view model obtains data — preview now, live later.
- **Generic request card variant**: the part, dunnage, scrap or die arrangement of the card, each drawing only its own cells.
- **Temp-named artefact**: anything standing in for a value the real feature will supply — a preview type, a preview project, a preview resource key or an interim database object — identified solely by the `Temp` convention so one search finds all of it.

## Success Criteria

### Measurable Outcomes

- **SC-001**: The owner can walk every screen of the flow and see both cards, with no database configured, without a single error dialog.
- **SC-002**: Every screen and both cards are reviewable in populated, empty and unavailable presentations, in under one minute each from a cold start.
- **SC-003**: Zero database connections are opened by the preview host, verified by observation and by the absence of any store dependency in it.
- **SC-004**: The shipped application builds and runs with the preview host removed from the solution.
- **SC-005**: The retired-symbol audit and the seam audit both pass unchanged.
- **SC-006**: Zero hard-coded user-visible strings in the new views; every one resolves from a resource key.
- **SC-007**: One search for `Temp` lists every temporary artefact in the solution, with nothing temporary left unlisted.
- **SC-008**: Adding a temporary artefact anywhere the shipped application can reach fails the build with a message naming it, verified by deliberately doing so once.

## Scope & Dependencies

**Builds on**: nothing in this feature. It uses the shipped design system (spec 014) and the existing card conventions (spec 012) as they stand.

**Deliberately absent from this chunk**: every database read, every queue write, the work center designation, permission checks, the real part/sequence lookup, the real dunnage catalogue, the real scrap list and the real die search. Each arrives in the chunk that owns it, and this chunk's screens are the place they will land.

**Later chunks depend on this one** — 016 through 022 all reuse these views, and each of 016 to 021 deletes the `Temp` artefact its own work replaces. No later chunk may be needed for these screens to be reviewable.

## Assumptions

- **A1**: The preview host is a development tool, not a shipped surface; it is excluded from packaging and from the application's reference graph.
- **A2**: The preview host may live in the same solution and may be run from the developer's machine; it is not installed on a plant workstation.
- **A3**: A reviewer accepts running one command to open the preview, rather than reaching it from inside the app.
- **A4**: The seam interfaces are declared in a project both the app and the preview host can reference — the same arrangement `IDunnageSubstitutePicker` already uses in Settings. The interfaces themselves are real, so they carry no `Temp` prefix; only their preview implementations do.
- **A5**: Layout review happens against the design system the app already ships; this chunk introduces no new visual language.
- **A6**: "Easy conversion later" is achieved by deletion: the real implementation lands beside a `Temp` artefact, the `Temp` artefact is removed, and no rename is needed. This is why FR-026 forbids anything from outliving it.
