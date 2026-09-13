-- Seed: seed_waitlist_request_item_images
-- Engine: MySQL 5.7
-- Purpose: Give the card's two-hop picture fallback (FR-009) something real to fall back to, under the two
--          scopes this feature adds to config_images_locations:
--            request_item      keyed by Item code      (the Item's own picture)
--            request_category  keyed by Category code  (the family a card falls back to)
--          Resolution order is Item -> Category family -> the existing placeholder, and a "nothing configured"
--          answer never replaces a picture the request already resolves (FR-009, FR-021).
-- Data, not DDL: scope is a free-form VARCHAR, so adding two values is a row change.
-- What is seeded, and what is deliberately not:
--   * Only paths to pictures that actually ship are seeded: Assets/RequestTypes/*.png, copied into the app
--     output directory alongside the executable. DoesPathExist resolves a relative path against the app
--     directory, so these rows resolve rather than falling through to the placeholder.
--   * The Deliver Category carries no family picture because none ships; it falls to the placeholder, which
--     is the honest answer and exercises the second hop.
--   * No row is seeded for an Item with no picture of its own: an operator adds those through the picture
--     screen, and a seeded row pointing at a file that does not exist would be a lie that resolves to a
--     placeholder anyway.
-- Rerunnable: clears only the two scopes this seed owns, so work-centre and legacy override rows are untouched.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

DELETE FROM config_images_locations WHERE scope IN ('request_item', 'request_category');

SET FOREIGN_KEY_CHECKS = 1;

-- --------------------------------------------------------------- request_category: the family a card falls back to
INSERT INTO config_images_locations
    (public_id, scope, scope_item_id, image_path, is_active, created_utc, updated_utc)
VALUES
    ('c2000000-0000-4000-8000-000000000001', 'request_category', 'Pickup',
     'Assets/RequestTypes/pickup.png', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('c2000000-0000-4000-8000-000000000002', 'request_category', 'Assist',
     'Assets/RequestTypes/forklift-assist.png', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('c2000000-0000-4000-8000-000000000003', 'request_category', 'Other',
     'Assets/RequestTypes/other.png', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());

-- --------------------------------------------------------------- request_item: the Item's own picture
INSERT INTO config_images_locations
    (public_id, scope, scope_item_id, image_path, is_active, created_utc, updated_utc)
VALUES
    ('c2000000-0000-4000-8000-000000000011', 'request_item', 'pickup-die',
     'Assets/RequestTypes/die-handling.png', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('c2000000-0000-4000-8000-000000000012', 'request_item', 'deliver-die',
     'Assets/RequestTypes/die-handling.png', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('c2000000-0000-4000-8000-000000000013', 'request_item', 'deliver-flatstock',
     'Assets/RequestTypes/flatstock.png', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('c2000000-0000-4000-8000-000000000014', 'request_item', 'assist-table-place',
     'Assets/RequestTypes/table-handling.png', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('c2000000-0000-4000-8000-000000000015', 'request_item', 'assist-table-remove',
     'Assets/RequestTypes/table-handling.png', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('c2000000-0000-4000-8000-000000000016', 'request_item', 'other',
     'Assets/RequestTypes/other.png', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());
