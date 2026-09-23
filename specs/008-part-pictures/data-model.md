# Phase 1 Data Model: A Picture for Every Part

Everything below is either an existing artifact this feature reshapes or a new one it introduces. Column names follow
`database-schema-rules.instructions.md`: lower-case snake case, `public_id` for the external identifier, `is_` for
booleans, `_utc` for UTC timestamps, and every constraint and index name inside 64 characters.

## Stored picture row — `config_images_locations`, reshaped

One row is one picture. The row already exists; what changes is the set of values its `scope` column may hold, and
therefore what its key means.

| Field | Type | Rule |
| --- | --- | --- |
| `id` | `BIGINT` | Unchanged surrogate key. |
| `public_id` | `CHAR(36)` | Unchanged. |
| `scope` | `VARCHAR(16)` | Widened from three values to five. `request_item`, `request_category`, `work_center`, and the two new ones: **`visual_part`** and **`wip_part`**. |
| `scope_item_id` | `VARCHAR(190)` | Unchanged width, widened meaning: for a part scope it is the **part number**, and 190 characters is the longest number this feature will accept (FR-008). |
| `image_path` | `VARCHAR(500)` | The path **relative** to the configured picture root (FR-010), and now naming the collection folder as well: `Visual/MMC/MMC0001000.png`, `WIP/Categorized Parts/12345-6.png`, `Waitlist/request_item/pickup-coil.png`. A rooted value written before the layout changed is still read. |
| `is_active` | `TINYINT(1)` | Unchanged. An inactive row is not a picture. |
| `created_by_user_id`, `updated_by_user_id` | `BIGINT` | Unchanged, and a foreign key to `core_users_profiles`. |
| `created_utc`, `updated_utc` | `DATETIME` | Unchanged. |

**Identity**: one row per `(scope, scope_item_id)`, enforced by the existing `uq_config_images_locations_scope_item`.
For a part, that pair is the system and the part number together, **never the part number alone** (FR-002, FR-006).
Two rows with the same `scope_item_id` under `visual_part` and `wip_part` are two different parts and are expected.

**Relationship**: one row has many change records. One row is referenced by no other table.

## Picture change record — `config_images_locations_history`, new

One row per set or replace (FR-027). Written in the same transaction as the row it describes, so a change is never
recorded without happening or happening without being recorded.

| Field | Type | Rule |
| --- | --- | --- |
| `id` | `BIGINT` | Surrogate key. |
| `public_id` | `CHAR(36)` | Unique. |
| `image_location_id` | `BIGINT` | The picture row this change belongs to. Foreign key to `config_images_locations.id`, `ON DELETE SET NULL`, so retiring a picture row does not erase its history. |
| `scope` | `VARCHAR(16)` | Copied at write time, so the record still says which system it was even if the picture row is retired. |
| `scope_item_id` | `VARCHAR(190)` | Copied at write time, for the same reason. |
| `previous_image_path` | `VARCHAR(500)` | **Null for a first picture.** Otherwise the relative path it replaced — the answer to "what did it replace". |
| `new_image_path` | `VARCHAR(500)` | The relative path that replaced it. |
| `changed_by_user_id` | `BIGINT` | The actor. Foreign key to `core_users_profiles(id)`, `ON DELETE SET NULL`. |
| `changed_utc` | `DATETIME` | The UTC moment. |

**Index**: `idx_config_images_locations_history_scope_item (scope, scope_item_id, changed_utc)`, left to right by
filter then ordering, which is how the record is read back.

**State transitions of a part picture**

| From | Event | To | What is written |
| --- | --- | --- | --- |
| absent | a picture is set | pictured | one row, one history row with a null predecessor |
| pictured | a picture is replaced | pictured, the previous file archived beside it | the row updated in place, one history row carrying the replaced path |
| pictured | the picture is withdrawn | inactive | the row's `is_active` cleared; no file is removed |
| archived | the retention period has passed | removed | the file is deleted; the history row stays |
| pictured | the part's file name belongs to another part | refused, unchanged | nothing: FR-007's refusal happens before any write |

## Part picture — the shape a surface reads

Not a table. The value every converted surface asks for, and the only thing those surfaces know.

| Field | Meaning | Rule |
| --- | --- | --- |
| System | Which of the three collections the picture belongs to | `Waitlist`, `Visual` or `WIP`. A Visual part and a WIP part with one number are two pictures (FR-002). |
| Part number | The key inside the system | 1 to 190 characters. Decides the family folder by its prefix (FR-012). |
| Family folder | Where inside the collection the file sits | `MMC`, `MMF`, `FGT`, or `Categorized Parts` for a number no prefix recognises (FR-005). |
| Relative path | What the store holds | Relative to the configured root, collection and folder included (FR-004, FR-010). |
| Resolved file | The absolute path a control draws from | The local copy when there is one, otherwise the source. Null when there is no picture, and then the caller draws the one shared placeholder (FR-014). |

**Validation at the trust boundary**: a picture is drawn only when the file exists, can be read as a picture, and
measures at least 48 by 48 pixels and square within 0.02 — the existing rule in `ImagePicturePolicy`, unchanged
(FR-013, FR-034). A file that fails it is not an error and not a picture: the placeholder is drawn and the miss is
recorded (FR-029).

## Part family

The group a part number puts it in, and the folder that follows from it.

| Prefix | Family | Folder |
| --- | --- | --- |
| `MMC` | Coil | `MMC` |
| `MMF` | Flatstock | `MMF` |
| `FGT` | Die | `FGT` |
| anything else | unrecognised | `Categorized Parts` |

Compared case-insensitively, so `mmc0001000` and `MMC0001000` are one family and one file name — which is also why
two numbers that differ only by case cannot both be pictured (FR-007's edge case).

## System folder

Which of the three collections a picture lives in, and therefore the first folder under the configured root.

| Collection | Folder | Holds | Kinds inside |
| --- | --- | --- | --- |
| the application's own | `Waitlist` | what the application pictures for its own screens | `request_item`, `request_category`, `work_center` — one folder each, never shared (FR-038) |
| Infor Visual parts | `Visual` | `visual_part` rows | one folder per family, plus `Categorized Parts` |
| WIP floor parts | `WIP` | `wip_part` rows | one folder per family, plus `Categorized Parts` |

Because the three kinds inside `Waitlist` have their own folders, the item `other` and the category `Other` both keep
their names and resolve to different files. No picture and no part is ever renamed to avoid a collision.

## Local copy

This machine's copy of one picture, made the first time that part is drawn (FR-030). Not a table: a folder and a
rule.

| Field | Meaning | Rule |
| --- | --- | --- |
| Cache root | Where this machine keeps its copies | The existing configured cache folder, defaulting to `%LOCALAPPDATA%\MTM_Waitlist\ImageCache`. |
| Collection folder | Which part collection a copy belongs to | Its own folder per collection, so the same file name from `Visual` and `WIP` cannot be one copy. |
| Source | Where the picture is read from before any copy exists | The configured picture root. |
| Freshness | Whether the copy has to be made again | Recopied when the copy is missing, or its size or last-written time differs from the source's. Otherwise nothing is copied (FR-030). |
| Unreachable source | A source that cannot be read | Recorded, the existing copy left in place, and the screen still opens (FR-031). |
| Empty source | A source that answers with nothing | **Never** evidence that pictures were removed; nothing already copied is deleted (FR-032). |

## Storage setting

The two folders and the retention period, all four already stored as organisation-wide settings in
`config_settings_values` and read through the existing resolver.

| Setting key | What it is | Default | Who may change it |
| --- | --- | --- | --- |
| `image_storage.shared_folder_path` | The root every picture sits under | `AppStoragePaths.ImagesRootDefault` | `permission.settings.storage_paths` — IT Department and Developer (FR-017) |
| `keys.folder_path` | The folder holding the key files | `AppStoragePaths.KeysFolderDefault` | the same entitlement (FR-016) |
| `image_storage.archive_keep_days` | How long a replaced picture is kept | 90 days (FR-037, raised from the shipped 30) | the same entitlement |
| `image_cache.enabled`, `image_cache.folder_path` | Whether and where this machine caches | enabled, `%LOCALAPPDATA%\MTM_Waitlist\ImageCache` | the storage entitlement, moved in this change (research D10) |

The root itself is a setting and is never compiled in (FR-009). The layout **inside** it is compiled, because the
folder names are how a person finds a file by hand and how the bulk file-drop path works (FR-028); what makes a
record portable is that it is stored relative to the root, not that the folders are configurable.

## Entitlement

Not a new artifact. `permission.settings.part_pictures` exists with its label, its gate sites and its shipped
baselines; this change widens its baseline to two more roles.

| Role | Holds it today | Holds it after | Why |
| --- | --- | --- | --- |
| `developer` | yes | yes | unchanged |
| `it_department` | yes | yes | unchanged |
| `plant_manager` | no | **yes** | FR-025 |
| `setup_lead` | no | **yes** | FR-025 |
| the other five roles | no | no | unchanged |

`permission.settings.storage_paths` is **not** widened: IT Department and Developer keep the folders (FR-017).
