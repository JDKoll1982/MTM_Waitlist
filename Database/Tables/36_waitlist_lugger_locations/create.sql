-- Create table: waitlist_lugger_locations
-- Engine: MySQL 5.7
-- Purpose: One row per named scrap lugger. Holds the name the lead maintains and the handler reads on the
--          `pickup-scrap` card, that lugger's own operating status, the reason it is blocked where it is
--          `Do Not Use`, and its place in the display order. Where the material goes is a per scrap type role
--          held by waitlist_lugger_scrap_assignments, so this table carries no dump-point column (FR-001,
--          FR-004, FR-012, research D2).
-- Source: specs/013-lugger-location-config/data-model.md section 1 and
--         specs/013-lugger-location-config/contracts/persistence-and-permission.md section 1.
-- Twin: uq_waitlist_lugger_locations_lugger_name is FR-004's uniqueness rule. The table collation is
--       utf8mb4_unicode_ci, so the unique key refuses `door 6 lugger` beside `Door 6 Lugger`.
-- Audit: every edit to this table appends a row to waitlist_lugger_arrangement_changes in the same
--        transaction; the application never writes either table (FR-032, research D6).
-- Rows are seeded by Database/Seeds/seed_waitlist_lugger_arrangement_defaults (7 rows).

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS waitlist_lugger_locations (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL COMMENT 'Public UUID for the lugger row.',
    lugger_name VARCHAR(64) NOT NULL COMMENT 'The name the lead maintains and the card shows, e.g. Door 6 Lugger.',
    status VARCHAR(16) NOT NULL DEFAULT 'Backup' COMMENT 'One of Available, Backup, Full, Do Not Use. Defaults to Backup: a new lugger holds no dump point until a scrap type order makes it one.',
    status_reason VARCHAR(500) NULL COMMENT 'Why the lugger is Do Not Use. Required for that status, NULL for the other three.',
    display_position INT NOT NULL COMMENT 'The lugger place in the display order.',
    created_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the lugger was added.',
    updated_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the lugger last changed.',
    PRIMARY KEY (id),
    UNIQUE KEY uq_waitlist_lugger_locations_public_id (public_id),
    UNIQUE KEY uq_waitlist_lugger_locations_lugger_name (lugger_name),
    KEY idx_waitlist_lugger_locations_display_position (display_position),
    KEY idx_waitlist_lugger_locations_status (status)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = 'One row per named scrap lugger: name, own status and reason, display order.';

SET FOREIGN_KEY_CHECKS = 1;
