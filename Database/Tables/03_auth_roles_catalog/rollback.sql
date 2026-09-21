-- Rollback for table: auth_roles_catalog
-- Reverses only the role ladder: the role_rank column and the index over it. It drops those and nothing
-- else, because the catalogue's rows and its other columns predate the feature.
--
-- Each drop is guarded on information_schema so the script is safe to re-run and safe on a store where the
-- ladder was never added.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET
    @has_role_rank_index := (
        SELECT COUNT(*)
        FROM information_schema.statistics
        WHERE
            table_schema = DATABASE()
            AND table_name = 'auth_roles_catalog'
            AND index_name = 'idx_auth_roles_catalog_role_rank'
    );

SET
    @sql_stmt := IF(
        @has_role_rank_index > 0,
        'ALTER TABLE auth_roles_catalog DROP INDEX idx_auth_roles_catalog_role_rank',
        'SELECT ''idx_auth_roles_catalog_role_rank not present'''
    );

PREPARE stmt FROM @sql_stmt;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;

SET
    @has_role_rank_column := (
        SELECT COUNT(*)
        FROM information_schema.columns
        WHERE
            table_schema = DATABASE()
            AND table_name = 'auth_roles_catalog'
            AND column_name = 'role_rank'
    );

SET
    @sql_stmt := IF(
        @has_role_rank_column > 0,
        'ALTER TABLE auth_roles_catalog DROP COLUMN role_rank',
        'SELECT ''auth_roles_catalog.role_rank not present'''
    );

PREPARE stmt FROM @sql_stmt;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;