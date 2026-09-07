-- Create table: waitlist_request_types
-- Engine: MySQL 5.7
-- Purpose: Real (non-mock) request-type catalog that drives the New-Request wizard. Each row is one
--          request type (Pickup, Other, Coil, Scrap, Flatstock, Table Handling, Die Handling,
--          Forklift Assist) with its stable GUID, WinUI control type name, default flow/text-input
--          validation rules, default image path, and ordered center data-grid column labels.
-- Source: Assets/Config/waitlist-request-types.json (requestType objects).
--         The public_id is the stable GUID in the JSON 'id' field (== RequestTypeInventory.StableId).

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS waitlist_request_types (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL COMMENT 'Stable request-type GUID from waitlist-request-types.json id (== RequestTypeInventory.StableId).',
    request_type VARCHAR(64) NOT NULL COMMENT 'Request type display name (e.g. Pickup, Coil, Forklift Assist).',
    control VARCHAR(255) NOT NULL COMMENT 'WinUI control full type name rendered for this request type.',
    flow VARCHAR(64) NOT NULL DEFAULT 'direct-to-confirmation' COMMENT 'Wizard flow for the request type (e.g. collect-input-then-confirm).',
    requires_text_input TINYINT(1) NOT NULL DEFAULT 0 COMMENT 'Whether the request type requires a free-text input step.',
    prompt_text VARCHAR(500) NULL COMMENT 'Prompt shown for the required text input.',
    min_length INT NOT NULL DEFAULT 0 COMMENT 'Minimum text-input length.',
    max_length INT NOT NULL DEFAULT 200 COMMENT 'Maximum text-input length.',
    default_image_path VARCHAR(500) NULL COMMENT 'Optional image path from JSON imagePath (usually null; images resolve via ImageLocationService).',
    center_data_grid_fields_json JSON NULL COMMENT 'Ordered center data-grid column labels (centerDataGridFields).',
    is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Whether the request type is offered in the wizard.',
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_waitlist_request_types_public_id (public_id),
    UNIQUE KEY uq_waitlist_request_types_request_type (request_type)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;
