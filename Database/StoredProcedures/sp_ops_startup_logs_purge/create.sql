-- Stored Procedure: sp_ops_startup_logs_purge
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T049, SC-014)
--
-- Purpose: keep the log store to its retention, bounded by age and by size, removing the oldest entries first.
--
-- Contract:
--   IN  p_max_age_days  INT  NULL for no age bound; otherwise entries older than this many days go
--   IN  p_max_rows      INT  NULL for no size bound; otherwise only the newest this many entries stay
--   OUT the affected-row count, through the non-query seam — every entry this call removed, both bounds
--       together, because a caller that has to add two numbers to know what happened will not.
--
-- Two bounds, one statement. They are combined with OR into a single DELETE on purpose: MySQL reports the
-- affected-row count of the last statement the procedure executed, so a body that deleted by age, then by size,
-- and then returned would report only the second count — and a body ending in COMMIT reports zero, which is why
-- nothing here is wrapped in a transaction. One statement is also the honest description of the rule: an entry
-- goes if it is too old, or if it is beyond the size bound counting back from the newest.
--
-- "Oldest first" is a property of the bounds rather than an ORDER BY, and it holds for both:
--   * the age bound removes a prefix of the store, because `created_utc` moves forward with `id`;
--   * the size bound keeps the newest `p_max_rows` by `id` and removes everything below them.
-- So the store is trimmed from its far end in both cases, and the entries that survive are always the most
-- recent ones.
--
-- A bound of NULL means "no bound on this axis", and a size of 0 means "keep nothing". Both are deliberate and
-- different: a caller that omits the size has not asked for the store to be emptied.
--
-- The size bound is expressed as a derived table rather than a direct subquery on the same table, because
-- MySQL refuses a DELETE whose WHERE reads the table being deleted from. The derived table is materialised
-- first, so the set of rows to keep is decided before any row is removed and the delete cannot chase its own
-- tail.
--
-- Retention is bounded by age and by size rather than by a setting read here: the caller supplies the numbers,
-- so the same procedure serves the shipped default and an operator who has changed the setting (ruleset,
-- "Retention windows are dynamic settings values").
--
-- Removing the oldest entries truncates the hash chain at its start; it does not break any surviving link. A
-- reader verifying the chain from the oldest surviving entry onwards still finds every link intact, which is
-- the most a retention policy can preserve.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_ops_startup_logs_purge;

DELIMITER $$

CREATE PROCEDURE sp_ops_startup_logs_purge(
    IN p_max_age_days INT,
    IN p_max_rows INT
)
BEGIN
    DECLARE v_has_age_bound TINYINT DEFAULT 0;
    DECLARE v_has_size_bound TINYINT DEFAULT 0;
    DECLARE v_cutoff_utc DATETIME DEFAULT NULL;
    DECLARE v_keep_rows INT DEFAULT 0;

    IF p_max_age_days IS NOT NULL AND p_max_age_days > 0 THEN
        SET v_has_age_bound = 1;
        SET v_cutoff_utc = DATE_SUB(fn_server_utc_now(), INTERVAL p_max_age_days DAY);
    END IF;

    IF p_max_rows IS NOT NULL AND p_max_rows >= 0 THEN
        SET v_has_size_bound = 1;
        SET v_keep_rows = p_max_rows;
    END IF;

    DELETE FROM ops_startup_logs
    WHERE
        (v_has_age_bound = 1 AND created_utc < v_cutoff_utc)
        OR (
            v_has_size_bound = 1
            AND id NOT IN (
                SELECT keep_rows.id
                FROM (
                    SELECT l.id
                    FROM ops_startup_logs l
                    ORDER BY l.id DESC
                    LIMIT v_keep_rows
                ) AS keep_rows
            )
        );
END$$

DELIMITER ;
