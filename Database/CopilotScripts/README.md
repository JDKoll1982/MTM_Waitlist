# Database/CopilotScripts

Reusable PowerShell helpers for the `mtm_mock` cache database (feature
`001-module-mock-visual-fallback`) and for the `mtm_waitlist` application store
(`refresh_schema_drift.ps1`). These exist because the interactive terminal cannot
reliably push multi-line `mysql` invocations — every script here is idempotent and
safe to re-run.

Prefer the `MYSQL_PWD` environment variable over the `-Password` parameter so the
credential never appears in a process command line.

All scripts resolve the repository root from their own location, so they work from any
working directory.

---

## `deploy_mtm_mock.ps1`

Deploys every `mtm_mock` artifact in dependency order:

1. `Bootstrap/create_database.sql`
2. `Tables/<name>/create.sql` — one mirror + one stage twin per shape
3. `StoredProcedures/<name>/create.sql` — the per-shape `sp_visual_<shape>_*` procedures and the two `sp_visual_read_shape_*` reporting procedures
4. `Bootstrap/update_table_descriptions.sql`
5. `Seeds/<name>/create.sql`

```powershell
$env:MYSQL_PWD='root'; ./Database/CopilotScripts/deploy_mtm_mock.ps1
./Database/CopilotScripts/deploy_mtm_mock.ps1 -Server localhost -Port 3306 -User root
```

## `build_mtm_mock_masters.ps1`

Regenerates the master lists (`AllTables.sql`, `AllSPs.sql`, `AllSeeds.sql`) from the
file-per-artifact sources. Run this after adding or changing an artifact.

`-Check` regenerates in memory and exits non-zero if a committed master list is stale —
use it as a verification gate.

Requires **PowerShell 7+ (`pwsh`)** and refuses to run under Windows PowerShell 5.1. 5.1 decodes
this BOM-less script with the ANSI codepage, so an em dash in the generated header becomes mojibake,
and its `-Encoding utf8` writes a BOM — silently rewriting all three masters. `-Check` cannot catch
that, because 5.1 mangles both sides of its comparison identically and still reports "up to date".

```powershell
pwsh -NoProfile -File Database/CopilotScripts/build_mtm_mock_masters.ps1
pwsh -NoProfile -File Database/CopilotScripts/build_mtm_mock_masters.ps1 -Check
```

## `verify_mtm_mock_deploy.ps1`

The end-to-end gate for the cache database. It checks master-list freshness, drops and
redeploys `mtm_mock` **from the master lists only**, then asserts:

| Check | Requirement |
|---|---|
| 14 tables, 16 procedures deploy | Schema + procedure completeness (seven shapes) |
| The freshness report enumerates every shape | A shape the report omits is reported as seed-only content however successfully it refreshes |
| Every shape refreshes and reads back with an identical result shape | FR-004 |
| No transient `_prev` table persists | FR-006 / SC-005 |
| No seed rows remain after refresh | FR-006 / FR-017 |
| The live mirror holds a complete sentinel snapshot | FR-006 / SC-005 |
| Master lists are in sync with their sources | DB ruleset |

```powershell
$env:MYSQL_PWD='root'; pwsh -NoProfile -File Database/CopilotScripts/verify_mtm_mock_deploy.ps1
```

> **Destructive**: this script drops `mtm_mock` first. It never touches `mtm_waitlist`,
> `mtm_wip_application_winforms`, or `mtm_receiving_application`.
>
> Because it drops the mirror, **do not run it on a host whose cache is warm** — the plant host is one. There the
> non-destructive checks are the ones to run: `build_mtm_mock_masters.ps1 -Check` for the master lists, and
> `Database/Mock/Validation/read_shape_metadata/validate.sql` (read-only) for the shape and freshness reports.
> This script's place is a development store that can be rebuilt.

---

## `capture_mock_seed_baseline.ps1`

Re-captures `Database/Mock/Seeds/seed_visual_mirror_baseline/create.sql` from a live `mtm_mock` mirror, then
regenerates `AllSeeds.sql` from it.

That seed is a **capture**, not hand-written content: a fresh install has never been refreshed, so without
baseline rows the very first fallback read would serve an empty set for every journey (FR-017). A capture goes
stale whenever the mirror's own refreshes change shape, and this one had — it still carried 79,218 shape-4 rows
that were overwhelmingly zero-stock, left over from before the population read was narrowed to locations holding
stock (T150/T151). This script is the remedy that the note in
`specs/001-module-mock-visual-fallback/tasks.md` left with the operator, because it needs the host the mirror
lives on.

**Read-only against the database.** It never writes to `mtm_mock`, and never touches the internal stores. The only
files it writes are the seed and the master list generated from it.

```powershell
# Report what would be captured and write nothing
pwsh -NoProfile -File Database/CopilotScripts/capture_mock_seed_baseline.ps1 -DryRun

# Capture
$env:MYSQL_PWD='root'; pwsh -NoProfile -File Database/CopilotScripts/capture_mock_seed_baseline.ps1
```

- Connection comes from `-Server`/`-User`/`-Password`, else `MTM_WAITLIST_DB_CONNECTION_STRING` (the
  application's own source of truth, so a workstation pointed at a remote mirror captures that mirror), else
  localhost/root.
- MySQL quotes every value (`QUOTE`), so an apostrophe, a tab or a newline inside a description round-trips into
  the file instead of breaking the statement. Every row is written with `is_seed_content = 1`: a copied row is
  not a refresh, and that flag is what lets the read-status surface say "seeded content, not yet refreshed"
  rather than reporting a fabricated cache age (FR-022).
- **It refuses to write a capture it cannot prove.** The rows read must equal the live `COUNT(*)`; a shape the
  mirror holds zero rows for is an error rather than a silent deletion of good baseline rows from the seed
  (`-AllowEmptyShape` overrides only the second of those).
- Shape 6 (`employee_by_number`) is **excluded by default**, because it carries real employee numbers and names
  and the locked ruleset keeps raw production user data out of non-production seed content.
  `-IncludeEmployeeShape` captures it anyway.
- `rollback.sql` is not regenerated: it deletes by `is_seed_content = 1` per shape, so it already covers whatever
  this writes.
- `-SkipMasterRebuild` leaves `AllSeeds.sql` alone. Do not use it unless regenerating by hand — `AllSeeds.sql`
  concatenates the seed verbatim and is stale the moment the seed changes, and `build_mtm_mock_masters.ps1 -Check`
  is the gate that catches it.

---

## `refresh_schema_drift.ps1`

Reports — and optionally repairs — a store whose **deployed stored procedures have fallen behind the
repository's own `create.sql` artifacts**.

This exists because that drift is **invisible at run time**. MySQL accepts a call that passes **more**
parameters than the deployed routine declares, **silently ignores the surplus, and reports success**: a call
site that passes a parameter the routine does not have writes its other columns, returns a healthy result, and
loses that one value with nothing logged, nothing thrown and nothing on screen to suggest anything is missing.
Found the hard way on 2026-10-11 (feature `012-waitlist-card-layout`): the app's per-Item answers had been
discarded on every request since the store was last deployed, and the die-shop cell they feed never appeared.

```powershell
# Report (changes nothing)
pwsh -NoProfile -File Database/CopilotScripts/refresh_schema_drift.ps1

# Repair only what was proved wrong
pwsh -NoProfile -File Database/CopilotScripts/refresh_schema_drift.ps1 -Apply `
    -Procedure sp_waitlist_request_insert,sp_waitlist_request_list

# Repair every routine in the schema, then re-seed one seed by name
pwsh -NoProfile -File Database/CopilotScripts/refresh_schema_drift.ps1 -Apply `
    -Seed seed_waitlist_request_item_configs
```

- Defaults to `mtm_waitlist` and to `$env:MTM_WAITLIST_DB_CONNECTION_STRING` (what the application itself
  resolves). The script probes the configured server, then this machine, so it follows the same rule as
  `MTM_Waitlist.Core/Services/MySqlHostFallback` — the app falls back to `localhost` when the plant host is
  unreachable, and the store it is actually reading is the one that has to be current.
- Reports **missing** and **parameter-drifted** routines. It deliberately does **not** claim body drift: a
  changed body cannot be told from reformatting this way, so `-Apply` is the remedy — it rewrites every routine
  in the schema from the repository, which is the only way to be sure a body matches its artifact.
- Seeds are never applied implicitly; each one is named with `-Seed`, because a seed is a data change.
- The password reaches `mysql.exe` through a temporary client option file outside the repository, never a
  command line.

## Related validation SQL

Diagnostic scripts (not part of the deploy path) live beside their artifacts:

- `Database/Mock/Validation/refresh_swap_smoke/validate.sql` — FR-006 atomic-swap proof
- `Database/Mock/Validation/all_shapes_roundtrip/validate.sql` — FR-004 shape parity proof

## Conventions

- **MySQL 5.7 syntax** is the target (production host); the local dev server is 9.6, so
  every artifact is verified to run on both.
- Each artifact ships `create.sql` + `rollback.sql` in the same change and is registered
  in the master lists (locked DB ruleset).
- Never edit `AllTables.sql` / `AllSPs.sql` / `AllSeeds.sql` by hand — regenerate them.

## Deviation from the locked ruleset (approved)

The locked ruleset describes the master lists as hand-maintained. For the `mtm_mock` cache they are generated
by `build_mtm_mock_masters.ps1` instead, and that deviation is recorded with its rationale in
`Database/Database-Ruleset.md` → *Artifact Layout and Release Governance* → *Review note — generated master
lists for the `mtm_mock` cache* (task T121 of `specs/001-module-mock-visual-fallback`).

In short:

- The three `Database/Mock/*.sql` aggregates are generated from the file-per-artifact sources in dependency
  order; the internal-store aggregates under `Database/Tables`, `Database/StoredProcedures` and `Database/Seeds`
  remain hand-maintained and are edited directly.
- `-Check` is the enforcement: it regenerates in memory and exits non-zero when a committed list is stale, so a
  forgotten regeneration fails the deploy gate instead of shipping a diverged aggregate.
- The artifacts that must still move by hand are unchanged: every `create.sql` ships with its `rollback.sql`,
  and `Database/Mock/Bootstrap/update_table_descriptions.sql` is updated in the same change (constitution III).
