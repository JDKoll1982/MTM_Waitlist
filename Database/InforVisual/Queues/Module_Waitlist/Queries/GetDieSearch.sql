-- ========================================
-- Module: Waitlist (generic request die search)
-- Queue Script: Get Die Search
-- Purpose: Return every die a search term matches, so a generic die request can be raised without a job. A term
--          that IS a die (FGT) number answers that die; a term that is a part number answers every die Infor
--          Visual ties to the part, each with the location it lives in (020 FR-002, FR-003, FR-006).
-- Target: Infor Visual SQL Server (VISUAL / MTMFG)
-- Template: Module_Waitlist GetPartMatches.sql (verbatim term, no wildcard) and Module_Setup GetSubordinateParts.sql
--           (FGT die classification, the die's home location on PART_SITE, the 'Die still needs location' text)
-- Parameters:
--   @Term - the term to match, already upper-cased and zero-padded by the application when it starts with a known
--           part-number prefix (MMC coil, MMF flatstock, FGT die). The application's input rule is
--           PartMatchSearchInput.Normalize; this script matches what it is handed.
--
-- LIVE ONLY. This script is deliberately NOT a read shape: it has no entry in VisualReadShapeCatalog and no
-- mtm_mock mirror, because an association search over three relations is not a mirrorable shape (020 FR-015,
-- research R8). Its caller is GenericRequestDieData, which reads it through the same live executor every other
-- Infor Visual read uses and never reaches for cached rows.
--
-- The generic die request itself does NOT use this script through that caller any more: it asks read shape 12
-- (die_search), whose live half is this script and whose mirror holds the same (term, die, association)
-- pairings, so a die request keeps working while Infor Visual is unreachable.
--
-- Association precedence, which is what keeps a part's list complete (FR-003): the term's own die number first,
-- then the dies PART_ALTERNATE records against the term's part, then the FGT parts that part's work orders
-- required. A die named by more than one of them appears once, under the strongest association.
--
-- A die is not inventoried, so location is the part's PRIMARY location, which lives on PART_SITE. The stock rows
-- are the fallback only when no primary location is recorded, and they are ordered by location id because a die's
-- every stock row carries quantity zero (GetSubordinateParts.sql records the same finding).
--
-- A die whose description is still the plant's 'Die still needs location' placeholder keeps that text: on a job it
-- means "this job has no die", while here it is a die that exists and has no home yet (FR-009).
-- ========================================

SET NOCOUNT ON;

DECLARE @TermNormalized nvarchar(190) = LTRIM(RTRIM(@Term));

-- A contains pattern, escaped the way GetPartMatches.sql escapes it, so a term holding '%', '_' or '[' matches
-- those characters rather than being read as a wildcard.
DECLARE @ContainsPattern nvarchar(580) =
    '%' + REPLACE(REPLACE(REPLACE(@TermNormalized, '[', '[[]'), '%', '[%]'), '_', '[_]') + '%';

IF @TermNormalized = ''
BEGIN
    RETURN;
END

WITH DieMatches AS
(
    SELECT
        part.ID AS DieNumber,
        LTRIM(RTRIM(COALESCE(part.DESCRIPTION, ''))) AS Description,
        'Die' AS Association,
        0 AS AssociationRank
    FROM PART AS part
    WHERE
        (part.ID = @TermNormalized OR part.ID LIKE @ContainsPattern)
        AND part.ID LIKE 'FGT%'

    UNION ALL

    SELECT
        die.ID,
        LTRIM(RTRIM(COALESCE(die.DESCRIPTION, ''))),
        'Explicit',
        1
    FROM PART_ALTERNATE AS alternate
    INNER JOIN PART AS die
        ON die.ID = alternate.ALTERNATE_PART_ID
    WHERE
        (alternate.PART_ID = @TermNormalized OR alternate.PART_ID LIKE @ContainsPattern)
        AND die.ID LIKE 'FGT%'

    UNION ALL

    SELECT
        die.ID,
        LTRIM(RTRIM(COALESCE(die.DESCRIPTION, ''))),
        'History',
        2
    FROM WORK_ORDER AS wo
    INNER JOIN REQUIREMENT AS req
        ON req.WORKORDER_TYPE = wo.TYPE
        AND req.WORKORDER_BASE_ID = wo.BASE_ID
        AND req.WORKORDER_LOT_ID = wo.LOT_ID
        AND req.WORKORDER_SPLIT_ID = wo.SPLIT_ID
        AND req.WORKORDER_SUB_ID = wo.SUB_ID
    INNER JOIN PART AS die
        ON die.ID = req.PART_ID
    WHERE
        (wo.PART_ID = @TermNormalized OR wo.PART_ID LIKE @ContainsPattern)
        AND req.PART_ID LIKE 'FGT%'
),
OnePerDie AS
(
    SELECT
        DieNumber,
        Description,
        Association,
        AssociationRank,
        ROW_NUMBER() OVER (
            PARTITION BY DieNumber
            ORDER BY AssociationRank) AS die_rank
    FROM DieMatches
)
SELECT TOP (200)
    matched.DieNumber,
    matched.Description,
    matched.Association,
    COALESCE(
        (SELECT TOP 1 NULLIF(LTRIM(RTRIM(site.PRIMARY_LOC_ID)), '')
         FROM PART_SITE AS site
         WHERE
             site.PART_ID = matched.DieNumber
             AND NULLIF(LTRIM(RTRIM(site.PRIMARY_LOC_ID)), '') IS NOT NULL
         ORDER BY site.SITE_ID),
        (SELECT TOP 1 NULLIF(LTRIM(RTRIM(stock.LOCATION_ID)), '')
         FROM PART_LOCATION AS stock
         WHERE
             stock.PART_ID = matched.DieNumber
             AND NULLIF(LTRIM(RTRIM(stock.LOCATION_ID)), '') IS NOT NULL
         ORDER BY stock.LOCATION_ID),
        '') AS Location
FROM OnePerDie AS matched
WHERE matched.die_rank = 1
ORDER BY
    matched.AssociationRank,
    matched.DieNumber;
