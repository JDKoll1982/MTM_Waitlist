-- Create table: auth_remembered_sign_ins
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T046, plan D12)
-- Purpose: the encrypted payload that lets a machine offer "remember me" without keeping a credential in
--          plaintext anywhere (FR-014).
--
-- The payload is written and read only by the application, which encrypts it before it arrives and decrypts it
-- after it leaves. This table therefore stores three things and understands none of them: `payload_ciphertext`,
-- the `payload_iv` it was encrypted with, and `key_fingerprint`, which names which key was used.
--
-- `key_fingerprint` is the reason a rotated key degrades instead of failing (FR-014). A fingerprint that no
-- longer matches the key in hand means the payload cannot be decrypted, and the sign-in form is shown rather
-- than an error being raised. Without the fingerprint the application would have to try to decrypt and treat
-- the failure as a fault, which is exactly the behaviour the requirement forbids.
--
-- One row per person per machine, enforced by `uq_auth_remembered_sign_ins_user_computer`, so a re-remember
-- replaces the payload in place. Clearing sets `is_active = 0` and blanks the payload rather than deleting the
-- row: a cleared row keeps its provenance and loses the only thing that could be decrypted. A read never
-- returns an inactive row, so a cleared payload cannot be decrypted by mistake.
--
-- `payload_iv` is never absent. A ciphertext without its initialisation vector cannot be decrypted at all, so a
-- row like that could only ever be a bug; the column is NOT NULL so the store refuses to hold one, and the
-- validation script asserts the same shape (contracts/sql-contracts.md section 2).

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS auth_remembered_sign_ins (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    user_id BIGINT NOT NULL,
    computer_id BIGINT NOT NULL,
    payload_ciphertext VARBINARY(512) NOT NULL COMMENT 'The encrypted remembered payload; opaque to the store',
    payload_iv VARBINARY(16) NOT NULL COMMENT 'The initialisation vector the ciphertext was produced with',
    key_fingerprint CHAR(64) NOT NULL COMMENT 'Identifies the key that encrypted the payload, so a rotated key is detected rather than raised',
    is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Soft state: 0 once cleared, and a cleared row is never read',
    created_utc DATETIME NOT NULL COMMENT 'When this person first used this machine to remember a sign-in',
    updated_utc DATETIME NOT NULL COMMENT 'When the payload was last written or cleared',
    PRIMARY KEY (id),
    UNIQUE KEY uq_auth_remembered_sign_ins_public_id (public_id),
    UNIQUE KEY uq_auth_remembered_sign_ins_user_computer (user_id, computer_id) COMMENT 'One remembered sign-in per person per machine',
    CONSTRAINT fk_auth_remembered_sign_ins_core_users_profiles_user_id FOREIGN KEY (user_id) REFERENCES core_users_profiles (id),
    CONSTRAINT fk_auth_remembered_sign_ins_core_computers_registry_computer_id FOREIGN KEY (computer_id) REFERENCES core_computers_registry (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = 'An encrypted remembered sign-in per person and machine, with the initialisation vector and the fingerprint of the key that produced it.';

SET FOREIGN_KEY_CHECKS = 1;
