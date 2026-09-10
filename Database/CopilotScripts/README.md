# Database/CopilotScripts

Reusable PowerShell helpers for the `mtm_mock` cache database (feature
`001-module-mock-visual-fallback`). These exist because the interactive terminal cannot
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
