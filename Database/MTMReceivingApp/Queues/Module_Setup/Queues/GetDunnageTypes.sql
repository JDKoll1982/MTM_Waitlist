-- ========================================
-- Module: setup
-- Queue Script: GetDunnageTypes
-- Purpose: Retrieve dunnage type catalog from mtm_receiving_application.
-- Notes:
--   - Source procedure pattern mirrors MTM_Receiving_Application:
--       sp_Dunnage_Types_GetAll
--   - @PartNumber/@SequenceNumber are accepted by caller for workflow context,
--     but type catalog retrieval is currently global.
-- ========================================

USE mtm_receiving_application;

CALL sp_Dunnage_Types_GetAll ();