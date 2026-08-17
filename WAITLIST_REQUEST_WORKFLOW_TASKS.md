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

## Phase 1: Establish Request Contract

- [x] Define a `WaitlistRequestDraft` model for data collected before submission.
- [x] Define a persisted `WaitlistRequest` model with a stable identifier, status, requester, work center, timestamp, request type, subtype, and request-specific fields.
- [ ] Include the active setup-job identity, workstation name, and request timestamp in the request contract.
- [ ] Include the verified requester employee number and employee name in the request contract.
- [ ] Include the request's target time, overdue state, assigned material handler, and cancellation reason when applicable.
- [x] Define a `WaitlistRequestSubmitResult` that distinguishes success, duplicate-warning-required, validation failure, and persistence failure.
- [ ] Decide and document the persistence schema or stored-procedure contract for production requests.
- [ ] Keep request metadata structured rather than encoding fields into free-form strings.

## Phase 2: Add a Request Service

- [x] Add an `IWaitlistRequestService` abstraction under the waitlist module.
- [x] Register the request service through the existing dependency-injection setup.
- [x] Implement submission validation in the request service, not in the view.
- [ ] Revalidate the selected Work Center's setup job immediately before the confirmation step; stop the workflow and require restart when the job changed.
- [ ] Treat each Work Center as having one active job at a time; surface a clear service/configuration error if multiple active jobs are returned.
- [x] Implement duplicate matching using active request status, work center, request type, subtype, and relevant request-specific identifiers.
- [ ] Route production requests through the existing helper-server/backend pattern.
- [ ] When mock data is enabled, short-circuit through an app-data-backed mock request store.
- [x] Add an in-memory/mock store that supports add, read, and reset behavior for the active session.
- [x] Ensure mock-created requests merge with the existing VITS and Expo sample rows.

## Phase 3: Complete the New Request Dialog Workflow

- [x] Change `IWaitlistNewRequestDialogService` to return a complete request draft or explicit workflow result instead of only `string?`.
- [ ] Preserve the selected work center through every dialog step.
- [ ] Require a valid active setup job before the user can continue.
- [ ] Request and verify the requester's employee number after Work Center selection and before request-type selection.
- [ ] Query Infor Visual for the employee number and active status; only continue when `EMPLOYEE.ACTIVE = 'Y'`.
- [ ] Show an invalid-ID error when no employee is found, show an inactive-employee error when the employee is not active, and otherwise confirm the returned employee name and number before continuing.
- [ ] Use the active job's available material context to determine which request types and subtypes the user can select.
- [ ] Hide coil-related choices when the active job has no coil data; apply equivalent filtering for flatstock, part, work-order, and other required job context.
- [ ] For request types with subtypes, show subtype selection.
- [ ] For request types with no subtypes, show the intermediate summary page before confirmation.
- [ ] Read input rules from `waitlist-request-types.json` and enforce required, minimum-length, and maximum-length validation.
- [ ] Show validation errors in a top summary in addition to field-level feedback.
- [ ] Provide Back/Cancel behavior that returns to the waitlist without saving a draft.
- [ ] If the work center must change, restart at work-center selection.
- [ ] Revalidate the active setup job immediately before confirmation, not during every dialog step; when it changed, explain that the request must restart against the current job.
- [ ] Show duplicate warning only after a duplicate is actually detected, with Continue and Cancel actions.
- [ ] Show confirmation details, current related requests, and estimated wait time without introducing a priority indicator.
- [x] Submit the draft through `IWaitlistRequestService`.
- [ ] On success, show Request Completed with Return to Waitlist and Add Another Request actions.
- [ ] Enable Add Another Request only after a successful submission.
- [ ] On failure, show the actual failure reason and offer retry when applicable.

## Phase 4: Finish JSON Configuration

- [ ] Populate `flow`, `requiresTextInput`, `promptText`, `minLength`, and `maxLength` for every subtype that requires user input.
- [ ] Configure Forklift Assist with prompt text `Enter description of why you need assistance`, minimum length 5, and maximum length 50.
- [ ] Configure Other / General Text Entry with a required description, minimum length 5, and maximum length 200.
- [ ] Configure Pickup / Pickup Other with a required description of what is needed.
- [ ] Configure Coil / Wrong Coil @ press with a required explanation.
- [ ] Configure Scrap / Empty to select the material classification from the active setup job or a fallback list when no coil or flatstock is associated.
- [ ] Define stored-field mappings for MMC, MMF, part, work order, and sequence values.
- [ ] Add request eligibility rules for required job data, so unavailable source data hides inapplicable types instead of asking the user for substitute values.
- [ ] Make job-data filtering explicit in configuration where possible, while keeping sensitive backend lookup and validation rules in services.
- [ ] Validate configuration at startup and fail safely with a clear operator-facing error when a configured control, field rule, or subtype is invalid.
- [ ] Update fallback request-type definitions so they retain required workflow rules if JSON is unavailable.

## Phase 5: Support Every Enabled Request Type

- [ ] Decide whether all eight configured top-level types are enabled for submission now.
- [ ] Implement complete line-card, detail-page, and request-data mappings for Other.
- [ ] Implement complete line-card, detail-page, and request-data mappings for Flatstock.
- [ ] Implement complete line-card, detail-page, and request-data mappings for Table Handling.
- [ ] Implement complete line-card, detail-page, and request-data mappings for Die Handling.
- [ ] Implement complete line-card, detail-page, and request-data mappings for Forklift Assist.
- [ ] If a request type is not yet end-to-end ready, remove or disable it in configuration rather than allowing a submission that cannot render.
- [ ] Replace image-path-only template selection with a stable request-type identifier when production request models are introduced.

## Phase 6: Update the Waitlist View Immediately After Submit

- [x] Add a refresh or collection-update mechanism to `WaitlistViewViewModel`.
- [x] Refresh the current building's active waitlist after a successful submission.
- [ ] Preserve the selected building and current scroll/selection context when possible.
- [x] Insert newly created mock requests alongside existing VITS or Expo records.
- [ ] Render the request using the correct request-type template and center-grid fields.
- [ ] Ensure remaining-time color thresholds continue to apply to newly created records.
- [ ] Add a distinct overdue visual state for requests whose target time has passed; keep those requests active until a user completes or cancels them.
- [ ] Make status, requester, work center, request title, and request-specific fields visible in the card.
- [ ] Make the new request navigable to a detail view.
- [ ] Ensure request details are loaded from the same request source as the active list, not regenerated sample data.

## Phase 7: Production Persistence and Status Workflow

- [ ] Implement the production query/stored-procedure route for creating a waitlist request.
- [ ] Validate production persistence with the required app configuration and mock toggles disabled.
- [ ] Add request status transitions for pending, accepted, completed, and canceled states.
- [ ] Wire Edit, Cancel, and Accept card actions to commands only when their status rules are defined.
- [ ] Allow the requester to cancel an accepted request when the work is no longer needed.
- [ ] When a request is canceled after acceptance, notify the assigned material handler immediately and foreground the material-handler waitlist app with a highly visible cancellation dialog.
- [ ] Record and display the cancellation reason, cancellation time, and the user who canceled it.
- [ ] Refresh the active waitlist after each status transition.
- [ ] Record an audit trail for creation, duplicate overrides, assignment, acceptance, cancellation, and completion.
- [ ] Handle concurrent updates and failed persistence without leaving stale cards in the UI.

## Phase 8: Tests

- [x] Add unit tests for request draft validation and result mapping.
- [ ] Add tests for JSON field-rule deserialization and invalid configuration handling.
- [ ] Add workflow tests for selecting a valid work center, canceling, selecting a subtype, and collecting text input.
- [ ] Add employee-number verification tests for valid active IDs, invalid IDs, inactive employees, and confirmed employee identity.
- [ ] Add tests proving request-type eligibility is filtered by the current job's available coil, flatstock, part, work-order, and sequence data.
- [ ] Add tests that a changed setup job is detected only at the final pre-confirmation validation point and requires a restart.
- [ ] Add a service-contract test that flags multiple active jobs for one Work Center as invalid configuration/data.
- [ ] Add tests for no-subtype intermediate summary routing.
- [ ] Add tests for Forklift Assist, General Text Entry, Pickup Other, Wrong Coil, and Scrap Empty rules.
- [x] Add duplicate-detection tests for Continue and Cancel outcomes.
- [x] Add mock-store tests confirming a successful submission appears in Expo and VITS waitlist results.
- [ ] Add view-model tests confirming the active waitlist refreshes after submission.
- [ ] Add tests that newly created requests resolve to the correct card template and detail sections.
- [ ] Add tests for overdue visual state while keeping overdue requests active.
- [ ] Add tests for cancellation after acceptance, material-handler notification delivery, and visible cancellation dialog behavior.
- [ ] Add failure-path tests for persistence errors, stale work centers, and invalid setup-job context.
- [ ] Run the focused waitlist test suite and the app build before each implementation milestone is closed.

## Documentation and Rollout

- [ ] Update `Documents/Development/Waitlist/NewRequestFeature/RequiredWorkflow.md` to reflect implemented behavior.
- [ ] Document the JSON schema and each supported field type.
- [ ] Document mock-data behavior and the settings required to exercise the workflow.
- [ ] Create a manual smoke checklist covering Expo and VITS request creation, duplicate warnings, cancellation, success, failure, and newly visible cards.
- [ ] Verify all user-facing wording uses `Work Center` consistently.

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
