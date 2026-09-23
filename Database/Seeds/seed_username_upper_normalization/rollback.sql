-- Rollback: seed_username_upper_normalization
-- Engine: MySQL 5.7
-- Reversal of 006-user-management-and-permissions (task T015)
--
-- Restores the sign-in names this migration changed, from the values it recorded in
-- `seed_username_case_backup`, then drops that table — it exists only for this migration's lifetime.
--
-- Rows whose names were already upper case are not in the backup and are not touched here. If the backup table
-- is absent, there is nothing recorded to restore and this script is a no-op; it says so rather than failing.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET
    @has_case_backup := (
        SELECT COUNT(*)
        FROM information_schema.tables
        WHERE
            table_schema = DATABASE()
            AND table_name = 'seed_username_case_backup'
    );

SET
    @sql_stmt := IF(
        @has_case_backup > 0,
        'UPDATE core_users_profiles u INNER JOIN seed_username_case_backup b ON b.user_id = u.id SET u.username_normalized = b.previous_username_normalized, u.updated_utc = UTC_TIMESTAMP()',
        'SELECT ''seed_username_case_backup absent: nothing recorded to restore'''
    );

PREPARE stmt FROM @sql_stmt;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;

SET
    @sql_stmt := IF(
        @has_case_backup > 0,
        'DROP TABLE IF EXISTS seed_username_case_backup',
        'SELECT ''seed_username_case_backup absent: nothing to drop'''
    );

PREPARE stmt FROM @sql_stmt;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;
