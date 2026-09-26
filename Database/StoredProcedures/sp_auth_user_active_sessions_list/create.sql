-- Stored Procedure: sp_auth_user_active_sessions_list
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T045)
--
-- Purpose: the support read. Every live session, newest first, optionally narrowed to one person or one machine.
--
-- Contract:
--   IN  p_user_id      BIGINT  NULL for every person
--   IN  p_computer_id  BIGINT  NULL for every machine
--   OUT one row per live session: public_id, user_id, display_name, computer_id, computer_name, issued_utc,
--       expires_utc, source_label
--
-- "Live" has one definition, and it is the same one the validating reader uses: active, not revoked, and not
-- yet expired by the store's clock. Writing the predicate out here rather than reusing a view keeps the two
-- readers comparable by reading, which matters when the question being answered is why somebody was or was not
-- asked to sign in.
--
-- The person's display name and the machine's name are joined in because the caller is a person reading a list,
-- and an identifier it cannot recognise is not an answer. Both joins are inner joins against NOT NULL foreign
-- keys, so a row can never be silently dropped by the read.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_user_active_sessions_list;

CREATE PROCEDURE sp_auth_user_active_sessions_list(
    IN p_user_id BIGINT,
    IN p_computer_id BIGINT
)
SELECT
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
    s.id DESC;
