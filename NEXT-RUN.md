# Next run — finish 012 on a plant-connected PC

**Who this is for: the agent.** The owner links this file in chat on a machine that can reach **Infor Visual** and
whose **MySQL stores have not yet been brought up to this branch**. Read it, then work it top to bottom. Do not ask
the owner to restate any of it.

| | |
|---|---|
| Branch | `012-waitlist-card-layout` |
| Feature | `specs/012-waitlist-card-layout` |
| Companion step | `implement` — **already** `implementing`, with 109 of 117 tasks ticked |
| Do **not** re-run | `specify`, `clarify`, `plan`, `tasks` for 012. The artifacts are settled and this branch is the record. |

**The one-line summary.** Eight tasks were left unticked because the machine they were written on could not reach the
plant: one Infor Visual SQL Server and no live `mtm_waitlist`. On the next machine both exist, so those eight become
doable — **but only after the stores are brought up to this branch**, because a store that has fallen behind its own
artifacts fails in a way that is invisible at run time (see §2, and the defect that cost a day: T112).

---

## 0. Read these first

1. **`LIVE-DB-AND-MOCK-UPDATES.md`** — the owner's own live-versus-repository comparison, done against the real
   `172.16.1.104` on 2026-10-14. It names the exact objects the live server is missing, in the order to apply them,
   with verification queries and two warnings that must not be ignored. **Do its "In order" list first**; §2 below
   only adds what it does not cover.
2. `specs/012-waitlist-card-layout/spec.md` — the requirements. FR-088 → FR-095 and FR-077 → FR-087 are the live-data
   ones; **SC-001 → SC-017** are what "verified" means.
3. `specs/012-waitlist-card-layout/tasks.md` — the eight unticked tasks carry their own blocking reason in the text of
   **T103**, which was written for exactly this hand-off. Read T103 before anything else.
4. `.github/memories/repo/infor-visual-disposition.md` — the Infor Visual / WIP facts, the two open clarifiers, and
   the exact shape of what T067 still has to confirm.
5. `Database/CopilotScripts/README.md` — the drift, deploy and verify scripts used in §2.
6. `.github/copilot-instructions.md` — the rules for this repo, which apply unchanged on the next machine.

---

## 1. What is actually left

| Task | What it needs | Why it is unticked |
|---|---|---|
| **T048** | drive the status column through every state against a live store | needs a store with real requests in each state |
| **T058** | drive the handler-condition flyout and confirm it survives a restart | needs a live store to write the condition to |
| **T067** | run `Database/Validation/disposition_input_finished_goods/validate.sql` against Infor Visual | the Visual host does not resolve off the plant network |
| **T068** | record the location codes T067 reports | depends on T067 |
| **T069** | drive the picker for each job kind and check the four returning Items' cards | needs live job data from Visual |
| **T071** | run `GetPartMatches.sql` against Infor Visual for an exact and a partial term | live Visual only — it has no cached shape **by design** (FR-084) |
| **T079** | one request per new answer, each checked on its card against SC-015 | needs a live store and a rendered card |
| **T088** | keyboard operability, no clipped values at the default window size, usable with animations off | needs the rendered application |

Everything else in the feature is built, and the non-live halves of T067/T079/T088 are already pinned by tests —
each task's own text says which half is done, so read it rather than re-deriving the gap.

---

## 2. Step 1 — bring the stores up to this branch

**Do this first.** The card's answers are silently discarded by a store that has fallen behind, with nothing logged
and nothing on screen (that is the T112 defect, and it is why this step is not optional). Nothing in §3 or §4 can be
trusted against a stale store.

### 2.0 The owner's list comes first

`LIVE-DB-AND-MOCK-UPDATES.md` is a real live-versus-repository comparison of `172.16.1.104`, and it is more specific
than anything derivable from here. Its findings, in its own order:

- **One column rename** on `core_computers_registry` (`description` → `location`), which
  `CREATE TABLE IF NOT EXISTS` cannot fix and so has to be issued by hand.
- **One missing procedure** (`sp_auth_user_previous_sign_ins_list`) and **five stale ones** (the three
  `sp_core_computers_registry_*` writes/reads and `sp_machine_configuration_reset`). Each file is self-contained
  (`DROP` + `CREATE`), so applying the file is the whole job — and five of them use `DELIMITER $$`, which the
  `mysql` client honours **only when the script arrives on stdin**, so pipe it in with `<` rather than `-e`.
- **Apply the rename before the procedures**, or both in one sitting: the procedures name the new column.
- **Back up first**, and **do not run `Database/Seeds/seed_config_image_storage_settings/create.sql`** — its first
  statement `TRUNCATE`s `config_settings_values`, which on live holds 171 role-scoped permission rows and 5
  user-scoped rows, and nothing in the seed path puts the user-scoped row back. Insert the four `image_storage.*`
  keys, or run only its `INSERT … ON DUPLICATE KEY UPDATE` statement, skipping the `TRUNCATE`.
- Its **Part 3** records what it checked and found **already correct** — do not redo it: `mtm_receiving_application` and
  `mtm_wip_application_winforms` need nothing (every procedure the app calls is present), the triggers match, and
  `Database/Bootstrap/update_table_descriptions.sql` is documentation metadata that live does not need.
- It also says the live `mtm_waitlist` objects were all created or altered on **2026-10-05**, which is the whole
  cause of the drift: the store is one redeploy behind the repository.

### 2.1 Find the store the application is actually reading

The app resolves its own host and falls back to `localhost` when the configured plant host is unreachable
(`MTM_Waitlist.Core/Services/MySqlHostFallback`). On this machine the configured host should answer, so the fallback
should not fire — confirm rather than assume, and note the store you conclude with:

```powershell
pwsh -NoProfile -File Database/CopilotScripts/refresh_schema_drift.ps1
```

That report is the authoritative "is this store current" answer. It defaults to `mtm_waitlist` and to
`$env:MTM_WAITLIST_DB_CONNECTION_STRING`.

**Prefer the owner's targeted list over a blanket repair.** `-Apply` rewrites every routine in the schema from the
repository, which is a broad write to a production store. The report is read-only and tells you exactly which
routines are missing or parameter-drifted, so use `-Apply -Procedure <names>` for what it proves wrong, and reach for
the unfiltered `-Apply` only if the targeted pass leaves findings standing.

### 2.2 The two new columns

Specs 012 added exactly two columns to `waitlist_requests_queue`: `handler_status` (the handler's condition) and
`answers_json` (the answers a request captured beyond `input_value`). Both are nullable with no default.

```sql
SHOW COLUMNS FROM mtm_waitlist.waitlist_requests_queue LIKE 'handler_status';
SHOW COLUMNS FROM mtm_waitlist.waitlist_requests_queue LIKE 'answers_json';
```

The owner's 2026-10-14 comparison **did not flag either column**, so expect both to be present — this is a
confirmation, not a repair. If one is genuinely **missing**, add it with an `ALTER TABLE` whose definition is copied
verbatim from `Database/Tables/18_waitlist_requests_queue/create.sql`; that file is the source of truth and it
documents both columns and why each exists, so read its header before you touch anything.

- There is **no migration runner in this repository** (`Database/Database-Ruleset.md`, *Artifact Layout and Release
  Governance*), and the table artifact is a `CREATE TABLE` with no per-column `ALTER`. So on an installed store this
  is a **manual, DBA-reviewed change** — write the `ALTER`, keep the reversal beside it, and say plainly in the run
  journal that it was applied by hand because the repository has no migration path for a column add.
- **Do not reinstall the store to get a column.** `mtm_waitlist` holds the plant's live requests. Reinstalling from
  seed is destructive, and the ruleset calls a reinstall the path only for a table "the reinstall recreates from
  seed" — a production store is not that. If the store is genuinely unusable, stop and ask the owner.

### 2.3 The routines

```powershell
# What is missing or parameter-drifted (read-only)
pwsh -NoProfile -File Database/CopilotScripts/refresh_schema_drift.ps1

# Repair only what the report proved wrong, by name
pwsh -NoProfile -File Database/CopilotScripts/refresh_schema_drift.ps1 -Apply `
    -Procedure sp_waitlist_request_insert,sp_waitlist_request_list
```

The three routines this feature moved are `sp_waitlist_request_insert` (gained `p_answers_json`),
`sp_waitlist_request_list` (gained `answers_json` and `assigned_material_handler_name`) and the new
`sp_waitlist_request_handler_status_update`. The owner's comparison did not flag them either, so again: confirm, and
repair only what the report proves.

One caveat on that comparison, and the reason this report still matters: it compared `mtm_waitlist` procedure
**bodies** textually (`.specify/_live/dbg.ps1` against `AllSPs.sql`) and found everything matching, but a body cannot
be told from reformatting that way — which is exactly why `-Apply` exists and why the drift report deliberately does
not claim body drift. So treat `-Apply` as the belt to the owner's braces, not as a replacement for his list.

Re-run the report with no `-Apply` afterwards and confirm **parameter drift 0** and **no missing routines**. Two
standing findings are fine and must be left alone: `sp_core_account_setup_create` is absent from the local store and
belongs to another feature's path (recorded by T112), and the retired remembered-sign-ins objects are the owner's
*optional* tidy-up (his file's §1.6), not a defect.

### 2.4 The seeds

Seeds are never applied implicitly; name each one, because a seed is a data change.

```powershell
pwsh -NoProfile -File Database/CopilotScripts/refresh_schema_drift.ps1 -Apply -Seed seed_waitlist_request_item_configs_label_move
pwsh -NoProfile -File Database/CopilotScripts/refresh_schema_drift.ps1 -Apply -Seed seed_waitlist_request_item_configs
```

- `seed_waitlist_request_item_configs_label_move` is the **idempotent in-place** migration that rewrites existing
  declaration rows to `Die Location` / `Home Location` without wiping operator edits. **Apply it first** on any store
  that has been configured by hand — including the plant store, where the owner's check records the item configs as
  already applied (24 of them). Running the truncate-and-reinsert seed first would throw away any local edit before
  the migration had a chance to move the labels.
- `seed_waitlist_request_item_configs` then reconciles every Item's declared cells with what the card draws. A stale
  one is why the die Items "did not even declare the die-shop cell" (T112).
- The owner's check already lists the item configs as applied on live, so expect the migrations to report "already
  moved" rather than changing rows. What matters is the **end state**: the two die Items must declare `Die Location`,
  both dunnage Items `Home Location`, and the die Items must declare the die-shop answer. `RequestItemConfigSeedAlignmentTests`
  is the alignment that has to hold, and `UI_WaitlistLineCardDetailTemplateTests` walks the shipped declarations, so a
  store that disagrees with the seed will show up as a cell drawing its own key rather than failing loudly — check
  the two die cards on screen.

### 2.5 The mock cache — the seventh shape

**The owner's check says `mtm_mock` is already current, so do not redeploy it.** That matters because the obvious tool
is the wrong one here: `verify_mtm_mock_deploy.ps1` **drops and redeploys** the cache from the master lists, which
would empty the mirror on a production host until the next refresh. Run its non-destructive half instead, or just
check the shape directly:

```powershell
$env:MYSQL_PWD='root'; pwsh -NoProfile -File Database/CopilotScripts/build_mtm_mock_masters.ps1 -Check
```

`-Check` regenerates the masters in memory and exits non-zero if a committed master list is stale — that is the
verification worth having, and it touches nothing.

**Confirm the seventh shape exists on the host.** The owner's Part 2.1 counts *six* shapes; the repository carries
**seven** — the seventh is `part_warehouse_quantities`, added by this feature:

- `Database/Mock/Tables/visual_part_warehouse_quantities_result/` (+ `_stage`)
- `Database/Mock/StoredProcedures/sp_visual_part_warehouse_quantities_get/` and `_refresh/`
- `Database/InforVisual/Queues/Module_Mock/Populations/part_warehouse_quantities_population.sql`

If it is missing on the host, deploy **that shape alone** (its two `create.sql` files, then the population script)
rather than running the whole destructive redeploy. If it is present, nothing to do.

**One known defect to chase on the host, from the owner's own Part 2.3:** the `employee_by_number` shape reports
`lastOutcome: succeeded` and 2 084 rows, but `refreshedUtc` is `null` and `isSeedContentOnly` is `true` — so it is
still serving the seeded baseline and has never held a refreshed snapshot, while the other shapes refreshed
successfully. Check it after the redeploy; if it is still seed-only, the refresh for that shape is reporting success
without swapping a snapshot, and that is a real defect worth recording even though it is outside this feature.

> **Do not deploy the Mock.Service from this machine unless it *is* the server.** `MTM_Waitlist.Mock.Service/deploy/
> install-mock-service.ps1` refuses with exit code 2 unless the local IPv4 addresses include `172.16.1.104`
> (`V-MTMFG-5.mantoolmfg.com`). A workstation that can *reach* Visual is not that host. Do not pass
> `-AllowNonServerHost`; hand the deployment to whoever is on the server. `LIVE-DB-AND-MOCK-UPDATES.md` Part 2.2 has the
> command and the reason, and the running service was the **1.0.0** build from 2026-10-05 while the repository is
> `1.1.0`.

### 2.6 What "current" looks like

- The owner's "In order" list is done through its step 6, and the verification queries in his file's §1.5 return what they
  expect: `sp_auth_user_previous_sign_ins_list` present, `core_computers_registry` carrying **only** `location`, and
  the retired remembered-sign-ins routines gone if you took the optional tidy-up.
- Drift report: no missing routines, parameter drift 0.
- Both new columns present on `waitlist_requests_queue`.
- Both seeds applied; the label move re-applied cleanly (it is idempotent).
- `build_mtm_mock_masters.ps1 -Check` exits 0 — **not** the destructive redeploy.
- The app starts and the waitlist list draws cells rather than a row's type name. That is the fastest end-to-end
  proof that the store is current, because the cell templates are the first thing a stale store blanks out.
- Restart the desktop app and, as the owner's list asks, exercise **sign-in**, the **previous-people list** and the
  **machine-configuration reset** — those three are what his Part 1 repairs exist to unblock, and they are not part of
  this feature's own verification.

---

## 3. Step 2 — T067 / T068: the Infor Visual location codes

**This is the one thing only the plant network can answer, and it is still provisional.**

`GetDispositionInput.sql` and its mock population twin now sum `PART_LOCATION.QTY` at the part's finished-goods
locations instead of reading `PART.QTY_ON_HAND` (the FR-090 defect). The class they use —

```
LOCATION.DESCRIPTION LIKE '%FINISHED GOODS%' OR WAREHOUSE.DESCRIPTION LIKE '%FINISHED GOODS%'
OR LOCATION.ID IN ('FG','DC-FG')
```

— was copied from the WIP side's taxonomy. **Those codes are unconfirmed for this installation.** The repo's own CSV
export is schema-only (no rows, no enum values), so nothing local can settle it.

```powershell
sqlcmd -S <visual-server> -d VISUAL -U <user> -P <password> `
    -i Database/Validation/disposition_input_finished_goods/validate.sql -v PartNumber="24733431"
```

(or set `@PartNumber` in the file and run it from SSMS). `sqlcmd` with ODBC 170 works against Visual; the password is
easiest passed as `$env:SQLCMDPASSWORD` rather than on a command line.

The script returns rows at every step — **nothing passes silently**, and its own header tells you how to read it:

1. every stocked location, its warehouse, and whether the finished-goods class matches it;
2. the NCM locations this installation actually uses;
3. the `ENUM_CODES` lists for `PART_LOCATION.STATUS` and `LOCATION.TYPE`;
4. the assertion: one known part's finished-goods sum beside `PART.QTY_ON_HAND` and its total.

**Read the output before trusting the predicate.** If step 4 reports the finished-goods sum equal to the part's
total, or step 1 matches no stocked location, the class is **wrong** for this installation. Then correct it in **all
four** places that apply it — they are deliberately identical so that a live read and a cached read cannot disagree:

- `Database/InforVisual/Queues/Module_Waitlist/Queries/GetDispositionInput.sql`
- `Database/InforVisual/Queues/Module_Mock/Populations/disposition_input_population.sql`
- `Database/InforVisual/Queues/Module_Waitlist/Queries/GetPartWarehouseQuantities.sql`
- `Database/InforVisual/Queues/Module_Mock/Populations/part_warehouse_quantities_population.sql`

Then re-run the validation, regenerate the mock masters if a queue script changed, and re-verify.

**T068** — record what the plant actually uses in `.github/memories/repo/infor-visual-disposition.md`, replacing the
"STILL UNCONFIRMED" paragraph with the codes found (and, if the predicate had to change, saying so and why). Close
both tasks with the step output as the evidence, and note that FR-090's *rule wording* stays with `clarify` — that
was decided in the task and must not be re-opened here.

---

## 4. Step 3 — the live end-to-end pass

Drive the real application. The tooling is in `tools/` and is documented in `tools/UI-AUTOMATION.md` — read §1 and §2
of that file before the first click; it records the primitives, the timing constants and the two capabilities that do
not exist (there is no `Invoke` for a hover, and `AutomationElement.FromPoint` answers "a pane" for WinUI content).

Sign-in: the app's own account and PIN. Pass them at run time (`tools/ui-drive.ps1 -Action signin -Identifier <user>
-Pin <pin>`); **never** write a credential into the document, a script, a test or a commit.

| Task | Drive | Record against |
|---|---|---|
| **T048** | unclaimed, claimed, overdue-with-no-condition, completed, cancelled, and one carrying a condition | SC-006, SC-007 — the word, the ring (left vs overrun), the person row, the theme, and that the condition and overdue are never both shown |
| **T058** | set a condition, refresh the list, restart the app | SC-016 — stored, re-themes the column, replaces an existing condition, restored after a restart |
| **T069** | the picker for each kind of job work | SC-010 — the four Items offered only when the job has the work, `Part Type` + `# of Skids` and **no** location cell |
| **T071** | `GetPartMatches.sql` for an exact term and a partial one | FR-082 → FR-084 — exact resolves at once, partial lists every containing part, and it is live (no cached shape) |
| **T079** | one request per new answer | SC-015 — the hopper size in `Size` **and its cell opening that size's own reference picture**, the die-shop phrase only when ticked, both wrong-part values, the weight, the count |
| **T088** | keyboard only; the default window size; animations off | SC-014, SC-017 — the flyout and every action button reachable and operable, nothing clipped, the card usable with animations off |

T079's first item is the one that was missing a mechanism until T097 built it, so it is the item to watch: the hopper
`Size` cell must enlarge its size's reference picture the way the card's other pictures do. The pictures live in the
`Waitlist\Hoppers` folder under the configured picture folder, one file per size word (`Small.png`, `Medium.png`,
`Large.png`); the folder is a setting on **Settings → Storage Paths** and the naming contract is
`docs/IMAGE-NAMING.md`. If a size's picture is absent the shared placeholder must draw — that is FR-080, not a defect.

Tick each task **only** with the evidence in hand, and put the evidence in the task's own line — the file's own rule
is that a task is ticked only once it is implemented **and verified**.

---

## 5. Step 4 — `speckit.companion.auto` for what is not 012's to decide

Everything that belongs to 012 goes through step 3. What is left are questions the inputs never settled, and those
belong to a **new** spec, which is exactly what `speckit.companion.auto` is for.

**Know what it does before you type it.** `speckit.companion.auto` is a **fresh-spec entry point**, exactly like
`specify`: it resolves the feature directory and **mints a new one**. `.specify/feature.json` is an *output* there,
not something to reuse — it currently points at `specs/012-waitlist-card-layout`, and reusing it would clobber the
finished 012 artifacts. It then runs `specify → plan → tasks → implement → mark-complete` with no pauses at review
gates, records `unattended: true`, and ends at `completed`. There is exactly one writer of `completed`
(`write-context.py --mark-complete`) — do not dispatch `mark-complete` by hand as a fifth step.

So: give it the **open questions**, one feature's worth, and let it mint the next `specs/NNN-…` beside 012.

The open questions, with where each is recorded:

| Question | Where it is recorded today |
|---|---|
| **FR-092 — where does NCM sit against Outside Service** when a part has both subcontracted work *and* stock at an NCM location? Today Outside Service wins and `pickup-ncm` is not offered for that job — an assumption, not a settled rule. | `spec.md` FR-092 `[NEEDS CLARIFICATION]`, and `infor-visual-disposition.md` (the priority list, decided there) |
| **FR-095 — remove the dunnage `Description` declaration, or keep it declared with the cell never drawn?** It is declared in the seed and provably unfillable, since no dunnage record carries one. | `spec.md` FR-095 `[NEEDS CLARIFICATION]`; the declaration is left in place by T083/T099 with a stated-fact TODO |
| **FR-090 — does the classifier's rule *wording* need amending**, as distinct from the input the code now reads? T066 corrected the input; the wording was deliberately left. | `spec.md` FR-090 `[NEEDS CLARIFICATION]`, and the note T068 writes |
| Anything **step 3 exposes** — a corrected finished-goods code, or a behaviour the live pass proves wrong that 012's own tasks cannot absorb | the run journal, and the task line it came from |

Invoke it with the feature described in the owner's words, not as a list of FR numbers — for example:

```
/speckit.companion.auto settle the remaining open questions on the waitlist card: where NCM sits in the item-type
priority when a part has both subcontracted work and stock at an NCM location, whether the dunnage Description
declaration is removed or kept with the cell hidden, and whether the item-type classifier's rule wording follows the
corrected finished-goods input
```

Then let it run. Do not narrate its steps; when it finishes, report only the files it changed and anything blocking.

**Do not** hand it the eight tasks from §1 as new scope — they are 012's, they are verified by driving 012's own
build, and re-specifying them would fork the record.

---

## 6. Step 5 — close out

1. **Journal and tick.** Companion's own `after-implement` hook records per-task progress into
   `.spec-context.json`; the tick marks in `tasks.md` are yours, each carrying its evidence.

2. **Gates — all four, from the repository root.** Every one must exit 0. The naming guard must report `New: 0`; its
   `Grandfathered:` count is pre-existing debt that is deliberately passable, so a rising *new* count is the only
   thing to act on.

```powershell
pwsh -NoProfile -File tools/run-tests.ps1 -All
pwsh -NoProfile -ExecutionPolicy Bypass -File .github/scripts/validate-csharp-xaml-naming.ps1
pwsh -NoProfile -ExecutionPolicy Bypass -File .github/scripts/validate-sql-naming.ps1
pwsh -NoProfile -ExecutionPolicy Bypass -File .github/scripts/validate-database-schema.ps1
```

3. **Then converge.** `/speckit.companion.converge` assesses the delivered code against spec, plan and tasks and
   appends anything genuinely unbuilt as new tasks. Run it only after step 3, so it is assessing verified work.

4. **Commit and push**, ending the message with this repo's trailer:

```text
Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```

---

## 7. Machine rules that must not be broken

- **Every subagent runs on DeepSeek V4.1 Flash** — `model: "deepseek/deepseek-flash"`. That is the model whose
  display name is *DeepSeek V4.1 Flash* (`v4.1`); **`deepseek/deepseek-v4-flash` is a different model** (`v4`) and is
  not the one to use. Never substitute a GitHub-hosted model (Claude, GPT, Gemini, Grok, Kimi, MAI) — if DeepSeek is
  unavailable, say so and stop rather than silently switching provider.
- **`python` or `py` — never `python3`.** On these workstations `python3` is a 0-byte Microsoft Store alias that
  answers "Python was not found" and silently drops the Companion run journal.
- **Every `.specify/scripts/powershell/*.ps1` invocation needs `-ExecutionPolicy Bypass`**: every execution-policy
  scope reports `Undefined`, so the effective policy is Windows' `Restricted` default and the script fails before any
  logic runs.
- **`pwsh` (PowerShell 7) for repo scripts.** Windows PowerShell 5.1 mangles the BOM-less mock-master builder and its
  `-Check` cannot catch it. 5.1 also has no ternary operator.
- **Never commit a credential.** No PIN, password or connection string in a document, script, test or commit. The
  connection string comes from the environment; the SQL password goes through a client option file or
  `$env:SQLCMDPASSWORD`, never a command line.
- **Chat is plain English, end-user facing.** No raw tool output, no JSON, no stack traces, no whole-artifact dumps,
  and never a code block to show a change you made with a tool. Anything the person must do goes last under
  `## What you need to do`.
- **Never edit a master list by hand.** `Database/Mock/All*.sql` are generated by
  `build_mtm_mock_masters.ps1`; the internal-store aggregates under `Database/Tables`, `StoredProcedures` and `Seeds`
  remain hand-maintained. Every new artifact needs a `create.sql` **and** a `rollback.sql`, and needs
  `Database/Bootstrap/update_table_descriptions.sql` touched — that is mandatory whenever any SQL artifact under
  `Database/` is added, modified or removed.
- **MySQL 5.7 syntax is the target.** Verify any artifact on both 5.7 and the newer local server.

---

## 8. Pitfalls already paid for — do not re-derive them

- **A full `dotnet build` reports 48 warnings and they are not yours.** Every one is `NU1902` / `NU1903` about
  `System.Security.Cryptography.Pkcs` 9.0.4 — one moderate advisory (`GHSA-2fmv-rv2c-59x7`) and one **high**
  (`GHSA-846f-f764-76mx`) — repeated across the projects and their target frameworks. They are NuGet *audit* findings
  surfaced on restore, they pre-date this branch, and they are absent from an incremental build that does not
  re-restore, which is why the same tree can report `0 Warning(s)` on one run and `48 Warning(s)` on the next.
  **Zero errors is the signal that matters here.** Bumping the package is a repository-wide decision with its own
  build risk and is deliberately not part of this work — report it, do not "tidy" it mid-run.
- **MySQL silently ignores surplus call parameters and reports success.** A caller passing an argument the routine
  does not declare loses that value with nothing logged, nothing thrown and nothing on screen. This is the T112
  defect and the reason §2 exists. After bringing any store current, **verify with the drift report, not with a
  successful call.**
- **`WMC9999 … Could not find any resources appropriate for the specified culture … ErrorMessages.resources` is a
  masked XAML error, not an environment problem.** The shipped `XamlCompiler` is missing its error satellite, so any
  real XAML error surfaces only as this. Surface the real one by introducing a deliberate C# error, or by bisecting
  the XAML you just changed.
- **`PRI175` / `PRI224 root node not found`** is stale PRI artifacts or a running `MTM_Waitlist.exe` holding the
  output. Stop the app, clear stale `*.pri` under `obj/`/`bin/`, rebuild.
- **[RelayCommand] strips a trailing `Async`** from the generated command name: `ContinueToReviewAsync` binds as
  `ContinueToReviewCommand`. Binding the wrong name is a silent XAML failure that shows up only as the masked
  `WMC9999` above.
- **`x:Load="false"` elements are null in code-behind.** Touching one in a page constructor hangs the UI thread.
- **A `ContentDialog` owns its `XamlRoot` and blocks it**, so a page navigated to behind the dialog can never be
  seen. A surface opened over a dialog must be a `Window`.
- **`SetCursorPos` does not hover a WinUI control** — it posts no mouse message. `AutomationElement.FromPoint`
  answers "a pane" for WinUI content, so a hit test cannot confirm an aim either. Both facts are recorded in
  `tools/UI-AUTOMATION.md` §7 with the tool that works around them.
- **`$host` is a read-only automatic variable.** `$host = '172.16.1.104'` silently keeps the PowerShell `Host` object
  and every DB call then fails with `Unknown MySQL server host`. Use `$dbHost`.
- **`mysql.exe` stdout is not captured** by the tool terminal — use PowerShell with the repo's `MySqlConnector.dll`,
  or a script file, rather than inline `mysql` invocations.
- **New `Resources.resw` keys must match `Feature_Element.Property`.** The naming guard grandfathers the existing
  tooltip map but fails any *new* key in another shape; the tooltip map's pre-012 spellings (`Coil number`,
  `Home location`, `Scrap type`) already resolve through their canonical keys, and adding a second entry beside one
  is a **build break** (`PRI277: Conflicting values for resource`), because the resource index is case-insensitive.

---

## 9. Also outstanding on this branch

One leg of the tooltip/inspector work was never proven live, and this machine is the one to prove it on:

- **Every tooltip in the app now goes through `shared:TooltipBehavior`** (`TooltipCoverageTests` fails if one does
  not), and **the developer control inspector is a window**, not a page, so it can open over the modal dialog that
  raised it. The window itself was confirmed rendering correctly; what was **not** confirmed is the
  `Ctrl+Alt+Right-Click` shortcut, because a synthesised hover does not register on this workstation and the shortcut
  is armed by a real hover.
- On a machine where a real pointer works: hover a tracked control, press `Ctrl+Alt+Right-Click`, and confirm the
  window titled `Control details — …` opens — first over the shell, and then (the case that motivated the change)
  over a **modal** dialog such as Settings → manage request item images. `tools/uia-inspect-control.ps1` drives the
  hover-and-shortcut sequence and reports what it saw; `tools/UI-AUTOMATION.md` §7 records its two platform facts and
  the temporary boot-hook recipe used to see the window without a hover.
- If the shortcut is genuinely dead, suspect `IsPointerHoveringActiveElement` — it needs a real `PointerEntered` on
  the tracked element — rather than the tooltip.

---

## 10. Done looks like

1. `LIVE-DB-AND-MOCK-UPDATES.md`'s "In order" list is done through its step 6, and the verification queries in its
   §1.5 return what they expect.
2. This feature's own objects are confirmed on the same store — two columns, three routines, both seeds — proved by
   the drift report rather than by a call that appeared to succeed.
3. T067 and T068 closed with the plant's real location codes recorded, and the four scripts agreeing on whichever
   class the plant actually uses.
4. T048, T058, T069, T071, T079 and T088 ticked, each with the drive it was verified by and the criterion it met.
5. `speckit.companion.auto` run once over the open questions, minting its own new spec directory — 012's artifacts
   left intact.
6. All four gates exit 0; `tools/run-tests.ps1 -All` is 0 failed.
7. The branch committed and pushed.
