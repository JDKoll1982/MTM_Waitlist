# Tasks: Generic Work Center — Die Search

**Scale note**: 31 tasks across five areas — a checked-in Infor Visual script, the die read and its seam, the die step's view model, the die page, and the confirmation/result/card path — with six test files. Watch the order: nothing renders until the Foundational phase has the script, the read and the draft field in place.

Input: [spec.md](./spec.md), [plan.md](./plan.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/)

The die page, its view model and its route already exist and are wired; the seam answers `Unavailable` today. This list gives the step a real source, makes the results complete and readable, and raises one request per chosen die.

---

## Phase 1: Setup

Files: `MTM_Waitlist.GenericRequest/Strings/en-us/Resources.resw`

**Wave 1 — independent (different files):**

- [x] **T001** [P] Add the die step's wording: a match count line, the "No location recorded" line, the "Choose at least one die" refusal, a raised-count line for the result step, and reword `GenericRequest_Die_Empty.Text` and the search placeholder so the empty line names Infor Visual and the box accepts a die number or a part number · `MTM_Waitlist.GenericRequest/Strings/en-us/Resources.resw`

---

## Phase 2: Foundational

Files: `Database/InforVisual/Queues/Module_Waitlist/Queries/GetDieSearch.sql`, `MTM_Waitlist.Core/Models/GenericRequestDieAnswer.cs`, `MTM_Waitlist.GenericRequest/Contracts/IGenericRequestDieData.cs`, `MTM_Waitlist.GenericRequest/Models/GenericRequestOptions.cs`, `MTM_Waitlist.GenericRequest/Models/GenericRequestDraft.cs`, `MTM_Waitlist.GenericRequest.Data/Services/GenericRequestDieData.cs`, `MTM_Waitlist.GenericRequest/Services/GenericRequestLiveStepData.cs`, `Services/DependencyInjection/ServiceRegistrationExtensions.cs`, `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestTestDoubles.cs`, `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestDieAnswerTests.cs`, `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestDieDataTests.cs`

Blocks every story: the read, the value the flow keeps, and the raise path are what the screens and the card are built on.

**Wave 1 — independent (different files):**

- [x] **T002** [P] Write the die search script: the term matched exactly as a die number, the `PART_ALTERNATE` dies of the term's part, and the `FGT` requirements of that part's work orders; one row per die with `DieNumber`, `Description`, `Location`, `Association`; location from `PART_SITE.PRIMARY_LOC_ID` then the lowest `PART_LOCATION.LOCATION_ID`; ordered die, then explicit, then history, then by number · `Database/InforVisual/Queues/Module_Waitlist/Queries/GetDieSearch.sql`
- [x] **T003** [P] Add `GenericRequestDieAnswer` to `MTM_Waitlist.Module_Core.Models` with `die.number` and `die.location`, `ToAnswers()` writing only what is stated, and `TryRead()` answering null when no die is stated and reading the keys case-insensitively · `MTM_Waitlist.Core/Models/GenericRequestDieAnswer.cs`
- [x] **T004** [P] Add the die read's own seam: `ReadDiesAsync(string searchText, CancellationToken)` answering `GenericRequestStepResult<GenericRequestDieOption>` · `MTM_Waitlist.GenericRequest/Contracts/IGenericRequestDieData.cs`
- [x] **T005** [P] Give `GenericRequestDieOption` the `LocationText` the row draws and the `IsSelected` flag a choice needs, keeping `DieNumber`, `Description` and `Location` as the read answered them · `MTM_Waitlist.GenericRequest/Models/GenericRequestOptions.cs`
- [x] **T006** [P] Keep every chosen die: `SelectedDies` on the draft, in the order they were chosen, with `DieNumber` staying the first one for the screens that already read it · `MTM_Waitlist.GenericRequest/Models/GenericRequestDraft.cs`
- [x] **T007** [P] Give the step stub a settable die read so a test can say whether a search found dies, found none, or could not be answered · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestTestDoubles.cs`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T008** [P] Build the live die read: normalise the term with `PartMatchSearchInput`, run the script through `IVisualQueryExecutor`, map each row to an option, and answer `Empty` for a completed read with no rows and `Unavailable` for a source that could not be asked or a read that failed · `MTM_Waitlist.GenericRequest.Data/Services/GenericRequestDieData.cs`
- [x] **T009** [P] Register `IGenericRequestDieData` against `GenericRequestDieData` in the composition root beside the part read · `Services/DependencyInjection/ServiceRegistrationExtensions.cs`

**⟶ Wait for Wave 2 to finish, then:**

- [x] **T010** [P] FR-011: assert a blank die writes no key, a stated die reads back trimmed, a request with no die reads null, and a key stored under different casing still reads · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestDieAnswerTests.cs`
- [x] **T011** [P] FR-002, FR-003, FR-004, FR-006, FR-009: assert a die number is matched exactly, a part number returns every die it owns with explicit associations before history and no die twice, a term matching nothing answers `Empty`, an unreachable source answers `Unavailable`, the term reaches the script normalised, and a die described `Die still needs location` keeps that text with no location invented from it · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestDieDataTests.cs`

**⟶ Wait for Wave 3 to finish, then:**

- [x] **T012** FR-001, FR-005, FR-011: hand `ReadDiesAsync` to the new seam, answer `Ready` for the Die step, and raise one request per chosen die with that die's number captured and its `die.number` / `die.location` answers written, keeping the single-request path when no die is stated · `MTM_Waitlist.GenericRequest/Services/GenericRequestLiveStepData.cs`

---

## Phase 3: User Story 1 — A die is found by its own number or by the part it makes (P1)

Files: `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestDieViewModel.cs`, `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestDieViewModelTests.cs`

**Goal**: entering the die step searches Infor Visual with whatever is typed, and the step says plainly whether a die matched, matched nothing, or could not be asked.
**Independent Test**: search a die number, then a part number, then a number that exists nowhere; the first two populate and the third leaves the step empty with Continue disabled and Infor Visual named.
**Checkpoint**: the step answers from the live source and never from a job.

### Tests

- [x] **T013** [US1] FR-001, FR-004: assert entering the step asks the seam once with the normalised term and that no job value is read anywhere in the search path · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestDieViewModelTests.cs`
- [x] **T014** [US1] FR-002, FR-013: assert a term matching nothing leaves the step empty with Continue disabled, and that Continue stays disabled while no die is chosen · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestDieViewModelTests.cs`
- [x] **T015** [US1] FR-005: assert an unreachable source leaves the step unavailable with the attempt time drawn, that Retry asks again with the term the requester typed, and that Continue is disabled throughout · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestDieViewModelTests.cs`

### Implementation

**Wave 1 — independent (different files):**

- [x] **T016** [US1] Run the search on entry and on Search, keep the typed term across a failure, expose the match count, mark rows as chosen, keep a chosen die marked when a later search returns it, and gate Continue on at least one choice · `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestDieViewModel.cs`

---

## Phase 4: User Story 2 — Every die a part has is listed, with its location (P1)

Files: `MTM_Waitlist.GenericRequest/Views/GenericRequestDiePage.xaml`, `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestDieSearchTests.cs`, `MTM_Waitlist.Tests/Module_GenericRequest/Views/GenericRequestDiePageMarkupTests.cs`

**Goal**: the results list every die with its own location, the count is visible above it, and a die with no location says so.
**Independent Test**: search a part owning several dies and confirm every die is listed with a location or the "No location recorded" line, with the count matching the list.
**Checkpoint**: no result row can draw a blank location, and a partial list cannot pass for a complete one.

### Tests

- [x] **T017** [US2] FR-003, FR-006, FR-007: assert a part owning several dies returns all of them, each with its own location, and that the count the step exposes equals the number listed · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestDieSearchTests.cs`
- [x] **T018** [US2] FR-006, FR-009: assert a die with no recorded location draws the "No location recorded" line and that a die described `Die still needs location` draws that text as its description with no location invented from it · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestDieSearchTests.cs`
- [x] **T019** [US2] FR-010: assert a die marked as chosen is still marked when a later search returns it, and that choosing two dies records both · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestDieSearchTests.cs`

### Implementation

**Wave 1 — independent (different files):**

- [x] **T020** [US2] Draw the count above the list, the per-row location line or its "No location recorded" wording, and a checkbox bound to each row's choice, keeping every match reachable through the list's own scrolling · `MTM_Waitlist.GenericRequest/Views/GenericRequestDiePage.xaml`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T021** [US2] FR-007, FR-008: assert the page binds the count, the location line and the row checkbox, and that the list scrolls rather than clipping a long result · `MTM_Waitlist.Tests/Module_GenericRequest/Views/GenericRequestDiePageMarkupTests.cs`

---

## Phase 5: User Story 3 — One die or several, each with its own request (P2)

Files: `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestSummaryViewModel.cs`, `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestResultViewModel.cs`, `MTM_Waitlist.Waitlist.View/Models/WaitlistRequestTitles.cs`, `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs`, `Strings/en-us/Resources.resw`, `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestDieSubmissionTests.cs`, `MTM_Waitlist.Tests/Module_Waitlist/WaitlistRequestTitlesDieTests.cs`

**Goal**: three dies chosen means three requests, the review lists all three, and each card names its own die and where it lives.
**Independent Test**: choose three dies, confirm the review lists three, raise, and confirm three requests exist each naming one die and its location.
**Checkpoint**: the review can never understate what is about to be raised.

### Tests

- [x] **T022** [US3] FR-011: assert three chosen dies raise three requests, each carrying its own die as the captured value, and that the review lists every one of them · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestDieSubmissionTests.cs`
- [x] **T023** [US3] FR-011: assert each raised request stores `die.number` and `die.location`, and that a die with no location stores no location key rather than an empty one · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestDieSubmissionTests.cs`
- [x] **T024** [US3] FR-012: assert a generic die request's card reads the die and its location from the stored answer with no job present · `MTM_Waitlist.Tests/Module_Waitlist/WaitlistRequestTitlesDieTests.cs`

### Implementation

**Wave 1 — independent (different files):**

- [x] **T025** [P] [US3] List every chosen die on the review, one row each, rather than the single die row it draws today · `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestSummaryViewModel.cs`
- [x] **T026** [P] [US3] Say how many requests were raised when a die request raises more than one, and keep the single request number for the ordinary case · `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestResultViewModel.cs`
- [x] **T027** [P] [US3] Answer a generic die request's die and die location from its stored answer when there is no job to read them from · `MTM_Waitlist.Waitlist.View/Models/WaitlistRequestTitles.cs`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T028** [US3] Answer the die-sourced cells of a generic die request from the stored answer, so the card's die location is drawn from the request rather than looked for in a job that does not exist · `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs`

**⟶ Wait for Wave 2 to finish, then:**

- [x] **T029** [US3] Add the card's wording for a generic die request whose stored answer carries no location, so the cell says so rather than drawing blank · `Strings/en-us/Resources.resw`

---

## Phase 6: Polish

**Wave 1 — independent (different files):**

- [x] **T030** [P] Validate against Success Criteria: run `dotnet build MTM_Waitlist.sln` and `dotnet test MTM_Waitlist.Tests\MTM_Waitlist.Tests.csproj`, confirming SC-001 (one search reaches the die), SC-002 (a multi-die part lists every die), SC-003 (no die reaches the queue unproven), SC-004 (three dies, three requests, all reviewed and named) and SC-005 (no blank location row)
- [x] **T031** [P] FR-014: confirm no `Temp` artefact exists in the shipped graph or anywhere in the repository, that the boundary audit passes, and remove the stale ignored preview output left on this workstation

---

## Dependencies & Execution Order

Setup → Foundational → US1 → US2 → US3 → Polish.

- T001 is independent and blocks nothing, but its keys are what the page and the result step draw.
- Foundational: wave 1 (T002–T007) in any order; ⟶ wait; wave 2 (T008, T009); ⟶ wait; wave 3 (T010, T011); ⟶ wait; wave 4 (T012). Every later phase reads what T008 answers and what T012 raises.
- US1 waits for Foundational: wave 1 (T016) after its tests (T013–T015), which fail until it lands.
- US2 waits for US1, because the page binds the view model US1 builds. Wave 1 (T020); ⟶ wait; wave 2 (T021).
- US3 waits for US2 only for the dies it lists; its own work is independent of the page. Wave 1 (T025, T026, T027) in any order; ⟶ wait; wave 2 (T028); ⟶ wait; wave 3 (T029).
- Polish runs last; T030 and T031 are independent.
- No file appears under two phases: the die view model belongs to US1, the die page to US2, the review/result/card files to US3, and the shared seam to Foundational.
