-- Stored Procedure: sp_user_management_reset_password
-- Engine: MySQL 5.7
-- Feature: 006-user-management-and-permissions (task T013)
--
-- Purpose: issue a fresh one-time PIN for one account, as one act.
--
-- Contract:
--   IN  p_user_id           BIGINT        the account being reset
--   IN  p_actor_user_id     BIGINT        the person performing the reset
--   IN  p_password_hash     VARCHAR(128)  the salted hash of the freshly generated PIN
--   IN  p_password_salt     VARBINARY(32)
--   IN  p_change_group_id   CHAR(36)      the audit grouping id; generated if absent
--
--   OUT the affected-row count, through the non-query seam
--
-- What it does, in one transaction:
--   * refuses a target that outranks the actor (mtm_target_outranks_actor, FR-019, FR-025);
--   * stores the hash and salt of the freshly generated PIN — the salted hash only, never the PIN itself,
--     which is what makes "readable back" mean plaintext in FR-030;
--   * sets `require_password_change = 1`, so the PIN must be replaced at the next sign-in (FR-029);
--   * clears the wrong-attempt count, because a fresh reset is one of only two things that clears it (FR-039);
--   * leaves `is_active` exactly as it was — a reset on a switched-off account does not switch it back on
--     (FR-029);
--   * leaves every existing session for that person alone, because a reset ends none (FR-035);
--   * writes ONE audit row recording the reset with **both value columns null**, because the credential is
--     never stored and there is no before or after value to record (FR-111).
--
-- A second reset simply replaces the earlier PIN, and the screen says so (FR-032): nothing here refuses a
-- repeated reset, and no history row is written for the PIN itself, so the earlier PIN is unrecoverable.
--
-- The actor's rung is read here and never taken from its caller (FR-025).
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_user_management_reset_password;

-- DELIMITER because this body is compound; see the note in
-- sp_auth_temporary_credential_attempt_record/create.sql.
DELIMITER $$

CREATE PROCEDURE sp_user_management_reset_password(
    IN p_user_id BIGINT,
    IN p_actor_user_id BIGINT,
    IN p_password_hash VARCHAR(128),
    IN p_password_salt VARBINARY(32),
    IN p_change_group_id CHAR(36)
)
BEGIN
    DECLARE v_actor_rank INT DEFAULT 0;
    DECLARE v_target_rank INT DEFAULT 0;
    DECLARE v_target_exists INT DEFAULT 0;
    DECLARE v_change_group_id CHAR(36);
    DECLARE v_actor_display_name VARCHAR(256) DEFAULT '';
    DECLARE v_actor_employee_identifier VARCHAR(128) DEFAULT '';
    DECLARE v_actor_role_code VARCHAR(64) DEFAULT '';

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SET v_change_group_id = COALESCE(NULLIF(TRIM(p_change_group_id), ''), UUID());

    START TRANSACTION;

    SELECT COUNT(*) INTO v_target_exists
    FROM core_users_profiles u
    WHERE u.id = p_user_id;

    SELECT COALESCE(MAX(r.role_rank), 0)
    INTO v_actor_rank
    FROM auth_roles_assignments ra
    INNER JOIN auth_roles_catalog r ON r.id = ra.role_id
    WHERE ra.user_id = p_actor_user_id;

    SELECT COALESCE(MAX(r.role_rank), 0)
    INTO v_target_rank
    FROM auth_roles_assignments ra
    INNER JOIN auth_roles_catalog r ON r.id = ra.role_id
    WHERE ra.user_id = p_user_id;

    IF v_target_exists > 0 AND v_target_rank > v_actor_rank THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_target_outranks_actor';
    END IF;

    SELECT COALESCE(u.display_name, ''), COALESCE(u.employee_identifier, '')
    INTO v_actor_display_name, v_actor_employee_identifier
    FROM core_users_profiles u
    WHERE u.id = p_actor_user_id;

    SELECT COALESCE(r.role_code, '')
    INTO v_actor_role_code
    FROM auth_roles_assignments ra
    INNER JOIN auth_roles_catalog r ON r.id = ra.role_id
    WHERE ra.user_id = p_actor_user_id
    ORDER BY r.role_rank DESC, r.role_code ASC
    LIMIT 1;

    UPDATE core_users_profiles
    SET password_hash = p_password_hash,
        password_salt = p_password_salt,
        require_password_change = 1,
        temporary_credential_failed_attempts = 0,
        updated_utc = UTC_TIMESTAMP()
    WHERE id = p_user_id;

    INSERT INTO auth_user_management_audit (
        public_id,
        change_group_id,
        target_user_id,
        field_name,
        previous_value,
        changed_value,
        actor_user_id,
        actor_display_name,
        actor_employee_identifier,
        actor_role_code,
        occurred_utc
    )
    VALUES (
        UUID(),
        v_change_group_id,
        p_user_id,
        'password_hash',
        NULL,
        NULL,
        p_actor_user_id,
        v_actor_display_name,
        v_actor_employee_identifier,
        v_actor_role_code,
        UTC_TIMESTAMP()
    );

    COMMIT;
END$$

DELIMITER ;
