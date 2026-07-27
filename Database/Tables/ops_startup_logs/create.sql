-- Create table: ops_startup_logs
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS ops_startup_logs (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    correlation_id CHAR(36) NOT NULL,
    created_utc DATETIME NOT NULL,
    level VARCHAR(16) NOT NULL,
    event_action VARCHAR(128) NOT NULL,
    outcome VARCHAR(32) NOT NULL,
    actor_kind VARCHAR(32) NULL,
    actor_id VARCHAR(128) NULL,
    host_id VARCHAR(128) NULL,
    mac_address VARCHAR(64) NULL,
    message TEXT NOT NULL,
    payload_json MEDIUMTEXT NULL,
    previous_hash CHAR(64) NULL,
    entry_hash CHAR(64) NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_ops_startup_logs_public_id (public_id),
    KEY idx_ops_startup_logs_created_utc (created_utc),
    KEY idx_ops_startup_logs_correlation_id (correlation_id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;