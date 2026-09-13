-- Create procedure: sp_waitlist_request_item_configs_get
-- Engine: MySQL 5.7
-- Purpose: Return one row per configured Item: the behaviour the application reads once, as the wizard's
--          Item step is entered (never per keystroke and never per row render).
-- Contract: specs/004-unified-card-item-picker/contracts/item-configuration.md section 2.
--           No parameters. Every column of waitlist_request_item_configs except id, and except the audit
--           column updated_by_user_id, which is not part of the read contract.
--           detail_fields_json and options_json are returned as JSON strings and parsed by the application:
--           the schema targets MySQL 5.7, where JSON_TABLE is unavailable.
--           A row whose item is not in the code catalog is dropped by the application, not here, because the
--           catalog lives in code.
-- Read-only: this procedure writes nothing and must not be reused to write anything.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_item_configs_get;

CREATE PROCEDURE sp_waitlist_request_item_configs_get()
SELECT
    public_id,
    item,
    category,
    control_flow,
    requires_answer,
    answer_value_type,
    prompt_text,
    min_length,
    max_length,
    options_json,
    detail_fields_json,
    allotted_minutes
FROM waitlist_request_item_configs
ORDER BY item ASC;
