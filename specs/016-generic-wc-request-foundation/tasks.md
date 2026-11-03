# Tasks: Generic Work Center — Designation and Request Foundation

## Scale note

This is an oversized change: roughly thirty files across the queue schema and four procedures, the waitlist request
seam, both generic request libraries, the new request wizard's entry point, the board and the detail route. Watch two
lines: the queue write (one seam, one procedure, one flag) and the answer seam (four reads that must report their
source as unavailable rather than offer a substitute).

Feature: [spec.md](./spec.md) · Plan: [plan.md](./plan.md) · Design: [data-model.md](./data-model.md) ·
Contracts: [contracts/](./contracts/) · Research: [research.md](./research.md)

---

## Phase 1: Setup

**Wave 1 — independent (different files):**

- [x] **T001** [P] Reference the generic request libraries from the application and stop excluding them from its build, so the flow screens can be hosted · `MTM_Waitlist.csproj`, `MTM_Waitlist.GenericRequest/MTM_Waitlist.GenericRequest.csproj`

---

## Phase 2: Foundational — the queue write path (blocks every story)

**Wave 1 — independent (different artifacts):**

- [x] **T002** [P] Make the queue's job id nullable and add `is_generic`, `description` and `accepted_warnings_json`, with the rollback that removes them · `Database/Tables/18_waitlist_requests_queue/create.sql`, `Database/Tables/18_waitlist_requests_queue/rollback.sql`
- [x] **T003** [P] Add `p_is_generic`, `p_description` and `p_accepted_warnings_json` to the insert procedure and write the job id through the optional-value idiom · `Database/StoredProcedures/sp_waitlist_request_insert/create.sql`, `Database/StoredProcedures/sp_waitlist_request_insert/rollback.sql`
- [x] **T004** [P] Return `is_generic`, `description` and `accepted_warnings_json` from both request reads, and add the handler columns the get read is missing · `Database/StoredProcedures/sp_waitlist_request_list/create.sql`, `Database/StoredProcedures/sp_waitlist_request_get/create.sql`
- [x] **T007** [P] Carry the generic flag, the description and the accepted warnings on the request draft and the stored request · `MTM_Waitlist.Waitlist.View/Models/WaitlistRequestDraft.cs`, `MTM_Waitlist.Waitlist.View/Models/WaitlistRequest.cs`
- [x] **T008** [P] Carry the generic flag, the stated answer, the description and the allotted minutes on the board row · `MTM_Waitlist.Waitlist.View/Models/SampleOrder.cs`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T006** Regenerate the aggregate SQL files and record the new columns' descriptions · `Database/Tables/AllTables.sql`, `Database/StoredProcedures/AllSPs.sql`, `Database/Bootstrap/update_table_descriptions.sql`
- [x] **T010** Refuse a blank job id only when the draft is not generic, send the three new parameters, and map the three new columns when a row is read · `MTM_Waitlist.Waitlist.View/Services/WaitlistRequestService.cs`

---

## Phase 3: US1 — the generic designation is maintained, not built in (P1)

### Implementation

**Wave 1 — independent (different files):**

- [x] **T005** [P] [US1] Return `is_generic` from the work-centre catalog read · `Database/StoredProcedures/sp_setup_work_centers_catalog_get/create.sql`, `Database/StoredProcedures/sp_setup_work_centers_catalog_get/rollback.sql`
- [x] **T009** [P] [US1] Carry the designation on the work-centre detail and the selection item · `MTM_Waitlist.Shared/Models/WorkCenterDetail.cs`, `MTM_Waitlist.Shared/Models/WorkCenterSelectionItem.cs`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T011** [US1] Map the designation from the catalog read onto each work-centre detail · `MTM_Waitlist.Shared/Services/WorkCenterCatalogService.cs`

**⟶ Wait for Wave 1 to finish, then:**

### Tests

- [x] **T012** [P] [US1] Prove a designated work centre reaches the selection item as generic, an undesignated one as not, and a fresh record as not (FR-001, FR-003) · `MTM_Waitlist.Tests/Module_Shared/WorkCenterGenericDesignationTests.cs`

**Checkpoint**: the designation still round-trips from the catalog to the wizard. No user-story work in this phase changes what a person sees.

---

## Phase 4: US2 — a work center with no work order can raise a real request (P1)

### Implementation

**Wave 1 — independent (different files):**

- [x] **T013** [P] [US2] Add the submission result type and the answer kind the item step asks for · `MTM_Waitlist.GenericRequest/Models/GenericRequestSubmission.cs`
- [x] **T014** [P] [US2] Carry the building, item code, answer, allotted minutes, accepted warnings and raised id on the draft, and make the part step conditional (FR-005, FR-012, FR-019) · `MTM_Waitlist.GenericRequest/Models/GenericRequestDraft.cs`
- [x] **T015** [P] [US2] Extend the item option with its code, its configured question, its options and its allotted minutes, and add the four answer flags (FR-006, FR-007, FR-013) · `MTM_Waitlist.GenericRequest/Models/GenericRequestOptions.cs`
- [x] **T016** [P] [US2] Add the submission to the answer seam (FR-009, FR-018) · `MTM_Waitlist.GenericRequest/Contracts/IGenericRequestStepData.cs`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T017** [US2] Gate the part step on the item's own requirement so a request that states a configured answer never reaches it · `MTM_Waitlist.GenericRequest/Services/GenericRequestFlowRules.cs`
- [x] **T018** [US2] Implement the live answer seam: generic work centres from the catalog, categories from the wizard's own list, items and their questions from the item catalog, and the four externally proved answers reporting unavailable · `MTM_Waitlist.GenericRequest/Services/GenericRequestLiveStepData.cs`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T019** [US2] Ask the item's configured question on the item step and record the answer and its identity on the draft · `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestItemViewModel.cs`
- [x] **T020** [US2] Let a step prepare before it advances, so the summary can raise the request and hold the requester when the write fails (FR-018, FR-029) · `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestStepViewModel.cs`
- [x] **T021** [P] [US2] Draw the configured question on the item page · `MTM_Waitlist.GenericRequest/Views/GenericRequestItemPage.xaml`

**⟶ Wait for T018, T019 and T020 to finish, then:**

- [x] **T022** [US2] Raise the request from the summary step and report the outcome on the result step · `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestSummaryViewModel.cs`, `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestResultViewModel.cs`
- [x] **T023** [P] [US2] Add the wording the new question, the submission state and the raised request need · `MTM_Waitlist.GenericRequest/Strings/en-us/Resources.resw`

**⟶ Wait for T001, T018 and T022 to finish, then:**

- [x] **T024** [US2] Host the flow over the shell's navigation: one navigator that carries the draft and maps each step to its view model · `Services/GenericRequestNavigator.cs`
- [x] **T025** [US2] Register the flow's view models, pages, answer seam and navigator, and point the pages at their host · `Services/DependencyInjection/ServiceRegistrationExtensions.cs`, `App.xaml.cs`
- [x] **T026** [US2] Enter the generic flow when the chosen work centre is designated generic, and keep the work-order flow unchanged (FR-005, FR-010) · `MTM_Waitlist.Waitlist.NewRequest/ViewModels/NewRequestWorkCenterViewModel.cs`

### Tests

- [x] **T027** [P] [US2] Prove the flow's order skips the part step for an answer-only item and still reaches it for an item that needs one · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestFlowRulesTests.cs`
- [x] **T028** [P] [US2] Prove the item step records the configured answer, refuses a blank one, and reports the four externally proved answers as unavailable (FR-007, FR-012, FR-021) · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestItemViewModelTests.cs`

**Checkpoint**: a designated work centre raises a generic request for an item that asks a configured question, and it appears on the board with no work order anywhere.

---

## Phase 5: US3 — the request carries an honest time and an honest ask (P2)

### Implementation

**Wave 1 — independent (different files):**

- [x] **T029** [P] [US3] Read the item's allotted minutes with the rest of its configuration and record them on the draft, leaving nothing when the item configures none (FR-013, FR-014) · `MTM_Waitlist.GenericRequest/Services/GenericRequestLiveStepData.cs`
- [x] **T030** [P] [US3] Require a needed-by answer on the details step and keep the description optional (FR-015, FR-016) · `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestDetailsViewModel.cs`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T031** [US3] Show the allotted time and the needed-by answer on the review step, and nothing where no time is configured (FR-013, FR-014, FR-017) · `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestSummaryViewModel.cs`

### Tests

- [x] **T032** [P] [US3] Prove a configured time reaches the request, an unconfigured item adds no estimate, and the review lists every answer given (FR-013, FR-014, FR-017) · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestSummaryViewModelTests.cs`

**Checkpoint**: a generic request carries the same configured time a work-order request would, and the requester's needed-by answer and description travel with it.

---

## Phase 6: US4 — the generic card tells the truth and is told apart (P2)

### Implementation

**Wave 1 — independent (different files):**

- [x] **T033** [P] [US4] Give the card a stated-answer variant and carry the category, the description and the stated value · `MTM_Waitlist.GenericRequest.Controls/Models/GenericRequestCardModel.cs`
- [x] **T034** [P] [US4] Draw the category, the stated answer and the description only when they have a value, and never a blank cell (FR-022, FR-024, FR-025) · `MTM_Waitlist.GenericRequest.Controls/Helpers/GenericRequestCardRules.cs`
- [x] **T035** [P] [US4] Add the wording the new cells need · `MTM_Waitlist.GenericRequest.Controls/Strings/en-us/Resources.resw`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T036** [US4] Build a generic card from a stored request, and stop drawing the work-order row for it (FR-008, FR-022, FR-023) · `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs`
- [x] **T037** [P] [US4] Choose the card template per row, overriding both selector overloads · `Module_Waitlist/Controls/GenericRequestCardTemplateSelector.cs`
- [x] **T038** [US4] Host the generic card beside the work-order card in one list (FR-022) · `Module_Waitlist/Views/WaitlistViewPage.xaml`, `Module_Waitlist/Views/WaitlistViewPage.xaml.cs`
- [x] **T039** [P] [US4] Add the generic request's detail route and register its page (FR-026) · `Services/DependencyInjection/ServiceRegistrationExtensions.cs`

**⟶ Wait for T036 to finish, then:**

- [x] **T040** [US4] Open the generic request's detail page from its card, with the requester, the time raised, the accepted warnings and its history (FR-026, FR-027) · `MTM_Waitlist.GenericRequest.Controls/Views/GenericRequestDetailPage.xaml.cs`, `MTM_Waitlist.GenericRequest.Controls/Views/GenericRequestDetailPage.xaml`

### Tests

- [x] **T041** [P] [US4] Prove an answer-only card draws its stated value, its category and its description, and that a request without a description draws no cell for one (FR-024, FR-025) · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestCardRulesTests.cs`

**Checkpoint**: generic and work-order requests sit in one list, told apart at a glance, and a generic card opens onto a detail page that adds only the facts it has.

---

## Phase 7: Polish

**Wave 1 — independent (different files):**

- [x] **T042** [P] Record what this chunk added to the persistence guidance · `Database/Database-Ruleset.md`
- [x] **T043** [P] Prove no `Temp` artefact survives and the boundary audit still passes (FR-030) · `MTM_Waitlist.Tests/Module_GenericRequest/TempArtefactBoundaryAuditTests.cs`

**⟶ Wait for every phase to finish, then:**

- [x] **T044** Validate against the spec's Success Criteria: build the solution and run the test suite (SC-001 to SC-006) · `MTM_Waitlist.sln`

---

## Dependencies & Execution Order

Setup → Foundational → US1 → US2 → US3 → US4 → Polish. Each phase's waves run in order; the joins above are the
only ordering constraints inside a phase.

- **Phase 1** wave 1 (T001) unblocks nothing directly, but T024 and T038 need it.
- **Phase 2** wave 1 (T002, T003, T004, T007, T008) blocks wave 2 (T006, T010); T010 is the last writer of the queue seam.
- **Phase 3** wave 1 (T005, T009) blocks T011; T012 depends on T009 only.
- **Phase 4** wave 1 (T013 to T016) blocks T017 and T018; T018 blocks T019, T022 and T028; T020 blocks T022; T022 and T001 block T024; T024 blocks T025; T025 blocks T026.
- **Phase 5** wave 1 (T029, T030) blocks T031; T029 also needs T018.
- **Phase 6** wave 1 (T033 to T035) blocks T036; T036 blocks T040; T037 and T038 need T033 and T001; T039 needs T040.
- **Phase 7** T042 and T043 are independent; T044 runs last and needs every phase.

### Parallel opportunities

Every `[P]` task touches a file no other task in its wave touches: the five queue artifacts in Phase 2 wave 1, the two
read artifacts in Phase 3 wave 1, the four model and contract files in Phase 4 wave 1, the card model, rules and
strings in Phase 6 wave 1, and the two polish documents.
