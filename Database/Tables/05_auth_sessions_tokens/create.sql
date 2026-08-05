-- Create table: auth_sessions_tokens
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS auth_sessions_tokens (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    user_id BIGINT NOT NULL,
    workstation_id BIGINT NULL,
    token_hash CHAR(64) NOT NULL,
    token_salt VARBINARY(32) NOT NULL,
    token_version SMALLINT NOT NULL DEFAULT 1,
    issued_utc DATETIME NOT NULL,
    expires_utc DATETIME NOT NULL,
    revoked_utc DATETIME NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    source_label VARCHAR(32) NOT NULL,
    created_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_auth_sessions_tokens_public_id (public_id),
    KEY idx_auth_sessions_tokens_user_id_expires_utc (user_id, expires_utc),
    KEY idx_auth_sessions_tokens_workstation_id_expires_utc (workstation_id, expires_utc),
    KEY idx_auth_sessions_tokens_is_active_expires_utc (is_active, expires_utc),
    CONSTRAINT fk_auth_sessions_tokens_core_users_profiles_user_id FOREIGN KEY (user_id) REFERENCES core_users_profiles (id),
    CONSTRAINT fk_sessions_tokens_workstations_workstation_id FOREIGN KEY (workstation_id) REFERENCES core_workstations_registry (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;