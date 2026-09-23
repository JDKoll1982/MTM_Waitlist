-- Validate part-picture schema artifacts
-- Engine: MySQL 5.7
-- Feature: 008-part-pictures (task T028)
--
-- What this file is
--   The same shape as Database/Validation/settings_schema/validate.sql: one SELECT whose rows are
--   the problems, filtered to the rows whose actual_value is 'missing'. A clean run returns no rows,
--   and .github/scripts/validate-database-schema.ps1 counts them by wrapping this file in
--   `SELECT COUNT(*) FROM ( … ) AS validation_issues`, so it must stay a single statement.
--
-- What it proves
--   * The widened `scope` comment on `config_images_locations` still names all five values, so a
--     reader of the store can see that a part picture is keyed by its system and not by its number.
--   * `config_images_locations_history` has its nine columns, its unique key, its read index, and
--     both foreign keys deleting to NULL so retiring a picture cannot erase its record.
--   * Every procedure this feature adds or changes is deployed, and the deployed body is the one
--     that answers: each carries the marker unique to its shipped definition. Existence alone would
--     pass against a store still holding the old body, which is exactly the failure worth catching
--     here, because the two changed procedures are unchanged in name and signature.
--   * Both picture-change triggers are deployed, because they, and not the procedures, are what write
--     the history row.

USE mtm_waitlist;

SELECT
    issue_type,
    object_name,
    expected_value,
    actual_value
FROM (
        -- The five scope values the widened column comment must name.
        SELECT
            'narrowed_scope_comment' AS issue_type, 'config_images_locations.scope' AS object_name,
            'comment names request_item, request_category, work_center, visual_part and wip_part' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'config_images_locations'
                        AND column_name = 'scope'
                        AND column_comment LIKE '%request_item%'
                        AND column_comment LIKE '%request_category%'
                        AND column_comment LIKE '%work_center%'
                        AND column_comment LIKE '%visual_part%'
                        AND column_comment LIKE '%wip_part%'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_table', 'config_images_locations_history', 'table exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.tables
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'config_images_locations_history'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'config_images_locations_history.id', 'BIGINT NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'config_images_locations_history'
                        AND column_name = 'id'
                        AND data_type = 'bigint'
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'config_images_locations_history.public_id', 'CHAR(36) NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'config_images_locations_history'
                        AND column_name = 'public_id'
                        AND data_type = 'char'
                        AND character_maximum_length = 36
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'config_images_locations_history.image_location_id', 'BIGINT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'config_images_locations_history'
                        AND column_name = 'image_location_id'
                        AND data_type = 'bigint'
                        AND is_nullable = 'YES'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'config_images_locations_history.scope', 'VARCHAR(16) NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'config_images_locations_history'
                        AND column_name = 'scope'
                        AND data_type = 'varchar'
                        AND character_maximum_length = 16
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'config_images_locations_history.scope_item_id', 'VARCHAR(190) NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'config_images_locations_history'
                        AND column_name = 'scope_item_id'
                        AND data_type = 'varchar'
                        AND character_maximum_length = 190
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'config_images_locations_history.previous_image_path', 'VARCHAR(500) NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'config_images_locations_history'
                        AND column_name = 'previous_image_path'
                        AND data_type = 'varchar'
                        AND character_maximum_length = 500
                        AND is_nullable = 'YES'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'config_images_locations_history.new_image_path', 'VARCHAR(500) NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'config_images_locations_history'
                        AND column_name = 'new_image_path'
                        AND data_type = 'varchar'
                        AND character_maximum_length = 500
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'config_images_locations_history.changed_by_user_id', 'BIGINT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'config_images_locations_history'
                        AND column_name = 'changed_by_user_id'
                        AND data_type = 'bigint'
                        AND is_nullable = 'YES'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'config_images_locations_history.changed_utc', 'DATETIME NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'config_images_locations_history'
                        AND column_name = 'changed_utc'
                        AND data_type = 'datetime'
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_key', 'uq_config_images_locations_history_public_id', 'unique key exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.statistics
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'config_images_locations_history'
                        AND index_name = 'uq_config_images_locations_history_public_id'
                        AND non_unique = 0
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_index', 'idx_config_images_locations_history_scope_item', 'index exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.statistics
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'config_images_locations_history'
                        AND index_name = 'idx_config_images_locations_history_scope_item'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_foreign_key', 'fk_config_images_locations_history_image_location_id', 'ON DELETE SET NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.referential_constraints
                    WHERE
                        constraint_schema = DATABASE()
                        AND table_name = 'config_images_locations_history'
                        AND constraint_name = 'fk_config_images_locations_history_image_location_id'
                        AND referenced_table_name = 'config_images_locations'
                        AND delete_rule = 'SET NULL'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_foreign_key', 'fk_config_images_locations_history_changed_by_user_id', 'ON DELETE SET NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.referential_constraints
                    WHERE
                        constraint_schema = DATABASE()
                        AND table_name = 'config_images_locations_history'
                        AND constraint_name = 'fk_config_images_locations_history_changed_by_user_id'
                        AND referenced_table_name = 'core_users_profiles'
                        AND delete_rule = 'SET NULL'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_routine', 'sp_config_images_locations_history_get', 'procedure exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_config_images_locations_history_get'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_routine', 'sp_config_images_locations_scope_paths_get', 'procedure exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_config_images_locations_scope_paths_get'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_routine', 'sp_config_images_locations_paths_move', 'procedure exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_config_images_locations_paths_move'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_routine', 'sp_config_images_locations_insert', 'procedure exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_config_images_locations_insert'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_routine', 'sp_config_images_locations_update', 'procedure exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_config_images_locations_update'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'stale_routine_body', 'sp_config_images_locations_history_get', 'body reads the change record newest first', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_config_images_locations_history_get'
                        AND routine_definition LIKE '%config_images_locations_history%'
                        AND routine_definition LIKE '%changed_utc%'
                        AND routine_definition LIKE '%ORDER BY%DESC%'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'stale_routine_body', 'sp_config_images_locations_scope_paths_get', 'body returns the active paths of one scope', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_config_images_locations_scope_paths_get'
                        AND routine_definition LIKE '%image_path%'
                        AND routine_definition LIKE '%is_active%'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'stale_routine_body', 'sp_config_images_locations_paths_move', 'body reports affected rows and honours the move flag', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_config_images_locations_paths_move'
                        AND routine_definition LIKE '%image_path%'
                        AND routine_definition LIKE '%mtm_picture_layout_move%'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'stale_routine_body', 'sp_config_images_locations_insert', 'body carries the first-picture trigger', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_config_images_locations_insert'
                        AND routine_definition LIKE '%trg_config_images_locations_history_on_insert%'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'stale_routine_body', 'sp_config_images_locations_update', 'body carries the replacement trigger, silent during the move', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_config_images_locations_update'
                        AND routine_definition LIKE '%trg_config_images_locations_history_on_update%'
                        AND routine_definition LIKE '%mtm_picture_layout_move%'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_trigger', 'trg_config_images_locations_history_on_insert', 'trigger exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.triggers
                    WHERE
                        trigger_schema = DATABASE()
                        AND trigger_name = 'trg_config_images_locations_history_on_insert'
                        AND event_manipulation = 'INSERT'
                        AND event_object_table = 'config_images_locations'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_trigger', 'trg_config_images_locations_history_on_update', 'trigger exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.triggers
                    WHERE
                        trigger_schema = DATABASE()
                        AND trigger_name = 'trg_config_images_locations_history_on_update'
                        AND event_manipulation = 'UPDATE'
                        AND event_object_table = 'config_images_locations'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'stale_trigger_body', 'trg_config_images_locations_history_on_update', 'body writes nothing during the recorded-path move', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.triggers
                    WHERE
                        trigger_schema = DATABASE()
                        AND trigger_name = 'trg_config_images_locations_history_on_update'
                        AND action_statement LIKE '%config_images_locations_history%'
                        AND action_statement LIKE '%mtm_picture_layout_move%'
                ), 'present', 'missing'
            )
    ) validation_results
WHERE
    actual_value = 'missing';
