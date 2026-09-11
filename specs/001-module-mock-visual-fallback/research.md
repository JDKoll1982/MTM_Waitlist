# Phase 0 Research — Module_Mock: Automatic Infor Visual Read Fallback

**Feature**: `001-module-mock-visual-fallback` | **Date**: 2026-09-09
**Inputs**: `spec.md`; grounding docs `WeekendProject/Module_Mock/{Spec,Plan,Tasks}.md` and
`Discovery/01-MockLogic-Inventory.md`, `Discovery/02-InforVisual-ReadShapes.md`,
`Discovery/03-Hardcoded-MySQL-Sql.md`; repo instructions under `.github/instructions/`.

Every "NEEDS CLARIFICATION" raised by the Technical Context has been resolved below. **No unresolved
NEEDS CLARIFICATION items remain.** Where the specification deliberately leaves a value to implementation time
("reasonable per-store defaults chosen at implementation time and are operator-configurable"), the decision below
fixes a **default**, not a hard-coded constant.

---

## R1. How a cache update becomes visible atomically (FR-006, FR-024, SC-005)

**Decision**: Each read shape owns a live table `<shape>_result` and an identical-structure twin
`<shape>_result_stage`. `sp_visual_<shape>_refresh` truncates the stage twin, loads it with the complete new
result, then performs a single multi-table `RENAME TABLE` that swaps the stage twin into the live name (and the old
live table into a disposable staging name). Readers only ever address the live name, and the swap is one statement.

**Rationale**: MySQL documents `RENAME TABLE` as atomic — "With the transaction table locking conditions satisfied,
the rename operation is done atomically; no other session can access any of the tables while the rename is in
progress", and "If any errors occur during a `RENAME TABLE`, the statement fails and no changes are made." The
documented left-to-right swap idiom (`RENAME TABLE old TO tmp, new TO old, tmp TO new`) is exactly the
"complete snapshot or nothing" behaviour FR-006 requires, with no reader lock wait.
Source: [MySQL 8.0 Reference Manual — RENAME TABLE](https://dev.mysql.com/doc/refman/8.0/en/rename-table.html).

**Alternatives considered**:
- `DELETE` + `INSERT` into the live table — rejected: readers observe an empty or partial table between statements.
- `TRUNCATE` inside a transaction — rejected: `TRUNCATE` is DDL and implicitly commits; not atomic with the load.
- A staging *schema* + view flip — rejected: MySQL has no `CREATE OR REPLACE VIEW`-based atomic flip for tables and
  the `mtm_mock` naming rules are table-oriented.
- Serialising readers behind a `GET_LOCK` — rejected: adds reader latency and a new failure mode (FR-006 forbids
  partial exposure, not concurrency).

---

## R2. Raw result table vs. pre-joined tall/narrow shape (spec §Key Entities, FR-004)

**Decision**: One **flattened, per-read result table** per read shape, mirroring the exact column set the live Visual
read returns (inputs as indexed key columns + outputs + `refreshed_utc`). Not a normalised,
part/work-order/sequence hierarchy and not a single generic key/value table.

**Rationale**: FR-004 and SC-004 require a cached result to be *structurally identical* to the live result so no
caller can distinguish the source. A flattened per-shape mirror is the only shape that satisfies this with a
projection-free `SELECT`. It is also what makes the extensibility playbook (FR-016, SC-012) a copy-and-adapt
procedure rather than a schema-design exercise.

**Alternatives considered**:
- Normalised mirror of the Visual schema — rejected: the fallback would need to re-implement Visual's queries,
  duplicating business logic and re-introducing drift risk.
- One generic `visual_result` table with a shape discriminator — rejected: per-shape typed columns and indexes
  disappear, and readers would need conversion logic, breaking structural identity.

---

## R3. Service process shape, single-instance, and tray lifetime (FR-007, FR-025)

**Decision**: `MTM_Waitlist.Mock.Service` is a WinUI 3 desktop app whose `OnLaunched` creates **only** a
`WinUIEx.TrayIcon` and starts the background engines; the settings window is created on demand from the tray menu.
Single-instance is enforced with `Microsoft.Windows.AppLifecycle.AppInstance.FindOrRegisterForKey` +
`RedirectActivationToAsync`, so a second launch surfaces the running instance instead of starting a second refresh
loop. Closing the settings window hides it (cancels close) and disposes the tray icon only on an explicit Quit.

**Rationale**: WinUIEx documents exactly this "window-less application that can launch a window on demand" pattern
and notes "this icon will keep the application process alive as well", which is what an always-on service needs
without a permanently visible window. `WindowManager.IsVisibleInTray` and the `WindowStateChanged` →
`AppWindow.IsShownInSwitchers` pattern provide minimize-to-tray when the settings window is open.
Sources: [WinUIEx — TrayIcon](https://github.com/dotmorten/winuiex/blob/main/docs/concepts/TrayIcon.md);
[Windows App SDK — AppLifecycle single/multi-instancing](https://github.com/microsoft/windowsappsdk/blob/main/specs/AppLifecycle/Instancing/AppLifecycle%20SingleMulti-Instancing.md)
(instance redirection is a non-terminal operation; the target instance processes the redirected activation through
its normal activation callback).

**Alternatives considered**:
- Windows Service (`sc create`) — rejected: cannot ship the required settings UI/tray presence in the same artifact
  and adds an installer surface the repo does not have.
- A second window with `AppWindow.Hide()` and no tray — rejected: undiscoverable, and the operator cannot quit or
  open settings.
- A console app with a message loop — rejected: the repo has no console tooling precedent, and XAML settings UI is
  required by FR-012.

---

## R4. Auto-start at logon for an unpackaged app (FR-007, SC-011)

**Decision**: Register a per-user auto-start entry under
`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, written and removed by the service's own settings UI.
The app remains a normal unpackaged WinUI 3 app.

**Rationale**: `Windows.ApplicationModel.StartupTask` requires package identity, and this repo builds unpackaged
(`WindowsPackageType=None`). The `Run` key is the per-user, non-elevated, package-identity-free mechanism, and the
repo already guards MSIX-only APIs behind `RuntimeHelper.IsMSIX` — this feature simply needs no such guard because it
uses no MSIX-only API. This also keeps SC-011 trivially satisfied: the client app never depends on the registration.

**Alternatives considered**:
- `StartupTask` (WinAppSDK/WinRT) — rejected: requires package identity.
- Task Scheduler task with "at logon" trigger — rejected: adds a second scheduling system and elevation/ownership
  questions for a single per-user process; also hides the service from the tray.
- A machine-wide `HKLM` Run key — rejected: needs elevation for no benefit (single host, single operator).

---

## R5. Hosting the network API inside a WinUI 3 process (FR-011, FR-026, SC-010)

**Decision**: Add `<FrameworkReference Include="Microsoft.AspNetCore.App" />` to the service app and host a minimal
API (`WebApplication`) with Kestrel on a configurable bind address/port, started by the same
`Microsoft.Extensions.Hosting` host that the tray app creates. A shared-token authentication handler validates a
constant-time comparison of the token against the DPAPI-protected stored value; the token is never rendered to the UI
or written to logs.

**Rationale**: The repo already depends on `Microsoft.Extensions.Hosting` `10.0.10` and builds DI hosts in
`App.xaml.cs`, so hosting a `WebApplication` in-process adds no new architectural concept. Kestrel provides routing,
middleware, and JSON binding for free, and a framework reference is the supported way to get ASP.NET Core without
adding NuGet assets. The `Microsoft.AspNetCore.App` framework can be referenced from a
`net10.0-windows10.0.19041.0` project; the service is self-contained `win-x64` like the app.

**Alternatives considered**:
- `System.Net.HttpListener` — rejected (see plan Complexity Tracking): no middleware pipeline, hand-rolled auth and
  binding, more ways to get SC-010 wrong.
- Embedding a full gRPC/other transport — rejected: FR-011 asks for a small HTTP interface; HTTP keeps the client
  (FR-011 "the application may request an immediate refresh") trivial.
- Out-of-process Kestrel console sidecar — rejected: doubles the deployment surface and the tray/settings UI would
  need an IPC channel anyway.

---

## R6. Storing the shared credential without exposing it (FR-026, SC-010)

**Decision**: The credential is generated on first run, stored as a DPAPI-protected blob under the service's local
app-data folder (`ProtectedData.Protect` with `DataProtectionScope.CurrentUser`), and persisted as an opaque
ciphertext field so it never lands in plaintext configuration. It is written once into the client app's configuration
by the operator (documented in `quickstart.md`) and is never displayed by the service UI or written to any log.

**Rationale**: FR-026 forbids plaintext configuration exposed to callers and forbids displaying/logging the secret.
DPAPI `CurrentUser` scope keeps the secret readable only by the service account on that host, with no extra key
management, and the service already runs as a single per-user process. Machine scope was not needed because the
service is a single-instance per-user on-host process.

**Alternatives considered**:
- Plaintext in `appsettings.json` / a config row — rejected by FR-026.
- Windows Credential Manager — viable, but requires an interop wrapper for no additional property over DPAPI here.
- A DPAPI-protected value stored in `config_settings_values` — rejected: that store is readable by every client and
  FR-026 forbids exposure to callers.

---

## R7. Backup mechanics (FR-009, FR-013, SC-008)

**Decision**: `mysqldump` executed by `BackupEngine` via `Process`, one invocation per store:
`mysqldump --host=<h> --port=<p> --user=<u> --password-file=<f> --single-transaction --routines --databases <db>
--result-file=<path>`. Output goes to the per-store configured destination with a timestamped, store-prefixed
filename. `--result-file` is mandatory; a successful exit code plus a non-empty artifact is the only success signal,
and the artifact is recorded with store, path, size, creation time, and retention state. The binary is probed up
front and its absence is reported as a distinct, non-success outcome (FR-013, edge case "Backup facility missing").

**Rationale**: MySQL documents `--single-transaction` as issuing `START TRANSACTION` with `REPEATABLE READ` so
InnoDB tables are dumped in a consistent state "without blocking any applications", `--result-file` as the required
Windows option because "A dump made using PowerShell on Windows with output redirection creates a file that has
UTF-16 encoding … UTF-16 is not permitted as a connection character set … the dump file cannot be loaded correctly",
and `--routines` as the flag that includes stored procedures (required, since the stores are SP-first after Phase 5).
The password is supplied through an option file rather than the command line, matching the documented guidance that
"Specifying a password on the command line should be considered insecure."
Source: [MySQL 8.0 Reference Manual — mysqldump](https://dev.mysql.com/doc/refman/8.0/en/mysqldump.html).

**Alternatives considered**:
- App-level logical export through `MySqlConnector` — rejected: re-implements dump semantics, loses trigger/event/
  routine fidelity, and cannot guarantee a consistent snapshot as cheaply.
- Physical/file-level copy (mysqlbackup / file copy) — rejected: out of scope for a single-host app, and the four
  stores are modest in size.

---

## R8. Restore semantics and interruption recovery (FR-010, FR-023, SC-009)

**Decision**: Restore runs **only** inside the service process on the MySQL host, is never bound to any network
endpoint, and is gated behind an explicit `ContentDialog` confirmation naming the store and artifact. The sequence is:
(1) take a safety `mysqldump` of the *current* target store; (2) `DROP DATABASE` + `CREATE DATABASE` (charset
`utf8mb4` / `utf8mb4_unicode_ci`); (3) reload the chosen artifact via the `mysql` client without `--force`; (4)
verify by re-counting the store's core tables; (5) record the outcome. Any failure leaves the safety snapshot
identified in the recorded outcome so the operator can recover (edge case "Restore interrupted").

**Rationale**: FR-010 requires a **full replacement** replacement and an explicit confirmation, and FR-023 requires
host-only reachability — so the restore entry point is a UI action on the service, and the network surface
deliberately omits it. Taking a safety dump before the destructive step is what makes the "restore fails partway"
edge case recoverable rather than catastrophic, and it costs one extra dump. Verification by row counts is what makes
SC-009's "verified replacement" claim meaningful rather than asserted.

**Alternatives considered**:
- Restoring over the live schema (`--add-drop-table` reload without dropping the database) — rejected: leaves
  objects that are absent from the artifact, so the result is not "exactly the contents of the selected backup".
- Network-exposed restore behind the same token — rejected explicitly by FR-023.
- Skipping the safety snapshot — rejected: the interrupted-restore edge case would have no recovery path.

---

## R9. Read-shape naming and the one deviation recorded and approved at the Phase 1 DDL review (FR-015, DB rules)

**Decision**: Table and procedure names are exactly the ones fixed by the specification and the grounding docs:
`visual_work_order_lookup_result`, `visual_operation_sequences_result`, `visual_subordinate_parts_result`,
`visual_inventory_locations_result`, `visual_disposition_input_result`, and
`sp_visual_<readShape>_{get,refresh}`. Stage twins are the same name with a `_stage` suffix.

**Rationale**: The spec's Key Entities and the grounding docs treat the shape name as the unit of extensibility
(FR-016), and the Plan's playbook is written against these exact names, so they are treated as frozen inputs rather
than renegotiable identifiers. The locked DB rules require lowercase snake_case with a module prefix, which these
satisfy; `mtm_mock` also carries the required `id` primary key, the `_utc` timestamp (`refreshed_utc`), and its unique
`uq_` keys. (`data-model.md` §3 is the authoritative index list; there is **no** `idx_`-named index in the initial
schema.)

**Deviation — documented and approved (not a blocker)**: the locked rules list `work_order` among "banned words".
`visual_work_order_lookup_result` and the `work_order` key columns retain the term because it names the business
entity, mirrors the Infor Visual source's own vocabulary, and matches the existing repository precedent
`Database/Tables/22_mock_work_orders`. Per the rules an exception requires explicit written approval, so it is recorded
as a header comment in the `create.sql` of both `visual_work_order_lookup_result` and
`visual_disposition_input_result` (and summarized in `data-model.md` §3.1). **Approver: the Tech Lead (database
reviewer) at the Phase 1 DDL review. Recorded location: the Phase 1 pull-request description, alongside the created
artifacts.**

**Alternatives considered**: renaming to `visual_wip_document_*` / `job_number` — rejected: it would diverge from the
spec's frozen shape names and from the source system's terminology, harming the maintainer experience SC-012 measures.

---

## R10. Where `mtm_mock` artifacts live (DB rules; Plan §2)

**Decision**: A per-database subtree `Database/Mock/**` containing `Bootstrap/`, `Tables/`, `StoredProcedures/`,
`Seeds/`, `Validation/`, and the `AllTables.sql` / `AllSPs.sql` / `AllSeeds.sql` master lists for that database.

**Rationale**: The locked rules mandate the file-per-artifact layout and the mandatory
`Bootstrap/update_table_descriptions.sql` maintenance file, but the flat `Database/Tables/` tree is validated as
`mtm_waitlist` (see the `ValidateDatabaseSchemaOnBuild` target in `MTM_Waitlist.csproj`, which passes
`-DatabaseName mtm_waitlist`). The repository already hosts non-`mtm_waitlist` database artifacts in per-database
subtrees (`Database/MTMReceivingApp/`, `Database/MTMWipApp/`), and the grounding Plan §2 names `Database/Mock/`
explicitly.

**Alternatives considered**: adding `mtm_mock` tables to the flat `Database/Tables/<NN>_...` sequence — rejected:
that sequence is the numbered `mtm_waitlist` catalog, the schema validator assumes it, and mixing a second database
into it would break the validator's meaning.

---

## R11. Fallback contract and reachability state machine (FR-002, FR-004, FR-024; flapping edge case)

**Decision**: `MTM_Waitlist.Mock` exposes one narrow seam per shape (`IVisualReadFallback`), each implemented as
*attempt live → on unreachability/timeout read `sp_visual_<shape>_get`*. The detector is a separate, newly written
`VisualReachabilityDetector` publishing `VisualReadStatus` (`Unknown → Live → Cached → Live`) plus a
`CachedDataAgeUtc` used for the cached-data age. Transitions to `Cached` require **two consecutive**
failures; transitions back to `Live` require **one** success; the detector probes on a timer with backoff while
`Cached` so a flapping source cannot thrash the UI or duplicate refreshes.

**Rationale**: FR-024 makes "reachable but legitimately empty" a valid live result and forbids substituting cache:
the decision must therefore be driven by *reachability*, never by *row count*, which is why the detector is a
separate component from the reads themselves. The 2-failure/1-success hysteresis is what satisfies the "flapping
source … must not thrash the UI, duplicate refreshes, or leave the indicator in a wrong state" edge case without
introducing a queue. The seam is deliberately one shape wide so FR-016's "MUST NOT require changes to the fallback
contract for existing shapes" holds: a sixth shape adds a sixth implementation.

**Alternatives considered**:
- Reusing the legacy `MockRoutingService`/`MockModeChangeDetector` stack — rejected outright: it is in the removal
  set (FR-014) and its semantics are a user-facing mode, which FR-003 forbids.
- Treating an empty live result as "unreachable" — rejected explicitly by FR-024 and the spec edge case.
- Per-call probing with no hysteresis — rejected: flapping would advertise a cache in use and then not use it, and
  the indicator would flicker (SC-006 depends on users trusting the indicator).

**Superseded in part (T126, 2026-09-11) — how the per-read decision is actually made.** This entry's title says the
detector drives the switch; in the shipped design the detector drives only the **indicator**, and each read decides
for itself. `VisualQueryExecutor` classifies every attempt as `Ok` / `Unreachable` / `Failed`, and
`IVisualReadFallback` consults `sp_visual_<shape>_get` on `Unreachable` only. That is stronger than a state machine in
one respect that matters: a read taken while the detector still says `Live` — because the first failure of the
two-failure hysteresis is in flight — still falls back correctly, and a `Failed` classification (a real query error)
never becomes a cache hit. The detector's 2-failure/1-success hysteresis and its backoff are unchanged, and they still
govern `IReadStatusProvider`, so the indicator cannot flicker. `CachedReadResult.RefreshedUtc` is `null` for the
reason recorded in `plan.md` → Complexity Tracking; cached-data age comes from
`sp_visual_read_shape_freshness_get`.

---

## R12. Read-only status indicator form and placement (FR-005, FR-022, SC-006)

**Decision**: A non-interactive `InfoBar` rendered in the existing shell (`Module_Mock/Views/ReadStatusIndicator.xaml`,
`IsClosable="False"`, no action buttons, no `Command`), bound to `ReadStatusSnapshot.IsCachedDataInUse` and
`ReadStatusSnapshot.CachedDataAgeUtc`. Visible only while `Cached`; collapses when `Live`.

**Rationale**: FR-005 requires an indicator that is read-only and non-interactive and clears automatically, and
FR-022 requires the cached-data **age** to be shown so the user can judge freshness while explicitly *not* refusing to
serve data by age. `IsClosable="False"` plus the absence of buttons is the concrete, verifiable meaning of
non-interactive (SC-006: "no participant reports being able to change a data mode"). Placing it in the shell keeps it
visible across screens, matching the spec assumption that no new navigation is introduced.

**Alternatives considered**:
- A `ToggleSwitch`/mode selector — rejected by FR-003/FR-005 and explicitly by the test intent in SC-006.
- A flyout/dialog on transition — rejected: intrusive, and it is not a persistent indicator.
- A new dedicated status page — rejected: the spec assumption keeps the indicator inside the existing shell with no
  new navigation.

---

## R13. Repo removal + SP-first enforcement mechanics (FR-014, FR-015, SC-013, SC-015)

**Decision**: Removal and enforcement are verified by two automated checks in `MTM_Waitlist.Tests`:
(1) a **retired-symbol audit** that fails if any of the removed types/keys exist or are referenced
(`ISampleDataService`, `Sample*Catalog`, `Feature.InforVisualMockData`, `Feature.RecvMockData`, `MockToggleService`,
`MockRouting*`, `MockMode*`, `MockConfigurationService`, `MockMasterDataService`, `UseMockData`, `MockDataExpander`,
`sp_mock_*`); and (2) an **inline-SQL audit** that fails if C# source contains SQL statement markers
(`SELECT`/`INSERT`/`UPDATE`/`DELETE`/`CALL` on a MySQL target) outside stored-procedure call sites.

**Rationale**: SC-013 makes "0 references" and "0 inline statement text" measurable outcomes, and SC-015 requires 0
tests referencing the retired systems. Encoding both as tests makes the outcome reproducible in the existing
`dotnet test` gate rather than a one-off manual grep, and it is what keeps Phase 5's SP-first conversion from
regressing. The audit is intentionally a *repo-source scan* and therefore needs no database.

**Alternatives considered**:
- A CI-only PowerShell check — rejected: two enforcement systems for the same rule; the test project already runs in
  every gate the team uses.
- Manual review — rejected: SC-013 explicitly asks for an "automated audit".

---

## R14. Dependencies and sequencing (Plan §3/§4)

**Decision**: Keep the grounding plan's phase order — additive first, then switch, then remove, then clean up:
(0) baseline → (1) `mtm_mock` DB + mirror + SPs + seed → (2) service app → (3) in-app `MTM_Waitlist.Mock` + routing
+ indicator → (4) remove legacy mock systems → (5) SP-first conversion → (6) real-data gap fixes → (7) docs →
(8) validation.

**Rationale**: Phase 3 must exist before Phase 4 deletes the fallback's predecessor, or the app loses its only
Visual resilience mid-sequence. Phases 1→2→3 have a hard data dependency chain (schema before refresh engine before
cached reads). Behaving this way keeps the application shippable at every phase boundary, which matters because the
defect being fixed (SC-001/SC-002) is a production problem and the removal phase is the riskiest.

**Alternatives considered**: removing the legacy system first — rejected: it would leave Visual reads with no
fallback at all during the interval.

---

## R15. Real-data gap closure (FR-019)

**Decision**: `CoilAvailabilityService` is wired to a real source during Phase 6 (no "coil assumed available"
default), and `RequestTypeCatalogService` (DB stored procedures) becomes the single authoritative source for
request-type definitions, with `ImageLocationService` moved off `Assets/Config/waitlist-request-types.json`.

**Rationale**: FR-019 requires coil availability to come from a real source and request-type definitions to have a
single authoritative source; `Discovery/01` notes 4 identifies these as the two competing/absent sources today, and
the spec's §12 behaviour changes list them as gaps to fix while here. Leaving them would mean FR-001 is satisfied
while FR-019 is not.

**Alternatives considered**: deferring both — rejected: they are the residue of the mock system being removed (the
sample coil catalog and the JSON catalog were the mock-era sources), so deferring would leave mock-shaped behaviour
behind under a new name.

---

## R16. Refresh scope and cadence (FR-008, SC-007; operator decision 2026-09-10)

**Decision**: two operator-chosen parameters, both recorded here because neither `spec.md` nor the earlier research
entry (R2) fixed them:

1. **What the mirror holds.** The refresh covers **every Infor Visual work order that is not closed or cancelled** —
   `WORK_ORDER.STATUS` in `{R (Released), U (Unreleased), F (Firmed)}` — together with its **operation sequences and
   subordinate parts**. Closed (`C`) and cancelled (`X`) orders are never cached. This is the same set the app already
   treats as *open*: `RequestDispositionStatusCodes.OpenStatusCodes` (`MTM_Waitlist.Settings`), confirmed against live
   `VISUAL/MTMFG` data on 2026-09-08 (Closed 68,278 · Cancelled 1,404 · Unreleased 42,138 · Released 680 · Firmed 0
   — i.e. roughly **42,800 open work orders**). The *inputs* a refresh reads are therefore the open work-order
   population, not only the work centers the floor currently has set up.
2. **When it runs.** The shipped default cadence is **eight fixed times per day, three hours apart, anchored at local
   midnight**: 00:00, 03:00, 06:00, 09:00, 12:00, 15:00, 18:00 and 21:00 **server-local time**. The schedule is a
   grid, not "interval since the last run", so a slow or skipped cycle cannot drag later runs off those times.

**Rationale**: the operator owns the trade-off between freshness and load on the Infor Visual server, and wants a
predictable whole-population copy rather than a per-dispatcher list. Anchoring the grid to local midnight makes the
eight runs land on round operator-recognisable times regardless of when the service started, and matches the spec's
requirement that a missed cycle must not disturb the next one (US3 acceptance 3).

**Scope consequences recorded at the same time** (they change *how much* data `T113` must read, not the fallback
contract): the five mirror tables keep their approved flattened, wipe-and-swap design (R2) — the cache is **not**
being normalised into shared "parts/sequences/work-orders" building blocks, and the refresh is **not** a
merge/upsert. Two earlier design ideas were considered with the operator on 2026-09-10 and **explicitly not
adopted**: (a) splitting the cache into rarely-refreshed master data plus incrementally merged work orders, and
(b) having the Module_Setup workflow cache a missing job on demand. Both would have changed the approved cache
shape and the "service owns the cache" write rule; the operator asked to change only the two parameters above.

**Alternatives considered**:
- *Cache only the jobs the floor is currently set up on* — rejected by the operator: the copy must cover the whole
  open work-order population, not just active setups.
- *Keep the shipped 15-minute interval* — rejected: with ~42,800 open orders (plus sequences and subordinate parts)
  that is a constant heavy read of Infor Visual for data that changes slowly.
- *Interval-since-last-run scheduling (what the engine did before this decision)* — rejected: run times would drift
  with cycle duration, so the eight daily runs would not land on the times the operator expects.

**Verification note**: `RefreshEngine` implements the grid and `RefreshEngineTests` asserts the 3-hour slots
(04:30 local → 06:00 local → 09:00 local) and the per-shape override grid. The open-order status filter itself lands
with `T113` (the payload source), since that is where the Infor read is authored; `sp_visual_refresh_inputs_get` (or
the per-shape bulk scripts) must select `STATUS IN ('R','U','F')`.

---

## Sources consulted

| Topic | Source |
|---|---|
| Atomic table swap semantics | [MySQL 8.0 Reference Manual — RENAME TABLE](https://dev.mysql.com/doc/refman/8.0/en/rename-table.html) |
| Backup/restore tooling and Windows encoding pitfall | [MySQL 8.0 Reference Manual — mysqldump](https://dev.mysql.com/doc/refman/8.0/en/mysqldump.html) |
| Tray-only WinUI 3 lifetime; minimize-to-tray | [WinUIEx — TrayIcon docs](https://github.com/dotmorten/winuiex/blob/main/docs/concepts/TrayIcon.md) (Context7 `/dotmorten/winuiex`) |
| Single-instance / activation redirection | [Windows App SDK — AppLifecycle single & multi-instancing spec](https://github.com/microsoft/windowsappsdk/blob/main/specs/AppLifecycle/Instancing/AppLifecycle%20SingleMulti-Instancing.md) (Context7 `/microsoft/windowsappsdk`) |
| Repo DB naming + artifact layout | `.github/instructions/database-schema-rules.instructions.md` |
| Repo C#/XAML naming + WinUI guardrails | `.github/instructions/csharp-xaml-naming-rules.instructions.md` |
| Repo MCP-first research policy | `.github/instructions/mcp-doc-research.instructions.md`, `.github/copilot-instructions.md` |

**Tooling note**: the Microsoft Learn MCP server reported as *currently disabled by the user* for this session, so
official-guidance grounding was taken from Context7 (Windows App SDK, WinUIEx) and from vendor documentation fetched
directly. If Microsoft Learn becomes available, the two Windows-specific decisions (R3, R4) are the ones worth
re-confirming against Microsoft docs; neither depends on a preview API.
