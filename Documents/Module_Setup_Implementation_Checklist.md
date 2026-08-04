# Module_Setup Implementation Checklist

This checklist translates the Module_Setup specification into an implementation plan that can be executed incrementally in the current WinUI 3 / MVVM repository structure.

## 0. Project Setup and Module Skeleton
- [ ] Create the new Module_Setup module structure under the repository conventions:
  - [ ] Module_Setup/Models
  - [ ] Module_Setup/Services
  - [ ] Module_Setup/ViewModels
  - [ ] Module_Setup/Views
  - [ ] Module_Setup/Contracts
- [ ] Add a module-level migration note or implementation note file for Module_Setup.
- [ ] Confirm the module will follow the existing MVVM/DI/navigation pattern used by the other modules.
- [x] Confirm Module_Setup will be a workflow embedded inside the shell navigation rather than a standalone window.
- [ ] Add the module to DI registration in the shared service registration pipeline.
- [ ] Add the module pages/view models to the page-service mapping.
- [ ] Reserve a shell navigation entry and localized resource keys for the new workflow.

## 1. Define the Core Data Model
- [ ] Create the primary setup state model that holds the full workflow context:
  - [ ] Work order
  - [ ] Part selection
  - [ ] Sequence selection
  - [ ] Dunnage type selection
  - [ ] Dunnage part selection
  - [ ] Review/confirm state
  - [ ] Active-job replacement state
- [ ] Create models for:
  - [ ] Work order input and normalized WO value
  - [ ] Part result / part selection item
  - [ ] Sequence result / selected sequence
  - [ ] Subordinate part item with category, on-hand quantity, location metadata
  - [ ] Dunnage type item
  - [ ] Dunnage part item with image path, metadata, selected state
  - [ ] Review summary payload
  - [ ] Setup save request / persistence request
- [ ] Define whether the workflow state should be persisted only in memory or also written to app-data / local state during navigation.
- [ ] Ensure models are immutable enough to support step-to-step navigation without state loss.

## 2. Define Contracts and Service Boundaries
- [ ] Create interfaces for the workflow services:
  - [ ] IWorkOrderValidationService
  - [ ] IInforVisualLookupService
  - [ ] ISubordinatePartService
  - [ ] IDunnageWorkflowService
  - [ ] ISetupPersistenceService
  - [ ] IActiveJobCoordinatorService
- [ ] Create an interface for the workflow-state coordinator if a single controller is used across steps.
- [ ] Define the expected return shapes for success, validation error, zero-result, multi-result, and failure cases.
- [ ] Define how the UI will receive progress, errors, and selected values from the service layer.

## 3. Implement Work Order Entry and Validation
- [ ] Implement WO normalization logic that accepts:
  - [ ] 76951
  - [ ] 076951
  - [ ] WO-076951
- [ ] Normalize to a canonical format before any downstream lookup, with expected output being WO-{6DidgetNumber}.
- [ ] Implement inline validation rules for invalid input.
- [ ] Ensure invalid input returns focus to the WO entry field.
- [ ] Implement a search command that triggers the workflow only after validation succeeds.
- [ ] Add UX states for:
  - [ ] idle
  - [ ] loading
  - [ ] valid
  - [ ] invalid
  - [ ] error
- [ ] Ensure the Next action is disabled until a valid WO is accepted.

## 4. Implement Infor Visual Lookup Flow
- [ ] Create the service that queries Infor Visual for part and sequence data using the normalized WO.
- [ ] Persist every lookup request/response to a SQL queue file under Database/InforVisual/Queues/Module and read the queue file back when replaying or rehydrating lookup context.
- [ ] Handle zero-result behavior with a non-blocking inline error state.
- [ ] Handle single-result behavior without forcing user selection.
- [ ] Handle multi-result behavior by prompting the user to pick exactly one part.
- [ ] Return the connected sequences for the selected WO and part.
- [ ] Expose the resolved WO/part/sequence context to the next workflow step.
- [ ] Add a fallback error path for unavailable or failed lookup services.
- [ ] Keep the lookup path isolated behind the service contract so the UI remains testable.

## 5. Implement Sequence Selection Step
- [ ] Create the Step 3 view model and page.
- [ ] Display the current WO and part clearly at the top of the page.
- [ ] Display available sequences/operations in a selectable list.
- [ ] Ensure one selection is required before Continue is enabled.
- [ ] Persist the selected sequence into the workflow state object.
- [ ] Show a summary panel with WO | Part | Sequence.
- [ ] Support Back and Continue navigation.

## 6. Implement Subordinate Part Retrieval and Categorization
- [ ] Implement the service that retrieves subordinate parts for the selected part/sequence context.
- [ ] Categorize subordinate parts into logical groups such as:
  - [ ] dies
  - [ ] coils
  - [ ] flatstock
  - [ ] components
  - [ ] other
- [ ] Preserve the categorized values for operator-request and service use.
- [ ] Include quantity, location, and stock state in the model where available.
- [ ] Support zero, one, or many dies for a given part/sequence context; show a neutral “No die assigned for this job” state when none exists.
- [ ] Prepare the data shape required by the review page.
- [ ] Add error handling for missing or malformed subordinate-part payloads.

## 7. Implement Dunnage Workflow (Type -> Part -> Review) IMPORTANT: REVIEW MOCKUPS IN Module_Setup_Spec.md
- [ ] Create a workflow state object that carries dunnage context across the step sequence.
- [ ] Implement the Step 4 workflow in two logical pages:
  - [ ] Page 1: choose dunnage type
  - [ ] Page 2: choose dunnage part for the selected type
- [ ] Display dunnage types in a card-style selection experience.
- [ ] Display dunnage parts in a card-style selection experience with image preview when present.
- [ ] Support a filter/search text box if the spec’s UI mockup is used as-is.
- [ ] Keep the selected type visually highlighted on Page 1 before advancing.
- [ ] Keep the selected part visible in the summary area while navigating.
- [ ] Support navigation paths:
  - [ ] Back to Types
  - [ ] Review
- [ ] If no image exists, show the neutral placeholder state.
- [ ] Ensure the selected dunnage summary is carried into the review page.

## 8. Implement Review and Confirm Step
- [ ] Create the Step 5 review page and view model.
- [ ] Display:
  - [ ] Work order
  - [ ] Part
  - [ ] Sequence
  - [ ] Coil context
  - [ ] Die context with zero/one/many dies and the no-die fallback state
  - [ ] Component parts list
  - [ ] On-hand quantities and stock-status messaging
  - [ ] Selected dunnage summary
- [ ] Provide Edit and Confirm actions.
- [ ] Ensure the review page is the last confirmation checkpoint before persistence.
- [ ] Make the Confirm action trigger the save/cleanup path.

## 9. Implement Save/Cleanup and Active Job Replacement
- [ ] Implement the save workflow that persists the setup state for downstream use.
- [ ] Check whether an active job already exists for the workstation/work center.
- [ ] Show a confirmation dialog when an existing active job would be replaced.
- [ ] Support Cancel without replacing the existing job.
- [ ] Support Replace and continue with the new setup state.
- [ ] Update the workstation job details once the new setup is saved.
- [ ] Ensure cleanup and persistence are idempotent and safe for repeat runs.

## 10. Implement Helper-Server Routing and Mock Data Support
- [ ] Add or reuse the app-data-backed mock-data toggle named `Feature.UseMockData`.
- [ ] Create a read-only helper server for queue-style or lookup-style actions.
- [ ] Create a read/write helper server for save/update-style actions.
- [ ] Implement the shared routing rule:
  - [ ] SearchButton / action entry -> helper server -> mock-data check -> sample-data service -> requested action
- [ ] When mock data is enabled, halt backend execution and use sample data.
- [ ] When mock data is disabled, continue to the real backend path.
- [ ] Ensure the helper-server behavior is DI-registered and uses the shared settings service.
    Examples for Mock Data:
        Coils: MMC0001000, MMC0000365, MMC0000056, MMCCS00365, MMCSR00365
        Flat-Stock: MMF0001154, MMF0000300, MMFCS01145, MMFSR00456
        Dies: FGT-001 (No Die For this Job)

## 11. Implement MySQL-Backed Persistence Layer
- [ ] Define the persistence contract for dunnage and setup data.
- [ ] Map the app model to the receiving-application-style MySQL entities:
  - [ ] dunnage types
  - [ ] dunnage parts
  - [ ] active label data queue
  - [ ] history records
- [ ] Create any repository/service classes needed to read and write these entities.
- [ ] Ensure explicit confirmation is required before overwriting preloaded values.
- [ ] Add error handling for database connectivity, missing rows, and save conflicts.
- [ ] Add logging around persistence operations.

## 12. Integrate Navigation, Shell, and DI
- [ ] Create the view models and pages for the workflow steps.
- [ ] Register the new view models and pages in the shared DI container.
- [ ] Register the workflow services and repositories in the DI container.
- [ ] Add the workflow to shell navigation.
- [ ] Ensure navigation stays integrated with the current shell/navigation services.
- [ ] Ensure the workflow can move forward and backward without losing context.
- [ ] Ensure the app uses the existing navigation-service conventions rather than parallel navigation stacks.

## 13. Implement UX and State Handling
- [ ] Create the step-based layout with persistent header and progress state.
- [ ] Ensure the current step and overall completion percentage are visible.
- [ ] Ensure each step keeps the user context visible.
- [ ] Implement loading indicators where backend lookups are in progress.
- [ ] Implement error banners / inline error messages for validation and lookup failures.
- [ ] Implement confirmation dialogs for replacement and overwrite scenarios.
- [ ] Make the UI accessible and readable with consistent spacing and contrast.

## 14. Localization and Resource Integration
- [ ] Add localized strings for:
  - [ ] page titles
  - [ ] field labels
  - [ ] validation messages
  - [ ] action buttons
  - [ ] confirmation dialogs
  - [ ] error states
- [ ] Use the existing resource-system pattern instead of hardcoded UI strings.
- [ ] Ensure the new workflow uses localized content for all visible UI text.

## 15. Add Tests
- [ ] Add unit tests for WO normalization and validation.
- [ ] Add tests for part/sequence lookup behavior.
- [ ] Add tests for subordinate-part categorization.
- [ ] Add tests for dunnage workflow state transitions.
- [ ] Add tests for review-summary generation.
- [ ] Add tests for helper-server mock-data short-circuit behavior.
- [ ] Add tests for save/cleanup replacement confirmation behavior.
- [ ] Add tests for persistence service behavior using a fake or in-memory implementation.

## 16. Validation and Acceptance Checklist
- [ ] A user can enter a valid WO and complete the setup flow without error.
- [ ] An invalid WO shows inline validation feedback and returns focus to the field.
- [ ] Zero, one, and multi-part situations are handled correctly.
- [ ] Sequence selection is preserved through review and confirm.
- [ ] Dunnage type and dunnage part selection behave as a guided workflow.
- [ ] Review and confirm shows all expected summary information.
- [ ] Existing active-job replacement is guarded by confirmation.
- [ ] The flow reads from and writes to the intended data sources without hardcoded secrets.
- [ ] Mock data is respected when `Feature.UseMockData` is enabled.
- [ ] The implementation builds successfully and the relevant tests pass.

## 17. Suggested Delivery Order
- [ ] Start with the models and workflow-state object.
- [ ] Implement validation and lookup services next.
- [ ] Build the step-by-step views and view models.
- [ ] Wire the helper-server and mock-data flow.
- [ ] Add persistence and save/cleanup logic.
- [ ] Finish with localization, tests, and build validation.
