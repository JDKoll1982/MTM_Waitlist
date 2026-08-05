-- ========================================
-- Procedure dependency: sp_Dunnage_Parts_Insert
-- Target database: mtm_receiving_application
-- Purpose: Quick Add dunnage part from MTM_Waitlist Module_Setup.
-- ========================================

USE mtm_receiving_application;

-- Expected to exist from MTM_Receiving_Application deployment:
--   sp_Dunnage_Parts_Insert(
--     IN  p_part_id VARCHAR(50),
--     IN  p_type_id INT,
--     IN  p_spec_values JSON,
--     IN  p_image_path VARCHAR(255),
--     IN  p_quantity_type VARCHAR(100),
--     IN  p_home_location VARCHAR(100),
--     IN  p_user VARCHAR(50),
--     OUT p_new_id INT
--   );

-- Deployment note:
-- If this procedure is missing in target environment, deploy it from
-- MTM_Receiving_Application Database_Deployment SQL artifacts before using Quick Add.