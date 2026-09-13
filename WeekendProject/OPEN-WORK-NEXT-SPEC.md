# WeekendProject — Open Work (seed for the next spec)

> **⚠️ Superseded vocabulary in §§4–§10 below (2026-09-13).** This seed is kept as written. Where it describes a
> request **type** or **subtype** — including the catalog-administration workstream and the per-type cards — that
> vocabulary is **retired**: `specs/004-unified-card-item-picker` (FR-023) replaced it with a Category/Item model,
> and its own §10 note that "no request-type/subtype editor will be built" is now settled in code, with
> `RetiredSymbolAuditTests` failing the build if any retired symbol returns.
>
> **State as of 2026-09-13 — §4 is delivered, so the next spec is §5.**
> `specs/003-waitlist-handler-fulfilment` was the §4 workstream (its own spec cites `PromptFiles/07`, `08` and
> `prompt.md` tasks 16–18 as its input) and it is **complete**: 49/49 tasks, build `0 Warning(s) 0 Error(s)`,
> suite `Failed: 0, Passed: 888, Skipped: 19`. §4 below records what it was asked for, what it delivered
> differently, and the two source files that are now their own accounting trap (§2). §5 is the next one.

**Purpose.** This is the input seed for the next `/speckit.specify` run: everything still open in
`WeekendProject/` after `specs/001-module-mock-visual-fallback` shipped. It answers two questions —
*was the WeekendProject context absorbed into `specs/001`?* (partly; see §1) and *what is left?* (§4–§10).
The absorbed-workstream answer now spans three specs, not one — see §1.

**How to use it.** Run `/speckit.specify` against one **workstream** at a time (§5–§10 — §4 is done), not the
whole file. Each workstream is sized to be one feature. The workstreams are grouped the way the source
checklists already group them, so the task text stays traceable to its origin file.

**Provenance.** Every task below was read out of the named source file. Box counts are exact counts of
`- [ ]` / `+ [ ]` markers in the live files at the time of the sweep (2026-09-11); where a count has moved
since, the live figure is given beside it and dated.

---

## 1. Answer: was `WeekendProject` absorbed into `specs/001`?

**Partly — in four distinct states, and it matters which is which.**

| State | Content | Action |
| --- | --- | --- |
| **Fully absorbed** | `Module_Mock/**` (Spec, Plan, Tasks, Discovery/01–03, Planning-Progress, README) → `specs/001` FR-001…FR-027, T001–T111, T115–T126 | Nothing to do. Do **not** re-run it. |
| **Absorbed only as a *removal*** | `PromptFiles/13` Phases 1–2 (mock routing / auto-force / central config / client polling / toast) and `PromptFiles/12` Subphases 0.3–0.5 (mock master tables, registry, `MockMasterDataService`, sample catalogs) | **Superseded, not open.** `specs/001` T034/T035/T038–T042 deleted these stacks and rebuilt the capability differently (`VisualReachabilityDetector`, `IReadStatusProvider`). Close them as retired. |
| **Delivered after `specs/001`, by a later spec** | `PromptFiles/07`, `08` + `prompt.md` tasks 16–18 → **`specs/003-waitlist-handler-fulfilment`** (**complete** 2026-09-13: 49/49 tasks, build clean, 888 passed / 0 failed). `specs/002-truthful-data-and-controls` (`implemented`) took the truthful-data and documentation-reconciliation work — §3 and §10.2 below name its tasks T039/T040/T042/T043/T044. | Do **not** re-run either. §4 records what 003 was asked for and the three places it answered differently. |
| **Not represented at all** | `PromptFiles/10`, `11`, `13`, `14-57%`, `15`, `App-Validation-Checklist` §4–§8, `Documents/Request-Config-Template.csv` | **This is the open work.** §5–§10 below. |

A topic search across all ten files of `specs/001` for `unified`, `User Management`, `IT Department`,
`RoleAuthorization`, `defect_type`, `urgency`, `allot`, `ignored location`, `My Requests`, `NCM`, `analytics`,
`Accept`, `Complete`, `Release`, `retention`, `deep-link`, `toast`, `Request-Config-Template`, `PromptFiles/0`,
`PromptFiles/1` and `Developer Settings` found **one incidental hit** (the word "unified" in
`checklists/requirements.md`, describing a status model). The open work below is genuinely unrepresented.

**Owner decision (2026-09-11): no request-type/subtype editor will be built.** The catalog-administration
workstream is therefore **cancelled**, not deferred — see §3. Do not resurrect the Developer Settings editor
page, the type edit view, or the guided wizard modal when writing the new spec.

`specs/001/tasks.md` now stands at **173 boxes: 173 `[x]`, 0 `[ ]`** — the last two, **T106** and **T151**, were
verified by the operator on 2026-09-12, so §10 is **documentation hygiene only** (its T106 item is closed; the
SC-007/SC-008 re-check is due 2026-10-12).

`specs/003/tasks.md` stands at **49 boxes: 49 `[x]`, 0 `[ ]`**, verified 2026-09-13. One stale line to correct
next time that spec is touched: its `spec.md` header still reads `**Status**: Draft`, which is now wrong — the
companion context records `status: completed`.

---

## 2. Do NOT use these as backlogs (accounting traps)

Open boxes **overstate** the remaining work. Carrying them forward would mean re-implementing shipped code.

| Source | Open boxes | Why it must not be treated as a backlog |
| --- | --- | --- |
| `WeekendProject/Module_Mock/Tasks.md` | 72 | The **seed** for `specs/001`. The identical work is 138/139 complete there. Ticked exactly once (Phase 5.3, `done 2026-09-10 (spec task T097)`) and never reconciled since. |
| `WeekendProject/PromptFiles/prompt.md` | 23 | The **master Task 1–23 index**. Tasks 1–15 and 19 are 100% checked in their per-task files; only 16–18 and 20–23 are genuinely open, and those are listed in §5/§6/§7. |
| `PromptFiles/14-30%`, `14-45%`, `14-47%`, `14-53%`, `14-55%` | 115 combined | Earlier **snapshots** of the same 47-item list that `14-57%` holds live (each revision reworded 3–8 items). Retire them; do not merge their counts. |
| **`PromptFiles/07-8%-Phase2-fulfill.md`** and **`08-70%-Phase2-urgency.md`** | **11 + 3 = 14** | **New 2026-09-13 — this is the seed for `specs/003`.** That spec cites both files in its own `## Input` line and delivered the workstream (§4), but **neither file was ever reconciled**: `07` still shows 11 open and `08` still shows 3, and neither carries a `specs/003` note anywhere. Exactly the trap `Module_Mock/Tasks.md` was. Do not reopen them to "finish" the work; §4 is the record. |

**Suggested handling in the new spec:** add a short "retired sources" note naming these files, so a future
reader does not open `14-30%` and start re-implementing a 30% snapshot of work already at 57%. **Applied** for
the original six: `OPEN-TASKS.md` §3.1 carries that note (`specs/002` FR-027). That note does **not** name
`07`/`08` — add them when it is next touched.

---

## 3. Obsolete requirements — must be amended, not implemented

These appear as open tasks in the source files but **cannot** be built, because the thing they depend on was
deliberately deleted. `specs/001` FR-003/FR-014 and constitution II forbid reintroducing any demo/mock mode, and
`RetiredSymbolAuditTests` fails the build if a retired symbol returns.

| Obsolete task | Source | Replace with |
| --- | --- | --- |
| Handler-data + note in **mock mode**; matching QA tests | `07` "Mock-data flow coverage" (2) | Delete both boxes. **Not deleted as of 2026-09-13** — `07` L40/L41 are still open, which is part of why that file overstates the backlog (§2). |
| Per-subtype max-allotted defaults in **mock mode** | `08` "Mock-data flow coverage" (1) | Delete; the data is already on the real rows. **Not deleted as of 2026-09-13** — `08` L38 is still open. `specs/003` delivered the ordering half of `08` and retired nothing. |
| Stock-snapshot-from-sample-data + its QA test | `10` §0.6 mock parity (2) | Delete; replace with a **test-double** policy consistent with `specs/001`. |
| Aged resolved **sample** request + its QA test | `11` "Mock-data flow coverage" (2) | Delete. |
| **Mock-data toggle** in user-management CRUD (a *locked* requirement) + "Mock-toggle parity" test case | `15` locked requirements + Subphase 1.7 | Amending a locked requirement — do it explicitly in the new spec, not silently. |
| Mock-master read service + mock-master grid editor + its QA test | `12` Subphase 0.4/0.5 (3) | Delete — the mock tables are gone. The real request-type catalog's **data** half is already done (`specs/001` T100/T101); its editor UI is cancelled (see the editor row below). |
| Central mock config + client refresh/race guard | `13` Subphase 2.2 (1) | Delete; superseded by `specs/001` T035. |
| "Mock toggles behave: `Feature.RecvMockData` ON shows sample coil weight" | `App-Validation-Checklist` §3 | Rewrite as a negative check: the key no longer exists. **Applied 2026-09-11** by `specs/001` T142 — the item is struck through and points at the live checks that now cover it. No §10 work needed. |
| **Request-type/subtype editor** — Developer Settings page, Request-Type master list, type edit view, 4-step guided wizard modal, per-control card, and their tests | `PromptFiles/13` Phase 4.3 (6) + `PromptFiles/12` Subphase 0.5/0.6 UI (2) | **Cancelled by owner decision 2026-09-11 — no request-type editor will be used.** Not rebuilt, not rescoped. The editor backend is already gone from the codebase and the editor SPs (`sp_waitlist_request_{types,subtypes}_{insert,update,delete,get_all}`) are already deleted from `AllSPs.sql`, so no code removal is needed — this is a documentation retirement only. **Applied 2026-09-11:** `13` (all of Phase 4 removed), `12` (Subphase 0.5 retired, 0.6 editor tasks removed) and the ChangeLog's Workflow 12/13 appendix. Dated ChangeLog entries from 2026-09-06 were left intact as history. |

**Also stale (not an obsolete task, but a documentation defect) — APPLIED 2026-09-11:** `WeekendProject/ChangeLog.Simple.md`
documented the removed manual Infor Visual toggle as current behaviour (line 29 and feature 19). `specs/001`
T102 swept `ChangeLog.md` + `Module_Mock/*` + `PromptFiles/*`, and T125 swept root `README.md` /
`CHANGELOG.md` / `RELEASE-NOTES.md` — **`ChangeLog.Simple.md` was in neither list**, so SC-016's "0 stale
references in the project documentation" was not met. **Fixed by `specs/001` T140:** the fallback section now
describes the automatic-only fallback, feature 19 is marked withdrawn, and the "still in progress" entry
claiming the cache was a bundled sample is gone. No §10 work needed.

---

## 4. Workstream 1 — Waitlist handler fulfilment & urgency ordering — ✅ DELIVERED (2026-09-13)

**Source:** `WeekendProject/PromptFiles/07-8%-Phase2-fulfill.md` (11 open), `08-70%-Phase2-urgency.md` (3 open),
`prompt.md` Tasks 16–18. **Live open boxes: 9 + 2** (after §3 retireals) — *still 11 + 3 as written, because
§3's deletions were never applied; see §2.*
**Smallest, highest-value, and unblocked** — it depends only on Phase-1 work that is already complete.

> ### Built by `specs/003-waitlist-handler-fulfilment`
>
> `status: completed`, 49/49 tasks. The mapping is not an inference — that spec's own `## Input` line names this
> section's sources: *"Spec seed `WeekendProject/SpecTemplates/02-handler-fulfilment-and-urgency.md` — from
> `PromptFiles/07-8%-Phase2-fulfill.md`, `PromptFiles/08-70%-Phase2-urgency.md`, and `prompt.md` tasks 16–18."*
> Gates re-run 2026-09-13: build `0 Warning(s) 0 Error(s)`; suite `Failed: 0, Passed: 888, Skipped: 19, Total: 907`.
>
> **Three deviations, all deliberate and all recorded in that spec.** Do not read the items below as still-open
> work:
>
> | This section asked for | `specs/003` delivered |
> | --- | --- |
> | §4.2 surface coil/part + requested quantity and the coil's location/stock on the card | **Partly.** FR-014 surfaces the handler data *the request actually carries* — requester, press/work center, remaining/overdue, waiting age, per-type detail values — and forbids substituting a value the request does not have. The real material identifiers (coil/part number, quantity, stock location) are **explicitly deferred to §5**, the unified-card and item-resolution workstream. |
> | §4.3 Release offered to the assigned handler | **Service only.** FR-007 keeps `ReleaseAsync` and tests it (including release-vs-cancel), but **no card and no page draws a Release control** — the action area carries the primary action (Accept, becoming Complete) and Cancel. Surfacing it later means binding the existing command, not adding a second one. |
> | §4.3 Edit visible to the creator, behaviour left as a TODO | **Dropped, not deferred.** FR-020 forbids drawing a control whose activation has no effect, so the creator's Edit affordance was not added. The creator's meaningful action is cancelling their own request. |
>
> **It also went further than this section in four places**, all after the first build was used on the floor: the
> actions moved off the detail page onto the list card (FR-025); the detail page reloads itself every 30 seconds
> while open (FR-026); a new-message indicator marks cards carrying unread human messages (FR-027/FR-028); and
> writing on a request opened to anyone signed in, with the history naming the sender (FR-012/FR-013). A
> concurrent-claim warning was added from the spec review (FR-024). Provenance for all of it is in
> `specs/003-waitlist-handler-fulfilment/spec.md` under **Provenance**.

### 4.1 Baseline gates (verification) — **DONE** (both re-run 2026-09-13)
- DevOps: `MTM_Waitlist.sln` builds clean (0 errors, 0 warnings) via
  `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false`.
- QA: full `MTM_Waitlist.Tests` suite passes (`dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64`).

### 4.2 Handler-facing data (UI + code) — **DELIVERED IN PART** (see the deviation table above)
- Surface the handler-needed data on the request card **and** the detail page: coil/part + requested quantity,
  the coil's location/stock, destination press/work center, requester, and urgency/remaining.
  *(Note: the source's "per mock toggle" parenthetical is stale — data is always live.)*
- QA: tests for handler-data population and note add/persist, then the full suite.

### 4.3 Role- and ownership-aware Accept / Complete / Release (UI + code + auth) — **DELIVERED** (Release is service-only; Edit was dropped)
- **Accept**: unaccepted request + viewer is Material Handler or above → show an **icon button** (not text);
  auto-assign to the signed-in user; status → `In Progress`; persisted.
- After accept the job **stays on the shared list**; Accept disappears for other handlers; only the **assigned**
  handler sees Complete and Release.
- **Complete** → `Done` (single step now; leave TODO hooks for a future multi-step handoff).
- **Release** → returns the job to the open list (**available**, *not* a cancellation).
- **Edit** visible to the creator only, behaviour intentionally a TODO (no behaviour).
- Only the assigned handler can Complete/Release; non-handler and non-owner see **no** actions.
- Handler can add a per-request **Note**, visible on the detail page and in history.
- QA: tests covering accept assignment, others-see-taken, assignee-only complete/release, release-vs-cancel.

### 4.4 Urgency ordering (UI + code) — **DELIVERED** (FR-015; wired at T017–T019, ordered-then-displayed agreement asserted)
- Wire the existing `UrgencyCalculator.OrderMostUrgentFirst` helper into the Waitlist list view model so the
  available/handler list is ordered most-urgent-first (least remaining / overdue first). *The due/remaining/
  overdue math and the per-subtype max-allotted settings editor are already done and ticked.*
- QA: tests for due/remaining/overdue math **and** list ordering, then the full suite.

### Constraint to carry (do not regress) — **HONOURED**
`PromptFiles/03-100%-Phase1-listdetail.md` contains a **"VERIFIED CARD ANATOMY … DO NOT REGRESS"** block. It is
still authoritative — reproduce it in the new spec's constraints rather than restating the card layout from
scratch.

---

## 5. Workstream 2 — Unified 2-line card + Category/Item taxonomy refactor — ▶ **NEXT**

> **This is the next spec** (as of 2026-09-13). §4 shipped as `specs/003-waitlist-handler-fulfilment`, and §11's
> order now starts here. It also **inherits §4's one substantive deviation**: the real material identifiers —
> coil/part number, requested quantity and stock location — are resolved *here* (see §5.6), not there.

**Source:** `PromptFiles/14-57%-Phase2-UnifiedWaitlistCard.md` (**20 open**, the live revision),
`Documents/Request-Config-Template.csv`, `Plan-Design-UnifiedWaitlistCard.md`,
`Plan-Design-TypeCategoryActionRefactor.md`, `Plan-Design-TypesSubtypesCatalog.md`, and
`App-Validation-Checklist.md` §4–§8 (the validation for this work).
**The largest UI surface**, and the prerequisite for much of Workstream 3's card-level metrics.

The source file's own status: *"27/47 = 57%. The open boxes are now the interactive in-app UI phases …
those require running the WinUI app (+ XamlMcp) to build and visually verify."* Its ordering is
Phase 3 (picker) → Phase 4 (card) → Phase 5 (NCM frontend) → Phase 6.2 → Phase 7.

### 5.1 Authoritative input — carry verbatim as requirements
`Documents/Request-Config-Template.csv` is the **23-row row spec** (Pickup 11 / Deliver 8 / Assist 3 / Other 1)
and the "SOURCE OF TRUTH" the checklists defer to ("CSV wins" where docs diverge). Its **`Notes / to-build`
column carries requirements found nowhere else** and must be transcribed into the spec, notably:
- `pickup-fg` / `pickup-wip` / `pickup-outside-service` — *"No persisted source yet; must build Infor Visual
  order lookup (or capture in Module_Setup refactor)"*.
- `pickup-ncm` — *"needs new mysql table + stored procedures + settings panel"*.
- `deliver-wrong-coil` / `deliver-wrong-flatstock` — user-entered explanation.
- `other` — card spans Row 1 with `RowSpan = 2`.

### 5.2 Subphase 1.2 — Category/Item → request model (code)
- Replace `ResolveImagePath`'s ad-hoc subtype keyword matching with the explicit Category/Item model.
  (`ResolveImagePath` is a private static in `WaitlistViewViewModel` and still drives live badge→template
  selection.)
- Update `NewRequestFlowRules.GetDefaultTypes()` to order by Category then Item.

### 5.3 Phase 3 — New Request picker (Category → Item) (UI)
Data core is done; these are the interactive UI steps.
- Re-lay New Request category selection to **Category (Pickup/Deliver/Assist/Other) → Item**, matching CSV
  `Order`. *(Increment #1 landed the canonical two-stage picker; it needs in-app visual verification.)*
- **Conditional visibility**: auto-populated Items (coil/flatstock/die/component/dunnage) appear only if the
  requesting job actually has that part. *Requires the composition-root bridge to `IActiveJobItemResolverService`
  (the NewRequest library references Settings but not Setup).*
- **Dunnage image-card selection**, same card style as Module_Setup; the user always selects the dunnage part.
- **User-entry Item paths**: Die destination, Component pick, NCM defect, Other free text — validated against
  Infor Visual where the CSV says so.
- Keep **destination = requesting work center** for all Deliver rows (no user entry).
- **Zero-payload Items** (Riser Table, Hopper) as pure flag selections.

### 5.4 Phase 4 — Uniform 2-line card (UI + code)
- Build the single uniform 2-line card control: **Line 1 = umbrella verb, Line 2 = item identifier**.
- Move type-specific detail fields **off** the card and onto the detail page.
- Render the full-width **'Other'** card on Row 1 with `RowSpan = 2`.
- Re-resolve item data at render for coil/flatstock/die/dunnage rows from work center/job.
- Update badge/selector so Category maps to the correct image family.

### 5.5 Phase 5 — NCM defect feature (UI + code + tests)
Backend (`waitlist_defect_types` table + CRUD SPs + `DefectTypeCatalogService`) is done.
- Settings panel: **defect-types editor** in Module_Settings (searchable list, add/remove).
- Wire NCM Item **Line 2** = `{PartNumber} / {Defect(User Entry)}` from the managed list.
- QA: NCM defect table + SP + picker tests.

### 5.6 Phase 6.2 — Item resolution implementation (code)
- Implement **FG / WIP / Outside** Item resolution from the confirmed derivation, reusing the
  `GetInventoryLocations.sql` / `LookupWorkOrder.sql` / `GetSubordinateParts.sql` join patterns
  (VISUAL / MTMFG), feeding the classifier's `DispositionInput`.
- Wire `{PartNumber} / {Sequence}` from `setup_active_jobs.sequence_number` and the derived type.
- **Gate:** FG/WIP/Outside items resolve a real part/sequence/disposition — **no hard-coded
  `FG-10042` / `WO-073112 / RM-48190`**.

### 5.7 Phase 7 — Validation & cleanup
- Tests covering catalog load, resolvers, the New Request picker, and card render across **all 23 canonical
  CSV rows**.
- Remove the now-obsolete mock field hard-coding in `WaitlistViewViewModel.AddRequestFields`, replaced by the
  resolvers.
- Security review: role gating and Infor Visual validation for user-entry Items.
- Then run `App-Validation-Checklist.md` §4–§8 (**27 checks**) as the acceptance suite for this workstream.
  *§0–§3 of that file are its `[NOW]` gates and are already executed by `specs/001` T104/T105 — do not re-list
  them here.*

---

## 6. Workstream 3 — Waitlist analytics

**Source:** `PromptFiles/10-0%-Phase3-analytics.md` (**23 open**) + `prompt.md` Tasks 20–21.
**Prerequisite:** resolve the five open decisions in §6.5 **before** specifying.

### 6.1 Baseline gates (verification)
- DevOps: solution builds clean. QA: full suite passes.

### 6.2 Subphase 0.1 — Stock-shortage snapshot at request creation (code + data)
- When a part/coil-bearing request is created, **quietly record a snapshot** of that coil/part's on-hand quantity
  and location, with no handler/requester action. Artifacts: `IStockSnapshotRecorder` in
  `MTM_Waitlist.Core/Contracts/Services`; recorder + snapshot model/service in `MTM_Waitlist.Analytics/Services`
  and `/Models`; the request-creation path invokes the Core contract via DI.
- QA: tests that request creation writes the snapshot, then the full suite.

### 6.3 Subphase 0.2 — Plant Manager waitlist analytics screen (UI + code + auth + nav)
- Role-gated (Plant Manager+) screen presenting: volumes by request type and over time; fill speed / average
  wait; on-time vs overdue; cancellations and reasons; handler workload/performance; and the stock-shortage
  metrics from §6.2. Artifacts: `AnalyticsViewModel` in `MTM_Waitlist.Analytics/ViewModels`; `AnalyticsPage.xaml(.cs)`
  in `Module_Analytics/Views`; metric/query services in `MTM_Waitlist.Analytics/Services`.
- Role-gate to Plant Manager and above; register page + view model via `PageService`/DI and add to shell
  navigation. Artifacts: `AllowedAnalyticsRoles`/`CanViewAnalytics`; `pageService.Configure<AnalyticsViewModel,
  AnalyticsPage>()` + `AddTransient` in `ServiceRegistrationExtensions.cs`; `AddAnalyticsModuleServices` in the
  app-root `ModuleDependencyInjectionExtensions.cs`; `<NavigationViewItem x:Uid="Shell_Analytics">` in
  `ShellPage.xaml` + role visibility; `Shell_Analytics.Content` in `Strings/en-us/Resources.resw`.
- QA: tests for metric computation from audit/snapshot data and role gating.

### 6.4 Subphase 0.3–0.6 — Supervisor analytics, preferences, export, localization
*(Prerequisite: §6.2 + §6.3 complete.)*
- **Supervisor Analytics page + view model**, role-gated at rank ≥ Production Lead, reusing the Waitlist
  building-filter interaction model.
- **Role gate** = Production Lead, Setup Lead, Plant Manager, IT Department, Developer, with explicit **deny**
  behaviour for every other role.
- **Segment** by the day-one user types — Material Handler, Production, Setup — as composable panels so each
  user type renders its own KPI + table set without reshaping page architecture.
- **Filter contract**: User Type, Shift, Building (All / Expo Drive / Vits Drive), plant-wide aggregation, and
  role-switch behaviour. Keep `BuildingSelectionService` as the building source of truth.
- **Database**: persist per-user analytics preferences (auto-refresh interval, visible KPI/table/chart toggles,
  default filters) per `database-schema-rules.instructions.md` — table under `Database/Tables/<NN_name>/` with
  `create.sql` + `rollback.sql` plus aggregate regeneration.
- **Backend**: stored procedures for user-scoped preference read/write, behind a repository/service boundary
  that fails gracefully when a preference row is absent.
- **VM**: default auto-refresh cadence **5 minutes**, with a manual refresh action and a persisted per-user
  override.
- **Print-friendly export**: OS/browser print-to-PDF scoped to the currently filtered data, excluding hidden
  sections. *(A direct PDF generation engine is explicitly out of scope for v1.)*
- **Localization**: `.resw` keys for navigation labels, filter labels, KPI labels and export action text — no
  literal UI text.
- **QA**: unit tests for role access, filter composition, segmentation, role-switch behaviour, preference
  serialization round-trip, refresh cadence, and that printed output reflects active filters and excludes
  hidden sections; then full suite + build green and summarize v1 limitations.

### 6.5 Open decisions to resolve first (blockers, not checkboxes)
1. App User GUID → name mapping.
2. Charting strategy.
3. Building source of truth.
4. Access-enforcement depth.
5. User creation is **excluded** here → routed to Workstream 5.

---

## 7. Workstream 4 — Administration: cancellations monitor & request retention

**Source:** `PromptFiles/11-0%-Phase3-admin.md` (**10 open**) + `prompt.md` Tasks 22–23. **Live: 8** after §3.

### 7.1 Baseline gates (verification)
- DevOps: solution builds clean. QA: full suite passes.

### 7.2 Subphase 0.1 — Admin monitor of cancelled requests (UI + code + auth)
- Role-gated (Developer/admin-level) view monitoring cancelled requests — **who** cancelled, **when**, request
  type/subtype, and **reason** — read from retained cancelled records that are never purged.
- Role-gate the monitor; register page + view model via `PageService`/DI and add to shell navigation.
- QA: tests that the monitor lists retained cancellations with the correct fields and enforces the role gate.

### 7.3 Subphase 0.2 — Resolved-request retention & archival (code + DB + UI)
- Honour `waitlist.resolved_retention_days` (**default 90**): resolved (`Done`/`Cancelled`) requests older than
  the window are **hidden from the active Waitlist list but preserved** — never deleted — for analytics/admin.
- Add active-list filtering so aged resolved requests do not appear, plus a housekeeping/archival routine;
  localize the UI strings.
- QA: tests for retention-window filtering and archival.

---

## 8. Workstream 5 — User management (Settings → Administration)

**Source:** `PromptFiles/15-0%-UserManagement.md` (**41 open**, all unchecked). **Live: 39** after §3.
**Recommendation: its own spec** — it is a self-contained workstream with a locked requirements section, its own
schema, and no dependency on the card work.

Its "**Locked requirements (do not re-litigate)**" section is fully specified: rank hierarchy; `Admin` →
`IT Department`; one role per user; create/edit fields; self-lockout guard; rank rule; audit table; deactivation
does not revoke tokens; search matches username/display name/employee ID/role; **Employee ID is NOT unique**;
username normalization → UPPERCASE plus a data migration; same-transaction profile + role; a new
"Administration" Settings category; **separate Pages**, not dialogs.

### 8.1 Baseline gates (verification)
- DevOps: solution builds clean. QA: full suite passes.

### 8.2 Subphase 1.1 — Database (DB)
- Add `IT Department` to the `auth_roles_catalog` seed (in `Admin`'s place) and regenerate `AllSeeds.sql`.
  Artifact: `Database/Seeds/<NN_name>/create.sql`.
- Add a **field-level** user-management audit table (e.g. `auth_user_management_audit`) capturing display name /
  employee id / active / role / password-reset changes plus acting user and timestamp, per
  `database-schema-rules.instructions.md`.
- Add stored procedures under `Database/StoredProcedures/`: `sp_user_management_list` (optional role filter /
  search, incl. role + active status), `sp_user_management_get`, `sp_user_management_create` (profile + role
  assignment **in one transaction**; enforce the role-rank rule), `sp_user_management_update` (display name /
  employee id / role / active; enforce rank + self-lockout), `sp_user_management_reset_password` (set `0000`,
  `require_password_change = 1`), and `sp_auth_roles_list` (roles for the picker).
- Switch `username_normalized` to **UPPERCASE** and migrate existing lowercase rows. *Affects the unique key,
  login credential reads, and session snapshot reads.*
- Regenerate `AllTables.sql` / `AllSeeds.sql` / `AllSPs.sql`; keep `update_table_descriptions.sql` in sync.
- QA: validate the new schema + SPs against a live `mtm_waitlist` (create/update/reset paths, rank rule,
  self-lockout rejection).

### 8.3 Subphase 1.2 — Shared role helper (code)
- Add `RoleAuthorization` — the rank table plus `IsAtLeast(role, required)` and a helper returning all roles
  ≥ a given rank.
- Refactor the existing hardcoded `Allowed*Roles` arrays (`SettingsViewModel`, `SetupWorkstationViewModel`,
  `DunnageWorkflowService`) onto it.
- Replace literal `"Admin"` references with the `IT Department` role.
- QA: unit tests for rank ordering, `IsAtLeast` boundaries, and the roles-at-or-above list.

### 8.4 Subphase 1.3 — Data layer (code)
- Add `IUserManagementRepository` + implementation calling the new stored procedures (**no inline SQL**).
- Add `IUserManagementService` + implementation for role checks, validation and audit writes; DI-register in
  `ServiceRegistrationExtensions.cs`.
- QA: repository tests — transactional create rolls back **both** rows on mid-way failure; duplicate
  `username_normalized` returns a typed "already exists" result; deactivation does not clear
  `auth_sessions_tokens`; one audit row per create/edit/reset.

### 8.5 Subphase 1.4 — ViewModel + Views (UI)
- Add the **Administration** category and a `CanManageUsers` gate (rank ≥ Production Lead), user list, search
  and sort/filter by role to `SettingsViewModel`.
- Add the Administration category and the **User Management** expander to `SettingsPage.xaml` (list, search,
  New User, per-row Edit) following the existing `muxc:Expander` + `x:Load` pattern.
- Add `CreateUserPage` and `EditUserPage` (+ view models), registered for navigation via DI / `PageService`.
- Render role badges and active/inactive indicators with `TextTrimming="CharacterEllipsis"` + tooltip and
  `Min`/`MaxWidth` — **never** a fixed width.
- Localization: `.resw` keys for category/expander headers, field labels and all action/error text.
- QA: `Module_Settings` view-model tests — list/search/sort/filter, role-rank guard, self-lockout guard,
  create/edit flows.

### 8.6 Subphase 1.5 — Critical mitigations (must implement)
- Do **not** copy the fire-and-forget init pattern (`_ = InitializeHotWorkCentersAsync();`) — use
  `async Task`, never `async void`.
- Do **not** use sync-over-async on the UI thread — read settings/roles with `await` in
  `OnNavigatedTo`/init, never `.Result` / `.GetAwaiter().GetResult()`.
- Keep `ObservableCollection` mutation strictly on the UI thread; marshal with `DispatcherQueue.TryEnqueue`
  (otherwise `RPC_E_WRONG_THREAD`).
- Do **not** wrap the transactional create in blind retry — return a typed "username already exists" on
  duplicate key (1062) or make the SP idempotent.
- Guard the Save command against reentrancy/double-click (`CanExecute` + `IsSaving`).
- Avoid hardcoded widths in the create/edit forms; verify at 150 %+ scaling.
- Use a virtualizing list for the user grid.

### 8.7 Subphase 1.6 — Input validation contract
`Username` 1–128 UPPERCASE · `FirstName`/`LastName` 1–128 → `display_name` ≤ 256 · `EmployeeId` exactly `\d{4}`
and **not unique** · `Role` ≤ current user's rank · `Active` cannot deactivate self · `ResetPassword` behind
confirmation · `SearchQuery` matches username/display name/employee id/role.
**Actor rank MUST be re-validated in the SP/service, not only in the UI.**

### 8.8 Subphase 1.7 — Required test cases
RoleAuthorization rank refusal · self-lockout rejection · transactional rollback · duplicate-username typed
result · uppercase normalization (`jsmith` → `JSMITH`, login matches either case) · Employee ID
non-uniqueness (`6229` shared by two users) · search matches all four fields · UI-thread collection safety ·
deactivate without session revocation · double-click Save creates exactly one row · audit trail writes a row
with acting user + timestamp.
*(The source's twelfth case, "mock-toggle parity", is obsolete — see §3.)*

---

## 9. Workstream 6 — Startup gate polish

**Source:** `PromptFiles/13-63%-Developer-UI.md` (startup-gate items only).

> **Cancelled by owner decision (2026-09-11): no request-type/subtype editor will be used.** That removes the
> entire catalog-administration workstream — the role-gated Developer Settings page, the Request-Type master
> list, the type edit view, the 4-step guided wizard modal, the per-control card, the real-catalog grid editor,
> and their tests (`13` Phase 4.3 ×6, `12` Subphase 0.5/0.6 UI ×2). Nothing is deferred or rescoped here; the
> editor is simply not being built. See §3. The only surviving item from this workstream is the startup gate
> below.

### 9.1 Splash messaging when `mtm_waitlist` is unreachable (UI)
- Confirm and polish the **splash messaging** — a clear, non-technical message ("Cannot connect to the MTM
  Waitlist server…") with a **Retry** option and a **Cancel** that exits.
  *`specs/001` FR-021 covers per-screen internal-store unavailable state; the splash/startup gate copy is not
  specified anywhere else.*

### 9.2 Verification
- QA: verify the hard no-start-without-`mtm_waitlist` gate with the user-facing message; full suite green.

---

## 10. Workstream 7 — Close out `specs/001` and documentation hygiene

**Source:** `specs/001/tasks.md` T106 (since verified — see §10.1) plus the gaps found in §1/§3. With T106 closed, this
workstream is **documentation hygiene only**: §10.2 below.

### 10.1 Spec 001 T106 — end-to-end acceptance — **DONE 2026-09-12**
- **Executed.** The service was published and run on the database host, and `quickstart.md` §1–§8 was walked on a
  running build by the operator: the fallback proof with Infor Visual unreachable, the defect proof on a **signed-in**
  build, and the backup/restore drill. T106 and T151 are ticked in `specs/001/tasks.md` (Phase 39 records the basis).
- **Started, not measured.** The **SC-007 / SC-008 30-day observation windows** (≥ 95 % of scheduled refresh cycles
  succeeding; 100 % of scheduled backup windows producing a restorable artifact per enabled store) began
  **2026-09-12**; the re-check is due **2026-10-12**. That is a dated follow-up rather than an open task, and no tick
  claims the windows passed.

### 10.2 Documentation hygiene (docs)
- ~~Fix `WeekendProject/ChangeLog.Simple.md` — it still documents the removed manual Infor Visual toggle as
  current (line 29, feature 19).~~ **Done** by `specs/001` T140: the fallback section describes the
  automatic-only fallback, feature 19 is marked withdrawn, and the "still in progress" claim that the cache was
  a bundled sample is gone. The same file's feature 18 alert caveat is owned by
  `specs/002-truthful-data-and-controls` T039.
- ~~Retire the stale backlog sources named in §2 (add a "retired sources" note so they are not reopened).~~
  **Done** — the "retired sources" note is in `OPEN-TASKS.md` §3.1, naming each of the six files §2 lists with
  the reason it must not be reopened; this section points at it rather than repeating it (FR-027).
- ~~Reconcile `WeekendProject/Module_Mock/Tasks.md`, which is 72 boxes behind reality.~~ **Done** by
  `specs/002-truthful-data-and-controls` T042: 67 of its 73 boxes are ticked against the shipped code and each
  of the six left open carries its own `— still open 2026-09-12:` reason.
- The remaining corrections this pass owns — the changelogs' wording and the ten defect status lines — are the
  documentation tasks of `specs/002-truthful-data-and-controls` (T039/T040/T043/T044).

---

## 11. Recommended sequencing

**Next up: §5 Unified 2-line card + Category/Item taxonomy.** §4 (row 2) shipped as `specs/003` on 2026-09-13,
so the run starts at row 3; rows 3–7 are unchanged and still apply.

| Order | Workstream | Why here |
| --- | --- | --- |
| 1 | ~~**§10 Close out `specs/001`**~~ — **done 2026-09-12** | Its T106 item is verified and the SC-007/SC-008 clocks are **started** (re-check due 2026-10-12). Only §10.2 documentation hygiene is left, and it can run alongside any other workstream. |
| 2 | ~~**§4 Handler fulfilment & urgency**~~ — **done 2026-09-13** as `specs/003-waitlist-handler-fulfilment` (`completed`, 49/49 tasks) | Smallest, already unblocked, no dependency on the taxonomy refactor, immediate user value. It turned out to need no taxonomy work, which is why it could go first. |
| 3 | **§5 Unified 2-line card + taxonomy** — **NEXT** | Largest UI surface; prerequisite for much of §6's card-level metrics, and now the owner of §4's deferred material identifiers. |
| 4 | **§6 Analytics** | Needs §5 for card-level metrics; resolve its five open decisions first. |
| 5 | **§7 Admin monitor & retention** | Independent, small. |
| 6 | **§8 User management** | Self-contained; can run in parallel with §5–§7 if capacity allows. |
| 7 | **§9 Startup gate polish** | Small and independent. Previously blocked on the editor decision; now that the editor is cancelled it can be done at any time. |

---

## 12. Summary of live open boxes

| Live source file | Open (as written) | Retired (§3) | Delivered by a spec | **Carry forward** |
| --- | ---: | ---: | ---: | ---: |
| `PromptFiles/07-8%-Phase2-fulfill.md` | 11 | 2 | **9** (`specs/003`, §4) | **0** |
| `PromptFiles/08-70%-Phase2-urgency.md` | 3 | 1 | **2** (`specs/003`, §4) | **0** |
| `PromptFiles/10-0%-Phase3-analytics.md` | 23 | 3 | 0 | 20 |
| `PromptFiles/11-0%-Phase3-admin.md` | 10 | 2 | 0 | 8 |
| `PromptFiles/12-71%-MockMasterData-DbDriven.md` | 6 | 6 | 0 | **0** |
| `PromptFiles/13-63%-Developer-UI.md` | 10 *(live: **4**)* | 8 | 0 | 2 |
| `PromptFiles/14-57%-Phase2-UnifiedWaitlistCard.md` | 20 | 0 | 0 | 20 |
| `PromptFiles/15-0%-UserManagement.md` | 41 | 2 | 0 | 39 |
| `PromptFiles/App-Validation-Checklist.md` §4–§8 | 27 | 0 | 0 | 27 |
| **Total** | **151** | **24** | **11** | **116** |

*2026-09-13: §4 shipped, so `07` and `08` contribute **nothing** to the carry-forward — 151 − 24 − 11 = **116**,
down from 127. The 11 `07`/`08` boxes are still listed as open in those files; they are unreconciled, not work
(§2).*

*`12` contributes nothing: its 4 mock-parity boxes are obsolete (§3) and its 2 editor boxes are cancelled
by the 2026-09-11 decision. `13` keeps only the two startup-gate items; its 6 editor boxes are cancelled and
its 1 race-guard box is obsolete — and the file itself has since moved from 10 open to **4**, so its
"open as written" figure is now stale in the safe direction.*

> **The sibling tracker is now behind by the same 11 boxes.** `OPEN-TASKS.md` §2 still reads
> *"Carry-forward backlog for the next spec — 127 boxes"* with its check line *"11 + 20 + 20 + 8 + 39 + 2 + 27 =
> 127"*, and its §5 item 8 still says to specify against *"§4 first"*. Both predate `specs/003`. Correct them
> when that file is next touched: 127 → **116**, the §4 row struck as delivered, and §5 item 8 pointed at §5.
> The two documents overlap by design (this one holds the seed text; that one holds live task state), so fixing
> only one leaves the contradiction intact.

*Reconciling the validation checklist:* the file holds **46** unchecked boxes in total. §0 (3), §0a (1), §1 (8),
§2 (1) and §3 (6) = **19** are its `[NOW]` pre-flight gates, and `specs/001` T104/T105 already executed them
(Phase 19: 699 passed / 0 failed / 0 skipped). Only §4–§8 = **27** belong to this backlog, which is why 27 —
not 46 — appears above.

Excluded by design (not work — see §2): `Module_Mock/Tasks.md` (72), `prompt.md` (23), the five `14-*`
archives (115), and — **new 2026-09-13** — `PromptFiles/07` (11) and `08` (3), whose workstream `specs/003`
delivered without reconciling them.
Also to fold in: `Documents/Request-Config-Template.csv` (23-row spec + `Notes / to-build`), the 19 SVGs in
`Mockups/` (UI reference art, already cited by the checklists), and the three `Plan-Design-*` reference docs.

---

## 13. Brainstorm log — the next spec (started 2026-09-13)

Resolved decisions from the `/speckit.superspec.brainstorm` session on **§5**. Each entry is a decision that
should reach `spec.md` when `/speckit.specify` runs; nothing here is implemented yet.

**Q1 — What should the next spec cover?** → **The picker and the card.** The next spec is **§5.3 (Phase 3,
re-lay the New Request picker as Category → Item) plus §5.4 (Phase 4, the uniform 2-line card)** and nothing
else. §5.5's defect-types editor and §5.6's FG/WIP/Outside item resolution each become **their own spec**.

*Why:* those two are the ones that change what a user sees, and §6's card-level metrics depend on the card.
The other two fail independently and are cheaper to finish on their own than to carry through one long run.

*Consequences for the seed:*
- §5.5 (NCM defect editor) and §5.6 (FG/WIP/Outside resolution) move out of the next spec. §5.1's CSV
  `Notes / to-build` entries for `pickup-fg`, `pickup-wip`, `pickup-outside-service` and `pickup-ncm` are
  **not** in scope — but `pickup-ncm`'s Line 2 format still is, because the card renders it.
- §5.6 is the owner of the real material identifiers that §4 deferred to §5 — so that deferral now lands in a
  *later* spec than the card, not this one. The card in this spec must therefore still show only what the
  request truthfully carries (§4's deviation, unchanged).
- Phase 7's validation narrows to the picker and the card; the resolver tests move with §5.6.

**Open questions raised but not yet asked about (carried forward):**
- Which wins when the CSV's `Notes / to-build` column and the shipped code disagree — the CSV is the declared
  "SOURCE OF TRUTH", yet its `pickup-ncm` row says the defect table, its procedures and its settings panel are
  still to build, while `DefectTypeCatalogService` and the catalog services already exist.
- The source checklist requires every remaining item to be "verified with XamlMcp", but XamlMcp is not wired in
  this repo, so that evidence cannot be produced as written.

**Q2 — The card layout is frozen "do not regress", but Phase 4 contradicts it. Which wins?** → **Retire the old
card and lift the freeze.**

The uniform 2-line card **replaces** the per-type cards; the old design is retired rather than modified. That is
an explicit, deliberate lifting of the 2026-09-06 freeze, not a silent regression — so when the spec is written
it must say so, and the change must carry:

- an update to `specs/003-waitlist-handler-fulfilment/spec.md` **Verbatim Constraints** and its **FR-021**
  (both currently assert the per-type detail grid stays on the card), recorded as a supersession, not a
  deletion of history;
- the four markup tests in `MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistLineCardMarkupTests.cs` that
  lock the old anatomy — `Card_KeepsTheFourMetadataRows`, `PerTypeDetailGrids_KeepTheTwoByThreeFourColumnPattern`,
  `Card_KeepsTheFixedSquareRequestImage`, `Card_KeepsTheCompactStatusPillBelowTheButtons` — rewritten to the new
  anatomy rather than deleted, so they still fail on an unintended change.

*Consequence:* Phase 4's "move type-specific detail fields to the detail page" is now **in** scope and is a
visible removal from the list, not an addition. The per-type `*WaitlistLineView.xaml` `DetailsContent` grids move
with it.

### Where the picker actually stands (explored 2026-09-13)

Section 5 reads as though Phase 3 needs building. Reading the code, the **back end is already done and tested**:

| Already exists | Path |
| --- | --- |
| The 23 canonical Items with Category + Order | `MTM_Waitlist.Settings/Models/RequestItemCatalog.cs`, `Services/RequestItemCatalogService.cs` |
| The visibility rules (which Items a job supports, Deliver destination) | `MTM_Waitlist.Settings/Services/RequestItemPickerRules.cs` |
| A plain "what parts does this job have" snapshot | `MTM_Waitlist.Settings/Models/RequestJobPartAvailability.cs` |
| The service that assembles the ordered, filtered option set | `MTM_Waitlist.Settings/Services/NewRequestPickerService.cs` |
| Legacy subtype → canonical Item mapping | `MTM_Waitlist.Settings/Services/RequestItemLegacyMapper.cs` |

`INewRequestPickerService`'s own documentation says it exists for *"the New Request picker (Phase 3) and the
uniform card renderer (Phase 4)"* to bind to. **What is missing is the screen**: the wizard is still
Work Center → Job Type → Subtype → Details → Preview → Summary → Result, with no Category/Item step anywhere.

So Phase 3 is **wiring an existing service into the wizard**, not building a taxonomy — and the real open
decisions are what the wizard's steps become and what gets stored on a request.

**Also confirmed:** the plumbing blocker is real and narrower than the checklist implies.
`MTM_Waitlist.Waitlist.NewRequest` references Core, Shared, Settings and Waitlist.View — **not Setup** — and
`IActiveJobItemResolverService` lives inside `MTM_Waitlist.Setup/Contracts/Services/SetupContracts.cs`. But
nothing needs to move: `RequestJobPartAvailability` already exists in Settings as *"a pure value snapshot so
picker rules stay testable and DB-free"*, and its own comment says *"a caller (Phase 3) maps an
`IActiveJobItemResolverService` snapshot onto these flags"*. The mapper call belongs at the composition root.

**Q3 — When a request is raised, what should be saved: the old type and subtype, the new Category and Item, or
both?** → **Save only the new Category and Item.** Stop writing `request_type` / `subtype`.

*Blast radius, measured — this is much larger than "wire the picker in", and the spec must carry it:*

| Touched | Where |
| --- | --- |
| Request table columns | `request_type`, `subtype` on the waitlist request, plus their read/write paths |
| Four stored procedures | `sp_waitlist_request_get`, `sp_waitlist_request_insert`, `sp_waitlist_request_list`, `sp_waitlist_request_subtypes_get` |
| The countdown / urgency | `IUrgencyDeadlineService` + `IUrgencySettingsService` (`MTM_Waitlist.Core`), configured **per subtype** — `UrgencyAllotmentEditorViewModel` is the editor |
| The card's picture | `ImageLocationService`, `ImageLocationScope`, `RequestSubtypeInventory`, `RequestSubtypeImagesDialog` — also keyed **per subtype** |
| Settings screens | `RequestSubtypeImagesDialog.xaml`, the allotment editor, `RequestSubtypeDisplayLabelService` |
| The wizard | `NewRequestSubtypePage.xaml`, `NewRequestSummaryPage.xaml` |
| The retired per-type cards | `CoilWaitlistLineView`, `PickupFg…`, `PickupNcm…`, `PickupOs…`, `PickupWip…` (these go with the Q2 freeze lift) |

*Not yet decided, and the next question:* the countdown and the pictures are both configured per subtype today,
so stopping the subtype leaves both without a key for newly raised requests. `RequestItemLegacyMapper` can still
map an **old** row's subtype onto a canonical Item, so historical rows are not lost — but a new request has no
subtype at all unless we keep writing one.

**Q4 — With the subtype gone, what should the countdown and the card pictures key on?** → **Move both onto the
new Item.**

The allotted minutes and the picture overrides are re-keyed from subtype to **Item**. So Settings ends up
configuring, per Item: the allotted minutes (`UrgencyAllotmentEditorViewModel` / `IUrgencySettingsService`) and
the picture (`ImageLocationService` / `ImageLocationScope` / `RequestSubtypeImagesDialog`). Both settings screens
need reworking, and the values already configured per subtype need moving onto their corresponding Items — the
same `RequestItemLegacyMapper` decides which Item each subtype's settings belong to.

*Consequence for the countdown specifically:* `IUrgencyDeadlineService.GetMaxAllottedAsync(...)` currently takes
a subtype and falls back to the documented 30-minute default. Re-keyed to Item, that default stops being a
fallback and becomes the value for any Item nobody has configured yet — so the spec must say what happens for an
Item that has no allotment set.

*Consequence for pictures:* the existing per-subtype image overrides must be migrated, and the file naming for
Item images has to be decided (the placeholder problem specs/003 fixed is the local precedent — a resolver
answer that is really "nothing configured" must not overwrite a good image).

**Q5 — Old requests have no Item stored. Where should the new card get their Item from?** → **Neither derive nor
back-fill: rewrite the seed data to the new structure and reinstall the local database.** The owner will run the
database reinstaller with the new seed before the first UI test, so the sample data is *born* in the new shape
and no migration path is needed at all.

*Why this is mechanically sound — verified:* `Database/install_local_database.vbs` applies
`Seeds\AllSeeds.sql`, and the master list `Database/Seeds/AllSeeds.sql` already exists. The seeds that must move
to the new structure are `seed_waitlist_request_catalog` and `seed_waitlist_requests_default` (plus whatever
holds the per-subtype allotments and pictures, now that Q4 keys those on Item).

*Consequences the spec must carry:*
- **No migration code.** No derive-on-read, no back-fill procedure, no paired data-migration script. The
  reinstall makes the sample data new; there is no old data to preserve in this environment.
- **`RequestItemLegacyMapper` changes role.** It stops being a read-path fallback for historical rows and
  becomes the *paper* tool used to decide which Item each existing subtype's settings belong to. Whether it
  survives in code at all is a spec decision.
- **The installer is the owner's action, never the agent's.** A reinstall is destructive and the constitution
  requires explicit confirmation; the agent must not run it.
- **The seed is a schema artifact** (constitution III), so any new column or table ships `create.sql` +
  `rollback.sql` and its `AllTables.sql` / `AllSeeds.sql` block in the same change.

*Finding that changes the seed work's size:* **the 23 canonical Items are already an in-code catalog**
(`MTM_Waitlist.Settings/Models/RequestItemCatalog.cs`, served by `RequestItemCatalogService`, no database access,
with `RequestItemCatalogTests` asserting all 23 rows and their per-category counts). So the Item list itself does
**not** need seeding. What needs seeding is the *request data* and the *per-Item settings* — and the existing
catalog tables `28_waitlist_request_types` / `29_waitlist_request_subtypes` (seeded by
`seed_waitlist_request_catalog`) become the question: obsolete, or replaced by an Item catalog table.

**Q6 — Where should a request's Category and Item be stored?** → **Give them their own columns, and purge all
the dead weight from the database as well.**

So `category` and `item` become real columns on the request row and `request_type` / `subtype` stop being
written. The owner added an explicit instruction on top of that: **remove what is now dead rather than leaving
it behind.**

*Dead-weight candidates to confirm during planning (named from a usage sweep, not yet individually verified):*

| Candidate | Evidence it may now be dead |
| --- | --- |
| `28_waitlist_request_types`, `29_waitlist_request_subtypes` | They are the *catalog* for a vocabulary the request no longer stores. The Item list is in code, so nothing obviously needs a table. |
| `seed_waitlist_request_catalog` | Seeds the two tables above. |
| `sp_waitlist_request_subtypes_get` | Reads the subtype catalog directly — dead if the catalog goes. |
| `RequestSubtypeInventory`, `IRequestSubtypeDisplayLabelService`, `RequestSubtypeDisplayLabelService` | All named for the retiring vocabulary. |
| `RequestSubtypeImagesDialog.xaml`, `ImageLocationScope` | The per-subtype picture configuration, which Q4 re-keys onto Item. `ImageLocationScope` may still be needed for work-centre images, so confirm before removing. |
| The request's `request_type` / `subtype` columns and their `sp_waitlist_request_{get,insert,list}` references | Replace with `category` / `item`. |

*Rules this purge must obey:* constitution III (paired `create.sql` / `rollback.sql` per artifact, and
`AllTables.sql` / `AllSPs.sql` / `AllSeeds.sql` kept in sync — a *removal* means the master-list block goes too),
and the repo's existing `RetiredSymbolAuditTests`, which fails the build when a retired symbol comes back. The
spec should say which symbols are retired so that guard can be extended to cover them.

**Q7 — What should the New Request wizard look like once Category and Item replace the type and subtype?** →
**Two steps: Category, then Item.**

The wizard becomes **work centre → Category → Item → Details → Preview → Summary → Result**. `NewRequestSubtypePage`
is **deleted**. The existing **Job Type page becomes the Category step**, and the Item step lists the Items the
chosen Category offers for this job — which is what `INewRequestPickerService.GetVisibleItems(category, availability)`
already returns, and what its own comment means by *"the JobType-page list"*.

*Consequences:*
- The **Details** step keeps its job but changes subject: it now collects the user-entry values the CSV marks
  `Needs user entry? = Yes` (Die destination, Component pick, NCM defect, Other free text) rather than a
  per-subtype set of fields.
- The **item list is filtered by the job** (Phase 3's conditional visibility), so the Item step needs the
  `RequestJobPartAvailability` snapshot filled at that moment — the composition-root mapping from
  `IActiveJobItemResolverService` decided above.
- `NewRequestSummaryPage` / `NewRequestPreviewPage` show Category and Item instead of type and subtype.

**Q8 — The checklist demands XamlMcp evidence that cannot be produced here. How should the screens be
verified?** → **Use the scripted UI tests, exactly as `specs/003` did.** This supersedes the checklist's
"verified with XamlMcp" requirement for the next spec.

*The tooling that already exists and is the precedent:*

| Script / recipe | Use |
| --- | --- |
| `tools/capture_app_window.ps1` | Launch-to-screenshot of the shell, waiting for the real shell window (it must not capture the splash) |
| `tools/capture_app_region.ps1` | Cropped capture of one part of the window |
| `tools/ocr_png.ps1` | Reads the captured text back, so a screenshot is machine-checkable rather than looked at |
| `.github/instructions/winui3-ui-automation.instructions.md` | The documented PowerShell recipe: launch, connect by process id, dump the visible text, read window geometry, navigate with `SelectionItemPattern` / `InvokePattern`, close |

*Traps `specs/003` hit and record, which the new spec inherits:* reaching the shell needs **both**
`MTM_WAITLIST_DB_CONNECTION_STRING` and `MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING`; the window must never be
located by title (the splash and the sign-in window report different titles); a **new control's bounding
rectangle must be re-read in the same command that clicks it**, or the click lands on whatever moved into those
coordinates; and the visible-text dump does **not** include buttons — they are a separate `ControlType.Button`
query.

**Q9 — The two Settings screens keyed on the old subtype now edit something nothing reads. Do they come along in
this spec?** → **Bring both Settings screens along.**

So the spec covers **both**: the allotted-minutes editor (`UrgencyAllotmentEditorViewModel` /
`IUrgencySettingsService`) and the picture screen (`RequestSubtypeImagesDialog` / `ImageLocationService`), each
re-keyed from subtype to **Item**.

*Reason this is not optional:* `specs/003` established **FR-020 — no control may be drawn whose activation has no
effect**. Leaving either screen on the old key would leave a Settings screen full of controls that change
nothing, which is the defect that rule exists to prevent.

### Settled scope for the next spec (after Q1 – Q9)

**In:** the Category→Item picker (two steps, job-filtered) · the uniform 2-line card replacing the per-type cards
(freeze lifted deliberately) · per-type detail fields moved to the detail page · `category` + `item` columns
replacing `request_type` / `subtype` · the four affected stored procedures · the dead-weight purge · both per-Item
Settings screens · rewritten seed data · scripted UI verification.

**Out:** the defect-types editor (Phase 5F) and FG/WIP/Outside item resolution (Phase 6.2) — their own specs.
§5.6 still owns the real material identifiers §4 deferred, so the card in this spec shows only what the request
truthfully carries.

**Q10 — An Item has no allotted minutes configured. What should the card show?** → **15 minutes, clearly marked
as a default — and every Item should carry its own default time so that fallback is the exception, not the norm.**

Three decisions, plus one new requirement the answer brought with it:

1. **The fallback is 15 minutes**, not the 30 the code uses today, and it must be **labelled as a default** so
   nobody reads it as a configured deadline. The request stays in the urgency order, which is why the fallback
   exists at all.
2. **Every Item gets a default time in the seed.** The 15-minute value is the safety net for an Item nobody has
   configured, not the expected state.
3. **New requirement — the minutes editor must show the average time each Item actually took to process**, so the
   person editing has a real number to reason from instead of a guess. The value being edited and the value being
   observed sit side by side.

*Consequences the spec must carry for the average:*
- It is a **read over request history**, so it goes through a stored procedure (constitution III), not inline SQL.
- **The seed must make it meaningful.** Because the database is reinstalled from seed (Q5), the seeded requests
  need completed examples with realistic timestamps *per Item* — otherwise every average reads zero and the
  editor's new column teaches nothing.
- **It overlaps §6 (analytics).** That workstream already asks for average wait time and handler performance;
  this is one number from the same family. The spec should say where the calculation lives so §6 reuses it
  rather than building a second one.
- **It is a display of observed data, not stored configuration** — so it must never be written back as if it were
  the configured value. The 15-minute default and the observed average are different numbers with different
  meanings, and the editor has to keep them visibly distinct.

**Q11 — The average processing time: measured between which two moments?** → **Accepted to completed — "how long
the work took", not "how long someone waited".**

So the average is computed from the pair `accepted_utc` → `completed_utc` on completed requests, grouped by Item.
It is deliberately **not** the same number as the countdown a handler sees, which runs from the request and
includes the time nobody had claimed it — the two are different quantities and the editor must not label the
observed average as if it were the deadline.

*Only completed requests count.* A cancelled or released request has no meaningful "work took" value, and a
released one may be re-accepted and completed later, so the calculation must take a request's **final** accepted →
completed pair rather than summing every attempt.

### Carried forward for the spec-writing step (not blocking, but must not be lost)

- **CSV versus code precedence.** The seed calls the CSV the "SOURCE OF TRUTH" and says the CSV wins where
documents disagree. That rule needs narrowing before it reaches `spec.md`, because the CSV's `Notes / to-build`
column is demonstrably stale: its `pickup-ncm` row says the defect table, its procedures and its settings panel
are still to build while `DefectTypeCatalogService` and the catalog services already exist. **The structural
columns (Category, Order, Item, umbrella verb, Card Line 2, `Needs user entry?`, `Options`) are the
specification; the `Notes / to-build` column is a to-do list whose items must each be verified against the code
before being treated as a requirement.**
- **The 'Other' card.** `Phase 4` requires it to render full width on Row 1 with `RowSpan = 2`, but the grid it
  spans is specified in `WeekendProject/PromptFiles/Plan-Design-UnifiedWaitlistCard.md`, which the spec must
  read rather than restate.
- **`Plan-Design-*` paths.** §5's source line names the three `Plan-Design-*.md` documents without a folder; they
  live in `WeekendProject/PromptFiles/`. Worth correcting so the spec does not cite a path that does not exist.
- **The checklist's baseline is stale.** `14-57%` Phase 0 records "552 passed / 8 skipped" (2026-09-08); the suite
  is 888 passed / 19 skipped as of 2026-09-13. Phase 7 also says "all 18" canonical rows where the CSV and
  `RequestItemCatalog` both hold **23**.

---

## 14. Design, first pass — **SUPERSEDED by §16**

> Kept for the record only. **§16 is the current design** and folds in every answer given after this draft was
> presented (§14.8 onward). Do not specify from this section.

Sectioned so each part can be accepted or rejected on its own. Nothing here is built.

### 14.1 What changes on a request

`category` and `item` replace `request_type` and `subtype` on the request row.

| Column | Holds | Example |
| --- | --- | --- |
| `category` | The umbrella category, one of the four | `Pickup` |
| `item` | The canonical Item id, exactly as `RequestItemCatalog` spells it | `pickup-coil` |

The old two columns and everything that reads them are removed, not left alongside. `sp_waitlist_request_get`,
`sp_waitlist_request_insert` and `sp_waitlist_request_list` change to carry the new pair;
`sp_waitlist_request_subtypes_get` is deleted with the catalog it reads. Each ships `create.sql` +
`rollback.sql`, and `AllTables.sql` / `AllSPs.sql` / `AllSeeds.sql` stay in sync.

### 14.2 The picker

The wizard becomes **work centre → Category → Item → Details → Preview → Summary → Result**.

- `NewRequestSubtypePage` is **deleted**; the Job Type page becomes the **Category** step.
- The **Item** step lists the Items that category offers *for this job*, in CSV `Order` — exactly what
  `INewRequestPickerService.GetVisibleItems(category, availability)` returns.
- The availability snapshot is filled at that moment by mapping `IActiveJobItemResolverService` (Setup) onto
  `RequestJobPartAvailability` (Settings) **at the composition root**, which is where the reference direction
  already allows it. Nothing moves between projects.
- The **Details** step collects the values the CSV flags `Needs user entry? = Yes`: Die destination, Component
  pick, NCM defect, and Other free text.
- Deliver rows keep **destination = the requesting work centre**, with no user entry.

### 14.3 The card

One uniform card for every Item — the per-type cards and their views are retired, and the 2026-09-06 freeze is
**lifted deliberately**, not regressed.

- **Line 1** = the umbrella verb (CSV `Umbrella verb (Card Line 1)`).
- **Line 2** = the item identifier (CSV `Card Line 2 (identifier)`) — for NCM, `{PartNumber} / {Defect}`.
- The 96×96 image is chosen by **Category**, not by subtype.
- The status pill, the new-message marker and the action area (the primary action — Accept, becoming Complete —
  plus Cancel) carry over from `specs/003` unchanged in behaviour.
- The per-type `*WaitlistLineView.xaml` `DetailsContent` grids move to the **detail page**.
- **Open point for approval:** does the card keep the four metadata rows (Requested by / Press / Remaining time /
  Waiting)? My recommendation is **yes** — they say the same thing for every Item, so they do not break
  uniformity, and the list is *ordered* by the remaining time, so a handler needs that number on the card to
  understand the order. The alternative is a strict reading of "2-line card" with everything but the two lines
  moved to the detail page.
- The `WaitlistLineCardMarkupTests` that lock the old anatomy are **rewritten to the new anatomy**, not deleted,
  so an unintended change still fails the build.

### 14.4 The two Settings screens

Both re-key from subtype to Item, and the picture screen's dialog stops being called "subtype":

- The **minutes editor** lists Items with the configured allotment **and** the observed average
  (`accepted_utc` → `completed_utc`, completed requests only, final pair), visibly distinct from each other.
- The **picture screen** chooses each Item's image, and inherits `specs/003`'s rule that a resolver answer which
  is really "nothing configured" must never overwrite a good image.
- An Item with no configured minutes uses the **15-minute default, labelled as such**, so it still takes its place
  in the urgency order.

### 14.5 Seed data and the install

The seed is rewritten to the new shape: `seed_waitlist_request_catalog` and `seed_waitlist_requests_default`
carry Category + Item, **every Item gets a default allotment**, and the seeded requests include **completed
examples per Item with realistic timestamps** so the new average column has something to show. The two
subtype-era catalog tables and their seed are removed. The owner runs
`Database/install_local_database.vbs` before the first UI test — the agent never runs it.

### 14.6 Verification

- Constitution VI gates unchanged: clean build, green suite.
- Database artifacts validated against a live `mtm_waitlist`.
- The screens verified with the **scripted UI tests**, as `specs/003` did: launch, read the visible text, navigate
  and click by name, capture and read back the regions that only a picture can prove. The checklist's
  "verified with XamlMcp" requirement is **superseded** — XamlMcp is not wired here and reports zero live apps.
- The `specs/003` running-app traps carry over verbatim (both connection variables needed; never locate the
  window by title; re-read a control's rectangle in the same command that clicks it).

### 14.7 Not in this spec

The defect-types editor (Phase 5F) and FG/WIP/Outside item resolution (Phase 6.2). The card therefore shows only
what a request truthfully carries — the real material identifiers §4 deferred remain deferred, to the spec that
owns §5.6.

---

### 14.8 Outcome of the review — 2026-09-13

**The direction stands.** The owner reviewed §14 and confirms *"the current loadout is good"* — the design is not
rejected, and nothing in §14.1–§14.7 is withdrawn. What was asked for instead is **further brainstorming on top of
it** before the specification is written, so the questions below this section are additions, not corrections.

Two things were settled in the same review:

- **The card keeps all four metadata rows** — Requested by, Press, Remaining time, Waiting. They read the same for
  every Item, so they do not break uniformity, and the remaining time is the number the list order is based on.
- **New requirement — a sorting control.** A flyout on the shell, beside the existing "My Requests" and building
  controls, lets the viewer choose **what the list is sorted by**. Today the order is fixed at most-urgent-first;
  this makes it the viewer's choice. *Not yet decided:* what the sort options are, what the default is, whether the
  choice is remembered per viewer, and how it sits against `specs/003`'s FR-015, which requires most-urgent-first
  ordering and has a gate and tests behind it.

### Q12 — The list is required to be most-urgent-first, and now there is a sorting choice. How do the two
coexist? → **The viewer chooses; urgency becomes just the default.**

`specs/003`'s **FR-015** is amended to read *"defaults to most-urgent-first"* rather than *"MUST be ordered
most-urgent-first"*, and its gate (G7) and the two ordering tests become the **default-order** tests. The safety
that requirement carried is moved to the visual layer: overdue rows already show their remaining time in bold red,
so overdue work is still visible when the order no longer leads with it.

---

## 15. Deep dive — the CSV and the seeded database (2026-09-13)

Read out of `WeekendProject/Documents/Request-Config-Template.csv` (23 rows × 21 columns) and the local MySQL
instance on `127.0.0.1` (server 9.6.0), database `mtm_waitlist` plus the `mtm_mock` mirror.

### 15.1 What the CSV actually specifies

**Counts:** Pickup 11, Deliver 8, Assist 3, Other 1 = **23**, matching `RequestItemCatalog`.

**The pathing axes.** Every row carries `Needs user entry?`, 0–2 sub-actions with a capture and a value type, an
`Options` list, an `Other allows free text?` flag, a source table plus the JSON fields the value comes from, and a
`Card Line 2 (identifier)` template. Those columns *are* the wizard's logic — not prose about it. Fourteen
distinct shapes fall out, but they collapse to seven:

| Shape | Rows | What the Details step does |
| --- | --- | --- |
| **Pure flag** — no value at all | `pickup-riser-table`, `pickup-hopper`, `deliver-riser-table`, `deliver-hopper` | Nothing to collect |
| **Job-derived, no entry** | `pickup-coil`, `deliver-coil`, `deliver-flatstock`, `deliver-die`, `assist-coil-turn`, `assist-table-place`, `assist-table-remove`, `pickup-dunnage`, `deliver-dunnage` | Nothing — the value is read from the job |
| **Job-derived + a choice** | `pickup-die` (destination: Die Shop / Home Location / Other), `pickup-component` (which component) | One enum pick |
| **Job-derived + free text** | `deliver-wrong-coil`, `deliver-wrong-flatstock` | One text explanation |
| **Custom-data value** | `pickup-scrap` | User entry, source `setup_part_sequence_custom_data` |
| **Free text** | `other` | One text message |
| **No source exists yet** | `pickup-fg`, `pickup-ncm`, `pickup-wip`, `pickup-outside-service` | Out of scope for the next spec |

**The visibility rules — and why the Item step can never be empty.** Thirteen rows state a job condition
(`pickup-coil` needs an MMC/MMF subordinate; `pickup-die` an FGT die; `deliver-coil`, `deliver-wrong-coil`,
`assist-coil-turn` need coil; `deliver-wrong-flatstock` flatstock; `pickup-dunnage`/`deliver-dunnage` a dunnage
assignment; `deliver-flatstock`/`deliver-die` "if nothing found, do not show"; `assist-table-place/remove`
subordinate parts). **Ten rows state no condition at all** — and six of those are in scope for this spec:
`pickup-riser-table`, `pickup-hopper`, `deliver-riser-table`, `deliver-hopper`, `pickup-scrap`, `other`.

So the Item step **always has something to offer**; it cannot come up empty. The boundary case is not "nothing to
pick" but "only the job-independent six", which is a normal state rather than an error.

### 15.2 What the seeded database actually holds

| Fact | Value |
| --- | --- |
| `setup_active_jobs` | **0 rows** |
| `setup_part_sequence_custom_data` | **0 rows** |
| `waitlist_requests_queue` | 16 rows, across 15 work centres |
| `waitlist_requests_audit` | 12 rows |
| `waitlist_request_types` / `waitlist_request_subtypes` | 8 / 24 rows |
| `waitlist_defect_types` | **0 rows** |
| `config_images_locations` | **0 rows** |
| `setup_work_centers_catalog` | 25 rows |
| `mtm_mock.visual_subordinate_parts_result` | **1,732 rows** — Component 858, Die 467, Coil 278, Flatstock 129 — across ~360 work orders |

The 16 seeded requests carry `request_type` / `subtype` and are spread across Pending, Accepted, Completed and
Canceled. **`input_value` is populated only for the free-text rows** (`Wrong Coil @ press`, `Forklift Assist`,
both `General Text Entry` rows, `Empty`) and is **NULL for every job-derived row** — the coil, FG, NCM, WIP and
Outside Service requests carry no item value, because there is no job to read one from.

### 15.3 The three findings that change the wiring

1. **`setup_active_jobs` is empty, so the picker's filter has nothing to read.** The Item step's conditional
   visibility depends on `subordinate_parts_json` on the active job. With no active job, every conditional Item is
   hidden and only the job-independent six appear — so **the picker cannot be tested as specified until active
   jobs exist.** The data to create them is available: `mtm_mock.visual_subordinate_parts_result` holds 1,732 real
   subordinate parts across the four categories the CSV keys on.
2. **The legacy→Item mapping is already in the database.** `waitlist_request_types` and
   `waitlist_request_subtypes` each carry a **`category` and an `item_id` column, already populated** — all 24
   subtypes have both, and `Forklift Assist` (the one type with no subtype dependency) has `Other` / `other`. So
   the seed rewrite already has its source of truth for what each existing request should become. This also
   softens Q6's "dead weight": those tables are not merely obsolete, they are **the mapping the new seed is built
   from** — they should be retired *after* the seed rewrite consumes them, not before.
3. **The pathing mechanism already exists, and it is data-driven per type/subtype.** The catalog rows carry
   `control flow` (`direct-to-confirmation` vs `collect-input-then-confirm`), `requires_text_input`, `prompt_text`,
   `min_length`, `max_length` and `center_data_grid_fields_json` (the per-type detail fields that `specs/003`
   renders as the card's per-type grid). **This is the machine that drives the wizard today, and it is keyed on
   the vocabulary being retired** — so the same metadata has to move onto the **Item**.

### 15.4 The wiring question this leaves

Whoever writes the spec must decide **where an Item's pathing metadata lives**: the control flow, whether it needs
text input, its prompt, its length limits, which options it offers, and which detail fields its page shows. Three
candidates, and they are not exclusive:

- **extend the in-code catalog** (`RequestItemCatalog`) — already holds 23 Items with Category/Order/umbrella/
  Line 2/user-entry flag, so it is the natural home; no database access, and its tests already assert the shape;
- **a database table keyed by Item** — keeps the metadata editable without a build, which is what the
  type/subtype tables did;
- **derive it from the CSV** at build time — makes the CSV the single source, but the CSV is not shipped with the
  app.

### Q13 — Where should an Item's pathing metadata live? → **A database table keyed by Item, purely.**

The six fields — control flow, requires-text-input, prompt text, min/max length, options, and the detail fields the
page shows — go into a **new table keyed by Item**. Reason given: *"there may be the need for an editor in the
future."*

*Read this correctly:* the 2026-09-11 decision stands — **no request-type editor is being built now**. This is
about not foreclosing one, which is why all six go in the database rather than into the in-code catalog. The
choice is deliberate and narrow: metadata that an editor *could* one day own must not be compiled in.

*Consequences the spec must carry:*

- **The table replaces `waitlist_request_types` / `waitlist_request_subtypes` as the behaviour source.** Constitution
  III applies: paired `create.sql` / `rollback.sql`, plus the `AllTables.sql` / `AllSeeds.sql` blocks. Retire the
  two old tables only **after** the seed rewrite has consumed their `item_id` mapping (§15.3 finding 2).
- **There are now two sources for an Item, and the spec must say which wins.** The Item's *existence, order,
  category and display text* are in the in-code `RequestItemCatalog` (23 rows, asserted by tests); its *behaviour*
  is in the database. So the spec must define both directions of disagreement: an Item in the catalog with **no**
  metadata row, and a metadata row for an Item **not** in the catalog. Neither may surface as a crash or as a
  silently half-configured Item.
- **A missing metadata row is a new boundary case for the Item step.** It is the same class of problem as
  `specs/003`'s image placeholder — an absent configuration must not be rendered as if it were a real
  configuration.
- **The wizard reads it once on the way in**, not per Item per keystroke, so a slow read cannot stutter the step.
- **The seed must carry a complete set** — every one of the 23 Items gets a metadata row, or the boundary case
  above becomes the normal case on a fresh install.

### Q14 — Where should the picker's job data come from? → *"a trigger or function that creates them after the
tables that house the data pulled from Visual update, or when called"*

**The "after the tables update" half cannot work, and this is checked rather than assumed.**
`Database/Mock/StoredProcedures/sp_visual_subordinate_parts_refresh/create.sql` does **not** insert into the live
table. It truncates a `_stage` table, inserts there, checks the staged count against the payload length ("live
snapshot left untouched" if they disagree), then swaps with a three-way **`RENAME TABLE`**:

```sql
RENAME TABLE visual_subordinate_parts_result       TO visual_subordinate_parts_result_prev,
             visual_subordinate_parts_result_stage TO visual_subordinate_parts_result,
             visual_subordinate_parts_result_prev  TO visual_subordinate_parts_result_stage;
```

Two consequences, both fatal to a row trigger: **MySQL does not fire row triggers on `RENAME TABLE`**, and the
`TRUNCATE` on the stage table fires no `DELETE` trigger either. So a trigger watching the mirror tables would
never fire on a real refresh — only if something wrote straight into the live table, which the atomic swap exists
precisely to avoid. (Separately: the mirror lives in `mtm_mock` and the jobs in `mtm_waitlist`, so a trigger would
be cross-database and would let the *fallback* store drive the *internal* store — the opposite of the constitution's
cache boundary.)

That leaves **"when called"**, which is the half of the answer that stands.

### Q15 — Re-evaluation of the "stored procedure that builds the jobs" suggestion

**Suggested and then withdrawn.** I put the builder procedure forward as the best answer; re-examining it against
the repo, it is the wrong shape, and the reasoning matters more than the conclusion.

| Objection | Evidence |
| --- | --- |
| **It would give `setup_active_jobs` a second writer.** | The table has exactly one write path today: `sp_setup_save_setup` (which stores `p_subordinate_parts_json` verbatim), called from `MTM_Waitlist.Setup/Services/SetupPersistenceService.cs`. The other two procedure hits — `sp_setup_active_jobs_latest_by_work_center_get`, `sp_setup_active_job_dunnage_assignments_get` — are reads. A builder would be a second author of "what parts does this job have", which is the drift constitution III's rationale exists to prevent. |
| **Its only callers would be the installer and the tests.** | That is a test fixture wearing production clothes. The repo's own UI-automation rules say not to add hooks to production code to make testing easier. |
| **It would make the picker look proven while proving nothing.** | The picker reads what **Setup** writes. A builder writes *different* data by a *different* path, so the picker would pass against the fixture while the real integration stayed unverified — a green test over a broken seam. |

**Corrected recommendation, in three parts:**

1. **Seed the active jobs as data.** The real problem is *"this environment has no setups"*, not *"jobs cannot be
   created"*. Seeding keeps the single writer untouched, and the reinstall is already part of the plan (§Q5).
   Precedent exists: `seed_setup_work_centers_default` and `seed_setup_workstations_default` already seed setup
   data, and `Database/Seeds/AllSeeds.sql` is the master list. **No seed populates `setup_active_jobs` today** —
   that is why it is empty.
2. **Build the seeded JSON to the writer's shape, not the mirror's columns.** `sp_setup_save_setup` takes
   `p_subordinate_parts_json JSON` and stores it verbatim; the app deserializes it into `SetupSubordinatePart`
   (`MTM_Waitlist.Setup/Models/SetupModels.cs`) with a normalised `Category` of Coil / Flatstock / Die / Component
   / Other — which is exactly the four categories the mirror holds (plus the fallback). The CSV documents the same
   field set (`part_number, description, location, on_hand_quantity, category`). **A seed built straight from
   `visual_subordinate_parts_result`'s raw columns without that shape would pass on seeded data and fail on real
   data** — so the seed's JSON must round-trip through the same deserializer the app uses, as a test.
3. **Prove the real path once.** Walk one work centre through the actual Work Center Setup flow in the running app
   — against the mock mirror, since `172.16.1.104` is unreachable from this workstation — then confirm the picker
   filters from what Setup actually wrote. That is the integration evidence a fixture cannot supply.

### Q16 — Agreed, "but have it test each possible setup type so nothing is left to chance"

So the seeded jobs are not a handful of plausible examples — they must **cover every visibility condition the
CSV states**, and a test must assert the result per work centre. Counting the distinct job facts the 23 rows
depend on gives a covering set, not a guess:

| Job fact | Which rows depend on it |
| --- | --- |
| has **coil** (MMC subordinate) | `pickup-coil`, `deliver-coil`, `deliver-wrong-coil`, `assist-coil-turn` |
| has **flatstock** (MMF subordinate) | `pickup-coil` (the same row), `deliver-wrong-flatstock` |
| has a **die** (FGT) | `pickup-die` |
| has **components** (neither coil nor flatstock) | `pickup-component` |
| has **dunnage** assigned | `pickup-dunnage`, `deliver-dunnage` |
| has **any subordinate part** | `deliver-flatstock`, `deliver-die` ("if nothing found, do not show"), `assist-table-place`, `assist-table-remove` |

**The covering set is eight seeded work-centre configurations:**

1. coil · 2. flatstock · 3. die · 4. components · 5. dunnage · 6. **everything at once** (proves the full 19-Item
set renders, and that a long Item list scrolls) · 7. **a job with no subordinate parts at all** (proves the
"if nothing found, do not show" rows stay hidden) · 8. **a work centre with no active job** (proves the
job-independent Items still appear).

**And a gate, so it cannot silently drift:** a test asserts, for each of the eight configurations, the *exact* set
of visible Items — driven through `RequestItemPickerRules` / `INewRequestPickerService`, not from a snapshot of
expected strings. Change an Item's visibility condition and that test fails, which is the point of "nothing left
to chance".

### Q17 — The four Items with no source: what happens to them? → *"remove all seeded requests"*

**Recorded, but ambiguous, and it collides with Q10.** The question asked was what happens to the four Items with
no data source; the answer names the seeded *requests*. Two readings, and they differ materially:

- **narrow** — remove the seeded requests *for those four Items* (8 of the 16: Pickup FG ×2, Pickup NCM ×2,
  Pickup WIP ×2, Outside Service ×2), leaving the other 8 as history;
- **literal** — remove *all 16* seeded requests.

**The collision:** Q10 requires the per-Item minutes editor to show **the average time each Item actually took**,
and that number needs completed requests to average. Removing all seeded requests leaves the column permanently
empty on a fresh install — the requirement still works, but the thing it was added to demonstrate has nothing to
show. This must be resolved before the seed is specified, not discovered afterwards.

### Q18 — Which samples get deleted? → **Remove all 16, and recreate properly seeded requests.**

`seed_waitlist_requests_default` is **rewritten, not patched**. The replacement set is built for the new model
rather than inherited from the old one, and it has to serve three named purposes at once:

- **cover the in-scope Items**, tied to the eight job configurations of Q16, so a card exists for every shape the
  picker can produce;
- **carry completed history per Item** with realistic accepted → completed timestamps, so the Q10 average column
  shows real numbers on a fresh install instead of zeros;
- **span the statuses the card must render** — Pending, Accepted, Completed and Canceled — plus at least one
  claimed-by-someone-else row, because `specs/003`'s action gates are exercised by exactly those states.

**No samples are seeded for the four Items with no data source** (`pickup-fg`, `pickup-ncm`, `pickup-wip`,
`pickup-outside-service`) — they cannot be raised until their own spec lands.

---

## 16. The revised design (supersedes §14)

Every answer above is folded in here. Where this section and §14 differ, **this one wins**.

### 16.1 Scope

The picker and the card, plus what those two need to work: the stored-column change, the per-Item metadata table,
both per-Item Settings screens, and the seed rewrite.

### 16.2 What a request stores

- `category` and `item` **replace** `request_type` and `subtype` on the request row. The old two stop being
  written and are removed.
- `sp_waitlist_request_get`, `sp_waitlist_request_insert` and `sp_waitlist_request_list` carry the new pair;
  `sp_waitlist_request_subtypes_get` is deleted.
- Paired `create.sql` / `rollback.sql` each; `AllTables.sql` / `AllSPs.sql` / `AllSeeds.sql` kept in sync.
- **No migration code** — the database is reinstalled from seed (§16.8), so there is no old data to carry.

### 16.3 The Item metadata table (new)

A table keyed by `item` holding what lives on the type/subtype rows today: control flow
(`direct-to-confirmation` vs `collect-input-then-confirm`), requires-text-input, prompt text, min/max length, the
options list, and the detail fields the page shows.

- **A complete row for every one of the 23 Items**, so a missing row is not the normal case.
- **Both directions of disagreement defined** — an Item in the code catalog with no metadata row, and a metadata
  row for an Item not in the catalog. Neither may crash or half-configure an Item.
- Read **once** on the way into the wizard, not per keystroke.
- The code catalog still owns existence, order, Category, umbrella verb and Line 2; its 23-row tests stay.

### 16.4 The wizard

**work centre → Category → Item → Details → Preview → Summary → Result.**

- `NewRequestSubtypePage` is deleted; the Job Type page becomes the **Category** step.
- The **Item** step lists what the job supports, in CSV Order. The job-independent Items are always present (six
  in scope), so the step is never empty.
- The availability snapshot is filled at the composition root, mapping `IActiveJobItemResolverService` onto
  `RequestJobPartAvailability`. Nothing moves between projects.
- **Details renders per Item**: nothing for a pure flag, nothing for a job-derived Item, one enum pick for die and
  component, one text explanation for wrong-coil and wrong-flatstock, a message for Other, a value for Scrap.
- Deliver rows keep destination = the requesting work centre.
- The four Items with no data source are **hidden** until their spec lands.

### 16.5 The card

One uniform card per Item; the per-type cards and their views are retired.

- **Line 1** = umbrella verb · **Line 2** = the CSV's identifier template.
- **Keeps**: the picture (chosen by **Item**, falling back to the Category's image family when the Item has none —
  see the self-review note in §16.14), the four metadata rows, the compact status pill, the new-message marker,
  and the action area from `specs/003` (primary Accept→Complete, plus Cancel).
- **Loses**: the per-type detail grids, which move to the detail page.
- The 2026-09-06 freeze is **lifted deliberately** — `specs/003`'s Verbatim Constraints and FR-021 are updated as a
  supersession, and the four markup tests are rewritten to the new anatomy rather than deleted.

### 16.6 The list and its sorting

- A **sorting flyout** on the shell, beside "My Requests" and the building selector. Most-urgent-first is the
  **default**.
- `specs/003`'s **FR-015** is amended from *"MUST be ordered most-urgent-first"* to *"defaults to
  most-urgent-first"*; its gate and the two ordering tests become the **default-order** tests.
- Overdue rows keep their bold-red remaining time, so overdue work stays visible when the order changes.

### 16.7 The two Settings screens

- **Minutes editor** — per Item: the configured allotment **and** the observed average (accepted → completed,
  completed requests only, final pair), shown as visibly distinct numbers.
- **Picture screen** — per Item, inheriting `specs/003`'s rule that a "nothing configured" answer never overwrites
  a good image.
- An Item with no configured minutes uses **15 minutes, labelled as a default**, and keeps its place in the order.
- Every Item is seeded with a default allotment.

### 16.8 Seeds and install

- `seed_waitlist_requests_default` **recreated**: in-scope Items across the eight job configurations, completed
  history per Item for the averages, and the four card statuses. Nothing seeded for the four sourceless Items.
- **New seeds**: the Item metadata table, the per-Item allotments, the per-Item pictures.
- **Active jobs seeded for eight work-centre configurations**, covering every visibility condition: coil ·
  flatstock · die · components · dunnage · everything at once · a job with no subordinate parts · a work centre
  with no active job. The JSON must be built in the **writer's shape** — `sp_setup_save_setup` stores
  `p_subordinate_parts_json` verbatim and the app deserializes it into `SetupSubordinatePart` with a normalised
  Category — and a test round-trips the seed through that same deserializer.
- `seed_waitlist_request_catalog` is retired **after** its `item_id` mapping has been consumed by the rewrite.
  That consumption happens **at design time**, when the new seed is written — it is not migration code, which
  §16.2 rules out.
- The owner runs `Database/install_local_database.vbs`. **The agent never does.**

### 16.9 Retirements

Removed, with the master lists updated: `waitlist_request_types` · `waitlist_request_subtypes` ·
`seed_waitlist_request_catalog` · `sp_waitlist_request_subtypes_get` · `RequestSubtypeInventory` ·
`IRequestSubtypeDisplayLabelService` / `RequestSubtypeDisplayLabelService` · the request's `request_type` /
`subtype` columns.

**Re-keyed, not removed:** `RequestSubtypeImagesDialog` and its view model are **renamed and re-pointed at Item**
(§16.7) — they are the picture screen the plan keeps, not dead weight. `ImageLocationScope` is likewise
inspected rather than assumed dead, since work-centre images may still use it.

**Inspected, not assumed dead:** `RequestItemLegacyMapper` may still back the seed rewrite.
`RetiredSymbolAuditTests` is extended to cover whatever is retired.

### 16.10 Verification

- Constitution VI gates: clean build, green suite.
- Database artifacts validated against a live local `mtm_waitlist`.
- **Scripted UI tests as `specs/003` did** — launch, read the visible text, navigate and click by name, capture and
  read back the regions only a picture can prove. The checklist's XamlMcp requirement is **superseded**.
- The `specs/003` running-app traps carry over: both connection variables needed; never locate the window by
  title; re-read a control's rectangle in the same command that clicks it.
- **The visibility matrix is a gate**: for each of the eight configurations, the exact set of visible Items,
  driven through the picker rules rather than a snapshot of expected strings.
- **The real path is proven once**: walk one work centre through the actual Setup flow (against the mock mirror,
  since `172.16.1.104` is unreachable from this workstation) and confirm the picker filters from what Setup wrote.

### 16.11 Not in this spec

Finished Goods, NCM, WIP and Outside Service (no data source — their own spec). The defect-types editor. The real
material identifiers §4 deferred: §5.6 still owns them, so the card shows only what a request truthfully carries.

### 16.12 Still open

- **The 'Other' card's full-width two-row span** — read the grid spec from
  `WeekendProject/PromptFiles/Plan-Design-UnifiedWaitlistCard.md` rather than restating it.

*The other three items this list once carried are settled in §16.15: the sort list, the detail fields per Item,
and the role gate on the two Settings screens.*

### 16.13 Approval and the sort list — 2026-09-13

**§16 approved as written.** The design is settled; the first-pass draft in §14 is superseded and kept for the
record only.

**The flyout offers five:** *Most urgent* (**default**) · *Longest waiting* · *Press* · *Requested by* · *Status*.

**Why "Most urgent" and "Longest waiting" are kept even though they overlap** — the owner asked whether they are
not the same thing, and the answer is *not in general*:

- **Waiting** is `requested_utc` → now. It is the "Waiting" row on the card.
- **Urgency** is derived from the **due** value — `target_time_utc`, or `requested_utc + that Item's allotted
  minutes` — and is the "Remaining time" row.

The two diverge exactly by **the per-Item allotment**. Under today's code every Item falls back to the same
30-minute default, so the two orders look almost identical and the distinction is invisible. **This spec is what
creates the difference**, because it gives every Item its own minutes (§16.7) — two requests raised at the same
moment will then have different urgency whenever their Items allow different minutes.

So both stay: they are the two numbers the card already displays, they agree today only because no Item has its
own minutes yet, and they will visibly disagree the moment the per-Item minutes are configured.

**Q20 — Should the sort choice be remembered?** → **Per person, always.** The choice is saved for that viewer and
comes back next time, the way the building selection already does.

*Consequence:* the sort joins the set of per-viewer persisted preferences, so the spec must name the storage used
for it (the existing local-settings mechanism, not a new one) and must define the first-run default — which is
**Most urgent**.

---

### 16.14 Spec self-review — 2026-09-13

Checked §16 against §13 and §15 for placeholders, contradictions, ambiguity and scope. **Two real contradictions
were found, both introduced by this draft, and both now fixed:**

1. **The card's picture contradicted the picture setting.** Q4 moved the pictures onto the **Item**, but §16.5 said
   the card chooses its picture by **Category**. Fixed: the card uses the **Item's** picture, falling back to the
   Category's image family only when the Item has none — which is what Phase 4's "Category maps to the correct
   image family" was always about.
2. **The picture screen was both kept and deleted.** §16.7 keeps it re-keyed onto Item, while §16.9 listed
   `RequestSubtypeImagesDialog` as removed. Fixed: it is **renamed and re-pointed**, not removed; only the
   `RequestSubtype*` names and the type/subtype catalog go.

**Also tightened:** §16.8 now says the legacy `item_id` mapping is consumed **at design time**, so it cannot be
read as the migration code §16.2 rules out.

**Unambiguous, checked:** the four metadata rows stay while the per-type grids move (both settled, and they are
not the same rows); the four sourceless Items are hidden from the picker and absent from the seed; the 15-minute
fallback is a fallback even though every Item is seeded with its own allotment; §16.11's exclusions match §16.1's
scope.

**Gaps the spec still has to fill — deliberate, and listed rather than hidden:** the four items in §16.12 (the
sort list is now settled and is the five named above; what remains is CSV-versus-code precedence, the 'Other'
card's grid span, and each Item's detail fields), plus the role gate on the two Settings screens, which should
reuse the existing `CanManage`-style gate rather than invent one.

### 16.15 Closing decisions — 2026-09-13

Three answers that close out the open list:

**1. The CSV is used *purely* for field definitions, and the field set is expected to change.** The per-Item field
content comes **only** from the CSV — its `Item value type`, `Card Line 2 (identifier)`, `Source columns / JSON
fields`, the two sub-action columns and `Options` together say what a row captures and of what type.

**No legacy fallback.** The retiring `center_data_grid_fields_json` is *not* a second source, and the "CSV wins"
precedence rule has nothing to arbitrate here because there is only one source. Where a legacy screen showed more
than the CSV defines, the extra fields are **not** carried over — accepted deliberately, on the grounds that
fields can be added or changed later.

*Consequence, stated concretely so it is a choice rather than a surprise:* Coil's legacy grid lists five fields
(`Coil number`, `Quantity in house`, `Coil description`, **`Average coil weight`**, **`Requesting work center`**)
while the CSV's `pickup-coil` row defines the first three and only mentions the average coil weight in its
`Notes / to-build` column. Under this rule those two are **absent from the detail page** until someone adds them
to the CSV — they are not smuggled in from the old column.

*What the spec must therefore guarantee:* **the field set is mutable by design.** Adding or changing an Item's
fields must be a **data change, not a code change** — which is the second reason the fields live in the metadata
table (§16.3) and not in the in-code catalog. Consequently **no test may assert a frozen field list**: the tests
assert the *mechanism* (fields are read from metadata, in the declared order, with the right value types and
labels), never the current contents. A test that hard-codes today's fields would turn the next CSV change into a
build failure.

**2. Reuse the existing role gates** on both Settings screens. No new gate is designed and no new role set is
introduced — whatever already governs the allotment editor and the picture screen continues to govern them, only
re-keyed from subtype to Item.

**3. The CSV is not part of the shipped code.** It stays a design and reference document under
`WeekendProject/Documents/`. It is **read at design time** to author the seed, and the runtime reads the seeded
**metadata table** (§16.3) — the app must not read, embed, parse or ship the CSV, and no build step may depend on
it. That is why §15.4's "derive it from the CSV at build time" option is rejected: the CSV is not in the app's
world at runtime.

**Result: the only item still open is the 'Other' card's grid span** (§16.12), which is a matter of reading the
existing design document rather than making a decision.
