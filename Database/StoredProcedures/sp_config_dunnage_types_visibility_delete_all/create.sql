-- Stored Procedure: sp_config_dunnage_types_visibility_delete_all
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T095)
--
-- Purpose: clear the whole visibility map before it is rewritten. Replaces the inline
--          `DELETE FROM config_dunnage_types_visibility;` in
--          `DunnageTypeVisibilityCatalogService.SaveVisibleDunnageTypesAsync` (FR-015).
--
-- Contract:
--   OUT no result set. The affected-row count reaches the caller from the client if it wants it; the caller
--       only cares that the table is empty afterwards.
--
-- This is a whole-table delete because the map is a *set*, not a collection of records: the caller rewrites it
-- from the receiving store's current type list, so a type that no longer exists in the receiving store must not
-- survive as a stale visibility row. That is also why no `WHERE` clause is offered here — a filtered delete
-- would have to know which rows the caller is about to write.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_dunnage_types_visibility_delete_all;

CREATE PROCEDURE sp_config_dunnage_types_visibility_delete_all()
DELETE FROM config_dunnage_types_visibility;
