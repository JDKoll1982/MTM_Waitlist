-- Create table: waitlist_request_item_configs
-- Engine: MySQL 5.7
-- Purpose: One row per catalogued request Item: how far the flow goes, whether an answer is required, the
--          answer's type, the prompt, the length limits, the offered options, the fields the Item's page
--          shows, and the Item's allotted minutes. This is what lets each of those change as data instead of
--          as code (FR-013, FR-015). The Item's existence, Category, order, umbrella verb and Line 2 template
--          stay in the code catalog; this table carries behaviour only.
-- Source: specs/004-unified-card-item-picker/data-model.md section 3 (the column set, types, nulls and
--         defaults) and specs/004-unified-card-item-picker/contracts/item-configuration.md sections 2-3.
--         Authored at design time from the retiring type/subtype catalogs and the per-Item field
--         definitions; the application never reads a design document (FR-027).
-- Twin: uq_waitlist_request_item_configs_item is the single-row-per-Item guarantee FR-014 needs: an Item
--       either has exactly one configuration or none, and no configuration is a reportable state rather
--       than a silently merged one.
-- Audit: updated_by_user_id is the auditing column the repo's other update procedures carry, and this
--        table's one writer (sp_waitlist_request_item_allotted_minutes_update) records it. It is deliberately
--        NOT part of the read contract: sp_waitlist_request_item_configs_get returns the configuration the
--        application consumes, and who last changed a figure is not part of that.
-- Rows are seeded by Database/Seeds/seed_waitlist_request_item_configs (24 rows: one per catalogued Item).

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS waitlist_request_item_configs (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL COMMENT 'Public UUID for the configuration row.',
    item VARCHAR(64) NOT NULL COMMENT 'Catalog item code (e.g. pickup-coil, deliver-wrong-flatstock, other). One row per Item.',
    category VARCHAR(16) NOT NULL COMMENT 'The Category the Item belongs to: Pickup, Deliver, Assist or Other.',
    control_flow VARCHAR(64) NOT NULL DEFAULT 'direct-to-confirmation' COMMENT 'How far the flow goes before confirmation: direct-to-confirmation or collect-input-then-confirm.',
    requires_answer TINYINT(1) NOT NULL DEFAULT 0 COMMENT 'Whether the Details step asks for an answer for this Item.',
    answer_value_type VARCHAR(16) NULL COMMENT 'enum or text where an answer is required; NULL otherwise.',
    prompt_text VARCHAR(500) NULL COMMENT 'The question shown where an answer is required.',
    min_length INT NOT NULL DEFAULT 0 COMMENT 'Minimum length of a text answer; ignored where none is required.',
    max_length INT NOT NULL DEFAULT 200 COMMENT 'Maximum length of a text answer; ignored where none is required.',
    options_json JSON NULL COMMENT 'Ordered options for an enumerated answer, where the options are fixed by configuration.',
    detail_fields_json JSON NULL COMMENT 'The ordered fields the Item page shows: label, value_type, source, order, is_required.',
    allotted_minutes INT NULL COMMENT 'The Item configured allotment in minutes. NULL means not configured, and the application then uses the labelled 15-minute default.',
    updated_by_user_id BIGINT NULL COMMENT 'User who last changed this configuration row. Audit only; not part of the read contract.',
    created_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the configuration row was created.',
    updated_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the configuration row was last updated.',
    PRIMARY KEY (id),
    UNIQUE KEY uq_waitlist_request_item_configs_public_id (public_id),
    UNIQUE KEY uq_waitlist_request_item_configs_item (item),
    KEY idx_waitlist_request_item_configs_category (category),
    CONSTRAINT fk_waitlist_request_item_configs_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES core_users_profiles (id) ON DELETE SET NULL
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = 'One row per catalogued request Item: flow, answer requirement, prompt, limits, options, page fields and allotted minutes. Behaviour as data.';

SET FOREIGN_KEY_CHECKS = 1;
