-- ========================================
-- Module: Mock cache (Module_Mock)
-- Population Script: Employee by number (shape 6 of 6: employee_by_number)
-- Purpose: Produce the COMPLETE mirror snapshot for shape 6 in one set-based read.
-- Target: Infor Visual SQL Server (VISUAL / MTMFG)
-- Parameters: none (the driver population is enumerated here, not supplied by the caller)
--
-- Projection contract (must match VisualReadShapeCatalog + sp_visual_employee_by_number_refresh):
--   EmployeeNumber (input key), FirstName, LastName.
--   GetEmployeeByNumber.sql is the per-key live read this projection is copied verbatim from; the
--   payload source additionally validates the returned column set against the catalog at runtime so
--   drift is reported as a schema mismatch instead of loading a wrong-shaped snapshot.
--
-- The driver population is the employees the application can ask for: every employee carrying a
-- numeric id, which is what an operator types. Inactive employees are included deliberately, because
-- the caller must be able to distinguish "not active" from "not known".
-- ========================================

SET NOCOUNT ON;

SELECT
	LTRIM(RTRIM(e.ID)) AS EmployeeNumber,
	COALESCE(e.FIRST_NAME, '') AS FirstName,
	COALESCE(e.LAST_NAME, '') AS LastName
FROM EMPLOYEE AS e
WHERE e.ID IS NOT NULL
  AND LTRIM(RTRIM(e.ID)) <> '';
