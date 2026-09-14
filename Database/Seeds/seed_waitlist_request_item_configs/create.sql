-- Seed: seed_waitlist_request_item_configs
-- Engine: MySQL 5.7
-- Purpose: Populate waitlist_request_item_configs with one row for every one of the twenty-three catalogued
--          Items, so a missing configuration row is the exception rather than the normal case and FR-014's
--          "every Item has a configuration row" is true of the shipped data.
-- Design-time sources (FR-027): the per-Item field definitions and the retiring type/subtype catalog, whose
--          populated category / item_id columns map each legacy leaf onto its canonical Item. Both are read
--          once, by a person, when this file is authored. The application never reads either document, and
--          Database/Mock/... and the build carry no reference to them.
-- Rerunnable: TRUNCATE first, so the file is deterministic and the table matches this file exactly. A
--          reinstall therefore returns every Item to its seeded allotment, which is the intended behaviour of
--          a database reinstalled from seed.
-- Notes on individual rows:
--   * The four out-of-scope Items (pickup-fg, pickup-ncm, pickup-wip, pickup-outside-service) are seeded like
--     every other Item and hidden by the visibility rules, not by deletion: a hidden Item and a missing row
--     are different states and must not be conflated (FR-028).
--   * assist-table-remove deliberately carries NO configured allotment, so the labelled 15-minute default is
--     real data rather than a hypothetical one.
--   * pickup-component's options are the requesting job's component list, so its options_json is NULL: the
--     enumerated values arrive with the job snapshot rather than from configuration. pickup-die's options are
--     fixed and are seeded here.
--   * pickup-dunnage and deliver-dunnage ask *which* dunnage the operator needs, so they too carry no
--     options_json: the choices are the dunnage parts assigned to the requesting job, and the `list` key on the
--     answer field names that job list so the choice never depends on the Item's identity (FR-013, FR-048).
--   * detail_fields_json's key order stays inside the JSON payload: a bare `order` column is banned by
--     Database/Database-Ruleset.md and is never introduced.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

TRUNCATE TABLE waitlist_request_item_configs;

SET FOREIGN_KEY_CHECKS = 1;

INSERT INTO waitlist_request_item_configs
    (public_id, item, category, control_flow, requires_answer, answer_value_type, prompt_text,
     min_length, max_length, options_json, detail_fields_json, allotted_minutes,
     created_utc, updated_utc)
VALUES
-- ------------------------------------------------------------------ Pickup (11)
('c1000000-0000-4000-8000-000000000001', 'pickup-coil', 'Pickup', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Coil number','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Quantity in house','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Coil description','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Average coil weight','value_type','string','source','job','order',4,'is_required',FALSE),
   JSON_OBJECT('label','Requesting work center','value_type','string','source','fixed','order',5,'is_required',FALSE)),
 30, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000002', 'pickup-die', 'Pickup', 'collect-input-then-confirm', 1, 'enum',
 'Choose where the die is going.', 0, 200,
 JSON_ARRAY('Die Shop', 'Home Location', 'Other'),
 JSON_ARRAY(
   JSON_OBJECT('label','Die','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Quantity','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Pickup location','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Destination','value_type','enum','source','answer','order',4,'is_required',TRUE)),
 45, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000003', 'pickup-component', 'Pickup', 'collect-input-then-confirm', 1, 'enum',
 'Choose the component to collect.', 0, 200,
 NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Component','value_type','enum','source','answer','order',1,'is_required',TRUE),
   JSON_OBJECT('label','Part description','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Quantity','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Requesting work center','value_type','string','source','fixed','order',4,'is_required',FALSE)),
 30, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000004', 'pickup-fg', 'Pickup', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Part number','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Quantity remaining','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Part description','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Customer','value_type','string','source','job','order',4,'is_required',FALSE),
   JSON_OBJECT('label','Packlist','value_type','string','source','job','order',5,'is_required',FALSE)),
 NULL, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000005', 'pickup-ncm', 'Pickup', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Part','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Quantity to move','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Pickup location','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Destination','value_type','string','source','fixed','order',4,'is_required',FALSE),
   JSON_OBJECT('label','Traceability ID','value_type','string','source','job','order',5,'is_required',FALSE)),
 NULL, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000006', 'pickup-wip', 'Pickup', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Part and quantity','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Pickup work center','value_type','string','source','fixed','order',2,'is_required',FALSE),
   JSON_OBJECT('label','WIP destination','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Operation sequence','value_type','string','source','job','order',4,'is_required',FALSE)),
 NULL, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000007', 'pickup-outside-service', 'Pickup', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Part','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Quantity to move','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Pickup location','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Destination','value_type','string','source','fixed','order',4,'is_required',FALSE),
   JSON_OBJECT('label','Traceability ID','value_type','string','source','job','order',5,'is_required',FALSE)),
 NULL, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000008', 'pickup-riser-table', 'Pickup', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Requesting work center','value_type','string','source','fixed','order',1,'is_required',FALSE)),
 20, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000009', 'pickup-dunnage', 'Pickup', 'collect-input-then-confirm', 1, 'enum',
 'Which dunnage do you need from the material handlers?', 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Dunnage part','value_type','enum','source','answer','list','dunnage','order',1,'is_required',TRUE),
   JSON_OBJECT('label','Description','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Home location','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Quantity on hand','value_type','string','source','job','order',4,'is_required',FALSE)),
 30, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000010', 'pickup-scrap', 'Pickup', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Scrap type','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Quantity involved','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Scrap lugger','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Pickup work center','value_type','string','source','fixed','order',4,'is_required',FALSE)),
 30, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000011', 'pickup-hopper', 'Pickup', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Pickup work center','value_type','string','source','fixed','order',1,'is_required',FALSE)),
 20, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

-- ------------------------------------------------------------------ Deliver (8)
('c1000000-0000-4000-8000-000000000012', 'deliver-coil', 'Deliver', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Coil number','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Quantity in house','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Coil description','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Average coil weight','value_type','string','source','job','order',4,'is_required',FALSE),
   JSON_OBJECT('label','Requesting work center','value_type','string','source','fixed','order',5,'is_required',FALSE)),
 30, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000013', 'deliver-riser-table', 'Deliver', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Requesting work center','value_type','string','source','fixed','order',1,'is_required',FALSE)),
 20, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000014', 'deliver-hopper', 'Deliver', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Requesting work center','value_type','string','source','fixed','order',1,'is_required',FALSE)),
 20, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000015', 'deliver-flatstock', 'Deliver', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Part','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Quantity','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Work center','value_type','string','source','fixed','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Destination','value_type','string','source','fixed','order',4,'is_required',FALSE)),
 30, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000016', 'deliver-die', 'Deliver', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Die','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Quantity','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Pickup location','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Destination','value_type','string','source','fixed','order',4,'is_required',FALSE)),
 45, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000017', 'deliver-dunnage', 'Deliver', 'collect-input-then-confirm', 1, 'enum',
 'Which dunnage do you need from the material handlers?', 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Dunnage part','value_type','enum','source','answer','list','dunnage','order',1,'is_required',TRUE),
   JSON_OBJECT('label','Description','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Home location','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Quantity on hand','value_type','string','source','job','order',4,'is_required',FALSE)),
 30, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000018', 'deliver-wrong-coil', 'Deliver', 'collect-input-then-confirm', 1, 'text',
 'Explain how or why the coil is wrong', 5, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Coil number','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Reason','value_type','text','source','answer','order',2,'is_required',TRUE),
   JSON_OBJECT('label','Correct coil','value_type','string','source','job','order',3,'is_required',FALSE)),
 30, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000019', 'deliver-wrong-flatstock', 'Deliver', 'collect-input-then-confirm', 1, 'text',
 'Explain how or why the flatstock is wrong', 5, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Part','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Reason','value_type','text','source','answer','order',2,'is_required',TRUE),
   JSON_OBJECT('label','Correct flatstock','value_type','string','source','job','order',3,'is_required',FALSE)),
 30, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

-- ------------------------------------------------------------------ Assist (3)
('c1000000-0000-4000-8000-000000000020', 'assist-coil-turn', 'Assist', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Coil number','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Quantity in house','value_type','string','source','job','order',2,'is_required',FALSE)),
 30, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000021', 'assist-table-place', 'Assist', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Part','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Quantity','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Work center','value_type','string','source','fixed','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Destination','value_type','string','source','job','order',4,'is_required',FALSE)),
 30, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000022', 'assist-table-remove', 'Assist', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Part','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Quantity','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Work center','value_type','string','source','fixed','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Destination','value_type','string','source','job','order',4,'is_required',FALSE)),
 NULL, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

-- ------------------------------------------------------------------ Other (1)
('c1000000-0000-4000-8000-000000000023', 'other', 'Other', 'collect-input-then-confirm', 1, 'text',
 'Enter a short description', 5, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Request description','value_type','text','source','answer','order',1,'is_required',TRUE),
   JSON_OBJECT('label','Requested work center','value_type','string','source','fixed','order',2,'is_required',FALSE)),
 15, UTC_TIMESTAMP(), UTC_TIMESTAMP());
