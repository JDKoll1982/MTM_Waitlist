-- Rollback for seed: seed_plant_buildings_default
--
-- Every row in `core_computer_buildings` was written either by this seed or by a computer's own setup, and the
-- two cannot be told apart afterwards, so the rollback removes both: reverting this seed is reverting the
-- feature, whose table rollback drops the table anyway.

USE mtm_waitlist;

DELETE FROM core_computer_buildings;

DELETE FROM core_buildings_catalog
WHERE building_name IN ('Expo Drive', 'Vits Drive');
