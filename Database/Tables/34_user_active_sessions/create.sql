-- Create table: user_active_sessions
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T044)
-- Purpose: the one active session for a person on a machine (FR-010, FR-011).
--
-- One row per person per machine, enforced by `uq_user_active_sessions_user_computer` rather than by the
-- procedures, so a second sign-in on the same machine replaces this row instead of adding a second one. The
-- row is reused across sign-outs: signing out sets `revoked_utc` and `is_active = 0` in place, and the next
-- sign-in repoints the same row at a freshly issued token. Nothing here is ever inserted twice for one person
-- and one machine, which is why the key can be the pair and not the pair plus a status.
--
-- The token is held as a salted hash and never in plaintext (ruleset, "Session and Security"): `token_hash`
-- is the SHA-256 hex digest the caller computed and `token_salt` is the salt it used. A reader of this table
-- can replay nothing and can reverse nothing; the store only ever compares digests.
--
-- Validity is decided by the store's own clock, never the workstation's: the reader compares
-- `fn_server_utc_now()` against `expires_utc`, and this table stores no "now" of its own. `fn_server_utc_now`
-- is deliberately retained for exactly this reason (contracts/sql-contracts.md section 5).
--
-- `source_label` records where the session came from in the caller's vocabulary (for example `sign_in`,
-- `remembered_sign_in`), which is what lets the support reader tell a remembered session from a typed one
-- without storing anything about the credential.
--
-- No row is ever hard-deleted by the application: a signed-out or expired row is the evidence that the session
-- existed, and the retention of that evidence is not this table's decision to make.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS user_active_sessions (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    user_id BIGINT NOT NULL,
    computer_id BIGINT NOT NULL,
    token_hash CHAR(64) NOT NULL COMMENT 'SHA-256 hex digest of the issued token; never the token itself',
    token_salt VARBINARY(32) NOT NULL COMMENT 'Salt the digest was computed with; present whenever a digest is',
    issued_utc DATETIME NOT NULL COMMENT 'When the token was issued, from the store clock',
    expires_utc DATETIME NOT NULL COMMENT 'When the token stops being valid, from the store clock',
    revoked_utc DATETIME NULL COMMENT 'When the session was cleared; NULL while it has not been',
    is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Soft state: 0 once cleared, so the row survives as evidence',
    source_label VARCHAR(32) NOT NULL COMMENT 'Caller vocabulary for where the session came from',
    created_utc DATETIME NOT NULL COMMENT 'When this row was first written; unchanged by a later sign-in',
    PRIMARY KEY (id),
    UNIQUE KEY uq_user_active_sessions_public_id (public_id),
    UNIQUE KEY uq_user_active_sessions_user_computer (user_id, computer_id) COMMENT 'One active row per person per machine',
    KEY idx_user_active_sessions_is_active_expires_utc (is_active, expires_utc) COMMENT 'Live-session reads filter on this pair',
    KEY idx_user_active_sessions_computer_id (computer_id),
    CONSTRAINT fk_user_active_sessions_core_users_profiles_user_id FOREIGN KEY (user_id) REFERENCES core_users_profiles (id),
    CONSTRAINT fk_user_active_sessions_core_computers_registry_computer_id FOREIGN KEY (computer_id) REFERENCES core_computers_registry (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = 'The one active session for a person on a machine: a salted token digest, its lifetime and its machine key.';

SET FOREIGN_KEY_CHECKS = 1;
