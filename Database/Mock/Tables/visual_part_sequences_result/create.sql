-- ============================================================
-- Object:      visual_part_sequences_result
-- Database:    mtm_mock
-- Engine:      MySQL 5.7
-- Kind:        Table / create (mirror, shape 10 of 12)
-- Created:     2026-11-15
-- Feature:     001-module-mock-visual-fallback (read shape 10, playbook step 2)
-- Shape key:   part_sequences
-- Source read: Database/InforVisual/Queues/Module_Waitlist/Queries/GetPartSequences.sql
-- Caller:      GenericRequestPartData.ReadSequencesAsync
-- ============================================================
--
-- Stores the flattened result of the generic work centre request's sequence list, so the sequence step
-- keeps answering while Infor Visual is unreachable (FR-002, FR-004).
--
-- One row per (part, sequence, relation). `part_number` is the shape's declared INPUT; the rest are its
-- declared outputs, in their declared order, so the physical layout is the one every mirror keeps:
-- id, the input keys, the output columns, then refreshed_utc and is_seed_content.
--
-- The relation is kept in `sequence_source` because the caller merges the rows a number is named by into
-- one option, and it can only do that if a row still says which relation named it.
--
-- The mirror holds the sequences of OPEN work orders, which is the domain the read offers: it ranks an
-- open order's operation ahead of a closed one, and a sequence the part only ever ran on orders the plant
-- has finished is not something a requester raises a request against. This is the same addressable
-- population rule work_order_search uses.
--
-- No baseline seed rows ship for this shape; an empty mirror before the service's first refresh answers
-- an empty list, which the sequence step already treats as nothing recorded yet.
-- ============================================================

USE mtm_mock;

SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS visual_part_sequences_result (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    part_number VARCHAR(64) NOT NULL,
    sequence VARCHAR(10) NOT NULL,
    description VARCHAR(255) NULL,
    operation_status VARCHAR(1) NULL,
    is_last_operation TINYINT NOT NULL DEFAULT 0,
    next_resource VARCHAR(255) NULL,
    cancelled_only TINYINT NOT NULL DEFAULT 0,
    sequence_source VARCHAR(20) NOT NULL,
    refreshed_utc DATETIME NOT NULL,
    is_seed_content TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (id),
    UNIQUE KEY uq_visual_part_sequences_result_key (part_number, sequence, sequence_source),
    KEY idx_visual_part_sequences_result_sequence (sequence)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;
