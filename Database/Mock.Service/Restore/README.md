# `Database/Mock.Service/Restore` — restore operation scripts

Two reviewed SQL artifacts used by the on-host service's **confirmation-gated, host-only** emergency restore
(`MTM_Waitlist.Mock.Service/Services/RestoreService.cs`, FR-010/FR-023). They are streamed to the `mysql`
client on **stdin** — never passed as `--execute` text and never concatenated into a command line.

| Artifact | Step | What it does |
|---|---|---|
| `replace_database.sql` | 2 of 4 | Drops and recreates the target store in `utf8mb4`, so the replacement is wholesale |
| `verify_restore.sql` | 4 of 4 | Reports the target store's base-table count, so "restored" is demonstrated rather than assumed |

Both carry the `{{database}}` placeholder. `RestoreService` substitutes it with the store's database name from
`BackupStore.ToDatabaseName()` — one of four fixed names, never operator text — writes the result to a
temporary file, and deletes it after the step.

## Recorded deviation from constitution III (stored-procedure-first)

Constitution III requires every data operation to go through a stored procedure, and requires a written
record for any deviation. **Step 2 cannot be a stored procedure:**

- MySQL rejects `CREATE DATABASE` and `DROP DATABASE` inside a stored routine.
- Even if it allowed them, the routine would have to live in the database being dropped — it would drop
  itself, and the reload would then have nothing to restore into.

Consequence: the statement text lives in these reviewed artifacts instead of in application code. That keeps
the part of Principle III that protects this codebase — **no SQL statement text inside C#** (FR-015, SC-013) —
while recording, rather than hiding, the part that the platform makes impossible.

Step 4 **is** expressible as a stored procedure in principle, and is written as a script here so both steps of
this one operation are invoked the same way, through the same placeholder mechanism, with no statement text
in C#. It is read-only (`information_schema`), runs no DDL, and touches no store's data.

Recorded in:

- `specs/001-module-mock-visual-fallback/plan.md` → Complexity Tracking
- `specs/001-module-mock-visual-fallback/tasks.md` → Phase 16 execution note (T138)

## Why the verification is store-agnostic

The previous implementation counted rows in two hard-coded tables. Both were waitlist tables, so restoring
`mtm_mock`, the receiving store, or the WIP store reported only "unverified"; and `core_workstations_registry`
no longer exists in the schema (it is `core_computers_registry`), so even a waitlist restore proved nothing.
The verification now asks the one question that holds for every store — how many base tables it has — and the
caller reports the backup artifact's size beside it. This directory holds no credentials and no connection
string; the MySQL password reaches the client only through the option file referenced by
`ServiceConfiguration.MySqlConnection.PasswordFilePath` (FR-026).
