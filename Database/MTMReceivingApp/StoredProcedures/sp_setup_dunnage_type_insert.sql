-- ========================================
-- Procedure dependency: sp_Dunnage_Types_Insert
-- Target database: mtm_receiving_application
-- Purpose: Quick Add dunnage type from MTM_Waitlist Module_Setup.
-- ========================================

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