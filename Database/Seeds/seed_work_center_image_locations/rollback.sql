-- Rollback: seed_work_center_image_locations
-- Engine: MySQL 5.7
-- Reverses: Database/Seeds/seed_work_center_image_locations/create.sql
--
-- Removes exactly the ten share paths the seed writes, and nothing else: a picture somebody chose for a work
-- centre on the Pictures screen is a different path, so it is left alone. A path this seed never wrote therefore
-- survives the rollback, which is the direction a rollback should fail in.
--
-- No row is matched by work centre, because four of the ten map to work centres the catalog does not hold and a
-- rollback keyed by name would silently skip whatever the catalog gained since.

USE mtm_waitlist;

DELETE FROM config_images_locations
WHERE
    scope = 'work_center'
    AND image_path IN (
        'X:\\MH_RESOURCE\\Material_Handler\\MTM Applications\\MTM Waitlist Application\\Images\\work_center\\4.jpg',
        'X:\\MH_RESOURCE\\Material_Handler\\MTM Applications\\MTM Waitlist Application\\Images\\work_center\\5.jpg',
        'X:\\MH_RESOURCE\\Material_Handler\\MTM Applications\\MTM Waitlist Application\\Images\\work_center\\6.jpg',
        'X:\\MH_RESOURCE\\Material_Handler\\MTM Applications\\MTM Waitlist Application\\Images\\work_center\\8.jpg',
        'X:\\MH_RESOURCE\\Material_Handler\\MTM Applications\\MTM Waitlist Application\\Images\\work_center\\9.jpg',
        'X:\\MH_RESOURCE\\Material_Handler\\MTM Applications\\MTM Waitlist Application\\Images\\work_center\\10.jpg',
        'X:\\MH_RESOURCE\\Material_Handler\\MTM Applications\\MTM Waitlist Application\\Images\\work_center\\11.jpg',
        'X:\\MH_RESOURCE\\Material_Handler\\MTM Applications\\MTM Waitlist Application\\Images\\work_center\\12.jpg',
        'X:\\MH_RESOURCE\\Material_Handler\\MTM Applications\\MTM Waitlist Application\\Images\\work_center\\14.jpg',
        'X:\\MH_RESOURCE\\Material_Handler\\MTM Applications\\MTM Waitlist Application\\Images\\work_center\\19.jpg'
    );
