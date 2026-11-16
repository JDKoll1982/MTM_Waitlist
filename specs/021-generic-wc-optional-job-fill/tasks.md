# Tasks — Optional Job Fill

**Scale note**: thirty-one tasks across five areas — the generic request module, the composition root, the request queue's
schema and procedures, the waitlist detail page, and their tests. Watch the ordering: the schema and the seam land
before any screen draws the offer, and provenance lands on the raise before the detail page can show it.

No Phase 1 setup tasks: this change adds no project, no package and no tooling.

## Phase 2 — Foundational

Files: `MTM_Waitlist.GenericRequest/Models/GenericRequestJobFill.cs`,
`MTM_Waitlist.GenericRequest/Contracts/IGenericRequestJobFillData.cs`,
`MTM_Waitlist.GenericRequest/Models/GenericRequestDraft.cs`,
`MTM_Waitlist.GenericRequest/Strings/en-us/Resources.resw`,
`Database/Tables/18_waitlist_requests_queue/create.sql`, `Database/Tables/18_waitlist_requests_queue/rollback.sql`,
`Database/Tables/AllTables.sql`, `Database/Bootstrap/update_table_descriptions.sql`,
`Database/StoredProcedures/sp_waitlist_request_insert/*`, `Database/StoredProcedures/sp_waitlist_request_get/*`,
`Database/StoredProcedures/sp_waitlist_request_list/*`, `Database/StoredProcedures/AllSPs.sql`,
`MTM_Waitlist.Waitlist.View/Models/WaitlistRequestDraft.cs`, `MTM_Waitlist.Waitlist.View/Models/WaitlistRequest.cs`,
`MTM_Waitlist.Waitlist.View/Services/WaitlistRequestService.cs`, `Services/GenericRequestJobFillSource.cs`,
`Services/DependencyInjection/ServiceRegistrationExtensions.cs`

**Wave 1 — independent (different files):**

- [x] **T001** [P] Create the accepted fill, its candidate and its four offer states, with one flag per seeded answer and the four clear methods · MTM_Waitlist.GenericRequest/Models/GenericRequestJobFill.cs
- [x] **T002** [P] Declare the job-fill seam: a probe that names the job and a read that returns the values, both over `GenericRequestStepResult` · MTM_Waitlist.GenericRequest/Contracts/IGenericRequestJobFillData.cs
- [x] **T003** [P] Add the pinned job-fill copy verbatim, including `Fill this request from job {0} at {1}?`, `From job {0}` and `The running job could not be read` · MTM_Waitlist.GenericRequest/Strings/en-us/Resources.resw
- [x] **T004** [P] Add `provenance_work_order VARCHAR(64) NULL` to the queue table, and drop it in the rollback · Database/Tables/18_waitlist_requests_queue/create.sql, Database/Tables/18_waitlist_requests_queue/rollback.sql
- [x] **T005** [P] Register the new column in the master table script and in the table-description maintenance file · Database/Tables/AllTables.sql, Database/Bootstrap/update_table_descriptions.sql

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2 — independent (different files):**

- [x] **T006** [P] Append `p_provenance_work_order` to the request insert and write it through the optional-value idiom · Database/StoredProcedures/sp_waitlist_request_insert/create.sql, Database/StoredProcedures/sp_waitlist_request_insert/rollback.sql
- [x] **T007** [P] Return provenance from the single-request read and from the list read · Database/StoredProcedures/sp_waitlist_request_get/create.sql, Database/StoredProcedures/sp_waitlist_request_get/rollback.sql, Database/StoredProcedures/sp_waitlist_request_list/create.sql, Database/StoredProcedures/sp_waitlist_request_list/rollback.sql
- [x] **T008** [P] Carry `ProvenanceWorkOrder` on the request draft and on the request model · MTM_Waitlist.Waitlist.View/Models/WaitlistRequestDraft.cs, MTM_Waitlist.Waitlist.View/Models/WaitlistRequest.cs
- [x] **T009** [P] Hold the accepted fill on the generic request draft and clear it with the answers it describes · MTM_Waitlist.GenericRequest/Models/GenericRequestDraft.cs

**⟶ Wait for Wave 2 to finish, then:**

**Wave 3 — independent (different files):**

- [x] **T010** [P] Mirror the three changed procedures into the stored-procedure master script · Database/StoredProcedures/AllSPs.sql
- [x] **T011** [P] Implement the job-fill source over the Setup module's active-job read, with the `simplified:` note where a permission gate would go, and register it · Services/GenericRequestJobFillSource.cs, Services/DependencyInjection/ServiceRegistrationExtensions.cs
- [x] **T012** [P] Pass provenance from the draft into the request write · MTM_Waitlist.Waitlist.View/Services/WaitlistRequestService.cs

## Phase 3 — User Story 1: the operator may let a running job fill the request (P1)

Files: `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestCategoryViewModel.cs`,
`MTM_Waitlist.GenericRequest/ViewModels/GenericRequestPartViewModel.cs`,
`MTM_Waitlist.GenericRequest/ViewModels/GenericRequestDunnageViewModel.cs`,
`MTM_Waitlist.GenericRequest/ViewModels/GenericRequestDieViewModel.cs`,
`MTM_Waitlist.GenericRequest/ViewModels/GenericRequestItemViewModel.cs`,
`MTM_Waitlist.GenericRequest/Views/GenericRequestCategoryPage.xaml`,
`MTM_Waitlist.GenericRequest/Views/GenericRequestPartPage.xaml`,
`Module_GenericRequest/Views/GenericRequestDunnagePage.xaml`,
`MTM_Waitlist.GenericRequest/Views/GenericRequestDiePage.xaml`

### Tests

Test files: `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestJobFillOfferTests.cs`,
`MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestJobFillFailureTests.cs`,
`MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestJobFillSeedingTests.cs`,
`MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestTestDoubles.cs`

**Wave 1 — independent (different files), written to fail first:**

- [x] **T013** [P] [US1] Prove the offer is drawn when a job is running and never when one is not, that declining offers nothing again in the same request, and that it is offered again on the next request (FR-001, FR-003, FR-004, A3, A7) · MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestJobFillOfferTests.cs
- [x] **T014** [P] [US1] Prove a failed accepted read seeds nothing, reports the attempt time, and that a retry re-reads without asking the question again (FR-016) · MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestJobFillFailureTests.cs
- [x] **T015** [P] [US1] Prove accepting seeds the part, sequence, die and dunnage and no identity value, that changing one answer stops it reading as the job's while the others do not, and that changing the item clears them all (FR-005, FR-006, FR-007, FR-008, FR-009, FR-011) · MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestJobFillSeedingTests.cs

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T016** [US1] Add a stub job-fill seam to the shared test doubles so every step test can stand one in · MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestTestDoubles.cs

### Implementation

**Wave 1 — independent (different files):**

- [x] **T017** [P] [US1] Probe on load and draw the offer, its two answers, the filled line and the unavailable state with a retry · MTM_Waitlist.GenericRequest/ViewModels/GenericRequestCategoryViewModel.cs
- [x] **T018** [P] [US1] Seed the part step from the fill and stop the qualifier the moment the part or sequence changes · MTM_Waitlist.GenericRequest/ViewModels/GenericRequestPartViewModel.cs
- [x] **T019** [P] [US1] Seed the dunnage step from the fill and stop the qualifier when another part is chosen · MTM_Waitlist.GenericRequest/ViewModels/GenericRequestDunnageViewModel.cs
- [x] **T020** [P] [US1] Seed the chosen dies from the fill, restore them on a revisit, and stop the qualifier when the choice changes · MTM_Waitlist.GenericRequest/ViewModels/GenericRequestDieViewModel.cs
- [x] **T021** [P] [US1] Drop the fill and the answers it seeded when the chosen item changes · MTM_Waitlist.GenericRequest/ViewModels/GenericRequestItemViewModel.cs

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2 — independent (different files):**

- [x] **T022** [P] [US1] Draw the offer panel above the category cards, with the two answers, the filled line and the unavailable line with its retry · MTM_Waitlist.GenericRequest/Views/GenericRequestCategoryPage.xaml
- [x] **T023** [P] [US1] Draw the `From job {0}` qualifier beside the seeded part · MTM_Waitlist.GenericRequest/Views/GenericRequestPartPage.xaml
- [x] **T024** [P] [US1] Draw the `From job {0}` qualifier beside the seeded dunnage part · Module_GenericRequest/Views/GenericRequestDunnagePage.xaml
- [x] **T025** [P] [US1] Draw the `From job {0}` qualifier beside the seeded die · MTM_Waitlist.GenericRequest/Views/GenericRequestDiePage.xaml

**⟶ Wait for Wave 2 to finish, then:**

- [x] **T026** [US1] Write the filled work order onto every request the raise produces, keeping the identity generic · MTM_Waitlist.GenericRequest/Services/GenericRequestLiveStepData.cs

**Checkpoint**: with a job running at a generic work center, the operator is offered the fill, accepts it, finds
the job's part, die and dunnage waiting as changeable answers, and declines on another request to see the flow
unchanged.

## Phase 4 — User Story 2: a filled request is still a generic request (P1)

Files: `Strings/en-us/Resources.resw`, `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewDetailViewModel.cs`

### Tests

Test files: `MTM_Waitlist.Tests/Module_Waitlist/WaitlistRequestJobFillProvenanceTests.cs`,
`MTM_Waitlist.Tests/Module_Waitlist/WaitlistDetailJobFillProvenanceTests.cs`

**Wave 1 — independent (different files), written to fail first:**

- [x] **T027** [P] [US2] Prove a filled request is raised generic, keeps its identity, and comes back from both reads with its provenance (FR-011, FR-012, FR-013) · MTM_Waitlist.Tests/Module_Waitlist/WaitlistRequestJobFillProvenanceTests.cs
- [x] **T028** [P] [US2] Prove the detail page draws the provenance line only for a filled request, and that the generic card draws no job cell (FR-012, FR-013) · MTM_Waitlist.Tests/Module_Waitlist/WaitlistDetailJobFillProvenanceTests.cs

### Implementation

**Wave 1:**

- [x] **T029** [P] [US2] Add the provenance copy verbatim · Strings/en-us/Resources.resw

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T030** [US2] Draw the provenance line on the request detail page, distinct from the Work order row · MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewDetailViewModel.cs

**Checkpoint**: a request filled from a job draws the generic card with no work order on it, and its detail page
names the job it was filled from as provenance.

## Phase 5 — Polish

- [x] **T031** [P] Confirm no `Temp` artefact of this feature remains anywhere in the solution, and that nothing was renamed into a permanent-looking name instead of being deleted (FR-017) · repository-wide
- [x] **T032** Validate against the Success Criteria: build the solution at the documented gate and run the full suite, then read the outcome against SC-001 to SC-005 · MTM_Waitlist.sln, MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj

## Dependencies & Execution Order

- Foundational runs first and blocks both stories: wave 1 (fill model, seam, copy, column, masters) → wave 2 (insert parameter, read projections, request model, draft member) → wave 3 (procedure masters, adapter and registration, provenance on the write).
- User Story 1 follows the Foundational phase: its tests are wave 1, then the shared test double, then the five view models in one independent wave, then the four pages, then the raise.
- User Story 2 depends on User Story 1's raise carrying provenance, so it runs after the Phase 3 checkpoint: the copy, then the detail row. Its tests are written before either.
- Polish runs last and depends on every phase.
- No file appears under two phases. The one file the two stories could both want, the application's resource file, is owned by User Story 2 alone.
