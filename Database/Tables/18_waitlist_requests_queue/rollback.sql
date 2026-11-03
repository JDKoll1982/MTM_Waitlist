-- Rollback for table: waitlist_requests_queue
-- Engine: MySQL 5.7
-- Drops the table wholesale. This table's rollback is the drop artifact it has always been: the re-key is a
-- column change on a table the reinstall recreates from seed, so reversing it means removing the table this
-- repo's seed and procedures are written against.
-- The two columns specs/012-waitlist-card-layout added (`handler_status`, `answers_json`) go with the table and
-- need no per-column drop: there is no path in this repository that removes only the columns of a table the
-- reinstall recreates. The same holds for the four changes specs/016-generic-wc-request-foundation makes
-- (`active_setup_job_id` nullable, `is_generic`, `description`, `accepted_warnings_json`).

USE mtm_waitlist;

DROP TABLE IF EXISTS waitlist_requests_queue;
