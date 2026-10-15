-- ========================================
-- Module: setup
-- Queue Script: GetDunnageParts
-- Purpose: Retrieve dunnage parts for a selected dunnage type.
-- Parameters:
--   @DunnageTypeId  (INT expected by sp_Dunnage_Parts_GetByType)
-- Notes:
--   - @PartNumber/@SequenceNumber are workflow context values maintained by app
--     state; this source procedure filters by type id.
--   - What carries `home_location` to the caller (FR-033, FR-045, FR-089): this script is a pass-through and
--     the projection lives in the receiving procedure and in the app, never here. sp_Dunnage_Parts_GetByType
--     returns the dunnage row's own `home_location` column - the column sp_Dunnage_Parts_Insert writes,
--     `home_location VARCHAR(100)` (see ../StoredProcedures/sp_Dunnage_Parts_Insert.sql) - and
--     DunnageWorkflowService.GetDunnagePartsFromBackendAsync reads that column off each returned row onto
--     `SetupDunnagePart.HomeLocation`. That part is what the Setup workflow persists with the assigned dunnage
--     parts, so the value reaches the waitlist snapshot's `Home Location` cell from there.
--   - Deliberately kept as the CALL: SetupReceivingMySqlScriptStore.LoadAsync reads this text as a warm-up and
--     the caller discards it, then invokes sp_Dunnage_Parts_GetByType by name (the same arrangement
--     documented in sp_Dunnage_Parts_Insert.sql), so no projection or filter of our own may live here.
-- ========================================

USE mtm_receiving_application;

CALL sp_Dunnage_Parts_GetByType (@DunnageTypeId);