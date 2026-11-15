-- ============================================================
-- Object:      visual_die_search_result
-- Database:    mtm_mock
-- Engine:      MySQL 5.7
-- Kind:        Table / create (mirror, shape 12 of 12)
-- Created:     2026-11-15
-- Feature:     001-module-mock-visual-fallback (read shape 12, playbook step 2)
-- Shape key:   die_search
-- Source read: Database/InforVisual/Queues/Module_Waitlist/Queries/GetDieSearch.sql
-- Caller:      GenericRequestDieData.ReadDiesAsync
-- ============================================================
--
-- Stores the flattened result of the generic die request's search, so a die request keeps answering while
-- Infor Visual is unreachable (FR-002, FR-004).
--
-- One row per (term, die, association). The term is the shape's declared INPUT and is what a die can be
-- found BY: a die's own number for a direct match, or a part's number for a die the plant ties to that
-- part. Holding the association rows — rather than only the dies — is what lets a part number still find
-- every die it owns offline, which is the association search 020 described as three relations.
--
-- ssociation is the precedence the live read ranks by: Die, then Explicit (PART_ALTERNATE), then
-- History (the dies the part's work orders required). The read procedure derives the rank from that name
-- and reports each die once, under its strongest association, so it can project exactly the live read's
-- four columns.
--
-- A die's location is the part's PRIMARY location on PART_SITE, falling back to its stock rows, exactly as
-- the live read answers it.
--
-- No baseline seed rows ship for this shape; an empty mirror before the service's first refresh answers
-- "no die matches", which the die page already treats as a miss rather than a failure.
-- ============================================================

USE mtm_mock;

SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS visual_die_search_result (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    term VARCHAR(190) NOT NULL,
    die_number VARCHAR(64) NOT NULL,
    description VARCHAR(255) NULL,
    association VARCHAR(16) NOT NULL,
    location VARCHAR(64) NULL,
    refreshed_utc DATETIME NOT NULL,
    is_seed_content TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (id),
    UNIQUE KEY uq_visual_die_search_result_key (term, die_number, association),
    KEY idx_visual_die_search_result_die_number (die_number)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;