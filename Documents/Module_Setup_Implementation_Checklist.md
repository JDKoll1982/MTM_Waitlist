# Module_Setup Implementation Checklist

This checklist translates the Module_Setup specification into an implementation plan that can be executed incrementally in the current WinUI 3 / MVVM repository structure.

## 0. Project Setup and Module Skeleton
- [x] Create the new Module_Setup module structure under the repository conventions:
  - [x] Module_Setup/Models
  - [x] Module_Setup/Services
  - [x] Module_Setup/ViewModels
  - [x] Module_Setup/Views
  - [x] Module_Setup/Contracts
- [x] Add a module-level migration note or implementation note file for Module_Setup.
- [x] Confirm the module will follow the existing MVVM/DI/navigation pattern used by the other modules.
- [x] Confirm Module_Setup will be a workflow embedded inside the shell navigation rather than a standalone window.
- [x] Add the module to DI registration in the shared service registration pipeline.
- [x] Add the module pages/view models to the page-service mapping.
- [x] Reserve a shell navigation entry and localized resource keys for the new workflow.

## 1. Define the Core Data Model
- [x] Create the primary setup state model that holds the full workflow context:
  - [x] Work order
  - [x] Part selection
  - [x] Sequence selection
  - [x] Dunnage type selection
  - [x] Dunnage part selection
  - [x] Review/confirm state
  - [x] Active-job replacement state
- [x] Create models for:
  - [x] Work order input and normalized WO value
  - [x] Part result / part selection item
  - [x] Sequence result / selected sequence
  - [x] Subordinate part item with category, on-hand quantity, location metadata
  - [x] Dunnage type item
  - [x] Dunnage part item with image path, metadata, selected state
  - [ ] Review summary payload
  - [x] Setup save request / persistence request
- [ ] Define whether the workflow state should be persisted only in memory or also written to app-data / local state during navigation.
- [ ] Ensure models are immutable enough to support step-to-step navigation without state loss.

## 2. Define Contracts and Service Boundaries
- [x] Create interfaces for the workflow services:
  - [x] IWorkOrderValidationService
  - [x] IInforVisualLookupService
  - [x] ISubordinatePartService
  - [x] IDunnageWorkflowService
  - [x] ISetupPersistenceService
  - [x] IActiveJobCoordinatorService
- [x] Create an interface for the workflow-state coordinator if a single controller is used across steps.
- [x] Define the expected return shapes for success, validation error, zero-result, multi-result, and failure cases.
- [x] Define how the UI will receive progress, errors, and selected values from the service layer.

## 3. Implement Work Order Entry and Validation
- [x] Implement WO normalization logic that accepts:
  - [x] 76951
  - [x] 076951
  - [x] WO-076951
- [x] Normalize to a canonical format before any downstream lookup, with expected output being WO-{6DidgetNumber}.
- [x] Implement inline validation rules for invalid input.
- [x] Ensure invalid input returns focus to the WO entry field.
- [x] Implement a search command that triggers the workflow only after validation succeeds.
- [ ] Add UX states for:
  - [ ] idle
  - [ ] loading
  - [ ] valid
  - [ ] invalid
  - [ ] error
- [x] Ensure the Next action is disabled until a valid WO is accepted.

## 4. Implement Infor Visual Lookup Flow
- [x] Create the service that queries Infor Visual for part and sequence data using the normalized WO.
- [x] Persist every lookup request/response definition as checked-in SQL scripts rather than runtime JSON files.
- [x] Handle zero-result behavior with a non-blocking inline error state.
- [x] Handle single-result behavior without forcing user selection.
- [x] Handle multi-result behavior by prompting the user to pick exactly one part.
- [x] Return the connected sequences for the selected WO and part.
- [x] Expose the resolved WO/part/sequence context to the next workflow step.
- [x] Add a fallback error path for unavailable or failed lookup services.
- [x] Keep the lookup path isolated behind the service contract so the UI remains testable.

## 5. Implement Sequence Selection Step
- [x] Create the Step 3 view model and page.
- [x] Display the current WO and part clearly at the top of the page.
- [x] Display available sequences/operations in a selectable list.
- [x] Ensure one selection is required before Continue is enabled.
- [x] Persist the selected sequence into the workflow state object.
- [x] Show a summary panel with WO | Part | Sequence.
- [x] Support Back and Continue navigation.

## 6. Implement Subordinate Part Retrieval and Categorization
- [x] Implement the service that retrieves subordinate parts for the selected part/sequence context.
- [x] Categorize subordinate parts into logical groups such as:
  - [x] dies
  - [x] coils
  - [ ] flatstock
  - [x] components
  - [ ] other
- [x] Preserve the categorized values for operator-request and service use.
- [x] Include quantity, location, and stock state in the model where available.
- [x] Support zero, one, or many dies for a given part/sequence context; show a neutral “No die assigned for this job” state when none exists.
- [x] Prepare the data shape required by the review page.
- [x] Add error handling for missing or malformed subordinate-part payloads.

## 7. Implement Dunnage Workflow (Type -> Part -> Review) IMPORTANT: REVIEW MOCKUPS IN Module_Setup_Spec.md
- [x] Create a workflow state object that carries dunnage context across the step sequence.
- [x] Implement the Step 4 workflow in two logical pages:
  - [x] Page 1: choose dunnage type
  - [x] Page 2: choose dunnage part for the selected type
- [x] Display dunnage types in a card-style selection experience.
- [x] Display dunnage parts in a card-style selection experience with image preview when present.
- [x] Support a filter/search text box if the spec’s UI mockup is used as-is.
- [x] Keep the selected type visually highlighted on Page 1 before advancing.
- [x] Keep the selected part visible in the summary area while navigating.
- [x] Support navigation paths:
  - [x] Back to Types
  - [x] Review
- [x] If no image exists, show the neutral placeholder state.
- [x] Ensure the selected dunnage summary is carried into the review page.
- [x] Mirror receiving dunnage UI (near pixel parity) for:
  - [x] type selection view
  - [x] part selection view
  - [x] review view
  - [x] quick-add type/part dialogs (adapted fields)
- [x] Implement pair assignment controls in setup flow:
  - [x] add selected dunnage part to pair
  - [x] remove assigned dunnage part
  - [x] remove all assigned for selected type
  - [x] clear all assigned for pair
- [x] Allow workflow to continue and save with zero assigned dunnage.
- [x] Role-gate quick add operations by `StartupState.CurrentRole`:
  - [x] Admin
  - [x] Developer
  - [x] Plant Manager
  - [x] Setup Lead
  - [x] Production Lead

## 8. Implement Review and Confirm Step
- [x] Create the Step 5 review page and view model.
- [x] Display:
  - [x] Work order
  - [x] Part
  - [x] Sequence
  - [x] Coil context
  - [x] Die context with zero/one/many dies and the no-die fallback state
  - [x] Component parts list
  - [x] On-hand quantities and stock-status messaging
  - [x] Selected dunnage summary
- [x] Provide Edit and Confirm actions.
- [x] Ensure the review page is the last confirmation checkpoint before persistence.
- [x] Make the Confirm action trigger the save/cleanup path.

## 9. Implement Save/Cleanup and Active Job Replacement
- [x] Implement the save workflow that persists the setup state for downstream use.
- [x] Check whether an active job already exists for the workstation/work center.
- [x] Show a confirmation dialog when an existing active job would be replaced.
- [x] Support Cancel without replacing the existing job.
- [x] Support Replace and continue with the new setup state.
- [ ] Update the workstation job details once the new setup is saved.
- [ ] Ensure cleanup and persistence are idempotent and safe for repeat runs.

## 10. Implement Helper-Server Routing and Mock Data Support
- [x] Add app-data-backed mock-data toggles:
  - [x] `Feature.InforVisualMockData` (default On)
  - [x] `Feature.RecvMockData` (default Off)
- [x] Create a read-only helper server for queue-style or lookup-style actions.
- [x] Create a read/write helper server for save/update-style actions.
- [x] Implement the shared routing rule:
  - [x] SearchButton / action entry -> helper server -> mock-data check -> sample-data service -> requested action
- [x] When mock data is enabled, halt backend execution and use sample data.
- [x] When mock data is disabled, continue to the real backend path.
- [x] Ensure the helper-server behavior is DI-registered and uses the shared settings service.
    Examples for Mock Data:
        Coils: MMC0001000, MMC0000365, MMC0000056, MMCCS00365, MMCSR00365
        Flat-Stock: MMF0001154, MMF0000300, MMFCS01145, MMFSR00456
        Dies: FGT-001 (No Die For this Job)

## 11. Implement MySQL-Backed Persistence Layer
- [ ] Define the persistence contract for dunnage and setup data in the correct database boundary.
- [ ] Map the app model to the receiving-application-style MySQL entities in mtm_receiving_application and mtm_waitlist:
  - [ ] dunnage types
  - [ ] dunnage parts
  - [ ] active label data queue
  - [ ] history records
- [ ] Place the dunnage script files under Database/MTMReceivingApp/Queues/Module_Setup/Queues.
- [ ] Place the setup save stored procedure under Database/StoredProcedures/sp_setup_save_setup/create.sql.
- [ ] Keep the rollback script for setup save at Database/StoredProcedures/sp_setup_save_setup/rollback.sql.
- [ ] Create any repository/service classes needed to read and write these entities.
- [ ] Ensure explicit confirmation is required before overwriting preloaded values.
- [ ] Add error handling for database connectivity, missing rows, and save conflicts.
- [ ] Add logging around persistence operations.
- [x] Add receiving-db quick-add operations for dunnage definitions:
  - [x] quick add type -> `sp_Dunnage_Types_Insert`
  - [x] quick add part -> `sp_Dunnage_Parts_Insert`

## 12. Integrate Navigation, Shell, and DI
- [x] Create the view models and pages for the workflow steps.
- [x] Register the new view models and pages in the shared DI container.
- [x] Register the workflow services and repositories in the DI container.
- [x] Add the workflow to shell navigation.
- [x] Ensure navigation stays integrated with the current shell/navigation services.
- [x] Ensure the workflow can move forward and backward without losing context.
- [x] Ensure the app uses the existing navigation-service conventions rather than parallel navigation stacks.

## 13. Implement UX and State Handling
- [x] Create the step-based layout with persistent header and progress state.
- [x] Ensure the current step and overall completion percentage are visible.
- [x] Ensure each step keeps the user context visible.
- [ ] Implement loading indicators where backend lookups are in progress.
- [x] Implement error banners / inline error messages for validation and lookup failures.
- [ ] Implement confirmation dialogs for replacement and overwrite scenarios.
- [ ] Make the UI accessible and readable with consistent spacing and contrast.

## 14. Localization and Resource Integration
- [x] Add localized strings for:
  - [x] page titles
  - [x] field labels
  - [x] validation messages
  - [x] action buttons
  - [x] confirmation dialogs
  - [x] error states
- [x] Use the existing resource-system pattern instead of hardcoded UI strings.
- [ ] Ensure the new workflow uses localized content for all visible UI text.

## 15. Add Tests
- [x] Add unit tests for WO normalization and validation.
- [x] Add tests for part/sequence lookup behavior.
- [x] Add tests for subordinate-part categorization.
- [x] Add tests for dunnage workflow state transitions.
- [x] Add tests for review-summary generation.
- [x] Add tests for helper-server mock-data short-circuit behavior.
- [x] Add tests for save/cleanup replacement confirmation behavior.
- [x] Add tests for persistence service behavior using a fake or in-memory implementation.

## 16. Validation and Acceptance Checklist
- [ ] A user can enter a valid WO and complete the setup flow without error.
- [x] An invalid WO shows inline validation feedback and returns focus to the field.
- [x] Zero, one, and multi-part situations are handled correctly.
- [x] Sequence selection is preserved through review and confirm.
- [x] Dunnage type and dunnage part selection behave as a guided workflow.
- [x] Review and confirm shows all expected summary information.
- [x] Existing active-job replacement is guarded by confirmation.
- [ ] The flow reads from and writes to the intended data sources without hardcoded secrets.
- [x] Mock data is respected when the module-specific setting is enabled (`Feature.InforVisualMockData` or `Feature.RecvMockData`).
- [x] The implementation builds successfully and the relevant tests pass.

## 17. Suggested Delivery Order
- [x] Start with the models and workflow-state object.
- [x] Implement validation and lookup services next.
- [x] Build the step-by-step views and view models.
- [x] Wire the helper-server and mock-data flow.
- [x] Add persistence and save/cleanup logic.
- [x] Finish with localization, tests, and build validation.
