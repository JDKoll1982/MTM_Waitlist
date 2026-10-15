-- ========================================
-- Module: Waitlist (incorrect part, fuzzy search)
-- Queue Script: Get Part Matches
-- Purpose: Return the parts a term matches: the part whose number IS the term, and every part whose
--          number CONTAINS the term, so the operator correcting an incorrect part can pick the right
--          one instead of typing free text (FR-082, FR-083).
-- Target: Infor Visual SQL Server (VISUAL / MTMFG)
-- Template: Module_Waitlist GetInventoryLocations.sql (PART table, verbatim/verbatim-contains match)
-- Parameters:
--   @Term - the term to match, already upper-cased and zero-padded by the application when it starts
--           with a known part-number prefix (MMC coil, MMF flatstock, FGT die). The application's input
--           rule is PartMatchSearchInput.Normalize; this script matches what it is handed.
--
-- LIVE ONLY. This script is deliberately NOT a read shape: it has no entry in VisualReadShapeCatalog and
-- no mtm_mock mirror, because a contains-search over every part is not a mirrorable shape (FR-084,
-- research R7). Its caller is PartMatchSearchService, which reads it through the same live executor every
-- other Infor Visual read uses and never reaches for cached rows.
--
-- The exact match is returned FIRST when it exists. The caller resolves it at once in that case rather
-- than offering a list, which is the rule FR-083 asks for; ordering it first means the same answer is
-- visible to a person reading this script's output by hand.
--
-- LIKE wildcards a part number could contain are escaped, so a term holding '%', '_' or '[' matches those
-- characters rather than being read as a pattern.
-- ========================================

SET NOCOUNT ON;

-- Wide enough that neither the term nor its escaped pattern is silently truncated: PART.ID holds at most
-- PartPictureLayout.MaxPartNumberLength (190) characters, and escaping can triple a wildcard character.
DECLARE @TermNormalized nvarchar(190) = LTRIM(RTRIM(@Term));

DECLARE @ContainsPattern nvarchar(580) =
    '%' + REPLACE(REPLACE(REPLACE(@TermNormalized, '[', '[[]'), '%', '[%]'), '_', '[_]') + '%';

SELECT TOP (200)
    part.ID AS PartNumber
FROM PART AS part
WHERE
    part.ID IS NOT NULL
    AND part.ID <> ''
    AND (
        part.ID = @TermNormalized
        OR part.ID LIKE @ContainsPattern
    )
ORDER BY
    CASE WHEN part.ID = @TermNormalized THEN 0 ELSE 1 END,
    part.ID;
