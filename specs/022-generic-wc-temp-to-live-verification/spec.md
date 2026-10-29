# Feature Specification: Generic Work Center — Temp-to-Live Conversion and Verification

**Feature Branch**: `022-generic-wc-temp-to-live-verification`
**Created**: 2026-10-29
**Status**: Draft
**Input**: Chunk 8 of 8. Chunks 015 to 021 each delete the `Temp` artefacts behind the screens they make real, and each states that deletion as a requirement. Nothing, until now, owned the **end state**: that no `Temp` artefact remains anywhere in the solution, and that each screen actually behaves on its live source the way the owner approved it on preview. Owner decision, 2026-10-29: close that gap with this chunk rather than leave it to the honour system, and verify behaviour as well as absence.

The map of all eight chunks is [BREAKDOWN.md](../015-generic-wc-ui-shell/BREAKDOWN.md). The combined specification is [PARENT-SPEC.md](../015-generic-wc-ui-shell/PARENT-SPEC.md).

## User Scenarios & Testing

### User Story 1 - Nothing temporary is left anywhere (Priority: P1)

A reviewer searches the solution for `Temp` and finds nothing belonging to this feature — no type, no namespace, no project, no resource key, no database object, no file. The preview project itself is either deleted or, if the team keeps it for future layout work, is the only place the word appears and is still unreachable from the shipped application.

**Why this priority**: this is the end state the whole convention existed to reach. Until it is asserted, every earlier chunk's deletion is a promise rather than a fact.

**Independent Test**: search the solution for `Temp`, then build and run the application, and confirm both that nothing remains and that nothing broke.

**Acceptance Scenarios**:

1. **Given** the solution after chunk 021, **When** it is searched for `Temp` across source, XAML, resource files, project files, configuration and database artefacts, **Then** no artefact belonging to this feature matches.
2. **Given** the search, **When** the preview project has been retained for future layout work, **Then** it is the only match and the shipped application still does not reference it.
3. **Given** the solution, **When** any `Temp`-prefixed artefact is reintroduced into the application or its reference graph, **Then** the build fails and names it.
4. **Given** the application after conversion, **When** it is built and run, **Then** it builds, starts, and reaches the board with no missing registration, key or resource.
5. **Given** a `Temp` artefact was removed, **Then** no other edit was needed to keep the build green — no registration, key, reference or test outlived it.

---

### User Story 2 - Each screen behaves on live data the way it was approved on preview (Priority: P1)

The owner reviewed every screen against preview data and approved a layout, a set of states and a set of card cells. This story proves the same screens, now reading live data, still show those same presentations: populated, empty and unavailable; the same cells in the same places; the same wording; and the unavailable state still carrying the time of the attempt and a retry.

**Why this priority**: a conversion that removes the temporary source but quietly changes what the screen shows has satisfied the letter of the previous story and broken the point of the exercise. Absence of fake data is worth nothing if the real data lands in the wrong cells.

**Independent Test**: walk each screen against live data and compare it with the approved preview presentation, screen by screen and cell by cell.

**Acceptance Scenarios**:

1. **Given** a screen approved on preview, **When** it is shown on its live source, **Then** it presents the same populated, empty and unavailable presentations the review approved.
2. **Given** the unavailable presentation, **Then** it still carries the time of the attempt and a retry control.
3. **Given** the generic card, **When** it is drawn from a live request, **Then** it draws the same cells, in the same places, as the approved variant, and no cell the review did not approve.
4. **Given** a screen whose live source returns an awkward real value — a part number containing a slash, a scrap type in lower case, a long work center name, an outside-service part with no quantity — **Then** the layout holds it exactly as the preview scenario proved.
5. **Given** the whole flow on live data, **When** a requester walks it end to end, **Then** every step is reachable by the same controls as in preview and no step is a dead end.
6. **Given** a screen where the live source and the approved preview disagree — a cell the preview drew that the live source cannot supply, or a state the preview never showed — **Then** the disagreement is recorded and resolved before this chunk is called done, rather than shipped.

---

### User Story 3 - A deferred chunk cannot leave temporary material orphaned (Priority: P2)

If chunk 017, 018, 019 or 020 is deferred past this one, its screens still exist and still need an answer. This story fixes what happens: those screens report their answer source as unavailable, exactly as chunk 016 requires, and they do so from a live check rather than from a temporary source left behind. A deferred chunk leaves no `Temp` artefact and no silently wrong screen.

**Why this priority**: the plant may take the chunks one release at a time, and the worst outcome would be a temporary source staying in the build because its chunk slipped.

**Independent Test**: with one chunk deliberately reverted, confirm its screen reports the source unavailable, no `Temp` artefact remains, and every other screen is unaffected.

**Acceptance Scenarios**:

1. **Given** a chunk deferred, **When** its screen is reached, **Then** it reports the answer source as unavailable with the time of the attempt and a retry.
2. **Given** a chunk deferred, **Then** no `Temp` artefact of that chunk remains in the solution.
3. **Given** a chunk deferred, **Then** every other screen behaves exactly as it does with the chunk present.
4. **Given** a deferred screen, **Then** it offers no text box, no offline list and no substitute for the missing answer.
5. **Given** the deferred chunk later ships, **Then** its screen begins working with no other screen changed.

---

### Edge Cases

- The preview project is deleted entirely: the solution, the app and the tests still build and run.
- The preview project is retained: it is excluded from packaging, unreachable from the app, and every one of its artefacts carries the `Temp` naming.
- A `Temp` artefact is found inside the preview project with a permanent-looking name: it is renamed to carry `Temp`, because the preview project must be uniformly identifiable.
- A live source returns a value shape the preview never showed — a null where the preview had a value, or an extra field: the presentation is decided and recorded, not left to chance.
- Removing a `Temp` resource key breaks a binding: the build or the startup check catches it before release, and no screen ships with a missing string.
- A `Temp` database artefact exists in a developer's local store: it is dropped by the chunk's own artifact, and no other store is touched.
- Two chunks convert the same shared artefact: whichever lands last removes it, and the build stays green throughout.

## Requirements

### Functional Requirements

**Absence**

- **FR-001**: After this chunk the solution MUST contain no `Temp` artefact belonging to this feature, checked across source code, XAML, resource files, project files, configuration and database artefacts.
- **FR-002**: Where the preview project is retained, it MUST be the only place the `Temp` naming appears, and it MUST remain unreachable from the shipped application, directly or transitively.
- **FR-003**: A build-time check MUST fail, naming the offending artefact, if any `Temp` artefact appears in the reported scope for this feature.
- **FR-004**: The check's exemptions MUST be listed one at a time, never by pattern, so no exemption can widen without being read.
- **FR-005**: The application MUST build, start and reach the board after every removal, with no registration, resource key, reference or test left dangling.
- **FR-006**: No `Temp` artefact MAY be renamed into a permanent-looking name in place of being replaced by a live source or deleted.
- **FR-007**: Any `Temp` database artefact created during the feature MUST be dropped by its own artifact, in the same change that removes the code depending on it, without touching any other store.

**Behaviour**

- **FR-008**: Each screen MUST present, on its live source, the same populated, empty and unavailable presentations the owner approved on preview.
- **FR-009**: The unavailable presentation MUST carry the time of the attempt and a retry control on every screen, as approved.
- **FR-010**: The generic card MUST draw, from a live request, the same cells in the same places as the approved variant, and no cell the review did not approve.
- **FR-011**: Awkward real values MUST render exactly as the preview scenarios proved them: a part number containing a slash, a scrap type in lower case, a long work center name, an outside-service part with no quantity.
- **FR-012**: The whole flow MUST remain walkable end to end on live data, by the same controls, with no dead end.
- **FR-013**: Every disagreement between the approved preview and the live presentation MUST be recorded and resolved before this chunk is complete, and MUST NOT be shipped as an unrecorded difference.
- **FR-014**: Every screen's verification MUST be recorded individually, naming the screen, the presentations compared and the outcome, so the review can be audited rather than remembered.

**Deferral**

- **FR-015**: A chunk that has not shipped MUST leave its screen reporting the answer source as unavailable, from a live check rather than from a retained temporary source.
- **FR-016**: A deferred chunk MUST leave no `Temp` artefact and MUST NOT change any other screen's behaviour.
- **FR-017**: A deferred chunk, when it later ships, MUST make its screen work without any other screen being edited.

### Key Entities

- **Temp artefact**: anything that carried the `Temp` naming during chunks 015 to 021 — a preview type, namespace, project, resource key or database object.
- **Screen verification record**: the per-screen evidence that the live presentation matches the approved preview presentation, naming what was compared.
- **Recorded disagreement**: a difference between an approved preview and the live presentation, with its resolution, kept so the decision is auditable.
- **Deferred screen**: a screen whose owning chunk has not shipped, presenting its answer source as unavailable from a live check.

## Success Criteria

### Measurable Outcomes

- **SC-001**: One search for `Temp` returns no artefact of this feature, or returns only the retained preview project.
- **SC-002**: The build fails, naming the artefact, whenever a `Temp` artefact is reintroduced anywhere the application can reach — verified by deliberately doing so once.
- **SC-003**: Every screen in the flow has a verification record showing its live presentation matches the approved preview presentation.
- **SC-004**: Zero unrecorded differences between the approved preview and the shipped presentation remain at completion.
- **SC-005**: The application builds, starts and reaches the board after the last removal, with zero missing registrations, keys or resources.
- **SC-006**: With any one of chunks 017 to 020 deferred, its screen reports the source unavailable and no `Temp` artefact survives it.
- **SC-007**: No artefact was renamed from `Temp` into a permanent-looking name in place of being replaced or deleted.

## Scope & Dependencies

**Builds on**: spec 015 (the naming rule and the guard this chunk tightens to "zero") and specs 016 to 021 (the screens, sources and deletions it verifies). It needs every one of them, which is why it is last.

**Nothing depends on this chunk**: a deferred 017, 018, 019 or 020 does not block 021, and none of 015 to 021 needs this chunk to be correct — it needs this chunk to be *proved*.

**Does not change behaviour**: this chunk removes, verifies and records. Where it finds a live presentation that differs from the approved preview (FR-013), the difference is resolved in the chunk that owns that screen, not here.

## Assumptions

- **A1**: The owner's approval of each screen's presentation is recorded from the chunk 015 review, so "the approved presentation" is a fact to compare against rather than a memory.
- **A2**: The preview project may be kept after conversion for future layout work, provided it stays unreachable from the application and uniformly `Temp`-named; deleting it entirely is equally acceptable and satisfies the same criteria.
- **A3**: A deferred chunk's screen already has a correct unavailable presentation, because chunk 016 requires it from the day that chunk ships.
- **A4**: The verification records live with the feature's documentation rather than in the application, since they are evidence for a reviewer and not a runtime concern.
