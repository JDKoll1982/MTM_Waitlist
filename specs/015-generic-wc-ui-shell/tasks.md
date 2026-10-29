# Tasks: Generic Work Center — Screen Layer and Preview Host

## Scale note

Forty-five files across three new projects, one new test area and one shared document. Two boundaries carry all
the risk: nothing `Temp`-named may reach the shipped application, and every XAML file must appear in the surface
inventory or the inventory audit fails.

## Phase 1: Setup

**Files**: `MTM_Waitlist.GenericRequest/MTM_Waitlist.GenericRequest.csproj`,
`MTM_Waitlist.GenericRequest.Controls/MTM_Waitlist.GenericRequest.Controls.csproj`,
`TempGenericRequest.Preview/TempGenericRequest.Preview.csproj`, `MTM_Waitlist.sln`

**Wave 1 — independent (different files):**

- [x] **T001** [P] [US1] Create the flow library project with its root namespace and project references to Core and Shared · `MTM_Waitlist.GenericRequest/MTM_Waitlist.GenericRequest.csproj`
- [x] **T002** [P] [US2] Create the card library project with its root namespace and project references to Core and Shared · `MTM_Waitlist.GenericRequest.Controls/MTM_Waitlist.GenericRequest.Controls.csproj`
- [x] **T003** [P] [US3] Create the preview application project, assembly and root namespace all carrying the `Temp` naming, referencing both new libraries and nothing from the application · `TempGenericRequest.Preview/TempGenericRequest.Preview.csproj`
- [x] **T004** [P] [US1] Add the flow library's screen wording · `MTM_Waitlist.GenericRequest/Strings/en-us/Resources.resw`
- [x] **T005** [P] [US2] Add the card library's wording · `MTM_Waitlist.GenericRequest.Controls/Strings/en-us/Resources.resw`
- [x] **T006** [P] [US3] Add the preview application's `Temp`-prefixed wording · `TempGenericRequest.Preview/Strings/en-us/Resources.resw`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T007** [US3] Register the three projects in the solution and confirm no project reference was added from the shipping application · `MTM_Waitlist.sln`

## Phase 2: Foundational

**Files**: `MTM_Waitlist.GenericRequest/Contracts/*`, `MTM_Waitlist.GenericRequest/Models/*`,
`MTM_Waitlist.GenericRequest/Services/GenericRequestFlowRules.cs`,
`MTM_Waitlist.GenericRequest/ViewModels/GenericRequestStepViewModel.cs`,
`MTM_Waitlist.GenericRequest/Controls/GenericRequestStepFrame.xaml`,
`MTM_Waitlist.GenericRequest.Controls/Models/*`, `MTM_Waitlist.GenericRequest.Controls/Helpers/GenericRequestCardRules.cs`

**Wave 1 — independent (different files):**

- [x] **T008** [P] [US1] Declare the two seams and the step and presentation enumerations (FR-010) · `MTM_Waitlist.GenericRequest/Contracts/IGenericRequestStepData.cs`, `MTM_Waitlist.GenericRequest/Contracts/IGenericRequestNavigator.cs`, `MTM_Waitlist.GenericRequest/Contracts/GenericRequestStep.cs`
- [x] **T009** [P] [US1] Write the result type and the draft (FR-006) · `MTM_Waitlist.GenericRequest/Models/GenericRequestStepResult.cs`, `MTM_Waitlist.GenericRequest/Models/GenericRequestDraft.cs`
- [x] **T010** [P] [US1] Write the eight option rows the screens draw · `MTM_Waitlist.GenericRequest/Models/GenericRequestOptions.cs`
- [x] **T011** [P] [US2] Write the card model, its cell and its history entry (FR-018) · `MTM_Waitlist.GenericRequest.Controls/Models/GenericRequestCardModel.cs`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T012** [US1] Write the routing rule that decides the next and previous step, including the conditional dunnage, scrap and die steps (FR-004) · `MTM_Waitlist.GenericRequest/Services/GenericRequestFlowRules.cs`
- [x] **T013** [US2] Write the rule that returns the cells one card variant draws and omits the cells it has no value for (FR-015, FR-018) · `MTM_Waitlist.GenericRequest.Controls/Helpers/GenericRequestCardRules.cs`

**⟶ Wait for T012 to finish, then:**

- [x] **T014** [US1] Write the step view-model base: load, presentation, attempt time, retry and continue/back commands (FR-005, FR-006) · `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestStepViewModel.cs`

**⟶ Wait for T014 to finish, then:**

- [x] **T015** [US1] Build the one control that draws a step's header and exactly one of the three presentations, with the attempt time and retry control on the unavailable state only (FR-006) · `MTM_Waitlist.GenericRequest/Controls/GenericRequestStepFrame.xaml`, `MTM_Waitlist.GenericRequest/Controls/GenericRequestStepFrame.xaml.cs`

**Checkpoint**: the seam, the models, the routing rule and the three-state presenter exist, so any screen can be
written against them.

## Phase 3: User Story 1 — every screen of the flow can be walked (P1)

**Goal**: a reviewer opens the preview, walks work center → category → item → part → dunnage/scrap/die → details →
summary → result, and sees each screen populated, empty and unavailable, with no database anywhere.

**Independent Test**: run the preview with no database configured, walk every screen forward and back, switch each
screen's scenario, and confirm the answers survive going back.

**Files**: `MTM_Waitlist.GenericRequest/ViewModels/GenericRequest*ViewModel.cs` (one per step except the base),
`MTM_Waitlist.GenericRequest/Views/GenericRequest*.xaml` (+ `.xaml.cs`),
`TempGenericRequest.Preview/Temp/*`, `TempGenericRequest.Preview/Views/TempPreviewShell.xaml*`,
`TempGenericRequest.Preview/App.xaml*`

**Wave 1 — independent (different files):**

- [x] **T016** [P] [US1] Lay out the work center step and its view model (FR-004, FR-005) · `MTM_Waitlist.GenericRequest/Views/GenericRequestWorkCenterPage.xaml`, `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestWorkCenterViewModel.cs`
- [x] **T017** [P] [US1] Lay out the category step and its view model (FR-004, FR-005) · `MTM_Waitlist.GenericRequest/Views/GenericRequestCategoryPage.xaml`, `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestCategoryViewModel.cs`
- [x] **T018** [P] [US1] Lay out the item step and its view model (FR-004, FR-005) · `MTM_Waitlist.GenericRequest/Views/GenericRequestItemPage.xaml`, `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestItemViewModel.cs`
- [x] **T019** [P] [US1] Lay out the part step with its WIP / finished-good stage and sequence list, and write its view model (FR-004, FR-007) · `MTM_Waitlist.GenericRequest/Views/GenericRequestPartPage.xaml`, `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestPartViewModel.cs`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T020** [P] [US1] Lay out the dunnage catalogue step as type list, part grid and selection summary, and write its view model (FR-004) · `MTM_Waitlist.GenericRequest/Views/GenericRequestDunnagePage.xaml`, `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestDunnageViewModel.cs`
- [x] **T021** [P] [US1] Lay out the scrap type step with lugger availability, and write its view model (FR-004) · `MTM_Waitlist.GenericRequest/Views/GenericRequestScrapTypePage.xaml`, `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestScrapTypeViewModel.cs`
- [x] **T022** [P] [US1] Lay out the die search step and write its view model (FR-004) · `MTM_Waitlist.GenericRequest/Views/GenericRequestDiePage.xaml`, `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestDieViewModel.cs`
- [x] **T023** [P] [US1] Lay out the details step with needed-by date and time, quantity and description, and write its view model (FR-004) · `MTM_Waitlist.GenericRequest/Views/GenericRequestDetailsPage.xaml`, `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestDetailsViewModel.cs`

**⟶ Wait for Wave 2 to finish, then:**

- [x] **T024** [P] [US1] Lay out the summary step as a review list of every answer, and write its view model (FR-004) · `MTM_Waitlist.GenericRequest/Views/GenericRequestSummaryPage.xaml`, `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestSummaryViewModel.cs`
- [x] **T025** [P] [US1] Lay out the result step and write its view model (FR-004) · `MTM_Waitlist.GenericRequest/Views/GenericRequestResultPage.xaml`, `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestResultViewModel.cs`

**⟶ Wait for Wave 3 to finish, then:**

- [x] **T026** [US1] Write the in-memory step data that answers every seam method from the chosen scenario, including the awkward real values, and opens no connection (FR-011, FR-013, FR-014) · `TempGenericRequest.Preview/Temp/TempGenericRequestStepData.cs`, `TempGenericRequest.Preview/Temp/TempPreviewScenarios.cs`
- [x] **T027** [US1] Write the navigator that swaps the hosted page, and the window that offers the screen and scenario choice (FR-012) · `TempGenericRequest.Preview/Temp/TempGenericRequestNavigator.cs`, `TempGenericRequest.Preview/Views/TempPreviewShell.xaml`, `TempGenericRequest.Preview/Views/TempPreviewShell.xaml.cs`, `TempGenericRequest.Preview/App.xaml`, `TempGenericRequest.Preview/App.xaml.cs`, `TempGenericRequest.Preview/MainWindow.xaml`, `TempGenericRequest.Preview/MainWindow.xaml.cs`

**Checkpoint**: every screen of the flow renders in all three presentations from the preview application, with no
database configured.

## Phase 4: User Story 2 — the two cards can be compared side by side (P1)

**Goal**: one list holds a generic card and a work-order card, distinguishable at a glance, and the generic card can
be switched through its part, dunnage, scrap and die variants without a blank cell.

**Independent Test**: open the card preview, switch the variant four times, and confirm each draws only its own cells
with no blank or placeholder cell.

**Files**: `MTM_Waitlist.GenericRequest.Controls/Controls/GenericRequestCard.xaml*`,
`MTM_Waitlist.GenericRequest.Controls/Views/GenericRequestDetailPage.xaml*`,
`TempGenericRequest.Preview/Views/TempPreviewCardPage.xaml*`,
`MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestCardRulesTests.cs`,
`MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestFlowRulesTests.cs`

**Wave 1 — independent (different files):**

- [x] **T028** [P] [US2] Prove the card rule draws only the cells of the chosen variant and omits a cell with no value (FR-015, FR-018) · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestCardRulesTests.cs`
- [x] **T029** [P] [US1] Prove the routing rule reaches the conditional steps and never leaves the flow (FR-004) · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestFlowRulesTests.cs`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T030** [US2] Build the generic card control that lays out its own cells and sees the variant change (FR-015, FR-016, FR-007) · `MTM_Waitlist.GenericRequest.Controls/Controls/GenericRequestCard.xaml`, `MTM_Waitlist.GenericRequest.Controls/Controls/GenericRequestCard.xaml.cs`
- [x] **T031** [US2] Build the detail page: the card's facts plus requester, time raised and history (FR-002) · `MTM_Waitlist.GenericRequest.Controls/Views/GenericRequestDetailPage.xaml`, `MTM_Waitlist.GenericRequest.Controls/Views/GenericRequestDetailPage.xaml.cs`

**⟶ Wait for Wave 2 to finish, then:**

- [x] **T032** [US2] Build the card preview page: both cards in one list, with variant and scenario choice (FR-017) · `TempGenericRequest.Preview/Views/TempPreviewCardPage.xaml`, `TempGenericRequest.Preview/Views/TempPreviewCardPage.xaml.cs`

**Checkpoint**: the generic card and the work-order card can be judged side by side in every variant.

## Phase 5: User Story 3 — the shipping application carries no preview code (P2)

**Goal**: the application builds and runs without the preview application, and any `Temp`-named artefact that
reaches it or its reference graph fails the build with a message naming it.

**Independent Test**: run the audit; then feed it a synthetic offending file and confirm it names that file.

**Files**: `MTM_Waitlist.Tests/Module_GenericRequest/TempArtefactBoundaryAuditTests.cs`

**Wave 1 — single task:**

- [x] **T033** [US3] Write the audit that scans the application and its whole reference graph for `Temp`-named types, namespaces, resource keys, files and database objects, allows only the preview project and test code, lists any other exemption explicitly, names the offending artefact, and proves itself against a synthetic offence (FR-019, FR-021, FR-023, FR-024, FR-025, FR-026)

**Checkpoint**: the `Temp` boundary is enforced by a test that fails and names what crossed it.

## Phase 6: Polish

**Files**: `docs/guides/guides-xaml-surface-inventory.md`

**Wave 1 — single task:**

- [x] **T034** [US1] List every new XAML file in the surface inventory with its kind, area, surface, path and whether it is modal, so the inventory audit still matches the tree (FR-009)

**⟶ Wait for T034 to finish, then:**

- [x] **T035** [US1] Build the solution clean and run the test suite, then confirm the application builds with the preview application removed from the solution (FR-020, SC-004, SC-005)

## Dependencies & Execution Order

Phase 1 creates the projects; nothing builds before it. Phase 2 blocks every story: the seam, the models, the
routing rule and the presenter are what the screens are written against. Within Phase 3 the waves are the flow
order, not a preference: the later screens reuse the shapes the earlier ones establish. Phase 4 follows Phase 3
because the card preview page reuses the preview application's shell. Phase 5 needs the projects to exist so its
audit has a reference graph to walk. Phase 6 closes the inventory and runs the gates.

## Parallel Opportunities

Waves T016 to T019, T020 to T023 and T024 to T025 are four, four and two independent screens respectively; each task
owns its own page and view model, so they can be written in any order within their wave. T028 and T029 are
independent test files.
