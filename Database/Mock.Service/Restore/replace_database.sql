-- ========================================
-- Module: On-host service — emergency restore
-- Artifact: replace_database.sql
-- Purpose: Step 2 of a confirmed restore — replace the target store wholesale, in utf8mb4.
-- Target: MySQL 5.7 (the host's own server)
-- Invoked by: MTM_Waitlist.Mock.Service/Services/RestoreService.cs (streamed to the mysql client on stdin)
--
-- Placeholder: {{database}} is substituted with the store's database name before the script is streamed.
--   The value is never operator text: it comes from BackupStore.ToDatabaseName(), one of the four fixed
--   store names (mtm_waitlist, mtm_wip_application_winforms, mtm_receiving_application, mtm_mock).
--
-- WHY THIS IS A SCRIPT AND NOT A STORED PROCEDURE (recorded deviation — constitution III)
--   Constitution III requires every data operation to go through a stored procedure. This step cannot be
--   one. MySQL refuses `CREATE DATABASE` / `DROP DATABASE` inside a stored routine, and even if it did not,
--   the routine would have to live in the very database being dropped, so it would drop itself. The
--   deviation is recorded in Database/Mock.Service/Restore/README.md and in
--   specs/001-module-mock-visual-fallback/plan.md (Complexity Tracking), which is the written record
--   constitution Governance requires for a deviation.
--
-- Consequence of the deviation: the statement text lives here, in a reviewed artifact, rather than in C#.
--   RestoreService reads this file, substitutes the placeholder, and streams the result to the client on
--   stdin — so no SQL statement text remains in application code (FR-015, SC-013).
--
-- No flags: if either statement fails, the client exits non-zero and the restore stops before the reload.
-- The reload deliberately does not pass --force, so a genuine error stops the restore instead of being
-- skipped over, and the operator keeps the safety snapshot taken immediately before this step.
-- ========================================

DROP DATABASE IF EXISTS {{database}};

CREATE DATABASE {{database}} CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
