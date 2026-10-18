# What to run on the live database and the Mock host

Written 2026-10-14 for the working tree at `012-waitlist-card-layout`
(= `master` + 2 documentation-only commits: the startup rebuild brief and the spec-kit
plan-validation scripts).

## How this was worked out

The branch itself carries no code or schema change, so "the changes on this branch" was read as
*the current build* and compared against what is actually running:

| Compared | Against | Result of the comparison |
| --- | --- | --- |
| `Database/` artifacts (file-per-artifact `create.sql` / `rollback.sql`) | the live MySQL server `172.16.1.104` (5.7.24) | 1 missing procedure, 1 missing column rename, 5 stale procedures |
| `Database/Mock/` artifacts | `mtm_mock` on that server | already current — nothing to do |
| Repo version (`1.1.0`) | the running `MTM_Waitlist.Mock.Service` (status API `http://172.16.1.104:5760/api/status`) | the running service is the **1.0.0** build from 2026-10-05 |
| Procedure names called by production code | every routine in the four stores | exactly one routine is called but does not exist |

Live `mtm_waitlist` objects were all created or last altered on **2026-10-05 07:11**, i.e. one full
redeploy before the 10 October refactor. That is the whole cause of the drift below: the live store
is five days behind the repository.

---

## Part 1 — Live MySQL (`mtm_waitlist` on 172.16.1.104)

### 1.1 Required — rename one column

```sql
ALTER TABLE core_computers_registry
    CHANGE COLUMN description location VARCHAR(255) NULL;
```

The 10 October commit renamed this column in `Database/Tables/02_core_computers_registry/create.sql`
(`description` → `location`). The app now reads and writes `location`. Live still has `description`.
`CREATE TABLE IF NOT EXISTS` cannot fix this, so the rename has to be issued by hand.

### 1.2 Required — add one missing procedure

Apply `Database/StoredProcedures/sp_auth_user_previous_sign_ins_list/create.sql`.

`MTM_Waitlist.Startup/Services/PreviousSignInService.cs` calls it to build the "people who signed in
here before" list. It does not exist on live, so that read fails today.

### 1.3 Required — replace five procedures

Each file contains its own `DROP PROCEDURE IF EXISTS` plus the `CREATE`, so applying the file is the
whole job:

* `Database/StoredProcedures/sp_core_computers_registry_get_all/create.sql`
* `Database/StoredProcedures/sp_core_computers_registry_lookup_by_name_get/create.sql`
* `Database/StoredProcedures/sp_core_computers_registry_update/create.sql`
* `Database/StoredProcedures/sp_core_computers_registry_upsert/create.sql`
* `Database/StoredProcedures/sp_machine_configuration_reset/create.sql`

All five are still on the old vocabulary: the three registry procedures select and write
`description`, and `sp_machine_configuration_reset` declares `IN p_reset_description`, while the app
passes `p_reset_location`. The machine-configuration flow cannot work until these are replaced.

Apply **1.1 before 1.3**, or both in the same sitting — the procedures name the new column.

### 1.4 How to run 1.1 – 1.3

Set the password in the environment (the same one `appsettings.json` uses) rather than on the command
line, and pipe the file in with `<`: five of these files use `DELIMITER $$`, which the `mysql` client
honours only when the script arrives on stdin.

```powershell
$env:MYSQL_PWD = '<password from appsettings.json>'
cd C:\Users\jkoll\source\repos\MTM_Waitlist
$mysql = 'C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe'

& $mysql -h 172.16.1.104 -P 3306 -u root mtm_waitlist -e "ALTER TABLE core_computers_registry CHANGE COLUMN description location VARCHAR(255) NULL;"

& $mysql -h 172.16.1.104 -P 3306 -u root mtm_waitlist < Database\StoredProcedures\sp_auth_user_previous_sign_ins_list\create.sql
& $mysql -h 172.16.1.104 -P 3306 -u root mtm_waitlist < Database\StoredProcedures\sp_core_computers_registry_get_all\create.sql
& $mysql -h 172.16.1.104 -P 3306 -u root mtm_waitlist < Database\StoredProcedures\sp_core_computers_registry_lookup_by_name_get\create.sql
& $mysql -h 172.16.1.104 -P 3306 -u root mtm_waitlist < Database\StoredProcedures\sp_core_computers_registry_update\create.sql
& $mysql -h 172.16.1.104 -P 3306 -u root mtm_waitlist < Database\StoredProcedures\sp_core_computers_registry_upsert\create.sql
& $mysql -h 172.16.1.104 -P 3306 -u root mtm_waitlist < Database\StoredProcedures\sp_machine_configuration_reset\create.sql
```

### 1.5 Verify Part 1

```sql
SELECT COUNT(*) FROM information_schema.routines
 WHERE routine_schema = 'mtm_waitlist'
   AND routine_name = 'sp_auth_user_previous_sign_ins_list';          -- expect 1

SELECT column_name FROM information_schema.columns
 WHERE table_schema = 'mtm_waitlist' AND table_name = 'core_computers_registry'
   AND column_name IN ('description', 'location');                    -- expect only: location

SELECT COUNT(*) FROM information_schema.routines
 WHERE routine_schema = 'mtm_waitlist'
   AND routine_name IN ('sp_auth_remembered_sign_ins_get',
                        'sp_auth_remembered_sign_ins_upsert',
                        'sp_auth_remembered_sign_ins_clear');         -- expect 0 after 1.6
```

### 1.6 Optional cleanup — the retired remembered-sign-ins feature

The feature was removed from the repository, but three procedures and one table are still live. Nothing
calls them any more, so this is tidiness rather than a fix. No foreign key points at the table, so the
order below is safe.

```powershell
& $mysql -h 172.16.1.104 -P 3306 -u root mtm_waitlist < Database\StoredProcedures\sp_auth_remembered_sign_ins_get\rollback.sql
& $mysql -h 172.16.1.104 -P 3306 -u root mtm_waitlist < Database\StoredProcedures\sp_auth_remembered_sign_ins_upsert\rollback.sql
& $mysql -h 172.16.1.104 -P 3306 -u root mtm_waitlist < Database\StoredProcedures\sp_auth_remembered_sign_ins_clear\rollback.sql
& $mysql -h 172.16.1.104 -P 3306 -u root mtm_waitlist < Database\Tables\35_auth_remembered_sign_ins\rollback.sql
```

### 1.7 Optional cleanup — one leftover table

`mtm_waitlist.seed_username_case_backup` (10 rows) exists on live but is not a repository artifact: it
is the "before" copy the username-normalisation seed made. Drop it only once you are satisfied you no
longer want that rollback record.

### 1.8 Recommended — the image-storage settings rows are missing

Live `config_settings_values` holds no `image_storage.*` keys at all, and `config_images_locations` is
empty. The app falls back to the defaults compiled into the build, so this is **not** a failure — but
Settings → picture storage will show those defaults instead of the plant's folder.

> **Do not run `Database/Seeds/seed_config_image_storage_settings/create.sql` as it stands.** Its first
> statement `TRUNCATE`s `config_settings_values`, and on live that table holds **171 role-scoped
> permission rows and 5 user-scoped rows**. The seed would erase them (only the role baselines are put
> back by the seed that follows it, and nothing puts the user-scoped row back).

Insert just the four keys instead, or run the file's `INSERT … ON DUPLICATE KEY UPDATE` statement and
skip the `TRUNCATE`/`FOREIGN_KEY_CHECKS` block above it. Back the table up first either way:

```sql
CREATE TABLE config_settings_values_backup_20261014 AS SELECT * FROM config_settings_values;
```

### 1.9 Applied 2026-10-16 — the two columns this branch adds to `waitlist_requests_queue`

**Already done on the host. This is the record, and the reversal.** `specs/012-waitlist-card-layout` adds exactly
two columns, and live had **neither** while its routines already used **both**. That combination is worse than a
plain gap, because nothing here fails loudly in the app:

- `sp_waitlist_request_list` failed outright with `Unknown column 'q.handler_status' in 'field list'` — so the
  waitlist screen could not be read at all on this branch.
- `sp_waitlist_request_handler_status_update` failed the same way, so a handler could never set a condition.
- `sp_waitlist_request_insert` carries `p_answers_json`, so a new request would have failed on insert.

Confirmed absent first (with the 21 request rows present), then applied as one statement:

```sql
ALTER TABLE mtm_waitlist.waitlist_requests_queue
  ADD COLUMN answers_json JSON NULL
    COMMENT 'The answers a request captured beyond input_value, keyed by the label the Item declared; NULL when the request captured none (FR-073, FR-087).' AFTER input_value,
  ADD COLUMN handler_status VARCHAR(32) NULL
    COMMENT 'The condition the assigned material handler reported: one of the four verbatim condition strings, or NULL when no condition is set. Deliberately separate from status (FR-012, FR-070).' AFTER status;
```

Both definitions are copied verbatim from `Database/Tables/18_waitlist_requests_queue/create.sql`, which is the
source of truth for the table. Verified afterwards: the columns exist with those types and comments,
`sp_waitlist_request_list` returns its 21 rows, and the handler-condition write reaches the store.

**Reversal.** Dropping them discards operator data — any condition a handler has set, and any answers a request
captured — so it is not a tidy-up:

```sql
ALTER TABLE mtm_waitlist.waitlist_requests_queue
  DROP COLUMN handler_status,
  DROP COLUMN answers_json;
```

There is no migration runner in this repository, so this was applied by hand rather than through an artifact. It is
additive: no existing row changed, and both columns are nullable with no default.

---

## Part 2 — The Mock service on the host

### 2.1 `mtm_mock` itself needs nothing

The mirror schema is already current: all 12 tables (six `_result` mirrors plus their six `_stage`
twins) and all 14 `sp_visual_*` procedures match the repository. The two `config_images_locations_history`
triggers belong to `mtm_waitlist` and also match. **There is no `mtm_mock` script to run.**

> **Correction 2026-10-16 — "current" was measured against six shapes, and the repository has seven.** The
> count above (12 tables, 14 procedures) describes shapes 1–6 only. `part_warehouse_quantities`, added by
> feature 012, was absent from the host entirely: no mirror table, no stage twin, no `_get`, no `_refresh`.
> Its artifacts were deployed on their own — the two table `create.sql` files, then its two procedures, then
> `Database/Mock/Bootstrap/update_table_descriptions.sql` — leaving the existing six shapes' mirrors
> untouched, and the service populated the shape on its next refresh (8 746 rows). The host now holds
> **14 tables and 16 procedures**, which is the repository's own set.
### 2.2 The running service is an older build

Its own status API reports `"serviceVersion":"1.0.0"`, started **2026-10-05 12:19**; the repository is
at **1.1.0** and has taken further changes since. Republish and redeploy it **on the host itself**
(`172.16.1.104`) — the deploy script decides what the machine can do from what it can reach, so it
belongs on the cache host, not on this workstation:

```powershell
cd <repo>\MTM_Waitlist.Mock.Service
pwsh -NoProfile -ExecutionPolicy Bypass -File .\deploy\install-mock-service.ps1 -Publish
```

That publishes a fresh Release output, deploys it, re-proves the secrets, restarts the service and
health-checks it. Run it with no argument instead if you would rather answer its questions first. It
keeps the service's stored state (configuration, backups, run records) unless you pass `-PurgeState`.

### 2.3 Then refresh the mirror and check every shape

```powershell
# ask an operator account to run a refresh; the API is role-gated
Invoke-WebRequest -Method Post -Uri 'http://172.16.1.104:5760/api/refresh' `
  -Headers @{ 'X-MTM-Mock-User' = 'JOHNK' }

# and read the result
Invoke-WebRequest -Uri 'http://172.16.1.104:5760/api/status' `
  -Headers @{ 'X-MTM-Mock-User' = 'JOHNK' }
```

Expect six shapes with a recent `refreshedUtc` and `"isSeedContentOnly": false`.

**One shape is currently wrong on the host even though the service calls its run a success.**
`employee_by_number` reports `lastOutcome: succeeded` and 2 084 rows, but `refreshedUtc` is `null` and
`isSeedContentOnly` is `true` — meaning the mirror is still serving the seeded baseline and has never
held a refreshed snapshot, while the other five shapes were refreshed successfully today at 11:00.
Check that shape specifically after the redeploy; if it still reports seed-only content, the refresh
for it is reporting success without swapping a snapshot.

> **Resolved 2026-10-16 — the last sentence above was the right instinct aimed at the wrong thing.** The
> refresh was never broken; the *freshness report* was. `sp_visual_read_shape_freshness_get` enumerates
> the shapes by hand, it shipped with five branches, and neither feature 011 nor feature 012 added the
> branch its new shape needed — so the report returned five rows and the status payload fell through to
> its `refreshedUtc: null` / `isSeedContentOnly: true` defaults for the shape it could not find. The
> mirror held real refreshed rows throughout. Fixed by adding the two missing branches, regenerating
> `Database/Mock/AllSPs.sql` through the builder, and pinning the enumeration against the shape catalog
> with a test that fails the build when a shape is added without its branch; the service was republished
> to **1.1.0** and redeployed on the host, where all **seven** shapes now report a fresh snapshot. Full
> record: `docs/defects/defects-mock-medium-service-status-reported-refreshed-shapes-as-seed-only.md`.

---

## Part 3 — Checked and already correct (no action)

* **`mtm_mock`**: 12 tables, 14 procedures, all matching.
* **`mtm_waitlist`**: 88 procedures, 3 functions, 2 views and 21 tables otherwise match. The two views
  and two functions differ only in text MySQL itself normalises (database-qualified column names,
  `ALGORITHM=UNDEFINED`, `SQL SECURITY DEFINER`, the order of `NO SQL` and `DETERMINISTIC`, and
  CR/LF/TAB escape representation) — the logic is identical.
* **Triggers**: both `config_images_locations_history` triggers exist and match.
* **`mtm_receiving_application` / `mtm_wip_application_winforms`**: every procedure the app calls
  exists (including `sp_Dunnage_Parts_Insert`, `sp_Dunnage_Types_Insert`,
  `sp_receiving_dunnage_types_get_all`, `sp_receiving_history_average_coil_weight`), and the dunnage
  tables are present. Nothing to run.
* **Seeds already applied on live**: role catalogue with no `admin` code, permission baselines
  (171 role rows), waitlist item configs (24), work centres (25), active jobs (8).
* **Infor Visual is reachable from the host** (`"visualSourceReachable": true`), so the refresh has a
  live source to read.

## Part 4 — What this check does not cover

* Building or republishing the desktop app itself — that is your local build, and this file says
  nothing about it.
* The Infor Visual side: the probe says it answers, but no Visual object was inspected.
* The procedure *bodies* in `mtm_receiving_application` and `mtm_wip_application_winforms`. Those are
  legacy stores with 200+ procedures each; only the handful the app calls were checked, and by name.
* `Database/Bootstrap/update_table_descriptions.sql` was not applied to live and is not needed for the
  build to run (it is documentation metadata).

---

## In order

> **Status, 2026-10-16 (added by the agent that ran this list on the host `V-MTMFG-5`).** Steps **1–4 and 7–8 are
> done**; the notes below say what was observed and what is left. Step 4's verification queries return what this
> file expects. **Step 9 is still yours**, and it is the one that needs a person at the keyboard.
>
> * **1–3 (the required repairs).** Applied: the `description` → `location` rename, and the routine set from
>   §1.2/§1.3 — `sp_auth_user_previous_sign_ins_list` now exists, the three registry procedures and
>   `sp_machine_configuration_reset` now use `location`/`p_reset_location`, and all seven `location` values
>   survived the rename. They were applied as part of recovering the store from an incident: on 2026-10-16 the
>   schema validator's repair path applied `Database/Bootstrap/create_database.sql` — which begins with
>   `DROP DATABASE IF EXISTS` — to this store, dropping every table, and the store was restored from the Mock
>   service's 01:00 dump before this list was re-applied. Full record:
>   `docs/defects/defects-database-critical-schema-validator-dropped-the-live-waitlist-store.md`. The validator is fixed and is
>   now read-only unless `-ApplySchema` is passed.
> * **4.** Run on the restored store: `core_computers_registry` carries **only** `location`,
>   `sp_auth_user_previous_sign_ins_list` returns 1 row from `information_schema.routines`, and the validator's own
>   live half reports `mtm_waitlist` schema is valid.
> * **5–6 are untouched** — they are optional/recommended and were not in scope. The four `image_storage.*` rows
>   are still missing, and `seed_username_case_backup` is still present.
> * **7–8.** The service was republished to **1.1.0** and redeployed on the host, and the mirror now carries
>   **seven** shapes, not six: `part_warehouse_quantities` was absent from `mtm_mock` entirely (12 tables, no
>   shape-7 artifacts) and was deployed on its own. A forced refresh returned seven successful results, and the
>   status payload reports every shape fresh with `isSeedContentOnly: false`.
> * **Recovery is bounded by the daily dump.** This host has `log_bin = OFF`, so there are no binary logs and no
>   point-in-time recovery: the Mock service's dump is the only copy, and a fresh one of the repaired store was
>   taken on 2026-10-16 (`mtm_waitlist_20261016T160557Z.sql`).

1. Take a backup of `mtm_waitlist` (or at least of `config_settings_values`).
2. Run the `ALTER TABLE` from 1.1.
3. Apply the six procedure files from 1.2 and 1.3.
4. Run the verification queries from 1.5.
5. Optional: retired objects (1.6) and the leftover backup table (1.7).
6. Recommended: add the four `image_storage.*` settings rows (1.8) — without the `TRUNCATE`.
7. On the host `172.16.1.104`: republish and redeploy `MTM_Waitlist.Mock.Service` (2.2).
8. Force a refresh and confirm all six shapes report fresh content, `employee_by_number` included (2.3).
9. Restart the desktop app and exercise sign-in, the previous-people list, and the machine-configuration
   reset.

**Added to this list on 2026-10-16 (agent):** a **step 10**, §1.9 — the two columns
`specs/012-waitlist-card-layout` adds to `waitlist_requests_queue`, applied by hand on the host. It is not in the
original ordering because this file's check did not look at that table, and it was found by running the app: with
the columns missing, the waitlist list read fails at the store. Steps 1–9 are unchanged.
