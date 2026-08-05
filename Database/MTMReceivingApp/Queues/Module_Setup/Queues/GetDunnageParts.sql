-- ========================================
-- Module: setup
-- Queue Script: GetDunnageParts
-- Purpose: Retrieve dunnage parts for a selected dunnage type.
-- Parameters:
--   @DunnageTypeId  (INT expected by sp_Dunnage_Parts_GetByType)
-- Notes:
--   - @PartNumber/@SequenceNumber are workflow context values maintained by app
--     state; this source procedure filters by type id.
-- ========================================

USE mtm_receiving_application;

CALL sp_Dunnage_Parts_GetByType (@DunnageTypeId);