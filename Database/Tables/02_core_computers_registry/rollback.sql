-- Rollback for table: core_computers_registry
-- Engine: MySQL 5.7
-- Idempotency: the rename is guarded on information_schema, so the script is safe to re-run and safe on a store
--              where it was never applied.
--
-- 010-startup-rebuild (task T238) changed this table rather than creating it: the free-text note it held became
-- the location it stands in, so the reversal has two parts and they are deliberately both present, in the same
-- shape Database/Tables/10_ops_startup_logs/rollback.sql uses:
--
--   1. The change is reversed precisely: `location` is renamed back to `description`. This is what a store
--      promoted before the feature needs. The rename is what reverses it rather than a drop, because the column
--      is the same column: dropping and re-adding it would throw away the locations a store had already
--      recorded. The name is the only thing the change touched.
--   2. The table drop that this file always ended with is kept unchanged. It is the reversal of the artifact as
--      a whole, and removing it because the change now has its own reversal would silently make this file no
--      longer the rollback of the create it sits beside.

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;

SET
    @has_location_column := (
        SELECT COUNT(*)
        FROM information_schema.columns
        WHERE
            table_schema = DATABASE()
            AND table_name = 'core_computers_registry'
            AND column_name = 'location'
    );

SET
    @sql_stmt := IF(
        @has_location_column = 1,
        'ALTER TABLE core_computers_registry CHANGE COLUMN location description VARCHAR(255) NULL COMMENT ''Optional free-text description for the computer.''',
        'SELECT ''core_computers_registry.location not present'''
    );

PREPARE stmt FROM @sql_stmt;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;

DROP TABLE IF EXISTS core_computers_registry;

SET FOREIGN_KEY_CHECKS = 1;