-- Stored Procedure: sp_config_images_locations_paths_move
-- Engine: MySQL 5.7
-- Feature: 008-part-pictures (task T021)
--
-- Purpose: rewrite the recorded picture paths from one layout to another, as the reviewed run-once step FR-036
--          calls for. One prefix in, one prefix out, and the rows it rewrote reported by the affected-row count.
--
-- Contract:
--   IN  p_old_prefix  VARCHAR(64)  the token every value to rewrite begins with, e.g. 'request_item'
--   IN  p_new_prefix  VARCHAR(64)  the token to put in its place, e.g. 'Waitlist/request_item'
--   OUT affected row count, through the non-query seam. The statement is a single UPDATE and is the procedure's
--       last statement, so MySQL reports this UPDATE's count for the CALL and a caller can say how many rows it
--       rewrote rather than guessing.
--
-- Why the prefix carries no separator: a recorded value is written with either separator depending on how it was
-- produced, and the move must not care. Rewriting only the leading token leaves whatever separator followed it
-- exactly where it was, so `request_item\pickup-coil.png` and `request_item/pickup-coil.png` both become
-- `Waitlist/request_item\pickup-coil.png` and `Waitlist/request_item/pickup-coil.png`.
--
-- Running it twice is safe. After the first run no value begins with the old prefix any more — a moved value
-- begins with the collection folder — so the second run matches nothing and reports zero rows. The two guards in
-- the WHERE clause are the same promise stated for the degenerate inputs: an empty prefix would match every row
-- and a prefix equal to its replacement would rewrite every row to itself, so neither is allowed to do anything.
--
-- Never run from application startup. Schema and data changes are hand-maintained, reviewed and promoted
-- (constitution III, database-schema-rules), and this one rewrites every picture the application has stored.
--
-- The picture change record: the image_path update triggers the AFTER UPDATE history trigger, and a layout move is
-- not a picture change. The caller sets the session flag `@mtm_picture_layout_move = 1` on the same connection
-- before calling this and clears it afterwards, which the trigger checks before recording. The paired seed does
-- exactly that; a hand-run move must too.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_paths_move;

CREATE PROCEDURE sp_config_images_locations_paths_move(
    IN p_old_prefix VARCHAR(64),
    IN p_new_prefix VARCHAR(64)
)
UPDATE config_images_locations
SET image_path = CONCAT(
        p_new_prefix,
        SUBSTRING(image_path, CHAR_LENGTH(p_old_prefix) + 1)
    )
WHERE is_active = 1
  AND p_old_prefix <> ''
  AND p_old_prefix <> p_new_prefix
  AND image_path LIKE CONCAT(p_old_prefix, '%');
