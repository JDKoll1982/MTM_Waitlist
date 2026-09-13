-- Rollback: seed_setup_active_jobs_eight_configurations
-- Engine: MySQL 5.7
-- Reverses: Database/Seeds/seed_setup_active_jobs_eight_configurations/create.sql
-- Removes only the seed's own work-centre block; 900-8 was never inserted, so there is nothing to remove for
-- the eighth configuration.

USE mtm_waitlist;

DELETE FROM setup_active_jobs
WHERE work_center IN ('900-1', '900-2', '900-3', '900-4', '900-5', '900-6', '900-7');
