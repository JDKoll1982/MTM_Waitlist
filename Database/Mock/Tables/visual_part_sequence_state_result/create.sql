-- ============================================================
-- Object:      visual_part_sequence_state_result
-- Database:    mtm_mock
-- Engine:      MySQL 5.7
-- Kind:        Table / create (mirror, shape 11 of 12)
-- Created:     2026-11-15
-- Feature:     001-module-mock-visual-fallback (read shape 11, playbook step 2)
-- Shape key:   part_sequence_state
-- Source read: Database/InforVisual/Queues/Module_Waitlist/Queries/GetPartSequenceState.sql
-- Caller:      GenericRequestPartData.ReadQuantityAsync
-- ============================================================
--
-- Stores one part's operation state at one sequence, so the quantity step keeps deciding who owns the
-- quantity while Infor Visual is unreachable (FR-002, FR-004).
--
-- At most one row per (part, sequence). No row means the part has no operation at that sequence, which
-- the caller reads as "no state recorded" and answers by asking the floor first — deliberately not the
-- same as an operation that exists with a null status.
--
-- part_number and sequence are the shape's declared INPUTS, held because the refresh procedure reads
-- every declared parameter and column out of the payload. The state the row carries is the operation that
-- speaks for the sequence: the open order's, exactly as the per-key read picks it.
--
-- The mirror holds the state of open work orders, the same addressable domain as shape 10. No baseline
-- seed rows ship; an empty mirror before the service's first refresh reads as "no state recorded", which
-- sends the caller to the floor — the answer it already gives for a part whose routing it cannot read.
-- ============================================================

USE mtm_mock;

SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS visual_part_sequence_state_result (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    part_number VARCHAR(64) NOT NULL,
    sequence VARCHAR(20) NOT NULL,
    operation_status VARCHAR(1) NULL,
    is_last_operation TINYINT NOT NULL DEFAULT 0,
    next_resource VARCHAR(255) NULL,
    refreshed_utc DATETIME NOT NULL,
    is_seed_content TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (id),
    UNIQUE KEY uq_visual_part_sequence_state_result_key (part_number, sequence)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;