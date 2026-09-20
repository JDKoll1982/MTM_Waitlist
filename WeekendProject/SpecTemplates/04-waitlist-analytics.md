# Spec seed 04 — Waitlist analytics

| Field | Value |
| --- | --- |
| **Suggested feature name** | `waitlist-analytics` |
| **Source** | `WeekendProject/OPEN-WORK-NEXT-SPEC.md` §6 — from `PromptFiles/10-0%-Phase3-analytics.md` (**20 carry-forward**) + `prompt.md` Tasks 20–21 |
| **Carry-forward boxes** | **20** |
| **Depends on** | template 03 (§5) for card-level metrics, and template 01 for truthful data. **Sequenced after template 06** by owner decision 2026-09-20 (§8 decision 8), because this feature's role gates are permission entries once 06 builds the matrix — writing them as literals first would mean writing them twice |
| **Defect file it completes** | `defects/Closed-Partial-High-NewRequest-QueueAndWaitTimeIsHardcoded.md` — completed by this feature: the Confirm screen's real queue count and wait estimate |
| **Validation** | role-access tests, filter-composition tests, per-lead data scoping, preference round-trip, print output; build clean; suite `Failed: 0` |

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
from the snapshot. Third, it adds a supervisor-level analytics view, opened by the leads and by Plant Manager and above. The
view is one screen with one tab per group of people — Material Handler, Production, Setup — and a lead sees
only the tab for their own group, while Plant Manager and above see all of them. A lead's figures cover only
their own people, and that scoping is applied where the data is read, not only where the screen is drawn, so
a lead never receives another group's rows. The view filters by user type, shift and building including a
plant-wide aggregate, follows the same building selection the rest of the application uses, and behaves
predictably when the signed-in user's role changes. Every figure on either screen is drawn as a graph, and
every graph carries one button that opens a window showing what the figure was worked out from and the rows
behind it, written in the reader's own words rather than database terms. Each user gets persisted analytics
preferences — which figures and tables are visible, and default filters — read and written through stored
procedures behind a service that fails gracefully when a person has no saved preferences yet. The screen
does not refresh itself on a timer: the figures are read when the screen opens and when the person asks for
them, and the screen always says when they were last read, while changing a filter does re-read. Analytics
prints through the operating system's print dialogue in two ways: a print of the whole open view, scoped to
the active tab and the filters that are active and excluding any section that is hidden, and a print of one
figure together with the rows behind it. Every label, filter, figure and export action is localised, no
user-facing text is hard-coded, and every database artifact ships with its rollback and its table-description
update. The screen's access rule is explicit: the listed roles see it, and every other role is denied, with
the denial enforced in the data layer as well as on the screen.
```

## 3. Carried-forward requirements (from the source; do not weaken)

**Stock-shortage snapshot (0.1)** — on creation of a part/coil-bearing request, quietly record the
coil/part's on-hand quantity and location with no handler/requester action. Artifacts named by the
source: `IStockSnapshotRecorder` in `MTM_Waitlist.Core/Contracts/Services`; the recorder, model and
service in `MTM_Waitlist.Analytics/Services` and `/Models`; the request-creation path invokes the Core
contract through DI. QA: a test that creation writes the snapshot, then the full suite.

**Plant Manager screen (0.2)** — volumes by request type and over time; fill speed / average wait;
on-time versus overdue; cancellations and reasons; handler workload/performance; the snapshot-based
stock-shortage metrics. **This is the same screen that 0.3–0.6 describes — one page, one view model, with
these figures distributed across its tabs (decisions 4 and 14). The source's "Plant Manager screen" and
"supervisor view" are one screen, not two.** Artifacts named: `AnalyticsViewModel` in `MTM_Waitlist.Analytics/ViewModels`;
`AnalyticsPage.xaml(.cs)` in `Module_Analytics/Views`; metric/query services in
`MTM_Waitlist.Analytics/Services`; gate `CanViewAnalytics`, **resolved from template 06's permission matrix** (shipped default reproduces
`AllowedAnalyticsRoles`); registration via
`pageService.Configure<AnalyticsViewModel, AnalyticsPage>()` + `AddTransient` in
`ServiceRegistrationExtensions.cs`; `AddAnalyticsModuleServices` in the app-root
`ModuleDependencyInjectionExtensions.cs`; `<NavigationViewItem x:Uid="Shell_Analytics">` in
`ShellPage.xaml` with role visibility and a `Shell_Analytics.Content` entry in
`Strings/en-us/Resources.resw`. QA: metric-computation and role-gating tests.

**Supervisor view + preferences + export + localisation (0.3–0.6)** — prerequisite: 0.1 and 0.2
complete. The same page and view model as 0.2, reached through a named permission entry in **template 06's
permission matrix**, whose shipped default is the roles entitled to see it, with every other role explicitly
denied. The view is **one tab per group** — Material Handler, Production, Setup — and a lead is given the tab
for their own group only, while Plant Manager and above are given all of them. Each lead's figures are
**scoped to their own people where the data is read**, not only where the screen is drawn. Filter contract =
user type, shift, building (All / Expo Drive / Vits Drive), plant-wide aggregation, role-switch behaviour,
with `BuildingSelectionService` staying the building source of truth; per-user preferences persisted per
`database-schema-rules.instructions.md` (table under `Database/Tables/<NN_name>/` with `create.sql` +
`rollback.sql` plus the aggregate regeneration); stored procedures for user-scoped preference read and
write behind a repository/service boundary that fails gracefully when a row is absent; **no automatic
refresh** — the figures are read when the screen opens and when the person asks, a filter change re-reads,
and the screen always reports the time of its last read, so the preference row carries no refresh-interval
column; print through the OS print path in two forms — the whole open view scoped to the active tab and the
active filters and excluding hidden sections, and one figure together with its own rows from that figure's
context menu (a direct PDF engine is **explicitly out of scope for v1**); `.resw` keys for navigation
labels, filter labels, KPI labels and export text — no literal UI text; tests for role access, filter
composition, per-lead data scoping, segmentation, role-switch behaviour, preference serialisation
round-trip, read-on-demand behaviour and the reported time of the last read, and that printed output
reflects the active filters and excludes hidden sections, then full suite and build green, and a summary of
v1 limitations.

## 4. Explicitly out of scope — do not resurrect

- **A direct PDF generation engine** — print through the OS/browser path only (source-explicit).
- **User creation** — routed to template 06 (user management), not built here.
- **Stock-snapshot-from-sample-data and its QA box** — obsolete; replace with a test-double policy
  consistent with `specs/001` (`OPEN-WORK-NEXT-SPEC.md` §3).
- **Mock-mode analytics boxes** in the source — obsolete.
- **Dashboarding beyond the figures §3 names** — no free-form chart builder, no ad-hoc pivot explorer, no
  user-defined metric designer, and no figures added to the page simply because a graph was easy to draw.
  The figures §3 names **are** drawn as graphs, with LiveCharts2, and each one is openable to the rows
  behind it. That is the whole of the charting scope.

## 5. Defect this spec completes

`defects/Closed-Partial-High-NewRequest-QueueAndWaitTimeIsHardcoded.md` — **this spec substantiates it and
completes the defect.** The Confirm screen carries **both**: the number of requests already waiting at that
work centre, and a waiting-time estimate worked out from the allowed minutes each request type carries. The
estimate MUST be labelled as an estimate and MUST say what it is worked out from — the requester is being shown
a prediction, so the basis travels with it. The defect file moves from *partial* to **closed** once this
feature ships, with this spec named as the spec that completed it.

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

## 7. Open decisions to resolve before `/speckit.specify`

1. **Which figures sit on each tab.** Decision 14 fixed the tab structure — one tab per group, a lead seeing
   only their own group's, Plant Manager and above seeing all of them — but not what each tab contains.
   §3's figure list is written for the analytics screen as a whole, so it has to be split per group before
   `/speckit.specify`, or the split gets invented during implementation.

*(The source's five blockers are all settled — see §8. Run `/speckit.clarify` with this list as the agenda.)*

---

## 8. Brainstorm log

### 2026-09-20 — session 1: what this spec covers

**Target confirmed.** This template is the next feature in the documented run order — `00-INDEX.md` gives
`01` → `02` → `03` → `04`, and `01`, `02`, `03` have shipped as `specs/002`, `specs/003` and `specs/004`
respectively. The next feature number is **`006`**: `005` is consumed by the abandoned
`specs/005-setup-work-order-search` run, whose own marker file records that the next `/speckit.specify`
takes `006`.

**Scope decided (owner, 2026-09-20): the whole workstream goes into one spec.** All four sub-capabilities
are in v1 — the stock-shortage snapshot (0.1), the Plant Manager screen (0.2), the supervisor view with
its filters (0.3–0.5), and preferences / reading the figures on demand / print / localisation (0.6).

**Consequences to carry into the requirements, not to rediscover later.**

1. The requirement set is large, so **every requirement needs its own verification path** — the one
   capability that grew in review is the one that ships unverified.
2. **(0.1) is the only part that needs new schema**, and the only part that changes a path that already
   works: request creation gains a snapshot hook through a Core contract. It is therefore the highest-risk
   piece and the one to design first, even though (0.2) is the visible one.
3. **(0.2)–(0.6) are reads** over what (0.1) plus the existing audit trail already store. They add no
   tables except the per-user preference row that (0.6) needs.
4. Because all four ship together, the spec must say plainly **which capability is incomplete while the
   feature is mid-build**, so a partial build is reviewable rather than appearing finished.

### 2026-09-20 — decision 2: how a person is attributed to a user type

**Decided (owner, 2026-09-20): record each person's role on the request and on every audit event, at the
moment it happens.** A role change must never re-attribute a person's history, or the figures a manager read
last month would change under them.

**What this requires.**
- A role snapshot on `waitlist_requests_queue` (the requester's role when the request was raised) and on the
  audit table (the actor's role for each event). Both are schema changes, so both ship paired
  `create.sql` / `rollback.sql`, the `update_table_descriptions.sql` update and the aggregate regeneration in
  the same change (constitution III).
- The role is captured **at write time**, so the write path needs the actor's role then, not a later lookup.
- Record the **`role_code`**, never the display name. `auth_roles_catalog` carries both, and keying on the name
  forks one person's history the first time a role is renamed.

**Grouping by person needs no identifier mapping.** `core_users_profiles` already carries `display_name`
alongside `employee_identifier`, and the request row and the audit rows already store an employee number, so
the join is available directly.

**Edge cases this decision creates. Each needs a stated answer in the spec.**

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| A person holding **several roles at once** | `auth_roles_assignments` is `UNIQUE (user_id, role_id)` with no end date, so several live rows are normal | One deterministic rule for the snapshot — most recently assigned, mirroring `sp_auth_user_row_get` — or a multi-role person is counted in two panels at once |
| A role changed since the request was raised | Changing a role **deletes** the old assignment, so history cannot be rebuilt afterwards | This is precisely why the snapshot exists. It is correct from the change onward and unknown before it |
| **Everything raised before the feature ships** | The columns do not exist yet, and old rows cannot be back-filled | An explicit, **visible** unattributed bucket. Never dropped silently, never guessed at, and the screen must say the figures exclude it |
| An **unjoinable** person | `core_users_profiles.employee_identifier` is nullable | The same unattributed bucket, for the same reason |
| A **renamed** role | `role_code` is stable; `role_name` is not | Key on `role_code`; a rename must not fork a person's history |
| A request with **no handler yet** | The handler columns stay empty until someone accepts | Workload panels count only events that happened, so an unaccepted request carries no handler figure rather than a zero attributed to nobody |

### 2026-09-20 — decision 9: figures are graphs, and every graph can be opened to show the rows behind it

**Decided (owner, 2026-09-20).** The screen presents **proper graphs**, not just tables of numbers. And
**every graph carries a button that opens a modal window showing the data it read to produce that graph.**

**The charting answer was checked rather than assumed.**

- **WinUI 3 ships no chart control.** The Microsoft Learn control index lists nothing higher-level than
  *Shapes* (ellipses, rectangles, polygons) under "Media, graphics, and shapes", so a graph is either
  hand-drawn or comes from a library. Hand-drawing every axis, label and series is real work with real
  accessibility gaps; do not let the spec imply the platform provides it.
- **LiveCharts2 is the library**, verified against its own documentation: the WinUI package is
  `LiveChartsCore.SkiaSharpView.WinUI`, and it is hosted as a `CartesianChart` driven by a `Series` array or a
  `SeriesSource` + `SeriesTemplate` pair. Its one stated constraint — SkiaSharp 3 publishes no `netX.0-windows`
  build, so a Windows-targeted TFM is required — is already satisfied by this repo's
  `net10.0-windows10.0.19041.0`, so no retarget is needed.
- **This is a `plan.md` decision, not a requirement.** The spec says figures are graphs and are traceable to
  their rows; which library draws them is design, and the constitution keeps technology out of `spec.md`. The
  verification above is recorded here so `/speckit.plan` inherits it instead of re-deriving it.

**The new requirement, and why it is a good one.** "Show me the data behind this figure" is the repo's own
truthfulness rule turned on the feature itself: `specs/001` FR-001/FR-003/FR-014 and `specs/002` removed
values nobody could substantiate. An analytics screen is the one place where a wrong figure is *invisible* —
it looks exactly like a right one. Making every figure openable to its rows is what keeps that from happening.

**Edge cases this requirement creates. Each needs a stated answer in the spec.**

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| The modal must show **the rows the graph was drawn from**, not a fresh query | The screen refreshes on a cadence, so a re-read can show a *different* set from the one behind the picture the user is looking at | Serve the captured rows the figure was computed from, or state on the modal that it re-read and when. A figure and its evidence must never disagree |
| An **aggregated** figure has no single row | "Average wait: 42 min" is computed *from* rows; the rows are not the figure | The modal shows the contributing rows **and** the computation that turned them into the figure — the count, and the rule. Without the rule, a wrong average looks correct |
| A figure with **thousands** of contributing rows | A year of requests is not a list anyone reads, and binding it all will freeze the UI | Page or cap the list, say on the modal how many of how many are shown, and never silently truncate |
| The **active filters** are what make the figure mean anything | The same figure differs by user type, shift and building | The modal repeats the filters that produced it, so a number lifted out of the screen still carries its meaning |
| An **excluded** row | Unattributed requests, hidden sections, and pre-change rows are excluded from figures | Exclusions appear on the modal as excluded, with the reason. A row that is missing from a total must not be missing from the evidence |
| The modal is not a page | It is opened from a figure, and the app's print path already excludes hidden sections | State whether the data appendix is part of a printed report or screen-only, and localise the modal's own strings and column headings like every other surface (constitution V) |

### 2026-09-20 — decision 10: what the window shows, and who the words are for

**Decided (owner, 2026-09-20).** Opening a figure shows **both**: what was worked out and from how many
rows, then the rows themselves, paged, with the count always visible. Plus a standing constraint on the whole
screen — **keep the UI clean, and write every word for the person using the app, never for a developer.**

**Why the wording constraint is a requirement and not a style note.** This screen is the one place in the
application where the reader is a manager making a decision, and it is the easiest place for developer
vocabulary to leak: a heading that reads `duration_minutes`, a note that says `AVG over 312 rows`, a filter
labelled `p95 = 48`. Every one of those is a number the reader cannot act on. The repo already forbids the
equivalent elsewhere — the confirm step says *"This will raise 2 requests — one for each die you chose."*
rather than exposing a resource key (`specs/004` T233), and `specs/002` removed values presented without a
substance behind them. Constitution VII makes the same demand of the chat channel; this is its UI half.

**Edge cases this pair of decisions creates.**

| Edge case | The answer the spec must state |
| --- | --- |
| Stating the arithmetic in words | *"Average wait — 42 minutes, worked out from 312 requests"*, never `AVG(duration)` or a formula the reader has to decode |
| Column headings in the row list | The reader's words — *"Waited", "Requested by", "Work centre"* — and never a database column name |
| The count when rows are paged | Always visible and never a lie: *"showing 50 of 312"*, not a list that quietly stops |
| The exclusion note | Plain and specific: *"312 of 318 are counted. 6 are not, because no role is recorded for them yet."* A bare "excluded: 6" tells the reader nothing they can act on |
| Internal identifiers | Request ids, user ids and role codes are **not** shown on this screen. They are plumbing, and a manager cannot use them |
| Times | Local time in the reader's format, not the UTC ISO form the logs use |
| Clutter | The "show me the data" affordance is **one** consistent control on every figure, not a different button in a different place each time, and the modal opens on the figure rather than replacing the screen |
| What the reader is told if the figure is empty | An empty graph says why it is empty — no data in the period, or everything filtered out — rather than drawing an axis with nothing on it |

### 2026-09-20 — decision 11: the snapshot records the figure, not where the figure came from

**Decided (owner, 2026-09-20): the shortage snapshot stores the on-hand figure and the location, and does
not record whether that figure was read live or served from the cached copy.**

**What the decision buys.** One clean number per request. The shortage figures stay a single honest total
instead of splitting into two populations a reader has to add up, and the snapshot write stays a single
insert with no source column, no enum, and no extra branch on the request-creation path.

**What it costs, stated once so it is not rediscovered as a bug.** The app's own rule is that an Infor
Visual read falls back to the warm cache when the source is unreachable, and that the shell surfaces that
with a cached-data indicator plus an age (`specs/001` FR-025). On this screen that indicator **will not
help**: analytics reads the local store, not Infor Visual, so nothing on the page will be lit when a
stored snapshot came from the cache. A shortage that exists only because the app could not reach Infor Visual
at that moment is therefore indistinguishable, in the report, from a real shortage on the floor. That is
accepted. Do not close this later as a defect without reopening this decision.

**Edge cases this decision leaves standing. Each still needs a stated answer in the spec.**

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| A part with **no stock record at all** | Not every part carries an on-hand figure, and the read can return nothing | Record **unknown**, never `0`. A zero is a claim — *"we had none"* — and the app cannot support it |
| A request with **no snapshot row** at all | The snapshot is best-effort and must never block raising a request | A request without a snapshot reads as *"no shortage data"*, and the report says how many such requests it holds. It must never be counted as *"no shortage"* |
| The snapshot write **fails** | The request is the thing the operator needs; the snapshot is not | The request is raised and the failure is recorded. A failed snapshot must never fail, delay, or block the request |
| A **cancelled** request whose snapshot was taken | The snapshot is written when the request is raised, before any outcome | Decide whether cancelled requests count in the shortage figures, and say so on the screen rather than in a footnote |

### 2026-09-20 — decision 12: the data is printable, but separately from the charts

**Decided (owner, 2026-09-20).** The data behind a figure prints **separately from the charts**, never
interleaved with them, and the control is a button on the figure's own pop-up rather than a switch in the
print dialogue. So the report itself never carries data, and the evidence is something the person asks for
from the figure they doubt.

**What this produces: two print outputs, not one.**

1. **The report** — the graphs, each carrying the same plain-language line its on-screen pop-up opens with
   (*what was worked out, and from how many requests*). This is the document that goes to a meeting.
2. **The data printout** — every row behind **one** figure, generated only when the person asks for it from
   that figure's pop-up. This is the document that answers a challenge. **It is not stapled to the report**:
   the report stays readable, and the evidence is a separate thing a person fetches deliberately.

**Edge cases this decision creates. Each needs a stated answer in the spec.**

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| The data printout **loses its report** | It prints separately, so it will be handed round on its own | It carries its **own** header: the period, the filters that were active, which figures it covers, and how many rows it holds. Loose pages with no header are unreviewable |
| Screen caps, paper does not | Decision 10 caps the on-screen list and says so. A printed data set is asked for *because* it should be complete | The on-screen list stays capped; the **printed** one is complete, and states its row count. A cap that silently applies to paper would defeat the entire option |
| The option is on and the data is huge | A year of requests is a great many rows | A long printout is expected and acceptable here — that is what the option is for — but the **pop-up** states the row count, and with it roughly how long the printout will be, before the button is pressed |
| Filters and hidden sections | The print rule already scopes the report to the active filters and excludes hidden sections | The **same rule applies to both** outputs. Data behind a hidden figure is not printed, and the data printout names the filters that shrank it |
| Whether the choice is remembered | Printing one figure's data is a one-off action taken on that figure's pop-up, not a setting | **Nothing is added to the preferences table.** One print, one action; not a setting |
| Nothing to print | A figure can legitimately have no rows in the period | The data printout says the figure had no rows rather than emitting an empty page with a heading on it |

### 2026-09-20 — decision 13: the data print belongs to the figure, not to the report

**Decided (owner, 2026-09-20): the data printout covers only the one figure the person had open, and the
control lives on that figure's own pop-up.** There is no report-level data appendix.

**Why this fits what came before it.** Decision 10 already puts the arithmetic and the rows on the figure's
pop-up, because that is where a person lands when they doubt a number. Decision 13 puts the print control in
the same place, so the doubt and its evidence and the paper all happen without the person leaving the figure
they are asking about. Nothing is added to the main screen, which is what "keep the UI clean" asked for.

**Consequences to carry into the requirements.**

- **The report is charts and basis lines, and nothing else.** There is no report-level data appendix, so the
  printed report cannot grow into a stack of tables no matter what the figures were computed over.
- **Printing the data for the whole report is done figure by figure.** A person wanting every number on paper
  opens each figure in turn. Do not add a report-level switch without reopening this decision.
- **The pop-up is now the evidence surface in three ways** — it shows the arithmetic, it shows the rows, and
  it prints them. Those three must stay consistent with each other, which is why the row set is captured when
  the figure is produced (decision 10) rather than re-read at print time.
- **The printed data page still carries its own header** — the period, the active filters, which figure it
  belongs to, and the row count. It is handed round separately from the report (decision 12), so it must be
  self-describing.

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| Two figures, one doubt | A person may want to question two figures in a row | Each printout names its own figure, so two sheets from two figures are never mistaken for one set. No bundling is attempted |
| The pop-up is open and the figures are read again behind it | The screen can be refreshed while a figure's window is open, and the window holds a captured row set | Printing what the window shows — **the rows it was opened with** — is correct, and its header states the period and filters it was built from, so the sheet stays true after the screen reads again |
| A figure with a very large row set | This is the case the option exists for | The printout is complete and states its row count. The on-screen list remains capped and says so (decision 10) |
| No rows, or a failed print | A figure can be empty; the OS print path can also be cancelled or fail | An empty figure prints a page saying it had no rows. A failed or cancelled print reports plainly and leaves the pop-up open — it must not close the window and lose the person's place |

### 2026-09-20 — decision 14: one tab per group, not stacked panels

**Decided (owner, 2026-09-20): the three groups are a tabbed screen — one tab per lead type.** A lead is
given the tab for their own group only; Plant Manager and above are given all of them.

**Why tabs are better here than panels, beyond preference.** Stacked panels on one tall page mean a lead
scrolls past two groups that are nearly empty for them to reach their own, and the page's length depends on
the visitor — which is exactly the "reshaping the page per visitor" the original avoided. A tab gives each
visitor one region that is fully theirs, at a stable length, and it turns "which figures belong to whom"
into a visible structure instead of an ordering someone has to remember.

**It also resolves an earlier edge case for free.** Decision 2 tables the person who holds *several* roles at
once. With tabs, such a person simply gets more than one tab; no rule is needed to pick between them, and no
figure is hidden from them.

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| A tab that is not selected is now literally **hidden** | §3's print rule scopes output to the active filters and **excludes hidden sections**, and tabs make "hidden" unambiguous | The screen print covers **only the open tab**, and its header names that group (decision 16) |
| Which tab the screen **opens** on | A lead has one tab; a Plant Manager has three | The screen opens on the visitor's own group's tab when they have one, and on the first tab when they have all of them. No tab choice is stored, so this needs no new preference column |
| A **visitor holding two lead roles** (decision 2's multi-role case) | `auth_roles_assignments` allows several live roles | They receive both tabs in catalogue order, and the screen opens on the same deterministic one the role snapshot rule picks (decision 2) so two visits do not open differently |
| A group with **nobody in it** | A group can have no staff, or none who raised requests in the period | The tab still exists and says why it is empty — no staff in the group, or no activity in the period. It must not be hidden, or the visitor concludes the group was fine |
| Tabs and the **screen's own filters** | Filters (shift, building, period) sit above the tabs and apply to whichever tab is open | One filter bar above the tabs, shared by all of them, so switching tabs never silently changes the question being asked |
| Keyboard and screen-reader use | Tabs are a navigation structure, not decoration | The tab strip uses the platform's tab semantics and every tab is reachable and nameable without a mouse (constitution V) |

### 2026-09-20 — decision 15: printing starts from a graph, by right-click

**Decided (owner, 2026-09-20): a figure's data is printed from a right-click menu on that figure**, so the
graph and the data both derive from the figure the menu was opened on. That removes any doubt about which
group, period or filter the paper is talking about, and the row set is still captured per figure rather than
re-read at print time. The screen-level print (decision 16) is a separate document with its own scope.

**Grounded in Microsoft Learn** (fetched from the Learn pages directly; record the source, per
`.github/instructions/mcp-doc-research.instructions.md`):

- **This is the platform's documented pattern.** "Menus and context menus" states that a context menu is
  *attached to a single element*, *displays secondary commands*, and is *invoked by right clicking (or an
  equivalent action, such as pressing and holding with your finger)*, associated through the element's
  `ContextFlyout` property. **Touch is therefore covered for free** — press-and-hold is the documented
  equivalent, not a workaround.
- **Learn prefers `CommandBarFlyout` over `MenuFlyout`** for a context menu: it "provides more functionality
  than MenuFlyout and, if desired, can achieve the same behavior and look of MenuFlyout by using only
  secondary commands". Use that unless there is a reason not to.
- **Keyboard is the part that is not free.** The "Keyboard accessibility" page's rule is that *any UI that can
  be activated by pointer should also be invokable by keyboard*, that only `Control`-derived elements support
  focus and tab navigation, and that an element which is not focus-capable by default must either be made a
  focusable control or be given `IsTabStop` plus a **visible focus indicator**. **A chart is not focusable by
  default.** So a print reachable only by right-click would be a genuine accessibility regression, and it is
  the one thing decision 15 must not become.

**How this is satisfied without weakening the decision.** Every figure already carries a visible button that
opens the figure's window with its rows (decision 10). That button is the **keyboard and touch path** to the
same figure and the same rows; the right-click flyout is the **pointer shortcut** alongside it. So:

- the flyout and the button must act on the **same captured row set** for that figure, or the two paths show
  different numbers;
- the flyout is never the *only* way to do anything — every command it offers is reachable without a right-click;
- the chart element itself is made focusable (`IsTabStop` plus a visible focus indicator) so the flyout is
  also reachable with the keyboard while the graph has focus;
- the flyout's items carry plain end-user labels and are localised, and so does the button (decision 10).

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| Print and the button disagree | Two entry points onto one figure | Both read the same captured row set. This is the failure mode the two-path design introduces, and it is the one to test directly |
| A graph with no rows | Legitimate — an empty period | The flyout still opens and says the graph has nothing to print, rather than the print silently producing a blank page |
| The right-click lands on the plot area vs the card | The chart is a composite of frame, axes and series | One `ContextFlyout` on the figure's container, so where inside the graph the click lands does not change what happens |
| Print fails or is cancelled | The OS print path can fail | Reported plainly; the screen and the figure stay as they were. A cancelled print must not lose the figure's captured rows |
| Accessibility is only claimed, never checked | This is easy to assert and easy to be wrong about | A keyboard-only pass and a screen-reader pass over one figure are part of the feature's verification, not a follow-up task |

### 2026-09-20 — decision 16: two print paths, and each says which one it is

**Decided (owner, 2026-09-20): there are two print paths.** A print on the screen covers the **whole open
view** — the tab that is open, scoped to the active filters, excluding hidden sections — and a right-click on a
graph covers **that graph and its own rows** (decision 15).

**Why both are needed.** The two needs are genuinely different and neither covers the other: a person going
into a meeting wants the whole picture on one set of sheets, and a person being challenged on one number wants
that number's evidence. This also keeps §3's carried-forward print requirement intact.

**The one hazard this creates is confusion, because there are now two documents.**

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| The reader cannot tell which printout they are holding | Two paths, two scopes, and both end up as paper | The two are **visibly different documents**. The screen print names the group (the tab) and the filters it covers; the graph print names the figure and its row count. Neither is a banner-free table of numbers |
| **A screen print on a manager's screen covers one of three tabs** | Decision 14 gives a manager three tabs; §3 says hidden sections are excluded from print | The screen print covers **only the open tab**, and the header names that group without relying on the reader remembering which tab was selected |
| A figure that is empty | Legitimate for the period | Empty figures still print, with their *"no rows in this period"* note, so the report shows the figure exists and is empty rather than omitting it and looking incomplete |
| The screen print while a graph's window is open | The two paths can be used in either order | Orphan handling is explicit: the window is not part of the screen print, and printing from the screen does not depend on what is open on top of it |
| Each document must stand on its own | Both get separated from the screen they came from | Both carry the period and the active filters; the graph print additionally carries the figure's own name and row count. A sheet that cannot be understood alone is not evidence |

### 2026-09-20 — decision 17: no automatic refresh; the figures are read when they are asked for

**Decided (owner, 2026-09-20): the screen does not refresh itself.** The figures are read when the screen
opens and when the person asks for them, and that is all.

**What this means for the stored preferences.** There is **no automatic-refresh-interval preference**: the
preference row carries which figures and tables are visible and the default filters, and no refresh column.

**The consequence, stated plainly so it is not discovered as a bug.** A screen with no timer can sit for hours
showing figures that are no longer true, and it will look exactly as current as a fresh one — that is the
failure analytics screens are most often criticised for. The mitigation is therefore not optional and is part
of this decision, not a nicety: **the screen always reports the time its figures were last read**, next to the
refresh control, so a stale number can never masquerade as a current one. This mirrors what the shell already
does for the Infor Visual cache (`specs/001` FR-025: an age, not a vague "may be out of date").

**One clarification, because reading this decision too literally would be worse than ignoring it.**
"Refresh only when the person asks" governs **time-based re-reading**. It does **not** mean filters stop
working. Applying or changing a filter, a period or a building is the person asking a different question, and
the figures **must** re-read for it. A build that skipped that would show filtered labels sitting over
unfiltered numbers — the single most damaging outcome this screen could have, because the numbers themselves
would look perfectly plausible.

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| Switching tabs | Decision 14 gives the screen several tabs | Switching a tab does **not** re-read. There is **one** as-of time for the whole screen, so two tabs can never disagree about when their numbers are from |
| Changing a filter | Distinct from a timer, and the distinction is the clarification above | A filter change **does** re-read. Say so explicitly in the requirement so it cannot be read as "only the button re-reads" |
| The refresh button vs. the cache-refresh permission | Decision 6 gates *pulling an Infor Visual cache refresh* to specific roles. This button is a different thing entirely: it re-reads the local store | Re-reading the analytics figures needs no extra permission — anyone who can see the screen can refresh it. Conflating the two would lock a Plant Manager out of their own screen |
| A refresh that fails or is slow | The store can be unreachable, like any other read | The screen keeps showing the figures it already has and says the refresh failed, leaving the as-of time honest. It must **not** blank the figures to an error state and lose what the person was reading (`specs/001` FR-021's per-screen unavailable state, with a manual retry) |
| The as-of time itself | The whole decision rests on it | One time, shown as local time in the reader's format, updated by every read that succeeds and left untouched by one that fails |

### 2026-09-20 — decision 3: the Confirm screen's waiting line

**Decided (owner, 2026-09-20): the Confirm screen shows both — how many requests are already waiting at that
work centre, and a waiting-time estimate.**

**Why the two halves are held to different standards.** The count is a fact the queue already holds. The
estimate is a prediction about the future, so it is only honest if the screen says it is an estimate and says
what it came from. That is the whole difference between this figure and the fabricated `"approximately 15
minutes"` the defect was raised about: that one had no basis and said nothing about being a guess.

**Edge cases this decides, each of which needs a stated answer in the spec.**

| Edge case | Why it happens | The answer the spec must state |
| --- | --- | --- |
| A request type carrying **no allowed minutes** | The estimate is derived from that figure, so with none there is nothing to derive from | **No estimate is shown at all** — never a default, never `0 minutes`. The count still appears, because the count needs no basis |
| A **brand-new work centre** with nothing waiting | The count is genuinely zero, which is a fact | Say so in plain words rather than hiding the line; an empty line reads as broken, and `0` alone reads as a fault |
| The figure **ages while the screen is open** | The Confirm screen is static; the queue moves on | State the moment the count was taken, so a requester does not read a stale number as current |
| The estimate **turns out wrong** | It is derived from allowed times, not from how long anything actually took | The wording must not promise a time. It is an estimate, said so, with its basis beside it |
| The work centre **has no history yet** | Nothing has been through it, so nothing can be averaged | The estimate must not be inferred from other work centres. Same rule as no allowed minutes: show nothing |

### 2026-09-20 — decision 4: the analytics screen is per-lead, scoped to that lead's own people

**Decided (owner, 2026-09-20).** The screen is not one view behind one gate. Each lead opens the same screen
and sees **only their own people's** figures:

| Who opens it | What they see |
| --- | --- |
| Production Lead | Operators (the `production` role) |
| Material Handler Lead | Material Handlers |
| Setup Lead | Setup Techs (the `setup` role) |
| Production Manager, Developer, Admin | Everyone |

**Consequences that belong in the requirements.**

- **Content is identical; only the rows change.** So the requirement is "scope the data to the viewer's
  people", not "build a second screen". One screen, one view model, a scope resolved from the viewer's role.
- **The scope is enforced where the data is read, not only where the screen is drawn.** A lead whose rows are
  filtered by the screen alone still receives everyone's numbers in the payload it was handed. **Both are
  required.**
- **Scoping is by role, not by department.** The app holds no department or reporting-line field, so "my
  people" can only mean "people holding the subordinate role" (`production`, `material_handler`, `setup`).

**The catalogue holds eight roles, and the names above do not all match it.**

| Name used | In `auth_roles_catalog`? | Reality |
| --- | --- | --- |
| Production Lead | yes | `production_lead` |
| Setup Lead | yes | `setup_lead` |
| Developer / Admin | yes | `developer` / `admin` |
| Operators / Setup Techs / Material Handlers | **as people, not as titles** | The titles are `production` ("Production"), `setup` ("Setup"), `material_handler` ("Material Handler"). Operators are the *people* in the Production role |
| **Material Handler Lead** | **no** | Nothing in the catalogue, and nothing planned that adds one — see decision 5 |
| **Production Manager** | **no** | Closest is `plant_manager` ("Plant Manager"), which already sees everything. **Treated as the Plant Manager** |
| IT Department (named by the source, §3) | **no** | Template 06 creates it by *replacing* `Admin`. This spec must gate on the role that exists today |

### 2026-09-20 — decision 5: Material Handler Lead becomes a real job title

**Decided (owner, 2026-09-20): add Material Handler Lead as a real role; Production Manager is the Plant
Manager.** The two were bundled in one choice, so the second half is decided as `Production Manager = Plant
Manager` and needs no new catalogue row.

**What adding a role actually costs, measured rather than assumed.** Seven shipping code sites each hold their
own written-out list of role names, so a title that is missing from one is an outsider there:

| Site | The list it holds |
| --- | --- |
| `MTM_Waitlist.Core/Services/RequestActionPolicy.cs` | `HandlerRoles` — who may accept/complete/release at all |
| `MTM_Waitlist.Mock.Service/Api/ServiceOperatorRoles.cs` | `Approved` — who may call the cache-refresh API |
| `MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs` | four panels: ignored locations, hot work centres, image locations, cache refresh |
| `MTM_Waitlist.Settings/ViewModels/UrgencyAllotmentEditorViewModel.cs` | who may edit allotted minutes ("Plant Manager and above") |
| `MTM_Waitlist.Setup/Services/DunnageWorkflowService.cs` | who may run the dunnage workflow |
| `MTM_Waitlist.Setup/ViewModels/SetupWorkCenterViewModel.cs` | who may change work-centre setup |
| `ViewModels/ShellViewModel.cs` | the badge glyph and colour shown beside the signed-in name — a **lookup**, not a gate, so it is the one list here that does not become a permission |

**Limitations this decision carries, both of which the spec must state rather than discover later.**

1. **Nobody can be given the new title through the app.** Assigning a role is the user-management work
   (template 06); until it ships, a Material Handler Lead exists only by a database update. The screen will be
   reachable by a role that cannot be granted from inside the application.
2. **The rank table is template 06's.** `06-user-management.md` and `15-0%-UserManagement.md` already define a
   rank order and already plan one role change (`Admin` → `IT Department`). Adding a second role here means 06
   must carry both, or the two specs will disagree about the same table. Cross-template dependency to record
   in §6.

### 2026-09-20 — decision 6: what the new title may do, decided list by list

**Decided (owner, 2026-09-20): decide each of the seven sites separately.** All ten role lists were read out
of the shipping code so each decision rests on the list that actually exists. Three of the ten are settled by
a rule the code already states.

| Where | What it controls | Who is on it today | Decision |
| --- | --- | --- | --- |
| `RequestActionPolicy` `HandlerRoles` | Accept / complete / release a request | **all eight roles**, plain Material Handlers included | **Add** — a lead cannot be refused an action their own handler may take |
| `ServiceOperatorRoles` `Approved` | Pulling a refresh of the cached Infor Visual data | Admin, Developer, Plant Manager, Setup Lead, Production Lead | **Add** — a lead is a lead |
| `SettingsViewModel` `AllowedIgnoredLocationManageRoles` | Ignored locations in inventory lists | everyone above a plain Material Handler | **Set in 06's matrix** (decision 7) |
| `SettingsViewModel` `AllowedHotWorkCenterManageRoles` | Hot work centres | Admin, Developer, Plant Manager, Setup Lead, Production Lead | **Set in 06's matrix** (decision 7) |
| `SettingsViewModel` `AllowedImageLocationManageRoles` | Part pictures | Admin, Developer | **No change** — the list is deliberately two roles wide |
| `SettingsViewModel` `AllowedCacheRefreshRoles` | Cache refresh from the Settings screen | Admin, Developer, Plant Manager | **No change** — the stated rule is "Plant Manager and above" |
| `UrgencyAllotmentEditorViewModel` `AllowedUrgencyManageRoles` | Allotted minutes per item | Admin, Developer, Plant Manager | **No change** — same "Plant Manager and above" rule |
| `DunnageWorkflowService` `AllowedQuickAddRoles` | Quick Add dunnage definitions | Admin, Developer, Plant Manager, Setup Lead, Production Lead | **Set in 06's matrix** (decision 7) |
| `SetupWorkCenterViewModel` `AllowedManageRoles` | Work-centre setup | Admin, Developer, Plant Manager, Setup Lead, Production Lead, **`Setup Tech`** | **Set in 06's matrix** (decision 7) |
| `ShellViewModel` `GetUserPresentation` | The badge beside the signed-in name — **a lookup, not a gate** | only `developer`, `admin` and `material handler` are recognised, so five of the eight real roles fall through to grey | **Not a permission.** Template 06 rewrites it against the real role codes (its decision 15) |

**Findings raised while reading those lists — none of them this feature's, all of them worth routing.**

1. **`SetupWorkCenterViewModel` gates on a role called `Setup Tech`, which the catalogue does not hold.** The
   catalogue's role is `setup` / "Setup". So that entry can never match a live person: the setup *worker* the
   name appears to intend is refused by the very list that names them, and the only roles the entry does admit
   are the five lead/admin roles beside it. Either the intended behaviour is "leads only" and the name is
   stale, or a Setup Tech is silently unable to do work-centre setup. **Route to the setup workstream, not
   here.**
2. **There is no single source of truth for roles.** Ten lists across seven files each spell role names out as
   literals, and they already disagree (`Admin` vs the planned `IT Department`; `Setup` vs `Setup Tech`). Adding
   one title means touching up to ten places, and a missed one fails silently — the role is simply an outsider
   there, with no error. This is why decision 5's cost is real rather than theoretical.
3. **`ShellViewModel`'s badge map keys on a different vocabulary from every other list.** It branches on
   `developer`, `admin`/`administrator`, `supervisor`/`manager` and `quality`/`quality inspector` — and only
   `developer`, `admin` and `material handler` are role names the catalogue holds. So **five of the eight real
   roles** get the plain grey badge today, Production Lead, Setup Lead and the **Plant Manager** among them.
   It is a lookup rather than a gate, so it does not become a permission: template 06 rewrites it against the
   real role codes (its decision 15).

4. **The owner's answer on ignored locations contradicts the screen's own wording, and the change it implies
   is wider than the question asked.** Asked whether the new title should be able to change ignored locations,
   the owner answered **"Plant Manager and above"**. Read one way that is simply *why* the new title misses
   out. Read the other way it is an instruction to **narrow the list**, because the list today is four roles
   wider than that: `Production`, `Production Lead`, `Setup` and `Setup Lead` are all on it, and the Settings
   screen says so out loud —

   > "Locations omitted from inventory lists and totals (for example WC, NCM, V-WC, NCM-VITS, SHIP). Editing is
   > limited to roles above Material Handler."

   — `Module_Settings/Views/SettingsPage.xaml`. So the code, the screen text and the owner now say three
   different things, and the second reading removes a capability four existing roles hold today. **Do not
   guess this one:** a narrowing is a real behaviour change that also has to update the screen text, the
   settings documentation and any test that asserts the current membership.

   **Settled as a matrix default — see decision 7.** The per-role answer, and any change to the screen text,
   are set in template 06.

### 2026-09-20 — decision 7: gates become settings, and the whole seven-site question dissolves

**The owner's answer, verbatim:**

> *"Lets simplfy this, create a new Settings Page, openable by the setting panel that allows Plant Manager
> and above to set what each role can do in the applicaiton, that way each gate can be named via a settings
> key and if the current user's key is false then they cant do that gated action"*

**What this does to decision 6.** The seven sites stop being seven decisions about one new title and become
**one decision about the shipped defaults**. "May a Material Handler Lead change ignored locations?" becomes
"what does *change ignored locations* default to for `material_handler_lead`?" — answerable later on a screen
without a code change. Decision 6's remaining open rows are therefore **parked, not lost**: they are the
initial values of the matrix, and the owner sets them when the matrix screen exists. The three rows already
settled stay settled — they become that screen's first three defaults.

**This already has half a home.** Template 06 (user management) is not merely adjacent; its **locked**
item 1.2 already plans the same refactor:

> "add `RoleAuthorization` — the rank table plus `IsAtLeast(role, required)` … **refactor the existing
> hard-coded `Allowed*Roles` arrays (`SettingsViewModel`, `SetupWorkstationViewModel`,
> `DunnageWorkflowService`) onto it**" — `06-user-management.md`, §3

So the owner's idea **amends a locked section**, and 06 states that its locked requirements are not to be
re-litigated. The owner must therefore say explicitly that 1.2 is being changed, or the two specs will
disagree about the same arrays.

**The store already exists — no new table is needed for the values.** `config_settings_values` (table 08) is
keyed by `setting_key` + `scope_type` + `scope_key` and already has `sp_config_settings_get_effective`,
`sp_config_settings_upsert`, `sp_config_settings_values_get` / `_delete`, plus `config_settings_history`
(table 09) for change history. A permission is a setting key; the role is the scope key.

**And the repo already has the right resolution shape.** `ImageStorageConfigurationResolver` documents its
order as *"1. Database override from `config_settings_values` (if set and not null) 2. default"* — the same
pattern `ImageStorageOptions` describes. Following it means an absent row, or a store that cannot be reached,
yields **the shipped default**, which is today's behaviour. That is the opposite of this repo's *service-API*
fail-closed rule, and it is the safer of the two here: failing closed would make one unreachable store turn
every button in the desktop app into a refusal, including for the person who could repair it.

**A matrix is not the same thing as the rank rule.** A rank rule ("may do X because rank ≥ Y") is derived and
stateless. A matrix is stored data per role per action. Template 06 still needs its rank table for its own
rule — you may not assign a role above your own rank — so the matrix **replaces the ten lists, not the rank
table**. Conflating them would put the escalation guard at the mercy of a settings row.

**Design questions this raises, none of them answered yet.**

1. **Failure semantics** — absent row or unreachable store: shipped default (the resolver precedent above), or
   refuse. This is the sharpest one and it applies to every gate in the application at once.
2. **A role with no row** — default, or deny.
3. **Who may edit the matrix, and may they edit their own access?** Template 06 already carries a
   self-lockout guard for users; this is the permission analogue, and a Plant Manager who removes their own
   access to *this* screen has no way back inside the application.
4. **The permission-name registry** — gates must be enumerable, or the screen cannot be drawn and a newly
   added gate cannot be given a default. This is the same trap that produced finding 2: a gate named in code
   and missing from the store currently fails silently.
5. **Audit** — a permission change is a security change; `config_settings_history` may already cover it, or a
   dedicated audit row may be wanted, as 06 does for user changes.
6. **`IT Department`** — 06 replaces `Admin`. The matrix must be populated for whichever of the two exists at
   the time, or the top role loses access the moment 06 ships.
7. **Analytics' own gates** — viewing the screen, viewing supervisor data, and printing/exporting are three
   new permission names this feature would contribute to the registry.

**Sequencing — the one thing that needs the owner's decision now.** Writing analytics first means its gates
are written against the ten hard-coded lists and then rewritten when the matrix lands: the same "buy it twice"
cost the owner already saw when choosing to add a role here. It is affordable, but it is not free, and it is
the owner's call whether analytics waits.

**Corrections carried forward from this session.** Finding 1 (`Setup Tech`, a gate entry that can never
match) and finding 2 (ten lists, no single definition) are **symptoms of the same missing piece**, and the
owner's decision 7 is the fix for the class rather than for the instances. Finding 3 is the odd one out: the
badge is a lookup rather than a gate, so it is settled by rewriting it against the real role codes instead of
by making it a permission (template 06, decision 15). Finding 2's specific claim — "adding one title means touching up to ten places" —
becomes untrue once the matrix exists, which is a good reason to build it before adding more titles.

### 2026-09-20 — decision 8: the permission matrix folds into template 06, and 06 runs first

**Decided (owner, 2026-09-20):** the role-permission screen and everything it gates are built inside
**template 06 (user management)** — not as a separate spec, and not here — and **06 runs before this
feature**.

| Document | What changed on the strength of this |
| --- | --- |
| `06-user-management.md` | header, §1, §2 input block, §3 (locked 1.2 amended, a matrix subsection added), §7 |
| `00-INDEX.md` | run order, the 06 carry-forward figure, the template → spec number table |
| This file | header dependency and sequencing note, §3 gates, §7, this log |

**Why 06 is the right home.** Its locked item 1.2 already refactors the same `Allowed*Roles` arrays this
decision is about, so building it anywhere else would build half of it twice. One owner for the role model
keeps the index's rule that one template is one workstream. Template 06 therefore grows to own roles, rank
**and** permissions.

**Why it runs before this feature.** Analytics' gates (view the screen, view supervisor data, print and
export) would otherwise be written as literal role lists and then rewritten once the matrix lands. Deciding
the order now means they are written once.

**What does not change.** Analytics' own design — the snapshot recorder, the metrics, the per-lead scoping,
per-user preferences, the print path — is untouched. Only *how its gates are expressed* changes.

**Numbering.** `/speckit.specify` assigns feature numbers itself and the index says not to type one into the
input, so this is an expectation and not a fact: **06 takes `specs/006-…` and this feature takes
`specs/007-…`.** Confirm against `.specify/feature.json` and the index's number table after each run.
