-- Create table: mock_master_tables_registry
-- Engine: MySQL 5.7
-- Purpose: Registry describing every mock master-data table. Each row lists a mock_* table's
--          machine table_name, its UI-readable name, and a plain-language description shown to
--          non-developers (for end-user auditing) in the Developer mock-data settings panel.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS mock_master_tables_registry (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    table_name VARCHAR(128) NOT NULL COMMENT 'Machine name of the mock master table (e.g. mock_parts).',
    ui_display_name VARCHAR(128) NOT NULL COMMENT 'UI-readable name shown in the Developer settings dropdown.',
    description_text VARCHAR(500) NOT NULL COMMENT 'Plain-language description for non-developers/end-user auditing.',
    is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Whether the mock master table is listed/editable.',
    sort_rank INT NOT NULL DEFAULT 0 COMMENT 'Display ordering within the dropdown.',
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_mock_master_tables_registry_public_id (public_id),
    UNIQUE KEY uq_mock_master_tables_registry_table_name (table_name)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;
