-- Stored Procedure: sp_core_buildings_get
-- Engine: MySQL 5.7
--
-- Purpose: the buildings every chooser offers, in the catalog's own display order. The caller decides whether a
--          withdrawn building is drawn: `is_active` travels with each row so a screen that must show what a work
--          centre still names can do so, and a chooser that must not offer it filters on the flag it received.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_buildings_get;

CREATE PROCEDURE sp_core_buildings_get()
SELECT
    building_name,
    is_active,
    sort_rank,
    updated_utc
FROM core_buildings_catalog
ORDER BY sort_rank ASC, building_name ASC;
