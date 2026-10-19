SET FOREIGN_KEY_CHECKS=0;
SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

DROP DATABASE IF EXISTS `mtm_waitlist`;
CREATE DATABASE IF NOT EXISTS `mtm_waitlist` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `mtm_waitlist`;

DELIMITER $$
DROP PROCEDURE IF EXISTS `sp_auth_roles_list`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_auth_roles_list` ()   SELECT r.id AS role_id,
       r.role_code,
       r.role_name,
       r.role_rank
FROM auth_roles_catalog r
WHERE r.role_code <> 'admin'
ORDER BY r.role_rank DESC, r.role_code ASC$$

DROP PROCEDURE IF EXISTS `sp_auth_temporary_credential_attempt_record`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_auth_temporary_credential_attempt_record` (IN `p_user_id` BIGINT, IN `p_was_successful` TINYINT)   BEGIN
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

DROP PROCEDURE IF EXISTS `sp_auth_user_active_sessions_clear`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_auth_user_active_sessions_clear` (IN `p_user_id` BIGINT, IN `p_computer_id` BIGINT)   UPDATE user_active_sessions
SET
    revoked_utc = fn_server_utc_now(),
    is_active = 0
WHERE
    user_id = p_user_id
    AND computer_id = p_computer_id
    AND is_active = 1$$

DROP PROCEDURE IF EXISTS `sp_auth_user_active_sessions_get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_auth_user_active_sessions_get` (IN `p_user_id` BIGINT, IN `p_computer_id` BIGINT, IN `p_token_hash` CHAR(64))   SELECT
    s.public_id,
    s.issued_utc,
    s.expires_utc,
    s.source_label,
    CASE
        WHEN s.is_active = 1
            AND s.revoked_utc IS NULL
            AND fn_server_utc_now() < s.expires_utc THEN 1
        ELSE 0
    END AS is_valid
FROM
    user_active_sessions s
WHERE
    s.user_id = p_user_id
    AND s.computer_id = p_computer_id
    AND s.token_hash = LOWER(TRIM(p_token_hash))
LIMIT 1$$

DROP PROCEDURE IF EXISTS `sp_auth_user_active_sessions_list`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_auth_user_active_sessions_list` (IN `p_user_id` BIGINT, IN `p_computer_id` BIGINT)   SELECT
    s.public_id,
    s.user_id,
    u.display_name,
    s.computer_id,
    c.computer_name,
    s.issued_utc,
    s.expires_utc,
    s.source_label
FROM
    user_active_sessions s
    INNER JOIN core_users_profiles u ON u.id = s.user_id
    INNER JOIN core_computers_registry c ON c.id = s.computer_id
WHERE
    (p_user_id IS NULL OR s.user_id = p_user_id)
    AND (p_computer_id IS NULL OR s.computer_id = p_computer_id)
    AND s.is_active = 1
    AND s.revoked_utc IS NULL
    AND fn_server_utc_now() < s.expires_utc
ORDER BY
    s.issued_utc DESC,
    s.id DESC$$

DROP PROCEDURE IF EXISTS `sp_auth_user_active_sessions_upsert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_auth_user_active_sessions_upsert` (IN `p_public_id` CHAR(36), IN `p_user_id` BIGINT, IN `p_computer_id` BIGINT, IN `p_token_hash` CHAR(64), IN `p_token_salt` VARBINARY(32), IN `p_session_length_minutes` INT, IN `p_source_label` VARCHAR(32))   BEGIN
    DECLARE v_now_utc DATETIME;
    DECLARE v_expires_utc DATETIME;

    IF p_session_length_minutes IS NULL OR p_session_length_minutes <= 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_session_length_invalid';
    END IF;

    SET v_now_utc = fn_server_utc_now();
    SET v_expires_utc = DATE_ADD(v_now_utc, INTERVAL p_session_length_minutes MINUTE);

    INSERT INTO user_active_sessions (
        public_id,
        user_id,
        computer_id,
        token_hash,
        token_salt,
        issued_utc,
        expires_utc,
        revoked_utc,
        is_active,
        source_label,
        created_utc
    )
    VALUES (
        TRIM(p_public_id),
        p_user_id,
        p_computer_id,
        LOWER(TRIM(p_token_hash)),
        p_token_salt,
        v_now_utc,
        v_expires_utc,
        NULL,
        1,
        TRIM(p_source_label),
        v_now_utc
    )
    ON DUPLICATE KEY UPDATE
        public_id = VALUES(public_id),
        token_hash = VALUES(token_hash),
        token_salt = VALUES(token_salt),
        issued_utc = VALUES(issued_utc),
        expires_utc = VALUES(expires_utc),
        revoked_utc = NULL,
        is_active = 1,
        source_label = VALUES(source_label);
    
    
END$$

DROP PROCEDURE IF EXISTS `sp_auth_user_credential_get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_auth_user_credential_get` (IN `p_username` VARCHAR(128))   SELECT u.id,
       COALESCE(r.role_code, '') AS role_code,
       COALESCE(u.display_name, '') AS display_name,
       COALESCE(u.employee_identifier, '') AS employee_identifier,
       COALESCE(u.password_hash, '') AS password_hash,
       u.password_salt,
       u.require_password_change,
       u.temporary_credential_failed_attempts
FROM core_users_profiles u
LEFT JOIN auth_roles_assignments ra ON ra.user_id = u.id
LEFT JOIN auth_roles_catalog r ON r.id = ra.role_id
WHERE u.username_normalized = p_username
  AND u.is_active = 1
ORDER BY ra.assigned_utc DESC
LIMIT 1$$

DROP PROCEDURE IF EXISTS `sp_auth_user_password_set`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_auth_user_password_set` (IN `p_user_id` BIGINT, IN `p_password_hash` VARCHAR(128), IN `p_password_salt` VARBINARY(32))   UPDATE core_users_profiles
SET
    password_hash = TRIM(p_password_hash),
    password_salt = p_password_salt,
    require_password_change = 0,
    temporary_credential_failed_attempts = 0
WHERE
    id = p_user_id
    AND is_active = 1$$

DROP PROCEDURE IF EXISTS `sp_auth_user_previous_sign_ins_list`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_auth_user_previous_sign_ins_list` (IN `p_computer_id` BIGINT)   SELECT
    u.username_normalized,
    u.display_name
FROM
    user_active_sessions s
    INNER JOIN core_users_profiles u ON u.id = s.user_id
WHERE
    s.computer_id = p_computer_id
ORDER BY
    s.issued_utc DESC,
    s.id DESC
LIMIT 6$$

DROP PROCEDURE IF EXISTS `sp_auth_user_roles_get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_auth_user_roles_get` (IN `p_username` VARCHAR(128))   SELECT COALESCE(r.role_code, '') AS role_code,
       COALESCE(r.role_name, '') AS role_name,
       COALESCE(r.role_rank, 0) AS role_rank,
       ra.assigned_utc AS assigned_utc
FROM core_users_profiles u
JOIN auth_roles_assignments ra ON ra.user_id = u.id
JOIN auth_roles_catalog r ON r.id = ra.role_id
WHERE u.username_normalized = p_username
  AND u.is_active = 1
ORDER BY ra.assigned_utc DESC, r.role_code ASC$$

DROP PROCEDURE IF EXISTS `sp_auth_user_row_get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_auth_user_row_get` (IN `p_username` VARCHAR(128))   SELECT u.id,
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
LIMIT 1$$

DROP PROCEDURE IF EXISTS `sp_config_dunnage_types_visibility_delete_all`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_config_dunnage_types_visibility_delete_all` ()   DELETE FROM config_dunnage_types_visibility$$

DROP PROCEDURE IF EXISTS `sp_config_dunnage_types_visibility_get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_config_dunnage_types_visibility_get` ()   SELECT dunnage_type_id, is_visible
FROM config_dunnage_types_visibility$$

DROP PROCEDURE IF EXISTS `sp_config_dunnage_types_visibility_insert_row`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_config_dunnage_types_visibility_insert_row` (IN `p_dunnage_type_id` BIGINT, IN `p_dunnage_type_name` VARCHAR(128), IN `p_is_visible` TINYINT)   INSERT INTO config_dunnage_types_visibility (
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
)$$

DROP PROCEDURE IF EXISTS `sp_config_hot_workcenters_delete_for_computer`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_config_hot_workcenters_delete_for_computer` (IN `p_computer_id` BIGINT)   DELETE FROM config_computer_hot_work_centers
WHERE computer_id = p_computer_id$$

DROP PROCEDURE IF EXISTS `sp_config_hot_workcenters_get_for_computer`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_config_hot_workcenters_get_for_computer` (IN `p_computer_name` VARCHAR(128))   SELECT
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
ORDER BY cwhc.sort_rank ASC, swc.work_center_name ASC$$

DROP PROCEDURE IF EXISTS `sp_config_hot_workcenters_upsert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_config_hot_workcenters_upsert` (IN `p_computer_id` BIGINT, IN `p_work_center_id` BIGINT, IN `p_sort_rank` INT, IN `p_modified_by_user_id` BIGINT)   INSERT INTO config_computer_hot_work_centers (
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
    updated_utc = UTC_TIMESTAMP()$$

DROP PROCEDURE IF EXISTS `sp_config_images_locations_count_active_get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_config_images_locations_count_active_get` ()   SELECT COUNT(*) as count
FROM config_images_locations
WHERE is_active = 1$$

DROP PROCEDURE IF EXISTS `sp_config_images_locations_count_by_scope_get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_config_images_locations_count_by_scope_get` (IN `p_scope` VARCHAR(16))   SELECT COUNT(*) as count
FROM config_images_locations
WHERE scope = p_scope
  AND is_active = 1$$

DROP PROCEDURE IF EXISTS `sp_config_images_locations_deactivate_for_scope`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_config_images_locations_deactivate_for_scope` (IN `p_scope` VARCHAR(16), IN `p_user_id` BIGINT)   UPDATE config_images_locations
SET is_active = 0,
    updated_by_user_id = p_user_id,
    updated_utc = UTC_TIMESTAMP()
WHERE scope = p_scope
  AND is_active = 1$$

DROP PROCEDURE IF EXISTS `sp_config_images_locations_delete`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_config_images_locations_delete` (IN `p_scope` VARCHAR(16), IN `p_scope_item_id` VARCHAR(190), IN `p_user_id` BIGINT)   UPDATE config_images_locations
SET is_active = 0,
    updated_by_user_id = p_user_id,
    updated_utc = UTC_TIMESTAMP()
WHERE scope = p_scope
  AND scope_item_id = p_scope_item_id
  AND is_active = 1$$

DROP PROCEDURE IF EXISTS `sp_config_images_locations_delete_by_public_id`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_config_images_locations_delete_by_public_id` (IN `p_public_id` CHAR(36), IN `p_user_id` BIGINT)   UPDATE config_images_locations
SET is_active = 0,
    updated_by_user_id = p_user_id,
    updated_utc = UTC_TIMESTAMP()
WHERE public_id = p_public_id
  AND is_active = 1$$

DROP PROCEDURE IF EXISTS `sp_config_images_locations_get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_config_images_locations_get` (IN `p_scope` VARCHAR(16), IN `p_scope_item_id` VARCHAR(190))   SELECT
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
LIMIT 1$$

DROP PROCEDURE IF EXISTS `sp_config_images_locations_get_all`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_config_images_locations_get_all` ()   SELECT
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
WHERE is_active = 1$$

DROP PROCEDURE IF EXISTS `sp_config_images_locations_get_by_public_id`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_config_images_locations_get_by_public_id` (IN `p_public_id` CHAR(36))   SELECT
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
LIMIT 1$$

DROP PROCEDURE IF EXISTS `sp_config_images_locations_get_by_scope`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_config_images_locations_get_by_scope` (IN `p_scope` VARCHAR(16))   SELECT
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
ORDER BY updated_utc DESC$$

DROP PROCEDURE IF EXISTS `sp_config_images_locations_history_get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_config_images_locations_history_get` (IN `p_scope` VARCHAR(16), IN `p_scope_item_id` VARCHAR(190))   SELECT
    h.previous_image_path,
    h.new_image_path,
    h.changed_by_user_id,
    COALESCE(u.display_name, '') AS changed_by_display_name,
    h.changed_utc
FROM
    config_images_locations_history h
    LEFT JOIN core_users_profiles u ON u.id = h.changed_by_user_id
WHERE
    h.scope = p_scope
    AND h.scope_item_id = p_scope_item_id
ORDER BY
    h.changed_utc DESC,
    h.id DESC$$

DROP PROCEDURE IF EXISTS `sp_config_images_locations_insert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_config_images_locations_insert` (IN `p_public_id` CHAR(36), IN `p_scope` VARCHAR(16), IN `p_scope_item_id` VARCHAR(190), IN `p_image_path` VARCHAR(500), IN `p_user_id` BIGINT)   INSERT INTO config_images_locations (
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
)$$

DROP PROCEDURE IF EXISTS `sp_config_images_locations_paths_move`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_config_images_locations_paths_move` (IN `p_old_prefix` VARCHAR(64), IN `p_new_prefix` VARCHAR(64))   UPDATE config_images_locations
SET image_path = CONCAT(
        p_new_prefix,
        SUBSTRING(image_path, CHAR_LENGTH(p_old_prefix) + 1)
    )
WHERE is_active = 1
  AND p_old_prefix <> ''
  AND p_old_prefix <> p_new_prefix
  AND image_path LIKE CONCAT(p_old_prefix, '%')$$

DROP PROCEDURE IF EXISTS `sp_config_images_locations_purge_inactive`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_config_images_locations_purge_inactive` ()   DELETE FROM config_images_locations
WHERE is_active = 0$$

DROP PROCEDURE IF EXISTS `sp_config_images_locations_reactivate`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_config_images_locations_reactivate` (IN `p_scope` VARCHAR(16), IN `p_scope_item_id` VARCHAR(190), IN `p_image_path` VARCHAR(500), IN `p_user_id` BIGINT)   UPDATE config_images_locations
SET image_path = p_image_path,
    is_active = 1,
    updated_by_user_id = p_user_id,
    updated_utc = UTC_TIMESTAMP()
WHERE scope = p_scope
  AND scope_item_id = p_scope_item_id$$

DROP PROCEDURE IF EXISTS `sp_config_images_locations_recent_get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_config_images_locations_recent_get` (IN `p_max_rows` INT)   SELECT
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
LIMIT p_max_rows$$

DROP PROCEDURE IF EXISTS `sp_config_images_locations_scope_paths_get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_config_images_locations_scope_paths_get` (IN `p_scope` VARCHAR(16))   SELECT
    i.scope_item_id,
    i.image_path
FROM
    config_images_locations i
WHERE
    i.scope = p_scope
    AND i.is_active = 1
ORDER BY
    i.scope_item_id$$

DROP PROCEDURE IF EXISTS `sp_config_images_locations_status_get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_config_images_locations_status_get` (IN `p_scope` VARCHAR(16), IN `p_scope_item_id` VARCHAR(190))   SELECT is_active
FROM config_images_locations
WHERE scope = p_scope
  AND scope_item_id = p_scope_item_id
LIMIT 1$$

DROP PROCEDURE IF EXISTS `sp_config_images_locations_update`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_config_images_locations_update` (IN `p_scope` VARCHAR(16), IN `p_scope_item_id` VARCHAR(190), IN `p_image_path` VARCHAR(500), IN `p_user_id` BIGINT)   UPDATE config_images_locations
SET image_path = p_image_path,
    updated_by_user_id = p_user_id,
    updated_utc = UTC_TIMESTAMP()
WHERE scope = p_scope
  AND scope_item_id = p_scope_item_id
  AND is_active = 1$$

DROP PROCEDURE IF EXISTS `sp_config_permissions_feature_baseline_get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_config_permissions_feature_baseline_get` (IN `p_setting_key` VARCHAR(190))   SELECT r.role_code,
       r.role_name,
       v.setting_value_bool
FROM config_settings_values v
INNER JOIN auth_roles_catalog r
        ON v.scope_key = CONCAT('role:', r.role_code)
WHERE v.setting_key = p_setting_key
  AND v.scope_type = 'role'
  AND v.value_type = 'bool'
  AND v.setting_value_bool = 1
ORDER BY r.role_rank DESC, r.role_code ASC$$

DROP PROCEDURE IF EXISTS `sp_config_permissions_feature_holders_get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_config_permissions_feature_holders_get` (IN `p_setting_key` VARCHAR(190))   SELECT u.id AS user_id,
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
ORDER BY u.is_active DESC, u.display_name ASC$$

DROP PROCEDURE IF EXISTS `sp_config_permissions_reversal_get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_config_permissions_reversal_get` (IN `p_user_id` BIGINT)   SELECT h.setting_key,
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
ORDER BY h.id ASC$$

DROP PROCEDURE IF EXISTS `sp_config_permissions_user_get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_config_permissions_user_get` (IN `p_user_id` BIGINT)   BEGIN
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

DROP PROCEDURE IF EXISTS `sp_config_permissions_user_set`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_config_permissions_user_set` (IN `p_user_id` BIGINT, IN `p_actor_user_id` BIGINT, IN `p_changes_json` JSON)   BEGIN
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
            
            
            IF v_key = 'permission.admin.permissions' THEN
                SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_permission_gate_fixed:permission.admin.permissions';
            END IF;

            
            
            
            
            
            
            IF v_key NOT LIKE 'permission.%' THEN
                SET v_signal_text = CONCAT('mtm_permission_key_invalid:', v_key);
                SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = v_signal_text;
            END IF;

            SELECT COUNT(*), MAX(v.setting_value_bool)
            INTO v_existing_count, v_existing_bool
            FROM config_settings_values v
            WHERE v.setting_key = v_key
              AND v.scope_key = v_scope_key;

            
            
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

                
                
                
                
                
                
                
                SET v_setting_id = NULL;

                SELECT v.id
                INTO v_setting_id
                FROM config_settings_values v
                WHERE v.setting_key = v_key
                  AND v.scope_key = v_scope_key
                LIMIT 1;
            ELSE
                
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

DROP PROCEDURE IF EXISTS `sp_config_settings_get_effective`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_config_settings_get_effective` (IN `p_setting_key` VARCHAR(190), IN `p_computer_id` BIGINT, IN `p_user_id` BIGINT)   SELECT
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
LIMIT 1$$

DROP PROCEDURE IF EXISTS `sp_config_settings_upsert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_config_settings_upsert` (IN `p_setting_key` VARCHAR(190), IN `p_scope_type` VARCHAR(16), IN `p_computer_id` BIGINT, IN `p_user_id` BIGINT, IN `p_setting_value` TEXT, IN `p_setting_value_int` BIGINT, IN `p_setting_value_bool` TINYINT, IN `p_setting_value_decimal` DECIMAL(18,6), IN `p_setting_value_datetime_utc` DATETIME, IN `p_value_type` VARCHAR(32), IN `p_updated_by_user_id` BIGINT)   INSERT INTO config_settings_values (
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
    updated_utc = UTC_TIMESTAMP()$$

DROP PROCEDURE IF EXISTS `sp_config_settings_values_delete`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_config_settings_values_delete` (IN `p_setting_key` VARCHAR(190), IN `p_scope_key` VARCHAR(255))   DELETE FROM config_settings_values
WHERE setting_key = p_setting_key
  AND scope_key = p_scope_key$$

DROP PROCEDURE IF EXISTS `sp_config_settings_values_get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_config_settings_values_get` (IN `p_setting_key` VARCHAR(190), IN `p_scope_key` VARCHAR(255))   SELECT
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
LIMIT 1$$

DROP PROCEDURE IF EXISTS `sp_core_computers_registry_delete`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_core_computers_registry_delete` (IN `p_id` BIGINT)   DELETE FROM core_computers_registry
WHERE id = p_id$$

DROP PROCEDURE IF EXISTS `sp_core_computers_registry_display_name_get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_core_computers_registry_display_name_get` (IN `p_display_name` VARCHAR(128))   SELECT
    id,
    computer_name,
    display_name,
    is_registered
FROM core_computers_registry
WHERE display_name = p_display_name
LIMIT 1$$

DROP PROCEDURE IF EXISTS `sp_core_computers_registry_get_all`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_core_computers_registry_get_all` ()   SELECT
    id,
    computer_name,
    display_name,
    location,
    mac_address_normalized,
    is_registered
FROM core_computers_registry
ORDER BY display_name ASC, computer_name ASC$$

DROP PROCEDURE IF EXISTS `sp_core_computers_registry_lookup_by_name_get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_core_computers_registry_lookup_by_name_get` (IN `p_name` VARCHAR(255))   SELECT
    id,
    computer_name,
    hostname_normalized,
    display_name,
    location,
    mac_address_normalized,
    is_registered
FROM core_computers_registry
WHERE computer_name = p_name
   OR hostname_normalized = p_name
ORDER BY CASE WHEN computer_name = p_name THEN 0 ELSE 1 END
LIMIT 1$$

DROP PROCEDURE IF EXISTS `sp_core_computers_registry_registered_get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_core_computers_registry_registered_get` ()   SELECT
    computer_name,
    display_name
FROM core_computers_registry
WHERE is_registered = 1
ORDER BY display_name ASC, computer_name ASC$$

DROP PROCEDURE IF EXISTS `sp_core_computers_registry_update`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_core_computers_registry_update` (IN `p_id` BIGINT, IN `p_computer_name` VARCHAR(128), IN `p_hostname_normalized` VARCHAR(255), IN `p_mac_address_normalized` VARCHAR(64), IN `p_display_name` VARCHAR(128), IN `p_location` VARCHAR(255), IN `p_is_registered` TINYINT)   UPDATE core_computers_registry
SET computer_name = p_computer_name,
    hostname_normalized = p_hostname_normalized,
    mac_address_normalized = p_mac_address_normalized,
    display_name = p_display_name,
    location = p_location,
    is_registered = p_is_registered,
    updated_utc = UTC_TIMESTAMP()
WHERE id = p_id$$

DROP PROCEDURE IF EXISTS `sp_core_computers_registry_upsert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_core_computers_registry_upsert` (IN `p_computer_name` VARCHAR(128), IN `p_hostname_normalized` VARCHAR(255), IN `p_mac_address_normalized` VARCHAR(64), IN `p_display_name` VARCHAR(128), IN `p_location` VARCHAR(255))   INSERT INTO core_computers_registry (
    public_id,
    computer_name,
    hostname_normalized,
    mac_address_normalized,
    display_name,
    location,
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
    p_location,
    1,
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
)
ON DUPLICATE KEY UPDATE
    computer_name = VALUES(computer_name),
    display_name = VALUES(display_name),
    location = VALUES(location),
    is_registered = 1,
    updated_utc = UTC_TIMESTAMP()$$

DROP PROCEDURE IF EXISTS `sp_core_employee_by_identifier_get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_core_employee_by_identifier_get` (IN `p_employee_identifier` VARCHAR(128))   SELECT u.employee_identifier,
       COALESCE(u.display_name, '') AS display_name,
       u.is_active
FROM core_users_profiles u
WHERE u.employee_identifier IS NOT NULL
  AND u.employee_identifier = p_employee_identifier
ORDER BY u.is_active DESC, u.id ASC
LIMIT 1$$

DROP PROCEDURE IF EXISTS `sp_machine_configuration_reset`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_machine_configuration_reset` (IN `p_computer_id` BIGINT, IN `p_actor_user_id` BIGINT, IN `p_default_display_name` VARCHAR(128), IN `p_reset_display_name` TINYINT, IN `p_reset_location` TINYINT, IN `p_reset_picture_sources` TINYINT, IN `p_reset_scoped_preferences` TINYINT)   BEGIN
    DECLARE v_now_utc DATETIME;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SET v_now_utc = fn_server_utc_now();

    START TRANSACTION;

    IF p_reset_picture_sources = 1 THEN
        UPDATE config_images_locations
        SET is_active = 0,
            updated_by_user_id = p_actor_user_id,
            updated_utc = v_now_utc
        WHERE scope = 'computer'
          AND computer_id = p_computer_id
          AND is_active = 1;
    END IF;

    IF p_reset_display_name = 1 OR p_reset_location = 1 THEN
        UPDATE core_computers_registry
        SET display_name = CASE
                WHEN p_reset_display_name = 1 THEN p_default_display_name
                ELSE display_name
            END,
            location = CASE
                WHEN p_reset_location = 1 THEN NULL
                ELSE location
            END,
            updated_utc = v_now_utc
        WHERE id = p_computer_id;
    END IF;

    IF p_reset_scoped_preferences = 1 THEN
        DELETE FROM config_settings_values
        WHERE computer_id = p_computer_id;
    END IF;

    COMMIT;
END$$

DROP PROCEDURE IF EXISTS `sp_ops_startup_logs_filter`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_ops_startup_logs_filter` (IN `p_level` VARCHAR(16), IN `p_host_id` VARCHAR(128), IN `p_actor_id` VARCHAR(128), IN `p_module` VARCHAR(128), IN `p_error_type` VARCHAR(128), IN `p_error_fingerprint` CHAR(64), IN `p_from_utc` DATETIME, IN `p_to_utc` DATETIME, IN `p_page_size` INT, IN `p_offset` INT)   BEGIN
    DECLARE v_page_size INT DEFAULT 200;
    DECLARE v_offset INT DEFAULT 0;
    DECLARE v_to_utc DATETIME;
    DECLARE v_from_utc DATETIME;

    SET v_page_size = CASE
        WHEN p_page_size IS NULL OR p_page_size <= 0 THEN 200
        WHEN p_page_size > 1000 THEN 1000
        ELSE p_page_size
    END;

    SET v_offset = CASE
        WHEN p_offset IS NULL OR p_offset < 0 THEN 0
        ELSE p_offset
    END;

    SET v_to_utc = COALESCE(p_to_utc, fn_server_utc_now());
    SET v_from_utc = COALESCE(p_from_utc, DATE_SUB(v_to_utc, INTERVAL 30 DAY));

    SELECT
        l.id,
        l.public_id,
        l.correlation_id,
        l.created_utc,
        l.level,
        l.event_action,
        l.outcome,
        l.actor_kind,
        l.actor_id,
        l.host_id,
        l.mac_address,
        l.module,
        l.error_type,
        l.message,
        l.exception_detail,
        l.error_fingerprint,
        l.previous_hash,
        l.entry_hash,
        l.payload_json
    FROM
        ops_startup_logs l
    WHERE
        l.created_utc >= v_from_utc
        AND l.created_utc <= v_to_utc
        AND (p_level IS NULL OR l.level = LOWER(TRIM(p_level)))
        AND (p_host_id IS NULL OR l.host_id = TRIM(p_host_id))
        AND (p_actor_id IS NULL OR l.actor_id = TRIM(p_actor_id))
        AND (p_module IS NULL OR l.module = TRIM(p_module))
        AND (p_error_type IS NULL OR l.error_type = TRIM(p_error_type))
        AND (p_error_fingerprint IS NULL OR l.error_fingerprint = LOWER(TRIM(p_error_fingerprint)))
    ORDER BY
        l.created_utc DESC,
        l.id DESC
    LIMIT v_page_size OFFSET v_offset;
END$$

DROP PROCEDURE IF EXISTS `sp_ops_startup_logs_fingerprint_groups_get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_ops_startup_logs_fingerprint_groups_get` (IN `p_level` VARCHAR(16), IN `p_host_id` VARCHAR(128), IN `p_actor_id` VARCHAR(128), IN `p_module` VARCHAR(128), IN `p_error_type` VARCHAR(128), IN `p_from_utc` DATETIME, IN `p_to_utc` DATETIME, IN `p_max_groups` INT)   BEGIN
    DECLARE v_max_groups INT DEFAULT 100;
    DECLARE v_to_utc DATETIME;
    DECLARE v_from_utc DATETIME;

    SET v_max_groups = CASE
        WHEN p_max_groups IS NULL OR p_max_groups <= 0 THEN 100
        WHEN p_max_groups > 500 THEN 500
        ELSE p_max_groups
    END;

    SET v_to_utc = COALESCE(p_to_utc, fn_server_utc_now());
    SET v_from_utc = COALESCE(p_from_utc, DATE_SUB(v_to_utc, INTERVAL 30 DAY));

    SELECT
        l.error_fingerprint,
        COUNT(*) AS occurrences,
        MIN(l.created_utc) AS first_seen_utc,
        MAX(l.created_utc) AS last_seen_utc,
        ANY_VALUE(l.error_type) AS error_type,
        ANY_VALUE(l.module) AS module,
        ANY_VALUE(l.message) AS sample_message
    FROM
        ops_startup_logs l
    WHERE
        l.error_fingerprint IS NOT NULL
        AND l.created_utc >= v_from_utc
        AND l.created_utc <= v_to_utc
        AND (p_level IS NULL OR l.level = LOWER(TRIM(p_level)))
        AND (p_host_id IS NULL OR l.host_id = TRIM(p_host_id))
        AND (p_actor_id IS NULL OR l.actor_id = TRIM(p_actor_id))
        AND (p_module IS NULL OR l.module = TRIM(p_module))
        AND (p_error_type IS NULL OR l.error_type = TRIM(p_error_type))
    GROUP BY
        l.error_fingerprint
    ORDER BY
        occurrences DESC,
        last_seen_utc DESC
    LIMIT v_max_groups;
END$$

DROP PROCEDURE IF EXISTS `sp_ops_startup_logs_insert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_ops_startup_logs_insert` (IN `p_public_id` CHAR(36), IN `p_correlation_id` CHAR(36), IN `p_level` VARCHAR(16), IN `p_event_action` VARCHAR(128), IN `p_outcome` VARCHAR(32), IN `p_actor_kind` VARCHAR(32), IN `p_actor_id` VARCHAR(128), IN `p_host_id` VARCHAR(128), IN `p_mac_address` VARCHAR(64), IN `p_module` VARCHAR(128), IN `p_error_type` VARCHAR(128), IN `p_message` TEXT, IN `p_exception_detail` MEDIUMTEXT, IN `p_error_fingerprint` CHAR(64), IN `p_payload_json` MEDIUMTEXT)   BEGIN
    DECLARE v_previous_hash CHAR(64) DEFAULT NULL;
    DECLARE v_entry_hash CHAR(64) DEFAULT NULL;
    DECLARE v_created_utc DATETIME;
    DECLARE v_lock_acquired TINYINT DEFAULT 0;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        IF v_lock_acquired = 1 THEN
            DO RELEASE_LOCK('mtm_waitlist.ops_startup_logs.chain');
        END IF;
        RESIGNAL;
    END;

    SET v_lock_acquired = GET_LOCK('mtm_waitlist.ops_startup_logs.chain', 10);

    IF v_lock_acquired <> 1 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_log_chain_busy';
    END IF;

    START TRANSACTION;

    SET v_created_utc = fn_server_utc_now();

    SELECT
        l.entry_hash
    INTO v_previous_hash
    FROM
        ops_startup_logs l
    ORDER BY
        l.id DESC
    LIMIT 1;

    SET v_entry_hash = SHA2(
        CONCAT_WS(
            CHAR(31),
            COALESCE(v_previous_hash, ''),
            TRIM(p_public_id),
            LOWER(TRIM(p_correlation_id)),
            v_created_utc,
            LOWER(TRIM(p_level)),
            TRIM(p_event_action),
            TRIM(p_outcome),
            COALESCE(TRIM(p_actor_kind), ''),
            COALESCE(TRIM(p_actor_id), ''),
            COALESCE(TRIM(p_host_id), ''),
            COALESCE(LOWER(TRIM(p_mac_address)), ''),
            COALESCE(TRIM(p_module), ''),
            COALESCE(TRIM(p_error_type), ''),
            TRIM(p_message),
            COALESCE(p_exception_detail, ''),
            COALESCE(LOWER(TRIM(p_error_fingerprint)), ''),
            COALESCE(p_payload_json, '')
        ),
        256
    );

    INSERT INTO ops_startup_logs (
        public_id,
        correlation_id,
        created_utc,
        level,
        event_action,
        outcome,
        actor_kind,
        actor_id,
        host_id,
        mac_address,
        module,
        error_type,
        exception_detail,
        error_fingerprint,
        message,
        payload_json,
        previous_hash,
        entry_hash
    )
    VALUES (
        TRIM(p_public_id),
        LOWER(TRIM(p_correlation_id)),
        v_created_utc,
        LOWER(TRIM(p_level)),
        TRIM(p_event_action),
        TRIM(p_outcome),
        NULLIF(TRIM(p_actor_kind), ''),
        NULLIF(TRIM(p_actor_id), ''),
        NULLIF(TRIM(p_host_id), ''),
        NULLIF(LOWER(TRIM(p_mac_address)), ''),
        NULLIF(TRIM(p_module), ''),
        NULLIF(TRIM(p_error_type), ''),
        NULLIF(p_exception_detail, ''),
        NULLIF(LOWER(TRIM(p_error_fingerprint)), ''),
        TRIM(p_message),
        NULLIF(p_payload_json, ''),
        v_previous_hash,
        v_entry_hash
    );

    COMMIT;

    IF v_lock_acquired = 1 THEN
        DO RELEASE_LOCK('mtm_waitlist.ops_startup_logs.chain');
    END IF;
END$$

DROP PROCEDURE IF EXISTS `sp_ops_startup_logs_purge`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_ops_startup_logs_purge` (IN `p_max_age_days` INT, IN `p_max_rows` INT)   BEGIN
    DECLARE v_has_age_bound TINYINT DEFAULT 0;
    DECLARE v_has_size_bound TINYINT DEFAULT 0;
    DECLARE v_cutoff_utc DATETIME DEFAULT NULL;
    DECLARE v_keep_rows INT DEFAULT 0;

    IF p_max_age_days IS NOT NULL AND p_max_age_days > 0 THEN
        SET v_has_age_bound = 1;
        SET v_cutoff_utc = DATE_SUB(fn_server_utc_now(), INTERVAL p_max_age_days DAY);
    END IF;

    IF p_max_rows IS NOT NULL AND p_max_rows >= 0 THEN
        SET v_has_size_bound = 1;
        SET v_keep_rows = p_max_rows;
    END IF;

    DELETE FROM ops_startup_logs
    WHERE
        (v_has_age_bound = 1 AND created_utc < v_cutoff_utc)
        OR (
            v_has_size_bound = 1
            AND id NOT IN (
                SELECT keep_rows.id
                FROM (
                    SELECT l.id
                    FROM ops_startup_logs l
                    ORDER BY l.id DESC
                    LIMIT v_keep_rows
                ) AS keep_rows
            )
        );
END$$

DROP PROCEDURE IF EXISTS `sp_setup_active_jobs_latest_by_work_center_get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_setup_active_jobs_latest_by_work_center_get` ()   SELECT aj.work_center, aj.work_order, aj.part_number, aj.sequence_number,
       aj.subordinate_parts_json,
       aj.selected_dunnage_parts_json
FROM setup_active_jobs aj
INNER JOIN (
    SELECT work_center, MAX(id) AS max_id
    FROM setup_active_jobs
    WHERE is_active = 1
    GROUP BY work_center
) latest ON latest.max_id = aj.id$$

DROP PROCEDURE IF EXISTS `sp_setup_active_job_dunnage_assignments_get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_setup_active_job_dunnage_assignments_get` (IN `p_work_order` VARCHAR(32), IN `p_part_number` VARCHAR(64), IN `p_sequence_number` VARCHAR(32))   SELECT selected_dunnage_parts_json
FROM setup_part_sequence_custom_data
WHERE part_number = TRIM(p_part_number)
  AND sequence_number = TRIM(p_sequence_number)
ORDER BY updated_utc DESC
LIMIT 1$$

DROP PROCEDURE IF EXISTS `sp_setup_job_history_insert_for_pair`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_setup_job_history_insert_for_pair` (IN `p_event_action` VARCHAR(32), IN `p_changed_by_user_id` BIGINT, IN `p_work_order` VARCHAR(32), IN `p_part_number` VARCHAR(64), IN `p_sequence_number` VARCHAR(32))   INSERT INTO setup_job_history (
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
LIMIT 1$$

DROP PROCEDURE IF EXISTS `sp_setup_job_history_scrap_type_get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_setup_job_history_scrap_type_get` (IN `p_work_order` VARCHAR(32), IN `p_part_number` VARCHAR(64), IN `p_sequence_number` VARCHAR(32))   SELECT selected_scrap_type
FROM setup_part_sequence_custom_data
WHERE part_number = TRIM(p_part_number)
  AND sequence_number = TRIM(p_sequence_number)
ORDER BY updated_utc DESC
LIMIT 1$$

DROP PROCEDURE IF EXISTS `sp_setup_part_sequence_custom_data_upsert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_setup_part_sequence_custom_data_upsert` (IN `p_part_number` VARCHAR(64), IN `p_sequence_number` VARCHAR(32), IN `p_selected_scrap_type` VARCHAR(128), IN `p_selected_dunnage_type_id` VARCHAR(64), IN `p_selected_dunnage_part_id` VARCHAR(64), IN `p_subordinate_parts_json` JSON, IN `p_selected_dunnage_parts_json` JSON, IN `p_updated_by_user_id` BIGINT)   INSERT INTO setup_part_sequence_custom_data (
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
    updated_utc = UTC_TIMESTAMP()$$

DROP PROCEDURE IF EXISTS `sp_setup_save_setup`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_setup_save_setup` (IN `p_work_order` VARCHAR(32), IN `p_part_number` VARCHAR(64), IN `p_sequence_number` VARCHAR(32), IN `p_work_center` VARCHAR(64), IN `p_selected_dunnage_type_id` VARCHAR(64), IN `p_selected_dunnage_part_id` VARCHAR(64), IN `p_subordinate_parts_json` JSON, IN `p_selected_dunnage_parts_json` JSON, IN `p_saved_by_user_id` BIGINT)   INSERT INTO setup_active_jobs (
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
	updated_utc = UTC_TIMESTAMP()$$

DROP PROCEDURE IF EXISTS `sp_setup_work_centers_catalog_get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_setup_work_centers_catalog_get` ()   SELECT
    id,
    work_center_name,
    building,
    sort_rank,
    is_active
FROM vw_setup_work_centers_active
ORDER BY building ASC, sort_rank ASC, work_center_name ASC$$

DROP PROCEDURE IF EXISTS `sp_setup_work_centers_delete`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_setup_work_centers_delete` (IN `p_work_center_id` VARCHAR(32))   DELETE FROM setup_work_centers_catalog
WHERE id = CAST(TRIM(p_work_center_id) AS UNSIGNED)$$

DROP PROCEDURE IF EXISTS `sp_setup_work_centers_exists_get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_setup_work_centers_exists_get` (IN `p_id` BIGINT)   SELECT id
FROM setup_work_centers_catalog
WHERE id = p_id
LIMIT 1$$

DROP PROCEDURE IF EXISTS `sp_setup_work_centers_get_all`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_setup_work_centers_get_all` ()   SELECT
    id,
    building,
    work_center_name,
    is_active,
    updated_utc
FROM vw_setup_work_centers_active
ORDER BY sort_rank ASC, work_center_name ASC$$

DROP PROCEDURE IF EXISTS `sp_setup_work_centers_touch`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_setup_work_centers_touch` (IN `p_work_center` VARCHAR(64), IN `p_updated_by_user_id` BIGINT)   UPDATE setup_work_centers_catalog
SET
    updated_utc = UTC_TIMESTAMP(),
    updated_by_user_id = COALESCE(p_updated_by_user_id, updated_by_user_id)
WHERE work_center_name = TRIM(p_work_center)$$

DROP PROCEDURE IF EXISTS `sp_setup_work_centers_upsert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_setup_work_centers_upsert` (IN `p_work_center_id` VARCHAR(32), IN `p_building` VARCHAR(64), IN `p_work_center_name` VARCHAR(64), IN `p_modified_by_user_id` BIGINT)   INSERT INTO setup_work_centers_catalog (
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
    updated_utc = UTC_TIMESTAMP()$$

DROP PROCEDURE IF EXISTS `sp_user_management_create`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_user_management_create` (IN `p_username` VARCHAR(128), IN `p_first_name` VARCHAR(128), IN `p_last_name` VARCHAR(128), IN `p_employee_identifier` VARCHAR(128), IN `p_role_code` VARCHAR(64), IN `p_actor_user_id` BIGINT, IN `p_password_hash` VARCHAR(128), IN `p_password_salt` VARBINARY(32), IN `p_change_group_id` CHAR(36))   BEGIN
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

DROP PROCEDURE IF EXISTS `sp_user_management_get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_user_management_get` (IN `p_user_id` BIGINT)   SELECT u.id AS user_id,
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
LIMIT 1$$

DROP PROCEDURE IF EXISTS `sp_user_management_list`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_user_management_list` (IN `p_search` VARCHAR(128), IN `p_role_code` VARCHAR(64))   SELECT u.id AS user_id,
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
ORDER BY u.display_name ASC, u.username_normalized ASC$$

DROP PROCEDURE IF EXISTS `sp_user_management_reset_password`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_user_management_reset_password` (IN `p_user_id` BIGINT, IN `p_actor_user_id` BIGINT, IN `p_password_hash` VARCHAR(128), IN `p_password_salt` VARBINARY(32), IN `p_change_group_id` CHAR(36))   BEGIN
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

DROP PROCEDURE IF EXISTS `sp_user_management_update`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_user_management_update` (IN `p_user_id` BIGINT, IN `p_username` VARCHAR(128), IN `p_first_name` VARCHAR(128), IN `p_last_name` VARCHAR(128), IN `p_employee_identifier` VARCHAR(128), IN `p_role_code` VARCHAR(64), IN `p_is_active` TINYINT, IN `p_actor_user_id` BIGINT, IN `p_change_group_id` CHAR(36))   BEGIN
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
        
        
        
        
        
        SELECT 'is_active', IF(v_current_is_active <> 0, '1', '0'), IF(p_is_active <> 0, '1', '0')
    ) AS changed
    WHERE NOT (changed.previous_value <=> changed.changed_value);

    COMMIT;
END$$

DROP PROCEDURE IF EXISTS `sp_waitlist_defect_types_delete`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_waitlist_defect_types_delete` (IN `p_id` BIGINT)   DELETE FROM waitlist_defect_types
WHERE id = p_id$$

DROP PROCEDURE IF EXISTS `sp_waitlist_defect_types_get_all`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_waitlist_defect_types_get_all` ()   SELECT
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
ORDER BY sort_order ASC, defect_name ASC$$

DROP PROCEDURE IF EXISTS `sp_waitlist_defect_types_insert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_waitlist_defect_types_insert` (IN `p_defect_name` VARCHAR(100), IN `p_description` VARCHAR(500), IN `p_sort_order` INT, IN `p_created_by_user_id` BIGINT)   INSERT INTO waitlist_defect_types (
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
)$$

DROP PROCEDURE IF EXISTS `sp_waitlist_defect_types_update`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_waitlist_defect_types_update` (IN `p_id` BIGINT, IN `p_defect_name` VARCHAR(100), IN `p_description` VARCHAR(500), IN `p_sort_order` INT, IN `p_updated_by_user_id` BIGINT)   UPDATE waitlist_defect_types
SET defect_name = NULLIF(TRIM(p_defect_name), ''),
    description = NULLIF(TRIM(COALESCE(p_description, '')), ''),
    sort_order = COALESCE(p_sort_order, 0),
    updated_by_user_id = p_updated_by_user_id,
    updated_utc = UTC_TIMESTAMP()
WHERE id = p_id$$

DROP PROCEDURE IF EXISTS `sp_waitlist_request_audit_insert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_waitlist_request_audit_insert` (IN `p_request_public_id` CHAR(36), IN `p_from_status` VARCHAR(32), IN `p_to_status` VARCHAR(32), IN `p_event_type` VARCHAR(32), IN `p_actor_employee_number` VARCHAR(32), IN `p_actor_employee_name` VARCHAR(128), IN `p_details` VARCHAR(255))   INSERT INTO waitlist_requests_audit (
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
)$$

DROP PROCEDURE IF EXISTS `sp_waitlist_request_audit_list`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_waitlist_request_audit_list` (IN `p_request_public_id` CHAR(36))   SELECT
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
ORDER BY occurred_utc ASC, id ASC$$

DROP PROCEDURE IF EXISTS `sp_waitlist_request_get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_waitlist_request_get` (IN `p_public_id` CHAR(36))   SELECT
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
    
    
    (SELECT a.actor_employee_name
       FROM waitlist_requests_audit a
      WHERE a.request_public_id = waitlist_requests_queue.public_id
        AND a.actor_employee_name IS NOT NULL
        AND a.actor_employee_number = waitlist_requests_queue.assigned_material_handler
      ORDER BY a.occurred_utc DESC
      LIMIT 1) AS assigned_material_handler_name,
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
LIMIT 1$$

DROP PROCEDURE IF EXISTS `sp_waitlist_request_handler_status_update`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_waitlist_request_handler_status_update` (IN `p_public_id` CHAR(36), IN `p_handler_status` VARCHAR(32), IN `p_expected_material_handler` VARCHAR(128))   UPDATE waitlist_requests_queue
SET handler_status = NULLIF(TRIM(COALESCE(p_handler_status, '')) COLLATE utf8mb4_unicode_ci, ''),
    updated_utc = UTC_TIMESTAMP()
WHERE public_id = p_public_id
  AND NULLIF(TRIM(COALESCE(p_expected_material_handler, '')), '') IS NOT NULL
  AND assigned_material_handler = TRIM(p_expected_material_handler)$$

DROP PROCEDURE IF EXISTS `sp_waitlist_request_insert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_waitlist_request_insert` (IN `p_public_id` CHAR(36), IN `p_building` VARCHAR(64), IN `p_work_center` VARCHAR(64), IN `p_category` VARCHAR(16), IN `p_item` VARCHAR(64), IN `p_input_value` VARCHAR(255), IN `p_active_setup_job_id` VARCHAR(64), IN `p_work_center_name` VARCHAR(64), IN `p_requester_employee_number` VARCHAR(32), IN `p_requester_employee_name` VARCHAR(128), IN `p_status` VARCHAR(32), IN `p_requested_utc` DATETIME, IN `p_target_time_utc` DATETIME, IN `p_is_overdue` TINYINT, IN `p_assigned_material_handler` VARCHAR(128), IN `p_cancellation_reason` VARCHAR(255), IN `p_note` VARCHAR(500), IN `p_answers_json` JSON)   INSERT INTO waitlist_requests_queue (
    public_id,
    building,
    work_center,
    category,
    item,
    input_value,
    answers_json,
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
    p_answers_json,
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
)$$

DROP PROCEDURE IF EXISTS `sp_waitlist_request_item_allotted_minutes_update`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_waitlist_request_item_allotted_minutes_update` (IN `p_item` VARCHAR(64), IN `p_allotted_minutes` INT, IN `p_updated_by_user_id` BIGINT)   UPDATE waitlist_request_item_configs
SET allotted_minutes = CASE
        WHEN p_allotted_minutes IS NULL THEN NULL
        ELSE LEAST(GREATEST(p_allotted_minutes, 1), 1440)
    END,
    updated_by_user_id = p_updated_by_user_id,
    updated_utc = UTC_TIMESTAMP()
WHERE item = TRIM(COALESCE(p_item, ''))$$

DROP PROCEDURE IF EXISTS `sp_waitlist_request_item_configs_get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_waitlist_request_item_configs_get` ()   SELECT
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
ORDER BY item ASC$$

DROP PROCEDURE IF EXISTS `sp_waitlist_request_item_observed_average_get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_waitlist_request_item_observed_average_get` ()   SELECT
    q.item,
    COUNT(*) AS completed_request_count,
    AVG(TIMESTAMPDIFF(SECOND, q.accepted_utc, q.completed_utc)) AS average_seconds
FROM waitlist_requests_queue q
WHERE q.status = 'Completed'
  AND q.accepted_utc IS NOT NULL
  AND q.completed_utc IS NOT NULL
GROUP BY q.item
ORDER BY q.item ASC$$

DROP PROCEDURE IF EXISTS `sp_waitlist_request_list`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_waitlist_request_list` (IN `p_building` VARCHAR(64), IN `p_include_resolved` TINYINT)   SELECT
    q.id,
    q.public_id,
    q.building,
    q.work_center,
    q.category,
    q.item,
    q.input_value,
    q.handler_status,
    q.answers_json,
    q.active_setup_job_id,
    q.work_center_name,
    q.requester_employee_number,
    q.requester_employee_name,
    q.status,
    q.requested_utc,
    q.target_time_utc,
    q.is_overdue,
    q.assigned_material_handler,
    
    
    
    
    
    (SELECT a.actor_employee_name
       FROM waitlist_requests_audit a
      WHERE a.request_public_id = q.public_id
        AND a.actor_employee_name IS NOT NULL
        AND a.actor_employee_number = q.assigned_material_handler
      ORDER BY a.occurred_utc DESC
      LIMIT 1) AS assigned_material_handler_name,
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
ORDER BY q.requested_utc ASC$$

DROP PROCEDURE IF EXISTS `sp_waitlist_request_status_update`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_waitlist_request_status_update` (IN `p_public_id` CHAR(36), IN `p_status` VARCHAR(32), IN `p_assigned_material_handler` VARCHAR(128), IN `p_cancellation_reason` VARCHAR(255), IN `p_canceled_by_employee_number` VARCHAR(32), IN `p_note` VARCHAR(500), IN `p_expected_status` VARCHAR(32))   UPDATE waitlist_requests_queue
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
  )$$

DROP FUNCTION IF EXISTS `fn_config_settings_scope_rank`$$
CREATE DEFINER=`root`@`localhost` FUNCTION `fn_config_settings_scope_rank` (`p_scope_type` VARCHAR(16)) RETURNS TINYINT(4) DETERMINISTIC RETURN CASE LOWER(TRIM(p_scope_type))
    WHEN 'computer' THEN 1
    WHEN 'all_users' THEN 2
    WHEN 'role' THEN 3
    WHEN 'admin' THEN 4
    WHEN 'developer' THEN 5
    WHEN 'user' THEN 6
    ELSE 0
END$$

DROP FUNCTION IF EXISTS `fn_server_utc_now`$$
CREATE DEFINER=`root`@`localhost` FUNCTION `fn_server_utc_now` () RETURNS DATETIME DETERMINISTIC NO SQL RETURN UTC_TIMESTAMP()$$

DROP FUNCTION IF EXISTS `fn_setup_work_center_name_normalized`$$
CREATE DEFINER=`root`@`localhost` FUNCTION `fn_setup_work_center_name_normalized` (`p_work_center_name` VARCHAR(64)) RETURNS VARCHAR(64) CHARSET utf8mb4 COLLATE utf8mb4_unicode_ci DETERMINISTIC RETURN TRIM(REPLACE(REPLACE(REPLACE(IFNULL(p_work_center_name, ''), '\r', ' '), '\n', ' '), '\t', ' '))$$

DELIMITER ;

DROP TABLE IF EXISTS `auth_roles_assignments`;
CREATE TABLE `auth_roles_assignments` (
  `id` bigint(20) NOT NULL COMMENT 'Surrogate primary key.',
  `public_id` char(36) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Public UUID for assignment record.',
  `user_id` bigint(20) NOT NULL COMMENT 'Foreign key to core_users_profiles.id.',
  `role_id` bigint(20) NOT NULL COMMENT 'Foreign key to auth_roles_catalog.id.',
  `assigned_utc` datetime NOT NULL COMMENT 'UTC timestamp when the role was assigned.',
  `assigned_by_user_id` bigint(20) DEFAULT NULL COMMENT 'User who assigned the role.'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='User-to-role assignment records for RBAC.';

TRUNCATE TABLE `auth_roles_assignments`;
INSERT INTO `auth_roles_assignments` (`id`, `public_id`, `user_id`, `role_id`, `assigned_utc`, `assigned_by_user_id`) VALUES
(1, '5690e08c-bdc8-11f1-bdd0-d843ae47d0d6', 2, 8, '2026-10-01 18:45:56', 2),
(2, '5690e21e-bdc8-11f1-bdd0-d843ae47d0d6', 1, 8, '2026-10-01 18:45:56', 1),
(3, '5690e31f-bdc8-11f1-bdd0-d843ae47d0d6', 3, 10, '2026-10-01 18:45:56', 3),
(4, '5690e3c6-bdc8-11f1-bdd0-d843ae47d0d6', 4, 8, '2026-10-01 18:45:56', 4),
(5, '5690e47d-bdc8-11f1-bdd0-d843ae47d0d6', 10, 1, '2026-10-01 18:45:56', 10),
(6, '5690e51a-bdc8-11f1-bdd0-d843ae47d0d6', 5, 7, '2026-10-01 18:45:56', 5),
(7, '5690e5d1-bdc8-11f1-bdd0-d843ae47d0d6', 9, 2, '2026-10-01 18:45:56', 9),
(8, '5690e67f-bdc8-11f1-bdd0-d843ae47d0d6', 7, 3, '2026-10-01 18:45:56', 7),
(9, '5690e733-bdc8-11f1-bdd0-d843ae47d0d6', 8, 4, '2026-10-01 18:45:56', 8),
(10, '5690e7d6-bdc8-11f1-bdd0-d843ae47d0d6', 6, 5, '2026-10-01 18:45:56', 6);

DROP TABLE IF EXISTS `auth_roles_catalog`;
CREATE TABLE `auth_roles_catalog` (
  `id` bigint(20) NOT NULL COMMENT 'Surrogate primary key.',
  `public_id` char(36) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Public UUID for role record.',
  `role_code` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Machine-friendly unique role code.',
  `role_name` varchar(128) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Human-readable role name.',
  `role_rank` int(11) NOT NULL DEFAULT '0' COMMENT 'The role rung used by every rank comparison. Higher outranks lower. A retired role is deliberately left at the default 0.',
  `created_utc` datetime NOT NULL COMMENT 'UTC timestamp when the row was created.',
  `updated_utc` datetime NOT NULL COMMENT 'UTC timestamp when the row was last updated.'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Role definitions used for RBAC authorization.';

TRUNCATE TABLE `auth_roles_catalog`;
INSERT INTO `auth_roles_catalog` (`id`, `public_id`, `role_code`, `role_name`, `role_rank`, `created_utc`, `updated_utc`) VALUES
(1, '567e5e52-bdc8-11f1-bdd0-d843ae47d0d6', 'material_handler', 'Material Handler', 10, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(2, '567e5fa2-bdc8-11f1-bdd0-d843ae47d0d6', 'production', 'Production', 10, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(3, '567e6048-bdc8-11f1-bdd0-d843ae47d0d6', 'production_lead', 'Production Lead', 70, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(4, '567e60ad-bdc8-11f1-bdd0-d843ae47d0d6', 'setup', 'Setup', 10, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(5, '567e610a-bdc8-11f1-bdd0-d843ae47d0d6', 'setup_lead', 'Setup Lead', 60, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(6, '567e6162-bdc8-11f1-bdd0-d843ae47d0d6', 'material_handler_lead', 'Material Handler Lead', 50, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(7, '567e61dc-bdc8-11f1-bdd0-d843ae47d0d6', 'plant_manager', 'Plant Manager', 80, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(8, '567e6240-bdc8-11f1-bdd0-d843ae47d0d6', 'developer', 'Developer', 100, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(10, '569890d9-bdc8-11f1-bdd0-d843ae47d0d6', 'it_department', 'IT Department', 90, '2026-10-01 18:45:56', '2026-10-01 18:45:56');

DROP TABLE IF EXISTS `auth_user_management_audit`;
CREATE TABLE `auth_user_management_audit` (
  `id` bigint(20) NOT NULL COMMENT 'Surrogate primary key.',
  `public_id` char(36) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Public UUID for external references.',
  `change_group_id` char(36) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Shared by every row one save produced, so one act reads as one act.',
  `target_user_id` bigint(20) NOT NULL COMMENT 'The person whose account changed.',
  `field_name` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'The one field this row is about.',
  `previous_value` text COLLATE utf8mb4_unicode_ci COMMENT 'The value before the change, or NULL where there is none to record.',
  `changed_value` text COLLATE utf8mb4_unicode_ci COMMENT 'The value after the change, or NULL where there is none to record.',
  `actor_user_id` bigint(20) DEFAULT NULL COMMENT 'The acting person, joined for durability.',
  `actor_display_name` varchar(256) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'The actor display name as it was when they acted.',
  `actor_employee_identifier` varchar(128) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'The actor employee number as it was when they acted.',
  `actor_role_code` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'The actor role code as it was when they acted.',
  `occurred_utc` datetime NOT NULL COMMENT 'UTC timestamp when the change was recorded.'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

TRUNCATE TABLE `auth_user_management_audit`;
DROP TABLE IF EXISTS `config_computer_hot_work_centers`;
CREATE TABLE `config_computer_hot_work_centers` (
  `id` bigint(20) NOT NULL COMMENT 'Surrogate primary key.',
  `public_id` char(36) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Public UUID for Local mapping row.',
  `computer_id` bigint(20) NOT NULL COMMENT 'Foreign key to core_computers_registry.id.',
  `work_center_id` bigint(20) NOT NULL COMMENT 'Foreign key to setup_work_centers_catalog.id.',
  `sort_rank` int(11) NOT NULL DEFAULT '100' COMMENT 'Display order for Local work centers per computer.',
  `is_active` tinyint(1) NOT NULL DEFAULT '1' COMMENT 'Whether this Local mapping is active.',
  `created_by_user_id` bigint(20) DEFAULT NULL COMMENT 'User who created the mapping row.',
  `updated_by_user_id` bigint(20) DEFAULT NULL COMMENT 'User who last updated the mapping row.',
  `created_utc` datetime NOT NULL COMMENT 'UTC timestamp when row was created.',
  `updated_utc` datetime NOT NULL COMMENT 'UTC timestamp when row was last updated.'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Per-computer Local work center preferences and ordering.';

TRUNCATE TABLE `config_computer_hot_work_centers`;
DROP TABLE IF EXISTS `config_dunnage_types_visibility`;
CREATE TABLE `config_dunnage_types_visibility` (
  `id` bigint(20) NOT NULL COMMENT 'Surrogate primary key.',
  `public_id` char(36) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Public UUID for visibility row.',
  `dunnage_type_id` bigint(20) NOT NULL COMMENT 'Source dunnage type identifier from mtm_receiving_application.dunnage_types.id.',
  `dunnage_type_name` varchar(128) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Dunnage type display name snapshot.',
  `is_visible` tinyint(1) NOT NULL DEFAULT '1' COMMENT 'Whether the dunnage type is visible in MTM Waitlist.',
  `created_by_user_id` bigint(20) DEFAULT NULL COMMENT 'User who created the visibility row.',
  `updated_by_user_id` bigint(20) DEFAULT NULL COMMENT 'User who last updated the visibility row.',
  `created_utc` datetime NOT NULL COMMENT 'UTC timestamp when row was created.',
  `updated_utc` datetime NOT NULL COMMENT 'UTC timestamp when row was last updated.'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Global dunnage type visibility preferences for MTM Waitlist.';

TRUNCATE TABLE `config_dunnage_types_visibility`;
DROP TABLE IF EXISTS `config_images_locations`;
CREATE TABLE `config_images_locations` (
  `id` bigint(20) NOT NULL COMMENT 'Surrogate primary key.',
  `public_id` char(36) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Public UUID for image location override row.',
  `scope` varchar(16) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Scope type: request_item, request_category, work_center, visual_part, wip_part, or computer.',
  `scope_item_id` varchar(190) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Identifier within scope: the Item or Category code, the numeric work center id, the part number, or <computer_id>:<source_kind> at computer scope.',
  `computer_id` bigint(20) DEFAULT NULL COMMENT 'At computer scope, the machine the row belongs to; NULL for the five picture scopes.',
  `image_path` varchar(500) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'File system path to the image file copied to the shared network folder.',
  `is_active` tinyint(1) NOT NULL DEFAULT '1' COMMENT 'Soft-delete flag; inactive rows are ignored during path resolution cascades.',
  `created_by_user_id` bigint(20) DEFAULT NULL COMMENT 'User who created the image override.',
  `updated_by_user_id` bigint(20) DEFAULT NULL COMMENT 'User who last updated the image override.',
  `created_utc` datetime NOT NULL COMMENT 'UTC timestamp when override was created.',
  `updated_utc` datetime NOT NULL COMMENT 'UTC timestamp when override was last updated.'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Image location overrides for request items, their Category families, work centers, parts and machines. Enables role-based customization of visual assets via cascade resolution pattern.';

TRUNCATE TABLE `config_images_locations`;DROP TRIGGER IF EXISTS `trg_config_images_locations_history_on_insert`;
DELIMITER $$
CREATE TRIGGER `trg_config_images_locations_history_on_insert` AFTER INSERT ON `config_images_locations` FOR EACH ROW BEGIN
    INSERT INTO config_images_locations_history (
        public_id,
        image_location_id,
        scope,
        scope_item_id,
        previous_image_path,
        new_image_path,
        changed_by_user_id,
        changed_utc
    )
    VALUES (
        UUID(),
        NEW.id,
        NEW.scope,
        NEW.scope_item_id,
        NULL,
        NEW.image_path,
        NEW.created_by_user_id,
        NEW.created_utc
    );
END
$$
DELIMITER ;
DROP TRIGGER IF EXISTS `trg_config_images_locations_history_on_update`;
DELIMITER $$
CREATE TRIGGER `trg_config_images_locations_history_on_update` AFTER UPDATE ON `config_images_locations` FOR EACH ROW BEGIN
    IF NOT (OLD.image_path <=> NEW.image_path) AND @mtm_picture_layout_move IS NULL THEN
        INSERT INTO config_images_locations_history (
            public_id,
            image_location_id,
            scope,
            scope_item_id,
            previous_image_path,
            new_image_path,
            changed_by_user_id,
            changed_utc
        )
        VALUES (
            UUID(),
            NEW.id,
            NEW.scope,
            NEW.scope_item_id,
            OLD.image_path,
            NEW.image_path,
            NEW.updated_by_user_id,
            NEW.updated_utc
        );
    END IF;
END
$$
DELIMITER ;

DROP TABLE IF EXISTS `config_images_locations_history`;
CREATE TABLE `config_images_locations_history` (
  `id` bigint(20) NOT NULL,
  `public_id` char(36) COLLATE utf8mb4_unicode_ci NOT NULL,
  `image_location_id` bigint(20) DEFAULT NULL COMMENT 'The picture row this change belongs to; NULL once that row is retired',
  `scope` varchar(16) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Copied at write time: request_item, request_category, work_center, visual_part, wip_part',
  `scope_item_id` varchar(190) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Copied at write time: the item code, category code, work center id, or part number',
  `previous_image_path` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'The relative path this change replaced; NULL for a first picture',
  `new_image_path` varchar(500) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'The relative path that replaced it',
  `changed_by_user_id` bigint(20) DEFAULT NULL COMMENT 'The actor who set or replaced the picture',
  `changed_utc` datetime NOT NULL COMMENT 'UTC timestamp of the change'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Who set or replaced a stored picture, when, and what the previous picture was.';

TRUNCATE TABLE `config_images_locations_history`;
DROP TABLE IF EXISTS `config_settings_history`;
CREATE TABLE `config_settings_history` (
  `id` bigint(20) NOT NULL COMMENT 'Surrogate primary key.',
  `public_id` char(36) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Public UUID for history row.',
  `config_setting_id` bigint(20) NOT NULL COMMENT 'Foreign key to config_settings_values.id.',
  `setting_key` varchar(190) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Setting key snapshot at time of change.',
  `scope_type` varchar(16) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Scope type snapshot at time of change.',
  `scope_key` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Scope key snapshot at time of change.',
  `computer_id` bigint(20) DEFAULT NULL COMMENT 'Optional computer scope foreign key snapshot.',
  `user_id` bigint(20) DEFAULT NULL COMMENT 'Optional user scope foreign key snapshot.',
  `previous_setting_value` text COLLATE utf8mb4_unicode_ci COMMENT 'Previous text setting value.',
  `previous_setting_value_int` bigint(20) DEFAULT NULL COMMENT 'Previous integer setting value.',
  `previous_setting_value_bool` tinyint(1) DEFAULT NULL COMMENT 'Previous boolean setting value.',
  `previous_setting_value_decimal` decimal(18,6) DEFAULT NULL COMMENT 'Previous decimal setting value.',
  `previous_setting_value_datetime_utc` datetime DEFAULT NULL COMMENT 'Previous datetime setting value in UTC.',
  `changed_setting_value` text COLLATE utf8mb4_unicode_ci COMMENT 'New text setting value.',
  `changed_setting_value_int` bigint(20) DEFAULT NULL COMMENT 'New integer setting value.',
  `changed_setting_value_bool` tinyint(1) DEFAULT NULL COMMENT 'New boolean setting value.',
  `changed_setting_value_decimal` decimal(18,6) DEFAULT NULL COMMENT 'New decimal setting value.',
  `changed_setting_value_datetime_utc` datetime DEFAULT NULL COMMENT 'New datetime setting value in UTC.',
  `value_type` varchar(32) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Declared value type for history payload.',
  `changed_by_user_id` bigint(20) DEFAULT NULL COMMENT 'User who made the setting change.',
  `changed_utc` datetime NOT NULL COMMENT 'UTC timestamp when the change was recorded.'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Audit trail for configuration setting changes.';

TRUNCATE TABLE `config_settings_history`;
DROP TABLE IF EXISTS `config_settings_values`;
CREATE TABLE `config_settings_values` (
  `id` bigint(20) NOT NULL COMMENT 'Surrogate primary key.',
  `public_id` char(36) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Public UUID for setting row.',
  `setting_key` varchar(190) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Unique setting identifier key.',
  `scope_type` varchar(16) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'all_users' COMMENT 'Scope kind such as computer, user, or all_users.',
  `scope_key` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'all_users' COMMENT 'Scope discriminator key value.',
  `computer_id` bigint(20) DEFAULT NULL COMMENT 'Optional computer scope foreign key.',
  `user_id` bigint(20) DEFAULT NULL COMMENT 'Optional user scope foreign key.',
  `setting_value` text COLLATE utf8mb4_unicode_ci COMMENT 'Text setting value payload.',
  `setting_value_int` bigint(20) DEFAULT NULL COMMENT 'Integer setting value payload.',
  `setting_value_bool` tinyint(1) DEFAULT NULL COMMENT 'Boolean setting value payload.',
  `setting_value_decimal` decimal(18,6) DEFAULT NULL COMMENT 'Decimal setting value payload.',
  `setting_value_datetime_utc` datetime DEFAULT NULL COMMENT 'Datetime setting value payload in UTC.',
  `value_type` varchar(32) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Declared value type for the setting payload.',
  `updated_by_user_id` bigint(20) DEFAULT NULL COMMENT 'User who last updated the setting.',
  `updated_utc` datetime NOT NULL COMMENT 'UTC timestamp when setting was last updated.'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Current effective configuration setting values by scope.';

TRUNCATE TABLE `config_settings_values`;
INSERT INTO `config_settings_values` (`id`, `public_id`, `setting_key`, `scope_type`, `scope_key`, `computer_id`, `user_id`, `setting_value`, `setting_value_int`, `setting_value_bool`, `setting_value_decimal`, `setting_value_datetime_utc`, `value_type`, `updated_by_user_id`, `updated_utc`) VALUES
(1, '5681feaa-bdc8-11f1-bdd0-d843ae47d0d6', 'sessions.retention_inactive_days', 'all_users', 'all_users', NULL, NULL, NULL, 30, NULL, NULL, NULL, 'int', NULL, '2026-10-01 18:45:56'),
(2, '5682005f-bdc8-11f1-bdd0-d843ae47d0d6', 'waitlist.resolved_retention_days', 'all_users', 'all_users', NULL, NULL, NULL, 90, NULL, NULL, NULL, 'int', NULL, '2026-10-01 18:45:56'),
(3, '56820146-bdc8-11f1-bdd0-d843ae47d0d6', 'settings.history_retention_days', 'all_users', 'all_users', NULL, NULL, NULL, 30, NULL, NULL, NULL, 'int', NULL, '2026-10-01 18:45:56'),
(4, '5694b944-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.requests.handle', 'role', 'role:developer', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(5, '5694bab2-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.cache.refresh_api', 'role', 'role:developer', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(6, '5694bb6a-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.ignored_locations', 'role', 'role:developer', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(7, '5694bbf7-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.hot_work_centers', 'role', 'role:developer', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(8, '5694bc80-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.part_pictures', 'role', 'role:developer', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(9, '5694bd0a-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.cache_refresh', 'role', 'role:developer', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(10, '5694bd8c-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.urgency_minutes', 'role', 'role:developer', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(11, '5694be05-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.defect_types', 'role', 'role:developer', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(12, '5694be8a-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.computers', 'role', 'role:developer', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(13, '5694bf11-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.setup.dunnage_quick_add', 'role', 'role:developer', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(14, '5694bf8e-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.setup.work_centers', 'role', 'role:developer', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(15, '5694c00b-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.admin.users', 'role', 'role:developer', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(16, '5694c08a-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.admin.reset_password', 'role', 'role:developer', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(17, '5694c108-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.admin.permissions', 'role', 'role:developer', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(18, '5694c182-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.requests.handle', 'role', 'role:it_department', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(19, '5694c200-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.cache.refresh_api', 'role', 'role:it_department', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(20, '5694c282-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.ignored_locations', 'role', 'role:it_department', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(21, '5694c30d-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.hot_work_centers', 'role', 'role:it_department', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(22, '5694c399-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.part_pictures', 'role', 'role:it_department', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(23, '5694c41d-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.cache_refresh', 'role', 'role:it_department', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(24, '5694c4a0-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.urgency_minutes', 'role', 'role:it_department', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(25, '5694c52d-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.defect_types', 'role', 'role:it_department', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(26, '5694c5ba-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.computers', 'role', 'role:it_department', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(27, '5694c63a-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.setup.dunnage_quick_add', 'role', 'role:it_department', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(28, '5694c6bb-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.setup.work_centers', 'role', 'role:it_department', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(29, '5694c740-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.admin.users', 'role', 'role:it_department', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(30, '5694c7c1-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.admin.reset_password', 'role', 'role:it_department', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(31, '5694c848-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.admin.permissions', 'role', 'role:it_department', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(32, '5694c8c7-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.requests.handle', 'role', 'role:plant_manager', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(33, '5694c951-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.cache.refresh_api', 'role', 'role:plant_manager', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(34, '5694c9d9-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.ignored_locations', 'role', 'role:plant_manager', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(35, '5694ca63-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.hot_work_centers', 'role', 'role:plant_manager', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(36, '5694caf3-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.part_pictures', 'role', 'role:plant_manager', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(37, '5694cb86-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.cache_refresh', 'role', 'role:plant_manager', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(38, '5694cc10-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.urgency_minutes', 'role', 'role:plant_manager', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(39, '5694cca8-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.defect_types', 'role', 'role:plant_manager', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(40, '5694cd35-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.computers', 'role', 'role:plant_manager', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(41, '5694cdc2-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.setup.dunnage_quick_add', 'role', 'role:plant_manager', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(42, '5694ce4b-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.setup.work_centers', 'role', 'role:plant_manager', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(43, '5694ced5-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.admin.users', 'role', 'role:plant_manager', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(44, '5694cf59-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.admin.reset_password', 'role', 'role:plant_manager', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(45, '5694cfde-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.admin.permissions', 'role', 'role:plant_manager', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(46, '5694d063-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.requests.handle', 'role', 'role:production_lead', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(47, '5694d0ea-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.cache.refresh_api', 'role', 'role:production_lead', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(48, '5694d17e-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.ignored_locations', 'role', 'role:production_lead', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(49, '5694d214-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.hot_work_centers', 'role', 'role:production_lead', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(50, '5694d2a7-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.part_pictures', 'role', 'role:production_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(51, '5694d330-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.cache_refresh', 'role', 'role:production_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(52, '5694d3c4-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.urgency_minutes', 'role', 'role:production_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(53, '5694d45c-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.defect_types', 'role', 'role:production_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(54, '5694d4e5-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.computers', 'role', 'role:production_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(55, '5694d570-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.setup.dunnage_quick_add', 'role', 'role:production_lead', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(56, '5694d601-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.setup.work_centers', 'role', 'role:production_lead', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(57, '5694d689-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.admin.users', 'role', 'role:production_lead', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(58, '5694d707-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.admin.reset_password', 'role', 'role:production_lead', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(59, '5694d78d-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.admin.permissions', 'role', 'role:production_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(60, '5694d81a-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.requests.handle', 'role', 'role:setup_lead', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(61, '5694d8b1-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.cache.refresh_api', 'role', 'role:setup_lead', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(62, '5694d942-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.ignored_locations', 'role', 'role:setup_lead', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(63, '5694d9da-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.hot_work_centers', 'role', 'role:setup_lead', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(64, '5694da78-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.part_pictures', 'role', 'role:setup_lead', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(65, '5694db0a-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.cache_refresh', 'role', 'role:setup_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(66, '5694dba0-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.urgency_minutes', 'role', 'role:setup_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(67, '5694dc3e-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.defect_types', 'role', 'role:setup_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(68, '5694dccb-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.computers', 'role', 'role:setup_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(69, '5694dd60-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.setup.dunnage_quick_add', 'role', 'role:setup_lead', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(70, '5694ddf5-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.setup.work_centers', 'role', 'role:setup_lead', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(71, '5694de7f-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.admin.users', 'role', 'role:setup_lead', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(72, '5694df0a-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.admin.reset_password', 'role', 'role:setup_lead', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(73, '5694df9e-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.admin.permissions', 'role', 'role:setup_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(74, '5694e030-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.requests.handle', 'role', 'role:material_handler_lead', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(75, '5694e0b3-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.cache.refresh_api', 'role', 'role:material_handler_lead', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(76, '5694e144-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.ignored_locations', 'role', 'role:material_handler_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(77, '5694e1d9-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.hot_work_centers', 'role', 'role:material_handler_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(78, '5694e269-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.part_pictures', 'role', 'role:material_handler_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(79, '5694e2f2-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.cache_refresh', 'role', 'role:material_handler_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(80, '5694e381-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.urgency_minutes', 'role', 'role:material_handler_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(81, '5694e412-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.defect_types', 'role', 'role:material_handler_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(82, '5694e497-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.computers', 'role', 'role:material_handler_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(83, '5694e520-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.setup.dunnage_quick_add', 'role', 'role:material_handler_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(84, '5694e5ac-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.setup.work_centers', 'role', 'role:material_handler_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(85, '5694e635-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.admin.users', 'role', 'role:material_handler_lead', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(86, '5694e6c1-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.admin.reset_password', 'role', 'role:material_handler_lead', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(87, '5694e746-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.admin.permissions', 'role', 'role:material_handler_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(88, '5694e7cc-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.requests.handle', 'role', 'role:material_handler', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(89, '5694e85b-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.cache.refresh_api', 'role', 'role:material_handler', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(90, '5694e8e5-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.ignored_locations', 'role', 'role:material_handler', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(91, '5694e977-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.hot_work_centers', 'role', 'role:material_handler', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(92, '5694ea08-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.part_pictures', 'role', 'role:material_handler', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(93, '5694ea96-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.cache_refresh', 'role', 'role:material_handler', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(94, '5694eb28-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.urgency_minutes', 'role', 'role:material_handler', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(95, '5694ebb6-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.defect_types', 'role', 'role:material_handler', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(96, '5694ec3c-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.computers', 'role', 'role:material_handler', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(97, '5694eccc-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.setup.dunnage_quick_add', 'role', 'role:material_handler', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(98, '5694ed5a-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.setup.work_centers', 'role', 'role:material_handler', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(99, '5694ede2-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.admin.users', 'role', 'role:material_handler', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(100, '5694ee74-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.admin.reset_password', 'role', 'role:material_handler', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(101, '5694eeff-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.admin.permissions', 'role', 'role:material_handler', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(102, '5694ef8e-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.requests.handle', 'role', 'role:production', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(103, '5694f019-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.cache.refresh_api', 'role', 'role:production', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(104, '5694f0ad-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.ignored_locations', 'role', 'role:production', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(105, '5694f155-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.hot_work_centers', 'role', 'role:production', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(106, '5694f1f7-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.part_pictures', 'role', 'role:production', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(107, '5694f291-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.cache_refresh', 'role', 'role:production', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(108, '5694f334-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.urgency_minutes', 'role', 'role:production', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(109, '5694f3d8-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.defect_types', 'role', 'role:production', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(110, '5694f472-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.computers', 'role', 'role:production', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(111, '5694f50e-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.setup.dunnage_quick_add', 'role', 'role:production', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(112, '5694f91c-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.setup.work_centers', 'role', 'role:production', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(113, '5694f9db-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.admin.users', 'role', 'role:production', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(114, '5694fa72-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.admin.reset_password', 'role', 'role:production', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(115, '5694fb06-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.admin.permissions', 'role', 'role:production', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(116, '5694fb99-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.requests.handle', 'role', 'role:setup', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(117, '5694fecd-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.cache.refresh_api', 'role', 'role:setup', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(118, '5694ff84-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.ignored_locations', 'role', 'role:setup', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(119, '5695002f-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.hot_work_centers', 'role', 'role:setup', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(120, '569500eb-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.part_pictures', 'role', 'role:setup', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(121, '5695019f-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.cache_refresh', 'role', 'role:setup', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(122, '56950242-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.urgency_minutes', 'role', 'role:setup', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(123, '569502fa-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.defect_types', 'role', 'role:setup', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(124, '569503a6-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.computers', 'role', 'role:setup', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(125, '56950448-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.setup.dunnage_quick_add', 'role', 'role:setup', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(126, '569504e5-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.setup.work_centers', 'role', 'role:setup', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(127, '5695057a-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.admin.users', 'role', 'role:setup', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(128, '56950612-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.admin.reset_password', 'role', 'role:setup', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(129, '56950733-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.admin.permissions', 'role', 'role:setup', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(130, '5695aa0b-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.storage_paths', 'role', 'role:developer', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(131, '5695ab22-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.storage_paths', 'role', 'role:it_department', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(132, '5695abd2-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.storage_paths', 'role', 'role:plant_manager', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(133, '5695ac73-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.storage_paths', 'role', 'role:production_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(134, '5695ad13-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.storage_paths', 'role', 'role:setup_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(135, '5695adb7-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.storage_paths', 'role', 'role:material_handler_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(136, '5695ae55-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.storage_paths', 'role', 'role:material_handler', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(137, '5695aeeb-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.storage_paths', 'role', 'role:production', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(138, '5695af90-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.storage_paths', 'role', 'role:setup', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(139, '56960137-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.machine_configuration', 'role', 'role:developer', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(140, '56960235-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.machine_configuration', 'role', 'role:it_department', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(141, '569602e7-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.machine_configuration', 'role', 'role:plant_manager', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(142, '5696038c-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.machine_configuration', 'role', 'role:production_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(143, '56960428-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.machine_configuration', 'role', 'role:setup_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(144, '569604c9-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.machine_configuration', 'role', 'role:material_handler_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(145, '56960571-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.machine_configuration', 'role', 'role:material_handler', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(146, '56960613-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.machine_configuration', 'role', 'role:production', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(147, '569606ac-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.machine_configuration', 'role', 'role:setup', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(148, '569656dc-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.ignored_locations_edit', 'role', 'role:developer', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(149, '569657d1-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.ignored_locations_edit', 'role', 'role:it_department', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(150, '56965877-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.ignored_locations_edit', 'role', 'role:plant_manager', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(151, '56965917-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.ignored_locations_edit', 'role', 'role:production_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(152, '569659b8-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.ignored_locations_edit', 'role', 'role:setup_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(153, '56965a63-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.ignored_locations_edit', 'role', 'role:material_handler_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(154, '56965b08-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.ignored_locations_edit', 'role', 'role:material_handler', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(155, '56965ba8-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.ignored_locations_edit', 'role', 'role:production', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(156, '56965c48-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.ignored_locations_edit', 'role', 'role:setup', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(157, '5696a89a-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.log_panel', 'role', 'role:developer', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(158, '5696a9e0-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.log_panel', 'role', 'role:it_department', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(159, '5696aa8e-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.log_panel', 'role', 'role:plant_manager', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(160, '5696ab30-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.log_panel', 'role', 'role:production_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(161, '5696abc7-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.log_panel', 'role', 'role:setup_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(162, '5696ac5c-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.log_panel', 'role', 'role:material_handler_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(163, '5696acf4-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.log_panel', 'role', 'role:material_handler', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(164, '5696ad88-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.log_panel', 'role', 'role:production', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(165, '5696ae1b-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.log_panel', 'role', 'role:setup', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(166, '5696ff22-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.session_length', 'role', 'role:developer', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(167, '56970080-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.session_length', 'role', 'role:it_department', NULL, NULL, NULL, NULL, 1, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(168, '5697013a-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.session_length', 'role', 'role:plant_manager', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(169, '569701e5-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.session_length', 'role', 'role:production_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(170, '56970289-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.session_length', 'role', 'role:setup_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(171, '56970331-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.session_length', 'role', 'role:material_handler_lead', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(172, '569703d3-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.session_length', 'role', 'role:material_handler', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(173, '569750c8-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.session_length', 'role', 'role:production', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(174, '569751a3-bdc8-11f1-bdd0-d843ae47d0d6', 'permission.settings.session_length', 'role', 'role:setup', NULL, NULL, NULL, NULL, 0, NULL, NULL, 'bool', NULL, '2026-10-01 18:45:56'),
(175, '5697a1ed-bdc8-11f1-bdd0-d843ae47d0d6', 'Feature.IgnoredLocations', 'all_users', 'all_users', NULL, NULL, 'WC,NCM,V-WC,NCM-VITS,SHIP', NULL, NULL, NULL, NULL, 'text', NULL, '2026-10-01 18:45:56'),
(176, '5697f01d-bdc8-11f1-bdd0-d843ae47d0d6', 'auth.session_length_minutes', 'all_users', 'all_users', NULL, NULL, NULL, 480, NULL, NULL, NULL, 'int', NULL, '2026-10-01 18:45:56'),
(177, 'ca0d87b2-c585-11f1-9c65-d843ae47d0d6', 'Waitlist.MessageSeen', 'user', 'user:2', NULL, 2, '{\"f0000000-0006-4000-8000-000000000006\":\"2026-10-11T15:09:35.5339564+00:00\",\"f0000000-0004-4000-8000-000000000004\":\"2026-10-11T15:09:48.3931386+00:00\",\"f0000000-0002-4000-8000-000000000002\":\"2026-10-11T15:30:42.1686166+00:00\",\"f0000000-0011-4000-8000-000000000011\":\"2026-10-11T15:31:21.2746103+00:00\",\"f0000000-0001-4000-8000-000000000001\":\"2026-10-11T15:31:23.9815478+00:00\",\"f0000000-0005-4000-8000-000000000005\":\"2026-10-11T15:31:30.274715+00:00\",\"f0000000-0008-4000-8000-000000000008\":\"2026-10-11T15:32:42.6857929+00:00\",\"f0000000-0003-4000-8000-000000000003\":\"2026-10-11T15:32:58.5587962+00:00\",\"f0000000-0010-4000-8000-000000000010\":\"2026-10-11T15:33:03.2375346+00:00\",\"1a785e68-cd4a-4b09-9e60-fa7a04cd3b0e\":\"2026-10-11T15:50:50.2796795+00:00\",\"0a0f160f-3320-4510-9dfb-c8068897f841\":\"2026-10-12T00:40:29.5353592+00:00\",\"171dece4-2358-49f6-b333-52c92e16693d\":\"2026-10-11T23:38:14.6702372+00:00\",\"99107b5a-8e65-4597-84a2-743590126a82\":\"2026-10-11T23:38:12.3802548+00:00\",\"f25ca85b-4def-4fd5-8f22-3d870f9c12b4\":\"2026-10-11T23:41:33.6711219+00:00\",\"9ca5e8b3-2d60-477e-adde-53378f5b947a\":\"2026-10-11T23:54:49.0824702+00:00\"}', NULL, NULL, NULL, NULL, 'text', 2, '2026-10-12 00:40:29');

DROP TABLE IF EXISTS `core_computers_registry`;
CREATE TABLE `core_computers_registry` (
  `id` bigint(20) NOT NULL COMMENT 'Surrogate primary key.',
  `public_id` char(36) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Public UUID for computer record.',
  `computer_name` varchar(128) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Friendly computer name.',
  `hostname_normalized` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Normalized host name used for identity matching.',
  `mac_address_normalized` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Normalized MAC address for computer identity.',
  `display_name` varchar(128) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'The name the computer reports for itself, held uniquely so no two records claim one name.',
  `location` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Where this computer stands; the only value a person supplies at setup.',
  `is_registered` tinyint(1) NOT NULL DEFAULT '1' COMMENT 'Whether the computer is currently registered.',
  `created_utc` datetime NOT NULL COMMENT 'UTC timestamp when the row was created.',
  `updated_utc` datetime NOT NULL COMMENT 'UTC timestamp when the row was last updated.'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Registered computer and host identity catalog.';

TRUNCATE TABLE `core_computers_registry`;
INSERT INTO `core_computers_registry` (`id`, `public_id`, `computer_name`, `hostname_normalized`, `mac_address_normalized`, `display_name`, `location`, `is_registered`, `created_utc`, `updated_utc`) VALUES
(1, '56938ec3-bdc8-11f1-bdd0-d843ae47d0d6', 'johnspc', 'johnspc', 'd8-43-ae-47-d0-d6', 'John\'s Computer', NULL, 1, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(2, '56939015-bdc8-11f1-bdd0-d843ae47d0d6', 'mtmfg-161', 'mtmfg-161', 'f4-f1-9e-38-64-d3', 'MTMFG 161', NULL, 1, '2026-10-01 18:45:56', '2026-10-01 18:45:56');

DROP TABLE IF EXISTS `core_users_profiles`;
CREATE TABLE `core_users_profiles` (
  `id` bigint(20) NOT NULL COMMENT 'Surrogate primary key.',
  `public_id` char(36) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Public UUID for external references.',
  `username_normalized` varchar(128) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Normalized unique username for sign-in lookup.',
  `first_name` varchar(128) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'The person given name, 1 to 128 characters.',
  `last_name` varchar(128) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'The person family name, 1 to 128 characters.',
  `password_hash` varchar(128) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '0000' COMMENT 'Password hash value for authentication.',
  `password_salt` varbinary(32) DEFAULT NULL COMMENT 'Per-user password salt.',
  `require_password_change` tinyint(1) NOT NULL DEFAULT '1' COMMENT 'Flag requiring password reset on next login.',
  `temporary_credential_failed_attempts` int(11) NOT NULL DEFAULT '0' COMMENT 'Wrong attempts made with a temporary credential. Cleared by a successful attempt or a fresh reset; never expires with time.',
  `display_name` varchar(256) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Display name shown in the UI.',
  `employee_identifier` varchar(128) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Optional employee number or identifier.',
  `is_active` tinyint(1) NOT NULL DEFAULT '1' COMMENT 'Whether the user account is active.',
  `created_utc` datetime NOT NULL COMMENT 'UTC timestamp when the row was created.',
  `updated_utc` datetime NOT NULL COMMENT 'UTC timestamp when the row was last updated.'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='User profile and authentication identity records.';

TRUNCATE TABLE `core_users_profiles`;
INSERT INTO `core_users_profiles` (`id`, `public_id`, `username_normalized`, `first_name`, `last_name`, `password_hash`, `password_salt`, `require_password_change`, `temporary_credential_failed_attempts`, `display_name`, `employee_identifier`, `is_active`, `created_utc`, `updated_utc`) VALUES
(1, '568475f3-bdc8-11f1-bdd0-d843ae47d0d6', 'JOHNK', 'John', 'Koll', 'CxrKO1xEmsC1VtDhgp2Envsig5vQ8ZErO8J1g7B7q2o=', 0xc3b851513baf5951482cec1d8d585ebf, 1, 0, 'John Koll', '6229', 1, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(2, '5684779b-bdc8-11f1-bdd0-d843ae47d0d6', 'JKOLL', 'John', 'Koll', 'W9FIrOU6tuKcI083q1L1tt4YT/fxjVOD+O6jRpLC1QQ=', 0xa0d48fb22ce29f09d1fded233b72d7d9, 0, 0, 'John Koll', '6229', 1, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(3, '56847864-bdc8-11f1-bdd0-d843ae47d0d6', 'TEST.ADMIN', 'Test', 'Admin', 'CxrKO1xEmsC1VtDhgp2Envsig5vQ8ZErO8J1g7B7q2o=', 0xc3b851513baf5951482cec1d8d585ebf, 1, 0, 'Test Admin', '9001', 1, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(4, '568478cb-bdc8-11f1-bdd0-d843ae47d0d6', 'TEST.DEVELOPER', 'Test', 'Developer', 'CxrKO1xEmsC1VtDhgp2Envsig5vQ8ZErO8J1g7B7q2o=', 0xc3b851513baf5951482cec1d8d585ebf, 1, 0, 'Test Developer', '9002', 1, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(5, '56847922-bdc8-11f1-bdd0-d843ae47d0d6', 'TEST.PLANT.MANAGER', 'Test', 'Plant Manager', 'CxrKO1xEmsC1VtDhgp2Envsig5vQ8ZErO8J1g7B7q2o=', 0xc3b851513baf5951482cec1d8d585ebf, 1, 0, 'Test Plant Manager', '9003', 1, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(6, '56847981-bdc8-11f1-bdd0-d843ae47d0d6', 'TEST.SETUP.LEAD', 'Test', 'Setup Lead', 'CxrKO1xEmsC1VtDhgp2Envsig5vQ8ZErO8J1g7B7q2o=', 0xc3b851513baf5951482cec1d8d585ebf, 1, 0, 'Test Setup Lead', '9004', 1, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(7, '568479d5-bdc8-11f1-bdd0-d843ae47d0d6', 'TEST.PRODUCTION.LEAD', 'Test', 'Production Lead', 'CxrKO1xEmsC1VtDhgp2Envsig5vQ8ZErO8J1g7B7q2o=', 0xc3b851513baf5951482cec1d8d585ebf, 1, 0, 'Test Production Lead', '9005', 1, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(8, '56847a3b-bdc8-11f1-bdd0-d843ae47d0d6', 'TEST.SETUP', 'Test', 'Setup', 'CxrKO1xEmsC1VtDhgp2Envsig5vQ8ZErO8J1g7B7q2o=', 0xc3b851513baf5951482cec1d8d585ebf, 1, 0, 'Test Setup', '9006', 1, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(9, '56847aa8-bdc8-11f1-bdd0-d843ae47d0d6', 'TEST.PRODUCTION', 'Test', 'Production', 'CxrKO1xEmsC1VtDhgp2Envsig5vQ8ZErO8J1g7B7q2o=', 0xc3b851513baf5951482cec1d8d585ebf, 1, 0, 'Test Production', '9007', 1, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(10, '56847b0b-bdc8-11f1-bdd0-d843ae47d0d6', 'TEST.MATERIAL.HANDLER', 'Test', 'Material Handler', 'CxrKO1xEmsC1VtDhgp2Envsig5vQ8ZErO8J1g7B7q2o=', 0xc3b851513baf5951482cec1d8d585ebf, 1, 0, 'Test Material Handler', '9008', 1, '2026-10-01 18:45:56', '2026-10-01 18:45:56');

DROP TABLE IF EXISTS `ops_startup_logs`;
CREATE TABLE `ops_startup_logs` (
  `id` bigint(20) NOT NULL COMMENT 'Surrogate primary key.',
  `public_id` char(36) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Public UUID for log record.',
  `correlation_id` char(36) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Correlation UUID for grouped startup events.',
  `created_utc` datetime NOT NULL COMMENT 'UTC timestamp of log event creation.',
  `level` varchar(16) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Log severity level.',
  `event_action` varchar(128) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Event action or operation name.',
  `outcome` varchar(32) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Outcome status for event.',
  `actor_kind` varchar(32) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Actor type for event source.',
  `actor_id` varchar(128) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Actor identifier value.',
  `host_id` varchar(128) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Host/computer identifier captured for event.',
  `mac_address` varchar(64) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'MAC address captured for event.',
  `module` varchar(128) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'The part of the application the entry came from; an ILogger category name lands here, and a category is a namespace-qualified type name.',
  `error_type` varchar(128) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'The fault type, when there was a fault.',
  `exception_detail` mediumtext COLLATE utf8mb4_unicode_ci COMMENT 'The serialized exception chain, one JSON node per exception, outermost first.',
  `error_fingerprint` char(64) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'SHA-256 of the fault shape, so the same fault groups together across machines; NULL without an exception.',
  `message` text COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Primary log message text.',
  `payload_json` mediumtext COLLATE utf8mb4_unicode_ci COMMENT 'Optional structured payload as JSON text.',
  `previous_hash` char(64) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Previous entry hash for chain validation.',
  `entry_hash` char(64) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Hash for current log entry integrity.'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Startup and operational telemetry log entries.';

TRUNCATE TABLE `ops_startup_logs`;
INSERT INTO `ops_startup_logs` (`id`, `public_id`, `correlation_id`, `created_utc`, `level`, `event_action`, `outcome`, `actor_kind`, `actor_id`, `host_id`, `mac_address`, `module`, `error_type`, `exception_detail`, `error_fingerprint`, `message`, `payload_json`, `previous_hash`, `entry_hash`) VALUES
(1, 'ca729740-f512-4b9b-995f-9c5f19d671dd', 'de3e8e87-8578-49bc-aa3b-7d62357af449', '2026-10-01 18:46:48', 'info', 'App', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'App', NULL, NULL, NULL, 'Host built.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30580\",\"threadId\":\"2\",\"uptimeSeconds\":\"0\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', NULL, 'b42266c081c02d2920b46d1667dbaea92847f2cca959a42c73b99329d79497c7'),
(2, 'c0f0c762-5fcd-46ee-80b7-da48485d75a0', 'dcb4d119-ba1f-4063-b484-117c9ba2527a', '2026-10-01 18:46:48', 'info', 'App', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'App', NULL, NULL, NULL, 'Visual reachability probe host started.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30580\",\"threadId\":\"2\",\"uptimeSeconds\":\"0\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'b42266c081c02d2920b46d1667dbaea92847f2cca959a42c73b99329d79497c7', 'f64b90c7142457c1bb9f7882248bdb7639a43f8f2f230feef39f68cc46f76cdb'),
(3, '333aca15-0c69-4728-aa44-d89b28b32fdf', '0e15d365-dc4f-46e3-bcff-ae88f04c2c92', '2026-10-01 18:46:48', 'info', 'App', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'App', NULL, NULL, NULL, 'App notification service initialized.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30580\",\"threadId\":\"2\",\"uptimeSeconds\":\"0\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'f64b90c7142457c1bb9f7882248bdb7639a43f8f2f230feef39f68cc46f76cdb', '44f3db3b7f915961c14dfa07bc4e0bdacee8fa50d6b568f8f73d2f411bc20769'),
(4, '169a75d9-da4a-46a6-9a94-40a87c366206', '0e4c0617-9a6b-4331-8459-2749f30c58fe', '2026-10-01 18:46:48', 'info', 'App', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'App', NULL, NULL, NULL, 'UnhandledException handler registered.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30580\",\"threadId\":\"2\",\"uptimeSeconds\":\"0\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '44f3db3b7f915961c14dfa07bc4e0bdacee8fa50d6b568f8f73d2f411bc20769', '48746e71739a880e4305752d53a0007b9357e3ea64caa6bde48e7728d88f976b'),
(5, '20478c88-2478-4c24-b16c-6b17dc55c26f', 'e9cdf40d-93ac-490b-b17f-06be73e88281', '2026-10-01 18:46:48', 'info', 'App', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'App', NULL, NULL, NULL, 'Background-thread fault handler registered.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30580\",\"threadId\":\"2\",\"uptimeSeconds\":\"0\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '48746e71739a880e4305752d53a0007b9357e3ea64caa6bde48e7728d88f976b', '79b217b3e4ff6f31dcc467d33bf193eabe3911b0cdc0b78c15865aaa43d793a8'),
(6, 'c6a5c49e-2f57-4081-bf77-552137151ceb', '95d91283-e421-4efb-9c3b-e4facc22a747', '2026-10-01 18:46:48', 'info', 'Launch', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'Launch', NULL, NULL, NULL, 'OnLaunched started.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30580\",\"threadId\":\"2\",\"uptimeSeconds\":\"0\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '79b217b3e4ff6f31dcc467d33bf193eabe3911b0cdc0b78c15865aaa43d793a8', '7fe8e8df7288b39f8baf6970bb1826a7fb47a2b72c3ba44933d64a19186fc07a'),
(7, '95f5eef6-7be3-496c-9a13-17bae0d552dd', 'b6d9317d-8ae6-4eb2-a9d3-e8c7b135765a', '2026-10-01 18:46:48', 'info', 'Microsoft.Hosting.Lifetime', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'Microsoft.Hosting.Lifetime', NULL, NULL, NULL, 'Application started. Press Ctrl+C to shut down.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30580\",\"threadId\":\"2\",\"uptimeSeconds\":\"0\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null},\"properties\":{\"{OriginalFormat}\":\"Application started. Press Ctrl\\u002BC to shut down.\"}}', '7fe8e8df7288b39f8baf6970bb1826a7fb47a2b72c3ba44933d64a19186fc07a', '06cb128126806d9efdd71b2e1f5b7a0d4af9c6ad4e8f422fe34feee9439bca70'),
(8, '94bce854-ec20-4ac7-afb5-4654796b1da5', '80e34339-ed49-4e1f-b9df-404e931b97a7', '2026-10-01 18:46:48', 'info', 'Microsoft.Hosting.Lifetime', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'Microsoft.Hosting.Lifetime', NULL, NULL, NULL, 'Hosting environment: Production', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30580\",\"threadId\":\"2\",\"uptimeSeconds\":\"0\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null},\"properties\":{\"EnvName\":\"Production\",\"{OriginalFormat}\":\"Hosting environment: {EnvName}\"}}', '06cb128126806d9efdd71b2e1f5b7a0d4af9c6ad4e8f422fe34feee9439bca70', '620dd9cd813e39cf3f29b82760cc7e08b6cf04d5d626b65113b75bf86817aa77'),
(9, '841d9f24-f8c7-4135-9b45-107bbf5ca72e', 'c2103cf5-792c-4109-b9da-692a18e352f9', '2026-10-01 18:46:48', 'info', 'Microsoft.Hosting.Lifetime', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'Microsoft.Hosting.Lifetime', NULL, NULL, NULL, 'Content root path: C:\\Users\\johnk\\source\\repos\\MTM_Waitlist\\bin\\x64\\Debug\\net10.0-windows10.0.19041.0\\win-x64\\', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30580\",\"threadId\":\"2\",\"uptimeSeconds\":\"0\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null},\"properties\":{\"ContentRoot\":\"C:\\\\Users\\\\johnk\\\\source\\\\repos\\\\MTM_Waitlist\\\\bin\\\\x64\\\\Debug\\\\net10.0-windows10.0.19041.0\\\\win-x64\\\\\",\"{OriginalFormat}\":\"Content root path: {ContentRoot}\"}}', '620dd9cd813e39cf3f29b82760cc7e08b6cf04d5d626b65113b75bf86817aa77', '3a57b6728106969e89cf0686b8eeb76f5fc85bac264afa065b4983b64c313bcc'),
(10, '5e788807-9044-40a4-a34b-b19b3dc74b8f', '99daf947-1b20-4df3-9fa8-23694ccbb270', '2026-10-01 18:46:48', 'info', 'Launch', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'Launch', NULL, NULL, NULL, 'Host started.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30580\",\"threadId\":\"2\",\"uptimeSeconds\":\"0\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '3a57b6728106969e89cf0686b8eeb76f5fc85bac264afa065b4983b64c313bcc', '655eac7c061e41aff9d805b0de17cd6c1b53d851183c32f6a39f1e197d88e6d0'),
(11, '1416e80a-dd6a-49c4-b3e2-0f6395629e5e', '3ea5714a-4418-43e0-aacf-bf446c34f277', '2026-10-01 18:46:48', 'info', 'MySqlHelperServer', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'MySqlHelperServer', NULL, NULL, NULL, 'ExecuteStoredProcedureQueryAsync started. Procedure=\'sp_core_computers_registry_get_all\', Target=\'mtm_waitlist\', Attempt=1.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30580\",\"threadId\":\"2\",\"uptimeSeconds\":\"1\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '655eac7c061e41aff9d805b0de17cd6c1b53d851183c32f6a39f1e197d88e6d0', '522b800a3c9869f00ab5b9a49b747589480d42716869549d7e8009185d9dcb5b'),
(12, '0ea87890-5f1c-490a-9ac5-957a909e7303', 'a2701c29-5bcf-4923-b9d1-d6eafd2ccf83', '2026-10-01 18:46:48', 'info', 'Launch', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'Launch', NULL, NULL, NULL, 'The launch was started.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30580\",\"threadId\":\"2\",\"uptimeSeconds\":\"1\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '522b800a3c9869f00ab5b9a49b747589480d42716869549d7e8009185d9dcb5b', '30a673c8f81152a17c29947c6c468baaedf07ab4b1ee3c1933aad2a8ce76d84f'),
(13, '897666a4-d0e9-45f5-9e21-744f38b34133', '29bf9a37-8072-4bbe-9b13-4dbef645f715', '2026-10-01 18:46:48', 'info', 'MySqlHelperServer', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'MySqlHelperServer', NULL, NULL, NULL, 'ExecuteStoredProcedureQueryAsync started. Procedure=\'sp_core_computers_registry_lookup_by_name_get\', Target=\'mtm_waitlist\', Attempt=1.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30580\",\"threadId\":\"8\",\"uptimeSeconds\":\"1\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '30a673c8f81152a17c29947c6c468baaedf07ab4b1ee3c1933aad2a8ce76d84f', '8499552d03fdb94a807409f9b79624d6e680e1107affe2c1b863a8452d1e556f'),
(14, 'b3565b4d-b013-4413-93ba-e3bd15a50c4a', '726b56e0-e39a-4ab7-b368-7d1df6847a91', '2026-10-01 18:46:48', 'info', 'MySqlHelperServer', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'MySqlHelperServer', NULL, NULL, NULL, 'ExecuteStoredProcedureQueryAsync started. Procedure=\'sp_core_computers_registry_lookup_by_name_get\', Target=\'mtm_waitlist\', Attempt=1.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30580\",\"threadId\":\"15\",\"uptimeSeconds\":\"1\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '8499552d03fdb94a807409f9b79624d6e680e1107affe2c1b863a8452d1e556f', '18ab0801345ddfbdfe791cc70be257f72dfe41c6f1ffe18971825fbbcd391038'),
(15, 'e2f1d563-869a-4db1-b564-42b139e40782', '0325fecb-9898-4a42-b236-f73dbd734bff', '2026-10-01 18:46:48', 'info', 'MySqlHelperServer', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'MySqlHelperServer', NULL, NULL, NULL, 'ExecuteStoredProcedureQueryAsync started. Procedure=\'sp_core_computers_registry_lookup_by_name_get\', Target=\'mtm_waitlist\', Attempt=1.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30580\",\"threadId\":\"8\",\"uptimeSeconds\":\"1\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '18ab0801345ddfbdfe791cc70be257f72dfe41c6f1ffe18971825fbbcd391038', '4d241060631f6aa4710445ca6c9db61b3116f883c2bdb98705deb9d9edd7b57e'),
(16, '93b08a61-4d2f-4c3d-9deb-b6f571dcaa19', 'd60bb3de-c1bc-42c9-8f3f-f482d80143a0', '2026-10-01 18:46:48', 'info', 'MySqlHelperServer', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'MySqlHelperServer', NULL, NULL, NULL, 'ExecuteStoredProcedureQueryAsync started. Procedure=\'sp_core_computers_registry_lookup_by_name_get\', Target=\'mtm_waitlist\', Attempt=1.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30580\",\"threadId\":\"8\",\"uptimeSeconds\":\"1\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '4d241060631f6aa4710445ca6c9db61b3116f883c2bdb98705deb9d9edd7b57e', '02bc66358300fd5c15141f7249843bc38df93815ab9a9e90c84cc1c5cfd99bd9'),
(17, 'de3cb223-8da9-40bb-a2dd-885096018561', 'ec3c4cb1-0ede-4ccb-baf9-01f8b2e29681', '2026-10-01 18:46:48', 'info', 'Launch', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'Launch', NULL, NULL, NULL, 'The launch ended at SignIn.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30580\",\"threadId\":\"8\",\"uptimeSeconds\":\"1\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '02bc66358300fd5c15141f7249843bc38df93815ab9a9e90c84cc1c5cfd99bd9', 'de8a121707c143db053a3ddc868f8162f692bdae7440b8f6fca78539c55e7eba'),
(18, '0662bf25-390a-41c1-9f57-de9b9c67be44', '7062a090-5ad3-4d23-a273-0548ad36be77', '2026-10-01 18:46:49', 'info', 'VisualProbe', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'VisualProbe', NULL, NULL, NULL, 'Infor Visual did not answer within 2022 ms. TaskCanceledException: A task was canceled.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30580\",\"threadId\":\"15\",\"uptimeSeconds\":\"2\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'de8a121707c143db053a3ddc868f8162f692bdae7440b8f6fca78539c55e7eba', '5a7f626b759e2f7db344952ac751353a28f081230bbf3edeaa3f5e37cf2ead44'),
(19, '11cb2b06-1cfa-4fd7-938e-5b59416f745d', '2a2edd17-d6d5-4979-925b-787cff03b6f2', '2026-10-01 18:46:50', 'info', 'MySqlHelperServer', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'MySqlHelperServer', NULL, NULL, NULL, 'ExecuteStoredProcedureQueryAsync started. Procedure=\'sp_visual_read_shape_freshness_get\', Target=\'mtm_mock\', Attempt=1.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30580\",\"threadId\":\"15\",\"uptimeSeconds\":\"3\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '5a7f626b759e2f7db344952ac751353a28f081230bbf3edeaa3f5e37cf2ead44', '11cb7582ebff057c27584ad792123e1cf3d7dc0f877d09cf900ce260a82ed470'),
(20, '916dc47a-b06b-43ce-ac97-10ef04fd718d', '385a760a-3ec5-4685-8c86-a2c71c3a0ef5', '2026-10-01 18:46:50', 'info', 'StoreAvailabilityTracker', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'StoreAvailabilityTracker', NULL, NULL, NULL, 'Store \'mtm_mock\' is now Available.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30580\",\"threadId\":\"15\",\"uptimeSeconds\":\"3\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '11cb7582ebff057c27584ad792123e1cf3d7dc0f877d09cf900ce260a82ed470', '6c588de4457e492b1b62e340345a12b64f3b1a675d5eca8dcffac05585e1dda5'),
(21, '54012417-1ed3-4b06-b4c8-44d6f2946986', '9dc53180-e8cc-4e5b-80f5-bf2e87042e6d', '2026-10-01 18:46:58', 'info', 'MySqlHelperServer', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'MySqlHelperServer', NULL, NULL, NULL, 'ExecuteStoredProcedureQueryAsync started. Procedure=\'sp_auth_user_credential_get\', Target=\'mtm_waitlist\', Attempt=1.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30580\",\"threadId\":\"2\",\"uptimeSeconds\":\"11\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '6c588de4457e492b1b62e340345a12b64f3b1a675d5eca8dcffac05585e1dda5', '6a26d0957e399f915192bdaad658a796753ba3c2d62f4290cd43fe0df4533b39'),
(22, 'e529e6d1-7218-4496-b0c1-83fbc8de9a30', 'd3b86928-5144-454c-9930-8c74165b776c', '2026-10-01 18:46:58', 'info', 'MySqlHelperServer', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'MySqlHelperServer', NULL, NULL, NULL, 'ExecuteStoredProcedureNonQueryAsync started. Procedure=\'sp_auth_temporary_credential_attempt_record\', Target=\'mtm_waitlist\', Attempt=1.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30580\",\"threadId\":\"15\",\"uptimeSeconds\":\"11\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '6a26d0957e399f915192bdaad658a796753ba3c2d62f4290cd43fe0df4533b39', 'db73555831c2458e4933dd2f11017f6106108717eee407e85104a20581c0425f'),
(23, '15c0e3e0-0dd6-4920-8c3d-d688a63724bc', 'bbda82f9-b51b-4fc8-b755-83dc3b3ae45b', '2026-10-01 18:46:58', 'info', 'MySqlHelperServer', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'MySqlHelperServer', NULL, NULL, NULL, 'ExecuteStoredProcedureNonQueryAsync started. Procedure=\'sp_auth_remembered_sign_ins_clear\', Target=\'mtm_waitlist\', Attempt=1.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30580\",\"threadId\":\"2\",\"uptimeSeconds\":\"11\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'db73555831c2458e4933dd2f11017f6106108717eee407e85104a20581c0425f', 'd0907db1655d70b8422436d80d95976fed47ea77a76739fa5ea344c4d99d26e6'),
(24, 'fde83aa3-84f3-4e0d-995b-2e04f096a51a', 'b2736057-803c-432e-a8b1-98fc807b0112', '2026-10-01 18:47:02', 'info', 'MySqlHelperServer', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'MySqlHelperServer', NULL, NULL, NULL, 'ExecuteStoredProcedureQueryAsync started. Procedure=\'sp_auth_user_row_get\', Target=\'mtm_waitlist\', Attempt=1.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30580\",\"threadId\":\"2\",\"uptimeSeconds\":\"15\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'd0907db1655d70b8422436d80d95976fed47ea77a76739fa5ea344c4d99d26e6', '570367e2d6ae5272e9a9d139241b67e6b4c32a7fc1175c8518cbe1afc9cc9068'),
(25, 'fb49ae41-dc71-4d1d-ba15-a1a93e192e47', '0da0211e-b9cb-411f-b3d8-d176aadcbec1', '2026-10-01 18:47:02', 'info', 'MySqlHelperServer', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'MySqlHelperServer', NULL, NULL, NULL, 'ExecuteStoredProcedureQueryAsync started. Procedure=\'sp_auth_user_roles_get\', Target=\'mtm_waitlist\', Attempt=1.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30580\",\"threadId\":\"8\",\"uptimeSeconds\":\"15\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '570367e2d6ae5272e9a9d139241b67e6b4c32a7fc1175c8518cbe1afc9cc9068', 'e3528bbdb5d45c38a067ce2ae84bc4bfa2093f8659affa62b722fce8e3599f63'),
(26, 'fe07bbf1-bdcc-4823-a810-077ef21d3b75', '74056bae-64fc-494b-b061-098d3f60e9fe', '2026-10-01 18:47:02', 'info', 'MySqlHelperServer', 'Success', 'user', '2', 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'MySqlHelperServer', NULL, NULL, NULL, 'ExecuteStoredProcedureQueryAsync started. Procedure=\'sp_config_settings_values_get\', Target=\'mtm_waitlist\', Attempt=1.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30580\",\"threadId\":\"8\",\"uptimeSeconds\":\"15\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'e3528bbdb5d45c38a067ce2ae84bc4bfa2093f8659affa62b722fce8e3599f63', 'd242a865c7aab2751b6061b633fff50bd9b7cbc56fc21e9d660347aeec60c199'),
(27, '3f8d7c31-daf0-49c5-a1ac-e9dfcdfe4446', '067f2dbf-ee96-41ba-8978-3e6015fe66d9', '2026-10-01 18:47:02', 'info', 'MySqlHelperServer', 'Success', 'user', '2', 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'MySqlHelperServer', NULL, NULL, NULL, 'ExecuteStoredProcedureNonQueryAsync started. Procedure=\'sp_auth_user_active_sessions_upsert\', Target=\'mtm_waitlist\', Attempt=1.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30580\",\"threadId\":\"8\",\"uptimeSeconds\":\"15\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'd242a865c7aab2751b6061b633fff50bd9b7cbc56fc21e9d660347aeec60c199', 'e7bb8efd3c5c6a5a8e9c60b55b13eee0876e72b0150ef11b8885318289215df4'),
(28, 'e6e2bd53-e1d7-4325-8aba-a2dbc640e424', 'a41bd8b5-c9cf-409a-af60-318e102069fb', '2026-10-01 18:47:02', 'info', 'MySqlHelperServer', 'Success', 'user', '2', 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'MySqlHelperServer', NULL, NULL, NULL, 'ExecuteStoredProcedureQueryAsync started. Procedure=\'sp_auth_user_active_sessions_get\', Target=\'mtm_waitlist\', Attempt=1.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30580\",\"threadId\":\"8\",\"uptimeSeconds\":\"15\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'e7bb8efd3c5c6a5a8e9c60b55b13eee0876e72b0150ef11b8885318289215df4', 'c65c53ed2abdfc363178dbd1ba323233ba426fd5f7e49d155b22dd69c3cf2a7f'),
(29, '4c033b7f-58c2-4058-ad49-e875cfef1ef5', '0cae4c30-de91-4132-bf82-f2dd43c8ba9d', '2026-10-01 18:47:02', 'info', 'MySqlHelperServer', 'Success', 'user', '2', 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'MySqlHelperServer', NULL, NULL, NULL, 'ExecuteStoredProcedureQueryAsync started. Procedure=\'sp_core_computers_registry_lookup_by_name_get\', Target=\'mtm_waitlist\', Attempt=1.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30580\",\"threadId\":\"8\",\"uptimeSeconds\":\"15\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'c65c53ed2abdfc363178dbd1ba323233ba426fd5f7e49d155b22dd69c3cf2a7f', '729d4d4a495236a3d77976169cba7a20c4e66cb7aa558cace0817d9fd719580e'),
(30, '79ec31a1-7c25-47d9-bd85-2d1c4a9d40e1', '9e600cbd-58cd-400d-a382-375f83e416da', '2026-10-01 18:47:02', 'info', 'MySqlHelperServer', 'Success', 'user', '2', 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'MySqlHelperServer', NULL, NULL, NULL, 'ExecuteStoredProcedureNonQueryAsync started. Procedure=\'sp_auth_user_password_set\', Target=\'mtm_waitlist\', Attempt=1.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30580\",\"threadId\":\"8\",\"uptimeSeconds\":\"15\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '729d4d4a495236a3d77976169cba7a20c4e66cb7aa558cace0817d9fd719580e', '9940b3821748b662df6a200cdbde14d0de5cdbf7ef2490a0a55f1bb7e879592a'),
(31, '2dadec38-70ad-4b2f-917a-ab24a38a79b3', '027232f0-0a5b-4298-bc13-60b87fd87e3c', '2026-10-01 18:47:02', 'info', 'Launch', 'Success', 'user', '2', 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'Launch', NULL, NULL, NULL, 'The new PIN has been set, so the application is starting again on it and this launch is ending.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30580\",\"threadId\":\"8\",\"uptimeSeconds\":\"15\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '9940b3821748b662df6a200cdbde14d0de5cdbf7ef2490a0a55f1bb7e879592a', 'ee910ae4a7dcd2ee6d177c82b0cb282697eba9e8b6ebc889dd72fed1c95d07e2'),
(32, '90fcd755-2496-4f62-9644-6b23fc4a825f', 'e98a323e-239c-4121-adf6-ef73df95dc3d', '2026-10-01 18:47:02', 'info', 'Launch', 'Success', 'user', '2', 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'Launch', NULL, NULL, NULL, 'The launch is ending: The new PIN has been set, so the application is starting again on it.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30580\",\"threadId\":\"8\",\"uptimeSeconds\":\"15\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'ee910ae4a7dcd2ee6d177c82b0cb282697eba9e8b6ebc889dd72fed1c95d07e2', '4e1a9383a07e992417d4df12b6b6949eb1457e0f7508b47272be2dff7a96c02e'),
(33, '5b46468d-aeea-42f3-8f51-00d5fb51c6c8', '8474d405-0a27-4bca-90a1-3d7ed8a718ff', '2026-10-01 18:47:02', 'info', 'App', 'Success', 'user', '2', 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'App', NULL, NULL, NULL, 'The process is ending: the lines already raised, including the reason, are being written to the store first.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30580\",\"threadId\":\"8\",\"uptimeSeconds\":\"15\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '4e1a9383a07e992417d4df12b6b6949eb1457e0f7508b47272be2dff7a96c02e', '1acd8c1f7f070e9cb041bcfdb7de509527c856168c741b18d95c2a9c38d505f2'),
(34, '02d07d6c-1b5f-4b60-8f40-a583c08b8f72', '6cf6ea25-f040-454d-9b6a-99e7a4f5aa7d', '2026-10-01 18:47:03', 'info', 'App', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'App', NULL, NULL, NULL, 'Host built.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30200\",\"threadId\":\"2\",\"uptimeSeconds\":\"0\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '1acd8c1f7f070e9cb041bcfdb7de509527c856168c741b18d95c2a9c38d505f2', '2eb4c7eb69253c6ac5ae46ca535f2d2a33be67d7333885a24728c25cbfd8c895'),
(35, '28925234-9237-4d32-8a65-b8554ed0152e', 'e094dad3-9f12-45de-b269-e208b3666d89', '2026-10-01 18:47:03', 'info', 'App', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'App', NULL, NULL, NULL, 'Visual reachability probe host started.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30200\",\"threadId\":\"2\",\"uptimeSeconds\":\"0\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '2eb4c7eb69253c6ac5ae46ca535f2d2a33be67d7333885a24728c25cbfd8c895', '4dad008c1b67c5313fef9a88983f254018e968e7a29939f9c2b738a03751f0f8'),
(36, 'cfe85f0b-a7ed-4de5-b5c6-f48463b1a556', 'b3b6c790-8e92-4871-95cf-2167fae00503', '2026-10-01 18:47:03', 'info', 'App', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'App', NULL, NULL, NULL, 'App notification service initialized.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30200\",\"threadId\":\"2\",\"uptimeSeconds\":\"0\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '4dad008c1b67c5313fef9a88983f254018e968e7a29939f9c2b738a03751f0f8', 'd6aee69db6f053372d47dd791957e6d799dbb7627d4566e474c430ffdc91adfc'),
(37, 'f7bfb0a7-9ba2-4228-b611-ae76bb550bc0', '461d99ca-623e-4ad8-b22c-75abf94089b4', '2026-10-01 18:47:03', 'info', 'App', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'App', NULL, NULL, NULL, 'UnhandledException handler registered.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30200\",\"threadId\":\"2\",\"uptimeSeconds\":\"0\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'd6aee69db6f053372d47dd791957e6d799dbb7627d4566e474c430ffdc91adfc', 'a0c585a7dcb4b95ee78c7120853032ad29949eb894190297cf73f0e58f7cd755'),
(38, '31de7c45-4b78-4300-adf5-48512f0da686', '8d1b91fb-3adb-4680-ac26-88e7e3beea38', '2026-10-01 18:47:03', 'info', 'App', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'App', NULL, NULL, NULL, 'Background-thread fault handler registered.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30200\",\"threadId\":\"2\",\"uptimeSeconds\":\"0\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'a0c585a7dcb4b95ee78c7120853032ad29949eb894190297cf73f0e58f7cd755', '493a5ec9a908ec17a7725b5615b0b5de92afffa4a91dffd3f943713bfd915076'),
(39, 'd25a6332-0732-4112-b0bf-9640ad93cb93', 'b102c1ba-d589-4205-a1a6-0c7d14818830', '2026-10-01 18:47:03', 'info', 'Launch', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'Launch', NULL, NULL, NULL, 'OnLaunched started.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30200\",\"threadId\":\"2\",\"uptimeSeconds\":\"0\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '493a5ec9a908ec17a7725b5615b0b5de92afffa4a91dffd3f943713bfd915076', '45081a6ea964f36e3ff4a6dc7acf4d91d30dbfb575f0ef2cc0bec8935bdab665'),
(40, 'a9dbd7cc-bd9a-4d9f-a443-463f8362fda2', '5a6d2b3b-451c-4718-afa7-61c0c04b7d1d', '2026-10-01 18:47:03', 'info', 'Microsoft.Hosting.Lifetime', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'Microsoft.Hosting.Lifetime', NULL, NULL, NULL, 'Application started. Press Ctrl+C to shut down.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30200\",\"threadId\":\"2\",\"uptimeSeconds\":\"0\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null},\"properties\":{\"{OriginalFormat}\":\"Application started. Press Ctrl\\u002BC to shut down.\"}}', '45081a6ea964f36e3ff4a6dc7acf4d91d30dbfb575f0ef2cc0bec8935bdab665', '8f33267d5f3032e06df5c782f0a0dd6c1f0be28dacc8d4c4acb4e640adefb241'),
(41, '6d0e33ec-c216-4762-9549-050696bad734', '35123086-10c0-40a4-9c98-b4146ca07a8b', '2026-10-01 18:47:03', 'info', 'Microsoft.Hosting.Lifetime', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'Microsoft.Hosting.Lifetime', NULL, NULL, NULL, 'Hosting environment: Production', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30200\",\"threadId\":\"2\",\"uptimeSeconds\":\"0\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null},\"properties\":{\"EnvName\":\"Production\",\"{OriginalFormat}\":\"Hosting environment: {EnvName}\"}}', '8f33267d5f3032e06df5c782f0a0dd6c1f0be28dacc8d4c4acb4e640adefb241', 'b2026947edb4221201b02237b640d36453b9581f4019d1fc0fa83811cc22be49'),
(42, '9b73938d-f5f7-41c2-a889-fea558488fec', '71a83711-e7e2-42c3-b7ba-cd8770910b3d', '2026-10-01 18:47:03', 'info', 'Microsoft.Hosting.Lifetime', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'Microsoft.Hosting.Lifetime', NULL, NULL, NULL, 'Content root path: C:\\Users\\johnk\\source\\repos\\MTM_Waitlist\\bin\\x64\\Debug\\net10.0-windows10.0.19041.0\\win-x64\\', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30200\",\"threadId\":\"2\",\"uptimeSeconds\":\"0\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null},\"properties\":{\"ContentRoot\":\"C:\\\\Users\\\\johnk\\\\source\\\\repos\\\\MTM_Waitlist\\\\bin\\\\x64\\\\Debug\\\\net10.0-windows10.0.19041.0\\\\win-x64\\\\\",\"{OriginalFormat}\":\"Content root path: {ContentRoot}\"}}', 'b2026947edb4221201b02237b640d36453b9581f4019d1fc0fa83811cc22be49', '75d6fbda5134a8fd75686728a04f263e01aef99c7da7e75f8d3769b1804be43b'),
(43, 'a0e36b39-1090-4161-813b-165e267b06bf', '3a7c6865-a830-49b0-8022-9b4c6181b743', '2026-10-01 18:47:03', 'info', 'Launch', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'Launch', NULL, NULL, NULL, 'Host started.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30200\",\"threadId\":\"2\",\"uptimeSeconds\":\"0\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '75d6fbda5134a8fd75686728a04f263e01aef99c7da7e75f8d3769b1804be43b', 'd2e4e71ffbed4e8394ae687f882052a5d02585b993b7607b5bd442db0d5ee2b4'),
(44, '10566347-62d0-43f0-b2ed-7c7f37d78f67', '1171f88b-abf6-463f-a34c-a7f022eab71f', '2026-10-01 18:47:04', 'info', 'MySqlHelperServer', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'MySqlHelperServer', NULL, NULL, NULL, 'ExecuteStoredProcedureQueryAsync started. Procedure=\'sp_core_computers_registry_get_all\', Target=\'mtm_waitlist\', Attempt=1.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30200\",\"threadId\":\"2\",\"uptimeSeconds\":\"1\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'd2e4e71ffbed4e8394ae687f882052a5d02585b993b7607b5bd442db0d5ee2b4', '24ccf20db599f6b4654381b0d298fd447fdc04a2285e91183ba8a98a56a04ec0'),
(45, '97c29838-d338-46c9-a14b-39f1e5dc9e0e', '6f3c1c70-0838-45e5-8df0-4371110727da', '2026-10-01 18:47:04', 'info', 'Launch', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'Launch', NULL, NULL, NULL, 'The launch was started.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30200\",\"threadId\":\"2\",\"uptimeSeconds\":\"1\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '24ccf20db599f6b4654381b0d298fd447fdc04a2285e91183ba8a98a56a04ec0', 'eecc6010b8114c0d43701b9eac3ed73beff35a7e7f194f4061e2a06317ec6856'),
(46, '037535c8-be43-4fe9-b142-1b37df29f35c', '7d79ed41-804b-418a-aa61-dae200ef89f2', '2026-10-01 18:47:04', 'info', 'MySqlHelperServer', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'MySqlHelperServer', NULL, NULL, NULL, 'ExecuteStoredProcedureQueryAsync started. Procedure=\'sp_core_computers_registry_lookup_by_name_get\', Target=\'mtm_waitlist\', Attempt=1.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30200\",\"threadId\":\"15\",\"uptimeSeconds\":\"1\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'eecc6010b8114c0d43701b9eac3ed73beff35a7e7f194f4061e2a06317ec6856', '9967e0433cd3197b5df226c7107479e83ee7e0474a09bd5864296cca67335252'),
(47, 'c8ecdcab-e893-4d41-99cf-724d10fe7a81', 'fce8383c-09ba-467b-bcdc-b2002bee461d', '2026-10-01 18:47:04', 'info', 'MySqlHelperServer', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'MySqlHelperServer', NULL, NULL, NULL, 'ExecuteStoredProcedureQueryAsync started. Procedure=\'sp_core_computers_registry_lookup_by_name_get\', Target=\'mtm_waitlist\', Attempt=1.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30200\",\"threadId\":\"15\",\"uptimeSeconds\":\"1\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '9967e0433cd3197b5df226c7107479e83ee7e0474a09bd5864296cca67335252', 'ad853dbe89c4639fc30d8fab3797bba5c4d80d7324b13a74e616db4cc64aa964'),
(48, 'e89f4beb-00b8-4f09-8d03-b929e8e64e02', '5928dbd7-0ab6-45b7-833e-03cbca0f1217', '2026-10-01 18:47:04', 'info', 'MySqlHelperServer', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'MySqlHelperServer', NULL, NULL, NULL, 'ExecuteStoredProcedureQueryAsync started. Procedure=\'sp_core_computers_registry_lookup_by_name_get\', Target=\'mtm_waitlist\', Attempt=1.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30200\",\"threadId\":\"15\",\"uptimeSeconds\":\"1\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'ad853dbe89c4639fc30d8fab3797bba5c4d80d7324b13a74e616db4cc64aa964', '34e177da2300f91dad74a1f6aac2143373118f7e76eb2b9f5b41fce316b01225'),
(49, 'dd0a9b09-8c53-422b-809f-cf0e4f3e7ffa', '04aac7ee-9516-4d4e-876c-40ce2072cfe3', '2026-10-01 18:47:04', 'info', 'MySqlHelperServer', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'MySqlHelperServer', NULL, NULL, NULL, 'ExecuteStoredProcedureQueryAsync started. Procedure=\'sp_core_computers_registry_lookup_by_name_get\', Target=\'mtm_waitlist\', Attempt=1.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30200\",\"threadId\":\"15\",\"uptimeSeconds\":\"1\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '34e177da2300f91dad74a1f6aac2143373118f7e76eb2b9f5b41fce316b01225', 'fb017e8205559ada63784e42e45022ed7fd8239dd248652e59dd272b216158cb'),
(50, 'd8e3147f-30ea-43e9-bb20-3da81a368934', 'b4b20157-5941-46d2-b095-4328f8eba76c', '2026-10-01 18:47:04', 'info', 'Launch', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'Launch', NULL, NULL, NULL, 'The launch ended at SignIn.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30200\",\"threadId\":\"15\",\"uptimeSeconds\":\"1\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'fb017e8205559ada63784e42e45022ed7fd8239dd248652e59dd272b216158cb', 'c76a7228ff2e1bebd262b3238ba43268afff278522f162fdec517b68bf422f08'),
(51, '62ec67a2-f363-427d-9766-3e8ab19acd9f', '1536e00e-ff66-43ba-a97c-418d721036b8', '2026-10-01 18:47:04', 'info', 'VisualProbe', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'VisualProbe', NULL, NULL, NULL, 'Infor Visual did not answer within 2014 ms. TaskCanceledException: A task was canceled.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30200\",\"threadId\":\"15\",\"uptimeSeconds\":\"2\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'c76a7228ff2e1bebd262b3238ba43268afff278522f162fdec517b68bf422f08', 'f3fee50c43c8a691b837df44e83898dcab2d0451209f4758649c5b07252f82d5'),
(52, '514e4068-65a0-495f-a91d-538a0506e3f1', 'bb45585d-f454-4265-89f2-593c5097f03b', '2026-10-01 18:47:05', 'info', 'MySqlHelperServer', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'MySqlHelperServer', NULL, NULL, NULL, 'ExecuteStoredProcedureQueryAsync started. Procedure=\'sp_visual_read_shape_freshness_get\', Target=\'mtm_mock\', Attempt=1.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30200\",\"threadId\":\"15\",\"uptimeSeconds\":\"3\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'f3fee50c43c8a691b837df44e83898dcab2d0451209f4758649c5b07252f82d5', 'b5e96577e82587d552785ac48dbe84155de28724d617a3f1c4920f165da50bd9'),
(53, '4ef53096-a771-4d29-821c-05670207ef4c', '58065a99-6d27-4068-bc90-cd867ce4a111', '2026-10-01 18:47:05', 'info', 'StoreAvailabilityTracker', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'StoreAvailabilityTracker', NULL, NULL, NULL, 'Store \'mtm_mock\' is now Available.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30200\",\"threadId\":\"11\",\"uptimeSeconds\":\"3\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'b5e96577e82587d552785ac48dbe84155de28724d617a3f1c4920f165da50bd9', '90c06f58e7d08c6d1ca37f171cc59ec700064625b9368a57aee3e8207d5e35b3'),
(54, '9b145fdb-f394-4bd4-be53-ab0465629cc9', '69148573-537e-41b2-abe7-534a05d08762', '2026-10-01 18:47:09', 'info', 'MySqlHelperServer', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'MySqlHelperServer', NULL, NULL, NULL, 'ExecuteStoredProcedureQueryAsync started. Procedure=\'sp_auth_user_credential_get\', Target=\'mtm_waitlist\', Attempt=1.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30200\",\"threadId\":\"2\",\"uptimeSeconds\":\"6\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '90c06f58e7d08c6d1ca37f171cc59ec700064625b9368a57aee3e8207d5e35b3', '34ba3f0494d663edbcd2c8e8f604b174b3d040e614ada888315250a61e248216'),
(55, '8c305aa3-3c57-4802-b409-5ec2f38823a6', '14950f1c-a04e-45eb-8d5e-626aa77b55dc', '2026-10-01 18:47:09', 'info', 'MySqlHelperServer', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'MySqlHelperServer', NULL, NULL, NULL, 'ExecuteStoredProcedureNonQueryAsync started. Procedure=\'sp_auth_remembered_sign_ins_clear\', Target=\'mtm_waitlist\', Attempt=1.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30200\",\"threadId\":\"2\",\"uptimeSeconds\":\"6\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '34ba3f0494d663edbcd2c8e8f604b174b3d040e614ada888315250a61e248216', '9f4fe2c20d259550951529b41a224d64bb705e1a0b10d4b4455f6d9145b4ed31'),
(56, 'f89cbc2f-3370-4786-826e-37595c7fd865', '76da5615-71fb-4d5a-915c-3f302a5f6e4a', '2026-10-01 18:47:09', 'info', 'MySqlHelperServer', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'MySqlHelperServer', NULL, NULL, NULL, 'ExecuteStoredProcedureQueryAsync started. Procedure=\'sp_auth_user_row_get\', Target=\'mtm_waitlist\', Attempt=1.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30200\",\"threadId\":\"2\",\"uptimeSeconds\":\"6\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '9f4fe2c20d259550951529b41a224d64bb705e1a0b10d4b4455f6d9145b4ed31', '335b30f7b54d1bf2edfbb5cc91c305b08a59d22499f17139c1e8fc5ae3963ef8'),
(57, 'e62cdc5a-ced5-45b8-b1ba-74e3517dbbaa', '10d3ecb3-072b-4b6e-87b6-317bbd1df7cd', '2026-10-01 18:47:09', 'info', 'MySqlHelperServer', 'Success', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'MySqlHelperServer', NULL, NULL, NULL, 'ExecuteStoredProcedureQueryAsync started. Procedure=\'sp_auth_user_roles_get\', Target=\'mtm_waitlist\', Attempt=1.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30200\",\"threadId\":\"10\",\"uptimeSeconds\":\"6\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '335b30f7b54d1bf2edfbb5cc91c305b08a59d22499f17139c1e8fc5ae3963ef8', '99e0125268b4c3c623acea39c55358460d46024d348ae2e0fbe34b22ed851063'),
(58, '6a1d2dec-028f-4348-8e80-d845235c012e', 'deafd156-5e72-4b4b-ad2b-a3525fae6e7d', '2026-10-01 18:47:09', 'info', 'MySqlHelperServer', 'Success', 'user', '2', 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'MySqlHelperServer', NULL, NULL, NULL, 'ExecuteStoredProcedureQueryAsync started. Procedure=\'sp_config_settings_values_get\', Target=\'mtm_waitlist\', Attempt=1.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30200\",\"threadId\":\"10\",\"uptimeSeconds\":\"6\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '99e0125268b4c3c623acea39c55358460d46024d348ae2e0fbe34b22ed851063', '83e013f4cdfa2c355fd207fd5899ac2d79aa00dde14c0a6353ac01b8349b9611'),
(59, 'e5e267c9-5608-417d-80cf-4c22f094733c', '55a8ecaa-4f65-483f-9ca1-2877c5c1d3b2', '2026-10-01 18:47:09', 'info', 'MySqlHelperServer', 'Success', 'user', '2', 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'MySqlHelperServer', NULL, NULL, NULL, 'ExecuteStoredProcedureNonQueryAsync started. Procedure=\'sp_auth_user_active_sessions_upsert\', Target=\'mtm_waitlist\', Attempt=1.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30200\",\"threadId\":\"10\",\"uptimeSeconds\":\"6\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '83e013f4cdfa2c355fd207fd5899ac2d79aa00dde14c0a6353ac01b8349b9611', '59caa98dbb6da00c7c7b9a20c42b79d19b0b927198bf33dd45dd4407c138a9e2'),
(60, '3a26183e-b749-4d22-bd24-3474081f8c96', 'b60f4848-9315-4730-ae93-4ec440968c59', '2026-10-01 18:47:09', 'info', 'MySqlHelperServer', 'Success', 'user', '2', 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'MySqlHelperServer', NULL, NULL, NULL, 'ExecuteStoredProcedureQueryAsync started. Procedure=\'sp_auth_user_active_sessions_get\', Target=\'mtm_waitlist\', Attempt=1.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30200\",\"threadId\":\"10\",\"uptimeSeconds\":\"6\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '59caa98dbb6da00c7c7b9a20c42b79d19b0b927198bf33dd45dd4407c138a9e2', '50d79a1bbcbfa22e9fb30f3ff6faa94e219f92e7e488109d0f51bc67c89431bf'),
(61, '961680f2-5ba4-4444-8038-cb1f18b09724', 'd51c4fc5-17aa-4db9-b7b8-306dd55d5888', '2026-10-01 18:47:09', 'info', 'MySqlHelperServer', 'Success', 'user', '2', 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'MySqlHelperServer', NULL, NULL, NULL, 'ExecuteStoredProcedureQueryAsync started. Procedure=\'sp_core_computers_registry_lookup_by_name_get\', Target=\'mtm_waitlist\', Attempt=1.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30200\",\"threadId\":\"11\",\"uptimeSeconds\":\"6\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '50d79a1bbcbfa22e9fb30f3ff6faa94e219f92e7e488109d0f51bc67c89431bf', 'c5bf899b05e4a2a79f0efd1bfcb5df29229effb98cea7e66a1909466a03a5d56'),
(62, '9528c851-0d79-49e4-bddf-57c2fd2b2dea', 'f008078e-a7df-4206-a6bc-96ed48c10503', '2026-10-01 18:47:09', 'info', 'MySqlHelperServer', 'Success', 'user', '2', 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'MySqlHelperServer', NULL, NULL, NULL, 'ExecuteStoredProcedureQueryAsync started. Procedure=\'sp_config_settings_values_get\', Target=\'mtm_waitlist\', Attempt=1.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.0.0.0\",\"informationalVersion\":\"1.0.0\\u002Bed0c04f47785e3b502cbcfb8cafbe50fbf772fc3\",\"osDescription\":\"Microsoft Windows 10.0.26200\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30200\",\"threadId\":\"15\",\"uptimeSeconds\":\"6\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'c5bf899b05e4a2a79f0efd1bfcb5df29229effb98cea7e66a1909466a03a5d56', '33022ea6b212296af557a0adb06c1b6a2c1a27b96011ce255a2d75f88cfcc3cc'),
(63, '6a4542c0-95cd-4133-8c80-bb3bdc076ea2', '762e5dd7-19aa-487f-9397-78d04e066c2e', '2026-10-10 22:55:44', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002B3a71b35bf0db5942848c5b76cec3bc60ef03c796\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"8924\",\"threadId\":\"11\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '33022ea6b212296af557a0adb06c1b6a2c1a27b96011ce255a2d75f88cfcc3cc', '9efd46ca6303fc18f87d74aa83fb139e0edced03219166320a5e1622818fe34a'),
(64, '1717d935-1874-46e0-acf5-b451d1bc40b4', 'c5cf15b9-7785-4819-98e7-9545f982cb05', '2026-10-11 06:13:13', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"29912\",\"threadId\":\"8\",\"uptimeSeconds\":\"5\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '9efd46ca6303fc18f87d74aa83fb139e0edced03219166320a5e1622818fe34a', 'a8bebfbab8e5d3c26cfebb2726bf3bb9e4c69cd7ceaa249cdcbbab04ac5a4a97'),
(65, '81d3ccc1-9d5f-4345-8f05-6164b852db61', 'd2389b1b-d546-4280-8830-c85cc5493d8c', '2026-10-11 06:15:00', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"36284\",\"threadId\":\"18\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'a8bebfbab8e5d3c26cfebb2726bf3bb9e4c69cd7ceaa249cdcbbab04ac5a4a97', '06d94eb5907430e80caad3f6f7d519d259df9e5f4ac420203b9b6981432ec319'),
(66, 'b52fd017-33d4-4f7e-a309-f3a6641e8707', '2fc8834e-ccc6-4c5f-8cd0-c71b1fa27de0', '2026-10-11 14:50:44', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"31268\",\"threadId\":\"10\",\"uptimeSeconds\":\"5\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '06d94eb5907430e80caad3f6f7d519d259df9e5f4ac420203b9b6981432ec319', 'a41804afac1d20e4e2dba4c85e28d5e3ff6455d5d897c0064a54e06b01e8ebc6'),
(67, '44ed8434-593b-402b-8c2d-865d0016f0a2', 'ae64bee2-8eb8-4b97-a9b8-24b5fe502cc1', '2026-10-11 14:53:31', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"33156\",\"threadId\":\"10\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'a41804afac1d20e4e2dba4c85e28d5e3ff6455d5d897c0064a54e06b01e8ebc6', 'b1a2d4dbae15545faee5f8592887fcbbada95662d00a9d05655204f37c8be50a'),
(68, '22e8d250-f267-44c1-8622-a2830e75c1a0', '0b30480e-b9b8-44de-886a-52853596a1c3', '2026-10-11 14:57:53', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"17604\",\"threadId\":\"10\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'b1a2d4dbae15545faee5f8592887fcbbada95662d00a9d05655204f37c8be50a', '68805ceb942d0de7dcd57f1bacdd4cde3f04b26857190e697fcdd16f1ee92592'),
(69, '9dabf53e-f4e0-41d2-97a6-02b558a5e4fc', 'b8cc333e-a796-4c61-8b81-87ce77a19dda', '2026-10-11 15:00:38', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"35040\",\"threadId\":\"8\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '68805ceb942d0de7dcd57f1bacdd4cde3f04b26857190e697fcdd16f1ee92592', '4f55458d47e11ce28854d8ecebf468e2647b95a330f699883ca31ab8f2a8bd2c'),
(70, 'ab5fe2a3-e5bf-44ad-9045-c472b249e1d8', 'ead5b4fd-e10b-4fd6-84ac-bf1c3465fd00', '2026-10-11 15:08:21', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"31684\",\"threadId\":\"14\",\"uptimeSeconds\":\"6\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '4f55458d47e11ce28854d8ecebf468e2647b95a330f699883ca31ab8f2a8bd2c', 'b350f433bf8716c19b0ea0458774d4c54a6f19e1dc3e0c374b6661d699dcd6ea'),
(71, '90f78cbf-c4f2-4c95-b973-680fcbbfa6f2', 'b22938eb-05a5-484d-91c5-b3e91e4e907a', '2026-10-11 15:30:24', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"34880\",\"threadId\":\"10\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'b350f433bf8716c19b0ea0458774d4c54a6f19e1dc3e0c374b6661d699dcd6ea', 'ad6bae2f276d51ad064c325aae6aa9f7ed393e7b83a1529798d276d9b2e32229'),
(72, '5d1ccfcf-9a70-4588-ac89-74012fe55514', '4be4797c-d9df-4c84-a6e3-773c1c9b4064', '2026-10-11 15:32:20', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"31072\",\"threadId\":\"8\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'ad6bae2f276d51ad064c325aae6aa9f7ed393e7b83a1529798d276d9b2e32229', '593c7cdc58f91a7e506553a83ea24565d0578bf3da62c327e26568947d07e698'),
(73, '4c7c3ffb-82e6-4054-970c-0eab27cda8e8', '878508a8-c58f-4f8d-84dd-cfda100d0e45', '2026-10-11 15:34:51', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"29920\",\"threadId\":\"14\",\"uptimeSeconds\":\"5\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '593c7cdc58f91a7e506553a83ea24565d0578bf3da62c327e26568947d07e698', 'dda6b2474e20770dafc38381cc0fe23c7451572accd982b12e49d091ce852fc7'),
(74, '6ec71659-01c0-48e0-b4be-baa3d7aaf634', 'c47503ea-083f-492d-be38-83675c4475d0', '2026-10-11 16:20:49', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"17656\",\"threadId\":\"15\",\"uptimeSeconds\":\"5\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'dda6b2474e20770dafc38381cc0fe23c7451572accd982b12e49d091ce852fc7', 'b463dffe39a20d26ea808e6583b097a44cfd6ad6f00d9a3a0c0db76057f4431d'),
(75, '256642e5-78b7-4cf3-8a6d-d3a294f72941', '43f3a55a-0890-4528-b5a9-1144134320b2', '2026-10-11 16:22:09', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"4636\",\"threadId\":\"15\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'b463dffe39a20d26ea808e6583b097a44cfd6ad6f00d9a3a0c0db76057f4431d', '80752763d65731cf7164385662534038b76aafb48d6a95db00c8ef587bc3cf5d'),
(76, 'ef2067f4-b682-403d-9c9d-26a467d381af', 'a008241e-a3f5-4e02-a879-0822c6417450', '2026-10-11 16:26:02', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"29936\",\"threadId\":\"8\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '80752763d65731cf7164385662534038b76aafb48d6a95db00c8ef587bc3cf5d', '4d92a2fbcc3f0ec862d7f06ce9074642c208ff5f4f6ee5962f2172d660fc0d2c'),
(77, 'c26f4f05-617a-4ab5-a396-430589141cd3', 'de3e9fff-03f1-478a-95c1-a557ce418b11', '2026-10-11 16:26:39', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"26516\",\"threadId\":\"8\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '4d92a2fbcc3f0ec862d7f06ce9074642c208ff5f4f6ee5962f2172d660fc0d2c', '9d2292e160263cc10dbf03ee78b227e8d5a58a5bd0dfaecfeb5793eefa0591c0'),
(78, 'cddb3465-eaa0-490c-a8e9-e4105fedb092', 'cb23d9e1-f552-4a9f-8b8d-7c6fcc00506d', '2026-10-11 16:34:50', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"14636\",\"threadId\":\"10\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '9d2292e160263cc10dbf03ee78b227e8d5a58a5bd0dfaecfeb5793eefa0591c0', 'd37d96ab24e2a643f14c5499e9f984f64fdd2eba4320ce3822d9866e96d0a900'),
(79, 'af28d535-9704-4df4-820f-5cb9a212b307', '17d4ec13-0a18-41d7-9278-80e944c94401', '2026-10-11 23:37:22', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"26628\",\"threadId\":\"15\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'd37d96ab24e2a643f14c5499e9f984f64fdd2eba4320ce3822d9866e96d0a900', '30cd3f84f2299c10ec3670cbadc14fd3b93b6722c62133c62f22e8ee1451bae5'),
(80, '0b8575cf-6060-40c6-961d-79aa5a9071dd', '6cc0778d-48d3-49c8-8866-b5aeab899e6e', '2026-10-11 23:44:19', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"29528\",\"threadId\":\"8\",\"uptimeSeconds\":\"5\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '30cd3f84f2299c10ec3670cbadc14fd3b93b6722c62133c62f22e8ee1451bae5', '4f13b2ea03c8b132451387feaef9dab4c21eb2e5ff909d3074efa11b58ce6852'),
(81, 'cff9ea63-1e8e-4720-9da5-8deea2efe394', '8b10fe55-6809-443e-a66b-93a890ef2377', '2026-10-11 23:50:33', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"5144\",\"threadId\":\"10\",\"uptimeSeconds\":\"5\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '4f13b2ea03c8b132451387feaef9dab4c21eb2e5ff909d3074efa11b58ce6852', 'e115630d201c79a95ebac48ab329f9204966366342e742b14d331532a4894057'),
(82, '851f8e4e-9531-4c0a-a687-7d6437481169', 'd053f8af-b789-4af5-b9bd-e0bc46cc0037', '2026-10-11 23:54:28', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"32056\",\"threadId\":\"10\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'e115630d201c79a95ebac48ab329f9204966366342e742b14d331532a4894057', '9337fad5bce8458f1e0dd6f956174620a0b3f6bc2503d9039f8a31c9314cd411'),
(83, '089f7f00-74fc-4013-b425-bc837d68db12', '6153c57e-1160-4227-a6ec-5f6a2fe8c026', '2026-10-11 23:59:04', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"22812\",\"threadId\":\"15\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '9337fad5bce8458f1e0dd6f956174620a0b3f6bc2503d9039f8a31c9314cd411', '1cc8bf65bd8599390418ac9d889df92e79b5918d2060b7dbbdc021c05085e1d5'),
(84, '7b339b0d-294c-4f77-b8a3-208baf3c2901', '440f3285-001a-4a2c-8bac-61d7b8520a56', '2026-10-12 00:04:04', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"2520\",\"threadId\":\"11\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '1cc8bf65bd8599390418ac9d889df92e79b5918d2060b7dbbdc021c05085e1d5', 'b039625b0791997b6a6babeab0c86e482ef194aabb6ba86cd311ea87456da0de'),
(85, 'd91bd311-5272-4efd-b585-4aeb97bc445d', '735cb69b-083f-40cf-8f6e-eb105d2729a2', '2026-10-12 00:04:18', 'warning', 'WaitlistRebuild', 'Blocked', 'user', '2', 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'WaitlistRebuild', NULL, NULL, NULL, 'WaitlistRebuild reuse=False list=16ms rows=3 declarations=7ms wcImages=38ms wcs=25 rows=1050ms handlerState=16ms sourceUpdate=1ms', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"2520\",\"threadId\":\"20\",\"uptimeSeconds\":\"18\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'b039625b0791997b6a6babeab0c86e482ef194aabb6ba86cd311ea87456da0de', '9af5f53a06d99af655504e6b0eb013863b8e1aeb84b67311fb05c3b7cc39183a'),
(86, '94f6e33e-1d4e-43fe-93e9-526ca0f15269', '72ab3e77-c3ff-41a9-91e8-040464f2de6c', '2026-10-12 00:04:29', 'warning', 'WaitlistRebuild', 'Blocked', 'user', '2', 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'WaitlistRebuild', NULL, NULL, NULL, 'WaitlistRebuild reuse=True list=1ms rows=3 declarations=2ms wcImages=17ms wcs=25 rows=0ms handlerState=0ms sourceUpdate=1ms', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"2520\",\"threadId\":\"10\",\"uptimeSeconds\":\"29\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '9af5f53a06d99af655504e6b0eb013863b8e1aeb84b67311fb05c3b7cc39183a', '250d695da1564025338547f8402bd1438b82abceb8ee09611d6d3795be4fc20f'),
(87, '08453bc9-8f74-4897-b53f-8bbfe4856538', '801531c3-a0d0-4ed9-bac0-dd749422a535', '2026-10-12 00:05:22', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"11504\",\"threadId\":\"10\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '250d695da1564025338547f8402bd1438b82abceb8ee09611d6d3795be4fc20f', 'ad14e9ed592c4b7d3ff12baa950c6d100341963513ff7ee7c1d0f12c22a46956'),
(88, '5be68e5c-4ff1-4823-aaf0-8b66abf01474', '80f4e13d-8303-4bcc-badd-b450f220504d', '2026-10-12 00:05:36', 'warning', 'WaitlistRebuild', 'Blocked', 'user', '2', 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'WaitlistRebuild', NULL, NULL, NULL, 'WaitlistRebuild reuse=False list=18ms rows=3 declarations=6ms wcImages=32ms wcs=25 rows=1051ms job=1037ms urgency=0ms images=5ms handlerState=13ms sourceUpdate=1ms', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"11504\",\"threadId\":\"10\",\"uptimeSeconds\":\"18\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'ad14e9ed592c4b7d3ff12baa950c6d100341963513ff7ee7c1d0f12c22a46956', '65aa3846a20bb9f6c90ac54e2e6259c07157302d8b29b9b3a24bf624536f9658'),
(89, '0530d066-504e-468e-8da2-5bccf692b010', 'd60a3618-e739-4ab5-b69e-bd49c63161a5', '2026-10-12 00:05:43', 'warning', 'WaitlistRebuild', 'Blocked', 'user', '2', 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'WaitlistRebuild', NULL, NULL, NULL, 'WaitlistRebuild reuse=False list=2ms rows=3 declarations=3ms wcImages=29ms wcs=25 rows=5ms job=4ms urgency=0ms images=0ms handlerState=1ms sourceUpdate=0ms', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"11504\",\"threadId\":\"11\",\"uptimeSeconds\":\"25\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '65aa3846a20bb9f6c90ac54e2e6259c07157302d8b29b9b3a24bf624536f9658', '1d0d590068592722020a027d658491e43e6cb6dfa3bd3b83e307d75b1b67b3c3'),
(90, 'f36ba2ee-bebc-4e4d-a050-cc6e56810f1a', '26700bb4-ad5d-4462-83d6-d6760494be71', '2026-10-12 00:05:57', 'warning', 'WaitlistRebuild', 'Blocked', 'user', '2', 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'WaitlistRebuild', NULL, NULL, NULL, 'WaitlistRebuild reuse=False list=7ms rows=3 declarations=1ms wcImages=19ms wcs=25 rows=3718ms job=1020ms urgency=0ms images=2698ms handlerState=1ms sourceUpdate=0ms', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"11504\",\"threadId\":\"8\",\"uptimeSeconds\":\"39\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '1d0d590068592722020a027d658491e43e6cb6dfa3bd3b83e307d75b1b67b3c3', 'd1764e66031b20a7f38e7eebeb4770446139425a992509e757ce106546865bf3'),
(91, 'e78ff542-9619-4a0f-8075-e16114d3b580', '919dff0c-20bf-4359-9825-f8d428a35889', '2026-10-12 00:07:36', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"6736\",\"threadId\":\"8\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'd1764e66031b20a7f38e7eebeb4770446139425a992509e757ce106546865bf3', 'd3ca07a7badfd10bdf3a96e72b207c8529f6c67d73f42c733b60b022caf42d29'),
(92, '56881bd6-a780-4827-a88e-e83eac31b8fd', 'bd20a55d-f213-4499-9094-ca690cd5e192', '2026-10-12 00:07:50', 'warning', 'WaitlistRebuild', 'Blocked', 'user', '2', 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'WaitlistRebuild', NULL, NULL, NULL, 'WaitlistRebuild reuse=False list=16ms rows=3 declarations=7ms wcImages=29ms wcs=25 rows=1060ms job=1045ms urgency=0ms images=5ms handlerState=16ms sourceUpdate=1ms', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"6736\",\"threadId\":\"15\",\"uptimeSeconds\":\"18\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'd3ca07a7badfd10bdf3a96e72b207c8529f6c67d73f42c733b60b022caf42d29', '3733729edf88cc102fa4682ecb006ac63bcfb3f4ba155b03410e9f2921d56654'),
(93, '8f2eb6d5-90b0-4b9d-98b8-ca09215b867e', 'a1f9cccb-f5df-4b75-be6c-53c0f3ee9226', '2026-10-12 00:07:58', 'warning', 'WaitlistRebuild', 'Blocked', 'user', '2', 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'WaitlistRebuild', NULL, NULL, NULL, 'WaitlistRebuild reuse=False list=3ms rows=3 declarations=3ms wcImages=19ms wcs=25 rows=3ms job=2ms urgency=0ms images=0ms handlerState=1ms sourceUpdate=1ms', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"6736\",\"threadId\":\"15\",\"uptimeSeconds\":\"26\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '3733729edf88cc102fa4682ecb006ac63bcfb3f4ba155b03410e9f2921d56654', '7087b0281cb13f5a4631aaee81bfc22cdf6eea282cfed38d6664ff337be785a4'),
(94, 'eba0f346-42d0-4fb2-8f6d-4d6981544fec', '1c319cff-299b-4361-84f3-6678fbcd69e4', '2026-10-12 00:08:08', 'warning', 'WaitlistRebuild', 'Blocked', 'user', '2', 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'WaitlistRebuild', NULL, NULL, NULL, 'WaitlistRebuild reuse=False list=2ms rows=3 declarations=1ms wcImages=24ms wcs=25 rows=9ms job=7ms urgency=0ms images=2ms handlerState=1ms sourceUpdate=0ms', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"6736\",\"threadId\":\"11\",\"uptimeSeconds\":\"36\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '7087b0281cb13f5a4631aaee81bfc22cdf6eea282cfed38d6664ff337be785a4', '2cf2ea45be543189de2f9eceb54d496c03e1f6c231a9125f962eb8e7cb8cd277'),
(95, '13201a40-2291-4194-b535-2fe151a4f7af', '8f256d78-32b4-40fe-b8b2-cb1b80ede9c1', '2026-10-12 00:08:58', 'warning', 'WaitlistRebuild', 'Blocked', 'user', '2', 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'WaitlistRebuild', NULL, NULL, NULL, 'WaitlistRebuild reuse=False list=3ms rows=3 declarations=1ms wcImages=23ms wcs=25 rows=4708ms job=2027ms urgency=0ms images=2681ms handlerState=1ms sourceUpdate=0ms', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"6736\",\"threadId\":\"15\",\"uptimeSeconds\":\"86\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '2cf2ea45be543189de2f9eceb54d496c03e1f6c231a9125f962eb8e7cb8cd277', 'f6f943a4972c2b6f4e955511e46c423f7903a3050af7a614c9d1b8ca81b42524'),
(96, 'ef684151-7fb9-4017-b085-43e6f5142919', 'e36e4e58-805b-4483-beb3-f8494c3247f4', '2026-10-12 00:10:56', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"23244\",\"threadId\":\"10\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'f6f943a4972c2b6f4e955511e46c423f7903a3050af7a614c9d1b8ca81b42524', '9e817b069af5ce6abb46b33b8f1b00cdf2c93d6148eb2807b0c104c4d8b80355'),
(97, '5fe9c386-69ec-4b9a-bf67-daff779d38df', 'ac6eabfe-3384-45a7-b615-d6bde3910f4f', '2026-10-12 00:11:11', 'warning', 'WaitlistRebuild', 'Blocked', 'user', '2', 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'WaitlistRebuild', NULL, NULL, NULL, 'WaitlistRebuild reuse=False list=20ms rows=3 declarations=6ms wcImages=38ms wcs=25 rows=1059ms job=1044ms urgency=0ms images=6ms handlerState=11ms sourceUpdate=1ms', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"23244\",\"threadId\":\"8\",\"uptimeSeconds\":\"18\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '9e817b069af5ce6abb46b33b8f1b00cdf2c93d6148eb2807b0c104c4d8b80355', 'e7a8db77857abbb7e59e46d904ea3f286780d0e6ee1de12e69519595889cf772'),
(98, 'e19253bb-8c69-4284-a780-4ac3a69a3563', '83cfb364-cd50-406e-a76b-3ea3eb7dd4aa', '2026-10-12 00:14:07', 'warning', 'WaitlistRebuild', 'Blocked', 'user', '2', 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'WaitlistRebuild', NULL, NULL, NULL, 'WaitlistRebuild reuse=False list=3ms rows=3 declarations=4ms wcImages=29ms wcs=25 rows=4720ms job=2009ms urgency=0ms images=2710ms handlerState=1ms sourceUpdate=0ms', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"23244\",\"threadId\":\"20\",\"uptimeSeconds\":\"195\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'e7a8db77857abbb7e59e46d904ea3f286780d0e6ee1de12e69519595889cf772', '66acbf643094af02dbf9039cea703b4474046d589966b2e04d9e871d1f0c376e'),
(99, '2039e4d6-809a-4cef-b7fa-fc1aca7c4c98', '2b718e26-52ee-4f31-b055-ad401ab9dc4a', '2026-10-12 00:16:46', 'warning', 'WaitlistRebuild', 'Blocked', 'user', '2', 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'WaitlistRebuild', NULL, NULL, NULL, 'WaitlistRebuild reuse=True list=0ms rows=3 declarations=3ms wcImages=25ms wcs=25 rows=1020ms job=1019ms urgency=0ms images=0ms handlerState=0ms sourceUpdate=1ms', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"23244\",\"threadId\":\"17\",\"uptimeSeconds\":\"353\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '66acbf643094af02dbf9039cea703b4474046d589966b2e04d9e871d1f0c376e', 'd94dabf613221d2e2df9306a08db57765dfa6ca81ee5daeb2f5683b9be44b954'),
(100, '8e28f2db-ad13-481b-971e-4b1828eb71e8', '7787b354-6352-4183-8249-88bde5c172fe', '2026-10-12 00:18:16', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"12644\",\"threadId\":\"8\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'd94dabf613221d2e2df9306a08db57765dfa6ca81ee5daeb2f5683b9be44b954', '68241eb835bc1870c9f90c596cb44b434ef12cc5b59a97a8753393b4537f60b9'),
(101, 'd21fb243-f486-41f8-8930-a17c7e93f219', 'f4df9de0-c6c7-41f4-8ddf-a5b0ccd1ae96', '2026-10-12 00:18:31', 'warning', 'WaitlistRebuild', 'Blocked', 'user', '2', 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'WaitlistRebuild', NULL, NULL, NULL, 'WaitlistRebuild reuse=False list=15ms rows=3 declarations=6ms wcImages=32ms wcs=25 warm=1052ms rows=15ms job=0ms urgency=0ms images=6ms handlerState=12ms sourceUpdate=1ms', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"12644\",\"threadId\":\"22\",\"uptimeSeconds\":\"18\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '68241eb835bc1870c9f90c596cb44b434ef12cc5b59a97a8753393b4537f60b9', 'd4e8ffe09164062a9667301e92bd64c4cf2b41ce1c3ac7c6b5fd4f616753576f'),
(102, '11a02dd2-5979-4247-bfe0-9d813f0cdc80', '70a5473c-1bc5-4d07-a04d-535053d92cc7', '2026-10-12 00:18:36', 'warning', 'WaitlistRebuild', 'Blocked', 'user', '2', 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'WaitlistRebuild', NULL, NULL, NULL, 'WaitlistRebuild reuse=False list=4ms rows=3 declarations=6ms wcImages=24ms wcs=25 warm=5ms rows=2ms job=0ms urgency=0ms images=2ms handlerState=1ms sourceUpdate=0ms', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"12644\",\"threadId\":\"10\",\"uptimeSeconds\":\"23\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'd4e8ffe09164062a9667301e92bd64c4cf2b41ce1c3ac7c6b5fd4f616753576f', '245251745309f190456e6f8496301aaab175e7be9d96dcc1e4ab8bfd809edb54'),
(103, 'aadd6651-25fa-4b81-9b90-b2f5e4c6ce35', '52559b1c-76a6-4aac-9550-06e02d6c4d8b', '2026-10-12 00:18:49', 'warning', 'WaitlistRebuild', 'Blocked', 'user', '2', 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'WaitlistRebuild', NULL, NULL, NULL, 'WaitlistRebuild reuse=False list=3ms rows=3 declarations=3ms wcImages=19ms wcs=25 warm=1010ms rows=2ms job=0ms urgency=0ms images=1ms handlerState=1ms sourceUpdate=0ms', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"12644\",\"threadId\":\"11\",\"uptimeSeconds\":\"36\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '245251745309f190456e6f8496301aaab175e7be9d96dcc1e4ab8bfd809edb54', 'f742813941cea3e3b594535c922a541dace88dc40c7b9b8eb29ecc1fb63feccf'),
(104, '946ff496-da9f-4116-aa8d-5f4e5ace6adf', '3a948539-8621-41fe-9f3b-24bb00faa2a1', '2026-10-12 00:21:29', 'warning', 'WaitlistRebuild', 'Blocked', 'user', '2', 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'WaitlistRebuild', NULL, NULL, NULL, 'WaitlistRebuild reuse=True list=1ms rows=3 declarations=2ms wcImages=21ms wcs=25 warm=2027ms rows=0ms job=0ms urgency=0ms images=0ms handlerState=0ms sourceUpdate=1ms', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"12644\",\"threadId\":\"13\",\"uptimeSeconds\":\"197\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'f742813941cea3e3b594535c922a541dace88dc40c7b9b8eb29ecc1fb63feccf', 'c5f84e12a4376c697a5ce4b18ac6495b9276b02ef7bef6d6c5fc9920b3a4e1c3'),
(105, '5a8fb47c-37b6-40bc-ba7c-e77d06ef4402', 'eeeba4bf-33e3-4887-8d95-9428a1ac3e64', '2026-10-12 00:29:39', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"24176\",\"threadId\":\"15\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'c5f84e12a4376c697a5ce4b18ac6495b9276b02ef7bef6d6c5fc9920b3a4e1c3', '2fc55767c991fd5f388c0de1f85bde3d071004b7263aaecb0046466055117ff3'),
(106, '22560c8d-f88f-4000-809f-0b6a4623ec7d', '44345f22-8ad3-4d65-b588-3a01198c0054', '2026-10-12 00:36:53', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"31856\",\"threadId\":\"10\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '2fc55767c991fd5f388c0de1f85bde3d071004b7263aaecb0046466055117ff3', '7da46ce6899116cf4b86aa988e563a8adadc2afe719dda053ff3b9b28023b6de'),
(107, '66741d4d-930a-403b-ae64-6a9cff5cd7fd', '7b266190-5bc1-4dd5-97c7-6fce56c98aab', '2026-10-12 00:42:18', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"7668\",\"threadId\":\"8\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '7da46ce6899116cf4b86aa988e563a8adadc2afe719dda053ff3b9b28023b6de', 'cc6af641de289eeba51a9a64500032763a1ab47b37190ea07ddd9dfb1dbf1364'),
(108, '8eb8d6a4-e183-4576-b442-9e809c95e7a3', 'ec3820f1-24d0-4336-81ae-6c163efe297d', '2026-10-12 00:48:51', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"16564\",\"threadId\":\"10\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'cc6af641de289eeba51a9a64500032763a1ab47b37190ea07ddd9dfb1dbf1364', '1585b8a247a1fe5e9cd82e89b24694e7150cf502560ae0b9a4f81f984faf10bf'),
(109, 'e34b681c-9901-45fb-be6f-14d664c472ca', '8078dfa3-717e-44bb-bb94-9780f217b354', '2026-10-12 01:04:56', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"22976\",\"threadId\":\"10\",\"uptimeSeconds\":\"5\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '1585b8a247a1fe5e9cd82e89b24694e7150cf502560ae0b9a4f81f984faf10bf', '8e20984a99ed95470d4b9cf6a6fd85488f06088ca05c51298dfdfc137ee318bb'),
(110, '9850c09a-2393-4fc1-a53f-6fb901255aa6', '439afc45-17a2-4754-b265-b146eec0229f', '2026-10-12 01:16:23', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"29404\",\"threadId\":\"11\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '8e20984a99ed95470d4b9cf6a6fd85488f06088ca05c51298dfdfc137ee318bb', 'de19b6431eb2f485c7bd5472812a2711cbd5c360090fcdfd0e809f86af696578'),
(111, '557d4c42-751c-4124-8d55-16c8d2562c4c', 'e42aae71-4c67-4800-8297-555f99c5c7bf', '2026-10-12 01:21:28', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"13884\",\"threadId\":\"8\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'de19b6431eb2f485c7bd5472812a2711cbd5c360090fcdfd0e809f86af696578', '7ecdc3545994871bf41563a0f12596da123c48c43b4bc270caddb68d06b03fd5'),
(112, 'c9909699-8d36-4759-855d-3b725cffd498', '5ee91fb4-a4ec-4cbb-aaa6-d0ec823ada62', '2026-10-12 01:26:57', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"9544\",\"threadId\":\"10\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '7ecdc3545994871bf41563a0f12596da123c48c43b4bc270caddb68d06b03fd5', '6148db0528364d4264e892555512c0ad25bc056eff80094207484fecb541a90f'),
(113, '51602943-39ce-4a66-adeb-9204a0e6757e', '7c2d7e85-d3e7-4b12-aa5a-c6ae7b86c6c9', '2026-10-12 01:35:44', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"3592\",\"threadId\":\"10\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '6148db0528364d4264e892555512c0ad25bc056eff80094207484fecb541a90f', '47f14ecb2b4140109b4c5a567baa25fc4fc824f3566487a2fd61bde09e4823e1'),
(114, 'ad27c769-38e6-4830-a4a5-69357c737849', '31d7414f-eab7-4bea-a4d3-048f0761b69f', '2026-10-12 01:43:59', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"10548\",\"threadId\":\"11\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '47f14ecb2b4140109b4c5a567baa25fc4fc824f3566487a2fd61bde09e4823e1', 'eb03335c97657e7ac19b3d96614a07bc6b521733bb9ff4255bea0834c1f353cd'),
(115, '3297b5c7-3a73-461f-8a17-f8fbda0a93db', 'f96ccd61-a095-4e14-87e1-637b4e0e1b50', '2026-10-12 01:46:02', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"11860\",\"threadId\":\"15\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'eb03335c97657e7ac19b3d96614a07bc6b521733bb9ff4255bea0834c1f353cd', '3c4f02a24c3260dbd8399efd9e9c9d10f34ad2f0d4506e433a80f11841bbf403'),
(116, '6dfd8719-6138-4462-ac33-e046e159d276', '4dfcb6eb-a912-40fe-a302-5f95d10d6bab', '2026-10-12 01:47:22', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"30752\",\"threadId\":\"11\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '3c4f02a24c3260dbd8399efd9e9c9d10f34ad2f0d4506e433a80f11841bbf403', '7ab8e458a21d6cc47d04cb81f35379185e1aa7bac1bcf896d31e45f30a5ec3eb'),
(117, '2efae8b4-a2d0-4611-ba39-322c7dafbb18', '20dee41c-8631-4d54-be9b-7fbac2793dfa', '2026-10-12 01:47:57', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"7212\",\"threadId\":\"8\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '7ab8e458a21d6cc47d04cb81f35379185e1aa7bac1bcf896d31e45f30a5ec3eb', '0aa28a312bf483fb28a63f7dc680bd0d633b9915ab84d8e8c039e58bdf4eebb1'),
(118, '6d5db1a6-f541-4b83-88d7-63e65dd46d67', 'f5bed60d-d80f-43bd-9ed1-9c0b4154706f', '2026-10-12 01:51:55', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"3000\",\"threadId\":\"11\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '0aa28a312bf483fb28a63f7dc680bd0d633b9915ab84d8e8c039e58bdf4eebb1', '9253e7fee9ad334d8842843d87d5477389ac72efcdbce3547e9db142841aba51'),
(119, '691b4693-2109-4183-a38b-c8edb772d078', '9c092a51-9f47-4ae0-bc85-ea234f4f0d37', '2026-10-12 01:55:00', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"14532\",\"threadId\":\"11\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '9253e7fee9ad334d8842843d87d5477389ac72efcdbce3547e9db142841aba51', '29954b20b2556912ba1277885d2bc4d1f729c03f5d07c147c5c68b4a64c0d870'),
(120, 'fc2d4b37-8e87-4d1d-9e04-f157ae3c1b66', '58bc21c6-2139-443c-9777-83ed90d45f2a', '2026-10-12 01:56:00', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"31504\",\"threadId\":\"11\",\"uptimeSeconds\":\"1\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '29954b20b2556912ba1277885d2bc4d1f729c03f5d07c147c5c68b4a64c0d870', '14a1169841bbc8664ed5cdcee8e140713b0d232d88d9a7e68896465895b5516f'),
(121, 'ee55aa63-1d81-4112-8b30-549a993e780a', 'cb80dbf9-0cf4-4017-b4eb-f8b8d56f86d3', '2026-10-12 01:56:52', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"29624\",\"threadId\":\"11\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '14a1169841bbc8664ed5cdcee8e140713b0d232d88d9a7e68896465895b5516f', '23ef8bd7c2678b50ac596c9d7014256623e0f1d2a6ad335928861eb791a7d5e0'),
(122, 'ed721740-337f-401c-9333-feda91e53076', '3c1d27cc-9abb-42f3-9826-c1fba46782ab', '2026-10-12 01:58:13', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"28208\",\"threadId\":\"8\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '23ef8bd7c2678b50ac596c9d7014256623e0f1d2a6ad335928861eb791a7d5e0', 'e444221312b40c0809e712950f2a9883f9c3adab5a1fe9834ac56a76421bfe48'),
(123, 'e2ff8aab-b7db-47aa-a424-8c67d9434d5f', '25fa81b9-489e-4a69-b3aa-903624d24981', '2026-10-12 01:59:35', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"4772\",\"threadId\":\"15\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'e444221312b40c0809e712950f2a9883f9c3adab5a1fe9834ac56a76421bfe48', 'e4299e00a084912202ee3a276b3ff80d5457a1843ab15911a2f429c378e3d28c'),
(124, '829ba6a7-971e-48b0-a724-edf77d9f6b08', '6fd8539c-2d76-43ac-9668-01b13bdb001f', '2026-10-12 02:01:28', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"10020\",\"threadId\":\"10\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'e4299e00a084912202ee3a276b3ff80d5457a1843ab15911a2f429c378e3d28c', '0c25ca30fcb8670fa970ba634c65c6c4472435e3ee5857df7f7f7fa9b241a990'),
(125, '3036665f-67b1-45a6-bc30-ad79da93f4e5', '9e9ced91-f09e-495a-9428-a6853d0f6853', '2026-10-12 02:13:09', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"31736\",\"threadId\":\"10\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '0c25ca30fcb8670fa970ba634c65c6c4472435e3ee5857df7f7f7fa9b241a990', '6a757d33dedeccdf54fa5e3aad07935868b29e3aa45055001ffed75d7607ae21'),
(126, 'd2985e1a-9e45-4b2a-a9fe-a78f32bc6147', '59e0a7b1-a003-45b0-8fde-2dbe6a0553be', '2026-10-12 02:17:32', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"24132\",\"threadId\":\"11\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '6a757d33dedeccdf54fa5e3aad07935868b29e3aa45055001ffed75d7607ae21', 'd266ad399ca42791320c383d43ca13cd99232ef621c287e8f0d4dc32280c51c0'),
(127, 'c5d69b36-49a2-4b85-b89c-f172dd212e04', 'b176d52d-f347-41cf-9d79-7e7896cc6cd1', '2026-10-12 02:23:38', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"28680\",\"threadId\":\"8\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', 'd266ad399ca42791320c383d43ca13cd99232ef621c287e8f0d4dc32280c51c0', '729ac4f12f7fcbc0e68d43ee615c6f997400cc027fa77248f1b06a2651a7cc9b'),
(128, '4148b406-2818-4e49-90c8-542a7e4f53ed', '6b453b4d-4ad0-4a3f-9f23-75bc3706d5c7', '2026-10-12 02:24:19', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"11368\",\"threadId\":\"15\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '729ac4f12f7fcbc0e68d43ee615c6f997400cc027fa77248f1b06a2651a7cc9b', '76ad8c849e26df69139cb0dee7229aacae1162b9f93ddc6f5a2968b3f14e60b3'),
(129, '87eba9b3-788c-43c6-993f-fac9f8ea6181', 'a531bafd-838f-4cca-a305-0478cd59ae2d', '2026-10-12 02:31:10', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"31152\",\"threadId\":\"8\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '76ad8c849e26df69139cb0dee7229aacae1162b9f93ddc6f5a2968b3f14e60b3', '512a90f330c2c22829a4a716931f50e79b679648b3a9049f0f1979f7b747bf8f'),
(130, '949cabc1-2e24-47fd-bdfc-379b3886b1ce', 'd4eb1226-1d9a-4714-9266-9d7a5899b488', '2026-10-12 04:31:41', 'error', 'PictureCache', 'Failure', 'system', NULL, 'JOHNSPC', 'd8-43-ae-47-d0-d6', 'PictureCache', NULL, NULL, NULL, 'The picture share could not be read, so the copies held on this computer are the ones being used. Sources that could not be read: waitlist pictures, dunnage pictures.', '{\"runtime\":{\"application\":\"MTM_Waitlist\",\"version\":\"1.1.0.0\",\"informationalVersion\":\"1.1.0\\u002Bf2202b4d4e0a06088771b5b1ee69f2862b073499\",\"osDescription\":\"Microsoft Windows 10.0.26300\",\"osArchitecture\":\"X64\",\"processArchitecture\":\"X64\",\"is64BitProcess\":true,\"is64BitOperatingSystem\":true,\"framework\":\".NET 10.0.12\",\"windowsAppSdkVersion\":\"3.0.0.0\",\"isPackaged\":false,\"processId\":\"8588\",\"threadId\":\"12\",\"uptimeSeconds\":\"4\",\"culture\":\"en-US\",\"uiCulture\":\"en-US\"},\"ui\":{\"window\":null,\"screen\":null,\"control\":null,\"controlType\":null,\"event\":null,\"command\":null,\"isUiThread\":null},\"database\":{\"provider\":null,\"providerVersion\":null,\"errorCode\":null,\"sqlState\":null,\"number\":null,\"database\":\"mtm_waitlist\",\"operation\":\"insert\",\"storedProcedure\":\"sp_ops_startup_logs_insert\",\"retryAttempt\":null,\"transactionActive\":null,\"transactionIsolationLevel\":null,\"connectionState\":null,\"commandTimeoutSeconds\":null,\"connectionTimeoutSeconds\":null,\"affectedRows\":null,\"elapsedMilliseconds\":null,\"statementFingerprint\":null}}', '512a90f330c2c22829a4a716931f50e79b679648b3a9049f0f1979f7b747bf8f', 'c7b641cb4bdcdbf93d29002757d145a777ebc41689cceee69d60bcb8dc0ba91e');

DROP TABLE IF EXISTS `seed_username_case_backup`;
CREATE TABLE `seed_username_case_backup` (
  `user_id` bigint(20) NOT NULL,
  `previous_username_normalized` varchar(128) COLLATE utf8mb4_unicode_ci NOT NULL,
  `recorded_utc` datetime NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

TRUNCATE TABLE `seed_username_case_backup`;
INSERT INTO `seed_username_case_backup` (`user_id`, `previous_username_normalized`, `recorded_utc`) VALUES
(1, 'johnk', '2026-10-01 18:45:56'),
(2, 'jkoll', '2026-10-01 18:45:56'),
(3, 'test.admin', '2026-10-01 18:45:56'),
(4, 'test.developer', '2026-10-01 18:45:56'),
(5, 'test.plant.manager', '2026-10-01 18:45:56'),
(6, 'test.setup.lead', '2026-10-01 18:45:56'),
(7, 'test.production.lead', '2026-10-01 18:45:56'),
(8, 'test.setup', '2026-10-01 18:45:56'),
(9, 'test.production', '2026-10-01 18:45:56'),
(10, 'test.material.handler', '2026-10-01 18:45:56');

DROP TABLE IF EXISTS `setup_active_jobs`;
CREATE TABLE `setup_active_jobs` (
  `id` bigint(20) NOT NULL COMMENT 'Surrogate primary key.',
  `public_id` char(36) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Public UUID for active job row.',
  `work_order` varchar(32) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Work order number.',
  `part_number` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Part number for the setup job.',
  `sequence_number` varchar(32) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Sequence number for the setup job.',
  `work_center` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Work center or press identifier.',
  `selected_dunnage_type_id` varchar(64) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Selected dunnage type identifier.',
  `selected_dunnage_part_id` varchar(64) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Selected dunnage part identifier.',
  `subordinate_parts_json` json DEFAULT NULL COMMENT 'Serialized subordinate parts payload for the active work-center setup row.',
  `selected_dunnage_parts_json` json DEFAULT NULL COMMENT 'Serialized selected dunnage parts payload.',
  `is_active` tinyint(1) NOT NULL DEFAULT '1' COMMENT 'Whether the active job row is active.',
  `created_by_user_id` bigint(20) DEFAULT NULL COMMENT 'User who created the active job row.',
  `updated_by_user_id` bigint(20) DEFAULT NULL COMMENT 'User who last updated the active job row.',
  `created_utc` datetime NOT NULL COMMENT 'UTC timestamp when row was created.',
  `updated_utc` datetime NOT NULL COMMENT 'UTC timestamp when row was last updated.'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Current active setup job assignments by work center.';

TRUNCATE TABLE `setup_active_jobs`;
INSERT INTO `setup_active_jobs` (`id`, `public_id`, `work_order`, `part_number`, `sequence_number`, `work_center`, `selected_dunnage_type_id`, `selected_dunnage_part_id`, `subordinate_parts_json`, `selected_dunnage_parts_json`, `is_active`, `created_by_user_id`, `updated_by_user_id`, `created_utc`, `updated_utc`) VALUES
(1, 'c3000000-0000-4000-8000-000000000001', 'WO-055298', 'S22919', '29', '100-03', NULL, '42', '[{\"User8\": \"X\", \"Category\": \"Die\", \"Location\": \"U-Z2-03\", \"ImagePath\": \"Assets/Placeholders/default-no-image.png\", \"IsLowStock\": false, \"PartNumber\": \"FGT0279-02\", \"Description\": \"Hand Load\", \"ShowsOnHand\": false, \"IsOutOfStock\": true, \"StockMessage\": \"NONE ON HAND!\", \"CategoryGlyph\": \"\", \"OnHandDisplay\": \"0\", \"StockStateKey\": \"OutOfStock\", \"OnHandQuantity\": 0, \"LocationDisplay\": \"U-Z2-03\", \"CategoryAccentKey\": \"Die\", \"ImageAutomationId\": \"SetupReviewPage_SubordinatePartImage_FGT0279-02\", \"SelectedScrapType\": \"3003 Aluminum\", \"CategoryDisplayName\": \"Die\"}]', '[{\"Id\": \"42\", \"TypeId\": \"6\", \"HasImage\": false, \"Metadata\": \"Quantity Type: Quantity | Home Location: Unassigned\", \"ImagePath\": \"\", \"PartNumber\": \"Bag 40x48 Gaylord\", \"DisplayName\": \"Bag 40x48 Gaylord\", \"HomeLocation\": \"\", \"DunnageTypeName\": \"Bags\", \"DisplayImagePath\": \"Assets/Placeholders/default-no-image.png\", \"ImageFallbackText\": \"No image available\", \"IsSelectedForPair\": false, \"ShowNoImagePlaceholder\": true}]', 1, NULL, NULL, '2026-10-01 18:45:56', '2026-10-11 15:44:40'),
(2, 'c3000000-0000-4000-8000-000000000002', 'WO-900002', 'PART-9002', '10', '100-06', NULL, NULL, '[{\"User8\": \"\", \"Category\": \"Component\", \"Location\": \"B-02-03\", \"IsLowStock\": false, \"PartNumber\": \"MMF0001154\", \"Description\": \"Flatstock 0.125 x 36\", \"OnHandQuantity\": 88.00, \"SelectedScrapType\": \"No Scrap\"}]', NULL, 1, NULL, NULL, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(3, 'c3000000-0000-4000-8000-000000000003', 'WO-900003', 'PART-9003', '10', '100-07', NULL, NULL, '[{\"User8\": \"\", \"Category\": \"Die\", \"Location\": \"DIE SHOP\", \"IsLowStock\": false, \"PartNumber\": \"FGT0002000\", \"Description\": \"Die 9003-A\", \"OnHandQuantity\": 0, \"SelectedScrapType\": \"Scrap Type Required\"}]', NULL, 1, NULL, NULL, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(4, 'c3000000-0000-4000-8000-000000000004', 'WO-900004', 'PART-9004', '10', '100-18', NULL, NULL, '[{\"User8\": \"\", \"Category\": \"Component\", \"Location\": \"C-04-12\", \"IsLowStock\": false, \"PartNumber\": \"CMP0004455\", \"Description\": \"Bushing, bronze\", \"OnHandQuantity\": 240.00, \"SelectedScrapType\": \"No Scrap\"}]', NULL, 1, NULL, NULL, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(5, 'c3000000-0000-4000-8000-000000000005', 'WO-900005', 'PART-9005', '10', '100-1806', 'DNG-TYPE-01', 'DNG-PART-01', '[]', '[{\"Id\": \"DNG-PART-01\", \"TypeId\": \"DNG-TYPE-01\", \"Metadata\": \"\", \"ImagePath\": \"\", \"PartNumber\": \"DNG0007788\", \"DisplayName\": \"Rack, 24 x 36 wire\", \"HomeLocation\": \"DUNNAGE-ROW-3\", \"DunnageTypeName\": \"Rack\", \"IsSelectedForPair\": true}]', 1, NULL, NULL, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(6, 'c3000000-0000-4000-8000-000000000006', 'WO-900006', 'PART-9006', '20', 'V100-33', 'DNG-TYPE-02', 'DNG-PART-02', '[{\"User8\": \"\", \"Category\": \"Coil\", \"Location\": \"A-01-07\", \"IsLowStock\": false, \"PartNumber\": \"MMC0001001\", \"Description\": \"Coil 0.048 x 36 wide\", \"OnHandQuantity\": 640.00, \"SelectedScrapType\": \"Aluminum Offal\"}, {\"User8\": \"\", \"Category\": \"Flatstock\", \"Location\": \"B-02-08\", \"IsLowStock\": true, \"PartNumber\": \"MMF0001155\", \"Description\": \"Flatstock 0.090 x 24\", \"OnHandQuantity\": 12.00, \"SelectedScrapType\": \"No Scrap\"}, {\"User8\": \"\", \"Category\": \"Die\", \"Location\": \"PRESS BAY\", \"IsLowStock\": false, \"PartNumber\": \"FGT0002001\", \"Description\": \"Die 9006-B\", \"OnHandQuantity\": 0, \"SelectedScrapType\": \"Scrap Type Required\"}, {\"User8\": \"\", \"Category\": \"Component\", \"Location\": \"C-04-15\", \"IsLowStock\": false, \"PartNumber\": \"CMP0004456\", \"Description\": \"Pin, dowel 8mm\", \"OnHandQuantity\": 0, \"SelectedScrapType\": \"\"}]', '[{\"Id\": \"DNG-PART-02\", \"TypeId\": \"DNG-TYPE-02\", \"Metadata\": \"\", \"ImagePath\": \"\", \"PartNumber\": \"DNG0007789\", \"DisplayName\": \"Tote, collapsible\", \"HomeLocation\": \"DUNNAGE-ROW-4\", \"DunnageTypeName\": \"Tote\", \"IsSelectedForPair\": true}]', 1, NULL, NULL, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(7, 'c3000000-0000-4000-8000-000000000007', 'WO-900007', 'PART-9007', '10', 'V100-34', NULL, NULL, '[]', NULL, 1, NULL, NULL, '2026-10-01 18:45:56', '2026-10-01 18:45:56');

DROP TABLE IF EXISTS `setup_job_history`;
CREATE TABLE `setup_job_history` (
  `id` bigint(20) NOT NULL COMMENT 'Surrogate primary key.',
  `public_id` char(36) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Public UUID for history row.',
  `active_job_id` bigint(20) DEFAULT NULL COMMENT 'Optional foreign key to setup_active_jobs.id.',
  `event_action` varchar(32) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Event action for history row.',
  `work_order` varchar(32) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Work order at time of event.',
  `part_number` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Part number at time of event.',
  `sequence_number` varchar(32) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Sequence number at time of event.',
  `work_center` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Work center at time of event.',
  `selected_dunnage_type_id` varchar(64) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Selected dunnage type identifier snapshot.',
  `selected_dunnage_part_id` varchar(64) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Selected dunnage part identifier snapshot.',
  `subordinate_parts_json` json DEFAULT NULL COMMENT 'Serialized subordinate parts snapshot, including saved scrap selection for the exact work order/part/sequence event.',
  `selected_dunnage_parts_json` json DEFAULT NULL COMMENT 'Serialized selected dunnage parts snapshot.',
  `changed_by_user_id` bigint(20) DEFAULT NULL COMMENT 'User who caused the history event.',
  `changed_utc` datetime NOT NULL COMMENT 'UTC timestamp when history row was recorded.'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Historical setup job events and saved setup snapshots.';

TRUNCATE TABLE `setup_job_history`;
INSERT INTO `setup_job_history` (`id`, `public_id`, `active_job_id`, `event_action`, `work_order`, `part_number`, `sequence_number`, `work_center`, `selected_dunnage_type_id`, `selected_dunnage_part_id`, `subordinate_parts_json`, `selected_dunnage_parts_json`, `changed_by_user_id`, `changed_utc`) VALUES
(1, 'b09f4c3b-c58a-11f1-9c65-d843ae47d0d6', 1, 'save', 'WO-055298', 'S22919', '29', '100-03', NULL, '42', '[{\"User8\": \"X\", \"Category\": \"Die\", \"Location\": \"U-Z2-03\", \"ImagePath\": \"Assets/Placeholders/default-no-image.png\", \"IsLowStock\": false, \"PartNumber\": \"FGT0279-02\", \"Description\": \"Hand Load\", \"ShowsOnHand\": false, \"IsOutOfStock\": true, \"StockMessage\": \"NONE ON HAND!\", \"CategoryGlyph\": \"\", \"OnHandDisplay\": \"0\", \"StockStateKey\": \"OutOfStock\", \"OnHandQuantity\": 0, \"LocationDisplay\": \"U-Z2-03\", \"CategoryAccentKey\": \"Die\", \"ImageAutomationId\": \"SetupReviewPage_SubordinatePartImage_FGT0279-02\", \"SelectedScrapType\": \"3003 Aluminum\", \"CategoryDisplayName\": \"Die\"}]', '[{\"Id\": \"42\", \"TypeId\": \"6\", \"HasImage\": false, \"Metadata\": \"Quantity Type: Quantity | Home Location: Unassigned\", \"ImagePath\": \"\", \"PartNumber\": \"Bag 40x48 Gaylord\", \"DisplayName\": \"Bag 40x48 Gaylord\", \"HomeLocation\": \"\", \"DunnageTypeName\": \"Bags\", \"DisplayImagePath\": \"Assets/Placeholders/default-no-image.png\", \"ImageFallbackText\": \"No image available\", \"IsSelectedForPair\": false, \"ShowNoImagePlaceholder\": true}]', NULL, '2026-10-11 15:44:40');

DROP TABLE IF EXISTS `setup_part_sequence_custom_data`;
CREATE TABLE `setup_part_sequence_custom_data` (
  `id` bigint(20) NOT NULL COMMENT 'Surrogate primary key.',
  `public_id` char(36) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Public UUID for custom pair row.',
  `part_number` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Part number key for custom setup data.',
  `sequence_number` varchar(32) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Sequence number key for custom setup data.',
  `selected_scrap_type` varchar(128) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Saved scrap selection for the part/sequence pair.',
  `selected_dunnage_type_id` varchar(64) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Saved dunnage type identifier for the part/sequence pair.',
  `selected_dunnage_part_id` varchar(64) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Saved dunnage part identifier for the part/sequence pair.',
  `subordinate_parts_json` json DEFAULT NULL COMMENT 'Serialized subordinate parts payload for the part/sequence pair.',
  `selected_dunnage_parts_json` json DEFAULT NULL COMMENT 'Serialized selected dunnage parts payload for the part/sequence pair.',
  `created_by_user_id` bigint(20) DEFAULT NULL COMMENT 'User who created the custom pair row.',
  `updated_by_user_id` bigint(20) DEFAULT NULL COMMENT 'User who last updated the custom pair row.',
  `created_utc` datetime NOT NULL COMMENT 'UTC timestamp when row was created.',
  `updated_utc` datetime NOT NULL COMMENT 'UTC timestamp when row was last updated.'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Custom setup data keyed by part and sequence for values not sourced from Infor Visual.';

TRUNCATE TABLE `setup_part_sequence_custom_data`;
INSERT INTO `setup_part_sequence_custom_data` (`id`, `public_id`, `part_number`, `sequence_number`, `selected_scrap_type`, `selected_dunnage_type_id`, `selected_dunnage_part_id`, `subordinate_parts_json`, `selected_dunnage_parts_json`, `created_by_user_id`, `updated_by_user_id`, `created_utc`, `updated_utc`) VALUES
(1, 'b09eae92-c58a-11f1-9c65-d843ae47d0d6', 'S22919', '29', '3003 Aluminum', NULL, '42', '[{\"User8\": \"X\", \"Category\": \"Die\", \"Location\": \"U-Z2-03\", \"ImagePath\": \"Assets/Placeholders/default-no-image.png\", \"IsLowStock\": false, \"PartNumber\": \"FGT0279-02\", \"Description\": \"Hand Load\", \"ShowsOnHand\": false, \"IsOutOfStock\": true, \"StockMessage\": \"NONE ON HAND!\", \"CategoryGlyph\": \"\", \"OnHandDisplay\": \"0\", \"StockStateKey\": \"OutOfStock\", \"OnHandQuantity\": 0, \"LocationDisplay\": \"U-Z2-03\", \"CategoryAccentKey\": \"Die\", \"ImageAutomationId\": \"SetupReviewPage_SubordinatePartImage_FGT0279-02\", \"SelectedScrapType\": \"3003 Aluminum\", \"CategoryDisplayName\": \"Die\"}]', '[{\"Id\": \"42\", \"TypeId\": \"6\", \"HasImage\": false, \"Metadata\": \"Quantity Type: Quantity | Home Location: Unassigned\", \"ImagePath\": \"\", \"PartNumber\": \"Bag 40x48 Gaylord\", \"DisplayName\": \"Bag 40x48 Gaylord\", \"HomeLocation\": \"\", \"DunnageTypeName\": \"Bags\", \"DisplayImagePath\": \"Assets/Placeholders/default-no-image.png\", \"ImageFallbackText\": \"No image available\", \"IsSelectedForPair\": false, \"ShowNoImagePlaceholder\": true}]', NULL, NULL, '2026-10-11 15:44:40', '2026-10-11 15:44:40');

DROP TABLE IF EXISTS `setup_work_centers_catalog`;
CREATE TABLE `setup_work_centers_catalog` (
  `id` bigint(20) NOT NULL COMMENT 'Surrogate primary key.',
  `public_id` char(36) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Public UUID for work center catalog row.',
  `building` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'Expo Drive' COMMENT 'Facility building where the work center is located.',
  `work_center_name` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Work center or press display name.',
  `is_active` tinyint(1) NOT NULL DEFAULT '1' COMMENT 'Whether this work center is active.',
  `sort_rank` int(11) NOT NULL DEFAULT '100' COMMENT 'Sort order rank for UI lists.',
  `created_by_user_id` bigint(20) DEFAULT NULL COMMENT 'User who created the work center row.',
  `updated_by_user_id` bigint(20) DEFAULT NULL COMMENT 'User who last updated the work center row.',
  `created_utc` datetime NOT NULL COMMENT 'UTC timestamp when row was created.',
  `updated_utc` datetime NOT NULL COMMENT 'UTC timestamp when row was last updated.'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Setup work center catalog used by setup and waitlist flows.';

TRUNCATE TABLE `setup_work_centers_catalog`;
INSERT INTO `setup_work_centers_catalog` (`id`, `public_id`, `building`, `work_center_name`, `is_active`, `sort_rank`, `created_by_user_id`, `updated_by_user_id`, `created_utc`, `updated_utc`) VALUES
(1, '569c77f6-bdc8-11f1-bdd0-d843ae47d0d6', 'Expo Drive', '100-03', 1, 10, NULL, NULL, '2026-10-01 18:45:56', '2026-10-11 15:44:40'),
(2, '569c7928-bdc8-11f1-bdd0-d843ae47d0d6', 'Expo Drive', '100-06', 1, 20, NULL, NULL, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(3, '569c79c4-bdc8-11f1-bdd0-d843ae47d0d6', 'Expo Drive', '100-07', 1, 30, NULL, NULL, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(4, '569c7a2e-bdc8-11f1-bdd0-d843ae47d0d6', 'Expo Drive', '100-08', 1, 40, NULL, NULL, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(5, '569c7a93-bdc8-11f1-bdd0-d843ae47d0d6', 'Expo Drive', '100-09', 1, 50, NULL, NULL, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(6, '569c7af9-bdc8-11f1-bdd0-d843ae47d0d6', 'Expo Drive', '100-12', 1, 60, NULL, NULL, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(7, '569c7b52-bdc8-11f1-bdd0-d843ae47d0d6', 'Expo Drive', '100-13', 1, 70, NULL, NULL, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(8, '569c7ba8-bdc8-11f1-bdd0-d843ae47d0d6', 'Expo Drive', '100-14', 1, 80, NULL, NULL, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(9, '569c7c07-bdc8-11f1-bdd0-d843ae47d0d6', 'Expo Drive', '100-15', 1, 90, NULL, NULL, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(10, '569c7c5e-bdc8-11f1-bdd0-d843ae47d0d6', 'Expo Drive', '100-16', 1, 100, NULL, NULL, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(11, '569c7cb4-bdc8-11f1-bdd0-d843ae47d0d6', 'Expo Drive', '100-17', 1, 110, NULL, NULL, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(12, '569c7d07-bdc8-11f1-bdd0-d843ae47d0d6', 'Expo Drive', '100-18', 1, 120, NULL, NULL, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(13, '569c7d61-bdc8-11f1-bdd0-d843ae47d0d6', 'Expo Drive', '100-19', 1, 130, NULL, NULL, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(14, '569c7db5-bdc8-11f1-bdd0-d843ae47d0d6', 'Expo Drive', '100-20', 1, 140, NULL, NULL, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(15, '569c7e09-bdc8-11f1-bdd0-d843ae47d0d6', 'Expo Drive', '100-21', 1, 150, NULL, NULL, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(16, '569c7e5d-bdc8-11f1-bdd0-d843ae47d0d6', 'Expo Drive', '100-22', 1, 160, NULL, NULL, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(17, '569c7eb5-bdc8-11f1-bdd0-d843ae47d0d6', 'Expo Drive', '100-23', 1, 170, NULL, NULL, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(18, '569c7f0b-bdc8-11f1-bdd0-d843ae47d0d6', 'Expo Drive', '100-24', 1, 180, NULL, NULL, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(19, '569c7f60-bdc8-11f1-bdd0-d843ae47d0d6', 'Expo Drive', '100-25', 1, 190, NULL, NULL, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(20, '569c7fb2-bdc8-11f1-bdd0-d843ae47d0d6', 'Expo Drive', '100-28', 1, 200, NULL, NULL, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(21, '569c800a-bdc8-11f1-bdd0-d843ae47d0d6', 'Expo Drive', '100-1807', 1, 210, NULL, NULL, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(22, '569c806c-bdc8-11f1-bdd0-d843ae47d0d6', 'Expo Drive', '100-1806', 1, 220, NULL, NULL, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(23, '569c80cb-bdc8-11f1-bdd0-d843ae47d0d6', 'Vits Drive', 'V100-33', 1, 230, NULL, NULL, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(24, '569c8123-bdc8-11f1-bdd0-d843ae47d0d6', 'Vits Drive', 'V100-34', 1, 240, NULL, NULL, '2026-10-01 18:45:56', '2026-10-01 18:45:56'),
(25, '569c817c-bdc8-11f1-bdd0-d843ae47d0d6', 'Vits Drive', 'V100-35', 1, 250, NULL, NULL, '2026-10-01 18:45:56', '2026-10-01 18:45:56');

DROP TABLE IF EXISTS `user_active_sessions`;
CREATE TABLE `user_active_sessions` (
  `id` bigint(20) NOT NULL,
  `public_id` char(36) COLLATE utf8mb4_unicode_ci NOT NULL,
  `user_id` bigint(20) NOT NULL,
  `computer_id` bigint(20) NOT NULL,
  `token_hash` char(64) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'SHA-256 hex digest of the issued token; never the token itself',
  `token_salt` varbinary(32) NOT NULL COMMENT 'Salt the digest was computed with; present whenever a digest is',
  `issued_utc` datetime NOT NULL COMMENT 'When the token was issued, from the store clock',
  `expires_utc` datetime NOT NULL COMMENT 'When the token stops being valid, from the store clock',
  `revoked_utc` datetime DEFAULT NULL COMMENT 'When the session was cleared; NULL while it has not been',
  `is_active` tinyint(1) NOT NULL DEFAULT '1' COMMENT 'Soft state: 0 once cleared, so the row survives as evidence',
  `source_label` varchar(32) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Caller vocabulary for where the session came from',
  `created_utc` datetime NOT NULL COMMENT 'When this row was first written; unchanged by a later sign-in'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='The one active session for a person on a machine: a salted token digest, its lifetime and its machine key.';

TRUNCATE TABLE `user_active_sessions`;
INSERT INTO `user_active_sessions` (`id`, `public_id`, `user_id`, `computer_id`, `token_hash`, `token_salt`, `issued_utc`, `expires_utc`, `revoked_utc`, `is_active`, `source_label`, `created_utc`) VALUES
(1, '278a6b00-3e53-4fc2-b43a-385e2956c86f', 2, 1, 'e5027decb0de4d896ce4950d39ee3d2e1d2b299c640e611a90dffb31f2beb2a4', 0x7872f57c8531ea166abd4ab5633100d7, '2026-10-12 04:31:50', '2026-10-12 12:31:50', NULL, 1, 'sign-in', '2026-10-01 18:47:02');
DROP VIEW IF EXISTS `vw_config_settings_scope_catalog`;
CREATE TABLE `vw_config_settings_scope_catalog` (
`setting_key` varchar(190)
,`scope_type` varchar(16)
,`scope_key` varchar(255)
,`computer_id` bigint(20)
,`user_id` bigint(20)
,`scope_rank` tinyint(4)
,`setting_value` text
,`setting_value_int` bigint(20)
,`setting_value_bool` tinyint(1)
,`setting_value_decimal` decimal(18,6)
,`setting_value_datetime_utc` datetime
,`value_type` varchar(32)
,`updated_by_user_id` bigint(20)
,`updated_utc` datetime
);
DROP VIEW IF EXISTS `vw_setup_work_centers_active`;
CREATE TABLE `vw_setup_work_centers_active` (
`id` bigint(20)
,`public_id` char(36)
,`building` varchar(64)
,`work_center_name` varchar(64)
,`is_active` tinyint(1)
,`sort_rank` int(11)
,`created_utc` datetime
,`updated_utc` datetime
);

DROP TABLE IF EXISTS `waitlist_defect_types`;
CREATE TABLE `waitlist_defect_types` (
  `id` bigint(20) NOT NULL COMMENT 'Surrogate primary key.',
  `public_id` char(36) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Public UUID for the defect type row.',
  `defect_name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Display name of the NCM defect type (e.g. Scratch, Dent, Wrong Color).',
  `description` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Optional short description shown in the editor/picker.',
  `sort_order` int(11) NOT NULL DEFAULT '0' COMMENT 'Display order within the picker list.',
  `is_active` tinyint(1) NOT NULL DEFAULT '1' COMMENT 'Whether the defect type is offered to workers.',
  `created_by_user_id` bigint(20) DEFAULT NULL COMMENT 'User who created the defect type.',
  `updated_by_user_id` bigint(20) DEFAULT NULL COMMENT 'User who last updated the defect type.',
  `created_utc` datetime NOT NULL COMMENT 'UTC timestamp when the defect type row was created.',
  `updated_utc` datetime NOT NULL COMMENT 'UTC timestamp when the defect type row was last updated.'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Managed list of non-conforming (NCM) defect types referenced by Pickup NCM requests (edited from Module_Settings).';

TRUNCATE TABLE `waitlist_defect_types`;
DROP TABLE IF EXISTS `waitlist_requests_audit`;
CREATE TABLE `waitlist_requests_audit` (
  `id` bigint(20) NOT NULL COMMENT 'Surrogate primary key.',
  `public_id` char(36) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Public UUID for the audit entry.',
  `request_public_id` char(36) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Waitlist request public_id this entry belongs to.',
  `from_status` varchar(32) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Status the request transitioned from.',
  `to_status` varchar(32) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Status the request transitioned to.',
  `event_type` varchar(32) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Lifecycle event (Created/Accepted/Completed/Canceled/Released).',
  `actor_employee_number` varchar(32) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Employee number of the actor who caused the event.',
  `actor_employee_name` varchar(128) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Display name of the actor who caused the event.',
  `details` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Free-text detail (e.g. cancellation reason).',
  `occurred_utc` datetime NOT NULL COMMENT 'UTC timestamp when the event occurred.'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Immutable audit/history trail of waitlist request status transitions (never purged).';

TRUNCATE TABLE `waitlist_requests_audit`;
INSERT INTO `waitlist_requests_audit` (`id`, `public_id`, `request_public_id`, `from_status`, `to_status`, `event_type`, `actor_employee_number`, `actor_employee_name`, `details`, `occurred_utc`) VALUES
(1, 'a0000000-0001-4000-8000-000000000001', 'f0000000-0001-4000-8000-000000000001', NULL, NULL, 'Created', '6229', 'John Koll', 'Request submitted for press 100-03.', '2026-10-01 18:10:57'),
(2, 'a0000000-0002-4000-8000-000000000002', 'f0000000-0002-4000-8000-000000000002', NULL, NULL, 'Created', '6229', 'John Koll', 'Wrong coil reported.', '2026-10-01 18:33:57'),
(3, 'a0000000-0003-4000-8000-000000000003', 'f0000000-0007-4000-8000-000000000007', NULL, NULL, 'Created', '5000', 'Other User', 'Other requester WIP request.', '2026-10-01 18:15:57'),
(4, 'a0000000-0010-4000-8000-000000000010', 'f0000000-0010-4000-8000-000000000010', NULL, 'Pending', 'Created', '6229', 'John Koll', 'Request created.', '2026-10-01 17:25:57'),
(5, 'a0000000-0011-4000-8000-000000000011', 'f0000000-0010-4000-8000-000000000010', 'Pending', 'Accepted', 'Accepted', '6229', 'John Koll', 'Handler claimed overdue coil move.', '2026-10-01 17:30:57'),
(6, 'a0000000-0013-4000-8000-000000000013', 'f0000000-0013-4000-8000-000000000013', NULL, 'Pending', 'Created', '6229', 'John Koll', 'Scrap request created.', '2026-10-01 15:45:57'),
(7, 'a0000000-0014-4000-8000-000000000014', 'f0000000-0013-4000-8000-000000000013', 'Pending', 'Accepted', 'Accepted', '6229', 'John Koll', 'Scrap accepted.', '2026-10-01 15:55:57'),
(8, 'a0000000-0015-4000-8000-000000000015', 'f0000000-0013-4000-8000-000000000013', 'Accepted', 'Completed', 'Completed', '6229', 'John Koll', 'Scrap completed.', '2026-10-01 16:45:57'),
(9, 'a0000000-0016-4000-8000-000000000016', 'f0000000-0015-4000-8000-000000000015', NULL, 'Pending', 'Created', '6229', 'John Koll', 'Outside-service request created.', '2026-10-01 16:45:57'),
(10, 'a0000000-0017-4000-8000-000000000017', 'f0000000-0015-4000-8000-000000000015', 'Pending', 'Canceled', 'Canceled', '6229', 'John Koll', 'Requester canceled own request.', '2026-10-01 16:55:57'),
(11, 'a0000000-0018-4000-8000-000000000018', 'f0000000-0016-4000-8000-000000000016', NULL, 'Pending', 'Created', '5000', 'Other User', 'Coil request created.', '2026-10-01 13:45:57'),
(12, 'a0000000-0019-4000-8000-000000000019', 'f0000000-0016-4000-8000-000000000016', 'Pending', 'Canceled', 'Canceled', '5000', 'Other User', 'Duplicate - canceled by requester.', '2026-10-01 14:05:57'),
(13, 'ca063dc2-c585-11f1-9c65-d843ae47d0d6', 'f0000000-0006-4000-8000-000000000006', 'Pending', 'Canceled', 'Canceled', '6229', NULL, NULL, '2026-10-11 15:09:35'),
(14, 'd1b0a8cd-c585-11f1-9c65-d843ae47d0d6', 'f0000000-0004-4000-8000-000000000004', 'Pending', 'Canceled', 'Canceled', '6229', NULL, NULL, '2026-10-11 15:09:48'),
(15, 'bcfed788-c588-11f1-9c65-d843ae47d0d6', 'f0000000-0002-4000-8000-000000000002', 'Pending', 'Canceled', 'Canceled', '6229', NULL, NULL, '2026-10-11 15:30:42'),
(16, 'd44cdda5-c588-11f1-9c65-d843ae47d0d6', 'f0000000-0011-4000-8000-000000000011', 'Accepted', 'Canceled', 'Canceled', '6229', NULL, NULL, '2026-10-11 15:31:21'),
(17, 'd5eb64b4-c588-11f1-9c65-d843ae47d0d6', 'f0000000-0001-4000-8000-000000000001', 'Pending', 'Canceled', 'Canceled', '6229', NULL, NULL, '2026-10-11 15:31:23'),
(18, 'd9abaf88-c588-11f1-9c65-d843ae47d0d6', 'f0000000-0005-4000-8000-000000000005', 'Pending', 'Canceled', 'Canceled', '6229', NULL, NULL, '2026-10-11 15:31:30'),
(19, '0e48ec47-c589-11f1-9c65-d843ae47d0d6', 'f0000000-0003-4000-8000-000000000003', 'Pending', 'Canceled', 'Canceled', '6229', NULL, NULL, '2026-10-11 15:32:58'),
(20, '11139fc6-c589-11f1-9c65-d843ae47d0d6', 'f0000000-0010-4000-8000-000000000010', 'Accepted', 'Canceled', 'Canceled', '6229', NULL, NULL, '2026-10-11 15:33:03'),
(21, 'bd86cb1d-c58a-11f1-9c65-d843ae47d0d6', '1a785e68-cd4a-4b09-9e60-fa7a04cd3b0e', NULL, NULL, 'Created', '6229', 'John Koll', 'Bag 40x48 Gaylord', '2026-10-11 15:45:02'),
(22, 'ca8ec65f-c58a-11f1-9c65-d843ae47d0d6', '0a0f160f-3320-4510-9dfb-c8068897f841', NULL, NULL, 'Created', '6229', 'John Koll', NULL, '2026-10-11 15:45:23'),
(23, '8d167057-c58b-11f1-9c65-d843ae47d0d6', '1a785e68-cd4a-4b09-9e60-fa7a04cd3b0e', 'Pending', 'Canceled', 'Canceled', '6229', NULL, NULL, '2026-10-11 15:50:50'),
(24, 'a1bac4a1-c58b-11f1-9c65-d843ae47d0d6', '0a0f160f-3320-4510-9dfb-c8068897f841', 'Pending', 'Accepted', 'Accepted', '6229', 'John Koll', 'Accepted this request.', '2026-10-11 15:51:24'),
(25, '3c5ed034-c58c-11f1-9c65-d843ae47d0d6', '99107b5a-8e65-4597-84a2-743590126a82', NULL, NULL, 'Created', '6229', 'John Koll', 'FGT0279-02', '2026-10-11 15:55:44'),
(26, '93145b7c-c58c-11f1-9c65-d843ae47d0d6', '171dece4-2358-49f6-b333-52c92e16693d', NULL, NULL, 'Created', '6229', 'John Koll', 'FGT0279-02', '2026-10-11 15:58:09'),
(27, 'cec045aa-c5cc-11f1-8232-d843ae47d0d6', 'f25ca85b-4def-4fd5-8f22-3d870f9c12b4', NULL, NULL, 'Created', '6229', 'John Koll', 'FGT0279-02', '2026-10-11 23:38:05'),
(28, 'd2acb505-c5cc-11f1-8232-d843ae47d0d6', '99107b5a-8e65-4597-84a2-743590126a82', 'Pending', 'Canceled', 'Canceled', '6229', NULL, NULL, '2026-10-11 23:38:12'),
(29, 'd409b9ec-c5cc-11f1-8232-d843ae47d0d6', '171dece4-2358-49f6-b333-52c92e16693d', 'Pending', 'Canceled', 'Canceled', '6229', NULL, NULL, '2026-10-11 23:38:14'),
(30, 'dcae6f32-c5cc-11f1-8232-d843ae47d0d6', 'f25ca85b-4def-4fd5-8f22-3d870f9c12b4', 'Pending', 'Accepted', 'Accepted', '6229', 'John Koll', 'Accepted this request.', '2026-10-11 23:38:29'),
(31, 'e279c7f8-c5cc-11f1-8232-d843ae47d0d6', '0a0f160f-3320-4510-9dfb-c8068897f841', 'Accepted', 'Pending', 'Released', '6229', 'John Koll', 'Released this request back to the open list.', '2026-10-11 23:38:38'),
(32, 'e3ed209b-c5cc-11f1-8232-d843ae47d0d6', 'f25ca85b-4def-4fd5-8f22-3d870f9c12b4', 'Accepted', 'Pending', 'Released', '6229', 'John Koll', 'Released this request back to the open list.', '2026-10-11 23:38:41'),
(33, 'e4e6778e-c5cc-11f1-8232-d843ae47d0d6', 'f25ca85b-4def-4fd5-8f22-3d870f9c12b4', 'Pending', 'Accepted', 'Accepted', '6229', 'John Koll', 'Accepted this request.', '2026-10-11 23:38:42'),
(34, 'e5c6ff80-c5cc-11f1-8232-d843ae47d0d6', 'f25ca85b-4def-4fd5-8f22-3d870f9c12b4', 'Accepted', 'Pending', 'Released', '6229', 'John Koll', 'Released this request back to the open list.', '2026-10-11 23:38:44'),
(35, 'e6355f12-c5cc-11f1-8232-d843ae47d0d6', 'f25ca85b-4def-4fd5-8f22-3d870f9c12b4', 'Pending', 'Accepted', 'Accepted', '6229', 'John Koll', 'Accepted this request.', '2026-10-11 23:38:45'),
(36, 'e7043a20-c5cc-11f1-8232-d843ae47d0d6', 'f25ca85b-4def-4fd5-8f22-3d870f9c12b4', 'Accepted', 'Pending', 'Released', '6229', 'John Koll', 'Released this request back to the open list.', '2026-10-11 23:38:46'),
(37, 'e773cb8a-c5cc-11f1-8232-d843ae47d0d6', 'f25ca85b-4def-4fd5-8f22-3d870f9c12b4', 'Pending', 'Accepted', 'Accepted', '6229', 'John Koll', 'Accepted this request.', '2026-10-11 23:38:47'),
(38, 'e7e24dc2-c5cc-11f1-8232-d843ae47d0d6', 'f25ca85b-4def-4fd5-8f22-3d870f9c12b4', 'Accepted', 'Pending', 'Released', '6229', 'John Koll', 'Released this request back to the open list.', '2026-10-11 23:38:47'),
(39, 'e84db73a-c5cc-11f1-8232-d843ae47d0d6', 'f25ca85b-4def-4fd5-8f22-3d870f9c12b4', 'Pending', 'Accepted', 'Accepted', '6229', 'John Koll', 'Accepted this request.', '2026-10-11 23:38:48'),
(40, 'f41e5361-c5cc-11f1-8232-d843ae47d0d6', '9ca5e8b3-2d60-477e-adde-53378f5b947a', NULL, NULL, 'Created', '6229', 'John Koll', NULL, '2026-10-11 23:39:08'),
(41, '3ed12bb1-c5cd-11f1-8232-d843ae47d0d6', 'f25ca85b-4def-4fd5-8f22-3d870f9c12b4', 'Accepted', 'Pending', 'Released', '6229', 'John Koll', 'Released this request back to the open list.', '2026-10-11 23:41:13'),
(42, '4618818f-c5cd-11f1-8232-d843ae47d0d6', 'f25ca85b-4def-4fd5-8f22-3d870f9c12b4', 'Pending', 'Accepted', 'Accepted', '6229', 'John Koll', 'Accepted this request.', '2026-10-11 23:41:26'),
(43, '46a856cb-c5cd-11f1-8232-d843ae47d0d6', 'f25ca85b-4def-4fd5-8f22-3d870f9c12b4', 'Accepted', 'Pending', 'Released', '6229', 'John Koll', 'Released this request back to the open list.', '2026-10-11 23:41:26'),
(44, '476cbaae-c5cd-11f1-8232-d843ae47d0d6', 'f25ca85b-4def-4fd5-8f22-3d870f9c12b4', 'Pending', 'Accepted', 'Accepted', '6229', 'John Koll', 'Accepted this request.', '2026-10-11 23:41:28'),
(45, '47ecf9a5-c5cd-11f1-8232-d843ae47d0d6', 'f25ca85b-4def-4fd5-8f22-3d870f9c12b4', 'Accepted', 'Pending', 'Released', '6229', 'John Koll', 'Released this request back to the open list.', '2026-10-11 23:41:29'),
(46, '4a3176b6-c5cd-11f1-8232-d843ae47d0d6', 'f25ca85b-4def-4fd5-8f22-3d870f9c12b4', 'Pending', 'Accepted', 'Accepted', '6229', 'John Koll', 'Accepted this request.', '2026-10-11 23:41:32'),
(47, '4aa7f276-c5cd-11f1-8232-d843ae47d0d6', 'f25ca85b-4def-4fd5-8f22-3d870f9c12b4', 'Accepted', 'Pending', 'Released', '6229', 'John Koll', 'Released this request back to the open list.', '2026-10-11 23:41:33'),
(48, '24c1f026-c5cf-11f1-8232-d843ae47d0d6', '9ca5e8b3-2d60-477e-adde-53378f5b947a', 'Pending', 'Accepted', 'Accepted', '6229', 'John Koll', 'Accepted this request.', '2026-10-11 23:54:49');

DROP TABLE IF EXISTS `waitlist_requests_queue`;
CREATE TABLE `waitlist_requests_queue` (
  `id` bigint(20) NOT NULL COMMENT 'Surrogate primary key.',
  `public_id` char(36) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Public UUID for the waitlist request.',
  `building` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Building the request belongs to.',
  `work_center` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Work center that raised the request.',
  `category` varchar(16) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'The Category the requester chose: Pickup, Deliver, Assist or Other.',
  `item` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'The Item the requester chose: one of the catalogued Item codes.',
  `input_value` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'The answer captured for an Item whose flow collects one.',
  `answers_json` json DEFAULT NULL COMMENT 'The answers a request captured beyond input_value, keyed by the label the Item declared; NULL when the request captured none (FR-073, FR-087).',
  `active_setup_job_id` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Active setup job associated with the request at submission time.',
  `work_center_name` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Work center that submitted the request.',
  `requester_employee_number` varchar(32) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Employee number of the requester.',
  `requester_employee_name` varchar(128) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Display name of the requester.',
  `status` varchar(32) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'Pending' COMMENT 'Lifecycle status, for example Pending, InProgress, Resolved, or Canceled.',
  `handler_status` varchar(32) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'The condition the assigned material handler reported: one of the four verbatim condition strings, or NULL when no condition is set. Deliberately separate from status, because the lifecycle word is still needed when no condition is set (FR-012, FR-070).',
  `requested_utc` datetime NOT NULL COMMENT 'UTC timestamp when the request was submitted.',
  `target_time_utc` datetime DEFAULT NULL COMMENT 'UTC target completion time when one is supplied.',
  `is_overdue` tinyint(1) NOT NULL DEFAULT '0' COMMENT 'True when the request passed its target time without resolution.',
  `assigned_material_handler` varchar(128) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Material handler assigned to fulfil the request.',
  `cancellation_reason` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Reason captured when the request is canceled.',
  `canceled_utc` datetime DEFAULT NULL COMMENT 'UTC timestamp when the request was canceled.',
  `canceled_by_employee_number` varchar(32) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Employee number of the person who canceled the request.',
  `note` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Optional handler/requester note on the request.',
  `accepted_utc` datetime DEFAULT NULL COMMENT 'UTC timestamp when a handler accepted/claimed the request.',
  `completed_utc` datetime DEFAULT NULL COMMENT 'UTC timestamp when the request was completed.',
  `released_utc` datetime DEFAULT NULL COMMENT 'UTC timestamp when a handler released the request back to open.',
  `created_utc` datetime NOT NULL COMMENT 'UTC timestamp when the row was created.',
  `updated_utc` datetime NOT NULL COMMENT 'UTC timestamp when the row was last updated.'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Submitted material-handling requests awaiting pickup, in progress, or resolved.';

TRUNCATE TABLE `waitlist_requests_queue`;
INSERT INTO `waitlist_requests_queue` (`id`, `public_id`, `building`, `work_center`, `category`, `item`, `input_value`, `answers_json`, `active_setup_job_id`, `work_center_name`, `requester_employee_number`, `requester_employee_name`, `status`, `handler_status`, `requested_utc`, `target_time_utc`, `is_overdue`, `assigned_material_handler`, `cancellation_reason`, `canceled_utc`, `canceled_by_employee_number`, `note`, `accepted_utc`, `completed_utc`, `released_utc`, `created_utc`, `updated_utc`) VALUES
(19, '1a785e68-cd4a-4b09-9e60-fa7a04cd3b0e', 'Expo Drive', '100-03', 'Deliver', 'deliver-dunnage', 'Bag 40x48 Gaylord', NULL, '100-03', '100-03', '6229', 'John Koll', 'Canceled', NULL, '2026-10-11 15:45:02', '2026-10-11 16:15:02', 0, NULL, NULL, '2026-10-11 15:50:50', '6229', NULL, NULL, NULL, NULL, '2026-10-11 15:45:02', '2026-10-11 15:50:50'),
(20, '0a0f160f-3320-4510-9dfb-c8068897f841', 'Expo Drive', '100-03', 'Pickup', 'pickup-wip', NULL, NULL, '100-03', '100-03', '6229', 'John Koll', 'Pending', NULL, '2026-10-11 15:45:24', '2026-10-11 16:00:24', 0, NULL, NULL, NULL, NULL, NULL, '2026-10-11 15:51:24', NULL, '2026-10-11 23:38:38', '2026-10-11 15:45:23', '2026-10-11 23:38:38'),
(21, '99107b5a-8e65-4597-84a2-743590126a82', 'Expo Drive', '100-03', 'Pickup', 'pickup-die', 'FGT0279-02', NULL, '100-03', '100-03', '6229', 'John Koll', 'Canceled', NULL, '2026-10-11 15:55:44', '2026-10-11 16:40:44', 0, NULL, NULL, '2026-10-11 23:38:12', '6229', NULL, NULL, NULL, NULL, '2026-10-11 15:55:44', '2026-10-11 23:38:12'),
(22, '171dece4-2358-49f6-b333-52c92e16693d', 'Expo Drive', '100-03', 'Deliver', 'deliver-die', 'FGT0279-02', NULL, '100-03', '100-03', '6229', 'John Koll', 'Canceled', NULL, '2026-10-11 15:58:09', '2026-10-11 16:43:09', 0, NULL, NULL, '2026-10-11 23:38:14', '6229', NULL, NULL, NULL, NULL, '2026-10-11 15:58:09', '2026-10-11 23:38:14'),
(23, 'f25ca85b-4def-4fd5-8f22-3d870f9c12b4', 'Expo Drive', '100-03', 'Pickup', 'pickup-die', 'FGT0279-02', '{\"Die Shop Direction\": \"true\"}', '100-03', '100-03', '6229', 'John Koll', 'Pending', NULL, '2026-10-11 23:38:06', '2026-10-12 00:23:06', 0, NULL, NULL, NULL, NULL, NULL, '2026-10-11 23:38:29', NULL, '2026-10-11 23:38:41', '2026-10-11 23:38:05', '2026-10-11 23:41:33'),
(24, '9ca5e8b3-2d60-477e-adde-53378f5b947a', 'Expo Drive', '100-03', 'Deliver', 'deliver-riser-table', NULL, NULL, '100-03', '100-03', '6229', 'John Koll', 'Accepted', NULL, '2026-10-11 23:39:08', '2026-10-11 23:59:08', 0, '6229', NULL, NULL, NULL, NULL, '2026-10-11 23:54:49', NULL, NULL, '2026-10-11 23:39:08', '2026-10-11 23:54:49');

DROP TABLE IF EXISTS `waitlist_request_item_configs`;
CREATE TABLE `waitlist_request_item_configs` (
  `id` bigint(20) NOT NULL COMMENT 'Surrogate primary key.',
  `public_id` char(36) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Public UUID for the configuration row.',
  `item` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Catalog item code (e.g. pickup-coil, deliver-wrong-flatstock, other). One row per Item.',
  `category` varchar(16) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'The Category the Item belongs to: Pickup, Deliver, Assist or Other.',
  `control_flow` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'direct-to-confirmation' COMMENT 'How far the flow goes before confirmation: direct-to-confirmation or collect-input-then-confirm.',
  `requires_answer` tinyint(1) NOT NULL DEFAULT '0' COMMENT 'Whether the Details step asks for an answer for this Item.',
  `answer_value_type` varchar(16) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'enum or text where an answer is required; NULL otherwise.',
  `prompt_text` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'The question shown where an answer is required.',
  `min_length` int(11) NOT NULL DEFAULT '0' COMMENT 'Minimum length of a text answer; ignored where none is required.',
  `max_length` int(11) NOT NULL DEFAULT '200' COMMENT 'Maximum length of a text answer; ignored where none is required.',
  `options_json` json DEFAULT NULL COMMENT 'Ordered options for an enumerated answer, where the options are fixed by configuration.',
  `detail_fields_json` json DEFAULT NULL COMMENT 'The ordered fields the Item page shows: label, value_type, source, order, is_required.',
  `allotted_minutes` int(11) DEFAULT NULL COMMENT 'The Item configured allotment in minutes. NULL means not configured, and the application then uses the labelled 15-minute default.',
  `updated_by_user_id` bigint(20) DEFAULT NULL COMMENT 'User who last changed this configuration row. Audit only; not part of the read contract.',
  `created_utc` datetime NOT NULL COMMENT 'UTC timestamp when the configuration row was created.',
  `updated_utc` datetime NOT NULL COMMENT 'UTC timestamp when the configuration row was last updated.'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='One row per catalogued request Item: flow, answer requirement, prompt, limits, options, page fields and allotted minutes. Behaviour as data.';

TRUNCATE TABLE `waitlist_request_item_configs`;
INSERT INTO `waitlist_request_item_configs` (`id`, `public_id`, `item`, `category`, `control_flow`, `requires_answer`, `answer_value_type`, `prompt_text`, `min_length`, `max_length`, `options_json`, `detail_fields_json`, `allotted_minutes`, `updated_by_user_id`, `created_utc`, `updated_utc`) VALUES
(1, 'c1000000-0000-4000-8000-000000000001', 'pickup-coil', 'Pickup', 'direct-to-confirmation', 0, NULL, NULL, 0, 200, NULL, '[{\"label\": \"Coil number\", \"order\": 1, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Quantity in house\", \"order\": 2, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Coil description\", \"order\": 3, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Average coil weight\", \"order\": 4, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Requesting work center\", \"order\": 5, \"source\": \"fixed\", \"value_type\": \"string\", \"is_required\": false}]', 30, NULL, '2026-10-11 16:08:45', '2026-10-11 16:08:45'),
(2, 'c1000000-0000-4000-8000-000000000002', 'pickup-die', 'Pickup', 'collect-input-then-confirm', 1, 'enum', 'Which die is this request for?', 0, 200, NULL, '[{\"list\": \"die\", \"label\": \"Die\", \"order\": 1, \"source\": \"answer\", \"value_type\": \"enum\", \"is_required\": true}, {\"label\": \"Die Location\", \"order\": 2, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Quantity\", \"order\": 3, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Die Shop Direction\", \"order\": 4, \"source\": \"answer\", \"value_type\": \"string\", \"is_required\": false}]', 45, NULL, '2026-10-11 16:08:45', '2026-10-11 16:08:45'),
(3, 'c1000000-0000-4000-8000-000000000003', 'pickup-component', 'Pickup', 'collect-input-then-confirm', 1, 'enum', 'Choose the component to collect.', 0, 200, NULL, '[{\"label\": \"Component\", \"order\": 1, \"source\": \"answer\", \"value_type\": \"enum\", \"is_required\": true}, {\"label\": \"Part description\", \"order\": 2, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Quantity\", \"order\": 3, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Requesting work center\", \"order\": 4, \"source\": \"fixed\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Total Qty In House\", \"order\": 5, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Item Type\", \"order\": 6, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}]', 30, NULL, '2026-10-11 16:08:45', '2026-10-11 16:08:45'),
(4, 'c1000000-0000-4000-8000-000000000004', 'pickup-fg', 'Pickup', 'direct-to-confirmation', 0, NULL, NULL, 0, 200, NULL, '[{\"label\": \"Part number\", \"order\": 1, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Quantity remaining\", \"order\": 2, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Part description\", \"order\": 3, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Customer\", \"order\": 4, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Packlist\", \"order\": 5, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Part Type\", \"order\": 6, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"# of Skids\", \"order\": 7, \"source\": \"answer\", \"value_type\": \"string\", \"is_required\": false}]', NULL, NULL, '2026-10-11 16:08:45', '2026-10-11 16:08:45'),
(5, 'c1000000-0000-4000-8000-000000000005', 'pickup-ncm', 'Pickup', 'direct-to-confirmation', 0, NULL, NULL, 0, 200, NULL, '[{\"label\": \"Part\", \"order\": 1, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Quantity to move\", \"order\": 2, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Pickup location\", \"order\": 3, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Destination\", \"order\": 4, \"source\": \"fixed\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Traceability ID\", \"order\": 5, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Part Type\", \"order\": 6, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"# of Skids\", \"order\": 7, \"source\": \"answer\", \"value_type\": \"string\", \"is_required\": false}]', NULL, NULL, '2026-10-11 16:08:45', '2026-10-11 16:08:45'),
(6, 'c1000000-0000-4000-8000-000000000006', 'pickup-wip', 'Pickup', 'direct-to-confirmation', 0, NULL, NULL, 0, 200, NULL, '[{\"label\": \"Part and quantity\", \"order\": 1, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Pickup work center\", \"order\": 2, \"source\": \"fixed\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"WIP destination\", \"order\": 3, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Operation sequence\", \"order\": 4, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Part Type\", \"order\": 5, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"# of Skids\", \"order\": 6, \"source\": \"answer\", \"value_type\": \"string\", \"is_required\": false}]', NULL, NULL, '2026-10-11 16:08:45', '2026-10-11 16:08:45'),
(7, 'c1000000-0000-4000-8000-000000000007', 'pickup-outside-service', 'Pickup', 'direct-to-confirmation', 0, NULL, NULL, 0, 200, NULL, '[{\"label\": \"Part\", \"order\": 1, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Quantity to move\", \"order\": 2, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Pickup location\", \"order\": 3, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Destination\", \"order\": 4, \"source\": \"fixed\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Traceability ID\", \"order\": 5, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Part Type\", \"order\": 6, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"# of Skids\", \"order\": 7, \"source\": \"answer\", \"value_type\": \"string\", \"is_required\": false}]', NULL, NULL, '2026-10-11 16:08:45', '2026-10-11 16:08:45'),
(8, 'c1000000-0000-4000-8000-000000000008', 'pickup-riser-table', 'Pickup', 'direct-to-confirmation', 0, NULL, NULL, 0, 200, NULL, '[{\"label\": \"Requesting work center\", \"order\": 1, \"source\": \"fixed\", \"value_type\": \"string\", \"is_required\": false}]', 20, NULL, '2026-10-11 16:08:45', '2026-10-11 16:08:45'),
(9, 'c1000000-0000-4000-8000-000000000009', 'pickup-dunnage', 'Pickup', 'collect-input-then-confirm', 1, 'enum', 'Which dunnage do you need from the material handlers?', 0, 200, NULL, '[{\"list\": \"dunnage\", \"label\": \"Dunnage part\", \"order\": 1, \"source\": \"answer\", \"value_type\": \"enum\", \"is_required\": true}, {\"label\": \"Description\", \"order\": 2, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Home Location\", \"order\": 3, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Quantity on hand\", \"order\": 4, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}]', 30, NULL, '2026-10-11 16:08:45', '2026-10-11 16:08:45'),
(10, 'c1000000-0000-4000-8000-000000000010', 'pickup-scrap', 'Pickup', 'direct-to-confirmation', 0, NULL, NULL, 0, 200, NULL, '[{\"label\": \"Scrap Type\", \"order\": 1, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Quantity involved\", \"order\": 2, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Scrap lugger\", \"order\": 3, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Pickup work center\", \"order\": 4, \"source\": \"fixed\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Lugger Location\", \"order\": 5, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}]', 30, NULL, '2026-10-11 16:08:45', '2026-10-11 16:08:45'),
(11, 'c1000000-0000-4000-8000-000000000011', 'pickup-hopper', 'Pickup', 'direct-to-confirmation', 0, NULL, NULL, 0, 200, NULL, '[{\"label\": \"Pickup work center\", \"order\": 1, \"source\": \"fixed\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Size\", \"order\": 2, \"source\": \"answer\", \"value_type\": \"enum\", \"is_required\": true}]', 20, NULL, '2026-10-11 16:08:45', '2026-10-11 16:08:45'),
(12, 'c1000000-0000-4000-8000-000000000012', 'deliver-coil', 'Deliver', 'direct-to-confirmation', 0, NULL, NULL, 0, 200, NULL, '[{\"label\": \"Coil number\", \"order\": 1, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Quantity in house\", \"order\": 2, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Coil description\", \"order\": 3, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Average coil weight\", \"order\": 4, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Requesting work center\", \"order\": 5, \"source\": \"fixed\", \"value_type\": \"string\", \"is_required\": false}]', 30, NULL, '2026-10-11 16:08:45', '2026-10-11 16:08:45'),
(13, 'c1000000-0000-4000-8000-000000000013', 'deliver-riser-table', 'Deliver', 'direct-to-confirmation', 0, NULL, NULL, 0, 200, NULL, '[{\"label\": \"Requesting work center\", \"order\": 1, \"source\": \"fixed\", \"value_type\": \"string\", \"is_required\": false}]', 20, NULL, '2026-10-11 16:08:45', '2026-10-11 16:08:45'),
(14, 'c1000000-0000-4000-8000-000000000014', 'deliver-hopper', 'Deliver', 'direct-to-confirmation', 0, NULL, NULL, 0, 200, NULL, '[{\"label\": \"Requesting work center\", \"order\": 1, \"source\": \"fixed\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Size\", \"order\": 2, \"source\": \"answer\", \"value_type\": \"enum\", \"is_required\": true}]', 20, NULL, '2026-10-11 16:08:45', '2026-10-11 16:08:45'),
(15, 'c1000000-0000-4000-8000-000000000015', 'deliver-flatstock', 'Deliver', 'direct-to-confirmation', 0, NULL, NULL, 0, 200, NULL, '[{\"label\": \"Part\", \"order\": 1, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Quantity\", \"order\": 2, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Work center\", \"order\": 3, \"source\": \"fixed\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Destination\", \"order\": 4, \"source\": \"fixed\", \"value_type\": \"string\", \"is_required\": false}]', 30, NULL, '2026-10-11 16:08:45', '2026-10-11 16:08:45'),
(16, 'c1000000-0000-4000-8000-000000000024', 'deliver-component', 'Deliver', 'collect-input-then-confirm', 1, 'enum', 'Choose the component to bring.', 0, 200, NULL, '[{\"list\": \"component\", \"label\": \"Component\", \"order\": 1, \"source\": \"answer\", \"value_type\": \"enum\", \"is_required\": true}, {\"label\": \"Part description\", \"order\": 2, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Quantity\", \"order\": 3, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Requesting work center\", \"order\": 4, \"source\": \"fixed\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Total Qty In House\", \"order\": 5, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Item Type\", \"order\": 6, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}]', 30, NULL, '2026-10-11 16:08:45', '2026-10-11 16:08:45'),
(17, 'c1000000-0000-4000-8000-000000000016', 'deliver-die', 'Deliver', 'collect-input-then-confirm', 1, 'enum', 'Which die is this request for?', 0, 200, NULL, '[{\"list\": \"die\", \"label\": \"Die\", \"order\": 1, \"source\": \"answer\", \"value_type\": \"enum\", \"is_required\": true}, {\"label\": \"Die Location\", \"order\": 2, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Quantity\", \"order\": 3, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Die Shop Direction\", \"order\": 4, \"source\": \"answer\", \"value_type\": \"string\", \"is_required\": false}]', 45, NULL, '2026-10-11 16:08:45', '2026-10-11 16:08:45'),
(18, 'c1000000-0000-4000-8000-000000000017', 'deliver-dunnage', 'Deliver', 'collect-input-then-confirm', 1, 'enum', 'Which dunnage do you need from the material handlers?', 0, 200, NULL, '[{\"list\": \"dunnage\", \"label\": \"Dunnage part\", \"order\": 1, \"source\": \"answer\", \"value_type\": \"enum\", \"is_required\": true}, {\"label\": \"Description\", \"order\": 2, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Home Location\", \"order\": 3, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Quantity on hand\", \"order\": 4, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}]', 30, NULL, '2026-10-11 16:08:45', '2026-10-11 16:08:45'),
(19, 'c1000000-0000-4000-8000-000000000018', 'deliver-wrong-coil', 'Deliver', 'collect-input-then-confirm', 1, 'text', 'Explain how or why the coil is wrong', 5, 200, NULL, '[{\"label\": \"Coil number\", \"order\": 1, \"source\": \"answer\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Reason\", \"order\": 2, \"source\": \"answer\", \"value_type\": \"text\", \"is_required\": true}, {\"label\": \"Correct coil\", \"order\": 3, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}]', 30, NULL, '2026-10-11 16:08:45', '2026-10-11 16:08:45'),
(20, 'c1000000-0000-4000-8000-000000000019', 'deliver-wrong-flatstock', 'Deliver', 'collect-input-then-confirm', 1, 'text', 'Explain how or why the flatstock is wrong', 5, 200, NULL, '[{\"label\": \"Part\", \"order\": 1, \"source\": \"answer\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Reason\", \"order\": 2, \"source\": \"answer\", \"value_type\": \"text\", \"is_required\": true}, {\"label\": \"Correct flatstock\", \"order\": 3, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}]', 30, NULL, '2026-10-11 16:08:45', '2026-10-11 16:08:45'),
(21, 'c1000000-0000-4000-8000-000000000020', 'assist-coil-turn', 'Assist', 'direct-to-confirmation', 0, NULL, NULL, 0, 200, NULL, '[{\"label\": \"Coil Number\", \"order\": 1, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Quantity in house\", \"order\": 2, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Weight of Coil\", \"order\": 3, \"source\": \"answer\", \"value_type\": \"string\", \"is_required\": true}]', 30, NULL, '2026-10-11 16:08:45', '2026-10-11 16:08:45'),
(22, 'c1000000-0000-4000-8000-000000000021', 'assist-table-place', 'Assist', 'direct-to-confirmation', 0, NULL, NULL, 0, 200, NULL, '[{\"label\": \"Part\", \"order\": 1, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Quantity\", \"order\": 2, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Work center\", \"order\": 3, \"source\": \"fixed\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Destination\", \"order\": 4, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}]', 30, NULL, '2026-10-11 16:08:45', '2026-10-11 16:08:45'),
(23, 'c1000000-0000-4000-8000-000000000022', 'assist-table-remove', 'Assist', 'direct-to-confirmation', 0, NULL, NULL, 0, 200, NULL, '[{\"label\": \"Part\", \"order\": 1, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Quantity\", \"order\": 2, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Work center\", \"order\": 3, \"source\": \"fixed\", \"value_type\": \"string\", \"is_required\": false}, {\"label\": \"Destination\", \"order\": 4, \"source\": \"job\", \"value_type\": \"string\", \"is_required\": false}]', NULL, NULL, '2026-10-11 16:08:45', '2026-10-11 16:08:45'),
(24, 'c1000000-0000-4000-8000-000000000023', 'other', 'Other', 'collect-input-then-confirm', 1, 'text', 'Enter a short description', 5, 200, NULL, '[{\"label\": \"Request description\", \"order\": 1, \"source\": \"answer\", \"value_type\": \"text\", \"is_required\": true}, {\"label\": \"Requested work center\", \"order\": 2, \"source\": \"fixed\", \"value_type\": \"string\", \"is_required\": false}]', 15, NULL, '2026-10-11 16:08:45', '2026-10-11 16:08:45');


ALTER TABLE `auth_roles_assignments`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_auth_roles_assignments_public_id` (`public_id`),
  ADD UNIQUE KEY `uq_auth_roles_assignments_user_role` (`user_id`,`role_id`),
  ADD KEY `idx_auth_roles_assignments_role_id` (`role_id`),
  ADD KEY `idx_auth_roles_assignments_assigned_by_user_id` (`assigned_by_user_id`);

ALTER TABLE `auth_roles_catalog`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_auth_roles_catalog_public_id` (`public_id`),
  ADD UNIQUE KEY `uq_auth_roles_catalog_role_code` (`role_code`),
  ADD KEY `idx_auth_roles_catalog_role_rank` (`role_rank`);

ALTER TABLE `auth_user_management_audit`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_auth_user_management_audit_public_id` (`public_id`),
  ADD KEY `idx_auth_user_management_audit_change_group_id` (`change_group_id`),
  ADD KEY `idx_audit_target_user_id_occurred_utc` (`target_user_id`,`occurred_utc`);

ALTER TABLE `config_computer_hot_work_centers`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_config_computer_hot_work_centers_public_id` (`public_id`),
  ADD UNIQUE KEY `uq_config_hot_work_centers_computer_work_center` (`computer_id`,`work_center_id`),
  ADD KEY `idx_config_hot_work_centers_computer_active_sort` (`computer_id`,`is_active`,`sort_rank`),
  ADD KEY `fk_config_hot_work_centers_work_center_id` (`work_center_id`);

ALTER TABLE `config_dunnage_types_visibility`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_config_dunnage_types_visibility_public_id` (`public_id`),
  ADD UNIQUE KEY `uq_config_dunnage_types_visibility_dunnage_type_id` (`dunnage_type_id`),
  ADD KEY `idx_config_dunnage_types_visibility_is_visible` (`is_visible`),
  ADD KEY `idx_config_dunnage_types_visibility_updated_utc` (`updated_utc`);

ALTER TABLE `config_images_locations`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_config_images_locations_public_id` (`public_id`),
  ADD UNIQUE KEY `uq_config_images_locations_scope_item` (`scope`,`scope_item_id`) COMMENT 'Ensure only one active override per scope/item pair',
  ADD KEY `idx_config_images_locations_scope_active` (`scope`,`is_active`) COMMENT 'Composite index for scope queries with active filter',
  ADD KEY `idx_config_images_locations_computer_id` (`computer_id`) COMMENT 'Read the six rows of one machine without scanning the scope',
  ADD KEY `idx_config_images_locations_created_by_user_id` (`created_by_user_id`),
  ADD KEY `idx_config_images_locations_updated_by_user_id` (`updated_by_user_id`);

ALTER TABLE `config_images_locations_history`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_config_images_locations_history_public_id` (`public_id`),
  ADD KEY `idx_config_images_locations_history_scope_item` (`scope`,`scope_item_id`,`changed_utc`) COMMENT 'Read one part''s record newest first',
  ADD KEY `idx_config_images_locations_history_image_location_id` (`image_location_id`),
  ADD KEY `idx_config_images_locations_history_changed_by_user_id` (`changed_by_user_id`);

ALTER TABLE `config_settings_history`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_config_settings_history_public_id` (`public_id`),
  ADD KEY `idx_config_settings_history_config_setting_id_changed_utc` (`config_setting_id`,`changed_utc`),
  ADD KEY `idx_config_settings_history_changed_by_user_id` (`changed_by_user_id`),
  ADD KEY `idx_config_settings_history_setting_scope_changed_utc` (`setting_key`,`scope_key`,`changed_utc`),
  ADD KEY `fk_config_settings_history_core_computers_registry_computer_id` (`computer_id`),
  ADD KEY `fk_history_users_user_id` (`user_id`);

ALTER TABLE `config_settings_values`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_config_settings_values_public_id` (`public_id`),
  ADD UNIQUE KEY `uq_config_settings_values_setting_scope` (`setting_key`,`scope_key`),
  ADD KEY `idx_config_settings_values_updated_by_user_id` (`updated_by_user_id`),
  ADD KEY `idx_config_settings_values_computer_id` (`computer_id`),
  ADD KEY `idx_config_settings_values_user_id` (`user_id`);

ALTER TABLE `core_computers_registry`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_core_computers_registry_public_id` (`public_id`),
  ADD UNIQUE KEY `uq_core_computers_registry_computer_mac_address` (`computer_name`,`mac_address_normalized`),
  ADD UNIQUE KEY `uq_core_computers_registry_display_name` (`display_name`);

ALTER TABLE `core_users_profiles`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_core_users_profiles_public_id` (`public_id`),
  ADD UNIQUE KEY `uq_core_users_profiles_username_normalized` (`username_normalized`);

ALTER TABLE `ops_startup_logs`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_ops_startup_logs_public_id` (`public_id`),
  ADD KEY `idx_ops_startup_logs_created_utc` (`created_utc`),
  ADD KEY `idx_ops_startup_logs_correlation_id` (`correlation_id`),
  ADD KEY `idx_ops_startup_logs_level_created_utc` (`level`,`created_utc`) COMMENT 'Panel filter: severity',
  ADD KEY `idx_ops_startup_logs_host_id_created_utc` (`host_id`,`created_utc`) COMMENT 'Panel filter: machine',
  ADD KEY `idx_ops_startup_logs_module_created_utc` (`module`,`created_utc`) COMMENT 'Panel filter: module',
  ADD KEY `idx_ops_startup_logs_actor_id_created_utc` (`actor_id`,`created_utc`) COMMENT 'Panel filter: person',
  ADD KEY `idx_ops_startup_logs_error_type_created_utc` (`error_type`,`created_utc`) COMMENT 'Panel filter: fault type',
  ADD KEY `idx_ops_startup_logs_error_fingerprint_created_utc` (`error_fingerprint`,`created_utc`) COMMENT 'Panel grouping and filter: fault fingerprint';

ALTER TABLE `seed_username_case_backup`
  ADD PRIMARY KEY (`user_id`);

ALTER TABLE `setup_active_jobs`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_setup_active_jobs_public_id` (`public_id`),
  ADD UNIQUE KEY `uq_setup_active_jobs_work_center` (`work_center`),
  ADD KEY `idx_setup_active_jobs_work_order` (`work_order`),
  ADD KEY `idx_setup_active_jobs_updated_utc` (`updated_utc`);

ALTER TABLE `setup_job_history`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_setup_job_history_public_id` (`public_id`),
  ADD KEY `idx_setup_job_history_active_job_id` (`active_job_id`),
  ADD KEY `idx_setup_job_history_work_order` (`work_order`),
  ADD KEY `idx_setup_job_history_changed_utc` (`changed_utc`);

ALTER TABLE `setup_part_sequence_custom_data`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_setup_part_sequence_custom_data_public_id` (`public_id`),
  ADD UNIQUE KEY `uq_setup_part_sequence_custom_data_part_sequence` (`part_number`,`sequence_number`),
  ADD KEY `idx_setup_part_sequence_custom_data_updated_utc` (`updated_utc`);

ALTER TABLE `setup_work_centers_catalog`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_setup_work_centers_catalog_public_id` (`public_id`),
  ADD UNIQUE KEY `uq_setup_work_centers_catalog_building_work_center_name` (`building`,`work_center_name`),
  ADD KEY `idx_setup_work_centers_catalog_is_active` (`is_active`),
  ADD KEY `idx_setup_work_centers_catalog_sort_rank` (`sort_rank`);

ALTER TABLE `user_active_sessions`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_user_active_sessions_public_id` (`public_id`),
  ADD UNIQUE KEY `uq_user_active_sessions_user_computer` (`user_id`,`computer_id`) COMMENT 'One active row per person per machine',
  ADD KEY `idx_user_active_sessions_is_active_expires_utc` (`is_active`,`expires_utc`) COMMENT 'Live-session reads filter on this pair',
  ADD KEY `idx_user_active_sessions_computer_id` (`computer_id`);

ALTER TABLE `waitlist_defect_types`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_waitlist_defect_types_public_id` (`public_id`),
  ADD UNIQUE KEY `uq_waitlist_defect_types_defect_name` (`defect_name`),
  ADD KEY `idx_waitlist_defect_types_is_active` (`is_active`);

ALTER TABLE `waitlist_requests_audit`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_waitlist_requests_audit_public_id` (`public_id`),
  ADD KEY `idx_waitlist_requests_audit_request_public_id` (`request_public_id`),
  ADD KEY `idx_waitlist_requests_audit_occurred_utc` (`occurred_utc`);

ALTER TABLE `waitlist_requests_queue`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_waitlist_requests_queue_public_id` (`public_id`),
  ADD KEY `idx_waitlist_requests_queue_building_status` (`building`,`status`),
  ADD KEY `idx_waitlist_requests_queue_work_center` (`work_center`),
  ADD KEY `idx_waitlist_requests_queue_requested_utc` (`requested_utc`),
  ADD KEY `idx_waitlist_requests_queue_status` (`status`),
  ADD KEY `idx_waitlist_requests_queue_item_status` (`item`,`status`);

ALTER TABLE `waitlist_request_item_configs`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_waitlist_request_item_configs_public_id` (`public_id`),
  ADD UNIQUE KEY `uq_waitlist_request_item_configs_item` (`item`),
  ADD KEY `idx_waitlist_request_item_configs_category` (`category`),
  ADD KEY `fk_waitlist_request_item_configs_updated_by_user_id` (`updated_by_user_id`);


ALTER TABLE `auth_roles_assignments`
  MODIFY `id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.', AUTO_INCREMENT=11;

ALTER TABLE `auth_roles_catalog`
  MODIFY `id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.', AUTO_INCREMENT=11;

ALTER TABLE `auth_user_management_audit`
  MODIFY `id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.';

ALTER TABLE `config_computer_hot_work_centers`
  MODIFY `id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.';

ALTER TABLE `config_dunnage_types_visibility`
  MODIFY `id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.';

ALTER TABLE `config_images_locations`
  MODIFY `id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.';

ALTER TABLE `config_images_locations_history`
  MODIFY `id` bigint(20) NOT NULL AUTO_INCREMENT;

ALTER TABLE `config_settings_history`
  MODIFY `id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.';

ALTER TABLE `config_settings_values`
  MODIFY `id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.', AUTO_INCREMENT=178;

ALTER TABLE `core_computers_registry`
  MODIFY `id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.', AUTO_INCREMENT=3;

ALTER TABLE `core_users_profiles`
  MODIFY `id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.', AUTO_INCREMENT=11;

ALTER TABLE `ops_startup_logs`
  MODIFY `id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.', AUTO_INCREMENT=131;

ALTER TABLE `setup_active_jobs`
  MODIFY `id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.', AUTO_INCREMENT=8;

ALTER TABLE `setup_job_history`
  MODIFY `id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.', AUTO_INCREMENT=2;

ALTER TABLE `setup_part_sequence_custom_data`
  MODIFY `id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.', AUTO_INCREMENT=2;

ALTER TABLE `setup_work_centers_catalog`
  MODIFY `id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.', AUTO_INCREMENT=26;

ALTER TABLE `user_active_sessions`
  MODIFY `id` bigint(20) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=2;

ALTER TABLE `waitlist_defect_types`
  MODIFY `id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.';

ALTER TABLE `waitlist_requests_audit`
  MODIFY `id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.', AUTO_INCREMENT=49;

ALTER TABLE `waitlist_requests_queue`
  MODIFY `id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.', AUTO_INCREMENT=25;

ALTER TABLE `waitlist_request_item_configs`
  MODIFY `id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.', AUTO_INCREMENT=25;
DROP TABLE IF EXISTS `vw_config_settings_scope_catalog`;

DROP VIEW IF EXISTS `vw_config_settings_scope_catalog`;
CREATE ALGORITHM=UNDEFINED DEFINER=`root`@`localhost` SQL SECURITY DEFINER VIEW `vw_config_settings_scope_catalog`  AS SELECT `config_settings_values`.`setting_key` AS `setting_key`, `config_settings_values`.`scope_type` AS `scope_type`, `config_settings_values`.`scope_key` AS `scope_key`, `config_settings_values`.`computer_id` AS `computer_id`, `config_settings_values`.`user_id` AS `user_id`, `fn_config_settings_scope_rank`(`config_settings_values`.`scope_type`) AS `scope_rank`, `config_settings_values`.`setting_value` AS `setting_value`, `config_settings_values`.`setting_value_int` AS `setting_value_int`, `config_settings_values`.`setting_value_bool` AS `setting_value_bool`, `config_settings_values`.`setting_value_decimal` AS `setting_value_decimal`, `config_settings_values`.`setting_value_datetime_utc` AS `setting_value_datetime_utc`, `config_settings_values`.`value_type` AS `value_type`, `config_settings_values`.`updated_by_user_id` AS `updated_by_user_id`, `config_settings_values`.`updated_utc` AS `updated_utc` FROM `config_settings_values` ;
DROP TABLE IF EXISTS `vw_setup_work_centers_active`;

DROP VIEW IF EXISTS `vw_setup_work_centers_active`;
CREATE ALGORITHM=UNDEFINED DEFINER=`root`@`localhost` SQL SECURITY DEFINER VIEW `vw_setup_work_centers_active`  AS SELECT `setup_work_centers_catalog`.`id` AS `id`, `setup_work_centers_catalog`.`public_id` AS `public_id`, `setup_work_centers_catalog`.`building` AS `building`, `setup_work_centers_catalog`.`work_center_name` AS `work_center_name`, `setup_work_centers_catalog`.`is_active` AS `is_active`, `setup_work_centers_catalog`.`sort_rank` AS `sort_rank`, `setup_work_centers_catalog`.`created_utc` AS `created_utc`, `setup_work_centers_catalog`.`updated_utc` AS `updated_utc` FROM `setup_work_centers_catalog` WHERE (`setup_work_centers_catalog`.`is_active` = 1) ;


ALTER TABLE `auth_roles_assignments`
  ADD CONSTRAINT `fk_auth_roles_assignments_auth_roles_catalog_role_id` FOREIGN KEY (`role_id`) REFERENCES `auth_roles_catalog` (`id`),
  ADD CONSTRAINT `fk_auth_roles_assignments_core_users_profiles_user_id` FOREIGN KEY (`user_id`) REFERENCES `core_users_profiles` (`id`),
  ADD CONSTRAINT `fk_roles_assignments_users_assigned_by_user_id` FOREIGN KEY (`assigned_by_user_id`) REFERENCES `core_users_profiles` (`id`);

ALTER TABLE `config_computer_hot_work_centers`
  ADD CONSTRAINT `fk_config_hot_work_centers_computer_id` FOREIGN KEY (`computer_id`) REFERENCES `core_computers_registry` (`id`),
  ADD CONSTRAINT `fk_config_hot_work_centers_work_center_id` FOREIGN KEY (`work_center_id`) REFERENCES `setup_work_centers_catalog` (`id`);

ALTER TABLE `config_images_locations`
  ADD CONSTRAINT `fk_config_images_locations_core_computers_registry_computer_id` FOREIGN KEY (`computer_id`) REFERENCES `core_computers_registry` (`id`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_config_images_locations_created_by_user_id` FOREIGN KEY (`created_by_user_id`) REFERENCES `core_users_profiles` (`id`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_config_images_locations_updated_by_user_id` FOREIGN KEY (`updated_by_user_id`) REFERENCES `core_users_profiles` (`id`) ON DELETE SET NULL;

ALTER TABLE `config_images_locations_history`
  ADD CONSTRAINT `fk_config_images_locations_history_changed_by_user_id` FOREIGN KEY (`changed_by_user_id`) REFERENCES `core_users_profiles` (`id`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_config_images_locations_history_image_location_id` FOREIGN KEY (`image_location_id`) REFERENCES `config_images_locations` (`id`) ON DELETE SET NULL;

ALTER TABLE `config_settings_history`
  ADD CONSTRAINT `fk_config_settings_history_core_computers_registry_computer_id` FOREIGN KEY (`computer_id`) REFERENCES `core_computers_registry` (`id`),
  ADD CONSTRAINT `fk_history_users_user_id` FOREIGN KEY (`user_id`) REFERENCES `core_users_profiles` (`id`),
  ADD CONSTRAINT `fk_settings_history_settings_values_config_setting_id` FOREIGN KEY (`config_setting_id`) REFERENCES `config_settings_values` (`id`),
  ADD CONSTRAINT `fk_settings_history_users_changed_by_user_id` FOREIGN KEY (`changed_by_user_id`) REFERENCES `core_users_profiles` (`id`);

ALTER TABLE `config_settings_values`
  ADD CONSTRAINT `fk_config_settings_values_core_computers_registry_computer_id` FOREIGN KEY (`computer_id`) REFERENCES `core_computers_registry` (`id`),
  ADD CONSTRAINT `fk_values_users_updated_by_user_id` FOREIGN KEY (`updated_by_user_id`) REFERENCES `core_users_profiles` (`id`),
  ADD CONSTRAINT `fk_values_users_user_id` FOREIGN KEY (`user_id`) REFERENCES `core_users_profiles` (`id`);

ALTER TABLE `setup_job_history`
  ADD CONSTRAINT `fk_setup_job_history_setup_active_jobs_active_job_id` FOREIGN KEY (`active_job_id`) REFERENCES `setup_active_jobs` (`id`) ON DELETE SET NULL;

ALTER TABLE `user_active_sessions`
  ADD CONSTRAINT `fk_user_active_sessions_core_computers_registry_computer_id` FOREIGN KEY (`computer_id`) REFERENCES `core_computers_registry` (`id`),
  ADD CONSTRAINT `fk_user_active_sessions_core_users_profiles_user_id` FOREIGN KEY (`user_id`) REFERENCES `core_users_profiles` (`id`);

ALTER TABLE `waitlist_request_item_configs`
  ADD CONSTRAINT `fk_waitlist_request_item_configs_updated_by_user_id` FOREIGN KEY (`updated_by_user_id`) REFERENCES `core_users_profiles` (`id`) ON DELETE SET NULL;

SET FOREIGN_KEY_CHECKS=1;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
