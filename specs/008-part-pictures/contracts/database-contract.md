# Contract: Schema, Stored Procedures and the Move

The database interface this feature exposes. Every artifact here ships `create.sql` and `rollback.sql` in the same
change, and the hand-maintained aggregates (`AllTables.sql`, `AllSPs.sql`, `AllSeeds.sql`) plus
`Database/Bootstrap/update_table_descriptions.sql` move with them, per constitution III and
`database-schema-rules.instructions.md`.

**The target server is MySQL 5.7.** Every artifact header names 5.7 deliberately: that is the version this goes on
when it goes live. A local development server running a newer build is expected and is not a reason to change a
header to match it.

## Tables

| Artifact | Change |
| --- | --- |
| `Tables/17_config_images_locations/` | **No column changes.** The `scope` column's comment is widened to name the five values it now holds. The unique key `uq_config_images_locations_scope_item` is untouched and is what enforces one picture per system and key. |
| `Tables/33_config_images_locations_history/` | **New.** One row per set or replace. |

The folder number 33 follows 32, which is the highest in use.

The new table is also applied to a store that already exists, by a guarded `information_schema` check and a prepared
`ALTER TABLE` in `Bootstrap/update_table_descriptions.sql`, following the block that already adds
`setup_work_centers_catalog.building`. `CREATE TABLE IF NOT EXISTS` alone would not create the table in a live store
if the create path had already run, which is why the guarded block is required rather than optional. The same file
carries the widened `scope` comment on an existing store.

**Columns of `config_images_locations_history`**: `id`, `public_id`, `image_location_id`, `scope`,
`scope_item_id`, `previous_image_path`, `new_image_path`, `changed_by_user_id`, `changed_utc`. Nullability,
the two foreign keys and the index are specified in `data-model.md`.

**Constraint and index names**, all inside 64 characters:

```text
uq_config_images_locations_history_public_id
idx_config_images_locations_history_scope_item
fk_config_images_locations_history_image_location_id
fk_config_images_locations_history_changed_by_user_id
```

## Stored procedures

Two existing procedures change, and three are new. No statement text enters application code: the picture write
stays on the procedure path it is on today, so one write owns one picture (constitution III).

| Procedure | Contract |
| --- | --- |
| `sp_config_images_locations_insert(p_scope, p_scope_item_id, p_image_path, p_created_by_user_id)` | **Changed.** Signature unchanged; it additionally writes one history row in the same transaction, with a null `previous_image_path`, and still returns the new row's id. |
| `sp_config_images_locations_update(p_public_id, p_image_path, p_updated_by_user_id)` | **Changed.** Signature unchanged; it additionally writes one history row in the same transaction carrying the `image_path` it replaced. |
| `sp_config_images_locations_history_get(p_scope, p_scope_item_id)` | **New.** Returns `previous_image_path`, `new_image_path`, `changed_by_user_id`, `changed_by_display_name`, `changed_utc`, newest first. Answers FR-027's "readable". |
| `sp_config_images_locations_scope_paths_get(p_scope)` | **New.** Returns `scope_item_id`, `image_path` for every active row of one scope. Two callers: the file-name collision check before a save, and the subtraction that produces the missing-picture list. |
| `sp_config_images_locations_paths_move(p_old_prefix, p_new_prefix)` | **New.** Rewrites `image_path` for active rows whose value begins with the old prefix, and reports the number of rows affected through the non-query seam. Run by the move, reversed by its paired rollback, and safe to run twice. |

## Queue scripts

These run against other stores and are not procedures in `mtm_waitlist`. They follow the existing queue convention
and are copied into the test project's output as real content.

| Artifact | Change |
| --- | --- |
| `Database/MTMWipApp/Queues/Module_Waitlist/Queues/GetWipInventoryPartNumbers.sql` | **New.** The distinct part numbers the WIP floor inventory holds. Read through `WipFloorInventoryService`, beside the existing `GetWipFloorQuantities.sql` and the same `FloorSnapshotScriptName` convention, so the store, the seam and the script-name pattern are unchanged. |
| `Database/InforVisual/Queues/Module_Setup/Queries/GetSubordinateParts.sql` | **Reused unchanged.** The Visual part numbers the application can name come through the existing subordinate-parts read shape, so no new read crosses the Visual boundary and the cached fallback applies as it already does. |

## Seeds

| Artifact | What it does | Its rollback |
| --- | --- | --- |
| `Seeds/seed_permission_role_baselines/` | **Changed.** `permission.settings.part_pictures` becomes 1 for `role:setup_lead` and for `role:plant_manager`, which is FR-025. Every other row is untouched, and `permission.settings.storage_paths` is **not** widened. | Restores those two rows to the value they held, and nothing else. |
| `Seeds/seed_picture_layout_move/` | **New.** Rewrites each recorded `image_path` from the pre-move shape to the new one — the folder level each collection and kind gained — and reports, rather than silently skipping, any row whose value it does not recognise, so FR-036's "none left unresolvable" is answered by a report and not by a guess. | Rewrites each value it changed back to the value it replaced, from the same table of pairs. |

`Database/Seeds/AllSeeds.sql` is regenerated in the same change, and so are `Database/Tables/AllTables.sql` and
`Database/StoredProcedures/AllSPs.sql`. The `Database/Mock/` aggregates are untouched: nothing in this feature
changes the mock store.

## The move

Two mechanisms, because the files and the rows move at different moments and the sequence can be interrupted
between them (research D13).

1. **The reader resolves both layouts.** `AppStoragePaths.ResolvePicturePath` answers for a value that names the
   pre-move place and for one that names the post-move place, so no picture is unresolvable while the move is
   incomplete and no picture has to be set again by hand. This is what satisfies FR-036 unconditionally.
2. **`tools/Move-PartPictureLayout.ps1` moves the files, and `Seeds/seed_picture_layout_move` rewrites the recorded
   values.** The tool takes the configured root, reports what it will move and what it moved, refuses to overwrite an
   existing file, and leaves everything it does not recognise where it is. It is run once, by hand, reviewed, and it
   is never run from application startup, which the schema rules require.

## Validation

`Validation/part_pictures_schema/validate.sql` proves the five scope values the stores and seeds write, the new
table's columns, keys and index, and that each of the three new procedures and the two changed ones exists and
answers.

## Live checks

An opt-in integration test asserts, against a live `mtm_waitlist`, that a first save writes one history row with a
null predecessor, a replacement writes one carrying the replaced path, the record reads back newest first, and the
move procedure reports the rows it rewrote and reverses exactly. It uses `Assert.Inconclusive` when no live store is
reachable, exactly as the existing procedure integration tests do.
