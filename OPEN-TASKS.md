# Open Tasks — MTM_Waitlist

**Generated:** 2026-09-11 (from the synced repository at `c2d5e04`, working tree clean); §1 updated 2026-09-12.
**Scope:** every live open (`- [ ]` / `+ [ ]`) checkbox in the tracked backlog documents, plus the items that
look open but are not.

**Read this first.** There are two genuinely different kinds of "open" here, and conflating them is the single
most expensive mistake available in this repo:

| Kind | Where | Count |
| --- | --- | ---: |
| **Live, actionable now** | `specs/001-module-mock-visual-fallback/tasks.md` | **0** |
| **Carry-forward for a future spec** | `WeekendProject/**` source checklists | **127** |
| **Not work at all** (retired sources + obsolete requirements) | see §3 | **223** |

Item-level detail for the carry-forward backlog deliberately lives in **one** place —
`WeekendProject/OPEN-WORK-NEXT-SPEC.md` §4–§10 — and is **not** duplicated here. This file is the index; that
file is the itemised breakdown. Duplicating it would create a divergent third copy, which is exactly the trap
§3 exists to prevent.

---

## 1. Live open work — `specs/001-module-mock-visual-fallback/tasks.md`

**173 boxes: 173 `[x]`, 0 `[ ]`** (re-counted from the file after this pass). **The active feature now has no open
task.** The last two — **T106** and **T151** (§1.1, §1.5), both environment-gated verifications — were verified by the
operator on 2026-09-12. T144, T147 and T148 were resolved on 2026-09-12 (§1.2–§1.4); T150 was implemented and T151
(its verification counterpart) added the same day (§1.5).
T159 and T160 were resolved the same day, **T158 — the failing backups — and T161 — the restore that connected to the
wrong server — are fixed**, **T155, T156 and T157 are closed** (§1.6), the follow-up passes **T162-T164 are done**
(§1.7), **T165 and T166 followed them** (§1.8), **T167 and T168 closed the day's capability work** (§1.9),
**T169 made the installer ask what you are trying to do** (§1.10), **T170 made it choose its own install folder and
notice a stale publish output** (§1.11), and **T171/T172 made the service and the installer work on a machine that
cannot reach the plant** (§1.12).

| ID | Severity | Summary | Blocker / decision needed |
| --- | --- | --- | --- |
| ~~T106~~ | ~~verification~~ | **VERIFIED 2026-09-12** — the end-to-end walkthrough (`quickstart.md` §1–§8) was executed on a running build and the SC-007/SC-008 observation windows were started. The 30-day **measurement** is a dated re-check (due 2026-10-12), not an open task — see §1.1 | — |
| ~~T151~~ | ~~verification~~ | **VERIFIED 2026-09-12** — all three parts confirmed: 2,012 shape-4 rows with none below 1 on hand (host), and the sampled part's detail grid matched the live read on a **signed-in** session — see §1.5 | — |
| ~~T144~~ | ~~coverage~~ | **RESOLVED 2026-09-12** — the cache guards, the live reads and the key the app sends were all narrowed to the canonical `WO-######` string; the operator's raw input stays lenient and is auto-formatted to that key before the lookup | — |
| ~~T147~~ | ~~HIGH~~ | **RESOLVED 2026-09-12** — the shared credential was retired; the API is authorized by the caller's application role | — |
| ~~T148~~ | ~~HIGH~~ | **RESOLVED 2026-09-12** — (a) the show-request channel + desktop shortcut shipped; (c) the service now writes its own daily log file | — |

### 1.1 T106 — end-to-end acceptance walkthrough — VERIFIED 2026-09-12

`quickstart.md` §1–§8 was executed on a running build and the box is ticked. **Recorded on the operator's
verification** — this pass does not re-derive each of §1–§8, but the parts that left a trace in the run output are
named below. Every blocker this section used to list is retired:

| Part | Outcome |
| --- | --- |
| §2 publish, host install, tray-only start, single-instance redirect, API gating | **Done** — host-verified 2026-09-12 (`tasks.md` Phase 25) |
| §2 steps 3–4 (tray Settings surface, operator access) | **Done** — operator interaction on the host; T147 needed nothing generated or recorded |
| §2 step 7 (one real refresh cycle) | **Done** — host-verified 2026-09-12 |
| §3 fallback proof, §4 defect proof | **Done** — a **signed-in** application session; the run output shows `GetInventoryLocations.sql` unable to reach Infor Visual and the read served from `mtm_mock`, which is §3's proof |
| §5 backup/restore drill | Covered by the same verified execution |
| §7 sixth-shape playbook | Covered by the same verified execution |
| **SC-007 / SC-008** | **Started 2026-09-12 — not measured.** The windows need 30 days of wall time; re-check due **2026-10-12** (≥ 95 % of scheduled refresh cycles succeeding; 100 % of scheduled backup windows producing a restorable artifact per enabled store). T106 asked that the **start be recorded**, which it now is; the tick is not a claim that the windows passed |

A fifth reason was added on 2026-09-11 and **retired 2026-09-12**: §3 step 3's *"every journey returns a complete,
correctly shaped result"* would pass on **shape** while still serving a **different work order** than the live read
for the same input — which was T144. The cache guards and the live reads were narrowed to the canonical
`WO-######` key, so the colliding bare-numeric key is no longer sent at all (the operator's raw input is still
accepted and auto-formatted to that key; §1.2).

### 1.2 T144 — RESOLVED 2026-09-12 (the key sent is the canonical `WO-######`; the raw input is auto-formatted to it)

The **transparency** half was **fixed 2026-09-11**: all five `Database/InforVisual/Queues/Module_Mock/Populations/*.sql`
emit exactly the live-resolvable key domain (`work_order_lookup` 701 · `operation_sequences` 893 ·
`subordinate_parts` 1,746 · `inventory_locations` 79,457 · `disposition_input` 701).

The **coverage** half was open because the application's input rule `^(?:WO-)?(\d{5,6})$`
(`MTM_Waitlist.Setup/Services/WorkOrderValidationService.cs`) could not express **42,143 of the 42,852** open Infor
Visual work orders, and widening it meant changing product behaviour. The operator took that decision on 2026-09-12
and took the **opposite** branch: the *key the application asks for* is **only** the `WO-######` form, and that is what
the five population guards and the four work-order live reads were narrowed to.

The operator's *raw input* is deliberately still lenient — `76951`, `076951` and `WO-076951` all name the same order —
and `WorkOrderValidationService` **auto-formats it to the canonical `WO-######` string before anything is sent**, so
Infor Visual and the cache only ever receive that canonical key. The distinction matters: accepting the loose forms is
a formatting convenience, whereas the key domain is what T144 was actually about, and it is the key domain that closed:

| Family | `BASE_ID` form | Open | Before | Now |
| --- | --- | ---: | --- | --- |
| `W` | literally `WO-` + 6 digits | 555 | reachable, cached | **the only reachable family** |
| `M` | bare numeric, exactly 6 digits | 154 | reachable via the stripped-form match | **unreachable by design** — no longer accepted, no longer cached |
| `M` | bare numeric, 5 digits | 76 | unreachable | unreachable |
| other | `Q` family (37,259) + non-prefixed `W` (140) + remaining `M` (4,668) | 42,067 | unreachable | unreachable |

Narrowing closes the defect class instead of extending it: the key the application asks for is always the verbatim
Infor Visual `BASE_ID` of a `W`-family order, so the live read and the cached copy are keyed by the same string.
The stripped-base-id match (`BASE_ID IN (@NormalizedWorkOrderTrimmed, @WorkOrderBaseId)`) was **removed** from
`LookupWorkOrder.sql`, `GetSequences.sql`, `GetSubordinateParts.sql` and `GetDispositionInput.sql` — leaving it would
still resolve bare `M` orders the application can no longer ask for, and the **3** five-digit cases that collide with
a `WO-0xxxxx` order would keep returning a *different* order than the cache served. Placeholder, validation and tooltip
copy names the accepted input forms again (`76951, 076951, or WO-076951`) because the service auto-formats them.
Offline suite at the narrowing: **747 / 728 / 19 / 0**; after the auto-format restoration: **760 / 741 / 19 / 0**.
The population reads still need a **live** re-run on the cache host to confirm the new row counts.

Full detail: `specs/001-module-mock-visual-fallback/tasks.md` Phase 24.

### 1.3 T147 — RESOLVED 2026-09-12 (role-based authorization replaced the shared credential)

The owner decision was taken on 2026-09-12: the shared credential was **removed** rather than made obtainable, and a
caller is now authorized by the **application role** of the user it names. `SharedTokenAuthenticationHandler` and
`SharedCredential` are deleted; `ApiSettings` carries no credential; the settings surface shows the approved roles
instead of offering a rotate button; and the client presents `X-MTM-Mock-User` (`MTM_MOCK_SERVICE_TOKEN` →
`MTM_MOCK_SERVICE_USER`). `ServiceOperatorAuthenticationHandler` + `ServiceOperatorRoleResolver` resolve the named
user through `sp_auth_user_row_get` against `mtm_waitlist` and require one of `Admin`, `Developer`, `Plant Manager`,
`Setup Lead`, `Production Lead`; every refusal answers `401 {"error":"unauthorized"}` indistinguishably, and an
unreachable store fails closed. The spec (FR-011/FR-012/FR-023/FR-026, SC-010), both service contracts and
`data-model.md` §9 were amended in the same change.

**Known limitation, accepted by the owner:** the user name is asserted over plain HTTP, so this is authorization
without cryptographic authentication.

**Host verification: DONE 2026-09-12** on `V-MTMFG-5` (an earlier revision of this note said it was outstanding).
On the host: `sp_auth_user_row_get` is deployed and resolves `jkoll`/`johnk` to `Developer`; anonymous, an invented
name, a real-but-unassigned name and the retired `X-MTM-Mock-Token` all answer a byte-identical
`401 {"error":"unauthorized"}`; an approved operator answers `200` with
`"operatorRoles":"Admin, Developer, Plant Manager, Setup Lead, Production Lead"` and no secret-shaped field; the
listener binds `0.0.0.0:5760`, answers a non-loopback call, and an inbound allow rule exists. Four defects the run
exposed were fixed the same day — recorded in `specs/001-module-mock-visual-fallback/tasks.md` **Phase 25**, which
also carries the residuals (T155-T158). Nothing about T147 is outstanding.

### 1.4 T148 — RESOLVED 2026-09-12 ((a) already shipped; (c) implemented; (b) fixed 2026-09-11)

- **(a) The UI is now reachable without the tray icon.** This was already implemented in commit `c2d5e04` and the task
text was stale: a second launch parses `--open-status` / `--open-settings`
(`Services/ServiceActivationParser.cs`), signals the running instance over session-local named events
(`Services/ServiceShowChannel.cs`), and `deploy/mock-service-control.ps1` — opened by the desktop shortcut the
installer places — is the operator's entry point. The tray-only *lifetime* is unchanged; only the *entry points* are.
- **(c) The service now keeps a durable log file.** Root cause was worse than "no log service": every diagnostic went
through `StartupDebugLog`, whose methods are `[Conditional("DEBUG")]`, so a published `Release` build compiled them
out. The service now writes JSON Lines to
`%LOCALAPPDATA%\MTM_Waitlist.Mock.Service\Logs\service_daily_<yyyy_MM_dd>.jsonl` via `Services/ServiceLog.cs` and an
`ILoggerProvider` registered in `ServiceHostBuilder.Build` (`Services/ServiceFileLoggerProvider.cs`), so every
container log message — the refresh engine's cycle failures above all — is durable and readable on the host.

- **(b) — FIXED 2026-09-11. No refresh had ever completed.** Root cause was **our deploy script**, not the
  service: `install-mock-service.ps1` wrote the secrets at User scope and launched the service with
  `Start-Process`, which hands the child a copy of *the script process's* environment block — and that process
  predated the variables. The service therefore started with **no** `MTM_*`/`INFOR_VISUAL_*` values and failed
  every metadata read and refresh cycle. Fixed in three places (apply the secrets to the script's own process
  and assert it — deploy is now **23** checks; treat a host with no login as unconfigured so the designed
  message shows instead of a raw access-denied; poll for process exit after the stop instead of sampling once).
  **Verified:** first ever successful refresh — all five shapes `Succeeded` in ~4.4 s.

**Host verification of (a) and (c): DONE 2026-09-12** on `V-MTMFG-5`. `mock-service-control.ps1 -Action ShowUi`
opened the service window on the Status page while the tray icon stayed unpromoted, and
`%LOCALAPPDATA%\MTM_Waitlist.Mock.Service\Logs\service_daily_2026_09_12.jsonl` recorded the startup (naming its log
directory), every refusal with its reason, and each shape's refresh outcome — written by a **Release** publish, which
previously wrote nothing at all. **§2 step 7 is satisfied:** `POST /api/refresh` returned all five shapes
`succeeded`. The operator-facing hint in `mock-service-control.ps1` that still pointed at a debugger and the Windows
Event Log was corrected in the same pass.

**Order of attack:** T147 and T148(a)/(c) are complete and host-verified. No host step remains for them; the
residuals from that run are T155-T158 in `specs/001-module-mock-visual-fallback/tasks.md` Phase 25.

### 1.5 T150 / T151 — shape-4 cache no longer stores zero-stock locations — both VERIFIED 2026-09-12

- **T150 (implemented).** `inventory_locations_population.sql` now ends with
  `HAVING MAX(COALESCE(pl.QTY, part.QTY_ON_HAND, 0)) >= 1`, so a `(part_number, location)` slot is only cached when
  it holds stock. The mirror carries a unique `(part_number, location)` key and the caller already keeps only
  on-hand `>= 1` (`MTM_Waitlist.Waitlist.View/Services/InventoryLocationFiltering.cs`), so a zero-stock row was
  occupying a slot nobody displays — 79,218 seeded rows were overwhelmingly zero-stock.
- **The replacement semantics were already correct**, and are unchanged: `sp_visual_inventory_locations_refresh`
  truncates the stage twin, inserts the payload, asserts `v_loaded = JSON_LENGTH(p_rows)`, then swaps the tables by
  `RENAME TABLE`. The live mirror is replaced wholesale every run, so a location that drops to zero disappears on
  the next cycle with no stale row. The exclusion is done at the **source** so that row-count assertion stays intact.
- **T151 (VERIFIED 2026-09-12 — all three parts).** Host-confirmed first, after the redeploy's startup cycle:
  `visual_inventory_locations_result` held **2,012** rows with **none** below 1 on hand (minimum `1.0000`), against the
  79,457 pre-change figure, and the other four shapes repopulated normally. The third part — that a sampled part's
  Waitlist detail grid matches the live read — was confirmed the same day on a **signed-in** session: the detail
  surface's inventory read (`GetInventoryLocationRowsAsync` for `RM-50218 / Customer RM-77`) returned the mirror's
  answer for that part, and `Raw=0, Displayed=0` is a **real empty answer** for a part the mirror holds no locations
  for, which FR-004 requires be served as-is rather than treated as a miss.

**Open question for the operator.** `Database/Mock/Seeds/seed_visual_mirror_baseline/create.sql` is a capture of the
live mirror taken 2026-09-12 and still carries those 79,218 shape-4 rows, 0-quantity included. They are harmless
(the app filters them) but now diverge from what the population read produces; regenerating the capture needs the
shared host and was **not** done in this run.

### 1.6 Residuals of the host run, and the app deployment gaps (T155-T160)

Full detail: `specs/001-module-mock-visual-fallback/tasks.md` **Phase 25** (the host run and the three defects it
exposed and fixed — filed as T152-T154), **Phase 26** (the cross-machine reconciliation, the suite gates and the
redeployment of both artifacts) and **Phase 31** (T155-T157).

| ID | Severity | What it is |
| --- | --- | --- |
| ~~T155~~ | ~~MEDIUM~~ | **RESOLVED 2026-09-12.** Not widened but **replaced**: the guard asked about the environment, so the two tests now drive a stub whose non-query affects **0 rows** — the exact branch that produces the failure they assert. `SetupPersistenceService` was moved onto `IMySqlHelperServer` to allow it, and `ProductionBackendAvailability.cs` was **deleted**. The task's stated blind spot turned out not to exist: both tests built the helper server with no options, so the `appsettings.json` fallback was never in play. See `tasks.md` Phase 31. |
| ~~T156~~ | ~~MEDIUM~~ | **RESOLVED 2026-09-12 (validated).** Dropped at **load time** by `ServiceConfigurationStore.TryDropRetiredCredentialProperties()` — covering an install that is never re-saved — **and** scrubbed from the live file and every backup copy of it by a new `purge retired credential` check in `install-mock-service.ps1` (deployment went from 23 to 24 checks, and to **25** with T166 — §1.8). See `tasks.md` Phase 31 and Phase 33. |
| ~~T157~~ | ~~LOW~~ | **RESOLVED 2026-09-12.** Seeded one account per user type as asked: eight `test.*` accounts in `seed_dev_masked_baseline` — five holding **approved** operator roles and three (`test.setup`, `test.production`, `test.material.handler`) deliberately **not** approved, so the refusal branch is reachable against a real store. The catalog also gained the `admin` role. No seeded account can log in (the `'0000'` placeholder, unchanged from the existing accounts). See `tasks.md` Phase 31. |
| ~~T158~~ | ~~LOW~~ | **RESOLVED 2026-09-12.** All four backup stores failed because `BackupEngine` built the `mysqldump` command line from the raw settings — `localhost`, no login, no credentials file — instead of the resolver it was already being handed and never used. All four stores now dump successfully on the cache host (180 KB / 7.0 MB / 3.7 MB / 985 KB). See `tasks.md` Phase 28. |
| ~~T161~~ | ~~MEDIUM~~ | **RESOLVED 2026-09-12.** `RestoreService` had the same defect T158 fixed in `BackupEngine`, so a restore connected to `localhost` with no login and failed with `Access denied for user 'ODBC'@'localhost'` — after its safety snapshot had succeeded. It now resolves the connection per store and shares one credentials file across its three invocations. The restore card also gained its **own database picker**, so the database to restore no longer has to be chosen in the backup card. See `tasks.md` Phase 29. |
| ~~T159~~ | ~~MEDIUM~~ | **RESOLVED 2026-09-12.** The app now has a publish profile (`Properties/PublishProfiles/win-x64-selfcontained.pubxml`) and a documented deploy step. Operator decision: **self-contained**, into the per-user folder `%LOCALAPPDATA%\Programs\MTM_Waitlist`. Verified: 662 files / 243.5 MB with `hostfxr.dll` + `coreclr.dll` present, and the published exe reached the Sign in window. See `tasks.md` Phase 27. |
| ~~T160~~ | ~~MEDIUM~~ | **RESOLVED 2026-09-12.** The client now has a consumer: a **Cached Infor Visual data** panel on the Settings page, restricted to **Plant Manager and above** — the same `{Admin, Developer, Plant Manager}` trio the Max Allotted Time panel uses. All failure paths are reported, never thrown. The two `MockServiceClient` values stay empty in the shipped file on purpose (the environment variables are the per-machine install mechanism). See `tasks.md` Phase 27. |

### 1.7 Follow-up passes outside the mock feature (T162-T164)

Full detail: `tasks.md` **Phase 30** (T162, T163) and **Phase 32** (T164).

| ID | Severity | What it was |
| --- | --- | --- |
| ~~T162~~ | ~~MEDIUM~~ | **RESOLVED 2026-09-12.** Phase 24 narrowed the work-order *input* rule along with the key, which broke the Setup textbox's long-standing auto-formatter. The key sent is still the canonical `WO-######`; the raw input is lenient again and is formatted to that key **before** the lookup, so the collision class stays closed. See `tasks.md` Phase 30. |
| ~~T163~~ | ~~LOW~~ | **RESOLVED 2026-09-12.** The image cards inside **Image Location Settings** were `Request Type`, `Work Center`, `Request Subtype`. Types and subtypes are now **one** card with two actions, followed by the work-center card. See `tasks.md` Phase 30. |
| ~~T164~~ | ~~MEDIUM~~ | **RESOLVED 2026-09-12.** An account still holding the temporary default password had to be signed into with `0000` before the change panel appeared. Startup now resolves the requirement from the store while the splash is up (new `sp_auth_password_reset_required_get`, called only on the login branch) and the window opens directly on the change panel — the sign-in form is never shown. The auto-login path is deliberately unchanged. See `tasks.md` Phase 32. |

### 1.8 Host reachability and the installer shortcut (T165, T166)

Full detail: `tasks.md` **Phase 33**.

| ID | Severity | What it was |
| --- | --- | --- |
| ~~T165~~ | ~~HIGH~~ | **RESOLVED 2026-09-12.** On a workstation that cannot reach the plant cache host (`172.16.1.104`), startup blocked with *"Could not validate startup session from the database"* and the log showed `Connect Timeout expired`, even though the same stores exist on `localhost`. New `MySqlHostFallback` keeps the configured server when it answers, otherwise repoints at `localhost` **only when localhost also answers**, and otherwise returns the string unchanged so the operation fails and is reported as before. Applied at both connection-string resolvers, so every store read inherits it. 5 new tests; suite 766 → **771**. |
| ~~T166~~ | ~~LOW~~ | **RESOLVED 2026-09-12.** `install-mock-service.ps1` wrote its desktop shortcut only to `-DesktopPath` (`C:\Users\jkoll\Desktop`, or the shell's Desktop known folder), so one installer served one of the two operator machines. New `-AdditionalDesktopPaths` (default `@('C:\Users\johnk\Desktop')`) receives the same shortcut, **best effort**: a missing profile is a `SKIP`, an unwritable folder a `WARN`, neither can fail the deployment. A redirected desktop is found beside the literal path. Deployment checks 24 → 25. |

### 1.9 Install anywhere, gate the work on reachability (T167, T168)

Full detail: `tasks.md` **Phase 34**.

| ID | Severity | What it was |
| --- | --- | --- |
| ~~T167~~ | ~~MEDIUM~~ | **RESOLVED 2026-09-12.** The service refused to install on anything but the cache host, and a host that could not reach Infor Visual attempted every refresh cycle anyway. It now installs anywhere, and refresh is **disabled** — not attempted — where Visual has been measured unreachable. New `ServiceCapabilityProbe` (Visual + all four stores, cached for a minute and re-measured, so restored access resumes without a restart), consumed by the refresh loop, the backup schedule, restore, the API and both surfaces; `POST /api/refresh` → `503 refreshUnavailable`; `GET /api/status` gains `refreshEnabled`/`refreshDisabledReason`. Not configured, or a probe that could not run, disables **nothing**. The installer's server-only guard became a `cache host` / `infor visual` / `mysql host` disposition report that is never fatal, `-AllowNonServerHost` was removed, and deployment went 25 → **27 checks**. |
| ~~T168~~ | ~~MEDIUM~~ | **RESOLVED 2026-09-12.** One unreachable database took the whole backup surface with it. Backup and restore are now disabled **per store**: the scheduled slot is skipped with no run record (so the last real backup stays visible), `POST /api/backup` → `503 storeUnavailable`, restore is refused before any safety snapshot (`RestoreOutcomeKind.StoreUnavailable`), the settings surface refuses *Back up now* and *Restore* with the reason, and `GET /api/status` reports per-store `isStoreReachable`/`unreachableReason`. The other stores are unaffected. |

### 1.10 The installer asks what you are trying to do (T169)

Full detail: `tasks.md` **Phase 35**.

| ID | Severity | What it was |
| --- | --- | --- |
| ~~T169~~ | ~~LOW~~ | **RESOLVED 2026-09-12.** Running `install-mock-service.ps1` by hand meant knowing five switches, and running it with no argument silently deployed the defaults. With **no argument** it now presents a guided flow: an intent menu (*Deploy* / *Publish and deploy* / *Clean sweep* / *Quit*), the install-folder question with the resolved default, the independent questions plus the one destructive one, a summary showing **the equivalent command line**, a yes/no confirmation, and a pause after the result. **Any argument — including `-NonInteractive` — bypasses it**, so automation is unaffected. Deployment checks unchanged at 27. |

### 1.11 The installer picks its own defaults (T170)

Full detail: `tasks.md` **Phase 36**.

| ID | Severity | What it was |
| --- | --- | --- |
| ~~T170~~ | ~~MEDIUM~~ | **RESOLVED 2026-09-12.** The install folder was a hard-coded default that only works where the account can create `C:\Services`, and a run that reused a **stale publish output** failed the content check — which reads as a deployment fault instead of an un-built source change. The folder is now resolved: an existing `C:\Services\MTM_Waitlist.Mock.Service`, else that path when this account can actually create it, else `%LOCALAPPDATA%\Programs\...`; the choice and reason are printed, an unwritable `-TargetPath` fails immediately naming both candidates, and the guided flow shows the folder as a default accepted with Enter. `Test-PublishOutputStale` makes section 4 report `WARN` when it reuses an out-of-date output, pre-selects *Publish and deploy* in the guided flow, and makes the step-5 failure name the cause. Verified by two real deployments on this workstation. |

### 1.12 Making a machine that cannot reach the plant actually work (T171, T172)

Full detail: `tasks.md` **Phase 37**.

| ID | Severity | What it was |
| --- | --- | --- |
| ~~T171~~ | ~~HIGH~~ | **RESOLVED 2026-09-12.** The **service** had no configured-host fallback: `MySqlConnectionStringResolver` returned the plant host untouched, so on a workstation every read failed while the *app* on the same machine reached the same databases through `MySqlHostFallback` (T165). The service references `MTM_Waitlist.Core`, so both returns of `Resolve` now go through that one helper — the app and the service cannot disagree about which host a store lives on. Proven end-to-end: `GET /api/status` as a named operator returns `200` (role lookup reached `mtm_waitlist` locally) while the installed secrets still name the plant host. |
| ~~T172~~ | ~~MEDIUM~~ | **RESOLVED 2026-09-12.** Three defects only a mirror-only host reveals. (a) The MySQL and Visual **secret proofs** failed when the plant was unreachable, so a deployment could never complete off-network — they are now capability-aware (configured host when it answers → this machine's MySQL → `WARN`; a proof that *ran* against a reachable server still `FAIL`s). (b) A **first run with no secrets set** aborted on `The property 'Value' cannot be found` — StrictMode versus a property that does not exist yet. (c) The extra **desktop shortcut** was written to `C:\Users\johnk\Desktop`, an empty husk on this machine, instead of the OneDrive-redirected desktop the shell shows; a redirected desktop now wins, and a path that resolves to the primary desktop is skipped. Verified with a full deployment (`DEPLOYMENT OK - 27 checks, 4 warning(s)`, exit 0), the shortcut on the visible desktop, the service running and its Status page rendering *Scheduled refresh: Disabled here*. |

---

## 2. Carry-forward backlog for the next spec — 127 boxes

Itemised in **`WeekendProject/OPEN-WORK-NEXT-SPEC.md`** §4–§10. Run `/speckit.specify` against **one
workstream at a time**, not the whole set.

| § | Workstream | Carry-forward | Source files |
| --- | --- | ---: | --- |
| 10 | Close out `specs/001` + documentation hygiene | — | `specs/001/tasks.md`, this file |
| 4 | Waitlist handler fulfilment & urgency ordering | 9 + 2 | `PromptFiles/07`, `PromptFiles/08` |
| 5 | Unified 2-line card + Category/Item taxonomy refactor | 20 | `PromptFiles/14-57%` |
| 6 | Waitlist analytics | 20 | `PromptFiles/10` |
| 7 | Administration: cancellations monitor & request retention | 8 | `PromptFiles/11` |
| 8 | User management (Settings → Administration) | 39 | `PromptFiles/15` |
| 9 | Startup gate polish | 2 | `PromptFiles/13` (the two `+ [ ]` boxes; see §4) |
| — | Validation checklist §4–§8 | 27 | `App-Validation-Checklist.md` |

**Checked: 11 + 20 + 20 + 8 + 39 + 2 + 27 = 127.**

Sequencing is in that file's §11; the short version is: **§4 first** now that §10's T106 item is closed and the
SC-007/SC-008 clocks are already running. §10 is documentation hygiene and can run alongside anything.

**Owner decision already taken (2026-09-11): no request-type/subtype editor will be built.** The
catalog-administration workstream is **cancelled, not deferred** — do not resurrect the Developer Settings
editor page, the type edit view, or the guided wizard modal.

---

## 3. Not work — do not carry these forward (223 boxes)

These sets of open boxes **overstate** the remaining work; treating them as a backlog means re-implementing
shipped code or rebuilding something deliberately deleted.

### 3.1 Retired sources (counting traps) — 210 boxes, 0 work

| Source | Open boxes | Why it is not a backlog |
| --- | ---: | --- |
| `WeekendProject/Module_Mock/Tasks.md` | 72 | The **seed** for `specs/001`; the identical work is 145/149 complete there. Ticked once and never reconciled. |
| `PromptFiles/prompt.md` | 23 | The **master Task 1–23 index**; Tasks 1–15 and 19 are 100 % checked in their own files, and the genuinely open ones (16–18, 20–23) already appear in §4/§6/§7. |
| `PromptFiles/14-30%`, `14-45%`, `14-47%`, `14-53%`, `14-55%` | 115 | Earlier **snapshots** of the same list `14-57%` holds live. Retire; never merge their counts. |

### 3.2 Obsolete requirements — cannot be built (13 boxes in live files)

Each depends on something deliberately deleted. `specs/001` FR-003/FR-014 and constitution II forbid
reintroducing any demo/mock mode, and `RetiredSymbolAuditTests` fails the build if a retired symbol returns.
The full table (with the replacement for each) is `OPEN-WORK-NEXT-SPEC.md` §3.

---

## 4. Stale counts in existing documents (correct these when touched)

Found while building this index. None of these are code defects; all of them can mislead a reader.

| Document | Says | Actually |
| --- | --- | --- |
| `OPEN-WORK-NEXT-SPEC.md` §1 | *"`specs/001/tasks.md` itself stands at **139 boxes: 138 `[x]`, 1 `[ ]`**"* | **173 boxes: 173 `[x]`, 0 `[ ]`** — T144/T147/T148 and the T162–T173 passes were added after that line was written, and T106/T151 closed on 2026-09-12 |
| `OPEN-WORK-NEXT-SPEC.md` §10.2 | *"Fix `ChangeLog.Simple.md` … the concrete gap behind SC-016"* | **Done** by `specs/001` T140 |
| `OPEN-WORK-NEXT-SPEC.md` §12 | `12` = 6 open / 6 retired; `13` = 10 open / 2 carry | **`12` = 1 open; `13` = 0 open** — the editor and mock-parity boxes were removed |
| `OPEN-WORK-NEXT-SPEC.md` §12 | Total **151 / 24 / 127** | **140 / 13 / 127** on the live files — 5 boxes were deleted from `12` and 6 from `13`, which is why the open and retired columns fell but the carry-forward total held |
| Everywhere | — | **`PromptFiles/13-63%-Developer-UI.md` uses `+ [ ]`, not `- [ ]`.** A `- [ ]`-only sweep reports it as **0 open**; it actually holds **4** `+ [ ]` boxes. Count both markers |
| `OPEN-WORK-NEXT-SPEC.md` §12 note | *"the file holds **46** unchecked boxes"* | **45** — T142 retired the `Feature.RecvMockData` item |

**Validation-checklist accounting** (`App-Validation-Checklist.md`, **45** unchecked): §0 (3) + §0a (1) +
§1 (8) + §2 (1) + §3 (5) = **18** are `[NOW]` pre-flight gates already executed by `specs/001` T104/T105
(Phase 19: 699 passed / 0 failed / 0 skipped). Only **§4–§8 = 27** belong to the carry-forward backlog — which
is why 27, not 45, appears in §2.

---

## 5. Recommended order

**1–7 are done (2026-09-12).** T144, T147, T148 and T150 were implemented in `/speckit.implement`; T148(a) turned
out to have shipped in `c2d5e04` already. The host-side proof has since run too — see item 6.

1. ~~**T147**~~ — **done.** The shared credential was retired; the API is authorized by the caller's application role.
2. ~~**T148(c)**~~ — **done.** The service writes its own daily log file (`Logs\service_daily_<date>.jsonl`).
3. ~~**T148(a)**~~ — **already shipped** in `c2d5e04` (show-request channel + desktop control shortcut).
4. ~~**T144 (coverage)**~~ — **done.** The key the app sends is now the canonical `WO-######` form, and the five
   population guards and four work-order live reads were narrowed with it. The operator's raw input stays lenient
   (`76951`, `076951`, `WO-076951`) and is auto-formatted to that key **before** the lookup, so the autoformatter
   still works while the collision class stays closed (§1.2).
5. ~~**T150**~~ — **done.** The shape-4 cache no longer stores zero-stock locations (§1.5).
6. ~~**Host validation**~~ — **done 2026-09-12.** `VALIDATION-PROMPT-SERVER.md` was executed on `V-MTMFG-5`
   (`tasks.md` Phase 25), the service and the app were both redeployed (Phase 26), and T151's zero-stock condition is
   confirmed (§1.5). Its signed-in remainder — the last thing T151 had left — was confirmed the same day, so nothing
   is outstanding from it.
7. ~~**T106**~~ — **done 2026-09-12.** The walkthrough was executed and the SC-007/SC-008 clocks were **started**;
   the 30-day re-check is due **2026-10-12** (§1.1). That re-check is a dated follow-up, not an open task — with it
   recorded, **nothing in `specs/001` is open**, so the next step is the new spec.
8. **Next spec** — `/speckit.specify` against `OPEN-WORK-NEXT-SPEC.md` **§4** first (smallest, unblocked, immediate
   user value), with §10 reduced to documentation hygiene now that its T106 item closed. The only dated item left in
   this file is the **2026-10-12** SC-007/SC-008 re-check.

---

*Provenance: box counts are exact counts of `- [ ]` / `+ [ ]` markers in the live files, read 2026-09-11 after
the repository sync (`c2d5e04`, clean tree). T144/T147/T148 detail is quoted from
`specs/001-module-mock-visual-fallback/tasks.md` Phase 23.*

*Updated 2026-09-12 (`/speckit.implement`, fifth pass): **the restore path is fixed too** — it now connects through the
same resolved connection the backups and reads use, and the restore card has its own database picker so the database to
restore is chosen in the restore card rather than in the backup one (T161). Box counts in §1 were re-counted from the
file (`161 boxes: 156 [x], 5 [ ]`). What remains: **T106**, the signed-in remainder of **T151**, and the residuals
**T155-T157**.*

*Updated 2026-09-12 (`/speckit.implement`, sixth pass): **§1 is closed.** The operator verified **T106** and **T151**,
the last two boxes in `specs/001`; §1 was re-counted from the file (**173 boxes: 173 `[x]`, 0 `[ ]`**) and the two
rows in the §1 table, §1.1 and §1.5 were rewritten from "blocked on" to an outcome record. The one thing a tick
cannot mean yet is stated plainly in §1.1: the **SC-007/SC-008 windows were started, not measured** — they need 30
days of wall time and the re-check is due **2026-10-12**. The same pass records **T173** (`tasks.md` Phase 38): a
`RPC_E_WRONG_THREAD` crash that stopped the cached-data indicator from ever appearing, fixed by marshalling the
provider's event onto the UI thread. Being a defect the walkthrough itself surfaced, it is recorded here rather than
left in `tasks.md` alone.*
