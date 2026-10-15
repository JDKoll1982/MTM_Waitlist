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
2. `Tables/<name>/create.sql` — 5 mirrors + 5 stage twins
3. `StoredProcedures/<name>/create.sql` — the ten `sp_visual_*` procedures
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
| 10 tables, 10 procedures deploy | Schema + procedure completeness |
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
