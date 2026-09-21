-- Stored Procedure: sp_auth_computer_registered_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T093)
--
-- Purpose: answer "is this machine a registered workstation?" for the logon screen. Replaces the inline
--          `SELECT COUNT(1) ...` in `StartupSessionRepository.ReadComputerRegisteredAsync` (FR-015).
--
-- Contract:
--   IN  p_hostname_normalized       VARCHAR(255)   column width from core_computers_registry
--   IN  p_mac_address_normalized    VARCHAR(64)
--
--   OUT one row / one column: the number of matching registered rows (0 when there is none). The caller reads it
--       as a scalar and treats "greater than zero" as registered — the same shape the statement returned.
--
-- Both columns must match: a machine that presents a different MAC than the one it registered with is not the
-- registered machine. `is_registered = 1` is part of the test, so a retired machine answers "no" without being
-- deleted from the registry (which would lose its history).
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_computer_registered_get;

CREATE PROCEDURE sp_auth_computer_registered_get(
    IN p_hostname_normalized VARCHAR(255),
    IN p_mac_address_normalized VARCHAR(64)
)
SELECT COUNT(1) AS registered_count
FROM core_computers_registry
WHERE hostname_normalized = p_hostname_normalized
  AND mac_address_normalized = p_mac_address_normalized
  AND is_registered = 1;

-- Stored Procedure: sp_auth_credentials_check
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T093); extended by
--          006-user-management-and-permissions (task T008)
--
-- Purpose: fetch the credential material for one active user, so the caller can verify a supplied password.
--          Replaces the inline SELECT in `StartupSessionRepository.CheckCredentialsAsync` (FR-015).
--
-- Contract:
--   IN  p_username  VARCHAR(128)   the *normalized* (UPPER-case) username; column width from
--                                  core_users_profiles.username_normalized
--
--   OUT zero or one row:
--     id, role_code, role_name, password_hash, password_salt, require_password_change, display_name,
--     employee_identifier, temporary_credential_failed_attempts
--
-- Sign-in-name contract (corrected by T008): a sign-in name is stored and compared in UPPER case, so the
-- caller normalises to upper case before calling. The comment here previously said lower case; that was true
-- before this feature and is not any more (FR-002).
--
-- Semantics preserved exactly from the statement this replaces:
--   * `is_active = 1` only — a deactivated account cannot authenticate.
--   * the role comes from a LEFT JOIN, so a user with no assignment still returns a row with an empty role
--     rather than being treated as unknown;
--   * `ORDER BY ra.assigned_utc DESC LIMIT 1` — the newest assignment wins when a user holds several;
--   * `COALESCE(..., '')` on role/display name/employee id, so the caller never sees NULL for them.
--
-- Added by this feature, and read by ordinal, so the calling code's ordinals moved in the same change:
--   * `role_code` — the identity a gate compares. `role_name` is presentation and is never compared for
--     access (FR-054, FR-105).
--   * `temporary_credential_failed_attempts` — the wrong-attempt count against a temporary credential. It is
--     held with the account so it survives a restart (FR-037) and does not expire with time (FR-038).
--
-- The caller normalizes the username before calling (it applies `Trim().ToUpperInvariant()`); the procedure
-- does not repeat that, so a caller that passes a mixed-case name is not silently rescued into matching. That
-- asymmetry with `sp_auth_user_row_get` is pre-existing caller behaviour and was deliberately preserved.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_credentials_check;

CREATE PROCEDURE sp_auth_credentials_check(
    IN p_username VARCHAR(128)
)
SELECT u.id,
       COALESCE(r.role_code, '') AS role_code,
       COALESCE(r.role_name, '') AS role_name,
       u.password_hash,
       u.password_salt,
       u.require_password_change,
       COALESCE(u.display_name, '') AS display_name,
       COALESCE(u.employee_identifier, '') AS employee_identifier,
       u.temporary_credential_failed_attempts
FROM core_users_profiles u
LEFT JOIN auth_roles_assignments ra ON ra.user_id = u.id
LEFT JOIN auth_roles_catalog r ON r.id = ra.role_id
WHERE u.username_normalized = p_username
  AND u.is_active = 1
ORDER BY ra.assigned_utc DESC
LIMIT 1;

-- ============================================================
-- Stored Procedure: sp_auth_password_reset_required_get
-- Engine: MySQL 5.7
-- Purpose: Report whether one account still holds its temporary default password, so the application
--          can put the operator straight onto the "set a new password" surface at startup, BEFORE the
--          sign-in form is ever shown.
--
-- Contract:
--   IN  p_username  VARCHAR(128)   the username as the caller holds it, matched against
--                                  `username_normalized` exactly as `sp_auth_user_row_get` does
--
--   OUT zero or one row:
--       user_id, role_code, role_name, display_name, employee_identifier, password_reset_required
--
-- `password_reset_required` is 1 when either of the two signals the sign-in read already honours is
-- present: the stored password is the temporary marker (`'0000'`, or an empty/absent hash — see
-- `IsTemporaryDefaultPassword` in `StartupSessionRepository`), or the row's `require_password_change`
-- flag is set. A user who has set a real password has the flag cleared by
-- `sp_auth_user_password_update`, so this answers "has this account ever been signed in with?".
--
-- The verdict is deliberately NOT widened by this feature. Those two signals stay the whole rule (task T008),
-- which is what makes the legacy `0000` marker keep working and what makes FR-031 true.
--
-- `role_code` is the identity a gate compares; `role_name` is presentation and is never compared for access
-- (FR-054, FR-105).
--
-- Sign-in-name contract (corrected by T008): a sign-in name is stored and compared in UPPER case (FR-002), so
-- the caller normalises to upper case before calling.
--
-- No credential material is returned — only the 0/1 verdict — so the read is safe to call before any
-- authentication has happened. The row is the same one `sp_auth_user_row_get` returns, so the caller
-- gets the identity it needs to complete the reset without a second lookup.
--
-- Returns zero rows for an unknown or inactive user, which the caller treats as "no prompt".
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_password_reset_required_get;

CREATE PROCEDURE sp_auth_password_reset_required_get(
    IN p_username VARCHAR(128)
)
SELECT u.id AS user_id,
       COALESCE(r.role_code, '') AS role_code,
       COALESCE(r.role_name, '') AS role_name,
       COALESCE(u.display_name, '') AS display_name,
       COALESCE(u.employee_identifier, '') AS employee_identifier,
       CASE
           WHEN u.require_password_change = 1 THEN 1
           WHEN COALESCE(u.password_hash, '') = '' THEN 1
           WHEN u.password_hash = '0000' THEN 1
           ELSE 0
       END AS password_reset_required
FROM core_users_profiles u
LEFT JOIN auth_roles_assignments ra ON ra.user_id = u.id
LEFT JOIN auth_roles_catalog r ON r.id = ra.role_id
WHERE u.username_normalized = p_username
  AND u.is_active = 1
ORDER BY ra.assigned_utc DESC
LIMIT 1;

-- Stored Procedure: sp_auth_roles_list
-- Engine: MySQL 5.7
-- Feature: 006-user-management-and-permissions (task T028)
--
-- Purpose: the one read behind the role picker, the ladder and the rank comparison.
--
-- Contract:
--   IN  none
--
--   OUT zero or more rows, ordered by rung descending then code ascending:
--     role_id, role_code, role_name, role_rank
--
-- This is the only read of the role catalogue in the application. Every rung the ladder compares comes from a row
-- this procedure returns, so the rank comparison and the roles-at-or-above list cannot disagree with each other
-- and there is no second place a rung is written down (FR-010).
--
-- The retired `admin` code is excluded. The catalogue row still exists until the rename seed removes it, and a
-- picker that offered it would offer a role nothing can be assigned by the time this feature ships. Filtering on
-- the code rather than on a rank keeps the read correct whether the retired row carries the column default or a
-- value: the exclusion is about the code, not about the number beside it.
--
-- Ordering is `role_rank DESC, role_code ASC`. The code breaks a tie so peers at one rung (the three worker
-- roles) come back in a stable order rather than in whatever order the engine chooses.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_roles_list;

CREATE PROCEDURE sp_auth_roles_list()
SELECT r.id AS role_id,
       r.role_code,
       r.role_name,
       r.role_rank
FROM auth_roles_catalog r
WHERE r.role_code <> 'admin'
ORDER BY r.role_rank DESC, r.role_code ASC;

-- Stored Procedure: sp_auth_session_expiry_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T093)
--
-- Purpose: report the expiry of a user's current database session, if one exists. Replaces the inline SELECT in
--          `StartupSessionRepository.ReadSessionExpiryUtcAsync` (FR-015).
--
-- Contract:
--   IN  p_user_id  BIGINT
--   OUT zero or one row / one column: `expires_utc` of the newest live session, or no row when the user has none
--       (which the caller reads as "no database session" rather than as an error).
--
-- "Live" means exactly what the statement this replaces meant: `is_active = 1` and `revoked_utc IS NULL`. A
-- revoked or deactivated token must not be reported as a valid session, so neither predicate is dropped.
-- `ORDER BY expires_utc DESC LIMIT 1` keeps the caller's reading: the session that lasts longest wins when more
-- than one is live.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_session_expiry_get;

CREATE PROCEDURE sp_auth_session_expiry_get(
    IN p_user_id BIGINT
)
SELECT expires_utc
FROM auth_sessions_tokens
WHERE user_id = p_user_id
  AND is_active = 1
  AND revoked_utc IS NULL
ORDER BY expires_utc DESC
LIMIT 1;

-- Stored Procedure: sp_auth_temporary_credential_attempt_record
-- Engine: MySQL 5.7
-- Feature: 006-user-management-and-permissions (task T009)
--
-- Purpose: record the outcome of one sign-in attempt made against a temporary credential, so the account's
--          wrong-attempt count goes up on a failure and back to zero on a success.
--
-- Contract:
--   IN  p_user_id          BIGINT   the account the attempt was made against
--   IN  p_was_successful   TINYINT  1 when the attempt succeeded, 0 when it failed
--   OUT the affected-row count, through the non-query seam
--
-- The count is held with the account, which is what makes it survive closing and reopening the application
-- (FR-037) and what makes it not expire with time (FR-038). Nothing here expires it and no other procedure
-- clears it except a fresh reset (sp_user_management_reset_password), so FR-039 has exactly two writers and
-- both are named.
--
-- Only the one column is written. The procedure deliberately does not touch `updated_utc`: this is a
-- credential-state counter, not a profile change, and moving the profile's updated timestamp would make the
-- account look edited when it was not.
--
-- A non-query on purpose, so a caller that cannot tell an unknown account from a recorded attempt can read
-- the affected-row count and decide. An unknown `p_user_id` affects no rows and is not an error.
--
-- FR-043: the count is a state, not a log. Nothing reads it back as a record of who tried.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_temporary_credential_attempt_record;

-- DELIMITER is required here and in every other compound body in this feature. The mysql client splits a script
-- on `;` unless told otherwise, so a BEGIN … END body arrives as fragments and the CREATE fails with 1064. No
-- procedure in this repo needed it before, because every one of them is a single statement.
DELIMITER $$

CREATE PROCEDURE sp_auth_temporary_credential_attempt_record(
    IN p_user_id BIGINT,
    IN p_was_successful TINYINT
)
BEGIN
    IF p_was_successful = 1 THEN
        UPDATE core_users_profiles
        SET temporary_credential_failed_attempts = 0
        WHERE id = p_user_id;
    ELSE
        UPDATE core_users_profiles
        SET temporary_credential_failed_attempts = temporary_credential_failed_attempts + 1
        WHERE id = p_user_id;
    END IF;
END$$

DELIMITER ;

-- Stored Procedure: sp_auth_user_password_update
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T093)
--
-- Purpose: store a new password hash/salt for one active user and clear the forced-change flag. Replaces the
--          inline UPDATE in `StartupSessionRepository.UpdatePasswordAsync` (FR-015).
--
-- Contract:
--   IN  p_user_id        BIGINT
--   IN  p_password_hash  VARCHAR(128)     hex-encoded PBKDF2 hash (column width from core_users_profiles)
--   IN  p_password_salt  VARBINARY(32)    the per-user salt
--
--   OUT no result set. The affected-row count reaches the caller from the client (MySqlConnector's non-query
--       return value); 0 means the user was unknown or no longer active, which the caller reports as failure.
--
-- Semantics preserved exactly: the `is_active = 1` guard stays, `require_password_change` is cleared to 0 because
-- the change has just happened, and `updated_utc` is stamped from the server clock. The salt is regenerated by
-- the caller per change; the procedure never invents one, so an unset salt cannot slip through.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_user_password_update;

CREATE PROCEDURE sp_auth_user_password_update(
    IN p_user_id BIGINT,
    IN p_password_hash VARCHAR(128),
    IN p_password_salt VARBINARY(32)
)
UPDATE core_users_profiles
SET password_hash = p_password_hash,
    password_salt = p_password_salt,
    require_password_change = 0,
    updated_utc = UTC_TIMESTAMP()
WHERE id = p_user_id
  AND is_active = 1;

-- Stored Procedure: sp_auth_user_row_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T093); extended by
--          006-user-management-and-permissions (task T008)
--
-- Purpose: resolve one active user's identity and role at logon. Replaces the inline SELECT in
--          `StartupSessionRepository.ReadUserRowAsync` (FR-015).
--
-- Contract:
--   IN  p_username  VARCHAR(128)   the username as the caller holds it
--
--   OUT zero or one row: id, role_code, role_name, display_name, employee_identifier
--
-- Identical to `sp_auth_credentials_check` minus the credential columns: this read is used for identity and
-- role only, so it never pulls a password hash into memory for a logon that is not checking a password.
--
-- `role_code` is the identity a gate compares; `role_name` is presentation and is never compared for access
-- (FR-054, FR-105). Both callers of this read need the code.
--
-- Sign-in-name contract (corrected by T008): a sign-in name is stored and compared in UPPER case (FR-002). The
-- asymmetry noted here before — that `sp_auth_credentials_check`'s caller normalised and this one's did not —
-- is still pre-existing caller behaviour, but the direction of that normalisation is now upper case.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_user_row_get;

CREATE PROCEDURE sp_auth_user_row_get(
    IN p_username VARCHAR(128)
)
SELECT u.id,
       COALESCE(r.role_code, '') AS role_code,
       COALESCE(r.role_name, '') AS role_name,
       COALESCE(u.display_name, '') AS display_name,
       COALESCE(u.employee_identifier, '') AS employee_identifier
FROM core_users_profiles u
LEFT JOIN auth_roles_assignments ra ON ra.user_id = u.id
LEFT JOIN auth_roles_catalog r ON r.id = ra.role_id
WHERE u.username_normalized = p_username
  AND u.is_active = 1
ORDER BY ra.assigned_utc DESC
LIMIT 1;

-- Stored Procedure: sp_config_dunnage_types_visibility_delete_all
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T095)
--
-- Purpose: clear the whole visibility map before it is rewritten. Replaces the inline
--          `DELETE FROM config_dunnage_types_visibility;` in
--          `DunnageTypeVisibilityCatalogService.SaveVisibleDunnageTypesAsync` (FR-015).
--
-- Contract:
--   OUT no result set. The affected-row count reaches the caller from the client if it wants it; the caller
--       only cares that the table is empty afterwards.
--
-- This is a whole-table delete because the map is a *set*, not a collection of records: the caller rewrites it
-- from the receiving store's current type list, so a type that no longer exists in the receiving store must not
-- survive as a stale visibility row. That is also why no `WHERE` clause is offered here — a filtered delete
-- would have to know which rows the caller is about to write.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_dunnage_types_visibility_delete_all;

CREATE PROCEDURE sp_config_dunnage_types_visibility_delete_all()
DELETE FROM config_dunnage_types_visibility;

-- Stored Procedure: sp_config_dunnage_types_visibility_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T095)
--
-- Purpose: the persisted dunnage-type visibility map, so the catalog can split types into visible and hidden.
--          Replaces the inline SELECT in `DunnageTypeVisibilityCatalogService.GetVisibilityMapAsync` (FR-015).
--
-- Contract:
--   OUT one row per stored type: dunnage_type_id, is_visible.
--
-- No filter and no ordering, exactly like the statement this replaces: the caller folds the rows into a
-- dictionary, and a type with no row is visible by default (the service treats a missing entry as visible).
-- Adding an `is_visible = 1` filter here would silently change that default for every stored-hidden type.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_dunnage_types_visibility_get;

CREATE PROCEDURE sp_config_dunnage_types_visibility_get()
SELECT dunnage_type_id, is_visible
FROM config_dunnage_types_visibility;

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

-- Stored Procedure: sp_config_hot_workcenters_delete_for_computer
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_hot_workcenters_delete_for_computer;

CREATE PROCEDURE sp_config_hot_workcenters_delete_for_computer(
    IN p_computer_id BIGINT
)
DELETE FROM config_computer_hot_work_centers
WHERE computer_id = p_computer_id;

-- Stored Procedure: sp_config_hot_workcenters_get_for_computer
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_hot_workcenters_get_for_computer;

CREATE PROCEDURE sp_config_hot_workcenters_get_for_computer(
    IN p_computer_name VARCHAR(128)
)
SELECT
    swc.work_center_name AS work_center_name,
    cwhc.sort_rank
FROM config_computer_hot_work_centers cwhc
INNER JOIN core_computers_registry cwr ON cwr.id = cwhc.computer_id
INNER JOIN setup_work_centers_catalog swc ON swc.id = cwhc.work_center_id
WHERE cwhc.is_active = 1
  AND swc.is_active = 1
  AND (
        cwr.computer_name = TRIM(p_computer_name)
        OR cwr.hostname_normalized = TRIM(p_computer_name)
      )
ORDER BY cwhc.sort_rank ASC, swc.work_center_name ASC;

-- Stored Procedure: sp_config_hot_workcenters_upsert
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_hot_workcenters_upsert;

CREATE PROCEDURE sp_config_hot_workcenters_upsert(
    IN p_computer_id BIGINT,
    IN p_work_center_id BIGINT,
    IN p_sort_rank INT,
    IN p_modified_by_user_id BIGINT
)
INSERT INTO config_computer_hot_work_centers (
    computer_id,
    work_center_id,
    public_id,
    sort_rank,
    is_active,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
)
VALUES (
    p_computer_id,
    p_work_center_id,
    UUID(),
    p_sort_rank,
    1,
    p_modified_by_user_id,
    p_modified_by_user_id,
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
)
ON DUPLICATE KEY UPDATE
    sort_rank = VALUES(sort_rank),
    is_active = 1,
    updated_by_user_id = VALUES(updated_by_user_id),
    updated_utc = UTC_TIMESTAMP();

-- Stored Procedure: sp_config_images_locations_count_active_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
--
-- Purpose: how many active image overrides exist in total. Replaces the inline `SELECT COUNT(*)` in
--          `ImageOverrideReadService.CountAllActiveOverridesAsync` (FR-015).
--
-- Contract:
--   OUT one row / one column: `count`. The caller reads it by column name, so the alias is part of the contract.
--
-- Unfiltered apart from `is_active = 1`, exactly like the statement it replaces: the count drives the Settings
-- summary line ("N images overridden"), which counts live overrides only.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_count_active_get;

CREATE PROCEDURE sp_config_images_locations_count_active_get()
SELECT COUNT(*) as count
FROM config_images_locations
WHERE is_active = 1;

-- Stored Procedure: sp_config_images_locations_count_by_scope_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
--
-- Purpose: how many active overrides one scope holds. Replaces the inline `SELECT COUNT(*)` in
--          `ImageOverrideReadService.CountActiveOverridesByScopeAsync` (FR-015).
--
-- Contract:
--   IN  p_scope  VARCHAR(16)
--   OUT one row / one column: `count` — the alias the caller reads by name.
--
-- Same predicate as the statement it replaces (`scope` + `is_active = 1`), so the per-scope counters in the
-- Settings UI keep the values they had.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_count_by_scope_get;

CREATE PROCEDURE sp_config_images_locations_count_by_scope_get(
    IN p_scope VARCHAR(16)
)
SELECT COUNT(*) as count
FROM config_images_locations
WHERE scope = p_scope
  AND is_active = 1;

-- Stored Procedure: sp_config_images_locations_deactivate_for_scope
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
--
-- Purpose: withdraw every live override in one scope at once. Replaces the inline UPDATE in
--          `ImageOverrideWriteService.DeactivateAllForScopeAsync` (FR-015).
--
-- Contract:
--   IN  p_scope    VARCHAR(16)
--   IN  p_user_id  BIGINT     NULL when the caller has no user
--   OUT affected row count — the number of overrides deactivated, which the caller returns to the UI.
--
-- **The return value is why this site had to change.** The caller previously ran this UPDATE through the
-- row-returning query helper and returned `rows.Count`; an UPDATE returns no rows, so the count was always 0 and
-- the UI reported "0 deactivated" however many overrides were withdrawn. The non-query path carries the real
-- count. Because the WHERE clause matches only `is_active = 1` rows and changes every matched row, the count is
-- the same as the matched-row count — no `UseAffectedRows` subtlety to worry about here.
--
-- Soft delete, like the single-row withdraw: this is a bulk version of `sp_config_images_locations_delete`, not a
-- delete, which is why it does not share `sp_config_images_locations_purge_inactive`'s hard-delete behaviour.
-- `AND is_active = 1` is kept from the statement this replaces, so an already-withdrawn override is not counted.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_deactivate_for_scope;

CREATE PROCEDURE sp_config_images_locations_deactivate_for_scope(
    IN p_scope VARCHAR(16),
    IN p_user_id BIGINT
)
UPDATE config_images_locations
SET is_active = 0,
    updated_by_user_id = p_user_id,
    updated_utc = UTC_TIMESTAMP()
WHERE scope = p_scope
  AND is_active = 1;

-- Stored Procedure: sp_config_images_locations_delete
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
--
-- Purpose: withdraw the override for one (scope, scope_item_id) pair. Replaces the inline UPDATE in
--          `ImageOverrideWriteService.DeleteOverrideAsync` (FR-015). This is a soft delete: the row survives
--          because `uq_config_images_locations_scope_item` spans the pair regardless of `is_active`, so a hard
--          delete would let the next create re-use the pair while an audit trail of the old image is lost.
--
-- Contract:
--   IN  p_scope          VARCHAR(16)
--   IN  p_scope_item_id  VARCHAR(190)
--   IN  p_user_id        BIGINT        NULL when the caller has no user
--   OUT affected row count; 0 is the caller's NOT_FOUND answer, exactly as before.
--
-- `AND is_active = 1` is kept from the statement this replaces: withdrawing an already-withdrawn override is not
-- a state change, so it must not report success. That also means the returned count is a genuine "did something
-- change" signal rather than "did a row match".
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_delete;

CREATE PROCEDURE sp_config_images_locations_delete(
    IN p_scope VARCHAR(16),
    IN p_scope_item_id VARCHAR(190),
    IN p_user_id BIGINT
)
UPDATE config_images_locations
SET is_active = 0,
    updated_by_user_id = p_user_id,
    updated_utc = UTC_TIMESTAMP()
WHERE scope = p_scope
  AND scope_item_id = p_scope_item_id
  AND is_active = 1;

-- Stored Procedure: sp_config_images_locations_delete_by_public_id
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
--
-- Purpose: withdraw one override addressed by its public identifier. Replaces the inline UPDATE in
--          `ImageOverrideWriteService.DeleteByPublicIdAsync` (FR-015). Soft delete, for the same reason as
--          `sp_config_images_locations_delete`.
--
-- Contract:
--   IN  p_public_id  CHAR(36)
--   IN  p_user_id    BIGINT     NULL when the caller has no user
--   OUT affected row count; 0 is the caller's NOT_FOUND answer.
--
-- **The return value is the whole reason this site had to change.** The caller previously ran this UPDATE through
-- the row-returning query helper and decided success from `rows.Count > 0`. An UPDATE returns no rows, so that
-- count was always 0 and the method reported NOT_FOUND for every call — including the ones where the row really
-- was withdrawn. Routing through the non-query helper is what makes the affected-row count available; the
-- predicate itself (`public_id` + `is_active = 1`) is unchanged.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_delete_by_public_id;

CREATE PROCEDURE sp_config_images_locations_delete_by_public_id(
    IN p_public_id CHAR(36),
    IN p_user_id BIGINT
)
UPDATE config_images_locations
SET is_active = 0,
    updated_by_user_id = p_user_id,
    updated_utc = UTC_TIMESTAMP()
WHERE public_id = p_public_id
  AND is_active = 1;

-- Stored Procedure: sp_config_images_locations_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
--
-- Purpose: the active image override for one exact (scope, scope_item_id) pair. Replaces the inline SELECT in
--          `ImageOverrideReadService.GetOverrideAsync` (FR-015).
--
-- Contract:
--   IN  p_scope          VARCHAR(16)    column width from config_images_locations
--   IN  p_scope_item_id  VARCHAR(190)
--   OUT zero or one row, the ten columns the caller maps (id, public_id, scope, scope_item_id, image_path,
--       is_active, created_by_user_id, updated_by_user_id, created_utc, updated_utc)
--
-- `is_active = 1` is part of the identity, not a filter the caller could add later: the override table is
-- soft-deleted, so a row whose `is_active` is 0 is a withdrawn override and must never be returned here. No row
-- is the caller's normal "no override" answer, which is why the procedure returns nothing rather than a null row.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_get;

CREATE PROCEDURE sp_config_images_locations_get(
    IN p_scope VARCHAR(16),
    IN p_scope_item_id VARCHAR(190)
)
SELECT
    id,
    public_id,
    scope,
    scope_item_id,
    image_path,
    is_active,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
FROM config_images_locations
WHERE scope = p_scope
  AND scope_item_id = p_scope_item_id
  AND is_active = 1
LIMIT 1;

-- Stored Procedure: sp_config_images_locations_get_all
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
--
-- Purpose: every active override, whatever its scope. Replaces the inline SELECT in
--          `ImageOverrideReadService.DetectOrphanedOverridesAsync` (FR-015).
--
-- Contract:
--   OUT one row per active override (the same ten columns). No ordering, deliberately: the caller checks each row
--       against its scope's own source table and collects the orphans, so the order it receives is irrelevant and
--       adding one here would only be a difference to explain later.
--
-- The caller is the orphan detector, which is why the active filter is the same `is_active = 1` the statement used
-- and why nothing else is narrowed: an orphan is exactly an active override whose scope item no longer exists.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_get_all;

CREATE PROCEDURE sp_config_images_locations_get_all()
SELECT
    id,
    public_id,
    scope,
    scope_item_id,
    image_path,
    is_active,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
FROM config_images_locations
WHERE is_active = 1;

-- Stored Procedure: sp_config_images_locations_get_by_public_id
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
--
-- Purpose: one active override, addressed by its public identifier. Replaces the inline SELECT in
--          `ImageOverrideReadService.GetOverrideByPublicIdAsync` (FR-015).
--
-- Contract:
--   IN  p_public_id  CHAR(36)     column width from config_images_locations.public_id
--   OUT zero or one row (the same ten columns); no row is the caller's "not found" answer.
--
-- `is_active = 1` matches the statement this replaces: a withdrawn override is not addressable by its public id,
-- and the Settings deep-link that uses this read must not resurrect one.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_get_by_public_id;

CREATE PROCEDURE sp_config_images_locations_get_by_public_id(
    IN p_public_id CHAR(36)
)
SELECT
    id,
    public_id,
    scope,
    scope_item_id,
    image_path,
    is_active,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
FROM config_images_locations
WHERE public_id = p_public_id
  AND is_active = 1
LIMIT 1;

-- Stored Procedure: sp_config_images_locations_get_by_scope
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
--
-- Purpose: every active override in one scope, newest first. Replaces the inline SELECT in
--          `ImageOverrideReadService.GetOverridesByScopeAsync` (FR-015).
--
-- Contract:
--   IN  p_scope  VARCHAR(16)
--   OUT one row per active override (same ten columns), ordered `updated_utc DESC` — the order the statement this
--       replaces used, which the Settings list depends on for "most recently changed first".
--
-- Same `is_active = 1` reasoning as `sp_config_images_locations_get`: withdrawn overrides are not part of the
-- scope's live set.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_get_by_scope;

CREATE PROCEDURE sp_config_images_locations_get_by_scope(
    IN p_scope VARCHAR(16)
)
SELECT
    id,
    public_id,
    scope,
    scope_item_id,
    image_path,
    is_active,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
FROM config_images_locations
WHERE scope = p_scope
  AND is_active = 1
ORDER BY updated_utc DESC;

-- Stored Procedure: sp_config_images_locations_insert
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
--
-- Purpose: create one image override. Replaces the inline INSERT in
--          `ImageOverrideWriteService.CreateOverrideAsync` (FR-015).
--
-- Contract:
--   IN  p_public_id      CHAR(36)      generated by the caller, which reads the row back by this value
--   IN  p_scope          VARCHAR(16)
--   IN  p_scope_item_id  VARCHAR(190)
--   IN  p_image_path     VARCHAR(500)
--   IN  p_user_id        BIGINT        NULL when the caller has no user
--   OUT affected row count (0 or 1); the caller treats anything but 1 as a database error, and MySQL's duplicate
--       key error for `uq_config_images_locations_scope_item` still surfaces as exception number 1062.
--
-- Field values the caller does not choose are generated here, matching the statement this replaces:
-- `is_active` is 1 because this is the create path, and both timestamps are `UTC_TIMESTAMP()` so the pair cannot
-- disagree. Both `*_by_user_id` columns take the same parameter — create and update are the same actor.
--
-- Parameter widths are the live column widths, so an over-long value fails here rather than being silently
-- truncated by the server.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_insert;

CREATE PROCEDURE sp_config_images_locations_insert(
    IN p_public_id CHAR(36),
    IN p_scope VARCHAR(16),
    IN p_scope_item_id VARCHAR(190),
    IN p_image_path VARCHAR(500),
    IN p_user_id BIGINT
)
INSERT INTO config_images_locations (
    public_id,
    scope,
    scope_item_id,
    image_path,
    is_active,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
)
VALUES (
    p_public_id,
    p_scope,
    p_scope_item_id,
    p_image_path,
    1,
    p_user_id,
    p_user_id,
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
);

-- Stored Procedure: sp_config_images_locations_purge_inactive
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
--
-- Purpose: permanently remove every withdrawn override. Replaces the inline `DELETE FROM … WHERE is_active = 0`
--          in `ImageOverrideWriteService.PurgeInactiveOverridesAsync` (FR-015). This is the only hard delete on
--          the table, and it is the deliberate escape hatch for the soft-delete design
--          (`sp_config_images_locations_delete` keeps the row; this is how an operator ultimately reclaims it).
--
-- Contract:
--   OUT affected row count — the number of rows actually purged. The caller returns this count to the UI, which
--       reports it, so a wrong number is a user-visible lie.
--
-- **The return value is why this site had to change.** The caller previously ran this DELETE through the
-- row-returning query helper and returned `rows.Count`. A DELETE returns no rows, so the count was always 0 —
-- the purge worked, but every caller was told nothing had been purged. The non-query path is what carries the
-- real affected-row count.
--
-- Unconditional apart from `is_active = 0`: no scope parameter, because purging is a whole-table maintenance
-- operation, and that matches the statement this replaces.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_purge_inactive;

CREATE PROCEDURE sp_config_images_locations_purge_inactive()
DELETE FROM config_images_locations
WHERE is_active = 0;

-- Stored Procedure: sp_config_images_locations_reactivate
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
--
-- Purpose: bring a soft-deleted override back to life with a new image path. Replaces the inline UPDATE in
--          `ImageOverrideWriteService.ReactivateOverrideAsync` (FR-015).
--
-- Contract:
--   IN  p_scope          VARCHAR(16)
--   IN  p_scope_item_id  VARCHAR(190)
--   IN  p_image_path     VARCHAR(500)
--   IN  p_user_id        BIGINT        NULL when the caller has no user
--   OUT affected row count; the caller treats less than 1 as a failure, which is also what it did before.
--
-- **No `is_active` predicate in the WHERE, matching the statement this replaces.** It reads as an omission but it
-- is not: the caller only reaches this path after `sp_config_images_locations_status_get` reported an existing
-- row whose `is_active` is 0, and the unique key guarantees that pair has exactly one row, so the update is
-- already aimed at that one row. Adding `AND is_active = 0` would be a tighter guard, but it would make the
-- procedure fail if the row had been flipped to active between the probe and this call, turning a harmless race
-- into a reported error; the current form is idempotent in that case, which is why it was written this way.
--
-- `created_utc`/`created_by_user_id` are left alone — this is the same logical row, so its creation record stays.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_reactivate;

CREATE PROCEDURE sp_config_images_locations_reactivate(
    IN p_scope VARCHAR(16),
    IN p_scope_item_id VARCHAR(190),
    IN p_image_path VARCHAR(500),
    IN p_user_id BIGINT
)
UPDATE config_images_locations
SET image_path = p_image_path,
    is_active = 1,
    updated_by_user_id = p_user_id,
    updated_utc = UTC_TIMESTAMP()
WHERE scope = p_scope
  AND scope_item_id = p_scope_item_id;

-- Stored Procedure: sp_config_images_locations_recent_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
--
-- Purpose: the most recently updated overrides, capped by the caller. Replaces the inline SELECT in
--          `ImageOverrideReadService.GetRecentlyUpdatedOverridesAsync` (FR-015).
--
-- Contract:
--   IN  p_max_rows  INT     how many rows to return
--   OUT up to `p_max_rows` rows (the same ten columns), ordered `updated_utc DESC`.
--
-- **This is the site whose `LIMIT` used to be interpolated straight into the statement text**
-- (`LIMIT {maxRecordCount}`), so it is the one read that genuinely needed a parameter. MySQL accepts an
-- integer-valued routine parameter directly in `LIMIT`, so the cap binds as `p_max_rows`.
--
-- Verified live on 5.7.24, because the accepted forms are narrower than they look: a routine parameter in `LIMIT`
-- works, and any expression does not — `LIMIT GREATEST(p,1)` is rejected with `ERROR 1327 Undeclared variable:
-- GREATEST`. A `BEGIN … END` body would be needed to clamp into a local variable first, and no other artifact in
-- this repo uses one (the client splits a routine body on `;` without a `DELIMITER` directive), so the clamp is
-- not applied here. `LIMIT 0` is legal and returns no rows; only `LIMIT NULL` errors, which the caller's
-- `maxRecordCount < 1` guard (`ArgumentException`) already prevents.
--
-- No `is_active` filter, deliberately, matching the statement this replaces: "recently updated" is an audit view
-- over the table and must be able to show a withdrawn override — that is how an operator sees what was
-- deactivated. Adding the filter here would silently hide those rows.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_recent_get;

CREATE PROCEDURE sp_config_images_locations_recent_get(
    IN p_max_rows INT
)
SELECT
    id,
    public_id,
    scope,
    scope_item_id,
    image_path,
    is_active,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
FROM config_images_locations
ORDER BY updated_utc DESC
LIMIT p_max_rows;

-- Stored Procedure: sp_config_images_locations_status_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
--
-- Purpose: the `is_active` state of one (scope, scope_item_id) pair, whether or not the override is live.
--          Replaces the inline `SELECT is_active … LIMIT 1` in `ImageOverrideWriteService.CreateOverrideAsync`
--          (FR-015).
--
-- Contract:
--   IN  p_scope          VARCHAR(16)
--   IN  p_scope_item_id  VARCHAR(190)
--   OUT zero or one row / one column: `is_active`. No row means the pair has never existed.
--
-- **There is deliberately no `is_active` filter here, and that is the whole point of the procedure.** The caller
-- has three distinct cases to tell apart before it can write — (a) no row, so INSERT; (b) an active row, so
-- report DUPLICATE_KEY; (c) an inactive row, which must be reactivated rather than re-inserted because the
-- unique key `uq_config_images_locations_scope_item` spans (scope, scope_item_id) regardless of `is_active`.
-- A filter here would collapse (b) and (c) into the same "no row" answer and turn case (c) into a duplicate-key
-- error at INSERT time. Every other read of this table filters to active rows; this one must not.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_status_get;

CREATE PROCEDURE sp_config_images_locations_status_get(
    IN p_scope VARCHAR(16),
    IN p_scope_item_id VARCHAR(190)
)
SELECT is_active
FROM config_images_locations
WHERE scope = p_scope
  AND scope_item_id = p_scope_item_id
LIMIT 1;

-- Stored Procedure: sp_config_images_locations_update
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
--
-- Purpose: repoint one live override at a different image. Replaces the inline UPDATE in
--          `ImageOverrideWriteService.UpdateOverrideAsync` (FR-015).
--
-- Contract:
--   IN  p_scope          VARCHAR(16)
--   IN  p_scope_item_id  VARCHAR(190)
--   IN  p_image_path     VARCHAR(500)
--   IN  p_user_id        BIGINT        NULL when the caller has no user
--   OUT affected row count.
--
-- `AND is_active = 1` is kept from the statement this replaces: an update is only meaningful for a live override,
-- and the caller has already reported NOT_FOUND when it could not read one. `created_*` columns are untouched —
-- repointing an image is not a new row.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_update;

CREATE PROCEDURE sp_config_images_locations_update(
    IN p_scope VARCHAR(16),
    IN p_scope_item_id VARCHAR(190),
    IN p_image_path VARCHAR(500),
    IN p_user_id BIGINT
)
UPDATE config_images_locations
SET image_path = p_image_path,
    updated_by_user_id = p_user_id,
    updated_utc = UTC_TIMESTAMP()
WHERE scope = p_scope
  AND scope_item_id = p_scope_item_id
  AND is_active = 1;

-- Stored Procedure: sp_config_permissions_feature_baseline_get
-- Engine: MySQL 5.7
-- Feature: 006-user-management-and-permissions (task T060)
--
-- Purpose: the roles whose baseline gives one feature, read from the seeded baselines (FR-075).
--
-- Contract:
--   IN  p_setting_key VARCHAR(190)  a key in the permission namespace
--
--   OUT zero or more rows: role_code, role_name, setting_value_bool
--
-- WHY THIS READ EXISTS RATHER THAN A SECOND COPY BESIDE THE VIEW. The answer to "which roles' baselines give this"
-- is the seeded data, and the declaration's two-direction check ties the declaration to that same data (FR-046,
-- FR-047, FR-048). Restating the answer beside the view would be a third copy, and a third copy is what drifts.
-- The view therefore asks the store what the baselines say.
--
-- Only `role`-scoped rows are returned. A person's own row is not a baseline, and it is the OTHER query on this
-- page — the one that names the people who differ — that answers about people.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_permissions_feature_baseline_get;

CREATE PROCEDURE sp_config_permissions_feature_baseline_get(
    IN p_setting_key VARCHAR(190)
)
SELECT r.role_code,
       r.role_name,
       v.setting_value_bool
FROM config_settings_values v
INNER JOIN auth_roles_catalog r
        ON v.scope_key = CONCAT('role:', r.role_code)
WHERE v.setting_key = p_setting_key
  AND v.scope_type = 'role'
  AND v.value_type = 'bool'
  AND v.setting_value_bool = 1
ORDER BY r.role_rank DESC, r.role_code ASC;

-- Stored Procedure: sp_config_permissions_feature_holders_get
-- Engine: MySQL 5.7
-- Feature: 006-user-management-and-permissions (task T054)
--
-- Purpose: answer "who holds this feature", reading only the people who DIFFER from their role's baseline, so the
--          second view on the permissions page reads the few people who were chosen for rather than every person
--          in the store (FR-076).
--
-- Contract:
--   IN  p_setting_key VARCHAR(190)  a key in the permission namespace
--
--   OUT zero or more rows: user_id, display_name, employee_identifier, role_code, is_active, setting_value_bool
--
-- Read-only: nothing here writes, and there is no counterpart write. The view it serves changes nothing, and
-- every change still happens on the person's own page (FR-077).
--
-- What it does NOT return is as important as what it does: the roles whose BASELINE gives the key are deliberately
-- absent. Those are read from the seeded baselines, which the declaration's two-direction check ties to the
-- declaration (FR-046, FR-047, FR-048), and restating them here would be a second copy of the answers that could
-- drift from the first. This procedure answers about people; the baselines answer about roles.
--
-- A person is listed when their own stored value differs from their role's baseline in EITHER direction, so a
-- person whose access was taken away is named exactly as one who was given more (FR-076). A person holding the
-- value but switched off is listed and carries `is_active` so the view can mark them (FR-079).
--
-- The person is matched on `scope_key` rather than on the `user_id` column. That is the keying the data model
-- pins for a person-scoped row, and the same choice sp_config_permissions_user_get documents: a row written with
-- the right `scope_key` and no `user_id` is invisible to a `user_id`-keyed join. That was a real failure seen by
-- running it.
--
-- A person whose role holds NO baseline row for the key is listed when they hold a stored value, because they
-- differ from what their role supplies. Their role supplies the shipped fallback in that case, which lives in the
-- declaration rather than in the store, so this read cannot compare against it; the shipped baselines cover every
-- role the catalogue holds, which is what makes that case theoretical rather than ordinary.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_permissions_feature_holders_get;

CREATE PROCEDURE sp_config_permissions_feature_holders_get(
    IN p_setting_key VARCHAR(190)
)
SELECT u.id AS user_id,
       COALESCE(u.display_name, '') AS display_name,
       COALESCE(u.employee_identifier, '') AS employee_identifier,
       COALESCE(hr.role_code, '') AS role_code,
       u.is_active,
       ov.setting_value_bool
FROM config_settings_values ov
INNER JOIN core_users_profiles u
        ON u.id = CAST(SUBSTRING(ov.scope_key, LENGTH('user:') + 1) AS UNSIGNED)
LEFT JOIN (
    SELECT ra.user_id,
           r.role_code,
           r.role_rank
    FROM auth_roles_assignments ra
    INNER JOIN auth_roles_catalog r ON r.id = ra.role_id
) hr ON hr.user_id = u.id
     AND hr.role_rank = (
         SELECT MAX(r2.role_rank)
         FROM auth_roles_assignments ra2
         INNER JOIN auth_roles_catalog r2 ON r2.id = ra2.role_id
         WHERE ra2.user_id = u.id
     )
LEFT JOIN config_settings_values rv
       ON rv.setting_key = ov.setting_key
      AND rv.scope_type = 'role'
      AND rv.scope_key = CONCAT('role:', COALESCE(hr.role_code, ''))
      AND rv.value_type = 'bool'
WHERE ov.setting_key = p_setting_key
  AND ov.scope_type = 'user'
  AND ov.scope_key LIKE 'user:%'
  AND ov.value_type = 'bool'
  AND (rv.setting_value_bool IS NULL OR IFNULL(ov.setting_value_bool, -1) <> IFNULL(rv.setting_value_bool, -1))
ORDER BY u.is_active DESC, u.display_name ASC;

-- Stored Procedure: sp_config_permissions_reversal_get
-- Engine: MySQL 5.7
-- Feature: 006-user-management-and-permissions (task T055)
--
-- Purpose: the recorded history of the most recent permission save for one person, so that save can be reversed
--          as a whole (FR-069, FR-071). This is a read of config_settings_history, which already exists; the
--          reversal adds no store of its own.
--
-- Contract:
--   IN  p_user_id BIGINT  the person whose last save is being reversed
--
--   OUT zero or more rows: setting_key, previous_setting_value_bool, changed_setting_value_bool,
--       baseline_setting_value_bool, changed_utc
--
-- The rows of one save are found by their exact `changed_utc`, which works because
-- sp_config_permissions_user_set reads the clock ONCE per call and stamps every row it writes with that one
-- value. Nothing about this read guesses a window: it takes the newest timestamp recorded against this person's
-- own scope, and returns every row carrying it.
--
-- The scope is `user:<id>` and nothing else. A settings change made through the ordinary settings path for the
-- same person is not a permission change, and reversing one must not reverse the other.
--
-- WHY THE BASELINE IS RETURNED. `previous_setting_value_bool` is NULL for a permission the person had no stored
-- choice about before the save, and the value then in force was their role's baseline. The reversal cannot restore
-- "no row" by deleting one, because this history table names the value row it changed and that row would be gone:
-- the foreign key from config_settings_history.config_setting_id to config_settings_values.id refused it when it
-- was tried. So the reversal writes the value that was in force instead, and it needs the baseline here to know
-- what that value was. What is restored is therefore the value, and the row that carries it is the person's own
-- rather than the role's: the page marks it as a choice for the person rather than as inherited, which is the one
-- observable difference and is recorded rather than hidden.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_permissions_reversal_get;

CREATE PROCEDURE sp_config_permissions_reversal_get(
    IN p_user_id BIGINT
)
SELECT h.setting_key,
       h.previous_setting_value_bool,
       h.changed_setting_value_bool,
       (
           SELECT rv.setting_value_bool
           FROM config_settings_values rv
           WHERE rv.setting_key = h.setting_key
             AND rv.scope_type = 'role'
             AND rv.scope_key = CONCAT(
                     'role:',
                     COALESCE((
                         SELECT r.role_code
                         FROM auth_roles_assignments ra
                         INNER JOIN auth_roles_catalog r ON r.id = ra.role_id
                         WHERE ra.user_id = p_user_id
                         ORDER BY r.role_rank DESC, r.role_code ASC
                         LIMIT 1
                     ), '')
                 )
             AND rv.value_type = 'bool'
           LIMIT 1
       ) AS baseline_setting_value_bool,
       h.changed_utc
FROM config_settings_history h
WHERE h.scope_key = CONCAT('user:', p_user_id)
  AND h.setting_key LIKE 'permission.%'
  AND h.changed_utc = (
        SELECT MAX(h2.changed_utc)
        FROM config_settings_history h2
        WHERE h2.scope_key = CONCAT('user:', p_user_id)
          AND h2.setting_key LIKE 'permission.%'
      )
ORDER BY h.id ASC;

-- Stored Procedure: sp_config_permissions_user_get
-- Engine: MySQL 5.7
-- Feature: 006-user-management-and-permissions (task T014)
--
-- Purpose: the permission rows that are stored for one person and for their role, so the page can show which
--          of them were chosen for the person and which are inherited from the role.
--
-- Contract:
--   IN  p_user_id   BIGINT   the person
--
--   OUT zero or more rows: setting_key, scope_type, scope_key, setting_value_bool, updated_utc
--
-- What it returns is what is STORED, not what is in force. The caller composes the answer: a key with a
-- `user` row takes it, a key with only a `role` row inherits it, and a key with neither falls back to the
-- shipped fallback. That composition is FR-049's order, and keeping it out of the store is what lets the same
-- declaration serve the page, the gate and the who-holds-this view.
--
-- The role is resolved INSIDE this procedure from the person's own assignment, highest rung first, so no caller
-- can ask about a role that is not the person's (FR-049, FR-017).
--
-- Only keys in the `permission.` namespace are returned. A person's rows in this table also carry preferences
-- that are not permissions — the saved roster filter, for one — and those are not part of this answer. Filtering
-- here rather than in the caller keeps the namespace rule in one place, the same rule
-- sp_config_permissions_user_set refuses to break on the way in.
--
-- Both branches match on `scope_key` — `user:<id>` and `role:<role_code>` — rather than on the `user_id` column.
-- `scope_key` is the keying the data model pins ("the person permission value is a `user`-scoped row in the same
-- table, `scope_key` `user:<id>`"), so this read does not depend on a second column being populated by whoever
-- writes the row. That was a real failure seen by running it: a row written with the right `scope_key` and no
-- `user_id` was invisible to the `user_id`-keyed form.
--
-- Users, not roles: this read never answers "who holds this", which is
-- sp_config_permissions_feature_holders_get's job.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_permissions_user_get;

-- DELIMITER because this body is compound; see the note in
-- sp_auth_temporary_credential_attempt_record/create.sql.
DELIMITER $$

CREATE PROCEDURE sp_config_permissions_user_get(
    IN p_user_id BIGINT
)
BEGIN
    DECLARE v_role_code VARCHAR(64) DEFAULT '';

    SELECT COALESCE(r.role_code, '')
    INTO v_role_code
    FROM auth_roles_assignments ra
    INNER JOIN auth_roles_catalog r ON r.id = ra.role_id
    WHERE ra.user_id = p_user_id
    ORDER BY r.role_rank DESC, r.role_code ASC
    LIMIT 1;

    SELECT v.setting_key,
           v.scope_type,
           v.scope_key,
           v.setting_value_bool,
           v.updated_utc
    FROM config_settings_values v
    WHERE v.setting_key LIKE 'permission.%'
      AND (
            (v.scope_type = 'user' AND v.scope_key = CONCAT('user:', p_user_id))
            OR (v.scope_type = 'role' AND v.scope_key = CONCAT('role:', v_role_code))
      )
    ORDER BY v.setting_key ASC, v.scope_type ASC;
END$$

DELIMITER ;

-- Stored Procedure: sp_config_permissions_user_set
-- Engine: MySQL 5.7
-- Feature: 006-user-management-and-permissions (task T054; amended by T055)
--
-- Purpose: take a whole change set as ONE call and apply it as one act, so a save of several permissions and the
--          reversal of that save are the same call with the same atomicity (FR-071, FR-072).
--
-- Contract:
--   IN  p_user_id       BIGINT        the person the permissions are being set for
--   IN  p_actor_user_id BIGINT        the person performing the change
--   IN  p_changes_json  JSON          [ { "key": ..., "from": ..., "to": ... }, ... ]
--
--   OUT the affected-row count, through the non-query seam. This is a write and returns no result set.
--
-- `from` is the value the caller last saw, and it is what makes a value that has moved impossible to overwrite
-- silently (FR-070). JSON null means "there was no stored value for this person", and it is refused if there now
-- is one.
--
-- `to` is the value to write. It is a value and not an absence: the reversal restores the value that was in force
-- before a save rather than deleting the person's row, because this history table names the value row it changed
-- and the foreign key refuses a row naming one that is gone. That is recorded in
-- sp_config_permissions_reversal_get's header, which is where the reversal is built, and it is the one observable
-- difference a reversal leaves: the restored value is carried by the person's own row rather than inherited.
--
-- THIS IS THE FIRST PLACE A PER-PERSON PERMISSION ROW IS EVER WRITTEN, and it must not run before the settings
-- scope precedence has been re-ranked, so that a person's own value wins over an inherited one (FR-053, decision
-- 29). The re-rank ships in the same feature's foundational phase and the order is a gate, not advice: a row
-- written under the old order would be silently overridden, and nothing would report it.
--
-- The actor's own rung, and the target's, are read here from auth_roles_assignments joined to
-- auth_roles_catalog. Neither is taken from the caller (FR-025).
--
-- Refusals, reported as a stable token so the caller can answer in the reader's words. The token is followed by
-- the key it is about where there is one, so "which key moved" is reported rather than only that something did:
--   mtm_target_outranks_actor        the target's rung is above the actor's
--   mtm_permission_gate_fixed        the set names the permission that opens this page
--   mtm_permission_key_invalid       the set names a key outside the permission namespace
--   mtm_permission_value_moved:<key> the stored value is not the `from` the caller last saw
--
-- `mtm_permission_key_invalid` is NOT in the refusal list contracts/database-contract.md pins. That list carries
-- no token for the namespace rule, and reusing one of the tokens it does carry would report a different refusal
-- than the one that happened, so the token is added and the contract is updated in the same change rather than
-- the rule being silently dropped.
--
-- A REVERSAL IS THE SAME CALL, which is why it obeys the same rule as the change it reverses (FR-069): the caller
-- sends the values back the other way round, and every check below applies to it exactly as it applied to the
-- save. There is deliberately no separate reversal path, because a second path would be a second rule.
--
-- ONE CLOCK FOR THE WHOLE CALL. `v_now` is read once and used by every row this call writes, so the rows of one
-- save share an exact `changed_utc` and the reversal can find them as a group. `UTC_TIMESTAMP()` per statement
-- would not do that: it returns the time the statement began, so a save of several keys straddling a second
-- boundary would write two timestamps and the reversal would reverse half of it. That is a real hazard and this
-- is the fix, not a precaution.
--
-- An EMPTY SET, and an entry that changes nothing, write NOTHING AT ALL (FR-068): the procedure returns before
-- the transaction opens when the set is empty, and skips the history row for an entry whose value already matches
-- what is stored. That is also why the page can treat "nothing has changed" as unavailable rather than as a write
-- that happens to have no effect.
--
-- One history row per changed key carries the actor, the key, the scope, both values and the time (FR-072). A
-- removal records the value it removed as `previous_setting_value_bool` and writes NULL as the changed value,
-- which is what "this person no longer has a choice here" looks like in a value column.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_permissions_user_set;

-- DELIMITER because this body is compound; see the note in
-- sp_auth_temporary_credential_attempt_record/create.sql.
DELIMITER $$

CREATE PROCEDURE sp_config_permissions_user_set(
    IN p_user_id BIGINT,
    IN p_actor_user_id BIGINT,
    IN p_changes_json JSON
)
BEGIN
    DECLARE v_count INT DEFAULT 0;
    DECLARE v_index INT DEFAULT 0;
    DECLARE v_key VARCHAR(190) DEFAULT '';
    DECLARE v_from_raw JSON DEFAULT NULL;
    DECLARE v_from_text VARCHAR(32) DEFAULT NULL;
    DECLARE v_from_bool TINYINT(1) DEFAULT NULL;
    DECLARE v_to_text VARCHAR(32) DEFAULT NULL;
    DECLARE v_to_bool TINYINT(1) DEFAULT 0;
    DECLARE v_target_rank INT DEFAULT 0;
    DECLARE v_actor_rank INT DEFAULT 0;
    DECLARE v_scope_key VARCHAR(255) DEFAULT '';
    DECLARE v_existing_count INT DEFAULT 0;
    DECLARE v_existing_bool TINYINT(1) DEFAULT NULL;
    DECLARE v_setting_id BIGINT DEFAULT NULL;
    DECLARE v_previous_bool TINYINT(1) DEFAULT NULL;
    DECLARE v_skip TINYINT(1) DEFAULT 0;
    DECLARE v_now DATETIME;
    DECLARE v_signal_text VARCHAR(255) DEFAULT '';

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SET v_count = IFNULL(JSON_LENGTH(p_changes_json), 0);

    -- Nothing to write. No transaction opens, so no row and no history record is written (FR-068).
    IF v_count > 0 THEN
        SET v_scope_key = CONCAT('user:', p_user_id);
        SET v_now = UTC_TIMESTAMP();

        START TRANSACTION;

        SELECT COALESCE(MAX(r.role_rank), 0)
        INTO v_target_rank
        FROM auth_roles_assignments ra
        INNER JOIN auth_roles_catalog r ON r.id = ra.role_id
        WHERE ra.user_id = p_user_id;

        SELECT COALESCE(MAX(r.role_rank), 0)
        INTO v_actor_rank
        FROM auth_roles_assignments ra
        INNER JOIN auth_roles_catalog r ON r.id = ra.role_id
        WHERE ra.user_id = p_actor_user_id;

        IF v_target_rank > v_actor_rank THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_target_outranks_actor';
        END IF;

        WHILE v_index < v_count DO
            SET v_skip = 0;

            SET v_key = COALESCE(JSON_UNQUOTE(JSON_EXTRACT(p_changes_json, CONCAT('$[', v_index, '].key'))), '');
            SET v_from_raw = JSON_EXTRACT(p_changes_json, CONCAT('$[', v_index, '].from'));
            SET v_to_text = LOWER(COALESCE(JSON_UNQUOTE(JSON_EXTRACT(p_changes_json, CONCAT('$[', v_index, '].to'))), 'false'));

            SET v_from_text = CASE
                WHEN v_from_raw IS NULL OR JSON_TYPE(v_from_raw) = 'NULL' THEN NULL
                ELSE LOWER(JSON_UNQUOTE(v_from_raw))
            END;

            SET v_from_bool = CASE
                WHEN v_from_text IS NULL THEN NULL
                WHEN v_from_text IN ('true', '1') THEN 1
                ELSE 0
            END;

            SET v_to_bool = IF(v_to_text IN ('true', '1'), 1, 0);

            IF v_key = '' THEN
                SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_permission_key_invalid:';
            END IF;
            -- The one fixed row: reading the declaration to decide whether the declaration may be edited would
            -- be circular, so this is refused here rather than on the page alone (FR-059).
            IF v_key = 'permission.admin.permissions' THEN
                SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_permission_gate_fixed:permission.admin.permissions';
            END IF;

            -- Outside the namespace this procedure owns. A preference is not a permission, and this call must
            -- not become a general settings writer through the back door.
            --
            -- MySQL 5.7's SIGNAL takes a literal or a variable in its SET clause and nothing else, so the two
            -- refusals that name the key build the text in a local variable first. A function call in the SET
            -- clause is a syntax error, which is how this was found by running it.
            IF v_key NOT LIKE 'permission.%' THEN
                SET v_signal_text = CONCAT('mtm_permission_key_invalid:', v_key);
                SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = v_signal_text;
            END IF;

            SELECT COUNT(*), MAX(v.setting_value_bool)
            INTO v_existing_count, v_existing_bool
            FROM config_settings_values v
            WHERE v.setting_key = v_key
              AND v.scope_key = v_scope_key;

            -- The value must still be the one the caller last saw, or it is overwritten blindly and a change
            -- somebody else made is lost without anybody being told (FR-070).
            IF v_from_bool IS NULL THEN
                IF v_existing_count > 0 THEN
                    SET v_signal_text = CONCAT('mtm_permission_value_moved:', v_key);
                    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = v_signal_text;
                END IF;
            ELSE
                IF v_existing_count = 0 OR IFNULL(v_existing_bool, -1) <> v_from_bool THEN
                    SET v_signal_text = CONCAT('mtm_permission_value_moved:', v_key);
                    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = v_signal_text;
                END IF;
            END IF;

            SET v_previous_bool = v_existing_bool;
            SET v_setting_id = NULL;

            IF v_existing_count = 0 OR IFNULL(v_existing_bool, -1) <> v_to_bool THEN
                INSERT INTO config_settings_values (
                    public_id,
                    setting_key,
                    scope_type,
                    scope_key,
                    user_id,
                    setting_value_bool,
                    value_type,
                    updated_by_user_id,
                    updated_utc
                )
                VALUES (
                    UUID(),
                    v_key,
                    'user',
                    v_scope_key,
                    p_user_id,
                    v_to_bool,
                    'bool',
                    p_actor_user_id,
                    v_now
                )
                ON DUPLICATE KEY UPDATE
                    setting_value_bool = VALUES(setting_value_bool),
                    value_type = VALUES(value_type),
                    user_id = VALUES(user_id),
                    updated_by_user_id = VALUES(updated_by_user_id),
                    updated_utc = VALUES(updated_utc);

                SET v_setting_id = LAST_INSERT_ID();

                IF IFNULL(v_setting_id, 0) = 0 THEN
                    SELECT v.id
                    INTO v_setting_id
                    FROM config_settings_values v
                    WHERE v.setting_key = v_key
                      AND v.scope_key = v_scope_key
                    LIMIT 1;
                END IF;
            ELSE
                -- The value is already what was asked for, so nothing changed and no history row is written.
                SET v_skip = 1;
            END IF;

            IF v_skip = 0 THEN
                INSERT INTO config_settings_history (
                    public_id,
                    config_setting_id,
                    setting_key,
                    scope_type,
                    scope_key,
                    user_id,
                    previous_setting_value_bool,
                    changed_setting_value_bool,
                    value_type,
                    changed_by_user_id,
                    changed_utc
                )
                VALUES (
                    UUID(),
                    v_setting_id,
                    v_key,
                    'user',
                    v_scope_key,
                    p_user_id,
                    v_previous_bool,
                    v_to_bool,
                    'bool',
                    p_actor_user_id,
                    v_now
                );
            END IF;

            SET v_index = v_index + 1;
        END WHILE;

        COMMIT;
    END IF;
END$$

DELIMITER ;

-- Create procedure: sp_config_settings_get_effective
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_settings_get_effective;

CREATE PROCEDURE sp_config_settings_get_effective(
    IN p_setting_key VARCHAR(190),
    IN p_computer_id BIGINT,
    IN p_user_id BIGINT
)
SELECT
    setting_key,
    scope_type,
    scope_key,
    computer_id,
    user_id,
    setting_value,
    setting_value_int,
    setting_value_bool,
    setting_value_decimal,
    setting_value_datetime_utc,
    value_type,
    updated_by_user_id,
    updated_utc,
    fn_config_settings_scope_rank(scope_type) AS scope_rank
FROM config_settings_values
WHERE setting_key = p_setting_key
  AND (
        (scope_type = 'computer' AND computer_id = p_computer_id)
        OR scope_type = 'all_users'
        OR (scope_type = 'user' AND user_id = p_user_id)
        OR scope_type IN ('admin', 'developer')
      )
ORDER BY fn_config_settings_scope_rank(scope_type) DESC, updated_utc DESC
LIMIT 1;

-- Create procedure: sp_config_settings_upsert
-- Engine: MySQL 5.7
--
-- This is NOT the permission write path, deliberately (006-user-management-and-permissions, task T009).
-- A permission value is written only by `sp_config_permissions_user_set`, which enforces the rank rule, the
-- moved-value check and the one fixed row in one transaction and writes the history row itself. Routing a
-- permission through here would bypass all four.
--
-- The `ELSE 'developer'` branch below therefore stays as it is. It is a known trap, and it is left in place
-- rather than fixed: its job is to give an unrecognised scope a deterministic `scope_key` instead of a NULL,
-- and the `role` scope is never written through this procedure. Seeded role baselines are plain SQL inserts in
-- Database/Seeds/seed_permission_role_baselines, and a person's own rows go through the change-set procedure
-- named above. Adding a `WHEN 'role'` branch here would create a second way to write a role baseline, which is
-- exactly what FR-048 keeps as data and FR-046 keeps in one place.
--
-- The equivalent trap on the read side is worth knowing too: `fn_config_settings_scope_rank` ranks `role` 3 and
-- `user` 6, so a row written with the wrong scope_key is not merely mislabelled, it resolves at the wrong rung.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_settings_upsert;

CREATE PROCEDURE sp_config_settings_upsert(
    IN p_setting_key VARCHAR(190),
    IN p_scope_type VARCHAR(16),
    IN p_computer_id BIGINT,
    IN p_user_id BIGINT,
    IN p_setting_value TEXT,
    IN p_setting_value_int BIGINT,
    IN p_setting_value_bool TINYINT,
    IN p_setting_value_decimal DECIMAL(18, 6),
    IN p_setting_value_datetime_utc DATETIME,
    IN p_value_type VARCHAR(32),
    IN p_updated_by_user_id BIGINT
)
INSERT INTO config_settings_values (
    public_id,
    setting_key,
    scope_type,
    scope_key,
    computer_id,
    user_id,
    setting_value,
    setting_value_int,
    setting_value_bool,
    setting_value_decimal,
    setting_value_datetime_utc,
    value_type,
    updated_by_user_id,
    updated_utc
)
SELECT
    UUID(),
    TRIM(p_setting_key),
    LOWER(TRIM(p_scope_type)),
    CASE LOWER(TRIM(p_scope_type))
        WHEN 'computer' THEN CONCAT('computer:', p_computer_id)
        WHEN 'user' THEN CONCAT('user:', p_user_id)
        WHEN 'all_users' THEN 'all_users'
        WHEN 'admin' THEN 'admin'
        ELSE 'developer'
    END,
    p_computer_id,
    p_user_id,
    p_setting_value,
    p_setting_value_int,
    p_setting_value_bool,
    p_setting_value_decimal,
    p_setting_value_datetime_utc,
    p_value_type,
    p_updated_by_user_id,
    UTC_TIMESTAMP()
FROM dual
ON DUPLICATE KEY UPDATE
    setting_value = VALUES(setting_value),
    setting_value_int = VALUES(setting_value_int),
    setting_value_bool = VALUES(setting_value_bool),
    setting_value_decimal = VALUES(setting_value_decimal),
    setting_value_datetime_utc = VALUES(setting_value_datetime_utc),
    value_type = VALUES(value_type),
    updated_by_user_id = VALUES(updated_by_user_id),
    updated_utc = UTC_TIMESTAMP();

-- Stored Procedure: sp_config_settings_values_delete
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T096)
--
-- Purpose
-- -------
-- Delete the configuration override stored for one exact (setting_key, scope_key) pair, for
-- `MTM_Waitlist.Settings/Services/ConfigSettingsValueService.DeleteSettingValueAsync`.
--
-- It replaces the caller's inline `DELETE FROM config_settings_values WHERE setting_key = … AND scope_key = …`
-- (FR-015, constitution III). The predicate is identical; only the location of the statement changed.
--
-- Contract
-- --------
--   IN  p_setting_key  VARCHAR(190)   exact setting key (column width: varchar(190))
--   IN  p_scope_key    VARCHAR(255)   exact scope key (column width: varchar(255))
--
--   OUT  no result set. The affected-row count reaches the caller from the client (MySqlConnector's
--        non-query return value); a delete that matches no row is a successful no-op with a count of 0,
--        exactly as the caller's own inline statement behaved.
--
-- Deliberately a hard delete of an override row, not of a primary entity: the override is a disposable
-- wrapper around a value that always has an appsettings.json fallback, which is why removing it is safe and
-- why the caller documents "a missing override must never break configuration resolution". Nothing else in
-- the schema references this row (no foreign key points at config_settings_values).
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_settings_values_delete;

CREATE PROCEDURE sp_config_settings_values_delete(
    IN p_setting_key VARCHAR(190),
    IN p_scope_key VARCHAR(255)
)
DELETE FROM config_settings_values
WHERE setting_key = p_setting_key
  AND scope_key = p_scope_key;

-- Stored Procedure: sp_config_settings_values_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T091)
--
-- Purpose
-- -------
-- Return the configuration override stored for one exact (setting_key, scope_key) pair, for
-- `MTM_Waitlist.Settings/Services/ConfigSettingsValueService.GetSettingValueAsync`.
--
-- Why this is not sp_config_settings_get_effective (recorded: T091 asked for that one)
-- ---------------------------------------------------------------------------------
-- That procedure answers a different question. It resolves the *effective* value across a precedence
-- chain — `WHERE setting_key = p_setting_key AND (computer match OR all_users OR user match OR admin OR
-- developer)` ordered by `fn_config_settings_scope_rank(scope_type) DESC, updated_utc DESC LIMIT 1` — and
-- it takes `(p_setting_key, p_computer_id, p_user_id)`, not a scope key. It also returns a different column
-- set: no `id`, no `public_id`, no `scope_key`, no `scope_type`.
--
-- The caller needs the *exact* override row for the scope it was asked about, and it parses `id`,
-- `public_id`, `scope_key` and `scope_type` off the result. Routing it to the precedence resolver would
-- have silently changed which row is read (a broader-scope fallback would be returned where the caller
-- expects `null`) and would have dropped four mapped columns. Verified against the live definition before
-- switching, which is exactly what this task asked for.
--
-- Contract
-- --------
--   IN  p_setting_key  VARCHAR(190)   exact setting key (column width: varchar(190))
--   IN  p_scope_key    VARCHAR(255)   exact scope key (column width: varchar(255))
--
--   OUT  zero or one row — the same fifteen columns and the same `LIMIT 1` the inline statement returned.
--
-- Column widths match `config_settings_values` as deployed (setting_key varchar(190), scope_key
-- varchar(255)); a wider parameter would only invite truncation-free mismatches that can never match a row.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_settings_values_get;

CREATE PROCEDURE sp_config_settings_values_get(
    IN p_setting_key VARCHAR(190),
    IN p_scope_key VARCHAR(255)
)
SELECT
    id,
    public_id,
    setting_key,
    scope_type,
    scope_key,
    computer_id,
    user_id,
    setting_value,
    setting_value_int,
    setting_value_bool,
    setting_value_decimal,
    setting_value_datetime_utc,
    value_type,
    updated_by_user_id,
    updated_utc
FROM config_settings_values
WHERE setting_key = p_setting_key
  AND scope_key = p_scope_key
LIMIT 1;

-- Create procedure: sp_core_buildings_upsert
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_buildings_upsert;

CREATE PROCEDURE sp_core_buildings_upsert(
    IN p_building_code VARCHAR(64),
    IN p_building_name VARCHAR(128),
    IN p_is_active TINYINT,
    IN p_updated_by_user_id BIGINT
)
INSERT INTO core_buildings_catalog (
    public_id,
    building_code,
    building_name,
    is_active,
    created_utc,
    updated_utc,
    updated_by_user_id
)
SELECT
    UUID(),
    TRIM(p_building_code),
    TRIM(p_building_name),
    p_is_active,
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP(),
    p_updated_by_user_id
FROM dual
ON DUPLICATE KEY UPDATE
    building_name = VALUES(building_name),
    is_active = VALUES(is_active),
    updated_utc = UTC_TIMESTAMP(),
    updated_by_user_id = VALUES(updated_by_user_id);

-- Stored Procedure: sp_core_computers_registry_delete
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T092)
--
-- Purpose: remove one registry row by primary key. Replaces the inline DELETE in
--          ComputerRegistryService.DeleteComputerAsync (FR-015).
--
-- Contract:
--   IN  p_id BIGINT
--   OUT no result set. The affected-row count reaches the caller from the client (MySqlConnector's non-query
--       return value), and the caller turns it into the boolean it returns: deleting an already-absent row is
--       a successful no-op that reports 0, which is what the inline statement did.
--
-- A hard delete is the documented policy for a primary entity in this schema, and no foreign key references
-- this table, so nothing is orphaned by removing a row.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computers_registry_delete;

CREATE PROCEDURE sp_core_computers_registry_delete(
    IN p_id BIGINT
)
DELETE FROM core_computers_registry
WHERE id = p_id;

-- Stored Procedure: sp_core_computers_registry_get_all
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T092)
--
-- Purpose: the whole registry, in display order. Replaces the inline SELECT in
--          ComputerRegistryService.GetAllComputersAsync (FR-015).
--
-- Contract:
--   OUT one row per registered machine: id, computer_name, display_name, description,
--       mac_address_normalized, is_registered — ordered by display_name then computer_name, exactly as the
--       statement this replaces ordered them, so the settings list keeps its current sequence.
--
-- No parameters: the caller filters nothing server-side.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computers_registry_get_all;

CREATE PROCEDURE sp_core_computers_registry_get_all()
SELECT
    id,
    computer_name,
    display_name,
    description,
    mac_address_normalized,
    is_registered
FROM core_computers_registry
ORDER BY display_name ASC, computer_name ASC;

-- Stored Procedure: sp_core_computers_registry_lookup_by_mac_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T092)
--
-- Purpose: find the most recently updated registry row for a MAC address, whatever name it currently holds.
--          Replaces the inline SELECT in ComputerRegistryService.LookupComputerByMacAsync (FR-015).
--
-- Contract:
--   IN  p_mac_address_normalized  VARCHAR(64)
--   OUT zero or one row: id, computer_name, display_name, description, mac_address_normalized, is_registered
--       (newest updated_utc first — the same tie-break the inline statement used, so a machine that was
--        renamed resolves to its current row rather than an older one)
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computers_registry_lookup_by_mac_get;

CREATE PROCEDURE sp_core_computers_registry_lookup_by_mac_get(
    IN p_mac_address_normalized VARCHAR(64)
)
SELECT
    id,
    computer_name,
    display_name,
    description,
    mac_address_normalized,
    is_registered
FROM core_computers_registry
WHERE mac_address_normalized = p_mac_address_normalized
ORDER BY updated_utc DESC
LIMIT 1;

-- Stored Procedure: sp_core_computers_registry_lookup_by_name_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T092; found while routing WorkCenterCatalogService)
--
-- Purpose: resolve one registry row from a machine name *or* its normalized hostname, preferring the exact
--          name match. Replaces two identical inline SELECTs in `WorkCenterCatalogService` —
--          `SaveHotWorkCentersAsync`'s workstation resolve and `ResolveCurrentComputerNameAsync` (FR-015).
--
-- Why this is an extra artifact: task T092 listed a lookup by (name + MAC) and a lookup by MAC, but the shared
-- service has no MAC at hand when it resolves a workstation — it has only the name it was given or the local
-- machine name. Both of its statements therefore matched on either column, with an exact-match tie-break. That
-- shape had no procedure, so this pair was added rather than widening one of the two that exist (a name+MAC
-- procedure cannot answer a name-only question, and a MAC-only one cannot either).
--
-- Contract:
--   IN  p_name  VARCHAR(255)   a computer_name or a hostname_normalized value
--   OUT zero or one row: id, computer_name, hostname_normalized, display_name, description,
--       mac_address_normalized, is_registered
--
--   The row is chosen by `ORDER BY CASE WHEN computer_name = p_name THEN 0 ELSE 1 END ... LIMIT 1`, so a name
--   that matches one machine's computer_name and another's hostname resolves to the computer_name match. That
--   is exactly the ordering the two statements this replaces used; dropping it would make the result depend on
--   insertion order.
--
-- Parameter width is 255 because it must accept a `hostname_normalized` (varchar(255)), which is wider than
-- `computer_name` (varchar(128)).
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computers_registry_lookup_by_name_get;

CREATE PROCEDURE sp_core_computers_registry_lookup_by_name_get(
    IN p_name VARCHAR(255)
)
SELECT
    id,
    computer_name,
    hostname_normalized,
    display_name,
    description,
    mac_address_normalized,
    is_registered
FROM core_computers_registry
WHERE computer_name = p_name
   OR hostname_normalized = p_name
ORDER BY CASE WHEN computer_name = p_name THEN 0 ELSE 1 END
LIMIT 1;

-- Stored Procedure: sp_core_computers_registry_lookup_by_name_mac_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T092)
--
-- Purpose: find the registry row for one exact (computer_name, mac_address_normalized) pair — the identity a
--          logon presents. Replaces the inline SELECT in ComputerRegistryService.LookupComputerAsync (FR-015).
--
-- Contract:
--   IN  p_computer_name           VARCHAR(128)  column width from core_computers_registry
--   IN  p_mac_address_normalized  VARCHAR(64)
--   OUT zero or one row: id, computer_name, display_name, description, mac_address_normalized, is_registered
--
-- No TRIM here: the caller already trims both values, so adding it would only hide a caller that stopped
-- doing so. The predicate is otherwise byte-for-byte the statement this replaces.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computers_registry_lookup_by_name_mac_get;

CREATE PROCEDURE sp_core_computers_registry_lookup_by_name_mac_get(
    IN p_computer_name VARCHAR(128),
    IN p_mac_address_normalized VARCHAR(64)
)
SELECT
    id,
    computer_name,
    display_name,
    description,
    mac_address_normalized,
    is_registered
FROM core_computers_registry
WHERE computer_name = p_computer_name
  AND mac_address_normalized = p_mac_address_normalized
LIMIT 1;

-- Stored Procedure: sp_core_computers_registry_registered_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T092; found while routing WorkCenterCatalogService)
--
-- Purpose: the registered machines, in display order — the computer picker's source. Replaces the inline
--          SELECT in `WorkCenterCatalogService.GetAvailableComputersAsync` (FR-015).
--
-- Why this is an extra artifact, and why it is not `sp_core_computers_registry_get_all`: the two reads differ
-- by exactly one predicate. `get_all` returns every row (it serves the registry editor, which must be able to
-- show a machine an operator has retired); this one returns only `is_registered = 1` (it serves a picker, which
-- must not offer a retired machine). Routing the picker to `get_all` would have quietly added unregistered
-- machines to it the first time an operator retired one, so the filter is kept in the database as its own
-- procedure instead of being dropped or duplicated into C#.
--
-- Contract:
--   OUT one row per registered machine: computer_name, display_name — ordered by display_name then
--       computer_name, the same order the statement this replaces used.
--
-- No parameters: "registered" is the procedure's whole job.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computers_registry_registered_get;

CREATE PROCEDURE sp_core_computers_registry_registered_get()
SELECT
    computer_name,
    display_name
FROM core_computers_registry
WHERE is_registered = 1
ORDER BY display_name ASC, computer_name ASC;

-- Stored Procedure: sp_core_computers_registry_update
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T092)
--
-- Purpose: edit one registry row by primary key, including its registration flag. Replaces the inline UPDATE in
--          ComputerRegistryService.UpdateComputerAsync (FR-015).
--
-- Contract:
--   IN  p_id                       BIGINT
--   IN  p_computer_name            VARCHAR(128)
--   IN  p_hostname_normalized      VARCHAR(255)
--   IN  p_mac_address_normalized   VARCHAR(64)
--   IN  p_display_name             VARCHAR(128)
--   IN  p_description              VARCHAR(255)   NULL when the caller has no description
--   IN  p_is_registered            TINYINT(1)     0 or 1 — an operator may retire a machine without deleting it
--
-- This is the one path where the MAC *is* rewritten (an operator correcting a mistyped address), which is why
-- it is keyed on `id` rather than on the MAC the way update-by-mac is. `public_id` and `created_utc` stay put.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computers_registry_update;

CREATE PROCEDURE sp_core_computers_registry_update(
    IN p_id BIGINT,
    IN p_computer_name VARCHAR(128),
    IN p_hostname_normalized VARCHAR(255),
    IN p_mac_address_normalized VARCHAR(64),
    IN p_display_name VARCHAR(128),
    IN p_description VARCHAR(255),
    IN p_is_registered TINYINT
)
UPDATE core_computers_registry
SET computer_name = p_computer_name,
    hostname_normalized = p_hostname_normalized,
    mac_address_normalized = p_mac_address_normalized,
    display_name = p_display_name,
    description = p_description,
    is_registered = p_is_registered,
    updated_utc = UTC_TIMESTAMP()
WHERE id = p_id;

-- Stored Procedure: sp_core_computers_registry_update_by_mac
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T092)
--
-- Purpose: follow a machine that was renamed — update the newest row for a MAC address. Replaces the inline
--          UPDATE in ComputerRegistryService.UpdateComputerByMacAsync (FR-015).
--
-- Contract:
--   IN  p_mac_address_normalized   VARCHAR(64)
--   IN  p_computer_name            VARCHAR(128)
--   IN  p_hostname_normalized      VARCHAR(255)
--   IN  p_display_name             VARCHAR(128)
--   IN  p_description              VARCHAR(255)   NULL when the caller has no description
--
-- `ORDER BY id DESC LIMIT 1` is kept deliberately: a MAC can legitimately have more than one row (renames,
-- reimages), and the caller's intent is to move the newest one. `mac_address_normalized` and `is_registered`
-- are not touched — the statement this replaces did not touch them either, and rewriting the MAC here would
-- break the identity the row is being found by.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computers_registry_update_by_mac;

CREATE PROCEDURE sp_core_computers_registry_update_by_mac(
    IN p_mac_address_normalized VARCHAR(64),
    IN p_computer_name VARCHAR(128),
    IN p_hostname_normalized VARCHAR(255),
    IN p_display_name VARCHAR(128),
    IN p_description VARCHAR(255)
)
UPDATE core_computers_registry
SET computer_name = p_computer_name,
    hostname_normalized = p_hostname_normalized,
    display_name = p_display_name,
    description = p_description,
    updated_utc = UTC_TIMESTAMP()
WHERE mac_address_normalized = p_mac_address_normalized
ORDER BY id DESC
LIMIT 1;

-- Stored Procedure: sp_core_computers_registry_upsert
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T092)
--
-- Purpose: register a computer, or refresh the row it already has. Replaces the inline
--          INSERT ... ON DUPLICATE KEY UPDATE in ComputerRegistryService.UpsertComputerAsync (FR-015).
--
-- Contract:
--   IN  p_computer_name            VARCHAR(128)
--   IN  p_hostname_normalized      VARCHAR(255)
--   IN  p_mac_address_normalized   VARCHAR(64)
--   IN  p_display_name             VARCHAR(128)
--   IN  p_description              VARCHAR(255)   NULL when the caller has no description
--
-- Semantics preserved from the statement this replaces:
--   * `public_id` is generated on insert and never rewritten on update (UUID() is only in the VALUES list).
--   * `created_utc` is set on insert and left alone on update.
--   * The duplicate key is whichever unique key collides (display_name is UNIQUE, and the
--     (computer_name, mac_address_normalized) pair is indexed): the update arms are the same ones the inline
--     statement carried — computer_name, display_name, description, is_registered = 1, updated_utc.
--   * `is_registered` is always set back to 1: seeing the machine at logon is what registration means.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computers_registry_upsert;

CREATE PROCEDURE sp_core_computers_registry_upsert(
    IN p_computer_name VARCHAR(128),
    IN p_hostname_normalized VARCHAR(255),
    IN p_mac_address_normalized VARCHAR(64),
    IN p_display_name VARCHAR(128),
    IN p_description VARCHAR(255)
)
INSERT INTO core_computers_registry (
    public_id,
    computer_name,
    hostname_normalized,
    mac_address_normalized,
    display_name,
    description,
    is_registered,
    created_utc,
    updated_utc
)
VALUES (
    UUID(),
    p_computer_name,
    p_hostname_normalized,
    p_mac_address_normalized,
    p_display_name,
    p_description,
    1,
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
)
ON DUPLICATE KEY UPDATE
    computer_name = VALUES(computer_name),
    display_name = VALUES(display_name),
    description = VALUES(description),
    is_registered = 1,
    updated_utc = UTC_TIMESTAMP();

-- Stored Procedure: sp_core_employee_by_identifier_get
-- Engine: MySQL 5.7
-- Feature: 004-unified-card-item-picker (task T143)
--
-- Purpose: resolve ONE account by the employee identifier the signed-in session carries, so a request can be
--          attributed to the person who actually raised it (FR-046). It replaces a hard-coded identity in the
--          New Request wizard's Work Center step, which named `6229` "John Koll" for every signed-in person and
--          therefore attributed every request in the store to one man.
--
-- Contract:
--   IN  p_employee_identifier  VARCHAR(128)  the identifier the caller holds
--
--   OUT zero or one row: employee_identifier, display_name, is_active
--
-- The comparison is on the STORED IDENTIFIER, never on a name: a name is not the key an account is found by, and
-- two people may hold similar names. An identifier no account carries returns no row at all, which is what lets
-- the caller refuse it instead of inventing a name for it.
--
-- Identifiers are not unique by themselves — `johnk` and `jkoll` deliberately share `6229`, both being John Koll
-- — so the read is ordered to answer with the active account first when more than one carries the identifier.
--
-- The row is returned even when the account is inactive, so the caller can say "that employee is not active"
-- rather than the weaker "no such employee".
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_employee_by_identifier_get;

CREATE PROCEDURE sp_core_employee_by_identifier_get(
    IN p_employee_identifier VARCHAR(128)
)
SELECT u.employee_identifier,
       COALESCE(u.display_name, '') AS display_name,
       u.is_active
FROM core_users_profiles u
WHERE u.employee_identifier IS NOT NULL
  AND u.employee_identifier = p_employee_identifier
ORDER BY u.is_active DESC, u.id ASC
LIMIT 1;

-- Stored Procedure: sp_server_utc_now_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T093)
--
-- Purpose: the database server's UTC clock, so session validity is judged against the server rather than the
--          client. Replaces the inline `SELECT fn_server_utc_now();` in
--          `StartupSessionRepository.ReadServerTimeUtcAsync` (FR-015, and the schema ruleset's session rule that
--          "session validity must be comparable to DB server UTC").
--
-- Contract:
--   OUT one row / one column: the server's UTC timestamp, from the existing `fn_server_utc_now()` function.
--
-- The function itself is unchanged and still the single implementation of "now" — this only gives the application
-- a procedure-shaped way to ask for it, so no statement text has to live in C#.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_server_utc_now_get;

CREATE PROCEDURE sp_server_utc_now_get()
SELECT fn_server_utc_now() AS server_utc_now;

-- Stored Procedure: sp_setup_active_job_dunnage_assignments_get
-- Engine: MySQL 5.7
-- Purpose: Get saved dunnage assignments JSON for an exact part/sequence pair from custom setup data.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_active_job_dunnage_assignments_get;

CREATE PROCEDURE sp_setup_active_job_dunnage_assignments_get(
    IN p_work_order VARCHAR(32),
    IN p_part_number VARCHAR(64),
    IN p_sequence_number VARCHAR(32)
)
SELECT selected_dunnage_parts_json
FROM setup_part_sequence_custom_data
WHERE part_number = TRIM(p_part_number)
  AND sequence_number = TRIM(p_sequence_number)
ORDER BY updated_utc DESC
LIMIT 1;

-- Stored Procedure: sp_setup_active_jobs_latest_by_work_center_get
-- Engine: MySQL 5.7
-- Purpose: Retrieve latest active setup row per work center.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_active_jobs_latest_by_work_center_get;

CREATE PROCEDURE sp_setup_active_jobs_latest_by_work_center_get()
SELECT aj.work_center, aj.work_order, aj.part_number, aj.sequence_number,
       aj.subordinate_parts_json,
       aj.selected_dunnage_parts_json
FROM setup_active_jobs aj
INNER JOIN (
    SELECT work_center, MAX(id) AS max_id
    FROM setup_active_jobs
    WHERE is_active = 1
    GROUP BY work_center
) latest ON latest.max_id = aj.id;

-- Stored Procedure: sp_setup_job_history_insert_for_pair
-- Engine: MySQL 5.7
-- Purpose: Insert a history snapshot for an exact work order/part/sequence pair from setup_active_jobs.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_job_history_insert_for_pair;

CREATE PROCEDURE sp_setup_job_history_insert_for_pair(
    IN p_event_action VARCHAR(32),
    IN p_changed_by_user_id BIGINT,
    IN p_work_order VARCHAR(32),
    IN p_part_number VARCHAR(64),
    IN p_sequence_number VARCHAR(32)
)
INSERT INTO setup_job_history (
    public_id,
    active_job_id,
    event_action,
    work_order,
    part_number,
    sequence_number,
    work_center,
    selected_dunnage_type_id,
    selected_dunnage_part_id,
    subordinate_parts_json,
    selected_dunnage_parts_json,
    changed_by_user_id,
    changed_utc
)
SELECT
    UUID(),
    aj.id,
    TRIM(p_event_action),
    aj.work_order,
    aj.part_number,
    aj.sequence_number,
    aj.work_center,
    aj.selected_dunnage_type_id,
    aj.selected_dunnage_part_id,
    aj.subordinate_parts_json,
    aj.selected_dunnage_parts_json,
    p_changed_by_user_id,
    UTC_TIMESTAMP()
FROM setup_active_jobs aj
WHERE aj.work_order = TRIM(p_work_order)
  AND aj.part_number = TRIM(p_part_number)
  AND aj.sequence_number = TRIM(p_sequence_number)
ORDER BY aj.updated_utc DESC
LIMIT 1;

-- Stored Procedure: sp_setup_job_history_scrap_type_get
-- Engine: MySQL 5.7
-- Purpose: Get saved scrap type for an exact part/sequence pair from custom setup data.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_job_history_scrap_type_get;

CREATE PROCEDURE sp_setup_job_history_scrap_type_get(
    IN p_work_order VARCHAR(32),
    IN p_part_number VARCHAR(64),
    IN p_sequence_number VARCHAR(32)
)
SELECT selected_scrap_type
FROM setup_part_sequence_custom_data
WHERE part_number = TRIM(p_part_number)
  AND sequence_number = TRIM(p_sequence_number)
ORDER BY updated_utc DESC
LIMIT 1;

-- Stored Procedure: sp_setup_part_sequence_custom_data_upsert
-- Engine: MySQL 5.7
-- Purpose: Upsert custom setup data for an exact part/sequence pair.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_part_sequence_custom_data_upsert;

CREATE PROCEDURE sp_setup_part_sequence_custom_data_upsert(
    IN p_part_number VARCHAR(64),
    IN p_sequence_number VARCHAR(32),
    IN p_selected_scrap_type VARCHAR(128),
    IN p_selected_dunnage_type_id VARCHAR(64),
    IN p_selected_dunnage_part_id VARCHAR(64),
    IN p_subordinate_parts_json JSON,
    IN p_selected_dunnage_parts_json JSON,
    IN p_updated_by_user_id BIGINT
)
INSERT INTO setup_part_sequence_custom_data (
    public_id,
    part_number,
    sequence_number,
    selected_scrap_type,
    selected_dunnage_type_id,
    selected_dunnage_part_id,
    subordinate_parts_json,
    selected_dunnage_parts_json,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
)
SELECT
    UUID(),
    TRIM(p_part_number),
    TRIM(p_sequence_number),
    NULLIF(TRIM(p_selected_scrap_type) COLLATE utf8mb4_unicode_ci, ''),
    NULLIF(TRIM(p_selected_dunnage_type_id) COLLATE utf8mb4_unicode_ci, ''),
    NULLIF(TRIM(p_selected_dunnage_part_id) COLLATE utf8mb4_unicode_ci, ''),
    p_subordinate_parts_json,
    p_selected_dunnage_parts_json,
    p_updated_by_user_id,
    p_updated_by_user_id,
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
FROM dual
ON DUPLICATE KEY UPDATE
    selected_scrap_type = VALUES(selected_scrap_type),
    selected_dunnage_type_id = VALUES(selected_dunnage_type_id),
    selected_dunnage_part_id = VALUES(selected_dunnage_part_id),
    subordinate_parts_json = VALUES(subordinate_parts_json),
    selected_dunnage_parts_json = VALUES(selected_dunnage_parts_json),
    updated_by_user_id = VALUES(updated_by_user_id),
    updated_utc = UTC_TIMESTAMP();

-- Stored Procedure: sp_setup_save_setup
-- Engine: MySQL 5.7
-- Purpose: Persist work center setup state.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_save_setup;

CREATE PROCEDURE sp_setup_save_setup(
	IN p_work_order VARCHAR(32),
	IN p_part_number VARCHAR(64),
	IN p_sequence_number VARCHAR(32),
	IN p_work_center VARCHAR(64),
	IN p_selected_dunnage_type_id VARCHAR(64),
	IN p_selected_dunnage_part_id VARCHAR(64),
	IN p_subordinate_parts_json JSON,
	IN p_selected_dunnage_parts_json JSON,
	IN p_saved_by_user_id BIGINT
)
INSERT INTO setup_active_jobs (
	public_id,
	work_order,
	part_number,
	sequence_number,
	work_center,
	selected_dunnage_type_id,
	selected_dunnage_part_id,
	subordinate_parts_json,
	selected_dunnage_parts_json,
	is_active,
	created_by_user_id,
	updated_by_user_id,
	created_utc,
	updated_utc
)
SELECT
	UUID(),
	TRIM(p_work_order),
	TRIM(p_part_number),
	TRIM(p_sequence_number),
	TRIM(p_work_center),
	NULLIF(TRIM(p_selected_dunnage_type_id) COLLATE utf8mb4_unicode_ci, ''),
	NULLIF(TRIM(p_selected_dunnage_part_id) COLLATE utf8mb4_unicode_ci, ''),
	p_subordinate_parts_json,
	p_selected_dunnage_parts_json,
	1,
	p_saved_by_user_id,
	p_saved_by_user_id,
	UTC_TIMESTAMP(),
	UTC_TIMESTAMP()
FROM dual
ON DUPLICATE KEY UPDATE
	work_order = VALUES(work_order),
	part_number = VALUES(part_number),
	sequence_number = VALUES(sequence_number),
	selected_dunnage_type_id = VALUES(selected_dunnage_type_id),
	selected_dunnage_part_id = VALUES(selected_dunnage_part_id),
	subordinate_parts_json = VALUES(subordinate_parts_json),
	selected_dunnage_parts_json = VALUES(selected_dunnage_parts_json),
	is_active = 1,
	updated_by_user_id = VALUES(updated_by_user_id),
	updated_utc = UTC_TIMESTAMP();

-- Stored Procedure: sp_setup_work_centers_catalog_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T090)
--
-- Purpose: the active work-center catalog with its display rank, in the order the Settings screen shows it.
--          Replaces the inline SELECT in `ImageLocationService.LoadWorkCenterDetailsAsync` (FR-015).
--
-- Why this exists instead of routing that call site to sp_setup_work_centers_get_all
-- -----------------------------------------------------------------------------------
-- T090 asked for exactly that routing and required the `IN (...)` filter semantics to be verified first. They do
-- not match, in three ways, so the switch was not made:
--
--   * that procedure does **not** select `sort_rank`, and this caller maps it into `WorkCenterItem.SortRank` —
--     every rank would have silently become 0;
--   * it orders by `sort_rank, work_center_name`, while this caller orders by `building, sort_rank,
--     work_center_name` — all 25 rows would have been reordered;
--   * it takes no parameters, so it cannot express `work_center_name IN (…)` at all.
--
-- Making it serve this caller would have meant changing a procedure that other call sites (the work-center
-- catalog and the image-override dialog) already depend on. A separate read is additive and leaves them alone.
--
-- The name filter is applied by the caller, not here: a variable-length name list has no parameter form in
-- MySQL 5.7 that is not a fragile delimited string, and the active catalog is small and bounded (25 rows live on
-- 2026-09-10), so narrowing it in C# costs nothing measurable and cannot break on a name containing a comma.
-- Everything else — the `is_active = 1` scope and the ordering — is preserved exactly.
--
-- Contract:
--   OUT one row per active work center: id, work_center_name, building, sort_rank, is_active
--       ordered by building ASC, sort_rank ASC, work_center_name ASC.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_work_centers_catalog_get;

CREATE PROCEDURE sp_setup_work_centers_catalog_get()
SELECT
    id,
    work_center_name,
    building,
    sort_rank,
    is_active
FROM vw_setup_work_centers_active
ORDER BY building ASC, sort_rank ASC, work_center_name ASC;

-- Stored Procedure: sp_setup_work_centers_delete
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_work_centers_delete;

CREATE PROCEDURE sp_setup_work_centers_delete(
    IN p_work_center_id VARCHAR(32)
)
DELETE FROM setup_work_centers_catalog
WHERE id = CAST(TRIM(p_work_center_id) AS UNSIGNED);

-- Stored Procedure: sp_setup_work_centers_exists_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
--
-- Purpose: does this work center exist, so the orphan detector can tell a live override from an orphaned one.
--          Replaces the inline `SELECT id FROM setup_work_centers_catalog WHERE id = @p_id LIMIT 1;` in
--          `ImageOverrideReadService.ValidateWorkCenterExistsAsync` (FR-015).
--
-- Contract:
--   IN  p_id  BIGINT
--   OUT zero or one row / one column: the id, when it exists. The caller only asks whether it got a row back.
--
-- Note the deliberate difference from `sp_setup_work_centers_get_all` and
-- `sp_setup_work_centers_catalog_get`: **no `is_active` filter.** This answers "does this work center exist",
-- which the orphan detector needs in order to distinguish "the work center was deleted from the catalog" from
-- "the work center is merely inactive right now" — filtering to active rows would report every override on a
-- temporarily inactive work center as an orphan and offer the operator a cleanup that should not happen.
-- That matches the statement this replaces, which also had no `is_active` predicate.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_work_centers_exists_get;

CREATE PROCEDURE sp_setup_work_centers_exists_get(
    IN p_id BIGINT
)
SELECT id
FROM setup_work_centers_catalog
WHERE id = p_id
LIMIT 1;

-- Stored Procedure: sp_setup_work_centers_get_all
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_work_centers_get_all;

CREATE PROCEDURE sp_setup_work_centers_get_all()
SELECT
    id,
    building,
    work_center_name,
    is_active,
    updated_utc
FROM vw_setup_work_centers_active
ORDER BY sort_rank ASC, work_center_name ASC;

-- Stored Procedure: sp_setup_work_centers_touch
-- Engine: MySQL 5.7
-- Purpose: Refresh the work center catalog "Last Updated" timestamp after setup activity.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_work_centers_touch;

CREATE PROCEDURE sp_setup_work_centers_touch(
    IN p_work_center VARCHAR(64),
    IN p_updated_by_user_id BIGINT
)
UPDATE setup_work_centers_catalog
SET
    updated_utc = UTC_TIMESTAMP(),
    updated_by_user_id = COALESCE(p_updated_by_user_id, updated_by_user_id)
WHERE work_center_name = TRIM(p_work_center);

-- Stored Procedure: sp_setup_work_centers_upsert
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_work_centers_upsert;

CREATE PROCEDURE sp_setup_work_centers_upsert(
    IN p_work_center_id VARCHAR(32),
    IN p_building VARCHAR(64),
    IN p_work_center_name VARCHAR(64),
    IN p_modified_by_user_id BIGINT
)
INSERT INTO setup_work_centers_catalog (
    id,
    public_id,
    building,
    work_center_name,
    is_active,
    sort_rank,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
)
VALUES (
    CAST(NULLIF(TRIM(p_work_center_id) COLLATE utf8mb4_unicode_ci, '') AS UNSIGNED),
    UUID(),
    NULLIF(TRIM(p_building) COLLATE utf8mb4_unicode_ci, ''),
    NULLIF(fn_setup_work_center_name_normalized(p_work_center_name) COLLATE utf8mb4_unicode_ci, ''),
    1,
    100,
    p_modified_by_user_id,
    p_modified_by_user_id,
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
)
ON DUPLICATE KEY UPDATE
    building = VALUES(building),
    work_center_name = VALUES(work_center_name),
    updated_by_user_id = VALUES(updated_by_user_id),
    updated_utc = UTC_TIMESTAMP();

-- Stored Procedure: sp_user_management_create
-- Engine: MySQL 5.7
-- Feature: 006-user-management-and-permissions (task T011)
--
-- Purpose: create one person, as one act. The profile and the role assignment are written in the same
--          transaction, so a person cannot come into existence with no role (FR-006) and cannot come into
--          existence with two (FR-009).
--
-- Contract:
--   IN  p_username              VARCHAR(128)  the sign-in name as typed; stored upper case
--   IN  p_first_name            VARCHAR(128)
--   IN  p_last_name             VARCHAR(128)
--   IN  p_employee_identifier   VARCHAR(128)  exactly four digits, deliberately not unique (FR-004)
--   IN  p_role_code             VARCHAR(64)   the role to assign
--   IN  p_actor_user_id         BIGINT        the person performing the create
--   IN  p_password_hash         VARCHAR(128)  the salted hash of the freshly generated PIN
--   IN  p_password_salt         VARBINARY(32)
--   IN  p_change_group_id       CHAR(36)      shared by every audit row this act writes; generated if absent
--
--   OUT the affected-row count, through the non-query seam. This is a write and returns no result set, so a
--       caller that needs the new account's id reads it back through sp_user_management_list.
--
-- The actor's own rung is read here, from auth_roles_assignments joined to auth_roles_catalog. It is never
-- taken from the caller: a caller that could assert its own rank could grant itself anything (FR-025).
--
-- Refusals, reported as a stable token so the caller can answer in the reader's words:
--   mtm_role_unknown  the requested role is not in the catalogue
--   mtm_rank_denied   the requested role's rung is above the actor's own
--
-- A duplicate sign-in name is deliberately NOT translated. MySQL's own duplicate-key error passes through the
-- resignal, and the caller reads `1062` from the provider and returns the typed "already taken" result
-- (FR-008). That keeps the 1062 answer as one behaviour with one source, and it is why there is no blind retry
-- anywhere in this path.
--
-- The audit rows: one per written field, sharing `p_change_group_id`, so one act reads as one act (FR-108,
-- FR-110). Writing the account's initial credential gets a row too, with **both value columns null**, for the
-- same reason a reset does (FR-111): the PIN is never stored, so there is no before or after value to record.
--
-- Stored credential: only the salted hash of the PIN reaches this procedure. The PIN itself is never sent to
-- the store and never appears in any readable row (FR-030).
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_user_management_create;

-- DELIMITER because this body is compound; see the note in
-- sp_auth_temporary_credential_attempt_record/create.sql.
DELIMITER $$

CREATE PROCEDURE sp_user_management_create(
    IN p_username VARCHAR(128),
    IN p_first_name VARCHAR(128),
    IN p_last_name VARCHAR(128),
    IN p_employee_identifier VARCHAR(128),
    IN p_role_code VARCHAR(64),
    IN p_actor_user_id BIGINT,
    IN p_password_hash VARCHAR(128),
    IN p_password_salt VARBINARY(32),
    IN p_change_group_id CHAR(36)
)
BEGIN
    DECLARE v_actor_rank INT DEFAULT 0;
    DECLARE v_requested_rank INT DEFAULT NULL;
    DECLARE v_role_id BIGINT DEFAULT NULL;
    DECLARE v_new_user_id BIGINT DEFAULT NULL;
    DECLARE v_username VARCHAR(128);
    DECLARE v_first_name VARCHAR(128);
    DECLARE v_last_name VARCHAR(128);
    DECLARE v_employee_identifier VARCHAR(128);
    DECLARE v_display_name VARCHAR(256);
    DECLARE v_change_group_id CHAR(36);
    DECLARE v_actor_display_name VARCHAR(256) DEFAULT '';
    DECLARE v_actor_employee_identifier VARCHAR(128) DEFAULT '';
    DECLARE v_actor_role_code VARCHAR(64) DEFAULT '';

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SET v_username = UPPER(TRIM(p_username));
    SET v_first_name = TRIM(p_first_name);
    SET v_last_name = TRIM(p_last_name);
    SET v_employee_identifier = NULLIF(TRIM(p_employee_identifier), '');
    SET v_display_name = CONCAT(v_first_name, ' ', v_last_name);
    SET v_change_group_id = COALESCE(NULLIF(TRIM(p_change_group_id), ''), UUID());

    START TRANSACTION;

    SET v_requested_rank = (
        SELECT r.role_rank
        FROM auth_roles_catalog r
        WHERE r.role_code = TRIM(p_role_code)
        LIMIT 1
    );

    IF v_requested_rank IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_role_unknown';
    END IF;

    SET v_role_id = (
        SELECT r.id
        FROM auth_roles_catalog r
        WHERE r.role_code = TRIM(p_role_code)
        LIMIT 1
    );

    SELECT COALESCE(MAX(r.role_rank), 0)
    INTO v_actor_rank
    FROM auth_roles_assignments ra
    INNER JOIN auth_roles_catalog r ON r.id = ra.role_id
    WHERE ra.user_id = p_actor_user_id;

    IF v_requested_rank > v_actor_rank THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_rank_denied';
    END IF;

    SELECT COALESCE(u.display_name, ''), COALESCE(u.employee_identifier, '')
    INTO v_actor_display_name, v_actor_employee_identifier
    FROM core_users_profiles u
    WHERE u.id = p_actor_user_id;

    SELECT COALESCE(r.role_code, '')
    INTO v_actor_role_code
    FROM auth_roles_assignments ra
    INNER JOIN auth_roles_catalog r ON r.id = ra.role_id
    WHERE ra.user_id = p_actor_user_id
    ORDER BY r.role_rank DESC, r.role_code ASC
    LIMIT 1;

    INSERT INTO core_users_profiles (
        public_id,
        username_normalized,
        first_name,
        last_name,
        password_hash,
        password_salt,
        require_password_change,
        temporary_credential_failed_attempts,
        display_name,
        employee_identifier,
        is_active,
        created_utc,
        updated_utc
    )
    VALUES (
        UUID(),
        v_username,
        v_first_name,
        v_last_name,
        p_password_hash,
        p_password_salt,
        1,
        0,
        v_display_name,
        v_employee_identifier,
        1,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    );

    SET v_new_user_id = LAST_INSERT_ID();

    INSERT INTO auth_roles_assignments (
        public_id,
        user_id,
        role_id,
        assigned_utc,
        assigned_by_user_id
    )
    VALUES (
        UUID(),
        v_new_user_id,
        v_role_id,
        UTC_TIMESTAMP(),
        p_actor_user_id
    );

    INSERT INTO auth_user_management_audit (
        public_id,
        change_group_id,
        target_user_id,
        field_name,
        previous_value,
        changed_value,
        actor_user_id,
        actor_display_name,
        actor_employee_identifier,
        actor_role_code,
        occurred_utc
    )
    VALUES
        (UUID(), v_change_group_id, v_new_user_id, 'username_normalized', NULL, v_username, p_actor_user_id, v_actor_display_name, v_actor_employee_identifier, v_actor_role_code, UTC_TIMESTAMP()),
        (UUID(), v_change_group_id, v_new_user_id, 'first_name', NULL, v_first_name, p_actor_user_id, v_actor_display_name, v_actor_employee_identifier, v_actor_role_code, UTC_TIMESTAMP()),
        (UUID(), v_change_group_id, v_new_user_id, 'last_name', NULL, v_last_name, p_actor_user_id, v_actor_display_name, v_actor_employee_identifier, v_actor_role_code, UTC_TIMESTAMP()),
        (UUID(), v_change_group_id, v_new_user_id, 'display_name', NULL, v_display_name, p_actor_user_id, v_actor_display_name, v_actor_employee_identifier, v_actor_role_code, UTC_TIMESTAMP()),
        (UUID(), v_change_group_id, v_new_user_id, 'employee_identifier', NULL, v_employee_identifier, p_actor_user_id, v_actor_display_name, v_actor_employee_identifier, v_actor_role_code, UTC_TIMESTAMP()),
        (UUID(), v_change_group_id, v_new_user_id, 'role_code', NULL, TRIM(p_role_code), p_actor_user_id, v_actor_display_name, v_actor_employee_identifier, v_actor_role_code, UTC_TIMESTAMP()),
        (UUID(), v_change_group_id, v_new_user_id, 'is_active', NULL, '1', p_actor_user_id, v_actor_display_name, v_actor_employee_identifier, v_actor_role_code, UTC_TIMESTAMP()),
        (UUID(), v_change_group_id, v_new_user_id, 'password_hash', NULL, NULL, p_actor_user_id, v_actor_display_name, v_actor_employee_identifier, v_actor_role_code, UTC_TIMESTAMP());

    COMMIT;
END$$

DELIMITER ;

-- Stored Procedure: sp_user_management_get
-- Engine: MySQL 5.7
-- Feature: 006-user-management-and-permissions (task T010)
--
-- Purpose: one person, for the person's page.
--
-- Contract:
--   IN  p_user_id   BIGINT   the account to read
--
--   OUT zero or one row:
--     user_id, public_id, username_normalized, first_name, last_name, display_name, employee_identifier,
--     role_code, role_name, role_rank, is_active, temporary_credential_failed_attempts
--
-- Reading is never refused (FR-027). This procedure takes no actor and applies no rank rule, so an account
-- that outranks the reader is still readable; the page shows its fields and actions as unavailable and states
-- the reason in words, which is a presentation decision made from `role_rank` rather than from a refusal here.
--
-- `temporary_credential_failed_attempts` is returned because the person's page is where an administrator
-- explains why a handed-over PIN stopped working. It is a count and nothing more: it is never presented as a
-- record of who tried (FR-043).
--
-- Highest rung wins when a person holds more than one role (FR-017), then the code breaks the tie. The pick is
-- a correlated subquery rather than a window function because the engine baseline is MySQL 5.7.
--
-- Zero rows means no such account, which the caller reports as a not-found state rather than an empty page.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_user_management_get;

CREATE PROCEDURE sp_user_management_get(
    IN p_user_id BIGINT
)
SELECT u.id AS user_id,
       u.public_id,
       u.username_normalized,
       u.first_name,
       u.last_name,
       u.display_name,
       COALESCE(u.employee_identifier, '') AS employee_identifier,
       COALESCE(r.role_code, '') AS role_code,
       COALESCE(r.role_name, '') AS role_name,
       COALESCE(r.role_rank, 0) AS role_rank,
       u.is_active,
       u.temporary_credential_failed_attempts
FROM core_users_profiles u
LEFT JOIN auth_roles_assignments ra
    ON ra.user_id = u.id
   AND ra.id = (
        SELECT ra2.id
        FROM auth_roles_assignments ra2
        INNER JOIN auth_roles_catalog r2 ON r2.id = ra2.role_id
        WHERE ra2.user_id = u.id
        ORDER BY r2.role_rank DESC, r2.role_code ASC
        LIMIT 1
   )
LEFT JOIN auth_roles_catalog r ON r.id = ra.role_id
WHERE u.id = p_user_id
LIMIT 1;

-- Stored Procedure: sp_user_management_list
-- Engine: MySQL 5.7
-- Feature: 006-user-management-and-permissions (task T010)
--
-- Purpose: the roster. Search across the four facts people actually know and filter by role, defaulting to
--          everyone.
--
-- Contract:
--   IN  p_search      VARCHAR(128)   free text, or NULL/empty to match everyone
--   IN  p_role_code   VARCHAR(64)    a role code, or NULL/empty to filter nothing
--
--   OUT one row per person:
--     user_id, public_id, username_normalized, display_name, employee_identifier, role_code, role_name,
--     role_rank, is_active
--
-- Everyone is returned by default and switched-off people are included (FR-088): a deactivated account is
-- still a person the reader needs to find and switch back on.
--
-- Search matches the sign-in name, the display name, the employee number and the role name (FR-086). The
-- comparison is case-insensitive because every column here is utf8mb4_unicode_ci.
--
-- Reading is never refused (FR-027): this procedure takes no actor and applies no rank rule, so an account the
-- reader cannot change is still readable. The unchangeable case is decided where the change is written, not
-- here.
--
-- Where a person holds more than one role, the highest rung decides (FR-017), then the code breaks a tie so the
-- answer is deterministic. The pick is a correlated subquery rather than a window function because this repo's
-- engine baseline is MySQL 5.7, which has no ROW_NUMBER().
--
-- No per-person permission row and no sample row is ever produced here. An unreachable store is reported by the
-- caller as an unavailable state with a retry, never as an empty roster (FR-096).
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_user_management_list;

CREATE PROCEDURE sp_user_management_list(
    IN p_search VARCHAR(128),
    IN p_role_code VARCHAR(64)
)
SELECT u.id AS user_id,
       u.public_id,
       u.username_normalized,
       u.display_name,
       COALESCE(u.employee_identifier, '') AS employee_identifier,
       COALESCE(r.role_code, '') AS role_code,
       COALESCE(r.role_name, '') AS role_name,
       COALESCE(r.role_rank, 0) AS role_rank,
       u.is_active
FROM core_users_profiles u
LEFT JOIN auth_roles_assignments ra
    ON ra.user_id = u.id
   AND ra.id = (
        SELECT ra2.id
        FROM auth_roles_assignments ra2
        INNER JOIN auth_roles_catalog r2 ON r2.id = ra2.role_id
        WHERE ra2.user_id = u.id
        ORDER BY r2.role_rank DESC, r2.role_code ASC
        LIMIT 1
   )
LEFT JOIN auth_roles_catalog r ON r.id = ra.role_id
WHERE (
        p_search IS NULL
        OR TRIM(p_search) = ''
        OR u.username_normalized LIKE CONCAT('%', TRIM(p_search), '%')
        OR u.display_name LIKE CONCAT('%', TRIM(p_search), '%')
        OR COALESCE(u.employee_identifier, '') LIKE CONCAT('%', TRIM(p_search), '%')
        OR COALESCE(r.role_name, '') LIKE CONCAT('%', TRIM(p_search), '%')
    )
  AND (
        p_role_code IS NULL
        OR TRIM(p_role_code) = ''
        OR r.role_code = TRIM(p_role_code)
    )
ORDER BY u.display_name ASC, u.username_normalized ASC;

-- Stored Procedure: sp_user_management_reset_password
-- Engine: MySQL 5.7
-- Feature: 006-user-management-and-permissions (task T013)
--
-- Purpose: issue a fresh one-time PIN for one account, as one act.
--
-- Contract:
--   IN  p_user_id           BIGINT        the account being reset
--   IN  p_actor_user_id     BIGINT        the person performing the reset
--   IN  p_password_hash     VARCHAR(128)  the salted hash of the freshly generated PIN
--   IN  p_password_salt     VARBINARY(32)
--   IN  p_change_group_id   CHAR(36)      the audit grouping id; generated if absent
--
--   OUT the affected-row count, through the non-query seam
--
-- What it does, in one transaction:
--   * refuses a target that outranks the actor (mtm_target_outranks_actor, FR-019, FR-025);
--   * stores the hash and salt of the freshly generated PIN — the salted hash only, never the PIN itself,
--     which is what makes "readable back" mean plaintext in FR-030;
--   * sets `require_password_change = 1`, so the PIN must be replaced at the next sign-in (FR-029);
--   * clears the wrong-attempt count, because a fresh reset is one of only two things that clears it (FR-039);
--   * leaves `is_active` exactly as it was — a reset on a switched-off account does not switch it back on
--     (FR-029);
--   * leaves every existing session for that person alone, because a reset ends none (FR-035);
--   * writes ONE audit row recording the reset with **both value columns null**, because the credential is
--     never stored and there is no before or after value to record (FR-111).
--
-- A second reset simply replaces the earlier PIN, and the screen says so (FR-032): nothing here refuses a
-- repeated reset, and no history row is written for the PIN itself, so the earlier PIN is unrecoverable.
--
-- The actor's rung is read here and never taken from its caller (FR-025).
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_user_management_reset_password;

-- DELIMITER because this body is compound; see the note in
-- sp_auth_temporary_credential_attempt_record/create.sql.
DELIMITER $$

CREATE PROCEDURE sp_user_management_reset_password(
    IN p_user_id BIGINT,
    IN p_actor_user_id BIGINT,
    IN p_password_hash VARCHAR(128),
    IN p_password_salt VARBINARY(32),
    IN p_change_group_id CHAR(36)
)
BEGIN
    DECLARE v_actor_rank INT DEFAULT 0;
    DECLARE v_target_rank INT DEFAULT 0;
    DECLARE v_target_exists INT DEFAULT 0;
    DECLARE v_change_group_id CHAR(36);
    DECLARE v_actor_display_name VARCHAR(256) DEFAULT '';
    DECLARE v_actor_employee_identifier VARCHAR(128) DEFAULT '';
    DECLARE v_actor_role_code VARCHAR(64) DEFAULT '';

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SET v_change_group_id = COALESCE(NULLIF(TRIM(p_change_group_id), ''), UUID());

    START TRANSACTION;

    SELECT COUNT(*) INTO v_target_exists
    FROM core_users_profiles u
    WHERE u.id = p_user_id;

    SELECT COALESCE(MAX(r.role_rank), 0)
    INTO v_actor_rank
    FROM auth_roles_assignments ra
    INNER JOIN auth_roles_catalog r ON r.id = ra.role_id
    WHERE ra.user_id = p_actor_user_id;

    SELECT COALESCE(MAX(r.role_rank), 0)
    INTO v_target_rank
    FROM auth_roles_assignments ra
    INNER JOIN auth_roles_catalog r ON r.id = ra.role_id
    WHERE ra.user_id = p_user_id;

    IF v_target_exists > 0 AND v_target_rank > v_actor_rank THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_target_outranks_actor';
    END IF;

    SELECT COALESCE(u.display_name, ''), COALESCE(u.employee_identifier, '')
    INTO v_actor_display_name, v_actor_employee_identifier
    FROM core_users_profiles u
    WHERE u.id = p_actor_user_id;

    SELECT COALESCE(r.role_code, '')
    INTO v_actor_role_code
    FROM auth_roles_assignments ra
    INNER JOIN auth_roles_catalog r ON r.id = ra.role_id
    WHERE ra.user_id = p_actor_user_id
    ORDER BY r.role_rank DESC, r.role_code ASC
    LIMIT 1;

    UPDATE core_users_profiles
    SET password_hash = p_password_hash,
        password_salt = p_password_salt,
        require_password_change = 1,
        temporary_credential_failed_attempts = 0,
        updated_utc = UTC_TIMESTAMP()
    WHERE id = p_user_id;

    INSERT INTO auth_user_management_audit (
        public_id,
        change_group_id,
        target_user_id,
        field_name,
        previous_value,
        changed_value,
        actor_user_id,
        actor_display_name,
        actor_employee_identifier,
        actor_role_code,
        occurred_utc
    )
    VALUES (
        UUID(),
        v_change_group_id,
        p_user_id,
        'password_hash',
        NULL,
        NULL,
        p_actor_user_id,
        v_actor_display_name,
        v_actor_employee_identifier,
        v_actor_role_code,
        UTC_TIMESTAMP()
    );

    COMMIT;
END$$

DELIMITER ;

-- Stored Procedure: sp_user_management_update
-- Engine: MySQL 5.7
-- Feature: 006-user-management-and-permissions (task T012)
--
-- Purpose: correct one person's account, and switch it off or back on, as one act.
--
-- Contract:
--   IN  p_user_id               BIGINT        the account being changed
--   IN  p_username              VARCHAR(128)  the sign-in name as typed; stored upper case
--   IN  p_first_name            VARCHAR(128)
--   IN  p_last_name             VARCHAR(128)
--   IN  p_employee_identifier   VARCHAR(128)
--   IN  p_role_code             VARCHAR(64)   the role the account should hold
--   IN  p_is_active             TINYINT       the active state the account should end in
--   IN  p_actor_user_id         BIGINT        the person performing the change
--   IN  p_change_group_id       CHAR(36)      shared by every audit row this act writes; generated if absent
--
--   OUT the affected-row count, through the non-query seam
--
-- The actor's own rung is read here, never taken from the caller (FR-025).
--
-- Refusals, reported as a stable token:
--   mtm_role_unknown            the requested role is not in the catalogue
--   mtm_target_outranks_actor   the account being changed ranks strictly above the actor (FR-019)
--   mtm_rank_denied             the requested role's rung is above the actor's own (FR-022)
--   mtm_self_deactivate_denied  the actor is switching off their own account (FR-023)
--   mtm_self_rename_denied      the actor is changing the sign-in name of the account they are signed in as
--                               (FR-024)
--
-- A peer is editable, because a person's own rank is not above their own rank (FR-020), and a person's own
-- password change is unaffected by any of this (FR-021).
--
-- The audit rows: one per field that ACTUALLY changed, sharing `p_change_group_id`. A save that changed
-- nothing writes no audit row at all (FR-026, FR-110). The comparison is done here rather than by the caller,
-- so "changed nothing" cannot be mis-reported by a form that sent every field back.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_user_management_update;

-- DELIMITER because this body is compound; see the note in
-- sp_auth_temporary_credential_attempt_record/create.sql.
DELIMITER $$

CREATE PROCEDURE sp_user_management_update(
    IN p_user_id BIGINT,
    IN p_username VARCHAR(128),
    IN p_first_name VARCHAR(128),
    IN p_last_name VARCHAR(128),
    IN p_employee_identifier VARCHAR(128),
    IN p_role_code VARCHAR(64),
    IN p_is_active TINYINT,
    IN p_actor_user_id BIGINT,
    IN p_change_group_id CHAR(36)
)
BEGIN
    DECLARE v_actor_rank INT DEFAULT 0;
    DECLARE v_target_rank INT DEFAULT 0;
    DECLARE v_requested_rank INT DEFAULT NULL;
    DECLARE v_role_id BIGINT DEFAULT NULL;
    DECLARE v_current_role_id BIGINT DEFAULT NULL;
    DECLARE v_current_role_code VARCHAR(64) DEFAULT '';
    DECLARE v_username VARCHAR(128);
    DECLARE v_first_name VARCHAR(128);
    DECLARE v_last_name VARCHAR(128);
    DECLARE v_employee_identifier VARCHAR(128);
    DECLARE v_display_name VARCHAR(256);
    DECLARE v_change_group_id CHAR(36);
    DECLARE v_current_username VARCHAR(128) DEFAULT '';
    DECLARE v_current_first_name VARCHAR(128) DEFAULT '';
    DECLARE v_current_last_name VARCHAR(128) DEFAULT '';
    DECLARE v_current_display_name VARCHAR(256) DEFAULT '';
    DECLARE v_current_employee_identifier VARCHAR(128) DEFAULT NULL;
    DECLARE v_current_is_active TINYINT DEFAULT 0;
    DECLARE v_actor_display_name VARCHAR(256) DEFAULT '';
    DECLARE v_actor_employee_identifier VARCHAR(128) DEFAULT '';
    DECLARE v_actor_role_code VARCHAR(64) DEFAULT '';

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SET v_username = UPPER(TRIM(p_username));
    SET v_first_name = TRIM(p_first_name);
    SET v_last_name = TRIM(p_last_name);
    SET v_employee_identifier = NULLIF(TRIM(p_employee_identifier), '');
    SET v_display_name = CONCAT(v_first_name, ' ', v_last_name);
    SET v_change_group_id = COALESCE(NULLIF(TRIM(p_change_group_id), ''), UUID());

    START TRANSACTION;

    SET v_requested_rank = (
        SELECT r.role_rank
        FROM auth_roles_catalog r
        WHERE r.role_code = TRIM(p_role_code)
        LIMIT 1
    );

    IF v_requested_rank IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_role_unknown';
    END IF;

    SET v_role_id = (
        SELECT r.id
        FROM auth_roles_catalog r
        WHERE r.role_code = TRIM(p_role_code)
        LIMIT 1
    );

    SELECT COALESCE(MAX(r.role_rank), 0)
    INTO v_actor_rank
    FROM auth_roles_assignments ra
    INNER JOIN auth_roles_catalog r ON r.id = ra.role_id
    WHERE ra.user_id = p_actor_user_id;

    -- The target's rung is checked BEFORE the requested role's, so the reader is told the real reason. A reader
    -- editing somebody who outranks them must hear "this account outranks you", not "you cannot assign that
    -- role": the second is true but describes the smaller half of the problem, and the page states the first.
    SELECT COALESCE(r.role_rank, 0), COALESCE(r.id, 0), COALESCE(r.role_code, '')
    INTO v_target_rank, v_current_role_id, v_current_role_code
    FROM auth_roles_assignments ra
    INNER JOIN auth_roles_catalog r ON r.id = ra.role_id
    WHERE ra.user_id = p_user_id
    ORDER BY r.role_rank DESC, r.role_code ASC
    LIMIT 1;

    IF v_target_rank > v_actor_rank THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_target_outranks_actor';
    END IF;

    IF v_requested_rank > v_actor_rank THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_rank_denied';
    END IF;

    SELECT COALESCE(u.username_normalized, ''),
           COALESCE(u.first_name, ''),
           COALESCE(u.last_name, ''),
           COALESCE(u.display_name, ''),
           u.employee_identifier,
           u.is_active
    INTO v_current_username,
         v_current_first_name,
         v_current_last_name,
         v_current_display_name,
         v_current_employee_identifier,
         v_current_is_active
    FROM core_users_profiles u
    WHERE u.id = p_user_id;

    IF p_is_active = 0 AND p_user_id = p_actor_user_id THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_self_deactivate_denied';
    END IF;

    IF v_username <> v_current_username AND p_user_id = p_actor_user_id THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_self_rename_denied';
    END IF;

    SELECT COALESCE(u.display_name, ''), COALESCE(u.employee_identifier, '')
    INTO v_actor_display_name, v_actor_employee_identifier
    FROM core_users_profiles u
    WHERE u.id = p_actor_user_id;

    SELECT COALESCE(r.role_code, '')
    INTO v_actor_role_code
    FROM auth_roles_assignments ra
    INNER JOIN auth_roles_catalog r ON r.id = ra.role_id
    WHERE ra.user_id = p_actor_user_id
    ORDER BY r.role_rank DESC, r.role_code ASC
    LIMIT 1;

    UPDATE core_users_profiles
    SET username_normalized = v_username,
        first_name = v_first_name,
        last_name = v_last_name,
        display_name = v_display_name,
        employee_identifier = v_employee_identifier,
        is_active = p_is_active,
        updated_utc = UTC_TIMESTAMP()
    WHERE id = p_user_id;

    IF v_role_id <> v_current_role_id OR v_current_role_id = 0 THEN
        DELETE FROM auth_roles_assignments WHERE user_id = p_user_id;

        INSERT INTO auth_roles_assignments (
            public_id,
            user_id,
            role_id,
            assigned_utc,
            assigned_by_user_id
        )
        VALUES (
            UUID(),
            p_user_id,
            v_role_id,
            UTC_TIMESTAMP(),
            p_actor_user_id
        );
    END IF;

    INSERT INTO auth_user_management_audit (
        public_id,
        change_group_id,
        target_user_id,
        field_name,
        previous_value,
        changed_value,
        actor_user_id,
        actor_display_name,
        actor_employee_identifier,
        actor_role_code,
        occurred_utc
    )
    SELECT UUID(),
           v_change_group_id,
           p_user_id,
           changed.field_name,
           changed.previous_value,
           changed.changed_value,
           p_actor_user_id,
           v_actor_display_name,
           v_actor_employee_identifier,
           v_actor_role_code,
           UTC_TIMESTAMP()
    FROM (
        SELECT 'username_normalized' AS field_name, v_current_username AS previous_value, v_username AS changed_value
        UNION ALL
        SELECT 'first_name', v_current_first_name, v_first_name
        UNION ALL
        SELECT 'last_name', v_current_last_name, v_last_name
        UNION ALL
        SELECT 'display_name', v_current_display_name, v_display_name
        UNION ALL
        SELECT 'employee_identifier', COALESCE(v_current_employee_identifier, ''), COALESCE(v_employee_identifier, '')
        UNION ALL
        SELECT 'role_code', v_current_role_code, TRIM(p_role_code)
        UNION ALL
        -- The two active flags are written as literals rather than CAST(... AS CHAR). A CAST takes the
        -- CONNECTION's collation while the routine's own variables take the database's, and MySQL 5.7 refuses
        -- to UNION the two: "Illegal mix of collations for operation 'UNION'". That is a real failure, not a
        -- theoretical one: on a utf8mb4 connection this save failed every time. A literal is coercible, so it
        -- adopts the branches' own collation and the save works whatever connection calls it.
        SELECT 'is_active', IF(v_current_is_active <> 0, '1', '0'), IF(p_is_active <> 0, '1', '0')
    ) AS changed
    WHERE NOT (changed.previous_value <=> changed.changed_value);

    COMMIT;
END$$

DELIMITER ;

-- Create procedure: sp_waitlist_defect_types_delete
-- Engine: MySQL 5.7
-- Purpose: Remove an NCM defect type from waitlist_defect_types by id (hard delete; the managed
--          defect list is a small reference catalog, so removal is a true delete rather than a soft flag).

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_defect_types_delete;

CREATE PROCEDURE sp_waitlist_defect_types_delete(
    IN p_id BIGINT
)
DELETE FROM waitlist_defect_types
WHERE id = p_id;

-- Create procedure: sp_waitlist_defect_types_get_all
-- Engine: MySQL 5.7
-- Purpose: List active NCM defect types (for the worker picker and the Module_Settings editor),
--          ordered by sort_order then defect_name.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_defect_types_get_all;

CREATE PROCEDURE sp_waitlist_defect_types_get_all()
SELECT
    id,
    public_id,
    defect_name,
    description,
    sort_order,
    is_active,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
FROM waitlist_defect_types
WHERE is_active = 1
ORDER BY sort_order ASC, defect_name ASC;

-- Create procedure: sp_waitlist_defect_types_insert
-- Engine: MySQL 5.7
-- Purpose: Insert a new active NCM defect type into waitlist_defect_types.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_defect_types_insert;

CREATE PROCEDURE sp_waitlist_defect_types_insert(
    IN p_defect_name VARCHAR(100),
    IN p_description VARCHAR(500),
    IN p_sort_order INT,
    IN p_created_by_user_id BIGINT
)
INSERT INTO waitlist_defect_types (
    public_id, defect_name, description, sort_order, is_active,
    created_by_user_id, updated_by_user_id, created_utc, updated_utc
)
VALUES (
    UUID(),
    NULLIF(TRIM(p_defect_name), ''),
    NULLIF(TRIM(COALESCE(p_description, '')), ''),
    COALESCE(p_sort_order, 0),
    1,
    p_created_by_user_id,
    NULL,
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
);

-- Create procedure: sp_waitlist_defect_types_update
-- Engine: MySQL 5.7
-- Purpose: Update an existing NCM defect type in waitlist_defect_types by id.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_defect_types_update;

CREATE PROCEDURE sp_waitlist_defect_types_update(
    IN p_id BIGINT,
    IN p_defect_name VARCHAR(100),
    IN p_description VARCHAR(500),
    IN p_sort_order INT,
    IN p_updated_by_user_id BIGINT
)
UPDATE waitlist_defect_types
SET defect_name = NULLIF(TRIM(p_defect_name), ''),
    description = NULLIF(TRIM(COALESCE(p_description, '')), ''),
    sort_order = COALESCE(p_sort_order, 0),
    updated_by_user_id = p_updated_by_user_id,
    updated_utc = UTC_TIMESTAMP()
WHERE id = p_id;

-- Create procedure: sp_waitlist_request_audit_insert
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_audit_insert;

CREATE PROCEDURE sp_waitlist_request_audit_insert(
    IN p_request_public_id CHAR(36),
    IN p_from_status VARCHAR(32),
    IN p_to_status VARCHAR(32),
    IN p_event_type VARCHAR(32),
    IN p_actor_employee_number VARCHAR(32),
    IN p_actor_employee_name VARCHAR(128),
    IN p_details VARCHAR(255)
)
INSERT INTO waitlist_requests_audit (
    public_id,
    request_public_id,
    from_status,
    to_status,
    event_type,
    actor_employee_number,
    actor_employee_name,
    details,
    occurred_utc
)
VALUES (
    UUID(),
    TRIM(p_request_public_id),
    NULLIF(TRIM(COALESCE(p_from_status, '')) COLLATE utf8mb4_unicode_ci, ''),
    NULLIF(TRIM(COALESCE(p_to_status, '')) COLLATE utf8mb4_unicode_ci, ''),
    TRIM(p_event_type),
    NULLIF(TRIM(COALESCE(p_actor_employee_number, '')) COLLATE utf8mb4_unicode_ci, ''),
    NULLIF(TRIM(COALESCE(p_actor_employee_name, '')) COLLATE utf8mb4_unicode_ci, ''),
    NULLIF(TRIM(COALESCE(p_details, '')) COLLATE utf8mb4_unicode_ci, ''),
    UTC_TIMESTAMP()
);

-- Create procedure: sp_waitlist_request_audit_list
-- Engine: MySQL 5.7
-- Purpose: Read one request's persisted audit/history entries, oldest first, so the request page can show
--          the history that was recorded before this session. The write half is sp_waitlist_request_audit_insert.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_audit_list;

CREATE PROCEDURE sp_waitlist_request_audit_list(
    IN p_request_public_id CHAR(36)
)
SELECT
    id,
    public_id,
    request_public_id,
    from_status,
    to_status,
    event_type,
    actor_employee_number,
    actor_employee_name,
    details,
    occurred_utc
FROM waitlist_requests_audit
WHERE request_public_id = TRIM(p_request_public_id)
ORDER BY occurred_utc ASC, id ASC;

-- Create procedure: sp_waitlist_request_get
-- Engine: MySQL 5.7
-- Re-keyed by specs/004-unified-card-item-picker: a request is identified by the Category and the Item the
-- requester chose, and the two legacy columns this procedure used to return no longer exist (FR-004).

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_get;

CREATE PROCEDURE sp_waitlist_request_get(
    IN p_public_id CHAR(36)
)
SELECT
    id,
    public_id,
    building,
    work_center,
    category,
    item,
    input_value,
    active_setup_job_id,
    work_center_name,
    requester_employee_number,
    requester_employee_name,
    status,
    requested_utc,
    target_time_utc,
    is_overdue,
    assigned_material_handler,
    cancellation_reason,
    canceled_utc,
    canceled_by_employee_number,
    note,
    accepted_utc,
    completed_utc,
    released_utc,
    created_utc,
    updated_utc
FROM waitlist_requests_queue
WHERE public_id = p_public_id
LIMIT 1;

-- Create procedure: sp_waitlist_request_insert
-- Engine: MySQL 5.7
-- Re-keyed by specs/004-unified-card-item-picker: a new request is raised with the Category and the Item the
-- requester chose, and no longer with a request type or a subtype (FR-004). The lifecycle columns this
-- procedure writes are unchanged, because the request lifecycle does not change with the Item (FR-032).

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_insert;

CREATE PROCEDURE sp_waitlist_request_insert(
    IN p_public_id CHAR(36),
    IN p_building VARCHAR(64),
    IN p_work_center VARCHAR(64),
    IN p_category VARCHAR(16),
    IN p_item VARCHAR(64),
    IN p_input_value VARCHAR(255),
    IN p_active_setup_job_id VARCHAR(64),
    IN p_work_center_name VARCHAR(64),
    IN p_requester_employee_number VARCHAR(32),
    IN p_requester_employee_name VARCHAR(128),
    IN p_status VARCHAR(32),
    IN p_requested_utc DATETIME,
    IN p_target_time_utc DATETIME,
    IN p_is_overdue TINYINT,
    IN p_assigned_material_handler VARCHAR(128),
    IN p_cancellation_reason VARCHAR(255),
    IN p_note VARCHAR(500)
)
INSERT INTO waitlist_requests_queue (
    public_id,
    building,
    work_center,
    category,
    item,
    input_value,
    active_setup_job_id,
    work_center_name,
    requester_employee_number,
    requester_employee_name,
    status,
    requested_utc,
    target_time_utc,
    is_overdue,
    assigned_material_handler,
    cancellation_reason,
    note,
    created_utc,
    updated_utc
)
VALUES (
    TRIM(p_public_id),
    TRIM(p_building),
    TRIM(p_work_center),
    TRIM(p_category),
    TRIM(p_item),
    NULLIF(TRIM(COALESCE(p_input_value, '')) COLLATE utf8mb4_unicode_ci, ''),
    TRIM(p_active_setup_job_id),
    TRIM(p_work_center_name),
    TRIM(p_requester_employee_number),
    TRIM(p_requester_employee_name),
    COALESCE(NULLIF(TRIM(COALESCE(p_status, '')) COLLATE utf8mb4_unicode_ci, ''), 'Pending'),
    COALESCE(p_requested_utc, UTC_TIMESTAMP()),
    p_target_time_utc,
    COALESCE(p_is_overdue, 0),
    NULLIF(TRIM(COALESCE(p_assigned_material_handler, '')) COLLATE utf8mb4_unicode_ci, ''),
    NULLIF(TRIM(COALESCE(p_cancellation_reason, '')) COLLATE utf8mb4_unicode_ci, ''),
    NULLIF(TRIM(COALESCE(p_note, '')) COLLATE utf8mb4_unicode_ci, ''),
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
);

-- Create procedure: sp_waitlist_request_item_allotted_minutes_update
-- Engine: MySQL 5.7
-- Purpose: Write the configured allotment for one Item. This is the ONLY writer of a configured allotment:
--          the observed average is display data, and it is never written back as though it were configured
--          (FR-018, FR-019).
-- Contract: specs/004-unified-card-item-picker/contracts/item-configuration.md section 3.
--   p_item            the Item code whose allotment is being set.
--   p_allotted_minutes the configured minutes. A non-NULL value is clamped here to 1..1440 as well as in the
--                     caller (UrgencySettingsService.SetMaxAllottedAsync clamps to the same range). A NULL
--                     value clears the configured figure, so the Item falls back to the labelled 15-minute
--                     default rather than to a stale number.
--   p_updated_by_user_id the auditing column this table's row carries, matching the repo's other update
--                     procedures.
-- Returns the affected-row count through the caller's non-query seam, so a caller can tell an Item that does
-- not exist (0 rows) from one that does.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_item_allotted_minutes_update;

CREATE PROCEDURE sp_waitlist_request_item_allotted_minutes_update(
    IN p_item VARCHAR(64),
    IN p_allotted_minutes INT,
    IN p_updated_by_user_id BIGINT
)
UPDATE waitlist_request_item_configs
SET allotted_minutes = CASE
        WHEN p_allotted_minutes IS NULL THEN NULL
        ELSE LEAST(GREATEST(p_allotted_minutes, 1), 1440)
    END,
    updated_by_user_id = p_updated_by_user_id,
    updated_utc = UTC_TIMESTAMP()
WHERE item = TRIM(COALESCE(p_item, ''));

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

-- Create procedure: sp_waitlist_request_item_observed_average_get
-- Engine: MySQL 5.7
-- Purpose: Per Item, how long its completed requests actually took. This is the observed half of the pair a
--          screen shows beside the configured allotment, and it is derived and never stored (FR-019).
-- Contract: specs/004-unified-card-item-picker/contracts/item-configuration.md section 4.
--   item                    the Item code.
--   completed_request_count  how many of that Item's requests completed.
--   average_seconds         the mean of completed_utc - accepted_utc over those requests.
-- The rules, and why each is written the way it is:
--   * Completed requests only. The stored status vocabulary is unchanged (FR-032), so this filters on the
--     existing 'Completed' value and adds no status.
--   * A request missing either endpoint is excluded; the average never treats a missing timestamp as zero.
--   * released_utc is deliberately NOT read. A request that was released and later completed is one row with
--     one accepted -> completed pair, so it contributes exactly once. Reading released_utc as a second
--     interval is how such a request would be counted twice, which is why it is absent here.
--   * No time window. The average covers all of an Item's completed requests.
--   * An Item with no completed request yields no row, so the screen shows the configured minutes alone
--     rather than a fabricated zero (FR-026).
-- Read-only: this procedure writes nothing, and the observed value never reaches
-- sp_waitlist_request_item_allotted_minutes_update (FR-018).

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_item_observed_average_get;

CREATE PROCEDURE sp_waitlist_request_item_observed_average_get()
SELECT
    q.item,
    COUNT(*) AS completed_request_count,
    AVG(TIMESTAMPDIFF(SECOND, q.accepted_utc, q.completed_utc)) AS average_seconds
FROM waitlist_requests_queue q
WHERE q.status = 'Completed'
  AND q.accepted_utc IS NOT NULL
  AND q.completed_utc IS NOT NULL
GROUP BY q.item
ORDER BY q.item ASC;

-- Create procedure: sp_waitlist_request_list
-- Engine: MySQL 5.7
-- Re-keyed by specs/004-unified-card-item-picker: the list carries each request's Category and Item, which is
-- what the one card shape is built from, and no longer carries a request type or a subtype (FR-004).

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_list;

-- last_message_utc is the newest NoteUpdated entry written by a person, and it is what the card's
-- new-message marker compares against. The request's own updated_utc cannot serve that purpose: it moves
-- for every lifecycle change, so a system event (created, accepted, completed, canceled) would raise the
-- marker as if somebody had sent a message. Entries with no actor are system writes and are excluded.
CREATE PROCEDURE sp_waitlist_request_list(
    IN p_building VARCHAR(64),
    IN p_include_resolved TINYINT
)
SELECT
    q.id,
    q.public_id,
    q.building,
    q.work_center,
    q.category,
    q.item,
    q.input_value,
    q.active_setup_job_id,
    q.work_center_name,
    q.requester_employee_number,
    q.requester_employee_name,
    q.status,
    q.requested_utc,
    q.target_time_utc,
    q.is_overdue,
    q.assigned_material_handler,
    q.cancellation_reason,
    q.canceled_utc,
    q.canceled_by_employee_number,
    q.note,
    q.accepted_utc,
    q.completed_utc,
    q.released_utc,
    q.created_utc,
    q.updated_utc,
    (SELECT MAX(a.occurred_utc)
       FROM waitlist_requests_audit a
      WHERE a.request_public_id = q.public_id
        AND a.event_type = 'NoteUpdated'
        AND a.actor_employee_number IS NOT NULL) AS last_message_utc
FROM waitlist_requests_queue q
WHERE (p_building IS NULL OR TRIM(p_building) = '' OR q.building = TRIM(p_building))
  AND (p_include_resolved = 1 OR q.status IN ('Pending', 'Accepted'))
ORDER BY q.requested_utc ASC;

-- Create procedure: sp_waitlist_request_status_update
-- Engine: MySQL 5.7
-- The transition is CONDITIONAL on the state the caller read (FR-043). Two handlers acting on the same request
-- at the same moment must produce exactly one outcome: the second handler's accept matches no row, the update
-- reports zero affected rows, and the caller is told rather than shown a success it did not get. Matching on
-- public_id alone is what let a second accept overwrite the first handler's assignment.
-- p_expected_status is the state the caller saw. An absent or empty value keeps the unconditional behaviour for
-- a caller that genuinely has no state to guard on.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_status_update;

CREATE PROCEDURE sp_waitlist_request_status_update(
    IN p_public_id CHAR(36),
    IN p_status VARCHAR(32),
    IN p_assigned_material_handler VARCHAR(128),
    IN p_cancellation_reason VARCHAR(255),
    IN p_canceled_by_employee_number VARCHAR(32),
    IN p_note VARCHAR(500),
    IN p_expected_status VARCHAR(32)
)
UPDATE waitlist_requests_queue
SET status = TRIM(p_status),
    assigned_material_handler = NULLIF(TRIM(COALESCE(p_assigned_material_handler, '')) COLLATE utf8mb4_unicode_ci, ''),
    note = COALESCE(NULLIF(TRIM(COALESCE(p_note, '')) COLLATE utf8mb4_unicode_ci, ''), note),
    accepted_utc = CASE
        WHEN UPPER(TRIM(p_status)) = 'ACCEPTED'
            THEN COALESCE(accepted_utc, UTC_TIMESTAMP())
        ELSE accepted_utc
    END,
    completed_utc = CASE
        WHEN UPPER(TRIM(p_status)) = 'COMPLETED'
            THEN COALESCE(completed_utc, UTC_TIMESTAMP())
        ELSE completed_utc
    END,
    released_utc = CASE
        WHEN UPPER(TRIM(p_status)) = 'PENDING'
            THEN COALESCE(released_utc, UTC_TIMESTAMP())
        ELSE released_utc
    END,
    cancellation_reason = CASE
        WHEN UPPER(TRIM(p_status)) = 'CANCELED'
            THEN NULLIF(TRIM(COALESCE(p_cancellation_reason, '')) COLLATE utf8mb4_unicode_ci, '')
        ELSE NULL
    END,
    canceled_utc = CASE
        WHEN UPPER(TRIM(p_status)) = 'CANCELED'
            THEN COALESCE(canceled_utc, UTC_TIMESTAMP())
        ELSE NULL
    END,
    canceled_by_employee_number = CASE
        WHEN UPPER(TRIM(p_status)) = 'CANCELED'
            THEN NULLIF(TRIM(COALESCE(p_canceled_by_employee_number, '')) COLLATE utf8mb4_unicode_ci, '')
        ELSE NULL
    END,
    updated_utc = UTC_TIMESTAMP()
WHERE public_id = p_public_id
  AND (
      NULLIF(TRIM(COALESCE(p_expected_status, '')), '') IS NULL
      OR UPPER(TRIM(status)) = UPPER(TRIM(p_expected_status) COLLATE utf8mb4_unicode_ci)
  );
