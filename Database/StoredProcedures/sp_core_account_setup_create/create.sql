-- Stored Procedure: sp_core_account_setup_create
-- Engine: MySQL 5.7
-- Feature: 011-identifier-first-signin (task T011)
--
-- Purpose: create one person's profile, their credential and their rank assignment in one act, for the
--          self set-up path. The person chooses their own PIN, so the account is written with
--          `require_password_change = 0`: it is never on a temporary credential, never sees the
--          five-attempt limit and never passes through the new-PIN step (FR-009, FR-045).
--
-- Contract:
--   IN  p_username              VARCHAR(128)  the sign-in name; stored upper case
--   IN  p_first_name            VARCHAR(128)
--   IN  p_last_name             VARCHAR(128)
--   IN  p_employee_identifier   VARCHAR(128)  the employee number the plant system confirmed
--   IN  p_password_hash         VARCHAR(128)  the salted hash of the PIN the person chose
--   IN  p_password_salt         VARBINARY(32)
--
--   OUT exactly one row describing the account that was created:
--       user_id, username_normalized, display_name, employee_identifier, role_code
--
-- The rank is read HERE, inside the procedure, as the lowest rung in `auth_roles_catalog`
-- (`ORDER BY role_rank ASC, role_code ASC LIMIT 1`). No caller may pass a rank in: a caller that could
-- name its own rank could grant itself anything, and "roles come from the store only" is an invariant
-- (FR-010, FR-039).
--
-- The caller hashes the PIN. The PIN itself never reaches the store and never appears in a readable row.
--
-- A duplicate sign-in name is deliberately NOT translated: MySQL's own duplicate-key error (1062) passes
-- through, so the one source of "already taken" stays MySQL's answer.
--
-- No audit row is written. The audit trail exists for changes an administrator makes to somebody else;
-- this act has no actor, and attributing it to the person it creates would invent one. That is a
-- deliberate departure from sp_user_management_create, recorded in analysis.md A2.
--
-- One transaction: a failure anywhere leaves no profile, no credential and no rank.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_account_setup_create;

DELIMITER $$

CREATE PROCEDURE sp_core_account_setup_create(
    IN p_username VARCHAR(128),
    IN p_first_name VARCHAR(128),
    IN p_last_name VARCHAR(128),
    IN p_employee_identifier VARCHAR(128),
    IN p_password_hash VARCHAR(128),
    IN p_password_salt VARBINARY(32)
)
BEGIN
    DECLARE v_username VARCHAR(128);
    DECLARE v_first_name VARCHAR(128);
    DECLARE v_last_name VARCHAR(128);
    DECLARE v_employee_identifier VARCHAR(128);
    DECLARE v_display_name VARCHAR(256);
    DECLARE v_role_id BIGINT DEFAULT NULL;
    DECLARE v_role_code VARCHAR(64) DEFAULT '';
    DECLARE v_new_user_id BIGINT DEFAULT NULL;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SET v_username = UPPER(TRIM(p_username));
    SET v_first_name = TRIM(p_first_name);
    SET v_last_name = TRIM(p_last_name);
    SET v_employee_identifier = NULLIF(TRIM(p_employee_identifier), '');
    SET v_display_name = CONCAT(v_first_name, ' ', v_last_name);

    IF v_username = '' OR v_first_name = '' OR v_last_name = '' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_setup_incomplete';
    END IF;

    START TRANSACTION;

    -- The lowest rung, chosen by the store's own ordering. There is no default rank name in this file.
    SELECT r.id, r.role_code
    INTO v_role_id, v_role_code
    FROM auth_roles_catalog r
    ORDER BY r.role_rank ASC, r.role_code ASC
    LIMIT 1;

    IF v_role_id IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_role_catalog_empty';
    END IF;

    INSERT INTO core_users_profiles (
        public_id,
        username_normalized,
        first_name,
        last_name,
        password_hash,
        password_salt,
        require_password_change,
        temporary_credential_failed_attempts,
        display_name,
        employee_identifier,
        is_active,
        created_utc,
        updated_utc
    )
    VALUES (
        UUID(),
        v_username,
        v_first_name,
        v_last_name,
        p_password_hash,
        p_password_salt,
        0,
        0,
        v_display_name,
        v_employee_identifier,
        1,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    );

    SET v_new_user_id = LAST_INSERT_ID();

    INSERT INTO auth_roles_assignments (
        public_id,
        user_id,
        role_id,
        assigned_utc,
        assigned_by_user_id
    )
    VALUES (
        UUID(),
        v_new_user_id,
        v_role_id,
        UTC_TIMESTAMP(),
        NULL
    );

    COMMIT;

    SELECT
        v_new_user_id AS user_id,
        v_username AS username_normalized,
        v_display_name AS display_name,
        COALESCE(v_employee_identifier, '') AS employee_identifier,
        v_role_code AS role_code;
END $$

DELIMITER ;
