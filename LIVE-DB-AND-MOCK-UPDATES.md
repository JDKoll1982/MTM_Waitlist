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

---

## Part 2 — The Mock service on the host

### 2.1 `mtm_mock` itself needs nothing

The mirror schema is already current: all 12 tables (six `_result` mirrors plus their six `_stage`
twins) and all 14 `sp_visual_*` procedures match the repository. The two `config_images_locations_history`
triggers belong to `mtm_waitlist` and also match. **There is no `mtm_mock` script to run.**

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
