-- Rollback: seed_setup_active_jobs_eight_configurations
-- Engine: MySQL 5.7
-- Retired: the seed no longer ships a create.sql, so nothing inserts these rows into a fresh store. This file
--          stays as the cleanup for a database that already holds them, and as the record of what the seed used
--          to deploy.
--
-- Why it was retired
--   The seven situations existed to drive the Item picker's visibility matrix through the real read path. That
--   matrix is now proved by `MTM_Waitlist.Tests/Module_Setup/Services/ActiveJobSeedRoundTripTests`, which writes
--   its own rows under work centres it owns and deletes them again, so the deployed seed was only ever putting
--   invented work orders on real work centres: every card then said a job was running that nobody had set up.
--   A freshly built store now has no active job, so every work centre reads "None" until a real Setup save
--   records one.
--
-- What it removes
--   The seven situations on the real work centres, plus the seven retired `900-*` fixture stations, whose rows a
--   database restored from an older seed may still hold. The eighth configuration, a work centre with no active
--   job, was never inserted, so there is nothing to remove for it.

USE mtm_waitlist;

DELETE FROM setup_active_jobs
WHERE work_center IN (
    '100-03', '100-06', '100-07', '100-18', '100-1806', 'V100-33', 'V100-34',
    '900-1', '900-2', '900-3', '900-4', '900-5', '900-6', '900-7');