-- Rollback: seed_picture_layout_move
-- Engine: MySQL 5.7
-- Feature: 008-part-pictures (task T021)
--
-- Purpose: rewrite each recorded value this seed changed back to the value it replaced, from the same table of
--          pairs and in the opposite direction.
--
-- The table of pairs, reversed: the `Waitlist` collection folder the move inserted comes back off, in front of
-- each of the three kinds. Nothing else about a value is touched, and a value that does not begin with one of the
-- three new prefixes is left exactly where it is.
--
-- Run `StoredProcedures/sp_config_images_locations_paths_move/rollback.sql` first. That drops the rewrite
-- procedure, and this file does not need it — the reversal is written out here rather than reached for through a
-- procedure that is about to stop existing, so the reversal survives the thing it reverses.
--
-- A value that was already in the new layout before this seed ran is indistinguishable from one it moved, so the
-- reversal puts every such value back to the pre-move shape. That is the honest reading of FR-036: the reversal
-- restores the layout the store had before this change, and a store that had already moved is a store where the
-- move was not this feature's.

USE mtm_waitlist;

SET NAMES utf8mb4;

-- A layout move is not a picture change, so the reversal suppresses the change record exactly as the move does.
SET @mtm_picture_layout_move = 1;

UPDATE config_images_locations
SET image_path = CONCAT(
        'request_item',
        SUBSTRING(image_path, CHAR_LENGTH('Waitlist/request_item') + 1)
    )
WHERE is_active = 1
  AND image_path LIKE 'Waitlist/request_item%';

UPDATE config_images_locations
SET image_path = CONCAT(
        'request_category',
        SUBSTRING(image_path, CHAR_LENGTH('Waitlist/request_category') + 1)
    )
WHERE is_active = 1
  AND image_path LIKE 'Waitlist/request_category%';

UPDATE config_images_locations
SET image_path = CONCAT(
        'work_center',
        SUBSTRING(image_path, CHAR_LENGTH('Waitlist/work_center') + 1)
    )
WHERE is_active = 1
  AND image_path LIKE 'Waitlist/work_center%';

SET @mtm_picture_layout_move = NULL;
