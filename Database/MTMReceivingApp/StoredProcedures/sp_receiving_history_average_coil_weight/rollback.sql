-- ============================================================
-- Stored Procedure: sp_receiving_history_average_coil_weight
-- Database:   mtm_receiving_application
-- Engine:     MySQL 5.7
-- Kind:       StoredProcedure / rollback
-- Created:    2026-09-10
-- Feature:    001-module-mock-visual-fallback (task T088)
-- ============================================================
--
-- Removes the procedure created by create.sql. Rollback is a plain drop: the procedure owns no data and
-- no table, so reversing it cannot lose a row.
--
-- Rollback does not restore the caller's former inline statement. If this procedure is dropped while
-- `AverageCoilWeightService` still calls it, that read fails loudly rather than silently falling back to
-- embedded SQL — which is the correct failure for a constitution-III regression, and the reason the two
-- halves ship in the same change.
-- ============================================================

USE mtm_receiving_application;

DROP PROCEDURE IF EXISTS sp_receiving_history_average_coil_weight;
