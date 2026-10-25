-- Create table: waitlist_lugger_scrap_assignments
-- Engine: MySQL 5.7
-- Purpose: One row is one statement that a scrap type dumps through a given lugger: the spelling the lead
--          entered, its normalised key, whether that pairing holds the dump point for the type, and where it
--          sits in the type's try order. The role lives here rather than on the lugger, because a lugger that
--          takes two scrap types can hold the role for one and be a backup for the other (FR-005, FR-006,
--          FR-007, FR-013, research D2).
-- Source: specs/013-lugger-location-config/data-model.md section 2 and
--         specs/013-lugger-location-config/contracts/persistence-and-permission.md section 1.
-- Twin: uq_waitlist_lugger_scrap_assignments_type_lugger refuses the same lugger twice for one scrap type,
--       keyed on the normalised value so ` 5052 ALUMINUM ` cannot slip past it.
-- Cascade: fk_waitlist_lugger_assignments_lugger_locations_lugger_id takes every pairing with the lugger when
--          it is removed, so no assignment ever points at a lugger that no longer exists (FR-005).
-- At most one is_dump_point = 1 row per scrap_type_key is enforced by the procedures, in one transaction, not
-- by a key: MySQL 5.7 has no partial unique index.
-- Rows are seeded by Database/Seeds/seed_waitlist_lugger_arrangement_defaults (7 rows).

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS waitlist_lugger_scrap_assignments (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL COMMENT 'Public UUID for the assignment row.',
    lugger_id BIGINT NOT NULL COMMENT 'The lugger this scrap type dumps through.',
    scrap_type VARCHAR(128) NOT NULL COMMENT 'The scrap type as the lead spelled it, or as the seed wrote it.',
    scrap_type_key VARCHAR(128) NOT NULL COMMENT 'LOWER(TRIM(scrap_type)); the value every lookup matches on.',
    is_dump_point TINYINT(1) NOT NULL DEFAULT 0 COMMENT '1 when this lugger is the one a handler is sent to for this scrap type.',
    sort_position INT NOT NULL COMMENT 'The order the system tries the luggers for this scrap type.',
    created_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the assignment was made.',
    updated_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the assignment or its position last changed.',
    PRIMARY KEY (id),
    UNIQUE KEY uq_waitlist_lugger_scrap_assignments_public_id (public_id),
    UNIQUE KEY uq_waitlist_lugger_scrap_assignments_type_lugger (scrap_type_key, lugger_id),
    KEY idx_waitlist_lugger_scrap_assignments_scrap_type_key (scrap_type_key),
    KEY idx_waitlist_lugger_scrap_assignments_lugger_id (lugger_id),
    CONSTRAINT fk_waitlist_lugger_assignments_lugger_locations_lugger_id FOREIGN KEY (lugger_id) REFERENCES waitlist_lugger_locations (id) ON DELETE CASCADE
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = 'One row per scrap type and lugger pairing: entered spelling, normalised key, dump-point role and try order.';

SET FOREIGN_KEY_CHECKS = 1;
