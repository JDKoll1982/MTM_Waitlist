-- ============================================================
-- Object:      visual_part_matches_result
-- Database:    mtm_mock
-- Engine:      MySQL 5.7
-- Kind:        Table / create (mirror, shape 9 of 12)
-- Created:     2026-11-15
-- Feature:     001-module-mock-visual-fallback (read shape 9, playbook step 2)
-- Shape key:   part_matches
-- Source read: Database/InforVisual/Queues/Module_Waitlist/Queries/GetPartMatches.sql
-- Caller:      GenericRequestPartData.ReadCompletionsAsync
-- ============================================================
--
-- Stores the flattened result of the generic work centre request's part search, so the part page keeps
-- answering while Infor Visual is unreachable (FR-002, FR-004). Read from a snapshot: the live table
-- always resolves to a complete result because refreshes swap it atomically with its _stage twin
-- (FR-006).
--
-- One row per part. `term` is the shape's declared INPUT and `description` is its second declared
-- output, so the physical layout is the one every mirror keeps: id, the input keys, the output columns,
-- then refreshed_utc and is_seed_content.
--
-- No baseline seed rows ship for this shape. The part master is refreshed by the on-host service, and
-- an empty mirror before its first refresh answers an empty completion list, which the part page
-- already treats as "nothing typed matches yet".
-- ============================================================

USE mtm_mock;

SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS visual_part_matches_result (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    term VARCHAR(190) NOT NULL,
    part_number VARCHAR(64) NOT NULL,
    description VARCHAR(255) NULL,
    refreshed_utc DATETIME NOT NULL,
    is_seed_content TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (id),
    UNIQUE KEY uq_visual_part_matches_result_part_number (part_number),
    KEY idx_visual_part_matches_result_term (term)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;
