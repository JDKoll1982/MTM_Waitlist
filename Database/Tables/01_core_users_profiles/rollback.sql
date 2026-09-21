-- Rollback for table: core_users_profiles
-- Reverses only the three columns added for user management: first_name, last_name and
-- temporary_credential_failed_attempts. It drops those three and nothing else, because every other column
-- in this table predates the feature and belongs to the sign-in path.
--
-- Each drop is guarded on information_schema so the script is safe to re-run and safe on a store where the
-- columns were never added.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET
    @has_first_name_column := (
        SELECT COUNT(*)
        FROM information_schema.columns
        WHERE
            table_schema = DATABASE()
            AND table_name = 'core_users_profiles'
            AND column_name = 'first_name'
    );

SET
    @sql_stmt := IF(
        @has_first_name_column > 0,
        'ALTER TABLE core_users_profiles DROP COLUMN first_name',
        'SELECT ''core_users_profiles.first_name not present'''
    );

PREPARE stmt FROM @sql_stmt;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;

SET
    @has_last_name_column := (
        SELECT COUNT(*)
        FROM information_schema.columns
        WHERE
            table_schema = DATABASE()
            AND table_name = 'core_users_profiles'
            AND column_name = 'last_name'
    );

SET
    @sql_stmt := IF(
        @has_last_name_column > 0,
        'ALTER TABLE core_users_profiles DROP COLUMN last_name',
        'SELECT ''core_users_profiles.last_name not present'''
    );

PREPARE stmt FROM @sql_stmt;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;

SET
    @has_attempts_column := (
        SELECT COUNT(*)
        FROM information_schema.columns
        WHERE
            table_schema = DATABASE()
            AND table_name = 'core_users_profiles'
            AND column_name = 'temporary_credential_failed_attempts'
    );

SET
    @sql_stmt := IF(
        @has_attempts_column > 0,
        'ALTER TABLE core_users_profiles DROP COLUMN temporary_credential_failed_attempts',
        'SELECT ''core_users_profiles.temporary_credential_failed_attempts not present'''
    );

PREPARE stmt FROM @sql_stmt;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;
