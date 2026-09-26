-- Stored Procedure: sp_auth_remembered_sign_ins_clear
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T046)
--
-- Purpose: forget the remembered sign-in for a person on this machine.
--
-- Contract:
--   IN  p_user_id      BIGINT
--   IN  p_computer_id  BIGINT
--   OUT the affected-row count, through the non-query seam. 0 means there was nothing remembered to forget,
--       which is a success, not a failure.
--
-- The row is kept and its payload is destroyed. `is_active = 0` alone would be enough for every reader in this
-- repository, but leaving the ciphertext behind would leave a value nobody intends to decrypt sitting in the
-- store, and an IV left behind with no payload is a pair that can only confuse. Both byte columns are set to
-- empty and the fingerprint to empty with them, so a cleared row is indistinguishable from one that never
-- held a payload.
--
-- This is the one place a stored byte column is written empty rather than with a value: an empty ciphertext is
-- not a ciphertext, and the read procedure's `is_active = 1` filter means it can never be returned.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_remembered_sign_ins_clear;

CREATE PROCEDURE sp_auth_remembered_sign_ins_clear(
    IN p_user_id BIGINT,
    IN p_computer_id BIGINT
)
UPDATE auth_remembered_sign_ins
SET
    payload_ciphertext = CAST('' AS BINARY),
    payload_iv = CAST('' AS BINARY),
    key_fingerprint = '',
    is_active = 0,
    updated_utc = fn_server_utc_now()
WHERE
    user_id = p_user_id
    AND computer_id = p_computer_id
    AND is_active = 1;
