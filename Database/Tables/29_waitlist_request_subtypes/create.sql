-- Create table: waitlist_request_subtypes
-- Engine: MySQL 5.7
-- Purpose: Real (non-mock) request-subtype/action catalog that drives the New-Request wizard. Each row
--          is one subtype (e.g. Pickup Coil, Bring, Wrong Coil @ press) belonging to a request type,
--          with its stable GUID, WinUI control type name, default flow/text-input validation rules,
--          image path, and ordered center data-grid column labels.
-- Source: Assets/Config/waitlist-request-types.json (subtypes[] objects).
--         The public_id is the stable GUID in the JSON 'id' field (== RequestSubtypeInventory.StableId).

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS waitlist_request_subtypes (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL COMMENT 'Stable subtype GUID from waitlist-request-types.json id (== RequestSubtypeInventory.StableId).',
    request_type_id BIGINT NOT NULL COMMENT 'Owning request type (waitlist_request_types.id).',
    subtype_name VARCHAR(128) NOT NULL COMMENT 'Subtype/action display name (e.g. Bring, Pickup Coil, Wrong Coil @ press).',
    control VARCHAR(255) NOT NULL COMMENT 'WinUI control full type name rendered for this subtype.',
    flow VARCHAR(64) NOT NULL DEFAULT 'direct-to-confirmation' COMMENT 'Wizard flow for the subtype (e.g. collect-input-then-confirm).',
    requires_text_input TINYINT(1) NOT NULL DEFAULT 0 COMMENT 'Whether the subtype requires a free-text input step.',
    prompt_text VARCHAR(500) NULL COMMENT 'Prompt shown for the required text input.',
    min_length INT NOT NULL DEFAULT 0 COMMENT 'Minimum text-input length.',
    max_length INT NOT NULL DEFAULT 200 COMMENT 'Maximum text-input length.',
    default_image_path VARCHAR(500) NULL COMMENT 'Optional image path from JSON imagePath (usually null; images resolve via ImageLocationService).',
    category VARCHAR(16) NULL COMMENT 'Canonical umbrella category (Pickup/Deliver/Assist/Other) this legacy subtype maps onto (leaf row).',
    item_id VARCHAR(64) NULL COMMENT 'Canonical item id from Request-Config-Template.csv (col 3) this legacy subtype maps onto (leaf row).',
    center_data_grid_fields_json JSON NULL COMMENT 'Ordered center data-grid column labels (centerDataGridFields).',
    is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Whether the subtype is offered in the wizard.',
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_waitlist_request_subtypes_public_id (public_id),
    UNIQUE KEY uq_waitlist_request_subtypes_type_name (request_type_id, subtype_name),
    KEY idx_waitlist_request_subtypes_request_type_id (request_type_id),
    CONSTRAINT fk_waitlist_request_subtypes_request_type_id FOREIGN KEY (request_type_id) REFERENCES waitlist_request_types (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;
