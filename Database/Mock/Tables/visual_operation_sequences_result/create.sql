-- ============================================================
-- Object:      visual_operation_sequences_result
-- Database:    mtm_mock
-- Engine:      MySQL 5.7
-- Kind:        Table / create (mirror, shape 2 of 5)
-- Created:     2026-09-09
-- Feature:     001-module-mock-visual-fallback (task T010)
-- Shape key:   operation_sequences
-- Source read: Database/InforVisual/Queues/Module_Setup/Queries/GetSequences.sql
-- Caller:      SetupLookupService.GetSequencesFromBackendAsync
-- ============================================================
--
-- See visual_work_order_lookup_result/create.sql for the shared mirror-table rationale and
-- the approved R9 `work_order` naming deviation.
--
-- `sequence_number` is typed INT UNSIGNED because the Visual source sequence is numeric
-- (OPERATION.SEQUENCE_NO). A real-data type mismatch is a FAILED refresh that leaves the
-- live snapshot untouched — never a corrupted snapshot (data-model.md §3.2, FR-006).
-- ============================================================

USE mtm_mock;

SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS visual_operation_sequences_result (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    normalized_work_order VARCHAR(64) NOT NULL,
    part_number VARCHAR(64) NOT NULL,
    sequence_number INT UNSIGNED NOT NULL,
    description VARCHAR(255) NULL,
    refreshed_utc DATETIME NOT NULL,
    is_seed_content TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (id),
    UNIQUE KEY uq_visual_operation_sequences_result_lookup_key
        (normalized_work_order, part_number, sequence_number)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;
