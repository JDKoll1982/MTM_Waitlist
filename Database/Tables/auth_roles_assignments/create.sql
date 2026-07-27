-- Create table: auth_roles_assignments
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS auth_roles_assignments (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    user_id BIGINT NOT NULL,
    role_id BIGINT NOT NULL,
    assigned_utc DATETIME NOT NULL,
    assigned_by_user_id BIGINT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_auth_roles_assignments_public_id (public_id),
    UNIQUE KEY uq_auth_roles_assignments_user_role (user_id, role_id),
    KEY idx_auth_roles_assignments_role_id (role_id),
    KEY idx_auth_roles_assignments_assigned_by_user_id (assigned_by_user_id),
    CONSTRAINT fk_auth_roles_assignments_core_users_profiles_user_id FOREIGN KEY (user_id) REFERENCES core_users_profiles (id),
    CONSTRAINT fk_auth_roles_assignments_auth_roles_catalog_role_id FOREIGN KEY (role_id) REFERENCES auth_roles_catalog (id),
    CONSTRAINT fk_roles_assignments_users_assigned_by_user_id FOREIGN KEY (assigned_by_user_id) REFERENCES core_users_profiles (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;