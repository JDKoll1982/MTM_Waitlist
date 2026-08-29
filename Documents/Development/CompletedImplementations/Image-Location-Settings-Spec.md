# Image Location Settings Spec

Status: Ready to implement
Owner module: `Module_Settings`
Related modules: `Module_Waitlist` (request type/subtype rendering), `Module_Core` (navigation, local settings)

## 1. Goal

Add a new collapsible settings panel that lets authorized users view and change the image
locations used for:

1. Request types
2. Work centers
3. Request subtypes

The panel follows the existing `muxc:Expander` pattern already used by the Appearance,
Mock Data, Local Workcenters, Dunnage Type Visibility, and About panels in
[Module_Settings/Views/SettingsPage.xaml](Module_Settings/Views/SettingsPage.xaml).

## 2. Confirmed Decisions

| # | Decision | Answer |
| --- | ---------- | -------- |
| 1 | Placement | New `Expander` on the existing Settings page |
| 2 | Row editing | Editable path text box + Browse (file picker) button per row |
| 3 | Work center images | New persisted `ImagePath` setting per work center |
| 4 | Subtype images | Add `imagePath` to the `waitlist-request-types.json` schema |
| 5 | Persistence | JSON provides defaults; overrides persist in a new shared MySQL table |
| 6 | Access control | Restricted to Admin and Developer roles |
| 7 | Row keys | Stable numeric/id keys, not display names, for all three cards |

### Default fallback images

| Scope | Default asset |
| ------- | -------------- |
| Request types and subtypes | `Assets\Images\default-request-type.png` |
| Work centers | `Assets\Images\default-workstation-image.png` |

## 3. UI Structure

### 3.1 Expander

- Placed in the Settings page root `Grid` alongside existing expanders.
- Header: `SymbolIcon` + `TextBlock` with bold semi-bold label, matching Local Workcenters.
- `x:Load` bound to a view-model visibility property driven by the role check.
- `IsExpanded="False"` by default.

### 3.2 Panel body

The expander body contains three `wct:SettingsCard` items. Each card has:

- A title.
- A short description of what the images are used for.
- A single primary `Button` that opens a modal.

| Card | Description | Modal |
| ------ | ------------- | ------- |
| Request Type Images | Images shown on waitlist line cards and detail heroes for each top-level request type | Request Type Images dialog |
| Work Center Images | Images representing each work center in selection and detail surfaces | Work Center Images dialog |
| Request Subtype Images | Images for each request subtype defined in the request type configuration | Request Subtype Images dialog |

### 3.3 Modal contents

Each modal is a `ContentDialog` that lists every item in its scope. Per row:

- Item name (read-only).
- Small image preview resolved from the effective path.
- Editable path `TextBox` bound to the row's override value.
- `Browse...` `Button` opening a file picker for image files.
- `Reset` action returning the row to the JSON/default value.

Dialog footer provides Save and Cancel. Save commits all row changes; Cancel discards.

### 3.4 Card 1 of 3 — Request Type Images

Scope: the top-level `requestType` entries in
[Assets/Config/waitlist-request-types.json](Assets/Config/waitlist-request-types.json).

Current inventory (8 rows):

| # | Request type |
| --- | -------------- |
| 1 | Pickup |
| 2 | Other |
| 3 | Coil |
| 4 | Scrap |
| 5 | Flatstock |
| 6 | Table Handling |
| 7 | Die Handling |
| 8 | Forklift Assist |

Row key: a stable numeric `id` added to each request type in the JSON configuration.
The `requestType` string remains the display label only, so renames do not orphan overrides.
Default when unresolved: `Assets\Images\default-request-type.png`.

#### Confirmed behavior

| Topic | Decision |
| ------- | ---------- |
| Row key | Stable `id` field added to each request type in JSON |
| Browsed file handling | File is copied into the shared network folder; the copied path is stored |
| Allowed file types | `.png`, `.jpg`, `.jpeg` |
| Validation | Reject files over 10 MB or non-square images |
| Preview | 48px square, `Stretch="Uniform"` |
| Save semantics | Save commits all rows; Cancel discards all |
| Reset all | Provided, guarded by a confirmation prompt |
| Missing file | Render the default asset with an inline warning; keep the stored path |
| Subtype cascade | Resolution order is subtype -> parent request type -> default asset |
| Apply timing | Immediate, via change notification to open views |

### 3.5 Card 2 of 3 — Work Center Images

Scope: all active rows in `setup_workstations_catalog`, retrieved through the existing
[Module_Shared/Services/WorkCenterCatalogService.cs](Module_Shared/Services/WorkCenterCatalogService.cs)
path. The list is unfiltered by workstation; sorting and filtering happen in the dialog.

Row key: `setup_workstations_catalog.id`.
Default when unresolved: `Assets\Images\default-workstation-image.png`.

#### 3.5 Confirmed behavior

| Topic | Decision |
| ------- | ---------- |
| Source list | All active catalog rows, not limited to Local work centers |
| Row key | `setup_workstations_catalog.id` |
| Grouping | Grouped by building |
| Finding rows | Search box plus a "has custom image" filter toggle |
| Bulk assign | Not supported; one row at a time |
| File rules | Identical to Card 1 (copy to shared network folder, `.png`/`.jpg`/`.jpeg`, reject over 10 MB or non-square, 48px preview, batch Save/Cancel, Reset all with confirmation, missing-file warning) |
| Database unavailable | Modal shows an error state and Save is disabled |
| Orphaned overrides | Pruned automatically on load, after a confirmation prompt |
| Consumers | Rows in `WorkCenterSelectionDialog`, and a semi-transparent background image on the waitlist detail page (not on waitlist line cards) |

### 3.6 Card 3 of 3 — Request Subtype Images

Scope: every entry in the `subtypes` array of each request type in
[Assets/Config/waitlist-request-types.json](Assets/Config/waitlist-request-types.json).
Current inventory is 24 subtypes across 8 parent types. Subtype names are not unique
across parents (`Bring` and `Pickup` each appear under both Coil and Flatstock).

Row key: a globally unique stable `id` added to each subtype in the JSON configuration.
Default when unresolved: parent request type image, then `Assets\Images\default-request-type.png`.

#### 3.6 Confirmed behavior

| Topic | Decision |
| ------- | ---------- |
| Row key | Globally unique stable `id` per subtype in JSON |
| Grouping | Collapsible groups by parent request type |
| Parents with no subtypes | Group is shown with a "no subtypes" placeholder row |
| Inherited preview | Shows the parent image with an "Inherited" badge |
| Per-row Reset | Clears the override so the parent image is inherited again |
| Finding rows | Search box plus "has custom image" filter toggle, same as Card 2 |
| File rules | Identical to Cards 1 and 2 |
| Consumers | Waitlist line card thumbnail and detail hero |

## 4. Data Model

### 4.1 Effective path resolution

```flow
override (local settings)  ->  json configured value  ->  parent request type  ->  scope default asset
```

The parent step applies only to subtype resolution.
If the resolved file does not exist on disk, fall back to the scope default asset
and surface an inline warning in the editing dialog.

### 4.2 Configuration change

`Assets/Config/waitlist-request-types.json` gains:

- a stable `id` (GUID) on each request type object
- a stable `id` (GUID) on each entry in the `subtypes` array
- an optional `imagePath` at both levels

GUIDs are the durable row keys for Cards 1 and 3, so renaming a display name never orphans
an override. Existing entries without `imagePath` remain valid and resolve through the
cascade to the default asset.

### 4.3 Override storage

All three cards share one new MySQL table holding image locations application-wide:
`config_images_locations`.

Proposed columns:

| Column | Purpose |
| -------- | --------- |
| `id` | Primary key |
| `public_id` | Public UUID |
| `scope` | `request_type`, `work_center`, or `request_subtype` |
| `scope_item_id` | Stable key of the target row within the scope |
| `image_path` | Stored path to the copied image |
| `is_active` | Soft-enable flag |
| `created_utc` / `updated_utc` | Audit timestamps |
| `updated_by_employee_id` | Who last changed the row |

Unique constraint: `uq_config_images_locations_scope_item` on (`scope`, `scope_item_id`).

Image overrides no longer use `ILocalSettingsService`. Local settings remain the storage
for unrelated feature toggles only.

### 4.4 Image file storage

Browsed files are copied into a shared network folder, not local app data, so that a custom
image selected on one workstation resolves on every other workstation. The stored
`image_path` points at the shared-folder copy.

| Topic | Decision |
| ------- | ---------- |
| Configuration | New options block in [appsettings.json](appsettings.json) for the default, overridable by a `config_settings_values` row so admins can change it in-app |
| UNC path | `X:\Software Development\Live Applications\MTM_Waitlist\Images` |
| Write account | The current Windows user; the share ACL grants write to admins |
| Share unreachable on Save | Block the save with a clear error and keep the dialog open |
| Share unreachable on read | Fall back to the scope default asset silently |
| File naming | Deterministic `{scope}_{row id}{extension}`, overwritten in place |
| Replaced files | Previous copies are archived as `{filename}-MM-DD-YYYY-NN.ext` in an `Archive` subfolder; the active file remains the current image |

## 5. Services

A new DI-registered service in `Module_Settings` owns:

- Loading request types and subtypes from the JSON configuration.
- Loading the work center list through `IWorkCenterCatalogService`.
- Reading and writing overrides in `config_images_locations` through the MySQL helper server.
- Copying selected image files into the shared network folder and validating type, size, and aspect.
- Resolving effective image paths for consumers and raising change notifications.

Waitlist rendering surfaces consume the resolver instead of hardcoded asset paths.

## 6. Access Control

- Panel visible only to Admin and Developer roles.
- Visibility driven from the existing role/privilege mechanism used by other
  role-restricted settings surfaces.
- Non-authorized users never see the expander.

## 7. Out of Scope

- Generating or editing images.
- Uploading images to a shared server.
- Changing the visual style of existing settings panels.

## 8. Open Questions

### 8.1 Card 1 of 3 — Request Type Images

Resolved. See section 3.4. Note the row-key change to a stable JSON `id`.

### 8.2 Card 2 of 3 — Work Center Images

Resolved. See section 3.5.

### 8.3 Card 3 of 3 — Request Subtype Images

Resolved. See section 3.6.

### 8.4 Cross-cutting

Resolved. See sections 4.2, 4.3, and 4.4.

## 9. Testing

Following the existing [MTM_Waitlist.Tests](MTM_Waitlist.Tests) layout:

- Unit tests for effective-path resolution across the full cascade, including the
  subtype -> parent -> default order and the missing-file fallback.
- Unit tests for file validation (extension allow list, 10 MB limit, square-aspect check).
- Tests covering the `config_images_locations` read/write layer, including orphan pruning
  and the unique constraint on (`scope`, `scope_item_id`).

## 10. Implementation Sequence

1. Add the `config_images_locations` table artifacts under `Database/Tables/` with matching
   rollback, plus the required update to `Database/Bootstrap/update_table_descriptions.sql`.
2. Add GUID `id` values to every request type and subtype in the JSON configuration.
3. Add the image storage options block and the `config_settings_values` override path.
4. Build the resolver/persistence service and register it in DI.
5. Add tests for resolution, validation, and persistence.
6. Build the Settings expander, the three cards, and the three dialogs.
7. Wire consumers: waitlist line cards, detail hero, detail background, and
   `WorkCenterSelectionDialog` rows.

Resolved. See sections 4.2, 4.3, and 4.4.

Only remaining input needed: the actual shared folder UNC path.
