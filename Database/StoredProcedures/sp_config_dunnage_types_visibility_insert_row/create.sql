-- Stored Procedure: sp_config_dunnage_types_visibility_insert_row
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T095)
--
-- Purpose: write one dunnage type's visibility row as part of the map rewrite. Replaces one row of the inline
--          multi-row INSERT in `DunnageTypeVisibilityCatalogService.SaveVisibleDunnageTypesAsync` (FR-015).
--
-- Contract:
--   IN  p_dunnage_type_id    BIGINT          the receiving store's dunnage_types.id
--   IN  p_dunnage_type_name  VARCHAR(128)    denormalized name, kept so the map is readable without the other store
--   IN  p_is_visible         TINYINT(1)      1 visible, 0 hidden
--
-- NAMING NOTE — why this is `insert_row` and not the `insert_many` this task named
-- -----------------------------------------------------------------------------------
-- The caller writes one row per type in the receiving store's list (23 live). A single set-based statement needs
-- the whole set inside the routine, and MySQL 5.7 cannot iterate a collection without either a helper numbers
-- table or a delimited string. The delimited-string form is unsafe here because one of the three values per row
-- (`dunnage_type_name`) is free text that may contain the delimiter, and the alternative — having an
-- `mtm_waitlist` routine read `mtm_receiving_application.dunnage_types` directly — was rejected because this repo
-- keeps the four stores separately configurable, so a cross-store read inside one store's procedure would break
-- the moment they are not co-located. The caller therefore calls this once per row; the sequence of calls is the
-- same delete-then-write shape it had before, so no atomicity was lost relative to the statement it replaces.
--
-- `public_id`, `created_utc` and `updated_utc` are generated here rather than passed in, because the caller
-- never chose them: it let the database default them, and passing NULL from C# for a NOT NULL column would fail.
-- The two `*_by_user_id` columns stay NULL, exactly as the statement this replaces wrote them.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_dunnage_types_visibility_insert_row;

CREATE PROCEDURE sp_config_dunnage_types_visibility_insert_row(
    IN p_dunnage_type_id BIGINT,
    IN p_dunnage_type_name VARCHAR(128),
    IN p_is_visible TINYINT
)
INSERT INTO config_dunnage_types_visibility (
    public_id,
    dunnage_type_id,
    dunnage_type_name,
    is_visible,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
)
VALUES (
    UUID(),
    p_dunnage_type_id,
    p_dunnage_type_name,
    p_is_visible,
    NULL,
    NULL,
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
);
