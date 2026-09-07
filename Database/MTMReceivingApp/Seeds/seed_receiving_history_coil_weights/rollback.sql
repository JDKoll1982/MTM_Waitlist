-- ========================================
-- Rollback: receiving_history coil skid weight seed
-- Target database: mtm_receiving_application
-- Removes only the rows inserted by create.sql (matched by load_guid).
-- ========================================

USE mtm_receiving_application;

DELETE FROM receiving_history
WHERE load_guid IN (
    'aaaaaaaa-0000-0000-0000-000000000001',
    'aaaaaaaa-0000-0000-0000-000000000002',
    'aaaaaaaa-0000-0000-0000-000000000003'
);
