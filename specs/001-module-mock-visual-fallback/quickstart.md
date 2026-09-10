# Quickstart — Module_Mock: Automatic Infor Visual Read Fallback

**Feature**: `001-module-mock-visual-fallback` | **Date**: 2026-09-09
**Plan**: [plan.md](./plan.md) · **Research**: [research.md](./research.md) · **Data model**: [data-model.md](./data-model.md)
· **Contracts**: [contracts/](./contracts)

This is the end-to-end operator/developer walkthrough: deploy the cache, start the service, prove the fallback, and
prove the defect is fixed. It is written to be run in phase order — every step is independently verifiable, and each
step names the artifacts it needs and the outcome it proves.

---

## 0. Prerequisites

| Requirement | Detail |
|---|---|
| OS | Windows 10 1809+ (`10.0.17763.0`), **x64**; PowerShell 7 (`pwsh`) |
| .NET | .NET 10 SDK (`net10.0-windows10.0.19041.0`) |
| MySQL | MySQL 5.7 on the database host; a login able to `CREATE DATABASE`, create tables/procedures, and run `mysqldump` |
| MySQL client tools | `mysqldump` and `mysql` on the host `PATH` (or set `mysqldumpPath` explicitly). Absence is *reported*, never worked around |
| Infor Visual | Read-only SQL Server `VISUAL`/`MTMFG` reachable from the host — required for refreshes, not for the fallback itself |
| Build | `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false` |
| Test | `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64` (live DB tests additionally require `MTM_WAITLIST_TEST_DB_CONNECTION_STRING`) |

**Before you start**: confirm a green baseline (build 0 warnings / 0 errors; full suite pass) and record the counts.
This feature is additive-then-subtractive, so a baseline is what lets you tell a new failure from an existing one.

---

## 1. Deploy `mtm_mock` (Phase 1 — additive, no application impact)

1. Create the database: `Database/Mock/Bootstrap/create_database.sql` (re-runnable; creates `mtm_mock` if absent,
   `utf8mb4` / `utf8mb4_unicode_ci`).
2. Deploy the five mirror tables **and their `_stage` twins**: `Database/Mock/Tables/<table_name>/create.sql`, then
   register both in `Database/Mock/AllTables.sql` and `Database/Mock/Bootstrap/update_table_descriptions.sql`.
3. Deploy the procedures: `Database/Mock/StoredProcedures/sp_visual_<shape>_{refresh,get}/create.sql` (ten procedures),
   then register in `Database/Mock/AllSPs.sql`.
4. Deploy the baseline seed rows: `Database/Mock/Seeds/<seed_name>/create.sql`, then register in
   `Database/Mock/AllSeeds.sql`.

**Verify**

```sql
-- shape reads return the seed content before any refresh has happened (FR-017)
CALL sp_visual_work_order_lookup_result_get('SEED-WO-1');
-- metadata proves these are seed rows, not refreshed rows
SELECT refreshed_utc, is_seed_content FROM visual_work_order_lookup_result LIMIT 5;   -- is_seed_content = 1
```

5. Run one real refresh and confirm the atomic swap:

```sql
CALL sp_visual_work_order_lookup_result_refresh(/* source parameters */);
SELECT refreshed_utc, is_seed_content FROM visual_work_order_lookup_result LIMIT 5;   -- is_seed_content = 0
SELECT COUNT(*) FROM visual_work_order_lookup_result_stage;                            -- previous snapshot
```

While that statement runs, a concurrent `SELECT` on the live table must always return either the complete previous
snapshot or the complete new one — never a mixture and never an empty table (FR-006, SC-005).

## 2. Deploy and start the service (Phase 2)

1. Build and publish `MTM_Waitlist.Mock.Service` (unpackaged, self-contained `win-x64`) onto the MySQL host.
2. Launch it. Expected: **no main window** — a tray icon appears and the background engines start.
3. Open the settings UI from the tray and confirm the configuration surface: refresh interval, per-store backup
   schedule/retention/destination, API bind address/port, Visual connection, and last-run status (FR-012, FR-013).
4. Generate the shared credential. **Record it out of band** — the UI will not display it again (FR-026).
5. Enable auto-start at logon (FR-007) and confirm the per-user `Run` entry exists:
   `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
6. Sign out and back in; the service must start automatically, minimized, with no user interaction (US3 acceptance 1).

**Verify the API** (see `contracts/mock-service-http-api.md`):

```powershell
# refused without the credential (SC-010)
curl.exe -i http://<host>:5760/api/status
# 401 {"error":"unauthorized"}

# status with the credential — note it never contains the credential itself
curl.exe -H "X-MTM-Mock-Token: <credential>" http://<host>:5760/api/status

# on-demand refresh
curl.exe -X POST -H "X-MTM-Mock-Token: <credential>" -H "Content-Type: application/json" `
  -d '{\"shapeKeys\":null}' http://<host>:5760/api/refresh

# restore is NOT reachable over the network (FR-023)
curl.exe -i -H "X-MTM-Mock-Token: <credential>" http://<host>:5760/api/restore
# 404
```

7. Leave the service running for one configured interval and confirm each shape's `lastOutcome` in `/api/status`
   becomes `succeeded` with a fresh `refreshedUtc` (US3 acceptance 2, SC-007).
8. Stop Infor Visual connectivity (or point the service at an unreachable server) for one cycle and confirm the
   outcome is `skippedSourceUnreachable` with a logged reason, that the mirror's `refreshed_utc` is **unchanged**, and
   that the next cycle still runs on schedule (US3 acceptance 3).

## 3. Deploy the application change (Phase 3)

1. Confirm `MTM_Waitlist.Mock` is referenced by the app and by `MTM_Waitlist.Setup`,
   `MTM_Waitlist.Waitlist.View`, and `MTM_Waitlist.Settings`, and registered in DI.
2. Configure each client with the shared credential (§2 step 4). A client without it must still work on cached data.
3. Launch the app and confirm the read-only status indicator is **absent** while Visual is reachable (FR-005).

**Prove the fallback (US2, SC-003, SC-004)**

1. Make Infor Visual unreachable (stop the service on the Visual side, or block the host).
2. Exercise all five journeys in the app: work-order lookup, operation sequences, subordinate parts, inventory
   locations, and request disposition.
   Expected — every one returns a complete, correctly shaped result, with no error surfaced and no partial rows.
3. Confirm the indicator appears, states that Infor Visual is unreachable, shows **the age of the cached data**, and is
   non-interactive: clicking it, pressing Enter on it, and tabbing through the page must trigger nothing (US4, SC-006).
4. Restore Visual connectivity and repeat a read — live data must be returned **without restarting the app**, and the
   indicator must clear (US2 acceptance 2, US4 acceptance 3).
5. Compare a cached result with a live result for the same input: identical column set and identical structure
   (FR-004, SC-004).
6. Stop the service entirely and repeat step 2 — the app must remain fully functional on the existing cache
   (FR-025, SC-011).
7. Reconnect to a source that legitimately returns **no rows** and confirm the app shows empty (not cached) — an empty
   live result must never be replaced by cache (FR-024).

## 4. Prove the reported defect is fixed (US1, SC-001, SC-002)

1. Wherever the retired demo setting used to be, confirm **no such control exists** (US1 acceptance 4, FR-014).
2. Save a workstation setup and confirm the work-center card shows the new assignment **immediately**, with no manual
   refresh and no restart. Repeat 20 times with no occurrences of "save acknowledged but card unchanged" (SC-001).
3. Run a mixed request lifecycle (accept, add a note, change status, cancel) at least 50 operations deep; confirm every
   action persists, survives an application restart, and is visible to a second user session (SC-002).
4. Confirm internal-store failure behaviour: make `mtm_waitlist` unavailable and confirm the affected screen retries
   with backoff and then shows a clear unavailable/error state with guidance — and **no** sample rows (FR-021).

## 5. Backups and emergency restore drill (US6, SC-008, SC-009)

1. Configure a schedule for one store only; confirm the other stores still back up on their own schedules
   (US6 acceptance 2).
2. After the window passes, confirm a restorable artifact exists with a non-zero size, is recorded with its timestamp
   and location, and appears in `GET /api/backups` (US6 acceptance 1, SC-008).
3. Rename `mysqldump` (or remove it from `PATH`) and run a backup cycle: expect a `toolUnavailable` outcome and **no**
   artifact record — no partial or corrupt artifact may be recorded as successful (US6 acceptance 5, FR-013).
4. Restore drill against a **throwaway** store:
   - Request a restore and **decline** the confirmation → the store must be byte-for-byte unchanged (US6 acceptance 3).
   - Request it again and **accept** → after completion the store must contain exactly the contents of the selected
     artifact, with the row counts verified, and the outcome recorded (US6 acceptance 4).
   - Confirm the restore is only performable from the service UI on the host — there is no network path to it
     (FR-023).
   - Time the drill end to end; it must finish under 15 minutes (SC-009).

## 6. Complete the removal and the SP-first conversion (Phases 4–6)

1. **Removal (Phase 4)**: delete both legacy mock families, the demo keys, the routing/auto-force/monitoring stack and
   its toasts, the Settings "use demo data" toggle, and the DB-backed `mock_*` tables/`sp_mock_*`/seeds — and rework
   the tests that exercised them. Exit condition: no references remain to any removed symbol, key, or table.
2. **SP-first (Phase 5)**: convert every inline MySQL statement listed in
   `WeekendProject/Module_Mock/Discovery/03-Hardcoded-MySQL-Sql.md` — route to the existing procedure where one
   covers it, otherwise create a new one (with rollback + master-list registration). Exit condition: no inline
   statement text remains in C#.
3. **Real-data gaps (Phase 6)**: wire `CoilAvailabilityService` to a real source, and make `RequestTypeCatalogService`
   the single authoritative request-type source (moving `ImageLocationService` off
   `Assets/Config/waitlist-request-types.json`) (FR-019).

## 7. Adding a sixth read shape (US7, FR-016, SC-012)

Follow the six ordered steps in `contracts/mock-service-configuration.md` §4 — capture the read, add the mirror +
stage tables, add the refresh/get procedures, register the shape in the shape catalog, add the in-app fallback
implementation and route its caller, then add the seed and the three tests. Each step names the single artifact it
produces, and no existing shape's artifacts or contracts change. The exit criterion is SC-012: a maintainer who has
not worked on the cache can complete it in one working day using only that playbook.

## 8. Final validation (Phases 7–8)

| Check | Command / evidence | Success criterion |
|---|---|---|
| Build | `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false` | 0 warnings, 0 errors (SC-015) |
| Offline suite | `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64` | All green; no test references a retired system (SC-015) |
| Live DB subset | same command with `MTM_WAITLIST_TEST_DB_CONNECTION_STRING` set | Green against the local MySQL |
| Retired-symbol audit | the audit test added in Phase 4 | 0 references to the retired demo/sample systems (SC-013) |
| Inline-SQL audit | the audit test added in Phase 5 | 0 inline database statement text in application code (SC-013) |
| In-app verification | UI inspection of the fallback + indicator and the work-center card after a save | US1/US2/US4 demonstrated on a running build |
| Service end-to-end | scheduled refresh, per-store backups, one throwaway restore, token-gated API | US3/US5/US6 demonstrated |
| Documentation | `WeekendProject/ChangeLog.md`, the Module_Mock docs, and the published playbook | 0 stale references to the retired demo systems; the playbook is published (SC-016, FR-028) |

**Troubleshooting notes carried over from this repository's build conventions**: a `PRI175`/`PRI224` PRI failure is
usually a running `MTM_Waitlist.exe` or stale `*.pri` under `obj/`/`bin/` rather than a code error; a
`WMC9999 ErrorMessages.resources` failure is a **masked XAML compile error**, not an environment problem. A
`[RelayCommand]`-generated command drops the trailing `Async` from its method name (`SaveAsync` → `SaveCommand`).
