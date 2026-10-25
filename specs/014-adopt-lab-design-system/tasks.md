# Tasks: One set of rules for every screen, window and dialog

**Feature**: `014-adopt-lab-design-system`  
**Plan**: [plan.md](./plan.md) · **Spec**: [spec.md](./spec.md)  
**Size**: oversized

Every task is one unit of verified work. A task is ticked only when its change builds clean and the suite is green.

## Phase 1 — Shared foundation (blocks everything)

The pieces every later task calls. Nothing in Phase 2 can start until these compile.

- [x] **T001** [P] Add the DevWinUI package reference to the shared project so the windowed host can live there `MTM_Waitlist.Shared/MTM_Waitlist.Shared.csproj`
- [x] **T002** [P] Declare the five dialog sizing keys and the message chrome as a new resource dictionary `Styles/Dialogs.xaml`
- [x] **T003** Merge that dictionary in the application's resources, in the same change that adds it `App.xaml`
- [x] **T004** [P] Define the six message kinds as one closed enum `MTM_Waitlist.Shared/Dialogs/MessageKind.cs`
- [x] **T005** [P] Define what a screen asks for, with its validation `MTM_Waitlist.Shared/Dialogs/MessageDialogRequest.cs`
- [x] **T006** [P] Define what comes back, one shape for every kind `MTM_Waitlist.Shared/Dialogs/MessageDialogOutcome.cs`
- [x] **T007** Build the one host that ties a dialog to its owner window `MTM_Waitlist.Shared/Dialogs/WindowedDialogHost.cs`
- [x] **T008** Build the standard message body: title, message, subject, and nothing the caller can reorder `MTM_Waitlist.Shared/Dialogs/MessageDialogContent.xaml(.cs)`
- [x] **T009** Build the single entry point that maps a kind to its actions, default and answer `MTM_Waitlist.Shared/Dialogs/MessageDialogHost.cs`

## Phase 2 — Convert the raise sites (14 files, 18 sites)

Each task converts one file's dialog constructions to the shared set, keeping its behaviour.

- [x] **T010** Convert the shell's sign-out question `Module_Core/Views/ShellPage.xaml.cs`
- [x] **T011** Convert the edit-user page's failure report and leave prompt `Module_Settings/Views/EditUserPage.xaml.cs`
- [x] **T012** Convert the picture override editor's failure report `Module_Settings/Views/ImageOverrideEditorControl.xaml.cs`
- [x] **T013** Convert the permissions page's failure report `Module_Settings/Views/PermissionsPage.xaml.cs`
- [x] **T014** Convert the work-centre picture dialog's failure report `Module_Settings/Views/WorkCenterImagesDialog.xaml.cs`
- [x] **T015** Convert the dunnage type page's scrap-type and clear-all prompts `Module_Setup/Views/SetupDunnageTypePage.xaml.cs`
- [x] **T016** Convert the work-centre page's workstation prompts `Module_Setup/Views/SetupWorkCenterPage.xaml.cs`
- [x] **T017** Convert the blocked state page's message `Module_Startup/Views/BlockedStatePage.xaml.cs`
- [x] **T018** Convert the machine setup page's message `Module_Startup/Views/MachineSetupPage.xaml.cs`
- [x] **T019** Convert the waitlist action prompt's three questions `Services/WaitlistRequestActionPrompt.cs`
- [x] **T020** Convert the setup dialog service's no-dunnage question `Services/SetupDialogService.cs`
- [x] **T021** Convert the navigation service's leave-with-changes question `Services/NavigationViewService.cs`
- [x] **T022** Convert the inspector's print message `Module_Shared/Views/ControlInspectorDetailWindow.xaml.cs`
- [x] **T023** Reference the shared project from the service console and convert its one message `MTM_Waitlist.Mock.Service/`

## Phase 3 — Layout, sizing and automation conformance

- [x] **T024** Point the three pages that still carry a literal column width at the shared cap `Module_Settings/Views/CreateUserPage.xaml`, `EditUserPage.xaml`, `LuggerArrangementPage.xaml`
- [x] **T025** Give the control inspector proportional columns and shared type sizes `Module_Shared/Views/ControlInspectorDetailWindow.xaml`
- [x] **T026** Move the four dialog files' own hard-coded widths onto the shared dialog sizing keys `Module_Settings/Views/ComputerEditDialog.xaml`, `RequestItemImagesDialog.xaml`, `WorkCenterImagesDialog.xaml`, `Module_Setup/Views/SetupDunnageImageSearchDialog.xaml`, `SetupWorkOrderSearchDialog.xaml`, `Module_Waitlist/Views/PartStockDialog.xaml`
- [x] **T027** Sweep every surface for the platform tooltip property, so none bypasses the shared behaviour and the inventory covers resource-only files too `Styles/`, `Module_Mock/Views/`, every module's Views

## Phase 4 — Audits and verification (the gates)

Each audit fails the build, so a later change cannot undo this work quietly.

- [x] **T028** Audit that no screen builds a message dialog of its own `MTM_Waitlist.Tests/Module_Shared/Dialogs/MessageDialogAdoptionTests.cs`
- [x] **T029** Audit the kind-to-action contract: order, default and Escape answer for all six `MTM_Waitlist.Tests/Module_Shared/Dialogs/MessageDialogContractTests.cs`
- [x] **T030** Audit that no dialog declares a width or height of its own `MTM_Waitlist.Tests/Module_Shared/Dialogs/DialogSizingAuditTests.cs`
- [x] **T031** Extend the page-content audit so every page and resource-only file is listed and checked `MTM_Waitlist.Tests/Module_Core/Views/PageContentWidthAuditTests.cs`
- [x] **T032** Write the inventory of every surface, with kind, area and modal flag `docs/guides/guides-xaml-surface-inventory.md`
- [x] **T033** Audit that the inventory matches the files on disk in both directions `MTM_Waitlist.Tests/Module_Core/Views/SurfaceInventoryAuditTests.cs`
- [x] **T034** Clean x64 build with zero warnings `MTM_Waitlist.csproj`
- [x] **T035** Green unit suite, including every audit above `MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj`

## Dependencies

- T001–T009 block T010–T023.
- T007 and T009 block T010–T023; T008 blocks T009.
- T030 needs T002 and T026 done.
- T033 needs T032.
- T034 and T035 close the run and need everything before them.

## Out of scope

- Flyouts and the picture enlarge overlay. They are anchored surfaces, not dialogs.
- The operator application's flows, wording and answers, which are preserved rather than redesigned.
