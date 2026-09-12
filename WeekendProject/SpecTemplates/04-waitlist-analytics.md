# Spec seed 04 — Waitlist analytics

| Field | Value |
| --- | --- |
| **Suggested feature name** | `waitlist-analytics` |
| **Source** | `WeekendProject/OPEN-WORK-NEXT-SPEC.md` §6 — from `PromptFiles/10-0%-Phase3-analytics.md` (**20 carry-forward**) + `prompt.md` Tasks 20–21 |
| **Carry-forward boxes** | **20** |
| **Depends on** | template 03 (§5) for card-level metrics, and template 01 for truthful data |
| **Defect files it may complete** | `defects/High-NewRequest-QueueAndWaitTimeIsHardcoded.md` — the *optional* follow-on: a real queue count and wait estimate |
| **Validation** | role-access tests, filter-composition tests, preference round-trip, print output; build clean; suite `Failed: 0` |

---

## 1. Why this spec exists

Everything the floor does is recorded — requests, statuses, transitions, cancellations, notes, times —
and none of it is visible as a whole. A Plant Manager cannot answer "how many requests, how fast are
they filled, how many were late, who is carrying the load, and did we run short of material when the
request was raised". This workstream builds that view, the stock-shortage snapshot taken at request
creation, and the supervisor-level segmentation on top.

## 2. Input to paste into `/speckit.specify`

```text
Give MTM Waitlist a waitlist analytics capability, gated to the roles that are entitled to see it.
The application already records every request, its status changes, its cancellations and their
reasons, its notes and its timestamps, but none of it is summarised anywhere. This feature adds, first,
a quiet stock-shortage snapshot: when a coil- or part-bearing request is created, the application
records what the coil or part's on-hand quantity and location were at that moment, with no action by
the requester or the handler, so a shortage can later be attributed to the state of the floor when the
request was raised rather than guessed at. Second, it adds a Plant Manager analytics screen presenting
request volumes by type and over time, fill speed and average wait, on-time against overdue,
cancellations with their reasons, handler workload and performance, and the stock-shortage metrics
from the snapshot. Third, it adds a supervisor-level analytics view, role-gated to Production Lead and
above, segmented by the day-one user types — Material Handler, Production, Setup — as composable
panels so each user type renders its own set of key figures and tables without reshaping the page. The
supervisor view filters by user type, shift and building including a plant-wide aggregate, follows the
same building selection the rest of the application uses, and behaves predictably when the signed-in
user's role changes. Each user gets persisted analytics preferences — automatic refresh interval,
which figures and tables are visible, and default filters — read and written through stored procedures
behind a service that fails gracefully when a person has no saved preferences yet, with a five-minute
default cadence, a manual refresh, and a per-user override. Analytics can be printed to PDF through
the operating system's print dialogue, scoped to the filters that are active and excluding any section
that is hidden. Every label, filter, figure and export action is localised, no user-facing text is
hard-coded, and every database artifact ships with its rollback and its table-description update. The
screen's access rule is explicit: the listed roles see it, and every other role is denied, with the
denial enforced in the data layer as well as on the screen.
```

## 3. Carried-forward requirements (from the source; do not weaken)

**Stock-shortage snapshot (0.1)** — on creation of a part/coil-bearing request, quietly record the
coil/part's on-hand quantity and location with no handler/requester action. Artifacts named by the
source: `IStockSnapshotRecorder` in `MTM_Waitlist.Core/Contracts/Services`; the recorder, model and
service in `MTM_Waitlist.Analytics/Services` and `/Models`; the request-creation path invokes the Core
contract through DI. QA: a test that creation writes the snapshot, then the full suite.

**Plant Manager screen (0.2)** — volumes by request type and over time; fill speed / average wait;
on-time versus overdue; cancellations and reasons; handler workload/performance; the snapshot-based
stock-shortage metrics. Artifacts named: `AnalyticsViewModel` in `MTM_Waitlist.Analytics/ViewModels`;
`AnalyticsPage.xaml(.cs)` in `Module_Analytics/Views`; metric/query services in
`MTM_Waitlist.Analytics/Services`; gate `AllowedAnalyticsRoles` / `CanViewAnalytics`; registration via
`pageService.Configure<AnalyticsViewModel, AnalyticsPage>()` + `AddTransient` in
`ServiceRegistrationExtensions.cs`; `AddAnalyticsModuleServices` in the app-root
`ModuleDependencyInjectionExtensions.cs`; `<NavigationViewItem x:Uid="Shell_Analytics">` in
`ShellPage.xaml` with role visibility and a `Shell_Analytics.Content` entry in
`Strings/en-us/Resources.resw`. QA: metric-computation and role-gating tests.

**Supervisor view + preferences + export + localisation (0.3–0.6)** — prerequisite: 0.1 and 0.2
complete. Supervisor page and view model gated at rank ≥ Production Lead; role list = Production Lead,
Setup Lead, Plant Manager, IT Department, Developer, with an explicit **deny** for every other role;
segment by Material Handler / Production / Setup as composable panels; filter contract = user type,
shift, building (All / Expo Drive / Vits Drive), plant-wide aggregation, role-switch behaviour, with
`BuildingSelectionService` staying the building source of truth; per-user preferences persisted per
`database-schema-rules.instructions.md` (table under `Database/Tables/<NN_name>/` with `create.sql` +
`rollback.sql` plus the aggregate regeneration); stored procedures for user-scoped preference read and
write behind a repository/service boundary that fails gracefully when a row is absent; default
auto-refresh 5 minutes with a manual refresh and a persisted override; print-to-PDF through the OS
print path scoped to the active filters and excluding hidden sections (a direct PDF engine is
**explicitly out of scope for v1**); `.resw` keys for navigation labels, filter labels, KPI labels and
export text — no literal UI text; tests for role access, filter composition, segmentation, role-switch
behaviour, preference serialisation round-trip, refresh cadence, and that printed output reflects the
active filters and excludes hidden sections, then full suite and build green, and a summary of v1
limitations.

## 4. Explicitly out of scope — do not resurrect

- **A direct PDF generation engine** — print through the OS/browser path only (source-explicit).
- **User creation** — routed to template 06 (user management), not built here.
- **Stock-snapshot-from-sample-data and its QA box** — obsolete; replace with a test-double policy
  consistent with `specs/001` (`OPEN-WORK-NEXT-SPEC.md` §3).
- **Mock-mode analytics boxes** in the source — obsolete.
- **Chart-heavy dashboarding as a v1 requirement** — the charting strategy is an open decision below;
  settle it before writing requirements.

## 5. Defect this spec may complete

`defects/High-NewRequest-QueueAndWaitTimeIsHardcoded.md` — template 01 removes the fabricated
queue/wait card. If a real queue count and wait estimate are wanted, **this is the spec that can
substantiate them** (active requests per work center, and an estimate derived from the per-sub-type
max-allotted minutes). Either make it an explicit requirement of this spec, or record in the defect
file that the field stays removed. Do not leave the defect file ambiguous.

## 6. Verification / gates

1. Build clean (`0 Warning(s) 0 Error(s)`); full suite `Failed: 0`.
2. Metric computation tested against seeded audit/snapshot data — including the empty case and the
   plant-wide aggregate.
3. Role gating tested for every role in and out of the lists, including the explicit denial path.
4. Preference round-trip tested; absent-row behaviour tested (no exception, defaults returned).
5. Print output verified to honour active filters and omit hidden sections.
6. Any new schema: `create.sql` + `rollback.sql` + the `update_table_descriptions.sql` update in the
   same change (`database-schema-rules.instructions.md`), regenerated into `AllTables.sql` /
   `AllSPs.sql` / `AllSeeds.sql` as applicable.

## 7. Open decisions to resolve first — the source's five blockers

1. **App user GUID → name mapping** — how a person is displayed when analytics group by user.
2. **Charting strategy** — which charting approach (if any) is in v1.
3. **Building source of truth** — keep `BuildingSelectionService` as the only source; confirm.
4. **Access-enforcement depth** — screen only, or screen plus data layer (the source implies both).
5. **User creation is excluded** here and routed to template 06 — confirm.

*(These are blockers, not checkboxes: resolve them before `/speckit.specify`, then run
`/speckit.clarify` with this list as the agenda.)*
