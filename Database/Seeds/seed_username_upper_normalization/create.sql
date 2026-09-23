-- Seed: seed_username_upper_normalization
-- Engine: MySQL 5.7
-- Feature: 006-user-management-and-permissions (task T015)
-- Purpose: bring every stored sign-in name onto the upper-case rule (FR-003, FR-002).
--
-- How it makes itself reversible
--   The original spelling is destroyed by the change, so it is recorded first, in
--   `seed_username_case_backup`. That table exists only for this migration's lifetime: rollback.sql reads it,
--   restores what it holds, and drops it. It is not a permanent schema artifact, which is why it is not listed
--   in AllTables.sql and has no create.sql of its own.
--   Re-running create.sql cannot overwrite a recorded original: the backup insert keeps the value it already
--   holds, so the first recording of a row is the one that survives.
--
-- How it reports rather than fails
--   A row whose upper-cased name would collide with another row's upper-cased name is left alone, and every
--   such row is listed in the result set at the end of this script. Nothing here aborts: a reported row is a
--   thing for a DBA to look at, not a reason to stop the migration.
--
-- A finding worth knowing, recorded here because only implementation revealed it
--   Every column here is `utf8mb4_unicode_ci`, which is case-INSENSITIVE, and that cuts both ways:
--   * The comparison must be forced binary. `u.username_normalized <> UPPER(u.username_normalized)` is FALSE
--     under that collation, because the two sides compare equal; the naive form therefore selects nothing and
--     the migration silently does nothing. Every comparison below is written `BINARY A <> BINARY B` for that
--     reason. This was caught by running the seed against a real store, not by reading it.
--   * `username_normalized` also carries a UNIQUE key under the same collation, so two rows differing only in
--     case cannot both exist. The collision branch is therefore not reachable through that key as the schema
--     stands; it can only fire if a store has been given a different collation. It is kept because the rule it
--     protects is real and a collation is not something this seed can assume.
--   The collation is also why FR-002's "signing in succeeds whatever case is typed" already held before this
--   change: the comparison folded case. What this migration actually fixes is the STORED form, so the column is
--   consistent for readers that compare it exactly and for anybody reading the table.

USE mtm_waitlist;

SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS seed_username_case_backup (
    user_id BIGINT NOT NULL,
    previous_username_normalized VARCHAR(128) NOT NULL,
    recorded_utc DATETIME NOT NULL,
    PRIMARY KEY (user_id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

INSERT INTO seed_username_case_backup (
    user_id,
    previous_username_normalized,
    recorded_utc
)
SELECT u.id, u.username_normalized, UTC_TIMESTAMP()
FROM core_users_profiles u
WHERE BINARY u.username_normalized <> BINARY UPPER(u.username_normalized)
  AND NOT EXISTS (
        SELECT 1
        FROM core_users_profiles x
        WHERE x.id <> u.id
          AND BINARY UPPER(x.username_normalized) = BINARY UPPER(u.username_normalized)
  )
ON DUPLICATE KEY UPDATE
    previous_username_normalized = seed_username_case_backup.previous_username_normalized;

SELECT 'skipped: would collide on the unique key' AS report, u.id AS user_id, u.username_normalized
FROM core_users_profiles u
WHERE BINARY u.username_normalized <> BINARY UPPER(u.username_normalized)
  AND EXISTS (
        SELECT 1
        FROM core_users_profiles x
        WHERE x.id <> u.id
          AND BINARY UPPER(x.username_normalized) = BINARY UPPER(u.username_normalized)
  )
ORDER BY u.username_normalized ASC;

UPDATE core_users_profiles u
INNER JOIN seed_username_case_backup b ON b.user_id = u.id
SET u.username_normalized = UPPER(u.username_normalized),
    u.updated_utc = UTC_TIMESTAMP();
