-- Stored Procedure: sp_auth_user_previous_sign_ins_list
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T235)
--
-- Purpose: the sign-in stage's offer. The last six people who signed in on one computer, most recent first, so
--          the stage can show them as choices that fill in a user name.
--
-- Contract:
--   IN  p_computer_id  BIGINT  the machine whose sign-in history is being offered
--   OUT up to six rows: username_normalized, display_name — the six most recent sign-ins first
--
-- This replaces the read remember-me carried (contracts/sql-contracts.md section 2). It is
-- the same question asked of rows the store already holds: `user_active_sessions` is keyed one row per person
-- per machine, and `sp_auth_user_active_sessions_upsert` refreshes `issued_utc` on every sign-in, so the six
-- largest `issued_utc` values for one machine are the six most recent people to sign in there.
--
-- It carries no credential of any kind, and that is the point rather than an omission. There is no token digest,
-- no salt, no payload and no key fingerprint in the projection, and no sign-in time either: the list offers a
-- name to type beside, and nothing in it can sign anybody in (FR-014, FR-048). `issued_utc` orders the result
-- without being returned, because nothing downstream acts on when the sign-in happened.
--
-- A row that has been signed out still counts. `sp_auth_user_active_sessions_clear` revokes the session in
-- place rather than deleting the row, and somebody who signed in and then signed out did sign in on this
-- computer — which is exactly what the list says. Rows are therefore read regardless of `is_active`.
--
-- Six is fixed here rather than passed in: FR-014 names six, nothing else asks this question, and a caller that
-- could widen it could turn a hint beside a form into a directory export.
--
-- This is a read and returns a result set. It owns no data, so its rollback drops nothing but the routine.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_user_previous_sign_ins_list;

CREATE PROCEDURE sp_auth_user_previous_sign_ins_list(
    IN p_computer_id BIGINT
)
SELECT
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
LIMIT 6;
