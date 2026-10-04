-- Create table: ops_startup_logs
-- Engine: MySQL 5.7
-- Feature: the log store is the existing one; 010-startup-rebuild (task T047) widened it, and this file is that
--          widened shape rather than a second table.
--
-- This is the one log store. The launch pipeline, the shell, Settings and the module libraries all write to it
-- through `sp_ops_startup_logs_insert`, which is its only writer, and read it through the filter and group
-- procedures. Nothing on the machine records a substitute entry when the store refuses a write (FR-025).
--
-- ============================================================
-- 010-startup-rebuild (task T047, plan D19): four columns and six indexes
-- ============================================================
-- Four columns were added for the developer panel, and each answers a question the previous shape could not:
--
--   * `module` names the part of the application an entry came from, so the panel can filter to one module
--     instead of reading every entry. It is also where an `ILogger` category name lands, which is why it is
--     128 characters and not 64: a category is a namespace-qualified type name, the longest one in this tree is
--     77 characters, and at 64 the store refused every entry from it with "Data too long for column 'p_module'"
--     (found by hand on 2026-09-28).
--   * `error_type` names the fault's type, which is what a reader searches by when they know what broke.
--   * `exception_detail` holds the serialized exception chain as a JSON array, one node per exception,
--     outermost first (contracts/logging-contract.md section 1.1). It is one column and not a child table on
--     purpose: the entry's hash covers one row, and splitting the chain would break the chain the store keeps.
--   * `error_fingerprint` is the SHA-256 of the fault's shape, so the same fault hashes the same on any machine
--     (FR-033, SC-014). It is what makes grouping possible, and it is NULL for an entry raised without an
--     exception.
--
-- All four are NULLable. A store that already holds entries has none of these values for them, and an entry
-- that is merely informational has no fault to describe.
--
-- Six indexes were added, one per filter the panel offers, each keyed on its own column followed by
-- `created_utc`. The order is the one every panel query uses: equality on the filter column, then a range over
-- the window, newest first. `created_utc` is second rather than first because the panel always filters before
-- it orders, and a leading `created_utc` would make each index a second copy of the existing one.
--
-- The chain columns are untouched and the table stays append-only: nothing updates or removes an entry except
-- `sp_ops_startup_logs_purge`, which removes only the oldest entries and only within the retention the operator
-- set.

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
    module VARCHAR(128) NULL COMMENT 'The part of the application the entry came from; an ILogger category name lands here, and a category is a namespace-qualified type name',
    error_type VARCHAR(128) NULL COMMENT 'The fault type, when there was a fault',
    exception_detail MEDIUMTEXT NULL COMMENT 'The serialized exception chain, one JSON node per exception, outermost first',
    error_fingerprint CHAR(64) NULL COMMENT 'SHA-256 of the fault shape, so the same fault groups together across machines; NULL without an exception',
    message TEXT NOT NULL,
    payload_json MEDIUMTEXT NULL,
    previous_hash CHAR(64) NULL,
    entry_hash CHAR(64) NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_ops_startup_logs_public_id (public_id),
    KEY idx_ops_startup_logs_created_utc (created_utc),
    KEY idx_ops_startup_logs_correlation_id (correlation_id),
    KEY idx_ops_startup_logs_level_created_utc (level, created_utc) COMMENT 'Panel filter: severity',
    KEY idx_ops_startup_logs_host_id_created_utc (host_id, created_utc) COMMENT 'Panel filter: machine',
    KEY idx_ops_startup_logs_module_created_utc (module, created_utc) COMMENT 'Panel filter: module',
    KEY idx_ops_startup_logs_actor_id_created_utc (actor_id, created_utc) COMMENT 'Panel filter: person',
    KEY idx_ops_startup_logs_error_type_created_utc (error_type, created_utc) COMMENT 'Panel filter: fault type',
    KEY idx_ops_startup_logs_error_fingerprint_created_utc (error_fingerprint, created_utc) COMMENT 'Panel grouping and filter: fault fingerprint'
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;