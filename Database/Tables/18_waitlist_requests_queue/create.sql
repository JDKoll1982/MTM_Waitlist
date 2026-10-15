-- Create table: waitlist_requests_queue
-- Engine: MySQL 5.7
-- Re-keyed by specs/004-unified-card-item-picker: a request carries the chosen Category and Item, and no
-- longer a request type or a subtype (FR-004). Everything else in this table is untouched, including the
-- lifecycle columns and the status vocabulary, because the request lifecycle does not change with the Item
-- (FR-032).
-- No back-fill and no migration path: the database is reinstalled from seed, and this change invents no path
-- for rows that do not exist.
-- idx_waitlist_requests_queue_item_status is added with the columns: sp_waitlist_request_item_observed_average_get
-- groups by item and filters on status, and the re-key is what makes that read possible.
-- Extended by specs/012-waitlist-card-layout: this change added exactly two columns, `handler_status` and
-- `answers_json`, and touched nothing else. `handler_status` is deliberately separate from `status`, because the
-- lifecycle word underneath a condition is still needed when no condition is set (FR-012, research R4).
-- `answers_json` exists because `input_value` holds exactly one answer and the die Items now ask two questions,
-- while the four returning Items each ask one; it is the same label-to-value object shape the schema already
-- uses for structured declaration data (`detail_fields_json`, `options_json`) (FR-073, FR-087, research R5).
-- Both are nullable with no default, so a row written before this change reads as NULL for both, which is the
-- same as "no condition" and "no extra answers" rather than a special case.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS waitlist_requests_queue (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    building VARCHAR(64) NOT NULL,
    work_center VARCHAR(64) NOT NULL,
    category VARCHAR(16) NOT NULL COMMENT 'The Category the requester chose: Pickup, Deliver, Assist or Other.',
    item VARCHAR(64) NOT NULL COMMENT 'The Item the requester chose: one of the catalogued Item codes.',
    input_value VARCHAR(255) NULL,
    answers_json JSON NULL COMMENT 'The answers a request captured beyond input_value, keyed by the label the Item declared; NULL when the request captured none (FR-073, FR-087).',
    active_setup_job_id VARCHAR(64) NOT NULL,
    work_center_name VARCHAR(64) NOT NULL,
    requester_employee_number VARCHAR(32) NOT NULL,
    requester_employee_name VARCHAR(128) NOT NULL,
    status VARCHAR(32) NOT NULL DEFAULT 'Pending',
    handler_status VARCHAR(32) NULL COMMENT 'The condition the assigned material handler reported: one of the four verbatim condition strings, or NULL when no condition is set. Deliberately separate from status (FR-012, FR-070).',
    requested_utc DATETIME NOT NULL,
    target_time_utc DATETIME NULL,
    is_overdue TINYINT(1) NOT NULL DEFAULT 0,
    assigned_material_handler VARCHAR(128) NULL,
    cancellation_reason VARCHAR(255) NULL,
    canceled_utc DATETIME NULL,
    canceled_by_employee_number VARCHAR(32) NULL,
    note VARCHAR(500) NULL,
    accepted_utc DATETIME NULL,
    completed_utc DATETIME NULL,
    released_utc DATETIME NULL,
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_waitlist_requests_queue_public_id (public_id),
    KEY idx_waitlist_requests_queue_building_status (building, status),
    KEY idx_waitlist_requests_queue_work_center (work_center),
    KEY idx_waitlist_requests_queue_requested_utc (requested_utc),
    KEY idx_waitlist_requests_queue_status (status),
    KEY idx_waitlist_requests_queue_item_status (item, status)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;
