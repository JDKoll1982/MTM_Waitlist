# SQL naming violations — grandfathered backlog

**Recorded:** 2026-09-20 · **Decision:** **D-7** (owner, 2026-09-20) · **Findings:** **11**
**Guard:** `.github/scripts/validate-sql-naming.ps1` · **Rules:** `.github/instructions/database-schema-rules.instructions.md`

Per decision **D-7** the eleven pre-existing violations are **not renamed now**. They are recorded here as
the backlog for a later normalization pass, and the rules file states plainly that pre-existing violations
are grandfathered while anything **new** still hard-fails the guard. Renaming them is explicitly out of
scope for this pass.

## Why this list did not exist before

The guard's failure path was `foreach ($error in $errors)`. `$Error` is a PowerShell **automatic read-only**
variable, so the moment there was a single finding the script died with
*"Cannot overwrite variable Error"*, exited **1**, and printed **not one violation**. The guard failed
loudly with the wrong message and no findings — a guard that certifies the wrong thing. The loop variable
was renamed (2026-09-20); the guard now prints what it found, and this report is that output.

## How to reproduce

```powershell
powershell -ExecutionPolicy Bypass -File .github/scripts/validate-sql-naming.ps1
```

Exit code `1` with the eleven findings below. The script derives the repository root from its own location
and now refuses to run — exit `1`, with a named path — when the derived root holds no `Database/` folder,
so a copy run from the wrong tree can no longer report a pass.

## The eleven, by rule

### Rule 1 — `Filename '<name>' does not match approved naming patterns` (7)

| # | File | Line |
| --- | --- | --- |
| 1 | `Database/InforVisual/Queues/Module_Mock/Populations/disposition_input_population.sql` | 1 |
| 2 | `Database/InforVisual/Queues/Module_Mock/Populations/inventory_locations_population.sql` | 1 |
| 3 | `Database/InforVisual/Queues/Module_Mock/Populations/operation_sequences_population.sql` | 1 |
| 4 | `Database/InforVisual/Queues/Module_Mock/Populations/subordinate_parts_population.sql` | 1 |
| 5 | `Database/InforVisual/Queues/Module_Mock/Populations/work_order_lookup_population.sql` | 1 |
| 6 | `Database/Mock.Service/Restore/replace_database.sql` | 1 |
| 7 | `Database/Mock.Service/Restore/verify_restore.sql` | 1 |

### Rule 2 — `Boolean column '<name>' must start with is_/has_ or end with _bool for typed setting values` (4)

| # | File | Line | Column |
| --- | --- | --- | --- |
| 8 | `Database/Tables/01_core_users_profiles/create.sql` | 16 | `require_password_change` |
| 9 | `Database/Tables/31_waitlist_request_item_configs/create.sql` | 33 | `requires_answer` |
| 10 | `Database/Tables/AllTables.sql` | 16 | `require_password_change` |
| 11 | `Database/Tables/AllTables.sql` | 673 | `requires_answer` |

Findings 10 and 11 are the aggregate's copies of 8 and 9, so a normalization pass has **two** columns and
**seven** filenames to settle, not eleven independent renames.

## What "grandfathered" means here

- The guard still **fails** on anything new. A new file, a new column or a renamed one is validated as
  written; nothing in this report excuses it.
- Editing one of the files above is expected to fix the violation it touches, and the guard will say so.
- The list is a backlog, not an approval. A later pass takes one rule at a time.
