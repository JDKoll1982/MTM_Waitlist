# Module_Setup Feature Specification

## 1. Purpose

Module_Setup will provide the workstation setup experience for entering a work order, resolving part and sequence information, and preparing the setup data required for downstream workstation operations. The flow is derived from the current workflow diagram in [SavedDiagram.drawio](SavedDiagram.drawio). Module_Setup will be implemented as a workflow embedded inside the shell navigation rather than as a separate standalone window.

## 2. Feature Scope

The feature covers:
- Entry of a work order identifier in the expected formats.
- Validation of the work order input format.
- Retrieval of associated part and sequence data from Infor Visual.
- Handling of zero, one, or multiple matching part results.
- Selection of an active sequence.
- Review of a summary for WO, part, and sequence.
- Retrieval and presentation of subordinate part categorizations.
- Guided dunnage selection using a MySQL-backed workflow of dunnage type -> dunnage part -> review/confirm.
- Near pixel-parity UX with MTM_Receiving_Application dunnage screens for type selection, part selection, review, and quick-add dialogs, adapted for setup pair assignment (not label-data save).
- Loading and saving dunnage setup values through the same MySQL-backed pattern used by the receiving application.
- Pair assignment behavior for selected part/sequence:
  - Add one or many dunnage parts.
  - Remove one assigned dunnage part.
  - Remove all assigned parts for the selected type.
  - Clear all assigned parts for the pair.
- Confirmation of preloaded values before persisting changes.
- Finalization of the setup flow and handoff to downstream save/cleanup logic.
- Helper-server routing for read-only and read/write interactions that short-circuits to mock data when module-specific toggles are enabled in app data:
  - `Feature.InforVisualMockData` (default On)
  - `Feature.RecvMockData` (default Off)
- Persistence of all Infor Visual lookup request/response definitions as checked-in SQL scripts in the Infor Visual queue-script folder so the workflow can be replayed without runtime JSON artifacts.
- Explicit separation of storage responsibilities:
  - Infor Visual SQL Server: work-order lookup, parts, sequences, and subordinate-part retrieval.
  - MySQL mtm_receiving_application: dunnage types and dunnage parts queue scripts under Database/MTMReceivingApp/Queues/Module_Setup/Queues.
  - MySQL mtm_waitlist: setup-state persistence, active-job coordination, and local history/audit data.
  - Stored procedure `sp_setup_save_setup` under Database/StoredProcedures/sp_setup_save_setup/create.sql for setup save persistence, with rollback script at Database/StoredProcedures/sp_setup_save_setup/rollback.sql.

## 3. User Journey

### Phase 1: Initial Entry
1. User enters a work order value such as 76951, 076951, or WO-076951.
2. User clicks Search.
3. The app normalizes the input to a canonical WO format.
4. If the format is invalid, the app shows an inline validation error and returns focus to the WO field.
5. If the format is valid, the app queries Infor Visual for matching parts and sequences.
6. If the data source fails, the app surfaces a database error and allows the user to try again.

### Phase 2: Infor Visual Phase
1. The system returns all associated part numbers.
2. If zero matches are found, the app shows an inline error.
3. If one match is found, the app continues directly to the sequence selection flow.
4. If multiple matches are found, the app prompts the user to pick a single part number.
5. The app displays the connected sequence list for the selected WO and part.
6. The user selects the active sequence.
7. The app shows a summary of WO, part, and sequence.
8. The user confirms the selection.
9. The system proceeds to the next phase to retrieve subordinate parts and related setup data.

### Phase 3: MySQL Database Phase
1. The system pulls subordinate parts for the selected part and sequence from Infor Visual.
2. The system categorizes subordinate parts using the defined rules (for example, FGT, MMC, MMF, or Other).
3. The system saves the categorized subordinate parts to the MySQL mtm_waitlist database for operator requests/services.
4. The system queries the MySQL mtm_receiving_application database for dunnage definitions by part-and-sequence context and loads the available dunnage types from the receiving-app queue script folder.
5. The user first selects the dunnage type from a guided type-selection screen.
6. The system then loads the matching dunnage parts for that selected type from MySQL.
7. The user selects the specific dunnage part to use and can either return to the type-selection screen or continue to review.
8. The system displays a review screen with the selected dunnage type, part, and supporting metadata before saving.
9. The system determines whether the current selection originated from existing MySQL-backed preloaded values.
10. If preloaded values are present, the user is prompted to confirm changes.
11. If no preloaded values exist, the selection is saved directly.
12. The setup flow finalizes and the data is handed off to the active queue/history persistence path in mtm_waitlist, with setup save persisted through `Database/StoredProcedures/sp_setup_save_setup/create.sql`.

### Phase 4: Saving and Cleanup
1. The system checks whether an active job already exists.
2. If an active job exists, the app shows a replacement confirmation.
3. If the user cancels, the app keeps the existing job and remains on the current page.
4. If the user confirms replacement, the app saves the active job and audit data.
5. The app displays the updated workstation job details.

## 4. Functional Requirements

### 4.1 Input Handling
- The UI must accept WO values in the formats 76951, 076951, and WO-076951.
- The app must normalize input to a canonical WO format before querying downstream services.
- Invalid formats must show inline validation feedback and return focus to the WO field.

### 4.2 Infor Visual Integration
- The system must query Infor Visual for part and sequence data based on the normalized work order.
- Zero-result handling must show a non-blocking inline error.
- Single-result handling must proceed without prompting the user.
- Multi-result handling must require the user to choose exactly one part.
- All Infor Visual lookup requests and responses must be written to SQL queue files under Database/InforVisual/Queues/Module and read back from those queue files whenever the workflow needs to replay or rehydrate lookup context.

### 4.3 Sequence Selection
- The system must return connected sequences for the chosen WO and part.
- The selection summary must clearly show WO, part, and sequence.

### 4.4 Subordinate Parts and Categorization
- The system must categorize subordinate parts into logical groups such as dies, coils, flatstock, or components.
- Categorized values must be persisted for operator requests and services.
- The review screen must display the selected subordinate part context clearly, including the primary coil and the relevant die selection(s) and the list of component parts that were resolved for the current work order and sequence.
- The review screen must support zero, one, or many dies for a given part/sequence context. When no die exists for the selected work order/part/sequence combination, the UI must show a neutral state such as “No die assigned for this job.”
- The review screen must surface on-hand quantities and any out-of-stock or low-stock state that should be visible to the operator before confirmation.

### 4.5 Dunnage Setup
- The UI must support a configurable set of dunnage types from settings, but the user experience should follow a guided workflow rather than a single flat form.
- The workflow must begin with dunnage type selection, then move to dunnage part selection, and finally reach review/confirm.
- The user must be able to return to the dunnage type-selection screen before advancing to review.
- The system must resolve part numbers, image paths, and supporting metadata for each selected dunnage part.
- If no image exists, the app must fall back to a default “No Image” state.
- The system must save and retrieve dunnage data through MySQL-backed entities and queue-script files in `Database/MTMReceivingApp/Queues/Module_Setup/Queues` that mirror the receiving application model: dunnage types, dunnage parts, active label data, and history records.
- The review/confirm screen must include a selected dunnage summary section that lists the chosen dunnage entries in a compact format.
- The review/confirm screen must be tabbed and include:
  - Job Context
  - Subordinate Parts
  - Dunnage Pair Assignments
- Preloaded values must require explicit confirmation before overwrite.
- Quick Add operations are role-gated and can write new dunnage definitions directly to mtm_receiving_application:
  - Add New Type
  - Add New Part
  - Allowed roles: Admin, Developer, Plant Manager, Setup Lead, Production Lead.

### 4.6 Save/Cleanup
- The system must guard replacement of an existing active job with a confirmation step.
- The workflow must allow cancellation without losing the existing active job.

## 5. Non-Functional Requirements

- The experience should follow existing MVVM patterns in the repository.
- UI strings should be localized through the resource system.
- Navigation should remain integrated with the existing shell and navigation services.
- The feature should gracefully handle service failures and show actionable error states.
- The feature should be privacy-first and avoid hardcoded secrets.
- The feature should honor app-data mock-data toggles and route through helper servers before backend execution when mock data is enabled:
  - Infor Visual flows use `Feature.InforVisualMockData` (default On).
  - Receiving/MySQL flows use `Feature.RecvMockData` (default Off).

## 6. Suggested Implementation Shape

The feature should be implemented as a dedicated module-aligned workflow with the following structure:
- ViewModels for the WO entry, selection, dunnage workflow, and confirmation flows.
- Views for the multi-step setup pages, including type selection, part selection, and review/confirm.
- Services for:
  - WO normalization and validation
  - Infor Visual lookup
  - MySQL dunnage retrieval and persistence
  - subordinate part categorization
  - active job save/cleanup coordination
- Models for WO input, part selection, sequence selection, dunnage types, dunnage parts, and setup state.
- Workflow state objects similar to the receiving application’s workflow-service pattern so the user can navigate between type selection, part selection, and review without losing context.

This spec should follow the receiving application’s concrete workflow conventions rather than a generic one-page dunnage form. In practice, that means a workflow-oriented UI around dunnage type selection, dunnage part selection, and review/save, backed by MySQL entities in mtm_receiving_application such as dunnage types, dunnage parts, the active label-data queue, and history records.

## 7. Guided Workflow UI Mockups

The dunnage portion of the guided workflow should follow a two-step selection pattern that mirrors the receiving application:
1. Select the dunnage type.
2. Select the dunnage part for that type.
3. Either return to the dunnage type-selection page or continue to the review/confirm page.

The feature should be presented as a step-based guided workflow with a persistent header, progress indicator, and clear action buttons. Each step should keep the user context visible so the progress feels linear and safe.

### 7.1 Step 1 - Work Order Entry

```text
+--------------------------------------------------------------+
| Module Setup                    [Step 1/5]  20% complete     |
|--------------------------------------------------------------|
| Enter Work Order                                             |
|                                                              |
| Work Order *                                                 |
| [76951________________________]  [Search]                    |
|                                                              |
|                                                              |
|                                                              |
| [Cancel]                           [Next]                    |
+--------------------------------------------------------------+
```

Elements:
- Single-line input field for WO.
- Search action button.
- Inline validation message area under the field.
- Progress indicator at the top.
- Cancel and Next actions, with Next disabled until a valid WO is accepted.

### 7.2 Step 2 - Part Selection

```text
+--------------------------------------------------------------+
| Module Setup                    [Step 2/5]  40% complete     |
|--------------------------------------------------------------|
| Select Part Number                                           |
|                                                              |
| Work Order: WO-076951                                        |
|                                                              |
| Found 3 matching parts:                                      |
| [ ] 12345678   |   Part A                                    |
| [x] 12345679   |   Part B   <- selected                      |
| [ ] 12345680   |   Part C                                    |
|                                                              |
| [Back]                           [Continue]                  |
+--------------------------------------------------------------+
```

Elements:
- Radio-button or selection list for part numbers.
- Summary of the current WO.
- Explicit selection state for single-part resolution.
- Back and Continue buttons.

### 7.3 Step 3 - Sequence Selection

```text
+--------------------------------------------------------------+
| Module Setup                    [Step 3/5]  60% complete     |
|--------------------------------------------------------------|
| Select Sequence                                              |
|                                                              |
| Work Order: WO-076951                                        |
| Part: 12345679                                               |
|                                                              |
| Available Sequences/Operations:                              |
| [ ] 10       [x] 20      [ ] 30      [ ] 40        [ ] 50    |
|                                                              |
| Summary: WO | Part | Sequence                                |
|                                                              |
| [Back]                           [Continue]                  |
+--------------------------------------------------------------+
```

Elements:
- Sequence list with one selected item.
- Persistent summary panel for WO, part, and sequence.
- Continue action that advances only after selection.

### 7.4 Step 4 - Dunnage Type and Part Selection

```text
+----------------------------------------------------------------------------------+
| Module Setup                                        [Step 4/5]  80% complete     |
|----------------------------------------------------------------------------------|
| Configure Dunnage                                                                |
|                                                                                  |
| Part: 12345679   Sequence: 20                                                    |
|                                                                                  |
| Page 1: Select Dunnage Type                                                      |
| [_Search/FilterTextBox________________________________________________________]  |
| [Icon Card] DunnageA          [Icon Card] DunnageC          [Icon Card] DunnageE |
| [Icon Card] DunnageB          [Icon Card] DunnageD                               |
|                                                                                  |
| Selected Type: Coils                                                             |
|                                                                                  |
| Page 2: Select Dunnage C                                                         |
| [_Search/FilterTextBox________________________________________________________]  |
| [Image Card] DunnageC A     [Image Card] DunnageC B     [Image Card] DunnageC C  |
| [Image Card] DunnageC D     [Image Card] DunnageC E                              |
|                                                                                  |
| Selected Part: DunnageC E                                                        |
|                                                                                  |
| [Back]                                                                [Review]   |
+----------------------------------------------------------------------------------+
```

Elements:
- A two-page guided workflow that mirrors the receiving app’s dunnage module:
  - Page 1 is dunnage type selection and uses large icon-style cards.
  - Page 2 is dunnage part selection and uses image-style cards for the available parts.
- The selected type should be visually highlighted on Page 1 before the user moves to Page 2.
- Page 2 should show the available parts for the selected type, with a card layout and image preview when available.
- The selected part should update the summary area and remain visible while the user navigates.
- Navigation should support:
  - Back to Types to return to Page 1.
  - Review to continue to the confirmation page.
- If no image exists, the card should fall back to a neutral placeholder state similar to the receiving app.

### 7.5 Step 5 - Review and Confirm

```text
+--------------------------------------------------------------+
| Module Setup                    [Step 5/5] 100% complete     |
|--------------------------------------------------------------|
| Review and Confirm                                           |
|                                                              |
| Work Order: WO-076951                                        |
| Part: 12345679                                               |
| Sequence: 20                                                 |
|                                                              |
| Coil: MMC0001000   On Hand: 12,568                           |
| Dies:                                                        |
| - FGT-0653   Location: V-A1-01                                |
| - FGT-001    No die assigned for this job                    |
|                                                              |
| Component Parts:                                             |
| 23-23451-006       On Hand: 125,000                          |
| 23-23451-007       On Hand: 15,000                           |
| 23-23451-006       On Hand: 0               NONE ON HAND!    |
|                                                              |
| Selected Dunnage Summary:                                    |
| - DunnageC A            DunnageA B            DunnageE D     |
|                                                              |
| [Edit]                           [Confirm]                   |
+--------------------------------------------------------------+
```

Elements:
- Final review screen that summarizes the work order, part, sequence, subordinate part context, and the selected dunnage choice.
- A compact summary layout for the coil, one-or-many die entries, and component part context, including on-hand quantities and clear out-of-stock messaging where applicable.
- A clear selected dunnage summary section that shows the chosen dunnage selections in a compact list.
- Clear action buttons for Edit and Confirm.
- Confirmation state for saving the setup data and moving to cleanup.

### 7.6 Error and Confirmation States

```text
+--------------------------------------------------------------+
| Module Setup                                                 |
|--------------------------------------------------------------|
| Validation Error                                             |
|                                                              |
| The work order format is invalid. Please try again.          |
|                                                              |
| [Try Again]                                                  |
+--------------------------------------------------------------+
```

```text
+--------------------------------------------------------------+
| Replace Existing Active Job?                                 |
|--------------------------------------------------------------|
| {WorkCenter} already has an active job.                      |
| Do you wish to Replace it?                                   |
|                                                              |
| [Cancel]                      [Replace]                      |
+--------------------------------------------------------------+
```

Elements:
- Inline validation error state.
- Confirmation dialog for replacing an existing active job.
- Clear, focused actions to support recovery and safety.

## 8. Acceptance Criteria

- A user can enter a WO and complete the setup flow without error for a valid input.
- Invalid format input shows inline validation feedback.
- Single and multi-part results are both handled correctly.
- A user can review and confirm or cancel a replacement of an existing active job.
- The setup experience can read from and write to the configured data sources without hardcoded secrets.

## 9. Notes

This spec is derived from the workflow in [SavedDiagram.drawio](SavedDiagram.drawio) and should be used as the implementation baseline for Module_Setup.
