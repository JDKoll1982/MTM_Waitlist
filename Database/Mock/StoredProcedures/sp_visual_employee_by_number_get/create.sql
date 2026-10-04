-- ============================================================
-- Stored Procedure: sp_visual_employee_by_number_get
-- Database:   mtm_mock
-- Engine:     MySQL 5.7
-- Kind:       StoredProcedure / create  (read, shape 6 of 6)
-- Feature:    011-identifier-first-signin (task T007)
-- Shape key:  employee_by_number
-- ============================================================
--
-- Parameterized read of the LIVE mirror. Returns exactly the column shape the live Infor Visual read
-- returns (FR-032), so a caller cannot distinguish cached from live:
--   live : EmployeeNumber, FirstName, LastName
--   here : same names/order, projected from the mirror's own columns.
--
-- An employee number the mirror has never held returns no row, which is what lets the set-up path
-- say it cannot be completed right now rather than guessing a name.
-- ============================================================

USE mtm_mock;

DROP PROCEDURE IF EXISTS sp_visual_employee_by_number_get;

CREATE PROCEDURE sp_visual_employee_by_number_get(
    IN p_employee_number VARCHAR(32)
)
SELECT
    employee_number AS EmployeeNumber,
    COALESCE(first_name, '') AS FirstName,
    COALESCE(last_name, '')  AS LastName
FROM visual_employee_by_number_result
WHERE employee_number = p_employee_number
LIMIT 1;
