-- Rollback Stored Procedure: sp_waitlist_request_handler_status_update
-- Reverses Database/StoredProcedures/sp_waitlist_request_handler_status_update/create.sql, as this repository's
-- procedure rollbacks do (the artefact is replaced wholesale, so reversing it is removing it). Re-applying the
-- previous body is what restores the state this procedure was added to: handler_status is then only ever written
-- by a statement that does not know which handler read the row, and the guard parameter goes with the definition.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_handler_status_update;
