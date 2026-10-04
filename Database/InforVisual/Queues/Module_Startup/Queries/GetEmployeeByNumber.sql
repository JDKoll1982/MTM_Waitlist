-- ========================================
-- Module: Startup
-- Queue Script: Get Employee By Number
-- Purpose: Resolve one employee's first and last name from their employee number
-- Target: Infor Visual SQL Server (VISUAL / MTMFG)
-- Parameters:
--   @EmployeeNumber - the four-digit employee number, matched verbatim against EMPLOYEE.ID
-- ========================================
--
-- Feature 011, read shape 6 of 6 (`employee_by_number`). The application reads this live first and
-- falls back to the mtm_mock mirror only when the reachability verdict is Cached or a live attempt
-- finds the source unreachable. The projection is copied verbatim into the population script and
-- into VisualReadShapeCatalog, so cached and live answers cannot drift in shape.
--
-- The number is matched as stored. An inactive employee is still returned, so the caller can say
-- "that employee is not active" rather than the weaker "no such employee".

SET NOCOUNT ON;

DECLARE @EmployeeNumberTrimmed nvarchar(32) = LTRIM(RTRIM(@EmployeeNumber));

SELECT TOP (1)
	e.ID AS EmployeeNumber,
	COALESCE(e.FIRST_NAME, '') AS FirstName,
	COALESCE(e.LAST_NAME, '') AS LastName
FROM EMPLOYEE AS e
WHERE e.ID = @EmployeeNumberTrimmed;
