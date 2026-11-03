-- Rollback Stored Procedure: sp_waitlist_request_insert
-- Reverses Database/StoredProcedures/sp_waitlist_request_insert/create.sql, as this repository's procedure
-- rollbacks do (the artefact is replaced wholesale, so reversing it is removing it). Re-applying the previous
-- body restores an insert that carries no answers; `p_answers_json` goes with the definition, and the row's
-- `answers_json` column is the table change's to roll back (task T003). The same holds for
-- `p_is_generic`, `p_description` and `p_accepted_warnings_json`, which specs/016-generic-wc-request-foundation
-- appended to the definition.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_insert;
