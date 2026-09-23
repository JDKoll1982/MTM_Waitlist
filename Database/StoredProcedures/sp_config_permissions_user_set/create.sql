-- Stored Procedure: sp_config_permissions_user_set
-- Engine: MySQL 5.7
-- Feature: 006-user-management-and-permissions (task T054; amended by T055)
--
-- Purpose: take a whole change set as ONE call and apply it as one act, so a save of several permissions and the
--          reversal of that save are the same call with the same atomicity (FR-071, FR-072).
--
-- Contract:
--   IN  p_user_id       BIGINT        the person the permissions are being set for
--   IN  p_actor_user_id BIGINT        the person performing the change
--   IN  p_changes_json  JSON          [ { "key": ..., "from": ..., "to": ... }, ... ]
--
--   OUT the affected-row count, through the non-query seam. This is a write and returns no result set.
--
-- `from` is the value the caller last saw, and it is what makes a value that has moved impossible to overwrite
-- silently (FR-070). JSON null means "there was no stored value for this person", and it is refused if there now
-- is one.
--
-- `to` is the value to write. It is a value and not an absence: the reversal restores the value that was in force
-- before a save rather than deleting the person's row, because this history table names the value row it changed
-- and the foreign key refuses a row naming one that is gone. That is recorded in
-- sp_config_permissions_reversal_get's header, which is where the reversal is built, and it is the one observable
-- difference a reversal leaves: the restored value is carried by the person's own row rather than inherited.
--
-- THIS IS THE FIRST PLACE A PER-PERSON PERMISSION ROW IS EVER WRITTEN, and it must not run before the settings
-- scope precedence has been re-ranked, so that a person's own value wins over an inherited one (FR-053, decision
-- 29). The re-rank ships in the same feature's foundational phase and the order is a gate, not advice: a row
-- written under the old order would be silently overridden, and nothing would report it.
--
-- The actor's own rung, and the target's, are read here from auth_roles_assignments joined to
-- auth_roles_catalog. Neither is taken from the caller (FR-025).
--
-- Refusals, reported as a stable token so the caller can answer in the reader's words. The token is followed by
-- the key it is about where there is one, so "which key moved" is reported rather than only that something did:
--   mtm_target_outranks_actor        the target's rung is above the actor's
--   mtm_permission_gate_fixed        the set names the permission that opens this page
--   mtm_permission_key_invalid       the set names a key outside the permission namespace
--   mtm_permission_value_moved:<key> the stored value is not the `from` the caller last saw
--
-- `mtm_permission_key_invalid` is NOT in the refusal list contracts/database-contract.md pins. That list carries
-- no token for the namespace rule, and reusing one of the tokens it does carry would report a different refusal
-- than the one that happened, so the token is added and the contract is updated in the same change rather than
-- the rule being silently dropped.
--
-- A REVERSAL IS THE SAME CALL, which is why it obeys the same rule as the change it reverses (FR-069): the caller
-- sends the values back the other way round, and every check below applies to it exactly as it applied to the
-- save. There is deliberately no separate reversal path, because a second path would be a second rule.
--
-- ONE CLOCK FOR THE WHOLE CALL. `v_now` is read once and used by every row this call writes, so the rows of one
-- save share an exact `changed_utc` and the reversal can find them as a group. `UTC_TIMESTAMP()` per statement
-- would not do that: it returns the time the statement began, so a save of several keys straddling a second
-- boundary would write two timestamps and the reversal would reverse half of it. That is a real hazard and this
-- is the fix, not a precaution.
--
-- An EMPTY SET, and an entry that changes nothing, write NOTHING AT ALL (FR-068): the procedure returns before
-- the transaction opens when the set is empty, and skips the history row for an entry whose value already matches
-- what is stored. That is also why the page can treat "nothing has changed" as unavailable rather than as a write
-- that happens to have no effect.
--
-- One history row per changed key carries the actor, the key, the scope, both values and the time (FR-072). A
-- removal records the value it removed as `previous_setting_value_bool` and writes NULL as the changed value,
-- which is what "this person no longer has a choice here" looks like in a value column.
--
-- THE VALUE ROW'S ID IS READ, NEVER TAKEN FROM LAST_INSERT_ID(). The upsert takes the UPDATE branch whenever the
-- person already has a row of their own, and an upsert that updated generates no id: LAST_INSERT_ID() then answers
-- with whatever the last insert on the connection generated, which on a pooled connection is another table's id
-- entirely. That is not a precaution — it named a config_settings_history row as a value row and the foreign key
-- refused the reversal, which is how it was found by running a save and its reversal against the live store.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_permissions_user_set;

-- DELIMITER because this body is compound; see the note in
-- sp_auth_temporary_credential_attempt_record/create.sql.
DELIMITER $$

CREATE PROCEDURE sp_config_permissions_user_set(
    IN p_user_id BIGINT,
    IN p_actor_user_id BIGINT,
    IN p_changes_json JSON
)
BEGIN
    DECLARE v_count INT DEFAULT 0;
    DECLARE v_index INT DEFAULT 0;
    DECLARE v_key VARCHAR(190) DEFAULT '';
    DECLARE v_from_raw JSON DEFAULT NULL;
    DECLARE v_from_text VARCHAR(32) DEFAULT NULL;
    DECLARE v_from_bool TINYINT(1) DEFAULT NULL;
    DECLARE v_to_text VARCHAR(32) DEFAULT NULL;
    DECLARE v_to_bool TINYINT(1) DEFAULT 0;
    DECLARE v_target_rank INT DEFAULT 0;
    DECLARE v_actor_rank INT DEFAULT 0;
    DECLARE v_scope_key VARCHAR(255) DEFAULT '';
    DECLARE v_existing_count INT DEFAULT 0;
    DECLARE v_existing_bool TINYINT(1) DEFAULT NULL;
    DECLARE v_setting_id BIGINT DEFAULT NULL;
    DECLARE v_previous_bool TINYINT(1) DEFAULT NULL;
    DECLARE v_skip TINYINT(1) DEFAULT 0;
    DECLARE v_now DATETIME;
    DECLARE v_signal_text VARCHAR(255) DEFAULT '';

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SET v_count = IFNULL(JSON_LENGTH(p_changes_json), 0);

    -- Nothing to write. No transaction opens, so no row and no history record is written (FR-068).
    IF v_count > 0 THEN
        SET v_scope_key = CONCAT('user:', p_user_id);
        SET v_now = UTC_TIMESTAMP();

        START TRANSACTION;

        SELECT COALESCE(MAX(r.role_rank), 0)
        INTO v_target_rank
        FROM auth_roles_assignments ra
        INNER JOIN auth_roles_catalog r ON r.id = ra.role_id
        WHERE ra.user_id = p_user_id;

        SELECT COALESCE(MAX(r.role_rank), 0)
        INTO v_actor_rank
        FROM auth_roles_assignments ra
        INNER JOIN auth_roles_catalog r ON r.id = ra.role_id
        WHERE ra.user_id = p_actor_user_id;

        IF v_target_rank > v_actor_rank THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_target_outranks_actor';
        END IF;

        WHILE v_index < v_count DO
            SET v_skip = 0;

            SET v_key = COALESCE(JSON_UNQUOTE(JSON_EXTRACT(p_changes_json, CONCAT('$[', v_index, '].key'))), '');
            SET v_from_raw = JSON_EXTRACT(p_changes_json, CONCAT('$[', v_index, '].from'));
            SET v_to_text = LOWER(COALESCE(JSON_UNQUOTE(JSON_EXTRACT(p_changes_json, CONCAT('$[', v_index, '].to'))), 'false'));

            SET v_from_text = CASE
                WHEN v_from_raw IS NULL OR JSON_TYPE(v_from_raw) = 'NULL' THEN NULL
                ELSE LOWER(JSON_UNQUOTE(v_from_raw))
            END;

            SET v_from_bool = CASE
                WHEN v_from_text IS NULL THEN NULL
                WHEN v_from_text IN ('true', '1') THEN 1
                ELSE 0
            END;

            SET v_to_bool = IF(v_to_text IN ('true', '1'), 1, 0);

            IF v_key = '' THEN
                SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_permission_key_invalid:';
            END IF;
            -- The one fixed row: reading the declaration to decide whether the declaration may be edited would
            -- be circular, so this is refused here rather than on the page alone (FR-059).
            IF v_key = 'permission.admin.permissions' THEN
                SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_permission_gate_fixed:permission.admin.permissions';
            END IF;

            -- Outside the namespace this procedure owns. A preference is not a permission, and this call must
            -- not become a general settings writer through the back door.
            --
            -- MySQL 5.7's SIGNAL takes a literal or a variable in its SET clause and nothing else, so the two
            -- refusals that name the key build the text in a local variable first. A function call in the SET
            -- clause is a syntax error, which is how this was found by running it.
            IF v_key NOT LIKE 'permission.%' THEN
                SET v_signal_text = CONCAT('mtm_permission_key_invalid:', v_key);
                SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = v_signal_text;
            END IF;

            SELECT COUNT(*), MAX(v.setting_value_bool)
            INTO v_existing_count, v_existing_bool
            FROM config_settings_values v
            WHERE v.setting_key = v_key
              AND v.scope_key = v_scope_key;

            -- The value must still be the one the caller last saw, or it is overwritten blindly and a change
            -- somebody else made is lost without anybody being told (FR-070).
            IF v_from_bool IS NULL THEN
                IF v_existing_count > 0 THEN
                    SET v_signal_text = CONCAT('mtm_permission_value_moved:', v_key);
                    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = v_signal_text;
                END IF;
            ELSE
                IF v_existing_count = 0 OR IFNULL(v_existing_bool, -1) <> v_from_bool THEN
                    SET v_signal_text = CONCAT('mtm_permission_value_moved:', v_key);
                    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = v_signal_text;
                END IF;
            END IF;

            SET v_previous_bool = v_existing_bool;
            SET v_setting_id = NULL;

            IF v_existing_count = 0 OR IFNULL(v_existing_bool, -1) <> v_to_bool THEN
                INSERT INTO config_settings_values (
                    public_id,
                    setting_key,
                    scope_type,
                    scope_key,
                    user_id,
                    setting_value_bool,
                    value_type,
                    updated_by_user_id,
                    updated_utc
                )
                VALUES (
                    UUID(),
                    v_key,
                    'user',
                    v_scope_key,
                    p_user_id,
                    v_to_bool,
                    'bool',
                    p_actor_user_id,
                    v_now
                )
                ON DUPLICATE KEY UPDATE
                    setting_value_bool = VALUES(setting_value_bool),
                    value_type = VALUES(value_type),
                    user_id = VALUES(user_id),
                    updated_by_user_id = VALUES(updated_by_user_id),
                    updated_utc = VALUES(updated_utc);

                -- The row's id is READ rather than taken from LAST_INSERT_ID(). An upsert that took the UPDATE
                -- branch generates no id at all, and LAST_INSERT_ID() then answers with whatever the last insert
                -- on this connection generated — on a pooled connection that is some other table's id entirely.
                -- It named a config_settings_history row as a value row and the foreign key refused the history
                -- write, which is how this was found by running it. The read is unconditional, because a
                -- non-zero answer is exactly as possible as a zero one and the guard below could not tell them
                -- apart.
                SET v_setting_id = NULL;

                SELECT v.id
                INTO v_setting_id
                FROM config_settings_values v
                WHERE v.setting_key = v_key
                  AND v.scope_key = v_scope_key
                LIMIT 1;
            ELSE
                -- The value is already what was asked for, so nothing changed and no history row is written.
                SET v_skip = 1;
            END IF;

            IF v_skip = 0 THEN
                INSERT INTO config_settings_history (
                    public_id,
                    config_setting_id,
                    setting_key,
                    scope_type,
                    scope_key,
                    user_id,
                    previous_setting_value_bool,
                    changed_setting_value_bool,
                    value_type,
                    changed_by_user_id,
                    changed_utc
                )
                VALUES (
                    UUID(),
                    v_setting_id,
                    v_key,
                    'user',
                    v_scope_key,
                    p_user_id,
                    v_previous_bool,
                    v_to_bool,
                    'bool',
                    p_actor_user_id,
                    v_now
                );
            END IF;

            SET v_index = v_index + 1;
        END WHILE;

        COMMIT;
    END IF;
END$$

DELIMITER ;
