-- ========================================
-- Procedure dependency: sp_Dunnage_Types_Insert
-- Target database: mtm_receiving_application
-- Purpose: Quick Add dunnage type from MTM_Waitlist Module_Setup.
-- ========================================
--
-- **This is a dependency note, NOT an artifact of this repository.** The procedure is owned by
-- MTM_Receiving_Application, which deploys it; nothing here creates, alters or drops it, and it must not be
-- authored here — that would fork a definition another application depends on.
--
-- Renamed 2026-09-10 (task T097) from `sp_setup_dunnage_type_insert.sql`. That name existed nowhere on the
-- server (verified against `information_schema.ROUTINES` on both schemas), while the file actually documents
-- `sp_Dunnage_Types_Insert`; the file name now matches the procedure it is about, and the caller that loads it
-- (`DunnageWorkflowService.AddDunnageTypeAsync`) was updated in the same change.
--
-- The load itself is a warm-up: `SetupReceivingStoredProcedureScriptStore.LoadAsync` reads this text and the
-- caller discards it, so nothing in this file executes. The live procedure is invoked on the next line.

USE mtm_receiving_application;

-- Expected to exist from MTM_Receiving_Application deployment:
--   sp_Dunnage_Types_Insert(
--     IN  p_type_name VARCHAR(100),
--     IN  p_icon VARCHAR(50),
--     IN  p_image_path VARCHAR(255),
--     IN  p_user VARCHAR(50),
--     OUT p_new_id INT
--   );

-- Deployment note:
-- If this procedure is missing in target environment, deploy it from
-- MTM_Receiving_Application Database_Deployment SQL artifacts before using Quick Add.