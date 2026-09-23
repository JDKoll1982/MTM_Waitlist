-- Seed: seed_picture_layout_move
-- Engine: MySQL 5.7
-- Feature: 008-part-pictures (task T021)
--
-- Purpose: rewrite every recorded picture path the application has already stored from the pre-move layout to the
--          one the share now uses (FR-036). The picture root does not change; what changes is where each picture
--          sits inside it, so a value that named `request_item\pickup-coil.png` now names
--          `Waitlist/request_item/pickup-coil.png`.
--
-- What moves, and why exactly these three
--   The application's own collection gained one folder level in front of it, so its three kinds each gain the
--   `Waitlist` collection. The two part collections are new and have nothing already stored to move. Every pair is
--   the same token in and the same token out with the collection folder in front, which is what keeps the move an
--   insertion rather than a rename.
--
-- Re-running
--   Each move is idempotent: after it, no value begins with the old prefix, so a second run rewrites nothing and
--   reports zero. Running this file twice is therefore safe and is the expected way to resume an interrupted
--   promotion.
--
-- What it reports rather than skips
--   The pairs are known and finite, so a stored value that matches none of them is not silently left behind: the
--   report at the end names every active value that begins with no collection folder, is not a rooted path and
--   names no drive. Those are the rows a person has to look at, and FR-036's "none left unresolvable" is answered
--   by that list rather than by a guess.
--
-- The picture change record
--   A layout move is not a picture change, so the whole move runs with `@mtm_picture_layout_move` set, which is the
--   flag the AFTER UPDATE trigger checks before it writes a history row. Without it this one reviewed step would
--   write a change row per stored picture, each naming an actor who never touched it.
--
-- Ordering
--   This folder sorts after the table and procedure folders, and it needs `sp_config_images_locations_paths_move`
--   to exist, which the procedure folder creates.
-- ============================================================

USE mtm_waitlist;

SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS sp_seed_picture_layout_move;

DELIMITER $$

CREATE PROCEDURE sp_seed_picture_layout_move()
BEGIN
    DECLARE v_request_items INT DEFAULT 0;
    DECLARE v_request_categories INT DEFAULT 0;
    DECLARE v_work_centers INT DEFAULT 0;

    -- Set for the whole move and cleared before this procedure returns, so nothing else on this connection is
    -- affected. A layout move is not a picture change, so the change record stays silent (FR-027).
    SET @mtm_picture_layout_move = 1;

    CALL sp_config_images_locations_paths_move('request_item', 'Waitlist/request_item');
    SET v_request_items = ROW_COUNT();

    CALL sp_config_images_locations_paths_move('request_category', 'Waitlist/request_category');
    SET v_request_categories = ROW_COUNT();

    CALL sp_config_images_locations_paths_move('work_center', 'Waitlist/work_center');
    SET v_work_centers = ROW_COUNT();

    SET @mtm_picture_layout_move = NULL;

    SELECT
        v_request_items AS request_item_paths_moved,
        v_request_categories AS request_category_paths_moved,
        v_work_centers AS work_center_paths_moved;
END$$

DELIMITER ;

CALL sp_seed_picture_layout_move();

-- The rows a person has to look at: an active value that begins with no collection folder, is not a UNC path and
-- names no drive. An empty result set is the answer FR-036 wants.
SELECT
    i.scope,
    i.scope_item_id,
    i.image_path AS unrecognised_path
FROM
    config_images_locations i
WHERE
    i.is_active = 1
    AND i.image_path NOT LIKE 'Waitlist%'
    AND i.image_path NOT LIKE 'Visual%'
    AND i.image_path NOT LIKE 'WIP%'
    AND LEFT(i.image_path, 2) <> '\\\\'
    AND LOCATE(':', i.image_path) = 0
ORDER BY
    i.scope,
    i.scope_item_id;
