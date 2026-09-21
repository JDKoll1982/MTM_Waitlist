-- Stored Procedure: sp_user_management_update
-- Engine: MySQL 5.7
-- Feature: 006-user-management-and-permissions (task T012)
--
-- Purpose: correct one person's account, and switch it off or back on, as one act.
--
-- Contract:
--   IN  p_user_id               BIGINT        the account being changed
--   IN  p_username              VARCHAR(128)  the sign-in name as typed; stored upper case
--   IN  p_first_name            VARCHAR(128)
--   IN  p_last_name             VARCHAR(128)
--   IN  p_employee_identifier   VARCHAR(128)
--   IN  p_role_code             VARCHAR(64)   the role the account should hold
--   IN  p_is_active             TINYINT       the active state the account should end in
--   IN  p_actor_user_id         BIGINT        the person performing the change
--   IN  p_change_group_id       CHAR(36)      shared by every audit row this act writes; generated if absent
--
--   OUT the affected-row count, through the non-query seam
--
-- The actor's own rung is read here, never taken from the caller (FR-025).
--
-- Refusals, reported as a stable token:
--   mtm_role_unknown            the requested role is not in the catalogue
--   mtm_target_outranks_actor   the account being changed ranks strictly above the actor (FR-019)
--   mtm_rank_denied             the requested role's rung is above the actor's own (FR-022)
--   mtm_self_deactivate_denied  the actor is switching off their own account (FR-023)
--   mtm_self_rename_denied      the actor is changing the sign-in name of the account they are signed in as
--                               (FR-024)
--
-- A peer is editable, because a person's own rank is not above their own rank (FR-020), and a person's own
-- password change is unaffected by any of this (FR-021).
--
-- The audit rows: one per field that ACTUALLY changed, sharing `p_change_group_id`. A save that changed
-- nothing writes no audit row at all (FR-026, FR-110). The comparison is done here rather than by the caller,
-- so "changed nothing" cannot be mis-reported by a form that sent every field back.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_user_management_update;

-- DELIMITER because this body is compound; see the note in
-- sp_auth_temporary_credential_attempt_record/create.sql.
DELIMITER $$

CREATE PROCEDURE sp_user_management_update(
    IN p_user_id BIGINT,
    IN p_username VARCHAR(128),
    IN p_first_name VARCHAR(128),
    IN p_last_name VARCHAR(128),
    IN p_employee_identifier VARCHAR(128),
    IN p_role_code VARCHAR(64),
    IN p_is_active TINYINT,
    IN p_actor_user_id BIGINT,
    IN p_change_group_id CHAR(36)
)
BEGIN
    DECLARE v_actor_rank INT DEFAULT 0;
    DECLARE v_target_rank INT DEFAULT 0;
    DECLARE v_requested_rank INT DEFAULT NULL;
    DECLARE v_role_id BIGINT DEFAULT NULL;
    DECLARE v_current_role_id BIGINT DEFAULT NULL;
    DECLARE v_current_role_code VARCHAR(64) DEFAULT '';
    DECLARE v_username VARCHAR(128);
    DECLARE v_first_name VARCHAR(128);
    DECLARE v_last_name VARCHAR(128);
    DECLARE v_employee_identifier VARCHAR(128);
    DECLARE v_display_name VARCHAR(256);
    DECLARE v_change_group_id CHAR(36);
    DECLARE v_current_username VARCHAR(128) DEFAULT '';
    DECLARE v_current_first_name VARCHAR(128) DEFAULT '';
    DECLARE v_current_last_name VARCHAR(128) DEFAULT '';
    DECLARE v_current_display_name VARCHAR(256) DEFAULT '';
    DECLARE v_current_employee_identifier VARCHAR(128) DEFAULT NULL;
    DECLARE v_current_is_active TINYINT DEFAULT 0;
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

    -- The target's rung is checked BEFORE the requested role's, so the reader is told the real reason. A reader
    -- editing somebody who outranks them must hear "this account outranks you", not "you cannot assign that
    -- role": the second is true but describes the smaller half of the problem, and the page states the first.
    SELECT COALESCE(r.role_rank, 0), COALESCE(r.id, 0), COALESCE(r.role_code, '')
    INTO v_target_rank, v_current_role_id, v_current_role_code
    FROM auth_roles_assignments ra
    INNER JOIN auth_roles_catalog r ON r.id = ra.role_id
    WHERE ra.user_id = p_user_id
    ORDER BY r.role_rank DESC, r.role_code ASC
    LIMIT 1;

    IF v_target_rank > v_actor_rank THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_target_outranks_actor';
    END IF;

    IF v_requested_rank > v_actor_rank THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_rank_denied';
    END IF;

    SELECT COALESCE(u.username_normalized, ''),
           COALESCE(u.first_name, ''),
           COALESCE(u.last_name, ''),
           COALESCE(u.display_name, ''),
           u.employee_identifier,
           u.is_active
    INTO v_current_username,
         v_current_first_name,
         v_current_last_name,
         v_current_display_name,
         v_current_employee_identifier,
         v_current_is_active
    FROM core_users_profiles u
    WHERE u.id = p_user_id;

    IF p_is_active = 0 AND p_user_id = p_actor_user_id THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_self_deactivate_denied';
    END IF;

    IF v_username <> v_current_username AND p_user_id = p_actor_user_id THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_self_rename_denied';
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
    SET username_normalized = v_username,
        first_name = v_first_name,
        last_name = v_last_name,
        display_name = v_display_name,
        employee_identifier = v_employee_identifier,
        is_active = p_is_active,
        updated_utc = UTC_TIMESTAMP()
    WHERE id = p_user_id;

    IF v_role_id <> v_current_role_id OR v_current_role_id = 0 THEN
        DELETE FROM auth_roles_assignments WHERE user_id = p_user_id;

        INSERT INTO auth_roles_assignments (
            public_id,
            user_id,
            role_id,
            assigned_utc,
            assigned_by_user_id
        )
        VALUES (
            UUID(),
            p_user_id,
            v_role_id,
            UTC_TIMESTAMP(),
            p_actor_user_id
        );
    END IF;

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
    SELECT UUID(),
           v_change_group_id,
           p_user_id,
           changed.field_name,
           changed.previous_value,
           changed.changed_value,
           p_actor_user_id,
           v_actor_display_name,
           v_actor_employee_identifier,
           v_actor_role_code,
           UTC_TIMESTAMP()
    FROM (
        SELECT 'username_normalized' AS field_name, v_current_username AS previous_value, v_username AS changed_value
        UNION ALL
        SELECT 'first_name', v_current_first_name, v_first_name
        UNION ALL
        SELECT 'last_name', v_current_last_name, v_last_name
        UNION ALL
        SELECT 'display_name', v_current_display_name, v_display_name
        UNION ALL
        SELECT 'employee_identifier', COALESCE(v_current_employee_identifier, ''), COALESCE(v_employee_identifier, '')
        UNION ALL
        SELECT 'role_code', v_current_role_code, TRIM(p_role_code)
        UNION ALL
        SELECT 'is_active', CAST(v_current_is_active AS CHAR), CAST(p_is_active AS CHAR)
    ) AS changed
    WHERE NOT (changed.previous_value <=> changed.changed_value);

    COMMIT;
END$$

DELIMITER ;
