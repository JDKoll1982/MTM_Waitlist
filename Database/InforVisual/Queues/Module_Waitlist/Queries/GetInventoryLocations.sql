-- ========================================
-- Module: Waitlist (Detail Location Grid)
-- Queue Script: Get Inventory Locations
-- Purpose: Return pooled per-part/per-location inventory rows (Part #, Location, On-hand
--          Quantity) for the Waitlist detail location grid.
-- Target: Infor Visual SQL Server (VISUAL / MTMFG)
-- Template: Module_Setup GetSubordinateParts.sql (PART / PART_LOCATION / WAREHOUSE joins)
-- Parameters:
--   @PartNumber
--
-- Note: Infor Visual reports coil stock as ONE pooled row per unique part-in-location:
--       a location appears once and pools all transactions for that part number into a
--       single total on-hand quantity, expressed as total WEIGHT in pounds. The Quantity
--       column is that pooled on-hand weight (lb); the Location column is the Infor
--       location/warehouse code (V-A0-01 style), NOT a friendly storage label.
--
--       The app service applies the on-hand >= 1 filter AND omits rows whose Location is in
--       the ignored-locations setting (file 05).
-- ========================================

SET NOCOUNT ON;

DECLARE @PartNumberNormalized nvarchar(60) = LTRIM(RTRIM(@PartNumber));

SELECT
    part.ID AS PartNumber,
    COALESCE(
        NULLIF(pl.LOCATION_ID, ''),
        NULLIF(pl.WAREHOUSE_ID, ''),
        NULLIF(wh.DESCRIPTION, ''),
        ''
    ) AS Location,
    CAST(COALESCE(pl.QTY, part.QTY_ON_HAND, 0) AS decimal(20, 8)) AS OnHandQuantity
FROM PART AS part
LEFT JOIN PART_LOCATION AS pl
    ON pl.PART_ID = part.ID
LEFT JOIN WAREHOUSE AS wh
    ON wh.ID = COALESCE(pl.WAREHOUSE_ID, pl.WAREHOUSE_ID)
WHERE
    part.ID = @PartNumberNormalized
    AND COALESCE(NULLIF(pl.LOCATION_ID, ''), NULLIF(pl.WAREHOUSE_ID, ''), NULLIF(wh.DESCRIPTION, ''), '') <> ''
ORDER BY
    Location,
    part.ID;
