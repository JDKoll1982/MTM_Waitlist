-- ========================================
-- Module: Setup
-- Queue Script: Get Sequences
-- Purpose: Resolve sequences for a selected work order and part
-- Parameters:
--   @NormalizedWorkOrder
--   @PartNumber
-- ========================================

SELECT
    @NormalizedWorkOrder AS NormalizedWorkOrder,
    @PartNumber AS PartNumber;