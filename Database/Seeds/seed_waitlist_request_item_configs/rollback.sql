-- Rollback: seed_waitlist_request_item_configs
-- Engine: MySQL 5.7
-- Reverses: Database/Seeds/seed_waitlist_request_item_configs/create.sql
-- Clears the seeded rows. The table itself is dropped by
-- Database/Tables/31_waitlist_request_item_configs/rollback.sql, which owns its existence.
--
-- Reversing the seed reconciliation itself (2026-10-10, spec 012 §9):
--   * The four corrected labels (`Die Location`, `Home Location`, `Scrap Type`, `Coil Number`) are moved back
--     in place by Database/Seeds/seed_waitlist_request_item_configs_label_move/rollback.sql, so an installed
--     store returns to the previous spellings without this file being rerun.
--   * The appended cells and the two source corrections are declarations rather than data, so they are reversed
--     by reinstating the previous revision of create.sql and rerunning it: this artifact truncates and reinserts,
--     so the previous revision restores every row exactly and leaves nothing of this one behind.

USE mtm_waitlist;

TRUNCATE TABLE waitlist_request_item_configs;
