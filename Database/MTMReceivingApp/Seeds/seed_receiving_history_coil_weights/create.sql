-- ========================================
-- Seed: receiving_history — coil skid weights (mock data)
-- Target database: mtm_receiving_application
-- Purpose: Insert mock receiving transactions for coil part 'MMC0001000'
--          so the Waitlist "Average coil weight" can be tested.
--
-- Average coil weight is computed as AVG(quantity) across every row whose
-- part_id matches the coil part. Each row is one RECEIVED SKID; its `quantity`
-- is the skid weight the material handler actually grasps. Some skids hold
-- 2+ coils, so we average the whole skid weight — we do NOT divide by the
-- number of coils on the skid. Current stock is irrelevant here.
--
-- Seeded values for MMC0001000: 4800 + 5000 + 5200 => AVG = 5000 (5,000 lb).
--
-- Rerunnable: rows inserted by this seed are deleted first by their load_guid.
-- ========================================

USE mtm_receiving_application;

DELETE FROM receiving_history
WHERE load_guid IN (
    'aaaaaaaa-0000-0000-0000-000000000001',
    'aaaaaaaa-0000-0000-0000-000000000002',
    'aaaaaaaa-0000-0000-0000-000000000003'
);

INSERT INTO receiving_history
    (load_guid, quantity, part_id, part_type, employee_number,
     received_date, transaction_date, is_non_po_item, is_quality_hold_required,
     is_quality_hold_acknowledged, is_reprint)
VALUES
    ('aaaaaaaa-0000-0000-0000-000000000001', 4800, 'MMC0001000', 'COIL', 6229,
     NOW(), CURDATE(), 0, 0, 0, 0),
    ('aaaaaaaa-0000-0000-0000-000000000002', 5000, 'MMC0001000', 'COIL', 6229,
     NOW(), CURDATE(), 0, 0, 0, 0),
    ('aaaaaaaa-0000-0000-0000-000000000003', 5200, 'MMC0001000', 'COIL', 6229,
     NOW(), CURDATE(), 0, 0, 0, 0);
