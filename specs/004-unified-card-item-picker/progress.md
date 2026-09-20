# Ralph Progress Log

Feature: 004-unified-card-item-picker
Started: 2026-09-20 07:25:58

## Codebase Patterns

- **Run SQL artifacts by piping into `mysql`, not with `source`.** `mysql -e "source file.sql"` fails with
  `ERROR 1064` because `source` is a client command, not SQL. Use
  `cmd /c "<mysql.exe> -h 127.0.0.1 -u root --default-character-set=utf8mb4 < <file.sql>"`.
- **`mysqldump` output cannot be replayed verbatim.** It carries a `SET @@GLOBAL.GTID_PURGED=...` line that fails with
  `ERROR 3546` on the same server. Strip any `GTID_PURGED` line before replaying a snapshot.
- **Read affected rows from a procedure with `ROW_COUNT()`.** Call the procedure, then call `ROW_COUNT()` as the very
  next statement in the same session. This is how FR-043's conditional update is proved rather than inspected.
- **`setup_active_jobs` column is `work_order`, not `job_number`**, and the request table is
  `waitlist_requests_queue`, not `waitlist_requests`.
- **Never write SQL through a PowerShell here-string.** `$` inside `@"..."@` gets interpolated (a `$[*]` in a regex
  silently becomes `[*]`). Write a `.sql` file with the editor instead.
- **The live store already diverged from the seeds before this work.** `100-3` held a real Setup save
  (`WO-055691` / `22-77725-110-Raw`, seq 19) dated 2026-09-14, and its `Die` subordinate carried the empty-Location
  `No Die` placeholder. Re-applying the jobs seed overwrites that row; snapshot before, restore after.
- **A `with` expression must be parenthesised before a method call.** `RequestJobPartAvailability.None with { … }
  .WithDies(…)` is a syntax error; write `(RequestJobPartAvailability.None with { … }).WithDies(…)`. New list
  members on that record are **init-only properties plus a `With…` helper**, never positional parameters.
- **This machine's `MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING` user variable breaks a test.**
  `StartupCoordinatorTests.RunAsync_WhenDatabaseConnectionStringIsMalformed_ReturnsBlockedAsync` expects the
  coordinator to block on a malformed literal, but the environment override wins, so it fails with
  `Assert.IsTrue failed` on `result.IsBlocked`. Clear both `MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING` and
  `MTM_WAITLIST_DB_CONNECTION_STRING` in the test process before reading a full-suite result.
- **Compare collections with `CollectionAssert.AreEqual`, not `Assert.AreEqual`.** Two `string[]` of equal contents
  fail `Assert.AreEqual`; every sibling assertion in this suite uses `CollectionAssert`.
- **A new wizard step is registered in two places, in flow order:** `pageService.Configure<VM, Page>()` beside the
  other steps and `services.AddTransient<VM>()` + `AddTransient<Page>()` in the same order. Its view model is
  reached by `NewRequestFlowRules.GetNextStepType(state).FullName!`, so the branch must sit **before** the
  `!requiresAnswer || answerCaptured → Preview` return.
- **`PageActivationAuditTests` covers every page generically** (parameterless constructor), but its
  `s_newRequestWizardPages` list holds only the linear spine — the alternative answer steps (`NewRequestDunnagePage`,
  and now `NewRequestDiePage`) are deliberately absent. Do **not** add a `ContinueCommand`-bearing step to
  `NewRequestWizardAdvanceMarkupTests`' choice-step list, which asserts that command is absent.
- **A `.ps1` written by an agent must be pure ASCII.** Windows PowerShell 5.1 reads a BOM-less `.ps1` as ANSI, so one
  em-dash inside a string literal corrupts the terminator and the file fails to parse. Use `-` and write generated
  scripts with an ASCII-only body.
- **`$PSScriptRoot` is the folder of the script being run, not the repo.** A helper generated into `%TEMP%` cannot
  derive the repo root from itself — hard-code the root or pass it in.
- **`Set-Content -Encoding UTF8` writes a BOM in PS 5.1.** For a clean UTF-8 file use
  `[System.IO.File]::WriteAllLines($path, $lines, (New-Object System.Text.UTF8Encoding($false)))`.
- **`$Error` is a read-only PowerShell automatic variable.** `foreach ($error in $errors)` aborts the loop with
  *"Cannot overwrite variable Error"* before printing anything, which turns a working guard into a guard that fails
  loudly with the wrong message and no findings. Never use `$error`/`$Error` as a loop variable.
- **The C#/XAML naming guard is diff-scoped by design.** `.github/workflows/csharp-xaml-naming-compliance.yml`
  builds its file list from `git diff --name-only $base $head` and calls the script with `-Files`, so a pull request
  that touches no C#/XAML/RESW file passes **and is not evidence that the tree is clean**. A repo-wide backlog of
  **910** findings sat behind that gate unmeasured until now.
- **Windows PowerShell 5.1 cannot run `.github/scripts/validate-database-schema.ps1`.** Its `Add-Type` of the
  isolated load context fails with *"The type or namespace name 'Loader' does not exist in the namespace
  'System.Runtime'"* — `System.Runtime.Loader` is not in the 5.1 framework — and the script exits **1 before it
  reaches a single check**, which reads as a schema failure and is not one. Always run it as
  `pwsh -NoProfile -ExecutionPolicy Bypass -File .github/scripts/validate-database-schema.ps1`. The CI workflow
  already uses `shell: pwsh`; do not "simplify" that to `powershell`.
- **Never resolve a build dependency with `Get-ChildItem -Recurse | Select-Object -First 1` over the NuGet cache.**
  `validate-database-schema.ps1` did that for `Microsoft.Extensions.Logging.Abstractions` and picked **1.1.1** — the
  first of 22 cached versions, nine majors behind the 10.0.10 the app resolves — so the isolated load context
  force-loaded the wrong assembly and MySqlConnector's logging type initializer threw. The script died before any
  check, with an error that names neither the file nor the real cause. Resolve from **beside the assembly that needs
  it** (the build output) first, and fall back to the **highest** cached version by parsing the version folder name
  in a `try/catch` — never to the first one the file system happens to enumerate. This is version-dependent
  behaviour: the CI runner's cache held only 10.0.10, so the same script worked there and failed only locally.
- **A red CI job is not evidence that a check ran.** `database-schema-validation.yml` has failed on **every** recent
  run (10 of 10, 2026-09-12 → 09-14) — and the reason is environmental: its connection-string secrets hold
  private-LAN hosts that a GitHub-hosted runner cannot reach (*"Unable to connect to any of the specified MySQL
  hosts"*, *"Connect Timeout expired."*), so the job fails honestly but for a reason that says nothing about the
  schema. With no secrets configured the same script skips and exits **0**. The job is therefore either vacuous or
  permanently red; read the log before treating either state as coverage.
- **`TRUNCATE` is refused on any table an FK references, whatever the row counts.** `Database/Seeds/
  seed_config_image_storage_settings/create.sql` opens with `TRUNCATE TABLE config_settings_values` and cannot
  succeed once `config_settings_history` exists with `fk_settings_history_settings_values_config_setting_id`
  (`NO ACTION`) — even though history held **0** rows against 3 values rows, so the block is vacuous and fatal. The
  seed is applied **first** of eight, so this breaks a fresh install and, because `Apply-Seeds` also runs on the
  success path, can turn an otherwise-green validation red at its final step. Use `DELETE FROM` where the FK must be
  honoured, or drop and re-add the FK around the truncate.
- **`git rm` needs `--` before the path list.** A repo path that begins with `-` is parsed as an option and the
  command fails or does nothing, silently. Use `git rm -r --quiet -- <paths>`.
- **A duplicate-create check must cover all four artifact kinds.** Scanning only `Database/Tables/**/create.sql`
  finds the table duplicates and misses the procedure, view and function duplicates created by the same rename —
  this repository had 11 pairs, not 3. Group every `create.sql` under `Database/**` by (kind, created object name)
  and fail on any name claimed twice.
- **A guard that runs after a connection-string gate never runs in CI.** `validate-database-schema.ps1` skipped
  its whole validation phase when the connection-string secrets were empty, so the duplicate creates passed every
  run; when the secrets *are* present the same phase cannot reach the private-LAN hosts and the job fails instead.
  Put static checks **before** the gate; they are the only checks a DB-less runner can perform.
- **`Database/Tables/20`–`29` hold `rollback.sql` only, by design.** They are the retained drops for the retired
  `mock_*` family and the retired request type/subtype catalog. `RetiredSymbolAuditTests.IsExemptFromSqlAudit`
  exempts any file named `rollback.sql` precisely so these can name an object that no longer exists. A create-less
  artifact folder is not corruption and must not be tidied away.

---


---
## Iteration 1 - 2026-09-20
**User Story**: Phase 9 Wave 9 gates - T137 (live database validation); partial progress on the Phase 9 batch
**Tasks Completed**: 
- [x] T137: Live database validation. Applied and reversed all three paired `create.sql` / `rollback.sql` artifacts against local `mtm_waitlist` (each exit 0, each reversal verified by post-state row counts), confirmed `AllSeeds.sql` and `Bootstrap/update_table_descriptions.sql` agree with the files on disk, and confirmed the seven prepared situations resolve from real work centres through the real read path.
**Tasks Remaining in Story**: 5 - T129, T130, T131, T133, T139 (running-app UI gates and the Success Criteria walk)
**Commit**: No commit - partial progress. Phase 9 is not complete: the UI gates T129/T130/T131/T133 and the criteria walk T139 are still open.
**Files Changed**: 
- specs/004-unified-card-item-picker/tasks.md (T137 ticked with recorded evidence)
- specs/004-unified-card-item-picker/progress.md (this entry)
**Learnings**:
- The three build traps still apply, but the Phase 9 remaining work is all running-app gates, so the next iteration needs a signed-in shell and the seven prepared situations present.
- The live store was already seeded with the seven situations, and `100-3` additionally held a real Setup save. The store was left in the seeded state with `100-3`'s live row restored verbatim, so the app gates can run without re-seeding.
- The guard change from this batch is now live locally (7-parameter `sp_waitlist_request_status_update`), so FR-043 behaviour can be exercised from the app as well as from SQL.
- FR-054 (one request per die) remains the only substantive unbuilt feature work; everything else outstanding is a verification gate or a documentation reconciliation.
---

---
## Iteration 2 - 2026-09-20 08:03
**User Story**: Phase 13 Wave 3 — One request per die (FR-054) and the request page's every die location (FR-057). Partial progress on the wave: the build is complete and verified, its two UI gates are not.
**Tasks Completed**: 
- [x] T186: Tests written first and run red. Three new files — `NewRequestDieViewModelTests` (8 cases), `NewRequestOneRequestPerDieTests` (6 cases) and `DestinationQuestionRetirementTests` (6 cases) — plus the two existing tests that named the retired question moved with it (`ResolveLine2_PickupDie_ShowsTheDieNumberAndLocationWhateverTheChosenDestination` is now `ResolveLine2_PickupDie_ShowsTheDieTheRequestIsFor`, and `RequestItemConfigurationServiceTests` carries no destination fixture any more).
- [x] T181: The destination question retired in one change. Both die rows in `create.sql` and `AllSeeds.sql` now read `'collect-input-then-confirm', 1, 'enum'` with `NULL` options and an answer field declaring `'list','die'`, so `deliver-die` left `direct-to-confirmation` and both Items reach the die step; `destination` left the Line 2 token set and the resolver branch was deleted. The CSV needed no edit — rows 3 and 17 already carried the 2026-09-20 D22 note. The docs moved with it: `spec.md` (scenario 2, SC-024, the retired-question bullet, the `pickup-component` note), `contracts/request-picker-flow.md` §10 (the "first die" rule replaced by "every die, in the job's own order") and `contracts/item-configuration.md` §5 (the `die` value added to the `list` table).
- [x] T182: The die step built and wired — `NewRequestDieViewModel`, `NewRequestDieOption`, `NewRequestDiePage.xaml` / `.xaml.cs`, the `die` branch in `GetNextStepType` placed before the Preview return beside the dunnage branch, the page configured and registered in flow order, and nine new resource keys.
- [x] T187: `RequestJobFieldValues.Resolve` now answers `die` with every die the job carries and `pickup location` with every location those dies record, blank entries dropped; the snapshot's primary die is used only when the job carries no die row of its own. `WaitlistRequestTitles` prefers the die the request itself stored.
- [x] T183: `NewRequestFlowState.ToDrafts()` yields one draft per selected die and `NewRequestSummaryViewModel.SubmitAsync` submits each in turn, counting the ones raised and reporting the count in plain language when more than one went through.
**Tasks Remaining in Story**: 2 - T188 and T184, the two running-app UI gates. Both need a signed-in session, and the masked `'0000'` credentials force a password change on first use, so neither can be walked headlessly.
**Commit**: No commit - partial progress. Wave 3's UI gates T184/T188 are unchecked, so the wave is not complete.
**Files Changed**: 
- MTM_Waitlist.Settings/Services/RequestItemLine2Resolver.cs, MTM_Waitlist.Settings/Models/RequestItemDefinition.cs, MTM_Waitlist.Settings/Services/RequestJobFieldValues.cs
- MTM_Waitlist.Waitlist.View/Models/WaitlistRequestTitles.cs, MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewDetailViewModel.cs
- MTM_Waitlist.Waitlist.NewRequest/Services/NewRequestFlowRules.cs, .../Models/NewRequestFlowState.cs, .../Models/NewRequestDieOption.cs, .../ViewModels/NewRequestDieViewModel.cs, .../ViewModels/NewRequestSummaryViewModel.cs
- Module_Waitlist/Views/NewRequestDiePage.xaml, Module_Waitlist/Views/NewRequestDiePage.xaml.cs
- Services/DependencyInjection/ServiceRegistrationExtensions.cs, Strings/en-us/Resources.resw
- Database/Seeds/seed_waitlist_request_item_configs/create.sql, Database/Seeds/AllSeeds.sql
- MTM_Waitlist.Tests/Module_Waitlist/ViewModels/NewRequestDieViewModelTests.cs, .../NewRequestOneRequestPerDieTests.cs, MTM_Waitlist.Tests/Module_Settings/Services/DestinationQuestionRetirementTests.cs
- MTM_Waitlist.Tests/Module_Settings/Services/{RequestItemConfigurationServiceTests,RequestJobFieldValuesTests}.cs, MTM_Waitlist.Tests/Module_Waitlist/Models/{NewRequestFlowStateTests,WaitlistRequestTitlesTests}.cs
- specs/004-unified-card-item-picker/{spec.md,data-model.md,research.md,tasks.md,progress.md}, specs/004-unified-card-item-picker/contracts/{card-and-identifier.md,request-picker-flow.md,item-configuration.md,verification-gates.md}
**Learnings**:
- **Two traps cost real time.** A `with` expression must be parenthesised before a method call (`(X with { … }).WithDies(…)`), and `Assert.AreEqual` on two `string[]` fails even when the contents match — the suite's convention is `CollectionAssert.AreEqual`.
- **The environment, not the code, failed one test.** `MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING` is set as a user variable on this machine and overrides the malformed literal `StartupCoordinatorTests.RunAsync_WhenDatabaseConnectionStringIsMalformed_ReturnsBlockedAsync` feeds in, so the coordinator does not block. Clearing it (and `MTM_WAITLIST_DB_CONNECTION_STRING`) makes it pass. Read a full-suite result with both cleared.
- **The snapshot's primary die is a fallback, not a merge.** A job whose `Dies` list is populated but whose locations are blank must yield **nothing** for a location — falling back to `job.DieLocation` there invented a location the job does not record. The fallback now applies only when the job carries no die row at all.
- **`deliver-die` had to leave `direct-to-confirmation`.** It never reached an answer step, so without moving it the die step would have been unreachable for half the die requests.
- **Not every new page belongs in every audit list.** `PageActivationAuditTests`'s parameterless-constructor scan already covers the new page; its `s_newRequestWizardPages` list holds only the linear spine, and `NewRequestWizardAdvanceMarkupTests`' choice-step list asserts `ContinueCommand` is **absent**, which the die page uses.
- **Verification evidence.** Build: `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false` → `0 Warning(s), 0 Error(s)`. Tests: `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64` → `total: 1136, failed: 0, succeeded: 1109, skipped: 27` (baseline 1089 total, so 47 tests added). The targeted filter over the touched suites was 79/79 green.
- **Next iteration's scope.** The only substantive feature work left in this file is the Phase 14 documentation and database-hygiene batches (T189 onward) and the database-artifact pairs (T213 onward). Everything else unchecked is a gate that needs a signed-in running app (T129/T130/T131/T133/T139, T164, T174, T184, T188) or the owner's own database reinstall (T165, which the task itself reserves to the owner).
---

---
## Iteration 3 - 2026-09-20
**User Story**: Phase 14 (US7, "The codebase is honest about itself") — Wave 4, "The guards that cannot fail" (T222-T226), plus the two documentation corrections that belong to the same defect class (T211, T212). Wave 4 is complete; US7 is not.
**Tasks Completed**:
- [x] T222: The SQL naming guard's failure path no longer dies on a reserved variable. Reproduced the defect first: `.github/scripts/validate-sql-naming.ps1` printed `SQL naming compliance failed:` followed by *"Cannot overwrite variable Error because it is read-only or constant"* at line 260 and exited 1 with **zero** findings, because `foreach ($error in $errors)` collides with PowerShell's read-only `$Error`. The loop variable is now `$violation`; the same run prints all eleven findings and exits 1.
- [x] T223: The eleven violations the crash was hiding are written up in `.github/reports/sql-naming-violations.md` with rule, file and line, and the rules file now says pre-existing violations are grandfathered while anything new still fails (D-7). Nothing was renamed. The eleven are seven filename-pattern failures (five files under `Database/InforVisual/Queues/Module_Mock/Populations/`, plus `Database/Mock.Service/Restore/replace_database.sql` and `verify_restore.sql`) and four boolean-column names (`require_password_change` in `01_core_users_profiles` and `AllTables.sql`, `requires_answer` in `31_waitlist_request_item_configs` and `AllTables.sql`).
- [x] T224: The guard no longer scans a wrong tree silently. It now resolves `Database/` from its own location and, when that folder is absent, prints the path it derived and exits 1 **before** scanning or changing directory. Proved by copying the script to `%TEMP%\sqlnaming-copytest\scripts\` and running it there: it named `…\Temp\Database` as the missing folder and exited 1, where it previously reported *"No SQL files found"* and exited 0. The scan itself now uses the absolute `$databaseRoot`.
- [x] T225: The C#/XAML rules file now matches its mechanism. The *Enforcement* section keeps the hard-fail, states that the check validates **the files a pull request changed**, warns that a run with no changed C#/XAML/RESW files passes by design and is not evidence the tree is clean, records that pre-existing violations are grandfathered and tracked in `.github/reports/csharp-xaml-naming-violations.md`, and keeps the written-approval requirement for exceptions. The *"Legacy naming must be bulk normalized (no grandfathering mode)"* clause is gone (D-4). The workflow was read rather than assumed: it derives its file list from the PR diff and calls the script with `-Files`, so the stated scope is the real one.
- [x] T226: `.github/reports/csharp-xaml-naming-violations.md` — every finding with rule, file and line, grouped by rule then sorted by file, with a per-rule summary and a per-project distribution. The report is the deliverable; no renames were made.
- [x] T211: `.github/instructions/database-schema-rules.instructions.md` no longer names the retired `core_workstations_registry`, and its migration section no longer describes a FluentMigrator runner. Verified first that `FluentMigrator` survives in the tree only as a description of the **retired** layout (`restructure-database-layout.ps1` and the validator's own comment).
- [x] T212: `Database/Database-Ruleset.md` *Core Startup Tables* now names `core_computers_registry`. `core_workstations_registry` survives in only three places in the tree after the edit — the two deliberate historical notes in `Database/Mock.Service/Restore/` that exist to record the rename, and nothing else. The same file's *Artifact Layout and Release Governance* section also still recommended FluentMigrator as the runner layer, the identical stale claim T211 corrected, so it was corrected in the same pass; leaving it would have recreated the contradiction between the two always-on rules files.
**Tasks Remaining in Story**: 46 unchecked tasks in the file. Within Phase 14 the documentation batch (T189-T210) and the database-artifact batch (T213-T221) are still open, as are T227-T232.
**Commit**: `e398ac1` — "fix(004): the guards that cannot fail, and the stale rules they hid" (7 files, +1566/-35). Committed as one coherent unit: the wave plus its two documentation siblings. US7 itself is not complete, so this is a checkpoint rather than a story completion. This progress entry lands in the follow-up docs commit. The unrelated in-flight work already in the working tree (the die-page feature files, `READINESS-CHECKLIST.md`, the spec and contract edits from iterations 1 and 2) was deliberately **not** swept in.
**Files Changed**:
- .github/scripts/validate-sql-naming.ps1 (T222 reserved-variable fix, T224 root guard)
- .github/instructions/database-schema-rules.instructions.md (T211 stale table name and migration model, T223 grandfathering clause)
- .github/instructions/csharp-xaml-naming-rules.instructions.md (T225 enforcement wording and real scope)
- .github/reports/sql-naming-violations.md (new, T223)
- .github/reports/csharp-xaml-naming-violations.md (new, T226)
- Database/Database-Ruleset.md (T212 table name and migration statement)
- specs/004-unified-card-item-picker/tasks.md (T211, T212, T222-T226 ticked with recorded evidence)
- specs/004-unified-card-item-picker/progress.md (this entry)
**Learnings**:
- **The guards were both broken, and both broken in the direction that hides work.** The SQL guard died on its own error path and reported nothing; the C#/XAML guard only ever looked at changed files, so a repo-wide backlog was invisible by design. A guard that cannot report is worse than no guard, because the green light is read as evidence.
- **The C#/XAML total is 910, not the 901 the plan recorded.** Re-measured live this iteration and confirmed against the report: 475 `Async Task method … must end with Async`, 377 `RESW key … must match Feature_Element.Property`, 33 `Private static readonly field … must be s_camelCase`, 24 `Public type … must match file name`, 1 `Private field … must be _camelCase`. The two dominant rules are 852 of the total, so a normalization pass should take those two first. The per-project split also drifted from the plan: Tests 493 (was 490) and Strings 359 (was 353).
- **The three duplicate table pairs are byte-identical, which unblocks T213.** `create.sql` **and** `rollback.sql` match by SHA256 for (`02_core_workstations_registry` / `02_core_computers_registry`), (`13_setup_workstations_catalog` / `13_setup_work_centers_catalog`) and (`14_config_workstation_hot_workcenters` / `14_config_computer_hot_work_centers`). In each pair the **legacy-named** folder is the stale half — every one of them creates the *new* table name, so the folder name is the only thing that is wrong. T213's proof condition is therefore already satisfied on this tree, and T214 is safe to run; T215 and T216 must follow in the same change to keep `AllTables.sql` and the bootstrap honest.
- **T206's premise is stale.** It says `capabilities/` is absent from the tree, but the directory exists and holds `startup/spec.md` and `startup-diagnostics/spec.md`. The task needs re-scoping to *restore or re-derive* `capabilities/DRIFT.md` rather than to create the folder.
- **Verification evidence.** SQL guard: eleven findings printed, exit 1. Wrong-tree guard: names the derived path, exit 1. C#/XAML guard: 910 findings, exit 1, matching the report line-for-line. No C#, XAML, RESW or `.csproj` file was touched this iteration, so there is no build or test signal to add to the last full-suite result (`total: 1136, failed: 0, succeeded: 1109, skipped: 27`).
- **Next iteration's scope.** T213 (prove the pairs) is already satisfied, so T214 → T215 → T216 (delete the stale half, then reconcile `AllTables.sql` and the bootstrap) is the natural next unit and is self-contained. Every other open task is either a running-app gate or reserved to the owner.
---
---
## Iteration 4 - 2026-09-20
**User Story**: Phase 14 Wave 3 - the database-artifact duplicate pairs (T213-T221), complete
**Tasks Completed**:
- [x] T213: proved all 11 duplicate-create pairs identical (SHA256, `create.sql` and `rollback.sql`)
- [x] T214: deleted the 11 superseded folders (22 files) with `git rm -r`
- [x] T215: swept for dangling references - none; the surviving folder is the only definition of each object
- [x] T216: reconciled the aggregate, the folder set and the bootstrap; proved a fresh install produces every table
- [x] T217: found and removed six duplicate *procedure* pairs, not just the tables
- [x] T218: recorded the rollback-only convention for `Database/Tables/20`-`29` in the ruleset
- [x] T219: confirmed and recorded that the mirror schema lives only under `Database/Mock/`
- [x] T220: taught `validate-database-schema.ps1` to fail on two artifacts creating one object
- [x] T221: established that the CI workflow is either vacuous or permanently red, and never schema-checked
**Tasks Remaining in Story**: None - the database-artifacts batch is complete. 37 tasks remain elsewhere in `tasks.md`.
**Commit**: the batch commit for this iteration (amended to carry the validator's assembly-resolution fix)
**Files Changed**:
- Database/Tables/02_core_workstations_registry/ (deleted)
- Database/Tables/13_setup_workstations_catalog/ (deleted)
- Database/Tables/14_config_workstation_hot_workcenters/ (deleted)
- Database/StoredProcedures/sp_setup_workstations_delete/ (deleted)
- Database/StoredProcedures/sp_setup_workstations_get_all/ (deleted)
- Database/StoredProcedures/sp_setup_workstations_touch/ (deleted)
- Database/StoredProcedures/sp_setup_workstations_upsert/ (deleted)
- Database/StoredProcedures/sp_config_hot_workcenters_delete_for_workstation/ (deleted)
- Database/StoredProcedures/sp_config_hot_workcenters_get_for_workstation/ (deleted)
- Database/Views/vw_setup_workstations_active/ (deleted)
- Database/Functions/fn_setup_workstation_name_normalized/ (deleted)
- .github/scripts/validate-database-schema.ps1 (T220 duplicate-object guard)
- Database/Database-Ruleset.md (T218, T219 review notes)
- specs/004-unified-card-item-picker/tasks.md (T213-T221 ticked with recorded evidence)
- specs/004-unified-card-item-picker/progress.md (this entry)
**Learnings**:
- **The duplicate defect was four artifact kinds wide, not one.** T217 asked only about procedures; the same rename left duplicate *views* and *functions* too. Counting the pairs that actually exist gives **11**, not the 3 the wave was scoped around: 3 tables, 6 procedures, 1 view, 1 function. All 11 are byte-identical in both files, and in every pair the legacy-named folder creates the **renamed** object - so the folder name is the only thing wrong, and deleting the stale half cannot change behaviour.
- **Deleting the stale half needed no aggregate edit.** `AllTables.sql` never listed the duplicate folders, so it already held each table exactly once. Verified as a **set equality**, not a spot check: 21 aggregate names and 21 folder names, nothing only-in-aggregate, nothing only-in-folders, no duplicate in the aggregate.
- **The fresh-install proof can be run without touching the live store.** `AllTables.sql` opens with `USE mtm_waitlist;`, so the file cannot be piped into a scratch schema as-is - strip that line, create `mtm_waitlist_probe`, apply, count, drop. The apply exited 0 and produced exactly **21** base tables including all three renamed ones; `mtm_waitlist_probe` was dropped afterwards and the live store was never in the path.
- **The live store now matches the aggregate exactly** - 21 base tables against 21 aggregate creates, same names. That is a stronger statement than the previous iteration's notes carried, and it means the T215 sweep's conclusion ("no dangling references") is corroborated by the running database rather than only by grep.
- **The duplicate creates survived CI because no schema check has ever run there, and the reason is structural.** `validate-database-schema.ps1` applies the table creates in order, so a second create of the same table is not an error to MySQL - it is a no-op overwrite. The check that would have caught it (two artifacts, one object) did not exist, and the phases that *would* have caught it downstream cannot run on a runner at all: with the connection-string secrets configured - and they **are** configured - the hosts they name are private-LAN addresses, so the job dies on *"Unable to connect to any of the specified MySQL hosts"* / *"Connect Timeout expired."* before reaching the schema. With no secrets it skips and exits 0 instead. Either way the tree is never compared against a database. The new guard sits **before** the gate for exactly that reason: it is the only check in the job that a runner can actually perform.
- **A red CI job is not coverage.** All 10 of the most recent runs of `database-schema-validation.yml` failed (2026-09-12 to 2026-09-14), and the failure is environmental - unreachable LAN hosts - so it says nothing about the schema. The job's two reachable states are "vacuous, exit 0" and "permanently red"; neither is a check. T221's real finding is narrower and less comfortable than the task assumed: the workflow never failed for a *schema* reason, and it also never passed for one.
- **A seed can be unable to run at all, and the failure is vacuous.** With a reachable store the validator now gets past the schema check and dies in its seed phase: `Database/Seeds/seed_config_image_storage_settings/create.sql` opens with `TRUNCATE TABLE config_settings_values`, which MySQL refuses once `config_settings_history` exists with `fk_settings_history_settings_values_config_setting_id` (`NO ACTION`) - regardless of row counts. History held **0** rows against 3 values rows, so the constraint blocks nothing real and the statement fails anyway. It is applied **first** of eight, so this breaks a fresh install; and because `Apply-Seeds` also runs on the success path, it can turn an otherwise-green validation red at its final step. Reported, not fixed here - it is a seed defect rather than part of the artifact-deduplication unit.
- **A destructive-sounding run wrote nothing, and that was verified rather than assumed.** The refused `TRUNCATE` aborted the seed loop on its **first** file, so no later seed ran and no partial state was left. Confirmed against the store afterwards: `config_settings_values` still holds its original 3 rows and **0** rows matching `image_storage.%` (the seed would have written 4).
- **The validator could not run locally at all, for a reason that never affected CI.** It resolved `Microsoft.Extensions.Logging.Abstractions` with `Get-ChildItem -Recurse | Select-Object -First 1`, which returns **1.1.1** - the oldest of 22 cached versions, nine majors behind the 10.0.10 the app resolves - and the isolated load context force-loaded it, so MySqlConnector's logging type initializer threw and the script died before any check. The runner's cache held only 10.0.10, which is why CI reported connection errors instead. Fixed by preferring the copy beside `MySqlConnector.dll` and taking the **highest** cached version as the fallback.
- **PowerShell 5.1 is a trap for this script.** The validator cannot run under 5.1 at all (`System.Runtime.Loader` absent), and it exits 1 with a message that looks like a schema failure. Anyone who runs it with `powershell` instead of `pwsh` will read a false red. Recorded in the patterns section above so the next iteration does not have to rediscover it.
- **Pre-existing uncommitted work was left alone.** The working tree carried substantive uncommitted changes from earlier iterations (`MTM_Waitlist.Settings`, `MTM_Waitlist.Waitlist.NewRequest`, `MTM_Waitlist.Waitlist.View`, `Services/DependencyInjection`, several test files, `Database/Seeds/*`, and `.specify/extensions/ralph/scripts/powershell/ralph-loop.ps1`) - 366 insertions across 16 files. They are outside this unit's scope and were not verified by this iteration, so they were deliberately **not** swept into this commit even though the loop recipe suggests `git add -A`. They remain uncommitted and are flagged for the owner below.
- **Verification evidence.** Duplicate scan after deletion: 0 duplicate creates across 21 table and 84 routine/view `create.sql` files. Validator on a clean tree with no connection-string variables, under `pwsh`: *"Duplicate-object check passed: 94 artifact(s) create 94 distinct object(s)."* then *"Skipping validation because no configured connection-string env vars are set."*, exit 0. Validator with a deliberately recreated duplicate: names the table and both paths, exit 1. Live store: for the workstation/work-centre family only the renamed objects exist. No C#, XAML, RESW or `.csproj` file was touched this iteration, so the last full-suite result (`total: 1136, failed: 0, succeeded: 1109, skipped: 27`) still stands and no build or test run was needed.
- **Next iteration's scope.** The remaining 37 tasks are dominated by running-app UI gates (T129-T133, T139, T164, T174, T184, T188), the owner's database reinstall (T165), and the documentation batch (T189-T210). The documentation batch is the only self-contained, verifiable unit left that does not need a signed-in app; the UI gates need a valid account credential and a running Debug build.
---
