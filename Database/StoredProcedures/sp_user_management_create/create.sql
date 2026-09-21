-- Stored Procedure: sp_user_management_create
-- Engine: MySQL 5.7
-- Feature: 006-user-management-and-permissions (task T011)
--
-- Purpose: create one person, as one act. The profile and the role assignment are written in the same
--          transaction, so a person cannot come into existence with no role (FR-006) and cannot come into
--          existence with two (FR-009).
--
-- Contract:
--   IN  p_username              VARCHAR(128)  the sign-in name as typed; stored upper case
--   IN  p_first_name            VARCHAR(128)
--   IN  p_last_name             VARCHAR(128)
--   IN  p_employee_identifier   VARCHAR(128)  exactly four digits, deliberately not unique (FR-004)
--   IN  p_role_code             VARCHAR(64)   the role to assign
--   IN  p_actor_user_id         BIGINT        the person performing the create
--   IN  p_password_hash         VARCHAR(128)  the salted hash of the freshly generated PIN
--   IN  p_password_salt         VARBINARY(32)
--   IN  p_change_group_id       CHAR(36)      shared by every audit row this act writes; generated if absent
--
--   OUT the affected-row count, through the non-query seam. This is a write and returns no result set, so a
--       caller that needs the new account's id reads it back through sp_user_management_list.
--
-- The actor's own rung is read here, from auth_roles_assignments joined to auth_roles_catalog. It is never
-- taken from the caller: a caller that could assert its own rank could grant itself anything (FR-025).
--
-- Refusals, reported as a stable token so the caller can answer in the reader's words:
--   mtm_role_unknown  the requested role is not in the catalogue
--   mtm_rank_denied   the requested role's rung is above the actor's own
--
-- A duplicate sign-in name is deliberately NOT translated. MySQL's own duplicate-key error passes through the
-- resignal, and the caller reads `1062` from the provider and returns the typed "already taken" result
-- (FR-008). That keeps the 1062 answer as one behaviour with one source, and it is why there is no blind retry
-- anywhere in this path.
--
-- The audit rows: one per written field, sharing `p_change_group_id`, so one act reads as one act (FR-108,
-- FR-110). Writing the account's initial credential gets a row too, with **both value columns null**, for the
-- same reason a reset does (FR-111): the PIN is never stored, so there is no before or after value to record.
--
-- Stored credential: only the salted hash of the PIN reaches this procedure. The PIN itself is never sent to
-- the store and never appears in any readable row (FR-030).
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_user_management_create;

-- DELIMITER because this body is compound; see the note in
-- sp_auth_temporary_credential_attempt_record/create.sql.
DELIMITER $$

CREATE PROCEDURE sp_user_management_create(
    IN p_username VARCHAR(128),
    IN p_first_name VARCHAR(128),
    IN p_last_name VARCHAR(128),
    IN p_employee_identifier VARCHAR(128),
    IN p_role_code VARCHAR(64),
    IN p_actor_user_id BIGINT,
    IN p_password_hash VARCHAR(128),
    IN p_password_salt VARBINARY(32),
    IN p_change_group_id CHAR(36)
)
BEGIN
    DECLARE v_actor_rank INT DEFAULT 0;
    DECLARE v_requested_rank INT DEFAULT NULL;
    DECLARE v_role_id BIGINT DEFAULT NULL;
    DECLARE v_new_user_id BIGINT DEFAULT NULL;
    DECLARE v_username VARCHAR(128);
    DECLARE v_first_name VARCHAR(128);
    DECLARE v_last_name VARCHAR(128);
    DECLARE v_employee_identifier VARCHAR(128);
    DECLARE v_display_name VARCHAR(256);
    DECLARE v_change_group_id CHAR(36);
    DECLARE v_actor_display_name VARCHAR(256) DEFAULT '';
    DECLARE v_actor_employee_identifier VARCHAR(128) DEFAULT '';
    DECLARE v_actor_role_code VARCHAR(64) DEFAULT '';

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
    SET v_change_group_id = COALESCE(NULLIF(TRIM(p_change_group_id), ''), UUID());

    START TRANSACTION;

    SET v_requested_rank = (
        SELECT r.role_rank
        FROM auth_roles_catalog r
        WHERE r.role_code = TRIM(p_role_code)
        LIMIT 1
    );

    IF v_requested_rank IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_role_unknown';
    END IF;

    SET v_role_id = (
        SELECT r.id
        FROM auth_roles_catalog r
        WHERE r.role_code = TRIM(p_role_code)
        LIMIT 1
    );

    SELECT COALESCE(MAX(r.role_rank), 0)
    INTO v_actor_rank
    FROM auth_roles_assignments ra
    INNER JOIN auth_roles_catalog r ON r.id = ra.role_id
    WHERE ra.user_id = p_actor_user_id;

    IF v_requested_rank > v_actor_rank THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_rank_denied';
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
        1,
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
        p_actor_user_id
    );

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
    VALUES
        (UUID(), v_change_group_id, v_new_user_id, 'username_normalized', NULL, v_username, p_actor_user_id, v_actor_display_name, v_actor_employee_identifier, v_actor_role_code, UTC_TIMESTAMP()),
        (UUID(), v_change_group_id, v_new_user_id, 'first_name', NULL, v_first_name, p_actor_user_id, v_actor_display_name, v_actor_employee_identifier, v_actor_role_code, UTC_TIMESTAMP()),
        (UUID(), v_change_group_id, v_new_user_id, 'last_name', NULL, v_last_name, p_actor_user_id, v_actor_display_name, v_actor_employee_identifier, v_actor_role_code, UTC_TIMESTAMP()),
        (UUID(), v_change_group_id, v_new_user_id, 'display_name', NULL, v_display_name, p_actor_user_id, v_actor_display_name, v_actor_employee_identifier, v_actor_role_code, UTC_TIMESTAMP()),
        (UUID(), v_change_group_id, v_new_user_id, 'employee_identifier', NULL, v_employee_identifier, p_actor_user_id, v_actor_display_name, v_actor_employee_identifier, v_actor_role_code, UTC_TIMESTAMP()),
        (UUID(), v_change_group_id, v_new_user_id, 'role_code', NULL, TRIM(p_role_code), p_actor_user_id, v_actor_display_name, v_actor_employee_identifier, v_actor_role_code, UTC_TIMESTAMP()),
        (UUID(), v_change_group_id, v_new_user_id, 'is_active', NULL, '1', p_actor_user_id, v_actor_display_name, v_actor_employee_identifier, v_actor_role_code, UTC_TIMESTAMP()),
        (UUID(), v_change_group_id, v_new_user_id, 'password_hash', NULL, NULL, p_actor_user_id, v_actor_display_name, v_actor_employee_identifier, v_actor_role_code, UTC_TIMESTAMP());

    COMMIT;
END$$

DELIMITER ;
