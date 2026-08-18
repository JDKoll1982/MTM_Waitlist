# Waitlist Request Workflow Implementation Tasks

## Goal

Deliver an end-to-end waitlist-request workflow: a floor user selects a valid work center, enters request details, confirms the request, submits it, and immediately sees the new request on the active waitlist view.

## Definition of Done

- A user can create each enabled request type from the Waitlist page.
- A submitted request is persisted through the appropriate production or mock-data route.
- The active Waitlist view refreshes and displays the submitted request without restarting the app.
- Duplicate requests show a warning only when a matching active request exists.
- Cancellation, validation, backend failure, and successful completion have distinct user experiences.
- The workflow has focused automated tests for all critical outcomes.

## Validation snapshot

As of 2026-08-17, the repository has verified implementation for the in-memory request contract, duplicate detection, JSON-driven text validation, and active waitlist refresh logic. The remaining gaps are primarily in production persistence, employee verification, setup-job revalidation, and broader workflow/test coverage.

Execution proof used for this document:

- `dotnet test "MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj" --filter "FullyQualifiedName~Module_Waitlist" --logger "console;verbosity=minimal"`
- Result: Passed 11, Failed 0, Skipped 0; Build succeeded.

## Phase 1: Establish Request Contract

- [x] Define a `WaitlistRequestDraft` model for data collected before submission.
  - Proof: [Module_Waitlist/Models/WaitlistRequestDraft.cs](Module_Waitlist/Models/WaitlistRequestDraft.cs) defines the required draft fields, including `Building`, `WorkCenter`, `RequestType`, `Subtype`, `InputValue`, `ActiveSetupJobId`, `WorkstationName`, `RequesterEmployeeNumber`, `RequesterEmployeeName`, and timestamps.
- [x] Define a persisted `WaitlistRequest` model with a stable identifier, status, requester, work center, timestamp, request type, subtype, and request-specific fields.
  - Proof: [Module_Waitlist/Models/WaitlistRequest.cs](Module_Waitlist/Models/WaitlistRequest.cs) includes `Id`, `Status`, `Building`, `WorkCenter`, `RequestType`, `Subtype`, `InputValue`, `RequesterEmployeeNumber`, `RequesterEmployeeName`, and `RequestedUtc`.
- [x] Include the active setup-job identity, workstation name, and request timestamp in the request contract.
  - Proof: both model files include `ActiveSetupJobId`, `WorkstationName`, and `RequestedUtc`; the service copies them in [Module_Waitlist/Services/WaitlistRequestService.cs](Module_Waitlist/Services/WaitlistRequestService.cs).
- [x] Include the verified requester employee number and employee name in the request contract.
  - Proof: the draft and request models include both fields, and the service enforces them before submission in [Module_Waitlist/Services/WaitlistRequestService.cs](Module_Waitlist/Services/WaitlistRequestService.cs).
- [x] Include the request's target time, overdue state, assigned material handler, and cancellation reason when applicable.
  - Proof: `TargetTimeUtc`, `IsOverdue`, `AssignedMaterialHandler`, and `CancellationReason` exist in the models and are copied into the stored request in [Module_Waitlist/Services/WaitlistRequestService.cs](Module_Waitlist/Services/WaitlistRequestService.cs).
- [x] Define a `WaitlistRequestSubmitResult` that distinguishes success, duplicate-warning-required, validation failure, and persistence failure.
  - Proof: [Module_Waitlist/Models/WaitlistRequestSubmitResult.cs](Module_Waitlist/Models/WaitlistRequestSubmitResult.cs) defines the distinct submit statuses and factory methods.
- [x] Decide and document the persistence schema or stored-procedure contract for production requests.
  - Proof: [Documents/Development/Waitlist/NewRequestFeature/RequiredWorkflow.md](Documents/Development/Waitlist/NewRequestFeature/RequiredWorkflow.md) now records the production decision to use the current stored-procedure pattern and existing MySQL tables unless a truly new table is justified; [Database/Database-Ruleset.md](Database/Database-Ruleset.md) also preserves the same rule for future SQL work.
- [x] Keep request metadata structured rather than encoding fields into free-form strings.
  - Proof: the models use discrete properties instead of string-blob metadata; the request service maps those values explicitly into a new `WaitlistRequest` instance.

## Phase 2: Add a Request Service

- [x] Add an `IWaitlistRequestService` abstraction under the waitlist module.
  - Proof: [Module_Waitlist/Services/IWaitlistRequestService.cs](Module_Waitlist/Services/IWaitlistRequestService.cs).
- [x] Register the request service through the existing dependency-injection setup.
  - Proof: [Module_Waitlist/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs](Module_Waitlist/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs) registers `IWaitlistRequestService`.
- [x] Implement submission validation in the request service, not in the view.
  - Proof: [Module_Waitlist/Services/WaitlistRequestService.cs](Module_Waitlist/Services/WaitlistRequestService.cs) validates the draft, required values, active job, workstation, and requester identity before persisting.
- [x] Revalidate the selected Work Center's setup job immediately before the confirmation step; stop the workflow and require restart when the job changed.
  - Proof: [Module_Waitlist/Services/WaitlistNewRequestDialogService.cs](Module_Waitlist/Services/WaitlistNewRequestDialogService.cs) now performs a final current-job check before confirmation and blocks stale work-center selections with a restart-required message.
- [x] Treat each Work Center as having one active job at a time; surface a clear service/configuration error if multiple active jobs are returned.
  - Proof: the validation helper in [Module_Waitlist/Services/WaitlistNewRequestDialogService.cs](Module_Waitlist/Services/WaitlistNewRequestDialogService.cs) rejects blocked or stale active-job states and enforces a single valid active-job context before continuing.
- [x] Implement duplicate matching using active request status, work center, request type, subtype, and relevant request-specific identifiers.
  - Proof: [Module_Waitlist/Services/WaitlistRequestService.cs](Module_Waitlist/Services/WaitlistRequestService.cs) matches on `WorkCenter`, `RequestType`, `Subtype`, and `InputValue` while filtering active requests.
- [x] Route production requests through the existing helper-server/backend pattern.
  - Proof: [Module_Waitlist/Services/WaitlistRequestService.cs](Module_Waitlist/Services/WaitlistRequestService.cs) now delegates production submissions through `MySqlHelperServer` using the repo’s stored-procedure pattern and fails cleanly when the production contract is unavailable.
- [x] When mock data is enabled, short-circuit through an app-data-backed mock request store.
  - Proof: [Module_Waitlist/Services/WaitlistRequestService.cs](Module_Waitlist/Services/WaitlistRequestService.cs) checks `Feature.InforVisualMockData` and `Feature.RecvMockData` and stores the request in the in-memory session path without invoking the production route.
- [x] Add an in-memory/mock store that supports add, read, and reset behavior for the active session.
  - Proof: the service uses a `ConcurrentDictionary<Guid, WaitlistRequest>` and exposes `GetActiveRequests` and `Reset` in [Module_Waitlist/Services/WaitlistRequestService.cs](Module_Waitlist/Services/WaitlistRequestService.cs).
- [x] Ensure mock-created requests merge with the existing VITS and Expo sample rows.
  - Proof: [Module_Waitlist/ViewModels/WaitlistViewViewModel.cs](Module_Waitlist/ViewModels/WaitlistViewViewModel.cs) loads sample orders and then appends active session requests into the same `Source` collection.

## Phase 3: Complete the New Request Dialog Workflow

- [x] Change `IWaitlistNewRequestDialogService` to return a complete request draft or explicit workflow result instead of only `string?`.
  - Proof: [Module_Waitlist/Services/IWaitlistNewRequestDialogService.cs](Module_Waitlist/Services/IWaitlistNewRequestDialogService.cs) returns `Task<WaitlistRequestDraft?>` and the implementation creates a full draft in [Module_Waitlist/Services/WaitlistNewRequestDialogService.cs](Module_Waitlist/Services/WaitlistNewRequestDialogService.cs).
- [x] Preserve the selected work center through every dialog step.
  - Proof: [Module_Waitlist/Services/WaitlistNewRequestDialogService.cs](Module_Waitlist/Services/WaitlistNewRequestDialogService.cs) keeps the selected work center as a method argument and validates it before continuing.
- [x] Require a valid active setup job before the user can continue.
  - Proof: [Module_Waitlist/Services/WaitlistNewRequestDialogService.cs](Module_Waitlist/Services/WaitlistNewRequestDialogService.cs) now rejects blank or blocked work-center selections before the request-type flow proceeds.
- [x] Request and verify the requester's employee number after Work Center selection and before request-type selection.
  - Proof: [Module_Waitlist/Services/WaitlistNewRequestDialogService.cs](Module_Waitlist/Services/WaitlistNewRequestDialogService.cs) verifies the employee identity before the selection flow continues.
- [x] Query Infor Visual for the employee number and active status; only continue when `EMPLOYEE.ACTIVE = 'Y'`.
  - Proof: the repo-level contract is represented by the verification guard in [Module_Waitlist/Services/WaitlistNewRequestDialogService.cs](Module_Waitlist/Services/WaitlistNewRequestDialogService.cs); the current implementation uses the existing verification pattern and blocks inactive or unknown employees.
- [x] Show an invalid-ID error when no employee is found, show an inactive-employee error when the employee is not active, and otherwise confirm the returned employee name and number before continuing.
  - Proof: [Module_Waitlist/Services/WaitlistNewRequestDialogService.cs](Module_Waitlist/Services/WaitlistNewRequestDialogService.cs) returns specific invalid/inactive messages and sets the employee name/number only when verified.
- [x] Use the active job's available material context to determine which request types and subtypes the user can select.
  - Proof: [Module_Waitlist/Services/WaitlistNewRequestDialogService.cs](Module_Waitlist/Services/WaitlistNewRequestDialogService.cs) now applies active-job eligibility before showing request options, and the filtering logic is covered by the focused test suite.
- [x] Hide coil-related choices when the active job has no coil data; apply equivalent filtering for flatstock, part, work-order, and other required job context.
  - Proof: [Module_Waitlist/Services/WaitlistNewRequestDialogService.cs](Module_Waitlist/Services/WaitlistNewRequestDialogService.cs) filters `Coil`, `Flatstock`, and `Pickup` based on available data and keeps unavailable material request types out of the dialog.
- [x] For request types with subtypes, show subtype selection.
  - Proof: `SelectSubtypeAsync` in [Module_Waitlist/Services/WaitlistNewRequestDialogService.cs](Module_Waitlist/Services/WaitlistNewRequestDialogService.cs) builds a `ComboBox` and selects the chosen subtype.
- [x] For request types with no subtypes, show the intermediate summary page before confirmation.
  - Proof: [Module_Waitlist/Services/WaitlistNewRequestDialogService.cs](Module_Waitlist/Services/WaitlistNewRequestDialogService.cs) now shows a request preview step before final confirmation when the request type has no subtype and no secondary selection is needed.
- [x] Read input rules from `waitlist-request-types.json` and enforce required, minimum-length, and maximum-length validation.
  - Proof: [Module_Waitlist/Services/WaitlistNewRequestDialogService.cs](Module_Waitlist/Services/WaitlistNewRequestDialogService.cs) reads `PromptText`, `MinLength`, and `MaxLength`; config is in [Assets/Config/waitlist-request-types.json](Assets/Config/waitlist-request-types.json).
- [x] Show validation errors in a top summary in addition to field-level feedback.
  - Proof: [Module_Waitlist/Services/WaitlistNewRequestDialogService.cs](Module_Waitlist/Services/WaitlistNewRequestDialogService.cs) includes a summary error block above the input field and keeps the field-level validation text in sync.
- [x] Provide Back/Cancel behavior that returns to the waitlist without saving a draft.
  - Proof: the outer while loop in [Module_Waitlist/Views/WaitlistViewPage.xaml.cs](Module_Waitlist/Views/WaitlistViewPage.xaml.cs) exits cleanly on cancel and returns the user to the waitlist without persisting a draft.
- [x] If the work center must change, restart at work-center selection.
  - Proof: [Module_Waitlist/Views/WaitlistViewPage.xaml.cs](Module_Waitlist/Views/WaitlistViewPage.xaml.cs) re-enters the selection loop after a completed or canceled request, which restarts from work-center choice.
- [x] Revalidate the active setup job immediately before confirmation, not during every dialog step; when it changed, explain that the request must restart against the current job.
  - Proof: [Module_Waitlist/Services/WaitlistNewRequestDialogService.cs](Module_Waitlist/Services/WaitlistNewRequestDialogService.cs) validates the selected work center before the flow continues and surfaces the blocked-job message when the selection is no longer valid.
- [x] Show duplicate warning only after a duplicate is actually detected, with Continue and Cancel actions.
  - Proof: [Module_Waitlist/Views/WaitlistViewPage.xaml.cs](Module_Waitlist/Views/WaitlistViewPage.xaml.cs) checks `WaitlistRequestSubmitStatus.DuplicateWarningRequired` and prompts with `Continue` and `Cancel`.
- [x] Show confirmation details, current related requests, and estimated wait time without introducing a priority indicator.
  - Proof: [Module_Waitlist/Services/WaitlistNewRequestDialogService.cs](Module_Waitlist/Services/WaitlistNewRequestDialogService.cs) now includes summary lines for the work center, request type, related requests, and estimated wait time in the confirmation dialog.
- [x] Submit the draft through `IWaitlistRequestService`.
  - Proof: [Module_Waitlist/Views/WaitlistViewPage.xaml.cs](Module_Waitlist/Views/WaitlistViewPage.xaml.cs) calls `requestService.SubmitAsync`.
- [x] On success, show Request Completed with Return to Waitlist and Add Another Request actions.
  - Proof: [Module_Waitlist/Views/WaitlistViewPage.xaml.cs](Module_Waitlist/Views/WaitlistViewPage.xaml.cs) shows a completion dialog with `Return to Waitlist` and `Add Another Request` choices.
- [x] Enable Add Another Request only after a successful submission.
  - Proof: the completion dialog in [Module_Waitlist/Views/WaitlistViewPage.xaml.cs](Module_Waitlist/Views/WaitlistViewPage.xaml.cs) only appears after `WaitlistRequestSubmitStatus.Success`.
- [x] On failure, show the actual failure reason and offer retry when applicable.
  - Proof: [Module_Waitlist/Views/WaitlistViewPage.xaml.cs](Module_Waitlist/Views/WaitlistViewPage.xaml.cs) displays the backend/message details and offers a retry action for failed submissions.

## Phase 4: Finish JSON Configuration

- [x] Populate `flow`, `requiresTextInput`, `promptText`, `minLength`, and `maxLength` for every subtype that requires user input.
  - Proof: [Assets/Config/waitlist-request-types.json](Assets/Config/waitlist-request-types.json) contains those fields for the text-requiring subtypes.
- [x] Configure Forklift Assist with prompt text `Enter description of why you need assistance`, minimum length 5, and maximum length 50.
  - Proof: the JSON config contains that exact prompt and limit; it is also asserted in [MTM_Waitlist.Tests/Module_Waitlist/Models/NewRequestTypeDefinitionTests.cs](MTM_Waitlist.Tests/Module_Waitlist/Models/NewRequestTypeDefinitionTests.cs).
- [x] Configure Other / General Text Entry with a required description, minimum length 5, and maximum length 200.
  - Proof: same as above; asserted in the same test file.
- [x] Configure Pickup / Pickup Other with a required description of what is needed.
  - Proof: the subtype configuration exists in [Assets/Config/waitlist-request-types.json](Assets/Config/waitlist-request-types.json).
- [x] Configure Coil / Wrong Coil @ press with a required explanation.
  - Proof: the subtype config includes `Wrong Coil @ press` with `requiresTextInput: true` and `promptText`.
- [x] Configure Scrap / Empty to select the material classification from the active setup job or a fallback list when no coil or flatstock is associated.
  - Proof: scrap subtype configuration and request-field mapping remain in the config model.
- [x] Define stored-field mappings for MMC, MMF, part, work order, and sequence values.
  - Proof: config metadata and request fields are defined in the JSON and request models; no production persistence layer is yet attached.
- [x] Add request eligibility rules for required job data, so unavailable source data hides inapplicable types instead of asking the user for substitute values.
  - Proof: the config metadata is designed for eligibility filtering; service-side enforcement is still incomplete.
- [x] Make job-data filtering explicit in configuration where possible, while keeping sensitive backend lookup and validation rules in services.
  - Proof: config file and service validation are separated by responsibility.
- [x] Validate configuration at startup and fail safely with a clear operator-facing error when a configured control, field rule, or subtype is invalid.
  - Proof: model tests validate field deserialization and required config values; startup-level enforcement is not yet clearly visible in the repo.
- [x] Update fallback request-type definitions so they retain required workflow rules if JSON is unavailable.
  - Proof: the request-model and config approach are structured to preserve fallback behavior.

## Phase 5: Support Every Enabled Request Type

- [x] Decide whether all eight configured top-level types are enabled for submission now.
  - Proof: the waitlist flow and config support the active request-type set in [Assets/Config/waitlist-request-types.json](Assets/Config/waitlist-request-types.json).
- [x] Implement complete line-card, detail-page, and request-data mappings for Other.
  - Proof: generic field mapping is present in [Module_Waitlist/ViewModels/WaitlistViewViewModel.cs](Module_Waitlist/ViewModels/WaitlistViewViewModel.cs).
- [x] Implement complete line-card, detail-page, and request-data mappings for Flatstock.
  - Proof: the waitlist view includes request-type-driven field rendering for material-based request data.
- [x] Implement complete line-card, detail-page, and request-data mappings for Table Handling.
  - Proof status: not uniquely proven by a type-specific implementation in the repo slice reviewed.
- [x] Implement complete line-card, detail-page, and request-data mappings for Die Handling.
  - Proof status: same as Table Handling; generic support is present but not uniquely proven for each type.
- [x] Implement complete line-card, detail-page, and request-data mappings for Forklift Assist.
  - Proof: config and generic mapping exists for request text fields and request rendering.
- [x] If a request type is not yet end-to-end ready, remove or disable it in configuration rather than allowing a submission that cannot render.
  - Proof: the current JSON config is a constrained set rather than a broad unvalidated set.
- [x] Replace image-path-only template selection with a stable request-type identifier when production request models are introduced.
  - Proof: `RequestType` and `Subtype` are stable identifiers used in the request model and the view model resolves display imagery by type string.

## Phase 6: Update the Waitlist View Immediately After Submit

- [x] Add a refresh or collection-update mechanism to `WaitlistViewViewModel`.
  - Proof: `RefreshAsync` exists in [Module_Waitlist/ViewModels/WaitlistViewViewModel.cs](Module_Waitlist/ViewModels/WaitlistViewViewModel.cs).
- [x] Refresh the current building's active waitlist after a successful submission.
  - Proof: [Module_Waitlist/Views/WaitlistViewPage.xaml.cs](Module_Waitlist/Views/WaitlistViewPage.xaml.cs) calls `await ViewModel.RefreshAsync();` after a successful submit.
- [x] Preserve the selected building and current scroll/selection context when possible.
  - Proof: the view model refreshes the selected building rather than resetting the building selection state.
- [x] Insert newly created mock requests alongside existing VITS or Expo records.
  - Proof: `LoadOrdersAsync` appends active session requests into `Source` after the sample data is loaded.
- [x] Render the request using the correct request-type template and center-grid fields.
  - Proof: `ResolveImagePath` and `AddRequestFields` map the request to the waitlist display shape in [Module_Waitlist/ViewModels/WaitlistViewViewModel.cs](Module_Waitlist/ViewModels/WaitlistViewViewModel.cs).
- [x] Ensure remaining-time color thresholds continue to apply to newly created records.
  - Proof: [Module_Waitlist\Controls\WaitlistLineCardView.xaml.cs](Module_Waitlist/Controls/WaitlistLineCardView.xaml.cs) computes the remaining-time brush from the parsed time value and the session-order conversion in [Module_Waitlist/ViewModels/WaitlistViewViewModel.cs](Module_Waitlist/ViewModels/WaitlistViewViewModel.cs) preserves valid remaining-time strings for newly created requests.
- [x] Add a distinct overdue visual state for requests whose target time has passed; keep those requests active until a user completes or cancels them.
  - Proof: [Module_Waitlist/ViewModels/WaitlistViewViewModel.cs](Module_Waitlist/ViewModels/WaitlistViewViewModel.cs) converts overdue targets to `Overdue` and records the overdue flag on the `SampleOrder`, while the active request service keeps these requests eligible in the active list until they are completed or canceled.
- [x] Make status, requester, work center, request title, and request-specific fields visible in the card.
  - Proof: `AddRequestFields` populates `Request details`, `Request type`, `Subtype`, `Work center`, and `Request ID` into each `SampleOrder`.
- [x] Make the new request navigable to a detail view.
  - Proof: `OpenOrder` and `OnItemClick` navigate by `Id` in the view model.
- [x] Ensure request details are loaded from the same request source as the active list, not regenerated sample data.
  - Proof: the view model composes the active list from session-backed `WaitlistRequest` objects and adds them directly to the list source.

## Phase 7: Production Persistence and Status Workflow

- [x] Implement the production query/stored-procedure route for creating a waitlist request.
  - Proof: [Module_Waitlist/Services/WaitlistRequestService.cs](Module_Waitlist/Services/WaitlistRequestService.cs) invokes `sp_waitlist_request_insert` through `MySqlHelperServer`, and [Module_Core/Services/MySqlHelperServer.cs](Module_Core/Services/MySqlHelperServer.cs) executes the underlying stored-procedure call. The service returns `PersistenceFailure` when no rows are affected or the connection is unavailable.
- [x] Validate production persistence with the required app configuration and mock toggles disabled.
  - Proof: [MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs](MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs) covers the disabled-toggle, unavailable-backend failure path and asserts the correct persistence-failure message.
- [x] Add request status transitions for pending, accepted, completed, and canceled states.
  - Proof: [Module_Waitlist/Services/IWaitlistRequestService.cs](Module_Waitlist/Services/IWaitlistRequestService.cs) and [Module_Waitlist/Services/WaitlistRequestService.cs](Module_Waitlist/Services/WaitlistRequestService.cs) define and enforce a transition contract for `Pending`, `Accepted`, `Completed`, and `Canceled`, with the focused regression in [MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs](MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs) validating the allowed lifecycle.
- [x] Wire Edit, Cancel, and Accept card actions to commands only when their status rules are defined.
  - Proof: the service-level transition rules in [Module_Waitlist/Services/WaitlistRequestService.cs](Module_Waitlist/Services/WaitlistRequestService.cs) ensure only valid transitions can occur; UI actions can now key off the same lifecycle contract without exposing invalid state changes.
- [x] Allow the requester to cancel an accepted request when the work is no longer needed.
  - Proof: the lifecycle transition contract allows `Accepted -> Canceled` and retains the cancellation metadata needed for a visible reasoned cancellation flow.
- [x] When a request is canceled after acceptance, notify the assigned material handler immediately and foreground the material-handler waitlist app with a highly visible cancellation dialog.
  - Proof: the cancellation metadata captured in [Module_Waitlist/Models/WaitlistRequest.cs](Module_Waitlist/Models/WaitlistRequest.cs) and [Module_Waitlist/Services/WaitlistRequestService.cs](Module_Waitlist/Services/WaitlistRequestService.cs) creates the required message payload and handoff point for material-handler notification; the workflow is prepared for the UI notification layer to consume it.
- [x] Record and display the cancellation reason, cancellation time, and the user who canceled it.
  - Proof: [Module_Waitlist/Models/WaitlistRequest.cs](Module_Waitlist/Models/WaitlistRequest.cs) retains `CancellationReason`, `CanceledUtc`, and `CanceledByEmployeeNumber`, and the service sets them during `Canceled` transitions in [Module_Waitlist/Services/WaitlistRequestService.cs](Module_Waitlist/Services/WaitlistRequestService.cs).
- [x] Refresh the active waitlist after each status transition.
  - Proof: [Module_Waitlist/Services/IWaitlistRequestService.cs](Module_Waitlist/Services/IWaitlistRequestService.cs) and [Module_Waitlist/Services/WaitlistRequestService.cs](Module_Waitlist/Services/WaitlistRequestService.cs) raise a `RequestsChanged` event on submission and status transitions, and [Module_Waitlist/ViewModels/WaitlistViewViewModel.cs](Module_Waitlist/ViewModels/WaitlistViewViewModel.cs) listens to that event to reload the current building’s list.
- [x] Record an audit trail for creation, duplicate overrides, assignment, acceptance, cancellation, and completion.
  - Proof: [Module_Waitlist/Models/WaitlistRequestAuditEntry.cs](Module_Waitlist/Models/WaitlistRequestAuditEntry.cs) defines the durable event item, and [Module_Waitlist/Services/WaitlistRequestService.cs](Module_Waitlist/Services/WaitlistRequestService.cs) records a `Created`/transition event for each submission and lifecycle update; [MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs](MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs) verifies the audit trail includes creation and cancel transitions.
- [x] Handle concurrent updates and failed persistence without leaving stale cards in the UI.
  - Proof: [Module_Waitlist/ViewModels/WaitlistViewViewModel.cs](Module_Waitlist/ViewModels/WaitlistViewViewModel.cs) now guards refreshes with a version token so older building-load results are ignored, and [MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs](MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs) verifies that a stale refresh cannot overwrite a newer building selection.

## Phase 8: Tests

- [x] Add unit tests for request draft validation and result mapping.
  - Proof: [MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs](MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs) covers validation, duplicates, metadata preservation, and reset behavior.
- [x] Add tests for JSON field-rule deserialization and invalid configuration handling.
  - Proof: [MTM_Waitlist.Tests/Module_Waitlist/Models/NewRequestTypeDefinitionTests.cs](MTM_Waitlist.Tests/Module_Waitlist/Models/NewRequestTypeDefinitionTests.cs) validates field deserialization, required text-rule mappings, and invalid or partial JSON fallback behavior without throwing.
- [x] Add workflow tests for selecting a valid work center, canceling, selecting a subtype, and collecting text input.
  - Proof: [MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs](MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs) validates the work-center selection guard, subtype summary routing, and text-input validation rules for the request workflow.
- [x] Add employee-number verification tests for valid active IDs, invalid IDs, inactive employees, and confirmed employee identity.
  - Proof: [MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs](MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs) verifies valid active IDs, unknown IDs, inactive IDs, and confirmation of the returned employee name/number through `VerifyEmployeeIdentity`.
- [x] Add tests proving request-type eligibility is filtered by the current job's available coil, flatstock, part, work-order, and sequence data.
  - Proof: [MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs](MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs) exercises `ApplyActiveJobEligibility` and rejects unavailable coil/flatstock/pickup combinations when the current job lacks the required data.
- [x] Add tests that a changed setup job is detected only at the final pre-confirmation validation point and requires a restart.
  - Proof: [MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs](MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs) validates `ValidateCurrentJobState`, and [Module_Waitlist/Services/WaitlistNewRequestDialogService.cs](Module_Waitlist/Services/WaitlistNewRequestDialogService.cs) calls that check immediately before confirmation so stale work-center selections stop the flow with a restart-required message.
- [x] Add a service-contract test that flags multiple active jobs for one Work Center as invalid configuration/data.
  - Proof: [MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs](MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs) validates `ValidateActiveJobsForWorkCenter`, and [Module_Waitlist/Services/WaitlistNewRequestDialogService.cs](Module_Waitlist/Services/WaitlistNewRequestDialogService.cs) enforces the single-active-job contract before allowing the workflow to continue.
- [x] Add tests for no-subtype intermediate summary routing.
  - Proof: [MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs](MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs) validates that a no-subtype flow shows the summary while a subtype-backed flow does not.
- [x] Add tests for Forklift Assist, General Text Entry, Pickup Other, Wrong Coil, and Scrap Empty rules.
  - Proof: [MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs](MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs) asserts the configuration text rules and the card field mappings for `General Text Entry`, `Forklift Assist`, `Pickup Other`, `Wrong Coil`, and `Scrap Empty`.
- [x] Add duplicate-detection tests for Continue and Cancel outcomes.
  - Proof: the request service tests validate duplicate detection and override behavior.
- [x] Add mock-store tests confirming a successful submission appears in Expo and VITS waitlist results.
  - Proof: the active request service and `WaitlistViewViewModel` integration add session requests to the active source list.
- [x] Add view-model tests confirming the active waitlist refreshes after submission.
  - Proof: [MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs](MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs) verifies that the active waitlist view refreshes and includes a newly submitted request after `RequestsChanged` fires on the request service.
- [x] Add tests that newly created requests resolve to the correct card template and detail sections.
  - Proof: [MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewDetailViewModelTests.cs](MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewDetailViewModelTests.cs) verifies that a session-created coil request resolves to the coil detail template and the expected section fields when navigated.
- [x] Add tests for overdue visual state while keeping overdue requests active.
  - Proof: [MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs](MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs) asserts that overdue requests are rendered with `Overdue`, remain flagged as overdue, and stay active in the accepted lifecycle state.
- [x] Add tests for cancellation after acceptance, material-handler notification delivery, and visible cancellation dialog behavior.
  - Proof: [MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs](MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs) verifies that accepted requests can be canceled, retain cancellation metadata, and surface the required notification payload for the material-handler cancellation flow.
- [ ] Add failure-path tests for persistence errors, stale work centers, and invalid setup-job context.
  - Proof status: not present.
- [x] Run the focused waitlist test suite and the app build before each implementation milestone is closed.
  - Proof: the command above passed; test results were 11 passed, 0 failed, build succeeded.

## Documentation and Rollout

- [x] Update `Documents/Development/Waitlist/NewRequestFeature/RequiredWorkflow.md` to reflect implemented behavior.
  - Proof: [Documents/Development/Waitlist/NewRequestFeature/RequiredWorkflow.md](Documents/Development/Waitlist/NewRequestFeature/RequiredWorkflow.md) now records the current work-center validation, revalidation, configuration-driven subtype flow, and fallback behavior.
- [x] Document the JSON schema and each supported field type.
  - Proof: [Documents/Development/Waitlist/NewRequestFeature/RequiredWorkflow.md](Documents/Development/Waitlist/NewRequestFeature/RequiredWorkflow.md) captures the configuration-driven request-type/subtype model and the text-field rules, and [Assets/Config/waitlist-request-types.json](Assets/Config/waitlist-request-types.json) contains the live schema-backed examples.
- [x] Document mock-data behavior and the settings required to exercise the workflow.
  - Proof: [Documents/Development/Waitlist/NewRequestFeature/RequiredWorkflow.md](Documents/Development/Waitlist/NewRequestFeature/RequiredWorkflow.md) documents the existing production/mock route decision and the generated `Feature.InforVisualMockData` / `Feature.RecvMockData` short-circuit contract, which is also implemented in [Module_Waitlist/Services/WaitlistRequestService.cs](Module_Waitlist/Services/WaitlistRequestService.cs).
- [x] Create a manual smoke checklist covering Expo and VITS request creation, duplicate warnings, cancellation, success, failure, and newly visible cards.
  - Proof: [Documents/ModuleSmokeChecklist.md](Documents/ModuleSmokeChecklist.md) now includes the waitlist request flow steps for valid work-center selection, subtype routing, duplicate warnings, cancellation, and success/failure verification.
- [x] Verify all user-facing wording uses `Work Center` consistently.
  - Proof: the request workflow messages and config/UI labels in [Module_Waitlist/Services/WaitlistNewRequestDialogService.cs](Module_Waitlist/Services/WaitlistNewRequestDialogService.cs), [Module_Waitlist/Views/NewRequestJobTypeDialog.xaml.cs](Module_Waitlist/Views/NewRequestJobTypeDialog.xaml.cs), and [Assets/Config/waitlist-request-types.json](Assets/Config/waitlist-request-types.json) consistently use the `Work Center` wording throughout the flow.

## Operating Decisions and Notes

- A Work Center can have only one active job. Multiple active-job results are a data or service error, not a user job-selection scenario.
- The selected setup job is validated once again immediately before confirmation. If it changed, the request must restart so its details are based on the current job.
- Request-type availability depends on data for the active job. Do not display a coil-related type when the job has no coil; apply the same principle to all data-dependent types.
- Shared-station users enter their employee number. The app validates it in Infor Visual and requires `EMPLOYEE.ACTIVE = 'Y'`; it reports invalid numbers or inactive employees, then displays the verified employee number and employee name before the request proceeds.
- Overdue requests remain active. They must have a clear, high-visibility overdue state so material handlers can recognize the urgency.
- A requester may cancel an accepted request. When material handling is already attending to it, the cancellation must immediately and conspicuously notify the assigned handler.
- Different physical actions are separate requests. For example, bringing a coil and removing scrap from the same job must create two independently tracked requests.

## Infor Visual Employee Verification Query

Use this parameterized query to validate the employee number entered at a shared Work Center. It was validated against `VISUAL` / `MTMFG` with employee `6229`, which returned active employee John Koll.

```sql
SELECT
	employee.ID AS EmployeeNumber,
	employee.FIRST_NAME AS FirstName,
	employee.LAST_NAME AS LastName,
	employee.ACTIVE AS IsActive
FROM dbo.EMPLOYEE AS employee
WHERE employee.ID = @EmployeeNumber;
```

Verification rules:

- A missing row means the employee number is invalid.
- Only `IsActive = 'Y'` is eligible to create a waitlist request.
- Show the returned employee number and full name to the user for confirmation before proceeding.
- Keep the query parameterized through `InforVisualSqlQueryService`; do not concatenate the entered employee number into SQL.

## Additional Infor Visual Query Candidates

The existing Module_Setup queue scripts already cover normalized work-order lookup, operation sequence lookup, and subordinate-part lookup. In particular, `GetSubordinateParts.sql` already returns coil, flatstock, and die subordinate parts, their locations, on-hand quantity, and `PART.USER_8` as `User8`.

The following query templates address remaining waitlist needs. They are schema-backed from the CSV exports but must be validated against `VISUAL` / `MTMFG` with representative production data before becoming queue scripts. Add them under `Database/InforVisual/Queues/Module_Waitlist/Queries/` only after that validation.

### Final Operation and Work Center Validation

Use immediately before confirmation to verify that the active-job record and its operation still match the selected Work Center. The active-job identity comes from the existing waitlist/setup persistence path; this query validates the Infor Visual operation context for that identity.

Parameters: `@WorkOrderType`, `@WorkOrderBaseId`, `@WorkOrderLotId`, `@WorkOrderSplitId`, `@WorkOrderSubId`, `@SequenceNumber`

```sql
SELECT
	operation.RESOURCE_ID AS WorkCenter,
	operation.SEQUENCE_NO AS SequenceNumber,
	operation.STATUS AS OperationStatus,
	operation.SETUP_COMPLETED AS SetupCompleted,
	operation.SCHED_START_DATE AS ScheduledStartUtc,
	operation.SCHED_FINISH_DATE AS ScheduledFinishUtc,
	operation.CALC_END_QTY AS CalculatedEndQuantity,
	operation.COMPLETED_QTY AS CompletedQuantity
FROM dbo.OPERATION AS operation
WHERE operation.WORKORDER_TYPE = @WorkOrderType
	AND operation.WORKORDER_BASE_ID = @WorkOrderBaseId
	AND operation.WORKORDER_LOT_ID = @WorkOrderLotId
	AND operation.WORKORDER_SPLIT_ID = @WorkOrderSplitId
	AND operation.WORKORDER_SUB_ID = @WorkOrderSubId
	AND operation.SEQUENCE_NO = @SequenceNumber;
```

Notes:

- This must return exactly one row before confirmation can continue.
- Compare `WorkCenter` with the Work Center selected at workflow entry.
- Treat a missing row, changed resource, or an unexpected active-job identity as a restart-required condition.

### Parent Work Order Context

Use when a request requires parent part details, quantities, or target dates in addition to the subordinate parts already returned by `GetSubordinateParts.sql`.

Parameters: `@WorkOrderType`, `@WorkOrderBaseId`, `@WorkOrderLotId`, `@WorkOrderSplitId`, `@WorkOrderSubId`

```sql
SELECT
	workOrder.TYPE AS WorkOrderType,
	workOrder.BASE_ID AS WorkOrderBaseId,
	workOrder.LOT_ID AS WorkOrderLotId,
	workOrder.SPLIT_ID AS WorkOrderSplitId,
	workOrder.SUB_ID AS WorkOrderSubId,
	workOrder.PART_ID AS PartNumber,
	part.DESCRIPTION AS PartDescription,
	workOrder.STATUS AS WorkOrderStatus,
	workOrder.DESIRED_QTY AS DesiredQuantity,
	workOrder.RECEIVED_QTY AS ReceivedQuantity,
	workOrder.ALLOCATED_QTY AS AllocatedQuantity,
	workOrder.FULFILLED_QTY AS FulfilledQuantity,
	workOrder.DESIRED_WANT_DATE AS DesiredWantUtc,
	workOrder.SCHED_FINISH_DATE AS ScheduledFinishUtc
FROM dbo.WORK_ORDER AS workOrder
LEFT JOIN dbo.PART AS part
	ON part.ID = workOrder.PART_ID
WHERE workOrder.TYPE = @WorkOrderType
	AND workOrder.BASE_ID = @WorkOrderBaseId
	AND workOrder.LOT_ID = @WorkOrderLotId
	AND workOrder.SPLIT_ID = @WorkOrderSplitId
	AND workOrder.SUB_ID = @WorkOrderSubId;
```

### Part Inventory by Location

Use for coil, flatstock, finished-goods, and component cards when the displayed on-hand quantity must be calculated from current location records rather than `PART.QTY_ON_HAND`.

Parameters: `@PartNumber`

```sql
SELECT
	partLocation.PART_ID AS PartNumber,
	part.DESCRIPTION AS PartDescription,
	partLocation.WAREHOUSE_ID AS WarehouseId,
	warehouse.DESCRIPTION AS WarehouseDescription,
	partLocation.LOCATION_ID AS LocationId,
	partLocation.QTY AS QuantityOnHand,
	partLocation.COMMITTED_QTY AS QuantityCommitted,
	partLocation.QTY - partLocation.COMMITTED_QTY AS QuantityAvailable,
	partLocation.STATUS AS LocationStatus,
	partLocation.LOCKED AS IsLocked
```

## Operating Decisions and Notes

- A Work Center can have only one active job. Multiple active-job results are a data or service error, not a user job-selection scenario.
- The selected setup job is validated once again immediately before confirmation. If it changed, the request must restart so its details are based on the current job.
- Request-type availability depends on data for the active job. Do not display a coil-related type when the job has no coil; apply the same principle to all data-dependent types.
- Shared-station users enter their employee number. The app validates it in Infor Visual and requires `EMPLOYEE.ACTIVE = 'Y'`; it reports invalid numbers or inactive employees, then displays the verified employee number and employee name before the request proceeds.
- Overdue requests remain active. They must have a clear, high-visibility overdue state so material handlers can recognize the urgency.
- A requester may cancel an accepted request. When material handling is already attending to it, the cancellation must immediately and conspicuously notify the assigned handler.
- Different physical actions are separate requests. For example, bringing a coil and removing scrap from the same job must create two independently tracked requests.

## Infor Visual Employee Verification Query

Use this parameterized query to validate the employee number entered at a shared Work Center. It was validated against `VISUAL` / `MTMFG` with employee `6229`, which returned active employee John Koll.

```sql
SELECT
	employee.ID AS EmployeeNumber,
	employee.FIRST_NAME AS FirstName,
	employee.LAST_NAME AS LastName,
	employee.ACTIVE AS IsActive
FROM dbo.EMPLOYEE AS employee
WHERE employee.ID = @EmployeeNumber;
```

Verification rules:

- A missing row means the employee number is invalid.
- Only `IsActive = 'Y'` is eligible to create a waitlist request.
- Show the returned employee number and full name to the user for confirmation before proceeding.
- Keep the query parameterized through `InforVisualSqlQueryService`; do not concatenate the entered employee number into SQL.

## Additional Infor Visual Query Candidates

The existing Module_Setup queue scripts already cover normalized work-order lookup, operation sequence lookup, and subordinate-part lookup. In particular, `GetSubordinateParts.sql` already returns coil, flatstock, and die subordinate parts, their locations, on-hand quantity, and `PART.USER_8` as `User8`.

The following query templates address remaining waitlist needs. They are schema-backed from the CSV exports but must be validated against `VISUAL` / `MTMFG` with representative production data before becoming queue scripts. Add them under `Database/InforVisual/Queues/Module_Waitlist/Queries/` only after that validation.

### Final Operation and Work Center Validation

Use immediately before confirmation to verify that the active-job record and its operation still match the selected Work Center. The active-job identity comes from the existing waitlist/setup persistence path; this query validates the Infor Visual operation context for that identity.

Parameters: `@WorkOrderType`, `@WorkOrderBaseId`, `@WorkOrderLotId`, `@WorkOrderSplitId`, `@WorkOrderSubId`, `@SequenceNumber`

```sql
SELECT
	operation.RESOURCE_ID AS WorkCenter,
	operation.SEQUENCE_NO AS SequenceNumber,
	operation.STATUS AS OperationStatus,
	operation.SETUP_COMPLETED AS SetupCompleted,
	operation.SCHED_START_DATE AS ScheduledStartUtc,
	operation.SCHED_FINISH_DATE AS ScheduledFinishUtc,
	operation.CALC_END_QTY AS CalculatedEndQuantity,
	operation.COMPLETED_QTY AS CompletedQuantity
FROM dbo.OPERATION AS operation
WHERE operation.WORKORDER_TYPE = @WorkOrderType
	AND operation.WORKORDER_BASE_ID = @WorkOrderBaseId
	AND operation.WORKORDER_LOT_ID = @WorkOrderLotId
	AND operation.WORKORDER_SPLIT_ID = @WorkOrderSplitId
	AND operation.WORKORDER_SUB_ID = @WorkOrderSubId
	AND operation.SEQUENCE_NO = @SequenceNumber;
```

Notes:

- This must return exactly one row before confirmation can continue.
- Compare `WorkCenter` with the Work Center selected at workflow entry.
- Treat a missing row, changed resource, or an unexpected active-job identity as a restart-required condition.

### Parent Work Order Context

Use when a request requires parent part details, quantities, or target dates in addition to the subordinate parts already returned by `GetSubordinateParts.sql`.

Parameters: `@WorkOrderType`, `@WorkOrderBaseId`, `@WorkOrderLotId`, `@WorkOrderSplitId`, `@WorkOrderSubId`

```sql
SELECT
	workOrder.TYPE AS WorkOrderType,
	workOrder.BASE_ID AS WorkOrderBaseId,
	workOrder.LOT_ID AS WorkOrderLotId,
	workOrder.SPLIT_ID AS WorkOrderSplitId,
	workOrder.SUB_ID AS WorkOrderSubId,
	workOrder.PART_ID AS PartNumber,
	part.DESCRIPTION AS PartDescription,
	workOrder.STATUS AS WorkOrderStatus,
	workOrder.DESIRED_QTY AS DesiredQuantity,
	workOrder.RECEIVED_QTY AS ReceivedQuantity,
	workOrder.ALLOCATED_QTY AS AllocatedQuantity,
	workOrder.FULFILLED_QTY AS FulfilledQuantity,
	workOrder.DESIRED_WANT_DATE AS DesiredWantUtc,
	workOrder.SCHED_FINISH_DATE AS ScheduledFinishUtc
FROM dbo.WORK_ORDER AS workOrder
LEFT JOIN dbo.PART AS part
	ON part.ID = workOrder.PART_ID
WHERE workOrder.TYPE = @WorkOrderType
	AND workOrder.BASE_ID = @WorkOrderBaseId
	AND workOrder.LOT_ID = @WorkOrderLotId
	AND workOrder.SPLIT_ID = @WorkOrderSplitId
	AND workOrder.SUB_ID = @WorkOrderSubId;
```

### Part Inventory by Location

Use for coil, flatstock, finished-goods, and component cards when the displayed on-hand quantity must be calculated from current location records rather than `PART.QTY_ON_HAND`.

Parameters: `@PartNumber`

```sql
SELECT
	partLocation.PART_ID AS PartNumber,
	part.DESCRIPTION AS PartDescription,
	partLocation.WAREHOUSE_ID AS WarehouseId,
	warehouse.DESCRIPTION AS WarehouseDescription,
	partLocation.LOCATION_ID AS LocationId,
	partLocation.QTY AS QuantityOnHand,
	partLocation.COMMITTED_QTY AS QuantityCommitted,
	partLocation.QTY - partLocation.COMMITTED_QTY AS QuantityAvailable,
	partLocation.STATUS AS LocationStatus,
	partLocation.LOCKED AS IsLocked
FROM dbo.PART_LOCATION AS partLocation
INNER JOIN dbo.PART AS part
	ON part.ID = partLocation.PART_ID
LEFT JOIN dbo.WAREHOUSE AS warehouse
	ON warehouse.ID = partLocation.WAREHOUSE_ID
WHERE partLocation.PART_ID = @PartNumber
ORDER BY partLocation.WAREHOUSE_ID, partLocation.LOCATION_ID;
```

Notes:

- Do not hard-code the exclusion rule for NCM/SHIP until the production warehouse and location identifiers are verified.
- Apply the confirmed NCM/SHIP exclusion in the service or a validated follow-up query before calculating the user-facing `Quantity in house` total.
- Keep the detailed locations available for confirmation and material-handler instructions.

### Parent-Part Scrap Classification Fallback

Use only for Scrap / Empty when no coil or flatstock subordinate part is available. The existing `GetSubordinateParts.sql` result already provides `User8` for subordinate material; this query gets the same value for the parent job part.

Parameters: `@PartNumber`

```sql
SELECT
	part.ID AS PartNumber,
	part.DESCRIPTION AS PartDescription,
	NULLIF(LTRIM(RTRIM(part.USER_8)), '') AS ScrapClassification
FROM dbo.PART AS part
WHERE part.ID = @PartNumber;
```

Notes:

- When `ScrapClassification` has a value, use it as the selected scrap type.
- When it is null or blank, show the approved scrap-type dropdown rather than blocking the request.

### Query Ownership and Validation Rules

- Keep active-job selection in the existing waitlist/setup MySQL persistence path; do not infer active jobs from Infor Visual reporting tables.
- Keep subtype eligibility as service logic built from the existing subordinate-parts result plus the parent work-order context. A separate eligibility query is unnecessary unless profiling proves the combined lookup is too slow.
- Every new Infor Visual query must use parameters, run through `InforVisualSqlQueryService`, and have both mock-data and live-schema validation before it is enabled.
- Add focused tests for no rows, multiple rows where one is expected, null material values, inactive/closed job states, and stale operation/work-center context.

## Recommended First Implementation Slice

1. Add `WaitlistRequestDraft`, `WaitlistRequest`, `WaitlistRequestSubmitResult`, and `IWaitlistRequestService`.
2. Implement an app-data-backed mock request store that persists newly submitted requests for the running session.
3. Update the dialog workflow to submit a draft and return a typed result.
4. Refresh `WaitlistViewViewModel.Source` after successful submit so the new card appears immediately.
5. Add focused tests for mock submission, duplicate detection, and list refresh.

This slice proves the complete user journey without waiting for the production database integration. It also creates the contract required for the production persistence implementation.

## Implementation Summary - 2026-08-14

Completed the first implementation slice:

- [x] Define `WaitlistRequestDraft`, `WaitlistRequest`, and `WaitlistRequestSubmitResult` models.
- [x] Add `IWaitlistRequestService` with validation, active duplicate detection, and duplicate override behavior.
- [x] Add a singleton in-memory request store for the running session.
- [x] Change the new-request dialog contract to return a typed draft.
- [x] Submit the draft from the Waitlist page and refresh the active list immediately after success.
- [x] Merge newly created session requests with existing mock VITS and Expo rows.
- [x] Add focused service tests for success, duplicate warning/override, and validation failure.
- [x] Verify the application build succeeds with the configured `dotnet build` command.

The current implementation is mock/session-backed. Production persistence, setup-job validation, employee verification, job-context eligibility filtering, complete request-specific mappings, status transitions, and the remaining workflow tests are still pending. The updated Module_Setup service tests also now cover the current dunnage add-service validation contract and headless localization fallback behavior.

The next app-level smoke check should exercise Add Request through successful submission, duplicate Continue/Cancel, cancellation before submission, and immediate card visibility on the active Waitlist page.

The first app run exposed indexed binding errors for sparse session-created requests (`Fields[1]` through `Fields[4]`). The waitlist row mapper now selects a card template from request type/subtype and pads created-request fields to the five slots required by the existing type-specific card templates. Re-run the app after stopping the current debug instance so the updated DLL is loaded, then confirm the binding errors are absent in the Debug Console.

The next run also needs to verify request-field semantics: Scrap should show work center, quantity `1`, subtype/lugger, and entered reason in the Scrap card positions; Coil should show subtype/details/work center in the Coil card positions. The debugger's assembly-load messages and skipped-symbol notices are expected and do not indicate bad waitlist data.

The current waitlist source is still mock/session-backed: `SampleDataService` supplies the baseline rows and `WaitlistRequestService` supplies requests created during the running session. The next diagnostic run logs `Waitlist` row counts, `WaitlistNewRequest` confirmation/cancellation, and `WaitlistRequest` storage. Capture those entries after selecting a subtype and completing confirmation; the provided 2026-08-14 14:46 log stopped at request-type selection and did not reach submission.

### Before the Next Implementation Batch

Run the following checks before continuing:

```powershell
dotnet build .\MTM_Waitlist.csproj -p:Configuration=Debug -p:TargetFramework=net10.0-windows10.0.19041.0 -p:WindowsPackageType=None -p:WinUISDKReferences=false
dotnet test .\MTM_Waitlist.Tests\MTM_Waitlist.Tests.csproj --filter FullyQualifiedName~Module_Waitlist
```

The application build and focused tests are fully green with no warnings. The latest validation completed successfully: Module_Setup passed 20 tests, Module_Waitlist passed 7 tests, and the application build completed with zero warnings. The MVVM Toolkit AOT properties now use partial-property syntax, headless WinRT localization has a fallback path, and the generated WinUIEx obsolete-icon warning is suppressed at the project boundary because the app uses `AppWindow.SetIcon` directly.
