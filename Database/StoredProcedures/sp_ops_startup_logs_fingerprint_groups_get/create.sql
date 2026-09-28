-- Stored Procedure: sp_ops_startup_logs_fingerprint_groups_get
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T049)
--
-- Purpose: the panel's grouped view. One row per distinct fault, most frequent first, so a reader sees which
--          fault is actually happening rather than a page of its occurrences.
--
-- Contract:
--   IN  p_level       VARCHAR(16)   NULL for any severity
--   IN  p_host_id     VARCHAR(128)  NULL for any machine
--   IN  p_actor_id    VARCHAR(128)  NULL for any person
--   IN  p_module      VARCHAR(128)  NULL for any module
--   IN  p_error_type  VARCHAR(128)  NULL for any fault type
--   IN  p_from_utc    DATETIME      NULL for the start of the default window
--   IN  p_to_utc      DATETIME      NULL for now
--   IN  p_max_groups  INT           NULL or not positive for the default group limit
--   OUT one row per distinct fingerprint: error_fingerprint, occurrences, first_seen_utc, last_seen_utc,
--       error_type, module, sample_message
--
-- Grouping is by `error_fingerprint` alone, and that is the whole point of the column: the same fault raised on
-- two machines, in two runs, with two different part numbers in its message, hashes the same (FR-033, SC-014),
-- so this view counts one fault and not two. An entry with no fingerprint is an entry with no exception, so it
-- is excluded rather than gathered into a NULL group — a group labelled "no fault" would sit at the top of the
-- list on any busy store and say nothing.
--
-- `error_type`, `module` and the sample message are values from the group rather than keys. They are read with
-- ANY_VALUE because a group is defined by its fingerprint and any member is as representative as another; the
-- window and the filters are the same ones the flat reader takes, so a reader can move between the two views
-- without the numbers changing under them.
--
-- The group limit is clamped like the page size is in the flat reader, for the same reason: the panel draws a
-- list, and a grouped read of a long-running store is still a read of a long-running store.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_ops_startup_logs_fingerprint_groups_get;

DELIMITER $$

CREATE PROCEDURE sp_ops_startup_logs_fingerprint_groups_get(
    IN p_level VARCHAR(16),
    IN p_host_id VARCHAR(128),
    IN p_actor_id VARCHAR(128),
    IN p_module VARCHAR(128),
    IN p_error_type VARCHAR(128),
    IN p_from_utc DATETIME,
    IN p_to_utc DATETIME,
    IN p_max_groups INT
)
BEGIN
    DECLARE v_max_groups INT DEFAULT 100;
    DECLARE v_to_utc DATETIME;
    DECLARE v_from_utc DATETIME;

    SET v_max_groups = CASE
        WHEN p_max_groups IS NULL OR p_max_groups <= 0 THEN 100
        WHEN p_max_groups > 500 THEN 500
        ELSE p_max_groups
    END;

    SET v_to_utc = COALESCE(p_to_utc, fn_server_utc_now());
    SET v_from_utc = COALESCE(p_from_utc, DATE_SUB(v_to_utc, INTERVAL 30 DAY));

    SELECT
        l.error_fingerprint,
        COUNT(*) AS occurrences,
        MIN(l.created_utc) AS first_seen_utc,
        MAX(l.created_utc) AS last_seen_utc,
        ANY_VALUE(l.error_type) AS error_type,
        ANY_VALUE(l.module) AS module,
        ANY_VALUE(l.message) AS sample_message
    FROM
        ops_startup_logs l
    WHERE
        l.error_fingerprint IS NOT NULL
        AND l.created_utc >= v_from_utc
        AND l.created_utc <= v_to_utc
        AND (p_level IS NULL OR l.level = LOWER(TRIM(p_level)))
        AND (p_host_id IS NULL OR l.host_id = TRIM(p_host_id))
        AND (p_actor_id IS NULL OR l.actor_id = TRIM(p_actor_id))
        AND (p_module IS NULL OR l.module = TRIM(p_module))
        AND (p_error_type IS NULL OR l.error_type = TRIM(p_error_type))
    GROUP BY
        l.error_fingerprint
    ORDER BY
        occurrences DESC,
        last_seen_utc DESC
    LIMIT v_max_groups;
END$$

DELIMITER ;
