-- Create table: waitlist_lugger_arrangement_changes
-- Engine: MySQL 5.7
-- Purpose: One row per recorded change to the lugger arrangement. It is both the audit trail FR-032 asks for
--          and the source of the in-application notice FR-033 delivers, read above a session watermark on
--          `id` (FR-037, research D6).
-- Source: specs/013-lugger-location-config/data-model.md section 3 and
--         specs/013-lugger-location-config/contracts/persistence-and-permission.md section 1.
-- No foreign key on lugger_id: a cascade would delete the audit with the lugger, and SET NULL would rewrite
-- history. `lugger_name` is the durable identifier and `lugger_id` a convenience (FR-005, research D3).
-- Append only: a row is written by the procedure that makes the change and is never updated or deleted.
-- change_kind is one of lugger_added, lugger_renamed, lugger_removed, status_changed,
-- dump_point_promoted, assignment_added, assignment_removed, assignment_reordered, lugger_reordered.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS waitlist_lugger_arrangement_changes (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL COMMENT 'Public UUID for the change row.',
    change_kind VARCHAR(32) NOT NULL COMMENT 'What kind of edit was made. One of the nine pinned kinds.',
    lugger_id BIGINT NULL COMMENT 'Cross reference to the lugger as it stands; NULL once the lugger was removed.',
    lugger_name VARCHAR(64) NOT NULL COMMENT 'The name the lugger holds after the change, or held when removed. Text on purpose, so the record outlives the row.',
    scrap_type VARCHAR(128) NULL COMMENT 'The affected scrap type as spelled at the time; NULL when the change touched none.',
    scrap_type_key VARCHAR(128) NULL COMMENT 'The normalised key, so a row is found by the same match the card uses.',
    previous_value VARCHAR(255) NULL COMMENT 'The value before the change, read according to change_kind.',
    new_value VARCHAR(255) NULL COMMENT 'The value after the change, read according to change_kind.',
    change_reason VARCHAR(500) NULL COMMENT 'The Do Not Use reason, kept after the lugger row clears it.',
    changed_by_employee_number VARCHAR(32) NOT NULL COMMENT 'Who made the change.',
    changed_by_employee_name VARCHAR(128) NOT NULL COMMENT 'The same person name, so the notice and the audit read without a join.',
    changed_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the change was made.',
    PRIMARY KEY (id),
    UNIQUE KEY uq_waitlist_lugger_arrangement_changes_public_id (public_id),
    KEY idx_waitlist_lugger_arrangement_changes_changed_utc (changed_utc),
    KEY idx_waitlist_lugger_arrangement_changes_lugger_id (lugger_id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = 'Append-only record of every edit to the lugger arrangement: what changed, from what to what, by whom and when.';

SET FOREIGN_KEY_CHECKS = 1;
