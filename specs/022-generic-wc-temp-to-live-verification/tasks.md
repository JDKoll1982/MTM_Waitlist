# Tasks: Generic Work Center — Temp-to-Live Conversion and Verification

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md) | **Size**: normal

No Setup phase and no Foundational phase: this chunk adds no configuration, no shared type and no seam.
The guard already exists and the record is a document, so the work starts at the first user story.

## Phase 3: User Story 1 — Nothing temporary is left anywhere (P1)

**Files**: `MTM_Waitlist.Tests/Module_GenericRequest/TempArtefactBoundaryAuditTests.cs`

**Wave 1 — independent:**

- [x] **T001** [P] [US1] Enumerate every project the solution file names as the audit's scope, instead of
  walking the application's reference graph · `MTM_Waitlist.Tests/Module_GenericRequest/TempArtefactBoundaryAuditTests.cs`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T002** [US1] Add a name-level exemption form (`path` plus declared name) beside the existing path
  form, so one unrelated fixture can be exempted without exempting its whole file ·
  `MTM_Waitlist.Tests/Module_GenericRequest/TempArtefactBoundaryAuditTests.cs`

**⟶ Wait for Wave 2 to finish, then:**

- [x] **T003** [US1] Record the three exemptions the widened scan needs, each with its written reason: this
  audit's own file, `TempRootFixture` in the service capability tests, and the temporary-credential test
  file · `MTM_Waitlist.Tests/Module_GenericRequest/TempArtefactBoundaryAuditTests.cs`

**⟶ Wait for Wave 3 to finish, then:**

- [x] **T004** [US1] Keep the deliberate-offence case asserting the failure names the file and the
  identifier, and the case asserting an unrelated constant is not flagged ·
  `MTM_Waitlist.Tests/Module_GenericRequest/TempArtefactBoundaryAuditTests.cs`

**Checkpoint**: the guard reports zero offences across the whole solution and fails by name when a
temporary artefact appears in any project, not only in one the application references.

## Phase 4: User Stories 2 and 3 — Each screen behaves as approved (P1), and a deferred chunk leaves nothing orphaned (P2)

**Files**: `specs/022-generic-wc-temp-to-live-verification/verification.md`

Both stories record into the one document a reviewer reads, so the whole phase owns that one file. That
keeps the file rule honest: one writer, no fan-out.

**Wave 1 — independent:**

- [x] **T005** [P] [US2] Write the record with one entry per screen — work center, category, item, part,
  dunnage, scrap, die, details, summary, result — each naming the live read behind it and the three
  presentations compared · `specs/022-generic-wc-temp-to-live-verification/verification.md`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T006** [US2] Fill each entry's evidence with the tests that already prove its populated, empty and
  unavailable presentations, including the attempt time and the retry ·
  `specs/022-generic-wc-temp-to-live-verification/verification.md`

**⟶ Wait for Wave 2 to finish, then:**

- [x] **T007** [US2] Record each entry's awkward-value case: part number with a slash, lower-case scrap
  type, long work center name, outside-service part with no quantity ·
  `specs/022-generic-wc-temp-to-live-verification/verification.md`

**⟶ Wait for Wave 3 to finish, then:**

- [x] **T008** [US2] Record each entry's outcome as `matches` or a disagreement with the chunk that owns its
  correction, so no difference ships unrecorded ·
  `specs/022-generic-wc-temp-to-live-verification/verification.md`

**⟶ Wait for Wave 4 to finish, then:**

- [x] **T009** [US3] Add the deferred-screen section: which chunks shipped, what a deferred screen would
  show, and the tests proving a screen whose source is unreachable offers the unavailable presentation and
  no substitute · `specs/022-generic-wc-temp-to-live-verification/verification.md`

**⟶ Wait for Wave 5 to finish, then:**

- [x] **T010** [US2] Add the closing section naming what a later reader runs to re-prove the record: the
  guard test, the full suite, and the intended `Temp` search ·
  `specs/022-generic-wc-temp-to-live-verification/verification.md`

**Checkpoint**: a reviewer can open one document and see, screen by screen, what was compared, what proved
it, and that no difference is outstanding.

## Phase 5: Polish

**Files**: none. This phase runs the checks and reports; the document is written by Phase 4.

- [x] **T011** [P] Validate against the success criteria: search the solution for `Temp`, build
  `MTM_Waitlist.sln`, run `MTM_Waitlist.Tests`, and confirm the guard's deliberate-offence test still names
  what it finds · `MTM_Waitlist.sln`

## Dependencies & Execution Order

Phase 3 runs first because its change is the only one that can break the build, and Phase 5's build is
meaningless until it lands. Phase 4 depends on Phase 3 only in reading order: the record's closing section
names the guard test that Phase 3 finishes. Phase 5 depends on both.

Waves: T001 → T002 → T003 → T004 (one file, so one task at a time); T005 → T006 → T007 → T008 → T009 → T010
(the same, in the record); T011 last, alone.
