-- ========================================
-- Module: Setup
-- Queue Script: Get Subordinate Parts
-- Purpose: Resolve subordinate parts for the selected work order, part, and sequence
-- Parameters:
--   @NormalizedWorkOrder
--   @PartNumber
--   @SequenceNumber
-- ========================================

SELECT
    @NormalizedWorkOrder AS NormalizedWorkOrder,
    @PartNumber AS PartNumber,
    @SequenceNumber AS SequenceNumber;