# Critical — Database tooling — the schema validator dropped the live `mtm_waitlist` store

| Field | Value |
| --- | --- |
| **Criticality** | Critical — every table in the plant's live `mtm_waitlist` was dropped and recreated empty: 21 requests, 181 settings rows, 15 user profiles, 15 role assignments, 7 computers, 24 item configs, 8 active jobs and 25 work centres. Recovered from the Mock service's own 01:00 backup; nothing else existed to recover from, because MySQL binary logging is off on that host |
| **Feature area** | `.github/scripts/validate-database-schema.ps1` — the repository's schema gate — and `Database/Bootstrap/create_database.sql` |
| **Type** | Destructive side effect in a read-oriented check, plus an unsafe call-site |
| **Found** | 2026-10-16, by the agent, immediately after running the gate as `docs/operations/operations-waitlist-next-run.md` §6 instructs. Not reported by a user; nothing else consumes this store but the desktop app |
| **Status** | **FIXED.** Store restored to its pre-incident content, then brought to the repository's vocabulary as the owner's own list requires. Validator made non-destructive and verified against a scratch store. See §4 and §5 |
| **Fixed by** | Making the bootstrap apply only to a database that does not exist, and making schema repair opt-in (`-ApplySchema`) so a check no longer migrates the store it is checking |
| **Files** | `.github/scripts/validate-database-schema.ps1`, `Database/Database-Ruleset.md` (review note), `docs/operations/operations-waitlist-next-run.md` §6 |

---

## 1. What happened

`docs/operations/operations-waitlist-next-run.md` §6 tells the agent to run all four gates, and to run the schema validator **with its
connection-string variables set so the live half actually executes** — because without them its `exit 0` is not a
live verdict. Its own table records the intent: *"duplicate-object check passes **and** the live half actually
runs"*.

Done exactly that, against the store those variables name — the plant host `172.16.1.104`.

The validator opened `mtm_waitlist`, ran its schema check, found gaps (the store was one redeploy behind, which
the owner's own comparison of 2026-10-14 says it was), and took its **repair** path:

1. `Install-Or-UpdateDatabase` applied `Database/Bootstrap/create_database.sql`, whose first statement is
   `DROP DATABASE IF EXISTS mtm_waitlist;` — its own header calls it *"the FIRST step of the fresh-store path, and
   only that"*.
2. Every table in the live store was therefore dropped and recreated empty, then the object files and the seeds
   were re-applied on top of nothing.

`information_schema` proved it: every `mtm_waitlist` table carried a `create_time` inside the same three-second
window, and every row count was zero.

## 2. Root cause

Two things had to be true at once, and both were:

* **The bootstrap is destructive and was applied unconditionally.** `create_database.sql` drops the database
  because the fresh-store path wants a clean one. `Install-Or-UpdateDatabase` applied it on the repair path too,
  where the database by definition already exists and holds the plant's data.
* **The repair path runs whenever the schema check finds *any* gap** — which a store one deploy behind always
  does, and which a store is *expected* to do. So the destructive path was not an edge case; it was the normal
  outcome of checking a store that was behind.

There was a third, quieter fault that made it worse: the object files each carry their own `USE mtm_waitlist;`,
so the install targets `mtm_waitlist` whatever database the connection string names. The `-DatabaseName`
parameter therefore does not redirect the install, and no amount of pointing the script elsewhere would have
protected the live store.

The seeds half of the same hazard had already been found and fixed on 2026-09-27 — the ruleset's review note
records that the script "used to apply every `Database/Seeds/**/create.sql` file whenever validation passed …
that made a read-only check into a writer", and made seed application opt-in. The schema repair was left as it
was.

## 3. Impact

* **Data loss on the plant store.** Everything in `mtm_waitlist` was destroyed at 10:59 on 2026-10-16.
* **Recovery was bounded by the backup cadence.** The Mock service's own daily dump was the only copy: MySQL
  reports `log_bin = OFF` on that host, so there are no binary logs and no point-in-time recovery. The store came
  back from the 01:00 dump, which is the state it had ten hours earlier. Nothing is known to have written to the
  store in between — the desktop application was not running — so the recovered content is believed complete, but
  that is an absence of evidence rather than a guarantee.
* **Any future run of the gate carried the same risk.** The instruction to run it that way is in `docs/operations/operations-waitlist-next-run.md`,
  so the next person following the hand-off would have done it again.

## 4. Fix

* **The bootstrap now applies only when the database is genuinely absent** (`Test-DatabaseExists`, asked on a
  server-level connection). Applied to a missing database the `DROP DATABASE IF EXISTS` has nothing to remove, so
  the fresh-store path is unchanged; applied to an existing one the whole statement set is skipped.
* **Schema repair is opt-in.** A run without `-ApplySchema` reports the gap count and exits non-zero instead of
  migrating. The named store is migrated only when the caller asks for it — the same principle the script already
  applied to seeds, and the same one `refresh_schema_drift.ps1` uses with its own `-Apply`.
* **The validator's live half is unchanged in the passing case**: it reads, reports and exits 0.
* The ruleset carries a **review note** recording the change and the incident, beside the seed note that
  documents the same hazard class.

## 5. Verification

**The fix, proved against a scratch store on the same server** (`mtm_waitlist_schema_probe`, created for the
check and dropped afterwards; seeded with one table holding one row that had to survive a repair):

| Run | Expected | Observed |
| --- | --- | --- |
| default, store behind | report the gap, write nothing | `schema validation found 56 gap(s)`; `Schema repair is not applied by default`; exit 1; the scratch table and its row intact |
| `-ApplySchema` | migrate, never drop | the object files applied, the scratch table and its row **still intact** — the bootstrap was skipped, which is the guard working |
| default, missing database | bootstrap and install | the missing-database branch created it and validation then passed |

**The store, restored and then brought current:**

* Restored from `mtm_waitlist_20261016T060000Z.sql` (the Mock service's 01:00 dump) with the mysql client.
  Row counts match the dump exactly: 21 requests, 181 settings rows, 15 users, 15 role assignments, 7 computers,
  24 item configs, 8 active jobs, 25 work centres, 25 objects, 95 routines.
* The owner's required repairs are then applied, so the store matches the repository rather than merely matching
  its own past: the `description` → `location` rename (`docs/operations/operations-database-live-and-mock-updates.md` §1.1 — all seven values
  preserved), and the routine set the repair rewrote, which includes `sp_auth_user_previous_sign_ins_list` and the
  five stale procedures of §1.2 and §1.3.
* Read-only proof on the restored store: `sp_core_computers_registry_get_all` answers with its `location` column,
  `sp_machine_configuration_reset` declares `p_reset_location` (the parameter the application passes),
  `sp_auth_user_previous_sign_ins_list` exists and takes its one argument.
* `validate-database-schema.ps1` run against it: **`MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING schema is valid`**,
  exit 0, having written nothing — the live verdict the gate was reaching for, reached safely.
* A fresh `POST /api/backup` of the repaired store was taken through the Mock cache service
  (`mtm_waitlist_20261016T160557Z.sql`), so the repaired state is no longer resting on a single 01:00 dump.

## 6. Residual risk to keep in view

* **The dump is the only safety net.** `log_bin = OFF` on that host means the daily backup is the floor and
  anything written since is unrecoverable if the store is lost. Enabling binary logging, or shortening the backup
  interval, is a decision for the owner.
* **`-ApplySchema` is still a broad write.** It rewrites every table, procedure, function and view from the
  repository, and it applies a repository column rename's dependent routines in whatever order the files happen to
  be enumerated — so a rename must be applied to the store first, by hand, as `docs/operations/operations-database-live-and-mock-updates.md` §1.1
  says. Opting in removes the accident, not the need for care.
* **`-DatabaseName` does not redirect the install**, because the object files carry their own `USE`. That is
  recorded here rather than fixed: what it protects against now comes from the switch instead, and changing the
  object files' own `USE` lines is a repository-wide decision.
