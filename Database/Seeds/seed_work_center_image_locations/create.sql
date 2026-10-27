-- Seed: seed_work_center_image_locations
-- Engine: MySQL 5.7
--
-- Purpose: give each work centre whose photo the plant already keeps on the share its picture by default, so a
--          freshly built store draws the real photo instead of the shared "no image" placeholder.
--
-- How a work centre is matched
--   `config_images_locations.scope_item_id` holds the numeric `setup_work_centers_catalog.id`, not the name, so
--   every row is written by looking that id up from the work centre's name rather than by writing an id here.
--   The file name carries the work centre's own number, zero-padded the way the catalog spells it: `8.jpg` is
--   work centre `100-08`, `4.jpg` is `100-04`, `19.jpg` is `100-19`.
--
-- Four of the ten files map to work centres this store does not hold
--   100-04, 100-05, 100-10 and 100-11 are on the share but not in the catalog, so the join writes nothing for
--   them and nothing is left behind pointing at an id that does not exist. They stay listed: the day Visual adds
--   those work centres to the catalog, re-running this seed gives them their picture with no edit here.
--
-- A picture somebody chose is never overwritten
--   `ON DUPLICATE KEY UPDATE` re-states the item id against the row that is already there, which changes
--   nothing. These rows are defaults, not orders: a picture set on the Pictures screen survives every re-run.
--
-- The paths are absolute and are escaped for MySQL
--   Every backslash is doubled because the server treats a backslash in a string literal as an escape character;
--   a single one would be swallowed and the stored path would not be the one on the share.

USE mtm_waitlist;

SET NAMES utf8mb4;

INSERT INTO
    config_images_locations (
        public_id,
        scope,
        scope_item_id,
        computer_id,
        image_path,
        is_active,
        created_by_user_id,
        updated_by_user_id,
        created_utc,
        updated_utc
    )
SELECT UUID(),
       'work_center',
       CAST(c.id AS CHAR),
       NULL,
       v.image_path,
       1,
       NULL,
       NULL,
       UTC_TIMESTAMP(),
       UTC_TIMESTAMP()
FROM (
    SELECT '100-04' AS work_center_name, 'X:\\MH_RESOURCE\\Material_Handler\\MTM Applications\\MTM Waitlist Application\\Images\\work_center\\4.jpg' AS image_path
    UNION ALL SELECT '100-05', 'X:\\MH_RESOURCE\\Material_Handler\\MTM Applications\\MTM Waitlist Application\\Images\\work_center\\5.jpg'
    UNION ALL SELECT '100-06', 'X:\\MH_RESOURCE\\Material_Handler\\MTM Applications\\MTM Waitlist Application\\Images\\work_center\\6.jpg'
    UNION ALL SELECT '100-08', 'X:\\MH_RESOURCE\\Material_Handler\\MTM Applications\\MTM Waitlist Application\\Images\\work_center\\8.jpg'
    UNION ALL SELECT '100-09', 'X:\\MH_RESOURCE\\Material_Handler\\MTM Applications\\MTM Waitlist Application\\Images\\work_center\\9.jpg'
    UNION ALL SELECT '100-10', 'X:\\MH_RESOURCE\\Material_Handler\\MTM Applications\\MTM Waitlist Application\\Images\\work_center\\10.jpg'
    UNION ALL SELECT '100-11', 'X:\\MH_RESOURCE\\Material_Handler\\MTM Applications\\MTM Waitlist Application\\Images\\work_center\\11.jpg'
    UNION ALL SELECT '100-12', 'X:\\MH_RESOURCE\\Material_Handler\\MTM Applications\\MTM Waitlist Application\\Images\\work_center\\12.jpg'
    UNION ALL SELECT '100-14', 'X:\\MH_RESOURCE\\Material_Handler\\MTM Applications\\MTM Waitlist Application\\Images\\work_center\\14.jpg'
    UNION ALL SELECT '100-19', 'X:\\MH_RESOURCE\\Material_Handler\\MTM Applications\\MTM Waitlist Application\\Images\\work_center\\19.jpg'
) AS v
INNER JOIN setup_work_centers_catalog c ON c.work_center_name = v.work_center_name
ON DUPLICATE KEY UPDATE scope_item_id = scope_item_id;
