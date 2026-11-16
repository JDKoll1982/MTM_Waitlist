# MTM_Database_Deployment — implementation status

Current as of 2026-11-16. Branch `021-generic-wc-optional-job-fill`, uncommitted at the time of writing.

This tool replaces the retired `Database/install_local_database.bat` / `.vbs` and
`.github/scripts/validate-database-schema.ps1` — the script whose `-ApplySchema` repair path dropped the live
`mtm_waitlist` store on 2026-10-16. It is ported from the working module of the same name in
`MTM_Receiving_Application`, so the workflow an operator already knows there is the workflow they get here.

## What is built and working

**Two projects, split so the suite can test the logic without hosting WinUI.**

| Project | What it holds | State |
| --- | --- | --- |
| `MTM_Database_Deployment` | WinUI 3 wizard: `App`, `MainWindow`, one view model, one converter, `app.manifest`, `deployment.settings.json` | Builds; not yet launched against a database |
| `MTM_Database_Deployment.Core` | 8 models, 7 services — connection settings, object catalog, SQL parser, MySQL gateway, schema comparer, deployment engine, failure log | Builds; parser and catalog covered by tests |
| `MTM_Waitlist.Tests/Module_Database_Deployment` | 9 test files, 78 cases | All pass |

The split is deliberate: a test project that references a WinUI executable fails every case on a missing
`Microsoft.WindowsAppRuntime.dll` module initializer, so the testable logic cannot live in the exe. Namespaces
are unchanged from the reference module, so its code reads as written.

**The wizard.** Three steps — connect, review the drift, save — with a sign-in flyout, a per-step safety line,
a copyable report, and a benign-skip channel that keeps a second deploy of an already-converged database from
looking broken. Diverge-only by default: the tool adds and replaces, it does not remove, unless the operator
explicitly confirms removing objects the database has and the scripts do not.

**The engine's deploy order.** Tables, then computed column/index/foreign-key changes, then views, procedures,
functions and triggers, then seeds, then the remaining removals last so a failed run never destroys anything
early. A failure during schema reconciliation stops the run rather than burying the real error under a cascade
of downstream failures.

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
3. **`USE` is re-pointed at the database the operator chose.** Every object script opens with
   `USE mtm_waitlist;` so it can be pasted into a client. Left alone that line overrides the picker, so a run
   meant for the practice copy would write to the live store. The engine rewrites it and says so in the report.
4. **A development reset is never deployed.** `Seeds/seed_dev_*` empties tables on purpose; it is skipped by the
   catalog rather than by a switch nobody would remember to clear.
5. **Two seeds no longer truncate.** `seed_setup_work_centers_default` and `seed_waitlist_request_item_configs`
   emptied their tables before inserting. They now insert only what is missing, matching every other seed and
   the rule that a re-run changes nothing and an operator's own rows survive.

## Verification

- `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64` — **0 warnings, 0 errors.**
- `dotnet test --filter FullyQualifiedName~Module_Database_Deployment` — **78 passed, 0 failed.**
- Full suite — **2799 passed, 56 skipped, 3 failed.** The three failures are the guardrail audits below, which
  did not yet know about this module.

## Not done yet

| Item | Why it matters |
| --- | --- |
| Three guardrail audits fail | `InlineSqlAuditTests` flags 5 SQL hits, `LocalStateFreeTests` flags 2 files writing under `%LOCALAPPDATA%`, `SurfaceInventoryAuditTests` flags `App.xaml` / `MainWindow.xaml`. All three are legitimate for a deployment tool — its whole job is to carry SQL, and its own XAML is not an app surface — but each needs an explicit, narrow exemption rather than a silent pass. |
| Never run against a live database | No deployment has been executed. Every engine path that touches a server is untested end-to-end, and the 56 skipped cases are the live-database integrations that would exercise them. |
| The exe has never been launched | The wizard builds but has not been opened, so layout, the sign-in flyout and the report window are unverified visually. |
| Old tree still authoritative | `Database/{Tables,…}` is now what the tool reads, so it is the single source of truth — but `Database-Ruleset.md`, `AGENTS.md` and `.github/copilot-instructions.md` still describe the retired installer and the deleted validator. |
| No launcher | Nothing in the solution runs the tool for an operator; it is built, not yet reachable. |

## The rule this module exists to keep

The retired validator's repair path applied a bootstrap whose first statement is
`DROP DATABASE IF EXISTS mtm_waitlist;` to a store that already existed and was one deploy behind. This tool has
no such path: it creates what is missing, replaces what differs, and removes only what a person confirmed on a
screen that named the object. The plant host runs with binary logging off, so a mistake here cannot be undone
from a log — only from the Mock service's daily dump, a day old.
