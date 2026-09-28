-- Seed: seed_retire_computer_scope_picture_sources
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T198)
--
-- Purpose: withdraw every `computer` scope row in `config_images_locations`, so a computer set up before this
--          change reads its pictures from the plant-wide setting afterwards (FR-040, SC-021).
--
-- Why this is a seed and not a schema change: the rows are data. The three per-computer sources were written by
-- `sp_config_images_locations_computer_sources_set`, which this task retires with this seed. What a computer set
-- up under the old shape still holds is three rows of its own, and withdrawing them is what leaves the plant-wide
-- answer as the only one in force.
--
-- It withdraws rather than deletes, deliberately. The change history and `sp_config_images_locations_history_get`
-- keep reading what was there, and a withdrawn row is what tells a reader that a computer once had folders of its
-- own. Nothing reads a `computer` scope row any more — that is the read the same task removed — so a withdrawn
-- row changes no answer, and a row that is still there is recoverable by hand.
--
-- The withdrawal goes through the shipped procedure rather than an UPDATE of its own, so one place owns that
-- column (constitution III) and the row's `updated_utc` is the store's clock rather than the installer's.
--
-- Ordering inside AllSeeds.sql: it depends on nothing but the procedures being installed, which happens before
-- every seed, and no other seed reads or writes a `computer` scope row, so its position in the file is free. It
-- reads and writes no setting, no user, no role and no request.

USE mtm_waitlist;

CALL sp_config_images_locations_deactivate_for_scope('computer', NULL);
