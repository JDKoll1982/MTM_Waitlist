-- Stored Procedure: sp_auth_remembered_sign_ins_get
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T046)
--
-- Purpose: hand the caller the payload it stored for a person on this machine, so it can try to decrypt it.
--
-- Contract:
--   IN  p_user_id      BIGINT
--   IN  p_computer_id  BIGINT
--   OUT zero or one row: payload_ciphertext, payload_iv, key_fingerprint, updated_utc
--
-- No row means nothing was ever remembered for this pair, or what was remembered has been cleared, and the
-- caller shows the ordinary sign-in form. An inactive row is never returned: clearing blanks the payload, so a
-- reader that could see one would be reading a row that decrypts to nothing.
--
-- The store does not and cannot tell whether the payload is still decryptable. `key_fingerprint` is returned so
-- the caller can answer that itself, which is what lets a rotated key fall back to the sign-in form instead of
-- raising (FR-014).

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_remembered_sign_ins_get;

CREATE PROCEDURE sp_auth_remembered_sign_ins_get(
    IN p_user_id BIGINT,
    IN p_computer_id BIGINT
)
SELECT
    r.payload_ciphertext,
    r.payload_iv,
    r.key_fingerprint,
    r.updated_utc
FROM
    auth_remembered_sign_ins r
WHERE
    r.user_id = p_user_id
    AND r.computer_id = p_computer_id
    AND r.is_active = 1
LIMIT 1;
