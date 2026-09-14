-- Rollback: seed_setup_active_jobs_eight_configurations
-- Engine: MySQL 5.7
-- Reverses: Database/Seeds/seed_setup_active_jobs_eight_configurations/create.sql
-- Removes only the seed's own block: the seven situations on the real work centres, plus the seven retired
-- fixture stations, whose rows a database restored from an older seed may still hold. The eighth
-- configuration, a work centre with no active job, was never inserted, so there is nothing to remove for it.

USE mtm_waitlist;

DELETE FROM setup_active_jobs
WHERE work_center IN (
    '100-3', '100-6', '100-7', '100-18', '100-1806', 'V100-33', 'V100-34',
    '900-1', '900-2', '900-3', '900-4', '900-5', '900-6', '900-7');
