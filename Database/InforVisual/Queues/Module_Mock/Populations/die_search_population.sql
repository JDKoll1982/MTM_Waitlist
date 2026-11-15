-- ========================================
-- Module: Mock cache (Module_Mock)
-- Population Script: Die search (shape 12 of 12: die_search)
-- Purpose: Produce the COMPLETE mirror snapshot for shape 12 in one set-based read.
-- Target: Infor Visual SQL Server (VISUAL / MTMFG)
-- Parameters: none (the driver population is enumerated here, not supplied by the caller)
--
-- Scope
-- -----
-- The per-key read (GetDieSearch.sql) finds a die three ways: the term IS a die number, PART_ALTERNATE
-- records the die against the term's part, or the term's work orders required the die. The population read
-- enumerates all three as (term, die, association) rows, so the mirror holds every pairing the live read
-- can answer — including the part-to-die pairings, which is what lets a part number still find its dies
-- while Infor Visual is unreachable (FR-002/FR-004 transparency).
--
-- Driver key
-- ----------
-- The shape's declared input is the term an operator searches BY. It carries the die's own number for a
-- direct match and the part's number for an association, and the read procedure matches on the term or on
-- the die number, so either kind of search is answered from the same snapshot.
--
-- Projection contract (must match VisualReadShapeCatalog + sp_visual_die_search_refresh):
--   Term, DieNumber, Description, Association, Location.
-- GetDieSearch.sql is the per-key live read this projection is copied verbatim from, including its
-- association precedence (Die, Explicit, History) and its location rule.
--
-- DISTINCT is deliberate: a part's work orders repeat the same die many times, and the mirror keeps one
-- row per (term, die, association).
-- ========================================

SET NOCOUNT ON;

WITH Associations AS
(
    -- Die: the term is the die's own number.
    SELECT
        LTRIM(RTRIM(part.ID)) AS Term,
        LTRIM(RTRIM(part.ID)) AS DieNumber,
        LTRIM(RTRIM(COALESCE(part.DESCRIPTION, ''))) AS Description,
        'Die' AS Association
    FROM PART AS part
    WHERE
        part.ID LIKE 'FGT%'
        AND NULLIF(LTRIM(RTRIM(part.ID)), '') IS NOT NULL

    UNION ALL

    -- Explicit: PART_ALTERNATE records the die against the term's part.
    SELECT
        LTRIM(RTRIM(alternate.PART_ID)) AS Term,
        LTRIM(RTRIM(die.ID)) AS DieNumber,
        LTRIM(RTRIM(COALESCE(die.DESCRIPTION, ''))) AS Description,
        'Explicit' AS Association
    FROM PART_ALTERNATE AS alternate
    INNER JOIN PART AS die
        ON die.ID = alternate.ALTERNATE_PART_ID
    WHERE
        die.ID LIKE 'FGT%'
        AND NULLIF(LTRIM(RTRIM(alternate.PART_ID)), '') IS NOT NULL

    UNION ALL

    -- History: the term's work orders required the die.
    SELECT
        LTRIM(RTRIM(wo.PART_ID)) AS Term,
        LTRIM(RTRIM(die.ID)) AS DieNumber,
        LTRIM(RTRIM(COALESCE(die.DESCRIPTION, ''))) AS Description,
        'History' AS Association
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
        req.PART_ID LIKE 'FGT%'
        AND NULLIF(LTRIM(RTRIM(wo.PART_ID)), '') IS NOT NULL
)
SELECT DISTINCT
    matched.Term AS Term,
    matched.DieNumber AS DieNumber,
    matched.Description AS Description,
    matched.Association AS Association,

    -- A die is not inventoried, so its location is the part's PRIMARY location on PART_SITE; the stock rows
    -- are the fallback only when no primary location is recorded, ordered by location id because a die's
    -- every stock row carries quantity zero.
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
        ''
    ) AS Location
FROM Associations AS matched
ORDER BY
    matched.Term,
    matched.DieNumber,
    matched.Association;
