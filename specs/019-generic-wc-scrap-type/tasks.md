# Tasks: Generic Work Center — Stated Scrap Type and Lugger

Input: [spec.md](./spec.md), [plan.md](./plan.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/scrap-answer-and-card.md](./contracts/scrap-answer-and-card.md)

The scrap step, its page and its route already exist and are wired. This list fills the source, gates the choice, keeps the answer and makes the card say where the scrap goes.

---

## Phase 1: Setup

Files: `Strings/en-us/Resources.resw`

**Wave 1 — independent (different files):**

- [x] **T001** [P] Add the card's lugger-unavailable wording as `Waitlist_Card_LuggerUnavailable.Text` so the card reports an unreadable arrangement instead of a raw key · `Strings/en-us/Resources.resw`

---

## Phase 2: Foundational

Files: `MTM_Waitlist.Core/Models/GenericRequestScrapAnswer.cs`, `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestScrapAnswerTests.cs`

Blocks every story: the flow writes this record and the card reads it.

**Wave 1 — independent (different files):**

- [x] **T002** [P] Add `GenericRequestScrapAnswer` in namespace `MTM_Waitlist.Module_Core.Models` with `ScrapTypeKey = "scrap.type"`, `ToAnswers()` writing only a non-blank type into a case-insensitive dictionary, and `TryRead()` returning null when no type is stated, copying the shape of `GenericRequestPartAnswer` · `MTM_Waitlist.Core/Models/GenericRequestScrapAnswer.cs`
- [x] **T003** [P] Cover the record: a blank type writes no key, a stated type reads back trimmed, a missing key reads null, and a key stored under different casing still reads · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestScrapAnswerTests.cs`

---

## Phase 3: User Story 1 — The requester states the scrap type (P1)

Files: `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestScrapTypeViewModel.cs`, `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestWorkCenterViewModel.cs`, `MTM_Waitlist.GenericRequest/Services/GenericRequestLiveStepData.cs`, `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestScrap{TypeViewModel,Options,Submission}Tests.cs`

**Goal**: entering the scrap step asks which scrap type it is, offers the plant's one list, and will not advance without a choice.
**Independent Test**: raise a generic scrap request and confirm the choices match the shared list in its own order, that the two no-scrap answers are absent, and that Continue stays disabled until a type is picked.
**Checkpoint**: the stated type reaches the confirmation step and the stored request.

### Tests

- [x] **T004** [US1] FR-002, FR-003: assert the offered types are the shared list in its own order with arrangement-only types appended, and that neither `No Scrap` nor `Scrap Type Required` appears, including when the arrangement records one of them · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestScrapOptionsTests.cs`
- [x] **T005** [US1] FR-004: assert Continue is disabled before a type is chosen and enabled after, and that it is disabled again when the seam reports unavailable · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestScrapTypeViewModelTests.cs`
- [x] **T006** [US1] FR-001, FR-005: assert the raised request records the chosen type under `scrap.type` and that raising it never asked a job for a scrap decision · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestScrapSubmissionTests.cs`

### Implementation

**Wave 1 — independent (different files):**

- [x] **T007** [P] [US1] Gate the step on a choice: override `CanAnswer` to require a selected option, notify the command when the selection changes, and match the restored choice to the draft ignoring surrounding spaces and case · `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestScrapTypeViewModel.cs`
- [x] **T008** [P] [US1] Clear the stated scrap type beside the four item requirements when the work center changes, so a type stated for one work center never rides into another · `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestWorkCenterViewModel.cs`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T009** [US1] Build the real choices: shared list in its own order, arrangement-only types appended and matched ignoring case, both no-scrap names filtered out, each option carrying the count of that type's luggers that can receive material; and stop reporting the Scrap step unavailable while leaving the Die step as it is · `MTM_Waitlist.GenericRequest/Services/GenericRequestLiveStepData.cs`

**⟶ Wait for Wave 2 to finish, then:**

- [x] **T010** [US1] Record the stated type on the raised request through the new answer record, beside the part and dunnage answers · `MTM_Waitlist.GenericRequest/Services/GenericRequestLiveStepData.cs`

---

## Phase 4: User Story 2 — The card says where the scrap goes (P1)

Files: `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs`, `MTM_Waitlist.Waitlist.View/Models/WaitlistRequestTitles.cs`, `MTM_Waitlist.Tests/Module_Waitlist/WaitlistViewViewModelScrapCardTests.cs`, `MTM_Waitlist.Tests/Module_Waitlist/WaitlistRequestTitlesScrapTests.cs`

**Goal**: a generic scrap card names the stated type and the lugger the arrangement assigns to it now.
**Independent Test**: raise generic scrap requests for several types and confirm each card names the lugger the arrangement assigns, follows the arrangement to a backup when the lugger is marked full, and never draws blank where a lugger belongs.
**Checkpoint**: the card stands on its own, with no job read anywhere in its path.

### Tests

- [x] **T011** [US2] FR-006, FR-007: assert a generic scrap card names the type and the same lugger a work-order card names for that same type · `MTM_Waitlist.Tests/Module_Waitlist/WaitlistViewViewModelScrapCardTests.cs`
- [x] **T012** [US2] FR-008: assert a type whose luggers are all blocked draws the type and the first lugger in the arrangement's order with its status beside the name · `MTM_Waitlist.Tests/Module_Waitlist/WaitlistViewViewModelScrapCardTests.cs`
- [x] **T013** [US2] FR-009, FR-010: assert a type with no lugger assigned draws the type and records the gap with no lugger, and that an arrangement that could not be read draws the type and reports the lugger unavailable rather than a stale one · `MTM_Waitlist.Tests/Module_Waitlist/WaitlistViewViewModelScrapCardTests.cs`
- [x] **T014** [US2] FR-005: assert the card's second line carries the stated type where the Item's identifier names the scrap type, so the card names it in both places · `MTM_Waitlist.Tests/Module_Waitlist/WaitlistRequestTitlesScrapTests.cs`

### Implementation

**Wave 1 — independent (different files):**

- [x] **T015** [P] [US2] Read the lugger arrangement once per list load and resolve a generic scrap request's two cells from it: `Scrap Type` from the stored answer, `Lugger Location` from the shared resolution, with the three outcomes separated by whether the arrangement holds rows · `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs`
- [x] **T016** [P] [US2] Fill the `{scrap_type}` token from the request's stored answer when there is no job to read it from, the way `{dunnage_part}` is already filled · `MTM_Waitlist.Waitlist.View/Models/WaitlistRequestTitles.cs`

---

## Phase 5: User Story 3 — The stated type survives the arrangement changing (P2)

Files: `MTM_Waitlist.Tests/Module_Waitlist/WaitlistViewViewModelScrapChangeTests.cs`

**Goal**: a request keeps the wording the requester stated, and reports a gap rather than an old lugger when its type is edited or removed.
**Independent Test**: raise a generic scrap request, remove its type from the arrangement, and confirm the card still names the stated type and reports the lugger unassigned.
**Checkpoint**: nothing is rewritten behind the requester's back, and a reassignment is followed because the dump point is read live.

### Tests

- [x] **T017** [US3] FR-011: assert a type removed from the arrangement, and a type renamed in it, both leave an existing request's stated wording unchanged · `MTM_Waitlist.Tests/Module_Waitlist/WaitlistViewViewModelScrapChangeTests.cs`
- [x] **T018** [US3] FR-012: assert a type reassigned to a different lugger draws the lugger assigned now · `MTM_Waitlist.Tests/Module_Waitlist/WaitlistViewViewModelScrapChangeTests.cs`
- [x] **T019** [US3] FR-013: assert a stored request carrying a type outside the shared list still draws with that type named · `MTM_Waitlist.Tests/Module_Waitlist/WaitlistViewViewModelScrapChangeTests.cs`

### Implementation

**Wave 1 — independent (different files):**

- [x] **T020** [P] [US3] Confirm the three survival tests pass with no production change. Storing the type and resolving the lugger live is what makes them hold; if one fails, fix the cause in the card path rather than weakening the test · `MTM_Waitlist.Tests/Module_Waitlist/WaitlistViewViewModelScrapChangeTests.cs`

---

## Phase 6: Polish

**Wave 1 — independent (different files):**

- [x] **T021** [P] Validate against Success Criteria: run `dotnet build MTM_Waitlist.sln` and `dotnet test MTM_Waitlist.Tests\MTM_Waitlist.Tests.csproj`, confirming SC-002 (no generic scrap card drawn without a type or with a blank lugger), SC-003 (generic and work-order cards name the identical lugger for a type), SC-004 (blocking a lugger moves the card to its backup) and SC-005 (no stated type is lost when the arrangement changes)
- [x] **T022** [P] Confirm no `Temp` artefact returned and the boundary audit still passes (FR-014) · `MTM_Waitlist.Tests/Module_GenericRequest/TempArtefactBoundaryAuditTests.cs`

---

## Dependencies & Execution Order

Setup → Foundational → US1 → US2 → US3 → Polish.

- T001 is independent. T002 and T003 are independent of each other but both precede every story.
- US1: wave 1 (T007, T008) in any order; ⟶ wait; wave 2 (T009); ⟶ wait; wave 3 (T010). The tests T004–T006 are written first and fail until the wave that satisfies them lands.
- US2 waits for US1, because the card reads the answer US1 stores. Wave 1 (T015, T016) is two different files and runs in any order.
- US3 waits for US2, because all three survival tests exercise the card.
- Polish runs last; T021 and T022 are independent.
