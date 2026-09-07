-- Seed: seed_waitlist_request_catalog
-- Engine: MySQL 5.7
-- Source: Assets/Config/waitlist-request-types.json (real request-type/subtype catalog).
-- Purpose: Populate the real (non-mock) request-type/subtype catalog tables
--          (waitlist_request_types + waitlist_request_subtypes) so the New-Request wizard has data.
--          public_id values are the stable GUIDs from the JSON 'id' field (== the
--          RequestTypeInventory / RequestSubtypeInventory StableId values used by image overrides),
--          so the DB matches the shipped JSON 1:1 and renames never orphan stored overrides.
-- Rerunnable: truncates both tables before inserting.

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;

TRUNCATE TABLE waitlist_request_subtypes;
TRUNCATE TABLE waitlist_request_types;
SET FOREIGN_KEY_CHECKS = 1;

-- ---------------------------------------------------------------
-- waitlist_request_types
-- ---------------------------------------------------------------
INSERT INTO waitlist_request_types
    (public_id, request_type, control, flow, requires_text_input, prompt_text,
     min_length, max_length, default_image_path, center_data_grid_fields_json,
     is_active, created_utc, updated_utc)
VALUES
    ('7bb056da-2dfd-4da5-824c-cff0973544fb', 'Pickup',
     'MTM_Waitlist.Module_Waitlist.Controls.Pickup.PickupRequestTypeImageView',
     'direct-to-confirmation', 0, NULL, 0, 200, NULL,
     JSON_ARRAY('Part','Quantity to move','Pickup location','Destination','Traceability ID'),
     1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('8ee9f259-e404-4d4f-8f20-b5dd7c1c220f', 'Other',
     'MTM_Waitlist.Module_Waitlist.Controls.Other.OtherRequestTypeImageView',
     'direct-to-confirmation', 0, NULL, 0, 200, NULL,
     JSON_ARRAY('Request description','Requested work center'),
     1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('20f434cb-59f2-4ecb-a623-84ff5fa3bed1', 'Coil',
     'MTM_Waitlist.Module_Waitlist.Controls.Coil.CoilRequestTypeImageView',
     'direct-to-confirmation', 0, NULL, 0, 200, NULL,
     JSON_ARRAY('Coil number','Quantity in house','Coil description','Average coil weight','Requesting work center'),
     1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('90dc8c5b-6a66-4cd4-94c1-5fc634363f5d', 'Scrap',
     'MTM_Waitlist.Module_Waitlist.Controls.Scrap.ScrapRequestTypeImageView',
     'direct-to-confirmation', 0, NULL, 0, 200, NULL,
     JSON_ARRAY('Pickup work center','Quantity involved','Scrap lugger','Scrap reason'),
     1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('805c5b0f-815f-46bf-b5a7-73de5d74fa1f', 'Flatstock',
     'MTM_Waitlist.Module_Waitlist.Controls.Flatstock.FlatstockRequestTypeImageView',
     'direct-to-confirmation', 0, NULL, 0, 200, NULL,
     JSON_ARRAY('Part','Quantity','Work center','Destination'),
     1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('b0fc9058-6c74-4171-9f46-11d9b4332b51', 'Table Handling',
     'MTM_Waitlist.Module_Waitlist.Controls.TableHandling.TableHandlingRequestTypeImageView',
     'direct-to-confirmation', 0, NULL, 0, 200, NULL,
     JSON_ARRAY('Part','Quantity','Work center','Destination'),
     1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('be310bec-a74d-4242-a1a8-6220557d8700', 'Die Handling',
     'MTM_Waitlist.Module_Waitlist.Controls.DieHandling.DieHandlingRequestTypeImageView',
     'direct-to-confirmation', 0, NULL, 0, 200, NULL,
     JSON_ARRAY('Die','Quantity','Pickup location','Destination'),
     1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('11a182a7-507c-4069-9763-b902bc7fe8a0', 'Forklift Assist',
     'MTM_Waitlist.Module_Waitlist.Controls.ForkliftAssist.ForkliftAssistRequestTypeImageView',
     'collect-input-then-confirm', 1, 'Enter description of why you need assistance', 5, 50, NULL,
     JSON_ARRAY('Description','Work center','Requested by'),
     1, UTC_TIMESTAMP(), UTC_TIMESTAMP());

-- ---------------------------------------------------------------
-- waitlist_request_subtypes (request_type_id resolved by public_id)
-- ---------------------------------------------------------------
INSERT INTO waitlist_request_subtypes
    (public_id, request_type_id, subtype_name, control, flow, requires_text_input, prompt_text,
     min_length, max_length, default_image_path, center_data_grid_fields_json,
     is_active, created_utc, updated_utc)
SELECT s.public_id, rt.id, s.subtype_name, s.control, s.flow, s.requires_text_input, s.prompt_text,
       s.min_length, s.max_length, s.default_image_path, s.center_data_grid_fields_json,
       s.is_active, UTC_TIMESTAMP(), UTC_TIMESTAMP()
FROM (
    SELECT 'feaa6b5e-13db-4ad8-9c17-188f40ca41ed' AS public_id, '7bb056da-2dfd-4da5-824c-cff0973544fb' AS parent_public_id,
           'Pickup Other' AS subtype_name,
           'MTM_Waitlist.Module_Waitlist.Controls.Pickup.PickupRequestTypeImageView' AS control,
           'collect-input-then-confirm' AS flow, 1 AS requires_text_input,
           'Enter what is needed' AS prompt_text, 5 AS min_length, 200 AS max_length, NULL AS default_image_path,
           JSON_ARRAY('Request description','Requested work center') AS center_data_grid_fields_json, 1 AS is_active
    UNION ALL SELECT '1a50ee24-6959-4242-9853-9b0e7ab19074','7bb056da-2dfd-4da5-824c-cff0973544fb','Pickup NCM',
           'MTM_Waitlist.Module_Waitlist.Controls.PickupNcm.PickupNcmWaitlistLineView','direct-to-confirmation',0,NULL,0,200,NULL,
           JSON_ARRAY('Part','Quantity to move','Pickup location','Destination','Traceability ID'),1
    UNION ALL SELECT '7bc50583-6bc1-4ffc-94e3-40f2a21ae6d7','7bb056da-2dfd-4da5-824c-cff0973544fb','Pickup WIP',
           'MTM_Waitlist.Module_Waitlist.Controls.PickupWip.PickupWipWaitlistLineView','direct-to-confirmation',0,NULL,0,200,NULL,
           JSON_ARRAY('Part and quantity','Pickup work center','WIP destination','Operation sequence'),1
    UNION ALL SELECT 'efb993e9-be75-4059-a619-be67519c9bc5','7bb056da-2dfd-4da5-824c-cff0973544fb','Pickup FG',
           'MTM_Waitlist.Module_Waitlist.Controls.PickupFg.PickupFgWaitlistLineView','direct-to-confirmation',0,NULL,0,200,NULL,
           JSON_ARRAY('Part number','Quantity remaining','Part description','Customer','Packlist'),1
    UNION ALL SELECT 'b46bab54-8067-436f-ac8b-d0531538422e','7bb056da-2dfd-4da5-824c-cff0973544fb','Pickup Coil',
           'MTM_Waitlist.Module_Waitlist.Controls.Coil.CoilWaitlistLineView','direct-to-confirmation',0,NULL,0,200,NULL,
           JSON_ARRAY('Coil number','Quantity in house','Coil description','Average coil weight','Requesting work center'),1
    UNION ALL SELECT 'f6113875-95ba-4469-959f-c91979266596','7bb056da-2dfd-4da5-824c-cff0973544fb','Pickup Flatstock',
           'MTM_Waitlist.Module_Waitlist.Controls.Flatstock.FlatstockRequestTypeImageView','direct-to-confirmation',0,NULL,0,200,NULL,
           JSON_ARRAY('Part','Quantity','Work center','Destination'),1
    UNION ALL SELECT '3be33961-67ad-4fb8-9da1-937996d8bccb','8ee9f259-e404-4d4f-8f20-b5dd7c1c220f','General Text Entry',
           'MTM_Waitlist.Module_Waitlist.Controls.Other.OtherRequestTypeImageView','collect-input-then-confirm',1,
           'Enter a short description',5,200,NULL,
           JSON_ARRAY('Request description','Requested work center'),1
    UNION ALL SELECT '8c5dce16-b806-4165-b314-2368bb97f6d7','20f434cb-59f2-4ecb-a623-84ff5fa3bed1','Bring',
           'MTM_Waitlist.Module_Waitlist.Controls.Coil.CoilWaitlistLineView','direct-to-confirmation',0,NULL,0,200,NULL,
           JSON_ARRAY('Coil number','Quantity in house','Coil description','Average coil weight','Requesting work center'),1
    UNION ALL SELECT '340966b0-d6a3-45d9-890e-21a01a3ad95d','20f434cb-59f2-4ecb-a623-84ff5fa3bed1','Pickup',
           'MTM_Waitlist.Module_Waitlist.Controls.Coil.CoilWaitlistLineView','direct-to-confirmation',0,NULL,0,200,NULL,
           JSON_ARRAY('Coil number','Quantity in house','Coil description','Average coil weight','Requesting work center'),1
    UNION ALL SELECT '9499cd7c-a8d0-4f5f-b1b7-8f5c2b03c258','20f434cb-59f2-4ecb-a623-84ff5fa3bed1','Wrong Coil @ press',
           'MTM_Waitlist.Module_Waitlist.Controls.Coil.CoilWaitlistLineView','collect-input-then-confirm',1,
           'Explain how or why the coil is wrong',5,200,NULL,
           JSON_ARRAY('Coil number','Quantity in house','Coil description','Average coil weight','Requesting work center'),1
    UNION ALL SELECT '8048b55c-02e8-4797-8c9e-a35fda586650','20f434cb-59f2-4ecb-a623-84ff5fa3bed1','Need Riser Table',
           'MTM_Waitlist.Module_Waitlist.Controls.Coil.CoilWaitlistLineView','direct-to-confirmation',0,NULL,0,200,NULL,
           JSON_ARRAY('Coil number','Quantity in house','Coil description','Average coil weight','Requesting work center'),1
    UNION ALL SELECT '4ad1825a-4fa5-4f9f-8ed3-0cc13f4de1bd','20f434cb-59f2-4ecb-a623-84ff5fa3bed1','Need Coil Turned around',
           'MTM_Waitlist.Module_Waitlist.Controls.Coil.CoilWaitlistLineView','direct-to-confirmation',0,NULL,0,200,NULL,
           JSON_ARRAY('Coil number','Quantity in house','Coil description','Average coil weight','Requesting work center'),1
    UNION ALL SELECT '73371828-06ce-49f1-9480-541e2498dc5d','90dc8c5b-6a66-4cd4-94c1-5fc634363f5d','Empty',
           'MTM_Waitlist.Module_Waitlist.Controls.Scrap.ScrapWaitlistLineView','direct-to-confirmation',0,NULL,0,200,NULL,
           JSON_ARRAY('Pickup work center','Quantity involved','Scrap lugger','Scrap reason'),1
    UNION ALL SELECT '185903aa-6f85-4c90-a31e-30e1fa855d0e','90dc8c5b-6a66-4cd4-94c1-5fc634363f5d','Pickup Hopper, do not return',
           'MTM_Waitlist.Module_Waitlist.Controls.Scrap.ScrapWaitlistLineView','direct-to-confirmation',0,NULL,0,200,NULL,
           JSON_ARRAY('Pickup work center','Quantity involved','Scrap lugger','Scrap reason'),1
    UNION ALL SELECT 'bab039e8-ea41-47af-a332-a2853f2148e0','90dc8c5b-6a66-4cd4-94c1-5fc634363f5d','Bring Hopper',
           'MTM_Waitlist.Module_Waitlist.Controls.Scrap.ScrapWaitlistLineView','direct-to-confirmation',0,NULL,0,200,NULL,
           JSON_ARRAY('Pickup work center','Quantity involved','Scrap lugger','Scrap reason'),1
    UNION ALL SELECT 'bb93c8a1-3921-4e8b-8874-10b9e5215bb3','805c5b0f-815f-46bf-b5a7-73de5d74fa1f','Bring',
           'MTM_Waitlist.Module_Waitlist.Controls.Flatstock.FlatstockRequestTypeImageView','direct-to-confirmation',0,NULL,0,200,NULL,
           JSON_ARRAY('Part','Quantity','Work center','Destination'),1
    UNION ALL SELECT 'ccb5ebd3-df70-4405-992f-4632377059a5','805c5b0f-815f-46bf-b5a7-73de5d74fa1f','Pickup',
           'MTM_Waitlist.Module_Waitlist.Controls.Flatstock.FlatstockRequestTypeImageView','direct-to-confirmation',0,NULL,0,200,NULL,
           JSON_ARRAY('Part','Quantity','Work center','Destination'),1
    UNION ALL SELECT '4d8f6d3b-6116-4d07-bd4a-3cc420b8001f','805c5b0f-815f-46bf-b5a7-73de5d74fa1f','Wrong Flatstock @ Workcenter',
           'MTM_Waitlist.Module_Waitlist.Controls.Flatstock.FlatstockRequestTypeImageView','direct-to-confirmation',0,NULL,0,200,NULL,
           JSON_ARRAY('Part','Quantity','Work center','Destination'),1
    UNION ALL SELECT 'dcbcc811-b52d-4b2f-810d-cc34200e2825','b0fc9058-6c74-4171-9f46-11d9b4332b51','Table Place Parts',
           'MTM_Waitlist.Module_Waitlist.Controls.TableHandling.TableHandlingRequestTypeImageView','direct-to-confirmation',0,NULL,0,200,NULL,
           JSON_ARRAY('Part','Quantity','Work center','Destination'),1
    UNION ALL SELECT '358870f2-e866-4e12-bd8a-e42f74b784f4','b0fc9058-6c74-4171-9f46-11d9b4332b51','Table Remove Parts',
           'MTM_Waitlist.Module_Waitlist.Controls.TableHandling.TableHandlingRequestTypeImageView','direct-to-confirmation',0,NULL,0,200,NULL,
           JSON_ARRAY('Part','Quantity','Work center','Destination'),1
    UNION ALL SELECT '20108458-386b-4337-a3c8-ba2e0481d930','be310bec-a74d-4242-a1a8-6220557d8700','Bring Die',
           'MTM_Waitlist.Module_Waitlist.Controls.DieHandling.DieHandlingRequestTypeImageView','direct-to-confirmation',0,NULL,0,200,NULL,
           JSON_ARRAY('Die','Quantity','Pickup location','Destination'),1
    UNION ALL SELECT '20bc250d-4c54-4afb-a678-7190b919ada3','be310bec-a74d-4242-a1a8-6220557d8700','Pull Die and Put Away',
           'MTM_Waitlist.Module_Waitlist.Controls.DieHandling.DieHandlingRequestTypeImageView','direct-to-confirmation',0,NULL,0,200,NULL,
           JSON_ARRAY('Die','Quantity','Pickup location','Destination'),1
    UNION ALL SELECT '36652f24-b2da-47d2-84ca-caaff1a608c4','be310bec-a74d-4242-a1a8-6220557d8700','Pull Die and Take to Die Shop',
           'MTM_Waitlist.Module_Waitlist.Controls.DieHandling.DieHandlingRequestTypeImageView','direct-to-confirmation',0,NULL,0,200,NULL,
           JSON_ARRAY('Die','Quantity','Pickup location','Destination'),1
    UNION ALL SELECT '53ec191c-f74c-4aba-9f15-55ef3c9f5cc5','be310bec-a74d-4242-a1a8-6220557d8700','Pull Die and Leave @ press',
           'MTM_Waitlist.Module_Waitlist.Controls.DieHandling.DieHandlingRequestTypeImageView','direct-to-confirmation',0,NULL,0,200,NULL,
           JSON_ARRAY('Die','Quantity','Pickup location','Destination'),1
) AS s
JOIN waitlist_request_types rt ON rt.public_id = s.parent_public_id;
