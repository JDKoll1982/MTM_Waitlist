# MTM_Database_Deployment — implementation status

Current as of 2026-11-16. Branch `021-generic-wc-optional-job-fill`, uncommitted at the time of writing.

This tool replaces the retired `Database/install_local_database.bat` / `.vbs` and
`.github/scripts/validate-database-schema.ps1` — the script whose `-ApplySchema` repair path dropped the live
`mtm_waitlist` store on 2026-10-16. It is ported from the working module of the same name in
`MTM_Receiving_Application`, so the workflow an operator already knows there is the workflow they get here.
Operator guide: `docs/guides/guides-database-deployment-tool.md`.

## What is built and working

**Two projects, split so the suite can test the logic without hosting WinUI.**

| Project | What it holds | State |
| --- | --- | --- |
| `MTM_Database_Deployment` | WinUI 3 wizard: `App`, `MainWindow`, one view model, one converter, `app.manifest`, `deployment.settings.json` | Builds; launched and read through UI Automation |
| `MTM_Database_Deployment.Core` | 9 models, 8 services — connection settings, object catalog, SQL parser, MySQL gateway, schema comparer, database backup, deployment engine, failure log | Builds; exercised end-to-end against a local MySQL 8.0.46 server |
| `MTM_Waitlist.Tests/Module_Database_Deployment` | 16 test files, 101 cases (92 run without a live server; 9 are the gated live-database cases) | All pass |

The split is deliberate: a test project that references a WinUI executable fails every case on a missing
`Microsoft.WindowsAppRuntime.dll` module initializer, so the testable logic cannot live in the exe. Namespaces
are unchanged from the reference module, so its code reads as written.

**The wizard.** Three steps — connect, review the drift, save — with a sign-in flyout, a per-step safety line,
a copyable report, and a benign-skip channel that keeps a second deploy of an already-converged database from
looking broken. Diverge-only by default: the tool adds and replaces, it does not remove, unless the operator
explicitly confirms removing objects the database has and the scripts do not.

**The picker grows with the server.** The configured databases come first, and Connect adds every `mtm_*` database
the server holds, so a copy made with the tool is selectable on the next run without editing a settings file. An
older settings file is also merged with the shipped names on load, which is why the cache mirror's copy appears
even on a machine whose `deployment.settings.json` predates it.

**The copy the operator can ask for.** Step 1 offers **Keep a copy of this database**: an exact duplicate into a
database whose name the operator types — tables with their foreign keys, indexes, comments and `AUTO_INCREMENT`
counter, every row, the views (re-pointed at the copy), the stored procedures and functions, and the triggers
(created after the rows, so they cannot fire during the copy). Each table is counted on both sides and a short copy
stops. A name that already exists is never replaced unless the operator ticks the box, and the name is validated
before anything is created because it reaches a statement.

**The copy every run takes first.** Before its first statement, in both modes, the tool copies every table of the
chosen database into `{database}_backup` with `CREATE TABLE ... LIKE` and `INSERT ... SELECT`, on the server. Each
table is counted before and after, and a copy that holds fewer rows than its source stops the run before anything
changes. Foreign-key constraints are deliberately not carried, so the backup is readable on its own when the live
schema is the thing that is broken. On a host with `log_bin = OFF` this copy is the only undo a deployment has.

**The engine's deploy order.** Tables, then computed column/index/foreign-key changes, then functions, views,
procedures and triggers, then seeds, then the remaining removals last so a failed run never destroys anything
early. Functions come before views because a fresh database has no functions until they are created and the
views rank by them. A failure during schema reconciliation stops the run rather than burying the real error under
a cascade of downstream failures.

**Two schemas, chosen by name.** `mtm_waitlist*` databases are deployed from `Database/`, `mtm_mock*` databases from
`Database/Mock/`, and any other name is refused with a sentence rather than guessed at. Before this existed the tool
had one tree and would write it wherever the picker pointed — which is how the cache mirror's copy was once rebuilt
as the waitlist store.

**A check writes to nothing.** The plan is built by applying the table scripts to a per-run scratch schema; every
script's own `USE` line is re-pointed at that scratch schema first, because otherwise the expected-schema build
writes into the real store. That leak did happen once — a check created eight empty tables in the live mirror — and
`SchemaComparerScopeLiveTests` now fails if it returns.

## Decisions that changed the ported design

1. **Driver: `MySql.Data` → `MySqlConnector`.** The repository already ships `MySqlConnector` everywhere else;
   adding a second driver would mean two connection pools to the same server. `AllowPublicKeyRetrieval = true`
   was added for MySQL 8.0 caching-SHA2 authentication over a non-TLS local connection.
2. **The catalog reads the repository's own tree, not a generated `Sql_Files` copy.** An earlier pass generated
   a normalised deployment tree under `Database/Database_Deployment/Sql_Files`. It was removed: the repository's
   convention is one folder per object holding `create.sql` + `rollback.sql`, and every audit test already reads
   those paths. A second copy would drift the moment someone edited one of the two. The catalog now reads
   `Database/{Tables,Views,StoredProcedures,Functions,Triggers,Seeds}/**/create.sql` directly and ignores
   `rollback.sql`, the `All*.sql` aggregates, and `_Archive`.
3. **`USE` is re-pointed at the database the operator chose — including underneath a header comment.** Every
   object script opens with `USE mtm_waitlist;` so it can be pasted into a client. Left alone that line overrides
   the picker, so a run meant for the practice copy would write to the live store. The splitter keeps the header
   comment inside the statement it precedes, so the re-pointing steps over the comment block first; without that
   a rebuild of a scratch database reported 130 failures naming a database it had never created.
4. **A development reset is never deployed.** `Seeds/seed_dev_*` empties tables on purpose; it is skipped by the
   catalog rather than by a switch nobody would remember to clear.
5. **Two seeds no longer truncate.** `seed_setup_work_centers_default` and `seed_waitlist_request_item_configs`
   emptied their tables before inserting. They now insert only what is missing, matching every other seed and
   the rule that a re-run changes nothing and an operator's own rows survive.
6. **The connection check pings instead of running a statement.** `SELECT 1` was inline SQL in the view model and
   a statement the tool has no reason to own; `MySqlConnection.PingAsync` proves the same thing.
7. **The maintained descriptions are applied on every run.** `Database/Bootstrap/update_table_descriptions.sql`
   owns a store's column and table wording, and a rebuild that skipped it produced the right shape with the wrong
   documentation: 230 columns came out worded as their table script had them. It is idempotent by construction, so
   it runs after the tables and before the routines — a guarded block in it can add a column a routine reads.
   Applying it exposed three places where the file had fallen behind its own artifacts
   (`waitlist_requests_queue.description`, `.active_setup_job_id` and `.is_generic`), which are now aligned so the
   file and the table scripts say the same thing.
8. **A per-project build is forced onto x64.** WinUI is never AnyCPU — the Windows App SDK self-contained payload
   refuses it — and the solution's Any CPU configuration is mapped to x64 only when the *solution* is what is
   built. The C# extension's build task invokes the project directly with `Platform=AnyCPU`, which failed with
   "WindowsAppSDKSelfContained requires a supported Windows architecture". `TreatAsLocalProperty="Platform"` on
   the `Project` element plus a normalization line makes the project build the same way either route takes.
9. **The artifact tree is chosen by database name.** `mtm_waitlist*` deploys from `Database/`, `mtm_mock*` from
   `Database/Mock/`, and any other name is refused rather than guessed at. The tool previously had one tree and
   wrote it wherever the picker pointed: selecting the cache mirror's copy and choosing "Start over" rebuilt it as
   the waitlist store, which is exactly what happened on 2026-11-16. The copy was restored from the mirror, and the
   tree now ships beside the executable so the mirror can be deployed from this tool at all.
10. **A plan never writes.** `Service_SchemaComparer` applied the table scripts to its scratch schema without
    re-pointing their `USE` lines, so building the expected schema wrote into the database each script named — a
    check created eight empty tables in the live mirror. The scripts are now scoped to the scratch schema, and the
    regression case fails without that.
11. **A seed runs once per database.** `deployment_seed_ledger` in the target database records each seed script the
    store has received, and safe update skips what is recorded. Several shipped seeds insert with
    `ON DUPLICATE KEY UPDATE`, so re-running them put shipped defaults back over an operator's values — the thing a
    safe update must not do. A store set up before the ledger existed is *recorded* as seeded on the first run and
    none of its seeds are run; the decision is taken from the store's state before the run creates anything, and
    the plan agrees with it. The table is excluded from the comparison so it is never offered as a leftover.

## Verification

- `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64` — **0 warnings, 0 errors.**
- `dotnet test --filter "FullyQualifiedName~Module_Database_Deployment|FullyQualifiedName~InlineSqlAuditTests|FullyQualifiedName~LocalStateFreeTests|FullyQualifiedName~SurfaceInventoryAuditTests"`
  — **92 passed, 2 skipped, 0 failed**.
- `dotnet test --filter FullyQualifiedName~Module_Database_Deployment` — **92 passed, 9 skipped, 0 failed** (the
  skipped ones are the live-database cases; with `MTM_DEPLOYMENT_TEST_CONNECTION_STRING` set all 101 pass).
- Full suite — **2822 passed, 61 skipped, 0 failed.** The guardrail audits that failed earlier in this pass now
  pass, each with a named exemption rather than a silent skip; see the table below. The C#/XAML naming guard is
  unchanged at its pre-existing **New: 83** — nothing added here contributes a finding.
- Fresh rebuild into an empty local MySQL 8.0.46 database: every one of the 151 shipped objects deploys, and a
  second rebuild leaves the previous database's rows in `mtm_deployment_check_backup`
  (`DatabaseBackupLiveTests`, gated on `MTM_DEPLOYMENT_TEST_CONNECTION_STRING` plus a this-machine check).
- `mtm_waitlist_test` was created on the plant host from this tree and compared with the live `mtm_waitlist`
  column by column and routine by routine. Identical: the table set, every column type/nullability/default,
  161 indexes, 23 foreign keys, engines and collations, database charset, 2 views, 3 functions, 2 triggers, and
  all 107 procedure names. Different, and expected: the branch's unreleased work (one column,
  `waitlist_requests_queue.provenance_work_order`, and eight routine bodies), seven routines that differ only by
  comments the `mysql` client stripped on the way into live, and one column comment live has not received yet.
- The exe was launched and read through UI Automation: the window title, the three step headings, the three
  database choices and the sign-in summary all render. The step-3 layout, the flyout and the report window were
  read from the markup, not driven.

## The guardrail exemptions, and why they are narrow

| Audit | What it exempts | Why that is right |
| --- | --- | --- |
| `InlineSqlAuditTests` | Six files in `MTM_Database_Deployment.Core` | Constitution III governs how the *application* reaches its stores. This tool creates those stored procedures: a fresh database has no procedure to call yet, and no procedure can be installed into a target that may not exist. Each entry names one file and states what is there; a staleness test fails once an entry stops describing its file. |
| `LocalStateFreeTests` | The crash log and the run report | FR-025/SC-016 govern the application, whose faults go to the store. The operator tool must hand IT something, and it is used precisely when the store is unreachable, missing or being rebuilt. Two entries, both named. |
| `SurfaceInventoryAuditTests` | `MTM_Database_Deployment/App.xaml` and `MainWindow.xaml` | Both are now listed in `docs/guides/guides-xaml-surface-inventory.md` as an `App` and a `Window`. The inventory's rule is that every surface is listed and checked, not that only the waitlist app has surfaces. |

## Not done yet

| Item | Why it matters |
| --- | --- |
| Live is one deploy behind the tree | The comparison above is the proof: `mtm_waitlist` is missing the branch's new column and eight routine bodies. Nothing has been deployed to it by this tool yet, and doing so is the next decision, not a step to take quietly. The cache mirror is behind its own tree for the same reason: `Database/Mock` carries four read shapes the mirror does not have yet. |
| The mirror's copy has no history of the incident | On 2026-11-16 `mtm_mock_test` was rebuilt with the waitlist tree and then restored from `mtm_mock`. `mtm_mock_test_backup` still holds the copy taken before that rebuild; nothing prunes it. |
| The practice copy has no operator data | `mtm_waitlist_test` carries the schema and the standard seed rows. Live's own rows — 13 requests, 11 people, 29 audit entries — were not copied, because a second store holding people's data is a decision, not a default. |
| The step-3 screens were not driven | The connect page renders and the rebuild path is proven by test, but the review list, the sign-in flyout, the removal checkbox and the report window have not been exercised with a person at the keyboard. |
| `Database/Bootstrap/create_database.sql` is unreferenced | Nothing runs it any more — the rebuild path creates the database itself. It is kept as the record of the fresh-store path; `update_table_descriptions.sql` beside it is now applied by the engine on every run. Decide whether the create script stays as documentation or goes. |
| No history of copies | The `{database}_backup` copy is replaced on every run, so it is the previous state rather than a history. If a second run follows a bad first one, the good copy is gone. |
| The module's type names fail the C#/XAML naming guard | Every type here carries a `Model_` / `Service_` prefix, ported unchanged from the reference module, so the guard counts 25 findings it would otherwise not have. The guard was at **New: 83 before this pass and is at New: 83 after it** — the artifacts added here were named to the repository's own convention rather than the module's, so nothing new was introduced. Renaming the 25 committed types is an owner decision, not something to slip into a feature branch. |

## The rule this module exists to keep

The retired validator's repair path applied a bootstrap whose first statement is
`DROP DATABASE IF EXISTS mtm_waitlist;` to a store that already existed and was one deploy behind. This tool has
no such path: it creates what is missing, replaces what differs, removes only what a person confirmed on a
screen that named the object, and copies the database before it starts. The plant host runs with binary logging
off, so a mistake here cannot be undone from a log — only from that copy or the Mock service's daily dump.
