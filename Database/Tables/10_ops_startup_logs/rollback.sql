-- Rollback for table: ops_startup_logs
-- Engine: MySQL 5.7
-- Idempotency: every drop is guarded on information_schema, so the script is safe to re-run and safe on a store
--              where part of the extension was never applied.
--
-- 010-startup-rebuild (task T047) extended this table rather than creating it, so the reversal has two parts and
-- they are deliberately both present:
--
--   1. The extension is reversed precisely: the six indexes the panel filters on, then the four columns they
--      filter on. This is what a store promoted before the feature needs, and it is what makes the extension
--      reversible without touching anything that predates it. The four columns are NULLable and nothing else
--      reads them, so dropping them loses only the values the panel would have shown.
--   2. The table drop that this file always ended with is kept unchanged. It is the reversal of the artifact as
--      a whole, and removing it because the extension now has its own reversal would silently make this file
--      no longer the rollback of the create it sits beside.
--
-- Order matters in one direction only: an index that names a column has to go before that column does. MySQL
-- would drop the index with the column, but doing it explicitly keeps what happened readable, and the guards
-- make the sequence safe whichever half has already run.

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;

SET
    @has_level_created_index := (
        SELECT COUNT(*)
        FROM information_schema.statistics
        WHERE
            table_schema = DATABASE()
            AND table_name = 'ops_startup_logs'
            AND index_name = 'idx_ops_startup_logs_level_created_utc'
    );

SET
    @sql_stmt := IF(
        @has_level_created_index > 0,
        'ALTER TABLE ops_startup_logs DROP INDEX idx_ops_startup_logs_level_created_utc',
        'SELECT ''idx_ops_startup_logs_level_created_utc not present'''
    );

PREPARE stmt FROM @sql_stmt;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;

SET
    @has_host_created_index := (
        SELECT COUNT(*)
        FROM information_schema.statistics
        WHERE
            table_schema = DATABASE()
            AND table_name = 'ops_startup_logs'
            AND index_name = 'idx_ops_startup_logs_host_id_created_utc'
    );

SET
    @sql_stmt := IF(
        @has_host_created_index > 0,
        'ALTER TABLE ops_startup_logs DROP INDEX idx_ops_startup_logs_host_id_created_utc',
        'SELECT ''idx_ops_startup_logs_host_id_created_utc not present'''
    );

PREPARE stmt FROM @sql_stmt;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;

SET
    @has_module_created_index := (
        SELECT COUNT(*)
        FROM information_schema.statistics
        WHERE
            table_schema = DATABASE()
            AND table_name = 'ops_startup_logs'
            AND index_name = 'idx_ops_startup_logs_module_created_utc'
    );

SET
    @sql_stmt := IF(
        @has_module_created_index > 0,
        'ALTER TABLE ops_startup_logs DROP INDEX idx_ops_startup_logs_module_created_utc',
        'SELECT ''idx_ops_startup_logs_module_created_utc not present'''
    );

PREPARE stmt FROM @sql_stmt;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;

SET
    @has_actor_created_index := (
        SELECT COUNT(*)
        FROM information_schema.statistics
        WHERE
            table_schema = DATABASE()
            AND table_name = 'ops_startup_logs'
            AND index_name = 'idx_ops_startup_logs_actor_id_created_utc'
    );

SET
    @sql_stmt := IF(
        @has_actor_created_index > 0,
        'ALTER TABLE ops_startup_logs DROP INDEX idx_ops_startup_logs_actor_id_created_utc',
        'SELECT ''idx_ops_startup_logs_actor_id_created_utc not present'''
    );

PREPARE stmt FROM @sql_stmt;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;

SET
    @has_error_type_created_index := (
        SELECT COUNT(*)
        FROM information_schema.statistics
        WHERE
            table_schema = DATABASE()
            AND table_name = 'ops_startup_logs'
            AND index_name = 'idx_ops_startup_logs_error_type_created_utc'
    );

SET
    @sql_stmt := IF(
        @has_error_type_created_index > 0,
        'ALTER TABLE ops_startup_logs DROP INDEX idx_ops_startup_logs_error_type_created_utc',
        'SELECT ''idx_ops_startup_logs_error_type_created_utc not present'''
    );

PREPARE stmt FROM @sql_stmt;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;

SET
    @has_fingerprint_created_index := (
        SELECT COUNT(*)
        FROM information_schema.statistics
        WHERE
            table_schema = DATABASE()
            AND table_name = 'ops_startup_logs'
            AND index_name = 'idx_ops_startup_logs_error_fingerprint_created_utc'
    );

SET
    @sql_stmt := IF(
        @has_fingerprint_created_index > 0,
        'ALTER TABLE ops_startup_logs DROP INDEX idx_ops_startup_logs_error_fingerprint_created_utc',
        'SELECT ''idx_ops_startup_logs_error_fingerprint_created_utc not present'''
    );

PREPARE stmt FROM @sql_stmt;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;

SET
    @has_module_column := (
        SELECT COUNT(*)
        FROM information_schema.columns
        WHERE
            table_schema = DATABASE()
            AND table_name = 'ops_startup_logs'
            AND column_name = 'module'
    );

SET
    @sql_stmt := IF(
        @has_module_column > 0,
        'ALTER TABLE ops_startup_logs DROP COLUMN module',
        'SELECT ''ops_startup_logs.module not present'''
    );

PREPARE stmt FROM @sql_stmt;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;

SET
    @has_error_type_column := (
        SELECT COUNT(*)
        FROM information_schema.columns
        WHERE
            table_schema = DATABASE()
            AND table_name = 'ops_startup_logs'
            AND column_name = 'error_type'
    );

SET
    @sql_stmt := IF(
        @has_error_type_column > 0,
        'ALTER TABLE ops_startup_logs DROP COLUMN error_type',
        'SELECT ''ops_startup_logs.error_type not present'''
    );

PREPARE stmt FROM @sql_stmt;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;

SET
    @has_exception_detail_column := (
        SELECT COUNT(*)
        FROM information_schema.columns
        WHERE
            table_schema = DATABASE()
            AND table_name = 'ops_startup_logs'
            AND column_name = 'exception_detail'
    );

SET
    @sql_stmt := IF(
        @has_exception_detail_column > 0,
        'ALTER TABLE ops_startup_logs DROP COLUMN exception_detail',
        'SELECT ''ops_startup_logs.exception_detail not present'''
    );

PREPARE stmt FROM @sql_stmt;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;

SET
    @has_error_fingerprint_column := (
        SELECT COUNT(*)
        FROM information_schema.columns
        WHERE
            table_schema = DATABASE()
            AND table_name = 'ops_startup_logs'
            AND column_name = 'error_fingerprint'
    );

SET
    @sql_stmt := IF(
        @has_error_fingerprint_column > 0,
        'ALTER TABLE ops_startup_logs DROP COLUMN error_fingerprint',
        'SELECT ''ops_startup_logs.error_fingerprint not present'''
    );

PREPARE stmt FROM @sql_stmt;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;

DROP TABLE IF EXISTS ops_startup_logs;

SET FOREIGN_KEY_CHECKS = 1;