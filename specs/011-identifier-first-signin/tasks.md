# Tasks: Identifier-First Sign-In and a Faster Launch

**Feature**: `specs/011-identifier-first-signin/`
**Size**: oversized

## Scale Note

Roughly sixty files across four subsystems: the launch module, the launch surfaces, the external-read cache and
its service, and the database. A reader should watch two things: that every wait keeps its own stated maximum
while the launch-wide budget is added on top, and that no launch path ever refreshes the cached plant data. There
is no Setup phase: this change adds no project, no dependency and no configuration.

## Phase 2: Foundational

Blocks every story: the sixth read shape must resolve before the set-up path can look a person up, and the shared
identifier types must exist before the sign-in stage can classify anything.

**Wave 1 - independent (different files):**

- [x] **T001** [P] Add the launch module value (`Employee`, folder `Module_Startup`) · `MTM_Waitlist.Mock/Models/VisualReadShapeModule.cs`, `MTM_Waitlist.Mock/Models/VisualReadShapeModuleExtensions.cs`
- [x] **T002** [P] Write the live source query for the sixth shape · `Database/InforVisual/Queues/Module_Startup/Queries/GetEmployeeByNumber.sql`
- [x] **T003** [P] Write the population query for the sixth shape · `Database/InforVisual/Queues/Module_Mock/Populations/employee_by_number_population.sql`
- [x] **T004** [P] Create the mirror table and its rollback · `Database/Mock/Tables/visual_employee_by_number_result/{create,rollback}.sql`
- [x] **T005** [P] Create the stage twin and its rollback · `Database/Mock/Tables/visual_employee_by_number_result_stage/{create,rollback}.sql`
- [x] **T006** [P] Create the previous twin and its rollback · `Database/Mock/Tables/visual_employee_by_number_result_prev/{create,rollback}.sql`
- [x] **T007** [P] Create the shape's get procedure and its rollback · `Database/Mock/StoredProcedures/sp_visual_employee_by_number_get/{create,rollback}.sql`
- [x] **T008** [P] Create the shape's refresh procedure and its rollback · `Database/Mock/StoredProcedures/sp_visual_employee_by_number_refresh/{create,rollback}.sql`
- [x] **T009** [P] Add the sixth catalog entry · `MTM_Waitlist.Mock/Services/VisualReadShapeCatalog.cs`
- [x] **T010** [P] Write the in-app fallback for the sixth shape · `MTM_Waitlist.Mock/Services/VisualEmployeeByNumberFallback.cs`
- [x] **T011** [P] Create the account-creation procedure and its rollback · `Database/StoredProcedures/sp_core_account_setup_create/{create,rollback}.sql`
- [x] **T012** [P] Add the identifier types · `MTM_Waitlist.Startup/Models/IdentifierKind.cs`, `MTM_Waitlist.Startup/Models/IdentifierSignInResult.cs`
- [x] **T013** [P] Add the read-shape row contract · `MTM_Waitlist.Mock/Contracts/EmployeeByNumberRow.cs`
- [x] **T014** [P] Add the launch budget type · `MTM_Waitlist.Startup/Models/LaunchBudget.cs`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T015** Regenerate the table aggregate · `Database/Mock/AllTables.sql`
- [x] **T016** Regenerate the procedure aggregate · `Database/Mock/AllSPs.sql`
- [x] **T017** Extend the table descriptions script · `Database/Mock/Bootstrap/update_table_descriptions.sql`
- [x] **T018** Register the new fallback and shape · `MTM_Waitlist.Mock/DependencyInjection/MockServiceRegistrationExtensions.cs`
- [x] **T019** Update the shape-count assertions five to six · `MTM_Waitlist.Tests/Module_Mock_Service/ShapeAdditionNoDowntimeTests.cs`, `MTM_Waitlist.Tests/Module_Mock/VisualReadRouteAuditTests.cs`
- [x] **T020** Update the read-shape count in the ruleset · `Database/Database-Ruleset.md`
- [x] **T021** Test falling back to the mirror for the sixth shape · `MTM_Waitlist.Tests/Module_Mock/VisualReadFallbackParityTests.cs` — DONE (2026-10-04): the runtime mirror-fallback case for `employee_by_number` was added — an unreachable live read serves the mirror through `sp_visual_employee_by_number_get` with the row the shape maps, a reachable-but-empty live answer is never replaced by the mirror, and the sixth shape was added to the live-script parameter proof. Row parity against the shipped scripts stays in `MTM_Waitlist.Tests/Module_Mock/EmployeeByNumberShapeParityTests.cs`.
- [x] **T022** Test the catalog holds six shapes with the new key · `MTM_Waitlist.Tests/Module_Mock_Service/RefreshShapeCatalogProviderTests.cs`

**Checkpoint**: the catalog, database and service resolve six shapes, and the sixth round-trips from the mirror.

## Phase 3: User Story 1 - Signing in by identifier (P1)

Files: `MTM_Waitlist.Startup/Services/IdentifierSignInService.cs`,
`MTM_Waitlist.Startup/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs`,
`MTM_Waitlist.Startup/ViewModels/SignInViewModel.cs`.

### Tests

- [x] **T023** [US1] Test the four-digit classification and its counter-cases · `MTM_Waitlist.Tests/Module_Startup/Services/IdentifierSignInServiceTests.cs`
- [x] **T024** [US1] Test store-first resolution, the inactive short-circuit and the shared-number ordering · `MTM_Waitlist.Tests/Module_Startup/Services/IdentifierSignInServiceTests.cs`

### Implementation

**Wave 1 - independent (different files):**

- [x] **T025** [P] [US1] Write the identifier classification and resolution service · `MTM_Waitlist.Startup/Services/IdentifierSignInService.cs`
- [x] **T026** [P] [US1] Drive the two stages and the refusal from the view model · `MTM_Waitlist.Startup/ViewModels/SignInViewModel.cs`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T027** [US1] Register the identifier service · `MTM_Waitlist.Startup/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs`
- [x] **T028** [US1] Present the identifier field and the store-first refusal in the sign-in view · `Module_Startup/Views/SignInPage.xaml`, `Module_Startup/Views/SignInPage.xaml.cs` (renamed from `SignInWindow` by T042) — DONE (2026-10-04): the stage now shows an identifier field first (Enter or the continue button both call the view model's own `EnterAsync`), then the PIN stage for whoever the identifier resolved to, with the store-first refusal in the message strip and the form still open. The credential machinery behind the stage is untouched.

**Checkpoint**: a person the store knows signs in by user name or employee number and reaches the credential
stage, with the attempt, session, machine and temporary-credential behaviour unchanged.

## Phase 4: User Story 2 - Self set-up for a plant-system person (P1)

Files: `MTM_Waitlist.Startup/Services/AccountCreationService.cs`,
`MTM_Waitlist.Startup/ViewModels/SetUpPanelViewModel.cs`, `Module_Startup/Views/SetUpPanel.xaml(.cs)`.

### Tests

- [x] **T029** [US2] Test creation writes profile, credential and store-read rank in one call · `MTM_Waitlist.Tests/Module_Startup/Services/AccountCreationServiceTests.cs`
- [x] **T030** [US2] Test a part-way failure leaves nothing behind · `MTM_Waitlist.Tests/Module_Startup/Services/AccountCreationServiceTests.cs`

### Implementation

**Wave 1 - independent (different files):**

- [x] **T031** [P] [US2] Write the account-creation service over the new procedure · `MTM_Waitlist.Startup/Services/AccountCreationService.cs`
- [x] **T032** [P] [US2] Write the set-up panel view model · `MTM_Waitlist.Startup/ViewModels/SetUpPanelViewModel.cs`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T033** [US2] Register the creation service and wire the panel from the sign-in view model · `MTM_Waitlist.Startup/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs`, `MTM_Waitlist.Startup/ViewModels/SignInViewModel.cs` — DONE (2026-10-04): the panel is consumed by `SignInPage`, which opens it on `SignInViewModel.SetUpRequested` and redraws on its cancel; `SetUpPanelViewModel` now takes the sign-in stage, so a created account is signed in on the PIN the person chose and the existing `SignInAccepted` route carries the launch on.
- [x] **T034** [US2] Present the set-up panel · `Module_Startup/Views/SetUpPanel.xaml`, `Module_Startup/Views/SetUpPanel.xaml.cs` — DONE (2026-10-04): the panel was created as a `UserControl` of the sign-in stage. It states the person is not set up for the waitlist yet, shows the plant system's first and last name pre-filled and editable, asks for the PIN twice under the existing rules (both values travelling as one argument), assigns the store's lowest rank through the creation procedure, and notes that a higher level needs IT. Enter walks the fields and submits only on the last; a blank field shows its hint.

**Checkpoint**: a person known only to the plant system is created on their chosen PIN with the lowest store
rank, and signs in immediately on it.

## Phase 5: User Story 3 - Keyboard behaviour (P2)

Files: `MTM_Waitlist.Startup/ViewModels/SetUpPanelViewModel.cs` (Enter handling),
`MTM_Waitlist.Tests/Module_Startup/ViewModels/SetUpPanelViewModelTests.cs`.

### Implementation

**Wave 1 - independent (different files):**

- [x] **T035** [P] [US3] Test Enter advances and submits at the end, and a blank field shows its hint · `MTM_Waitlist.Tests/Module_Startup/ViewModels/SetUpPanelViewModelTests.cs`
- [x] **T036** [P] [US3] Test Enter submits at the identifier and credential stages · `MTM_Waitlist.Tests/Module_Startup/ViewModels/SignInViewModelTests.cs`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T037** [US3] Implement the Enter and blank-field behaviour · `MTM_Waitlist.Startup/ViewModels/SetUpPanelViewModel.cs`, `MTM_Waitlist.Startup/ViewModels/SignInViewModel.cs`

**Checkpoint**: the whole sign-in path is completable from the keyboard.

## Phase 6: User Story 4 - One window for the whole life of the app (P2)

Files: `Module_Startup/Views/LaunchWindow.xaml(.cs)`, `App.xaml.cs`, `MainWindow.xaml.cs`,
`MTM_Waitlist.Startup/Services/WindowPlacementService.cs`.

### Tests

- [x] **T038** [US4] Test stage selection maps each outcome to exactly one stage · `MTM_Waitlist.Tests/Module_Startup/Services/LaunchStageSelectionTests.cs`
- [x] **T039** [US4] Test re-centring is skipped for a resize the person performs · `MTM_Waitlist.Tests/Module_Startup/Services/WindowPlacementServiceTests.cs`

### Implementation

**Wave 1 - independent (different files):**

- [x] **T040** [P] [US4] Write the window placement service (centre, re-centre, maximise) · `MTM_Waitlist.Startup/Services/WindowPlacementService.cs`
- [x] **T041** [P] [US4] Create the one host window · `MainWindow.xaml(.cs)` — DONE (2026-10-04) **as a deviation from plan line 53**: the owner's decision settles the plan's self-contradiction — there is ONE window for the whole life of the app and it is the existing `MainWindow`, so no `LaunchWindow` was created and no second window exists. `MainWindow` gained a stage host (`ContentPresenter`) and `ShowStage(LaunchStage, UIElement)`, which sizes, re-centres and maximises through `WindowPlacementService`. The deviation is recorded in `plan.md`.

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T042** [US4] Convert the four surfaces into stage views · `Module_Startup/Views/SplashPage.xaml(.cs)`, `Module_Startup/Views/SignInPage.xaml(.cs)`, `Module_Startup/Views/MachineSetupPage.xaml(.cs)`, `Module_Startup/Views/BlockedStatePage.xaml(.cs)` — DONE (2026-10-04): the four surfaces are stage content of the one window rather than windows. `SplashWindow` was deleted (the existing `SplashPage` is the stage), and `SignInWindow`, `MachineSetupWindow` and `BlockedStateWindow` became the pages `SignInPage`, `MachineSetupPage` and `BlockedStatePage` — a `Page` named `…Window` would break the locked role-suffix rule. Each implements `ILaunchStageSurface`, so the one window's close box states each stage's own reason. `RetiredSymbolAuditTests.RebuiltStartupViewSet_IsTheLaunchSurface` and `MachineSetupNoBypassTests` were re-pinned to the new set deliberately.
- [x] **T043** [US4] Choose a stage instead of opening a window · `App.xaml.cs` — DONE (2026-10-04): `ShowLaunchSurface` now maps the outcome through `LaunchStageSelection.For` and shows one stage in the one window; the shell is handed over only from `MainScreens`, and the window's close box is answered by the stage that is showing through `ILaunchStageSurface`. The four window fields and their open/close helpers are gone.
- [x] **T044** [US4] Hide the navigation panel during the launch, maximise on hand-over · `MainWindow.xaml`, `MainWindow.xaml.cs` — DONE (2026-10-04): `MainWindow` now declares a stage host (`ContentPresenter`) that the host fills; the navigation panel exists only inside the shell stage, so it is absent for every launch stage. The window resizes between phases and re-centres after each resize it makes, opens centred on the screen it starts on, and maximises through `WindowPlacementService` only on the move to the shell (`LaunchStageSelection.Maximises`).

**Checkpoint**: one window hosts every launch stage and becomes the shell, maximised, with the shell reachable
only from `MainScreens`.

## Phase 7: User Story 5 - A faster, more resilient launch (P2)

Files: `MTM_Waitlist.Startup/Services/LaunchPipeline.cs`, `MTM_Waitlist.Startup/Services/LaunchStepCatalog.cs`,
`MTM_Waitlist.Startup/Services/LaunchStepGroups.cs`.

### Tests

- [x] **T045** [US5] Test the group members start together and the group ends when all end · `MTM_Waitlist.Tests/Module_Startup/Services/LaunchPipelineConcurrencyTests.cs`
- [x] **T046** [US5] Test a budget overrun ends at Blocked naming the budget · `MTM_Waitlist.Tests/Module_Startup/Services/LaunchBudgetTests.cs`
- [x] **T047** [US5] Test each step still fails at its own maximum while others continue · `MTM_Waitlist.Tests/Module_Startup/Services/LaunchPipelineConcurrencyTests.cs`
- [x] **T048** [US5] Test no launch path calls a cache refresh · `MTM_Waitlist.Tests/Module_Startup/Services/LaunchNoRefreshTests.cs`

### Implementation

**Wave 1 - independent (different files):**

- [x] **T049** [P] [US5] Declare the parallel groups · `MTM_Waitlist.Startup/Services/LaunchStepGroups.cs`
- [x] **T050** [P] [US5] Move `picture-cache` to the first catalogue position · `MTM_Waitlist.Startup/Services/LaunchStepCatalog.cs`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T051** [US5] Run groups concurrently and apply the launch-wide budget · `MTM_Waitlist.Startup/Services/LaunchPipeline.cs`

**Checkpoint**: the session checks and the local reads overlap, the picture cache starts first, the priming
stays last, and a launch that passes the budget stops with a cause.

## Phase 8: User Story 6 - Several lines at once (P3)

Files: `MTM_Waitlist.Startup/ViewModels/LaunchSurfaceViewModel.cs`, `Module_Startup/Views/SplashPage.xaml(.cs)`,
`MTM_Waitlist.Startup/Models/SplashFeedLine.cs`.

### Tests

- [x] **T052** [US6] Test several open lines render at once, each written before its work · `MTM_Waitlist.Tests/Module_Startup/ViewModels/LaunchSurfaceViewModelTests.cs`

### Implementation

**Wave 1 - independent (different files):**

- [x] **T053** [P] [US6] Write the surface view model over the append-only feed · `MTM_Waitlist.Startup/ViewModels/LaunchSurfaceViewModel.cs`
- [x] **T054** [P] [US6] Extend the feed line model for an open line · `MTM_Waitlist.Startup/Models/SplashFeedLine.cs`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T055** [US6] Render every open line · `Module_Startup/Views/SplashPage.xaml`, `Module_Startup/Views/SplashPage.xaml.cs` — DONE (2026-10-04): the surface's rows come from `LaunchSurfaceViewModel.Lines`, one row per operation from the moment it announces itself until the row that ends it arrives, and an open row carries its own moving indicator. `SplashViewModel` still supplies the progress count and the stop's diagnosis; it no longer supplies the line the surface shows.

**Checkpoint**: a concurrent launch shows each operation as its own line.

## Phase 9: User Story 7 - The sixth read shape's proofs (P3)

The shape lands in Foundational; this phase proves parity and keeps the counts honest.

- [x] **T056** [US7] Prove the mirror's row shape matches the live read · `MTM_Waitlist.Tests/Module_Mock/VisualReadFallbackParityTests.cs`
- [x] **T057** [US7] Prove the service's startup validation accepts a catalog of six · `MTM_Waitlist.Tests/Module_Mock_Service/RefreshShapeCatalogProviderTests.cs` — DONE (2026-10-04): the service's own startup-validation case was added — the six-shape catalog validates with `employee_by_number` on the refreshable work list, and a drift in that shape's own mirror projection is reported against that shape alone while the five shipped shapes still refresh.

**Checkpoint**: the sixth shape resolves from the mirror under a cached verdict with live shape parity.

## Phase 10: Polish

- [x] **T058** Reconcile the retired-key-file statement with the code · `.github/instructions/startup-rebuild.instructions.md` — DONE (2026-10-04): the instruction document was the stale side and was corrected to match the code — no key is copied or kept on the computer, the plant's shared key is read from its share, never written or held locally, and its path is never recorded. The spec's FR-035 was corrected as well, because as the pipeline first wrote it, it required the application to stop reading a shared key at all, which would have removed the shipped remembered-sign-in capability. Behaviour is unchanged. Original note: the instruction file says the shared key file is retired because nothing reads a key, while `RememberedSignInService` still reads one through `ISharedKeySource`/`SharedKeyFileSource` (registered in `ModuleDependencyInjectionExtensions`), and FR-035 forbids reading a shared key file while also requiring no behaviour change. Correcting either side is a behaviour or policy change, so it was reported rather than made
- [x] **T059** Reconcile the launch-step contract table with the reordered catalogue · `specs/010-startup-rebuild/contracts/launch-step-contract.md`
- [x] **T060** Update the living spec with this feature's deltas · `capabilities/startup-launch/spec.md`
- [x] **T061** Run the full test suite and the build guards · solution-wide

## Dependencies & Execution Order

- Foundational (Phase 2) blocks every story. Wave 1 is thirteen independent file sets; Wave 2 folds their
  aggregates, registrations, count assertions and tests, and waits for Wave 1.
- US1 (Phase 3) and US2 (Phase 4) are P1. US2 depends on Foundational only. US1's view-model change and US2's
  view-model change both touch `SignInViewModel.cs`; US2's T033 therefore runs after US1's T026.
- US3 (Phase 5) depends on US2's panel existing; its tests come first.
- US4 (Phase 6) is independent of US1 to US3 and can run in parallel with them.
- US5 (Phase 7) depends on Foundational's `LaunchBudget` only.
- US6 (Phase 8) depends on US5 being concurrent, since the several-lines case only arises then.
- US7 (Phase 9) depends on Foundational.
- Polish (Phase 10) runs after every phase, and its suite run (T061) is the single validation point.
