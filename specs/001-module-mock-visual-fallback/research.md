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

---

## R2. Raw result table vs. pre-joined tall/narrow shape (spec §Key Entities, FR-004, SC-004)

**Decision**: One **flattened, per-read result table** per read shape, mirroring the exact column set the live Visual
read returns (inputs as indexed key columns + outputs + `refreshed_utc`). Not a normalised,
part/work-order/sequence hierarchy and not a single generic key/value table.

---

## R3. Service process shape, single-instance, and tray lifetime (FR-007, FR-012, FR-025)

**Decision**: `MTM_Waitlist.Mock.Service` is a WinUI 3 desktop app whose `OnLaunched` creates **only** a
`WinUIEx.TrayIcon` and starts the background engines; the settings window is created on demand from the tray menu.
Single-instance is enforced with `Microsoft.Windows.AppLifecycle.AppInstance.FindOrRegisterForKey` +
`RedirectActivationToAsync`, so a second launch surfaces the running instance instead of starting a second refresh
loop. Closing the settings window hides it (cancels close) and disposes the tray icon only on an explicit Quit.

---

## R4. Auto-start at logon for an unpackaged app (FR-007, SC-011)

**Decision**: Register a per-user auto-start entry under
`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, written and removed by the service's own settings UI.
The app remains a normal unpackaged WinUI 3 app.

---

## R5. Hosting the network API inside a WinUI 3 process (FR-011, FR-026, SC-010)

**Decision**: Add `<FrameworkReference Include="Microsoft.AspNetCore.App" />` to the service app and host a minimal
API (`WebApplication`) with Kestrel on a configurable bind address/port, started by the same
`Microsoft.Extensions.Hosting` host that the tray app creates. A shared-token authentication handler validates a
constant-time comparison of the token against the DPAPI-protected stored value; the token is never rendered to the UI
or written to logs.

---

## R6. Storing the shared credential without exposing it (FR-026, SC-010)

**Decision**: The credential is generated on first run, stored as a DPAPI-protected blob under the service's local
app-data folder (`ProtectedData.Protect` with `DataProtectionScope.CurrentUser`), and persisted as an opaque
ciphertext field so it never lands in plaintext configuration. It is written once into the client app's configuration
by the operator (documented in `quickstart.md`) and is never displayed by the service UI or written to any log.

---

## R7. Backup mechanics (FR-009, FR-013, SC-008)

**Decision**: `mysqldump` executed by `BackupEngine` via `Process`, one invocation per store:
`mysqldump --host=<h> --port=<p> --user=<u> --password-file=<f> --single-transaction --routines --databases <db>
--result-file=<path>`. Output goes to the per-store configured destination with a timestamped, store-prefixed
filename. `--result-file` is mandatory; a successful exit code plus a non-empty artifact is the only success signal,
and the artifact is recorded with store, path, size, creation time, and retention state. The binary is probed up
front and its absence is reported as a distinct, non-success outcome (FR-013, edge case "Backup facility missing").

---

## R8. Restore semantics and interruption recovery (FR-010, FR-023, SC-009)

**Decision**: Restore runs **only** inside the service process on the MySQL host, is never bound to any network
endpoint, and is gated behind an explicit `ContentDialog` confirmation naming the store and artifact. The sequence is:
(1) take a safety `mysqldump` of the *current* target store; (2) `DROP DATABASE` + `CREATE DATABASE` (charset
`utf8mb4` / `utf8mb4_unicode_ci`); (3) reload the chosen artifact via the `mysql` client without `--force`; (4)
verify by re-counting the store's core tables; (5) record the outcome. Any failure leaves the safety snapshot
identified in the recorded outcome so the operator can recover (edge case "Restore interrupted").

---

## R9. Read-shape naming and the one deviation recorded and approved at the Phase 1 DDL review (FR-015, FR-016, SC-012, DB rules)

**Decision**: Table and procedure names are exactly the ones fixed by the specification and the grounding docs:
`visual_work_order_lookup_result`, `visual_operation_sequences_result`, `visual_subordinate_parts_result`,
`visual_inventory_locations_result`, `visual_disposition_input_result`, and
`sp_visual_<readShape>_{get,refresh}`. Stage twins are the same name with a `_stage` suffix.

**Deviation — documented and approved (not a blocker)**: the locked rules list `work_order` among "banned words".
`visual_work_order_lookup_result` and the `work_order` key columns retain the term because it names the business
entity, mirrors the Infor Visual source's own vocabulary, and matches the existing repository precedent
`Database/Tables/22_mock_work_orders`. Per the rules an exception requires explicit written approval, so it is recorded
as a header comment in the `create.sql` of both `visual_work_order_lookup_result` and
`visual_disposition_input_result` (and summarized in `data-model.md` §3.1). **Approver: the Tech Lead (database
reviewer) at the Phase 1 DDL review. Recorded location: the Phase 1 pull-request description, alongside the created
artifacts.**

---

## R10. Where `mtm_mock` artifacts live (DB rules; Plan §2)

**Decision**: A per-database subtree `Database/Mock/**` containing `Bootstrap/`, `Tables/`, `StoredProcedures/`,
`Seeds/`, `Validation/`, and the `AllTables.sql` / `AllSPs.sql` / `AllSeeds.sql` master lists for that database.

---

## R11. Fallback contract and reachability state machine (FR-002, FR-003, FR-004, FR-016, FR-024, SC-006; flapping edge case)

**Decision**: `MTM_Waitlist.Mock` exposes one narrow seam per shape (`IVisualReadFallback`), each implemented as
*attempt live → on unreachability/timeout read `sp_visual_<shape>_get`*. The detector is a separate, newly written
`VisualReachabilityDetector` publishing `VisualReadStatus` (`Unknown → Live → Cached → Live`) plus a
`CachedDataAgeUtc` used for the cached-data age. Transitions to `Cached` require **two consecutive**
failures; transitions back to `Live` require **one** success; the detector probes on a timer with backoff while
`Cached` so a flapping source cannot thrash the UI or duplicate refreshes.

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

## R12. Read-only status indicator form and placement (FR-003, FR-005, FR-022, SC-006)

**Decision**: A non-interactive `InfoBar` rendered in the existing shell (`Module_Mock/Views/ReadStatusIndicator.xaml`,
`IsClosable="False"`, no action buttons, no `Command`), bound to `ReadStatusSnapshot.IsCachedDataInUse` and
`ReadStatusSnapshot.CachedDataAgeUtc`. Visible only while `Cached`; collapses when `Live`.

---

## R13. Repo removal + SP-first enforcement mechanics (FR-014, FR-015, SC-013, SC-015)

**Decision**: Removal and enforcement are verified by two automated checks in `MTM_Waitlist.Tests`:
(1) a **retired-symbol audit** that fails if any of the removed types/keys exist or are referenced
(`ISampleDataService`, `Sample*Catalog`, `Feature.InforVisualMockData`, `Feature.RecvMockData`, `MockToggleService`,
`MockRouting*`, `MockMode*`, `MockConfigurationService`, `MockMasterDataService`, `UseMockData`, `MockDataExpander`,
`sp_mock_*`); and (2) an **inline-SQL audit** that fails if C# source contains SQL statement markers
(`SELECT`/`INSERT`/`UPDATE`/`DELETE`/`CALL` on a MySQL target) outside stored-procedure call sites.

---

## R14. Dependencies and sequencing (SC-001, SC-002; Plan §3/§4)

**Decision**: Keep the grounding plan's phase order — additive first, then switch, then remove, then clean up:
(0) baseline → (1) `mtm_mock` DB + mirror + SPs + seed → (2) service app → (3) in-app `MTM_Waitlist.Mock` + routing
+ indicator → (4) remove legacy mock systems → (5) SP-first conversion → (6) real-data gap fixes → (7) docs →
(8) validation.

---

## R15. Real-data gap closure (FR-001, FR-019)

**Decision**: `CoilAvailabilityService` is wired to a real source during Phase 6 (no "coil assumed available"
default), and `RequestTypeCatalogService` (DB stored procedures) becomes the single authoritative source for
request-type definitions, with `ImageLocationService` moved off `Assets/Config/waitlist-request-types.json`.

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

**Scope consequences recorded at the same time** (they change *how much* data `T113` must read, not the fallback
contract): the five mirror tables keep their approved flattened, wipe-and-swap design (R2) — the cache is **not**
being normalised into shared "parts/sequences/work-orders" building blocks, and the refresh is **not** a
merge/upsert.

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

<!-- token-budget: compacted (level=aggressive) on 2026-09-12T16:34:34Z; original at research.full.md -->
