# Bringing the MTM database up to date

**Applies to**: `Database/**` in this repository, and any MySQL 8.0 store it is deployed to.
**Checked by**: `MTM_Waitlist.Tests/Module_Database_Deployment/**` and the three guardrail audits named below.

## Why this exists

The live `mtm_waitlist` was destroyed once by a script whose job was only to *check* it. On 2026-10-16 a
check-then-repair validator applied `Database/Bootstrap/create_database.sql` — whose first statement is
`DROP DATABASE IF EXISTS mtm_waitlist;` — to a store that already existed and was one deploy behind. Every table
came back empty, and the store was recovered from the Mock service's 01:00 dump. That host runs with binary
logging off, so there is no point-in-time recovery: the daily dump is the only copy, and it is a day old.

The validator and the GUI installer that shared its behaviour were both removed on 2026-11-16. In their place is
one tool, `MTM_Database_Deployment`, and the rule it keeps is simple: **it shows what it is about to change before
it changes anything, and it copies the database before the first statement runs.**

## What it does

The tool reads the repository's own artifact tree — `Database/{Tables,Views,StoredProcedures,Functions,Triggers,
Seeds}/**/create.sql` — and compares it with what the chosen database holds. It then offers three things:

1. **Connect.** It probes the configured server, falls back to the local one, and signs in for real so a wrong
   password is reported on the page where it was typed rather than halfway through a save.
2. **Review.** Every difference is listed in plain words: new tables, new fields, roomier fields, housekeeping
   rules, replaced stored instructions, reference lists, and anything that exists only in the database. Nothing is
   written while this page is open.
3. **Save.** It copies the database, then applies the differences in an order that cannot bury a real error.

Two modes exist. **Safe update** adds and replaces, and removes only what the operator ticks a box for.
**Start over** drops and recreates the database from the scripts — and this is the one that needed the copy.

## The copy the run takes first

Before the first statement of every run — safe update or start over — the tool copies every table of the chosen
database into a database called **`{database}_backup`**, for example `mtm_waitlist_backup`:

- `CREATE TABLE ... LIKE` carries the columns and indexes; `INSERT ... SELECT` carries the rows.
- The copy is made on the server, so nothing leaves the host and no external client is needed.
- Each table is counted before and after. A copy that holds fewer rows than its source stops the run **before**
  anything is changed, and says so.
- Foreign-key constraints are deliberately not copied: a backup has to be readable on its own, when the live
  schema is already the thing that is broken.
- Views, procedures, functions and triggers are *not* copied — they are the repository's own files, and a restore
  of them is a normal run of this tool.
- A database that does not exist yet, or holds no tables, has nothing to copy; the run says so and continues.

The copy is replaced on every run. It is the previous state, not a history: if a run is wrong, restore from it
before the next run overwrites it.

## Which scripts belong to which database

The tool ships two different schemas, and it picks between them by the database's name:

| Database name begins with | Scripts used |
| --- | --- |
| `mtm_waitlist` | `Database/{Tables,Views,StoredProcedures,Functions,Triggers,Seeds}` |
| `mtm_mock` | `Database/Mock/{Tables,StoredProcedures,Seeds}` |
| anything else | none — Check and Save are refused, with a sentence saying so |

This is a guard, not a convenience. Before it existed the tool had one tree and would deploy it wherever the picker
pointed: choosing the cache mirror and pressing Save wrote the waitlist's twenty-seven tables into it, and choosing
"Start over" replaced the mirror outright. Another application's store (`mtm_receiving_application`,
`mtm_wip_application_winforms`) is refused for the same reason — those are not this tool's to change.

**A check never writes.** A plan is built by applying the table scripts to a scratch schema whose name is generated
per run, then reading back what they made. Every shipped script opens with `USE <its own database>;`, so that line is
re-pointed at the scratch schema first; without that, the expected-schema build writes into the real store — which is
how a check once created eight empty tables in the live cache mirror.

## Seeds run once per database

A safe update does not re-seed a store that has already been seeded. The tool keeps a record — the table
`deployment_seed_ledger` in the target database, holding one row per seed script the store has received — and a
seed that is on that record is reported as skipped rather than run again.

This matters because "re-runnable" is not the same as "harmless": several shipped seeds insert with
`ON DUPLICATE KEY UPDATE`, so a second pass would write the shipped default back over whatever an operator has
since chosen. Start over is unaffected — it recreates the database, so its ledger goes with it and every seed runs
into the empty store.

A database that was set up before the tool kept a record — the live store, the cache mirror, anything an earlier
installer built — has an empty ledger but is already seeded. On the first run against such a store the tool
**records the shipped seeds as already applied and runs none of them**, and says so in the report. A seed added in
a later release is only recorded this way on that first run; from the second run onward a seed the store has not
received is offered like any other change.

To deliberately apply one seed to an existing store, delete its row from `deployment_seed_ledger` and run Check
and Save again. The table is this tool's bookkeeping: no application reads it, and the comparison never offers it
as a leftover to remove.

## Making a copy of a database

Once connected, the first page offers **Keep a copy of this database**. Type a name, press **Make the copy**, and
the tool duplicates the chosen database into a database of its own on the same computer:

- the tables, with their foreign keys, indexes, comments, engine, collation and `AUTO_INCREMENT` counter;
- every row, inserted from the source so no value is re-encoded;
- the views, with any mention of the source schema pointed at the copy — the server stores a view's tables fully
  qualified, so an unedited definition would read the source;
- the stored procedures and functions;
- the triggers, created after the rows so they cannot fire during the copy.

Every table is counted on both sides, and a copy that holds fewer rows than its source stops and says so. The
source is only read. The name is validated before anything is created — letters, digits and underscores, at most
64 characters, never the source's own name — because it reaches a statement.

**A database with that name is never replaced unless the box is ticked.** Replacing drops it first, so the box is
off by default and the refusal names the box to tick.

This is different from the copy every deployment takes for itself: that one is `{database}_backup`, it is replaced
on every run, and it deliberately carries no foreign keys or stored instructions, because it exists to be read when
the schema is what broke.

## Running it

```powershell
# Double-clickable for an operator; builds the tool for this machine and opens the wizard.
Database\DeployDatabase.cmd
```

or, from a developer shell:

```powershell
dotnet run --project MTM_Database_Deployment\MTM_Database_Deployment.csproj -p:Platform=x64
```

Connection defaults are in `MTM_Database_Deployment\deployment.settings.json` next to the executable. A site
points that file at its own server; the sign-in flyout writes the user name and password back to the same file on
that machine only.

The list of databases to choose from grows on its own: after Connect, every database the server holds whose name
starts with `mtm_` is added to the picker, so a copy made with this tool can be selected on the next run without
editing the file. The configured names come first, in the order the file lists them, and a name that is already
listed is not repeated.

Where the tool leaves things behind:

| Path | What it is |
| --- | --- |
| `%LOCALAPPDATA%\MTM Database Tool\reports\database-update-<timestamp>.txt` | The report of a run: what was applied, what was already in place, what failed, and how long it took |
| `%LOCALAPPDATA%\MTM Database Tool\logs\failure-<timestamp>.log` | A crash the operator can send to IT; never contains the password |

## What the deploy order is, and why

Tables first, then the maintained descriptions, then functions, then views, procedures and triggers, then seeds,
then the remaining removals. Descriptions come before the routines because a guarded block in that file can add a
column a routine in the same run reads; functions come before views because a fresh database has no functions until
they are created and the views rank by them; removals come last so a run that fails early destroys nothing. A
column that could not be added stops the run so the real error is visible instead of buried under a cascade of
downstream failures.

`Database/Bootstrap/update_table_descriptions.sql` is applied on every run. It is idempotent by construction —
every block asks `information_schema` whether the change is needed — and it is the file that owns a store's column
and table wording. Without it a rebuild produces the right shape and a store whose columns are documented with
whatever the table script happened to say, which is not what an updated store carries.

Every script's own `USE mtm_waitlist;` line is re-pointed at the database chosen on the first page. Without that,
a run meant for the practice copy would write to the live store: the scripts carry that line so they can be
pasted into a client, and it arrives underneath the script's header comment.

`Seeds/seed_dev_*` is never deployed — it empties tables on purpose, which is a developer's reset and not part of
bringing a store up to date.

## The guardrail exemptions this module needed, and why they are narrow

Three repository audits had to learn about the tool rather than be weakened:

| Audit | What it exempts | Why that is right |
| --- | --- | --- |
| `InlineSqlAuditTests` | Five files in `MTM_Database_Deployment.Core` that carry `information_schema` reads and DDL | Constitution III governs how the *application* reaches its stores. This tool is the one that creates those stored procedures: a fresh database has no procedure to call, and no procedure can be installed into a target that may not exist. Each entry is a named file with a stated reason, and a staleness test fails once an entry stops describing its file. |
| `LocalStateFreeTests` | The crash log and the run report | FR-025 and SC-016 govern the application, whose faults go to the store. The operator tool has to hand IT something, and it is used precisely when the store is unreachable, missing or being rebuilt. |
| `SurfaceInventoryAuditTests` | `MTM_Database_Deployment/App.xaml` and `MainWindow.xaml` | Both are listed in `docs/guides/guides-xaml-surface-inventory.md` as an `App` and a `Window`. The inventory's rule is that every surface is listed and checked, not that only the waitlist app has surfaces. |

## Verification

- Fresh rebuild into an empty local MySQL 8.0.46 database: every one of the 151 shipped objects deploys, the
  descriptions are applied on top, and a second rebuild leaves the previous database's rows in
  `{database}_backup` —
  `MTM_Waitlist.Tests/Module_Database_Deployment/DatabaseBackupLiveTests.cs` and
  `DeploymentDescriptionsLiveTests.cs`.
- Those cases are gated twice: they need `MTM_DEPLOYMENT_TEST_CONNECTION_STRING`, and the server in it must be
  this machine. A production host never sees them.
- A practice copy built from this tree on 2026-11-16 matches the plant's `mtm_waitlist` in every structural
  respect — table set, all column shapes, 161 indexes, 23 foreign keys, engines and collations, database charset,
  2 views, 3 functions, 2 triggers, and the 107 procedure names. What remains different is the unreleased work on
  this branch (one new column, eight changed routine bodies), seven routines whose bodies differ only by comments
  the `mysql` client stripped on the way into live, and one column comment live has not received yet.
- The duplicate path was proved on a real database, not a fixture: `mtm_mock` was copied into `mtm_mock_test` on
  the plant host by this module's own code, and then compared object by object — 16 tables with equal row counts
  and equal `CHECKSUM TABLE` values, identical `SHOW CREATE TABLE` text for all 16, 18 routine bodies identical,
  and nothing in the copy naming the source.
