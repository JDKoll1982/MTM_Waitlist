-- ========================================
-- Module: Mock cache (Module_Mock)
-- Population Script: Part matches (shape 9 of 12: part_matches)
-- Purpose: Produce the COMPLETE mirror snapshot for shape 9 in one set-based read.
-- Target: Infor Visual SQL Server (VISUAL / MTMFG)
-- Parameters: none (the driver population is enumerated here, not supplied by the caller)
--
-- Scope
-- -----
-- The domain the per-key read can return is the plant's part master: the live read matches a typed term
-- against PART.ID with no other filter (GetPartMatches.sql), so every part with a number is an answer it
-- could give. The snapshot therefore holds every part, and the mirror can answer any completion the live
-- read would have answered (FR-002/FR-004 transparency).
--
-- Driver key
-- ----------
-- The shape's declared input is the term an operator searches BY, not a row key, so the snapshot projects
-- each row's own part number into it. It is stored beside the output columns exactly as `work_order_search`
-- stores the work order it searched by, and the read procedure filters on the OUTPUT column.
--
-- Projection contract (must match VisualReadShapeCatalog + sp_visual_part_matches_refresh):
--   Term, PartNumber, Description.
-- GetPartMatches.sql is the per-key live read this projection is copied verbatim from.
-- ========================================

SET NOCOUNT ON;

SELECT
    LTRIM(RTRIM(part.ID)) AS Term,
    LTRIM(RTRIM(part.ID)) AS PartNumber,
    LTRIM(RTRIM(COALESCE(part.DESCRIPTION, ''))) AS Description
FROM PART AS part
WHERE
    part.ID IS NOT NULL
    AND LTRIM(RTRIM(part.ID)) <> ''
ORDER BY
    PartNumber;
