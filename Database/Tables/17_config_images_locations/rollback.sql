-- Rollback script: config_images_locations
-- Purpose: Drop the config_images_locations table and restore prior state
-- Idempotency: Uses DROP TABLE IF EXISTS for safe re-execution, and every extension drop below is guarded on
--              information_schema so the script is safe on a store where the machine scope was never applied.
--
-- Feature 008-part-pictures (task T004): the only change this change made to the table was the `scope` column's
-- comment. A comment is not recoverable on its own and is not worth a migration, and the table drop below is
-- already the complete reversal of the table, so this file is deliberately left as the drop it always was.
--
-- Feature 010-startup-rebuild (task T051) adds the extension drops ahead of that table drop. The extension is
-- `computer_id` and the index and foreign key over it. `scope_item_id` is not reverted and does not need to be:
-- it was already a free-form identifier per scope, and the machine scope only says what its value looks like.
-- `uq_config_images_locations_scope_item` is untouched by the extension and by this reversal, which is the point
-- of the composite item id — the key that guards one picture per item never had to change.
--
-- Dropping `computer_id` on its own would take the index and the foreign key with it, because a MySQL index
-- cannot outlive a column it names. The three guarded drops below do it the other way round, and the order is
-- not cosmetic: MySQL refuses to drop an index while a foreign key still needs it (error 1553), so the key goes
-- first. Doing all three explicitly also makes either half re-runnable and keeps what happened readable.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

SET
    @has_computer_key := (
        SELECT COUNT(*)
        FROM information_schema.table_constraints
        WHERE
            constraint_schema = DATABASE()
            AND table_name = 'config_images_locations'
            AND constraint_name = 'fk_config_images_locations_core_computers_registry_computer_id'
            AND constraint_type = 'FOREIGN KEY'
    );

SET
    @sql_stmt := IF(
        @has_computer_key > 0,
        'ALTER TABLE config_images_locations DROP FOREIGN KEY fk_config_images_locations_core_computers_registry_computer_id',
        'SELECT ''fk_config_images_locations_core_computers_registry_computer_id not present'''
    );

PREPARE stmt FROM @sql_stmt;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;

SET
    @has_computer_index := (
        SELECT COUNT(*)
        FROM information_schema.statistics
        WHERE
            table_schema = DATABASE()
            AND table_name = 'config_images_locations'
            AND index_name = 'idx_config_images_locations_computer_id'
    );

SET
    @sql_stmt := IF(
        @has_computer_index > 0,
        'ALTER TABLE config_images_locations DROP INDEX idx_config_images_locations_computer_id',
        'SELECT ''idx_config_images_locations_computer_id not present'''
    );

PREPARE stmt FROM @sql_stmt;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;

SET
    @has_computer_column := (
        SELECT COUNT(*)
        FROM information_schema.columns
        WHERE
            table_schema = DATABASE()
            AND table_name = 'config_images_locations'
            AND column_name = 'computer_id'
    );

SET
    @sql_stmt := IF(
        @has_computer_column > 0,
        'ALTER TABLE config_images_locations DROP COLUMN computer_id',
        'SELECT ''config_images_locations.computer_id not present'''
    );

PREPARE stmt FROM @sql_stmt;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;

DROP TABLE IF EXISTS config_images_locations;

SET FOREIGN_KEY_CHECKS = 1;