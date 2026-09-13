-- Seed: seed_setup_active_jobs_eight_configurations
-- Engine: MySQL 5.7
-- Purpose: Seed the eight active-job configurations the Run 4 visibility matrix is driven through, so the
--          Item picker's rules are proved against the REAL read path (ActiveJobItemResolverService and its own
--          deserializer) rather than against a hand-written expectation.
-- The eight configurations, on the seed's own work-centre block:
--   900-1  coil only            MMC subordinate, a REAL scrap type
--   900-2  flatstock only       MMF subordinate deliberately tagged Component, so the part-number prefix wins
--                               (this is the job that proves a flatstock-only job is offered pickup-coil, D21)
--   900-3  die only             FGT subordinate, scrap value 'Scrap Type Required'
--   900-4  component only       non-prefixed part, stored Category Component, scrap value 'No Scrap'
--   900-5  dunnage only         no subordinate part, an assigned dunnage part
--   900-6  everything at once   coil + flatstock + die + component, a REAL scrap type, and dunnage
--   900-7  no subordinate part  an empty subordinate array, no dunnage
--   (900-8) a work centre with NO active job: realised by the ABSENCE of a row, which is what the resolver
--           reads as "no active job" — the seed inserts nothing for it on purpose.
-- Scrap decisions across the block cover the whole three-way gate (a real type, 'No Scrap',
-- 'Scrap Type Required', and unset), which is the input FR-031 and contract section 5 are written against.
-- Shape: subordinate_parts_json / selected_dunnage_parts_json are written in exactly the shape the write path
-- stores them (SetupPersistenceService serializes the models directly, so the keys are PascalCase property
-- names), because the read path deserializes into SetupSubordinatePart / SetupDunnagePart with the default
-- options. A hand-written shape that merely looked plausible would make this seed a test of itself.
-- Rerunnable: clears its own seven rows by work centre, and nothing else.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

DELETE FROM setup_active_jobs
WHERE work_center IN ('900-1', '900-2', '900-3', '900-4', '900-5', '900-6', '900-7');

SET FOREIGN_KEY_CHECKS = 1;

-- --------------------------------------------------------------- 900-1: coil only, a real scrap type
INSERT INTO setup_active_jobs
    (public_id, work_order, part_number, sequence_number, work_center,
     selected_dunnage_type_id, selected_dunnage_part_id,
     subordinate_parts_json, selected_dunnage_parts_json, is_active, created_utc, updated_utc)
VALUES
    ('c3000000-0000-4000-8000-000000000001', 'WO-900001', 'PART-9001', '10', '900-1',
     NULL, NULL,
     JSON_ARRAY(JSON_OBJECT(
        'Category', 'Coil',
        'PartNumber', 'MMC0001000',
        'Description', 'Coil 0.062 x 48 wide',
        'Location', 'A-01-01',
        'OnHandQuantity', 1200.50,
        'User8', '',
        'SelectedScrapType', 'Steel Offal',
        'IsLowStock', FALSE)),
     NULL, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

-- --------------------------------------------------------------- 900-2: flatstock only, prefix wins over the stored tag
    ('c3000000-0000-4000-8000-000000000002', 'WO-900002', 'PART-9002', '10', '900-2',
     NULL, NULL,
     JSON_ARRAY(JSON_OBJECT(
        'Category', 'Component',
        'PartNumber', 'MMF0001154',
        'Description', 'Flatstock 0.125 x 36',
        'Location', 'B-02-03',
        'OnHandQuantity', 88.00,
        'User8', '',
        'SelectedScrapType', 'No Scrap',
        'IsLowStock', FALSE)),
     NULL, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

-- --------------------------------------------------------------- 900-3: die only, the placeholder scrap value
    ('c3000000-0000-4000-8000-000000000003', 'WO-900003', 'PART-9003', '10', '900-3',
     NULL, NULL,
     JSON_ARRAY(JSON_OBJECT(
        'Category', 'Die',
        'PartNumber', 'FGT0002000',
        'Description', 'Die 9003-A',
        'Location', 'DIE SHOP',
        'OnHandQuantity', 0,
        'User8', '',
        'SelectedScrapType', 'Scrap Type Required',
        'IsLowStock', FALSE)),
     NULL, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

-- --------------------------------------------------------------- 900-4: a component, and a real "no scrap" answer
    ('c3000000-0000-4000-8000-000000000004', 'WO-900004', 'PART-9004', '10', '900-4',
     NULL, NULL,
     JSON_ARRAY(JSON_OBJECT(
        'Category', 'Component',
        'PartNumber', 'CMP0004455',
        'Description', 'Bushing, bronze',
        'Location', 'C-04-12',
        'OnHandQuantity', 240.00,
        'User8', '',
        'SelectedScrapType', 'No Scrap',
        'IsLowStock', FALSE)),
     NULL, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

-- --------------------------------------------------------------- 900-5: no subordinate part, dunnage assigned
    ('c3000000-0000-4000-8000-000000000005', 'WO-900005', 'PART-9005', '10', '900-5',
     'DNG-TYPE-01', 'DNG-PART-01',
     JSON_ARRAY(),
     JSON_ARRAY(JSON_OBJECT(
        'Id', 'DNG-PART-01',
        'TypeId', 'DNG-TYPE-01',
        'PartNumber', 'DNG0007788',
        'DisplayName', 'Rack, 24 x 36 wire',
        'DunnageTypeName', 'Rack',
        'HomeLocation', 'DUNNAGE-ROW-3',
        'ImagePath', '',
        'Metadata', '',
        'IsSelectedForPair', TRUE)),
     1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

-- --------------------------------------------------------------- 900-6: everything at once
    ('c3000000-0000-4000-8000-000000000006', 'WO-900006', 'PART-9006', '20', '900-6',
     'DNG-TYPE-02', 'DNG-PART-02',
     JSON_ARRAY(
        JSON_OBJECT(
            'Category', 'Coil',
            'PartNumber', 'MMC0001001',
            'Description', 'Coil 0.048 x 36 wide',
            'Location', 'A-01-07',
            'OnHandQuantity', 640.00,
            'User8', '',
            'SelectedScrapType', 'Aluminum Offal',
            'IsLowStock', FALSE),
        JSON_OBJECT(
            'Category', 'Flatstock',
            'PartNumber', 'MMF0001155',
            'Description', 'Flatstock 0.090 x 24',
            'Location', 'B-02-08',
            'OnHandQuantity', 12.00,
            'User8', '',
            'SelectedScrapType', 'No Scrap',
            'IsLowStock', TRUE),
        JSON_OBJECT(
            'Category', 'Die',
            'PartNumber', 'FGT0002001',
            'Description', 'Die 9006-B',
            'Location', 'PRESS BAY',
            'OnHandQuantity', 0,
            'User8', '',
            'SelectedScrapType', 'Scrap Type Required',
            'IsLowStock', FALSE),
        JSON_OBJECT(
            'Category', 'Component',
            'PartNumber', 'CMP0004456',
            'Description', 'Pin, dowel 8mm',
            'Location', 'C-04-15',
            'OnHandQuantity', 0,
            'User8', '',
            'SelectedScrapType', '',
            'IsLowStock', FALSE)),
     JSON_ARRAY(JSON_OBJECT(
        'Id', 'DNG-PART-02',
        'TypeId', 'DNG-TYPE-02',
        'PartNumber', 'DNG0007789',
        'DisplayName', 'Tote, collapsible',
        'DunnageTypeName', 'Tote',
        'HomeLocation', 'DUNNAGE-ROW-4',
        'ImagePath', '',
        'Metadata', '',
        'IsSelectedForPair', TRUE)),
     1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

-- --------------------------------------------------------------- 900-7: an active job with no subordinate part
    ('c3000000-0000-4000-8000-000000000007', 'WO-900007', 'PART-9007', '10', '900-7',
     NULL, NULL,
     JSON_ARRAY(),
     NULL, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());
