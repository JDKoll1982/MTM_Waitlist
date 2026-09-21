-- Seed: seed_role_admin_to_it_department
-- Engine: MySQL 5.7
-- Feature: 006-user-management-and-permissions (task T032)
-- Purpose: the administrator role is removed and IT Department takes its place, with every account moved across
--          (FR-011, FR-012, FR-013, FR-014).
--
-- Why this is a stored program and not a plain script
--   The change is three statements that must all land or none: insert the incoming role, repoint every
--   assignment, remove the outgoing row. MySQL 5.7 has no DECLARE ... HANDLER outside a stored program, so the
--   work lives in a procedure this script creates, calls and drops. The handler rolls back and resignals, so the
--   interrupted state FR-013 forbids, an account holding no role, cannot be left behind. The procedure is
--   transient on purpose: it is not a schema artifact, so it is dropped before this script ends and it appears
--   in no aggregate and no folder of its own.
--
-- The rung is written explicitly
--   `it_department` is inserted with role_rank = 90 and not left to the column default. The column is
--   NOT NULL DEFAULT 0 (T004), so an insert that omitted it would put the renamed top role BELOW the worker
--   roles at 10, and the rank rule would then refuse it the accounts it exists to own.
--
-- The order of the three statements
--   Insert, repoint, remove. `auth_roles_assignments.role_id` carries a RESTRICT foreign key back to the
--   catalogue, so the retired row cannot be removed while an assignment still names it.
--
-- The reversal, and the window in which it is exact
--   rollback.sql restores the row this migration removed and moves the accounts back. That reversal is exact
--   only while no account has been given IT Department since this migration ran. Without a record of which
--   accounts this script moved, the reversal moves every account standing on IT Department; inside that window
--   no other writer can have put one there, so the two sets are the same set. The retired row's `public_id`
--   cannot be restored either, because the row is gone: the reversal gives it a fresh one, which no reader
--   keys on. Both headers state this, because the window is the whole reason the reversal is exact.
--
-- Running it twice
--   A second run finds no retired row to remove and leaves the store alone. It reports that rather than failing,
--   so re-running the file is safe.

USE mtm_waitlist;

SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS sp_seed_role_admin_to_it_department;

DELIMITER $$

CREATE PROCEDURE sp_seed_role_admin_to_it_department()
BEGIN
    DECLARE v_retired_role_id BIGINT DEFAULT NULL;
    DECLARE v_incoming_role_id BIGINT DEFAULT NULL;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;

        RESIGNAL;
    END;

    START TRANSACTION;

    SET v_retired_role_id = (
        SELECT r.id
        FROM auth_roles_catalog r
        WHERE r.role_code = 'admin'
        LIMIT 1
    );

    IF v_retired_role_id IS NOT NULL THEN
        SET v_incoming_role_id = (
            SELECT r.id
            FROM auth_roles_catalog r
            WHERE r.role_code = 'it_department'
            LIMIT 1
        );

        IF v_incoming_role_id IS NULL THEN
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
                'it_department',
                'IT Department',
                90,
                UTC_TIMESTAMP(),
                UTC_TIMESTAMP()
            );

            SET v_incoming_role_id = LAST_INSERT_ID();
        ELSE
            UPDATE auth_roles_catalog
            SET role_name = 'IT Department',
                role_rank = 90,
                updated_utc = UTC_TIMESTAMP()
            WHERE id = v_incoming_role_id;
        END IF;

        UPDATE auth_roles_assignments
        SET role_id = v_incoming_role_id
        WHERE role_id = v_retired_role_id;

        DELETE FROM auth_roles_catalog
        WHERE id = v_retired_role_id;
    END IF;

    COMMIT;
END$$

DELIMITER ;

CALL sp_seed_role_admin_to_it_department();

DROP PROCEDURE IF EXISTS sp_seed_role_admin_to_it_department;

-- Reported, so a run that changed nothing is visible rather than inferred.
SELECT
    'admin absent, it_department present' AS expected_after_apply,
    (SELECT COUNT(*) FROM auth_roles_catalog WHERE role_code = 'admin') AS retired_rows,
    (SELECT COUNT(*) FROM auth_roles_catalog WHERE role_code = 'it_department') AS renamed_rows,
    (
        SELECT r.role_rank
        FROM auth_roles_catalog r
        WHERE r.role_code = 'it_department'
        LIMIT 1
    ) AS renamed_rung,
    (
        SELECT COUNT(*)
        FROM auth_roles_assignments ra
        INNER JOIN auth_roles_catalog r ON r.id = ra.role_id
        WHERE r.role_code = 'it_department'
    ) AS accounts_on_the_renamed_role,
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
