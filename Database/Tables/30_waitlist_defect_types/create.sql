-- Create table: waitlist_defect_types
-- Engine: MySQL 5.7
-- Purpose: Managed list of non-conforming (NCM) defect types that a Pickup NCM request references.
--          Backed by a Module_Settings editor (Admin/Developer) so a worker can pick an NCM defect
--          from a searchable list rather than typing a free-form value. Mirrors the CSV pickup-ncm
--          "defections" note (new mysql table + stored procedures + settings panel to add/remove).

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS waitlist_defect_types (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL COMMENT 'Public UUID for the defect type row.',
    defect_name VARCHAR(100) NOT NULL COMMENT 'Display name of the NCM defect type (e.g. Scratch, Dent, Wrong Color).',
    description VARCHAR(500) NULL COMMENT 'Optional short description shown in the editor/picker.',
    sort_order INT NOT NULL DEFAULT 0 COMMENT 'Display order within the picker list.',
    is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Whether the defect type is offered to workers.',
    created_by_user_id BIGINT NULL COMMENT 'User who created the defect type.',
    updated_by_user_id BIGINT NULL COMMENT 'User who last updated the defect type.',
    created_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the defect type row was created.',
    updated_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the defect type row was last updated.',
    PRIMARY KEY (id),
    UNIQUE KEY uq_waitlist_defect_types_public_id (public_id),
    UNIQUE KEY uq_waitlist_defect_types_defect_name (defect_name),
    KEY idx_waitlist_defect_types_is_active (is_active)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;
