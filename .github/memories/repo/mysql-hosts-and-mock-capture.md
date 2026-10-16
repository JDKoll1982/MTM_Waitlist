# MySQL hosts reachable from this workstation, and the mock baseline capture

Recorded 2026-10-16. Two **different** MySQL servers answer from this workstation, and confusing them is
the expensive mistake this note exists to prevent.

| Address | Version | What is there |
| --- | --- | --- |
| `localhost` | **8.0.46** | A pristine development instance. Schemas: `mysql`, `information_schema`, `performance_schema`, `sys`, `sakila`, `world`. **No `mtm_mock`, no `mtm_waitlist`** until something deploys them. |
| `172.16.1.104` | **5.7.24** | The plant host `V-MTMFG-5`. Every operational store, and the **warm** `mtm_mock` mirror. |

Verified by comparing `@@server_uuid` on both, so this is not an inference from an address looking local —
`localhost` and `172.16.1.104` are genuinely separate processes.

**Why it matters.** `localhost` is the safe place to *prove* a database artifact loads. Building `mtm_mock`
there and loading a seed into it touches nothing live:

```powershell
$env:MYSQL_PWD='root'
pwsh -NoProfile -File Database/CopilotScripts/deploy_mtm_mock.ps1 -Server localhost
```

Never use the plant host for that. `verify_mtm_mock_deploy.ps1` **drops `mtm_mock`**, and the plant mirror is
warm — the script's own README says its place is a development store that can be rebuilt.

Authentication is `root` with the password in `MYSQL_PWD` (the application's own connection string lives in
`MTM_WAITLIST_DB_CONNECTION_STRING`; password values are not recorded here).

## `Database/CopilotScripts/capture_mock_seed_baseline.ps1`

Regenerates `Database/Mock/Seeds/seed_visual_mirror_baseline/create.sql` from a live mirror, then rebuilds
`AllSeeds.sql`. This is the fix for the open question filed at the end of
`specs/001-module-mock-visual-fallback/tasks.md` Phase 24: the committed capture predated the shape-4
population narrowing (T150/T151) and carried 79,218 overwhelmingly zero-stock rows. Re-captured 2026-10-16:
**83,224 → 12,990 rows**, shape 4 **79,218 → 1,623**.

Read-only against the database. It refuses to write when the rows it read do not equal the live `COUNT(*)`,
or when a shape is empty. Shape 6 (`employee_by_number`) is excluded by default — real employee data, and the
locked ruleset keeps production user data out of seed content — opt in with `-IncludeEmployeeShape`.

### Two traps this script fell into, worth knowing before writing another capture

- **`mysql` batch output puts the row terminator BEFORE the next record, not after this one.** Splitting a
  stream on a custom record separator therefore leaves a leading `\r\n` on every record but the first, so
  `.TrimEnd()` is the wrong end and each tuple after the first lands at column 0. **Loading the file still
  succeeds**, because a newline is legal whitespace in the middle of a statement — so a "load verified" check
  passes on a malformed file and only reading the output catches it.
- **A SQL literal inside the generating `SELECT` is consumed by the parser.** To make the emitted text carry
  quotes — a `datetime` value, say — the expression must be `QUOTE('<value>')`, not `'<value>'`. The bare form
  emits `2026-10-16 12:43:32` and the client rejects the file at line 1 of the first statement.
