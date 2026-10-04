-- Rollback: seed_retire_computer_scope_picture_sources
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T198)
--
-- Nothing to run, and that is stated rather than left for a reader to infer from an empty file.
--
-- The seed withdraws the `computer` scope rows. Reactivating them would put a computer back onto folders of its
-- own that nothing reads any more (FR-040), so a reversal would restore exactly the configuration this change
-- exists to remove. That is not a reversal, it is a re-introduction.
--
-- The rows themselves are not lost: withdrawing sets one flag. A store that needs one computer back on its own
-- folders has the row still there, and `sp_config_images_locations_reactivate` is the shipped way to put a single
-- row back by its public id.
--
-- The table's own rollback (`Database/Tables/17_config_images_locations/rollback.sql`) is what removes the rows
-- themselves, and it is unaffected by this seed.

USE mtm_waitlist;
