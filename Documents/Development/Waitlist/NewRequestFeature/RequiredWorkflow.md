New Request Workflow (End User)

Status: Updated to match current implementation and latest 100k-row data pass (2026-08-07).

Purpose

This workflow allows a floor user to start a new waitlist request from the Waitlist page, while enforcing a setup-job check so requests only start for presses/work centers that currently have an active setup job.

Current End-User Flow (Implemented Today)

1. User clicks the green + button on the Waitlist page.
2. A Select Work Center modal opens.
3. The modal shows:
   - Workstation identity at the top (current workstation name).
   - Hot Workcenters section (quick access list).
   - Other Workcenters section (all other available work centers).
4. User clicks a press/work center card.
5. The app validates whether that selected press/work center has an active setup job.
6. If no active setup job exists, the app blocks continuation and shows:
   No active setup job is available for this work center. Please restart the request after selecting a valid press.
7. If an active setup job exists, the selection is accepted and the work-center modal closes.
8. The guided new-request flow continues with request-type selection, optional subtype selection, required text validation, summary handling, and final confirmation.
9. If the work center changes or becomes stale at the final validation step, the workflow stops and requires the user to restart from the work center selection.

Implementation status:
- Work-center validation, employee verification, job-state revalidation, subtype branching, text-field validation, duplicate warning, and waitlist refresh are implemented and covered by the waitlist test suite.
- The configuration-driven request types and text rules continue to be loaded from `Assets/Config/waitlist-request-types.json` with safe fallback defaults when malformed values are encountered.

Production Persistence Contract (Current Decision)

For live production waitlist requests, the implementation should use the existing database pattern already established in the repo:

- Use `MySqlHelperServer` and the existing stored-procedure execution pattern (`ExecuteStoredProcedureQueryAsync`, `ExecuteStoredProcedureNonQueryAsync`, and `ExecuteSqlQueryAsync`) instead of inventing a separate custom persistence layer.
- Reuse current operational tables and queue-backed database structures unless a new table is clearly required by the business workflow and approved by the DB contract review.
- Keep waitlist insert/update logic aligned with `Database/Database-Ruleset.md` and the file-per-artifact layout under `Database/StoredProcedures/`.
- Preserve the existing mock-data short-circuit pattern using `Feature.InforVisualMockData` and `Feature.RecvMockData` so production behavior is still testable without changing the app's runtime contract.
- Any production SQL contract should be written in the same stored-procedure style as the rest of the repo, with parameterized queries and explicit review of table reuse before creating a new table.

This is the default contract for any future waitlist persistence work and is intended to avoid schema drift and duplicated persistence patterns.

Guided New Request Experience (Next Phase)

This is the intended user-facing flow after work-center selection succeeds:

1. Step 1: Choose Job Type.
2. Step 2: If the selected Job Type has Subtypes, show Subtype selection.
3. Step 3: If the selected Subtype requires additional inputs, show required input fields.
4. Step 4: Show a confirmation page with a concise summary of the request.
5. Step 5: Submit and create the waitlist request.

Type and Subtype Catalog (For Guided Modal)

- Pickup
  - Pickup Other - Textbox (What)
  - Pickup NCM - Next Page Show associated Parts except dunnage
  - Pickup WIP - Uses stored part number and sequence of job assocaited with workcenter
  - Pickup FG - Uses stored part number and Work Order
  - Pickup Coil - Uses stored MMC Number
  - Pickup Flatstock - Uses stored MMF Number
- Other, General Text Entry bottom of list
- Coil
  - Bring - Uses stored MMC Number -> Conf
  - Pickup - Uses stored MMC Number -> Conf
  - Wrong Coil @ press - Textbox (Explain how/why it is wrong)
  - Need Riser Table -> Conf
  - Need Coil Turned around -> Conf
- Scrap
  - Empty - Requires 
  - Pickup Hopper, do not return
  - Bring Hopper - Small, Medium, Large
- Flatstock
  - Bring
  - Pickup
  - Wrong Flatstock @ Workcenter
- Table Handling
  - Table Place Parts
  - Table Remove Parts
- Die Handling
  - Bring Die
  - Pull Die and Put Away
  - Pull Die and Take to Die Shop
  - Pull Die and Leave @ press
- Forklift Assist (requires a description text field)

Clarification Decisions Captured (End User Facing)

Status: Captured from latest review notes (2026-08-07).

1. Use Work Center as the standard user-facing term everywhere.
2. After selecting a valid work center, the guided New Request modal should open immediately.
3. Users can cancel from the Job Type screen and return to the waitlist without saving a draft.
4. Users cannot switch work centers inside the guided flow. To change work center, they must cancel and restart.
5. Forklift Assist description prompt text should be:
  Enter description of why you need assistance
6. Forklift Assist description length rules:
  - Minimum: 5 characters
  - Maximum: 50 characters
7. Confirmation page should show:
  - Details about what they requested
  - Current requests
  - Estimated wait time
8. Do not include urgency indicator or default priority on confirmation.
9. On success, show Request Completed with two actions:
  - Return to Waitlist
  - Add Another Request
10. On failure, show the failure reason.
11. Add Another Request is enabled only when the prior request completed without errors.
12. Validation errors should be shown as a top summary.
13. Duplicate requests should be allowed with warning (not hard blocked), with action buttons:
  - Continue
  - Cancel
14. No role-based restrictions are currently required for Job Types or Subtypes.
15. For Job Types with no Subtypes, route directly to confirmation.
16. Request types, subtypes, and required UI controls should be configuration-driven from JSON so maintenance is fast and does not require reworking the feature implementation pattern.

Additional Clarification Questions Needed

1. Suggested JSON-driven schema and field rules (proposal)

Guiding rule
- Default behavior: subtype requires no extra UI controls and routes directly to confirmation.
- Exception behavior: only explicitly configured subtypes render additional controls.

Base request fields (always required)
- requestType
- subtype (when selected)
- workCenter
- workstationName
- activeSetupJobId (or activeSetupJobPublicId)
- requestedByUser
- requestTimestampUtc

Proposed subtype configuration model

{
  "requestType": "Coil",
  "subtype": "Bring",
  "flow": "direct-to-confirmation",
  "requiredStoredFields": ["mmcNumber"],
  "optionalStoredFields": [],
  "uiControls": []
}

{
  "requestType": "Coil",
  "subtype": "Wrong Coil @ press",
  "flow": "collect-input-then-confirm",
  "requiredStoredFields": ["mmcNumber"],
  "optionalStoredFields": [],
  "uiControls": [
    {
      "id": "wrongCoilReason",
      "type": "multilineText",
      "label": "Explain how or why the coil is wrong",
      "required": true,
      "minLength": 5,
      "maxLength": 200
    }
  ]
}

Suggested required-vs-optional rules by subtype
- Default for all subtypes: no extra UI fields, no optional fields, direct to confirmation.
- Pickup -> Pickup Other: require whatIsNeeded text.
- Pickup -> Pickup NCM: no manual text; require selected associated part (excluding dunnage) from loaded list.
- Coil -> Wrong Coil @ press: require reason text.
- Forklift Assist: require assistance description text (min 5, max 50).
- Other -> General Text Entry: require free-text description (length to be finalized).
- Scrap -> Empty: required field to be finalized (see open question below).

2. For Other -> General Text Entry, what are the minimum and maximum text lengths? Minimum 5 Maximum 200
3. In Scrap -> Empty, what exact required field(s) should be captured? Get the scrap type for the connected coil or flatstock from the Infor Visual Database (PART Table, USER_8 column) if the job has no coil or flatstock then it should use dropdown menu 
4. For notes that reference stored values (for example MMC Number and MMF Number), what exact end-user labels should be shown on the confirmation page?
5. On submit failure, should Return to Waitlist always be available, or should there also be a Retry Submit action?