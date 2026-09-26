-- Stored Procedure: sp_config_images_locations_computer_sources_set
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T051)
--
-- Purpose: write one machine's three picture sources, as one act.
--
-- Contract:
--   IN  p_computer_id          BIGINT        the machine, from `core_computers_registry.id`
--   IN  p_actor_user_id        BIGINT        the person completing machine setup; NULL when there is none
--   IN  p_shared_folder_path   VARCHAR(500)  the root every stored picture sits under
--   IN  p_keys_folder_path     VARCHAR(500)  the folder holding the shared key material
--   IN  p_dunnage_root_path    VARCHAR(500)  the root the dunnage pictures sit under
--   OUT the affected-row count, through the non-query seam. MySQL reports 1 per inserted row and 2 per replaced
--       row, so a first save reports 3 and a re-save reports 6; the caller treats anything below 3 as a
--       database error.
--
-- All three rows are written in one statement inside one transaction, because a machine with two of its three
-- folders is not half-configured — it is configured wrongly, and the next launch would read pictures from a root
-- nobody chose. Committing them together means a launch either finds all three or finds none.
--
-- This is the only writer of a `computer` scope row, and it composes the item id and the machine column from the
-- same parameter in the same statement. That is what keeps the two representations from disagreeing — the item id
-- is what the unique key uses, and `computer_id` is what the reader filters on.
--
-- A re-save replaces the paths in place and reactivates rows that had been withdrawn, so saving twice leaves
-- three rows and not six. `created_*` is left alone on the replace path: it records when this machine was first
-- given its folders, and a later save is not that moment.
--
-- An empty or missing path is refused as `mtm_computer_source_path_missing`. The machine's three sources are
-- required by the shape the setup screen collects (contracts/machine-configuration-contract.md section 1), so a
-- save that omits one is a caller bug rather than a machine that reads from nowhere.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_computer_sources_set;

DELIMITER $$

CREATE PROCEDURE sp_config_images_locations_computer_sources_set(
    IN p_computer_id BIGINT,
    IN p_actor_user_id BIGINT,
    IN p_shared_folder_path VARCHAR(500),
    IN p_keys_folder_path VARCHAR(500),
    IN p_dunnage_root_path VARCHAR(500)
)
BEGIN
    DECLARE v_computer_key VARCHAR(190);
    DECLARE v_now_utc DATETIME;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    IF p_shared_folder_path IS NULL OR TRIM(p_shared_folder_path) = ''
        OR p_keys_folder_path IS NULL OR TRIM(p_keys_folder_path) = ''
        OR p_dunnage_root_path IS NULL OR TRIM(p_dunnage_root_path) = '' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_computer_source_path_missing';
    END IF;

    SET v_computer_key = CAST(p_computer_id AS CHAR);
    SET v_now_utc = fn_server_utc_now();

    START TRANSACTION;

    INSERT INTO config_images_locations (
        public_id,
        scope,
        scope_item_id,
        computer_id,
        image_path,
        is_active,
        created_by_user_id,
        updated_by_user_id,
        created_utc,
        updated_utc
    )
    VALUES
        (UUID(), 'computer', CONCAT(v_computer_key, ':shared_folder'), p_computer_id, TRIM(p_shared_folder_path), 1, p_actor_user_id, p_actor_user_id, v_now_utc, v_now_utc),
        (UUID(), 'computer', CONCAT(v_computer_key, ':keys_folder'), p_computer_id, TRIM(p_keys_folder_path), 1, p_actor_user_id, p_actor_user_id, v_now_utc, v_now_utc),
        (UUID(), 'computer', CONCAT(v_computer_key, ':dunnage_root'), p_computer_id, TRIM(p_dunnage_root_path), 1, p_actor_user_id, p_actor_user_id, v_now_utc, v_now_utc)
    ON DUPLICATE KEY UPDATE
        image_path = VALUES(image_path),
        computer_id = VALUES(computer_id),
        is_active = 1,
        updated_by_user_id = VALUES(updated_by_user_id),
        updated_utc = VALUES(updated_utc);

    COMMIT;
END$$

DELIMITER ;
