# Open Tasks — MTM_Waitlist

**Generated:** 2026-09-11 (from the synced repository at `c2d5e04`, working tree clean).
**Scope:** every live open (`- [ ]` / `+ [ ]`) checkbox in the tracked backlog documents, plus the items that
look open but are not.

**Read this first.** There are two genuinely different kinds of "open" here, and conflating them is the single
most expensive mistake available in this repo:

| Kind | Where | Count |
| --- | --- | ---: |
| **Live, actionable now** | `specs/001-module-mock-visual-fallback/tasks.md` | **2** |
| **Carry-forward for a future spec** | `WeekendProject/**` source checklists | **127** |
| **Not work at all** (retired sources + obsolete requirements) | see §3 | **223** |

Item-level detail for the carry-forward backlog deliberately lives in **one** place —
`WeekendProject/OPEN-WORK-NEXT-SPEC.md` §4–§10 — and is **not** duplicated here. This file is the index; that
file is the itemised breakdown. Duplicating it would create a divergent third copy, which is exactly the trap
§3 exists to prevent.

---

## 1. Live open work — `specs/001-module-mock-visual-fallback/tasks.md`

**149 boxes: 147 `[x]`, 2 `[ ]`.** These two are the only tasks in the active feature that are not done. T147 and
T148 were resolved on 2026-09-12 (see §1.3 and §1.4).

| ID | Severity | Summary | Blocker / decision needed |
| --- | --- | --- | --- |
| **T106** | verification | End-to-end acceptance walkthrough — `quickstart.md` §1–§8 on a running build | Environment + operator interaction; see §1.1 |
| **T144** | coverage | The cache's key domain still can't express most open work orders (transparency half is fixed) | **Operator/product decision** |
| ~~T147~~ | ~~HIGH~~ | **RESOLVED 2026-09-12** — the shared credential was retired; the API is authorized by the caller's application role | — |
| ~~T148~~ | ~~HIGH~~ | **RESOLVED 2026-09-12** — (a) the show-request channel + desktop shortcut shipped; (c) the service now writes its own daily log file | — |

### 1.1 T106 — end-to-end acceptance walkthrough

Execute `quickstart.md` §1–§8 on a running build. Already **done**: §2's publish, host install, tray-only
start, single-instance redirect and API gating (the blocker that stopped Phases 18/19/21 — *"a host-side
deployment to another machine"* — is retired; the host install is in place).

Still outstanding, and why:

| Part | Blocked on |
| --- | --- |
| §2 steps 3–4 (tray Settings surface, operator access), step 7 (one real refresh cycle) | Operator interaction on the host. **T147 is no longer a blocker** — there is nothing to generate or record |
| §3 fallback proof, §4 defect proof | A **signed-in** application session |
| §5 backup/restore drill | A configured service (two secrets) and a throwaway store |
| §7 sixth-shape playbook | A maintainer-day exercise, not a mechanical gate |
| **SC-007 / SC-008** | **30-day observation windows — not started.** They cannot start until the service runs continuously on the host |

A fifth reason was added on 2026-09-11: §3 step 3's *"every journey returns a complete, correctly shaped
result"* would pass on **shape** while still serving a **different work order** than the live read for the same
input — which is T144.

### 1.2 T144 — cache key domain ≠ live read domain (coverage half)

The **transparency** half is **fixed**: all five `Database/InforVisual/Queues/Module_Mock/Populations/*.sql`
now emit exactly the live-resolvable key domain, verified live (`work_order_lookup` 701 · `operation_sequences`
893 · `subordinate_parts` 1,746 · `inventory_locations` 79,457 · `disposition_input` 701).

What remains is **coverage**, and it needs a product decision. The application's input rule
`^(?:WO-)?(\d{5,6})$` (`MTM_Waitlist.Setup/Services/WorkOrderValidationService.cs`) **cannot express 42,143 of
the 42,852** open Infor Visual work orders:

| Family | `BASE_ID` form | Open | Live-resolvable | In cache |
| --- | --- | ---: | --- | --- |
| `W` | literally `WO-` + digits | 555 | yes (verbatim) | yes (after fix) |
| `M` | bare numeric, exactly 6 digits | 154 | yes (via stripped form) | yes |
| `M` | bare numeric, 5 digits | 76 | **no** (padded key matches nothing, or a *different* `WO-` order) | **no** (correctly) |
| other | `Q` family (37,259) + non-prefixed `W` (140) + remaining `M` (4,668) | 42,067 | **no** — not expressible by the input rule at all | n/a |

Widening the rule changes the five shared live scripts **and** `WorkOrderValidationService`, i.e. product
behaviour outside this feature — hence a decision, not a fix. **3** of the 76 five-digit cases produce a
cross-order collision, which is why simply adding the bare numeric form to the live scripts is *not* a safe
shortcut: one key would then match two different orders.

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
without cryptographic authentication. **Host verification is outstanding** — see `VALIDATION-PROMPT-SERVER.md`.

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
  and assert it — deploy is now **22** checks; treat a host with no login as unconfigured so the designed
  message shows instead of a raw access-denied; poll for process exit after the stop instead of sampling once).
  **Verified:** first ever successful refresh — all five shapes `Succeeded` in ~4.4 s.

**Order of attack (as executed, 2026-09-12):** T147 and (c) are done; the remaining host step is to re-run the
deployment and re-check §2 step 7, which is now the first item of `VALIDATION-PROMPT-SERVER.md`.

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

Sequencing is in that file's §11; the short version is: **§10 first**, because it starts the SC-007/SC-008
30-day clocks that need wall time.

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
| `OPEN-WORK-NEXT-SPEC.md` §1 | *"`specs/001/tasks.md` itself stands at **139 boxes: 138 `[x]`, 1 `[ ]`**"* | **149 boxes: 145 `[x]`, 4 `[ ]`** — T144, T147, T148 were added after that line was written |
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

**1–3 are done (2026-09-12).** T147 and T148(c) were implemented in `/speckit.implement`; T148(a) turned out to have
shipped in `c2d5e04` already. What remains is the host-side proof of those changes, which is
`VALIDATION-PROMPT-SERVER.md` in the repository root.

1. ~~**T147**~~ — **done.** The shared credential was retired; the API is authorized by the caller's application role.
2. ~~**T148(c)**~~ — **done.** The service writes its own daily log file (`Logs\service_daily_<date>.jsonl`).
3. ~~**T148(a)**~~ — **already shipped** in `c2d5e04` (show-request channel + desktop control shortcut).
4. **Host validation** — run `VALIDATION-PROMPT-SERVER.md` on `V-MTMFG-5` before treating either change as verified.
5. **T144 (coverage)** — needs your product decision on the work-order input rule. Do it before §3's fallback
   proof is treated as meaningful, since that proof can pass on shape while serving the wrong order.
6. **T106** — the walkthrough and the SC-007/SC-008 clocks. Start the clocks as early as possible; they need
   30 days of wall time and nothing else in this list is on the critical path for them.
7. **Next spec** — `/speckit.specify` against `OPEN-WORK-NEXT-SPEC.md` §10 first, then §4 (smallest,
   unblocked, immediate user value).

---

*Provenance: box counts are exact counts of `- [ ]` / `+ [ ]` markers in the live files, read 2026-09-11 after
the repository sync (`c2d5e04`, clean tree). T144/T147/T148 detail is quoted from
`specs/001-module-mock-visual-fallback/tasks.md` Phase 23.*

*Updated 2026-09-12 (`/speckit.implement`): T147 and T148 are resolved — see §1.3/§1.4 and §5. Box counts in §1 were
re-counted from the file after the change (`147 [x]` / `2 [ ]`). The remaining host-side proof is
`VALIDATION-PROMPT-SERVER.md` in the repository root.*
