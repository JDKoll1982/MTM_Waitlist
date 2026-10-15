-- ========================================
-- Validation: disposition input — finished goods (and NCM) location codes
-- Target: Infor Visual SQL Server (VISUAL / MTMFG)
-- Feature: 012-waitlist-card-layout, task T067 (FR-090, FR-092, FR-093)
-- Run:     sqlcmd -S <visual-server> -d VISUAL -U <user> -P <password> -i this-file -v PartNumber="24733431"
--          (or set @PartNumber below and run it from SSMS)
--
-- WHY THIS EXISTS
--   GetDispositionInput.sql now sums PART_LOCATION.QTY at the part's FINISHED-GOODS locations instead of reading
--   PART.QTY_ON_HAND (the FR-090 defect). The finished-goods class it uses was copied from the taxonomy already
--   documented for the WIP side and already used by GetPartWarehouseQuantities.sql:
--       LOCATION.DESCRIPTION LIKE '%FINISHED GOODS%' OR WAREHOUSE.DESCRIPTION LIKE '%FINISHED GOODS%'
--       OR LOCATION.ID IN ('FG','DC-FG')
--   Those CODES ARE UNCONFIRMED FOR THIS INSTALLATION. The CSV schema export carries no data rows and no enum
--   values, so nothing in the repository can produce them: this script reads the live plant system and reports
--   what the installation actually uses, which is what makes the predicate authoritative (or shows it is wrong).
--
-- WHAT IT REPORTS (each step returns rows; nothing passes silently)
--   1. The codes found: every stocked location, its warehouse, and whether the finished-goods class matches it.
--   2. The non-conforming (NCM) locations this installation actually uses, for the NCM side of the derivation.
--   3. The code lists: ENUM_CODES values for PART_LOCATION.STATUS and LOCATION.TYPE.
--   4. The assertion on one known part: its finished-goods sum next to PART.QTY_ON_HAND and its total.
--
-- READ THE OUTPUT BEFORE TRUSTING THE PREDICATE: if step 4 shows the finished-goods sum equal to the total, or
-- step 1 matches no stocked location, the class is wrong for this installation and GetDispositionInput.sql's
-- predicate (and its population twin) must be corrected to the codes reported here.
-- ========================================

SET NOCOUNT ON;

-- The part the assertion runs against. Defaults to the part the file-14 Phase 6 run validated live
-- (WO-074011 / 24733431, order R, open 33, on-hand 54 at the time). Override with -v PartNumber="...".
DECLARE @PartNumber nvarchar(60) = LTRIM(RTRIM(N'$(PartNumber)'));
IF @PartNumber IN (N'', N'$(PartNumber)')
BEGIN
    SET @PartNumber = N'24733431';
END

DECLARE @PartNumberNormalized nvarchar(60) = @PartNumber;

-- ---------------------------------------------------------------------------------------------------
-- Step 1: the finished-goods class as this installation actually spells it, over locations that hold stock.
-- ---------------------------------------------------------------------------------------------------
SELECT
    '1. finished-goods class' AS Step,
    loc.WAREHOUSE_ID,
    loc.ID AS LocationCode,
    loc.TYPE AS LocationType,
    loc.DESCRIPTION AS LocationDescription,
    wh.DESCRIPTION AS WarehouseDescription,
    COUNT(pl.PART_ID) AS StockedPartCount,
    CAST(COALESCE(SUM(COALESCE(pl.QTY, 0)), 0) AS decimal(20, 8)) AS StockedQuantity,
    CASE
        WHEN ISNULL(loc.DESCRIPTION, '') LIKE '%FINISHED GOODS%'
            OR ISNULL(wh.DESCRIPTION, '') LIKE '%FINISHED GOODS%'
            OR ISNULL(loc.ID, '') IN ('FG', 'DC-FG')
            THEN N'matches the finished-goods predicate'
        ELSE N'does not match the finished-goods predicate'
    END AS ClassVerdict
FROM LOCATION AS loc
LEFT JOIN WAREHOUSE AS wh
    ON wh.ID = loc.WAREHOUSE_ID
LEFT JOIN PART_LOCATION AS pl
    ON pl.WAREHOUSE_ID = loc.WAREHOUSE_ID
    AND pl.LOCATION_ID = loc.ID
GROUP BY
    loc.WAREHOUSE_ID,
    loc.ID,
    loc.TYPE,
    loc.DESCRIPTION,
    wh.DESCRIPTION
HAVING
    COUNT(pl.PART_ID) > 0
ORDER BY
    ClassVerdict DESC,
    StockedQuantity DESC,
    loc.WAREHOUSE_ID,
    loc.ID;

-- ---------------------------------------------------------------------------------------------------
-- Step 2: the non-conforming (NCM) locations this installation uses, for FR-093's location-derived state.
-- The NCM signal is the LOCATION the stock occupies — never the part's recorded item type.
-- ---------------------------------------------------------------------------------------------------
SELECT
    '2. NCM locations' AS Step,
    loc.WAREHOUSE_ID,
    loc.ID AS LocationCode,
    loc.TYPE AS LocationType,
    loc.DESCRIPTION AS LocationDescription,
    wh.DESCRIPTION AS WarehouseDescription,
    COUNT(pl.PART_ID) AS StockedPartCount,
    CAST(COALESCE(SUM(COALESCE(pl.QTY, 0)), 0) AS decimal(20, 8)) AS StockedQuantity
FROM LOCATION AS loc
LEFT JOIN WAREHOUSE AS wh
    ON wh.ID = loc.WAREHOUSE_ID
LEFT JOIN PART_LOCATION AS pl
    ON pl.WAREHOUSE_ID = loc.WAREHOUSE_ID
    AND pl.LOCATION_ID = loc.ID
WHERE
    ISNULL(loc.ID, '') LIKE 'NCM%'
    OR ISNULL(loc.DESCRIPTION, '') LIKE 'NCM%'
GROUP BY
    loc.WAREHOUSE_ID,
    loc.ID,
    loc.TYPE,
    loc.DESCRIPTION,
    wh.DESCRIPTION
ORDER BY
    StockedQuantity DESC,
    loc.WAREHOUSE_ID,
    loc.ID;

-- ---------------------------------------------------------------------------------------------------
-- Step 3: the code lists themselves. ENUM_CODES is where a code column's actual values live, so this is what
-- turns a guessed code into a confirmed one. PART_LOCATION.STATUS is the Infor-side NCM signal's home;
-- LOCATION.TYPE is the location class's.
-- ---------------------------------------------------------------------------------------------------
SELECT
    '3. code lists' AS Step,
    ec.TABLE_NAME,
    ec.COLUMN_NAME,
    ec.ENUM_VALUE,
    ec.ENUM_TEXT,
    ec.LANGUAGE_ID
FROM ENUM_CODES AS ec
WHERE
    (ec.TABLE_NAME = 'PART_LOCATION' AND ec.COLUMN_NAME = 'STATUS')
    OR (ec.TABLE_NAME = 'LOCATION' AND ec.COLUMN_NAME = 'TYPE')
ORDER BY
    ec.TABLE_NAME,
    ec.COLUMN_NAME,
    ec.ENUM_VALUE;

-- ---------------------------------------------------------------------------------------------------
-- Step 4: the assertion. One known part's finished-goods sum, its whole-location sum, and PART.QTY_ON_HAND
-- next to each other — the three figures the FR-090 defect conflated. The verdict is CHECK when the corrected
-- figure and the old one agree, because that is exactly the case where the predicate may be matching nothing.
-- ---------------------------------------------------------------------------------------------------
DECLARE @TotalQuantity decimal(20, 8) = (
    SELECT COALESCE(SUM(COALESCE(pl.QTY, 0)), 0)
    FROM PART_LOCATION AS pl
    WHERE pl.PART_ID = @PartNumberNormalized
);

DECLARE @FinishedGoodsQuantity decimal(20, 8) = (
    SELECT COALESCE(SUM(COALESCE(pl.QTY, 0)), 0)
    FROM PART_LOCATION AS pl
    LEFT JOIN WAREHOUSE AS wh
        ON wh.ID = pl.WAREHOUSE_ID
    LEFT JOIN LOCATION AS loc
        ON loc.WAREHOUSE_ID = pl.WAREHOUSE_ID
        AND loc.ID = pl.LOCATION_ID
    WHERE pl.PART_ID = @PartNumberNormalized
        AND (ISNULL(loc.DESCRIPTION, '') LIKE '%FINISHED GOODS%'
             OR ISNULL(wh.DESCRIPTION, '') LIKE '%FINISHED GOODS%'
             OR ISNULL(loc.ID, '') IN ('FG', 'DC-FG'))
);

DECLARE @PartQtyOnHand decimal(20, 8) = (
    SELECT COALESCE(part.QTY_ON_HAND, 0)
    FROM PART AS part
    WHERE part.ID = @PartNumberNormalized
);

SELECT
    '4. assertion' AS Step,
    @PartNumberNormalized AS PartNumber,
    @FinishedGoodsQuantity AS FinishedGoodsSumFromLocations,
    @TotalQuantity AS SumOverEveryLocation,
    @PartQtyOnHand AS PartQtyOnHandTheOldFigure,
    CASE
        WHEN @FinishedGoodsQuantity = @PartQtyOnHand THEN N'CHECK - the corrected figure equals PART.QTY_ON_HAND, so the finished-goods class may be matching every location this part is stocked in; confirm the codes in step 1'
        WHEN @FinishedGoodsQuantity > @TotalQuantity THEN N'CHECK - the finished-goods sum exceeds the total no location can hold; the predicate is matching more than the part holds'
        ELSE N'OK - the finished-goods figure is a location-filtered subset of the part''s total, which is what the classifier documents'
    END AS Verdict;

-- The same part, location by location, so a reader can see which of its locations the class did and did not match.
SELECT
    '4b. the known part, location by location' AS Step,
    pl.WAREHOUSE_ID,
    pl.LOCATION_ID,
    loc.TYPE AS LocationType,
    loc.DESCRIPTION AS LocationDescription,
    wh.DESCRIPTION AS WarehouseDescription,
    CAST(COALESCE(pl.QTY, 0) AS decimal(20, 8)) AS LocationQuantity,
    pl.STATUS AS PartLocationStatus,
    CASE
        WHEN ISNULL(loc.DESCRIPTION, '') LIKE '%FINISHED GOODS%'
            OR ISNULL(wh.DESCRIPTION, '') LIKE '%FINISHED GOODS%'
            OR ISNULL(loc.ID, '') IN ('FG', 'DC-FG')
            THEN 1
        ELSE 0
    END AS CountsAsFinishedGoods,
    CASE
        WHEN ISNULL(loc.ID, '') LIKE 'NCM%' OR ISNULL(loc.DESCRIPTION, '') LIKE 'NCM%'
            THEN 1
        ELSE 0
    END AS CountsAsNonConforming
FROM PART_LOCATION AS pl
LEFT JOIN WAREHOUSE AS wh
    ON wh.ID = pl.WAREHOUSE_ID
LEFT JOIN LOCATION AS loc
    ON loc.WAREHOUSE_ID = pl.WAREHOUSE_ID
    AND loc.ID = pl.LOCATION_ID
WHERE pl.PART_ID = @PartNumberNormalized
ORDER BY
    CountsAsFinishedGoods DESC,
    pl.WAREHOUSE_ID,
    pl.LOCATION_ID;
