-- Rollback for seed: seed_role_admin_to_it_department
-- Engine: MySQL 5.7
-- Feature: 006-user-management-and-permissions (task T032)
-- Purpose: put the administrator role back with the accounts that held it, and remove IT Department.
--
-- The rung the restored row carries
--   The retired row is re-inserted with role_rank = 0, which is the rung it held when the migration removed it:
--   seed_dev_masked_baseline deliberately writes that row without a rank, so it stood on the column default of 0
--   (T027, asserted by the seed's own suite in T031). This is a restore of the row that was replaced, not an
--   invented rung for a role that never had one.
--
-- The window in which this reversal is exact
--   It is exact only while no account has been given IT Department since the migration ran. There is no record of
--   which accounts the migration moved, so this script moves every account standing on IT Department. Inside the
--   window no other writer has put one there, so the sets are the same set. Once an account has been assigned IT
--   Department in the application, this reversal would move that account too.
--
-- What cannot be restored
--   The retired row's `public_id`. The row is gone, so the insert below gives it a fresh one. No reader keys on
--   that value; every reader keys on `role_code`.
--
-- Running it twice
--   A second run finds no IT Department row to remove and leaves the store alone.

USE mtm_waitlist;

SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS sp_seed_role_it_department_to_admin_revert;

DELIMITER $$

CREATE PROCEDURE sp_seed_role_it_department_to_admin_revert()
BEGIN
    DECLARE v_incoming_role_id BIGINT DEFAULT NULL;
    DECLARE v_retired_role_id BIGINT DEFAULT NULL;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;

        RESIGNAL;
    END;

    START TRANSACTION;

    SET v_incoming_role_id = (
        SELECT r.id
        FROM auth_roles_catalog r
        WHERE r.role_code = 'it_department'
        LIMIT 1
    );

    IF v_incoming_role_id IS NOT NULL THEN
        SET v_retired_role_id = (
            SELECT r.id
            FROM auth_roles_catalog r
            WHERE r.role_code = 'admin'
            LIMIT 1
        );

        IF v_retired_role_id IS NULL THEN
            INSERT INTO auth_roles_catalog (
                public_id,
                role_code,
                role_name,
                role_rank,
                created_utc,
                updated_utc
            )
            VALUES (
                UUID(),
                'admin',
                'Admin',
                0,
                UTC_TIMESTAMP(),
                UTC_TIMESTAMP()
            );

            SET v_retired_role_id = LAST_INSERT_ID();
        END IF;

        UPDATE auth_roles_assignments
        SET role_id = v_retired_role_id
        WHERE role_id = v_incoming_role_id;

        DELETE FROM auth_roles_catalog
        WHERE id = v_incoming_role_id;
    END IF;

    COMMIT;
END$$

DELIMITER ;

CALL sp_seed_role_it_department_to_admin_revert();

DROP PROCEDURE IF EXISTS sp_seed_role_it_department_to_admin_revert;

SELECT
    'admin present at rung 0, it_department absent' AS expected_after_reversal,
    (SELECT COUNT(*) FROM auth_roles_catalog WHERE role_code = 'it_department') AS renamed_rows,
    (SELECT COUNT(*) FROM auth_roles_catalog WHERE role_code = 'admin') AS retired_rows,
    (
        SELECT r.role_rank
        FROM auth_roles_catalog r
        WHERE r.role_code = 'admin'
        LIMIT 1
    ) AS retired_rung,
    (
        SELECT COUNT(*)
        FROM auth_roles_assignments ra
        INNER JOIN auth_roles_catalog r ON r.id = ra.role_id
        WHERE r.role_code = 'admin'
    ) AS accounts_on_the_restored_role,
    (
        SELECT COUNT(*)
        FROM core_users_profiles u
        WHERE u.username_normalized IS NOT NULL
          AND NOT EXISTS (
                SELECT 1
                FROM auth_roles_assignments ra
                WHERE ra.user_id = u.id
          )
    ) AS accounts_with_no_role;
