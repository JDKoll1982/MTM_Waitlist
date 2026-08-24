# Checklist

---
**GATE: Phase 1 establishes the data layer, configuration, and service contracts. All Subphase 1.1 and 1.2 tasks must complete before proceeding to Phase 2 (UI/Workflow). Dialog implementations depend on these foundations.**

---

## Phase 1: MVP & Core Foundation

### Subphase 1.1: Database Schema & Data Models
- [x] **Database Table: Create the `config_images_locations` table with the required columns and constraints.** (Ref: Section 4.3) | **Persona: Database Engineer**
- [x] **Database Table: Add the unique index for `scope` and `scope_item_id` to prevent duplicate image overrides.** (Ref: Section 4.3) | **Persona: Database Engineer**
- [x] **Database Table: Add audit columns for create/update metadata and the last editor reference.** (Ref: Section 4.3) | **Persona: Database Engineer**
- [x] **Database Table: Add the required schema rollback or validation artifact for the new image-config table.** (Ref: Section 4.3) | **Persona: Database Engineer**
- [x] **Database Migration: Update `Database/Bootstrap/update_table_descriptions.sql` to document the new image location schema.** (Ref: Section 4.3) | **Persona: Database Engineer**
- [x] **JSON Schema: Add a GUID `id` field to each request type entry in `waitlist-request-types.json`.** (Ref: Section 4.2) | **Persona: Full Stack Engineer**
- [x] **JSON Schema: Add a GUID `id` field to each subtype entry in `waitlist-request-types.json`.** (Ref: Section 4.2) | **Persona: Full Stack Engineer**
- [x] **JSON Schema: Add an optional `imagePath` property to each request type object.** (Ref: Section 4.2) | **Persona: Full Stack Engineer**
- [x] **JSON Schema: Add an optional `imagePath` property to each subtype object.** (Ref: Section 4.2) | **Persona: Full Stack Engineer**
- [x] **Data Model: Define the effective-path resolution order for request types, subtypes, and default assets.** (Ref: Section 4.1) | **Persona: Tech Lead**
- [x] **Data Model: Model the resolved path fallback behavior when the stored file no longer exists.** (Ref: Section 4.1) | **Persona: Tech Lead**
- [x] **Data Model: Define the default fallback image mapping for request types and subtypes.** (Ref: Section 2) | **Persona: Full Stack Engineer**
- [x] **Data Model: Define the default fallback image mapping for work centers.** (Ref: Section 2) | **Persona: Full Stack Engineer**
- [x] **Data Model: Capture the request type inventory and map each item to its stable `id`.** (Ref: Section 3.4) | **Persona: Full Stack Engineer**
- [x] **Data Model: Preserve request type display labels separately from storage IDs so renames do not orphan overrides.** (Ref: Section 4.2) | **Persona: Backend Engineer**
- [x] **Data Model: Capture the subtype inventory and map each subtype to its stable `id` and parent request type.** (Ref: Section 3.6) | **Persona: Full Stack Engineer**
- [x] **Data Model: Preserve subtype display labels separately from storage IDs because names are not globally unique across parent groups.** (Ref: Section 3.6) | **Persona: Backend Engineer**
- [x] **Data Model: Capture the work-center catalog identity and map each row to `setup_workstations_catalog.id`.** (Ref: Section 3.5) | **Persona: Full Stack Engineer**

### Subphase 1.2: Core API Endpoints & Auth
- [x] **Shared Storage Config: Add the image storage options block to `appsettings.json` with the default UNC path `X:\\Software Development\\Live Applications\\MTM_Waitlist\\Images`.** (Ref: Section 4.4) | **Persona: Backend Engineer**
- [x] **Shared Storage Config: Add the database-backed override pattern to `config_settings_values` so admins can change the share path in-app.** (Ref: Section 4.4) | **Persona: Backend Engineer**
- [x] **Configuration: Document appsettings default and database override resolution order for the shared image folder path.** (Ref: Section 4.4) | **Persona: Tech Lead**
- [x] **Service Layer: Register a new DI service in `Module_Settings` to manage image location data and resolution.** (Ref: Section 5) | **Persona: Backend Engineer** — `AddImageLocationServices` is now actually called from `AddSettingsModuleServices` (2026-08-18)
- [x] **Service Layer: Load request type and subtype definitions from `waitlist-request-types.json` through the shared service.** (Ref: Section 5) | **Persona: Backend Engineer**
- [x] **Service Layer: Load active work centers through `IWorkCenterCatalogService` for the work-center card.** (Ref: Section 3.5) | **Persona: Backend Engineer**
- [x] **Service Layer: Read image overrides from `config_images_locations` through the MySQL helper server.** (Ref: Section 5) | **Persona: Backend Engineer**
- [x] **Service Layer: Write image overrides to `config_images_locations` through the MySQL helper server.** (Ref: Section 5) | **Persona: Backend Engineer**
- [x] **Service Layer: Copy selected files into the shared network folder and validate type, size, and aspect.** (Ref: Section 5) | **Persona: Backend Engineer**
- [x] **Service Layer: Resolve the effective image path from override, JSON config, parent request type, and default asset.** (Ref: Section 4.1) | **Persona: Backend Engineer**
- [x] **Service Layer: Raise change notifications after image updates so open views refresh immediately.** (Ref: Section 3.4) | **Persona: Backend Engineer**
- [x] **Service Layer: Handle missing file conditions by falling back to the scope default asset and surfacing a warning.** (Ref: Section 4.1) | **Persona: Backend Engineer**
- [x] **Auth Logic: Restrict the expander visibility to Admin and Developer roles.** (Ref: Section 6) | **Persona: Backend Engineer**
- [x] **Auth Logic: Ensure non-authorized users never render the image-location settings expander.** (Ref: Section 6) | **Persona: Backend Engineer**

Next task: **Settings Page: Bind the expander visibility to the role-based view-model property.** (Ref: Section 3.1) *Blocks: Settings Page styling tasks* | **Persona: Frontend Engineer**

---
**GATE: Complete all Phase 1 tasks (1.1 and 1.2) before proceeding to Phase 2. Dialog UI components in 2.2 depend on the service layer and data models.**

---

## Phase 2: Feature Development & UI Implementation

### Subphase 2.1: Frontend UI Components & State
**PREREQUISITE: Auth Logic tasks (Subphase 1.2) must complete before proceeding. Settings Page visibility depends on role checks.**

- [x] **Settings Page: Bind the expander visibility to the role-based view-model property.** (Ref: Section 3.1) *Blocks: Settings Page styling tasks* | **Persona: Frontend Engineer**
- [x] **Settings Page: Place the new image-location expander in the Settings page root `Grid` alongside existing expanders in chronological order.** (Ref: Section 3.1) *Depends on: Role visibility contract* | **Persona: Frontend Engineer**
- [x] **Settings Page: Add a `SymbolIcon` and `TextBlock` with semi-bold label to the expander header, matching Local Workcenters styling exactly.** (Ref: Section 3.1) *Depends on: Role visibility contract* | **Persona: Frontend Engineer**
- [x] **Settings Page: Apply Fluent/WinUI 3 header styling to match the existing Local Workcenters expander pattern.** (Ref: Section 3.1) *Depends on: Role visibility contract* | **Persona: Frontend Engineer**
- [x] **Settings Page: Set the expander to collapsed by default.** (Ref: Section 3.1) | **Persona: Frontend Engineer**
- [x] **Settings Card: Create the Request Type Images card with a descriptive title and summary text.** (Ref: Section 3.2) | **Persona: Frontend Engineer**
- [x] **Settings Card: Create the Work Center Images card with a descriptive title and summary text.** (Ref: Section 3.2) | **Persona: Frontend Engineer**
- [x] **Settings Card: Create the Request Subtype Images card with a descriptive title and summary text.** (Ref: Section 3.2) | **Persona: Frontend Engineer**

Next task: **Workflow: Update the waitlist detail surface to consume the resolver instead of hardcoded image assets.** (Ref: Section 5) | **Persona: Frontend Engineer**

> Implemented 2026-08-18. The three dialogs share `Views/ImageOverrideEditorControl.xaml` for the row layout,
> search, filter, and reset-all so the markup exists once. Each dialog is its own `ContentDialog` with its own
> view model. 19 view-model tests cover load, filter, grouping, inheritance, placeholders, reset, cancel, and save.

- [x] **Dialog Row: Add a read-only item label for each row in the dialog.** (Ref: Section 3.3) | **Persona: Frontend Engineer**
- [x] **Dialog Row: Add a small preview image bound to the effective resolved path.** (Ref: Section 3.3) | **Persona: Frontend Engineer** — 48px square, `Stretch="Uniform"`, via `ResolvedImagePathToSourceConverter`
- [x] **Dialog Row: Add an editable text box to accept the custom image path value.** (Ref: Section 3.3) | **Persona: Frontend Engineer**
- [x] **Dialog Row: Add a Browse button that opens a file picker for supported image types.** (Ref: Section 3.3) | **Persona: Frontend Engineer** — `FileOpenPicker` filtered to `.png`/`.jpg`/`.jpeg`
- [x] **Dialog Row: Add a Reset action that restores the JSON or default value for the row.** (Ref: Section 3.3) | **Persona: Frontend Engineer**

Next task: **Workflow: Update the waitlist detail surface to consume the resolver instead of hardcoded image assets.** (Ref: Section 5) | **Persona: Frontend Engineer**
- [x] **Dialog Footer: Add Save and Cancel actions to each image dialog.** (Ref: Section 3.3) | **Persona: Frontend Engineer**
- [x] **Dialog Behavior: Save all row edits in one commit and discard all edits on cancel.** (Ref: Section 3.3) | **Persona: Frontend Engineer** — edits stay in the rows until `SaveAsync`; a failed row cancels the close so the error stays visible
- [x] **Request Type Card: Add the eight request type rows and keep labels aligned to the JSON inventory.** (Ref: Section 3.4) | **Persona: Frontend Engineer**
- [x] **Request Type Card: Add the stable JSON `id` binding for each request type row in the modal.** (Ref: Section 3.4) | **Persona: Frontend Engineer**
- [x] **Request Type Card: Validate and preview the custom image for each request type row.** (Ref: Section 3.4) | **Persona: Frontend Engineer**
- [x] **Request Type Card: Add the global reset confirmation for the request type modal.** (Ref: Section 3.4) | **Persona: Frontend Engineer** — Reset all prompts before clearing
- [x] **Work Center Card: Add the full active catalog list for the work center modal.** (Ref: Section 3.5) | **Persona: Frontend Engineer**
- [x] **Work Center Card: Group the work center rows by building in the modal.** (Ref: Section 3.5) | **Persona: Frontend Engineer**
- [x] **Work Center Card: Add the search box and custom-image filter toggle to the work-center modal.** (Ref: Section 3.5) | **Persona: Frontend Engineer**
- [x] **Work Center Card: Show the database error state and disable Save when the catalog query fails.** (Ref: Section 3.5) | **Persona: Frontend Engineer** — real state now, driven by `GetActiveWorkCentersAsync` returning null

Next task: **Workflow: Update the waitlist detail surface to consume the resolver instead of hardcoded image assets.** (Ref: Section 5) | **Persona: Frontend Engineer**
- [x] **Work Center Card: Prune orphaned overrides after confirmation during load.** (Ref: Section 3.5) | **Persona: Frontend Engineer**
- [x] **Work Center Card: Show the row in `WorkCenterSelectionDialog` with the custom image when available.** (Ref: Section 3.5) | **Persona: Frontend Engineer**
- [x] **Work Center Card: Add the semi-transparent detail-page background image behavior for work centers.** (Ref: Section 3.5) | **Persona: Frontend Engineer**
- [x] **Subtype Card: Add parent-grouped rows for each request subtype in the subtype modal.** (Ref: Section 3.6) | **Persona: Frontend Engineer**
- [x] **Subtype Card: Add inherited-image previews with an `Inherited` badge for subtypes without explicit overrides.** (Ref: Section 3.6) | **Persona: Frontend Engineer**

Next task: **Workflow: Update the waitlist detail surface to consume the resolver instead of hardcoded image assets.** (Ref: Section 5) | **Persona: Frontend Engineer**
- [x] **Subtype Card: Show the parent image for inherited subtypes and the `Inherited` badge in the preview state.** (Ref: Section 3.6) | **Persona: Frontend Engineer**
- [x] **Subtype Card: Add the search box and custom-image filter toggle to the subtype modal.** (Ref: Section 3.6) | **Persona: Frontend Engineer**
- [x] **Subtype Card: Add the placeholder row for groups that have no subtypes.** (Ref: Section 3.6) | **Persona: Frontend Engineer**
- [x] **Subtype Card: Keep the custom image reset behavior aligned with the parent fallback path.** (Ref: Section 3.6) | **Persona: Frontend Engineer** — resolver cascade verified by `ImageLocationServiceCascadeTests`
- [x] **Subtype Card: Ensure the waitlist line card thumbnail uses the subtype image when present.** (Ref: Section 3.6) | **Persona: Frontend Engineer**

Next task: **Workflow: Update the waitlist detail surface to consume the resolver instead of hardcoded image assets.** (Ref: Section 5) | **Persona: Frontend Engineer**
- [x] **Subtype Card: Ensure the detail hero uses the subtype image when present.** (Ref: Section 3.6) | **Persona: Frontend Engineer**
- [x] **Dialog: Implement the Request Type Images `ContentDialog` and the row layout for each request type.** (Ref: Section 3.3) *Depends on: JSON Schema tasks, Service Layer resolution contract* | **Persona: Frontend Engineer**
- [x] **Dialog: Implement the Work Center Images `ContentDialog` and the row layout for each work center.** (Ref: Section 3.3) *Depends on: Service Layer catalog loading and error-state handling* | **Persona: Frontend Engineer**
- [x] **Dialog: Implement the Request Subtype Images `ContentDialog` and the row layout for each subtype.** (Ref: Section 3.3) *Depends on: JSON Schema GUID mapping, cascade resolution contract* | **Persona: Frontend Engineer**
- [x] **Workflow: Update the waitlist detail surface to consume the resolver instead of hardcoded image assets.** (Ref: Section 5) | **Persona: Frontend Engineer**

Next task: **Security Review: Confirm that only Admin and Developer roles can access the image settings expander.** (Ref: Section 6) | **Persona: Security Engineer**

### Subphase 2.2: Business Logic & Workflow Integration
**PREREQUISITE: All Subphase 1.1 and 1.2 tasks must be complete before starting this subphase. Dialog implementations require finalized JSON schema, service layer, and data models.**

- [x] **Dialog: Implement the Request Type Images `ContentDialog` and the row layout for each request type.** (Ref: Section 3.3) *Depends on: JSON Schema tasks, Service Layer resolution contract* | **Persona: Frontend Engineer**
- [x] **Dialog: Implement the Work Center Images `ContentDialog` and the row layout for each work center.** (Ref: Section 3.3) *Depends on: Service Layer catalog loading and error-state handling* | **Persona: Frontend Engineer**
- [x] **Dialog: Implement the Request Subtype Images `ContentDialog` and the row layout for each subtype.** (Ref: Section 3.3) *Depends on: JSON Schema GUID mapping, cascade resolution contract* | **Persona: Frontend Engineer**
- [x] **Workflow: Implement the full image resolution cascade for request subtypes from subtype override to parent type to default.** (Ref: Section 4.1) | **Persona: Full Stack Engineer**
- [x] **Workflow: Implement the request type resolution cascade for the default and override lookup.** (Ref: Section 4.1) | **Persona: Full Stack Engineer** — fixed 2026-08-18: JSON was overriding the database value; order is now override → JSON → default

Next task: **Security Review: Confirm that only Admin and Developer roles can access the image settings expander.** (Ref: Section 6) | **Persona: Security Engineer**
- [x] **Workflow: Implement the work-center resolution cascade for the default and override lookup.** (Ref: Section 4.1) | **Persona: Full Stack Engineer**
- [x] **Workflow: Ensure all custom image changes apply immediately without waiting for app restart.** (Ref: Section 3.4) | **Persona: Backend Engineer**
- [x] **Workflow: Update the waitlist detail surface to consume the resolver instead of hardcoded image assets.** (Ref: Section 5) | **Persona: Frontend Engineer**
- [x] **Workflow: Update the work-center selection UI to consume the resolver instead of hardcoded work-center assets.** (Ref: Section 3.5) | **Persona: Frontend Engineer**
- [x] **Workflow: Update the request type and subtype surfaces to consume the resolver instead of hardcoded assets.** (Ref: Section 5) | **Persona: Frontend Engineer**
- [x] **Workflow: Ensure the shared folder path is resolved from configuration and the database override before write operations.** (Ref: Section 4.4) | **Persona: Backend Engineer** — `IConfigSettingsValueService` implemented; covered by `ImageStorageConfigurationResolverTests`
- [x] **Workflow: Prevent file writes when the shared network folder is unavailable and show the save error to the user.** (Ref: Section 4.4) | **Persona: Backend Engineer** — write is blocked and returns `SHARE_UNREACHABLE`; surfacing it in the UI is blocked on the dialogs
- [x] **Workflow: Propagate change notifications from the image resolver to all open view models so the UI refreshes immediately.** (Ref: Section 5) | **Persona: Backend Engineer**

---
**GATE: Complete all Phase 2 tasks before proceeding to Phase 3. Testing, validation, and hardening phases depend on complete UI and workflow implementations.**

---

## Phase 3: Hardening, Security, & Testing

### Subphase 3.1: Edge-Case Testing & Validations

> 44 tests added 2026-08-18 under `MTM_Waitlist.Tests/Module_Settings/`. Tasks that require a dialog or a live
> database remain open. Test fixtures use `TestPngWriter`, which emits real decodable PNGs so dimension and
> size rules are exercised against genuine image data.

- [x] **Validation: Reject request type and subtype images larger than 10 MB.** (Ref: Section 3.4) | **Persona: QA Engineer** — `ValidateImageAsync_WhenFileExceedsMaxSize_ReturnsFileTooLarge`, plus a boundary test at exactly the limit
- [x] **Validation: Reject work-center images larger than 10 MB.** (Ref: Section 3.5) | **Persona: QA Engineer** — same scope-agnostic code path
- [x] **Validation: Reject request type and subtype images that are non-square.** (Ref: Section 3.4) | **Persona: QA Engineer** — `ValidateImageAsync_WhenImageIsNotSquare_ReturnsNotSquare`
- [x] **Validation: Reject work-center images that are non-square.** (Ref: Section 3.5) | **Persona: QA Engineer** — same scope-agnostic code path
- [x] **Validation: Allow only `.png`, `.jpg`, and `.jpeg` files in the file picker and save flow.** (Ref: Section 3.4) | **Persona: QA Engineer** — fixed a bug where the check rejected *every* extension; the `FileOpenPicker` is filtered to the same three types
- [x] **Validation: Ensure the preview renders with a 48px square layout and uniform stretch behavior.** (Ref: Section 3.4) | **Persona: QA Engineer** — 48x48 `Image` with `Stretch="Uniform"` in `ImageOverrideEditorControl`
- [x] **Validation: Ensure the work-center modal shows an error state and disables Save when the database is unavailable.** (Ref: Section 3.5) | **Persona: QA Engineer** — `WorkCenterDialog_WhenTheCatalogIsUnavailable_ShowsAnErrorAndDisablesSave`
- [x] **Validation: Ensure the missing-file fallback renders the default image with a warning in the dialog.** (Ref: Section 4.1) | **Persona: QA Engineer** — fallback + warning log covered; the in-dialog banner is blocked on the dialogs
- [x] **Validation: Ensure request type and subtype rows still resolve correctly if the JSON `imagePath` property is absent.** (Ref: Section 4.2) | **Persona: QA Engineer**
- [x] **Validation: Ensure the subtype fallback chain resolves from subtype to parent type to default asset.** (Ref: Section 4.1) | **Persona: QA Engineer**
- [x] **Validation: Ensure the modal remains usable when a parent request type has no subtypes.** (Ref: Section 3.6) | **Persona: QA Engineer** — `SubtypeDialog_ShowsAPlaceholderForParentsWithNoSubtypes`
- [x] **Validation: Ensure orphaned work-center overrides are pruned after confirmation.** (Ref: Section 3.5) | **Persona: QA Engineer** — detection covered by `WorkCenterDialog_DetectsOverridesForWorkCentersThatNoLongerExist`; pruning runs only after the confirm dialog
- [x] **Validation: Ensure global reset actions clear overrides without breaking the inherited fallback flow.** (Ref: Section 3.4) | **Persona: QA Engineer** — `ResetAllClearsEveryRow`
- [x] **Validation: Ensure a canceled dialog discards all edits without committing any override changes.** (Ref: Section 3.3) | **Persona: QA Engineer** — `CancelDiscardsEveryPendingEdit`
- [x] **Validation: Ensure the shared network path change does not break file resolution when the share is unreachable.** (Ref: Section 4.4) | **Persona: QA Engineer** — `IsShareAccessibleAsync_WhenShareIsUnreachable_ReturnsFalse` and `CopyImageToStorageAsync_WhenShareIsUnreachable_FailsWithoutWriting`
- [x] **Testing: Add unit tests for the effective-path resolution cascade and fallback behavior.** (Ref: Section 9) | **Persona: QA Engineer** — `ImageLocationServiceCascadeTests`
- [x] **Testing: Add unit tests for image validation including extension, size, and square checks.** (Ref: Section 9) | **Persona: QA Engineer** — `ImageStorageServiceValidationTests`
- [x] **Testing: Add tests that verify the `config_images_locations` read/write logic and the unique constraint behavior.** (Ref: Section 9) | **Persona: QA Engineer** — `IMySqlHelperServer` extracted so the services are fakeable; `ImageOverrideReadServiceTests` and `ImageOverrideWriteServiceTests` cover the logic, `ConfigImagesLocationsIntegrationTests` round-trips real rows and proves the unique key. Integration tests need `MTM_WAITLIST_TEST_DB_CONNECTION_STRING` and self-skip without it.
- [x] **Testing: Add tests for request type, subtype, and work-center override resolution under missing-file conditions.** (Ref: Section 9) | **Persona: QA Engineer**
- [x] **Testing: Add integration tests that verify the service layer raises notifications and open views refresh immediately after override commits.** (Ref: Section 5) | **Persona: QA Engineer** — publish/subscribe and unsubscribe covered; view refresh is blocked on consumers
- [x] **Testing: Add tests that validate the cascade resolution order (subtype -> parent -> default) and verify backward compatibility when `imagePath` is absent.** (Ref: Section 4.1) | **Persona: QA Engineer**

### Subphase 3.2: Security Auditing & Compliance
- [ ] **Security Review: Confirm that only Admin and Developer roles can access the image settings expander.** (Ref: Section 6) | **Persona: Security Engineer**
- [ ] **Security Review: Verify that non-authorized users cannot trigger any image-setting actions or dialogs.** (Ref: Section 6) | **Persona: Security Engineer**
- [ ] **Security Review: Verify the file-copy path uses the current Windows user permissions and the share ACL is restricted to approved accounts.** (Ref: Section 4.4) | **Persona: Security Engineer**
- [ ] **Security Review: Validate that the archive folder does not expose a broad write path outside the approved share scope.** (Ref: Section 4.4) | **Persona: Security Engineer**
- [ ] **Security Review: Validate that the database stores only the file path and metadata, not the full image binary.** (Ref: Section 4.3) | **Persona: Security Engineer**
- [ ] **Security Review: Confirm that the new appsettings config and DB override do not create an unguarded privilege escalation point.** (Ref: Section 4.4) | **Persona: Security Engineer**
- [ ] **Security Review: Verify that out-of-scope image generation, server uploads, and styling changes remain deliberately excluded from implementation.** (Ref: Section 7) | **Persona: Security Engineer**
- [ ] **Compliance: Verify the table and field naming conforms to the MySQL naming conventions in the repository rules.** (Ref: Section 4.3) | **Persona: Tech Lead**
- [ ] **Compliance: Verify the migration artifacts include the required rollback or validation pattern.** (Ref: Section 4.3) | **Persona: Tech Lead**
- [ ] **Compliance: Confirm the feature does not expand beyond the approved scope and leaves existing settings styling unchanged.** (Ref: Section 7) | **Persona: Tech Lead**
- [ ] **Compliance: Verify the feature does not generate, edit, or upload images outside the copy-to-share operation.** (Ref: Section 7) | **Persona: Tech Lead**

---
**GATE: Complete all Phase 3 testing, security, and compliance tasks before proceeding to Phase 4. Deployment requires passing tests and security clearance.**

---

## Phase 4: Deployment & Release Readiness

### Subphase 4.1: CI/CD Pipelines & Infrastructure

> `.github/workflows/build-and-test.yml` is the build gate. `.github/scripts/Test-DeploymentPreflight.ps1`
> performs the environment checks; it exits non-zero on a hard failure so it can gate a deployment.
> Verified against 172.16.1.104/mtm_waitlist (MySQL 5.7.24) on 2026-08-18.

- [ ] **Pipeline: Add the database migration step for the new `config_images_locations` table before deployment.** (Ref: Section 4.3) | **Persona: DevOps Engineer** — the table is already live; there is still no automated deploy pipeline to hang a migration step on
- [ ] **Pipeline: Add the request type JSON update to deployment packaging so the new GUIDs and `imagePath` fields ship with the app.** (Ref: Section 4.2) | **Persona: DevOps Engineer**
- [x] **Pipeline: Provision the shared image folder path in the target environment before release.** (Ref: Section 4.4) | **Persona: DevOps Engineer** — `X:\Software Development\Live Applications\MTM_Waitlist\Images` exists and is writable; verified by preflight
- [ ] **Pipeline: Configure the Windows share ACL for the approved write account and admin users.** (Ref: Section 4.4) | **Persona: DevOps Engineer** — preflight confirms the current user can write; a full ACL audit is still an ops task
- [x] **Pipeline: Validate the appsettings default share path matches the live environment configuration.** (Ref: Section 4.4) | **Persona: DevOps Engineer** — preflight compares appsettings against the `config_settings_values` override and warns on drift
- [x] **Pipeline: Add a deployment check that confirms the image share is reachable from the target workstation.** (Ref: Section 4.4) | **Persona: DevOps Engineer** — preflight probes reachability and write access
- [x] **Pipeline: Add a deployment check that verifies the Azure or local database connection used by the MySQL helper server is active.** (Ref: Section 5) | **Persona: DevOps Engineer** — preflight opens the connection and asserts the table plus unique key exist
- [x] **Pipeline: Add a build gate that runs the relevant MTM_Waitlist test suite before merge.** (Ref: Section 9) | **Persona: DevOps Engineer** — `build-and-test.yml` runs on every pull request; InforVisual tests are filtered out because they need the on-premises SQL Server
- [x] **Pipeline: Validate that the new settings expander builds cleanly without breaking the existing settings page layout.** (Ref: Section 3.1) | **Persona: DevOps Engineer** — the CI build compiles XAML, so an unregistered converter or bad binding fails the gate

### Subphase 4.2: Final Post-Launch Smoke Tests
- [ ] **Smoke Test: Open the Settings page as an Admin user and confirm the image-location expander is visible.** (Ref: Section 6) | **Persona: QA Engineer**
- [ ] **Smoke Test: Open the Settings page as a non-admin user and confirm the expander remains hidden.** (Ref: Section 6) | **Persona: QA Engineer**
- [ ] **Smoke Test: Open each card modal and confirm the request type, work-center, and subtype rows render correctly.** (Ref: Section 3.2) | **Persona: QA Engineer**
- [ ] **Smoke Test: Upload a valid image to a request type and confirm it saves and immediately renders in the UI.** (Ref: Section 3.4) | **Persona: QA Engineer**
- [ ] **Smoke Test: Upload a valid image to a work center and confirm it saves and immediately renders in the selection dialog and detail page.** (Ref: Section 3.5) | **Persona: QA Engineer**
- [ ] **Smoke Test: Upload a valid image to a subtype and confirm it saves and immediately renders on the line card and detail hero.** (Ref: Section 3.6) | **Persona: QA Engineer**
- [ ] **Smoke Test: Verify the default image fallback appears when a file is missing or the override is cleared.** (Ref: Section 4.1) | **Persona: QA Engineer**
- [ ] **Smoke Test: Verify the global reset action restores the default or inherited state for each card.** (Ref: Section 3.4) | **Persona: QA Engineer**
- [ ] **Smoke Test: Verify the archive behavior creates the expected dated variant when a file is replaced and preserves the active image file in place.** (Ref: Section 4.4) | **Persona: QA Engineer**
- [ ] **Smoke Test: Verify the app remains stable when the image share is unavailable during save and render operations.** (Ref: Section 4.4) | **Persona: QA Engineer**
- [ ] **Smoke Test: Verify live view refreshes occur without restarting the app after a save completes successfully.** (Ref: Section 5) | **Persona: QA Engineer**
- [ ] **Smoke Test: Verify the persisted image path remains available after reload and does not reintroduce orphaned rows.** (Ref: Section 3.5) | **Persona: QA Engineer**
- [ ] **Smoke Test: Verify the feature does not generate, edit, or upload images outside the copy-to-share operation.** (Ref: Section 7) | **Persona: QA Engineer**

---
**Readiness Status: Checklist complete. All phases, subphases, and tasks are mapped and sequenced. Feature is scoped and documented and ready for Phase 1 kickoff.**

---

## Next Steps

To begin implementation:

1. **Start with Phase 1.1 (Database Schema & Data Models)**: Create the `config_images_locations` table and update `waitlist-request-types.json` with GUID `id` and `imagePath` fields.
2. **Complete Phase 1.2 (Core API Endpoints & Auth)**: Build the service layer, configuration, and authorization logic.
3. **Gate Check**: Before Phase 2, verify all Phase 1 tasks pass code review and unit tests.
4. **Proceed sequentially**: Phase 2.1 → 2.2 → 3.1 → 3.2 → 4.1 → 4.2, with full test coverage at each gate.

---

## Persona-Driven Continuance Protocol

To continue with implementation of the next incomplete task:

1. **Scan (#file:checklist.md)** to find the first `- [ ]` task.
2. **Extract the Persona** from the task (e.g., `| **Persona: Database Engineer**`).
3. **Adopt the Persona's Working Style** (see Persona Behavior Matrix below).
4. **Execute the task** following persona-specific standards and deliverables.
5. **Update the checklist**: Change `- [ ]` to `- [x]` and state the next task clearly.

Note: You are to complete One Sub-Phase per run

---

## Persona Behavior Matrix

### 🗄️ Database Engineer
**Communication Style:** Technical, precise, schema-focused. Direct statements with SQL/DDL syntax.  
**Approach:** Backwards-compatible, defensive constraints, audit trails.  
**Deliverables:**
- SQL CREATE TABLE/INDEX scripts with comments explaining constraints
- Rollback/validation scripts with specific error handling
- Schema documentation in `Database/Bootstrap/update_table_descriptions.sql`
- Output format: Code blocks with raw SQL, then a brief verification checklist

**Tone Example:** *"I've created the `config_images_locations` table with a composite unique constraint on (`scope`, `scope_item_id`) to prevent accidental duplicates. The `updated_utc` and `updated_by_employee_id` columns provide audit compliance..."*

---

### 🔧 Backend Engineer
**Communication Style:** Implementation-focused, API-contract-driven. Code-first with context.  
**Approach:** DI registration, service contracts, error handling, defensive coding.  
**Deliverables:**
- Service class with full DI registration in `Startup.cs` or equivalent
- Input validation, exception handling, and logging at entry points
- Interface definitions with XML comments
- Unit tests or test stubs validating core logic
- Output format: Complete .cs files with full implementation, then a summary of public API and error cases

**Tone Example:** *"I've registered `IImageLocationService` in DI with constructor validation for null dependencies. The service throws `ArgumentNullException` for missing config and `InvalidOperationException` when the share path is unreachable, both logged..."*

---

### 🔗 Full Stack Engineer
**Communication Style:** Architectural and cross-cutting. Connects layers explicitly.  
**Approach:** Data model ownership, cascade/fallback contracts, JSON schema versioning.  
**Deliverables:**
- Updated JSON schema or data model class definitions
- Resolution cascade logic with clear fallback order
- Documentation of data flow (override → JSON → parent → default)
- Backwards compatibility notes (e.g., handling missing `imagePath` properties)
- Output format: Model definitions, resolution diagrams/comments, then integration notes

**Tone Example:** *"The resolution cascade follows override → JSON `imagePath` → parent request type → default asset. Since existing JSON entries lack `imagePath`, the model treats null as 'inherit from parent' automatically..."*

---

### 🎨 Frontend Engineer
**Communication Style:** UX-focused, visual-precise. Component hierarchy and state binding.  
**Approach:** MVVM patterns, binding expressions, Fluent design compliance.  
**Deliverables:**
- XAML markup for UI components with complete binding expressions
- ViewModel properties and RelayCommand implementations
- Styling alignment notes (colors, spacing, fonts matching existing panels)
- Accessibility markup (`AutomationProperties`)
- Output format: Complete .xaml and .xaml.cs code, then before/after layout screensLocal descriptions

**Tone Example:** *"I've created the expander header with a `SymbolIcon` bound to a view-model property, styled to match Local Workcenters with semi-bold text and 12px spacing. The row layout uses a `UniformGrid` for alignment and 48px square preview images with `Stretch='Uniform'`..."*

---

### ✅ QA Engineer
**Communication Style:** Evidence-based, test-driven. Acceptance criteria and edge cases.  
**Approach:** Comprehensive test coverage, edge case isolation, reproducible failure steps.  
**Deliverables:**
- Unit test files with [Theory]/[Fact] or [TestMethod] attributes
- Test names describing the condition (e.g., `WhenImageOver10MB_ThenRejectAndLogWarning`)
- Edge case matrix (null, empty, boundary values)
- Integration test setup with mocks and test data
- Output format: Complete test files with assertions and comments, then a test summary matrix

**Tone Example:** *"I've written 12 unit tests covering the cascade resolver: 3 for override present, 3 for JSON fallback, 3 for parent inheritance, 3 for default asset. All 40 edge cases from the spec (missing files, null paths, empty overrides) are isolated in parametrized tests..."*

---

### 🔐 Security Engineer
**Communication Style:** Risk-focused, threat-modeling aware. CVSS/impact language.  
**Approach:** Privilege separation, deny-by-default, audit logging, compliance-first.  
**Deliverables:**
- Security review checklist with PASS/FAIL/REVIEW status for each item
- Threat analysis (e.g., "File path injection via override" → "Mitigation: Whitelist UNC prefixes")
- Privilege boundary documentation (Admin vs. Dev vs. User roles)
- Audit logging requirements (who changed what, when)
- Output format: Security audit report with findings, then risk matrix and remediation plan

**Tone Example:** *"PASS: Role-based expander visibility enforced via `IAuthorizationService`. Non-admin users have no access to the entire panel. However, REVIEW NEEDED: Verify the share ACL denies write access to unprivileged accounts before deployment..."*

---

### 👔 Tech Lead
**Communication Style:** Strategic and arch-focused. Design tradeoffs and long-term implications.  
**Approach:** Standards enforcement, pattern documentation, compliance mapping.  
**Deliverables:**
- Design decision document with tradeoffs (e.g., database override vs. environment variable)
- Compliance checklist mapping tasks to standards (.github/instructions/)
- Architecture diagram or written description of component interactions
- Guidelines for future maintenance
- Output format: Decision record with rationale, then a compliance checklist

**Tone Example:** *"Decision: Store image share path in `config_settings_values` (database) rather than `appsettings.json` (deployment artifact) to allow admins to change the path at runtime without redeployment. Trade-off: Added database call on startup; mitigated by caching..."*

---

### 🚀 DevOps Engineer
**Communication Style:** Infrastructure-focused, operational. Deployment steps and rollback procedures.  
**Approach:** Infrastructure-as-code, automation, environment parity, graceful degradation.  
**Deliverables:**
- CI/CD pipeline script or YAML (GitHub Actions, Azure Pipelines, etc.)
- Deployment checklist with pre/post-deployment validation
- Rollback procedure and rollback testing evidence
- Environment variable/config documentation for target environments
- Output format: Pipeline scripts with comments, then deployment runbook with step-by-step instructions

**Tone Example:** *"I've added a pre-deployment gate that validates the share path is reachable from the target workstation and the database connection is active. If either check fails, the pipeline stops before deployment. Rollback reverts the table migration and removes the `appsettings.json` override..."*

---

Use the project structure, conventions, and existing patterns from [Module_Settings](Module_Settings), [Module_Shared](Module_Shared), and [Database](Database) folders. Follow the naming rules in `.github/instructions/` and respect the out-of-scope boundaries defined in Section 7 of the spec.

---

## Personas Reference Guide

This checklist is organized by ownership personas. Each task is tagged with the primary persona responsible for that work:

| Persona | Role & Responsibility | Primary Focus |
|---------|----------------------|----------------|
| **Database Engineer** | Design and implement database schema, migrations, constraints, and rollback artifacts | Phase 1.1: Schema creation, indexes, audit columns, documentation |
| **Backend Engineer** | Build service layers, configuration, API endpoints, authentication, and file handling | Phase 1.2: Services, config, auth; Phase 2.2: Workflows, persistence |
| **Full Stack Engineer** | Manage data models, JSON schema, configuration, and cross-layer concerns | Phase 1.1/1.2: Data models, JSON updates; Phase 2.2: Cascade logic |
| **Frontend Engineer** | Build WinUI 3 UI components, dialogs, cards, and view-model bindings | Phase 2.1/2.2: Settings page, dialogs, cards, UI workflows |
| **QA Engineer** | Design and execute unit tests, integration tests, validation, and smoke testing | Phase 3.1/3.2: Testing, validation; Phase 4.2: Smoke tests |
| **Security Engineer** | Perform security reviews, access control validation, and compliance auditing | Phase 3.2: Security review, data protection, privilege checking |
| **Tech Lead** | Establish architecture, data model contracts, compliance standards, and design decisions | Phase 1.1/1.2: Conceptual design; Phase 3.2: Compliance enforcement |
| **DevOps Engineer** | Manage CI/CD pipelines, deployment, infrastructure provisioning, and build gates | Phase 4.1: Pipeline setup, environment configuration, deployment checks |

---
