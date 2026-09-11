-- ========================================
-- Module: On-host service — emergency restore
-- Artifact: verify_restore.sql
-- Purpose: Step 4 of a confirmed restore — demonstrate that the reload actually produced a store, instead
--          of trusting the client's exit code alone.
-- Target: MySQL 5.7 (the host's own server)
-- Invoked by: MTM_Waitlist.Mock.Service/Services/RestoreService.cs (streamed to the mysql client on stdin)
--
-- Placeholder: {{database}} — see replace_database.sql for the provenance and the injection argument.
--
-- What this reports, and why it is store-agnostic:
--   The earlier implementation hard-coded two table names and counted their rows. Both were waitlist
--   tables, so restoring mtm_mock / the receiving store / the WIP store reported nothing but
--   "unverified"; and one of the two, core_workstations_registry, no longer exists in the schema (it is
--   core_computers_registry), so even a waitlist restore never proved anything. A verification step that
--   cannot work for three of the four stores is worse than none, because it reads as evidence.
--   This script therefore asks the only question that is true for every store: "how many base tables does
--   this database have now?" A reload that failed silently leaves 0, and the reload itself is exact
--   (a whole-database mysqldump artifact, loaded with no --force) — so the table count is the right
--   post-condition, and the caller also reports the artifact's size so the two numbers can be read
--   together.
--
-- Read-only: information_schema only. No flag is needed; the default tab-separated output is parsed by
-- the caller.
-- ========================================

SELECT COUNT(*) AS table_count
FROM information_schema.TABLES
WHERE TABLE_SCHEMA = '{{database}}'
  AND TABLE_TYPE = 'BASE TABLE';
